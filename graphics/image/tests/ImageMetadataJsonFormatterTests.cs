using System.Text.Json;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataJsonFormatterTests
{
    [Test]
    public void ToJson_NullDocumentReturnsEmptyObject()
    {
        using (JsonDocument json = JsonDocument.Parse(new ImageMetadataFormatter().ToJson(null)))
        {
            JsonElement root = json.RootElement;

            Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(root.EnumerateObject().MoveNext(), Is.False);
        }
    }

    [Test]
    public void ToJson_IncludesDocumentFormatSummaryFramesAndDiagnostics()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Png);
        document.Format.Confidence = 100;
        document.Format.DetectedByMagic = true;
        document.ByteLength = 42;
        ImageMetadataFrame frame = document.AddFrameWithDimensions(10, 20);
        frame.Role = ImageMetadataRole.Thumbnail;
        frame.ContainerItemId = 12;
        frame.ParentFrameIndex = 2;
        frame.CanvasX = 3;
        frame.CanvasY = 4;
        frame.BitDepth = 8;
        frame.ChannelCount = 4;
        frame.HasAlpha = true;
        frame.PixelAspectRatio = 1.25;
        frame.HorizontalResolution = 300;
        frame.VerticalResolution = 240;
        frame.ResolutionUnit = "pixels/inch";
        frame.SourceColorType = "Truecolor with alpha";
        frame.IsInterlaced = true;
        document.Summary.PixelAspectRatio = 1.25;
        document.Summary.HorizontalResolution = 300;
        document.Summary.VerticalResolution = 240;
        document.Summary.ResolutionUnit = "pixels/inch";
        document.Summary.SourceColorType = "Truecolor with alpha";
        document.Summary.IsInterlaced = true;
        document.Summary.IsAnimated = true;
        document.Summary.AnimationLoopCount = 2;
        document.Summary.TotalDurationMilliseconds = 500;
        document.IsCorrupt = true;
        document.Summary.Author = "Ada";
        document.Summary.Keywords.Add("sample");
        document.AddDiagnosticWithSeverity(MetadataSeverity.Warning, "W001", "warning");

        using (JsonDocument json = JsonDocument.Parse(new ImageMetadataFormatter().ToJson(document)))
        {
            JsonElement root = json.RootElement;
            JsonElement format = root.GetProperty("format");
            JsonElement summary = root.GetProperty("summary");
            JsonElement frames = root.GetProperty("frames");
            JsonElement diagnostics = root.GetProperty("diagnostics");

            Assert.That(format.GetProperty("kind").GetString(), Is.EqualTo("Png"));
            Assert.That(format.GetProperty("mimeType").GetString(), Is.EqualTo("image/png"));
            Assert.That(format.GetProperty("confidence").GetInt32(), Is.EqualTo(100));
            Assert.That(format.GetProperty("detectedByMagic").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("byteLength").GetInt64(), Is.EqualTo(42));
            Assert.That(root.GetProperty("isCorrupt").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("isIncomplete").GetBoolean(), Is.False);
            Assert.That(summary.GetProperty("width").GetInt32(), Is.EqualTo(10));
            Assert.That(summary.GetProperty("height").GetInt32(), Is.EqualTo(20));
            Assert.That(summary.GetProperty("author").GetString(), Is.EqualTo("Ada"));
            Assert.That(summary.GetProperty("keywords")[0].GetString(), Is.EqualTo("sample"));
            Assert.That(summary.GetProperty("pixelAspectRatio").GetDouble(), Is.EqualTo(1.25));
            Assert.That(summary.GetProperty("horizontalResolution").GetDouble(), Is.EqualTo(300));
            Assert.That(summary.GetProperty("verticalResolution").GetDouble(), Is.EqualTo(240));
            Assert.That(summary.GetProperty("resolutionUnit").GetString(), Is.EqualTo("pixels/inch"));
            Assert.That(summary.GetProperty("sourceColorType").GetString(), Is.EqualTo("Truecolor with alpha"));
            Assert.That(summary.GetProperty("isInterlaced").GetBoolean(), Is.True);
            Assert.That(summary.GetProperty("isAnimated").GetBoolean(), Is.True);
            Assert.That(summary.GetProperty("animationLoopCount").GetInt32(), Is.EqualTo(2));
            Assert.That(summary.GetProperty("totalDurationMilliseconds").GetInt64(), Is.EqualTo(500));
            Assert.That(frames.GetArrayLength(), Is.EqualTo(1));
            Assert.That(frames[0].GetProperty("width").GetInt32(), Is.EqualTo(10));
            Assert.That(frames[0].GetProperty("height").GetInt32(), Is.EqualTo(20));
            Assert.That(frames[0].GetProperty("role").GetString(), Is.EqualTo("Thumbnail"));
            Assert.That(frames[0].GetProperty("containerItemId").GetInt64(), Is.EqualTo(12));
            Assert.That(frames[0].GetProperty("parentFrameIndex").GetInt32(), Is.EqualTo(2));
            Assert.That(frames[0].GetProperty("canvasX").GetInt32(), Is.EqualTo(3));
            Assert.That(frames[0].GetProperty("canvasY").GetInt32(), Is.EqualTo(4));
            Assert.That(frames[0].GetProperty("bitDepth").GetInt32(), Is.EqualTo(8));
            Assert.That(frames[0].GetProperty("channelCount").GetInt32(), Is.EqualTo(4));
            Assert.That(frames[0].GetProperty("hasAlpha").GetBoolean(), Is.True);
            Assert.That(frames[0].GetProperty("pixelAspectRatio").GetDouble(), Is.EqualTo(1.25));
            Assert.That(frames[0].GetProperty("resolutionUnit").GetString(), Is.EqualTo("pixels/inch"));
            Assert.That(frames[0].GetProperty("sourceColorType").GetString(), Is.EqualTo("Truecolor with alpha"));
            Assert.That(frames[0].GetProperty("isInterlaced").GetBoolean(), Is.True);
            Assert.That(diagnostics.GetArrayLength(), Is.EqualTo(1));
            Assert.That(diagnostics[0].GetProperty("severity").GetString(), Is.EqualTo("Warning"));
            Assert.That(diagnostics[0].GetProperty("code").GetString(), Is.EqualTo("W001"));
        }
    }

    [Test]
    public void ToJson_IncludesDirectoriesTagsChildrenProfilesAndRawBlocks()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Jpeg);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        MetadataTag make = directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        make.NumericId = 271;
        make.DisplayValue = "Canon";
        make.RawValue = new byte[] { 67, 97, 110, 111, 110 };
        directory.AddTagWithValue("Exif.IFD0.ExposureTime", "Exposure Time", MetadataValue.FromRational(1, 400));
        MetadataDirectory child = directory.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        child.AddTagWithValue("Exif.GPS.Latitude", "Latitude", MetadataValue.FromDouble(45.5));
        MetadataProfile profile = document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "Display P3", new byte[] { 1, 2, 3 });
        profile.Role = ImageMetadataRole.Preview;
        profile.ContainerItemId = 27;
        profile.SourceFrameIndex = 4;
        MetadataRawBlock rawBlock = document.AddRawBlockWithRange(MetadataFamily.Jpeg, "APP1", "Exif", 12, 30);
        rawBlock.Data = new byte[] { 9, 8 };

        using (JsonDocument json = JsonDocument.Parse(new ImageMetadataFormatter().ToJson(document)))
        {
            JsonElement root = json.RootElement;
            JsonElement directories = root.GetProperty("directories");
            JsonElement rootDirectory = directories[0];
            JsonElement tags = rootDirectory.GetProperty("tags");
            JsonElement makeTag = tags[0];
            JsonElement exposureTag = tags[1];
            JsonElement childDirectory = rootDirectory.GetProperty("children")[0];
            JsonElement profiles = root.GetProperty("profiles");
            JsonElement rawBlocks = root.GetProperty("rawBlocks");

            Assert.That(directories.GetArrayLength(), Is.EqualTo(1));
            Assert.That(rootDirectory.GetProperty("family").GetString(), Is.EqualTo("Exif"));
            Assert.That(rootDirectory.GetProperty("group").GetString(), Is.EqualTo("IFD0"));
            Assert.That(makeTag.GetProperty("key").GetString(), Is.EqualTo("Exif.IFD0.Make"));
            Assert.That(makeTag.GetProperty("numericId").GetInt32(), Is.EqualTo(271));
            Assert.That(makeTag.GetProperty("rawValueLength").GetInt32(), Is.EqualTo(5));
            Assert.That(makeTag.GetProperty("value").GetProperty("kind").GetString(), Is.EqualTo("String"));
            Assert.That(makeTag.GetProperty("value").GetProperty("text").GetString(), Is.EqualTo("Canon"));
            Assert.That(exposureTag.GetProperty("value").GetProperty("kind").GetString(), Is.EqualTo("Rational"));
            Assert.That(exposureTag.GetProperty("value").GetProperty("numerator").GetInt64(), Is.EqualTo(1));
            Assert.That(exposureTag.GetProperty("value").GetProperty("denominator").GetInt64(), Is.EqualTo(400));
            Assert.That(childDirectory.GetProperty("family").GetString(), Is.EqualTo("Gps"));
            Assert.That(childDirectory.GetProperty("tags")[0].GetProperty("value").GetProperty("doubleValue").GetDouble(), Is.EqualTo(45.5));
            Assert.That(profiles[0].GetProperty("kind").GetString(), Is.EqualTo("Icc"));
            Assert.That(profiles[0].GetProperty("index").GetInt32(), Is.Zero);
            Assert.That(profiles[0].GetProperty("role").GetString(), Is.EqualTo("Preview"));
            Assert.That(profiles[0].GetProperty("containerItemId").GetInt64(), Is.EqualTo(27));
            Assert.That(profiles[0].GetProperty("sourceFrameIndex").GetInt32(), Is.EqualTo(4));
            Assert.That(profiles[0].GetProperty("byteLength").GetInt32(), Is.EqualTo(3));
            Assert.That(rawBlocks[0].GetProperty("blockType").GetString(), Is.EqualTo("APP1"));
            Assert.That(rawBlocks[0].GetProperty("offset").GetInt64(), Is.EqualTo(12));
            Assert.That(rawBlocks[0].GetProperty("byteLength").GetInt32(), Is.EqualTo(2));
        }
    }

    [Test]
    public void ToJson_WritesSupportedMetadataValuePayloads()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        MetadataValue stringList = new MetadataValue();
        stringList.Kind = MetadataValueKind.StringList;
        stringList.TextValues = new string[] { "one", "two" };
        MetadataDirectory referenced = MetadataDirectory.Create(MetadataFamily.Thumbnail, "thumb", "Preview");

        directory.AddTagWithValue("int", "Integer", MetadataValue.FromInteger(-7));
        directory.AddTagWithValue("uint", "Unsigned", MetadataValue.FromUnsignedInteger(8));
        directory.AddTagWithValue("signed", "Signed Rational", MetadataValue.FromSignedRational(-1, 2));
        directory.AddTagWithValue("date", "Date", MetadataValue.FromDateText("2024:01:02 03:04:05"));
        directory.AddTagWithValue("xml", "XML", MetadataValue.FromXmlText("<x />"));
        directory.AddTagWithValue("list", "List", stringList);
        directory.AddTagWithValue("bytes", "Bytes", MetadataValue.FromBytes(new byte[] { 1, 2, 255 }));
        directory.AddTagWithValue("ref", "Reference", MetadataValue.FromDirectory(referenced));
        directory.AddTagWithValue("bool", "Boolean", MetadataValue.FromBoolean(true));
        directory.AddTagWithValue("ints", "Integers", MetadataValue.FromIntegerArray(new long[] { -1, 2 }));
        directory.AddTagWithValue("uints", "Unsigned Integers", MetadataValue.FromUnsignedIntegerArray(new ulong[] { 3, 4 }));
        directory.AddTagWithValue("rationals", "Rationals", MetadataValue.FromRationalArray(new long[] { 1, 3 }, new long[] { 2, 4 }));
        directory.AddTagWithValue("signedRationals", "Signed Rationals", MetadataValue.FromSignedRationalArray(new long[] { -1 }, new long[] { 2 }));
        directory.AddTagWithValue("doubles", "Doubles", MetadataValue.FromDoubleArray(new double[] { 1.25, -2.5 }));

        using (JsonDocument json = JsonDocument.Parse(new ImageMetadataFormatter().ToJson(document)))
        {
            JsonElement tags = json.RootElement.GetProperty("directories")[0].GetProperty("tags");

            Assert.That(tags[0].GetProperty("value").GetProperty("integerValue").GetInt64(), Is.EqualTo(-7));
            Assert.That(tags[1].GetProperty("value").GetProperty("unsignedIntegerValue").GetUInt64(), Is.EqualTo(8));
            Assert.That(tags[2].GetProperty("value").GetProperty("kind").GetString(), Is.EqualTo("SignedRational"));
            Assert.That(tags[2].GetProperty("value").GetProperty("numerator").GetInt64(), Is.EqualTo(-1));
            Assert.That(tags[3].GetProperty("value").GetProperty("text").GetString(), Is.EqualTo("2024:01:02 03:04:05"));
            Assert.That(tags[4].GetProperty("value").GetProperty("text").GetString(), Is.EqualTo("<x />"));
            Assert.That(tags[5].GetProperty("value").GetProperty("textValues")[1].GetString(), Is.EqualTo("two"));
            Assert.That(tags[6].GetProperty("value").GetProperty("byteLength").GetInt32(), Is.EqualTo(3));
            Assert.That(tags[6].GetProperty("value").GetProperty("hex").GetString(), Is.EqualTo("0102ff"));
            Assert.That(tags[7].GetProperty("value").GetProperty("directoryName").GetString(), Is.EqualTo("Preview"));
            Assert.That(tags[8].GetProperty("value").GetProperty("booleanValue").GetBoolean(), Is.True);
            Assert.That(tags[9].GetProperty("value").GetProperty("integerValues")[0].GetInt64(), Is.EqualTo(-1));
            Assert.That(tags[10].GetProperty("value").GetProperty("unsignedIntegerValues")[1].GetUInt64(), Is.EqualTo(4));
            Assert.That(tags[11].GetProperty("value").GetProperty("numerators")[1].GetInt64(), Is.EqualTo(3));
            Assert.That(tags[11].GetProperty("value").GetProperty("denominators")[1].GetInt64(), Is.EqualTo(4));
            Assert.That(tags[12].GetProperty("value").GetProperty("kind").GetString(), Is.EqualTo("SignedRationalArray"));
            Assert.That(tags[13].GetProperty("value").GetProperty("doubleValues")[1].GetDouble(), Is.EqualTo(-2.5));
        }
    }
}
