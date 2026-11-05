namespace barcode;

internal static class Code128CodeSets
{
    internal static bool CanEncodeByte(int byteValue, Code128CodeSet codeSet, bool upperShift)
    {
        if (byteValue < 0 || byteValue > 255)
        {
            return false;
        }

        int baseValue = byteValue;
        if (upperShift)
        {
            if (byteValue < 128)
            {
                return false;
            }

            baseValue = byteValue - 128;
        }
        else if (byteValue >= 128)
        {
            return false;
        }

        switch (codeSet)
        {
            case Code128CodeSet.A:
                return baseValue <= 95;
            case Code128CodeSet.B:
                return baseValue >= 32 && baseValue <= 127;
            case Code128CodeSet.C:
                return false;
            default:
                throw new BarcodeValidationException("A concrete Code 128 code set is required for byte mapping.");
        }
    }

    internal static int EncodeByte(int byteValue, Code128CodeSet codeSet, bool upperShift)
    {
        if (!CanEncodeByte(byteValue, codeSet, upperShift))
        {
            throw new BarcodeValidationException("The byte cannot be represented by the selected Code 128 code set and upper-shift state.");
        }

        int baseValue = byteValue;
        if (upperShift)
        {
            baseValue = byteValue - 128;
        }

        if (codeSet == Code128CodeSet.A)
        {
            if (baseValue < 32)
            {
                return baseValue + 64;
            }

            return baseValue - 32;
        }

        return baseValue - 32;
    }

    internal static int DecodeByte(int codeword, Code128CodeSet codeSet, bool upperShift)
    {
        if (codeSet != Code128CodeSet.A && codeSet != Code128CodeSet.B)
        {
            throw new BarcodeValidationException("Code 128 byte decoding requires code set A or B.");
        }

        Code128CodewordRole role = Code128Patterns.GetCodewordRole(codeword, codeSet);
        if (role != Code128CodewordRole.Data)
        {
            throw new BarcodeValidationException("The Code 128 codeword is not data in the selected code set.");
        }

        int byteValue = 0;
        if (codeSet == Code128CodeSet.A)
        {
            if (codeword <= 63)
            {
                byteValue = codeword + 32;
            }
            else
            {
                byteValue = codeword - 64;
            }
        }
        else
        {
            byteValue = codeword + 32;
        }

        if (upperShift)
        {
            byteValue = byteValue + 128;
        }

        return byteValue;
    }

    internal static bool RequiresUpperShift(int byteValue, Code128CodeSet codeSet)
    {
        if (CanEncodeByte(byteValue, codeSet, false))
        {
            return false;
        }

        if (CanEncodeByte(byteValue, codeSet, true))
        {
            return true;
        }

        throw new BarcodeValidationException("The byte cannot be represented by the selected Code 128 code set.");
    }

    internal static int EncodeDigitPair(int firstByte, int secondByte)
    {
        ValidateDigitByte(firstByte);
        ValidateDigitByte(secondByte);
        return ((firstByte - '0') * 10) + (secondByte - '0');
    }

    internal static int[] DecodeDigitPair(int codeword)
    {
        if (codeword < 0 || codeword > 99)
        {
            throw new BarcodeValidationException("Code 128 code set C data codeword must be from zero through 99.");
        }

        int firstByte = '0' + (codeword / 10);
        int secondByte = '0' + (codeword % 10);
        return new int[] { firstByte, secondByte };
    }

    internal static Code128CodeSet GetShiftedCodeSet(Code128CodeSet codeSet)
    {
        switch (codeSet)
        {
            case Code128CodeSet.A:
                return Code128CodeSet.B;
            case Code128CodeSet.B:
                return Code128CodeSet.A;
            default:
                throw new BarcodeValidationException("Code 128 SHIFT is valid only between code sets A and B.");
        }
    }

    internal static int GetSwitchCodeword(Code128CodeSet currentCodeSet, Code128CodeSet targetCodeSet)
    {
        ValidateConcreteCodeSet(currentCodeSet);
        ValidateConcreteCodeSet(targetCodeSet);
        if (currentCodeSet == targetCodeSet)
        {
            throw new BarcodeValidationException("A Code 128 code-set switch must change the active code set.");
        }

        switch (targetCodeSet)
        {
            case Code128CodeSet.A:
                return Code128Patterns.CodeACodeword;
            case Code128CodeSet.B:
                return Code128Patterns.CodeBCodeword;
            case Code128CodeSet.C:
                return Code128Patterns.CodeCCodeword;
            default:
                throw new BarcodeValidationException("A concrete Code 128 target code set is required.");
        }
    }

    internal static Code128CodeSet ApplySwitchCodeword(Code128CodeSet currentCodeSet, int codeword)
    {
        ValidateConcreteCodeSet(currentCodeSet);
        Code128CodewordRole role = Code128Patterns.GetCodewordRole(codeword, currentCodeSet);
        switch (role)
        {
            case Code128CodewordRole.CodeA:
                return Code128CodeSet.A;
            case Code128CodewordRole.CodeB:
                return Code128CodeSet.B;
            case Code128CodewordRole.CodeC:
                return Code128CodeSet.C;
            default:
                throw new BarcodeValidationException("The supplied codeword is not a code-set switch in the active Code 128 code set.");
        }
    }

    internal static int GetFunctionCodeword(int functionNumber, Code128CodeSet codeSet)
    {
        ValidateConcreteCodeSet(codeSet);
        switch (functionNumber)
        {
            case 1:
                return Code128Patterns.Function1Codeword;
            case 2:
                if (codeSet != Code128CodeSet.C)
                {
                    return Code128Patterns.Function2Codeword;
                }

                break;
            case 3:
                if (codeSet != Code128CodeSet.C)
                {
                    return Code128Patterns.Function3Codeword;
                }

                break;
            case 4:
                if (codeSet == Code128CodeSet.A)
                {
                    return Code128Patterns.Function4InCodeSetACodeword;
                }

                if (codeSet == Code128CodeSet.B)
                {
                    return Code128Patterns.Function4InCodeSetBCodeword;
                }

                break;
        }

        throw new BarcodeValidationException("The requested function is unavailable in the selected Code 128 code set.");
    }

    internal static void ValidateDigitByte(int byteValue)
    {
        if (byteValue < '0' || byteValue > '9')
        {
            throw new BarcodeValidationException("Code 128 code set C requires two ASCII digits.");
        }
    }

    internal static void ValidateConcreteCodeSet(Code128CodeSet codeSet)
    {
        if (codeSet != Code128CodeSet.A && codeSet != Code128CodeSet.B && codeSet != Code128CodeSet.C)
        {
            throw new BarcodeValidationException("A concrete Code 128 code set is required.");
        }
    }
}

internal class Code128CodeSetState
{
    Code128CodeSet ActiveCodeSet;
    bool IsShiftPending;

    internal Code128CodeSetState(Code128CodeSet initialCodeSet)
    {
        Code128CodeSets.ValidateConcreteCodeSet(initialCodeSet);
        ActiveCodeSet = initialCodeSet;
    }

    internal void ReadShift()
    {
        if (IsShiftPending)
        {
            throw new BarcodeValidationException("Code 128 SHIFT must be followed by one data character.");
        }

        Code128CodeSets.GetShiftedCodeSet(ActiveCodeSet);
        IsShiftPending = true;
    }

    internal void ReadSwitch(int codeword)
    {
        if (IsShiftPending)
        {
            throw new BarcodeValidationException("A code-set switch cannot replace the data character required after Code 128 SHIFT.");
        }

        ActiveCodeSet = Code128CodeSets.ApplySwitchCodeword(ActiveCodeSet, codeword);
    }

    internal Code128CodeSet ConsumeDataCodeSet()
    {
        if (IsShiftPending)
        {
            IsShiftPending = false;
            return Code128CodeSets.GetShiftedCodeSet(ActiveCodeSet);
        }

        return ActiveCodeSet;
    }

    internal Code128CodeSet GetActiveCodeSet()
    {
        return ActiveCodeSet;
    }

    internal bool HasPendingShift()
    {
        return IsShiftPending;
    }

    internal void ValidateFinished()
    {
        if (IsShiftPending)
        {
            throw new BarcodeValidationException("Code 128 SHIFT must be followed by one data character.");
        }
    }
}

internal class Code128UpperShiftState
{
    bool IsUpperShiftLatched;
    bool IsFunction4ImmediatelyPending;
    bool IsSingleUpperShiftPending;

    internal void ReadFunction4()
    {
        if (IsSingleUpperShiftPending)
        {
            throw new BarcodeValidationException("A second non-consecutive FNC4 cannot precede the pending Code 128 data character.");
        }

        if (IsFunction4ImmediatelyPending)
        {
            IsUpperShiftLatched = !IsUpperShiftLatched;
            IsFunction4ImmediatelyPending = false;
        }
        else
        {
            IsFunction4ImmediatelyPending = true;
        }
    }

    internal void ReadOtherControl()
    {
        if (IsFunction4ImmediatelyPending)
        {
            IsFunction4ImmediatelyPending = false;
            IsSingleUpperShiftPending = true;
        }
    }

    internal int ApplyToDataByte(int baseByteValue)
    {
        if (baseByteValue < 0 || baseByteValue > 127)
        {
            throw new BarcodeValidationException("Code 128 upper shift requires a base ASCII byte.");
        }

        bool invertForSingleByte = IsFunction4ImmediatelyPending || IsSingleUpperShiftPending;
        bool useUpperHalf = IsUpperShiftLatched;
        if (invertForSingleByte)
        {
            useUpperHalf = !useUpperHalf;
        }

        IsFunction4ImmediatelyPending = false;
        IsSingleUpperShiftPending = false;
        if (useUpperHalf)
        {
            return baseByteValue + 128;
        }

        return baseByteValue;
    }

    internal bool IsLatched()
    {
        return IsUpperShiftLatched;
    }

    internal bool HasPendingSingleShift()
    {
        return IsFunction4ImmediatelyPending || IsSingleUpperShiftPending;
    }

    internal void ValidateFinished()
    {
        if (HasPendingSingleShift())
        {
            throw new BarcodeValidationException("Code 128 FNC4 must be followed by a data character.");
        }
    }
}
