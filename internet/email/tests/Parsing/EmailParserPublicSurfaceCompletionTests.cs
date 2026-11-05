using System.Text;
using email;

namespace email.Tests;

public class EmailParserPublicSurfaceCompletionTests
{
    [Test]
    public void WholeParserMemoryAndStreamOverloadsProduceEquivalentMessages()
    {
        byte[] source = Bytes("Subject: parity\r\n\r\nbody");
        ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(source);
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailParser parser = new EmailParser();

        EmailParseResult genericMemory = parser.ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(memory, options);
        EmailParseResult messageMemory = parser.ParseMessageWithInputUsingReadOnlyMemory(memory);
        EmailParseResult messageMemoryOptions = parser.ParseMessageWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(memory, options);
        MemoryStream genericStreamInput = new MemoryStream(source, false);
        EmailParseResult genericStream = parser.ParseWithInputUsingStream(genericStreamInput);
        MemoryStream messageStreamInput = new MemoryStream(source, false);
        EmailParseResult messageStream = parser.ParseMessageWithInputUsingStream(messageStreamInput);

        AssertParsedMessage(genericMemory, source.Length);
        AssertParsedMessage(messageMemory, source.Length);
        AssertParsedMessage(messageMemoryOptions, source.Length);
        AssertParsedMessage(genericStream, source.Length);
        AssertParsedMessage(messageStream, source.Length);
        Assert.That(genericStreamInput.CanRead, Is.True);
        Assert.That(messageStreamInput.CanRead, Is.True);
        genericStreamInput.Dispose();
        messageStreamInput.Dispose();
    }

    [Test]
    public void EntityParserMemoryAndStreamOverloadsExposeCompleteCountsAndStatus()
    {
        byte[] source = Bytes("Content-Type: text/plain\r\n\r\nbody");
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailParser parser = new EmailParser();
        EmailEntityParseResult memory = parser.ParseEntityWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(new ReadOnlyMemory<byte>(source), options);
        MemoryStream streamInput = new MemoryStream(source, false);
        EmailEntityParseResult stream = parser.ParseEntityWithInputUsingStream(streamInput);

        AssertParsedEntity(memory, source.Length);
        AssertParsedEntity(stream, source.Length);
        Assert.That(streamInput.CanRead, Is.True);
        streamInput.Dispose();
    }

    [Test]
    public void HeaderBlockConvenienceOverloadsReportNormalAndRecoveredCompletion()
    {
        EmailHeaderBlockParser parser = new EmailHeaderBlockParser();
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();
        byte[] completeSource = Bytes("Subject: value\r\n\r\nbody");
        EmailHeaderBlockParseResult complete = parser.ParseWithInputAndOptionsAndEndOfInput(completeSource, options, true);
        EmailHeaderBlockParseResult positioned = parser.ParseWithInputAndOptionsAndInitialPosition(Bytes("Subject: value\r\n\r\n"), options, new EmailSourcePosition(20, 3, 4));
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetAllowMissingHeaderBodySeparator(true);
        options.SetCompatibilityOptions(compatibility);
        EmailHeaderBlockParseResult recovered = parser.ParseWithInputAndOptionsAndEndOfInput(Bytes("Subject: value\r\nbody\r\n"), options, true);

        Assert.That(complete.IsComplete(), Is.True);
        Assert.That(complete.HasFatalError(), Is.False);
        Assert.That(complete.WasRecovered(), Is.False);
        Assert.That(positioned.GetHeaders().GetAt(0).GetValueSourcePosition()!.GetByteOffset(), Is.EqualTo(28));
        Assert.That(recovered.IsComplete(), Is.True);
        Assert.That(recovered.WasRecovered(), Is.True);
    }

    [Test]
    public void TokenReaderFactoriesAcceptArrayMemoryAndStreamsAndDisposeIdempotently()
    {
        byte[] source = Bytes("Subject: tokens\r\n\r\nbody");
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailTokenReaderOperations factory = new EmailTokenReaderOperations();
        EmailTokenReader[] readers = new EmailTokenReader[]
        {
            factory.CreateWithInputUsingValueArray(source),
            factory.CreateWithInputUsingReadOnlyMemory(new ReadOnlyMemory<byte>(source)),
            factory.CreateWithInputUsingStream(new MemoryStream(source, false)),
            factory.CreateWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(new ReadOnlyMemory<byte>(source), options),
            factory.CreateWithInputAndOptionsUsingStreamAndEmailParseOptions(new MemoryStream(source, false), options)
        };

        foreach (EmailTokenReader reader in readers)
        {
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetTokenKind(), Is.EqualTo(EmailTokenKind.MessageStart));
            reader.Dispose();
            reader.Close();
        }

        EmailTokenReader direct = factory.CreateWithOptions(options);
        direct.StartMemory(new ReadOnlyMemory<byte>(source));
        Assert.That(direct.Read(), Is.True);
        direct.Dispose();
    }

    [Test]
    public void FeedAndBoundaryIncrementalProcessorsExposeLifecycleMetadata()
    {
        EmailFeedParser feed = new EmailFeedParser(new EmailParseOptionsOperations().CreatePreserving());
        feed.Feed(Bytes("Subject: partial\r\n"));
        feed.Dispose();
        feed.Close();

        byte[] multipart = Bytes("--edge\r\npart\r\n--edge--\r\n");
        EmailBoundaryScanner scanner = new EmailBoundaryScanner("edge", new EmailParseOptions());
        int split = 0;
        while (split < multipart.Length)
        {
            scanner.Feed(new ReadOnlySpan<byte>(multipart, split, 1));
            split++;
        }

        EmailBoundaryScanResult result = scanner.Complete();
        Assert.That(scanner.IsComplete(), Is.True);
        Assert.That(scanner.HasClosingBoundary(), Is.True);
        Assert.That(scanner.GetSourceByteCount(), Is.EqualTo(multipart.Length));
        Assert.That(result.HasClosingBoundary(), Is.True);
    }

    static void AssertParsedMessage(EmailParseResult result, int length)
    {
        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(length));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(length));
        Assert.That(result.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("parity"));
    }

    static void AssertParsedEntity(EmailEntityParseResult result, int length)
    {
        Assert.That(result.HasEntity(), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.HasFatalError(), Is.False);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(length));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(length));
        Assert.That(result.GetEntity()!.GetMediaType()!.IsType("text", "plain"), Is.True);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
