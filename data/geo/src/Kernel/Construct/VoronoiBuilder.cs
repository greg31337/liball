using System;
using System.Collections.Generic;

namespace geo;

internal class VoronoiHalfPlane
{
    internal double FirstX;
    internal double FirstY;
    internal double DirectionX;
    internal double DirectionY;
    internal double Threshold;
    internal double Scale;
}

internal static class VoronoiBuilder
{
    internal static Geometry GetVoronoiDiagram(
        Geometry geometry, Envelope clipEnvelope)
    {
        ValidateClipEnvelope(clipEnvelope);
        Coordinate[] sites = DelaunayBuilder.ExtractUniqueSites(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        List<Geometry> cells = new List<Geometry>();
        int siteIndex = 0;
        while (siteIndex < sites.Length)
        {
            List<Coordinate> cell = CreateEnvelopeCell(
                clipEnvelope, sites[siteIndex]);
            int otherIndex = 0;
            while (otherIndex < sites.Length && cell.Count != 0)
            {
                if (otherIndex != siteIndex)
                {
                    VoronoiHalfPlane plane = CreateHalfPlane(
                        sites[siteIndex], sites[otherIndex], clipEnvelope);
                    cell = ClipToHalfPlane(cell, plane);
                }
                otherIndex++;
            }
            if (cell.Count >= 3)
            {
                LinearRing shell = new LinearRing();
                shell.Coordinates = CloseCell(cell);
                if (RingMath.GetExactSignedAreaSign(
                    shell.Coordinates) != 0)
                {
                    cells.Add(GeometryBuilder.CreatePolygonValue(
                        shell, [], options));
                }
            }
            siteIndex++;
        }
        return GeometryBuilder.CreateGeometryCollectionValue(
            cells.ToArray(), options);
    }

    internal static void ValidateClipEnvelope(Envelope envelope)
    {
        if (envelope == null ||
            !double.IsFinite(envelope.MinX) ||
            !double.IsFinite(envelope.MinY) ||
            !double.IsFinite(envelope.MaxX) ||
            !double.IsFinite(envelope.MaxY) ||
            envelope.MinX >= envelope.MaxX ||
            envelope.MinY >= envelope.MaxY)
        {
            throw new InvalidGeometryException(
                "Voronoi clip envelope must be finite and non-empty.");
        }
    }

    internal static List<Coordinate> CreateEnvelopeCell(
        Envelope envelope, Coordinate ordinateSource)
    {
        List<Coordinate> cell = new List<Coordinate>();
        cell.Add(CreateCellCoordinate(
            envelope.MinX, envelope.MinY, ordinateSource));
        cell.Add(CreateCellCoordinate(
            envelope.MaxX, envelope.MinY, ordinateSource));
        cell.Add(CreateCellCoordinate(
            envelope.MaxX, envelope.MaxY, ordinateSource));
        cell.Add(CreateCellCoordinate(
            envelope.MinX, envelope.MaxY, ordinateSource));
        return cell;
    }

    internal static VoronoiHalfPlane CreateHalfPlane(
        Coordinate site, Coordinate other, Envelope envelope)
    {
        double scale = 1.0;
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, other.X, site.X);
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, other.Y, site.Y);
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, envelope.MinX, site.X);
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, envelope.MinY, site.Y);
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, envelope.MaxX, site.X);
        scale = DelaunayBuilder.SelectDifferenceMagnitude(
            scale, envelope.MaxY, site.Y);
        VoronoiHalfPlane plane = new VoronoiHalfPlane();
        plane.FirstX = site.X;
        plane.FirstY = site.Y;
        plane.DirectionX = GetScaledDifference(other.X, site.X, scale);
        plane.DirectionY = GetScaledDifference(other.Y, site.Y, scale);
        plane.Threshold = (plane.DirectionX * plane.DirectionX +
            plane.DirectionY * plane.DirectionY) / 2.0;
        plane.Scale = scale;
        return plane;
    }

    internal static List<Coordinate> ClipToHalfPlane(
        List<Coordinate> polygon, VoronoiHalfPlane plane)
    {
        List<Coordinate> result = new List<Coordinate>();
        if (polygon.Count == 0) return result;
        Coordinate previous = polygon[polygon.Count - 1];
        double previousValue = GetHalfPlaneValue(previous, plane);
        bool previousInside = previousValue <= 0.0;
        int index = 0;
        while (index < polygon.Count)
        {
            Coordinate current = polygon[index];
            double currentValue = GetHalfPlaneValue(current, plane);
            bool currentInside = currentValue <= 0.0;
            if (currentInside != previousInside)
            {
                double denominator = previousValue - currentValue;
                if (denominator != 0.0)
                {
                    double fraction = previousValue / denominator;
                    if (fraction < 0.0) fraction = 0.0;
                    if (fraction > 1.0) fraction = 1.0;
                    result.Add(
                        RobustLineIntersection.InterpolateLineCoordinate(
                            previous, current, fraction));
                }
            }
            if (currentInside)
            {
                result.Add(RobustLineIntersection.CopyCoordinate(current));
            }
            previous = current;
            previousValue = currentValue;
            previousInside = currentInside;
            index++;
        }
        return RemoveAdjacentDuplicates(result);
    }

    internal static double GetHalfPlaneValue(
        Coordinate coordinate, VoronoiHalfPlane plane)
    {
        double x = GetScaledDifference(
            coordinate.X, plane.FirstX, plane.Scale);
        double y = GetScaledDifference(
            coordinate.Y, plane.FirstY, plane.Scale);
        return x * plane.DirectionX + y * plane.DirectionY -
            plane.Threshold;
    }

    internal static double GetScaledDifference(
        double first, double second, double scale)
    {
        double difference = first - second;
        if (double.IsFinite(difference)) return difference / scale;
        return first / scale - second / scale;
    }

    internal static List<Coordinate> RemoveAdjacentDuplicates(
        List<Coordinate> coordinates)
    {
        List<Coordinate> result = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Count)
        {
            if (result.Count == 0 ||
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    result[result.Count - 1], coordinates[index]))
            {
                result.Add(coordinates[index]);
            }
            index++;
        }
        if (result.Count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                result[0], result[result.Count - 1]))
        {
            result.RemoveAt(result.Count - 1);
        }
        return result;
    }

    internal static Coordinate[] CloseCell(List<Coordinate> cell)
    {
        Coordinate[] closed = new Coordinate[cell.Count + 1];
        int index = 0;
        while (index < cell.Count)
        {
            closed[index] =
                RobustLineIntersection.CopyCoordinate(cell[index]);
            index++;
        }
        closed[cell.Count] =
            RobustLineIntersection.CopyCoordinate(cell[0]);
        return closed;
    }

    internal static Coordinate CreateCellCoordinate(
        double x, double y, Coordinate ordinateSource)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        coordinate.Z = ordinateSource.Z;
        coordinate.M = ordinateSource.M;
        return coordinate;
    }
}
