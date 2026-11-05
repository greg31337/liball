using System.Collections.Generic;

namespace http;

internal class Http2PendingPriority
{
    internal int StreamId;
    internal HttpPriority Priority = new HttpPriority();
}

internal class Http2StreamStateMachine
{
    internal Http2EndpointRole endpointRole;
    internal int nextLocalStreamId;
    internal int highestRemoteStreamId;
    internal int activeLocalStreams;
    internal int activeRemoteStreams;
    internal int maximumRetainedClosedStreams;
    internal long maximumLocalConcurrentStreams = long.MaxValue;
    internal long maximumRemoteConcurrentStreams = long.MaxValue;
    internal bool remoteGoAwayReceived;
    internal int localInitialWindowSize =
        Http2Constants.DefaultInitialWindowSize;
    internal int remoteInitialWindowSize =
        Http2Constants.DefaultInitialWindowSize;
    internal List<Http2StreamSnapshot> streams =
        new List<Http2StreamSnapshot>();
    internal List<int> closedStreamOrder = new List<int>();
    internal List<Http2PendingPriority> pendingPriorities =
        new List<Http2PendingPriority>();

    internal void ResetStreams(
        Http2EndpointRole role,
        int maximumRetained)
    {
        endpointRole = role;
        nextLocalStreamId = role == Http2EndpointRole.Client ? 1 : 2;
        highestRemoteStreamId = 0;
        activeLocalStreams = 0;
        activeRemoteStreams = 0;
        maximumRetainedClosedStreams = maximumRetained;
        maximumLocalConcurrentStreams = long.MaxValue;
        maximumRemoteConcurrentStreams = long.MaxValue;
        remoteGoAwayReceived = false;
        localInitialWindowSize = Http2Constants.DefaultInitialWindowSize;
        remoteInitialWindowSize = Http2Constants.DefaultInitialWindowSize;
        streams = new List<Http2StreamSnapshot>();
        closedStreamOrder = new List<int>();
        pendingPriorities = new List<Http2PendingPriority>();
    }

    internal Http2ConnectionResult ValidateLocalFrame(Http2Action action)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (action.Frame == null)
        {
            return RejectStreamOperation(
                result,
                "invalid-http2-send-frame-action",
                "A send-frame action requires an HTTP/2 frame.");
        }

        Http2Frame frame = action.Frame;
        if (remoteGoAwayReceived &&
            (frame.FrameType == Http2FrameType.PushPromise ||
            frame.FrameType == Http2FrameType.Headers &&
                FindStream(frame.StreamId) == null))
        {
            return RejectStreamOperation(
                result,
                "http2-goaway-received",
                "New streams cannot be created after receiving GOAWAY.");
        }

        if (action.HasStreamId &&
            frame.StreamId != 0 && frame.StreamId != action.StreamId)
        {
            return RejectStreamOperation(
                result,
                "http2-action-stream-id-mismatch",
                "The action and frame stream identifiers must match.");
        }

        if (action.HasStreamId && frame.StreamId == 0)
        {
            frame.StreamId = action.StreamId;
        }

        if (frame.FrameType == Http2FrameType.Headers && frame.StreamId == 0)
        {
            if (endpointRole != Http2EndpointRole.Client)
            {
                return RejectStreamOperation(
                    result,
                    "http2-server-stream-requires-push",
                    "A server can create a stream only with PUSH_PROMISE.");
            }

            if (nextLocalStreamId == 0)
            {
                return RejectStreamOperation(
                    result,
                    "http2-stream-id-exhausted",
                    "No local HTTP/2 stream identifiers remain available.");
            }

            frame.StreamId = nextLocalStreamId;
        }

        if (frame.StreamId != 0)
        {
            action.HasStreamId = true;
            action.StreamId = frame.StreamId;
        }

        if (frame.FrameType == Http2FrameType.Headers)
        {
            Http2StreamSnapshot? existing = FindStream(frame.StreamId);
            if (existing == null)
            {
                if (endpointRole != Http2EndpointRole.Client)
                {
                    return RejectStreamOperation(
                        result,
                        "http2-server-stream-requires-push",
                        "A server can create a stream only with PUSH_PROMISE.");
                }

                Http2ConnectionResult identifierValidation =
                    ValidateNewLocalIdentifier(result, frame.StreamId);
                if (identifierValidation.Status != HttpOperationStatus.Done)
                {
                    return identifierValidation;
                }

                if (activeLocalStreams >= maximumLocalConcurrentStreams)
                {
                    return RejectStreamOperation(
                        result,
                        "http2-maximum-local-streams-reached",
                        "The peer concurrent-stream limit prevents opening another local stream.");
                }

                int pendingCommitments =
                    CountPendingPrioritiesAbove(frame.StreamId);
                if ((long)activeLocalStreams + pendingCommitments >=
                    maximumLocalConcurrentStreams)
                {
                    return RejectStreamOperation(
                        result,
                        "http2-priority-update-concurrency",
                        "Idle priority updates and active request streams reach the peer limit.");
                }
            }
            else if (existing.StreamState ==
                Http2StreamState.ReservedLocal &&
                activeLocalStreams >= maximumLocalConcurrentStreams)
            {
                return RejectStreamOperation(
                    result,
                    "http2-maximum-local-streams-reached",
                    "The peer concurrent-stream limit prevents activating the reserved stream.");
            }
        }

        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            if (endpointRole != Http2EndpointRole.Server)
            {
                return RejectStreamOperation(
                    result,
                    "http2-client-cannot-push",
                    "A client cannot send PUSH_PROMISE.");
            }

            Http2ConnectionResult associatedValidation =
                ValidateLocalLegality(result, frame);
            if (associatedValidation.Status != HttpOperationStatus.Done)
            {
                return associatedValidation;
            }

            Http2StreamSnapshot? associated = FindStream(frame.StreamId);
            if (associated == null || IsLocalIdentifier(frame.StreamId) ||
                associated.StreamState != Http2StreamState.Open &&
                associated.StreamState != Http2StreamState.HalfClosedRemote)
            {
                return RejectStreamOperation(
                    result,
                    "http2-invalid-push-associated-stream",
                    "PUSH_PROMISE must be sent on an open or remotely ended peer-initiated stream.");
            }

            if (!frame.HasPromisedStreamId)
            {
                if (nextLocalStreamId == 0)
                {
                    return RejectStreamOperation(
                        result,
                        "http2-stream-id-exhausted",
                        "No local HTTP/2 stream identifiers remain available.");
                }

                frame.HasPromisedStreamId = true;
                frame.PromisedStreamId = nextLocalStreamId;
            }

            if (FindStream(frame.PromisedStreamId) != null)
            {
                return RejectStreamOperation(
                    result,
                    "http2-promised-stream-id-reused",
                    "A promised stream identifier cannot be reused.");
            }

            return ValidateNewLocalIdentifier(result, frame.PromisedStreamId);
        }

        return ValidateLocalLegality(result, frame);
    }

    internal void ApplyRemoteConcurrencySettings(Http2Settings settings)
    {
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier ==
                Http2SettingIdentifiers.MaxConcurrentStreams)
            {
                maximumLocalConcurrentStreams = setting.Value;
            }

            index = index + 1;
        }
    }

    internal void ApplyLocalConcurrencySettings(Http2Settings settings)
    {
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier ==
                Http2SettingIdentifiers.MaxConcurrentStreams)
            {
                maximumRemoteConcurrentStreams = setting.Value;
            }

            index = index + 1;
        }
    }

    internal Http2ConnectionResult ApplyLocalFrame(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            Http2StreamSnapshot promised = CreateStream(
                frame.PromisedStreamId,
                Http2StreamState.ReservedLocal,
                true,
                frame.StreamId);
            InsertStream(promised);
            AdvanceNextLocalIdentifier(frame.PromisedStreamId);
            return result;
        }

        Http2StreamSnapshot? stream = FindStream(frame.StreamId);
        if (frame.FrameType == Http2FrameType.Headers && stream == null)
        {
            DiscardSkippedPendingPriorities(frame.StreamId);
            stream = CreateStream(
                frame.StreamId,
                Http2StreamState.Open,
                false,
                0);
            stream.SentHeaders = true;
            InsertStream(stream);
            AdvanceNextLocalIdentifier(frame.StreamId);
        }
        else if (frame.FrameType == Http2FrameType.Headers && stream != null)
        {
            stream.SentHeaders = true;
            if (stream.StreamState == Http2StreamState.ReservedLocal)
            {
                stream.StreamState = Http2StreamState.HalfClosedRemote;
                stream.RemoteEndStream = true;
                ActivateStream(stream);
            }
        }

        if (stream == null)
        {
            return result;
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            ApplyReset(stream, frame, true);
            return result;
        }

        if (frame.FrameType == Http2FrameType.Data)
        {
            stream.SentContentLength = stream.SentContentLength +
                frame.Data.Length;
        }

        if ((frame.FrameType == Http2FrameType.Headers ||
            frame.FrameType == Http2FrameType.Data) && frame.EndStream)
        {
            ApplyLocalEndStream(stream);
        }

        return result;
    }

    internal Http2ConnectionResult ApplyRemoteFrame(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        Http2ConnectionResult legality =
            ValidateRemoteLegality(result, frame);
        if (legality.Status != HttpOperationStatus.Done)
        {
            return legality;
        }

        Http2StreamSnapshot? existingStream = FindStream(frame.StreamId);
        if (existingStream != null &&
            existingStream.StreamState == Http2StreamState.Closed &&
            (existingStream.ResetByLocal &&
                frame.FrameType != Http2FrameType.PushPromise ||
            existingStream.ClosedByLocal &&
                (frame.FrameType == Http2FrameType.RstStream ||
                frame.FrameType == Http2FrameType.WindowUpdate)))
        {
            return result;
        }

        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            if (!frame.HasPromisedStreamId ||
                FindStream(frame.PromisedStreamId) != null)
            {
                return RejectStreamOperation(
                    result,
                    "http2-promised-stream-id-reused",
                    "A peer promised stream identifier must be new.");
            }

            Http2ConnectionResult validation = ValidateNewRemoteIdentifier(
                result, frame.PromisedStreamId);
            if (validation.Status != HttpOperationStatus.Done)
            {
                return validation;
            }

            Http2StreamSnapshot promised = CreateStream(
                frame.PromisedStreamId,
                Http2StreamState.ReservedRemote,
                true,
                frame.StreamId);
            InsertStream(promised);
            highestRemoteStreamId = frame.PromisedStreamId;
            return result;
        }

        Http2StreamSnapshot? stream = FindStream(frame.StreamId);
        if (frame.FrameType == Http2FrameType.Headers && stream == null)
        {
            Http2ConnectionResult validation = ValidateNewRemoteIdentifier(
                result, frame.StreamId);
            if (validation.Status != HttpOperationStatus.Done)
            {
                return validation;
            }

            DiscardSkippedPendingPriorities(frame.StreamId);
            stream = CreateStream(
                frame.StreamId,
                Http2StreamState.Open,
                false,
                0);
            stream.ReceivedHeaders = true;
            InsertStream(stream);
            highestRemoteStreamId = frame.StreamId;
        }
        else if (frame.FrameType == Http2FrameType.Headers && stream != null)
        {
            stream.ReceivedHeaders = true;
            if (stream.StreamState == Http2StreamState.ReservedRemote)
            {
                stream.StreamState = Http2StreamState.HalfClosedLocal;
                stream.LocalEndStream = true;
                ActivateStream(stream);
            }
        }

        if (stream == null)
        {
            return result;
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            ApplyReset(stream, frame, false);
            return result;
        }

        if (frame.FrameType == Http2FrameType.Data)
        {
            if (frame.Data != null)
            {
                stream.ReceivedContentLength = stream.ReceivedContentLength +
                    frame.Data.Length;
            }
        }

        if ((frame.FrameType == Http2FrameType.Headers ||
            frame.FrameType == Http2FrameType.Data) && frame.EndStream)
        {
            ApplyRemoteEndStream(stream);
        }

        return result;
    }

    internal Http2ConnectionResult ValidateNewLocalIdentifier(
        Http2ConnectionResult result,
        int streamId)
    {
        if (streamId <= 0 ||
            streamId > Http2Constants.MaximumStreamIdentifier ||
            !IsLocalIdentifier(streamId))
        {
            return RejectStreamOperation(
                result,
                "invalid-http2-local-stream-id",
                "A local stream identifier has the wrong range or parity.");
        }

        if (nextLocalStreamId == 0 || streamId < nextLocalStreamId)
        {
            return RejectStreamOperation(
                result,
                "http2-local-stream-id-not-monotonic",
                "Local stream identifiers must increase monotonically.");
        }

        return result;
    }

    internal Http2ConnectionResult ValidateNewRemoteIdentifier(
        Http2ConnectionResult result,
        int streamId)
    {
        if (streamId <= 0 ||
            streamId > Http2Constants.MaximumStreamIdentifier ||
            IsLocalIdentifier(streamId))
        {
            return RejectStreamOperation(
                result,
                "invalid-http2-remote-stream-id",
                "A peer stream identifier has the wrong range or parity.");
        }

        if (streamId <= highestRemoteStreamId)
        {
            return RejectStreamOperation(
                result,
                "http2-remote-stream-id-not-monotonic",
                "Peer stream identifiers must increase monotonically.");
        }

        return result;
    }

    internal bool IsLocalIdentifier(int streamId)
    {
        if (endpointRole == Http2EndpointRole.Client)
        {
            return streamId % 2 == 1;
        }

        return streamId % 2 == 0;
    }

    internal int MaximumRemoteStreamIdentifier()
    {
        return endpointRole == Http2EndpointRole.Server ?
            Http2Constants.MaximumStreamIdentifier :
            Http2Constants.MaximumStreamIdentifier - 1;
    }

    internal void ApplyReceivedGoAway(int lastStreamId)
    {
        remoteGoAwayReceived = true;
        List<Http2StreamSnapshot> affected =
            new List<Http2StreamSnapshot>();
        int index = 0;
        while (index < streams.Count)
        {
            Http2StreamSnapshot stream = streams[index];
            if (IsLocalIdentifier(stream.StreamId) &&
                stream.StreamId > lastStreamId)
            {
                affected.Add(stream);
            }

            index = index + 1;
        }

        int affectedIndex = 0;
        while (affectedIndex < affected.Count)
        {
            Http2StreamSnapshot stream = affected[affectedIndex];
            stream.IsRetryEligible = !stream.IsPushed;
            CloseStream(stream);
            affectedIndex = affectedIndex + 1;
        }
    }

    internal void AdvanceNextLocalIdentifier(int usedStreamId)
    {
        if (usedStreamId >= Http2Constants.MaximumStreamIdentifier - 1)
        {
            nextLocalStreamId = 0;
            return;
        }

        nextLocalStreamId = usedStreamId + 2;
    }

    internal Http2StreamSnapshot CreateStream(
        int streamId,
        Http2StreamState streamState,
        bool pushed,
        int associatedStreamId)
    {
        Http2StreamSnapshot stream = new Http2StreamSnapshot();
        stream.StreamId = streamId;
        stream.StreamState = streamState;
        stream.ReceiveWindow = localInitialWindowSize;
        stream.SendWindow = remoteInitialWindowSize;
        stream.IsPushed = pushed;
        if (pushed)
        {
            stream.HasAssociatedStreamId = true;
            stream.AssociatedStreamId = associatedStreamId;
        }

        if (IsActiveState(streamState) && IsLocalIdentifier(streamId))
        {
            activeLocalStreams = activeLocalStreams + 1;
        }
        else if (IsActiveState(streamState))
        {
            activeRemoteStreams = activeRemoteStreams + 1;
        }

        ApplyPendingPriority(stream);

        return stream;
    }

    internal bool CanApplyRemoteWindowSettings(Http2Settings settings)
    {
        int simulatedInitial = remoteInitialWindowSize;
        int settingIndex = 0;
        while (settingIndex < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[settingIndex];
            if (setting.Identifier ==
                Http2SettingIdentifiers.InitialWindowSize)
            {
                long accumulatedDelta =
                    (long)setting.Value - remoteInitialWindowSize;
                int streamIndex = 0;
                while (streamIndex < streams.Count)
                {
                    long adjusted = streams[streamIndex].SendWindow;
                    if (streams[streamIndex].StreamState !=
                        Http2StreamState.Closed)
                    {
                        adjusted = adjusted + accumulatedDelta;
                    }

                    if (adjusted > int.MaxValue || adjusted < int.MinValue)
                    {
                        return false;
                    }

                    streamIndex = streamIndex + 1;
                }

                simulatedInitial = (int)setting.Value;
            }

            settingIndex = settingIndex + 1;
        }

        return simulatedInitial >= 0;
    }

    internal bool CanApplyLocalWindowSettings(Http2Settings settings)
    {
        int simulatedInitial = localInitialWindowSize;
        int settingIndex = 0;
        while (settingIndex < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[settingIndex];
            if (setting.Identifier ==
                Http2SettingIdentifiers.InitialWindowSize)
            {
                long accumulatedDelta =
                    (long)setting.Value - localInitialWindowSize;
                int streamIndex = 0;
                while (streamIndex < streams.Count)
                {
                    long adjusted = streams[streamIndex].ReceiveWindow;
                    if (streams[streamIndex].StreamState !=
                        Http2StreamState.Closed)
                    {
                        adjusted = adjusted + accumulatedDelta;
                    }

                    if (adjusted > int.MaxValue || adjusted < int.MinValue)
                    {
                        return false;
                    }

                    streamIndex = streamIndex + 1;
                }

                simulatedInitial = (int)setting.Value;
            }

            settingIndex = settingIndex + 1;
        }

        return simulatedInitial >= 0;
    }

    internal void ApplyRemoteWindowSettings(Http2Settings settings)
    {
        int settingIndex = 0;
        while (settingIndex < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[settingIndex];
            if (setting.Identifier ==
                Http2SettingIdentifiers.InitialWindowSize)
            {
                int nextInitial = (int)setting.Value;
                int delta = nextInitial - remoteInitialWindowSize;
                int streamIndex = 0;
                while (streamIndex < streams.Count)
                {
                    if (streams[streamIndex].StreamState !=
                        Http2StreamState.Closed)
                    {
                        streams[streamIndex].SendWindow =
                            streams[streamIndex].SendWindow + delta;
                    }

                    streamIndex = streamIndex + 1;
                }

                remoteInitialWindowSize = nextInitial;
            }

            settingIndex = settingIndex + 1;
        }
    }

    internal void ApplyLocalWindowSettings(Http2Settings settings)
    {
        int settingIndex = 0;
        while (settingIndex < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[settingIndex];
            if (setting.Identifier ==
                Http2SettingIdentifiers.InitialWindowSize)
            {
                int nextInitial = (int)setting.Value;
                int delta = nextInitial - localInitialWindowSize;
                int streamIndex = 0;
                while (streamIndex < streams.Count)
                {
                    if (streams[streamIndex].StreamState !=
                        Http2StreamState.Closed)
                    {
                        streams[streamIndex].ReceiveWindow =
                            streams[streamIndex].ReceiveWindow + delta;
                    }

                    streamIndex = streamIndex + 1;
                }

                localInitialWindowSize = nextInitial;
            }

            settingIndex = settingIndex + 1;
        }
    }

    internal void InsertStream(Http2StreamSnapshot stream)
    {
        int index = 0;
        while (index < streams.Count &&
            streams[index].StreamId < stream.StreamId)
        {
            index = index + 1;
        }

        streams.Insert(index, stream);
    }

    internal Http2StreamSnapshot? FindStream(int streamId)
    {
        int index = 0;
        while (index < streams.Count)
        {
            if (streams[index].StreamId == streamId)
            {
                return streams[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void ApplyLocalEndStream(Http2StreamSnapshot stream)
    {
        if (stream.LocalEndStream)
        {
            return;
        }

        stream.LocalEndStream = true;
        if (stream.StreamState == Http2StreamState.Open)
        {
            stream.StreamState = Http2StreamState.HalfClosedLocal;
            return;
        }

        if (stream.StreamState == Http2StreamState.HalfClosedRemote)
        {
            CloseStream(stream);
        }
    }

    internal void ApplyRemoteEndStream(Http2StreamSnapshot stream)
    {
        if (stream.RemoteEndStream)
        {
            return;
        }

        stream.RemoteEndStream = true;
        if (stream.StreamState == Http2StreamState.Open)
        {
            stream.StreamState = Http2StreamState.HalfClosedRemote;
            return;
        }

        if (stream.StreamState == Http2StreamState.HalfClosedLocal)
        {
            CloseStream(stream);
        }
    }

    internal void ApplyReset(
        Http2StreamSnapshot stream,
        Http2Frame frame,
        bool sentByLocalEndpoint)
    {
        stream.IsReset = true;
        stream.ResetByLocal = sentByLocalEndpoint;
        if (sentByLocalEndpoint)
        {
            stream.ClosedByLocal = true;
        }

        stream.HasErrorCode = frame.HasErrorCode;
        stream.ErrorCode = frame.ErrorCode;
        stream.ErrorCodeValue = frame.ErrorCodeValue;
        stream.IsKnownErrorCode = frame.IsKnownErrorCode;
        stream.IsRetryEligible = !sentByLocalEndpoint &&
            frame.ErrorCodeValue ==
                (uint)Http2ErrorCode.RefusedStream &&
            !stream.IsPushed;
        CloseStream(stream);
    }

    internal void CloseStream(Http2StreamSnapshot stream)
    {
        if (stream.StreamState == Http2StreamState.Closed)
        {
            return;
        }

        bool wasActive = IsActiveState(stream.StreamState);
        if (stream.LocalEndStream)
        {
            stream.ClosedByLocal = true;
        }

        stream.StreamState = Http2StreamState.Closed;
        if (wasActive && IsLocalIdentifier(stream.StreamId))
        {
            activeLocalStreams = activeLocalStreams - 1;
        }
        else if (wasActive)
        {
            activeRemoteStreams = activeRemoteStreams - 1;
        }

        closedStreamOrder.Add(stream.StreamId);
        TrimClosedStreams();
    }

    internal void ActivateStream(Http2StreamSnapshot stream)
    {
        if (IsLocalIdentifier(stream.StreamId))
        {
            activeLocalStreams = activeLocalStreams + 1;
        }
        else
        {
            activeRemoteStreams = activeRemoteStreams + 1;
        }
    }

    internal bool IsActiveState(Http2StreamState streamState)
    {
        return streamState == Http2StreamState.Open ||
            streamState == Http2StreamState.HalfClosedLocal ||
            streamState == Http2StreamState.HalfClosedRemote;
    }

    internal void TrimClosedStreams()
    {
        while (closedStreamOrder.Count > maximumRetainedClosedStreams)
        {
            int streamId = closedStreamOrder[0];
            closedStreamOrder.RemoveAt(0);
            int index = 0;
            while (index < streams.Count)
            {
                if (streams[index].StreamId == streamId &&
                    streams[index].StreamState == Http2StreamState.Closed)
                {
                    streams.RemoveAt(index);
                    break;
                }

                index = index + 1;
            }
        }
    }

    internal void CopyStateToSnapshot(Http2ConnectionSnapshot snapshot)
    {
        snapshot.NextLocalStreamId = nextLocalStreamId;
        snapshot.HighestRemoteStreamId = highestRemoteStreamId;
        snapshot.ActiveLocalStreams = activeLocalStreams;
        snapshot.ActiveRemoteStreams = activeRemoteStreams;
        snapshot.MaximumLocalConcurrentStreams =
            maximumLocalConcurrentStreams;
        snapshot.MaximumRemoteConcurrentStreams =
            maximumRemoteConcurrentStreams;
        snapshot.LocalInitialStreamWindow = localInitialWindowSize;
        snapshot.RemoteInitialStreamWindow = remoteInitialWindowSize;
        snapshot.PendingPriorityCount = pendingPriorities.Count;
        int index = 0;
        while (index < streams.Count)
        {
            snapshot.Streams.Add(CopyStream(streams[index]));
            index = index + 1;
        }
    }

    internal Http2StreamSnapshot CopyStream(Http2StreamSnapshot source)
    {
        Http2StreamSnapshot copy = new Http2StreamSnapshot();
        copy.StreamId = source.StreamId;
        copy.StreamState = source.StreamState;
        copy.ReceiveWindow = source.ReceiveWindow;
        copy.SendWindow = source.SendWindow;
        copy.ReceivedHeaders = source.ReceivedHeaders;
        copy.SentHeaders = source.SentHeaders;
        copy.LocalEndStream = source.LocalEndStream;
        copy.RemoteEndStream = source.RemoteEndStream;
        copy.ReceivedContentLength = source.ReceivedContentLength;
        copy.SentContentLength = source.SentContentLength;
        copy.IsReset = source.IsReset;
        copy.ResetByLocal = source.ResetByLocal;
        copy.ClosedByLocal = source.ClosedByLocal;
        copy.HasErrorCode = source.HasErrorCode;
        copy.ErrorCode = source.ErrorCode;
        copy.ErrorCodeValue = source.ErrorCodeValue;
        copy.IsKnownErrorCode = source.IsKnownErrorCode;
        copy.IsPushed = source.IsPushed;
        copy.HasAssociatedStreamId = source.HasAssociatedStreamId;
        copy.AssociatedStreamId = source.AssociatedStreamId;
        copy.PushRequest = HttpSnapshotCopy.CopyRequest(source.PushRequest);
        copy.IsPushCacheable = source.IsPushCacheable;
        copy.IsRetryEligible = source.IsRetryEligible;
        copy.ClientPriority = CopyPriority(source.ClientPriority);
        copy.ServerPriority = CopyPriority(source.ServerPriority);
        copy.PriorityUpdate = CopyPriority(source.PriorityUpdate);
        copy.Priority = CopyPriority(source.Priority);
        return copy;
    }

    internal Http2ConnectionResult ApplyRemotePriorityUpdate(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (endpointRole != Http2EndpointRole.Server || frame.Priority == null)
        {
            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Connection,
                Http2ErrorCode.ProtocolError,
                "http2-invalid-priority-update-role",
                "Only a server can receive a valid PRIORITY_UPDATE frame.");
        }

        int streamId = frame.PrioritizedElementId;
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (frame.PrioritizedElementIsPush)
        {
            if (stream == null)
            {
                if (IsIdleIdentifier(streamId))
                {
                    return RejectRemoteFrame(
                        result,
                        Http2ErrorScope.Connection,
                        Http2ErrorCode.ProtocolError,
                        "http2-priority-update-idle-push",
                        "PRIORITY_UPDATE cannot reference an idle push stream.");
                }

                return result;
            }

            if (!stream.IsPushed)
            {
                return RejectRemoteFrame(
                    result,
                    Http2ErrorScope.Connection,
                    Http2ErrorCode.ProtocolError,
                    "http2-priority-update-invalid-push",
                    "A push PRIORITY_UPDATE must reference a pushed stream.");
            }

            if (stream.StreamState != Http2StreamState.Closed)
            {
                ApplyPriorityUpdateToStream(stream, frame.Priority);
            }

            return result;
        }

        if (stream != null)
        {
            if (stream.StreamState != Http2StreamState.HalfClosedLocal &&
                stream.StreamState != Http2StreamState.Closed)
            {
                ApplyPriorityUpdateToStream(stream, frame.Priority);
            }

            return result;
        }

        if (!IsIdleIdentifier(streamId))
        {
            return result;
        }

        int pendingIndex = FindPendingPriorityIndex(streamId);
        long committed = activeRemoteStreams + pendingPriorities.Count;
        if (pendingIndex < 0 && committed >= maximumRemoteConcurrentStreams)
        {
            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Connection,
                Http2ErrorCode.ProtocolError,
                "http2-priority-update-concurrency",
                "Idle priority updates and active request streams exceed the advertised limit.");
        }

        StorePendingPriority(streamId, frame.Priority);
        return result;
    }

    internal Http2ConnectionResult ApplyLocalPriorityUpdate(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (endpointRole != Http2EndpointRole.Client || frame.Priority == null)
        {
            return RejectStreamOperation(
                result,
                "http2-invalid-priority-update-role",
                "Only a client can send a valid PRIORITY_UPDATE frame.");
        }

        int streamId = frame.PrioritizedElementId;
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (frame.PrioritizedElementIsPush)
        {
            if (stream == null || !stream.IsPushed)
            {
                return RejectStreamOperation(
                    result,
                    "http2-priority-update-idle-push",
                    "PRIORITY_UPDATE cannot reference an idle push stream.");
            }

            if (stream.StreamState != Http2StreamState.Closed)
            {
                ApplyPriorityUpdateToStream(stream, frame.Priority);
            }

            return result;
        }

        if (stream != null)
        {
            if (stream.StreamState != Http2StreamState.HalfClosedRemote &&
                stream.StreamState != Http2StreamState.Closed)
            {
                ApplyPriorityUpdateToStream(stream, frame.Priority);
            }

            return result;
        }

        if (!IsIdleIdentifier(streamId))
        {
            return result;
        }

        int pendingIndex = FindPendingPriorityIndex(streamId);
        long committed = activeLocalStreams + pendingPriorities.Count;
        if (pendingIndex < 0 && committed >= maximumLocalConcurrentStreams)
        {
            return RejectStreamOperation(
                result,
                "http2-priority-update-concurrency",
                "Idle priority updates and active request streams exceed the peer limit.");
        }

        StorePendingPriority(streamId, frame.Priority);
        return result;
    }

    internal void ApplyClientPrioritySignal(
        int streamId,
        HttpPriority priority)
    {
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (stream == null)
        {
            return;
        }

        stream.ClientPriority = CopyPriority(priority);
        RefreshEffectivePriority(stream);
    }

    internal void ApplyServerPrioritySignal(
        int streamId,
        HttpPriority priority)
    {
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (stream == null)
        {
            return;
        }

        stream.ServerPriority = CopyPriority(priority);
        RefreshEffectivePriority(stream);
    }

    internal void ApplyPriorityUpdateToStream(
        Http2StreamSnapshot stream,
        HttpPriority priority)
    {
        stream.PriorityUpdate = CopyPriority(priority);
        RefreshEffectivePriority(stream);
    }

    internal void RefreshEffectivePriority(Http2StreamSnapshot stream)
    {
        if (stream.PriorityUpdate != null)
        {
            stream.Priority = CopyPriority(stream.PriorityUpdate);
            return;
        }

        if (stream.ClientPriority == null && stream.ServerPriority == null)
        {
            stream.Priority = null;
            return;
        }

        HttpPriority client = stream.ClientPriority ?? new HttpPriority();
        HttpPriority server = stream.ServerPriority ?? new HttpPriority();
        stream.Priority = PriorityCodec.MergeClientAndServer(client, server);
    }

    internal void StorePendingPriority(int streamId, HttpPriority priority)
    {
        int index = FindPendingPriorityIndex(streamId);
        HttpPriority stored = CopyPriority(priority) ?? new HttpPriority();
        if (index >= 0)
        {
            pendingPriorities[index].Priority = stored;
            return;
        }

        Http2PendingPriority pending = new Http2PendingPriority();
        pending.StreamId = streamId;
        pending.Priority = stored;
        pendingPriorities.Add(pending);
    }

    internal int FindPendingPriorityIndex(int streamId)
    {
        int index = 0;
        while (index < pendingPriorities.Count)
        {
            if (pendingPriorities[index].StreamId == streamId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ApplyPendingPriority(Http2StreamSnapshot stream)
    {
        int index = FindPendingPriorityIndex(stream.StreamId);
        if (index < 0)
        {
            return;
        }

        ApplyPriorityUpdateToStream(
            stream, pendingPriorities[index].Priority);
        pendingPriorities.RemoveAt(index);
    }

    internal void DiscardSkippedPendingPriorities(int streamId)
    {
        int index = pendingPriorities.Count - 1;
        while (index >= 0)
        {
            if (pendingPriorities[index].StreamId < streamId)
            {
                pendingPriorities.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal int CountPendingPrioritiesAbove(int streamId)
    {
        int count = 0;
        int index = 0;
        while (index < pendingPriorities.Count)
        {
            if (pendingPriorities[index].StreamId > streamId)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal HttpPriority? CopyPriority(HttpPriority? source)
    {
        return HttpSnapshotCopy.CopyPriority(source);
    }

    internal Http2ConnectionResult RejectStreamOperation(
        Http2ConnectionResult result,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCodeName = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal Http2ConnectionResult ValidateLocalLegality(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (frame.FrameType == Http2FrameType.Unknown || frame.StreamId == 0)
        {
            return result;
        }

        Http2StreamSnapshot? stream = FindStream(frame.StreamId);
        if (stream == null)
        {
            if (frame.FrameType == Http2FrameType.Priority)
            {
                return result;
            }

            if (frame.FrameType == Http2FrameType.Headers &&
                IsIdleIdentifier(frame.StreamId))
            {
                return result;
            }

            return RejectStreamOperation(
                result,
                IsIdleIdentifier(frame.StreamId) ?
                    "http2-local-frame-on-idle-stream" :
                    "http2-local-frame-on-closed-stream",
                IsIdleIdentifier(frame.StreamId) ?
                    "The frame cannot be sent on an idle stream." :
                    "The frame cannot be sent on a closed stream.");
        }

        if (IsLocalFrameAllowed(stream.StreamState, frame.FrameType))
        {
            return result;
        }

        return RejectStreamOperation(
            result,
            "http2-local-frame-invalid-for-stream-state",
            "The frame cannot be sent in the current stream state.");
    }

    internal Http2ConnectionResult ValidateRemoteLegality(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (frame.FrameType == Http2FrameType.Unknown || frame.StreamId == 0)
        {
            return result;
        }

        if (frame.FrameType == Http2FrameType.PushPromise &&
            endpointRole != Http2EndpointRole.Client)
        {
            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Connection,
                Http2ErrorCode.ProtocolError,
                "http2-server-cannot-receive-push",
                "A server cannot receive PUSH_PROMISE.");
        }

        Http2StreamSnapshot? stream = FindStream(frame.StreamId);
        if (frame.FrameType == Http2FrameType.PushPromise)
        {
            bool resetRace = stream != null &&
                stream.StreamState == Http2StreamState.Closed &&
                stream.ResetByLocal;
            if (!IsLocalIdentifier(frame.StreamId) || stream == null ||
                !resetRace && stream.StreamState != Http2StreamState.Open &&
                stream.StreamState != Http2StreamState.HalfClosedLocal)
            {
                return RejectRemoteFrame(
                    result,
                    Http2ErrorScope.Connection,
                    Http2ErrorCode.ProtocolError,
                    "http2-invalid-push-associated-stream",
                    "PUSH_PROMISE must be received on an open or locally ended local stream.");
            }
        }

        if (stream != null &&
            stream.StreamState == Http2StreamState.Closed &&
            stream.ResetByLocal)
        {
            return result;
        }

        if (stream != null &&
            frame.FrameType == Http2FrameType.Headers &&
            stream.StreamState == Http2StreamState.ReservedRemote &&
            activeRemoteStreams >= maximumRemoteConcurrentStreams)
        {
            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Stream,
                Http2ErrorCode.RefusedStream,
                "http2-maximum-remote-streams-exceeded",
                "The peer exceeded the advertised concurrent-stream limit.");
        }

        if (stream == null)
        {
            if (frame.FrameType == Http2FrameType.Headers)
            {
                Http2ConnectionResult identifierValidation =
                    ValidateNewRemoteIdentifier(result, frame.StreamId);
                if (identifierValidation.Status != HttpOperationStatus.Done)
                {
                    return identifierValidation;
                }
            }

            if (frame.FrameType == Http2FrameType.Priority)
            {
                return result;
            }

            if (IsIdleIdentifier(frame.StreamId))
            {
                if (frame.FrameType == Http2FrameType.Headers &&
                    endpointRole == Http2EndpointRole.Server &&
                    !IsLocalIdentifier(frame.StreamId))
                {
                    if (activeRemoteStreams >=
                        maximumRemoteConcurrentStreams)
                    {
                        return RejectRemoteFrame(
                            result,
                            Http2ErrorScope.Stream,
                            Http2ErrorCode.RefusedStream,
                            "http2-maximum-remote-streams-exceeded",
                            "The peer exceeded the advertised concurrent-stream limit.");
                    }

                    int pendingCommitments =
                        CountPendingPrioritiesAbove(frame.StreamId);
                    if ((long)activeRemoteStreams + pendingCommitments >=
                        maximumRemoteConcurrentStreams)
                    {
                        return RejectRemoteFrame(
                            result,
                            Http2ErrorScope.Connection,
                            Http2ErrorCode.ProtocolError,
                            "http2-priority-update-concurrency",
                            "Idle priority updates and active request streams exceed the advertised limit.");
                    }

                    return result;
                }

                return RejectRemoteFrame(
                    result,
                    Http2ErrorScope.Connection,
                    Http2ErrorCode.ProtocolError,
                    "http2-frame-on-idle-stream",
                    "The peer sent a frame that is not permitted on an idle stream.");
            }

            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Connection,
                Http2ErrorCode.StreamClosed,
                "http2-frame-on-closed-stream",
                "The peer sent a frame on a closed stream.");
        }

        if (IsRemoteFrameAllowed(stream.StreamState, frame.FrameType))
        {
            return result;
        }

        if (stream.StreamState == Http2StreamState.Closed &&
            stream.ClosedByLocal &&
            (frame.FrameType == Http2FrameType.RstStream ||
            frame.FrameType == Http2FrameType.WindowUpdate))
        {
            return result;
        }

        if (stream.StreamState == Http2StreamState.HalfClosedRemote)
        {
            return RejectRemoteFrame(
                result,
                Http2ErrorScope.Stream,
                Http2ErrorCode.StreamClosed,
                "http2-frame-after-remote-end",
                "The peer sent a frame after ending its sending direction.");
        }

        return RejectRemoteFrame(
            result,
            Http2ErrorScope.Connection,
            stream.StreamState == Http2StreamState.Closed ?
                Http2ErrorCode.StreamClosed : Http2ErrorCode.ProtocolError,
            stream.StreamState == Http2StreamState.Closed ?
                "http2-frame-on-closed-stream" :
                "http2-frame-invalid-for-stream-state",
            "The peer sent a frame that is not permitted in the current stream state.");
    }

    internal bool IsLateRemoteFrame(Http2Frame frame)
    {
        Http2StreamSnapshot? stream = FindStream(frame.StreamId);
        return stream != null &&
            stream.StreamState == Http2StreamState.Closed &&
            stream.ResetByLocal;
    }

    internal void ApplyPushRequestMetadata(
        int streamId,
        HttpRequest request)
    {
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (stream == null || !stream.IsPushed)
        {
            return;
        }

        stream.PushRequest = request;
        stream.IsPushCacheable = request.MethodMetadata != null &&
            request.MethodMetadata.IsSafe &&
            request.MethodMetadata.IsCacheable;
    }

    internal void CopyPushMetadataToMessage(
        int streamId,
        Http2MessageResult message)
    {
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (stream == null || !stream.IsPushed)
        {
            return;
        }

        message.IsPushed = true;
        message.HasAssociatedStreamId = stream.HasAssociatedStreamId;
        message.AssociatedStreamId = stream.AssociatedStreamId;
        message.PushRequest = stream.PushRequest;
        message.IsPushCacheable = stream.IsPushCacheable;
        if (message.EndStream)
        {
            message.IsComplete = true;
        }
    }

    internal bool IsIdleIdentifier(int streamId)
    {
        if (streamId <= 0 ||
            streamId > Http2Constants.MaximumStreamIdentifier)
        {
            return false;
        }

        if (IsLocalIdentifier(streamId))
        {
            return nextLocalStreamId != 0 && streamId >= nextLocalStreamId;
        }

        return streamId > highestRemoteStreamId;
    }

    internal bool IsLocalFrameAllowed(
        Http2StreamState state,
        Http2FrameType frameType)
    {
        if (state == Http2StreamState.ReservedLocal)
        {
            return frameType == Http2FrameType.Headers ||
                frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority;
        }

        if (state == Http2StreamState.ReservedRemote)
        {
            return frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.WindowUpdate;
        }

        if (state == Http2StreamState.HalfClosedLocal)
        {
            return frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.WindowUpdate ||
                frameType == Http2FrameType.Continuation;
        }

        if (state == Http2StreamState.Closed)
        {
            return frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.Continuation;
        }

        return true;
    }

    internal bool IsRemoteFrameAllowed(
        Http2StreamState state,
        Http2FrameType frameType)
    {
        if (state == Http2StreamState.ReservedLocal)
        {
            return frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.WindowUpdate;
        }

        if (state == Http2StreamState.ReservedRemote)
        {
            return frameType == Http2FrameType.Headers ||
                frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority;
        }

        if (state == Http2StreamState.HalfClosedRemote)
        {
            return frameType == Http2FrameType.RstStream ||
                frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.WindowUpdate ||
                frameType == Http2FrameType.Continuation;
        }

        if (state == Http2StreamState.Closed)
        {
            return frameType == Http2FrameType.Priority ||
                frameType == Http2FrameType.Continuation;
        }

        return true;
    }

    internal Http2ConnectionResult RejectRemoteFrame(
        Http2ConnectionResult result,
        Http2ErrorScope errorScope,
        Http2ErrorCode errorCode,
        string errorName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorScope = errorScope;
        result.HasErrorCode = true;
        result.ErrorCode = errorCode;
        result.ErrorCodeValue = (uint)errorCode;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal void CloseForStreamError(
        int streamId,
        Http2ErrorCode errorCode)
    {
        Http2StreamSnapshot? stream = FindStream(streamId);
        if (stream == null)
        {
            if (errorCode != Http2ErrorCode.RefusedStream ||
                streamId <= highestRemoteStreamId ||
                streamId <= 0 ||
                streamId > Http2Constants.MaximumStreamIdentifier ||
                IsLocalIdentifier(streamId))
            {
                return;
            }

            stream = CreateStream(
                streamId,
                Http2StreamState.Open,
                false,
                0);
            InsertStream(stream);
            highestRemoteStreamId = streamId;
        }

        stream.IsReset = true;
        stream.ResetByLocal = true;
        stream.ClosedByLocal = true;
        stream.HasErrorCode = true;
        stream.ErrorCode = errorCode;
        stream.ErrorCodeValue = (uint)errorCode;
        stream.IsKnownErrorCode = true;
        stream.IsRetryEligible =
            errorCode == Http2ErrorCode.RefusedStream && !stream.IsPushed;
        CloseStream(stream);
    }
}
