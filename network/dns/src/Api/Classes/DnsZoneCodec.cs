namespace dns;

/// <summary>Parses, validates, and formats DNS zone master files.</summary>
public class DnsZoneCodec : IDnsZoneCodec
{
    /// <summary>Creates a DNS zone codec.</summary>
    public DnsZoneCodec()
    {
    }

    /// <summary>Parses one zone master-file source.</summary>
    /// <param name="source">The complete master-file text.</param>
    /// <param name="options">The origin, defaults, and resource limits.</param>
    /// <returns>The loaded zone or source diagnostics.</returns>
    public DnsZoneLoadResult ParseZone(
        string source,
        DnsZoneLoadOptions options)
    {
        return DnsMasterFileParser.ParseZone(source, null, options);
    }

    /// <summary>Formats one zone as master-file text.</summary>
    /// <param name="zone">The zone to format.</param>
    /// <param name="options">The text formatting policy.</param>
    /// <returns>The formatted zone text or diagnostics.</returns>
    public DnsTextResult FormatZone(
        DnsZone zone,
        DnsTextOptions options)
    {
        return DnsZoneText.FormatZone(zone, options);
    }

    /// <summary>Validates the structure of one DNS zone.</summary>
    /// <param name="zone">The zone to validate.</param>
    /// <returns>The structural validation result and diagnostics.</returns>
    public DnsZoneValidationResult ValidateZone(DnsZone zone)
    {
        return DnsZoneValidator.ValidateZone(zone);
    }
}
