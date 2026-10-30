using de.openelp.feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Server.Data;

public static class VehicleModelConfiguration
{
    public static void ConfigureVehicles(this ModelBuilder model)
    {
        model.Entity<VehicleStatus>(entity =>
        {
            entity.ToTable("VehicleStatuses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.NormalizedName).IsUnique();
            entity.HasData(
                new VehicleStatus { Id = VehicleStatusDefaults.ReadyId, Name = "Betriebsbereit", NormalizedName = "BETRIEBSBEREIT", AllowsBooking = true },
                new VehicleStatus { Id = VehicleStatusDefaults.MaintenanceId, Name = "Wartung", NormalizedName = "WARTUNG", AllowsBooking = false },
                new VehicleStatus { Id = VehicleStatusDefaults.DeregisteredId, Name = "Abgemeldet", NormalizedName = "ABGEMELDET", AllowsBooking = false });
        });
        model.Entity<VehicleType>(entity =>
        {
            entity.ToTable("VehicleTypes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.NormalizedName).IsUnique();
        });
        model.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RadioCallSign).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.LicensePlate).HasMaxLength(30);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.FireDepartmentId, x.IsActive });
            entity.HasOne(x => x.FireDepartment).WithMany().HasForeignKey(x => x.FireDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EquipmentOfficer).WithMany().HasForeignKey(x => x.EquipmentOfficerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.VehicleStatusId).HasDefaultValue(VehicleStatusDefaults.ReadyId);
            entity.HasOne(x => x.VehicleStatus).WithMany().HasForeignKey(x => x.VehicleStatusId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<VehicleReservation>(entity =>
        {
            entity.ToTable("VehicleReservations", table => table.HasCheckConstraint("CK_VehicleReservation_Dates", "\"From\" <= \"To\""));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PayloadHash).HasMaxLength(64);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.CommandId).IsUnique();
            entity.HasIndex(x => new { x.VehicleId, x.Status, x.From, x.To });
            entity.HasIndex(x => x.RequesterId);
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<VehicleBookingDay>(entity =>
        {
            entity.ToTable("VehicleBookingDays");
            entity.HasKey(x => new { x.VehicleId, x.Date });
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<VehicleEvent>(entity =>
        {
            entity.ToTable("VehicleEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(50);
            entity.HasIndex(x => new { x.VehicleId, x.OccurredAtUtc });
        });
        model.Entity<VehicleMailMessage>(entity =>
        {
            entity.ToTable("VehicleMailMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasMaxLength(30);
            entity.HasIndex(x => new { x.ReservationId, x.Kind }).IsUnique();
            entity.HasIndex(x => new { x.SentAtUtc, x.NextAttemptAtUtc });
        });
    }
}
