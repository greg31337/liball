using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class TlsIoResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool Closed;
    internal int BytesProcessed;
    internal byte[] Bytes = [];
}

internal static class TlsStreamOperations
{
    const int MaximumWriteChunkLength = 16384;

    internal static TlsIoResult WriteAll(
        SslStream stream,
        byte[] bytes,
        int offset,
        int length,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        ValidateStream(stream, deadline, cancellation);
        ValidateBufferRange(bytes, offset, length);
        TlsIoResult result = new TlsIoResult();
        while (result.BytesProcessed < length)
        {
            int count = length - result.BytesProcessed;
            if (count > MaximumWriteChunkLength)
            {
                count = MaximumWriteChunkLength;
            }

            Task writeTask = stream.WriteAsync(
                bytes.AsMemory(offset + result.BytesProcessed, count),
                CancellationToken.None).AsTask();
            if (!WaitForWriteTask(
                writeTask, result, deadline, cancellation))
            {
                return result;
            }

            result.BytesProcessed = result.BytesProcessed + count;
        }

        result.Success = true;
        return result;
    }

    internal static TlsIoResult ReadExact(
        SslStream stream,
        int length,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        ValidateStream(stream, deadline, cancellation);
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        TlsIoResult result = new TlsIoResult();
        result.Bytes = new byte[length];
        while (result.BytesProcessed < length)
        {
            Task<int> readTask = stream.ReadAsync(
                result.Bytes.AsMemory(
                    result.BytesProcessed,
                    length - result.BytesProcessed),
                CancellationToken.None).AsTask();
            if (!WaitForReadTask(
                readTask, result, deadline, cancellation))
            {
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

    static bool WaitForWriteTask(
        Task task,
        TlsIoResult result,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        while (!task.IsCompleted)
        {
            if (TransportCancellation.IsStateRequested(cancellation))
            {
                result.Cancelled = true;
                return false;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.TimedOut = true;
                return false;
            }

            Thread.Sleep(1);
        }

        if (!task.IsCompletedSuccessfully)
        {
            result.Closed = true;
            return false;
        }

        return true;
    }

    static bool WaitForReadTask(
        Task<int> task,
        TlsIoResult result,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        while (!task.IsCompleted)
        {
            if (TransportCancellation.IsStateRequested(cancellation))
            {
                result.Cancelled = true;
                return false;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.TimedOut = true;
                return false;
            }

            Thread.Sleep(1);
        }

        if (!task.IsCompletedSuccessfully)
        {
            result.Closed = true;
            return false;
        }

        return true;
    }

    static void ValidateStream(
        SslStream stream,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        if (stream == null || deadline == null || cancellation == null)
        {
            throw new ArgumentNullException("stream");
        }
    }

    static void ValidateBufferRange(byte[] bytes, int offset, int length)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (offset < 0 || length < 0 || offset > bytes.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    static byte[] CopyPrefix(byte[] bytes, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(bytes, result, length);
        return result;
    }
}
