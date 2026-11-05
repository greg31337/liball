namespace ftp;

/// <summary>
/// Stores one fixed-user authentication record without a plaintext password.
/// </summary>
public class FtpUserRecord
{
    /// <summary>
    /// The case-sensitive or provider-normalized user name.
    /// </summary>
    public string UserName;

    /// <summary>
    /// The password-verifier salt.
    /// </summary>
    public byte[] Salt;

    /// <summary>
    /// The SHA-256 verifier derived from the salt bytes followed by the UTF-8 password bytes.
    /// </summary>
    public byte[] PasswordVerifier;

    /// <summary>
    /// Provider-defined roles assigned after authentication.
    /// </summary>
    public string[] Roles;

    /// <summary>
    /// The optional virtual home directory.
    /// </summary>
    public string? HomeDirectory;

    /// <summary>
    /// The optional required ACCT value.
    /// </summary>
    public string? Account;

    /// <summary>
    /// Indicates that this user record cannot authenticate.
    /// </summary>
    public bool Disabled;

    /// <summary>
    /// Indicates that the user may be considered for write authorization.
    /// </summary>
    public bool AllowWrite;

    /// <summary>
    /// Optional storage, transfer, bandwidth, and listing limits applied after authentication.
    /// </summary>
    public FtpResourceLimits ResourceLimits;

    /// <summary>
    /// Creates an empty disabled record with no verifier.
    /// </summary>
    public FtpUserRecord()
    {
        UserName = "";
        Salt = new byte[0];
        PasswordVerifier = new byte[0];
        Roles = new string[0];
        HomeDirectory = null;
        Account = null;
        Disabled = true;
        AllowWrite = false;
        ResourceLimits = new FtpResourceLimits();
    }
}
