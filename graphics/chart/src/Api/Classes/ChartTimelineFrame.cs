namespace chart;
/// <summary>Stores one immutable retained-scene snapshot in a sequential chart timeline.</summary>
public class ChartTimelineFrame : IChartTimelineFrame
{
    internal string FrameId;
    internal ChartRenderScene Scene;
    internal int DurationMilliseconds;

    /// <summary>Initializes a timeline frame by copying its retained scene.</summary>
    /// <param name = "frameId">The stable frame identifier.</param>
    /// <param name = "scene">The retained scene snapshot.</param>
    /// <param name = "durationMilliseconds">The positive frame duration in milliseconds.</param>
    public ChartTimelineFrame(string frameId, ChartRenderScene scene, int durationMilliseconds)
    {
        ChartPlot.ValidateId(frameId);
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        if (durationMilliseconds <= 0)
        {
            throw new System.ArgumentOutOfRangeException("durationMilliseconds");
        }

        FrameId = frameId;
        Scene = scene.Copy();
        DurationMilliseconds = durationMilliseconds;
    }

    /// <summary>Creates an independent frame and scene copy.</summary>
    /// <returns>The copied timeline frame.</returns>
    public ChartTimelineFrame Copy()
    {
        return new ChartTimelineFrame(FrameId, Scene, DurationMilliseconds);
    }

    /// <summary>Returns the stable frame identifier.</summary>
    /// <returns>The frame identifier.</returns>
    public string GetFrameId()
    {
        return FrameId;
    }

    /// <summary>Returns an independent retained-scene copy.</summary>
    /// <returns>The copied frame scene.</returns>
    public ChartRenderScene GetScene()
    {
        return Scene.Copy();
    }

    /// <summary>Returns the positive frame duration in milliseconds.</summary>
    /// <returns>The frame duration.</returns>
    public int GetDurationMilliseconds()
    {
        return DurationMilliseconds;
    }
}
