namespace qrcode;

/// <summary>Represents invalid caller input or configuration.</summary>
public class QrInputException : global::System.Exception
{
    /// <summary>The machine-readable failure category.</summary>
    public QrFailureReason FailureReason = QrFailureReason.InvalidInput;

    /// <summary>The input or option field associated with the failure, or <c>null</c> when unavailable.</summary>
    public string? InputName;

    /// <summary>Initializes an input exception with a message and input name.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="inputName">The input or option field associated with the failure.</param>
    public QrInputException(string message, string? inputName) : base(message)
    {
        InputName = inputName;
    }
}
