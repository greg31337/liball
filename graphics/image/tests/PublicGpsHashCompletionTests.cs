using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicGpsHashCompletionTests
{
    [Test]
    public void GpsFactory_NormalizesLongCardinalAndAltitudeReferencesForFormatting()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        IImageMetadataFormatter formatter = new ImageMetadataFormatter();

        GpsCoordinate below = factory.CreateGpsCoordinate(34, 30, 0, "South", 118, 15, 0, "West", 12.5, "Below Sea Level");
        GpsCoordinate inferred = factory.CreateGpsCoordinate(-10, 15, 0, null, -20, 30, 0, null, -7.25, null);
        GpsCoordinate explicitPositive = factory.CreateGpsCoordinate(-1, 30, 0, "N", -2, 15, 0, "E", 8, "Above Sea Level");

        Assert.That(below.Latitude, Is.EqualTo(-34.5));
        Assert.That(below.Longitude, Is.EqualTo(-118.25));
        Assert.That(below.Altitude, Is.EqualTo(-12.5));
        Assert.That(below.LatitudeReference, Is.EqualTo("S"));
        Assert.That(below.LongitudeReference, Is.EqualTo("W"));
        Assert.That(formatter.ToGpsText(below), Is.EqualTo("34.5 S, 118.25 W, -12.5 m"));

        Assert.That(inferred.LatitudeReference, Is.EqualTo("S"));
        Assert.That(inferred.LongitudeReference, Is.EqualTo("W"));
        Assert.That(formatter.ToGpsText(inferred), Is.EqualTo("10.25 S, 20.5 W, -7.25 m"));

        Assert.That(explicitPositive.Latitude, Is.EqualTo(-1.5));
        Assert.That(explicitPositive.Longitude, Is.EqualTo(-2.25));
        Assert.That(explicitPositive.LatitudeReference, Is.EqualTo("N"));
        Assert.That(explicitPositive.LongitudeReference, Is.EqualTo("E"));
        Assert.That(formatter.ToGpsText(explicitPositive), Is.EqualTo("1.5 N, 2.25 E, 8 m Above Sea Level"));
    }

    [Test]
    public void HashSelection_MergesNestedRepeatedFrameProfileAndRawBlockRanges()
    {
        byte[] source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory exif = factory.CreateDirectory(MetadataFamily.Exif, "IFD0", "EXIF");
        exif.Offset = 0;
        exif.Length = 2;
        MetadataDirectory nested = factory.CreateDirectory(MetadataFamily.Xmp, "dc", "Nested XMP");
        nested.Offset = 2;
        nested.Length = 3;
        MetadataTag nestedTag = factory.CreateTag("Xmp.dc.title", "Title", MetadataFamily.Xmp, factory.CreateStringValue("Title"));
        nestedTag.Offset = 4;
        nestedTag.Length = 2;
        nested.Tags.Add(nestedTag);
        exif.Children.Add(nested);
        document.Directories.Add(exif);
        MetadataDirectory repeated = factory.CreateDirectory(MetadataFamily.Xmp, "dc", "Repeated XMP");
        repeated.RepeatIndex = 1;
        repeated.Offset = 6;
        repeated.Length = 1;
        document.Directories.Add(repeated);

        ImageMetadataFrame frame = new ImageMetadataFrame();
        MetadataDirectory frameDirectory = factory.CreateDirectory(MetadataFamily.Xmp, "dc", "Frame XMP");
        frameDirectory.Offset = 5;
        frameDirectory.Length = 2;
        frame.Directories.Add(frameDirectory);
        document.Frames.Add(frame);

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Offset = 8;
        profile.Length = 2;
        document.Profiles.Add(profile);
        MetadataProfile ignoredProfile = new MetadataProfile();
        ignoredProfile.Kind = MetadataProfileKind.Icc;
        ignoredProfile.Family = MetadataFamily.Icc;
        ignoredProfile.Offset = 12;
        ignoredProfile.Length = 2;
        document.Profiles.Add(ignoredProfile);

        document.RawBlocks.Add(RawBlock(MetadataFamily.Xmp, "APP1", 10, 2));
        document.RawBlocks.Add(RawBlock(MetadataFamily.Xmp, "APP13", 14, 2));
        ImageMetadataHashOptions options = new ImageMetadataHashOptions();
        options.Families = new MetadataFamily[] { MetadataFamily.Xmp };
        options.ProfileKinds = new MetadataProfileKind[] { MetadataProfileKind.Xmp };
        options.RawBlockTypes = new string[] { "APP1" };

        ImageMetadataHashResult result = new ImageMetadataHash().ComputeBytes(source, document, options);

        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.Success));
        Assert.That(result.RangeCount, Is.EqualTo(2));
        Assert.That(result.HashedByteCount, Is.EqualTo(9));
        Assert.That(result.HexDigest, Is.EqualTo(HashHelper.ComputeSha256Hex(new byte[] { 2, 3, 4, 5, 6, 8, 9, 10, 11 })));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void HashPublicEntryPoints_ReportEveryDocumentedFailureOutcome()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        IImageMetadataHash hash = new ImageMetadataHash();
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataHashResult nullBytes = hash.ComputeBytes(null, document, null);
        ImageMetadataHashResult nullByteDocument = hash.ComputeBytes(source, null, null);
        ImageMetadataHashResult nullSpanDocument = hash.ComputeSpan(source, null, null);
        ImageMetadataHashResult nullStream = hash.ComputeStream(null, document, null);
        MemoryStream unreadableStream = new MemoryStream(source);
        unreadableStream.Dispose();
        ImageMetadataHashResult unreadable = hash.ComputeStream(unreadableStream, document, null);

        ImageMetadataHashOptions noCategories = new ImageMetadataHashOptions();
        noCategories.IncludeDirectories = false;
        noCategories.IncludeProfiles = false;
        noCategories.IncludeRawBlocks = false;
        ImageMetadataHashResult empty = hash.ComputeBytes(source, document, noCategories);

        ImageMetadataDocument inseparable = new ImageMetadataDocument();
        MetadataDirectory svg = new MetadataDirectory();
        svg.Family = MetadataFamily.Svg;
        svg.Group = "XML";
        svg.Offset = 0;
        svg.Length = source.Length;
        inseparable.Directories.Add(svg);
        ImageMetadataHashResult separation = hash.ComputeBytes(source, inseparable, null);

        ImageMetadataHashOptions badAlgorithm = new ImageMetadataHashOptions();
        badAlgorithm.AlgorithmName = "CRC32";
        ImageMetadataHashResult unsupported = hash.ComputeBytes(source, document, badAlgorithm);

        AssertInvalidInput(nullBytes);
        AssertInvalidInput(nullByteDocument);
        AssertInvalidInput(nullSpanDocument);
        AssertInvalidInput(nullStream);
        AssertInvalidInput(unreadable);
        Assert.That(empty.Status, Is.EqualTo(ImageMetadataHashStatus.EmptyMetadata));
        Assert.That(empty.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.metadata.empty"));
        Assert.That(separation.Status, Is.EqualTo(ImageMetadataHashStatus.UnsupportedSeparation));
        Assert.That(separation.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.separation.unsupported"));
        Assert.That(unsupported.Status, Is.EqualTo(ImageMetadataHashStatus.UnsupportedAlgorithm));
        Assert.That(unsupported.AlgorithmName, Is.EqualTo("CRC32"));
        Assert.That(unsupported.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.algorithm.unsupported"));
    }

    public static MetadataRawBlock RawBlock(MetadataFamily family, string blockType, long offset, long length)
    {
        MetadataRawBlock block = new MetadataRawBlock();
        block.Family = family;
        block.BlockType = blockType;
        block.Offset = offset;
        block.Length = length;
        return block;
    }

    public static void AssertInvalidInput(ImageMetadataHashResult result)
    {
        Assert.That(result.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(result.HexDigest, Is.Empty);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("metadata-hash.input.invalid"));
    }
}
