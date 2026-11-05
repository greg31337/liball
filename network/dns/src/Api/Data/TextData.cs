using System.Collections.Generic;

namespace dns;

/// <summary>Controls DNS presentation and zone-text formatting.</summary>
public class DnsTextOptions
{
    /// <summary>Requests canonical lowercase names and deterministic record ordering.</summary>
    public bool Canonical;

    /// <summary>Uses registered mnemonic names for known classes, types, and codes.</summary>
    public bool UseMnemonicCodes = true;

    /// <summary>Includes message header information when formatting a complete message.</summary>
    public bool IncludeHeader = true;

    /// <summary>Includes section headings when formatting a complete message.</summary>
    public bool IncludeSectionNames = true;

    /// <summary>Allows long resource records to use parenthesized multiline output.</summary>
    public bool MultilineRecords;

    /// <summary>Retains available input token and comment formatting.</summary>
    public bool PreserveInputFormatting;

    /// <summary>The optional origin used to shorten absolute names.</summary>
    public DnsName? Origin;

    /// <summary>The number of spaces used for each indentation level.</summary>
    public int IndentSize = 2;

    /// <summary>The preferred maximum output line length.</summary>
    public int MaximumLineLength = 120;
}

/// <summary>Holds a presentation parsing or formatting result.</summary>
public class DnsTextResult
{
    /// <summary>Indicates that the text operation succeeded.</summary>
    public bool Success;

    /// <summary>The parsed or formatted presentation text.</summary>
    public string Text = string.Empty;

    /// <summary>The optional parsed DNS name.</summary>
    public DnsName? Name;

    /// <summary>The optional parsed DNS question.</summary>
    public DnsQuestion? Question;

    /// <summary>The optional parsed resource record.</summary>
    public DnsResourceRecord? Record;

    /// <summary>The optional parsed resource-record set.</summary>
    public DnsResourceRecordSet? RecordSet;

    /// <summary>The optional parsed DNS message.</summary>
    public DnsMessage? Message;

    /// <summary>The optional parsed zone.</summary>
    public DnsZone? Zone;

    /// <summary>General diagnostics produced by the operation.</summary>
    public List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();

    /// <summary>Zone-source diagnostics produced by the operation.</summary>
    public List<DnsZoneDiagnostic> ZoneDiagnostics = new List<DnsZoneDiagnostic>();
}
