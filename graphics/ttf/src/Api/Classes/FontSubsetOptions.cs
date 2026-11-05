namespace ttf;

/// <summary>Defines deterministic font subsetting, retention, and rights policies.</summary>
/// <remarks>
/// The default is the compatible profile: compact subset output, conventional
/// control and space glyph retention, no hint, layout, vertical, color, or
/// unknown tables, safe metadata retention, and full-font fallback when rights
/// permit embedding but prohibit subsetting.
/// Unknown tables may be retained in subset output only when original glyph
/// identifiers are preserved because their internal glyph references are not
/// known.
/// </remarks>
public class FontSubsetOptions : IFontSubsetOptions
{
    FontSubsetEmbeddingMode embeddingMode;
    FontSubsetGlyphRetentionMode glyphRetentionMode;
    FontSubsetRecommendedGlyphPolicy recommendedGlyphPolicy;
    FontSubsetHintPolicy hintPolicy;
    FontSubsetLayoutPolicy layoutPolicy;
    FontSubsetVerticalPolicy verticalPolicy;
    FontSubsetColorPolicy colorPolicy;
    FontSubsetMetadataPolicy metadataPolicy;
    FontSubsetUnknownTablePolicy unknownTablePolicy;
    FontSubsetRightsPolicy rightsPolicy;
    string? subsetPrefixOverride;
    FontOperationOptions operationOptions;

    /// <summary>Creates options using the compatible profile.</summary>
    public FontSubsetOptions()
    {
        embeddingMode = FontSubsetEmbeddingMode.Subset;
        glyphRetentionMode = FontSubsetGlyphRetentionMode.Compact;
        recommendedGlyphPolicy =
            FontSubsetRecommendedGlyphPolicy.IncludeRecommended;
        hintPolicy = FontSubsetHintPolicy.Drop;
        layoutPolicy = FontSubsetLayoutPolicy.Drop;
        verticalPolicy = FontSubsetVerticalPolicy.Drop;
        colorPolicy = FontSubsetColorPolicy.Drop;
        metadataPolicy = FontSubsetMetadataPolicy.RetainSafe;
        unknownTablePolicy = FontSubsetUnknownTablePolicy.Drop;
        rightsPolicy = FontSubsetRightsPolicy.EmbedFullFont;
        subsetPrefixOverride = null;
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Creates an explicitly configured option set.</summary>
    /// <param name="embeddingMode">Subset or full-font output.</param>
    /// <param name="glyphRetentionMode">Compact or preserved glyph identifiers.</param>
    /// <param name="hintPolicy">Hint data retention.</param>
    /// <param name="layoutPolicy">Layout table retention.</param>
    /// <param name="verticalPolicy">Vertical table retention.</param>
    /// <param name="colorPolicy">Color table retention.</param>
    /// <param name="metadataPolicy">Descriptive metadata retention.</param>
    /// <param name="unknownTablePolicy">Unknown table retention.</param>
    /// <param name="rightsPolicy">Embedding-right restriction response.</param>
    /// <returns>A new configured option set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A policy value is not defined.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Compact subset output requests retention of unknown tables.
    /// </exception>
    /// <remarks>
    /// This overload uses the recommended control and space glyph policy.
    /// </remarks>
    internal static FontSubsetOptions Create(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode glyphRetentionMode,
        FontSubsetHintPolicy hintPolicy,
        FontSubsetLayoutPolicy layoutPolicy,
        FontSubsetVerticalPolicy verticalPolicy,
        FontSubsetColorPolicy colorPolicy,
        FontSubsetMetadataPolicy metadataPolicy,
        FontSubsetUnknownTablePolicy unknownTablePolicy,
        FontSubsetRightsPolicy rightsPolicy)
    {
        ValidateEmbeddingMode(embeddingMode, "embeddingMode");
        ValidateGlyphRetentionMode(glyphRetentionMode, "glyphRetentionMode");
        ValidateHintPolicy(hintPolicy, "hintPolicy");
        ValidateLayoutPolicy(layoutPolicy, "layoutPolicy");
        ValidateVerticalPolicy(verticalPolicy, "verticalPolicy");
        ValidateColorPolicy(colorPolicy, "colorPolicy");
        ValidateMetadataPolicy(metadataPolicy, "metadataPolicy");
        ValidateUnknownTablePolicy(unknownTablePolicy, "unknownTablePolicy");
        ValidateRightsPolicy(rightsPolicy, "rightsPolicy");
        ValidateCombination(
            embeddingMode,
            glyphRetentionMode,
            unknownTablePolicy,
            "unknownTablePolicy");
        FontSubsetOptions options = new FontSubsetOptions();
        options.embeddingMode = embeddingMode;
        options.glyphRetentionMode = glyphRetentionMode;
        options.recommendedGlyphPolicy =
            FontSubsetRecommendedGlyphPolicy.IncludeRecommended;
        options.hintPolicy = hintPolicy;
        options.layoutPolicy = layoutPolicy;
        options.verticalPolicy = verticalPolicy;
        options.colorPolicy = colorPolicy;
        options.metadataPolicy = metadataPolicy;
        options.unknownTablePolicy = unknownTablePolicy;
        options.rightsPolicy = rightsPolicy;
        options.subsetPrefixOverride = null;
        return options;
    }

    /// <summary>Creates an explicitly configured option set.</summary>
    /// <param name="embeddingMode">Subset or full-font output.</param>
    /// <param name="glyphRetentionMode">Compact or preserved glyph identifiers.</param>
    /// <param name="recommendedGlyphPolicy">Recommended glyph retention.</param>
    /// <param name="hintPolicy">Hint data retention.</param>
    /// <param name="layoutPolicy">Layout table retention.</param>
    /// <param name="verticalPolicy">Vertical table retention.</param>
    /// <param name="colorPolicy">Color table retention.</param>
    /// <param name="metadataPolicy">Descriptive metadata retention.</param>
    /// <param name="unknownTablePolicy">Unknown table retention.</param>
    /// <param name="rightsPolicy">Embedding-right restriction response.</param>
    /// <returns>A new configured option set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A policy value is not defined.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Compact subset output requests retention of unknown tables.
    /// </exception>
    internal static FontSubsetOptions Create(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode glyphRetentionMode,
        FontSubsetRecommendedGlyphPolicy recommendedGlyphPolicy,
        FontSubsetHintPolicy hintPolicy,
        FontSubsetLayoutPolicy layoutPolicy,
        FontSubsetVerticalPolicy verticalPolicy,
        FontSubsetColorPolicy colorPolicy,
        FontSubsetMetadataPolicy metadataPolicy,
        FontSubsetUnknownTablePolicy unknownTablePolicy,
        FontSubsetRightsPolicy rightsPolicy)
    {
        ValidateEmbeddingMode(embeddingMode, "embeddingMode");
        ValidateGlyphRetentionMode(glyphRetentionMode, "glyphRetentionMode");
        ValidateRecommendedGlyphPolicy(
            recommendedGlyphPolicy,
            "recommendedGlyphPolicy");
        ValidateHintPolicy(hintPolicy, "hintPolicy");
        ValidateLayoutPolicy(layoutPolicy, "layoutPolicy");
        ValidateVerticalPolicy(verticalPolicy, "verticalPolicy");
        ValidateColorPolicy(colorPolicy, "colorPolicy");
        ValidateMetadataPolicy(metadataPolicy, "metadataPolicy");
        ValidateUnknownTablePolicy(unknownTablePolicy, "unknownTablePolicy");
        ValidateRightsPolicy(rightsPolicy, "rightsPolicy");
        ValidateCombination(
            embeddingMode,
            glyphRetentionMode,
            unknownTablePolicy,
            "unknownTablePolicy");
        FontSubsetOptions options = new FontSubsetOptions();
        options.embeddingMode = embeddingMode;
        options.glyphRetentionMode = glyphRetentionMode;
        options.recommendedGlyphPolicy = recommendedGlyphPolicy;
        options.hintPolicy = hintPolicy;
        options.layoutPolicy = layoutPolicy;
        options.verticalPolicy = verticalPolicy;
        options.colorPolicy = colorPolicy;
        options.metadataPolicy = metadataPolicy;
        options.unknownTablePolicy = unknownTablePolicy;
        options.rightsPolicy = rightsPolicy;
        options.subsetPrefixOverride = null;
        return options;
    }

    /// <summary>Creates options using the compatible profile.</summary>
    /// <returns>A new compatible option set.</returns>
    internal static FontSubsetOptions Create()
    {
        return new FontSubsetOptions();
    }

    /// <summary>Creates a strict compact-subset profile.</summary>
    /// <returns>A new profile that rejects rights conflicts and emits minimal metadata.</returns>
    internal static FontSubsetOptions CreateStrict()
    {
        FontSubsetOptions options = FontSubsetOptions.Create(
            FontSubsetEmbeddingMode.Subset,
            FontSubsetGlyphRetentionMode.Compact,
            FontSubsetRecommendedGlyphPolicy.RequestedOnly,
            FontSubsetHintPolicy.Drop,
            FontSubsetLayoutPolicy.Drop,
            FontSubsetVerticalPolicy.Drop,
            FontSubsetColorPolicy.Drop,
            FontSubsetMetadataPolicy.Minimal,
            FontSubsetUnknownTablePolicy.Drop,
            FontSubsetRightsPolicy.Reject);
        FontOperationOptions operation = new FontOperationOptions();
        operation.SetMode(FontLoadMode.Strict);
        operation.SetLimits(FontLimits.CreateStrict());
        options.SetOperationOptions(operation);
        return options;
    }

    /// <summary>Creates a compatible compact-subset profile.</summary>
    /// <returns>A new profile that retains safe metadata and honors no-subsetting through full output.</returns>
    internal static FontSubsetOptions CreateCompatible()
    {
        return FontSubsetOptions.Create();
    }

    /// <summary>Gets the requested embedding mode.</summary>
    /// <returns>The subset or full-font mode.</returns>
    public FontSubsetEmbeddingMode GetEmbeddingMode() { return embeddingMode; }

    /// <summary>Sets the requested embedding mode.</summary>
    /// <param name="value">The subset or full-font mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Subset mode conflicts with compact glyph identifiers and retained unknown tables.
    /// </exception>
    public void SetEmbeddingMode(FontSubsetEmbeddingMode value)
    {
        ValidateEmbeddingMode(value, "value");
        ValidateCombination(
            value,
            glyphRetentionMode,
            unknownTablePolicy,
            "value");
        embeddingMode = value;
    }

    /// <summary>Gets the glyph identifier retention mode.</summary>
    /// <returns>The compact or preserve mode.</returns>
    public FontSubsetGlyphRetentionMode GetGlyphRetentionMode()
    {
        return glyphRetentionMode;
    }

    /// <summary>Sets the glyph identifier retention mode.</summary>
    /// <param name="value">The compact or preserve mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Compact subset output conflicts with retained unknown tables.
    /// </exception>
    public void SetGlyphRetentionMode(FontSubsetGlyphRetentionMode value)
    {
        ValidateGlyphRetentionMode(value, "value");
        ValidateCombination(
            embeddingMode,
            value,
            unknownTablePolicy,
            "value");
        glyphRetentionMode = value;
    }

    /// <summary>Gets the recommended glyph retention policy.</summary>
    /// <returns>The recommended glyph policy.</returns>
    public FontSubsetRecommendedGlyphPolicy GetRecommendedGlyphPolicy()
    {
        return recommendedGlyphPolicy;
    }

    /// <summary>Sets the recommended glyph retention policy.</summary>
    /// <param name="value">The recommended glyph policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    public void SetRecommendedGlyphPolicy(
        FontSubsetRecommendedGlyphPolicy value)
    {
        ValidateRecommendedGlyphPolicy(value, "value");
        recommendedGlyphPolicy = value;
    }

    /// <summary>Gets the hint retention policy.</summary>
    /// <returns>The hint policy.</returns>
    public FontSubsetHintPolicy GetHintPolicy() { return hintPolicy; }

    /// <summary>Sets the hint retention policy.</summary>
    /// <param name="value">The hint policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetHintPolicy(FontSubsetHintPolicy value)
    {
        ValidateHintPolicy(value, "value");
        hintPolicy = value;
    }

    /// <summary>Gets the layout retention policy.</summary>
    /// <returns>The layout policy.</returns>
    public FontSubsetLayoutPolicy GetLayoutPolicy() { return layoutPolicy; }

    /// <summary>Sets the layout retention policy.</summary>
    /// <param name="value">The layout policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetLayoutPolicy(FontSubsetLayoutPolicy value)
    {
        ValidateLayoutPolicy(value, "value");
        layoutPolicy = value;
    }

    /// <summary>Gets the vertical table retention policy.</summary>
    /// <returns>The vertical policy.</returns>
    public FontSubsetVerticalPolicy GetVerticalPolicy() { return verticalPolicy; }

    /// <summary>Sets the vertical table retention policy.</summary>
    /// <param name="value">The vertical policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetVerticalPolicy(FontSubsetVerticalPolicy value)
    {
        ValidateVerticalPolicy(value, "value");
        verticalPolicy = value;
    }

    /// <summary>Gets the color table retention policy.</summary>
    /// <returns>The color policy.</returns>
    public FontSubsetColorPolicy GetColorPolicy() { return colorPolicy; }

    /// <summary>Sets the color table retention policy.</summary>
    /// <param name="value">The color policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetColorPolicy(FontSubsetColorPolicy value)
    {
        ValidateColorPolicy(value, "value");
        colorPolicy = value;
    }

    /// <summary>Gets the metadata retention policy.</summary>
    /// <returns>The metadata policy.</returns>
    public FontSubsetMetadataPolicy GetMetadataPolicy() { return metadataPolicy; }

    /// <summary>Sets the metadata retention policy.</summary>
    /// <param name="value">The metadata policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetMetadataPolicy(FontSubsetMetadataPolicy value)
    {
        ValidateMetadataPolicy(value, "value");
        metadataPolicy = value;
    }

    /// <summary>Gets the unknown table retention policy.</summary>
    /// <returns>The unknown table policy.</returns>
    public FontSubsetUnknownTablePolicy GetUnknownTablePolicy()
    {
        return unknownTablePolicy;
    }

    /// <summary>Sets the unknown table retention policy.</summary>
    /// <param name="value">The unknown table policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Safe retention conflicts with compact subset glyph identifiers.
    /// </exception>
    public void SetUnknownTablePolicy(FontSubsetUnknownTablePolicy value)
    {
        ValidateUnknownTablePolicy(value, "value");
        ValidateCombination(
            embeddingMode,
            glyphRetentionMode,
            value,
            "value");
        unknownTablePolicy = value;
    }

    /// <summary>Gets the embedding-right response policy.</summary>
    /// <returns>The rights policy.</returns>
    public FontSubsetRightsPolicy GetRightsPolicy() { return rightsPolicy; }

    /// <summary>Sets the embedding-right response policy.</summary>
    /// <param name="value">The rights policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    public void SetRightsPolicy(FontSubsetRightsPolicy value)
    {
        ValidateRightsPolicy(value, "value");
        rightsPolicy = value;
    }

    /// <summary>Gets the caller-selected subset prefix override.</summary>
    /// <returns>
    /// The six-uppercase-letter prefix, or null when a deterministic prefix
    /// should be generated.
    /// </returns>
    public string? GetSubsetPrefixOverride()
    {
        return subsetPrefixOverride;
    }

    /// <summary>Sets or clears the subset prefix collision override.</summary>
    /// <param name="value">
    /// Exactly six ASCII uppercase letters, or null to use deterministic
    /// generation.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// <c>value</c> is non-null and is not exactly six ASCII uppercase letters.
    /// </exception>
    public void SetSubsetPrefixOverride(string? value)
    {
        FontSubsetPrefix.ValidateOverride(value);
        subsetPrefixOverride = value;
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

    static void ValidateEmbeddingMode(
        FontSubsetEmbeddingMode value,
        string parameterName)
    {
        if (value < FontSubsetEmbeddingMode.Subset ||
            value > FontSubsetEmbeddingMode.FullFont)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateGlyphRetentionMode(
        FontSubsetGlyphRetentionMode value,
        string parameterName)
    {
        if (value < FontSubsetGlyphRetentionMode.Compact ||
            value > FontSubsetGlyphRetentionMode.Preserve)
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

    static void ValidateRecommendedGlyphPolicy(
        FontSubsetRecommendedGlyphPolicy value,
        string parameterName)
    {
        if (value < FontSubsetRecommendedGlyphPolicy.RequestedOnly ||
            value > FontSubsetRecommendedGlyphPolicy.IncludeRecommended)
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

    static void ValidateUnknownTablePolicy(
        FontSubsetUnknownTablePolicy value,
        string parameterName)
    {
        if (value < FontSubsetUnknownTablePolicy.Drop ||
            value > FontSubsetUnknownTablePolicy.RetainSafe)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateRightsPolicy(
        FontSubsetRightsPolicy value,
        string parameterName)
    {
        if (value < FontSubsetRightsPolicy.Reject ||
            value > FontSubsetRightsPolicy.EmbedFullFont)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateCombination(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetGlyphRetentionMode glyphRetentionMode,
        FontSubsetUnknownTablePolicy unknownTablePolicy,
        string parameterName)
    {
        if (embeddingMode == FontSubsetEmbeddingMode.Subset &&
            glyphRetentionMode == FontSubsetGlyphRetentionMode.Compact &&
            unknownTablePolicy == FontSubsetUnknownTablePolicy.RetainSafe)
        {
            throw new System.ArgumentException(
                "Unknown tables can be retained in subset output only when original glyph identifiers are preserved.",
                parameterName);
        }
    }
}
