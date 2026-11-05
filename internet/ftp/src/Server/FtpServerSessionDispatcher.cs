using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpServerSessionDispatcher
{
    internal object StateLock;
    internal SemaphoreSlim Capacity;
    internal List<Task<object?>> SessionTasks;
    internal List<FtpServerSessionContext> ActiveContexts;
    internal ManualResetEventSlim Drained;
    internal Func<FtpServerSessionContext, object?> SessionOperation;
    internal FtpServerLimitManager Limits;
    internal int AcceptedSessions;
    internal int ActiveSessions;
    internal int PeakActiveSessions;
    internal int CompletedSessions;
    internal int FailedSessions;
    internal int ForcedSessions;
    internal int RejectedSessions;

    internal FtpServerSessionDispatcher(
        int maximumPendingConnections,
        Func<FtpServerSessionContext, object?> sessionOperation,
        FtpServerLimitManager limits)
    {
        StateLock = new object();
        Capacity = new SemaphoreSlim(
            maximumPendingConnections, maximumPendingConnections);
        SessionTasks = new List<Task<object?>>();
        ActiveContexts = new List<FtpServerSessionContext>();
        Drained = new ManualResetEventSlim(true);
        SessionOperation = sessionOperation;
        Limits = limits;
        AcceptedSessions = 0;
        ActiveSessions = 0;
        PeakActiveSessions = 0;
        CompletedSessions = 0;
        FailedSessions = 0;
        ForcedSessions = 0;
        RejectedSessions = 0;
    }

    internal bool WaitForCapacity(FtpServerListener listener)
    {
        while (!listener.Stopping)
        {
            WaitHandle[] signals = new WaitHandle[]
            {
                Capacity.AvailableWaitHandle,
                listener.StopSignal.WaitHandle
            };
            int signalIndex = WaitHandle.WaitAny(signals);
            if (signalIndex != 0 || listener.Stopping)
            {
                return false;
            }
            if (Capacity.Wait(0))
            {
                return true;
            }
        }
        return false;
    }

    internal void ReleaseCapacity()
    {
        Capacity.Release();
    }

    internal void DispatchSession(
        Socket socket,
        FtpListenerOptions listenerSettings)
    {
        FtpServerSessionContext context =
            new FtpServerSessionContext(socket, listenerSettings, Limits);
        bool admitted = Limits.TryRegisterSession(context);
        lock (StateLock)
        {
            AcceptedSessions++;
            if (!admitted)
            {
                RejectedSessions++;
            }
        }
        if (!admitted)
        {
            context.CloseResources();
            Capacity.Release();
            return;
        }

        lock (StateLock)
        {
            RemoveCompletedTasks();
            if (ActiveSessions == 0)
            {
                Drained.Reset();
            }
            ActiveSessions++;
            ActiveContexts.Add(context);
            if (ActiveSessions > PeakActiveSessions)
            {
                PeakActiveSessions = ActiveSessions;
            }
        }

        Task<object?> task = Task.Run(() =>
        {
            return ExecuteSession(context);
        });
        lock (StateLock)
        {
            SessionTasks.Add(task);
        }
    }

    internal object? ExecuteSession(FtpServerSessionContext context)
    {
        FtpFaultIsolationResult operationResult =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpSocketSettings.ApplyConnectedSocket(
                    context.Socket, context.ListenerSettings.ControlSocket);
                return SessionOperation(context);
            });
        FtpFaultIsolationResult cleanupResult =
            context.CloseResources();
        lock (StateLock)
        {
            ActiveContexts.Remove(context);
            ActiveSessions--;
            CompletedSessions++;
            if (!operationResult.Succeeded || !cleanupResult.Succeeded)
            {
                FailedSessions++;
            }
            if (ActiveSessions == 0)
            {
                Drained.Set();
            }
        }
        Capacity.Release();
        return operationResult;
    }

    internal void RemoveCompletedTasks()
    {
        int index = SessionTasks.Count - 1;
        while (index >= 0)
        {
            if (SessionTasks[index].IsCompleted)
            {
                SessionTasks.RemoveAt(index);
            }
            index--;
        }
    }

    internal int GetAcceptedSessionCount()
    {
        lock (StateLock)
        {
            return AcceptedSessions;
        }
    }

    internal int GetActiveSessionCount()
    {
        lock (StateLock)
        {
            return ActiveSessions;
        }
    }

    internal int GetPeakActiveSessionCount()
    {
        lock (StateLock)
        {
            return PeakActiveSessions;
        }
    }

    internal int GetCompletedSessionCount()
    {
        lock (StateLock)
        {
            return CompletedSessions;
        }
    }

    internal int GetFailedSessionCount()
    {
        lock (StateLock)
        {
            return FailedSessions;
        }
    }

    internal bool WaitForDrain(int timeoutMilliseconds)
    {
        return Drained.Wait(timeoutMilliseconds);
    }

    internal FtpFaultIsolationResult ForceCloseSessions()
    {
        FtpServerSessionContext[] contexts;
        lock (StateLock)
        {
            contexts = ActiveContexts.ToArray();
            ForcedSessions += contexts.Length;
        }

        FtpFaultIsolationResult result =
            FtpServerSessionContext.CreateSuccessfulResult();
        int index = 0;
        while (index < contexts.Length)
        {
            FtpFaultIsolationResult cleanup = contexts[index].CloseResources();
            if (!cleanup.Succeeded && result.Succeeded)
            {
                result = cleanup;
            }
            index++;
        }
        return result;
    }

    internal int GetForcedSessionCount()
    {
        lock (StateLock)
        {
            return ForcedSessions;
        }
    }

    internal int GetRejectedSessionCount()
    {
        lock (StateLock)
        {
            return RejectedSessions;
        }
    }
}
