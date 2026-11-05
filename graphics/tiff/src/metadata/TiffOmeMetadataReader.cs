using System;

namespace tiff;

internal class TiffOmeMetadataReader
{
    internal static TiffOmeMetadataReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read OME metadata.");
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.ImageDescription);
        if (entry == null)
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageDescription must use ASCII data.", directory.ImageIndex, TiffTagId.ImageDescription);
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, new TiffLimits());
        diagnostics.Merge(loadResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        string description = TiffOmeUtf8.Decode(loadResult.Bytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        string xml = ExtractOmeXml(description);
        if (xml.Length == 0)
        {
            return TiffOmeMetadataReadResult.Create(TiffOmeMetadata.Create(false, "", description, loadResult.Bytes, false, false, "", new TiffOmeImage[0], new TiffOmeFileReference[0], new TiffOmePyramidLevel[0]), diagnostics);
        }

        TiffOmeMetadata metadata = TiffOmeXmlParser.Parse(xml, description, loadResult.Bytes, new TiffLimits(), diagnostics);
        return TiffOmeMetadataReadResult.Create(metadata, diagnostics);
    }

    internal static TiffOmeMetadataReadResult ReadSource(TiffDirectory directory, ITiffRandomAccessSource source, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null || source == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory and source are required to read OME metadata.");
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.ImageDescription);
        if (entry == null)
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageDescription must use ASCII data.", directory.ImageIndex, TiffTagId.ImageDescription);
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        string description = TiffOmeUtf8.Decode(loadResult.Bytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        string xml = ExtractOmeXml(description);
        if (xml.Length == 0)
        {
            TiffOmeMetadata plain = TiffOmeMetadata.Create(false, "", description, loadResult.Bytes, false, false, "", new TiffOmeImage[0], new TiffOmeFileReference[0], new TiffOmePyramidLevel[0]);
            return TiffOmeMetadataReadResult.Create(plain, diagnostics);
        }

        TiffOmeMetadata metadata = TiffOmeXmlParser.Parse(xml, description, loadResult.Bytes, limits, diagnostics);
        return TiffOmeMetadataReadResult.Create(metadata, diagnostics);
    }

    internal static bool IsOmeXml(string text)
    {
        return ExtractOmeXml(text).Length > 0;
    }

    internal static string ExtractOmeXml(string text)
    {
        string normalized = TiffOmeMetadata.NormalizeString(text);
        int start = SkipWhitespace(normalized, 0);
        int contentStart = start;
        if (StartsWithAt(normalized, start, "<?xml"))
        {
            int declarationEnd = FindXmlDeclarationEnd(normalized, start);
            if (declarationEnd < 0)
            {
                return "";
            }

            contentStart = declarationEnd;
        }

        int rootStart = SkipWhitespaceAndComments(normalized, contentStart);
        if (rootStart >= 0 && IsOmeRootStart(normalized, rootStart))
        {
            return normalized.Substring(start);
        }

        return "";
    }

    internal static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static int SkipWhitespaceAndComments(string text, int start)
    {
        int index = SkipWhitespace(text, start);
        while (StartsWithAt(text, index, "<!--"))
        {
            int commentEnd = FindXmlCommentEnd(text, index + 4);
            if (commentEnd < 0)
            {
                return -1;
            }

            index = SkipWhitespace(text, commentEnd);
        }

        return index;
    }

    internal static int FindXmlCommentEnd(string text, int start)
    {
        int index = start;
        while (index + 2 < text.Length)
        {
            if (text[index] == '-' && text[index + 1] == '-' && text[index + 2] == '>')
            {
                return index + 3;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindXmlDeclarationEnd(string text, int start)
    {
        int index = start;
        while (index + 1 < text.Length)
        {
            if (text[index] == '?' && text[index + 1] == '>')
            {
                return index + 2;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsOmeRootStart(string text, int start)
    {
        if (!StartsWithAt(text, start, "<OME"))
        {
            return false;
        }

        int next = start + 4;
        if (next >= text.Length)
        {
            return false;
        }

        return text[next] == '>' || text[next] == '/' || char.IsWhiteSpace(text[next]);
    }

    internal static bool StartsWithAt(string text, int start, string prefix)
    {
        if (text == null || prefix == null)
        {
            return false;
        }

        if (start < 0 || start + prefix.Length > text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (text[start + index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
