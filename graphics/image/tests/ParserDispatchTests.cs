using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ParserDispatchTests
{
    [Test]
    public void GetParserName_RoutesRepresentativeFormats()
    {
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Jpeg), Is.EqualTo("JpegMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Png), Is.EqualTo("PngMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.BigTiff), Is.EqualTo("TiffMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Cur), Is.EqualTo("IcoMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Svgz), Is.EqualTo("SvgMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Avif), Is.EqualTo("BmffMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Cr3), Is.EqualTo("RawMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Cr2), Is.EqualTo("RawMetadataParser"));
        Assert.That(ParserDispatch.GetParserName(ImageFormatKind.Unknown), Is.EqualTo("UnknownMetadataParser"));
    }

    [Test]
    public void Parse_DispatchesDetectedPngToPngParserAndCreatesBaseDocument()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        ImageFormatInfo format = new ImageFormatDetector().DetectBytes(stream.ToArray());

        ImageMetadataDocument document = ParserDispatch.Parse(reader, format, new ImageMetadataReaderOptions());

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/png"));
        Assert.That(document.ByteLength, Is.EqualTo(8));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.chunk.header.truncated"));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("PNG"));
    }

    [Test]
    public void Parse_CarriesReaderDiagnosticsIntoDocument()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MetadataDiagnostic.AddWarning(diagnostics, "reader.warning", "reader warning");
        MemoryStream stream = new MemoryStream(new byte[] { 255, 216, 255 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        ImageFormatInfo format = new ImageFormatDetector().DetectBytes(stream.ToArray());

        ImageMetadataDocument document = ParserDispatch.Parse(reader, format, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("reader.warning"));
        Assert.That(document.Diagnostics[1].Code, Is.EqualTo("jpeg.marker.missing"));
        Assert.That(document.Diagnostics[1].BlockType, Is.EqualTo("JPEG"));
    }

    [Test]
    public void Parse_RoutesBmffAndRawFamilies()
    {
        ImageMetadataDocument avif = ParserDispatch.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Avif), null);
        ImageMetadataDocument cr2 = ParserDispatch.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Cr2), null);

        Assert.That(avif.Format.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(avif.Diagnostics[0].BlockType, Is.EqualTo("BmffMetadataParser"));
        Assert.That(cr2.Format.Kind, Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(cr2.Diagnostics[0].Code, Is.EqualTo("tiff.reader.null"));
    }

    [Test]
    public void Parse_WithNullFormatUsesUnknownParser()
    {
        ImageMetadataDocument document = ParserDispatch.Parse(null, null, null);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(document.Diagnostics[0].BlockType, Is.EqualTo("UnknownMetadataParser"));
    }

    [Test]
    public void ParseWithParser_UsesInjectedParserWithoutFormatRouting()
    {
        FakeImageMetadataParser parser = new FakeImageMetadataParser();
        ImageFormatInfo format = ImageFormatMapping.CreateInfo(ImageFormatKind.Png);
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();

        ImageMetadataDocument document = ParserDispatch.ParseWithParser(null, format, options, parser);

        Assert.That(document, Is.SameAs(parser.Result));
        Assert.That(parser.ParseCalled, Is.True);
        Assert.That(parser.ReceivedFormat, Is.SameAs(format));
        Assert.That(parser.ReceivedOptions, Is.SameAs(options));
    }

    [Test]
    public void GetParser_ExposesOneContractForGroupedFormats()
    {
        IImageMetadataParser tiff = ParserDispatch.GetParser(ImageFormatKind.BigTiff);
        IImageMetadataParser raw = ParserDispatch.GetParser(ImageFormatKind.Raf);

        Assert.That(tiff.Supports(ImageFormatKind.Tiff), Is.True);
        Assert.That(tiff.GetName(), Is.EqualTo("TiffMetadataParser"));
        Assert.That(raw.Supports(ImageFormatKind.Cr3), Is.True);
        Assert.That(raw.GetName(), Is.EqualTo("RawMetadataParser"));
    }

    [Test]
    public void GetParser_MapsEveryKnownFormat()
    {
        foreach (ImageFormatKind kind in System.Enum.GetValues<ImageFormatKind>())
        {
            if (kind != ImageFormatKind.Unknown)
            {
                Assert.That(ParserDispatch.GetParserName(kind), Is.Not.EqualTo("UnknownMetadataParser"), kind.ToString());
            }
        }
    }
}

internal class FakeImageMetadataParser : IImageMetadataParser
{
    public bool ParseCalled;
    public ImageFormatInfo? ReceivedFormat;
    public ImageMetadataReaderOptions? ReceivedOptions;
    public ImageMetadataDocument Result;

    public FakeImageMetadataParser()
    {
        ParseCalled = false;
        Result = new ImageMetadataDocument();
    }

    public bool Supports(ImageFormatKind kind)
    {
        return true;
    }

    public ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ParseCalled = true;
        ReceivedFormat = format;
        ReceivedOptions = options;
        return Result;
    }

    public string GetName()
    {
        return "FakeImageMetadataParser";
    }
}
