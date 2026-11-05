namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifTrackEdit"/>.</summary>
public interface IAvifTrackEdit
{
    /// <summary>Returns the edit duration in movie-timescale units.</summary>
    /// <returns>The positive segment duration.</returns>
    ulong GetSegmentDuration();

    /// <summary>Returns the edit duration in seconds.</summary>
    /// <returns>The duration, or zero when the movie timescale is unavailable.</returns>
    double GetSegmentDurationSeconds();

    /// <summary>Returns the starting media time in media-timescale units.</summary>
    /// <returns>The media time, or minus one for an empty edit.</returns>
    long GetMediaTime();

    /// <summary>Reports whether this segment is an empty edit.</summary>
    /// <returns><c>true</c> when the media time is minus one.</returns>
    bool IsEmpty();

    /// <summary>Returns the signed integer portion of the media playback rate.</summary>
    /// <returns>Zero for a dwell edit or one for normal playback.</returns>
    int GetMediaRateInteger();

    /// <summary>Returns the signed fractional portion of the media playback rate.</summary>
    /// <returns>The 16-bit fractional component.</returns>
    int GetMediaRateFraction();
}
