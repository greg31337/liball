using geo;

namespace geo.Tests;

public class PublicStaticDataTests
{
    [Test]
    public void GeometryConstantsExposeDocumentedMetadataAndSafeDefaults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeometryConstants.UnknownSpatialReferenceId, Is.Zero);
            Assert.That(GeometryConstants.Crs84SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(double.IsNaN(GeometryConstants.NoDataOrdinate), Is.True);
            Assert.That(GeometryConstants.DefaultMaximumCoordinates, Is.EqualTo(1000000));
            Assert.That(GeometryConstants.DefaultMaximumComponents, Is.EqualTo(100000));
            Assert.That(GeometryConstants.DefaultMaximumNestingDepth, Is.EqualTo(64));
            Assert.That(GeometryConstants.DefaultMaximumInputBytes, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(GeometryConstants.DefaultAbsoluteTolerance, Is.EqualTo(1e-12));
            Assert.That(GeometryConstants.DefaultRelativeTolerance, Is.EqualTo(1e-12));
        });
    }

    [Test]
    public void WkbTypeCodesComposeStandardDimensionalCodesAndEwkbFlags()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WkbTypeCodes.Point, Is.EqualTo((int)GeometryType.Point));
            Assert.That(WkbTypeCodes.LineString, Is.EqualTo((int)GeometryType.LineString));
            Assert.That(WkbTypeCodes.Polygon, Is.EqualTo((int)GeometryType.Polygon));
            Assert.That(WkbTypeCodes.MultiPoint, Is.EqualTo((int)GeometryType.MultiPoint));
            Assert.That(WkbTypeCodes.MultiLineString, Is.EqualTo((int)GeometryType.MultiLineString));
            Assert.That(WkbTypeCodes.MultiPolygon, Is.EqualTo((int)GeometryType.MultiPolygon));
            Assert.That(WkbTypeCodes.GeometryCollection, Is.EqualTo((int)GeometryType.GeometryCollection));
            Assert.That(WkbTypeCodes.CircularString, Is.EqualTo((int)GeometryType.CircularString));
            Assert.That(WkbTypeCodes.CompoundCurve, Is.EqualTo((int)GeometryType.CompoundCurve));
            Assert.That(WkbTypeCodes.CurvePolygon, Is.EqualTo((int)GeometryType.CurvePolygon));
            Assert.That(WkbTypeCodes.MultiCurve, Is.EqualTo((int)GeometryType.MultiCurve));
            Assert.That(WkbTypeCodes.MultiSurface, Is.EqualTo((int)GeometryType.MultiSurface));
            Assert.That(WkbTypeCodes.PolyhedralSurface, Is.EqualTo((int)GeometryType.PolyhedralSurface));
            Assert.That(WkbTypeCodes.Tin, Is.EqualTo((int)GeometryType.Tin));
            Assert.That(WkbTypeCodes.Triangle, Is.EqualTo((int)GeometryType.Triangle));
            Assert.That(WkbTypeCodes.Point + WkbTypeCodes.ZOffset, Is.EqualTo(1001));
            Assert.That(WkbTypeCodes.Point + WkbTypeCodes.MOffset, Is.EqualTo(2001));
            Assert.That(WkbTypeCodes.Point + WkbTypeCodes.ZmOffset, Is.EqualTo(3001));
            Assert.That(WkbTypeCodes.EwkbZFlag | WkbTypeCodes.EwkbMFlag | WkbTypeCodes.EwkbSridFlag, Is.EqualTo(0xe0000000u));
            Assert.That(WkbTypeCodes.EwkbTypeMask, Is.EqualTo(0x1fffffffu));
        });
    }

    [Test]
    public void De9ImPatternsExposeCanonicalNamedPredicateMasks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(De9ImPatterns.TopologicallyEquals, Is.EqualTo("T*F**FFF*"));
            Assert.That(De9ImPatterns.Disjoint, Is.EqualTo("FF*FF****"));
            Assert.That(De9ImPatterns.Within, Is.EqualTo("T*F**F***"));
            Assert.That(De9ImPatterns.Contains, Is.EqualTo("T*****FF*"));
            Assert.That(De9ImPatterns.ContainsProperly, Is.EqualTo("T**FF*FF*"));
            Assert.That(De9ImPatterns.TouchesInteriorBoundary, Is.EqualTo("FT*******"));
            Assert.That(De9ImPatterns.TouchesBoundaryInterior, Is.EqualTo("F**T*****"));
            Assert.That(De9ImPatterns.TouchesBoundaryBoundary, Is.EqualTo("F***T****"));
            Assert.That(De9ImPatterns.CrossesLowerDimension, Is.EqualTo("T*T******"));
            Assert.That(De9ImPatterns.CrossesHigherDimension, Is.EqualTo("T*****T**"));
            Assert.That(De9ImPatterns.CrossesLineLine, Is.EqualTo("0********"));
            Assert.That(De9ImPatterns.OverlapsPointOrArea, Is.EqualTo("T*T***T**"));
            Assert.That(De9ImPatterns.OverlapsLine, Is.EqualTo("1*T***T**"));
        });
    }

    [Test]
    public void GeoJsonNamesMatchRfc7946AndRfc8142Tokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeoJsonNames.MediaType, Is.EqualTo("application/geo+json"));
            Assert.That(GeoJsonNames.SequenceMediaType, Is.EqualTo("application/geo+json-seq"));
            Assert.That(GeoJsonNames.TypeMember, Is.EqualTo("type"));
            Assert.That(GeoJsonNames.CoordinatesMember, Is.EqualTo("coordinates"));
            Assert.That(GeoJsonNames.GeometriesMember, Is.EqualTo("geometries"));
            Assert.That(GeoJsonNames.GeometryMember, Is.EqualTo("geometry"));
            Assert.That(GeoJsonNames.PropertiesMember, Is.EqualTo("properties"));
            Assert.That(GeoJsonNames.FeaturesMember, Is.EqualTo("features"));
            Assert.That(GeoJsonNames.IdMember, Is.EqualTo("id"));
            Assert.That(GeoJsonNames.BoundingBoxMember, Is.EqualTo("bbox"));
            Assert.That(GeoJsonNames.PointType, Is.EqualTo("Point"));
            Assert.That(GeoJsonNames.LineStringType, Is.EqualTo("LineString"));
            Assert.That(GeoJsonNames.PolygonType, Is.EqualTo("Polygon"));
            Assert.That(GeoJsonNames.MultiPointType, Is.EqualTo("MultiPoint"));
            Assert.That(GeoJsonNames.MultiLineStringType, Is.EqualTo("MultiLineString"));
            Assert.That(GeoJsonNames.MultiPolygonType, Is.EqualTo("MultiPolygon"));
            Assert.That(GeoJsonNames.GeometryCollectionType, Is.EqualTo("GeometryCollection"));
            Assert.That(GeoJsonNames.FeatureType, Is.EqualTo("Feature"));
            Assert.That(GeoJsonNames.FeatureCollectionType, Is.EqualTo("FeatureCollection"));
            Assert.That(GeoJsonNames.RecordSeparator, Is.EqualTo('\u001e'));
            Assert.That(GeoJsonNames.RecordTerminator, Is.EqualTo('\n'));
        });
    }
}
