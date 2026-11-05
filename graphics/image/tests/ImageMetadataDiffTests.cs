using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataDiffTests
{
    [Test]
    public void Compare_EquivalentDocumentsIgnoreOffsetsAndUniqueItemOrder()
    {
        ImageMetadataDocument left = CreateEquivalentDocument(false, 10);
        ImageMetadataDocument right = CreateEquivalentDocument(true, 900);

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.AreEqual, Is.True);
        Assert.That(result.Differences, Is.Empty);
    }

    [Test]
    public void Compare_ReportsDirectoryTagProfileAndSummaryDifferences()
    {
        ImageMetadataDocument left = new ImageMetadataDocument();
        MetadataDirectory leftExif = left.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Left EXIF");
        leftExif.Path = "TIFF/IFD0";
        MetadataTag leftMake = leftExif.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        leftMake.RawValue = new byte[] { 1 };
        leftExif.AddTagWithValue("Exif.IFD0.Serial", "Serial", MetadataValue.FromString("A1"));
        left.AddDirectoryWithName(MetadataFamily.Gps, "GPS", "GPS");
        MetadataProfile leftIcc = left.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.APP2", "Display", new byte[] { 1, 2 });
        leftIcc.MimeType = "application/vnd.iccprofile";
        leftIcc.Role = ImageMetadataRole.Thumbnail;
        leftIcc.ContainerItemId = 1;
        leftIcc.SourceFrameIndex = 0;
        left.AddProfileWithData(MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.APP1", "EXIF", new byte[] { 4 });
        left.Summary.Title = "Before";
        left.Summary.Keywords.Add("one");
        left.Summary.Gps.IsValid = true;
        left.Summary.Gps.Latitude = 45;

        ImageMetadataDocument right = new ImageMetadataDocument();
        MetadataDirectory rightExif = right.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "Right EXIF");
        rightExif.Path = "TIFF/IFD0";
        MetadataTag rightMake = rightExif.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Nikon"));
        rightMake.RawValue = new byte[] { 2 };
        rightExif.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("Z8"));
        right.AddDirectoryWithName(MetadataFamily.Xmp, "dc", "Dublin Core");
        MetadataProfile rightIcc = right.AddProfileWithData(MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.APP2", "Display", new byte[] { 1, 3 });
        rightIcc.MimeType = "application/vnd.iccprofile";
        rightIcc.Role = ImageMetadataRole.Preview;
        rightIcc.ContainerItemId = 2;
        rightIcc.SourceFrameIndex = 1;
        right.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 5 });
        right.Summary.Title = "After";
        right.Summary.Keywords.Add("two");
        right.Summary.Gps.IsValid = true;
        right.Summary.Gps.Latitude = 46;

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.AreEqual, Is.False);
        Assert.That(result.Differences, Has.Some.EqualTo("Directory Document/Exif.IFD0[0].Name: \"Left EXIF\" -> \"Right EXIF\""));
        Assert.That(result.Differences, Has.Some.EqualTo("Directory removed: Document/Gps.GPS[0]"));
        Assert.That(result.Differences, Has.Some.EqualTo("Directory added: Document/Xmp.dc[0]"));
        Assert.That(result.Differences, Has.Some.Contains("Exif.IFD0.Make[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Exif.IFD0.Make[0].RawValue"));
        Assert.That(result.Differences, Has.Some.Contains("Tag removed:").And.Contains("Exif.IFD0.Serial[0]"));
        Assert.That(result.Differences, Has.Some.Contains("Tag added:").And.Contains("Exif.IFD0.Model[0]"));
        Assert.That(result.Differences, Has.Some.Contains("Profile Icc.Icc.APP2[0].Data:").And.Contains("2 bytes, sha256"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Icc.Icc.APP2[0].Role:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Icc.Icc.APP2[0].ContainerItemId:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Profile Icc.Icc.APP2[0].SourceFrameIndex:"));
        Assert.That(result.Differences, Has.Some.EqualTo("Profile removed: Exif.Exif.APP1[0]"));
        Assert.That(result.Differences, Has.Some.EqualTo("Profile added: Xmp.Xmp.APP1[0]"));
        Assert.That(result.Differences, Has.Some.EqualTo("Summary.Title: \"Before\" -> \"After\""));
        Assert.That(result.Differences, Has.Some.EqualTo("Summary.Keywords: \"[one]\" -> \"[two]\""));
        Assert.That(result.Differences, Has.Some.EqualTo("Summary.Gps.Latitude: 45 -> 46"));
    }

    [Test]
    public void Compare_UsesOccurrencesForRepeatedTagsAndProfiles()
    {
        ImageMetadataDocument left = new ImageMetadataDocument();
        MetadataDirectory leftIptc = left.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC");
        leftIptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("one"));
        leftIptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("two"));
        left.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 1 });
        left.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 2 });

        ImageMetadataDocument right = new ImageMetadataDocument();
        MetadataDirectory rightIptc = right.AddDirectoryWithName(MetadataFamily.Iptc, "Application2", "IPTC");
        rightIptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("one"));
        rightIptc.AddTagWithValue("Iptc.Application2.Keywords", "Keywords", MetadataValue.FromString("three"));
        right.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 1 });
        right.AddProfileWithData(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.APP1", "XMP", new byte[] { 3 });

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.Differences, Has.Some.Contains("Iptc.Application2.Keywords[1].Value"));
        Assert.That(result.Differences, Has.None.Contains("Iptc.Application2.Keywords[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Profile Xmp.Xmp.APP1[1].Data"));
        Assert.That(result.Differences, Has.None.Contains("Profile Xmp.Xmp.APP1[0].Data"));
    }

    [Test]
    public void Compare_TreatsNullAsAnEmptyDocument()
    {
        ImageMetadataDiffResult empty = new ImageMetadataDiff().Compare(null, new ImageMetadataDocument());
        ImageMetadataDocument populated = new ImageMetadataDocument();
        populated.Summary.Author = "Ada";

        ImageMetadataDiffResult changed = new ImageMetadataDiff().Compare(null, populated);

        Assert.That(empty.AreEqual, Is.True);
        Assert.That(empty.Differences, Is.Empty);
        Assert.That(changed.AreEqual, Is.False);
        Assert.That(changed.Differences, Is.EqualTo(new[] { "Summary.Author: \"\" -> \"Ada\"" }));
    }

    [Test]
    public void Compare_ReportsCommonImageSummaryDifferences()
    {
        ImageMetadataDocument left = new ImageMetadataDocument();
        ImageMetadataDocument right = new ImageMetadataDocument();
        right.Summary.PixelAspectRatio = 1.25;
        right.Summary.HorizontalResolution = 300;
        right.Summary.VerticalResolution = 240;
        right.Summary.ResolutionUnit = "pixels/inch";
        right.Summary.SourceColorType = "Truecolor";
        right.Summary.IsInterlaced = true;
        right.Summary.IsProgressive = true;
        right.Summary.IsAnimated = true;
        right.Summary.AnimationLoopCount = 2;
        right.Summary.TotalDurationMilliseconds = 500;

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.Differences, Has.Some.StartsWith("Summary.PixelAspectRatio:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.HorizontalResolution:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.VerticalResolution:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.ResolutionUnit:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.SourceColorType:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.IsInterlaced:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.IsProgressive:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.IsAnimated:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.AnimationLoopCount:"));
        Assert.That(result.Differences, Has.Some.StartsWith("Summary.TotalDurationMilliseconds:"));
    }

    [Test]
    public void Compare_DetectsChangesAcrossTypedTagValues()
    {
        ImageMetadataDocument left = CreateTypedValueDocument(false);
        ImageMetadataDocument right = CreateTypedValueDocument(true);

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.AreEqual, Is.False);
        Assert.That(result.Differences, Has.Count.EqualTo(16));
        Assert.That(result.Differences, Has.All.Contains(".Value:"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.Integer[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.StringList[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.Bytes[0].Value").And.Contains("sha256"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.Boolean[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.IntegerArray[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.RationalArray[0].Value"));
        Assert.That(result.Differences, Has.Some.Contains("Typed.DoubleArray[0].Value"));
    }

    [Test]
    public void Compare_TraversesNestedAndFrameScopedDirectories()
    {
        ImageMetadataDocument left = new ImageMetadataDocument();
        MetadataDirectory leftRoot = left.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "EXIF");
        MetadataDirectory leftGps = leftRoot.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS");
        leftGps.AddTagWithValue("Gps.GPS.Latitude", "Latitude", MetadataValue.FromDouble(45));
        ImageMetadataFrame leftFrame = left.AddFrameWithDimensions(10, 10);
        leftFrame.AddDirectoryWithName(MetadataFamily.Png, "fcTL", "Frame Control");

        ImageMetadataDocument right = new ImageMetadataDocument();
        MetadataDirectory rightRoot = right.AddDirectoryWithName(MetadataFamily.Exif, "IFD0", "EXIF");
        MetadataDirectory rightGps = rightRoot.AddChildWithName(MetadataFamily.Gps, "GPS", "GPS");
        rightGps.AddTagWithValue("Gps.GPS.Latitude", "Latitude", MetadataValue.FromDouble(46));

        ImageMetadataDiffResult result = new ImageMetadataDiff().Compare(left, right);

        Assert.That(result.Differences, Has.Some.Contains("Document/Exif.IFD0[0]/Gps.GPS[0]/Gps.GPS.Latitude[0].Value"));
        Assert.That(result.Differences, Has.Some.EqualTo("Directory removed: Frame[0]/Png.fcTL[0]"));
    }

    public static ImageMetadataDocument CreateEquivalentDocument(bool reverseOrder, long offset)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory exif = MetadataDirectory.Create(MetadataFamily.Exif, "IFD0", "EXIF");
        exif.Path = "TIFF/IFD0";
        exif.Offset = offset;
        MetadataTag make;
        MetadataTag model;
        if (reverseOrder)
        {
            model = exif.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("R5"));
            make = exif.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
        }
        else
        {
            make = exif.AddTagWithValue("Exif.IFD0.Make", "Make", MetadataValue.FromString("Canon"));
            model = exif.AddTagWithValue("Exif.IFD0.Model", "Model", MetadataValue.FromString("R5"));
        }

        make.Offset = offset + 1;
        model.Offset = offset + 2;

        MetadataDirectory xmp = MetadataDirectory.Create(MetadataFamily.Xmp, "dc", "Dublin Core");
        xmp.Path = "XMP/dc";
        xmp.Offset = offset + 10;
        xmp.AddTagWithValue("Xmp.dc.title", "Title", MetadataValue.FromString("Sample"));
        if (reverseOrder)
        {
            document.AddDirectory(xmp);
            document.AddDirectory(exif);
        }
        else
        {
            document.AddDirectory(exif);
            document.AddDirectory(xmp);
        }

        MetadataProfile icc = new MetadataProfile();
        icc.Kind = MetadataProfileKind.Icc;
        icc.Family = MetadataFamily.Icc;
        icc.Key = "Icc.APP2";
        icc.Name = "ICC";
        icc.MimeType = "application/vnd.iccprofile";
        icc.Data = new byte[] { 1, 2, 3 };
        icc.Length = 3;
        icc.Offset = offset;
        MetadataProfile xmpProfile = new MetadataProfile();
        xmpProfile.Kind = MetadataProfileKind.Xmp;
        xmpProfile.Family = MetadataFamily.Xmp;
        xmpProfile.Key = "Xmp.APP1";
        xmpProfile.Name = "XMP";
        xmpProfile.MimeType = "application/rdf+xml";
        xmpProfile.Text = "<rdf/>";
        xmpProfile.Length = 6;
        xmpProfile.Offset = offset + 20;
        if (reverseOrder)
        {
            document.AddProfile(xmpProfile);
            document.AddProfile(icc);
        }
        else
        {
            document.AddProfile(icc);
            document.AddProfile(xmpProfile);
        }

        document.Summary.Width = 640;
        document.Summary.Height = 480;
        document.Summary.Title = "Sample";
        document.Summary.Keywords.Add("one");
        document.Summary.Gps.IsValid = true;
        document.Summary.Gps.Latitude = 45.5;
        return document;
    }

    public static ImageMetadataDocument CreateTypedValueDocument(bool alternate)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "Typed", "Typed Values");
        directory.AddTagWithValue("Typed.Integer", "Integer", MetadataValue.FromInteger(alternate ? -2 : -1));
        directory.AddTagWithValue("Typed.Unsigned", "Unsigned", MetadataValue.FromUnsignedInteger(alternate ? 2u : 1u));
        directory.AddTagWithValue("Typed.Rational", "Rational", MetadataValue.FromRational(alternate ? 2 : 1, 3));
        directory.AddTagWithValue("Typed.SignedRational", "Signed Rational", MetadataValue.FromSignedRational(alternate ? -2 : -1, 3));
        directory.AddTagWithValue("Typed.Double", "Double", MetadataValue.FromDouble(alternate ? 2.5 : 1.5));
        directory.AddTagWithValue("Typed.String", "String", MetadataValue.FromString(alternate ? "right" : "left"));
        MetadataValue list = new MetadataValue();
        list.Kind = MetadataValueKind.StringList;
        list.TextValues = new string[] { alternate ? "right" : "left" };
        directory.AddTagWithValue("Typed.StringList", "String List", list);
        directory.AddTagWithValue("Typed.Bytes", "Bytes", MetadataValue.FromBytes(new byte[] { alternate ? (byte)2 : (byte)1 }));
        directory.AddTagWithValue("Typed.Date", "Date", MetadataValue.FromDateText(alternate ? "2026-02-02" : "2026-01-01"));
        directory.AddTagWithValue("Typed.Xml", "XML", MetadataValue.FromXmlText(alternate ? "<right/>" : "<left/>"));
        directory.AddTagWithValue("Typed.Boolean", "Boolean", MetadataValue.FromBoolean(alternate));
        directory.AddTagWithValue("Typed.IntegerArray", "Integer Array", MetadataValue.FromIntegerArray(new long[] { alternate ? -2 : -1, 3 }));
        directory.AddTagWithValue("Typed.UnsignedIntegerArray", "Unsigned Integer Array", MetadataValue.FromUnsignedIntegerArray(new ulong[] { alternate ? 2u : 1u, 3 }));
        directory.AddTagWithValue("Typed.RationalArray", "Rational Array", MetadataValue.FromRationalArray(new long[] { alternate ? 2 : 1 }, new long[] { 3 }));
        directory.AddTagWithValue("Typed.SignedRationalArray", "Signed Rational Array", MetadataValue.FromSignedRationalArray(new long[] { alternate ? -2 : -1 }, new long[] { 3 }));
        directory.AddTagWithValue("Typed.DoubleArray", "Double Array", MetadataValue.FromDoubleArray(new double[] { alternate ? 2.5 : 1.5 }));
        return document;
    }
}
