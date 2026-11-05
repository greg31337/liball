using System;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageFormatDetectorTests
{
    [Test]
    public void DetectBytes_DetectsCommonRasterFormats()
    {
        AssertKind(ImageFormatKind.Jpeg, new byte[] { 255, 216, 255, 224 });
        AssertKind(ImageFormatKind.Png, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        AssertKind(ImageFormatKind.Gif, Encoding.ASCII.GetBytes("GIF89a"));
        AssertKind(ImageFormatKind.WebP, BuildRiffWebP());
        AssertKind(ImageFormatKind.Bmp, Encoding.ASCII.GetBytes("BMxxxx"));
        AssertKind(ImageFormatKind.Ico, new byte[] { 0, 0, 1, 0, 1, 0 });
        AssertKind(ImageFormatKind.Cur, new byte[] { 0, 0, 2, 0, 1, 0 });
    }

    [Test]
    public void DetectBytes_DetectsTiffBigTiffAndRawWrappers()
    {
        AssertKind(ImageFormatKind.Tiff, new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 });
        AssertKind(ImageFormatKind.BigTiff, new byte[] { 73, 73, 43, 0, 8, 0, 0, 0 });
        AssertKind(ImageFormatKind.Cr2, new byte[] { 73, 73, 42, 0, 16, 0, 0, 0, 67, 82, 2, 0 });
        AssertKind(ImageFormatKind.Orf, new byte[] { 73, 73, 82, 79, 8, 0 });
        AssertKind(ImageFormatKind.Rw2, new byte[] { 73, 73, 85, 0, 8, 0 });
        AssertKind(ImageFormatKind.Raf, Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW test"));
        AssertKind(ImageFormatKind.Mrw, new byte[] { 0, 77, 82, 77, 0, 0 });
        AssertKind(ImageFormatKind.Crw, RawMetadataDispatcherTests.CrwHeader());
    }

    [Test]
    public void DetectBytes_DetectsAlternateEndianAndVersionSignatures()
    {
        AssertKind(ImageFormatKind.Tiff, new byte[] { 77, 77, 0, 42 });
        AssertKind(ImageFormatKind.BigTiff, new byte[] { 77, 77, 0, 43 });
        AssertKind(ImageFormatKind.Orf, new byte[] { 73, 73, 82, 83 });
        AssertKind(ImageFormatKind.Gif, Encoding.ASCII.GetBytes("GIF87a"));
        AssertKind(ImageFormatKind.RadianceHdr, Encoding.ASCII.GetBytes("#?RGBE\n"));
        AssertKind(ImageFormatKind.Eps, new byte[] { 197, 208, 211, 198 });
    }

    [TestCase("P1")]
    [TestCase("P2")]
    [TestCase("P3")]
    [TestCase("P4")]
    [TestCase("P5")]
    [TestCase("P6")]
    [TestCase("P7")]
    [TestCase("PF")]
    [TestCase("Pf")]
    public void DetectBytes_DetectsEveryNetpbmMagic(string magic)
    {
        AssertKind(ImageFormatKind.Netpbm, Encoding.ASCII.GetBytes(magic + "\n"));
    }

    [Test]
    public void DetectBytes_DetectsBmffBrands()
    {
        ImageFormatInfo avif = new ImageFormatDetector().DetectBytes(BuildFtyp("avif"));
        ImageFormatInfo heic = new ImageFormatDetector().DetectBytes(BuildFtyp("heic"));
        ImageFormatInfo heif = new ImageFormatDetector().DetectBytes(BuildFtyp("mif1"));
        ImageFormatInfo cr3 = new ImageFormatDetector().DetectBytes(BuildFtyp("crx "));
        ImageFormatInfo jp2 = new ImageFormatDetector().DetectBytes(BuildFtyp("jp2 "));

        Assert.That(avif.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(heic.Kind, Is.EqualTo(ImageFormatKind.Heic));
        Assert.That(heif.Kind, Is.EqualTo(ImageFormatKind.Heif));
        Assert.That(cr3.Kind, Is.EqualTo(ImageFormatKind.Cr3));
        Assert.That(jp2.Kind, Is.EqualTo(ImageFormatKind.Jpeg2000));
        Assert.That(avif.ContainerBrand, Is.EqualTo("avif"));
        Assert.That(avif.DetectedByMagic, Is.True);
    }

    [Test]
    public void DetectBytes_DetectsProfessionalAndModernFormats()
    {
        AssertKind(ImageFormatKind.Jpeg2000, new byte[] { 0, 0, 0, 12, 106, 80, 32, 32, 13, 10, 135, 10 });
        AssertKind(ImageFormatKind.JpegXl, new byte[] { 0, 0, 0, 12, 74, 88, 76, 32, 13, 10, 135, 10 });
        AssertKind(ImageFormatKind.JpegXl, new byte[] { 255, 10, 1, 2 });
        AssertKind(ImageFormatKind.Psd, new byte[] { 56, 66, 80, 83, 0, 1 });
        AssertKind(ImageFormatKind.Psb, new byte[] { 56, 66, 80, 83, 0, 2 });
        AssertKind(ImageFormatKind.Dds, Encoding.ASCII.GetBytes("DDS data"));
        AssertKind(ImageFormatKind.Qoi, Encoding.ASCII.GetBytes("qoifdata"));
    }

    [Test]
    public void DetectBytes_DetectsTextAndLegacyFormats()
    {
        AssertKind(ImageFormatKind.RadianceHdr, Encoding.ASCII.GetBytes("#?RADIANCE\n"));
        AssertKind(ImageFormatKind.Eps, Encoding.ASCII.GetBytes("%!PS-Adobe-3.0 EPSF-3.0"));
        AssertKind(ImageFormatKind.Netpbm, Encoding.ASCII.GetBytes("P6\n1 1\n255\n"));
        AssertKind(ImageFormatKind.Netpbm, Encoding.ASCII.GetBytes("PF\n1 1\n-1\n"));
        AssertKind(ImageFormatKind.Pcx, new byte[] { 10, 5, 1, 8, 0, 0 });
        AssertKind(ImageFormatKind.Tga, BuildTgaWithFooter());
        AssertKind(ImageFormatKind.Svg, Encoding.ASCII.GetBytes("  <?xml version=\"1.0\"?><svg></svg>"));
        AssertKind(ImageFormatKind.Svgz, new byte[] { 31, 139, 8, 0 });
    }

    [Test]
    public void DetectBytes_DetectsSvgWithBomAndCaseInsensitiveElement()
    {
        byte[] markup = Encoding.UTF8.GetBytes(" \n<SVG xmlns=\"http://www.w3.org/2000/svg\"/>");
        byte[] bytes = Join(Encoding.UTF8.GetPreamble(), markup);

        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(bytes);

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Svg));
        Assert.That(info.Confidence, Is.EqualTo(85));
        Assert.That(info.MimeType, Is.EqualTo("image/svg+xml"));
        Assert.That(info.PreferredExtension, Is.EqualTo(".svg"));
    }

    [Test]
    public void DetectBytes_DetectsAsciiUtf8AndUtf16XmpPackets()
    {
        string xml = "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/>";
        byte[] ascii = Encoding.UTF8.GetBytes(xml);
        byte[] utf8 = Join(Encoding.UTF8.GetPreamble(), Encoding.UTF8.GetBytes(xml));
        byte[] utf16Little = Join(Encoding.Unicode.GetPreamble(), Encoding.Unicode.GetBytes(xml));
        byte[] utf16Big = Join(Encoding.BigEndianUnicode.GetPreamble(), Encoding.BigEndianUnicode.GetBytes(xml));

        AssertKind(ImageFormatKind.Xmp, ascii);
        AssertKind(ImageFormatKind.Xmp, utf8);
        AssertKind(ImageFormatKind.Xmp, utf16Little);
        AssertKind(ImageFormatKind.Xmp, utf16Big);
    }

    [Test]
    public void DetectBytes_ReturnsUnknownForEmptyUnknownOrGenericBmff()
    {
        Assert.That(new ImageFormatDetector().DetectBytes(null).Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(new ImageFormatDetector().DetectBytes(new byte[0]).Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(new ImageFormatDetector().DetectBytes(new byte[] { 1, 2, 3, 4 }).Kind, Is.EqualTo(ImageFormatKind.Unknown));

        ImageFormatInfo bmff = new ImageFormatDetector().DetectBytes(BuildFtyp("abcd"));
        Assert.That(bmff.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(bmff.Name, Is.EqualTo("BMFF"));
        Assert.That(bmff.ContainerBrand, Is.EqualTo("abcd"));
        Assert.That(bmff.DetectedByMagic, Is.True);
        AssertUnknownDiagnostic(bmff);
    }

    [Test]
    public void DetectSpan_AcceptsStackAllocatedProbeAndDetectsContainerBrand()
    {
        ReadOnlySpan<byte> pngProbe = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        ReadOnlySpan<byte> avifProbe = BuildFtyp("avif");
        IImageFormatDetector detector = new ImageFormatDetector();

        ImageFormatInfo png = detector.DetectSpan(pngProbe);
        ImageFormatInfo avif = detector.DetectSpan(avifProbe);

        Assert.That(png.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(png.DetectedByMagic, Is.True);
        Assert.That(png.Diagnostics, Is.Empty);
        Assert.That(avif.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(avif.ContainerBrand, Is.EqualTo("avif"));
    }

    [TestCase("photo.PNG", ImageFormatKind.Png)]
    [TestCase(".jpeg", ImageFormatKind.Jpeg)]
    [TestCase("webp", ImageFormatKind.WebP)]
    [TestCase("folder/archive.JPX", ImageFormatKind.Jpeg2000)]
    [TestCase("folder\\image.SVGZ", ImageFormatKind.Svgz)]
    [TestCase("picture.pfm", ImageFormatKind.Netpbm)]
    [TestCase("sidecar.xmp", ImageFormatKind.Xmp)]
    public void DetectBytesWithHint_FallsBackToFilenameOrExtension(string hint, ImageFormatKind expected)
    {
        IImageFormatDetector detector = new ImageFormatDetector();

        ImageFormatInfo info = detector.DetectBytesWithHint(new byte[] { 1, 2, 3, 4 }, hint);

        Assert.That(info.Kind, Is.EqualTo(expected));
        Assert.That(info.DetectedByMagic, Is.False);
        Assert.That(info.Confidence, Is.EqualTo(40));
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void DetectBytesWithHint_RecognizesEveryPreferredExtension()
    {
        IImageFormatDetector detector = new ImageFormatDetector();

        foreach (ImageFormatKind kind in Enum.GetValues<ImageFormatKind>())
        {
            if (kind != ImageFormatKind.Unknown)
            {
                string extension = ImageFormatMapping.GetPreferredExtension(kind);
                ImageFormatInfo info = detector.DetectBytesWithHint(new byte[] { 1 }, "image" + extension);

                Assert.That(info.Kind, Is.EqualTo(kind), extension);
                Assert.That(info.DetectedByMagic, Is.False, extension);
            }
        }
    }

    [Test]
    public void DetectionHint_CanEstablishFormatWithoutProbeBytes()
    {
        IImageFormatDetector detector = new ImageFormatDetector();

        ImageFormatInfo bytes = detector.DetectBytesWithHint(null, "photo.png");
        ImageFormatInfo span = detector.DetectSpanWithHint(ReadOnlySpan<byte>.Empty, ".gif");

        Assert.That(bytes.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(bytes.Diagnostics, Is.Empty);
        Assert.That(span.Kind, Is.EqualTo(ImageFormatKind.Gif));
        Assert.That(span.Diagnostics, Is.Empty);
    }

    [Test]
    public void DetectionHint_RefinesUnknownBmffOnlyToBmffImageFormat()
    {
        IImageFormatDetector detector = new ImageFormatDetector();
        byte[] genericBmff = BuildFtyp("abcd");

        ImageFormatInfo avif = detector.DetectBytesWithHint(genericBmff, "photo.avif");
        ImageFormatInfo png = detector.DetectBytesWithHint(genericBmff, "photo.png");

        Assert.That(avif.Kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(avif.ContainerBrand, Is.EqualTo("abcd"));
        Assert.That(avif.DetectedByMagic, Is.False);
        Assert.That(avif.Diagnostics, Is.Empty);
        Assert.That(png.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(png.ContainerBrand, Is.EqualTo("abcd"));
        AssertUnknownDiagnostic(png);
    }

    [TestCase(".dng", ImageFormatKind.Dng)]
    [TestCase("photo.NEF", ImageFormatKind.Nef)]
    [TestCase("photo.arw", ImageFormatKind.Arw)]
    [TestCase("photo.orf", ImageFormatKind.Orf)]
    [TestCase("photo.pef", ImageFormatKind.Pef)]
    [TestCase("photo.rw2", ImageFormatKind.Rw2)]
    [TestCase("photo.srw", ImageFormatKind.Srw)]
    [TestCase("photo.3fr", ImageFormatKind.ThreeFr)]
    [TestCase("photo.kdc", ImageFormatKind.Kdc)]
    [TestCase("photo.rwl", ImageFormatKind.Rwl)]
    [TestCase("photo.gpr", ImageFormatKind.Gpr)]
    [TestCase("photo.cr2", ImageFormatKind.Cr2)]
    public void DetectSpanWithHint_RefinesAmbiguousTiffRawFormats(string hint, ImageFormatKind expected)
    {
        ReadOnlySpan<byte> tiff = stackalloc byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };

        ImageFormatInfo info = new ImageFormatDetector().DetectSpanWithHint(tiff, hint);

        Assert.That(info.Kind, Is.EqualTo(expected));
        Assert.That(info.DetectedByMagic, Is.False);
        Assert.That(info.Confidence, Is.EqualTo(75));
        Assert.That(info.Diagnostics, Is.Empty);
    }

    [Test]
    public void DetectionHints_DoNotOverrideSpecificMagicOrMisclassifyTiff()
    {
        IImageFormatDetector detector = new ImageFormatDetector();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        byte[] tiff = new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 };

        ImageFormatInfo magicWins = detector.DetectBytesWithHint(png, "photo.nef");
        ImageFormatInfo ordinaryMismatch = detector.DetectBytesWithHint(tiff, "photo.jpg");
        ImageFormatInfo proprietaryRawMismatch = detector.DetectBytesWithHint(tiff, "photo.mrw");

        Assert.That(magicWins.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(magicWins.DetectedByMagic, Is.True);
        Assert.That(ordinaryMismatch.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        Assert.That(ordinaryMismatch.DetectedByMagic, Is.True);
        Assert.That(proprietaryRawMismatch.Kind, Is.EqualTo(ImageFormatKind.Tiff));
        Assert.That(proprietaryRawMismatch.DetectedByMagic, Is.True);
    }

    [Test]
    public void DetectStreamWithHint_UsesFallbackAndRestoresSeekablePosition()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
        stream.Position = 2;

        ImageFormatInfo info = new ImageFormatDetector().DetectStreamWithHint(stream, "image.qoi");

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        Assert.That(info.DetectedByMagic, Is.False);
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void UnknownDetection_ReturnsStructuredDiagnostic()
    {
        IImageFormatDetector detector = new ImageFormatDetector();

        ImageFormatInfo nullBytes = detector.DetectBytes(null);
        ImageFormatInfo unknownBytes = detector.DetectSpan(stackalloc byte[] { 1, 2, 3, 4 });
        ImageFormatInfo unknownHint = detector.DetectBytesWithHint(new byte[] { 1 }, "photo.unrecognized");
        ImageFormatInfo nullStream = detector.DetectStream(null);

        AssertUnknownDiagnostic(nullBytes);
        AssertUnknownDiagnostic(unknownBytes);
        AssertUnknownDiagnostic(unknownHint);
        AssertUnknownDiagnostic(nullStream);
    }

    [Test]
    public void MetadataReader_PropagatesUnknownFormatDiagnostic()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.Diagnostics, Has.Some.Matches<MetadataDiagnostic>(item => item.Code == "format.unknown" && item.Severity == MetadataSeverity.Unsupported));
    }

    [TestCaseSource(nameof(TruncatedAndNearMatchSignatures))]
    public void DetectBytes_RejectsTruncatedAndNearMatchSignatures(byte[] bytes)
    {
        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(bytes);

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(info.DetectedByMagic, Is.False);
        Assert.That(info.Confidence, Is.Zero);
    }

    [Test]
    public void DetectStream_RestoresPositionForSeekableStreams()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0, 0, 137, 80, 78, 71, 13, 10, 26, 10 });
        stream.Position = 2;

        ImageFormatInfo info = new ImageFormatDetector().DetectStream(stream);

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void DetectStream_ReadsUntilSignatureAvailableWhenStreamShortReads()
    {
        ShortReadStream seekable = new ShortReadStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, true, 1);
        ShortReadStream nonSeekable = new ShortReadStream(new byte[] { 255, 216, 255, 224 }, false, 1);

        ImageFormatInfo png = new ImageFormatDetector().DetectStream(seekable);
        ImageFormatInfo jpeg = new ImageFormatDetector().DetectStream(nonSeekable);

        Assert.That(png.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(seekable.Position, Is.Zero);
        Assert.That(jpeg.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(nonSeekable.PositionValue, Is.EqualTo(4));
    }

    [Test]
    public void DetectStream_ProbesTgaFooterBeyondLeadingProbeAndRestoresPosition()
    {
        byte[] bytes = new byte[ImageFormatDetectorCore.StreamProbeLength + 100];
        byte[] footer = BuildTgaWithFooter();
        Array.Copy(footer, footer.Length - 18, bytes, bytes.Length - 18, 18);
        ShortReadStream stream = new ShortReadStream(bytes, true, 2);
        stream.Position = 5;

        ImageFormatInfo info = new ImageFormatDetector().DetectStream(stream);

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Tga));
        Assert.That(info.Confidence, Is.EqualTo(100));
        Assert.That(stream.Position, Is.EqualTo(5));
    }

    [Test]
    public void DetectStream_NullReturnsUnknownAndNonSeekableConsumesAtMostProbeLength()
    {
        byte[] bytes = new byte[ImageFormatDetectorCore.StreamProbeLength + 100];
        NonSeekableReadStream stream = new NonSeekableReadStream(bytes);

        ImageFormatInfo missing = new ImageFormatDetector().DetectStream(null);
        ImageFormatInfo unknown = new ImageFormatDetector().DetectStream(stream);

        Assert.That(missing.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(unknown.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(stream.PositionValue, Is.EqualTo(ImageFormatDetectorCore.StreamProbeLength));
    }

    [Test]
    public void HelperMethods_HandleAsciiMatchingAndBrands()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("xxAbCdyy");

        Assert.That(ImageFormatDetectorCore.MatchesAscii(bytes, 2, "AbCd"), Is.True);
        Assert.That(ImageFormatDetectorCore.MatchesAsciiIgnoreCase(bytes, 2, "abcd"), Is.True);
        Assert.That(ImageFormatDetectorCore.ContainsAsciiIgnoreCase(bytes, "BCd", 8), Is.True);
        Assert.That(ImageFormatDetectorCore.ReadAscii(bytes, 2, 4), Is.EqualTo("AbCd"));
        Assert.That(ImageFormatDetectorCore.ReadAscii(bytes, 20, 4), Is.EqualTo(""));
    }

    static void AssertKind(ImageFormatKind expected, byte[] bytes)
    {
        ImageFormatInfo info = new ImageFormatDetector().DetectBytes(bytes);

        Assert.That(info.Kind, Is.EqualTo(expected));
        Assert.That(info.DetectedByMagic, Is.True);
        Assert.That(info.Confidence, Is.GreaterThan(0));
    }

    static void AssertUnknownDiagnostic(ImageFormatInfo info)
    {
        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(info.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo("format.unknown"));
        Assert.That(info.Diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Unsupported));
    }

    static object[] TruncatedAndNearMatchSignatures()
    {
        return new object[]
        {
            new byte[] { 255, 216 },
            new byte[] { 137, 80, 78, 71, 13, 10, 26 },
            Encoding.ASCII.GetBytes("GIF89"),
            Encoding.ASCII.GetBytes("RIFFxxxxWEPB"),
            new byte[] { 0, 0, 1 },
            new byte[] { 73, 73, 42 },
            new byte[] { 0, 0, 0, 12, 106, 80, 32, 32, 13, 10, 135 },
            new byte[] { 56, 66, 80, 83, 0, 3 },
            new byte[] { 10, 1, 1, 8 },
            Encoding.ASCII.GetBytes("not xml <svg/>")
        };
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] bytes = new byte[first.Length + second.Length];
        Array.Copy(first, 0, bytes, 0, first.Length);
        Array.Copy(second, 0, bytes, first.Length, second.Length);
        return bytes;
    }

    static byte[] BuildRiffWebP()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("RIFFxxxxWEBP");
        return bytes;
    }

    static byte[] BuildFtyp(string brand)
    {
        byte[] bytes = new byte[16];
        bytes[0] = 0;
        bytes[1] = 0;
        bytes[2] = 0;
        bytes[3] = 16;
        bytes[4] = 102;
        bytes[5] = 116;
        bytes[6] = 121;
        bytes[7] = 112;
        byte[] brandBytes = Encoding.ASCII.GetBytes(brand);
        bytes[8] = brandBytes[0];
        bytes[9] = brandBytes[1];
        bytes[10] = brandBytes[2];
        bytes[11] = brandBytes[3];
        return bytes;
    }

    static byte[] BuildTgaWithFooter()
    {
        byte[] bytes = new byte[32];
        byte[] footer = Encoding.ASCII.GetBytes("TRUEVISION-XFILE.");
        int start = bytes.Length - 18;
        int index = 0;
        while (index < footer.Length)
        {
            bytes[start + index] = footer[index];
            index = index + 1;
        }

        bytes[bytes.Length - 1] = 0;
        return bytes;
    }
}

public class ShortReadStream : Stream
{
    public byte[] Bytes;
    public bool Seekable;
    public int MaximumRead;
    public int PositionValue;

    public ShortReadStream(byte[] bytes, bool seekable, int maximumRead)
    {
        Bytes = bytes;
        Seekable = seekable;
        MaximumRead = maximumRead;
        PositionValue = 0;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return Seekable; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return Bytes.Length; }
    }

    public override long Position
    {
        get { return PositionValue; }
        set { PositionValue = (int)value; }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int available = Bytes.Length - PositionValue;
        if (available <= 0)
        {
            return 0;
        }

        int actual = count;
        if (actual > MaximumRead) actual = MaximumRead;
        if (actual > available) actual = available;
        Array.Copy(Bytes, PositionValue, buffer, offset, actual);
        PositionValue = PositionValue + actual;
        return actual;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin) PositionValue = (int)offset;
        if (origin == SeekOrigin.Current) PositionValue = PositionValue + (int)offset;
        if (origin == SeekOrigin.End) PositionValue = Bytes.Length + (int)offset;
        return PositionValue;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }
}
