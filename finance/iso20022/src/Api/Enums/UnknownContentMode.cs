namespace iso20022;

/// <summary>Controls handling of XML content without a known model.</summary>
public enum UnknownContentMode
{
    /// <summary>Reject unknown content.</summary>
    Reject = 0,
    /// <summary>Preserve unknown content for inspection and round trips.</summary>
    Preserve = 1,
    /// <summary>Ignore unknown content explicitly.</summary>
    Ignore = 2
}
