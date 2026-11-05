using System.Collections.Generic;

namespace jpeg;

internal class JpegArithmeticDcCoefficientDecoder
{
    internal JpegArithmeticDecoderStateMachine StateMachine = new JpegArithmeticDecoderStateMachine(new byte[0]);
    internal int[][] DcStatistics = new int[4][];
    internal int[] FixedStatistics = new int[] { 113 };
    internal int[] DcPredictors = new int[256];
    internal int[] DcContexts = new int[256];
    internal int[] DecisionBits = new int[0];
    internal int DecisionIndex;
    internal bool UsesDecisionBits;

    internal JpegArithmeticDcCoefficientDecoder(byte[] entropyData)
    {
        StateMachine = new JpegArithmeticDecoderStateMachine(entropyData);
        Initialize();
    }

    internal static JpegArithmeticDcCoefficientDecoder FromStream(System.IO.Stream stream)
    {
        return FromStateMachine(JpegArithmeticDecoderStateMachine.FromStream(stream));
    }

    internal static JpegArithmeticDcCoefficientDecoder FromStateMachine(JpegArithmeticDecoderStateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }

        JpegArithmeticDcCoefficientDecoder decoder = new JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.StateMachine = stateMachine;
        decoder.Initialize();
        return decoder;
    }

    internal void Initialize()
    {
        int index = 0;
        while (index < DcStatistics.Length)
        {
            DcStatistics[index] = new int[64];
            index = index + 1;
        }

        FixedStatistics = new int[] { 113 };
        DcPredictors = new int[256];
        DcContexts = new int[256];
        DecisionBits = new int[0];
        DecisionIndex = 0;
        UsesDecisionBits = false;
    }

    internal int[] DecodeDcFirstBlock(JpegArithmeticConditioningTable conditioningTable, int componentIndex, int successiveLow)
    {
        ValidateSuccessiveLow(successiveLow);
        ValidateComponentIndex(componentIndex);
        ValidateDcConditioningTable(conditioningTable);

        int[] coefficients = new int[JpegConstants.BlockSampleCount];
        int difference = DecodeDcDifference(conditioningTable, componentIndex);
        int value = DcPredictors[componentIndex] + difference;
        DcPredictors[componentIndex] = value;
        coefficients[0] = value << successiveLow;
        return coefficients;
    }

    internal int[] DecodeDcFirstBlockForScanComponent(List<JpegArithmeticConditioningTable> tables, JpegScanHeader scan, JpegScanComponent component, int componentIndex)
    {
        ValidateDcFirstScan(scan);
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegArithmeticConditioningTable table = FindOrCreateDefaultDcTable(tables, component.DcEntropyTableIdentifier);
        return DecodeDcFirstBlock(table, componentIndex, scan.SuccessiveLow);
    }

    internal void DecodeDcRefinementBlock(int[] coefficients, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);

        int bit = DecodeDecision(FixedStatistics, 0);
        if (bit == 1)
        {
            coefficients[0] = coefficients[0] + (1 << successiveLow);
        }
    }

    internal void DecodeDcRefinementBlockForScan(JpegScanHeader scan, int[] coefficients)
    {
        ValidateDcRefinementScan(scan);
        DecodeDcRefinementBlock(coefficients, scan.SuccessiveLow);
    }

    internal int DecodeDcDifference(JpegArithmeticConditioningTable conditioningTable, int componentIndex)
    {
        ValidateComponentIndex(componentIndex);
        ValidateDcConditioningTable(conditioningTable);

        int[] statistics = DcStatistics[conditioningTable.Identifier];
        int context = DcContexts[componentIndex];
        if (DecodeDecision(statistics, context) == 0)
        {
            DcContexts[componentIndex] = 0;
            return 0;
        }

        int sign = DecodeDecision(statistics, context + 1);
        int magnitudeContext = context + 2 + sign;
        int magnitude = DecodeMagnitudeCategory(statistics, magnitudeContext);
        UpdateDcContext(conditioningTable, componentIndex, magnitude, sign);
        int difference = DecodeMagnitudeBits(statistics, magnitudeContext, magnitude);
        if (sign == 1)
        {
            difference = -difference;
        }

        return difference;
    }

    internal int DecodeMagnitudeCategory(int[] statistics, int context)
    {
        int magnitude = DecodeDecision(statistics, context);
        if (magnitude != 0)
        {
            context = 20;
            while (DecodeDecision(statistics, context) != 0)
            {
                magnitude = magnitude << 1;
                if (magnitude == 0x8000)
                {
                    throw JpegInvalidDataException.Create("Arithmetic DC magnitude category overflowed.");
                }

                context = context + 1;
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

    internal void UpdateDcContext(JpegArithmeticConditioningTable conditioningTable, int componentIndex, int magnitude, int sign)
    {
        int lowerThreshold = (1 << conditioningTable.Lower) >> 1;
        int upperThreshold = (1 << conditioningTable.Upper) >> 1;
        if (magnitude < lowerThreshold)
        {
            DcContexts[componentIndex] = 0;
        }
        else if (magnitude > upperThreshold)
        {
            DcContexts[componentIndex] = 12 + (sign * 4);
        }
        else
        {
            DcContexts[componentIndex] = 4 + (sign * 4);
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

    internal JpegArithmeticConditioningTable CreateDefaultDcConditioningTable(int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw new System.ArgumentOutOfRangeException("identifier");
        }

        JpegArithmeticConditioningTable table = new JpegArithmeticConditioningTable();
        table.TableClass = 0;
        table.Identifier = identifier;
        table.Lower = 0;
        table.Upper = 1;
        table.ConditioningValue = 0x10;
        return table;
    }

    internal JpegArithmeticConditioningTable FindOrCreateDefaultDcTable(List<JpegArithmeticConditioningTable>? tables, int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw new System.ArgumentOutOfRangeException("identifier");
        }

        if (tables != null)
        {
            foreach (JpegArithmeticConditioningTable table in tables)
            {
                if (table.TableClass == 0 && table.Identifier == identifier)
                {
                    return table;
                }
            }
        }

        return CreateDefaultDcConditioningTable(identifier);
    }

    internal void ResetDcState()
    {
        int index = 0;
        while (index < DcPredictors.Length)
        {
            DcPredictors[index] = 0;
            DcContexts[index] = 0;
            index = index + 1;
        }

        index = 0;
        while (index < DcStatistics.Length)
        {
            StateMachine.ResetStatistics(DcStatistics[index]);
            index = index + 1;
        }

        FixedStatistics[0] = 113;
    }

    internal void ValidateDcConditioningTable(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic conditioning table class does not match DC decoding.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegInvalidDataException.Create("Arithmetic conditioning table identifier must be between 0 and 3.");
        }

        if (table.Lower < 0 || table.Lower > 15 || table.Upper < 0 || table.Upper > 15)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC conditioning bounds must be between 0 and 15.");
        }

        if (table.Lower > table.Upper)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC conditioning lower bound cannot exceed upper bound.");
        }
    }

    internal void ValidateDcFirstScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC first scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh != 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC first scans must have successive high approximation 0.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
    }

    internal void ValidateDcRefinementScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC refinement scans must use spectral selection 0..0.");
        }

        if (scan.SuccessiveHigh <= 0)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC refinement scans must have a positive successive high approximation.");
        }

        ValidateSuccessiveLow(scan.SuccessiveLow);
        if (scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegInvalidDataException.Create("Arithmetic DC refinement scans must decrease successive approximation by one bit.");
        }
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

    internal void ValidateSuccessiveLow(int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw JpegInvalidDataException.Create("Arithmetic successive low approximation must be between 0 and 15.");
        }
    }

    internal void ValidateComponentIndex(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= DcPredictors.Length)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }
    }
}
