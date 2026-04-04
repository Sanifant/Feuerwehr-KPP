using de.openelp.feuerwehr.domain;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.infrastructure
{
    public class AppDbContext: DbContext
    {
        public DbSet<InventoryItem> InventoryItems { get; set; }

        public DbSet<InventoryItemRelationship> InventoryItemRelationships { get; set; }

        public DbSet<ItemRelationshipLabel> ItemRelationshipLabels { get; set; }

        public DbSet<InventoryCategory> InventoryCategories { get; set; }

        public DbSet<Hydrant> Hydrants { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryItemRelationship>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.ParentItem)
                    .WithMany(i => i.ChildRelationships)
                    .HasForeignKey(r => r.ParentItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.ParentItemId, r.ChildItemId, r.RelationshipLabelId })
                    .IsUnique();
            });
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasOne(i => i.Category)
                    .WithMany(c => c.Items)
                    .HasForeignKey(i => i.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict); // Kategorie kann nicht gelöscht werden solange Items existieren
            });

            modelBuilder.Entity<InventoryCategory>(entity =>
            {
                entity.HasKey(c => c.Id);

                // Name muss eindeutig sein
                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });
        }
    }
}
