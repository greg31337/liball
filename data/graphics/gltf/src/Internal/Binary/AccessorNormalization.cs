using System;

namespace gltf;

internal class AccessorNormalization
{
    internal double[] DecodeSignedValues(
        ReadOnlySpan<int> source,
        GltfComponentType componentType)
    {
        GetSignedMaximum(componentType);
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = DecodeSignedValue(source[index], componentType);
            index++;
        }

        return result;
    }

    internal double[] DecodeUnsignedValues(
        ReadOnlySpan<uint> source,
        GltfComponentType componentType)
    {
        GetUnsignedMaximum(componentType);
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = DecodeUnsignedValue(source[index], componentType);
            index++;
        }

        return result;
    }

    internal int[] EncodeSignedValues(
        ReadOnlySpan<double> source,
        GltfComponentType componentType)
    {
        GetSignedMaximum(componentType);
        int[] result = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = EncodeSignedValue(source[index], componentType);
            index++;
        }

        return result;
    }

    internal uint[] EncodeUnsignedValues(
        ReadOnlySpan<double> source,
        GltfComponentType componentType)
    {
        GetUnsignedMaximum(componentType);
        uint[] result = new uint[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = EncodeUnsignedValue(source[index], componentType);
            index++;
        }

        return result;
    }

    internal double DecodeSignedValue(
        int value,
        GltfComponentType componentType)
    {
        int maximum = GetSignedMaximum(componentType);
        int minimum = GetSignedMinimum(componentType);
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                "value",
                $"Value {value} is outside the {componentType} component range {minimum} through {maximum}.");
        }

        double result = (double)value / maximum;
        if (result < -1.0)
        {
            return -1.0;
        }

        return result;
    }

    internal double DecodeUnsignedValue(
        uint value,
        GltfComponentType componentType)
    {
        uint maximum = GetUnsignedMaximum(componentType);
        if (value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                "value",
                $"Value {value} is outside the {componentType} component range 0 through {maximum}.");
        }

        return (double)value / maximum;
    }

    internal int EncodeSignedValue(
        double value,
        GltfComponentType componentType)
    {
        int maximum = GetSignedMaximum(componentType);
        ValidateSignedNormalizedValue(value);
        double scaled = value * maximum;
        double rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);
        return (int)rounded;
    }

    internal uint EncodeUnsignedValue(
        double value,
        GltfComponentType componentType)
    {
        uint maximum = GetUnsignedMaximum(componentType);
        ValidateUnsignedNormalizedValue(value);
        double scaled = value * maximum;
        double rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);
        return (uint)rounded;
    }

    internal int GetSignedMaximum(GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                return 127;
            case GltfComponentType.SignedShort:
                return 32767;
            default:
                throw new ArgumentException(
                    "Signed normalization requires signed byte or signed short components.",
                    "componentType");
        }
    }

    internal int GetSignedMinimum(GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.SignedByte:
                return -128;
            case GltfComponentType.SignedShort:
                return -32768;
            default:
                throw new ArgumentException(
                    "Signed normalization requires signed byte or signed short components.",
                    "componentType");
        }
    }

    internal uint GetUnsignedMaximum(GltfComponentType componentType)
    {
        switch (componentType)
        {
            case GltfComponentType.UnsignedByte:
                return 255U;
            case GltfComponentType.UnsignedShort:
                return 65535U;
            default:
                throw new ArgumentException(
                    "Unsigned normalization requires unsigned byte or unsigned short components.",
                    "componentType");
        }
    }

    internal void ValidateSignedNormalizedValue(double value)
    {
        if (!double.IsFinite(value) || value < -1.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                "value",
                "A signed normalized value must be finite and between -1 and 1 inclusive.");
        }
    }

    internal void ValidateUnsignedNormalizedValue(double value)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                "value",
                "An unsigned normalized value must be finite and between 0 and 1 inclusive.");
        }
    }
}
