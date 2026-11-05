namespace fake;

/// <summary>Represents synthetic television-series and episode-shaped metadata.</summary>
/// <remarks>The record does not represent a produced, broadcast, licensed, or published episode.</remarks>
public class TelevisionEpisodeRecord
{
    /// <summary>The normalized locale requested from the generator.</summary>
    public string Locale = string.Empty;

    /// <summary>The locale of the vocabulary source, always <c>invariant</c>.</summary>
    public string VocabularyLocale = string.Empty;

    /// <summary>The compositional fictional series title.</summary>
    public string SeriesTitle = string.Empty;

    /// <summary>The compositional fictional episode title.</summary>
    public string EpisodeTitle = string.Empty;

    /// <summary>The short project-authored compositional episode summary.</summary>
    public string Summary = string.Empty;

    /// <summary>The broad conventional television genre.</summary>
    public string Genre = string.Empty;

    /// <summary>The project-fictional network name.</summary>
    public string Network = string.Empty;

    /// <summary>The positive season number.</summary>
    public int SeasonNumber;

    /// <summary>The positive episode number within the season.</summary>
    public int EpisodeNumber;

    /// <summary>The season-and-episode code correlated with the numeric fields.</summary>
    public string EpisodeCode = string.Empty;

    /// <summary>The positive runtime-shaped value in minutes.</summary>
    public int DurationMinutes;

    /// <summary>The synthetic air date at or before the captured reference time.</summary>
    public System.DateTimeOffset AirDate;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether title and summary text use project-authored compositions, always <c>true</c>.</summary>
    public bool UsesProjectAuthoredText;

    /// <summary>Whether the record represents a published work, always <c>false</c>.</summary>
    public bool RepresentsPublishedWork;

    /// <summary>Whether the record represents a produced or broadcast episode, always <c>false</c>.</summary>
    public bool RepresentsBroadcastEpisode;

    internal TelevisionEpisodeRecord()
    {
    }
}
