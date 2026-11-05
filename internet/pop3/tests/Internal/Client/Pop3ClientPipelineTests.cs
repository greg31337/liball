using System.Text;

namespace pop3.Tests;

public class Pop3ClientPipelineTests
{
    [Test]
    public void PolicyRequiresOptInAndAdvertisementUnlessForced()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3ClientOptions options = new Pop3ClientOptions();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();

        capabilities.SupportsPipelining = true;
        Assert.That(pipeline.IsEnabled(options, capabilities), Is.False);
        options.EnablePipelining = true;
        capabilities.SupportsPipelining = false;
        Assert.That(pipeline.IsEnabled(options, capabilities), Is.False);
        capabilities.SupportsPipelining = true;
        Assert.That(pipeline.IsEnabled(options, capabilities), Is.True);
        options.EnablePipelining = false;
        options.ForcePipelining = true;
        capabilities.SupportsPipelining = false;
        Assert.That(pipeline.IsEnabled(options, capabilities), Is.True);
        Assert.Throws<ArgumentNullException>(() => pipeline.IsEnabled(null!, capabilities));
        Assert.Throws<ArgumentNullException>(() => pipeline.IsEnabled(options, null!));
        stream.Dispose();
    }

    [Test]
    public void SafetyClassificationAllowsKnownIndependentCommandsOnly()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3Command[] safe = new[]
        {
            Command(Pop3Commands.Stat, new string[0], false),
            Command(Pop3Commands.List, new[] { "1" }, false),
            Command(Pop3Commands.List, new string[0], true),
            Command(Pop3Commands.Uidl, new[] { "1" }, false),
            Command(Pop3Commands.Retr, new[] { "1" }, true),
            Command(Pop3Commands.Top, new[] { "1", "0" }, true),
            Command(Pop3Commands.Dele, new[] { "1" }, false),
            Command(Pop3Commands.Noop, new string[0], false),
            Command(Pop3Commands.Capa, new string[0], true),
            Command(Pop3Commands.Lang, new string[0], true)
        };
        Pop3Command[] barriers = new[]
        {
            Command(Pop3Commands.User, new[] { "alice" }, false),
            Command(Pop3Commands.Pass, new[] { "secret" }, false),
            Command(Pop3Commands.Apop, new[] { "alice", "digest" }, false),
            Command(Pop3Commands.Auth, new[] { "PLAIN" }, false),
            Command(Pop3Commands.Stls, new string[0], false),
            Command(Pop3Commands.Utf8, new string[0], false),
            Command(Pop3Commands.Rset, new string[0], false),
            Command(Pop3Commands.Quit, new string[0], false),
            Command(Pop3Commands.Lang, new[] { "en" }, false),
            Command("XONE", new string[0], false)
        };

        Assert.That(safe, Is.All.Matches<Pop3Command>(pipeline.IsPipelineSafe));
        Assert.That(barriers, Is.All.Matches<Pop3Command>(command => !pipeline.IsPipelineSafe(command)));
        Assert.That(pipeline.IsPipelineSafe(null!), Is.False);
        stream.Dispose();
    }

    [Test]
    public void WritesEveryCommandBeforeReadingOrderedMixedResponses()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream(
            "+OK 3 100\r\n" +
            "+OK message follows\r\n" +
            "line\r\n" +
            "..dot\r\n" +
            ".\r\n" +
            "+OK alive\r\n" +
            "+OK after pipeline\r\n");
        Pop3WireReader reader = new Pop3WireReader(stream, 8);
        Pop3WireWriter writer = new Pop3WireWriter(stream, 8);
        Pop3ClientPipeline pipeline = new Pop3ClientPipeline(reader, writer);
        Pop3PipelineRequest stat = Request(
            Command(Pop3Commands.Stat, new string[0], false));
        Pop3PipelineRequest retrieve = Request(
            Command(Pop3Commands.Retr, new[] { "2" }, true));
        retrieve.TransferKind = Pop3TransferKind.Retrieve;
        retrieve.MessageNumber = 2;
        retrieve.MemoryCeilingBytes = 100;
        Pop3PipelineRequest noop = Request(
            Command(Pop3Commands.Noop, new string[0], false));

        Pop3PipelineResult[] results = pipeline.Execute(
            new[] { stat, retrieve, noop },
            CancellationToken.None);

        Assert.That(stream.WrittenWhenFirstRead, Is.EqualTo(
            "STAT\r\nRETR 2\r\nNOOP\r\n"));
        Assert.That(results.Select(result => result.Command.Keyword),
            Is.EqualTo(new[] { Pop3Commands.Stat, Pop3Commands.Retr, Pop3Commands.Noop }));
        Assert.That(results.Select(result => result.Response.Text),
            Is.EqualTo(new[] { "3 100", "message follows", "alive" }));
        Assert.That(results[0].Command, Is.SameAs(stat.Command));
        Assert.That(results[1].Response.IsMultiline, Is.True);
        Assert.That(Encoding.ASCII.GetString(results[1].MultilineData),
            Is.EqualTo("line\r\n.dot\r\n"));
        Assert.That(results[1].MultilineOctets,
            Is.EqualTo(results[1].MultilineData.LongLength));
        Assert.That(results[2].Response.IsMultiline, Is.False);
        Assert.That(reader.ReadResponse(512, CancellationToken.None).Text,
            Is.EqualTo("after pipeline"));
        stream.Dispose();
    }

    [Test]
    public void FullMixedPipelineCorrelatesSingleAndMultilineResponsesInOrder()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream(
            "+OK 1 10\r\n" +
            "+OK listing\r\n1 10\r\n.\r\n" +
            "+OK 1 stable-id\r\n" +
            "+OK identifiers\r\n1 stable-id\r\n.\r\n" +
            "+OK 6 octets\r\nbody\r\n.\r\n" +
            "+OK headers\r\nH: v\r\n\r\n.\r\n" +
            "+OK marked\r\n");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3PipelineRequest listOne = Request(
            Command(Pop3Commands.List, new[] { "1" }, false));
        Pop3PipelineRequest listAll = Request(
            Command(Pop3Commands.List, new string[0], true));
        listAll.TransferKind = Pop3TransferKind.List;
        Pop3PipelineRequest uidlOne = Request(
            Command(Pop3Commands.Uidl, new[] { "1" }, false));
        Pop3PipelineRequest uidlAll = Request(
            Command(Pop3Commands.Uidl, new string[0], true));
        uidlAll.TransferKind = Pop3TransferKind.Uidl;
        Pop3PipelineRequest retrieve = Request(
            Command(Pop3Commands.Retr, new[] { "1" }, true));
        retrieve.TransferKind = Pop3TransferKind.Retrieve;
        retrieve.MessageNumber = 1;
        Pop3PipelineRequest top = Request(
            Command(Pop3Commands.Top, new[] { "1", "0" }, true));
        top.TransferKind = Pop3TransferKind.Top;
        top.MessageNumber = 1;
        Pop3PipelineRequest delete = Request(
            Command(Pop3Commands.Dele, new[] { "1" }, false));

        Pop3PipelineResult[] results = pipeline.Execute(
            new[] { listOne, listAll, uidlOne, uidlAll, retrieve, top, delete },
            CancellationToken.None);

        Assert.That(stream.WrittenWhenFirstRead, Is.EqualTo(
            "LIST 1\r\nLIST\r\nUIDL 1\r\nUIDL\r\nRETR 1\r\nTOP 1 0\r\nDELE 1\r\n"));
        Assert.That(results.Select(result => result.Command.Keyword), Is.EqualTo(new[]
        {
            Pop3Commands.List,
            Pop3Commands.List,
            Pop3Commands.Uidl,
            Pop3Commands.Uidl,
            Pop3Commands.Retr,
            Pop3Commands.Top,
            Pop3Commands.Dele
        }));
        Assert.That(results.Select(result => result.Response.Text), Is.EqualTo(new[]
        {
            "1 10",
            "listing",
            "1 stable-id",
            "identifiers",
            "6 octets",
            "headers",
            "marked"
        }));
        Assert.That(Encoding.ASCII.GetString(results[1].MultilineData),
            Is.EqualTo("1 10\r\n"));
        Assert.That(Encoding.ASCII.GetString(results[3].MultilineData),
            Is.EqualTo("1 stable-id\r\n"));
        Assert.That(Encoding.ASCII.GetString(results[4].MultilineData),
            Is.EqualTo("body\r\n"));
        Assert.That(Encoding.ASCII.GetString(results[5].MultilineData),
            Is.EqualTo("H: v\r\n\r\n"));
        Assert.That(results[6].MultilineData, Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void MultilinePayloadCanStreamToCallerDestination()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream(
            "+OK listing follows\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        MemoryStream destination = new MemoryStream();
        Pop3PipelineRequest request = Request(
            Command(Pop3Commands.List, new string[0], true));
        request.MultilineDestination = destination;
        request.TransferKind = Pop3TransferKind.List;

        Pop3PipelineResult result = pipeline.Execute(
            new[] { request },
            CancellationToken.None)[0];

        Assert.That(result.MultilineData, Is.Empty);
        Assert.That(result.MultilineOctets, Is.EqualTo(12));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("1 10\r\n2 20\r\n"));
        Assert.That(destination.CanWrite, Is.True);
        stream.Dispose();
        destination.Dispose();
    }

    [Test]
    public void NegativeMultilineResponseIsCorrelatedAndLaterResponsesAreDrained()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream(
            "-ERR [SYS/TEMP] message unavailable\r\n" +
            "+OK alive\r\n" +
            "+OK synchronized\r\n");
        Pop3WireReader reader = new Pop3WireReader(stream, 8);
        Pop3ClientPipeline pipeline = new Pop3ClientPipeline(
            reader,
            new Pop3WireWriter(stream, 8));
        Pop3PipelineRequest retrieve = Request(
            Command(Pop3Commands.Retr, new[] { "4" }, true));
        retrieve.TransferKind = Pop3TransferKind.Retrieve;
        retrieve.MessageNumber = 4;
        Pop3PipelineRequest noop = Request(
            Command(Pop3Commands.Noop, new string[0], false));

        Pop3PipelineResult[] results = pipeline.Execute(
            new[] { retrieve, noop },
            CancellationToken.None);

        Assert.That(stream.WrittenWhenFirstRead, Is.EqualTo("RETR 4\r\nNOOP\r\n"));
        Assert.That(results[0].Response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(results[0].Response.ResponseCodeLevels,
            Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(results[0].Response.IsMultiline, Is.False);
        Assert.That(results[0].MultilineData, Is.Empty);
        Assert.That(results[1].Response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(reader.ReadResponse(512, CancellationToken.None).Text,
            Is.EqualTo("synchronized"));
        stream.Dispose();
    }

    [Test]
    public void EntireBatchIsValidatedBeforeTheFirstWrite()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3PipelineRequest valid = Request(
            Command(Pop3Commands.Noop, new string[0], false));
        Pop3PipelineRequest malformed = Request(
            Command("TOOLONG", new string[0], false));

        Assert.Throws<InvalidDataException>(() => pipeline.Execute(
            new[] { valid, malformed },
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => pipeline.Execute(
            new Pop3PipelineRequest[] { valid, null! },
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.ReadCount, Is.Zero);
        stream.Dispose();
    }

    [Test]
    public void DependencyAndProtocolBarriersAreRejectedBeforeWriting()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3PipelineRequest independent = Request(
            Command(Pop3Commands.Dele, new[] { "1" }, false));
        Pop3PipelineRequest dependent = Request(
            Command(Pop3Commands.Stat, new string[0], false));
        dependent.DependsOnPrevious = true;
        Pop3PipelineRequest quit = Request(
            Command(Pop3Commands.Quit, new string[0], false));

        Assert.Throws<InvalidOperationException>(() => pipeline.Execute(
            new[] { independent, dependent },
            CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => pipeline.Execute(
            new[] { quit },
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.ReadCount, Is.Zero);
        stream.Dispose();
    }

    [Test]
    public void IncorrectMultilineMetadataAndDestinationsAreRejectedBeforeWriting()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3PipelineRequest missingMetadata = Request(
            Command(Pop3Commands.Retr, new[] { "1" }, false));
        Pop3PipelineRequest extraMetadata = Request(
            Command(Pop3Commands.Noop, new string[0], true));
        Pop3PipelineRequest singleWithDestination = Request(
            Command(Pop3Commands.Noop, new string[0], false));
        singleWithDestination.MultilineDestination = new MemoryStream();

        Assert.Throws<ArgumentException>(() => pipeline.Execute(
            new[] { missingMetadata },
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => pipeline.Execute(
            new[] { extraMetadata },
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => pipeline.Execute(
            new[] { singleWithDestination },
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.ReadCount, Is.Zero);
        singleWithDestination.MultilineDestination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PreCancellationDoesNotWriteOrReadAValidatedBatch()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream("+OK unused\r\n");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() => pipeline.Execute(
                new[]
                {
                    Request(Command(Pop3Commands.Noop, new string[0], false))
                },
                source.Token));
        }

        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.ReadCount, Is.Zero);
        stream.Dispose();
    }

    [Test]
    public void MalformedMultilineFramingStopsBeforeAFollowingResponse()
    {
        PipelineDuplexStream stream = new PipelineDuplexStream(
            "+OK message follows\r\n" +
            "unterminated\r\n");
        Pop3ClientPipeline pipeline = CreatePipeline(stream);
        Pop3PipelineRequest retrieve = Request(
            Command(Pop3Commands.Retr, new[] { "1" }, true));
        retrieve.TransferKind = Pop3TransferKind.Retrieve;
        retrieve.MessageNumber = 1;
        Pop3PipelineRequest noop = Request(
            Command(Pop3Commands.Noop, new string[0], false));

        Assert.Throws<EndOfStreamException>(() => pipeline.Execute(
            new[] { retrieve, noop },
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("RETR 1\r\nNOOP\r\n"));
        stream.Dispose();
    }

    static Pop3ClientPipeline CreatePipeline(PipelineDuplexStream stream)
    {
        return new Pop3ClientPipeline(
            new Pop3WireReader(stream, 8),
            new Pop3WireWriter(stream, 8));
    }

    static Pop3Command Command(
        string keyword,
        string[] arguments,
        bool expectsMultiline)
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = keyword;
        command.Arguments = arguments;
        command.ExpectsMultilineResponse = expectsMultiline;
        return command;
    }

    static Pop3PipelineRequest Request(Pop3Command command)
    {
        Pop3PipelineRequest request = new Pop3PipelineRequest();
        request.Command = command;
        return request;
    }

    class PipelineDuplexStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public int ReadCount;
        public string? WrittenWhenFirstRead;

        public PipelineDuplexStream(string responses)
        {
            Responses = Encoding.ASCII.GetBytes(responses);
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
            get { return true; }
        }

        public override long Length
        {
            get { return Responses.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.ASCII.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (ReadCount == 0)
            {
                WrittenWhenFirstRead = WrittenText();
            }
            ReadCount++;
            int remaining = Responses.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(Responses, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
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
            Written.Write(buffer, offset, count);
        }
    }
}
