using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.Common.Models;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;

namespace De.Openelp.Feuerwehr.App.ViewModels
{
    /// <summary>
    /// Provides bindable state and commands for the map view.
    /// </summary>
    public partial class MapViewModel : ViewModelBase
    {

        [ObservableProperty]
        private Map? _map;

        [ObservableProperty]
        private HydrantPin _selectedHydrant = new ();

        [ObservableProperty]
        private bool _isFlyoutOpen;

        [ObservableProperty]
        private bool _canZoomIn = true;

        [ObservableProperty]
        private bool _canAddHydrant = true;

        [ObservableProperty]
        private bool _isAddVisible = false;

        [ObservableProperty]
        private Hydrant _newHydrant = new ();

        private double zoomFactor = 6;
        private readonly IHydrantService hydrantService;
        private readonly IGpsService gpsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapViewModel" /> class with its required dependencies.
        /// </summary>
        /// <param name="service">The hydrant service handling the requested operations.</param>
        /// <param name="gpsService">The platform location service used to locate the device.</param>
        public MapViewModel(IHydrantService service, IGpsService gpsService)
        {

            this.hydrantService = service;
            this.gpsService = gpsService;

            this.InitializeMap();
            this.DrawHydrants();
        }

        /// <summary>
        /// Increases the map zoom level.
        /// </summary>
        [RelayCommand]
        public void ZoomIn()
        {
            if (this.zoomFactor == 1)
            {
                this.CanZoomIn = false;
                return;
            }

            this.zoomFactor--;
            this.Map?.Navigator.ZoomTo(this.zoomFactor);
        }

        /// <summary>
        /// Decreases the map zoom level.
        /// </summary>
        [RelayCommand]
        public void ZoomOut()
        {
            this.CanZoomIn = true;
            this.zoomFactor++;
            this.Map?.Navigator.ZoomTo(this.zoomFactor);
        }

        /// <summary>
        /// Requests the device position and centers the map when a location is available.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        [RelayCommand]
        public async Task CenterOnLocationAsync(CancellationToken cancellationToken)
        {
            GpsLocation? location = await this.GetLocationForCommandAsync(cancellationToken);

            if (location != null)
            {

                MPoint startPoint = new (location.Longitude, location.Latitude);
                (double x, double y) spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);
                this.Map?.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);
                this.zoomFactor = 10;
                this.Map?.Navigator.ZoomTo(this.zoomFactor);
            }
        }

        /// <summary>
        /// Requests the current location before opening hydrant creation at the returned coordinates.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        [RelayCommand]
        public async Task SettingsAsync(CancellationToken cancellationToken)
        {
            GpsLocation? location = await this.GetLocationForCommandAsync(cancellationToken);

            if (location != null)
            {
                this.IsAddVisible = true;
                MPoint startPoint = new (location.Longitude, location.Latitude);

                this.NewHydrant = new Hydrant
                {
                    Latitude = startPoint.Y,
                    Longitude = startPoint.X,
                    Id = 0
                };
            }
        }

        /// <summary>
        /// Opens hydrant creation using the selected location.
        /// </summary>
        [RelayCommand]
        public void AddHydrant()
        {
            this.IsAddVisible = false;
        }

        /// <summary>
        /// Handles selection of a hydrant or map element at the clicked map position.
        /// </summary>
        /// <param name="mapEventArgs">The map tap event containing the selected display coordinates.</param>
        [RelayCommand]
        public void MapClicked(TappedEventArgs? mapEventArgs)
        {
            if (this.Map is null || mapEventArgs?.Source is not MapControl mapControl)
            {
                this.IsFlyoutOpen = false;
                return;
            }

            ILayer? layer = this.Map.Layers.FirstOrDefault(l => l.Name == "Hydrants");
            if (layer is null)
            {
                this.IsFlyoutOpen = false;
                return;
            }

            Point tapPosition = mapEventArgs.GetPosition(mapControl);
            MapInfo mapInfo = mapControl.GetMapInfo(new ScreenPosition(tapPosition.X, tapPosition.Y), new[] { layer });

            if (mapInfo?.Feature is PointFeature clickedFeature && clickedFeature["Model"] is HydrantPin hydrantPin)
            {
                this.SelectedHydrant = hydrantPin;
                this.IsFlyoutOpen = true;
            }
            else
            {
                this.IsFlyoutOpen = false;
            }
        }

        /// <summary>
        /// Loads the initial device position and updates the current-location pin when a location is available.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task InitializeLocationAsync(CancellationToken cancellationToken = default)
        {
            GpsLocation? location = await this.gpsService.GetCurrentLocationAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Map? map = this.Map;

            if (location != null && map != null)
            {

                MPoint startPoint = new (location.Longitude, location.Latitude);
                (double x, double y) spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);
                map.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);

                // --- 2. Einen Pin (Marker) hinzufügen
                PointFeature pinFeature = new (spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);

                // Stylen des Pins (z.B. ein roter Punkt)
                pinFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 1,
                    Fill = new Brush { Color = new Color { A = 255, R = 255, G = 0, B = 0 } } // Rot
                });

                // Layer für den Pin erstellen und der Karte hinzufügen
                MemoryLayer memoryLayer = new ()
                {
                    Name = "Pins",
                    Features = new List<IFeature> { pinFeature }
                };
                ILayer? previousPins = map.Layers.FirstOrDefault(layer => layer.Name == "Pins");
                if (previousPins != null)
                {
                    _ = map.Layers.Remove(previousPins);
                }

                map.Layers.Add(memoryLayer);
            }
        }

        private void DrawHydrants()
        {
            _ = this.hydrantService.GetAllAsync().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    List<HydrantPin> hydrants = task.Result.Select(h => new HydrantPin
                    {
                        Diameter = h.DiameterDescription,
                        Latitude = h.Latitude,
                        Longitude = h.Longitude
                    }).ToList();

                    IEnumerable<ILayer> layers = this.HydrantLayer(hydrants);

                    this.Map.Layers.Add(layers);
                }
            });
        }

        private async Task<GpsLocation?> GetLocationForCommandAsync(CancellationToken cancellationToken)
        {
            try
            {
                GpsLocation? location = await this.gpsService.GetCurrentLocationAsync(cancellationToken);
                return cancellationToken.IsCancellationRequested ? null : location;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Bound commands must complete normally when navigation cancels them.
                return null;
            }
        }

        private void InitializeMap()
        {
            Map map = new ();
            map.Layers.Add(OpenStreetMap.CreateTileLayer(userAgent: "FeuerwehrApp/1.0"));

            map.Navigator.ZoomTo(this.zoomFactor);
            this.Map = map;
        }

        private IEnumerable<ILayer> HydrantLayer(List<HydrantPin> hydrants)
        {
            List<PointFeature> hydrantFeatures = new ();

            foreach (HydrantPin hydrant in hydrants)
            {
                (double x, double y) mercatorCordinate = SphericalMercator.FromLonLat(hydrant.Longitude, hydrant.Latitude);
                PointFeature hydrantFeature = new (mercatorCordinate);

                hydrantFeature["Model"] = hydrant;

                hydrantFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 1,
                    SymbolType = SymbolType.Triangle,
                    Fill = new Brush { Color = new Color { A = 255, R = 0, G = 0, B = 255 } } // Blau
                });

                hydrantFeatures.Add(hydrantFeature);
            }

            MemoryLayer hydrantLayer = new ()
            {
                Name = "Hydrants",
                Features = hydrantFeatures
            };

            return new List<ILayer> { hydrantLayer };
        }
    }
}
