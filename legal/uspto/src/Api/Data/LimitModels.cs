namespace uspto;

/// <summary>
/// Defines cross-cutting request, response, parsing, and aggregation limits.
/// </summary>
public class UsptoLimits
{
    /// <summary>The maximum encoded URI length in characters.</summary>
    public int MaximumUriCharacters = 8192;

    /// <summary>The maximum encoded request body length in bytes.</summary>
    public long MaximumRequestBytes = UsptoDefaults.MaximumResponseBytes;

    /// <summary>The maximum buffered response length in bytes.</summary>
    public long MaximumResponseBytes = UsptoDefaults.MaximumResponseBytes;

    /// <summary>The maximum retained raw response length in bytes.</summary>
    public long MaximumRawResponseBytes = UsptoDefaults.MaximumRawResponseBytes;

    /// <summary>The maximum number of pages processed by one aggregate request.</summary>
    public int MaximumPages = UsptoDefaults.MaximumPages;

    /// <summary>The maximum number of records returned by one aggregate request.</summary>
    public int MaximumResults = UsptoDefaults.MaximumResults;

    /// <summary>The maximum JSON nesting depth.</summary>
    public int MaximumJsonDepth = 128;

    /// <summary>The maximum XML nesting depth.</summary>
    public int MaximumXmlDepth = 128;

    /// <summary>The maximum decoded scalar string length in characters.</summary>
    public int MaximumStringCharacters = 1048576;

    /// <summary>The maximum delimited field length in characters.</summary>
    public int MaximumDelimitedFieldCharacters = 1048576;

    /// <summary>The maximum number of columns accepted in one delimited row.</summary>
    public int MaximumDelimitedColumns = 10000;
}
