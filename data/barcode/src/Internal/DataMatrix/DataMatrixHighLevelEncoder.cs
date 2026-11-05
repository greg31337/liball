namespace barcode;

internal static class DataMatrixHighLevelEncoder
{
    internal const int PadCodeword = 129;
    internal const int LatchToC40Codeword = 230;
    internal const int LatchToBase256Codeword = 231;
    internal const int Fnc1Codeword = 232;
    internal const int StructuredAppendCodeword = 233;
    internal const int ReaderProgrammingCodeword = 234;
    internal const int UpperShiftCodeword = 235;
    internal const int Macro05Codeword = 236;
    internal const int Macro06Codeword = 237;
    internal const int LatchToX12Codeword = 238;
    internal const int LatchToTextCodeword = 239;
    internal const int LatchToEdifactCodeword = 240;
    internal const int EciCodeword = 241;
    internal const int UnlatchCodeword = 254;

    internal static int[] EncodeAscii(byte[] data)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("Data Matrix ASCII data must not be null.");
        }

        if (data.Length == 0)
        {
            throw new BarcodeEncodingException("Data Matrix ASCII data must not be empty.");
        }

        return EncodeAsciiSegment(data, 0, data.Length);
    }

    internal static int[] EncodeAsciiSegment(byte[] data, int startIndex, int count)
    {
        if (data == null)
        {
            throw new BarcodeValidationException("Data Matrix ASCII segment data must not be null.");
        }

        int endIndex = CheckedMath.ValidateRange(startIndex, count, data.Length);
        int maximumCodewordCount = CheckedMath.Multiply(count, 2);
        int[] buffer = new int[maximumCodewordCount];
        int inputIndex = startIndex;
        int outputCount = 0;
        while (inputIndex < endIndex)
        {
            int value = data[inputIndex];
            if (inputIndex + 1 < endIndex
                && IsAsciiDigit(value)
                && IsAsciiDigit(data[inputIndex + 1]))
            {
                buffer[outputCount] = EncodeDigitPair(value, data[inputIndex + 1]);
                outputCount = outputCount + 1;
                inputIndex = inputIndex + 2;
            }
            else if (value <= 127)
            {
                buffer[outputCount] = value + 1;
                outputCount = outputCount + 1;
                inputIndex = inputIndex + 1;
            }
            else
            {
                buffer[outputCount] = UpperShiftCodeword;
                buffer[outputCount + 1] = value - 127;
                outputCount = outputCount + 2;
                inputIndex = inputIndex + 1;
            }
        }

        return CopyPrefix(buffer, outputCount);
    }

    internal static int[] EncodeAsciiToCapacity(byte[] data, int dataCodewordCapacity)
    {
        int[] encoded = EncodeAscii(data);
        return PadCodewords(encoded, dataCodewordCapacity);
    }

    internal static int EncodeDigitPair(int firstByte, int secondByte)
    {
        if (!IsAsciiDigit(firstByte) || !IsAsciiDigit(secondByte))
        {
            throw new BarcodeValidationException("Data Matrix ASCII digit-pair inputs must both be ASCII digits.");
        }

        return 130 + ((firstByte - 48) * 10) + secondByte - 48;
    }

    internal static int CountConsecutiveDigits(byte[] data, int startIndex)
    {
        if (data == null)
        {
            throw new BarcodeValidationException("Data Matrix digit-count data must not be null.");
        }

        if (startIndex < 0 || startIndex > data.Length)
        {
            throw new BarcodeValidationException("Data Matrix digit-count start index is outside the data range.");
        }

        int index = startIndex;
        while (index < data.Length && IsAsciiDigit(data[index]))
        {
            index = index + 1;
        }

        return index - startIndex;
    }

    internal static int GetLatchCodeword(DataMatrixEncodingMode encodingMode)
    {
        switch (encodingMode)
        {
            case DataMatrixEncodingMode.C40:
                return LatchToC40Codeword;
            case DataMatrixEncodingMode.Base256:
                return LatchToBase256Codeword;
            case DataMatrixEncodingMode.X12:
                return LatchToX12Codeword;
            case DataMatrixEncodingMode.Text:
                return LatchToTextCodeword;
            case DataMatrixEncodingMode.Edifact:
                return LatchToEdifactCodeword;
            default:
                throw new BarcodeValidationException("Data Matrix encoding mode does not have an ASCII latch codeword.");
        }
    }

    internal static DataMatrixEncodingMode FindModeForLatchCodeword(int codeword)
    {
        switch (codeword)
        {
            case LatchToC40Codeword:
                return DataMatrixEncodingMode.C40;
            case LatchToBase256Codeword:
                return DataMatrixEncodingMode.Base256;
            case LatchToX12Codeword:
                return DataMatrixEncodingMode.X12;
            case LatchToTextCodeword:
                return DataMatrixEncodingMode.Text;
            case LatchToEdifactCodeword:
                return DataMatrixEncodingMode.Edifact;
            default:
                return DataMatrixEncodingMode.Automatic;
        }
    }

    internal static bool IsLatchCodeword(int codeword)
    {
        return FindModeForLatchCodeword(codeword) != DataMatrixEncodingMode.Automatic;
    }

    internal static int[] PadCodewords(int[] codewords, int dataCodewordCapacity)
    {
        if (codewords == null)
        {
            throw new BarcodeValidationException("Data Matrix codewords to pad must not be null.");
        }

        if (dataCodewordCapacity < 0
            || dataCodewordCapacity > DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity)
        {
            throw new BarcodeValidationException("Data Matrix data-codeword capacity is outside the standard symbol range.");
        }

        if (codewords.Length > dataCodewordCapacity)
        {
            throw new BarcodeEncodingException("Data Matrix codewords exceed the selected symbol's data capacity.");
        }

        ValidateCodewords(codewords);
        int[] padded = new int[dataCodewordCapacity];
        int index = 0;
        while (index < codewords.Length)
        {
            padded[index] = codewords[index];
            index = index + 1;
        }

        if (index < padded.Length)
        {
            padded[index] = PadCodeword;
            index = index + 1;
        }

        while (index < padded.Length)
        {
            padded[index] = Randomize253State(PadCodeword, index + 1);
            index = index + 1;
        }

        return padded;
    }

    internal static int Randomize253State(int codeword, int codewordPosition)
    {
        Validate253StateCodeword(codeword);
        ValidateCodewordPosition(codewordPosition);
        int pseudoRandomNumber = ((149 * (codewordPosition % 253)) % 253) + 1;
        int randomized = codeword + pseudoRandomNumber;
        if (randomized > 254)
        {
            randomized = randomized - 254;
        }

        return randomized;
    }

    internal static int Derandomize253State(int randomizedCodeword, int codewordPosition)
    {
        Validate253StateCodeword(randomizedCodeword);
        ValidateCodewordPosition(codewordPosition);
        int pseudoRandomNumber = ((149 * (codewordPosition % 253)) % 253) + 1;
        int original = randomizedCodeword - pseudoRandomNumber;
        if (original < 1)
        {
            original = original + 254;
        }

        return original;
    }

    internal static int Randomize255State(int codeword, int codewordPosition)
    {
        Validate255StateCodeword(codeword);
        ValidateCodewordPosition(codewordPosition);
        int pseudoRandomNumber = ((149 * (codewordPosition % 255)) % 255) + 1;
        int randomized = codeword + pseudoRandomNumber;
        if (randomized > 255)
        {
            randomized = randomized - 256;
        }

        return randomized;
    }

    internal static int Derandomize255State(int randomizedCodeword, int codewordPosition)
    {
        Validate255StateCodeword(randomizedCodeword);
        ValidateCodewordPosition(codewordPosition);
        int pseudoRandomNumber = ((149 * (codewordPosition % 255)) % 255) + 1;
        int original = randomizedCodeword - pseudoRandomNumber;
        if (original < 0)
        {
            original = original + 256;
        }

        return original;
    }

    static bool IsAsciiDigit(int value)
    {
        return value >= 48 && value <= 57;
    }

    static int[] CopyPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void ValidateCodewords(int[] codewords)
    {
        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > 255)
            {
                throw new BarcodeValidationException("Data Matrix data codewords must be zero through 255.");
            }

            index = index + 1;
        }
    }

    static void Validate253StateCodeword(int codeword)
    {
        if (codeword < 1 || codeword > 254)
        {
            throw new BarcodeValidationException("Data Matrix 253-state codeword must be one through 254.");
        }
    }

    static void Validate255StateCodeword(int codeword)
    {
        if (codeword < 0 || codeword > 255)
        {
            throw new BarcodeValidationException("Data Matrix 255-state codeword must be zero through 255.");
        }
    }

    static void ValidateCodewordPosition(int codewordPosition)
    {
        if (codewordPosition <= 0)
        {
            throw new BarcodeValidationException("Data Matrix codeword position must be positive and one-based.");
        }
    }
}
