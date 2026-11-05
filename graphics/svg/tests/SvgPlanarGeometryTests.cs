namespace svg.Tests;

public class SvgPlanarGeometryTests
{
    [Test]
    public void BowTieReportsOnlyItsNonAdjacentCrossing()
    {
        SvgPathSelfIntersectionResult result = Parse("M0 0 L10 10 L0 10 L10 0 Z").FindSelfIntersectionsWithOptions(Options());
        SvgPathSelfIntersectionResult square = Parse("M0 0 L10 0 L10 10 L0 10 Z").FindSelfIntersectionsWithOptions(Options());

        Assert.Multiple(() =>
        {
            AssertSelfSuccess(result, 1);
            Assert.That(result.Intersections[0].Point.X, Is.EqualTo(5).Within(0.0001));
            Assert.That(result.Intersections[0].Point.Y, Is.EqualTo(5).Within(0.0001));
            Assert.That(result.Intersections[0].FirstCommandIndex, Is.EqualTo(1));
            Assert.That(result.Intersections[0].SecondCommandIndex, Is.EqualTo(3));
            Assert.That(result.Intersections[0].Kind, Is.EqualTo(SvgIntersectionKind.Crossing));
            AssertSelfSuccess(square, 0);
        });
    }

    [Test]
    public void CurvedSelfContactUsesOriginalParametersAndTangents()
    {
        SvgPathData path = Parse("M0 5 L10 5 L10 6 L0 6 Z M0 0 Q5 10 10 0");
        SvgPlanarOptions options = Options();
        options.Tolerance = 0.00001;

        SvgPathSelfIntersectionResult result = path.FindSelfIntersectionsWithOptions(options);

        SvgPathSelfIntersection contact = result.Intersections.Single(item => item.FirstCommandIndex == 1 && item.SecondCommandIndex == 6);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(contact.Point.X, Is.EqualTo(5).Within(0.0001));
            Assert.That(contact.Point.Y, Is.EqualTo(5).Within(0.0001));
            Assert.That(contact.FirstAmount, Is.EqualTo(0.5).Within(0.0001));
            Assert.That(contact.SecondAmount, Is.EqualTo(0.5).Within(0.0001));
            Assert.That(contact.Kind, Is.EqualTo(SvgIntersectionKind.Tangent));
            Assert.That(contact.DuplicateCount, Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public void SplitEdgesProducesSourceOrderedAtomicEdgesAtCrossings()
    {
        System.Collections.Generic.List<SvgPlanarEdge> edges = new System.Collections.Generic.List<SvgPlanarEdge>();
        edges.Add(Edge(0, 0, 10, 10, 7));
        edges.Add(Edge(0, 10, 10, 0, 9));

        SvgPlanarSplitResult result = new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(edges, Options());

        Assert.Multiple(() =>
        {
            AssertSplitSuccess(result, 4);
            AssertEdge(result.Edges[0], 0, 0, 5, 5, 7, 0, 0.5);
            AssertEdge(result.Edges[1], 5, 5, 10, 10, 7, 0.5, 1);
            AssertEdge(result.Edges[2], 0, 10, 5, 5, 9, 0, 0.5);
            AssertEdge(result.Edges[3], 5, 5, 10, 0, 9, 0.5, 1);
            Assert.That(result.ComparisonCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PathsFlattenAndSplitIntoReusablePlanarEdges()
    {
        SvgPlanarSplitResult result = Parse("M0 0 L10 10 L0 10 L10 0 Z").SplitPlanarEdgesWithOptions(Options());

        Assert.Multiple(() =>
        {
            AssertSplitSuccess(result, 6);
            Assert.That(result.Edges.Count(edge => edge.Start.X == 5 && edge.Start.Y == 5 || edge.End.X == 5 && edge.End.Y == 5), Is.EqualTo(4));
            Assert.That(result.Edges[0].SourceIndex, Is.EqualTo(1));
            Assert.That(result.Edges[1].SourceIndex, Is.EqualTo(1));
            Assert.That(result.Edges[3].SourceIndex, Is.EqualTo(3));
            Assert.That(result.Edges[4].SourceIndex, Is.EqualTo(3));
            Assert.That(result.Edges[5].SourceIndex, Is.EqualTo(4));
        });
    }

    [Test]
    public void SplitEdgesHandlesOverlapEndpointsWithoutRemovingMultiplicity()
    {
        System.Collections.Generic.List<SvgPlanarEdge> edges = new System.Collections.Generic.List<SvgPlanarEdge>();
        edges.Add(Edge(0, 0, 10, 0, 0));
        edges.Add(Edge(4, 0, 8, 0, 1));

        SvgPlanarSplitResult result = new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(edges, Options());

        Assert.Multiple(() =>
        {
            AssertSplitSuccess(result, 4);
            AssertEdge(result.Edges[0], 0, 0, 4, 0, 0, 0, 0.4);
            AssertEdge(result.Edges[1], 4, 0, 8, 0, 0, 0.4, 0.8);
            AssertEdge(result.Edges[2], 8, 0, 10, 0, 0, 0.8, 1);
            AssertEdge(result.Edges[3], 4, 0, 8, 0, 1, 0, 1);
        });
    }

    [Test]
    public void DirectedEdgesClassifyNonzeroEvenOddAndBoundaryWinding()
    {
        System.Collections.Generic.List<SvgPlanarEdge> nested = Square(0, 0, 10, 10, 0);
        nested.AddRange(Square(2, 2, 8, 8, 4));
        SvgPlanarOptions options = Options();

        SvgPlanarWindingResult nonzero = new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(nested, Point(5, 5), SvgFillRule.Nonzero, options);
        SvgPlanarWindingResult evenOdd = new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(nested, Point(5, 5), SvgFillRule.EvenOdd, options);
        SvgPlanarWindingResult boundary = new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(nested, Point(0, 5), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertWinding(nonzero, SvgFillContainmentKind.Inside, true, 2, 2);
            AssertWinding(evenOdd, SvgFillContainmentKind.Outside, false, 2, 2);
            AssertWinding(boundary, SvgFillContainmentKind.Boundary, true, 0, 0);
        });
    }

    [Test]
    public void WindingClassificationScalesHugeFiniteCoordinates()
    {
        System.Collections.Generic.List<SvgPlanarEdge> huge = Square(0, 0, 1e300, 1e300, 0);

        SvgPlanarWindingResult result = new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(huge, Point(5e299, 5e299), SvgFillRule.Nonzero, Options());

        AssertWinding(result, SvgFillContainmentKind.Inside, true, 1, 1);
    }

    [Test]
    public void UnorderedDirectedEdgesReconstructDeterministicClosedContours()
    {
        System.Collections.Generic.List<SvgPlanarEdge> edges = new System.Collections.Generic.List<SvgPlanarEdge>();
        edges.Add(Edge(7, 2, 5, 2, 6));
        edges.Add(Edge(2, 2, 0, 2, 2));
        edges.Add(Edge(5, 0, 7, 0, 4));
        edges.Add(Edge(0, 0, 2, 0, 0));
        edges.Add(Edge(5, 2, 5, 0, 7));
        edges.Add(Edge(2, 0, 2, 2, 1));
        edges.Add(Edge(7, 0, 7, 2, 5));
        edges.Add(Edge(0, 2, 0, 0, 3));

        SvgContourReconstructionResult result = new SvgPlanarGeometry().ReconstructContoursWithEdgesAndOptions(edges, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgPlanarError.None));
            Assert.That(result.Contours, Has.Count.EqualTo(2));
            Assert.That(result.Contours[0].Points, Has.Count.EqualTo(5));
            Assert.That(result.Contours[0].Points[0].X, Is.EqualTo(0));
            Assert.That(result.Contours[0].Points[0].Y, Is.EqualTo(0));
            Assert.That(result.Contours[0].SignedArea, Is.EqualTo(4));
            Assert.That(result.Contours[1].Points[0].X, Is.EqualTo(5));
            Assert.That(result.Contours[1].SignedArea, Is.EqualTo(4));
        });
    }

    [Test]
    public void InvalidInputsOpenContoursAndSecurityLimitsReturnStableFailures()
    {
        SvgPlanarOptions invalid = Options();
        invalid.Tolerance = double.NaN;
        SvgPlanarOptions comparisonLimit = Options();
        comparisonLimit.MaximumComparisons = 1;
        SvgPlanarOptions edgeLimit = Options();
        edgeLimit.MaximumEdges = 3;
        SvgPlanarOptions intersectionLimit = Options();
        intersectionLimit.MaximumIntersections = 1;
        SvgPlanarOptions depthLimit = Options();
        depthLimit.Tolerance = 0.00001;
        depthLimit.MaximumSubdivisionDepth = 0;
        SvgPlanarEdge mutated = Edge(0, 0, 1, 1, 0);
        mutated.Start.X = double.NaN;
        System.Collections.Generic.List<SvgPlanarEdge> crossing = new System.Collections.Generic.List<SvgPlanarEdge>();
        crossing.Add(Edge(0, 0, 10, 10, 0));
        crossing.Add(Edge(0, 10, 10, 0, 1));
        System.Collections.Generic.List<SvgPlanarEdge> open = new System.Collections.Generic.List<SvgPlanarEdge>();
        open.Add(Edge(0, 0, 1, 0, 0));

        Assert.Multiple(() =>
        {
            AssertSelfFailure(new SvgPlanarGeometry().FindSelfIntersectionsWithPathAndOptions(null, Options()), SvgPlanarError.InvalidArgument);
            AssertSelfFailure(Parse("M0 0 L10 10 L0 10 L10 0 Z").FindSelfIntersectionsWithOptions(comparisonLimit), SvgPlanarError.ComparisonLimitExceeded);
            AssertSelfFailure(Parse("M0 0 Q5 10 10 0").FindSelfIntersectionsWithOptions(depthLimit), SvgPlanarError.SubdivisionLimitExceeded);
            Assert.That(new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(crossing, edgeLimit).Error, Is.EqualTo(SvgPlanarError.EdgeLimitExceeded));
            Assert.That(new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(new System.Collections.Generic.List<SvgPlanarEdge> { Edge(0, 0, 10, 0, 0), Edge(4, 0, 8, 0, 1) }, intersectionLimit).Error, Is.EqualTo(SvgPlanarError.IntersectionLimitExceeded));
            Assert.That(new SvgPlanarGeometry().SplitEdgesWithEdgesAndOptions(new System.Collections.Generic.List<SvgPlanarEdge> { mutated }, Options()).Error, Is.EqualTo(SvgPlanarError.InvalidArgument));
            Assert.That(new SvgPlanarGeometry().ClassifyWindingWithEdgesAndPointAndFillRuleAndOptions(crossing, Point(1, 1), (SvgFillRule)99, Options()).Error, Is.EqualTo(SvgPlanarError.InvalidArgument));
            Assert.That(new SvgPlanarGeometry().ReconstructContoursWithEdgesAndOptions(open, Options()).Error, Is.EqualTo(SvgPlanarError.OpenContour));
            AssertSelfFailure(Parse("M0 0").FindSelfIntersectionsWithOptions(invalid), SvgPlanarError.InvalidArgument);
        });
    }

    static SvgPathData Parse(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 1000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPlanarOptions Options()
    {
        SvgPlanarOptions options = new SvgPlanarOptions();
        options.Tolerance = 0.000001;
        return options;
    }

    static SvgPoint Point(double x, double y) { return new SvgPoint(x, y); }
    static SvgPlanarEdge Edge(double x1, double y1, double x2, double y2, int source) { return new SvgPlanarEdge(Point(x1, y1), Point(x2, y2), source); }

    static System.Collections.Generic.List<SvgPlanarEdge> Square(double left, double top, double right, double bottom, int source)
    {
        System.Collections.Generic.List<SvgPlanarEdge> edges = new System.Collections.Generic.List<SvgPlanarEdge>();
        edges.Add(Edge(left, top, right, top, source));
        edges.Add(Edge(right, top, right, bottom, source + 1));
        edges.Add(Edge(right, bottom, left, bottom, source + 2));
        edges.Add(Edge(left, bottom, left, top, source + 3));
        return edges;
    }

    static void AssertSelfSuccess(SvgPathSelfIntersectionResult result, int count)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPlanarError.None));
        Assert.That(result.Intersections, Has.Count.EqualTo(count));
    }

    static void AssertSelfFailure(SvgPathSelfIntersectionResult result, SvgPlanarError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Intersections, Is.Null);
    }

    static void AssertSplitSuccess(SvgPlanarSplitResult result, int count)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPlanarError.None));
        Assert.That(result.Edges, Has.Count.EqualTo(count));
    }

    static void AssertEdge(SvgPlanarEdge edge, double x1, double y1, double x2, double y2, int source, double startAmount, double endAmount)
    {
        Assert.That(edge.Start.X, Is.EqualTo(x1).Within(0.000001));
        Assert.That(edge.Start.Y, Is.EqualTo(y1).Within(0.000001));
        Assert.That(edge.End.X, Is.EqualTo(x2).Within(0.000001));
        Assert.That(edge.End.Y, Is.EqualTo(y2).Within(0.000001));
        Assert.That(edge.SourceIndex, Is.EqualTo(source));
        Assert.That(edge.StartAmount, Is.EqualTo(startAmount).Within(0.000001));
        Assert.That(edge.EndAmount, Is.EqualTo(endAmount).Within(0.000001));
    }

    static void AssertWinding(SvgPlanarWindingResult result, SvgFillContainmentKind kind, bool contains, int winding, int crossings)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.Contains, Is.EqualTo(contains));
        Assert.That(result.WindingNumber, Is.EqualTo(winding));
        Assert.That(result.CrossingCount, Is.EqualTo(crossings));
    }
}
