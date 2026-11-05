using System;

namespace diameter.Tests;

public class EnumContractTests
{
    [Test]
    public void ProtocolEnumsExposeStableValues()
    {
        AssertEnum<DiameterMessageFlags>(
            ("None", 0),
            ("Request", 128),
            ("Proxyable", 64),
            ("Error", 32),
            ("PotentiallyRetransmitted", 16));
        AssertEnum<DiameterAvpFlags>(
            ("None", 0),
            ("VendorSpecific", 128),
            ("Mandatory", 64),
            ("Protected", 32));
        AssertEnum<DiameterAvpBaseType>(
            ("OctetString", 0),
            ("Integer32", 1),
            ("Integer64", 2),
            ("Unsigned32", 3),
            ("Unsigned64", 4),
            ("Float32", 5),
            ("Float64", 6),
            ("Grouped", 7));
        AssertEnum<DiameterAddressFamily>(("IPv4", 1), ("IPv6", 2));
        AssertEnum<DiameterTransportKind>(("Tcp", 0), ("Sctp", 1), ("External", 2));
        AssertEnum<DiameterSecurityMode>(
            ("None", 0),
            ("Tls", 1),
            ("Dtls", 2),
            ("Ipsec", 3),
            ("External", 4));
        AssertEnum<DiameterNodeRole>(
            ("None", 0),
            ("Client", 1),
            ("Server", 2),
            ("Relay", 4),
            ("Proxy", 8),
            ("Redirect", 16),
            ("Translation", 32));
    }

    [Test]
    public void PeerEnumsExposeStableValues()
    {
        AssertEnum<DiameterPeerState>(
            ("Closed", 0),
            ("WaitConnectionAcknowledgement", 1),
            ("WaitInitiatorCapabilitiesAnswer", 2),
            ("WaitConnectionAcknowledgementElection", 3),
            ("WaitReturns", 4),
            ("ResponderOpen", 5),
            ("InitiatorOpen", 6),
            ("Closing", 7));
        AssertEnum<DiameterPeerWatchdogState>(
            ("Initial", 0),
            ("Okay", 1),
            ("Suspect", 2),
            ("Down", 3),
            ("Reopen", 4));
        AssertEnum<DiameterPeerEventKind>(
            ("Added", 0),
            ("Removed", 1),
            ("StateChanged", 2),
            ("CapabilitiesChanged", 3),
            ("RestartDetected", 4),
            ("TransportFailed", 5));
        AssertEnum<DiameterConnectionDirection>(("Inbound", 0), ("Outbound", 1));
        AssertEnum<DiameterDisconnectCause>(
            ("Rebooting", 0),
            ("Busy", 1),
            ("DoNotWantToTalkToYou", 2));
        AssertEnum<DiameterPeerSource>(("Static", 0), ("Dynamic", 1));
        AssertEnum<DiameterCapabilityChange>(
            ("None", 0),
            ("HostAddresses", 1),
            ("VendorInformation", 2),
            ("ProductName", 4),
            ("FirmwareRevision", 8),
            ("Applications", 16),
            ("SecurityMechanisms", 32));
    }

    [Test]
    public void RoutingEnumsExposeStableValues()
    {
        AssertEnum<DiameterRouteAction>(
            ("NoRoute", 0),
            ("Local", 1),
            ("Relay", 2),
            ("Proxy", 3),
            ("Redirect", 4),
            ("Translate", 5),
            ("Reject", 6));
        AssertEnum<DiameterAgentRole>(
            ("None", 0),
            ("Relay", 1),
            ("Proxy", 2),
            ("Redirect", 3),
            ("Translation", 4));
        AssertEnum<DiameterRedirectHostUsage>(
            ("DoNotCache", 0),
            ("AllSession", 1),
            ("AllRealm", 2),
            ("RealmAndApplication", 3),
            ("AllApplication", 4),
            ("AllHost", 5),
            ("AllUser", 6));
        AssertEnum<DiameterRouteSource>(
            ("None", 0),
            ("Static", 1),
            ("Redirect", 2),
            ("Dynamic", 3),
            ("Default", 4),
            ("SessionAffinity", 5));
        AssertEnum<DiameterRoutingPriority>(
            ("Priority0", 0),
            ("Priority1", 1),
            ("Priority2", 2),
            ("Priority3", 3),
            ("Priority4", 4),
            ("Priority5", 5),
            ("Priority6", 6),
            ("Priority7", 7),
            ("Priority8", 8),
            ("Priority9", 9),
            ("Priority10", 10),
            ("Priority11", 11),
            ("Priority12", 12),
            ("Priority13", 13),
            ("Priority14", 14),
            ("Priority15", 15));
        AssertEnum<DiameterTranslationDisposition>(
            ("Untranslated", 0),
            ("Translated", 1),
            ("Error", 2));
    }

    [Test]
    public void SessionEnumsExposeStableValues()
    {
        AssertEnum<DiameterSessionKind>(
            ("StatefulAuthorization", 0),
            ("StatelessAuthorization", 1));
        AssertEnum<DiameterSessionRole>(("Client", 0), ("Server", 1));
        AssertEnum<DiameterSessionState>(
            ("Created", 0),
            ("Pending", 1),
            ("Open", 2),
            ("Reauthorizing", 3),
            ("Terminating", 4),
            ("Aborting", 5),
            ("Closed", 6),
            ("Failed", 7));
        AssertEnum<DiameterAuthRequestType>(
            ("AuthenticateOnly", 1),
            ("AuthorizeOnly", 2),
            ("AuthorizeAuthenticate", 3));
        AssertEnum<DiameterAuthSessionState>(
            ("StateMaintained", 0),
            ("NoStateMaintained", 1));
        AssertEnum<DiameterReAuthRequestType>(
            ("AuthorizeOnly", 0),
            ("AuthorizeAuthenticate", 1));
        AssertEnum<DiameterTerminationCause>(
            ("Logout", 1),
            ("ServiceNotProvided", 2),
            ("BadAnswer", 3),
            ("Administrative", 4),
            ("LinkBroken", 5),
            ("AuthExpired", 6),
            ("UserMoved", 7),
            ("SessionTimeout", 8));
    }

    [Test]
    public void AccountingEnumsExposeStableValues()
    {
        AssertEnum<DiameterAccountingRecordType>(
            ("Event", 1),
            ("Start", 2),
            ("Interim", 3),
            ("Stop", 4));
        AssertEnum<DiameterAccountingRealtimeRequired>(
            ("DeliverAndGrant", 1),
            ("GrantAndStore", 2),
            ("GrantAndLose", 3));
        AssertEnum<DiameterAccountingState>(
            ("Idle", 0),
            ("PendingEvent", 1),
            ("Open", 2),
            ("PendingInterim", 3),
            ("PendingStop", 4),
            ("Closed", 5),
            ("Failed", 6));
    }

    [Test]
    public void CreditControlEnumsExposeStableValues()
    {
        AssertEnum<DiameterCreditControlRequestType>(
            ("Initial", 1),
            ("Update", 2),
            ("Termination", 3),
            ("Event", 4));
        AssertEnum<DiameterRequestedAction>(
            ("DirectDebiting", 0),
            ("RefundAccount", 1),
            ("CheckBalance", 2),
            ("PriceEnquiry", 3));
        AssertEnum<DiameterCreditControlState>(
            ("Idle", 0),
            ("PendingInitial", 1),
            ("Open", 2),
            ("PendingUpdate", 3),
            ("PendingTermination", 4),
            ("Closed", 5),
            ("Failed", 6));
        AssertEnum<DiameterCreditControlFailureHandling>(
            ("Terminate", 0),
            ("Continue", 1),
            ("RetryAndTerminate", 2));
        AssertEnum<DiameterDirectDebitFailureHandling>(
            ("TerminateOrBuffer", 0),
            ("Continue", 1));
        AssertEnum<DiameterCreditControlUnitType>(
            ("Time", 0),
            ("Money", 1),
            ("TotalOctets", 2),
            ("InputOctets", 3),
            ("OutputOctets", 4),
            ("ServiceSpecificUnits", 5));
        AssertEnum<DiameterFinalUnitAction>(
            ("Terminate", 0),
            ("Redirect", 1),
            ("RestrictAccess", 2));
        AssertEnum<DiameterTariffChangeUsage>(
            ("UnitBeforeTariffChange", 0),
            ("UnitAfterTariffChange", 1),
            ("UnitIndeterminate", 2));
        AssertEnum<DiameterMultipleServicesIndicator>(
            ("NotSupported", 0),
            ("Supported", 1));
        AssertEnum<DiameterCheckBalanceResult>(
            ("EnoughCredit", 0),
            ("NoCredit", 1));
        AssertEnum<DiameterCreditControlDisposition>(
            ("Granted", 0),
            ("PartiallyGranted", 1),
            ("Denied", 2),
            ("FinalUnit", 3),
            ("Failed", 4));
        AssertEnum<DiameterSubscriptionIdType>(
            ("EndUserE164", 0),
            ("EndUserImsi", 1),
            ("EndUserSipUri", 2),
            ("EndUserNai", 3),
            ("EndUserPrivate", 4));
        AssertEnum<DiameterUserEquipmentInfoType>(
            ("Imeisv", 0),
            ("Mac", 1),
            ("Eui64", 2),
            ("ModifiedEui64", 3));
        AssertEnum<DiameterRedirectAddressType>(
            ("IPv4", 0),
            ("IPv6", 1),
            ("Url", 2),
            ("SipUri", 3));
    }

    [Test]
    public void OverloadEnumsExposeStableValues()
    {
        AssertEnum<DiameterOverloadReportType>(("Host", 0), ("Realm", 1), ("Peer", 2));
        AssertEnum<DiameterOverloadAlgorithm>(("None", 0), ("Loss", 1), ("Rate", 4));
        AssertEnum<DiameterOverloadScope>(
            ("Host", 0),
            ("Realm", 1),
            ("Peer", 2),
            ("Application", 3));
        AssertEnum<DiameterAdmissionAction>(("Admit", 0), ("Throttle", 1), ("Divert", 2));
        AssertEnum<DiameterLoadSource>(("Host", 0), ("Peer", 1));
    }

    [Test]
    public void ResultAndHealthEnumsExposeStableValues()
    {
        AssertEnum<DiameterResultClass>(
            ("Informational", 1),
            ("Success", 2),
            ("ProtocolError", 3),
            ("TransientFailure", 4),
            ("PermanentFailure", 5));
        AssertEnum<DiameterIssueSeverity>(
            ("Information", 0),
            ("Warning", 1),
            ("Error", 2));
        AssertEnum<DiameterCodecStatus>(
            ("Success", 0),
            ("Incomplete", 1),
            ("Invalid", 2),
            ("DestinationTooSmall", 3),
            ("LimitExceeded", 4));
        AssertEnum<DiameterValidationStatus>(("Valid", 0), ("Invalid", 1));
        AssertEnum<DiameterSendStatus>(
            ("Accepted", 0),
            ("NoRoute", 1),
            ("PeerUnavailable", 2),
            ("QueueFull", 3),
            ("PendingLimitReached", 4),
            ("Overloaded", 5),
            ("Closed", 6),
            ("Failed", 7));
        AssertEnum<DiameterHealthStatus>(
            ("Healthy", 0),
            ("Degraded", 1),
            ("Unhealthy", 2),
            ("Stopped", 3));
    }

    private static void AssertEnum<TEnum>(params (string Name, int Value)[] expected)
        where TEnum : struct, Enum
    {
        Assert.That(Enum.GetNames<TEnum>(), Has.Length.EqualTo(expected.Length));

        foreach ((string name, int value) in expected)
        {
            Assert.That(Enum.GetName(typeof(TEnum), value), Is.EqualTo(name));
        }
    }
}
