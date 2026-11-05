namespace fake;

/// <summary>
/// Selects the textual presentation of a generated phone fixture.
/// </summary>
public enum PhoneNumberFormat
{
    /// <summary>Uses the locale's national display format.</summary>
    National = 0,

    /// <summary>Uses the locale's international display format.</summary>
    International = 1,

    /// <summary>Uses a compact E.164-shaped format beginning with a plus sign.</summary>
    E164 = 2
}
