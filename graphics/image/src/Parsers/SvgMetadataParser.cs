namespace image;

internal class SvgMetadataParser
{
    internal const string SvgNamespace = "http://www.w3.org/2000/svg";

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "svg.reader.null", "SVG parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (MaxSegmentBytes(options) < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.segment.limit.negative", "SVG segment limit was negative.", "SVG", 0, MaxSegmentBytes(options));
            Finish(document, reader);
            return document;
        }

        if (MaxXmlBytes(options) < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.xml.limit.negative", "SVG XML byte limit was negative.", "SVG", 0, MaxXmlBytes(options));
            Finish(document, reader);
            return document;
        }

        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.seek", "SVG source could not be reached.", "SVG", 0, document.ByteLength);
            Finish(document, reader);
            return document;
        }

        long sourceLength = reader.Remaining();
        if (sourceLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.source.large", "SVG source was too large to buffer.", "SVG", 0, sourceLength);
            Finish(document, reader);
            return document;
        }

        if (sourceLength > MaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.source.limit", "SVG source exceeded the configured segment limit.", "SVG", 0, sourceLength);
            Finish(document, reader);
            return document;
        }

        byte[] source = reader.ReadBytes((int)sourceLength);
        bool compressed = IsGzip(source) || document.Format.Kind == ImageFormatKind.Svgz;
        document.Format = ImageFormatMapping.CreateInfo(compressed ? ImageFormatKind.Svgz : ImageFormatKind.Svg);
        AddRawBlock(document, options, compressed ? "Gzip" : "XML", compressed ? "Compressed SVG Document" : "SVG XML Document", source);

        byte[] xmlBytes = source;
        if (compressed)
        {
            if (!IsGzip(source))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svgz.signature.invalid", "SVGZ source did not contain a gzip signature.", "SVGZ", 0, PrefixLength(source, 2));
                Finish(document, reader);
                return document;
            }

            GzipDecompressionResult result = GzipHelper.Decompress(source, DecompressedLimit(options), document.Diagnostics, "SVGZ");
            if (!result.Complete)
            {
                Finish(document, reader);
                return document;
            }

            xmlBytes = result.Data;
        }
        else if ((long)xmlBytes.Length > MaxXmlBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.xml.limit", "SVG XML exceeded the configured XML byte limit.", "SVG", 0, xmlBytes.Length);
            Finish(document, reader);
            return document;
        }

        string xmlText = MetadataXmlReader.DecodeXmlBytes(xmlBytes, MaxXmlBytes(options), document.Diagnostics, compressed ? "SVGZ" : "SVG");
        if (xmlText.Length == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.xml.empty", "SVG XML document was empty.", "SVG", 0, xmlBytes.Length);
            Finish(document, reader);
            return document;
        }

        MetadataXmlRoot root = MetadataXmlReader.ReadRoot(xmlText, MaxXmlCharacters(options), document.Diagnostics, compressed ? "SVGZ" : "SVG");
        if (!root.Found)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.root.missing", "SVG document did not contain a root element.", "SVG", 0, xmlBytes.Length);
            Finish(document, reader);
            return document;
        }

        AddDocumentDirectory(document, root, source.Length, xmlBytes.Length, compressed, EncodingName(xmlBytes));
        if (root.LocalName != "svg")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.root.invalid", "SVG root element was not svg.", "SVG", 0, xmlBytes.Length);
        }
        else if (root.NamespaceUri.Length == 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "svg.namespace.missing", "SVG root element did not declare the SVG namespace.", "SVG", 0, xmlBytes.Length);
        }
        else if (root.NamespaceUri != SvgNamespace)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "svg.namespace.invalid", "SVG root element used an unexpected namespace.", "SVG", 0, xmlBytes.Length);
        }

        if (root.LocalName == "svg" && (root.NamespaceUri.Length == 0 || root.NamespaceUri == SvgNamespace))
        {
            SvgMetadataExtractor.Extract(document, xmlText, source.Length, xmlBytes.Length, options);
        }

        Finish(document, reader);
        return document;
    }

    internal static void AddDocumentDirectory(ImageMetadataDocument document, MetadataXmlRoot root, long sourceLength, long xmlLength, bool compressed, string encoding)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Svg, "Document", compressed ? "SVGZ Document" : "SVG Document");
        directory.Path = "SVG/Document";
        directory.Offset = 0;
        directory.Length = sourceLength;
        AddTag(directory, "Svg.Document.Compressed", "Gzip Compressed", MetadataValue.FromBoolean(compressed), 0, sourceLength, compressed ? "Gzip" : "Uncompressed");
        AddTag(directory, "Svg.Document.SourceByteLength", "Source Byte Length", MetadataValue.FromInteger(sourceLength), 0, sourceLength, "");
        AddTag(directory, "Svg.Document.XmlByteLength", "XML Byte Length", MetadataValue.FromInteger(xmlLength), 0, sourceLength, "");
        AddTag(directory, "Svg.Document.Encoding", "XML Encoding", MetadataValue.FromString(encoding), 0, PrefixLengthValue(xmlLength, 3), encoding);
        AddTag(directory, "Svg.Document.RootName", "Root Element", MetadataValue.FromString(root.LocalName), 0, xmlLength, root.LocalName);
        AddTag(directory, "Svg.Document.Namespace", "Root Namespace", MetadataValue.FromString(root.NamespaceUri), 0, xmlLength, root.NamespaceUri);
        AddTag(directory, "Svg.Document.Prefix", "Root Prefix", MetadataValue.FromString(root.Prefix), 0, xmlLength, root.Prefix);
    }

    internal static string EncodingName(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            return "UTF-8";
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return "UTF-16LE";
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return "UTF-16BE";
        }

        return "UTF-8";
    }

    internal static bool IsGzip(byte[] bytes)
    {
        return bytes != null && bytes.Length >= 2 && bytes[0] == 31 && bytes[1] == 139;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void AddRawBlock(ImageMetadataDocument document, ImageMetadataReaderOptions options, string blockType, string name, byte[] data)
    {
        if (!BmpHeaderParser.CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Svg, blockType, name, 0, data.Length);
        if (BmpHeaderParser.CaptureRawBlockData(options))
        {
            if ((long)data.Length > BmpHeaderParser.MaxRawBlockBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "svg.raw-block.limit", "SVG source exceeded the configured raw block limit.", blockType, 0, data.Length);
            }
            else
            {
                block.Data = BmpHeaderParser.CopyBytes(data, 0, data.Length);
            }
        }
    }

    internal static long DecompressedLimit(ImageMetadataReaderOptions options)
    {
        long xmlLimit = MaxXmlBytes(options);
        long decompressedLimit = MaxDecompressedBytes(options);
        if (xmlLimit < decompressedLimit)
        {
            return xmlLimit;
        }

        return decompressedLimit;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long MaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long MaxXmlCharacters(ImageMetadataReaderOptions options)
    {
        long bytes = MaxXmlBytes(options);
        if (bytes > 4611686018427387903)
        {
            return 9223372036854775807;
        }

        return bytes * 2;
    }

    internal static long MaxDecompressedBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxDecompressedBytes : options.MaxDecompressedBytes;
    }

    internal static int PrefixLength(byte[] data, int maximum)
    {
        return data.Length < maximum ? data.Length : maximum;
    }

    internal static long PrefixLengthValue(long length, int maximum)
    {
        return length < maximum ? length : maximum;
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        TiffHeaderParser.CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}
