using geo;

namespace geo.Tests;

public class FinalApiBehaviorAuditTests
{
    [Test]
    public void EveryCoreTypeLayoutAndEmptyStateCrossesThePublicApiStack()
    {
        IGeometryValidator validator = new GeometryValidator();
        IGeometryInspector inspector = new GeometryInspector();
        IGeometryEditor editor = new GeometryEditor(
            new GeometryFactoryOptions());
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryRelations relations = new GeometryRelations();
        IGeometryMeasures measures = new GeometryMeasures();
        IGeometryOverlay overlay = new GeometryOverlay();
        IWktCodec textCodec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        IWkbCodec bigEndian = BinaryCodec(ByteOrder.BigEndian);
        IWkbCodec littleEndian = BinaryCodec(ByteOrder.LittleEndian);
        IGeoJsonCodec geoJson = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());

        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in CoreTypes())
            {
                int emptyIndex = 0;
                while (emptyIndex < 2)
                {
                    bool empty = emptyIndex == 1;
                    string text = empty ? EmptyText(type, layout) :
                        NonEmptyText(type, layout);
                    Geometry source = textCodec.ReadWkt(text);
                    string description = type + " " + layout +
                        (empty ? " EMPTY" : " non-empty");
                    ValidationResult validation =
                        validator.ValidateGeometry(source);
                    Geometry clone = editor.CloneGeometry(source);
                    Geometry normalized = editor.Normalize(source);
                    Geometry renormalized = editor.Normalize(normalized);
                    Geometry textRoundTrip = textCodec.ReadWkt(
                        textCodec.WriteWkt(source));
                    Geometry bigEndianRoundTrip = bigEndian.ReadWkb(
                        bigEndian.WriteWkb(source));
                    Geometry littleEndianRoundTrip = littleEndian.ReadWkb(
                        littleEndian.WriteWkb(source));
                    IPreparedGeometry prepared =
                        new PreparedGeometry(source);
                    RelationMatrix relation = relations.Relate(source, source);
                    RelationMatrix transpose =
                        relations.TransposeRelation(relation);
                    Geometry intersection = overlay.Intersection(source, source);
                    Geometry union = overlay.Union(source, source);
                    Geometry difference = overlay.Difference(source, source);
                    Geometry symmetricDifference =
                        overlay.SymmetricDifference(source, source);

                    Assert.Multiple(() =>
                    {
                        Assert.That(validation.IsValid, Is.True,
                            description + ": " + validation.Message);
                        Assert.That(inspector.GetGeometryType(source),
                            Is.EqualTo(type), description);
                        Assert.That(inspector.GetCoordinateLayout(source),
                            Is.EqualTo(layout), description);
                        Assert.That(inspector.IsEmpty(source),
                            Is.EqualTo(empty), description);
                        Assert.That(predicates.AreExactlyEqual(source, clone),
                            Is.True, description + " clone");
                        Assert.That(predicates.AreExactlyEqual(
                            normalized, renormalized), Is.True,
                            description + " normalize");
                        Assert.That(predicates.AreExactlyEqual(
                            source, textRoundTrip), Is.True,
                            description + " WKT");
                        Assert.That(predicates.AreExactlyEqual(
                            source, bigEndianRoundTrip), Is.True,
                            description + " big-endian WKB");
                        Assert.That(predicates.AreExactlyEqual(
                            source, littleEndianRoundTrip), Is.True,
                            description + " little-endian WKB");
                        Assert.That(predicates.AreExactlyEqual(
                            source, prepared.GetGeometrySnapshot()), Is.True,
                            description + " prepared snapshot");
                        Assert.That(prepared.Intersects(source),
                            Is.EqualTo(!empty),
                            description + " prepared intersection");
                        Assert.That(prepared.DistancePrepared(source),
                            empty ? Is.EqualTo(double.PositiveInfinity) :
                                Is.EqualTo(0.0),
                            description + " prepared distance");
                        Assert.That(transpose.Cells,
                            Is.EqualTo(relation.Cells),
                            description + " self relation");
                        Assert.That(measures.Area(source),
                            Is.GreaterThanOrEqualTo(0.0), description);
                        Assert.That(measures.Length(source),
                            Is.GreaterThanOrEqualTo(0.0), description);
                        Assert.That(inspector.IsEmpty(difference), Is.True,
                            description + " self difference");
                        Assert.That(inspector.IsEmpty(symmetricDifference),
                            Is.True, description + " self symmetric difference");
                    });

                    if (empty)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(inspector.IsEmpty(intersection), Is.True,
                                description + " self intersection");
                            Assert.That(inspector.IsEmpty(union), Is.True,
                                description + " self union");
                        });
                    }
                    else
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(predicates.AreTopologicallyEqual(
                                source, intersection), Is.True,
                                description + " self intersection");
                            Assert.That(predicates.AreTopologicallyEqual(
                                source, union), Is.True,
                                description + " self union");
                        });
                    }

                    if (layout == CoordinateLayout.XY ||
                        layout == CoordinateLayout.XYZ)
                    {
                        Geometry crs84 = editor.ReplaceSpatialReferenceId(
                            source, GeometryConstants.Crs84SpatialReferenceId);
                        GeoJsonDocument document = GeometryDocument(crs84);
                        GeoJsonDocument roundTrip = geoJson.ReadGeoJson(
                            geoJson.WriteGeoJson(document));
                        if (empty)
                        {
                            Assert.Multiple(() =>
                            {
                                Assert.That(roundTrip.Geometry!.GeometryType,
                                    Is.EqualTo(type),
                                    description + " GeoJSON type");
                                Assert.That(inspector.IsEmpty(
                                    roundTrip.Geometry.Value!), Is.True,
                                    description + " GeoJSON empty");
                                Assert.That(roundTrip.Geometry.Value!
                                    .CoordinateLayout,
                                    Is.EqualTo(CoordinateLayout.XY),
                                    description + " canonical empty layout");
                            });
                        }
                        else
                        {
                            Assert.That(predicates.AreNormalizedEqual(
                                crs84, roundTrip.Geometry!.Value!), Is.True,
                                description + " GeoJSON");
                        }
                    }
                    emptyIndex++;
                }
            }
        }
    }

    [Test]
    public void EveryCoreTypeAndLayoutRejectsMalformedCoordinates()
    {
        IGeometryValidator validator = new GeometryValidator();
        IWktCodec textCodec = new WktCodec(
            new WktReadOptions(), new WktWriteOptions());
        IWkbCodec binaryCodec = BinaryCodec(ByteOrder.LittleEndian);
        IGeoJsonCodec geoJson = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());

        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in CoreTypes())
            {
                Geometry malformed = textCodec.ReadWkt(
                    NonEmptyText(type, layout));
                FirstCoordinate(malformed).X = double.NaN;
                ValidationResult result =
                    validator.ValidateGeometry(malformed);
                string description = type + " " + layout;

                Assert.Multiple(() =>
                {
                    Assert.That(result.IsValid, Is.False, description);
                    Assert.That(result.Error,
                        Is.EqualTo(ValidationError.NonFiniteCoordinate),
                        description);
                    Assert.That(result.Message, Is.Not.Empty, description);
                    Assert.Throws<InvalidGeometryException>(() =>
                        textCodec.WriteWkt(malformed), description + " WKT");
                    Assert.Throws<InvalidGeometryException>(() =>
                        binaryCodec.WriteWkb(malformed), description + " WKB");
                });

                if (layout == CoordinateLayout.XY ||
                    layout == CoordinateLayout.XYZ)
                {
                    Assert.Throws<InvalidGeometryException>(() =>
                        geoJson.WriteGeoJson(GeometryDocument(malformed)),
                        description + " GeoJSON");
                }
            }
        }
    }

    [Test]
    public void StandardsModesHandleEveryExtendedAndOutOfProfileFamily()
    {
        IGeoJsonCodec geoJson = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        IWktCodec strictText = new WktCodec(
            new WktReadOptions { AllowExtendedTypes = false },
            new WktWriteOptions { AllowExtendedTypes = false });
        IWkbCodec strictBinary = new WkbCodec(
            new WkbReadOptions(),
            new WkbWriteOptions());
        GeometryFactory factory = new GeometryFactory(
            new GeometryFactoryOptions());

        foreach (GeometryType type in ExtendedTypes())
        {
            Geometry empty = factory.CreateEmptyGeometry(type);
            Geometry binaryRoundTrip = strictBinary.ReadWkb(
                strictBinary.WriteWkb(empty));
            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidGeometryException>(() =>
                    strictText.WriteWkt(empty), type + " strict WKT");
                Assert.That(binaryRoundTrip.GeometryType,
                    Is.EqualTo(type), type + " extended WKB");
                Assert.Throws<GeoJsonException>(() =>
                    geoJson.WriteGeoJson(GeometryDocument(empty)),
                    type + " strict GeoJSON");
            });
        }

        foreach (CoordinateLayout layout in new[]
            { CoordinateLayout.XYM, CoordinateLayout.XYZM })
        {
            Geometry point = new WktCodec(
                new WktReadOptions(), new WktWriteOptions()).ReadWkt(
                    NonEmptyText(GeometryType.Point, layout));
            Assert.Throws<GeoJsonException>(() =>
                geoJson.WriteGeoJson(GeometryDocument(point)),
                layout.ToString());
        }

        Geometry wrongCrs = new GeometryEditor(
            new GeometryFactoryOptions()).ReplaceSpatialReferenceId(
                factory.CreatePoint(new Coordinate { X = 1.0, Y = 2.0 }),
                3857);
        Assert.Throws<GeoJsonException>(() =>
            geoJson.WriteGeoJson(GeometryDocument(wrongCrs)),
            "strict GeoJSON CRS84 requirement");
    }

    private static IWkbCodec BinaryCodec(ByteOrder byteOrder)
    {
        return new WkbCodec(
            new WkbReadOptions(),
            new WkbWriteOptions { ByteOrder = byteOrder });
    }

    private static GeoJsonDocument GeometryDocument(Geometry geometry)
    {
        return new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.Geometry,
            Geometry = new GeoJsonGeometry
            {
                GeometryType = geometry.GeometryType,
                Value = geometry
            }
        };
    }

    private static Coordinate FirstCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.Point)
            return geometry.Point!.Coordinate!;
        if (geometry.GeometryType == GeometryType.LineString)
            return geometry.LineString!.Coordinates[0];
        if (geometry.GeometryType == GeometryType.Polygon)
            return geometry.Polygon!.Shell.Coordinates[0];
        if (geometry.GeometryType == GeometryType.MultiPoint)
            return geometry.MultiPoint!.Points[0].Coordinate!;
        if (geometry.GeometryType == GeometryType.MultiLineString)
            return geometry.MultiLineString!.LineStrings[0].Coordinates[0];
        if (geometry.GeometryType == GeometryType.MultiPolygon)
            return geometry.MultiPolygon!.Polygons[0].Shell.Coordinates[0];
        return FirstCoordinate(
            geometry.GeometryCollection!.Geometries[0]);
    }

    private static CoordinateLayout[] AllLayouts()
    {
        return
        [
            CoordinateLayout.XY,
            CoordinateLayout.XYZ,
            CoordinateLayout.XYM,
            CoordinateLayout.XYZM
        ];
    }

    private static GeometryType[] CoreTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection
        ];
    }

    private static GeometryType[] ExtendedTypes()
    {
        return
        [
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

    private static string EmptyText(
        GeometryType type, CoordinateLayout layout)
    {
        return TypeName(type) + LayoutSuffix(layout) + " EMPTY";
    }

    private static string NonEmptyText(
        GeometryType type, CoordinateLayout layout)
    {
        string suffix = LayoutSuffix(layout);
        string first = CoordinateText(layout, 0.0, 0.0);
        string second = CoordinateText(layout, 4.0, 0.0);
        string third = CoordinateText(layout, 4.0, 3.0);
        string fourth = CoordinateText(layout, 0.0, 3.0);
        string farFirst = CoordinateText(layout, 10.0, 10.0);
        string farSecond = CoordinateText(layout, 12.0, 10.0);
        string farThird = CoordinateText(layout, 12.0, 12.0);
        string farFourth = CoordinateText(layout, 10.0, 12.0);
        string ring = first + ", " + second + ", " + third +
            ", " + fourth + ", " + first;
        string farRing = farFirst + ", " + farSecond + ", " +
            farThird + ", " + farFourth + ", " + farFirst;
        if (type == GeometryType.Point)
            return "POINT" + suffix + " (" + first + ")";
        if (type == GeometryType.LineString)
            return "LINESTRING" + suffix + " (" + first + ", " +
                third + ")";
        if (type == GeometryType.Polygon)
            return "POLYGON" + suffix + " ((" + ring + "))";
        if (type == GeometryType.MultiPoint)
            return "MULTIPOINT" + suffix + " ((" + first + "), (" +
                third + "))";
        if (type == GeometryType.MultiLineString)
            return "MULTILINESTRING" + suffix + " ((" + first + ", " +
                second + "), (" + farFirst + ", " + farSecond + "))";
        if (type == GeometryType.MultiPolygon)
            return "MULTIPOLYGON" + suffix + " (((" + ring + ")), ((" +
                farRing + ")))";
        return "GEOMETRYCOLLECTION" + suffix + " (POINT" + suffix +
            " (" + first + "), LINESTRING" + suffix + " (" + farFirst +
            ", " + farSecond + "), POLYGON" + suffix + " ((" + ring + ")))";
    }

    private static string CoordinateText(
        CoordinateLayout layout, double x, double y)
    {
        string planar = x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            " " + y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (layout == CoordinateLayout.XYZ) return planar + " 3";
        if (layout == CoordinateLayout.XYM) return planar + " 4";
        if (layout == CoordinateLayout.XYZM) return planar + " 3 4";
        return planar;
    }

    private static string LayoutSuffix(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XYZ) return " Z";
        if (layout == CoordinateLayout.XYM) return " M";
        if (layout == CoordinateLayout.XYZM) return " ZM";
        return "";
    }

    private static string TypeName(GeometryType type)
    {
        if (type == GeometryType.Point) return "POINT";
        if (type == GeometryType.LineString) return "LINESTRING";
        if (type == GeometryType.Polygon) return "POLYGON";
        if (type == GeometryType.MultiPoint) return "MULTIPOINT";
        if (type == GeometryType.MultiLineString) return "MULTILINESTRING";
        if (type == GeometryType.MultiPolygon) return "MULTIPOLYGON";
        return "GEOMETRYCOLLECTION";
    }
}
