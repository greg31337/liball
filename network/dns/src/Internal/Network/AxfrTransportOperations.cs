using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class DnsAxfrExchangeResult
{
    internal DnsTransportStatus Status = DnsTransportStatus.NetworkError;
    internal List<DnsMessage> Messages = new List<DnsMessage>();
    internal long ByteCount;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal interface IAxfrExchangeProvider
{
    DnsAxfrExchangeResult ReceiveMessages(
        DnsTransferRequest request,
        DnsMessage query,
        byte[] queryBytes,
        DnsClockValue deadline);
}

internal class TcpAxfrExchangeProvider : IAxfrExchangeProvider
{
    internal TcpAxfrExchangeProvider()
    {
    }

    public DnsAxfrExchangeResult ReceiveMessages(
        DnsTransferRequest request,
        DnsMessage query,
        byte[] queryBytes,
        DnsClockValue deadline)
    {
        return AxfrTransportOperations.ReceiveMessages(
            request,
            query,
            queryBytes,
            deadline);
    }
}

internal static class AxfrTransportOperations
{
    internal static DnsAxfrExchangeResult ReceiveMessages(
        DnsTransferRequest request,
        DnsMessage query,
        byte[] queryBytes,
        DnsClockValue deadline)
    {
        DnsAxfrExchangeResult result = new DnsAxfrExchangeResult();
        List<IPEndPoint> endpoints = EndpointOperations.Resolve(request.Endpoint);
        IPEndPoint endpoint = endpoints[0];
        Socket socket = new Socket(
            endpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        Task connected = socket.ConnectAsync(endpoint);
        while (!connected.IsCompleted)
        {
            if (request.CancellationRequested)
            {
                socket.Close();
                result.Status = DnsTransportStatus.Cancelled;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                socket.Close();
                result.Status = DnsTransportStatus.TimedOut;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!connected.IsCompletedSuccessfully)
        {
            socket.Close();
            return result;
        }

        TransportCancellationState cancellation =
            new TransportCancellationState();
        cancellation.CancellationRequested = request.CancellationRequested;
        byte[] frame = CreateFrame(queryBytes);
        SocketIoResult sent = SocketOperations.SendAll(
            socket,
            frame,
            0,
            frame.Length,
            deadline,
            cancellation);
        if (!sent.Success)
        {
            ApplyIoFailure(result, sent);
            socket.Close();
            return result;
        }

        bool complete = false;
        while (!complete)
        {
            if (request.CancellationRequested)
            {
                result.Status = DnsTransportStatus.Cancelled;
                socket.Close();
                return result;
            }

            if (result.Messages.Count >= request.MaximumMessages)
            {
                result.Status = DnsTransportStatus.ResponseTooLarge;
                AddDiagnostic(
                    result,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "transfer.maximumMessages",
                    "The AXFR response exceeded the message limit.");
                socket.Close();
                return result;
            }

            SocketIoResult prefix = SocketOperations.ReceiveExact(
                socket,
                DnsProtocolLimits.TcpLengthPrefixLength,
                deadline,
                cancellation);
            if (!prefix.Success)
            {
                ApplyFrameFailure(result, prefix, "length prefix");
                socket.Close();
                return result;
            }

            int length = (prefix.Bytes[0] << 8) | prefix.Bytes[1];
            if (length == 0)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                AddDiagnostic(
                    result,
                    DnsDiagnosticCode.InvalidTcpFrame,
                    "tcpFrame.messageLength",
                    "An AXFR TCP frame cannot be empty.");
                socket.Close();
                return result;
            }

            if (length > request.TransportOptions.MaximumResponseLength ||
                result.ByteCount > request.MaximumBytes - length)
            {
                result.Status = DnsTransportStatus.ResponseTooLarge;
                AddDiagnostic(
                    result,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "transfer.maximumBytes",
                    "The AXFR response exceeded the wire-octet limit.");
                socket.Close();
                return result;
            }

            SocketIoResult payload = SocketOperations.ReceiveExact(
                socket,
                length,
                deadline,
                cancellation);
            if (!payload.Success)
            {
                ApplyFrameFailure(result, payload, "message payload");
                socket.Close();
                return result;
            }

            DnsDecodeOptions options = new DnsDecodeOptions();
            options.MaximumMessageLength =
                request.TransportOptions.MaximumResponseLength;
            DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
                payload.Bytes,
                options);
            if (!decoded.Success || decoded.Message == null)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
                socket.Close();
                return result;
            }

            result.Messages.Add(decoded.Message);
            result.ByteCount = result.ByteCount + length;
            complete = TransferIsComplete(request, result.Messages);
        }

        socket.Close();
        result.Status = DnsTransportStatus.Success;
        return result;
    }

    static byte[] CreateFrame(byte[] messageBytes)
    {
        byte[] result = new byte[
            DnsProtocolLimits.TcpLengthPrefixLength + messageBytes.Length];
        result[0] = (byte)(messageBytes.Length >> 8);
        result[1] = (byte)(messageBytes.Length & 255);
        Array.Copy(
            messageBytes,
            0,
            result,
            DnsProtocolLimits.TcpLengthPrefixLength,
            messageBytes.Length);
        return result;
    }

    static bool TransferIsComplete(
        DnsTransferRequest request,
        List<DnsMessage> messages)
    {
        if (messages.Count > 0 &&
            MessageOperations.ReadEffectiveResponseCode(
                messages[messages.Count - 1]) != 0)
        {
            return true;
        }

        DnsResourceRecord? opening = null;
        DnsResourceRecord? final = null;
        int answerCount = 0;
        int soaCount = 0;
        int messageIndex = 0;
        while (messageIndex < messages.Count)
        {
            DnsMessage message = messages[messageIndex];
            int answerIndex = 0;
            while (answerIndex < message.Answers.Count)
            {
                DnsResourceRecord record = message.Answers[answerIndex];
                final = record;
                answerCount = answerCount + 1;
                int typeValue = DnsHeaderCodec.ResolveCode(
                    record.RecordTypeValue,
                    (int)record.RecordType);
                if (typeValue == (int)DnsRecordType.Soa)
                {
                    if (opening == null)
                    {
                        opening = record;
                    }

                    soaCount = soaCount + 1;
                }

                answerIndex = answerIndex + 1;
            }

            messageIndex = messageIndex + 1;
        }

        if (request.Kind == DnsTransferKind.Ixfr && answerCount == 1 &&
            soaCount == 1)
        {
            return true;
        }

        if (opening == null || final == null || soaCount < 2 ||
            !SoaSerialsEqual(opening, final))
        {
            return false;
        }

        return true;
    }

    static bool SoaSerialsEqual(
        DnsResourceRecord left,
        DnsResourceRecord right)
    {
        int leftType = DnsHeaderCodec.ResolveCode(
            left.RecordTypeValue,
            (int)left.RecordType);
        int rightType = DnsHeaderCodec.ResolveCode(
            right.RecordTypeValue,
            (int)right.RecordType);
        return leftType == (int)DnsRecordType.Soa &&
            rightType == (int)DnsRecordType.Soa &&
            left.Data != null && left.Data.Soa != null &&
            right.Data != null && right.Data.Soa != null &&
            left.Data.Soa.Serial == right.Data.Soa.Serial;
    }

    static void ApplyIoFailure(
        DnsAxfrExchangeResult result,
        SocketIoResult io)
    {
        if (io.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
        }
        else if (io.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
        }
        else
        {
            result.Status = DnsTransportStatus.NetworkError;
        }
    }

    static void ApplyFrameFailure(
        DnsAxfrExchangeResult result,
        SocketIoResult io,
        string field)
    {
        if (io.Cancelled || io.TimedOut)
        {
            ApplyIoFailure(result, io);
            return;
        }

        result.Status = DnsTransportStatus.ProtocolError;
        AddDiagnostic(
            result,
            DnsDiagnosticCode.InvalidTcpFrame,
            "tcpFrame",
            $"The AXFR {field} ended before all declared bytes arrived.");
    }

    static void AddDiagnostic(
        DnsAxfrExchangeResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
