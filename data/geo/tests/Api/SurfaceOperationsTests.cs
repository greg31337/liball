using geo;

namespace geo.Tests;

public class SurfaceOperationsTests
{
    [Test]
    public void InspectionAndPatchExtractionCoverSurfaceComponents()
    {
        IGeometryInspector inspector = new GeometryInspector();
        Geometry tin = Read(
            "TIN Z (((0 0 0, 2 0 0, 0 2 0, 0 0 0)), " +
            "((2 0 0, 2 2 4, 0 2 0, 2 0 0)))");
        Geometry surface = Read(
            "POLYHEDRALSURFACE Z (" +
            "((0 0 0, 2 0 0, 2 2 0, 0 2 0, 0 0 0)), " +
            "((3 0 1, 5 0 1, 5 2 1, 3 2 1, 3 0 1)))");

        Geometry trianglePatch = inspector.GetComponentAt(tin, 1);
        Geometry polygonPatch = inspector.GetComponentAt(surface, 0);
        tin.Tin!.Triangles[1].Shell.Coordinates[0].X = 100.0;
        surface.PolyhedralSurface!.Patches[0]
            .Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(inspector.GetTopologicalDimension(tin),
                Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(inspector.GetTopologicalDimension(surface),
                Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(inspector.CountComponents(tin), Is.EqualTo(2));
            Assert.That(inspector.CountCoordinates(tin), Is.EqualTo(8));
            Assert.That(inspector.CountComponents(surface), Is.EqualTo(2));
            Assert.That(inspector.CountCoordinates(surface),
                Is.EqualTo(10));
            Assert.That(trianglePatch.GeometryType,
                Is.EqualTo(GeometryType.Triangle));
            Assert.That(trianglePatch.Triangle!.Shell.Coordinates[0].X,
                Is.EqualTo(2.0));
            Assert.That(polygonPatch.GeometryType,
                Is.EqualTo(GeometryType.Polygon));
            Assert.That(polygonPatch.Polygon!.Shell.Coordinates[0].X,
                Is.EqualTo(0.0));
            Assert.That(trianglePatch.CoordinateLayout,
                Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(inspector.GetEnvelope(surface).MaxX,
                Is.EqualTo(100.0));
            Assert.Throws<InvalidGeometryException>(() =>
                inspector.GetComponentAt(tin, 2));
        });
    }

    [Test]
    public void SurfaceClosureUsesExactThreeDimensionalEdgeIncidence()
    {
        IGeometryInspector inspector = new GeometryInspector();
        Geometry openTin = Read(
            "TIN Z (((0 0 0, 2 0 0, 0 2 0, 0 0 0)), " +
            "((2 0 0, 2 2 0, 0 2 0, 2 0 0)))");
        Geometry closedTin = Read(TetrahedronText("TIN Z"));
        Geometry openSurface = Read(
            OpenTetrahedronText("POLYHEDRALSURFACE Z"));
        Geometry closedSurface = Read(
            TetrahedronText("POLYHEDRALSURFACE Z"));

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(openTin), Is.False);
            Assert.That(inspector.IsClosed(closedTin), Is.True);
            Assert.That(inspector.IsClosed(openSurface), Is.False);
            Assert.That(inspector.IsClosed(closedSurface), Is.True);
            Assert.That(inspector.IsClosed(Read("TIN EMPTY")), Is.False);
            Assert.That(inspector.IsClosed(Read(
                "POLYHEDRALSURFACE EMPTY")), Is.False);
        });
    }

    [Test]
    public void TriangleAndTinBoundariesExposeOnlyUnsharedEdges()
    {
        IGeometryInspector inspector = new GeometryInspector();
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry triangle = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        Geometry tin = Read(
            "TIN (((0 0, 2 0, 0 2, 0 0)), " +
            "((2 0, 2 2, 0 2, 2 0)))");

        Geometry triangleBoundary = inspector.GetBoundary(
            triangle, BoundaryNodeRule.Mod2);
        Geometry tinBoundary = inspector.GetBoundary(
            tin, BoundaryNodeRule.Endpoint);
        tin.Tin!.Triangles[0].Shell.Coordinates[0].X = 100.0;

        Assert.Multiple(() =>
        {
            Assert.That(triangleBoundary.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(triangleBoundary.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(triangleBoundary.MultiLineString.LineStrings[0]
                .Coordinates, Has.Length.EqualTo(4));
            Assert.That(tinBoundary.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(tinBoundary.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(tinBoundary.MultiLineString.LineStrings[0]
                .Coordinates, Has.Length.EqualTo(5));
            Assert.That(inspector.ExtractUniqueCoordinates(tinBoundary),
                Has.Length.EqualTo(4));
            Assert.That(measures.Length(tinBoundary),
                Is.EqualTo(8.0));
            Assert.That(inspector.GetEnvelope(tinBoundary).MaxX,
                Is.EqualTo(2.0));
        });
    }

    [Test]
    public void PolyhedralBoundaryIsMissingFaceRingOrEmptyWhenClosed()
    {
        IGeometryInspector inspector = new GeometryInspector();
        Geometry open = Read(
            OpenTetrahedronText("POLYHEDRALSURFACE Z"));
        Geometry closed = Read(
            TetrahedronText("POLYHEDRALSURFACE Z"));

        Geometry openBoundary = inspector.GetBoundary(
            open, BoundaryNodeRule.Mod2);
        Geometry closedBoundary = inspector.GetBoundary(
            closed, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            Assert.That(openBoundary.MultiLineString!.LineStrings,
                Has.Length.EqualTo(1));
            Assert.That(openBoundary.MultiLineString.LineStrings[0]
                .Coordinates, Has.Length.EqualTo(4));
            Assert.That(inspector.ExtractUniqueCoordinates(openBoundary),
                Has.Length.EqualTo(3));
            Assert.That(closedBoundary.GeometryType,
                Is.EqualTo(GeometryType.MultiLineString));
            Assert.That(closedBoundary.MultiLineString!.LineStrings,
                Is.Empty);
            Assert.That(inspector.IsEmpty(closedBoundary), Is.True);
        });
    }

    [Test]
    public void ProjectedAndThreeDimensionalAreasAggregateSurfacePatches()
    {
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry triangle = Read(
            "TRIANGLE Z ((0 0 0, 3 0 0, 0 4 12, 0 0 0))");
        Geometry tin = Read(
            "TIN Z (((0 0 0, 3 0 0, 0 4 12, 0 0 0)), " +
            "((3 0 0, 3 4 12, 0 4 12, 3 0 0)))");
        Geometry surface = Read(
            TetrahedronText("POLYHEDRALSURFACE Z"));
        Geometry collection = Read(
            "GEOMETRYCOLLECTION Z (" +
            "TRIANGLE Z ((0 0 0, 3 0 0, 0 4 12, 0 0 0)), " +
            "POINT Z (9 9 9))");

        Assert.Multiple(() =>
        {
            Assert.That(measures.Area(triangle), Is.EqualTo(6.0));
            Assert.That(measures.SurfaceArea3D(triangle),
                Is.EqualTo(6.0 * Math.Sqrt(10.0)).Within(1.0e-13));
            Assert.That(measures.Area(tin), Is.EqualTo(12.0));
            Assert.That(measures.SurfaceArea3D(tin),
                Is.EqualTo(12.0 * Math.Sqrt(10.0)).Within(1.0e-13));
            Assert.That(measures.Area(surface), Is.EqualTo(12.0));
            Assert.That(measures.SurfaceArea3D(surface),
                Is.EqualTo(15.0 + Math.Sqrt(68.0) + Math.Sqrt(41.0))
                    .Within(1.0e-13));
            Assert.That(measures.Area(collection), Is.EqualTo(6.0));
            Assert.That(measures.SurfaceArea3D(collection),
                Is.EqualTo(6.0 * Math.Sqrt(10.0)).Within(1.0e-13));
            Assert.That(measures.Area(Read("TRIANGLE EMPTY")), Is.Zero);
            Assert.That(measures.SurfaceArea3D(
                Read("POLYHEDRALSURFACE EMPTY")), Is.Zero);
        });
    }

    [Test]
    public void MalformedAndNonManifoldSurfaceOperationsFail()
    {
        IGeometryInspector inspector = new GeometryInspector();
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry malformed = Read(
            "TRIANGLE ((0 0, 2 0, 0 2, 0 0))");
        malformed.Triangle!.Shell.Coordinates =
            [
                new Coordinate { X = 0.0, Y = 0.0 },
                new Coordinate { X = 2.0, Y = 0.0 },
                new Coordinate { X = 0.0, Y = 2.0 }
            ];
        Geometry nonManifold = Read(
            "TIN (((0 0, 2 0, 0 2, 0 0)), " +
            "((2 0, 0 0, 1 -2, 2 0)), " +
            "((0 0, 2 0, 1 3, 0 0)))");

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                measures.Area(malformed));
            Assert.Throws<InvalidGeometryException>(() =>
                measures.SurfaceArea3D(malformed));
            Assert.Throws<InvalidGeometryException>(() =>
                inspector.GetBoundary(
                    malformed, BoundaryNodeRule.Mod2));
            Assert.Throws<InvalidGeometryException>(() =>
                inspector.GetBoundary(
                    nonManifold, BoundaryNodeRule.Mod2));
        });
    }

    [Test]
    public void SurfaceFamiliesRoundTripAcrossWktAndWkbWithMetadata()
    {
        string[] values =
        [
            "SRID=4979;TRIANGLE Z ((0 0 1, 4 0 2, 1 3 3, 0 0 1))",
            "SRID=4979;TIN Z (((0 0 1, 2 0 1, 0 2 1, 0 0 1)), " +
                "((2 0 1, 2 2 1, 0 2 1, 2 0 1)))",
            "SRID=4979;POLYHEDRALSURFACE Z (" +
                "((0 0 1, 2 0 1, 2 2 1, 0 2 1, 0 0 1)), " +
                "((3 0 2, 5 0 2, 5 2 2, 3 2 2, 3 0 2)))"
        ];
        WktCodec textCodec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions { IncludeSpatialReferenceId = true });
        WkbCodec binaryCodec = new WkbCodec(
            new WkbReadOptions { Flavor = WkbFlavor.Ewkb },
            new WkbWriteOptions
            {
                ByteOrder = ByteOrder.LittleEndian,
                Flavor = WkbFlavor.Ewkb,
                IncludeSpatialReferenceId = true
            });
        GeometryPredicates predicates = new GeometryPredicates();

        foreach (string value in values)
        {
            Geometry source = textCodec.ReadWkt(value);
            Geometry textRoundTrip = textCodec.ReadWkt(
                textCodec.WriteWkt(source));
            Geometry binaryRoundTrip = binaryCodec.ReadWkb(
                binaryCodec.WriteWkb(source));

            Assert.Multiple(() =>
            {
                Assert.That(predicates.AreExactlyEqual(
                    source, textRoundTrip), Is.True, value);
                Assert.That(predicates.AreExactlyEqual(
                    source, binaryRoundTrip), Is.True, value);
                Assert.That(binaryRoundTrip.CoordinateLayout,
                    Is.EqualTo(CoordinateLayout.XYZ), value);
                Assert.That(binaryRoundTrip.SpatialReferenceId,
                    Is.EqualTo(4979), value);
            });
        }
    }

    [Test]
    public void PublicValidationEnforcesSurfaceModelAndTopologyInvariants()
    {
        GeometryValidator validator = new GeometryValidator();
        Geometry validTriangle = Read(
            "TRIANGLE ((0 0, 4 0, 1 3, 0 0))");
        Geometry collinearTriangle = Read(
            "TRIANGLE ((0 0, 2 0, 4 0, 0 0))");
        Geometry overlappingTin = Read(
            "TIN (((0 0, 4 0, 1 3, 0 0)), " +
            "((0 0, 4 0, 3 3, 0 0)))");
        Geometry nonManifoldSurface = Read(
            "POLYHEDRALSURFACE (" +
            "((0 0, 4 0, 1 2, 0 0)), " +
            "((4 0, 0 0, 1 -2, 4 0)), " +
            "((0 0, 4 0, 3 2, 0 0)))");

        ValidationResult valid = validator.ValidateGeometry(validTriangle);
        ValidationResult collinear =
            validator.ValidateGeometry(collinearTriangle);
        ValidationResult overlapping =
            validator.ValidateGeometry(overlappingTin);
        ValidationResult nonManifold =
            validator.ValidateGeometry(nonManifoldSurface);
        Geometry point = new GeometryFactory(
            new GeometryFactoryOptions()).CreatePoint(
                new Coordinate { X = 10.0, Y = 10.0 });
        GeometryMeasures measures = new GeometryMeasures();

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(collinear.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(overlapping.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(nonManifold.Error,
                Is.EqualTo(ValidationError.InvalidSurface));
            Assert.That(collinear.ComponentPath,
                Does.Contain("Triangle/Shell"));
            Assert.That(overlapping.ComponentPath,
                Does.Contain("Tin/Triangles"));
            Assert.That(nonManifold.ComponentPath,
                Does.Contain("PolyhedralSurface/Patches"));
            Assert.Throws<InvalidGeometryException>(() =>
                measures.Distance(collinearTriangle, point));
            Assert.Throws<InvalidGeometryException>(() =>
                measures.Distance(overlappingTin, point));
            Assert.Throws<InvalidGeometryException>(() =>
                measures.Distance(nonManifoldSurface, point));
        });
    }

    [Test]
    public void SurfaceFamiliesParticipateInLinearPlanarKernels()
    {
        Geometry[] surfaces =
        [
            Read("TRIANGLE ((0 0, 4 0, 0 4, 0 0))"),
            Read("TIN (((0 0, 2 0, 0 2, 0 0)), " +
                "((5 0, 7 0, 5 2, 5 0)))"),
            Read("POLYHEDRALSURFACE (" +
                "((0 0, 4 0, 4 4, 0 4, 0 0)))")
        ];
        GeometryFactory factory = new GeometryFactory(
            new GeometryFactoryOptions());
        Geometry point = factory.CreatePoint(
            new Coordinate { X = 0.5, Y = 0.5 });
        GeometryMeasures measures = new GeometryMeasures();
        GeometryPredicates predicates = new GeometryPredicates();
        GeometryRelations relations = new GeometryRelations();
        GeometryOverlay overlay = new GeometryOverlay();
        GeometryConstructive constructive = new GeometryConstructive();
        GeometrySimplifier simplifier = new GeometrySimplifier();
        SimplifyParameters simplifyParameters = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.DouglasPeucker,
            Tolerance = 0.01
        };
        Geometry triangleBuffer = constructive.Buffer(
            surfaces[0], 0.1, new BufferParameters());

        Assert.That(predicates.Contains(triangleBuffer, point), Is.True);

        foreach (Geometry surface in surfaces)
        {
            Coordinate? representative = measures.PointOnSurface(surface);
            Geometry intersection = overlay.Intersection(surface, point);
            Geometry simplified = simplifier.Simplify(
                surface, simplifyParameters);

            Assert.Multiple(() =>
            {
                Assert.That(measures.Distance(surface, point), Is.Zero,
                    surface.GeometryType.ToString());
                Assert.That(measures.IndexedDistance(surface, point), Is.Zero,
                    surface.GeometryType.ToString());
                Assert.That(predicates.Contains(surface, point), Is.True,
                    surface.GeometryType.ToString());
                Assert.That(relations.LocatePoint(
                    point.Point!.Coordinate!, surface),
                    Is.EqualTo(TopologicalLocation.Interior),
                    surface.GeometryType.ToString());
                Assert.That(representative, Is.Not.Null,
                    surface.GeometryType.ToString());
                Assert.That(relations.LocatePoint(
                    representative!, surface), Is.Not.EqualTo(
                        TopologicalLocation.Exterior),
                    surface.GeometryType.ToString());
                Assert.That(intersection.GeometryType,
                    Is.EqualTo(GeometryType.Point),
                    surface.GeometryType.ToString());
                Assert.That(intersection.Point!.Coordinate, Is.Not.Null,
                    surface.GeometryType.ToString());
                Assert.That(simplified.GeometryType, Is.AnyOf(
                    GeometryType.Polygon, GeometryType.MultiPolygon),
                    surface.GeometryType.ToString());
            });
        }
    }

    [Test]
    public void ClosedSurfaceRoundTripRetainsClosureBoundaryAndArea()
    {
        Geometry source = Read(
            TetrahedronText("POLYHEDRALSURFACE Z"));
        WkbCodec codec = new WkbCodec(
            new WkbReadOptions(), new WkbWriteOptions());
        Geometry roundTrip = codec.ReadWkb(codec.WriteWkb(source));
        GeometryInspector inspector = new GeometryInspector();
        GeometryMeasures measures = new GeometryMeasures();
        Geometry boundary = inspector.GetBoundary(
            roundTrip, BoundaryNodeRule.Mod2);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.IsClosed(roundTrip), Is.True);
            Assert.That(inspector.IsEmpty(boundary), Is.True);
            Assert.That(measures.Area(roundTrip),
                Is.EqualTo(measures.Area(source)));
            Assert.That(measures.SurfaceArea3D(roundTrip),
                Is.EqualTo(measures.SurfaceArea3D(source)));
        });
    }

    private static string TetrahedronText(string type)
    {
        return type + " (" +
            "((0 0 0, 4 0 0, 1 3 0, 0 0 0)), " +
            "((0 0 0, 1 1 4, 4 0 0, 0 0 0)), " +
            "((4 0 0, 1 1 4, 1 3 0, 4 0 0)), " +
            "((1 3 0, 1 1 4, 0 0 0, 1 3 0)))";
    }

    private static string OpenTetrahedronText(string type)
    {
        return type + " (" +
            "((0 0 0, 1 1 4, 4 0 0, 0 0 0)), " +
            "((4 0 0, 1 1 4, 1 3 0, 4 0 0)), " +
            "((1 3 0, 1 1 4, 0 0 0, 1 3 0)))";
    }

    private static Geometry Read(string text)
    {
        IWktCodec codec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        return codec.ReadWkt(text);
    }
}
