namespace barcode;

internal class DataMatrixHighLevelDecodeResult
{
    internal byte[] RawBytes = new byte[0];
    internal string? Text;
    internal string? CharacterEncoding;
    internal EciSegment[] EciSegments = new EciSegment[0];
    internal bool IsGs1;
    internal bool ReaderProgramming;
    internal int MacroType;
    internal StructuredAppendInfo? StructuredAppend;
    internal int ConsumedCodewordCount;
}

internal class DataMatrixDecodedSegment
{
    internal byte[] Bytes = new byte[0];
    internal int NextCodewordIndex;
}

internal class DataMatrixDecodedEci
{
    internal int AssignmentNumber;
    internal int NextCodewordIndex;
}

internal class DataMatrixTextInterpretation
{
    internal string? Text;
    internal string? CharacterEncoding;
}

internal class DataMatrixEciEncoding
{
    internal global::System.Text.Encoding? Encoding;
    internal string? Name;
}

internal static class DataMatrixHighLevelDecoder
{
    internal const int DefaultEciAssignmentNumber = 3;
    internal const int BinaryEciAssignmentNumber = 899;

    internal static string? FindEciEncodingName(int assignmentNumber)
    {
        return GetEciEncoding(assignmentNumber).Name;
    }

    internal static DataMatrixHighLevelDecodeResult Decode(int[] dataCodewords)
    {
        ValidateInputCodewords(dataCodewords);
        DataMatrixDecodedByteBuffer output = new DataMatrixDecodedByteBuffer();
        output.Bytes = new byte[CheckedMath.Multiply(dataCodewords.Length, 2)];
        EciSegment[] eciBuffer = new EciSegment[dataCodewords.Length];
        int eciCount = 0;
        int index = 0;
        int firstFnc1Position = 0;
        bool isGs1 = false;
        bool readerProgramming = false;
        int macroType = 0;
        StructuredAppendInfo? structuredAppend = null;
        int consumedCodewordCount = dataCodewords.Length;
        bool finished = false;
        while (index < dataCodewords.Length && !finished)
        {
            int codeword = dataCodewords[index];
            if (codeword >= 1 && codeword <= 128)
            {
                output.AppendByte(codeword - 1);
                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.PadCodeword)
            {
                ValidatePadding(dataCodewords, index);
                consumedCodewordCount = index + 1;
                finished = true;
            }
            else if (codeword >= 130 && codeword <= 229)
            {
                int pair = codeword - 130;
                output.AppendByte(48 + (pair / 10));
                output.AppendByte(48 + (pair % 10));
                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.LatchToC40Codeword)
            {
                DataMatrixDecodedSegment segment = DecodeTripletSegment(
                    dataCodewords,
                    index,
                    DataMatrixEncodingMode.C40);
                AppendSegmentBytes(output, segment.Bytes);
                index = segment.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.LatchToBase256Codeword)
            {
                DataMatrixDecodedSegment segment = DecodeBase256Segment(dataCodewords, index);
                AppendSegmentBytes(output, segment.Bytes);
                index = segment.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.Fnc1Codeword)
            {
                if (index == firstFnc1Position)
                {
                    isGs1 = true;
                }
                else
                {
                    output.AppendByte(29);
                }

                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.StructuredAppendCodeword)
            {
                if (index != 0 || structuredAppend != null)
                {
                    throw new BarcodeDecodingException("Data Matrix structured append must be the first codeword.");
                }

                structuredAppend = DecodeStructuredAppend(dataCodewords);
                firstFnc1Position = 4;
                index = 4;
            }
            else if (codeword == DataMatrixHighLevelEncoder.ReaderProgrammingCodeword)
            {
                if (index != 0 || readerProgramming)
                {
                    throw new BarcodeDecodingException("Data Matrix reader programming must be the first codeword.");
                }

                readerProgramming = true;
                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.UpperShiftCodeword)
            {
                if (index + 1 >= dataCodewords.Length
                    || dataCodewords[index + 1] < 1
                    || dataCodewords[index + 1] > 128)
                {
                    throw new BarcodeDecodingException("Data Matrix ASCII Upper Shift is incomplete or invalid.");
                }

                output.AppendByte(dataCodewords[index + 1] + 127);
                index = index + 2;
            }
            else if (codeword == DataMatrixHighLevelEncoder.Macro05Codeword
                || codeword == DataMatrixHighLevelEncoder.Macro06Codeword)
            {
                if (index != 0 || macroType != 0)
                {
                    throw new BarcodeDecodingException("Data Matrix Macro 05 or Macro 06 must be the first codeword.");
                }

                macroType = codeword == DataMatrixHighLevelEncoder.Macro05Codeword ? 5 : 6;
                index = index + 1;
            }
            else if (codeword == DataMatrixHighLevelEncoder.LatchToX12Codeword)
            {
                DataMatrixDecodedSegment segment = DecodeTripletSegment(
                    dataCodewords,
                    index,
                    DataMatrixEncodingMode.X12);
                AppendSegmentBytes(output, segment.Bytes);
                index = segment.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.LatchToTextCodeword)
            {
                DataMatrixDecodedSegment segment = DecodeTripletSegment(
                    dataCodewords,
                    index,
                    DataMatrixEncodingMode.Text);
                AppendSegmentBytes(output, segment.Bytes);
                index = segment.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.LatchToEdifactCodeword)
            {
                DataMatrixDecodedSegment segment = DecodeEdifactSegment(dataCodewords, index);
                AppendSegmentBytes(output, segment.Bytes);
                index = segment.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.EciCodeword)
            {
                DataMatrixDecodedEci decodedEci = DecodeEci(dataCodewords, index);
                if (eciCount > 0)
                {
                    EciSegment previous = eciBuffer[eciCount - 1];
                    previous.ByteCount = output.Count - previous.ByteOffset;
                }

                EciSegment eciSegment = new EciSegment();
                eciSegment.AssignmentNumber = decodedEci.AssignmentNumber;
                eciSegment.ByteOffset = output.Count;
                eciBuffer[eciCount] = eciSegment;
                eciCount = eciCount + 1;
                index = decodedEci.NextCodewordIndex;
            }
            else if (codeword == DataMatrixHighLevelEncoder.UnlatchCodeword
                && index == dataCodewords.Length - 1)
            {
                index = index + 1;
            }
            else
            {
                throw new BarcodeDecodingException("Data Matrix ASCII encodation contains a reserved codeword.");
            }
        }

        if (eciCount > 0)
        {
            EciSegment finalSegment = eciBuffer[eciCount - 1];
            finalSegment.ByteCount = output.Count - finalSegment.ByteOffset;
        }

        byte[] rawBytes = output.CopyBytes();
        EciSegment[] eciSegments = CopyEciSegments(eciBuffer, eciCount);
        DataMatrixTextInterpretation interpretation = InterpretText(rawBytes, eciSegments);
        string? text = interpretation.Text;
        if (text != null && macroType != 0)
        {
            string macroIdentifier = macroType == 5 ? "05" : "06";
            text = "[)>\u001E" + macroIdentifier + "\u001D" + text + "\u001E\u0004";
        }

        DataMatrixHighLevelDecodeResult result = new DataMatrixHighLevelDecodeResult();
        result.RawBytes = rawBytes;
        result.Text = text;
        result.CharacterEncoding = interpretation.CharacterEncoding;
        result.EciSegments = eciSegments;
        result.IsGs1 = isGs1;
        result.ReaderProgramming = readerProgramming;
        result.MacroType = macroType;
        result.StructuredAppend = structuredAppend;
        result.ConsumedCodewordCount = consumedCodewordCount;
        return result;
    }

    static DataMatrixDecodedSegment DecodeTripletSegment(
        int[] codewords,
        int latchIndex,
        DataMatrixEncodingMode mode)
    {
        int index = latchIndex + 1;
        int pairCount = 0;
        bool foundUnlatch = false;
        while (index < codewords.Length)
        {
            if (codewords[index] == DataMatrixHighLevelEncoder.UnlatchCodeword)
            {
                foundUnlatch = true;
                break;
            }

            if (index + 1 >= codewords.Length)
            {
                break;
            }

            pairCount = pairCount + 1;
            index = index + 2;
        }

        if (pairCount == 0)
        {
            throw new BarcodeDecodingException("Data Matrix triplet mode contains no complete packed pair.");
        }

        int segmentEnd = foundUnlatch ? index + 1 : latchIndex + 1 + (pairCount * 2);
        int[] segmentCodewords = CopyCodewordRange(
            codewords,
            latchIndex,
            segmentEnd - latchIndex);
        byte[] bytes;
        if (mode == DataMatrixEncodingMode.C40)
        {
            bytes = DataMatrixModeCodec.DecodeC40(segmentCodewords);
        }
        else if (mode == DataMatrixEncodingMode.Text)
        {
            bytes = DataMatrixModeCodec.DecodeText(segmentCodewords);
        }
        else
        {
            bytes = DataMatrixModeCodec.DecodeX12(segmentCodewords);
        }

        DataMatrixDecodedSegment segment = new DataMatrixDecodedSegment();
        segment.Bytes = bytes;
        segment.NextCodewordIndex = segmentEnd;
        return segment;
    }

    static DataMatrixDecodedSegment DecodeEdifactSegment(int[] codewords, int latchIndex)
    {
        int dataStart = latchIndex + 1;
        int dataIndex = dataStart;
        int segmentEnd = dataStart;
        bool foundUnlatch = false;
        while (codewords.Length - dataIndex >= 3 && !foundUnlatch)
        {
            int bitOffset = 0;
            while (bitOffset < 24 && !foundUnlatch)
            {
                int value = ReadEdifactValue(codewords, dataIndex, bitOffset);
                bitOffset = bitOffset + 6;
                if (value == DataMatrixModeCodec.EdifactUnlatchValue)
                {
                    int consumed = (bitOffset + 7) / 8;
                    if (!HasZeroEdifactPadding(codewords, dataIndex, bitOffset, consumed * 8))
                    {
                        throw new BarcodeDecodingException("Data Matrix EDIFACT unlatch has nonzero padding bits.");
                    }

                    segmentEnd = dataIndex + consumed;
                    foundUnlatch = true;
                }
            }

            if (!foundUnlatch)
            {
                dataIndex = dataIndex + 3;
                segmentEnd = dataIndex;
            }
        }

        if (!foundUnlatch && dataIndex < codewords.Length)
        {
            int remainingCodewords = codewords.Length - dataIndex;
            int remainingBits = remainingCodewords * 8;
            int bitOffset = 0;
            while (remainingBits - bitOffset >= 6 && !foundUnlatch)
            {
                int value = ReadEdifactValue(codewords, dataIndex, bitOffset);
                bitOffset = bitOffset + 6;
                if (value == DataMatrixModeCodec.EdifactUnlatchValue
                    && HasZeroEdifactPadding(codewords, dataIndex, bitOffset, ((bitOffset + 7) / 8) * 8))
                {
                    segmentEnd = dataIndex + ((bitOffset + 7) / 8);
                    foundUnlatch = true;
                }
            }
        }

        if (segmentEnd == dataStart)
        {
            throw new BarcodeDecodingException("Data Matrix EDIFACT mode contains no decodable values.");
        }

        int[] segmentCodewords = CopyCodewordRange(
            codewords,
            latchIndex,
            segmentEnd - latchIndex);
        DataMatrixDecodedSegment segment = new DataMatrixDecodedSegment();
        segment.Bytes = DataMatrixModeCodec.DecodeEdifact(segmentCodewords);
        segment.NextCodewordIndex = segmentEnd;
        return segment;
    }

    static DataMatrixDecodedSegment DecodeBase256Segment(int[] codewords, int latchIndex)
    {
        if (latchIndex + 1 >= codewords.Length)
        {
            throw new BarcodeDecodingException("Data Matrix Base 256 segment is missing its length field.");
        }

        int firstLength = DataMatrixHighLevelEncoder.Derandomize255State(
            codewords[latchIndex + 1],
            latchIndex + 2);
        int lengthFieldCount = 1;
        int dataCount;
        if (firstLength == 0)
        {
            dataCount = codewords.Length - latchIndex - 2;
        }
        else if (firstLength <= 249)
        {
            dataCount = firstLength;
        }
        else
        {
            if (latchIndex + 2 >= codewords.Length)
            {
                throw new BarcodeDecodingException("Data Matrix Base 256 segment is missing its second length field.");
            }

            int secondLength = DataMatrixHighLevelEncoder.Derandomize255State(
                codewords[latchIndex + 2],
                latchIndex + 3);
            lengthFieldCount = 2;
            dataCount = (250 * (firstLength - 249)) + secondLength;
        }

        if (dataCount < 0 || dataCount > DataMatrixModeCodec.MaximumBase256DataCount)
        {
            throw new BarcodeDecodingException("Data Matrix Base 256 length is outside the supported range.");
        }

        int segmentLength = CheckedMath.Add(CheckedMath.Add(1, lengthFieldCount), dataCount);
        if (segmentLength > codewords.Length - latchIndex)
        {
            throw new BarcodeDecodingException("Data Matrix Base 256 segment is shorter than its length field.");
        }

        int[] segmentCodewords = CopyCodewordRange(codewords, latchIndex, segmentLength);
        DataMatrixDecodedSegment segment = new DataMatrixDecodedSegment();
        segment.Bytes = DataMatrixModeCodec.DecodeBase256(segmentCodewords, latchIndex + 1);
        segment.NextCodewordIndex = latchIndex + segmentLength;
        return segment;
    }

    static DataMatrixDecodedEci DecodeEci(int[] codewords, int eciIndex)
    {
        if (eciIndex + 1 >= codewords.Length)
        {
            throw new BarcodeDecodingException("Data Matrix ECI designator is truncated.");
        }

        int first = codewords[eciIndex + 1];
        DataMatrixDecodedEci result = new DataMatrixDecodedEci();
        if (first >= 1 && first <= 127)
        {
            result.AssignmentNumber = first - 1;
            result.NextCodewordIndex = eciIndex + 2;
            return result;
        }

        if (first >= 128 && first <= 191)
        {
            if (eciIndex + 2 >= codewords.Length
                || codewords[eciIndex + 2] < 1
                || codewords[eciIndex + 2] > 254)
            {
                throw new BarcodeDecodingException("Data Matrix two-codeword ECI designator is truncated or invalid.");
            }

            result.AssignmentNumber = ((first - 128) * 254) + codewords[eciIndex + 2] + 126;
            result.NextCodewordIndex = eciIndex + 3;
            return result;
        }

        if (first >= 192 && first <= 207)
        {
            if (eciIndex + 3 >= codewords.Length
                || codewords[eciIndex + 2] < 1
                || codewords[eciIndex + 2] > 254
                || codewords[eciIndex + 3] < 1
                || codewords[eciIndex + 3] > 254)
            {
                throw new BarcodeDecodingException("Data Matrix three-codeword ECI designator is truncated or invalid.");
            }

            result.AssignmentNumber = ((first - 192) * 64516)
                + ((codewords[eciIndex + 2] - 1) * 254)
                + codewords[eciIndex + 3]
                + 16382;
            if (result.AssignmentNumber > DataMatrixControlCodewordEncoder.MaximumEciAssignmentNumber)
            {
                throw new BarcodeDecodingException("Data Matrix ECI assignment exceeds 999999.");
            }

            result.NextCodewordIndex = eciIndex + 4;
            return result;
        }

        throw new BarcodeDecodingException("Data Matrix ECI designator begins with a reserved value.");
    }

    static StructuredAppendInfo DecodeStructuredAppend(int[] codewords)
    {
        if (codewords.Length < 4)
        {
            throw new BarcodeDecodingException("Data Matrix structured append header is truncated.");
        }

        int sequence = codewords[1];
        int index = sequence >> 4;
        int count = 17 - (sequence & 15);
        int firstFilePart = codewords[2];
        int secondFilePart = codewords[3];
        if (sequence < 1
            || sequence > 254
            || count < 2
            || count > DataMatrixControlCodewordEncoder.MaximumStructuredAppendCount
            || index < 0
            || index >= count
            || firstFilePart < 1
            || firstFilePart > 254
            || secondFilePart < 1
            || secondFilePart > 254)
        {
            throw new BarcodeDecodingException("Data Matrix structured append metadata is invalid.");
        }

        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = index;
        append.Count = count;
        append.FileIdentifier = FormatThreeDigits(firstFilePart) + FormatThreeDigits(secondFilePart);
        append.IsLastSegment = index == count - 1;
        return append;
    }

    static DataMatrixTextInterpretation InterpretText(byte[] rawBytes, EciSegment[] eciSegments)
    {
        global::System.Text.StringBuilder text = new global::System.Text.StringBuilder();
        int currentAssignment = DefaultEciAssignmentNumber;
        int byteOffset = 0;
        bool supported = true;
        int segmentIndex = 0;
        while (segmentIndex < eciSegments.Length)
        {
            EciSegment segment = eciSegments[segmentIndex];
            if (segment.ByteOffset < byteOffset || segment.ByteOffset > rawBytes.Length)
            {
                throw new BarcodeDecodingException("Data Matrix ECI byte ranges are inconsistent.");
            }

            supported = AppendInterpretedBytes(
                text,
                rawBytes,
                byteOffset,
                segment.ByteOffset - byteOffset,
                currentAssignment)
                && supported;
            currentAssignment = segment.AssignmentNumber;
            byteOffset = segment.ByteOffset;
            segmentIndex = segmentIndex + 1;
        }

        supported = AppendInterpretedBytes(
            text,
            rawBytes,
            byteOffset,
            rawBytes.Length - byteOffset,
            currentAssignment)
            && supported;
        DataMatrixTextInterpretation result = new DataMatrixTextInterpretation();
        result.Text = supported ? text.ToString() : null;
        result.CharacterEncoding = FindSingleCharacterEncoding(eciSegments);
        return result;
    }

    static bool AppendInterpretedBytes(
        global::System.Text.StringBuilder text,
        byte[] rawBytes,
        int startIndex,
        int count,
        int assignmentNumber)
    {
        if (count == 0)
        {
            return true;
        }

        DataMatrixEciEncoding eciEncoding = GetEciEncoding(assignmentNumber);
        if (eciEncoding.Encoding == null)
        {
            return false;
        }

        text.Append(eciEncoding.Encoding.GetString(rawBytes, startIndex, count));
        return true;
    }

    static DataMatrixEciEncoding GetEciEncoding(int assignmentNumber)
    {
        DataMatrixEciEncoding result = new DataMatrixEciEncoding();
        int codePage = 0;
        if (assignmentNumber == 0 || assignmentNumber == 2)
        {
            codePage = 437;
            result.Name = "IBM437";
        }
        else if (assignmentNumber == 1 || assignmentNumber == 3)
        {
            codePage = 28591;
            result.Name = "ISO-8859-1";
        }
        else if (assignmentNumber >= 4 && assignmentNumber <= 11)
        {
            codePage = 28588 + assignmentNumber;
            result.Name = "ISO-8859-" + (assignmentNumber - 2).ToString();
        }
        else if (assignmentNumber == 15 || assignmentNumber == 17)
        {
            codePage = 28588 + assignmentNumber;
            result.Name = "ISO-8859-" + (assignmentNumber - 2).ToString();
        }
        else if (assignmentNumber == 20)
        {
            codePage = 932;
            result.Name = "Shift_JIS";
        }
        else if (assignmentNumber >= 21 && assignmentNumber <= 23)
        {
            codePage = 1229 + assignmentNumber;
            result.Name = "windows-" + codePage.ToString();
        }
        else if (assignmentNumber == 24)
        {
            codePage = 1256;
            result.Name = "windows-1256";
        }
        else if (assignmentNumber == 25)
        {
            codePage = 1201;
            result.Name = "UTF-16BE";
        }
        else if (assignmentNumber == 26)
        {
            codePage = 65001;
            result.Name = "UTF-8";
        }
        else if (assignmentNumber == 27 || assignmentNumber == 170)
        {
            codePage = 20127;
            result.Name = "US-ASCII";
        }
        else if (assignmentNumber == 28)
        {
            codePage = 950;
            result.Name = "Big5";
        }
        else if (assignmentNumber == 29)
        {
            codePage = 54936;
            result.Name = "GB18030";
        }
        else if (assignmentNumber == 30)
        {
            codePage = 51949;
            result.Name = "EUC-KR";
        }
        else if (assignmentNumber == BinaryEciAssignmentNumber)
        {
            result.Name = "Binary";
            return result;
        }
        else
        {
            return result;
        }

        global::System.Text.Encoding.RegisterProvider(
            global::System.Text.CodePagesEncodingProvider.Instance);
        result.Encoding = global::System.Text.Encoding.GetEncoding(codePage);
        return result;
    }

    static string? FindSingleCharacterEncoding(EciSegment[] eciSegments)
    {
        int assignment = DefaultEciAssignmentNumber;
        if (eciSegments.Length > 0)
        {
            if (eciSegments[0].ByteOffset != 0)
            {
                return null;
            }

            assignment = eciSegments[0].AssignmentNumber;
            int index = 1;
            while (index < eciSegments.Length)
            {
                if (eciSegments[index].AssignmentNumber != assignment)
                {
                    return null;
                }

                index = index + 1;
            }
        }

        return GetEciEncoding(assignment).Name;
    }

    static void ValidatePadding(int[] codewords, int padIndex)
    {
        int index = padIndex + 1;
        while (index < codewords.Length)
        {
            int expected = DataMatrixHighLevelEncoder.Randomize253State(
                DataMatrixHighLevelEncoder.PadCodeword,
                index + 1);
            if (codewords[index] != expected)
            {
                throw new BarcodeDecodingException("Data Matrix randomized padding is invalid.");
            }

            index = index + 1;
        }
    }

    static void ValidateInputCodewords(int[] codewords)
    {
        if (codewords == null)
        {
            throw new BarcodeDecodingException("Data Matrix data codewords must not be null.");
        }

        if (codewords.Length == 0)
        {
            throw new BarcodeDecodingException("Data Matrix data codewords must not be empty.");
        }

        if (codewords.Length > DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity)
        {
            throw new BarcodeDecodingException("Data Matrix data codewords exceed the largest standard symbol.");
        }

        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > 255)
            {
                throw new BarcodeDecodingException("Data Matrix data codeword is outside the byte range.");
            }

            index = index + 1;
        }
    }

    static void AppendSegmentBytes(DataMatrixDecodedByteBuffer output, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            output.AppendByte(bytes[index]);
            index = index + 1;
        }
    }

    static int ReadEdifactValue(int[] codewords, int dataStart, int bitOffset)
    {
        int value = 0;
        int bit = 0;
        while (bit < 6)
        {
            int absoluteBit = bitOffset + bit;
            int codewordIndex = dataStart + (absoluteBit / 8);
            int bitInCodeword = 7 - (absoluteBit % 8);
            value = (value << 1) | ((codewords[codewordIndex] >> bitInCodeword) & 1);
            bit = bit + 1;
        }

        return value;
    }

    static bool HasZeroEdifactPadding(
        int[] codewords,
        int dataStart,
        int bitOffset,
        int endBitOffset)
    {
        int bit = bitOffset;
        while (bit < endBitOffset)
        {
            int codewordIndex = dataStart + (bit / 8);
            int bitInCodeword = 7 - (bit % 8);
            if (((codewords[codewordIndex] >> bitInCodeword) & 1) != 0)
            {
                return false;
            }

            bit = bit + 1;
        }

        return true;
    }

    static int[] CopyCodewordRange(int[] source, int startIndex, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[startIndex + index];
            index = index + 1;
        }

        return copy;
    }

    static EciSegment[] CopyEciSegments(EciSegment[] source, int count)
    {
        EciSegment[] copy = new EciSegment[count];
        int index = 0;
        while (index < count)
        {
            EciSegment segment = new EciSegment();
            segment.AssignmentNumber = source[index].AssignmentNumber;
            segment.ByteOffset = source[index].ByteOffset;
            segment.ByteCount = source[index].ByteCount;
            copy[index] = segment;
            index = index + 1;
        }

        return copy;
    }

    static string FormatThreeDigits(int value)
    {
        if (value < 10)
        {
            return "00" + value.ToString();
        }

        if (value < 100)
        {
            return "0" + value.ToString();
        }

        return value.ToString();
    }
}
