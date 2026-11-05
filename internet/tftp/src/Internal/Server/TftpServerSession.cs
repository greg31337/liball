using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace tftp;

internal class TftpServerSession
{
    internal string RequestIdentity;
    internal TftpPacket RequestPacket;
    internal TftpServerRequest ServerRequest;
    internal IPEndPoint ClientEndPoint;
    internal UdpDatagramSocket TransferSocket;
    internal ITftpServerSessionHandler Handler;
    internal CancellationTokenSource Cancellation;
    internal Task<TftpSessionOutcome>? ExecutionTask;
    internal byte[] RetainedResponse;
    internal bool HasRetainedResponse;
    internal int DuplicateInitialRequestCount;
    internal bool ResourcesClosed;
    internal object SyncRoot;

    internal TftpServerSession(
        string requestIdentity,
        IPEndPoint clientEndPoint,
        IPAddress localAddress,
        TftpPacket requestPacket,
        ITftpServerSessionHandler handler,
        CancellationToken cancellationToken)
    {
        if (requestIdentity == null || requestIdentity.Length == 0)
        {
            throw new TftpProtocolException(
                "The server request identity cannot be empty.",
                false,
                0);
        }

        if (clientEndPoint == null || localAddress == null ||
            requestPacket == null || handler == null)
        {
            throw new TftpProtocolException(
                "The server session inputs cannot be null.",
                false,
                0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        RequestIdentity = requestIdentity;
        ClientEndPoint = new IPEndPoint(
            clientEndPoint.Address,
            clientEndPoint.Port);
        RequestPacket = CopyPacket(requestPacket);
        ServerRequest = CreateServerRequest(RequestPacket, ClientEndPoint);
        TransferSocket = UdpDatagramSocket.BindLocalSocket(
            localAddress,
            0,
            cancellationToken);
        Handler = handler;
        Cancellation = new CancellationTokenSource();
        ExecutionTask = null;
        RetainedResponse = [];
        HasRetainedResponse = false;
        DuplicateInitialRequestCount = 0;
        ResourcesClosed = false;
        SyncRoot = new object();
    }

    internal void StartExecution()
    {
        lock (SyncRoot)
        {
            if (ExecutionTask != null)
            {
                throw new TftpProtocolException(
                    "The server session has already started.",
                    false,
                    0);
            }

            ExecutionTask = Task.Run(() => Handler.ExecuteSession(
                this,
                Cancellation.Token));
        }
    }

    internal int SendAndRetainResponse(
        byte[] response,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (response == null || response.Length == 0)
        {
            throw new TftpProtocolException(
                "A retained server response cannot be empty.",
                false,
                0);
        }

        byte[] retainedCopy = new byte[response.Length];
        response.CopyTo(retainedCopy, 0);
        lock (SyncRoot)
        {
            if (ResourcesClosed)
            {
                throw new TftpProtocolException(
                    "The server session socket is closed.",
                    false,
                    0);
            }

            RetainedResponse = retainedCopy;
            HasRetainedResponse = true;
            return TransferSocket.Socket.SendTo(
                retainedCopy,
                SocketFlags.None,
                ClientEndPoint);
        }
    }

    internal bool RegisterDuplicateAndResend(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (SyncRoot)
        {
            DuplicateInitialRequestCount += 1;
            if (!HasRetainedResponse || ResourcesClosed)
            {
                return false;
            }

            TransferSocket.Socket.SendTo(
                RetainedResponse,
                SocketFlags.None,
                ClientEndPoint);
            return true;
        }
    }

    internal int SendErrorResponse(
        TftpPacket errorPacket,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] encoded = TftpServerErrorMapper.EncodeErrorPacket(errorPacket);
        lock (SyncRoot)
        {
            if (ResourcesClosed)
            {
                throw new TftpProtocolException(
                    "The server session socket is closed.",
                    false,
                    0);
            }

            return TransferSocket.Socket.SendTo(
                encoded,
                SocketFlags.None,
                ClientEndPoint);
        }
    }

    internal bool IsExecutionComplete()
    {
        lock (SyncRoot)
        {
            if (ExecutionTask == null)
            {
                return false;
            }

            return ExecutionTask.IsCompleted;
        }
    }

    internal TftpSessionOutcome GetExecutionOutcome()
    {
        lock (SyncRoot)
        {
            if (ExecutionTask == null || !ExecutionTask.IsCompleted)
            {
                throw new TftpProtocolException(
                    "The server session has not completed.",
                    false,
                    0);
            }

            if (ExecutionTask.IsCompletedSuccessfully)
            {
                TftpSessionOutcome completedOutcome = ExecutionTask.Result;
                if (completedOutcome != null)
                {
                    return completedOutcome;
                }
            }

            TftpSessionOutcome failureOutcome = new TftpSessionOutcome();
            if (ExecutionTask.IsCanceled)
            {
                failureOutcome.TerminalState = TftpTransferState.Cancelled;
                failureOutcome.Message = "The server transfer was cancelled.";
                return failureOutcome;
            }

            failureOutcome.TerminalState = TftpTransferState.Failed;
            failureOutcome.HasError = true;
            failureOutcome.ErrorCode = TftpErrorCode.Undefined;
            AggregateException? taskException = ExecutionTask.Exception;
            if (taskException == null)
            {
                failureOutcome.Message =
                    "The server transfer failed without an exception detail.";
            }
            else
            {
                Exception baseException = taskException.GetBaseException();
                failureOutcome.Message = baseException.Message;
            }

            return failureOutcome;
        }
    }

    internal void CancelExecution()
    {
        Cancellation.Cancel();
    }

    internal void CloseResources()
    {
        lock (SyncRoot)
        {
            if (ResourcesClosed)
            {
                return;
            }

            TransferSocket.CloseSocket();
            ResourcesClosed = true;
        }
    }

    internal static TftpPacket CopyPacket(TftpPacket source)
    {
        TftpPacket copy = new TftpPacket();
        copy.Opcode = source.Opcode;
        if (source.Filename == null)
        {
            copy.Filename = "";
        }
        else
        {
            copy.Filename = source.Filename;
        }

        if (source.Mode == null)
        {
            copy.Mode = "";
        }
        else
        {
            copy.Mode = source.Mode;
        }

        copy.BlockNumber = source.BlockNumber;
        if (source.Payload == null)
        {
            copy.Payload = [];
        }
        else
        {
            copy.Payload = new byte[source.Payload.Length];
            source.Payload.CopyTo(copy.Payload, 0);
        }

        copy.ErrorCode = source.ErrorCode;
        if (source.ErrorMessage == null)
        {
            copy.ErrorMessage = "";
        }
        else
        {
            copy.ErrorMessage = source.ErrorMessage;
        }

        copy.Options = CopyOptions(source.Options);
        return copy;
    }

    internal static TftpServerRequest CreateServerRequest(
        TftpPacket packet,
        IPEndPoint clientEndPoint)
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = packet.Filename;
        if (packet.Opcode == TftpOpcode.WriteRequest)
        {
            request.Direction = TftpTransferDirection.Write;
        }
        else
        {
            request.Direction = TftpTransferDirection.Read;
        }

        if (string.Equals(
            packet.Mode,
            TftpProtocolValues.NetasciiMode,
            StringComparison.OrdinalIgnoreCase))
        {
            request.Mode = TftpTransferMode.Netascii;
        }
        else
        {
            request.Mode = TftpTransferMode.Octet;
        }

        request.ClientEndpoint.Host = clientEndPoint.Address.ToString();
        request.ClientEndpoint.Port = clientEndPoint.Port;
        request.ClientEndpoint.AddressFamily = clientEndPoint.AddressFamily;
        request.RequestedOptions = CopyOptions(packet.Options);
        return request;
    }

    internal static List<TftpOption> CopyOptions(List<TftpOption> source)
    {
        List<TftpOption> copies = [];
        if (source == null)
        {
            return copies;
        }

        int optionIndex = 0;
        while (optionIndex < source.Count)
        {
            TftpOption sourceOption = source[optionIndex];
            TftpOption copy = new TftpOption();
            if (sourceOption != null)
            {
                if (sourceOption.Name != null)
                {
                    copy.Name = sourceOption.Name;
                }

                if (sourceOption.Value != null)
                {
                    copy.Value = sourceOption.Value;
                }
            }

            copies.Add(copy);
            optionIndex += 1;
        }

        return copies;
    }
}
