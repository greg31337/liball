using System.Collections.Generic;
using System;

namespace ftp;

internal class FtpServerRuntime
{
    internal object StateLock;
    internal IFtpFileSystem? FileSystem;
    internal IFtpAuthenticator? Authenticator;
    internal IFtpAuthorizer? Authorizer;
    internal IFtpHostResolver? HostResolver;
    internal IFtpCertificateProvider? CertificateProvider;
    internal IFtpEventSink? EventSink;
    internal FtpServerOptions? Settings;
    internal List<FtpServerListener> Listeners;
    internal Func<FtpServerSessionContext, object?> SessionOperation;
    internal Func<FtpServerCommandContext, FtpCommandResult> CommandOperation;
    internal FtpServerSessionDispatcher? SessionDispatcher;
    internal FtpServerLimitManager? Limits;
    internal FtpPassivePortPool? PassivePortPool;
    internal FtpServerState State;

    internal FtpServerRuntime(
        IFtpFileSystem? fileSystem,
        IFtpAuthenticator? authenticator,
        IFtpAuthorizer? authorizer,
        IFtpHostResolver? hostResolver,
        IFtpCertificateProvider? certificateProvider,
        IFtpEventSink? eventSink)
    {
        FtpConfigurationValidator.ValidateServerProviderPresence(
            fileSystem,
            authenticator,
            authorizer,
            hostResolver,
            certificateProvider,
            eventSink);
        StateLock = new object();
        FileSystem = fileSystem;
        Authenticator = authenticator;
        Authorizer = authorizer;
        HostResolver = hostResolver;
        CertificateProvider = certificateProvider;
        EventSink = eventSink;
        Settings = null;
        Listeners = new List<FtpServerListener>();
        CommandOperation = commandContext =>
            FtpServerLoginCommandHandler.HandleCommand(commandContext);
        SessionOperation = context => RunSession(context);
        SessionDispatcher = null;
        Limits = null;
        PassivePortPool = null;
        State = FtpServerState.Stopped;
    }

    internal object? RunSession(FtpServerSessionContext context)
    {
        FtpServerOptions? settings = Settings;
        if (settings == null)
        {
            throw new FtpConfigurationException(
                "FTP server runtime settings are unavailable.", null);
        }
        IFtpAuthenticator? authenticator = settings.FixedUsers == null ?
            Authenticator : new FixedUserFtpAuthenticator(settings.FixedUsers);

        FtpServerSession session = new FtpServerSession(
            context,
            settings,
            FileSystem,
            authenticator,
            Authorizer,
            HostResolver,
            CertificateProvider,
            EventSink,
            CommandOperation);
        session.InitializeDataConnections(PassivePortPool);
        return session.RunSession();
    }

    internal void StartServer(FtpServerOptions? options)
    {
        lock (StateLock)
        {
            if (State != FtpServerState.Stopped)
            {
                throw new FtpProtocolException(
                    "An FTP server can be started only when stopped.", null);
            }

            FtpServerOptions snapshot =
                FtpServerOptionsSnapshot.CreateValidated(options);
            State = FtpServerState.Starting;
            Settings = snapshot;
            Limits = new FtpServerLimitManager(snapshot);
            PassivePortPool = new FtpPassivePortPool(
                snapshot.DataConnection.PassivePortStart,
                snapshot.DataConnection.PassivePortEnd);
            SessionDispatcher = new FtpServerSessionDispatcher(
                snapshot.MaximumSessions, SessionOperation, Limits);
            FtpFaultIsolationResult startResult =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    StartListeners(snapshot);
                    StartAcceptLoops();
                    return null;
                });
            if (!startResult.Succeeded)
            {
                FtpFaultIsolationResult cleanupResult = CloseListeners();
                State = FtpServerState.Faulted;
                System.Exception? failure = startResult.Failure == null ?
                    cleanupResult.Failure : startResult.Failure;
                throw new FtpConnectionException(
                    "The FTP server could not start every configured listener.",
                    failure);
            }
            State = FtpServerState.Running;
        }
    }

    internal void StopServer()
    {
        FtpServerOptions? settings;
        FtpServerSessionDispatcher? dispatcher;
        bool drain;
        lock (StateLock)
        {
            if (State == FtpServerState.Stopped)
            {
                return;
            }

            if (State == FtpServerState.Draining ||
                State == FtpServerState.Stopping)
            {
                throw new FtpProtocolException(
                    "The FTP server is already stopping.", null);
            }

            drain = State == FtpServerState.Running;
            State = drain ? FtpServerState.Draining : FtpServerState.Stopping;
            settings = Settings;
            dispatcher = SessionDispatcher;
        }

        int gracefulTimeoutMilliseconds = settings == null ?
            1 : settings.Timeouts.GracefulStopTimeoutMilliseconds;
        FtpDeadline deadline = FtpTimeout.StartDeadline(
            gracefulTimeoutMilliseconds);
        FtpFaultIsolationResult listenerCleanup = CloseListeners();
        bool drained = dispatcher == null || dispatcher.GetActiveSessionCount() == 0;
        if (drain && !drained && !FtpTimeout.HasDeadlineExpired(deadline))
        {
            int remainingMilliseconds =
                FtpTimeout.GetRemainingWaitMilliseconds(deadline);
            drained = dispatcher == null ||
                dispatcher.WaitForDrain(remainingMilliseconds);
        }

        lock (StateLock)
        {
            State = FtpServerState.Stopping;
        }

        FtpFaultIsolationResult sessionCleanup =
            FtpServerSessionContext.CreateSuccessfulResult();
        if (!drained && dispatcher != null)
        {
            sessionCleanup = dispatcher.ForceCloseSessions();
        }

        if (!listenerCleanup.Succeeded || !sessionCleanup.Succeeded)
        {
            lock (StateLock)
            {
                State = FtpServerState.Faulted;
            }
            System.Exception? failure = !listenerCleanup.Succeeded ?
                listenerCleanup.Failure : sessionCleanup.Failure;
            throw new FtpConnectionException(
                "The FTP server could not close every listener and session resource.",
                failure);
        }

        lock (StateLock)
        {
            PassivePortPool = null;
            State = FtpServerState.Stopped;
        }
    }

    internal FtpServerState GetServerState()
    {
        lock (StateLock)
        {
            return State;
        }
    }

    internal void StartListeners(FtpServerOptions settings)
    {
        int index = 0;
        while (index < settings.Listeners.Length)
        {
            FtpServerListener listener =
                new FtpServerListener(settings.Listeners[index]);
            Listeners.Add(listener);
            listener.StartListener();
            index++;
        }
    }

    internal FtpFaultIsolationResult CloseListeners()
    {
        FtpFaultIsolationResult result = new FtpFaultIsolationResult();
        result.Succeeded = true;
        int index = Listeners.Count - 1;
        while (index >= 0)
        {
            FtpFaultIsolationResult closeResult =
                Listeners[index].CloseListener();
            if (!closeResult.Succeeded && result.Succeeded)
            {
                result = closeResult;
            }
            index--;
        }
        Listeners.Clear();
        return result;
    }

    internal void StartAcceptLoops()
    {
        FtpServerSessionDispatcher? dispatcher = SessionDispatcher;
        if (dispatcher == null)
        {
            throw new FtpConnectionException(
                "The FTP server session dispatcher is unavailable.", null);
        }

        int index = 0;
        while (index < Listeners.Count)
        {
            Listeners[index].StartAcceptLoop(dispatcher);
            index++;
        }
    }
}
