namespace pop3;

/// <summary>Writes POP3 protocol commands and responses to a transport.</summary>
public interface IPop3WireWriter
{
    /// <summary>Writes one POP3 command line.</summary>
    /// <param name="command">The command to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted command-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    void WriteCommand(Pop3Command command, int maximumLineOctets, System.Threading.CancellationToken cancellationToken);

    /// <summary>Writes one POP3 response line.</summary>
    /// <param name="response">The response to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted response-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    void WriteResponse(Pop3Response response, int maximumLineOctets, System.Threading.CancellationToken cancellationToken);

    /// <summary>Writes a dot-terminated multiline CAPA response.</summary>
    /// <param name="capabilities">The ordered capabilities to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted capability-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    void WriteCapabilities(Pop3Capability[] capabilities, int maximumLineOctets, System.Threading.CancellationToken cancellationToken);

    /// <summary>Copies, normalizes, and dot-stuffs caller-provided multiline data.</summary>
    /// <param name="source">The source stream, which remains caller-owned.</param>
    /// <param name="maximumLineOctets">The maximum permitted source data-line size.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking copy.</param>
    /// <returns>The number of unstuffed transmitted-representation octets consumed.</returns>
    long WriteMultilineData(System.IO.Stream source, int maximumLineOctets, System.Threading.CancellationToken cancellationToken);
}
