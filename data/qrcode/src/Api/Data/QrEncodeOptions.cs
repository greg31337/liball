namespace qrcode;

/// <summary>Controls symbol selection, payload encoding, and encode resource limits.</summary>
public class QrEncodeOptions
{
    /// <summary>Whether QR Code Model 2 may be selected.</summary>
    public bool AllowModel2 = true;

    /// <summary>Whether Micro QR Code may be selected.</summary>
    public bool AllowMicro = true;

    /// <summary>Whether rectangular Micro QR Code may be selected.</summary>
    public bool AllowRectangularMicro = true;

    /// <summary>Whether legacy QR Code Model 1 may be selected.</summary>
    /// <remarks>Encoding legacy Model 1 is disabled by default.</remarks>
    public bool AllowLegacyModel1;

    /// <summary>Whether <see cref="SymbolKind"/> must be used instead of automatic family selection.</summary>
    public bool ForceSymbolKind;

    /// <summary>The symbol family to use when <see cref="ForceSymbolKind"/> is enabled.</summary>
    public QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    /// <summary>The minimum permitted numbered version, or zero when no numbered lower bound applies.</summary>
    public int MinimumVersion = 1;

    /// <summary>The maximum permitted numbered version.</summary>
    public int MaximumVersion = 40;

    /// <summary>The minimum permitted symbol width in modules, or zero when unconstrained.</summary>
    public int MinimumWidth;

    /// <summary>The maximum permitted symbol width in modules, or zero when unconstrained.</summary>
    public int MaximumWidth;

    /// <summary>The minimum permitted symbol height in modules, or zero when unconstrained.</summary>
    public int MinimumHeight;

    /// <summary>The maximum permitted symbol height in modules, or zero when unconstrained.</summary>
    public int MaximumHeight;

    /// <summary>Whether <see cref="ErrorCorrectionLevel"/> must be used exactly.</summary>
    public bool ForceErrorCorrectionLevel;

    /// <summary>The requested error-correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium;

    /// <summary>Whether error correction may be increased without enlarging the selected symbol.</summary>
    public bool BoostErrorCorrectionLevel = true;

    /// <summary>The requested mask or automatic mask selection.</summary>
    public QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    /// <summary>Whether text may be split into multiple modes to reduce encoded size.</summary>
    public bool OptimizeSegments = true;

    /// <summary>The initial ECI assignment, or -1 to use the standards-defined default interpretation.</summary>
    public int EciAssignment = -1;

    /// <summary>Whether FNC1 first-position processing is enabled.</summary>
    public bool UseFnc1First;

    /// <summary>The FNC1 second-position application indicator, or -1 when it is disabled.</summary>
    public int Fnc1SecondApplicationIndicator = -1;

    /// <summary>Whether the non-ISO Hanzi compatibility extension may be emitted.</summary>
    public bool EnableHanziCompatibility;

    /// <summary>The quiet-zone width in modules, or -1 to use the selected profile's standard width.</summary>
    public int QuietZoneModules = -1;

    /// <summary>The maximum accepted input byte count.</summary>
    public int MaximumInputBytes = 1048576;

    /// <summary>The maximum number of payload and control segments.</summary>
    public int MaximumSegments = 1024;

    /// <summary>The maximum number of structured-append symbols.</summary>
    public int MaximumStructuredAppendSymbols = 16;
}
