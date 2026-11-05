namespace icc;

internal static class XyzTagCodec
{
    internal static void DecodeXyzArray(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The XYZ tag reserved bytes shall be zero.");
            return;
        }

        int encodedByteCount = payload.Length - 8;
        if (encodedByteCount % 12 != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The XYZ payload contains {encodedByteCount} value bytes, which is not divisible by the 12-byte XYZNumber width.");
            return;
        }

        int valueCount = encodedByteCount / 12;
        IccXyzNumber[] values = new IccXyzNumber[valueCount];
        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        int index = 0;
        while (index < valueCount)
        {
            values[index] = IccNumberCodec.ReadXyzNumber(reader);
            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.XyzArray);
        data.XyzValues = values;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {valueCount} XYZ values.");
    }
}
