namespace barcode;

internal class Pdf417ByteEncodingResult
{
    internal byte[] Bytes = new byte[0];
    internal int ModeCodeword;
    internal int[] DataCodewords = new int[0];
    internal int[] Codewords = new int[0];
    internal bool ReturnsToTextMode;
}

internal class Pdf417ByteDecodingResult
{
    internal byte[] Bytes = new byte[0];
    internal int ModeCodeword;
    internal int[] DataCodewords = new int[0];
    internal int ConsumedCodewordCount;
    internal bool ReturnsToTextMode;
}

internal static class Pdf417ByteCompaction
{
    internal const int LatchWithRemainderCodeword = 901;
    internal const int ShiftFromTextCodeword = 913;
    internal const int LatchSixByteCodeword = 924;
    internal const int BytesPerPackedGroup = 6;
    internal const int CodewordsPerPackedGroup = 5;
    internal const int CodewordBase = 900;
    internal const ulong MaximumPackedGroupValue = 281474976710655UL;

    internal static Pdf417ByteEncodingResult EncodeBytes(
        byte[] bytes,
        bool startsInTextMode)
    {
        if (bytes == null)
        {
            throw new BarcodeEncodingException("PDF417 byte-compaction input must not be null.");
        }

        if (bytes.Length == 0)
        {
            throw new BarcodeEncodingException("PDF417 byte compaction requires at least one byte.");
        }

        int modeCodeword;
        if (bytes.Length == 1 && startsInTextMode)
        {
            modeCodeword = ShiftFromTextCodeword;
        }
        else if (bytes.Length % BytesPerPackedGroup == 0)
        {
            modeCodeword = LatchSixByteCodeword;
        }
        else
        {
            modeCodeword = LatchWithRemainderCodeword;
        }

        return EncodeWithMode(bytes, modeCodeword);
    }

    internal static Pdf417ByteEncodingResult EncodeWithMode(
        byte[] bytes,
        int modeCodeword)
    {
        ValidateEncodingInput(bytes, modeCodeword);
        int packedGroupCount = modeCodeword == ShiftFromTextCodeword
            ? 0
            : bytes.Length / BytesPerPackedGroup;
        int remainderCount = bytes.Length - (packedGroupCount * BytesPerPackedGroup);
        int dataCodewordCount = CheckedMath.Add(
            CheckedMath.Multiply(packedGroupCount, CodewordsPerPackedGroup),
            remainderCount);
        int[] dataCodewords = new int[dataCodewordCount];
        int[] groupCodewords = new int[CodewordsPerPackedGroup];
        int byteOffset = 0;
        int codewordOffset = 0;
        int groupIndex = 0;
        while (groupIndex < packedGroupCount)
        {
            ulong value = 0;
            int byteIndex = 0;
            while (byteIndex < BytesPerPackedGroup)
            {
                value = (value << 8) | bytes[byteOffset + byteIndex];
                byteIndex = byteIndex + 1;
            }

            int digitIndex = CodewordsPerPackedGroup - 1;
            while (digitIndex >= 0)
            {
                groupCodewords[digitIndex] = (int)(value % CodewordBase);
                value = value / CodewordBase;
                digitIndex = digitIndex - 1;
            }

            digitIndex = 0;
            while (digitIndex < CodewordsPerPackedGroup)
            {
                dataCodewords[codewordOffset + digitIndex] = groupCodewords[digitIndex];
                digitIndex = digitIndex + 1;
            }

            byteOffset = byteOffset + BytesPerPackedGroup;
            codewordOffset = codewordOffset + CodewordsPerPackedGroup;
            groupIndex = groupIndex + 1;
        }

        while (byteOffset < bytes.Length)
        {
            dataCodewords[codewordOffset] = bytes[byteOffset];
            byteOffset = byteOffset + 1;
            codewordOffset = codewordOffset + 1;
        }

        int[] codewords = new int[CheckedMath.Add(dataCodewords.Length, 1)];
        codewords[0] = modeCodeword;
        CopyIntegersToOffset(dataCodewords, codewords, 1);
        Pdf417ByteEncodingResult result = new Pdf417ByteEncodingResult();
        result.Bytes = CopyBytes(bytes);
        result.ModeCodeword = modeCodeword;
        result.DataCodewords = dataCodewords;
        result.Codewords = codewords;
        result.ReturnsToTextMode = modeCodeword == ShiftFromTextCodeword;
        return result;
    }

    internal static Pdf417ByteDecodingResult DecodeCodewords(int[] codewords)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("PDF417 byte-compaction codewords must not be null.");
        }

        if (codewords.Length < 2)
        {
            throw new BarcodeDecodingException("PDF417 byte compaction requires a mode codeword and byte data.");
        }

        int[] dataCodewords = new int[codewords.Length - 1];
        int index = 1;
        while (index < codewords.Length)
        {
            dataCodewords[index - 1] = codewords[index];
            index = index + 1;
        }

        Pdf417ByteDecodingResult result = DecodeDataCodewords(
            dataCodewords,
            codewords[0]);
        result.ConsumedCodewordCount = codewords.Length;
        return result;
    }

    internal static Pdf417ByteDecodingResult DecodeDataCodewords(
        int[] dataCodewords,
        int modeCodeword)
    {
        ValidateDecodingInput(dataCodewords, modeCodeword);
        int packedGroupCount;
        int remainderCount;
        if (modeCodeword == ShiftFromTextCodeword)
        {
            packedGroupCount = 0;
            remainderCount = 1;
        }
        else if (modeCodeword == LatchSixByteCodeword)
        {
            packedGroupCount = dataCodewords.Length / CodewordsPerPackedGroup;
            remainderCount = 0;
        }
        else
        {
            packedGroupCount = (dataCodewords.Length - 1) / CodewordsPerPackedGroup;
            remainderCount = dataCodewords.Length
                - (packedGroupCount * CodewordsPerPackedGroup);
        }

        int byteCount = CheckedMath.Add(
            CheckedMath.Multiply(packedGroupCount, BytesPerPackedGroup),
            remainderCount);
        byte[] bytes = new byte[byteCount];
        int codewordOffset = 0;
        int byteOffset = 0;
        int groupIndex = 0;
        while (groupIndex < packedGroupCount)
        {
            ulong value = DecodePackedGroup(dataCodewords, codewordOffset);
            int byteIndex = BytesPerPackedGroup - 1;
            while (byteIndex >= 0)
            {
                bytes[byteOffset + byteIndex] = (byte)(value & 255);
                value = value >> 8;
                byteIndex = byteIndex - 1;
            }

            codewordOffset = codewordOffset + CodewordsPerPackedGroup;
            byteOffset = byteOffset + BytesPerPackedGroup;
            groupIndex = groupIndex + 1;
        }

        while (codewordOffset < dataCodewords.Length)
        {
            int codeword = dataCodewords[codewordOffset];
            if (codeword < 0 || codeword > 255)
            {
                throw new BarcodeDecodingException("PDF417 byte-compaction remainder codewords must be between 0 and 255.");
            }

            bytes[byteOffset] = (byte)codeword;
            codewordOffset = codewordOffset + 1;
            byteOffset = byteOffset + 1;
        }

        Pdf417ByteDecodingResult result = new Pdf417ByteDecodingResult();
        result.Bytes = bytes;
        result.ModeCodeword = modeCodeword;
        result.DataCodewords = CopyIntegers(dataCodewords);
        result.ConsumedCodewordCount = CheckedMath.Add(dataCodewords.Length, 1);
        result.ReturnsToTextMode = modeCodeword == ShiftFromTextCodeword;
        return result;
    }

    static ulong DecodePackedGroup(int[] codewords, int offset)
    {
        ulong value = 0;
        int index = 0;
        while (index < CodewordsPerPackedGroup)
        {
            int codeword = codewords[offset + index];
            if (codeword < 0 || codeword >= CodewordBase)
            {
                throw new BarcodeDecodingException("PDF417 packed byte codewords must be between 0 and 899.");
            }

            value = (value * CodewordBase) + (uint)codeword;
            index = index + 1;
        }

        if (value > MaximumPackedGroupValue)
        {
            throw new BarcodeDecodingException("PDF417 packed byte codewords exceed the six-byte range.");
        }

        return value;
    }

    static void ValidateEncodingInput(byte[] bytes, int modeCodeword)
    {
        if (bytes == null)
        {
            throw new BarcodeEncodingException("PDF417 byte-compaction input must not be null.");
        }

        if (bytes.Length == 0)
        {
            throw new BarcodeEncodingException("PDF417 byte compaction requires at least one byte.");
        }

        if (modeCodeword == ShiftFromTextCodeword)
        {
            if (bytes.Length != 1)
            {
                throw new BarcodeEncodingException("PDF417 byte shift 913 requires exactly one byte.");
            }

            return;
        }

        if (modeCodeword == LatchSixByteCodeword)
        {
            if (bytes.Length % BytesPerPackedGroup != 0)
            {
                throw new BarcodeEncodingException("PDF417 byte latch 924 requires complete six-byte groups.");
            }

            return;
        }

        if (modeCodeword == LatchWithRemainderCodeword)
        {
            if (bytes.Length % BytesPerPackedGroup == 0)
            {
                throw new BarcodeEncodingException("PDF417 byte latch 901 requires a one-to-five-byte remainder; use 924 for complete groups.");
            }

            return;
        }

        throw new BarcodeEncodingException("PDF417 byte-compaction mode codeword is invalid.");
    }

    static void ValidateDecodingInput(int[] codewords, int modeCodeword)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("PDF417 byte-compaction data codewords must not be null.");
        }

        if (codewords.Length == 0)
        {
            throw new BarcodeDecodingException("PDF417 byte compaction requires at least one data codeword.");
        }

        if (modeCodeword == ShiftFromTextCodeword)
        {
            if (codewords.Length != 1)
            {
                throw new BarcodeDecodingException("PDF417 byte shift 913 requires exactly one data codeword.");
            }

            return;
        }

        if (modeCodeword == LatchSixByteCodeword)
        {
            if (codewords.Length % CodewordsPerPackedGroup != 0)
            {
                throw new BarcodeDecodingException("PDF417 byte latch 924 requires complete five-codeword groups.");
            }

            return;
        }

        if (modeCodeword == LatchWithRemainderCodeword)
        {
            return;
        }

        throw new BarcodeDecodingException("PDF417 byte-compaction mode codeword is invalid.");
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
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
