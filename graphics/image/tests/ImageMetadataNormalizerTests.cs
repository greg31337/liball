using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataNormalizerTests
{
    [Test]
    public void BuildSummary_UsesFramesProfilesAndExifCameraFields()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame firstFrame = document.AddFrameWithDimensions(640, 480);
        firstFrame.ColorSpace = ImageColorSpaceKind.DisplayP3;
        document.AddFrameWithDimensions(320, 240);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        directory.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        directory.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("R5"));
        directory.AddTagWithValue("Exif.Photo.LensModel", "Lens Model", MetadataValue.FromString("RF 50mm"));
        directory.AddTagWithValue("Exif.Photo.DateTimeOriginal", "Date Time Original", MetadataValue.FromDateText("2024:01:02 03:04:05"));
        directory.AddTagWithValue("Exif.IFD0.Software", "Software", MetadataValue.FromString("image app"));
        document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "Display P3", new byte[] { 1, 2, 3 });

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary, Is.SameAs(document.Summary));
        Assert.That(summary.Width, Is.EqualTo(640));
        Assert.That(summary.Height, Is.EqualTo(480));
        Assert.That(summary.FrameCount, Is.EqualTo(2));
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(summary.HasColorProfile, Is.True);
        Assert.That(summary.ColorProfileName, Is.EqualTo("Display P3"));
        Assert.That(summary.CameraMake, Is.EqualTo("Canon"));
        Assert.That(summary.CameraModel, Is.EqualTo("R5"));
        Assert.That(summary.LensModel, Is.EqualTo("RF 50mm"));
        Assert.That(summary.CaptureTimeText, Is.EqualTo("2024:01:02 03:04:05"));
        Assert.That(summary.Software, Is.EqualTo("image app"));
    }

    [Test]
    public void BuildSummary_NormalizesCommonFrameAndAnimationFields()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame first = document.AddFrameWithDimensions(100, 80);
        first.PixelAspectRatio = 1.25;
        first.HorizontalResolution = 300;
        first.VerticalResolution = 240;
        first.ResolutionUnit = "pixels/inch";
        first.SourceColorType = "Truecolor with alpha";
        first.IsProgressive = true;
        first.IsAnimated = true;
        first.DurationMilliseconds = 125;
        ImageMetadataFrame second = document.AddFrameWithDimensions(100, 80);
        second.IsAnimated = true;
        second.DurationMilliseconds = 75;
        MetadataDirectory animation = document.AddDirectoryWithName(MetadataFamily.Container, "Animation", "Animation");
        animation.AddTagWithValue("WebP.ANIM.LoopCount", "Loop Count", MetadataValue.FromInteger(3));
        MetadataDirectory png = document.AddDirectoryWithName(MetadataFamily.Png, "IHDR", "PNG Header");
        png.AddTagWithValue("Png.IHDR.InterlaceMethod", "Interlace Method", MetadataValue.FromInteger(1));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.PixelAspectRatio, Is.EqualTo(1.25));
        Assert.That(summary.HorizontalResolution, Is.EqualTo(300));
        Assert.That(summary.VerticalResolution, Is.EqualTo(240));
        Assert.That(summary.ResolutionUnit, Is.EqualTo("pixels/inch"));
        Assert.That(summary.SourceColorType, Is.EqualTo("Truecolor with alpha"));
        Assert.That(summary.IsInterlaced, Is.True);
        Assert.That(summary.IsProgressive, Is.True);
        Assert.That(summary.IsAnimated, Is.True);
        Assert.That(summary.AnimationLoopCount, Is.EqualTo(3));
        Assert.That(summary.TotalDurationMilliseconds, Is.EqualTo(200));
        Assert.That(second.PixelAspectRatio, Is.EqualTo(1.25));
        Assert.That(second.HorizontalResolution, Is.EqualTo(300));
        Assert.That(second.ResolutionUnit, Is.EqualTo("pixels/inch"));
        Assert.That(second.SourceColorType, Is.EqualTo("Truecolor with alpha"));
    }

    [Test]
    public void BuildSummary_DerivesCorruptAndIncompleteStatusFromDiagnostics()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Diagnostics.Add(MetadataDiagnostic.Create(MetadataSeverity.Warning, "warning", "Warning"));
        document.Diagnostics.Add(MetadataDiagnostic.Create(MetadataSeverity.RecoverableError, "invalid", "Invalid"));

        new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.False);

        document.Diagnostics.Add(MetadataDiagnostic.Create(MetadataSeverity.Truncated, "truncated", "Truncated"));
        new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(document.IsCorrupt, Is.True);
        Assert.That(document.IsIncomplete, Is.True);
    }

    [TestCase("RGB ", ImageColorSpaceKind.Rgb)]
    [TestCase("GRAY", ImageColorSpaceKind.Gray)]
    [TestCase("CMYK", ImageColorSpaceKind.Cmyk)]
    [TestCase("Lab ", ImageColorSpaceKind.Lab)]
    [TestCase("XYZ ", ImageColorSpaceKind.Xyz)]
    [TestCase("YCbr", ImageColorSpaceKind.YCbCr)]
    [TestCase("HSV ", ImageColorSpaceKind.Hsv)]
    [TestCase("HLS ", ImageColorSpaceKind.Hsl)]
    [TestCase("unknown", ImageColorSpaceKind.Unknown)]
    public void BuildSummary_NormalizesIccDataColorSpace(string signature, ImageColorSpaceKind expected)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = document.AddFrameWithDimensions(10, 10);
        frame.ColorSpace = ImageColorSpaceKind.YCbCr;
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Icc, "Header", "ICC Header");
        directory.AddTagWithValue("Icc.Header.ColorSpace", "Color Space", MetadataValue.FromString(signature));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.ColorSpace, Is.EqualTo(expected == ImageColorSpaceKind.Unknown ? ImageColorSpaceKind.YCbCr : expected));
    }

    [TestCase("Display P3", ImageColorSpaceKind.DisplayP3)]
    [TestCase("Adobe RGB (1998)", ImageColorSpaceKind.AdobeRgb)]
    [TestCase("ProPhoto RGB", ImageColorSpaceKind.ProPhotoRgb)]
    [TestCase("Wide Gamut RGB", ImageColorSpaceKind.WideGamutRgb)]
    public void BuildSummary_UsesNamedRgbIccProfile(string name, ImageColorSpaceKind expected)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "ICC Profile", new byte[] { 1 });
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Icc, "Header", "ICC Header");
        directory.AddTagWithValue("Icc.Header.ColorSpace", "Color Space", MetadataValue.FromString("RGB "));
        directory.AddTagWithValue("Icc.desc.Description", "Description", MetadataValue.FromString(name));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.ColorProfileName, Is.EqualTo(name));
        Assert.That(summary.ColorSpace, Is.EqualTo(expected));
    }

    [TestCase("Png.cICP.ColorPrimaries", 12, ImageColorSpaceKind.DisplayP3)]
    [TestCase("Bmff.Item.ColorPrimaries", 9, ImageColorSpaceKind.WideGamutRgb)]
    [TestCase("JpegXl.Codestream.Primaries", 11, ImageColorSpaceKind.DisplayP3)]
    [TestCase("Jpeg2000.colr.EnumeratedColorSpace", 21, ImageColorSpaceKind.ProPhotoRgb)]
    [TestCase("Png.IHDR.ColorType", 0, ImageColorSpaceKind.Gray)]
    [TestCase("JpegXl.Codestream.ColorSpace", 1, ImageColorSpaceKind.Gray)]
    public void BuildSummary_NormalizesNativeColorFields(string key, int value, ImageColorSpaceKind expected)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Container, "Color", "Color");
        directory.AddTagWithValue(key, "Color", MetadataValue.FromInteger(value));
        if (key == "Png.cICP.ColorPrimaries")
        {
            directory.AddTagWithValue("Png.cICP.MatrixCoefficients", "Matrix", MetadataValue.FromInteger(0));
        }

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.ColorSpace, Is.EqualTo(expected));
    }

    [TestCase("Png.iCCP.ProfileName")]
    [TestCase("WebP.ICCP.ProfileByteLength")]
    [TestCase("Jpeg2000.colr.IccProfileByteLength")]
    public void BuildSummary_RecognizesDeclaredProfilesWhenIccBytesAreNotParsed(string key)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Container, "Color", "Color");
        directory.AddTagWithValue(key, "ICC", MetadataValue.FromString("Declared"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.HasColorProfile, Is.True);
    }

    [Test]
    public void BuildSummary_RecognizesHeifAndJpegXlIccDeclarations()
    {
        ImageMetadataDocument heif = new ImageMetadataDocument();
        MetadataDirectory heifDirectory = heif.AddDirectoryWithName(MetadataFamily.Bmff, "Item", "Item");
        heifDirectory.AddTagWithValue("Bmff.Item.ColorType", "Color Type", MetadataValue.FromString("prof"));
        Assert.That(new ImageMetadataNormalizer().BuildSummary(heif).HasColorProfile, Is.True);

        ImageMetadataDocument jpegXl = new ImageMetadataDocument();
        MetadataDirectory jpegXlDirectory = jpegXl.AddDirectoryWithName(MetadataFamily.JpegXl, "Codestream", "Codestream");
        jpegXlDirectory.AddTagWithValue("JpegXl.Codestream.WantIcc", "ICC", MetadataValue.FromBoolean(true));
        Assert.That(new ImageMetadataNormalizer().BuildSummary(jpegXl).HasColorProfile, Is.True);
    }

    [Test]
    public void BuildSummary_PngCicpTakesPrecedenceOverIcc()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "icc", "ICC Profile", new byte[] { 1 });
        MetadataDirectory icc = document.AddDirectoryWithName(MetadataFamily.Icc, "Header", "ICC Header");
        icc.AddTagWithValue("Icc.Header.ColorSpace", "Color Space", MetadataValue.FromString("GRAY"));
        MetadataDirectory png = document.AddDirectoryWithName(MetadataFamily.Png, "cICP", "PNG cICP");
        png.AddTagWithValue("Png.cICP.ColorPrimaries", "Primaries", MetadataValue.FromInteger(12));
        png.AddTagWithValue("Png.cICP.MatrixCoefficients", "Matrix", MetadataValue.FromInteger(0));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.HasColorProfile, Is.True);
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
    }

    [Test]
    public void BuildSummary_MapsOrientationRatingResolutionAndTextFields()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        exif.AddTagWithValue("Exif.IFD0.Orientation", "Orientation", MetadataValue.FromUnsignedInteger(6));
        exif.AddTagWithValue("Exif.IFD0.XResolution", "X Resolution", MetadataValue.FromRational(300, 1));
        exif.AddTagWithValue("Exif.IFD0.YResolution", "Y Resolution", MetadataValue.FromRational(300, 1));
        exif.AddTagWithValue("Exif.IFD0.ResolutionUnit", "Resolution Unit", MetadataValue.FromString("inches"));
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        xmp.AddTagWithValue("Xmp.dc.creator", "creator", MetadataValue.FromString("Ada"));
        xmp.AddTagWithValue("Xmp.dc.rights", "rights", MetadataValue.FromString("Copyright 2026"));
        xmp.AddTagWithValue("Xmp.dc.title", "title", MetadataValue.FromString("Title"));
        xmp.AddTagWithValue("Xmp.dc.description", "description", MetadataValue.FromString("Description"));
        MetadataDirectory xmpRoot = document.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        xmpRoot.AddTagWithValue("Xmp.xmp.Rating", "Rating", MetadataValue.FromInteger(4));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Rotate90));
        Assert.That(summary.ResolutionText, Is.EqualTo("300/1 x 300/1 inches"));
        Assert.That(summary.HorizontalResolution, Is.EqualTo(300));
        Assert.That(summary.VerticalResolution, Is.EqualTo(300));
        Assert.That(summary.ResolutionUnit, Is.EqualTo("inches"));
        Assert.That(summary.Author, Is.EqualTo("Ada"));
        Assert.That(summary.Copyright, Is.EqualTo("Copyright 2026"));
        Assert.That(summary.Title, Is.EqualTo("Title"));
        Assert.That(summary.Description, Is.EqualTo("Description"));
        Assert.That(summary.Rating, Is.EqualTo(4));
    }

    [TestCase("JpegXl.Codestream.Orientation", 7, ImageOrientation.MirrorHorizontalRotate90)]
    [TestCase("Bmff.Item.Orientation", 5, ImageOrientation.MirrorHorizontalRotate270)]
    [TestCase("Radiance.Orientation", 8, ImageOrientation.Rotate270)]
    [TestCase("Xmp.tiff.Orientation", 2, ImageOrientation.MirrorHorizontal)]
    [TestCase("Xmp/TIFF/Orientation", 4, ImageOrientation.MirrorVertical)]
    public void BuildSummary_NormalizesContainerAndXmpOrientationTags(string key, int value, ImageOrientation expected)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Container, "Orientation", "Orientation");
        directory.AddTagWithValue(key, "Orientation", MetadataValue.FromInteger(value));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Orientation, Is.EqualTo(expected));
    }

    [Test]
    public void BuildSummary_PrefersContainerFrameOrientationOverExifFallback()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = document.AddFrameWithDimensions(640, 480);
        frame.Orientation = ImageOrientation.Rotate180;
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        exif.AddTagWithValue("Exif.IFD0.Orientation", "Orientation", MetadataValue.FromUnsignedInteger(6));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Rotate180));
    }

    [Test]
    public void BuildSummary_SkipsInvalidContainerValuesAndUsesExifFallback()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = document.AddFrameWithDimensions(640, 480);
        frame.Orientation = (ImageOrientation)99;
        MetadataDirectory container = document.AddDirectoryWithName(MetadataFamily.Container, "Codestream", "Codestream");
        container.AddTagWithValue("JpegXl.Codestream.Orientation", "Orientation", MetadataValue.FromInteger(9));
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Image File Directory 0");
        exif.AddTagWithValue("Exif.IFD0.Orientation", "Orientation", MetadataValue.FromUnsignedInteger(6));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Rotate90));
    }

    [TestCase(0, -1, ImageOrientation.Normal)]
    [TestCase(0, 0, ImageOrientation.MirrorVertical)]
    [TestCase(0, 1, ImageOrientation.MirrorHorizontal)]
    [TestCase(90, -1, ImageOrientation.Rotate270)]
    [TestCase(90, 0, ImageOrientation.MirrorHorizontalRotate270)]
    [TestCase(90, 1, ImageOrientation.MirrorHorizontalRotate90)]
    [TestCase(180, -1, ImageOrientation.Rotate180)]
    [TestCase(180, 0, ImageOrientation.MirrorHorizontal)]
    [TestCase(180, 1, ImageOrientation.MirrorVertical)]
    [TestCase(270, -1, ImageOrientation.Rotate90)]
    [TestCase(270, 0, ImageOrientation.MirrorHorizontalRotate90)]
    [TestCase(270, 1, ImageOrientation.MirrorHorizontalRotate270)]
    [TestCase(45, -1, ImageOrientation.Unknown)]
    [TestCase(0, 2, ImageOrientation.Unknown)]
    public void OrientationFromHeifTransform_MapsRotationThenMirror(int rotation, int mirrorAxis, ImageOrientation expected)
    {
        Assert.That(ImageMetadataNormalizerCore.OrientationFromHeifTransform(rotation, mirrorAxis), Is.EqualTo(expected));
    }

    [Test]
    public void BuildSummary_MapsKeywordsAndGpsFields()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        MetadataValue keywords = new MetadataValue();
        keywords.Kind = MetadataValueKind.StringList;
        keywords.TextValues = new string[] { "one", "two", "one" };
        xmp.AddTagWithValue("Xmp.dc.subject", "subject", keywords);
        MetadataDirectory gps = document.AddDirectoryWithName(MetadataFamily.Gps, "GPS", "GPS IFD");
        gps.AddTagWithValue("Exif.GPS.Latitude", "Latitude", MetadataValue.FromDouble(45.5));
        gps.AddTagWithValue("Exif.GPS.Longitude", "Longitude", MetadataValue.FromDouble(-122.25));
        gps.AddTagWithValue("Exif.GPS.Altitude", "Altitude", MetadataValue.FromRational(125, 2));
        gps.AddTagWithValue("Exif.GPS.LatitudeRef", "Latitude Ref", MetadataValue.FromString("N"));
        gps.AddTagWithValue("Exif.GPS.LongitudeRef", "Longitude Ref", MetadataValue.FromString("W"));
        gps.AddTagWithValue("Exif.GPS.AltitudeRef", "Altitude Ref", MetadataValue.FromString("Above Sea Level"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Keywords, Has.Count.EqualTo(2));
        Assert.That(summary.Keywords[0], Is.EqualTo("one"));
        Assert.That(summary.Keywords[1], Is.EqualTo("two"));
        Assert.That(summary.Gps.IsValid, Is.True);
        Assert.That(summary.Gps.Latitude, Is.EqualTo(45.5));
        Assert.That(summary.Gps.Longitude, Is.EqualTo(-122.25));
        Assert.That(summary.Gps.Altitude, Is.EqualTo(62.5));
        Assert.That(summary.Gps.LatitudeReference, Is.EqualTo("N"));
        Assert.That(summary.Gps.LongitudeReference, Is.EqualTo("W"));
        Assert.That(summary.Gps.AltitudeReference, Is.EqualTo("Above Sea Level"));
    }

    [Test]
    public void BuildSummary_SkipsBlankTextAndUsesAlternateMetadataFamilies()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        xmp.AddTagWithValue("Xmp.dc.creator", "Creator", MetadataValue.FromString(""));
        xmp.AddTagWithValue("Xmp.dc.rights", "Rights", MetadataValue.FromString("   "));
        xmp.AddTagWithValue("Xmp.dc.title", "Title", MetadataValue.FromString(""));
        xmp.AddTagWithValue("Xmp.dc.description", "Description", MetadataValue.FromString(""));
        MetadataDirectory iptc = document.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC Application 2");
        iptc.AddTagWithValue("Iptc.Application2.Byline", "Byline", MetadataValue.FromString("Ada"));
        iptc.AddTagWithValue("Iptc.Application2.CopyrightNotice", "Copyright", MetadataValue.FromString("Copyright 2026"));
        iptc.AddTagWithValue("Iptc.Application2.Headline", "Headline", MetadataValue.FromString("Fallback title"));
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "ExifIFD", "EXIF");
        exif.AddTagWithValue("Exif.ExifIFD.UserComment", "User Comment", MetadataValue.FromString("Fallback description"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Author, Is.EqualTo("Ada"));
        Assert.That(summary.Copyright, Is.EqualTo("Copyright 2026"));
        Assert.That(summary.Title, Is.EqualTo("Fallback title"));
        Assert.That(summary.Description, Is.EqualTo("Fallback description"));
    }

    [Test]
    public void BuildSummary_AggregatesTrimsAndDeduplicatesKeywords()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        MetadataValue subjects = new MetadataValue();
        subjects.Kind = MetadataValueKind.StringList;
        subjects.TextValues = new string[] { " Travel ", "night" };
        xmp.AddTagWithValue("Xmp.dc.subject", "Subject", subjects);
        MetadataDirectory iptc = document.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC Application 2");
        iptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("Night"));
        iptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("portrait"));
        MetadataDirectory png = document.AddDirectoryWithName(MetadataFamily.Png, "Text", "PNG Text");
        png.AddTagWithValue("Png.Text.Keywords", "Keywords", MetadataValue.FromString("studio; travel; "));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Keywords, Is.EqualTo(new[] { "Travel", "night", "portrait", "studio" }));
    }

    [Test]
    public void BuildSummary_CombinesIptcSoftwareNameAndVersion()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory iptc = document.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC Application 2");
        iptc.AddTagWithValue("Iptc.Application2.OriginatingProgram", "Program", MetadataValue.FromString("Photo Desk"));
        iptc.AddTagWithValue("Iptc.Application2.ProgramVersion", "Version", MetadataValue.FromString("4.2"));

        Assert.That(new ImageMetadataNormalizer().BuildSummary(document).Software, Is.EqualTo("Photo Desk 4.2"));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(12, 1)]
    [TestCase(13, 2)]
    [TestCase(38, 3)]
    [TestCase(63, 4)]
    [TestCase(88, 5)]
    [TestCase(99, 5)]
    public void BuildSummary_MapsMicrosoftPercentRatingToFiveStars(int percent, int expected)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "MicrosoftPhoto", "Microsoft Photo");
        directory.AddTagWithValue("Xmp.MicrosoftPhoto.Rating", "Rating", MetadataValue.FromInteger(percent));

        Assert.That(new ImageMetadataNormalizer().BuildSummary(document).Rating, Is.EqualTo(expected));
    }

    [Test]
    public void BuildSummary_PrefersValidSimpleRatingAndFallsBackFromInvalidValue()
    {
        ImageMetadataDocument preferred = new ImageMetadataDocument();
        MetadataDirectory preferredDirectory = preferred.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        preferredDirectory.AddTagWithValue("Xmp.xmp.Rating", "Rating", MetadataValue.FromInteger(-1));
        preferredDirectory.AddTagWithValue("Xmp.MicrosoftPhoto.Rating", "Rating", MetadataValue.FromInteger(99));

        ImageMetadataDocument fallback = new ImageMetadataDocument();
        MetadataDirectory fallbackDirectory = fallback.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        fallbackDirectory.AddTagWithValue("Xmp.xmp.Rating", "Rating", MetadataValue.FromInteger(7));
        fallbackDirectory.AddTagWithValue("Xmp.MicrosoftPhoto.Rating", "Rating", MetadataValue.FromInteger(75));

        Assert.That(new ImageMetadataNormalizer().BuildSummary(preferred).Rating, Is.EqualTo(-1));
        Assert.That(new ImageMetadataNormalizer().BuildSummary(fallback).Rating, Is.EqualTo(4));
    }

    [Test]
    public void BuildSummary_PrefersOriginalCaptureMetadataAndSkipsBlankValues()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory exif = document.AddDirectoryWithName(MetadataFamily.Exif, "ExifIFD", "EXIF");
        exif.AddTagWithValue("Exif.Photo.DateTimeOriginal", "Date Time Original", MetadataValue.FromDateText("   "));
        exif.AddTagWithValue("Exif.IFD0.DateTime", "Date Time", MetadataValue.FromDateText("2026:08:12 18:00:00"));
        MetadataDirectory xmp = document.AddDirectoryWithName(MetadataFamily.Xmp, "exif", "XMP EXIF");
        xmp.AddTagWithValue("Xmp.exif.DateTimeOriginal", "Date Time Original", MetadataValue.FromDateText("2026-08-12T10:11:12-07:00"));
        MetadataDirectory photoshop = document.AddDirectoryWithName(MetadataFamily.Xmp, "photoshop", "Adobe Photoshop");
        photoshop.AddTagWithValue("Xmp.photoshop.DateCreated", "Date Created", MetadataValue.FromDateText("2026-08-12T10:12:00-07:00"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.CaptureTimeText, Is.EqualTo("2026-08-12T10:11:12-07:00"));
    }

    [Test]
    public void BuildSummary_CombinesIptcContentAndDigitalCreationDateTimes()
    {
        ImageMetadataDocument contentDocument = new ImageMetadataDocument();
        MetadataDirectory content = contentDocument.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC Application 2");
        content.AddTagWithValue("Iptc.Application2.DateCreated", "Date Created", MetadataValue.FromDateText("20260812"));
        content.AddTagWithValue("Iptc.Application2.TimeCreated", "Time Created", MetadataValue.FromString("141530-0700"));
        content.AddTagWithValue("Iptc.Application2.DigitalCreationDate", "Digital Creation Date", MetadataValue.FromDateText("20260813"));
        content.AddTagWithValue("Iptc.Application2.DigitalCreationTime", "Digital Creation Time", MetadataValue.FromString("010203+0000"));

        ImageMetadataDocument digitalDocument = new ImageMetadataDocument();
        MetadataDirectory digital = digitalDocument.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC Application 2");
        digital.AddTagWithValue("Iptc.Application2.DigitalCreationDate", "Digital Creation Date", MetadataValue.FromDateText("20260813"));
        digital.AddTagWithValue("Iptc.Application2.DigitalCreationTime", "Digital Creation Time", MetadataValue.FromString("010203+0000"));

        Assert.That(new ImageMetadataNormalizer().BuildSummary(contentDocument).CaptureTimeText, Is.EqualTo("20260812 141530-0700"));
        Assert.That(new ImageMetadataNormalizer().BuildSummary(digitalDocument).CaptureTimeText, Is.EqualTo("20260813 010203+0000"));
    }

    [TestCase("Exif.ExifIFD.DateTimeDigitized", "2026:08:12 10:11:12")]
    [TestCase("Xmp.xmp.CreateDate", "2026-08-12T10:11:12Z")]
    [TestCase("Exif.IFD0.DateTime", "2026:08:12 10:11:12")]
    [TestCase("Png.tIME.LastModificationTime", "2026-08-12T10:11:12Z")]
    public void BuildSummary_UsesEmbeddedDigitalAndModificationFallbacks(string key, string value)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Unknown, "Date", "Date");
        directory.AddTagWithValue(key, "Date", MetadataValue.FromDateText(value));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.CaptureTimeText, Is.EqualTo(value));
    }

    [Test]
    public void BuildSummary_ReturnsDefaultsForNullDocument()
    {
        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(null);

        Assert.That(summary.Width, Is.EqualTo(0));
        Assert.That(summary.FrameCount, Is.EqualTo(0));
        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Unknown));
        Assert.That(summary.Keywords, Is.Empty);
        Assert.That(summary.Gps.IsValid, Is.False);
    }
}
