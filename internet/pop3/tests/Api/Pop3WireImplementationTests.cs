using System.Text;

namespace pop3.Tests;

public class Pop3WireImplementationTests
{
    [Test]
    public void ReaderParsesGreetingResponsesAndMultilineDataAcrossFragments()
    {
        byte[] wire = Encoding.ASCII.GetBytes(
            "+OK ready <123@example.test>\r\n" +
            "-ERR [SYS/TEMP] mailbox busy\r\n" +
            "..dot\r\n" +
            ".\r\n");
        FragmentedReadStream transport = new FragmentedReadStream(wire, 1);
        Pop3WireReader reader = new Pop3WireReader(transport, 3);

        Pop3Response greeting = reader.ReadResponse(512, CancellationToken.None);
        Pop3Response failure = reader.ReadResponse(512, CancellationToken.None);
        MemoryStream destination = new MemoryStream();
        long copied = reader.CopyMultilineResponse(destination, 64, CancellationToken.None);

        Assert.That(greeting.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(greeting.GreetingTimestamp, Is.EqualTo("<123@example.test>"));
        Assert.That(failure.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(failure.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(failure.Text, Is.EqualTo("[SYS/TEMP] mailbox busy"));
        Assert.That(copied, Is.EqualTo(6));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(".dot\r\n"));
        Assert.That(destination.CanWrite, Is.True);
    }

    [Test]
    public void ReaderParsesCommandsUsingTheCallerLineLimit()
    {
        Pop3WireReader reader = new Pop3WireReader(
            new FragmentedReadStream(Encoding.ASCII.GetBytes("uSeR alice\r\nNOOP\r\n"), 2),
            4);

        Pop3Command command = reader.ReadCommand(255, CancellationToken.None);
        Pop3Command next = reader.ReadCommand(6, CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "alice" }));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Noop));
    }

    [Test]
    public void ReaderSupportsLargeSaslLinesWithoutRelaxingNormalResponseLimits()
    {
        string challenge = new string('A', 600);
        Pop3WireReader saslReader = new Pop3WireReader(
            new MemoryStream(Encoding.ASCII.GetBytes("+ " + challenge + "\r\n")),
            8);
        Pop3WireReader normalReader = new Pop3WireReader(
            new MemoryStream(Encoding.ASCII.GetBytes("+OK " + new string('a', 507) + "\r\n")),
            8);

        Assert.That(saslReader.ReadResponse(604, CancellationToken.None).Text, Is.EqualTo(challenge));
        Assert.Throws<InvalidDataException>(() => normalReader.ReadResponse(600, CancellationToken.None));
    }

    [Test]
    public void ReaderReportsMissingAndOverLimitProtocolLines()
    {
        Pop3WireReader emptyCommand = new Pop3WireReader(new MemoryStream(), 2);
        Pop3WireReader emptyResponse = new Pop3WireReader(new MemoryStream(), 2);
        Pop3WireReader limited = new Pop3WireReader(
            new MemoryStream(Encoding.ASCII.GetBytes("USER alice\r\n")),
            2);

        Assert.Throws<EndOfStreamException>(() => emptyCommand.ReadCommand(255, CancellationToken.None));
        Assert.Throws<EndOfStreamException>(() => emptyResponse.ReadResponse(512, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => limited.ReadCommand(8, CancellationToken.None));
    }

    [Test]
    public void WriterEmitsCommandsResponsesCapabilitiesAndMultilineData()
    {
        MemoryStream transport = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(transport, 3);
        Pop3Command command = new Pop3Command();
        command.Keyword = "user";
        command.Arguments = new[] { "alice" };
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "denied";
        response.ResponseCodeLevels = new[] { "auth" };
        Pop3Capability[] capabilities = new[]
        {
            Capability("top"),
            Capability("sasl", "PLAIN"),
            Capability("X-Custom", "one"),
            Capability("X-Custom", "two")
        };
        MemoryStream payload = new MemoryStream(Encoding.ASCII.GetBytes("a\n.dot"));

        writer.WriteCommand(command, 255, CancellationToken.None);
        writer.WriteResponse(response, 512, CancellationToken.None);
        writer.WriteCapabilities(capabilities, 512, CancellationToken.None);
        long transmitted = writer.WriteMultilineData(payload, 64, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(9));
        Assert.That(
            Encoding.ASCII.GetString(transport.ToArray()),
            Is.EqualTo(
                "USER alice\r\n" +
                "-ERR [AUTH] denied\r\n" +
                "TOP\r\n" +
                "SASL PLAIN\r\n" +
                "X-Custom one\r\n" +
                "X-Custom two\r\n" +
                ".\r\n" +
                "a\r\n" +
                "..dot\r\n" +
                ".\r\n"));
        Assert.That(payload.CanRead, Is.True);
        Assert.That(transport.CanWrite, Is.True);
    }

    [Test]
    public void WriterAndReaderRoundTripPublicWireObjects()
    {
        MemoryStream transport = new MemoryStream();
        Pop3Command source = new Pop3Command();
        source.Keyword = Pop3Commands.Top;
        source.Arguments = new[] { "3", "10" };
        new Pop3WireWriter(transport, 2).WriteCommand(source, 255, CancellationToken.None);
        transport.Position = 0;

        Pop3Command parsed = new Pop3WireReader(transport, 2).ReadCommand(255, CancellationToken.None);

        Assert.That(parsed.Keyword, Is.EqualTo(Pop3Commands.Top));
        Assert.That(parsed.Arguments, Is.EqualTo(new[] { "3", "10" }));
        Assert.That(parsed.ExpectsMultilineResponse, Is.True);
    }

    [Test]
    public void CapabilityWriterPrevalidatesTheCompleteListBeforeWriting()
    {
        MemoryStream transport = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(transport, 2);
        Pop3Capability[] capabilities = new[]
        {
            Capability("TOP"),
            Capability("USER", "invalid")
        };

        Assert.Throws<InvalidDataException>(() => writer.WriteCapabilities(
            capabilities, 512, CancellationToken.None));
        Assert.That(transport.ToArray(), Is.Empty);
    }

    [Test]
    public void WriterAppliesCallerLimitsAndProtocolLimitsBeforeWritingLines()
    {
        MemoryStream commandTransport = new MemoryStream();
        MemoryStream responseTransport = new MemoryStream();
        MemoryStream capabilityTransport = new MemoryStream();
        Pop3WireWriter commandWriter = new Pop3WireWriter(commandTransport, 2);
        Pop3WireWriter responseWriter = new Pop3WireWriter(responseTransport, 2);
        Pop3WireWriter capabilityWriter = new Pop3WireWriter(capabilityTransport, 2);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.User;
        command.Arguments = new[] { "alice" };
        Pop3Response response = new Pop3Response();
        response.Text = new string('a', 507);

        Assert.Throws<InvalidDataException>(() => commandWriter.WriteCommand(command, 11, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => responseWriter.WriteResponse(response, 600, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => capabilityWriter.WriteCapabilities(
            new[] { Capability("SASL", "PLAIN") }, 11, CancellationToken.None));
        Assert.That(commandTransport.ToArray(), Is.Empty);
        Assert.That(responseTransport.ToArray(), Is.Empty);
        Assert.That(capabilityTransport.ToArray(), Is.Empty);
    }

    [Test]
    public void WriterHandlesSmallTransportWriteWindows()
    {
        MaximumWriteStream transport = new MaximumWriteStream(2);
        Pop3WireWriter writer = new Pop3WireWriter(transport, 2);
        Pop3Response response = new Pop3Response();
        response.Text = "ready";

        writer.WriteResponse(response, 512, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(transport.ToArray()), Is.EqualTo("+OK ready\r\n"));
        Assert.That(transport.WriteCallCount, Is.GreaterThan(1));
    }

    [Test]
    public void PublicWireOperationsHonorPreCanceledTokens()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FragmentedReadStream input = new FragmentedReadStream(Encoding.ASCII.GetBytes("NOOP\r\n"), 6);
        MemoryStream output = new MemoryStream();
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Noop;

        Assert.Throws<OperationCanceledException>(() => new Pop3WireReader(input, 2).ReadCommand(
            255, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => new Pop3WireWriter(output, 2).WriteCommand(
            command, 255, cancellation.Token));
        Assert.That(input.ReadCallCount, Is.Zero);
        Assert.That(output.ToArray(), Is.Empty);
    }

    [Test]
    public void ConstructorsRejectInvalidStreamsAndBufferSizes()
    {
        MemoryStream readable = new MemoryStream();
        MaximumWriteStream writable = new MaximumWriteStream(4);

        Assert.Throws<ArgumentNullException>(() => new Pop3WireReader(null!, 1));
        Assert.Throws<ArgumentNullException>(() => new Pop3WireWriter(null!, 1));
        Assert.Throws<ArgumentException>(() => new Pop3WireReader(writable, 1));
        Assert.Throws<ArgumentException>(() => new Pop3WireWriter(new MemoryStream(new byte[0], false), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3WireReader(readable, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3WireWriter(new MemoryStream(), 0));
    }

    [Test]
    public void CapabilityWritersRejectNullCollectionsAndImpossibleLineLimits()
    {
        Pop3WireWriter writer = new Pop3WireWriter(new MemoryStream(), 2);

        Assert.Throws<ArgumentNullException>(() => writer.WriteCapabilities(
            null!, 512, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteCapabilities(
            Array.Empty<Pop3Capability>(), 2, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => writer.WriteServerCapabilities(
            null!, Pop3SessionState.Authorization, 512, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteServerCapabilities(
            Array.Empty<Pop3Capability>(),
            Pop3SessionState.Authorization,
            2,
            CancellationToken.None));
    }

    [Test]
    public void ServerMultilineWritersPrevalidateCollectionsLimitsAndLineLengths()
    {
        MemoryStream transport = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(transport, 2);
        Pop3Capability longCapability = Capability("X-LONG", new string('a', 20));
        Pop3MessageInfo longMessage = new Pop3MessageInfo();
        longMessage.MessageNumber = int.MaxValue;
        longMessage.OctetCount = long.MaxValue;
        Pop3UniqueId longUniqueId = new Pop3UniqueId();
        longUniqueId.MessageNumber = int.MaxValue;
        longUniqueId.Value = new string('u', 70);
        Pop3Language longLanguage = new Pop3Language();
        longLanguage.Tag = "en";
        longLanguage.Description = new string('d', 20);

        Assert.Throws<InvalidDataException>(() => writer.WriteServerCapabilities(
            new[] { longCapability },
            Pop3SessionState.Authorization,
            8,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => writer.WriteServerListEntries(
            null!, 512, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteServerListEntries(
            Array.Empty<Pop3MessageInfo>(), 2, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteServerListEntries(
            new[] { longMessage }, 8, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => writer.WriteServerUniqueIdEntries(
            null!, 512, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteServerUniqueIdEntries(
            Array.Empty<Pop3UniqueId>(), 2, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteServerUniqueIdEntries(
            new[] { longUniqueId }, 8, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => writer.WriteServerLanguageEntries(
            null!, 512, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteServerLanguageEntries(
            Array.Empty<Pop3Language>(), 2, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteServerLanguageEntries(
            new[] { longLanguage }, 8, CancellationToken.None));
        Assert.That(transport.ToArray(), Is.Empty);
    }

    [Test]
    public void ServerLanguageWriterRejectsEachMissingMetadataFieldBeforeWriting()
    {
        MemoryStream transport = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(transport, 2);
        Pop3Language missingTag = new Pop3Language();
        missingTag.Tag = null!;
        missingTag.Description = "English";
        Pop3Language missingDescription = new Pop3Language();
        missingDescription.Tag = "en";
        missingDescription.Description = null!;

        Assert.Throws<InvalidDataException>(() => writer.WriteServerLanguageEntries(
            new Pop3Language[] { null! }, 512, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteServerLanguageEntries(
            new[] { missingTag }, 512, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteServerLanguageEntries(
            new[] { missingDescription }, 512, CancellationToken.None));
        Assert.That(transport.ToArray(), Is.Empty);
    }

    static Pop3Capability Capability(string name, params string[] parameters)
    {
        Pop3Capability capability = new Pop3Capability();
        capability.Name = name;
        capability.Parameters = parameters;
        return capability;
    }

    private class FragmentedReadStream : Stream
    {
        byte[] Data;
        int MaximumReadSize;
        int PositionValue;
        public int ReadCallCount;

        public FragmentedReadStream(byte[] data, int maximumReadSize)
        {
            Data = data;
            MaximumReadSize = maximumReadSize;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return Data.Length; }
        }

        public override long Position
        {
            get { return PositionValue; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            int remaining = Data.Length - PositionValue;
            int readCount = Math.Min(count, Math.Min(MaximumReadSize, remaining));
            Array.Copy(Data, PositionValue, buffer, offset, readCount);
            PositionValue += readCount;
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    private class MaximumWriteStream : Stream
    {
        MemoryStream Destination = new MemoryStream();
        int MaximumWriteSize;
        public int WriteCallCount;

        public MaximumWriteStream(int maximumWriteSize)
        {
            MaximumWriteSize = maximumWriteSize;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return Destination.Length; }
        }

        public override long Position
        {
            get { return Destination.Position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > MaximumWriteSize)
            {
                throw new InvalidOperationException("write exceeded configured fragment size");
            }
            WriteCallCount++;
            Destination.Write(buffer, offset, count);
        }

        public byte[] ToArray()
        {
            return Destination.ToArray();
        }
    }
}
