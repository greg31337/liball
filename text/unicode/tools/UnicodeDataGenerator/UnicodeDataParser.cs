namespace unicode.Tools.UnicodeDataGenerator;

public static class UnicodeDataParser
{
    public static UnicodeSpecialCasingRecord[] ReadSpecialCasing(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeSpecialCasingRecord> records = new List<UnicodeSpecialCasingRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseSpecialCasingLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeSpecialCasingRecord ParseSpecialCasingLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 4)
        {
            throw new FormatException("SpecialCasing.txt records must contain code point, lower, title, and upper mappings.");
        }

        string conditionField = "";
        if (fields.Length > 4)
        {
            conditionField = fields[4];
        }

        return new UnicodeSpecialCasingRecord(
            ParseCodePoint(fields[0]),
            ParseCodePointSequence(fields[1]),
            ParseCodePointSequence(fields[2]),
            ParseCodePointSequence(fields[3]),
            ParseConditionList(conditionField),
            ExtractComment(line));
    }

    public static UnicodeCaseFoldingRecord[] ReadCaseFolding(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeCaseFoldingRecord> records = new List<UnicodeCaseFoldingRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseCaseFoldingLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeCaseFoldingRecord ParseCaseFoldingLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("CaseFolding.txt records must contain code point, status, and mapping.");
        }

        ValidateCaseFoldingStatus(fields[1]);
        return new UnicodeCaseFoldingRecord(
            ParseCodePoint(fields[0]),
            fields[1],
            ParseCodePointSequence(fields[2]),
            ExtractComment(line));
    }

    public static UnicodeNormalizationTestRecord[] ReadNormalizationTest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodeNormalizationTestRecord> records = new List<UnicodeNormalizationTestRecord>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("@", StringComparison.Ordinal))
            {
                continue;
            }

            records.Add(ParseNormalizationTestLine(rawLine));
        }

        return records.ToArray();
    }

    public static UnicodeBoundaryTestRecord[] ReadGraphemeBreakTest(string path)
    {
        return ReadBoundaryTest(path);
    }

    public static UnicodeBoundaryTestRecord[] ReadWordBreakTest(string path)
    {
        return ReadBoundaryTest(path);
    }

    public static UnicodeBoundaryTestRecord[] ReadSentenceBreakTest(string path)
    {
        return ReadBoundaryTest(path);
    }

    public static UnicodeBoundaryTestRecord[] ReadLineBreakTest(string path)
    {
        return ReadBoundaryTest(path);
    }

    public static UnicodeConfusableRecord[] ReadConfusables(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodeConfusableRecord> records = new List<UnicodeConfusableRecord>();

        foreach (string rawLine in File.ReadLines(path))
        {
            if (IsSecurityDataLine(rawLine))
            {
                records.Add(ParseConfusableLine(rawLine));
            }
        }

        return records.ToArray();
    }

    public static UnicodeConfusableRecord ParseConfusableLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("confusables.txt records must contain source, target, and type fields.");
        }

        return new UnicodeConfusableRecord(
            ParseCodePointSequence(fields[0]),
            ParseCodePointSequence(fields[1]),
            fields[2],
            ExtractComment(line));
    }

    public static UnicodeIdentifierStatusRecord[] ReadIdentifierStatus(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodeIdentifierStatusRecord> records = new List<UnicodeIdentifierStatusRecord>();

        foreach (string rawLine in File.ReadLines(path))
        {
            if (IsSecurityDataLine(rawLine))
            {
                records.Add(ParseIdentifierStatusLine(rawLine));
            }
        }

        return records.ToArray();
    }

    public static UnicodeIdentifierStatusRecord ParseIdentifierStatusLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("IdentifierStatus.txt records must contain a code point range and status value.");
        }

        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodeIdentifierStatusRecord(range[0], range[1], fields[1], ExtractComment(line));
    }

    public static UnicodeIdentifierTypeRecord[] ReadIdentifierType(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodeIdentifierTypeRecord> records = new List<UnicodeIdentifierTypeRecord>();

        foreach (string rawLine in File.ReadLines(path))
        {
            if (IsSecurityDataLine(rawLine))
            {
                records.Add(ParseIdentifierTypeLine(rawLine));
            }
        }

        return records.ToArray();
    }

    public static UnicodeIdentifierTypeRecord ParseIdentifierTypeLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("IdentifierType.txt records must contain a code point range and at least one type value.");
        }

        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodeIdentifierTypeRecord(
            range[0],
            range[1],
            ParseConditionList(fields[1]),
            ExtractComment(line));
    }

    public static UnicodeBoundaryTestRecord[] ReadBoundaryTest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodeBoundaryTestRecord> records = new List<UnicodeBoundaryTestRecord>();

        foreach (string rawLine in File.ReadLines(path))
        {
            if (IsBoundaryTestDataLine(rawLine))
            {
                records.Add(ParseBoundaryTestLine(rawLine));
            }
        }

        return records.ToArray();
    }

    public static UnicodeBoundaryTestRecord ParseBoundaryTestLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = dataLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 3)
        {
            throw new FormatException("Boundary test records must contain boundary markers and at least one code point.");
        }

        List<int> codePoints = new List<int>();
        List<bool> breakBefore = new List<bool>();
        bool expectingMarker = true;

        int index = 0;
        while (index < fields.Length)
        {
            string field = fields[index];
            if (field == "÷" || field == "×")
            {
                if (!expectingMarker)
                {
                    throw new FormatException("Boundary test records must alternate markers and code points.");
                }

                breakBefore.Add(field == "÷");
                expectingMarker = false;
            }
            else
            {
                if (expectingMarker)
                {
                    throw new FormatException("Boundary test records must place a marker before each code point.");
                }

                codePoints.Add(ParseCodePoint(field));
                expectingMarker = true;
            }

            index = index + 1;
        }

        if (expectingMarker)
        {
            throw new FormatException("Boundary test records must end with a boundary marker.");
        }

        return new UnicodeBoundaryTestRecord(codePoints.ToArray(), breakBefore.ToArray(), ExtractComment(line));
    }

    public static UnicodeNormalizationTestRecord ParseNormalizationTestLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 5)
        {
            throw new FormatException("NormalizationTest.txt records must contain source, NFC, NFD, NFKC, and NFKD fields.");
        }

        return new UnicodeNormalizationTestRecord(
            ParseCodePointSequence(fields[0]),
            ParseCodePointSequence(fields[1]),
            ParseCodePointSequence(fields[2]),
            ParseCodePointSequence(fields[3]),
            ParseCodePointSequence(fields[4]),
            ExtractComment(line));
    }

    public static UnicodeNormalizationQuickCheckRecord[] ReadNormalizationQuickChecks(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeNormalizationQuickCheckRecord> records = new List<UnicodeNormalizationQuickCheckRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            if (IsNormalizationQuickCheckLine(lines[index]))
            {
                records.Add(ParseNormalizationQuickCheckLine(lines[index]));
            }

            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeNormalizationMappingRecord[] ReadNfkcCaseFoldMappings(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeNormalizationMappingRecord> records = new List<UnicodeNormalizationMappingRecord>();
        int index = 0;
        while (index < lines.Length)
        {
            string[] fields = SplitFields(lines[index]);
            if (fields.Length >= 3 && fields[1] == "NFKC_CF")
            {
                int[] range = ParseCodePointRange(fields[0]);
                records.Add(new UnicodeNormalizationMappingRecord(
                    range[0],
                    range[1],
                    ParseCodePointSequence(fields[2])));
            }

            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeNormalizationQuickCheckRecord ParseNormalizationQuickCheckLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("DerivedNormalizationProps.txt quick-check records must contain a range, form property, and value.");
        }

        if (!fields[1].EndsWith("_QC", StringComparison.Ordinal))
        {
            throw new FormatException("Normalization quick-check property names must end with _QC.");
        }

        string form = fields[1].Substring(0, fields[1].Length - 3);
        string quickCheckValue = fields[2];
        ValidateNormalizationFormName(form);
        ValidateQuickCheckValue(quickCheckValue);
        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodeNormalizationQuickCheckRecord(range[0], range[1], form, quickCheckValue);
    }

    public static UnicodeCompositionExclusionRecord[] ReadCompositionExclusions(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeCompositionExclusionRecord> records = new List<UnicodeCompositionExclusionRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseCompositionExclusionLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeCompositionExclusionRecord ParseCompositionExclusionLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 1 || fields[0].Length == 0)
        {
            throw new FormatException("CompositionExclusions.txt records must contain a code point or code point range.");
        }

        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodeCompositionExclusionRecord(range[0], range[1]);
    }

    public static UnicodeDecompositionRecord[] ReadDecompositionMappings(string path)
    {
        UnicodeDataRecord[] unicodeData = ReadUnicodeData(path);
        List<UnicodeDecompositionRecord> records = new List<UnicodeDecompositionRecord>();
        int index = 0;

        while (index < unicodeData.Length)
        {
            if (unicodeData[index].DecompositionMapping.Length > 0)
            {
                records.Add(new UnicodeDecompositionRecord(
                    unicodeData[index].CodePoint,
                    unicodeData[index].DecompositionType,
                    unicodeData[index].DecompositionMapping));
            }

            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodePropertyRangeRecord[] ReadEmojiData(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodeEmojiSequenceRecord[] ReadEmojiSequences(string path)
    {
        return ReadEmojiSequenceFile(path);
    }

    public static UnicodeEmojiSequenceRecord[] ReadEmojiZwjSequences(string path)
    {
        return ReadEmojiSequenceFile(path);
    }

    public static UnicodeEmojiSequenceRecord[] ReadEmojiVariationSequences(string path)
    {
        return ReadEmojiSequenceFile(path);
    }

    public static UnicodeEmojiSequenceRecord[] ReadEmojiSequenceFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeEmojiSequenceRecord> records = new List<UnicodeEmojiSequenceRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseEmojiSequenceLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeEmojiSequenceRecord ParseEmojiSequenceLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("Emoji sequence records must contain code points and a property name.");
        }

        string[] codePointFields = fields[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (codePointFields.Length == 0)
        {
            throw new FormatException("Emoji sequence records must contain at least one code point.");
        }

        int[] startCodePoints = new int[codePointFields.Length];
        int[] endCodePoints = new int[codePointFields.Length];
        int index = 0;

        while (index < codePointFields.Length)
        {
            int[] range = ParseCodePointRange(codePointFields[index]);
            startCodePoints[index] = range[0];
            endCodePoints[index] = range[1];
            index = index + 1;
        }

        return new UnicodeEmojiSequenceRecord(startCodePoints, endCodePoints, fields[1], ExtractComment(line));
    }

    public static UnicodePropertyRangeRecord[] ReadEastAsianWidth(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadLineBreak(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadGraphemeBreakProperty(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadWordBreakProperty(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadSentenceBreakProperty(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadIndicConjunctBreak(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodePropertyRangeRecord> records = new List<UnicodePropertyRangeRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            if (IsIndicConjunctBreakLine(lines[index]))
            {
                records.Add(ParseIndicConjunctBreakLine(lines[index]));
            }

            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodePropertyRangeRecord[] ReadHangulSyllableType(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord ParseIndicConjunctBreakLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("Indic conjunct break records must contain a code point range and property value.");
        }

        string value = fields[1];
        if (fields.Length > 2)
        {
            if (fields[1] == "InCB")
            {
                value = fields[2];
            }

            if (fields[1] == "Indic_Conjunct_Break")
            {
                value = fields[2];
            }
        }

        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodePropertyRangeRecord(range[0], range[1], value);
    }

    public static UnicodeBidiMirrorRecord[] ReadBidiMirroring(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeBidiMirrorRecord> records = new List<UnicodeBidiMirrorRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseBidiMirroringLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeBidiMirrorRecord ParseBidiMirroringLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("BidiMirroring.txt records must contain source and mirror code points.");
        }

        return new UnicodeBidiMirrorRecord(ParseCodePoint(fields[0]), ParseCodePoint(fields[1]));
    }

    public static UnicodeBidiBracketRecord[] ReadBidiBrackets(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeBidiBracketRecord> records = new List<UnicodeBidiBracketRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseBidiBracketLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeBidiBracketRecord ParseBidiBracketLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("BidiBrackets.txt records must contain source, paired bracket, and bracket type.");
        }

        return new UnicodeBidiBracketRecord(ParseCodePoint(fields[0]), ParseCodePoint(fields[1]), fields[2]);
    }

    public static UnicodeJoiningRecord[] ReadJoiningData(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeJoiningRecord> records = new List<UnicodeJoiningRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseJoiningLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeJoiningRecord ParseJoiningLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 4)
        {
            throw new FormatException("ArabicShaping.txt records must contain code point, name, joining type, and joining group.");
        }

        return new UnicodeJoiningRecord(ParseCodePoint(fields[0]), fields[1], fields[2], fields[3]);
    }

    public static UnicodePropertyRangeRecord[] ReadScripts(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadBlocks(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadDerivedAge(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadDisplayPropertyFile(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodeScriptExtensionsRecord[] ReadScriptExtensions(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeScriptExtensionsRecord> records = new List<UnicodeScriptExtensionsRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseScriptExtensionsLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeScriptExtensionsRecord ParseScriptExtensionsLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("ScriptExtensions.txt records must contain a code point range and script list.");
        }

        int[] range = ParseCodePointRange(fields[0]);
        string[] scripts = fields[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (scripts.Length == 0)
        {
            throw new FormatException("ScriptExtensions.txt records must contain at least one script value.");
        }

        return new UnicodeScriptExtensionsRecord(range[0], range[1], scripts);
    }

    public static UnicodePropertyRangeRecord[] ReadPropList(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadDerivedCoreProperties(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadExtractedPropertyFile(string path)
    {
        return ReadPropertyRanges(path);
    }

    public static UnicodePropertyRangeRecord[] ReadPropertyRangesWithMissing(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<UnicodePropertyRangeRecord> records = new List<UnicodePropertyRangeRecord>();
        foreach (string rawLine in File.ReadLines(path))
        {
            string trimmed = rawLine.Trim();
            if (trimmed.StartsWith("# @missing:", StringComparison.Ordinal))
            {
                records.Add(ParsePropertyRangeLine(trimmed.Substring("# @missing:".Length).Trim()));
                continue;
            }

            string line = StripComment(rawLine).Trim();
            if (line.Length > 0)
            {
                records.Add(ParsePropertyRangeLine(line));
            }
        }

        return records.ToArray();
    }

    public static UnicodePropertyRangeRecord[] ReadPropertyRanges(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodePropertyRangeRecord> records = new List<UnicodePropertyRangeRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParsePropertyRangeLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodePropertyRangeRecord ParsePropertyRangeLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("Property range records must contain a code point range and property name.");
        }

        int[] range = ParseCodePointRange(fields[0]);
        return new UnicodePropertyRangeRecord(range[0], range[1], fields[1]);
    }

    public static UnicodePropertyAliasRecord[] ReadPropertyAliases(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodePropertyAliasRecord> records = new List<UnicodePropertyAliasRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParsePropertyAliasLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeNameAliasRecord[] ReadNameAliases(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeNameAliasRecord> records = new List<UnicodeNameAliasRecord>();
        int index = 0;
        while (index < lines.Length)
        {
            records.Add(ParseNameAliasLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeNameAliasRecord ParseNameAliasLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("NameAliases.txt records must contain a code point, name, and alias type.");
        }

        return new UnicodeNameAliasRecord(ParseCodePoint(fields[0]), fields[1], fields[2]);
    }

    public static UnicodePropertyAliasRecord ParsePropertyAliasLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("PropertyAliases.txt records must contain at least short and long names.");
        }

        return new UnicodePropertyAliasRecord(fields[0], fields[1], CopyFields(fields, 2));
    }

    public static UnicodePropertyValueAliasRecord[] ReadPropertyValueAliases(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodePropertyValueAliasRecord> records = new List<UnicodePropertyValueAliasRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParsePropertyValueAliasLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodePropertyValueAliasRecord ParsePropertyValueAliasLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            throw new FormatException("PropertyValueAliases.txt records must contain property, value, and at least one alias.");
        }

        return new UnicodePropertyValueAliasRecord(fields[0], fields[1], CopyFields(fields, 2));
    }

    public static UnicodeNamedSequenceRecord[] ReadNamedSequences(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeNamedSequenceRecord> records = new List<UnicodeNamedSequenceRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseNamedSequenceLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeNamedSequenceRecord ParseNamedSequenceLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            throw new FormatException("NamedSequences.txt records must contain a name and code point sequence.");
        }

        return new UnicodeNamedSequenceRecord(fields[0], ParseCodePointSequence(fields[1]));
    }

    public static UnicodeDataRecord[] ReadUnicodeData(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] lines = ReadDataLines(path);
        List<UnicodeDataRecord> records = new List<UnicodeDataRecord>();
        int index = 0;

        while (index < lines.Length)
        {
            records.Add(ParseUnicodeDataLine(lines[index]));
            index = index + 1;
        }

        return records.ToArray();
    }

    public static UnicodeDataRecord ParseUnicodeDataLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length != 15)
        {
            throw new FormatException("UnicodeData.txt records must contain 15 semicolon-delimited fields.");
        }

        return new UnicodeDataRecord(
            ParseCodePoint(fields[0]),
            fields[1],
            fields[2],
            ParseInteger(fields[3]),
            fields[4],
            fields[9] == "Y",
            ParseDecompositionType(fields[5]),
            ParseDecompositionMapping(fields[5]),
            ParseOptionalInteger(fields[6]),
            ParseOptionalInteger(fields[7]),
            UnicodeNumericValue.Parse(fields[8]),
            ParseOptionalCodePoint(fields[12]),
            ParseOptionalCodePoint(fields[13]),
            ParseOptionalCodePoint(fields[14]));
    }

    public static string[] ReadDataLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<string> lines = new List<string>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            lines.Add(line);
        }

        return lines.ToArray();
    }

    public static string[] ReadSecurityDataLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        List<string> lines = new List<string>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("@", StringComparison.Ordinal))
            {
                continue;
            }

            lines.Add(line);
        }

        return lines.ToArray();
    }

    public static bool IsSecurityDataLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string dataLine = StripComment(line).Trim();
        if (dataLine.Length == 0)
        {
            return false;
        }

        return !dataLine.StartsWith("@", StringComparison.Ordinal);
    }

    public static string StripComment(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        int commentIndex = line.IndexOf('#');
        if (commentIndex < 0)
        {
            return line;
        }

        return line.Substring(0, commentIndex);
    }

    public static string ExtractComment(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        int commentIndex = line.IndexOf('#');
        if (commentIndex < 0)
        {
            return "";
        }

        return line.Substring(commentIndex + 1).Trim();
    }

    public static string[] SplitFields(string dataLine)
    {
        ArgumentNullException.ThrowIfNull(dataLine);

        return dataLine.Split(';', StringSplitOptions.TrimEntries);
    }

    public static string[] CopyFields(string[] fields, int startIndex)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (startIndex >= fields.Length)
        {
            return Array.Empty<string>();
        }

        string[] aliases = new string[fields.Length - startIndex];
        int index = startIndex;

        while (index < fields.Length)
        {
            aliases[index - startIndex] = fields[index];
            index = index + 1;
        }

        return aliases;
    }

    public static int ParseCodePoint(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return int.Parse(text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static int ParseOptionalCodePoint(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return -1;
        }

        return ParseCodePoint(text);
    }

    public static int[] ParseCodePointRange(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        string rangeText = text.Trim();
        int separatorIndex = rangeText.IndexOf("..", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            int codePoint = ParseCodePoint(rangeText);
            return new int[] { codePoint, codePoint };
        }

        int start = ParseCodePoint(rangeText.Substring(0, separatorIndex));
        int end = ParseCodePoint(rangeText.Substring(separatorIndex + 2));
        if (end < start)
        {
            throw new FormatException("Code point range end must be greater than or equal to start.");
        }

        return new int[] { start, end };
    }

    public static int ParseInteger(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static int ParseOptionalInteger(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return -1;
        }

        return ParseInteger(text);
    }

    public static string ParseDecompositionType(string field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (!field.StartsWith("<", StringComparison.Ordinal))
        {
            return "";
        }

        int endIndex = field.IndexOf('>');
        if (endIndex < 0)
        {
            throw new FormatException("Decomposition type marker is missing a closing angle bracket.");
        }

        return field.Substring(1, endIndex - 1);
    }

    public static int[] ParseDecompositionMapping(string field)
    {
        ArgumentNullException.ThrowIfNull(field);

        string mappingText = field.Trim();
        if (mappingText.Length == 0)
        {
            return Array.Empty<int>();
        }

        if (mappingText.StartsWith("<", StringComparison.Ordinal))
        {
            int endIndex = mappingText.IndexOf('>');
            if (endIndex < 0)
            {
                throw new FormatException("Decomposition type marker is missing a closing angle bracket.");
            }

            mappingText = mappingText.Substring(endIndex + 1).Trim();
        }

        if (mappingText.Length == 0)
        {
            return Array.Empty<int>();
        }

        string[] parts = mappingText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int[] mapping = new int[parts.Length];
        int index = 0;

        while (index < parts.Length)
        {
            mapping[index] = ParseCodePoint(parts[index]);
            index = index + 1;
        }

        return mapping;
    }

    public static int[] ParseCodePointSequence(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int[] codePoints = new int[parts.Length];
        int index = 0;

        while (index < parts.Length)
        {
            codePoints[index] = ParseCodePoint(parts[index]);
            index = index + 1;
        }

        return codePoints;
    }

    public static string[] ParseConditionList(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    static bool IsNormalizationQuickCheckLine(string line)
    {
        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 3)
        {
            return false;
        }

        return fields[1].EndsWith("_QC", StringComparison.Ordinal);
    }

    static bool IsIndicConjunctBreakLine(string line)
    {
        string dataLine = StripComment(line).Trim();
        string[] fields = SplitFields(dataLine);
        if (fields.Length < 2)
        {
            return false;
        }

        if (fields[1] == "InCB")
        {
            return true;
        }

        return fields[1] == "Indic_Conjunct_Break";
    }

    static bool IsBoundaryTestDataLine(string line)
    {
        string dataLine = StripComment(line).Trim();
        if (dataLine.Length == 0)
        {
            return false;
        }

        return !dataLine.StartsWith("@", StringComparison.Ordinal);
    }

    static void ValidateNormalizationFormName(string form)
    {
        if (form == "NFC")
        {
            return;
        }

        if (form == "NFD")
        {
            return;
        }

        if (form == "NFKC")
        {
            return;
        }

        if (form == "NFKD")
        {
            return;
        }

        throw new FormatException("Unknown normalization quick-check form: " + form);
    }

    static void ValidateQuickCheckValue(string quickCheckValue)
    {
        if (quickCheckValue == "Y")
        {
            return;
        }

        if (quickCheckValue == "N")
        {
            return;
        }

        if (quickCheckValue == "M")
        {
            return;
        }

        throw new FormatException("Unknown normalization quick-check value: " + quickCheckValue);
    }

    static void ValidateCaseFoldingStatus(string status)
    {
        if (status == "C")
        {
            return;
        }

        if (status == "F")
        {
            return;
        }

        if (status == "S")
        {
            return;
        }

        if (status == "T")
        {
            return;
        }

        throw new FormatException("Unknown case folding status: " + status);
    }
}
