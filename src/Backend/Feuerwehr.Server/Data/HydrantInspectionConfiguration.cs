namespace de.openelp.feuerwehr.Server.Data
{
    using de.openelp.feuerwehr.Common.Models;
    using de.openelp.feuerwehr.Server.Models.Hydrant;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    /// <summary>Maps inspection history and optional JPEG file metadata.</summary>
    public sealed class HydrantInspectionConfiguration : IEntityTypeConfiguration<HydrantInspection>
    {
        public void Configure(EntityTypeBuilder<HydrantInspection> builder)
        {
            string statuses = string.Join(", ", Enum.GetValues<HydrantStatus>().Select(status => (int)status));
            builder.ToTable("HydrantInspections", table =>
            {
                table.HasCheckConstraint("CK_HydrantInspections_PreviousStatus", $"\"PreviousStatus\" IN ({statuses})");
                table.HasCheckConstraint("CK_HydrantInspections_NewStatus", $"\"NewStatus\" IN ({statuses})");
                table.HasCheckConstraint("CK_HydrantInspections_CommentLength", "\"Comment\" IS NULL OR length(\"Comment\") <= 500");
                table.HasCheckConstraint("CK_HydrantInspections_Photo", "(\"PhotoStorageKey\" IS NULL AND \"PhotoContentType\" IS NULL) OR (\"PhotoStorageKey\" IS NOT NULL AND length(\"PhotoStorageKey\") > 0 AND \"PhotoContentType\" IS NOT NULL AND \"PhotoContentType\" = 'image/jpeg')");
            });
            builder.HasKey(inspection => inspection.Id);
            builder.HasOne(inspection => inspection.Hydrant).WithMany()
                .HasForeignKey(inspection => inspection.HydrantId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(inspection => inspection.HydrantId);
            builder.Property(inspection => inspection.PreviousStatus).IsRequired();
            builder.Property(inspection => inspection.NewStatus).IsRequired();
            builder.Property(inspection => inspection.Comment).HasMaxLength(HydrantInspection.MaximumCommentLength);
            builder.Property(inspection => inspection.PhotoStorageKey).HasMaxLength(500);
            builder.Property(inspection => inspection.PhotoContentType).HasMaxLength(100);
        }
    }
}
