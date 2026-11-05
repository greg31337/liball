using System.Text;
using geo;

namespace geo.Tests;

public class PublishedStandardExampleTests
{
    [Test]
    public void Rfc7946GeometryExamplesHaveExactCanonicalRepresentations()
    {
        IGeoJsonCodec codec = GeoJsonCodecValue();

        foreach (StandardTextFixture fixture in
            StandardExampleFixtures.Rfc7946GeometryExamples())
        {
            GeoJsonDocument document = codec.ReadGeoJson(fixture.Input);

            Assert.Multiple(() =>
            {
                Assert.That(document.ObjectType,
                    Is.EqualTo(GeoJsonObjectType.Geometry), fixture.Name);
                Assert.That(document.Geometry!.GeometryType,
                    Is.EqualTo(fixture.GeometryType), fixture.Name);
                Assert.That(codec.WriteGeoJson(document),
                    Is.EqualTo(fixture.Expected), fixture.Name);
            });
        }
    }

    [Test]
    public void Rfc7946FeatureCollectionExampleHasExactContent()
    {
        StandardTextFixture fixture =
            StandardExampleFixtures.Rfc7946FeatureCollectionExample();
        IGeoJsonCodec codec = GeoJsonCodecValue();

        GeoJsonDocument document = codec.ReadGeoJson(fixture.Input);

        Assert.Multiple(() =>
        {
            Assert.That(document.ObjectType,
                Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(document.FeatureCollection!.Features,
                Has.Length.EqualTo(3));
            Assert.That(document.FeatureCollection.Features[0]
                .Geometry!.GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(document.FeatureCollection.Features[1]
                .Geometry!.GeometryType,
                Is.EqualTo(GeometryType.LineString));
            Assert.That(document.FeatureCollection.Features[2]
                .Properties.GetProperty("prop1").GetProperty("this")
                .GetString(), Is.EqualTo("that"));
            Assert.That(codec.WriteGeoJson(document),
                Is.EqualTo(fixture.Expected));
        });
    }

    [Test]
    public void Rfc7946AntimeridianExamplesAreExactCutTargets()
    {
        IGeoJsonCodec codec = GeoJsonCodecValue();
        IGeoJsonOperations operations = new GeoJsonOperations();
        StandardTextFixture[] fixtures =
        [
            StandardExampleFixtures.Rfc7946AntimeridianLineExample(),
            StandardExampleFixtures.Rfc7946AntimeridianPolygonExample()
        ];

        foreach (StandardTextFixture fixture in fixtures)
        {
            GeoJsonDocument source = codec.ReadGeoJson(fixture.Input);
            GeoJsonDocument cut = operations.CutAntimeridian(source);
            GeoJsonDocument expected = codec.ReadGeoJson(fixture.Expected);

            Assert.Multiple(() =>
            {
                Assert.That(cut.Geometry!.GeometryType,
                    Is.EqualTo(fixture.GeometryType), fixture.Name);
                Assert.That(codec.WriteGeoJson(expected),
                    Is.EqualTo(fixture.Expected), fixture.Name);
                Assert.That(new GeometryPredicates().AreTopologicallyEqual(
                    cut.Geometry.Value!, expected.Geometry!.Value!),
                    Is.True, fixture.Name);
            });
        }
    }

    [Test]
    public void Rfc7946FijiExampleHasExactWrappedBoundingBox()
    {
        const string fijiPoints =
            "{\"type\":\"MultiPoint\",\"coordinates\":" +
            "[[177,-20],[-178,-16]]}";
        GeoJsonDocument document =
            GeoJsonCodecValue().ReadGeoJson(fijiPoints);

        double[] result = new GeoJsonOperations()
            .ComputeGeoJsonBoundingBox(document);

        Assert.That(result,
            Is.EqualTo(new[] { 177.0, -20.0, -178.0, -16.0 }));
    }

    [Test]
    public void Rfc8142ExampleHasExactFramingAndObjectOrder()
    {
        StandardTextFixture fixture =
            StandardExampleFixtures.Rfc8142HeterogeneousSequence();
        IGeoJsonSequenceCodec codec = new GeoJsonSequenceCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        byte[] bytes = Encoding.UTF8.GetBytes(fixture.Input);

        GeoJsonReadResult[] results = codec.ReadGeoJsonSequence(
            new MemoryStream(bytes));
        MemoryStream output = new MemoryStream();
        codec.WriteGeoJsonSequence(
            results.Select(result => result.Document!).ToArray(), output);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results.All(result => result.Success), Is.True);
            Assert.That(results[0].Document!.ObjectType,
                Is.EqualTo(GeoJsonObjectType.Geometry));
            Assert.That(results[1].Document!.ObjectType,
                Is.EqualTo(GeoJsonObjectType.Feature));
            Assert.That(results[2].Document!.ObjectType,
                Is.EqualTo(GeoJsonObjectType.FeatureCollection));
            Assert.That(output.ToArray(),
                Is.EqualTo(Encoding.UTF8.GetBytes(fixture.Expected)));
        });
    }

    [Test]
    public void OgcSfaInspectionAndSerializationExamplesAreExact()
    {
        IGeometryInspector inspector = new GeometryInspector();
        IGeometryMeasures measures = new GeometryMeasures();
        IGeometryValidator validator = new GeometryValidator();
        Geometry blueLake = ReadOgc(StandardExampleFixtures.BlueLake);
        Geometry route = ReadOgc(StandardExampleFixtures.Route75);
        Geometry gooseIsland = ReadOgc(StandardExampleFixtures.GooseIsland);
        Polygon blueLakePolygon = blueLake.Polygon!;
        Geometry road102 = ReadOgc(
            StandardExampleFixtures.Route5Segment102);
        Geometry road106 = ReadOgc(StandardExampleFixtures.RoadSegment106);
        Geometry ponds = ReadOgc(StandardExampleFixtures.Ponds);
        Geometry bridge = ReadOgc(StandardExampleFixtures.CamBridge);

        Geometry boundary = inspector.GetBoundary(
            gooseIsland, BoundaryNodeRule.Mod2);
        Envelope envelope = inspector.GetEnvelope(gooseIsland);
        Coordinate start = inspector.GetStartPoint(road102)!;
        Coordinate end = inspector.GetEndPoint(road102)!;
        Coordinate centroid = measures.Centroid(ponds)!;
        Coordinate pointOnSurface = measures.PointOnSurface(ponds)!;

        Assert.Multiple(() =>
        {
            Assert.That(inspector.GetTopologicalDimension(blueLake),
                Is.EqualTo(TopologicalDimension.Surface), "T6");
            Assert.That(inspector.GetGeometryType(route),
                Is.EqualTo(GeometryType.MultiLineString), "T7");
            Assert.That(WriteOgc(gooseIsland), Is.EqualTo(
                StandardExampleFixtures.GooseIsland), "T8");
            Assert.That(inspector.GetSpatialReferenceId(gooseIsland),
                Is.EqualTo(101), "T10");
            Assert.That(inspector.IsEmpty(road102), Is.False, "T11");
            Assert.That(validator.ValidateGeometry(blueLake).IsValid,
                Is.True, "T12");
            AssertCanonicalGeometry(
                "LINESTRING (67 13, 67 18, 59 18, 59 13, 67 13)",
                BoundaryLine(boundary), "T13");
            Assert.That(envelope.MinX, Is.EqualTo(59.0), "T14 min X");
            Assert.That(envelope.MinY, Is.EqualTo(13.0), "T14 min Y");
            Assert.That(envelope.MaxX, Is.EqualTo(67.0), "T14 max X");
            Assert.That(envelope.MaxY, Is.EqualTo(18.0), "T14 max Y");
            Assert.That(bridge.Point!.Coordinate!.X,
                Is.EqualTo(44.0), "T15");
            Assert.That(bridge.Point.Coordinate.Y,
                Is.EqualTo(31.0), "T16");
            Assert.That(start.X, Is.EqualTo(0.0), "T17 X");
            Assert.That(start.Y, Is.EqualTo(18.0), "T17 Y");
            Assert.That(end.X, Is.EqualTo(44.0), "T18 X");
            Assert.That(end.Y, Is.EqualTo(31.0), "T18 Y");
            Assert.That(inspector.IsClosed(boundary), Is.True, "T19");
            Assert.That(inspector.IsRing(
                BoundaryLine(boundary)), Is.True, "T20");
            Assert.That(measures.Length(road106),
                Is.EqualTo(26.0), "T21");
            Assert.That(inspector.CountCoordinates(road102),
                Is.EqualTo(5), "T22");
            Assert.That(inspector.GetPointAt(road102, 0).X,
                Is.EqualTo(0.0), "T23 X");
            Assert.That(inspector.GetPointAt(road102, 0).Y,
                Is.EqualTo(18.0), "T23 Y");
            Assert.That(measures.Area(gooseIsland),
                Is.EqualTo(40.0), "T26");
            Assert.That(blueLake.Polygon!.Holes,
                Has.Length.EqualTo(1), "T28");
            Assert.That(inspector.CountComponents(route),
                Is.EqualTo(2), "T30");
            AssertCanonicalGeometry(
                "LINESTRING (16 0, 16 23, 16 48)",
                inspector.GetComponentAt(route, 1), "T31");
            Assert.That(inspector.IsClosed(route), Is.False, "T32");
            Assert.That(measures.Length(route),
                Is.EqualTo(96.0), "T33");
            Assert.That(centroid.X, Is.EqualTo(25.0), "T34 X");
            Assert.That(centroid.Y, Is.EqualTo(42.0), "T34 Y");
            Assert.That(new GeometryPredicates().Contains(
                ponds, ReadOgc($"POINT ({pointOnSurface.X} " +
                    $"{pointOnSurface.Y})")), Is.True, "T35");
            Assert.That(measures.Area(ponds), Is.EqualTo(8.0), "T36");
        });

        AssertOgcWkbFixture(gooseIsland);
        AssertCanonicalGeometry(
            "LINESTRING (52 18, 66 23, 73 9, 48 6, 52 18)",
            RingGeometry(blueLakePolygon.Shell), "T27");
        AssertCanonicalGeometry(
            "LINESTRING (59 18, 67 18, 67 13, 59 13, 59 18)",
            RingGeometry(blueLakePolygon.Holes[0]), "T29");
    }

    [Test]
    public void OgcSfaPredicateMetricAndConstructiveExamplesAreExact()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryRelations relations = new GeometryRelations();
        IGeometryMeasures measures = new GeometryMeasures();
        IGeometryOverlay overlay = new GeometryOverlay();
        IGeometryConstructive constructive = new GeometryConstructive();
        Geometry blueLake = ReadOgc(StandardExampleFixtures.BlueLake);
        Geometry route = ReadOgc(StandardExampleFixtures.Route75);
        Geometry forest = ReadOgc(StandardExampleFixtures.GreenForest);
        Geometry bridge = ReadOgc(StandardExampleFixtures.CamBridge);
        Geometry stream = ReadOgc(StandardExampleFixtures.CamStream);
        Geometry road = ReadOgc(
            StandardExampleFixtures.Route5Segment102);
        Geometry house = ReadOgc(
            StandardExampleFixtures.House215Footprint);
        Geometry ashton = ReadOgc(StandardExampleFixtures.Ashton);
        Geometry island = ReadOgc(StandardExampleFixtures.GooseIsland);

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreTopologicallyEqual(island,
                ReadOgc(StandardExampleFixtures.GooseIsland)),
                Is.True, "T37");
            Assert.That(predicates.Disjoint(route, ashton),
                Is.True, "T38");
            Assert.That(predicates.Touches(stream, blueLake),
                Is.True, "T39");
            Assert.That(predicates.Within(house, ashton),
                Is.True, "T40");
            Assert.That(predicates.Overlaps(forest, ashton),
                Is.True, "T41");
            Assert.That(predicates.Crosses(road, route),
                Is.True, "T42");
            Assert.That(predicates.Intersects(road, route),
                Is.True, "T43");
            Assert.That(predicates.Contains(forest, ashton),
                Is.False, "T44");
            Assert.That(relations.MatchRelationPattern(
                relations.Relate(forest, ashton), "TTTTTTTTT"),
                Is.True, "T45");
            Assert.That(measures.Distance(bridge, ashton),
                Is.EqualTo(12.0), "T46");
        });

        AssertCanonicalGeometry("POINT (52 18)",
            overlay.Intersection(stream, blueLake), "T47");
        AssertCanonicalGeometry(
            "POLYGON ((56 34, 62 48, 84 48, 84 42, 56 34))",
            overlay.Difference(ashton, forest), "T48");
        AssertCanonicalGeometry(
            "POLYGON ((52 18, 66 23, 73 9, 48 6, 52 18))",
            overlay.Union(blueLake, island), "T49");
        AssertCanonicalGeometry(
            "POLYGON ((52 18, 66 23, 73 9, 48 6, 52 18))",
            overlay.SymmetricDifference(blueLake, island), "T50");

        Geometry buffer = constructive.Buffer(
            bridge, 15.0, new BufferParameters());
        Geometry[] buildings =
        [
            ReadOgc(StandardExampleFixtures.House123),
            ReadOgc(StandardExampleFixtures.House215)
        ];
        Assert.That(buildings.Count(building =>
            predicates.Contains(buffer, building)), Is.EqualTo(1), "T51");
        AssertCanonicalGeometry(
            "POLYGON ((52 18, 66 23, 73 9, 48 6, 52 18))",
            constructive.ConvexHull(blueLake), "T52");
    }

    private static IGeoJsonCodec GeoJsonCodecValue()
    {
        return new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
    }

    private static Geometry ReadOgc(string text)
    {
        return new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions()).ReadWkt("SRID=101;" + text);
    }

    private static string WriteOgc(Geometry geometry)
    {
        return new WktCodec(
            new WktReadOptions(), new WktWriteOptions()).WriteWkt(geometry);
    }

    private static void AssertOgcWkbFixture(Geometry gooseIsland)
    {
        IWkbCodec codec = new WkbCodec(
            new WkbReadOptions(), new WkbWriteOptions
            {
                ByteOrder = ByteOrder.LittleEndian
            });
        string actual = codec.WriteWkbHex(gooseIsland);
        Geometry roundTrip = codec.ReadWkbHex(
            StandardExampleFixtures.GooseIslandWkbLittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(
                StandardExampleFixtures.GooseIslandWkbLittleEndian),
                "T9 bytes");
            Assert.That(WriteOgc(roundTrip), Is.EqualTo(
                StandardExampleFixtures.GooseIsland), "T9 round trip");
        });
    }

    private static Geometry BoundaryLine(Geometry boundary)
    {
        if (boundary.GeometryType == GeometryType.LineString)
        {
            return boundary;
        }
        return new GeometryInspector().GetComponentAt(boundary, 0);
    }

    private static Geometry RingGeometry(LinearRing ring)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = GeometryType.LineString;
        geometry.SpatialReferenceId = 101;
        geometry.LineString = new LineString
        {
            Coordinates = ring.Coordinates
        };
        return geometry;
    }

    private static void AssertCanonicalGeometry(
        string expectedText, Geometry actual, string description)
    {
        Geometry expected = ReadOgc(expectedText);
        IGeometryEditor editor = new GeometryEditor(
            new GeometryFactoryOptions());

        Assert.That(WriteOgc(editor.Normalize(actual)),
            Is.EqualTo(WriteOgc(editor.Normalize(expected))), description);
    }
}
