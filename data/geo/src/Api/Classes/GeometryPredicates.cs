namespace geo;

/// <summary>Evaluates geometry predicates with validated envelope short-circuits.</summary>
public class GeometryPredicates : IGeometryPredicates
{
    /// <summary>Creates a geometry predicate provider.</summary>
    public GeometryPredicates()
    {
    }

    /// <summary>Determines whether two geometries have identical metadata, structure, and ordinates.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometry values are exactly equal.</returns>
    public bool AreExactlyEqual(Geometry first, Geometry second)
    {
        return GeometryEquality.AreExactlyEqual(first, second);
    }

    /// <summary>Determines whether matching geometry structures have ordinates within a tolerance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="tolerance">The finite non-negative ordinate tolerance.</param>
    /// <returns>True when the structures match and every ordinate is within the tolerance.</returns>
    public bool AreEqualWithinTolerance(
        Geometry first, Geometry second, double tolerance)
    {
        return GeometryEquality.AreEqualWithinTolerance(
            first, second, tolerance);
    }

    /// <summary>Determines whether canonical normalized geometry values are structurally equal.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when normalized copies of the values are equal.</returns>
    public bool AreNormalizedEqual(Geometry first, Geometry second)
    {
        return GeometryEquality.AreNormalizedEqual(first, second);
    }

    /// <summary>Determines whether two core geometries represent the same planar point set.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries are topologically equal.</returns>
    public bool AreTopologicallyEqual(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        Envelope firstEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(values[0]);
        Envelope secondEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(values[1]);
        if (!EnvelopesHaveSameBounds(firstEnvelope, secondEnvelope))
        {
            return false;
        }
        return GeometryEquality.AreTopologicallyEqual(values[0], values[1]);
    }

    /// <summary>Determines whether two core geometries have no point in common.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries are disjoint.</returns>
    public bool Disjoint(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        Envelope firstEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(values[0]);
        Envelope secondEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(values[1]);
        if (EnvelopeAlgorithms.IsDisjointEnvelopeValue(
            firstEnvelope, secondEnvelope)) return true;
        return GeometryPredicateAlgorithms.Disjoint(values[0], values[1]);
    }

    /// <summary>Determines whether two core geometries have at least one point in common.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries intersect.</returns>
    public bool Intersects(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!GeometryEnvelopesIntersect(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Intersects(values[0], values[1]);
    }

    /// <summary>Determines whether core geometries meet through boundaries without interior overlap.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries touch.</returns>
    public bool Touches(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!GeometryEnvelopesIntersect(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Touches(values[0], values[1]);
    }

    /// <summary>Determines whether core geometries cross according to their topological dimensions.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries cross.</returns>
    public bool Crosses(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!GeometryEnvelopesIntersect(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Crosses(values[0], values[1]);
    }

    /// <summary>Determines whether the first core geometry lies within the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry is within the second.</returns>
    public bool Within(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!SecondEnvelopeCoversFirst(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Within(values[0], values[1]);
    }

    /// <summary>Determines whether the first core geometry contains the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry contains the second.</returns>
    public bool Contains(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!FirstEnvelopeCoversSecond(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Contains(values[0], values[1]);
    }

    /// <summary>Determines whether equal-dimensional core geometries partially overlap.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries overlap.</returns>
    public bool Overlaps(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!GeometryEnvelopesIntersect(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Overlaps(values[0], values[1]);
    }

    /// <summary>Determines whether every point of the second core geometry is covered by the first.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry covers the second.</returns>
    public bool Covers(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!FirstEnvelopeCoversSecond(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.Covers(values[0], values[1]);
    }

    /// <summary>Determines whether every point of the first core geometry is covered by the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry is covered by the second.</returns>
    public bool CoveredBy(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!SecondEnvelopeCoversFirst(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.CoveredBy(values[0], values[1]);
    }

    /// <summary>Determines whether the second core geometry lies strictly in the first geometry's interior.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry properly contains the second.</returns>
    public bool ContainsProperly(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        ValidateTopologicalInputs(values[0], values[1]);
        if (!FirstEnvelopeCoversSecond(values[0], values[1])) return false;
        return GeometryPredicateAlgorithms.ContainsProperly(values[0], values[1]);
    }

    /// <summary>Determines whether two non-empty core geometries are within a planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <returns>True when the minimum planar distance does not exceed the limit.</returns>
    public bool WithinDistance(
        Geometry first, Geometry second, double maximumDistance)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return GeometryPredicateAlgorithms.WithinDistance(
            values[0], values[1], maximumDistance);
    }

    /// <summary>Determines topological equality after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are topologically equal.</returns>
    public bool AreTopologicallyEqualAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return AreTopologicallyEqual(values[0], values[1]);
    }

    /// <summary>Determines disjointness after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are disjoint.</returns>
    public bool DisjointAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Disjoint(values[0], values[1]);
    }

    /// <summary>Determines intersection after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries intersect.</returns>
    public bool IntersectsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Intersects(values[0], values[1]);
    }

    /// <summary>Determines boundary touching after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries touch.</returns>
    public bool TouchesAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Touches(values[0], values[1]);
    }

    /// <summary>Determines crossing after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries cross.</returns>
    public bool CrossesAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Crosses(values[0], values[1]);
    }

    /// <summary>Determines within containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry is within the second.</returns>
    public bool WithinAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Within(values[0], values[1]);
    }

    /// <summary>Determines containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry contains the second.</returns>
    public bool ContainsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Contains(values[0], values[1]);
    }

    /// <summary>Determines overlap after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries overlap.</returns>
    public bool OverlapsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Overlaps(values[0], values[1]);
    }

    /// <summary>Determines coverage after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry covers the second.</returns>
    public bool CoversAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return Covers(values[0], values[1]);
    }

    /// <summary>Determines reverse coverage after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry is covered by the second.</returns>
    public bool CoveredByAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return CoveredBy(values[0], values[1]);
    }

    /// <summary>Determines proper containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry properly contains the second.</returns>
    public bool ContainsProperlyAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return ContainsProperly(values[0], values[1]);
    }

    /// <summary>Determines bounded distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are within the distance limit.</returns>
    public bool WithinDistanceAfterCurveLinearization(
        Geometry first, Geometry second, double maximumDistance,
        CurveLinearizationParameters parameters)
    {
        Geometry[] values = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return WithinDistance(
            values[0], values[1], maximumDistance);
    }

    internal static void ValidateTopologicalInputs(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        RelationGeometryParts firstParts = new RelationGeometryParts(first);
        RelationGeometryParts secondParts = new RelationGeometryParts(second);
        if (firstParts.Root == null || secondParts.Root == null)
        {
            throw new InvalidGeometryException(
                "Predicate geometry validation did not produce roots.");
        }
    }

    internal static bool GeometryEnvelopesIntersect(
        Geometry first, Geometry second)
    {
        Envelope firstEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(first);
        Envelope secondEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(second);
        return EnvelopeAlgorithms.IntersectsEnvelopeValue(
            firstEnvelope, secondEnvelope);
    }

    internal static bool FirstEnvelopeCoversSecond(
        Geometry first, Geometry second)
    {
        Envelope firstEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(first);
        Envelope secondEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(second);
        return EnvelopeAlgorithms.CoversEnvelopeValue(
            firstEnvelope, secondEnvelope);
    }

    internal static bool SecondEnvelopeCoversFirst(
        Geometry first, Geometry second)
    {
        Envelope firstEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(first);
        Envelope secondEnvelope = EnvelopeAlgorithms.CreateGeometryEnvelope(second);
        return EnvelopeAlgorithms.CoversEnvelopeValue(
            secondEnvelope, firstEnvelope);
    }

    internal static bool EnvelopesHaveSameBounds(
        Envelope first, Envelope second)
    {
        bool firstEmpty = EnvelopeAlgorithms.ValidateAndCheckEmpty(first);
        bool secondEmpty = EnvelopeAlgorithms.ValidateAndCheckEmpty(second);
        if (firstEmpty || secondEmpty)
        {
            return firstEmpty && secondEmpty;
        }
        return first.MinX == second.MinX && first.MinY == second.MinY &&
            first.MaxX == second.MaxX && first.MaxY == second.MaxY;
    }
}
