namespace de.openelp.feuerwehr.Server.Services.HydrantModules
{
    /// <summary>Configures the persistent directory used for inspection photos.</summary>
    public sealed class HydrantInspectionPhotoStorageOptions
    {
        public const string SectionName = "HydrantInspectionPhotos";

        /// <summary>Gets or sets the photo directory; relative paths are relative to the API content root.</summary>
        public string RootPath { get; set; } = "App_Data/hydrant-inspection-photos";
    }
}
