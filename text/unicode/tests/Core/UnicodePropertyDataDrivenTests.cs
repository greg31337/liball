using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodePropertyDataDrivenTests
{
    const int UnicodeCodePointCount = 0x110000;

    [Test]
    public void UnicodeDataProperties_MatchEveryCodePointIncludingDefaultsAndRangeTransitions()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeDataRecord[] records = UnicodeDataParser.ReadUnicodeData(DataPath("UnicodeData.txt"));
        UnicodeGeneralCategory[] categories = Enumerable.Repeat(
            UnicodeGeneralCategory.OtherNotAssigned,
            UnicodeCodePointCount).ToArray();
        int[] combiningClasses = new int[UnicodeCodePointCount];
        bool[] mirrored = new bool[UnicodeCodePointCount];

        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodeDataRecord record = records[recordIndex];
            int end = record.CodePoint;
            if (IsRangeFirst(record))
            {
                end = records[recordIndex + 1].CodePoint;
                recordIndex++;
            }

            UnicodeGeneralCategory category = GeneralCategoryFromName(record.GeneralCategory);
            for (int codePoint = record.CodePoint; codePoint <= end; codePoint++)
            {
                categories[codePoint] = category;
                combiningClasses[codePoint] = record.CombiningClass;
                mirrored[codePoint] = record.BidiMirrored;
            }

            recordIndex++;
        }

        HashSet<UnicodeGeneralCategory> seenCategories = new HashSet<UnicodeGeneralCategory>();
        for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
        {
            Equal(categories[codePoint], properties.GetGeneralCategory(codePoint), "General_Category", codePoint);
            Equal(combiningClasses[codePoint], properties.GetCombiningClass(codePoint), "Canonical_Combining_Class", codePoint);
            Equal(mirrored[codePoint], properties.IsBidiMirrored(codePoint), "Bidi_Mirrored", codePoint);
            seenCategories.Add(categories[codePoint]);
        }

        AssertEveryEnumValueSeen(seenCategories, "General_Category");
    }

    [Test]
    public void BidiProperties_MatchFullDerivedRangesMirrorsAndPairedBrackets()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeBidiClass[] expectedClasses = Enumerable.Repeat(
            UnicodeBidiClass.LeftToRight,
            UnicodeCodePointCount).ToArray();
        UnicodePropertyRangeRecord[] classRanges = UnicodeDataParser.ReadPropertyRangesWithMissing(
            DataPath("extracted", "DerivedBidiClass.txt"));

        foreach (UnicodePropertyRangeRecord range in classRanges)
        {
            UnicodeBidiClass value = BidiClassFromName(range.PropertyName);
            for (int codePoint = range.StartCodePoint; codePoint <= range.EndCodePoint; codePoint++)
            {
                expectedClasses[codePoint] = value;
            }
        }

        HashSet<UnicodeBidiClass> seenClasses = new HashSet<UnicodeBidiClass>();
        for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
        {
            Equal(expectedClasses[codePoint], properties.GetBidiClass(codePoint), "Bidi_Class", codePoint);
            seenClasses.Add(expectedClasses[codePoint]);
        }

        AssertEveryEnumValueSeen(seenClasses, "Bidi_Class");

        foreach (UnicodeBidiMirrorRecord mirror in UnicodeDataParser.ReadBidiMirroring(DataPath("BidiMirroring.txt")))
        {
            Equal(mirror.MirrorCodePoint, properties.GetBidiMirror(mirror.CodePoint), "Bidi_Mirroring_Glyph", mirror.CodePoint);
        }

        HashSet<UnicodeBidiPairedBracketType> seenBracketTypes = new HashSet<UnicodeBidiPairedBracketType>
        {
            UnicodeBidiPairedBracketType.None,
        };
        foreach (UnicodeBidiBracketRecord bracket in UnicodeDataParser.ReadBidiBrackets(DataPath("BidiBrackets.txt")))
        {
            UnicodeBidiPairedBracketType type = bracket.BracketType == "o"
                ? UnicodeBidiPairedBracketType.Open
                : UnicodeBidiPairedBracketType.Close;
            Equal(bracket.PairedBracketCodePoint, properties.GetBidiPairedBracket(bracket.CodePoint), "Bidi_Paired_Bracket", bracket.CodePoint);
            Equal(type, properties.GetBidiPairedBracketType(bracket.CodePoint), "Bidi_Paired_Bracket_Type", bracket.CodePoint);
            seenBracketTypes.Add(type);
        }

        Equal(0x0041, properties.GetBidiMirror(0x0041), "Bidi_Mirroring_Glyph default", 0x0041);
        Equal(0x0041, properties.GetBidiPairedBracket(0x0041), "Bidi_Paired_Bracket default", 0x0041);
        Equal(UnicodeBidiPairedBracketType.None, properties.GetBidiPairedBracketType(0x0041), "Bidi_Paired_Bracket_Type default", 0x0041);
        AssertEveryEnumValueSeen(seenBracketTypes, "Bidi_Paired_Bracket_Type");
    }

    [Test]
    public void DecompositionAndNumericProperties_MatchEveryUnicodeDataMappingAndRational()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeDataRecord[] records = UnicodeDataParser.ReadUnicodeData(DataPath("UnicodeData.txt"));
        HashSet<UnicodeDecompositionType> seenDecompositionTypes = new HashSet<UnicodeDecompositionType>
        {
            UnicodeDecompositionType.None,
        };
        HashSet<UnicodeNumericType> seenNumericTypes = new HashSet<UnicodeNumericType>
        {
            UnicodeNumericType.None,
        };
        HashSet<int> numericCodePoints = new HashSet<int>();

        foreach (UnicodeDataRecord record in records)
        {
            if (record.DecompositionMapping.Length > 0)
            {
                UnicodeDecompositionType type = DecompositionTypeFromName(record.DecompositionType);
                Equal(type, properties.GetDecompositionType(record.CodePoint), "Decomposition_Type", record.CodePoint);
                EqualArray(record.DecompositionMapping, properties.GetDecompositionMapping(record.CodePoint), "Decomposition_Mapping", record.CodePoint);
                seenDecompositionTypes.Add(type);
            }

            if (record.NumericValue.HasValue)
            {
                UnicodeNumericType numericType = NumericTypeFromRecord(record);
                UnicodeRationalValue value = properties.GetNumericValueExact(record.CodePoint);
                Equal(numericType, properties.GetNumericType(record.CodePoint), "Numeric_Type", record.CodePoint);
                Equal(true, value.HasValue, "Numeric_Value presence", record.CodePoint);
                Equal(record.NumericValue.Numerator, value.Numerator, "Numeric_Value numerator", record.CodePoint);
                Equal(record.NumericValue.Denominator, value.Denominator, "Numeric_Value denominator", record.CodePoint);
                Equal(
                    (double)record.NumericValue.Numerator / (double)record.NumericValue.Denominator,
                    properties.GetNumericValue(record.CodePoint),
                    "Numeric_Value",
                    record.CodePoint);
                numericCodePoints.Add(record.CodePoint);
                seenNumericTypes.Add(numericType);
            }
        }

        for (int codePoint = 0xAC00; codePoint <= 0xD7A3; codePoint++)
        {
            Equal(UnicodeDecompositionType.Canonical, properties.GetDecompositionType(codePoint), "Hangul Decomposition_Type", codePoint);
            EqualArray(HangulDecomposition(codePoint), properties.GetDecompositionMapping(codePoint), "Hangul Decomposition_Mapping", codePoint);
            seenDecompositionTypes.Add(UnicodeDecompositionType.Canonical);
        }

        foreach (int codePoint in numericCodePoints)
        {
            AssertNumericBoundary(properties, numericCodePoints, codePoint - 1);
            AssertNumericBoundary(properties, numericCodePoints, codePoint + 1);
        }

        AssertEveryEnumValueSeen(seenDecompositionTypes, "Decomposition_Type");
        AssertEveryEnumValueSeen(seenNumericTypes, "Numeric_Type");
    }

    [Test]
    public void ScriptBlockAndWidthProperties_MatchFullRangesAndScriptExtensions()
    {
        UnicodeProperties properties = new UnicodeProperties();
        HashSet<UnicodeScript> scripts = AssertRangeProperty(
            UnicodeDataParser.ReadScripts(DataPath("Scripts.txt")),
            UnicodeScript.Unknown,
            ScriptFromName,
            properties.GetScript,
            "Script",
            false);
        HashSet<UnicodeBlock> blocks = AssertRangeProperty(
            UnicodeDataParser.ReadBlocks(DataPath("Blocks.txt")),
            UnicodeBlock.NoBlock,
            BlockFromName,
            properties.GetBlock,
            "Block",
            false);
        HashSet<UnicodeEastAsianWidth> widths = AssertRangeProperty(
            UnicodeDataParser.ReadEastAsianWidth(DataPath("EastAsianWidth.txt")),
            UnicodeEastAsianWidth.Neutral,
            EastAsianWidthFromName,
            properties.GetEastAsianWidth,
            "East_Asian_Width",
            false);

        AssertEveryEnumValueSeen(scripts, "Script");
        AssertEveryEnumValueSeen(blocks, "Block");
        AssertEveryEnumValueSeen(widths, "East_Asian_Width");

        Dictionary<int, UnicodeScript[]> extensions = new Dictionary<int, UnicodeScript[]>();
        UnicodeScriptExtensionsRecord[] records = UnicodeDataParser.ReadScriptExtensions(DataPath("ScriptExtensions.txt"));
        foreach (UnicodeScriptExtensionsRecord record in records)
        {
            UnicodeScript[] expected = record.Scripts
                .Select(ScriptFromName)
                .Where(script => script != UnicodeScript.Unknown)
                .ToArray();
            for (int codePoint = record.StartCodePoint; codePoint <= record.EndCodePoint; codePoint++)
            {
                UnicodeScript[] supported = expected.Length > 0
                    ? expected
                    : new UnicodeScript[] { properties.GetScript(codePoint) };
                extensions[codePoint] = supported;
                EqualArray(supported, properties.GetScriptExtensions(codePoint), "Script_Extensions", codePoint);
            }
        }

        foreach (UnicodeScriptExtensionsRecord record in records)
        {
            AssertScriptExtensionBoundary(properties, extensions, record.StartCodePoint - 1);
            AssertScriptExtensionBoundary(properties, extensions, record.EndCodePoint + 1);
        }

        UnicodeScript[] returned = properties.GetScriptExtensions(0x30FC);
        returned[0] = UnicodeScript.Unknown;
        EqualArray(extensions[0x30FC], properties.GetScriptExtensions(0x30FC), "Script_Extensions ownership", 0x30FC);
    }

    [Test]
    public void JoiningProperties_MatchEveryExplicitRecordAndDerivedDefault()
    {
        UnicodeProperties properties = new UnicodeProperties();
        Dictionary<int, UnicodeJoiningRecord> explicitRecords = UnicodeDataParser.ReadJoiningData(
            DataPath("ArabicShaping.txt")).ToDictionary(record => record.CodePoint);
        HashSet<UnicodeJoiningType> seenTypes = new HashSet<UnicodeJoiningType>();
        HashSet<UnicodeJoiningGroup> seenGroups = new HashSet<UnicodeJoiningGroup>();

        for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
        {
            UnicodeJoiningType expectedType;
            UnicodeJoiningGroup expectedGroup;
            if (explicitRecords.TryGetValue(codePoint, out UnicodeJoiningRecord? record))
            {
                expectedType = JoiningTypeFromName(record.JoiningType);
                expectedGroup = EnumFromNormalizedName<UnicodeJoiningGroup>(record.JoiningGroup);
            }
            else
            {
                UnicodeGeneralCategory category = properties.GetGeneralCategory(codePoint);
                expectedType = category == UnicodeGeneralCategory.NonspacingMark ||
                    category == UnicodeGeneralCategory.EnclosingMark ||
                    category == UnicodeGeneralCategory.Format
                    ? UnicodeJoiningType.Transparent
                    : UnicodeJoiningType.NonJoining;
                expectedGroup = UnicodeJoiningGroup.NoJoiningGroup;
            }

            Equal(expectedType, properties.GetJoiningType(codePoint), "Joining_Type", codePoint);
            Equal(expectedGroup, properties.GetJoiningGroup(codePoint), "Joining_Group", codePoint);
            seenTypes.Add(expectedType);
            seenGroups.Add(expectedGroup);
        }

        AssertEveryEnumValueSeen(seenTypes, "Joining_Type");
        AssertEveryEnumValueSeen(seenGroups, "Joining_Group");
    }

    [Test]
    public void AgeProperty_MatchesEveryRangeDefaultAndTransition()
    {
        UnicodeProperties properties = new UnicodeProperties();
        HashSet<UnicodeAge> ages = AssertRangeProperty(
            UnicodeDataParser.ReadDerivedAge(DataPath("DerivedAge.txt")),
            UnicodeAge.Unassigned,
            AgeFromName,
            properties.GetAge,
            "Age",
            true);

        AssertEveryEnumValueSeen(ages, "Age");
    }

    [Test]
    public void BoundaryProperties_MatchEveryUnicode17RangeTransitionAndEnumValue()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodePropertyRangeRecord[] grapheme = UnicodeDataParser.ReadGraphemeBreakProperty(
            DataPath("auxiliary", "GraphemeBreakProperty.txt"));
        UnicodePropertyRangeRecord[] word = UnicodeDataParser.ReadWordBreakProperty(
            DataPath("auxiliary", "WordBreakProperty.txt"));
        UnicodePropertyRangeRecord[] sentence = UnicodeDataParser.ReadSentenceBreakProperty(
            DataPath("auxiliary", "SentenceBreakProperty.txt"));
        UnicodePropertyRangeRecord[] line = UnicodeDataParser.ReadLineBreak(DataPath("LineBreak.txt"));
        UnicodePropertyRangeRecord[] indic = UnicodeDataParser.ReadIndicConjunctBreak(
            DataPath("DerivedCoreProperties.txt"));
        UnicodePropertyRangeRecord[] hangul = UnicodeDataParser.ReadHangulSyllableType(
            DataPath("HangulSyllableType.txt"));
        UnicodePropertyRangeRecord[] emoji = UnicodeDataParser.ReadEmojiData(
            DataPath("emoji", "emoji-data.txt"));

        UnicodeGraphemeBreakClass[] expectedGrapheme = Enumerable.Repeat(
            UnicodeGraphemeBreakClass.Other,
            UnicodeCodePointCount).ToArray();
        bool[] graphemePoints = new bool[UnicodeCodePointCount];
        FillRanges(expectedGrapheme, graphemePoints, emoji.Where(record => record.PropertyName == "Extended_Pictographic"),
            name => UnicodeGraphemeBreakClass.ExtendedPictographic);
        FillRanges(expectedGrapheme, graphemePoints, hangul, UnicodeSegments.GraphemeBreakClassFromName);
        FillRanges(expectedGrapheme, graphemePoints, grapheme, UnicodeSegments.GraphemeBreakClassFromName);
        HashSet<UnicodeGraphemeBreakClass> graphemeValues = AssertMarkedPoints(
            expectedGrapheme,
            graphemePoints,
            properties.GetGraphemeBreakClass,
            "Grapheme_Cluster_Break");

        UnicodeWordBreakClass[] expectedWord = Enumerable.Repeat(
            UnicodeWordBreakClass.Other,
            UnicodeCodePointCount).ToArray();
        bool[] wordPoints = new bool[UnicodeCodePointCount];
        FillRanges(expectedWord, wordPoints, emoji.Where(record => record.PropertyName == "Extended_Pictographic"),
            name => UnicodeWordBreakClass.ExtendedPictographic);
        FillRanges(expectedWord, wordPoints, word, UnicodeSegments.WordBreakClassFromName);
        HashSet<UnicodeWordBreakClass> wordValues = AssertMarkedPoints(
            expectedWord,
            wordPoints,
            properties.GetWordBreakClass,
            "Word_Break");

        HashSet<UnicodeSentenceBreakClass> sentenceValues = AssertRangeProperty(
            sentence,
            UnicodeSentenceBreakClass.Other,
            UnicodeSegments.SentenceBreakClassFromName,
            properties.GetSentenceBreakClass,
            "Sentence_Break",
            false);
        HashSet<UnicodeLineBreakClass> lineValues = AssertRangeProperty(
            line,
            UnicodeLineBreakClass.Unknown,
            UnicodeSegments.LineBreakClassFromName,
            properties.GetLineBreakClass,
            "Line_Break",
            false);
        HashSet<UnicodeIndicConjunctBreak> indicValues = AssertRangeProperty(
            indic,
            UnicodeIndicConjunctBreak.None,
            IndicConjunctBreakFromName,
            properties.GetIndicConjunctBreak,
            "Indic_Conjunct_Break",
            false);
        HashSet<UnicodeHangulSyllableType> hangulValues = AssertRangeProperty(
            hangul,
            UnicodeHangulSyllableType.NotApplicable,
            HangulSyllableTypeFromName,
            properties.GetHangulSyllableType,
            "Hangul_Syllable_Type",
            false);

        AssertEveryEnumValueSeen(graphemeValues, "Grapheme_Cluster_Break");
        AssertEveryEnumValueSeen(wordValues, "Word_Break");
        AssertEveryEnumValueSeen(sentenceValues, "Sentence_Break");
        AssertEveryEnumValueSeen(lineValues, "Line_Break");
        AssertEveryEnumValueSeen(indicValues, "Indic_Conjunct_Break");
        AssertEveryEnumValueSeen(hangulValues, "Hangul_Syllable_Type");
    }

    [Test]
    public void BinaryProperties_MatchEveryOfficialRangeAndTransition()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadPropList(DataPath("PropList.txt"))
            .Concat(UnicodeDataParser.ReadDerivedCoreProperties(DataPath("DerivedCoreProperties.txt")))
            .Concat(UnicodeDataParser.ReadEmojiData(DataPath("emoji", "emoji-data.txt")))
            .ToArray();

        foreach (UnicodeBinaryProperty property in Enum.GetValues<UnicodeBinaryProperty>())
        {
            if (property == UnicodeBinaryProperty.PrivateUse || property == UnicodeBinaryProperty.Control)
            {
                UnicodeGeneralCategory category = property == UnicodeBinaryProperty.PrivateUse
                    ? UnicodeGeneralCategory.PrivateUse
                    : UnicodeGeneralCategory.Control;
                for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
                {
                    bool expected = properties.GetGeneralCategory(codePoint) == category;
                    Equal(expected, properties.HasBinaryProperty(codePoint, property), property.ToString(), codePoint);
                }

                continue;
            }

            string propertyName = BinaryPropertyName(property);
            bool[] expectedValues = new bool[UnicodeCodePointCount];
            bool[] points = new bool[UnicodeCodePointCount];
            foreach (UnicodePropertyRangeRecord record in records.Where(record => record.PropertyName == propertyName))
            {
                for (int codePoint = record.StartCodePoint; codePoint <= record.EndCodePoint; codePoint++)
                {
                    expectedValues[codePoint] = true;
                    points[codePoint] = true;
                }

                MarkPoint(points, record.StartCodePoint - 1);
                MarkPoint(points, record.EndCodePoint + 1);
            }

            for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
            {
                if (points[codePoint])
                {
                    Equal(expectedValues[codePoint], properties.HasBinaryProperty(codePoint, property), propertyName, codePoint);
                }
            }
        }
    }

    [Test]
    public void PropertyLookups_ReturnDocumentedDefaultsForOutOfRangeCodePoints()
    {
        UnicodeProperties properties = new UnicodeProperties();
        int[] invalidCodePoints = new int[] { -1, 0x110000, int.MinValue, int.MaxValue };

        foreach (int codePoint in invalidCodePoints)
        {
            Equal(UnicodeGeneralCategory.OtherNotAssigned, properties.GetGeneralCategory(codePoint), "General_Category default", codePoint);
            Equal(0, properties.GetCombiningClass(codePoint), "Canonical_Combining_Class default", codePoint);
            Equal(UnicodeBidiClass.LeftToRight, properties.GetBidiClass(codePoint), "Bidi_Class default", codePoint);
            Equal(false, properties.IsBidiMirrored(codePoint), "Bidi_Mirrored default", codePoint);
            Equal(codePoint, properties.GetBidiMirror(codePoint), "Bidi_Mirroring_Glyph default", codePoint);
            Equal(codePoint, properties.GetBidiPairedBracket(codePoint), "Bidi_Paired_Bracket default", codePoint);
            Equal(UnicodeBidiPairedBracketType.None, properties.GetBidiPairedBracketType(codePoint), "Bidi_Paired_Bracket_Type default", codePoint);
            Equal(UnicodeDecompositionType.None, properties.GetDecompositionType(codePoint), "Decomposition_Type default", codePoint);
            EqualArray(Array.Empty<int>(), properties.GetDecompositionMapping(codePoint), "Decomposition_Mapping default", codePoint);
            Equal(UnicodeScript.Unknown, properties.GetScript(codePoint), "Script default", codePoint);
            EqualArray(new UnicodeScript[] { UnicodeScript.Unknown }, properties.GetScriptExtensions(codePoint), "Script_Extensions default", codePoint);
            Equal(UnicodeBlock.NoBlock, properties.GetBlock(codePoint), "Block default", codePoint);
            Equal(UnicodeEastAsianWidth.Neutral, properties.GetEastAsianWidth(codePoint), "East_Asian_Width default", codePoint);
            Equal(-1.0, properties.GetNumericValue(codePoint), "Numeric_Value default", codePoint);
            Equal(UnicodeNumericType.None, properties.GetNumericType(codePoint), "Numeric_Type default", codePoint);
            Equal(false, properties.GetNumericValueExact(codePoint).HasValue, "Numeric_Value exact default", codePoint);
            Equal(UnicodeJoiningType.NonJoining, properties.GetJoiningType(codePoint), "Joining_Type default", codePoint);
            Equal(UnicodeJoiningGroup.NoJoiningGroup, properties.GetJoiningGroup(codePoint), "Joining_Group default", codePoint);
            Equal(UnicodeAge.Unassigned, properties.GetAge(codePoint), "Age default", codePoint);
            Equal(UnicodeIndicConjunctBreak.None, properties.GetIndicConjunctBreak(codePoint), "Indic_Conjunct_Break default", codePoint);
            Equal(UnicodeHangulSyllableType.NotApplicable, properties.GetHangulSyllableType(codePoint), "Hangul_Syllable_Type default", codePoint);
            Equal(UnicodeGraphemeBreakClass.Other, properties.GetGraphemeBreakClass(codePoint), "Grapheme_Cluster_Break default", codePoint);
            Equal(UnicodeWordBreakClass.Other, properties.GetWordBreakClass(codePoint), "Word_Break default", codePoint);
            Equal(UnicodeSentenceBreakClass.Other, properties.GetSentenceBreakClass(codePoint), "Sentence_Break default", codePoint);
            Equal(UnicodeLineBreakClass.Unknown, properties.GetLineBreakClass(codePoint), "Line_Break default", codePoint);
            Equal("", properties.GetName(codePoint), "Name default", codePoint);

            foreach (UnicodeBinaryProperty property in Enum.GetValues<UnicodeBinaryProperty>())
            {
                Equal(false, properties.HasBinaryProperty(codePoint, property), property + " default", codePoint);
            }
        }

        int surrogate = 0xD800;
        Equal(UnicodeGeneralCategory.Surrogate, properties.GetGeneralCategory(surrogate), "Surrogate General_Category", surrogate);
        Equal(UnicodeBidiClass.LeftToRight, properties.GetBidiClass(surrogate), "Surrogate Bidi_Class", surrogate);
        Equal(UnicodeDecompositionType.None, properties.GetDecompositionType(surrogate), "Surrogate Decomposition_Type", surrogate);
        Equal(UnicodeScript.Unknown, properties.GetScript(surrogate), "Surrogate Script", surrogate);
        Equal(UnicodeEastAsianWidth.Neutral, properties.GetEastAsianWidth(surrogate), "Surrogate East_Asian_Width", surrogate);
        Equal(UnicodeNumericType.None, properties.GetNumericType(surrogate), "Surrogate Numeric_Type", surrogate);
        Equal(UnicodeJoiningType.NonJoining, properties.GetJoiningType(surrogate), "Surrogate Joining_Type", surrogate);
        Equal(UnicodeAge.Version2_0, properties.GetAge(surrogate), "Surrogate Age", surrogate);
        Equal(UnicodeGraphemeBreakClass.Other, properties.GetGraphemeBreakClass(surrogate), "Surrogate Grapheme_Cluster_Break", surrogate);
        Equal(UnicodeWordBreakClass.Other, properties.GetWordBreakClass(surrogate), "Surrogate Word_Break", surrogate);
        Equal(UnicodeSentenceBreakClass.Other, properties.GetSentenceBreakClass(surrogate), "Surrogate Sentence_Break", surrogate);
        Equal(UnicodeLineBreakClass.Surrogate, properties.GetLineBreakClass(surrogate), "Surrogate Line_Break", surrogate);
    }

    static HashSet<T> AssertRangeProperty<T>(
        IEnumerable<UnicodePropertyRangeRecord> sourceRecords,
        T defaultValue,
        Func<string, T> convert,
        Func<int, T> lookup,
        string propertyName,
        bool assertEveryCodePoint)
    {
        UnicodePropertyRangeRecord[] records = sourceRecords.ToArray();
        T[] expected = Enumerable.Repeat(defaultValue, UnicodeCodePointCount).ToArray();
        bool[] points = new bool[UnicodeCodePointCount];
        FillRanges(expected, points, records, convert);

        HashSet<T> seen = new HashSet<T> { defaultValue };
        for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
        {
            if (assertEveryCodePoint || points[codePoint])
            {
                Equal(expected[codePoint], lookup(codePoint), propertyName, codePoint);
                seen.Add(expected[codePoint]);
            }
        }

        return seen;
    }

    static void FillRanges<T>(
        T[] expected,
        bool[] points,
        IEnumerable<UnicodePropertyRangeRecord> records,
        Func<string, T> convert)
    {
        foreach (UnicodePropertyRangeRecord record in records)
        {
            T value = convert(record.PropertyName);
            for (int codePoint = record.StartCodePoint; codePoint <= record.EndCodePoint; codePoint++)
            {
                expected[codePoint] = value;
                points[codePoint] = true;
            }

            MarkPoint(points, record.StartCodePoint - 1);
            MarkPoint(points, record.EndCodePoint + 1);
        }
    }

    static HashSet<T> AssertMarkedPoints<T>(T[] expected, bool[] points, Func<int, T> lookup, string propertyName)
    {
        HashSet<T> seen = new HashSet<T>();
        for (int codePoint = 0; codePoint < UnicodeCodePointCount; codePoint++)
        {
            if (points[codePoint])
            {
                Equal(expected[codePoint], lookup(codePoint), propertyName, codePoint);
                seen.Add(expected[codePoint]);
            }
        }

        seen.Add(expected[0x0378]);
        Equal(expected[0x0378], lookup(0x0378), propertyName + " default", 0x0378);
        return seen;
    }

    static void MarkPoint(bool[] points, int codePoint)
    {
        if (codePoint >= 0 && codePoint < points.Length)
        {
            points[codePoint] = true;
        }
    }

    static void AssertScriptExtensionBoundary(
        UnicodeProperties properties,
        Dictionary<int, UnicodeScript[]> extensions,
        int codePoint)
    {
        if (codePoint < 0 || codePoint >= UnicodeCodePointCount)
        {
            return;
        }

        UnicodeScript[]? expected;
        if (!extensions.TryGetValue(codePoint, out expected))
        {
            expected = new UnicodeScript[] { properties.GetScript(codePoint) };
        }

        EqualArray(expected, properties.GetScriptExtensions(codePoint), "Script_Extensions transition", codePoint);
    }

    static void AssertNumericBoundary(UnicodeProperties properties, HashSet<int> numericCodePoints, int codePoint)
    {
        if (codePoint < 0 || codePoint >= UnicodeCodePointCount)
        {
            return;
        }

        bool expected = numericCodePoints.Contains(codePoint);
        Equal(expected, properties.GetNumericValueExact(codePoint).HasValue, "Numeric_Value transition", codePoint);
        if (!expected)
        {
            Equal(UnicodeNumericType.None, properties.GetNumericType(codePoint), "Numeric_Type transition", codePoint);
        }
    }

    static int[] HangulDecomposition(int codePoint)
    {
        int syllableIndex = codePoint - 0xAC00;
        int leading = 0x1100 + (syllableIndex / 588);
        int vowel = 0x1161 + ((syllableIndex % 588) / 28);
        int trailingIndex = syllableIndex % 28;
        if (trailingIndex == 0)
        {
            return new int[] { leading, vowel };
        }

        return new int[] { leading, vowel, 0x11A7 + trailingIndex };
    }

    static UnicodeGeneralCategory GeneralCategoryFromName(string name)
    {
        return name switch
        {
            "Lu" => UnicodeGeneralCategory.UppercaseLetter,
            "Ll" => UnicodeGeneralCategory.LowercaseLetter,
            "Lt" => UnicodeGeneralCategory.TitlecaseLetter,
            "Lm" => UnicodeGeneralCategory.ModifierLetter,
            "Lo" => UnicodeGeneralCategory.OtherLetter,
            "Mn" => UnicodeGeneralCategory.NonspacingMark,
            "Mc" => UnicodeGeneralCategory.SpacingMark,
            "Me" => UnicodeGeneralCategory.EnclosingMark,
            "Nd" => UnicodeGeneralCategory.DecimalNumber,
            "Nl" => UnicodeGeneralCategory.LetterNumber,
            "No" => UnicodeGeneralCategory.OtherNumber,
            "Pc" => UnicodeGeneralCategory.ConnectorPunctuation,
            "Pd" => UnicodeGeneralCategory.DashPunctuation,
            "Ps" => UnicodeGeneralCategory.OpenPunctuation,
            "Pe" => UnicodeGeneralCategory.ClosePunctuation,
            "Pi" => UnicodeGeneralCategory.InitialPunctuation,
            "Pf" => UnicodeGeneralCategory.FinalPunctuation,
            "Po" => UnicodeGeneralCategory.OtherPunctuation,
            "Sm" => UnicodeGeneralCategory.MathSymbol,
            "Sc" => UnicodeGeneralCategory.CurrencySymbol,
            "Sk" => UnicodeGeneralCategory.ModifierSymbol,
            "So" => UnicodeGeneralCategory.OtherSymbol,
            "Zs" => UnicodeGeneralCategory.SpaceSeparator,
            "Zl" => UnicodeGeneralCategory.LineSeparator,
            "Zp" => UnicodeGeneralCategory.ParagraphSeparator,
            "Cc" => UnicodeGeneralCategory.Control,
            "Cf" => UnicodeGeneralCategory.Format,
            "Cs" => UnicodeGeneralCategory.Surrogate,
            "Co" => UnicodeGeneralCategory.PrivateUse,
            _ => UnicodeGeneralCategory.OtherNotAssigned,
        };
    }

    static UnicodeBidiClass BidiClassFromName(string name)
    {
        return name switch
        {
            "R" or "Right_To_Left" => UnicodeBidiClass.RightToLeft,
            "AL" or "Arabic_Letter" => UnicodeBidiClass.ArabicLetter,
            "EN" or "European_Number" => UnicodeBidiClass.EuropeanNumber,
            "ES" or "European_Separator" => UnicodeBidiClass.EuropeanSeparator,
            "ET" or "European_Terminator" => UnicodeBidiClass.EuropeanTerminator,
            "AN" or "Arabic_Number" => UnicodeBidiClass.ArabicNumber,
            "CS" or "Common_Separator" => UnicodeBidiClass.CommonSeparator,
            "NSM" or "Nonspacing_Mark" => UnicodeBidiClass.NonspacingMark,
            "BN" or "Boundary_Neutral" or "LRE" or "RLE" or "LRO" or "RLO" or "PDF" or
                "LRI" or "RLI" or "FSI" or "PDI" => UnicodeBidiClass.BoundaryNeutral,
            "B" or "Paragraph_Separator" => UnicodeBidiClass.ParagraphSeparator,
            "S" or "Segment_Separator" => UnicodeBidiClass.SegmentSeparator,
            "WS" or "White_Space" => UnicodeBidiClass.WhiteSpace,
            "ON" or "Other_Neutral" => UnicodeBidiClass.OtherNeutral,
            _ => UnicodeBidiClass.LeftToRight,
        };
    }

    static UnicodeDecompositionType DecompositionTypeFromName(string name)
    {
        return name.Length == 0
            ? UnicodeDecompositionType.Canonical
            : EnumFromNormalizedName<UnicodeDecompositionType>(name);
    }

    static UnicodeNumericType NumericTypeFromRecord(UnicodeDataRecord record)
    {
        if (record.DecimalDigitValue >= 0)
        {
            return UnicodeNumericType.Decimal;
        }

        return record.DigitValue >= 0 ? UnicodeNumericType.Digit : UnicodeNumericType.Numeric;
    }

    static UnicodeScript ScriptFromName(string name)
    {
        return name switch
        {
            "Common" or "Zyyy" => UnicodeScript.Common,
            "Inherited" or "Zinh" => UnicodeScript.Inherited,
            "Latin" or "Latn" => UnicodeScript.Latin,
            "Greek" or "Grek" => UnicodeScript.Greek,
            "Cyrillic" or "Cyrl" => UnicodeScript.Cyrillic,
            "Hebrew" or "Hebr" => UnicodeScript.Hebrew,
            "Arabic" or "Arab" => UnicodeScript.Arabic,
            "Devanagari" or "Deva" => UnicodeScript.Devanagari,
            "Hiragana" or "Hira" => UnicodeScript.Hiragana,
            "Katakana" or "Kana" => UnicodeScript.Katakana,
            "Han" or "Hani" => UnicodeScript.Han,
            "Hangul" or "Hang" => UnicodeScript.Hangul,
            "Armenian" or "Armn" => UnicodeScript.Armenian,
            "Bengali" or "Beng" => UnicodeScript.Bengali,
            "Ethiopic" or "Ethi" => UnicodeScript.Ethiopic,
            "Georgian" or "Geor" => UnicodeScript.Georgian,
            "Grantha" or "Gran" => UnicodeScript.Grantha,
            "Gujarati" or "Gujr" => UnicodeScript.Gujarati,
            "Gurmukhi" or "Guru" => UnicodeScript.Gurmukhi,
            "Kannada" or "Knda" => UnicodeScript.Kannada,
            "Khmer" or "Khmr" => UnicodeScript.Khmer,
            "Lao" or "Laoo" => UnicodeScript.Lao,
            "Malayalam" or "Mlym" => UnicodeScript.Malayalam,
            "Myanmar" or "Mymr" => UnicodeScript.Myanmar,
            "Oriya" or "Orya" => UnicodeScript.Oriya,
            "Sinhala" or "Sinh" => UnicodeScript.Sinhala,
            "Tamil" or "Taml" => UnicodeScript.Tamil,
            "Telugu" or "Telu" => UnicodeScript.Telugu,
            "Thaana" or "Thaa" => UnicodeScript.Thaana,
            "Thai" => UnicodeScript.Thai,
            "Tibetan" or "Tibt" => UnicodeScript.Tibetan,
            _ => UnicodeScript.Unknown,
        };
    }

    static UnicodeBlock BlockFromName(string name)
    {
        return name switch
        {
            "Basic Latin" => UnicodeBlock.BasicLatin,
            "Latin-1 Supplement" => UnicodeBlock.Latin1Supplement,
            "Combining Diacritical Marks" => UnicodeBlock.CombiningDiacriticalMarks,
            "Greek and Coptic" => UnicodeBlock.GreekAndCoptic,
            "Cyrillic" => UnicodeBlock.Cyrillic,
            "Hebrew" => UnicodeBlock.Hebrew,
            "Arabic" => UnicodeBlock.Arabic,
            "Devanagari" => UnicodeBlock.Devanagari,
            "Hiragana" => UnicodeBlock.Hiragana,
            "Katakana" => UnicodeBlock.Katakana,
            "CJK Unified Ideographs" => UnicodeBlock.CjkUnifiedIdeographs,
            "Hangul Syllables" => UnicodeBlock.HangulSyllables,
            "Private Use Area" => UnicodeBlock.PrivateUseArea,
            "Specials" => UnicodeBlock.Specials,
            _ => UnicodeBlock.NoBlock,
        };
    }

    static UnicodeEastAsianWidth EastAsianWidthFromName(string name)
    {
        return name switch
        {
            "A" => UnicodeEastAsianWidth.Ambiguous,
            "H" => UnicodeEastAsianWidth.Halfwidth,
            "F" => UnicodeEastAsianWidth.Fullwidth,
            "Na" => UnicodeEastAsianWidth.Narrow,
            "W" => UnicodeEastAsianWidth.Wide,
            _ => UnicodeEastAsianWidth.Neutral,
        };
    }

    static UnicodeJoiningType JoiningTypeFromName(string name)
    {
        return name switch
        {
            "D" => UnicodeJoiningType.DualJoining,
            "R" => UnicodeJoiningType.RightJoining,
            "C" => UnicodeJoiningType.JoinCausing,
            "T" => UnicodeJoiningType.Transparent,
            "L" => UnicodeJoiningType.LeftJoining,
            _ => UnicodeJoiningType.NonJoining,
        };
    }

    static UnicodeAge AgeFromName(string name)
    {
        return Enum.Parse<UnicodeAge>("Version" + name.Replace('.', '_'));
    }

    static UnicodeIndicConjunctBreak IndicConjunctBreakFromName(string name)
    {
        return EnumFromNormalizedName<UnicodeIndicConjunctBreak>(name);
    }

    static UnicodeHangulSyllableType HangulSyllableTypeFromName(string name)
    {
        return name switch
        {
            "L" => UnicodeHangulSyllableType.L,
            "V" => UnicodeHangulSyllableType.V,
            "T" => UnicodeHangulSyllableType.T,
            "LV" => UnicodeHangulSyllableType.Lv,
            "LVT" => UnicodeHangulSyllableType.Lvt,
            _ => UnicodeHangulSyllableType.NotApplicable,
        };
    }

    static string BinaryPropertyName(UnicodeBinaryProperty property)
    {
        return property switch
        {
            UnicodeBinaryProperty.WhiteSpace => "White_Space",
            UnicodeBinaryProperty.DefaultIgnorableCodePoint => "Default_Ignorable_Code_Point",
            UnicodeBinaryProperty.Uppercase => "Uppercase",
            UnicodeBinaryProperty.Lowercase => "Lowercase",
            UnicodeBinaryProperty.Alphabetic => "Alphabetic",
            UnicodeBinaryProperty.Emoji => "Emoji",
            UnicodeBinaryProperty.EmojiPresentation => "Emoji_Presentation",
            UnicodeBinaryProperty.ExtendedPictographic => "Extended_Pictographic",
            UnicodeBinaryProperty.EmojiComponent => "Emoji_Component",
            UnicodeBinaryProperty.EmojiModifier => "Emoji_Modifier",
            UnicodeBinaryProperty.EmojiModifierBase => "Emoji_Modifier_Base",
            UnicodeBinaryProperty.VariationSelector => "Variation_Selector",
            UnicodeBinaryProperty.NoncharacterCodePoint => "Noncharacter_Code_Point",
            UnicodeBinaryProperty.PatternWhiteSpace => "Pattern_White_Space",
            UnicodeBinaryProperty.PatternSyntax => "Pattern_Syntax",
            _ => "",
        };
    }

    static T EnumFromNormalizedName<T>(string source) where T : struct, Enum
    {
        string normalizedSource = NormalizeName(source);
        foreach (string name in Enum.GetNames<T>())
        {
            if (string.Equals(normalizedSource, NormalizeName(name), StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<T>(name);
            }
        }

        Assert.Fail("No " + typeof(T).Name + " value maps Unicode property value " + source + ".");
        return default;
    }

    static string NormalizeName(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }

    static bool IsRangeFirst(UnicodeDataRecord record)
    {
        return record.Name.EndsWith(", First>", StringComparison.Ordinal);
    }

    static string DataPath(params string[] parts)
    {
        string path = Path.Combine(new string[] { FindRepositoryRoot(), "data", "Unicode17.0.0" }.Concat(parts).ToArray());
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "todo.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    static void AssertEveryEnumValueSeen<T>(HashSet<T> seen, string propertyName) where T : struct, Enum
    {
        foreach (T value in Enum.GetValues<T>().Distinct())
        {
            if (!seen.Contains(value))
            {
                Assert.Fail(propertyName + " did not exercise enum value " + value + ".");
            }
        }
    }

    static void Equal<T>(T expected, T actual, string propertyName, int codePoint)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Assert.Fail(propertyName + " mismatch at U+" + codePoint.ToString("X") +
                ": expected " + expected + ", actual " + actual + ".");
        }
    }

    static void EqualArray<T>(T[] expected, T[] actual, string propertyName, int codePoint)
    {
        if (!expected.SequenceEqual(actual))
        {
            Assert.Fail(propertyName + " mismatch at U+" + codePoint.ToString("X") +
                ": expected [" + string.Join(", ", expected) + "], actual [" + string.Join(", ", actual) + "].");
        }
    }
}
