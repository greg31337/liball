using System.Collections.Generic;

namespace dns;

/// <summary>Holds a DNS name as case-preserving wire-label octets.</summary>
public class DnsName
{
    /// <summary>The ordered labels without length octets or a terminating root label.</summary>
    public List<byte[]> Labels = new List<byte[]>();

    /// <summary>Indicates that the name ends at the DNS root.</summary>
    public bool IsAbsolute;
}

/// <summary>Controls strict and bounded DNS decoding.</summary>
public class DnsDecodeOptions
{
    /// <summary>Requires every supplied input octet to belong to the decoded value.</summary>
    public bool RequireCompleteInput = true;

    /// <summary>Rejects nonzero reserved DNS header bits.</summary>
    public bool RejectReservedHeaderBits = true;

    /// <summary>Requests schema-aware decoding for recognized resource-record types.</summary>
    public bool DecodeTypedRecordData = true;

    /// <summary>Allows malformed recognized RDATA to be retained as opaque bytes.</summary>
    public bool FallBackToOpaqueRecordData;

    /// <summary>The largest accepted DNS message length in octets.</summary>
    public int MaximumMessageLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>The largest accepted count in any one DNS message section.</summary>
    public int MaximumSectionEntryCount = DnsProtocolLimits.MaximumSectionEntryCount;

    /// <summary>The largest total number of entries processed across all message sections.</summary>
    public int MaximumTotalSectionEntryCount = DnsProtocolLimits.MaximumSectionEntryCount;

    /// <summary>The largest accepted resource-data length in octets.</summary>
    public int MaximumRecordDataLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>The largest number of compression pointers followed while decoding one name.</summary>
    public int MaximumCompressionPointerHops =
        DnsProtocolLimits.DefaultCompressionPointerHopLimit;
}

/// <summary>Controls deterministic and bounded DNS encoding.</summary>
public class DnsEncodeOptions
{
    /// <summary>Enables DNS name compression where the record schema permits it.</summary>
    public bool EnableCompression = true;

    /// <summary>Requests canonical DNSSEC-compatible wire output.</summary>
    public bool Canonical;

    /// <summary>Derives all message section counts from their corresponding collections.</summary>
    public bool DeriveSectionCounts = true;

    /// <summary>Allows whole record sets to be removed when output exceeds the size limit.</summary>
    public bool TruncateToMaximumLength;

    /// <summary>Retains the question section when applying size truncation.</summary>
    public bool PreserveQuestionOnTruncation = true;

    /// <summary>The largest encoded message length in octets.</summary>
    public int MaximumMessageLength = DnsProtocolLimits.MaximumTcpMessageLength;

    /// <summary>The UDP payload limit used when truncation is enabled.</summary>
    public int MaximumUdpPayloadSize = DnsProtocolLimits.ClassicUdpPayloadSize;

    /// <summary>The canonical ordering and normalization policy.</summary>
    public DnsCanonicalOrderOptions CanonicalOrder = new DnsCanonicalOrderOptions();
}

/// <summary>Controls canonical DNS name and record ordering.</summary>
public class DnsCanonicalOrderOptions
{
    /// <summary>Converts ASCII letters in names to lowercase wire octets.</summary>
    public bool LowercaseNames = true;

    /// <summary>Disables name compression in canonical output.</summary>
    public bool DisableCompression = true;

    /// <summary>Sorts records by their canonical owner and RDATA ordering.</summary>
    public bool SortRecords = true;

    /// <summary>Normalizes record TTL values before comparison or grouping.</summary>
    public bool NormalizeRecordTtl;

    /// <summary>Suppresses byte-equivalent duplicate records.</summary>
    public bool SuppressDuplicateRecords = true;
}

/// <summary>Describes a DNS decoding or encoding problem without retaining caller buffers.</summary>
public class DnsDiagnostic
{
    /// <summary>The machine-readable diagnostic code.</summary>
    public DnsDiagnosticCode Code;

    /// <summary>Indicates that an input or output offset is available.</summary>
    public bool HasOffset;

    /// <summary>The zero-based message offset associated with the diagnostic.</summary>
    public int Offset;

    /// <summary>Indicates that a DNS message section is available.</summary>
    public bool HasSection;

    /// <summary>The DNS message section associated with the diagnostic.</summary>
    public DnsMessageSection Section;

    /// <summary>Indicates that a resource-record index is available.</summary>
    public bool HasRecordIndex;

    /// <summary>The zero-based record index within the associated section.</summary>
    public int RecordIndex;

    /// <summary>Indicates that an EDNS option index is available.</summary>
    public bool HasOptionIndex;

    /// <summary>The zero-based option index within the associated OPT record.</summary>
    public int OptionIndex;

    /// <summary>The optional field name associated with the diagnostic.</summary>
    public string? Field;

    /// <summary>The human-readable reason for the diagnostic.</summary>
    public string Reason = string.Empty;
}

/// <summary>Holds the exception-less result of a DNS decode operation.</summary>
public class DnsDecodeResult
{
    /// <summary>Indicates that the requested value decoded successfully.</summary>
    public bool Success;

    /// <summary>The number of input octets consumed by the decoded value.</summary>
    public int BytesConsumed;

    /// <summary>The optional decoded DNS name.</summary>
    public DnsName? Name;

    /// <summary>The optional decoded DNS question.</summary>
    public DnsQuestion? Question;

    /// <summary>The optional decoded resource record.</summary>
    public DnsResourceRecord? Record;

    /// <summary>The optional decoded resource-record set.</summary>
    public DnsResourceRecordSet? RecordSet;

    /// <summary>The optional decoded DNS message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional decoded DNS-over-TCP frame.</summary>
    public DnsTcpFrame? TcpFrame;

    /// <summary>The optional schema-specific decoded RDATA values.</summary>
    public DnsRecordData? RecordData;

    /// <summary>The optional result of decoding one EDNS option.</summary>
    public DnsEdnsOptionResult? EdnsOptionResult;

    /// <summary>Input octets remaining after the decoded value.</summary>
    public byte[] TrailingBytes = [];

    /// <summary>Diagnostics produced while decoding.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds the exception-less result of a DNS encode operation.</summary>
public class DnsEncodeResult
{
    /// <summary>Indicates that the requested value encoded successfully.</summary>
    public bool Success;

    /// <summary>The number of output octets written.</summary>
    public int BytesWritten;

    /// <summary>The encoded wire-format bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>Indicates that whole record sets were removed to satisfy a size limit.</summary>
    public bool Truncated;

    /// <summary>The optional message represented by the final encoded output.</summary>
    public DnsMessage? Message;

    /// <summary>Diagnostics produced while encoding.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

/// <summary>Holds one DNS-over-TCP length-prefixed message frame.</summary>
public class DnsTcpFrame
{
    /// <summary>The message length declared by the frame prefix.</summary>
    public int MessageLength;

    /// <summary>The DNS message bytes without the two-octet length prefix.</summary>
    public byte[] MessageBytes = [];

    /// <summary>Indicates that all bytes declared by the frame prefix are present.</summary>
    public bool Complete;
}
