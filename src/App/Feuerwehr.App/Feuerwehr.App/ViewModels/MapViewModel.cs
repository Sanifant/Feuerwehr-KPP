using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Rendering;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using Mapsui.Widgets.InfoWidgets;

namespace De.Openelp.Feuerwehr.App.ViewModels
{
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

        public MapViewModel(IHydrantService service, IGpsService gpsService)
        {

            this.hydrantService = service;
            this.gpsService = gpsService;

            this.InitializeMap();
            this.DrawHydrants();
        }

        [RelayCommand]
        /// <summary>
        /// Executes zoom In.
        /// </summary>
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

        [RelayCommand]
        /// <summary>
        /// Executes zoom Out.
        /// </summary>
        public void ZoomOut()
        {
            this.CanZoomIn = true;
            this.zoomFactor++;
            this.Map?.Navigator.ZoomTo(this.zoomFactor);
        }

        [RelayCommand]
        /// <summary>
        /// Executes center On Location.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task CenterOnLocationAsync(CancellationToken cancellationToken)
        {
            GpsLocation? location = await this.GetLocationForCommandAsync(cancellationToken);

            if (location != null)
            {

                MPoint startPoint = new MPoint(location.Longitude, location.Latitude);
                (double x, double y) spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);
                this.Map?.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);
                this.zoomFactor = 10;
                this.Map?.Navigator.ZoomTo(this.zoomFactor);
            }
        }

        [RelayCommand]
        /// <summary>
        /// Executes settings.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task SettingsAsync(CancellationToken cancellationToken)
        {
            GpsLocation? location = await this.GetLocationForCommandAsync(cancellationToken);

            if (location != null)
            {
                this.IsAddVisible = true;
                MPoint startPoint = new MPoint(location.Longitude, location.Latitude);

                this.NewHydrant = new Hydrant
                {
                    Latitude = startPoint.Y,
                    Longitude = startPoint.X,
                    Id = 0
                };
            }
        }

        [RelayCommand]
        /// <summary>
        /// Adds the hydrant.
        /// </summary>
        public void AddHydrant()
        {
            this.IsAddVisible = false;
        }

        [RelayCommand]
        /// <summary>
        /// Executes map Clicked.
        /// </summary>
        /// <param name="mapEventArgs">The map Event Args.</param>
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
        /// Initializes the location.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task InitializeLocationAsync(CancellationToken cancellationToken = default)
        {
            GpsLocation? location = await this.gpsService.GetCurrentLocationAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Map? map = this.Map;

            if (location != null && map != null)
            {

                MPoint startPoint = new MPoint(location.Longitude, location.Latitude);
                (double x, double y) spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);
                map.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);

                // --- 2. Einen Pin (Marker) hinzufügen
                PointFeature pinFeature = new PointFeature(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);

                // Stylen des Pins (z.B. ein roter Punkt)
                pinFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 1,
                    Fill = new Brush { Color = new Color { A = 255, R = 255, G = 0, B = 0 } } // Rot
                });

                // Layer für den Pin erstellen und der Karte hinzufügen
                MemoryLayer memoryLayer = new MemoryLayer
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
            Map map = new Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer(userAgent: "FeuerwehrApp/1.0"));

            map.Navigator.ZoomTo(this.zoomFactor);
            this.Map = map;
        }

        private IEnumerable<ILayer> HydrantLayer(List<HydrantPin> hydrants)
        {
            List<PointFeature> hydrantFeatures = new List<PointFeature>();

            foreach (HydrantPin hydrant in hydrants)
            {
                (double x, double y) mercatorCordinate = SphericalMercator.FromLonLat(hydrant.Longitude, hydrant.Latitude);
                PointFeature hydrantFeature = new PointFeature(mercatorCordinate);

                hydrantFeature["Model"] = hydrant;

                hydrantFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 1,
                    SymbolType = SymbolType.Triangle,
                    Fill = new Brush { Color = new Color { A = 255, R = 0, G = 0, B = 255 } } // Blau
                });

                hydrantFeatures.Add(hydrantFeature);
            }

            MemoryLayer hydrantLayer = new MemoryLayer
            {
                Name = "Hydrants",
                Features = hydrantFeatures
            };

            return new List<ILayer> { hydrantLayer };
        }
    }
}
