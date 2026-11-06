using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleService
    {
        Task<ServiceResult> Types(bool includeInactive, CancellationToken ct);
        Task<ServiceResult> Statuses(bool includeInactive, CancellationToken ct);
        Task<ServiceResult> SaveStatus(Guid? id, SaveVehicleStatusRequest request, CancellationToken ct);
        Task<ServiceResult> SaveType(Guid? id, SaveVehicleTypeRequest request, CancellationToken ct);
        Task<ServiceResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct);
        Task<ServiceResult> List(bool includeInactive, CancellationToken ct);
        Task<ServiceResult> Availability(DateOnly from, DateOnly to, CancellationToken ct, string? vehicleType = null);
        Task<ServiceResult> ManagementOptions(CancellationToken ct);
        Task<ServiceResult> Save(Guid? id, SaveVehicleRequest request, CancellationToken ct);
        Task<ServiceResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct);
        Task<ServiceResult> Reservations(string scope, CancellationToken ct);
        Task<ServiceResult> Reserve(Guid id, CreateVehicleReservationRequest request, bool direct, CancellationToken ct);
        Task<ServiceResult> Decide(Guid id, DecideVehicleReservationRequest request, VehicleReservationStatus status, CancellationToken ct);
    }
}
