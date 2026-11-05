using System;
using System.IO;
using System.Text;

namespace geo;

internal class WktFormatter
{
    internal WktWriteOptions Options;
    internal string Text = "";

    internal WktFormatter(Geometry geometry, WktWriteOptions options)
    {
        Options = CopyAndValidateOptions(options);
        GeometryTransforms.ValidateEditedGeometryStructure(geometry);
        ValidateFormattingTree(geometry);
        StringBuilder builder = new StringBuilder();
        if (Options.IncludeSpatialReferenceId)
        {
            builder.Append("SRID=");
            builder.Append(geometry.SpatialReferenceId);
            builder.Append(';');
        }
        AppendTaggedGeometry(builder, geometry);
        Text = builder.ToString();
    }

    internal string FormatString()
    {
        return Text;
    }

    internal WriteResult TryWriteCharacters(Span<char> destination)
    {
        WriteResult result = new WriteResult();
        result.RequiredSize = Text.Length;
        if (destination.Length < Text.Length)
        {
            ParseError error = new ParseError();
            error.Kind = ParseErrorKind.LimitExceeded;
            error.Offset = destination.Length;
            error.Message = $"WKT destination requires {Text.Length} characters but only {destination.Length} are available.";
            result.Error = error;
            return result;
        }

        Text.AsSpan().CopyTo(destination);
        result.Success = true;
        result.WrittenSize = Text.Length;
        return result;
    }

    internal void WriteToTextWriter(TextWriter writer)
    {
        if (writer == null)
        {
            throw new InvalidGeometryException("WKT text writer is null.");
        }
        writer.Write(Text);
    }

    internal void AppendTaggedGeometry(StringBuilder builder, Geometry geometry)
    {
        builder.Append(GetTypeName(geometry.GeometryType));
        AppendLayoutToken(builder, geometry.CoordinateLayout);
        builder.Append(' ');
        if (IsCanonicalEmptyPayload(geometry))
        {
            builder.Append("EMPTY");
            return;
        }

        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Point point = (Point)payload;
                builder.Append('(');
                AppendCoordinate(builder, point.Coordinate, geometry.CoordinateLayout);
                builder.Append(')');
                break;
            case GeometryType.LineString:
                AppendCoordinateSequence(builder, ((LineString)payload).Coordinates, geometry.CoordinateLayout);
                break;
            case GeometryType.Polygon:
                AppendPolygonText(builder, (Polygon)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiPoint:
                AppendMultiPointText(builder, (MultiPoint)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiLineString:
                AppendMultiLineText(builder, (MultiLineString)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiPolygon:
                AppendMultiPolygonText(builder, (MultiPolygon)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.GeometryCollection:
                AppendGeometryCollectionText(builder, (GeometryCollection)payload);
                break;
            case GeometryType.Triangle:
                AppendTriangleText(builder, (Triangle)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.Tin:
                AppendTinText(builder, (Tin)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.PolyhedralSurface:
                AppendPolyhedralSurfaceText(builder, (PolyhedralSurface)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.CircularString:
                AppendCoordinateSequence(builder, ((CircularString)payload).Coordinates, geometry.CoordinateLayout);
                break;
            case GeometryType.CompoundCurve:
                AppendCompoundCurveText(builder, (CompoundCurve)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.CurvePolygon:
                AppendCurvePolygonText(builder, (CurvePolygon)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiCurve:
                AppendMultiCurveText(builder, (MultiCurve)payload, geometry.CoordinateLayout);
                break;
            case GeometryType.MultiSurface:
                AppendMultiSurfaceText(builder, (MultiSurface)payload, geometry.CoordinateLayout);
                break;
            default:
                throw new InvalidGeometryException("Geometry type cannot be formatted as WKT.");
        }
    }

    internal void AppendLayoutToken(StringBuilder builder, CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XYZ) builder.Append(" Z");
        else if (layout == CoordinateLayout.XYM) builder.Append(" M");
        else if (layout == CoordinateLayout.XYZM) builder.Append(" ZM");
    }

    internal void AppendCoordinate(StringBuilder builder, Coordinate? coordinate, CoordinateLayout layout)
    {
        if (coordinate == null)
        {
            throw new InvalidGeometryException("A non-empty WKT coordinate is null.");
        }
        builder.Append(WktNumber.FormatOrdinate(coordinate.X, Options));
        builder.Append(' ');
        builder.Append(WktNumber.FormatOrdinate(coordinate.Y, Options));
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            builder.Append(' ');
            builder.Append(WktNumber.FormatOrdinate(coordinate.Z, Options));
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            builder.Append(' ');
            builder.Append(WktNumber.FormatOrdinate(coordinate.M, Options));
        }
    }

    internal void AppendCoordinateSequence(StringBuilder builder, Coordinate[] coordinates, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < coordinates.Length)
        {
            if (index != 0) builder.Append(", ");
            AppendCoordinate(builder, coordinates[index], layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendPolygonText(StringBuilder builder, Polygon polygon, CoordinateLayout layout)
    {
        builder.Append('(');
        AppendCoordinateSequence(builder, polygon.Shell.Coordinates, layout);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            builder.Append(", ");
            AppendCoordinateSequence(builder, polygon.Holes[index].Coordinates, layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendTriangleText(StringBuilder builder, Triangle triangle, CoordinateLayout layout)
    {
        builder.Append('(');
        AppendCoordinateSequence(builder, triangle.Shell.Coordinates, layout);
        builder.Append(')');
    }

    internal void AppendMultiPointText(StringBuilder builder, MultiPoint multiPoint, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            if (index != 0) builder.Append(", ");
            Point point = multiPoint.Points[index];
            if (point.Coordinate == null)
            {
                builder.Append("EMPTY");
            }
            else
            {
                builder.Append('(');
                AppendCoordinate(builder, point.Coordinate, layout);
                builder.Append(')');
            }
            index++;
        }
        builder.Append(')');
    }

    internal void AppendMultiLineText(StringBuilder builder, MultiLineString multiLine, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            if (index != 0) builder.Append(", ");
            LineString line = multiLine.LineStrings[index];
            if (line.Coordinates.Length == 0) builder.Append("EMPTY");
            else AppendCoordinateSequence(builder, line.Coordinates, layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendMultiPolygonText(StringBuilder builder, MultiPolygon multiPolygon, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            if (index != 0) builder.Append(", ");
            Polygon polygon = multiPolygon.Polygons[index];
            if (polygon.Shell.Coordinates.Length == 0) builder.Append("EMPTY");
            else AppendPolygonText(builder, polygon, layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendGeometryCollectionText(StringBuilder builder, GeometryCollection collection)
    {
        builder.Append('(');
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            if (index != 0) builder.Append(", ");
            AppendTaggedGeometry(builder, collection.Geometries[index]);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendTinText(StringBuilder builder, Tin tin, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            if (index != 0) builder.Append(", ");
            Triangle triangle = tin.Triangles[index];
            if (triangle.Shell.Coordinates.Length == 0) builder.Append("EMPTY");
            else AppendTriangleText(builder, triangle, layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendPolyhedralSurfaceText(StringBuilder builder, PolyhedralSurface surface, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < surface.Patches.Length)
        {
            if (index != 0) builder.Append(", ");
            Polygon polygon = surface.Patches[index];
            if (polygon.Shell.Coordinates.Length == 0) builder.Append("EMPTY");
            else AppendPolygonText(builder, polygon, layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendCompoundCurveText(StringBuilder builder, CompoundCurve compoundCurve, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < compoundCurve.Curves.Length)
        {
            if (index != 0) builder.Append(", ");
            Geometry curve = compoundCurve.Curves[index];
            if (curve.GeometryType == GeometryType.LineString)
            {
                LineString line = (LineString)GeometryDispatch.GetSelectedPayload(curve);
                AppendCoordinateSequence(builder, line.Coordinates, layout);
            }
            else
            {
                AppendTaggedGeometry(builder, curve);
            }
            index++;
        }
        builder.Append(')');
    }

    internal void AppendCurvePolygonText(StringBuilder builder, CurvePolygon polygon, CoordinateLayout layout)
    {
        builder.Append('(');
        AppendCurveComponentText(builder, polygon.Shell, layout);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            builder.Append(", ");
            AppendCurveComponentText(builder, polygon.Holes[index], layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendMultiCurveText(StringBuilder builder, MultiCurve multiCurve, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            if (index != 0) builder.Append(", ");
            AppendCurveComponentText(builder, multiCurve.Curves[index], layout);
            index++;
        }
        builder.Append(')');
    }

    internal void AppendCurveComponentText(StringBuilder builder, Geometry? curve, CoordinateLayout layout)
    {
        if (curve == null)
        {
            throw new InvalidGeometryException("WKT curve component is null.");
        }
        if (curve.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(curve);
            if (line.Coordinates.Length == 0) AppendTaggedGeometry(builder, curve);
            else AppendCoordinateSequence(builder, line.Coordinates, layout);
        }
        else
        {
            AppendTaggedGeometry(builder, curve);
        }
    }

    internal void AppendMultiSurfaceText(StringBuilder builder, MultiSurface multiSurface, CoordinateLayout layout)
    {
        builder.Append('(');
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            if (index != 0) builder.Append(", ");
            Geometry surface = multiSurface.Surfaces[index];
            if (surface.GeometryType == GeometryType.Polygon)
            {
                Polygon polygon = (Polygon)GeometryDispatch.GetSelectedPayload(surface);
                if (polygon.Shell.Coordinates.Length == 0) AppendTaggedGeometry(builder, surface);
                else AppendPolygonText(builder, polygon, layout);
            }
            else
            {
                AppendTaggedGeometry(builder, surface);
            }
            index++;
        }
        builder.Append(')');
    }

    internal bool IsCanonicalEmptyPayload(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return ((Point)payload).Coordinate == null;
            case GeometryType.LineString:
                return ((LineString)payload).Coordinates.Length == 0;
            case GeometryType.Polygon:
                return ((Polygon)payload).Shell.Coordinates.Length == 0;
            case GeometryType.MultiPoint:
                return ((MultiPoint)payload).Points.Length == 0;
            case GeometryType.MultiLineString:
                return ((MultiLineString)payload).LineStrings.Length == 0;
            case GeometryType.MultiPolygon:
                return ((MultiPolygon)payload).Polygons.Length == 0;
            case GeometryType.GeometryCollection:
                return ((GeometryCollection)payload).Geometries.Length == 0;
            case GeometryType.Triangle:
                return ((Triangle)payload).Shell.Coordinates.Length == 0;
            case GeometryType.Tin:
                return ((Tin)payload).Triangles.Length == 0;
            case GeometryType.PolyhedralSurface:
                return ((PolyhedralSurface)payload).Patches.Length == 0;
            case GeometryType.CircularString:
                return ((CircularString)payload).Coordinates.Length == 0;
            case GeometryType.CompoundCurve:
                return ((CompoundCurve)payload).Curves.Length == 0;
            case GeometryType.CurvePolygon:
                return ((CurvePolygon)payload).Shell == null;
            case GeometryType.MultiCurve:
                return ((MultiCurve)payload).Curves.Length == 0;
            case GeometryType.MultiSurface:
                return ((MultiSurface)payload).Surfaces.Length == 0;
            default:
                return false;
        }
    }

    internal void ValidateFormattingTree(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LinearRing)
        {
            throw new InvalidGeometryException("LinearRing is a WKT component and cannot be formatted as a tagged geometry.");
        }
        if (WktParser.IsExtendedGeometryType(geometry.GeometryType) && !Options.AllowExtendedTypes)
        {
            throw new InvalidGeometryException("Extended WKT geometry formatting is disabled.");
        }

        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.GeometryCollection:
                ValidateGeometryArray(((GeometryCollection)payload).Geometries);
                break;
            case GeometryType.Triangle:
                ValidateTrianglePayload((Triangle)payload);
                break;
            case GeometryType.Tin:
                ValidateTriangleArray(((Tin)payload).Triangles);
                break;
            case GeometryType.PolyhedralSurface:
                ValidatePolygonArray(((PolyhedralSurface)payload).Patches);
                break;
            case GeometryType.CircularString:
                ValidateCircularCoordinates(((CircularString)payload).Coordinates);
                break;
            case GeometryType.CompoundCurve:
                ValidateCompoundCurvePayload((CompoundCurve)payload);
                break;
            case GeometryType.CurvePolygon:
                ValidateCurvePolygonPayload((CurvePolygon)payload);
                break;
            case GeometryType.MultiCurve:
                ValidateMultiCurvePayload((MultiCurve)payload);
                break;
            case GeometryType.MultiSurface:
                ValidateMultiSurfacePayload((MultiSurface)payload);
                break;
            default:
                break;
        }
    }

    internal void ValidateGeometryArray(Geometry[] geometries)
    {
        int index = 0;
        while (index < geometries.Length)
        {
            ValidateFormattingTree(geometries[index]);
            index++;
        }
    }

    internal void ValidateTrianglePayload(Triangle triangle)
    {
        if (triangle.Shell.Coordinates.Length != 0 && triangle.Shell.Coordinates.Length != 4)
        {
            throw new InvalidGeometryException("A non-empty Triangle must contain exactly four ring coordinates.");
        }
    }

    internal void ValidateTriangleArray(Triangle[] triangles)
    {
        int index = 0;
        while (index < triangles.Length)
        {
            ValidateTrianglePayload(triangles[index]);
            index++;
        }
    }

    internal void ValidatePolygonArray(Polygon[] polygons)
    {
        int index = 0;
        while (index < polygons.Length)
        {
            Polygon polygon = polygons[index];
            Geometry holder = new Geometry();
            holder.Point = null;
            holder.GeometryType = GeometryType.Polygon;
            holder.Polygon = polygon;
            ValidateFormattingTree(holder);
            index++;
        }
    }

    internal void ValidateCircularCoordinates(Coordinate[] coordinates)
    {
        if (coordinates.Length != 0 && (coordinates.Length < 3 || coordinates.Length % 2 == 0))
        {
            throw new InvalidGeometryException("A non-empty CircularString requires an odd number of at least three coordinates.");
        }
    }

    internal void ValidateCompoundCurvePayload(CompoundCurve compoundCurve)
    {
        int index = 0;
        Coordinate? previousEnd = null;
        while (index < compoundCurve.Curves.Length)
        {
            Geometry curve = compoundCurve.Curves[index];
            if (curve.GeometryType != GeometryType.LineString && curve.GeometryType != GeometryType.CircularString)
            {
                throw new InvalidGeometryException("CompoundCurve components must be LineString or CircularString values.");
            }
            ValidateFormattingTree(curve);
            Coordinate? start = GetCurveStartCoordinate(curve);
            Coordinate? end = GetCurveEndCoordinate(curve);
            if (start == null || end == null)
            {
                throw new InvalidGeometryException("CompoundCurve components cannot be empty.");
            }
            if (previousEnd != null && (previousEnd.X != start.X || previousEnd.Y != start.Y))
            {
                throw new InvalidGeometryException("CompoundCurve components must be continuous.");
            }
            previousEnd = end;
            index++;
        }
    }

    internal void ValidateCurvePolygonPayload(CurvePolygon polygon)
    {
        if (polygon.Shell == null)
        {
            if (polygon.Holes.Length != 0)
            {
                throw new InvalidGeometryException("An empty CurvePolygon cannot contain holes.");
            }
            return;
        }

        ValidateCurveRing(polygon.Shell);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            ValidateCurveRing(polygon.Holes[index]);
            index++;
        }
    }

    internal void ValidateCurveRing(Geometry curve)
    {
        ValidateCurveType(curve, true);
        Coordinate? start = GetCurveStartCoordinate(curve);
        Coordinate? end = GetCurveEndCoordinate(curve);
        if (start == null || end == null || start.X != end.X || start.Y != end.Y)
        {
            throw new InvalidGeometryException("CurvePolygon rings must be non-empty and closed.");
        }
        if (curve.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(curve);
            if (line.Coordinates.Length < 4 || !GeometryInspection.HasThreeDistinctPlanarCoordinates(line.Coordinates))
            {
                throw new InvalidGeometryException("A linear CurvePolygon ring requires four coordinates and three distinct positions.");
            }
        }
    }

    internal void ValidateMultiCurvePayload(MultiCurve multiCurve)
    {
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            ValidateCurveType(multiCurve.Curves[index], true);
            index++;
        }
    }

    internal void ValidateCurveType(Geometry curve, bool allowCompoundCurve)
    {
        if (curve.GeometryType != GeometryType.LineString && curve.GeometryType != GeometryType.CircularString &&
            (!allowCompoundCurve || curve.GeometryType != GeometryType.CompoundCurve))
        {
            throw new InvalidGeometryException("A curve component has an unsupported geometry type.");
        }
        ValidateFormattingTree(curve);
    }

    internal void ValidateMultiSurfacePayload(MultiSurface multiSurface)
    {
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            Geometry surface = multiSurface.Surfaces[index];
            if (surface.GeometryType != GeometryType.Polygon && surface.GeometryType != GeometryType.CurvePolygon &&
                surface.GeometryType != GeometryType.Triangle && surface.GeometryType != GeometryType.Tin &&
                surface.GeometryType != GeometryType.PolyhedralSurface)
            {
                throw new InvalidGeometryException("A MultiSurface component has an unsupported geometry type.");
            }
            ValidateFormattingTree(surface);
            index++;
        }
    }

    internal Coordinate? GetCurveStartCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(geometry);
            return line.Coordinates.Length == 0 ? null : line.Coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CircularString)
        {
            CircularString circularString = (CircularString)GeometryDispatch.GetSelectedPayload(geometry);
            return circularString.Coordinates.Length == 0 ? null : circularString.Coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve)
        {
            CompoundCurve compoundCurve = (CompoundCurve)GeometryDispatch.GetSelectedPayload(geometry);
            return compoundCurve.Curves.Length == 0 ? null : GetCurveStartCoordinate(compoundCurve.Curves[0]);
        }
        return null;
    }

    internal Coordinate? GetCurveEndCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(geometry);
            return line.Coordinates.Length == 0 ? null : line.Coordinates[line.Coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CircularString)
        {
            CircularString circularString = (CircularString)GeometryDispatch.GetSelectedPayload(geometry);
            return circularString.Coordinates.Length == 0 ? null : circularString.Coordinates[circularString.Coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve)
        {
            CompoundCurve compoundCurve = (CompoundCurve)GeometryDispatch.GetSelectedPayload(geometry);
            return compoundCurve.Curves.Length == 0 ? null : GetCurveEndCoordinate(compoundCurve.Curves[compoundCurve.Curves.Length - 1]);
        }
        return null;
    }

    internal static WktWriteOptions CopyAndValidateOptions(WktWriteOptions source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("WKT write options are null.");
        }
        if (source.DecimalPrecision < 0 || source.DecimalPrecision > 17)
        {
            throw new InvalidGeometryException("WKT decimal precision must be between zero and seventeen.");
        }

        WktWriteOptions options = new WktWriteOptions();
        options.DecimalPrecision = source.DecimalPrecision;
        options.TrimTrailingZeros = source.TrimTrailingZeros;
        options.IncludeSpatialReferenceId = source.IncludeSpatialReferenceId;
        options.AllowExtendedTypes = source.AllowExtendedTypes;
        return options;
    }

    internal static string GetTypeName(GeometryType geometryType)
    {
        if (geometryType == GeometryType.Point) return "POINT";
        if (geometryType == GeometryType.LineString) return "LINESTRING";
        if (geometryType == GeometryType.Polygon) return "POLYGON";
        if (geometryType == GeometryType.MultiPoint) return "MULTIPOINT";
        if (geometryType == GeometryType.MultiLineString) return "MULTILINESTRING";
        if (geometryType == GeometryType.MultiPolygon) return "MULTIPOLYGON";
        if (geometryType == GeometryType.GeometryCollection) return "GEOMETRYCOLLECTION";
        if (geometryType == GeometryType.CircularString) return "CIRCULARSTRING";
        if (geometryType == GeometryType.CompoundCurve) return "COMPOUNDCURVE";
        if (geometryType == GeometryType.CurvePolygon) return "CURVEPOLYGON";
        if (geometryType == GeometryType.MultiCurve) return "MULTICURVE";
        if (geometryType == GeometryType.MultiSurface) return "MULTISURFACE";
        if (geometryType == GeometryType.PolyhedralSurface) return "POLYHEDRALSURFACE";
        if (geometryType == GeometryType.Tin) return "TIN";
        if (geometryType == GeometryType.Triangle) return "TRIANGLE";
        throw new InvalidGeometryException("Geometry type cannot be represented by WKT.");
    }
}
