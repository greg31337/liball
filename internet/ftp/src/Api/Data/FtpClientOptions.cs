namespace ftp;

/// <summary>
/// Configures an FTP client session.
/// </summary>
public class FtpClientOptions
{
    /// <summary>
    /// The remote control endpoint.
    /// </summary>
    public FtpEndpoint Endpoint;

    /// <summary>
    /// The optional local endpoint bound before opening the control connection.
    /// </summary>
    public FtpEndpoint? LocalBindEndpoint;

    /// <summary>
    /// Credentials used when authentication is requested.
    /// </summary>
    public FtpCredentials Credentials;

    /// <summary>
    /// Shared operation timeout and cancellation settings.
    /// </summary>
    public FtpTimeoutOptions Timeouts;

    /// <summary>
    /// Control-channel TCP socket behavior.
    /// </summary>
    public FtpSocketOptions ControlSocket;

    /// <summary>
    /// Data-connection behavior.
    /// </summary>
    public FtpDataConnectionOptions DataConnection;

    /// <summary>
    /// TLS and certificate behavior.
    /// </summary>
    public FtpTlsOptions Tls;

    /// <summary>
    /// Explicit compatibility behavior for nonconforming servers.
    /// </summary>
    public FtpClientQuirkOptions Quirks;

    /// <summary>
    /// The required control-channel security mode.
    /// </summary>
    public FtpSecurityMode SecurityMode;

    /// <summary>
    /// The optional HOST value sent before authentication.
    /// </summary>
    public string? VirtualHost;

    /// <summary>
    /// Indicates that FEAT and supported options are negotiated.
    /// </summary>
    public bool NegotiateFeatures;

    /// <summary>
    /// Indicates that UTF-8 is enabled when supported.
    /// </summary>
    public bool EnableUtf8;

    /// <summary>
    /// Indicates that advertised PRET compatibility is used before data-connection setup.
    /// </summary>
    public bool EnablePret;

    /// <summary>
    /// The optional client identifier sent with CLNT when the server advertises it.
    /// </summary>
    public string? ClientName;

    /// <summary>
    /// Indicates that credentials may be sent on an unprotected control channel.
    /// </summary>
    public bool AllowPlaintextCredentials;

    /// <summary>
    /// Indicates that malformed or nonstandard replies are rejected.
    /// </summary>
    public bool StrictReplyParsing;

    /// <summary>
    /// Indicates that commands and replies redact credential and certificate secrets.
    /// </summary>
    public bool RedactSecrets;

    /// <summary>
    /// The number of automatic connection retries.
    /// </summary>
    public int RetryCount;

    /// <summary>
    /// Creates a passive, explicit-TLS-required client configuration.
    /// </summary>
    public FtpClientOptions()
    {
        Endpoint = new FtpEndpoint();
        LocalBindEndpoint = null;
        Credentials = new FtpCredentials();
        Timeouts = new FtpTimeoutOptions();
        ControlSocket = new FtpSocketOptions();
        DataConnection = new FtpDataConnectionOptions();
        Tls = new FtpTlsOptions();
        Quirks = new FtpClientQuirkOptions();
        SecurityMode = FtpSecurityMode.ExplicitTlsRequired;
        VirtualHost = null;
        NegotiateFeatures = true;
        EnableUtf8 = true;
        EnablePret = false;
        ClientName = null;
        AllowPlaintextCredentials = FtpDefaults.AllowPlaintextCredentials;
        StrictReplyParsing = true;
        RedactSecrets = true;
        RetryCount = FtpDefaults.RetryCount;
    }
}
