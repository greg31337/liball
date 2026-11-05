namespace ttf;

internal static class PdfCidFontType0Builder
{
    internal static PdfCidFontType0Artifact Build(
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

        FontSubset subset = subsetBuilder.Build();
        int[] oldToNewGlyphIds = subset.GetOldToNewGlyphMap();
        int[] newToOldGlyphIds = subset.GetNewToOldGlyphMap();
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            oldToNewGlyphIds,
            newToOldGlyphIds);
        int[] charsetCids = CreateCharsetCids(
            allocation,
            newToOldGlyphIds.Length);
        byte[] fontFile3 = subset.CreatePdfCidCffData(
            charsetCids).CopyData();
        PdfCidMappingArtifact cidMapping =
            PdfCidMappingBuilder.BuildType0(
                allocation,
                charsetCids,
                pdfOptions);
        PdfToUnicodeMappingArtifact toUnicodeMappings =
            PdfToUnicodeMappingBuilder.Build(usage, allocator);
        PdfCidWidthsArtifact widths =
            PdfCidWidthsBuilder.Build(instance, allocation);
        PdfCidVerticalWidthsArtifact? verticalWidths =
            pdfOptions.GetWritingMode() == PdfWritingMode.Vertical ?
            PdfCidVerticalWidthsBuilder.Build(instance, allocation) : null;
        PdfFontDescriptorFlagsReport descriptorFlags =
            PdfFontDescriptorFlagsBuilder.Build(instance);
        PdfFontResourceNames resourceNames =
            PdfFontResourceNamesBuilder.Build(subset.GetSubsetName());
        PdfFontDescriptorMetrics descriptorMetrics =
            PdfFontDescriptorMetricsBuilder.Build(
                instance,
                PdfEmbeddedFontFileKind.FontFile3,
                "CIDFontType0C",
                fontFile3.Length);
        return new PdfCidFontType0Artifact(
            fontFile3,
            resourceNames,
            allocation,
            charsetCids,
            cidMapping,
            toUnicodeMappings,
            widths,
            verticalWidths,
            descriptorFlags,
            descriptorMetrics,
            subset.GetEmbeddingDecision());
    }

    internal static int[] CreateCharsetCids(
        PdfCidAllocation allocation,
        int glyphCount)
    {
        int[] charsetCids = allocation.CopySubsetGlyphToCids();

        if (charsetCids.Length != glyphCount)
        {
            throw FontFormatException.Create(
                "The CFF subset map does not match its glyph count.");
        }

        int nextCid = allocation.GetCidCount();
        int glyphId = 0;

        while (glyphId < charsetCids.Length)
        {
            if (charsetCids[glyphId] < 0)
            {
                if (nextCid > ushort.MaxValue)
                {
                    throw FontLimitException.Create(
                        "The CFF charset exceeds the 16-bit CID space.");
                }

                charsetCids[glyphId] = nextCid;
                nextCid += 1;
            }

            glyphId += 1;
        }

        return charsetCids;
    }

    static void ValidateSelection(
        FontInstance instance,
        PdfFontOptions options)
    {
        FontFace face = instance.GetFace();
        SfntCffTable? cff = face.GetCffTable();

        if (face.GetOutlineKind() != FontOutlineKind.Cff1 ||
            cff == null || !cff.GetTopDictionary().IsCidKeyed())
        {
            throw new System.InvalidOperationException(
                "CIDFontType0 requires CID-keyed CFF 1 outlines.");
        }

        PdfFontKind kind = options.GetFontKind();

        if (kind != PdfFontKind.Automatic &&
            kind != PdfFontKind.Type0CidFontType0)
        {
            throw new System.InvalidOperationException(
                "The selected PDF font kind is not CIDFontType0.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_2 &&
            PdfFontOptionResolver.RejectsVersionMismatch(options))
        {
            throw new System.InvalidOperationException(
                "CIDFontType0 requires PDF 1.2 or later.");
        }
    }
}
