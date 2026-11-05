namespace pop3;

/// <summary>Serves caller-supplied accepted POP3 connections.</summary>
public interface IPop3Server
{
    /// <summary>Serves one accepted connection using the supplied server configuration.</summary>
    /// <param name="connection">The accepted connection.</param>
    /// <param name="options">The listener-neutral server configuration.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking server session.</param>
    void ServeConnection(Pop3ServerConnection connection, Pop3ServerOptions options, System.Threading.CancellationToken cancellationToken);
}
