using System;
using System.IO;

namespace geo;

internal class WkbFormatter
{
    internal WkbWriteOptions Options;
    internal byte[] Bytes = [];

    internal WkbFormatter(Geometry geometry, WkbWriteOptions options)
    {
        Options = CopyAndValidateOptions(options);
        int size = WkbSizer.GetConfiguredSize(geometry, Options);
        Bytes = new byte[size];
        int written = WriteGeometryValue(geometry, 0, true);
        if (written != size)
        {
            throw new InvalidGeometryException("Computed WKB size does not match the encoded byte count.");
        }
    }

    internal byte[] FormatBytes()
    {
        byte[] copy = new byte[Bytes.Length];
        Bytes.CopyTo(copy, 0);
        return copy;
    }

    internal string FormatHex()
    {
        return Convert.ToHexString(Bytes);
    }

    internal WriteResult TryWriteBytes(Span<byte> destination)
    {
        WriteResult result = new WriteResult();
        result.RequiredSize = Bytes.Length;
        if (destination.Length < Bytes.Length)
        {
            ParseError error = new ParseError();
            error.Kind = ParseErrorKind.LimitExceeded;
            error.Offset = destination.Length;
            error.Message =
                $"WKB destination requires {Bytes.Length} bytes but only {destination.Length} are available.";
            result.Error = error;
            return result;
        }

        Bytes.AsSpan().CopyTo(destination);
        result.Success = true;
        result.WrittenSize = Bytes.Length;
        return result;
    }

    internal void WriteToStream(Stream stream)
    {
        EndianWriter.ValidateStream(stream);
        stream.Write(Bytes, 0, Bytes.Length);
    }

    internal int WriteGeometryValue(Geometry geometry, int offset, bool root)
    {
        offset = WriteHeaderValue(
            geometry.GeometryType, geometry.CoordinateLayout, geometry.SpatialReferenceId, offset, root);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return WriteCoordinateValue(((Point)payload).Coordinate, geometry.CoordinateLayout, offset);
            case GeometryType.LineString:
                return WriteCoordinateSequenceValue(
                    ((LineString)payload).Coordinates, geometry.CoordinateLayout, offset);
            case GeometryType.Polygon:
                return WritePolygonPayloadValue((Polygon)payload, geometry.CoordinateLayout, offset);
            case GeometryType.MultiPoint:
                return WriteMultiPointPayloadValue((MultiPoint)payload, geometry.CoordinateLayout, offset);
            case GeometryType.MultiLineString:
                return WriteMultiLinePayloadValue((MultiLineString)payload, geometry.CoordinateLayout, offset);
            case GeometryType.MultiPolygon:
                return WriteMultiPolygonPayloadValue((MultiPolygon)payload, geometry.CoordinateLayout, offset);
            case GeometryType.GeometryCollection:
                return WriteGeometryArrayValue(((GeometryCollection)payload).Geometries, offset);
            case GeometryType.Triangle:
                return WriteTrianglePayloadValue((Triangle)payload, geometry.CoordinateLayout, offset);
            case GeometryType.Tin:
                return WriteTinPayloadValue((Tin)payload, geometry.CoordinateLayout, offset);
            case GeometryType.PolyhedralSurface:
                return WritePolyhedralSurfacePayloadValue(
                    (PolyhedralSurface)payload, geometry.CoordinateLayout, offset);
            case GeometryType.CircularString:
                return WriteCoordinateSequenceValue(
                    ((CircularString)payload).Coordinates, geometry.CoordinateLayout, offset);
            case GeometryType.CompoundCurve:
                return WriteGeometryArrayValue(((CompoundCurve)payload).Curves, offset);
            case GeometryType.CurvePolygon:
                return WriteCurvePolygonPayloadValue((CurvePolygon)payload, offset);
            case GeometryType.MultiCurve:
                return WriteGeometryArrayValue(((MultiCurve)payload).Curves, offset);
            case GeometryType.MultiSurface:
                return WriteGeometryArrayValue(((MultiSurface)payload).Surfaces, offset);
            default:
                throw new InvalidGeometryException("Geometry type cannot be written as WKB.");
        }
    }

    internal int WriteHeaderValue(
        GeometryType geometryType, CoordinateLayout layout, int spatialReferenceId, int offset, bool root)
    {
        EndianWriter.WriteByteToArray(Bytes, offset, (byte)Options.ByteOrder);
        offset++;
        uint typeCode = GetTypeCodeValue(geometryType, layout);
        bool includeSpatialReferenceId = root && Options.Flavor == WkbFlavor.Ewkb &&
            Options.IncludeSpatialReferenceId;
        if (includeSpatialReferenceId)
        {
            typeCode = typeCode | WkbTypeCodes.EwkbSridFlag;
        }
        EndianWriter.WriteUInt32ToArray(Bytes, offset, typeCode, Options.ByteOrder);
        offset += 4;
        if (includeSpatialReferenceId)
        {
            EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)spatialReferenceId, Options.ByteOrder);
            offset += 4;
        }
        return offset;
    }

    internal int WriteCoordinateValue(Coordinate? coordinate, CoordinateLayout layout, int offset)
    {
        double x = GeometryConstants.NoDataOrdinate;
        double y = GeometryConstants.NoDataOrdinate;
        double z = GeometryConstants.NoDataOrdinate;
        double m = GeometryConstants.NoDataOrdinate;
        if (coordinate != null)
        {
            x = coordinate.X;
            y = coordinate.Y;
            z = coordinate.Z;
            m = coordinate.M;
        }

        EndianWriter.WriteDoubleToArray(Bytes, offset, x, Options.ByteOrder);
        offset += 8;
        EndianWriter.WriteDoubleToArray(Bytes, offset, y, Options.ByteOrder);
        offset += 8;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            EndianWriter.WriteDoubleToArray(Bytes, offset, z, Options.ByteOrder);
            offset += 8;
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            EndianWriter.WriteDoubleToArray(Bytes, offset, m, Options.ByteOrder);
            offset += 8;
        }
        return offset;
    }

    internal int WriteCoordinateSequenceValue(Coordinate[] coordinates, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)coordinates.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < coordinates.Length)
        {
            offset = WriteCoordinateValue(coordinates[index], layout, offset);
            index++;
        }
        return offset;
    }

    internal int WritePolygonPayloadValue(Polygon polygon, CoordinateLayout layout, int offset)
    {
        int ringCount = polygon.Shell.Coordinates.Length == 0 ? 0 : polygon.Holes.Length + 1;
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)ringCount, Options.ByteOrder);
        offset += 4;
        if (ringCount == 0) return offset;
        offset = WriteCoordinateSequenceValue(polygon.Shell.Coordinates, layout, offset);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            offset = WriteCoordinateSequenceValue(polygon.Holes[index].Coordinates, layout, offset);
            index++;
        }
        return offset;
    }

    internal int WriteTrianglePayloadValue(Triangle triangle, CoordinateLayout layout, int offset)
    {
        int ringCount = triangle.Shell.Coordinates.Length == 0 ? 0 : 1;
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)ringCount, Options.ByteOrder);
        offset += 4;
        if (ringCount != 0)
        {
            offset = WriteCoordinateSequenceValue(triangle.Shell.Coordinates, layout, offset);
        }
        return offset;
    }

    internal int WriteMultiPointPayloadValue(MultiPoint multiPoint, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)multiPoint.Points.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            offset = WriteHeaderValue(GeometryType.Point, layout, 0, offset, false);
            offset = WriteCoordinateValue(multiPoint.Points[index].Coordinate, layout, offset);
            index++;
        }
        return offset;
    }

    internal int WriteMultiLinePayloadValue(MultiLineString multiLine, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)multiLine.LineStrings.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            offset = WriteHeaderValue(GeometryType.LineString, layout, 0, offset, false);
            offset = WriteCoordinateSequenceValue(multiLine.LineStrings[index].Coordinates, layout, offset);
            index++;
        }
        return offset;
    }

    internal int WriteMultiPolygonPayloadValue(MultiPolygon multiPolygon, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)multiPolygon.Polygons.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            offset = WriteHeaderValue(GeometryType.Polygon, layout, 0, offset, false);
            offset = WritePolygonPayloadValue(multiPolygon.Polygons[index], layout, offset);
            index++;
        }
        return offset;
    }

    internal int WriteGeometryArrayValue(Geometry[] geometries, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)geometries.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < geometries.Length)
        {
            offset = WriteGeometryValue(geometries[index], offset, false);
            index++;
        }
        return offset;
    }

    internal int WriteTinPayloadValue(Tin tin, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)tin.Triangles.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            offset = WriteHeaderValue(GeometryType.Triangle, layout, 0, offset, false);
            offset = WriteTrianglePayloadValue(tin.Triangles[index], layout, offset);
            index++;
        }
        return offset;
    }

    internal int WritePolyhedralSurfacePayloadValue(
        PolyhedralSurface surface, CoordinateLayout layout, int offset)
    {
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)surface.Patches.Length, Options.ByteOrder);
        offset += 4;
        int index = 0;
        while (index < surface.Patches.Length)
        {
            offset = WriteHeaderValue(GeometryType.Polygon, layout, 0, offset, false);
            offset = WritePolygonPayloadValue(surface.Patches[index], layout, offset);
            index++;
        }
        return offset;
    }

    internal int WriteCurvePolygonPayloadValue(CurvePolygon polygon, int offset)
    {
        int ringCount = polygon.Shell == null ? 0 : polygon.Holes.Length + 1;
        EndianWriter.WriteUInt32ToArray(Bytes, offset, (uint)ringCount, Options.ByteOrder);
        offset += 4;
        if (polygon.Shell == null) return offset;
        offset = WriteGeometryValue(polygon.Shell, offset, false);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            offset = WriteGeometryValue(polygon.Holes[index], offset, false);
            index++;
        }
        return offset;
    }

    internal uint GetTypeCodeValue(GeometryType geometryType, CoordinateLayout layout)
    {
        uint typeCode = (uint)(int)geometryType;
        if (Options.Flavor == WkbFlavor.Ogc)
        {
            if (layout == CoordinateLayout.XYZ) typeCode += (uint)WkbTypeCodes.ZOffset;
            else if (layout == CoordinateLayout.XYM) typeCode += (uint)WkbTypeCodes.MOffset;
            else if (layout == CoordinateLayout.XYZM) typeCode += (uint)WkbTypeCodes.ZmOffset;
            return typeCode;
        }

        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            typeCode = typeCode | WkbTypeCodes.EwkbZFlag;
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            typeCode = typeCode | WkbTypeCodes.EwkbMFlag;
        }
        return typeCode;
    }

    internal static WkbWriteOptions CopyAndValidateOptions(WkbWriteOptions source)
    {
        WkbSizer.ValidateWriteOptions(source);
        WkbWriteOptions copy = new WkbWriteOptions();
        copy.ByteOrder = source.ByteOrder;
        copy.Flavor = source.Flavor;
        copy.IncludeSpatialReferenceId = source.IncludeSpatialReferenceId;
        return copy;
    }
}
