namespace pop3;

/// <summary>Creates connection-scoped server sessions for one SASL mechanism.</summary>
public interface IPop3SaslServerFactory
{
    /// <summary>Gets the advertised SASL mechanism name.</summary>
    /// <returns>The mechanism name.</returns>
    string GetMechanism();

    /// <summary>Creates a SASL server session for an accepted connection.</summary>
    /// <param name="connection">The accepted connection context.</param>
    /// <returns>A new connection-scoped SASL server session.</returns>
    IPop3SaslServerSession CreateSession(Pop3ServerConnection connection);
}
