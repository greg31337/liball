namespace fake;

/// <summary>
/// Selects the consumer context exercised by an explicit inert adversarial string.
/// </summary>
public enum AdversarialTextContext
{
    /// <summary>SQL command or expression input.</summary>
    Sql = 0,

    /// <summary>HTML text or markup input.</summary>
    Html = 1,

    /// <summary>Spreadsheet cell input.</summary>
    Spreadsheet = 2,

    /// <summary>Command-shell input.</summary>
    Shell = 3,

    /// <summary>File-system path input.</summary>
    Path = 4,

    /// <summary>Printf-style format input.</summary>
    FormatString = 5,

    /// <summary>Template-language input.</summary>
    Template = 6,

    /// <summary>Protocol header value input.</summary>
    Header = 7
}
