namespace iso20022;

/// <summary>Identifies how date and time lexical forms are represented.</summary>
public enum DateTimeLexicalMode
{
    /// <summary>Preserve the source lexical form.</summary>
    Preserve = 0,
    /// <summary>Use a normalized lexical form.</summary>
    Normalize = 1
}
