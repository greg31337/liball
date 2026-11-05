using geo;

namespace geo.Tests;

public class PublicEnumTests
{
    [TestCase(CoordinateLayout.XY, 0)]
    [TestCase(CoordinateLayout.XYZ, 1)]
    [TestCase(CoordinateLayout.XYM, 2)]
    [TestCase(CoordinateLayout.XYZM, 3)]
    public void CoordinateLayoutsHaveStableValues(CoordinateLayout value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(GeometryType.Point, 1)]
    [TestCase(GeometryType.LineString, 2)]
    [TestCase(GeometryType.Polygon, 3)]
    [TestCase(GeometryType.MultiPoint, 4)]
    [TestCase(GeometryType.MultiLineString, 5)]
    [TestCase(GeometryType.MultiPolygon, 6)]
    [TestCase(GeometryType.GeometryCollection, 7)]
    [TestCase(GeometryType.CircularString, 8)]
    [TestCase(GeometryType.CompoundCurve, 9)]
    [TestCase(GeometryType.CurvePolygon, 10)]
    [TestCase(GeometryType.MultiCurve, 11)]
    [TestCase(GeometryType.MultiSurface, 12)]
    [TestCase(GeometryType.PolyhedralSurface, 15)]
    [TestCase(GeometryType.Tin, 16)]
    [TestCase(GeometryType.Triangle, 17)]
    [TestCase(GeometryType.LinearRing, 101)]
    public void GeometryTypesUseOgcCodesWhereDefined(GeometryType value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(TopologicalDimension.Empty, -1)]
    [TestCase(TopologicalDimension.Point, 0)]
    [TestCase(TopologicalDimension.Curve, 1)]
    [TestCase(TopologicalDimension.Surface, 2)]
    [TestCase(RelationDimension.False, -1)]
    [TestCase(RelationDimension.Point, 0)]
    [TestCase(RelationDimension.Curve, 1)]
    [TestCase(RelationDimension.Surface, 2)]
    public void DimensionValuesMatchDe9Im(object value, int expected)
    {
        Assert.That(Convert.ToInt32(value), Is.EqualTo(expected));
    }

    [TestCase(ByteOrder.BigEndian, 0)]
    [TestCase(ByteOrder.LittleEndian, 1)]
    public void ByteOrdersMatchWkbMarkers(ByteOrder value, int expected)
    {
        Assert.That((int)value, Is.EqualTo(expected));
    }

    [TestCase(BufferCapStyle.Round, 1)]
    [TestCase(BufferCapStyle.Flat, 2)]
    [TestCase(BufferCapStyle.Square, 3)]
    [TestCase(BufferJoinStyle.Round, 1)]
    [TestCase(BufferJoinStyle.Mitre, 2)]
    [TestCase(BufferJoinStyle.Bevel, 3)]
    public void BufferStyleValuesMatchEstablishedGeometryConventions(object value, int expected)
    {
        Assert.That(Convert.ToInt32(value), Is.EqualTo(expected));
    }

    [Test]
    public void RemainingPolicyEnumsExposeTheirCompleteStableValueSets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<PrecisionModelType>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetValues<RingOrientation>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetValues<WkbFlavor>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(Enum.GetValues<BoundaryNodeRule>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(Enum.GetValues<TopologicalLocation>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetValues<SegmentIntersectionType>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(Enum.GetValues<SimplificationAlgorithm>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(Enum.GetValues<RepairMethod>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(Enum.GetValues<ValidationError>().Select(value => (int)value), Is.EqualTo(Enumerable.Range(0, 30)));
            Assert.That(Enum.GetValues<GeoJsonObjectType>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetValues<ParseErrorKind>().Select(value => (int)value), Is.EqualTo(Enumerable.Range(0, 14)));
            Assert.That(Enum.GetValues<SpatialUnit>().Select(value => (int)value), Is.EqualTo(Enumerable.Range(0, 7)));
            Assert.That(Enum.GetValues<CurveLinearizationMode>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetValues<CoverageMergeStrategy>().Select(value => (int)value), Is.EqualTo(new[] { 0, 1, 2 }));
        });
    }

    [Test]
    public void PublicDataContractsCarryEveryEnumWithoutConversion()
    {
        GeometryFactoryOptions factory = new GeometryFactoryOptions
        {
            CoordinateLayout = CoordinateLayout.XYZM,
            PrecisionModelType = PrecisionModelType.Fixed,
            RingOrientation = RingOrientation.CounterClockwise,
            BoundaryNodeRule = BoundaryNodeRule.MonovalentEndpoint
        };
        WkbWriteOptions wkb = new WkbWriteOptions
        {
            ByteOrder = ByteOrder.LittleEndian,
            Flavor = WkbFlavor.Ewkb
        };
        RelationMatrix relation = new RelationMatrix
        {
            Cells = new[] { RelationDimension.Surface },
            LeftDimension = TopologicalDimension.Curve,
            RightDimension = TopologicalDimension.Surface
        };
        SegmentIntersectionResult intersection = new SegmentIntersectionResult
        {
            Type = SegmentIntersectionType.CollinearOverlap,
            Location = TopologicalLocation.Boundary
        };
        BufferParameters buffer = new BufferParameters
        {
            CapStyle = BufferCapStyle.Square,
            JoinStyle = BufferJoinStyle.Bevel
        };
        SimplifyParameters simplify = new SimplifyParameters
        {
            Algorithm = SimplificationAlgorithm.VisvalingamWhyatt
        };
        RepairParameters repair = new RepairParameters
        {
            Method = RepairMethod.Structure,
            CoverageMergeStrategy = CoverageMergeStrategy.LargestArea
        };
        ValidationResult validation = new ValidationResult
        {
            Error = ValidationError.SelfIntersection
        };
        GeoJsonDocument geoJson = new GeoJsonDocument
        {
            ObjectType = GeoJsonObjectType.Geometry,
            Geometry = new GeoJsonGeometry { GeometryType = GeometryType.MultiPolygon }
        };
        ParseError parseError = new ParseError { Kind = ParseErrorKind.InvalidGeoJson };
        CircleResult circle = new CircleResult { Unit = SpatialUnit.NauticalMiles };
        CurveLinearizationParameters curve = new CurveLinearizationParameters
        {
            Mode = CurveLinearizationMode.MaximumDeviation
        };

        Assert.Multiple(() =>
        {
            Assert.That(factory.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(factory.PrecisionModelType, Is.EqualTo(PrecisionModelType.Fixed));
            Assert.That(factory.RingOrientation, Is.EqualTo(RingOrientation.CounterClockwise));
            Assert.That(factory.BoundaryNodeRule, Is.EqualTo(BoundaryNodeRule.MonovalentEndpoint));
            Assert.That(wkb.ByteOrder, Is.EqualTo(ByteOrder.LittleEndian));
            Assert.That(wkb.Flavor, Is.EqualTo(WkbFlavor.Ewkb));
            Assert.That(relation.Cells, Is.EqualTo(new[] { RelationDimension.Surface }));
            Assert.That(relation.LeftDimension, Is.EqualTo(TopologicalDimension.Curve));
            Assert.That(relation.RightDimension, Is.EqualTo(TopologicalDimension.Surface));
            Assert.That(intersection.Type, Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
            Assert.That(intersection.Location, Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(buffer.CapStyle, Is.EqualTo(BufferCapStyle.Square));
            Assert.That(buffer.JoinStyle, Is.EqualTo(BufferJoinStyle.Bevel));
            Assert.That(simplify.Algorithm, Is.EqualTo(SimplificationAlgorithm.VisvalingamWhyatt));
            Assert.That(repair.Method, Is.EqualTo(RepairMethod.Structure));
            Assert.That(repair.CoverageMergeStrategy, Is.EqualTo(CoverageMergeStrategy.LargestArea));
            Assert.That(validation.Error, Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(geoJson.ObjectType, Is.EqualTo(GeoJsonObjectType.Geometry));
            Assert.That(geoJson.Geometry?.GeometryType, Is.EqualTo(GeometryType.MultiPolygon));
            Assert.That(parseError.Kind, Is.EqualTo(ParseErrorKind.InvalidGeoJson));
            Assert.That(circle.Unit, Is.EqualTo(SpatialUnit.NauticalMiles));
            Assert.That(curve.Mode, Is.EqualTo(CurveLinearizationMode.MaximumDeviation));
        });
    }
}
