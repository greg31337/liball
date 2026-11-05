using System;

namespace raw;

internal class TiffEntry
{
    internal ushort Tag;
    internal TiffFieldType FieldType;
    internal ulong Count;
    internal byte[] RawBytes;
    internal byte[] ValueSlotBytes;
    internal bool Inline;
    internal ulong ValueOffset;
    internal RawByteOrder ByteOrder;
    internal long EntryOffset;

    internal TiffEntry(ushort tag, TiffFieldType fieldType, ulong count, byte[] rawBytes, byte[] valueSlotBytes, bool inline, ulong valueOffset, RawByteOrder byteOrder, long entryOffset)
    {
        Tag = tag;
        FieldType = fieldType;
        Count = count;
        RawBytes = rawBytes;
        ValueSlotBytes = valueSlotBytes;
        Inline = inline;
        ValueOffset = valueOffset;
        ByteOrder = byteOrder;
        EntryOffset = entryOffset;
    }

    internal static TiffEntry Parse(BoundedInput directoryInput, BoundedInput containerInput, TiffHeader header)
    {
        if (directoryInput == null || containerInput == null || header == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF entry parsing requires directory, container, and header inputs.", -1);
        }

        long entryOffset = directoryInput.GetAbsolutePosition();
        int entrySize = 12;
        int slotOffset = 8;
        int slotSize = 4;
        if (header.BigTiff)
        {
            entrySize = 20;
            slotOffset = 12;
            slotSize = 8;
        }

        byte[] entryBytes = new byte[entrySize];
        directoryInput.ReadExactly(entryBytes);
        ushort tag = EndianReader.ReadUInt16(entryBytes.AsSpan(0, 2), header.ByteOrder);
        ushort typeCode = EndianReader.ReadUInt16(entryBytes.AsSpan(2, 2), header.ByteOrder);
        TiffFieldType fieldType = TiffFieldTypes.FromCode(typeCode, CheckedMath.Add(entryOffset, 2));
        ulong count;
        if (header.BigTiff)
        {
            count = EndianReader.ReadUInt64(entryBytes.AsSpan(4, 8), header.ByteOrder);
        }
        else
        {
            count = EndianReader.ReadUInt32(entryBytes.AsSpan(4, 4), header.ByteOrder);
        }

        long signedCount = CheckedMath.ToInt64(count);
        long byteCount = CheckedMath.Multiply(signedCount, TiffFieldTypes.GetElementSize(fieldType));
        CheckedMath.RequirePositiveLimit((containerInput.Limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxTagValueBytes, "The configured TIFF tag-value limit must be greater than zero.");
        if (byteCount > containerInput.Limits.MaxTagValueBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF field value exceeds the configured tag-value limit.", entryOffset);
        }
        CheckedMath.AllocationSize(byteCount, 1, containerInput.Limits);
        int allocationSize = CheckedMath.ToInt32(byteCount);
        byte[] rawBytes = new byte[allocationSize];
        byte[] slotBytes = new byte[slotSize];
        entryBytes.AsSpan(slotOffset, slotSize).CopyTo(slotBytes);

        if (byteCount <= slotSize)
        {
            slotBytes.AsSpan(0, allocationSize).CopyTo(rawBytes);
            return new TiffEntry(tag, fieldType, count, rawBytes, slotBytes, true, 0, header.ByteOrder, entryOffset);
        }

        ulong valueOffset;
        if (header.BigTiff)
        {
            valueOffset = EndianReader.ReadUInt64(slotBytes, header.ByteOrder);
        }
        else
        {
            valueOffset = EndianReader.ReadUInt32(slotBytes, header.ByteOrder);
        }
        long absoluteValueOffset = CheckedMath.Add(header.BaseOffset, CheckedMath.ToInt64(valueOffset));
        if (absoluteValueOffset < containerInput.Start)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The TIFF field value starts before the container window.", absoluteValueOffset);
        }
        long containerEnd = CheckedMath.Add(containerInput.Start, containerInput.Length);
        if (absoluteValueOffset > containerEnd || byteCount > containerEnd - absoluteValueOffset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The TIFF field value extends beyond the container window.", absoluteValueOffset);
        }
        long relativeValueOffset = absoluteValueOffset - containerInput.Start;
        containerInput.ReadAt(relativeValueOffset, rawBytes);
        return new TiffEntry(tag, fieldType, count, rawBytes, slotBytes, false, valueOffset, header.ByteOrder, entryOffset);
    }

    internal byte[] GetByteValues()
    {
        RequireType(TiffFieldType.Byte);
        return CopyRawBytes();
    }

    internal byte GetByte()
    {
        RequireScalar();
        return GetByteValues()[0];
    }

    internal sbyte[] GetSignedByteValues()
    {
        RequireType(TiffFieldType.SignedByte);
        sbyte[] values = new sbyte[RawBytes.Length];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = BitUtility.ToInt8Bits(RawBytes[index]);
        }
        return values;
    }

    internal sbyte GetSignedByte()
    {
        RequireScalar();
        return GetSignedByteValues()[0];
    }

    internal ushort[] GetUInt16Values()
    {
        RequireType(TiffFieldType.Short);
        ushort[] values = new ushort[RawBytes.Length / 2];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadUInt16(RawBytes.AsSpan(index * 2, 2), ByteOrder);
        }
        return values;
    }

    internal ushort GetUInt16()
    {
        RequireScalar();
        return GetUInt16Values()[0];
    }

    internal short[] GetInt16Values()
    {
        RequireType(TiffFieldType.SignedShort);
        short[] values = new short[RawBytes.Length / 2];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadInt16(RawBytes.AsSpan(index * 2, 2), ByteOrder);
        }
        return values;
    }

    internal short GetInt16()
    {
        RequireScalar();
        return GetInt16Values()[0];
    }

    internal uint[] GetUInt32Values()
    {
        RequireUInt32Type();
        uint[] values = new uint[RawBytes.Length / 4];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadUInt32(RawBytes.AsSpan(index * 4, 4), ByteOrder);
        }
        return values;
    }

    internal uint GetUInt32()
    {
        RequireScalar();
        return GetUInt32Values()[0];
    }

    internal int[] GetInt32Values()
    {
        RequireType(TiffFieldType.SignedLong);
        int[] values = new int[RawBytes.Length / 4];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadInt32(RawBytes.AsSpan(index * 4, 4), ByteOrder);
        }
        return values;
    }

    internal int GetInt32()
    {
        RequireScalar();
        return GetInt32Values()[0];
    }

    internal ulong[] GetUInt64Values()
    {
        RequireUInt64Type();
        ulong[] values = new ulong[RawBytes.Length / 8];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadUInt64(RawBytes.AsSpan(index * 8, 8), ByteOrder);
        }
        return values;
    }

    internal ulong GetUInt64()
    {
        RequireScalar();
        return GetUInt64Values()[0];
    }

    internal long[] GetInt64Values()
    {
        RequireType(TiffFieldType.SignedLong8);
        long[] values = new long[RawBytes.Length / 8];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadInt64(RawBytes.AsSpan(index * 8, 8), ByteOrder);
        }
        return values;
    }

    internal long GetInt64()
    {
        RequireScalar();
        return GetInt64Values()[0];
    }

    internal RawRational[] GetRationalValues()
    {
        if (FieldType != TiffFieldType.Rational && FieldType != TiffFieldType.SignedRational)
        {
            throw TypeMismatch();
        }
        RawRational[] values = new RawRational[RawBytes.Length / 8];
        for (int index = 0; index < values.Length; index++)
        {
            int offset = index * 8;
            if (FieldType == TiffFieldType.Rational)
            {
                uint numerator = EndianReader.ReadUInt32(RawBytes.AsSpan(offset, 4), ByteOrder);
                uint denominator = EndianReader.ReadUInt32(RawBytes.AsSpan(offset + 4, 4), ByteOrder);
                values[index] = new RawRational(numerator, denominator);
            }
            else
            {
                int numerator = EndianReader.ReadInt32(RawBytes.AsSpan(offset, 4), ByteOrder);
                int denominator = EndianReader.ReadInt32(RawBytes.AsSpan(offset + 4, 4), ByteOrder);
                values[index] = RawRational.CreateSigned(numerator, denominator);
            }
        }
        return values;
    }

    internal RawRational GetRational()
    {
        RequireScalar();
        return GetRationalValues()[0];
    }

    internal float[] GetSingleValues()
    {
        RequireType(TiffFieldType.Float);
        float[] values = new float[RawBytes.Length / 4];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadSingle(RawBytes.AsSpan(index * 4, 4), ByteOrder);
        }
        return values;
    }

    internal float GetSingle()
    {
        RequireScalar();
        return GetSingleValues()[0];
    }

    internal double[] GetDoubleValues()
    {
        RequireType(TiffFieldType.Double);
        double[] values = new double[RawBytes.Length / 8];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = EndianReader.ReadDouble(RawBytes.AsSpan(index * 8, 8), ByteOrder);
        }
        return values;
    }

    internal double GetDouble()
    {
        RequireScalar();
        return GetDoubleValues()[0];
    }

    internal string GetAscii()
    {
        RequireType(TiffFieldType.Ascii);
        return System.Text.Encoding.ASCII.GetString(RawBytes);
    }

    internal string GetUtf8()
    {
        RequireType(TiffFieldType.Utf8);
        return new System.Text.UTF8Encoding(false, true).GetString(RawBytes);
    }

    internal byte[] GetUndefinedBytes()
    {
        RequireType(TiffFieldType.Undefined);
        return CopyRawBytes();
    }

    internal byte[] CopyRawBytes()
    {
        byte[] copy = new byte[RawBytes.Length];
        RawBytes.AsSpan().CopyTo(copy);
        return copy;
    }

    internal void RequireScalar()
    {
        if (Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A scalar TIFF conversion requires exactly one value.", EntryOffset);
        }
    }

    internal void RequireType(TiffFieldType expected)
    {
        if (FieldType != expected)
        {
            throw TypeMismatch();
        }
    }

    internal void RequireUInt32Type()
    {
        if (FieldType != TiffFieldType.Long && FieldType != TiffFieldType.Ifd)
        {
            throw TypeMismatch();
        }
    }

    internal void RequireUInt64Type()
    {
        if (FieldType != TiffFieldType.Long8 && FieldType != TiffFieldType.Ifd8)
        {
            throw TypeMismatch();
        }
    }

    internal RawFormatException TypeMismatch()
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The requested conversion does not match the TIFF field type.", EntryOffset);
    }
}
