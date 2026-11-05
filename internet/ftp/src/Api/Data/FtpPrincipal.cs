namespace ftp;

/// <summary>
/// Represents an authenticated or anonymous FTP identity.
/// </summary>
public class FtpPrincipal
{
    /// <summary>
    /// The stable provider-defined identity name.
    /// </summary>
    public string Name;

    /// <summary>
    /// Indicates that authentication completed successfully.
    /// </summary>
    public bool Authenticated;

    /// <summary>
    /// Indicates that the identity is anonymous.
    /// </summary>
    public bool Anonymous;

    /// <summary>
    /// The optional account name associated with the identity.
    /// </summary>
    public string? Account;

    /// <summary>
    /// The optional TLS client-certificate identity.
    /// </summary>
    public string? CertificateIdentity;

    /// <summary>
    /// Provider-defined roles assigned to the identity.
    /// </summary>
    public string[] Roles;

    /// <summary>
    /// The optional virtual home directory.
    /// </summary>
    public string? HomeDirectory;

    /// <summary>
    /// Optional storage, transfer, bandwidth, and listing limits for the identity.
    /// </summary>
    public FtpResourceLimits ResourceLimits;

    /// <summary>
    /// Creates an unauthenticated, non-anonymous identity.
    /// </summary>
    public FtpPrincipal()
    {
        Name = "";
        Authenticated = false;
        Anonymous = false;
        Account = null;
        CertificateIdentity = null;
        Roles = new string[0];
        HomeDirectory = null;
        ResourceLimits = new FtpResourceLimits();
    }
}
