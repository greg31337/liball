using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffOmeMetadataWriterTests
{
    [Test]
    public void WriteClassicOmeMetadata_WritesImageDescriptionAndReadsOmeXml()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16) };
        string omeXml = CreateOmeXml(16, 8);
        byte[] fileBytes = new byte[8 + TiffOmeMetadataWriter.CalculateClassicWriteByteCount(values, omeXml)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffOmeMetadataWriteResult writeResult = TiffCodec.WriteClassicOmeMetadata(fileBytes, values, omeXml, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffOmeMetadataReadResult readResult = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.FindEntry(TiffTagId.ImageDescription)!.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasOmeXml, Is.True);
        Assert.That(readResult.Metadata.Xml, Is.EqualTo(omeXml));
    }

    [Test]
    public void WriteClassicOmeMetadata_ReplacesExistingImageDescription()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "old")
        };
        string omeXml = CreateOmeXml(32, 24);
        byte[] fileBytes = new byte[8 + TiffOmeMetadataWriter.CalculateClassicWriteByteCount(values, omeXml)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffOmeMetadataWriteResult writeResult = TiffCodec.WriteClassicOmeMetadata(fileBytes, values, omeXml, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffOmeMetadataReadResult readResult = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(2));
        Assert.That(readResult.Metadata.Xml, Is.EqualTo(omeXml));
    }

    [Test]
    public void WriteClassicOmeMetadata_RejectsPlainImageDescription()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16) };

        TiffOmeMetadataWriteResult result = TiffCodec.WriteClassicOmeMetadata(new byte[128], values, "plain description", TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageDescription));
    }

    [Test]
    public void WriteClassicOmeMetadata_ReportsShortDestination()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16) };

        TiffOmeMetadataWriteResult result = TiffCodec.WriteClassicOmeMetadata(new byte[16], values, CreateOmeXml(16, 8), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void CalculateClassicWriteByteCount_ReturnsZeroForNonOmeXml()
    {
        int byteCount = TiffOmeMetadataWriter.CalculateClassicWriteByteCount(new TiffTagValue[0], "plain");

        Assert.That(byteCount, Is.EqualTo(0));
    }

    static string CreateOmeXml(int sizeX, int sizeY)
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?><OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" SizeX=\"" + sizeX.ToString() + "\" SizeY=\"" + sizeY.ToString() + "\" /></Image></OME>";
    }
}
