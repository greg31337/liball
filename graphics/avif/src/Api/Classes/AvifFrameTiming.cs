namespace avif;

/// <summary>Describes one timed-sequence frame presentation interval.</summary>
public class AvifFrameTiming : IAvifFrameTiming
{
    uint timescale;
    ulong presentationTime;
    uint duration;

    internal AvifFrameTiming(
        uint timescale,
        ulong presentationTime,
        uint duration)
    {
        this.timescale = timescale;
        this.presentationTime = presentationTime;
        this.duration = duration;
    }

    /// <summary>Returns the number of media time units per second.</summary>
    /// <returns>The positive media timescale.</returns>
    public uint GetTimescale()
    {
        return timescale;
    }

    /// <summary>Returns the presentation timestamp in media-timescale units.</summary>
    /// <returns>The unsigned presentation timestamp.</returns>
    public ulong GetPresentationTime()
    {
        return presentationTime;
    }

    /// <summary>Returns the frame duration in media-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    public uint GetDuration()
    {
        return duration;
    }

    /// <summary>Returns the presentation timestamp in seconds.</summary>
    /// <returns>The timestamp in seconds.</returns>
    public double GetPresentationTimeSeconds()
    {
        return timescale == 0 ? 0 : presentationTime / (double)timescale;
    }

    /// <summary>Returns the frame duration in seconds.</summary>
    /// <returns>The duration in seconds.</returns>
    public double GetDurationSeconds()
    {
        return timescale == 0 ? 0 : duration / (double)timescale;
    }
}
