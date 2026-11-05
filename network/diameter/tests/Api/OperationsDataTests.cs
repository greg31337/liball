namespace diameter.Tests;

public class OperationsDataTests
{
    [Test]
    public void StatisticsFieldsRepresentAllOperationalAreas()
    {
        DateTimeOffset capturedAt = new DateTimeOffset(
            2026, 8, 29, 12, 30, 0, TimeSpan.Zero);
        DiameterNodeStatistics statistics = new DiameterNodeStatistics();
        statistics.CapturedAt = capturedAt;
        statistics.BytesReceived = 1;
        statistics.MessagesSent = 2;
        statistics.AvpsDecoded = 3;
        statistics.DecodeErrors = 4;
        statistics.PeersConnected = 5;
        statistics.WatchdogTimeouts = 6;
        statistics.RequestsPending = 7;
        statistics.Retransmissions = 8;
        statistics.DuplicateRequests = 9;
        statistics.RoutesAvailable = 10;
        statistics.RedirectsApplied = 11;
        statistics.SessionsActive = 12;
        statistics.AccountingRecordsSent = 13;
        statistics.CreditControlAnswers = 14;
        statistics.LoadReportsReceived = 15;
        statistics.OverloadRejections = 16;
        statistics.QueueDepth = 17;
        statistics.StoreFailures = 18;

        Assert.Multiple(() =>
        {
            Assert.That(statistics.CapturedAt, Is.EqualTo(capturedAt));
            Assert.That(statistics.BytesReceived, Is.EqualTo(1));
            Assert.That(statistics.MessagesSent, Is.EqualTo(2));
            Assert.That(statistics.AvpsDecoded, Is.EqualTo(3));
            Assert.That(statistics.DecodeErrors, Is.EqualTo(4));
            Assert.That(statistics.PeersConnected, Is.EqualTo(5));
            Assert.That(statistics.WatchdogTimeouts, Is.EqualTo(6));
            Assert.That(statistics.RequestsPending, Is.EqualTo(7));
            Assert.That(statistics.Retransmissions, Is.EqualTo(8));
            Assert.That(statistics.DuplicateRequests, Is.EqualTo(9));
            Assert.That(statistics.RoutesAvailable, Is.EqualTo(10));
            Assert.That(statistics.RedirectsApplied, Is.EqualTo(11));
            Assert.That(statistics.SessionsActive, Is.EqualTo(12));
            Assert.That(statistics.AccountingRecordsSent, Is.EqualTo(13));
            Assert.That(statistics.CreditControlAnswers, Is.EqualTo(14));
            Assert.That(statistics.LoadReportsReceived, Is.EqualTo(15));
            Assert.That(statistics.OverloadRejections, Is.EqualTo(16));
            Assert.That(statistics.QueueDepth, Is.EqualTo(17));
            Assert.That(statistics.StoreFailures, Is.EqualTo(18));
        });
    }

    [Test]
    public void NodeHealthStartsWithOwnedEmptyCollections()
    {
        DiameterNodeHealth first = new DiameterNodeHealth();
        DiameterNodeHealth second = new DiameterNodeHealth();
        DiameterSubsystemHealth subsystem = new DiameterSubsystemHealth();
        DiameterOperationalEvent failure = new DiameterOperationalEvent();

        first.Subsystems.Add(subsystem);
        first.RecentFailures.Add(failure);

        Assert.Multiple(() =>
        {
            Assert.That(first.Subsystems, Has.Count.EqualTo(1));
            Assert.That(first.RecentFailures, Has.Count.EqualTo(1));
            Assert.That(second.Subsystems, Is.Empty);
            Assert.That(second.RecentFailures, Is.Empty);
            Assert.That(first.Subsystems[0], Is.SameAs(subsystem));
            Assert.That(first.RecentFailures[0], Is.SameAs(failure));
        });
    }

    [Test]
    public void HealthFieldsRoundTripStatusAndDiagnostics()
    {
        DateTimeOffset checkedAt = new DateTimeOffset(
            2026, 8, 29, 12, 31, 0, TimeSpan.Zero);
        DiameterSubsystemHealth subsystem = new DiameterSubsystemHealth();
        subsystem.Name = "requests";
        subsystem.Status = DiameterHealthStatus.Degraded;
        subsystem.Detail = "queue pressure";
        subsystem.CheckedAt = checkedAt;
        subsystem.LastHealthyAt = checkedAt.AddMinutes(-1);
        subsystem.LastFailureAt = checkedAt.AddSeconds(-5);
        subsystem.FailureCount = 2;
        subsystem.Required = true;
        subsystem.PendingWork = 40;
        subsystem.UtilizationPercentage = 80;
        subsystem.TimerLagMilliseconds = 25;

        DiameterNodeHealth health = new DiameterNodeHealth();
        health.Status = DiameterHealthStatus.Degraded;
        health.CheckedAt = checkedAt;
        health.Summary = "request processing is degraded";
        health.AcceptingRequests = true;
        health.Draining = false;
        health.Subsystems.Add(subsystem);

        Assert.Multiple(() =>
        {
            Assert.That(health.Status, Is.EqualTo(DiameterHealthStatus.Degraded));
            Assert.That(health.CheckedAt, Is.EqualTo(checkedAt));
            Assert.That(health.Summary, Is.EqualTo("request processing is degraded"));
            Assert.That(health.AcceptingRequests, Is.True);
            Assert.That(health.Draining, Is.False);
            Assert.That(health.Subsystems[0].Name, Is.EqualTo("requests"));
            Assert.That(health.Subsystems[0].Detail, Is.EqualTo("queue pressure"));
            Assert.That(health.Subsystems[0].FailureCount, Is.EqualTo(2));
            Assert.That(health.Subsystems[0].Required, Is.True);
            Assert.That(health.Subsystems[0].PendingWork, Is.EqualTo(40));
            Assert.That(health.Subsystems[0].UtilizationPercentage, Is.EqualTo(80));
            Assert.That(health.Subsystems[0].TimerLagMilliseconds, Is.EqualTo(25));
        });
    }

    [Test]
    public void OperationalEventMetadataAndSnapshotsRoundTrip()
    {
        DateTimeOffset occurredAt = new DateTimeOffset(
            2026, 8, 29, 12, 32, 0, TimeSpan.Zero);
        DiameterIdentity peer = new DiameterIdentity();
        DiameterPeerEvent peerEvent = new DiameterPeerEvent();
        DiameterDisconnectRequest disconnect =
            new DiameterDisconnectRequest();
        DiameterDiscoveryResult discovery =
            new DiameterDiscoveryResult();
        DiameterDictionarySnapshot dictionary =
            new DiameterDictionarySnapshot();
        DiameterNodeStatistics statistics = new DiameterNodeStatistics();
        DiameterNodeHealth health = new DiameterNodeHealth();
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.SequenceNumber = 42;
        eventData.OccurredAt = occurredAt;
        eventData.Source = "peer-manager";
        eventData.Category = "peer";
        eventData.Code = "peer.disconnected";
        eventData.Description = "The peer connection closed.";
        eventData.CorrelationId = "request-7";
        eventData.Redacted = true;
        eventData.PeerIdentity = peer;
        eventData.PeerEvent = peerEvent;
        eventData.DisconnectRequest = disconnect;
        eventData.DiscoveryResult = discovery;
        eventData.DictionarySnapshot = dictionary;
        eventData.SessionId = "session-1";
        eventData.ApplicationId = 4;
        eventData.CommandCode = 272;
        eventData.ResultCode = 3002;
        eventData.Statistics = statistics;
        eventData.NodeHealth = health;

        Assert.Multiple(() =>
        {
            Assert.That(eventData.SequenceNumber, Is.EqualTo(42));
            Assert.That(eventData.OccurredAt, Is.EqualTo(occurredAt));
            Assert.That(eventData.Source, Is.EqualTo("peer-manager"));
            Assert.That(eventData.Category, Is.EqualTo("peer"));
            Assert.That(eventData.Code, Is.EqualTo("peer.disconnected"));
            Assert.That(eventData.Description, Is.EqualTo("The peer connection closed."));
            Assert.That(eventData.CorrelationId, Is.EqualTo("request-7"));
            Assert.That(eventData.Redacted, Is.True);
            Assert.That(eventData.PeerIdentity, Is.SameAs(peer));
            Assert.That(eventData.PeerEvent, Is.SameAs(peerEvent));
            Assert.That(eventData.DisconnectRequest,
                Is.SameAs(disconnect));
            Assert.That(eventData.DiscoveryResult,
                Is.SameAs(discovery));
            Assert.That(eventData.DictionarySnapshot,
                Is.SameAs(dictionary));
            Assert.That(eventData.SessionId, Is.EqualTo("session-1"));
            Assert.That(eventData.ApplicationId, Is.EqualTo(4));
            Assert.That(eventData.CommandCode, Is.EqualTo(272));
            Assert.That(eventData.ResultCode, Is.EqualTo(3002));
            Assert.That(eventData.Statistics, Is.SameAs(statistics));
            Assert.That(eventData.NodeHealth, Is.SameAs(health));
        });
    }

    [Test]
    public void EventAndRedactionContractsCarryStructuredEvents()
    {
        DiameterOperationalEvent source = new DiameterOperationalEvent();
        source.Description = "secret";
        OperationsRedactionPolicyProbe policy =
            new OperationsRedactionPolicyProbe();
        OperationsEventSinkProbe sink = new OperationsEventSinkProbe();

        DiameterOperationalEvent redacted = policy.RedactDiameterEvent(source);
        sink.HandleDiameterEvent(redacted);

        Assert.Multiple(() =>
        {
            Assert.That(source.Description, Is.EqualTo("secret"));
            Assert.That(redacted, Is.Not.SameAs(source));
            Assert.That(redacted.Description, Is.EqualTo("[redacted]"));
            Assert.That(redacted.Redacted, Is.True);
            Assert.That(sink.LastEvent, Is.SameAs(redacted));
        });
    }

    [Test]
    public void OperationalEventDefaultsRepresentNoAssociatedEventData()
    {
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();

        Assert.Multiple(() =>
        {
            Assert.That(eventData.Configuration, Is.Null);
            Assert.That(eventData.PeerIdentity, Is.Null);
            Assert.That(eventData.PeerEvent, Is.Null);
            Assert.That(eventData.DisconnectRequest, Is.Null);
            Assert.That(eventData.DiscoveryResult, Is.Null);
            Assert.That(eventData.Message, Is.Null);
            Assert.That(eventData.Application, Is.Null);
            Assert.That(eventData.Uri, Is.Null);
            Assert.That(eventData.Endpoint, Is.Null);
            Assert.That(eventData.EncodeResult, Is.Null);
            Assert.That(eventData.DecodeResult, Is.Null);
            Assert.That(eventData.AvpDecodeResult, Is.Null);
            Assert.That(eventData.FramingResult, Is.Null);
            Assert.That(eventData.ValueResult, Is.Null);
            Assert.That(eventData.DictionarySnapshot, Is.Null);
            Assert.That(eventData.ValidationResult, Is.Null);
            Assert.That(eventData.Statistics, Is.Null);
            Assert.That(eventData.NodeHealth, Is.Null);
            Assert.That(eventData.MessageFlags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(eventData.AvpFlags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(eventData.AvpBaseType, Is.EqualTo(DiameterAvpBaseType.OctetString));
            Assert.That(eventData.AddressFamily, Is.EqualTo((DiameterAddressFamily)0));
            Assert.That(eventData.TransportKind, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(eventData.SecurityMode, Is.EqualTo(DiameterSecurityMode.None));
            Assert.That(eventData.NodeRole, Is.EqualTo(DiameterNodeRole.None));
            Assert.That(eventData.PeerState, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(eventData.PeerEventKind, Is.EqualTo(DiameterPeerEventKind.Added));
            Assert.That(eventData.ConnectionDirection, Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(eventData.DisconnectCause, Is.EqualTo(DiameterDisconnectCause.Rebooting));
            Assert.That(eventData.PeerSource, Is.EqualTo(DiameterPeerSource.Static));
            Assert.That(eventData.CapabilityChange, Is.EqualTo(DiameterCapabilityChange.None));
            Assert.That(eventData.RouteAction, Is.EqualTo(DiameterRouteAction.NoRoute));
            Assert.That(eventData.AgentRole, Is.EqualTo(DiameterAgentRole.None));
            Assert.That(eventData.RedirectHostUsage, Is.EqualTo(DiameterRedirectHostUsage.DoNotCache));
            Assert.That(eventData.RouteSource, Is.EqualTo(DiameterRouteSource.None));
            Assert.That(eventData.RoutingPriority, Is.EqualTo(DiameterRoutingPriority.Priority0));
            Assert.That(eventData.TranslationDisposition, Is.EqualTo(DiameterTranslationDisposition.Untranslated));
            Assert.That(eventData.SessionKind, Is.EqualTo(DiameterSessionKind.StatefulAuthorization));
            Assert.That(eventData.SessionRole, Is.EqualTo(DiameterSessionRole.Client));
            Assert.That(eventData.SessionState, Is.EqualTo(DiameterSessionState.Created));
            Assert.That(eventData.AuthRequestType, Is.EqualTo((DiameterAuthRequestType)0));
            Assert.That(eventData.AuthSessionState, Is.EqualTo(DiameterAuthSessionState.StateMaintained));
            Assert.That(eventData.ReAuthRequestType, Is.EqualTo(DiameterReAuthRequestType.AuthorizeOnly));
            Assert.That(eventData.TerminationCause, Is.EqualTo((DiameterTerminationCause)0));
            Assert.That(eventData.AccountingRecordType, Is.EqualTo((DiameterAccountingRecordType)0));
            Assert.That(eventData.AccountingRealtimeRequired, Is.EqualTo((DiameterAccountingRealtimeRequired)0));
            Assert.That(eventData.AccountingState, Is.EqualTo(DiameterAccountingState.Idle));
            Assert.That(eventData.CreditControlRequestType, Is.EqualTo((DiameterCreditControlRequestType)0));
            Assert.That(eventData.RequestedAction, Is.EqualTo(DiameterRequestedAction.DirectDebiting));
            Assert.That(eventData.CreditControlState, Is.EqualTo(DiameterCreditControlState.Idle));
            Assert.That(eventData.CreditControlFailureHandling, Is.EqualTo(DiameterCreditControlFailureHandling.Terminate));
            Assert.That(eventData.DirectDebitFailureHandling, Is.EqualTo(DiameterDirectDebitFailureHandling.TerminateOrBuffer));
            Assert.That(eventData.CreditControlUnitType, Is.EqualTo(DiameterCreditControlUnitType.Time));
            Assert.That(eventData.FinalUnitAction, Is.EqualTo(DiameterFinalUnitAction.Terminate));
            Assert.That(eventData.TariffChangeUsage, Is.EqualTo(DiameterTariffChangeUsage.UnitBeforeTariffChange));
            Assert.That(eventData.MultipleServicesIndicator, Is.EqualTo(DiameterMultipleServicesIndicator.NotSupported));
            Assert.That(eventData.CheckBalanceResult, Is.EqualTo(DiameterCheckBalanceResult.EnoughCredit));
            Assert.That(eventData.CreditControlDisposition, Is.EqualTo(DiameterCreditControlDisposition.Granted));
            Assert.That(eventData.SubscriptionIdType, Is.EqualTo(DiameterSubscriptionIdType.EndUserE164));
            Assert.That(eventData.UserEquipmentInfoType, Is.EqualTo(DiameterUserEquipmentInfoType.Imeisv));
            Assert.That(eventData.RedirectAddressType, Is.EqualTo(DiameterRedirectAddressType.IPv4));
            Assert.That(eventData.OverloadReportType, Is.EqualTo(DiameterOverloadReportType.Host));
            Assert.That(eventData.OverloadAlgorithm, Is.EqualTo(DiameterOverloadAlgorithm.None));
            Assert.That(eventData.OverloadScope, Is.EqualTo(DiameterOverloadScope.Host));
            Assert.That(eventData.AdmissionAction, Is.EqualTo(DiameterAdmissionAction.Admit));
            Assert.That(eventData.LoadSource, Is.EqualTo(DiameterLoadSource.Host));
            Assert.That(eventData.ResultClass, Is.EqualTo((DiameterResultClass)0));
            Assert.That(eventData.IssueSeverity, Is.EqualTo(DiameterIssueSeverity.Information));
            Assert.That(eventData.CodecStatus, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(eventData.ValidationStatus, Is.EqualTo(DiameterValidationStatus.Valid));
            Assert.That(eventData.SendStatus, Is.EqualTo(DiameterSendStatus.Accepted));
            Assert.That(eventData.HealthStatus, Is.EqualTo(DiameterHealthStatus.Healthy));
        });
    }

    [Test]
    public void OperationalEventReferenceFieldsRoundTrip()
    {
        DiameterNodeConfiguration configuration = new DiameterNodeConfiguration();
        DiameterMessage message = new DiameterMessage();
        DiameterApplicationId application = new DiameterApplicationId();
        DiameterUri uri = new DiameterUri();
        DiameterEndpoint endpoint = new DiameterEndpoint();
        DiameterEncodeResult encodeResult = new DiameterEncodeResult();
        DiameterDecodeResult decodeResult = new DiameterDecodeResult();
        DiameterAvpDecodeResult avpDecodeResult = new DiameterAvpDecodeResult();
        DiameterFramingResult framingResult = new DiameterFramingResult();
        DiameterValueResult valueResult = new DiameterValueResult();
        DiameterValidationResult validationResult = new DiameterValidationResult();

        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.Configuration = configuration;
        eventData.Message = message;
        eventData.Application = application;
        eventData.Uri = uri;
        eventData.Endpoint = endpoint;
        eventData.EncodeResult = encodeResult;
        eventData.DecodeResult = decodeResult;
        eventData.AvpDecodeResult = avpDecodeResult;
        eventData.FramingResult = framingResult;
        eventData.ValueResult = valueResult;
        eventData.ValidationResult = validationResult;

        Assert.Multiple(() =>
        {
            Assert.That(eventData.Configuration, Is.SameAs(configuration));
            Assert.That(eventData.Message, Is.SameAs(message));
            Assert.That(eventData.Application, Is.SameAs(application));
            Assert.That(eventData.Uri, Is.SameAs(uri));
            Assert.That(eventData.Endpoint, Is.SameAs(endpoint));
            Assert.That(eventData.EncodeResult, Is.SameAs(encodeResult));
            Assert.That(eventData.DecodeResult, Is.SameAs(decodeResult));
            Assert.That(eventData.AvpDecodeResult, Is.SameAs(avpDecodeResult));
            Assert.That(eventData.FramingResult, Is.SameAs(framingResult));
            Assert.That(eventData.ValueResult, Is.SameAs(valueResult));
            Assert.That(eventData.ValidationResult, Is.SameAs(validationResult));
        });
    }

    [Test]
    public void OperationalEventEnumFieldsRoundTrip()
    {
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.MessageFlags = DiameterMessageFlags.PotentiallyRetransmitted;
        eventData.AvpFlags = DiameterAvpFlags.Protected;
        eventData.AvpBaseType = DiameterAvpBaseType.Grouped;
        eventData.AddressFamily = DiameterAddressFamily.IPv6;
        eventData.TransportKind = DiameterTransportKind.External;
        eventData.SecurityMode = DiameterSecurityMode.External;
        eventData.NodeRole = DiameterNodeRole.Translation;
        eventData.PeerState = DiameterPeerState.Closing;
        eventData.PeerEventKind = DiameterPeerEventKind.TransportFailed;
        eventData.ConnectionDirection = DiameterConnectionDirection.Outbound;
        eventData.DisconnectCause = DiameterDisconnectCause.DoNotWantToTalkToYou;
        eventData.PeerSource = DiameterPeerSource.Dynamic;
        eventData.CapabilityChange = DiameterCapabilityChange.SecurityMechanisms;
        eventData.RouteAction = DiameterRouteAction.Reject;
        eventData.AgentRole = DiameterAgentRole.Translation;
        eventData.RedirectHostUsage = DiameterRedirectHostUsage.AllUser;
        eventData.RouteSource = DiameterRouteSource.SessionAffinity;
        eventData.RoutingPriority = DiameterRoutingPriority.Priority15;
        eventData.TranslationDisposition = DiameterTranslationDisposition.Error;
        eventData.SessionKind = DiameterSessionKind.StatelessAuthorization;
        eventData.SessionRole = DiameterSessionRole.Server;
        eventData.SessionState = DiameterSessionState.Failed;
        eventData.AuthRequestType = DiameterAuthRequestType.AuthorizeAuthenticate;
        eventData.AuthSessionState = DiameterAuthSessionState.NoStateMaintained;
        eventData.ReAuthRequestType = DiameterReAuthRequestType.AuthorizeAuthenticate;
        eventData.TerminationCause = DiameterTerminationCause.SessionTimeout;
        eventData.AccountingRecordType = DiameterAccountingRecordType.Stop;
        eventData.AccountingRealtimeRequired = DiameterAccountingRealtimeRequired.GrantAndLose;
        eventData.AccountingState = DiameterAccountingState.Failed;
        eventData.CreditControlRequestType = DiameterCreditControlRequestType.Event;
        eventData.RequestedAction = DiameterRequestedAction.PriceEnquiry;
        eventData.CreditControlState = DiameterCreditControlState.Failed;
        eventData.CreditControlFailureHandling = DiameterCreditControlFailureHandling.RetryAndTerminate;
        eventData.DirectDebitFailureHandling = DiameterDirectDebitFailureHandling.Continue;
        eventData.CreditControlUnitType = DiameterCreditControlUnitType.ServiceSpecificUnits;
        eventData.FinalUnitAction = DiameterFinalUnitAction.RestrictAccess;
        eventData.TariffChangeUsage = DiameterTariffChangeUsage.UnitIndeterminate;
        eventData.MultipleServicesIndicator = DiameterMultipleServicesIndicator.Supported;
        eventData.CheckBalanceResult = DiameterCheckBalanceResult.NoCredit;
        eventData.CreditControlDisposition = DiameterCreditControlDisposition.Failed;
        eventData.SubscriptionIdType = DiameterSubscriptionIdType.EndUserPrivate;
        eventData.UserEquipmentInfoType = DiameterUserEquipmentInfoType.ModifiedEui64;
        eventData.RedirectAddressType = DiameterRedirectAddressType.SipUri;
        eventData.OverloadReportType = DiameterOverloadReportType.Peer;
        eventData.OverloadAlgorithm = DiameterOverloadAlgorithm.Rate;
        eventData.OverloadScope = DiameterOverloadScope.Application;
        eventData.AdmissionAction = DiameterAdmissionAction.Divert;
        eventData.LoadSource = DiameterLoadSource.Peer;
        eventData.ResultClass = DiameterResultClass.PermanentFailure;
        eventData.IssueSeverity = DiameterIssueSeverity.Error;
        eventData.CodecStatus = DiameterCodecStatus.LimitExceeded;
        eventData.ValidationStatus = DiameterValidationStatus.Invalid;
        eventData.SendStatus = DiameterSendStatus.Failed;
        eventData.HealthStatus = DiameterHealthStatus.Stopped;

        Assert.Multiple(() =>
        {
            Assert.That(eventData.MessageFlags, Is.EqualTo(DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That(eventData.AvpFlags, Is.EqualTo(DiameterAvpFlags.Protected));
            Assert.That(eventData.AvpBaseType, Is.EqualTo(DiameterAvpBaseType.Grouped));
            Assert.That(eventData.AddressFamily, Is.EqualTo(DiameterAddressFamily.IPv6));
            Assert.That(eventData.TransportKind, Is.EqualTo(DiameterTransportKind.External));
            Assert.That(eventData.SecurityMode, Is.EqualTo(DiameterSecurityMode.External));
            Assert.That(eventData.NodeRole, Is.EqualTo(DiameterNodeRole.Translation));
            Assert.That(eventData.PeerState, Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(eventData.PeerEventKind, Is.EqualTo(DiameterPeerEventKind.TransportFailed));
            Assert.That(eventData.ConnectionDirection, Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(eventData.DisconnectCause, Is.EqualTo(DiameterDisconnectCause.DoNotWantToTalkToYou));
            Assert.That(eventData.PeerSource, Is.EqualTo(DiameterPeerSource.Dynamic));
            Assert.That(eventData.CapabilityChange, Is.EqualTo(DiameterCapabilityChange.SecurityMechanisms));
            Assert.That(eventData.RouteAction, Is.EqualTo(DiameterRouteAction.Reject));
            Assert.That(eventData.AgentRole, Is.EqualTo(DiameterAgentRole.Translation));
            Assert.That(eventData.RedirectHostUsage, Is.EqualTo(DiameterRedirectHostUsage.AllUser));
            Assert.That(eventData.RouteSource, Is.EqualTo(DiameterRouteSource.SessionAffinity));
            Assert.That(eventData.RoutingPriority, Is.EqualTo(DiameterRoutingPriority.Priority15));
            Assert.That(eventData.TranslationDisposition, Is.EqualTo(DiameterTranslationDisposition.Error));
            Assert.That(eventData.SessionKind, Is.EqualTo(DiameterSessionKind.StatelessAuthorization));
            Assert.That(eventData.SessionRole, Is.EqualTo(DiameterSessionRole.Server));
            Assert.That(eventData.SessionState, Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(eventData.AuthRequestType, Is.EqualTo(DiameterAuthRequestType.AuthorizeAuthenticate));
            Assert.That(eventData.AuthSessionState, Is.EqualTo(DiameterAuthSessionState.NoStateMaintained));
            Assert.That(eventData.ReAuthRequestType, Is.EqualTo(DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(eventData.TerminationCause, Is.EqualTo(DiameterTerminationCause.SessionTimeout));
            Assert.That(eventData.AccountingRecordType, Is.EqualTo(DiameterAccountingRecordType.Stop));
            Assert.That(eventData.AccountingRealtimeRequired, Is.EqualTo(DiameterAccountingRealtimeRequired.GrantAndLose));
            Assert.That(eventData.AccountingState, Is.EqualTo(DiameterAccountingState.Failed));
            Assert.That(eventData.CreditControlRequestType, Is.EqualTo(DiameterCreditControlRequestType.Event));
            Assert.That(eventData.RequestedAction, Is.EqualTo(DiameterRequestedAction.PriceEnquiry));
            Assert.That(eventData.CreditControlState, Is.EqualTo(DiameterCreditControlState.Failed));
            Assert.That(eventData.CreditControlFailureHandling, Is.EqualTo(DiameterCreditControlFailureHandling.RetryAndTerminate));
            Assert.That(eventData.DirectDebitFailureHandling, Is.EqualTo(DiameterDirectDebitFailureHandling.Continue));
            Assert.That(eventData.CreditControlUnitType, Is.EqualTo(DiameterCreditControlUnitType.ServiceSpecificUnits));
            Assert.That(eventData.FinalUnitAction, Is.EqualTo(DiameterFinalUnitAction.RestrictAccess));
            Assert.That(eventData.TariffChangeUsage, Is.EqualTo(DiameterTariffChangeUsage.UnitIndeterminate));
            Assert.That(eventData.MultipleServicesIndicator, Is.EqualTo(DiameterMultipleServicesIndicator.Supported));
            Assert.That(eventData.CheckBalanceResult, Is.EqualTo(DiameterCheckBalanceResult.NoCredit));
            Assert.That(eventData.CreditControlDisposition, Is.EqualTo(DiameterCreditControlDisposition.Failed));
            Assert.That(eventData.SubscriptionIdType, Is.EqualTo(DiameterSubscriptionIdType.EndUserPrivate));
            Assert.That(eventData.UserEquipmentInfoType, Is.EqualTo(DiameterUserEquipmentInfoType.ModifiedEui64));
            Assert.That(eventData.RedirectAddressType, Is.EqualTo(DiameterRedirectAddressType.SipUri));
            Assert.That(eventData.OverloadReportType, Is.EqualTo(DiameterOverloadReportType.Peer));
            Assert.That(eventData.OverloadAlgorithm, Is.EqualTo(DiameterOverloadAlgorithm.Rate));
            Assert.That(eventData.OverloadScope, Is.EqualTo(DiameterOverloadScope.Application));
            Assert.That(eventData.AdmissionAction, Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(eventData.LoadSource, Is.EqualTo(DiameterLoadSource.Peer));
            Assert.That(eventData.ResultClass, Is.EqualTo(DiameterResultClass.PermanentFailure));
            Assert.That(eventData.IssueSeverity, Is.EqualTo(DiameterIssueSeverity.Error));
            Assert.That(eventData.CodecStatus, Is.EqualTo(DiameterCodecStatus.LimitExceeded));
            Assert.That(eventData.ValidationStatus, Is.EqualTo(DiameterValidationStatus.Invalid));
            Assert.That(eventData.SendStatus, Is.EqualTo(DiameterSendStatus.Failed));
            Assert.That(eventData.HealthStatus, Is.EqualTo(DiameterHealthStatus.Stopped));
        });
    }
}

internal class OperationsEventSinkProbe : IDiameterEventSink
{
    internal DiameterOperationalEvent? LastEvent;

    public void HandleDiameterEvent(DiameterOperationalEvent eventData)
    {
        LastEvent = eventData;
    }
}

internal class OperationsRedactionPolicyProbe : IRedactionPolicy
{
    public DiameterOperationalEvent RedactDiameterEvent(
        DiameterOperationalEvent eventData)
    {
        DiameterOperationalEvent redacted = new DiameterOperationalEvent();
        redacted.Description = "[redacted]";
        redacted.Redacted = true;
        return redacted;
    }
}
