namespace geo;

/// <summary>Specifies geometry and coverage repair policies.</summary>
public class RepairParameters
{
    /// <summary>The selected make-valid strategy.</summary>
    public RepairMethod Method;

    /// <summary>The policy used when coverage regions compete during cleaning.</summary>
    public CoverageMergeStrategy CoverageMergeStrategy;

    /// <summary>Whether collapsed lower-dimensional components are retained.</summary>
    public bool KeepCollapsedComponents;

    /// <summary>Whether coordinates having non-finite ordinates are removed.</summary>
    public bool RemoveNonFiniteCoordinates = true;

    /// <summary>Whether consecutive repeated coordinates are removed.</summary>
    public bool RemoveRepeatedCoordinates = true;

    /// <summary>The non-negative snapping tolerance used during repair.</summary>
    public double SnapTolerance;

    /// <summary>The maximum coverage gap width eligible for cleaning.</summary>
    public double MaximumGapWidth;
}
