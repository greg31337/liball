namespace chart;
/// <summary>
/// Renders configured chart content into a caller-provided target.
/// </summary>
public interface IChartRenderer
{
    /// <summary>
    /// Renders the configured chart content into the supplied target.
    /// </summary>
    /// <param name = "target">The target that receives drawing operations.</param>
    void RenderTo(IChartRenderTarget target);
}
