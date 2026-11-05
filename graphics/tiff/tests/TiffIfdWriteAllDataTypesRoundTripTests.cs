using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffIfdWriteAllDataTypesRoundTripTests
{
    [Test]
    public void WriteClassicWithValueBytes_RoundTripsAllClassicTiffDataTypes()
    {
        TiffDataType[] dataTypes = GetClassicDataTypes();
        TiffIfdEntry[] entries = CreateEntries(dataTypes);
        byte[][] values = CreateValueBytes(dataTypes);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.BigEndian, 8);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, values, TiffByteOrder.BigEndian, 8, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.BigEndian, 8);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(dataTypes.Length));
        AssertRoundTrippedValues(directoryResult.Directories[0], fileBytes, dataTypes, values);
    }

    [Test]
    public void WriteBigTiffWithValueBytes_RoundTripsAllKnownTiffDataTypes()
    {
        TiffDataType[] dataTypes = GetAllKnownDataTypes();
        TiffIfdEntry[] entries = CreateEntries(dataTypes);
        byte[][] values = CreateValueBytes(dataTypes);
        int layoutByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryLayoutByteCount(entries, values);
        byte[] fileBytes = new byte[16 + layoutByteCount];
        TiffHeader.WriteBigTiff(fileBytes.AsSpan(0, 16), TiffByteOrder.LittleEndian, 16);

        TiffDiagnostics diagnostics = TiffDirectoryWriter.WriteBigTiffWithValueBytes(fileBytes.AsSpan(16, layoutByteCount), entries, values, TiffByteOrder.LittleEndian, 16, 0);
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadBigTiff(fileBytes, TiffByteOrder.LittleEndian, 16);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(directoryResult.Success, Is.True);
        Assert.That(directoryResult.DirectoryCount(), Is.EqualTo(1));
        Assert.That(directoryResult.Directories[0].EntryCount(), Is.EqualTo(dataTypes.Length));
        AssertRoundTrippedValues(directoryResult.Directories[0], fileBytes, dataTypes, values);
    }

    static void AssertRoundTrippedValues(TiffDirectory directory, byte[] fileBytes, TiffDataType[] dataTypes, byte[][] values)
    {
        int index = 0;
        while (index < dataTypes.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes);

            Assert.That(entry.TagId, Is.EqualTo(CreateTagId(index)));
            Assert.That(entry.DataType, Is.EqualTo(dataTypes[index]));
            Assert.That(entry.Count, Is.EqualTo(GetRoundTripCount(dataTypes[index])));
            Assert.That(entry.ValueByteCount, Is.EqualTo(values[index].Length));
            Assert.That(loadResult.Success, Is.True);
            Assert.That(loadResult.Bytes, Is.EqualTo(values[index]));

            index = index + 1;
        }
    }

    static TiffIfdEntry[] CreateEntries(TiffDataType[] dataTypes)
    {
        TiffIfdEntry[] entries = new TiffIfdEntry[dataTypes.Length];
        int index = 0;
        while (index < dataTypes.Length)
        {
            TiffIfdEntry entry = new TiffIfdEntry();
            entry.TagId = CreateTagId(index);
            entry.DataType = dataTypes[index];
            entry.Count = GetRoundTripCount(dataTypes[index]);
            entry.RawCount = (ulong)entry.Count;
            entry.ValueOffset = 0;
            entry.ValueOffsetBytes = new byte[0];
            entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(entry.DataType, entry.Count);
            entry.ValueSlotByteCount = 0;
            entries[index] = entry;
            index = index + 1;
        }

        return entries;
    }

    static byte[][] CreateValueBytes(TiffDataType[] dataTypes)
    {
        byte[][] values = new byte[dataTypes.Length][];
        int index = 0;
        while (index < dataTypes.Length)
        {
            int byteCount = (int)TiffDataTypeInfo.CalculateValueByteCount(dataTypes[index], GetRoundTripCount(dataTypes[index]));
            values[index] = CreateValueBytes(byteCount, index);
            index = index + 1;
        }

        return values;
    }

    static byte[] CreateValueBytes(int byteCount, int seed)
    {
        byte[] bytes = new byte[byteCount];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)((seed * 17 + index + 1) & 0xFF);
            index = index + 1;
        }

        if (bytes.Length > 0)
        {
            bytes[bytes.Length - 1] = 0;
        }

        return bytes;
    }

    static int GetRoundTripCount(TiffDataType dataType)
    {
        int elementSize = TiffDataTypeInfo.GetElementSize(dataType);
        if (elementSize == 1)
        {
            return 9;
        }

        if (elementSize == 2)
        {
            return 5;
        }

        if (elementSize == 4)
        {
            return 3;
        }

        return 2;
    }

    static int CreateTagId(int index)
    {
        return 65000 + index;
    }

    static TiffDataType[] GetClassicDataTypes()
    {
        return new TiffDataType[]
        {
            TiffDataType.Byte,
            TiffDataType.Ascii,
            TiffDataType.Short,
            TiffDataType.Long,
            TiffDataType.Rational,
            TiffDataType.SByte,
            TiffDataType.Undefined,
            TiffDataType.SShort,
            TiffDataType.SLong,
            TiffDataType.SRational,
            TiffDataType.Float,
            TiffDataType.Double,
            TiffDataType.Ifd
        };
    }

    static TiffDataType[] GetAllKnownDataTypes()
    {
        return new TiffDataType[]
        {
            TiffDataType.Byte,
            TiffDataType.Ascii,
            TiffDataType.Short,
            TiffDataType.Long,
            TiffDataType.Rational,
            TiffDataType.SByte,
            TiffDataType.Undefined,
            TiffDataType.SShort,
            TiffDataType.SLong,
            TiffDataType.SRational,
            TiffDataType.Float,
            TiffDataType.Double,
            TiffDataType.Ifd,
            TiffDataType.Long8,
            TiffDataType.SLong8,
            TiffDataType.Ifd8
        };
    }
}
