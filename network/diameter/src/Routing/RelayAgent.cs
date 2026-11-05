using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class RelayTransactionState
{
    internal DiameterConnectionMetadata SourceConnection =
        new DiameterConnectionMetadata();
    internal DiameterConnectionMetadata DestinationConnection =
        new DiameterConnectionMetadata();
    internal DiameterIdentity SourcePeer = new DiameterIdentity();
    internal DiameterIdentity DestinationPeer = new DiameterIdentity();
    internal DiameterMessage OriginalRequest = new DiameterMessage();
    internal DiameterMessage ForwardedRequest = new DiameterMessage();
    internal uint SourceHopByHopIdentifier;
    internal uint DestinationHopByHopIdentifier;
    internal uint EndToEndIdentifier;
    internal uint CommandCode;
    internal uint ApplicationId;
    internal DiameterMessageFlags ProxyableFlag;
    internal DiameterRoutingPriority RoutingPriority =
        DiameterRoutingPriority.Priority10;
    internal bool RoutingPriorityPresent;
    internal DiameterRouteAction Action;
    internal DiameterAgentRole AgentRole;
}

internal class RelayRequestProcessingResult
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

internal class RelayAnswerProcessingResult
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

internal class RelayPathValidationResult
{
    internal bool Valid;
    internal DiameterIdentity SourcePeer = new DiameterIdentity();
    internal DiameterIdentity DestinationPeer = new DiameterIdentity();
    internal DiameterConnectionMetadata SourceConnection =
        new DiameterConnectionMetadata();
    internal DiameterConnectionMetadata DestinationConnection =
        new DiameterConnectionMetadata();
    internal string Code = string.Empty;
}

internal class RelayAgent
{
    internal object TransactionLock = new object();
    internal Dictionary<string, RelayTransactionState> TransactionsBySource =
        new Dictionary<string, RelayTransactionState>();
    internal Dictionary<string, RelayTransactionState> TransactionsByDestination =
        new Dictionary<string, RelayTransactionState>();
    internal IDiameterIdentifierSource IdentifierSource;
    internal ICreditControlPrivacyPolicy? CreditControlPrivacyPolicy;
    internal int MaximumTransactions;

    internal RelayAgent(
        IDiameterIdentifierSource identifierSource,
        int maximumTransactions)
    {
        if (identifierSource == null)
        {
            throw new ArgumentException(
                "A relay identifier source is required.");
        }

        if (maximumTransactions <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumTransactions");
        }

        IdentifierSource = identifierSource;
        MaximumTransactions = maximumTransactions;
    }

    internal RelayRequestProcessingResult ForwardRelayRequest(
        DiameterRouteDecision? decision,
        DiameterConnectionMetadata? sourceConnection)
    {
        return ForwardAgentRequest(
            decision,
            sourceConnection,
            DiameterRouteAction.Relay,
            DiameterAgentRole.Relay);
    }

    internal RelayRequestProcessingResult ForwardAgentRequest(
        DiameterRouteDecision? decision,
        DiameterConnectionMetadata? sourceConnection,
        DiameterRouteAction expectedAction,
        DiameterAgentRole expectedRole)
    {
        RelayPathValidationResult path = ValidateRelayPath(
            decision,
            sourceConnection,
            expectedAction,
            expectedRole);
        if (!path.Valid || decision == null || decision.RoutedMessage == null)
        {
            return CreateRelayRequestFailure(path.Code);
        }

        DiameterMessage original = decision.Request.Message;
        lock (TransactionLock)
        {
            if (TransactionsByDestination.Count >= MaximumTransactions)
            {
                return CreateRelayRequestFailure(
                    "relay-transaction-limit-reached");
            }

            string sourceKey = CreateRelayTransactionKey(
                path.SourceConnection.ConnectionId,
                original.HopByHopIdentifier);
            if (TransactionsBySource.ContainsKey(sourceKey))
            {
                return CreateRelayRequestFailure(
                    "relay-source-transaction-collision");
            }

            uint destinationHopByHopIdentifier =
                IdentifierSource.CreateHopByHopIdentifier();
            string destinationKey = CreateRelayTransactionKey(
                path.DestinationConnection.ConnectionId,
                destinationHopByHopIdentifier);
            if (destinationHopByHopIdentifier ==
                    original.HopByHopIdentifier ||
                TransactionsByDestination.ContainsKey(destinationKey))
            {
                return CreateRelayRequestFailure(
                    "relay-hop-by-hop-identifier-unavailable");
            }

            DiameterMessage forwarded = RouteTableCloner.CloneRouteMessage(
                decision.RoutedMessage);
            if (CreditControlPrivacyPolicy != null &&
                forwarded.ApplicationId ==
                    DiameterApplicationIds.CreditControl &&
                forwarded.CommandCode ==
                    DiameterCommandCodes.CreditControl)
            {
                forwarded = new DiameterCreditControlPrivacyProcessor()
                    .ApplyDiameterCreditControlPrivacyPolicy(
                        forwarded,
                        CreditControlPrivacyPolicy,
                        expectedRole);
            }

            forwarded.HopByHopIdentifier = destinationHopByHopIdentifier;
            forwarded.EndToEndIdentifier = original.EndToEndIdentifier;
            RelayTransactionState transaction =
                new RelayTransactionState();
            transaction.SourceConnection =
                CloneRelayConnection(path.SourceConnection);
            transaction.DestinationConnection =
                CloneRelayConnection(path.DestinationConnection);
            transaction.SourcePeer = CloneRelayIdentity(path.SourcePeer);
            transaction.DestinationPeer =
                CloneRelayIdentity(path.DestinationPeer);
            transaction.OriginalRequest =
                RouteTableCloner.CloneRouteMessage(original);
            transaction.ForwardedRequest =
                RouteTableCloner.CloneRouteMessage(forwarded);
            transaction.SourceHopByHopIdentifier =
                original.HopByHopIdentifier;
            transaction.DestinationHopByHopIdentifier =
                destinationHopByHopIdentifier;
            transaction.EndToEndIdentifier = original.EndToEndIdentifier;
            transaction.CommandCode = original.CommandCode;
            transaction.ApplicationId = original.ApplicationId;
            transaction.ProxyableFlag = original.Flags &
                DiameterMessageFlags.Proxyable;
            transaction.RoutingPriority =
                decision.Request.RoutingPriority;
            transaction.RoutingPriorityPresent =
                decision.Request.RoutingPriorityPresent;
            transaction.Action = expectedAction;
            transaction.AgentRole = expectedRole;
            TransactionsBySource[sourceKey] = transaction;
            TransactionsByDestination[destinationKey] = transaction;

            RelayRequestProcessingResult result =
                new RelayRequestProcessingResult();
            result.Accepted = true;
            result.Message = RouteTableCloner.CloneRouteMessage(forwarded);
            result.DestinationConnection =
                CloneRelayConnection(path.DestinationConnection);
            result.DestinationPeer = CloneRelayIdentity(path.DestinationPeer);
            result.Transaction = CloneRelayTransaction(transaction);
            result.RoutingPriority = transaction.RoutingPriority;
            result.RoutingPriorityPresent =
                transaction.RoutingPriorityPresent;
            result.Code = "relay-request-forwarded";
            return result;
        }
    }

    internal RelayAnswerProcessingResult ForwardRelayAnswer(
        DiameterConnectionMetadata? sourceConnection,
        DiameterMessage? answer)
    {
        string headerFailure = ValidateRelayAnswerHeader(answer);
        if (headerFailure.Length > 0 || answer == null ||
            sourceConnection == null ||
            !ValidateRelayConnection(sourceConnection))
        {
            string code = headerFailure.Length > 0 ?
                headerFailure : "relay-answer-path-invalid";
            return CreateRelayAnswerFailure(code);
        }

        DiameterMessage received = answer;
        string key = CreateRelayTransactionKey(
            sourceConnection.ConnectionId,
            received.HopByHopIdentifier);
        lock (TransactionLock)
        {
            if (!TransactionsByDestination.ContainsKey(key))
            {
                return DiagnoseUnknownRelayAnswer(
                    sourceConnection,
                    received);
            }

            RelayTransactionState transaction =
                TransactionsByDestination[key];
            if (!AreRelayConnectionPathsEqual(
                sourceConnection,
                transaction.DestinationConnection))
            {
                return CreateRelayAnswerFailure(
                    "relay-answer-path-mismatch");
            }

            string correlationFailure = ValidateRelayAnswerCorrelation(
                transaction,
                received);
            if (correlationFailure.Length > 0)
            {
                return CreateRelayAnswerFailure(correlationFailure);
            }

            DrmpInspectionResult answerPriority =
                DrmpProcessor.InspectAnswerPriority(
                    received,
                    transaction.RoutingPriority);
            if (!answerPriority.Valid)
            {
                return CreateRelayAnswerFailure(answerPriority.Code);
            }

            string sourceKey = CreateRelayTransactionKey(
                transaction.SourceConnection.ConnectionId,
                transaction.SourceHopByHopIdentifier);
            TransactionsByDestination.Remove(key);
            TransactionsBySource.Remove(sourceKey);
            DiameterMessage forwarded =
                RouteTableCloner.CloneRouteMessage(received);
            if (CreditControlPrivacyPolicy != null &&
                forwarded.ApplicationId ==
                    DiameterApplicationIds.CreditControl &&
                forwarded.CommandCode ==
                    DiameterCommandCodes.CreditControl)
            {
                forwarded = new DiameterCreditControlPrivacyProcessor()
                    .ApplyDiameterCreditControlPrivacyPolicy(
                        forwarded,
                        CreditControlPrivacyPolicy,
                        transaction.AgentRole);
            }

            forwarded.HopByHopIdentifier =
                transaction.SourceHopByHopIdentifier;
            forwarded.EndToEndIdentifier = transaction.EndToEndIdentifier;
            RelayAnswerProcessingResult result =
                new RelayAnswerProcessingResult();
            result.Correlated = true;
            result.Forwarded = true;
            result.Message = forwarded;
            result.DestinationConnection =
                CloneRelayConnection(transaction.SourceConnection);
            result.DestinationPeer =
                CloneRelayIdentity(transaction.SourcePeer);
            result.Transaction = CloneRelayTransaction(transaction);
            result.RoutingPriority = answerPriority.Priority;
            result.RoutingPriorityPresent = answerPriority.Present;
            result.Code = "relay-answer-forwarded";
            return result;
        }
    }

    internal int ReadRelayTransactionCount()
    {
        lock (TransactionLock)
        {
            return TransactionsByDestination.Count;
        }
    }

    internal RelayPathValidationResult ValidateRelayPath(
        DiameterRouteDecision? decision,
        DiameterConnectionMetadata? sourceConnection,
        DiameterRouteAction expectedAction,
        DiameterAgentRole expectedRole)
    {
        RelayPathValidationResult result =
            new RelayPathValidationResult();
        if (decision == null || decision.Request == null ||
            decision.Request.Message == null ||
            decision.Request.SourcePeer == null ||
            decision.RoutedMessage == null ||
            decision.SelectedCandidate == null ||
            decision.SelectedCandidate.PeerIdentity == null ||
            decision.SelectedCandidate.Peer == null ||
            decision.SelectedCandidate.Peer.Connection == null ||
            sourceConnection == null)
        {
            result.Code = "relay-path-required";
            return result;
        }

        if (!decision.Routed ||
            decision.Action != expectedAction ||
            decision.AgentRole != expectedRole ||
            decision.SelectedCandidate.Action != expectedAction ||
            decision.SelectedCandidate.AgentRole != expectedRole ||
            ((expectedAction != DiameterRouteAction.Relay ||
            expectedRole != DiameterAgentRole.Relay) &&
            (expectedAction != DiameterRouteAction.Proxy ||
            expectedRole != DiameterAgentRole.Proxy)))
        {
            result.Code = "relay-route-required";
            return result;
        }

        DiameterValueResult sourcePeer =
            ValueCodecs.NormalizeDiameterIdentity(
                decision.Request.SourcePeer);
        DiameterValueResult destinationPeer =
            ValueCodecs.NormalizeDiameterIdentity(
                decision.SelectedCandidate.PeerIdentity);
        DiameterValueResult configuredDestinationPeer =
            ValueCodecs.NormalizeDiameterIdentity(
                decision.SelectedCandidate.Peer.Configuration.Identity);
        if (sourcePeer.Status != DiameterCodecStatus.Success ||
            destinationPeer.Status != DiameterCodecStatus.Success ||
            configuredDestinationPeer.Status != DiameterCodecStatus.Success ||
            configuredDestinationPeer.Text != destinationPeer.Text)
        {
            result.Code = "relay-peer-identity-invalid";
            return result;
        }

        DiameterConnectionMetadata destinationConnection =
            decision.SelectedCandidate.Peer.Connection;
        if (!ValidateRelayConnection(sourceConnection) ||
            !ValidateRelayConnection(destinationConnection) ||
            !DoesRelayConnectionMatchPeer(
                sourceConnection,
                sourcePeer.Text) ||
            !DoesRelayConnectionMatchPeer(
                destinationConnection,
                destinationPeer.Text))
        {
            result.Code = "relay-connection-path-invalid";
            return result;
        }

        if (sourcePeer.Text == destinationPeer.Text ||
            sourceConnection.ConnectionId ==
                destinationConnection.ConnectionId)
        {
            result.Code = "relay-connection-path-loop";
            return result;
        }

        string messageFailure = ValidateRelayRequestMessages(
            decision.Request.Message,
            decision.RoutedMessage);
        if (messageFailure.Length > 0)
        {
            result.Code = messageFailure;
            return result;
        }

        result.Valid = true;
        result.SourcePeer.Value = sourcePeer.Text;
        result.DestinationPeer.Value = destinationPeer.Text;
        result.SourceConnection = CloneRelayConnection(sourceConnection);
        result.DestinationConnection =
            CloneRelayConnection(destinationConnection);
        result.Code = "relay-path-valid";
        return result;
    }

    internal string ValidateRelayRequestMessages(
        DiameterMessage original,
        DiameterMessage routed)
    {
        if (!ValidateRelayMessageStorage(original) ||
            !ValidateRelayMessageStorage(routed))
        {
            return "relay-request-storage-invalid";
        }

        int flags = (int)original.Flags;
        if (original.Version != DiameterProtocolConstants.Version ||
            original.CommandCode == 0U ||
            original.CommandCode >
                DiameterProtocolConstants.MaximumCommandCode ||
            (original.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (original.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            flags < 0 || flags > 255 ||
            (flags & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            return "relay-request-header-invalid";
        }

        if (routed.Version != original.Version ||
            routed.Flags != original.Flags ||
            routed.CommandCode != original.CommandCode ||
            routed.ApplicationId != original.ApplicationId ||
            routed.HopByHopIdentifier != original.HopByHopIdentifier ||
            routed.EndToEndIdentifier != original.EndToEndIdentifier)
        {
            return "relay-routed-request-correlation-invalid";
        }

        return string.Empty;
    }

    internal string ValidateRelayAnswerHeader(DiameterMessage? answer)
    {
        if (answer == null)
        {
            return "relay-answer-required";
        }

        if (!ValidateRelayMessageStorage(answer))
        {
            return "relay-answer-storage-invalid";
        }

        int flags = (int)answer.Flags;
        if (answer.Version != DiameterProtocolConstants.Version ||
            answer.CommandCode == 0U ||
            answer.CommandCode > DiameterProtocolConstants.MaximumCommandCode ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            (answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None ||
            flags < 0 || flags > 255 ||
            (flags & DiameterProtocolConstants.MessageReservedFlagMask) != 0)
        {
            return "relay-answer-header-invalid";
        }

        return string.Empty;
    }

    internal string ValidateRelayAnswerCorrelation(
        RelayTransactionState transaction,
        DiameterMessage answer)
    {
        if (answer.CommandCode != transaction.CommandCode)
        {
            return "relay-answer-command-code-mismatch";
        }

        if (answer.ApplicationId != transaction.ApplicationId)
        {
            return "relay-answer-application-id-mismatch";
        }

        if (answer.HopByHopIdentifier !=
            transaction.DestinationHopByHopIdentifier)
        {
            return "relay-answer-hop-by-hop-mismatch";
        }

        if (answer.EndToEndIdentifier != transaction.EndToEndIdentifier)
        {
            return "relay-answer-end-to-end-mismatch";
        }

        DiameterMessageFlags answerProxyable = answer.Flags &
            DiameterMessageFlags.Proxyable;
        if (answerProxyable != transaction.ProxyableFlag)
        {
            return "relay-answer-proxyable-bit-mismatch";
        }

        return string.Empty;
    }

    internal RelayAnswerProcessingResult DiagnoseUnknownRelayAnswer(
        DiameterConnectionMetadata sourceConnection,
        DiameterMessage answer)
    {
        bool hopMismatch = false;
        bool pathMismatch = false;
        foreach (RelayTransactionState transaction in
            TransactionsByDestination.Values)
        {
            bool sameCommand = transaction.CommandCode == answer.CommandCode;
            bool sameApplication =
                transaction.ApplicationId == answer.ApplicationId;
            bool sameEndToEnd =
                transaction.EndToEndIdentifier == answer.EndToEndIdentifier;
            bool sameConnection = transaction.DestinationConnection.ConnectionId ==
                sourceConnection.ConnectionId;
            if (sameConnection && sameCommand && sameApplication &&
                sameEndToEnd && transaction.DestinationHopByHopIdentifier !=
                    answer.HopByHopIdentifier)
            {
                hopMismatch = true;
            }

            if (!sameConnection && sameCommand && sameApplication &&
                sameEndToEnd && transaction.DestinationHopByHopIdentifier ==
                    answer.HopByHopIdentifier)
            {
                pathMismatch = true;
            }
        }

        if (pathMismatch)
        {
            return CreateRelayAnswerFailure(
                "relay-answer-path-mismatch");
        }

        if (hopMismatch)
        {
            return CreateRelayAnswerFailure(
                "relay-answer-hop-by-hop-mismatch");
        }

        return CreateRelayAnswerFailure("relay-answer-not-pending");
    }

    internal bool ValidateRelayConnection(
        DiameterConnectionMetadata connection)
    {
        return connection != null &&
            !string.IsNullOrWhiteSpace(connection.ConnectionId) &&
            connection.LocalEndpoint != null &&
            connection.LocalEndpoint.Identity != null &&
            connection.RemoteEndpoint != null &&
            connection.RemoteEndpoint.Identity != null &&
            connection.Transport != null &&
            connection.Security != null &&
            connection.Security.PeerCertificateDnsNames != null &&
            connection.Open;
    }

    internal bool ValidateRelayMessageStorage(DiameterMessage message)
    {
        if (message == null || message.Avps == null)
        {
            return false;
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool DoesRelayConnectionMatchPeer(
        DiameterConnectionMetadata connection,
        string normalizedPeerIdentity)
    {
        if (connection.RemoteEndpoint.Identity == null ||
            connection.RemoteEndpoint.Identity.Value == null ||
            connection.RemoteEndpoint.Identity.Value.Length == 0)
        {
            return true;
        }

        DiameterValueResult identity =
            ValueCodecs.NormalizeDiameterIdentity(
                connection.RemoteEndpoint.Identity);
        return identity.Status == DiameterCodecStatus.Success &&
            identity.Text == normalizedPeerIdentity;
    }

    internal bool AreRelayConnectionPathsEqual(
        DiameterConnectionMetadata first,
        DiameterConnectionMetadata second)
    {
        return first.ConnectionId == second.ConnectionId &&
            first.Direction == second.Direction &&
            first.LocalEndpoint.Identity.Value ==
                second.LocalEndpoint.Identity.Value &&
            first.LocalEndpoint.Host == second.LocalEndpoint.Host &&
            object.Equals(
                first.LocalEndpoint.Address,
                second.LocalEndpoint.Address) &&
            first.LocalEndpoint.Port == second.LocalEndpoint.Port &&
            first.LocalEndpoint.Transport == second.LocalEndpoint.Transport &&
            first.LocalEndpoint.Security == second.LocalEndpoint.Security &&
            first.RemoteEndpoint.Identity.Value ==
                second.RemoteEndpoint.Identity.Value &&
            first.RemoteEndpoint.Host == second.RemoteEndpoint.Host &&
            object.Equals(
                first.RemoteEndpoint.Address,
                second.RemoteEndpoint.Address) &&
            first.RemoteEndpoint.Port == second.RemoteEndpoint.Port &&
            first.RemoteEndpoint.Transport == second.RemoteEndpoint.Transport &&
            first.RemoteEndpoint.Security == second.RemoteEndpoint.Security &&
            first.Transport.Kind == second.Transport.Kind &&
            first.InboundStream == second.InboundStream &&
            first.OutboundStream == second.OutboundStream;
    }

    internal string CreateRelayTransactionKey(
        string connectionId,
        uint hopByHopIdentifier)
    {
        return connectionId + "|" +
            hopByHopIdentifier.ToString(CultureInfo.InvariantCulture);
    }

    internal RelayRequestProcessingResult CreateRelayRequestFailure(
        string code)
    {
        RelayRequestProcessingResult result =
            new RelayRequestProcessingResult();
        result.Code = code;
        return result;
    }

    internal RelayAnswerProcessingResult CreateRelayAnswerFailure(
        string code)
    {
        RelayAnswerProcessingResult result =
            new RelayAnswerProcessingResult();
        result.Discarded = true;
        result.Code = code;
        return result;
    }

    internal DiameterConnectionMetadata CloneRelayConnection(
        DiameterConnectionMetadata source)
    {
        return QueuedDiameterConnection.CloneConnectionMetadata(source);
    }

    internal DiameterIdentity CloneRelayIdentity(DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal RelayTransactionState CloneRelayTransaction(
        RelayTransactionState source)
    {
        RelayTransactionState result = new RelayTransactionState();
        result.SourceConnection =
            CloneRelayConnection(source.SourceConnection);
        result.DestinationConnection =
            CloneRelayConnection(source.DestinationConnection);
        result.SourcePeer = CloneRelayIdentity(source.SourcePeer);
        result.DestinationPeer = CloneRelayIdentity(source.DestinationPeer);
        result.OriginalRequest =
            RouteTableCloner.CloneRouteMessage(source.OriginalRequest);
        result.ForwardedRequest =
            RouteTableCloner.CloneRouteMessage(source.ForwardedRequest);
        result.SourceHopByHopIdentifier =
            source.SourceHopByHopIdentifier;
        result.DestinationHopByHopIdentifier =
            source.DestinationHopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.ProxyableFlag = source.ProxyableFlag;
        result.RoutingPriority = source.RoutingPriority;
        result.RoutingPriorityPresent = source.RoutingPriorityPresent;
        result.Action = source.Action;
        result.AgentRole = source.AgentRole;
        return result;
    }
}
