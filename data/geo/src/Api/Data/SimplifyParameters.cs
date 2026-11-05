namespace geo;

/// <summary>Specifies the algorithm used to simplify a geometry.</summary>
public class SimplifyParameters
{
    /// <summary>The selected simplification algorithm.</summary>
    public SimplificationAlgorithm Algorithm;

    /// <summary>The non-negative simplification distance or area tolerance.</summary>
    public double Tolerance;

    /// <summary>Whether open-line endpoints must be retained.</summary>
    public bool PreserveEndpoints = true;

    /// <summary>Whether collapsed lower-dimensional components are retained.</summary>
    public bool KeepCollapsedComponents;
}
