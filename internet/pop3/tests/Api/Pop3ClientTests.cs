using System.Text;

namespace pop3.Tests;

public class Pop3ClientTests
{
    [Test]
    public void PublicClientRunsACompleteUserPasswordTransactionWorkflow()
    {
        ClientSequenceStream stream = new ClientSequenceStream(
            "+OK ready\r\n" +
            "+OK initial capabilities\r\nUSER\r\nUTF8 USER\r\nLANG\r\n.\r\n" +
            "+OK refreshed capabilities\r\nUSER\r\nUTF8 USER\r\nLANG\r\n.\r\n" +
            "+OK UTF8 enabled\r\n" +
            "+OK languages\r\nfr Français\r\n.\r\n" +
            "+OK language selected\r\n" +
            "+OK default language\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "+OK transaction capabilities\r\nTOP\r\nUIDL\r\nLANG\r\n.\r\n" +
            "+OK 2 10\r\n" +
            "+OK 1 4\r\n" +
            "+OK listing\r\n1 4\r\n2 6\r\n.\r\n" +
            "+OK 1 id-one\r\n" +
            "+OK identifiers\r\n1 id-one\r\n2 id-two\r\n.\r\n" +
            "+OK 4 octets\r\nab\r\n.\r\n" +
            "+OK 6 octets\r\nbody\r\n.\r\n" +
            "+OK top\r\nhead\r\n.\r\n" +
            "+OK top\r\ntop\r\n.\r\n" +
            "+OK first deleted\r\n" +
            "+OK second deleted\r\n" +
            "+OK reset\r\n" +
            "+OK 2 10\r\n" +
            "+OK still active\r\n" +
            "+OK listing\r\n1 4\r\n2 6\r\n.\r\n" +
            "+OK first deleted again\r\n" +
            "+OK second deleted again\r\n" +
            "+OK extension accepted\r\n" +
            "+OK extension data\r\ndata\r\n..dot\r\n.\r\n" +
            "+OK committed\r\n");
        Pop3ClientOptions options = CreateOptions();
        options.Authentication.AllowUnprotectedCredentials = true;
        IPop3Client client = new Pop3Client();

        client.ConnectStream(stream, options, CancellationToken.None);
        Pop3CapabilitySet capabilities = client.RefreshCapabilities(
            CancellationToken.None);
        client.EnableUtf8(CancellationToken.None);
        Pop3Language[] languages = client.GetLanguages(CancellationToken.None);
        client.SelectLanguage("fr", CancellationToken.None);
        client.ResetLanguage(CancellationToken.None);
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "alice";
        credentials.Password = "secret";
        Pop3AuthenticationResult authentication = client.AuthenticateUserPassword(
            credentials,
            CancellationToken.None);

        Pop3MailboxStatus status = client.GetMailboxStatus(CancellationToken.None);
        Pop3MessageInfo message = client.GetMessageInfo(1, CancellationToken.None);
        Pop3MessageInfo[] messages = client.GetAllMessageInfo(CancellationToken.None);
        Pop3UniqueId uniqueId = client.GetUniqueId(1, CancellationToken.None);
        Pop3UniqueId[] uniqueIds = client.GetAllUniqueIds(CancellationToken.None);
        MemoryStream messageDestination = new MemoryStream();
        long messageOctets = client.RetrieveMessageToStream(
            1,
            messageDestination,
            CancellationToken.None);
        byte[] memoryMessage = client.RetrieveMessageToMemory(
            2,
            CancellationToken.None);
        Assert.That(client.RetrieveMessagesToStreams(
            new int[0],
            new Stream[0],
            CancellationToken.None), Is.Empty);
        Assert.That(client.RetrieveMessagesToMemory(
            new int[0],
            CancellationToken.None), Is.Empty);
        MemoryStream topDestination = new MemoryStream();
        long topOctets = client.RetrieveTopToStream(
            1,
            0,
            topDestination,
            CancellationToken.None);
        byte[] memoryTop = client.RetrieveTopToMemory(
            2,
            1,
            CancellationToken.None);
        Assert.That(client.RetrieveTopBatchToStreams(
            new int[0],
            new int[0],
            new Stream[0],
            CancellationToken.None), Is.Empty);
        Assert.That(client.RetrieveTopBatchToMemory(
            new int[0],
            new int[0],
            CancellationToken.None), Is.Empty);

        client.DeleteMessage(1, CancellationToken.None);
        client.DeleteMessages(new[] { 2 }, CancellationToken.None);
        Pop3MailboxStatus restored = client.ResetDeletions(CancellationToken.None);
        client.NoOperation(CancellationToken.None);
        client.DeleteAllMessages(CancellationToken.None);
        Pop3Command singleExtension = new Pop3Command();
        singleExtension.Keyword = "XONE";
        singleExtension.Arguments = new[] { "value" };
        Pop3Response extensionResponse = client.ExecuteSingleLineExtension(
            singleExtension,
            CancellationToken.None);
        Pop3Command multilineExtension = new Pop3Command();
        multilineExtension.Keyword = "XMUL";
        multilineExtension.ExpectsMultilineResponse = true;
        MemoryStream extensionDestination = new MemoryStream();
        Pop3Response multilineResponse = client.ExecuteMultilineExtension(
            multilineExtension,
            extensionDestination,
            CancellationToken.None);

        Assert.That(capabilities.SupportsUser, Is.True);
        Assert.That(capabilities.SupportsUtf8, Is.True);
        Assert.That(languages.Select(language => language.Tag),
            Is.EqualTo(new[] { "fr" }));
        Assert.That(authentication.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(authentication.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(10));
        Assert.That(message.OctetCount, Is.EqualTo(4));
        Assert.That(messages.Select(item => item.MessageNumber),
            Is.EqualTo(new[] { 1, 2 }));
        Assert.That(uniqueId.Value, Is.EqualTo("id-one"));
        Assert.That(uniqueIds.Select(item => item.Value),
            Is.EqualTo(new[] { "id-one", "id-two" }));
        Assert.That(messageOctets, Is.EqualTo(4));
        Assert.That(Encoding.ASCII.GetString(messageDestination.ToArray()),
            Is.EqualTo("ab\r\n"));
        Assert.That(Encoding.ASCII.GetString(memoryMessage),
            Is.EqualTo("body\r\n"));
        Assert.That(topOctets, Is.EqualTo(6));
        Assert.That(Encoding.ASCII.GetString(topDestination.ToArray()),
            Is.EqualTo("head\r\n"));
        Assert.That(Encoding.ASCII.GetString(memoryTop), Is.EqualTo("top\r\n"));
        Assert.That(restored.MessageCount, Is.EqualTo(2));
        Assert.That(restored.TotalOctets, Is.EqualTo(10));
        Assert.That(extensionResponse.Text, Is.EqualTo("extension accepted"));
        Assert.That(multilineResponse.IsMultiline, Is.True);
        Assert.That(Encoding.ASCII.GetString(extensionDestination.ToArray()),
            Is.EqualTo("data\r\n.dot\r\n"));
        Assert.That(client.GetSnapshot().Messages,
            Is.All.Matches<Pop3MessageInfo>(item => item.IsDeleted));

        client.QuitAndCommit(CancellationToken.None);

        Assert.That(client.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\n" +
            "CAPA\r\n" +
            "UTF8\r\n" +
            "LANG\r\n" +
            "LANG fr\r\n" +
            "LANG *\r\n" +
            "USER alice\r\n" +
            "PASS secret\r\n" +
            "CAPA\r\n" +
            "STAT\r\n" +
            "LIST 1\r\n" +
            "LIST\r\n" +
            "UIDL 1\r\n" +
            "UIDL\r\n" +
            "RETR 1\r\n" +
            "RETR 2\r\n" +
            "TOP 1 0\r\n" +
            "TOP 2 1\r\n" +
            "DELE 1\r\n" +
            "DELE 2\r\n" +
            "RSET\r\n" +
            "STAT\r\n" +
            "NOOP\r\n" +
            "LIST\r\n" +
            "DELE 1\r\n" +
            "DELE 2\r\n" +
            "XONE value\r\n" +
            "XMUL\r\n" +
            "QUIT\r\n"));

        messageDestination.Dispose();
        topDestination.Dispose();
        extensionDestination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PublicClientStartsDisconnectedAndCanAbortDeterministically()
    {
        IPop3Client client = new Pop3Client();

        Pop3ClientSnapshot initial = client.GetSnapshot();
        client.Abort();
        Pop3ClientSnapshot closed = client.GetSnapshot();
        client.Abort();

        Assert.That(initial.State, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(initial.IsConnected, Is.False);
        Assert.That(closed.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(closed.IsConnected, Is.False);
        Assert.That(client.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
    }

    static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 1024;
        options.MessageMemoryCeilingBytes = 1024;
        options.LeaveStreamOpen = true;
        return options;
    }

    class ClientSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public ClientSequenceStream(string responses)
        {
            Responses = Encoding.UTF8.GetBytes(responses);
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
            return Encoding.UTF8.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = Responses.Length - ReadPosition;
            if (remaining == 0)
            {
                return 0;
            }
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
