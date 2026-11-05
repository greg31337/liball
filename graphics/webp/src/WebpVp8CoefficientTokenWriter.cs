using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8EncodedCoefficientToken
{
    internal int CoefficientIndex;
    internal WebpVp8CoefficientToken Token;
    internal int ExtraBits;
    internal int ExtraBitCount;
    internal int SignBit;
    internal int ContextBefore;

    internal WebpVp8EncodedCoefficientToken()
    {
        CoefficientIndex = 0;
        Token = WebpVp8CoefficientToken.DctEob;
        ExtraBits = 0;
        ExtraBitCount = 0;
        SignBit = 0;
        ContextBefore = 0;
    }
}

internal static class WebpVp8CoefficientTokenWriter
{
    internal static readonly int[] CoefficientTree = new int[] { -11, 2, 0, 4, -1, 6, 8, 12, -2, 10, -3, -4, 14, 16, -5, -6, 18, 20, -7, -8, -9, -10 };
    internal static readonly int[] CoefficientBands = new int[] { 0, 1, 2, 3, 6, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7 };
    internal static readonly int[] CategoryBases = new int[] { 5, 7, 11, 19, 35, 67 };
    internal static readonly int[] CategoryBitCounts = new int[] { 1, 2, 3, 4, 5, 11 };
    internal static readonly byte[][] CategoryProbabilities = new byte[][]
    {
        new byte[] { 159 },
        new byte[] { 165, 145 },
        new byte[] { 173, 148, 140 },
        new byte[] { 176, 155, 140, 135 },
        new byte[] { 180, 157, 141, 134, 130 },
        new byte[] { 254, 254, 243, 230, 196, 177, 153, 140, 133, 130, 129 }
    };

    internal static List<WebpVp8EncodedCoefficientToken> GenerateTokens(int[] coefficients, int startCoefficient, int initialContext)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        ValidateBlock(coefficients, "coefficients");
        ValidateStartCoefficient(startCoefficient);
        ValidateContext(initialContext);

        List<WebpVp8EncodedCoefficientToken> tokens = new List<WebpVp8EncodedCoefficientToken>();
        int lastNonZero = FindLastNonZeroCoefficient(coefficients, startCoefficient);
        int context = initialContext;
        int coefficientIndex = startCoefficient;
        while (coefficientIndex <= lastNonZero)
        {
            WebpVp8EncodedCoefficientToken token = CreateToken(coefficients[coefficientIndex], coefficientIndex, context);
            tokens.Add(token);
            context = FindNextContext(AbsoluteValue(coefficients[coefficientIndex]));
            coefficientIndex = coefficientIndex + 1;
        }

        if (coefficientIndex < 16)
        {
            WebpVp8EncodedCoefficientToken eob = new WebpVp8EncodedCoefficientToken();
            eob.CoefficientIndex = coefficientIndex;
            eob.ContextBefore = context;
            eob.Token = WebpVp8CoefficientToken.DctEob;
            tokens.Add(eob);
        }

        return tokens;
    }

    internal static int WriteBlock(WebpVp8BoolWriter writer, WebpVp8CoefficientProbabilityTable probabilities, int[] coefficients, int planeType, int startCoefficient, int initialContext)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(probabilities);
        ValidatePlaneType(planeType);

        List<WebpVp8EncodedCoefficientToken> tokens = GenerateTokens(coefficients, startCoefficient, initialContext);
        bool previousCoefficientWasZero = false;
        int index = 0;
        while (index < tokens.Count)
        {
            WebpVp8EncodedCoefficientToken token = tokens[index];
            WriteToken(writer, probabilities, planeType, token.CoefficientIndex, token.ContextBefore, previousCoefficientWasZero, token);
            previousCoefficientWasZero = token.Token == WebpVp8CoefficientToken.Dct0;
            index = index + 1;
        }

        return FindLastNonZeroCoefficient(coefficients, startCoefficient) >= startCoefficient ? 1 : 0;
    }

    internal static void WriteToken(WebpVp8BoolWriter writer, WebpVp8CoefficientProbabilityTable probabilities, int planeType, int coefficientIndex, int context, bool previousCoefficientWasZero, WebpVp8EncodedCoefficientToken token)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(probabilities);
        ArgumentNullException.ThrowIfNull(token);
        ValidatePlaneType(planeType);
        ValidateCoefficientIndex(coefficientIndex);
        ValidateContext(context);

        WriteTokenTree(writer, probabilities, planeType, coefficientIndex, context, previousCoefficientWasZero, token.Token);
        WriteExtraBits(writer, token);
        if (token.Token != WebpVp8CoefficientToken.DctEob && token.Token != WebpVp8CoefficientToken.Dct0)
        {
            writer.WriteBit(token.SignBit);
        }
    }

    internal static WebpVp8EncodedCoefficientToken CreateToken(int coefficient, int coefficientIndex, int context)
    {
        WebpVp8EncodedCoefficientToken token = new WebpVp8EncodedCoefficientToken();
        token.CoefficientIndex = coefficientIndex;
        token.ContextBefore = context;
        if (coefficient < 0)
        {
            token.SignBit = 1;
        }

        int absoluteValue = AbsoluteValue(coefficient);
        token.Token = TokenForAbsoluteValue(absoluteValue);
        if (absoluteValue > 4)
        {
            int category = (int)token.Token - (int)WebpVp8CoefficientToken.DctCat1;
            token.ExtraBits = absoluteValue - CategoryBases[category];
            token.ExtraBitCount = CategoryBitCounts[category];
        }

        return token;
    }

    internal static void WriteTokenTree(WebpVp8BoolWriter writer, WebpVp8CoefficientProbabilityTable probabilities, int planeType, int coefficientIndex, int context, bool previousCoefficientWasZero, WebpVp8CoefficientToken token)
    {
        int treeIndex = previousCoefficientWasZero ? 2 : 0;
        while (treeIndex >= 0)
        {
            int left = CoefficientTree[treeIndex];
            int right = CoefficientTree[treeIndex + 1];
            int bit = 0;
            if (!BranchContainsToken(left, token))
            {
                if (!BranchContainsToken(right, token))
                {
                    throw new ArgumentException("VP8 coefficient token cannot follow previous zero state", "token");
                }

                bit = 1;
            }

            int band = CoefficientBands[coefficientIndex];
            byte probability = probabilities.GetProbability(planeType, band, context, treeIndex >> 1);
            writer.WriteBool(probability, bit);
            if (bit == 0)
            {
                treeIndex = left;
            }
            else
            {
                treeIndex = right;
            }

            if (treeIndex <= 0)
            {
                return;
            }
        }
    }

    internal static bool BranchContainsToken(int branchValue, WebpVp8CoefficientToken token)
    {
        if (branchValue <= 0)
        {
            return -branchValue == (int)token;
        }

        return NodeContainsToken(branchValue, token);
    }

    internal static bool NodeContainsToken(int treeIndex, WebpVp8CoefficientToken token)
    {
        return BranchContainsToken(CoefficientTree[treeIndex], token) || BranchContainsToken(CoefficientTree[treeIndex + 1], token);
    }

    internal static void WriteExtraBits(WebpVp8BoolWriter writer, WebpVp8EncodedCoefficientToken token)
    {
        if (token.ExtraBitCount == 0)
        {
            return;
        }

        int category = (int)token.Token - (int)WebpVp8CoefficientToken.DctCat1;
        byte[] probabilities = CategoryProbabilities[category];
        int index = 0;
        while (index < token.ExtraBitCount)
        {
            int shift = token.ExtraBitCount - index - 1;
            writer.WriteBool(probabilities[index], (token.ExtraBits >> shift) & 1);
            index = index + 1;
        }
    }

    internal static WebpVp8CoefficientToken TokenForAbsoluteValue(int absoluteValue)
    {
        if (absoluteValue == 0)
        {
            return WebpVp8CoefficientToken.Dct0;
        }

        if (absoluteValue <= 4)
        {
            return (WebpVp8CoefficientToken)absoluteValue;
        }

        int category = 0;
        while (category < CategoryBases.Length)
        {
            int maximum = CategoryBases[category] + ((1 << CategoryBitCounts[category]) - 1);
            if (absoluteValue <= maximum)
            {
                return (WebpVp8CoefficientToken)((int)WebpVp8CoefficientToken.DctCat1 + category);
            }

            category = category + 1;
        }

        throw new ArgumentOutOfRangeException("absoluteValue");
    }

    internal static int FindLastNonZeroCoefficient(int[] coefficients, int startCoefficient)
    {
        int index = 15;
        while (index >= startCoefficient)
        {
            if (coefficients[index] != 0)
            {
                return index;
            }

            index = index - 1;
        }

        return startCoefficient - 1;
    }

    internal static int AbsoluteValue(int value)
    {
        if (value < 0)
        {
            return -value;
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

    internal static void ValidateBlock(int[] block, string name)
    {
        if (block.Length != 16)
        {
            throw new ArgumentException("VP8 coefficient blocks must contain 16 values", name);
        }
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
