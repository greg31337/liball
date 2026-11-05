using System.Text;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicMetadataApiTests
{
    const int DateTimeOriginal = 36867;
    const int GpsAltitude = 6;
    const int UnknownMetadataTag = 65020;
    const string PlainDescription = "plain metadata description";
    const string JsonDescription = "{\"camera\":\"public-api\",\"frames\":3}";
    const string OmeXml = "<OME><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" SizeX=\"1\" SizeY=\"1\" /></Image></OME>";

    static readonly byte[] XmpBytes = Encoding.UTF8.GetBytes("<x:xmpmeta>raw packet</x:xmpmeta>");
    static readonly byte[] IccBytes = new byte[] { 0, 0, 0, 8, 97, 99, 115, 112 };
    static readonly byte[] PhotoshopBytes = new byte[] { 56, 66, 73, 77, 3, 237, 0, 0, 0, 0, 0, 2, 9, 10 };
    static readonly byte[] IptcBytes = new byte[] { 28, 2, 5, 0, 4, 116, 101, 115, 116 };
    static readonly byte[] UnknownBytes = new byte[] { 0, 255, 16, 128, 7 };

    [Test]
    public void MetadataFamilies_WriteUpdateRemoveAndReadThroughPublicApi()
    {
        byte[] bytes = CreateMetadataFile();
        TiffFile file = OpenMetadata(bytes);
        TiffImage main = file.GetImage(0);
        TiffImage json = file.GetImage(1);
        TiffImage ome = file.GetImage(2);
        TiffImage imageJ = file.GetImage(3);

        Assert.That(file.Success, Is.True, file.GetDiagnostics().ToString());
        Assert.That(file.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        AssertMainMetadata(main);
        Assert.That(main.GetAsciiTagScalar(TiffTagId.Artist), Is.EqualTo("updated artist"));
        Assert.That(main.GetTag(TiffTagId.Software), Is.Null);
        Assert.That(main.GetTagValue(UnknownMetadataTag)!.RawBytes, Is.EqualTo(UnknownBytes));

        TiffImageDescriptionMetadata plain = main.ReadImageDescription().Metadata;
        TiffImageDescriptionMetadata structured = json.ReadImageDescription().Metadata;
        TiffOmeMetadata omeMetadata = ome.ReadOmeMetadata().Metadata;
        TiffImageJMetadata imageJMetadata = imageJ.ReadImageJMetadata().Metadata;

        Assert.That(plain.Text, Is.EqualTo(PlainDescription));
        Assert.That(plain.RawBytes, Is.EqualTo(AsciiBytes(PlainDescription)));
        Assert.That(structured.Text, Is.EqualTo(JsonDescription));
        Assert.That(structured.HasJson, Is.True);
        Assert.That(structured.RawBytes, Is.EqualTo(AsciiBytes(JsonDescription)));
        Assert.That(omeMetadata.HasOmeXml, Is.True);
        Assert.That(omeMetadata.Xml, Is.EqualTo(OmeXml));
        Assert.That(omeMetadata.ImageDescriptionRawBytes, Is.EqualTo(AsciiBytes(OmeXml)));
        Assert.That(imageJMetadata.HasImageJ, Is.True);
        Assert.That(imageJMetadata.Channels, Is.EqualTo(2));
        Assert.That(imageJMetadata.ImageCount, Is.EqualTo(2));
        Assert.That(imageJMetadata.RawBytes, Is.EqualTo(AsciiBytes(imageJMetadata.RawDescription)));
    }

    [Test]
    public void CopyMetadata_PreservesFlatFamiliesUnknownValuesAndDescriptionBytes()
    {
        TiffFile source = OpenRead(CreateMetadataFile());
        Assert.That(source.GetImage(0).GetRawTagBytes(TiffTagId.GeoAsciiParams), Is.EqualTo(AsciiBytes("WGS 84|")));
        TiffMetadataCopyResult preservingCopy = TiffApi.Copy.CopyMetadataPreservingUnknown(source.GetImage(0));
        TiffTagValue unknown = FindValue(preservingCopy.Values, UnknownMetadataTag)!;
        TiffWriteOptions options = CreateWriteOptions();
        TiffWriter destination = TiffApi.Factory.CreateWriter(options);
        int imageIndex = 0;
        while (imageIndex < source.GetImageCount())
        {
            int destinationIndex = destination.AddConfiguredImage(CreateImageConfiguration(imageIndex == 0));
            Assert.That(destinationIndex, Is.EqualTo(imageIndex));
            Assert.That(destination.WriteSourceRaster(destinationIndex, new byte[] { (byte)(200 + imageIndex) }).HasErrors(), Is.False);
            TiffDiagnostics copy = TiffApi.Copy.CopyMetadataToWriter(source.GetImage(imageIndex), destination, destinationIndex);
            Assert.That(copy.HasErrors(), Is.False, DiagnosticText(copy));
            imageIndex = imageIndex + 1;
        }

        TiffDiagnostics flush = destination.Flush();
        TiffFile copied = OpenMetadata(destination.GetFileBytes());
        TiffImage copiedMain = copied.GetImage(0);

        Assert.That(preservingCopy.Success, Is.True, preservingCopy.Diagnostics.ToString());
        Assert.That(unknown.RawBytes, Is.EqualTo(UnknownBytes));
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(copiedMain.ReadXmp().Packet.Bytes, Is.EqualTo(source.GetImage(0).ReadXmp().Packet.Bytes));
        Assert.That(copiedMain.ReadIccProfile().Profile.Bytes, Is.EqualTo(source.GetImage(0).ReadIccProfile().Profile.Bytes));
        Assert.That(copiedMain.ReadPhotoshop().Metadata.RawBytes, Is.EqualTo(source.GetImage(0).ReadPhotoshop().Metadata.RawBytes));
        Assert.That(copiedMain.ReadIptc().Metadata.RawBytes, Is.EqualTo(source.GetImage(0).ReadIptc().Metadata.RawBytes));
        Assert.That(copiedMain.ReadGdalMetadata().Metadata.MetadataRawBytes, Is.EqualTo(source.GetImage(0).ReadGdalMetadata().Metadata.MetadataRawBytes));
        Assert.That(copiedMain.GetTagValue(UnknownMetadataTag)!.RawBytes, Is.EqualTo(UnknownBytes));
        imageIndex = 0;
        while (imageIndex < source.GetImageCount())
        {
            Assert.That(copied.GetImage(imageIndex).GetRawTagBytes(TiffTagId.ImageDescription), Is.EqualTo(source.GetImage(imageIndex).GetRawTagBytes(TiffTagId.ImageDescription)));
            imageIndex = imageIndex + 1;
        }

        Assert.That(copied.GetImage(1).ReadImageDescription().Metadata.HasJson, Is.True);
        Assert.That(copied.GetImage(2).ReadOmeMetadata().Metadata.HasOmeXml, Is.True);
        Assert.That(copied.GetImage(3).ReadImageJMetadata().Metadata.HasImageJ, Is.True);
    }

    [Test]
    public void RewriteImageMetadata_PreservesNestedAndRawMetadataPlusEncodedRasterBytes()
    {
        TiffFile sourceFile = OpenRead(CreateMetadataFile());
        TiffImage source = sourceFile.GetImage(0);
        TiffRawStripReadResult sourceStrip = source.ReadRawStrip(0);
        TiffWriter destination = TiffApi.Factory.CreateWriter(CreateWriteOptions());
        TiffMetadataRewriteOptions options = new TiffMetadataRewriteOptions();
        options.Updates = new TiffTagValue[] { TiffApi.Data.CreateAsciiTag(TiffTagId.Copyright, "rewritten copyright") };
        options.RemovedTagIds = new int[] { TiffTagId.Artist };

        TiffMetadataRewriteResult rewrite = TiffApi.Copy.RewriteImageMetadata(source, destination, options);
        TiffDiagnostics flush = destination.Flush();
        TiffImage rewritten = OpenRead(destination.GetFileBytes()).GetImage(0);

        Assert.That(rewrite.Success, Is.True, DiagnosticText(rewrite.Diagnostics));
        Assert.That(rewrite.MetadataCopied, Is.True);
        Assert.That(rewrite.UnknownMetadataPreserved, Is.True);
        Assert.That(rewrite.RawImageDataPassedThrough, Is.True);
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(rewritten.ReadRawStrip(0).Bytes, Is.EqualTo(sourceStrip.Bytes));
        Assert.That(rewritten.ReadExif().Metadata.HasExif, Is.True);
        Assert.That(FindValue(rewritten.ReadExif().Metadata.Values, DateTimeOriginal)!.Text, Is.EqualTo("2026:08:27 09:10:11"));
        Assert.That(rewritten.ReadGps().Metadata.HasGps, Is.True);
        Assert.That(FindValue(rewritten.ReadGps().Metadata.Values, GpsAltitude)!.RationalValues[0].Numerator, Is.EqualTo(1234));
        Assert.That(rewritten.ReadXmp().Packet.Bytes, Is.EqualTo(source.ReadXmp().Packet.Bytes));
        Assert.That(rewritten.ReadIccProfile().Profile.Bytes, Is.EqualTo(source.ReadIccProfile().Profile.Bytes));
        Assert.That(rewritten.ReadPhotoshop().Metadata.RawBytes, Is.EqualTo(source.ReadPhotoshop().Metadata.RawBytes));
        Assert.That(rewritten.ReadIptc().Metadata.RawBytes, Is.EqualTo(source.ReadIptc().Metadata.RawBytes));
        Assert.That(rewritten.GetRawTagBytes(UnknownMetadataTag), Is.EqualTo(source.GetRawTagBytes(UnknownMetadataTag)));
        Assert.That(rewritten.GetAsciiTagScalar(TiffTagId.Copyright), Is.EqualTo("rewritten copyright"));
        Assert.That(rewritten.GetTag(TiffTagId.Artist), Is.Null);
    }

    [Test]
    public void RemoveMetadata_RemovesFlatAndNestedFamiliesWithoutChangingRaster()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(CreateWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateImageConfiguration(true));
        Assert.That(writer.WriteSourceRaster(imageIndex, new byte[] { 45 }).HasErrors(), Is.False);
        AssertSuccess(writer.WriteExif(imageIndex, CreateExif()));
        AssertSuccess(writer.WriteGps(imageIndex, CreateGps()));
        AssertSuccess(writer.UpdateMetadata(imageIndex, new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.Artist, "temporary"),
            TiffApi.Data.CreateRawTag(UnknownMetadataTag, TiffDataType.Undefined, UnknownBytes.Length, UnknownBytes)
        }));

        TiffDiagnostics removal = writer.RemoveMetadata(imageIndex, new int[] { TiffTagId.Artist, UnknownMetadataTag, TiffTagId.ExifIfd, TiffTagId.GpsIfd });
        TiffDiagnostics flush = writer.Flush();
        TiffImage image = OpenRead(writer.GetFileBytes()).GetImage(0);
        byte[] raster = new byte[1];
        TiffDiagnostics read = image.ReadSourceRasterToBuffer(raster);

        Assert.That(removal.HasErrors(), Is.False, removal.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(image.GetTag(TiffTagId.Artist), Is.Null);
        Assert.That(image.GetTag(UnknownMetadataTag), Is.Null);
        Assert.That(image.ReadExif().Metadata.HasExif, Is.False);
        Assert.That(image.ReadGps().Metadata.HasGps, Is.False);
        Assert.That(read.HasErrors(), Is.False, read.ToString());
        Assert.That(raster, Is.EqualTo(new byte[] { 45 }));
    }

    static byte[] CreateMetadataFile()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(CreateWriteOptions());
        int mainIndex = writer.AddConfiguredImage(CreateImageConfiguration(true));
        int jsonIndex = writer.AddConfiguredImage(CreateImageConfiguration(false));
        int omeIndex = writer.AddConfiguredImage(CreateImageConfiguration(false));
        int imageJIndex = writer.AddConfiguredImage(CreateImageConfiguration(false));
        Assert.That(writer.WriteSourceRaster(mainIndex, new byte[] { 77 }).HasErrors(), Is.False);
        Assert.That(writer.WriteSourceRaster(jsonIndex, new byte[] { 78 }).HasErrors(), Is.False);
        Assert.That(writer.WriteSourceRaster(omeIndex, new byte[] { 79 }).HasErrors(), Is.False);
        Assert.That(writer.WriteSourceRaster(imageJIndex, new byte[] { 80 }).HasErrors(), Is.False);

        AssertSuccess(writer.WriteExif(mainIndex, CreateExif()));
        AssertSuccess(writer.WriteGps(mainIndex, CreateGps()));
        AssertSuccess(writer.WriteXmp(mainIndex, CreateXmp()));
        AssertSuccess(writer.WriteIccProfile(mainIndex, CreateIcc()));
        AssertSuccess(writer.WritePhotoshop(mainIndex, CreatePhotoshop()));
        AssertSuccess(writer.WriteIptc(mainIndex, CreateIptc()));
        AssertSuccess(writer.WriteImageDescription(mainIndex, PlainDescription));
        AssertSuccess(writer.WriteGdalMetadata(mainIndex, CreateGdal()));
        AssertSuccess(writer.WriteGeoTiffMetadata(mainIndex, CreateGeoModel(), CreateGeoKeys(), CreateGeoParameters()));
        AssertSuccess(writer.UpdateMetadata(mainIndex, new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.Artist, "updated artist"),
            TiffApi.Data.CreateAsciiTag(TiffTagId.Software, "remove this value"),
            TiffApi.Data.CreateRawTag(UnknownMetadataTag, TiffDataType.Undefined, UnknownBytes.Length, UnknownBytes)
        }));
        AssertSuccess(writer.RemoveMetadata(mainIndex, new int[] { TiffTagId.Software }));
        AssertSuccess(writer.WriteImageDescription(jsonIndex, JsonDescription));
        AssertSuccess(writer.WriteOmeMetadata(omeIndex, CreateOme()));
        AssertSuccess(writer.WriteImageJMetadata(imageJIndex, CreateImageJ()));
        TiffDiagnostics flush = writer.Flush();
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        return writer.GetFileBytes();
    }

    static TiffWriteOptions CreateWriteOptions()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.BigTiff;
        options.ByteOrder = TiffByteOrder.BigEndian;
        return options;
    }

    static TiffWriterImageConfiguration CreateImageConfiguration(bool compressed)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 1;
        configuration.Length = 1;
        if (compressed)
        {
            configuration.Compression = TiffCompression.PackBits;
        }

        return configuration;
    }

    static TiffFile OpenRead(byte[] bytes)
    {
        TiffReadOptions options = new TiffReadOptions();
        options.PreserveUnknownTags = true;
        return TiffApi.Factory.OpenReadBytes(bytes, options);
    }

    static TiffFile OpenMetadata(byte[] bytes)
    {
        TiffReadOptions options = new TiffReadOptions();
        options.PreserveUnknownTags = true;
        return TiffApi.Factory.OpenMetadataBytes(bytes, options);
    }

    static void AssertMainMetadata(TiffImage image)
    {
        Assert.That(image.ReadExif().Metadata.HasExif, Is.True);
        Assert.That(FindValue(image.ReadExif().Metadata.Values, DateTimeOriginal)!.Text, Is.EqualTo("2026:08:27 09:10:11"));
        Assert.That(image.ReadGps().Metadata.HasGps, Is.True);
        Assert.That(FindValue(image.ReadGps().Metadata.Values, GpsAltitude)!.RationalValues[0].Numerator, Is.EqualTo(1234));
        Assert.That(image.ReadXmp().Packet.Bytes, Is.EqualTo(XmpBytes));
        Assert.That(image.ReadIccProfile().Profile.Bytes, Is.EqualTo(IccBytes));
        Assert.That(image.ReadPhotoshop().Metadata.RawBytes, Is.EqualTo(PhotoshopBytes));
        Assert.That(image.ReadIptc().Metadata.RawBytes, Is.EqualTo(IptcBytes));
        Assert.That(image.ReadGdalMetadata().Metadata.MetadataXml, Does.Contain("AREA_OR_POINT"));
        Assert.That(image.ReadGdalMetadata().Metadata.NoDataText, Is.EqualTo("-9999"));
        Assert.That(image.ReadGeoModel().Metadata.PixelScale, Is.EqualTo(new double[] { 30d, 30d, 0d }));
        Assert.That(image.ReadGeoKeyDirectory().Directory.Keys[0].KeyId, Is.EqualTo(1024));
        Assert.That(image.ReadGeoParameters().Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
    }

    static TiffExifMetadata CreateExif()
    {
        TiffExifMetadata metadata = new TiffExifMetadata();
        metadata.HasExif = true;
        metadata.Values = new TiffTagValue[] { TiffApi.Data.CreateAsciiTag(DateTimeOriginal, "2026:08:27 09:10:11") };
        return metadata;
    }

    static TiffGpsMetadata CreateGps()
    {
        TiffGpsMetadata metadata = new TiffGpsMetadata();
        metadata.HasGps = true;
        metadata.Values = new TiffTagValue[] { TiffApi.Data.CreateRationalScalarTag(GpsAltitude, TiffDataType.Rational, 1234, 10) };
        return metadata;
    }

    static TiffXmpPacket CreateXmp()
    {
        TiffXmpPacket packet = new TiffXmpPacket();
        packet.HasXmp = true;
        packet.DataType = TiffDataType.Byte;
        packet.Bytes = XmpBytes;
        return packet;
    }

    static TiffIccProfile CreateIcc()
    {
        TiffIccProfile profile = new TiffIccProfile();
        profile.HasProfile = true;
        profile.Bytes = IccBytes;
        return profile;
    }

    static TiffPhotoshopMetadata CreatePhotoshop()
    {
        TiffPhotoshopMetadata metadata = new TiffPhotoshopMetadata();
        metadata.HasPhotoshop = true;
        metadata.RawBytes = PhotoshopBytes;
        return metadata;
    }

    static TiffIptcMetadata CreateIptc()
    {
        TiffIptcMetadata metadata = new TiffIptcMetadata();
        metadata.HasIptc = true;
        metadata.RawBytes = IptcBytes;
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
        model.PixelScale = new double[] { 30d, 30d, 0d };
        model.HasTiepoints = true;
        TiffGeoTiepoint tiepoint = new TiffGeoTiepoint();
        tiepoint.ModelX = 100d;
        tiepoint.ModelY = 200d;
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
        parameters.DoubleParams = new double[] { 298.257223563d };
        parameters.HasAsciiParams = true;
        parameters.AsciiText = "WGS 84|";
        return parameters;
    }

    static TiffOmeMetadata CreateOme()
    {
        TiffOmeMetadata metadata = new TiffOmeMetadata();
        metadata.HasOmeXml = true;
        metadata.Xml = OmeXml;
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

    static TiffTagValue? FindValue(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].TagId == tagId)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static byte[] AsciiBytes(string value)
    {
        return Encoding.ASCII.GetBytes(value + "\0");
    }

    static void AssertSuccess(TiffDiagnostics diagnostics)
    {
        Assert.That(diagnostics.HasErrors(), Is.False, DiagnosticText(diagnostics));
    }

    static string DiagnosticText(TiffDiagnostics diagnostics)
    {
        string text = "";
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (text.Length > 0)
            {
                text = text + " | ";
            }

            text = text + diagnostics.Format(diagnostics.Get(index));
            index = index + 1;
        }

        return text;
    }
}
