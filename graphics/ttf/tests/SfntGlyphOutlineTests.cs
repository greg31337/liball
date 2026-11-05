using ttf;

namespace ttf.Tests;

internal class SfntGlyphOutlineTests
{
    [Test]
    public void EmitsMoveLinesAndCloseForAllOnCurveContour()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, true),
            Point(1, 10, 0, true),
            Point(2, 5, 10, true)
        });
        SfntGlyphOutline outline = glyph.GetNormalizedOutline();
        SfntOutlineContour contour = outline.GetContour(0);

        Assert.Multiple(() =>
        {
            Assert.That(outline.GetGlyphId(), Is.EqualTo(9));
            Assert.That(outline.GetContourCount(), Is.EqualTo(1));
            Assert.That(outline.GetNormalizedPointCount(), Is.EqualTo(3));
            Assert.That(outline.GetImpliedPointCount(), Is.Zero);
            Assert.That(outline.GetCommandCount(), Is.EqualTo(4));
            Assert.That(contour.GetPointCount(), Is.EqualTo(3));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(4));
            Assert.That(contour.GetImpliedPointCount(), Is.Zero);
        });

        AssertEnd(contour.GetCommand(0), SfntPathCommandKind.Move, 0, 0);
        AssertEnd(contour.GetCommand(1), SfntPathCommandKind.Line, 10, 0);
        AssertEnd(contour.GetCommand(2), SfntPathCommandKind.Line, 5, 10);
        Assert.That(contour.GetCommand(3).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
    }

    [Test]
    public void InsertsMidpointBetweenConsecutiveOffCurvePoints()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, true),
            Point(1, 5, 5, false),
            Point(2, 6, 8, false),
            Point(3, 10, 10, true)
        });
        SfntOutlineContour contour = glyph.GetNormalizedOutline().GetContour(0);
        SfntOutlinePoint implied = contour.GetPoint(2);

        Assert.Multiple(() =>
        {
            Assert.That(contour.GetPointCount(), Is.EqualTo(5));
            Assert.That(contour.GetImpliedPointCount(), Is.EqualTo(1));
            Assert.That(implied.GetX(), Is.EqualTo(5.5));
            Assert.That(implied.GetY(), Is.EqualTo(6.5));
            Assert.That(implied.IsOnCurve(), Is.True);
            Assert.That(implied.IsImplied(), Is.True);
            Assert.That(implied.HasSourcePoint(), Is.False);
            Assert.That(contour.GetPoint(1).GetSourcePointIndex(), Is.EqualTo(1));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(4));
        });

        AssertEnd(contour.GetCommand(0), SfntPathCommandKind.Move, 0, 0);
        AssertQuadratic(contour.GetCommand(1), 5, 5, 5.5, 6.5);
        AssertQuadratic(contour.GetCommand(2), 6, 8, 10, 10);
        Assert.That(contour.GetCommand(3).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
    }

    [Test]
    public void UsesCyclicImpliedStartWhenFirstAndLastPointsAreOffCurve()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 1, false),
            Point(1, 10, 10, true),
            Point(2, 11, 0, false)
        });
        SfntOutlineContour contour = glyph.GetNormalizedOutline().GetContour(0);
        SfntOutlinePoint start = contour.GetPoint(0);

        Assert.Multiple(() =>
        {
            Assert.That(start.GetX(), Is.EqualTo(5.5));
            Assert.That(start.GetY(), Is.EqualTo(0.5));
            Assert.That(start.IsImplied(), Is.True);
            Assert.That(contour.GetPoint(1).GetSourcePointIndex(), Is.Zero);
            Assert.That(contour.GetPoint(2).GetSourcePointIndex(), Is.EqualTo(1));
            Assert.That(contour.GetPoint(3).GetSourcePointIndex(), Is.EqualTo(2));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(4));
        });

        AssertEnd(contour.GetCommand(0), SfntPathCommandKind.Move, 5.5, 0.5);
        AssertQuadratic(contour.GetCommand(1), 0, 1, 10, 10);
        AssertQuadratic(contour.GetCommand(2), 11, 0, 5.5, 0.5);
        Assert.That(contour.GetCommand(3).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
    }

    [Test]
    public void RotatesContourToLastPointWhenOnlyLastEndpointIsOnCurve()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, false),
            Point(1, 10, 5, true),
            Point(2, 20, 0, true)
        });
        SfntOutlineContour contour = glyph.GetNormalizedOutline().GetContour(0);

        Assert.Multiple(() =>
        {
            Assert.That(contour.GetPointCount(), Is.EqualTo(3));
            Assert.That(contour.GetPoint(0).GetSourcePointIndex(), Is.EqualTo(2));
            Assert.That(contour.GetPoint(1).GetSourcePointIndex(), Is.Zero);
            Assert.That(contour.GetPoint(2).GetSourcePointIndex(), Is.EqualTo(1));
            Assert.That(contour.GetImpliedPointCount(), Is.Zero);
            Assert.That(contour.GetCommandCount(), Is.EqualTo(3));
        });

        AssertEnd(contour.GetCommand(0), SfntPathCommandKind.Move, 20, 0);
        AssertQuadratic(contour.GetCommand(1), 0, 0, 10, 5);
        Assert.That(contour.GetCommand(2).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
    }

    [Test]
    public void InsertsEveryCyclicMidpointForAllOffCurveContour()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, false),
            Point(1, 9, 3, false),
            Point(2, 3, 9, false)
        });
        SfntGlyphOutline outline = glyph.GetNormalizedOutline();
        SfntOutlineContour contour = outline.GetContour(0);

        Assert.Multiple(() =>
        {
            Assert.That(contour.GetPointCount(), Is.EqualTo(6));
            Assert.That(contour.GetImpliedPointCount(), Is.EqualTo(3));
            Assert.That(outline.GetImpliedPointCount(), Is.EqualTo(3));
            Assert.That(contour.GetPoint(0).GetX(), Is.EqualTo(1.5));
            Assert.That(contour.GetPoint(0).GetY(), Is.EqualTo(4.5));
            Assert.That(contour.GetPoint(2).GetX(), Is.EqualTo(4.5));
            Assert.That(contour.GetPoint(2).GetY(), Is.EqualTo(1.5));
            Assert.That(contour.GetPoint(4).GetX(), Is.EqualTo(6));
            Assert.That(contour.GetPoint(4).GetY(), Is.EqualTo(6));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(5));
        });

        AssertQuadratic(contour.GetCommand(1), 0, 0, 4.5, 1.5);
        AssertQuadratic(contour.GetCommand(2), 9, 3, 6, 6);
        AssertQuadratic(contour.GetCommand(3), 3, 9, 1.5, 4.5);
    }

    [Test]
    public void CalculatesExactExtremaForCyclicTwoPointOffCurveContour()
    {
        SfntGlyphOutline outline = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, -3, 5, false),
            Point(1, 4, -2, false)
        }).GetNormalizedOutline();
        SfntOutlineContour contour = outline.GetContour(0);
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(contour.GetPointCount(), Is.EqualTo(4));
            Assert.That(contour.GetImpliedPointCount(), Is.EqualTo(2));
            Assert.That(contour.GetPoint(0).GetX(), Is.EqualTo(0.5));
            Assert.That(contour.GetPoint(0).GetY(), Is.EqualTo(1.5));
            Assert.That(contour.GetPoint(2).GetX(), Is.EqualTo(0.5));
            Assert.That(contour.GetPoint(2).GetY(), Is.EqualTo(1.5));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(4));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-1.25));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-0.25));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(2.25));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(3.25));
        });

        AssertQuadratic(contour.GetCommand(1), -3, 5, 0.5, 1.5);
        AssertQuadratic(contour.GetCommand(2), 4, -2, 0.5, 1.5);
    }

    [Test]
    public void KeepsContoursAndFlattenedCommandsInSourceOrder()
    {
        SfntSimpleGlyph glyph = CreateGlyph(
            new SfntSimpleGlyphPoint[]
            {
                Point(0, 0, 0, true),
                Point(1, 5, 0, true)
            },
            new SfntSimpleGlyphPoint[]
            {
                Point(2, 10, 10, true),
                Point(3, 15, 10, true),
                Point(4, 15, 15, true)
            });
        SfntGlyphOutline outline = glyph.GetNormalizedOutline();

        Assert.Multiple(() =>
        {
            Assert.That(outline.GetContourCount(), Is.EqualTo(2));
            Assert.That(outline.GetNormalizedPointCount(), Is.EqualTo(5));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(7));
            Assert.That(outline.GetContour(0).GetCommandCount(), Is.EqualTo(3));
            Assert.That(outline.GetContour(1).GetCommandCount(), Is.EqualTo(4));
            Assert.That(outline.GetCommand(0).GetKind(), Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(outline.GetCommand(2).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(outline.GetCommand(3).GetKind(), Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(outline.GetCommand(6).GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
        });
    }

    [Test]
    public void HandlesSinglePointAndEmptyDegenerateContours()
    {
        SfntGlyphOutline oneOnCurve = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 4, 7, true)
        }).GetNormalizedOutline();
        SfntGlyphOutline oneOffCurve = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 4, 7, false)
        }).GetNormalizedOutline();
        SfntGlyphOutline empty = CreateGlyph().GetNormalizedOutline();

        Assert.Multiple(() =>
        {
            Assert.That(oneOnCurve.GetContour(0).GetCommandCount(), Is.EqualTo(2));
            Assert.That(oneOnCurve.GetContour(0).GetCommand(0).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(oneOnCurve.GetContour(0).GetCommand(1).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(oneOffCurve.GetContour(0).GetPointCount(), Is.EqualTo(2));
            Assert.That(oneOffCurve.GetContour(0).GetImpliedPointCount(), Is.EqualTo(1));
            Assert.That(oneOffCurve.GetContour(0).GetCommandCount(), Is.EqualTo(3));
            Assert.That(oneOffCurve.GetContour(0).GetCommand(1).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Quadratic));
            Assert.That(empty.GetContourCount(), Is.Zero);
            Assert.That(empty.GetCommandCount(), Is.Zero);
            Assert.That(empty.GetNormalizedPointCount(), Is.Zero);
        });
    }

    [Test]
    public void RetainsRepeatedCoincidentPointsInDegenerateContour()
    {
        SfntGlyphOutline outline = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 5, -7, true),
            Point(1, 5, -7, true),
            Point(2, 5, -7, true)
        }).GetNormalizedOutline();
        SfntOutlineContour contour = outline.GetContour(0);
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(contour.GetPointCount(), Is.EqualTo(3));
            Assert.That(contour.GetCommandCount(), Is.EqualTo(4));
            Assert.That(contour.GetCommand(0).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(contour.GetCommand(1).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Line));
            Assert.That(contour.GetCommand(2).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Line));
            Assert.That(contour.GetCommand(3).GetKind(),
                Is.EqualTo(SfntPathCommandKind.Close));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(5));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(5));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-7));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(-7));
        });
    }

    [Test]
    public void CachesNormalizedOutlineOnSimpleGlyph()
    {
        SfntSimpleGlyph glyph = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, true),
            Point(1, 1, 1, true)
        });

        SfntGlyphOutline first = glyph.GetNormalizedOutline();
        SfntGlyphOutline second = glyph.GetNormalizedOutline();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void EnforcesResolvedPointLimitBeforeAddingImpliedPoints()
    {
        SfntSimpleGlyphPoint[] points = new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, false),
            Point(1, 9, 3, false),
            Point(2, 3, 9, false)
        };
        SfntSimpleGlyph glyph = CreateGlyphWithPointLimit(5, points);

        Assert.Throws<FontLimitException>(() => glyph.GetNormalizedOutline());
    }

    [Test]
    public void AccessorsRejectInvalidIndicesAndInapplicableCoordinates()
    {
        SfntGlyphOutline outline = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, true),
            Point(1, 5, 5, false),
            Point(2, 10, 10, true)
        }).GetNormalizedOutline();
        SfntOutlineContour contour = outline.GetContour(0);
        SfntPathCommand move = contour.GetCommand(0);
        SfntPathCommand quadratic = contour.GetCommand(1);
        SfntPathCommand close = contour.GetCommand(2);
        SfntOutlinePoint implied = CreateGlyph(new SfntSimpleGlyphPoint[]
        {
            Point(0, 0, 0, false)
        }).GetNormalizedOutline().GetContour(0).GetPoint(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => outline.GetContour(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => outline.GetContour(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => outline.GetCommand(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => outline.GetCommand(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => contour.GetPoint(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => contour.GetPoint(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => contour.GetCommand(3));
            Assert.Throws<InvalidOperationException>(() => move.GetControlX());
            Assert.Throws<InvalidOperationException>(() => close.GetEndX());
            Assert.Throws<InvalidOperationException>(() => close.GetEndY());
            Assert.Throws<InvalidOperationException>(() => implied.GetSourcePointIndex());
            Assert.That(quadratic.GetControlX(), Is.EqualTo(5));
        });
    }

    [Test]
    public void NormalizerRejectsNullGlyph()
    {
        Assert.Throws<ArgumentNullException>(() => SfntSimpleGlyphNormalizer.Normalize(null!));
    }

    static void AssertEnd(
        SfntPathCommand command,
        SfntPathCommandKind kind,
        double x,
        double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(kind));
            Assert.That(command.GetEndX(), Is.EqualTo(x));
            Assert.That(command.GetEndY(), Is.EqualTo(y));
        });
    }

    static void AssertQuadratic(
        SfntPathCommand command,
        double controlX,
        double controlY,
        double endX,
        double endY)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(SfntPathCommandKind.Quadratic));
            Assert.That(command.GetControlX(), Is.EqualTo(controlX));
            Assert.That(command.GetControlY(), Is.EqualTo(controlY));
            Assert.That(command.GetEndX(), Is.EqualTo(endX));
            Assert.That(command.GetEndY(), Is.EqualTo(endY));
        });
    }

    static SfntSimpleGlyph CreateGlyph(params SfntSimpleGlyphPoint[][] contours)
    {
        List<SfntSimpleGlyphPoint> points = new List<SfntSimpleGlyphPoint>();
        int[] contourEndPoints = new int[contours.Length];
        int contourIndex = 0;

        while (contourIndex < contours.Length)
        {
            points.AddRange(contours[contourIndex]);
            contourEndPoints[contourIndex] = points.Count - 1;
            contourIndex += 1;
        }

        return new SfntSimpleGlyph(
            9,
            new SfntGlyphHeader(contours.Length, 0, 0, 20, 20),
            contourEndPoints,
            Array.Empty<byte>(),
            points.ToArray(),
            false,
            0,
            int.MaxValue);
    }

    static SfntSimpleGlyph CreateGlyphWithPointLimit(
        int pointLimit,
        SfntSimpleGlyphPoint[] points)
    {
        return new SfntSimpleGlyph(
            9,
            new SfntGlyphHeader(1, 0, 0, 20, 20),
            new int[] { points.Length - 1 },
            Array.Empty<byte>(),
            points,
            false,
            0,
            pointLimit);
    }

    static SfntSimpleGlyphPoint Point(
        int index,
        int x,
        int y,
        bool onCurve)
    {
        return new SfntSimpleGlyphPoint(
            index,
            onCurve ? 1 : 0,
            x,
            y,
            0,
            0);
    }
}
