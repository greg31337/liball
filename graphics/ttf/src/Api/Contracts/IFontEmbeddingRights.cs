namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontEmbeddingRights"/>.</summary>
public interface IFontEmbeddingRights
{
    /// <summary>Gets the OS/2 table version used for interpretation.</summary>
    /// <returns>The table version.</returns>
    int GetTableVersion();

    /// <summary>Gets the raw fsType flags.</summary>
    /// <returns>The raw flags.</returns>
    int GetRawFlags();

    /// <summary>Gets the mutually interpreted permission-bit subset.</summary>
    /// <returns>The raw permission bits.</returns>
    int GetPermissionFlags();

    /// <summary>Gets the effective permission.</summary>
    /// <returns>The effective permission.</returns>
    FontEmbeddingPermission GetPermission();

    /// <summary>Reports whether restricted permission was declared.</summary>
    /// <returns>True when the restricted bit is set.</returns>
    bool DeclaresRestrictedPermission();

    /// <summary>Reports whether preview-and-print permission was declared.</summary>
    /// <returns>True when the preview-and-print bit is set.</returns>
    bool DeclaresPreviewAndPrintPermission();

    /// <summary>Reports whether editable permission was declared.</summary>
    /// <returns>True when the editable bit is set.</returns>
    bool DeclaresEditablePermission();

    /// <summary>Gets the number of declared permission bits.</summary>
    /// <returns>The declared permission count.</returns>
    int GetDeclaredPermissionCount();

    /// <summary>Reports whether legacy least-restrictive precedence was required.</summary>
    /// <returns>True when multiple legacy permission bits were resolved.</returns>
    bool UsesLegacyLeastRestrictivePrecedence();

    /// <summary>Gets restriction flags ignored because of the OS/2 version.</summary>
    /// <returns>The ignored flag bits.</returns>
    int GetIgnoredFlags();

    /// <summary>Reports whether any font embedding is allowed.</summary>
    /// <returns>True when embedding is permitted.</returns>
    bool AllowsEmbedding();

    /// <summary>Reports whether permanent installation is allowed.</summary>
    /// <returns>True when permanent installation is permitted.</returns>
    bool AllowsPermanentInstallation();

    /// <summary>Reports whether embedded documents may be edited.</summary>
    /// <returns>True when editing is permitted.</returns>
    bool AllowsDocumentEditing();

    /// <summary>Reports whether embedded documents must be read-only.</summary>
    /// <returns>True for preview-and-print permission.</returns>
    bool RequiresReadOnlyDocuments();

    /// <summary>Reports whether the embedded font must be loaded temporarily.</summary>
    /// <returns>True when permanent installation is not granted.</returns>
    bool RequiresTemporaryLoading();

    /// <summary>Reports whether subsetting is allowed.</summary>
    /// <returns>True when a subset may be embedded.</returns>
    bool AllowsSubsetting();

    /// <summary>Reports whether complete-font embedding is required.</summary>
    /// <returns>True when subsetting is prohibited.</returns>
    bool RequiresFullEmbedding();

    /// <summary>Reports whether outline data may be embedded.</summary>
    /// <returns>True when outline embedding is permitted.</returns>
    bool AllowsOutlineEmbedding();

    /// <summary>Reports whether bitmap data may be embedded.</summary>
    /// <returns>True when bitmap embedding is permitted.</returns>
    bool AllowsBitmapEmbedding();

    /// <summary>Reports whether embedding is limited to bitmap data.</summary>
    /// <returns>True when outlines cannot be embedded.</returns>
    bool RequiresBitmapOnlyEmbedding();

    /// <summary>Gets the policy-neutral recommended action.</summary>
    /// <returns>The recommended action.</returns>
    FontEmbeddingAction GetRecommendedAction();

    /// <summary>Gets the number of active restriction diagnostics.</summary>
    /// <returns>The restriction count.</returns>
    int GetRestrictionCount();

    /// <summary>Gets one stable diagnostic code for an active restriction.</summary>
    /// <param name="index">The zero-based restriction index.</param>
    /// <returns>The diagnostic code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the restriction list.</exception>
    FontDiagnosticCode GetRestrictionCode(int index);
}
