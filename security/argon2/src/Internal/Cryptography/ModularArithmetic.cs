namespace argon2;

internal static class ModularArithmetic
{
    internal static ulong Add64(ulong left, ulong right)
    {
        ulong lowerMask = 0xFFFFFFFFUL;
        ulong lower = (left & lowerMask) + (right & lowerMask);
        ulong upper = (left >> 32) + (right >> 32) + (lower >> 32);
        return (upper << 32) | (lower & lowerMask);
    }

    internal static ulong RotateRight64(ulong value, int distance)
    {
        int normalizedDistance = distance & 63;
        int complementaryDistance = (64 - normalizedDistance) & 63;
        return (value >> normalizedDistance) | (value << complementaryDistance);
    }
}
