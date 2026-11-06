using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Data
{
    public class FeuerwehrDbContext : IdentityDbContext<ApplicationUser>
    {
        public FeuerwehrDbContext(DbContextOptions<FeuerwehrDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hydrant> Hydrants { get; set; }

        public DbSet<FireDepartment> FireDepartments { get; set; }

        public DbSet<TrainingCourse> TrainingCourses { get; set; }

        public DbSet<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; }

        public DbSet<Incident> Incidents { get; set; }

        public DbSet<MapElement> MapElements { get; set; }

        public DbSet<MapSymbol> MapSymbols { get; set; }

        public DbSet<MapSymbolCategory> MapSymbolCategories { get; set; }

        public DbSet<MapAuditEvent> MapAuditEvents { get; set; }

        public DbSet<EditorLease> EditorLeases { get; set; }

        public DbSet<DiaryCategory> DiaryCategories { get; set; }

        public DbSet<DiaryEntry> DiaryEntries { get; set; }

        public DbSet<DiaryEntryRevision> DiaryEntryRevisions { get; set; }

        public DbSet<ProcessedCommand> ProcessedCommands { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        public DbSet<InventoryCategory> InventoryCategories { get; set; }

        public DbSet<EquipmentType> EquipmentTypes { get; set; }

        public DbSet<Equipment> Equipment { get; set; }

        public DbSet<EquipmentInspection> EquipmentInspections { get; set; }

        public DbSet<EquipmentReminderDelivery> EquipmentReminderDeliveries { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<VehicleReservation> VehicleReservations { get; set; }

        public DbSet<VehicleBookingDay> VehicleBookingDays { get; set; }

        public DbSet<VehicleEvent> VehicleEvents { get; set; }

        public DbSet<VehicleMailMessage> VehicleMailMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureInventory();
            modelBuilder.ConfigureVehicles();

            _ = modelBuilder.HasSequence<int>("Hydrants_Id_seq")
                .StartsAt(1)
                .IncrementsBy(1);

            _ = modelBuilder.Entity<Hydrant>(entity =>
            {
                _ = entity.ToTable("Hydrants");
                _ = entity.HasKey(e => e.Id);
                _ = entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"Hydrants_Id_seq\"')");

                _ = entity.OwnsOne(e => e.Address, address =>
                {
                    _ = address.Property(a => a.Street).HasColumnName("Street").IsRequired();
                    _ = address.Property(a => a.HouseNumber).HasColumnName("HouseNumber").IsRequired();
                    _ = address.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired();
                    _ = address.Property(a => a.City).HasColumnName("City").IsRequired();
                    _ = address.Property(a => a.AdditionalInfo).HasColumnName("AdditionalInfo");
                });

                // Spatial Data Mapping
                _ = entity.Property(e => e.Longitude)
                    .IsRequired();
                _ = entity.Property(e => e.Latitude)
                    .IsRequired();

                // Structural Properties
                _ = entity.Property(e => e.NominalDiameter)
                    .IsRequired()
                    .HasColumnName("DN_Diameter");

                // Map Enums as Strings for readability in SQL
                _ = entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                _ = entity.Property(e => e.WaterSource)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                _ = entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                _ = entity.Property(e => e.Notes)
                    .HasMaxLength(1000);
            });
            _ = modelBuilder.Entity<FireDepartment>(entity =>
                {
                    _ = entity.ToTable("FireDepartment");
                    _ = entity.HasKey(e => e.Id);
                    _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
                }
            );

            _ = modelBuilder.Entity<TrainingCourse>(entity =>
                {
                    _ = entity.ToTable("TrainingCourse");
                    _ = entity.HasKey(e => e.Id);
                    _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
                }
            );

            _ = modelBuilder.Entity<FireDepartmentTrainingCourse>(entity =>
            {
                _ = entity.ToTable("FireDepartmentTrainingCourses");
                _ = entity.HasKey(e => new { e.FireDepartmentId, e.TrainingCourseId });

                _ = entity.Property(e => e.SeatsAssigned)
                    .IsRequired();

                _ = entity.HasOne(e => e.FireDepartment)
                    .WithMany(fd => fd.FireDepartmentTrainingCourses)
                    .HasForeignKey(e => e.FireDepartmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                _ = entity.HasOne(e => e.TrainingCourse)
                    .WithMany(tc => tc.FireDepartmentTrainingCourses)
                    .HasForeignKey(e => e.TrainingCourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApplicationUser relationship with FireDepartment
            _ = modelBuilder.Entity<ApplicationUser>(entity =>
            {
                _ = entity.HasOne(u => u.FireDepartment)
                    .WithMany()
                    .HasForeignKey(u => u.FireDepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            _ = modelBuilder.HasSequence<long>("MapAuditSequence").StartsAt(1).IncrementsBy(1);
            _ = modelBuilder.HasSequence<long>("DiaryEntryNumberSeq").StartsAt(1).IncrementsBy(1);

            _ = modelBuilder.Entity<Incident>(entity =>
            {
                _ = entity.ToTable("Incidents");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                _ = entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                _ = entity.HasIndex(x => x.Status).HasDatabaseName("IX_Incidents_Status");
                _ = entity.HasIndex(x => x.Status).IsUnique().HasFilter("\"Status\" = 'Active'").HasDatabaseName("UX_Incidents_OnlyOneActive");
            });

            _ = modelBuilder.Entity<MapElement>(entity =>
            {
                _ = entity.ToTable("MapElements");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.ElementType).HasConversion<string>().HasMaxLength(20);
                _ = entity.Property(x => x.GeometryJson).IsRequired();
                _ = entity.HasIndex(x => new { x.IncidentId, x.UpdatedAtUtc });
                _ = entity.HasOne(x => x.Incident)
                    .WithMany()
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            _ = modelBuilder.Entity<MapSymbol>(entity =>
            {
                _ = entity.ToTable("MapSymbols");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => new { x.Category, x.IsActive });
            });

            _ = modelBuilder.Entity<MapSymbolCategory>(entity =>
            {
                _ = entity.ToTable("MapSymbolCategories");
                _ = entity.HasKey(x => x.Name);
                _ = entity.HasData(Common.Models.IncidentModules.MapSymbolCategories.All.Select(name => new MapSymbolCategory { Name = name }));
            });

            _ = modelBuilder.Entity<MapAuditEvent>(entity =>
            {
                _ = entity.ToTable("MapAuditEvents");
                _ = entity.HasKey(x => x.Id);
                _ = entity.Property(x => x.SequenceNumber).HasDefaultValueSql("nextval('\"MapAuditSequence\"')");
                _ = entity.HasIndex(x => new { x.IncidentId, x.SequenceNumber }).IsUnique();
            });

            _ = modelBuilder.Entity<EditorLease>(entity =>
            {
                _ = entity.ToTable("EditorLeases");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => x.IncidentId).IsUnique();
                _ = entity.HasOne(x => x.Incident)
                    .WithMany()
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            _ = modelBuilder.Entity<DiaryCategory>(entity =>
            {
                _ = entity.ToTable("DiaryCategories");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => x.Code).IsUnique();
                _ = entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
                _ = entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            });

            _ = modelBuilder.Entity<DiaryEntry>(entity =>
            {
                _ = entity.ToTable("DiaryEntries");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => new { x.IncidentId, x.EntryNumber }).IsUnique();
                _ = entity.Property(x => x.EntryNumber).HasDefaultValueSql("nextval('\"DiaryEntryNumberSeq\"')");
            });

            _ = modelBuilder.Entity<DiaryEntryRevision>(entity =>
            {
                _ = entity.ToTable("DiaryEntryRevisions");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => new { x.DiaryEntryId, x.RevisionNumber }).IsUnique();
                _ = entity.Property(x => x.RevisionType).HasConversion<string>().HasMaxLength(20);
                _ = entity.HasOne(x => x.DiaryEntry)
                    .WithMany()
                    .HasForeignKey(x => x.DiaryEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            _ = modelBuilder.Entity<ProcessedCommand>(entity =>
            {
                _ = entity.ToTable("ProcessedCommands");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => x.CommandId).IsUnique();
                _ = entity.HasIndex(x => new { x.UserId, x.Scope, x.TargetId });
            });

            _ = modelBuilder.Entity<OutboxMessage>(entity =>
            {
                _ = entity.ToTable("OutboxMessages");
                _ = entity.HasKey(x => x.Id);
                _ = entity.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
            });
        }
    }
}
