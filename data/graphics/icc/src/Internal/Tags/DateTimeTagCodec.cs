namespace icc;

internal static class DateTimeTagCodec
{
    internal static void DecodeDateTime(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 20)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A date/time tag shall contain exactly 20 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The date/time tag reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        IccDateTime value = IccNumberCodec.ReadDateTimeNumber(reader);
        if (!IsValidDateTime(value))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The date/time tag contains a component outside the ICC dateTimeNumber ranges.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.DateTime);
        data.DateTime = value;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded an ICC date and time in UTC.");
    }

    internal static bool IsValidDateTime(IccDateTime value)
    {
        return value.Year != 0 && value.Month >= 1 && value.Month <= 12 && value.Day >= 1 && value.Day <= 31 && value.Hour >= 0 && value.Hour <= 23 && value.Minute >= 0 && value.Minute <= 59 && value.Second >= 0 && value.Second <= 59;
    }
}
