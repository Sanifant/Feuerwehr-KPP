using System;
using System.Threading.Tasks;

namespace Feuerwehr.App.Services
{
    

    public interface IGpsService
    {
        Task<GpsLocation?> GetCurrentLocationAsync();
    }

    public class GpsLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class GpsService : IGpsService
    {
        public async Task<GpsLocation?> GetCurrentLocationAsync()
        {
            /*
            try
            {
                // 1. Abfrage-Genauigkeit definieren
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

                // 2. Position abfragen (Fragt den Nutzer im Hintergrund nach Rechten, falls nötig)
                Location? location = await Geolocation.Default.GetLocationAsync(request);

                return location; // Enthält .Latitude und .Longitude
            }
            catch (FeatureNotSupportedException)
            {
                // GPS wird auf diesem Gerät/Plattform nicht unterstützt (z.B. mancher Linux-Desktop)
                System.Diagnostics.Debug.WriteLine("GPS auf diesem Gerät nicht unterstützt.");
                return null;
            }
            catch (PermissionException)
            {
                // Der Nutzer hat den Zugriff auf den Standort verweigert
                System.Diagnostics.Debug.WriteLine("Standort-Berechtigung verweigert.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei der Standortermittlung: {ex.Message}");
                return null;
            }
            */

            return new GpsLocation
            {
                Latitude = 53.929134, // Beispiel: Stockelsdorf / Pohnsdorf
                Longitude = 10.638436
            };
        }
    }
}
