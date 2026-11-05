namespace fake;

/// <summary>Describes one supported issuer's PAN and verification-code shapes.</summary>
/// <remarks>Patterns and test PANs describe fixtures only and make no account, acceptance, or authorization claim.</remarks>
public class CardIssuerPattern
{
    /// <summary>The supported issuer.</summary>
    public CardIssuer Issuer;

    /// <summary>The issuer display name.</summary>
    public string Name = string.Empty;

    /// <summary>The supported decimal prefix or inclusive-prefix-range descriptions.</summary>
    public string[] PrefixPatterns = new string[0];

    /// <summary>The supported PAN lengths associated with the issuer shape.</summary>
    public int[] PanLengths = new int[0];

    /// <summary>The expected verification-code length.</summary>
    public int CvvLength;

    /// <summary>The Luhn-valid test PAN used for generated fixtures.</summary>
    public string TestPan = string.Empty;

    /// <summary>Whether the generated fixture is explicitly test-only.</summary>
    public bool IsSyntheticTestPattern;

    /// <summary>The explicit project marker attached to generated test-card records.</summary>
    public string TestMarker = string.Empty;

    internal CardIssuerPattern()
    {
    }
}
