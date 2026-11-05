namespace icc;

internal static class Icc2ArrayTagCodec
{
    internal static void DecodeFloatingPointArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The floating-point-array tag reserved bytes shall be zero.");
            return;
        }

        int width = GetFloatingPointWidth(typeSignature);
        int encodedByteCount = payload.Length - 8;
        if (encodedByteCount % width != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The floating-point-array payload contains {encodedByteCount} value bytes, which is not divisible by its {width}-byte element width.");
            return;
        }

        System.ReadOnlyMemory<byte> valueBytes = payload.Slice(8);
        if (!ContainsOnlyFiniteFloatingPointValues(valueBytes, typeSignature))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "An ICC.2 floating-point array shall not contain infinity, NaN, or forbidden subnormal values.");
            return;
        }

        int valueCount = encodedByteCount / width;
        double[] values = new double[valueCount];
        BigEndianReader reader = new BigEndianReader(valueBytes, state.AbsolutePayloadOffset + 8);
        int index = 0;
        while (index < valueCount)
        {
            if (typeSignature == IccTypeSignatures.Float16Array)
            {
                values[index] = IccNumberCodec.ReadFloat16(reader);
            }
            else if (typeSignature == IccTypeSignatures.Float32Array)
            {
                values[index] = IccNumberCodec.ReadFloat32(reader);
            }
            else
            {
                values[index] = IccNumberCodec.ReadFloat64(reader);
            }

            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.FloatingPointArray);
        data.FloatingPointValues = values;
        string projectionError = RegisteredArrayProjectionCodec.ProjectMultiplexDefaults(state, data);
        if (projectionError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, projectionError);
            return;
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {valueCount} ICC.2 floating-point-array values.");
    }

    internal static void DecodeSparseMatrixArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 16)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A sparseMatrixArrayType payload requires a 16-byte header, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The sparseMatrixArrayType reserved bytes shall be zero.");
            return;
        }

        int equivalentOutputChannels = ReadUInt16At(payload, 8);
        int encodingValue = ReadUInt16At(payload, 10);
        uint encodedMatrixCount = ReadUInt32At(payload, 12);
        if (equivalentOutputChannels == 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "A sparseMatrixArrayType shall declare at least one equivalent output channel.");
            return;
        }

        if (encodingValue < 1 || encodingValue > 4)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Sparse-matrix encoding selector {encodingValue} is not defined by ICC.2.");
            return;
        }

        if (encodedMatrixCount > System.Array.MaxLength)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The sparse-matrix count exceeds the supported managed array size.");
            return;
        }

        int matrixCount = (int)encodedMatrixCount;
        if (matrixCount > (payload.Length - 16) / 8)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The sparse-matrix count exceeds the number of minimum-sized matrices that fit in the payload.");
            return;
        }

        int[] matrixSizes = new int[matrixCount];
        int offset = 16;
        int expectedRows = 0;
        int expectedColumns = 0;
        int matrixIndex = 0;
        while (matrixIndex < matrixCount)
        {
            int matrixSize = ValidateCompactSparseMatrix(payload, offset, encodingValue);
            if (matrixSize < 0)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Sparse matrix {matrixIndex} is truncated, malformed, or does not use zero compact padding.");
                return;
            }

            int rows = ReadUInt16At(payload, offset);
            int columns = ReadUInt16At(payload, offset + 2);
            if (matrixIndex == 0)
            {
                expectedRows = rows;
                expectedColumns = columns;
                long denseChannelCount = (long)rows * columns;
                if (denseChannelCount != equivalentOutputChannels)
                {
                    TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The sparse matrices represent {denseChannelCount} dense output channels, but the tag declares {equivalentOutputChannels}.");
                    return;
                }
            }
            else if (rows != expectedRows || columns != expectedColumns)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "All matrices in a sparseMatrixArrayType shall have identical row and column counts.");
                return;
            }

            matrixSizes[matrixIndex] = matrixSize;
            offset = offset + matrixSize;
            matrixIndex = matrixIndex + 1;
        }

        if (offset != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The sparseMatrixArrayType leaves {payload.Length - offset} unconsumed bytes after its declared matrix list.");
            return;
        }

        IccSparseMatrixEncoding encoding = (IccSparseMatrixEncoding)encodingValue;
        IccSparseMatrix[] matrices = new IccSparseMatrix[matrixCount];
        offset = 16;
        matrixIndex = 0;
        while (matrixIndex < matrixCount)
        {
            int matrixSize = matrixSizes[matrixIndex];
            BigEndianReader reader = new BigEndianReader(payload.Slice(offset, matrixSize), state.AbsolutePayloadOffset + offset);
            IccSparseMatrixNumber number = IccNumberCodec.ReadSparseMatrix(reader, encoding, matrixSize);
            matrices[matrixIndex] = ConvertSparseMatrix(number);
            offset = offset + matrixSize;
            matrixIndex = matrixIndex + 1;
        }

        IccSparseMatrixArray sparseArray = new IccSparseMatrixArray();
        sparseArray.EquivalentOutputChannels = equivalentOutputChannels;
        sparseArray.Encoding = encodingValue;
        sparseArray.Matrices = matrices;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.SparseMatrixArray);
        data.SparseMatrixArray = sparseArray;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {matrixCount} compact sparse matrices.");
    }

    internal static byte[] SerializeFloatingPointArray(uint typeSignature, double[] values)
    {
        return SerializeFloatingPointArrayForMode(typeSignature, values, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeFloatingPointArrayForMode(uint typeSignature, double[] values, IccWriteMode mode)
    {
        if (values == null)
        {
            throw new IccProfileWriteException("Floating-point-array values cannot be null.");
        }

        int width = GetFloatingPointWidthForWrite(typeSignature);
        long payloadSize = 8L + ((long)values.Length * width);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The floating-point-array payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(typeSignature);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < values.Length)
        {
            double value = DeterministicTagSerialization.NormalizeFloatingPoint(values[index], mode);
            if (typeSignature == IccTypeSignatures.Float16Array)
            {
                IccNumberCodec.WriteFloat16(writer, IccNumberCodec.ConvertDoubleToFloat16Source(value));
            }
            else if (typeSignature == IccTypeSignatures.Float32Array)
            {
                IccNumberCodec.WriteFloat32(writer, IccNumberCodec.ConvertDoubleToFloat32(value));
            }
            else
            {
                IccNumberCodec.WriteFloat64(writer, value);
            }

            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeSparseMatrixArray(IccSparseMatrixArray value)
    {
        ValidateSparseMatrixArrayForWrite(value);
        IccSparseMatrixEncoding encoding = (IccSparseMatrixEncoding)value.Encoding;
        IccSparseMatrixNumber[] numbers = new IccSparseMatrixNumber[value.Matrices.Length];
        long payloadSize = 16;
        int index = 0;
        while (index < value.Matrices.Length)
        {
            IccSparseMatrixNumber number = ConvertSparseMatrixForWrite(value.Matrices[index], encoding, index);
            IccNumberCodec.ValidateSparseMatrixForWrite(number);
            int elementSize = IccNumberCodec.GetSparseMatrixElementSize(encoding);
            int matrixSize = IccNumberCodec.GetSparseMatrixEncodedSize(number.Rows, number.Values.Length, elementSize, System.Array.MaxLength);
            payloadSize = payloadSize + matrixSize;
            if (payloadSize > System.Array.MaxLength)
            {
                throw new IccProfileWriteException("The sparseMatrixArrayType payload exceeds the supported managed array size.");
            }

            numbers[index] = number;
            index = index + 1;
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.SparseMatrixArray);
        writer.WriteUInt32(0);
        writer.WriteUInt16((ushort)value.EquivalentOutputChannels);
        writer.WriteUInt16((ushort)value.Encoding);
        writer.WriteUInt32((uint)value.Matrices.Length);
        index = 0;
        while (index < numbers.Length)
        {
            IccSparseMatrixNumber number = numbers[index];
            int elementSize = IccNumberCodec.GetSparseMatrixElementSize(encoding);
            int matrixSize = IccNumberCodec.GetSparseMatrixEncodedSize(number.Rows, number.Values.Length, elementSize, System.Array.MaxLength);
            IccNumberCodec.WriteSparseMatrix(writer, number, matrixSize);
            index = index + 1;
        }

        return payload;
    }

    internal static int ValidateCompactSparseMatrix(System.ReadOnlyMemory<byte> payload, int offset, int encodingValue)
    {
        if (offset < 0 || offset > payload.Length - 6)
        {
            return -1;
        }

        int rows = ReadUInt16At(payload, offset);
        int columns = ReadUInt16At(payload, offset + 2);
        if (rows == 0 || columns == 0)
        {
            return -1;
        }

        long entryCountOffset = (long)offset + 4L + ((long)rows * 2L);
        if (entryCountOffset > payload.Length - 2)
        {
            return -1;
        }

        int entryCount = ReadUInt16At(payload, (int)entryCountOffset);
        long indexEnd = entryCountOffset + 2L + ((long)entryCount * 2L);
        long dataOffset = (indexEnd + 3L) & -4L;
        int elementSize = encodingValue == 1 ? 1 : encodingValue == 4 ? 4 : 2;
        long dataEnd = dataOffset + ((long)entryCount * elementSize);
        long matrixEnd = (dataEnd + 3L) & -4L;
        if (matrixEnd > payload.Length || matrixEnd - offset > int.MaxValue)
        {
            return -1;
        }

        int rowIndex = 0;
        int previousRowStart = 0;
        while (rowIndex < rows)
        {
            int rowStart = ReadUInt16At(payload, offset + 4 + (rowIndex * 2));
            if ((rowIndex == 0 && rowStart != 0) || rowStart < previousRowStart || rowStart > entryCount)
            {
                return -1;
            }

            previousRowStart = rowStart;
            rowIndex = rowIndex + 1;
        }

        int row = 0;
        while (row < rows)
        {
            int start = ReadUInt16At(payload, offset + 4 + (row * 2));
            int end = row + 1 < rows ? ReadUInt16At(payload, offset + 4 + ((row + 1) * 2)) : entryCount;
            int entry = start;
            int previousColumn = -1;
            while (entry < end)
            {
                int column = ReadUInt16At(payload, (int)entryCountOffset + 2 + (entry * 2));
                if (column >= columns || column <= previousColumn)
                {
                    return -1;
                }

                previousColumn = column;
                entry = entry + 1;
            }

            row = row + 1;
        }

        long paddingOffset = indexEnd;
        while (paddingOffset < dataOffset)
        {
            if (payload.Span[(int)paddingOffset] != 0)
            {
                return -1;
            }

            paddingOffset = paddingOffset + 1;
        }

        paddingOffset = dataEnd;
        while (paddingOffset < matrixEnd)
        {
            if (payload.Span[(int)paddingOffset] != 0)
            {
                return -1;
            }

            paddingOffset = paddingOffset + 1;
        }

        if (!SparseValuesAreFinite(payload, (int)dataOffset, entryCount, encodingValue))
        {
            return -1;
        }

        return (int)(matrixEnd - offset);
    }

    internal static bool SparseValuesAreFinite(System.ReadOnlyMemory<byte> payload, int offset, int count, int encodingValue)
    {
        if (encodingValue < 3)
        {
            return true;
        }

        int index = 0;
        while (index < count)
        {
            if (encodingValue == 3)
            {
                int bits = ReadUInt16At(payload, offset + (index * 2));
                if ((bits & 0x7c00) == 0x7c00)
                {
                    return false;
                }
            }
            else
            {
                uint bits = ReadUInt32At(payload, offset + (index * 4));
                uint exponent = bits & 0x7f800000;
                uint fraction = bits & 0x007fffff;
                if (exponent == 0x7f800000 || (exponent == 0 && fraction != 0))
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsOnlyFiniteFloatingPointValues(System.ReadOnlyMemory<byte> bytes, uint typeSignature)
    {
        int width = GetFloatingPointWidth(typeSignature);
        int offset = 0;
        while (offset < bytes.Length)
        {
            if (width == 2)
            {
                int bits = ReadUInt16At(bytes, offset);
                if ((bits & 0x7c00) == 0x7c00)
                {
                    return false;
                }
            }
            else if (width == 4)
            {
                uint bits = ReadUInt32At(bytes, offset);
                uint exponent = bits & 0x7f800000;
                uint fraction = bits & 0x007fffff;
                if (exponent == 0x7f800000 || (exponent == 0 && fraction != 0))
                {
                    return false;
                }
            }
            else
            {
                ulong bits = ReadUInt64At(bytes, offset);
                if ((bits & 0x7ff0000000000000) == 0x7ff0000000000000)
                {
                    return false;
                }
            }

            offset = offset + width;
        }

        return true;
    }

    internal static void ValidateSparseMatrixArrayForWrite(IccSparseMatrixArray value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("A sparse-matrix array cannot be null.");
        }

        if (value.Matrices == null)
        {
            throw new IccProfileWriteException("Sparse-matrix array matrices cannot be null.");
        }

        if (value.EquivalentOutputChannels <= 0 || value.EquivalentOutputChannels > ushort.MaxValue)
        {
            throw new IccProfileWriteException("The equivalent output-channel count shall fit in a non-zero unsigned 16-bit integer.");
        }

        if (value.Encoding < 1 || value.Encoding > 4)
        {
            throw new IccProfileWriteException("The sparse-matrix encoding selector is not defined by ICC.2.");
        }

        int expectedRows = 0;
        int expectedColumns = 0;
        int index = 0;
        while (index < value.Matrices.Length)
        {
            IccSparseMatrix matrix = value.Matrices[index];
            if (matrix == null)
            {
                throw new IccProfileWriteException($"Sparse matrix {index} cannot be null.");
            }

            if (matrix.Encoding != value.Encoding)
            {
                throw new IccProfileWriteException($"Sparse matrix {index} does not use the array encoding selector.");
            }

            if (index == 0)
            {
                expectedRows = matrix.Rows;
                expectedColumns = matrix.Columns;
                long denseChannelCount = (long)expectedRows * expectedColumns;
                if (denseChannelCount != value.EquivalentOutputChannels)
                {
                    throw new IccProfileWriteException("The equivalent output-channel count shall equal the dense row-by-column matrix size.");
                }
            }
            else if (matrix.Rows != expectedRows || matrix.Columns != expectedColumns)
            {
                throw new IccProfileWriteException("All matrices in a sparse-matrix array shall have identical dimensions.");
            }

            index = index + 1;
        }
    }

    internal static IccSparseMatrix ConvertSparseMatrix(IccSparseMatrixNumber number)
    {
        IccSparseMatrix matrix = new IccSparseMatrix();
        matrix.Rows = number.Rows;
        matrix.Columns = number.Columns;
        matrix.Encoding = (int)number.Encoding;
        matrix.RowStartOffsets = ConvertUShortArray(number.RowStartOffsets);
        matrix.ColumnIndices = ConvertUShortArray(number.ColumnIndices);
        matrix.Values = number.Values;
        matrix.IndexPadding = number.IndexPadding;
        matrix.DataPadding = number.DataPadding;
        return matrix;
    }

    internal static IccSparseMatrixNumber ConvertSparseMatrixForWrite(IccSparseMatrix matrix, IccSparseMatrixEncoding encoding, int index)
    {
        if (matrix.RowStartOffsets == null || matrix.ColumnIndices == null || matrix.Values == null)
        {
            throw new IccProfileWriteException($"Sparse matrix {index} arrays cannot be null.");
        }

        ushort[] rowOffsets = ConvertIntArrayForWrite(matrix.RowStartOffsets, $"sparse matrix {index} row offset");
        ushort[] columnIndices = ConvertIntArrayForWrite(matrix.ColumnIndices, $"sparse matrix {index} column index");
        return new IccSparseMatrixNumber(matrix.Rows, matrix.Columns, encoding, rowOffsets, columnIndices, matrix.Values, new byte[0], new byte[0]);
    }

    internal static int[] ConvertUShortArray(ushort[] source)
    {
        int[] values = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            values[index] = source[index];
            index = index + 1;
        }

        return values;
    }

    internal static ushort[] ConvertIntArrayForWrite(int[] source, string description)
    {
        ushort[] values = new ushort[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] < 0 || source[index] > ushort.MaxValue)
            {
                throw new IccProfileWriteException($"The {description} at index {index} does not fit in an unsigned 16-bit integer.");
            }

            values[index] = (ushort)source[index];
            index = index + 1;
        }

        return values;
    }

    internal static int GetFloatingPointWidth(uint typeSignature)
    {
        if (typeSignature == IccTypeSignatures.Float16Array)
        {
            return 2;
        }

        if (typeSignature == IccTypeSignatures.Float32Array)
        {
            return 4;
        }

        return 8;
    }

    internal static int GetFloatingPointWidthForWrite(uint typeSignature)
    {
        if (typeSignature != IccTypeSignatures.Float16Array && typeSignature != IccTypeSignatures.Float32Array && typeSignature != IccTypeSignatures.Float64Array)
        {
            throw new IccProfileWriteException($"Type signature 0x{typeSignature:X8} is not an ICC.2 floating-point-array type.");
        }

        return GetFloatingPointWidth(typeSignature);
    }

    internal static int ReadUInt16At(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        return (bytes.Span[offset] << 8) | bytes.Span[offset + 1];
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        return ((uint)bytes.Span[offset] << 24)
            | ((uint)bytes.Span[offset + 1] << 16)
            | ((uint)bytes.Span[offset + 2] << 8)
            | bytes.Span[offset + 3];
    }

    internal static ulong ReadUInt64At(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        ulong high = ReadUInt32At(bytes, offset);
        ulong low = ReadUInt32At(bytes, offset + 4);
        return (high << 32) | low;
    }
}
