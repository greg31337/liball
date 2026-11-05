namespace ftp;

/// <summary>
/// Receives structured FTP lifecycle and operation events.
/// </summary>
public interface IFtpEventSink
{
    /// <summary>
    /// Records one event without exposing credentials or other secrets.
    /// </summary>
    /// <param name="eventData">The structured event data.</param>
    void RecordEvent(FtpEvent eventData);
}

