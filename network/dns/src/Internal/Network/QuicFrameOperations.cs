using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class QuicFrameResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool ProtocolError;
    internal bool TooLarge;
    internal DnsTransportStatus FailureStatus = DnsTransportStatus.NetworkError;
    internal byte[] Bytes = [];
    internal string? DiagnosticReason;
}

internal class QuicReadResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool Closed;
    internal DnsTransportStatus FailureStatus = DnsTransportStatus.NetworkError;
    internal int BytesProcessed;
    internal byte[] Bytes = [];
}

internal static class QuicFrameOperations
{
    internal static QuicFrameResult WriteQuery(
        Stream stream,
        byte[] queryBytes,
        DnsTransportRequest request,
        DnsClockValue deadline,
        CancellationToken cancellationToken)
    {
        ValidateOperation(stream, request, deadline);
        if (queryBytes == null || queryBytes.Length == 0 ||
            queryBytes.Length > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException("queryBytes");
        }

        byte[] frame = CreateFrame(queryBytes);
        Task writeTask = stream.WriteAsync(
            frame.AsMemory(), cancellationToken).AsTask();
        QuicFrameResult result = new QuicFrameResult();
        while (!writeTask.IsCompleted)
        {
            if (request.CancellationRequested)
            {
                result.Cancelled = true;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.TimedOut = true;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!writeTask.IsCompletedSuccessfully)
        {
            result.FailureStatus =
                QuicFailureOperations.ReadTaskStatus(writeTask);
            return result;
        }

        result.Success = true;
        return result;
    }

    internal static QuicFrameResult ReadResponse(
        Stream stream,
        int maximumResponseLength,
        DnsTransportRequest request,
        DnsClockValue deadline,
        CancellationToken cancellationToken)
    {
        ValidateOperation(stream, request, deadline);
        if (maximumResponseLength < 1 ||
            maximumResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException("maximumResponseLength");
        }

        QuicFrameResult result = new QuicFrameResult();
        QuicReadResult prefix = ReadExact(
            stream,
            DnsProtocolLimits.TcpLengthPrefixLength,
            request,
            deadline,
            cancellationToken);
        if (!prefix.Success)
        {
            ApplyReadFailure(result, prefix);
            if (prefix.Closed)
            {
                result.ProtocolError = true;
                result.DiagnosticReason =
                    "The DNS-over-QUIC stream ended before its length prefix was complete.";
            }

            return result;
        }

        int responseLength = (prefix.Bytes[0] << 8) | prefix.Bytes[1];
        if (responseLength == 0)
        {
            result.ProtocolError = true;
            result.DiagnosticReason =
                "A DNS-over-QUIC stream cannot declare a zero-length message.";
            return result;
        }

        if (responseLength > maximumResponseLength)
        {
            result.TooLarge = true;
            return result;
        }

        QuicReadResult payload = ReadExact(
            stream,
            responseLength,
            request,
            deadline,
            cancellationToken);
        result.Bytes = payload.Bytes;
        if (!payload.Success)
        {
            ApplyReadFailure(result, payload);
            if (payload.Closed)
            {
                result.ProtocolError = true;
                result.DiagnosticReason =
                    "The DNS-over-QUIC stream ended before its declared message was complete.";
            }

            return result;
        }

        byte[] extra = new byte[1];
        Task<int> trailingTask = stream.ReadAsync(
            extra.AsMemory(), cancellationToken).AsTask();
        while (!trailingTask.IsCompleted)
        {
            if (request.CancellationRequested)
            {
                result.Cancelled = true;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.TimedOut = true;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!trailingTask.IsCompletedSuccessfully)
        {
            result.FailureStatus =
                QuicFailureOperations.ReadTaskStatus(trailingTask);
            return result;
        }

        if (trailingTask.Result != 0)
        {
            result.ProtocolError = true;
            result.DiagnosticReason =
                "A DNS-over-QUIC stream contained bytes after its declared message.";
            return result;
        }

        result.Success = true;
        return result;
    }

    internal static byte[] CreateFrame(byte[] messageBytes)
    {
        if (messageBytes == null || messageBytes.Length == 0 ||
            messageBytes.Length > DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException("messageBytes");
        }

        byte[] frame = new byte[
            DnsProtocolLimits.TcpLengthPrefixLength + messageBytes.Length];
        frame[0] = (byte)(messageBytes.Length >> 8);
        frame[1] = (byte)(messageBytes.Length & 255);
        Array.Copy(
            messageBytes,
            0,
            frame,
            DnsProtocolLimits.TcpLengthPrefixLength,
            messageBytes.Length);
        return frame;
    }

    static QuicReadResult ReadExact(
        Stream stream,
        int length,
        DnsTransportRequest request,
        DnsClockValue deadline,
        CancellationToken cancellationToken)
    {
        QuicReadResult result = new QuicReadResult();
        result.Bytes = new byte[length];
        while (result.BytesProcessed < length)
        {
            Task<int> readTask = stream.ReadAsync(
                result.Bytes.AsMemory(
                    result.BytesProcessed,
                    length - result.BytesProcessed),
                cancellationToken).AsTask();
            while (!readTask.IsCompleted)
            {
                if (request.CancellationRequested)
                {
                    result.Cancelled = true;
                    result.Bytes = CopyPrefix(
                        result.Bytes, result.BytesProcessed);
                    return result;
                }

                if (TransportTime.RemainingMilliseconds(deadline) == 0)
                {
                    result.TimedOut = true;
                    result.Bytes = CopyPrefix(
                        result.Bytes, result.BytesProcessed);
                    return result;
                }

                Thread.Sleep(1);
            }

            if (!readTask.IsCompletedSuccessfully)
            {
                result.FailureStatus =
                    QuicFailureOperations.ReadTaskStatus(readTask);
                result.Bytes = CopyPrefix(
                    result.Bytes, result.BytesProcessed);
                return result;
            }

            int received = readTask.Result;
            if (received == 0)
            {
                result.Closed = true;
                result.Bytes = CopyPrefix(
                    result.Bytes, result.BytesProcessed);
                return result;
            }

            result.BytesProcessed = result.BytesProcessed + received;
        }

        result.Success = true;
        return result;
    }

    static void ApplyReadFailure(
        QuicFrameResult result,
        QuicReadResult read)
    {
        result.TimedOut = read.TimedOut;
        result.Cancelled = read.Cancelled;
        result.FailureStatus = read.FailureStatus;
    }

    static void ValidateOperation(
        Stream stream,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        if (stream == null || request == null || deadline == null)
        {
            throw new ArgumentNullException("stream");
        }
    }

    static byte[] CopyPrefix(byte[] source, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(source, result, length);
        return result;
    }
}
