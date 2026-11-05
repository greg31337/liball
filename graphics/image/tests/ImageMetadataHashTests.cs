using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataHashTests
{
    [Test]
    public void ComputeSha256_HashesMergedMetadataRangesInSourceOrder()
    {
        byte[] source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "EXIF");
        directory.Offset = 2;
        directory.Length = 4;
        MetadataTag tag = directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("test"));
        tag.Offset = 4;
        tag.Length = 4;
        MetadataProfile profile = document.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 7, 8 });
        profile.Offset = 7;
        profile.Length = 2;
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP13", "Photoshop", 0, 2);
        block.Data = new byte[] { 99, 99 };

        string hash = new ImageMetadataHash().ComputeSha256(source, document);

        Assert.That(hash, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })));
    }

    [Test]
    public void ComputeSha256_PngPixelChangesDoNotChangeMetadataHash()
    {
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "tEXt", "IDAT", "IEND" },
            new byte[][]
            {
                PngMetadataExtractorTests.Ihdr(6),
                PngMetadataExtractorTests.Latin1Payload("Title", "Sunset"),
                new byte[] { 10, 20, 30, 40 },
                new byte[0]
            });
        ImageMetadataDocument document = PngMetadataExtractorTests.ParsePng(png, new ImageMetadataReaderOptions());
        MetadataDirectory imageData = FindDirectory(document, MetadataFamily.Png, "IDAT");
        byte[] changed = (byte[])png.Clone();
        changed[imageData.Offset + 8] = 99;

        string originalHash = new ImageMetadataHash().ComputeSha256(png, document);
        string changedHash = new ImageMetadataHash().ComputeSha256(changed, document);

        Assert.That(originalHash, Is.Not.Empty);
        Assert.That(changedHash, Is.EqualTo(originalHash));
    }

    [Test]
    public void ComputeSha256_PngMetadataChangesChangeHash()
    {
        byte[] png = PngMetadataExtractorTests.CreatePng(
            new string[] { "IHDR", "tEXt", "IDAT", "IEND" },
            new byte[][]
            {
                PngMetadataExtractorTests.Ihdr(6),
                PngMetadataExtractorTests.Latin1Payload("Title", "Sunset"),
                new byte[] { 10, 20, 30, 40 },
                new byte[0]
            });
        ImageMetadataDocument document = PngMetadataExtractorTests.ParsePng(png, new ImageMetadataReaderOptions());
        MetadataDirectory text = FindDirectory(document, MetadataFamily.Png, "tEXt");
        byte[] changed = (byte[])png.Clone();
        changed[text.Offset + 8 + 6] = (byte)'X';

        string originalHash = new ImageMetadataHash().ComputeSha256(png, document);
        string changedHash = new ImageMetadataHash().ComputeSha256(changed, document);

        Assert.That(changedHash, Is.Not.EqualTo(originalHash));
    }

    [Test]
    public void ComputeHash_SupportsNamedAlgorithmsCaseInsensitively()
    {
        byte[] source = new byte[] { 1, 2, 3, 4 };
        ImageMetadataDocument document = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 0, source.Length);

        string sha1 = new ImageMetadataHash().ComputeHash(source, document, "sha-1");
        string md5 = new ImageMetadataHash().ComputeHash(source, document, "Md5");
        string defaultHash = new ImageMetadataHash().ComputeHash(source, document, null);

        Assert.That(sha1, Is.EqualTo(HashHelper.ToHex(HashHelper.ComputeSha1(source))));
        Assert.That(md5, Is.EqualTo(HashHelper.ToHex(HashHelper.ComputeMd5(source))));
        Assert.That(defaultHash, Is.EqualTo(HashHelper.ComputeSha256Hex(source)));
    }

    [Test]
    public void ComputeSha256_ExcludesThumbnailPixelAndWholeSvgRanges()
    {
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataProfile thumbnail = document.AddProfileWithData(MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, "Thumbnail", "Thumbnail", source);
        thumbnail.Offset = 0;
        thumbnail.Length = source.Length;
        MetadataRawBlock item = document.AddRawBlockWithRange(MetadataFamily.Ico, "Item", "ICO/CUR Image Item", 0, source.Length);
        item.Data = source;
        MetadataDirectory svg = document.AddDirectoryWithName(MetadataFamily.Svg, "XML", "SVG Document");
        svg.Offset = 0;
        svg.Length = source.Length;

        Assert.That(new ImageMetadataHash().ComputeSha256(source, document), Is.Empty);
    }

    [Test]
    public void ComputeHash_ReturnsEmptyForMissingRangesInvalidInputAndUnsupportedAlgorithm()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        ImageMetadataDocument invalid = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 2, 5);

        Assert.That(new ImageMetadataHash().ComputeSha256(source, new ImageMetadataDocument()), Is.Empty);
        Assert.That(new ImageMetadataHash().ComputeSha256(source, invalid), Is.Empty);
        Assert.That(new ImageMetadataHash().ComputeSha256(null, invalid), Is.Empty);
        Assert.That(new ImageMetadataHash().ComputeSha256(source, null), Is.Empty);
        Assert.That(new ImageMetadataHash().ComputeHash(source, invalid, "CRC32"), Is.Empty);
    }

    [Test]
    public void HashOptionsAndResult_HaveInitializedDefaults()
    {
        ImageMetadataHashOptions options = new ImageMetadataHashOptions();
        ImageMetadataHashResult result = new ImageMetadataHashResult();

        Assert.That(options.AlgorithmName, Is.EqualTo("SHA-256"));
        Assert.That(options.IncludeDirectories, Is.True);
        Assert.That(options.IncludeProfiles, Is.True);
        Assert.That(options.IncludeRawBlocks, Is.True);
        Assert.That(options.Families, Is.Empty);
        Assert.That(options.ProfileKinds, Is.Empty);
        Assert.That(options.RawBlockTypes, Is.Empty);
        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Unknown));
        Assert.That(result.AlgorithmName, Is.Empty);
        Assert.That(result.HexDigest, Is.Empty);
        Assert.That(result.RangeCount, Is.Zero);
        Assert.That(result.HashedByteCount, Is.Zero);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ComputeBytes_AppliesFamilyProfileKindAndRawBlockTypeSelections()
    {
        byte[] source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "EXIF");
        exif.Offset = 0;
        exif.Length = 2;
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        xmp.Offset = 2;
        xmp.Length = 2;
        MetadataProfile xmpProfile = document.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "xmp", "XMP", new byte[] { 4, 5 });
        xmpProfile.Offset = 4;
        xmpProfile.Length = 2;
        MetadataProfile iccProfile = document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "ICC", new byte[] { 6, 7 });
        iccProfile.Offset = 6;
        iccProfile.Length = 2;
        document.AddRawBlockWithRange(MetadataFamily.Xmp, "APP1", "XMP", 8, 2);
        document.AddRawBlockWithRange(MetadataFamily.Photoshop, "APP13", "Photoshop", 10, 2);
        ImageMetadataHashOptions options = new ImageMetadataHashOptions();
        options.Families = new MetadataFamily[] { MetadataFamily.Xmp };
        options.ProfileKinds = new MetadataProfileKind[] { MetadataProfileKind.Xmp };
        options.RawBlockTypes = new string[] { "APP1" };

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeBytes(source, document, options);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.AlgorithmName, Is.EqualTo("SHA-256"));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 2, 3, 4, 5, 8, 9 })));
        Assert.That(result.RangeCount, Is.EqualTo(2));
        Assert.That(result.HashedByteCount, Is.EqualTo(6));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ComputeBytes_CategoryFlagsCanSelectOnlyProfiles()
    {
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "EXIF");
        directory.Offset = 0;
        directory.Length = 2;
        MetadataProfile profile = document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "ICC", new byte[] { 3, 4 });
        profile.Offset = 2;
        profile.Length = 2;
        document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP1", "EXIF", 4, 2);
        ImageMetadataHashOptions options = new ImageMetadataHashOptions();
        options.IncludeDirectories = false;
        options.IncludeRawBlocks = false;
        options.ProfileKinds = new MetadataProfileKind[] { MetadataProfileKind.Icc };
        options.AlgorithmName = "md5";

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeBytes(source, document, options);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.AlgorithmName, Is.EqualTo("MD5"));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ToHex(HashHelper.ComputeMd5(new byte[] { 3, 4 }))));
        Assert.That(result.HashedByteCount, Is.EqualTo(2));
    }

    [Test]
    public void ComputeSpan_HashesCallerOwnedSliceWithoutChangingLegacyBehavior()
    {
        byte[] storage = new byte[] { 99, 98, 1, 2, 3, 4, 97 };
        ImageMetadataDocument document = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 1, 3);
        IImageMetadataHash hash = new ImageMetadataHash();

        ImageMetadataHashResult result = hash.ComputeSpan(storage.AsSpan(2, 4), document, null);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 2, 3, 4 })));
        Assert.That(result.RangeCount, Is.EqualTo(1));
        Assert.That(result.HashedByteCount, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStream_HashesFromCurrentPositionAndRestoresSeekableStream()
    {
        byte[] bytes = new byte[] { 90, 91, 0, 1, 2, 3, 4, 5 };
        MemoryStream stream = new MemoryStream(bytes);
        stream.Position = 2;
        ImageMetadataDocument document = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 2, 3);

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeStream(stream, document, null);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 2, 3, 4 })));
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void ComputeStream_HashesNonSeekableRangesWithoutBufferingWholeSource()
    {
        byte[] bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6 };
        NonSeekableReadStream stream = new NonSeekableReadStream(bytes);
        ImageMetadataDocument document = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 2, 3);
        document.ByteLength = bytes.Length;

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeStream(stream, document, null);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 2, 3, 4 })));
        Assert.That(stream.PositionValue, Is.EqualTo(5));
    }

    [Test]
    public void ExplicitResult_DistinguishesEmptyUnsupportedSeparationAlgorithmAndInvalidInput()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        ImageMetadataHash hash = new ImageMetadataHash();
        ImageMetadataHashResult empty = hash.ComputeBytes(source, new ImageMetadataDocument(), null);
        ImageMetadataDocument inseparable = new ImageMetadataDocument();
        MetadataDirectory svg = inseparable.AddDirectoryWithName(MetadataFamily.Svg, "XML", "SVG");
        svg.Offset = 0;
        svg.Length = source.Length;
        ImageMetadataHashResult separation = hash.ComputeBytes(source, inseparable, null);
        ImageMetadataHashOptions unsupportedOptions = new ImageMetadataHashOptions();
        unsupportedOptions.AlgorithmName = "CRC32";
        ImageMetadataHashResult algorithm = hash.ComputeBytes(source, new ImageMetadataDocument(), unsupportedOptions);
        ImageMetadataHashResult invalid = hash.ComputeBytes(null, new ImageMetadataDocument(), null);

        Assert.That(empty.Status, Is.EqualTo(ImageMetadataHashStatus.EmptyMetadata));
        Assert.That(empty.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.metadata.empty"));
        Assert.That(separation.Status, Is.EqualTo(ImageMetadataHashStatus.UnsupportedSeparation));
        Assert.That(separation.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.separation.unsupported"));
        Assert.That(algorithm.Status, Is.EqualTo(ImageMetadataHashStatus.UnsupportedAlgorithm));
        Assert.That(algorithm.AlgorithmName, Is.EqualTo("CRC32"));
        Assert.That(algorithm.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.algorithm.unsupported"));
        Assert.That(invalid.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.input.invalid"));
        Assert.That(empty.HexDigest, Is.Empty);
        Assert.That(separation.HexDigest, Is.Empty);
        Assert.That(algorithm.HexDigest, Is.Empty);
        Assert.That(invalid.HexDigest, Is.Empty);
    }

    [Test]
    public void ComputeStream_TruncatedNonSeekableSelectionReturnsInvalidInput()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2 });
        ImageMetadataDocument document = DocumentWithDirectoryRange(MetadataFamily.Exif, "IFD0", 1, 5);
        document.ByteLength = 10;

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeStream(stream, document, null);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(result.HexDigest, Is.Empty);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.stream.truncated"));
    }

    public static ImageMetadataDocument DocumentWithDirectoryRange(MetadataFamily family, string group, long offset, long length)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(family, group, group);
        directory.Offset = offset;
        directory.Length = length;
        return document;
    }

    public static MetadataDirectory FindDirectory(ImageMetadataDocument document, MetadataFamily family, string group)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory.Family == family && directory.Group == group)
            {
                return directory;
            }
        }

        throw new System.InvalidOperationException("Expected metadata directory was not found: " + group);
    }
}
