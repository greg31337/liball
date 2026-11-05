namespace ttf;

/// <summary>Defines PDF-neutral font resource selection and compatibility options.</summary>
/// <remarks>
/// The compatible profile selects an automatic Type 0 resource targeting PDF
/// 1.7, horizontal writing, subset embedding, and full-font fallback when
/// embedding rights prohibit only subsetting. Archival and accessibility
/// profiles request focused compatibility findings; they do not claim document
/// conformance.
/// </remarks>
public class PdfFontOptions : IPdfFontOptions
{
    PdfFontKind fontKind;
    PdfVersion version;
    PdfWritingMode writingMode;
    PdfFontEmbeddingMode embeddingMode;
    PdfFontRightsPolicy rightsPolicy;
    PdfArchivalProfile archivalProfile;
    PdfAccessibilityProfile accessibilityProfile;
    PdfCidAllocationMode cidAllocationMode;
    string? subsetPrefixOverride;
    FontSubsetHintPolicy hintPolicy;
    FontSubsetLayoutPolicy layoutPolicy;
    FontSubsetVerticalPolicy verticalPolicy;
    FontSubsetColorPolicy colorPolicy;
    FontSubsetMetadataPolicy metadataPolicy;
    PdfSimpleFontPolicy simpleFontPolicy;
    PdfStaticInstancePolicy staticInstancePolicy;
    PdfVersionCompatibilityPolicy versionCompatibilityPolicy;
    PdfProfileCompatibilityPolicy profileCompatibilityPolicy;
    FontOperationOptions operationOptions;

    /// <summary>Creates options using the compatible profile.</summary>
    public PdfFontOptions()
    {
        fontKind = PdfFontKind.Automatic;
        version = PdfVersion.Pdf1_7;
        writingMode = PdfWritingMode.Horizontal;
        embeddingMode = PdfFontEmbeddingMode.Subset;
        rightsPolicy = PdfFontRightsPolicy.EmbedFullFont;
        archivalProfile = PdfArchivalProfile.None;
        accessibilityProfile = PdfAccessibilityProfile.None;
        cidAllocationMode = PdfCidAllocationMode.Dense;
        subsetPrefixOverride = null;
        hintPolicy = FontSubsetHintPolicy.Drop;
        layoutPolicy = FontSubsetLayoutPolicy.Drop;
        verticalPolicy = FontSubsetVerticalPolicy.Drop;
        colorPolicy = FontSubsetColorPolicy.Drop;
        metadataPolicy = FontSubsetMetadataPolicy.RetainSafe;
        simpleFontPolicy = PdfSimpleFontPolicy.ExplicitOnly;
        staticInstancePolicy =
            PdfStaticInstancePolicy.MaterializeSelectedInstance;
        versionCompatibilityPolicy =
            PdfVersionCompatibilityPolicy.Reject;
        profileCompatibilityPolicy =
            PdfProfileCompatibilityPolicy.Report;
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Creates an explicitly configured option set.</summary>
    /// <param name="fontKind">The requested resource and program strategy.</param>
    /// <param name="version">The target PDF language version.</param>
    /// <param name="writingMode">The horizontal or vertical writing mode.</param>
    /// <param name="embeddingMode">Subset or full-font embedding.</param>
    /// <param name="rightsPolicy">The embedding-right response.</param>
    /// <param name="archivalProfile">The PDF/A profile to assess.</param>
    /// <param name="accessibilityProfile">The PDF/UA profile to assess.</param>
    /// <returns>A new configured PDF font option set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// An enum value is not defined.
    /// </exception>
    internal static PdfFontOptions Create(
        PdfFontKind fontKind,
        PdfVersion version,
        PdfWritingMode writingMode,
        PdfFontEmbeddingMode embeddingMode,
        PdfFontRightsPolicy rightsPolicy,
        PdfArchivalProfile archivalProfile,
        PdfAccessibilityProfile accessibilityProfile)
    {
        ValidateFontKind(fontKind, "fontKind");
        ValidateVersion(version, "version");
        ValidateWritingMode(writingMode, "writingMode");
        ValidateEmbeddingMode(embeddingMode, "embeddingMode");
        ValidateRightsPolicy(rightsPolicy, "rightsPolicy");
        ValidateArchivalProfile(archivalProfile, "archivalProfile");
        ValidateAccessibilityProfile(
            accessibilityProfile,
            "accessibilityProfile");
        PdfFontOptions options = new PdfFontOptions();
        options.fontKind = fontKind;
        options.version = version;
        options.writingMode = writingMode;
        options.embeddingMode = embeddingMode;
        options.rightsPolicy = rightsPolicy;
        options.archivalProfile = archivalProfile;
        options.accessibilityProfile = accessibilityProfile;
        return options;
    }

    /// <summary>Creates options using the compatible profile.</summary>
    /// <returns>A new compatible PDF font option set.</returns>
    internal static PdfFontOptions Create()
    {
        return new PdfFontOptions();
    }

    /// <summary>Creates a strict profile that rejects rights conflicts.</summary>
    /// <returns>A new strict PDF font option set.</returns>
    internal static PdfFontOptions CreateStrict()
    {
        PdfFontOptions options = PdfFontOptions.Create(
            PdfFontKind.Automatic,
            PdfVersion.Pdf1_7,
            PdfWritingMode.Horizontal,
            PdfFontEmbeddingMode.Subset,
            PdfFontRightsPolicy.Reject,
            PdfArchivalProfile.None,
            PdfAccessibilityProfile.None);
        FontOperationOptions operation = new FontOperationOptions();
        operation.SetMode(FontLoadMode.Strict);
        operation.SetLimits(FontLimits.CreateStrict());
        options.SetOperationOptions(operation);
        return options;
    }

    /// <summary>Creates the broadly compatible default profile.</summary>
    /// <returns>A new compatible PDF font option set.</returns>
    internal static PdfFontOptions CreateCompatible()
    {
        return PdfFontOptions.Create();
    }

    /// <summary>Gets the requested PDF font kind.</summary>
    /// <returns>The resource and embedded program strategy.</returns>
    public PdfFontKind GetFontKind() { return fontKind; }

    /// <summary>Sets the requested PDF font kind.</summary>
    /// <param name="value">The resource and embedded program strategy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetFontKind(PdfFontKind value)
    {
        ValidateFontKind(value, "value");
        fontKind = value;
    }

    /// <summary>Gets the target PDF language version.</summary>
    /// <returns>The target version.</returns>
    public PdfVersion GetVersion() { return version; }

    /// <summary>Sets the target PDF language version.</summary>
    /// <param name="value">The target version.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetVersion(PdfVersion value)
    {
        ValidateVersion(value, "value");
        version = value;
    }

    /// <summary>Gets the PDF writing mode.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    public PdfWritingMode GetWritingMode() { return writingMode; }

    /// <summary>Sets the PDF writing mode.</summary>
    /// <param name="value">The horizontal or vertical writing mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetWritingMode(PdfWritingMode value)
    {
        ValidateWritingMode(value, "value");
        writingMode = value;
    }

    /// <summary>Gets the requested embedding mode.</summary>
    /// <returns>Subset or full-font embedding.</returns>
    public PdfFontEmbeddingMode GetEmbeddingMode() { return embeddingMode; }

    /// <summary>Sets the requested embedding mode.</summary>
    /// <param name="value">Subset or full-font embedding.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetEmbeddingMode(PdfFontEmbeddingMode value)
    {
        ValidateEmbeddingMode(value, "value");
        embeddingMode = value;
    }

    /// <summary>Gets the embedding-right response policy.</summary>
    /// <returns>The rights policy.</returns>
    public PdfFontRightsPolicy GetRightsPolicy() { return rightsPolicy; }

    /// <summary>Sets the embedding-right response policy.</summary>
    /// <param name="value">The rights policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetRightsPolicy(PdfFontRightsPolicy value)
    {
        ValidateRightsPolicy(value, "value");
        rightsPolicy = value;
    }

    /// <summary>Gets the requested PDF/A compatibility profile.</summary>
    /// <returns>The archival profile.</returns>
    public PdfArchivalProfile GetArchivalProfile()
    {
        return archivalProfile;
    }

    /// <summary>Sets the PDF/A compatibility profile to assess.</summary>
    /// <param name="value">The archival profile.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetArchivalProfile(PdfArchivalProfile value)
    {
        ValidateArchivalProfile(value, "value");
        archivalProfile = value;
    }

    /// <summary>Gets the requested PDF/UA compatibility profile.</summary>
    /// <returns>The accessibility profile.</returns>
    public PdfAccessibilityProfile GetAccessibilityProfile()
    {
        return accessibilityProfile;
    }

    /// <summary>Sets the PDF/UA compatibility profile to assess.</summary>
    /// <param name="value">The accessibility profile.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetAccessibilityProfile(PdfAccessibilityProfile value)
    {
        ValidateAccessibilityProfile(value, "value");
        accessibilityProfile = value;
    }

    /// <summary>Gets the document-local CID allocation mode.</summary>
    /// <returns>The CID allocation mode.</returns>
    public PdfCidAllocationMode GetCidAllocationMode()
    {
        return cidAllocationMode;
    }

    /// <summary>Sets the document-local CID allocation mode.</summary>
    /// <param name="value">The CID allocation mode.</param>
    public void SetCidAllocationMode(PdfCidAllocationMode value)
    {
        ValidateCidAllocationMode(value, "value");
        cidAllocationMode = value;
    }

    /// <summary>Gets the caller-supplied subset-prefix collision override.</summary>
    /// <returns>Six uppercase letters, or null for deterministic generation.</returns>
    public string? GetSubsetPrefixOverride()
    {
        return subsetPrefixOverride;
    }

    /// <summary>Sets or clears the subset-prefix collision override.</summary>
    /// <param name="value">Six uppercase ASCII letters, or null.</param>
    public void SetSubsetPrefixOverride(string? value)
    {
        FontSubsetPrefix.ValidateOverride(value);
        subsetPrefixOverride = value;
    }

    /// <summary>Gets the embedded-font hint retention policy.</summary>
    /// <returns>The hint policy.</returns>
    public FontSubsetHintPolicy GetHintPolicy() { return hintPolicy; }

    /// <summary>Sets the embedded-font hint retention policy.</summary>
    /// <param name="value">The hint policy.</param>
    public void SetHintPolicy(FontSubsetHintPolicy value)
    {
        ValidateHintPolicy(value, "value");
        hintPolicy = value;
    }

    /// <summary>Gets the embedded-font layout retention policy.</summary>
    /// <returns>The layout policy.</returns>
    public FontSubsetLayoutPolicy GetLayoutPolicy() { return layoutPolicy; }

    /// <summary>Sets the embedded-font layout retention policy.</summary>
    /// <param name="value">The layout policy.</param>
    public void SetLayoutPolicy(FontSubsetLayoutPolicy value)
    {
        ValidateLayoutPolicy(value, "value");
        layoutPolicy = value;
    }

    /// <summary>Gets the embedded-font vertical-table retention policy.</summary>
    /// <returns>The vertical policy.</returns>
    public FontSubsetVerticalPolicy GetVerticalPolicy()
    {
        return verticalPolicy;
    }

    /// <summary>Sets the embedded-font vertical-table retention policy.</summary>
    /// <param name="value">The vertical policy.</param>
    public void SetVerticalPolicy(FontSubsetVerticalPolicy value)
    {
        ValidateVerticalPolicy(value, "value");
        verticalPolicy = value;
    }

    /// <summary>Gets the embedded-font color-table retention policy.</summary>
    /// <returns>The color policy.</returns>
    public FontSubsetColorPolicy GetColorPolicy() { return colorPolicy; }

    /// <summary>Sets the embedded-font color-table retention policy.</summary>
    /// <param name="value">The color policy.</param>
    public void SetColorPolicy(FontSubsetColorPolicy value)
    {
        ValidateColorPolicy(value, "value");
        colorPolicy = value;
    }

    /// <summary>Gets the embedded-font metadata retention policy.</summary>
    /// <returns>The metadata policy.</returns>
    public FontSubsetMetadataPolicy GetMetadataPolicy()
    {
        return metadataPolicy;
    }

    /// <summary>Sets the embedded-font metadata retention policy.</summary>
    /// <param name="value">The metadata policy.</param>
    public void SetMetadataPolicy(FontSubsetMetadataPolicy value)
    {
        ValidateMetadataPolicy(value, "value");
        metadataPolicy = value;
    }

    /// <summary>Gets the restricted simple-font availability policy.</summary>
    /// <returns>The simple-font policy.</returns>
    public PdfSimpleFontPolicy GetSimpleFontPolicy()
    {
        return simpleFontPolicy;
    }

    /// <summary>Sets the restricted simple-font availability policy.</summary>
    /// <param name="value">The simple-font policy.</param>
    public void SetSimpleFontPolicy(PdfSimpleFontPolicy value)
    {
        ValidateSimpleFontPolicy(value, "value");
        simpleFontPolicy = value;
    }

    /// <summary>Gets the variable-font static-instance policy.</summary>
    /// <returns>The static-instance policy.</returns>
    public PdfStaticInstancePolicy GetStaticInstancePolicy()
    {
        return staticInstancePolicy;
    }

    /// <summary>Sets the variable-font static-instance policy.</summary>
    /// <param name="value">The static-instance policy.</param>
    public void SetStaticInstancePolicy(PdfStaticInstancePolicy value)
    {
        ValidateStaticInstancePolicy(value, "value");
        staticInstancePolicy = value;
    }

    /// <summary>Gets the target-version mismatch policy.</summary>
    /// <returns>The version compatibility policy.</returns>
    public PdfVersionCompatibilityPolicy GetVersionCompatibilityPolicy()
    {
        return versionCompatibilityPolicy;
    }

    /// <summary>Sets the target-version mismatch policy.</summary>
    /// <param name="value">The version compatibility policy.</param>
    public void SetVersionCompatibilityPolicy(
        PdfVersionCompatibilityPolicy value)
    {
        ValidateVersionCompatibilityPolicy(value, "value");
        versionCompatibilityPolicy = value;
    }

    /// <summary>Gets the requested-profile finding policy.</summary>
    /// <returns>The profile compatibility policy.</returns>
    public PdfProfileCompatibilityPolicy GetProfileCompatibilityPolicy()
    {
        return profileCompatibilityPolicy;
    }

    /// <summary>Sets the requested-profile finding policy.</summary>
    /// <param name="value">The profile compatibility policy.</param>
    public void SetProfileCompatibilityPolicy(
        PdfProfileCompatibilityPolicy value)
    {
        ValidateProfileCompatibilityPolicy(value, "value");
        profileCompatibilityPolicy = value;
    }

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetOperationOptions(FontOperationOptions value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        operationOptions = value.Copy();
    }

    internal FontOperationOptions GetOperationSettings()
    {
        return operationOptions;
    }

    internal PdfFontOptions Copy()
    {
        PdfFontOptions copy = PdfFontOptions.Create(
            fontKind,
            version,
            writingMode,
            embeddingMode,
            rightsPolicy,
            archivalProfile,
            accessibilityProfile);
        copy.cidAllocationMode = cidAllocationMode;
        copy.subsetPrefixOverride = subsetPrefixOverride;
        copy.hintPolicy = hintPolicy;
        copy.layoutPolicy = layoutPolicy;
        copy.verticalPolicy = verticalPolicy;
        copy.colorPolicy = colorPolicy;
        copy.metadataPolicy = metadataPolicy;
        copy.simpleFontPolicy = simpleFontPolicy;
        copy.staticInstancePolicy = staticInstancePolicy;
        copy.versionCompatibilityPolicy = versionCompatibilityPolicy;
        copy.profileCompatibilityPolicy = profileCompatibilityPolicy;
        copy.operationOptions = operationOptions.Copy();
        return copy;
    }

    static void ValidateFontKind(
        PdfFontKind value,
        string parameterName)
    {
        if (value < PdfFontKind.Automatic ||
            value > PdfFontKind.SimpleTrueType)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateVersion(
        PdfVersion value,
        string parameterName)
    {
        if (value < PdfVersion.Pdf1_0 || value > PdfVersion.Pdf2_0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateWritingMode(
        PdfWritingMode value,
        string parameterName)
    {
        if (value < PdfWritingMode.Horizontal ||
            value > PdfWritingMode.Vertical)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateEmbeddingMode(
        PdfFontEmbeddingMode value,
        string parameterName)
    {
        if (value < PdfFontEmbeddingMode.Subset ||
            value > PdfFontEmbeddingMode.FullFont)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateRightsPolicy(
        PdfFontRightsPolicy value,
        string parameterName)
    {
        if (value < PdfFontRightsPolicy.Reject ||
            value > PdfFontRightsPolicy.EmbedFullFont)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateArchivalProfile(
        PdfArchivalProfile value,
        string parameterName)
    {
        if (value < PdfArchivalProfile.None ||
            value > PdfArchivalProfile.PdfA4)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateAccessibilityProfile(
        PdfAccessibilityProfile value,
        string parameterName)
    {
        if (value < PdfAccessibilityProfile.None ||
            value > PdfAccessibilityProfile.PdfUa2)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateCidAllocationMode(
        PdfCidAllocationMode value,
        string parameterName)
    {
        if (value < PdfCidAllocationMode.Dense ||
            value > PdfCidAllocationMode.PreserveSourceGlyphIdentifiers)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateHintPolicy(
        FontSubsetHintPolicy value,
        string parameterName)
    {
        if (value < FontSubsetHintPolicy.Drop ||
            value > FontSubsetHintPolicy.Retain)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateLayoutPolicy(
        FontSubsetLayoutPolicy value,
        string parameterName)
    {
        if (value < FontSubsetLayoutPolicy.Drop ||
            value > FontSubsetLayoutPolicy.Retain)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateVerticalPolicy(
        FontSubsetVerticalPolicy value,
        string parameterName)
    {
        if (value < FontSubsetVerticalPolicy.Drop ||
            value > FontSubsetVerticalPolicy.Retain)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateColorPolicy(
        FontSubsetColorPolicy value,
        string parameterName)
    {
        if (value < FontSubsetColorPolicy.Drop ||
            value > FontSubsetColorPolicy.Retain)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateMetadataPolicy(
        FontSubsetMetadataPolicy value,
        string parameterName)
    {
        if (value < FontSubsetMetadataPolicy.Minimal ||
            value > FontSubsetMetadataPolicy.RetainSafe)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateSimpleFontPolicy(
        PdfSimpleFontPolicy value,
        string parameterName)
    {
        if (value < PdfSimpleFontPolicy.ExplicitOnly ||
            value > PdfSimpleFontPolicy.Disallow)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateStaticInstancePolicy(
        PdfStaticInstancePolicy value,
        string parameterName)
    {
        if (value < PdfStaticInstancePolicy.MaterializeSelectedInstance ||
            value > PdfStaticInstancePolicy.RequireStaticSource)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateVersionCompatibilityPolicy(
        PdfVersionCompatibilityPolicy value,
        string parameterName)
    {
        if (value < PdfVersionCompatibilityPolicy.Reject ||
            value > PdfVersionCompatibilityPolicy.Report)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateProfileCompatibilityPolicy(
        PdfProfileCompatibilityPolicy value,
        string parameterName)
    {
        if (value < PdfProfileCompatibilityPolicy.Report ||
            value > PdfProfileCompatibilityPolicy.Reject)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
