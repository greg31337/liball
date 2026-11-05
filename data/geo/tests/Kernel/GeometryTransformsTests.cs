using geo;

namespace geo.Tests;

public class GeometryTransformsTests
{
    [Test]
    public void ReverseChangesCoordinateTraversalWithoutMutatingInputs()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry line = factory.CreateLineString([
            CoordinateValue(0.0, 0.0),
            CoordinateValue(1.0, 1.0),
            CoordinateValue(2.0, 2.0)
        ]);
        Geometry polygon = factory.CreatePolygon(RingValue(0.0, 0.0, 4.0, 4.0), [RingValue(1.0, 1.0, 2.0, 2.0)]);

        Geometry reversedLine = GeometryTransforms.ReverseGeometryValue(line);
        Geometry reversedPolygon = GeometryTransforms.ReverseGeometryValue(polygon);

        Assert.Multiple(() =>
        {
            Assert.That(reversedLine.LineString!.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(reversedLine.LineString.Coordinates[2].X, Is.EqualTo(0.0));
            Assert.That(line.LineString!.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(SignedArea(reversedPolygon.Polygon!.Shell.Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(reversedPolygon.Polygon.Holes[0].Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(polygon.Polygon!.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(reversedPolygon.Polygon.Shell, Is.Not.SameAs(polygon.Polygon.Shell));
        });
    }

    [Test]
    public void CompoundCurveReverseAlsoReversesComponentOrder()
    {
        Geometry first = LineGeometry(0.0, 0.0, 1.0, 0.0);
        Geometry second = LineGeometry(1.0, 0.0, 2.0, 0.0);
        Geometry compound = new Geometry
        {
            GeometryType = GeometryType.CompoundCurve,
            Point = null,
            CompoundCurve = new CompoundCurve { Curves = [first, second] }
        };

        Geometry reversed = GeometryTransforms.ReverseGeometryValue(compound);

        Assert.Multiple(() =>
        {
            Assert.That(reversed.CompoundCurve!.Curves[0].LineString!.Coordinates[0].X, Is.EqualTo(2.0));
            Assert.That(reversed.CompoundCurve.Curves[0].LineString!.Coordinates[1].X, Is.EqualTo(1.0));
            Assert.That(reversed.CompoundCurve.Curves[1].LineString!.Coordinates[0].X, Is.EqualTo(1.0));
            Assert.That(reversed.CompoundCurve.Curves[1].LineString!.Coordinates[1].X, Is.EqualTo(0.0));
            Assert.That(compound.CompoundCurve!.Curves[0], Is.SameAs(first));
        });
    }

    [Test]
    public void OrientRingsAppliesOppositeShellAndHoleDirections()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry source = factory.CreatePolygon(RingValue(0.0, 0.0, 5.0, 5.0), [RingValue(1.0, 1.0, 2.0, 2.0)]);

        Geometry clockwise = GeometryTransforms.OrientRingValues(source, RingOrientation.Clockwise);
        Geometry counterClockwise = GeometryTransforms.OrientRingValues(clockwise, RingOrientation.CounterClockwise);

        Assert.Multiple(() =>
        {
            Assert.That(SignedArea(clockwise.Polygon!.Shell.Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(clockwise.Polygon.Holes[0].Coordinates), Is.GreaterThan(0.0));
            Assert.That(SignedArea(counterClockwise.Polygon!.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.That(SignedArea(counterClockwise.Polygon.Holes[0].Coordinates), Is.LessThan(0.0));
            Assert.That(SignedArea(source.Polygon!.Shell.Coordinates), Is.GreaterThan(0.0));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.OrientRingValues(source, RingOrientation.Undefined));
        });
    }

    [Test]
    public void AffineTransformChangesOnlyXyAndRejectsNonFiniteOrCollapsedResults()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZM });
        Geometry point = factory.CreatePoint(CoordinateValue(2.0, 3.0, 7.0, 8.0));
        AffineTransform transform = new AffineTransform
        {
            M00 = 2.0,
            M01 = 1.0,
            M02 = 5.0,
            M10 = -1.0,
            M11 = 3.0,
            M12 = 4.0
        };

        Geometry transformed = GeometryTransforms.ApplyAffineTransformValue(point, transform);
        GeometryFactory xyFactory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry polygon = xyFactory.CreateEnvelopePolygon(new Envelope { MinX = 0.0, MinY = 0.0, MaxX = 2.0, MaxY = 2.0 });
        Geometry nestedPolygon = xyFactory.CreateGeometryCollection([polygon]);
        AffineTransform collapse = new AffineTransform { M00 = 0.0, M11 = 0.0 };

        Assert.Multiple(() =>
        {
            Assert.That(transformed.Point!.Coordinate!.X, Is.EqualTo(12.0));
            Assert.That(transformed.Point.Coordinate.Y, Is.EqualTo(11.0));
            Assert.That(transformed.Point.Coordinate.Z, Is.EqualTo(7.0));
            Assert.That(transformed.Point.Coordinate.M, Is.EqualTo(8.0));
            Assert.That(point.Point!.Coordinate!.X, Is.EqualTo(2.0));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ApplyAffineTransformValue(point, new AffineTransform { M00 = double.NaN }));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ApplyAffineTransformValue(point, null!));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ApplyAffineTransformValue(polygon, collapse));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ApplyAffineTransformValue(nestedPolygon, collapse));
        });
    }

    [Test]
    public void SpatialReferenceReplacementUpdatesNestedMetadataOnlyOnTheCopy()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry point = factory.CreatePoint(CoordinateValue(1.0, 2.0));
        Geometry nested = factory.CreateGeometryCollection([factory.CreateGeometryCollection([point])]);

        Geometry replaced = GeometryTransforms.ReplaceSpatialReferenceValue(nested, 3857);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(replaced.GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(replaced.GeometryCollection.Geometries[0].GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(3857));
            Assert.That(nested.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(point.SpatialReferenceId, Is.EqualTo(4326));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ReplaceSpatialReferenceValue(nested, -1));
        });
    }

    [Test]
    public void CoordinateLayoutConversionPreservesExistingAndFillsNewOrdinates()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry source = factory.CreateGeometryCollection([
            factory.CreatePoint(CoordinateValue(1.0, 2.0)),
            factory.CreateLineString([CoordinateValue(3.0, 4.0), CoordinateValue(5.0, 6.0)])
        ]);

        Geometry xyzm = GeometryTransforms.ConvertCoordinateLayoutValue(source, CoordinateLayout.XYZM, 7.0, 8.0);
        Geometry xym = GeometryTransforms.ConvertCoordinateLayoutValue(xyzm, CoordinateLayout.XYM, 100.0, 200.0);
        Coordinate[] xyzmCoordinates = GeometryInspection.ExtractCoordinateCopies(xyzm);
        Coordinate[] xymCoordinates = GeometryInspection.ExtractCoordinateCopies(xym);

        Assert.Multiple(() =>
        {
            Assert.That(xyzm.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(xyzm.GeometryCollection!.Geometries[0].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(xyzmCoordinates[0].Z, Is.EqualTo(7.0));
            Assert.That(xyzmCoordinates[0].M, Is.EqualTo(8.0));
            Assert.That(xym.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYM));
            Assert.That(double.IsNaN(xymCoordinates[0].Z), Is.True);
            Assert.That(xymCoordinates[0].M, Is.EqualTo(8.0));
            Assert.That(source.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ConvertCoordinateLayoutValue(source, CoordinateLayout.XYZ, double.NaN, 0.0));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.ConvertCoordinateLayoutValue(source, (CoordinateLayout)99, 0.0, 0.0));
        });
    }

    [Test]
    public void GridSnapUsesPrecisionModelAndRejectsRingCollapse()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZ });
        Geometry point = factory.CreatePoint(CoordinateValue(1.24, 2.26, 9.0, double.NaN));
        PrecisionModel precision = new PrecisionModel { Type = PrecisionModelType.Fixed, Scale = 2.0 };

        Geometry snapped = GeometryTransforms.SnapGeometryToGridValue(point, precision);
        GeometryFactory xyFactory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry smallPolygon = xyFactory.CreateEnvelopePolygon(new Envelope { MinX = 0.1, MinY = 0.1, MaxX = 0.4, MaxY = 0.4 });

        Assert.Multiple(() =>
        {
            Assert.That(snapped.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(snapped.Point.Coordinate.Y, Is.EqualTo(2.5));
            Assert.That(snapped.Point.Coordinate.Z, Is.EqualTo(9.0));
            Assert.That(point.Point!.Coordinate!.X, Is.EqualTo(1.24));
            Assert.Throws<InvalidGeometryException>(() => GeometryTransforms.SnapGeometryToGridValue(smallPolygon, new PrecisionModel { Type = PrecisionModelType.Fixed, Scale = 1.0 }));
            Assert.Throws<PrecisionException>(() => GeometryTransforms.SnapGeometryToGridValue(point, null!));
            Assert.Throws<PrecisionException>(() => GeometryTransforms.SnapGeometryToGridValue(point, new PrecisionModel { Scale = 0.0 }));
        });
    }

    [Test]
    public void RepeatedCoordinateRemovalCleansSequencesAndCreatesCanonicalCollapsedEmpties()
    {
        Geometry line = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString
            {
                Coordinates = [CoordinateValue(0.0, 0.0), CoordinateValue(0.0, 0.0), CoordinateValue(2.0, 2.0), CoordinateValue(2.0, 2.0)]
            }
        };
        Geometry collapsed = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [CoordinateValue(1.0, 1.0), CoordinateValue(1.0, 1.0)] }
        };
        LinearRing repeatedRing = new LinearRing
        {
            Coordinates =
            [
                CoordinateValue(0.0, 0.0),
                CoordinateValue(2.0, 0.0),
                CoordinateValue(2.0, 0.0),
                CoordinateValue(2.0, 2.0),
                CoordinateValue(0.0, 2.0),
                CoordinateValue(0.0, 0.0)
            ]
        };
        Geometry polygon = new Geometry { GeometryType = GeometryType.Polygon, Point = null, Polygon = new Polygon { Shell = repeatedRing } };

        Geometry cleanedLine = GeometryTransforms.RemoveRepeatedCoordinatesValue(line);
        Geometry cleanedCollapsed = GeometryTransforms.RemoveRepeatedCoordinatesValue(collapsed);
        Geometry cleanedPolygon = GeometryTransforms.RemoveRepeatedCoordinatesValue(polygon);

        Assert.Multiple(() =>
        {
            Assert.That(cleanedLine.LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(cleanedLine.LineString.Coordinates[0].X, Is.EqualTo(0.0));
            Assert.That(cleanedLine.LineString.Coordinates[1].X, Is.EqualTo(2.0));
            Assert.That(cleanedCollapsed.LineString!.Coordinates, Is.Empty);
            Assert.That(cleanedPolygon.Polygon!.Shell.Coordinates, Has.Length.EqualTo(5));
            Assert.That(line.LineString!.Coordinates, Has.Length.EqualTo(4));
            Assert.That(polygon.Polygon!.Shell.Coordinates, Has.Length.EqualTo(6));
        });
    }

    [Test]
    public void FlattenProducesIndependentOrderedLeavesAndSkipsEmptyMembers()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { SpatialReferenceId = 4326 });
        Geometry multiPoint = factory.CreateMultiPoint([
            new Point { Coordinate = CoordinateValue(1.0, 1.0) },
            new Point { Coordinate = CoordinateValue(2.0, 2.0) }
        ]);
        Geometry line = factory.CreateLineString([CoordinateValue(3.0, 3.0), CoordinateValue(4.0, 4.0)]);
        Geometry nested = factory.CreateGeometryCollection([
            multiPoint,
            factory.CreateGeometryCollection([factory.CreatePoint(null), line])
        ]);

        Geometry flattened = GeometryCollections.FlattenGeometryValue(nested);
        multiPoint.MultiPoint!.Points[0].Coordinate!.X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(flattened.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(flattened.GeometryCollection!.Geometries, Has.Length.EqualTo(3));
            Assert.That(flattened.GeometryCollection.Geometries[0].GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(flattened.GeometryCollection.Geometries[0].Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(flattened.GeometryCollection.Geometries[1].Point!.Coordinate!.X, Is.EqualTo(2.0));
            Assert.That(flattened.GeometryCollection.Geometries[2].GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(flattened.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(GeometryCollections.FlattenGeometryValue(factory.CreatePoint(null)).GeometryCollection!.Geometries, Is.Empty);
        });
    }

    [Test]
    public void CombineSelectsNarrowestCoreTypeAndValidatesMetadata()
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions { SpatialReferenceId = 4326 };
        GeometryFactory factory = new GeometryFactory(options);
        Geometry first = factory.CreatePoint(CoordinateValue(1.0, 1.0));
        Geometry second = factory.CreatePoint(CoordinateValue(2.0, 2.0));
        Geometry line = factory.CreateLineString([CoordinateValue(0.0, 0.0), CoordinateValue(3.0, 3.0)]);

        Geometry empty = GeometryCollections.CombineGeometryValues([], options);
        Geometry singleton = GeometryCollections.CombineGeometryValues([first], options);
        Geometry homogeneous = GeometryCollections.CombineGeometryValues([first, second], options);
        Geometry heterogeneous = GeometryCollections.CombineGeometryValues([line, first], options);
        Geometry circular = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            SpatialReferenceId = 4326,
            CircularString = new CircularString
            {
                Coordinates = [CoordinateValue(0.0, 0.0), CoordinateValue(1.0, 1.0), CoordinateValue(2.0, 0.0)]
            }
        };
        Geometry extendedSingleton = GeometryCollections.CombineGeometryValues([circular], options);
        first.Point!.Coordinate!.X = 100.0;
        Geometry wrongSrid = new Geometry { SpatialReferenceId = 3857 };

        Assert.Multiple(() =>
        {
            Assert.That(empty.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(empty.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(singleton.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(singleton.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(homogeneous.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(homogeneous.MultiPoint!.Points[0].Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(heterogeneous.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(extendedSingleton.GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(extendedSingleton, Is.Not.SameAs(circular));
            Assert.Throws<InvalidGeometryException>(() => GeometryCollections.CombineGeometryValues([second, wrongSrid], options));
            Assert.Throws<InvalidGeometryException>(() => GeometryCollections.CombineGeometryValues(null!, options));
        });
    }

    [Test]
    public void ExtractionByTypeAndDimensionUsesFlattenedNarrowestResults()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry multiPoint = factory.CreateMultiPoint([
            new Point { Coordinate = CoordinateValue(1.0, 1.0) },
            new Point { Coordinate = CoordinateValue(2.0, 2.0) }
        ]);
        Geometry line = factory.CreateLineString([CoordinateValue(3.0, 3.0), CoordinateValue(4.0, 4.0)]);
        Geometry polygon = factory.CreateEnvelopePolygon(new Envelope { MinX = 5.0, MinY = 5.0, MaxX = 7.0, MaxY = 7.0 });
        Geometry collection = factory.CreateGeometryCollection([polygon, multiPoint, line]);

        Geometry points = GeometryCollections.ExtractGeometryTypeValue(collection, GeometryType.Point);
        Geometry lines = GeometryCollections.ExtractTopologicalDimensionValue(collection, TopologicalDimension.Curve);
        Geometry surfaces = GeometryCollections.ExtractTopologicalDimensionValue(collection, TopologicalDimension.Surface);
        Geometry missing = GeometryCollections.ExtractGeometryTypeValue(collection, GeometryType.Triangle);

        Assert.Multiple(() =>
        {
            Assert.That(points.GeometryType, Is.EqualTo(GeometryType.MultiPoint));
            Assert.That(points.MultiPoint!.Points, Has.Length.EqualTo(2));
            Assert.That(lines.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(surfaces.GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(missing.GeometryType, Is.EqualTo(GeometryType.Triangle));
            Assert.That(missing.Triangle!.Shell.Coordinates, Is.Empty);
            Assert.Throws<InvalidGeometryException>(() => GeometryCollections.ExtractGeometryTypeValue(collection, (GeometryType)999));
            Assert.Throws<InvalidGeometryException>(() => GeometryCollections.ExtractTopologicalDimensionValue(collection, (TopologicalDimension)99));
        });
    }

    private static Geometry LineGeometry(double startX, double startY, double endX, double endY)
    {
        return new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [CoordinateValue(startX, startY), CoordinateValue(endX, endY)] }
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static Coordinate CoordinateValue(double x, double y, double z, double m)
    {
        return new Coordinate { X = x, Y = y, Z = z, M = m };
    }

    private static LinearRing RingValue(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        Coordinate first = CoordinateValue(minimumX, minimumY);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(maximumX, minimumY),
                CoordinateValue(maximumX, maximumY),
                CoordinateValue(minimumX, maximumY),
                first
            ]
        };
    }

    private static double SignedArea(Coordinate[] coordinates)
    {
        double sum = 0.0;
        int index = 0;
        while (index < coordinates.Length - 1)
        {
            sum += coordinates[index].X * coordinates[index + 1].Y - coordinates[index + 1].X * coordinates[index].Y;
            index++;
        }
        return sum / 2.0;
    }
}
