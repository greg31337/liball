namespace qrcode;

/// <summary>Describes a low-level request to evaluate payload capacity.</summary>
public class QrCapacityRequest
{
    /// <summary>The requested QR family.</summary>
    public QrSymbolKind SymbolKind;

    /// <summary>The requested version number, or zero when the profile uses explicit dimensions.</summary>
    public int Version;

    /// <summary>The requested symbol width in modules, or zero when derived from the version.</summary>
    public int Width;

    /// <summary>The requested symbol height in modules, or zero when derived from the version.</summary>
    public int Height;

    /// <summary>The requested error-correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel;

    /// <summary>The payload mode whose count header is included in the calculation.</summary>
    public QrEncodingMode EncodingMode;

    /// <summary>The mode-specific number of payload characters.</summary>
    public int CharacterCount;

    /// <summary>The number of payload data bits excluding headers.</summary>
    public int PayloadBitCount;

    /// <summary>The number of additional control-header bits.</summary>
    public int ControlBitCount;
}
