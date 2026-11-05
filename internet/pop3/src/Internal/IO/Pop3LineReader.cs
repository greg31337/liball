namespace pop3;

internal class Pop3ServerCommandLine
{
    internal byte[] Content = new byte[0];
    internal bool EndOfStream;
    internal bool IsValid = true;
    internal string ErrorText = "";
}

internal class Pop3LineReader
{
    Pop3BufferedInput Input;

    internal Pop3LineReader(Pop3BufferedInput input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        Input = input;
    }

    internal byte[]? ReadCommandLine(System.Threading.CancellationToken cancellationToken)
    {
        return ReadBoundedLine(Pop3Defaults.MaximumCommandLineOctets, cancellationToken);
    }

    internal Pop3ServerCommandLine ReadServerCommandLine(
        System.Threading.CancellationToken cancellationToken)
    {
        byte[] content = new byte[Pop3Defaults.MaximumCommandLineOctets - 2];
        int contentLength = 0;
        bool isValid = true;
        string errorText = "";
        while (true)
        {
            int value = Input.ReadByteValue(cancellationToken);
            if (value < 0)
            {
                if (contentLength == 0 && isValid)
                {
                    Pop3ServerCommandLine end = new Pop3ServerCommandLine();
                    end.EndOfStream = true;
                    return end;
                }
                throw new System.IO.EndOfStreamException(
                    "The POP3 stream ended before the command-line terminator.");
            }

            if (value == 13)
            {
                int nextValue = Input.ReadByteValue(cancellationToken);
                if (nextValue < 0)
                {
                    throw new System.IO.EndOfStreamException(
                        "The POP3 stream ended after a command-line carriage return.");
                }
                if (nextValue == 10)
                {
                    return CreateServerCommandLine(
                        content,
                        contentLength,
                        isValid,
                        errorText);
                }
                throw new System.IO.InvalidDataException(
                    "A POP3 command line contains a bare carriage return.");
            }

            if (value == 10)
            {
                throw new System.IO.InvalidDataException(
                    "A POP3 command line contains a bare line feed.");
            }
            if (contentLength == content.Length)
            {
                isValid = false;
                errorText = "The command exceeds 255 octets including CRLF.";
                continue;
            }
            if (isValid)
            {
                content[contentLength] = (byte)value;
                contentLength++;
            }
        }
    }

    internal byte[]? ReadResponseLine(System.Threading.CancellationToken cancellationToken)
    {
        return ReadBoundedLine(Pop3Defaults.MaximumResponseLineOctets, cancellationToken);
    }

    internal byte[]? ReadCapabilityLine(System.Threading.CancellationToken cancellationToken)
    {
        return ReadBoundedLine(Pop3Defaults.MaximumResponseLineOctets, cancellationToken);
    }

    internal byte[]? ReadDataLine(int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        return ReadBoundedLine(maximumLineOctets, cancellationToken);
    }

    internal void DiscardBufferedInput()
    {
        Input.DiscardBufferedBytes();
    }

    byte[]? ReadBoundedLine(int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        if (maximumLineOctets < 2)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }

        byte[] content = new byte[maximumLineOctets - 2];
        int contentLength = 0;
        while (true)
        {
            int value = Input.ReadByteValue(cancellationToken);
            if (value < 0)
            {
                if (contentLength == 0)
                {
                    return null;
                }
                throw new System.IO.EndOfStreamException("The POP3 stream ended before the line terminator.");
            }

            if (value == 13)
            {
                int nextValue = Input.ReadByteValue(cancellationToken);
                if (nextValue < 0)
                {
                    throw new System.IO.EndOfStreamException("The POP3 stream ended after a carriage return.");
                }
                if (nextValue != 10)
                {
                    throw new System.IO.InvalidDataException("A POP3 line contains a bare carriage return.");
                }
                return CopyContent(content, contentLength);
            }

            if (value == 10)
            {
                throw new System.IO.InvalidDataException("A POP3 line contains a bare line feed.");
            }
            if (contentLength == content.Length)
            {
                throw new System.IO.InvalidDataException("A POP3 line exceeds its permitted octet count.");
            }

            content[contentLength] = (byte)value;
            contentLength++;
        }
    }

    byte[] CopyContent(byte[] content, int contentLength)
    {
        byte[] result = new byte[contentLength];
        System.Array.Copy(content, 0, result, 0, contentLength);
        return result;
    }

    Pop3ServerCommandLine CreateServerCommandLine(
        byte[] content,
        int contentLength,
        bool isValid,
        string errorText)
    {
        Pop3ServerCommandLine line = new Pop3ServerCommandLine();
        line.Content = CopyContent(content, contentLength);
        line.IsValid = isValid;
        line.ErrorText = errorText;
        return line;
    }
}
