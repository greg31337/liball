namespace asn1;

internal class OerOpenTypeCodec
{
    internal AsnResourceLimits Limits;

    internal OerOpenTypeCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal OerContentResult EncodeOpenType(byte[] completeEncoding)
    {
        return new OerContentCodec(this.Limits).EncodeVariableContent(
            completeEncoding);
    }

    internal OerContentResult DecodeOpenType(
        byte[] data,
        int offset,
        int count,
        bool canonical)
    {
        return new OerContentCodec(this.Limits).DecodeVariableContent(
            data,
            offset,
            count,
            canonical);
    }
}
