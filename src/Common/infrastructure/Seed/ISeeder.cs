namespace de.openelp.feuerwehr.infrastructure.Seed
{
    public interface ISeeder
    {
        Task SeedAsync(AppDbContext context);
    }
}