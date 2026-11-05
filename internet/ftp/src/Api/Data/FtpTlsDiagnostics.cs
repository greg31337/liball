namespace ftp;

/// <summary>
/// Carries negotiated TLS facts without certificate bodies, keys, or secret material.
/// </summary>
public class FtpTlsDiagnostics
{
    /// <summary>
    /// Indicates that TLS authentication completed successfully.
    /// </summary>
    public bool Authenticated;

    /// <summary>
    /// Indicates that the local endpoint used the TLS server role.
    /// </summary>
    public bool ServerRole;

    /// <summary>
    /// Indicates that TLS was established before FTP application traffic.
    /// </summary>
    public bool ImplicitTls;

    /// <summary>
    /// Indicates that a clear FTP control connection was explicitly upgraded.
    /// </summary>
    public bool ExplicitUpgrade;

    /// <summary>
    /// Indicates that the negotiated stream protects an FTP data connection.
    /// </summary>
    public bool DataChannel;

    /// <summary>
    /// Indicates that TLS session resumption was permitted.
    /// </summary>
    public bool TlsResumeAllowed;

    /// <summary>
    /// Indicates that application data is encrypted.
    /// </summary>
    public bool Encrypted;

    /// <summary>
    /// Indicates that application data is integrity protected.
    /// </summary>
    public bool Signed;

    /// <summary>
    /// Indicates that both TLS peers authenticated with certificates.
    /// </summary>
    public bool MutuallyAuthenticated;

    /// <summary>
    /// The negotiated TLS protocol name.
    /// </summary>
    public string Protocol;

    /// <summary>
    /// The negotiated TLS cipher-suite name.
    /// </summary>
    public string CipherSuite;

    /// <summary>
    /// Indicates that a local certificate participated in the handshake.
    /// </summary>
    public bool LocalCertificateAvailable;

    /// <summary>
    /// Indicates that a remote certificate participated in the handshake.
    /// </summary>
    public bool RemoteCertificateAvailable;

    /// <summary>
    /// The optional SHA-256 fingerprint of the remote certificate, never its encoded body.
    /// </summary>
    public string? RemoteCertificateSha256Fingerprint;

    /// <summary>
    /// Indicates that runtime TLS session-reuse evidence is available.
    /// </summary>
    public bool SessionReuseEvidenceAvailable;

    /// <summary>
    /// Indicates that the TLS runtime reports session reuse.
    /// </summary>
    public bool SessionReused;

    /// <summary>
    /// Indicates that a reused TLS session matches the protected control session.
    /// </summary>
    public bool SessionMatchesControl;

    /// <summary>
    /// Creates an unauthenticated diagnostic snapshot without certificate identifiers.
    /// </summary>
    public FtpTlsDiagnostics()
    {
        Authenticated = false;
        ServerRole = false;
        ImplicitTls = false;
        ExplicitUpgrade = false;
        DataChannel = false;
        TlsResumeAllowed = false;
        Encrypted = false;
        Signed = false;
        MutuallyAuthenticated = false;
        Protocol = "";
        CipherSuite = "";
        LocalCertificateAvailable = false;
        RemoteCertificateAvailable = false;
        RemoteCertificateSha256Fingerprint = null;
        SessionReuseEvidenceAvailable = false;
        SessionReused = false;
        SessionMatchesControl = false;
    }
}
