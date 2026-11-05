namespace pop3;

/// <summary>Provides authentication decisions for POP3 sessions.</summary>
public interface IPop3AuthenticationProvider
{
    /// <summary>Checks whether the login-delay policy permits an authentication attempt.</summary>
    /// <param name="username">The claimed user name.</param>
    /// <param name="connection">The safe accepted-connection context.</param>
    /// <param name="cancellationToken">A token that can cancel provider work.</param>
    /// <returns>A success result when permitted, or a classified delay result.</returns>
    Pop3AuthenticationResult CheckLoginDelay(string username, Pop3ServerConnection connection, System.Threading.CancellationToken cancellationToken);

    /// <summary>Classifies a USER and PASS authentication attempt.</summary>
    /// <param name="credentials">The supplied credentials.</param>
    /// <param name="connection">The safe accepted-connection context.</param>
    /// <param name="cancellationToken">A token that can cancel provider work.</param>
    /// <returns>The classified authentication result.</returns>
    Pop3AuthenticationResult AuthenticateUserPassword(Pop3Credentials credentials, Pop3ServerConnection connection, System.Threading.CancellationToken cancellationToken);

    /// <summary>Classifies an APOP authentication attempt.</summary>
    /// <param name="request">The supplied APOP values.</param>
    /// <param name="connection">The safe accepted-connection context.</param>
    /// <param name="cancellationToken">A token that can cancel provider work.</param>
    /// <returns>The classified authentication result.</returns>
    Pop3AuthenticationResult AuthenticateApop(Pop3ApopRequest request, Pop3ServerConnection connection, System.Threading.CancellationToken cancellationToken);

    /// <summary>Classifies a decoded built-in SASL PLAIN authentication attempt.</summary>
    /// <param name="credentials">The decoded PLAIN credentials.</param>
    /// <param name="connection">The safe accepted-connection context.</param>
    /// <param name="cancellationToken">A token that can cancel provider work.</param>
    /// <returns>The classified authentication result.</returns>
    Pop3AuthenticationResult AuthenticateSaslPlain(Pop3Credentials credentials, Pop3ServerConnection connection, System.Threading.CancellationToken cancellationToken);
}
