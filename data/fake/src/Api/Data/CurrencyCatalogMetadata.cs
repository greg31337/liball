namespace fake;

/// <summary>
/// Describes the source and update state of the bundled ISO 4217 currency catalog.
/// </summary>
public class CurrencyCatalogMetadata
{
    /// <summary>The registry source name.</summary>
    public string Source = string.Empty;

    /// <summary>The registry source page.</summary>
    public string SourceUrl = string.Empty;

    /// <summary>The publication version of the bundled snapshot.</summary>
    public string SourceVersion = string.Empty;

    /// <summary>The ISO publication date recorded in the source snapshot.</summary>
    public string PublishedOn = string.Empty;

    /// <summary>The date on which the source snapshot was retrieved.</summary>
    public string RetrievedOn = string.Empty;

    /// <summary>The number of deduplicated coded records in the catalog.</summary>
    public int RecordCount;

    internal CurrencyCatalogMetadata()
    {
    }
}
