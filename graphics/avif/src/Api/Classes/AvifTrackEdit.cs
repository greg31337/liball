namespace avif;

/// <summary>Describes one presentation edit applied to a timed image track.</summary>
public class AvifTrackEdit : IAvifTrackEdit
{
    ulong segmentDuration;
    uint movieTimescale;
    long mediaTime;
    short mediaRateInteger;
    short mediaRateFraction;

    internal AvifTrackEdit(
        ulong segmentDuration,
        uint movieTimescale,
        long mediaTime,
        short mediaRateInteger,
        short mediaRateFraction)
    {
        this.segmentDuration = segmentDuration;
        this.movieTimescale = movieTimescale;
        this.mediaTime = mediaTime;
        this.mediaRateInteger = mediaRateInteger;
        this.mediaRateFraction = mediaRateFraction;
    }

    /// <summary>Returns the edit duration in movie-timescale units.</summary>
    /// <returns>The positive segment duration.</returns>
    public ulong GetSegmentDuration()
    {
        return segmentDuration;
    }

    /// <summary>Returns the edit duration in seconds.</summary>
    /// <returns>The duration, or zero when the movie timescale is unavailable.</returns>
    public double GetSegmentDurationSeconds()
    {
        return movieTimescale == 0 ? 0 : segmentDuration / (double)movieTimescale;
    }

    /// <summary>Returns the starting media time in media-timescale units.</summary>
    /// <returns>The media time, or minus one for an empty edit.</returns>
    public long GetMediaTime()
    {
        return mediaTime;
    }

    /// <summary>Reports whether this segment is an empty edit.</summary>
    /// <returns><c>true</c> when the media time is minus one.</returns>
    public bool IsEmpty()
    {
        return mediaTime == -1;
    }

    /// <summary>Returns the signed integer portion of the media playback rate.</summary>
    /// <returns>Zero for a dwell edit or one for normal playback.</returns>
    public int GetMediaRateInteger()
    {
        return mediaRateInteger;
    }

    /// <summary>Returns the signed fractional portion of the media playback rate.</summary>
    /// <returns>The 16-bit fractional component.</returns>
    public int GetMediaRateFraction()
    {
        return mediaRateFraction;
    }
}
