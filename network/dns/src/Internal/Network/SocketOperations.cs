using System;
using System.Net.Sockets;
using System.Threading;

namespace dns;

internal class SocketPollResult
{
    internal bool Ready;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool Error;
}

internal class SocketIoResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool Closed;
    internal int BytesProcessed;
    internal byte[] Bytes = [];
}

internal static class SocketOperations
{
    const int MaximumPollMilliseconds = 50;

    internal static SocketPollResult WaitForReadable(
        Socket socket,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        return WaitForSocket(
            socket,
            deadline,
            cancellation,
            SelectMode.SelectRead);
    }

    internal static SocketPollResult WaitForWritable(
        Socket socket,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        return WaitForSocket(
            socket,
            deadline,
            cancellation,
            SelectMode.SelectWrite);
    }

    internal static SocketIoResult SendAll(
        Socket socket,
        byte[] bytes,
        int offset,
        int length,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        ValidateSocket(socket, deadline);
        ValidateBufferRange(bytes, offset, length);
        SocketIoResult result = new SocketIoResult();
        while (result.BytesProcessed < length)
        {
            SocketPollResult poll = WaitForWritable(
                socket,
                deadline,
                cancellation);
            if (!poll.Ready)
            {
                CopyPollFailure(result, poll);
                return result;
            }

            int sent = socket.Send(
                bytes,
                offset + result.BytesProcessed,
                length - result.BytesProcessed,
                SocketFlags.None);
            if (sent == 0)
            {
                result.Closed = true;
                return result;
            }

            result.BytesProcessed = result.BytesProcessed + sent;
        }

        result.Success = true;
        return result;
    }

    internal static SocketIoResult ReceiveExact(
        Socket socket,
        int length,
        DnsClockValue deadline,
        TransportCancellationState cancellation)
    {
        ValidateSocket(socket, deadline);
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        SocketIoResult result = new SocketIoResult();
        result.Bytes = new byte[length];
        while (result.BytesProcessed < length)
        {
            SocketPollResult poll = WaitForReadable(
                socket,
                deadline,
                cancellation);
            if (!poll.Ready)
            {
                CopyPollFailure(result, poll);
                result.Bytes = CopyPrefix(result.Bytes, result.BytesProcessed);
                return result;
            }

            int received = socket.Receive(
                result.Bytes,
                result.BytesProcessed,
                length - result.BytesProcessed,
                SocketFlags.None);
            if (received == 0)
            {
                result.Closed = true;
                result.Bytes = CopyPrefix(result.Bytes, result.BytesProcessed);
                return result;
            }

            result.BytesProcessed = result.BytesProcessed + received;
        }

        result.Success = true;
        return result;
    }

    static SocketPollResult WaitForSocket(
        Socket socket,
        DnsClockValue deadline,
        TransportCancellationState cancellation,
        SelectMode mode)
    {
        ValidateSocket(socket, deadline);
        if (cancellation == null)
        {
            throw new ArgumentNullException("cancellation");
        }

        SocketPollResult result = new SocketPollResult();
        while (true)
        {
            if (TransportCancellation.IsStateRequested(cancellation))
            {
                result.Cancelled = true;
                return result;
            }

            int remaining = TransportTime.RemainingMilliseconds(deadline);
            if (remaining == 0)
            {
                result.TimedOut = true;
                return result;
            }

            int pollMilliseconds = remaining;
            if (pollMilliseconds > MaximumPollMilliseconds)
            {
                pollMilliseconds = MaximumPollMilliseconds;
            }

            bool ready = socket.Poll(
                pollMilliseconds * 1000, mode);
            if (ready)
            {
                result.Ready = true;
                return result;
            }

            if (socket.Poll(0, SelectMode.SelectError))
            {
                result.Error = true;
                return result;
            }
        }
    }

    static void ValidateSocket(Socket socket, DnsClockValue deadline)
    {
        if (socket == null || deadline == null)
        {
            throw new ArgumentNullException("socket");
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

    static void CopyPollFailure(
        SocketIoResult result,
        SocketPollResult poll)
    {
        result.TimedOut = poll.TimedOut;
        result.Cancelled = poll.Cancelled;
        result.Closed = poll.Error;
    }

    static byte[] CopyPrefix(byte[] bytes, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(bytes, result, length);
        return result;
    }
}
