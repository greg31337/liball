namespace barcode;

internal class Pdf417NumericEncodingResult
{
    internal string Digits = "";
    internal int[] DataCodewords = new int[0];
    internal int[] Codewords = new int[0];
    internal int[] SegmentDigitStarts = new int[0];
    internal int[] SegmentDigitCounts = new int[0];
    internal int[] SegmentCodewordStarts = new int[0];
    internal int[] SegmentCodewordCounts = new int[0];
}

internal class Pdf417NumericDecodingResult
{
    internal string Digits = "";
    internal int[] DataCodewords = new int[0];
    internal int[] SegmentDigitStarts = new int[0];
    internal int[] SegmentDigitCounts = new int[0];
    internal int[] SegmentCodewordStarts = new int[0];
    internal int[] SegmentCodewordCounts = new int[0];
    internal int ConsumedCodewordCount;
}

internal static class Pdf417NumericCompaction
{
    internal const int NumericLatchCodeword = 902;
    internal const int CodewordBase = 900;
    internal const int MaximumDigitsPerSegment = 44;
    internal const int MaximumCodewordsPerSegment = 15;

    internal static Pdf417NumericEncodingResult EncodeDigits(string digits)
    {
        ValidateDigits(digits);
        int segmentCount = (digits.Length + MaximumDigitsPerSegment - 1)
            / MaximumDigitsPerSegment;
        int maximumDataCodewordCount = CheckedMath.Multiply(
            segmentCount,
            MaximumCodewordsPerSegment);
        int[] dataBuffer = new int[maximumDataCodewordCount];
        int[] segmentDigitStarts = new int[segmentCount];
        int[] segmentDigitCounts = new int[segmentCount];
        int[] segmentCodewordStarts = new int[segmentCount];
        int[] segmentCodewordCounts = new int[segmentCount];
        int[] reverseBuffer = new int[MaximumCodewordsPerSegment];
        int digitOffset = 0;
        int codewordOffset = 0;
        int segmentIndex = 0;
        while (digitOffset < digits.Length)
        {
            int digitCount = digits.Length - digitOffset;
            if (digitCount > MaximumDigitsPerSegment)
            {
                digitCount = MaximumDigitsPerSegment;
            }

            string decimalText = "1" + digits.Substring(digitOffset, digitCount);
            global::System.Numerics.BigInteger value = global::System.Numerics.BigInteger.Parse(
                decimalText);
            int reverseCount = 0;
            while (value > 0)
            {
                reverseBuffer[reverseCount] = (int)(value % CodewordBase);
                reverseCount = reverseCount + 1;
                value = value / CodewordBase;
            }

            segmentDigitStarts[segmentIndex] = digitOffset;
            segmentDigitCounts[segmentIndex] = digitCount;
            segmentCodewordStarts[segmentIndex] = codewordOffset;
            segmentCodewordCounts[segmentIndex] = reverseCount;
            int reverseIndex = reverseCount - 1;
            while (reverseIndex >= 0)
            {
                dataBuffer[codewordOffset] = reverseBuffer[reverseIndex];
                codewordOffset = codewordOffset + 1;
                reverseIndex = reverseIndex - 1;
            }

            digitOffset = digitOffset + digitCount;
            segmentIndex = segmentIndex + 1;
        }

        int[] dataCodewords = CopyIntegerPrefix(dataBuffer, codewordOffset);
        int[] codewords = new int[CheckedMath.Add(dataCodewords.Length, 1)];
        codewords[0] = NumericLatchCodeword;
        CopyIntegersToOffset(dataCodewords, codewords, 1);
        Pdf417NumericEncodingResult result = new Pdf417NumericEncodingResult();
        result.Digits = digits;
        result.DataCodewords = dataCodewords;
        result.Codewords = codewords;
        result.SegmentDigitStarts = segmentDigitStarts;
        result.SegmentDigitCounts = segmentDigitCounts;
        result.SegmentCodewordStarts = segmentCodewordStarts;
        result.SegmentCodewordCounts = segmentCodewordCounts;
        return result;
    }

    internal static Pdf417NumericDecodingResult DecodeCodewords(int[] codewords)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("PDF417 numeric-compaction codewords must not be null.");
        }

        if (codewords.Length < 2 || codewords[0] != NumericLatchCodeword)
        {
            throw new BarcodeDecodingException("PDF417 numeric compaction requires latch codeword 902 followed by data.");
        }

        int[] dataCodewords = new int[codewords.Length - 1];
        int index = 1;
        while (index < codewords.Length)
        {
            dataCodewords[index - 1] = codewords[index];
            index = index + 1;
        }

        Pdf417NumericDecodingResult result = DecodeDataCodewords(dataCodewords);
        result.ConsumedCodewordCount = codewords.Length;
        return result;
    }

    internal static Pdf417NumericDecodingResult DecodeDataCodewords(int[] dataCodewords)
    {
        ValidateDataCodewords(dataCodewords);
        int segmentCount = (dataCodewords.Length + MaximumCodewordsPerSegment - 1)
            / MaximumCodewordsPerSegment;
        int[] segmentDigitStarts = new int[segmentCount];
        int[] segmentDigitCounts = new int[segmentCount];
        int[] segmentCodewordStarts = new int[segmentCount];
        int[] segmentCodewordCounts = new int[segmentCount];
        global::System.Text.StringBuilder digits = new global::System.Text.StringBuilder(
            CheckedMath.Multiply(segmentCount, MaximumDigitsPerSegment));
        int codewordOffset = 0;
        int segmentIndex = 0;
        while (codewordOffset < dataCodewords.Length)
        {
            int codewordCount = dataCodewords.Length - codewordOffset;
            if (codewordCount > MaximumCodewordsPerSegment)
            {
                codewordCount = MaximumCodewordsPerSegment;
            }

            global::System.Numerics.BigInteger value = 0;
            int index = 0;
            while (index < codewordCount)
            {
                value = (value * CodewordBase) + dataCodewords[codewordOffset + index];
                index = index + 1;
            }

            string decimalText = value.ToString();
            if (decimalText.Length < 2 || decimalText[0] != '1')
            {
                throw new BarcodeDecodingException("PDF417 numeric segment does not contain the required leading sentinel digit.");
            }

            string segmentDigits = decimalText.Substring(1);
            if (segmentIndex < segmentCount - 1
                && segmentDigits.Length != MaximumDigitsPerSegment)
            {
                throw new BarcodeDecodingException("A non-terminal PDF417 numeric segment must represent exactly 44 digits.");
            }

            if (segmentDigits.Length > MaximumDigitsPerSegment)
            {
                throw new BarcodeDecodingException("PDF417 numeric segment exceeds 44 decimal digits.");
            }

            segmentDigitStarts[segmentIndex] = digits.Length;
            segmentDigitCounts[segmentIndex] = segmentDigits.Length;
            segmentCodewordStarts[segmentIndex] = codewordOffset;
            segmentCodewordCounts[segmentIndex] = codewordCount;
            digits.Append(segmentDigits);
            codewordOffset = codewordOffset + codewordCount;
            segmentIndex = segmentIndex + 1;
        }

        Pdf417NumericDecodingResult result = new Pdf417NumericDecodingResult();
        result.Digits = digits.ToString();
        result.DataCodewords = CopyIntegerPrefix(dataCodewords, dataCodewords.Length);
        result.SegmentDigitStarts = segmentDigitStarts;
        result.SegmentDigitCounts = segmentDigitCounts;
        result.SegmentCodewordStarts = segmentCodewordStarts;
        result.SegmentCodewordCounts = segmentCodewordCounts;
        result.ConsumedCodewordCount = CheckedMath.Add(dataCodewords.Length, 1);
        return result;
    }

    static void ValidateDigits(string digits)
    {
        if (digits == null)
        {
            throw new BarcodeEncodingException("PDF417 numeric-compaction input must not be null.");
        }

        if (digits.Length == 0)
        {
            throw new BarcodeEncodingException("PDF417 numeric compaction requires at least one digit.");
        }

        int index = 0;
        while (index < digits.Length)
        {
            if (digits[index] < '0' || digits[index] > '9')
            {
                throw new BarcodeEncodingException("PDF417 numeric compaction accepts decimal digits only.");
            }

            index = index + 1;
        }
    }

    static void ValidateDataCodewords(int[] dataCodewords)
    {
        if (dataCodewords == null)
        {
            throw new BarcodeDecodingException("PDF417 numeric data codewords must not be null.");
        }

        if (dataCodewords.Length == 0)
        {
            throw new BarcodeDecodingException("PDF417 numeric compaction requires at least one data codeword.");
        }

        int index = 0;
        while (index < dataCodewords.Length)
        {
            if (dataCodewords[index] < 0 || dataCodewords[index] >= CodewordBase)
            {
                throw new BarcodeDecodingException("PDF417 numeric data codewords must be between 0 and 899.");
            }

            index = index + 1;
        }
    }

    static int[] CopyIntegerPrefix(int[] source, int count)
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

    static void CopyIntegersToOffset(int[] source, int[] target, int targetOffset)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[targetOffset + index] = source[index];
            index = index + 1;
        }
    }
}
