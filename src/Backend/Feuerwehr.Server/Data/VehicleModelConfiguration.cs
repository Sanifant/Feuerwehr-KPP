using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Data
{
    public static class VehicleModelConfiguration
    {
        /// <summary>
        /// Executes configure Vehicles.
        /// </summary>
        /// <param name="model">The model.</param>
        public static void ConfigureVehicles(this ModelBuilder model)
        {
            _ = model.Entity<VehicleStatus>(entity =>
            {
                _ = entity.ToTable("VehicleStatuses");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                _ = entity.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasIndex(x => x.NormalizedName).IsUnique();
                _ = entity.HasData(
                    new VehicleStatus { Id = VehicleStatusDefaults.ReadyId, Name = "Betriebsbereit", NormalizedName = "BETRIEBSBEREIT", AllowsBooking = true },
                    new VehicleStatus { Id = VehicleStatusDefaults.MaintenanceId, Name = "Wartung", NormalizedName = "WARTUNG", AllowsBooking = false },
                    new VehicleStatus { Id = VehicleStatusDefaults.DeregisteredId, Name = "Abgemeldet", NormalizedName = "ABGEMELDET", AllowsBooking = false });
            });
            _ = model.Entity<VehicleType>(entity =>
            {
                _ = entity.ToTable("VehicleTypes");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                _ = entity.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasIndex(x => x.NormalizedName).IsUnique();
            });
            _ = model.Entity<Vehicle>(entity =>
            {
                _ = entity.ToTable("Vehicles");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.RadioCallSign).HasMaxLength(100).IsRequired();
                _ = entity.Property(x => x.Name).HasMaxLength(150);
                _ = entity.Property(x => x.LicensePlate).HasMaxLength(30);
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasIndex(x => new { x.FireDepartmentId, x.IsActive });
                _ = entity.HasOne(x => x.FireDepartment).WithMany().HasForeignKey(x => x.FireDepartmentId).OnDelete(DeleteBehavior.Restrict);
                _ = entity.HasOne(x => x.EquipmentOfficer).WithMany().HasForeignKey(x => x.EquipmentOfficerId).OnDelete(DeleteBehavior.Restrict);
                _ = entity.HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
                _ = entity.Property(x => x.VehicleStatusId).HasDefaultValue(VehicleStatusDefaults.ReadyId);
                _ = entity.HasOne(x => x.VehicleStatus).WithMany().HasForeignKey(x => x.VehicleStatusId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<VehicleReservation>(entity =>
            {
                _ = entity.ToTable("VehicleReservations", table => table.HasCheckConstraint("CK_VehicleReservation_Dates", "\"From\" <= \"To\""));
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Comment).HasMaxLength(2000);
                _ = entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                _ = entity.Property(x => x.PayloadHash).HasMaxLength(64);
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasIndex(x => x.CommandId).IsUnique();
                _ = entity.HasIndex(x => new { x.VehicleId, x.Status, x.From, x.To });
                _ = entity.HasIndex(x => x.RequesterId);
                _ = entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<VehicleBookingDay>(entity =>
            {
                _ = entity.ToTable("VehicleBookingDays");
                _ = entity.HasKey(x => new { x.VehicleId, x.Date });
                _ = entity.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
                _ = entity.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<VehicleEvent>(entity =>
            {
                _ = entity.ToTable("VehicleEvents");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Action).HasMaxLength(50);
                _ = entity.HasIndex(x => new { x.VehicleId, x.OccurredAtUtc });
            });
            _ = model.Entity<VehicleMailMessage>(entity =>
            {
                _ = entity.ToTable("VehicleMailMessages");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Kind).HasMaxLength(30);
                _ = entity.HasIndex(x => new { x.ReservationId, x.Kind }).IsUnique();
                _ = entity.HasIndex(x => new { x.SentAtUtc, x.NextAttemptAtUtc });
            });
        }
    }
}
