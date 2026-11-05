using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageJMetadataTests
{
    [Test]
    public void ReadImageJMetadata_ExtractsHyperstackFieldsFromImageDescription()
    {
        string description = "ImageJ=1.54f\nimages=24\nchannels=3\nslices=4\nframes=2\nhyperstack=true\nmode=composite\nunit=micron\nspacing=0.25\n";
        TiffDirectory directory = CreateImageDescriptionDirectory(description, 64, out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasImageJ, Is.True);
        Assert.That(result.Metadata.Version, Is.EqualTo("1.54f"));
        Assert.That(result.Metadata.ImageCount, Is.EqualTo(24));
        Assert.That(result.Metadata.Channels, Is.EqualTo(3));
        Assert.That(result.Metadata.Slices, Is.EqualTo(4));
        Assert.That(result.Metadata.Frames, Is.EqualTo(2));
        Assert.That(result.Metadata.IsHyperstack, Is.True);
        Assert.That(result.Metadata.Mode, Is.EqualTo("composite"));
        Assert.That(result.Metadata.Unit, Is.EqualTo("micron"));
        Assert.That(result.Metadata.Spacing, Is.EqualTo("0.25"));
        Assert.That(result.Metadata.EntryCount(), Is.EqualTo(9));
        Assert.That(result.Metadata.GetValue("MODE"), Is.EqualTo("composite"));
        Assert.That(result.Metadata.RawDescription, Is.EqualTo(description));
        Assert.That(result.Metadata.RawBytes.Length, Is.EqualTo(description.Length + 1));
    }

    [Test]
    public void ReadImageJMetadata_ReturnsEmptyForPlainImageDescription()
    {
        TiffDirectory directory = CreateImageDescriptionDirectory("plain description", 64, out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasImageJ, Is.False);
        Assert.That(result.Metadata.RawDescription, Is.EqualTo("plain description"));
        Assert.That(result.Metadata.EntryCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReadImageJMetadata_ReturnsEmptyWhenImageDescriptionIsMissing()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, 100) });

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasImageJ, Is.False);
        Assert.That(result.Metadata.Version, Is.EqualTo(""));
    }

    [Test]
    public void ReadImageJMetadata_LeavesMalformedNumericFieldsAsZero()
    {
        string description = "ImageJ=1.53\nimages=abc\nchannels=2x\nslices=5\nhyperstack=yes\n";
        TiffDirectory directory = CreateImageDescriptionDirectory(description, 64, out byte[] fileBytes);

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasImageJ, Is.True);
        Assert.That(result.Metadata.ImageCount, Is.EqualTo(0));
        Assert.That(result.Metadata.Channels, Is.EqualTo(0));
        Assert.That(result.Metadata.Slices, Is.EqualTo(5));
        Assert.That(result.Metadata.IsHyperstack, Is.True);
    }

    [Test]
    public void ReadImageJMetadata_PropagatesInvalidImageDescriptionTagType()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateEntry(TiffTagId.ImageDescription, TiffDataType.Long, 1, 100) });

        TiffImageJMetadataReadResult result = TiffCodec.ReadImageJMetadata(directory, new byte[128]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageDescription));
    }

    [Test]
    public void ParseEntries_TrimsWhitespaceAndCarriageReturns()
    {
        TiffImageJMetadata metadata = TiffImageJMetadataReader.Parse("ImageJ = 1.52\r\nhyperstack = true\r\n", new byte[0]);

        Assert.That(metadata.HasImageJ, Is.True);
        Assert.That(metadata.Version, Is.EqualTo("1.52"));
        Assert.That(metadata.IsHyperstack, Is.True);
        Assert.That(metadata.EntryCount(), Is.EqualTo(2));
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
