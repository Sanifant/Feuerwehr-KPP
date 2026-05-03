// RelationshipLabelSeeder.cs
using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.infrastructure.Seed
{
    public class RelationshipLabelSeeder : ISeeder
    {
        public async Task SeedAsync(AppDbContext context)
        {
            if (context.ItemRelationshipLabels.Any()) return;

            var labels = new List<ItemRelationshipLabel>
            {
                new() { Id = Guid.NewGuid(), Name = "Zubehör",            Description = "Optionales Zubehör" },
                new() { Id = Guid.NewGuid(), Name = "Ersatzteil",         Description = "Ersatzteile für das Gerät" },
                new() { Id = Guid.NewGuid(), Name = "Bestandteil",        Description = "Festes Bestandteil des Geräts" },
                new() { Id = Guid.NewGuid(), Name = "Verbrauchsmaterial", Description = "Verbrauchsmaterial" }
            };

            await context.ItemRelationshipLabels.AddRangeAsync(labels);
            await context.SaveChangesAsync();
        }
    }
}