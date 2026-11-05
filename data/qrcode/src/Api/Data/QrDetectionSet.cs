namespace qrcode;

/// <summary>Contains deterministically ordered detections and preprocessing diagnostics.</summary>
public class QrDetectionSet
{
    /// <summary>The detected symbols in deterministic result order.</summary>
    public QrDetection[] Detections = new QrDetection[0];

    /// <summary>The binarization strategy that produced the selected candidates.</summary>
    public QrBinarizationMode BinarizationMode = QrBinarizationMode.Automatic;

    /// <summary>The polarity that produced the selected candidates.</summary>
    public QrPolarity Polarity = QrPolarity.Automatic;

    /// <summary>The selected binary preprocessing raster, or <c>null</c> when not retained.</summary>
    public QrRaster? BinaryRaster;

    /// <summary>The thresholds retained from preprocessing regions or candidates.</summary>
    public int[] Thresholds = new int[0];

    /// <summary>The number of candidates examined before duplicate suppression.</summary>
    public int CandidateCount;

    /// <summary>Human-readable preprocessing diagnostics in processing order.</summary>
    public string[] Diagnostics = new string[0];
}
