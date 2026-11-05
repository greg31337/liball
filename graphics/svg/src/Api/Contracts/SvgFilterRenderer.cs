namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgFilterRenderer"/>.</summary>
public interface ISvgFilterRenderer
{
    /// <summary>Plans and evaluates one normalized filter resource.</summary>
    /// <param name="filter">The tagged normalized filter resource.</param>
    /// <param name="options">The source images, transforms, allocation limits, and work bound.</param>
    /// <returns>The completed positioned filter image or a stable failure.</returns>
    SvgFilterRenderResult Render(SvgSceneResource filter, SvgFilterRenderOptions options);
    /// <summary>Evaluates an allocated successful filter graph plan.</summary>
    /// <param name="plan">The successful graph plan with allocated surfaces.</param>
    /// <param name="options">The work bound and graph allocation metadata.</param>
    /// <returns>The completed positioned filter image or a stable failure.</returns>
    SvgFilterRenderResult Evaluate(SvgFilterGraphPlan plan, SvgFilterRenderOptions options);
}
