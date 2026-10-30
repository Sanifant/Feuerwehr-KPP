namespace De.Openelp.Feuerwehr.App.Models;

/// <summary>A device location in WGS84 coordinates (degrees).</summary>
public sealed class GpsLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

