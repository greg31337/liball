namespace qrcode;

/// <summary>Controls QR quality measurements, grading inputs, and applicable profiles.</summary>
public class QrQualityOptions
{
    /// <summary>Whether QR Code Model 2 quality may be analyzed.</summary>
    public bool AllowModel2 = true;

    /// <summary>Whether Micro QR Code quality may be analyzed.</summary>
    public bool AllowMicro = true;

    /// <summary>Whether rectangular Micro QR Code quality may be analyzed.</summary>
    public bool AllowRectangularMicro = true;

    /// <summary>Whether legacy QR Code Model 1 quality may be analyzed.</summary>
    public bool AllowLegacyModel1;

    /// <summary>Whether <see cref="SymbolKind"/> is inferred instead of forced.</summary>
    public bool AutomaticSymbolKind = true;

    /// <summary>The symbol family used when <see cref="AutomaticSymbolKind"/> is disabled.</summary>
    public QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    /// <summary>The sampling-aperture width in modules, greater than zero and at most one.</summary>
    /// <remarks>Image quality analysis averages a square grid of samples centered on each module.</remarks>
    public double ApertureDiameterModules = 1.0;

    /// <summary>The number of luminance samples taken across each module axis, from one through sixteen.</summary>
    public int SamplesPerModule = 1;

    /// <summary>Whether symbol contrast is calculated.</summary>
    public bool AnalyzeContrast = true;

    /// <summary>Whether modulation is calculated.</summary>
    public bool AnalyzeModulation = true;

    /// <summary>Whether fixed-pattern damage is calculated.</summary>
    public bool AnalyzeFixedPatterns = true;

    /// <summary>Whether axial and grid non-uniformity are calculated.</summary>
    public bool AnalyzeGridUniformity = true;

    /// <summary>Whether print growth is calculated.</summary>
    public bool AnalyzePrintGrowth = true;

    /// <summary>Whether unused error correction is calculated.</summary>
    public bool AnalyzeUnusedErrorCorrection = true;

    /// <summary>Whether an overall grade requires calibrated verifier inputs.</summary>
    public bool RequireCalibratedInputs;

    /// <summary>The maximum number of module samples retained for one analysis.</summary>
    public int MaximumModuleSamples = 1048576;
}
