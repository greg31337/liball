namespace dns;

/// <summary>Identifies a DNS class code.</summary>
public enum DnsClass
{
    /// <summary>Identifies the reserved class zero.</summary>
    Reserved = 0,

    /// <summary>Identifies the Internet class.</summary>
    Internet = 1,

    /// <summary>Identifies the Chaos class.</summary>
    Chaos = 3,

    /// <summary>Identifies the Hesiod class.</summary>
    Hesiod = 4,

    /// <summary>Identifies the dynamic-update NONE query class.</summary>
    None = 254,

    /// <summary>Identifies the ANY query class.</summary>
    Any = 255
}

/// <summary>Identifies a DNS operation code.</summary>
public enum DnsOpcode
{
    /// <summary>Identifies a standard query.</summary>
    Query = 0,

    /// <summary>Identifies the obsolete inverse-query operation.</summary>
    InverseQuery = 1,

    /// <summary>Identifies a server-status request.</summary>
    Status = 2,

    /// <summary>Identifies a zone-change notification.</summary>
    Notify = 4,

    /// <summary>Identifies a dynamic-update request.</summary>
    Update = 5,

    /// <summary>Identifies a DNS Stateful Operations message.</summary>
    StatefulOperations = 6
}

/// <summary>Identifies a DNS response code.</summary>
public enum DnsResponseCode
{
    /// <summary>Indicates that no error occurred.</summary>
    NoError = 0,

    /// <summary>Indicates a format error.</summary>
    FormatError = 1,

    /// <summary>Indicates a server failure.</summary>
    ServerFailure = 2,

    /// <summary>Indicates that the domain name does not exist.</summary>
    NameError = 3,

    /// <summary>Indicates that the requested operation is not implemented.</summary>
    NotImplemented = 4,

    /// <summary>Indicates that the query was refused.</summary>
    Refused = 5,

    /// <summary>Indicates that a name exists when it should not.</summary>
    NameExists = 6,

    /// <summary>Indicates that a resource-record set exists when it should not.</summary>
    RecordSetExists = 7,

    /// <summary>Indicates that a required resource-record set does not exist.</summary>
    RecordSetDoesNotExist = 8,

    /// <summary>Indicates that the server is not authoritative or not authorized.</summary>
    NotAuthoritative = 9,

    /// <summary>Indicates that a name is outside the specified zone.</summary>
    NotZone = 10,

    /// <summary>Indicates that a DNS Stateful Operations type is not implemented.</summary>
    StatefulOperationTypeNotImplemented = 11,

    /// <summary>Indicates an unsupported EDNS version.</summary>
    BadVersion = 16,

    /// <summary>Indicates that a TSIG key was not recognized.</summary>
    BadKey = 17,

    /// <summary>Indicates that a signature is outside its accepted time window.</summary>
    BadTime = 18,

    /// <summary>Indicates an invalid TKEY mode.</summary>
    BadMode = 19,

    /// <summary>Indicates a duplicate key name.</summary>
    BadName = 20,

    /// <summary>Indicates that an algorithm is not supported.</summary>
    BadAlgorithm = 21,

    /// <summary>Indicates invalid TSIG truncation.</summary>
    BadTruncation = 22,

    /// <summary>Indicates a bad or missing DNS server cookie.</summary>
    BadCookie = 23
}

/// <summary>Identifies a section of a conventional DNS message.</summary>
public enum DnsMessageSection
{
    /// <summary>Identifies the question section.</summary>
    Question = 0,

    /// <summary>Identifies the answer section.</summary>
    Answer = 1,

    /// <summary>Identifies the authority section.</summary>
    Authority = 2,

    /// <summary>Identifies the additional section.</summary>
    Additional = 3
}

/// <summary>Identifies a bit in the DNS message header flags field.</summary>
public enum DnsHeaderFlag
{
    /// <summary>Indicates that no header flag is set.</summary>
    None = 0,

    /// <summary>Indicates that the message is a response.</summary>
    Response = 32768,

    /// <summary>Indicates an authoritative answer.</summary>
    AuthoritativeAnswer = 1024,

    /// <summary>Indicates that the message was truncated.</summary>
    Truncated = 512,

    /// <summary>Requests recursive resolution.</summary>
    RecursionDesired = 256,

    /// <summary>Indicates that recursive resolution is available.</summary>
    RecursionAvailable = 128,

    /// <summary>Identifies the reserved DNS header bit.</summary>
    Reserved = 64,

    /// <summary>Indicates that DNSSEC data was authenticated.</summary>
    AuthenticData = 32,

    /// <summary>Disables DNSSEC checking by the receiving resolver.</summary>
    CheckingDisabled = 16
}

/// <summary>Identifies a bit in the EDNS flags field.</summary>
public enum DnsEdnsFlag
{
    /// <summary>Indicates that no EDNS flag is set.</summary>
    None = 0,

    /// <summary>Requests DNSSEC records in the response.</summary>
    DnssecAnswerOk = 32768,

    /// <summary>Indicates support for compact DNS answers.</summary>
    CompactAnswersOk = 16384,

    /// <summary>Indicates support for the temporary delegation-extensions flag.</summary>
    DelegationExtensions = 8192
}

/// <summary>Identifies an EDNS option code.</summary>
public enum DnsEdnsOptionCode
{
    /// <summary>Identifies the reserved option code zero.</summary>
    Reserved = 0,

    /// <summary>Identifies the Long-Lived Queries option.</summary>
    LongLivedQuery = 1,

    /// <summary>Identifies the Update Lease option.</summary>
    UpdateLease = 2,

    /// <summary>Identifies the Name Server Identifier option.</summary>
    NameServerIdentifier = 3,

    /// <summary>Identifies the DNSSEC Algorithm Understood option.</summary>
    DnssecAlgorithmUnderstood = 5,

    /// <summary>Identifies the DS Hash Understood option.</summary>
    DsHashUnderstood = 6,

    /// <summary>Identifies the NSEC3 Hash Understood option.</summary>
    Nsec3HashUnderstood = 7,

    /// <summary>Identifies the Client Subnet option.</summary>
    ClientSubnet = 8,

    /// <summary>Identifies the Expire option.</summary>
    Expire = 9,

    /// <summary>Identifies the DNS Cookie option.</summary>
    Cookie = 10,

    /// <summary>Identifies the TCP Keepalive option.</summary>
    TcpKeepalive = 11,

    /// <summary>Identifies the Padding option.</summary>
    Padding = 12,

    /// <summary>Identifies the CHAIN option.</summary>
    Chain = 13,

    /// <summary>Identifies the Key Tag option.</summary>
    KeyTag = 14,

    /// <summary>Identifies the Extended DNS Error option.</summary>
    ExtendedDnsError = 15,

    /// <summary>Identifies the Client Tag option.</summary>
    ClientTag = 16,

    /// <summary>Identifies the Server Tag option.</summary>
    ServerTag = 17,

    /// <summary>Identifies the Report Channel option.</summary>
    ReportChannel = 18,

    /// <summary>Identifies the Zone Version option.</summary>
    ZoneVersion = 19,

    /// <summary>Identifies the Multiple QTYPE Query option.</summary>
    MultipleQuestionTypeQuery = 20,

    /// <summary>Identifies the Multiple QTYPE Response option.</summary>
    MultipleQuestionTypeResponse = 21,

    /// <summary>Identifies the EDE extra-text language option.</summary>
    ExtendedErrorTextLanguage = 22,

    /// <summary>Identifies the filtering-contact option.</summary>
    FilteringContact = 23,

    /// <summary>Identifies the filtering-organization option.</summary>
    FilteringOrganization = 24,

    /// <summary>Identifies the filtering-database option.</summary>
    FilteringDatabase = 25,

    /// <summary>Identifies the Structured DNS Error option.</summary>
    StructuredDnsError = 26,

    /// <summary>Identifies the Cisco Umbrella identifier option.</summary>
    UmbrellaIdentifier = 20292,

    /// <summary>Identifies the Device Identifier option.</summary>
    DeviceIdentifier = 26946,

    /// <summary>Identifies the first locally assigned experimental option code.</summary>
    LocalExperimentalStart = 65001,

    /// <summary>Identifies the last locally assigned experimental option code.</summary>
    LocalExperimentalEnd = 65534,

    /// <summary>Identifies the option code reserved for future expansion.</summary>
    ReservedForFutureExpansion = 65535
}
