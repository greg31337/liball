using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class SvgMetadataParserTests
{
    [Test]
    public void Parse_PlainSvgValidatesRootAndCreatesDocumentMetadata()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><svg xmlns=\"http://www.w3.org/2000/svg\"><g/></svg>");

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svg));
        Assert.That(document.Format.Name, Is.EqualTo("SVG"));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(document.Directories[0].Family, Is.EqualTo(MetadataFamily.Svg));
        Assert.That(document.Directories[0].Group, Is.EqualTo("Document"));
        Assert.That(document.Directories[0].Path, Is.EqualTo("SVG/Document"));
        Assert.That(document.Directories[0].Length, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Compressed").Value.BooleanValue, Is.False);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.SourceByteLength").Value.IntegerValue, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.XmlByteLength").Value.IntegerValue, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Encoding").Value.Text, Is.EqualTo("UTF-8"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.RootName").Value.Text, Is.EqualTo("svg"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Namespace").Value.Text, Is.EqualTo(SvgMetadataParser.SvgNamespace));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("XML"));
    }

    [Test]
    public void Parse_SvgzDecompressesWithinLimitsAndReportsSizes()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><path/></svg>");
        byte[] gzip = Compress(xml);

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svgz, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svgz));
        Assert.That(document.Format.Name, Is.EqualTo("SVGZ"));
        Assert.That(document.ByteLength, Is.EqualTo(gzip.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Compressed").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.SourceByteLength").Value.IntegerValue, Is.EqualTo(gzip.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.XmlByteLength").Value.IntegerValue, Is.EqualTo(xml.Length));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("Gzip"));
        Assert.That(document.RawBlocks[0].Length, Is.EqualTo(gzip.Length));
    }

    [Test]
    public void Parse_GzipMagicOverridesPlainSvgFormatHint()
    {
        byte[] gzip = Compress(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svgz));
    }

    [Test]
    public void Parse_Utf16BomIsDecodedAndReported()
    {
        byte[] text = Encoding.Unicode.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        byte[] bytes = new byte[text.Length + 2];
        bytes[0] = 255;
        bytes[1] = 254;
        System.Array.Copy(text, 0, bytes, 2, text.Length);

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.Encoding").Value.Text, Is.EqualTo("UTF-16LE"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.RootName").Value.Text, Is.EqualTo("svg"));
    }

    [Test]
    public void Parse_ExternalDtdIsIgnoredWithoutResolvingResource()
    {
        string xml = "<!DOCTYPE svg SYSTEM \"file:///definitely/missing/external.dtd\"><svg xmlns=\"http://www.w3.org/2000/svg\"/>";

        ImageMetadataDocument document = Parse(Encoding.UTF8.GetBytes(xml), ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Has.Count.EqualTo(1));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.RootName").Value.Text, Is.EqualTo("svg"));
        AssertDiagnostic(document, "xml.dtd.ignored", MetadataSeverity.Unsupported);
    }

    [Test]
    public void Parse_InvalidRootIsPreservedWithDiagnostic()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<html xmlns=\"http://www.w3.org/1999/xhtml\"/>");

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(RequiredMetadataQuery.FindFirst(document, "Svg.Document.RootName").Value.Text, Is.EqualTo("html"));
        AssertDiagnostic(document, "svg.root.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_UnbalancedElementsReturnDiagnosticInsteadOfParsingPartialXml()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><g></svg>");

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "xml.malformed", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "svg.root.missing", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_MissingAndInvalidNamespacesAddDiagnostics()
    {
        ImageMetadataDocument missing = Parse(Encoding.UTF8.GetBytes("<svg/>"), ImageFormatKind.Svg, new ImageMetadataReaderOptions());
        ImageMetadataDocument invalid = Parse(Encoding.UTF8.GetBytes("<svg xmlns=\"urn:not-svg\"/>"), ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        AssertDiagnostic(missing, "svg.namespace.missing", MetadataSeverity.Warning);
        AssertDiagnostic(invalid, "svg.namespace.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_UncompressedXmlLimitStopsBeforeXmlParsing()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxXmlBytes = 10;

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svg, options);

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "svg.xml.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_SvgzDecompressedLimitStopsBeforeXmlParsing()
    {
        byte[] gzip = Compress(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><g/></svg>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxDecompressedBytes = 12;

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svgz, options);

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "gzip.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_CompressedSourceLimitStopsBeforeBuffering()
    {
        byte[] gzip = Compress(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxSegmentBytes = gzip.Length - 1;

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svgz, options);

        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        AssertDiagnostic(document, "svg.source.limit", MetadataSeverity.LimitExceeded);
    }

    [Test]
    public void Parse_SvgzHintWithoutGzipSignatureIsRejected()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svgz, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Svgz));
        AssertDiagnostic(document, "svgz.signature.invalid", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_TruncatedGzipIsReportedWithoutDecompression()
    {
        byte[] bytes = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 0, 3 };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Svgz, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "gzip.truncated", MetadataSeverity.Truncated);
    }

    [Test]
    public void Parse_EmptySvgAddsDiagnostic()
    {
        ImageMetadataDocument document = Parse(new byte[0], ImageFormatKind.Svg, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "svg.xml.empty", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void Parse_RawCaptureRetainsOriginalCompressedBytes()
    {
        byte[] gzip = Compress(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svgz, options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(document.RawBlocks[0].Data, Is.EqualTo(gzip));
    }

    [Test]
    public void Parse_NullReaderReturnsDiagnosticDocument()
    {
        ImageMetadataDocument document = SvgMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Svg), new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "svg.reader.null", MetadataSeverity.RecoverableError);
    }

    [Test]
    public void GzipValidationRejectsReservedFlags()
    {
        byte[] gzip = Compress(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));
        gzip[3] = 224;

        ImageMetadataDocument document = Parse(gzip, ImageFormatKind.Svgz, new ImageMetadataReaderOptions());

        Assert.That(document.Directories, Is.Empty);
        AssertDiagnostic(document, "gzip.flags.invalid", MetadataSeverity.RecoverableError);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageFormatKind kind, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return SvgMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
    }

    public static byte[] Compress(byte[] bytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }
    }

    public static MetadataDiagnostic FindDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return diagnostic;
            }
        }

        throw new System.InvalidOperationException("Expected diagnostic was not found: " + code);
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        MetadataDiagnostic diagnostic = FindDiagnostic(document, code);
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic.Severity, Is.EqualTo(severity));
    }
}
