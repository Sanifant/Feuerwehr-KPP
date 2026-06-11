using Feuerwehr.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Data
{
    public class FeuerwehrDbContext : DbContext
    {
        public FeuerwehrDbContext(DbContextOptions<FeuerwehrDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hydrant> Hydrants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Hydrant>(entity =>
            {
                entity.ToTable("Hydrants");
                entity.HasKey(e => e.Id);

                entity.OwnsOne(e => e.Address, address =>
                {
                    address.Property(a => a.Street).HasColumnName("Street").IsRequired();
                    address.Property(a => a.HouseNumber).HasColumnName("HouseNumber").IsRequired();
                    address.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired();
                    address.Property(a => a.City).HasColumnName("City").IsRequired();
                    address.Property(a => a.AdditionalInfo).HasColumnName("AdditionalInfo");
                });

                // Spatial Data Mapping
                entity.Property(e => e.Longitude)
                    .IsRequired();
                entity.Property(e => e.Latitude)
                    .IsRequired();

                // Structural Properties
                entity.Property(e => e.NominalDiameter)
                    .IsRequired()
                    .HasColumnName("DN_Diameter");

                // Map Enums as Strings for readability in SQL
                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.WaterSource)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

            });
        }
    }
}
