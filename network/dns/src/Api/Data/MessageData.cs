using System.Collections.Generic;

namespace dns;

/// <summary>Holds every field represented by a DNS message header.</summary>
public class DnsMessageHeader
{
    /// <summary>The 16-bit transaction identifier.</summary>
    public ushort Identifier;

    /// <summary>The recognized operation-code value.</summary>
    public DnsOpcode Opcode;

    /// <summary>The raw operation-code integer, including unrecognized values.</summary>
    public int OpcodeValue;

    /// <summary>The recognized base response-code value.</summary>
    public DnsResponseCode ResponseCode;

    /// <summary>The raw base response-code integer, including unrecognized values.</summary>
    public int ResponseCodeValue;

    /// <summary>The recognized DNS header flag bits.</summary>
    public DnsHeaderFlag Flags;

    /// <summary>The complete raw 16-bit header flags field.</summary>
    public int FlagsValue;

    /// <summary>The question count read from or written to the DNS header.</summary>
    public int QuestionCount;

    /// <summary>The answer count read from or written to the DNS header.</summary>
    public int AnswerCount;

    /// <summary>The authority count read from or written to the DNS header.</summary>
    public int AuthorityCount;

    /// <summary>The additional count read from or written to the DNS header.</summary>
    public int AdditionalCount;
}

/// <summary>Holds one DNS question entry.</summary>
public class DnsQuestion
{
    /// <summary>The queried DNS name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized query type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw query-type integer, including unrecognized values.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized query class.</summary>
    public DnsClass Class;

    /// <summary>The raw query-class integer, including unrecognized values.</summary>
    public int ClassValue;
}

/// <summary>Holds one DNS resource record and its lossless RDATA forms.</summary>
public class DnsResourceRecord
{
    /// <summary>The owner name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized resource-record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw resource-record type integer, including unrecognized values.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized resource-record class.</summary>
    public DnsClass Class;

    /// <summary>The raw resource-record class integer, including unrecognized values.</summary>
    public int ClassValue;

    /// <summary>The time to live in seconds.</summary>
    public uint TimeToLive;

    /// <summary>The optional schema-specific RDATA values.</summary>
    public DnsRecordData? Data;

    /// <summary>The original or caller-supplied opaque RDATA bytes.</summary>
    public byte[] RawData = [];

    /// <summary>Requests opaque RDATA encoding even when typed data is available.</summary>
    public bool UseRawData;
}

/// <summary>Holds records sharing a canonical owner, class, and type.</summary>
public class DnsResourceRecordSet
{
    /// <summary>The common owner name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized common resource-record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw common resource-record type integer.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized common resource-record class.</summary>
    public DnsClass Class;

    /// <summary>The raw common resource-record class integer.</summary>
    public int ClassValue;

    /// <summary>The normalized time to live in seconds.</summary>
    public uint TimeToLive;

    /// <summary>The records belonging to the set.</summary>
    public List<DnsResourceRecord> Records = new List<DnsResourceRecord>();
}

/// <summary>Holds a complete DNS message and its four ordered sections.</summary>
public class DnsMessage
{
    /// <summary>The DNS message header.</summary>
    public DnsMessageHeader Header = new DnsMessageHeader();

    /// <summary>The ordered question entries.</summary>
    public List<DnsQuestion> Questions = new List<DnsQuestion>();

    /// <summary>The ordered answer records.</summary>
    public List<DnsResourceRecord> Answers = new List<DnsResourceRecord>();

    /// <summary>The ordered authority records.</summary>
    public List<DnsResourceRecord> Authorities = new List<DnsResourceRecord>();

    /// <summary>The ordered additional records.</summary>
    public List<DnsResourceRecord> Additionals = new List<DnsResourceRecord>();
}
