using System.Collections.Generic;

namespace http;

internal class Http2FlowControl
{
    internal int receiveWindow = Http2Constants.DefaultInitialWindowSize;
    internal int sendWindow = Http2Constants.DefaultInitialWindowSize;
    internal int unreleasedConnectionCapacity;
    internal int pendingConnectionCredit;
    internal Dictionary<int, int> unreleasedStreamCapacity =
        new Dictionary<int, int>();
    internal Dictionary<int, int> pendingStreamCredit =
        new Dictionary<int, int>();

    internal void ResetFlowControl()
    {
        receiveWindow = Http2Constants.DefaultInitialWindowSize;
        sendWindow = Http2Constants.DefaultInitialWindowSize;
        unreleasedConnectionCapacity = 0;
        pendingConnectionCredit = 0;
        unreleasedStreamCapacity = new Dictionary<int, int>();
        pendingStreamCredit = new Dictionary<int, int>();
    }

    internal Http2ConnectionResult ConsumeConnectionData(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        long adjusted = (long)receiveWindow - frame.FlowControlledLength;
        if (frame.FlowControlledLength < 0 || adjusted < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorScope = Http2ErrorScope.Connection;
            result.HasErrorCode = true;
            result.ErrorCode = Http2ErrorCode.FlowControlError;
            result.ErrorCodeValue = (uint)Http2ErrorCode.FlowControlError;
            result.IsKnownErrorCode = true;
            result.ErrorCodeName = "http2-connection-flow-control-exceeded";
            result.ErrorMessage =
                "The DATA frame exceeds the connection receive window.";
            return result;
        }

        receiveWindow = (int)adjusted;
        return result;
    }

    internal Http2ConnectionResult ReserveSendCapacity(
        Http2Action action,
        Http2Frame frame,
        Http2StreamStateMachine streamState)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        int required = frame.FlowControlledLength;
        if (required < 0 || action.Capacity < 0 ||
            action.Capacity != 0 && action.Capacity != required)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-capacity-reservation";
            result.ErrorMessage =
                "A DATA action capacity must be zero or match its flow-controlled payload length.";
            return result;
        }

        Http2StreamSnapshot? stream = streamState.FindStream(frame.StreamId);
        if (stream == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-send-flow-control-stream-missing";
            result.ErrorMessage =
                "Outbound DATA requires an existing stream flow-control window.";
            return result;
        }

        action.Capacity = required;
        if (required > 0 &&
            (required > sendWindow || required > stream.SendWindow))
        {
            result.Status = HttpOperationStatus.NeedOutput;
            result.ErrorCodeName = "http2-send-flow-control-blocked";
            result.ErrorMessage =
                "Outbound DATA is blocked by the connection or stream send window.";
            return result;
        }

        sendWindow = sendWindow - required;
        stream.SendWindow = stream.SendWindow - required;
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal Http2ConnectionResult ApplyRemoteWindowUpdate(
        Http2Frame frame,
        Http2StreamStateMachine streamState)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (!frame.HasWindowIncrement || frame.WindowIncrement <= 0 ||
            frame.WindowIncrement > Http2Constants.MaximumWindowSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorScope = frame.StreamId == 0 ?
                Http2ErrorScope.Connection : Http2ErrorScope.Stream;
            result.HasErrorCode = true;
            result.ErrorCode = Http2ErrorCode.ProtocolError;
            result.ErrorCodeValue = (uint)Http2ErrorCode.ProtocolError;
            result.IsKnownErrorCode = true;
            result.ErrorCodeName = "invalid-http2-window-update-increment";
            result.ErrorMessage =
                "A WINDOW_UPDATE increment must be a positive 31-bit value.";
            return result;
        }

        if (frame.StreamId == 0)
        {
            long connectionWindow =
                (long)sendWindow + frame.WindowIncrement;
            if (connectionWindow > Http2Constants.MaximumWindowSize)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorScope = Http2ErrorScope.Connection;
                result.HasErrorCode = true;
                result.ErrorCode = Http2ErrorCode.FlowControlError;
                result.ErrorCodeValue =
                    (uint)Http2ErrorCode.FlowControlError;
                result.IsKnownErrorCode = true;
                result.ErrorCodeName =
                    "http2-connection-window-update-overflow";
                result.ErrorMessage =
                    "The WINDOW_UPDATE increment overflows the connection send window.";
                return result;
            }

            sendWindow = (int)connectionWindow;
            return result;
        }

        Http2StreamSnapshot? stream = streamState.FindStream(frame.StreamId);
        if (stream == null || stream.StreamState == Http2StreamState.Closed)
        {
            return result;
        }

        long streamWindow =
            (long)stream.SendWindow + frame.WindowIncrement;
        if (streamWindow > Http2Constants.MaximumWindowSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorScope = Http2ErrorScope.Stream;
            result.HasErrorCode = true;
            result.ErrorCode = Http2ErrorCode.FlowControlError;
            result.ErrorCodeValue = (uint)Http2ErrorCode.FlowControlError;
            result.IsKnownErrorCode = true;
            result.ErrorCodeName = "http2-stream-window-update-overflow";
            result.ErrorMessage =
                "The WINDOW_UPDATE increment overflows the stream send window.";
            return result;
        }

        stream.SendWindow = (int)streamWindow;
        return result;
    }

    internal Http2ConnectionResult ConsumeStreamData(
        Http2Frame frame,
        Http2StreamSnapshot stream)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        long adjusted =
            (long)stream.ReceiveWindow - frame.FlowControlledLength;
        if (frame.FlowControlledLength < 0 || adjusted < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorScope = Http2ErrorScope.Stream;
            result.HasErrorCode = true;
            result.ErrorCode = Http2ErrorCode.FlowControlError;
            result.ErrorCodeValue = (uint)Http2ErrorCode.FlowControlError;
            result.IsKnownErrorCode = true;
            result.ErrorCodeName = "http2-stream-flow-control-exceeded";
            result.ErrorMessage =
                "The DATA frame exceeds the stream receive window.";
            return result;
        }

        stream.ReceiveWindow = (int)adjusted;
        return result;
    }

    internal void RegisterDeliveredData(
        Http2ConnectionResult result,
        Http2Frame frame,
        Http2StreamSnapshot stream,
        int streamInitialWindow)
    {
        int contentLength = frame.Data.Length;
        unreleasedConnectionCapacity =
            unreleasedConnectionCapacity + contentLength;
        int streamCapacity = 0;
        if (unreleasedStreamCapacity.ContainsKey(stream.StreamId))
        {
            streamCapacity = unreleasedStreamCapacity[stream.StreamId];
        }

        unreleasedStreamCapacity[stream.StreamId] =
            streamCapacity + contentLength;
        int automaticCredit = frame.FlowControlledLength - contentLength;
        if (automaticCredit > 0)
        {
            pendingConnectionCredit =
                pendingConnectionCredit + automaticCredit;
            if (stream.StreamState != Http2StreamState.Closed)
            {
                int streamCredit = 0;
                if (pendingStreamCredit.ContainsKey(stream.StreamId))
                {
                    streamCredit = pendingStreamCredit[stream.StreamId];
                }

                pendingStreamCredit[stream.StreamId] =
                    streamCredit + automaticCredit;
            }
        }

        if (stream.StreamState == Http2StreamState.Closed)
        {
            pendingStreamCredit.Remove(stream.StreamId);
        }

        FlushCredits(result, stream, streamInitialWindow);
    }

    internal void RegisterRejectedData(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        pendingConnectionCredit =
            pendingConnectionCredit + frame.FlowControlledLength;
        FlushCredits(result, null, 0);
    }

    internal Http2ConnectionResult ReleaseCapacity(
        int streamId,
        int capacity,
        Http2StreamStateMachine streamState)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        if (capacity <= 0 || streamId < 0 ||
            streamId > Http2Constants.MaximumStreamIdentifier)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-capacity-release";
            result.ErrorMessage =
                "A capacity release requires a valid target and positive capacity.";
            return result;
        }

        if (streamId == 0)
        {
            if (capacity > unreleasedConnectionCapacity)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName =
                    "http2-connection-capacity-over-release";
                result.ErrorMessage =
                    "Released connection capacity exceeds delivered DATA capacity.";
                return result;
            }

            unreleasedConnectionCapacity =
                unreleasedConnectionCapacity - capacity;
            pendingConnectionCredit = pendingConnectionCredit + capacity;
            result.Status = HttpOperationStatus.Done;
            FlushCredits(result, null, 0);
            return result;
        }

        int availableStreamCapacity = 0;
        if (unreleasedStreamCapacity.ContainsKey(streamId))
        {
            availableStreamCapacity = unreleasedStreamCapacity[streamId];
        }

        if (capacity > availableStreamCapacity)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-stream-capacity-over-release";
            result.ErrorMessage =
                "Released stream capacity exceeds delivered DATA capacity.";
            return result;
        }

        int remainingStreamCapacity = availableStreamCapacity - capacity;
        if (remainingStreamCapacity == 0)
        {
            unreleasedStreamCapacity.Remove(streamId);
        }
        else
        {
            unreleasedStreamCapacity[streamId] = remainingStreamCapacity;
        }

        int connectionCapacity = capacity;
        if (connectionCapacity > unreleasedConnectionCapacity)
        {
            connectionCapacity = unreleasedConnectionCapacity;
        }

        unreleasedConnectionCapacity =
            unreleasedConnectionCapacity - connectionCapacity;
        pendingConnectionCredit =
            pendingConnectionCredit + connectionCapacity;
        Http2StreamSnapshot? stream = streamState.FindStream(streamId);
        if (stream != null && stream.StreamState != Http2StreamState.Closed)
        {
            int streamCredit = 0;
            if (pendingStreamCredit.ContainsKey(streamId))
            {
                streamCredit = pendingStreamCredit[streamId];
            }

            pendingStreamCredit[streamId] = streamCredit + capacity;
        }
        else
        {
            pendingStreamCredit.Remove(streamId);
        }

        result.Status = HttpOperationStatus.Done;
        FlushCredits(result, stream, streamState.localInitialWindowSize);
        return result;
    }

    internal void FlushCredits(
        Http2ConnectionResult result,
        Http2StreamSnapshot? stream,
        int streamInitialWindow)
    {
        int connectionThreshold =
            (Http2Constants.DefaultInitialWindowSize + 1) / 2;
        if (pendingConnectionCredit > 0 &&
            (pendingConnectionCredit >= connectionThreshold ||
                receiveWindow == 0))
        {
            int increment = pendingConnectionCredit;
            receiveWindow = receiveWindow + increment;
            pendingConnectionCredit = 0;
            result.Actions.Add(CreateWindowUpdateAction(0, increment));
        }

        if (stream == null)
        {
            return;
        }

        if (stream.StreamState == Http2StreamState.Closed)
        {
            pendingStreamCredit.Remove(stream.StreamId);
            return;
        }

        int streamCredit = 0;
        if (pendingStreamCredit.ContainsKey(stream.StreamId))
        {
            streamCredit = pendingStreamCredit[stream.StreamId];
        }

        int streamThreshold = streamInitialWindow / 2 +
            streamInitialWindow % 2;
        if (streamThreshold < 1)
        {
            streamThreshold = 1;
        }

        if (streamCredit > 0 &&
            (streamCredit >= streamThreshold || stream.ReceiveWindow <= 0))
        {
            stream.ReceiveWindow = stream.ReceiveWindow + streamCredit;
            pendingStreamCredit.Remove(stream.StreamId);
            result.Actions.Add(CreateWindowUpdateAction(
                stream.StreamId, streamCredit));
        }
    }

    internal void FlushAvailableCredits(
        Http2ConnectionResult result,
        Http2StreamStateMachine streamState)
    {
        FlushCredits(result, null, 0);
        int index = 0;
        while (index < streamState.streams.Count)
        {
            FlushCredits(
                result,
                streamState.streams[index],
                streamState.localInitialWindowSize);
            index = index + 1;
        }
    }

    internal void DiscardClosedStreamCredits(
        Http2StreamStateMachine streamState)
    {
        List<int> discarded = new List<int>();
        foreach (KeyValuePair<int, int> credit in pendingStreamCredit)
        {
            Http2StreamSnapshot? stream =
                streamState.FindStream(credit.Key);
            if (stream == null ||
                stream.StreamState == Http2StreamState.Closed)
            {
                discarded.Add(credit.Key);
            }
        }

        int index = 0;
        while (index < discarded.Count)
        {
            pendingStreamCredit.Remove(discarded[index]);
            index = index + 1;
        }
    }

    internal Http2Action CreateWindowUpdateAction(
        int streamId,
        int increment)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.WindowUpdate;
        frame.StreamId = streamId;
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = increment;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.ReleaseCapacity;
        action.Frame = frame;
        action.HasStreamId = streamId != 0;
        action.StreamId = streamId;
        action.Capacity = increment;
        return action;
    }

    internal void CopyFlowControlToSnapshot(Http2ConnectionSnapshot snapshot)
    {
        snapshot.ReceiveWindow = receiveWindow;
        snapshot.SendWindow = sendWindow;
        snapshot.UnreleasedReceiveCapacity =
            unreleasedConnectionCapacity;
        snapshot.PendingReceiveWindowCredit = pendingConnectionCredit;
        snapshot.IsFlowControlBlocked = sendWindow <= 0;
        int index = 0;
        while (index < snapshot.Streams.Count)
        {
            Http2StreamSnapshot stream = snapshot.Streams[index];
            if (unreleasedStreamCapacity.ContainsKey(stream.StreamId))
            {
                stream.UnreleasedReceiveCapacity =
                    unreleasedStreamCapacity[stream.StreamId];
            }

            if (pendingStreamCredit.ContainsKey(stream.StreamId))
            {
                stream.PendingReceiveWindowCredit =
                    pendingStreamCredit[stream.StreamId];
            }

            stream.IsFlowControlBlocked =
                sendWindow <= 0 || stream.SendWindow <= 0;
            index = index + 1;
        }
    }
}
