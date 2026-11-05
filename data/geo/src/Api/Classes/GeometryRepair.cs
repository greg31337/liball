namespace geo;

/// <summary>Provides owning geometry, coordinate, ring, precision, and coverage repairs.</summary>
public class GeometryRepair : IGeometryRepair
{
    /// <summary>Creates a geometry repair provider.</summary>
    public GeometryRepair()
    {
    }

    /// <summary>Builds valid geometry using the selected linework or structure strategy.</summary>
    /// <param name="geometry">The geometry to repair.</param>
    /// <param name="parameters">The cleanup, strategy, and collapse policies.</param>
    /// <returns>An owning valid geometry with the narrowest suitable type.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or parameters cannot be repaired.</exception>
    /// <exception cref="TopologyException">Valid topology cannot be reconstructed.</exception>
    public Geometry MakeValid(
        Geometry geometry, RepairParameters parameters)
    {
        return GeometryRepairEngine.MakeValid(geometry, parameters);
    }

    /// <summary>Removes configured non-finite and repeated coordinates and repairs sequence cardinality.</summary>
    /// <param name="geometry">The geometry whose coordinates are cleaned.</param>
    /// <param name="parameters">The coordinate cleanup policies.</param>
    /// <returns>An owning cleaned geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or parameters are invalid.</exception>
    public Geometry CleanCoordinates(
        Geometry geometry, RepairParameters parameters)
    {
        return GeometryRepairEngine.CleanCoordinates(
            geometry, parameters);
    }

    /// <summary>Closes, nodes, polygonizes, and reassigns polygon rings.</summary>
    /// <param name="geometry">The polygonal geometry whose rings are repaired.</param>
    /// <param name="parameters">The cleanup and collapse policies.</param>
    /// <returns>An owning valid polygonal or collapsed result.</returns>
    /// <exception cref="InvalidGeometryException">The geometry or parameters are invalid.</exception>
    /// <exception cref="TopologyException">Valid rings cannot be reconstructed.</exception>
    public Geometry RepairRings(
        Geometry geometry, RepairParameters parameters)
    {
        return GeometryRepairEngine.RepairRings(geometry, parameters);
    }

    /// <summary>Reduces geometry to a fixed grid while preserving valid topology.</summary>
    /// <param name="geometry">The valid core geometry to reduce.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>An owning valid reduced geometry.</returns>
    /// <exception cref="InvalidGeometryException">The geometry is invalid or unsupported.</exception>
    /// <exception cref="PrecisionException">The precision model or reduced topology is invalid.</exception>
    public Geometry ReducePrecision(
        Geometry geometry, PrecisionModel precisionModel)
    {
        PrecisionReductionResult result =
            TopologyPreservingPrecisionReducer.ReduceGeometry(
                geometry, precisionModel);
        return result.Geometry;
    }

    /// <summary>Removes components that cannot retain their declared dimension.</summary>
    /// <param name="geometry">The geometry to clean.</param>
    /// <returns>An owning result without collapsed components.</returns>
    /// <exception cref="InvalidGeometryException">The geometry cannot be cleaned.</exception>
    /// <exception cref="TopologyException">Valid topology cannot be reconstructed.</exception>
    public Geometry RemoveCollapsedComponents(Geometry geometry)
    {
        return GeometryRepairEngine.RemoveCollapsedComponents(geometry);
    }

    /// <summary>Resolves coverage overlaps and eligible narrow gaps using the configured ownership policy.</summary>
    /// <param name="polygonal">The polygon coverage to clean.</param>
    /// <param name="parameters">The coverage ownership and snapping policies.</param>
    /// <returns>An owning cleaned polygon coverage.</returns>
    /// <exception cref="InvalidGeometryException">The coverage or parameters are invalid.</exception>
    /// <exception cref="TopologyException">Valid coverage topology cannot be reconstructed.</exception>
    public Geometry CleanCoverage(
        Geometry polygonal, RepairParameters parameters)
    {
        return GeometryRepairEngine.CleanCoverage(
            polygonal, parameters);
    }
}
