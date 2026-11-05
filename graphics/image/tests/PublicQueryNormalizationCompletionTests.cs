using System.Collections.Generic;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicQueryNormalizationCompletionTests
{
    [Test]
    public void Query_PreservesDepthFirstSourceOrderAcrossNestedRepeatedAndFrameDirectories()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory first = factory.CreateDirectory(MetadataFamily.Exif, "IFD0", "First");
        first.Path = "TIFF/IFD0";
        MetadataTag firstTag = factory.CreateTag("Shared.Tag", "Shared", MetadataFamily.Exif, factory.CreateStringValue("first"));
        MetadataTag repeatedTag = factory.CreateTag("Shared.Tag", "Shared", MetadataFamily.Exif, factory.CreateStringValue("repeated"));
        first.Tags.Add(firstTag);
        first.Tags.Add(repeatedTag);
        MetadataDirectory child = factory.CreateDirectory(MetadataFamily.Gps, "GPS", "Child");
        child.Path = "TIFF/IFD0/GPS";
        MetadataTag childTag = factory.CreateTag("Gps.Tag", "Coordinate", MetadataFamily.Gps, factory.CreateDoubleValue(45.5));
        child.Tags.Add(childTag);
        first.Children.Add(child);

        MetadataDirectory second = factory.CreateDirectory(MetadataFamily.Exif, "IFD0", "Second");
        second.Path = "TIFF/IFD0";
        second.RepeatIndex = 1;
        second.FrameIndex = 2;
        MetadataTag secondTag = factory.CreateTag("Shared.Tag", "Shared", MetadataFamily.Exif, factory.CreateStringValue("second"));
        second.Tags.Add(secondTag);
        document.Directories.Add(first);
        document.Directories.Add(second);

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Index = 2;
        MetadataDirectory frameDirectory = factory.CreateDirectory(MetadataFamily.Exif, "IFD0", "Frame");
        frameDirectory.Path = "Frame/IFD0";
        frameDirectory.FrameIndex = 2;
        MetadataTag frameTag = factory.CreateTag("Shared.Tag", "Shared", MetadataFamily.Exif, factory.CreateStringValue("frame"));
        frameDirectory.Tags.Add(frameTag);
        frame.Directories.Add(frameDirectory);
        document.Frames.Add(frame);
        IImageMetadataQuery query = new ImageMetadataQuery();

        List<MetadataDirectory> directories = query.GetDirectories(document);
        List<MetadataTag> tags = query.GetTags(document);
        List<MetadataTag> shared = query.Find(document, "Shared.Tag");
        List<MetadataDirectory> repeatedPaths = query.FindDirectoriesByPath(document, "TIFF/IFD0");
        List<MetadataTag> frameTags = query.GetTagsForFrame(document, 2);

        Assert.That(directories, Is.EqualTo(new MetadataDirectory[] { first, child, second, frameDirectory }));
        Assert.That(tags, Is.EqualTo(new MetadataTag[] { firstTag, repeatedTag, childTag, secondTag, frameTag }));
        Assert.That(shared, Is.EqualTo(new MetadataTag[] { firstTag, repeatedTag, secondTag, frameTag }));
        Assert.That(query.FindFirst(document, "Shared.Tag"), Is.SameAs(firstTag));
        Assert.That(repeatedPaths, Is.EqualTo(new MetadataDirectory[] { first, second }));
        Assert.That(query.FindDirectoriesByFamily(document, MetadataFamily.Exif), Is.EqualTo(new MetadataDirectory[] { first, second, frameDirectory }));
        Assert.That(query.GetDirectoriesForFrame(document, 2), Is.EqualTo(new MetadataDirectory[] { second, frameDirectory }));
        Assert.That(frameTags, Is.EqualTo(new MetadataTag[] { secondTag, frameTag }));
        Assert.That(query.FindTagsForFrame(document, 2, "Shared.Tag"), Is.EqualTo(new MetadataTag[] { secondTag, frameTag }));
    }

    [Test]
    public void Normalizer_PopulatesEverySummaryFieldThroughThePublicContract()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame first = new ImageMetadataFrame();
        first.Width = 640;
        first.Height = 480;
        first.PixelAspectRatio = 1.25;
        first.HorizontalResolution = 300;
        first.VerticalResolution = 240;
        first.ResolutionUnit = "inches";
        first.Orientation = ImageOrientation.Rotate90;
        first.ColorSpace = ImageColorSpaceKind.DisplayP3;
        first.SourceColorType = "Truecolor with alpha";
        first.IsInterlaced = true;
        first.IsProgressive = true;
        first.IsAnimated = true;
        first.DurationMilliseconds = 125;
        ImageMetadataFrame second = new ImageMetadataFrame();
        second.Width = 320;
        second.Height = 200;
        second.DurationMilliseconds = 75;
        document.Frames.Add(first);
        document.Frames.Add(second);

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Name = "Display P3";
        document.Profiles.Add(profile);
        MetadataDirectory directory = factory.CreateDirectory(MetadataFamily.Exif, "All", "All normalized fields");
        AddTag(directory, factory, "Exif.IFD0.Make", factory.CreateStringValue("Canon"));
        AddTag(directory, factory, "Exif.IFD0.Model", factory.CreateStringValue("R5"));
        AddTag(directory, factory, "Exif.ExifIFD.LensModel", factory.CreateStringValue("RF 50mm"));
        AddTag(directory, factory, "Exif.ExifIFD.DateTimeOriginal", factory.CreateDateTextValue("2026:08:25 12:34:56"));
        AddTag(directory, factory, "Xmp.dc.creator", factory.CreateStringValue("Ada"));
        AddTag(directory, factory, "Xmp.dc.rights", factory.CreateStringValue("Copyright 2026"));
        AddTag(directory, factory, "Xmp.dc.title", factory.CreateStringValue("Title"));
        AddTag(directory, factory, "Xmp.dc.description", factory.CreateStringValue("Description"));
        AddTag(directory, factory, "Xmp.dc.subject", factory.CreateStringListValue(new string[] { "ocean", "sunset" }));
        AddTag(directory, factory, "Xmp.xmp.Rating", factory.CreateIntegerValue(5));
        AddTag(directory, factory, "Xmp.xmp.CreatorTool", factory.CreateStringValue("Metadata Tool"));
        AddTag(directory, factory, "Exif.GPS.Latitude", factory.CreateDoubleValue(45.5));
        AddTag(directory, factory, "Exif.GPS.LatitudeRef", factory.CreateStringValue("N"));
        AddTag(directory, factory, "Exif.GPS.Longitude", factory.CreateDoubleValue(122.25));
        AddTag(directory, factory, "Exif.GPS.LongitudeRef", factory.CreateStringValue("W"));
        AddTag(directory, factory, "Exif.GPS.Altitude", factory.CreateDoubleValue(62.5));
        AddTag(directory, factory, "Exif.GPS.AltitudeRef", factory.CreateStringValue("Above Sea Level"));
        AddTag(directory, factory, "Exif.IFD0.XResolution", factory.CreateRationalValue(300, 1));
        AddTag(directory, factory, "Exif.IFD0.YResolution", factory.CreateRationalValue(240, 1));
        AddTag(directory, factory, "Exif.IFD0.ResolutionUnit", factory.CreateStringValue("inches"));
        AddTag(directory, factory, "Gif.Animation.LoopCount", factory.CreateIntegerValue(3));
        document.Directories.Add(directory);

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Width, Is.EqualTo(640));
        Assert.That(summary.Height, Is.EqualTo(480));
        Assert.That(summary.FrameCount, Is.EqualTo(2));
        Assert.That(summary.PixelAspectRatio, Is.EqualTo(1.25));
        Assert.That(summary.HorizontalResolution, Is.EqualTo(300));
        Assert.That(summary.VerticalResolution, Is.EqualTo(240));
        Assert.That(summary.ResolutionUnit, Is.EqualTo("inches"));
        Assert.That(summary.ResolutionText, Is.EqualTo("300/1 x 240/1 inches"));
        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Rotate90));
        Assert.That(summary.CameraMake, Is.EqualTo("Canon"));
        Assert.That(summary.CameraModel, Is.EqualTo("R5"));
        Assert.That(summary.LensModel, Is.EqualTo("RF 50mm"));
        Assert.That(summary.CaptureTimeText, Is.EqualTo("2026:08:25 12:34:56"));
        Assert.That(summary.CaptureTime.IsValid, Is.True);
        Assert.That(summary.CaptureTime.Precision, Is.EqualTo(MetadataDatePrecision.Second));
        Assert.That(summary.Gps.IsValid, Is.True);
        Assert.That(summary.Gps.Latitude, Is.EqualTo(45.5));
        Assert.That(summary.Gps.Longitude, Is.EqualTo(-122.25));
        Assert.That(summary.Gps.Altitude, Is.EqualTo(62.5));
        Assert.That(summary.Author, Is.EqualTo("Ada"));
        Assert.That(summary.Copyright, Is.EqualTo("Copyright 2026"));
        Assert.That(summary.Title, Is.EqualTo("Title"));
        Assert.That(summary.Description, Is.EqualTo("Description"));
        Assert.That(summary.Keywords, Is.EqualTo(new string[] { "ocean", "sunset" }));
        Assert.That(summary.Rating, Is.EqualTo(5));
        Assert.That(summary.HasColorProfile, Is.True);
        Assert.That(summary.ColorProfileName, Is.EqualTo("Display P3"));
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.DisplayP3));
        Assert.That(summary.SourceColorType, Is.EqualTo("Truecolor with alpha"));
        Assert.That(summary.IsInterlaced, Is.True);
        Assert.That(summary.IsProgressive, Is.True);
        Assert.That(summary.IsAnimated, Is.True);
        Assert.That(summary.AnimationLoopCount, Is.EqualTo(3));
        Assert.That(summary.TotalDurationMilliseconds, Is.EqualTo(200));
        Assert.That(summary.Software, Is.EqualTo("Metadata Tool"));
        Assert.That(document.Summary, Is.SameAs(summary));
    }

    [Test]
    public void Normalizer_HigherPrioritySourcesWinAcrossConflictingPublicMetadata()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataDocument document = new ImageMetadataDocument();
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = 100;
        frame.Height = 80;
        frame.Orientation = ImageOrientation.Rotate180;
        frame.ColorSpace = ImageColorSpaceKind.AdobeRgb;
        document.Frames.Add(frame);
        MetadataDirectory directory = factory.CreateDirectory(MetadataFamily.Exif, "Priority", "Priority");
        AddTag(directory, factory, "Raw.RAF.Header.CameraMake", factory.CreateStringValue("Low make"));
        AddTag(directory, factory, "Exif.IFD0.Make", factory.CreateStringValue("High make"));
        AddTag(directory, factory, "Iptc.Application2.Byline", factory.CreateStringValue("Low author"));
        AddTag(directory, factory, "Xmp.dc.creator", factory.CreateStringValue("High author"));
        AddTag(directory, factory, "Iptc.Application2.ObjectName", factory.CreateStringValue("Low title"));
        AddTag(directory, factory, "Xmp.dc.title", factory.CreateStringValue("High title"));
        AddTag(directory, factory, "Xmp.photoshop.DateCreated", factory.CreateDateTextValue("2026-08-24T01:02:03Z"));
        AddTag(directory, factory, "Exif.ExifIFD.DateTimeOriginal", factory.CreateDateTextValue("2026:08:25 12:34:56"));
        AddTag(directory, factory, "Xmp.MicrosoftPhoto.Rating", factory.CreateIntegerValue(99));
        AddTag(directory, factory, "Xmp.xmp.Rating", factory.CreateIntegerValue(4));
        AddTag(directory, factory, "Exif.IFD0.Orientation", factory.CreateIntegerValue(6));
        document.Directories.Add(directory);

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.Width, Is.EqualTo(100));
        Assert.That(summary.Height, Is.EqualTo(80));
        Assert.That(summary.Orientation, Is.EqualTo(ImageOrientation.Rotate180));
        Assert.That(summary.ColorSpace, Is.EqualTo(ImageColorSpaceKind.AdobeRgb));
        Assert.That(summary.CameraMake, Is.EqualTo("High make"));
        Assert.That(summary.Author, Is.EqualTo("High author"));
        Assert.That(summary.Title, Is.EqualTo("High title"));
        Assert.That(summary.CaptureTimeText, Is.EqualTo("2026:08:25 12:34:56"));
        Assert.That(summary.Rating, Is.EqualTo(4));
    }

    public static void AddTag(MetadataDirectory directory, IImageMetadataFactory factory, string key, MetadataValue value)
    {
        directory.Tags.Add(factory.CreateTag(key, key, directory.Family, value));
    }
}
