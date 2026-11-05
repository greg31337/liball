using System;

namespace geo;

internal static class RingMath
{
    internal static double GetSignedArea(Coordinate[] coordinates)
    {
        ValidateRingCoordinates(coordinates);
        if (coordinates.Length < 3) return 0.0;
        Coordinate origin = coordinates[0];
        double sum = 0.0;
        double compensation = 0.0;
        int index = 1;
        while (index < coordinates.Length - 1)
        {
            double firstX = coordinates[index].X - origin.X;
            double firstY = coordinates[index].Y - origin.Y;
            double secondX = coordinates[index + 1].X - origin.X;
            double secondY = coordinates[index + 1].Y - origin.Y;
            double term = firstX * secondY - secondX * firstY;
            double adjusted = term - compensation;
            double next = sum + adjusted;
            compensation = next - sum - adjusted;
            sum = next;
            index++;
        }
        return sum / 2.0;
    }

    internal static RingOrientation GetRingOrientation(Coordinate[] coordinates)
    {
        int sign = GetExactSignedAreaSign(coordinates);
        if (sign > 0) return RingOrientation.CounterClockwise;
        if (sign < 0) return RingOrientation.Clockwise;
        return RingOrientation.Undefined;
    }

    internal static int GetExactSignedAreaSign(Coordinate[] coordinates)
    {
        ValidateRingCoordinates(coordinates);
        if (coordinates.Length < 3) return 0;
        BinaryProduct[] products = new BinaryProduct[coordinates.Length * 2];
        int[] coefficients = new int[products.Length];
        Coordinate previous = coordinates[coordinates.Length - 1];
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate current = coordinates[index];
            int productIndex = index * 2;
            products[productIndex] = RobustDeterminant.CreateBinaryProduct(
                previous.X, current.Y);
            coefficients[productIndex] = 1;
            products[productIndex + 1] = RobustDeterminant.CreateBinaryProduct(
                previous.Y, current.X);
            coefficients[productIndex + 1] = -1;
            previous = current;
            index++;
        }
        ExactDyadicValue area = ExactDyadicArithmetic.CreateProductSum(
            products, coefficients);
        return area.Sign;
    }

    internal static void ValidateRingCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Ring coordinate sequence is null.");
        }
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(coordinates[index], "Ring coordinate");
            index++;
        }
    }
}
