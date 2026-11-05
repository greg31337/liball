using System.Text;
using email;

namespace email.Tests;

public class EmailTokenReaderTests
{
    [Test]
    public void PullsMessageHeadersEntityBoundariesAndBoundedBodyChunksInOrder()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailStorageOptions storage = options.GetStorageOptions();
        storage.SetCopyBufferBytes(3);
        options.SetStorageOptions(storage);
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWithOptions(options);
        reader.Start(Bytes("Subject: token test\r\nContent-Type: text/plain\r\n\r\nabcdefghij"));

        AssertToken(reader, EmailTokenKind.MessageStart, 0, "root");
        AssertToken(reader, EmailTokenKind.Headers, 0, "root");
        Assert.That(reader.GetHeaders()!.Contains("Subject"), Is.True);
        AssertToken(reader, EmailTokenKind.EntityStart, 0, "root");
        AssertToken(reader, EmailTokenKind.Headers, 0, "root");
        Assert.That(reader.GetHeaders()!.Contains("Content-Type"), Is.True);
        Assert.That(reader.GetHeaders()!.Contains("Subject"), Is.False);
        AssertBodyChunk(reader, "abc", EmailBodyChunkKind.Content);
        AssertBodyChunk(reader, "def", EmailBodyChunkKind.Content);
        AssertBodyChunk(reader, "ghi", EmailBodyChunkKind.Content);
        AssertBodyChunk(reader, "j", EmailBodyChunkKind.Content);
        AssertToken(reader, EmailTokenKind.EntityEnd, 0, "root");
        AssertToken(reader, EmailTokenKind.MessageEnd, 0, "root");
        AssertToken(reader, EmailTokenKind.End, 0, "root");

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.IsComplete(), Is.True);
        Assert.That(reader.GetConsumedByteCount(), Is.EqualTo(reader.GetSourceByteCount()));
        reader.Close();
    }

    [Test]
    public void MultipartTraversalLabelsPreambleAndEpilogueAndTracksChildPaths()
    {
        const string source = "Content-Type: multipart/mixed; boundary=x\r\n\r\npre\r\n--x\r\n\r\none\r\n--x\r\n\r\ntwo\r\n--x--\r\nepi";
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWith();
        reader.Start(Bytes(source));
        List<string> chunks = new List<string>();
        List<EmailBodyChunkKind> chunkKinds = new List<EmailBodyChunkKind>();
        List<string> entityStarts = new List<string>();

        while (reader.Read())
        {
            if (reader.GetTokenKind() == EmailTokenKind.EntityStart)
            {
                entityStarts.Add(reader.GetPath().GetDisplayText());
            }
            else if (reader.GetTokenKind() == EmailTokenKind.BodyChunk)
            {
                chunks.Add(ReadText(reader.OpenBodyChunk()));
                chunkKinds.Add(reader.GetBodyChunkKind());
            }
        }

        Assert.That(entityStarts, Is.EqualTo(new[] { "root", "root/1", "root/2" }));
        Assert.That(chunks, Is.EqualTo(new[] { "pre", "one", "two", "epi" }));
        Assert.That(chunkKinds, Is.EqualTo(new[] { EmailBodyChunkKind.Preamble, EmailBodyChunkKind.Content, EmailBodyChunkKind.Content, EmailBodyChunkKind.Epilogue }));
        reader.Close();
    }

    [Test]
    public void SkipBodyJumpsFromEntityHeadersToItsMatchingEndBoundary()
    {
        const string source = "Content-Type: multipart/mixed; boundary=x\r\n\r\n--x\r\n\r\none\r\n--x\r\n\r\ntwo\r\n--x--\r\n";
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWith();
        reader.Start(Bytes(source));

        AssertToken(reader, EmailTokenKind.MessageStart, 0, "root");
        AssertToken(reader, EmailTokenKind.Headers, 0, "root");
        AssertToken(reader, EmailTokenKind.EntityStart, 0, "root");
        AssertToken(reader, EmailTokenKind.Headers, 0, "root");
        reader.SkipBody();

        AssertToken(reader, EmailTokenKind.EntityEnd, 0, "root");
        AssertToken(reader, EmailTokenKind.MessageEnd, 0, "root");
        AssertToken(reader, EmailTokenKind.End, 0, "root");
        Assert.That(reader.Read(), Is.False);
        reader.Close();
    }

    [Test]
    public void ParserIssuesAreExposedAsErrorTokensWithoutLosingTheTree()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateStrict();
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWithOptions(options);
        reader.Start(Bytes("Content-Transfer-Encoding: base64; broken\r\n\r\npayload"));
        bool sawEntity = false;
        EmailIssue issue = null!;

        while (reader.Read())
        {
            if (reader.GetTokenKind() == EmailTokenKind.EntityStart)
            {
                sawEntity = true;
            }

            if (reader.GetTokenKind() == EmailTokenKind.Error)
            {
                issue = reader.GetIssue()!;
            }
        }

        Assert.That(sawEntity, Is.True);
        Assert.That(issue, Is.Not.Null);
        Assert.That(issue.GetCode(), Is.EqualTo(EmailIssueCode.TransferEncodingMalformed));
        Assert.That(issue.GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(reader.IsComplete(), Is.True);
        reader.Close();
    }

    [Test]
    public void CloseDeletesTemporaryBodiesOwnedByTheReaderAndIsIdempotent()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            EmailParseOptions options = new EmailParseOptionsOperations().CreateConformingTolerant();
            EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
            ingest.SetStorageMode(EmailBodyStorageMode.Store);
            ingest.SetDecodeOnIngest(true);
            options.SetBodyIngestOptions(ingest);
            EmailStorageOptions storage = options.GetStorageOptions();
            storage.SetTemporaryDirectory(directory);
            storage.GetLimits().SetMemoryThresholdBytes(1);
            options.SetStorageOptions(storage);
            EmailTokenReader reader = new EmailTokenReaderOperations().CreateWithOptions(options);
            reader.StartStream(new MemoryStream(Bytes("\r\ntemporary body")));

            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(2));
            reader.Close();
            reader.Close();

            Assert.That(Directory.GetFiles(directory), Is.Empty);
            Assert.Throws<ObjectDisposedException>(() => reader.Read());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AccessorsRejectInvalidReaderStatesAndTokenKinds()
    {
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWith();

        Assert.Throws<InvalidOperationException>(() => reader.Read());
        reader.Start(Bytes("\r\nbody"));
        Assert.Throws<InvalidOperationException>(() => reader.GetTokenKind());
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetTokenKind(), Is.EqualTo(EmailTokenKind.MessageStart));
        Assert.Throws<InvalidOperationException>(() => reader.GetHeaders());
        Assert.Throws<InvalidOperationException>(() => reader.OpenBodyChunk());
        Assert.Throws<InvalidOperationException>(() => reader.SkipBody());
        Assert.Throws<InvalidOperationException>(() => reader.Start(Bytes("\r\nother")));
        reader.Close();
    }

    static void AssertToken(EmailTokenReader reader, EmailTokenKind kind, int depth, string path)
    {
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetTokenKind(), Is.EqualTo(kind));
        Assert.That(reader.GetDepth(), Is.EqualTo(depth));
        Assert.That(reader.GetPath().GetDisplayText(), Is.EqualTo(path));
    }

    static void AssertBodyChunk(EmailTokenReader reader, string expected, EmailBodyChunkKind kind)
    {
        AssertToken(reader, EmailTokenKind.BodyChunk, 0, "root");
        Assert.That(reader.GetBodyChunkKind(), Is.EqualTo(kind));
        Assert.That(reader.GetBodyChunkByteCount(), Is.EqualTo(expected.Length));
        Assert.That(ReadText(reader.OpenBodyChunk()), Is.EqualTo(expected));
    }

    static string ReadText(Stream stream)
    {
        using (stream)
        using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
        {
            return reader.ReadToEnd();
        }
    }

    static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-token-reader-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
