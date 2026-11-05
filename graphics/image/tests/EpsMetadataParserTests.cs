using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class EpsMetadataParserTests
{
    [Test]
    public void Parse_ExtractsDscCommentsBoundingBoxesAndSummary()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%Title: (Sample\\(Art\\))\n%%Creator: Vector Tool 5\n%%For: Jane Doe\n%%CreationDate: 2026-08-12\n%%BoundingBox: -10 -20 190 80\n%%HiResBoundingBox: -9.5 -19.25 190.75 80.5\n%%Pages: 1\n%%LanguageLevel: 3\n%%Orientation: Portrait\n%%DocumentData: Clean7Bit\n%%DocumentFonts: Helvetica Times-Roman\n%%DocumentNeededResources: font Helvetica\n%%+ font Times-Roman\n%%EndComments\n%%Page: 1 1\nshowpage\n%%EOF\n";
        MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(eps));

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Eps));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Eps.DSC.Signature").Value.Text, Is.EqualTo("%!PS-Adobe-3.0 EPSF-3.0"));
        Assert.That(Find(document, "Eps.DSC.Title").Value.Text, Is.EqualTo("Sample(Art)"));
        Assert.That(Find(document, "Eps.DSC.Creator").Value.Text, Is.EqualTo("Vector Tool 5"));
        Assert.That(Find(document, "Eps.DSC.For").Value.Text, Is.EqualTo("Jane Doe"));
        Assert.That(Find(document, "Eps.DSC.CreationDate").Value.Kind, Is.EqualTo(MetadataValueKind.DateText));
        Assert.That(Find(document, "Eps.DSC.Pages").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(Find(document, "Eps.DSC.LanguageLevel").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(Find(document, "Eps.DSC.DocumentFonts").Value.TextValues, Is.EqualTo(new[] { "Helvetica", "Times-Roman" }));
        Assert.That(Find(document, "Eps.DSC.DocumentNeededResources").Value.TextValues, Is.EqualTo(new[] { "font", "Helvetica", "font", "Times-Roman" }));
        Assert.That(Find(document, "Eps.DSC.BoundingBox.Width").Value.DoubleValue, Is.EqualTo(200));
        Assert.That(Find(document, "Eps.DSC.BoundingBox.Height").Value.DoubleValue, Is.EqualTo(100));
        Assert.That(Find(document, "Eps.DSC.HiResBoundingBox.Width").Value.DoubleValue, Is.EqualTo(200.25).Within(0.00001));
        Assert.That(Find(document, "Eps.DSC.HiResBoundingBox.Height").Value.DoubleValue, Is.EqualTo(99.75).Within(0.00001));
        Assert.That(document.Directories.Single().Path, Is.EqualTo("EPS/DSC"));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(201));
        Assert.That(document.Frames[0].Height, Is.EqualTo(100));
        Assert.That(document.Frames[0].Compression, Is.EqualTo(ImageCompressionKind.None));
        Assert.That(document.Summary.Title, Is.EqualTo("Sample(Art)"));
        Assert.That(document.Summary.Author, Is.EqualTo("Jane Doe"));
        Assert.That(document.Summary.Software, Is.EqualTo("Vector Tool 5"));
        Assert.That(document.Summary.CaptureTimeText, Is.EqualTo("2026-08-12"));
        Assert.That(document.RawBlocks.Single().BlockType, Is.EqualTo("DSCHeader"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Parse_UsesTrailerValuesForAtEndComments()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%Title: (atend)\n%%BoundingBox: (atend)\n%%Pages: (atend)\n%%EndComments\nshowpage\n%%Trailer\n%%Title: Final Title\n%%BoundingBox: 5 10 105 210\n%%Pages: 2\n%%EOF\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.ASCII.GetBytes(eps)));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Eps.DSC.Title").Value.Text, Is.EqualTo("Final Title"));
        Assert.That(Find(document, "Eps.DSC.Pages").Value.IntegerValue, Is.EqualTo(2));
        Assert.That(Find(document, "Eps.DSC.BoundingBox.Width").Value.DoubleValue, Is.EqualTo(100));
        Assert.That(document.Frames[0].Width, Is.EqualTo(100));
        Assert.That(document.Frames[0].Height, Is.EqualTo(200));
        MetadataTag trailerTitle = document.Directories.Single().Tags.Last(item => item.Key == "Eps.DSC.Comment.Title");
        Assert.That(trailerTitle.DisplayValue, Is.EqualTo("Trailer: Final Title"));
        Assert.That(trailerTitle.Confidence, Is.EqualTo(95));
    }

    [Test]
    public void Parse_ExtractsEmbeddedXmpAndNormalizesItsFields()
    {
        string packet = XmpPacket("XMP Title", "XMP Tool");
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%Title: DSC Title\n%%BoundingBox: 0 0 10 20\n%ADO_ContainsXMP: MainFirst\n%%EndComments\n" + packet + "\nshowpage\n%%EOF\n";
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(Encoding.UTF8.GetBytes(eps)), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Eps.XMP.LocationHint").Value.Text, Is.EqualTo("MainFirst"));
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.EPS"));
        Assert.That(document.Profiles[0].Text, Does.Contain("XMP Title"));
        Assert.That(Find(document, "Xmp.dc.title").Value.Text, Is.EqualTo("XMP Title"));
        Assert.That(Find(document, "Xmp.xmp.CreatorTool").Value.Text, Is.EqualTo("XMP Tool"));
        Assert.That(document.Summary.Title, Is.EqualTo("XMP Title"));
        Assert.That(document.Summary.Software, Is.EqualTo("XMP Tool"));
        Assert.That(document.RawBlocks.Single(item => item.BlockType == "XMP").Data, Is.EqualTo(document.Profiles[0].Data));
    }

    [Test]
    public void Parse_MainFirstAndMainLastHintsChooseExpectedPacket()
    {
        string first = XmpPacket("First", "Tool One");
        string last = XmpPacket("Last", "Tool Two");
        string prefix = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%ADO_ContainsXMP: ";
        string suffix = "\n%%EndComments\n" + first + "\n" + last + "\n%%EOF\n";

        ImageMetadataDocument firstDocument = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.UTF8.GetBytes(prefix + "MainFirst" + suffix)));
        ImageMetadataDocument lastDocument = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.UTF8.GetBytes(prefix + "MainLast" + suffix)));

        Assert.That(Find(firstDocument, "Xmp.dc.title").Value.Text, Is.EqualTo("First"));
        Assert.That(Find(lastDocument, "Xmp.dc.title").Value.Text, Is.EqualTo("Last"));
        Assert.That(firstDocument.Profiles, Has.Count.EqualTo(1));
        Assert.That(lastDocument.Profiles, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_NoMainHintDoesNotExtractObjectPacket()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%ADO_ContainsXMP: NoMain\n%%EndComments\n" + XmpPacket("Object", "Tool") + "\n%%EOF\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.UTF8.GetBytes(eps)));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Xmp.dc.title"), Is.Null);
    }

    [Test]
    public void Parse_MissingOrTruncatedDeclaredXmpIsDiagnosed()
    {
        string missing = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%ADO_ContainsXMP: MainFirst\n%%EndComments\nshowpage\n";
        ImageMetadataDocument missingDocument = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.ASCII.GetBytes(missing)));
        Assert.That(HasDiagnostic(missingDocument, "eps.xmp.packet.missing"), Is.True);

        string truncated = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%ADO_ContainsXMP: MainFirst\n%%EndComments\n<?xpacket begin='x'?><x:xmpmeta xmlns:x='adobe:ns:meta/'>";
        ImageMetadataDocument truncatedDocument = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.ASCII.GetBytes(truncated)));
        Assert.That(HasDiagnostic(truncatedDocument, "eps.xmp.packet.truncated"), Is.True);
        Assert.That(truncatedDocument.Profiles, Is.Empty);
    }

    [Test]
    public void Parse_XmpOptionsAndLimitsAreHonored()
    {
        string packet = XmpPacket("Title", "Tool");
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%ADO_ContainsXMP: MainFirst\n%%EndComments\n" + packet;
        byte[] bytes = Encoding.UTF8.GetBytes(eps);
        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.ParseXmp = false;
        Assert.That(ParseDirect(bytes, disabled).Profiles, Is.Empty);

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.MaxProfileBytes = 20;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(HasDiagnostic(limitedDocument, "eps.xmp.profile.limit"), Is.True);
        Assert.That(limitedDocument.Profiles, Is.Empty);
    }

    [Test]
    public void Parse_BinaryPreviewWrapperResolvesPostScriptSection()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%Title: Wrapped\n%%BoundingBox: 0 0 40 30\n%%EndComments\nshowpage\n";
        byte[] postScript = Encoding.ASCII.GetBytes(eps);
        byte[] bytes = CreateBinaryWrapper(postScript, new byte[] { 1, 2, 3, 4 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Eps));
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Directories.Select(item => item.Path), Is.EqualTo(new[] { "EPS/BinaryHeader", "EPS/DSC" }));
        Assert.That(Find(document, "Eps.Binary.PostScriptOffset").Value.IntegerValue, Is.EqualTo(30));
        Assert.That(Find(document, "Eps.Binary.PostScriptLength").Value.IntegerValue, Is.EqualTo(postScript.Length));
        Assert.That(Find(document, "Eps.Binary.TiffLength").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(Find(document, "Eps.Binary.Checksum").DisplayValue, Is.EqualTo("Not provided"));
        Assert.That(Find(document, "Eps.DSC.Title").Value.Text, Is.EqualTo("Wrapped"));
        Assert.That(document.Frames[0].Width, Is.EqualTo(40));
        Assert.That(document.RawBlocks.Select(item => item.BlockType), Is.EqualTo(new[] { "BinaryHeader", "DSCHeader" }));
    }

    [Test]
    public void Parse_InvalidBinaryPostScriptRangeIsDiagnosed()
    {
        byte[] bytes = new byte[30];
        WriteUnsigned32LittleEndian(bytes, 0, 3335770309);
        WriteUnsigned32LittleEndian(bytes, 4, 100);
        WriteUnsigned32LittleEndian(bytes, 8, 20);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(HasDiagnostic(document, "eps.binary.postscript.range"), Is.True);
        Assert.That(document.Directories, Is.Empty);
    }

    [Test]
    public void Parse_SkipsDeclaredBinaryAndLineDataBlocks()
    {
        string prefix = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%%EndComments\n%%BeginBinary: 21\n";
        string fakeBinary = "%%Title: Not Binary X";
        Assert.That(Encoding.ASCII.GetByteCount(fakeBinary), Is.EqualTo(21));
        string middle = "\n%%BeginData: 1 Lines\n%%Creator: Not Creator\n%%Title: Real Title\n%%EOF\n";
        byte[] bytes = Encoding.ASCII.GetBytes(prefix + fakeBinary + middle);

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(bytes));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(Find(document, "Eps.DSC.Title").Value.Text, Is.EqualTo("Real Title"));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Eps.DSC.Creator"), Is.Null);
    }

    [Test]
    public void Parse_MalformedDscValuesWarnAndPreserveOtherMetadata()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 10 10 5 5\n%%HiResBoundingBox: invalid\n%%Pages: many\n%%LanguageLevel: zero\n%%EndComments\n%%EOF\n";

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(Encoding.ASCII.GetBytes(eps)));

        Assert.That(HasDiagnostic(document, "eps.dsc.bounding-box.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "eps.dsc.hires-bounding-box.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "eps.dsc.pages.invalid"), Is.True);
        Assert.That(HasDiagnostic(document, "eps.dsc.languagelevel.invalid"), Is.True);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_InvalidSignatureAndMissingEpsfAreDiagnosed()
    {
        byte[] invalid = Encoding.ASCII.GetBytes("not eps at all");
        ImageMetadataDocument invalidDocument = ParseDirect(invalid, new ImageMetadataReaderOptions());
        Assert.That(HasDiagnostic(invalidDocument, "eps.signature.invalid"), Is.True);

        byte[] postScript = Encoding.ASCII.GetBytes("%!PS-Adobe-3.0\n%%BoundingBox: 0 0 10 10\n%%EndComments\nshowpage\n");
        ImageMetadataDocument postScriptDocument = ParseDirect(postScript, new ImageMetadataReaderOptions());
        Assert.That(HasDiagnostic(postScriptDocument, "eps.signature.epsf.missing"), Is.True);
        Assert.That(postScriptDocument.Frames, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_DscTagAndSourceLimitsAreHonored()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%Title: One\n%%Creator: Two\n%%BoundingBox: 0 0 10 10\n%%EndComments\n";
        byte[] bytes = Encoding.ASCII.GetBytes(eps);
        ImageMetadataReaderOptions tagLimited = new ImageMetadataReaderOptions();
        tagLimited.MaxTagCount = 1;
        ImageMetadataDocument tagDocument = ParseDirect(bytes, tagLimited);
        Assert.That(HasDiagnostic(tagDocument, "eps.dsc.tag.limit"), Is.True);
        Assert.That(tagDocument.Directories, Has.Count.EqualTo(1));

        ImageMetadataReaderOptions sourceLimited = new ImageMetadataReaderOptions();
        sourceLimited.MaxSegmentBytes = 20;
        ImageMetadataDocument sourceDocument = ParseDirect(bytes, sourceLimited);
        Assert.That(HasDiagnostic(sourceDocument, "eps.source.limit"), Is.True);
        Assert.That(sourceDocument.Directories, Is.Empty);
    }

    [Test]
    public void Parse_RawBlockCaptureHonorsOptionsAndLimit()
    {
        string eps = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 10 10\n%%EndComments\nshowpage\n";
        byte[] bytes = Encoding.ASCII.GetBytes(eps);
        ImageMetadataReaderOptions captured = new ImageMetadataReaderOptions();
        captured.CaptureRawBlockData = true;
        ImageMetadataDocument capturedDocument = ParseDirect(bytes, captured);
        Assert.That(capturedDocument.RawBlocks.Single().Data.Length, Is.GreaterThan(40));

        ImageMetadataReaderOptions limited = new ImageMetadataReaderOptions();
        limited.CaptureRawBlockData = true;
        limited.MaxRawBlockBytes = 20;
        ImageMetadataDocument limitedDocument = ParseDirect(bytes, limited);
        Assert.That(limitedDocument.RawBlocks.Single().Data, Is.Empty);
        Assert.That(HasDiagnostic(limitedDocument, "eps.raw.block.limit"), Is.True);

        ImageMetadataReaderOptions disabled = new ImageMetadataReaderOptions();
        disabled.CaptureRawBlocks = false;
        Assert.That(ParseDirect(bytes, disabled).RawBlocks, Is.Empty);
    }

    [Test]
    public void Parse_NullReaderReportsDiagnostic()
    {
        ImageMetadataDocument document = EpsMetadataParser.Parse(null, ImageFormatMapping.CreateInfo(ImageFormatKind.Eps), new ImageMetadataReaderOptions());

        Assert.That(HasDiagnostic(document, "eps.reader.null"), Is.True);
        Assert.That(document.Frames, Is.Empty);
    }

    public static string XmpPacket(string title, string tool)
    {
        return "<?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>" +
            "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>" +
            "<rdf:Description rdf:about='' xmlns:dc='http://purl.org/dc/elements/1.1/' xmlns:xmp='http://ns.adobe.com/xap/1.0/'>" +
            "<dc:title><rdf:Alt><rdf:li xml:lang='x-default'>" + title + "</rdf:li></rdf:Alt></dc:title>" +
            "<xmp:CreatorTool>" + tool + "</xmp:CreatorTool></rdf:Description></rdf:RDF></x:xmpmeta>" +
            "<?xpacket end='w'?>";
    }

    public static byte[] CreateBinaryWrapper(byte[] postScript, byte[] tiff)
    {
        byte[] bytes = new byte[30 + postScript.Length + tiff.Length];
        WriteUnsigned32LittleEndian(bytes, 0, 3335770309);
        WriteUnsigned32LittleEndian(bytes, 4, 30);
        WriteUnsigned32LittleEndian(bytes, 8, postScript.Length);
        WriteUnsigned32LittleEndian(bytes, 20, 30 + postScript.Length);
        WriteUnsigned32LittleEndian(bytes, 24, tiff.Length);
        bytes[28] = 255;
        bytes[29] = 255;
        for (int index = 0; index < postScript.Length; index++) bytes[30 + index] = postScript[index];
        for (int index = 0; index < tiff.Length; index++) bytes[30 + postScript.Length + index] = tiff[index];
        return bytes;
    }

    public static void WriteUnsigned32LittleEndian(byte[] bytes, int offset, long value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    public static ImageMetadataDocument ParseDirect(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return EpsMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Eps), options);
    }

    public static MetadataTag Find(ImageMetadataDocument document, string key)
    {
        return RequiredMetadataQuery.FindFirst(document, key);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        return document.Diagnostics.Any(item => item.Code == code);
    }
}
