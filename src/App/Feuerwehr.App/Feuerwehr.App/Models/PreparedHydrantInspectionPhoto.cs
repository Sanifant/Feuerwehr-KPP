namespace de.openelp.feuerwehr.App.Models
{
    using de.openelp.feuerwehr.Common.Models.HydrantInspections;

    /// <summary>Prepared JPEG bytes and their display dimensions for an inspection upload.</summary>
    public sealed class PreparedHydrantInspectionPhoto
    {
        public PreparedHydrantInspectionPhoto(byte[] content, int width, int height)
        {
            this.Content = content;
            this.Width = width;
            this.Height = height;
        }

        public byte[] Content { get; }

        public int Width { get; }

        public int Height { get; }

        public string ContentType => HydrantInspectionPhotoLimits.ContentType;
    }
}
