using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tftp;

/// <summary>
/// Downloads and uploads content using TFTP.
/// </summary>
public class TftpClient : ITftpClient
{
    internal ITftpTransferObserver? Observer;

    /// <summary>
    /// Initializes a client with an optional transfer observer.
    /// </summary>
    /// <param name="observer">The observer to notify, or null for no observation.</param>
    public TftpClient(ITftpTransferObserver? observer)
    {
        Observer = observer;
    }

    /// <summary>
    /// Downloads remote content into a caller-owned stream.
    /// </summary>
    /// <param name="request">The read request.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    public TftpTransferResult DownloadToStream(
        TftpReadRequest request,
        Stream destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateReadStreamInputs(request, destination);
        TftpTransferOptions transferOptions = request.Options;
        ObserveConfiguredState(
            TftpTransferDirection.Read,
            TftpTransferState.Created,
            transferOptions.Mode,
            request.Endpoint,
            "Client read transfer created.");
        ObserveConfiguredState(
            TftpTransferDirection.Read,
            TftpTransferState.Resolving,
            transferOptions.Mode,
            request.Endpoint,
            "Resolving the client read endpoint.");
        TftpSystemTimeSource timeSource = new TftpSystemTimeSource();
        long operationStartedMilliseconds =
            timeSource.GetTimestampMilliseconds();
        TftpEndpointResolutionResult resolution =
            TftpEndpointResolver.ResolveClientEndpoint(
                request.Endpoint,
                cancellationToken);
        UdpDatagramSocket datagramSocket = UdpDatagramSocket.BindLocalSocket(
            resolution.LocalEndPoint.Address,
            resolution.LocalEndPoint.Port,
            cancellationToken);
        using (Socket ownedSocket = datagramSocket.Socket)
        {
            TftpClientReadStateMachine state =
                new TftpClientReadStateMachine(
                    request,
                    destination,
                    resolution.RemoteEndPoint,
                    timeSource);
            return ExecuteReadTransfer(
                state,
                datagramSocket,
                timeSource,
                operationStartedMilliseconds,
                cancellationToken);
        }
    }

    /// <summary>
    /// Uploads content from a caller-owned stream.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="source">The readable source stream.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    public TftpTransferResult UploadFromStream(
        TftpWriteRequest request,
        Stream source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateWriteStreamInputs(request, source);
        TftpTransferOptions transferOptions = request.Options;
        ObserveConfiguredState(
            TftpTransferDirection.Write,
            TftpTransferState.Created,
            transferOptions.Mode,
            request.Endpoint,
            "Client write transfer created.");
        ObserveConfiguredState(
            TftpTransferDirection.Write,
            TftpTransferState.Resolving,
            transferOptions.Mode,
            request.Endpoint,
            "Resolving the client write endpoint.");
        TftpSystemTimeSource timeSource = new TftpSystemTimeSource();
        long operationStartedMilliseconds =
            timeSource.GetTimestampMilliseconds();
        TftpEndpointResolutionResult resolution =
            TftpEndpointResolver.ResolveClientEndpoint(
                request.Endpoint,
                cancellationToken);
        UdpDatagramSocket datagramSocket = UdpDatagramSocket.BindLocalSocket(
            resolution.LocalEndPoint.Address,
            resolution.LocalEndPoint.Port,
            cancellationToken);
        using (Socket ownedSocket = datagramSocket.Socket)
        {
            TftpClientWriteStateMachine state =
                new TftpClientWriteStateMachine(
                    request,
                    source,
                    resolution.RemoteEndPoint,
                    timeSource);
            return ExecuteWriteTransfer(
                state,
                datagramSocket,
                timeSource,
                operationStartedMilliseconds,
                cancellationToken);
        }
    }

    /// <summary>
    /// Downloads remote content and publishes it at a local file path.
    /// </summary>
    /// <param name="request">The read request.</param>
    /// <param name="localPath">The local destination file path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    public TftpTransferResult DownloadToFile(
        TftpReadRequest request,
        string localPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateReadFileInputs(request, localPath);
        string destinationPath = GetValidatedClientFilePath(localPath);
        using (TftpPendingClientDownload pendingDownload =
            new TftpPendingClientDownload(destinationPath))
        {
            TftpTransferResult result = DownloadToStream(
                request,
                pendingDownload.Stream,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            pendingDownload.PublishDownload();
            return result;
        }
    }

    /// <summary>
    /// Uploads content from a local file path.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="localPath">The local source file path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    public TftpTransferResult UploadFromFile(
        TftpWriteRequest request,
        string localPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateWriteFileInputs(request, localPath);
        string sourcePath = GetValidatedClientFilePath(localPath);
        using (FileStream source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan))
        {
            return UploadFromStream(request, source, cancellationToken);
        }
    }

    internal void ValidateReadFileInputs(
        TftpReadRequest request,
        string localPath)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client read request cannot be null.",
                false,
                0);
        }

        if (request.Endpoint == null)
        {
            throw new TftpProtocolException(
                "The client read endpoint cannot be null.",
                false,
                0);
        }

        if (request.Options == null)
        {
            throw new TftpProtocolException(
                "The client read transfer options cannot be null.",
                false,
                0);
        }

        if (localPath == null || string.IsNullOrWhiteSpace(localPath))
        {
            throw new TftpProtocolException(
                "The client download path cannot be empty.",
                false,
                0);
        }
    }

    internal void ValidateWriteFileInputs(
        TftpWriteRequest request,
        string localPath)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client write request cannot be null.",
                false,
                0);
        }

        if (request.Endpoint == null)
        {
            throw new TftpProtocolException(
                "The client write endpoint cannot be null.",
                false,
                0);
        }

        if (request.Options == null)
        {
            throw new TftpProtocolException(
                "The client write transfer options cannot be null.",
                false,
                0);
        }

        if (localPath == null || string.IsNullOrWhiteSpace(localPath))
        {
            throw new TftpProtocolException(
                "The client upload path cannot be empty.",
                false,
                0);
        }
    }

    internal string GetValidatedClientFilePath(string localPath)
    {
        string fullPath = Path.GetFullPath(localPath);
        string fileName = Path.GetFileName(fullPath);
        if (fileName.Length == 0)
        {
            throw new TftpProtocolException(
                "The client file path must identify a file in a directory.",
                false,
                0);
        }

        return fullPath;
    }

    internal void ValidateReadStreamInputs(
        TftpReadRequest request,
        Stream destination)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client read request cannot be null.",
                false,
                0);
        }

        if (request.Endpoint == null)
        {
            throw new TftpProtocolException(
                "The client read endpoint cannot be null.",
                false,
                0);
        }

        if (request.Options == null)
        {
            throw new TftpProtocolException(
                "The client read transfer options cannot be null.",
                false,
                0);
        }

        if (destination == null || !destination.CanWrite)
        {
            throw new TftpProtocolException(
                "The client read destination stream must be writable.",
                false,
                0);
        }
    }

    internal void ValidateWriteStreamInputs(
        TftpWriteRequest request,
        Stream source)
    {
        if (request == null)
        {
            throw new TftpProtocolException(
                "The client write request cannot be null.",
                false,
                0);
        }

        if (request.Endpoint == null)
        {
            throw new TftpProtocolException(
                "The client write endpoint cannot be null.",
                false,
                0);
        }

        if (request.Options == null)
        {
            throw new TftpProtocolException(
                "The client write transfer options cannot be null.",
                false,
                0);
        }

        if (source == null || !source.CanRead)
        {
            throw new TftpProtocolException(
                "The client write source stream must be readable.",
                false,
                0);
        }
    }

    internal TftpTransferResult ExecuteReadTransfer(
        TftpClientReadStateMachine state,
        UdpDatagramSocket datagramSocket,
        TftpSystemTimeSource timeSource,
        long operationStartedMilliseconds,
        CancellationToken cancellationToken)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] receiveBuffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        TftpClientReadResult current = state.StartRead(cancellationToken);
        while (true)
        {
            ObserveReadAction(state, current);
            SendClientPacket(
                datagramSocket,
                current.Packet,
                current.TargetEndPoint,
                cancellationToken);
            ThrowForReadActionFailure(current);
            if (current.Action == TftpClientReadAction.Completed)
            {
                return CreateReadTransferResult(
                    state,
                    timeSource,
                    operationStartedMilliseconds);
            }

            TftpClientReadResult timerResult =
                state.EvaluateReadTimer(cancellationToken);
            if (timerResult.Action != TftpClientReadAction.Wait)
            {
                current = timerResult;
                continue;
            }

            UdpDatagramReceiveResult received =
                datagramSocket.ReceiveBoundedDatagramOrTimeout(
                    receiveBuffer,
                    receiveBuffer.Length,
                    timerResult.WaitMilliseconds,
                    cancellationToken);
            if (!received.HasDatagram)
            {
                current = state.EvaluateReadTimer(cancellationToken);
                continue;
            }

            TftpPacketDecodeResult decoded = TftpClientDatagramDecoder.Decode(
                codec,
                receiveBuffer.AsSpan(0, received.Length),
                received.ExceededMaximumLength);
            if (!decoded.Success)
            {
                HandleMalformedReadDatagram(
                    state,
                    datagramSocket,
                    received.RemoteEndPoint,
                    decoded.FailureMessage,
                    cancellationToken);
                current = timerResult;
                continue;
            }

            bool observeRemoteError = true;
            if (decoded.Packet.Opcode == TftpOpcode.Error &&
                !state.EnteredReceiver &&
                TftpOptionNegotiator.CanRetryRequestWithoutOptions(
                    state.TransferOptions,
                    state.ActiveRequestedOptions,
                    state.RetriedWithoutOptions,
                    decoded.Packet.ErrorCode))
            {
                observeRemoteError = false;
            }

            if (observeRemoteError)
            {
                ObserveRemoteErrorPacket(
                    TftpTransferDirection.Read,
                    state.TransferOptions.Mode,
                    received.RemoteEndPoint,
                    decoded.Packet,
                    current.BytesTransferred,
                    current.EncodedBytesTransferred,
                    current.RetransmissionCount,
                    current.DuplicatePacketCount,
                    current.NegotiatedOptions);
            }

            current = state.ProcessReadPacket(
                decoded.Packet,
                received.RemoteEndPoint,
                cancellationToken);
        }
    }

    internal TftpTransferResult ExecuteWriteTransfer(
        TftpClientWriteStateMachine state,
        UdpDatagramSocket datagramSocket,
        TftpSystemTimeSource timeSource,
        long operationStartedMilliseconds,
        CancellationToken cancellationToken)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] receiveBuffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        TftpClientWriteResult current = state.StartWrite(cancellationToken);
        while (true)
        {
            ObserveWriteAction(state, current);
            SendClientPacket(
                datagramSocket,
                current.Packet,
                current.TargetEndPoint,
                cancellationToken);
            ThrowForWriteActionFailure(current);
            if (current.Action == TftpClientWriteAction.Completed)
            {
                return CreateWriteTransferResult(
                    state,
                    timeSource,
                    operationStartedMilliseconds);
            }

            TftpClientWriteResult timerResult =
                state.EvaluateWriteTimer(cancellationToken);
            if (timerResult.Action != TftpClientWriteAction.Wait)
            {
                current = timerResult;
                continue;
            }

            UdpDatagramReceiveResult received =
                datagramSocket.ReceiveBoundedDatagramOrTimeout(
                    receiveBuffer,
                    receiveBuffer.Length,
                    timerResult.WaitMilliseconds,
                    cancellationToken);
            if (!received.HasDatagram)
            {
                current = state.EvaluateWriteTimer(cancellationToken);
                continue;
            }

            TftpPacketDecodeResult decoded = TftpClientDatagramDecoder.Decode(
                codec,
                receiveBuffer.AsSpan(0, received.Length),
                received.ExceededMaximumLength);
            if (!decoded.Success)
            {
                HandleMalformedWriteDatagram(
                    state,
                    datagramSocket,
                    received.RemoteEndPoint,
                    decoded.FailureMessage,
                    cancellationToken);
                current = timerResult;
                continue;
            }

            bool observeRemoteError = true;
            if (decoded.Packet.Opcode == TftpOpcode.Error &&
                !state.EnteredSender &&
                TftpOptionNegotiator.CanRetryRequestWithoutOptions(
                    state.TransferOptions,
                    state.ActiveRequestedOptions,
                    state.RetriedWithoutOptions,
                    decoded.Packet.ErrorCode))
            {
                observeRemoteError = false;
            }

            if (observeRemoteError)
            {
                ObserveRemoteErrorPacket(
                    TftpTransferDirection.Write,
                    state.TransferOptions.Mode,
                    received.RemoteEndPoint,
                    decoded.Packet,
                    current.BytesTransferred,
                    current.EncodedBytesTransferred,
                    current.RetransmissionCount,
                    current.DuplicatePacketCount,
                    current.NegotiatedOptions);
            }

            current = state.ProcessWritePacket(
                decoded.Packet,
                received.RemoteEndPoint,
                cancellationToken);
        }
    }

    internal void SendClientPacket(
        UdpDatagramSocket datagramSocket,
        TftpPacket? packet,
        IPEndPoint? targetEndPoint,
        CancellationToken cancellationToken)
    {
        if (packet == null)
        {
            return;
        }

        if (targetEndPoint == null)
        {
            throw new TftpProtocolException(
                "An outbound client packet requires a target endpoint.",
                false,
                0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        datagramSocket.Socket.SendTo(
            encoded.AsSpan(),
            SocketFlags.None,
            targetEndPoint);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal void HandleMalformedReadDatagram(
        TftpClientReadStateMachine state,
        UdpDatagramSocket datagramSocket,
        IPEndPoint peerEndPoint,
        string message,
        CancellationToken cancellationToken)
    {
        TftpPeerValidation validation =
            state.PeerTracker.ClassifyPeer(peerEndPoint);
        if (validation == TftpPeerValidation.UnexpectedAddress)
        {
            return;
        }

        if (validation == TftpPeerValidation.UnknownTransferId)
        {
            datagramSocket.SendUnknownTransferIdError(
                peerEndPoint,
                TftpProtocolValues.MaximumRequestPacketLength,
                cancellationToken);
            return;
        }

        TftpPacket error = CreateClientErrorPacket(
            TftpErrorCode.IllegalOperation,
            message);
        SendClientPacket(
            datagramSocket,
            error,
            peerEndPoint,
            cancellationToken);
        ObserveTransferState(
            TftpTransferDirection.Read,
            TftpTransferState.Failed,
            state.TransferOptions.Mode,
            peerEndPoint,
            0,
            0,
            state.RetransmissionTimer.TotalRetransmissionCount,
            0,
            state.NegotiatedOptions,
            true,
            TftpErrorCode.IllegalOperation,
            message,
            0);
        throw new TftpProtocolException(message, false, 0);
    }

    internal void HandleMalformedWriteDatagram(
        TftpClientWriteStateMachine state,
        UdpDatagramSocket datagramSocket,
        IPEndPoint peerEndPoint,
        string message,
        CancellationToken cancellationToken)
    {
        TftpPeerValidation validation =
            state.PeerTracker.ClassifyPeer(peerEndPoint);
        if (validation == TftpPeerValidation.UnexpectedAddress)
        {
            return;
        }

        if (validation == TftpPeerValidation.UnknownTransferId)
        {
            datagramSocket.SendUnknownTransferIdError(
                peerEndPoint,
                TftpProtocolValues.MaximumRequestPacketLength,
                cancellationToken);
            return;
        }

        TftpPacket error = CreateClientErrorPacket(
            TftpErrorCode.IllegalOperation,
            message);
        SendClientPacket(
            datagramSocket,
            error,
            peerEndPoint,
            cancellationToken);
        ObserveTransferState(
            TftpTransferDirection.Write,
            TftpTransferState.Failed,
            state.TransferOptions.Mode,
            peerEndPoint,
            0,
            0,
            state.RetransmissionTimer.TotalRetransmissionCount,
            0,
            state.NegotiatedOptions,
            true,
            TftpErrorCode.IllegalOperation,
            message,
            0);
        throw new TftpProtocolException(message, false, 0);
    }

    internal void ThrowForReadActionFailure(TftpClientReadResult result)
    {
        if (!result.HasFailure)
        {
            return;
        }

        if (result.IsOptionFailure)
        {
            throw new TftpOptionNegotiationException(
                result.FailureMessage,
                result.FailureOptionName,
                result.FailureOptionValue);
        }

        throw new TftpProtocolException(
            result.FailureMessage,
            false,
            0);
    }

    internal void ThrowForWriteActionFailure(TftpClientWriteResult result)
    {
        if (!result.HasFailure)
        {
            return;
        }

        if (result.IsOptionFailure)
        {
            throw new TftpOptionNegotiationException(
                result.FailureMessage,
                result.FailureOptionName,
                result.FailureOptionValue);
        }

        throw new TftpProtocolException(
            result.FailureMessage,
            false,
            0);
    }

    internal TftpPacket CreateClientErrorPacket(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }

    internal TftpTransferResult CreateReadTransferResult(
        TftpClientReadStateMachine state,
        TftpSystemTimeSource timeSource,
        long operationStartedMilliseconds)
    {
        TftpReceiverStateMachine? receiver = state.Receiver;
        if (receiver == null)
        {
            throw new TftpProtocolException(
                "A completed client read has no receiver state.",
                false,
                0);
        }

        TftpTransferResult result = new TftpTransferResult();
        result.Direction = TftpTransferDirection.Read;
        result.State = TftpTransferState.Completed;
        result.Mode = state.TransferOptions.Mode;
        result.FinalEndpoint = CreateEndpointSnapshot(
            state.PeerTracker.ExpectedAddress,
            state.PeerTracker.TransferPort);
        result.BytesTransferred = receiver.BytesTransferred;
        result.EncodedBytesTransferred = receiver.EncodedBytesTransferred;
        result.ElapsedMilliseconds = GetElapsedMilliseconds(
            timeSource,
            operationStartedMilliseconds);
        result.RetransmissionCount =
            state.RetransmissionTimer.TotalRetransmissionCount;
        result.DuplicatePacketCount = receiver.DuplicatePacketCount;
        result.EffectiveBlockSize = state.EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = state.EffectiveTimeoutSeconds;
        result.HasTransferSize = state.HasTransferSize;
        result.TransferSize = state.TransferSize;
        result.NegotiatedOptions = CopyClientOptions(state.NegotiatedOptions);
        return result;
    }

    internal TftpTransferResult CreateWriteTransferResult(
        TftpClientWriteStateMachine state,
        TftpSystemTimeSource timeSource,
        long operationStartedMilliseconds)
    {
        TftpSenderStateMachine? sender = state.Sender;
        if (sender == null)
        {
            throw new TftpProtocolException(
                "A completed client write has no sender state.",
                false,
                0);
        }

        TftpTransferResult result = new TftpTransferResult();
        result.Direction = TftpTransferDirection.Write;
        result.State = TftpTransferState.Completed;
        result.Mode = state.TransferOptions.Mode;
        result.FinalEndpoint = CreateEndpointSnapshot(
            state.PeerTracker.ExpectedAddress,
            state.PeerTracker.TransferPort);
        result.BytesTransferred = sender.BytesTransferred;
        result.EncodedBytesTransferred = sender.EncodedBytesTransferred;
        result.ElapsedMilliseconds = GetElapsedMilliseconds(
            timeSource,
            operationStartedMilliseconds);
        result.RetransmissionCount =
            state.RetransmissionTimer.TotalRetransmissionCount;
        result.DuplicatePacketCount = sender.DuplicatePacketCount;
        result.EffectiveBlockSize = state.EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = state.EffectiveTimeoutSeconds;
        result.HasTransferSize = state.HasTransferSize;
        result.TransferSize = state.TransferSize;
        result.NegotiatedOptions = CopyClientOptions(state.NegotiatedOptions);
        return result;
    }

    internal long GetElapsedMilliseconds(
        TftpSystemTimeSource timeSource,
        long operationStartedMilliseconds)
    {
        long now = timeSource.GetTimestampMilliseconds();
        if (now < operationStartedMilliseconds)
        {
            return 0;
        }

        return now - operationStartedMilliseconds;
    }

    internal void ObserveReadAction(
        TftpClientReadStateMachine state,
        TftpClientReadResult result)
    {
        TftpTransferState transferState = TftpTransferState.Receiving;
        string message = "Client read transfer is receiving data.";
        if (result.Action == TftpClientReadAction.SendRequest)
        {
            transferState = TftpTransferState.Requesting;
            message = "Sending the client read request.";
        }
        else if (result.Action == TftpClientReadAction.SendAcknowledgement &&
            result.Packet != null && result.Packet.BlockNumber == 0)
        {
            transferState = TftpTransferState.Negotiating;
            message = "Acknowledging client read option negotiation.";
        }
        else if (result.IsDallying)
        {
            transferState = TftpTransferState.Dallying;
            message = "Client read transfer is dallying after final data.";
        }
        else if (result.Action == TftpClientReadAction.Completed)
        {
            transferState = TftpTransferState.Completed;
            message = "Client read transfer completed.";
        }
        else if (result.HasFailure)
        {
            transferState = TftpTransferState.Failed;
            message = result.FailureMessage;
        }

        IPEndPoint peerEndPoint = ResultReadPeer(state, result);
        ushort blockNumber = ResultPacketBlockNumber(result.Packet);
        ObserveTransferState(
            TftpTransferDirection.Read,
            transferState,
            state.TransferOptions.Mode,
            peerEndPoint,
            result.BytesTransferred,
            result.EncodedBytesTransferred,
            result.RetransmissionCount,
            result.DuplicatePacketCount,
            result.NegotiatedOptions,
            result.HasFailure,
            result.FailureErrorCode,
            message,
            blockNumber);
    }

    internal void ObserveWriteAction(
        TftpClientWriteStateMachine state,
        TftpClientWriteResult result)
    {
        TftpTransferState transferState = TftpTransferState.Sending;
        string message = "Client write transfer is sending data.";
        if (result.Action == TftpClientWriteAction.SendRequest)
        {
            transferState = TftpTransferState.Requesting;
            message = "Sending the client write request.";
        }
        else if (result.Action == TftpClientWriteAction.SendData &&
            result.EnteredSender &&
            result.NegotiatedOptions.Count > 0 &&
            result.Packet != null &&
            result.Packet.BlockNumber == 1 &&
            result.BytesTransferred == 0)
        {
            transferState = TftpTransferState.Negotiating;
            message = "Client write option negotiation completed.";
        }
        else if (result.Action == TftpClientWriteAction.Completed)
        {
            transferState = TftpTransferState.Completed;
            message = "Client write transfer completed.";
        }
        else if (result.HasFailure)
        {
            transferState = TftpTransferState.Failed;
            message = result.FailureMessage;
        }

        IPEndPoint peerEndPoint = ResultWritePeer(state, result);
        ushort blockNumber = ResultPacketBlockNumber(result.Packet);
        ObserveTransferState(
            TftpTransferDirection.Write,
            transferState,
            state.TransferOptions.Mode,
            peerEndPoint,
            result.BytesTransferred,
            result.EncodedBytesTransferred,
            result.RetransmissionCount,
            result.DuplicatePacketCount,
            result.NegotiatedOptions,
            result.HasFailure,
            result.FailureErrorCode,
            message,
            blockNumber);
    }

    internal IPEndPoint ResultReadPeer(
        TftpClientReadStateMachine state,
        TftpClientReadResult result)
    {
        if (result.TargetEndPoint != null)
        {
            return result.TargetEndPoint;
        }

        int port = state.InitialServerEndPoint.Port;
        if (state.PeerTracker.IsTransferPortLocked)
        {
            port = state.PeerTracker.TransferPort;
        }

        return new IPEndPoint(state.PeerTracker.ExpectedAddress, port);
    }

    internal IPEndPoint ResultWritePeer(
        TftpClientWriteStateMachine state,
        TftpClientWriteResult result)
    {
        if (result.TargetEndPoint != null)
        {
            return result.TargetEndPoint;
        }

        int port = state.InitialServerEndPoint.Port;
        if (state.PeerTracker.IsTransferPortLocked)
        {
            port = state.PeerTracker.TransferPort;
        }

        return new IPEndPoint(state.PeerTracker.ExpectedAddress, port);
    }

    internal ushort ResultPacketBlockNumber(TftpPacket? packet)
    {
        if (packet == null)
        {
            return 0;
        }

        if (packet.Opcode == TftpOpcode.Data ||
            packet.Opcode == TftpOpcode.Acknowledgement)
        {
            return packet.BlockNumber;
        }

        return 0;
    }

    internal void ObserveRemoteErrorPacket(
        TftpTransferDirection direction,
        TftpTransferMode mode,
        IPEndPoint peerEndPoint,
        TftpPacket packet,
        long bytesTransferred,
        long encodedBytesTransferred,
        int retransmissionCount,
        int duplicatePacketCount,
        List<TftpOption> negotiatedOptions)
    {
        if (packet.Opcode != TftpOpcode.Error)
        {
            return;
        }

        string message = packet.ErrorMessage;
        if (message == null)
        {
            message = "";
        }

        ObserveTransferState(
            direction,
            TftpTransferState.Failed,
            mode,
            peerEndPoint,
            bytesTransferred,
            encodedBytesTransferred,
            retransmissionCount,
            duplicatePacketCount,
            negotiatedOptions,
            true,
            packet.ErrorCode,
            message,
            0);
    }

    internal void ObserveConfiguredState(
        TftpTransferDirection direction,
        TftpTransferState state,
        TftpTransferMode mode,
        TftpEndpoint endpoint,
        string message)
    {
        ITftpTransferObserver? observer = Observer;
        if (observer == null)
        {
            return;
        }

        TftpTransferEvent transferEvent = new TftpTransferEvent();
        transferEvent.Direction = direction;
        transferEvent.State = state;
        transferEvent.Mode = mode;
        transferEvent.Peer = CopyConfiguredEndpoint(endpoint);
        transferEvent.Message = message;
        observer.Observe(transferEvent);
    }

    internal void ObserveTransferState(
        TftpTransferDirection direction,
        TftpTransferState state,
        TftpTransferMode mode,
        IPEndPoint peerEndPoint,
        long bytesTransferred,
        long encodedBytesTransferred,
        int retransmissionCount,
        int duplicatePacketCount,
        List<TftpOption> negotiatedOptions,
        bool hasError,
        TftpErrorCode errorCode,
        string message,
        ushort blockNumber)
    {
        ITftpTransferObserver? observer = Observer;
        if (observer == null)
        {
            return;
        }

        TftpTransferEvent transferEvent = new TftpTransferEvent();
        transferEvent.Direction = direction;
        transferEvent.State = state;
        transferEvent.Mode = mode;
        transferEvent.Peer = CreateEndpointSnapshot(
            peerEndPoint.Address,
            peerEndPoint.Port);
        transferEvent.BlockNumber = blockNumber;
        transferEvent.BytesTransferred = bytesTransferred;
        transferEvent.EncodedBytesTransferred = encodedBytesTransferred;
        transferEvent.RetransmissionCount = retransmissionCount;
        transferEvent.DuplicatePacketCount = duplicatePacketCount;
        transferEvent.NegotiatedOptions = CopyClientOptions(negotiatedOptions);
        transferEvent.HasError = hasError;
        transferEvent.ErrorCode = errorCode;
        transferEvent.Message = message;
        observer.Observe(transferEvent);
    }

    internal TftpEndpoint CopyConfiguredEndpoint(TftpEndpoint endpoint)
    {
        TftpEndpoint copy = new TftpEndpoint();
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.LocalAddress = endpoint.LocalAddress;
        copy.AddressFamily = endpoint.AddressFamily;
        return copy;
    }

    internal TftpEndpoint CreateEndpointSnapshot(
        IPAddress address,
        int port)
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = address.ToString();
        endpoint.Port = port;
        endpoint.AddressFamily = address.AddressFamily;
        return endpoint;
    }

    internal List<TftpOption> CopyClientOptions(List<TftpOption> options)
    {
        List<TftpOption> copies = new List<TftpOption>();
        foreach (TftpOption option in options)
        {
            TftpOption copy = new TftpOption();
            copy.Name = option.Name;
            copy.Value = option.Value;
            copies.Add(copy);
        }

        return copies;
    }
}
