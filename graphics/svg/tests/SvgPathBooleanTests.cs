namespace svg.Tests;

public class SvgPathBooleanTests
{
    [Test]
    public void OverlappingRectanglesSupportEveryBooleanOperation()
    {
        SvgPathData first = Rect(0, 0, 10, 10);
        SvgPathData second = Rect(5, 0, 15, 10);

        SvgPathBooleanResult union = first.Boolean(second, SvgPathBooleanOperation.Union);
        SvgPathBooleanResult intersection = first.Boolean(second, SvgPathBooleanOperation.Intersection);
        SvgPathBooleanResult difference = first.Boolean(second, SvgPathBooleanOperation.Difference);
        SvgPathBooleanResult xor = first.Boolean(second, SvgPathBooleanOperation.Xor);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(union, 1);
            AssertFilled(union, 2, 5, true);
            AssertFilled(union, 7, 5, true);
            AssertFilled(union, 12, 5, true);
            AssertBooleanSuccess(intersection, 1);
            AssertFilled(intersection, 2, 5, false);
            AssertFilled(intersection, 7, 5, true);
            AssertFilled(intersection, 12, 5, false);
            AssertBooleanSuccess(difference, 1);
            AssertFilled(difference, 2, 5, true);
            AssertFilled(difference, 7, 5, false);
            AssertBooleanSuccess(xor, 2);
            AssertFilled(xor, 2, 5, true);
            AssertFilled(xor, 7, 5, false);
            AssertFilled(xor, 12, 5, true);
        });
    }

    [Test]
    public void IdenticalAndTouchingBoundariesAreDeduplicated()
    {
        SvgPathData square = Rect(0, 0, 10, 10);
        SvgPathData touching = Rect(10, 0, 20, 10);

        SvgPathBooleanResult unionSame = new SvgPathBoolean().Union(square, square);
        SvgPathBooleanResult intersectionSame = new SvgPathBoolean().Intersection(square, square);
        SvgPathBooleanResult differenceSame = new SvgPathBoolean().Difference(square, square);
        SvgPathBooleanResult xorSame = new SvgPathBoolean().ExclusiveOr(square, square);
        SvgPathBooleanResult touchingUnion = new SvgPathBoolean().Union(square, touching);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(unionSame, 1);
            AssertBooleanSuccess(intersectionSame, 1);
            AssertFilled(unionSame, 5, 5, true);
            AssertFilled(intersectionSame, 5, 5, true);
            AssertBooleanSuccess(differenceSame, 0);
            AssertBooleanSuccess(xorSame, 0);
            Assert.That(differenceSame.PathData.Commands, Is.Empty);
            Assert.That(xorSame.PathData.Commands, Is.Empty);
            AssertBooleanSuccess(touchingUnion, 1);
            AssertFilled(touchingUnion, 15, 5, true);
        });
    }

    [Test]
    public void ContainedDifferenceReconstructsAnOppositelyOrientedHole()
    {
        SvgPathBooleanResult result = new SvgPathBoolean().Difference(Rect(0, 0, 10, 10), Rect(2, 2, 8, 8));

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(result, 2);
            AssertFilled(result, 1, 1, true);
            AssertFilled(result, 5, 5, false);
            Assert.That(result.PathData.GetSubpathCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void EmptyOperandsPreserveBooleanIdentities()
    {
        SvgPathData empty = Parse("");
        SvgPathData square = Rect(0, 0, 10, 10);

        SvgPathBooleanResult union = new SvgPathBoolean().Union(empty, square);
        SvgPathBooleanResult intersection = new SvgPathBoolean().Intersection(empty, square);
        SvgPathBooleanResult difference = new SvgPathBoolean().Difference(square, empty);
        SvgPathBooleanResult xor = new SvgPathBoolean().ExclusiveOr(square, empty);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(union, 1);
            AssertBooleanSuccess(intersection, 0);
            AssertBooleanSuccess(difference, 1);
            AssertBooleanSuccess(xor, 1);
            AssertFilled(union, 5, 5, true);
            AssertFilled(difference, 5, 5, true);
            AssertFilled(xor, 5, 5, true);
        });
    }

    [Test]
    public void OperandFillRulesAreAppliedIndependently()
    {
        SvgPathData nestedSameDirection = Parse("M0 0 L10 0 L10 10 L0 10 Z M2 2 L8 2 L8 8 L2 8 Z");
        SvgPathData center = Rect(3, 3, 7, 7);
        SvgPathBooleanOptions options = Options();

        SvgPathBooleanResult nonzero = new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(nestedSameDirection, center, SvgPathBooleanOperation.Intersection, SvgFillRule.Nonzero, SvgFillRule.Nonzero, options);
        SvgPathBooleanResult evenOdd = new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(nestedSameDirection, center, SvgPathBooleanOperation.Intersection, SvgFillRule.EvenOdd, SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(nonzero, 1);
            AssertFilled(nonzero, 5, 5, true);
            AssertBooleanSuccess(evenOdd, 0);
            AssertFilled(evenOdd, 5, 5, false);
        });
    }

    [Test]
    public void ClippingMatchesIntersectionForExplicitRules()
    {
        SvgPathData subject = Rect(0, 0, 10, 10);
        SvgPathData clip = Rect(4, -2, 8, 12);
        SvgPathBooleanOptions options = Options();

        SvgPathBooleanResult clipped = new SvgPathBoolean().Clip(subject, clip, SvgFillRule.Nonzero, SvgFillRule.EvenOdd, options);
        SvgPathBooleanResult intersection = new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(subject, clip, SvgPathBooleanOperation.Intersection, SvgFillRule.Nonzero, SvgFillRule.EvenOdd, options);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(clipped, 1);
            AssertFilled(clipped, 6, 5, true);
            AssertFilled(clipped, 2, 5, false);
            Assert.That(clipped.PathData.ToMinifiedText(), Is.EqualTo(intersection.PathData.ToMinifiedText()));
        });
    }

    [Test]
    public void CurvesAndArcsAreFlattenedWithinConfiguredTolerance()
    {
        SvgPathData circle = Parse("M10 0 A10 10 0 1 1 -10 0 A10 10 0 1 1 10 0 Z");
        SvgPathData strip = Rect(-5, -12, 5, 12);
        SvgPathBooleanOptions options = Options();
        options.Tolerance = 0.001;

        SvgPathBooleanResult result = new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(circle, strip, SvgPathBooleanOperation.Intersection, SvgFillRule.Nonzero, SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(result, 1);
            AssertFilled(result, 0, 0, true);
            AssertFilled(result, 6, 0, false);
            AssertFilled(result, 0, 11, false);
            Assert.That(result.EdgeCount, Is.GreaterThan(4));
        });
    }

    [Test]
    public void DisjointUnionOrderingIsDeterministicAcrossOperandOrder()
    {
        SvgPathData left = Rect(0, 0, 2, 2);
        SvgPathData right = Rect(10, 0, 12, 2);

        SvgPathBooleanResult forward = new SvgPathBoolean().Union(left, right);
        SvgPathBooleanResult reverse = new SvgPathBoolean().Union(right, left);

        Assert.Multiple(() =>
        {
            AssertBooleanSuccess(forward, 2);
            AssertBooleanSuccess(reverse, 2);
            Assert.That(forward.PathData.ToMinifiedText(), Is.EqualTo(reverse.PathData.ToMinifiedText()));
            Assert.That(forward.PathData.Commands[0].EndX, Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidInputsMutationAndLimitsReturnStableFailures()
    {
        SvgPathData square = Rect(0, 0, 10, 10);
        SvgPathData curve = Parse("M0 0 Q5 10 10 0 Z");
        SvgPathData mutated = Rect(0, 0, 10, 10);
        mutated.Commands[1].Values[0] = double.NaN;
        SvgPathBooleanOptions invalidTolerance = Options();
        invalidTolerance.Tolerance = double.NaN;
        SvgPathBooleanOptions edgeLimit = Options();
        edgeLimit.MaximumEdges = 4;
        SvgPathBooleanOptions comparisonLimit = Options();
        comparisonLimit.MaximumComparisons = 1;
        SvgPathBooleanOptions intersectionLimit = Options();
        intersectionLimit.MaximumIntersections = 1;
        SvgPathBooleanOptions depthLimit = Options();
        depthLimit.Tolerance = 0.00001;
        depthLimit.MaximumSubdivisionDepth = 0;

        Assert.Multiple(() =>
        {
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(null, square, SvgPathBooleanOperation.Union), SvgPathBooleanError.InvalidArgument);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(square, square, (SvgPathBooleanOperation)99), SvgPathBooleanError.InvalidArgument);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(square, square, SvgPathBooleanOperation.Union, (SvgFillRule)99, SvgFillRule.Nonzero, Options()), SvgPathBooleanError.InvalidArgument);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(square, square, SvgPathBooleanOperation.Union, SvgFillRule.Nonzero, SvgFillRule.Nonzero, invalidTolerance), SvgPathBooleanError.InvalidArgument);
            AssertBooleanFailure(new SvgPathBoolean().Union(mutated, square), SvgPathBooleanError.InvalidPath);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(square, square, SvgPathBooleanOperation.Union, SvgFillRule.Nonzero, SvgFillRule.Nonzero, edgeLimit), SvgPathBooleanError.EdgeLimitExceeded);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(square, square, SvgPathBooleanOperation.Union, SvgFillRule.Nonzero, SvgFillRule.Nonzero, comparisonLimit), SvgPathBooleanError.ComparisonLimitExceeded);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(square, square, SvgPathBooleanOperation.Union, SvgFillRule.Nonzero, SvgFillRule.Nonzero, intersectionLimit), SvgPathBooleanError.IntersectionLimitExceeded);
            AssertBooleanFailure(new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(curve, square, SvgPathBooleanOperation.Union, SvgFillRule.Nonzero, SvgFillRule.Nonzero, depthLimit), SvgPathBooleanError.SubdivisionLimitExceeded);
        });
    }

    static SvgPathData Rect(double left, double top, double right, double bottom)
    {
        return Parse("M" + left + " " + top + " L" + right + " " + top + " L" + right + " " + bottom + " L" + left + " " + bottom + " Z");
    }

    static SvgPathData Parse(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 10000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPathBooleanOptions Options()
    {
        SvgPathBooleanOptions options = new SvgPathBooleanOptions();
        options.Tolerance = 0.0001;
        return options;
    }

    static void AssertBooleanSuccess(SvgPathBooleanResult result, int contours)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathBooleanError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.ContourCount, Is.EqualTo(contours));
    }

    static void AssertBooleanFailure(SvgPathBooleanResult result, SvgPathBooleanError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }

    static void AssertFilled(SvgPathBooleanResult result, double x, double y, bool expected)
    {
        SvgFillContainmentOptions options = new SvgFillContainmentOptions();
        options.Tolerance = 0.00001;
        SvgFillContainmentResult containment = result.PathData.ClassifyFillWithOptions(new SvgPoint(x, y), SvgFillRule.Nonzero, options);
        Assert.That(containment.Success, Is.True);
        Assert.That(containment.Contains, Is.EqualTo(expected));
    }
}
