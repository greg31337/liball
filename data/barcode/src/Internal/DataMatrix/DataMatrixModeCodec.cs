namespace barcode;

internal class DataMatrixDecodedByteBuffer
{
    internal byte[] Bytes = new byte[0];
    internal int Count;

    internal void AppendByte(int value)
    {
        if (value < 0 || value > 255)
        {
            throw new BarcodeDecodingException("Decoded Data Matrix byte is outside the byte range.");
        }

        if (Count >= Bytes.Length)
        {
            throw new BarcodeDecodingException("Decoded Data Matrix segment exceeds its expected output bound.");
        }

        Bytes[Count] = (byte)value;
        Count = Count + 1;
    }

    internal byte[] CopyBytes()
    {
        byte[] copy = new byte[Count];
        int index = 0;
        while (index < Count)
        {
            copy[index] = Bytes[index];
            index = index + 1;
        }

        return copy;
    }
}

internal static class DataMatrixModeCodec
{
    internal const int C40Shift1Value = 0;
    internal const int C40Shift2Value = 1;
    internal const int C40Shift3Value = 2;
    internal const int C40UpperShiftValue = 30;
    internal const int EdifactUnlatchValue = 31;
    internal const int MaximumBase256DataCount = 1555;

    internal static int[] EncodeC40(byte[] data)
    {
        return EncodeC40Text(data, false);
    }

    internal static byte[] DecodeC40(int[] codewords)
    {
        return DecodeC40Text(codewords, false);
    }

    internal static int[] EncodeText(byte[] data)
    {
        return EncodeC40Text(data, true);
    }

    internal static byte[] DecodeText(int[] codewords)
    {
        return DecodeC40Text(codewords, true);
    }

    internal static int[] EncodeX12(byte[] data)
    {
        ValidateNonemptyEncodingData(data, "ANSI X12");
        int index = 0;
        while (index < data.Length)
        {
            GetX12Value(data[index]);
            index = index + 1;
        }

        int modeByteCount = data.Length - (data.Length % 3);
        if (modeByteCount == 0)
        {
            return DataMatrixHighLevelEncoder.EncodeAscii(data);
        }

        int[] packed = new int[(modeByteCount / 3) * 2];
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < modeByteCount)
        {
            int first = GetX12Value(data[inputIndex]);
            int second = GetX12Value(data[inputIndex + 1]);
            int third = GetX12Value(data[inputIndex + 2]);
            int packedValue = (1600 * first) + (40 * second) + third + 1;
            packed[outputIndex] = packedValue / 256;
            packed[outputIndex + 1] = packedValue % 256;
            inputIndex = inputIndex + 3;
            outputIndex = outputIndex + 2;
        }

        int[] tail = DataMatrixHighLevelEncoder.EncodeAsciiSegment(
            data,
            modeByteCount,
            data.Length - modeByteCount);
        return FrameTripletSegment(
            DataMatrixHighLevelEncoder.LatchToX12Codeword,
            packed,
            tail);
    }

    internal static byte[] DecodeX12(int[] codewords)
    {
        ValidateNonemptyCodewords(codewords, "ANSI X12");
        if (codewords[0] != DataMatrixHighLevelEncoder.LatchToX12Codeword)
        {
            return DecodeAsciiOnly(codewords);
        }

        DataMatrixDecodedByteBuffer output = CreateDecodeBuffer(codewords.Length);
        int index = 1;
        while (index < codewords.Length && codewords[index] != DataMatrixHighLevelEncoder.UnlatchCodeword)
        {
            if (index + 1 >= codewords.Length)
            {
                throw new BarcodeDecodingException("ANSI X12 segment ends with an incomplete packed pair.");
            }

            int[] values = UnpackTriplet(codewords[index], codewords[index + 1]);
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                output.AppendByte(GetX12Byte(values[valueIndex]));
                valueIndex = valueIndex + 1;
            }

            index = index + 2;
        }

        if (index < codewords.Length)
        {
            DecodeAsciiTail(codewords, index + 1, output);
        }

        return output.CopyBytes();
    }

    internal static int[] EncodeEdifact(byte[] data)
    {
        ValidateNonemptyEncodingData(data, "EDIFACT");
        int index = 0;
        while (index < data.Length)
        {
            if (data[index] < 32 || data[index] > 94)
            {
                throw new BarcodeEncodingException("Data Matrix EDIFACT supports only ASCII bytes 32 through 94.");
            }

            index = index + 1;
        }

        int valueCount = CheckedMath.Add(data.Length, 1);
        int packedCount = CheckedMath.Add(CheckedMath.Multiply(valueCount, 6), 7) / 8;
        int[] codewords = new int[CheckedMath.Add(packedCount, 1)];
        codewords[0] = DataMatrixHighLevelEncoder.LatchToEdifactCodeword;
        int inputIndex = 0;
        int outputIndex = 1;
        while (data.Length - inputIndex >= 4)
        {
            int first = data[inputIndex] & 0x3F;
            int second = data[inputIndex + 1] & 0x3F;
            int third = data[inputIndex + 2] & 0x3F;
            int fourth = data[inputIndex + 3] & 0x3F;
            int packedValue = (first << 18) | (second << 12) | (third << 6) | fourth;
            codewords[outputIndex] = (packedValue >> 16) & 0xFF;
            codewords[outputIndex + 1] = (packedValue >> 8) & 0xFF;
            codewords[outputIndex + 2] = packedValue & 0xFF;
            inputIndex = inputIndex + 4;
            outputIndex = outputIndex + 3;
        }

        int remaining = data.Length - inputIndex;
        int[] finalValues = new int[4];
        index = 0;
        while (index < remaining)
        {
            finalValues[index] = data[inputIndex + index] & 0x3F;
            index = index + 1;
        }

        finalValues[remaining] = EdifactUnlatchValue;
        int finalValueCount = remaining + 1;
        int finalPacked = (finalValues[0] << 18)
            | (finalValues[1] << 12)
            | (finalValues[2] << 6)
            | finalValues[3];
        codewords[outputIndex] = (finalPacked >> 16) & 0xFF;
        if (finalValueCount >= 2)
        {
            codewords[outputIndex + 1] = (finalPacked >> 8) & 0xFF;
        }

        if (finalValueCount >= 3)
        {
            codewords[outputIndex + 2] = finalPacked & 0xFF;
        }

        return codewords;
    }

    internal static byte[] DecodeEdifact(int[] codewords)
    {
        ValidateNonemptyCodewords(codewords, "EDIFACT");
        if (codewords[0] != DataMatrixHighLevelEncoder.LatchToEdifactCodeword)
        {
            throw new BarcodeDecodingException("EDIFACT segment does not begin with the EDIFACT latch.");
        }

        ValidateCodewordRange(codewords);
        DataMatrixDecodedByteBuffer output = CreateDecodeBuffer(codewords.Length);
        int dataCodewordCount = codewords.Length - 1;
        int bitLength = CheckedMath.Multiply(dataCodewordCount, 8);
        int bitOffset = 0;
        bool foundUnlatch = false;
        while (bitLength - bitOffset >= 6)
        {
            int value = ReadEdifactValue(codewords, bitOffset);
            bitOffset = bitOffset + 6;
            if (value == EdifactUnlatchValue)
            {
                foundUnlatch = true;
                break;
            }

            if ((value & 0x20) == 0)
            {
                value = value | 0x40;
            }

            output.AppendByte(value);
        }

        if (!foundUnlatch && dataCodewordCount % 3 != 0)
        {
            throw new BarcodeDecodingException("EDIFACT segment is missing its unlatch value.");
        }

        if (foundUnlatch && bitLength - bitOffset > 6)
        {
            throw new BarcodeDecodingException("EDIFACT segment contains codewords after its unlatch padding.");
        }

        while (foundUnlatch && bitOffset < bitLength)
        {
            if (ReadEdifactBit(codewords, bitOffset) != 0)
            {
                throw new BarcodeDecodingException("EDIFACT segment has nonzero bits after its unlatch value.");
            }

            bitOffset = bitOffset + 1;
        }

        return output.CopyBytes();
    }

    internal static int[] EncodeBase256(byte[] data, int latchCodewordPosition)
    {
        ValidateNonemptyEncodingData(data, "Base 256");
        ValidateBase256Position(latchCodewordPosition);
        if (data.Length > MaximumBase256DataCount)
        {
            throw new BarcodeEncodingException("Data Matrix Base 256 segment exceeds 1555 data bytes.");
        }

        int lengthFieldCount = data.Length <= 249 ? 1 : 2;
        int[] codewords = new int[CheckedMath.Add(CheckedMath.Add(data.Length, lengthFieldCount), 1)];
        codewords[0] = DataMatrixHighLevelEncoder.LatchToBase256Codeword;
        int outputIndex = 1;
        if (lengthFieldCount == 1)
        {
            codewords[outputIndex] = DataMatrixHighLevelEncoder.Randomize255State(
                data.Length,
                CheckedMath.Add(latchCodewordPosition, outputIndex));
            outputIndex = outputIndex + 1;
        }
        else
        {
            int firstLength = (data.Length / 250) + 249;
            int secondLength = data.Length % 250;
            codewords[outputIndex] = DataMatrixHighLevelEncoder.Randomize255State(
                firstLength,
                CheckedMath.Add(latchCodewordPosition, outputIndex));
            outputIndex = outputIndex + 1;
            codewords[outputIndex] = DataMatrixHighLevelEncoder.Randomize255State(
                secondLength,
                CheckedMath.Add(latchCodewordPosition, outputIndex));
            outputIndex = outputIndex + 1;
        }

        int inputIndex = 0;
        while (inputIndex < data.Length)
        {
            codewords[outputIndex] = DataMatrixHighLevelEncoder.Randomize255State(
                data[inputIndex],
                CheckedMath.Add(latchCodewordPosition, outputIndex));
            inputIndex = inputIndex + 1;
            outputIndex = outputIndex + 1;
        }

        return codewords;
    }

    internal static byte[] DecodeBase256(int[] codewords, int latchCodewordPosition)
    {
        ValidateNonemptyCodewords(codewords, "Base 256");
        ValidateBase256Position(latchCodewordPosition);
        ValidateCodewordRange(codewords);
        if (codewords[0] != DataMatrixHighLevelEncoder.LatchToBase256Codeword)
        {
            throw new BarcodeDecodingException("Base 256 segment does not begin with the Base 256 latch.");
        }

        if (codewords.Length < 2)
        {
            throw new BarcodeDecodingException("Base 256 segment is missing its length field.");
        }

        int inputIndex = 1;
        int firstLength = DataMatrixHighLevelEncoder.Derandomize255State(
            codewords[inputIndex],
            CheckedMath.Add(latchCodewordPosition, inputIndex));
        inputIndex = inputIndex + 1;
        int dataCount;
        if (firstLength == 0)
        {
            dataCount = codewords.Length - inputIndex;
        }
        else if (firstLength <= 249)
        {
            dataCount = firstLength;
        }
        else
        {
            if (inputIndex >= codewords.Length)
            {
                throw new BarcodeDecodingException("Base 256 segment is missing its second length codeword.");
            }

            int secondLength = DataMatrixHighLevelEncoder.Derandomize255State(
                codewords[inputIndex],
                CheckedMath.Add(latchCodewordPosition, inputIndex));
            inputIndex = inputIndex + 1;
            dataCount = (250 * (firstLength - 249)) + secondLength;
        }

        if (dataCount < 0 || dataCount > MaximumBase256DataCount)
        {
            throw new BarcodeDecodingException("Base 256 length field is outside the supported range.");
        }

        if (codewords.Length - inputIndex != dataCount)
        {
            throw new BarcodeDecodingException("Base 256 length field does not match the segment data length.");
        }

        byte[] data = new byte[dataCount];
        int outputIndex = 0;
        while (outputIndex < data.Length)
        {
            data[outputIndex] = (byte)DataMatrixHighLevelEncoder.Derandomize255State(
                codewords[inputIndex],
                CheckedMath.Add(latchCodewordPosition, inputIndex));
            inputIndex = inputIndex + 1;
            outputIndex = outputIndex + 1;
        }

        return data;
    }

    static int[] EncodeC40Text(byte[] data, bool textMode)
    {
        ValidateNonemptyEncodingData(data, textMode ? "Text" : "C40");
        int maximumValueCount = CheckedMath.Multiply(data.Length, 4);
        int[] values = new int[maximumValueCount];
        int[] cumulativeValueCounts = new int[CheckedMath.Add(data.Length, 1)];
        int byteIndex = 0;
        int valueCount = 0;
        while (byteIndex < data.Length)
        {
            valueCount = AppendC40TextValues(data[byteIndex], textMode, values, valueCount);
            cumulativeValueCounts[byteIndex + 1] = valueCount;
            byteIndex = byteIndex + 1;
        }

        int modeByteCount = data.Length;
        while (modeByteCount > 0 && cumulativeValueCounts[modeByteCount] % 3 != 0)
        {
            modeByteCount = modeByteCount - 1;
        }

        if (modeByteCount == 0)
        {
            return DataMatrixHighLevelEncoder.EncodeAscii(data);
        }

        int modeValueCount = cumulativeValueCounts[modeByteCount];
        int[] packed = PackTriplets(values, modeValueCount);
        int[] tail = DataMatrixHighLevelEncoder.EncodeAsciiSegment(
            data,
            modeByteCount,
            data.Length - modeByteCount);
        int latch = textMode
            ? DataMatrixHighLevelEncoder.LatchToTextCodeword
            : DataMatrixHighLevelEncoder.LatchToC40Codeword;
        return FrameTripletSegment(latch, packed, tail);
    }

    static byte[] DecodeC40Text(int[] codewords, bool textMode)
    {
        string modeName = textMode ? "Text" : "C40";
        ValidateNonemptyCodewords(codewords, modeName);
        int expectedLatch = textMode
            ? DataMatrixHighLevelEncoder.LatchToTextCodeword
            : DataMatrixHighLevelEncoder.LatchToC40Codeword;
        if (codewords[0] != expectedLatch)
        {
            return DecodeAsciiOnly(codewords);
        }

        DataMatrixDecodedByteBuffer output = CreateDecodeBuffer(codewords.Length);
        int shift = 0;
        bool upperShift = false;
        int index = 1;
        while (index < codewords.Length && codewords[index] != DataMatrixHighLevelEncoder.UnlatchCodeword)
        {
            if (index + 1 >= codewords.Length)
            {
                throw new BarcodeDecodingException(modeName + " segment ends with an incomplete packed pair.");
            }

            int[] values = UnpackTriplet(codewords[index], codewords[index + 1]);
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                int value = values[valueIndex];
                if (shift == 0)
                {
                    if (value <= 2)
                    {
                        shift = value + 1;
                    }
                    else
                    {
                        int decoded = DecodeC40TextBasicValue(value, textMode);
                        AppendC40TextByte(output, decoded, upperShift);
                        upperShift = false;
                    }
                }
                else if (shift == 1)
                {
                    AppendC40TextByte(output, value, upperShift);
                    upperShift = false;
                    shift = 0;
                }
                else if (shift == 2)
                {
                    if (value == C40UpperShiftValue)
                    {
                        if (upperShift)
                        {
                            throw new BarcodeDecodingException(modeName + " segment repeats Upper Shift without data.");
                        }

                        upperShift = true;
                    }
                    else
                    {
                        int decoded = DecodeC40TextShift2Value(value);
                        AppendC40TextByte(output, decoded, upperShift);
                        upperShift = false;
                    }

                    shift = 0;
                }
                else
                {
                    int decoded = DecodeC40TextShift3Value(value, textMode);
                    AppendC40TextByte(output, decoded, upperShift);
                    upperShift = false;
                    shift = 0;
                }

                valueIndex = valueIndex + 1;
            }

            index = index + 2;
        }

        if (shift != 0 || upperShift)
        {
            throw new BarcodeDecodingException(modeName + " segment ends with an incomplete shift sequence.");
        }

        if (index < codewords.Length)
        {
            DecodeAsciiTail(codewords, index + 1, output);
        }

        return output.CopyBytes();
    }

    static int AppendC40TextValues(int byteValue, bool textMode, int[] values, int startIndex)
    {
        int index = startIndex;
        int value = byteValue;
        if (value >= 128)
        {
            values[index] = C40Shift2Value;
            values[index + 1] = C40UpperShiftValue;
            index = index + 2;
            value = value - 128;
        }

        if (value <= 31)
        {
            values[index] = C40Shift1Value;
            values[index + 1] = value;
            return index + 2;
        }

        if (value == 32)
        {
            values[index] = 3;
            return index + 1;
        }

        if (value >= 48 && value <= 57)
        {
            values[index] = value - 44;
            return index + 1;
        }

        if (!textMode && value >= 65 && value <= 90)
        {
            values[index] = value - 51;
            return index + 1;
        }

        if (textMode && value >= 97 && value <= 122)
        {
            values[index] = value - 83;
            return index + 1;
        }

        if (value >= 33 && value <= 47)
        {
            values[index] = C40Shift2Value;
            values[index + 1] = value - 33;
            return index + 2;
        }

        if (value >= 58 && value <= 64)
        {
            values[index] = C40Shift2Value;
            values[index + 1] = value - 43;
            return index + 2;
        }

        if (value >= 91 && value <= 95)
        {
            values[index] = C40Shift2Value;
            values[index + 1] = value - 69;
            return index + 2;
        }

        values[index] = C40Shift3Value;
        if (!textMode)
        {
            values[index + 1] = value - 96;
        }
        else if (value == 96)
        {
            values[index + 1] = 0;
        }
        else if (value >= 65 && value <= 90)
        {
            values[index + 1] = value - 64;
        }
        else
        {
            values[index + 1] = value - 96;
        }

        return index + 2;
    }

    static int[] PackTriplets(int[] values, int valueCount)
    {
        int[] packed = new int[(valueCount / 3) * 2];
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < valueCount)
        {
            int packedValue = (1600 * values[inputIndex])
                + (40 * values[inputIndex + 1])
                + values[inputIndex + 2]
                + 1;
            packed[outputIndex] = packedValue / 256;
            packed[outputIndex + 1] = packedValue % 256;
            inputIndex = inputIndex + 3;
            outputIndex = outputIndex + 2;
        }

        return packed;
    }

    static int[] UnpackTriplet(int firstCodeword, int secondCodeword)
    {
        ValidateCodeword(firstCodeword);
        ValidateCodeword(secondCodeword);
        int packedValue = (firstCodeword * 256) + secondCodeword - 1;
        if (packedValue < 0 || packedValue > 63999)
        {
            throw new BarcodeDecodingException("Data Matrix packed triplet is outside the value range.");
        }

        int[] values = new int[3];
        values[0] = packedValue / 1600;
        int remainder = packedValue - (values[0] * 1600);
        values[1] = remainder / 40;
        values[2] = remainder - (values[1] * 40);
        return values;
    }

    static int[] FrameTripletSegment(int latch, int[] packed, int[] tail)
    {
        int length = CheckedMath.Add(CheckedMath.Add(packed.Length, tail.Length), 2);
        int[] codewords = new int[length];
        codewords[0] = latch;
        int outputIndex = 1;
        int index = 0;
        while (index < packed.Length)
        {
            codewords[outputIndex] = packed[index];
            outputIndex = outputIndex + 1;
            index = index + 1;
        }

        codewords[outputIndex] = DataMatrixHighLevelEncoder.UnlatchCodeword;
        outputIndex = outputIndex + 1;
        index = 0;
        while (index < tail.Length)
        {
            codewords[outputIndex] = tail[index];
            outputIndex = outputIndex + 1;
            index = index + 1;
        }

        return codewords;
    }

    static byte[] DecodeAsciiOnly(int[] codewords)
    {
        DataMatrixDecodedByteBuffer output = CreateDecodeBuffer(codewords.Length);
        DecodeAsciiTail(codewords, 0, output);
        return output.CopyBytes();
    }

    static void DecodeAsciiTail(
        int[] codewords,
        int startIndex,
        DataMatrixDecodedByteBuffer output)
    {
        int index = startIndex;
        while (index < codewords.Length)
        {
            int codeword = codewords[index];
            ValidateCodeword(codeword);
            if (codeword >= 1 && codeword <= 128)
            {
                output.AppendByte(codeword - 1);
                index = index + 1;
            }
            else if (codeword >= 130 && codeword <= 229)
            {
                int pair = codeword - 130;
                output.AppendByte(48 + (pair / 10));
                output.AppendByte(48 + (pair % 10));
                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.UpperShiftCodeword)
            {
                if (index + 1 >= codewords.Length
                    || codewords[index + 1] < 1
                    || codewords[index + 1] > 128)
                {
                    throw new BarcodeDecodingException("Data Matrix ASCII tail has an invalid Upper Shift sequence.");
                }

                output.AppendByte(codewords[index + 1] + 127);
                index = index + 2;
            }
            else
            {
                throw new BarcodeDecodingException("Data Matrix ASCII tail contains a reserved codeword.");
            }
        }
    }

    static int DecodeC40TextBasicValue(int value, bool textMode)
    {
        if (value == 3)
        {
            return 32;
        }

        if (value >= 4 && value <= 13)
        {
            return value + 44;
        }

        if (value >= 14 && value <= 39)
        {
            return textMode ? value + 83 : value + 51;
        }

        throw new BarcodeDecodingException("Data Matrix C40/Text basic value is invalid.");
    }

    static int DecodeC40TextShift2Value(int value)
    {
        if (value >= 0 && value <= 14)
        {
            return value + 33;
        }

        if (value >= 15 && value <= 21)
        {
            return value + 43;
        }

        if (value >= 22 && value <= 26)
        {
            return value + 69;
        }

        if (value == 27)
        {
            return 29;
        }

        throw new BarcodeDecodingException("Data Matrix C40/Text Shift 2 value is reserved.");
    }

    static int DecodeC40TextShift3Value(int value, bool textMode)
    {
        if (!textMode)
        {
            return value + 96;
        }

        if (value == 0 || value >= 27)
        {
            return value + 96;
        }

        return value + 64;
    }

    static void AppendC40TextByte(
        DataMatrixDecodedByteBuffer output,
        int value,
        bool upperShift)
    {
        if (upperShift)
        {
            value = value + 128;
        }

        output.AppendByte(value);
    }

    static int GetX12Value(int byteValue)
    {
        if (byteValue == 13)
        {
            return 0;
        }

        if (byteValue == 42)
        {
            return 1;
        }

        if (byteValue == 62)
        {
            return 2;
        }

        if (byteValue == 32)
        {
            return 3;
        }

        if (byteValue >= 48 && byteValue <= 57)
        {
            return byteValue - 44;
        }

        if (byteValue >= 65 && byteValue <= 90)
        {
            return byteValue - 51;
        }

        throw new BarcodeEncodingException("Data Matrix ANSI X12 byte is outside the X12 character set.");
    }

    static int GetX12Byte(int value)
    {
        if (value == 0)
        {
            return 13;
        }

        if (value == 1)
        {
            return 42;
        }

        if (value == 2)
        {
            return 62;
        }

        if (value == 3)
        {
            return 32;
        }

        if (value >= 4 && value <= 13)
        {
            return value + 44;
        }

        if (value >= 14 && value <= 39)
        {
            return value + 51;
        }

        throw new BarcodeDecodingException("Data Matrix ANSI X12 value is invalid.");
    }

    static int ReadEdifactValue(int[] codewords, int bitOffset)
    {
        int value = 0;
        int bit = 0;
        while (bit < 6)
        {
            value = (value << 1) | ReadEdifactBit(codewords, bitOffset + bit);
            bit = bit + 1;
        }

        return value;
    }

    static int ReadEdifactBit(int[] codewords, int bitOffset)
    {
        int codewordIndex = 1 + (bitOffset / 8);
        int bitInCodeword = 7 - (bitOffset % 8);
        return (codewords[codewordIndex] >> bitInCodeword) & 1;
    }

    static DataMatrixDecodedByteBuffer CreateDecodeBuffer(int codewordCount)
    {
        DataMatrixDecodedByteBuffer output = new DataMatrixDecodedByteBuffer();
        output.Bytes = new byte[CheckedMath.Multiply(codewordCount, 2)];
        return output;
    }

    static void ValidateNonemptyEncodingData(byte[] data, string modeName)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("Data Matrix " + modeName + " data must not be null.");
        }

        if (data.Length == 0)
        {
            throw new BarcodeEncodingException("Data Matrix " + modeName + " data must not be empty.");
        }
    }

    static void ValidateNonemptyCodewords(int[] codewords, string modeName)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("Data Matrix " + modeName + " codewords must not be null.");
        }

        if (codewords.Length == 0)
        {
            throw new BarcodeDecodingException("Data Matrix " + modeName + " codewords must not be empty.");
        }

        ValidateCodewordRange(codewords);
    }

    static void ValidateCodewordRange(int[] codewords)
    {
        int index = 0;
        while (index < codewords.Length)
        {
            ValidateCodeword(codewords[index]);
            index = index + 1;
        }
    }

    static void ValidateCodeword(int codeword)
    {
        if (codeword < 0 || codeword > 255)
        {
            throw new BarcodeDecodingException("Data Matrix codeword must be zero through 255.");
        }
    }

    static void ValidateBase256Position(int latchCodewordPosition)
    {
        if (latchCodewordPosition <= 0)
        {
            throw new BarcodeValidationException("Base 256 latch codeword position must be positive and one-based.");
        }
    }
}
