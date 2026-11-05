namespace qrcode;

/// <summary>Identifies the structural or payload role of a symbol module.</summary>
public enum QrModuleRole
{
    /// <summary>The module role is not known.</summary>
    Unknown = 0,

    /// <summary>The module belongs to the surrounding quiet zone.</summary>
    QuietZone = 1,

    /// <summary>The module belongs to a finder pattern.</summary>
    Finder = 2,

    /// <summary>The module belongs to a finder-pattern separator.</summary>
    Separator = 3,

    /// <summary>The module belongs to a timing pattern.</summary>
    Timing = 4,

    /// <summary>The module belongs to an alignment or extension pattern.</summary>
    Alignment = 5,

    /// <summary>The module stores format information.</summary>
    Format = 6,

    /// <summary>The module stores version information.</summary>
    Version = 7,

    /// <summary>The module is the fixed dark module.</summary>
    DarkModule = 8,

    /// <summary>The module stores a data-codeword bit.</summary>
    Data = 9,

    /// <summary>The module stores an error-correction-codeword bit.</summary>
    ErrorCorrection = 10,

    /// <summary>The module stores a remainder bit.</summary>
    Remainder = 11
}
