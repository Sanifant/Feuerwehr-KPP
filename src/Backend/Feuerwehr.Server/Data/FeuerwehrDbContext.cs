using Feuerwehr.Common.Models;
using Feuerwehr.Server.Models.IncidentModules;
using Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Data
{
    public class FeuerwehrDbContext : IdentityDbContext<ApplicationUser>
    {
        public FeuerwehrDbContext(DbContextOptions<FeuerwehrDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hydrant> Hydrants { get; set; }
        
        public DbSet<FireDepartment>  FireDepartments { get; set; }
        
        public DbSet<TrainingCourse> TrainingCourses { get; set; }

        public DbSet<FireDepartmentTrainingCourse> FireDepartmentTrainingCourses { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<MapElement> MapElements { get; set; }
        public DbSet<MapAuditEvent> MapAuditEvents { get; set; }
        public DbSet<EditorLease> EditorLeases { get; set; }
        public DbSet<DiaryCategory> DiaryCategories { get; set; }
        public DbSet<DiaryEntry> DiaryEntries { get; set; }
        public DbSet<DiaryEntryRevision> DiaryEntryRevisions { get; set; }
        public DbSet<ProcessedCommand> ProcessedCommands { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

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
                }
            );

            modelBuilder.Entity<FireDepartmentTrainingCourse>(entity =>
            {
                entity.ToTable("FireDepartmentTrainingCourses");
                entity.HasKey(e => new { e.FireDepartmentId, e.TrainingCourseId });

                entity.Property(e => e.SeatsAssigned)
                    .IsRequired();

                entity.HasOne(e => e.FireDepartment)
                    .WithMany(fd => fd.FireDepartmentTrainingCourses)
                    .HasForeignKey(e => e.FireDepartmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.TrainingCourse)
                    .WithMany(tc => tc.FireDepartmentTrainingCourses)
                    .HasForeignKey(e => e.TrainingCourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApplicationUser relationship with FireDepartment
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.FireDepartment)
                    .WithMany()
                    .HasForeignKey(u => u.FireDepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.HasSequence<long>("MapAuditSequence").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<long>("DiaryEntryNumberSeq").StartsAt(1).IncrementsBy(1);

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.ToTable("Incidents");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.HasIndex(x => x.Status).HasDatabaseName("IX_Incidents_Status");
                entity.HasIndex(x => x.Status).IsUnique().HasFilter("\"Status\" = 'Active'").HasDatabaseName("UX_Incidents_OnlyOneActive");
            });

            modelBuilder.Entity<MapElement>(entity =>
            {
                entity.ToTable("MapElements");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ElementType).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.GeometryJson).IsRequired();
                entity.HasIndex(x => new { x.IncidentId, x.UpdatedAtUtc });
                entity.HasOne(x => x.Incident)
                    .WithMany()
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MapAuditEvent>(entity =>
            {
                entity.ToTable("MapAuditEvents");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SequenceNumber).HasDefaultValueSql("nextval('\"MapAuditSequence\"')");
                entity.HasIndex(x => new { x.IncidentId, x.SequenceNumber }).IsUnique();
            });

            modelBuilder.Entity<EditorLease>(entity =>
            {
                entity.ToTable("EditorLeases");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.IncidentId).IsUnique();
                entity.HasOne(x => x.Incident)
                    .WithMany()
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DiaryCategory>(entity =>
            {
                entity.ToTable("DiaryCategories");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            });

            modelBuilder.Entity<DiaryEntry>(entity =>
            {
                entity.ToTable("DiaryEntries");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.IncidentId, x.EntryNumber }).IsUnique();
                entity.Property(x => x.EntryNumber).HasDefaultValueSql("nextval('\"DiaryEntryNumberSeq\"')");
            });

            modelBuilder.Entity<DiaryEntryRevision>(entity =>
            {
                entity.ToTable("DiaryEntryRevisions");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DiaryEntryId, x.RevisionNumber }).IsUnique();
                entity.Property(x => x.RevisionType).HasConversion<string>().HasMaxLength(20);
                entity.HasOne(x => x.DiaryEntry)
                    .WithMany()
                    .HasForeignKey(x => x.DiaryEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProcessedCommand>(entity =>
            {
                entity.ToTable("ProcessedCommands");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.CommandId).IsUnique();
                entity.HasIndex(x => new { x.UserId, x.Scope, x.TargetId });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
            });
        }
    }
}
