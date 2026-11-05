namespace ftp;

/// <summary>
/// Configures the built-in fixed-user authenticator.
/// </summary>
public class FtpFixedUserOptions
{
    /// <summary>
    /// The configured fixed-user records.
    /// </summary>
    public FtpUserRecord[] Users;

    /// <summary>
    /// Indicates that anonymous authentication is enabled.
    /// </summary>
    public bool AllowAnonymous;

    /// <summary>
    /// The principal name assigned to anonymous sessions.
    /// </summary>
    public string AnonymousName;

    /// <summary>
    /// Indicates that authentication requires a protected control channel.
    /// </summary>
    public bool RequireProtectedControl;

    /// <summary>
    /// The maximum attempts accepted in one session.
    /// </summary>
    public int MaximumAttempts;

    /// <summary>
    /// The uniform failure delay in milliseconds.
    /// </summary>
    public int FailureDelayMilliseconds;

    /// <summary>
    /// Creates an empty authenticator configuration with authentication disabled.
    /// </summary>
    public FtpFixedUserOptions()
    {
        Users = new FtpUserRecord[0];
        AllowAnonymous = false;
        AnonymousName = "anonymous";
        RequireProtectedControl = true;
        MaximumAttempts = FtpDefaults.MaximumAuthenticationAttempts;
        FailureDelayMilliseconds = 1000;
    }
}

