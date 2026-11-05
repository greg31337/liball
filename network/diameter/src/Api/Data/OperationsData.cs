using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Contains the outcome of one Diameter node lifecycle operation.</summary>
public class DiameterNodeLifecycleResult
{
    /// <summary>The lifecycle state after the operation completed.</summary>
    public DiameterNodeState State;

    /// <summary>Indicates that the requested lifecycle outcome was reached.</summary>
    public bool Succeeded;

    /// <summary>Indicates that the requested outcome had already been reached.</summary>
    public bool Repeated;

    /// <summary>Indicates that shutdown forced accepted work to end.</summary>
    public bool Forced;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains a point-in-time snapshot of Diameter node activity.
/// </summary>
/// <remarks>
/// Counter fields are cumulative for the node lifetime. Active and queued
/// fields are gauges captured at <c>CapturedAt</c>.
/// </remarks>
public class DiameterNodeStatistics
{
    /// <summary>The time at which the snapshot was captured.</summary>
    public DateTimeOffset CapturedAt;

    /// <summary>The number of bytes received from transports.</summary>
    public long BytesReceived;

    /// <summary>The number of bytes sent to transports.</summary>
    public long BytesSent;

    /// <summary>The number of Diameter messages received.</summary>
    public long MessagesReceived;

    /// <summary>The number of Diameter messages sent.</summary>
    public long MessagesSent;

    /// <summary>The number of AVPs decoded.</summary>
    public long AvpsDecoded;

    /// <summary>The number of AVPs encoded.</summary>
    public long AvpsEncoded;

    /// <summary>The number of message or AVP decoding failures.</summary>
    public long DecodeErrors;

    /// <summary>The number of validation failures.</summary>
    public long ValidationErrors;

    /// <summary>The number of configured peers.</summary>
    public long PeersConfigured;

    /// <summary>The number of currently connected peers.</summary>
    public long PeersConnected;

    /// <summary>The number of peer connections opened.</summary>
    public long PeerConnectionsOpened;

    /// <summary>The number of peer connections closed.</summary>
    public long PeerConnectionsClosed;

    /// <summary>The number of watchdog requests sent.</summary>
    public long WatchdogsSent;

    /// <summary>The number of watchdog answers received.</summary>
    public long WatchdogAnswersReceived;

    /// <summary>The number of watchdog operations that timed out.</summary>
    public long WatchdogTimeouts;

    /// <summary>The number of application requests received.</summary>
    public long RequestsReceived;

    /// <summary>The number of application requests sent.</summary>
    public long RequestsSent;

    /// <summary>The number of application answers received.</summary>
    public long AnswersReceived;

    /// <summary>The number of application answers sent.</summary>
    public long AnswersSent;

    /// <summary>The number of currently pending requests.</summary>
    public long RequestsPending;

    /// <summary>The number of requests that timed out.</summary>
    public long RequestTimeouts;

    /// <summary>The number of requests retransmitted.</summary>
    public long Retransmissions;

    /// <summary>The number of duplicate requests detected.</summary>
    public long DuplicateRequests;

    /// <summary>The number of routes currently available.</summary>
    public long RoutesAvailable;

    /// <summary>The number of successful route selections.</summary>
    public long RouteSelections;

    /// <summary>The number of route selection failures.</summary>
    public long RouteFailures;

    /// <summary>The number of redirect answers received.</summary>
    public long RedirectsReceived;

    /// <summary>The number of cached or received redirects applied.</summary>
    public long RedirectsApplied;

    /// <summary>The number of currently active sessions.</summary>
    public long SessionsActive;

    /// <summary>The number of sessions started.</summary>
    public long SessionsStarted;

    /// <summary>The number of sessions ended.</summary>
    public long SessionsEnded;

    /// <summary>The number of accounting records sent.</summary>
    public long AccountingRecordsSent;

    /// <summary>The number of accounting records received.</summary>
    public long AccountingRecordsReceived;

    /// <summary>The number of failed accounting operations.</summary>
    public long AccountingFailures;

    /// <summary>The number of credit-control requests processed.</summary>
    public long CreditControlRequests;

    /// <summary>The number of credit-control answers processed.</summary>
    public long CreditControlAnswers;

    /// <summary>The number of failed credit-control operations.</summary>
    public long CreditControlFailures;

    /// <summary>The number of load reports sent.</summary>
    public long LoadReportsSent;

    /// <summary>The number of load reports received.</summary>
    public long LoadReportsReceived;

    /// <summary>The number of overload reports sent.</summary>
    public long OverloadReportsSent;

    /// <summary>The number of overload reports received.</summary>
    public long OverloadReportsReceived;

    /// <summary>The number of requests rejected by overload control.</summary>
    public long OverloadRejections;

    /// <summary>The combined depth of bounded work queues.</summary>
    public long QueueDepth;

    /// <summary>The number of operations rejected by full queues.</summary>
    public long QueueRejections;

    /// <summary>The number of durable store reads.</summary>
    public long StoreReads;

    /// <summary>The number of durable store writes.</summary>
    public long StoreWrites;

    /// <summary>The number of durable store failures.</summary>
    public long StoreFailures;
}

/// <summary>
/// Contains the health of one named node subsystem.
/// </summary>
public class DiameterSubsystemHealth
{
    /// <summary>The stable subsystem name.</summary>
    public string Name = string.Empty;

    /// <summary>The subsystem health status.</summary>
    public DiameterHealthStatus Status;

    /// <summary>A human-readable explanation of the current status.</summary>
    public string Detail = string.Empty;

    /// <summary>The time at which the subsystem was checked.</summary>
    public DateTimeOffset CheckedAt;

    /// <summary>The last time at which the subsystem was healthy.</summary>
    public DateTimeOffset LastHealthyAt;

    /// <summary>The last time at which the subsystem failed.</summary>
    public DateTimeOffset LastFailureAt;

    /// <summary>The number of failures observed during the node lifetime.</summary>
    public long FailureCount;

    /// <summary>Indicates whether node health depends on this subsystem.</summary>
    public bool Required;

    /// <summary>The current queued or pending work associated with the subsystem.</summary>
    public long PendingWork;

    /// <summary>The subsystem utilization percentage from zero through one hundred.</summary>
    public int UtilizationPercentage;

    /// <summary>The observed timer lag in milliseconds.</summary>
    public long TimerLagMilliseconds;
}

/// <summary>
/// Contains a point-in-time aggregate health snapshot for a Diameter node.
/// </summary>
public class DiameterNodeHealth
{
    /// <summary>The aggregate node health status.</summary>
    public DiameterHealthStatus Status;

    /// <summary>The time at which the health snapshot was captured.</summary>
    public DateTimeOffset CheckedAt;

    /// <summary>A human-readable summary of the aggregate status.</summary>
    public string Summary = string.Empty;

    /// <summary>Indicates whether the node is accepting new application work.</summary>
    public bool AcceptingRequests;

    /// <summary>Indicates whether the node is draining accepted work.</summary>
    public bool Draining;

    /// <summary>The owned subsystem health snapshots.</summary>
    public List<DiameterSubsystemHealth> Subsystems =
        new List<DiameterSubsystemHealth>();

    /// <summary>The owned recent operational failures in occurrence order.</summary>
    public List<DiameterOperationalEvent> RecentFailures =
        new List<DiameterOperationalEvent>();
}

/// <summary>
/// Contains typed protocol and subsystem details for an operational event.
/// </summary>
/// <remarks>
/// A producer populates the fields relevant to the reported event. Fields that
/// do not apply retain their default values.
/// </remarks>
public class DiameterOperationalEvent
{
    /// <summary>The source-local sequence number used to order events.</summary>
    public long SequenceNumber;

    /// <summary>The time at which the event occurred.</summary>
    public DateTimeOffset OccurredAt;

    /// <summary>The stable name of the component that produced the event.</summary>
    public string Source = string.Empty;

    /// <summary>The stable event category.</summary>
    public string Category = string.Empty;

    /// <summary>The stable machine-readable event code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable event description.</summary>
    public string Description = string.Empty;

    /// <summary>An optional request, session, or host correlation value.</summary>
    public string CorrelationId = string.Empty;

    /// <summary>Indicates that sensitive payload data has been removed or replaced.</summary>
    public bool Redacted;

    /// <summary>Contains the peer identity associated with the event.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>Contains the session identifier associated with the event.</summary>
    public string? SessionId;

    /// <summary>Contains the related Diameter application identifier value.</summary>
    public uint ApplicationId;

    /// <summary>Contains the related Diameter command code.</summary>
    public uint CommandCode;

    /// <summary>Contains the related Diameter result code.</summary>
    public uint ResultCode;

    /// <summary>Contains the related peer event.</summary>
    public DiameterPeerEvent? PeerEvent;

    /// <summary>Contains the related peer disconnect request.</summary>
    public DiameterDisconnectRequest? DisconnectRequest;

    /// <summary>Contains the related peer discovery result.</summary>
    public DiameterDiscoveryResult? DiscoveryResult;

    /// <summary>Contains the related node configuration.</summary>
    public DiameterNodeConfiguration? Configuration;

    /// <summary>Contains the related Diameter message.</summary>
    public DiameterMessage? Message;

    /// <summary>Contains the related application identifier.</summary>
    public DiameterApplicationId? Application;

    /// <summary>Contains the related Diameter URI.</summary>
    public DiameterUri? Uri;

    /// <summary>Contains the related transport endpoint.</summary>
    public DiameterEndpoint? Endpoint;

    /// <summary>Contains the related encoding result.</summary>
    public DiameterEncodeResult? EncodeResult;

    /// <summary>Contains the related message decoding result.</summary>
    public DiameterDecodeResult? DecodeResult;

    /// <summary>Contains the related AVP decoding result.</summary>
    public DiameterAvpDecodeResult? AvpDecodeResult;

    /// <summary>Contains the related framing result.</summary>
    public DiameterFramingResult? FramingResult;

    /// <summary>Contains the related AVP value result.</summary>
    public DiameterValueResult? ValueResult;

    /// <summary>Contains the related composed dictionary snapshot.</summary>
    public DiameterDictionarySnapshot? DictionarySnapshot;

    /// <summary>Contains the related Diameter validation result.</summary>
    public DiameterValidationResult? ValidationResult;

    /// <summary>Contains the related node statistics snapshot.</summary>
    public DiameterNodeStatistics? Statistics;

    /// <summary>Contains the related node health snapshot.</summary>
    public DiameterNodeHealth? NodeHealth;

    /// <summary>Contains the related Diameter message flags.</summary>
    public DiameterMessageFlags MessageFlags;

    /// <summary>Contains the related Diameter AVP flags.</summary>
    public DiameterAvpFlags AvpFlags;

    /// <summary>Contains the related Diameter AVP base type.</summary>
    public DiameterAvpBaseType AvpBaseType;

    /// <summary>Contains the related network address family.</summary>
    public DiameterAddressFamily AddressFamily;

    /// <summary>Contains the related transport kind.</summary>
    public DiameterTransportKind TransportKind;

    /// <summary>Contains the related transport security mode.</summary>
    public DiameterSecurityMode SecurityMode;

    /// <summary>Contains the related node roles.</summary>
    public DiameterNodeRole NodeRole;

    /// <summary>Contains the related peer state.</summary>
    public DiameterPeerState PeerState;

    /// <summary>Contains the related peer event kind.</summary>
    public DiameterPeerEventKind PeerEventKind;

    /// <summary>Contains the related connection direction.</summary>
    public DiameterConnectionDirection ConnectionDirection;

    /// <summary>Contains the related disconnect cause.</summary>
    public DiameterDisconnectCause DisconnectCause;

    /// <summary>Contains the related peer source.</summary>
    public DiameterPeerSource PeerSource;

    /// <summary>Contains the related capability changes.</summary>
    public DiameterCapabilityChange CapabilityChange;

    /// <summary>Contains the related route action.</summary>
    public DiameterRouteAction RouteAction;

    /// <summary>Contains the related agent role.</summary>
    public DiameterAgentRole AgentRole;

    /// <summary>Contains the related redirect-host usage.</summary>
    public DiameterRedirectHostUsage RedirectHostUsage;

    /// <summary>Contains the related route source.</summary>
    public DiameterRouteSource RouteSource;

    /// <summary>Contains the related routing priority.</summary>
    public DiameterRoutingPriority RoutingPriority;

    /// <summary>Contains the related translation disposition.</summary>
    public DiameterTranslationDisposition TranslationDisposition;

    /// <summary>Contains the related session kind.</summary>
    public DiameterSessionKind SessionKind;

    /// <summary>Contains the related session role.</summary>
    public DiameterSessionRole SessionRole;

    /// <summary>Contains the related session state.</summary>
    public DiameterSessionState SessionState;

    /// <summary>Contains the related authentication request type.</summary>
    public DiameterAuthRequestType AuthRequestType;

    /// <summary>Contains the related authentication session state.</summary>
    public DiameterAuthSessionState AuthSessionState;

    /// <summary>Contains the related re-authorization request type.</summary>
    public DiameterReAuthRequestType ReAuthRequestType;

    /// <summary>Contains the related session termination cause.</summary>
    public DiameterTerminationCause TerminationCause;

    /// <summary>Contains the related accounting record type.</summary>
    public DiameterAccountingRecordType AccountingRecordType;

    /// <summary>Contains the related real-time accounting requirement.</summary>
    public DiameterAccountingRealtimeRequired AccountingRealtimeRequired;

    /// <summary>Contains the related accounting state.</summary>
    public DiameterAccountingState AccountingState;

    /// <summary>Contains the related credit-control request type.</summary>
    public DiameterCreditControlRequestType CreditControlRequestType;

    /// <summary>Contains the related requested action.</summary>
    public DiameterRequestedAction RequestedAction;

    /// <summary>Contains the related credit-control state.</summary>
    public DiameterCreditControlState CreditControlState;

    /// <summary>Contains the related credit-control failure handling.</summary>
    public DiameterCreditControlFailureHandling CreditControlFailureHandling;

    /// <summary>Contains the related direct-debit failure handling.</summary>
    public DiameterDirectDebitFailureHandling DirectDebitFailureHandling;

    /// <summary>Contains the related credit-control unit type.</summary>
    public DiameterCreditControlUnitType CreditControlUnitType;

    /// <summary>Contains the related final-unit action.</summary>
    public DiameterFinalUnitAction FinalUnitAction;

    /// <summary>Contains the related tariff-change usage.</summary>
    public DiameterTariffChangeUsage TariffChangeUsage;

    /// <summary>Contains the related multiple-services indicator.</summary>
    public DiameterMultipleServicesIndicator MultipleServicesIndicator;

    /// <summary>Contains the related balance-check result.</summary>
    public DiameterCheckBalanceResult CheckBalanceResult;

    /// <summary>Contains the related credit-control disposition.</summary>
    public DiameterCreditControlDisposition CreditControlDisposition;

    /// <summary>Contains the related subscription identifier type.</summary>
    public DiameterSubscriptionIdType SubscriptionIdType;

    /// <summary>Contains the related user-equipment information type.</summary>
    public DiameterUserEquipmentInfoType UserEquipmentInfoType;

    /// <summary>Contains the related redirect address type.</summary>
    public DiameterRedirectAddressType RedirectAddressType;

    /// <summary>Contains the related overload report type.</summary>
    public DiameterOverloadReportType OverloadReportType;

    /// <summary>Contains the related overload algorithm.</summary>
    public DiameterOverloadAlgorithm OverloadAlgorithm;

    /// <summary>Contains the related overload scope.</summary>
    public DiameterOverloadScope OverloadScope;

    /// <summary>Contains the related admission action.</summary>
    public DiameterAdmissionAction AdmissionAction;

    /// <summary>Contains the related load source.</summary>
    public DiameterLoadSource LoadSource;

    /// <summary>Contains the related Diameter result class.</summary>
    public DiameterResultClass ResultClass;

    /// <summary>Contains the related issue severity.</summary>
    public DiameterIssueSeverity IssueSeverity;

    /// <summary>Contains the related codec status.</summary>
    public DiameterCodecStatus CodecStatus;

    /// <summary>Contains the related validation status.</summary>
    public DiameterValidationStatus ValidationStatus;

    /// <summary>Contains the related send status.</summary>
    public DiameterSendStatus SendStatus;

    /// <summary>Contains the related health status.</summary>
    public DiameterHealthStatus HealthStatus;

    internal DiameterOperationalEvent CloneDiameterOperationalEvent()
    {
        return (DiameterOperationalEvent)MemberwiseClone();
    }
}
