namespace ttf;

internal static class PdfFontResourceBuilder
{
    internal static PdfFontResource Build(
        PdfFontUsage usage,
        PdfCidAllocator allocator)
    {
        if (usage == null || allocator == null)
        {
            throw new System.ArgumentNullException("values");
        }

        PdfFontKind requested = usage.CopyOptions().GetFontKind();
        PdfFontKind resolved = ResolveFontKind(usage, requested);

        if (resolved == PdfFontKind.SimpleTrueType)
        {
            PdfSimpleFontEncodingArtifact encoding =
                PdfFontEncodingBuilder.BuildSimple(usage, allocator);
            PdfCidFontType2Artifact program = BuildSimpleProgram(
                usage,
                allocator);
            return CreateResource(
                usage,
                allocator,
                resolved,
                program,
                null,
                null,
                encoding);
        }

        if (resolved == PdfFontKind.Type0CidFontType2)
        {
            return CreateResource(
                usage,
                allocator,
                resolved,
                PdfCidFontType2Builder.Build(usage, allocator),
                null,
                null,
                null);
        }

        if (resolved == PdfFontKind.Type0CidFontType0)
        {
            return CreateResource(
                usage,
                allocator,
                resolved,
                null,
                PdfCidFontType0Builder.Build(usage, allocator),
                null,
                null);
        }

        return CreateResource(
            usage,
            allocator,
            resolved,
            null,
            null,
            PdfOpenTypeFontBuilder.Build(usage, allocator),
            null);
    }

    static PdfFontResource CreateResource(
        PdfFontUsage usage,
        PdfCidAllocator allocator,
        PdfFontKind fontKind,
        PdfCidFontType2Artifact? type2,
        PdfCidFontType0Artifact? type0,
        PdfOpenTypeFontArtifact? openType,
        PdfSimpleFontEncodingArtifact? simpleEncoding)
    {
        byte[] embeddedFont;
        PdfToUnicodeMappingArtifact mappings;
        FontSubsetEmbeddingDecision decision;

        if (fontKind == PdfFontKind.Type0CidFontType0)
        {
            System.ArgumentNullException.ThrowIfNull(type0);
            embeddedFont = type0.CopyFontFile3();
            mappings = type0.GetToUnicodeMappings();
            decision = type0.GetEmbeddingDecision();
        }
        else if (fontKind == PdfFontKind.Type0OpenType)
        {
            System.ArgumentNullException.ThrowIfNull(openType);
            embeddedFont = openType.CopyFontFile3();
            mappings = openType.GetToUnicodeMappings();
            decision = openType.GetEmbeddingDecision();
        }
        else
        {
            System.ArgumentNullException.ThrowIfNull(type2);
            embeddedFont = type2.CopyFontFile2();
            if (fontKind == PdfFontKind.SimpleTrueType)
            {
                System.ArgumentNullException.ThrowIfNull(simpleEncoding);
            }
            if (fontKind == PdfFontKind.SimpleTrueType)
            {
                System.ArgumentNullException.ThrowIfNull(simpleEncoding);
                mappings = simpleEncoding.GetToUnicodeMappings();
            }
            else
            {
                mappings = type2.GetToUnicodeMappings();
            }
            decision = type2.GetEmbeddingDecision();
        }

        string key = PdfFontResourceKeyBuilder.Build(
            usage,
            allocator,
            fontKind,
            embeddedFont,
            mappings,
            decision);
        PdfFontOptions options = usage.CopyOptions();
        PdfType0EncodingMetadata? type0Encoding =
            fontKind == PdfFontKind.SimpleTrueType ?
            null : PdfFontEncodingBuilder.BuildType0(options);
        PdfFontResourceReport report =
            PdfFontResourceReportBuilder.Build(
                options,
                fontKind,
                allocator.CopyAllocatedCids(),
                mappings,
                decision);
        if (options.GetProfileCompatibilityPolicy() ==
                PdfProfileCompatibilityPolicy.Reject &&
            (report.HasPdfAEmbeddingConcerns() ||
                report.HasPdfUaUnicodeConcerns()))
        {
            throw new System.InvalidOperationException(
                "The PDF font resource has a rejected requested-profile finding.");
        }
        return new PdfFontResource(
            key,
            report,
            fontKind,
            type0Encoding,
            type2,
            type0,
            openType,
            simpleEncoding,
            options);
    }

    static PdfFontKind ResolveFontKind(
        PdfFontUsage usage,
        PdfFontKind requested)
    {
        if (requested != PdfFontKind.Automatic)
        {
            return requested;
        }

        FontFace face = usage.GetInstance().GetFace();
        FontOutlineKind outlineKind = face.GetOutlineKind();

        if (outlineKind == FontOutlineKind.TrueType)
        {
            return PdfFontKind.Type0CidFontType2;
        }

        SfntCffTable? cff = face.GetCffTable();
        if (outlineKind == FontOutlineKind.Cff1 && cff != null &&
            cff.GetTopDictionary().IsCidKeyed())
        {
            return PdfFontKind.Type0CidFontType0;
        }

        return PdfFontKind.Type0OpenType;
    }

    static PdfCidFontType2Artifact BuildSimpleProgram(
        PdfFontUsage usage,
        PdfCidAllocator allocator)
    {
        FontInstance instance = usage.GetInstance();
        PdfFontOptions options = usage.CopyOptions();
        options.SetFontKind(PdfFontKind.Type0CidFontType2);

        if (options.GetVersion() < PdfVersion.Pdf1_2)
        {
            options.SetVersion(PdfVersion.Pdf1_2);
        }

        PdfFontUsage mirror = new PdfFontUsage(
            PdfFontUsageKey.Create(instance, options),
            instance,
            options);
        int[] usageGlyphIds = usage.CopyGlyphIdsInUseOrder();
        int index = 0;

        while (index < usageGlyphIds.Length)
        {
            mirror.AddGlyph(usageGlyphIds[index], new int[0]);
            index += 1;
        }

        PdfFontUsageMapping[] mappings = usage.CopyMappings();
        index = 0;

        while (index < mappings.Length)
        {
            mirror.AddGlyph(
                mappings[index].GetGlyphId(),
                mappings[index].CopyUnicodeSequence());
            index += 1;
        }

        PdfCidAllocator mirrorAllocator = PdfCidAllocator.Create(mirror);

        return PdfCidFontType2Builder.Build(mirror, mirrorAllocator);
    }
}
