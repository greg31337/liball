namespace barcode;

internal class Pdf417HighLevelDecodeResult
{
    internal byte[] RawBytes = new byte[0];
    internal string? Text;
    internal string? CharacterEncoding;
    internal EciSegment[] EciSegments = new EciSegment[0];
    internal Pdf417CompactionMode[] SegmentModes = new Pdf417CompactionMode[0];
}

internal class Pdf417DecodedByteBuffer
{
    internal byte[] Bytes;
    internal int Count;

    internal Pdf417DecodedByteBuffer(int capacity)
    {
        Bytes = new byte[capacity];
    }

    internal void AppendByteValue(int value)
    {
        if (value < 0 || value > 255 || Count >= Bytes.Length)
        {
            throw new BarcodeDecodingException("PDF417 decoded byte output exceeds its valid range.");
        }

        Bytes[Count] = (byte)value;
        Count = Count + 1;
    }

    internal void AppendByteArray(byte[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            AppendByteValue(values[index]);
            index = index + 1;
        }
    }
}

internal class Pdf417DecodedEci
{
    internal int AssignmentNumber;
    internal int NextCodewordIndex;
}

internal class Pdf417TextInterpretation
{
    internal string? Text;
    internal string? CharacterEncoding;
}

internal static class Pdf417HighLevelDecoder
{
    internal const int DefaultEciAssignmentNumber = 3;

    internal static Pdf417HighLevelDecodeResult DecodeHighLevelCodewords(
        int[] codewords)
    {
        ValidateHighLevelInput(codewords);
        Pdf417DecodedByteBuffer output = new Pdf417DecodedByteBuffer(
            CheckedMath.Multiply(codewords.Length, 4));
        EciSegment[] eciBuffer = new EciSegment[codewords.Length];
        Pdf417CompactionMode[] modeBuffer = new Pdf417CompactionMode[codewords.Length];
        int eciCount = 0;
        int modeCount = 0;
        int index = 0;
        Pdf417CompactionMode mode = Pdf417CompactionMode.Text;
        Pdf417TextSubmode textSubmode = Pdf417TextSubmode.Alpha;
        while (index < codewords.Length)
        {
            int codeword = codewords[index];
            if (codeword == Pdf417TextCompaction.TextLatchCodeword)
            {
                mode = Pdf417CompactionMode.Text;
                textSubmode = Pdf417TextSubmode.Alpha;
                index = index + 1;
            }
            else if (codeword == Pdf417ByteCompaction.LatchWithRemainderCodeword
                || codeword == Pdf417ByteCompaction.LatchSixByteCodeword)
            {
                mode = Pdf417CompactionMode.Byte;
                index = index + 1;
            }
            else if (codeword == Pdf417ByteCompaction.ShiftFromTextCodeword)
            {
                if (mode != Pdf417CompactionMode.Text
                    || index + 1 >= codewords.Length
                    || codewords[index + 1] < 0
                    || codewords[index + 1] > 255)
                {
                    throw new BarcodeDecodingException("PDF417 byte shift must occur in text mode and contain one byte.");
                }

                modeBuffer[modeCount] = Pdf417CompactionMode.Byte;
                modeCount = modeCount + 1;
                output.AppendByteValue(codewords[index + 1]);
                index = index + 2;
            }
            else if (codeword == Pdf417NumericCompaction.NumericLatchCodeword)
            {
                mode = Pdf417CompactionMode.Numeric;
                index = index + 1;
            }
            else if (codeword == Pdf417CompactionPlanner.CharacterSetEciCodeword
                || codeword == Pdf417CompactionPlanner.GeneralPurposeEciCodeword
                || codeword == Pdf417CompactionPlanner.UserDefinedEciCodeword)
            {
                Pdf417DecodedEci decodedEci = DecodeEciCodewords(
                    codewords,
                    index);
                if (eciCount > 0)
                {
                    EciSegment previous = eciBuffer[eciCount - 1];
                    previous.ByteCount = output.Count - previous.ByteOffset;
                }

                EciSegment segment = new EciSegment();
                segment.AssignmentNumber = decodedEci.AssignmentNumber;
                segment.ByteOffset = output.Count;
                eciBuffer[eciCount] = segment;
                eciCount = eciCount + 1;
                index = decodedEci.NextCodewordIndex;
            }
            else if (codeword >= Pdf417TextCompaction.TextLatchCodeword)
            {
                throw new BarcodeDecodingException("PDF417 high-level data contains a reserved control codeword.");
            }
            else
            {
                int end = FindNextControlCodeword(codewords, index);
                int[] segmentCodewords = CopyHighLevelRange(
                    codewords,
                    index,
                    end - index);
                modeBuffer[modeCount] = mode;
                modeCount = modeCount + 1;
                if (mode == Pdf417CompactionMode.Text)
                {
                    Pdf417TextDecodingResult text =
                        Pdf417TextCompaction.DecodeCodewords(
                            segmentCodewords,
                            textSubmode);
                    textSubmode = text.FinalSubmode;
                    int characterIndex = 0;
                    while (characterIndex < text.Text.Length)
                    {
                        char character = text.Text[characterIndex];
                        if (character > 255)
                        {
                            throw new BarcodeDecodingException("PDF417 text compaction produced a character outside the byte repertoire.");
                        }

                        output.AppendByteValue(character);
                        characterIndex = characterIndex + 1;
                    }
                }
                else if (mode == Pdf417CompactionMode.Byte)
                {
                    int byteMode = FindByteModeCodeword(codewords, index);
                    Pdf417ByteDecodingResult bytes =
                        Pdf417ByteCompaction.DecodeDataCodewords(
                            segmentCodewords,
                            byteMode);
                    output.AppendByteArray(bytes.Bytes);
                }
                else
                {
                    Pdf417NumericDecodingResult numeric =
                        Pdf417NumericCompaction.DecodeDataCodewords(
                            segmentCodewords);
                    int digitIndex = 0;
                    while (digitIndex < numeric.Digits.Length)
                    {
                        output.AppendByteValue(numeric.Digits[digitIndex]);
                        digitIndex = digitIndex + 1;
                    }
                }

                index = end;
            }
        }

        if (eciCount > 0)
        {
            EciSegment finalSegment = eciBuffer[eciCount - 1];
            finalSegment.ByteCount = output.Count - finalSegment.ByteOffset;
        }

        byte[] rawBytes = CopyDecodedBytes(output);
        if (rawBytes.Length == 0)
        {
            throw new BarcodeDecodingException("PDF417 high-level data does not contain a payload.");
        }

        EciSegment[] eciSegments = CopyDecodedEciSegments(eciBuffer, eciCount);
        Pdf417TextInterpretation interpretation = InterpretDecodedText(
            rawBytes,
            eciSegments);
        Pdf417HighLevelDecodeResult result = new Pdf417HighLevelDecodeResult();
        result.RawBytes = rawBytes;
        result.Text = interpretation.Text;
        result.CharacterEncoding = interpretation.CharacterEncoding;
        result.EciSegments = eciSegments;
        result.SegmentModes = CopyDecodedModes(modeBuffer, modeCount);
        return result;
    }

    static Pdf417DecodedEci DecodeEciCodewords(
        int[] codewords,
        int index)
    {
        int control = codewords[index];
        int assignment;
        int nextIndex;
        if (control == Pdf417CompactionPlanner.CharacterSetEciCodeword)
        {
            if (index + 1 >= codewords.Length
                || codewords[index + 1] < 0
                || codewords[index + 1] >= 900)
            {
                throw new BarcodeDecodingException("PDF417 character-set ECI designator is incomplete or invalid.");
            }

            assignment = codewords[index + 1];
            nextIndex = index + 2;
        }
        else if (control == Pdf417CompactionPlanner.GeneralPurposeEciCodeword)
        {
            if (index + 2 >= codewords.Length
                || codewords[index + 1] < 0
                || codewords[index + 1] >= 900
                || codewords[index + 2] < 0
                || codewords[index + 2] >= 900)
            {
                throw new BarcodeDecodingException("PDF417 general-purpose ECI designator is incomplete or invalid.");
            }

            assignment = ((codewords[index + 1] + 1) * 900)
                + codewords[index + 2];
            nextIndex = index + 3;
        }
        else
        {
            if (index + 1 >= codewords.Length
                || codewords[index + 1] < 0
                || codewords[index + 1] >= 900)
            {
                throw new BarcodeDecodingException("PDF417 user-defined ECI designator is incomplete or invalid.");
            }

            assignment = 810900 + codewords[index + 1];
            nextIndex = index + 2;
        }

        if (assignment < 0
            || assignment > Pdf417CompactionPlanner.MaximumEciAssignmentNumber)
        {
            throw new BarcodeDecodingException("PDF417 ECI assignment number is outside the supported designator range.");
        }

        Pdf417DecodedEci decoded = new Pdf417DecodedEci();
        decoded.AssignmentNumber = assignment;
        decoded.NextCodewordIndex = nextIndex;
        return decoded;
    }

    static int FindNextControlCodeword(int[] codewords, int startIndex)
    {
        int index = startIndex;
        while (index < codewords.Length
            && codewords[index] < Pdf417TextCompaction.TextLatchCodeword)
        {
            index = index + 1;
        }

        return index;
    }

    static int FindByteModeCodeword(int[] codewords, int dataIndex)
    {
        int index = dataIndex - 1;
        while (index >= 0)
        {
            int codeword = codewords[index];
            if (codeword == Pdf417ByteCompaction.LatchWithRemainderCodeword
                || codeword == Pdf417ByteCompaction.LatchSixByteCodeword)
            {
                return codeword;
            }

            if (codeword == Pdf417TextCompaction.TextLatchCodeword
                || codeword == Pdf417NumericCompaction.NumericLatchCodeword)
            {
                break;
            }

            index = index - 1;
        }

        throw new BarcodeDecodingException("PDF417 byte data has no preceding byte-compaction latch.");
    }

    static Pdf417TextInterpretation InterpretDecodedText(
        byte[] rawBytes,
        EciSegment[] eciSegments)
    {
        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        int assignment = DefaultEciAssignmentNumber;
        int byteOffset = 0;
        bool supported = true;
        int segmentIndex = 0;
        while (segmentIndex < eciSegments.Length)
        {
            EciSegment segment = eciSegments[segmentIndex];
            if (segment.ByteOffset < byteOffset
                || segment.ByteOffset > rawBytes.Length)
            {
                throw new BarcodeDecodingException("PDF417 ECI byte ranges are inconsistent.");
            }

            supported = AppendInterpretedRange(
                text,
                rawBytes,
                byteOffset,
                segment.ByteOffset - byteOffset,
                assignment)
                && supported;
            assignment = segment.AssignmentNumber;
            byteOffset = segment.ByteOffset;
            segmentIndex = segmentIndex + 1;
        }

        supported = AppendInterpretedRange(
            text,
            rawBytes,
            byteOffset,
            rawBytes.Length - byteOffset,
            assignment)
            && supported;
        Pdf417TextInterpretation result = new Pdf417TextInterpretation();
        result.Text = supported ? text.ToString() : null;
        result.CharacterEncoding = FindDecodedCharacterEncoding(eciSegments);
        return result;
    }

    static bool AppendInterpretedRange(
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

        string? encodingName = DataMatrixHighLevelDecoder.FindEciEncodingName(
            assignmentNumber);
        if (encodingName == null || encodingName == "Binary")
        {
            return false;
        }

        global::System.Text.Encoding.RegisterProvider(
            global::System.Text.CodePagesEncodingProvider.Instance);
        global::System.Text.Encoding encoding =
            global::System.Text.Encoding.GetEncoding(encodingName);
        text.Append(encoding.GetString(rawBytes, startIndex, count));
        return true;
    }

    static string? FindDecodedCharacterEncoding(EciSegment[] eciSegments)
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

        return DataMatrixHighLevelDecoder.FindEciEncodingName(assignment);
    }

    static int[] CopyHighLevelRange(
        int[] source,
        int startIndex,
        int count)
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

    static byte[] CopyDecodedBytes(Pdf417DecodedByteBuffer source)
    {
        byte[] copy = new byte[source.Count];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source.Bytes[index];
            index = index + 1;
        }

        return copy;
    }

    static EciSegment[] CopyDecodedEciSegments(
        EciSegment[] source,
        int count)
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

    static Pdf417CompactionMode[] CopyDecodedModes(
        Pdf417CompactionMode[] source,
        int count)
    {
        Pdf417CompactionMode[] copy = new Pdf417CompactionMode[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void ValidateHighLevelInput(int[] codewords)
    {
        if (codewords == null || codewords.Length == 0)
        {
            throw new BarcodeDecodingException("PDF417 high-level decoding requires at least one codeword.");
        }

        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0
                || codewords[index] >= Pdf417CodewordTable.CodewordCount)
            {
                throw new BarcodeDecodingException("PDF417 high-level codewords must be between zero and 928.");
            }

            index = index + 1;
        }
    }
}
