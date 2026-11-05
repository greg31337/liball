namespace geo;

/// <summary>Identifies a geometry simplification strategy.</summary>
public enum SimplificationAlgorithm
{
    /// <summary>Douglas-Peucker distance simplification.</summary>
    DouglasPeucker = 0,

    /// <summary>Topology-preserving tagged-line simplification.</summary>
    TopologyPreserving = 1,

    /// <summary>Visvalingam-Whyatt effective-area simplification.</summary>
    VisvalingamWhyatt = 2,

    /// <summary>Polygon inner or outer hull simplification.</summary>
    PolygonHull = 3
}
