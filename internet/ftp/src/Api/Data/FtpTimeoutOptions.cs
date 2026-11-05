using System.Threading;

namespace ftp;

/// <summary>
/// Defines bounded timeouts and cancellation for synchronous FTP operations.
/// </summary>
public class FtpTimeoutOptions
{
    /// <summary>
    /// The control-connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMilliseconds;

    /// <summary>
    /// The control-channel read timeout in milliseconds.
    /// </summary>
    public int ControlReadTimeoutMilliseconds;

    /// <summary>
    /// The control-channel write timeout in milliseconds.
    /// </summary>
    public int ControlWriteTimeoutMilliseconds;

    /// <summary>
    /// The data-connection setup timeout in milliseconds.
    /// </summary>
    public int DataConnectionTimeoutMilliseconds;

    /// <summary>
    /// The TLS handshake timeout in milliseconds.
    /// </summary>
    public int TlsHandshakeTimeoutMilliseconds;

    /// <summary>
    /// The session idle timeout in milliseconds.
    /// </summary>
    public int IdleTimeoutMilliseconds;

    /// <summary>
    /// The graceful server stop timeout in milliseconds.
    /// </summary>
    public int GracefulStopTimeoutMilliseconds;

    /// <summary>
    /// Cancellation observed by synchronous operations.
    /// </summary>
    public CancellationToken CancellationToken;

    /// <summary>
    /// Creates timeout options using the bounded library defaults.
    /// </summary>
    public FtpTimeoutOptions()
    {
        ConnectTimeoutMilliseconds = FtpDefaults.ConnectTimeoutMilliseconds;
        ControlReadTimeoutMilliseconds = FtpDefaults.ControlReadTimeoutMilliseconds;
        ControlWriteTimeoutMilliseconds = FtpDefaults.ControlWriteTimeoutMilliseconds;
        DataConnectionTimeoutMilliseconds = FtpDefaults.DataConnectionTimeoutMilliseconds;
        TlsHandshakeTimeoutMilliseconds = FtpDefaults.TlsHandshakeTimeoutMilliseconds;
        IdleTimeoutMilliseconds = FtpDefaults.IdleTimeoutMilliseconds;
        GracefulStopTimeoutMilliseconds = FtpDefaults.GracefulStopTimeoutMilliseconds;
        CancellationToken = default(CancellationToken);
    }
}

