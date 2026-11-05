namespace fake;

/// <summary>Describes the source and update state of the bundled IBAN format catalog.</summary>
public class IbanCatalogMetadata
{
    /// <summary>The source transcription name.</summary>
    public string Source = string.Empty;

    /// <summary>The source transcription page.</summary>
    public string SourceUrl = string.Empty;

    /// <summary>The version of the bundled transcription.</summary>
    public string SourceVersion = string.Empty;

    /// <summary>The upstream registry named by the transcription.</summary>
    public string Upstream = string.Empty;

    /// <summary>The date on which the pinned source artifact was retrieved.</summary>
    public string RetrievedOn = string.Empty;

    /// <summary>The number of country and territory records in the catalog.</summary>
    public int RecordCount;

    internal IbanCatalogMetadata()
    {
    }
}
