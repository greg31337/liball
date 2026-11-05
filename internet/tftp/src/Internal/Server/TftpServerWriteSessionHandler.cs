using System;
using System.Threading;

namespace tftp;

internal class TftpServerWriteSessionHandler : ITftpServerSessionHandler
{
    internal ITftpServerStorage Storage;
    internal TftpServerOptions ServerOptions;

    internal TftpServerWriteSessionHandler(
        ITftpServerStorage storage,
        TftpServerOptions serverOptions)
    {
        Storage = storage;
        ServerOptions = serverOptions;
    }

    /// <summary>
    /// Executes one accepted server write session.
    /// </summary>
    /// <param name="session">The accepted transfer session.</param>
    /// <param name="cancellationToken">A token that cancels the session.</param>
    /// <returns>The terminal session outcome.</returns>
    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        using (TftpServerWriteStateMachine state =
            new TftpServerWriteStateMachine(
                session.RequestPacket,
                session.ClientEndPoint,
                Storage,
                ServerOptions,
                new TftpSystemTimeSource()))
        {
            TftpServerWriteResult result = state.StartWrite(cancellationToken);
            session.ServerRequest = state.ServerRequest;
            while (true)
            {
                if (result.Action ==
                    TftpServerWriteAction.SendOptionAcknowledgement ||
                    result.Action == TftpServerWriteAction.SendAcknowledgement ||
                    result.Action == TftpServerWriteAction.ReacknowledgeData ||
                    result.Action == TftpServerWriteAction.RetransmitPacket)
                {
                    SendWritePacket(session, result.Packet, cancellationToken);
                }
                else if (result.Action == TftpServerWriteAction.SendError)
                {
                    SendWriteError(session, result.Packet, cancellationToken);
                    return CreateWriteOutcome(result, false);
                }
                else if (result.Action == TftpServerWriteAction.Completed)
                {
                    return CreateWriteOutcome(result, true);
                }

                TftpServerWriteResult timerResult = state.EvaluateWriteTimer(
                    cancellationToken);
                if (timerResult.Action ==
                    TftpServerWriteAction.RetransmitPacket ||
                    timerResult.Action == TftpServerWriteAction.Completed ||
                    timerResult.Action == TftpServerWriteAction.SendError)
                {
                    result = timerResult;
                    continue;
                }

                long waitMilliseconds = timerResult.WaitMilliseconds;
                int maximumDatagramLength =
                    state.EffectiveBlockSize + TftpProtocolValues.DataHeaderLength;
                if (maximumDatagramLength <
                    ServerOptions.MaximumRequestPacketLength)
                {
                    maximumDatagramLength =
                        ServerOptions.MaximumRequestPacketLength;
                }

                byte[] buffer = new byte[maximumDatagramLength + 1];
                UdpDatagramReceiveResult datagram =
                    session.TransferSocket.ReceiveBoundedDatagramOrTimeout(
                        buffer,
                        buffer.Length,
                        waitMilliseconds,
                        cancellationToken);
                if (!datagram.HasDatagram)
                {
                    result = state.EvaluateWriteTimer(cancellationToken);
                    continue;
                }

                TftpServerPeerDatagramResult peerResult =
                    TftpServerPeerDatagramProcessor.ClassifyDatagram(
                        session,
                        datagram.RemoteEndPoint,
                        buffer,
                        datagram.Length,
                        state.EffectiveBlockSize,
                        ServerOptions.MaximumRequestPacketLength,
                        cancellationToken);
                if (peerResult.Action ==
                    TftpServerPeerDatagramAction.IgnoreUnexpectedAddress ||
                    peerResult.Action ==
                        TftpServerPeerDatagramAction.RejectUnknownTransferId)
                {
                    result = state.CreateWriteResult(
                        TftpServerWriteAction.Wait,
                        null,
                        0);
                    continue;
                }

                if (peerResult.Action == TftpServerPeerDatagramAction.SendError)
                {
                    TftpPacket? peerError = peerResult.ErrorPacket;
                    if (peerError == null)
                    {
                        throw new TftpProtocolException(
                            "The malformed peer response is unavailable.",
                            false,
                            0);
                    }

                    result = state.CreateWriteFailure(
                        peerError.ErrorCode,
                        peerError.ErrorMessage,
                        false);
                    continue;
                }

                TftpPacket? packet = peerResult.Packet;
                if (packet == null)
                {
                    throw new TftpProtocolException(
                        "The decoded server write packet is unavailable.",
                        false,
                        0);
                }

                if (packet.Opcode == TftpOpcode.Error)
                {
                    return CreateRemoteWriteOutcome(packet, state);
                }

                result = state.ProcessWritePacket(packet, cancellationToken);
            }
        }
    }

    internal void SendWritePacket(
        TftpServerSession session,
        TftpPacket? packet,
        CancellationToken cancellationToken)
    {
        if (packet == null)
        {
            throw new TftpProtocolException(
                "The server write response packet is unavailable.",
                false,
                0);
        }

        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        session.SendAndRetainResponse(encoded, cancellationToken);
    }

    internal void SendWriteError(
        TftpServerSession session,
        TftpPacket? packet,
        CancellationToken cancellationToken)
    {
        if (packet == null)
        {
            throw new TftpProtocolException(
                "The server write ERROR packet is unavailable.",
                false,
                0);
        }

        session.SendErrorResponse(packet, cancellationToken);
    }

    internal TftpSessionOutcome CreateWriteOutcome(
        TftpServerWriteResult result,
        bool succeeded)
    {
        TftpSessionOutcome outcome = new TftpSessionOutcome();
        outcome.Succeeded = succeeded;
        if (succeeded)
        {
            outcome.TerminalState = TftpTransferState.Completed;
            outcome.Message = "Server write completed.";
        }
        else
        {
            outcome.TerminalState = TftpTransferState.Failed;
            outcome.HasError = true;
            outcome.ErrorCode = result.FailureErrorCode;
            outcome.Message = result.FailureMessage;
        }

        outcome.BytesTransferred = result.BytesTransferred;
        outcome.EncodedBytesTransferred = result.EncodedBytesTransferred;
        outcome.RetransmissionCount = result.RetransmissionCount;
        outcome.DuplicatePacketCount = result.DuplicatePacketCount;
        outcome.NegotiatedOptions =
            TftpServerSession.CopyOptions(result.NegotiatedOptions);
        return outcome;
    }

    internal TftpSessionOutcome CreateRemoteWriteOutcome(
        TftpPacket packet,
        TftpServerWriteStateMachine state)
    {
        TftpServerWriteResult result = state.CreateWriteResult(
            TftpServerWriteAction.Wait,
            null,
            0);
        result.FailureErrorCode = packet.ErrorCode;
        result.FailureMessage = packet.ErrorMessage;
        return CreateWriteOutcome(result, false);
    }
}
