namespace icc;

internal class LutWriteLayout
{
    internal int GridPointCount;
    internal int InputTableEntryCount;
    internal int OutputTableEntryCount;
    internal int ClutValueCount;
    internal int PayloadSize;

    internal LutWriteLayout()
    {
        GridPointCount = 0;
        InputTableEntryCount = 0;
        OutputTableEntryCount = 0;
        ClutValueCount = 0;
        PayloadSize = 0;
    }
}

internal static class LutTagCodec
{
    internal static void DecodeLut8(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 48)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A lut8Type payload requires at least 48 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload) || payload.Span[11] != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The lut8Type reserved bytes and padding byte shall be zero.");
            return;
        }

        int inputChannels = payload.Span[8];
        int outputChannels = payload.Span[9];
        int gridPointCount = payload.Span[10];
        string headerError = ValidateEncodedHeader(inputChannels, outputChannels, gridPointCount, "lut8Type");
        if (headerError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, headerError);
            return;
        }

        int clutValueCount = CalculateClutValueCount(inputChannels, outputChannels, gridPointCount);
        if (clutValueCount < 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The lut8Type CLUT dimensions exceed the supported managed size.");
            return;
        }

        long expectedSize = 48L + ((long)inputChannels * 256L) + clutValueCount + ((long)outputChannels * 256L);
        if (expectedSize != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The lut8Type dimensions require exactly {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        IccLut lut = CreateDecodedLut(payload, state.AbsolutePayloadOffset, inputChannels, outputChannels, gridPointCount, 256, 256, clutValueCount, false);
        lut.PcsEncoding = IccPcsEncodingKind.Normalized;
        CompleteDecode(state, payload, typeSignature, lut, "lut8Type");
    }

    internal static void DecodeLut16(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 52)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A lut16Type payload requires at least 52 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload) || payload.Span[11] != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The lut16Type reserved bytes and padding byte shall be zero.");
            return;
        }

        int inputChannels = payload.Span[8];
        int outputChannels = payload.Span[9];
        int gridPointCount = payload.Span[10];
        string headerError = ValidateEncodedHeader(inputChannels, outputChannels, gridPointCount, "lut16Type");
        if (headerError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, headerError);
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(48, 4), state.AbsolutePayloadOffset + 48);
        int inputTableEntryCount = countReader.ReadUInt16();
        int outputTableEntryCount = countReader.ReadUInt16();
        if (inputTableEntryCount < 2 || inputTableEntryCount > 4096 || outputTableEntryCount < 2 || outputTableEntryCount > 4096)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A lut16Type input and output table shall each contain between 2 and 4096 entries, but this payload declares {inputTableEntryCount} and {outputTableEntryCount}.");
            return;
        }

        int clutValueCount = CalculateClutValueCount(inputChannels, outputChannels, gridPointCount);
        if (clutValueCount < 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The lut16Type CLUT dimensions exceed the supported managed size.");
            return;
        }

        long expectedSize = 52L
            + ((long)inputChannels * inputTableEntryCount * 2L)
            + ((long)clutValueCount * 2L)
            + ((long)outputChannels * outputTableEntryCount * 2L);
        if (expectedSize != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The lut16Type dimensions and table counts require exactly {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        IccLut lut = CreateDecodedLut(payload, state.AbsolutePayloadOffset, inputChannels, outputChannels, gridPointCount, inputTableEntryCount, outputTableEntryCount, clutValueCount, true);
        lut.PcsEncoding = IccPcsEncodingKind.Version2;
        CompleteDecode(state, payload, typeSignature, lut, "lut16Type");
    }

    internal static byte[] SerializeLut8(IccLut lut)
    {
        LutWriteLayout layout = ValidateLutForWrite(lut, false);
        byte[] payload = new byte[layout.PayloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        WriteHeader(writer, IccTypeSignatures.Lut8, lut, layout.GridPointCount);
        WriteTables(writer, lut.InputTables, false);
        WriteValues(writer, lut.Clut.Values, false);
        WriteTables(writer, lut.OutputTables, false);
        return payload;
    }

    internal static byte[] SerializeLut16(IccLut lut)
    {
        LutWriteLayout layout = ValidateLutForWrite(lut, true);
        byte[] payload = new byte[layout.PayloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        WriteHeader(writer, IccTypeSignatures.Lut16, lut, layout.GridPointCount);
        writer.WriteUInt16((ushort)layout.InputTableEntryCount);
        writer.WriteUInt16((ushort)layout.OutputTableEntryCount);
        WriteTables(writer, lut.InputTables, true);
        WriteValues(writer, lut.Clut.Values, true);
        WriteTables(writer, lut.OutputTables, true);
        return payload;
    }

    internal static IccLut CreateDecodedLut(System.ReadOnlyMemory<byte> payload, long absoluteOffset, int inputChannels, int outputChannels, int gridPointCount, int inputTableEntryCount, int outputTableEntryCount, int clutValueCount, bool sixteenBit)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = new double[9];
        BigEndianReader matrixReader = new BigEndianReader(payload.Slice(12, 36), absoluteOffset + 12);
        int index = 0;
        while (index < 9)
        {
            lut.Matrix.Values[index] = IccNumberCodec.ReadS15Fixed16(matrixReader);
            index = index + 1;
        }

        int offset = sixteenBit ? 52 : 48;
        int valueWidth = sixteenBit ? 2 : 1;
        lut.InputTables = new double[inputChannels][];
        index = 0;
        while (index < inputChannels)
        {
            lut.InputTables[index] = DecodeTable(payload, offset, inputTableEntryCount, sixteenBit);
            offset = offset + (inputTableEntryCount * valueWidth);
            index = index + 1;
        }

        lut.Clut.InputChannels = inputChannels;
        lut.Clut.OutputChannels = outputChannels;
        lut.Clut.GridPoints = new int[inputChannels];
        index = 0;
        while (index < inputChannels)
        {
            lut.Clut.GridPoints[index] = gridPointCount;
            index = index + 1;
        }

        lut.Clut.Values = DecodeTable(payload, offset, clutValueCount, sixteenBit);
        offset = offset + (clutValueCount * valueWidth);
        lut.OutputTables = new double[outputChannels][];
        index = 0;
        while (index < outputChannels)
        {
            lut.OutputTables[index] = DecodeTable(payload, offset, outputTableEntryCount, sixteenBit);
            offset = offset + (outputTableEntryCount * valueWidth);
            index = index + 1;
        }

        return lut;
    }

    internal static double[] DecodeTable(System.ReadOnlyMemory<byte> payload, int offset, int count, bool sixteenBit)
    {
        double[] values = new double[count];
        if (sixteenBit)
        {
            BigEndianReader reader = new BigEndianReader(payload.Slice(offset, count * 2), offset);
            int index = 0;
            while (index < count)
            {
                values[index] = reader.ReadUInt16() / 65535.0;
                index = index + 1;
            }

            return values;
        }

        System.ReadOnlySpan<byte> bytes = payload.Span;
        int byteIndex = 0;
        while (byteIndex < count)
        {
            values[byteIndex] = bytes[offset + byteIndex] / 255.0;
            byteIndex = byteIndex + 1;
        }

        return values;
    }

    internal static LutWriteLayout ValidateLutForWrite(IccLut lut, bool sixteenBit)
    {
        string typeName = sixteenBit ? "lut16Type" : "lut8Type";
        if (lut == null || lut.Matrix == null || lut.Matrix.Values == null || lut.InputTables == null || lut.Clut == null || lut.OutputTables == null)
        {
            throw new IccProfileWriteException($"The {typeName} data and its processing elements cannot be null.");
        }

        if (lut.InputChannels < 1 || lut.InputChannels > IccConstants.MaximumIcc1ChannelCount || lut.OutputChannels < 1 || lut.OutputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"The {typeName} input and output channel counts shall each be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
        }

        if (lut.Matrix.Rows != 3 || lut.Matrix.Columns != 3 || lut.Matrix.Values.Length != 9)
        {
            throw new IccProfileWriteException($"The {typeName} matrix shall contain exactly three rows, three columns, and nine values.");
        }

        if (lut.InputTables.Length != lut.InputChannels || lut.OutputTables.Length != lut.OutputChannels)
        {
            throw new IccProfileWriteException($"The {typeName} shall contain one input table per input channel and one output table per output channel.");
        }

        if (lut.Clut.GridPoints == null || lut.Clut.Values == null || lut.Clut.InputChannels != lut.InputChannels || lut.Clut.OutputChannels != lut.OutputChannels || lut.Clut.GridPoints.Length != lut.InputChannels)
        {
            throw new IccProfileWriteException($"The {typeName} CLUT dimensions shall match the LUT input and output channel counts.");
        }

        int gridPointCount = lut.Clut.GridPoints[0];
        if (gridPointCount < 2 || gridPointCount > 255)
        {
            throw new IccProfileWriteException($"The {typeName} grid-point count shall be between 2 and 255.");
        }

        int index = 1;
        while (index < lut.Clut.GridPoints.Length)
        {
            if (lut.Clut.GridPoints[index] != gridPointCount)
            {
                throw new IccProfileWriteException($"The {typeName} shall use the same grid-point count in every input dimension.");
            }

            index = index + 1;
        }

        int inputTableEntryCount = lut.InputTables[0] == null ? 0 : lut.InputTables[0].Length;
        int outputTableEntryCount = lut.OutputTables[0] == null ? 0 : lut.OutputTables[0].Length;
        if (sixteenBit)
        {
            if (inputTableEntryCount < 2 || inputTableEntryCount > 4096 || outputTableEntryCount < 2 || outputTableEntryCount > 4096)
            {
                throw new IccProfileWriteException("A lut16Type input and output table shall each contain between 2 and 4096 entries.");
            }
        }
        else if (inputTableEntryCount != 256 || outputTableEntryCount != 256)
        {
            throw new IccProfileWriteException("Every lut8Type input and output table shall contain exactly 256 entries.");
        }

        ValidateTables(lut.InputTables, inputTableEntryCount, "input", typeName);
        ValidateTables(lut.OutputTables, outputTableEntryCount, "output", typeName);
        ValidateValues(lut.Matrix.Values, false);
        int clutValueCount = CalculateClutValueCount(lut.InputChannels, lut.OutputChannels, gridPointCount);
        if (clutValueCount < 0)
        {
            throw new IccProfileWriteException($"The {typeName} CLUT dimensions exceed the supported managed size.");
        }

        if (lut.Clut.Values.Length != clutValueCount)
        {
            throw new IccProfileWriteException($"The {typeName} CLUT requires exactly {clutValueCount} values, but it contains {lut.Clut.Values.Length}.");
        }

        ValidateValues(lut.Clut.Values, true);
        long valueWidth = sixteenBit ? 2L : 1L;
        long headerSize = sixteenBit ? 52L : 48L;
        long payloadSize = headerSize
            + ((long)lut.InputChannels * inputTableEntryCount * valueWidth)
            + ((long)clutValueCount * valueWidth)
            + ((long)lut.OutputChannels * outputTableEntryCount * valueWidth);
        if (payloadSize > int.MaxValue)
        {
            throw new IccProfileWriteException($"The {typeName} payload exceeds the supported managed size.");
        }

        LutWriteLayout layout = new LutWriteLayout();
        layout.GridPointCount = gridPointCount;
        layout.InputTableEntryCount = inputTableEntryCount;
        layout.OutputTableEntryCount = outputTableEntryCount;
        layout.ClutValueCount = clutValueCount;
        layout.PayloadSize = (int)payloadSize;
        return layout;
    }

    internal static void ValidateTables(double[][] tables, int expectedEntryCount, string tableKind, string typeName)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index] == null || tables[index].Length != expectedEntryCount)
            {
                throw new IccProfileWriteException($"Every {typeName} {tableKind} table shall contain the same required number of entries.");
            }

            ValidateValues(tables[index], true);
            index = index + 1;
        }
    }

    internal static void ValidateValues(double[] values, bool normalized)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (normalized)
            {
                if (System.Double.IsNaN(values[index]) || System.Double.IsInfinity(values[index]) || values[index] < 0.0 || values[index] > 1.0)
                {
                    throw new IccProfileWriteException("A normalized LUT table value shall be finite and between 0 and 1.");
                }

                IccNumberCodec.EncodeNormalizedUInt16(values[index]);
            }
            else
            {
                IccNumberCodec.EncodeS15Fixed16(values[index]);
            }

            index = index + 1;
        }
    }

    internal static void WriteHeader(BigEndianWriter writer, uint typeSignature, IccLut lut, int gridPointCount)
    {
        writer.WriteUInt32(typeSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt8((byte)lut.InputChannels);
        writer.WriteUInt8((byte)lut.OutputChannels);
        writer.WriteUInt8((byte)gridPointCount);
        writer.WriteUInt8(0);
        int index = 0;
        while (index < lut.Matrix.Values.Length)
        {
            IccNumberCodec.WriteS15Fixed16(writer, lut.Matrix.Values[index]);
            index = index + 1;
        }
    }

    internal static void WriteTables(BigEndianWriter writer, double[][] tables, bool sixteenBit)
    {
        int index = 0;
        while (index < tables.Length)
        {
            WriteValues(writer, tables[index], sixteenBit);
            index = index + 1;
        }
    }

    internal static void WriteValues(BigEndianWriter writer, double[] values, bool sixteenBit)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (sixteenBit)
            {
                writer.WriteUInt16(IccNumberCodec.EncodeNormalizedUInt16(values[index]));
            }
            else
            {
                writer.WriteUInt8(IccNumberCodec.EncodeNormalizedUInt8(values[index]));
            }

            index = index + 1;
        }
    }

    internal static int CalculateClutValueCount(int inputChannels, int outputChannels, int gridPointCount)
    {
        if (inputChannels < 1 || outputChannels < 1 || gridPointCount < 1)
        {
            return -1;
        }

        int cellCount = 1;
        int index = 0;
        while (index < inputChannels)
        {
            if (cellCount > int.MaxValue / gridPointCount)
            {
                return -1;
            }

            cellCount = cellCount * gridPointCount;
            index = index + 1;
        }

        if (cellCount > int.MaxValue / outputChannels)
        {
            return -1;
        }

        return cellCount * outputChannels;
    }

    internal static string ValidateEncodedHeader(int inputChannels, int outputChannels, int gridPointCount, string typeName)
    {
        if (inputChannels < 1 || inputChannels > IccConstants.MaximumIcc1ChannelCount || outputChannels < 1 || outputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            return $"The {typeName} input and output channel counts shall each be between 1 and {IccConstants.MaximumIcc1ChannelCount}.";
        }

        if (gridPointCount < 2)
        {
            return $"The {typeName} grid-point count shall be between 2 and 255.";
        }

        return string.Empty;
    }

    internal static void CompleteDecode(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, IccLut lut, string typeName)
    {
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Lut);
        data.Lut = lut;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {typeName} with {lut.InputChannels} input channels, {lut.OutputChannels} output channels, and {lut.Clut.GridPoints[0]} grid points per dimension.");
    }
}
