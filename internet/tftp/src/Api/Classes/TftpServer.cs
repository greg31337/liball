using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tftp;

/// <summary>
/// Runs concurrent TFTP read and write sessions over one or more UDP listeners.
/// </summary>
public class TftpServer : ITftpServer
{
    internal ITftpServerStorage? ConfiguredStorage;
    internal ITftpTransferObserver? Observer;
    internal object SyncRoot;
    internal TftpServerListenerLifecycle? CurrentLifecycle;
    internal TftpSessionManager? CurrentSessionManager;
    internal TftpServerStatus LastStatus;

    /// <summary>
    /// Initializes a server with optional custom storage and transfer observation.
    /// </summary>
    /// <param name="storage">
    /// The custom storage provider, or null to construct directory storage from
    /// <see cref="TftpServerOptions.DirectoryStorage"/> when the server runs.
    /// </param>
    /// <param name="observer">The optional transfer observer.</param>
    public TftpServer(
        ITftpServerStorage? storage,
        ITftpTransferObserver? observer)
    {
        ConfiguredStorage = storage;
        Observer = observer;
        SyncRoot = new object();
        CurrentLifecycle = null;
        CurrentSessionManager = null;
        LastStatus = new TftpServerStatus();
    }

    /// <summary>
    /// Binds the configured endpoints and runs until cancellation or an explicit stop.
    /// </summary>
    /// <param name="options">The listener, transfer, and storage settings.</param>
    /// <param name="cancellationToken">A token that requests the configured stop mode.</param>
    /// <exception cref="TftpProtocolException">
    /// Thrown when the options are invalid or this server is already running.
    /// </exception>
    /// <exception cref="TftpStorageException">
    /// Thrown when neither custom nor directory-backed storage is configured.
    /// </exception>
    public void Run(
        TftpServerOptions options,
        CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new TftpProtocolException(
                "The server options cannot be null.",
                false,
                0);
        }

        TftpServerOptions optionsSnapshot = CopyRunOptions(options);
        ITftpServerStorage storage = SelectStorage(optionsSnapshot);
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(optionsSnapshot);
        TftpSessionManager sessionManager = new TftpSessionManager(
            lifecycle,
            Observer);
        RegisterServerRun(lifecycle, sessionManager);
        using (TftpServerRunGuard runGuard = new TftpServerRunGuard(
            this,
            lifecycle,
            sessionManager))
        {
            lifecycle.BindListeners(cancellationToken);
            bool cancellationApplied = false;
            while (lifecycle.GetStatusSnapshot().IsRunning)
            {
                sessionManager.ProcessCompletedSessions();
                if (cancellationToken.IsCancellationRequested &&
                    !cancellationApplied)
                {
                    cancellationApplied = true;
                    sessionManager.StopSessions(optionsSnapshot.StopMode);
                }

                TftpServerStatus status = lifecycle.GetStatusSnapshot();
                if (!status.IsRunning)
                {
                    break;
                }

                if (status.IsDraining)
                {
                    Thread.Sleep(5);
                    continue;
                }

                TftpListenerPollResult pollResult = lifecycle.PollListeners(
                    25,
                    CancellationToken.None);
                if (pollResult.HasDatagram)
                {
                    ProcessListenerDatagram(
                        pollResult,
                        sessionManager,
                        storage,
                        optionsSnapshot);
                }
            }

            sessionManager.ProcessCompletedSessions();
        }
    }

    /// <summary>
    /// Stops accepting requests and either drains or cancels active transfers.
    /// </summary>
    /// <param name="mode">The requested graceful or immediate stop mode.</param>
    /// <exception cref="TftpProtocolException">
    /// Thrown when <c>mode</c> is not defined.
    /// </exception>
    public void Stop(TftpServerStopMode mode)
    {
        TftpServerListenerLifecycle.ValidateStopMode(mode);
        TftpSessionManager? sessionManager;
        lock (SyncRoot)
        {
            sessionManager = CurrentSessionManager;
        }

        if (sessionManager != null)
        {
            sessionManager.StopSessions(mode);
        }
    }

    /// <summary>
    /// Gets an independent snapshot of listener state and transfer counts.
    /// </summary>
    /// <returns>The current or most recently completed run status.</returns>
    public TftpServerStatus GetStatus()
    {
        lock (SyncRoot)
        {
            TftpServerListenerLifecycle? lifecycle = CurrentLifecycle;
            if (lifecycle != null)
            {
                return lifecycle.GetStatusSnapshot();
            }

            return CopyStatus(LastStatus);
        }
    }

    internal void ProcessListenerDatagram(
        TftpListenerPollResult pollResult,
        TftpSessionManager sessionManager,
        ITftpServerStorage storage,
        TftpServerOptions options)
    {
        UdpDatagramSocket? listener = pollResult.Listener;
        UdpDatagramReceiveResult? datagram = pollResult.Datagram;
        if (listener == null || datagram == null)
        {
            return;
        }

        TftpServerInitialDatagramResult initialResult =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                pollResult.Buffer,
                datagram,
                options.MaximumRequestPacketLength);
        if (initialResult.Action ==
            TftpServerInitialDatagramAction.Ignore)
        {
            return;
        }

        if (initialResult.Action ==
            TftpServerInitialDatagramAction.SendError)
        {
            SendListenerError(
                listener,
                datagram.RemoteEndPoint,
                initialResult.ErrorPacket);
            return;
        }

        TftpPacket requestPacket = initialResult.RequestPacket;

        ITftpServerSessionHandler handler;
        if (requestPacket.Opcode == TftpOpcode.ReadRequest)
        {
            handler = new TftpServerReadSessionHandler(storage, options);
        }
        else
        {
            handler = new TftpServerWriteSessionHandler(storage, options);
        }

        IPAddress localAddress = datagram.DestinationAddress;

        TftpServerSessionStartResult startResult =
            sessionManager.StartSession(
                datagram.RemoteEndPoint,
                localAddress,
                requestPacket,
                handler,
                CancellationToken.None);
        if (startResult.Action ==
            TftpSessionAdmissionAction.SessionLimitReached)
        {
            TftpPacket error = TftpServerErrorMapper.CreateErrorPacket(
                TftpErrorCode.StorageExhausted,
                "The server session limit has been reached.",
                options.MaximumRequestPacketLength);
            SendListenerError(listener, datagram.RemoteEndPoint, error);
        }
        else if (startResult.Action ==
            TftpSessionAdmissionAction.ServerStopping)
        {
            TftpPacket error = TftpServerErrorMapper.CreateErrorPacket(
                TftpErrorCode.Undefined,
                "The server is stopping and cannot accept new work.",
                options.MaximumRequestPacketLength);
            SendListenerError(listener, datagram.RemoteEndPoint, error);
        }
    }

    internal void SendListenerError(
        UdpDatagramSocket listener,
        IPEndPoint remoteEndPoint,
        TftpPacket? errorPacket)
    {
        if (errorPacket == null)
        {
            return;
        }

        byte[] encoded = TftpServerErrorMapper.EncodeErrorPacket(errorPacket);
        listener.Socket.SendTo(
            encoded,
            SocketFlags.None,
            remoteEndPoint);
    }

    internal ITftpServerStorage SelectStorage(TftpServerOptions options)
    {
        ITftpServerStorage? storage = ConfiguredStorage;
        if (storage != null)
        {
            return storage;
        }

        TftpDirectoryStorageOptions? directoryOptions =
            options.DirectoryStorage;
        if (directoryOptions == null)
        {
            throw new TftpStorageException(
                "The server requires custom or directory-backed storage.",
                "RunServer",
                null);
        }

        return new TftpDirectoryStorage(directoryOptions);
    }

    internal void RegisterServerRun(
        TftpServerListenerLifecycle lifecycle,
        TftpSessionManager sessionManager)
    {
        lock (SyncRoot)
        {
            if (CurrentLifecycle != null)
            {
                throw new TftpProtocolException(
                    "The server is already running.",
                    false,
                    0);
            }

            CurrentLifecycle = lifecycle;
            CurrentSessionManager = sessionManager;
            LastStatus = new TftpServerStatus();
        }
    }

    internal void FinishServerRun(TftpServerListenerLifecycle lifecycle)
    {
        lock (SyncRoot)
        {
            if (CurrentLifecycle == lifecycle)
            {
                LastStatus = lifecycle.GetStatusSnapshot();
                CurrentLifecycle = null;
                CurrentSessionManager = null;
            }
        }
    }

    internal static TftpServerOptions CopyRunOptions(TftpServerOptions source)
    {
        if (source.Bindings == null)
        {
            throw new TftpProtocolException(
                "The server bindings cannot be null.",
                false,
                0);
        }

        TftpServerOptions copy =
            TftpServerReadStateMachine.CopyServerOptions(source);
        foreach (TftpServerBinding sourceBinding in source.Bindings)
        {
            if (sourceBinding == null)
            {
                throw new TftpProtocolException(
                    "A server binding cannot be null.",
                    false,
                    0);
            }

            TftpServerBinding binding = new TftpServerBinding();
            binding.Address = sourceBinding.Address;
            binding.Port = sourceBinding.Port;
            copy.Bindings.Add(binding);
        }

        TftpDirectoryStorageOptions? sourceDirectory = source.DirectoryStorage;
        if (sourceDirectory != null)
        {
            TftpDirectoryStorageOptions directory =
                new TftpDirectoryStorageOptions();
            directory.RootPath = sourceDirectory.RootPath;
            directory.AllowRead = sourceDirectory.AllowRead;
            directory.AllowWrite = sourceDirectory.AllowWrite;
            directory.AllowCreate = sourceDirectory.AllowCreate;
            directory.AllowOverwrite = sourceDirectory.AllowOverwrite;
            directory.AllowSubdirectories =
                sourceDirectory.AllowSubdirectories;
            directory.RejectSymbolicLinks =
                sourceDirectory.RejectSymbolicLinks;
            copy.DirectoryStorage = directory;
        }

        return copy;
    }

    internal static TftpServerStatus CopyStatus(TftpServerStatus source)
    {
        TftpServerStatus copy = new TftpServerStatus();
        copy.IsRunning = source.IsRunning;
        copy.IsDraining = source.IsDraining;
        copy.ActiveTransferCount = source.ActiveTransferCount;
        copy.AcceptedTransferCount = source.AcceptedTransferCount;
        copy.CompletedTransferCount = source.CompletedTransferCount;
        copy.FailedTransferCount = source.FailedTransferCount;
        foreach (TftpServerBinding sourceBinding in source.BoundEndpoints)
        {
            TftpServerBinding binding = new TftpServerBinding();
            binding.Address = sourceBinding.Address;
            binding.Port = sourceBinding.Port;
            copy.BoundEndpoints.Add(binding);
        }

        return copy;
    }
}
