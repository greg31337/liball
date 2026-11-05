namespace fix;

/// <summary>Contains the explicit outcome of submitting a message to a session.</summary>
public class SessionSendResult
{
    /// <summary>Whether the message was accepted for transmission.</summary>
    public bool Accepted;
    /// <summary>The application or backpressure disposition.</summary>
    public MessageDisposition Disposition;
    /// <summary>The assigned sender sequence number, or zero when not assigned.</summary>
    public int SequenceNumber;
    /// <summary>The pending outbound queue count.</summary>
    public int QueueCount;
    /// <summary>A stable machine-readable outcome code.</summary>
    public string Code = "";
    /// <summary>A human-readable outcome description.</summary>
    public string Text = "";
}
