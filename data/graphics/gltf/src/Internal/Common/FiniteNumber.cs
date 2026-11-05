using System;

namespace gltf;

internal static class FiniteNumber
{
    internal static bool IsFiniteSingle(float value)
    {
        return float.IsFinite(value);
    }

    internal static bool IsFiniteDouble(double value)
    {
        return double.IsFinite(value);
    }

    internal static float RequireFiniteSingle(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "The value must be finite.");
        }

        return value;
    }

    internal static double RequireFiniteDouble(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "The value must be finite.");
        }

        return value;
    }
}
