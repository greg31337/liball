namespace ftp;

/// <summary>
/// Discards structured FTP events.
/// </summary>
public class NullFtpEventSink : IFtpEventSink
{
    /// <summary>
    /// Creates a stateless event sink that performs no work.
    /// </summary>
    public NullFtpEventSink()
    {
    }

    /// <summary>
    /// Discards one event.
    /// </summary>
    /// <param name="eventData">The event to discard.</param>
    public void RecordEvent(FtpEvent eventData)
    {
    }
}
