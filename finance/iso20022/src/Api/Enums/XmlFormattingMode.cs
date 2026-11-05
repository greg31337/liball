namespace iso20022;

/// <summary>Controls the requested XML output representation.</summary>
public enum XmlFormattingMode
{
    /// <summary>Write compact XML without discretionary indentation.</summary>
    Compact = 0,
    /// <summary>Write indented XML.</summary>
    Indented = 1,
    /// <summary>Preserve available source lexical choices.</summary>
    Preserve = 2,
    /// <summary>Write a deterministic canonical representation.</summary>
    Canonical = 3
}
