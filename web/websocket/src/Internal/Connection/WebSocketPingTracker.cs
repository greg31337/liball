using System;

namespace websocket;

internal class WebSocketTrackedPing
{
    internal byte[] Token = [];
    internal long SentElapsedMilliseconds;
    internal WebSocketTrackedPing? Next;
}

internal class WebSocketPingTrackingResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal bool Matched;
    internal int OutstandingCount;
    internal bool HasAcknowledgementEvent;
    internal WebSocketEvent AcknowledgementEvent = new WebSocketEvent();
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketPingTracker
{
    int MaximumOutstandingCount;
    int OutstandingCount;
    WebSocketTrackedPing? Head;
    WebSocketTrackedPing? Tail;

    internal WebSocketPingTracker(int maximumOutstandingCount)
    {
        if (maximumOutstandingCount <= 0)
        {
            throw new WebSocketLimitException();
        }

        MaximumOutstandingCount = maximumOutstandingCount;
    }

    internal WebSocketPingTrackingResult TrackPingToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount,
        long sentElapsedMilliseconds)
    {
        WebSocketPingTrackingResult validation = ValidateToken(
            token,
            tokenOffset,
            tokenCount);
        if (validation.HasError)
        {
            return validation;
        }

        if (sentElapsedMilliseconds < 0)
        {
            return CreateStateFailure(
                "ping-sent-time-invalid",
                "The tracked Ping send time cannot be negative.");
        }

        if (token == null)
        {
            return CreateStateFailure(
                "ping-token-invalid",
                "The tracked Ping token is required.");
        }

        if (FindToken(token, tokenOffset, tokenCount) != null)
        {
            return CreateStateFailure(
                "ping-token-duplicate",
                "The Ping token is already outstanding.");
        }

        if (OutstandingCount == MaximumOutstandingCount)
        {
            return CreateLimitFailure(
                "ping-tracking-capacity-exceeded",
                "The maximum number of outstanding Ping tokens is already tracked.");
        }

        WebSocketTrackedPing tracked = new WebSocketTrackedPing();
        tracked.Token = CopyRange(token, tokenOffset, tokenCount);
        tracked.SentElapsedMilliseconds = sentElapsedMilliseconds;
        if (Tail == null)
        {
            Head = tracked;
        }
        else
        {
            Tail.Next = tracked;
        }

        Tail = tracked;
        OutstandingCount = OutstandingCount + 1;
        WebSocketPingTrackingResult result =
            new WebSocketPingTrackingResult();
        result.OutstandingCount = OutstandingCount;
        return result;
    }

    internal WebSocketPingTrackingResult MatchPongToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount,
        long receivedElapsedMilliseconds)
    {
        WebSocketPingTrackingResult validation = ValidateToken(
            token,
            tokenOffset,
            tokenCount);
        if (validation.HasError)
        {
            return validation;
        }

        if (receivedElapsedMilliseconds < 0)
        {
            return CreateStateFailure(
                "pong-received-time-invalid",
                "The Pong receive time cannot be negative.");
        }

        if (token == null)
        {
            return CreateStateFailure(
                "ping-token-invalid",
                "The tracked Ping token is required.");
        }

        WebSocketTrackedPing? prior = null;
        WebSocketTrackedPing? tracked = Head;
        while (tracked != null &&
            !TokensEqual(tracked.Token, token, tokenOffset, tokenCount))
        {
            prior = tracked;
            tracked = tracked.Next;
        }

        if (tracked == null)
        {
            WebSocketPingTrackingResult unmatched =
                new WebSocketPingTrackingResult();
            unmatched.OutstandingCount = OutstandingCount;
            return unmatched;
        }

        if (receivedElapsedMilliseconds < tracked.SentElapsedMilliseconds)
        {
            return CreateStateFailure(
                "ping-elapsed-time-regressed",
                "The Pong receive time precedes its Ping send time.");
        }

        RemoveTrackedPing(prior, tracked);
        WebSocketPingTrackingResult matched =
            new WebSocketPingTrackingResult();
        matched.Matched = true;
        matched.OutstandingCount = OutstandingCount;
        matched.HasAcknowledgementEvent = true;
        matched.AcknowledgementEvent = CreateAcknowledgementEvent(
            tracked.Token,
            receivedElapsedMilliseconds -
                tracked.SentElapsedMilliseconds);
        Array.Clear(tracked.Token);
        tracked.Next = null;
        return matched;
    }

    internal bool RemovePingToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        if (!TokenRangeValid(token, tokenOffset, tokenCount) || token == null)
        {
            return false;
        }

        WebSocketTrackedPing? prior = null;
        WebSocketTrackedPing? tracked = Head;
        while (tracked != null &&
            !TokensEqual(tracked.Token, token, tokenOffset, tokenCount))
        {
            prior = tracked;
            tracked = tracked.Next;
        }

        if (tracked == null)
        {
            return false;
        }

        RemoveTrackedPing(prior, tracked);
        Array.Clear(tracked.Token);
        tracked.Next = null;
        return true;
    }

    internal bool ContainsPingToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        if (!TokenRangeValid(token, tokenOffset, tokenCount) || token == null)
        {
            return false;
        }

        return FindToken(token, tokenOffset, tokenCount) != null;
    }

    internal int GetOutstandingPingCount()
    {
        return OutstandingCount;
    }

    internal int GetOutstandingPingCapacity()
    {
        return MaximumOutstandingCount;
    }

    internal void ClearOutstandingPings()
    {
        WebSocketTrackedPing? tracked = Head;
        while (tracked != null)
        {
            WebSocketTrackedPing? next = tracked.Next;
            Array.Clear(tracked.Token);
            tracked.Next = null;
            tracked = next;
        }

        Head = null;
        Tail = null;
        OutstandingCount = 0;
    }

    void RemoveTrackedPing(
        WebSocketTrackedPing? prior,
        WebSocketTrackedPing tracked)
    {
        if (prior == null)
        {
            Head = tracked.Next;
        }
        else
        {
            prior.Next = tracked.Next;
        }

        if (object.ReferenceEquals(Tail, tracked))
        {
            Tail = prior;
        }

        OutstandingCount = OutstandingCount - 1;
    }

    WebSocketTrackedPing? FindToken(
        byte[] token,
        int tokenOffset,
        int tokenCount)
    {
        WebSocketTrackedPing? tracked = Head;
        while (tracked != null)
        {
            if (TokensEqual(
                tracked.Token,
                token,
                tokenOffset,
                tokenCount))
            {
                return tracked;
            }

            tracked = tracked.Next;
        }

        return null;
    }

    static WebSocketPingTrackingResult ValidateToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        if (!TokenRangeValid(token, tokenOffset, tokenCount))
        {
            return CreateStateFailure(
                "ping-token-range-invalid",
                "The Ping token range is invalid.");
        }

        if (tokenCount >
            WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            return CreateLimitFailure(
                "ping-token-limit-exceeded",
                "The Ping token exceeds 125 bytes.");
        }

        return new WebSocketPingTrackingResult();
    }

    static bool TokenRangeValid(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        return token != null &&
            tokenOffset >= 0 &&
            tokenOffset <= token.Length &&
            tokenCount >= 0 &&
            tokenCount <= token.Length - tokenOffset;
    }

    static bool TokensEqual(
        byte[] tracked,
        byte[] candidate,
        int candidateOffset,
        int candidateCount)
    {
        if (tracked.Length != candidateCount)
        {
            return false;
        }

        int index = 0;
        while (index < tracked.Length)
        {
            if (tracked[index] != candidate[candidateOffset + index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte[] CopyRange(
        byte[] source,
        int sourceOffset,
        int sourceCount)
    {
        byte[] copy = new byte[sourceCount];
        source.AsSpan(sourceOffset, sourceCount).CopyTo(copy);
        return copy;
    }

    static WebSocketEvent CreateAcknowledgementEvent(
        byte[] token,
        long elapsedMilliseconds)
    {
        WebSocketEvent connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.PingAcknowledged;
        connectionEvent.Payload = new byte[token.Length];
        token.AsSpan().CopyTo(connectionEvent.Payload);
        connectionEvent.PayloadOffset = 0;
        connectionEvent.PayloadCount = token.Length;
        connectionEvent.First = true;
        connectionEvent.Final = true;
        connectionEvent.HasPingElapsedMilliseconds = true;
        connectionEvent.PingElapsedMilliseconds = elapsedMilliseconds;
        return connectionEvent;
    }

    static WebSocketPingTrackingResult CreateStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailure(WebSocketParseStatus.ProtocolFailure, error);
    }

    static WebSocketPingTrackingResult CreateLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailure(WebSocketParseStatus.LimitRejected, error);
    }

    static WebSocketPingTrackingResult CreateFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        WebSocketPingTrackingResult result =
            new WebSocketPingTrackingResult();
        result.Status = status;
        result.Accepted = false;
        result.HasError = true;
        result.Error = error;
        return result;
    }
}
