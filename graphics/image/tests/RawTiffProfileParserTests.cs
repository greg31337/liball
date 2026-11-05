using System;
using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class RawTiffProfileParserTests
{
    [Test]
    public void Parse_StandardTiffProfilesPreserveEveryRawIdentity()
    {
        ImageFormatKind[] kinds = new[]
        {
            ImageFormatKind.Dng,
            ImageFormatKind.Nef,
            ImageFormatKind.Arw,
            ImageFormatKind.Pef,
            ImageFormatKind.Srw,
            ImageFormatKind.ThreeFr,
            ImageFormatKind.Kdc,
            ImageFormatKind.Rwl,
            ImageFormatKind.Gpr
        };

        foreach (ImageFormatKind kind in kinds)
        {
            ImageMetadataDocument document = Parse(ClassicTiff(), kind, new ImageMetadataReaderOptions());

            Assert.That(document.Diagnostics, Is.Empty, kind.ToString());
            Assert.That(document.Format.Kind, Is.EqualTo(kind), kind.ToString());
            Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Format").Value.Text, Is.EqualTo(ImageFormatMapping.GetName(kind)), kind.ToString());
            Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Container").Value.Text, Is.EqualTo("TIFF"), kind.ToString());
            Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.TiffOffset").Value.IntegerValue, Is.EqualTo(0), kind.ToString());
            Assert.That(document.RawBlocks, Has.Count.EqualTo(1), kind.ToString());
            Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("Header"), kind.ToString());
        }
    }

    [TestCase(".dng", ImageFormatKind.Dng)]
    [TestCase(".NEF", ImageFormatKind.Nef)]
    [TestCase(".arw", ImageFormatKind.Arw)]
    [TestCase(".orf", ImageFormatKind.Orf)]
    [TestCase(".pef", ImageFormatKind.Pef)]
    [TestCase(".rw2", ImageFormatKind.Rw2)]
    [TestCase(".srw", ImageFormatKind.Srw)]
    [TestCase(".3fr", ImageFormatKind.ThreeFr)]
    [TestCase(".kdc", ImageFormatKind.Kdc)]
    [TestCase(".mrw", ImageFormatKind.Mrw)]
    [TestCase(".rwl", ImageFormatKind.Rwl)]
    [TestCase(".gpr", ImageFormatKind.Gpr)]
    public void KindFromExtension_MapsEveryTiffLikeRawProfile(string extension, ImageFormatKind expected)
    {
        Assert.That(RawTiffProfileParser.KindFromExtension(extension), Is.EqualTo(expected));
    }

    [TestCase(ImageFormatKind.Orf, 82, 79, "ORF")]
    [TestCase(ImageFormatKind.Orf, 82, 83, "ORF")]
    [TestCase(ImageFormatKind.Rw2, 85, 0, "RW2")]
    public void Parse_AlternateRawMagicTraversesClassicTiffIfd(ImageFormatKind kind, byte firstMagic, byte secondMagic, string variant)
    {
        byte[] bytes = ClassicTiff();
        bytes[2] = firstMagic;
        bytes[3] = secondMagic;

        ImageMetadataDocument document = Parse(bytes, kind, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(kind));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.EntryCount"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.HeaderVariant").Value.Text, Is.EqualTo(variant));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.Header.Magic").Value.IntegerValue, Is.EqualTo(firstMagic | secondMagic << 8));
    }

    [Test]
    public void Parse_MrwFindsTtwBlockAndRelocatesEmbeddedTiffOffsets()
    {
        byte[] bytes = MrwWithTtw(ClassicTiff());

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Mrw, new ImageMetadataReaderOptions());

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Mrw));
        Assert.That(document.ByteLength, Is.EqualTo(bytes.Length));
        Assert.That(FindDirectory(document, "Header").Offset, Is.EqualTo(16));
        Assert.That(FindDirectory(document, "IFD0").Offset, Is.EqualTo(24));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Container").Value.Text, Is.EqualTo("MRW"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.TiffOffset").Value.IntegerValue, Is.EqualTo(16));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.TiffLength").Value.IntegerValue, Is.EqualTo(14));
        Assert.That(document.RawBlocks, Has.Count.EqualTo(2));
        Assert.That(document.RawBlocks[0].BlockType, Is.EqualTo("Header"));
        Assert.That(document.RawBlocks[1].BlockType, Is.EqualTo("TTW"));
        Assert.That(document.RawBlocks[1].Offset, Is.EqualTo(8));
        Assert.That(document.RawBlocks[1].Length, Is.EqualTo(22));
    }

    [Test]
    public void Parse_MrwWithoutTtwReturnsRecoverableDiagnostic()
    {
        byte[] bytes = new byte[]
        {
            0, 77, 82, 77, 0, 0, 0, 16,
            0, 80, 82, 68, 0, 0, 0, 0
        };

        ImageMetadataDocument document = Parse(bytes, ImageFormatKind.Mrw, new ImageMetadataReaderOptions());

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Mrw));
        Assert.That(HasDiagnostic(document, "raw.mrw.ttw.missing"), Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Tiff.Header.Magic"), Is.Null);
    }

    [Test]
    public void ReadFile_UsesRawExtensionForAnOtherwiseGenericTiff()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "raw-profile-" + Guid.NewGuid().ToString("N") + ".dng");
        File.WriteAllBytes(path, DngTiff());

        ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

        File.Delete(path);
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Dng));
        Assert.That(document.Format.DetectedByMagic, Is.False);
        Assert.That(document.Format.Confidence, Is.EqualTo(75));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Tiff.IFD0.DNGVersion"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Raw.Profile.Format").Value.Text, Is.EqualTo("DNG"));
    }

    [Test]
    public void ReadFile_MagicTakesPrecedenceOverRawExtension()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "raw-profile-" + Guid.NewGuid().ToString("N") + ".nef");
        File.WriteAllBytes(path, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        ImageMetadataDocument document = new ImageMetadataReader().ReadFile(path);

        File.Delete(path);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Format.DetectedByMagic, Is.True);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Raw.Profile.Format"), Is.Null);
    }

    [Test]
    public void Parse_RawBlockDataHonorsCaptureOptions()
    {
        ImageMetadataReaderOptions capturedOptions = new ImageMetadataReaderOptions();
        capturedOptions.CaptureRawBlockData = true;
        ImageMetadataReaderOptions disabledOptions = new ImageMetadataReaderOptions();
        disabledOptions.CaptureRawBlocks = false;

        ImageMetadataDocument captured = Parse(ClassicTiff(), ImageFormatKind.Nef, capturedOptions);
        ImageMetadataDocument disabled = Parse(ClassicTiff(), ImageFormatKind.Nef, disabledOptions);

        Assert.That(captured.RawBlocks, Has.Count.EqualTo(1));
        Assert.That(captured.RawBlocks[0].Data, Is.EqualTo(new byte[] { 73, 73, 42, 0, 8, 0, 0, 0 }));
        Assert.That(disabled.RawBlocks, Is.Empty);
    }

    public static ImageMetadataDocument Parse(byte[] bytes, ImageFormatKind kind, ImageMetadataReaderOptions options)
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(bytes);
        BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
        return RawTiffProfileParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
    }

    public static byte[] ClassicTiff()
    {
        return new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            0, 0,
            0, 0, 0, 0
        };
    }

    public static byte[] DngTiff()
    {
        return new byte[]
        {
            73, 73, 42, 0, 8, 0, 0, 0,
            1, 0,
            18, 198, 1, 0, 4, 0, 0, 0, 1, 4, 0, 0,
            0, 0, 0, 0
        };
    }

    public static byte[] MrwWithTtw(byte[] tiff)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(0);
        bytes.Add(77);
        bytes.Add(82);
        bytes.Add(77);
        AddUnsigned32BigEndian(bytes, 16 + tiff.Length);
        bytes.Add(0);
        bytes.Add(84);
        bytes.Add(84);
        bytes.Add(87);
        AddUnsigned32BigEndian(bytes, tiff.Length);
        bytes.AddRange(tiff);
        return bytes.ToArray();
    }

    public static void AddUnsigned32BigEndian(List<byte> bytes, int value)
    {
        bytes.Add((byte)((value >> 24) & 255));
        bytes.Add((byte)((value >> 16) & 255));
        bytes.Add((byte)((value >> 8) & 255));
        bytes.Add((byte)(value & 255));
    }

    public static MetadataDirectory FindDirectory(ImageMetadataDocument document, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Group == group)
            {
                return directory;
            }
        }

        throw new System.InvalidOperationException("Expected metadata directory was not found: " + group);
    }

    public static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
