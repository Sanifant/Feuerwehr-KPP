namespace de.openelp.feuerwehr.Server.Models.Vehicles
{
    public sealed class VehicleType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int Version { get; set; } = 1;
    }
}
