namespace icc;

internal static class SignatureTagCodec
{
    internal static void DecodeSignature(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A signature tag shall contain exactly 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The signature tag reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Signature);
        data.Signature = reader.ReadUInt32();
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded a signature value.");
    }
}
