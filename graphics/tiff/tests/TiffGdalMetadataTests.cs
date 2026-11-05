using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGdalMetadataTests
{
    [Test]
    public void WriteClassicGdalMetadata_WritesAndReadsMetadataAndNoData()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        TiffGdalMetadata metadata = CreateMetadata("<GDALMetadata><Item name=\"AREA_OR_POINT\">Area</Item></GDALMetadata>", "-9999");
        byte[] fileBytes = new byte[8 + TiffGdalMetadataWriter.CalculateClassicWriteByteCount(values, metadata)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffGdalMetadataWriteResult writeResult = TiffCodec.WriteClassicGdalMetadata(fileBytes, values, metadata, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];
        TiffGdalMetadataReadResult readResult = TiffCodec.ReadGdalMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(3));
        Assert.That(directory.FindEntry(TiffTagId.GdalMetadata)!.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(directory.FindEntry(TiffTagId.GdalNoData)!.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasMetadata, Is.True);
        Assert.That(readResult.Metadata.MetadataXml, Is.EqualTo("<GDALMetadata><Item name=\"AREA_OR_POINT\">Area</Item></GDALMetadata>"));
        Assert.That(readResult.Metadata.HasNoData, Is.True);
        Assert.That(readResult.Metadata.NoDataText, Is.EqualTo("-9999"));
        Assert.That(readResult.Metadata.MetadataRawBytes[readResult.Metadata.MetadataRawBytes.Length - 1], Is.EqualTo(0));
    }

    [Test]
    public void ReadGdalMetadata_ReturnsEmptyWhenTagsAreMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffGdalMetadataReadResult result = TiffCodec.ReadGdalMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasMetadata, Is.False);
        Assert.That(result.Metadata.HasNoData, Is.False);
        Assert.That(result.Metadata.MetadataXml, Is.EqualTo(""));
        Assert.That(result.Metadata.NoDataText, Is.EqualTo(""));
    }

    [Test]
    public void WriteClassicGdalMetadata_ReplacesExistingGdalTagsAndPreservesOtherValues()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffGdalMetadataWriter.CreateMetadataValue("old"),
            TiffGdalMetadataWriter.CreateNoDataValue("0")
        };
        TiffGdalMetadata metadata = CreateMetadata("new", "255");
        byte[] fileBytes = new byte[8 + TiffGdalMetadataWriter.CalculateClassicWriteByteCount(values, metadata)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffGdalMetadataWriteResult writeResult = TiffCodec.WriteClassicGdalMetadata(fileBytes, values, metadata, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffGdalMetadataReadResult readResult = TiffCodec.ReadGdalMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(3));
        Assert.That(readResult.Metadata.MetadataXml, Is.EqualTo("new"));
        Assert.That(readResult.Metadata.NoDataText, Is.EqualTo("255"));
    }

    [Test]
    public void ReadGdalMetadata_ReportsInvalidMetadataTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.GdalMetadata, TiffDataType.Byte, 4, 64) });

        TiffGdalMetadataReadResult result = TiffCodec.ReadGdalMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GdalMetadata));
    }

    [Test]
    public void ReadGdalMetadata_ReportsInvalidAscii()
    {
        TiffIfdEntry entry = CreateEntry(TiffTagId.GdalNoData, TiffDataType.Ascii, 3, 0);
        entry.ValueOffsetBytes = new byte[] { 45, 49, 50, 0 };
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { entry });

        TiffGdalMetadataReadResult result = TiffCodec.ReadGdalMetadata(directory, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
    }

    [Test]
    public void WriteClassicGdalMetadata_ReportsShortDestination()
    {
        TiffGdalMetadata metadata = CreateMetadata("metadata", "-1");

        TiffGdalMetadataWriteResult result = TiffCodec.WriteClassicGdalMetadata(new byte[16], new TiffTagValue[0], metadata, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffGdalMetadata CreateMetadata(string metadataXml, string noDataText)
    {
        return TiffGdalMetadata.Create(true, metadataXml, new string[] { metadataXml }, new byte[0], true, noDataText, new string[] { noDataText }, new byte[0]);
    }

    static TiffIfdEntry CreateEntry(int tagId, TiffDataType dataType, long count, ulong valueOffset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueOffsetBytes = new byte[4];
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
