using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpServerSession
{
    internal FtpServerSessionContext Context;
    internal object CommandLock;
    internal object ReplyLock;
    internal FtpServerOptions Settings;
    internal IFtpFileSystem? DefaultFileSystem;
    internal IFtpAuthenticator? DefaultAuthenticator;
    internal IFtpAuthorizer? DefaultAuthorizer;
    internal IFtpHostResolver? HostResolver;
    internal IFtpCertificateProvider? DefaultCertificateProvider;
    internal IFtpEventSink? EventSink;
    internal Func<FtpServerCommandContext, FtpCommandResult> CommandOperation;
    internal FtpSessionStateMachine State;
    internal FtpReplyWriter ReplyWriter;
    internal Stream? ControlStream;
    internal NetworkStream? TransportStream;
    internal FtpVirtualHostOptions? ActiveHost;
    internal IFtpFileSystem? ActiveFileSystem;
    internal IFtpAuthenticator? ActiveAuthenticator;
    internal IFtpAuthorizer? ActiveAuthorizer;
    internal IFtpCertificateProvider? ActiveCertificateProvider;
    internal FtpFeatureSet ConfiguredFeatures;
    internal FtpFeatureSet ActiveFeatures;
    internal FtpOptionSessionState OptionState;
    internal IFtpExtensionHandler[] ActiveExtensionHandlers;
    internal string ActiveRootPath;
    internal FtpTlsServerStream? ControlTls;
    internal string? CertificateIdentity;
    internal FtpPrincipal? AuthenticationPrincipal;
    internal FtpServerActiveConnector? ActiveConnector;
    internal FtpPassiveListener? PassiveListener;
    internal FtpAllocationArgument? PendingAllocation;
    internal int AuthenticationAttempts;
    internal bool Closing;
    internal bool ControlInputSynchronized;
    internal FtpServerPendingTransfer? PendingTransfer;
    internal FtpCommand? PretIntent;
    internal bool PretDataSetupAccepted;
    internal string? ClientName;

    internal FtpServerSession(
        FtpServerSessionContext context,
        FtpServerOptions settings,
        IFtpFileSystem? fileSystem,
        IFtpAuthenticator? authenticator,
        IFtpAuthorizer? authorizer,
        IFtpHostResolver? hostResolver,
        IFtpCertificateProvider? certificateProvider,
        IFtpEventSink? eventSink,
        Func<FtpServerCommandContext, FtpCommandResult> commandOperation)
    {
        Context = context;
        CommandLock = new object();
        ReplyLock = new object();
        Settings = settings;
        DefaultFileSystem = fileSystem;
        DefaultAuthenticator = authenticator;
        DefaultAuthorizer = authorizer;
        HostResolver = hostResolver;
        DefaultCertificateProvider = certificateProvider;
        EventSink = eventSink;
        CommandOperation = commandOperation;
        State = new FtpSessionStateMachine();
        ReplyWriter = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);
        ControlStream = null;
        TransportStream = null;
        ActiveHost = null;
        ActiveFileSystem = fileSystem;
        ActiveAuthenticator = authenticator;
        ActiveAuthorizer = authorizer;
        ActiveCertificateProvider = certificateProvider;
        ConfiguredFeatures = new FtpFeatureSet();
        ActiveFeatures = new FtpFeatureSet();
        OptionState = new FtpOptionSessionState();
        ActiveExtensionHandlers = settings.ExtensionHandlers;
        ActiveRootPath = "";
        ControlTls = null;
        CertificateIdentity = null;
        AuthenticationPrincipal = null;
        ActiveConnector = null;
        PassiveListener = null;
        PendingAllocation = null;
        AuthenticationAttempts = 0;
        Closing = false;
        ControlInputSynchronized = true;
        PendingTransfer = null;
        PretIntent = null;
        PretDataSetupAccepted = false;
        ClientName = null;
        RefreshActiveFeatures();
    }

    internal object? RunSession()
    {
        State.OpenConnection();
        ResolveInitialHost();
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                OpenControlStream();
                ApplyInitialHostState();
                RecordLifecycleEvent("FTP server session connected.");
                WriteGreeting();
                RunCommandLoop();
                return null;
            });

        State.State.SessionState = FtpSessionState.Closing;
        FtpFaultIsolationResult dataCleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                StopPendingTransfer();
                ResetDataConnections();
                return null;
            });
        RecordLifecycleEvent("FTP server session is closing.");
        State.CloseConnection();
        RecordLifecycleEvent("FTP server session closed.");
        if (!operation.Succeeded || !dataCleanup.Succeeded)
        {
            RecordFaultEvent("server session");
            System.Exception? failure = operation.Failure == null ?
                dataCleanup.Failure : operation.Failure;
            if (failure != null)
            {
                throw new FtpConnectionException(
                    "The FTP server session failed.", failure);
            }
            throw new FtpConnectionException(
                "The FTP server session did not reach a terminal result.", null);
        }
        return null;
    }

    internal void InitializeDataConnections(FtpPassivePortPool? portPool)
    {
        IPEndPoint? remote = Context.Socket.RemoteEndPoint as IPEndPoint;
        IPEndPoint? local = Context.Socket.LocalEndPoint as IPEndPoint;
        if (remote == null || local == null)
        {
            throw new FtpConnectionException(
                "The FTP control connection requires IP endpoints.", null);
        }

        if (Settings.DataConnection.Mode != FtpDataConnectionMode.Passive)
        {
            ActiveConnector = new FtpServerActiveConnector(
                remote.Address, Settings.DataConnection);
        }
        if (Settings.DataConnection.Mode != FtpDataConnectionMode.Active)
        {
            PassiveListener = new FtpPassiveListener(
                remote.Address,
                local.Address,
                Context.ListenerSettings,
                Settings.DataConnection,
                portPool);
        }
    }

    internal void ResetDataConnections()
    {
        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null)
        {
            active.ResetSession();
        }
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null)
        {
            passive.ResetSession();
        }
        Context.ClearPassiveSocket();
        PendingAllocation = null;
        ClearPretIntent();
    }

    internal void SetPretIntent(FtpCommand intent)
    {
        PretIntent = FtpExtensionDispatcher.CopyCommand(intent);
        PretDataSetupAccepted = false;
    }

    internal bool HasPretIntentForSetup()
    {
        return PretIntent != null;
    }

    internal void AcceptPretDataSetup()
    {
        PretDataSetupAccepted = true;
    }

    internal bool MatchesPretTransfer(FtpCommand command)
    {
        return PretDataSetupAccepted &&
            FtpPretExtension.MatchesIntent(PretIntent, command);
    }

    internal void ClearPretIntent()
    {
        PretIntent = null;
        PretDataSetupAccepted = false;
    }

    internal void ClosePassiveSetup()
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null)
        {
            passive.CloseListener();
        }
        Context.ClearPassiveSocket();
    }

    internal void ClearActiveSetup()
    {
        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null)
        {
            active.ClearDataSetup();
        }
    }

    internal FtpReply OpenPassiveSetup(
        bool extended,
        FtpAddressFamily requestedFamily)
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive == null)
        {
            throw new FtpConfigurationException(
                "A passive data listener is required.", null);
        }
        Context.ClearPassiveSocket();
        FtpReply reply = passive.OpenPassiveListener(
            extended, requestedFamily);
        if (passive.Listener != null)
        {
            Context.RegisterPassiveSocket(passive.Listener);
        }
        return reply;
    }

    internal void EnableExtendedPassiveOnly()
    {
        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null)
        {
            active.EnableExtendedPassiveOnly();
        }
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null)
        {
            passive.EnableExtendedPassiveOnly();
        }
        Context.ClearPassiveSocket();
    }

    internal bool HasDataConnectionSetup()
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null && passive.Open)
        {
            return true;
        }
        FtpServerActiveConnector? active = ActiveConnector;
        return active != null && active.Configured;
    }

    internal void DiscardDataConnectionSetup()
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null)
        {
            passive.CloseListener();
        }
        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null)
        {
            active.ClearDataSetup();
        }
        Context.ClearPassiveSocket();
    }

    internal FtpDataConnectionCoordinator CreateDataConnectionCoordinator()
    {
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null && passive.Open)
        {
            coordinator.SetOnDemandConnection(
                (timeoutMilliseconds, cancellationToken) =>
                    AcceptPassiveDataConnection(
                        timeoutMilliseconds, cancellationToken),
                () => ClearConsumedPassiveSetup());
            return coordinator;
        }

        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null && active.Configured)
        {
            coordinator.SetOnDemandConnection(
                (timeoutMilliseconds, cancellationToken) =>
                    OpenActiveDataConnection(
                        timeoutMilliseconds, cancellationToken),
                () => ClearConsumedActiveSetup());
            return coordinator;
        }
        throw new FtpConfigurationException(
            "A configured FTP data connection is required.", null);
    }

    internal Socket AcceptPassiveDataConnection(
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive == null)
        {
            throw new FtpConfigurationException(
                "An open passive FTP listener is required.", null);
        }
        Socket socket = passive.AcceptConnection(
            timeoutMilliseconds, cancellationToken);
        Context.ClearPassiveSocket();
        Context.RegisterDataSocket(socket);
        return socket;
    }

    internal Socket OpenActiveDataConnection(
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        FtpServerActiveConnector? active = ActiveConnector;
        if (active == null)
        {
            throw new FtpConfigurationException(
                "A configured active FTP endpoint is required.", null);
        }
        Socket socket = active.OpenOutboundConnection(
            timeoutMilliseconds, cancellationToken);
        Context.RegisterDataSocket(socket);
        return socket;
    }

    internal void ClearConsumedPassiveSetup()
    {
        FtpPassiveListener? passive = PassiveListener;
        if (passive != null)
        {
            passive.CloseListener();
        }
        Context.ClearPassiveSocket();
    }

    internal void ClearConsumedActiveSetup()
    {
        FtpServerActiveConnector? active = ActiveConnector;
        if (active != null)
        {
            active.ClearDataSetup();
        }
    }

    internal void OpenControlStream()
    {
        TransportStream = new NetworkStream(Context.Socket, false);
        ControlStream = TransportStream;
        if (Context.ListenerSettings.SecurityMode != FtpSecurityMode.ImplicitTls)
        {
            return;
        }

        FtpTlsServerStreamRequest request = new FtpTlsServerStreamRequest();
        request.TransportStream = TransportStream;
        request.Options = ActiveHost == null ?
            Context.ListenerSettings.Tls : ActiveHost.Tls;
        request.HandshakeTimeoutMilliseconds =
            Settings.Timeouts.TlsHandshakeTimeoutMilliseconds;
        request.CancellationToken = Settings.Timeouts.CancellationToken;
        request.LeaveTransportOpen = true;
        request.AllowTlsResume = true;
        request.CertificateProvider = ActiveCertificateProvider;
        request.HostName = ActiveHost == null ?
            "" : ActiveHost.HostName;
        request.LocalEndpoint = CopySocketEndpoint(Context.Socket.LocalEndPoint);
        request.RemoteEndpoint = CopySocketEndpoint(Context.Socket.RemoteEndPoint);
        FtpTlsServerStream tls = new FtpTlsServerStream(request);
        FtpTlsStreamResult result = tls.AuthenticateImplicitServer();
        if (result.Stream == null)
        {
            throw new FtpTlsException(
                "The implicit FTP TLS handshake returned no stream.", null);
        }
        Context.RegisterTlsResource(result.Stream);
        ControlTls = tls;
        ControlStream = result.Stream;
        CertificateIdentity =
            result.Diagnostics.RemoteCertificateSha256Fingerprint;
        State.CompleteImplicitTls();
        FtpEvent tlsEvent = FtpTlsDiagnosticsFactory.CreateEvent(
            result,
            Context.ListenerSettings.SecurityMode,
            State.State.DataProtectionLevel,
            State.State.SessionState);
        EnrichSessionEvent(tlsEvent);
        FtpEventPipeline.RecordEvent(EventSink, tlsEvent);
    }

    internal void ResolveInitialHost()
    {
        string? defaultHostName = Settings.DefaultHostName;
        if (defaultHostName == null || HostResolver == null)
        {
            return;
        }

        FtpVirtualHostRequest request = new FtpVirtualHostRequest();
        request.HostName = defaultHostName;
        request.LocalEndpoint = CopySocketEndpoint(Context.Socket.LocalEndPoint);
        request.RemoteEndpoint = CopySocketEndpoint(Context.Socket.RemoteEndPoint);
        request.ImplicitTls =
            Context.ListenerSettings.SecurityMode == FtpSecurityMode.ImplicitTls;
        FtpFaultIsolationResult resolution =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return HostResolver.ResolveVirtualHost(request);
            });
        FtpVirtualHostResult? result =
            resolution.Value as FtpVirtualHostResult;
        if (!resolution.Succeeded || result == null ||
            !result.Handled || !result.Found || result.Host == null)
        {
            if (!resolution.Succeeded)
            {
                RecordFaultEvent("virtual-host resolution");
            }
            throw new FtpConnectionException(
                "The default FTP virtual host could not be resolved.",
                resolution.Failure);
        }

        FtpConfigurationValidator.ValidateVirtualHostOptions(result.Host);
        if (!ActivateHost(result.Host))
        {
            throw new FtpConnectionException(
                "The default FTP virtual host reached its session limit.", null);
        }
    }

    internal void ApplyInitialHostState()
    {
        if (ActiveHost != null)
        {
            State.State.SelectedHost = ActiveHost.HostName;
            State.State.SessionState = FtpSessionState.HostSelected;
        }
    }

    internal bool ActivateHost(FtpVirtualHostOptions host)
    {
        if (!Context.TrySelectHost(
            host.HostName, host.MaximumSessions, host.MaximumSessionsPerUser))
        {
            return false;
        }

        ActiveHost = host;
        ActiveFileSystem = host.FileSystem == null ?
            DefaultFileSystem : host.FileSystem;
        ActiveAuthenticator = host.Authenticator == null ?
            DefaultAuthenticator : host.Authenticator;
        ActiveAuthorizer = host.Authorizer == null ?
            DefaultAuthorizer : host.Authorizer;
        ActiveCertificateProvider = host.CertificateProvider == null ?
            DefaultCertificateProvider : host.CertificateProvider;
        ConfiguredFeatures =
            FtpServerOptionsSnapshot.CopyFeatureSet(host.Features);
        ActiveExtensionHandlers = host.ExtensionHandlers == null ?
            Settings.ExtensionHandlers :
            FtpServerOptionsSnapshot.CopyExtensionHandlers(
                host.ExtensionHandlers);
        ActiveRootPath = host.RootPath;
        RefreshActiveFeatures();
        return true;
    }

    internal void ResetActiveHost()
    {
        ActiveHost = null;
        ActiveFileSystem = DefaultFileSystem;
        ActiveAuthenticator = DefaultAuthenticator;
        ActiveAuthorizer = DefaultAuthorizer;
        ActiveCertificateProvider = DefaultCertificateProvider;
        ConfiguredFeatures = new FtpFeatureSet();
        ActiveExtensionHandlers = Settings.ExtensionHandlers;
        ActiveRootPath = "";
        RefreshActiveFeatures();
    }

    internal void RefreshActiveFeatures()
    {
        ActiveFeatures = FtpServerFeatureBuilder.BuildFeatureSet(
            this, ConfiguredFeatures);
        InitializeOptionState();
    }

    internal void InitializeOptionState()
    {
        OptionState = new FtpOptionSessionState();
        OptionState.Utf8Enabled = true;
        FtpMlstFeatureInfo mlst =
            FtpFeatureCodec.ExtractMlstFacts(ActiveFeatures);
        if (mlst.Advertised)
        {
            OptionState.MlstFacts = mlst.DefaultFacts;
            OptionState.MlstFactsConfigured = true;
        }
    }

    internal void WriteGreeting()
    {
        string greeting = ActiveHost == null ?
            "FTP service ready" : ActiveHost.Greeting;
        FtpReply reply = CreateReply(FtpReplyCodes.ServiceReady, greeting);
        WriteReply(reply, null);
    }

    internal void RunCommandLoop()
    {
        Closing = false;
        while (!Closing)
        {
            FtpFaultIsolationResult readResult =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return ReadCommand();
                });
            if (!readResult.Succeeded)
            {
                HandleReadFailure(readResult);
            }
            else
            {
                FtpCommand? command = readResult.Value as FtpCommand;
                if (command == null)
                {
                    Closing = true;
                }
                else
                {
                    DispatchCommand(command);
                }
            }
        }
    }

    internal FtpCommand? ReadCommand()
    {
        ControlInputSynchronized = true;
        Stream? stream = ControlStream;
        if (stream == null)
        {
            throw new FtpConnectionException(
                "The FTP server control stream is unavailable.", null);
        }

        FtpLineReader reader = new FtpLineReader(
            FtpDefaults.MaximumControlLineBytes, 1);
        while (reader.GetPendingLineCount() == 0)
        {
            int timeoutMilliseconds = GetIdleReadTimeout();
            stream.ReadTimeout = timeoutMilliseconds;
            int value = stream.ReadByte();
            if (value < 0)
            {
                reader.CompleteInput();
                return null;
            }
            byte[] input = new byte[] { (byte)value };
            if ((reader.SawCarriageReturn && value != 10) ||
                (!reader.SawCarriageReturn && value != 10 && value != 13 &&
                    reader.LineLength >= reader.LineBuffer.Length))
            {
                ControlInputSynchronized = false;
            }
            reader.AppendBytes(input, 0, 1);
        }

        byte[]? line = reader.ReadLine();
        if (line == null)
        {
            throw new FtpProtocolException(
                "The FTP server line reader completed without a command.", null);
        }
        return FtpCommandParser.ParseEncodedCommand(
            line, OptionState.Utf8Enabled ? Encoding.UTF8 : Encoding.ASCII);
    }

    internal int GetIdleReadTimeout()
    {
        long currentMilliseconds = Environment.TickCount64;
        if (Context.HasIdleExpired(currentMilliseconds))
        {
            throw new TimeoutException("The FTP server session is idle.");
        }

        long elapsed = currentMilliseconds - Context.LastActivityMilliseconds;
        if (elapsed < 0)
        {
            elapsed = 0;
        }
        int timeout = Settings.Timeouts.IdleTimeoutMilliseconds - (int)elapsed;
        return timeout <= 0 ? 1 : timeout;
    }

    internal void HandleReadFailure(FtpFaultIsolationResult readResult)
    {
        FtpProtocolException? protocolFailure =
            readResult.Failure as FtpProtocolException;
        if (protocolFailure != null)
        {
            bool accepted = Context.TryRecordMalformedLine();
            if (accepted)
            {
                WriteReply(CreateReply(
                    FtpReplyCodes.SyntaxError,
                    "Malformed FTP command line."), null);
                if (!ControlInputSynchronized)
                {
                    RecordPolicyEvent(
                        "The FTP control stream lost command framing synchronization.");
                    Closing = true;
                }
            }
            else
            {
                WriteReply(CreateReply(
                    FtpReplyCodes.ServiceUnavailable,
                    "Too many malformed FTP command lines."), null);
                RecordPolicyEvent(
                    "The malformed FTP command limit was exceeded.");
                Closing = true;
            }
            return;
        }

        if (Context.HasIdleExpired(Environment.TickCount64))
        {
            WriteReply(CreateReply(
                FtpReplyCodes.ServiceUnavailable,
                "FTP session idle timeout."), null);
            RecordPolicyEvent("The FTP session idle timeout expired.");
        }
        else
        {
            RecordFaultEvent("control read");
        }
        Closing = true;
    }

    internal void DispatchCommand(FtpCommand command)
    {
        lock (CommandLock)
        {
            DispatchCommandLocked(command);
        }
    }

    internal void DispatchCommandLocked(FtpCommand command)
    {
        if (!Context.TryRecordCommand(Environment.TickCount64))
        {
            WriteReply(CreateReply(
                FtpReplyCodes.ServiceUnavailable,
                "FTP command rate limit exceeded."), command);
            RecordPolicyEvent("The FTP command rate limit was exceeded.");
            Closing = true;
            return;
        }

        RecordCommandEvent(command);
        FtpCommandResult? result = null;
        if (FtpExtensionDispatcher.HandlesCommand(command, State.State))
        {
            result = DispatchExtensionCommand(command);
        }
        if (result == null && command.Kind == FtpCommandKind.Host)
        {
            result = RouteHostCommand(command);
        }
        else if (result == null)
        {
            FtpServerCommandContext commandContext =
                new FtpServerCommandContext(this, command);
            FtpFaultIsolationResult dispatch =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return CommandOperation(commandContext);
                });
            result = dispatch.Value as FtpCommandResult;
            if (!dispatch.Succeeded || result == null)
            {
                RecordFaultEvent("command handler");
                result = CreateFailureResult(command);
            }
        }
        if (result.Command == null || string.IsNullOrEmpty(result.Command.Verb))
        {
            result.Command = command;
        }

        FtpReply finalReply = WriteCommandResult(result, command);
        State.ApplyCommandReply(command, finalReply);
        ApplyCommandTransition(command, finalReply);
        if (FtpReplyCoordinator.IsDataCommand(command.Kind) &&
            FtpReplyCoordinator.IsTransferPreliminaryReply(finalReply.Code))
        {
            StartPendingTransfer(command);
        }
        if (result.ConnectionClosed ||
            finalReply.Code == FtpReplyCodes.ServiceClosingControlConnection ||
            finalReply.Code == FtpReplyCodes.ServiceUnavailable)
        {
            Closing = true;
        }
    }

    internal void RegisterPendingTransfer(
        FtpCommand command,
        Func<FtpReply> operation,
        CancellationTokenSource cancellation)
    {
        if (PendingTransfer != null)
        {
            throw new FtpProtocolException(
                "Only one server data transfer can be pending.", null);
        }
        PendingTransfer = new FtpServerPendingTransfer(
            command, operation, cancellation);
    }

    internal bool HasPendingTransfer()
    {
        return PendingTransfer != null;
    }

    internal void StartPendingTransfer(FtpCommand command)
    {
        FtpServerPendingTransfer? pending = PendingTransfer;
        if (pending == null || pending.Started ||
            pending.Command != command)
        {
            throw new FtpProtocolException(
                "The server data transfer was not registered before its preliminary reply.",
                null);
        }
        pending.Started = true;
        pending.Worker = Task.Run<object?>(() =>
        {
            return RunPendingTransfer(pending);
        });
    }

    internal object? RunPendingTransfer(
        FtpServerPendingTransfer pending)
    {
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return pending.Operation.Invoke();
            });
        FtpReply? finalReply = operation.Value as FtpReply;
        FtpFaultIsolationResult completion =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                CompletePendingTransfer(pending, operation, finalReply);
                return null;
            });
        FtpServerSessionContext.CloseResource(pending.Cancellation);
        if (!completion.Succeeded)
        {
            RecordFaultEvent("pending data transfer completion");
        }
        return null;
    }

    internal void CompletePendingTransfer(
        FtpServerPendingTransfer pending,
        FtpFaultIsolationResult operation,
        FtpReply? finalReply)
    {
        lock (CommandLock)
        {
            bool owned = object.ReferenceEquals(PendingTransfer, pending);
            if (owned)
            {
                PendingTransfer = null;
            }
            if (!pending.Aborted && !Closing && owned)
            {
                if (!operation.Succeeded || finalReply == null)
                {
                    RecordFaultEvent("pending data transfer");
                    finalReply = CreateReply(
                        FtpReplyCodes.ConnectionClosedTransferAborted,
                        "Data connection closed; transfer aborted.");
                }
                WriteReply(finalReply, pending.Command);
                State.ApplyCommandReply(pending.Command, finalReply);
            }
        }
    }

    internal bool AbortPendingTransfer()
    {
        FtpServerPendingTransfer? pending = PendingTransfer;
        if (pending == null)
        {
            return false;
        }
        pending.Aborted = true;
        pending.Cancellation.Cancel();
        return true;
    }

    internal void StopPendingTransfer()
    {
        FtpServerPendingTransfer? pending;
        lock (CommandLock)
        {
            pending = PendingTransfer;
            if (pending != null)
            {
                pending.Aborted = true;
                pending.Cancellation.Cancel();
                if (!pending.Started)
                {
                    pending.Started = true;
                    pending.Worker = Task.Run<object?>(() =>
                    {
                        return RunPendingTransfer(pending);
                    });
                }
            }
        }
        Context.AbortTransferResources();
        if (pending != null && pending.Worker != null)
        {
            Task timeout = Task.Delay(
                Settings.Timeouts.DataConnectionTimeoutMilliseconds);
            Task completion = Task.WhenAny(
                new Task[] { pending.Worker, timeout });
            completion.Wait();
        }
    }

    internal FtpCommandResult? DispatchExtensionCommand(FtpCommand command)
    {
        if (ActiveExtensionHandlers.Length == 0)
        {
            return null;
        }
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(command, State.State);
        if (!sequence.Allowed)
        {
            return CreateCommandResult(
                command, sequence.ReplyCode, sequence.Message, false);
        }
        if (command.Kind == FtpCommandKind.Site)
        {
            FtpAuthorizationGate gate = new FtpAuthorizationGate(
                ActiveAuthorizer, ActiveHost, EventSink);
            FtpAuthorizationResult authorization = gate.AuthorizeSiteOperation(
                State.State.Principal,
                command.Argument,
                State.State.ControlProtected);
            if (!authorization.Allowed)
            {
                return CreateCommandResult(
                    command,
                    FtpReplyCodes.RequestedActionNotTaken,
                    "The SITE operation was not authorized.",
                    false);
            }
        }

        FtpExtensionContext extensionContext =
            FtpExtensionDispatcher.CreateContext(
                command, State.State, ActiveFeatures);
        FtpExtensionDispatcher dispatcher =
            new FtpExtensionDispatcher(ActiveExtensionHandlers);
        FtpExtensionResult extensionResult =
            dispatcher.DispatchExtension(extensionContext);
        if (extensionResult.Disposition ==
            FtpExtensionDisposition.NotHandled)
        {
            return null;
        }

        RecordExtensionEvent(command, extensionResult.Disposition);
        if (extensionResult.FailureMessage ==
            "The FTP extension handler failed.")
        {
            RecordFaultEvent("extension handler");
        }
        return CreateExtensionCommandResult(command, extensionResult);
    }

    internal FtpCommandResult CreateExtensionCommandResult(
        FtpCommand command,
        FtpExtensionResult extensionResult)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = extensionResult.Replies;
        result.ConnectionClosed = extensionResult.CloseConnection;
        result.FailureMessage = extensionResult.FailureMessage;
        FtpReply finalReply =
            extensionResult.Replies[extensionResult.Replies.Length - 1];
        result.FinalReply = finalReply;
        result.Succeeded =
            extensionResult.Disposition == FtpExtensionDisposition.Handled &&
            finalReply.Code >= 200 && finalReply.Code < 400;
        return result;
    }

    internal FtpCommandResult RouteHostCommand(FtpCommand command)
    {
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(command, State.State);
        if (!sequence.Allowed)
        {
            return CreateCommandResult(
                command, sequence.ReplyCode, sequence.Message, false);
        }
        if (HostResolver == null || command.Argument == null)
        {
            return CreateCommandResult(
                command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The requested FTP virtual host is unavailable.",
                false);
        }

        FtpVirtualHostRequest request = CreateHostRequest(command.Argument);
        FtpFaultIsolationResult resolution =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return HostResolver.ResolveVirtualHost(request);
            });
        FtpVirtualHostResult? resolved =
            resolution.Value as FtpVirtualHostResult;
        if (!resolution.Succeeded || resolved == null ||
            !resolved.Handled || !resolved.Found || resolved.Host == null)
        {
            if (!resolution.Succeeded)
            {
                RecordFaultEvent("virtual-host resolution");
            }
            return CreateCommandResult(
                command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The requested FTP virtual host is unavailable.",
                false);
        }

        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpConfigurationValidator.ValidateVirtualHostOptions(
                    resolved.Host);
                return null;
            });
        if (!validation.Succeeded)
        {
            return CreateCommandResult(
                command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The requested FTP virtual host is unavailable.",
                false);
        }
        if (!ActivateHost(resolved.Host))
        {
            RecordPolicyEvent(
                "The FTP virtual-host session limit was exceeded.");
            return CreateCommandResult(
                command,
                FtpReplyCodes.ServiceUnavailable,
                "The requested FTP virtual host has no available sessions.",
                true);
        }

        return CreateCommandResult(
            command,
            FtpReplyCodes.ServiceReady,
            resolved.Host.Greeting,
            false);
    }

    internal FtpVirtualHostRequest CreateHostRequest(string hostName)
    {
        FtpVirtualHostRequest request = new FtpVirtualHostRequest();
        request.HostName = hostName;
        request.LocalEndpoint = CopySocketEndpoint(Context.Socket.LocalEndPoint);
        request.RemoteEndpoint = CopySocketEndpoint(Context.Socket.RemoteEndPoint);
        request.ImplicitTls =
            Context.ListenerSettings.SecurityMode == FtpSecurityMode.ImplicitTls;
        return request;
    }

    internal void ApplyCommandTransition(
        FtpCommand command,
        FtpReply reply)
    {
        if (command.Kind == FtpCommandKind.Host &&
            reply.Code == FtpReplyCodes.ServiceReady)
        {
            AuthenticationPrincipal = null;
            AuthenticationAttempts = 0;
        }
        if ((command.Kind == FtpCommandKind.User ||
             command.Kind == FtpCommandKind.Pass ||
             command.Kind == FtpCommandKind.Acct) &&
            (State.State.SessionState == FtpSessionState.Authenticating ||
             State.State.SessionState == FtpSessionState.Authenticated))
        {
            State.State.Principal = AuthenticationPrincipal;
        }
        if (command.Kind == FtpCommandKind.Auth &&
            reply.Code == FtpReplyCodes.SecurityExchangeComplete &&
            FtpServerLoginCommandHandler.IsTlsMechanism(command.Argument))
        {
            UpgradeExplicitControl(reply);
        }
        if (command.Kind == FtpCommandKind.Ccc &&
            reply.Code == FtpReplyCodes.CommandOkay)
        {
            ClearProtectedControl(command, reply, false);
        }
        if (command.Kind == FtpCommandKind.Rein &&
            reply.Code == FtpReplyCodes.ServiceReady)
        {
            ResetDataConnections();
            ClearProtectedControl(command, reply, true);
        }
    }

    internal void UpgradeExplicitControl(FtpReply authorizationReply)
    {
        NetworkStream? transport = TransportStream;
        if (transport == null)
        {
            throw new FtpTlsException(
                "The FTP control transport is unavailable for TLS.", null);
        }
        FtpTlsServerStreamRequest request = new FtpTlsServerStreamRequest();
        request.TransportStream = transport;
        request.Options = FtpServerLoginCommandHandler.ResolveTlsOptions(this);
        request.HandshakeTimeoutMilliseconds =
            Settings.Timeouts.TlsHandshakeTimeoutMilliseconds;
        request.CancellationToken = Settings.Timeouts.CancellationToken;
        request.LeaveTransportOpen = true;
        request.AllowTlsResume = true;
        request.CertificateProvider = ActiveCertificateProvider;
        request.HostName = ActiveHost == null ? "" : ActiveHost.HostName;
        request.LocalEndpoint = CopySocketEndpoint(Context.Socket.LocalEndPoint);
        request.RemoteEndpoint = CopySocketEndpoint(Context.Socket.RemoteEndPoint);
        FtpTlsServerStream tls = new FtpTlsServerStream(request);
        FtpTlsStreamResult result =
            tls.UpgradeExplicitServer(authorizationReply);
        if (result.Stream == null)
        {
            throw new FtpTlsException(
                "The explicit FTP TLS handshake returned no stream.", null);
        }
        Context.RegisterTlsResource(result.Stream);
        ControlTls = tls;
        ControlStream = result.Stream;
        CertificateIdentity =
            result.Diagnostics.RemoteCertificateSha256Fingerprint;
        State.CompleteSecurityExchange(true);
        FtpEvent tlsEvent = FtpTlsDiagnosticsFactory.CreateEvent(
            result,
            Context.ListenerSettings.SecurityMode,
            State.State.DataProtectionLevel,
            State.State.SessionState);
        EnrichSessionEvent(tlsEvent);
        FtpEventPipeline.RecordEvent(EventSink, tlsEvent);
    }

    internal void ClearProtectedControl(
        FtpCommand command,
        FtpReply reply,
        bool reinitialize)
    {
        FtpTlsServerStream? tls = ControlTls;
        if (tls != null)
        {
            tls.ShutdownServerTlsForClearControl();
            Context.ClearTlsResource();
            ControlTls = null;
            ControlStream = TransportStream;
        }
        if (!reinitialize)
        {
            State.CompleteClearCommandChannel(command, reply);
        }
        else
        {
            CertificateIdentity = null;
        }
    }

    internal FtpReply WriteCommandResult(
        FtpCommandResult result,
        FtpCommand command)
    {
        FtpReply? finalReply = null;
        int index = 0;
        while (index < result.Replies.Length)
        {
            FtpReply reply = result.Replies[index];
            WriteReply(reply, command);
            finalReply = reply;
            index++;
        }
        if (result.Replies.Length == 0 && result.FinalReply != null)
        {
            finalReply = result.FinalReply;
            WriteReply(finalReply, command);
        }
        if (finalReply == null)
        {
            finalReply = CreateReply(
                FtpReplyCodes.RequestedActionAbortedLocalError,
                "FTP command handler returned no reply.");
            WriteReply(finalReply, command);
        }
        return finalReply;
    }

    internal void WriteReply(FtpReply reply, FtpCommand? command)
    {
        lock (ReplyLock)
        {
            Stream? stream = ControlStream;
            if (stream == null)
            {
                throw new FtpConnectionException(
                    "The FTP server control stream is unavailable.", null);
            }

            byte[] bytes = ReplyWriter.FormatEncodedReply(
                reply,
                OptionState.Utf8Enabled ? Encoding.UTF8 : Encoding.ASCII);
            stream.WriteTimeout =
                Settings.Timeouts.ControlWriteTimeoutMilliseconds;
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            if (command != null)
            {
                RecordReplyEvent(command, reply);
            }
        }
    }

    internal void WriteTransferMarker(
        FtpCommand command,
        string marker)
    {
        lock (CommandLock)
        {
            FtpServerPendingTransfer? pending = PendingTransfer;
            if (pending == null || pending.Aborted || Closing ||
                pending.Command != command)
            {
                return;
            }
            WriteReply(CreateReply(
                FtpReplyCodes.RestartMarker,
                "MARK " + marker + " = " + marker), command);
        }
    }

    internal void RecordLifecycleEvent(string message)
    {
        FtpEvent eventData = CreateSessionEvent(FtpEventKind.Lifecycle, message);
        FtpClearControlAudit.RecordEvent(EventSink, eventData);
    }

    internal void RecordCommandEvent(FtpCommand command)
    {
        FtpEvent eventData = FtpTranscriptRedactor.CreateCommandEvent(command);
        EnrichSessionEvent(eventData);
        FtpClearControlAudit.RecordEvent(EventSink, eventData);
    }

    internal void RecordReplyEvent(FtpCommand command, FtpReply reply)
    {
        FtpEvent eventData = FtpTranscriptRedactor.CreateReplyEvent(
            command, reply);
        EnrichSessionEvent(eventData);
        FtpClearControlAudit.RecordEvent(EventSink, eventData);
    }

    internal void RecordExtensionEvent(
        FtpCommand command,
        FtpExtensionDisposition disposition)
    {
        FtpEvent eventData = FtpEventPipeline.CreateBaseEvent(
            FtpEventKind.Command);
        eventData.Command = command;
        eventData.HasCommandKind = true;
        eventData.CommandKind = command.Kind;
        eventData.HasExtensionDisposition = true;
        eventData.ExtensionDisposition = disposition;
        EnrichSessionEvent(eventData);
        FtpEventPipeline.RecordEvent(EventSink, eventData);
    }

    internal void RecordAuthenticationEvent(
        FtpAuthenticationRequest request,
        FtpAuthenticationResult result)
    {
        FtpEvent eventData =
            FtpEventPipeline.CreateAuthenticationEvent(request, result);
        EnrichSessionEvent(eventData);
        FtpEventPipeline.RecordEvent(EventSink, eventData);
    }

    internal void RecordPolicyEvent(string message)
    {
        FtpEvent eventData = FtpEventPipeline.CreatePolicyEvent(message);
        EnrichSessionEvent(eventData);
        FtpEventPipeline.RecordEvent(EventSink, eventData);
    }

    internal void RecordFaultEvent(string activity)
    {
        FtpEvent eventData = FtpEventPipeline.CreateFaultEvent(activity);
        EnrichSessionEvent(eventData);
        FtpEventPipeline.RecordEvent(EventSink, eventData);
    }

    internal FtpEvent CreateSessionEvent(FtpEventKind kind, string message)
    {
        FtpEvent eventData = FtpEventPipeline.CreateBaseEvent(kind);
        eventData.Message = message;
        EnrichSessionEvent(eventData);
        return eventData;
    }

    internal void EnrichSessionEvent(FtpEvent eventData)
    {
        eventData.HasSecurityMode = true;
        eventData.SecurityMode = Context.ListenerSettings.SecurityMode;
        eventData.HasSessionState = true;
        eventData.SessionState = State.State.SessionState;
        eventData.RemoteEndpoint =
            CopySocketEndpoint(Context.Socket.RemoteEndPoint);
        eventData.Redacted = true;
    }

    internal static FtpCommandResult CreateFailureResult(FtpCommand command)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.FinalReply = CreateReply(
            FtpReplyCodes.RequestedActionAbortedLocalError,
            "FTP command handler failed.");
        result.FailureMessage = "The FTP command handler failed.";
        return result;
    }

    internal static FtpCommandResult CreateCommandResult(
        FtpCommand command,
        int code,
        string text,
        bool closeConnection)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.FinalReply = CreateReply(code, text);
        result.ConnectionClosed = closeConnection;
        result.Succeeded = code >= 200 && code < 300;
        return result;
    }

    internal static FtpCommandResult CreateNotImplementedResult(
        FtpServerCommandContext commandContext)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = commandContext.Command;
        result.FinalReply = CreateReply(
            FtpReplyCodes.CommandNotImplemented,
            "FTP command is not implemented.");
        return result;
    }

    internal static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = FtpEndpointCodec.CreateEndpointReply(code, text);
        reply.RawText = code.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + " " + text;
        return reply;
    }

    internal static FtpEndpoint? CopySocketEndpoint(EndPoint? endpoint)
    {
        IPEndPoint? ipEndpoint = endpoint as IPEndPoint;
        if (ipEndpoint == null)
        {
            return null;
        }

        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = ipEndpoint.Address.ToString();
        copy.Port = ipEndpoint.Port;
        copy.AddressFamily =
            ipEndpoint.AddressFamily == AddressFamily.InterNetwork ?
                FtpAddressFamily.IPv4 : FtpAddressFamily.IPv6;
        return copy;
    }
}
