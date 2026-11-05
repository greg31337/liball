namespace dns;

/// <summary>Parses, validates, and formats DNS zone master files.</summary>
public interface IDnsZoneCodec
{
    /// <summary>Parses one zone master-file source.</summary>
    /// <param name="source">The complete master-file text.</param>
    /// <param name="options">The origin, directive, and resource limits.</param>
    /// <returns>The loaded zone or source diagnostics.</returns>
    DnsZoneLoadResult ParseZone(
        string source,
        DnsZoneLoadOptions options);

    /// <summary>Formats one zone as canonical or presentation-oriented master-file text.</summary>
    /// <param name="zone">The zone to format.</param>
    /// <param name="options">The requested text formatting policy.</param>
    /// <returns>The formatted text or diagnostics.</returns>
    DnsTextResult FormatZone(
        DnsZone zone,
        DnsTextOptions options);

    /// <summary>Validates the structure and record relationships of one zone.</summary>
    /// <param name="zone">The zone to validate.</param>
    /// <returns>The validation state and diagnostics.</returns>
    DnsZoneValidationResult ValidateZone(DnsZone zone);
}
