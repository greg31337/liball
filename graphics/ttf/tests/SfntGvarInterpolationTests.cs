using ttf;

namespace ttf.Tests;

internal class SfntGvarInterpolationTests
{
    [Test]
    public void CalculatesNonIntermediateTupleScalarsAcrossAxes()
    {
        SfntGvarTupleVariation positive = CreateVariation(
            new double[] { 1, 0 },
            null,
            null,
            true,
            new int[0],
            new int[0]);
        SfntGvarTupleVariation negative = CreateVariation(
            new double[] { -1, 0 },
            null,
            null,
            true,
            new int[0],
            new int[0]);
        SfntGvarTupleVariation interiorPeak = CreateVariation(
            new double[] { 0.5, 0 },
            null,
            null,
            true,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(positive.CalculateScalar(new double[] { 0.25, -1 }),
                Is.EqualTo(0.25));
            Assert.That(positive.CalculateScalar(new double[] { 1, 0.75 }),
                Is.EqualTo(1));
            Assert.That(positive.CalculateScalar(new double[] { -0.1, 0 }),
                Is.Zero);
            Assert.That(negative.CalculateScalar(new double[] { -0.5, 1 }),
                Is.EqualTo(0.5));
            Assert.That(negative.CalculateScalar(new double[] { 0.1, 0 }),
                Is.Zero);
            Assert.That(interiorPeak.CalculateScalar(new double[] { 0.5, 0 }),
                Is.EqualTo(1));
            Assert.That(interiorPeak.CalculateScalar(new double[] { 0.75, 0 }),
                Is.Zero);
        });
    }

    [Test]
    public void CalculatesIntermediateScalarsAndIgnoresInvalidRegionAxes()
    {
        SfntGvarTupleVariation intermediate = CreateVariation(
            new double[] { 0.5, -0.5 },
            new double[] { 0.25, -1 },
            new double[] { 1, -0.25 },
            true,
            new int[0],
            new int[0]);
        SfntGvarTupleVariation invalidOrdering = CreateVariation(
            new double[] { 0.5, 0 },
            new double[] { 0.75, 0 },
            new double[] { 1, 0 },
            true,
            new int[0],
            new int[0]);
        SfntGvarTupleVariation crossesOrigin = CreateVariation(
            new double[] { 0.25, 0 },
            new double[] { -0.5, 0 },
            new double[] { 0.75, 0 },
            true,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(intermediate.CalculateScalar(new double[] { 0.5, -0.5 }),
                Is.EqualTo(1));
            Assert.That(intermediate.CalculateScalar(new double[] { 0.375, -0.375 }),
                Is.EqualTo(0.25));
            Assert.That(intermediate.CalculateScalar(new double[] { 0.2, -0.5 }),
                Is.Zero);
            Assert.That(intermediate.CalculateScalar(new double[] { 0.75, 0 }),
                Is.Zero);
            Assert.That(invalidOrdering.CalculateScalar(new double[] { -1, 1 }),
                Is.EqualTo(1));
            Assert.That(crossesOrigin.CalculateScalar(new double[] { -1, 1 }),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidatesTupleScalarCoordinates()
    {
        SfntGvarTupleVariation variation = CreateVariation(
            new double[] { 1, 0 },
            null,
            null,
            true,
            new int[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                variation.CalculateScalar(null!));
            Assert.Throws<ArgumentException>(() =>
                variation.CalculateScalar(new double[] { 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                variation.CalculateScalar(new double[] { double.NaN, 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                variation.CalculateScalar(new double[] { 1.01, 0 }));
        });
    }

    [Test]
    public void ExpandsSparseDeltasUsingContourLocalIupInterpolation()
    {
        SfntSimpleGlyph glyph = CreateGlyph();
        SfntGvarTupleVariation variation = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(
                false,
                new int[] { 1, 3, 6, 8, 10, 14 }),
            new int[] { 20, 60, 7, 5, -5, 30 },
            new int[] { -20, 20, -9, 4, 4, 40 });
        SfntGvarDeltaSet deltas = variation.CreateAllPointDeltas(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(deltas.GetPointCount(), Is.EqualTo(17));
            Assert.That(deltas.GetXDelta(0), Is.EqualTo(20));
            Assert.That(deltas.GetXDelta(1), Is.EqualTo(20));
            Assert.That(deltas.GetXDelta(2), Is.EqualTo(40));
            Assert.That(deltas.GetXDelta(3), Is.EqualTo(60));
            Assert.That(deltas.GetXDelta(4), Is.EqualTo(60));
            Assert.That(deltas.GetYDelta(0), Is.EqualTo(-20));
            Assert.That(deltas.GetYDelta(2), Is.Zero);
            Assert.That(deltas.GetYDelta(4), Is.EqualTo(20));
            Assert.That(deltas.GetXDelta(5), Is.EqualTo(7));
            Assert.That(deltas.GetXDelta(6), Is.EqualTo(7));
            Assert.That(deltas.GetXDelta(7), Is.EqualTo(7));
            Assert.That(deltas.GetYDelta(5), Is.EqualTo(-9));
            Assert.That(deltas.GetXDelta(9), Is.Zero);
            Assert.That(deltas.GetYDelta(9), Is.EqualTo(4));
            Assert.That(deltas.GetXDelta(11), Is.Zero);
            Assert.That(deltas.GetXDelta(12), Is.Zero);
            Assert.That(deltas.IsInferred(11), Is.False);
            Assert.That(deltas.IsInferred(12), Is.False);
            Assert.That(deltas.GetXDelta(13), Is.Zero);
            Assert.That(deltas.GetXDelta(14), Is.EqualTo(30));
            Assert.That(deltas.GetYDelta(14), Is.EqualTo(40));
            Assert.That(deltas.GetXDelta(15), Is.Zero);
            Assert.That(deltas.GetXDelta(16), Is.Zero);
            Assert.That(deltas.IsExplicit(1), Is.True);
            Assert.That(deltas.IsInferred(1), Is.False);
            Assert.That(deltas.IsExplicit(2), Is.False);
            Assert.That(deltas.IsInferred(2), Is.True);
            Assert.That(deltas.IsExplicit(14), Is.True);
            Assert.That(deltas.IsInferred(14), Is.False);
        });
    }

    [Test]
    public void PreservesAllPointDeltasWithoutInference()
    {
        SfntSimpleGlyph glyph = CreateGlyph();
        int[] xDeltas = new int[17];
        int[] yDeltas = new int[17];
        int index = 0;

        while (index < xDeltas.Length)
        {
            xDeltas[index] = index - 8;
            yDeltas[index] = 16 - index;
            index += 1;
        }

        SfntGvarTupleVariation variation = CreateVariation(
            new double[] { 1 },
            null,
            null,
            true,
            xDeltas,
            yDeltas);
        SfntGvarDeltaSet deltas = variation.CreateAllPointDeltas(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(deltas.GetXDelta(0), Is.EqualTo(-8));
            Assert.That(deltas.GetXDelta(8), Is.Zero);
            Assert.That(deltas.GetXDelta(16), Is.EqualTo(8));
            Assert.That(deltas.GetYDelta(0), Is.EqualTo(16));
            Assert.That(deltas.GetYDelta(16), Is.Zero);
            Assert.That(deltas.IsExplicit(0), Is.True);
            Assert.That(deltas.IsExplicit(16), Is.True);
            Assert.That(deltas.IsInferred(0), Is.False);
            Assert.That(deltas.IsInferred(16), Is.False);
        });
    }

    [Test]
    public void SparseDuplicatePointNumbersUseTheLastExplicitDelta()
    {
        SfntSimpleGlyph glyph = CreateGlyph();
        SfntGvarTupleVariation variation = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(false, new int[] { 6, 6 }),
            new int[] { 2, 9 },
            new int[] { -3, -7 });
        SfntGvarDeltaSet deltas = variation.CreateAllPointDeltas(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(deltas.GetXDelta(5), Is.EqualTo(9));
            Assert.That(deltas.GetXDelta(6), Is.EqualTo(9));
            Assert.That(deltas.GetXDelta(7), Is.EqualTo(9));
            Assert.That(deltas.GetYDelta(5), Is.EqualTo(-7));
            Assert.That(deltas.GetYDelta(6), Is.EqualTo(-7));
            Assert.That(deltas.GetYDelta(7), Is.EqualTo(-7));
        });
    }

    [Test]
    public void SingleTouchedPointPropagatesOnlyWithinItsContour()
    {
        SfntSimpleGlyph glyph = CreateGlyph();
        SfntGvarTupleVariation variation = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(false, new int[] { 2 }),
            new int[] { 17 },
            new int[] { -9 });

        SfntGvarDeltaSet deltas = variation.CreateAllPointDeltas(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(
                Enumerable.Range(0, 5).Select(deltas.GetXDelta),
                Is.All.EqualTo(17));
            Assert.That(
                Enumerable.Range(0, 5).Select(deltas.GetYDelta),
                Is.All.EqualTo(-9));
            Assert.That(deltas.IsExplicit(2), Is.True);
            Assert.That(deltas.IsInferred(0), Is.True);
            Assert.That(deltas.IsInferred(4), Is.True);
            Assert.That(deltas.GetXDelta(5), Is.Zero);
            Assert.That(deltas.GetYDelta(12), Is.Zero);
            Assert.That(deltas.GetXDelta(13), Is.Zero);
            Assert.That(deltas.GetYDelta(16), Is.Zero);
        });
    }

    [Test]
    public void ExpandsSparseCompositeSelectionsWithoutIupInference()
    {
        SfntGvarTupleVariation variation = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(false, new int[] { 0, 4 }),
            new int[] { 12, 30 },
            new int[] { -8, 40 });
        SfntGvarDeltaSet deltas =
            SfntGvarIupInterpolator.CreateSelectedPointDeltas(variation, 6);

        Assert.Multiple(() =>
        {
            Assert.That(deltas.GetPointCount(), Is.EqualTo(6));
            Assert.That(deltas.GetXDelta(0), Is.EqualTo(12));
            Assert.That(deltas.GetYDelta(0), Is.EqualTo(-8));
            Assert.That(deltas.GetXDelta(1), Is.Zero);
            Assert.That(deltas.GetYDelta(3), Is.Zero);
            Assert.That(deltas.GetXDelta(4), Is.EqualTo(30));
            Assert.That(deltas.GetYDelta(4), Is.EqualTo(40));
            Assert.That(deltas.IsExplicit(0), Is.True);
            Assert.That(deltas.IsExplicit(4), Is.True);
            Assert.That(deltas.IsInferred(1), Is.False);
            Assert.That(deltas.IsInferred(3), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntGvarIupInterpolator.CreateSelectedPointDeltas(variation, -1));
        });
    }

    [Test]
    public void RejectsInvalidDeltaSelectionsAndAccessorIndexes()
    {
        SfntSimpleGlyph glyph = CreateGlyph();
        SfntGvarTupleVariation countMismatch = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(false, new int[] { 1, 3 }),
            new int[] { 5 },
            new int[] { 6 });
        SfntGvarTupleVariation outsideGlyph = new SfntGvarTupleVariation(
            0,
            0,
            true,
            false,
            true,
            new SfntGvarTuple(new double[] { 1 }),
            null,
            null,
            new SfntGvarPointNumbers(false, new int[] { 17 }),
            new int[] { 5 },
            new int[] { 6 });
        SfntGvarDeltaSet valid = CreateVariation(
            new double[] { 1 },
            null,
            null,
            true,
            new int[17],
            new int[17]).CreateAllPointDeltas(glyph);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                countMismatch.CreateAllPointDeltas(glyph));
            Assert.Throws<FontFormatException>(() =>
                outsideGlyph.CreateAllPointDeltas(glyph));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGvarIupInterpolator.CreateAllPointDeltas(null!, glyph));
            Assert.Throws<ArgumentNullException>(() =>
                SfntGvarIupInterpolator.CreateAllPointDeltas(countMismatch, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.GetXDelta(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.GetYDelta(17));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.IsExplicit(17));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.IsInferred(-1));
        });
    }

    static SfntGvarTupleVariation CreateVariation(
        double[] peak,
        double[]? start,
        double[]? end,
        bool allPoints,
        int[] xDeltas,
        int[] yDeltas)
    {
        return new SfntGvarTupleVariation(
            0,
            0,
            true,
            start != null,
            !allPoints,
            new SfntGvarTuple(peak),
            start == null ? null : new SfntGvarTuple(start),
            end == null ? null : new SfntGvarTuple(end),
            new SfntGvarPointNumbers(allPoints, new int[0]),
            xDeltas,
            yDeltas);
    }

    static SfntSimpleGlyph CreateGlyph()
    {
        int[] xCoordinates = new int[]
        {
            0, 25, 50, 75, 100,
            0, 10, 20,
            10, 10, 10,
            30, 40
        };
        SfntSimpleGlyphPoint[] points = new SfntSimpleGlyphPoint[xCoordinates.Length];
        int index = 0;

        while (index < points.Length)
        {
            int y = index >= 8 && index <= 10 ? 10 : xCoordinates[index];
            points[index] = new SfntSimpleGlyphPoint(
                index,
                1,
                xCoordinates[index],
                y,
                0,
                0);
            index += 1;
        }

        return new SfntSimpleGlyph(
            3,
            new SfntGlyphHeader(4, 0, 0, 100, 100),
            new int[] { 4, 7, 10, 12 },
            new byte[0],
            points,
            false,
            0,
            100);
    }
}
