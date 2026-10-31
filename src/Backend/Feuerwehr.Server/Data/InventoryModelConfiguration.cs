using De.Openelp.Feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Data
{
    internal static class InventoryModelConfiguration
    {
        /// <summary>
        /// Executes configure Inventory.
        /// </summary>
        /// <param name="model">The model.</param>
        public static void ConfigureInventory(this ModelBuilder model)
        {
            model.HasSequence<int>("InventoryBarcodeSequence").StartsAt(1).HasMax(999);
            model.Entity<InventoryCategory>(entity =>
            {
                entity.ToTable("InventoryCategories");
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
                entity.HasIndex(x => x.NormalizedName).IsUnique();
                entity.HasData(
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000001"), Name = "Löschgeräte", NormalizedName = "LÖSCHGERÄTE" },
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000002"), Name = "Armaturen", NormalizedName = "ARMATUREN" },
                    new InventoryCategory { Id = Guid.Parse("f3050002-0000-0000-0000-000000000003"), Name = "Beleuchtung", NormalizedName = "BELEUCHTUNG" });
            });
            model.Entity<EquipmentType>(entity =>
            {
                entity.ToTable("EquipmentTypes");
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.InspectionInstructions).HasMaxLength(8000).IsRequired();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });
            model.Entity<Equipment>(entity =>
            {
                entity.ToTable("Equipment");
                entity.Property(x => x.BarcodeNumber).HasDefaultValueSql("nextval('\"InventoryBarcodeSequence\"')");
                entity.HasIndex(x => x.BarcodeNumber).IsUnique();
                entity.Property(x => x.SerialNumber).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Manufacturer).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.EquipmentType).WithMany().HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.Restrict);
            });
            model.Entity<EquipmentInspection>(entity =>
            {
                entity.ToTable("EquipmentInspections");
                entity.HasIndex(x => x.CommandId).IsUnique();
                entity.HasIndex(x => new { x.EquipmentId, x.RecordedAtUtc });
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Comment).HasMaxLength(4000);
                entity.Property(x => x.InspectorId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.InspectorName).HasMaxLength(300).IsRequired();
                entity.Property(x => x.EquipmentTypeName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.InspectionInstructions).HasMaxLength(8000).IsRequired();
                entity.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            });
            model.Entity<EquipmentReminderDelivery>(entity =>
            {
                entity.ToTable("EquipmentReminderDeliveries");
                entity.Property(x => x.RecipientId).HasMaxLength(450).IsRequired();
                entity.HasIndex(x => new { x.EquipmentId, x.RecipientId, x.DueOn }).IsUnique();
                entity.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }

}
