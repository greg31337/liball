namespace unicode.Tools.UnicodeDataGenerator;

public static class UnicodeTableWriter
{
    public static void WriteCoreTables(string dataDirectory, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        UnicodeDataRecord[] unicodeData = UnicodeDataParser.ReadUnicodeData(Path.Combine(dataDirectory, "UnicodeData.txt"));
        UnicodePropertyRangeRecord[] propList = UnicodeDataParser.ReadPropList(Path.Combine(dataDirectory, "PropList.txt"));
        UnicodePropertyRangeRecord[] derivedCoreProperties = UnicodeDataParser.ReadDerivedCoreProperties(Path.Combine(dataDirectory, "DerivedCoreProperties.txt"));
        UnicodePropertyRangeRecord[] scripts = UnicodeDataParser.ReadScripts(Path.Combine(dataDirectory, "Scripts.txt"));
        UnicodeScriptExtensionsRecord[] scriptExtensions = File.Exists(Path.Combine(dataDirectory, "ScriptExtensions.txt"))
            ? UnicodeDataParser.ReadScriptExtensions(Path.Combine(dataDirectory, "ScriptExtensions.txt"))
            : Array.Empty<UnicodeScriptExtensionsRecord>();
        UnicodePropertyRangeRecord[] blocks = File.Exists(Path.Combine(dataDirectory, "Blocks.txt"))
            ? UnicodeDataParser.ReadBlocks(Path.Combine(dataDirectory, "Blocks.txt"))
            : Array.Empty<UnicodePropertyRangeRecord>();
        UnicodePropertyRangeRecord[] eastAsianWidths = UnicodeDataParser.ReadEastAsianWidth(Path.Combine(dataDirectory, "EastAsianWidth.txt"));
        UnicodePropertyRangeRecord[] bidiClasses = File.Exists(Path.Combine(dataDirectory, "extracted", "DerivedBidiClass.txt"))
            ? UnicodeDataParser.ReadPropertyRangesWithMissing(Path.Combine(dataDirectory, "extracted", "DerivedBidiClass.txt"))
            : Array.Empty<UnicodePropertyRangeRecord>();
        UnicodeBidiMirrorRecord[] bidiMirrors = UnicodeDataParser.ReadBidiMirroring(Path.Combine(dataDirectory, "BidiMirroring.txt"));
        UnicodeBidiBracketRecord[] bidiBrackets = UnicodeDataParser.ReadBidiBrackets(Path.Combine(dataDirectory, "BidiBrackets.txt"));
        UnicodeJoiningRecord[] joiningData = File.Exists(Path.Combine(dataDirectory, "ArabicShaping.txt"))
            ? UnicodeDataParser.ReadJoiningData(Path.Combine(dataDirectory, "ArabicShaping.txt"))
            : Array.Empty<UnicodeJoiningRecord>();
        UnicodePropertyRangeRecord[] ages = File.Exists(Path.Combine(dataDirectory, "DerivedAge.txt"))
            ? UnicodeDataParser.ReadDerivedAge(Path.Combine(dataDirectory, "DerivedAge.txt"))
            : Array.Empty<UnicodePropertyRangeRecord>();
        UnicodeDecompositionRecord[] decompositions = UnicodeDataParser.ReadDecompositionMappings(Path.Combine(dataDirectory, "UnicodeData.txt"));
        UnicodeCompositionExclusionRecord[] compositionExclusions = UnicodeDataParser.ReadCompositionExclusions(Path.Combine(dataDirectory, "CompositionExclusions.txt"));
        UnicodeNormalizationQuickCheckRecord[] quickChecks = UnicodeDataParser.ReadNormalizationQuickChecks(Path.Combine(dataDirectory, "DerivedNormalizationProps.txt"));
        UnicodeNormalizationMappingRecord[] nfkcCaseFoldMappings = UnicodeDataParser.ReadNfkcCaseFoldMappings(Path.Combine(dataDirectory, "DerivedNormalizationProps.txt"));
        UnicodeSpecialCasingRecord[] specialCasing = UnicodeDataParser.ReadSpecialCasing(Path.Combine(dataDirectory, "SpecialCasing.txt"));
        UnicodeCaseFoldingRecord[] caseFolding = UnicodeDataParser.ReadCaseFolding(Path.Combine(dataDirectory, "CaseFolding.txt"));
        UnicodePropertyRangeRecord[] graphemeBreaks = UnicodeDataParser.ReadGraphemeBreakProperty(Path.Combine(dataDirectory, "auxiliary", "GraphemeBreakProperty.txt"));
        UnicodePropertyRangeRecord[] wordBreaks = UnicodeDataParser.ReadWordBreakProperty(Path.Combine(dataDirectory, "auxiliary", "WordBreakProperty.txt"));
        UnicodePropertyRangeRecord[] sentenceBreaks = UnicodeDataParser.ReadSentenceBreakProperty(Path.Combine(dataDirectory, "auxiliary", "SentenceBreakProperty.txt"));
        UnicodePropertyRangeRecord[] lineBreaks = UnicodeDataParser.ReadLineBreak(Path.Combine(dataDirectory, "LineBreak.txt"));
        UnicodePropertyRangeRecord[] indicConjunctBreaks = UnicodeDataParser.ReadIndicConjunctBreak(Path.Combine(dataDirectory, "DerivedCoreProperties.txt"));
        UnicodePropertyRangeRecord[] hangulSyllableTypes = UnicodeDataParser.ReadHangulSyllableType(Path.Combine(dataDirectory, "HangulSyllableType.txt"));
        UnicodePropertyRangeRecord[] emojiBoundaryProperties = UnicodeDataParser.ReadEmojiData(Path.Combine(dataDirectory, "emoji", "emoji-data.txt"));
        UnicodeConfusableRecord[] confusables = UnicodeDataParser.ReadConfusables(Path.Combine(dataDirectory, "security", "confusables.txt"));
        UnicodeIdentifierStatusRecord[] identifierStatuses = UnicodeDataParser.ReadIdentifierStatus(Path.Combine(dataDirectory, "security", "IdentifierStatus.txt"));
        UnicodeIdentifierTypeRecord[] identifierTypes = UnicodeDataParser.ReadIdentifierType(Path.Combine(dataDirectory, "security", "IdentifierType.txt"));
        UnicodeNamedSequenceRecord[] namedSequences = File.Exists(Path.Combine(dataDirectory, "NamedSequences.txt"))
            ? UnicodeDataParser.ReadNamedSequences(Path.Combine(dataDirectory, "NamedSequences.txt"))
            : Array.Empty<UnicodeNamedSequenceRecord>();
        UnicodeNameAliasRecord[] nameAliases = File.Exists(Path.Combine(dataDirectory, "NameAliases.txt"))
            ? UnicodeDataParser.ReadNameAliases(Path.Combine(dataDirectory, "NameAliases.txt"))
            : Array.Empty<UnicodeNameAliasRecord>();
        UnicodePropertyAliasRecord[] propertyAliases = File.Exists(Path.Combine(dataDirectory, "PropertyAliases.txt"))
            ? UnicodeDataParser.ReadPropertyAliases(Path.Combine(dataDirectory, "PropertyAliases.txt"))
            : Array.Empty<UnicodePropertyAliasRecord>();
        UnicodePropertyValueAliasRecord[] propertyValueAliases = File.Exists(Path.Combine(dataDirectory, "PropertyValueAliases.txt"))
            ? UnicodeDataParser.ReadPropertyValueAliases(Path.Combine(dataDirectory, "PropertyValueAliases.txt"))
            : Array.Empty<UnicodePropertyValueAliasRecord>();

        Directory.CreateDirectory(outputDirectory);
        WriteCategoryTables(Path.Combine(outputDirectory, "UnicodeCategoryTables.cs"), unicodeData);
        WritePropertyTables(
            Path.Combine(outputDirectory, "UnicodePropertyTables.cs"),
            CombinePropertyRanges(CombinePropertyRanges(propList, derivedCoreProperties), emojiBoundaryProperties));
        WriteIdentifierTables(Path.Combine(outputDirectory, "UnicodeIdentifierTables.cs"), derivedCoreProperties, propList);
        WriteScriptTables(Path.Combine(outputDirectory, "UnicodeScriptTables.cs"), scripts, scriptExtensions);
        WriteBlockTables(Path.Combine(outputDirectory, "UnicodeBlockTables.cs"), blocks);
        WriteWidthTables(Path.Combine(outputDirectory, "UnicodeWidthTables.cs"), eastAsianWidths);
        WriteBidiTables(Path.Combine(outputDirectory, "UnicodeBidiTables.cs"), unicodeData, bidiClasses, bidiMirrors, bidiBrackets);
        WriteJoiningTables(Path.Combine(outputDirectory, "UnicodeJoiningTables.cs"), joiningData);
        WriteAgeTables(Path.Combine(outputDirectory, "UnicodeAgeTables.cs"), ages);
        WriteNumericTables(Path.Combine(outputDirectory, "UnicodeNumericTables.cs"), unicodeData);
        WriteNameTables(Path.Combine(outputDirectory, "UnicodeNameTables.cs"), unicodeData);
        WriteNamedSequenceTables(Path.Combine(outputDirectory, "UnicodeNamedSequenceTables.cs"), namedSequences);
        WriteAliasTables(
            Path.Combine(outputDirectory, "UnicodeAliasTables.cs"),
            nameAliases,
            propertyAliases,
            propertyValueAliases);
        WriteNormalizationTablesWithCaseFold(
            Path.Combine(outputDirectory, "UnicodeNormalizationTables.cs"),
            decompositions,
            compositionExclusions,
            quickChecks,
            nfkcCaseFoldMappings);
        WriteCaseTables(
            Path.Combine(outputDirectory, "UnicodeCaseTables.cs"),
            unicodeData,
            specialCasing,
            caseFolding,
            derivedCoreProperties,
            propList);
        WriteBoundaryTables(Path.Combine(outputDirectory, "UnicodeBoundaryTables.cs"), graphemeBreaks, wordBreaks, sentenceBreaks, lineBreaks, indicConjunctBreaks, hangulSyllableTypes, emojiBoundaryProperties);
        WriteSecurityTables(Path.Combine(outputDirectory, "UnicodeSecurityTables.cs"), confusables, identifierStatuses, identifierTypes);
        WritePdfEncodingTables(Path.Combine(outputDirectory, "UnicodePdfEncodingTables.cs"));
    }

    public static void WritePlaceholderSource(string outputPath, string className)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("namespace unicode;");
        writer.WriteLine();
        writer.WriteLine("internal static class " + className);
        writer.WriteLine("{");
        writer.WriteLine("    internal const string UnicodeVersion = \"" + UnicodeDataFiles.UnicodeVersion + "\";");
        writer.WriteLine("}");
    }

    public static void WritePdfEncodingTables(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodePdfEncodingTables");
        WriteIntArrayMethod(writer, "PdfDocEncodingToUnicode", CreatePdfDocEncodingToUnicode());
        WriteRangeMethod(
            writer,
            "PdfDocEncodingIdentityRanges",
            new int[] { 0x0000, 0x0020, 0x00A1, 0x00AE },
            new int[] { 0x0017, 0x007E, 0x00AC, 0x00FF });
        WriteRangeValueMethod(
            writer,
            "PdfDocEncodingUnicodeToByte",
            PdfDocEncodingSpecialCodePoints(),
            PdfDocEncodingSpecialCodePoints(),
            PdfDocEncodingSpecialBytes());
        WriteFooter(writer);
    }

    public static void WriteJoiningTables(string outputPath, UnicodeJoiningRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        List<string> typeNames = new List<string>();
        Dictionary<string, int> typeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        List<string> groupNames = new List<string>();
        Dictionary<string, int> groupIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] starts = new int[records.Length];
        int[] ends = new int[records.Length];
        int[] typeValues = new int[records.Length];
        int[] groupValues = new int[records.Length];
        int index = 0;

        while (index < records.Length)
        {
            starts[index] = records[index].CodePoint;
            ends[index] = records[index].CodePoint;
            typeValues[index] = GetValueIndex(records[index].JoiningType, typeNames, typeIndexes);
            groupValues[index] = GetValueIndex(records[index].JoiningGroup, groupNames, groupIndexes);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeJoiningTables");
        WriteRangeValueMethod(writer, "JoiningTypes", starts, ends, typeValues);
        WriteStringArrayMethod(writer, "JoiningTypeNames", typeNames.ToArray());
        WriteRangeValueMethod(writer, "JoiningGroups", starts, ends, groupValues);
        WriteStringArrayMethod(writer, "JoiningGroupNames", groupNames.ToArray());
        WriteFooter(writer);
    }

    public static void WriteAgeTables(string outputPath, UnicodePropertyRangeRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        WriteNamedRangeTable(outputPath, "UnicodeAgeTables", "Ages", "AgeNames", records);
    }

    public static void WriteCategoryTables(string outputPath, UnicodeDataRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        List<int> starts = new List<int>();
        List<int> ends = new List<int>();
        List<int> values = new List<int>();
        List<int> combiningStarts = new List<int>();
        List<int> combiningEnds = new List<int>();
        List<int> combiningValues = new List<int>();
        int index = 0;

        while (index < records.Length)
        {
            int endCodePoint = records[index].CodePoint;
            if (IsRangeFirst(records[index]) && index + 1 < records.Length && IsRangeLast(records[index + 1]))
            {
                endCodePoint = records[index + 1].CodePoint;
            }

            starts.Add(records[index].CodePoint);
            ends.Add(endCodePoint);
            values.Add(GetGeneralCategoryValue(records[index].GeneralCategory));
            if (records[index].CombiningClass != 0)
            {
                combiningStarts.Add(records[index].CodePoint);
                combiningEnds.Add(endCodePoint);
                combiningValues.Add(records[index].CombiningClass);
            }

            if (endCodePoint != records[index].CodePoint)
            {
                index = index + 1;
            }

            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeCategoryTables");
        WriteRangeValueMethod(writer, "GeneralCategories", starts.ToArray(), ends.ToArray(), values.ToArray());
        WriteStringArrayMethod(writer, "GeneralCategoryNames", GeneralCategoryNames());
        WriteRangeValueMethod(writer, "CombiningClasses", combiningStarts.ToArray(), combiningEnds.ToArray(), combiningValues.ToArray());
        WriteFooter(writer);
    }

    public static void WritePropertyTables(string outputPath, UnicodePropertyRangeRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        List<int> starts = new List<int>();
        List<int> ends = new List<int>();
        List<int> values = new List<int>();
        int index = 0;

        while (index < records.Length)
        {
            int value = GetBinaryPropertyValue(records[index].PropertyName);
            if (value >= 0)
            {
                starts.Add(records[index].StartCodePoint);
                ends.Add(records[index].EndCodePoint);
                values.Add(value);
            }

            index = index + 1;
        }

        int[] startValues = starts.ToArray();
        int[] endValues = ends.ToArray();
        int[] propertyValues = values.ToArray();
        SortRangesByValueAndStart(startValues, endValues, propertyValues);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodePropertyTables");
        WriteRangeValueMethod(writer, "BinaryProperties", startValues, endValues, propertyValues);
        WriteStringArrayMethod(writer, "BinaryPropertyNames", BinaryPropertyNames());
        WriteFooter(writer);
    }

    public static void WriteIdentifierTables(
        string outputPath,
        UnicodePropertyRangeRecord[] derivedCoreProperties,
        UnicodePropertyRangeRecord[] propList)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(derivedCoreProperties);
        ArgumentNullException.ThrowIfNull(propList);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeIdentifierTables");
        WriteRangeMethod(writer, "IdStart", FilterPropertyRanges(derivedCoreProperties, "ID_Start"));
        WriteRangeMethod(writer, "IdContinue", FilterPropertyRanges(derivedCoreProperties, "ID_Continue"));
        WriteRangeMethod(writer, "XidStart", FilterPropertyRanges(derivedCoreProperties, "XID_Start"));
        WriteRangeMethod(writer, "XidContinue", FilterPropertyRanges(derivedCoreProperties, "XID_Continue"));
        WriteRangeMethod(writer, "PatternSyntax", FilterPropertyRanges(propList, "Pattern_Syntax"));
        WriteRangeMethod(writer, "PatternWhiteSpace", FilterPropertyRanges(propList, "Pattern_White_Space"));
        WriteStringArrayMethod(
            writer,
            "IdentifierPropertyNames",
            new string[]
            {
                "ID_Start",
                "ID_Continue",
                "XID_Start",
                "XID_Continue",
                "Pattern_Syntax",
                "Pattern_White_Space",
            });
        WriteFooter(writer);
    }

    public static void WriteScriptTables(
        string outputPath,
        UnicodePropertyRangeRecord[] records,
        UnicodeScriptExtensionsRecord[] extensions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(extensions);

        int[] starts = new int[records.Length];
        int[] ends = new int[records.Length];
        int[] values = new int[records.Length];
        int index = 0;
        while (index < records.Length)
        {
            starts[index] = records[index].StartCodePoint;
            ends[index] = records[index].EndCodePoint;
            values[index] = GetScriptValue(records[index].PropertyName);
            index = index + 1;
        }

        SortRanges(starts, ends, values);

        int[] extensionStarts = new int[extensions.Length];
        int[] extensionEnds = new int[extensions.Length];
        int[] extensionOffsets = new int[extensions.Length];
        int[] extensionLengths = new int[extensions.Length];
        List<int> extensionValues = new List<int>();
        index = 0;
        while (index < extensions.Length)
        {
            extensionStarts[index] = extensions[index].StartCodePoint;
            extensionEnds[index] = extensions[index].EndCodePoint;
            extensionOffsets[index] = extensionValues.Count;
            int scriptIndex = 0;
            while (scriptIndex < extensions[index].Scripts.Length)
            {
                int value = GetScriptValue(extensions[index].Scripts[scriptIndex]);
                if (value != 0)
                {
                    extensionValues.Add(value);
                    extensionLengths[index] = extensionLengths[index] + 1;
                }

                scriptIndex = scriptIndex + 1;
            }

            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeScriptTables");
        WriteRangeValueMethod(writer, "Scripts", starts, ends, values);
        WriteStringArrayMethod(writer, "ScriptNames", ScriptNames());
        WriteIntArrayMethod(writer, "ScriptExtensionStarts", extensionStarts);
        WriteIntArrayMethod(writer, "ScriptExtensionEnds", extensionEnds);
        WriteIntArrayMethod(writer, "ScriptExtensionOffsets", extensionOffsets);
        WriteIntArrayMethod(writer, "ScriptExtensionLengths", extensionLengths);
        WriteIntArrayMethod(writer, "ScriptExtensionValues", extensionValues.ToArray());
        WriteFooter(writer);
    }

    public static void WriteBlockTables(string outputPath, UnicodePropertyRangeRecord[] records)
    {
        WriteMappedRangeTable(outputPath, "UnicodeBlockTables", "Blocks", records, GetBlockValue);
    }

    public static void WriteWidthTables(string outputPath, UnicodePropertyRangeRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeWidthTables");
        WriteMappedRangeMethod(writer, "EastAsianWidths", records, GetEastAsianWidthValue);
        WriteStringArrayMethod(writer, "EastAsianWidthNames", EastAsianWidthNames());
        WriteFooter(writer);
    }

    public static void WriteBidiTables(
        string outputPath,
        UnicodeDataRecord[] unicodeData,
        UnicodePropertyRangeRecord[] derivedBidiClasses,
        UnicodeBidiMirrorRecord[] mirrors,
        UnicodeBidiBracketRecord[] brackets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(unicodeData);
        ArgumentNullException.ThrowIfNull(derivedBidiClasses);
        ArgumentNullException.ThrowIfNull(mirrors);
        ArgumentNullException.ThrowIfNull(brackets);

        int[] starts;
        int[] ends;
        int[] bidiValues;
        int index = 0;

        if (derivedBidiClasses.Length > 0)
        {
            int[] scalarValues = new int[0x110000];
            while (index < derivedBidiClasses.Length)
            {
                int value = GetBidiClassValue(derivedBidiClasses[index].PropertyName);
                int codePoint = derivedBidiClasses[index].StartCodePoint;
                while (codePoint <= derivedBidiClasses[index].EndCodePoint)
                {
                    scalarValues[codePoint] = value;
                    codePoint = codePoint + 1;
                }

                index = index + 1;
            }

            List<int> compactStarts = new List<int>();
            List<int> compactEnds = new List<int>();
            List<int> compactValues = new List<int>();
            int rangeStart = 0;
            index = 1;
            while (index <= scalarValues.Length)
            {
                if (index == scalarValues.Length || scalarValues[index] != scalarValues[rangeStart])
                {
                    compactStarts.Add(rangeStart);
                    compactEnds.Add(index - 1);
                    compactValues.Add(scalarValues[rangeStart]);
                    rangeStart = index;
                }

                index = index + 1;
            }

            starts = compactStarts.ToArray();
            ends = compactEnds.ToArray();
            bidiValues = compactValues.ToArray();
        }
        else
        {
            starts = new int[unicodeData.Length];
            ends = new int[unicodeData.Length];
            bidiValues = new int[unicodeData.Length];
            while (index < unicodeData.Length)
            {
                starts[index] = unicodeData[index].CodePoint;
                ends[index] = unicodeData[index].CodePoint;
                bidiValues[index] = GetBidiClassValue(unicodeData[index].BidiClass);
                index = index + 1;
            }

            SortRanges(starts, ends, bidiValues);
        }

        int[] mirrorStarts = new int[mirrors.Length];
        int[] mirrorEnds = new int[mirrors.Length];
        int[] mirrorValues = new int[mirrors.Length];
        index = 0;

        while (index < mirrors.Length)
        {
            mirrorStarts[index] = mirrors[index].CodePoint;
            mirrorEnds[index] = mirrors[index].CodePoint;
            mirrorValues[index] = mirrors[index].MirrorCodePoint;
            index = index + 1;
        }

        List<int> mirroredStarts = new List<int>();
        List<int> mirroredEnds = new List<int>();
        index = 0;
        while (index < unicodeData.Length)
        {
            int endCodePoint = unicodeData[index].CodePoint;
            if (IsRangeFirst(unicodeData[index]) && index + 1 < unicodeData.Length && IsRangeLast(unicodeData[index + 1]))
            {
                endCodePoint = unicodeData[index + 1].CodePoint;
            }

            if (unicodeData[index].BidiMirrored)
            {
                mirroredStarts.Add(unicodeData[index].CodePoint);
                mirroredEnds.Add(endCodePoint);
            }

            if (endCodePoint != unicodeData[index].CodePoint)
            {
                index = index + 1;
            }

            index = index + 1;
        }

        List<string> bracketTypeNames = new List<string>();
        Dictionary<string, int> bracketTypeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] bracketStarts = new int[brackets.Length];
        int[] bracketEnds = new int[brackets.Length];
        int[] bracketValues = new int[brackets.Length];
        int[] bracketTypes = new int[brackets.Length];
        index = 0;

        while (index < brackets.Length)
        {
            bracketStarts[index] = brackets[index].CodePoint;
            bracketEnds[index] = brackets[index].CodePoint;
            bracketValues[index] = brackets[index].PairedBracketCodePoint;
            bracketTypes[index] = GetValueIndex(brackets[index].BracketType, bracketTypeNames, bracketTypeIndexes);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeBidiTables");
        WriteRangeValueMethod(writer, "BidiClasses", starts, ends, bidiValues);
        WriteStringArrayMethod(writer, "BidiClassNames", BidiClassNames());
        WriteRangeMethod(writer, "BidiMirrored", mirroredStarts.ToArray(), mirroredEnds.ToArray());
        WriteRangeValueMethod(writer, "BidiMirrors", mirrorStarts, mirrorEnds, mirrorValues);
        WriteRangeValueMethod(writer, "BidiBracketPairs", bracketStarts, bracketEnds, bracketValues);
        WriteRangeValueMethod(writer, "BidiBracketTypes", bracketStarts, bracketEnds, bracketTypes);
        WriteStringArrayMethod(writer, "BidiBracketTypeNames", bracketTypeNames.ToArray());
        WriteFooter(writer);
    }

    public static void WriteNumericTables(string outputPath, UnicodeDataRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        List<int> codePoints = new List<int>();
        List<long> numerators = new List<long>();
        List<long> denominators = new List<long>();
        List<int> numericTypes = new List<int>();
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].NumericValue.HasValue)
            {
                codePoints.Add(records[index].CodePoint);
                numerators.Add(records[index].NumericValue.Numerator);
                denominators.Add(records[index].NumericValue.Denominator);
                int numericType = 3;
                if (records[index].DecimalDigitValue >= 0)
                {
                    numericType = 1;
                }
                else if (records[index].DigitValue >= 0)
                {
                    numericType = 2;
                }

                numericTypes.Add(numericType);
            }

            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeNumericTables");
        WriteIntArrayMethod(writer, "NumericCodePoints", codePoints.ToArray());
        WriteIntArrayMethod(writer, "NumericTypes", numericTypes.ToArray());
        WriteLongArrayMethod(writer, "NumericNumerators", numerators.ToArray());
        WriteLongArrayMethod(writer, "NumericDenominators", denominators.ToArray());
        WriteFooter(writer);
    }

    public static void WriteNameTables(string outputPath, UnicodeDataRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        List<int> codePoints = new List<int>();
        List<string> names = new List<string>();
        List<int> algorithmicStarts = new List<int>();
        List<int> algorithmicEnds = new List<int>();
        List<string> algorithmicPrefixes = new List<string>();
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].Name.Length > 0 && !records[index].Name.StartsWith("<", StringComparison.Ordinal))
            {
                codePoints.Add(records[index].CodePoint);
                names.Add(records[index].Name);
            }

            index = index + 1;
        }

        index = 0;
        while (index + 1 < records.Length)
        {
            if (IsRangeFirst(records[index]) && IsRangeLast(records[index + 1]))
            {
                string prefix = GetAlgorithmicNamePrefix(records[index].Name);
                if (prefix.Length > 0)
                {
                    algorithmicStarts.Add(records[index].CodePoint);
                    algorithmicEnds.Add(records[index + 1].CodePoint);
                    algorithmicPrefixes.Add(prefix);
                }

                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeNameTables");
        WriteIntArrayMethod(writer, "NameCodePoints", codePoints.ToArray());
        WriteStringArrayMethod(writer, "Names", names.ToArray());
        WriteIntArrayMethod(writer, "AlgorithmicNameStarts", algorithmicStarts.ToArray());
        WriteIntArrayMethod(writer, "AlgorithmicNameEnds", algorithmicEnds.ToArray());
        WriteStringArrayMethod(writer, "AlgorithmicNamePrefixes", algorithmicPrefixes.ToArray());
        WriteFooter(writer);
    }

    public static void WriteNamedSequenceTables(string outputPath, UnicodeNamedSequenceRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        string[] names = new string[records.Length];
        int[] offsets = new int[records.Length];
        int[] lengths = new int[records.Length];
        List<int> codePoints = new List<int>();
        int index = 0;
        while (index < records.Length)
        {
            names[index] = records[index].Name;
            offsets[index] = codePoints.Count;
            lengths[index] = records[index].CodePoints.Length;
            AddCodePoints(codePoints, records[index].CodePoints);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeNamedSequenceTables");
        WriteStringArrayMethod(writer, "Names", names);
        WriteIntArrayMethod(writer, "CodePoints", codePoints.ToArray());
        WriteIntArrayMethod(writer, "Offsets", offsets);
        WriteIntArrayMethod(writer, "Lengths", lengths);
        WriteFooter(writer);
    }

    public static void WriteAliasTables(
        string outputPath,
        UnicodeNameAliasRecord[] nameAliases,
        UnicodePropertyAliasRecord[] propertyAliases,
        UnicodePropertyValueAliasRecord[] propertyValueAliases)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(nameAliases);
        ArgumentNullException.ThrowIfNull(propertyAliases);
        ArgumentNullException.ThrowIfNull(propertyValueAliases);

        int[] nameAliasCodePoints = new int[nameAliases.Length];
        string[] nameAliasNames = new string[nameAliases.Length];
        int[] nameAliasTypes = new int[nameAliases.Length];
        int index = 0;
        while (index < nameAliases.Length)
        {
            nameAliasCodePoints[index] = nameAliases[index].CodePoint;
            nameAliasNames[index] = nameAliases[index].Name;
            nameAliasTypes[index] = GetNameAliasTypeValue(nameAliases[index].AliasType);
            index = index + 1;
        }

        string[] propertyShortNames = new string[propertyAliases.Length];
        string[] propertyLongNames = new string[propertyAliases.Length];
        int[] propertyAliasOffsets = new int[propertyAliases.Length];
        int[] propertyAliasLengths = new int[propertyAliases.Length];
        List<string> propertyAliasNames = new List<string>();
        index = 0;
        while (index < propertyAliases.Length)
        {
            propertyShortNames[index] = propertyAliases[index].ShortName;
            propertyLongNames[index] = propertyAliases[index].LongName;
            propertyAliasOffsets[index] = propertyAliasNames.Count;
            propertyAliasLengths[index] = propertyAliases[index].Aliases.Length;
            AddStrings(propertyAliasNames, propertyAliases[index].Aliases);
            index = index + 1;
        }

        string[] valuePropertyNames = new string[propertyValueAliases.Length];
        string[] valueShortNames = new string[propertyValueAliases.Length];
        string[] valueLongNames = new string[propertyValueAliases.Length];
        int[] valueAliasOffsets = new int[propertyValueAliases.Length];
        int[] valueAliasLengths = new int[propertyValueAliases.Length];
        List<string> valueAliasNames = new List<string>();
        index = 0;
        while (index < propertyValueAliases.Length)
        {
            UnicodePropertyValueAliasRecord record = propertyValueAliases[index];
            valuePropertyNames[index] = record.PropertyName;
            valueShortNames[index] = record.ValueName;
            valueLongNames[index] = GetPropertyValueLongName(record);
            valueAliasOffsets[index] = valueAliasNames.Count;
            valueAliasLengths[index] = record.Aliases.Length;
            AddStrings(valueAliasNames, record.Aliases);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeAliasTables");
        WriteIntArrayMethod(writer, "NameAliasCodePoints", nameAliasCodePoints);
        WriteStringArrayMethod(writer, "NameAliasNames", nameAliasNames);
        WriteIntArrayMethod(writer, "NameAliasTypes", nameAliasTypes);
        WriteStringArrayMethod(writer, "PropertyShortNames", propertyShortNames);
        WriteStringArrayMethod(writer, "PropertyLongNames", propertyLongNames);
        WriteIntArrayMethod(writer, "PropertyAliasOffsets", propertyAliasOffsets);
        WriteIntArrayMethod(writer, "PropertyAliasLengths", propertyAliasLengths);
        WriteStringArrayMethod(writer, "PropertyAliasNames", propertyAliasNames.ToArray());
        WriteStringArrayMethod(writer, "ValuePropertyNames", valuePropertyNames);
        WriteStringArrayMethod(writer, "ValueShortNames", valueShortNames);
        WriteStringArrayMethod(writer, "ValueLongNames", valueLongNames);
        WriteIntArrayMethod(writer, "ValueAliasOffsets", valueAliasOffsets);
        WriteIntArrayMethod(writer, "ValueAliasLengths", valueAliasLengths);
        WriteStringArrayMethod(writer, "ValueAliasNames", valueAliasNames.ToArray());
        WriteFooter(writer);
    }

    public static void WriteNormalizationTables(
        string outputPath,
        UnicodeDecompositionRecord[] decompositions,
        UnicodeCompositionExclusionRecord[] compositionExclusions,
        UnicodeNormalizationQuickCheckRecord[] quickChecks)
    {
        WriteNormalizationTablesWithCaseFold(
            outputPath,
            decompositions,
            compositionExclusions,
            quickChecks,
            Array.Empty<UnicodeNormalizationMappingRecord>());
    }

    public static void WriteNormalizationTablesWithCaseFold(
        string outputPath,
        UnicodeDecompositionRecord[] decompositions,
        UnicodeCompositionExclusionRecord[] compositionExclusions,
        UnicodeNormalizationQuickCheckRecord[] quickChecks,
        UnicodeNormalizationMappingRecord[] nfkcCaseFoldMappings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(decompositions);
        ArgumentNullException.ThrowIfNull(compositionExclusions);
        ArgumentNullException.ThrowIfNull(quickChecks);
        ArgumentNullException.ThrowIfNull(nfkcCaseFoldMappings);

        List<string> decompositionTypes = new List<string>();
        Dictionary<string, int> decompositionTypeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] decompositionCodePoints = new int[decompositions.Length];
        int[] decompositionTypeValues = new int[decompositions.Length];
        int[] decompositionOffsets = new int[decompositions.Length];
        int[] decompositionLengths = new int[decompositions.Length];
        List<int> decompositionMapping = new List<int>();
        int index = 0;

        while (index < decompositions.Length)
        {
            decompositionCodePoints[index] = decompositions[index].CodePoint;
            decompositionTypeValues[index] = GetValueIndex(decompositions[index].DecompositionType, decompositionTypes, decompositionTypeIndexes);
            decompositionOffsets[index] = decompositionMapping.Count;
            decompositionLengths[index] = decompositions[index].Mapping.Length;
            AddCodePoints(decompositionMapping, decompositions[index].Mapping);
            index = index + 1;
        }

        int[] exclusionStarts = new int[compositionExclusions.Length];
        int[] exclusionEnds = new int[compositionExclusions.Length];
        int[] exclusionValues = new int[compositionExclusions.Length];
        index = 0;

        while (index < compositionExclusions.Length)
        {
            exclusionStarts[index] = compositionExclusions[index].StartCodePoint;
            exclusionEnds[index] = compositionExclusions[index].EndCodePoint;
            exclusionValues[index] = 1;
            index = index + 1;
        }

        List<string> quickCheckForms = new List<string>();
        Dictionary<string, int> quickCheckFormIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        List<string> quickCheckValues = new List<string>();
        Dictionary<string, int> quickCheckValueIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] quickCheckStarts = new int[quickChecks.Length];
        int[] quickCheckEnds = new int[quickChecks.Length];
        int[] quickCheckFormValues = new int[quickChecks.Length];
        int[] quickCheckResultValues = new int[quickChecks.Length];
        index = 0;

        while (index < quickChecks.Length)
        {
            quickCheckStarts[index] = quickChecks[index].StartCodePoint;
            quickCheckEnds[index] = quickChecks[index].EndCodePoint;
            quickCheckFormValues[index] = GetValueIndex(quickChecks[index].Form, quickCheckForms, quickCheckFormIndexes);
            quickCheckResultValues[index] = GetValueIndex(quickChecks[index].QuickCheckValue, quickCheckValues, quickCheckValueIndexes);
            index = index + 1;
        }

        int[] nfkcCaseFoldStarts = new int[nfkcCaseFoldMappings.Length];
        int[] nfkcCaseFoldEnds = new int[nfkcCaseFoldMappings.Length];
        int[] nfkcCaseFoldOffsets = new int[nfkcCaseFoldMappings.Length];
        int[] nfkcCaseFoldLengths = new int[nfkcCaseFoldMappings.Length];
        List<int> nfkcCaseFoldCodePoints = new List<int>();
        index = 0;
        while (index < nfkcCaseFoldMappings.Length)
        {
            nfkcCaseFoldStarts[index] = nfkcCaseFoldMappings[index].StartCodePoint;
            nfkcCaseFoldEnds[index] = nfkcCaseFoldMappings[index].EndCodePoint;
            nfkcCaseFoldOffsets[index] = nfkcCaseFoldCodePoints.Count;
            nfkcCaseFoldLengths[index] = nfkcCaseFoldMappings[index].Mapping.Length;
            AddCodePoints(nfkcCaseFoldCodePoints, nfkcCaseFoldMappings[index].Mapping);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeNormalizationTables");
        WriteIntArrayMethod(writer, "DecompositionCodePoints", decompositionCodePoints);
        WriteIntArrayMethod(writer, "DecompositionTypeIndexes", decompositionTypeValues);
        WriteStringArrayMethod(writer, "DecompositionTypeNames", decompositionTypes.ToArray());
        WriteIntArrayMethod(writer, "DecompositionMappingOffsets", decompositionOffsets);
        WriteIntArrayMethod(writer, "DecompositionMappingLengths", decompositionLengths);
        WriteIntArrayMethod(writer, "DecompositionMappingCodePoints", decompositionMapping.ToArray());
        WriteRangeValueMethod(writer, "CompositionExclusions", exclusionStarts, exclusionEnds, exclusionValues);
        WriteIntArrayMethod(writer, "QuickCheckStarts", quickCheckStarts);
        WriteIntArrayMethod(writer, "QuickCheckEnds", quickCheckEnds);
        WriteIntArrayMethod(writer, "QuickCheckFormIndexes", quickCheckFormValues);
        WriteIntArrayMethod(writer, "QuickCheckValueIndexes", quickCheckResultValues);
        WriteStringArrayMethod(writer, "QuickCheckFormNames", quickCheckForms.ToArray());
        WriteStringArrayMethod(writer, "QuickCheckValueNames", quickCheckValues.ToArray());
        WriteIntArrayMethod(writer, "NfkcCaseFoldStarts", nfkcCaseFoldStarts);
        WriteIntArrayMethod(writer, "NfkcCaseFoldEnds", nfkcCaseFoldEnds);
        WriteIntArrayMethod(writer, "NfkcCaseFoldOffsets", nfkcCaseFoldOffsets);
        WriteIntArrayMethod(writer, "NfkcCaseFoldLengths", nfkcCaseFoldLengths);
        WriteIntArrayMethod(writer, "NfkcCaseFoldCodePoints", nfkcCaseFoldCodePoints.ToArray());
        WriteFooter(writer);
    }

    public static void WriteCaseTables(
        string outputPath,
        UnicodeDataRecord[] unicodeData,
        UnicodeSpecialCasingRecord[] specialCasing,
        UnicodeCaseFoldingRecord[] caseFolding,
        UnicodePropertyRangeRecord[] derivedCoreProperties,
        UnicodePropertyRangeRecord[] propList)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(unicodeData);
        ArgumentNullException.ThrowIfNull(specialCasing);
        ArgumentNullException.ThrowIfNull(caseFolding);
        ArgumentNullException.ThrowIfNull(derivedCoreProperties);
        ArgumentNullException.ThrowIfNull(propList);

        List<int> simpleUppercaseCodePoints = new List<int>();
        List<int> simpleUppercaseMappings = new List<int>();
        List<int> simpleLowercaseCodePoints = new List<int>();
        List<int> simpleLowercaseMappings = new List<int>();
        List<int> simpleTitlecaseCodePoints = new List<int>();
        List<int> simpleTitlecaseMappings = new List<int>();
        int index = 0;

        while (index < unicodeData.Length)
        {
            if (unicodeData[index].SimpleUppercaseMapping >= 0)
            {
                simpleUppercaseCodePoints.Add(unicodeData[index].CodePoint);
                simpleUppercaseMappings.Add(unicodeData[index].SimpleUppercaseMapping);
            }

            if (unicodeData[index].SimpleLowercaseMapping >= 0)
            {
                simpleLowercaseCodePoints.Add(unicodeData[index].CodePoint);
                simpleLowercaseMappings.Add(unicodeData[index].SimpleLowercaseMapping);
            }

            if (unicodeData[index].SimpleTitlecaseMapping >= 0)
            {
                simpleTitlecaseCodePoints.Add(unicodeData[index].CodePoint);
                simpleTitlecaseMappings.Add(unicodeData[index].SimpleTitlecaseMapping);
            }

            index = index + 1;
        }

        int[] specialCaseCodePoints = new int[specialCasing.Length];
        int[] specialLowercaseOffsets = new int[specialCasing.Length];
        int[] specialLowercaseLengths = new int[specialCasing.Length];
        int[] specialTitlecaseOffsets = new int[specialCasing.Length];
        int[] specialTitlecaseLengths = new int[specialCasing.Length];
        int[] specialUppercaseOffsets = new int[specialCasing.Length];
        int[] specialUppercaseLengths = new int[specialCasing.Length];
        int[] specialConditionOffsets = new int[specialCasing.Length];
        int[] specialConditionLengths = new int[specialCasing.Length];
        List<int> specialLowercaseMappings = new List<int>();
        List<int> specialTitlecaseMappings = new List<int>();
        List<int> specialUppercaseMappings = new List<int>();
        List<int> specialConditionIndexes = new List<int>();
        List<string> specialConditionNames = new List<string>();
        Dictionary<string, int> specialConditionNameIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        index = 0;

        while (index < specialCasing.Length)
        {
            specialCaseCodePoints[index] = specialCasing[index].CodePoint;

            specialLowercaseOffsets[index] = specialLowercaseMappings.Count;
            specialLowercaseLengths[index] = specialCasing[index].LowercaseMapping.Length;
            AddCodePoints(specialLowercaseMappings, specialCasing[index].LowercaseMapping);

            specialTitlecaseOffsets[index] = specialTitlecaseMappings.Count;
            specialTitlecaseLengths[index] = specialCasing[index].TitlecaseMapping.Length;
            AddCodePoints(specialTitlecaseMappings, specialCasing[index].TitlecaseMapping);

            specialUppercaseOffsets[index] = specialUppercaseMappings.Count;
            specialUppercaseLengths[index] = specialCasing[index].UppercaseMapping.Length;
            AddCodePoints(specialUppercaseMappings, specialCasing[index].UppercaseMapping);

            specialConditionOffsets[index] = specialConditionIndexes.Count;
            specialConditionLengths[index] = specialCasing[index].Conditions.Length;
            AddStringIndexes(specialConditionIndexes, specialCasing[index].Conditions, specialConditionNames, specialConditionNameIndexes);

            index = index + 1;
        }

        int[] caseFoldCodePoints = new int[caseFolding.Length];
        int[] caseFoldStatusIndexes = new int[caseFolding.Length];
        int[] caseFoldMappingOffsets = new int[caseFolding.Length];
        int[] caseFoldMappingLengths = new int[caseFolding.Length];
        List<string> caseFoldStatusNames = new List<string>();
        Dictionary<string, int> caseFoldStatusNameIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        List<int> caseFoldMappings = new List<int>();
        index = 0;

        while (index < caseFolding.Length)
        {
            caseFoldCodePoints[index] = caseFolding[index].CodePoint;
            caseFoldStatusIndexes[index] = GetValueIndex(caseFolding[index].Status, caseFoldStatusNames, caseFoldStatusNameIndexes);
            caseFoldMappingOffsets[index] = caseFoldMappings.Count;
            caseFoldMappingLengths[index] = caseFolding[index].Mapping.Length;
            AddCodePoints(caseFoldMappings, caseFolding[index].Mapping);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeCaseTables");
        WriteIntArrayMethod(writer, "SimpleUppercaseCodePoints", simpleUppercaseCodePoints.ToArray());
        WriteIntArrayMethod(writer, "SimpleUppercaseMappings", simpleUppercaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SimpleLowercaseCodePoints", simpleLowercaseCodePoints.ToArray());
        WriteIntArrayMethod(writer, "SimpleLowercaseMappings", simpleLowercaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SimpleTitlecaseCodePoints", simpleTitlecaseCodePoints.ToArray());
        WriteIntArrayMethod(writer, "SimpleTitlecaseMappings", simpleTitlecaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SpecialCaseCodePoints", specialCaseCodePoints);
        WriteIntArrayMethod(writer, "SpecialLowercaseOffsets", specialLowercaseOffsets);
        WriteIntArrayMethod(writer, "SpecialLowercaseLengths", specialLowercaseLengths);
        WriteIntArrayMethod(writer, "SpecialLowercaseMappings", specialLowercaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SpecialTitlecaseOffsets", specialTitlecaseOffsets);
        WriteIntArrayMethod(writer, "SpecialTitlecaseLengths", specialTitlecaseLengths);
        WriteIntArrayMethod(writer, "SpecialTitlecaseMappings", specialTitlecaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SpecialUppercaseOffsets", specialUppercaseOffsets);
        WriteIntArrayMethod(writer, "SpecialUppercaseLengths", specialUppercaseLengths);
        WriteIntArrayMethod(writer, "SpecialUppercaseMappings", specialUppercaseMappings.ToArray());
        WriteIntArrayMethod(writer, "SpecialConditionOffsets", specialConditionOffsets);
        WriteIntArrayMethod(writer, "SpecialConditionLengths", specialConditionLengths);
        WriteIntArrayMethod(writer, "SpecialConditionIndexes", specialConditionIndexes.ToArray());
        WriteStringArrayMethod(writer, "SpecialConditionNames", specialConditionNames.ToArray());
        WriteIntArrayMethod(writer, "CaseFoldCodePoints", caseFoldCodePoints);
        WriteIntArrayMethod(writer, "CaseFoldStatusIndexes", caseFoldStatusIndexes);
        WriteStringArrayMethod(writer, "CaseFoldStatusNames", caseFoldStatusNames.ToArray());
        WriteIntArrayMethod(writer, "CaseFoldMappingOffsets", caseFoldMappingOffsets);
        WriteIntArrayMethod(writer, "CaseFoldMappingLengths", caseFoldMappingLengths);
        WriteIntArrayMethod(writer, "CaseFoldMappings", caseFoldMappings.ToArray());
        WriteRangeMethod(writer, "Cased", FilterPropertyRanges(derivedCoreProperties, "Cased"));
        WriteRangeMethod(writer, "CaseIgnorable", FilterPropertyRanges(derivedCoreProperties, "Case_Ignorable"));
        WriteRangeMethod(writer, "SoftDotted", FilterPropertyRanges(propList, "Soft_Dotted"));
        WriteFooter(writer);
    }

    public static void WriteBoundaryTables(
        string outputPath,
        UnicodePropertyRangeRecord[] graphemeBreaks,
        UnicodePropertyRangeRecord[] wordBreaks,
        UnicodePropertyRangeRecord[] sentenceBreaks,
        UnicodePropertyRangeRecord[] lineBreaks,
        UnicodePropertyRangeRecord[] indicConjunctBreaks,
        UnicodePropertyRangeRecord[] hangulSyllableTypes,
        UnicodePropertyRangeRecord[] emojiBoundaryProperties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(graphemeBreaks);
        ArgumentNullException.ThrowIfNull(wordBreaks);
        ArgumentNullException.ThrowIfNull(sentenceBreaks);
        ArgumentNullException.ThrowIfNull(lineBreaks);
        ArgumentNullException.ThrowIfNull(indicConjunctBreaks);
        ArgumentNullException.ThrowIfNull(hangulSyllableTypes);
        ArgumentNullException.ThrowIfNull(emojiBoundaryProperties);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeBoundaryTables");
        WriteNamedRangeMethods(writer, "GraphemeBreaks", "GraphemeBreakNames", graphemeBreaks);
        WriteNamedRangeMethods(writer, "WordBreaks", "WordBreakNames", wordBreaks);
        WriteNamedRangeMethods(writer, "SentenceBreaks", "SentenceBreakNames", sentenceBreaks);
        WriteNamedRangeMethods(writer, "LineBreaks", "LineBreakNames", lineBreaks);
        WriteNamedRangeMethods(writer, "IndicConjunctBreaks", "IndicConjunctBreakNames", indicConjunctBreaks);
        WriteNamedRangeMethods(writer, "HangulSyllableTypes", "HangulSyllableTypeNames", hangulSyllableTypes);
        WriteNamedRangeMethods(writer, "EmojiBoundaryProperties", "EmojiBoundaryPropertyNames", emojiBoundaryProperties);
        WriteFooter(writer);
    }

    public static void WriteSecurityTables(
        string outputPath,
        UnicodeConfusableRecord[] confusables,
        UnicodeIdentifierStatusRecord[] identifierStatuses,
        UnicodeIdentifierTypeRecord[] identifierTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(confusables);
        ArgumentNullException.ThrowIfNull(identifierStatuses);
        ArgumentNullException.ThrowIfNull(identifierTypes);

        List<string> statusNames = new List<string>();
        Dictionary<string, int> statusIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] statusStarts = new int[identifierStatuses.Length];
        int[] statusEnds = new int[identifierStatuses.Length];
        int[] statusValues = new int[identifierStatuses.Length];
        List<int> allowedStarts = new List<int>();
        List<int> allowedEnds = new List<int>();
        int index = 0;

        while (index < identifierStatuses.Length)
        {
            statusStarts[index] = identifierStatuses[index].StartCodePoint;
            statusEnds[index] = identifierStatuses[index].EndCodePoint;
            statusValues[index] = GetValueIndex(identifierStatuses[index].Status, statusNames, statusIndexes);

            if (identifierStatuses[index].Status == "Allowed")
            {
                allowedStarts.Add(identifierStatuses[index].StartCodePoint);
                allowedEnds.Add(identifierStatuses[index].EndCodePoint);
            }

            index = index + 1;
        }

        int[] confusableSourceOffsets = new int[confusables.Length];
        int[] confusableSourceLengths = new int[confusables.Length];
        int[] confusableTargetOffsets = new int[confusables.Length];
        int[] confusableTargetLengths = new int[confusables.Length];
        int[] confusableTypeIndexes = new int[confusables.Length];
        List<int> confusableSourceCodePoints = new List<int>();
        List<int> confusableTargetCodePoints = new List<int>();
        List<string> confusableTypeNames = new List<string>();
        Dictionary<string, int> confusableTypeNameIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        index = 0;

        while (index < confusables.Length)
        {
            confusableSourceOffsets[index] = confusableSourceCodePoints.Count;
            confusableSourceLengths[index] = confusables[index].Source.Length;
            AddCodePoints(confusableSourceCodePoints, confusables[index].Source);
            confusableTargetOffsets[index] = confusableTargetCodePoints.Count;
            confusableTargetLengths[index] = confusables[index].Target.Length;
            AddCodePoints(confusableTargetCodePoints, confusables[index].Target);
            confusableTypeIndexes[index] = GetValueIndex(confusables[index].Type, confusableTypeNames, confusableTypeNameIndexes);
            index = index + 1;
        }

        int[] typeStarts = new int[identifierTypes.Length];
        int[] typeEnds = new int[identifierTypes.Length];
        int[] typeOffsets = new int[identifierTypes.Length];
        int[] typeLengths = new int[identifierTypes.Length];
        List<int> typeIndexes = new List<int>();
        List<string> typeNames = new List<string>();
        Dictionary<string, int> typeNameIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        index = 0;

        while (index < identifierTypes.Length)
        {
            typeStarts[index] = identifierTypes[index].StartCodePoint;
            typeEnds[index] = identifierTypes[index].EndCodePoint;
            typeOffsets[index] = typeIndexes.Count;
            typeLengths[index] = identifierTypes[index].Types.Length;
            AddStringIndexes(typeIndexes, identifierTypes[index].Types, typeNames, typeNameIndexes);
            index = index + 1;
        }

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, "UnicodeSecurityTables");
        WriteRangeValueMethod(writer, "IdentifierStatuses", statusStarts, statusEnds, statusValues);
        WriteStringArrayMethod(writer, "IdentifierStatusNames", statusNames.ToArray());
        WriteRangeMethod(writer, "AllowedIdentifierStatus", allowedStarts.ToArray(), allowedEnds.ToArray());
        WriteIntArrayMethod(writer, "ConfusableSourceOffsets", confusableSourceOffsets);
        WriteIntArrayMethod(writer, "ConfusableSourceLengths", confusableSourceLengths);
        WriteIntArrayMethod(writer, "ConfusableSourceCodePoints", confusableSourceCodePoints.ToArray());
        WriteIntArrayMethod(writer, "ConfusableTargetOffsets", confusableTargetOffsets);
        WriteIntArrayMethod(writer, "ConfusableTargetLengths", confusableTargetLengths);
        WriteIntArrayMethod(writer, "ConfusableTargetCodePoints", confusableTargetCodePoints.ToArray());
        WriteIntArrayMethod(writer, "ConfusableTypeIndexes", confusableTypeIndexes);
        WriteStringArrayMethod(writer, "ConfusableTypeNames", confusableTypeNames.ToArray());
        WriteIntArrayMethod(writer, "IdentifierTypeStarts", typeStarts);
        WriteIntArrayMethod(writer, "IdentifierTypeEnds", typeEnds);
        WriteIntArrayMethod(writer, "IdentifierTypeOffsets", typeOffsets);
        WriteIntArrayMethod(writer, "IdentifierTypeLengths", typeLengths);
        WriteIntArrayMethod(writer, "IdentifierTypeIndexes", typeIndexes.ToArray());
        WriteStringArrayMethod(writer, "IdentifierTypeNames", typeNames.ToArray());
        WriteFooter(writer);
    }

    static int[] CreatePdfDocEncodingToUnicode()
    {
        int[] values = new int[256];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = index;
            index = index + 1;
        }

        int[] bytes = PdfDocEncodingSpecialBytes();
        int[] codePoints = PdfDocEncodingSpecialCodePoints();
        index = 0;
        while (index < bytes.Length)
        {
            values[bytes[index]] = codePoints[index];
            index = index + 1;
        }

        values[0x7F] = 0xFFFD;
        values[0x9F] = 0xFFFD;
        values[0xAD] = 0xFFFD;
        return values;
    }

    static int[] PdfDocEncodingSpecialCodePoints()
    {
        return new int[]
        {
            0x0131,
            0x0141,
            0x0142,
            0x0152,
            0x0153,
            0x0160,
            0x0161,
            0x0178,
            0x017D,
            0x017E,
            0x0192,
            0x02C6,
            0x02C7,
            0x02D8,
            0x02D9,
            0x02DA,
            0x02DB,
            0x02DC,
            0x02DD,
            0x2013,
            0x2014,
            0x2018,
            0x2019,
            0x201A,
            0x201C,
            0x201D,
            0x201E,
            0x2020,
            0x2021,
            0x2022,
            0x2026,
            0x2030,
            0x2039,
            0x203A,
            0x2044,
            0x20AC,
            0x2122,
            0x2212,
            0xFB01,
            0xFB02,
        };
    }

    static int[] PdfDocEncodingSpecialBytes()
    {
        return new int[]
        {
            0x9A,
            0x95,
            0x9B,
            0x96,
            0x9C,
            0x97,
            0x9D,
            0x98,
            0x99,
            0x9E,
            0x86,
            0x1A,
            0x19,
            0x18,
            0x1B,
            0x1E,
            0x1D,
            0x1F,
            0x1C,
            0x85,
            0x84,
            0x8F,
            0x90,
            0x91,
            0x8D,
            0x8E,
            0x8C,
            0x81,
            0x82,
            0x80,
            0x83,
            0x8B,
            0x88,
            0x89,
            0x87,
            0xA0,
            0x92,
            0x8A,
            0x93,
            0x94,
        };
    }

    static UnicodePropertyRangeRecord[] CombinePropertyRanges(UnicodePropertyRangeRecord[] first, UnicodePropertyRangeRecord[] second)
    {
        UnicodePropertyRangeRecord[] combined = new UnicodePropertyRangeRecord[first.Length + second.Length];
        int index = 0;

        while (index < first.Length)
        {
            combined[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;

        while (secondIndex < second.Length)
        {
            combined[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return combined;
    }

    static UnicodePropertyRangeRecord[] FilterPropertyRanges(UnicodePropertyRangeRecord[] records, string propertyName)
    {
        List<UnicodePropertyRangeRecord> filtered = new List<UnicodePropertyRangeRecord>();
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].PropertyName == propertyName)
            {
                filtered.Add(records[index]);
            }

            index = index + 1;
        }

        return filtered.ToArray();
    }

    static bool IsRangeFirst(UnicodeDataRecord record)
    {
        return record.Name.EndsWith(", First>", StringComparison.Ordinal);
    }

    static bool IsRangeLast(UnicodeDataRecord record)
    {
        return record.Name.EndsWith(", Last>", StringComparison.Ordinal);
    }

    static int GetGeneralCategoryValue(string name)
    {
        string[] names = GeneralCategoryNames();
        int index = Array.IndexOf(names, name);
        return index < 0 ? 0 : index;
    }

    static string[] GeneralCategoryNames()
    {
        return new string[]
        {
            "Cn", "Lu", "Ll", "Lt", "Lm", "Lo", "Mn", "Mc", "Me", "Nd",
            "Nl", "No", "Pc", "Pd", "Ps", "Pe", "Pi", "Pf", "Po", "Sm",
            "Sc", "Sk", "So", "Zs", "Zl", "Zp", "Cc", "Cf", "Cs", "Co",
        };
    }

    static int GetBinaryPropertyValue(string name)
    {
        string[] names = BinaryPropertyNames();
        return Array.IndexOf(names, name);
    }

    static string[] BinaryPropertyNames()
    {
        return new string[]
        {
            "White_Space",
            "Default_Ignorable_Code_Point",
            "Uppercase",
            "Lowercase",
            "Alphabetic",
            "Emoji",
            "Emoji_Presentation",
            "Extended_Pictographic",
            "Emoji_Component",
            "Emoji_Modifier",
            "Emoji_Modifier_Base",
            "Variation_Selector",
            "Noncharacter_Code_Point",
            "Pattern_White_Space",
            "Pattern_Syntax",
        };
    }

    static int GetScriptValue(string name)
    {
        string[] names = ScriptNames();
        int index = Array.IndexOf(names, name);
        if (index >= 0)
        {
            return index;
        }

        string[] codes = new string[]
        {
            "Zzzz", "Zyyy", "Zinh", "Latn", "Grek", "Cyrl", "Hebr", "Arab", "Deva", "Hira", "Kana", "Hani", "Hang",
            "Armn", "Beng", "Ethi", "Geor", "Gran", "Gujr", "Guru", "Knda", "Khmr", "Laoo", "Mlym", "Mymr", "Orya", "Sinh",
            "Taml", "Telu", "Thaa", "Thai", "Tibt",
        };
        index = Array.IndexOf(codes, name);
        return index < 0 ? 0 : index;
    }

    static string[] ScriptNames()
    {
        return new string[]
        {
            "Unknown", "Common", "Inherited", "Latin", "Greek", "Cyrillic", "Hebrew",
            "Arabic", "Devanagari", "Hiragana", "Katakana", "Han", "Hangul", "Armenian",
            "Bengali", "Ethiopic", "Georgian", "Grantha", "Gujarati", "Gurmukhi", "Kannada",
            "Khmer", "Lao", "Malayalam", "Myanmar", "Oriya", "Sinhala", "Tamil", "Telugu",
            "Thaana", "Thai", "Tibetan",
        };
    }

    static int GetBlockValue(string name)
    {
        string[] names = new string[]
        {
            "No_Block", "Basic Latin", "Latin-1 Supplement", "Combining Diacritical Marks",
            "Greek and Coptic", "Cyrillic", "Hebrew", "Arabic", "Devanagari", "Hiragana",
            "Katakana", "CJK Unified Ideographs", "Hangul Syllables", "Private Use Area", "Specials",
        };
        int index = Array.IndexOf(names, name);
        return index < 0 ? 0 : index;
    }

    static int GetEastAsianWidthValue(string name)
    {
        return Array.IndexOf(EastAsianWidthNames(), name);
    }

    static string[] EastAsianWidthNames()
    {
        return new string[] { "N", "A", "H", "F", "Na", "W" };
    }

    static int GetBidiClassValue(string name)
    {
        int index = Array.IndexOf(BidiClassNames(), name);
        if (index >= 0)
        {
            return index;
        }

        string[] longNames = new string[]
        {
            "Left_To_Right", "Right_To_Left", "Arabic_Letter", "European_Number",
            "European_Separator", "European_Terminator", "Arabic_Number", "Common_Separator",
            "Nonspacing_Mark", "Boundary_Neutral", "Paragraph_Separator", "Segment_Separator",
            "White_Space", "Other_Neutral",
        };
        index = Array.IndexOf(longNames, name);
        if (index >= 0)
        {
            return index;
        }

        if (name == "LRE" || name == "RLE" || name == "LRO" || name == "RLO" || name == "PDF" ||
            name == "LRI" || name == "RLI" || name == "FSI" || name == "PDI")
        {
            return 9;
        }

        return 0;
    }

    static string[] BidiClassNames()
    {
        return new string[] { "L", "R", "AL", "EN", "ES", "ET", "AN", "CS", "NSM", "BN", "B", "S", "WS", "ON" };
    }

    static void AddCodePoints(List<int> destination, int[] codePoints)
    {
        int index = 0;

        while (index < codePoints.Length)
        {
            destination.Add(codePoints[index]);
            index = index + 1;
        }
    }

    static void AddStrings(List<string> destination, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            destination.Add(values[index]);
            index = index + 1;
        }
    }

    static int GetNameAliasTypeValue(string name)
    {
        string[] names = new string[] { "correction", "control", "alternate", "figment", "abbreviation" };
        int index = Array.IndexOf(names, name);
        return index < 0 ? 2 : index;
    }

    static string GetPropertyValueLongName(UnicodePropertyValueAliasRecord record)
    {
        if (record.Aliases.Length == 0)
        {
            return record.ValueName;
        }

        if (record.PropertyName == "ccc" && record.Aliases.Length > 1)
        {
            return record.Aliases[1];
        }

        return record.Aliases[0];
    }

    static string GetAlgorithmicNamePrefix(string rangeName)
    {
        if (rangeName.StartsWith("<Hangul Syllable", StringComparison.Ordinal))
        {
            return "HANGUL SYLLABLE";
        }

        if (rangeName.StartsWith("<CJK Ideograph", StringComparison.Ordinal))
        {
            return "CJK UNIFIED IDEOGRAPH";
        }

        if (rangeName.StartsWith("<Tangut Ideograph", StringComparison.Ordinal))
        {
            return "TANGUT IDEOGRAPH";
        }

        return "";
    }

    static void AddStringIndexes(List<int> destination, string[] values, List<string> names, Dictionary<string, int> indexes)
    {
        int index = 0;

        while (index < values.Length)
        {
            destination.Add(GetValueIndex(values[index], names, indexes));
            index = index + 1;
        }
    }

    static void WriteNamedRangeTable(
        string outputPath,
        string className,
        string methodName,
        string namesMethodName,
        UnicodePropertyRangeRecord[] records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, className);
        WriteNamedRangeMethods(writer, methodName, namesMethodName, records);
        WriteFooter(writer);
    }

    static void WriteMappedRangeTable(
        string outputPath,
        string className,
        string methodName,
        UnicodePropertyRangeRecord[] records,
        Func<string, int> getValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(getValue);

        using StreamWriter writer = CreateWriter(outputPath);
        WriteHeader(writer, className);
        WriteMappedRangeMethod(writer, methodName, records, getValue);
        WriteFooter(writer);
    }

    static void WriteMappedRangeMethod(
        StreamWriter writer,
        string methodName,
        UnicodePropertyRangeRecord[] records,
        Func<string, int> getValue)
    {
        int[] starts = new int[records.Length];
        int[] ends = new int[records.Length];
        int[] values = new int[records.Length];
        int index = 0;
        while (index < records.Length)
        {
            starts[index] = records[index].StartCodePoint;
            ends[index] = records[index].EndCodePoint;
            int value = getValue(records[index].PropertyName);
            values[index] = value < 0 ? 0 : value;
            index = index + 1;
        }

        SortRanges(starts, ends, values);

        WriteRangeValueMethod(writer, methodName, starts, ends, values);
    }

    static void SortRanges(int[] starts, int[] ends, int[] values)
    {
        int index = 1;
        while (index < starts.Length)
        {
            int start = starts[index];
            int end = ends[index];
            int value = values[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && starts[insertionIndex] > start)
            {
                starts[insertionIndex + 1] = starts[insertionIndex];
                ends[insertionIndex + 1] = ends[insertionIndex];
                values[insertionIndex + 1] = values[insertionIndex];
                insertionIndex = insertionIndex - 1;
            }

            starts[insertionIndex + 1] = start;
            ends[insertionIndex + 1] = end;
            values[insertionIndex + 1] = value;
            index = index + 1;
        }
    }

    static void SortRangesByValueAndStart(int[] starts, int[] ends, int[] values)
    {
        int index = 1;
        while (index < starts.Length)
        {
            int start = starts[index];
            int end = ends[index];
            int value = values[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 &&
                (values[insertionIndex] > value ||
                (values[insertionIndex] == value && starts[insertionIndex] > start)))
            {
                starts[insertionIndex + 1] = starts[insertionIndex];
                ends[insertionIndex + 1] = ends[insertionIndex];
                values[insertionIndex + 1] = values[insertionIndex];
                insertionIndex = insertionIndex - 1;
            }

            starts[insertionIndex + 1] = start;
            ends[insertionIndex + 1] = end;
            values[insertionIndex + 1] = value;
            index = index + 1;
        }
    }

    static void WriteNamedRangeMethods(
        StreamWriter writer,
        string methodName,
        string namesMethodName,
        UnicodePropertyRangeRecord[] records)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(namesMethodName);
        ArgumentNullException.ThrowIfNull(records);

        List<string> names = new List<string>();
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        int[] starts = new int[records.Length];
        int[] ends = new int[records.Length];
        int[] values = new int[records.Length];
        int index = 0;

        while (index < records.Length)
        {
            starts[index] = records[index].StartCodePoint;
            ends[index] = records[index].EndCodePoint;
            values[index] = GetValueIndex(records[index].PropertyName, names, indexes);
            index = index + 1;
        }

        SortRanges(starts, ends, values);

        WriteRangeValueMethod(writer, methodName, starts, ends, values);
        WriteStringArrayMethod(writer, namesMethodName, names.ToArray());
    }

    static int GetValueIndex(string value, List<string> names, Dictionary<string, int> indexes)
    {
        if (indexes.TryGetValue(value, out int existingIndex))
        {
            return existingIndex;
        }

        int index = names.Count;
        names.Add(value);
        indexes.Add(value, index);
        return index;
    }

    static StreamWriter CreateWriter(string outputPath)
    {
        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
    }

    static void WriteHeader(StreamWriter writer, string className)
    {
        writer.WriteLine("namespace unicode;");
        writer.WriteLine();
        writer.WriteLine("internal static class " + className);
        writer.WriteLine("{");
        writer.WriteLine("    internal const string UnicodeVersion = \"" + UnicodeDataFiles.UnicodeVersion + "\";");
    }

    static void WriteFooter(StreamWriter writer)
    {
        writer.WriteLine("}");
    }

    static void WriteRangeMethod(StreamWriter writer, string methodName, UnicodePropertyRangeRecord[] records)
    {
        int[] starts = new int[records.Length];
        int[] ends = new int[records.Length];
        int index = 0;
        while (index < records.Length)
        {
            starts[index] = records[index].StartCodePoint;
            ends[index] = records[index].EndCodePoint;
            index = index + 1;
        }

        writer.WriteLine();
        writer.WriteLine("    internal static readonly UnicodeRange[] " + methodName + "Data = UnicodeRange.FromArrays(");
        writer.WriteLine("        " + FormatCompactIntArray(starts) + ",");
        writer.WriteLine("        " + FormatCompactIntArray(ends) + ");");
        writer.WriteLine();
        writer.WriteLine("    internal static UnicodeRange[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return " + methodName + "Data;");
        writer.WriteLine("    }");
    }

    static void WriteRangeMethod(StreamWriter writer, string methodName, int[] starts, int[] ends)
    {
        writer.WriteLine();
        writer.WriteLine("    internal static readonly UnicodeRange[] " + methodName + "Data = UnicodeRange.FromArrays(");
        writer.WriteLine("        " + FormatCompactIntArray(starts) + ",");
        writer.WriteLine("        " + FormatCompactIntArray(ends) + ");");
        writer.WriteLine();
        writer.WriteLine("    internal static UnicodeRange[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return " + methodName + "Data;");
        writer.WriteLine("    }");
    }

    static void WriteRangeValueMethod(StreamWriter writer, string methodName, int[] starts, int[] ends, int[] values)
    {
        writer.WriteLine();
        writer.WriteLine("    internal static readonly UnicodeRangeValue[] " + methodName + "Data = UnicodeRangeValue.FromArrays(");
        writer.WriteLine("        " + FormatCompactIntArray(starts) + ",");
        writer.WriteLine("        " + FormatCompactIntArray(ends) + ",");
        writer.WriteLine("        " + FormatCompactIntArray(values) + ");");
        writer.WriteLine();
        writer.WriteLine("    internal static UnicodeRangeValue[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return " + methodName + "Data;");
        writer.WriteLine("    }");
    }

    static string FormatCompactIntArray(int[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("new int[] { ");
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatInt(values[index]));
            index = index + 1;
        }

        builder.Append(" }");
        return builder.ToString();
    }

    static void WriteStringArrayMethod(StreamWriter writer, string methodName, string[] values)
    {
        writer.WriteLine();
        writer.WriteLine("    internal static string[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return new string[]");
        writer.WriteLine("        {");
        int index = 0;

        while (index < values.Length)
        {
            writer.WriteLine("            \"" + EscapeString(values[index]) + "\",");
            index = index + 1;
        }

        writer.WriteLine("        };");
        writer.WriteLine("    }");
    }

    static void WriteIntArrayMethod(StreamWriter writer, string methodName, int[] values)
    {
        writer.WriteLine();
        writer.WriteLine("    internal static int[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return new int[]");
        writer.WriteLine("        {");
        int index = 0;

        while (index < values.Length)
        {
            writer.WriteLine("            " + FormatInt(values[index]) + ",");
            index = index + 1;
        }

        writer.WriteLine("        };");
        writer.WriteLine("    }");
    }

    static void WriteLongArrayMethod(StreamWriter writer, string methodName, long[] values)
    {
        writer.WriteLine();
        writer.WriteLine("    internal static long[] " + methodName + "()");
        writer.WriteLine("    {");
        writer.WriteLine("        return new long[]");
        writer.WriteLine("        {");
        int index = 0;

        while (index < values.Length)
        {
            writer.WriteLine("            " + values[index].ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
            index = index + 1;
        }

        writer.WriteLine("        };");
        writer.WriteLine("    }");
    }

    static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
