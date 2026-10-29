using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;

namespace de.openelp.feuerwehr.App.Services;

public static class SituationMapLayers
{
    public static MemoryLayer CreateElements(IEnumerable<MapElementDto> elements)
    {
        var features = new List<IFeature>();
        foreach (var element in elements)
        {
            using var json = JsonDocument.Parse(element.GeometryJson);
            var coordinates = json.RootElement.GetProperty("coordinates");
            var color = Color.FromString(element.ColorHex);
            IFeature feature;
            if (element.ElementType == MapElementType.Symbol)
            {
                var point = Project(coordinates);
                feature = new PointFeature(point.X, point.Y);
                if (!string.IsNullOrWhiteSpace(element.SymbolSvg))
                {
                    // Normalize dimensions like the web map: symbols occupy 48 x 48 screen units.
                    var svg = XElement.Parse(element.SymbolSvg);
                    svg.SetAttributeValue("width", "48");
                    svg.SetAttributeValue("height", "48");
                    feature.Styles.Add(new ImageStyle { Image = "svg-content://" + svg });
                }
                else
                    feature.Styles.Add(new SymbolStyle { Fill = new Brush(color), Outline = new Pen(Color.White, 2) });
            }
            else
            {
                var rings = coordinates.EnumerateArray()
                    .Select(ring => new LinearRing(ring.EnumerateArray().Select(Project).ToArray())).ToArray();
                feature = new GeometryFeature { Geometry = new Polygon(rings[0], rings.Skip(1).ToArray()) };
                feature.Styles.Add(new VectorStyle
                {
                    Fill = new Brush(new Color(color.R, color.G, color.B, 102)),
                    Outline = new Pen(color, 2)
                });
            }

            feature["Element"] = element;
            var label = element.ElementType == MapElementType.Symbol
                ? element.RadioCallName ?? element.Label : element.Label;
            if (!string.IsNullOrWhiteSpace(label))
                feature.Styles.Add(new LabelStyle
                {
                    Text = label,
                    ForeColor = Color.Black,
                    Halo = new Pen(Color.White, 2),
                    Offset = new Offset(0, -32)
                });
            features.Add(feature);
        }
        return new MemoryLayer { Name = "Lage", Style = null, Features = features };
    }

    public static MemoryLayer CreateHydrants(IEnumerable<MapHydrantDto> hydrants)
    {
        return new MemoryLayer
        {
            Name = "Hydranten",
            Style = new SymbolStyle { SymbolType = SymbolType.Triangle, Fill = new Brush(Color.Blue), Outline = new Pen(Color.White, 1) },
            Features = hydrants.Select(hydrant =>
            {
                var point = SphericalMercator.FromLonLat(hydrant.Longitude, hydrant.Latitude);
                return new PointFeature(point.x, point.y);
            }).ToArray()
        };
    }

    private static Coordinate Project(JsonElement coordinate)
    {
        var point = SphericalMercator.FromLonLat(coordinate[0].GetDouble(), coordinate[1].GetDouble());
        return new Coordinate(point.x, point.y);
    }
}
