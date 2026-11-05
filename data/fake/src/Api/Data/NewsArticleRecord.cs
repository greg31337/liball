namespace fake;

/// <summary>Represents synthetic news-article-shaped metadata composed from project-authored text.</summary>
/// <remarks>The record does not report a real event and does not represent published journalism.</remarks>
public class NewsArticleRecord
{
    /// <summary>The normalized locale requested from the generator.</summary>
    public string Locale = string.Empty;

    /// <summary>The locale of the vocabulary source, always <c>invariant</c>.</summary>
    public string VocabularyLocale = string.Empty;

    /// <summary>The compositional fictional headline.</summary>
    public string Headline = string.Empty;

    /// <summary>The short project-authored synthetic bulletin.</summary>
    public string Summary = string.Empty;

    /// <summary>The broad conventional news category.</summary>
    public string Category = string.Empty;

    /// <summary>The project-fictional news outlet.</summary>
    public string Outlet = string.Empty;

    /// <summary>The project-fictional byline.</summary>
    public string Byline = string.Empty;

    /// <summary>The synthetic publication date at or before the captured reference time.</summary>
    public System.DateTimeOffset PublishedAt;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether headline and summary text use project-authored compositions, always <c>true</c>.</summary>
    public bool UsesProjectAuthoredText;

    /// <summary>Whether the record represents a published work, always <c>false</c>.</summary>
    public bool RepresentsPublishedWork;

    /// <summary>Whether the record reports an actual event, always <c>false</c>.</summary>
    public bool RepresentsReportedEvent;

    internal NewsArticleRecord()
    {
    }
}
