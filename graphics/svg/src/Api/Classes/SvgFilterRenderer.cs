namespace svg;

/// <summary>Evaluates bounded normalized color, compositing, convolution, morphology, displacement, image, noise, lighting, blur, shadow, flood, offset, and tile filter primitives.</summary>
public class SvgFilterRenderer : ISvgFilterRenderer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFilterRenderer()
    {
    }

    /// <summary>Plans and evaluates one normalized filter resource.</summary>
    /// <param name="filter">The tagged normalized filter resource.</param>
    /// <param name="options">The source images, transforms, allocation limits, and work bound.</param>
    /// <returns>The completed positioned filter image or a stable failure.</returns>
    public SvgFilterRenderResult Render(SvgSceneResource filter, SvgFilterRenderOptions options)
    {
        SvgFilterRenderResult failure = new SvgFilterRenderResult();
        if (options == null || options.GraphOptions == null || !options.GraphOptions.AllocateSurfaces)
        {
            failure.Error = SvgFilterRenderError.InvalidArgument;
            return failure;
        }
        SvgFilterGraphPlan plan = new SvgFilterGraphPlanner().Plan(filter, options.GraphOptions);
        if (!plan.Success)
        {
            failure.Error = SvgFilterRenderError.PlanningFailed;
            failure.Plan = plan;
            return failure;
        }
        return new SvgFilterRenderer().Evaluate(plan, options);
    }

    /// <summary>Evaluates an allocated successful filter graph plan.</summary>
    /// <param name="plan">The successful graph plan with allocated surfaces.</param>
    /// <param name="options">The work bound and graph allocation metadata.</param>
    /// <returns>The completed positioned filter image or a stable failure.</returns>
    public SvgFilterRenderResult Evaluate(SvgFilterGraphPlan plan, SvgFilterRenderOptions options)
    {
        SvgFilterRenderState state = new SvgFilterRenderState(plan, options);
        return state.Run();
    }
}
