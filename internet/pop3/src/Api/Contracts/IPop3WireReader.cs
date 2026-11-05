namespace pop3;

/// <summary>Reads POP3 protocol responses from a transport.</summary>
public interface IPop3WireReader
{
    /// <summary>Reads one POP3 command line.</summary>
    /// <param name="maximumLineOctets">The maximum permitted command-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking read.</param>
    /// <returns>The parsed command.</returns>
    Pop3Command ReadCommand(int maximumLineOctets, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads one POP3 response line.</summary>
    /// <param name="maximumLineOctets">The maximum permitted response-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking read.</param>
    /// <returns>The parsed response.</returns>
    Pop3Response ReadResponse(int maximumLineOctets, System.Threading.CancellationToken cancellationToken);

    /// <summary>Copies and dot-unstuffs one multiline response to a caller-owned destination.</summary>
    /// <param name="destination">The destination stream, which remains caller-owned.</param>
    /// <param name="maximumLineOctets">The maximum permitted unbounded data-line size.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking copy.</param>
    /// <returns>The number of unstuffed payload octets written.</returns>
    long CopyMultilineResponse(System.IO.Stream destination, int maximumLineOctets, System.Threading.CancellationToken cancellationToken);
}
