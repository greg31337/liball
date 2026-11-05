namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgFilterGraphPlanner"/>.</summary>
public interface ISvgFilterGraphPlanner
{
    /// <summary>Builds a bounded execution and surface plan for one normalized filter resource.</summary>
    /// <param name="filter">The tagged normalized filter resource.</param>
    /// <param name="options">The transform, source images, allocation choice, and safety limits.</param>
    /// <returns>The completed plan or a stable failure.</returns>
    SvgFilterGraphPlan Plan(SvgSceneResource filter, SvgFilterGraphOptions options);
}
