using System.Collections.Generic;

namespace dns;

/// <summary>Holds one AXFR or IXFR client or server request.</summary>
public class DnsTransferRequest
{
    /// <summary>The requested zone-transfer protocol.</summary>
    public DnsTransferKind Kind;

    /// <summary>The zone to transfer.</summary>
    public DnsName Zone = new DnsName();

    /// <summary>The recognized zone class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw zone class integer, including unrecognized values.</summary>
    public int ClassValue = 1;

    /// <summary>Indicates that the requestor's current SOA serial is supplied.</summary>
    public bool HasCurrentSerial;

    /// <summary>The requestor's current SOA serial for IXFR.</summary>
    public uint CurrentSerial;

    /// <summary>The remote transfer endpoint.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The transfer transport, which is TCP by default.</summary>
    public DnsTransportKind Transport = DnsTransportKind.Tcp;

    /// <summary>The connection, timeout, and response-size policy.</summary>
    public DnsTransportOptions TransportOptions = new DnsTransportOptions();

    /// <summary>The optional complete transfer request message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional TSIG key used to authenticate the transfer.</summary>
    public DnsTsigKey? TsigKey;

    /// <summary>The authorization policy used when serving a transfer.</summary>
    public DnsAuthorizationPolicy Authorization = new DnsAuthorizationPolicy();

    /// <summary>The largest number of transfer messages processed.</summary>
    public int MaximumMessages = 100000;

    /// <summary>The largest total number of transferred resource records.</summary>
    public int MaximumRecords = 1000000;

    /// <summary>The largest total number of transferred wire octets.</summary>
    public long MaximumBytes = 1073741824;

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds one completed AXFR or IXFR transfer outcome.</summary>
public class DnsTransferResult
{
    /// <summary>Indicates that the transfer completed and validated successfully.</summary>
    public bool Success;

    /// <summary>The transfer protocol that produced the result.</summary>
    public DnsTransferKind Kind;

    /// <summary>The terminal transport state.</summary>
    public DnsTransportStatus TransportStatus;

    /// <summary>The recognized final DNS response code.</summary>
    public DnsResponseCode ResponseCode;

    /// <summary>The raw final response-code integer, including extended values.</summary>
    public int ResponseCodeValue;

    /// <summary>The transferred zone name.</summary>
    public DnsName? ZoneName;

    /// <summary>The optional complete transferred zone.</summary>
    public DnsZone? Zone;

    /// <summary>The ordered transfer messages.</summary>
    public List<DnsMessage> Messages = new List<DnsMessage>();

    /// <summary>The ordered incremental journal entries.</summary>
    public List<DnsZoneJournalEntry> JournalEntries = new List<DnsZoneJournalEntry>();

    /// <summary>Indicates that an IXFR request was already up to date.</summary>
    public bool UpToDate;

    /// <summary>Indicates that an IXFR request fell back to AXFR.</summary>
    public bool FellBackToAxfr;

    /// <summary>Indicates that an initial SOA serial is available.</summary>
    public bool HasInitialSerial;

    /// <summary>The SOA serial before applying the transfer.</summary>
    public uint InitialSerial;

    /// <summary>Indicates that a final SOA serial is available.</summary>
    public bool HasFinalSerial;

    /// <summary>The SOA serial after applying the transfer.</summary>
    public uint FinalSerial;

    /// <summary>The number of processed transfer messages.</summary>
    public int MessageCount;

    /// <summary>The number of processed resource records.</summary>
    public int RecordCount;

    /// <summary>The number of processed transfer wire octets.</summary>
    public long ByteCount;

    /// <summary>The final TSIG authentication and chain state.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>Diagnostics produced while receiving or serving the transfer.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds one outgoing or received DNS NOTIFY operation.</summary>
public class DnsNotifyRequest
{
    /// <summary>The notified zone name.</summary>
    public DnsName Zone = new DnsName();

    /// <summary>The recognized notified zone class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw notified zone class integer, including unrecognized values.</summary>
    public int ClassValue = 1;

    /// <summary>Indicates that the notifier's SOA serial is supplied.</summary>
    public bool HasSerial;

    /// <summary>The notifier's current SOA serial.</summary>
    public uint Serial;

    /// <summary>The remote primary or secondary endpoint.</summary>
    public DnsEndpoint Endpoint = new DnsEndpoint();

    /// <summary>The transport and retry policy.</summary>
    public DnsTransportOptions TransportOptions = new DnsTransportOptions();

    /// <summary>The optional complete NOTIFY message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional TSIG key used to authenticate the notification.</summary>
    public DnsTsigKey? TsigKey;

    /// <summary>The authorization policy used when processing a received notification.</summary>
    public DnsAuthorizationPolicy Authorization = new DnsAuthorizationPolicy();

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds a sent or processed DNS NOTIFY outcome.</summary>
public class DnsNotifyResult
{
    /// <summary>Indicates that notification processing completed successfully.</summary>
    public bool Success;

    /// <summary>Indicates that the notification was acknowledged.</summary>
    public bool Acknowledged;

    /// <summary>Indicates that the receiver should initiate a zone transfer.</summary>
    public bool TransferRequired;

    /// <summary>Indicates that a zone transfer was initiated.</summary>
    public bool TransferTriggered;

    /// <summary>The recognized NOTIFY response code.</summary>
    public DnsResponseCode ResponseCode;

    /// <summary>The raw NOTIFY response-code integer, including extended values.</summary>
    public int ResponseCodeValue;

    /// <summary>The optional received or generated response message.</summary>
    public DnsMessage? Response;

    /// <summary>The transport attempts made while sending the notification.</summary>
    public List<DnsTransportResult> Attempts = new List<DnsTransportResult>();

    /// <summary>The optional TSIG signing or authentication result.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>The optional transfer outcome triggered by the notification.</summary>
    public DnsTransferResult? Transfer;

    /// <summary>The elapsed notification duration in milliseconds.</summary>
    public long ElapsedMilliseconds;

    /// <summary>Diagnostics produced while sending or processing the notification.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds one RFC 2136 dynamic-update prerequisite.</summary>
public class DnsUpdatePrerequisite
{
    /// <summary>The prerequisite form.</summary>
    public DnsUpdatePrerequisiteKind Kind;

    /// <summary>The owner name tested by the prerequisite.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized prerequisite record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw prerequisite record-type integer, including unrecognized values.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized prerequisite class.</summary>
    public DnsClass Class;

    /// <summary>The raw prerequisite class integer, including unrecognized values.</summary>
    public int ClassValue;

    /// <summary>The records required by a value-dependent RRset prerequisite.</summary>
    public List<DnsResourceRecord> Records = new List<DnsResourceRecord>();
}

/// <summary>Holds one RFC 2136 dynamic-update mutation.</summary>
public class DnsUpdateChange
{
    /// <summary>The update mutation form.</summary>
    public DnsUpdateChangeKind Kind;

    /// <summary>The owner name changed by the update.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized changed record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw changed record-type integer, including unrecognized values.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized update record class.</summary>
    public DnsClass Class;

    /// <summary>The raw update record class integer, including unrecognized values.</summary>
    public int ClassValue;

    /// <summary>The TTL for records added by the change.</summary>
    public uint TimeToLive;

    /// <summary>The individual records added or deleted by the change.</summary>
    public List<DnsResourceRecord> Records = new List<DnsResourceRecord>();
}

/// <summary>Holds one complete RFC 2136 dynamic-update request.</summary>
public class DnsUpdateRequest
{
    /// <summary>The zone to update.</summary>
    public DnsName Zone = new DnsName();

    /// <summary>The remote client endpoint requesting the update.</summary>
    public DnsEndpoint ClientEndpoint = new DnsEndpoint();

    /// <summary>The recognized zone class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw zone class integer, including unrecognized values.</summary>
    public int ClassValue = 1;

    /// <summary>The ordered update prerequisites.</summary>
    public List<DnsUpdatePrerequisite> Prerequisites = new List<DnsUpdatePrerequisite>();

    /// <summary>The ordered update mutations.</summary>
    public List<DnsUpdateChange> Changes = new List<DnsUpdateChange>();

    /// <summary>The additional records carried by the update message.</summary>
    public List<DnsResourceRecord> AdditionalRecords = new List<DnsResourceRecord>();

    /// <summary>The optional complete UPDATE message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional TSIG key used to authenticate the update.</summary>
    public DnsTsigKey? TsigKey;

    /// <summary>The update authorization policy.</summary>
    public DnsAuthorizationPolicy Authorization = new DnsAuthorizationPolicy();

    /// <summary>Indicates that an expected SOA serial is supplied.</summary>
    public bool HasExpectedSerial;

    /// <summary>The SOA serial that must be current before applying the update.</summary>
    public uint ExpectedSerial;

    /// <summary>Indicates that the caller has requested cancellation.</summary>
    public bool CancellationRequested;
}

/// <summary>Holds an atomic dynamic-update outcome.</summary>
public class DnsUpdateResult
{
    /// <summary>Indicates that update processing completed without a protocol failure.</summary>
    public bool Success;

    /// <summary>Indicates that every requested mutation was committed.</summary>
    public bool Applied;

    /// <summary>The recognized UPDATE response code.</summary>
    public DnsResponseCode ResponseCode;

    /// <summary>The raw UPDATE response-code integer, including extended values.</summary>
    public int ResponseCodeValue;

    /// <summary>The optional generated UPDATE response message.</summary>
    public DnsMessage? Response;

    /// <summary>The optional atomic zone transaction.</summary>
    public DnsZoneTransaction? Transaction;

    /// <summary>The optional TSIG signing or authentication result.</summary>
    public DnsTsigResult? Tsig;

    /// <summary>Indicates that a resulting SOA serial is available.</summary>
    public bool HasResultingSerial;

    /// <summary>The SOA serial after a successful update.</summary>
    public uint ResultingSerial;

    /// <summary>Diagnostics produced while validating or applying the update.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}
