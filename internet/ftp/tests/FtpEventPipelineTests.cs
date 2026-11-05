namespace ftp.Tests;

public class FtpEventPipelineTests
{
    [Test]
    public void Null_event_sink_discards_events_without_side_effects()
    {
        NullFtpEventSink sink = new NullFtpEventSink();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => sink.RecordEvent(new FtpEvent()));
            Assert.DoesNotThrow(() => sink.RecordEvent(null!));
        });
    }

    [Test]
    public void Pipeline_records_detached_redacted_snapshots()
    {
        PipelineEventSink sink = new PipelineEventSink();
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Pass;
        command.Verb = "PASS";
        command.Argument = "top-secret";
        command.RawText = "PASS top-secret";
        command.Sensitive = true;
        FtpReply reply = FtpServerSession.CreateReply(
            FtpReplyCodes.UserLoggedIn, "accepted top-secret");
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = "alice";
        principal.Authenticated = true;
        principal.Account = "secret-account";
        principal.CertificateIdentity = "secret-certificate";
        principal.HomeDirectory = "/secret/home";
        principal.Roles = new[] { "reader" };
        FtpTransferProgress progress = new FtpTransferProgress();
        progress.Path = "/file";
        progress.LogicalBytes = 7;
        FtpTlsDiagnostics diagnostics = new FtpTlsDiagnostics();
        diagnostics.Authenticated = true;
        diagnostics.Protocol = "TLS1.3";
        FtpEvent source = new FtpEvent();
        source.Kind = FtpEventKind.Reply;
        source.Command = command;
        source.Reply = reply;
        source.Principal = principal;
        source.TransferProgress = progress;
        source.TlsDiagnostics = diagnostics;
        source.RemoteEndpoint = new FtpEndpoint();
        source.RemoteEndpoint.Host = "127.0.0.1";
        source.Message = "top-secret";

        bool recorded = FtpEventPipeline.RecordEvent(sink, source);
        command.Argument = "mutated";
        reply.Text = "mutated";
        principal.Roles[0] = "mutated";
        progress.LogicalBytes = 99;
        diagnostics.Protocol = "mutated";
        source.RemoteEndpoint.Host = "mutated";
        FtpEvent captured = sink.Events.Single();

        Assert.Multiple(() =>
        {
            Assert.That(recorded, Is.True);
            Assert.That(captured, Is.Not.SameAs(source));
            Assert.That(captured.Redacted, Is.True);
            Assert.That(captured.Timestamp.HasValue, Is.True);
            Assert.That(captured.Command, Is.Not.Null);
            Assert.That(captured.Command!.Argument,
                Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(captured.Reply, Is.Not.Null);
            Assert.That(captured.Reply!.Text,
                Is.EqualTo(FtpTranscriptRedactor.RedactedText));
            Assert.That(captured.Message, Does.Not.Contain("top-secret"));
            Assert.That(captured.Principal, Is.Not.Null);
            Assert.That(captured.Principal!.Name, Is.EqualTo("alice"));
            Assert.That(captured.Principal.Account, Is.Null);
            Assert.That(captured.Principal.CertificateIdentity, Is.Null);
            Assert.That(captured.Principal.HomeDirectory, Is.Null);
            Assert.That(captured.Principal.Roles, Is.EqualTo(new[] { "reader" }));
            Assert.That(captured.TransferProgress!.LogicalBytes, Is.EqualTo(7));
            Assert.That(captured.TlsDiagnostics!.Protocol, Is.EqualTo("TLS1.3"));
            Assert.That(captured.RemoteEndpoint!.Host, Is.EqualTo("127.0.0.1"));
        });
    }

    [Test]
    public void Pipeline_factories_cover_every_required_activity_category()
    {
        PipelineEventSink sink = new PipelineEventSink();
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = "alice";
        principal.Authenticated = true;
        principal.Account = "secret-account";
        FtpAuthenticationRequest authenticationRequest =
            new FtpAuthenticationRequest();
        authenticationRequest.UserName = "alice";
        authenticationRequest.Password = "secret-password";
        FtpAuthenticationResult authenticationResult =
            new FtpAuthenticationResult();
        authenticationResult.Status = FtpAuthenticationStatus.Success;
        authenticationResult.Principal = principal;
        FtpAuthorizationRequest authorizationRequest =
            new FtpAuthorizationRequest();
        authorizationRequest.Principal = principal;
        authorizationRequest.Operation = FtpAuthorizationOperation.Read;
        authorizationRequest.Path = "/file";
        FtpAuthorizationResult authorizationResult =
            FtpAuthorizationGate.CreateAllowedResult();
        FtpEntry entry = new FtpEntry();
        entry.Type = FtpEntryType.File;
        FtpTransferProgress progress = new FtpTransferProgress();
        progress.Direction = FtpTransferDirection.Upload;
        progress.Path = "/file";
        FtpTlsDiagnostics diagnostics = new FtpTlsDiagnostics();
        diagnostics.Authenticated = true;
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Pass;
        command.Verb = "PASS";
        command.Argument = "secret-password";
        command.RawText = "PASS secret-password";
        command.Sensitive = true;
        FtpReply reply = FtpServerSession.CreateReply(
            FtpReplyCodes.UserLoggedIn, "Logged in");
        FtpEvent lifecycle = FtpEventPipeline.CreateBaseEvent(
            FtpEventKind.Lifecycle);
        lifecycle.Message = "FTP lifecycle activity.";
        FtpEvent commandEvent = FtpTranscriptRedactor.CreateCommandEvent(command);
        FtpEvent replyEvent =
            FtpTranscriptRedactor.CreateReplyEvent(command, reply);
        FtpEvent[] events = new[]
        {
            lifecycle,
            commandEvent,
            replyEvent,
            FtpEventPipeline.CreateAuthenticationEvent(
                authenticationRequest, authenticationResult),
            FtpEventPipeline.CreateAuthorizationEvent(
                authorizationRequest, authorizationResult),
            FtpEventPipeline.CreateTlsEvent(diagnostics, true),
            FtpEventPipeline.CreateFileSystemEvent(
                FtpAuthorizationOperation.Read, "/file", entry, true),
            FtpEventPipeline.CreateTransferEvent(
                FtpEventKind.TransferStarted, progress),
            FtpEventPipeline.CreateTransferEvent(
                FtpEventKind.TransferProgress, progress),
            FtpEventPipeline.CreateTransferEvent(
                FtpEventKind.TransferCompleted, progress),
            FtpEventPipeline.CreatePolicyEvent("FTP policy rejected the request."),
            FtpEventPipeline.CreateFaultEvent("password=secret\r")
        };

        foreach (FtpEvent eventData in events)
        {
            Assert.That(FtpEventPipeline.RecordEvent(sink, eventData), Is.True);
        }

        FtpEventKind[] kinds = sink.Events.Select(eventData =>
            eventData.Kind).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(kinds, Does.Contain(FtpEventKind.Lifecycle));
            Assert.That(kinds, Does.Contain(FtpEventKind.Command));
            Assert.That(kinds, Does.Contain(FtpEventKind.Reply));
            Assert.That(kinds, Does.Contain(FtpEventKind.Authentication));
            Assert.That(kinds, Does.Contain(FtpEventKind.Authorization));
            Assert.That(kinds, Does.Contain(FtpEventKind.Tls));
            Assert.That(kinds, Does.Contain(FtpEventKind.FileSystem));
            Assert.That(kinds, Does.Contain(FtpEventKind.TransferStarted));
            Assert.That(kinds, Does.Contain(FtpEventKind.TransferProgress));
            Assert.That(kinds, Does.Contain(FtpEventKind.TransferCompleted));
            Assert.That(kinds, Does.Contain(FtpEventKind.PolicyRejection));
            Assert.That(kinds, Does.Contain(FtpEventKind.Fault));
            Assert.That(sink.Events.All(eventData => eventData.Redacted), Is.True);
            Assert.That(sink.Events.All(eventData =>
                eventData.Timestamp.HasValue), Is.True);
            Assert.That(string.Join(" ", sink.Events.Select(eventData =>
                eventData.Message)), Does.Not.Contain("secret-password"));
            Assert.That(sink.Events.Single(eventData =>
                eventData.Kind == FtpEventKind.Authentication)
                .Principal!.Account, Is.Null);
            Assert.That(sink.Events.Single(eventData =>
                eventData.Kind == FtpEventKind.FileSystem).EntryType,
                Is.EqualTo(FtpEntryType.File));
        });
    }

    [Test]
    public void Authorization_faults_emit_redacted_fault_and_decision_events()
    {
        RecordingFtpAuthorizer provider = new RecordingFtpAuthorizer();
        provider.Throw = true;
        PipelineEventSink sink = new PipelineEventSink();
        FtpAuthorizationGate gate = new FtpAuthorizationGate(
            provider, null, sink);
        FtpAuthorizationRequest request = new FtpAuthorizationRequest();
        request.Principal.Name = "alice";
        request.Principal.Authenticated = true;
        request.Operation = FtpAuthorizationOperation.Read;
        request.Path = "/file";

        FtpAuthorizationResult result = gate.AuthorizeOperation(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Allowed, Is.False);
            Assert.That(sink.Events.Select(eventData => eventData.Kind),
                Is.EqualTo(new[]
                {
                    FtpEventKind.Fault,
                    FtpEventKind.Authorization
                }));
            Assert.That(sink.Events.All(eventData =>
                !eventData.Message.Contains("Provider fault")), Is.True);
            Assert.That(sink.Events[1].HasAuthorizationOperation, Is.True);
            Assert.That(sink.Events[1].AuthorizationOperation,
                Is.EqualTo(FtpAuthorizationOperation.Read));
        });
    }

    [Test]
    public void Pipeline_isolates_failing_and_missing_sinks()
    {
        FtpEvent eventData = FtpEventPipeline.CreateFaultEvent("test");

        Assert.Multiple(() =>
        {
            Assert.That(FtpEventPipeline.RecordEvent(null, eventData), Is.False);
            Assert.That(FtpEventPipeline.RecordEvent(
                new ThrowingPipelineEventSink(), eventData), Is.False);
        });
    }
}

public class PipelineEventSink : IFtpEventSink
{
    public List<FtpEvent> Events;

    public PipelineEventSink()
    {
        Events = new List<FtpEvent>();
    }

    public void RecordEvent(FtpEvent eventData)
    {
        Events.Add(eventData);
    }
}

public class ThrowingPipelineEventSink : IFtpEventSink
{
    public ThrowingPipelineEventSink()
    {
    }

    public void RecordEvent(FtpEvent eventData)
    {
        throw new InvalidOperationException("Event sink failure.");
    }
}
