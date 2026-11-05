namespace image;

/// <summary>Classifies the meaning and impact of a metadata diagnostic.</summary>
public enum MetadataSeverity
{
    /// <summary>Reports parser context that does not indicate a problem.</summary>
    Info = 0,
    /// <summary>Reports suspicious input or a condition that may reduce result quality.</summary>
    Warning = 1,
    /// <summary>Reports malformed data that was skipped while extraction continued.</summary>
    RecoverableError = 2,
    /// <summary>Reports a recognized feature that this library does not decode.</summary>
    Unsupported = 3,
    /// <summary>Reports data that ended before its declared structure was complete.</summary>
    Truncated = 4,
    /// <summary>Reports work or data skipped because a configured safety limit was reached.</summary>
    LimitExceeded = 5
}
