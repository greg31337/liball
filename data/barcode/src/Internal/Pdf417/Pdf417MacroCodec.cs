namespace barcode;

internal class Pdf417MacroBlock
{
    internal StructuredAppendInfo Macro = new StructuredAppendInfo();
    internal string? FileName;
    internal bool HasTimestamp;
    internal long Timestamp;
    internal string? Sender;
    internal string? Addressee;
    internal bool HasFileSize;
    internal long FileSize;
    internal bool HasFileChecksum;
    internal int FileChecksum;
    internal int[] Codewords = new int[0];
}

internal static class Pdf417MacroCodec
{
    internal const int MacroMarkerCodeword = 928;
    internal const int OptionalFieldCodeword = 923;
    internal const int TerminatorCodeword = 922;
    internal const int FileNameField = 0;
    internal const int SegmentCountField = 1;
    internal const int TimestampField = 2;
    internal const int SenderField = 3;
    internal const int AddresseeField = 4;
    internal const int FileSizeField = 5;
    internal const int ChecksumField = 6;
    internal const int DefinedOptionalFieldCount = 7;
    internal const int SegmentIndexDigitCount = 5;
    internal const int SegmentCountDigitCount = 5;
    internal const int TimestampDigitCount = 11;
    internal const int ChecksumDigitCount = 5;
    internal const int MaximumSegmentIndex = 99998;
    internal const int MaximumSegmentCount = 99999;
    internal const int MaximumChecksum = 65535;
    internal const int MaximumControlBlockCodewordCount = 928;

    internal static int[] EncodeControlBlock(Pdf417Options options)
    {
        StructuredAppendInfo macro = ValidateEncodingOptions(options);
        int[] buffer = new int[MaximumControlBlockCodewordCount];
        int count = 0;
        buffer[count] = MacroMarkerCodeword;
        count = count + 1;

        int[] segmentIndex = EncodeNumericValue(
            macro.Index,
            SegmentIndexDigitCount);
        if (segmentIndex.Length != 2)
        {
            throw new BarcodeEncodingException("Macro PDF417 segment index must encode as exactly two codewords.");
        }

        count = AppendCodewords(buffer, count, segmentIndex);
        int[] fileIdentifier = EncodeFileIdentifier(macro.FileIdentifier);
        count = AppendCodewords(buffer, count, fileIdentifier);

        if (options.FileName != null)
        {
            int[] fileName = EncodeTextField(options.FileName, "Macro PDF417 file name must not be empty.");
            count = AppendOptionalField(buffer, count, FileNameField, fileName);
        }

        if (macro.Count > 0)
        {
            int[] segmentCount = EncodeNumericValue(
                macro.Count,
                SegmentCountDigitCount);
            count = AppendOptionalField(
                buffer,
                count,
                SegmentCountField,
                segmentCount);
        }

        if (options.Timestamp > 0)
        {
            int[] timestamp = EncodeNumericValue(
                options.Timestamp,
                TimestampDigitCount);
            count = AppendOptionalField(
                buffer,
                count,
                TimestampField,
                timestamp);
        }

        if (options.Sender != null)
        {
            int[] sender = EncodeTextField(options.Sender, "Macro PDF417 sender must not be empty.");
            count = AppendOptionalField(buffer, count, SenderField, sender);
        }

        if (options.Addressee != null)
        {
            int[] addressee = EncodeTextField(options.Addressee, "Macro PDF417 addressee must not be empty.");
            count = AppendOptionalField(buffer, count, AddresseeField, addressee);
        }

        if (options.FileSize > 0)
        {
            int[] fileSize = EncodeNumericValue(options.FileSize, 0);
            count = AppendOptionalField(buffer, count, FileSizeField, fileSize);
        }

        if (options.FileChecksum > 0)
        {
            int[] checksum = EncodeNumericValue(
                options.FileChecksum,
                ChecksumDigitCount);
            count = AppendOptionalField(buffer, count, ChecksumField, checksum);
        }

        if (macro.IsLastSegment)
        {
            if (count >= buffer.Length)
            {
                throw new BarcodeEncodingException("Macro PDF417 control block exceeds the symbol codeword capacity.");
            }

            buffer[count] = TerminatorCodeword;
            count = count + 1;
        }

        return CopyIntegerPrefix(buffer, count);
    }

    internal static Pdf417MacroBlock DecodeControlBlock(int[] codewords)
    {
        ValidateEncodedControlBlock(codewords);
        Pdf417MacroBlock block = new Pdf417MacroBlock();
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Format = BarcodeFormat.MacroPdf417;
        block.Macro = macro;
        block.Codewords = CopyIntegerPrefix(codewords, codewords.Length);

        int[] segmentIndexCodewords = new int[] { codewords[1], codewords[2] };
        string segmentDigits = DecodeNumericField(
            segmentIndexCodewords,
            SegmentIndexDigitCount);
        long segmentIndex = ParseDecodedDecimal(segmentDigits);
        if (segmentIndex > MaximumSegmentIndex)
        {
            throw new BarcodeDecodingException("Macro PDF417 segment index exceeds 99998.");
        }

        macro.Index = (int)segmentIndex;
        int index = 3;
        global::System.Text.StringBuilder fileIdentifier = new global::System.Text.StringBuilder();
        while (index < codewords.Length
            && codewords[index] != OptionalFieldCodeword
            && codewords[index] != TerminatorCodeword)
        {
            int fileCodeword = codewords[index];
            if (fileCodeword < 0 || fileCodeword >= Pdf417NumericCompaction.CodewordBase)
            {
                throw new BarcodeDecodingException("Macro PDF417 file identifier codewords must be between zero and 899.");
            }

            fileIdentifier.Append(fileCodeword.ToString(
                "D3",
                global::System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        if (fileIdentifier.Length == 0)
        {
            throw new BarcodeDecodingException("Macro PDF417 requires at least one file identifier codeword.");
        }

        macro.FileIdentifier = fileIdentifier.ToString();
        bool[] seenFields = new bool[DefinedOptionalFieldCount];
        while (index < codewords.Length)
        {
            if (codewords[index] == TerminatorCodeword)
            {
                if (index != codewords.Length - 1)
                {
                    throw new BarcodeDecodingException("Macro PDF417 terminator must be the final control-block codeword.");
                }

                macro.IsLastSegment = true;
                index = index + 1;
                continue;
            }

            if (codewords[index] != OptionalFieldCodeword
                || index + 1 >= codewords.Length)
            {
                throw new BarcodeDecodingException("Macro PDF417 optional field tag or designator is missing.");
            }

            int designator = codewords[index + 1];
            if (designator < 0 || designator >= DefinedOptionalFieldCount)
            {
                throw new BarcodeDecodingException("Macro PDF417 optional field designator is not defined.");
            }

            if (seenFields[designator])
            {
                throw new BarcodeDecodingException("Macro PDF417 optional fields must not be repeated within one control block.");
            }

            seenFields[designator] = true;
            int dataStart = index + 2;
            int dataEnd = dataStart;
            while (dataEnd < codewords.Length
                && codewords[dataEnd] != OptionalFieldCodeword
                && codewords[dataEnd] != TerminatorCodeword)
            {
                dataEnd = dataEnd + 1;
            }

            if (dataEnd == dataStart)
            {
                throw new BarcodeDecodingException("Macro PDF417 optional fields must not be empty.");
            }

            int[] fieldCodewords = CopyIntegerRange(
                codewords,
                dataStart,
                dataEnd - dataStart);
            DecodeOptionalField(block, designator, fieldCodewords);
            index = dataEnd;
        }

        ValidateDecodedMetadata(block);
        return block;
    }

    internal static int[] EncodeFileIdentifier(string? fileIdentifier)
    {
        if (fileIdentifier == null
            || fileIdentifier.Length == 0
            || fileIdentifier.Length % 3 != 0)
        {
            throw new BarcodeEncodingException("Macro PDF417 file identifier must contain one or more three-digit codeword groups.");
        }

        int[] codewords = new int[fileIdentifier.Length / 3];
        int group = 0;
        while (group < codewords.Length)
        {
            int value = 0;
            int digit = 0;
            while (digit < 3)
            {
                char character = fileIdentifier[(group * 3) + digit];
                if (character < '0' || character > '9')
                {
                    throw new BarcodeEncodingException("Macro PDF417 file identifier must contain decimal digits only.");
                }

                value = (value * 10) + character - '0';
                digit = digit + 1;
            }

            if (value >= Pdf417NumericCompaction.CodewordBase)
            {
                throw new BarcodeEncodingException("Macro PDF417 file identifier groups must represent codewords from 000 through 899.");
            }

            codewords[group] = value;
            group = group + 1;
        }

        return codewords;
    }

    static void DecodeOptionalField(
        Pdf417MacroBlock block,
        int designator,
        int[] fieldCodewords)
    {
        switch (designator)
        {
            case FileNameField:
                block.FileName = DecodeTextField(fieldCodewords);
                return;
            case SegmentCountField:
                string countDigits = DecodeNumericField(
                    fieldCodewords,
                    SegmentCountDigitCount);
                long count = ParseDecodedDecimal(countDigits);
                if (count < 1 || count > MaximumSegmentCount)
                {
                    throw new BarcodeDecodingException("Macro PDF417 segment count must be from one through 99999.");
                }

                block.Macro.Count = (int)count;
                return;
            case TimestampField:
                string timestampDigits = DecodeNumericField(
                    fieldCodewords,
                    TimestampDigitCount);
                block.Timestamp = ParseDecodedDecimal(timestampDigits);
                block.HasTimestamp = true;
                return;
            case SenderField:
                block.Sender = DecodeTextField(fieldCodewords);
                return;
            case AddresseeField:
                block.Addressee = DecodeTextField(fieldCodewords);
                return;
            case FileSizeField:
                string fileSizeDigits = DecodeNumericField(fieldCodewords, 0);
                block.FileSize = ParseDecodedDecimal(fileSizeDigits);
                block.HasFileSize = true;
                return;
            default:
                string checksumDigits = DecodeNumericField(
                    fieldCodewords,
                    ChecksumDigitCount);
                long checksum = ParseDecodedDecimal(checksumDigits);
                if (checksum > MaximumChecksum)
                {
                    throw new BarcodeDecodingException("Macro PDF417 checksum must be from zero through 65535.");
                }

                block.FileChecksum = (int)checksum;
                block.HasFileChecksum = true;
                return;
        }
    }

    static string DecodeTextField(int[] fieldCodewords)
    {
        int start = 0;
        if (fieldCodewords[0] == Pdf417TextCompaction.TextLatchCodeword)
        {
            start = 1;
        }

        if (start == fieldCodewords.Length)
        {
            throw new BarcodeDecodingException("Macro PDF417 text optional field must not be empty.");
        }

        int[] textCodewords = CopyIntegerRange(
            fieldCodewords,
            start,
            fieldCodewords.Length - start);
        Pdf417TextDecodingResult decoded = Pdf417TextCompaction.DecodeCodewords(
            textCodewords,
            Pdf417TextSubmode.Alpha);
        if (decoded.Text.Length == 0)
        {
            throw new BarcodeDecodingException("Macro PDF417 text optional field must decode to at least one character.");
        }

        return decoded.Text;
    }

    static string DecodeNumericField(
        int[] fieldCodewords,
        int requiredDigitCount)
    {
        int start = 0;
        if (fieldCodewords[0] == Pdf417NumericCompaction.NumericLatchCodeword)
        {
            start = 1;
        }

        if (start == fieldCodewords.Length)
        {
            throw new BarcodeDecodingException("Macro PDF417 numeric optional field must not be empty.");
        }

        int[] numericCodewords = CopyIntegerRange(
            fieldCodewords,
            start,
            fieldCodewords.Length - start);
        Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeDataCodewords(
            numericCodewords);
        if (requiredDigitCount > 0 && decoded.Digits.Length != requiredDigitCount)
        {
            throw new BarcodeDecodingException("Macro PDF417 fixed-width numeric field has an invalid digit count.");
        }

        return decoded.Digits;
    }

    static long ParseDecodedDecimal(string digits)
    {
        long value = 0;
        int index = 0;
        while (index < digits.Length)
        {
            int digit = digits[index] - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                throw new BarcodeDecodingException("Macro PDF417 numeric optional field exceeds the supported integer range.");
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        return value;
    }

    static int[] EncodeTextField(string value, string emptyMessage)
    {
        if (value.Length == 0)
        {
            throw new BarcodeEncodingException(emptyMessage);
        }

        Pdf417TextEncodingResult encoded = Pdf417TextCompaction.EncodeText(
            value,
            Pdf417TextSubmode.Alpha);
        return encoded.Codewords;
    }

    static int[] EncodeNumericValue(long value, int fixedDigitCount)
    {
        string digits = fixedDigitCount == 0
            ? value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString(
                "D" + fixedDigitCount.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                global::System.Globalization.CultureInfo.InvariantCulture);
        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);
        return encoded.DataCodewords;
    }

    static int AppendOptionalField(
        int[] target,
        int targetCount,
        int designator,
        int[] fieldCodewords)
    {
        if (targetCount > target.Length - fieldCodewords.Length - 2)
        {
            throw new BarcodeEncodingException("Macro PDF417 control block exceeds the symbol codeword capacity.");
        }

        target[targetCount] = OptionalFieldCodeword;
        target[targetCount + 1] = designator;
        return AppendCodewords(target, targetCount + 2, fieldCodewords);
    }

    static int AppendCodewords(int[] target, int targetCount, int[] values)
    {
        if (targetCount > target.Length - values.Length)
        {
            throw new BarcodeEncodingException("Macro PDF417 control block exceeds the symbol codeword capacity.");
        }

        int index = 0;
        while (index < values.Length)
        {
            target[targetCount] = values[index];
            targetCount = targetCount + 1;
            index = index + 1;
        }

        return targetCount;
    }

    static StructuredAppendInfo ValidateEncodingOptions(Pdf417Options options)
    {
        if (options == null || options.Macro == null)
        {
            throw new BarcodeEncodingException("Macro PDF417 options and segment metadata must not be null.");
        }

        StructuredAppendInfo macro = options.Macro;
        if (macro.Format != BarcodeFormat.Automatic
            && macro.Format != BarcodeFormat.MacroPdf417)
        {
            throw new BarcodeEncodingException("Structured-append metadata is not for Macro PDF417.");
        }

        if (macro.Index < 0 || macro.Index > MaximumSegmentIndex)
        {
            throw new BarcodeEncodingException("Macro PDF417 segment index must be from zero through 99998.");
        }

        if (macro.Count < 0 || macro.Count > MaximumSegmentCount)
        {
            throw new BarcodeEncodingException("Macro PDF417 segment count must be zero or from one through 99999.");
        }

        if (macro.Count > 0 && macro.Index >= macro.Count)
        {
            throw new BarcodeEncodingException("Macro PDF417 segment index must be less than the segment count.");
        }

        if (macro.Count > 0
            && macro.IsLastSegment != (macro.Index == macro.Count - 1))
        {
            throw new BarcodeEncodingException("Macro PDF417 last-segment flag conflicts with the segment count.");
        }

        EncodeFileIdentifier(macro.FileIdentifier);
        if (options.Timestamp < 0 || options.Timestamp > 99999999999)
        {
            throw new BarcodeEncodingException("Macro PDF417 timestamp must be zero or from one through 99999999999.");
        }

        if (options.FileSize < 0)
        {
            throw new BarcodeEncodingException("Macro PDF417 file size must not be negative.");
        }

        if (options.FileChecksum < 0 || options.FileChecksum > MaximumChecksum)
        {
            throw new BarcodeEncodingException("Macro PDF417 checksum must be zero or from one through 65535.");
        }

        return macro;
    }

    static void ValidateEncodedControlBlock(int[] codewords)
    {
        if (codewords == null
            || codewords.Length < 4
            || codewords.Length > MaximumControlBlockCodewordCount
            || codewords[0] != MacroMarkerCodeword)
        {
            throw new BarcodeDecodingException("Macro PDF417 control block must start with 928 and contain required metadata.");
        }

        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > MacroMarkerCodeword)
            {
                throw new BarcodeDecodingException("Macro PDF417 control-block codewords must be between zero and 928.");
            }

            index = index + 1;
        }
    }

    static void ValidateDecodedMetadata(Pdf417MacroBlock block)
    {
        if (block.Macro.Count > 0 && block.Macro.Index >= block.Macro.Count)
        {
            throw new BarcodeDecodingException("Macro PDF417 segment index must be less than the segment count.");
        }

        if (block.Macro.Count > 0
            && block.Macro.IsLastSegment != (block.Macro.Index == block.Macro.Count - 1))
        {
            throw new BarcodeDecodingException("Macro PDF417 terminator conflicts with the segment count.");
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

    static int[] CopyIntegerRange(int[] source, int start, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[start + index];
            index = index + 1;
        }

        return copy;
    }
}
