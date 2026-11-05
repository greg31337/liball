using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpSessionManager
{
    internal TftpServerListenerLifecycle Lifecycle;
    internal ITftpTransferObserver? Observer;
    internal Dictionary<string, TftpServerSession> Sessions;
    internal object SyncRoot;

    internal TftpSessionManager(
        TftpServerListenerLifecycle lifecycle,
        ITftpTransferObserver? observer)
    {
        if (lifecycle == null)
        {
            throw new TftpProtocolException(
                "The server lifecycle cannot be null.",
                false,
                0);
        }

        Lifecycle = lifecycle;
        Observer = observer;
        Sessions = new Dictionary<string, TftpServerSession>();
        SyncRoot = new object();
    }

    internal TftpServerSessionStartResult StartSession(
        IPEndPoint clientEndPoint,
        IPAddress localAddress,
        TftpPacket requestPacket,
        ITftpServerSessionHandler handler,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (clientEndPoint == null || localAddress == null ||
            requestPacket == null || handler == null)
        {
            throw new TftpProtocolException(
                "The server session inputs cannot be null.",
                false,
                0);
        }

        if (clientEndPoint.AddressFamily != localAddress.AddressFamily)
        {
            throw new TftpProtocolException(
                "The server session address families do not match.",
                false,
                0);
        }

        TftpServerSessionStartResult startResult =
            new TftpServerSessionStartResult();
        TftpTransferEvent? transferEvent = null;
        lock (SyncRoot)
        {
            TftpSessionAdmissionResult admission = Lifecycle.AdmitRequest(
                clientEndPoint,
                requestPacket);
            startResult.Action = admission.Action;
            if (admission.Action == TftpSessionAdmissionAction.Duplicate)
            {
                if (Sessions.ContainsKey(admission.RequestIdentity))
                {
                    TftpServerSession existingSession =
                        Sessions[admission.RequestIdentity];
                    startResult.Session = existingSession;
                    startResult.ResponseResent =
                        existingSession.RegisterDuplicateAndResend(
                            cancellationToken);
                    transferEvent = CreateSessionEvent(
                        existingSession,
                        TftpTransferState.Requesting,
                        null,
                        "Duplicate initial request received.");
                }
            }
            else if (admission.Action == TftpSessionAdmissionAction.Accepted)
            {
                TftpServerSession session = new TftpServerSession(
                    admission.RequestIdentity,
                    clientEndPoint,
                    localAddress,
                    requestPacket,
                    handler,
                    CancellationToken.None);
                if (cancellationToken.IsCancellationRequested)
                {
                    session.CloseResources();
                    Lifecycle.CompleteTransfer(
                        admission.RequestIdentity,
                        false);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Sessions.Add(admission.RequestIdentity, session);
                startResult.Session = session;
                session.StartExecution();
                transferEvent = CreateSessionEvent(
                    session,
                    TftpTransferState.Requesting,
                    null,
                    "Server transfer accepted.");
            }
        }

        ObserveEvent(transferEvent);
        return startResult;
    }

    internal int ProcessCompletedSessions()
    {
        List<TftpServerCompletionRecord> completions = [];
        lock (SyncRoot)
        {
            List<string> completedIdentities = [];
            foreach (string identity in Sessions.Keys)
            {
                TftpServerSession candidate = Sessions[identity];
                if (candidate.IsExecutionComplete())
                {
                    completedIdentities.Add(identity);
                }
            }

            int completionIndex = 0;
            while (completionIndex < completedIdentities.Count)
            {
                string identity = completedIdentities[completionIndex];
                TftpServerSession session = Sessions[identity];
                TftpSessionOutcome outcome = session.GetExecutionOutcome();
                session.CloseResources();
                Sessions.Remove(identity);
                Lifecycle.CompleteTransfer(identity, outcome.Succeeded);
                TftpServerCompletionRecord completion =
                    new TftpServerCompletionRecord();
                completion.Session = session;
                completion.Outcome = outcome;
                completions.Add(completion);
                completionIndex += 1;
            }
        }

        int observationIndex = 0;
        while (observationIndex < completions.Count)
        {
            TftpServerCompletionRecord completion =
                completions[observationIndex];
            TftpServerSession? session = completion.Session;
            TftpSessionOutcome? outcome = completion.Outcome;
            if (session != null && outcome != null)
            {
                TftpTransferEvent transferEvent = CreateSessionEvent(
                    session,
                    outcome.TerminalState,
                    outcome,
                    outcome.Message);
                ObserveEvent(transferEvent);
            }

            observationIndex += 1;
        }

        return completions.Count;
    }

    internal void StopSessions(TftpServerStopMode stopMode)
    {
        TftpServerListenerLifecycle.ValidateStopMode(stopMode);
        lock (SyncRoot)
        {
            if (stopMode == TftpServerStopMode.Immediate)
            {
                foreach (TftpServerSession session in Sessions.Values)
                {
                    session.CancelExecution();
                    session.CloseResources();
                }
            }

            Lifecycle.Stop(stopMode);
        }
    }

    internal int GetActiveSessionCount()
    {
        lock (SyncRoot)
        {
            return Sessions.Count;
        }
    }

    internal TftpServerSession? FindSession(string requestIdentity)
    {
        if (requestIdentity == null)
        {
            return null;
        }

        lock (SyncRoot)
        {
            if (!Sessions.ContainsKey(requestIdentity))
            {
                return null;
            }

            return Sessions[requestIdentity];
        }
    }

    internal TftpTransferEvent CreateSessionEvent(
        TftpServerSession session,
        TftpTransferState state,
        TftpSessionOutcome? outcome,
        string message)
    {
        TftpTransferEvent transferEvent = new TftpTransferEvent();
        transferEvent.Direction = session.ServerRequest.Direction;
        transferEvent.State = state;
        transferEvent.Mode = session.ServerRequest.Mode;
        transferEvent.Peer.Host = session.ClientEndPoint.Address.ToString();
        transferEvent.Peer.Port = session.ClientEndPoint.Port;
        transferEvent.Peer.AddressFamily =
            session.ClientEndPoint.AddressFamily;
        transferEvent.DuplicatePacketCount =
            session.DuplicateInitialRequestCount;
        transferEvent.Message = message;
        transferEvent.ServerRequest = CopyServerRequest(session.ServerRequest);
        if (outcome != null)
        {
            transferEvent.BytesTransferred = outcome.BytesTransferred;
            transferEvent.EncodedBytesTransferred =
                outcome.EncodedBytesTransferred;
            transferEvent.RetransmissionCount = outcome.RetransmissionCount;
            transferEvent.DuplicatePacketCount =
                transferEvent.DuplicatePacketCount +
                outcome.DuplicatePacketCount;
            transferEvent.NegotiatedOptions =
                TftpServerSession.CopyOptions(outcome.NegotiatedOptions);
            transferEvent.HasError = outcome.HasError;
            transferEvent.ErrorCode = outcome.ErrorCode;
        }

        return transferEvent;
    }

    internal TftpServerRequest CopyServerRequest(TftpServerRequest source)
    {
        TftpServerRequest copy = new TftpServerRequest();
        copy.Filename = source.Filename;
        copy.Direction = source.Direction;
        copy.Mode = source.Mode;
        copy.ClientEndpoint.Host = source.ClientEndpoint.Host;
        copy.ClientEndpoint.Port = source.ClientEndpoint.Port;
        copy.ClientEndpoint.LocalAddress = source.ClientEndpoint.LocalAddress;
        copy.ClientEndpoint.AddressFamily = source.ClientEndpoint.AddressFamily;
        copy.RequestedOptions =
            TftpServerSession.CopyOptions(source.RequestedOptions);
        copy.EffectiveOptions =
            TftpServerSession.CopyOptions(source.EffectiveOptions);
        copy.HasAnnouncedSize = source.HasAnnouncedSize;
        copy.AnnouncedSize = source.AnnouncedSize;
        return copy;
    }

    internal void ObserveEvent(TftpTransferEvent? transferEvent)
    {
        ITftpTransferObserver? observer = Observer;
        if (observer == null || transferEvent == null)
        {
            return;
        }

        observer.Observe(transferEvent);
    }
}
