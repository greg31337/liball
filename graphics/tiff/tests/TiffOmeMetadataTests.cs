using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffOmeMetadataTests
{
    [Test]
    public void ReadOmeMetadata_ExtractsOmeXmlFromImageDescription()
    {
        string omeXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" SizeX=\"16\" SizeY=\"8\" /></Image></OME>";
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16) };
        byte[] fileBytes = new byte[8 + TiffImageDescriptionWriter.CalculateClassicWriteByteCount(values, omeXml)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffCodec.WriteClassicImageDescription(fileBytes, values, omeXml, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.True);
        Assert.That(result.Metadata.Xml, Is.EqualTo(omeXml));
        Assert.That(result.Metadata.ImageDescriptionText, Is.EqualTo(omeXml));
        Assert.That(result.Metadata.ImageDescriptionRawBytes.Length, Is.EqualTo(omeXml.Length + 1));
    }

    [Test]
    public void ReadOmeMetadata_RecognizesWhitespaceBeforeOmeRoot()
    {
        string description = "  \n<OME><Image ID=\"Image:0\" /></OME>";
        TiffDirectory directory = CreateImageDescriptionDirectory(description, 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.True);
        Assert.That(result.Metadata.Xml, Is.EqualTo("<OME><Image ID=\"Image:0\" /></OME>"));
    }

    [Test]
    public void ReadOmeMetadata_ReturnsEmptyForPlainImageDescription()
    {
        TiffDirectory directory = CreateImageDescriptionDirectory("plain description", 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.False);
        Assert.That(result.Metadata.Xml, Is.EqualTo(""));
        Assert.That(result.Metadata.ImageDescriptionText, Is.EqualTo("plain description"));
    }

    [Test]
    public void ReadOmeMetadata_ReturnsEmptyWhenImageDescriptionIsMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.False);
        Assert.That(result.Metadata.Xml, Is.EqualTo(""));
    }

    [Test]
    public void ReadOmeMetadata_PropagatesInvalidImageDescriptionTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageDescription, TiffDataType.Long, 1, 100) });

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageDescription));
    }

    [Test]
    public void IsOmeXml_ReturnsFalseForIncompleteXmlDeclaration()
    {
        Assert.That(TiffOmeMetadataReader.IsOmeXml("<?xml version=\"1.0\" <OME />"), Is.False);
    }

    static TiffDirectory CreateImageDescriptionDirectory(string text, int offset, out byte[] fileBytes)
    {
        byte[] encoded = TiffAscii.EncodeString(text);
        fileBytes = new byte[offset + encoded.Length + 16];
        WriteBytes(fileBytes, offset, encoded);
        return TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageDescription, TiffDataType.Ascii, encoded.Length, (ulong)offset) });
    }

    static void WriteBytes(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
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
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)valueOffset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        return entry;
    }
}
