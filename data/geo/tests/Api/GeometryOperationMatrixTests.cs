using geo;

namespace geo.Tests;

public class GeometryOperationMatrixTests
{
    [Test]
    public void EveryNonEmptyGeometrySupportsInspectionEnvelopeNormalizationAndOwningEdits()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());
        Geometry[] geometries = CreateNonEmptyGeometryValues();

        foreach (Geometry source in geometries)
        {
            string name = source.GeometryType.ToString();
            Geometry baseline = editor.CloneGeometry(source);
            Coordinate[] sourceCoordinates = GeometryInspection.ExtractCoordinateCopies(source);
            Envelope envelope = inspector.GetEnvelope(source);
            Geometry clone = editor.CloneGeometry(source);
            Geometry normalized = editor.Normalize(source);
            Geometry normalizedAgain = editor.Normalize(normalized);
            Geometry reversed = editor.Reverse(source);
            Geometry oriented = editor.OrientRings(source, RingOrientation.CounterClockwise);
            Geometry transformed = editor.ApplyAffineTransform(source, new AffineTransform());
            Geometry replaced = editor.ReplaceSpatialReferenceId(source, 4326);
            Geometry converted = editor.ConvertCoordinateLayout(source, CoordinateLayout.XYZ, 7.0, 0.0);
            Geometry snapped = editor.SnapToGrid(source, new PrecisionModel());
            Geometry cleaned = editor.RemoveRepeatedPoints(source);
            Geometry flattened = editor.Flatten(source);
            Geometry combined = editor.Combine([source]);
            Geometry extractedType = editor.ExtractByType(source, source.GeometryType);
            Geometry extractedDimension = editor.ExtractByDimension(source, inspector.GetTopologicalDimension(source));

            Assert.Multiple(() =>
            {
                Assert.That(inspector.GetGeometryType(source), Is.EqualTo(source.GeometryType), name);
                Assert.That(inspector.IsEmpty(source), Is.False, name);
                Assert.That(inspector.CountCoordinates(source), Is.EqualTo(sourceCoordinates.Length), name);
                Assert.That(inspector.CountCoordinates(source), Is.GreaterThan(0), name);
                Assert.That(inspector.GetTopologicalDimension(source), Is.EqualTo(ExpectedDimension(source.GeometryType)), name);
                AssertEnvelopeContainsCoordinates(
                    envelope, sourceCoordinates, source.GeometryType, name);
                Assert.That(GeometryNormalization.CompareGeometryValues(normalized, normalizedAgain), Is.EqualTo(0), name);
                Assert.That(clone, Is.Not.SameAs(source), name);
                Assert.That(normalized, Is.Not.SameAs(source), name);
                Assert.That(reversed, Is.Not.SameAs(source), name);
                Assert.That(oriented, Is.Not.SameAs(source), name);
                Assert.That(transformed, Is.Not.SameAs(source), name);
                Assert.That(replaced, Is.Not.SameAs(source), name);
                Assert.That(replaced.SpatialReferenceId, Is.EqualTo(4326), name);
                Assert.That(converted, Is.Not.SameAs(source), name);
                Assert.That(converted.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ), name);
                Assert.That(snapped, Is.Not.SameAs(source), name);
                Assert.That(cleaned, Is.Not.SameAs(source), name);
                Assert.That(flattened.GeometryType, Is.EqualTo(GeometryType.GeometryCollection), name);
                Assert.That(combined, Is.Not.SameAs(source), name);
                Assert.That(extractedType, Is.Not.SameAs(source), name);
                Assert.That(extractedDimension, Is.Not.SameAs(source), name);
                Assert.That(GeometryNormalization.CompareGeometryValues(source, baseline), Is.EqualTo(0), name);
            });

            Coordinate[] cloneCoordinates = CoordinateSequences.ExtractCoordinates(
                clone,
                GeometryConstants.DefaultMaximumNestingDepth,
                GeometryConstants.DefaultMaximumComponents);
            cloneCoordinates[0].X = 1000.0;
            Assert.That(GeometryInspection.ExtractCoordinateCopies(source)[0].X, Is.EqualTo(sourceCoordinates[0].X), name);
        }
    }

    [Test]
    public void EveryCanonicalEmptyGeometryHasStableInspectionEnvelopeEditorAndBoundaryBehavior()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYM,
            SpatialReferenceId = 4326
        });
        GeometryInspector inspector = new GeometryInspector();
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());

        foreach (GeometryType type in AllGeometryTypes())
        {
            string name = type.ToString();
            Geometry source = factory.CreateEmptyGeometry(type);
            Geometry baseline = editor.CloneGeometry(source);
            Geometry normalized = editor.Normalize(source);
            Geometry normalizedAgain = editor.Normalize(normalized);
            Geometry reversed = editor.Reverse(source);
            Geometry oriented = editor.OrientRings(source, RingOrientation.Clockwise);
            Geometry transformed = editor.ApplyAffineTransform(source, new AffineTransform());
            Geometry replaced = editor.ReplaceSpatialReferenceId(source, 3857);
            Geometry converted = editor.ConvertCoordinateLayout(source, CoordinateLayout.XYZM, 5.0, 0.0);
            Geometry snapped = editor.SnapToGrid(source, new PrecisionModel());
            Geometry cleaned = editor.RemoveRepeatedPoints(source);
            Geometry flattened = editor.Flatten(source);
            Geometry combined = editor.Combine([source]);
            Geometry extractedType = editor.ExtractByType(source, type);
            Geometry extractedDimension = editor.ExtractByDimension(source, TopologicalDimension.Empty);
            Envelope envelope = inspector.GetEnvelope(source);

            Assert.Multiple(() =>
            {
                Assert.That(inspector.IsEmpty(source), Is.True, name);
                Assert.That(inspector.CountCoordinates(source), Is.EqualTo(0), name);
                Assert.That(inspector.GetTopologicalDimension(source), Is.EqualTo(TopologicalDimension.Empty), name);
                Assert.That(EnvelopeAlgorithms.ValidateAndCheckEmpty(envelope), Is.True, name);
                Assert.That(GeometryNormalization.CompareGeometryValues(normalized, normalizedAgain), Is.EqualTo(0), name);
                Assert.That(normalized.GeometryType, Is.EqualTo(type), name);
                Assert.That(reversed.GeometryType, Is.EqualTo(type), name);
                Assert.That(oriented.GeometryType, Is.EqualTo(type), name);
                Assert.That(transformed.GeometryType, Is.EqualTo(type), name);
                Assert.That(replaced.GeometryType, Is.EqualTo(type), name);
                Assert.That(replaced.SpatialReferenceId, Is.EqualTo(3857), name);
                Assert.That(converted.GeometryType, Is.EqualTo(type), name);
                Assert.That(converted.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM), name);
                Assert.That(snapped.GeometryType, Is.EqualTo(type), name);
                Assert.That(cleaned.GeometryType, Is.EqualTo(type), name);
                Assert.That(flattened.GeometryCollection!.Geometries, Is.Empty, name);
                Assert.That(combined.GeometryType, Is.EqualTo(type), name);
                Assert.That(extractedType.GeometryType, Is.EqualTo(type), name);
                Assert.That(inspector.IsEmpty(extractedDimension), Is.True, name);
                Assert.That(GeometryNormalization.CompareGeometryValues(source, baseline), Is.EqualTo(0), name);
            });

            if (SupportsEmptyBoundary(type))
            {
                Geometry boundary = inspector.GetBoundary(source, BoundaryNodeRule.Mod2);
                Assert.That(inspector.IsEmpty(boundary), Is.True, name);
            }
            else
            {
                Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(source, BoundaryNodeRule.Mod2), name);
            }
        }
    }

    [Test]
    public void BoundaryBehaviorIsDefinedForEveryNonEmptyGeometryTypeWithoutMutation()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());

        foreach (Geometry source in CreateNonEmptyGeometryValues())
        {
            string name = source.GeometryType.ToString();
            Geometry baseline = editor.CloneGeometry(source);
            if (SupportsNonEmptyBoundary(source.GeometryType))
            {
                Geometry boundary = inspector.GetBoundary(source, BoundaryNodeRule.Mod2);
                Assert.That(boundary, Is.Not.SameAs(source), name);
                Assert.That(boundary.CoordinateLayout, Is.EqualTo(source.CoordinateLayout), name);
                Assert.That(boundary.SpatialReferenceId, Is.EqualTo(source.SpatialReferenceId), name);
            }
            else
            {
                Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(source, BoundaryNodeRule.Mod2), name);
            }

            Assert.That(GeometryNormalization.CompareGeometryValues(source, baseline), Is.EqualTo(0), name);
        }
    }

    [Test]
    public void EveryRecursiveGeometryPayloadRejectsCyclesAcrossPublicOperations()
    {
        GeometryInspector inspector = new GeometryInspector();
        GeometryEditor editor = new GeometryEditor(new GeometryFactoryOptions());

        foreach (Geometry cyclic in CreateCyclicGeometryValues())
        {
            string name = cyclic.GeometryType.ToString();
            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidGeometryException>(() => inspector.CountCoordinates(cyclic), name);
                Assert.Throws<InvalidGeometryException>(() => inspector.GetEnvelope(cyclic), name);
                Assert.Throws<InvalidGeometryException>(() => inspector.GetBoundary(cyclic, BoundaryNodeRule.Mod2), name);
                Assert.Throws<InvalidGeometryException>(() => editor.CloneGeometry(cyclic), name);
                Assert.Throws<InvalidGeometryException>(() => editor.Normalize(cyclic), name);
                Assert.Throws<InvalidGeometryException>(() => editor.Reverse(cyclic), name);
                Assert.Throws<InvalidGeometryException>(() => editor.Flatten(cyclic), name);
            });
        }
    }

    private static Geometry[] CreateNonEmptyGeometryValues()
    {
        return
        [
            PointGeometry(0.0, 0.0),
            LineGeometry(0.0, 0.0, 4.0, 3.0),
            RingGeometry(),
            PolygonGeometry(),
            MultiPointGeometry(),
            MultiLineGeometry(),
            MultiPolygonGeometry(),
            CollectionGeometry(),
            CircularGeometry(0.0, 0.0, 2.0, 3.0, 4.0, 0.0),
            CompoundGeometry(),
            CurvePolygonGeometry(),
            MultiCurveGeometry(),
            MultiSurfaceGeometry(),
            PolyhedralSurfaceGeometry(),
            TinGeometry(),
            TriangleGeometry()
        ];
    }

    private static GeometryType[] AllGeometryTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.LinearRing,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection,
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface,
            GeometryType.PolyhedralSurface,
            GeometryType.Tin,
            GeometryType.Triangle
        ];
    }

    private static Geometry[] CreateCyclicGeometryValues()
    {
        Geometry collection = EmptyShell(GeometryType.GeometryCollection);
        collection.GeometryCollection = new GeometryCollection { Geometries = [collection] };
        Geometry compound = EmptyShell(GeometryType.CompoundCurve);
        compound.CompoundCurve = new CompoundCurve { Curves = [compound] };
        Geometry curvePolygon = EmptyShell(GeometryType.CurvePolygon);
        curvePolygon.CurvePolygon = new CurvePolygon { Shell = curvePolygon };
        Geometry multiCurve = EmptyShell(GeometryType.MultiCurve);
        multiCurve.MultiCurve = new MultiCurve { Curves = [multiCurve] };
        Geometry multiSurface = EmptyShell(GeometryType.MultiSurface);
        multiSurface.MultiSurface = new MultiSurface { Surfaces = [multiSurface] };
        return [collection, compound, curvePolygon, multiCurve, multiSurface];
    }

    private static Geometry EmptyShell(GeometryType type)
    {
        return new Geometry { GeometryType = type, Point = null };
    }

    private static Geometry PointGeometry(double x, double y)
    {
        return new Geometry { Point = new Point { Coordinate = CoordinateValue(x, y) } };
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

    private static Geometry RingGeometry()
    {
        return new Geometry { GeometryType = GeometryType.LinearRing, Point = null, LinearRing = RectangleRing() };
    }

    private static Geometry PolygonGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.Polygon,
            Point = null,
            Polygon = PolygonValue()
        };
    }

    private static Geometry MultiPointGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiPoint,
            Point = null,
            MultiPoint = new MultiPoint
            {
                Points =
                [
                    new Point { Coordinate = CoordinateValue(0.0, 0.0) },
                    new Point { Coordinate = CoordinateValue(4.0, 3.0) }
                ]
            }
        };
    }

    private static Geometry MultiLineGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiLineString,
            Point = null,
            MultiLineString = new MultiLineString
            {
                LineStrings = [new LineString { Coordinates = [CoordinateValue(0.0, 0.0), CoordinateValue(4.0, 3.0)] }]
            }
        };
    }

    private static Geometry MultiPolygonGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiPolygon,
            Point = null,
            MultiPolygon = new MultiPolygon { Polygons = [PolygonValue()] }
        };
    }

    private static Geometry CollectionGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.GeometryCollection,
            Point = null,
            GeometryCollection = new GeometryCollection
            {
                Geometries = [PointGeometry(0.0, 0.0), LineGeometry(0.0, 0.0, 4.0, 3.0)]
            }
        };
    }

    private static Geometry CircularGeometry(double firstX, double firstY, double middleX, double middleY, double lastX, double lastY)
    {
        return new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates = [CoordinateValue(firstX, firstY), CoordinateValue(middleX, middleY), CoordinateValue(lastX, lastY)]
            }
        };
    }

    private static Geometry CompoundGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.CompoundCurve,
            Point = null,
            CompoundCurve = new CompoundCurve
            {
                Curves =
                [
                    LineGeometry(0.0, 0.0, 1.0, 0.0),
                    CircularGeometry(1.0, 0.0, 2.0, 3.0, 4.0, 0.0)
                ]
            }
        };
    }

    private static Geometry CurvePolygonGeometry()
    {
        Geometry shell = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates =
                [
                    CoordinateValue(0.0, 0.0),
                    CoordinateValue(2.0, 3.0),
                    CoordinateValue(4.0, 0.0),
                    CoordinateValue(2.0, 1.0),
                    CoordinateValue(0.0, 0.0)
                ]
            }
        };
        return new Geometry
        {
            GeometryType = GeometryType.CurvePolygon,
            Point = null,
            CurvePolygon = new CurvePolygon { Shell = shell }
        };
    }

    private static Geometry MultiCurveGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiCurve,
            Point = null,
            MultiCurve = new MultiCurve { Curves = [CircularGeometry(0.0, 0.0, 2.0, 3.0, 4.0, 0.0)] }
        };
    }

    private static Geometry MultiSurfaceGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.MultiSurface,
            Point = null,
            MultiSurface = new MultiSurface { Surfaces = [PolygonGeometry()] }
        };
    }

    private static Geometry PolyhedralSurfaceGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.PolyhedralSurface,
            Point = null,
            PolyhedralSurface = new PolyhedralSurface { Patches = [PolygonValue()] }
        };
    }

    private static Geometry TinGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.Tin,
            Point = null,
            Tin = new Tin { Triangles = [TriangleValue()] }
        };
    }

    private static Geometry TriangleGeometry()
    {
        return new Geometry
        {
            GeometryType = GeometryType.Triangle,
            Point = null,
            Triangle = TriangleValue()
        };
    }

    private static Polygon PolygonValue()
    {
        return new Polygon { Shell = RectangleRing() };
    }

    private static Triangle TriangleValue()
    {
        Coordinate first = CoordinateValue(0.0, 0.0);
        return new Triangle
        {
            Shell = new LinearRing
            {
                Coordinates = [first, CoordinateValue(4.0, 0.0), CoordinateValue(2.0, 3.0), first]
            }
        };
    }

    private static LinearRing RectangleRing()
    {
        Coordinate first = CoordinateValue(0.0, 0.0);
        return new LinearRing
        {
            Coordinates =
            [
                first,
                CoordinateValue(4.0, 0.0),
                CoordinateValue(4.0, 3.0),
                CoordinateValue(0.0, 3.0),
                first
            ]
        };
    }

    private static Coordinate CoordinateValue(double x, double y)
    {
        return new Coordinate { X = x, Y = y };
    }

    private static TopologicalDimension ExpectedDimension(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return TopologicalDimension.Point;
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.MultiLineString:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.MultiCurve:
            case GeometryType.GeometryCollection:
                return TopologicalDimension.Curve;
            default:
                return TopologicalDimension.Surface;
        }
    }

    private static bool SupportsEmptyBoundary(GeometryType type)
    {
        return type == GeometryType.Point || type == GeometryType.LineString || type == GeometryType.LinearRing ||
            type == GeometryType.Polygon || type == GeometryType.MultiPoint || type == GeometryType.MultiLineString ||
            type == GeometryType.MultiPolygon || type == GeometryType.GeometryCollection ||
            type == GeometryType.Triangle || type == GeometryType.Tin ||
            type == GeometryType.PolyhedralSurface ||
            type == GeometryType.CircularString ||
            type == GeometryType.CompoundCurve ||
            type == GeometryType.CurvePolygon ||
            type == GeometryType.MultiCurve ||
            type == GeometryType.MultiSurface;
    }

    private static bool SupportsNonEmptyBoundary(GeometryType type)
    {
        return type == GeometryType.Point || type == GeometryType.LineString || type == GeometryType.LinearRing ||
            type == GeometryType.Polygon || type == GeometryType.MultiPoint || type == GeometryType.MultiLineString ||
            type == GeometryType.MultiPolygon || type == GeometryType.Triangle ||
            type == GeometryType.Tin || type == GeometryType.PolyhedralSurface ||
            type == GeometryType.CircularString ||
            type == GeometryType.CompoundCurve ||
            type == GeometryType.CurvePolygon ||
            type == GeometryType.MultiCurve ||
            type == GeometryType.MultiSurface;
    }

    private static void AssertEnvelopeContainsCoordinates(
        Envelope envelope, Coordinate[] coordinates,
        GeometryType geometryType, string name)
    {
        double minimumX = coordinates[0].X;
        double minimumY = coordinates[0].Y;
        double maximumX = coordinates[0].X;
        double maximumY = coordinates[0].Y;
        int index = 1;
        while (index < coordinates.Length)
        {
            if (coordinates[index].X < minimumX) minimumX = coordinates[index].X;
            if (coordinates[index].Y < minimumY) minimumY = coordinates[index].Y;
            if (coordinates[index].X > maximumX) maximumX = coordinates[index].X;
            if (coordinates[index].Y > maximumY) maximumY = coordinates[index].Y;
            index++;
        }

        bool curved = geometryType == GeometryType.CircularString ||
            geometryType == GeometryType.CompoundCurve ||
            geometryType == GeometryType.CurvePolygon ||
            geometryType == GeometryType.MultiCurve ||
            geometryType == GeometryType.MultiSurface;
        Assert.Multiple(() =>
        {
            Assert.That(envelope.MinX, curved ?
                Is.LessThanOrEqualTo(minimumX) : Is.EqualTo(minimumX), name);
            Assert.That(envelope.MinY, curved ?
                Is.LessThanOrEqualTo(minimumY) : Is.EqualTo(minimumY), name);
            Assert.That(envelope.MaxX, curved ?
                Is.GreaterThanOrEqualTo(maximumX) : Is.EqualTo(maximumX), name);
            Assert.That(envelope.MaxY, curved ?
                Is.GreaterThanOrEqualTo(maximumY) : Is.EqualTo(maximumY), name);
        });
    }
}
