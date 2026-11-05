using geo;

namespace geo.Tests;

public class GeometryEditorTests
{
    [Test]
    public void EditorSnapshotsConfigurationForEmptyCombine()
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZ,
            SpatialReferenceId = 4326
        };
        IGeometryEditor editor = new GeometryEditor(options);
        options.CoordinateLayout = CoordinateLayout.XY;
        options.SpatialReferenceId = 3857;

        Geometry result = editor.Combine([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(result.GeometryCollection!.Geometries, Is.Empty);
            Assert.Throws<PrecisionException>(() => new GeometryEditor(null!));
            Assert.Throws<InvalidGeometryException>(() => new GeometryEditor(new GeometryFactoryOptions { SpatialReferenceId = -1 }));
        });
    }

    [Test]
    public void CopyCanonicalAndTraversalOperationsReturnIndependentValues()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());
        Geometry source = factory.CreateLineString([
            CoordinateValue(2.0, 0.0),
            CoordinateValue(1.0, 0.0),
            CoordinateValue(0.0, 0.0)
        ]);

        Geometry clone = editor.CloneGeometry(source);
        Geometry normalized = editor.Normalize(source);
        Geometry reversed = editor.Reverse(source);
        clone.LineString!.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.LineString, Is.Not.SameAs(source.LineString));
            Assert.That(source.LineString!.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(normalized.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(reversed.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(reversed, Is.Not.SameAs(source));
        });
    }

    [Test]
    public void CoordinateEditingOperationsRouteThroughOwningAlgorithms()
    {
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());
        Geometry line = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates =
                [
                    CoordinateValue(0.24, 0.26),
                    CoordinateValue(0.24, 0.26),
                    CoordinateValue(2.24, 2.26)
                ]
            }
        };
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry polygon = factory.CreateEnvelopePolygon(new Envelope
        {
            MinX = 0.0,
            MinY = 0.0,
            MaxX = 2.0,
            MaxY = 2.0
        });
        AffineTransform transform = new AffineTransform { M02 = 10.0, M12 = -5.0 };
        PrecisionModel precision = new PrecisionModel { Type = PrecisionModelType.Fixed, Scale = 2.0 };

        Geometry oriented = editor.OrientRings(polygon, RingOrientation.Clockwise);
        Geometry transformed = editor.ApplyAffineTransform(line, transform);
        Geometry replaced = editor.ReplaceSpatialReferenceId(line, 4326);
        Geometry converted = editor.ConvertCoordinateLayout(line, CoordinateLayout.XYZM, 7.0, 8.0);
        Geometry snapped = editor.SnapToGrid(line, precision);
        Geometry cleaned = editor.RemoveRepeatedPoints(line);

        Assert.Multiple(() =>
        {
            Assert.That(SignedArea(oriented.Polygon!.Shell.Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(polygon.Polygon!.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(transformed.LineString!.Coordinates[0].X, Is.EqualTo(10.24));
            Assert.That(transformed.LineString.Coordinates[0].Y, Is.EqualTo(-4.74));
            Assert.That(replaced.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(converted.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(converted.LineString!.Coordinates[0].Z, Is.EqualTo(7.0));
            Assert.That(converted.LineString.Coordinates[0].M, Is.EqualTo(8.0));
            Assert.That(snapped.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(snapped.LineString.Coordinates[0].Y, Is.EqualTo(0.5));
            Assert.That(cleaned.LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(line.LineString!.Coordinates, Has.Length.EqualTo(3));
            Assert.That(line.LineString.Coordinates[0].X, Is.EqualTo(0.24));
            Assert.That(line.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(line.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
        });
    }

    [Test]
    public void CollectionEditingOperationsReturnNarrowIndependentResults()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());
        Geometry first = factory.CreatePoint(CoordinateValue(1.0, 1.0));
        Geometry second = factory.CreatePoint(CoordinateValue(2.0, 2.0));
        Geometry line = factory.CreateLineString([CoordinateValue(3.0, 3.0), CoordinateValue(4.0, 4.0)]);
        Geometry nested = factory.CreateGeometryCollection([
            first,
            factory.CreateGeometryCollection([second, line])
        ]);

        Geometry flattened = editor.Flatten(nested);
        Geometry combined = editor.Combine([first, second]);
        Geometry points = editor.ExtractByType(nested, GeometryType.Point);
        Geometry curves = editor.ExtractByDimension(nested, TopologicalDimension.Curve);
        nested.GeometryCollection!.Geometries[0].Point!.Coordinate!.X = 100.0;
        first.Point!.Coordinate!.X = 200.0;

        Assert.Multiple(() =>
        {
            Assert.That(flattened.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(flattened.GeometryCollection!.Geometries, Has.Length.EqualTo(3));
            Assert.That(flattened.GeometryCollection.Geometries[0].Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(combined.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(combined.MultiPoint!.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(points.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(points.MultiPoint!.Points, Has.Length.EqualTo(2));
            Assert.That(points.MultiPoint.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(curves.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(curves.LineString!.Coordinates[0].X, Is.EqualTo(3.0));
        });
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static double SignedArea(Coordinate[] coordinates)
    {
        double twiceArea = 0.0;
        int index = 0;
        while (index < coordinates.Length - 1)
        {
            twiceArea += coordinates[index].X * coordinates[index + 1].Y - coordinates[index + 1].X * coordinates[index].Y;
            index++;
        }

        return twiceArea / 2.0;
    }
}
