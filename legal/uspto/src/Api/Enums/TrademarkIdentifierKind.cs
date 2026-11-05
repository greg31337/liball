namespace uspto;

/// <summary>
/// Identifies the form of a trademark case identifier.
/// </summary>
public enum TrademarkIdentifierKind
{
    /// <summary>The identifier kind is not known.</summary>
    Unknown = 0,

    /// <summary>A United States trademark serial number.</summary>
    SerialNumber = 1,

    /// <summary>A United States trademark registration number.</summary>
    RegistrationNumber = 2,

    /// <summary>A USPTO trademark reference number.</summary>
    ReferenceNumber = 3,

    /// <summary>A Madrid international registration number.</summary>
    InternationalRegistrationNumber = 4
}
