# Universal geospatial geometry library development plan

## Goal and scope

Build a dependency-free .NET geometry engine whose public model follows OGC Simple Feature Access (SFA) 1.2.1 / ISO 19125, whose JSON interchange follows GeoJSON RFC 7946, and whose computational geometry coverage is comparable to the core of JTS, GEOS, NetTopologySuite, Shapely, Boost.Geometry, and Turf.

The primary kernel is deterministic planar Euclidean geometry in X/Y. Z and M ordinates are preserved, validated, serialized, interpolated where an operation has an unambiguous source segment, and otherwise assigned a documented no-data value; they do not alter 2D topology. A separate geographic service supplies explicit WGS84 great-circle and rhumb calculations for RFC 7946 data so callers cannot accidentally mix angular coordinates with planar distance, area, or buffer units.

The first complete compatibility target is the seven linear SFA/GeoJSON geometry kinds: Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, and GeometryCollection. LinearRing is a polygon component. The SFA surface types Triangle, TIN, and PolyhedralSurface and the interoperable curve extensions CircularString, CompoundCurve, CurvePolygon, MultiCurve, and MultiSurface are later phases built on the complete linear kernel. Strict RFC 7946 output never emits those extended types.

This plan covers library implementation and functional tests. It deliberately contains no performance/benchmark, documentation, packaging, release, or syntax-policy test tasks.

## Standards and downloaded research

The following resources are already stored under specs/ and are inputs to implementation, not source dependencies:

| Local resource | Authority and use |
| --- | --- |
| specs/ogc-sfa-1.2.1-part-1-common-architecture.pdf | [OGC 06-103r4](https://docs.ogc.org/is/06-103r4/06-103r4.pdf): geometry model, dimensions, validity, DE-9IM, WKT, WKB, and SFA semantics; its technical content is identical to ISO 19125-1:2004. |
| specs/ogc-sfa-1.2.1-part-2-sql-option.pdf | [OGC 06-104r4](https://docs.ogc.org/is/06-104r4/06-104r4.pdf): the complete ST-style operation vocabulary and conformance behavior corresponding to ISO 19125-2. |
| specs/ogc-sfa-standard-overview.html | [OGC SFA overview](https://www.ogc.org/standards/sfa/): status, versions, model summary, and ISO relationship. |
| specs/rfc7946-geojson.txt | [RFC 7946](https://www.rfc-editor.org/rfc/rfc7946.html): normative GeoJSON object, geometry, feature, CRS84, ring, bbox, antimeridian, precision, foreign-member, and interoperability rules. |
| specs/rfc8142-geojson-text-sequences.txt | [RFC 8142](https://www.rfc-editor.org/rfc/rfc8142.html): GeoJSON streaming sequence framing and media-type rules. |
| specs/jts-features.html | [JTS feature inventory](https://locationtech.github.io/jts/jts-features.html) and the reference architecture for robust predicates, noding, overlay, precision, constructions, and indexes. |
| specs/geos-c-api.html | [GEOS C API](https://libgeos.org/doxygen/geos__c_8h.html): current operation inventory, options, prepared geometry surface, STRtree behavior, repair, coverage, and curved-geometry extensions. |
| specs/nettopologysuite-geometries-api.html | [NetTopologySuite geometry API](https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.html): .NET/JTS model conventions and a candidate tests-only differential oracle. |
| specs/shapely-2.1.2-manual.pdf | [Shapely 2.1.2 manual](https://shapely.readthedocs.io/_/downloads/en/2.1.2/pdf/): user-facing model, predicates, set operations, constructions, validation, prepared geometries, and STRtree behavior. |
| specs/boost-geometry-algorithms.html | [Boost.Geometry algorithms](https://www.boost.org/latest/libs/geometry/doc/html/geometry/reference/algorithms.html): generic algorithm inventory and dynamic R-tree design reference. |
| specs/turf-getting-started.html | [Turf guide](https://turfjs.org/docs/getting-started): GeoJSON feature workflow and explicit geographic measurement conventions. |

Official catalogue metadata that cannot be redistributed as full ISO text remains linked at [ISO 19125-1:2004](https://www.iso.org/standard/40114.html) and [ISO 19125-2:2004](https://www.iso.org/standard/40115.html). The latter is withdrawn, but its corresponding OGC 06-104r4 document remains useful as the operation/conformance reference.

### Findings adopted from existing libraries

| Library family | Capabilities adopted | Boundary of adoption |
| --- | --- | --- |
| JTS / NetTopologySuite | Explicit precision model; robust orientation/intersection; indexed noding; DE-9IM; OverlayNG-style result construction; validation and GeometryFixer semantics; linear referencing; coverage operations; prepared geometries; STRtree; triangulation and Voronoi. | API inheritance, properties, overloads, LINQ, generic public types, and mutable filters cannot be copied because syntax.md forbids them. |
| GEOS / Shapely | Complete predicate, metric, overlay, buffer, hull, clipping, simplification, polygonization, line merge, snapping, repair, nearest-point, minimum-shape, and prepared operation surface; canonical heterogeneous results. | Native bindings and GEOS itself are forbidden under src/. All algorithms are clean .NET implementations. |
| Boost.Geometry | Coordinate-strategy separation, low-level segment algorithms, envelope-first filtering, dynamic R-tree insertion/removal, and explicit correction/orientation behavior. | Template/concept APIs are replaced by fixed public data and service interfaces. |
| Turf | Full RFC 7946 Feature and FeatureCollection handling, flatten/combine/explode workflows, bbox clipping, antimeridian awareness, and separately named great-circle/rhumb utilities. | Raster/grid interpolation, contours/isobands, clustering/statistics, random data generation, routing, and map projections are outside a geometry kernel. |

## Governing implementation constraints

- src/ uses only the .NET base class libraries. System.Buffers, System.Collections.Generic, System.IO, System.Numerics, System.Text, and System.Text.Json are allowed; no NuGet or native geometry dependency is allowed.
- Span, ReadOnlySpan, ArrayPool, and stream APIs are used where they materially avoid copying, but every public API also has an owning result form whose lifetime is straightforward.
- All public types live under src/Api and exactly follow the directory categories in syntax.md. All implementation helpers are internal and live outside src/Api.
- Public geometry types cannot inherit from a base class. Geometry is therefore a validated tagged union containing exactly one nullable payload field selected by GeometryType. Every algorithm dispatches on that tag.
- Public data classes expose fields, have no public methods, and are treated as caller-owned mutable transfer values. Service constructors snapshot configuration; prepared geometries and indexes snapshot geometry/index inputs. Operations do not mutate caller data unless their contract is explicitly an editor operation, and editor operations return new values.
- Public behavior is supplied through non-inheriting interfaces with uniquely named instance methods. Public implementation classes expose one constructor and only members declared by their interfaces. There are no public static methods or overloaded method names.
- Try-style methods return result data classes because out/ref parameters are forbidden. Exceptions remain the general failure mechanism; Try methods are added for expected parse, buffer-size, validity, and query misses.
- All public types and members receive the XML comments required by syntax.md as part of their implementation task; there is no separate documentation task.
- Tests may use external packages. NetTopologySuite can be referenced only by tests as a differential oracle, never as the definition of behavior when it conflicts with OGC/RFC requirements.

## Behavioral contract and invariants

### Geometry and coordinate semantics

- Coordinate layouts are XY, XYZ, XYM, and XYZM. Missing Z/M is represented by double.NaN inside Coordinate and interpreted through CoordinateLayout, never by nullable value types.
- Geometry stores GeometryType, CoordinateLayout, SpatialReferenceId, and one matching payload. Unknown SpatialReferenceId uses a named integer constant. Binary operations require equal known SRIDs; an explicit coercion operation can change metadata but does not transform coordinates.
- Empty values exist for every geometry type. Empty Point has a null coordinate payload; empty sequences and collections use empty arrays. Null arrays, non-finite X/Y, inconsistent layouts/tags, and malformed rings are structurally invalid.
- A LineString is empty or has at least two coordinates. A LinearRing is empty or closed with at least four coordinates and at least three distinct X/Y positions. Polygon stores one shell and zero or more holes.
- MultiPoint, MultiLineString, and MultiPolygon are homogeneous. GeometryCollection is heterogeneous, permits empty members, and forbids cyclic object graphs during validation, copying, and serialization.
- Triangle has exactly one closed four-position ring and no holes. TIN contains Triangles. PolyhedralSurface contains polygon patches. Curved geometries have explicit continuity, arc cardinality, closure, and linearization-tolerance rules.
- Planar operations use X/Y only. Z/M values at retained vertices survive; values at constructed points are interpolated from a unique source edge, reconciled when sources agree, otherwise set to NaN. No operation silently claims 3D topology.
- Structural equality, normalized structural equality, and DE-9IM topological equality are distinct APIs. Canonical normalization fixes ring start vertices and orientation, orders homogeneous parts, and preserves the declared layout and SRID.

### Robustness and precision

- PrecisionModel supports floating double, floating single, and fixed grid scale/offset. Precision reduction has pointwise and topology-preserving modes and reports collapsed components.
- Fast double calculations are protected by magnitude/error filters. Ambiguous orientation, determinant, and line-intersection cases fall back to expansion/double-double arithmetic implemented with built-in doubles.
- Segment noding supports monotone-chain envelope filtering, sweep-line/indexed candidate generation, exact intersection classification, proper/non-proper intersections, collinear overlap, repeated points, and snap rounding to a fixed grid.
- Overlay validates noding, labels topology edges, removes duplicate/cancelled edges, builds maximal/minimal rings, assigns holes, extracts mixed-dimension results, and normalizes the result. A robust retry path applies a derived safe precision grid before throwing TopologyException.
- Precision-specific overlay and buffer methods apply the caller's grid during noding, not as an unsafe coordinate-only post-rounding step.

### Standards behavior

- Default boundary semantics use the OGC mod-2 rule. Endpoint, multivalent-endpoint, and monovalent-endpoint boundary rules are selectable for Relate.
- DE-9IM exposes all nine interior/boundary/exterior intersection dimensions, canonical matrix text, transpose, named patterns, and arbitrary T/F/*/0/1/2 pattern matching.
- Named predicates are TopologicallyEquals, Disjoint, Intersects, Touches, Crosses, Within, Contains, Overlaps, Covers, CoveredBy, ContainsProperly, and WithinDistance. Envelope rejection is only an optimization and never changes semantics.
- Overlay covers Intersection, Union, Difference, SymmetricDifference, unary/cascaded union, homogeneous and heterogeneous outputs, empty results, and mixed dimensions. GeometryCollection inputs are recursively flattened and supported when their point-set semantics are defined.
- RFC 7946 parsing accepts the seven fixed geometry names, Feature, FeatureCollection, null feature geometry, string/number ids, arbitrary JSON properties, bbox, and foreign members. Parsed JsonElement values are cloned so they outlive JsonDocument.
- Strict GeoJSON writing uses CRS84 longitude/latitude order, no crs member, finite positions, at most XYZ, right-hand polygon rings, legal bbox shape, and no extended geometry types. Tolerant reading may accept wrong ring orientation but normalizes on strict output.
- Antimeridian-spanning bboxes preserve west greater than east. Explicit cutting utilities split lines and polygons at ±180 without pretending a planar envelope is a wrapped geographic bbox.
- WKT/WKB cover EMPTY and XY/XYZ/XYM/XYZM for every supported SFA type, both byte orders, nested collections, and exact finite numeric formatting. EWKT SRID and EWKB SRID flags are explicit optional flavors, never confused with strict OGC encodings.

## Complete feature inventory

### Model, construction, inspection, and editing

- Core and empty geometry values: Point, LineString, LinearRing, Polygon with holes, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection.
- SFA surface values: Triangle, TIN, PolyhedralSurface; surface boundary, closure, patch access, projected 2D area, and explicit 3D surface area.
- Curved interoperability values: CircularString, CompoundCurve, CurvePolygon, MultiCurve, MultiSurface; arc validation, envelope, length, area, curve-to-line, and tolerance-controlled participation in linear topology.
- Coordinate and metadata access: layout, topological dimension, coordinate dimension, SRID, emptiness, simplicity, closure, ring status, component/point/ring/patch counts, start/end/point-N, coordinate extraction, and unique coordinate extraction.
- Constructors/factories for every type, empty values, rectangles, envelopes, circles, regular polygons, line segments, triangles, and safe collection promotion to the narrowest homogeneous type.
- Deep clone, normalize/canonicalize, reverse, ring reorientation, affine translate/rotate/scale/skew, coordinate-layout conversion, SRID metadata replacement, grid snapping, repeated-point removal, flatten, explode, combine, extract-by-dimension/type, and collection assembly.
- Envelope operations: create/expand, contains, covers, intersects, disjoint, distance, intersection, union, center, width, height, area, and conversion to Polygon.

### I/O

- WKT reader/writer for strict OGC types, dimensional tokens, EMPTY, nested collections, invariant and round-trip numeric formatting, configurable trimming/precision, character spans, strings, TextReader/TextWriter, and detailed parse offsets.
- WKB reader/writer for little/big endian, all dimensions and nested types, byte arrays, ReadOnlySpan/Span, Stream, exact encoded-size calculation, hex conversion, strict length/count/recursion limits, and optional EWKB SRID flavor.
- GeoJSON UTF-8/string/stream reader/writer for Geometry, Feature, FeatureCollection, bbox, id, properties, null geometry, foreign members, strict/tolerant modes, output coordinate precision, and read limits.
- RFC 8142 GeoJSON text sequence reader/writer with record-separator framing, per-record failures, and bounded streaming.
- Format failure reporting includes error kind, message, byte/character offset, JSON path or geometry component path, and non-throwing Try variants.

### Low-level computational geometry

- Coordinate/vector arithmetic, dot/cross product, norms, angles, quadrants, interpolation, projection factors, signed ring area, and lexicographic comparison.
- Filtered robust determinant/orientation, robust line-line intersection, segment intersection classification, point-on-line/segment, collinear overlap, segment-envelope tests, and nearest points between point/segment pairs.
- Ray-crossing and winding point location for rings, polygons with holes, multipolygons, lines, and collections, with explicit Interior/Boundary/Exterior results.
- Monotone chains, envelope interval indexes, sweep-line candidate enumeration, segment strings, node maps, edge splitting, duplicate-edge merging, fully-noded validation, indexed noding, snapping, and snap rounding.
- Planar graph nodes, directed edges, labels, depth propagation, connected subgraphs, ring building, component extraction, and deterministic ordering.

### Predicates, relations, and metrics

- Unary checks: IsEmpty, IsClosed, IsRing, IsSimple with detailed non-simple location, IsValid, and rectangle detection.
- Exact/tolerance equality, canonical equality, and topological equality.
- Full DE-9IM Relate matrix, configurable boundary node rules, matrix transpose, arbitrary relation-pattern matching, and every named SFA predicate.
- Envelope, coordinate count, dimension, boundary, convex dimension, area, signed area, line length, polygon perimeter, and collection aggregation.
- Minimum planar distance, distance-within threshold, nearest point pair and component locations, indexed distance, Hausdorff distance, discrete Fréchet distance, and densified variants.
- Centroid for point/line/area/mixed geometries with highest-dimension weighting, representative point/point-on-surface, and minimum clearance with witness segment.

### Overlay, clipping, and combination

- Robust binary intersection, union, difference, and symmetric difference for all valid linear geometry combinations.
- Fixed-precision variants, robust fallback, unary union, cascaded polygon union, disjoint-subset union, dissolve, and collection normalization.
- Rectangle clipping with dimension-preserving results, general polygon clipping through intersection, line splitting by points/lines/polygon boundaries, and grid-cell subdivision.
- Geometry snap, self-node, mutual node, shared paths with direction classification, line merge with directed option, build-area, polygonize, and full polygonizer diagnostics for dangles, cut edges, and invalid rings.

### Constructive geometry

- Boundary, envelope polygon, centroid, representative point, convex hull, concave hull by ratio/maximum edge length, and polygon hull simplification.
- Buffer with round/flat/square caps; round/mitre/bevel joins; quadrant segments; mitre limit; negative/zero distance; single-sided lines; fixed precision; and empty/collapse handling.
- Offset curves, parallel curves, Minkowski-style point/line/area buffering, and buffer-result validation.
- Douglas-Peucker, topology-preserving, Visvalingam-Whyatt, and polygon-hull simplification; repeated-point cleanup and optional endpoint preservation.
- Densification by maximum segment length, densified topology input, line substring, and curve linearization.
- Minimum rotated rectangle, minimum-width strip, minimum bounding circle, minimum/maximum diameter, maximum inscribed circle, largest empty circle, and polygon tangents.
- Delaunay triangulation, constrained Delaunay triangulation, Voronoi diagram clipped to an envelope, polygon triangulation, and Euclidean minimum spanning tree.

### Validity, cleaning, and repair

- Structural validation for tag/payload/layout/SRID/finite coordinate consistency, recursion/cycle/size limits, minimum coordinate counts, and closed rings.
- OGC simplicity and validity diagnostics: repeated vertices where illegal, self-intersections, ring self-touch, holes outside shell, nested holes, disconnected interior, duplicate rings, overlapping polygon components, and non-node intersections.
- ValidationResult carries validity, ValidationError, message, coordinate, and component path without nullable value types.
- Make-valid linework and structure methods, optional collapsed-component retention, removal of non-finite coordinates and repeats, ring repair, hole/shell reassignment, polygonal union, and narrowest-type result selection.
- Explicit clean, normalize, orient, snap, precision-reduce, remove-small-rings, and remove-collapsed-components operations; no hidden repair during ordinary construction.

### Prepared geometry and spatial indexes

- Prepared Point/MultiPoint, LineString/MultiLineString, Polygon/MultiPolygon, and collection snapshots with cached envelope, representative points, segment indexes, and point-in-area locators.
- Prepared Intersects, Disjoint, Contains, ContainsProperly, Covers, CoveredBy, Within, Crosses, Overlaps, Touches, Relate, RelatePattern, Distance, WithinDistance, and NearestPoints with identical results to unprepared operations.
- Immutable packed STRtree with configurable node capacity, deterministic Sort-Tile-Recursive packing, envelope query, predicate-filtered query, iteration, nearest and k-nearest neighbor, within-distance query, removal before/after build policy, and empty-tree behavior.
- Dynamic R-tree with insert, remove, update, envelope query, nearest/k-nearest, quadratic split, choose-subtree, condense/reinsert, root collapse, duplicate ids, and snapshot query results.
- Index entries hold an integer id, Envelope, optional Geometry, and optional object payload; geometry query refinement is explicit rather than implied by envelope hits.

### Linear referencing, coverages, and geographic helpers

- Project point to line, interpolate by absolute/normalized length, extract point/location, extract line substring, locate endpoints, offset located points, clamp invalid locations, and operate across MultiLineString components.
- M-coordinate helpers: interpolate M, locate by M, extract M interval, validate monotonic measure, and preserve M through line splitting/merging where unambiguous.
- Polygonal coverage validation for overlaps, non-noded edges, and gaps up to a supplied width; coverage boundary/edge extraction, union, topology-preserving Visvalingam simplification, cleaning, and gap/overlap merge policy.
- WGS84 angular/radian/linear unit conversion, haversine distance, initial/final bearing, destination, midpoint, great-circle line, rhumb distance/bearing/destination, geographic line length, spherical polygon area, along/slice, nearest point on geographic line, and circle generation.
- RFC 7946 antimeridian cutting for lines/polygons, wrapped bbox computation, ring rewind, coordinate truncation, Feature/FeatureCollection bbox, flatten, explode, combine, and spatial join helpers for points-within-polygons and feature intersection.

### Explicit non-goals

- No CRS database, datum conversion, projection engine, geocoding, routing/network analysis, raster/coverage grid interpolation, contour/isoband generation, clustering/statistics, or random feature generation.
- No arbitrary user-defined geometry type, generic public geometry coordinate type, mutable callback/filter API, database provider, shapefile/GML/KML/GeoPackage codec, or rendering subsystem.
- No claim that planar buffer/area/distance on longitude/latitude is geodesic; callers must select the geographic service explicitly.

## Proposed public API and source-file manifest

Names below are proposed at method-family level. Final signatures must remain positional, non-overloaded, and compliant with syntax.md.

### Public data classes: src/Api/Data

| Planned files | Purpose |
| --- | --- |
| Coordinate.cs, Envelope.cs, PrecisionModel.cs, AffineTransform.cs | Coordinate ordinates, bounds, numeric grid policy, and affine matrix transfer values. |
| Geometry.cs | Tagged union with metadata and one payload selected by GeometryType. |
| Point.cs, LineString.cs, LinearRing.cs, Polygon.cs | Atomic linear geometry payloads. |
| MultiPoint.cs, MultiLineString.cs, MultiPolygon.cs, GeometryCollection.cs | Homogeneous and heterogeneous collection payloads. |
| Triangle.cs, Tin.cs, PolyhedralSurface.cs | SFA surface payloads. |
| CircularString.cs, CompoundCurve.cs, CurvePolygon.cs, MultiCurve.cs, MultiSurface.cs | Curved interoperability payloads. |
| GeoJsonDocument.cs, GeoJsonGeometry.cs, GeoJsonFeature.cs, GeoJsonFeatureCollection.cs | Full RFC 7946 root/object model with raw cloned JSON values and foreign members. |
| GeometryFactoryOptions.cs, BufferParameters.cs, SimplifyParameters.cs, RepairParameters.cs, CurveLinearizationParameters.cs | Construction and operation options without default parameters. |
| WktReadOptions.cs, WktWriteOptions.cs, WkbReadOptions.cs, WkbWriteOptions.cs, GeoJsonReadOptions.cs, GeoJsonWriteOptions.cs | Explicit codec limits, strictness, flavor, layout, and formatting. |
| GeometryReadResult.cs, GeoJsonReadResult.cs, WriteResult.cs, ParseError.cs | Try-style outcomes and position-aware failures. |
| RelationMatrix.cs, DistanceResult.cs, SegmentIntersectionResult.cs, ValidationResult.cs, CoverageValidationResult.cs | Relation, witness, low-level intersection, and diagnostic outcomes. |
| LinearLocation.cs, SpatialIndexItem.cs, SpatialIndexQuery.cs, CircleResult.cs | Linear referencing, fixed-type index entries/queries, and circle construction details. |

Every options/result/data class is used by at least one public interface or another public data class, and every data class contains at least one public instance field.

### Public enums: src/Api/Enums

CoordinateLayout.cs, GeometryType.cs, TopologicalDimension.cs, PrecisionModelType.cs, RingOrientation.cs, ByteOrder.cs, WkbFlavor.cs, BoundaryNodeRule.cs, TopologicalLocation.cs, RelationDimension.cs, SegmentIntersectionType.cs, BufferCapStyle.cs, BufferJoinStyle.cs, SimplificationAlgorithm.cs, RepairMethod.cs, ValidationError.cs, GeoJsonObjectType.cs, ParseErrorKind.cs, SpatialUnit.cs, CurveLinearizationMode.cs, and CoverageMergeStrategy.cs.

Each enum has explicit Int32 member values and is consumed by a public data class or interface.

### Public static data: src/Api/Static

| Planned file | Fields only |
| --- | --- |
| GeometryConstants.cs | Unknown SRID, CRS84 SRID convention, no-data ordinate, default limits, and numeric tolerances. |
| De9ImPatterns.cs | Named OGC relation pattern strings. |
| WkbTypeCodes.cs | OGC base and dimensional type-code constants plus explicit EWKB flags. |
| GeoJsonNames.cs | Media types and fixed member/type-name strings. |

### Public exceptions: src/Api/Exceptions

GeometryException.cs, GeometryParseException.cs, InvalidGeometryException.cs, TopologyException.cs, PrecisionException.cs, GeoJsonException.cs, and SpatialIndexException.cs. Each derives directly from System.Exception and has at most one constructor.

### Public operation contracts: src/Api/Contracts

| Planned interface | Proposed instance API surface, without overload detail |
| --- | --- |
| IGeometryFactory.cs | GetFactoryOptions, CreateEmptyGeometry, CreatePoint, CreateLineString, CreatePolygon, CreateMultiPoint, CreateMultiLineString, CreateMultiPolygon, CreateGeometryCollection, CreateTriangle, CreateTin, CreatePolyhedralSurface, CreateCircularString, CreateCompoundCurve, CreateCurvePolygon, CreateMultiCurve, CreateMultiSurface, CreateRectangle, and CreateCircle. |
| IGeometryInspector.cs | GetGeometryType, GetTopologicalDimension, GetCoordinateLayout, GetSpatialReferenceId, IsEmpty, IsClosed, IsRing, IsSimple, IsRectangle, CountCoordinates, CountComponents, GetPointAt, GetStartPoint, GetEndPoint, GetComponentAt, GetBoundary, GetEnvelope, ExtractCoordinates, and ExtractUniqueCoordinates. |
| IGeometryEditor.cs | CloneGeometry, Normalize, Reverse, OrientRings, ApplyAffineTransform, ReplaceSpatialReferenceId, ConvertCoordinateLayout, SnapToGrid, RemoveRepeatedPoints, Flatten, Combine, ExtractByType, and ExtractByDimension. |
| ICoordinateAlgorithms.cs | CompareCoordinates, InterpolateCoordinate, OrientationIndex, LocatePointOnSegment, IntersectSegments, NearestSegmentPoints, SignedRingArea, RingOrientationOf, DotProduct, CrossProduct, and AngleBetween. |
| IWktCodec.cs | ReadWkt, TryReadWkt, ReadWktCharacters, WriteWkt, TryWriteWktCharacters, and WriteWktToTextWriter. |
| IWkbCodec.cs | ReadWkb, TryReadWkb, ReadWkbStream, ReadWkbHex, GetWkbSize, WriteWkb, TryWriteWkbBytes, WriteWkbStream, and WriteWkbHex. |
| IGeoJsonCodec.cs | ReadGeoJson, TryReadGeoJson, ReadGeoJsonUtf8, ReadGeoJsonStream, WriteGeoJson, TryWriteGeoJsonUtf8, and WriteGeoJsonStream. |
| IGeoJsonSequenceCodec.cs | ReadGeoJsonSequence, WriteGeoJsonSequence, and TryReadGeoJsonRecord. |
| IGeometryPredicates.cs | ExactlyEquals, ToleranceEquals, NormalizedEquals, TopologicallyEquals, Disjoint, Intersects, Touches, Crosses, Within, Contains, Overlaps, Covers, CoveredBy, ContainsProperly, and WithinDistance. |
| IGeometryRelations.cs | Relate, RelateWithBoundaryRule, MatchRelationPattern, TransposeRelation, and LocatePoint. |
| IGeometryMeasures.cs | Area, SurfaceArea3D, Length, Perimeter, Distance, IndexedDistance, NearestPoints, HausdorffDistance, FrechetDistance, Centroid, PointOnSurface, MinimumClearance, and EnvelopeDistance. |
| IGeometryOverlay.cs | Intersection, IntersectionAtPrecision, Union, UnionAtPrecision, Difference, DifferenceAtPrecision, SymmetricDifference, SymmetricDifferenceAtPrecision, UnaryUnion, CascadedUnion, DisjointSubsetUnion, Dissolve, ClipToEnvelope, Split, Snap, Node, and SharedPaths. |
| IGeometryConstructive.cs | ConvexHull, ConcaveHull, Buffer, SingleSidedBuffer, OffsetCurve, MinimumRotatedRectangle, MinimumBoundingCircle, MinimumWidth, MaximumInscribedCircle, LargestEmptyCircle, PolygonTangents, Delaunay, ConstrainedDelaunay, Voronoi, TriangulatePolygon, and MinimumSpanningTree. |
| IGeometrySimplifier.cs | Simplify, Densify, SimplifyPolygonHull, and LinearizeCurves. |
| IGeometryAssembler.cs | LineMerge, DirectedLineMerge, Polygonize, PolygonizeFull, BuildArea, and ExtractCoverageEdges. |
| IGeometryValidator.cs | ValidateStructure, ValidateGeometry, CheckSimplicity, ValidateCoverage, and ValidateBufferResult. |
| IGeometryRepair.cs | MakeValid, CleanCoordinates, RepairRings, ReducePrecision, RemoveCollapsedComponents, and CleanCoverage. |
| IPreparedGeometry.cs | GetGeometrySnapshot, all named binary predicates, RelatePrepared, MatchPreparedRelation, DistancePrepared, WithinPreparedDistance, and NearestPreparedPoints. |
| ISpatialIndex.cs | BuildIndex, InsertItem, RemoveItem, UpdateItem, QueryEnvelope, QueryGeometry, QueryNearest, QueryKNearest, QueryWithinDistance, EnumerateItems, CountItems, and ClearIndex. |
| ILinearReferencer.cs | ProjectLocation, InterpolateLocation, ExtractPoint, ExtractOffsetPoint, ExtractLine, LocateMeasure, ExtractMeasureInterval, and ValidateMonotonicMeasure. |
| ICoverageOperations.cs | ValidatePolygonCoverage, UnionPolygonCoverage, SimplifyPolygonCoverage, CleanPolygonCoverage, and ExtractPolygonCoverageEdges. |
| IGeographicOperations.cs | ConvertUnits, HaversineDistance, InitialBearing, FinalBearing, Destination, GeographicMidpoint, GreatCircle, RhumbDistance, RhumbBearing, RhumbDestination, GeographicLength, SphericalArea, AlongGeographicLine, SliceGeographicLine, NearestOnGeographicLine, and GeographicCircle. |
| IGeoJsonOperations.cs | ComputeGeoJsonBoundingBox, CutAntimeridian, RewindGeoJson, TruncateGeoJsonCoordinates, FlattenFeatures, ExplodeFeatures, CombineFeatures, PointsWithinPolygons, and IntersectingFeatures. |

### Public implementation classes: src/Api/Classes

GeometryFactory.cs, GeometryInspector.cs, GeometryEditor.cs, CoordinateAlgorithms.cs, WktCodec.cs, WkbCodec.cs, GeoJsonCodec.cs, GeoJsonSequenceCodec.cs, GeometryPredicates.cs, GeometryRelations.cs, GeometryMeasures.cs, GeometryOverlay.cs, GeometryConstructive.cs, GeometrySimplifier.cs, GeometryAssembler.cs, GeometryValidator.cs, GeometryRepair.cs, PreparedGeometry.cs, StrTreeIndex.cs, RTreeIndex.cs, LinearReferencer.cs, CoverageOperations.cs, GeographicOperations.cs, and GeoJsonOperations.cs.

Each class implements only its corresponding interface except StrTreeIndex and RTreeIndex, which separately implement ISpatialIndex. Each has one public constructor that receives all required collaborators/configuration; method names are never overloaded.

### Planned internal source files outside src/Api

| Directory | Planned files and responsibility |
| --- | --- |
| src/Kernel/Model | GeometryDispatch.cs, GeometryCopy.cs, GeometryBuilder.cs, GeometryCanonicalizer.cs, CoordinateSequences.cs, ComponentWalker.cs, GeometryCycleGuard.cs, and CurveLinearizer.cs. |
| src/Kernel/Numerics | FloatingPointFilter.cs, ExpansionArithmetic.cs, DoubleDouble.cs, RobustDeterminant.cs, RobustOrientation.cs, RobustLineIntersection.cs, and PrecisionGrid.cs. |
| src/Kernel/Algorithms | VectorMath.cs, RingMath.cs, SegmentMath.cs, PointLocator.cs, CentroidAccumulator.cs, DistanceAlgorithm.cs, HausdorffAlgorithm.cs, FrechetAlgorithm.cs, ConvexHullAlgorithm.cs, ConcaveHullAlgorithm.cs, MinimumShapes.cs, and GeographicMath.cs. |
| src/Kernel/Noding | SegmentString.cs, SegmentNode.cs, SegmentNodeList.cs, MonotoneChain.cs, SweepLineIndex.cs, IndexedNoder.cs, SnapNoder.cs, SnapRounder.cs, EdgeNodingValidator.cs, and NodedSegmentMerger.cs. |
| src/Kernel/Topology | TopologyLabel.cs, TopologyNode.cs, TopologyEdge.cs, DirectedTopologyEdge.cs, PlanarGraph.cs, BoundaryComputer.cs, RelationComputer.cs, RelationMatrixBuilder.cs, ConnectedSubgraph.cs, RingBuilder.cs, and PolygonBuilder.cs. |
| src/Kernel/Overlay | OverlayEngine.cs, OverlayResultExtractor.cs, OverlayPrecisionEngine.cs, OverlayRobustness.cs, UnaryUnionEngine.cs, CascadedUnionEngine.cs, RectangleClipper.cs, GeometrySplitter.cs, GeometrySnapper.cs, and SharedPathFinder.cs. |
| src/Kernel/Buffer | BufferCurveBuilder.cs, OffsetSegmentGenerator.cs, OffsetCurveSetBuilder.cs, BufferSubgraph.cs, BufferBuilder.cs, and BufferValidator.cs. |
| src/Kernel/Construct | LineMerger.cs, Polygonizer.cs, AreaBuilder.cs, DelaunayBuilder.cs, ConstrainedDelaunayBuilder.cs, VoronoiBuilder.cs, PolygonTriangulator.cs, MinimumSpanningTreeBuilder.cs, and CircleBuilder.cs. |
| src/Kernel/Simplify | DouglasPeucker.cs, TopologyPreservingSimplifier.cs, VisvalingamWhyatt.cs, PolygonHullSimplifier.cs, and Densifier.cs. |
| src/Kernel/Validation | StructuralValidator.cs, SimplicityChecker.cs, GeometryValidityChecker.cs, PolygonValidityChecker.cs, SurfaceValidityChecker.cs, CoverageValidator.cs, GeometryFixer.cs, RingRepairer.cs, and CoverageCleaner.cs. |
| src/Kernel/Index | IntervalList.cs, PackedNode.cs, StrTreeBuilder.cs, RTreeNode.cs, RTreeBuilder.cs, RTreeSplitter.cs, BranchAndBoundNearest.cs, and SegmentSpatialIndex.cs. |
| src/Kernel/Prepared | PreparedPointData.cs, PreparedLineData.cs, PreparedPolygonData.cs, PreparedCollectionData.cs, and PreparedPredicateEvaluator.cs. |
| src/Kernel/LinearRef | LinearLocationMath.cs, LengthIndex.cs, MeasureIndex.cs, and LineSubstringBuilder.cs. |
| src/Io/Wkt | WktTokenizer.cs, WktParser.cs, WktFormatter.cs, and WktNumber.cs. |
| src/Io/Wkb | EndianReader.cs, EndianWriter.cs, WkbParser.cs, WkbFormatter.cs, and WkbSizer.cs. |
| src/Io/GeoJson | GeoJsonParser.cs, GeoJsonFormatter.cs, GeoJsonValidator.cs, GeoJsonBoundingBox.cs, GeoJsonSequenceReader.cs, GeoJsonSequenceWriter.cs, and AntimeridianCutter.cs. |

## Test strategy and planned test files

Functional completion means every public type, public field contract, public method, option branch, enum value, constants class, documented exception path, empty geometry case, geometry-type combination, coordinate layout, and supported serialization flavor has an automated test. Tests must not merely execute methods: they assert exact results, normalized equivalence, invariants, or independent reference outcomes.

- tests/Api contains GeometryFactoryTests.cs, GeometryInspectorTests.cs, GeometryEditorTests.cs, CoordinateAlgorithmsTests.cs, GeometryPredicatesTests.cs, GeometryRelationsTests.cs, GeometryMeasuresTests.cs, GeometryOverlayTests.cs, GeometryConstructiveTests.cs, GeometrySimplifierTests.cs, GeometryAssemblerTests.cs, GeometryValidatorTests.cs, GeometryRepairTests.cs, PreparedGeometryTests.cs, SpatialIndexContractTests.cs, LinearReferencerTests.cs, CoverageOperationsTests.cs, GeographicOperationsTests.cs, and GeoJsonOperationsTests.cs.
- tests/Io contains WktCodecTests.cs, WkbCodecTests.cs, GeoJsonCodecTests.cs, GeoJsonSequenceCodecTests.cs, MalformedInputTests.cs, SpanAndStreamIoTests.cs, and RoundTripTests.cs.
- tests/Standards contains OgcSfaConformanceTests.cs, De9ImConformanceTests.cs, GeoJsonRfc7946Tests.cs, GeoJsonAntimeridianTests.cs, and GeoJsonTextSequenceTests.cs.
- tests/Robustness contains RobustOrientationTests.cs, SegmentIntersectionTests.cs, NodingTests.cs, PrecisionModelTests.cs, TopologyCollapseTests.cs, ExtremeMagnitudeTests.cs, and DeterminismTests.cs.
- tests/Indexes contains StrTreeIndexTests.cs, RTreeIndexTests.cs, IndexNearestTests.cs, IndexMutationTests.cs, and IndexConcurrencySemanticsTests.cs; these assert correctness and lifecycle only, never timings.
- tests/Differential contains NetTopologySuitePredicateTests.cs, NetTopologySuiteOverlayTests.cs, NetTopologySuiteBufferTests.cs, NetTopologySuiteIoTests.cs, and RandomizedInvariantTests.cs. Fixed seeds and minimized saved regressions make failures reproducible.
- tests/Fixtures contains OGC WKT/WKB examples, RFC 7946 JSON examples, RFC 8142 sequences, DE-9IM matrices, invalid geometry cases, topology-collapse regressions, and tests-only upstream-compatible XML/JSON cases with recorded provenance.
- Property/invariant coverage includes round-trip preservation, relation transpose/inverse identities, predicate duals, intersection commutativity, union/symmetric-difference commutativity, difference identities, area partition bounds, buffer containment, normalization idempotence, repair validity, prepared/unprepared equality, indexed/brute-force result equality, and deterministic output under component permutation.

## Implementation tasks

### Foundation and public model

- [DONE] T001: Establish the Geo namespace, create the required Api and internal directory structure, and keep src/geo.csproj limited to built-in .NET references.
- [DONE] T002: Implement all public enum files with explicit Int32 values and connect every enum to a public data or contract use.
- [DONE] T003: Implement GeometryConstants, De9ImPatterns, WkbTypeCodes, and GeoJsonNames as field-only public static data classes.
- [DONE] T004: Implement Coordinate, Envelope, PrecisionModel, and AffineTransform field-based data classes with XY/XYZ/XYM/XYZM and unknown-SRID conventions.
- [DONE] T005: Implement Point, LineString, LinearRing, Polygon, MultiPoint, MultiLineString, MultiPolygon, and GeometryCollection payload data classes.
- [DONE] T006: Implement Geometry as a non-inheriting tagged union with metadata and nullable payload fields for every planned geometry kind.
- [DONE] T007: Implement Triangle, Tin, PolyhedralSurface, CircularString, CompoundCurve, CurvePolygon, MultiCurve, and MultiSurface payload data classes.
- [DONE] T008: Implement GeoJsonDocument, GeoJsonGeometry, GeoJsonFeature, and GeoJsonFeatureCollection with cloned JsonElement values, bbox arrays, and foreign-member maps.
- [DONE] T009: Implement operation parameter, codec option, Try-result, relation, distance, validation, linear-location, index-item, query, and circle-result data classes from the manifest.
- [DONE] T010: Implement all custom exceptions as direct System.Exception subclasses with one constructor each and no exception inheritance chain.
- [DONE] T011: Implement internal component walking, tag dispatch, recursion/cycle guarding, coordinate sequence access, and deep-copy primitives.
- [DONE] T012: Implement IGeometryFactory and GeometryFactory for all empty and core linear geometry constructors with precision application and structural precondition failures.
- [DONE] T013: Implement GeometryFactory constructors for rectangle, envelope polygon, circle, regular polygon, segment, and narrowest valid homogeneous collection promotion.
- [DONE] T014: Add exhaustive factory/model tests for every data payload, geometry tag, coordinate layout, SRID, empty form, null/mismatch failure, and caller-input snapshot rule.

### Inspection, envelopes, normalization, and editing

- [DONE] T015: Implement Envelope creation, expansion, containment, coverage, intersection, union, distance, center, dimensions, area, empty behavior, and polygon conversion in internal algorithms.
- [DONE] T016: Implement IGeometryInspector and GeometryInspector metadata, dimension, count, point/component access, start/end, coordinate extraction, envelope, emptiness, closure, ring, and rectangle operations.
- [DONE] T017: Implement OGC boundary construction for every core linear geometry and each selectable boundary-node rule needed by relation processing.
- [DONE] T018: Implement deterministic deep clone and canonical normalization including coordinate ordering, ring rotation/orientation, component ordering, and narrowest result typing.
- [DONE] T019: Implement reverse, ring orientation correction, affine transformation, layout conversion, SRID metadata replacement, grid snap, repeated-point removal, flatten, combine, and extraction operations.
- [DONE] T020: Implement IGeometryEditor and GeometryEditor over the internal copy/canonical/edit algorithms without mutating caller-owned data.
- [DONE] T021: Add inspector, envelope, boundary, normalization, editor, recursion-cycle, idempotence, and mutation-isolation tests for every geometry type and empty case.

### WKT, WKB, and GeoJSON I/O

- [DONE] T022: Implement a bounded WKT tokenizer with invariant numeric parsing, dimensional tokens, keywords, nesting depth, character offsets, and actionable parse errors.
- [DONE] T023: Implement strict WKT parsing for EMPTY and all core linear SFA geometries in XY, XYZ, XYM, and XYZM, including nested GeometryCollection layout checks.
- [DONE] T024: Extend WKT parsing to Triangle, TIN, PolyhedralSurface, and planned curved geometry tokens while rejecting reserved/unknown types deterministically.
- [DONE] T025: Implement canonical WKT formatting with round-trip doubles, trim and decimal precision controls, dimensional tokens, empty values, spans, strings, and TextWriter output.
- [DONE] T026: Implement IWktCodec and WktCodec throwing and Try APIs with no overloaded member names.
- [DONE] T027: Implement endian-safe bounded WKB primitive readers and writers over ReadOnlySpan, Span, byte arrays, and Stream without unsafe code.
- [DONE] T028: Implement strict OGC WKB parsing for both byte orders, all layouts, core types, counts, nested collections, exact consumption, depth limits, and malformed/truncated input.
- [DONE] T029: Extend WKB parsing and sizing to Triangle, TIN, PolyhedralSurface, and planned curved types using OGC/ISO dimensional type codes.
- [DONE] T030: Implement WKB sizing and writing for both byte orders, all layouts, strict OGC flavor, explicit EWKB-SRID flavor, hex encoding, spans, arrays, and streams.
- [DONE] T031: Implement IWkbCodec and WkbCodec throwing and Try APIs, including required-size reporting when a destination Span is too small.
- [DONE] T032: Implement bounded System.Text.Json parsing for every RFC 7946 Geometry object with strict/tolerant ring orientation, finite positions, legal nesting, and foreign-member preservation.
- [DONE] T033: Implement RFC 7946 Feature and FeatureCollection parsing with null geometry, string/number id, arbitrary properties, bbox, foreign members, and cloned JsonElement lifetimes.
- [DONE] T034: Implement strict GeoJSON formatting with CRS84 rules, right-hand rings, no crs member, legal finite XY/XYZ positions, configurable output precision, bbox, properties, and foreign members.
- [DONE] T035: Implement IGeoJsonCodec and GeoJsonCodec string, UTF-8 Span, Stream, throwing, and Try forms with detailed JSON path and byte-offset failures.
- [DONE] T036: Implement RFC 8142 record-separator sequence reading/writing, bounded records, recovery/reporting for malformed records, and IGeoJsonSequenceCodec.
- [DONE] T037: Add complete WKT tests across tokens, whitespace, case, empty forms, dimensions, numeric extremes, malformed offsets, all types, options, and canonical round trips.
- [DONE] T038: Add complete WKB tests across endian/layout/type combinations, exact bytes, hex, spans, streams, EWKB flavor, truncation, malicious counts/depth, and round trips.
- [DONE] T039: Add RFC 7946 and RFC 8142 tests for every root/type, nulls, properties, ids, bbox dimensions/wrapping, foreign members, ring rules, strict failures, tolerant reads, streams, and sequences.

### Robust numeric and low-level algorithms

- [DONE] T040: Implement vector arithmetic, dot/cross products, norm scaling, quadrants, interpolation, projection factors, angles, coordinate ordering, and signed ring area.
- [DONE] T041: Implement floating-point error filters and expansion/double-double arithmetic for robust determinant signs without decimal, unsafe code, or external numerics.
- [DONE] T042: Implement filtered robust OrientationIndex with exact fallback and stable handling of coincident, near-collinear, extreme-magnitude, and non-finite inputs.
- [DONE] T043: Implement robust infinite-line and segment intersection with proper, endpoint, collinear-point, collinear-overlap, disjoint, and witness-coordinate results.
- [DONE] T044: Implement point-on-line/segment, segment-envelope rejection, and nearest point/witness calculations for point-point, point-segment, and segment-segment cases.
- [DONE] T045: Implement robust ring orientation and point location using ray crossing/winding with explicit boundary classification.
- [DONE] T046: Implement point location for Point, lineal, polygonal, multi, mixed collection, surface, and curve-linearized inputs.
- [DONE] T047: Implement ICoordinateAlgorithms and CoordinateAlgorithms over the low-level kernel.
- [DONE] T048: Add exhaustive coordinate, orientation, determinant, ring, point-location, and segment-intersection tests including adversarial floating-point regressions.

### Noding and topology graph

- [DONE] T049: Implement segment strings, segment nodes, sorted node lists, intersection insertion, edge splitting, duplicate removal, and source metadata carriage.
- [DONE] T050: Implement monotone chains and an envelope interval/sweep-line index for deterministic candidate segment pair enumeration.
- [DONE] T051: Implement indexed mutual/self noding with proper intersection detection, collinear overlap splitting, and optional validation-only mode.
- [DONE] T052: Implement vertex snapping, mutual geometry snapping, fixed-grid hot pixels, snap rounding, collapsed-edge cleanup, and fully-noded output validation.
- [DONE] T053: Implement topology labels, nodes, edges, directed edges, stars, planar graph linking, depth propagation, connected subgraphs, maximal/minimal rings, and polygon building.
- [DONE] T054: Implement deterministic merged-edge labels and X/Y node keying while propagating Z/M values according to the ordinate reconciliation contract.
- [DONE] T055: Add noding, snap-rounding, planar-graph, ring-building, duplicate-edge, Z/M propagation, and deterministic-order tests.

### Predicates and DE-9IM

- [DONE] T056: Implement RelationMatrix storage validation, canonical text, transpose, cell updates, and T/F/*/0/1/2 pattern evaluation.
- [DONE] T057: Implement DE-9IM relation graph labeling for all Point/lineal/polygonal dimension pairs and empty geometries.
- [DONE] T058: Extend Relate to homogeneous and heterogeneous collections with mod-2, endpoint, monovalent-endpoint, and multivalent-endpoint rules.
- [DONE] T059: Implement IGeometryRelations and GeometryRelations for Relate, boundary-rule Relate, pattern matching, transpose, and point location.
- [DONE] T060: Implement exact, tolerance, normalized, and topological equality without hiding System.Object member names.
- [DONE] T061: Implement Disjoint, Intersects, Touches, Crosses, Within, Contains, Overlaps, Covers, CoveredBy, ContainsProperly, and WithinDistance from DE-9IM/metric semantics.
- [DONE] T062: Implement IGeometryPredicates and GeometryPredicates with safe envelope short-circuits and complete empty/collection semantics.
- [DONE] T063: Add OGC DE-9IM conformance tests for every dimension pair, named predicate pattern, converse/transpose identity, boundary rule, empty value, and collection case.
- [DONE] T064: Add predicate equality, tolerance, invalid-input, SRID-mismatch, and randomized differential tests against NetTopologySuite where its behavior matches the standards.

### Measures and distance

- [DONE] T065: Implement coordinate count, projected area/signed area, length, perimeter, and collection aggregation with numerically stable sums.
- [DONE] T066: Implement dimension-aware centroid accumulation for point, line, area, mixed, empty, surface, and curve-linearized inputs.
- [DONE] T067: Implement representative point/point-on-surface for lineal and polygonal geometries with a guaranteed non-empty result on a non-empty valid input.
- [DONE] T068: Implement brute-force and segment-indexed minimum distance with nearest coordinates and component/segment witness locations.
- [DONE] T069: Implement early-exit distance-within, envelope distance, and prepared reusable distance facets.
- [DONE] T070: Implement discrete Hausdorff and Fréchet distance with optional segment densification and witness points.
- [DONE] T071: Implement minimum clearance and the witness segment that realizes the robustness measure.
- [DONE] T072: Implement IGeometryMeasures and GeometryMeasures for all metric and witness result operations.
- [DONE] T073: Add exact fixture, empty, degenerate, mixed-dimension, holes, multiparts, Z/M, extreme magnitude, indexed/brute-force equivalence, and NTS differential metric tests.

### Overlay, clipping, and combination

- [DONE] T074: Implement the topology overlay engine for Intersection, Union, Difference, and SymmetricDifference using robust noding, edge labels, graph selection, and mixed-dimension extraction.
- [DONE] T075: Implement canonical narrowest-type overlay results for all core geometry pairings, empties, collections, touching-only cases, and dimensional collapses.
- [DONE] T076: Implement fixed-precision overlay with snap rounding and a deterministic robust fallback precision strategy that preserves caller SRID/layout metadata.
- [DONE] T077: Implement unary union, cascaded polygon union, disjoint-subset union, dissolve, and duplicate component elimination.
- [DONE] T078: Implement dimension-preserving rectangle clipping, general clipping through intersection, and grid subdivision without polygon-only assumptions.
- [DONE] T079: Implement geometry splitting by points, lines, and polygon boundaries with source-order preservation and complete no-split behavior.
- [DONE] T080: Implement public snap, self/mutual node, and shared-path operations with forward/reverse path classification.
- [DONE] T081: Implement IGeometryOverlay and GeometryOverlay with ordinary and explicitly named precision methods and consistent topology exceptions.
- [DONE] T082: Add complete overlay tests for every type pair, commutativity where applicable, identities, empties, holes, collections, collapses, precision grids, SRIDs, Z/M carriage, and known robustness regressions.
- [DONE] T083: Add clipping, splitting, snapping, noding, shared-path, unary/cascaded/disjoint union, and differential NTS overlay tests with fixed randomized seeds.

### Buffer and constructive operations

- [DONE] T084: Implement offset segment generation for round/mitre/bevel joins, round/flat/square caps, mitre limits, quadrant resolution, precision grids, and robust fillet intersections.
- [DONE] T085: Implement point, line, polygon, collection, negative, zero, and single-sided buffer curve generation and noded polygon construction.
- [DONE] T086: Implement buffer validation for distance containment, interior/exterior checks, and invalid result diagnosis.
- [DONE] T087: Implement offset curve extraction and deterministic handling of loops, joined/disjoint curves, and collapsed offsets.
- [DONE] T088: Implement monotone-chain convex hull with duplicate/collinear handling and narrowest Point/LineString/Polygon output.
- [DONE] T089: Implement concave hull by target ratio and maximum edge length, with optional holes and connected valid polygon output.
- [DONE] T090: Implement minimum rotated rectangle, minimum width/diameter, maximum diameter, and minimum bounding circle with witness data.
- [DONE] T091: Implement maximum inscribed circle, largest empty circle with obstacle geometries, and tangents from a point to a polygon.
- [DONE] T092: Implement Delaunay, constrained Delaunay, clipped Voronoi, polygon triangulation, and Euclidean minimum spanning tree using the robust orientation kernel.
- [DONE] T093: Implement IGeometryConstructive and GeometryConstructive across hull, buffer, offset, minimum-shape, circle, triangulation, Voronoi, tangent, and tree operations.
- [DONE] T094: Add exhaustive buffer/offset tests for every cap/join/precision/distance mode, invalid options, holes, collections, collapse, invariants, and NTS differential cases.
- [DONE] T095: Add hull, minimum-shape, circle, tangent, triangulation, Voronoi, and spanning-tree tests for degeneracy, validity, containment, witnesses, and deterministic output.

### Simplification, densification, line merge, and polygonization

- [DONE] T096: Implement Douglas-Peucker simplification for lines, rings, polygons, and collections with endpoint and minimum-ring safeguards.
- [DONE] T097: Implement topology-preserving simplification using indexed tagged lines and rejection of invalid new intersections.
- [DONE] T098: Implement Visvalingam-Whyatt simplification and polygon hull inner/outer simplification with validity preservation.
- [DONE] T099: Implement maximum-segment-length densification with precision application and source-edge Z/M interpolation.
- [DONE] T100: Implement line merge and directed line merge over a planar edge graph with isolated and loop components.
- [DONE] T101: Implement polygonization and build-area with dangle, cut-edge, invalid-ring, hole/shell, and full diagnostic outputs.
- [DONE] T102: Implement IGeometrySimplifier, GeometrySimplifier, IGeometryAssembler, and GeometryAssembler.
- [DONE] T103: Add simplification/densification tests for all algorithms, tolerances, endpoints, rings, holes, topology, Z/M, idempotence, and invalid parameter failures.
- [DONE] T104: Add line-merge, directed-merge, polygonize, diagnostics, and build-area tests covering branches, loops, duplicates, dangles, cut edges, and nested rings.

### Validation, precision reduction, and repair

- [DONE] T105: Implement structural validation for tags, payloads, layouts, finite X/Y, SRID consistency, cycles, nesting/count limits, coordinate counts, closure, and curved arc cardinality.
- [DONE] T106: Implement simplicity checks for lineal and multipoint geometries with detailed non-simple location and boundary-rule behavior.
- [DONE] T107: Implement OGC ring/polygon validity for self-intersection, self-touch, holes outside shell, nested holes, disconnected interiors, duplicates, and non-noded intersections.
- [DONE] T108: Implement MultiPolygon, GeometryCollection, Triangle, TIN, PolyhedralSurface, and curved geometry validity and closure rules.
- [DONE] T109: Implement topology-preserving precision reduction with collapse reporting and valid polygon reconstruction.
- [DONE] T110: Implement make-valid linework and structure strategies, collapsed-result policy, non-finite/repeated coordinate cleanup, ring repair, shell/hole reassignment, and narrowest output typing.
- [DONE] T111: Implement IGeometryValidator, GeometryValidator, IGeometryRepair, and GeometryRepair with detailed ValidationResult and expected Try-style behavior.
- [DONE] T112: Add exhaustive structural, simplicity, validity error-code/location/path, precision-collapse, and adversarial invalid geometry tests.
- [DONE] T113: Add repair tests for every invalidity class, both methods, collapse policies, preservation expectations, valid output, empty results, and NTS GeometryFixer differential cases.

### Prepared geometries and indexes

- [DONE] T114: Implement immutable prepared point, line, polygon, and collection snapshots with cached envelopes, segment indexes, representative points, and point-in-area locators.
- [DONE] T115: Implement all IPreparedGeometry predicates, Relate/pattern, distance/within-distance, and nearest-point methods with semantics identical to ordinary services.
- [DONE] T116: Implement PreparedGeometry as the public prepared service with one constructor, invalid-input handling, and safe concurrent read-only calls.
- [DONE] T117: Implement deterministic packed STRtree construction with configurable capacity, stable sort-tile-recursive packing, empty trees, and build lifecycle.
- [DONE] T118: Implement STRtree envelope/geometry queries, enumeration, removal policy, nearest/k-nearest branch-and-bound, and within-distance query.
- [DONE] T119: Implement dynamic R-tree choose-subtree, insert, quadratic split, remove, condense/reinsert, update, root collapse, duplicate-id, and clear behavior.
- [DONE] T120: Implement dynamic R-tree envelope/geometry, enumeration, nearest/k-nearest, and within-distance queries using snapshot results.
- [DONE] T121: Implement StrTreeIndex and RTreeIndex as separate ISpatialIndex implementations with integer ids, optional geometry/payload, explicit refinement, and predictable mutation rules.
- [DONE] T122: Add prepared/unprepared equivalence tests for every supported predicate/relation/metric, geometry family, empty case, invalid input, and snapshot isolation.
- [DONE] T123: Add STRtree and R-tree contract tests for lifecycle, queries, false-positive refinement, mutations, duplicates, nearest ties, empty indexes, brute-force equivalence, deterministic ordering, and concurrent query semantics without timing assertions.

### Linear referencing and measured coordinates

- [DONE] T124: Implement LinearLocation comparison, normalization, endpoint resolution, component traversal, and absolute/normalized length indexing.
- [DONE] T125: Implement point projection, interpolation, offset point, and line substring extraction for LineString and MultiLineString with degenerate segments.
- [DONE] T126: Implement monotonic M validation, locate/interpolate by M, measured interval extraction, and M preservation through split/merge operations.
- [DONE] T127: Implement ILinearReferencer and LinearReferencer with clamp/error policy expressed through explicit data/options rather than overloads.
- [DONE] T128: Add linear-reference tests for components, endpoints, normalized/absolute indexes, repeated points, offsets, substrings, clamping, monotonic/nonmonotonic M, Z/M interpolation, and empty inputs.

### Polygonal coverages

- [DONE] T129: Implement coverage edge extraction and indexed detection of non-noded edges, overlaps, and gaps bounded by a caller-supplied maximum width.
- [DONE] T130: Implement coverage union using shared-edge cancellation and polygon reconstruction.
- [DONE] T131: Implement topology-preserving coverage-wide Visvalingam simplification that simplifies each shared edge once and reuses it in adjacent polygons.
- [DONE] T132: Implement coverage cleaning with snapping distance, gap width, overlap merge strategy, valid reconstruction, and diagnostic reporting.
- [DONE] T133: Implement ICoverageOperations and CoverageOperations for validation, edge extraction, union, simplification, and cleaning.
- [DONE] T134: Add coverage tests for valid mosaics, overlaps, gaps, T-junctions, shared edges, holes, simplification consistency, cleaning strategies, and union area invariants.

### Geographic and GeoJSON feature operations

- [DONE] T135: Implement unit conversion, normalized longitude/latitude helpers, haversine distance, bearings, destination, midpoint, and pole/antimeridian-safe numeric behavior.
- [DONE] T136: Implement great-circle and rhumb lines/distances/destinations plus geographic line length, along, slice, and nearest-on-line operations.
- [DONE] T137: Implement spherical polygon/multipolygon area with holes and explicit treatment of antimeridian crossings.
- [DONE] T138: Implement geographic circle generation and IGeographicOperations/GeographicOperations with explicit units on every angular-to-linear calculation.
- [DONE] T139: Implement wrapped RFC 7946 bbox calculation and antimeridian cutting for points, lines, polygons, multi-geometries, collections, Features, and FeatureCollections.
- [DONE] T140: Implement GeoJSON rewind, coordinate truncation, bbox assignment, flatten, explode, combine, points-within-polygons, and intersecting-feature joins while preserving ids/properties/foreign members.
- [DONE] T141: Implement IGeoJsonOperations and GeoJsonOperations with strict rejection of unsupported extended geometry output.
- [DONE] T142: Add geographic tests against independently calculated authoritative fixtures for units, distance, bearing, destination, rhumb/great-circle paths, spherical area, poles, dateline, along/slice, nearest, and circles.
- [DONE] T143: Add RFC 7946 feature-operation tests for wrapped bbox, antimeridian splits, winding, truncation, flatten/explode/combine, spatial joins, null geometry, properties, ids, and foreign-member preservation.

### SFA surfaces and curved interoperability

- [DONE] T144: Implement Triangle, TIN, and PolyhedralSurface inspection, boundary, patch extraction, closure, projected area, and 3D surface area algorithms.
- [DONE] T145: Implement CircularString arc centers/radii/sweep selection, length/envelope/area contributions, closure, and tolerance-controlled adaptive linearization.
- [DONE] T146: Implement CompoundCurve continuity, CurvePolygon ring construction, MultiCurve/MultiSurface aggregation, and curve-aware factory/inspection/edit operations.
- [DONE] T147: Integrate curved values into WKT/WKB, validation, distance, predicates, overlay, buffer, and simplification through explicit linearization parameters and error bounds.
- [DONE] T148: Add SFA surface tests for model invariants, WKT/WKB, closure, boundary, 2D/3D area, validity, and core operation participation.
- [DONE] T149: Add curved geometry tests for arc direction, major/minor arcs, continuity, closure, envelopes, metrics, format round trips, linearization error, and linear-kernel operation equivalence.

### Complete API verification

- [DONE] T150: Add tests-only NetTopologySuite dependency and deterministic adapters that compare equivalent core geometries, predicates, metrics, overlay, buffer, validity, repair, and WKT/WKB behavior without leaking the dependency into src/.
- [DONE] T151: Transcribe applicable OGC SFA conformance examples and RFC 7946/RFC 8142 examples into provenance-marked fixtures and exact expected-result tests.
- [DONE] T152: Add fixed-seed randomized generators for valid and intentionally invalid points, lines, polygons, multis, and collections, plus automatic minimized regression fixture capture.
- [DONE] T153: Add algebraic invariant tests for relation converse/transpose, predicate duals, set-operation identities, area bounds, normalization idempotence, buffer containment, repair validity, and serialization round trips.
- [DONE] T154: Add public API contract tests that enumerate and exercise every public interface method, option branch, enum value, constants field, data-field invariant, and custom exception path without testing syntax policy itself.
- [DONE] T155: Audit the src/Api manifest against functional tests and close every uncovered behavior, geometry type/layout combination, empty case, malformed input, and standards requirement before declaring implementation complete.
