namespace ttf;

internal static class PdfFontOptionResolver
{
    internal static FontSubsetOptions CreateSubsetOptions(
        PdfFontOptions options)
    {
        FontSubsetEmbeddingMode embeddingMode =
            options.GetEmbeddingMode() == PdfFontEmbeddingMode.Subset ?
            FontSubsetEmbeddingMode.Subset :
            FontSubsetEmbeddingMode.FullFont;
        FontSubsetGlyphRetentionMode glyphRetentionMode =
            options.GetCidAllocationMode() ==
                PdfCidAllocationMode.PreserveSourceGlyphIdentifiers ?
            FontSubsetGlyphRetentionMode.Preserve :
            FontSubsetGlyphRetentionMode.Compact;
        FontSubsetOptions subsetOptions = FontSubsetOptions.Create(
            embeddingMode,
            glyphRetentionMode,
            FontSubsetRecommendedGlyphPolicy.RequestedOnly,
            options.GetHintPolicy(),
            options.GetLayoutPolicy(),
            options.GetVerticalPolicy(),
            options.GetColorPolicy(),
            options.GetMetadataPolicy(),
            FontSubsetUnknownTablePolicy.Drop,
            ConvertRightsPolicy(options.GetRightsPolicy()));
        subsetOptions.SetSubsetPrefixOverride(
            options.GetSubsetPrefixOverride());
        return subsetOptions;
    }

    internal static void ValidateInstance(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (options.GetSimpleFontPolicy() ==
                PdfSimpleFontPolicy.Disallow &&
            options.GetFontKind() == PdfFontKind.SimpleTrueType)
        {
            throw new System.InvalidOperationException(
                "Simple PDF fonts are disabled by the selected policy.");
        }

        if (options.GetStaticInstancePolicy() ==
                PdfStaticInstancePolicy.RequireStaticSource &&
            HasVariationData(instance.GetFace()))
        {
            throw new System.InvalidOperationException(
                "The selected PDF policy requires a source without live variation data.");
        }
    }

    internal static bool RejectsVersionMismatch(PdfFontOptions options)
    {
        return options.GetVersionCompatibilityPolicy() ==
            PdfVersionCompatibilityPolicy.Reject;
    }

    static FontSubsetRightsPolicy ConvertRightsPolicy(
        PdfFontRightsPolicy policy)
    {
        if (policy == PdfFontRightsPolicy.Reject)
        {
            return FontSubsetRightsPolicy.Reject;
        }

        if (policy == PdfFontRightsPolicy.Report)
        {
            return FontSubsetRightsPolicy.Report;
        }

        return FontSubsetRightsPolicy.EmbedFullFont;
    }

    static bool HasVariationData(FontFace face)
    {
        return face.GetOutlineKind() == FontOutlineKind.Cff2 ||
            face.HasTable(FontTags.Fvar) ||
            face.HasTable(FontTags.Avar) ||
            face.HasTable(FontTags.Gvar) ||
            face.HasTable(FontTags.Cvar) ||
            face.HasTable(FontTags.Hvar) ||
            face.HasTable(FontTags.Vvar) ||
            face.HasTable(FontTags.Mvar);
    }
}
