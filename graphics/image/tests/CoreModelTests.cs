using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class CoreModelTests
{
    [Test]
    public void DocumentConstructor_InitializesNestedCollectionsAndDefaults()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();

        Assert.That(document.Format, Is.Not.Null);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(document.Format.MimeType, Is.EqualTo("application/octet-stream"));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.Directories, Is.Empty);
        Assert.That(document.Profiles, Is.Empty);
        Assert.That(document.RawBlocks, Is.Empty);
        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.IsCorrupt, Is.False);
        Assert.That(document.IsIncomplete, Is.False);
        Assert.That(document.Summary, Is.Not.Null);
        Assert.That(document.Summary.Gps, Is.Not.Null);
        Assert.That(document.Summary.Keywords, Is.Empty);
    }

    [Test]
    public void MetadataDirectoryAndTag_FactoriesCaptureIdentityAndValueType()
    {
        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        MetadataValue value = MetadataValue.FromString("Canon");
        MetadataTag tag = MetadataTag.Create("Exif.IFD0.Make", "Make", MetadataFamily.Exif, value);

        directory.Tags.Add(tag);

        Assert.That(directory.Family, Is.EqualTo(MetadataFamily.Exif));
        Assert.That(directory.Group, Is.EqualTo("IFD0"));
        Assert.That(directory.Name, Is.EqualTo("Image File Directory 0"));
        Assert.That(directory.FrameIndex, Is.EqualTo(-1));
        Assert.That(directory.Tags, Has.Count.EqualTo(1));
        Assert.That(tag.Key, Is.EqualTo("Exif.IFD0.Make"));
        Assert.That(tag.ValueType, Is.EqualTo(MetadataValueKind.String));
        Assert.That(tag.Value.Text, Is.EqualTo("Canon"));
    }

    [Test]
    public void MetadataValueFactories_PreserveTypedPayloads()
    {
        MetadataValue integerValue = MetadataValue.FromInteger(-12);
        MetadataValue unsignedValue = MetadataValue.FromUnsignedInteger(12);
        MetadataValue rationalValue = MetadataValue.FromRational(1, 400);
        MetadataValue bytesValue = MetadataValue.FromBytes(new byte[] { 1, 2, 3 });
        MetadataValue booleanValue = MetadataValue.FromBoolean(true);
        MetadataValue empty = new MetadataValue();

        Assert.That(integerValue.Kind, Is.EqualTo(MetadataValueKind.Integer));
        Assert.That(integerValue.IntegerValue, Is.EqualTo(-12));
        Assert.That(unsignedValue.Kind, Is.EqualTo(MetadataValueKind.UnsignedInteger));
        Assert.That(unsignedValue.UnsignedIntegerValue, Is.EqualTo(12));
        Assert.That(rationalValue.Kind, Is.EqualTo(MetadataValueKind.Rational));
        Assert.That(rationalValue.Numerator, Is.EqualTo(1));
        Assert.That(rationalValue.Denominator, Is.EqualTo(400));
        Assert.That(bytesValue.Kind, Is.EqualTo(MetadataValueKind.ByteArray));
        Assert.That(bytesValue.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(booleanValue.Kind, Is.EqualTo(MetadataValueKind.Boolean));
        Assert.That(booleanValue.BooleanValue, Is.True);
        Assert.That(empty.IntegerValues, Is.Empty);
        Assert.That(empty.UnsignedIntegerValues, Is.Empty);
        Assert.That(empty.Numerators, Is.Empty);
        Assert.That(empty.Denominators, Is.Empty);
        Assert.That(empty.DoubleValues, Is.Empty);
        Assert.That(empty.TextValues, Is.Empty);
    }

    [Test]
    public void ImageFormatInfoFactory_CapturesIdentityAndPreservesDefaults()
    {
        ImageFormatInfo info = ImageFormatInfo.Create(ImageFormatKind.Png, "PNG", "image/png", ".png");

        Assert.That(info.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(info.Name, Is.EqualTo("PNG"));
        Assert.That(info.MimeType, Is.EqualTo("image/png"));
        Assert.That(info.PreferredExtension, Is.EqualTo(".png"));
        Assert.That(info.ContainerBrand, Is.Empty);
        Assert.That(info.Confidence, Is.Zero);
        Assert.That(info.DetectedByMagic, Is.False);
    }

    [Test]
    public void FrameSummaryDiagnosticProfileAndRawBlock_HaveStableDefaults()
    {
        ImageMetadataFrame frame = new ImageMetadataFrame();
        ImageMetadataSummary summary = new ImageMetadataSummary();
        MetadataDiagnostic diagnostic = MetadataDiagnostic.Create(MetadataSeverity.Warning, "W001", "warning");
        MetadataProfile profile = new MetadataProfile();
        MetadataRawBlock rawBlock = new MetadataRawBlock();

        Assert.That(frame.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Unknown));
        Assert.That(frame.Role, Is.EqualTo(ImageMetadataRole.Unknown));
        Assert.That(frame.ContainerItemId, Is.EqualTo(-1));
        Assert.That(frame.ParentFrameIndex, Is.EqualTo(-1));
        Assert.That(frame.CanvasX, Is.Zero);
        Assert.That(frame.CanvasY, Is.Zero);
        Assert.That(frame.PixelAspectRatio, Is.Zero);
        Assert.That(frame.HorizontalResolution, Is.Zero);
        Assert.That(frame.VerticalResolution, Is.Zero);
        Assert.That(frame.ResolutionUnit, Is.Empty);
        Assert.That(frame.SourceColorType, Is.Empty);
        Assert.That(frame.IsInterlaced, Is.False);
        Assert.That(frame.IsProgressive, Is.False);
        Assert.That(frame.Compression, Is.EqualTo(ImageCompressionKind.Unknown));
        Assert.That(frame.Orientation, Is.EqualTo(ImageOrientation.Unknown));
        Assert.That(frame.Directories, Is.Empty);
        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Unknown));
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.Unknown));
        Assert.That(summary.PixelAspectRatio, Is.Zero);
        Assert.That(summary.HorizontalResolution, Is.Zero);
        Assert.That(summary.VerticalResolution, Is.Zero);
        Assert.That(summary.ResolutionUnit, Is.Empty);
        Assert.That(summary.SourceColorType, Is.Empty);
        Assert.That(summary.IsInterlaced, Is.False);
        Assert.That(summary.IsProgressive, Is.False);
        Assert.That(summary.IsAnimated, Is.False);
        Assert.That(summary.AnimationLoopCount, Is.Zero);
        Assert.That(summary.TotalDurationMilliseconds, Is.Zero);
        Assert.That(diagnostic.Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(diagnostic.Code, Is.EqualTo("W001"));
        Assert.That(profile.Kind, Is.EqualTo(MetadataProfileKind.Unknown));
        Assert.That(profile.Role, Is.EqualTo(ImageMetadataRole.Unknown));
        Assert.That(profile.ContainerItemId, Is.EqualTo(-1));
        Assert.That(profile.SourceFrameIndex, Is.EqualTo(-1));
        Assert.That(rawBlock.Offset, Is.EqualTo(-1));
    }

    [Test]
    public void DocumentHelpers_AssignFrameAndProfileIdentityAndRelationships()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame primary = document.AddFrameWithDimensions(100, 80);
        ImageMetadataFrame page = new ImageMetadataFrame();
        page.Role = ImageMetadataRole.Page;
        document.AddFrame(page);
        MetadataProfile thumbnail = document.AddProfileWithData(MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, "thumb", "Thumbnail", new byte[] { 1 });
        MetadataProfile preview = new MetadataProfile();
        preview.Kind = MetadataProfileKind.Thumbnail;
        preview.Role = ImageMetadataRole.Preview;
        preview.SourceFrameIndex = 1;
        document.AddProfile(preview);

        Assert.That(primary.Index, Is.Zero);
        Assert.That(primary.Role, Is.EqualTo(ImageMetadataRole.Primary));
        Assert.That(page.Index, Is.EqualTo(1));
        Assert.That(page.Role, Is.EqualTo(ImageMetadataRole.Page));
        Assert.That(thumbnail.Index, Is.Zero);
        Assert.That(thumbnail.Role, Is.EqualTo(ImageMetadataRole.Thumbnail));
        Assert.That(thumbnail.SourceFrameIndex, Is.Zero);
        Assert.That(preview.Index, Is.EqualTo(1));
        Assert.That(preview.Role, Is.EqualTo(ImageMetadataRole.Preview));
        Assert.That(preview.SourceFrameIndex, Is.EqualTo(1));
    }
}
