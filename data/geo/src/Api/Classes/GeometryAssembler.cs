using System.Collections.Generic;

namespace geo;

/// <summary>Provides owning line merge, polygonization, area building, and coverage-edge assembly.</summary>
public class GeometryAssembler : IGeometryAssembler
{
    /// <summary>Creates a geometry assembly provider.</summary>
    public GeometryAssembler()
    {
    }

    /// <summary>Merges connected linework regardless of source direction.</summary>
    /// <param name="geometry">The source linework.</param>
    /// <returns>An owning merged lineal geometry.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public Geometry LineMerge(Geometry geometry)
    {
        return LineMerger.MergeGeometry(geometry, false);
    }

    /// <summary>Merges only linework whose source directions agree.</summary>
    /// <param name="geometry">The source linework.</param>
    /// <returns>An owning direction-preserving lineal geometry.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public Geometry DirectedLineMerge(Geometry geometry)
    {
        return LineMerger.MergeGeometry(geometry, true);
    }

    /// <summary>Builds every bounded polygonal face from correctly noded linework.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>An owning polygonal geometry.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">The linework cannot be polygonized consistently.</exception>
    public Geometry Polygonize(Geometry geometry)
    {
        return Polygonizer.PolygonizeGeometry(geometry);
    }

    /// <summary>Builds polygons and returns all polygonization diagnostics.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>A four-member collection containing polygons, dangles, cut edges, and invalid rings.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">The linework cannot be polygonized consistently.</exception>
    public Geometry PolygonizeFull(Geometry geometry)
    {
        return Polygonizer.PolygonizeFullGeometry(geometry);
    }

    /// <summary>Builds a dissolved area using odd-even ring nesting.</summary>
    /// <param name="geometry">The source linework or polygon boundaries.</param>
    /// <returns>An owning polygonal area.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    /// <exception cref="TopologyException">A valid area cannot be constructed.</exception>
    public Geometry BuildArea(Geometry geometry)
    {
        return AreaBuilder.BuildGeometry(geometry);
    }

    /// <summary>Extracts the shell and hole edge sequences of polygonal coverage.</summary>
    /// <param name="polygonal">The polygonal coverage.</param>
    /// <returns>An owning multi-line edge geometry.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or contains a non-polygonal component.</exception>
    public Geometry ExtractCoverageEdges(Geometry polygonal)
    {
        GeometryInspection.EnsureGeometryGraph(polygonal);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(polygonal);
        List<LineString> lines = new List<LineString>();
        CollectCoveragePolygonEdges(polygonal, lines, options);
        return GeometryBuilder.CreateMultiLineStringValue(
            lines.ToArray(), options);
    }

    internal static void CollectCoveragePolygonEdges(
        Geometry geometry, List<LineString> lines,
        GeometryFactoryOptions options)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Polygon:
                GeometryBoundary.AddPolygonBoundaryLines(
                    (Polygon)payload, lines, options);
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    GeometryBoundary.AddPolygonBoundaryLines(
                        multiPolygon.Polygons[index], lines, options);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectCoveragePolygonEdges(
                        collection.Geometries[index], lines, options);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Coverage edge extraction requires polygonal geometry.");
        }
    }
}
