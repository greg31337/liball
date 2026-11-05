namespace ttf;

internal static class PdfFontEncodingBuilder
{
    internal static PdfType0EncodingMetadata BuildType0(
        PdfFontOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.GetFontKind() == PdfFontKind.SimpleTrueType)
        {
            throw new System.InvalidOperationException(
                "A simple font cannot use an Identity CMap.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_2 &&
            PdfFontOptionResolver.RejectsVersionMismatch(options))
        {
            throw new System.InvalidOperationException(
                "Identity CMaps require PDF 1.2 or later.");
        }

        PdfWritingMode writingMode = options.GetWritingMode();
        string name = writingMode == PdfWritingMode.Horizontal ?
            "Identity-H" : "Identity-V";
        return new PdfType0EncodingMetadata(name, writingMode);
    }

    internal static PdfSimpleFontEncodingArtifact BuildSimple(
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

        PdfFontOptions options = usage.CopyOptions();
        ValidateSimpleSelection(instance, options);
        int[] usageGlyphIds = usage.CopyGlyphIdsInUseOrder();
        allocator.GetOrAddCids(usageGlyphIds, byte.MaxValue);
        int[] sourceGlyphIds = allocator.CopySourceGlyphIdsInCidOrder();
        PdfSimpleFontEncodingDifference[] differences =
            new PdfSimpleFontEncodingDifference[sourceGlyphIds.Length];
        int code = 0;

        while (code < differences.Length)
        {
            int sourceGlyphId = sourceGlyphIds[code];
            int glyphId = sourceGlyphId < 0 ? 0 : sourceGlyphId;
            string glyphName = code == 0 || sourceGlyphId < 0 ?
                ".notdef" : instance.GetGlyphName(glyphId);
            differences[code] = new PdfSimpleFontEncodingDifference(
                code,
                glyphId,
                glyphName);
            code += 1;
        }

        return new PdfSimpleFontEncodingArtifact(
            "WinAnsiEncoding",
            differences,
            PdfToUnicodeMappingBuilder.Build(usage, allocator));
    }

    static void ValidateSimpleSelection(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (options.GetFontKind() != PdfFontKind.SimpleTrueType)
        {
            throw new System.InvalidOperationException(
                "Simple-font encoding must be explicitly requested.");
        }

        if (instance.GetFace().GetOutlineKind() !=
            FontOutlineKind.TrueType)
        {
            throw new System.InvalidOperationException(
                "Simple PDF font encoding requires TrueType outlines.");
        }

        if (options.GetWritingMode() != PdfWritingMode.Horizontal)
        {
            throw new System.InvalidOperationException(
                "Simple PDF font encoding does not support vertical writing.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_1 &&
            PdfFontOptionResolver.RejectsVersionMismatch(options))
        {
            throw new System.InvalidOperationException(
                "Simple TrueType font embedding requires PDF 1.1 or later.");
        }
    }
}
