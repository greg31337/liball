using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PerspectiveTransformTests
{
    [Test]
    public void MapsIdentityWithoutRetainingOrChangingCallerCorners()
    {
        QrQuadrilateral corners = Rectangle(-7, 4, 19, 11);
        PerspectiveTransform transform = PerspectiveTransform.Create(corners, corners);
        QrPoint point = new QrPoint { X = 3, Y = 8 };
        QrPoint mapped = transform.MapPoint(point);

        AssertPoint(mapped, 3, 8);
        Assert.That(mapped, Is.Not.SameAs(point));
        AssertPoint(corners.TopLeft!, -7, 4);
        AssertPoint(corners.BottomRight!, 12, 15);
        corners.TopLeft!.X = 900;
        AssertPoint(transform.MapPoint(point), 3, 8);
        AssertPoint(point, 3, 8);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MapsAffineRotationSkewTranslationAndEitherWinding(bool mirror)
    {
        QrQuadrilateral source = Rectangle(0, 0, 1, 1);
        double direction = mirror ? -1 : 1;
        QrQuadrilateral destination = Quad(
            8, -3, 8, -3 + direction * 4,
            6, -2 + direction * 4, 6, -2);
        PerspectiveTransform transform = PerspectiveTransform.Create(source, destination);
        AssertCorners(transform, source, destination);
        AssertPoint(transform.MapPoint(new QrPoint { X = 0.25, Y = 0.75 }),
            6.5, -2.25 + direction);
        AssertPoint(transform.Invert().MapPoint(new QrPoint
        {
            X = 6.5, Y = -2.25 + direction
        }), 0.25, 0.75);
    }

    [Test]
    public void MapsKnownProjectiveInteriorAndInvertsIt()
    {
        QrQuadrilateral source = Rectangle(0, 0, 1, 1);
        QrQuadrilateral destination = Quad(
            3, 5, 50.0 / 11, 40.0 / 11,
            60.0 / 13, 70.0 / 13, 10.0 / 3, 20.0 / 3);
        PerspectiveTransform transform = PerspectiveTransform.Create(source, destination);
        PerspectiveTransform inverse = transform.Invert();
        PerspectiveTransform reverse = PerspectiveTransform.Create(destination, source);
        AssertCorners(transform, source, destination);
        AssertCorners(inverse, destination, source);

        for (int row = 0; row <= 10; row++)
        {
            for (int column = 0; column <= 10; column++)
            {
                double x = column / 10.0;
                double y = row / 10.0;
                // Independent analytic homography used to construct the fixture.
                double denominator = x + 2 * y + 10;
                double expectedX = (20 * x + 10 * y + 30) / denominator;
                double expectedY = (-10 * x + 30 * y + 50) / denominator;
                QrPoint mapped = transform.MapPoint(new QrPoint { X = x, Y = y });
                AssertPoint(mapped, expectedX, expectedY);
                AssertPoint(inverse.MapPoint(mapped), x, y);
                AssertPoint(reverse.MapPoint(mapped), x, y);
            }
        }
    }

    [Test]
    public void MapsBetweenTwoNonAffineQuadrilaterals()
    {
        QrQuadrilateral source = Quad(-2, -1, 3, -2, 4, 2, -1, 3);
        QrQuadrilateral destination = Quad(10, 20, 110, 15, 95, 80, 20, 95);
        PerspectiveTransform transform = PerspectiveTransform.Create(source, destination);
        PerspectiveTransform inverse = transform.Invert();
        AssertCorners(transform, source, destination);
        AssertCorners(inverse, destination, source);
        for (int row = 0; row < 10; row++)
        {
            for (int column = 0; column < 10; column++)
            {
                QrPoint point = new QrPoint { X = column / 10.0, Y = row / 10.0 };
                AssertPoint(inverse.MapPoint(transform.MapPoint(point)), point.X, point.Y);
            }
        }
    }

    [TestCase(1E160)]
    [TestCase(1E-160)]
    public void GeometryChecksDoNotOverflowOrUnderflowWithCoordinateUnits(double scale)
    {
        QrQuadrilateral source = Rectangle(0, 0, scale, scale);
        QrQuadrilateral destination = Rectangle(0, 0, 2 * scale, 3 * scale);
        PerspectiveTransform transform = PerspectiveTransform.Create(source, destination);
        QrPoint point = new QrPoint { X = scale * 0.25, Y = scale * 0.75 };
        QrPoint mapped = transform.MapPoint(point);
        AssertPoint(new QrPoint { X = mapped.X / scale, Y = mapped.Y / scale }, 0.5, 2.25);
        QrPoint restored = transform.Invert().MapPoint(mapped);
        AssertPoint(new QrPoint { X = restored.X / scale, Y = restored.Y / scale }, 0.25, 0.75);
    }

    [TestCase(1E200)]
    [TestCase(1E-200)]
    [TestCase(-1E200)]
    public void InvertsEquivalentCoefficientScalesWithoutMutatingTransform(double scale)
    {
        PerspectiveTransform transform = new PerspectiveTransform
        {
            M00 = 2 * scale, M01 = scale, M02 = 3 * scale,
            M10 = -scale, M11 = 3 * scale, M12 = 5 * scale,
            M20 = 0.1 * scale, M21 = 0.2 * scale, M22 = scale
        };
        PerspectiveTransform inverse = transform.Invert();
        AssertPoint(inverse.MapPoint(new QrPoint { X = 3, Y = 5 }), 0, 0);
        AssertPoint(inverse.MapPoint(new QrPoint { X = 60.0 / 13, Y = 70.0 / 13 }), 1, 1);
        Assert.That(transform.M00, Is.EqualTo(2 * scale));
        Assert.That(transform.M22, Is.EqualTo(scale));
    }

    [Test]
    public void RejectsMissingQuadrilateralsAndEveryMissingCorner()
    {
        QrQuadrilateral valid = Rectangle(0, 0, 1, 1);
        AssertInvalidQuad(null!, valid);
        for (int corner = 0; corner < 4; corner++)
        {
            QrQuadrilateral invalid = Rectangle(0, 0, 1, 1);
            switch (corner)
            {
                case 0: invalid.TopLeft = null; break;
                case 1: invalid.TopRight = null; break;
                case 2: invalid.BottomRight = null; break;
                case 3: invalid.BottomLeft = null; break;
            }
            AssertInvalidQuad(invalid, valid);
        }
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void RejectsEveryNonFiniteCornerCoordinateAndMappingInput(double value)
    {
        QrQuadrilateral valid = Rectangle(0, 0, 1, 1);
        for (int corner = 0; corner < 4; corner++)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                QrQuadrilateral invalid = Rectangle(0, 0, 1, 1);
                QrPoint point = Corners(invalid)[corner];
                if (axis == 0) point.X = value;
                else point.Y = value;
                AssertInvalidQuad(invalid, valid);
            }
        }
        PerspectiveTransform transform = PerspectiveTransform.Create(valid, valid);
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { X = value }));
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { Y = value }));
        Assert.Throws<QrInputException>(() => transform.MapPoint(null!));
    }

    [Test]
    public void RejectsCollapsedCollinearConcaveCrossedAndNumericallyDegenerateGeometry()
    {
        QrQuadrilateral[] invalid =
        [
            Rectangle(0, 0, 0, 0),
            Quad(0, 0, 1, 0, 1, 0, 0, 1),
            Quad(0, 0, 1, 0, 2, 0, 0, 1),
            Quad(0, 0, 1, 1, 2, 2, 3, 3),
            Quad(0, 0, 2, 0, 0.5, 0.5, 0, 2),
            Quad(0, 0, 1, 1, 1, 0, 0, 1),
            Rectangle(0, 0, 1, 1E-14),
            Quad(-double.MaxValue, 0, double.MaxValue, 0,
                double.MaxValue, 1, -double.MaxValue, 1)
        ];
        foreach (QrQuadrilateral quad in invalid)
            AssertInvalidQuad(quad, Rectangle(0, 0, 1, 1));
    }

    [Test]
    public void RejectsSingularAndNonFiniteTransformInversion()
    {
        PerspectiveTransform[] invalid =
        [
            new PerspectiveTransform(),
            new PerspectiveTransform { M00 = 1, M11 = 1 },
            new PerspectiveTransform { M00 = 1, M01 = 2, M10 = 2, M11 = 4, M22 = 1 },
            new PerspectiveTransform { M00 = 1, M01 = 1, M10 = 1, M11 = 1 + 1E-15, M22 = 1 },
            new PerspectiveTransform { M00 = double.NaN, M11 = 1, M22 = 1 },
            new PerspectiveTransform { M00 = 1, M12 = double.PositiveInfinity, M22 = 1 },
            new PerspectiveTransform { M00 = 1, M11 = 1, M21 = double.NegativeInfinity }
        ];
        foreach (PerspectiveTransform transform in invalid)
            Assert.Throws<QrInputException>(() => transform.Invert());
    }

    [Test]
    public void RejectsProjectiveHorizonAndNonFiniteMappedCoordinates()
    {
        PerspectiveTransform transform = new PerspectiveTransform
        {
            M00 = 1, M11 = 1, M20 = 1, M22 = 1
        };
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { X = -1 }));
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { X = -1 + 1E-15 }));
        AssertPoint(transform.MapPoint(new QrPoint { X = -2, Y = 1 }), 2, -1);
        transform.M00 = double.MaxValue;
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { X = 2 }));
        transform.M20 = double.MaxValue;
        Assert.Throws<QrInputException>(() => transform.MapPoint(new QrPoint { X = 2 }));
        Assert.Throws<QrInputException>(() => new PerspectiveTransform().MapPoint(new QrPoint()));
    }

    [Test]
    public void ExceptionlessMappingReusesOutputAndPreservesItOnFailure()
    {
        PerspectiveTransform transform = new PerspectiveTransform
        {
            M00 = 1, M11 = 1, M20 = 1, M22 = 1
        };
        QrPoint mapped = new QrPoint();
        Assert.That(transform.TryMapPoint(1, 4, mapped), Is.True);
        AssertPoint(mapped, 0.5, 2);
        foreach (double x in new[] { -1, -1 + 1E-15, double.NaN, double.PositiveInfinity })
        {
            Assert.That(transform.TryMapPoint(x, 1, mapped), Is.False);
            AssertPoint(mapped, 0.5, 2);
        }
        Assert.That(transform.TryMapPoint(1, double.NegativeInfinity, mapped), Is.False);
        AssertPoint(mapped, 0.5, 2);
        transform.M11 = double.MaxValue;
        Assert.That(transform.TryMapPoint(1, 4, mapped), Is.False);
        AssertPoint(mapped, 0.5, 2);
        Assert.Throws<QrInputException>(() => transform.TryMapPoint(0, 0, null!));
    }

    static void AssertInvalidQuad(QrQuadrilateral invalid, QrQuadrilateral valid)
    {
        QrInputException sourceError = Assert.Throws<QrInputException>(
            () => PerspectiveTransform.Create(invalid, valid))!;
        QrInputException destinationError = Assert.Throws<QrInputException>(
            () => PerspectiveTransform.Create(valid, invalid))!;
        Assert.That(sourceError.InputName, Is.EqualTo("source"));
        Assert.That(destinationError.InputName, Is.EqualTo("destination"));
    }

    static void AssertCorners(PerspectiveTransform transform,
        QrQuadrilateral source, QrQuadrilateral destination)
    {
        QrPoint[] from = Corners(source);
        QrPoint[] to = Corners(destination);
        for (int index = 0; index < 4; index++)
            AssertPoint(transform.MapPoint(from[index]), to[index].X, to[index].Y);
    }

    static void AssertPoint(QrPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(1E-9));
        Assert.That(point.Y, Is.EqualTo(y).Within(1E-9));
    }

    static QrPoint[] Corners(QrQuadrilateral quad)
    {
        return [quad.TopLeft!, quad.TopRight!, quad.BottomRight!, quad.BottomLeft!];
    }

    static QrQuadrilateral Rectangle(double x, double y, double width, double height)
    {
        return Quad(x, y, x + width, y, x + width, y + height, x, y + height);
    }

    static QrQuadrilateral Quad(double x0, double y0, double x1, double y1,
        double x2, double y2, double x3, double y3)
    {
        return new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = x0, Y = y0 },
            TopRight = new QrPoint { X = x1, Y = y1 },
            BottomRight = new QrPoint { X = x2, Y = y2 },
            BottomLeft = new QrPoint { X = x3, Y = y3 }
        };
    }
}
