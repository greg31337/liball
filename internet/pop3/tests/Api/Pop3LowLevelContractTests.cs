namespace pop3.Tests;

public class Pop3LowLevelContractTests
{
    [Test]
    public void WireContractsCarryLimitsCancellationAndCallerStreams()
    {
        WireProbe probe = new WireProbe();
        IPop3WireReader reader = probe;
        IPop3WireWriter writer = probe;
        Pop3Command command = new Pop3Command();
        Pop3Response response = new Pop3Response();
        Pop3Capability capability = new Pop3Capability();
        CancellationTokenSource source = new CancellationTokenSource();
        MemoryStream destination = new MemoryStream();
        MemoryStream multilineSource = new MemoryStream(new byte[] { 1, 2, 3 });
        probe.Command = command;
        probe.Response = response;
        probe.MultilinePayload = new byte[] { 4, 5 };

        Assert.That(reader.ReadCommand(255, source.Token), Is.SameAs(command));
        Assert.That(reader.ReadResponse(512, source.Token), Is.SameAs(response));
        Assert.That(reader.CopyMultilineResponse(destination, 4096, source.Token), Is.EqualTo(2));
        writer.WriteCommand(command, 255, source.Token);
        writer.WriteResponse(response, 512, source.Token);
        writer.WriteCapabilities(new[] { capability }, 512, source.Token);
        Assert.That(writer.WriteMultilineData(multilineSource, 4096, source.Token), Is.EqualTo(3));

        Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(probe.LastCommand, Is.SameAs(command));
        Assert.That(probe.LastResponse, Is.SameAs(response));
        Assert.That(probe.LastCapabilities, Is.EqualTo(new[] { capability }));
        Assert.That(probe.LastLimit, Is.EqualTo(4096));
        Assert.That(probe.LastCancellationToken, Is.EqualTo(source.Token));
    }

    [Test]
    public void TlsContractsPreserveTransportIdentityAndOptions()
    {
        TlsProbe probe = new TlsProbe();
        IPop3ClientTlsProvider clientProvider = probe;
        IPop3ServerTlsProvider serverProvider = probe;
        MemoryStream clientStream = new MemoryStream();
        MemoryStream serverStream = new MemoryStream();
        Pop3TlsClientOptions clientOptions = new Pop3TlsClientOptions();
        Pop3TlsServerOptions serverOptions = new Pop3TlsServerOptions();
        CancellationTokenSource source = new CancellationTokenSource();

        Assert.That(clientProvider.SecureClientStream(clientStream, clientOptions, source.Token), Is.SameAs(clientStream));
        Assert.That(serverProvider.SecureServerStream(serverStream, serverOptions, source.Token), Is.SameAs(serverStream));
        Assert.That(probe.ClientOptions, Is.SameAs(clientOptions));
        Assert.That(probe.ServerOptions, Is.SameAs(serverOptions));
        Assert.That(probe.LastCancellationToken, Is.EqualTo(source.Token));
    }

    [Test]
    public void SaslClientContractSupportsCompleteExchangeLifecycle()
    {
        SaslClientProbe probe = new SaslClientProbe();
        IPop3SaslClient client = probe;
        MemoryStream stream = new MemoryStream();

        Assert.That(client.GetMechanism(), Is.EqualTo("TEST"));
        Assert.That(client.BeginAuthentication().Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(client.ContinueAuthentication(new byte[] { 2, 3 }).Data, Is.EqualTo(new byte[] { 2, 3 }));
        Assert.That(client.ApplySecurityLayer(stream), Is.SameAs(stream));
        client.CancelAuthentication();

        Assert.That(probe.WasCancelled, Is.True);
    }

    [Test]
    public void LoggingAndTransferContractsDeliverOriginalDataObjects()
    {
        CallbackProbe probe = new CallbackProbe();
        IPop3ProtocolLogger logger = probe;
        IPop3TransferObserver observer = probe;
        Pop3LogEntry entry = new Pop3LogEntry();
        Pop3TransferProgress progress = new Pop3TransferProgress();

        logger.Log(entry);
        observer.Report(progress);

        Assert.That(probe.Entry, Is.SameAs(entry));
        Assert.That(probe.Progress, Is.SameAs(progress));
    }

    private class WireProbe : IPop3WireReader, IPop3WireWriter
    {
        public Pop3Command Command = new Pop3Command();
        public Pop3Response Response = new Pop3Response();
        public byte[] MultilinePayload = Array.Empty<byte>();
        public Pop3Command? LastCommand;
        public Pop3Response? LastResponse;
        public Pop3Capability[] LastCapabilities = Array.Empty<Pop3Capability>();
        public int LastLimit;
        public CancellationToken LastCancellationToken;

        public Pop3Command ReadCommand(int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
            return Command;
        }

        public Pop3Response ReadResponse(int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
            return Response;
        }

        public long CopyMultilineResponse(Stream destination, int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
            destination.Write(MultilinePayload);
            return MultilinePayload.Length;
        }

        public void WriteCommand(Pop3Command command, int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastCommand = command;
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
        }

        public void WriteResponse(Pop3Response response, int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastResponse = response;
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
        }

        public void WriteCapabilities(Pop3Capability[] capabilities, int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastCapabilities = capabilities;
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
        }

        public long WriteMultilineData(Stream source, int maximumLineOctets, CancellationToken cancellationToken)
        {
            LastLimit = maximumLineOctets;
            LastCancellationToken = cancellationToken;
            return source.Length;
        }
    }

    private class TlsProbe : IPop3ClientTlsProvider, IPop3ServerTlsProvider
    {
        public Pop3TlsClientOptions? ClientOptions;
        public Pop3TlsServerOptions? ServerOptions;
        public CancellationToken LastCancellationToken;

        public Stream SecureClientStream(Stream stream, Pop3TlsClientOptions options, CancellationToken cancellationToken)
        {
            ClientOptions = options;
            LastCancellationToken = cancellationToken;
            return stream;
        }

        public Stream SecureServerStream(Stream stream, Pop3TlsServerOptions options, CancellationToken cancellationToken)
        {
            ServerOptions = options;
            LastCancellationToken = cancellationToken;
            return stream;
        }
    }

    private class SaslClientProbe : IPop3SaslClient
    {
        public bool WasCancelled;

        public string GetMechanism()
        {
            return "TEST";
        }

        public Pop3SaslStep BeginAuthentication()
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Data = new byte[] { 1 };
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Data = challenge;
            return step;
        }

        public void CancelAuthentication()
        {
            WasCancelled = true;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
        }
    }

    private class CallbackProbe : IPop3ProtocolLogger, IPop3TransferObserver
    {
        public Pop3LogEntry? Entry;
        public Pop3TransferProgress? Progress;

        public void Log(Pop3LogEntry entry)
        {
            Entry = entry;
        }

        public void Report(Pop3TransferProgress progress)
        {
            Progress = progress;
        }
    }
}
