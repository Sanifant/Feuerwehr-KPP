namespace de.openelp.feuerwehr.Tests.Backend.Services.HydrantModules
{
    using de.openelp.feuerwehr.Server.Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    internal sealed class HydrantInspectionTestContext(DbContextOptions<FeuerwehrDbContext> options) : FeuerwehrDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (IMutableSequence sequence in modelBuilder.Model.GetSequences().ToList())
            {
                modelBuilder.Model.RemoveSequence(sequence.Name, sequence.Schema);
            }
        }
    }
}
