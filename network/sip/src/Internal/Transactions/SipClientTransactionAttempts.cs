namespace sip;

internal class SipClientTransactionAttempts
{
    SipMessage requestTemplate;
    SipServerCandidate[] candidates;
    SipEndpointAddress sentBy;
    SipProtocolTokenGenerator tokens;
    bool requestRPort;
    bool started;
    bool responseReceived;
    int candidateIndex = -1;
    SipMessage? currentRequest;
    SipTransactionKey? currentKey;
    string? currentBranch;
    string? connectionId;
    string? flowId;

    internal SipClientTransactionAttempts(SipMessage? request,
        SipServerCandidate[]? serverCandidates, SipEndpointAddress? localSentBy,
        bool includeRPort, ISipRandomSource? randomSource)
    {
        if (request == null || request.Kind != SipMessageKind.Request)
        {
            throw new System.ArgumentException("A SIP request is required.", "request");
        }
        SipMessage? copiedRequest = SipSnapshotCopy.CopyMessageModel(request);
        if (copiedRequest == null)
        {
            throw new System.ArgumentException("The SIP request cannot be copied.", "request");
        }
        if (serverCandidates == null || serverCandidates.Length == 0
            || serverCandidates.Length > SipDefaults.MaxDnsRecordCount)
        {
            throw new System.ArgumentException(
                "At least one bounded server candidate is required.", "serverCandidates");
        }
        if (localSentBy == null || localSentBy.Host == null
            || localSentBy.Host.Length == 0
            || localSentBy.Host.Length > SipDefaults.MaxHeaderLineBytes
            || localSentBy.Port < -1 || localSentBy.Port == 0
            || localSentBy.Port > 65535)
        {
            throw new System.ArgumentException(
                "A valid local sent-by endpoint is required.", "localSentBy");
        }
        if (randomSource == null)
        {
            throw new System.ArgumentNullException("randomSource");
        }

        requestTemplate = copiedRequest;
        candidates = CopyCandidates(serverCandidates);
        sentBy = CopyEndpoint(localSentBy);
        tokens = new SipProtocolTokenGenerator(randomSource);
        requestRPort = includeRPort;
    }

    internal SipClientTransactionAttemptResult StartAttempt()
    {
        if (started)
        {
            throw new System.InvalidOperationException(
                "The client transaction attempt sequence has already started.");
        }

        started = true;
        candidateIndex = 0;
        return CreateAttempt(false);
    }

    internal SipClientTransactionAttemptResult RetryAfterTransportFailure(
        string? errorMessage)
    {
        RequireStarted();
        if (errorMessage == null || errorMessage.Length == 0
            || errorMessage.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException(
                "A bounded transport failure message is required.", "errorMessage");
        }

        if (responseReceived)
        {
            SipClientTransactionAttemptResult suppressed = CreateStatusResult();
            suppressed.Suppressed = true;
            suppressed.ErrorMessage = errorMessage;
            return suppressed;
        }
        if (candidateIndex + 1 >= candidates.Length)
        {
            SipClientTransactionAttemptResult exhausted = CreateStatusResult();
            exhausted.Exhausted = true;
            exhausted.ErrorMessage = errorMessage;
            return exhausted;
        }

        candidateIndex = candidateIndex + 1;
        connectionId = null;
        flowId = null;
        return CreateAttempt(true);
    }

    internal SipSendRequest CreateAffineSend(SipMessage? message)
    {
        RequireStarted();
        if (message == null || message.Kind != SipMessageKind.Request
            || currentBranch == null
            || !SipViaRouting.TopViaMatches(message, sentBy.Host,
                sentBy.Port, currentBranch))
        {
            throw new System.ArgumentException(
                "A request belonging to the current transaction attempt is required.", "message");
        }

        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null)
        {
            throw new System.ArgumentException("The SIP request cannot be copied.", "message");
        }
        return CreateSendRequest(copy);
    }

    internal bool CaptureSendAffinity(SipSendResult? result)
    {
        RequireStarted();
        if (result == null || !result.Success)
        {
            return false;
        }

        SipConnectionInfo? connection = result.Connection;
        SipFlow? flow = result.Flow;
        if (!ConnectionMatchesCurrentTransport(connection)
            || !FlowMatchesCurrentTransport(flow))
        {
            return false;
        }
        bool captured = false;
        if (connection != null && connection.Id != null && connection.Id.Length > 0)
        {
            connectionId = connection.Id;
            captured = true;
        }
        if (flow != null && flow.Id != null && flow.Id.Length > 0)
        {
            flowId = flow.Id;
            captured = true;
        }
        if (connectionId == null && flow != null && flow.Connection != null
            && flow.Connection.Id != null && flow.Connection.Id.Length > 0)
        {
            connectionId = flow.Connection.Id;
            captured = true;
        }
        return captured;
    }

    internal bool ObserveResponse(SipTransportMessage? response)
    {
        RequireStarted();
        if (response == null || response.Message == null
            || response.Message.Kind != SipMessageKind.Response
            || currentBranch == null || currentKey == null
            || !SipTransactionMatching.MatchResponse(
                currentKey, response.Message, false)
            || response.Transport != SipTransportKind.Unknown
                && response.Transport != candidates[candidateIndex].Transport)
        {
            return false;
        }

        responseReceived = true;
        CaptureResponseAffinity(response);
        return true;
    }

    internal bool HasEstablishedTransaction()
    {
        return responseReceived;
    }

    internal int GetCandidateIndex()
    {
        return candidateIndex;
    }

    internal string? GetCurrentBranch()
    {
        return currentBranch;
    }

    SipClientTransactionAttemptResult CreateAttempt(bool retried)
    {
        SipServerCandidate candidate = candidates[candidateIndex];
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(requestTemplate);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The client transaction request template cannot be copied.");
        }

        currentBranch = tokens.CreateBranch();
        SipEndpointAddress attemptSentBy = CopyEndpoint(sentBy);
        attemptSentBy.Transport = candidate.Transport;
        attemptSentBy.Secure = candidate.Secure;
        SipViaRouting.ReplaceRequestVia(copy, attemptSentBy,
            currentBranch, requestRPort);
        SipValidationIssue[] issues = SipMessageValidator.ValidateMessageModel(copy, true);
        if (SipMessageValidator.HasErrorIssues(issues))
        {
            throw new System.ArgumentException(
                "The request is invalid after creating its transaction Via.", "request");
        }

        currentRequest = copy;
        currentKey = SipTransactionMatching.CreateKey(
            currentRequest, SipTransactionRole.Client);
        SipClientTransactionAttemptResult result = CreateStatusResult();
        result.Created = true;
        result.Retried = retried;
        result.SendRequest = CreateSendRequest(copy);
        return result;
    }

    SipClientTransactionAttemptResult CreateStatusResult()
    {
        SipClientTransactionAttemptResult result = new SipClientTransactionAttemptResult();
        result.CandidateIndex = candidateIndex;
        if (candidateIndex >= 0 && candidateIndex < candidates.Length)
        {
            result.Candidate = CopyCandidate(candidates[candidateIndex]);
        }
        result.Branch = currentBranch;
        return result;
    }

    SipSendRequest CreateSendRequest(SipMessage message)
    {
        SipServerCandidate candidate = candidates[candidateIndex];
        SipSendRequest send = new SipSendRequest();
        send.Message = SipSnapshotCopy.CopyMessageModel(message);
        send.RemoteAddress = CreateRemoteEndpoint(candidate);
        send.LocalAddress = CopyEndpoint(sentBy);
        send.LocalAddress.Transport = candidate.Transport;
        send.LocalAddress.Secure = candidate.Secure;
        send.Transport = candidate.Transport;
        send.ConnectionId = connectionId;
        send.FlowId = flowId;
        return send;
    }

    void CaptureResponseAffinity(SipTransportMessage response)
    {
        if (ConnectionMatchesCurrentTransport(response.Connection)
            && response.Connection != null && response.Connection.Id != null
            && response.Connection.Id.Length > 0)
        {
            connectionId = response.Connection.Id;
        }
        if (FlowMatchesCurrentTransport(response.Flow)
            && response.Flow != null && response.Flow.Id != null
            && response.Flow.Id.Length > 0)
        {
            flowId = response.Flow.Id;
            if (connectionId == null && response.Flow != null
                && response.Flow.Connection != null)
            {
                connectionId = response.Flow.Connection.Id;
            }
        }
    }

    bool ConnectionMatchesCurrentTransport(SipConnectionInfo? connection)
    {
        return connection == null
            || connection.Transport == SipTransportKind.Unknown
            || connection.Transport == candidates[candidateIndex].Transport;
    }

    bool FlowMatchesCurrentTransport(SipFlow? flow)
    {
        return flow == null
            || flow.Connection == null
            || flow.Connection.Transport == SipTransportKind.Unknown
            || flow.Connection.Transport == candidates[candidateIndex].Transport;
    }

    void RequireStarted()
    {
        if (!started || currentRequest == null)
        {
            throw new System.InvalidOperationException(
                "The client transaction attempt sequence has not started.");
        }
    }

    static SipServerCandidate[] CopyCandidates(SipServerCandidate[] source)
    {
        SipServerCandidate[] result = new SipServerCandidate[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipServerCandidate? candidate = source[index];
            if (!CandidateIsValid(candidate))
            {
                throw new System.ArgumentException(
                    "Every server candidate must identify a valid SIP transport endpoint.",
                    "serverCandidates");
            }
            result[index] = CopyCandidate(candidate);
            index = index + 1;
        }
        return result;
    }

    static bool CandidateIsValid(SipServerCandidate? candidate)
    {
        if (candidate == null || candidate.Host == null
            || candidate.Host.Length == 0
            || candidate.Host.Length > SipDefaults.MaxHeaderLineBytes
            || candidate.Port <= 0 || candidate.Port > 65535
            || candidate.Transport == SipTransportKind.Unknown
            || candidate.Transport == SipTransportKind.Custom)
        {
            return false;
        }
        if (candidate.Secure)
        {
            return candidate.Transport == SipTransportKind.Tls
                || candidate.Transport == SipTransportKind.SecureWebSocket
                || candidate.Transport == SipTransportKind.Dtls;
        }
        return true;
    }

    static SipServerCandidate CopyCandidate(SipServerCandidate source)
    {
        SipServerCandidate copy = new SipServerCandidate();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Priority = source.Priority;
        copy.Weight = source.Weight;
        copy.RecordKind = source.RecordKind;
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.Secure = source.Secure;
        return copy;
    }

    static SipEndpointAddress CreateRemoteEndpoint(SipServerCandidate candidate)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = candidate.Host;
        endpoint.Port = candidate.Port;
        endpoint.Transport = candidate.Transport;
        endpoint.Secure = candidate.Secure;
        return endpoint;
    }

    static SipEndpointAddress CopyEndpoint(SipEndpointAddress source)
    {
        SipEndpointAddress copy = new SipEndpointAddress();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Transport = source.Transport;
        copy.Secure = source.Secure;
        return copy;
    }
}
