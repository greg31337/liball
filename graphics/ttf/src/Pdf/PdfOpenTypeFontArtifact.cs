namespace ttf;

internal static class PdfOpenTypeFontBuilder
{
    internal static PdfOpenTypeFontArtifact Build(
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
        int[] newToOldGlyphIds = subset.GetNewToOldGlyphMap();
        PdfCidAllocation allocation = allocator.BindSubsetGlyphs(
            subset.GetOldToNewGlyphMap(),
            newToOldGlyphIds);
        FontOutlineKind sourceKind = instance.GetFace().GetOutlineKind();
        FontOutlineKind embeddedKind = sourceKind == FontOutlineKind.TrueType ?
            FontOutlineKind.TrueType : FontOutlineKind.Cff1;
        PdfFontKind descendantKind = sourceKind == FontOutlineKind.TrueType ?
            PdfFontKind.Type0CidFontType2 :
            PdfFontKind.Type0CidFontType0;
        bool usesCffCharset = IsCidKeyedCff(instance);
        bool usesDirectCids = sourceKind != FontOutlineKind.TrueType &&
            !usesCffCharset;
        int[] charsetCids = new int[0];
        byte[]? cffData = null;

        if (usesCffCharset)
        {
            charsetCids = PdfCidFontType0Builder.CreateCharsetCids(
                allocation,
                newToOldGlyphIds.Length);
            cffData = subset.CreatePdfCidCffData(
                charsetCids).CopyData();
        }
        else if (usesDirectCids)
        {
            ValidateDirectCidMapping(allocation);
            cffData = subset.CreateCffData().CopyData();
        }

        byte[] fontFile3 = FontSubsetResultBuilder.CreatePdfOpenType(
            subset,
            cffData);
        PdfCidMappingArtifact cidMapping;

        if (descendantKind == PdfFontKind.Type0CidFontType2)
        {
            cidMapping = PdfCidMappingBuilder.BuildType2(
                allocation,
                pdfOptions);
        }
        else
        {
            int[] embeddedCids = usesCffCharset ?
                charsetCids : PdfCidMappingBuilder.CreateDenseCids(
                    newToOldGlyphIds.Length);
            cidMapping = PdfCidMappingBuilder.BuildType0(
                allocation,
                embeddedCids,
                pdfOptions);
        }

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
                "OpenType",
                fontFile3.Length);

        return new PdfOpenTypeFontArtifact(
            fontFile3,
            resourceNames,
            allocation,
            sourceKind,
            embeddedKind,
            descendantKind,
            usesCffCharset,
            usesDirectCids,
            charsetCids,
            cidMapping,
            toUnicodeMappings,
            widths,
            verticalWidths,
            descriptorFlags,
            descriptorMetrics,
            subset.GetEmbeddingDecision());
    }

    static bool IsCidKeyedCff(FontInstance instance)
    {
        FontFace face = instance.GetFace();
        SfntCffTable? cff = face.GetCffTable();
        return face.GetOutlineKind() == FontOutlineKind.Cff1 && cff != null &&
            cff.GetTopDictionary().IsCidKeyed();
    }

    static void ValidateDirectCidMapping(PdfCidAllocation allocation)
    {
        int cid = 0;

        while (cid < allocation.GetCidCount())
        {
            if (allocation.GetSourceGlyphId(cid) >= 0 &&
                allocation.GetSubsetGlyphId(cid) != cid)
            {
                throw new System.InvalidOperationException(
                    "A name-keyed CFF OpenType program requires each document CID to equal its embedded GID.");
            }

            cid += 1;
        }
    }

    static void ValidateSelection(
        FontInstance instance,
        PdfFontOptions options)
    {
        FontOutlineKind kind = instance.GetFace().GetOutlineKind();

        if (kind != FontOutlineKind.TrueType &&
            kind != FontOutlineKind.Cff1 &&
            kind != FontOutlineKind.Cff2)
        {
            throw new System.InvalidOperationException(
                "OpenType FontFile3 output requires TrueType or CFF outlines.");
        }

        PdfFontKind fontKind = options.GetFontKind();

        if (fontKind != PdfFontKind.Automatic &&
            fontKind != PdfFontKind.Type0OpenType)
        {
            throw new System.InvalidOperationException(
                "The selected PDF font kind is not OpenType.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_6 &&
            PdfFontOptionResolver.RejectsVersionMismatch(options))
        {
            throw new System.InvalidOperationException(
                "OpenType FontFile3 output requires PDF 1.6 or later.");
        }
    }
}
