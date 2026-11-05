namespace geo;

/// <summary>Assembles linework and polygonal area into independent owning geometries.</summary>
public interface IGeometryAssembler
{
    /// <summary>Merges connected linework regardless of source direction.</summary>
    /// <param name="geometry">The source linework.</param>
    /// <returns>An owning merged lineal geometry.</returns>
    Geometry LineMerge(Geometry geometry);

    /// <summary>Merges only linework whose source directions agree.</summary>
    /// <param name="geometry">The source linework.</param>
    /// <returns>An owning direction-preserving lineal geometry.</returns>
    Geometry DirectedLineMerge(Geometry geometry);

    /// <summary>Builds every bounded polygonal face from correctly noded linework.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>An owning polygonal geometry.</returns>
    Geometry Polygonize(Geometry geometry);

    /// <summary>Builds polygons and returns all polygonization diagnostics.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>A four-member collection containing polygons, dangles, cut edges, and invalid rings.</returns>
    Geometry PolygonizeFull(Geometry geometry);

    /// <summary>Builds a dissolved area using odd-even ring nesting.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>An owning polygonal area.</returns>
    Geometry BuildArea(Geometry geometry);

    /// <summary>Extracts the shell and hole edge sequences of polygonal coverage.</summary>
    /// <param name="polygonal">The polygonal coverage.</param>
    /// <returns>An owning multi-line edge geometry.</returns>
    Geometry ExtractCoverageEdges(Geometry polygonal);
}
