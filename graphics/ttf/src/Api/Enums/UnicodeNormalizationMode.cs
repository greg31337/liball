namespace ttf;

/// <summary>Selects Unicode normalization performed before shaping.</summary>
public enum UnicodeNormalizationMode
{
    /// <summary>Preserve the decoded scalar sequence.</summary>
    None = 0,

    /// <summary>Apply canonical composition.</summary>
    Nfc = 1,

    /// <summary>Apply canonical decomposition.</summary>
    Nfd = 2,

    /// <summary>Apply compatibility decomposition and canonical composition.</summary>
    Nfkc = 3,

    /// <summary>Apply compatibility decomposition.</summary>
    Nfkd = 4
}
