using System;
using System.IO;

namespace tiff;

/// <summary>Represents the byte order, container kind, and first-directory offset read from a TIFF header.</summary>
public class TiffHeader
{
    /// <summary>Specifies the byte order used for multibyte values.</summary>
    public TiffByteOrder ByteOrder;
    /// <summary>Specifies whether the container uses classic TIFF or BigTIFF offsets.</summary>
    public TiffFileKind FileKind;
    /// <summary>Contains the first IFD offset.</summary>
    public ulong FirstIfdOffset;

    /// <summary>Creates a new instance of the TIFF Header class.</summary>
    public TiffHeader()
    {
        ByteOrder = TiffByteOrder.Unknown;
        FileKind = TiffFileKind.Unknown;
        FirstIfdOffset = 0;
    }

    /// <summary>Creates a new instance of the TIFF Header class.</summary>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <param name="firstIfdOffset">The first IFD offset.</param>
    internal static TiffHeader Create(TiffByteOrder byteOrder, TiffFileKind fileKind, ulong firstIfdOffset)
    {
        TiffHeader value = new TiffHeader();
        value.Initialize(byteOrder, fileKind, firstIfdOffset);
        return value;
    }

    internal void Initialize(TiffByteOrder byteOrder, TiffFileKind fileKind, ulong firstIfdOffset)
    {
        ByteOrder = byteOrder;
        FileKind = fileKind;
        FirstIfdOffset = firstIfdOffset;
    }

    /// <summary>Determines whether is classic under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsClassic()
    {
        return FileKind == TiffFileKind.Classic;
    }

    /// <summary>Determines whether is big TIFF under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsBigTiff()
    {
        return FileKind == TiffFileKind.BigTiff;
    }

    internal static TiffByteOrder DetectByteOrder(ReadOnlySpan<byte> source)
    {
        if (source.Length < 2)
        {
            return TiffByteOrder.Unknown;
        }

        if (source[0] == 0x49 && source[1] == 0x49)
        {
            return TiffByteOrder.LittleEndian;
        }

        if (source[0] == 0x4D && source[1] == 0x4D)
        {
            return TiffByteOrder.BigEndian;
        }

        return TiffByteOrder.Unknown;
    }

    internal static bool IsClassicTiff(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
        {
            return false;
        }

        TiffByteOrder byteOrder = DetectByteOrder(source);
        if (byteOrder == TiffByteOrder.Unknown)
        {
            return false;
        }

        ushort version = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        return version == (ushort)TiffFileKind.Classic;
    }

    internal static bool IsBigTiff(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4)
        {
            return false;
        }

        TiffByteOrder byteOrder = DetectByteOrder(source);
        if (byteOrder == TiffByteOrder.Unknown)
        {
            return false;
        }

        ushort version = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        return version == (ushort)TiffFileKind.BigTiff;
    }

    internal static TiffHeaderReadResult ReadClassic(ReadOnlySpan<byte> source)
    {
        return ReadClassic(source, new TiffReadOptions());
    }

    internal static TiffHeaderReadResult ReadClassic(ReadOnlySpan<byte> source, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffHeader header = new TiffHeader();

        if (source.Length < 8)
        {
            diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "Classic TIFF header requires 8 bytes.", 0);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        TiffByteOrder byteOrder = DetectByteOrder(source);
        header.ByteOrder = byteOrder;
        if (byteOrder == TiffByteOrder.Unknown)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidByteOrder, "Classic TIFF header must start with II or MM byte-order marker.", 0);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        ushort version = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        if (version != (ushort)TiffFileKind.Classic)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidTiffVersion, "Classic TIFF header version must be 42.", 2);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        header.FileKind = TiffFileKind.Classic;
        header.FirstIfdOffset = TiffBinaryReader.ReadUInt32(source.Slice(4, 4), byteOrder);
        ValidateClassicFirstIfdOffset(header.FirstIfdOffset, actualOptions, diagnostics);
        return TiffHeaderReadResult.Create(header, diagnostics);
    }

    internal static TiffHeaderReadResult ReadClassic(Stream stream)
    {
        return ReadClassic(stream, new TiffReadOptions());
    }

    internal static TiffHeaderReadResult ReadClassic(Stream stream, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffHeader header = new TiffHeader();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to read a classic TIFF header.");
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        byte[] buffer = new byte[8];
        int total = 0;
        while (total < 8)
        {
            int read = stream.Read(buffer, total, 8 - total);
            if (read == 0)
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "Classic TIFF header requires 8 bytes.", 0);
                return TiffHeaderReadResult.Create(header, diagnostics);
            }

            total = total + read;
        }

        TiffHeaderReadResult result = ReadClassic(buffer, actualOptions);
        diagnostics.Merge(result.Diagnostics);
        return TiffHeaderReadResult.Create(result.Header, diagnostics);
    }

    internal static TiffHeaderReadResult ReadBigTiff(ReadOnlySpan<byte> source)
    {
        return ReadBigTiff(source, new TiffReadOptions());
    }

    internal static TiffHeaderReadResult ReadBigTiff(ReadOnlySpan<byte> source, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffHeader header = new TiffHeader();

        if (source.Length < 16)
        {
            diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "BigTIFF header requires 16 bytes.", 0);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        TiffByteOrder byteOrder = DetectByteOrder(source);
        header.ByteOrder = byteOrder;
        if (byteOrder == TiffByteOrder.Unknown)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidByteOrder, "BigTIFF header must start with II or MM byte-order marker.", 0);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        ushort version = TiffBinaryReader.ReadUInt16(source.Slice(2, 2), byteOrder);
        if (version != (ushort)TiffFileKind.BigTiff)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidTiffVersion, "BigTIFF header version must be 43.", 2);
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        ushort offsetByteSize = TiffBinaryReader.ReadUInt16(source.Slice(4, 2), byteOrder);
        ushort reserved = TiffBinaryReader.ReadUInt16(source.Slice(6, 2), byteOrder);
        ValidateBigTiffFixedFields(offsetByteSize, reserved, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        header.FileKind = TiffFileKind.BigTiff;
        header.FirstIfdOffset = TiffBinaryReader.ReadUInt64(source.Slice(8, 8), byteOrder);
        ValidateBigTiffFirstIfdOffset(header.FirstIfdOffset, actualOptions, diagnostics);
        return TiffHeaderReadResult.Create(header, diagnostics);
    }

    internal static TiffHeaderReadResult ReadBigTiff(Stream stream)
    {
        return ReadBigTiff(stream, new TiffReadOptions());
    }

    internal static TiffHeaderReadResult ReadBigTiff(Stream stream, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffHeader header = new TiffHeader();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to read a BigTIFF header.");
            return TiffHeaderReadResult.Create(header, diagnostics);
        }

        byte[] buffer = new byte[16];
        int total = 0;
        while (total < 16)
        {
            int read = stream.Read(buffer, total, 16 - total);
            if (read == 0)
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "BigTIFF header requires 16 bytes.", 0);
                return TiffHeaderReadResult.Create(header, diagnostics);
            }

            total = total + read;
        }

        TiffHeaderReadResult result = ReadBigTiff(buffer, actualOptions);
        diagnostics.Merge(result.Diagnostics);
        return TiffHeaderReadResult.Create(result.Header, diagnostics);
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffHeader header)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A header is required to write a classic TIFF header.");
            return diagnostics;
        }

        if (header.FileKind != TiffFileKind.Classic)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "Header file kind must be classic TIFF.");
            return diagnostics;
        }

        return WriteClassic(destination, header.ByteOrder, header.FirstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteClassic(destination, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeWriteOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ValidateClassicWriteRequest(destination.Length, byteOrder, firstIfdOffset, actualOptions, diagnostics);
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        WriteByteOrderMarker(destination, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)TiffFileKind.Classic, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(4, 4), (uint)firstIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteClassic(Stream stream, TiffHeader header)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A header is required to write a classic TIFF header.");
            return diagnostics;
        }

        if (header.FileKind != TiffFileKind.Classic)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "Header file kind must be classic TIFF.");
            return diagnostics;
        }

        return WriteClassic(stream, header.ByteOrder, header.FirstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Stream stream, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteClassic(stream, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Stream stream, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeWriteOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A writable stream is required to write a classic TIFF header.");
            return diagnostics;
        }

        if (!stream.CanWrite)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "Stream must be writable to write a classic TIFF header.");
            return diagnostics;
        }

        byte[] buffer = new byte[8];
        diagnostics.Merge(WriteClassic(buffer, byteOrder, firstIfdOffset, actualOptions));
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        TiffBinaryWriter.WriteBytes(stream, buffer);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffHeader header)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A header is required to write a BigTIFF header.");
            return diagnostics;
        }

        if (header.FileKind != TiffFileKind.BigTiff)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "Header file kind must be BigTIFF.");
            return diagnostics;
        }

        return WriteBigTiff(destination, header.ByteOrder, header.FirstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteBigTiff(destination, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeWriteOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ValidateBigTiffWriteRequest(destination.Length, byteOrder, firstIfdOffset, actualOptions, diagnostics);
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        WriteByteOrderMarker(destination, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)TiffFileKind.BigTiff, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(4, 2), 8, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(6, 2), 0, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(8, 8), firstIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiff(Stream stream, TiffHeader header)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A header is required to write a BigTIFF header.");
            return diagnostics;
        }

        if (header.FileKind != TiffFileKind.BigTiff)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "Header file kind must be BigTIFF.");
            return diagnostics;
        }

        return WriteBigTiff(stream, header.ByteOrder, header.FirstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Stream stream, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteBigTiff(stream, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Stream stream, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeWriteOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A writable stream is required to write a BigTIFF header.");
            return diagnostics;
        }

        if (!stream.CanWrite)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "Stream must be writable to write a BigTIFF header.");
            return diagnostics;
        }

        byte[] buffer = new byte[16];
        diagnostics.Merge(WriteBigTiff(buffer, byteOrder, firstIfdOffset, actualOptions));
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        TiffBinaryWriter.WriteBytes(stream, buffer);
        return diagnostics;
    }

    internal static void ValidateClassicFirstIfdOffset(ulong offset, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (offset == 0 || offset >= 8)
        {
            return;
        }

        if (options.Strict)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "Classic TIFF first IFD offset must be zero or point after the 8-byte header.", 4);
        }
        else
        {
            diagnostics.AddWarningAt(TiffErrorCode.InvalidOffset, "Classic TIFF first IFD offset points inside the header.", 4);
        }
    }

    internal static void ValidateClassicWriteRequest(int destinationLength, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        if (destinationLength < 8)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Classic TIFF header writer requires an 8-byte destination.");
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic TIFF header must be written as little-endian or big-endian.");
        }

        if (!TiffMath.IsClassicOffset(firstIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic TIFF first IFD offset must fit in 32 bits.");
        }

        ValidateClassicWriteFirstIfdOffset(firstIfdOffset, options, diagnostics);
    }

    internal static void ValidateClassicWriteFirstIfdOffset(ulong offset, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        if (offset == 0 || offset >= 8)
        {
            return;
        }

        if (options.Strict)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Classic TIFF first IFD offset must be zero or point after the 8-byte header.");
        }
        else
        {
            diagnostics.AddWarning(TiffErrorCode.InvalidOffset, "Classic TIFF first IFD offset points inside the header.");
        }
    }

    internal static void ValidateBigTiffFixedFields(ushort offsetByteSize, ushort reserved, TiffDiagnostics diagnostics)
    {
        if (offsetByteSize != 8)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidBigTiffHeader, "BigTIFF offset byte-size field must be 8.", 4);
        }

        if (reserved != 0)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidBigTiffHeader, "BigTIFF reserved header field must be zero.", 6);
        }
    }

    internal static void ValidateBigTiffFirstIfdOffset(ulong offset, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (offset == 0 || offset >= 16)
        {
            return;
        }

        if (options.Strict)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "BigTIFF first IFD offset must be zero or point after the 16-byte header.", 8);
        }
        else
        {
            diagnostics.AddWarningAt(TiffErrorCode.InvalidOffset, "BigTIFF first IFD offset points inside the header.", 8);
        }
    }

    internal static void ValidateBigTiffWriteRequest(int destinationLength, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        if (destinationLength < 16)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "BigTIFF header writer requires a 16-byte destination.");
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF header must be written as little-endian or big-endian.");
        }

        ValidateBigTiffWriteFirstIfdOffset(firstIfdOffset, options, diagnostics);
    }

    internal static void ValidateBigTiffWriteFirstIfdOffset(ulong offset, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        if (offset == 0 || offset >= 16)
        {
            return;
        }

        if (options.Strict)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "BigTIFF first IFD offset must be zero or point after the 16-byte header.");
        }
        else
        {
            diagnostics.AddWarning(TiffErrorCode.InvalidOffset, "BigTIFF first IFD offset points inside the header.");
        }
    }

    internal static TiffReadOptions NormalizeOptions(TiffReadOptions options)
    {
        if (options == null)
        {
            return new TiffReadOptions();
        }

        return options;
    }

    internal static TiffWriteOptions NormalizeWriteOptions(TiffWriteOptions options)
    {
        if (options == null)
        {
            return new TiffWriteOptions();
        }

        return options;
    }

    internal static void WriteByteOrderMarker(Span<byte> destination, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            destination[0] = 0x49;
            destination[1] = 0x49;
            return;
        }

        if (byteOrder == TiffByteOrder.BigEndian)
        {
            destination[0] = 0x4D;
            destination[1] = 0x4D;
        }
    }
}
