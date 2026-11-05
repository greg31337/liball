using System.Collections.Generic;
using System.IO;
using System.Text;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class EmbeddedMetadataIntegrationTests
{
    [Test]
    public void ReadStream_CompositeJpegExtractsRequestedMetadataFamiliesTogether()
    {
        byte[] jpeg = CreateCompositeJpeg();
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.DecodeMakerNotes = true;
        options.CaptureRawBlockData = true;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(jpeg), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Jpeg));
        Assert.That(document.ByteLength, Is.EqualTo(jpeg.Length));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.Byline").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Xmp.xmp.Rating").Value.IntegerValue, Is.EqualTo(4));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Icc.Header.Signature").Value.Text, Is.EqualTo("acsp"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.ResourceId").Value.IntegerValue, Is.EqualTo(1028));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.Vendor").Value.Text, Is.EqualTo("Canon"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "MakerNote.MakerNote0.FirmwareVersion").Value.Text, Is.EqualTo("FW1"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jumbf.APP11.Identifier").Value.Text, Is.EqualTo("JUMBF"));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Iptc));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Icc));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.PhotoshopImageResource));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.MakerNote));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Jumbf));
        Assert.That(document.RawBlocks, Has.Some.Matches<MetadataRawBlock>(block => block.Family == MetadataFamily.MakerNote && block.Data.Length > 0));
        Assert.That(document.Summary.HasColorProfile, Is.True);
        Assert.That(document.Summary.Rating, Is.EqualTo(4));
    }

    [Test]
    public void ReadStreamWithOptions_DisablesXmpIccAndMakerNotePreservationIndependently()
    {
        byte[] jpeg = CreateCompositeJpeg();
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();
        options.ParseXmp = false;
        options.ParseIcc = false;
        options.PreserveMakerNotes = false;
        options.DecodeMakerNotes = true;

        ImageMetadataDocument document = new ImageMetadataReader().ReadStreamWithOptions(new MemoryStream(jpeg), options);

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Profiles, Has.None.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Xmp));
        Assert.That(document.Profiles, Has.None.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.Icc));
        Assert.That(document.Profiles, Has.None.Matches<MetadataProfile>(profile => profile.Kind == MetadataProfileKind.MakerNote));
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Xmp.xmp.Rating"), Is.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "Icc.Header.Signature"), Is.Null);
        Assert.That(new ImageMetadataQuery().FindFirst(document, "MakerNote.MakerNote0.Vendor"), Is.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Exif.ExifIFD.MakerNote"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Iptc.Application2.Byline").Value.Text, Is.EqualTo("Ada"));
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Photoshop.IRB.ResourceId"), Is.Not.Null);
        Assert.That(RequiredMetadataQuery.FindFirst(document, "Jumbf.APP11.Identifier"), Is.Not.Null);
    }

    [TestCaseSource(nameof(JumbfCases))]
    public void ReadStream_DiscoversJumbfAcrossSupportedContainers(byte[] payload, ImageFormatKind formatKind, MetadataProfileKind profileKind)
    {
        ImageMetadataDocument document = new ImageMetadataReader().ReadStream(new MemoryStream(payload));

        Assert.That(document.Diagnostics, Is.Empty);
        Assert.That(document.Format.Kind, Is.EqualTo(formatKind));
        Assert.That(document.Profiles, Has.Some.Matches<MetadataProfile>(profile => profile.Kind == profileKind && profile.Data.Length > 0));
    }

    public static object[] JumbfCases()
    {
        return new object[]
        {
            new object[] { CreateJpegJumbf(), ImageFormatKind.Jpeg, MetadataProfileKind.Jumbf },
            new object[] { CreateBmffJumbf(), ImageFormatKind.Avif, MetadataProfileKind.Jumbf },
            new object[] { CreateJpegXlJumbf(), ImageFormatKind.JpegXl, MetadataProfileKind.Jumbf }
        };
    }

    public static byte[] CreateCompositeJpeg()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);

        byte[] tiff = TiffMakerNoteExtractorTests.CreateMakerNoteTiffWithMake("Canon", TiffMakerNoteExtractorTests.CreateCanonMakerNotePayload());
        JpegMetadataExtractionTests.AddSegment(bytes, 225, JpegMetadataExtractionTests.Combine(Encoding.ASCII.GetBytes("Exif\0\0"), tiff));

        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:xmp='http://ns.adobe.com/xap/1.0/'><rdf:Description xmp:Rating='4'/></rdf:RDF></x:xmpmeta>";
        byte[] xmp = JpegMetadataExtractionTests.Combine(Encoding.ASCII.GetBytes(JpegXmpExtractor.StandardXmpIdentifier), Encoding.UTF8.GetBytes(xml));
        JpegMetadataExtractionTests.AddSegment(bytes, 225, xmp);

        byte[] icc = JpegIccExtractorTests.CreateIccPayload(1, 1, IccProfileParserTests.CreateProfile(false));
        JpegMetadataExtractionTests.AddSegment(bytes, 226, icc);

        byte[] iptc = JpegPhotoshopExtractorTests.BuildIptcData();
        byte[] resource = JpegPhotoshopExtractorTests.BuildResource(1028, "", iptc);
        byte[] photoshop = JpegPhotoshopExtractorTests.BuildPhotoshopPayload(new byte[][] { resource });
        JpegMetadataExtractionTests.AddSegment(bytes, 237, photoshop);

        byte[] jumbf = JpegMetadataExtractionTests.Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.JumbfIdentifier), new byte[] { 1, 2, 3, 4 });
        JpegMetadataExtractionTests.AddSegment(bytes, 235, jumbf);
        JpegMetadataExtractionTests.AddFrame(bytes, 192, 64, 32, 3);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static byte[] CreateJpegJumbf()
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(255);
        bytes.Add(216);
        byte[] jumbf = JpegMetadataExtractionTests.Combine(Encoding.ASCII.GetBytes(JpegRawProfileExtractor.JumbfIdentifier), new byte[] { 1, 2, 3 });
        JpegMetadataExtractionTests.AddSegment(bytes, 235, jumbf);
        bytes.Add(255);
        bytes.Add(217);
        return bytes.ToArray();
    }

    public static byte[] CreateBmffJumbf()
    {
        byte[] description = BmffMetadataExtractorTests.CreateBox("jumd", Encoding.ASCII.GetBytes("generic manifest"));
        byte[] jumbf = BmffMetadataExtractorTests.CreateBox("jumb", description);
        return BmffMetadataExtractorTests.Join(BmffMetadataExtractorTests.CreateFtyp(), jumbf);
    }

    public static byte[] CreateJpegXlJumbf()
    {
        byte[] description = JpegXlBoxTraverserTests.CreateBox("jumd", Encoding.ASCII.GetBytes("generic manifest"));
        return JpegXlBoxTraverserTests.Join(
            JpegXlBoxTraverserTests.CreateSignatureBox(),
            JpegXlBoxTraverserTests.CreateFileTypeBox(0),
            JpegXlBoxTraverserTests.CreateBox("jumb", description));
    }
}
