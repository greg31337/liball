namespace icc;

internal static class ArrayTagCodec
{
    internal static void DecodeIntegerArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The integer-array tag reserved bytes shall be zero.");
            return;
        }

        int valueWidth = GetIntegerWidth(typeSignature);
        int encodedByteCount = payload.Length - 8;
        if (encodedByteCount % valueWidth != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The integer-array payload contains {encodedByteCount} value bytes, which is not divisible by its {valueWidth}-byte element width.");
            return;
        }

        int valueCount = encodedByteCount / valueWidth;
        long[] values = new long[valueCount];
        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        int index = 0;
        while (index < valueCount)
        {
            values[index] = ReadIntegerValue(reader, typeSignature);
            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.IntegerArray);
        data.IntegerValues = values;
        string projectionError = RegisteredArrayProjectionCodec.ProjectMultiplexDefaults(state, data);
        if (projectionError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, projectionError);
            return;
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {valueCount} integer-array values.");
    }

    internal static void DecodeFixedPointArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The fixed-point-array tag reserved bytes shall be zero.");
            return;
        }

        int encodedByteCount = payload.Length - 8;
        if (encodedByteCount % 4 != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The fixed-point-array payload contains {encodedByteCount} value bytes, which is not divisible by its 4-byte element width.");
            return;
        }

        int valueCount = encodedByteCount / 4;
        double[] values = new double[valueCount];
        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        int index = 0;
        while (index < valueCount)
        {
            if (typeSignature == IccTypeSignatures.S15Fixed16Array)
            {
                values[index] = IccNumberCodec.ReadS15Fixed16(reader);
            }
            else
            {
                values[index] = IccNumberCodec.ReadU16Fixed16(reader);
            }

            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.FixedPointArray);
        data.FixedPointValues = values;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {valueCount} fixed-point-array values.");
    }

    internal static int GetIntegerWidth(uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.UInt8Array:
                return 1;
            case IccTypeSignatures.UInt16Array:
                return 2;
            case IccTypeSignatures.UInt32Array:
                return 4;
            default:
                return 8;
        }
    }

    internal static long ReadIntegerValue(BigEndianReader reader, uint typeSignature)
    {
        switch (typeSignature)
        {
            case IccTypeSignatures.UInt8Array:
                return reader.ReadUInt8();
            case IccTypeSignatures.UInt16Array:
                return reader.ReadUInt16();
            case IccTypeSignatures.UInt32Array:
                return reader.ReadUInt32();
            default:
                return reader.ReadInt64();
        }
    }
}
