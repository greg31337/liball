namespace fake;

/// <summary>Configures deterministic record-oriented CSV export.</summary>
public class CsvOptions
{
    /// <summary>Whether the first record contains column names.</summary>
    public bool IncludeHeader;

    /// <summary>The single field delimiter character.</summary>
    public char Delimiter;

    /// <summary>The record separator, using CRLF by default.</summary>
    public string NewLine = string.Empty;

    /// <summary>The text emitted for null cells; an empty value intentionally matches an empty string cell.</summary>
    public string NullValue = string.Empty;

    /// <summary>Whether string cells beginning with spreadsheet formula prefixes receive an apostrophe.</summary>
    public bool GuardSpreadsheetFormula;

    /// <summary>Initializes RFC 4180-oriented safe defaults.</summary>
    public CsvOptions()
    {
        IncludeHeader = true;
        Delimiter = ',';
        NewLine = "\r\n";
        NullValue = "";
        GuardSpreadsheetFormula = true;
    }
}
