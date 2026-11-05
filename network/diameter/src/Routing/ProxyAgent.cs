using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace diameter;

internal class ProxyInformationInspection
{
    internal bool Valid;
    internal int Index = -1;
    internal DiameterIdentity ProxyHost = new DiameterIdentity();
    internal byte[] ProxyState = new byte[0];
    internal string Code = string.Empty;
}

internal class ProxyRequestProcessingResult
{
    internal bool Accepted;
    internal DiameterMessage? Message;
    internal DiameterConnectionMetadata? DestinationConnection;
    internal DiameterIdentity? DestinationPeer;
    internal RelayTransactionState? Transaction;
    internal DiameterRoutingPriority RoutingPriority =
        DiameterRoutingPriority.Priority10;
    internal bool RoutingPriorityPresent;
    internal string Code = string.Empty;
}

internal class ProxyAnswerProcessingResult
{
    internal bool Correlated;
    internal bool Forwarded;
    internal bool Discarded;
    internal DiameterMessage? Message;
    internal DiameterConnectionMetadata? DestinationConnection;
    internal DiameterIdentity? DestinationPeer;
    internal RelayTransactionState? Transaction;
    internal DiameterRoutingPriority RoutingPriority =
        DiameterRoutingPriority.Priority10;
    internal bool RoutingPriorityPresent;
    internal string Code = string.Empty;
}

internal class ProxyAgent
{
    internal const int ProxyStatePayloadLength = 49;
    internal const int ProxyStateTagLength = 32;
    internal const int ProxyStateLength = 81;
    internal const byte ProxyStateVersion = 1;
    internal object ProxyStateLock = new object();
    internal Dictionary<string, byte[]> ProtectedStates =
        new Dictionary<string, byte[]>();
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal byte[] StateProtectionKey;
    internal RelayAgent Transactions;
    internal DiameterBaseAvpFactory AvpFactory =
        new DiameterBaseAvpFactory();

    internal ProxyAgent(
        DiameterIdentity localHost,
        byte[] stateProtectionKey,
        IDiameterIdentifierSource identifierSource,
        int maximumTransactions)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A proxy agent requires a valid local host identity.");
        }

        if (stateProtectionKey == null || stateProtectionKey.Length < 16)
        {
            throw new ArgumentException(
                "A proxy state protection key requires at least 128 bits.");
        }

        LocalHost.Value = normalized.Text;
        StateProtectionKey = new byte[stateProtectionKey.Length];
        Array.Copy(
            stateProtectionKey,
            StateProtectionKey,
            stateProtectionKey.Length);
        Transactions = new RelayAgent(
            identifierSource,
            maximumTransactions);
    }

    internal ProxyRequestProcessingResult ForwardProxyRequest(
        DiameterRouteDecision? decision,
        DiameterConnectionMetadata? sourceConnection)
    {
        if (decision == null || decision.Request == null ||
            decision.Request.Message == null ||
            decision.RoutedMessage == null ||
            decision.SelectedCandidate == null ||
            !decision.Routed ||
            decision.Action != DiameterRouteAction.Proxy ||
            decision.AgentRole != DiameterAgentRole.Proxy ||
            decision.SelectedCandidate.Action != DiameterRouteAction.Proxy ||
            decision.SelectedCandidate.AgentRole != DiameterAgentRole.Proxy)
        {
            return CreateProxyRequestFailure("proxy-route-required");
        }

        if (sourceConnection == null ||
            !Transactions.ValidateRelayConnection(sourceConnection))
        {
            return CreateProxyRequestFailure("proxy-source-path-invalid");
        }

        byte[] state = CreateProtectedProxyState(
            decision.Request.Message,
            sourceConnection);
        DiameterProxyInformation information =
            new DiameterProxyInformation();
        information.ProxyHost.Value = LocalHost.Value;
        information.ProxyState = state;
        DiameterMessage routed = RouteTableCloner.CloneRouteMessage(
            decision.RoutedMessage);
        routed.Avps.Add(AvpFactory.CreateProxyInfoAvp(information));
        DiameterRouteDecision ownedDecision =
            CreateProxyRouteDecision(decision, routed);
        RelayRequestProcessingResult forwarded =
            Transactions.ForwardAgentRequest(
                ownedDecision,
                sourceConnection,
                DiameterRouteAction.Proxy,
                DiameterAgentRole.Proxy);
        if (!forwarded.Accepted || forwarded.Transaction == null ||
            forwarded.Message == null ||
            forwarded.DestinationConnection == null)
        {
            return CreateProxyRequestFailure(forwarded.Code);
        }

        string key = Transactions.CreateRelayTransactionKey(
            forwarded.DestinationConnection.ConnectionId,
            forwarded.Message.HopByHopIdentifier);
        lock (ProxyStateLock)
        {
            ProtectedStates[key] = CloneProxyState(state);
        }

        ProxyRequestProcessingResult result =
            new ProxyRequestProcessingResult();
        result.Accepted = true;
        result.Message = RouteTableCloner.CloneRouteMessage(
            forwarded.Message);
        result.DestinationConnection =
            Transactions.CloneRelayConnection(
                forwarded.DestinationConnection);
        if (forwarded.DestinationPeer != null)
        {
            result.DestinationPeer = Transactions.CloneRelayIdentity(
                forwarded.DestinationPeer);
        }

        result.Transaction = Transactions.CloneRelayTransaction(
            forwarded.Transaction);
        result.RoutingPriority = forwarded.RoutingPriority;
        result.RoutingPriorityPresent =
            forwarded.RoutingPriorityPresent;
        result.Code = "proxy-request-forwarded";
        return result;
    }

    internal ProxyAnswerProcessingResult ForwardProxyAnswer(
        DiameterConnectionMetadata? sourceConnection,
        DiameterMessage? answer)
    {
        string headerFailure = Transactions.ValidateRelayAnswerHeader(answer);
        if (headerFailure.Length > 0 || sourceConnection == null ||
            answer == null ||
            !Transactions.ValidateRelayConnection(sourceConnection))
        {
            string code = headerFailure.Length > 0 ?
                headerFailure : "proxy-answer-path-invalid";
            return CreateProxyAnswerFailure(code);
        }

        string key = Transactions.CreateRelayTransactionKey(
            sourceConnection.ConnectionId,
            answer.HopByHopIdentifier);
        byte[]? expectedState = null;
        lock (ProxyStateLock)
        {
            if (ProtectedStates.ContainsKey(key))
            {
                expectedState = CloneProxyState(ProtectedStates[key]);
            }
        }

        if (expectedState == null)
        {
            return CreateProxyAnswerFailure("proxy-answer-not-pending");
        }

        ProxyInformationInspection inspection =
            InspectLastProxyInformation(answer);
        if (!inspection.Valid)
        {
            return CreateProxyAnswerFailure(inspection.Code);
        }

        if (!CryptographicOperations.FixedTimeEquals(
                inspection.ProxyState,
                expectedState) ||
            !VerifyProtectedProxyState(inspection.ProxyState))
        {
            return CreateProxyAnswerFailure("proxy-state-invalid");
        }

        RelayAnswerProcessingResult forwarded =
            Transactions.ForwardRelayAnswer(sourceConnection, answer);
        if (!forwarded.Forwarded || forwarded.Message == null)
        {
            return CreateProxyAnswerFailure(forwarded.Code);
        }

        forwarded.Message.Avps.RemoveAt(inspection.Index);
        lock (ProxyStateLock)
        {
            ProtectedStates.Remove(key);
        }

        ProxyAnswerProcessingResult result =
            new ProxyAnswerProcessingResult();
        result.Correlated = true;
        result.Forwarded = true;
        result.Message = RouteTableCloner.CloneRouteMessage(
            forwarded.Message);
        if (forwarded.DestinationConnection != null)
        {
            result.DestinationConnection =
                Transactions.CloneRelayConnection(
                    forwarded.DestinationConnection);
        }

        if (forwarded.DestinationPeer != null)
        {
            result.DestinationPeer = Transactions.CloneRelayIdentity(
                forwarded.DestinationPeer);
        }

        if (forwarded.Transaction != null)
        {
            result.Transaction = Transactions.CloneRelayTransaction(
                forwarded.Transaction);
        }

        result.RoutingPriority = forwarded.RoutingPriority;
        result.RoutingPriorityPresent =
            forwarded.RoutingPriorityPresent;
        result.Code = "proxy-answer-forwarded";
        return result;
    }

    internal int ReadProxyTransactionCount()
    {
        lock (ProxyStateLock)
        {
            return ProtectedStates.Count;
        }
    }

    internal DiameterRouteDecision CreateProxyRouteDecision(
        DiameterRouteDecision source,
        DiameterMessage routed)
    {
        DiameterRouteDecision result = new DiameterRouteDecision();
        result.Request = RouteTableCloner.CloneRouteRequest(source.Request);
        result.Action = source.Action;
        result.AgentRole = source.AgentRole;
        result.RoutedMessage = RouteTableCloner.CloneRouteMessage(routed);
        result.Routed = true;
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        DiameterRouteCandidate? sourceCandidate = source.SelectedCandidate;
        if (sourceCandidate != null)
        {
            candidate.Action = sourceCandidate.Action;
            candidate.AgentRole = sourceCandidate.AgentRole;
            candidate.PeerIdentity = RouteTableCloner.CloneRouteIdentity(
                sourceCandidate.PeerIdentity);
            if (sourceCandidate.Peer != null)
            {
                candidate.Peer = PeerTableCloner.ClonePeerTableSnapshot(
                    sourceCandidate.Peer);
            }
        }

        result.SelectedCandidate = candidate;
        return result;
    }

    internal byte[] CreateProtectedProxyState(
        DiameterMessage request,
        DiameterConnectionMetadata sourceConnection)
    {
        byte[] payload = new byte[ProxyStatePayloadLength];
        payload[0] = ProxyStateVersion;
        WriteProxyStateUInt32(
            payload,
            1,
            request.HopByHopIdentifier);
        WriteProxyStateUInt32(
            payload,
            5,
            request.EndToEndIdentifier);
        WriteProxyStateUInt32(payload, 9, request.CommandCode);
        WriteProxyStateUInt32(payload, 13, request.ApplicationId);
        byte[] connectionHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(sourceConnection.ConnectionId));
        Array.Copy(connectionHash, 0, payload, 17, 16);
        byte[] nonce = new byte[16];
        RandomNumberGenerator.Fill(nonce);
        Array.Copy(nonce, 0, payload, 33, nonce.Length);
        byte[] tag = HMACSHA256.HashData(
            StateProtectionKey,
            payload);
        byte[] result = new byte[ProxyStateLength];
        Array.Copy(payload, 0, result, 0, payload.Length);
        Array.Copy(
            tag,
            0,
            result,
            ProxyStatePayloadLength,
            ProxyStateTagLength);
        return result;
    }

    internal bool VerifyProtectedProxyState(byte[] state)
    {
        if (state == null || state.Length != ProxyStateLength ||
            state[0] != ProxyStateVersion)
        {
            return false;
        }

        byte[] payload = new byte[ProxyStatePayloadLength];
        byte[] actualTag = new byte[ProxyStateTagLength];
        Array.Copy(state, 0, payload, 0, payload.Length);
        Array.Copy(
            state,
            ProxyStatePayloadLength,
            actualTag,
            0,
            actualTag.Length);
        byte[] expectedTag = HMACSHA256.HashData(
            StateProtectionKey,
            payload);
        return CryptographicOperations.FixedTimeEquals(
            actualTag,
            expectedTag);
    }

    internal ProxyInformationInspection InspectLastProxyInformation(
        DiameterMessage message)
    {
        ProxyInformationInspection result =
            new ProxyInformationInspection();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ProxyInfo)
            {
                result.Index = index;
            }

            index = index + 1;
        }

        if (result.Index < 0)
        {
            result.Code = "proxy-information-missing";
            return result;
        }

        DiameterAvp proxyInformation = message.Avps[result.Index];
        DiameterValueResult decoded =
            GroupedValueCodec.DecodeGroupedValue(
                proxyInformation.Data,
                null,
                new DiameterLimitConfiguration());
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            result.Code = "proxy-information-invalid";
            return result;
        }

        int hostCount = 0;
        int stateCount = 0;
        index = 0;
        while (index < decoded.Avps.Count)
        {
            DiameterAvp child = decoded.Avps[index];
            if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.ProxyHost)
            {
                hostCount = hostCount + 1;
                DiameterValueResult host =
                    ValueCodecs.DecodeDiameterIdentity(child.Data);
                if (host.Status != DiameterCodecStatus.Success ||
                    host.Identity == null)
                {
                    result.Code = "proxy-host-invalid";
                    return result;
                }

                result.ProxyHost.Value = host.Identity.Value;
            }
            else if (child.VendorId == DiameterVendorIds.Ietf &&
                child.Code == DiameterAvpCodes.ProxyState)
            {
                stateCount = stateCount + 1;
                result.ProxyState = CloneProxyState(child.Data);
            }

            index = index + 1;
        }

        if (hostCount != 1 || stateCount != 1)
        {
            result.Code = "proxy-information-invalid";
            return result;
        }

        if (!ValueCodecs.AreDiameterIdentitiesEqual(
            result.ProxyHost,
            LocalHost))
        {
            result.Code = "proxy-information-not-local";
            return result;
        }

        result.Valid = true;
        result.Code = "proxy-information-local";
        return result;
    }

    internal void WriteProxyStateUInt32(
        byte[] destination,
        int offset,
        uint value)
    {
        byte[] encoded = new byte[4];
        NetworkByteOrder.WriteUInt32(encoded, value);
        Array.Copy(encoded, 0, destination, offset, encoded.Length);
    }

    internal byte[] CloneProxyState(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal ProxyRequestProcessingResult CreateProxyRequestFailure(
        string code)
    {
        ProxyRequestProcessingResult result =
            new ProxyRequestProcessingResult();
        result.Code = code;
        return result;
    }

    internal ProxyAnswerProcessingResult CreateProxyAnswerFailure(
        string code)
    {
        ProxyAnswerProcessingResult result =
            new ProxyAnswerProcessingResult();
        result.Discarded = true;
        result.Code = code;
        return result;
    }
}
