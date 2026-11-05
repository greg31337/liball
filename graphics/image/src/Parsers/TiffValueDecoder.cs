using System;

namespace image;

internal class TiffValueDecoder
{
    internal const int TypeByte = 1;
    internal const int TypeAscii = 2;
    internal const int TypeShort = 3;
    internal const int TypeLong = 4;
    internal const int TypeRational = 5;
    internal const int TypeSignedByte = 6;
    internal const int TypeUndefined = 7;
    internal const int TypeSignedShort = 8;
    internal const int TypeSignedLong = 9;
    internal const int TypeSignedRational = 10;
    internal const int TypeFloat = 11;
    internal const int TypeDouble = 12;
    internal const int TypeIfd = 13;
    internal const int TypeLong8 = 16;
    internal const int TypeSignedLong8 = 17;
    internal const int TypeIfd8 = 18;
    internal const int TagUserComment = 37510;
    internal const int TagXpTitle = 40091;
    internal const int TagXpSubject = 40095;

    internal static MetadataTag? DecodeEntry(ImageMetadataDocument document, BoundedStreamReader reader, MetadataDirectory directory, TiffIfdEntry entry, bool littleEndian, int valueOffsetSize)
    {
        if (document == null || reader == null || directory == null || entry == null)
        {
            return null;
        }

        int elementSize = ElementByteSize(entry.TypeId);
        if (elementSize == 0)
        {
            return AddUnsupportedTag(document, directory, entry, valueOffsetSize);
        }

        if (entry.Count > ulong.MaxValue / (ulong)elementSize)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.tag.value.size.overflow", "TIFF tag value byte size overflowed.", "TIFF", entry.EntryOffset, TiffHeaderParser.ClampOffset(entry.Count));
            return null;
        }

        ulong byteCount = entry.Count * (ulong)elementSize;

        if (byteCount > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tiff.tag.value.large", "TIFF tag value byte size exceeded the supported buffer size.", "TIFF", entry.EntryOffset, TiffHeaderParser.ClampOffset(byteCount));
            return null;
        }

        byte[] rawValue = ReadValueBytes(document, reader, entry, byteCount, valueOffsetSize);
        if ((ulong)ByteLength(rawValue) != byteCount)
        {
            return null;
        }

        MetadataValue value = DecodeValue(document, directory, entry, rawValue, littleEndian);
        return AddDecodedTag(directory, entry, rawValue, value);
    }

    internal static byte[] ReadValueBytes(ImageMetadataDocument document, BoundedStreamReader reader, TiffIfdEntry entry, ulong byteCount, int valueOffsetSize)
    {
        if (byteCount <= (ulong)valueOffsetSize)
        {
            return CopyBytes(entry.ValueField, 0, (int)byteCount);
        }

        if (!TiffIfdTraverser.CanReadRange(document, entry.ValueOrOffset, (int)byteCount, "tiff.tag.value.truncated"))
        {
            return new byte[0];
        }

        if (!reader.SeekAbsolute(TiffHeaderParser.ClampOffset(entry.ValueOrOffset)))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.tag.value.seek", "TIFF tag value offset could not be reached.", "TIFF", TiffHeaderParser.ClampOffset(entry.ValueOrOffset), TiffHeaderParser.ClampOffset(byteCount));
            return new byte[0];
        }

        byte[] bytes = reader.ReadBytes((int)byteCount);
        if ((ulong)bytes.Length != byteCount)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.tag.value.truncated", "TIFF tag value data was truncated.", "TIFF", TiffHeaderParser.ClampOffset(entry.ValueOrOffset), TiffHeaderParser.ClampOffset(byteCount));
            return new byte[0];
        }

        return bytes;
    }

    internal static MetadataValue DecodeValue(ImageMetadataDocument document, MetadataDirectory directory, TiffIfdEntry entry, byte[] rawValue, bool littleEndian)
    {
        if (IsExifUserComment(directory, entry))
        {
            return DecodeExifUserComment(document, rawValue, littleEndian);
        }

        if (IsWindowsXpText(entry))
        {
            return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedUtf16LittleEndian(rawValue, 0, rawValue.Length, document.Diagnostics, "TIFF"));
        }

        if (entry.TypeId == TypeAscii)
        {
            return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedAscii(rawValue, 0, ByteLength(rawValue), document.Diagnostics, "TIFF"));
        }

        if (entry.TypeId == TypeUndefined)
        {
            return MetadataValue.FromBytes(rawValue);
        }

        if (entry.Count == 1)
        {
            return DecodeScalar(entry.TypeId, rawValue, 0, littleEndian);
        }

        return DecodeArray(entry.TypeId, entry.Count, rawValue, littleEndian);
    }

    internal static bool IsWindowsXpText(TiffIfdEntry entry)
    {
        return entry != null && entry.TagId >= TagXpTitle && entry.TagId <= TagXpSubject;
    }

    internal static bool IsExifUserComment(MetadataDirectory directory, TiffIfdEntry entry)
    {
        return directory != null && entry != null && directory.Family == MetadataFamily.Exif && entry.TagId == TagUserComment;
    }

    internal static MetadataValue DecodeExifUserComment(ImageMetadataDocument document, byte[] rawValue, bool littleEndian)
    {
        if (ByteLength(rawValue) < 8)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "exif.usercomment.encoding.truncated", "EXIF user comment encoding marker was truncated.", "EXIF", 0, ByteLength(rawValue));
            return MetadataValue.FromBytes(rawValue);
        }

        if (IsAsciiUserComment(rawValue))
        {
            return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedAscii(rawValue, 8, rawValue.Length - 8, document.Diagnostics, "EXIF"));
        }

        if (IsUnicodeUserComment(rawValue))
        {
            if (littleEndian)
            {
                return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedUtf16LittleEndian(rawValue, 8, rawValue.Length - 8, document.Diagnostics, "EXIF"));
            }

            return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedUtf16BigEndian(rawValue, 8, rawValue.Length - 8, document.Diagnostics, "EXIF"));
        }

        if (IsUndefinedUserComment(rawValue))
        {
            return MetadataValue.FromString(MetadataStringReader.DecodeNullTerminatedUtf8(rawValue, 8, rawValue.Length - 8, document.Diagnostics, "EXIF"));
        }

        if (IsJisUserComment(rawValue))
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "exif.usercomment.encoding.unsupported", "EXIF user comment JIS encoding is not supported.", "EXIF");
            return MetadataValue.FromBytes(rawValue);
        }

        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "exif.usercomment.encoding.unknown", "EXIF user comment encoding marker was unknown.", "EXIF", 0, 8);
        return MetadataValue.FromBytes(rawValue);
    }

    internal static bool IsAsciiUserComment(byte[] rawValue)
    {
        return rawValue != null
            && rawValue.Length >= 8
            && rawValue[0] == 65
            && rawValue[1] == 83
            && rawValue[2] == 67
            && rawValue[3] == 73
            && rawValue[4] == 73
            && rawValue[5] == 0
            && rawValue[6] == 0
            && rawValue[7] == 0;
    }

    internal static bool IsUnicodeUserComment(byte[] rawValue)
    {
        return rawValue != null
            && rawValue.Length >= 8
            && rawValue[0] == 85
            && rawValue[1] == 78
            && rawValue[2] == 73
            && rawValue[3] == 67
            && rawValue[4] == 79
            && rawValue[5] == 68
            && rawValue[6] == 69
            && rawValue[7] == 0;
    }

    internal static bool IsJisUserComment(byte[] rawValue)
    {
        return rawValue != null
            && rawValue.Length >= 8
            && rawValue[0] == 74
            && rawValue[1] == 73
            && rawValue[2] == 83
            && rawValue[3] == 0
            && rawValue[4] == 0
            && rawValue[5] == 0
            && rawValue[6] == 0
            && rawValue[7] == 0;
    }

    internal static bool IsUndefinedUserComment(byte[] rawValue)
    {
        if (rawValue == null || rawValue.Length < 8)
        {
            return false;
        }

        int index = 0;
        while (index < 8)
        {
            if (rawValue[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static MetadataValue DecodeScalar(int typeId, byte[] rawValue, int offset, bool littleEndian)
    {
        switch (typeId)
        {
            case TypeByte:
                return MetadataValue.FromUnsignedInteger(rawValue[offset]);
            case TypeShort:
                return MetadataValue.FromUnsignedInteger(ReadUnsigned16(rawValue, offset, littleEndian));
            case TypeLong:
            case TypeIfd:
                return MetadataValue.FromUnsignedInteger(ReadUnsigned32(rawValue, offset, littleEndian));
            case TypeRational:
                return MetadataValue.FromRational(ReadUnsigned32(rawValue, offset, littleEndian), ReadUnsigned32(rawValue, offset + 4, littleEndian));
            case TypeSignedByte:
                return MetadataValue.FromInteger(IntegerBitPattern.ToSigned8(rawValue[offset]));
            case TypeSignedShort:
                return MetadataValue.FromInteger(ReadSigned16(rawValue, offset, littleEndian));
            case TypeSignedLong:
                return MetadataValue.FromInteger(ReadSigned32(rawValue, offset, littleEndian));
            case TypeSignedRational:
                return MetadataValue.FromSignedRational(ReadSigned32(rawValue, offset, littleEndian), ReadSigned32(rawValue, offset + 4, littleEndian));
            case TypeFloat:
                return MetadataValue.FromDouble(BitConverter.Int32BitsToSingle(ReadSigned32(rawValue, offset, littleEndian)));
            case TypeDouble:
                return MetadataValue.FromDouble(BitConverter.Int64BitsToDouble(ReadSigned64(rawValue, offset, littleEndian)));
            case TypeLong8:
            case TypeIfd8:
                return MetadataValue.FromUnsignedInteger(ReadUnsigned64(rawValue, offset, littleEndian));
            case TypeSignedLong8:
                return MetadataValue.FromInteger(ReadSigned64(rawValue, offset, littleEndian));
            default:
                return MetadataValue.FromBytes(rawValue);
        }
    }

    internal static MetadataValue DecodeArray(int typeId, ulong count, byte[] rawValue, bool littleEndian)
    {
        int elementCount = (int)count;
        int elementSize = ElementByteSize(typeId);

        if (typeId == TypeByte || typeId == TypeShort || typeId == TypeLong || typeId == TypeIfd || typeId == TypeLong8 || typeId == TypeIfd8)
        {
            ulong[] values = new ulong[elementCount];
            int index = 0;
            while (index < elementCount)
            {
                int offset = index * elementSize;
                if (typeId == TypeByte) values[index] = rawValue[offset];
                else if (typeId == TypeShort) values[index] = ReadUnsigned16(rawValue, offset, littleEndian);
                else if (typeId == TypeLong || typeId == TypeIfd) values[index] = ReadUnsigned32(rawValue, offset, littleEndian);
                else values[index] = ReadUnsigned64(rawValue, offset, littleEndian);
                index = index + 1;
            }

            return MetadataValue.FromUnsignedIntegerArray(values);
        }

        if (typeId == TypeSignedByte || typeId == TypeSignedShort || typeId == TypeSignedLong || typeId == TypeSignedLong8)
        {
            long[] values = new long[elementCount];
            int index = 0;
            while (index < elementCount)
            {
                int offset = index * elementSize;
                if (typeId == TypeSignedByte) values[index] = IntegerBitPattern.ToSigned8(rawValue[offset]);
                else if (typeId == TypeSignedShort) values[index] = ReadSigned16(rawValue, offset, littleEndian);
                else if (typeId == TypeSignedLong) values[index] = ReadSigned32(rawValue, offset, littleEndian);
                else values[index] = ReadSigned64(rawValue, offset, littleEndian);
                index = index + 1;
            }

            return MetadataValue.FromIntegerArray(values);
        }

        if (typeId == TypeRational || typeId == TypeSignedRational)
        {
            long[] numerators = new long[elementCount];
            long[] denominators = new long[elementCount];
            int index = 0;
            while (index < elementCount)
            {
                int offset = index * elementSize;
                if (typeId == TypeRational)
                {
                    numerators[index] = ReadUnsigned32(rawValue, offset, littleEndian);
                    denominators[index] = ReadUnsigned32(rawValue, offset + 4, littleEndian);
                }
                else
                {
                    numerators[index] = ReadSigned32(rawValue, offset, littleEndian);
                    denominators[index] = ReadSigned32(rawValue, offset + 4, littleEndian);
                }
                index = index + 1;
            }

            if (typeId == TypeRational)
            {
                return MetadataValue.FromRationalArray(numerators, denominators);
            }

            return MetadataValue.FromSignedRationalArray(numerators, denominators);
        }

        if (typeId == TypeFloat || typeId == TypeDouble)
        {
            double[] values = new double[elementCount];
            int index = 0;
            while (index < elementCount)
            {
                int offset = index * elementSize;
                if (typeId == TypeFloat) values[index] = BitConverter.Int32BitsToSingle(ReadSigned32(rawValue, offset, littleEndian));
                else values[index] = BitConverter.Int64BitsToDouble(ReadSigned64(rawValue, offset, littleEndian));
                index = index + 1;
            }

            return MetadataValue.FromDoubleArray(values);
        }

        return MetadataValue.FromBytes(rawValue);
    }

    internal static MetadataTag AddDecodedTag(MetadataDirectory directory, TiffIfdEntry entry, byte[] rawValue, MetadataValue value)
    {
        string group = directory.Group;
        string keyName = TiffTagNameTable.KeyNameFor(directory.Family, group, entry.TagId);
        string displayName = TiffTagNameTable.DisplayNameFor(directory.Family, group, entry.TagId);
        MetadataTag tag = MetadataTag.Create(PrefixForFamily(directory.Family) + "." + group + "." + keyName, displayName, directory.Family, value);
        tag.NumericId = entry.TagId;
        tag.RawValue = rawValue;
        tag.DisplayValue = ImageMetadataFormatterCore.FormatValue(value);
        tag.Offset = ValueOffset(entry, rawValue);
        tag.Length = ByteLength(rawValue);
        tag.Confidence = 100;
        return directory.AddTag(tag);
    }

    internal static MetadataTag AddUnsupportedTag(ImageMetadataDocument document, MetadataDirectory directory, TiffIfdEntry entry, int valueOffsetSize)
    {
        MetadataDiagnostic.AddUnsupported(document.Diagnostics, "tiff.tag.type.unsupported", "TIFF tag used an unsupported field type.", "TIFF");
        byte[] rawValue = CopyBytes(entry.ValueField, 0, valueOffsetSize);
        return AddDecodedTag(directory, entry, rawValue, MetadataValue.FromBytes(rawValue));
    }

    internal static long ValueOffset(TiffIfdEntry entry, byte[] rawValue)
    {
        if (entry == null)
        {
            return -1;
        }

        if (ByteLength(rawValue) <= 0)
        {
            return -1;
        }

        if ((ulong)ByteLength(rawValue) <= (ulong)ByteLength(entry.ValueField))
        {
            return entry.ValueFieldOffset;
        }

        return TiffHeaderParser.ClampOffset(entry.ValueOrOffset);
    }

    internal static int ElementByteSize(int typeId)
    {
        switch (typeId)
        {
            case TypeByte:
            case TypeAscii:
            case TypeSignedByte:
            case TypeUndefined:
                return 1;
            case TypeShort:
            case TypeSignedShort:
                return 2;
            case TypeLong:
            case TypeIfd:
            case TypeSignedLong:
            case TypeFloat:
                return 4;
            case TypeRational:
            case TypeSignedRational:
            case TypeDouble:
            case TypeLong8:
            case TypeSignedLong8:
            case TypeIfd8:
                return 8;
            default:
                return 0;
        }
    }

    internal static string PrefixForFamily(MetadataFamily family)
    {
        if (family == MetadataFamily.Exif)
        {
            return "Exif";
        }

        if (family == MetadataFamily.Gps)
        {
            return "Gps";
        }

        if (family == MetadataFamily.Raw)
        {
            return "Raw";
        }

        return "Tiff";
    }

    internal static ushort ReadUnsigned16(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (ushort)((uint)bytes[offset] | ((uint)bytes[offset + 1] << 8));
        }

        return (ushort)(((uint)bytes[offset] << 8) | (uint)bytes[offset + 1]);
    }

    internal static short ReadSigned16(byte[] bytes, int offset, bool littleEndian)
    {
        return IntegerBitPattern.ToSigned16(ReadUnsigned16(bytes, offset, littleEndian));
    }

    internal static uint ReadUnsigned32(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (uint)bytes[offset]
                | ((uint)bytes[offset + 1] << 8)
                | ((uint)bytes[offset + 2] << 16)
                | ((uint)bytes[offset + 3] << 24);
        }

        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | (uint)bytes[offset + 3];
    }

    internal static int ReadSigned32(byte[] bytes, int offset, bool littleEndian)
    {
        return IntegerBitPattern.ToSigned32(ReadUnsigned32(bytes, offset, littleEndian));
    }

    internal static ulong ReadUnsigned64(byte[] bytes, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return (ulong)bytes[offset]
                | ((ulong)bytes[offset + 1] << 8)
                | ((ulong)bytes[offset + 2] << 16)
                | ((ulong)bytes[offset + 3] << 24)
                | ((ulong)bytes[offset + 4] << 32)
                | ((ulong)bytes[offset + 5] << 40)
                | ((ulong)bytes[offset + 6] << 48)
                | ((ulong)bytes[offset + 7] << 56);
        }

        return ((ulong)bytes[offset] << 56)
            | ((ulong)bytes[offset + 1] << 48)
            | ((ulong)bytes[offset + 2] << 40)
            | ((ulong)bytes[offset + 3] << 32)
            | ((ulong)bytes[offset + 4] << 24)
            | ((ulong)bytes[offset + 5] << 16)
            | ((ulong)bytes[offset + 6] << 8)
            | (ulong)bytes[offset + 7];
    }

    internal static long ReadSigned64(byte[] bytes, int offset, bool littleEndian)
    {
        return IntegerBitPattern.ToSigned64(ReadUnsigned64(bytes, offset, littleEndian));
    }

    internal static byte[] CopyBytes(byte[] bytes, int offset, int count)
    {
        if (bytes == null || offset < 0 || count <= 0 || offset >= bytes.Length)
        {
            return new byte[0];
        }

        int actualCount = count;
        if (offset + actualCount > bytes.Length)
        {
            actualCount = bytes.Length - offset;
        }

        byte[] result = new byte[actualCount];
        int index = 0;
        while (index < actualCount)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int ByteLength(byte[] bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
    }
}
