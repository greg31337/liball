namespace fix;

/// <summary>Authenticates inbound FIX logon requests.</summary>
public interface ISessionAuthenticator
{
    /// <summary>Authenticates one logon request.</summary>
    /// <param name="request">The credentials and logon context.</param>
    /// <returns>The explicit authentication outcome.</returns>
    AuthenticationResult AuthenticateSession(AuthenticationRequest request);
}
