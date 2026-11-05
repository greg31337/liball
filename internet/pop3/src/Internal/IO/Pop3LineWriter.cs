namespace pop3;

internal class Pop3LineWriter
{
    Pop3BufferedOutput Output;

    internal Pop3LineWriter(Pop3BufferedOutput output)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        Output = output;
    }

    internal void WriteCommandLine(byte[] content, System.Threading.CancellationToken cancellationToken)
    {
        WriteBoundedLine(content, Pop3Defaults.MaximumCommandLineOctets, cancellationToken);
    }

    internal void WriteResponseLine(byte[] content, System.Threading.CancellationToken cancellationToken)
    {
        WriteBoundedLine(content, Pop3Defaults.MaximumResponseLineOctets, cancellationToken);
    }

    internal void WriteCapabilityLine(byte[] content, System.Threading.CancellationToken cancellationToken)
    {
        WriteBoundedLine(content, Pop3Defaults.MaximumResponseLineOctets, cancellationToken);
    }

    internal void WriteDataLine(byte[] content, int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        WriteBoundedLine(content, maximumLineOctets, cancellationToken);
    }

    void WriteBoundedLine(byte[] content, int maximumLineOctets, System.Threading.CancellationToken cancellationToken)
    {
        if (content == null)
        {
            throw new System.ArgumentNullException("content");
        }
        if (maximumLineOctets < 2)
        {
            throw new System.ArgumentOutOfRangeException("maximumLineOctets");
        }
        if (content.Length > maximumLineOctets - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 line exceeds its permitted octet count.");
        }

        int index = 0;
        while (index < content.Length)
        {
            if (content[index] == 13 || content[index] == 10)
            {
                throw new System.IO.InvalidDataException("POP3 line content cannot contain a line terminator.");
            }
            index++;
        }

        Output.WriteBytes(content, 0, content.Length, cancellationToken);
        Output.WriteByteValue(13, cancellationToken);
        Output.WriteByteValue(10, cancellationToken);
        Output.FlushOutput(cancellationToken);
    }
}
