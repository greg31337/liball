using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPhotoshopIptcMetadataTests
{
    [Test]
    public void ReadPhotoshop_ExtractsImageResourceBlocks()
    {
        byte[] resourceBytes = CreatePhotoshopResourceBytes();
        byte[] fileBytes = WithPayload(resourceBytes, 64);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Photoshop, TiffDataType.Byte, resourceBytes.Length, 64) });

        TiffPhotoshopReadResult result = TiffCodec.ReadPhotoshop(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasPhotoshop, Is.True);
        Assert.That(result.Metadata.RawBytes, Is.EqualTo(resourceBytes));
        Assert.That(result.Metadata.BlockCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.Blocks[0].Signature, Is.EqualTo("8BIM"));
        Assert.That(result.Metadata.Blocks[0].ResourceId, Is.EqualTo(0x0404));
        Assert.That(result.Metadata.Blocks[0].Name, Is.EqualTo(""));
        Assert.That(result.Metadata.Blocks[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.Metadata.Blocks[1].Name, Is.EqualTo("dpi"));
        Assert.That(result.Metadata.Blocks[1].Data, Is.EqualTo(new byte[] { 9, 10 }));
    }

    [Test]
    public void ReadIptc_ExtractsDatasets()
    {
        byte[] iptcBytes = new byte[] { 0x1c, 2, 5, 0, 4, 116, 101, 115, 116, 0x1c, 2, 120, 0, 2, 1, 2 };
        byte[] fileBytes = WithPayload(iptcBytes, 64);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Iptc, TiffDataType.Undefined, iptcBytes.Length, 64) });

        TiffIptcReadResult result = TiffCodec.ReadIptc(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasIptc, Is.True);
        Assert.That(result.Metadata.RawBytes, Is.EqualTo(iptcBytes));
        Assert.That(result.Metadata.BlockCount(), Is.EqualTo(2));
        Assert.That(result.Metadata.Blocks[0].RecordNumber, Is.EqualTo(2));
        Assert.That(result.Metadata.Blocks[0].DataSetNumber, Is.EqualTo(5));
        Assert.That(result.Metadata.Blocks[0].Data, Is.EqualTo(new byte[] { 116, 101, 115, 116 }));
        Assert.That(result.Metadata.Blocks[1].DataSetNumber, Is.EqualTo(120));
        Assert.That(result.Metadata.Blocks[1].Data, Is.EqualTo(new byte[] { 1, 2 }));
    }

    [Test]
    public void ReadPhotoshop_ReturnsEmptyWhenTagIsMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[0]);

        TiffPhotoshopReadResult result = TiffCodec.ReadPhotoshop(directory, new byte[16]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasPhotoshop, Is.False);
        Assert.That(result.Metadata.BlockCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadPhotoshop_ReportsTruncatedResourceBlock()
    {
        byte[] resourceBytes = new byte[] { 56, 66, 73, 77, 4 };
        byte[] fileBytes = WithPayload(resourceBytes, 32);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Photoshop, TiffDataType.Byte, resourceBytes.Length, 32) });

        TiffPhotoshopReadResult result = TiffCodec.ReadPhotoshop(directory, fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ReadIptc_ReportsInvalidTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Iptc, TiffDataType.Long, 1, 100) });

        TiffIptcReadResult result = TiffCodec.ReadIptc(directory, new byte[16]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.Iptc));
    }

    [Test]
    public void ReadIptc_ReportsInvalidDatasetMarker()
    {
        byte[] iptcBytes = new byte[] { 0, 2, 5, 0, 1, 10 };
        byte[] fileBytes = WithPayload(iptcBytes, 32);
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.Iptc, TiffDataType.Undefined, iptcBytes.Length, 32) });

        TiffIptcReadResult result = TiffCodec.ReadIptc(directory, fileBytes);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static byte[] CreatePhotoshopResourceBytes()
    {
        return new byte[]
        {
            56, 66, 73, 77, 4, 4, 0, 0, 0, 0, 0, 3, 1, 2, 3, 0,
            56, 66, 73, 77, 3, 237, 3, 100, 112, 105, 0, 0, 0, 2, 9, 10
        };
    }

    static byte[] WithPayload(byte[] payload, int offset)
    {
        byte[] bytes = new byte[offset + payload.Length];
        int index = 0;
        while (index < payload.Length)
        {
            bytes[offset + index] = payload[index];
            index = index + 1;
        }

        return bytes;
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
