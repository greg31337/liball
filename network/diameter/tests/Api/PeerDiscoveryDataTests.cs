using System;
using System.Net;

namespace diameter.Tests;

public class PeerDiscoveryDataTests
{
    [Test]
    public void PeerDataDefaultsAreUsable()
    {
        DiameterPeerConfiguration configuration = new DiameterPeerConfiguration();
        DiameterPeerCapabilities capabilities = new DiameterPeerCapabilities();
        DiameterPeerSnapshot snapshot = new DiameterPeerSnapshot();
        DiameterPeerEvent peerEvent = new DiameterPeerEvent();
        DiameterDisconnectRequest disconnect = new DiameterDisconnectRequest();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Identity.Value, Is.Empty);
            Assert.That(configuration.Realm.Value, Is.Empty);
            Assert.That(configuration.Endpoints, Is.Empty);
            Assert.That(configuration.RequiredApplications, Is.Empty);
            Assert.That(configuration.Source, Is.EqualTo(DiameterPeerSource.Static));
            Assert.That(configuration.Enabled, Is.True);
            Assert.That(configuration.ConnectAutomatically, Is.True);
            Assert.That(configuration.AcceptInboundConnections, Is.True);
            Assert.That(configuration.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));

            Assert.That(capabilities.OriginHost.Value, Is.Empty);
            Assert.That(capabilities.OriginRealm.Value, Is.Empty);
            Assert.That(capabilities.HostAddresses, Is.Empty);
            Assert.That(capabilities.VendorId, Is.Zero);
            Assert.That(capabilities.ProductName, Is.Empty);
            Assert.That(capabilities.FirmwareRevision, Is.Zero);
            Assert.That(capabilities.SupportedVendorIds, Is.Empty);
            Assert.That(capabilities.AuthApplicationIds, Is.Empty);
            Assert.That(capabilities.AcctApplicationIds, Is.Empty);
            Assert.That(capabilities.VendorSpecificApplications, Is.Empty);
            Assert.That(capabilities.InbandSecurityIds, Is.Empty);
            Assert.That(capabilities.OriginStateId, Is.Zero);
            Assert.That(capabilities.CommonApplications, Is.Empty);
            Assert.That(capabilities.UpdatedAt, Is.EqualTo(default(DateTimeOffset)));

            Assert.That(snapshot.Configuration, Is.Not.Null);
            Assert.That(snapshot.Capabilities, Is.Not.Null);
            Assert.That(snapshot.CurrentEndpoint, Is.Null);
            Assert.That(snapshot.Connection, Is.Null);
            Assert.That(snapshot.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(snapshot.Direction, Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(snapshot.StateChangedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.ConnectedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.NextConnectionAttemptAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.LastActivityAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.LastWatchdogAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Initial));
            Assert.That(snapshot.WatchdogDeadlineAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.PendingWatchdogHopByHopIdentifier, Is.Zero);
            Assert.That(snapshot.PendingWatchdogEndToEndIdentifier, Is.Zero);
            Assert.That(snapshot.ReopenWatchdogMisses, Is.Zero);
            Assert.That(snapshot.ConsecutiveWatchdogSuccesses, Is.Zero);
            Assert.That(snapshot.ConsecutiveWatchdogFailures, Is.Zero);
            Assert.That(snapshot.ReconnectCount, Is.Zero);
            Assert.That(snapshot.DisconnectPending, Is.False);
            Assert.That(snapshot.DisconnectInitiatedLocally, Is.False);
            Assert.That(snapshot.DisconnectDrainPending, Is.False);
            Assert.That(snapshot.DisconnectAnswerPending, Is.False);
            Assert.That(snapshot.DisconnectTransportClosurePending, Is.False);
            Assert.That(snapshot.DisconnectDeadlineAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.PendingDisconnectHopByHopIdentifier, Is.Zero);
            Assert.That(snapshot.PendingDisconnectEndToEndIdentifier, Is.Zero);
            Assert.That(snapshot.LastDisconnectCause,
                Is.EqualTo(DiameterDisconnectCause.Rebooting));
            Assert.That(snapshot.LastDisconnectedAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(snapshot.WatchdogPending, Is.False);
            Assert.That(snapshot.Available, Is.False);

            Assert.That(peerEvent.Kind, Is.EqualTo(DiameterPeerEventKind.Added));
            Assert.That(peerEvent.Peer, Is.Not.Null);
            Assert.That(peerEvent.PreviousState, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(peerEvent.CurrentState, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(peerEvent.CapabilityChange, Is.EqualTo(DiameterCapabilityChange.None));
            Assert.That(peerEvent.PreviousOriginStateId, Is.Zero);
            Assert.That(peerEvent.CurrentOriginStateId, Is.Zero);
            Assert.That(peerEvent.OccurredAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(peerEvent.Code, Is.Empty);
            Assert.That(peerEvent.Message, Is.Empty);

            Assert.That(disconnect.PeerIdentity.Value, Is.Empty);
            Assert.That(disconnect.Cause, Is.EqualTo(DiameterDisconnectCause.Rebooting));
            Assert.That(disconnect.TimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(disconnect.WaitForAnswer, Is.True);
            Assert.That(disconnect.SuppressReconnect, Is.False);
        });
    }

    [Test]
    public void DiscoveryDataDefaultsAreUsable()
    {
        DiameterDiscoveryRequest request = new DiameterDiscoveryRequest();
        DiameterDiscoveryResult result = new DiameterDiscoveryResult();
        DiameterNaptrRecord naptr = new DiameterNaptrRecord();
        DiameterSrvRecord srv = new DiameterSrvRecord();
        DiameterAddressRecord address = new DiameterAddressRecord();

        Assert.Multiple(() =>
        {
            Assert.That(request.Realm.Value, Is.Empty);
            Assert.That(request.Application, Is.Not.Null);
            Assert.That(request.Transport, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(request.Security, Is.EqualTo(DiameterSecurityMode.None));
            Assert.That(request.AllowStaticPeers, Is.True);
            Assert.That(request.AllowDnsDiscovery, Is.True);
            Assert.That(request.AllowCachedResults, Is.True);
            Assert.That(request.MaximumResults, Is.EqualTo(32));

            Assert.That(result.Request, Is.Not.Null);
            Assert.That(result.Peers, Is.Empty);
            Assert.That(result.Endpoints, Is.Empty);
            Assert.That(result.NaptrRecords, Is.Empty);
            Assert.That(result.SrvRecords, Is.Empty);
            Assert.That(result.AddressRecords, Is.Empty);
            Assert.That(result.Resolved, Is.False);
            Assert.That(result.FromCache, Is.False);
            Assert.That(result.Negative, Is.False);
            Assert.That(result.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(result.Code, Is.Empty);
            Assert.That(result.Message, Is.Empty);

            Assert.That(naptr.Name, Is.Empty);
            Assert.That(naptr.Order, Is.Zero);
            Assert.That(naptr.Preference, Is.Zero);
            Assert.That(naptr.Flags, Is.Empty);
            Assert.That(naptr.Service, Is.Empty);
            Assert.That(naptr.RegularExpression, Is.Empty);
            Assert.That(naptr.Replacement, Is.Empty);
            Assert.That(naptr.TimeToLiveSeconds, Is.Zero);
            Assert.That(naptr.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));

            Assert.That(srv.Name, Is.Empty);
            Assert.That(srv.Priority, Is.Zero);
            Assert.That(srv.Weight, Is.Zero);
            Assert.That(srv.Port, Is.Zero);
            Assert.That(srv.Target, Is.Empty);
            Assert.That(srv.TimeToLiveSeconds, Is.Zero);
            Assert.That(srv.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));

            Assert.That(address.Name, Is.Empty);
            Assert.That(address.Address, Is.Null);
            Assert.That(address.TimeToLiveSeconds, Is.Zero);
            Assert.That(address.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));
        });
    }

    [Test]
    public void MutablePeerAndDiscoveryStorageIsOwnedByEachObject()
    {
        DiameterPeerConfiguration firstConfiguration = new DiameterPeerConfiguration();
        DiameterPeerConfiguration secondConfiguration = new DiameterPeerConfiguration();
        DiameterPeerCapabilities firstCapabilities = new DiameterPeerCapabilities();
        DiameterPeerCapabilities secondCapabilities = new DiameterPeerCapabilities();
        DiameterPeerSnapshot firstSnapshot = new DiameterPeerSnapshot();
        DiameterPeerSnapshot secondSnapshot = new DiameterPeerSnapshot();
        DiameterPeerEvent firstEvent = new DiameterPeerEvent();
        DiameterPeerEvent secondEvent = new DiameterPeerEvent();
        DiameterDiscoveryResult firstResult = new DiameterDiscoveryResult();
        DiameterDiscoveryResult secondResult = new DiameterDiscoveryResult();

        firstConfiguration.Identity.Value = "first.example";
        firstConfiguration.Endpoints.Add(new DiameterEndpoint());
        firstConfiguration.RequiredApplications.Add(new DiameterApplicationId());
        firstCapabilities.OriginHost.Value = "first.example";
        firstCapabilities.HostAddresses.Add(IPAddress.Loopback);
        firstCapabilities.SupportedVendorIds.Add(1);
        firstCapabilities.AuthApplicationIds.Add(2);
        firstCapabilities.AcctApplicationIds.Add(3);
        firstCapabilities.VendorSpecificApplications.Add(new DiameterApplicationId());
        firstCapabilities.InbandSecurityIds.Add(4);
        firstCapabilities.CommonApplications.Add(new DiameterApplicationId());
        firstSnapshot.Configuration.Identity.Value = "snapshot.example";
        firstEvent.Peer.Configuration.Identity.Value = "event.example";
        firstResult.Request.Realm.Value = "example";
        firstResult.Peers.Add(new DiameterPeerConfiguration());
        firstResult.Endpoints.Add(new DiameterEndpoint());
        firstResult.NaptrRecords.Add(new DiameterNaptrRecord());
        firstResult.SrvRecords.Add(new DiameterSrvRecord());
        firstResult.AddressRecords.Add(new DiameterAddressRecord());

        Assert.Multiple(() =>
        {
            Assert.That(secondConfiguration.Identity.Value, Is.Empty);
            Assert.That(secondConfiguration.Endpoints, Is.Empty);
            Assert.That(secondConfiguration.RequiredApplications, Is.Empty);
            Assert.That(firstConfiguration.Identity, Is.Not.SameAs(secondConfiguration.Identity));
            Assert.That(firstConfiguration.Realm, Is.Not.SameAs(secondConfiguration.Realm));
            Assert.That(firstCapabilities.OriginHost, Is.Not.SameAs(secondCapabilities.OriginHost));
            Assert.That(firstCapabilities.OriginRealm, Is.Not.SameAs(secondCapabilities.OriginRealm));
            Assert.That(secondCapabilities.HostAddresses, Is.Empty);
            Assert.That(secondCapabilities.SupportedVendorIds, Is.Empty);
            Assert.That(secondCapabilities.AuthApplicationIds, Is.Empty);
            Assert.That(secondCapabilities.AcctApplicationIds, Is.Empty);
            Assert.That(secondCapabilities.VendorSpecificApplications, Is.Empty);
            Assert.That(secondCapabilities.InbandSecurityIds, Is.Empty);
            Assert.That(secondCapabilities.CommonApplications, Is.Empty);
            Assert.That(secondSnapshot.Configuration.Identity.Value, Is.Empty);
            Assert.That(firstSnapshot.Configuration, Is.Not.SameAs(secondSnapshot.Configuration));
            Assert.That(firstSnapshot.Capabilities, Is.Not.SameAs(secondSnapshot.Capabilities));
            Assert.That(secondEvent.Peer.Configuration.Identity.Value, Is.Empty);
            Assert.That(firstEvent.Peer, Is.Not.SameAs(secondEvent.Peer));
            Assert.That(secondResult.Request.Realm.Value, Is.Empty);
            Assert.That(firstResult.Request, Is.Not.SameAs(secondResult.Request));
            Assert.That(secondResult.Peers, Is.Empty);
            Assert.That(secondResult.Endpoints, Is.Empty);
            Assert.That(secondResult.NaptrRecords, Is.Empty);
            Assert.That(secondResult.SrvRecords, Is.Empty);
            Assert.That(secondResult.AddressRecords, Is.Empty);
        });
    }

    [Test]
    public void PeerFieldsAndOperationalEventReferencesRoundTrip()
    {
        DateTimeOffset occurredAt = new DateTimeOffset(
            2026, 8, 29, 12, 30, 0, TimeSpan.Zero);
        DiameterEndpoint endpoint = CreateEndpoint();
        DiameterApplicationId application = CreateApplication();

        DiameterPeerConfiguration configuration = CreateValidPeerConfiguration();
        configuration.Source = DiameterPeerSource.Dynamic;
        configuration.Enabled = false;
        configuration.ConnectAutomatically = false;
        configuration.AcceptInboundConnections = false;
        configuration.ExpiresAt = occurredAt.AddHours(1);

        DiameterPeerCapabilities capabilities = new DiameterPeerCapabilities();
        capabilities.OriginHost.Value = "peer.example";
        capabilities.OriginRealm.Value = "example";
        capabilities.HostAddresses.Add(IPAddress.Parse("2001:db8::1"));
        capabilities.VendorId = 10415;
        capabilities.ProductName = "Diameter test peer";
        capabilities.FirmwareRevision = 7;
        capabilities.SupportedVendorIds.Add(10415);
        capabilities.AuthApplicationIds.Add(4);
        capabilities.AcctApplicationIds.Add(3);
        capabilities.VendorSpecificApplications.Add(application);
        capabilities.InbandSecurityIds.Add(1);
        capabilities.OriginStateId = 123;
        capabilities.CommonApplications.Add(application);
        capabilities.UpdatedAt = occurredAt;

        DiameterConnectionMetadata connection = new DiameterConnectionMetadata();
        DiameterPeerSnapshot snapshot = new DiameterPeerSnapshot();
        snapshot.Configuration = configuration;
        snapshot.Capabilities = capabilities;
        snapshot.CurrentEndpoint = endpoint;
        snapshot.Connection = connection;
        snapshot.State = DiameterPeerState.InitiatorOpen;
        snapshot.Direction = DiameterConnectionDirection.Outbound;
        snapshot.StateChangedAt = occurredAt.AddMinutes(-5);
        snapshot.ConnectedAt = occurredAt.AddMinutes(-4);
        snapshot.NextConnectionAttemptAt = occurredAt.AddMinutes(5);
        snapshot.LastActivityAt = occurredAt.AddSeconds(-10);
        snapshot.LastWatchdogAt = occurredAt.AddSeconds(-30);
        snapshot.WatchdogState = DiameterPeerWatchdogState.Suspect;
        snapshot.WatchdogDeadlineAt = occurredAt.AddSeconds(15);
        snapshot.PendingWatchdogHopByHopIdentifier = 100;
        snapshot.PendingWatchdogEndToEndIdentifier = 200;
        snapshot.ReopenWatchdogMisses = 1;
        snapshot.ConsecutiveWatchdogSuccesses = 3;
        snapshot.ConsecutiveWatchdogFailures = 1;
        snapshot.ReconnectCount = 9;
        snapshot.DisconnectPending = true;
        snapshot.DisconnectInitiatedLocally = true;
        snapshot.DisconnectDrainPending = true;
        snapshot.DisconnectAnswerPending = true;
        snapshot.DisconnectTransportClosurePending = true;
        snapshot.DisconnectDeadlineAt = occurredAt.AddSeconds(20);
        snapshot.PendingDisconnectHopByHopIdentifier = 300;
        snapshot.PendingDisconnectEndToEndIdentifier = 400;
        snapshot.LastDisconnectCause = DiameterDisconnectCause.Busy;
        snapshot.LastDisconnectedAt = occurredAt.AddMinutes(-1);
        snapshot.WatchdogPending = true;
        snapshot.Available = true;

        DiameterPeerEvent peerEvent = new DiameterPeerEvent();
        peerEvent.Kind = DiameterPeerEventKind.CapabilitiesChanged;
        peerEvent.Peer = snapshot;
        peerEvent.PreviousState = DiameterPeerState.WaitInitiatorCapabilitiesAnswer;
        peerEvent.CurrentState = DiameterPeerState.InitiatorOpen;
        peerEvent.CapabilityChange = DiameterCapabilityChange.Applications;
        peerEvent.PreviousOriginStateId = 122;
        peerEvent.CurrentOriginStateId = 123;
        peerEvent.OccurredAt = occurredAt;
        peerEvent.Code = "peer.capabilities.changed";
        peerEvent.Message = "Peer applications changed.";

        DiameterDisconnectRequest disconnect = new DiameterDisconnectRequest();
        disconnect.PeerIdentity = configuration.Identity;
        disconnect.Cause = DiameterDisconnectCause.Busy;
        disconnect.TimeoutMilliseconds = 5000;
        disconnect.WaitForAnswer = false;
        disconnect.SuppressReconnect = true;

        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.PeerEvent = peerEvent;
        eventData.DisconnectRequest = disconnect;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Source, Is.EqualTo(DiameterPeerSource.Dynamic));
            Assert.That(configuration.Enabled, Is.False);
            Assert.That(configuration.ConnectAutomatically, Is.False);
            Assert.That(configuration.AcceptInboundConnections, Is.False);
            Assert.That(configuration.ExpiresAt, Is.EqualTo(occurredAt.AddHours(1)));
            Assert.That(capabilities.HostAddresses[0], Is.EqualTo(IPAddress.Parse("2001:db8::1")));
            Assert.That(capabilities.VendorId, Is.EqualTo(10415U));
            Assert.That(capabilities.ProductName, Is.EqualTo("Diameter test peer"));
            Assert.That(capabilities.FirmwareRevision, Is.EqualTo(7U));
            Assert.That(capabilities.SupportedVendorIds, Is.EqualTo(new uint[] { 10415 }));
            Assert.That(capabilities.AuthApplicationIds, Is.EqualTo(new uint[] { 4 }));
            Assert.That(capabilities.AcctApplicationIds, Is.EqualTo(new uint[] { 3 }));
            Assert.That(capabilities.VendorSpecificApplications[0], Is.SameAs(application));
            Assert.That(capabilities.InbandSecurityIds, Is.EqualTo(new uint[] { 1 }));
            Assert.That(capabilities.OriginStateId, Is.EqualTo(123U));
            Assert.That(capabilities.CommonApplications[0], Is.SameAs(application));
            Assert.That(capabilities.UpdatedAt, Is.EqualTo(occurredAt));
            Assert.That(snapshot.Configuration, Is.SameAs(configuration));
            Assert.That(snapshot.Capabilities, Is.SameAs(capabilities));
            Assert.That(snapshot.CurrentEndpoint, Is.SameAs(endpoint));
            Assert.That(snapshot.Connection, Is.SameAs(connection));
            Assert.That(snapshot.State, Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(snapshot.Direction, Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(snapshot.StateChangedAt, Is.EqualTo(occurredAt.AddMinutes(-5)));
            Assert.That(snapshot.ConnectedAt, Is.EqualTo(occurredAt.AddMinutes(-4)));
            Assert.That(snapshot.NextConnectionAttemptAt, Is.EqualTo(occurredAt.AddMinutes(5)));
            Assert.That(snapshot.LastActivityAt, Is.EqualTo(occurredAt.AddSeconds(-10)));
            Assert.That(snapshot.LastWatchdogAt, Is.EqualTo(occurredAt.AddSeconds(-30)));
            Assert.That(snapshot.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Suspect));
            Assert.That(snapshot.WatchdogDeadlineAt,
                Is.EqualTo(occurredAt.AddSeconds(15)));
            Assert.That(snapshot.PendingWatchdogHopByHopIdentifier, Is.EqualTo(100U));
            Assert.That(snapshot.PendingWatchdogEndToEndIdentifier, Is.EqualTo(200U));
            Assert.That(snapshot.ReopenWatchdogMisses, Is.EqualTo(1));
            Assert.That(snapshot.ConsecutiveWatchdogSuccesses, Is.EqualTo(3));
            Assert.That(snapshot.ConsecutiveWatchdogFailures, Is.EqualTo(1));
            Assert.That(snapshot.ReconnectCount, Is.EqualTo(9));
            Assert.That(snapshot.DisconnectPending, Is.True);
            Assert.That(snapshot.DisconnectInitiatedLocally, Is.True);
            Assert.That(snapshot.DisconnectDrainPending, Is.True);
            Assert.That(snapshot.DisconnectAnswerPending, Is.True);
            Assert.That(snapshot.DisconnectTransportClosurePending, Is.True);
            Assert.That(snapshot.DisconnectDeadlineAt,
                Is.EqualTo(occurredAt.AddSeconds(20)));
            Assert.That(snapshot.PendingDisconnectHopByHopIdentifier,
                Is.EqualTo(300U));
            Assert.That(snapshot.PendingDisconnectEndToEndIdentifier,
                Is.EqualTo(400U));
            Assert.That(snapshot.LastDisconnectCause,
                Is.EqualTo(DiameterDisconnectCause.Busy));
            Assert.That(snapshot.LastDisconnectedAt,
                Is.EqualTo(occurredAt.AddMinutes(-1)));
            Assert.That(snapshot.WatchdogPending, Is.True);
            Assert.That(snapshot.Available, Is.True);
            Assert.That(peerEvent.Kind, Is.EqualTo(DiameterPeerEventKind.CapabilitiesChanged));
            Assert.That(peerEvent.Peer, Is.SameAs(snapshot));
            Assert.That(peerEvent.PreviousState, Is.EqualTo(DiameterPeerState.WaitInitiatorCapabilitiesAnswer));
            Assert.That(peerEvent.CurrentState, Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(peerEvent.CapabilityChange, Is.EqualTo(DiameterCapabilityChange.Applications));
            Assert.That(peerEvent.PreviousOriginStateId, Is.EqualTo(122U));
            Assert.That(peerEvent.CurrentOriginStateId, Is.EqualTo(123U));
            Assert.That(peerEvent.OccurredAt, Is.EqualTo(occurredAt));
            Assert.That(peerEvent.Code, Is.EqualTo("peer.capabilities.changed"));
            Assert.That(peerEvent.Message, Is.EqualTo("Peer applications changed."));
            Assert.That(disconnect.PeerIdentity, Is.SameAs(configuration.Identity));
            Assert.That(disconnect.Cause, Is.EqualTo(DiameterDisconnectCause.Busy));
            Assert.That(disconnect.TimeoutMilliseconds, Is.EqualTo(5000));
            Assert.That(disconnect.WaitForAnswer, Is.False);
            Assert.That(disconnect.SuppressReconnect, Is.True);
            Assert.That(eventData.PeerEvent, Is.SameAs(peerEvent));
            Assert.That(eventData.DisconnectRequest, Is.SameAs(disconnect));
        });
    }

    [Test]
    public void DiscoveryFieldsAndOperationalEventReferenceRoundTrip()
    {
        DateTimeOffset expiresAt = new DateTimeOffset(
            2026, 8, 29, 13, 0, 0, TimeSpan.Zero);
        DiameterApplicationId application = CreateApplication();
        DiameterDiscoveryRequest request = new DiameterDiscoveryRequest();
        request.Realm.Value = "example";
        request.Application = application;
        request.Transport = DiameterTransportKind.Sctp;
        request.Security = DiameterSecurityMode.Dtls;
        request.AllowStaticPeers = false;
        request.AllowDnsDiscovery = true;
        request.AllowCachedResults = false;
        request.MaximumResults = 7;

        DiameterNaptrRecord naptr = new DiameterNaptrRecord();
        naptr.Name = "example";
        naptr.Order = 10;
        naptr.Preference = 20;
        naptr.Flags = "S";
        naptr.Service = "AAA+D2S";
        naptr.RegularExpression = "";
        naptr.Replacement = "_diameter._sctp.example";
        naptr.TimeToLiveSeconds = 60;
        naptr.ExpiresAt = expiresAt;

        DiameterSrvRecord srv = new DiameterSrvRecord();
        srv.Name = "_diameter._sctp.example";
        srv.Priority = 1;
        srv.Weight = 10;
        srv.Port = 3868;
        srv.Target = "peer.example";
        srv.TimeToLiveSeconds = 45;
        srv.ExpiresAt = expiresAt;

        DiameterAddressRecord address = new DiameterAddressRecord();
        address.Name = "peer.example";
        address.Address = IPAddress.Parse("192.0.2.1");
        address.TimeToLiveSeconds = 30;
        address.ExpiresAt = expiresAt;

        DiameterEndpoint endpoint = CreateEndpoint();
        DiameterDiscoveryResult result = new DiameterDiscoveryResult();
        result.Request = request;
        DiameterPeerConfiguration peer = new DiameterPeerConfiguration();
        result.Peers.Add(peer);
        result.Endpoints.Add(endpoint);
        result.NaptrRecords.Add(naptr);
        result.SrvRecords.Add(srv);
        result.AddressRecords.Add(address);
        result.Resolved = true;
        result.FromCache = true;
        result.Negative = true;
        result.ExpiresAt = expiresAt;
        result.Code = "discovery.complete";
        result.Message = "Discovery completed.";

        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.DiscoveryResult = result;

        Assert.Multiple(() =>
        {
            Assert.That(request.Realm.Value, Is.EqualTo("example"));
            Assert.That(request.Application, Is.SameAs(application));
            Assert.That(request.Transport, Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(request.Security, Is.EqualTo(DiameterSecurityMode.Dtls));
            Assert.That(request.AllowStaticPeers, Is.False);
            Assert.That(request.AllowDnsDiscovery, Is.True);
            Assert.That(request.AllowCachedResults, Is.False);
            Assert.That(request.MaximumResults, Is.EqualTo(7));
            Assert.That(naptr.Name, Is.EqualTo("example"));
            Assert.That(naptr.Order, Is.EqualTo(10));
            Assert.That(naptr.Preference, Is.EqualTo(20));
            Assert.That(naptr.Flags, Is.EqualTo("S"));
            Assert.That(naptr.Service, Is.EqualTo("AAA+D2S"));
            Assert.That(naptr.RegularExpression, Is.Empty);
            Assert.That(naptr.Replacement, Is.EqualTo("_diameter._sctp.example"));
            Assert.That(naptr.TimeToLiveSeconds, Is.EqualTo(60U));
            Assert.That(naptr.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(srv.Name, Is.EqualTo("_diameter._sctp.example"));
            Assert.That(srv.Priority, Is.EqualTo(1));
            Assert.That(srv.Weight, Is.EqualTo(10));
            Assert.That(srv.Port, Is.EqualTo(3868));
            Assert.That(srv.Target, Is.EqualTo("peer.example"));
            Assert.That(srv.TimeToLiveSeconds, Is.EqualTo(45U));
            Assert.That(srv.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(address.Name, Is.EqualTo("peer.example"));
            Assert.That(address.Address, Is.EqualTo(IPAddress.Parse("192.0.2.1")));
            Assert.That(address.TimeToLiveSeconds, Is.EqualTo(30U));
            Assert.That(address.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Peers[0], Is.SameAs(peer));
            Assert.That(result.Endpoints[0], Is.SameAs(endpoint));
            Assert.That(result.NaptrRecords[0], Is.SameAs(naptr));
            Assert.That(result.SrvRecords[0], Is.SameAs(srv));
            Assert.That(result.AddressRecords[0], Is.SameAs(address));
            Assert.That(result.Resolved, Is.True);
            Assert.That(result.FromCache, Is.True);
            Assert.That(result.Negative, Is.True);
            Assert.That(result.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(result.Code, Is.EqualTo("discovery.complete"));
            Assert.That(result.Message, Is.EqualTo("Discovery completed."));
            Assert.That(eventData.DiscoveryResult, Is.SameAs(result));
        });
    }

    [Test]
    public void PeerAndDisconnectValidationAcceptsSupportedConfigurations()
    {
        DiameterPeerConfiguration staticPeer = CreateValidPeerConfiguration();

        DiameterPeerConfiguration dynamicPeer = CreateValidPeerConfiguration();
        dynamicPeer.Source = DiameterPeerSource.Dynamic;
        dynamicPeer.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        DiameterPeerConfiguration tlsPeer = CreateValidPeerConfiguration();
        tlsPeer.Endpoints[0].Security = DiameterSecurityMode.Tls;

        DiameterPeerConfiguration externalPeer = CreateValidPeerConfiguration();
        externalPeer.Endpoints[0].Host = string.Empty;
        externalPeer.Endpoints[0].Port = 0;
        externalPeer.Endpoints[0].Transport = DiameterTransportKind.External;
        externalPeer.Endpoints[0].Security = DiameterSecurityMode.External;

        DiameterDisconnectRequest disconnect = new DiameterDisconnectRequest();
        disconnect.PeerIdentity.Value = "peer.example";
        disconnect.Cause = DiameterDisconnectCause.DoNotWantToTalkToYou;
        disconnect.TimeoutMilliseconds = 1;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(staticPeer.ValidatePeerConfiguration);
            Assert.DoesNotThrow(dynamicPeer.ValidatePeerConfiguration);
            Assert.DoesNotThrow(tlsPeer.ValidatePeerConfiguration);
            Assert.DoesNotThrow(externalPeer.ValidatePeerConfiguration);
            Assert.DoesNotThrow(disconnect.ValidateDisconnectRequest);
        });
    }

    [Test]
    public void PeerValidationRejectsInvalidIdentityEndpointsAndRelationships()
    {
        DiameterPeerConfiguration missingIdentity = CreateValidPeerConfiguration();
        missingIdentity.Identity.Value = string.Empty;

        DiameterPeerConfiguration noEndpoints = CreateValidPeerConfiguration();
        noEndpoints.Endpoints.Clear();

        DiameterPeerConfiguration duplicateEndpoints = CreateValidPeerConfiguration();
        duplicateEndpoints.Endpoints.Add(CreateEndpoint());

        DiameterPeerConfiguration duplicateApplications = CreateValidPeerConfiguration();
        duplicateApplications.RequiredApplications.Add(CreateApplication());

        DiameterPeerConfiguration invalidSource = CreateValidPeerConfiguration();
        invalidSource.Source = (DiameterPeerSource)99;

        DiameterPeerConfiguration staticExpiry = CreateValidPeerConfiguration();
        staticExpiry.ExpiresAt = DateTimeOffset.UtcNow;

        DiameterPeerConfiguration missingDynamicExpiry = CreateValidPeerConfiguration();
        missingDynamicExpiry.Source = DiameterPeerSource.Dynamic;

        DiameterPeerConfiguration invalidPort = CreateValidPeerConfiguration();
        invalidPort.Endpoints[0].Port = 0;

        DiameterPeerConfiguration invalidTransport = CreateValidPeerConfiguration();
        invalidTransport.Endpoints[0].Transport = (DiameterTransportKind)99;

        DiameterPeerConfiguration incompatibleSecurity = CreateValidPeerConfiguration();
        incompatibleSecurity.Endpoints[0].Transport = DiameterTransportKind.Sctp;
        incompatibleSecurity.Endpoints[0].Security = DiameterSecurityMode.Tls;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(missingIdentity.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(noEndpoints.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(duplicateEndpoints.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(duplicateApplications.ValidatePeerConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidSource.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(staticExpiry.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(missingDynamicExpiry.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(invalidPort.ValidatePeerConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(invalidTransport.ValidatePeerConfiguration);
            Assert.Throws<ArgumentException>(incompatibleSecurity.ValidatePeerConfiguration);
        });
    }

    [Test]
    public void DiscoveryAndDisconnectValidationRejectsInvalidRequests()
    {
        DiameterDiscoveryRequest validDiscovery = CreateValidDiscoveryRequest();

        DiameterDiscoveryRequest missingRealm = CreateValidDiscoveryRequest();
        missingRealm.Realm.Value = string.Empty;

        DiameterDiscoveryRequest noSources = CreateValidDiscoveryRequest();
        noSources.AllowStaticPeers = false;
        noSources.AllowDnsDiscovery = false;

        DiameterDiscoveryRequest noResults = CreateValidDiscoveryRequest();
        noResults.MaximumResults = 0;

        DiameterDiscoveryRequest tooManyResults = CreateValidDiscoveryRequest();
        tooManyResults.MaximumResults = 1025;

        DiameterDiscoveryRequest incompatibleSecurity = CreateValidDiscoveryRequest();
        incompatibleSecurity.Transport = DiameterTransportKind.Sctp;
        incompatibleSecurity.Security = DiameterSecurityMode.Tls;

        DiameterDiscoveryRequest invalidTransport = CreateValidDiscoveryRequest();
        invalidTransport.Transport = (DiameterTransportKind)99;

        DiameterDiscoveryRequest invalidSecurity = CreateValidDiscoveryRequest();
        invalidSecurity.Security = (DiameterSecurityMode)99;

        DiameterDisconnectRequest missingIdentity = new DiameterDisconnectRequest();

        DiameterDisconnectRequest invalidCause = new DiameterDisconnectRequest();
        invalidCause.PeerIdentity.Value = "peer.example";
        invalidCause.Cause = (DiameterDisconnectCause)99;

        DiameterDisconnectRequest invalidTimeout = new DiameterDisconnectRequest();
        invalidTimeout.PeerIdentity.Value = "peer.example";
        invalidTimeout.TimeoutMilliseconds = 0;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(validDiscovery.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(missingRealm.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(noSources.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentOutOfRangeException>(noResults.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentOutOfRangeException>(tooManyResults.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(incompatibleSecurity.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentOutOfRangeException>(invalidTransport.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentOutOfRangeException>(invalidSecurity.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(missingIdentity.ValidateDisconnectRequest);
            Assert.Throws<ArgumentOutOfRangeException>(invalidCause.ValidateDisconnectRequest);
            Assert.Throws<ArgumentOutOfRangeException>(invalidTimeout.ValidateDisconnectRequest);
        });
    }

    [Test]
    public void PeerValidationRejectsEveryRemainingNullAndEndpointBranch()
    {
        AssertEveryMutationIsRejected(
            CreateValidPeerConfiguration,
            value => value.ValidatePeerConfiguration(),
            value => value.Identity = null!,
            value => value.Realm = null!,
            value => value.Endpoints = null!,
            value => value.RequiredApplications = null!,
            value => value.Endpoints.Add(null!),
            value => value.RequiredApplications[0] = null!,
            value => value.RequiredApplications.Add(null!),
            value => value.Endpoints[0] = null!,
            value => value.Endpoints[0].Identity = null!,
            value => value.Endpoints[0].Port = -1,
            value => value.Endpoints[0].Port = 65536,
            value =>
            {
                value.Endpoints[0].Address = null;
                value.Endpoints[0].Host = " ";
            },
            value => value.Endpoints[0].Identity.Value = "bad peer",
            value => value.Endpoints[0].Security =
                (DiameterSecurityMode)(-1),
            value =>
            {
                value.Endpoints[0].Transport = DiameterTransportKind.Tcp;
                value.Endpoints[0].Security = DiameterSecurityMode.Dtls;
            },
            value =>
            {
                value.Endpoints[0].Transport = DiameterTransportKind.Tcp;
                value.Endpoints[0].Security = DiameterSecurityMode.External;
            });

        DiameterPeerConfiguration nullAddressDuplicate =
            CreateValidPeerConfiguration();
        nullAddressDuplicate.Endpoints[0].Address = null;
        DiameterEndpoint duplicate = CreateEndpoint();
        duplicate.Address = null;
        nullAddressDuplicate.Endpoints.Add(duplicate);

        DiameterPeerConfiguration ipsec = CreateValidPeerConfiguration();
        ipsec.Endpoints[0].Security = DiameterSecurityMode.Ipsec;
        DiameterPeerConfiguration dtls = CreateValidPeerConfiguration();
        dtls.Endpoints[0].Transport = DiameterTransportKind.Sctp;
        dtls.Endpoints[0].Security = DiameterSecurityMode.Dtls;
        DiameterPeerConfiguration tls = CreateValidPeerConfiguration();
        tls.Endpoints[0].Security = DiameterSecurityMode.Tls;
        DiameterPeerConfiguration external = CreateValidPeerConfiguration();
        external.Endpoints[0].Transport = DiameterTransportKind.External;
        external.Endpoints[0].Security = DiameterSecurityMode.External;

        DiameterPeerConfiguration distinctApplications =
            CreateValidPeerConfiguration();
        DiameterApplicationId differentId = CreateApplication();
        differentId.Id = 9U;
        distinctApplications.RequiredApplications.Add(differentId);
        DiameterPeerConfiguration distinctVendors =
            CreateValidPeerConfiguration();
        DiameterApplicationId differentVendor = CreateApplication();
        differentVendor.VendorId = 1U;
        distinctVendors.RequiredApplications.Add(differentVendor);
        DiameterPeerConfiguration distinctAccounting =
            CreateValidPeerConfiguration();
        DiameterApplicationId accounting = CreateApplication();
        accounting.IsAccounting = true;
        distinctAccounting.RequiredApplications.Add(accounting);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                nullAddressDuplicate.ValidatePeerConfiguration);
            Assert.DoesNotThrow(ipsec.ValidatePeerConfiguration);
            Assert.DoesNotThrow(dtls.ValidatePeerConfiguration);
            Assert.DoesNotThrow(tls.ValidatePeerConfiguration);
            Assert.DoesNotThrow(external.ValidatePeerConfiguration);
            Assert.DoesNotThrow(
                distinctApplications.ValidatePeerConfiguration);
            Assert.DoesNotThrow(
                distinctVendors.ValidatePeerConfiguration);
            Assert.DoesNotThrow(
                distinctAccounting.ValidatePeerConfiguration);
        });
    }

    [Test]
    public void DiscoveryValidationCoversNullAndSecurityCompatibilityBranches()
    {
        DiameterDiscoveryRequest missingRealm = CreateValidDiscoveryRequest();
        missingRealm.Realm = null!;
        DiameterDiscoveryRequest missingApplication =
            CreateValidDiscoveryRequest();
        missingApplication.Application = null!;
        DiameterDiscoveryRequest dtlsOverTcp = CreateValidDiscoveryRequest();
        dtlsOverTcp.Security = DiameterSecurityMode.Dtls;
        DiameterDiscoveryRequest externalOverTcp =
            CreateValidDiscoveryRequest();
        externalOverTcp.Security = DiameterSecurityMode.External;
        DiameterDiscoveryRequest validDtls = CreateValidDiscoveryRequest();
        validDtls.Transport = DiameterTransportKind.Sctp;
        validDtls.Security = DiameterSecurityMode.Dtls;
        DiameterDiscoveryRequest validExternal =
            CreateValidDiscoveryRequest();
        validExternal.Transport = DiameterTransportKind.External;
        validExternal.Security = DiameterSecurityMode.External;

        DiameterDisconnectRequest nullPeer =
            new DiameterDisconnectRequest();
        nullPeer.PeerIdentity = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingRealm.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(
                missingApplication.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(
                dtlsOverTcp.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(
                externalOverTcp.ValidateDiscoveryRequest);
            Assert.DoesNotThrow(validDtls.ValidateDiscoveryRequest);
            Assert.DoesNotThrow(validExternal.ValidateDiscoveryRequest);
            Assert.Throws<ArgumentException>(
                nullPeer.ValidateDisconnectRequest);
        });
    }

    private static DiameterPeerConfiguration CreateValidPeerConfiguration()
    {
        DiameterPeerConfiguration configuration = new DiameterPeerConfiguration();
        configuration.Identity.Value = "peer.example";
        configuration.Realm.Value = "example";
        configuration.Endpoints.Add(CreateEndpoint());
        configuration.RequiredApplications.Add(CreateApplication());
        return configuration;
    }

    private static DiameterDiscoveryRequest CreateValidDiscoveryRequest()
    {
        DiameterDiscoveryRequest request = new DiameterDiscoveryRequest();
        request.Realm.Value = "example";
        request.Application = CreateApplication();
        return request;
    }

    private static DiameterEndpoint CreateEndpoint()
    {
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Loopback;
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        endpoint.Security = DiameterSecurityMode.None;
        return endpoint;
    }

    private static DiameterApplicationId CreateApplication()
    {
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = DiameterApplicationIds.CreditControl;
        application.VendorId = DiameterVendorIds.ThreeGpp;
        return application;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }
}
