using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;

namespace De.Openelp.Feuerwehr.App.ViewModels
{
    /// <summary>
    /// Executes situation Map View Model.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="authService">The auth Service.</param>
    public partial class SituationMapViewModel(ISituationMapService service, IAuthService authService) : ViewModelBase
    {
        private Guid? displayedIncidentId;
        private double zoomFactor = 6;

        [ObservableProperty]
        private Map? map;

        [ObservableProperty]
        private bool _canZoomIn = true;

        [ObservableProperty]
        private string incidentTitle = "Lagekarte";

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private string lastUpdated = string.Empty;

        public bool CanView => authService.IsAuthenticated && (authService.UserRoles.Contains("SituationMapViewer") || authService.UserRoles.Contains("SituationMapEditor"));

        /// <summary>
        /// Executes activate.
        /// </summary>
        public void Activate()
        {
            authService.AuthStateChanged += this.OnAuthStateChanged;

            Map currentMap = new Map();
            currentMap.Layers.Add(OpenStreetMap.CreateTileLayer(userAgent: "FeuerwehrApp/1.0"));

            currentMap.Navigator.ZoomTo(this.zoomFactor);

            this.Map = currentMap;
        }

        /// <summary>
        /// Executes deactivate.
        /// </summary>
        public void Deactivate()
        {
            authService.AuthStateChanged -= this.OnAuthStateChanged;
            this.RefreshCommand.Cancel();
            this.Clear();
        }

        [RelayCommand(CanExecute = nameof(CanView))]
        /// <summary>
        /// Executes refresh.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            if (!this.CanView)
            {
                this.Clear();
                this.StatusMessage = "Keine Berechtigung für die Lagekarte.";
                return;
            }

            try
            {
                this.StatusMessage = "Lagekarte wird geladen â€¦";
                IncidentSummaryDto? incident = await service.GetActiveIncidentAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!this.CanView)
                {
                    return;
                }

                if (incident == null)
                {
                    this.Clear();
                    this.StatusMessage = "Kein aktiver Einsatz vorhanden.";
                    return;
                }

                MapStateResponse state = await service.GetStateAsync(incident.Id, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!this.CanView)
                {
                    return;
                }

                MemoryLayer elements = SituationMapLayers.CreateElements(state.Elements);
                MemoryLayer hydrants = SituationMapLayers.CreateHydrants(state.Hydrants);
                Map? currentMap = this.Map;
                if (currentMap != null && this.displayedIncidentId != incident.Id)
                {
                    MPoint startPoint = new MPoint(incident.OriginLongitude, incident.OriginLatitude);
                    (double x, double y) spericalMercatorCoordinate = SphericalMercator.FromLonLat(startPoint.X, startPoint.Y);

                    currentMap.Navigator.CenterOn(spericalMercatorCoordinate.x, spericalMercatorCoordinate.y);
                    currentMap.Navigator.ZoomTo(2);

                    this.displayedIncidentId = incident.Id;
                }

                foreach (ILayer? layer in currentMap.Layers.Where(layer => layer.Name is "Lage" or "Hydranten").ToArray())
                {
                    currentMap.Layers.Remove(layer);
                    layer.Dispose();
                }
                currentMap.Layers.Add(hydrants);
                currentMap.Layers.Add(elements);
                currentMap.Refresh();
                this.IncidentTitle = incident.Name;
                this.LastUpdated = $"Stand: {state.ServerUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}";
                this.StatusMessage = string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                this.Clear();
                this.StatusMessage = exception is HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized }
                    ? "Keine Berechtigung für die Lagekarte. Bitte erneut anmelden."
                    : "Die Lagekarte konnte nicht geladen werden. Bitte erneut aktualisieren.";
            }
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

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.CanView));
            this.RefreshCommand.NotifyCanExecuteChanged();
            if (!this.CanView)
            {
                this.RefreshCommand.Cancel();
                this.Clear();
                this.StatusMessage = "Keine Berechtigung für die Lagekarte.";
            }
        }

        private void Clear()
        {
            Map? previousMap = this.Map;
            this.Map = null;
            previousMap?.Dispose();
            this.displayedIncidentId = null;
            this.IncidentTitle = "Lagekarte";
            this.LastUpdated = string.Empty;
        }
    }
}
