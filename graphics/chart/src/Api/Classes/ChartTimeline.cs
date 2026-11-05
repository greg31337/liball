namespace chart;
/// <summary>Stores an ordered collection of deterministic chart frame snapshots.</summary>
public class ChartTimeline : IChartTimeline
{
    internal ChartTimelineFrame[] Frames;
    internal int FrameCount;

    /// <summary>Initializes an empty chart timeline.</summary>
    public ChartTimeline()
    {
        Frames = new ChartTimelineFrame[4];
        FrameCount = 0;
    }

    /// <summary>Adds an independent frame snapshot at the end of the sequence.</summary>
    /// <param name = "frame">The frame to copy and append.</param>
    /// <returns>The copied stored frame.</returns>
    public ChartTimelineFrame AddFrame(ChartTimelineFrame frame)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        if (FrameIndex(frame.FrameId) >= 0)
        {
            throw new System.ArgumentException("A timeline frame with the same id already exists.");
        }

        if (FrameCount > 0 && (Frames[0].Scene.Width != frame.Scene.Width || Frames[0].Scene.Height != frame.Scene.Height))
        {
            throw new System.ArgumentException("Timeline frames must use the same scene dimensions.");
        }

        EnsureCapacity(FrameCount + 1);
        Frames[FrameCount] = frame.Copy();
        FrameCount = FrameCount + 1;
        return Frames[FrameCount - 1];
    }

    /// <summary>Returns one frame in insertion order.</summary>
    /// <param name = "index">The zero-based frame index.</param>
    /// <returns>The stored frame.</returns>
    public ChartTimelineFrame FrameAt(int index)
    {
        if (index < 0 || index >= FrameCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Frames[index];
    }

    /// <summary>Returns the total duration of all frames.</summary>
    /// <returns>The total duration in milliseconds.</returns>
    public long TotalDurationMilliseconds()
    {
        long total = 0;
        int index = 0;
        while (index < FrameCount)
        {
            total = total + Frames[index].DurationMilliseconds;
            index = index + 1;
        }

        return total;
    }

    /// <summary>Creates an independent timeline and frame copy.</summary>
    /// <returns>The copied timeline.</returns>
    public ChartTimeline Copy()
    {
        ChartTimeline copy = new ChartTimeline();
        int index = 0;
        while (index < FrameCount)
        {
            copy.AddFrame(Frames[index]);
            index = index + 1;
        }

        return copy;
    }

    /// <summary>Returns the number of frames.</summary>
    /// <returns>The frame count.</returns>
    public int GetFrameCount()
    {
        return FrameCount;
    }

    internal int FrameIndex(string frameId)
    {
        ChartPlot.ValidateId(frameId);
        int index = 0;
        while (index < FrameCount)
        {
            if (Frames[index].FrameId == frameId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Frames.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Frames.Length, capacity);
        ChartTimelineFrame[] frames = new ChartTimelineFrame[newCapacity];
        System.Array.Copy(Frames, frames, FrameCount);
        Frames = frames;
    }
}
