using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Builds Mapsui layers from situation-map geometries, symbols, and hydrant coordinates.
    /// </summary>
    public static class SituationMapLayers
    {
        /// <summary>
        /// Projects map-element GeoJSON into Mapsui features with symbols, colors, and labels.
        /// </summary>
        /// <param name="elements">The situation-map elements whose GeoJSON and display attributes form the layer.</param>
        /// <returns>A Mapsui layer containing the projected map elements and their display styles.</returns>
        public static MemoryLayer CreateElements(IEnumerable<MapElementDto> elements)
        {
            List<IFeature> features = new ();
            foreach (MapElementDto element in elements)
            {
                using JsonDocument json = JsonDocument.Parse(element.GeometryJson);
                JsonElement coordinates = json.RootElement.GetProperty("coordinates");
                Color color = Color.FromString(element.ColorHex);
                IFeature feature;
                if (element.ElementType == MapElementType.Symbol)
                {
                    Coordinate point = Project(coordinates);
                    feature = new PointFeature(point.X, point.Y);
                    if (!string.IsNullOrWhiteSpace(element.SymbolSvg))
                    {
                        // Normalize dimensions like the web map: symbols occupy 48 x 48 screen units.
                        XElement svg = XElement.Parse(element.SymbolSvg);
                        svg.SetAttributeValue("width", "48");
                        svg.SetAttributeValue("height", "48");
                        feature.Styles.Add(new ImageStyle { Image = "svg-content://" + svg });
                    }
                    else
                        feature.Styles.Add(new SymbolStyle { Fill = new Brush(color), Outline = new Pen(Color.White, 2) });
                }
                else
                {
                    LinearRing[] rings = coordinates.EnumerateArray()
                        .Select(ring => new LinearRing(ring.EnumerateArray().Select(Project).ToArray())).ToArray();
                    feature = new GeometryFeature { Geometry = new Polygon(rings[0], rings.Skip(1).ToArray()) };
                    feature.Styles.Add(new VectorStyle
                    {
                        Fill = new Brush(new Color(color.R, color.G, color.B, 102)),
                        Outline = new Pen(color, 2)
                    });
                }

                feature["Element"] = element;
                string? label = element.ElementType == MapElementType.Symbol
                    ? element.RadioCallName ?? element.Label : element.Label;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    feature.Styles.Add(new LabelStyle
                    {
                        Text = label,
                        ForeColor = Color.Black,
                        Halo = new Pen(Color.White, 2),
                        Offset = new Offset(0, -32)
                    });
                }

                features.Add(feature);
            }

            return new MemoryLayer { Name = "Lage", Style = null, Features = features };
        }

        /// <summary>
        /// Projects hydrant locations into a Mapsui point layer.
        /// </summary>
        /// <param name="hydrants">The hydrant locations to project into the map layer.</param>
        /// <returns>A Mapsui layer containing the projected hydrant points.</returns>
        public static MemoryLayer CreateHydrants(IEnumerable<MapHydrantDto> hydrants)
        {
            return new MemoryLayer
            {
                Name = "Hydranten",
                Style = new SymbolStyle { SymbolType = SymbolType.Triangle, Fill = new Brush(Color.Blue), Outline = new Pen(Color.White, 1) },
                Features = hydrants.Select(hydrant =>
                {
                    (double x, double y) point = SphericalMercator.FromLonLat(hydrant.Longitude, hydrant.Latitude);
                    return new PointFeature(point.x, point.y);
                }).ToArray()
            };
        }

        private static Coordinate Project(JsonElement coordinate)
        {
            (double x, double y) point = SphericalMercator.FromLonLat(coordinate[0].GetDouble(), coordinate[1].GetDouble());
            return new Coordinate(point.x, point.y);
        }
    }
}
