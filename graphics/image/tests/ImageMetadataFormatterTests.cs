using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataFormatterTests
{
    [Test]
    public void ToText_IncludesDocumentHeaderAndCounts()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Png);
        document.ByteLength = 42;
        document.IsCorrupt = true;
        ImageMetadataFrame frame = document.AddFrameWithDimensions(10, 20);
        frame.SourceColorType = "Truecolor";
        frame.PixelAspectRatio = 1.5;

        string text = new ImageMetadataFormatter().ToText(document);

        Assert.That(text, Does.Contain("Format: PNG"));
        Assert.That(text, Does.Contain("Kind: Png"));
        Assert.That(text, Does.Contain("MIME: image/png"));
        Assert.That(text, Does.Contain("Extension: .png"));
        Assert.That(text, Does.Contain("Byte length: 42"));
        Assert.That(text, Does.Contain("Corrupt: True"));
        Assert.That(text, Does.Contain("Incomplete: False"));
        Assert.That(text, Does.Contain("Frames: 1"));
        Assert.That(text, Does.Contain("- Frame 0 Primary: 10x20 at 0,0"));
        Assert.That(text, Does.Contain("color type Truecolor, aspect 1.5"));
    }

    [Test]
    public void ToText_IncludesDirectoriesTagsChildrenProfilesRawBlocksAndDiagnostics()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        directory.AddTagWithValue("Exif.IFD0.ExposureTime", "Exposure Time", MetadataValue.FromRational(1, 400));
        MetadataDirectory child = directory.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        child.AddTagWithValue("Exif.GPS.Latitude", "Latitude", MetadataValue.FromDouble(45.5));
        document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "Display P3", new byte[] { 1, 2, 3 });
        document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP1", "Exif", 12, 30);
        document.AddDiagnosticWithSeverity(MetadataSeverity.Warning, "W001", "warning");

        string text = new ImageMetadataFormatter().ToText(document);

        Assert.That(text, Does.Contain("[Exif/IFD0] Image File Directory 0"));
        Assert.That(text, Does.Contain("Exif.IFD0.Make (Make) = Canon"));
        Assert.That(text, Does.Contain("Exif.IFD0.ExposureTime (Exposure Time) = 1/400"));
        Assert.That(text, Does.Contain("[Gps/GPS] GPS IFD"));
        Assert.That(text, Does.Contain("Exif.GPS.Latitude (Latitude) = 45.5"));
        Assert.That(text, Does.Contain("Profiles: 1"));
        Assert.That(text, Does.Contain("- 0 Unknown Icc Display P3 3 bytes"));
        Assert.That(text, Does.Contain("Raw blocks: 1"));
        Assert.That(text, Does.Contain("- APP1 Exif @12+30"));
        Assert.That(text, Does.Contain("Diagnostics: 1"));
        Assert.That(text, Does.Contain("- Warning W001: warning"));
    }

    [Test]
    public void FormatValue_FormatsSupportedValueKinds()
    {
        MetadataValue stringList = new MetadataValue();
        stringList.Kind = MetadataValueKind.StringList;
        stringList.TextValues = new string[] { "one", "two" };

        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromInteger(-1)), Is.EqualTo("-1"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromUnsignedInteger(2)), Is.EqualTo("2"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromSignedRational(-1, 2)), Is.EqualTo("-1/2"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromDouble(1.25)), Is.EqualTo("1.25"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromIntegerArray(new long[] { -1, 2 })), Is.EqualTo("-1, 2"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromUnsignedIntegerArray(new ulong[] { 3, 4 })), Is.EqualTo("3, 4"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromRationalArray(new long[] { 1, 3 }, new long[] { 2, 4 })), Is.EqualTo("1/2, 3/4"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromSignedRationalArray(new long[] { -1 }, new long[] { 2 })), Is.EqualTo("-1/2"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromDoubleArray(new double[] { 1.25, -2.5 })), Is.EqualTo("1.25, -2.5"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromDateText("2024:01:02 03:04:05")), Is.EqualTo("2024:01:02 03:04:05"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromBytes(new byte[] { 1, 2 })), Is.EqualTo("2 bytes"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(MetadataValue.FromBoolean(true)), Is.EqualTo("true"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(stringList), Is.EqualTo("one, two"));
        Assert.That(ImageMetadataFormatterCore.FormatValue(null), Is.EqualTo(""));
    }

    [Test]
    public void ToText_ReturnsEmptyForNullDocument()
    {
        Assert.That(new ImageMetadataFormatter().ToText(null), Is.EqualTo(""));
    }
}
