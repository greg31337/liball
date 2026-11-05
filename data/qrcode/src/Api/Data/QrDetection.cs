namespace qrcode;

/// <summary>Contains detected geometry, sampled modules, profile estimates, and confidence metadata.</summary>
public class QrDetection
{
    /// <summary>The detected symbol boundary, or <c>null</c> when geometry is incomplete.</summary>
    public QrQuadrilateral? Corners;

    /// <summary>The sampled module matrix, or <c>null</c> when sampling was not completed.</summary>
    public QrMatrix? SampledMatrix;

    /// <summary>The measured quiet-zone evidence, or <c>null</c> when it has not been measured.</summary>
    public QrQuietZoneMeasurement? QuietZone;

    /// <summary>Whether <see cref="EstimatedSymbolKind"/> contains a usable profile estimate.</summary>
    public bool HasSymbolKindEstimate;

    /// <summary>The estimated QR family.</summary>
    public QrSymbolKind EstimatedSymbolKind = QrSymbolKind.Model2;

    /// <summary>The estimated numbered version, or zero when unknown or dimension-based.</summary>
    public int EstimatedVersion;

    /// <summary>The estimated symbol width in modules.</summary>
    public int EstimatedWidth;

    /// <summary>The estimated symbol height in modules.</summary>
    public int EstimatedHeight;

    /// <summary>The detected polarity.</summary>
    public QrPolarity Polarity = QrPolarity.Automatic;

    /// <summary>Whether the available evidence distinguishes mirrored from ordinary geometry.</summary>
    public bool HasMirrorEstimate;

    /// <summary>Whether mirrored geometry was detected when <see cref="HasMirrorEstimate"/> is true.</summary>
    public bool IsMirrored;

    /// <summary>The overall detection confidence from zero through one.</summary>
    public double Confidence;

    /// <summary>The estimated module width in image pixels.</summary>
    public double ModuleSize;

    /// <summary>The stable zero-based candidate index assigned during detection.</summary>
    public int CandidateIndex;

    /// <summary>Human-readable detector diagnostics in processing order.</summary>
    public string[] Diagnostics = new string[0];
}
