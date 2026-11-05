namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifFrameTiming"/>.</summary>
public interface IAvifFrameTiming
{
    /// <summary>Returns the number of media time units per second.</summary>
    /// <returns>The positive media timescale.</returns>
    uint GetTimescale();

    /// <summary>Returns the presentation timestamp in media-timescale units.</summary>
    /// <returns>The unsigned presentation timestamp.</returns>
    ulong GetPresentationTime();

    /// <summary>Returns the frame duration in media-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    uint GetDuration();

    /// <summary>Returns the presentation timestamp in seconds.</summary>
    /// <returns>The timestamp in seconds.</returns>
    double GetPresentationTimeSeconds();

    /// <summary>Returns the frame duration in seconds.</summary>
    /// <returns>The duration in seconds.</returns>
    double GetDurationSeconds();
}
