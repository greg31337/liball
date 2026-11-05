namespace ftp;

/// <summary>
/// Authenticates USER, PASS, ACCT, anonymous, or client-certificate identities.
/// </summary>
public interface IFtpAuthenticator
{
    /// <summary>
    /// Processes one authentication step.
    /// </summary>
    /// <param name="request">The credentials and protected-channel context.</param>
    /// <returns>The exception-less authentication result.</returns>
    FtpAuthenticationResult AuthenticateIdentity(FtpAuthenticationRequest request);
}

