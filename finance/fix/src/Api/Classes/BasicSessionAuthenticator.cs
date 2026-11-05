using System;
using System.Security.Cryptography;
using System.Text;

namespace fix;

/// <summary>Authenticates configured session identifiers and credentials before an optional custom decision.</summary>
public class BasicSessionAuthenticator : ISessionAuthenticator
{
    internal SessionId ExpectedId = new SessionId();
    internal string ExpectedUsername = "";
    internal string ExpectedPassword = "";
    internal ISessionAuthenticator? CustomAuthenticator;

    /// <summary>Creates a basic authenticator from immutable-by-copy session settings and an optional custom authenticator.</summary>
    /// <param name="settings">The expected session identity and credentials.</param>
    /// <param name="customAuthenticator">An optional final authenticator that can inspect raw data and logon context.</param>
    /// <exception cref="FixSessionException">Thrown when settings are missing.</exception>
    public BasicSessionAuthenticator(SessionSettings settings, ISessionAuthenticator? customAuthenticator)
    {
        if (settings == null)
        {
            throw new FixSessionException("The basic authentication settings are missing.");
        }
        ExpectedId = CloneSessionId(settings.Id);
        ExpectedUsername = settings.Username == null ? "" : settings.Username;
        ExpectedPassword = settings.Password == null ? "" : settings.Password;
        CustomAuthenticator = customAuthenticator;
    }

    /// <summary>Validates the complete session identity and configured credentials before applying a custom decision.</summary>
    /// <param name="request">The credentials, raw bytes, peer address, and logon context.</param>
    /// <returns>The explicit authentication result.</returns>
    public AuthenticationResult AuthenticateSession(AuthenticationRequest request)
    {
        if (request == null)
        {
            return CreateFailure("missing-authentication-request", "The authentication request is missing.");
        }
        if (request.Id == null)
        {
            return CreateFailure("missing-session-id", "The requested session identity is missing.");
        }
        AuthenticationResult? identityFailure = ValidateSessionId(request.Id);
        if (identityFailure != null)
        {
            return identityFailure;
        }
        if (ExpectedUsername.Length != 0 && !ConstantTimeMatches(ExpectedUsername, request.Username))
        {
            return CreateFailure("invalid-username", "The supplied username is invalid.");
        }
        if (ExpectedPassword.Length != 0 && !ConstantTimeMatches(ExpectedPassword, request.Password))
        {
            return CreateFailure("invalid-password", "The supplied password is invalid.");
        }
        ISessionAuthenticator? custom = CustomAuthenticator;
        if (custom != null)
        {
            AuthenticationResult customResult = custom.AuthenticateSession(request);
            if (customResult == null)
            {
                return CreateFailure("missing-custom-authentication-result", "The custom authenticator did not return a decision.");
            }
            return customResult;
        }
        return CreateSuccess();
    }

    internal AuthenticationResult? ValidateSessionId(SessionId actual)
    {
        if (!StringEquals(ExpectedId.BeginString, actual.BeginString))
        {
            return CreateFailure("begin-string-mismatch", "The requested BeginString does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.SenderCompID, actual.SenderCompID))
        {
            return CreateFailure("sender-comp-id-mismatch", "The requested SenderCompID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.SenderSubID, actual.SenderSubID))
        {
            return CreateFailure("sender-sub-id-mismatch", "The requested SenderSubID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.SenderLocationID, actual.SenderLocationID))
        {
            return CreateFailure("sender-location-id-mismatch", "The requested SenderLocationID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.TargetCompID, actual.TargetCompID))
        {
            return CreateFailure("target-comp-id-mismatch", "The requested TargetCompID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.TargetSubID, actual.TargetSubID))
        {
            return CreateFailure("target-sub-id-mismatch", "The requested TargetSubID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.TargetLocationID, actual.TargetLocationID))
        {
            return CreateFailure("target-location-id-mismatch", "The requested TargetLocationID does not match the configured session.");
        }
        if (!StringEquals(ExpectedId.Qualifier, actual.Qualifier))
        {
            return CreateFailure("session-qualifier-mismatch", "The requested qualifier does not match the configured session.");
        }
        return null;
    }

    internal static bool ConstantTimeMatches(string expected, string actual)
    {
        if (actual == null)
        {
            return false;
        }
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    internal static bool StringEquals(string expected, string actual)
    {
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    internal static SessionId CloneSessionId(SessionId source)
    {
        SessionId copy = new SessionId();
        if (source == null)
        {
            return copy;
        }
        copy.BeginString = source.BeginString;
        copy.SenderCompID = source.SenderCompID;
        copy.SenderSubID = source.SenderSubID;
        copy.SenderLocationID = source.SenderLocationID;
        copy.TargetCompID = source.TargetCompID;
        copy.TargetSubID = source.TargetSubID;
        copy.TargetLocationID = source.TargetLocationID;
        copy.Qualifier = source.Qualifier;
        return copy;
    }

    internal static AuthenticationResult CreateSuccess()
    {
        AuthenticationResult result = new AuthenticationResult();
        result.Authenticated = true;
        result.Code = "authenticated";
        result.Text = "Authentication succeeded.";
        return result;
    }

    internal static AuthenticationResult CreateFailure(string code, string text)
    {
        AuthenticationResult result = new AuthenticationResult();
        result.Disconnect = true;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
