using System.Collections.Generic;

namespace dns;

/// <summary>Holds a complete DNS zone and its maintenance journal.</summary>
public class DnsZone
{
    /// <summary>The absolute zone apex name.</summary>
    public DnsName Apex = new DnsName();

    /// <summary>The recognized zone class.</summary>
    public DnsClass Class;

    /// <summary>The raw zone class integer, including unrecognized values.</summary>
    public int ClassValue;

    /// <summary>Indicates that a zone-file default TTL is available.</summary>
    public bool HasDefaultTimeToLive;

    /// <summary>The zone-file default TTL in seconds.</summary>
    public uint DefaultTimeToLive;

    /// <summary>The zone nodes in source or canonical order.</summary>
    public List<DnsZoneNode> Nodes = new List<DnsZoneNode>();

    /// <summary>The ordered committed zone journal entries.</summary>
    public List<DnsZoneJournalEntry> JournalEntries = new List<DnsZoneJournalEntry>();
}

/// <summary>Holds all resource-record sets and child nodes at one zone name.</summary>
public class DnsZoneNode
{
    /// <summary>The absolute owner name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The resource-record sets at this name.</summary>
    public List<DnsResourceRecordSet> RecordSets = new List<DnsResourceRecordSet>();

    /// <summary>The immediate child nodes in label order.</summary>
    public List<DnsZoneNode> Children = new List<DnsZoneNode>();

    /// <summary>Indicates that this node is a delegation point below the apex.</summary>
    public bool IsDelegation;

    /// <summary>Indicates an existing name without resource-record sets.</summary>
    public bool IsEmptyNonTerminal;
}

/// <summary>Controls bounded zone master-file loading.</summary>
public class DnsZoneLoadOptions
{
    /// <summary>The optional initial origin.</summary>
    public DnsName? Origin;

    /// <summary>Indicates that an initial default TTL is available.</summary>
    public bool HasDefaultTimeToLive;

    /// <summary>The initial default TTL in seconds.</summary>
    public uint DefaultTimeToLive;

    /// <summary>The recognized initial class.</summary>
    public DnsClass Class = DnsClass.Internet;

    /// <summary>The raw initial class integer.</summary>
    public int ClassValue = 1;

    /// <summary>The optional base directory for relative include paths.</summary>
    public string? BaseDirectory;

    /// <summary>Allows processing of include directives.</summary>
    public bool AllowInclude = true;

    /// <summary>Allows bounded generate directives.</summary>
    public bool AllowGenerate = true;

    /// <summary>Validates zone structure after parsing.</summary>
    public bool ValidateZone = true;

    /// <summary>Retains source-order and formatting metadata where available.</summary>
    public bool PreserveSourceFormatting;

    /// <summary>The largest permitted nested include depth.</summary>
    public int MaximumIncludeDepth = 16;

    /// <summary>The largest number of records emitted by one generate directive.</summary>
    public int MaximumGeneratedRecords = 100000;

    /// <summary>The largest total number of loaded resource records.</summary>
    public int MaximumRecords = 1000000;
}

/// <summary>Holds the result of loading a zone master file.</summary>
public class DnsZoneLoadResult
{
    /// <summary>Indicates that loading and requested validation succeeded.</summary>
    public bool Success;

    /// <summary>The optional loaded zone.</summary>
    public DnsZone? Zone;

    /// <summary>The optional structural validation result.</summary>
    public DnsZoneValidationResult? Validation;

    /// <summary>The number of resource records loaded.</summary>
    public int RecordsLoaded;

    /// <summary>The include files processed in encounter order.</summary>
    public List<string> IncludedFiles = new List<string>();

    /// <summary>The source diagnostics produced while loading.</summary>
    public List<DnsZoneDiagnostic> Diagnostics = new List<DnsZoneDiagnostic>();
}

/// <summary>Holds structural zone-validation state and diagnostics.</summary>
public class DnsZoneValidationResult
{
    /// <summary>Indicates that the zone satisfies the requested structural policy.</summary>
    public bool Valid;

    /// <summary>Indicates that exactly one apex SOA record is present.</summary>
    public bool HasApexSoa;

    /// <summary>Indicates that at least one apex NS record is present.</summary>
    public bool HasApexNameServer;

    /// <summary>The validation diagnostics.</summary>
    public List<DnsZoneDiagnostic> Diagnostics = new List<DnsZoneDiagnostic>();
}

/// <summary>Describes a zone source or structural validation problem.</summary>
public class DnsZoneDiagnostic
{
    /// <summary>The machine-readable diagnostic code.</summary>
    public DnsDiagnosticCode Code;

    /// <summary>Indicates that the diagnostic prevents successful processing.</summary>
    public bool IsError = true;

    /// <summary>The human-readable diagnostic reason.</summary>
    public string Reason = string.Empty;

    /// <summary>The optional source path.</summary>
    public string? SourcePath;

    /// <summary>Indicates that a source line is available.</summary>
    public bool HasLine;

    /// <summary>The one-based source line.</summary>
    public int Line;

    /// <summary>Indicates that a source column is available.</summary>
    public bool HasColumn;

    /// <summary>The one-based source column.</summary>
    public int Column;

    /// <summary>The optional related owner name.</summary>
    public DnsName? Name;

    /// <summary>The optional related resource record.</summary>
    public DnsResourceRecord? Record;
}

/// <summary>Holds one atomic zone mutation.</summary>
public class DnsZoneChange
{
    /// <summary>The mutation kind.</summary>
    public DnsZoneChangeKind Kind;

    /// <summary>The affected owner name.</summary>
    public DnsName Name = new DnsName();

    /// <summary>The recognized affected record type.</summary>
    public DnsRecordType RecordType;

    /// <summary>The raw affected record type integer.</summary>
    public int RecordTypeValue;

    /// <summary>The recognized affected class.</summary>
    public DnsClass Class;

    /// <summary>The raw affected class integer.</summary>
    public int ClassValue;

    /// <summary>The optional individual resource record.</summary>
    public DnsResourceRecord? Record;

    /// <summary>The optional replacement or deleted resource-record set.</summary>
    public DnsResourceRecordSet? RecordSet;
}

/// <summary>Holds a bounded set of zone changes applied atomically.</summary>
public class DnsZoneTransaction
{
    /// <summary>The caller or store transaction identifier.</summary>
    public long Identifier;

    /// <summary>Indicates that an expected SOA serial is supplied.</summary>
    public bool HasExpectedSerial;

    /// <summary>The SOA serial that must be current before applying changes.</summary>
    public uint ExpectedSerial;

    /// <summary>Indicates that a resulting SOA serial is supplied.</summary>
    public bool HasResultingSerial;

    /// <summary>The SOA serial after applying changes.</summary>
    public uint ResultingSerial;

    /// <summary>The ordered changes in the transaction.</summary>
    public List<DnsZoneChange> Changes = new List<DnsZoneChange>();

    /// <summary>Requires all changes to commit or none to commit.</summary>
    public bool Atomic = true;

    /// <summary>Indicates that the transaction was committed.</summary>
    public bool Committed;
}

/// <summary>Holds one committed zone transaction for IXFR journal processing.</summary>
public class DnsZoneJournalEntry
{
    /// <summary>The monotonically increasing local journal sequence.</summary>
    public long Sequence;

    /// <summary>The SOA serial before the transaction.</summary>
    public uint FromSerial;

    /// <summary>The SOA serial after the transaction.</summary>
    public uint ToSerial;

    /// <summary>The committed transaction.</summary>
    public DnsZoneTransaction Transaction = new DnsZoneTransaction();

    /// <summary>The journal timestamp as Unix milliseconds.</summary>
    public long RecordedAtUnixMilliseconds;
}
