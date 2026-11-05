using System;
using System.Security.Cryptography;
using System.Text;

namespace ftp;

/// <summary>
/// Authenticates a fixed snapshot of salted user verifier records.
/// </summary>
public class FixedUserFtpAuthenticator : IFtpAuthenticator
{
    internal static readonly byte[] DummySalt =
        Encoding.UTF8.GetBytes("ftp-fixed-user-dummy-salt");
    internal static readonly byte[] DummyVerifier =
        SHA256.HashData(DummySalt);
    internal FtpUserRecord[] Users;
    internal AnonymousFtpAuthenticator AnonymousAuthenticator;
    internal bool RequireProtectedControl;
    internal int MaximumAttempts;
    internal int FailureDelayMilliseconds;

    /// <summary>
    /// Creates an authenticator from a validated snapshot of fixed-user records and policy.
    /// </summary>
    /// <param name="options">The user records, anonymous option, protection, attempt, and delay policy.</param>
    /// <exception cref="System.ArgumentNullException">The options are null.</exception>
    /// <exception cref="FtpConfigurationException">The options are invalid.</exception>
    public FixedUserFtpAuthenticator(FtpFixedUserOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        FtpConfigurationValidator.ValidateFixedUserOptions(options);
        FtpFixedUserOptions? snapshot =
            FtpServerOptionsSnapshot.CopyFixedUsers(options);
        if (snapshot == null)
        {
            throw new FtpConfigurationException(
                "Fixed-user options could not be copied.", null);
        }
        Users = snapshot.Users;
        AnonymousAuthenticator = new AnonymousFtpAuthenticator(snapshot);
        RequireProtectedControl = snapshot.RequireProtectedControl;
        MaximumAttempts = snapshot.MaximumAttempts;
        FailureDelayMilliseconds = snapshot.FailureDelayMilliseconds;
    }

    /// <summary>
    /// Processes one USER, PASS, or ACCT authentication step.
    /// </summary>
    /// <param name="request">The credentials, intermediate principal, and channel context.</param>
    /// <returns>An exception-less authentication result with uniform failures.</returns>
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
            PerformDummyVerification(request.Password);
            return CreateFailure();
        }

        FtpAuthenticationResult anonymous =
            AnonymousAuthenticator.AuthenticateIdentity(request);
        if (anonymous.Status != FtpAuthenticationStatus.NotHandled)
        {
            return anonymous;
        }
        if (request.AttemptNumber < 1 ||
            request.AttemptNumber > MaximumAttempts ||
            (RequireProtectedControl && !request.ControlProtected))
        {
            PerformDummyVerification(request.Password);
            return CreateFailure();
        }

        FtpUserRecord? user = FindUser(request.UserName);
        if (request.Password == null)
        {
            if (request.Account != null && request.Principal != null &&
                request.Principal.Authenticated)
            {
                return CompleteAccountStep(request, user);
            }
            return CreatePasswordChallenge(request);
        }

        byte[] salt = user == null ? DummySalt : user.Salt;
        byte[] expected = user == null ? DummyVerifier : user.PasswordVerifier;
        byte[] actual = DerivePasswordVerifier(salt, request.Password);
        bool passwordMatches =
            CryptographicOperations.FixedTimeEquals(actual, expected);
        CryptographicOperations.ZeroMemory(actual);
        if (user == null || user.Disabled || !passwordMatches)
        {
            return CreateFailure();
        }
        if (user.Account != null && request.Account == null)
        {
            FtpAuthenticationResult accountRequired =
                new FtpAuthenticationResult();
            accountRequired.Status =
                FtpAuthenticationStatus.AdditionalInformationRequired;
            accountRequired.Principal = CreatePrincipal(
                user, request, true, null);
            accountRequired.Challenge = "Account information required.";
            return accountRequired;
        }
        if (user.Account != null &&
            !FixedTimeTextEquals(user.Account, request.Account))
        {
            return CreateFailure();
        }

        return CreateSuccess(user, request, request.Account);
    }

    internal FtpAuthenticationResult CompleteAccountStep(
        FtpAuthenticationRequest request,
        FtpUserRecord? user)
    {
        if (user == null || user.Disabled || user.Account == null ||
            request.Principal == null ||
            !string.Equals(
                request.Principal.Name, user.UserName, StringComparison.Ordinal) ||
            !FixedTimeTextEquals(user.Account, request.Account))
        {
            PerformDummyVerification(request.Account);
            return CreateFailure();
        }
        return CreateSuccess(user, request, request.Account);
    }

    internal FtpAuthenticationResult CreatePasswordChallenge(
        FtpAuthenticationRequest request)
    {
        FtpAuthenticationResult challenge = new FtpAuthenticationResult();
        challenge.Status =
            FtpAuthenticationStatus.AdditionalInformationRequired;
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = request.UserName;
        principal.CertificateIdentity = request.CertificateIdentity;
        challenge.Principal = principal;
        challenge.Challenge = "Password required.";
        return challenge;
    }

    internal FtpAuthenticationResult CreateSuccess(
        FtpUserRecord user,
        FtpAuthenticationRequest request,
        string? account)
    {
        FtpAuthenticationResult success = new FtpAuthenticationResult();
        success.Status = FtpAuthenticationStatus.Success;
        success.Principal = CreatePrincipal(user, request, true, account);
        return success;
    }

    internal FtpPrincipal CreatePrincipal(
        FtpUserRecord user,
        FtpAuthenticationRequest request,
        bool authenticated,
        string? account)
    {
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = user.UserName;
        principal.Authenticated = authenticated;
        principal.Anonymous = false;
        principal.Account = account;
        principal.CertificateIdentity = request.CertificateIdentity == null &&
            request.Principal != null ?
                request.Principal.CertificateIdentity : request.CertificateIdentity;
        principal.Roles = FtpServerOptionsSnapshot.CopyStrings(user.Roles);
        principal.HomeDirectory = user.HomeDirectory;
        principal.ResourceLimits =
            FtpServerOptionsSnapshot.CopyResourceLimits(user.ResourceLimits);
        return principal;
    }

    internal FtpUserRecord? FindUser(string userName)
    {
        FtpUserRecord? match = null;
        int index = 0;
        while (index < Users.Length)
        {
            if (string.Equals(
                Users[index].UserName, userName, StringComparison.Ordinal))
            {
                match = Users[index];
            }
            index++;
        }
        return match;
    }

    internal byte[] DerivePasswordVerifier(byte[] salt, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(
            passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);
        byte[] verifier = SHA256.HashData(saltedPassword);
        CryptographicOperations.ZeroMemory(passwordBytes);
        CryptographicOperations.ZeroMemory(saltedPassword);
        return verifier;
    }

    internal bool FixedTimeTextEquals(string expected, string? supplied)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] suppliedBytes = supplied == null ?
            new byte[0] : Encoding.UTF8.GetBytes(supplied);
        bool matches = CryptographicOperations.FixedTimeEquals(
            expectedBytes, suppliedBytes);
        CryptographicOperations.ZeroMemory(expectedBytes);
        CryptographicOperations.ZeroMemory(suppliedBytes);
        return matches;
    }

    internal void PerformDummyVerification(string? supplied)
    {
        string secret = supplied == null ? "" : supplied;
        byte[] actual = DerivePasswordVerifier(DummySalt, secret);
        CryptographicOperations.FixedTimeEquals(actual, DummyVerifier);
        CryptographicOperations.ZeroMemory(actual);
    }

    internal FtpAuthenticationResult CreateFailure()
    {
        FtpAuthenticationResult failure = new FtpAuthenticationResult();
        failure.Status = FtpAuthenticationStatus.Failure;
        failure.FailureMessage = "Authentication failed.";
        failure.RetryDelayMilliseconds = FailureDelayMilliseconds;
        return failure;
    }
}
