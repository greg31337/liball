namespace geo;

/// <summary>Provides owning data values used to configure and report geometry operations.</summary>
public interface IGeometryDataProvider
{
    /// <summary>Gets a coordinate value.</summary>
    /// <returns>The coordinate value.</returns>
    Coordinate GetCoordinate();

    /// <summary>Gets an envelope value.</summary>
    /// <returns>The envelope value.</returns>
    Envelope GetEnvelope();

    /// <summary>Gets a precision model.</summary>
    /// <returns>The precision model.</returns>
    PrecisionModel GetPrecisionModel();

    /// <summary>Gets an affine transformation.</summary>
    /// <returns>The affine transformation.</returns>
    AffineTransform GetAffineTransform();

    /// <summary>Gets geometry factory options.</summary>
    /// <returns>The geometry factory options.</returns>
    GeometryFactoryOptions GetFactoryOptions();

    /// <summary>Gets well-known binary write options.</summary>
    /// <returns>The well-known binary write options.</returns>
    WkbWriteOptions GetWkbWriteOptions();

    /// <summary>Gets well-known text read options.</summary>
    /// <returns>The well-known text read options.</returns>
    WktReadOptions GetWktReadOptions();

    /// <summary>Gets well-known text write options.</summary>
    /// <returns>The well-known text write options.</returns>
    WktWriteOptions GetWktWriteOptions();

    /// <summary>Gets well-known binary read options.</summary>
    /// <returns>The well-known binary read options.</returns>
    WkbReadOptions GetWkbReadOptions();

    /// <summary>Gets GeoJSON read options.</summary>
    /// <returns>The GeoJSON read options.</returns>
    GeoJsonReadOptions GetGeoJsonReadOptions();

    /// <summary>Gets GeoJSON write options.</summary>
    /// <returns>The GeoJSON write options.</returns>
    GeoJsonWriteOptions GetGeoJsonWriteOptions();

    /// <summary>Gets a geometry read result.</summary>
    /// <returns>The geometry read result.</returns>
    GeometryReadResult GetGeometryReadResult();

    /// <summary>Gets a GeoJSON read result.</summary>
    /// <returns>The GeoJSON read result.</returns>
    GeoJsonReadResult GetGeoJsonReadResult();

    /// <summary>Gets a write result.</summary>
    /// <returns>The write result.</returns>
    WriteResult GetWriteResult();

    /// <summary>Gets a topological relation matrix.</summary>
    /// <returns>The topological relation matrix.</returns>
    RelationMatrix GetRelationMatrix();

    /// <summary>Gets a segment intersection result.</summary>
    /// <returns>The segment intersection result.</returns>
    SegmentIntersectionResult GetSegmentIntersectionResult();

    /// <summary>Gets a distance result.</summary>
    /// <returns>The distance result.</returns>
    DistanceResult GetDistanceResult();

    /// <summary>Gets buffer parameters.</summary>
    /// <returns>The buffer parameters.</returns>
    BufferParameters GetBufferParameters();

    /// <summary>Gets simplification parameters.</summary>
    /// <returns>The simplification parameters.</returns>
    SimplifyParameters GetSimplifyParameters();

    /// <summary>Gets geometry repair parameters.</summary>
    /// <returns>The geometry repair parameters.</returns>
    RepairParameters GetRepairParameters();

    /// <summary>Gets a geometry validation result.</summary>
    /// <returns>The geometry validation result.</returns>
    ValidationResult GetValidationResult();

    /// <summary>Gets a coverage validation result.</summary>
    /// <returns>The coverage validation result.</returns>
    CoverageValidationResult GetCoverageValidationResult();

    /// <summary>Gets a linear location.</summary>
    /// <returns>The linear location.</returns>
    LinearLocation GetLinearLocation();

    /// <summary>Gets a spatial index item.</summary>
    /// <returns>The spatial index item.</returns>
    SpatialIndexItem GetSpatialIndexItem();

    /// <summary>Gets a spatial index query.</summary>
    /// <returns>The spatial index query.</returns>
    SpatialIndexQuery GetSpatialIndexQuery();

    /// <summary>Gets a GeoJSON document.</summary>
    /// <returns>The GeoJSON document.</returns>
    GeoJsonDocument GetGeoJsonDocument();

    /// <summary>Gets a parse error.</summary>
    /// <returns>The parse error.</returns>
    ParseError GetParseError();

    /// <summary>Gets a circle result.</summary>
    /// <returns>The circle result.</returns>
    CircleResult GetCircleResult();

    /// <summary>Gets curve linearization parameters.</summary>
    /// <returns>The curve linearization parameters.</returns>
    CurveLinearizationParameters GetCurveLinearizationParameters();
}
