using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffWriterHighLevelMetadataTests
{
    const int DateTimeOriginal = 36867;
    const int GpsAltitude = 6;

    [TestCase(TiffFileKind.Classic)]
    [TestCase(TiffFileKind.BigTiff)]
    public void MetadataWriters_RoundTripAllFamiliesAndNestedIfds(TiffFileKind fileKind)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        ITiffWriter writer = new TiffFactory().CreateWriter(options);
        int metadataImageIndex = writer.AddImage(new TiffImageDefinition());
        int omeImageIndex = writer.AddImage(new TiffImageDefinition());
        int imageJImageIndex = writer.AddImage(new TiffImageDefinition());

        AssertSuccess(writer.WriteExif(metadataImageIndex, CreateExif()));
        AssertSuccess(writer.WriteGps(metadataImageIndex, CreateGps()));
        AssertSuccess(writer.WriteXmp(metadataImageIndex, CreateXmp("<x:xmpmeta>first</x:xmpmeta>")));
        AssertSuccess(writer.WriteXmp(metadataImageIndex, CreateXmp("<x:xmpmeta>second</x:xmpmeta>")));
        AssertSuccess(writer.WriteIccProfile(metadataImageIndex, CreateIcc()));
        AssertSuccess(writer.WritePhotoshop(metadataImageIndex, CreatePhotoshop()));
        AssertSuccess(writer.WriteIptc(metadataImageIndex, CreateIptc()));
        AssertSuccess(writer.WriteImageDescription(metadataImageIndex, "plain description"));
        AssertSuccess(writer.WriteGdalMetadata(metadataImageIndex, CreateGdal()));
        AssertSuccess(writer.WriteGeoTiffMetadata(metadataImageIndex, CreateGeoModel(), CreateGeoKeys(), CreateGeoParameters()));
        AssertSuccess(writer.WriteOmeMetadata(omeImageIndex, CreateOme()));
        AssertSuccess(writer.WriteImageJMetadata(imageJImageIndex, CreateImageJ()));

        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenMetadata(writer.GetFileBytes(), new TiffReadOptions());
        ITiffImage image = file.GetImage(metadataImageIndex);
        ITiffImage omeImage = file.GetImage(omeImageIndex);
        ITiffImage imageJImage = file.GetImage(imageJImageIndex);

        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(fileKind));
        Assert.That(image.ReadExif().Metadata.GetAscii(DateTimeOriginal), Is.EqualTo("2026:08:26 14:30:00"));
        Assert.That(image.ReadGps().Metadata.GetRationalScalar(GpsAltitude).Numerator, Is.EqualTo(1234));
        Assert.That(image.ReadXmp().Packet.Text, Is.EqualTo("<x:xmpmeta>second</x:xmpmeta>"));
        Assert.That(image.ReadIccProfile().Profile.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 4, 97, 99, 115, 112 }));
        Assert.That(image.ReadPhotoshop().Metadata.Blocks[0].Name, Is.EqualTo("dpi"));
        Assert.That(image.ReadPhotoshop().Metadata.Blocks[0].Data, Is.EqualTo(new byte[] { 9, 10 }));
        Assert.That(image.ReadIptc().Metadata.Blocks[0].Data, Is.EqualTo(new byte[] { 116, 101, 115, 116 }));
        Assert.That(image.ReadImageDescription().Metadata.Text, Is.EqualTo("plain description"));
        Assert.That(image.ReadGdalMetadata().Metadata.NoDataText, Is.EqualTo("-9999"));
        Assert.That(image.ReadGeoModel().Metadata.PixelScale, Is.EqualTo(new double[] { 30, 30, 0 }));
        Assert.That(image.ReadGeoKeyDirectory().Directory.Keys[0].KeyId, Is.EqualTo(1024));
        Assert.That(image.ReadGeoParameters().Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
        Assert.That(omeImage.ReadOmeMetadata().Metadata.HasOmeXml, Is.True);
        Assert.That(imageJImage.ReadImageJMetadata().Metadata.Channels, Is.EqualTo(2));
        if (fileKind == TiffFileKind.BigTiff)
        {
            Assert.That(image.GetTag(TiffTagId.ExifIfd)!.DataType, Is.EqualTo(TiffDataType.Ifd8));
            Assert.That(image.GetTag(TiffTagId.GpsIfd)!.DataType, Is.EqualTo(TiffDataType.Ifd8));
        }
        else
        {
            Assert.That(image.GetTag(TiffTagId.ExifIfd)!.DataType, Is.EqualTo(TiffDataType.Long));
            Assert.That(image.GetTag(TiffTagId.GpsIfd)!.DataType, Is.EqualTo(TiffDataType.Long));
        }
    }

    [Test]
    public void MetadataWriters_RejectInvalidModelsWithoutAddingTags()
    {
        ITiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(new TiffImageDefinition());
        TiffDiagnostics exif = writer.WriteExif(imageIndex, new TiffExifMetadata());
        TiffDiagnostics xmp = writer.WriteXmp(imageIndex, new TiffXmpPacket());
        TiffDiagnostics photoshop = writer.WritePhotoshop(imageIndex, new TiffPhotoshopMetadata());
        TiffOmeMetadata omeMetadata = new TiffOmeMetadata();
        omeMetadata.HasOmeXml = true;
        omeMetadata.Xml = "not XML";
        TiffDiagnostics ome = writer.WriteOmeMetadata(imageIndex, omeMetadata);

        Assert.That(exif.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(xmp.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(photoshop.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(ome.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static void AssertSuccess(TiffDiagnostics diagnostics)
    {
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
    }

    static TiffExifMetadata CreateExif()
    {
        TiffExifMetadata metadata = new TiffExifMetadata();
        metadata.HasExif = true;
        metadata.Values = new TiffTagValue[] { TiffTagValue.CreateAscii(DateTimeOriginal, "2026:08:26 14:30:00") };
        return metadata;
    }

    static TiffGpsMetadata CreateGps()
    {
        TiffGpsMetadata metadata = new TiffGpsMetadata();
        metadata.HasGps = true;
        metadata.Values = new TiffTagValue[] { TiffTagValue.CreateRationalScalar(GpsAltitude, TiffDataType.Rational, 1234, 10) };
        return metadata;
    }

    static TiffXmpPacket CreateXmp(string text)
    {
        TiffXmpPacket packet = new TiffXmpPacket();
        packet.HasXmp = true;
        packet.Text = text;
        return packet;
    }

    static TiffIccProfile CreateIcc()
    {
        TiffIccProfile profile = new TiffIccProfile();
        profile.HasProfile = true;
        profile.Bytes = new byte[] { 0, 0, 0, 4, 97, 99, 115, 112 };
        return profile;
    }

    static TiffPhotoshopMetadata CreatePhotoshop()
    {
        TiffPhotoshopImageResourceBlock block = new TiffPhotoshopImageResourceBlock();
        block.Signature = "8BIM";
        block.ResourceId = 0x03ed;
        block.Name = "dpi";
        block.Data = new byte[] { 9, 10 };
        TiffPhotoshopMetadata metadata = new TiffPhotoshopMetadata();
        metadata.HasPhotoshop = true;
        metadata.Blocks = new TiffPhotoshopImageResourceBlock[] { block };
        return metadata;
    }

    static TiffIptcMetadata CreateIptc()
    {
        TiffIptcBlock block = new TiffIptcBlock();
        block.RecordNumber = 2;
        block.DataSetNumber = 5;
        block.Data = new byte[] { 116, 101, 115, 116 };
        TiffIptcMetadata metadata = new TiffIptcMetadata();
        metadata.HasIptc = true;
        metadata.Blocks = new TiffIptcBlock[] { block };
        return metadata;
    }

    static TiffGdalMetadata CreateGdal()
    {
        TiffGdalMetadata metadata = new TiffGdalMetadata();
        metadata.HasMetadata = true;
        metadata.MetadataXml = "<GDALMetadata><Item name=\"AREA_OR_POINT\">Area</Item></GDALMetadata>";
        metadata.HasNoData = true;
        metadata.NoDataText = "-9999";
        return metadata;
    }

    static TiffGeoModelMetadata CreateGeoModel()
    {
        TiffGeoModelMetadata model = new TiffGeoModelMetadata();
        model.HasPixelScale = true;
        model.PixelScale = new double[] { 30, 30, 0 };
        model.HasTiepoints = true;
        TiffGeoTiepoint tiepoint = new TiffGeoTiepoint();
        tiepoint.ModelX = 100;
        tiepoint.ModelY = 200;
        model.Tiepoints = new TiffGeoTiepoint[] { tiepoint };
        return model;
    }

    static TiffGeoKeyDirectory CreateGeoKeys()
    {
        TiffGeoKeyEntry key = new TiffGeoKeyEntry();
        key.KeyId = 1024;
        key.Count = 1;
        key.ValueOffset = 1;
        TiffGeoKeyDirectory directory = new TiffGeoKeyDirectory();
        directory.HasDirectory = true;
        directory.KeyDirectoryVersion = 1;
        directory.KeyRevision = 1;
        directory.MinorRevision = 1;
        directory.NumberOfKeys = 1;
        directory.Keys = new TiffGeoKeyEntry[] { key };
        return directory;
    }

    static TiffGeoParameterMetadata CreateGeoParameters()
    {
        TiffGeoParameterMetadata parameters = new TiffGeoParameterMetadata();
        parameters.HasDoubleParams = true;
        parameters.DoubleParams = new double[] { 298.257223563 };
        parameters.HasAsciiParams = true;
        parameters.AsciiText = "WGS 84|";
        return parameters;
    }

    static TiffOmeMetadata CreateOme()
    {
        TiffOmeMetadata metadata = new TiffOmeMetadata();
        metadata.HasOmeXml = true;
        metadata.Xml = "<OME><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" SizeX=\"4\" SizeY=\"3\" /></Image></OME>";
        return metadata;
    }

    static TiffImageJMetadata CreateImageJ()
    {
        TiffImageJMetadata metadata = new TiffImageJMetadata();
        metadata.HasImageJ = true;
        metadata.Version = "1.54";
        metadata.ImageCount = 2;
        metadata.Channels = 2;
        return metadata;
    }
}
