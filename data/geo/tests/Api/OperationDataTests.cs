using geo;

namespace geo.Tests;

public class OperationDataTests
{
    [Test]
    public void OperationParametersExposeSafeDefaultsAndRetainConfiguration()
    {
        GeometryFactoryOptions factory = new GeometryFactoryOptions();
        BufferParameters buffer = new BufferParameters();
        SimplifyParameters simplify = new SimplifyParameters();
        RepairParameters repair = new RepairParameters();
        CurveLinearizationParameters curve = new CurveLinearizationParameters();

        buffer.QuadrantSegments = 12;
        buffer.MitreLimit = 3.5;
        buffer.SingleSided = true;
        simplify.Tolerance = 0.25;
        simplify.KeepCollapsedComponents = true;
        repair.SnapTolerance = 0.001;
        repair.MaximumGapWidth = 2.0;
        curve.Tolerance = 0.005;
        curve.MaximumSegments = 2048;

        Assert.Multiple(() =>
        {
            Assert.That(factory.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
            Assert.That(factory.CoordinateLayout, Is.EqualTo(CoordinateLayout.XY));
            Assert.That(factory.PrecisionModelType, Is.EqualTo(PrecisionModelType.Floating));
            Assert.That(factory.PrecisionScale, Is.EqualTo(1.0));
            Assert.That(factory.PrecisionOffsetX, Is.Zero);
            Assert.That(factory.PrecisionOffsetY, Is.Zero);

            Assert.That(buffer.CapStyle, Is.EqualTo(BufferCapStyle.Round));
            Assert.That(buffer.JoinStyle, Is.EqualTo(BufferJoinStyle.Round));
            Assert.That(buffer.QuadrantSegments, Is.EqualTo(12));
            Assert.That(buffer.MitreLimit, Is.EqualTo(3.5));
            Assert.That(buffer.SingleSided, Is.True);
            Assert.That(buffer.Precision.Type, Is.EqualTo(PrecisionModelType.Floating));

            Assert.That(simplify.Algorithm, Is.EqualTo(SimplificationAlgorithm.DouglasPeucker));
            Assert.That(simplify.Tolerance, Is.EqualTo(0.25));
            Assert.That(simplify.PreserveEndpoints, Is.True);
            Assert.That(simplify.KeepCollapsedComponents, Is.True);

            Assert.That(repair.Method, Is.EqualTo(RepairMethod.Linework));
            Assert.That(repair.CoverageMergeStrategy, Is.EqualTo(CoverageMergeStrategy.KeepFirst));
            Assert.That(repair.KeepCollapsedComponents, Is.False);
            Assert.That(repair.RemoveNonFiniteCoordinates, Is.True);
            Assert.That(repair.RemoveRepeatedCoordinates, Is.True);
            Assert.That(repair.SnapTolerance, Is.EqualTo(0.001));
            Assert.That(repair.MaximumGapWidth, Is.EqualTo(2.0));

            Assert.That(curve.Mode, Is.EqualTo(CurveLinearizationMode.MaximumDeviation));
            Assert.That(curve.Tolerance, Is.EqualTo(0.005));
            Assert.That(curve.MaximumSegments, Is.EqualTo(2048));
        });
    }

    [Test]
    public void CodecOptionsAreBoundedStrictAndRoundTripOrientedByDefault()
    {
        WktReadOptions wktRead = new WktReadOptions();
        WktWriteOptions wktWrite = new WktWriteOptions();
        WkbReadOptions wkbRead = new WkbReadOptions();
        WkbWriteOptions wkbWrite = new WkbWriteOptions();
        GeoJsonReadOptions geoJsonRead = new GeoJsonReadOptions();
        GeoJsonWriteOptions geoJsonWrite = new GeoJsonWriteOptions();

        Assert.Multiple(() =>
        {
            Assert.That(wktRead.MaximumCoordinates, Is.EqualTo(GeometryConstants.DefaultMaximumCoordinates));
            Assert.That(wktRead.MaximumComponents, Is.EqualTo(GeometryConstants.DefaultMaximumComponents));
            Assert.That(wktRead.MaximumNestingDepth, Is.EqualTo(GeometryConstants.DefaultMaximumNestingDepth));
            Assert.That(wktRead.AllowExtendedTypes, Is.True);
            Assert.That(wktRead.AllowSpatialReferenceId, Is.False);
            Assert.That(wktWrite.DecimalPrecision, Is.EqualTo(17));
            Assert.That(wktWrite.TrimTrailingZeros, Is.True);
            Assert.That(wktWrite.IncludeSpatialReferenceId, Is.False);
            Assert.That(wktWrite.AllowExtendedTypes, Is.True);

            Assert.That(wkbRead.Flavor, Is.EqualTo(WkbFlavor.Ogc));
            Assert.That(wkbRead.MaximumCoordinates, Is.EqualTo(GeometryConstants.DefaultMaximumCoordinates));
            Assert.That(wkbRead.MaximumComponents, Is.EqualTo(GeometryConstants.DefaultMaximumComponents));
            Assert.That(wkbRead.MaximumNestingDepth, Is.EqualTo(GeometryConstants.DefaultMaximumNestingDepth));
            Assert.That(wkbRead.MaximumInputBytes, Is.EqualTo(GeometryConstants.DefaultMaximumInputBytes));
            Assert.That(wkbRead.RequireExactLength, Is.True);
            Assert.That(wkbWrite.ByteOrder, Is.EqualTo(ByteOrder.BigEndian));
            Assert.That(wkbWrite.Flavor, Is.EqualTo(WkbFlavor.Ogc));
            Assert.That(wkbWrite.IncludeSpatialReferenceId, Is.False);

            Assert.That(geoJsonRead.Strict, Is.True);
            Assert.That(geoJsonRead.PreserveForeignMembers, Is.True);
            Assert.That(geoJsonRead.MaximumCoordinates, Is.EqualTo(GeometryConstants.DefaultMaximumCoordinates));
            Assert.That(geoJsonRead.MaximumComponents, Is.EqualTo(GeometryConstants.DefaultMaximumComponents));
            Assert.That(geoJsonRead.MaximumNestingDepth, Is.EqualTo(GeometryConstants.DefaultMaximumNestingDepth));
            Assert.That(geoJsonRead.MaximumInputBytes, Is.EqualTo(GeometryConstants.DefaultMaximumInputBytes));
            Assert.That(geoJsonWrite.Strict, Is.True);
            Assert.That(geoJsonWrite.CoordinatePrecision, Is.EqualTo(15));
            Assert.That(geoJsonWrite.IncludeBoundingBox, Is.False);
            Assert.That(geoJsonWrite.IncludeForeignMembers, Is.True);
            Assert.That(geoJsonWrite.NormalizeRingOrientation, Is.True);
            Assert.That(geoJsonWrite.Indented, Is.False);
        });
    }

    [Test]
    public void TryResultsCarrySuccessValuesFailureDetailsAndBufferSizes()
    {
        ParseError error = new ParseError
        {
            Kind = ParseErrorKind.UnexpectedToken,
            Message = "Expected coordinate",
            Offset = 27,
            JsonPath = "$.geometry.coordinates[1]",
            ComponentPath = "Geometry/Point"
        };
        Geometry geometry = new Geometry();
        GeoJsonDocument document = new GeoJsonDocument();
        GeometryReadResult geometrySuccess = new GeometryReadResult
        {
            Success = true,
            Geometry = geometry,
            Consumed = 32
        };
        GeometryReadResult geometryFailure = new GeometryReadResult { Error = error, Consumed = 27 };
        GeoJsonReadResult geoJsonSuccess = new GeoJsonReadResult
        {
            Success = true,
            Document = document,
            Consumed = 128
        };
        WriteResult writeFailure = new WriteResult
        {
            RequiredSize = 256,
            WrittenSize = 64,
            Error = error
        };

        Assert.Multiple(() =>
        {
            Assert.That(geometrySuccess.Success, Is.True);
            Assert.That(geometrySuccess.Geometry, Is.SameAs(geometry));
            Assert.That(geometrySuccess.Error, Is.Null);
            Assert.That(geometrySuccess.Consumed, Is.EqualTo(32));
            Assert.That(geometryFailure.Success, Is.False);
            Assert.That(geometryFailure.Geometry, Is.Null);
            Assert.That(geometryFailure.Error, Is.SameAs(error));
            Assert.That(geometryFailure.Consumed, Is.EqualTo(27));
            Assert.That(geoJsonSuccess.Success, Is.True);
            Assert.That(geoJsonSuccess.Document, Is.SameAs(document));
            Assert.That(geoJsonSuccess.Error, Is.Null);
            Assert.That(geoJsonSuccess.Consumed, Is.EqualTo(128));
            Assert.That(writeFailure.Success, Is.False);
            Assert.That(writeFailure.RequiredSize, Is.EqualTo(256));
            Assert.That(writeFailure.WrittenSize, Is.EqualTo(64));
            Assert.That(writeFailure.Error, Is.SameAs(error));
            Assert.That(error.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(error.Message, Is.EqualTo("Expected coordinate"));
            Assert.That(error.Offset, Is.EqualTo(27));
            Assert.That(error.JsonPath, Is.EqualTo("$.geometry.coordinates[1]"));
            Assert.That(error.ComponentPath, Is.EqualTo("Geometry/Point"));
        });
    }

    [Test]
    public void DiagnosticResultsCarryRelationDistanceIntersectionAndValidationWitnesses()
    {
        RelationMatrix relation = new RelationMatrix();
        Coordinate first = new Coordinate { X = 1.0, Y = 2.0 };
        Coordinate second = new Coordinate { X = 4.0, Y = 6.0 };
        LinearLocation firstLocation = new LinearLocation { ComponentIndex = 1, SegmentIndex = 2, SegmentFraction = 0.25 };
        LinearLocation secondLocation = new LinearLocation { ComponentIndex = 3, SegmentIndex = 4, SegmentFraction = 0.75 };
        DistanceResult distance = new DistanceResult
        {
            HasResult = true,
            Distance = 5.0,
            FirstCoordinate = first,
            SecondCoordinate = second,
            FirstLocation = firstLocation,
            SecondLocation = secondLocation
        };
        SegmentIntersectionResult intersection = new SegmentIntersectionResult
        {
            Type = SegmentIntersectionType.CollinearOverlap,
            Location = TopologicalLocation.Boundary,
            FirstIntersection = first,
            SecondIntersection = second
        };
        ValidationResult validation = new ValidationResult
        {
            IsValid = false,
            Error = ValidationError.SelfIntersection,
            Message = "Ring crosses itself",
            Coordinate = first,
            ComponentPath = "Polygon/Shell"
        };
        Geometry invalidGeometry = new Geometry();
        CoverageValidationResult coverage = new CoverageValidationResult
        {
            IsValid = false,
            Error = ValidationError.CoverageGap,
            Message = "Coverage gap",
            InvalidGeometry = invalidGeometry,
            Coordinate = second,
            ComponentPath = "MultiPolygon/1"
        };

        Assert.Multiple(() =>
        {
            Assert.That(relation.Cells, Has.Length.EqualTo(9));
            Assert.That(relation.Cells, Has.All.EqualTo(RelationDimension.False));
            Assert.That(relation.LeftDimension, Is.EqualTo(TopologicalDimension.Empty));
            Assert.That(relation.RightDimension, Is.EqualTo(TopologicalDimension.Empty));
            Assert.That(distance.HasResult, Is.True);
            Assert.That(distance.Distance, Is.EqualTo(5.0));
            Assert.That(distance.FirstCoordinate, Is.SameAs(first));
            Assert.That(distance.SecondCoordinate, Is.SameAs(second));
            Assert.That(distance.FirstLocation, Is.SameAs(firstLocation));
            Assert.That(distance.SecondLocation, Is.SameAs(secondLocation));
            Assert.That(intersection.Type, Is.EqualTo(SegmentIntersectionType.CollinearOverlap));
            Assert.That(intersection.Location, Is.EqualTo(TopologicalLocation.Boundary));
            Assert.That(intersection.FirstIntersection, Is.SameAs(first));
            Assert.That(intersection.SecondIntersection, Is.SameAs(second));
            Assert.That(intersection.IsProper, Is.False);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Error, Is.EqualTo(ValidationError.SelfIntersection));
            Assert.That(validation.Message, Is.EqualTo("Ring crosses itself"));
            Assert.That(validation.Coordinate, Is.SameAs(first));
            Assert.That(validation.ComponentPath, Is.EqualTo("Polygon/Shell"));
            Assert.That(coverage.IsValid, Is.False);
            Assert.That(coverage.Error, Is.EqualTo(ValidationError.CoverageGap));
            Assert.That(coverage.InvalidGeometry, Is.SameAs(invalidGeometry));
            Assert.That(coverage.Coordinate, Is.SameAs(second));
            Assert.That(coverage.ComponentPath, Is.EqualTo("MultiPolygon/1"));
        });
    }

    [Test]
    public void LinearIndexAndCircleDataCarryLocationsQueriesPayloadsAndWitnesses()
    {
        LinearLocation location = new LinearLocation
        {
            ComponentIndex = 2,
            SegmentIndex = 7,
            SegmentFraction = 0.625
        };
        Envelope envelope = new Envelope { MinX = 1.0, MinY = 2.0, MaxX = 3.0, MaxY = 4.0 };
        Geometry geometry = new Geometry();
        object payload = new object();
        SpatialIndexItem item = new SpatialIndexItem
        {
            Id = 99,
            Envelope = envelope,
            Geometry = geometry,
            Payload = payload
        };
        SpatialIndexQuery query = new SpatialIndexQuery
        {
            Envelope = envelope,
            Geometry = geometry,
            RefineWithGeometry = true,
            MaximumResults = 5,
            MaximumDistance = 12.5
        };
        Coordinate center = new Coordinate { X = 10.0, Y = 20.0 };
        Coordinate witness = new Coordinate { X = 13.0, Y = 24.0 };
        Coordinate[] witnesses = new[] { witness };
        CircleResult circle = new CircleResult
        {
            Geometry = geometry,
            Center = center,
            Radius = 5.0,
            Unit = SpatialUnit.Kilometers,
            Witnesses = witnesses
        };

        Assert.Multiple(() =>
        {
            Assert.That(location.IsValid, Is.True);
            Assert.That(location.ComponentIndex, Is.EqualTo(2));
            Assert.That(location.SegmentIndex, Is.EqualTo(7));
            Assert.That(location.SegmentFraction, Is.EqualTo(0.625));
            Assert.That(item.Id, Is.EqualTo(99));
            Assert.That(item.Envelope, Is.SameAs(envelope));
            Assert.That(item.Geometry, Is.SameAs(geometry));
            Assert.That(item.Payload, Is.SameAs(payload));
            Assert.That(query.Envelope, Is.SameAs(envelope));
            Assert.That(query.Geometry, Is.SameAs(geometry));
            Assert.That(query.RefineWithGeometry, Is.True);
            Assert.That(query.MaximumResults, Is.EqualTo(5));
            Assert.That(query.MaximumDistance, Is.EqualTo(12.5));
            Assert.That(circle.Geometry, Is.SameAs(geometry));
            Assert.That(circle.Center, Is.SameAs(center));
            Assert.That(circle.Radius, Is.EqualTo(5.0));
            Assert.That(circle.Unit, Is.EqualTo(SpatialUnit.Kilometers));
            Assert.That(circle.Witnesses, Is.SameAs(witnesses));
            Assert.That(circle.Witnesses[0], Is.SameAs(witness));
        });
    }
}
