namespace qrcode;

/// <summary>Identifies a member of the QR Code symbol family.</summary>
public enum QrSymbolKind
{
    /// <summary>QR Code Model 2.</summary>
    Model2 = 0,

    /// <summary>Micro QR Code.</summary>
    Micro = 1,

    /// <summary>Rectangular Micro QR Code.</summary>
    rMQR = 2,

    /// <summary>Legacy QR Code Model 1.</summary>
    LegacyModel1 = 3
}
