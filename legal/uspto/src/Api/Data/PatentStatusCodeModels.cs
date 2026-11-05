namespace uspto;

/// <summary>
/// Describes one patent application status code and its official description.
/// </summary>
public class PatentStatusCode
{
    /// <summary>The numeric application status code.</summary>
    public int Code = 0;

    /// <summary>The official application status description.</summary>
    public string? Description = null;

    /// <summary>Unmodeled status-code fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of patent application status codes.
/// </summary>
public class PatentStatusCodeResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned status codes in service order.</summary>
    public PatentStatusCode[] StatusCodes = new PatentStatusCode[0];

    /// <summary>A lookup from each returned code to its description.</summary>
    public global::System.Collections.Generic.Dictionary<int, string> DescriptionsByCode = new global::System.Collections.Generic.Dictionary<int, string>();

    /// <summary>The number of status codes held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching status codes.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The offset-page coordinates associated with the response.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether all requested pages were processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
