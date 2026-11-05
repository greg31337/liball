namespace pop3;

/// <summary>Reads and parses POP3 wire data from a caller-owned transport stream.</summary>
public class Pop3WireReader : IPop3WireReader
{
    internal Pop3LineReader LineReader;
    internal Pop3MultilineReader MultilineReader;
    internal Pop3CommandParser CommandParser = new Pop3CommandParser();
    internal Pop3ResponseParser ResponseParser = new Pop3ResponseParser();
    internal bool IsFirstResponse = true;

    /// <summary>Creates a reader over a caller-owned readable stream.</summary>
    /// <param name="stream">The readable POP3 transport stream.</param>
    /// <param name="bufferSize">The positive internal transport buffer size.</param>
    public Pop3WireReader(System.IO.Stream stream, int bufferSize)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The POP3 wire reader stream must be readable.", "stream");
        }

        Pop3BufferedInput input = new Pop3BufferedInput(stream, bufferSize, bufferSize);
        LineReader = new Pop3LineReader(input);
        MultilineReader = new Pop3MultilineReader(LineReader);
    }

    /// <summary>Reads one POP3 command line.</summary>
    /// <param name="maximumLineOctets">The maximum permitted command-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking read.</param>
    /// <returns>The parsed command.</returns>
    public Pop3Command ReadCommand(int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        byte[]? line = LineReader.ReadDataLine(maximumLineOctets, cancellationToken);
        if (line == null)
        {
            throw new System.IO.EndOfStreamException("The POP3 stream ended before a command was available.");
        }
        return CommandParser.ParseCommand(line);
    }

    /// <summary>Reads one POP3 response line.</summary>
    /// <param name="maximumLineOctets">The maximum permitted response-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking read.</param>
    /// <returns>The parsed response.</returns>
    public Pop3Response ReadResponse(int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        byte[]? line = LineReader.ReadDataLine(maximumLineOctets, cancellationToken);
        if (line == null)
        {
            throw new System.IO.EndOfStreamException("The POP3 stream ended before a response was available.");
        }

        Pop3Response response = ResponseParser.ParseResponse(line, IsFirstResponse, maximumLineOctets);
        IsFirstResponse = false;
        return response;
    }

    /// <summary>Copies and dot-unstuffs one multiline response to a caller-owned destination.</summary>
    /// <param name="destination">The destination stream, which remains caller-owned.</param>
    /// <param name="maximumLineOctets">The maximum permitted unbounded data-line size.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking copy.</param>
    /// <returns>The number of unstuffed payload octets written.</returns>
    public long CopyMultilineResponse(
        System.IO.Stream destination,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        return MultilineReader.CopyMultilineData(
            destination,
            maximumLineOctets,
            null,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            cancellationToken);
    }

    internal void DiscardBufferedInput()
    {
        LineReader.DiscardBufferedInput();
    }

    internal Pop3ServerCommandLine ReadServerCommandLine(
        System.Threading.CancellationToken cancellationToken)
    {
        return LineReader.ReadServerCommandLine(cancellationToken);
    }
}
