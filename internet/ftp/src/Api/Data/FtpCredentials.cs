namespace ftp;

/// <summary>
/// Carries FTP USER, PASS, and optional ACCT credentials.
/// </summary>
public class FtpCredentials
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
    /// Indicates that anonymous authentication semantics are requested.
    /// </summary>
    public bool Anonymous;

    /// <summary>
    /// Creates empty, non-anonymous credentials.
    /// </summary>
    public FtpCredentials()
    {
        UserName = "";
        Password = null;
        Account = null;
        Anonymous = false;
    }
}

