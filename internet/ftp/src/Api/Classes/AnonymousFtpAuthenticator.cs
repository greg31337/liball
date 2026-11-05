using System;

namespace ftp;

/// <summary>
/// Authenticates explicitly enabled anonymous FTP identities.
/// </summary>
public class AnonymousFtpAuthenticator : IFtpAuthenticator
{
    internal bool Enabled;
    internal string PrincipalName;
    internal bool RequireProtectedControl;
    internal int MaximumAttempts;
    internal int FailureDelayMilliseconds;

    /// <summary>
    /// Creates an anonymous authenticator from a validated fixed-user policy snapshot.
    /// </summary>
    /// <param name="options">The anonymous identity, protection, attempt, and delay policy.</param>
    /// <exception cref="System.ArgumentNullException">The options are null.</exception>
    /// <exception cref="FtpConfigurationException">The options are invalid.</exception>
    public AnonymousFtpAuthenticator(FtpFixedUserOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        FtpConfigurationValidator.ValidateFixedUserOptions(options);
        Enabled = options.AllowAnonymous;
        PrincipalName = options.AnonymousName;
        RequireProtectedControl = options.RequireProtectedControl;
        MaximumAttempts = options.MaximumAttempts;
        FailureDelayMilliseconds = options.FailureDelayMilliseconds;
    }

    /// <summary>
    /// Processes an anonymous USER or PASS authentication step.
    /// </summary>
    /// <param name="request">The anonymous credentials and channel context.</param>
    /// <returns>An exception-less authentication result.</returns>
    public FtpAuthenticationResult AuthenticateIdentity(
        FtpAuthenticationRequest request)
    {
        if (request == null)
        {
            return CreateFailure();
        }
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            FtpConfigurationValidator.HasInvalidText(request.UserName) ||
            FtpConfigurationValidator.HasInvalidOptionalText(request.Password) ||
            FtpConfigurationValidator.HasInvalidOptionalText(request.Account) ||
            FtpConfigurationValidator.HasInvalidOptionalText(
                request.CertificateIdentity))
        {
            return CreateFailure();
        }
        if (!Enabled || !IsAnonymousName(request.UserName))
        {
            return CreateUnhandled();
        }
        if (request.AttemptNumber < 1 ||
            request.AttemptNumber > MaximumAttempts ||
            (RequireProtectedControl && !request.ControlProtected))
        {
            return CreateFailure();
        }
        if (request.Password == null)
        {
            FtpAuthenticationResult additional = new FtpAuthenticationResult();
            additional.Status =
                FtpAuthenticationStatus.AdditionalInformationRequired;
            additional.Principal = CreatePrincipal(request, false);
            additional.Challenge = "Anonymous password required.";
            return additional;
        }

        FtpAuthenticationResult success = new FtpAuthenticationResult();
        success.Status = FtpAuthenticationStatus.Success;
        success.Principal = CreatePrincipal(request, true);
        return success;
    }

    internal bool IsAnonymousName(string userName)
    {
        return string.Equals(
            userName, PrincipalName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(userName, "anonymous", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(userName, "ftp", StringComparison.OrdinalIgnoreCase);
    }

    internal FtpPrincipal CreatePrincipal(
        FtpAuthenticationRequest request,
        bool authenticated)
    {
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = PrincipalName;
        principal.Authenticated = authenticated;
        principal.Anonymous = true;
        principal.Account = request.Account;
        principal.CertificateIdentity = request.CertificateIdentity;
        return principal;
    }

    internal FtpAuthenticationResult CreateFailure()
    {
        FtpAuthenticationResult failure = new FtpAuthenticationResult();
        failure.Status = FtpAuthenticationStatus.Failure;
        failure.FailureMessage = "Authentication failed.";
        failure.RetryDelayMilliseconds = FailureDelayMilliseconds;
        return failure;
    }

    internal FtpAuthenticationResult CreateUnhandled()
    {
        return new FtpAuthenticationResult();
    }
}
