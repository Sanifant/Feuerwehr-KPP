using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Feuerwehr.App.Models;
using Feuerwehr.App.Services;
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

namespace Feuerwehr.App.ViewModels
{
    public partial class MapViewModel : ViewModelBase
    {

        [ObservableProperty]
        private Map? _map;

        [ObservableProperty]
        private HydrantPin _selectedHydrant = new();

        [ObservableProperty]
        private bool _isFlyoutOpen;


        private readonly IHydrantService hydrantService;

        public MapViewModel(IHydrantService service)
        {
            InitializeMap();

            hydrantService = service;

            DrawHydrants();

            Map.PointerPressed += MapClicked;   
        }

        private void DrawHydrants()
        {
            hydrantService.GetAllAsync().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var hydrants = task.Result.Select(h => new HydrantPin
                    { 
                        Diameter = h.DiameterDescription,
                        Latitude = h.Latitude,
                        Longitude = h.Longitude
                    }).ToList();

                    var layers = HydrantLayer(hydrants);

                    Map.Layers.Add(layers);
                }
            });
        }

        private void MapClicked(object? sender, MapEventArgs e)
        {
            //System.Diagnostics.Debugger.Break();

            if(sender is Map map)
            {
            }
        }

        [RelayCommand]
        public void ZoomIn()
        { }

        [RelayCommand]
        public void ZoomOut()
        { }

        [RelayCommand]
        public void CenterOnLocation()
        { }

        [RelayCommand]
        public void Settings()
        { }

        [RelayCommand]
        public void MapClicked(TappedEventArgs? mapEventArgs)
        {
            if (Map is null || mapEventArgs?.Source is not MapControl mapControl)
            {
                IsFlyoutOpen = false;
                return;
            }

            var layer = Map.Layers.FirstOrDefault(l => l.Name == "Hydrants");
            if (layer is null)
            {
                IsFlyoutOpen = false;
                return;
            }

            var tapPosition = mapEventArgs.GetPosition(mapControl);
            var mapInfo = mapControl.GetMapInfo(new ScreenPosition(tapPosition.X, tapPosition.Y), new[] { layer });

            if (mapInfo?.Feature is PointFeature clickedFeature && clickedFeature["Model"] is HydrantPin hydrantPin)
            {
                SelectedHydrant = hydrantPin;
                IsFlyoutOpen = true;
            }
            else
            {
                IsFlyoutOpen = false;
            }
        }

        private void InitializeMap()
        {
            Map map = new Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            var startPoint = new MPoint(10.638436, 53.929134);
            var spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);
            map.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);
            map.Navigator.ZoomTo(6);

            // --- 2. Einen Pin (Marker) hinzufügen
            var pinFeature = new PointFeature(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);

            // Stylen des Pins (z.B. ein roter Punkt)
            pinFeature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 1,
                Fill = new Brush { Color = new Color { A = 255, R = 255, G = 0, B = 0 } } // Rot
            });

            // Layer für den Pin erstellen und der Karte hinzufügen
            var memoryLayer = new MemoryLayer
            {
                Name = "Pins",
                Features = new List<IFeature> { pinFeature }
            };
            map.Layers.Add(memoryLayer);


            Map = map;
        }

        private IEnumerable<ILayer> HydrantLayer(List<HydrantPin> hydrants)
        {
            var hydrantFeatures = new List<PointFeature>();

            foreach (var hydrant in hydrants)
            {
                var mercatorCordinate = SphericalMercator.FromLonLat(hydrant.Longitude, hydrant.Latitude);
                var hydrantFeature = new PointFeature(mercatorCordinate);

                hydrantFeature["Model"] = hydrant;

                hydrantFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 1,
                    SymbolType = SymbolType.Triangle,
                    Fill = new Brush { Color = new Color { A = 255, R = 0, G = 0, B = 255 } } // Blau
                });

                hydrantFeatures.Add(hydrantFeature);
            }

            var hydrantLayer = new MemoryLayer
            {
                Name = "Hydrants",
                Features = hydrantFeatures,
                Tag = new LayerInfo { IsMapInfoLayer = true }
            };

            return new List<ILayer> { hydrantLayer };
        }
    }

    internal class LayerInfo
    {
        public bool IsMapInfoLayer { get; set; }
    }
}
