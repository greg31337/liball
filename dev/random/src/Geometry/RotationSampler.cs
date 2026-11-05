using System;

namespace random;

/// <summary>
/// Provides uniform three-dimensional unit quaternions and rotation matrices.
/// </summary>
internal static class RotationSampler
{
    const double Tau = 6.283185307179586476925286766559005768;

    /// <summary>Returns a uniform unit quaternion representing a three-dimensional rotation.</summary>
    /// <param name="generator">The generator that supplies three independent unit values.</param>
    /// <returns>Four components in <c>x, y, z, w</c> order.</returns>
    /// <remarks>The quaternion is uniform on the unit 3-sphere; opposite quaternions represent the same rotation.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] Quaternion(IRandomSource generator)
    {
        double[] result = new double[4];
        Quaternion(generator, result);
        return result;
    }

    /// <summary>Writes a uniform unit quaternion representing a three-dimensional rotation.</summary>
    /// <param name="generator">The generator that supplies three independent unit values.</param>
    /// <param name="destination">The destination of exactly four components in <c>x, y, z, w</c> order.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is not four.</exception>
    public static void Quaternion(IRandomSource generator, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.ExactSpanLength(destination.Length, 4, "destination");
        double radialUnit = FloatingUniform.NextDoubleClosedOpen(generator);
        double firstAngle = Tau * FloatingUniform.NextDoubleClosedOpen(generator);
        double secondAngle = Tau * FloatingUniform.NextDoubleClosedOpen(generator);
        double firstRadius = Sqrt(generator, 1.0 - radialUnit);
        double secondRadius = Sqrt(generator, radialUnit);
        destination[0] = firstRadius * Sin(generator, firstAngle);
        destination[1] = firstRadius * Cos(generator, firstAngle);
        destination[2] = secondRadius * Sin(generator, secondAngle);
        destination[3] = secondRadius * Cos(generator, secondAngle);
        NormalizeQuaternion(generator, destination);
    }

    /// <summary>Returns a uniform three-dimensional rotation matrix.</summary>
    /// <param name="generator">The generator that supplies a uniform unit quaternion.</param>
    /// <returns>A row-major orthogonal <c>3 * 3</c> matrix with determinant positive one.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] Rotation3D(IRandomSource generator)
    {
        double[] result = new double[9];
        Rotation3D(generator, result);
        return result;
    }

    /// <summary>Writes a uniform three-dimensional rotation matrix.</summary>
    /// <param name="generator">The generator that supplies a uniform unit quaternion.</param>
    /// <param name="destination">The destination of exactly nine row-major matrix entries.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is not nine.</exception>
    public static void Rotation3D(IRandomSource generator, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.ExactSpanLength(destination.Length, 9, "destination");
        double[] quaternion = new double[4];
        Quaternion(generator, quaternion);
        QuaternionMatrix(quaternion, destination);
    }

    static void NormalizeQuaternion(IRandomSource generator, Span<double> quaternion)
    {
        double squaredNorm = 0.0;
        int index = 0;
        while (index < quaternion.Length)
        {
            squaredNorm += quaternion[index] * quaternion[index];
            index++;
        }

        double inverseNorm = 1.0 / Sqrt(generator, squaredNorm);
        index = 0;
        while (index < quaternion.Length)
        {
            quaternion[index] *= inverseNorm;
            index++;
        }
    }

    static void QuaternionMatrix(double[] quaternion, Span<double> destination)
    {
        double x = quaternion[0];
        double y = quaternion[1];
        double z = quaternion[2];
        double w = quaternion[3];
        double xx = x * x;
        double yy = y * y;
        double zz = z * z;
        double xy = x * y;
        double xz = x * z;
        double yz = y * z;
        double xw = x * w;
        double yw = y * w;
        double zw = z * w;
        destination[0] = 1.0 - 2.0 * (yy + zz);
        destination[1] = 2.0 * (xy - zw);
        destination[2] = 2.0 * (xz + yw);
        destination[3] = 2.0 * (xy + zw);
        destination[4] = 1.0 - 2.0 * (xx + zz);
        destination[5] = 2.0 * (yz - xw);
        destination[6] = 2.0 * (xz - yw);
        destination[7] = 2.0 * (yz + xw);
        destination[8] = 1.0 - 2.0 * (xx + yy);
    }

    static double Sqrt(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    static double Sin(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sin(value);
        }

        return Math.Sin(value);
    }

    static double Cos(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Cos(value);
        }

        return Math.Cos(value);
    }
}
