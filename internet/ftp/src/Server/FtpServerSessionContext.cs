using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ftp;

internal class FtpServerSessionContext
{
    internal Socket Socket;
    internal FtpListenerOptions ListenerSettings;
    internal FtpServerLimitManager Limits;
    internal string RemoteAddress;
    internal object ResourceLock;
    internal Socket? PassiveSocket;
    internal Socket? DataSocket;
    internal IDisposable? TlsResource;
    internal IDisposable? FileResource;
    internal bool ResourcesClosed;
    internal bool Admitted;
    internal bool PendingLogin;
    internal string? AuthenticatedUser;
    internal string? SelectedHost;
    internal int MaximumHostSessionsPerUser;
    internal bool TransferActive;
    internal long LastActivityMilliseconds;
    internal Queue<long> CommandTimes;
    internal int MalformedLines;

    internal FtpServerSessionContext(
        Socket socket,
        FtpListenerOptions listenerSettings,
        FtpServerLimitManager limits)
    {
        Socket = socket;
        ListenerSettings = listenerSettings;
        Limits = limits;
        IPEndPoint? remoteEndpoint = socket.RemoteEndPoint as IPEndPoint;
        RemoteAddress = remoteEndpoint == null ? "" :
            FtpPeerPolicy.NormalizeAddress(remoteEndpoint.Address).ToString();
        ResourceLock = new object();
        PassiveSocket = null;
        DataSocket = null;
        TlsResource = null;
        FileResource = null;
        ResourcesClosed = false;
        Admitted = false;
        PendingLogin = false;
        AuthenticatedUser = null;
        SelectedHost = null;
        MaximumHostSessionsPerUser = 0;
        TransferActive = false;
        LastActivityMilliseconds = 0;
        CommandTimes = new Queue<long>();
        MalformedLines = 0;
    }

    internal bool TryAuthenticateUser(string userName)
    {
        return Limits.TryAuthenticateUser(this, userName);
    }

    internal bool TryResetAuthentication()
    {
        return Limits.TryResetAuthentication(this);
    }

    internal bool TrySelectHost(
        string hostName,
        int maximumSessions,
        int maximumSessionsPerUser)
    {
        return Limits.TrySelectHost(
            this, hostName, maximumSessions, maximumSessionsPerUser);
    }

    internal void ResetHostSelection()
    {
        Limits.ResetHostSelection(this);
    }

    internal bool TryRecordCommand(long currentMilliseconds)
    {
        return Limits.TryRecordCommand(this, currentMilliseconds);
    }

    internal bool TryRecordMalformedLine()
    {
        return Limits.TryRecordMalformedLine(this);
    }

    internal void RecordActivity(long currentMilliseconds)
    {
        Limits.RecordActivity(this, currentMilliseconds);
    }

    internal bool HasIdleExpired(long currentMilliseconds)
    {
        return Limits.HasIdleExpired(this, currentMilliseconds);
    }

    internal bool TryBeginTransfer()
    {
        return Limits.TryBeginTransfer(this);
    }

    internal void EndTransfer()
    {
        Limits.EndTransfer(this);
    }

    internal void RegisterPassiveSocket(Socket? socket)
    {
        RegisterSocketResource(socket, true);
    }

    internal void RegisterDataSocket(Socket? socket)
    {
        RegisterSocketResource(socket, false);
    }

    internal void ClearPassiveSocket()
    {
        lock (ResourceLock)
        {
            PassiveSocket = null;
        }
    }

    internal void ClearDataSocket()
    {
        lock (ResourceLock)
        {
            DataSocket = null;
        }
    }

    internal void ClearFileResource()
    {
        lock (ResourceLock)
        {
            FileResource = null;
        }
    }

    internal FtpFaultIsolationResult AbortTransferResources()
    {
        Socket? dataSocket;
        IDisposable? fileResource;
        bool transferActive;
        lock (ResourceLock)
        {
            dataSocket = DataSocket;
            fileResource = FileResource;
            DataSocket = null;
            FileResource = null;
            transferActive = TransferActive;
        }

        FtpFaultIsolationResult result = CloseResource(dataSocket);
        FtpFaultIsolationResult fileCleanup = CloseResource(fileResource);
        if (!fileCleanup.Succeeded && result.Succeeded)
        {
            result = fileCleanup;
        }
        if (transferActive)
        {
            EndTransfer();
        }
        return result;
    }

    internal void RegisterTlsResource(IDisposable? resource)
    {
        RegisterDisposableResource(resource, true);
    }

    internal void ClearTlsResource()
    {
        lock (ResourceLock)
        {
            TlsResource = null;
        }
    }

    internal void RegisterFileResource(IDisposable? resource)
    {
        RegisterDisposableResource(resource, false);
    }

    internal void RegisterSocketResource(Socket? socket, bool passive)
    {
        if (socket == null)
        {
            throw new FtpConfigurationException(
                "A server session socket resource is required.", null);
        }

        bool closeImmediately = false;
        lock (ResourceLock)
        {
            if (ResourcesClosed)
            {
                closeImmediately = true;
            }
            else if (passive)
            {
                PassiveSocket = socket;
            }
            else
            {
                DataSocket = socket;
            }
        }
        if (closeImmediately)
        {
            FtpFaultIsolationResult cleanup = CloseResource(socket);
            if (!cleanup.Succeeded)
            {
                throw new FtpConnectionException(
                    "A late server session socket could not be closed.",
                    cleanup.Failure);
            }
        }
    }

    internal void RegisterDisposableResource(
        IDisposable? resource,
        bool tlsResource)
    {
        if (resource == null)
        {
            throw new FtpConfigurationException(
                "A server session disposable resource is required.", null);
        }

        bool closeImmediately = false;
        lock (ResourceLock)
        {
            if (ResourcesClosed)
            {
                closeImmediately = true;
            }
            else if (tlsResource)
            {
                TlsResource = resource;
            }
            else
            {
                FileResource = resource;
            }
        }
        if (closeImmediately)
        {
            FtpFaultIsolationResult cleanup = CloseResource(resource);
            if (!cleanup.Succeeded)
            {
                throw new FtpConnectionException(
                    "A late server session resource could not be closed.",
                    cleanup.Failure);
            }
        }
    }

    internal FtpFaultIsolationResult CloseResources()
    {
        Socket? passiveSocket = null;
        Socket? dataSocket = null;
        IDisposable? tlsResource = null;
        IDisposable? fileResource = null;
        bool closeControl = false;
        lock (ResourceLock)
        {
            if (!ResourcesClosed)
            {
                ResourcesClosed = true;
                passiveSocket = PassiveSocket;
                dataSocket = DataSocket;
                tlsResource = TlsResource;
                fileResource = FileResource;
                PassiveSocket = null;
                DataSocket = null;
                TlsResource = null;
                FileResource = null;
                closeControl = true;
            }
        }

        FtpFaultIsolationResult result = CreateSuccessfulResult();
        if (!closeControl)
        {
            return result;
        }

        Limits.ReleaseSession(this);

        FtpFaultIsolationResult passiveCleanup = CloseResource(passiveSocket);
        if (!passiveCleanup.Succeeded)
        {
            result = passiveCleanup;
        }
        FtpFaultIsolationResult dataCleanup = CloseResource(dataSocket);
        if (!dataCleanup.Succeeded && result.Succeeded)
        {
            result = dataCleanup;
        }
        FtpFaultIsolationResult tlsCleanup = CloseResource(tlsResource);
        if (!tlsCleanup.Succeeded && result.Succeeded)
        {
            result = tlsCleanup;
        }
        FtpFaultIsolationResult fileCleanup = CloseResource(fileResource);
        if (!fileCleanup.Succeeded && result.Succeeded)
        {
            result = fileCleanup;
        }
        FtpFaultIsolationResult controlCleanup = CloseResource(Socket);
        if (!controlCleanup.Succeeded && result.Succeeded)
        {
            result = controlCleanup;
        }
        return result;
    }

    internal static FtpFaultIsolationResult CloseResource(
        IDisposable? resource)
    {
        return new FtpResourceLifetime(resource).DisposeResource();
    }

    internal static FtpFaultIsolationResult CreateSuccessfulResult()
    {
        FtpFaultIsolationResult result = new FtpFaultIsolationResult();
        result.Succeeded = true;
        return result;
    }
}
