namespace geo;

/// <summary>Stores the dimensions of the nine DE-9IM intersections.</summary>
public class RelationMatrix
{
    /// <summary>The nine row-major intersection dimensions.</summary>
    public RelationDimension[] Cells =
    [
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False,
        RelationDimension.False
    ];

    /// <summary>The topological dimension of the left geometry.</summary>
    public TopologicalDimension LeftDimension = TopologicalDimension.Empty;

    /// <summary>The topological dimension of the right geometry.</summary>
    public TopologicalDimension RightDimension = TopologicalDimension.Empty;
}
