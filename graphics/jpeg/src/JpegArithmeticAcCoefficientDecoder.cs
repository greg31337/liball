using System.Collections.Generic;

namespace jpeg;

internal class JpegArithmeticAcCoefficientDecoder
{
    internal JpegArithmeticDecoderStateMachine StateMachine = new JpegArithmeticDecoderStateMachine(new byte[0]);
    internal int[][] AcStatistics = new int[4][];
    internal int[] FixedStatistics = new int[] { 113 };
    internal int[] DecisionBits = new int[0];
    internal int DecisionIndex;
    internal bool UsesDecisionBits;

    internal JpegArithmeticAcCoefficientDecoder(byte[] entropyData)
    {
        StateMachine = new JpegArithmeticDecoderStateMachine(entropyData);
        Initialize();
    }

    internal static JpegArithmeticAcCoefficientDecoder FromStream(System.IO.Stream stream)
    {
        return FromStateMachine(JpegArithmeticDecoderStateMachine.FromStream(stream));
    }

    internal static JpegArithmeticAcCoefficientDecoder FromStateMachine(JpegArithmeticDecoderStateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }

        JpegArithmeticAcCoefficientDecoder decoder = new JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.StateMachine = stateMachine;
        decoder.Initialize();
        return decoder;
    }

    internal void Initialize()
    {
        int index = 0;
        while (index < AcStatistics.Length)
        {
            AcStatistics[index] = new int[256];
            index = index + 1;
        }

        FixedStatistics = new int[] { 113 };
        DecisionBits = new int[0];
        DecisionIndex = 0;
        UsesDecisionBits = false;
    }

    internal void DecodeAcFirstBlock(int[] coefficients, JpegArithmeticConditioningTable conditioningTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        ValidateAcConditioningTable(conditioningTable);

        int[] statistics = AcStatistics[conditioningTable.Identifier];
        int coefficientIndex = spectralStart;
        while (coefficientIndex <= spectralEnd)
        {
            int context = 3 * (coefficientIndex - 1);
            if (DecodeDecision(statistics, context) != 0)
            {
                return;
            }

            while (DecodeDecision(statistics, context + 1) == 0)
            {
                context = context + 3;
                coefficientIndex = coefficientIndex + 1;
                if (coefficientIndex > spectralEnd)
                {
                    throw JpegInvalidDataException.Create("Arithmetic AC first zero run exceeded the scan spectral range.");
                }
            }

            int sign = DecodeDecision(FixedStatistics, 0);
            context = context + 2;
            int magnitude = DecodeAcMagnitudeCategory(statistics, context, coefficientIndex, conditioningTable.Kx);
            int value = DecodeMagnitudeBits(statistics, context, magnitude);
            if (sign == 1)
            {
                value = -value;
            }

            coefficients[coefficientIndex] = value << successiveLow;
            coefficientIndex = coefficientIndex + 1;
        }
    }

    internal void DecodeAcFirstBlockForScan(JpegScanHeader scan, JpegArithmeticConditioningTable conditioningTable, int[] coefficients)
    {
        ValidateAcFirstScan(scan);
        DecodeAcFirstBlock(coefficients, conditioningTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal void DecodeAcRefinementBlock(int[] coefficients, JpegArithmeticConditioningTable conditioningTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        ValidateAcConditioningTable(conditioningTable);

        int[] statistics = AcStatistics[conditioningTable.Identifier];
        int bitValue = 1 << successiveLow;
        int lastNonzero = FindLastNonzeroCoefficient(coefficients, spectralEnd);
        int coefficientIndex = spectralStart;
        while (coefficientIndex <= spectralEnd)
        {
            int context = 3 * (coefficientIndex - 1);
            if (coefficientIndex > lastNonzero)
            {
                if (DecodeDecision(statistics, context) != 0)
                {
                    return;
                }
            }

            bool coefficientHandled = false;
            while (!coefficientHandled)
            {
                if (coefficients[coefficientIndex] != 0)
                {
                    if (DecodeDecision(statistics, context + 2) != 0)
                    {
                        RefineCoefficient(coefficients, coefficientIndex, bitValue);
                    }

                    coefficientHandled = true;
                }
                else if (DecodeDecision(statistics, context + 1) != 0)
                {
                    if (DecodeDecision(FixedStatistics, 0) != 0)
                    {
                        coefficients[coefficientIndex] = -bitValue;
                    }
                    else
                    {
                        coefficients[coefficientIndex] = bitValue;
                    }

                    coefficientHandled = true;
                }
                else
                {
                    context = context + 3;
                    coefficientIndex = coefficientIndex + 1;
                    if (coefficientIndex > spectralEnd)
                    {
                        throw JpegInvalidDataException.Create("Arithmetic AC refinement zero run exceeded the scan spectral range.");
                    }
                }
            }

            coefficientIndex = coefficientIndex + 1;
        }
    }

    internal void DecodeAcRefinementBlockForScan(JpegScanHeader scan, JpegArithmeticConditioningTable conditioningTable, int[] coefficients)
    {
        ValidateAcRefinementScan(scan);
        DecodeAcRefinementBlock(coefficients, conditioningTable, scan.SpectralStart, scan.SpectralEnd, scan.SuccessiveLow);
    }

    internal int DecodeAcMagnitudeCategory(int[] statistics, int context, int coefficientIndex, int conditioningKx)
    {
        int magnitude = DecodeDecision(statistics, context);
        if (magnitude != 0)
        {
            if (DecodeDecision(statistics, context) != 0)
            {
                magnitude = magnitude << 1;
                if (coefficientIndex <= conditioningKx)
                {
                    context = 189;
                }
                else
                {
                    context = 217;
                }

                while (DecodeDecision(statistics, context) != 0)
                {
                    magnitude = magnitude << 1;
                    if (magnitude == 0x8000)
                    {
                        throw JpegInvalidDataException.Create("Arithmetic AC magnitude category overflowed.");
                    }

                    context = context + 1;
                }
            }
        }

        return magnitude;
    }

    internal int DecodeMagnitudeBits(int[] statistics, int context, int magnitude)
    {
        int value = magnitude;
        context = context + 14;
        magnitude = magnitude >> 1;
        while (magnitude > 0)
        {
            if (DecodeDecision(statistics, context) != 0)
            {
                value = value | magnitude;
            }

            magnitude = magnitude >> 1;
        }

        return value + 1;
    }

    internal int FindLastNonzeroCoefficient(int[] coefficients, int spectralEnd)
    {
        int index = spectralEnd;
        while (index > 0)
        {
            if (coefficients[index] != 0)
            {
                return index;
            }

            index = index - 1;
        }

        return 0;
    }

    internal void RefineCoefficient(int[] coefficients, int index, int bitValue)
    {
        if (coefficients[index] < 0)
        {
            coefficients[index] = coefficients[index] - bitValue;
        }
        else
        {
            coefficients[index] = coefficients[index] + bitValue;
        }
    }

    internal int DecodeDecision(int[] statistics, int contextIndex)
    {
        StateMachine.ValidateContext(statistics, contextIndex);
        if (UsesDecisionBits)
        {
            if (DecisionIndex >= DecisionBits.Length)
            {
                throw JpegInvalidDataException.Create("Scripted arithmetic decision bits ended before decoding completed.");
            }

            int bit = DecisionBits[DecisionIndex];
            if (bit != 0 && bit != 1)
            {
                throw JpegInvalidDataException.Create("Scripted arithmetic decision bits must be 0 or 1.");
            }

            DecisionIndex = DecisionIndex + 1;
            return bit;
        }

        return StateMachine.DecodeBit(statistics, contextIndex);
    }

    internal void SetDecisionBits(int[] decisionBits)
    {
        if (decisionBits == null)
        {
            throw new System.ArgumentNullException("decisionBits");
        }

        DecisionBits = decisionBits;
        DecisionIndex = 0;
        UsesDecisionBits = true;
    }

    internal void ClearDecisionBits()
    {
        DecisionBits = new int[0];
        DecisionIndex = 0;
        UsesDecisionBits = false;
    }

    internal JpegArithmeticConditioningTable CreateDefaultAcConditioningTable(int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw new System.ArgumentOutOfRangeException("identifier");
        }

        JpegArithmeticConditioningTable table = new JpegArithmeticConditioningTable();
        table.TableClass = 1;
        table.Identifier = identifier;
        table.Kx = 5;
        table.ConditioningValue = 5;
        return table;
    }

    internal JpegArithmeticConditioningTable FindOrCreateDefaultAcTable(List<JpegArithmeticConditioningTable>? tables, int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw new System.ArgumentOutOfRangeException("identifier");
        }

        if (tables != null)
        {
            foreach (JpegArithmeticConditioningTable table in tables)
            {
                if (table.TableClass == 1 && table.Identifier == identifier)
                {
                    return table;
                }
            }
        }

        return CreateDefaultAcConditioningTable(identifier);
    }

    internal void ResetAcState()
    {
        int index = 0;
        while (index < AcStatistics.Length)
        {
            StateMachine.ResetStatistics(AcStatistics[index]);
            index = index + 1;
        }

        FixedStatistics[0] = 113;
    }

    internal void ValidateAcConditioningTable(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 1)
        {
            throw JpegInvalidDataException.Create("Arithmetic conditioning table class does not match AC decoding.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegInvalidDataException.Create("Arithmetic conditioning table identifier must be between 0 and 3.");
        }

        if (table.Kx < 1 || table.Kx > 63)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC conditioning value must be between 1 and 63.");
        }
    }

    internal void ValidateAcFirstScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        ValidateAcRange(scan.SpectralStart, scan.SpectralEnd);
        if (scan.SuccessiveHigh != 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC first scans must have successive high approximation 0.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC scans must contain exactly one component.");
        }
    }

    internal void ValidateAcRefinementScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        ValidateAcRange(scan.SpectralStart, scan.SpectralEnd);
        if (scan.SuccessiveHigh <= 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC refinement scans must decrease successive approximation by one bit.");
        }

        if (GetScanComponentCount(scan) != 1)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC scans must contain exactly one component.");
        }
    }

    internal int GetScanComponentCount(JpegScanHeader scan)
    {
        if (scan.ComponentCount != 0)
        {
            return scan.ComponentCount;
        }

        if (scan.Components == null)
        {
            return 0;
        }

        return scan.Components.Count;
    }

    internal void ValidateCoefficientBlock(int[] coefficients)
    {
        if (coefficients == null)
        {
            throw new System.ArgumentNullException("coefficients");
        }

        if (coefficients.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateAcRange(int spectralStart, int spectralEnd)
    {
        if (spectralStart < 1 || spectralStart > 63)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC scans must start between coefficients 1 and 63.");
        }

        if (spectralEnd < spectralStart || spectralEnd > 63)
        {
            throw JpegInvalidDataException.Create("Arithmetic AC scan spectral end is invalid.");
        }
    }

    internal void ValidateSuccessiveLow(int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw JpegInvalidDataException.Create("Arithmetic successive low approximation must be between 0 and 15.");
        }
    }
}
