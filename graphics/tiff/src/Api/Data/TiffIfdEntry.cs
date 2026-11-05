using System;
using System.IO;

namespace tiff;

/// <summary>Represents one TIFF image file directory entry, including its encoded value location.</summary>
public class TiffIfdEntry
{
    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;
    /// <summary>Specifies the TIFF field data type.</summary>
    public TiffDataType DataType;
    /// <summary>Contains the number of elements represented by the value.</summary>
    public long Count;
    /// <summary>Contains the raw count.</summary>
    public ulong RawCount;
    /// <summary>Contains the value offset.</summary>
    public ulong ValueOffset;
    /// <summary>Contains the value offset bytes.</summary>
    public byte[] ValueOffsetBytes;
    /// <summary>Contains the value byte count.</summary>
    public long ValueByteCount;
    /// <summary>Contains the value slot byte count.</summary>
    public int ValueSlotByteCount;

    /// <summary>Creates a new instance of the TIFF IFD Entry class.</summary>
    public TiffIfdEntry()
    {
        TagId = 0;
        DataType = TiffDataType.Unknown;
        Count = 0;
        RawCount = 0;
        ValueOffset = 0;
        ValueOffsetBytes = new byte[0];
        ValueByteCount = 0;
        ValueSlotByteCount = 0;
    }

    /// <summary>Determines whether the entry value fits in the classic inline value slot.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool FitsClassicInline()
    {
        return ValueByteCount <= 4;
    }

    /// <summary>Determines whether the entry value fits in the big TIFF inline value slot.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool FitsBigTiffInline()
    {
        return ValueByteCount <= 8;
    }

    /// <summary>Determines whether has external value under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasExternalValue()
    {
        return ValueByteCount > ValueSlotByteCount;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF IFD entry.</returns>
    internal TiffIfdEntry Copy()
    {
        TiffIfdEntry copy = new TiffIfdEntry();
        copy.TagId = TagId;
        copy.DataType = DataType;
        copy.Count = Count;
        copy.RawCount = RawCount;
        copy.ValueOffset = ValueOffset;
        copy.ValueOffsetBytes = CopyBytes(ValueOffsetBytes);
        copy.ValueByteCount = ValueByteCount;
        copy.ValueSlotByteCount = ValueSlotByteCount;
        return copy;
    }

    internal static TiffIfdEntryReadResult ReadClassic(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry entry = new TiffIfdEntry();

        if (source.Length < 12)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "Classic IFD entry requires 12 bytes.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic IFD entry requires a known byte order.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        entry.TagId = TiffBinaryReader.ReadUInt16(source.Slice(0, 2), byteOrder);
        ushort rawDataType = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        entry.DataType = (TiffDataType)rawDataType;
        entry.Count = TiffBinaryReader.ReadUInt32(source.Slice(4, 4), byteOrder);
        entry.RawCount = (ulong)entry.Count;
        entry.ValueOffset = TiffBinaryReader.ReadUInt32(source.Slice(8, 4), byteOrder);
        entry.ValueOffsetBytes = CopyBytes(source.Slice(8, 4));
        entry.ValueSlotByteCount = 4;

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagType, "Classic IFD entry contains an unknown TIFF data type.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(entry.DataType, entry.Count);
        return TiffIfdEntryReadResult.Create(entry, diagnostics);
    }

    internal static TiffIfdEntryReadResult ReadBigTiff(ReadOnlySpan<byte> source, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry entry = new TiffIfdEntry();

        if (source.Length < 20)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "BigTIFF IFD entry requires 20 bytes.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF IFD entry requires a known byte order.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        entry.TagId = TiffBinaryReader.ReadUInt16(source.Slice(0, 2), byteOrder);
        ushort rawDataType = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        entry.DataType = (TiffDataType)rawDataType;
        entry.RawCount = TiffBinaryReader.ReadUInt64(source.Slice(4, 8), byteOrder);
        entry.ValueOffset = TiffBinaryReader.ReadUInt64(source.Slice(12, 8), byteOrder);
        entry.ValueOffsetBytes = CopyBytes(source.Slice(12, 8));
        entry.ValueSlotByteCount = 8;

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagType, "BigTIFF IFD entry contains an unknown TIFF data type.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        SetCheckedValueByteCount(entry, diagnostics);
        return TiffIfdEntryReadResult.Create(entry, diagnostics);
    }

    internal static TiffIfdEntryReadResult ReadClassic(Stream stream, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry entry = new TiffIfdEntry();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to read a classic IFD entry.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        byte[] buffer = new byte[12];
        int total = 0;
        while (total < 12)
        {
            int read = stream.Read(buffer, total, 12 - total);
            if (read == 0)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "Classic IFD entry requires 12 bytes.");
                return TiffIfdEntryReadResult.Create(entry, diagnostics);
            }

            total = total + read;
        }

        TiffIfdEntryReadResult result = ReadClassic(buffer, byteOrder);
        diagnostics.Merge(result.Diagnostics);
        return TiffIfdEntryReadResult.Create(result.Entry, diagnostics);
    }

    internal static TiffIfdEntryReadResult ReadBigTiff(Stream stream, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdEntry entry = new TiffIfdEntry();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to read a BigTIFF IFD entry.");
            return TiffIfdEntryReadResult.Create(entry, diagnostics);
        }

        byte[] buffer = new byte[20];
        int total = 0;
        while (total < 20)
        {
            int read = stream.Read(buffer, total, 20 - total);
            if (read == 0)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "BigTIFF IFD entry requires 20 bytes.");
                return TiffIfdEntryReadResult.Create(entry, diagnostics);
            }

            total = total + read;
        }

        TiffIfdEntryReadResult result = ReadBigTiff(buffer, byteOrder);
        diagnostics.Merge(result.Diagnostics);
        return TiffIfdEntryReadResult.Create(result.Entry, diagnostics);
    }

    internal static void SetCheckedValueByteCount(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        if (entry.RawCount > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "BigTIFF IFD entry count is larger than supported signed range.");
            return;
        }

        int elementSize = TiffDataTypeInfo.GetElementSize(entry.DataType);
        if (entry.RawCount > (ulong)(long.MaxValue / elementSize))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "BigTIFF IFD entry value byte count overflows supported signed range.");
            return;
        }

        entry.Count = (long)entry.RawCount;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(entry.DataType, entry.Count);
    }

    internal static byte[] CopyBytes(ReadOnlySpan<byte> source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
