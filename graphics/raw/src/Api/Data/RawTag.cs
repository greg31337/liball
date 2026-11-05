using System;

namespace raw;

/// <summary>Contains decoded text together with its Exif character designation and exact bytes.</summary>
public class RawTextValue
{
    /// <summary>The decoded text.</summary>
    public string Text;
    /// <summary>The recognized character encoding.</summary>
    public RawTextEncoding Encoding;
    /// <summary>The exact encoded text bytes without an Exif character-code prefix.</summary>
    public byte[] Bytes;
    /// <summary>The exact eight-byte Exif character-code prefix, or an empty array.</summary>
    public byte[] Prefix;
    /// <summary>Whether the encoded text ended with a null character or byte.</summary>
    public bool NullTerminated;
    /// <summary>Whether the character designation and bytes were decoded without a known incompatibility.</summary>
    public bool Valid;

    /// <summary>Creates a decoded text result.</summary>
    /// <param name="text">The decoded text.</param>
    /// <param name="encoding">The recognized encoding.</param>
    /// <param name="bytes">The exact text bytes without a character-code prefix.</param>
    /// <param name="prefix">The exact character-code prefix, or <c>null</c>.</param>
    /// <param name="nullTerminated">Whether a terminator was present.</param>
    /// <param name="valid">Whether decoding was fully recognized.</param>
    public RawTextValue(string text, RawTextEncoding encoding, byte[] bytes, byte[]? prefix, bool nullTerminated, bool valid)
    {
        Text = text == null ? string.Empty : text;
        Encoding = encoding;
        Bytes = bytes == null ? System.Array.Empty<byte>() : Copy(bytes);
        Prefix = prefix == null ? System.Array.Empty<byte>() : Copy(prefix);
        NullTerminated = nullTerminated;
        Valid = valid;
    }

    internal static byte[] Copy(byte[]? source)
    {
        byte[] result = new byte[(source ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length];
        source.AsSpan().CopyTo(result);
        return result;
    }
}

/// <summary>Stores one exact TIFF or Exif value and provides type-checked conversions and encoders.</summary>
public class RawTagValue
{
    /// <summary>The TIFF or Exif field type.</summary>
    public RawTagType Type;
    /// <summary>The number of typed values, including a string terminator when required by the format.</summary>
    public ulong Count;
    /// <summary>The byte order used by numeric values.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>The exact encoded field bytes.</summary>
    public byte[] RawBytes;

    /// <summary>Creates an exact typed tag value.</summary>
    /// <param name="type">The field type.</param>
    /// <param name="count">The TIFF value count.</param>
    /// <param name="byteOrder">The numeric byte order.</param>
    /// <param name="rawBytes">The exact encoded bytes.</param>
    /// <exception cref="RawFormatException">The type, count, byte order, or byte length is invalid.</exception>
    public RawTagValue(RawTagType type, ulong count, RawByteOrder byteOrder, byte[] rawBytes)
    {
        RequireByteOrder(byteOrder);
        if (rawBytes == null)
        {
            throw Invalid("Tag value bytes are required.");
        }
        int elementSize = GetElementSize(type);
        if (count > ulong.MaxValue / (ulong)elementSize)
        {
            throw Invalid("The tag value count overflows its encoded byte length.");
        }
        ulong expected = count * (ulong)elementSize;
        if (expected != (ulong)rawBytes.Length)
        {
            throw Invalid("The tag value byte length does not match its type and count.");
        }
        Type = type;
        Count = count;
        ByteOrder = byteOrder;
        RawBytes = RawTextValue.Copy(rawBytes);
    }

    /// <summary>Returns an independent copy of the exact encoded bytes.</summary>
    /// <returns>The copied bytes.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }

    /// <summary>Returns unsigned byte values.</summary>
    /// <returns>The values.</returns>
    internal byte[] GetByteValues()
    {
        RequireType(RawTagType.Byte);
        return CopyBytes();
    }

    /// <summary>Returns signed byte values.</summary>
    /// <returns>The values.</returns>
    internal sbyte[] GetSignedByteValues()
    {
        RequireType(RawTagType.SignedByte);
        sbyte[] values = new sbyte[RawBytes.Length];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = BitUtility.ToInt8Bits(RawBytes[index]);
            index++;
        }
        return values;
    }

    /// <summary>Returns unsigned 16-bit values.</summary>
    /// <returns>The values.</returns>
    internal ushort[] GetUInt16Values()
    {
        RequireType(RawTagType.Short);
        ushort[] values = new ushort[RawBytes.Length / 2];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadUInt16(RawBytes.AsSpan(index * 2, 2), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns signed 16-bit values.</summary>
    /// <returns>The values.</returns>
    internal short[] GetInt16Values()
    {
        RequireType(RawTagType.SignedShort);
        short[] values = new short[RawBytes.Length / 2];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadInt16(RawBytes.AsSpan(index * 2, 2), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns unsigned 32-bit integer or classic IFD-offset values.</summary>
    /// <returns>The values.</returns>
    internal uint[] GetUInt32Values()
    {
        if (Type != RawTagType.Long && Type != RawTagType.Ifd)
        {
            throw TypeMismatch();
        }
        uint[] values = new uint[RawBytes.Length / 4];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadUInt32(RawBytes.AsSpan(index * 4, 4), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns signed 32-bit values.</summary>
    /// <returns>The values.</returns>
    internal int[] GetInt32Values()
    {
        RequireType(RawTagType.SignedLong);
        int[] values = new int[RawBytes.Length / 4];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadInt32(RawBytes.AsSpan(index * 4, 4), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns unsigned 64-bit integer or BigTIFF IFD-offset values.</summary>
    /// <returns>The values.</returns>
    internal ulong[] GetUInt64Values()
    {
        if (Type != RawTagType.Long8 && Type != RawTagType.Ifd8)
        {
            throw TypeMismatch();
        }
        ulong[] values = new ulong[RawBytes.Length / 8];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadUInt64(RawBytes.AsSpan(index * 8, 8), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns signed 64-bit values.</summary>
    /// <returns>The values.</returns>
    internal long[] GetInt64Values()
    {
        RequireType(RawTagType.SignedLong8);
        long[] values = new long[RawBytes.Length / 8];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadInt64(RawBytes.AsSpan(index * 8, 8), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns exact signed or unsigned rational values.</summary>
    /// <returns>The rational values.</returns>
    internal RawRational[] GetRationalValues()
    {
        if (Type != RawTagType.Rational && Type != RawTagType.SignedRational)
        {
            throw TypeMismatch();
        }
        RawRational[] values = new RawRational[RawBytes.Length / 8];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * 8;
            if (Type == RawTagType.Rational)
            {
                values[index] = new RawRational(EndianReader.ReadUInt32(RawBytes.AsSpan(offset, 4), ByteOrder), EndianReader.ReadUInt32(RawBytes.AsSpan(offset + 4, 4), ByteOrder));
            }
            else
            {
                values[index] = RawRational.CreateSigned(EndianReader.ReadInt32(RawBytes.AsSpan(offset, 4), ByteOrder), EndianReader.ReadInt32(RawBytes.AsSpan(offset + 4, 4), ByteOrder));
            }
            index++;
        }
        return values;
    }

    /// <summary>Returns single-precision values.</summary>
    /// <returns>The values.</returns>
    internal float[] GetSingleValues()
    {
        RequireType(RawTagType.Float);
        float[] values = new float[RawBytes.Length / 4];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadSingle(RawBytes.AsSpan(index * 4, 4), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Returns double-precision values.</summary>
    /// <returns>The values.</returns>
    internal double[] GetDoubleValues()
    {
        RequireType(RawTagType.Double);
        double[] values = new double[RawBytes.Length / 8];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = EndianReader.ReadDouble(RawBytes.AsSpan(index * 8, 8), ByteOrder);
            index++;
        }
        return values;
    }

    /// <summary>Decodes an ASCII, UTF-8, or Exif character-prefixed textual value.</summary>
    /// <returns>The decoded text and exact encoding details.</returns>
    /// <exception cref="RawFormatException">The value type is not textual.</exception>
    internal RawTextValue GetText()
    {
        if (Type == RawTagType.Ascii)
        {
            return ExifTextCodec.DecodeAscii(RawBytes);
        }
        if (Type == RawTagType.Utf8)
        {
            return ExifTextCodec.DecodeUtf8(RawBytes);
        }
        if (Type == RawTagType.Undefined)
        {
            return ExifTextCodec.DecodePrefixed(RawBytes, ByteOrder);
        }
        throw TypeMismatch();
    }

    /// <summary>Encodes unsigned byte values.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateByte(byte[] values)
    {
        return new RawTagValue(RawTagType.Byte, RequireArray(values), RawByteOrder.LittleEndian, values);
    }

    /// <summary>Encodes signed byte values.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateSignedByte(sbyte[] values)
    {
        if (values == null)
        {
            throw Invalid("Tag values are required.");
        }
        byte[] bytes = new byte[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            bytes[index] = BitUtility.ToUInt8Bits(values[index]);
            index++;
        }
        return new RawTagValue(RawTagType.SignedByte, (ulong)values.Length, RawByteOrder.LittleEndian, bytes);
    }

    /// <summary>Encodes unsigned 16-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateUInt16(ushort[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 2, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.Short, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes signed 16-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateInt16(short[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 2, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.SignedShort, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes unsigned 32-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateUInt32(uint[] values, RawByteOrder byteOrder)
    {
        return CreateUInt32Core(values, byteOrder, RawTagType.Long);
    }

    /// <summary>Encodes classic TIFF IFD offsets.</summary>
    /// <param name="values">The offsets.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateIfd(uint[] values, RawByteOrder byteOrder)
    {
        return CreateUInt32Core(values, byteOrder, RawTagType.Ifd);
    }

    /// <summary>Encodes signed 32-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateInt32(int[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 4, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.SignedLong, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes unsigned 64-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateUInt64(ulong[] values, RawByteOrder byteOrder)
    {
        return CreateUInt64Core(values, byteOrder, RawTagType.Long8);
    }

    /// <summary>Encodes BigTIFF IFD offsets.</summary>
    /// <param name="values">The offsets.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateIfd8(ulong[] values, RawByteOrder byteOrder)
    {
        return CreateUInt64Core(values, byteOrder, RawTagType.Ifd8);
    }

    /// <summary>Encodes signed 64-bit values.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateInt64(long[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 8, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteInt64(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.SignedLong8, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes exact rational values.</summary>
    /// <param name="values">Uniformly signed or unsigned rational values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateRational(RawRational[] values, RawByteOrder byteOrder)
    {
        bool signed = values.Length > 0 && values[0] != null && values[0].Signed;
        return CreateRationalCore(values, byteOrder, signed);
    }

    internal static RawTagValue CreateUnsignedRational(RawRational[] values, RawByteOrder byteOrder)
    {
        return CreateRationalCore(values, byteOrder, false);
    }

    internal static RawTagValue CreateSignedRational(RawRational[] values, RawByteOrder byteOrder)
    {
        return CreateRationalCore(values, byteOrder, true);
    }

    internal static RawTagValue CreateRationalCore(RawRational[] values, RawByteOrder byteOrder, bool signed)
    {
        byte[] bytes = Allocate(values, 8, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            RawRational value = values[index];
            if (value == null || value.Signed != signed)
            {
                throw Invalid("Rational values must be non-null and use uniform signedness.");
            }
            RawRational.RequireValid(value);
            int offset = index * 8;
            if (signed)
            {
                EndianWriter.WriteInt32(bytes.AsSpan(offset, 4), (int)value.Numerator, byteOrder);
                EndianWriter.WriteInt32(bytes.AsSpan(offset + 4, 4), (int)value.Denominator, byteOrder);
            }
            else
            {
                EndianWriter.WriteUInt32(bytes.AsSpan(offset, 4), (uint)value.Numerator, byteOrder);
                EndianWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), (uint)value.Denominator, byteOrder);
            }
            index++;
        }
        return new RawTagValue(signed ? RawTagType.SignedRational : RawTagType.Rational, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes single-precision values without numeric conversion.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateSingle(float[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 4, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteSingle(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.Float, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes double-precision values without numeric conversion.</summary>
    /// <param name="values">The values.</param>
    /// <param name="byteOrder">The destination byte order.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateDouble(double[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = Allocate(values, 8, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteDouble(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(RawTagType.Double, (ulong)values.Length, byteOrder, bytes);
    }

    /// <summary>Encodes a null-terminated Exif ASCII string.</summary>
    /// <param name="text">Seven-bit text without an embedded null.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateAscii(string text)
    {
        return ExifTextCodec.EncodeAscii(text);
    }

    /// <summary>Encodes a null-terminated Exif 3 UTF-8 string without a byte-order mark.</summary>
    /// <param name="text">Unicode text without an embedded null.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateUtf8(string text)
    {
        return ExifTextCodec.EncodeUtf8(text);
    }

    /// <summary>Copies an opaque UNDEFINED value.</summary>
    /// <param name="bytes">The exact bytes.</param>
    /// <returns>The typed value.</returns>
    internal static RawTagValue CreateUndefined(byte[] bytes)
    {
        return new RawTagValue(RawTagType.Undefined, RequireArray(bytes), RawByteOrder.LittleEndian, bytes);
    }

    /// <summary>Encodes an Exif UserComment, GPSProcessingMethod, or GPSAreaInformation value with an eight-byte character designation.</summary>
    /// <param name="text">Text without an embedded null.</param>
    /// <param name="encoding">ASCII, JIS, Unicode, or unknown designation.</param>
    /// <param name="byteOrder">The containing TIFF byte order used for legacy UTF-16 compatibility.</param>
    /// <returns>An UNDEFINED typed value containing the prefix and text.</returns>
    internal static RawTagValue CreatePrefixedText(string text, RawTextEncoding encoding, RawByteOrder byteOrder)
    {
        return ExifTextCodec.EncodePrefixed(text, encoding, byteOrder);
    }

    internal static RawTagValue CreateUInt32Core(uint[] values, RawByteOrder byteOrder, RawTagType type)
    {
        byte[] bytes = Allocate(values, 4, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(type, (ulong)values.Length, byteOrder, bytes);
    }

    internal static RawTagValue CreateUInt64Core(ulong[] values, RawByteOrder byteOrder, RawTagType type)
    {
        byte[] bytes = Allocate(values, 8, byteOrder);
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
            index++;
        }
        return new RawTagValue(type, (ulong)values.Length, byteOrder, bytes);
    }

    internal static byte[] Allocate(System.Array values, int elementSize, RawByteOrder byteOrder)
    {
        if (values == null)
        {
            throw Invalid("Tag values are required.");
        }
        RequireByteOrder(byteOrder);
        return new byte[values.Length * elementSize];
    }

    internal static ulong RequireArray(System.Array values)
    {
        if (values == null)
        {
            throw Invalid("Tag values are required.");
        }
        return (ulong)values.Length;
    }

    internal static int GetElementSize(RawTagType type)
    {
        switch (type)
        {
            case RawTagType.Byte:
            case RawTagType.Ascii:
            case RawTagType.SignedByte:
            case RawTagType.Undefined:
            case RawTagType.Utf8:
                return 1;
            case RawTagType.Short:
            case RawTagType.SignedShort:
                return 2;
            case RawTagType.Long:
            case RawTagType.SignedLong:
            case RawTagType.Float:
            case RawTagType.Ifd:
                return 4;
            case RawTagType.Rational:
            case RawTagType.SignedRational:
            case RawTagType.Double:
            case RawTagType.Long8:
            case RawTagType.SignedLong8:
            case RawTagType.Ifd8:
                return 8;
            default:
                throw Invalid("The tag field type is not defined.");
        }
    }

    internal void RequireType(RawTagType expected)
    {
        if (Type != expected)
        {
            throw TypeMismatch();
        }
    }

    internal static void RequireByteOrder(RawByteOrder byteOrder)
    {
        if (byteOrder != RawByteOrder.LittleEndian && byteOrder != RawByteOrder.BigEndian)
        {
            throw Invalid("The tag byte order is not defined.");
        }
    }

    internal RawFormatException TypeMismatch()
    {
        return Invalid("The requested conversion does not match the tag field type.");
    }

    internal static RawFormatException Invalid(string message)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, message, -1);
    }
}

/// <summary>Describes one named, typed TIFF, Exif, GPS, or interoperability tag.</summary>
public class RawTag
{
    /// <summary>The unsigned 16-bit tag identifier.</summary>
    public ushort Id;
    /// <summary>The Exif 3.1 field name, or a stable hexadecimal fallback.</summary>
    public string Name;
    /// <summary>The containing stable IFD path.</summary>
    public string IfdPath;
    /// <summary>The absolute TIFF entry offset.</summary>
    public long EntryOffset;
    /// <summary>Whether the tag has a standard name in its containing IFD kind.</summary>
    public bool Known;
    /// <summary>The exact typed value.</summary>
    public RawTagValue Value;

    /// <summary>Creates a public tag description.</summary>
    /// <param name="id">The tag identifier.</param>
    /// <param name="name">The known name or stable fallback.</param>
    /// <param name="ifdPath">The containing IFD path.</param>
    /// <param name="entryOffset">The absolute entry offset.</param>
    /// <param name="known">Whether the name is standardized.</param>
    /// <param name="value">The exact typed value.</param>
    public RawTag(ushort id, string name, string ifdPath, long entryOffset, bool known, RawTagValue value)
    {
        if (value == null)
        {
            throw RawTagValue.Invalid("A tag value is required.");
        }
        Id = id;
        Name = name == null ? string.Empty : name;
        IfdPath = ifdPath == null ? string.Empty : ifdPath;
        EntryOffset = entryOffset;
        Known = known;
        Value = value;
    }
}

/// <summary>Describes one public TIFF, Exif, GPS, or interoperability directory.</summary>
public class RawIfd
{
    /// <summary>The stable parser path.</summary>
    public string Path;
    /// <summary>The semantic directory kind.</summary>
    public RawIfdKind Kind;
    /// <summary>The absolute directory offset.</summary>
    public long Offset;
    /// <summary>The tags in encoded directory order.</summary>
    public RawTag[] Tags;

    /// <summary>Creates a public directory description.</summary>
    /// <param name="path">The stable path.</param>
    /// <param name="kind">The semantic kind.</param>
    /// <param name="offset">The absolute offset.</param>
    /// <param name="tags">The tags, or <c>null</c> for none.</param>
    public RawIfd(string path, RawIfdKind kind, long offset, RawTag[] tags)
    {
        Path = path == null ? string.Empty : path;
        Kind = kind;
        Offset = offset;
        Tags = tags == null ? System.Array.Empty<RawTag>() : tags;
    }

    /// <summary>Finds the first tag with an identifier.</summary>
    /// <param name="id">The tag identifier.</param>
    /// <returns>The matching tag, or <c>null</c>.</returns>
    internal RawTag? FindTag(ushort id)
    {
        int index = 0;
        while (index < Tags.Length)
        {
            if (Tags[index] != null && Tags[index].Id == id)
            {
                return Tags[index];
            }
            index++;
        }
        return null;
    }

    /// <summary>Finds the first tag with an ordinal field name.</summary>
    /// <param name="name">The case-sensitive Exif field name.</param>
    /// <returns>The matching tag, or <c>null</c>.</returns>
    internal RawTag? FindTagByName(string name)
    {
        if (name == null)
        {
            return null;
        }
        int index = 0;
        while (index < Tags.Length)
        {
            if (Tags[index] != null && Tags[index].Name == name)
            {
                return Tags[index];
            }
            index++;
        }
        return null;
    }
}
