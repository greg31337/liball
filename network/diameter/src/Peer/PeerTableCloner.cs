using System;

namespace diameter;

internal static class PeerTableCloner
{
    internal static DiameterPeerSnapshot ClonePeerTableSnapshot(
        DiameterPeerSnapshot source)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration = ClonePeerTableConfiguration(source.Configuration);
        result.Capabilities = ClonePeerTableCapabilities(source.Capabilities);
        if (source.CurrentEndpoint != null)
        {
            result.CurrentEndpoint = ClonePeerTableEndpoint(source.CurrentEndpoint);
        }

        if (source.Connection != null)
        {
            result.Connection = QueuedDiameterConnection.CloneConnectionMetadata(
                source.Connection);
        }

        result.State = source.State;
        result.Direction = source.Direction;
        result.StateChangedAt = source.StateChangedAt;
        result.ConnectedAt = source.ConnectedAt;
        result.NextConnectionAttemptAt = source.NextConnectionAttemptAt;
        result.LastActivityAt = source.LastActivityAt;
        result.LastWatchdogAt = source.LastWatchdogAt;
        result.WatchdogState = source.WatchdogState;
        result.WatchdogDeadlineAt = source.WatchdogDeadlineAt;
        result.PendingWatchdogHopByHopIdentifier =
            source.PendingWatchdogHopByHopIdentifier;
        result.PendingWatchdogEndToEndIdentifier =
            source.PendingWatchdogEndToEndIdentifier;
        result.ReopenWatchdogMisses = source.ReopenWatchdogMisses;
        result.ConsecutiveWatchdogSuccesses = source.ConsecutiveWatchdogSuccesses;
        result.ConsecutiveWatchdogFailures = source.ConsecutiveWatchdogFailures;
        result.ReconnectCount = source.ReconnectCount;
        result.ReconnectSuppressed = source.ReconnectSuppressed;
        result.ReconnectSuppressionCause = source.ReconnectSuppressionCause;
        result.ReconnectSuppressedAt = source.ReconnectSuppressedAt;
        result.DisconnectPending = source.DisconnectPending;
        result.DisconnectInitiatedLocally = source.DisconnectInitiatedLocally;
        result.DisconnectDrainPending = source.DisconnectDrainPending;
        result.DisconnectAnswerPending = source.DisconnectAnswerPending;
        result.DisconnectTransportClosurePending =
            source.DisconnectTransportClosurePending;
        result.DisconnectDeadlineAt = source.DisconnectDeadlineAt;
        result.PendingDisconnectHopByHopIdentifier =
            source.PendingDisconnectHopByHopIdentifier;
        result.PendingDisconnectEndToEndIdentifier =
            source.PendingDisconnectEndToEndIdentifier;
        result.LastDisconnectCause = source.LastDisconnectCause;
        result.LastDisconnectedAt = source.LastDisconnectedAt;
        result.WatchdogPending = source.WatchdogPending;
        result.Available = source.Available;
        return result;
    }

    internal static DiameterPeerConfiguration ClonePeerTableConfiguration(
        DiameterPeerConfiguration source)
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = source.Identity.Value;
        result.Realm.Value = source.Realm.Value;
        result.Source = source.Source;
        result.Enabled = source.Enabled;
        result.ConnectAutomatically = source.ConnectAutomatically;
        result.AcceptInboundConnections = source.AcceptInboundConnections;
        result.ExpiresAt = source.ExpiresAt;
        int endpointIndex = 0;
        while (endpointIndex < source.Endpoints.Count)
        {
            result.Endpoints.Add(
                ClonePeerTableEndpoint(source.Endpoints[endpointIndex]));
            endpointIndex = endpointIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < source.RequiredApplications.Count)
        {
            result.RequiredApplications.Add(
                ClonePeerTableApplication(
                    source.RequiredApplications[applicationIndex]));
            applicationIndex = applicationIndex + 1;
        }

        return result;
    }

    internal static DiameterPeerEvent ClonePeerTableEvent(
        DiameterPeerEvent source)
    {
        DiameterPeerEvent result = new DiameterPeerEvent();
        result.Kind = source.Kind;
        result.Peer = ClonePeerTableSnapshot(source.Peer);
        result.PreviousState = source.PreviousState;
        result.CurrentState = source.CurrentState;
        result.CapabilityChange = source.CapabilityChange;
        result.PreviousOriginStateId = source.PreviousOriginStateId;
        result.CurrentOriginStateId = source.CurrentOriginStateId;
        result.OccurredAt = source.OccurredAt;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterPeerCapabilities ClonePeerTableCapabilities(
        DiameterPeerCapabilities source)
    {
        DiameterPeerCapabilities result = new DiameterPeerCapabilities();
        result.OriginHost.Value = source.OriginHost.Value;
        result.OriginRealm.Value = source.OriginRealm.Value;
        int addressIndex = 0;
        while (addressIndex < source.HostAddresses.Count)
        {
            result.HostAddresses.Add(source.HostAddresses[addressIndex]);
            addressIndex = addressIndex + 1;
        }

        result.VendorId = source.VendorId;
        result.ProductName = source.ProductName;
        result.FirmwareRevision = source.FirmwareRevision;
        int vendorIndex = 0;
        while (vendorIndex < source.SupportedVendorIds.Count)
        {
            result.SupportedVendorIds.Add(source.SupportedVendorIds[vendorIndex]);
            vendorIndex = vendorIndex + 1;
        }

        int authIndex = 0;
        while (authIndex < source.AuthApplicationIds.Count)
        {
            result.AuthApplicationIds.Add(source.AuthApplicationIds[authIndex]);
            authIndex = authIndex + 1;
        }

        int accountingIndex = 0;
        while (accountingIndex < source.AcctApplicationIds.Count)
        {
            result.AcctApplicationIds.Add(source.AcctApplicationIds[accountingIndex]);
            accountingIndex = accountingIndex + 1;
        }

        int vendorApplicationIndex = 0;
        while (vendorApplicationIndex < source.VendorSpecificApplications.Count)
        {
            result.VendorSpecificApplications.Add(
                ClonePeerTableApplication(
                    source.VendorSpecificApplications[vendorApplicationIndex]));
            vendorApplicationIndex = vendorApplicationIndex + 1;
        }

        int securityIndex = 0;
        while (securityIndex < source.InbandSecurityIds.Count)
        {
            result.InbandSecurityIds.Add(source.InbandSecurityIds[securityIndex]);
            securityIndex = securityIndex + 1;
        }

        result.OriginStateId = source.OriginStateId;
        int commonIndex = 0;
        while (commonIndex < source.CommonApplications.Count)
        {
            result.CommonApplications.Add(
                ClonePeerTableApplication(source.CommonApplications[commonIndex]));
            commonIndex = commonIndex + 1;
        }

        result.UpdatedAt = source.UpdatedAt;
        return result;
    }

    internal static DiameterEndpoint ClonePeerTableEndpoint(DiameterEndpoint source)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = source.Identity.Value;
        result.Host = source.Host;
        result.Address = source.Address;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Security = source.Security;
        return result;
    }

    internal static DiameterApplicationId ClonePeerTableApplication(
        DiameterApplicationId source)
    {
        DiameterApplicationId result = new DiameterApplicationId();
        result.Id = source.Id;
        result.VendorId = source.VendorId;
        result.IsAccounting = source.IsAccounting;
        return result;
    }
}
