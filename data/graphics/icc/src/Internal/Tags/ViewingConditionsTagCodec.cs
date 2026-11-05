namespace icc;

internal static class ViewingConditionsTagCodec
{
    internal static void DecodeViewingConditions(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 36)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A viewingConditionsType payload requires exactly 36 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The viewingConditionsType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        IccXyzNumber illuminant = IccNumberCodec.ReadXyzNumber(reader);
        IccXyzNumber surround = IccNumberCodec.ReadXyzNumber(reader);
        uint illuminantKind = reader.ReadUInt32();
        if (illuminantKind > 8)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The viewingConditionsType illuminant encoding {illuminantKind} is not defined.");
            return;
        }

        IccViewingConditions conditions = new IccViewingConditions();
        conditions.Illuminant = MeasurementTagCodec.ToXyzColor(illuminant);
        conditions.Surround = MeasurementTagCodec.ToXyzColor(surround);
        conditions.IlluminantKind = (IccStandardIlluminant)illuminantKind;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.ViewingConditions);
        data.ViewingConditions = conditions;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded viewingConditionsType parameters.");
    }

    internal static byte[] SerializeViewingConditions(IccViewingConditions conditions)
    {
        if (conditions == null || conditions.Illuminant == null || conditions.Surround == null)
        {
            throw new IccProfileWriteException("Viewing conditions and both XYZ colours cannot be null.");
        }

        int illuminantKind = (int)conditions.IlluminantKind;
        if (illuminantKind < 0 || illuminantKind > 8)
        {
            throw new IccProfileWriteException("Viewing-condition illuminant shall use a defined ICC encoding.");
        }

        MeasurementTagCodec.ValidateXyzColor(conditions.Illuminant, "viewing-condition illuminant");
        MeasurementTagCodec.ValidateXyzColor(conditions.Surround, "viewing-condition surround");

        byte[] payload = new byte[36];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ViewingConditions);
        writer.WriteZeroBytes(4);
        IccNumberCodec.WriteXyzNumber(writer, MeasurementTagCodec.ToXyzNumber(conditions.Illuminant));
        IccNumberCodec.WriteXyzNumber(writer, MeasurementTagCodec.ToXyzNumber(conditions.Surround));
        writer.WriteUInt32((uint)illuminantKind);
        return payload;
    }
}
