namespace fake;

/// <summary>Represents synthetic movie-shaped metadata composed from project-authored text.</summary>
/// <remarks>The record does not represent a released, licensed, reviewed, or otherwise published movie.</remarks>
public class MovieRecord
{
    /// <summary>The normalized locale requested from the generator.</summary>
    public string Locale = string.Empty;

    /// <summary>The locale of the vocabulary source, always <c>invariant</c>.</summary>
    public string VocabularyLocale = string.Empty;

    /// <summary>The compositional fictional movie title.</summary>
    public string Title = string.Empty;

    /// <summary>The short project-authored compositional summary.</summary>
    public string Summary = string.Empty;

    /// <summary>The broad conventional genre.</summary>
    public string Genre = string.Empty;

    /// <summary>The project-fictional studio name.</summary>
    public string Studio = string.Empty;

    /// <summary>The positive runtime-shaped value in minutes.</summary>
    public int RuntimeMinutes;

    /// <summary>The synthetic release date at or before the captured reference time.</summary>
    public System.DateTimeOffset ReleaseDate;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether title and summary text use project-authored compositions, always <c>true</c>.</summary>
    public bool UsesProjectAuthoredText;

    /// <summary>Whether the record represents a published work, always <c>false</c>.</summary>
    public bool RepresentsPublishedWork;

    /// <summary>Whether the record represents a released movie, always <c>false</c>.</summary>
    public bool RepresentsReleasedMovie;

    internal MovieRecord()
    {
    }
}
