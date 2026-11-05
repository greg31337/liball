using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageJMetadataWriterTests
{
    [Test]
    public void WriteClassicImageJMetadata_WritesAndReadsHyperstackFields()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16) };
        TiffImageJMetadata metadata = CreateHyperstackMetadata();
        byte[] fileBytes = new byte[8 + TiffImageJMetadataWriter.CalculateClassicWriteByteCount(values, metadata)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffImageJMetadataWriteResult writeResult = TiffCodec.WriteClassicImageJMetadata(fileBytes, values, metadata, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffImageJMetadataReadResult readResult = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.FindEntry(TiffTagId.ImageDescription)!.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Metadata.HasImageJ, Is.True);
        Assert.That(readResult.Metadata.Version, Is.EqualTo("1.54f"));
        Assert.That(readResult.Metadata.ImageCount, Is.EqualTo(24));
        Assert.That(readResult.Metadata.Channels, Is.EqualTo(3));
        Assert.That(readResult.Metadata.Slices, Is.EqualTo(4));
        Assert.That(readResult.Metadata.Frames, Is.EqualTo(2));
        Assert.That(readResult.Metadata.IsHyperstack, Is.True);
        Assert.That(readResult.Metadata.Mode, Is.EqualTo("composite"));
        Assert.That(readResult.Metadata.Unit, Is.EqualTo("micron"));
        Assert.That(readResult.Metadata.Spacing, Is.EqualTo("0.25"));
    }

    [Test]
    public void WriteClassicImageJMetadata_PreservesCustomEntries()
    {
        TiffImageJMetadata metadata = TiffImageJMetadata.Create(
            true,
            true,
            "",
            0,
            0,
            0,
            0,
            "",
            "",
            "",
            "",
            new byte[0],
            new TiffImageJMetadataEntry[]
            {
                TiffImageJMetadataEntry.Create("ImageJ", "1.53"),
                TiffImageJMetadataEntry.Create("hyperstack", "true"),
                TiffImageJMetadataEntry.Create("custom", "kept")
            });
        byte[] fileBytes = new byte[8 + TiffImageJMetadataWriter.CalculateClassicWriteByteCount(new TiffTagValue[0], metadata)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffImageJMetadataWriteResult writeResult = TiffCodec.WriteClassicImageJMetadata(fileBytes, new TiffTagValue[0], metadata, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffImageJMetadataReadResult readResult = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Metadata.Version, Is.EqualTo("1.53"));
        Assert.That(readResult.Metadata.GetValue("custom"), Is.EqualTo("kept"));
    }

    [Test]
    public void WriteClassicImageJMetadata_ReplacesExistingImageDescription()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 16),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "old")
        };
        TiffImageJMetadata metadata = CreateHyperstackMetadata();
        byte[] fileBytes = new byte[8 + TiffImageJMetadataWriter.CalculateClassicWriteByteCount(values, metadata)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffImageJMetadataWriteResult writeResult = TiffCodec.WriteClassicImageJMetadata(fileBytes, values, metadata, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffImageJMetadataReadResult readResult = TiffCodec.ReadImageJMetadata(directory, fileBytes);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(2));
        Assert.That(readResult.Metadata.HasImageJ, Is.True);
        Assert.That(readResult.Metadata.Version, Is.EqualTo("1.54f"));
    }

    [Test]
    public void WriteClassicImageJMetadata_RejectsMissingImageJMetadata()
    {
        TiffImageJMetadata metadata = new TiffImageJMetadata();

        TiffImageJMetadataWriteResult result = TiffCodec.WriteClassicImageJMetadata(new byte[128], new TiffTagValue[0], metadata, TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ImageDescription));
    }

    [Test]
    public void WriteClassicImageJMetadata_ReportsShortDestination()
    {
        TiffImageJMetadataWriteResult result = TiffCodec.WriteClassicImageJMetadata(new byte[16], new TiffTagValue[0], CreateHyperstackMetadata(), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    static TiffImageJMetadata CreateHyperstackMetadata()
    {
        return TiffImageJMetadata.Create(true, true, "1.54f", 24, 3, 4, 2, "composite", "micron", "0.25", "", new byte[0], new TiffImageJMetadataEntry[0]);
    }
}
