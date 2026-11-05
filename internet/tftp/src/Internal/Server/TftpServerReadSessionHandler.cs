using System;
using System.Net;
using System.Threading;

namespace tftp;

internal class TftpServerReadSessionHandler : ITftpServerSessionHandler
{
    internal ITftpServerStorage Storage;
    internal TftpServerOptions ServerOptions;

    internal TftpServerReadSessionHandler(
        ITftpServerStorage storage,
        TftpServerOptions serverOptions)
    {
        Storage = storage;
        ServerOptions = serverOptions;
    }

    /// <summary>
    /// Executes one accepted server read session.
    /// </summary>
    /// <param name="session">The accepted transfer session.</param>
    /// <param name="cancellationToken">A token that cancels the session.</param>
    /// <returns>The terminal session outcome.</returns>
    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        using (TftpServerReadStateMachine state =
            new TftpServerReadStateMachine(
                session.RequestPacket,
                session.ClientEndPoint,
                Storage,
                ServerOptions,
                new TftpSystemTimeSource()))
        {
            TftpServerReadResult result = state.StartRead(cancellationToken);
            session.ServerRequest = state.ServerRequest;
            while (true)
            {
                if (result.Action ==
                    TftpServerReadAction.SendOptionAcknowledgement ||
                    result.Action == TftpServerReadAction.SendData ||
                    result.Action == TftpServerReadAction.RetransmitPacket)
                {
                    SendReadPacket(session, result.Packet, cancellationToken);
                }
                else if (result.Action == TftpServerReadAction.SendError)
                {
                    SendReadError(session, result.Packet, cancellationToken);
                    return CreateReadOutcome(result, false);
                }
                else if (result.Action == TftpServerReadAction.Completed)
                {
                    return CreateReadOutcome(result, true);
                }

                TftpServerReadResult timerResult = state.EvaluateReadTimer(
                    cancellationToken);
                if (timerResult.Action ==
                    TftpServerReadAction.RetransmitPacket)
                {
                    result = timerResult;
                    continue;
                }

                long waitMilliseconds = timerResult.WaitMilliseconds;
                byte[] buffer = new byte[
                    ServerOptions.MaximumRequestPacketLength + 1];
                UdpDatagramReceiveResult datagram =
                    session.TransferSocket.ReceiveBoundedDatagramOrTimeout(
                        buffer,
                        buffer.Length,
                        waitMilliseconds,
                        cancellationToken);
                if (!datagram.HasDatagram)
                {
                    result = state.EvaluateReadTimer(cancellationToken);
                    continue;
                }

                TftpServerPeerDatagramResult peerResult =
                    TftpServerPeerDatagramProcessor.ClassifyDatagram(
                        session,
                        datagram.RemoteEndPoint,
                        buffer,
                        datagram.Length,
                        0,
                        ServerOptions.MaximumRequestPacketLength,
                        cancellationToken);
                if (peerResult.Action ==
                    TftpServerPeerDatagramAction.IgnoreUnexpectedAddress ||
                    peerResult.Action ==
                        TftpServerPeerDatagramAction.RejectUnknownTransferId)
                {
                    result = state.CreateReadResult(
                        TftpServerReadAction.Wait,
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

                    result = state.CreateReadFailure(
                        peerError.ErrorCode,
                        peerError.ErrorMessage,
                        false);
                    continue;
                }

                TftpPacket? packet = peerResult.Packet;
                if (packet == null)
                {
                    throw new TftpProtocolException(
                        "The decoded server read packet is unavailable.",
                        false,
                        0);
                }

                if (packet.Opcode == TftpOpcode.Error)
                {
                    return CreateRemoteReadOutcome(packet, state);
                }

                result = state.ProcessReadPacket(packet, cancellationToken);
            }
        }
    }

    internal void SendReadPacket(
        TftpServerSession session,
        TftpPacket? packet,
        CancellationToken cancellationToken)
    {
        if (packet == null)
        {
            throw new TftpProtocolException(
                "The server read response packet is unavailable.",
                false,
                0);
        }

        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        session.SendAndRetainResponse(encoded, cancellationToken);
    }

    internal void SendReadError(
        TftpServerSession session,
        TftpPacket? packet,
        CancellationToken cancellationToken)
    {
        if (packet == null)
        {
            throw new TftpProtocolException(
                "The server read ERROR packet is unavailable.",
                false,
                0);
        }

        session.SendErrorResponse(packet, cancellationToken);
    }

    internal TftpSessionOutcome CreateReadOutcome(
        TftpServerReadResult result,
        bool succeeded)
    {
        TftpSessionOutcome outcome = new TftpSessionOutcome();
        outcome.Succeeded = succeeded;
        if (succeeded)
        {
            outcome.TerminalState = TftpTransferState.Completed;
            outcome.Message = "Server read completed.";
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

    internal TftpSessionOutcome CreateRemoteReadOutcome(
        TftpPacket packet,
        TftpServerReadStateMachine state)
    {
        TftpServerReadResult result = state.CreateReadResult(
            TftpServerReadAction.Wait,
            null,
            0);
        result.FailureErrorCode = packet.ErrorCode;
        result.FailureMessage = packet.ErrorMessage;
        return CreateReadOutcome(result, false);
    }
}
