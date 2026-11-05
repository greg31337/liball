namespace icc;

internal class IccPositionNumber
{
    internal uint Offset;
    internal uint Size;

    internal IccPositionNumber(uint offset, uint size)
    {
        Offset = offset;
        Size = size;
    }
}

internal class IccResponse16Number
{
    internal ushort DeviceCode;
    internal ushort Reserved;
    internal double Measurement;

    internal IccResponse16Number(ushort deviceCode, ushort reserved, double measurement)
    {
        DeviceCode = deviceCode;
        Reserved = reserved;
        Measurement = measurement;
    }
}

internal enum IccValueEncoding
{
    Float32 = 0,
    Float16 = 1,
    UInt16 = 2,
    UInt8 = 3
}

internal enum IccSparseMatrixEncoding
{
    UInt8 = 1,
    UInt16 = 2,
    Float16 = 3,
    Float32 = 4
}

internal enum IccInterpolationHint
{
    None = 0,
    Trilinear = 1,
    Tetrahedral = 2
}

internal class IccSpectralRangeNumber
{
    internal float StartWavelength;
    internal float EndWavelength;
    internal ushort Steps;

    internal IccSpectralRangeNumber(float startWavelength, float endWavelength, ushort steps)
    {
        StartWavelength = startWavelength;
        EndWavelength = endWavelength;
        Steps = steps;
    }
}

internal class IccTintArrayNumber
{
    internal int TintCount;
    internal int SampleCount;
    internal IccValueEncoding Encoding;
    internal double[] Values;

    internal IccTintArrayNumber(int tintCount, int sampleCount, IccValueEncoding encoding, double[] values)
    {
        TintCount = tintCount;
        SampleCount = sampleCount;
        Encoding = encoding;
        Values = values;
    }
}

internal class IccSparseMatrixNumber
{
    internal int Rows;
    internal int Columns;
    internal IccSparseMatrixEncoding Encoding;
    internal ushort[] RowStartOffsets;
    internal ushort[] ColumnIndices;
    internal double[] Values;
    internal byte[] IndexPadding;
    internal byte[] DataPadding;

    internal IccSparseMatrixNumber(int rows, int columns, IccSparseMatrixEncoding encoding, ushort[] rowStartOffsets, ushort[] columnIndices, double[] values, byte[] indexPadding, byte[] dataPadding)
    {
        Rows = rows;
        Columns = columns;
        Encoding = encoding;
        RowStartOffsets = rowStartOffsets;
        ColumnIndices = columnIndices;
        Values = values;
        IndexPadding = indexPadding;
        DataPadding = dataPadding;
    }
}

internal static class IccNumberCodec
{
    internal const double S15Fixed16Scale = 65536.0;
    internal const double U16Fixed16Scale = 65536.0;
    internal const double U1Fixed15Scale = 32768.0;
    internal const double U8Fixed8Scale = 256.0;
    internal const double S15Fixed16Minimum = -32768.0;
    internal const double S15Fixed16Maximum = 32767.9999847412109375;
    internal const double U16Fixed16Maximum = 65535.9999847412109375;
    internal const double U1Fixed15Maximum = 1.999969482421875;
    internal const double U8Fixed8Maximum = 255.99609375;

    internal static float ReadFloat32(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(4);
        long offset = reader.GetAbsoluteOffset();
        uint bits = reader.ReadUInt32();
        ValidateFloat32BitsForRead(bits, offset);
        int signedBits = ConvertUInt32BitsToInt32(bits);
        return System.BitConverter.Int32BitsToSingle(signedBits);
    }

    internal static void WriteFloat32(BigEndianWriter writer, float value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        int signedBits = System.BitConverter.SingleToInt32Bits(value);
        uint bits = ConvertInt32BitsToUInt32(signedBits);
        ValidateFloat32BitsForWrite(bits, writer.GetAbsoluteOffset());
        writer.WriteUInt32(bits);
    }

    internal static double ReadS15Fixed16(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        return DecodeS15Fixed16(reader.ReadInt32());
    }

    internal static void WriteS15Fixed16(BigEndianWriter writer, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        int encoded = EncodeS15Fixed16(value);
        writer.WriteInt32(encoded);
    }

    internal static double ReadU16Fixed16(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        return DecodeU16Fixed16(reader.ReadUInt32());
    }

    internal static void WriteU16Fixed16(BigEndianWriter writer, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        uint encoded = EncodeU16Fixed16(value);
        writer.WriteUInt32(encoded);
    }

    internal static double ReadU1Fixed15(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        return DecodeU1Fixed15(reader.ReadUInt16());
    }

    internal static void WriteU1Fixed15(BigEndianWriter writer, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        ushort encoded = EncodeU1Fixed15(value);
        writer.WriteUInt16(encoded);
    }

    internal static double ReadU8Fixed8(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        return DecodeU8Fixed8(reader.ReadUInt16());
    }

    internal static void WriteU8Fixed8(BigEndianWriter writer, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        ushort encoded = EncodeU8Fixed8(value);
        writer.WriteUInt16(encoded);
    }

    internal static double DecodeS15Fixed16(int value)
    {
        return value / S15Fixed16Scale;
    }

    internal static int EncodeS15Fixed16(double value)
    {
        ValidateFiniteFixedPointValue(value, "s15Fixed16Number");
        if (value <= S15Fixed16Minimum)
        {
            return int.MinValue;
        }

        if (value >= S15Fixed16Maximum)
        {
            return int.MaxValue;
        }

        double rounded = System.Math.Round(value * S15Fixed16Scale, System.MidpointRounding.AwayFromZero);
        return (int)rounded;
    }

    internal static double DecodeU16Fixed16(uint value)
    {
        return value / U16Fixed16Scale;
    }

    internal static uint EncodeU16Fixed16(double value)
    {
        ValidateFiniteFixedPointValue(value, "u16Fixed16Number");
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= U16Fixed16Maximum)
        {
            return uint.MaxValue;
        }

        double rounded = System.Math.Round(value * U16Fixed16Scale, System.MidpointRounding.AwayFromZero);
        return (uint)rounded;
    }

    internal static double DecodeU1Fixed15(ushort value)
    {
        return value / U1Fixed15Scale;
    }

    internal static ushort EncodeU1Fixed15(double value)
    {
        ValidateFiniteFixedPointValue(value, "u1Fixed15Number");
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= U1Fixed15Maximum)
        {
            return ushort.MaxValue;
        }

        double rounded = System.Math.Round(value * U1Fixed15Scale, System.MidpointRounding.AwayFromZero);
        return (ushort)rounded;
    }

    internal static double DecodeU8Fixed8(ushort value)
    {
        return value / U8Fixed8Scale;
    }

    internal static ushort EncodeU8Fixed8(double value)
    {
        ValidateFiniteFixedPointValue(value, "u8Fixed8Number");
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= U8Fixed8Maximum)
        {
            return ushort.MaxValue;
        }

        double rounded = System.Math.Round(value * U8Fixed8Scale, System.MidpointRounding.AwayFromZero);
        return (ushort)rounded;
    }

    internal static IccXyzNumber ReadXyzNumber(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(12);
        double x = ReadS15Fixed16(reader);
        double y = ReadS15Fixed16(reader);
        double z = ReadS15Fixed16(reader);
        IccXyzNumber value = new IccXyzNumber();
        value.X = x;
        value.Y = y;
        value.Z = z;
        return value;
    }

    internal static void WriteXyzNumber(BigEndianWriter writer, IccXyzNumber value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        if (value == null)
        {
            throw new IccProfileWriteException("An XYZ number cannot be null.");
        }

        int x = EncodeS15Fixed16(value.X);
        int y = EncodeS15Fixed16(value.Y);
        int z = EncodeS15Fixed16(value.Z);
        writer.EnsureAvailable(12);
        writer.WriteInt32(x);
        writer.WriteInt32(y);
        writer.WriteInt32(z);
    }

    internal static IccDateTime ReadDateTimeNumber(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(12);
        IccDateTime value = new IccDateTime();
        value.Year = reader.ReadUInt16();
        value.Month = reader.ReadUInt16();
        value.Day = reader.ReadUInt16();
        value.Hour = reader.ReadUInt16();
        value.Minute = reader.ReadUInt16();
        value.Second = reader.ReadUInt16();
        return value;
    }

    internal static void WriteDateTimeNumber(BigEndianWriter writer, IccDateTime value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        if (value == null)
        {
            throw new IccProfileWriteException("An ICC date and time cannot be null.");
        }

        ushort year = ValidateUInt16ForWrite(value.Year, "year");
        ushort month = ValidateUInt16ForWrite(value.Month, "month");
        ushort day = ValidateUInt16ForWrite(value.Day, "day");
        ushort hour = ValidateUInt16ForWrite(value.Hour, "hour");
        ushort minute = ValidateUInt16ForWrite(value.Minute, "minute");
        ushort second = ValidateUInt16ForWrite(value.Second, "second");
        writer.EnsureAvailable(12);
        writer.WriteUInt16(year);
        writer.WriteUInt16(month);
        writer.WriteUInt16(day);
        writer.WriteUInt16(hour);
        writer.WriteUInt16(minute);
        writer.WriteUInt16(second);
    }

    internal static IccPositionNumber ReadPositionNumber(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(8);
        uint offset = reader.ReadUInt32();
        uint size = reader.ReadUInt32();
        return new IccPositionNumber(offset, size);
    }

    internal static void WritePositionNumber(BigEndianWriter writer, IccPositionNumber value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        if (value == null)
        {
            throw new IccProfileWriteException("A position number cannot be null.");
        }

        writer.EnsureAvailable(8);
        writer.WriteUInt32(value.Offset);
        writer.WriteUInt32(value.Size);
    }

    internal static IccResponse16Number ReadResponse16Number(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(8);
        ushort deviceCode = reader.ReadUInt16();
        ushort reserved = reader.ReadUInt16();
        double measurement = ReadS15Fixed16(reader);
        return new IccResponse16Number(deviceCode, reserved, measurement);
    }

    internal static void WriteResponse16Number(BigEndianWriter writer, IccResponse16Number value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        if (value == null)
        {
            throw new IccProfileWriteException("A response number cannot be null.");
        }

        int measurement = EncodeS15Fixed16(value.Measurement);
        writer.EnsureAvailable(8);
        writer.WriteUInt16(value.DeviceCode);
        writer.WriteUInt16(value.Reserved);
        writer.WriteInt32(measurement);
    }

    internal static float ReadFloat16(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(2);
        long offset = reader.GetAbsoluteOffset();
        ushort bits = reader.ReadUInt16();
        ValidateFloat16BitsForRead(bits, offset);
        System.Half value = System.BitConverter.UInt16BitsToHalf(bits);
        return (float)value;
    }

    internal static void WriteFloat16(BigEndianWriter writer, float value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        ushort bits = EncodeFloat16Bits(value, writer.GetAbsoluteOffset());
        writer.WriteUInt16(bits);
    }

    internal static double ReadFloat64(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(8);
        long offset = reader.GetAbsoluteOffset();
        ulong bits = reader.ReadUInt64();
        ValidateFloat64BitsForRead(bits, offset);
        long signedBits = ConvertUInt64BitsToInt64(bits);
        return System.BitConverter.Int64BitsToDouble(signedBits);
    }

    internal static void WriteFloat64(BigEndianWriter writer, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        long signedBits = System.BitConverter.DoubleToInt64Bits(value);
        ulong bits = ConvertInt64BitsToUInt64(signedBits);
        ValidateFloat64BitsForWrite(bits, writer.GetAbsoluteOffset());
        writer.WriteUInt64(bits);
    }

    internal static double DecodeAzimuthNumber(double value)
    {
        ValidateNormalizedNumberForRead(value, 0.0, 1.0, "azimuthNumber");
        return value * 360.0 - 180.0;
    }

    internal static double EncodeAzimuthNumber(double degrees)
    {
        ValidateNormalizedNumberForWrite(degrees, -180.0, 180.0, "azimuth angle");
        return (degrees + 180.0) / 360.0;
    }

    internal static double DecodeZenithNumber(double value)
    {
        ValidateNormalizedNumberForRead(value, 0.0, 1.0, "zenithNumber");
        return value * 90.0;
    }

    internal static double EncodeZenithNumber(double degrees)
    {
        ValidateNormalizedNumberForWrite(degrees, 0.0, 90.0, "zenith angle");
        return degrees / 90.0;
    }

    internal static double DecodeHorizontalNumber(double value)
    {
        ValidateNormalizedNumberForRead(value, -1.0, 1.0, "horizontalNumber");
        return value;
    }

    internal static double EncodeHorizontalNumber(double value)
    {
        ValidateNormalizedNumberForWrite(value, -1.0, 1.0, "horizontalNumber");
        return value;
    }

    internal static double DecodeVerticalNumber(double value)
    {
        ValidateNormalizedNumberForRead(value, -1.0, 1.0, "verticalNumber");
        return value;
    }

    internal static double EncodeVerticalNumber(double value)
    {
        ValidateNormalizedNumberForWrite(value, -1.0, 1.0, "verticalNumber");
        return value;
    }

    internal static IccSpectralRangeNumber ReadSpectralRange(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        reader.cursor.EnsureAvailable(6);
        float start = ReadFloat16(reader);
        float end = ReadFloat16(reader);
        ushort steps = reader.ReadUInt16();
        return new IccSpectralRangeNumber(start, end, steps);
    }

    internal static void WriteSpectralRange(BigEndianWriter writer, IccSpectralRangeNumber value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        if (value == null)
        {
            throw new IccProfileWriteException("A spectral range cannot be null.");
        }

        ushort start = EncodeFloat16Bits(value.StartWavelength, writer.GetAbsoluteOffset());
        ushort end = EncodeFloat16Bits(value.EndWavelength, writer.GetAbsoluteOffset());
        writer.EnsureAvailable(6);
        writer.WriteUInt16(start);
        writer.WriteUInt16(end);
        writer.WriteUInt16(value.Steps);
    }

    internal static IccValueEncoding DecodeValueEncoding(uint value)
    {
        switch (value)
        {
            case 0:
                return IccValueEncoding.Float32;
            case 1:
                return IccValueEncoding.Float16;
            case 2:
                return IccValueEncoding.UInt16;
            case 3:
                return IccValueEncoding.UInt8;
            default:
                throw new IccProfileFormatException($"Value-encoding selector {value} is not defined by ICC.2.");
        }
    }

    internal static uint EncodeValueEncoding(IccValueEncoding value)
    {
        switch (value)
        {
            case IccValueEncoding.Float32:
                return 0;
            case IccValueEncoding.Float16:
                return 1;
            case IccValueEncoding.UInt16:
                return 2;
            case IccValueEncoding.UInt8:
                return 3;
            default:
                throw new IccProfileWriteException("The value encoding is not defined by ICC.2.");
        }
    }

    internal static IccInterpolationHint DecodeInterpolationHint(uint value)
    {
        switch (value)
        {
            case 0:
                return IccInterpolationHint.None;
            case 1:
                return IccInterpolationHint.Trilinear;
            case 2:
                return IccInterpolationHint.Tetrahedral;
            default:
                throw new IccProfileFormatException($"Interpolation-hint selector {value} is not defined by ICC.2.");
        }
    }

    internal static uint EncodeInterpolationHint(IccInterpolationHint value)
    {
        switch (value)
        {
            case IccInterpolationHint.None:
                return 0;
            case IccInterpolationHint.Trilinear:
                return 1;
            case IccInterpolationHint.Tetrahedral:
                return 2;
            default:
                throw new IccProfileWriteException("The interpolation hint is not defined by ICC.2.");
        }
    }

    internal static IccSparseMatrixEncoding DecodeSparseMatrixEncoding(uint value)
    {
        switch (value)
        {
            case 1:
                return IccSparseMatrixEncoding.UInt8;
            case 2:
                return IccSparseMatrixEncoding.UInt16;
            case 3:
                return IccSparseMatrixEncoding.Float16;
            case 4:
                return IccSparseMatrixEncoding.Float32;
            default:
                throw new IccProfileFormatException($"Sparse-matrix encoding selector {value} is not defined by ICC.2.");
        }
    }

    internal static int GetValueEncodingSize(IccValueEncoding encoding)
    {
        switch (encoding)
        {
            case IccValueEncoding.Float32:
                return 4;
            case IccValueEncoding.Float16:
                return 2;
            case IccValueEncoding.UInt16:
                return 2;
            case IccValueEncoding.UInt8:
                return 1;
            default:
                throw new IccProfileFormatException("The value encoding is not defined by ICC.2.");
        }
    }

    internal static double ReadEncodedValue(BigEndianReader reader, IccValueEncoding encoding)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        switch (encoding)
        {
            case IccValueEncoding.Float32:
                return ReadFloat32(reader);
            case IccValueEncoding.Float16:
                return ReadFloat16(reader);
            case IccValueEncoding.UInt16:
                return reader.ReadUInt16() / 65535.0;
            case IccValueEncoding.UInt8:
                return reader.ReadUInt8() / 255.0;
            default:
                throw new IccProfileFormatException("The value encoding is not defined by ICC.2.");
        }
    }

    internal static void WriteEncodedValue(BigEndianWriter writer, IccValueEncoding encoding, double value)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        switch (encoding)
        {
            case IccValueEncoding.Float32:
                WriteFloat32(writer, ConvertDoubleToFloat32(value));
                break;
            case IccValueEncoding.Float16:
                WriteFloat16(writer, ConvertDoubleToFloat16Source(value));
                break;
            case IccValueEncoding.UInt16:
                writer.WriteUInt16(EncodeNormalizedUInt16(value));
                break;
            case IccValueEncoding.UInt8:
                writer.WriteUInt8(EncodeNormalizedUInt8(value));
                break;
            default:
                throw new IccProfileWriteException("The value encoding is not defined by ICC.2.");
        }
    }

    internal static IccTintArrayNumber ReadTintArray(BigEndianReader reader, int tintCount, int sampleCount, IccValueEncoding encoding, int maximumBytes)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        int valueCount = ValidateTintArrayDimensionsForRead(tintCount, sampleCount);
        int valueSize = GetValueEncodingSize(encoding);
        int byteCount = CheckedSize.MultiplyCount(valueCount, valueSize, maximumBytes);
        reader.cursor.EnsureAvailable(byteCount);
        double[] values = new double[valueCount];
        int index = 0;
        while (index < valueCount)
        {
            values[index] = ReadEncodedValue(reader, encoding);
            index = index + 1;
        }

        return new IccTintArrayNumber(tintCount, sampleCount, encoding, values);
    }

    internal static void WriteTintArray(BigEndianWriter writer, IccTintArrayNumber value, int maximumBytes)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        ValidateTintArrayForWrite(value);
        int valueSize = GetValueEncodingSize(value.Encoding);
        int byteCount = CheckedSize.MultiplyCount(value.Values.Length, valueSize, maximumBytes);
        int validationIndex = 0;
        while (validationIndex < value.Values.Length)
        {
            ValidateEncodedValueForWrite(value.Encoding, value.Values[validationIndex]);
            validationIndex = validationIndex + 1;
        }

        writer.EnsureAvailable(byteCount);
        int index = 0;
        while (index < value.Values.Length)
        {
            WriteEncodedValue(writer, value.Encoding, value.Values[index]);
            index = index + 1;
        }
    }

    internal static IccSparseMatrixNumber ReadSparseMatrix(BigEndianReader reader, IccSparseMatrixEncoding encoding, int maximumBytes)
    {
        if (reader == null)
        {
            throw new IccProfileFormatException("A number reader cannot be null.");
        }

        CheckedSize.AccumulateBytes(0, 4, maximumBytes);
        reader.cursor.EnsureAvailable(4);
        int rows = reader.ReadUInt16();
        int columns = reader.ReadUInt16();
        ValidateSparseDimensionsForRead(rows, columns);
        int rowByteCount = CheckedSize.MultiplyCount(rows, 2, maximumBytes);
        int rowSectionBytes = CheckedSize.AccumulateBytes(rowByteCount, 2, maximumBytes);
        reader.cursor.EnsureAvailable(rowSectionBytes);
        ushort[] rowStarts = new ushort[rows];
        int row = 0;
        while (row < rows)
        {
            rowStarts[row] = reader.ReadUInt16();
            row = row + 1;
        }

        int entryCount = reader.ReadUInt16();
        int elementSize = GetSparseMatrixElementSize(encoding);
        int encodedSize = GetSparseMatrixEncodedSize(rows, entryCount, elementSize, maximumBytes);
        int consumed = 6 + rowByteCount;
        reader.cursor.EnsureAvailable(encodedSize - consumed);
        ushort[] columnIndices = new ushort[entryCount];
        int entry = 0;
        while (entry < entryCount)
        {
            columnIndices[entry] = reader.ReadUInt16();
            entry = entry + 1;
        }

        int indexEnd = consumed + entryCount * 2;
        int dataOffset = CheckedSize.AlignSize(indexEnd, 4, maximumBytes);
        byte[] indexPadding = reader.ReadExactBytes(dataOffset - indexEnd);
        ValidateSparseStructureForRead(rows, columns, rowStarts, columnIndices);
        double[] values = new double[entryCount];
        entry = 0;
        while (entry < entryCount)
        {
            values[entry] = ReadSparseMatrixValue(reader, encoding);
            entry = entry + 1;
        }

        int dataEnd = dataOffset + entryCount * elementSize;
        byte[] dataPadding = reader.ReadExactBytes(encodedSize - dataEnd);
        return new IccSparseMatrixNumber(rows, columns, encoding, rowStarts, columnIndices, values, indexPadding, dataPadding);
    }

    internal static void WriteSparseMatrix(BigEndianWriter writer, IccSparseMatrixNumber value, int maximumBytes)
    {
        if (writer == null)
        {
            throw new IccProfileWriteException("A number writer cannot be null.");
        }

        int startPosition = writer.GetWritePosition();
        ValidateSparseMatrixForWrite(value);
        int elementSize = GetSparseMatrixElementSize(value.Encoding);
        int encodedSize = GetSparseMatrixEncodedSize(value.Rows, value.Values.Length, elementSize, maximumBytes);
        int entry = 0;
        while (entry < value.Values.Length)
        {
            ValidateSparseMatrixValueForWrite(value.Encoding, value.Values[entry]);
            entry = entry + 1;
        }

        writer.EnsureAvailable(encodedSize);
        writer.WriteUInt16((ushort)value.Rows);
        writer.WriteUInt16((ushort)value.Columns);
        int row = 0;
        while (row < value.RowStartOffsets.Length)
        {
            writer.WriteUInt16(value.RowStartOffsets[row]);
            row = row + 1;
        }

        writer.WriteUInt16((ushort)value.Values.Length);
        entry = 0;
        while (entry < value.ColumnIndices.Length)
        {
            writer.WriteUInt16(value.ColumnIndices[entry]);
            entry = entry + 1;
        }

        int written = writer.GetWritePosition() - startPosition;
        int dataOffset = CheckedSize.AlignSize(written, 4, encodedSize);
        writer.WriteZeroBytes(dataOffset - written);
        entry = 0;
        while (entry < value.Values.Length)
        {
            WriteSparseMatrixValue(writer, value.Encoding, value.Values[entry]);
            entry = entry + 1;
        }

        written = writer.GetWritePosition() - startPosition;
        writer.WriteZeroBytes(encodedSize - written);
    }

    internal static int GetSparseMatrixElementSize(IccSparseMatrixEncoding encoding)
    {
        switch (encoding)
        {
            case IccSparseMatrixEncoding.UInt8:
                return 1;
            case IccSparseMatrixEncoding.UInt16:
                return 2;
            case IccSparseMatrixEncoding.Float16:
                return 2;
            case IccSparseMatrixEncoding.Float32:
                return 4;
            default:
                throw new IccProfileFormatException("The sparse-matrix encoding is not defined by ICC.2.");
        }
    }

    internal static int GetSparseMatrixEncodedSize(int rows, int entryCount, int elementSize, int maximumBytes)
    {
        if (elementSize != 1 && elementSize != 2 && elementSize != 4)
        {
            throw new IccProfileFormatException("A sparse-matrix element must occupy one, two, or four bytes.");
        }

        int rowBytes = CheckedSize.MultiplyCount(rows, 2, maximumBytes);
        int indexBytes = CheckedSize.MultiplyCount(entryCount, 2, maximumBytes);
        int indexEnd = CheckedSize.AccumulateBytes(6, rowBytes, maximumBytes);
        indexEnd = CheckedSize.AccumulateBytes(indexEnd, indexBytes, maximumBytes);
        int dataOffset = CheckedSize.AlignSize(indexEnd, 4, maximumBytes);
        int dataBytes = CheckedSize.MultiplyCount(entryCount, elementSize, maximumBytes);
        int dataEnd = CheckedSize.AccumulateBytes(dataOffset, dataBytes, maximumBytes);
        return CheckedSize.AlignSize(dataEnd, 4, maximumBytes);
    }

    internal static double ReadSparseMatrixValue(BigEndianReader reader, IccSparseMatrixEncoding encoding)
    {
        switch (encoding)
        {
            case IccSparseMatrixEncoding.UInt8:
                return reader.ReadUInt8() / 255.0;
            case IccSparseMatrixEncoding.UInt16:
                return reader.ReadUInt16() / 65535.0;
            case IccSparseMatrixEncoding.Float16:
                return ReadFloat16(reader);
            case IccSparseMatrixEncoding.Float32:
                return ReadFloat32(reader);
            default:
                throw new IccProfileFormatException("The sparse-matrix encoding is not defined by ICC.2.");
        }
    }

    internal static void WriteSparseMatrixValue(BigEndianWriter writer, IccSparseMatrixEncoding encoding, double value)
    {
        switch (encoding)
        {
            case IccSparseMatrixEncoding.UInt8:
                writer.WriteUInt8(EncodeNormalizedUInt8(value));
                break;
            case IccSparseMatrixEncoding.UInt16:
                writer.WriteUInt16(EncodeNormalizedUInt16(value));
                break;
            case IccSparseMatrixEncoding.Float16:
                WriteFloat16(writer, ConvertDoubleToFloat16Source(value));
                break;
            case IccSparseMatrixEncoding.Float32:
                WriteFloat32(writer, ConvertDoubleToFloat32(value));
                break;
            default:
                throw new IccProfileWriteException("The sparse-matrix encoding is not defined by ICC.2.");
        }
    }

    internal static ushort EncodeFloat16Bits(float value, long offset)
    {
        System.Half converted = (System.Half)value;
        ushort bits = System.BitConverter.HalfToUInt16Bits(converted);
        ValidateFloat16BitsForWrite(bits, offset);
        return bits;
    }

    internal static float ConvertDoubleToFloat16Source(double value)
    {
        ValidateFiniteNumberForWrite(value, "float16Number");
        float converted = (float)value;
        if (float.IsInfinity(converted))
        {
            throw new IccProfileWriteException("The value is outside the finite float16Number encoding range.");
        }

        return converted;
    }

    internal static float ConvertDoubleToFloat32(double value)
    {
        ValidateFiniteNumberForWrite(value, "float32Number");
        float converted = (float)value;
        int signedBits = System.BitConverter.SingleToInt32Bits(converted);
        uint bits = ConvertInt32BitsToUInt32(signedBits);
        ValidateFloat32BitsForWrite(bits, 0);
        return converted;
    }

    internal static byte EncodeNormalizedUInt8(double value)
    {
        ValidateFiniteNumberForWrite(value, "normalized uInt8Number");
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= 1.0)
        {
            return byte.MaxValue;
        }

        double rounded = System.Math.Round(value * 255.0, System.MidpointRounding.AwayFromZero);
        return (byte)rounded;
    }

    internal static ushort EncodeNormalizedUInt16(double value)
    {
        ValidateFiniteNumberForWrite(value, "normalized uInt16Number");
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= 1.0)
        {
            return ushort.MaxValue;
        }

        double rounded = System.Math.Round(value * 65535.0, System.MidpointRounding.AwayFromZero);
        return (ushort)rounded;
    }

    internal static void ValidateEncodedValueForWrite(IccValueEncoding encoding, double value)
    {
        switch (encoding)
        {
            case IccValueEncoding.Float32:
                ConvertDoubleToFloat32(value);
                break;
            case IccValueEncoding.Float16:
                float halfSource = ConvertDoubleToFloat16Source(value);
                EncodeFloat16Bits(halfSource, 0);
                break;
            case IccValueEncoding.UInt16:
                EncodeNormalizedUInt16(value);
                break;
            case IccValueEncoding.UInt8:
                EncodeNormalizedUInt8(value);
                break;
            default:
                throw new IccProfileWriteException("The value encoding is not defined by ICC.2.");
        }
    }

    internal static int ValidateTintArrayDimensionsForRead(int tintCount, int sampleCount)
    {
        if (tintCount <= 0 || sampleCount <= 0)
        {
            throw new IccProfileFormatException("A tint array must contain at least one tint and one sample per tint.");
        }

        return CheckedSize.MultiplyCount(tintCount, sampleCount, System.Array.MaxLength);
    }

    internal static void ValidateTintArrayForWrite(IccTintArrayNumber value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("A tint array cannot be null.");
        }

        if (value.TintCount <= 0 || value.SampleCount <= 0)
        {
            throw new IccProfileWriteException("A tint array must contain at least one tint and one sample per tint.");
        }

        if (value.Values == null)
        {
            throw new IccProfileWriteException("Tint-array values cannot be null.");
        }

        if (value.TintCount > int.MaxValue / value.SampleCount)
        {
            throw new IccProfileWriteException("The tint-array dimensions overflow a signed managed size.");
        }

        int expectedCount = value.TintCount * value.SampleCount;
        if (value.Values.Length != expectedCount)
        {
            throw new IccProfileWriteException("The tint-array value count does not match its tint and sample dimensions.");
        }

        if (value.Encoding != IccValueEncoding.Float32 && value.Encoding != IccValueEncoding.Float16 && value.Encoding != IccValueEncoding.UInt16 && value.Encoding != IccValueEncoding.UInt8)
        {
            throw new IccProfileWriteException("The tint-array value encoding is not defined by ICC.2.");
        }
    }

    internal static void ValidateSparseDimensionsForRead(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
        {
            throw new IccProfileFormatException("A sparse matrix must contain at least one row and one column.");
        }
    }

    internal static void ValidateSparseStructureForRead(int rows, int columns, ushort[] rowStarts, ushort[] columnIndices)
    {
        if (rowStarts[0] != 0)
        {
            throw new IccProfileFormatException("The first sparse-matrix row must start at entry zero.");
        }

        int row = 0;
        while (row < rows)
        {
            int start = rowStarts[row];
            int end = columnIndices.Length;
            if (row + 1 < rows)
            {
                end = rowStarts[row + 1];
            }

            if (start > end || end > columnIndices.Length)
            {
                throw new IccProfileFormatException("Sparse-matrix row offsets must be monotonic and within the entry array.");
            }

            int entry = start;
            int previousColumn = -1;
            while (entry < end)
            {
                int column = columnIndices[entry];
                if (column >= columns)
                {
                    throw new IccProfileFormatException("A sparse-matrix column index is outside the matrix dimensions.");
                }

                if (column <= previousColumn)
                {
                    throw new IccProfileFormatException("Sparse-matrix column indices must increase within each row.");
                }

                previousColumn = column;
                entry = entry + 1;
            }

            row = row + 1;
        }
    }

    internal static void ValidateSparseMatrixForWrite(IccSparseMatrixNumber value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("A sparse matrix cannot be null.");
        }

        if (value.Rows <= 0 || value.Rows > ushort.MaxValue || value.Columns <= 0 || value.Columns > ushort.MaxValue)
        {
            throw new IccProfileWriteException("Sparse-matrix dimensions must each fit in a non-zero unsigned 16-bit integer.");
        }

        if (value.RowStartOffsets == null || value.ColumnIndices == null || value.Values == null)
        {
            throw new IccProfileWriteException("Sparse-matrix row offsets, column indices, and values cannot be null.");
        }

        if (value.RowStartOffsets.Length != value.Rows)
        {
            throw new IccProfileWriteException("The sparse-matrix row-offset count does not match its row count.");
        }

        if (value.ColumnIndices.Length != value.Values.Length || value.Values.Length > ushort.MaxValue)
        {
            throw new IccProfileWriteException("Sparse-matrix columns and values must have the same unsigned 16-bit entry count.");
        }

        if (value.Encoding != IccSparseMatrixEncoding.UInt8 && value.Encoding != IccSparseMatrixEncoding.UInt16 && value.Encoding != IccSparseMatrixEncoding.Float16 && value.Encoding != IccSparseMatrixEncoding.Float32)
        {
            throw new IccProfileWriteException("The sparse-matrix encoding is not defined by ICC.2.");
        }

        ValidateSparseStructureForWrite(value.Rows, value.Columns, value.RowStartOffsets, value.ColumnIndices);
    }

    internal static void ValidateSparseStructureForWrite(int rows, int columns, ushort[] rowStarts, ushort[] columnIndices)
    {
        if (rowStarts[0] != 0)
        {
            throw new IccProfileWriteException("The first sparse-matrix row must start at entry zero.");
        }

        int row = 0;
        while (row < rows)
        {
            int start = rowStarts[row];
            int end = columnIndices.Length;
            if (row + 1 < rows)
            {
                end = rowStarts[row + 1];
            }

            if (start > end || end > columnIndices.Length)
            {
                throw new IccProfileWriteException("Sparse-matrix row offsets must be monotonic and within the entry array.");
            }

            int entry = start;
            int previousColumn = -1;
            while (entry < end)
            {
                int column = columnIndices[entry];
                if (column >= columns)
                {
                    throw new IccProfileWriteException("A sparse-matrix column index is outside the matrix dimensions.");
                }

                if (column <= previousColumn)
                {
                    throw new IccProfileWriteException("Sparse-matrix column indices must increase within each row.");
                }

                previousColumn = column;
                entry = entry + 1;
            }

            row = row + 1;
        }
    }

    internal static void ValidateSparseMatrixValueForWrite(IccSparseMatrixEncoding encoding, double value)
    {
        switch (encoding)
        {
            case IccSparseMatrixEncoding.UInt8:
                EncodeNormalizedUInt8(value);
                break;
            case IccSparseMatrixEncoding.UInt16:
                EncodeNormalizedUInt16(value);
                break;
            case IccSparseMatrixEncoding.Float16:
                float halfSource = ConvertDoubleToFloat16Source(value);
                EncodeFloat16Bits(halfSource, 0);
                break;
            case IccSparseMatrixEncoding.Float32:
                ConvertDoubleToFloat32(value);
                break;
            default:
                throw new IccProfileWriteException("The sparse-matrix encoding is not defined by ICC.2.");
        }
    }

    internal static void ValidateFloat16BitsForRead(ushort bits, long offset)
    {
        if ((bits & 0x7c00) == 0x7c00)
        {
            throw new IccProfileFormatException($"The float16Number at byte offset {offset} is infinite or NaN, which ICC.2 forbids.");
        }
    }

    internal static void ValidateFloat16BitsForWrite(ushort bits, long offset)
    {
        if ((bits & 0x7c00) == 0x7c00)
        {
            throw new IccProfileWriteException($"The float16Number at byte offset {offset} cannot be written because it is infinite or NaN.");
        }
    }

    internal static void ValidateFloat64BitsForRead(ulong bits, long offset)
    {
        if ((bits & 0x7ff0000000000000) == 0x7ff0000000000000)
        {
            throw new IccProfileFormatException($"The float64Number at byte offset {offset} is infinite or NaN, which ICC.2 forbids.");
        }
    }

    internal static void ValidateFloat64BitsForWrite(ulong bits, long offset)
    {
        if ((bits & 0x7ff0000000000000) == 0x7ff0000000000000)
        {
            throw new IccProfileWriteException($"The float64Number at byte offset {offset} cannot be written because it is infinite or NaN.");
        }
    }

    internal static void ValidateNormalizedNumberForRead(double value, double minimum, double maximum, string typeName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
        {
            throw new IccProfileFormatException($"The ICC.2 {typeName} is outside its permitted encoding range.");
        }
    }

    internal static void ValidateNormalizedNumberForWrite(double value, double minimum, double maximum, string typeName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
        {
            throw new IccProfileWriteException($"The ICC.2 {typeName} is outside its permitted encoding range.");
        }
    }

    internal static void ValidateFiniteNumberForWrite(double value, string typeName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"A non-finite value cannot be encoded as an ICC.2 {typeName}.");
        }
    }

    internal static void ValidateFloat32BitsForRead(uint bits, long offset)
    {
        uint exponent = bits & 0x7f800000;
        uint fraction = bits & 0x007fffff;
        if (exponent == 0x7f800000)
        {
            throw new IccProfileFormatException($"The float32Number at byte offset {offset} is infinite or NaN, which ICC.1 forbids.");
        }

        if (exponent == 0 && fraction != 0)
        {
            throw new IccProfileFormatException($"The float32Number at byte offset {offset} is subnormal, which ICC.1 forbids.");
        }
    }

    internal static void ValidateFloat32BitsForWrite(uint bits, long offset)
    {
        uint exponent = bits & 0x7f800000;
        uint fraction = bits & 0x007fffff;
        if (exponent == 0x7f800000)
        {
            throw new IccProfileWriteException($"The float32Number at byte offset {offset} cannot be written because it is infinite or NaN.");
        }

        if (exponent == 0 && fraction != 0)
        {
            throw new IccProfileWriteException($"The float32Number at byte offset {offset} cannot be written because it is subnormal.");
        }
    }

    internal static void ValidateFiniteFixedPointValue(double value, string typeName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"A non-finite value cannot be encoded as an ICC {typeName}.");
        }
    }

    internal static ushort ValidateUInt16ForWrite(int value, string componentName)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw new IccProfileWriteException($"The ICC date-time {componentName} must fit in an unsigned 16-bit integer.");
        }

        return (ushort)value;
    }

    internal static int ConvertUInt32BitsToInt32(uint value)
    {
        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return -1 - (int)(uint.MaxValue - value);
    }

    internal static uint ConvertInt32BitsToUInt32(int value)
    {
        if (value >= 0)
        {
            return (uint)value;
        }

        return uint.MaxValue - (uint)(-1 - value);
    }

    internal static long ConvertUInt64BitsToInt64(ulong value)
    {
        if (value <= long.MaxValue)
        {
            return (long)value;
        }

        return -1 - (long)(ulong.MaxValue - value);
    }

    internal static ulong ConvertInt64BitsToUInt64(long value)
    {
        if (value >= 0)
        {
            return (ulong)value;
        }

        return ulong.MaxValue - (ulong)(-1 - value);
    }
}
