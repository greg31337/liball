using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class GifMetadataExtractorTests
{
    [Test]
    public void Parse_ExtractsConcatenatedCommentTextAndRawBytes()
    {
        List<byte> bytes = CreateGif();
        AddBytes(bytes, new byte[] { 33, 254, 5 });
        AddBytes(bytes, Encoding.ASCII.GetBytes("hello"));
        AddBytes(bytes, new byte[] { 6 });
        AddBytes(bytes, Encoding.ASCII.GetBytes(" world"));
        AddBytes(bytes, new byte[] { 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        MetadataTag comment = RequiredMetadataQuery.FindFirst(document, "Gif.Comment.Text");
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(comment.Value.Text, Is.EqualTo("hello world"));
        Assert.That(comment.RawValue, Is.EqualTo(Encoding.ASCII.GetBytes("hello world")));
        Assert.That(comment.DisplayValue, Is.EqualTo("hello world"));
    }

    [Test]
    public void Parse_ExtractsGenericApplicationIdentifierAndData()
    {
        List<byte> bytes = CreateGif();
        AddApplicationHeader(bytes, "TESTAPP1abc");
        AddBytes(bytes, new byte[] { 2, 1, 2, 3, 3, 4, 5, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        MetadataTag identifier = RequiredMetadataQuery.FindFirst(document, "Gif.Application.Identifier");
        MetadataTag authentication = RequiredMetadataQuery.FindFirst(document, "Gif.Application.AuthenticationCode");
        Assert.That(identifier.Value.Text, Is.EqualTo("TESTAPP1"));
        Assert.That(identifier.RawValue, Is.EqualTo(Encoding.ASCII.GetBytes("TESTAPP1")));
        Assert.That(authentication.Value.Text, Is.EqualTo("abc"));
        Assert.That(authentication.RawValue, Is.EqualTo(Encoding.ASCII.GetBytes("abc")));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Application.IdentifierAndAuthenticationCode").Value.Text, Is.EqualTo("TESTAPP1abc"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Application.Data").Value.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Parse_AppliesGraphicControlsToFollowingFramesAndSetsAnimationStatus()
    {
        List<byte> bytes = CreateGif();
        AddGraphicControl(bytes, 11, 25, 1);
        AddBytes(bytes, new byte[] { 33, 254, 1, 120, 0 });
        AddImage(bytes);
        AddGraphicControl(bytes, 4, 5, 0);
        AddImage(bytes);
        bytes.Add(59);

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Frames, Has.Count.EqualTo(2));
        Assert.That(document.Frames[0].DurationMilliseconds, Is.EqualTo(250));
        Assert.That(document.Frames[0].HasAlpha, Is.True);
        Assert.That(document.Frames[1].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(document.Frames[1].HasAlpha, Is.False);
        Assert.That(document.Frames[0].IsAnimated, Is.True);
        Assert.That(document.Frames[1].IsAnimated, Is.True);
        Assert.That(document.Summary.IsAnimated, Is.True);
        Assert.That(document.Summary.TotalDurationMilliseconds, Is.EqualTo(300));
        Assert.That(new ImageMetadataQuery().Find(document, "Gif.GraphicControl.DisposalMethod")[0].Value.IntegerValue, Is.EqualTo(2));
        Assert.That(new ImageMetadataQuery().Find(document, "Gif.GraphicControl.DelayMilliseconds")[0].Value.IntegerValue, Is.EqualTo(250));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.GraphicControl.TransparencyFlag").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Animation.IsAnimated").Value.BooleanValue, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Animation.FrameCount").Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ExtractsNetscapeLoopCountAndMarksLoopingImageAnimated()
    {
        List<byte> bytes = CreateGif();
        AddApplicationHeader(bytes, "NETSCAPE2.0");
        AddBytes(bytes, new byte[] { 3, 1, 7, 0, 0 });
        AddImage(bytes);
        bytes.Add(59);

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Application.LoopCount").Value.IntegerValue, Is.EqualTo(7));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Animation.LoopCount").Value.IntegerValue, Is.EqualTo(7));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Animation.IsAnimated").Value.BooleanValue, Is.True);
        Assert.That(document.Frames[0].IsAnimated, Is.True);
        Assert.That(document.Summary.IsAnimated, Is.True);
        Assert.That(document.Summary.AnimationLoopCount, Is.EqualTo(7));
    }

    [Test]
    public void Parse_PreservesXmpPacketWithoutMagicTrailer()
    {
        byte[] packet = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x='adobe:ns:meta/'/>");
        List<byte> bytes = CreateGif();
        AddApplicationHeader(bytes, "XMP DataXMP");
        AddBytes(bytes, packet);
        AddXmpMagicTrailer(bytes);
        bytes.Add(59);

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Xmp));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Xmp));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Xmp.GIF.ApplicationExtension"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/rdf+xml"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(packet));
        Assert.That(document.Profiles[0].Text, Is.EqualTo(Encoding.UTF8.GetString(packet)));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Application.XmpPacketByteLength").Value.IntegerValue, Is.EqualTo(packet.Length));
    }

    [Test]
    public void Parse_ReportsInvalidXmpTrailerAndHonorsXmpOptionAndLimit()
    {
        List<byte> invalidBytes = CreateGif();
        AddApplicationHeader(invalidBytes, "XMP DataXMP");
        AddBytes(invalidBytes, new byte[] { 2, 60, 62, 0, 59 });

        ImageMetadataDocument invalid = ParseGif(invalidBytes.ToArray(), new ImageMetadataReaderOptions());

        AssertDiagnostic(invalid, "gif.xmp.trailer.invalid", MetadataSeverity.RecoverableError);
        Assert.That(invalid.Profiles, Is.Empty);

        byte[] packet = Encoding.UTF8.GetBytes("<x/>");
        List<byte> validBytes = CreateGif();
        AddApplicationHeader(validBytes, "XMP DataXMP");
        AddBytes(validBytes, packet);
        AddXmpMagicTrailer(validBytes);
        validBytes.Add(59);
        ImageMetadataReaderOptions limitedOptions = new ImageMetadataReaderOptions();
        limitedOptions.MaxXmlBytes = 3;

        ImageMetadataDocument limited = ParseGif(validBytes.ToArray(), limitedOptions);

        AssertDiagnostic(limited, "gif.xmp.profile.limit", MetadataSeverity.LimitExceeded);
        Assert.That(limited.Profiles, Has.Count.EqualTo(1));
        Assert.That(limited.Profiles[0].Length, Is.EqualTo(4));
        Assert.That(limited.Profiles[0].Data, Is.Empty);

        ImageMetadataReaderOptions disabledOptions = new ImageMetadataReaderOptions();
        disabledOptions.ParseXmp = false;

        ImageMetadataDocument disabled = ParseGif(validBytes.ToArray(), disabledOptions);

        Assert.That(disabled.Diagnostics, Is.Empty);
        Assert.That(disabled.Profiles, Is.Empty);
    }

    [Test]
    public void Parse_PreservesIccProfileAndHonorsProfileLimit()
    {
        byte[] profileBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
        List<byte> bytes = CreateGif();
        AddApplicationHeader(bytes, "ICCRGBG1012");
        AddBytes(bytes, new byte[] { 2, 1, 2, 4, 3, 4, 5, 6, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.Count.EqualTo(1));
        Assert.That(document.Profiles[0].Kind, Is.EqualTo(MetadataProfileKind.Icc));
        Assert.That(document.Profiles[0].Family, Is.EqualTo(MetadataFamily.Icc));
        Assert.That(document.Profiles[0].Key, Is.EqualTo("Icc.GIF.ApplicationExtension"));
        Assert.That(document.Profiles[0].MimeType, Is.EqualTo("application/vnd.iccprofile"));
        Assert.That(document.Profiles[0].Data, Is.EqualTo(profileBytes));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Gif.Application.IccProfileByteLength").Value.IntegerValue, Is.EqualTo(6));

        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.MaxProfileBytes = 5;
        ImageMetadataDocument limited = ParseGif(bytes.ToArray(), options);

        AssertDiagnostic(limited, "gif.icc.profile.limit", MetadataSeverity.LimitExceeded);
        Assert.That(limited.Profiles, Has.Count.EqualTo(1));
        Assert.That(limited.Profiles[0].Length, Is.EqualTo(6));
        Assert.That(limited.Profiles[0].Data, Is.Empty);
    }

    [Test]
    public void Parse_ReportsMalformedSemanticExtensionFields()
    {
        List<byte> bytes = CreateGif();
        AddGraphicControl(bytes, 244, 0, 0);
        AddApplicationHeader(bytes, "NETSCAPE2.0");
        AddBytes(bytes, new byte[] { 2, 2, 0, 0, 59 });

        ImageMetadataDocument document = ParseGif(bytes.ToArray(), new ImageMetadataReaderOptions());

        AssertDiagnostic(document, "gif.graphic-control.reserved.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "gif.graphic-control.disposal.invalid", MetadataSeverity.RecoverableError);
        AssertDiagnostic(document, "gif.netscape.loop.invalid", MetadataSeverity.RecoverableError);
    }

    public static List<byte> CreateGif()
    {
        return GifBlockParserTests.CreateHeader(2, 2, 0, 0, 0);
    }

    public static void AddGraphicControl(List<byte> bytes, int packed, int delay, int transparentIndex)
    {
        AddBytes(bytes, new byte[]
        {
            33, 249, 4, (byte)packed,
            (byte)(delay & 255), (byte)((delay >> 8) & 255),
            (byte)transparentIndex, 0
        });
    }

    public static void AddApplicationHeader(List<byte> bytes, string identifierAndCode)
    {
        AddBytes(bytes, new byte[] { 33, 255, 11 });
        AddBytes(bytes, Encoding.ASCII.GetBytes(identifierAndCode));
    }

    public static void AddImage(List<byte> bytes)
    {
        GifBlockParserTests.AddImageDescriptor(bytes, 0, 0, 2, 2, 0);
        AddBytes(bytes, new byte[] { 2, 0 });
    }

    public static void AddXmpMagicTrailer(List<byte> bytes)
    {
        bytes.Add(1);
        int value = 255;
        while (value >= 0)
        {
            bytes.Add((byte)value);
            value = value - 1;
        }

        bytes.Add(0);
    }

    public static void AddBytes(List<byte> bytes, byte[] values)
    {
        foreach (byte value in values)
        {
            bytes.Add(value);
        }
    }

    public static void AssertDiagnostic(ImageMetadataDocument document, string code, MetadataSeverity severity)
    {
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity));
    }

    public static ImageMetadataDocument ParseGif(byte[] bytes, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return GifMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Gif), options);
    }
}
