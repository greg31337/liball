namespace qrcode;

/// <summary>Represents an operation rejected before exceeding a configured resource limit.</summary>
public class QrResourceLimitException : global::System.Exception
{
    /// <summary>The machine-readable failure category.</summary>
    public QrFailureReason FailureReason = QrFailureReason.ResourceLimit;

    /// <summary>The stable name of the exceeded limit.</summary>
    public string LimitName;

    /// <summary>The requested count, size, or amount.</summary>
    public int RequestedValue;

    /// <summary>The configured maximum count, size, or amount.</summary>
    public int MaximumValue;

    /// <summary>Initializes a resource-limit exception with requested and maximum values.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="limitName">The stable name of the exceeded limit.</param>
    /// <param name="requestedValue">The requested count, size, or amount.</param>
    /// <param name="maximumValue">The configured maximum count, size, or amount.</param>
    public QrResourceLimitException(
        string message,
        string limitName,
        int requestedValue,
        int maximumValue) : base(message)
    {
        LimitName = limitName;
        RequestedValue = requestedValue;
        MaximumValue = maximumValue;
    }
}
