using System;

namespace diameter;

internal class RequestFailoverPolicy
{
    internal bool ApplicationAllowsFailover = true;
    internal bool SessionAllowsFailover = true;
    internal bool DeliveryUncertain = true;
    internal bool RemoveDestinationHost;
    internal DiameterMessage? RoutedMessage;
}

internal class RequestFailoverDestination
{
    internal bool Valid;
    internal bool Present;
    internal bool MatchesCurrentPeer;
    internal string Code = string.Empty;
}

internal class RequestFailoverResult
{
    internal bool Eligible;
    internal bool Retried;
    internal bool AlternateRoute;
    internal DiameterPendingRequestSnapshot? PendingRequest;
    internal DiameterMessage? Message;
    internal DiameterIdentity? PeerIdentity;
    internal string Code = string.Empty;
}

internal class RequestFailoverController
{
    internal PendingRequestTable Table;
    internal IDiameterIdentifierSource IdentifierSource;

    internal RequestFailoverController(
        PendingRequestTable table,
        IDiameterIdentifierSource identifierSource)
    {
        if (table == null || identifierSource == null)
        {
            throw new ArgumentException(
                "A pending-request table and identifier source are required.");
        }

        Table = table;
        IdentifierSource = identifierSource;
    }

    internal RequestFailoverResult FailoverPendingRequest(
        long requestIdentifier,
        DiameterIdentity? alternatePeer,
        RequestFailoverPolicy? policy)
    {
        PendingRequestTableResult lookup =
            Table.ReadPendingRequestByIdentifier(requestIdentifier);
        if (!lookup.Found || lookup.PendingRequest == null)
        {
            return CreateRequestFailoverFailure(
                "pending-request-not-found");
        }

        DiameterPendingRequestSnapshot pending = lookup.PendingRequest;
        if (!pending.FailoverEligible ||
            !pending.SendRequest.AllowFailover)
        {
            return CreateRequestFailoverFailure(
                "request-failover-disabled");
        }

        if (!pending.RetransmissionEligible ||
            !pending.SendRequest.AllowRetransmission)
        {
            return CreateRequestFailoverFailure(
                "request-retransmission-disabled");
        }

        if (policy == null)
        {
            return CreateRequestFailoverFailure(
                "request-failover-policy-required");
        }

        if (!policy.ApplicationAllowsFailover)
        {
            return CreateRequestFailoverFailure(
                "application-failover-denied");
        }

        if (!policy.SessionAllowsFailover)
        {
            return CreateRequestFailoverFailure(
                "session-failover-denied");
        }

        if (!pending.Delivered || !policy.DeliveryUncertain)
        {
            return CreateRequestFailoverFailure(
                "request-not-retransmittable");
        }

        DiameterValueResult normalizedAlternate =
            ValueCodecs.NormalizeDiameterIdentity(alternatePeer);
        if (normalizedAlternate.Status != DiameterCodecStatus.Success)
        {
            return CreateRequestFailoverFailure(
                "invalid-alternate-peer");
        }

        DiameterValueResult normalizedCurrent =
            ValueCodecs.NormalizeDiameterIdentity(pending.PeerIdentity);
        if (normalizedCurrent.Status != DiameterCodecStatus.Success)
        {
            return CreateRequestFailoverFailure(
                "invalid-current-peer");
        }

        if (normalizedAlternate.Text == normalizedCurrent.Text)
        {
            return CreateRequestFailoverFailure(
                "alternate-peer-not-distinct");
        }

        RequestFailoverDestination destination =
            ReadRequestFailoverDestination(
                pending.OutboundMessage,
                normalizedCurrent.Text);
        if (!destination.Valid)
        {
            return CreateRequestFailoverFailure(destination.Code);
        }

        if (destination.MatchesCurrentPeer &&
            !policy.RemoveDestinationHost)
        {
            return CreateRequestFailoverFailure(
                "fixed-destination-failover-denied");
        }

        DiameterMessage retransmission = Table.ClonePendingMessage(
            pending.OutboundMessage);
        if (policy.RoutedMessage != null)
        {
            retransmission = Table.ClonePendingMessage(
                policy.RoutedMessage);
        }

        if (policy.RemoveDestinationHost)
        {
            RemoveRequestFailoverDestinationHosts(retransmission);
        }

        uint nextHopByHopIdentifier =
            IdentifierSource.CreateHopByHopIdentifier();
        if (nextHopByHopIdentifier == pending.HopByHopIdentifier)
        {
            return CreateRequestFailoverFailure(
                "hop-by-hop-identifier-not-new");
        }

        retransmission.Flags = retransmission.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;
        retransmission.HopByHopIdentifier = nextHopByHopIdentifier;
        retransmission.EndToEndIdentifier = pending.EndToEndIdentifier;
        DiameterIdentity alternate = new DiameterIdentity();
        alternate.Value = normalizedAlternate.Text;
        PendingRequestTableResult retried = Table.RetryPendingRequest(
            requestIdentifier,
            pending.HopByHopIdentifier,
            alternate,
            retransmission,
            true);
        if (!retried.Accepted || retried.PendingRequest == null)
        {
            RequestFailoverResult rejected =
                CreateRequestFailoverFailure(retried.Code);
            rejected.Eligible = true;
            return rejected;
        }

        RequestFailoverResult result = new RequestFailoverResult();
        result.Eligible = true;
        result.Retried = true;
        result.AlternateRoute = true;
        result.PendingRequest = retried.PendingRequest;
        result.Message = Table.ClonePendingMessage(
            retried.PendingRequest.OutboundMessage);
        result.PeerIdentity = Table.ClonePendingIdentity(
            retried.PendingRequest.PeerIdentity);
        result.Code = "request-retransmitted-to-alternate";
        return result;
    }

    internal RequestFailoverDestination ReadRequestFailoverDestination(
        DiameterMessage message,
        string normalizedCurrentPeer)
    {
        RequestFailoverDestination result =
            new RequestFailoverDestination();
        int count = 0;
        string destination = string.Empty;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationHost)
            {
                count = count + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    result.Code = "invalid-destination-host";
                    return result;
                }

                destination = decoded.Text;
            }

            index = index + 1;
        }

        if (count > 1)
        {
            result.Code = "invalid-destination-host-count";
            return result;
        }

        result.Valid = true;
        result.Present = count == 1;
        result.MatchesCurrentPeer = result.Present &&
            destination == normalizedCurrentPeer;
        result.Code = "valid-failover-destination";
        return result;
    }

    internal void RemoveRequestFailoverDestinationHosts(
        DiameterMessage message)
    {
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationHost)
            {
                message.Avps.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal RequestFailoverResult CreateRequestFailoverFailure(
        string code)
    {
        RequestFailoverResult result = new RequestFailoverResult();
        result.Code = code;
        return result;
    }
}
