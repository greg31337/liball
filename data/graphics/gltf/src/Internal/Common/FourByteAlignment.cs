using System;

namespace gltf;

internal static class FourByteAlignment
{
    internal static bool IsAligned(int value)
    {
        if (value < 0)
        {
            return false;
        }

        return (value & 3) == 0;
    }

    internal static int GetPadding(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        return (4 - (value & 3)) & 3;
    }

    internal static int Align(int value)
    {
        int padding = GetPadding(value);
        return CheckedArithmetic.AddNonNegativeInt32(value, padding);
    }
}
