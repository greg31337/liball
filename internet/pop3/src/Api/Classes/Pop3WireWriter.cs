namespace pop3;

/// <summary>Formats and writes POP3 wire data to a caller-owned transport stream.</summary>
public class Pop3WireWriter : IPop3WireWriter
{
    internal Pop3LineWriter LineWriter;
    internal Pop3MultilineWriter MultilineWriter;
    internal Pop3TopContentWriter TopContentWriter;
    internal Pop3CommandParser CommandParser = new Pop3CommandParser();
    internal Pop3ResponseParser ResponseParser = new Pop3ResponseParser();
    internal Pop3CapabilityParser CapabilityParser = new Pop3CapabilityParser();

    /// <summary>Creates a writer over a caller-owned writable stream.</summary>
    /// <param name="stream">The writable POP3 transport stream.</param>
    /// <param name="bufferSize">The positive internal transport buffer size.</param>
    public Pop3WireWriter(System.IO.Stream stream, int bufferSize)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("The POP3 wire writer stream must be writable.", "stream");
        }

        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, bufferSize, bufferSize);
        LineWriter = new Pop3LineWriter(output);
        MultilineWriter = new Pop3MultilineWriter(LineWriter);
        TopContentWriter = new Pop3TopContentWriter(LineWriter);
    }

    /// <summary>Writes one POP3 command line.</summary>
    /// <param name="command">The command to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted command-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    public void WriteCommand(
        Pop3Command command,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] line = CommandParser.FormatCommand(command);
        LineWriter.WriteDataLine(line, maximumLineOctets, cancellationToken);
    }

    /// <summary>Writes one POP3 response line.</summary>
    /// <param name="response">The response to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted response-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    public void WriteResponse(
        Pop3Response response,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] line = ResponseParser.FormatResponse(response, maximumLineOctets);
        LineWriter.WriteDataLine(line, maximumLineOctets, cancellationToken);
    }

    /// <summary>Writes a dot-terminated multiline CAPA response.</summary>
    /// <param name="capabilities">The ordered capabilities to write.</param>
    /// <param name="maximumLineOctets">The maximum permitted capability-line size including CRLF.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking write.</param>
    public void WriteCapabilities(
        Pop3Capability[] capabilities,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();

        byte[][] lines = new byte[capabilities.Length][];
        int index = 0;
        while (index < capabilities.Length)
        {
            lines[index] = CapabilityParser.FormatCapability(capabilities[index], Pop3SessionState.Authorization);
            if (lines[index].Length > maximumLineOctets - 2)
            {
                throw new System.IO.InvalidDataException("A POP3 capability line exceeds its configured octet limit.");
            }
            index++;
        }

        index = 0;
        while (index < lines.Length)
        {
            LineWriter.WriteDataLine(lines[index], maximumLineOctets, cancellationToken);
            index++;
        }
        LineWriter.WriteDataLine(new byte[] { 46 }, maximumLineOctets, cancellationToken);
    }

    internal void WriteServerCapabilities(
        Pop3Capability[] capabilities,
        Pop3SessionState state,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (capabilities == null)
        {
            throw new System.ArgumentNullException("capabilities");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();

        byte[][] lines = new byte[capabilities.Length][];
        int index = 0;
        while (index < capabilities.Length)
        {
            lines[index] = CapabilityParser.FormatCapability(
                capabilities[index],
                state);
            if (lines[index].Length > maximumLineOctets - 2)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 capability line exceeds its configured octet limit.");
            }
            index++;
        }

        index = 0;
        while (index < lines.Length)
        {
            LineWriter.WriteDataLine(
                lines[index],
                maximumLineOctets,
                cancellationToken);
            index++;
        }
        LineWriter.WriteDataLine(
            new byte[] { 46 },
            maximumLineOctets,
            cancellationToken);
    }

    internal void WriteServerListEntries(
        Pop3MessageInfo[] messages,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (messages == null)
        {
            throw new System.ArgumentNullException("messages");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();

        byte[][] lines = new byte[messages.Length][];
        int index = 0;
        while (index < messages.Length)
        {
            Pop3MessageInfo message = messages[index];
            if (message == null || message.MessageNumber < 1 ||
                message.OctetCount < 0 || message.IsDeleted)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 LIST response contains invalid message metadata.");
            }
            string line = message.MessageNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                " " +
                message.OctetCount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            lines[index] = System.Text.Encoding.ASCII.GetBytes(line);
            if (lines[index].Length > maximumLineOctets - 2)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 LIST response line exceeds its configured octet limit.");
            }
            index++;
        }

        index = 0;
        while (index < lines.Length)
        {
            LineWriter.WriteDataLine(
                lines[index],
                maximumLineOctets,
                cancellationToken);
            index++;
        }
        LineWriter.WriteDataLine(
            new byte[] { 46 },
            maximumLineOctets,
            cancellationToken);
    }

    internal void WriteServerUniqueIdEntries(
        Pop3UniqueId[] uniqueIds,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (uniqueIds == null)
        {
            throw new System.ArgumentNullException("uniqueIds");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();

        byte[][] lines = new byte[uniqueIds.Length][];
        int index = 0;
        while (index < uniqueIds.Length)
        {
            Pop3UniqueId uniqueId = uniqueIds[index];
            if (uniqueId == null || uniqueId.MessageNumber < 1 ||
                !IsValidServerUniqueId(uniqueId.Value))
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 UIDL response contains an invalid unique identifier.");
            }
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (uniqueIds[previousIndex].MessageNumber ==
                        uniqueId.MessageNumber ||
                    string.Equals(
                        uniqueIds[previousIndex].Value,
                        uniqueId.Value,
                        System.StringComparison.Ordinal))
                {
                    throw new System.IO.InvalidDataException(
                        "A POP3 UIDL response contains duplicate values.");
                }
                previousIndex++;
            }
            string line = uniqueId.MessageNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                " " + uniqueId.Value;
            lines[index] = System.Text.Encoding.ASCII.GetBytes(line);
            if (lines[index].Length > maximumLineOctets - 2)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 UIDL response line exceeds its configured octet limit.");
            }
            index++;
        }

        index = 0;
        while (index < lines.Length)
        {
            LineWriter.WriteDataLine(
                lines[index],
                maximumLineOctets,
                cancellationToken);
            index++;
        }
        LineWriter.WriteDataLine(
            new byte[] { 46 },
            maximumLineOctets,
            cancellationToken);
    }

    internal void WriteServerLanguageEntries(
        Pop3Language[] languages,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        if (languages == null)
        {
            throw new System.ArgumentNullException("languages");
        }
        if (maximumLineOctets < 3)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        cancellationToken.ThrowIfCancellationRequested();

        byte[][] lines = new byte[languages.Length][];
        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, true);
        int index = 0;
        while (index < languages.Length)
        {
            Pop3Language language = languages[index];
            if (language == null || language.Tag == null ||
                language.Description == null)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 LANG response contains invalid language metadata.");
            }
            string line = language.Tag;
            if (language.Description.Length > 0)
            {
                line += " " + language.Description;
            }
            lines[index] = encoding.GetBytes(line);
            if (lines[index].Length > maximumLineOctets - 2)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 LANG response line exceeds its configured octet limit.");
            }
            index++;
        }

        index = 0;
        while (index < lines.Length)
        {
            LineWriter.WriteDataLine(
                lines[index],
                maximumLineOctets,
                cancellationToken);
            index++;
        }
        LineWriter.WriteDataLine(
            new byte[] { 46 },
            maximumLineOctets,
            cancellationToken);
    }

    bool IsValidServerUniqueId(string value)
    {
        if (value == null || value.Length < 1 || value.Length > 70)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 33 || value[index] > 126)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal long WriteServerMessageContent(
        System.IO.Stream source,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        int messageNumber,
        long expectedBytes,
        bool requireUtf8,
        System.Threading.CancellationToken cancellationToken)
    {
        return MultilineWriter.WriteObservedMultilineContent(
            source,
            maximumLineOctets,
            observer,
            Pop3TransferKind.Retrieve,
            messageNumber,
            expectedBytes,
            requireUtf8,
            cancellationToken);
    }

    internal void WriteServerMessageTerminator(
        IPop3TransferObserver? observer,
        int messageNumber,
        long transmittedOctets,
        long expectedBytes,
        System.Threading.CancellationToken cancellationToken)
    {
        MultilineWriter.WriteObservedMultilineTerminator(
            observer,
            Pop3TransferKind.Retrieve,
            messageNumber,
            transmittedOctets,
            expectedBytes,
            cancellationToken);
    }

    internal long WriteServerTopContent(
        System.IO.Stream source,
        int bodyLineCount,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        int messageNumber,
        bool requireUtf8,
        System.Threading.CancellationToken cancellationToken)
    {
        return TopContentWriter.WriteTopContent(
            source,
            bodyLineCount,
            maximumLineOctets,
            observer,
            messageNumber,
            requireUtf8,
            cancellationToken);
    }

    internal void WriteServerTopTerminator(
        IPop3TransferObserver? observer,
        int messageNumber,
        long transmittedOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        MultilineWriter.WriteObservedMultilineTerminator(
            observer,
            Pop3TransferKind.Top,
            messageNumber,
            transmittedOctets,
            -1,
            cancellationToken);
    }

    internal long WriteServerExtensionData(
        System.IO.Stream source,
        int maximumLineOctets,
        IPop3TransferObserver? observer,
        System.Threading.CancellationToken cancellationToken)
    {
        return MultilineWriter.WriteObservedMultilineData(
            source,
            maximumLineOctets,
            observer,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            cancellationToken);
    }

    /// <summary>Copies, normalizes, and dot-stuffs caller-provided multiline data.</summary>
    /// <param name="source">The source stream, which remains caller-owned.</param>
    /// <param name="maximumLineOctets">The maximum permitted source data-line size.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking copy.</param>
    /// <returns>The number of unstuffed transmitted-representation octets consumed.</returns>
    public long WriteMultilineData(
        System.IO.Stream source,
        int maximumLineOctets,
        System.Threading.CancellationToken cancellationToken)
    {
        return MultilineWriter.WriteMultilineData(source, maximumLineOctets, cancellationToken);
    }
}
