namespace jpeg;

internal class JpegArithmeticCoefficientEncoder
{
    internal JpegArithmeticEncoderStateMachine StateMachine = new JpegArithmeticEncoderStateMachine();
    internal int[][] DcStatistics = new int[4][];
    internal int[][] AcStatistics = new int[4][];
    internal int[] FixedStatistics = new int[] { 113 };
    internal int[] DcPredictors = new int[256];
    internal int[] DcContexts = new int[256];

    internal JpegArithmeticCoefficientEncoder()
    {
        StateMachine = new JpegArithmeticEncoderStateMachine();
        Initialize();
    }

    internal static JpegArithmeticCoefficientEncoder FromStateMachine(JpegArithmeticEncoderStateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }

        JpegArithmeticCoefficientEncoder encoder = new JpegArithmeticCoefficientEncoder();
        encoder.StateMachine = stateMachine;
        encoder.Initialize();
        return encoder;
    }

    internal void Initialize()
    {
        int index = 0;
        while (index < 4)
        {
            DcStatistics[index] = new int[64];
            AcStatistics[index] = new int[256];
            index = index + 1;
        }

        FixedStatistics = new int[] { 113 };
        DcPredictors = new int[256];
        DcContexts = new int[256];
    }

    internal void EncodeDcFirstBlock(int[] coefficients, JpegArithmeticConditioningTable conditioningTable, int componentIndex, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);
        ValidateComponentIndex(componentIndex);
        ValidateDcConditioningTable(conditioningTable);

        int coefficient = coefficients[0] >> successiveLow;
        EncodeDcDifference(coefficient, conditioningTable, componentIndex);
    }

    internal void EncodeDcDifference(int coefficient, JpegArithmeticConditioningTable conditioningTable, int componentIndex)
    {
        ValidateComponentIndex(componentIndex);
        ValidateDcConditioningTable(conditioningTable);

        int[] statistics = DcStatistics[conditioningTable.Identifier];
        int context = DcContexts[componentIndex];
        long wideDifference = (long)coefficient - (long)DcPredictors[componentIndex];
        if (wideDifference < -65535L || wideDifference > 65535L)
        {
            throw JpegWriteValidationException.Create("Arithmetic DC coefficient difference magnitude must fit in 16 bits.");
        }

        int difference = (int)wideDifference;
        if (difference == 0)
        {
            StateMachine.EncodeBit(statistics, context, 0);
            DcContexts[componentIndex] = 0;
            return;
        }

        DcPredictors[componentIndex] = coefficient;
        StateMachine.EncodeBit(statistics, context, 1);

        int sign = 0;
        int magnitude = difference;
        if (difference > 0)
        {
            StateMachine.EncodeBit(statistics, context + 1, 0);
            context = context + 2;
        }
        else
        {
            sign = 1;
            magnitude = 0 - difference;
            StateMachine.EncodeBit(statistics, context + 1, 1);
            context = context + 3;
        }

        int encodedMagnitude = EncodeMagnitudeCategory(statistics, context, 20, magnitude);
        UpdateDcContext(conditioningTable, componentIndex, encodedMagnitude, sign);
        EncodeMagnitudeBits(statistics, context + 14, encodedMagnitude, magnitude - 1);
    }

    internal void EncodeDcRefinementBlock(int[] coefficients, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateSuccessiveLow(successiveLow);

        int bit = (coefficients[0] >> successiveLow) & 1;
        StateMachine.EncodeBit(FixedStatistics, 0, bit);
    }

    internal void EncodeAcFirstBlock(int[] coefficients, JpegArithmeticConditioningTable conditioningTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        ValidateAcConditioningTable(conditioningTable);

        int[] statistics = AcStatistics[conditioningTable.Identifier];
        int lastNonzero = FindLastNonzeroAfterPointTransform(coefficients, spectralStart, spectralEnd, successiveLow);
        int coefficientIndex = spectralStart;
        while (coefficientIndex <= lastNonzero)
        {
            int context = 3 * (coefficientIndex - 1);
            StateMachine.EncodeBit(statistics, context, 0);
            int coefficient = ShiftAcCoefficientDown(coefficients[coefficientIndex], successiveLow);
            while (coefficient == 0)
            {
                StateMachine.EncodeBit(statistics, context + 1, 0);
                context = context + 3;
                coefficientIndex = coefficientIndex + 1;
                coefficient = ShiftAcCoefficientDown(coefficients[coefficientIndex], successiveLow);
            }

            StateMachine.EncodeBit(statistics, context + 1, 1);
            int magnitude = GetMagnitude(coefficient);
            if (coefficient > 0)
            {
                StateMachine.EncodeBit(FixedStatistics, 0, 0);
            }
            else
            {
                StateMachine.EncodeBit(FixedStatistics, 0, 1);
            }

            context = context + 2;
            int encodedMagnitude = EncodeAcMagnitudeCategory(statistics, context, coefficientIndex, conditioningTable.Kx, magnitude);
            EncodeMagnitudeBits(statistics, context + 14, encodedMagnitude, magnitude - 1);
            coefficientIndex = coefficientIndex + 1;
        }

        if (coefficientIndex <= spectralEnd)
        {
            int context = 3 * (coefficientIndex - 1);
            StateMachine.EncodeBit(statistics, context, 1);
        }
    }

    internal void EncodeAcRefinementBlock(int[] coefficients, JpegArithmeticConditioningTable conditioningTable, int spectralStart, int spectralEnd, int successiveLow)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcRange(spectralStart, spectralEnd);
        ValidateSuccessiveLow(successiveLow);
        ValidateAcConditioningTable(conditioningTable);

        int[] statistics = AcStatistics[conditioningTable.Identifier];
        int lastScanBit = FindLastNonzeroAfterPointTransform(coefficients, spectralStart, spectralEnd, successiveLow);
        int lastPreviouslyNonzero = FindLastNonzeroAfterPointTransform(coefficients, spectralStart, lastScanBit, successiveLow + 1);
        int coefficientIndex = spectralStart;
        while (coefficientIndex <= lastScanBit)
        {
            int context = 3 * (coefficientIndex - 1);
            if (coefficientIndex > lastPreviouslyNonzero)
            {
                StateMachine.EncodeBit(statistics, context, 0);
            }

            bool coefficientHandled = false;
            while (!coefficientHandled)
            {
                int coefficient = ShiftAcMagnitudeDown(coefficients[coefficientIndex], successiveLow);
                if (coefficient != 0)
                {
                    if ((coefficient >> 1) != 0)
                    {
                        StateMachine.EncodeBit(statistics, context + 2, coefficient & 1);
                    }
                    else
                    {
                        StateMachine.EncodeBit(statistics, context + 1, 1);
                        if (coefficients[coefficientIndex] < 0)
                        {
                            StateMachine.EncodeBit(FixedStatistics, 0, 1);
                        }
                        else
                        {
                            StateMachine.EncodeBit(FixedStatistics, 0, 0);
                        }
                    }

                    coefficientHandled = true;
                }
                else
                {
                    StateMachine.EncodeBit(statistics, context + 1, 0);
                    context = context + 3;
                    coefficientIndex = coefficientIndex + 1;
                }
            }

            coefficientIndex = coefficientIndex + 1;
        }

        if (coefficientIndex <= spectralEnd)
        {
            int context = 3 * (coefficientIndex - 1);
            StateMachine.EncodeBit(statistics, context, 1);
        }
    }

    internal int EncodeMagnitudeCategory(int[] statistics, int context, int extensionContext, int magnitude)
    {
        int value = magnitude - 1;
        int encodedMagnitude = 0;
        if (value != 0)
        {
            StateMachine.EncodeBit(statistics, context, 1);
            encodedMagnitude = 1;
            int valueBits = value;
            context = extensionContext;
            valueBits = valueBits >> 1;
            while (valueBits != 0)
            {
                StateMachine.EncodeBit(statistics, context, 1);
                encodedMagnitude = encodedMagnitude << 1;
                context = context + 1;
                valueBits = valueBits >> 1;
            }
        }

        StateMachine.EncodeBit(statistics, context, 0);
        return encodedMagnitude;
    }

    internal int EncodeAcMagnitudeCategory(int[] statistics, int context, int coefficientIndex, int conditioningKx, int magnitude)
    {
        int value = magnitude - 1;
        int encodedMagnitude = 0;
        if (value != 0)
        {
            StateMachine.EncodeBit(statistics, context, 1);
            encodedMagnitude = 1;
            int valueBits = value;
            valueBits = valueBits >> 1;
            if (valueBits != 0)
            {
                StateMachine.EncodeBit(statistics, context, 1);
                encodedMagnitude = encodedMagnitude << 1;
                if (coefficientIndex <= conditioningKx)
                {
                    context = 189;
                }
                else
                {
                    context = 217;
                }

                valueBits = valueBits >> 1;
                while (valueBits != 0)
                {
                    StateMachine.EncodeBit(statistics, context, 1);
                    encodedMagnitude = encodedMagnitude << 1;
                    context = context + 1;
                    valueBits = valueBits >> 1;
                }
            }
        }

        StateMachine.EncodeBit(statistics, context, 0);
        return encodedMagnitude;
    }

    internal void EncodeMagnitudeBits(int[] statistics, int context, int encodedMagnitude, int valueBits)
    {
        encodedMagnitude = encodedMagnitude >> 1;
        while (encodedMagnitude > 0)
        {
            int bit = 0;
            if ((encodedMagnitude & valueBits) != 0)
            {
                bit = 1;
            }

            StateMachine.EncodeBit(statistics, context, bit);
            encodedMagnitude = encodedMagnitude >> 1;
        }
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

    internal int FindLastNonzeroAfterPointTransform(int[] coefficients, int spectralStart, int spectralEnd, int successiveLow)
    {
        int index = spectralEnd;
        while (index >= spectralStart)
        {
            if (ShiftAcMagnitudeDown(coefficients[index], successiveLow) != 0)
            {
                return index;
            }

            index = index - 1;
        }

        return spectralStart - 1;
    }

    internal int ShiftAcCoefficientDown(int coefficient, int successiveLow)
    {
        int bitValue = 1 << successiveLow;
        return coefficient / bitValue;
    }

    internal int ShiftAcMagnitudeDown(int coefficient, int successiveLow)
    {
        int magnitude = GetMagnitude(coefficient);
        return ShiftAcCoefficientDown(magnitude, successiveLow);
    }

    internal int GetMagnitude(int value)
    {
        long magnitude = value;
        if (magnitude < 0)
        {
            magnitude = 0L - magnitude;
        }

        if (magnitude > 65535L)
        {
            throw JpegWriteValidationException.Create("Arithmetic coefficient magnitude must fit in 16 bits.");
        }

        return (int)magnitude;
    }

    internal void Finish()
    {
        StateMachine.Finish();
    }

    internal byte[] ToArray()
    {
        return StateMachine.ToArray();
    }

    internal void ValidateDcConditioningTable(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 0)
        {
            throw JpegWriteValidationException.Create("Arithmetic conditioning table class does not match DC encoding.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("Arithmetic conditioning table identifier must be between 0 and 3.");
        }

        if (table.Lower < 0 || table.Lower > 15 || table.Upper < 0 || table.Upper > 15 || table.Lower > table.Upper)
        {
            throw JpegWriteValidationException.Create("Arithmetic DC conditioning bounds are invalid.");
        }
    }

    internal void ValidateAcConditioningTable(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 1)
        {
            throw JpegWriteValidationException.Create("Arithmetic conditioning table class does not match AC encoding.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("Arithmetic conditioning table identifier must be between 0 and 3.");
        }

        if (table.Kx < 1 || table.Kx > 63)
        {
            throw JpegWriteValidationException.Create("Arithmetic AC conditioning value must be between 1 and 63.");
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
            throw JpegWriteValidationException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateAcRange(int spectralStart, int spectralEnd)
    {
        if (spectralStart < 1 || spectralStart > 63)
        {
            throw JpegWriteValidationException.Create("Arithmetic AC scans must start between coefficients 1 and 63.");
        }

        if (spectralEnd < spectralStart || spectralEnd > 63)
        {
            throw JpegWriteValidationException.Create("Arithmetic AC scan spectral end is invalid.");
        }
    }

    internal void ValidateSuccessiveLow(int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw JpegWriteValidationException.Create("Arithmetic successive low approximation must be between 0 and 15.");
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
