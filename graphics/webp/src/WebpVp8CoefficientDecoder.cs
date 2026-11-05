using System;

namespace webp;

internal enum WebpVp8CoefficientToken
{
    Dct0 = 0,
    Dct1 = 1,
    Dct2 = 2,
    Dct3 = 3,
    Dct4 = 4,
    DctCat1 = 5,
    DctCat2 = 6,
    DctCat3 = 7,
    DctCat4 = 8,
    DctCat5 = 9,
    DctCat6 = 10,
    DctEob = 11
}

internal class WebpVp8CoefficientProbabilityTable
{
    internal byte[] Values;

    internal WebpVp8CoefficientProbabilityTable()
    {
        Values = new byte[WebpVp8CoefficientDecoder.ProbabilityTableSize];
        int index = 0;
        while (index < Values.Length)
        {
            Values[index] = 128;
            index = index + 1;
        }
    }

    internal byte GetProbability(int planeType, int coefficientBand, int context, int tokenTreeIndex)
    {
        return Values[WebpVp8CoefficientDecoder.ProbabilityIndex(planeType, coefficientBand, context, tokenTreeIndex)];
    }

    internal void SetProbability(int planeType, int coefficientBand, int context, int tokenTreeIndex, byte probability)
    {
        if (probability == 0)
        {
            throw new ArgumentOutOfRangeException("probability");
        }

        Values[WebpVp8CoefficientDecoder.ProbabilityIndex(planeType, coefficientBand, context, tokenTreeIndex)] = probability;
    }
}

internal class WebpVp8CoefficientBlock
{
    internal int[] Coefficients;
    internal bool HasCoefficients;
    internal int LastDecodedCoefficient;
    internal int EndingContext;

    internal WebpVp8CoefficientBlock()
    {
        Coefficients = new int[16];
        HasCoefficients = false;
        LastDecodedCoefficient = -1;
        EndingContext = 0;
    }
}

internal static class WebpVp8CoefficientDecoder
{
    internal const int PlaneYWithY2 = 0;
    internal const int PlaneY2 = 1;
    internal const int PlaneUv = 2;
    internal const int PlaneYWithoutY2 = 3;
    internal const int CoefficientBandCount = 8;
    internal const int ContextCount = 3;
    internal const int TokenProbabilityCount = 11;
    internal const int ProbabilityTableSize = 4 * CoefficientBandCount * ContextCount * TokenProbabilityCount;

    internal static readonly int[] CoefficientTree = new int[] { -11, 2, 0, 4, -1, 6, 8, 12, -2, 10, -3, -4, 14, 16, -5, -6, 18, 20, -7, -8, -9, -10 };
    internal static readonly int[] CoefficientBands = new int[] { 0, 1, 2, 3, 6, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7 };
    internal static readonly int[] CategoryBases = new int[] { 5, 7, 11, 19, 35, 67 };
    internal static readonly byte[][] CategoryProbabilities = new byte[][]
    {
        new byte[] { 159 },
        new byte[] { 165, 145 },
        new byte[] { 173, 148, 140 },
        new byte[] { 176, 155, 140, 135 },
        new byte[] { 180, 157, 141, 134, 130 },
        new byte[] { 254, 254, 243, 230, 196, 177, 153, 140, 133, 130, 129 }
    };

    internal static WebpVp8CoefficientBlock DecodeBlock(WebpVp8BoolDecoder decoder, WebpVp8CoefficientProbabilityTable probabilities, int planeType, int startCoefficient, int initialContext)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        ArgumentNullException.ThrowIfNull(probabilities);
        ValidatePlaneType(planeType);
        ValidateStartCoefficient(startCoefficient);
        ValidateContext(initialContext);

        WebpVp8CoefficientBlock block = new WebpVp8CoefficientBlock();
        int context = initialContext;
        bool previousCoefficientWasZero = false;
        int coefficientIndex = startCoefficient;
        while (coefficientIndex < 16)
        {
            WebpVp8CoefficientToken token = DecodeToken(decoder, probabilities, planeType, coefficientIndex, context, previousCoefficientWasZero);
            if (token == WebpVp8CoefficientToken.DctEob)
            {
                break;
            }

            int absoluteValue = DecodeAbsoluteValue(decoder, token);
            if (absoluteValue != 0)
            {
                block.HasCoefficients = true;
                if (decoder.DecodeBit() != 0)
                {
                    block.Coefficients[coefficientIndex] = -absoluteValue;
                }
                else
                {
                    block.Coefficients[coefficientIndex] = absoluteValue;
                }
            }

            context = FindNextContext(absoluteValue);
            previousCoefficientWasZero = token == WebpVp8CoefficientToken.Dct0;
            block.LastDecodedCoefficient = coefficientIndex;
            coefficientIndex = coefficientIndex + 1;
        }

        block.EndingContext = block.HasCoefficients ? 1 : 0;
        return block;
    }

    internal static WebpVp8CoefficientToken DecodeToken(WebpVp8BoolDecoder decoder, WebpVp8CoefficientProbabilityTable probabilities, int planeType, int coefficientIndex, int context, bool previousCoefficientWasZero)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        ArgumentNullException.ThrowIfNull(probabilities);
        ValidatePlaneType(planeType);
        ValidateCoefficientIndex(coefficientIndex);
        ValidateContext(context);

        int band = CoefficientBands[coefficientIndex];
        int treeIndex = previousCoefficientWasZero ? 2 : 0;
        while (true)
        {
            int tokenTreeIndex = treeIndex >> 1;
            byte probability = probabilities.GetProbability(planeType, band, context, tokenTreeIndex);
            int bit = decoder.DecodeBool(probability);
            treeIndex = CoefficientTree[treeIndex + bit];
            if (treeIndex <= 0)
            {
                return (WebpVp8CoefficientToken)(-treeIndex);
            }
        }
    }

    internal static int DecodeAbsoluteValue(WebpVp8BoolDecoder decoder, WebpVp8CoefficientToken token)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        if (token == WebpVp8CoefficientToken.DctEob)
        {
            return 0;
        }

        if (token == WebpVp8CoefficientToken.Dct0)
        {
            return 0;
        }

        if (token == WebpVp8CoefficientToken.Dct1)
        {
            return 1;
        }

        if (token == WebpVp8CoefficientToken.Dct2)
        {
            return 2;
        }

        if (token == WebpVp8CoefficientToken.Dct3)
        {
            return 3;
        }

        if (token == WebpVp8CoefficientToken.Dct4)
        {
            return 4;
        }

        int category = (int)token - (int)WebpVp8CoefficientToken.DctCat1;
        return CategoryBases[category] + DecodeExtraBits(decoder, CategoryProbabilities[category]);
    }

    internal static int ProbabilityIndex(int planeType, int coefficientBand, int context, int tokenTreeIndex)
    {
        ValidatePlaneType(planeType);
        if (coefficientBand < 0 || coefficientBand >= CoefficientBandCount)
        {
            throw new ArgumentOutOfRangeException("coefficientBand");
        }

        ValidateContext(context);
        if (tokenTreeIndex < 0 || tokenTreeIndex >= TokenProbabilityCount)
        {
            throw new ArgumentOutOfRangeException("tokenTreeIndex");
        }

        return (((planeType * CoefficientBandCount) + coefficientBand) * ContextCount + context) * TokenProbabilityCount + tokenTreeIndex;
    }

    internal static int DecodeExtraBits(WebpVp8BoolDecoder decoder, byte[] probabilities)
    {
        int value = 0;
        int index = 0;
        while (index < probabilities.Length)
        {
            value = (value << 1) + decoder.DecodeBool(probabilities[index]);
            index = index + 1;
        }

        return value;
    }

    internal static int FindNextContext(int absoluteValue)
    {
        if (absoluteValue == 0)
        {
            return 0;
        }

        if (absoluteValue == 1)
        {
            return 1;
        }

        return 2;
    }

    internal static void ValidatePlaneType(int planeType)
    {
        if (planeType < 0 || planeType > 3)
        {
            throw new ArgumentOutOfRangeException("planeType");
        }
    }

    internal static void ValidateStartCoefficient(int startCoefficient)
    {
        if (startCoefficient < 0 || startCoefficient > 15)
        {
            throw new ArgumentOutOfRangeException("startCoefficient");
        }
    }

    internal static void ValidateCoefficientIndex(int coefficientIndex)
    {
        if (coefficientIndex < 0 || coefficientIndex > 15)
        {
            throw new ArgumentOutOfRangeException("coefficientIndex");
        }
    }

    internal static void ValidateContext(int context)
    {
        if (context < 0 || context > 2)
        {
            throw new ArgumentOutOfRangeException("context");
        }
    }
}
