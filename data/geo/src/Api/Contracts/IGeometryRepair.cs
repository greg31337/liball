namespace geo;

/// <summary>Repairs invalid geometry, coordinate sequences, rings, precision, and polygon coverage.</summary>
public interface IGeometryRepair
{
    /// <summary>Builds valid geometry using the selected linework or structure strategy.</summary>
    /// <param name="geometry">The geometry to repair.</param>
    /// <param name="parameters">The cleanup, strategy, and collapse policies.</param>
    /// <returns>An owning valid geometry with the narrowest suitable type.</returns>
    Geometry MakeValid(
        Geometry geometry, RepairParameters parameters);

    /// <summary>Removes configured non-finite and repeated coordinates and repairs sequence cardinality.</summary>
    /// <param name="geometry">The geometry whose coordinates are cleaned.</param>
    /// <param name="parameters">The coordinate cleanup policies.</param>
    /// <returns>An owning cleaned geometry.</returns>
    Geometry CleanCoordinates(
        Geometry geometry, RepairParameters parameters);

    /// <summary>Closes, nodes, polygonizes, and reassigns polygon rings.</summary>
    /// <param name="geometry">The polygonal geometry whose rings are repaired.</param>
    /// <param name="parameters">The cleanup and collapse policies.</param>
    /// <returns>An owning valid polygonal or collapsed result.</returns>
    Geometry RepairRings(
        Geometry geometry, RepairParameters parameters);

    /// <summary>Reduces geometry to a fixed grid while preserving valid topology.</summary>
    /// <param name="geometry">The valid core geometry to reduce.</param>
    /// <param name="precisionModel">The fixed precision grid.</param>
    /// <returns>An owning valid reduced geometry.</returns>
    Geometry ReducePrecision(
        Geometry geometry, PrecisionModel precisionModel);

    /// <summary>Removes components that cannot retain their declared dimension.</summary>
    /// <param name="geometry">The geometry to clean.</param>
    /// <returns>An owning result without collapsed components.</returns>
    Geometry RemoveCollapsedComponents(Geometry geometry);

    /// <summary>Resolves coverage overlaps and eligible narrow gaps using the configured ownership policy.</summary>
    /// <param name="polygonal">The polygon coverage to clean.</param>
    /// <param name="parameters">The coverage ownership and snapping policies.</param>
    /// <returns>An owning cleaned polygon coverage.</returns>
    Geometry CleanCoverage(
        Geometry polygonal, RepairParameters parameters);
}
