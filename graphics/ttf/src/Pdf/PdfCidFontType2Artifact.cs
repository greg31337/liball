namespace ttf;

internal static class PdfCidFontType2Builder
{
    internal static PdfCidFontType2Artifact Build(
        PdfFontUsage usage,
        PdfCidAllocator allocator)
    {
        if (usage == null || allocator == null)
        {
            throw new System.ArgumentNullException("values");
        }

        FontInstance instance = usage.GetInstance();

        if (allocator.GetSourceGlyphCount() != instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The CID allocator does not belong to the usage font.",
                "allocator");
        }

        PdfFontOptions pdfOptions = usage.CopyOptions();
        ValidateSelection(instance, pdfOptions);
        int[] usageGlyphIds = usage.CopyGlyphIdsInUseOrder();
        allocator.GetOrAddCids(usageGlyphIds, ushort.MaxValue);
        int[] glyphIds =
            allocator.CopyAllocatedSourceGlyphIdsInCidOrder();
        FontSubsetBuilder subsetBuilder = new FontSubsetBuilder(
            instance,
            PdfFontOptionResolver.CreateSubsetOptions(pdfOptions));
        int index = 0;

        while (index < glyphIds.Length)
        {
            subsetBuilder.AddGlyph(glyphIds[index]);
            index += 1;
        }

        FontSubsetResult result = subsetBuilder.BuildResult();
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            result.GetOldToNewGlyphMap(),
            result.GetNewToOldGlyphMap());
        PdfCidMappingArtifact cidMapping =
            PdfCidMappingBuilder.BuildType2(allocation, pdfOptions);
        PdfToUnicodeMappingArtifact toUnicodeMappings =
            PdfToUnicodeMappingBuilder.Build(usage, allocator);
        PdfCidWidthsArtifact widths =
            PdfCidWidthsBuilder.Build(instance, allocation);
        PdfCidVerticalWidthsArtifact? verticalWidths =
            pdfOptions.GetWritingMode() == PdfWritingMode.Vertical ?
            PdfCidVerticalWidthsBuilder.Build(instance, allocation) : null;
        PdfFontDescriptorFlagsReport descriptorFlags =
            PdfFontDescriptorFlagsBuilder.Build(instance);
        byte[] fontFile2 = result.CopyFontBytes();
        PdfFontResourceNames resourceNames =
            PdfFontResourceNamesBuilder.Build(result.GetSubsetName());
        PdfFontDescriptorMetrics descriptorMetrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile2,
                null,
                fontFile2.Length);
        return new PdfCidFontType2Artifact(
            fontFile2,
            resourceNames,
            allocation,
            cidMapping,
            toUnicodeMappings,
            widths,
            verticalWidths,
            descriptorFlags,
            descriptorMetrics,
            result.GetEmbeddingDecision());
    }

    static void ValidateSelection(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (instance.GetFace().GetOutlineKind() != FontOutlineKind.TrueType)
        {
            throw new System.InvalidOperationException(
                "CIDFontType2 requires TrueType outlines.");
        }

        PdfFontKind kind = options.GetFontKind();

        if (kind != PdfFontKind.Automatic &&
            kind != PdfFontKind.Type0CidFontType2)
        {
            throw new System.InvalidOperationException(
                "The selected PDF font kind is not CIDFontType2.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_2 &&
            PdfFontOptionResolver.RejectsVersionMismatch(options))
        {
            throw new System.InvalidOperationException(
                "CIDFontType2 requires PDF 1.2 or later.");
        }
    }
}
