using geo;

namespace geo.Tests;

public class CurvedLinearKernelIntegrationTests
{
    [Test]
    public void DistanceUsesTheSuppliedCurveDeviationBound()
    {
        GeometryFactory factory = Factory();
        Geometry arc = Semicircle(factory);
        double ordinate = 1.1 / Math.Sqrt(2.0);
        Geometry point = factory.CreatePoint(
            CoordinateValue(-ordinate, ordinate));
        GeometryMeasures measures = new GeometryMeasures();

        double coarse = measures.DistanceAfterCurveLinearization(
            arc, point, CurveParameters(0.5));
        double fine = measures.DistanceAfterCurveLinearization(
            arc, point, CurveParameters(0.001));

        Assert.Multiple(() =>
        {
            Assert.That(coarse, Is.GreaterThan(0.35));
            Assert.That(fine, Is.InRange(0.1, 0.101));
            Assert.That(fine, Is.LessThan(coarse));
        });
    }

    [Test]
    public void ValidationAndDistanceMatchManualLinearization()
    {
        GeometryFactory factory = Factory();
        Geometry polygon = SemicirclePolygon(factory);
        Geometry target = factory.CreatePoint(CoordinateValue(0.0, 2.0));
        CurveLinearizationParameters parameters = CurveParameters(0.002);
        GeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry linearPolygon = simplifier.LinearizeCurves(
            polygon, parameters);
        Geometry linearTarget = simplifier.LinearizeCurves(
            target, parameters);
        GeometryValidator validator = new GeometryValidator();
        GeometryMeasures measures = new GeometryMeasures();

        ValidationResult expectedValidation =
            validator.ValidateGeometry(linearPolygon);
        ValidationResult actualValidation =
            validator.ValidateAfterCurveLinearization(
                polygon, parameters);
        double expectedDistance = measures.Distance(
            linearPolygon, linearTarget);
        DistanceResult expectedNearest = measures.NearestPoints(
            linearPolygon, linearTarget);
        DistanceResult actualNearest =
            measures.NearestPointsAfterCurveLinearization(
                polygon, target, parameters);

        Assert.Multiple(() =>
        {
            Assert.That(actualValidation.IsValid,
                Is.EqualTo(expectedValidation.IsValid));
            Assert.That(actualValidation.Error,
                Is.EqualTo(expectedValidation.Error));
            Assert.That(measures.DistanceAfterCurveLinearization(
                polygon, target, parameters), Is.EqualTo(expectedDistance));
            Assert.That(measures.IndexedDistanceAfterCurveLinearization(
                polygon, target, parameters), Is.EqualTo(
                    measures.IndexedDistance(linearPolygon, linearTarget)));
            Assert.That(actualNearest.HasResult,
                Is.EqualTo(expectedNearest.HasResult));
            Assert.That(actualNearest.Distance,
                Is.EqualTo(expectedNearest.Distance));
        });
    }

    [Test]
    public void EveryCurvePredicateMatchesItsLinearKernelEquivalent()
    {
        GeometryFactory factory = Factory();
        Geometry first = SemicirclePolygon(factory);
        Geometry second = factory.CreateRectangle(
            CoordinateValue(-0.5, 0.25),
            CoordinateValue(0.5, 1.25));
        CurveLinearizationParameters parameters = CurveParameters(0.005);
        GeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry linearFirst = simplifier.LinearizeCurves(first, parameters);
        Geometry linearSecond = simplifier.LinearizeCurves(second, parameters);
        GeometryPredicates predicates = new GeometryPredicates();

        Assert.Multiple(() =>
        {
            Assert.That(predicates
                .AreTopologicallyEqualAfterCurveLinearization(
                    first, second, parameters), Is.EqualTo(predicates
                        .AreTopologicallyEqual(linearFirst, linearSecond)));
            Assert.That(predicates.DisjointAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Disjoint(linearFirst, linearSecond)));
            Assert.That(predicates.IntersectsAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Intersects(linearFirst, linearSecond)));
            Assert.That(predicates.TouchesAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Touches(linearFirst, linearSecond)));
            Assert.That(predicates.CrossesAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Crosses(linearFirst, linearSecond)));
            Assert.That(predicates.WithinAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Within(linearFirst, linearSecond)));
            Assert.That(predicates.ContainsAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Contains(linearFirst, linearSecond)));
            Assert.That(predicates.OverlapsAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Overlaps(linearFirst, linearSecond)));
            Assert.That(predicates.CoversAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.Covers(linearFirst, linearSecond)));
            Assert.That(predicates.CoveredByAfterCurveLinearization(
                first, second, parameters), Is.EqualTo(
                    predicates.CoveredBy(linearFirst, linearSecond)));
            Assert.That(predicates
                .ContainsProperlyAfterCurveLinearization(
                    first, second, parameters), Is.EqualTo(predicates
                        .ContainsProperly(linearFirst, linearSecond)));
            Assert.That(predicates
                .WithinDistanceAfterCurveLinearization(
                    first, second, 0.25, parameters), Is.EqualTo(predicates
                        .WithinDistance(linearFirst, linearSecond, 0.25)));
        });
    }

    [Test]
    public void CurveOverlayMatchesManuallyLinearizedOverlay()
    {
        GeometryFactory factory = Factory();
        Geometry first = Semicircle(factory);
        Geometry second = factory.CreateLineString([
            CoordinateValue(-2.0, 0.5),
            CoordinateValue(2.0, 0.5)
        ]);
        CurveLinearizationParameters parameters = CurveParameters(0.005);
        GeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry linearFirst = simplifier.LinearizeCurves(first, parameters);
        Geometry linearSecond = simplifier.LinearizeCurves(second, parameters);
        GeometryOverlay overlay = new GeometryOverlay();
        GeometryPredicates predicates = new GeometryPredicates();
        Geometry multiCurve = factory.CreateMultiCurve([first, second]);
        Geometry linearMultiCurve = simplifier.LinearizeCurves(
            multiCurve, parameters);

        Assert.Multiple(() =>
        {
            AssertEquivalent(predicates,
                overlay.Intersection(linearFirst, linearSecond),
                overlay.IntersectionAfterCurveLinearization(
                    first, second, parameters));
            AssertEquivalent(predicates,
                overlay.Union(linearFirst, linearSecond),
                overlay.UnionAfterCurveLinearization(
                    first, second, parameters));
            AssertEquivalent(predicates,
                overlay.Difference(linearFirst, linearSecond),
                overlay.DifferenceAfterCurveLinearization(
                    first, second, parameters));
            AssertEquivalent(predicates,
                overlay.SymmetricDifference(linearFirst, linearSecond),
                overlay.SymmetricDifferenceAfterCurveLinearization(
                    first, second, parameters));
            AssertEquivalent(predicates,
                overlay.UnaryUnion(linearMultiCurve),
                overlay.UnaryUnionAfterCurveLinearization(
                    multiCurve, parameters));
        });
    }

    [Test]
    public void CurveBufferAndSimplificationMatchManualLinearization()
    {
        GeometryFactory factory = Factory();
        Geometry arc = Semicircle(factory);
        CurveLinearizationParameters curveParameters =
            CurveParameters(0.005);
        GeometrySimplifier simplifier = new GeometrySimplifier();
        Geometry linearized = simplifier.LinearizeCurves(
            arc, curveParameters);
        GeometryConstructive constructive = new GeometryConstructive();
        GeometryPredicates predicates = new GeometryPredicates();
        BufferParameters bufferParameters = new BufferParameters
        {
            QuadrantSegments = 4
        };
        SimplifyParameters simplifyParameters = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.DouglasPeucker,
            Tolerance = 0.01
        };

        Assert.Multiple(() =>
        {
            AssertEquivalent(predicates,
                constructive.Buffer(
                    linearized, 0.25, bufferParameters),
                constructive.BufferAfterCurveLinearization(
                    arc, 0.25, bufferParameters, curveParameters));
            AssertEquivalent(predicates,
                constructive.SingleSidedBuffer(
                    linearized, 0.25, bufferParameters),
                constructive.SingleSidedBufferAfterCurveLinearization(
                    arc, 0.25, bufferParameters, curveParameters));
            AssertEquivalent(predicates,
                constructive.OffsetCurve(
                    linearized, 0.25, bufferParameters),
                constructive.OffsetCurveAfterCurveLinearization(
                    arc, 0.25, bufferParameters, curveParameters));
            AssertEquivalent(predicates,
                simplifier.Simplify(linearized, simplifyParameters),
                simplifier.SimplifyAfterCurveLinearization(
                    arc, simplifyParameters, curveParameters));
        });
    }

    private static void AssertEquivalent(
        GeometryPredicates predicates, Geometry expected, Geometry actual)
    {
        Assert.That(predicates.AreExactlyEqual(actual, expected), Is.True);
    }

    private static Geometry Semicircle(GeometryFactory factory)
    {
        return factory.CreateCircularString([
            CoordinateValue(-1.0, 0.0),
            CoordinateValue(0.0, 1.0),
            CoordinateValue(1.0, 0.0)
        ]);
    }

    private static Geometry SemicirclePolygon(GeometryFactory factory)
    {
        Geometry arc = Semicircle(factory);
        Geometry baseLine = factory.CreateLineString([
            CoordinateValue(1.0, 0.0),
            CoordinateValue(-1.0, 0.0)
        ]);
        Geometry shell = factory.CreateCompoundCurve([arc, baseLine]);
        return factory.CreateCurvePolygon(shell, []);
    }

    private static CurveLinearizationParameters CurveParameters(
        double maximumDeviation)
    {
        return new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumDeviation,
            Tolerance = maximumDeviation,
            MaximumSegments = 10000
        };
    }

    private static GeometryFactory Factory()
    {
        return new GeometryFactory(new GeometryFactoryOptions());
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }
}
