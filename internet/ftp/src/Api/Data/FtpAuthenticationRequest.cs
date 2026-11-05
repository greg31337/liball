namespace ftp;

/// <summary>
/// Carries one authentication step to an FTP authentication provider.
/// </summary>
public class FtpAuthenticationRequest
{
    /// <summary>
    /// The USER value.
    /// </summary>
    public string UserName;

    /// <summary>
    /// The optional PASS value.
    /// </summary>
    public string? Password;

    /// <summary>
    /// The optional ACCT value.
    /// </summary>
    public string? Account;

    /// <summary>
    /// The optional principal established by an earlier authentication step.
    /// </summary>
    public FtpPrincipal? Principal;

    /// <summary>
    /// The optional TLS client-certificate identity.
    /// </summary>
    public string? CertificateIdentity;

    /// <summary>
    /// Indicates that the control channel is protected.
    /// </summary>
    public bool ControlProtected;

    /// <summary>
    /// The one-based authentication attempt number.
    /// </summary>
    public int AttemptNumber;

    /// <summary>
    /// Creates an empty, unprotected first-step authentication request.
    /// </summary>
    public FtpAuthenticationRequest()
    {
        UserName = "";
        Password = null;
        Account = null;
        Principal = null;
        CertificateIdentity = null;
        ControlProtected = false;
        AttemptNumber = 0;
    }
}

