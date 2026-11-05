namespace chart;
/// <summary>Defines an ordered collection of deterministic chart frame snapshots.</summary>
public interface IChartTimeline
{
    /// <summary>Adds an independent frame snapshot at the end of the sequence.</summary>
    /// <param name = "frame">The frame to copy and append.</param>
    /// <returns>The copied stored frame.</returns>
    ChartTimelineFrame AddFrame(ChartTimelineFrame frame);
    /// <summary>Returns one frame in insertion order.</summary>
    /// <param name = "index">The zero-based frame index.</param>
    /// <returns>The stored frame.</returns>
    ChartTimelineFrame FrameAt(int index);
    /// <summary>Returns the total duration of all frames.</summary>
    /// <returns>The total duration in milliseconds.</returns>
    long TotalDurationMilliseconds();
    /// <summary>Creates an independent timeline and frame copy.</summary>
    /// <returns>The copied timeline.</returns>
    ChartTimeline Copy();
    /// <summary>Returns the number of frames.</summary>
    /// <returns>The frame count.</returns>
    int GetFrameCount();
}
