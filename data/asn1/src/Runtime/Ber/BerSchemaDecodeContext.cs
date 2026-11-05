namespace asn1;

internal class BerSchemaDecodeContext
{
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;

    internal BerSchemaDecodeContext(
        AsnCodecOptions options,
        AsnResourceLimits limits)
    {
        this.Options = options;
        this.Limits = limits;
        this.Resources = new ResourceCounter(limits);
    }
}
