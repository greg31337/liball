namespace dns;

/// <summary>Identifies a machine-readable DNS diagnostic.</summary>
public enum DnsDiagnosticCode
{
    /// <summary>Indicates that no diagnostic was produced.</summary>
    None = 0,

    /// <summary>Indicates that more input bytes were required.</summary>
    UnexpectedEnd = 1,

    /// <summary>Indicates disallowed trailing input.</summary>
    TrailingData = 2,

    /// <summary>Indicates an invalid encoded length.</summary>
    InvalidLength = 3,

    /// <summary>Indicates a value outside its permitted range.</summary>
    ValueOutOfRange = 4,

    /// <summary>Indicates that a configured resource limit was exceeded.</summary>
    ResourceLimitExceeded = 5,

    /// <summary>Indicates an invalid DNS label.</summary>
    InvalidLabel = 6,

    /// <summary>Indicates an expanded DNS name that is too long.</summary>
    NameTooLong = 7,

    /// <summary>Indicates a compression pointer outside the message.</summary>
    CompressionPointerOutOfRange = 8,

    /// <summary>Indicates a disallowed forward compression pointer.</summary>
    CompressionPointerForward = 9,

    /// <summary>Indicates a compression-pointer cycle.</summary>
    CompressionLoop = 10,

    /// <summary>Indicates that the compression-pointer hop limit was exceeded.</summary>
    CompressionHopLimitExceeded = 11,

    /// <summary>Indicates inconsistent DNS section counts.</summary>
    SectionCountMismatch = 12,

    /// <summary>Indicates inconsistent resource-data length.</summary>
    RecordDataLengthMismatch = 13,

    /// <summary>Indicates invalid resource-record data.</summary>
    InvalidRecordData = 14,

    /// <summary>Indicates an invalid DNSSEC type bitmap.</summary>
    InvalidTypeBitmap = 15,

    /// <summary>Indicates invalid Service Binding parameters.</summary>
    InvalidSvcbParameters = 16,

    /// <summary>Indicates invalid EDNS data.</summary>
    InvalidEdns = 17,

    /// <summary>Indicates invalid OPT placement or multiplicity.</summary>
    InvalidOptPlacement = 18,

    /// <summary>Indicates invalid TSIG placement.</summary>
    InvalidTsigPlacement = 19,

    /// <summary>Indicates an invalid DNS-over-TCP frame.</summary>
    InvalidTcpFrame = 20,

    /// <summary>Indicates an unsupported protocol version.</summary>
    UnsupportedVersion = 21,

    /// <summary>Indicates an unsupported cryptographic algorithm.</summary>
    UnsupportedAlgorithm = 22,

    /// <summary>Indicates invalid DNS presentation text.</summary>
    InvalidText = 23,

    /// <summary>Indicates invalid zone data.</summary>
    InvalidZone = 24,

    /// <summary>Indicates a protocol response that does not match its request.</summary>
    ResponseMismatch = 25
}

/// <summary>Identifies a DNS query transport.</summary>
public enum DnsTransportKind
{
    /// <summary>Identifies DNS over UDP.</summary>
    Udp = 0,

    /// <summary>Identifies DNS over TCP.</summary>
    Tcp = 1,

    /// <summary>Identifies DNS over TLS.</summary>
    Tls = 2,

    /// <summary>Identifies DNS over HTTPS.</summary>
    Https = 3,

    /// <summary>Identifies DNS over QUIC.</summary>
    Quic = 4
}

/// <summary>Identifies the outcome of a DNS transport operation.</summary>
public enum DnsTransportStatus
{
    /// <summary>Indicates that the exchange completed successfully.</summary>
    Success = 0,

    /// <summary>Indicates that the exchange timed out.</summary>
    TimedOut = 1,

    /// <summary>Indicates that the exchange was cancelled.</summary>
    Cancelled = 2,

    /// <summary>Indicates a network input/output failure.</summary>
    NetworkError = 3,

    /// <summary>Indicates an invalid DNS protocol response.</summary>
    ProtocolError = 4,

    /// <summary>Indicates transport authentication failure.</summary>
    AuthenticationFailed = 5,

    /// <summary>Indicates that the requested transport is unsupported.</summary>
    Unsupported = 6,

    /// <summary>Indicates that the response exceeded a configured size limit.</summary>
    ResponseTooLarge = 7,

    /// <summary>Indicates that the transport is closed.</summary>
    Closed = 8
}

/// <summary>Identifies the semantic kind of a DNS cache entry.</summary>
public enum DnsCacheEntryKind
{
    /// <summary>Identifies a positive answer.</summary>
    Positive = 0,

    /// <summary>Identifies a cached nonexistent name.</summary>
    NegativeName = 1,

    /// <summary>Identifies cached absence of the requested record type.</summary>
    NegativeData = 2,

    /// <summary>Identifies a bounded cached resolution failure.</summary>
    Failure = 3
}

/// <summary>Identifies the outcome of DNS resolution.</summary>
public enum DnsResolveStatus
{
    /// <summary>Indicates successful resolution.</summary>
    Success = 0,

    /// <summary>Indicates that the name exists without the requested data.</summary>
    NoData = 1,

    /// <summary>Indicates that the requested name does not exist.</summary>
    NameError = 2,

    /// <summary>Indicates that resolution timed out.</summary>
    TimedOut = 3,

    /// <summary>Indicates that resolution was cancelled.</summary>
    Cancelled = 4,

    /// <summary>Indicates that resolution was refused.</summary>
    Refused = 5,

    /// <summary>Indicates an upstream server failure.</summary>
    ServerFailure = 6,

    /// <summary>Indicates an invalid protocol response.</summary>
    ProtocolError = 7,

    /// <summary>Indicates an alias or referral loop.</summary>
    Loop = 8,

    /// <summary>Indicates that a configured resolution depth limit was reached.</summary>
    DepthLimitExceeded = 9,

    /// <summary>Indicates that the requested operation is unsupported.</summary>
    Unsupported = 10
}

/// <summary>Identifies the kind of answer produced by an authoritative server.</summary>
public enum DnsAuthoritativeAnswerKind
{
    /// <summary>Identifies a positive authoritative answer.</summary>
    Positive = 0,

    /// <summary>Identifies an answer containing an alias chain.</summary>
    Alias = 1,

    /// <summary>Identifies a delegation referral.</summary>
    Referral = 2,

    /// <summary>Identifies an authoritative no-data answer.</summary>
    NoData = 3,

    /// <summary>Identifies an authoritative nonexistent-name answer.</summary>
    NameError = 4,

    /// <summary>Identifies a policy refusal.</summary>
    Refused = 5,

    /// <summary>Identifies an unsupported operation.</summary>
    NotImplemented = 6,

    /// <summary>Identifies an authoritative processing failure.</summary>
    ServerFailure = 7
}

/// <summary>Identifies a mutation applied to zone storage.</summary>
public enum DnsZoneChangeKind
{
    /// <summary>Indicates that no zone mutation is specified.</summary>
    None = 0,

    /// <summary>Adds a resource record.</summary>
    Add = 1,

    /// <summary>Replaces a resource-record set.</summary>
    ReplaceRecordSet = 2,

    /// <summary>Deletes one resource record.</summary>
    DeleteRecord = 3,

    /// <summary>Deletes a resource-record set.</summary>
    DeleteRecordSet = 4,

    /// <summary>Deletes all records at a name.</summary>
    DeleteName = 5
}

/// <summary>Identifies a DNS zone-transfer protocol.</summary>
public enum DnsTransferKind
{
    /// <summary>Identifies a full AXFR transfer.</summary>
    Axfr = 0,

    /// <summary>Identifies an incremental IXFR transfer.</summary>
    Ixfr = 1
}

/// <summary>Identifies a dynamic-update prerequisite form.</summary>
public enum DnsUpdatePrerequisiteKind
{
    /// <summary>Requires that a name exists.</summary>
    NameInUse = 0,

    /// <summary>Requires that a name does not exist.</summary>
    NameNotInUse = 1,

    /// <summary>Requires that a resource-record set exists.</summary>
    RecordSetExists = 2,

    /// <summary>Requires that a resource-record set has the supplied values.</summary>
    RecordSetExistsWithValues = 3,

    /// <summary>Requires that a resource-record set does not exist.</summary>
    RecordSetDoesNotExist = 4
}

/// <summary>Identifies a dynamic-update change form.</summary>
public enum DnsUpdateChangeKind
{
    /// <summary>Adds a resource record.</summary>
    Add = 0,

    /// <summary>Deletes one resource record.</summary>
    DeleteRecord = 1,

    /// <summary>Deletes a complete resource-record set.</summary>
    DeleteRecordSet = 2,

    /// <summary>Deletes every resource-record set at a name.</summary>
    DeleteName = 3
}

/// <summary>Identifies the lifecycle or terminal state of a DNS server run.</summary>
public enum DnsServerStatus
{
    /// <summary>Indicates that the server is stopped.</summary>
    Stopped = 0,

    /// <summary>Indicates that the server is accepting requests.</summary>
    Running = 1,

    /// <summary>Indicates that the server stopped after cancellation.</summary>
    Cancelled = 2,

    /// <summary>Indicates that the server stopped because of a failure.</summary>
    Failed = 3
}
