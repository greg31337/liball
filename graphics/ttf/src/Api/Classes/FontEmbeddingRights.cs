namespace ttf;

/// <summary>Reports interpreted OS/2 embedding rights without overriding them.</summary>
public class FontEmbeddingRights : IFontEmbeddingRights
{
    SfntEmbeddingRightsReport report;

    internal FontEmbeddingRights(SfntEmbeddingRightsReport report)
    {
        this.report = report;
    }

    /// <summary>Gets the OS/2 table version used for interpretation.</summary>
    /// <returns>The table version.</returns>
    public int GetTableVersion() { return report.GetTableVersion(); }

    /// <summary>Gets the raw fsType flags.</summary>
    /// <returns>The raw flags.</returns>
    public int GetRawFlags() { return report.GetRawFlags(); }

    /// <summary>Gets the mutually interpreted permission-bit subset.</summary>
    /// <returns>The raw permission bits.</returns>
    public int GetPermissionFlags() { return report.GetPermissionFlags(); }

    /// <summary>Gets the effective permission.</summary>
    /// <returns>The effective permission.</returns>
    public FontEmbeddingPermission GetPermission()
    {
        SfntEmbeddingPermission permission = report.GetPermission();
        if (permission == SfntEmbeddingPermission.Restricted) { return FontEmbeddingPermission.Restricted; }
        if (permission == SfntEmbeddingPermission.PreviewAndPrint) { return FontEmbeddingPermission.PreviewAndPrint; }
        if (permission == SfntEmbeddingPermission.Editable) { return FontEmbeddingPermission.Editable; }
        return FontEmbeddingPermission.Installable;
    }

    /// <summary>Reports whether restricted permission was declared.</summary>
    /// <returns>True when the restricted bit is set.</returns>
    public bool DeclaresRestrictedPermission() { return report.DeclaresRestrictedPermission(); }

    /// <summary>Reports whether preview-and-print permission was declared.</summary>
    /// <returns>True when the preview-and-print bit is set.</returns>
    public bool DeclaresPreviewAndPrintPermission() { return report.DeclaresPreviewAndPrintPermission(); }

    /// <summary>Reports whether editable permission was declared.</summary>
    /// <returns>True when the editable bit is set.</returns>
    public bool DeclaresEditablePermission() { return report.DeclaresEditablePermission(); }

    /// <summary>Gets the number of declared permission bits.</summary>
    /// <returns>The declared permission count.</returns>
    public int GetDeclaredPermissionCount() { return report.GetDeclaredPermissionCount(); }

    /// <summary>Reports whether legacy least-restrictive precedence was required.</summary>
    /// <returns>True when multiple legacy permission bits were resolved.</returns>
    public bool UsesLegacyLeastRestrictivePrecedence()
    {
        return report.UsesLegacyLeastRestrictivePrecedence();
    }

    /// <summary>Gets restriction flags ignored because of the OS/2 version.</summary>
    /// <returns>The ignored flag bits.</returns>
    public int GetIgnoredFlags() { return report.GetIgnoredFlags(); }

    /// <summary>Reports whether any font embedding is allowed.</summary>
    /// <returns>True when embedding is permitted.</returns>
    public bool AllowsEmbedding() { return report.AllowsEmbedding(); }

    /// <summary>Reports whether permanent installation is allowed.</summary>
    /// <returns>True when permanent installation is permitted.</returns>
    public bool AllowsPermanentInstallation() { return report.AllowsPermanentInstallation(); }

    /// <summary>Reports whether embedded documents may be edited.</summary>
    /// <returns>True when editing is permitted.</returns>
    public bool AllowsDocumentEditing() { return report.AllowsDocumentEditing(); }

    /// <summary>Reports whether embedded documents must be read-only.</summary>
    /// <returns>True for preview-and-print permission.</returns>
    public bool RequiresReadOnlyDocuments() { return report.RequiresReadOnlyDocuments(); }

    /// <summary>Reports whether the embedded font must be loaded temporarily.</summary>
    /// <returns>True when permanent installation is not granted.</returns>
    public bool RequiresTemporaryLoading() { return report.RequiresTemporaryLoading(); }

    /// <summary>Reports whether subsetting is allowed.</summary>
    /// <returns>True when a subset may be embedded.</returns>
    public bool AllowsSubsetting() { return report.AllowsSubsetting(); }

    /// <summary>Reports whether complete-font embedding is required.</summary>
    /// <returns>True when subsetting is prohibited.</returns>
    public bool RequiresFullEmbedding() { return report.RequiresFullEmbedding(); }

    /// <summary>Reports whether outline data may be embedded.</summary>
    /// <returns>True when outline embedding is permitted.</returns>
    public bool AllowsOutlineEmbedding() { return report.AllowsOutlineEmbedding(); }

    /// <summary>Reports whether bitmap data may be embedded.</summary>
    /// <returns>True when bitmap embedding is permitted.</returns>
    public bool AllowsBitmapEmbedding() { return report.AllowsBitmapEmbedding(); }

    /// <summary>Reports whether embedding is limited to bitmap data.</summary>
    /// <returns>True when outlines cannot be embedded.</returns>
    public bool RequiresBitmapOnlyEmbedding() { return report.RequiresBitmapOnlyEmbedding(); }

    /// <summary>Gets the policy-neutral recommended action.</summary>
    /// <returns>The recommended action.</returns>
    public FontEmbeddingAction GetRecommendedAction()
    {
        SfntEmbeddingRecommendedAction action = report.GetRecommendedAction();
        if (action == SfntEmbeddingRecommendedAction.EmbedFullFont) { return FontEmbeddingAction.EmbedFullFont; }
        if (action == SfntEmbeddingRecommendedAction.EmbedBitmapDataOnly) { return FontEmbeddingAction.EmbedBitmapDataOnly; }
        if (action == SfntEmbeddingRecommendedAction.EmbedCompleteBitmapDataOnly) { return FontEmbeddingAction.EmbedCompleteBitmapDataOnly; }
        if (action == SfntEmbeddingRecommendedAction.DoNotEmbedWithoutExplicitPermission)
        {
            return FontEmbeddingAction.DoNotEmbedWithoutExplicitPermission;
        }
        return FontEmbeddingAction.EmbedSubsetOrFullFont;
    }

    /// <summary>Gets the number of active restriction diagnostics.</summary>
    /// <returns>The restriction count.</returns>
    public int GetRestrictionCount() { return report.GetRestrictionDiagnosticCount(); }

    /// <summary>Gets one stable diagnostic code for an active restriction.</summary>
    /// <param name="index">The zero-based restriction index.</param>
    /// <returns>The diagnostic code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the restriction list.</exception>
    public FontDiagnosticCode GetRestrictionCode(int index)
    {
        return report.GetRestrictionDiagnosticCode(index);
    }
}
