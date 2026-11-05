using System.Collections.Generic;

namespace avif;

internal class IccParseResult
{
    internal AvifResultCode Code;
    internal IccParsedProfile? Profile;

    internal IccParseResult(AvifResultCode code)
    {
        Code = code;
        Profile = null;
    }
}

internal class IccTag
{
    internal string Signature;
    internal int Offset;
    internal int Size;
    internal string Type;

    internal IccTag(string signature, int offset, int size, string type)
    {
        Signature = signature;
        Offset = offset;
        Size = size;
        Type = type;
    }
}

internal class IccCurveParseResult
{
    internal AvifResultCode Code;
    internal IccCurve? Curve;
    internal int Size;

    internal IccCurveParseResult(AvifResultCode code)
    {
        Code = code;
        Curve = null;
        Size = 0;
    }
}

internal class IccCurve
{
    internal int Kind;
    internal double[] Values;

    internal IccCurve(int kind, double[] values)
    {
        Kind = kind;
        Values = values;
    }

    internal double Evaluate(double value)
    {
        value = System.Math.Max(0, System.Math.Min(1, value));
        if (Kind == -1)
        {
            return value;
        }
        if (Kind == -2)
        {
            if (Values.Length == 1)
            {
                return System.Math.Pow(value, Values[0]);
            }
            double position = value * (Values.Length - 1);
            int lower = (int)System.Math.Floor(position);
            int upper = System.Math.Min(Values.Length - 1, lower + 1);
            double fraction = position - lower;
            return Values[lower] * (1 - fraction)
                + Values[upper] * fraction;
        }

        double g = Values[0];
        if (Kind == 0)
        {
            return System.Math.Pow(value, g);
        }
        double a = Values[1];
        double b = Values[2];
        if (Kind == 1)
        {
            return value >= -b / a
                ? System.Math.Pow(a * value + b, g) : 0;
        }
        double c = Values[3];
        if (Kind == 2)
        {
            return value >= -b / a
                ? System.Math.Pow(a * value + b, g) + c : c;
        }
        double d = Values[4];
        if (Kind == 3)
        {
            return value >= d
                ? System.Math.Pow(a * value + b, g) : c * value;
        }
        double e = Values[5];
        double f = Values[6];
        return value >= d
            ? System.Math.Pow(a * value + b, g) + e
            : c * value + f;
    }

    internal double Invert(double value)
    {
        double minimum = 0;
        double maximum = 1;
        int iteration = 0;
        while (iteration < 48)
        {
            double middle = (minimum + maximum) / 2;
            if (Evaluate(middle) < value)
            {
                minimum = middle;
            }
            else
            {
                maximum = middle;
            }
            iteration++;
        }
        return (minimum + maximum) / 2;
    }
}

internal class IccClut
{
    internal int InputChannels;
    internal int OutputChannels;
    internal int[] GridPoints;
    internal double[] Values;

    internal IccClut(
        int inputChannels,
        int outputChannels,
        int[] gridPoints,
        double[] values)
    {
        InputChannels = inputChannels;
        OutputChannels = outputChannels;
        GridPoints = gridPoints;
        Values = values;
    }

    internal double[]? Evaluate(double[]? input)
    {
        if (input == null || input.Length < InputChannels
            || InputChannels > 15)
        {
            return null;
        }
        int[] lower = new int[InputChannels];
        double[] fraction = new double[InputChannels];
        int channel = 0;
        while (channel < InputChannels)
        {
            double value = System.Math.Max(
                0, System.Math.Min(1, input[channel]));
            double position = value * (GridPoints[channel] - 1);
            lower[channel] = System.Math.Min(
                GridPoints[channel] - 2,
                (int)System.Math.Floor(position));
            fraction[channel] = position - lower[channel];
            channel++;
        }

        double[] output = new double[OutputChannels];
        int cornerCount = 1 << InputChannels;
        int corner = 0;
        while (corner < cornerCount)
        {
            double weight = 1;
            int entry = 0;
            channel = 0;
            while (channel < InputChannels)
            {
                bool upper = (corner & (1 << channel)) != 0;
                int coordinate = lower[channel] + (upper ? 1 : 0);
                weight *= upper
                    ? fraction[channel] : 1 - fraction[channel];
                entry = entry * GridPoints[channel] + coordinate;
                channel++;
            }
            int outputChannel = 0;
            while (outputChannel < OutputChannels)
            {
                output[outputChannel] += weight
                    * Values[entry * OutputChannels + outputChannel];
                outputChannel++;
            }
            corner++;
        }
        return output;
    }
}

internal enum IccPipelineKind
{
    Lut = 0,
    AToB = 1,
    BToA = 2
}

internal class IccPipeline
{
    internal IccPipelineKind Kind;
    internal int InputChannels;
    internal int OutputChannels;
    internal IccCurve[]? InputCurves;
    internal IccCurve[]? MiddleCurves;
    internal IccCurve[]? OutputCurves;
    internal double[]? Matrix;
    internal IccClut? Clut;

    internal IccPipeline(IccPipelineKind kind)
    {
        Kind = kind;
        InputChannels = 0;
        OutputChannels = 0;
        InputCurves = null;
        MiddleCurves = null;
        OutputCurves = null;
        Matrix = null;
        Clut = null;
    }

    internal double[]? Evaluate(double[] input)
    {
        if (input == null || input.Length < InputChannels)
        {
            return null;
        }
        double[]? value = CopyChannels(input, InputChannels);
        if (Kind == IccPipelineKind.Lut)
        {
            value = ApplyCurves(value, InputCurves);
            value = ApplyMatrix(value, Matrix);
            value = Clut == null ? value : Clut.Evaluate(value);
            return ApplyCurves(value, OutputCurves);
        }
        if (Kind == IccPipelineKind.AToB)
        {
            value = ApplyCurves(value, InputCurves);
            value = Clut == null ? value : Clut.Evaluate(value);
            value = ApplyCurves(value, MiddleCurves);
            value = ApplyMatrix(value, Matrix);
            return ApplyCurves(value, OutputCurves);
        }

        value = ApplyCurves(value, InputCurves);
        value = ApplyMatrix(value, Matrix);
        value = ApplyCurves(value, MiddleCurves);
        value = Clut == null ? value : Clut.Evaluate(value);
        return ApplyCurves(value, OutputCurves);
    }

    static double[]? ApplyCurves(double[]? values, IccCurve[]? curves)
    {
        if (values == null || curves == null)
        {
            return values;
        }
        if (values.Length < curves.Length)
        {
            return null;
        }
        double[] result = new double[curves.Length];
        int index = 0;
        while (index < curves.Length)
        {
            result[index] = curves[index].Evaluate(values[index]);
            index++;
        }
        return result;
    }

    static double[]? ApplyMatrix(double[]? values, double[]? matrix)
    {
        if (matrix == null)
        {
            return values;
        }
        if (values == null || values.Length < 3)
        {
            return null;
        }
        return new double[]
        {
            matrix[0] * values[0] + matrix[1] * values[1]
                + matrix[2] * values[2] + matrix[9],
            matrix[3] * values[0] + matrix[4] * values[1]
                + matrix[5] * values[2] + matrix[10],
            matrix[6] * values[0] + matrix[7] * values[1]
                + matrix[8] * values[2] + matrix[11]
        };
    }

    static double[] CopyChannels(double[] values, int count)
    {
        double[] copy = new double[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = values[index];
            index++;
        }
        return copy;
    }
}

internal class IccParsedProfile
{
    internal byte[] Data;
    internal int VersionMajor;
    internal int VersionMinor;
    internal string DeviceClass;
    internal string ColorSpace;
    internal string Pcs;
    internal AvifRenderingIntent Intent;
    internal List<IccTag> Tags;
    internal IccPipeline?[] AToB;
    internal IccPipeline?[] BToA;
    internal IccCurve[]? MatrixCurves;
    internal double[]? Matrix;
    internal double[]? MediaWhite;
    internal double[]? ChromaticAdaptation;

    internal IccParsedProfile()
    {
        Data = System.Array.Empty<byte>();
        DeviceClass = string.Empty;
        ColorSpace = string.Empty;
        Pcs = string.Empty;
        Tags = new List<IccTag>();
        AToB = new IccPipeline[3];
        BToA = new IccPipeline[3];
        MatrixCurves = null;
        Matrix = null;
        MediaWhite = new double[] { 0.9642, 1, 0.8249 };
        ChromaticAdaptation = null;
    }
}
