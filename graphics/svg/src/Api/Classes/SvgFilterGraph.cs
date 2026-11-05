namespace svg;

/// <summary>Plans normalized filter execution, captures standard inputs, and reuses surfaces after their final reads.</summary>
public class SvgFilterGraphPlanner : ISvgFilterGraphPlanner
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFilterGraphPlanner()
    {
    }

    /// <summary>Builds a bounded execution and surface plan for one normalized filter resource.</summary>
    /// <param name="filter">The tagged normalized filter resource.</param>
    /// <param name="options">The transform, source images, allocation choice, and safety limits.</param>
    /// <returns>The completed plan or a stable failure.</returns>
    public SvgFilterGraphPlan Plan(SvgSceneResource filter, SvgFilterGraphOptions options)
    {
        SvgFilterGraphState state = new SvgFilterGraphState(filter, options);
        return state.Run();
    }
}
