using System;

namespace raw;

internal class TiffHeader
{
    internal RawByteOrder ByteOrder;
    internal bool BigTiff;
    internal ushort Version;
    internal int OffsetSize;
    internal ulong FirstIfdOffset;
    internal long BaseOffset;
    internal int HeaderSize;

    internal TiffHeader(RawByteOrder byteOrder, bool bigTiff, ushort version, int offsetSize, ulong firstIfdOffset, long baseOffset, int headerSize)
    {
        ByteOrder = byteOrder;
        BigTiff = bigTiff;
        Version = version;
        OffsetSize = offsetSize;
        FirstIfdOffset = firstIfdOffset;
        BaseOffset = baseOffset;
        HeaderSize = headerSize;
    }

    internal static TiffHeader Parse(BoundedInput? input)
    {
        if (input == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A bounded TIFF input is required.", -1);
        }

        long baseOffset = input.GetAbsolutePosition();
        byte[] bytes = new byte[8];
        input.ReadExactly(bytes.AsSpan(0, 2));

        RawByteOrder byteOrder;
        if (bytes[0] == 0x49 && bytes[1] == 0x49)
        {
            byteOrder = RawByteOrder.LittleEndian;
        }
        else if (bytes[0] == 0x4D && bytes[1] == 0x4D)
        {
            byteOrder = RawByteOrder.BigEndian;
        }
        else
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "The TIFF byte-order signature is invalid.", baseOffset);
        }

        input.ReadExactly(bytes.AsSpan(0, 2));
        ushort version = EndianReader.ReadUInt16(bytes.AsSpan(0, 2), byteOrder);
        if (version == 42)
        {
            input.ReadExactly(bytes.AsSpan(0, 4));
            ulong firstIfdOffset = EndianReader.ReadUInt32(bytes.AsSpan(0, 4), byteOrder);
            return new TiffHeader(byteOrder, false, version, 4, firstIfdOffset, baseOffset, 8);
        }
        if (version == 43)
        {
            input.ReadExactly(bytes.AsSpan(0, 4));
            ushort offsetSize = EndianReader.ReadUInt16(bytes.AsSpan(0, 2), byteOrder);
            if (offsetSize != 8)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A BigTIFF header must use eight-byte offsets.", CheckedMath.Add(baseOffset, 4));
            }

            ushort reserved = EndianReader.ReadUInt16(bytes.AsSpan(2, 2), byteOrder);
            if (reserved != 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The BigTIFF header reserved field must be zero.", CheckedMath.Add(baseOffset, 6));
            }

            input.ReadExactly(bytes.AsSpan(0, 8));
            ulong firstIfdOffset = EndianReader.ReadUInt64(bytes.AsSpan(0, 8), byteOrder);
            return new TiffHeader(byteOrder, true, version, 8, firstIfdOffset, baseOffset, 16);
        }

        throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "The TIFF version signature is neither classic TIFF nor BigTIFF.", CheckedMath.Add(baseOffset, 2));
    }

    internal long GetFirstIfdAbsoluteOffset()
    {
        if (FirstIfdOffset == 0)
        {
            return 0;
        }
        return CheckedMath.Add(BaseOffset, CheckedMath.ToInt64(FirstIfdOffset));
    }
}
