using De.Openelp.Feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Data
{
    /// <summary>
    /// Configures Entity Framework relationships, constraints, and indexes for inventory persistence.
    /// </summary>
    internal static class InventoryModelConfiguration
    {
        /// <summary>
        /// Configures inventory entity relationships, constraints, and barcode allocation.
        /// </summary>
        /// <param name="model">The database context or model builder used by this operation.</param>
        public static void ConfigureInventory(this ModelBuilder model)
        {
            _ = model.HasSequence<int>("InventoryBarcodeSequence").StartsAt(1).HasMax(999);
            _ = model.Entity<InventoryCategory>(entity =>
            {
                _ = entity.ToTable("InventoryCategories");
                _ = entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                _ = entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
                _ = entity.HasIndex(x => x.NormalizedName).IsUnique();
                _ = entity.HasData(
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000001"), Name = "Löschgeräte", NormalizedName = "LÖSCHGERÄTE" },
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000002"), Name = "Armaturen", NormalizedName = "ARMATUREN" },
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000003"), Name = "Beleuchtung", NormalizedName = "BELEUCHTUNG" });
            });
            _ = model.Entity<EquipmentType>(entity =>
            {
                _ = entity.ToTable("EquipmentTypes");
                _ = entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                _ = entity.Property(x => x.InspectionInstructions).HasMaxLength(8000).IsRequired();
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<Equipment>(entity =>
            {
                _ = entity.ToTable("Equipment");
                _ = entity.Property(x => x.BarcodeNumber).HasDefaultValueSql("nextval('\"InventoryBarcodeSequence\"')");
                _ = entity.HasIndex(x => x.BarcodeNumber).IsUnique();
                _ = entity.Property(x => x.SerialNumber).HasMaxLength(150).IsRequired();
                _ = entity.Property(x => x.Manufacturer).HasMaxLength(150).IsRequired();
                _ = entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                _ = entity.Property(x => x.Version).IsConcurrencyToken();
                _ = entity.HasOne(x => x.EquipmentType).WithMany().HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<EquipmentInspection>(entity =>
            {
                _ = entity.ToTable("EquipmentInspections");
                _ = entity.HasIndex(x => x.CommandId).IsUnique();
                _ = entity.HasIndex(x => new { x.EquipmentId, x.RecordedAtUtc });
                _ = entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                _ = entity.Property(x => x.Comment).HasMaxLength(4000);
                _ = entity.Property(x => x.InspectorId).HasMaxLength(450).IsRequired();
                _ = entity.Property(x => x.InspectorName).HasMaxLength(300).IsRequired();
                _ = entity.Property(x => x.EquipmentTypeName).HasMaxLength(150).IsRequired();
                _ = entity.Property(x => x.InspectionInstructions).HasMaxLength(8000).IsRequired();
                _ = entity.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            });
            _ = model.Entity<EquipmentReminderDelivery>(entity =>
            {
                _ = entity.ToTable("EquipmentReminderDeliveries");
                _ = entity.Property(x => x.RecipientId).HasMaxLength(450).IsRequired();
                _ = entity.HasIndex(x => new { x.EquipmentId, x.RecipientId, x.DueOn }).IsUnique();
                _ = entity.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
