namespace fix;

/// <summary>Identifies an application's decision for a session message.</summary>
public enum MessageDisposition
{
    /// <summary>Accept and continue processing the message.</summary>
    Accept = 0,
    /// <summary>Reject the message.</summary>
    Reject = 1,
    /// <summary>Discard the message without application processing.</summary>
    Drop = 2,
    /// <summary>Disconnect the session.</summary>
    Disconnect = 3
}
