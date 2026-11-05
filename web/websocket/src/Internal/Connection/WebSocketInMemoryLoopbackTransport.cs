using System;

namespace websocket;

internal class WebSocketLoopbackPumpResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int StepCount;
    internal int ClientToServerByteCount;
    internal int ServerToClientByteCount;
    internal int ClientReceivedFrameCount;
    internal int ServerReceivedFrameCount;
    internal int ClientToServerQueuedByteCount;
    internal int ServerToClientQueuedByteCount;
    internal bool Progress;
    internal bool Blocked;
    internal bool Quiescent;
    internal bool StepLimitReached;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketLoopbackStepResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ClientToServerByteCount;
    internal int ServerToClientByteCount;
    internal int ClientReceivedFrameCount;
    internal int ServerReceivedFrameCount;
    internal bool Progress;
    internal bool ClientReceiveBlocked;
    internal bool ServerReceiveBlocked;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketLoopbackReceiveResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int FrameCount;
    internal bool ResumeRequired;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketInMemoryLoopbackTransport
{
    WebSocketConnection Client;
    WebSocketConnection Server;
    BoundedByteQueue ClientToServerBytes;
    BoundedByteQueue ServerToClientBytes;
    int MaximumChunkCount;
    int MaximumObservedClientToServerByteCount;
    int MaximumObservedServerToClientByteCount;
    bool ClientReceiveResumeRequired;
    bool ServerReceiveResumeRequired;
    bool ClientOutputEndRequested;
    bool ServerOutputEndRequested;
    bool ClientOutputEndDelivered;
    bool ServerOutputEndDelivered;
    bool Reset;

    internal WebSocketInMemoryLoopbackTransport(
        WebSocketConnection? client,
        WebSocketConnection? server,
        int transportCapacity,
        int maximumChunkCount)
    {
        if (client == null ||
            server == null ||
            transportCapacity <= 0 ||
            maximumChunkCount <= 0)
        {
            throw new WebSocketStateException();
        }

        WebSocketConnectionInfo clientInformation =
            client.GetConnectionInfo();
        WebSocketConnectionInfo serverInformation =
            server.GetConnectionInfo();
        if (clientInformation.Role != WebSocketRole.Client ||
            serverInformation.Role != WebSocketRole.Server ||
            clientInformation.Transport != serverInformation.Transport)
        {
            throw new WebSocketStateException();
        }

        Client = client;
        Server = server;
        ClientToServerBytes = new BoundedByteQueue(transportCapacity);
        ServerToClientBytes = new BoundedByteQueue(transportCapacity);
        MaximumChunkCount = maximumChunkCount;
    }

    internal WebSocketLoopbackPumpResult PumpLoopbackTraffic(
        int maximumStepCount)
    {
        if (maximumStepCount <= 0 || Reset)
        {
            throw new WebSocketStateException();
        }

        WebSocketLoopbackPumpResult result =
            new WebSocketLoopbackPumpResult();
        while (result.StepCount < maximumStepCount)
        {
            WebSocketLoopbackStepResult step = PumpLoopbackStep();
            result.StepCount = result.StepCount + 1;
            result.ClientToServerByteCount =
                result.ClientToServerByteCount +
                    step.ClientToServerByteCount;
            result.ServerToClientByteCount =
                result.ServerToClientByteCount +
                    step.ServerToClientByteCount;
            result.ClientReceivedFrameCount =
                result.ClientReceivedFrameCount +
                    step.ClientReceivedFrameCount;
            result.ServerReceivedFrameCount =
                result.ServerReceivedFrameCount +
                    step.ServerReceivedFrameCount;
            result.Progress = result.Progress || step.Progress;
            if (step.HasError)
            {
                result.Status = step.Status;
                result.HasError = true;
                result.Error = WebSocketFrameCodec.CopyError(step.Error);
                break;
            }

            if (step.ClientReceiveBlocked || step.ServerReceiveBlocked)
            {
                result.Status = step.Status;
                result.Blocked = true;
                break;
            }

            if (!HasPendingLoopbackWork())
            {
                result.Quiescent = true;
                break;
            }

            if (!step.Progress)
            {
                result.Blocked = true;
                result.Status = WebSocketParseStatus.NeedOutputCapacity;
                break;
            }
        }

        if (!result.Quiescent &&
            !result.Blocked &&
            !result.HasError &&
            result.StepCount == maximumStepCount &&
            HasPendingLoopbackWork())
        {
            result.StepLimitReached = true;
        }

        result.ClientToServerQueuedByteCount =
            ClientToServerBytes.GetByteCount();
        result.ServerToClientQueuedByteCount =
            ServerToClientBytes.GetByteCount();
        return result;
    }

    internal WebSocketParseStatus AdvanceLoopbackTime(
        long elapsedMilliseconds)
    {
        if (Reset)
        {
            throw new WebSocketStateException();
        }

        WebSocketParseStatus clientStatus =
            Client.AdvanceConnectionTime(elapsedMilliseconds);
        WebSocketParseStatus serverStatus =
            Server.AdvanceConnectionTime(elapsedMilliseconds);
        if (clientStatus == WebSocketParseStatus.ProtocolFailure ||
            clientStatus == WebSocketParseStatus.LimitRejected)
        {
            return clientStatus;
        }

        if (serverStatus == WebSocketParseStatus.ProtocolFailure ||
            serverStatus == WebSocketParseStatus.LimitRejected)
        {
            return serverStatus;
        }

        if (clientStatus == WebSocketParseStatus.NeedOutputCapacity ||
            serverStatus == WebSocketParseStatus.NeedOutputCapacity)
        {
            return WebSocketParseStatus.NeedOutputCapacity;
        }

        return WebSocketParseStatus.Complete;
    }

    internal void EndClientLoopbackOutput()
    {
        if (!Reset && !ClientOutputEndDelivered)
        {
            ClientOutputEndRequested = true;
        }
    }

    internal void EndServerLoopbackOutput()
    {
        if (!Reset && !ServerOutputEndDelivered)
        {
            ServerOutputEndRequested = true;
        }
    }

    internal WebSocketParseStatus ResetLoopbackTransport()
    {
        if (Reset)
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        ClientToServerBytes.ClearBytes();
        ServerToClientBytes.ClearBytes();
        ClientReceiveResumeRequired = false;
        ServerReceiveResumeRequired = false;
        ClientOutputEndRequested = true;
        ServerOutputEndRequested = true;
        ClientOutputEndDelivered = true;
        ServerOutputEndDelivered = true;
        Reset = true;
        WebSocketConnectionTransitionResult clientResult =
            Client.ResetConnectionTransport();
        WebSocketConnectionTransitionResult serverResult =
            Server.ResetConnectionTransport();
        if (clientResult.HasError)
        {
            return clientResult.Status;
        }

        return serverResult.Status;
    }

    internal int GetClientToServerQueuedByteCount()
    {
        return ClientToServerBytes.GetByteCount();
    }

    internal int GetServerToClientQueuedByteCount()
    {
        return ServerToClientBytes.GetByteCount();
    }

    internal int GetMaximumObservedClientToServerByteCount()
    {
        return MaximumObservedClientToServerByteCount;
    }

    internal int GetMaximumObservedServerToClientByteCount()
    {
        return MaximumObservedServerToClientByteCount;
    }

    internal bool HasDeliveredClientOutputEnd()
    {
        return ClientOutputEndDelivered;
    }

    internal bool HasDeliveredServerOutputEnd()
    {
        return ServerOutputEndDelivered;
    }

    WebSocketLoopbackStepResult PumpLoopbackStep()
    {
        WebSocketLoopbackStepResult step =
            new WebSocketLoopbackStepResult();

        if (ClientReceiveResumeRequired)
        {
            WebSocketLoopbackReceiveResult resumed =
                ResumeConnectionInput(Client);
            ClientReceiveResumeRequired = resumed.ResumeRequired;
            MergeClientReceive(step, resumed);
        }

        if (ServerReceiveResumeRequired)
        {
            WebSocketLoopbackReceiveResult resumed =
                ResumeConnectionInput(Server);
            ServerReceiveResumeRequired = resumed.ResumeRequired;
            MergeServerReceive(step, resumed);
        }

        if (!step.HasError && !ServerReceiveResumeRequired &&
            ClientToServerBytes.GetByteCount() != 0)
        {
            WebSocketLoopbackReceiveResult received =
                ReceiveQueuedBytes(Server, ClientToServerBytes);
            ServerReceiveResumeRequired = received.ResumeRequired;
            MergeServerReceive(step, received);
        }

        if (!step.HasError && !ClientReceiveResumeRequired &&
            ServerToClientBytes.GetByteCount() != 0)
        {
            WebSocketLoopbackReceiveResult received =
                ReceiveQueuedBytes(Client, ServerToClientBytes);
            ClientReceiveResumeRequired = received.ResumeRequired;
            MergeClientReceive(step, received);
        }

        if (!step.HasError && !ServerReceiveResumeRequired)
        {
            int written = TransferConnectionOutput(
                Client,
                ClientToServerBytes);
            if (written != 0)
            {
                step.Progress = true;
            }
        }

        if (!step.HasError && !ClientReceiveResumeRequired)
        {
            int written = TransferConnectionOutput(
                Server,
                ServerToClientBytes);
            if (written != 0)
            {
                step.Progress = true;
            }
        }

        UpdateMaximumObservedCounts();
        DeliverRequestedOutputEnds(step);
        if (step.ClientReceiveBlocked || step.ServerReceiveBlocked)
        {
            step.Status = SelectBlockedStatus(step);
        }

        return step;
    }

    WebSocketLoopbackReceiveResult ResumeConnectionInput(
        WebSocketConnection connection)
    {
        WebSocketInboundFramePumpResult resumed =
            connection.ReceiveConnectionBytes([]);
        return CreateReceiveResult(resumed);
    }

    WebSocketLoopbackReceiveResult ReceiveQueuedBytes(
        WebSocketConnection connection,
        BoundedByteQueue queue)
    {
        int readCount = queue.GetByteCount();
        if (readCount > MaximumChunkCount)
        {
            readCount = MaximumChunkCount;
        }

        byte[] source = new byte[readCount];
        int peeked = queue.PeekBytes(source);
        if (peeked != source.Length)
        {
            throw new WebSocketStateException();
        }

        WebSocketInboundFramePumpResult received =
            connection.ReceiveConnectionBytes(source);
        if (received.ConsumedCount != 0)
        {
            byte[] consumed = new byte[received.ConsumedCount];
            int removed = queue.ReadBytes(consumed);
            Array.Clear(consumed);
            if (removed != received.ConsumedCount)
            {
                throw new WebSocketStateException();
            }
        }

        Array.Clear(source);
        return CreateReceiveResult(received);
    }

    int TransferConnectionOutput(
        WebSocketConnection connection,
        BoundedByteQueue queue)
    {
        int writeCount = queue.GetByteAvailableCapacity();
        if (writeCount > MaximumChunkCount)
        {
            writeCount = MaximumChunkCount;
        }

        int pendingCount = connection.GetPendingConnectionWriteCount();
        if (writeCount > pendingCount)
        {
            writeCount = pendingCount;
        }

        if (writeCount == 0)
        {
            return 0;
        }

        byte[] bytes = new byte[writeCount];
        int written = connection.WriteConnectionBytes(bytes);
        int retained = queue.WriteBytes(
            bytes.AsSpan(0, written));
        Array.Clear(bytes);
        if (written != writeCount || retained != written)
        {
            throw new WebSocketStateException();
        }

        return written;
    }

    void DeliverRequestedOutputEnds(WebSocketLoopbackStepResult step)
    {
        if (ClientOutputEndRequested &&
            !ClientOutputEndDelivered &&
            !ServerReceiveResumeRequired &&
            Client.GetPendingConnectionWriteCount() == 0 &&
            ClientToServerBytes.GetByteCount() == 0)
        {
            WebSocketConnectionTransitionResult ended =
                Server.EndConnectionInput();
            ClientOutputEndDelivered = true;
            step.Progress = true;
            MergeTransition(step, ended);
        }

        if (ServerOutputEndRequested &&
            !ServerOutputEndDelivered &&
            !ClientReceiveResumeRequired &&
            Server.GetPendingConnectionWriteCount() == 0 &&
            ServerToClientBytes.GetByteCount() == 0)
        {
            WebSocketConnectionTransitionResult ended =
                Client.EndConnectionInput();
            ServerOutputEndDelivered = true;
            step.Progress = true;
            MergeTransition(step, ended);
        }
    }

    void MergeClientReceive(
        WebSocketLoopbackStepResult step,
        WebSocketLoopbackReceiveResult received)
    {
        step.ServerToClientByteCount =
            step.ServerToClientByteCount + received.ConsumedCount;
        step.ClientReceivedFrameCount =
            step.ClientReceivedFrameCount + received.FrameCount;
        if (received.ConsumedCount != 0 || received.FrameCount != 0)
        {
            step.Progress = true;
        }

        if (received.HasError)
        {
            SetStepFailure(step, received.Status, received.Error);
        }
        else if (received.ResumeRequired)
        {
            step.ClientReceiveBlocked = true;
            step.Status = received.Status;
        }
    }

    void MergeServerReceive(
        WebSocketLoopbackStepResult step,
        WebSocketLoopbackReceiveResult received)
    {
        step.ClientToServerByteCount =
            step.ClientToServerByteCount + received.ConsumedCount;
        step.ServerReceivedFrameCount =
            step.ServerReceivedFrameCount + received.FrameCount;
        if (received.ConsumedCount != 0 || received.FrameCount != 0)
        {
            step.Progress = true;
        }

        if (received.HasError)
        {
            SetStepFailure(step, received.Status, received.Error);
        }
        else if (received.ResumeRequired)
        {
            step.ServerReceiveBlocked = true;
            step.Status = received.Status;
        }
    }

    void MergeTransition(
        WebSocketLoopbackStepResult step,
        WebSocketConnectionTransitionResult transition)
    {
        if (transition.HasError && !step.HasError)
        {
            SetStepFailure(
                step,
                transition.Status,
                transition.Error);
        }
    }

    void SetStepFailure(
        WebSocketLoopbackStepResult step,
        WebSocketParseStatus status,
        WebSocketError error)
    {
        if (!step.HasError)
        {
            step.Status = status;
            step.HasError = true;
            step.Error = WebSocketFrameCodec.CopyError(error);
        }
    }

    void UpdateMaximumObservedCounts()
    {
        int clientToServerCount =
            ClientToServerBytes.GetByteCount();
        if (clientToServerCount >
            MaximumObservedClientToServerByteCount)
        {
            MaximumObservedClientToServerByteCount =
                clientToServerCount;
        }

        int serverToClientCount =
            ServerToClientBytes.GetByteCount();
        if (serverToClientCount >
            MaximumObservedServerToClientByteCount)
        {
            MaximumObservedServerToClientByteCount =
                serverToClientCount;
        }
    }

    bool HasPendingLoopbackWork()
    {
        return ClientReceiveResumeRequired ||
            ServerReceiveResumeRequired ||
            ClientToServerBytes.GetByteCount() != 0 ||
            ServerToClientBytes.GetByteCount() != 0 ||
            Client.GetPendingConnectionWriteCount() != 0 ||
            Server.GetPendingConnectionWriteCount() != 0 ||
            (ClientOutputEndRequested &&
                !ClientOutputEndDelivered) ||
            (ServerOutputEndRequested &&
                !ServerOutputEndDelivered);
    }

    static WebSocketLoopbackReceiveResult CreateReceiveResult(
        WebSocketInboundFramePumpResult source)
    {
        WebSocketLoopbackReceiveResult result =
            new WebSocketLoopbackReceiveResult();
        result.Status = source.Status;
        result.ConsumedCount = source.ConsumedCount;
        result.FrameCount = source.FrameCount;
        result.ResumeRequired =
            source.Status == WebSocketParseStatus.NeedOutputCapacity ||
            source.Status == WebSocketParseStatus.NeedEventCapacity;
        result.HasError = source.HasError;
        result.Error = WebSocketFrameCodec.CopyError(source.Error);
        return result;
    }

    static WebSocketParseStatus SelectBlockedStatus(
        WebSocketLoopbackStepResult step)
    {
        if (step.Status == WebSocketParseStatus.NeedEventCapacity)
        {
            return WebSocketParseStatus.NeedEventCapacity;
        }

        return WebSocketParseStatus.NeedOutputCapacity;
    }
}
