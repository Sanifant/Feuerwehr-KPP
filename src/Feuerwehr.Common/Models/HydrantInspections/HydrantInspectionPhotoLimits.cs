namespace de.openelp.feuerwehr.Common.Models.HydrantInspections
{
    /// <summary>Shared requirements for prepared inspection photo uploads.</summary>
    public static class HydrantInspectionPhotoLimits
    {
        public const int MaximumDimension = 2048;

        public const int JpegQuality = 85;

        public const int MaximumBytes = 5 * 1024 * 1024;

        public const string ContentType = "image/jpeg";
    }
}
