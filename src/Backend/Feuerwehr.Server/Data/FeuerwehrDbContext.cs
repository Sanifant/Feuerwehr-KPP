using Feuerwehr.Common.Models;
using Feuerwehr.Server.Models;
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
        
        public DbSet<FireDepartment>  FireDepartments { get; set; }
        
        public DbSet<TrainingCourse> TrainingCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<int>("Hydrants_Id_seq")
                .StartsAt(1)
                .IncrementsBy(1);
            
            modelBuilder.Entity<Hydrant>(entity =>
            {
                entity.ToTable("Hydrants");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"Hydrants_Id_seq\"')");

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
/*
            modelBuilder.Entity<FireDepartment>(entity =>
                {
                    entity.ToTable("FireDepartment");
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                }
            );
            
            modelBuilder.Entity<TrainingCourse>(entity =>
                {
                    entity.ToTable("TrainingCourse");
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();

                    entity.HasOne(tc => tc.OrganizingFireDepartment)
                        .WithMany(fd => fd.TrainingCourses)
                        .HasForeignKey(tc => tc.OrganizingFireDepartmentId)
                        .OnDelete(DeleteBehavior.SetNull);
                }
            );*/
        }
    }
}
