using System.Net.Sockets;
using websocket;

namespace websocket.Tests.Autobahn;

internal static class AutobahnConnectionDriver
{
    internal static void RunEcho(AutobahnOpenedConnection opened)
    {
        Run(opened, true, null);
    }

    internal static string ReadOneTextMessage(AutobahnOpenedConnection opened)
    {
        string? message = null;
        Run(
            opened,
            false,
            (messageType, payload) =>
            {
                if (messageType == WebSocketMessageType.Text && message == null)
                {
                    message = System.Text.Encoding.UTF8.GetString(payload);
                }
            });
        if (message == null)
        {
            throw new InvalidOperationException(
                "Autobahn control endpoint returned no text message.");
        }

        return message;
    }

    internal static void RunWithoutEcho(AutobahnOpenedConnection opened)
    {
        Run(opened, false, null);
    }

    static void Run(
        AutobahnOpenedConnection opened,
        bool echo,
        Action<WebSocketMessageType, byte[]>? onMessage)
    {
        bool discardInput = false;
        try
        {
            if (opened.TrailingBytes.Length != 0 &&
                !ProcessBytes(opened, opened.TrailingBytes, echo, onMessage))
            {
                discardInput = true;
            }

            byte[] input = new byte[65536];
            while (true)
            {
                FlushOutput(opened);
                WebSocketConnectionState state =
                    opened.Connection.GetConnectionInfo().State;
                if (state == WebSocketConnectionState.Closed ||
                    state == WebSocketConnectionState.Failed)
                {
                    return;
                }

                int count;
                try
                {
                    count = opened.Stream.Read(input, 0, input.Length);
                }
                catch (IOException)
                {
                    WebSocketConnectionInfo information =
                        opened.Connection.GetConnectionInfo();
                    if (!discardInput &&
                        (!information.HasLocalClose ||
                            !information.HasRemoteClose))
                    {
                        throw;
                    }

                    if (discardInput)
                    {
                        opened.Connection.ResetConnectionTransport();
                    }
                    else
                    {
                        opened.Connection.EndConnectionInput();
                    }

                    return;
                }

                if (count == 0)
                {
                    opened.Connection.EndConnectionInput();
                    FlushOutput(opened);
                    return;
                }

                if (discardInput)
                {
                    continue;
                }

                if (!ProcessBytes(
                    opened,
                    input.AsSpan(0, count),
                    echo,
                    onMessage))
                {
                    discardInput = true;
                }
            }
        }
        finally
        {
            opened.Stream.Dispose();
        }
    }

    static bool ProcessBytes(
        AutobahnOpenedConnection opened,
        ReadOnlySpan<byte> source,
        bool echo,
        Action<WebSocketMessageType, byte[]>? onMessage)
    {
        int offset = 0;
        while (offset < source.Length)
        {
            WebSocketFrameReadResult boundary =
                opened.BoundaryCodec.ReadFrame(source.Slice(offset));
            if (boundary.ConsumedCount == 0)
            {
                throw new InvalidOperationException(
                    "WebSocket frame boundary codec made no input progress: " +
                    boundary.Status);
            }

            WebSocketInboundFramePumpResult result =
                opened.Connection.ReceiveConnectionBytes(
                    source.Slice(offset, boundary.ConsumedCount));
            offset += result.ConsumedCount;
            DrainEvents(opened, echo, onMessage);
            FlushOutput(opened);
            if (boundary.EndOfFrame)
            {
                opened.BoundaryCodec.ResetFrameRead();
            }

            if (result.HasError)
            {
                return false;
            }

            if (result.ConsumedCount == 0)
            {
                throw new InvalidOperationException(
                    "WebSocket connection made no input progress: " + result.Status);
            }
        }

        DrainEvents(opened, echo, onMessage);
        FlushOutput(opened);
        return true;
    }

    static void DrainEvents(
        AutobahnOpenedConnection opened,
        bool echo,
        Action<WebSocketMessageType, byte[]>? onMessage)
    {
        WebSocketEvent? item = opened.Connection.ReadNextEvent();
        while (item != null)
        {
            if (item.Type == WebSocketEventType.MessageData)
            {
                byte[] payload = item.Payload
                    .AsSpan(item.PayloadOffset, item.PayloadCount)
                    .ToArray();
                if (onMessage != null)
                {
                    onMessage(item.MessageType, payload);
                }

                if (echo)
                {
                    EchoMessage(opened, item.MessageType, payload);
                }
            }

            item = opened.Connection.ReadNextEvent();
        }
    }

    static void EchoMessage(
        AutobahnOpenedConnection opened,
        WebSocketMessageType messageType,
        byte[] payload)
    {
        WebSocketSendRequest request = new WebSocketSendRequest
        {
            MessageType = messageType,
            Payload = payload,
            PayloadCount = payload.Length
        };
        WebSocketParseStatus status =
            opened.Connection.SendWholeMessage(request);
        while (status == WebSocketParseStatus.NeedOutputCapacity)
        {
            FlushOutput(opened);
            status = opened.Connection.SendWholeMessage(request);
        }

        if (status != WebSocketParseStatus.Complete)
        {
            throw new InvalidOperationException(
                "Echo message was rejected: " + status);
        }
    }

    static void FlushOutput(AutobahnOpenedConnection opened)
    {
        byte[] output = new byte[65536];
        while (opened.Connection.GetPendingConnectionWriteCount() != 0)
        {
            int count = opened.Connection.WriteConnectionBytes(output);
            if (count == 0)
            {
                throw new IOException("WebSocket connection made no output progress.");
            }

            opened.Stream.Write(output, 0, count);
        }
    }
}
