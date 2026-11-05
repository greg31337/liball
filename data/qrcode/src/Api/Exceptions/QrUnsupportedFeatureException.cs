namespace qrcode;

/// <summary>Represents a recognized QR feature that the requested workflow cannot process.</summary>
public class QrUnsupportedFeatureException : global::System.Exception
{
    /// <summary>The machine-readable failure category.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The stable name of the unsupported feature.</summary>
    public string FeatureName;

    /// <summary>The associated protocol value, or -1 when no numeric value applies.</summary>
    public int ProtocolValue;

    /// <summary>Initializes an unsupported-feature exception with protocol context.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="failureReason">The machine-readable feature failure category.</param>
    /// <param name="featureName">The stable name of the unsupported feature.</param>
    /// <param name="protocolValue">The associated protocol value, or -1.</param>
    public QrUnsupportedFeatureException(
        string message,
        QrFailureReason failureReason,
        string featureName,
        int protocolValue) : base(message)
    {
        FailureReason = failureReason;
        FeatureName = featureName;
        ProtocolValue = protocolValue;
    }
}
