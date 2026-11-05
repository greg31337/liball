using System;
using System.Globalization;

namespace geo;

internal class PlanarNodeKey
{
    internal double X;
    internal double Y;
    internal long XBits;
    internal long YBits;
    internal string Identity;

    internal PlanarNodeKey(Coordinate coordinate)
    {
        VectorMath.ValidateCoordinateValue(coordinate, "Planar node key coordinate");
        X = coordinate.X == 0.0 ? 0.0 : coordinate.X;
        Y = coordinate.Y == 0.0 ? 0.0 : coordinate.Y;
        XBits = BitConverter.DoubleToInt64Bits(X);
        YBits = BitConverter.DoubleToInt64Bits(Y);
        Identity = XBits.ToString(CultureInfo.InvariantCulture) + ":" +
            YBits.ToString(CultureInfo.InvariantCulture);
    }

    internal static int CompareKeys(PlanarNodeKey first, PlanarNodeKey second)
    {
        if (first.X < second.X) return -1;
        if (first.X > second.X) return 1;
        if (first.Y < second.Y) return -1;
        if (first.Y > second.Y) return 1;
        return 0;
    }
}

internal static class OrdinateReconciler
{
    internal static double ReconcileOptionalOrdinate(double first, double second)
    {
        if (double.IsNaN(first) || double.IsNaN(second)) return double.NaN;
        if (first != second) return double.NaN;
        return first == 0.0 ? 0.0 : first;
    }

    internal static Coordinate ReconcileCoordinates(
        Coordinate first, Coordinate second)
    {
        VectorMath.ValidateCoordinateValue(first, "First reconciled coordinate");
        VectorMath.ValidateCoordinateValue(second, "Second reconciled coordinate");
        if (!RobustLineIntersection.CoordinatesEqualPlanar(first, second))
        {
            throw new InvalidGeometryException(
                "Only coordinates with equal X/Y values can be reconciled.");
        }
        Coordinate result = RobustLineIntersection.CopyCoordinate(first);
        if (result.X == 0.0) result.X = 0.0;
        if (result.Y == 0.0) result.Y = 0.0;
        result.Z = ReconcileOptionalOrdinate(first.Z, second.Z);
        result.M = ReconcileOptionalOrdinate(first.M, second.M);
        return result;
    }

    internal static void ReconcileCoordinateInto(
        Coordinate target, Coordinate contribution)
    {
        Coordinate reconciled = ReconcileCoordinates(target, contribution);
        target.X = reconciled.X;
        target.Y = reconciled.Y;
        target.Z = reconciled.Z;
        target.M = reconciled.M;
    }
}
