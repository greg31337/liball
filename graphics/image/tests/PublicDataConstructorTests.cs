using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicDataConstructorTests
{
    [Test]
    public void GpsAndFormatConstructors_InitializeEveryField()
    {
        GpsCoordinate gps = new GpsCoordinate();
        ImageFormatInfo format = new ImageFormatInfo();

        Assert.That(gps.IsValid, Is.False);
        Assert.That(gps.Latitude, Is.Zero);
        Assert.That(gps.Longitude, Is.Zero);
        Assert.That(gps.Altitude, Is.Zero);
        Assert.That(gps.LatitudeReference, Is.Empty);
        Assert.That(gps.LongitudeReference, Is.Empty);
        Assert.That(gps.AltitudeReference, Is.Empty);
        Assert.That(format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(format.Name, Is.Empty);
        Assert.That(format.MimeType, Is.EqualTo("application/octet-stream"));
        Assert.That(format.PreferredExtension, Is.Empty);
        Assert.That(format.ContainerBrand, Is.Empty);
        Assert.That(format.Confidence, Is.Zero);
        Assert.That(format.DetectedByMagic, Is.False);
        Assert.That(format.Diagnostics, Is.Empty);
    }

    [Test]
    public void DocumentAndFrameConstructors_InitializeEveryField()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = new ImageMetadataFrame();

        Assert.That(document.Format, Is.Not.Null);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.ByteLength, Is.Zero);
        Assert.That(document.IsCorrupt, Is.False);
        Assert.That(document.IsIncomplete, Is.False);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Summary, Is.Not.Null);
        Assert.That(frame.Index, Is.Zero);
        Assert.That(frame.Role, Is.EqualTo(ImageMetadataRole.Unknown));
        Assert.That(frame.ContainerItemId, Is.EqualTo(-1));
        Assert.That(frame.ParentFrameIndex, Is.EqualTo(-1));
        Assert.That(frame.Width, Is.Zero);
        Assert.That(frame.Height, Is.Zero);
        Assert.That(frame.CanvasWidth, Is.Zero);
        Assert.That(frame.CanvasHeight, Is.Zero);
        Assert.That(frame.CanvasX, Is.Zero);
        Assert.That(frame.CanvasY, Is.Zero);
        Assert.That(frame.PixelAspectRatio, Is.Zero);
        Assert.That(frame.HorizontalResolution, Is.Zero);
        Assert.That(frame.VerticalResolution, Is.Zero);
        Assert.That(frame.ResolutionUnit, Is.Empty);
        Assert.That(frame.BitDepth, Is.Zero);
        Assert.That(frame.ChannelCount, Is.Zero);
        Assert.That(frame.HasAlpha, Is.False);
        Assert.That(frame.IsAnimated, Is.False);
        Assert.That(frame.DurationMilliseconds, Is.Zero);
        Assert.That(frame.Compression, Is.EqualTo(ImageCompressionKind.Unknown));
        Assert.That(frame.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Unknown));
        Assert.That(frame.SourceColorType, Is.Empty);
        Assert.That(frame.IsInterlaced, Is.False);
        Assert.That(frame.IsProgressive, Is.False);
        Assert.That(frame.Orientation, Is.EqualTo(ImageOrientation.Unknown));
        Assert.That(frame.Directories, Is.Empty);
    }

    [Test]
    public void SummaryConstructor_InitializesEveryField()
    {
        ImageMetadataSummary summary = new ImageMetadataSummary();

        Assert.That(summary.Width, Is.Zero);
        Assert.That(summary.Height, Is.Zero);
        Assert.That(summary.FrameCount, Is.Zero);
        Assert.That(summary.PixelAspectRatio, Is.Zero);
        Assert.That(summary.HorizontalResolution, Is.Zero);
        Assert.That(summary.VerticalResolution, Is.Zero);
        Assert.That(summary.ResolutionUnit, Is.Empty);
        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Unknown));
        Assert.That(summary.CameraMake, Is.Empty);
        Assert.That(summary.CameraModel, Is.Empty);
        Assert.That(summary.LensModel, Is.Empty);
        Assert.That(summary.CaptureTimeText, Is.Empty);
        Assert.That(summary.CaptureTime, Is.Not.Null);
        Assert.That(summary.CaptureTime.IsValid, Is.False);
        Assert.That(summary.Gps, Is.Not.Null);
        Assert.That(summary.Gps.IsValid, Is.False);
        Assert.That(summary.Author, Is.Empty);
        Assert.That(summary.Copyright, Is.Empty);
        Assert.That(summary.Title, Is.Empty);
        Assert.That(summary.Description, Is.Empty);
        Assert.That(summary.Keywords, Is.Empty);
        Assert.That(summary.Rating, Is.Zero);
        Assert.That(summary.HasColorProfile, Is.False);
        Assert.That(summary.ColorProfileName, Is.Empty);
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Unknown));
        Assert.That(summary.SourceColorType, Is.Empty);
        Assert.That(summary.IsInterlaced, Is.False);
        Assert.That(summary.IsProgressive, Is.False);
        Assert.That(summary.IsAnimated, Is.False);
        Assert.That(summary.AnimationLoopCount, Is.Zero);
        Assert.That(summary.TotalDurationMilliseconds, Is.Zero);
        Assert.That(summary.ResolutionText, Is.Empty);
        Assert.That(summary.Software, Is.Empty);
    }

    [Test]
    public void DirectoryAndTagConstructors_InitializeEveryField()
    {
        MetadataDirectory directory = new MetadataDirectory();
        MetadataTag tag = new MetadataTag();

        Assert.That(directory.Family, Is.EqualTo(MetadataFamily.Unknown));
        Assert.That(directory.Group, Is.Empty);
        Assert.That(directory.Name, Is.Empty);
        Assert.That(directory.Path, Is.Empty);
        Assert.That(directory.FrameIndex, Is.EqualTo(-1));
        Assert.That(directory.Offset, Is.EqualTo(-1));
        Assert.That(directory.Length, Is.Zero);
        Assert.That(directory.RepeatIndex, Is.Zero);
        Assert.That(directory.Tags, Is.Empty);
        Assert.That(directory.Children, Is.Empty);
        Assert.That(tag.Key, Is.Empty);
        Assert.That(tag.Name, Is.Empty);
        Assert.That(tag.Family, Is.EqualTo(MetadataFamily.Unknown));
        Assert.That(tag.Group, Is.Empty);
        Assert.That(tag.NumericId, Is.Zero);
        Assert.That(tag.ValueType, Is.EqualTo(MetadataValueKind.Empty));
        Assert.That(tag.Value, Is.Not.Null);
        Assert.That(tag.Value.Kind, Is.EqualTo(MetadataValueKind.Empty));
        Assert.That(tag.RawValue, Is.Empty);
        Assert.That(tag.DisplayValue, Is.Empty);
        Assert.That(tag.Offset, Is.EqualTo(-1));
        Assert.That(tag.Length, Is.Zero);
        Assert.That(tag.Confidence, Is.Zero);
        Assert.That(tag.RepeatIndex, Is.Zero);
    }

    [Test]
    public void ProfileRawBlockAndDiagnosticConstructors_InitializeEveryField()
    {
        MetadataProfile profile = new MetadataProfile();
        MetadataRawBlock rawBlock = new MetadataRawBlock();
        MetadataDiagnostic diagnostic = new MetadataDiagnostic();

        Assert.That(profile.Index, Is.Zero);
        Assert.That(profile.Role, Is.EqualTo(ImageMetadataRole.Unknown));
        Assert.That(profile.ContainerItemId, Is.EqualTo(-1));
        Assert.That(profile.SourceFrameIndex, Is.EqualTo(-1));
        Assert.That(profile.Kind, Is.EqualTo(MetadataProfileKind.Unknown));
        Assert.That(profile.Family, Is.EqualTo(MetadataFamily.Unknown));
        Assert.That(profile.Key, Is.Empty);
        Assert.That(profile.Name, Is.Empty);
        Assert.That(profile.MimeType, Is.EqualTo("application/octet-stream"));
        Assert.That(profile.Data, Is.Empty);
        Assert.That(profile.Text, Is.Empty);
        Assert.That(profile.Offset, Is.EqualTo(-1));
        Assert.That(profile.Length, Is.Zero);
        Assert.That(profile.SequenceNumber, Is.Zero);
        Assert.That(profile.SequenceCount, Is.Zero);
        Assert.That(rawBlock.Family, Is.EqualTo(MetadataFamily.Unknown));
        Assert.That(rawBlock.BlockType, Is.Empty);
        Assert.That(rawBlock.Name, Is.Empty);
        Assert.That(rawBlock.Offset, Is.EqualTo(-1));
        Assert.That(rawBlock.Length, Is.Zero);
        Assert.That(rawBlock.Data, Is.Empty);
        Assert.That(diagnostic.Severity, Is.EqualTo(MetadataSeverity.Info));
        Assert.That(diagnostic.Code, Is.Empty);
        Assert.That(diagnostic.Message, Is.Empty);
        Assert.That(diagnostic.BlockType, Is.Empty);
        Assert.That(diagnostic.Offset, Is.EqualTo(-1));
        Assert.That(diagnostic.Length, Is.Zero);
    }

    [Test]
    public void MetadataValueConstructor_InitializesEveryField()
    {
        MetadataValue value = new MetadataValue();

        Assert.That(value.Kind, Is.EqualTo(MetadataValueKind.Empty));
        Assert.That(value.IntegerValue, Is.Zero);
        Assert.That(value.UnsignedIntegerValue, Is.Zero);
        Assert.That(value.Numerator, Is.Zero);
        Assert.That(value.Denominator, Is.EqualTo(1));
        Assert.That(value.DoubleValue, Is.Zero);
        Assert.That(value.IntegerValues, Is.Empty);
        Assert.That(value.UnsignedIntegerValues, Is.Empty);
        Assert.That(value.Numerators, Is.Empty);
        Assert.That(value.Denominators, Is.Empty);
        Assert.That(value.DoubleValues, Is.Empty);
        Assert.That(value.Text, Is.Empty);
        Assert.That(value.TextValues, Is.Empty);
        Assert.That(value.Bytes, Is.Empty);
        Assert.That(value.DirectoryValue, Is.Null);
        Assert.That(value.BooleanValue, Is.False);
    }

    [Test]
    public void DateAndDiffConstructors_InitializeEveryField()
    {
        MetadataDate date = new MetadataDate();
        ImageMetadataDiffResult diff = new ImageMetadataDiffResult();

        Assert.That(date.IsValid, Is.False);
        Assert.That(date.OriginalText, Is.Empty);
        Assert.That(date.Precision, Is.EqualTo(MetadataDatePrecision.Unknown));
        Assert.That(date.Year, Is.Zero);
        Assert.That(date.Month, Is.Zero);
        Assert.That(date.Day, Is.Zero);
        Assert.That(date.Hour, Is.Zero);
        Assert.That(date.Minute, Is.Zero);
        Assert.That(date.Second, Is.Zero);
        Assert.That(date.FractionalSecondText, Is.Empty);
        Assert.That(date.HasTimeZone, Is.False);
        Assert.That(date.TimeZoneOffsetMinutes, Is.Zero);
        Assert.That(diff.AreEqual, Is.True);
        Assert.That(diff.Differences, Is.Empty);
    }

    [Test]
    public void HashConstructors_InitializeEveryField()
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
    public void ScanConstructors_InitializeEveryField()
    {
        ImageMetadataScanOptions options = new ImageMetadataScanOptions();
        ImageMetadataScanFileResult file = new ImageMetadataScanFileResult();
        ImageMetadataScanResult result = new ImageMetadataScanResult();

        Assert.That(options.Recursive, Is.False);
        Assert.That(options.Extensions, Is.Empty);
        Assert.That(options.Formats, Is.Empty);
        Assert.That(options.ReaderOptions, Is.Not.Null);
        Assert.That(options.ReaderOptions.MaxFileBytes, Is.EqualTo(536870912));
        Assert.That(file.Path, Is.Empty);
        Assert.That(file.IsSuccess, Is.False);
        Assert.That(file.Document, Is.Not.Null);
        Assert.That(file.Document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(file.Diagnostics, Is.Empty);
        Assert.That(result.Files, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ReaderOptionsConstructor_InitializesEveryField()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();

        Assert.That(options.MaxFileBytes, Is.EqualTo(536870912));
        Assert.That(options.MaxSegmentBytes, Is.EqualTo(67108864));
        Assert.That(options.MaxRawBlockBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxProfileBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxXmlBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxDecompressedBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxIfdCount, Is.EqualTo(4096));
        Assert.That(options.MaxNestingDepth, Is.EqualTo(64));
        Assert.That(options.MaxTagCount, Is.EqualTo(65536));
        Assert.That(options.MaxFrameCount, Is.EqualTo(10000));
        Assert.That(options.CaptureRawBlocks, Is.True);
        Assert.That(options.CaptureRawBlockData, Is.False);
        Assert.That(options.ParseXmp, Is.True);
        Assert.That(options.ParseIcc, Is.True);
        Assert.That(options.PreserveMakerNotes, Is.True);
        Assert.That(options.DecodeMakerNotes, Is.False);
        Assert.That(options.DiagnosticsVerbosity, Is.EqualTo(MetadataDiagnosticsVerbosity.Normal));
    }
}
