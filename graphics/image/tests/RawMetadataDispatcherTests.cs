using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class RawMetadataDispatcherTests
{
    [Test]
    public void Classify_MapsEveryRawFormatToItsContainerFamily()
    {
        ImageFormatKind[] tiffKinds = new[]
        {
            ImageFormatKind.Dng,
            ImageFormatKind.Nef,
            ImageFormatKind.Arw,
            ImageFormatKind.Orf,
            ImageFormatKind.Pef,
            ImageFormatKind.Rw2,
            ImageFormatKind.Srw,
            ImageFormatKind.ThreeFr,
            ImageFormatKind.Kdc,
            ImageFormatKind.Mrw,
            ImageFormatKind.Rwl,
            ImageFormatKind.Gpr,
            ImageFormatKind.Cr2
        };

        foreach (ImageFormatKind kind in tiffKinds)
        {
            Assert.That(RawMetadataDispatcher.Classify(kind), Is.EqualTo(RawContainerKind.Tiff), kind.ToString());
        }

        Assert.That(RawMetadataDispatcher.Classify(ImageFormatKind.Cr3), Is.EqualTo(RawContainerKind.Bmff));
        Assert.That(RawMetadataDispatcher.Classify(ImageFormatKind.Crw), Is.EqualTo(RawContainerKind.Ciff));
        Assert.That(RawMetadataDispatcher.Classify(ImageFormatKind.Raf), Is.EqualTo(RawContainerKind.Raf));
        Assert.That(RawMetadataDispatcher.Classify(ImageFormatKind.Jpeg), Is.EqualTo(RawContainerKind.Unknown));
        Assert.That(RawMetadataDispatcher.Classify(ImageFormatKind.Unknown), Is.EqualTo(RawContainerKind.Unknown));
    }

    [Test]
    public void Parse_TiffLikeRawUsesTiffParserAndPreservesRawIdentity()
    {
        byte[] bytes = new byte[] { 73, 73, 42, 0, 0, 0, 0, 0 };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Dng);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Dng));
        Assert.That(document.Format.Name, Is.EqualTo("DNG"));
        Assert.That(document.Format.MimeType, Is.EqualTo("image/x-adobe-dng"));
        Assert.That(document.Format.PreferredExtension, Is.EqualTo(".dng"));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.ByteOrder").Value.Text, Is.EqualTo("LittleEndian"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Magic").Value.IntegerValue, Is.EqualTo(42));
    }

    [Test]
    public void Parse_Cr2UsesTiffBranchAndRetainsCanonFormat()
    {
        byte[] bytes = new byte[]
        {
            73, 73, 42, 0,
            0, 0, 0, 0,
            67, 82, 2, 0,
            0, 0, 0, 0
        };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Cr2);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr2));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Magic"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.HeaderVariant").Value.Text, Is.EqualTo("CR2"));
    }

    [Test]
    public void Parse_Cr3UsesBmffBranch()
    {
        byte[] bytes = BmffBoxTraverserTests.CreateBox("ftyp", new byte[] { 99, 114, 120, 32, 0, 0, 0, 0 });

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Cr3);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Cr3));
        Assert.That(document.Format.ContainerBrand, Is.EqualTo("crx "));
        Assert.That(document.Directories, Has.Count.EqualTo(2));
        Assert.That(document.Directories[0].Group, Is.EqualTo("ftyp"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Bmff.ftyp.MajorBrand").Value.Text, Is.EqualTo("crx "));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Format").Value.Text, Is.EqualTo("CR3"));
    }

    [Test]
    public void Parse_CrwAndRafReachTheirDedicatedBranches()
    {
        ImageMetadataDocument crw = Parse(CrwHeader(), ImageFormatKind.Crw);
        ImageMetadataDocument raf = Parse(RafHeader(), ImageFormatKind.Raf);

        Assert.That(crw.Format.Kind, Is.EqualTo(ImageFormatKind.Crw));
        Assert.That(crw.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(crw, "Raw.CIFF.Header.Signature").Value.Text, Is.EqualTo("HEAPCCDR"));
        Assert.That(crw.Directories, Has.Some.Matches<MetadataDirectory>(directory => directory.Group == "Root"));
        Assert.That(raf.Format.Kind, Is.EqualTo(ImageFormatKind.Raf));
        Assert.That(raf.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(raf, "Raw.Profile.Container").Value.Text, Is.EqualTo("RAF"));
    }

    [Test]
    public void Parse_UnsupportedKindReportsDispatcherDiagnosticAndCarriesReaderDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MetadataDiagnostic.AddWarning(diagnostics, "source.warning", "Source warning");
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);

        ImageMetadataDocument document = RawMetadataDispatcher.Parse(reader, ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg), new ImageMetadataReaderOptions());

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.ByteLength, Is.EqualTo(3));
        Assert.That(document.Diagnostics, Has.Count.EqualTo(2));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("source.warning"));
        Assert.That(document.Diagnostics[1].Code, Is.EqualTo("raw.container.unsupported"));
        Assert.That(document.Diagnostics[1].BlockType, Is.EqualTo("RawMetadataDispatcher"));
    }

    [Test]
    public void ImageReader_DetectsCrwAndDispatchesToCiffBranch()
    {
        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(CrwHeader()));

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Crw));
        Assert.That(document.Format.DetectedByMagic, Is.True);
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Container").Value.Text, Is.EqualTo("CIFF"));
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageFormatKind kind)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return RawMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), new ImageMetadataReaderOptions());
    }

    public static byte[] CrwHeader()
    {
        return new byte[]
        {
            73, 73,
            14, 0, 0, 0,
            72, 69, 65, 80, 67, 67, 68, 82,
            0, 0,
            0, 0, 0, 0
        };
    }

    public static byte[] RafHeader()
    {
        byte[] bytes = new byte[108];
        byte[] signature = Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW ");
        signature.CopyTo(bytes, 0);
        Encoding.ASCII.GetBytes("0201").CopyTo(bytes, 16);
        Encoding.ASCII.GetBytes("TEST0001").CopyTo(bytes, 20);
        Encoding.ASCII.GetBytes("FUJIFILM X-T5").CopyTo(bytes, 28);
        Encoding.ASCII.GetBytes("0103").CopyTo(bytes, 60);
        return bytes;
    }
}
