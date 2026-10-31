using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleTransferService
    {
        Task<ServiceResult> Export(VehicleTransferKind kind, CancellationToken ct);
        Task<ServiceResult> Import(VehicleTransferKind kind, Stream stream, CancellationToken ct);
    }
}
