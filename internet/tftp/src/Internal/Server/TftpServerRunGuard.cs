using System;

namespace tftp;

internal class TftpServerRunGuard : IDisposable
{
    internal TftpServer Server;
    internal TftpServerListenerLifecycle Lifecycle;
    internal TftpSessionManager SessionManager;
    internal bool Disposed;

    internal TftpServerRunGuard(
        TftpServer server,
        TftpServerListenerLifecycle lifecycle,
        TftpSessionManager sessionManager)
    {
        Server = server;
        Lifecycle = lifecycle;
        SessionManager = sessionManager;
        Disposed = false;
    }

    /// <summary>
    /// Stops remaining sessions and releases the registered server run.
    /// </summary>
    public void Dispose()
    {
        if (Disposed)
        {
            return;
        }

        SessionManager.StopSessions(TftpServerStopMode.Immediate);
        Server.FinishServerRun(Lifecycle);
        Disposed = true;
    }
}
