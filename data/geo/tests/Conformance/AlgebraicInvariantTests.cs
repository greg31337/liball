using geo;

namespace geo.Tests;

public class AlgebraicInvariantTests
{
    [Test]
    public void RelationConverseEqualsMatrixTranspose()
    {
        IGeometryRelations relations = new GeometryRelations();
        Geometry[][] pairs = RelationPairs();

        int index = 0;
        while (index < pairs.Length)
        {
            Geometry first = pairs[index][0];
            Geometry second = pairs[index][1];
            RelationMatrix forward = relations.Relate(first, second);
            RelationMatrix converse = relations.Relate(second, first);
            RelationMatrix transpose = relations.TransposeRelation(forward);
            RelationMatrix restored = relations.TransposeRelation(transpose);
            string description = PairDescription(first, second);

            Assert.Multiple(() =>
            {
                Assert.That(transpose.Cells, Is.EqualTo(converse.Cells),
                    description);
                Assert.That(transpose.LeftDimension,
                    Is.EqualTo(converse.LeftDimension), description);
                Assert.That(transpose.RightDimension,
                    Is.EqualTo(converse.RightDimension), description);
                Assert.That(restored.Cells, Is.EqualTo(forward.Cells),
                    description + " double transpose");
                Assert.That(restored.LeftDimension,
                    Is.EqualTo(forward.LeftDimension), description);
                Assert.That(restored.RightDimension,
                    Is.EqualTo(forward.RightDimension), description);
            });
            index++;
        }
    }

    [Test]
    public void PredicateDualsAndSymmetriesAgree()
    {
        IGeometryPredicates predicates = new GeometryPredicates();
        Geometry[][] pairs = RelationPairs();

        int index = 0;
        while (index < pairs.Length)
        {
            Geometry first = pairs[index][0];
            Geometry second = pairs[index][1];
            string description = PairDescription(first, second);

            Assert.Multiple(() =>
            {
                Assert.That(predicates.Within(first, second),
                    Is.EqualTo(predicates.Contains(second, first)),
                    description + " within/contains");
                Assert.That(predicates.Covers(first, second),
                    Is.EqualTo(predicates.CoveredBy(second, first)),
                    description + " covers/covered-by");
                Assert.That(predicates.Disjoint(first, second),
                    Is.EqualTo(predicates.Disjoint(second, first)),
                    description + " disjoint");
                Assert.That(predicates.Intersects(first, second),
                    Is.EqualTo(predicates.Intersects(second, first)),
                    description + " intersects");
                Assert.That(predicates.Touches(first, second),
                    Is.EqualTo(predicates.Touches(second, first)),
                    description + " touches");
                Assert.That(predicates.Crosses(first, second),
                    Is.EqualTo(predicates.Crosses(second, first)),
                    description + " crosses");
                Assert.That(predicates.Overlaps(first, second),
                    Is.EqualTo(predicates.Overlaps(second, first)),
                    description + " overlaps");
                Assert.That(predicates.WithinDistance(first, second, 2.5),
                    Is.EqualTo(predicates.WithinDistance(
                        second, first, 2.5)),
                    description + " within-distance");
            });
            index++;
        }
    }

    [Test]
    public void SetOperationsSatisfyIdentityAndReconstructionLaws()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryInspector inspector = new GeometryInspector();
        Geometry first = Read(
            "POLYGON ((0 0, 8 0, 8 6, 0 6, 0 0))");
        Geometry second = Read(
            "POLYGON ((4 -2, 10 -2, 10 4, 4 4, 4 -2))");

        Geometry intersection = overlay.Intersection(first, second);
        Geometry firstRemainder = overlay.Difference(first, second);
        Geometry secondRemainder = overlay.Difference(second, first);
        Geometry reconstructedFirst = overlay.Union(
            intersection, firstRemainder);
        Geometry reconstructedSecond = overlay.Union(
            intersection, secondRemainder);
        Geometry symmetricDifference = overlay.SymmetricDifference(
            first, second);
        Geometry remainderUnion = overlay.Union(
            firstRemainder, secondRemainder);

        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreTopologicallyEqual(
                overlay.Intersection(first, first), first), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                overlay.Union(first, first), first), Is.True);
            Assert.That(inspector.IsEmpty(
                overlay.Difference(first, first)), Is.True);
            Assert.That(inspector.IsEmpty(
                overlay.SymmetricDifference(first, first)), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                reconstructedFirst, first), Is.True,
                "(A intersect B) union (A minus B) = A");
            Assert.That(predicates.AreTopologicallyEqual(
                reconstructedSecond, second), Is.True,
                "(A intersect B) union (B minus A) = B");
            Assert.That(predicates.AreTopologicallyEqual(
                symmetricDifference, remainderUnion), Is.True,
                "A xor B = (A minus B) union (B minus A)");
            Assert.That(predicates.AreTopologicallyEqual(
                overlay.Union(first, second),
                overlay.Union(second, first)), Is.True);
            Assert.That(predicates.AreTopologicallyEqual(
                overlay.Intersection(first, second),
                overlay.Intersection(second, first)), Is.True);
        });
    }

    [Test]
    public void OverlayAreasStayWithinSetTheoreticBounds()
    {
        IGeometryOverlay overlay = new GeometryOverlay();
        IGeometryMeasures measures = new GeometryMeasures();
        Geometry first = Read(
            "POLYGON ((0 0, 8 0, 8 6, 0 6, 0 0))");
        Geometry second = Read(
            "POLYGON ((4 -2, 10 -2, 10 4, 4 4, 4 -2))");

        double firstArea = measures.Area(first);
        double secondArea = measures.Area(second);
        double intersectionArea = measures.Area(
            overlay.Intersection(first, second));
        double unionArea = measures.Area(overlay.Union(first, second));
        double firstDifferenceArea = measures.Area(
            overlay.Difference(first, second));
        double secondDifferenceArea = measures.Area(
            overlay.Difference(second, first));
        double symmetricDifferenceArea = measures.Area(
            overlay.SymmetricDifference(first, second));
        const double tolerance = 1.0e-9;

        Assert.Multiple(() =>
        {
            Assert.That(intersectionArea,
                Is.InRange(0.0, Math.Min(firstArea, secondArea)));
            Assert.That(unionArea,
                Is.InRange(Math.Max(firstArea, secondArea),
                    firstArea + secondArea));
            Assert.That(firstDifferenceArea,
                Is.InRange(0.0, firstArea));
            Assert.That(secondDifferenceArea,
                Is.InRange(0.0, secondArea));
            Assert.That(symmetricDifferenceArea,
                Is.InRange(0.0, firstArea + secondArea));
            Assert.That(unionArea,
                Is.EqualTo(firstArea + secondArea - intersectionArea)
                    .Within(tolerance));
            Assert.That(firstDifferenceArea + intersectionArea,
                Is.EqualTo(firstArea).Within(tolerance));
            Assert.That(secondDifferenceArea + intersectionArea,
                Is.EqualTo(secondArea).Within(tolerance));
            Assert.That(symmetricDifferenceArea,
                Is.EqualTo(unionArea - intersectionArea).Within(tolerance));
        });
    }

    [Test]
    public void NormalizationIsIdempotentForEveryCoreGeometryType()
    {
        const int seed = 153001;
        const int casesPerType = 12;
        RandomGeometryFixtureGenerator generator =
            new RandomGeometryFixtureGenerator(seed);
        IGeometryEditor editor = new GeometryEditor(
            new GeometryFactoryOptions());
        IGeometryPredicates predicates = new GeometryPredicates();
        GeometryType[] types = CoreTypes();

        int typeIndex = 0;
        int caseIndex = 0;
        while (typeIndex < types.Length)
        {
            int typeCase = 0;
            while (typeCase < casesPerType)
            {
                Geometry geometry = generator.NextValid(types[typeIndex]);
                int capturedIndex = caseIndex;
                RandomizedRegressionCapture.Check(
                    seed, capturedIndex, geometry, candidate =>
                    {
                        Geometry once = editor.Normalize(candidate);
                        Geometry twice = editor.Normalize(once);
                        Assert.That(predicates.AreExactlyEqual(once, twice),
                            Is.True, "seed=" + seed + ", case=" +
                            capturedIndex + ", type=" + types[typeIndex]);
                    });
                typeCase++;
                caseIndex++;
            }
            typeIndex++;
        }
    }

    [Test]
    public void BuffersRespectContainmentAndAreaMonotonicity()
    {
        IGeometryConstructive constructive = new GeometryConstructive();
        IGeometryPredicates predicates = new GeometryPredicates();
        IGeometryMeasures measures = new GeometryMeasures();
        BufferParameters parameters = new BufferParameters
        {
            QuadrantSegments = 8
        };
        Geometry[] sources =
        [
            Read("POINT (5 5)"),
            Read("LINESTRING (1 1, 9 1, 9 7)"),
            Read("POLYGON ((0 0, 12 0, 12 10, 0 10, 0 0))")
        ];

        int index = 0;
        while (index < sources.Length)
        {
            Geometry source = sources[index];
            Geometry positive = constructive.Buffer(source, 1.5, parameters);
            Assert.Multiple(() =>
            {
                Assert.That(predicates.Covers(positive, source), Is.True,
                    source.GeometryType.ToString());
                Assert.That(measures.Area(positive),
                    Is.GreaterThanOrEqualTo(measures.Area(source)),
                    source.GeometryType.ToString());
            });
            index++;
        }

        Geometry polygon = sources[2];
        Geometry zero = constructive.Buffer(polygon, 0.0, parameters);
        Geometry negative = constructive.Buffer(polygon, -1.5, parameters);
        Assert.Multiple(() =>
        {
            Assert.That(predicates.AreTopologicallyEqual(zero, polygon),
                Is.True, "zero buffer identity");
            Assert.That(predicates.Covers(polygon, negative), Is.True,
                "negative buffer is covered by source");
            Assert.That(measures.Area(negative),
                Is.LessThanOrEqualTo(measures.Area(polygon)));
        });
    }

    [Test]
    public void MakeValidAlwaysProducesValidGeometry()
    {
        IGeometryRepair repair = new GeometryRepair();
        IGeometryValidator validator = new GeometryValidator();
        Geometry[] invalid =
        [
            Read("POLYGON ((0 0, 5 5, 0 5, 5 0, 0 0))"),
            Read("POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0), " +
                "(12 1, 12 3, 14 3, 14 1, 12 1))"),
            Read("MULTIPOLYGON (((0 0, 6 0, 6 6, 0 6, 0 0)), " +
                "((3 2, 9 2, 9 8, 3 8, 3 2)))")
        ];
        RepairMethod[] methods =
            [RepairMethod.Linework, RepairMethod.Structure];

        int geometryIndex = 0;
        while (geometryIndex < invalid.Length)
        {
            Assert.That(validator.ValidateGeometry(
                invalid[geometryIndex]).IsValid, Is.False,
                "precondition " + geometryIndex);
            int methodIndex = 0;
            while (methodIndex < methods.Length)
            {
                RepairParameters parameters = new RepairParameters
                {
                    Method = methods[methodIndex]
                };
                Geometry repaired = repair.MakeValid(
                    invalid[geometryIndex], parameters);
                ValidationResult validation =
                    validator.ValidateGeometry(repaired);
                Assert.That(validation.IsValid, Is.True,
                    "geometry=" + geometryIndex + ", method=" +
                    methods[methodIndex] + ": " + validation.Message);
                methodIndex++;
            }
            geometryIndex++;
        }
    }

    [Test]
    public void SerializationRoundTripsPreserveEveryCoreGeometryType()
    {
        const int seed = 153002;
        const int casesPerType = 12;
        RandomGeometryFixtureGenerator generator =
            new RandomGeometryFixtureGenerator(seed);
        IGeometryEditor editor = new GeometryEditor(
            new GeometryFactoryOptions());
        IGeometryPredicates predicates = new GeometryPredicates();
        IWktCodec textCodec = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true },
            new WktWriteOptions { IncludeSpatialReferenceId = true });
        IWkbCodec binaryCodec = new WkbCodec(
            new WkbReadOptions { Flavor = WkbFlavor.Ewkb },
            new WkbWriteOptions
            {
                ByteOrder = ByteOrder.BigEndian,
                Flavor = WkbFlavor.Ewkb,
                IncludeSpatialReferenceId = true
            });
        IGeoJsonCodec geoJsonCodec = new GeoJsonCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions());
        GeometryType[] types = CoreTypes();

        int typeIndex = 0;
        int caseIndex = 0;
        while (typeIndex < types.Length)
        {
            int typeCase = 0;
            while (typeCase < casesPerType)
            {
                Geometry generated = generator.NextValid(types[typeIndex]);
                Geometry geometry = editor.ReplaceSpatialReferenceId(
                    generated, 3857);
                int capturedIndex = caseIndex;
                RandomizedRegressionCapture.Check(
                    seed, capturedIndex, geometry, candidate =>
                    {
                        Geometry textRoundTrip = textCodec.ReadWkt(
                            textCodec.WriteWkt(candidate));
                        Geometry binaryRoundTrip = binaryCodec.ReadWkb(
                            binaryCodec.WriteWkb(candidate));
                        Assert.Multiple(() =>
                        {
                            Assert.That(predicates.AreExactlyEqual(
                                candidate, textRoundTrip), Is.True,
                                "EWKT seed=" + seed + ", case=" +
                                capturedIndex);
                            Assert.That(predicates.AreExactlyEqual(
                                candidate, binaryRoundTrip), Is.True,
                                "EWKB seed=" + seed + ", case=" +
                                capturedIndex);
                        });
                    });

                if (generated.CoordinateLayout == CoordinateLayout.XY ||
                    generated.CoordinateLayout == CoordinateLayout.XYZ)
                {
                    Geometry geoJsonSource = editor.ReplaceSpatialReferenceId(
                        generated, GeometryConstants.Crs84SpatialReferenceId);
                    GeoJsonDocument document = GeometryDocument(geoJsonSource);
                    GeoJsonDocument roundTrip = geoJsonCodec.ReadGeoJson(
                        geoJsonCodec.WriteGeoJson(document));
                    Assert.That(predicates.AreNormalizedEqual(
                        geoJsonSource, roundTrip.Geometry!.Value!), Is.True,
                        "GeoJSON seed=" + seed + ", case=" + caseIndex);
                }
                typeCase++;
                caseIndex++;
            }
            typeIndex++;
        }
    }

    private static Geometry[][] RelationPairs()
    {
        return
        [
            [Read("POINT (2 2)"),
                Read("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))")],
            [Read("POINT (0 3)"),
                Read("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))")],
            [Read("LINESTRING (-1 3, 7 3)"),
                Read("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0))")],
            [Read("POLYGON ((0 0, 5 0, 5 5, 0 5, 0 0))"),
                Read("POLYGON ((3 2, 8 2, 8 7, 3 7, 3 2))")],
            [Read("POLYGON ((0 0, 2 0, 2 2, 0 2, 0 0))"),
                Read("POLYGON ((5 5, 7 5, 7 7, 5 7, 5 5))")]
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

    private static string PairDescription(Geometry first, Geometry second)
    {
        return first.GeometryType + " / " + second.GeometryType +
            ": " + Write(first) + " / " + Write(second);
    }

    private static Geometry Read(string text)
    {
        return new WktCodec(
            new WktReadOptions(), new WktWriteOptions()).ReadWkt(text);
    }

    private static string Write(Geometry geometry)
    {
        return new WktCodec(
            new WktReadOptions(), new WktWriteOptions()).WriteWkt(geometry);
    }
}
