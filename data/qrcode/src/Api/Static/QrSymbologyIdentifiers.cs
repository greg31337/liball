namespace qrcode;

/// <summary>Defines ISO/IEC 15424 QR Code symbology identifiers.</summary>
public static class QrSymbologyIdentifiers
{
    /// <summary>The two-character QR symbology identifier prefix.</summary>
    public const string Prefix = "]Q";

    /// <summary>The legacy QR Code Model 1 identifier.</summary>
    public const string LegacyModel1 = "]Q0";

    /// <summary>The QR Code Model 2 identifier without ECI or FNC1.</summary>
    public const string Model2 = "]Q1";

    /// <summary>The QR Code Model 2 identifier with ECI protocol active.</summary>
    public const string Model2WithEci = "]Q2";

    /// <summary>The QR Code Model 2 identifier with FNC1 first position active.</summary>
    public const string Model2WithFnc1First = "]Q3";

    /// <summary>The QR Code Model 2 identifier with ECI and FNC1 first position active.</summary>
    public const string Model2WithEciAndFnc1First = "]Q4";

    /// <summary>The QR Code Model 2 identifier with FNC1 second position active.</summary>
    public const string Model2WithFnc1Second = "]Q5";

    /// <summary>The QR Code Model 2 identifier with ECI and FNC1 second position active.</summary>
    public const string Model2WithEciAndFnc1Second = "]Q6";
}
