namespace asn1;

internal class BerSchemaEncodeContext
{
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;

    internal BerSchemaEncodeContext(
        AsnCodecOptions options,
        AsnResourceLimits limits)
    {
        this.Options = options;
        this.Limits = limits;
        this.Resources = new ResourceCounter(limits);
    }
}
