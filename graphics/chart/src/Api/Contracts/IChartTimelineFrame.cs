namespace chart;
/// <summary>Defines one immutable retained-scene snapshot in a sequential chart timeline.</summary>
public interface IChartTimelineFrame
{
    /// <summary>Creates an independent frame and scene copy.</summary>
    /// <returns>The copied timeline frame.</returns>
    ChartTimelineFrame Copy();
    /// <summary>Returns the stable frame identifier.</summary>
    /// <returns>The frame identifier.</returns>
    string GetFrameId();
    /// <summary>Returns an independent retained-scene copy.</summary>
    /// <returns>The copied frame scene.</returns>
    ChartRenderScene GetScene();
    /// <summary>Returns the positive frame duration in milliseconds.</summary>
    /// <returns>The frame duration.</returns>
    int GetDurationMilliseconds();
}
