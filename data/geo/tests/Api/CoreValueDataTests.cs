using geo;

namespace geo.Tests;

public class CoreValueDataTests
{
    [Test]
    public void CoordinatesRepresentEverySupportedLayoutWithNaNForMissingOrdinates()
    {
        Coordinate xy = new Coordinate { X = 1.0, Y = 2.0 };
        Coordinate xyz = new Coordinate { X = 3.0, Y = 4.0, Z = 5.0 };
        Coordinate xym = new Coordinate { X = 6.0, Y = 7.0, M = 8.0 };
        Coordinate xyzm = new Coordinate { X = 9.0, Y = 10.0, Z = 11.0, M = 12.0 };

        Assert.Multiple(() =>
        {
            Assert.That(xy.X, Is.EqualTo(1.0));
            Assert.That(xy.Y, Is.EqualTo(2.0));
            Assert.That(double.IsNaN(xy.Z), Is.True);
            Assert.That(double.IsNaN(xy.M), Is.True);

            Assert.That(xyz.Z, Is.EqualTo(5.0));
            Assert.That(double.IsNaN(xyz.M), Is.True);

            Assert.That(double.IsNaN(xym.Z), Is.True);
            Assert.That(xym.M, Is.EqualTo(8.0));

            Assert.That(xyzm.Z, Is.EqualTo(11.0));
            Assert.That(xyzm.M, Is.EqualTo(12.0));
        });
    }

    [Test]
    public void EnvelopeDefaultsToEmptyAndRetainsAssignedBounds()
    {
        Envelope empty = new Envelope();
        Envelope bounds = new Envelope
        {
            MinX = -12.5,
            MinY = -3.25,
            MaxX = 45.0,
            MaxY = 91.75
        };

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(empty.MinX), Is.True);
            Assert.That(double.IsNaN(empty.MinY), Is.True);
            Assert.That(double.IsNaN(empty.MaxX), Is.True);
            Assert.That(double.IsNaN(empty.MaxY), Is.True);
            Assert.That(bounds.MinX, Is.EqualTo(-12.5));
            Assert.That(bounds.MinY, Is.EqualTo(-3.25));
            Assert.That(bounds.MaxX, Is.EqualTo(45.0));
            Assert.That(bounds.MaxY, Is.EqualTo(91.75));
        });
    }

    [Test]
    public void PrecisionModelDefaultsToFloatingAndRetainsFixedGridConfiguration()
    {
        PrecisionModel floating = new PrecisionModel();
        PrecisionModel fixedGrid = new PrecisionModel
        {
            Type = PrecisionModelType.Fixed,
            Scale = 1000.0,
            OffsetX = 250.0,
            OffsetY = -125.0
        };

        Assert.Multiple(() =>
        {
            Assert.That(floating.Type, Is.EqualTo(PrecisionModelType.Floating));
            Assert.That(floating.Scale, Is.EqualTo(1.0));
            Assert.That(floating.OffsetX, Is.Zero);
            Assert.That(floating.OffsetY, Is.Zero);
            Assert.That(fixedGrid.Type, Is.EqualTo(PrecisionModelType.Fixed));
            Assert.That(fixedGrid.Scale, Is.EqualTo(1000.0));
            Assert.That(fixedGrid.OffsetX, Is.EqualTo(250.0));
            Assert.That(fixedGrid.OffsetY, Is.EqualTo(-125.0));
        });
    }

    [Test]
    public void AffineTransformDefaultsToIdentityAndUsesRowMajorCoefficients()
    {
        AffineTransform identity = new AffineTransform();
        AffineTransform transform = new AffineTransform
        {
            M00 = 2.0,
            M01 = 3.0,
            M02 = 5.0,
            M10 = 7.0,
            M11 = 11.0,
            M12 = 13.0
        };
        double x = transform.M00 * 17.0 + transform.M01 * 19.0 + transform.M02;
        double y = transform.M10 * 17.0 + transform.M11 * 19.0 + transform.M12;

        Assert.Multiple(() =>
        {
            Assert.That(identity.M00, Is.EqualTo(1.0));
            Assert.That(identity.M01, Is.Zero);
            Assert.That(identity.M02, Is.Zero);
            Assert.That(identity.M10, Is.Zero);
            Assert.That(identity.M11, Is.EqualTo(1.0));
            Assert.That(identity.M12, Is.Zero);
            Assert.That(x, Is.EqualTo(96.0));
            Assert.That(y, Is.EqualTo(341.0));
        });
    }

    [Test]
    public void FactoryOptionsDefaultToUnknownSpatialReference()
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
            Assert.That(options.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(options.PrecisionModelType, Is.EqualTo(PrecisionModelType.Floating));
            Assert.That(options.RingOrientation, Is.EqualTo(RingOrientation.Undefined));
            Assert.That(options.BoundaryNodeRule, Is.EqualTo(BoundaryNodeRule.Mod2));
        });
    }
}
