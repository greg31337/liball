namespace ttf;

internal class SfntGsubParserContext
{
    FontDataSlice tableData;
    int glyphCount;
    FontLimits limits;
    int parsedEntries;
    int lookupCount;

    internal SfntGsubParserContext(
        FontDataSlice tableData,
        int glyphCount,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.glyphCount = glyphCount;
        this.limits = limits;
        parsedEntries = 0;
    }

    internal void SetSharedParsingState(
        int lookupCount,
        int parsedEntries)
    {
        this.lookupCount = lookupCount;
        this.parsedEntries = parsedEntries;
    }

    internal int GetParsedEntryCount()
    {
        return parsedEntries;
    }

    internal SfntGsubTable Parse(SfntLayoutTable layoutTable)
    {
        SfntLayoutLookupList layoutLookups = layoutTable.GetLookupList();
        lookupCount = layoutLookups.GetLookupCount();
        SfntGsubLookup[] lookups =
            new SfntGsubLookup[layoutLookups.GetLookupCount()];
        int lookupIndex = 0;

        while (lookupIndex < lookups.Length)
        {
            SfntLayoutLookup lookup = layoutLookups.GetLookup(lookupIndex);
            int lookupType = lookup.GetLookupType();

            if (lookupType < 1 || lookupType > 8)
            {
                throw FontFormatException.Create(
                    "A GSUB lookup type is invalid.");
            }

            int subtableCount = lookup.GetSubtableCount();
            int effectiveLookupType = lookupType;
            int[] subtableOffsets = new int[subtableCount];
            SfntExtensionLookup[] extensions = lookupType == 7 ?
                new SfntExtensionLookup[subtableCount] :
                new SfntExtensionLookup[0];
            int subtableIndex = 0;

            while (subtableIndex < subtableCount)
            {
                int offset = lookup.GetSubtableTableOffset(subtableIndex);

                if (lookupType == 7)
                {
                    SfntExtensionLookup extension =
                        SfntLayoutPrimitiveParser.ParseExtension(
                            tableData,
                            offset,
                            7,
                            limits);
                    int extensionType = extension.GetLookupType();

                    if (extensionType < 1 || extensionType > 8)
                    {
                        throw FontFormatException.Create(
                            "A GSUB extension lookup type is invalid.");
                    }

                    if (subtableIndex > 0 &&
                        extensionType != effectiveLookupType)
                    {
                        throw FontFormatException.Create(
                            "GSUB extension subtables have different lookup types.");
                    }

                    effectiveLookupType = extensionType;
                    extensions[subtableIndex] = extension;
                    subtableOffsets[subtableIndex] =
                        extension.GetExtensionTableOffset();
                }
                else
                {
                    subtableOffsets[subtableIndex] = offset;
                }

                subtableIndex += 1;
            }

            SfntGsubSingleSubstitution[] singles =
                effectiveLookupType == 1 ?
                new SfntGsubSingleSubstitution[subtableCount] :
                new SfntGsubSingleSubstitution[0];
            SfntGsubMultipleSubstitution[] multiples =
                effectiveLookupType == 2 ?
                new SfntGsubMultipleSubstitution[subtableCount] :
                new SfntGsubMultipleSubstitution[0];
            SfntGsubAlternateSubstitution[] alternates =
                effectiveLookupType == 3 ?
                new SfntGsubAlternateSubstitution[subtableCount] :
                new SfntGsubAlternateSubstitution[0];
            SfntGsubLigatureSubstitution[] ligatures =
                effectiveLookupType == 4 ?
                new SfntGsubLigatureSubstitution[subtableCount] :
                new SfntGsubLigatureSubstitution[0];
            SfntGsubContextSubstitution[] contexts =
                effectiveLookupType == 5 ?
                new SfntGsubContextSubstitution[subtableCount] :
                new SfntGsubContextSubstitution[0];
            SfntGsubChainedContextSubstitution[] chainedContexts =
                effectiveLookupType == 6 ?
                new SfntGsubChainedContextSubstitution[subtableCount] :
                new SfntGsubChainedContextSubstitution[0];
            SfntGsubReverseSubstitution[] reverses =
                effectiveLookupType == 8 ?
                new SfntGsubReverseSubstitution[subtableCount] :
                new SfntGsubReverseSubstitution[0];
            subtableIndex = 0;

            while (subtableIndex < subtableCount)
            {
                int offset = subtableOffsets[subtableIndex];

                if (effectiveLookupType == 1)
                {
                    singles[subtableIndex] = ParseSingle(offset);
                }
                else if (effectiveLookupType == 2)
                {
                    multiples[subtableIndex] = ParseMultiple(offset);
                }
                else if (effectiveLookupType == 3)
                {
                    alternates[subtableIndex] = ParseAlternate(offset);
                }
                else if (effectiveLookupType == 4)
                {
                    ligatures[subtableIndex] = ParseLigature(offset);
                }
                else if (effectiveLookupType == 5)
                {
                    contexts[subtableIndex] = ParseContext(offset);
                }
                else if (effectiveLookupType == 6)
                {
                    chainedContexts[subtableIndex] =
                        ParseChainedContext(offset);
                }
                else if (effectiveLookupType == 8)
                {
                    reverses[subtableIndex] = ParseReverse(offset);
                }

                subtableIndex += 1;
            }

            lookups[lookupIndex] = new SfntGsubLookup(
                lookup,
                singles,
                multiples,
                alternates,
                ligatures,
                contexts,
                chainedContexts,
                extensions,
                reverses,
                effectiveLookupType);
            lookupIndex += 1;
        }

        return new SfntGsubTable(layoutTable, lookups, limits);
    }

    SfntGsubSingleSubstitution ParseSingle(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GSUB SingleSubst header is truncated.");
        int format = reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();

        if (format == 1)
        {
            int deltaGlyphId = reader.ReadInt16();
            SfntCoverageTable coverage = ParseCoverage(
                offset,
                coverageOffset,
                6,
                "A GSUB SingleSubst Coverage offset is invalid.");
            ValidateDeltaOutputs(coverage, deltaGlyphId);
            return new SfntGsubSingleSubstitution(
                format,
                coverage,
                deltaGlyphId,
                new int[0]);
        }

        if (format == 2)
        {
            int substituteCount = reader.ReadUInt16();
            AddEntries(substituteCount);
            int headerLength = CheckedFontMath.GetRangeEnd(
                6,
                substituteCount,
                2);
            reader = CreateReader(
                offset,
                headerLength,
                "A format 2 GSUB SingleSubst array is truncated.");
            reader.ReadUInt16();
            reader.ReadUInt16();
            reader.ReadUInt16();
            int[] substitutes = ReadGlyphIds(reader, substituteCount);
            SfntCoverageTable coverage = ParseCoverage(
                offset,
                coverageOffset,
                headerLength,
                "A GSUB SingleSubst Coverage offset is invalid.");
            ValidateCoverageCount(
                coverage,
                substituteCount,
                "A GSUB SingleSubst glyph count does not match its Coverage table.");
            return new SfntGsubSingleSubstitution(
                format,
                coverage,
                0,
                substitutes);
        }

        throw FontFormatException.Create(
            "The GSUB SingleSubst format is not supported.");
    }

    SfntGsubMultipleSubstitution ParseMultiple(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GSUB MultipleSubst header is truncated.");
        int format = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GSUB MultipleSubst format is not supported.");
        }

        int coverageOffset = reader.ReadUInt16();
        int sequenceCount = reader.ReadUInt16();
        AddEntries(sequenceCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            sequenceCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB MultipleSubst offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] sequenceOffsets = ReadOffsets(reader, sequenceCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A GSUB MultipleSubst Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            sequenceCount,
            "A GSUB MultipleSubst sequence count does not match its Coverage table.");
        SfntGsubGlyphSequence[] sequences =
            new SfntGsubGlyphSequence[sequenceCount];
        int index = 0;

        while (index < sequences.Length)
        {
            int sequenceOffset = ResolveRequiredOffset(
                offset,
                sequenceOffsets[index],
                headerLength,
                2,
                "A GSUB Sequence offset is invalid.");
            sequences[index] = ParseSequence(sequenceOffset);
            index += 1;
        }

        return new SfntGsubMultipleSubstitution(coverage, sequences);
    }

    SfntGsubAlternateSubstitution ParseAlternate(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GSUB AlternateSubst header is truncated.");
        int format = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GSUB AlternateSubst format is not supported.");
        }

        int coverageOffset = reader.ReadUInt16();
        int alternateSetCount = reader.ReadUInt16();
        AddEntries(alternateSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            alternateSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB AlternateSubst offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] alternateSetOffsets = ReadOffsets(
            reader,
            alternateSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A GSUB AlternateSubst Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            alternateSetCount,
            "A GSUB AlternateSubst set count does not match its Coverage table.");
        SfntGsubAlternateSet[] alternateSets =
            new SfntGsubAlternateSet[alternateSetCount];
        int index = 0;

        while (index < alternateSets.Length)
        {
            int setOffset = ResolveRequiredOffset(
                offset,
                alternateSetOffsets[index],
                headerLength,
                2,
                "A GSUB AlternateSet offset is invalid.");
            alternateSets[index] = ParseAlternateSet(setOffset);
            index += 1;
        }

        return new SfntGsubAlternateSubstitution(coverage, alternateSets);
    }

    SfntGsubLigatureSubstitution ParseLigature(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GSUB LigatureSubst header is truncated.");
        int format = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GSUB LigatureSubst format is not supported.");
        }

        int coverageOffset = reader.ReadUInt16();
        int ligatureSetCount = reader.ReadUInt16();
        AddEntries(ligatureSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            ligatureSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB LigatureSubst offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] ligatureSetOffsets = ReadOffsets(
            reader,
            ligatureSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A GSUB LigatureSubst Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            ligatureSetCount,
            "A GSUB LigatureSubst set count does not match its Coverage table.");
        SfntGsubLigatureSet[] ligatureSets =
            new SfntGsubLigatureSet[ligatureSetCount];
        int index = 0;

        while (index < ligatureSets.Length)
        {
            int setOffset = ResolveRequiredOffset(
                offset,
                ligatureSetOffsets[index],
                headerLength,
                2,
                "A GSUB LigatureSet offset is invalid.");
            ligatureSets[index] = ParseLigatureSet(setOffset);
            index += 1;
        }

        return new SfntGsubLigatureSubstitution(coverage, ligatureSets);
    }

    internal SfntGsubContextSubstitution ParseContext(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB contextual subtable is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParseContextFormat1(offset);
        }

        if (format == 2)
        {
            return ParseContextFormat2(offset);
        }

        if (format == 3)
        {
            return ParseContextFormat3(offset);
        }

        throw FontFormatException.Create(
            "The GSUB contextual format is not supported.");
    }

    SfntGsubContextSubstitution ParseContextFormat1(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A format 1 GSUB contextual header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int ruleSetCount = reader.ReadUInt16();
        AddEntries(ruleSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            ruleSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A format 1 GSUB contextual rule-set array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] ruleSetOffsets = ReadOffsets(reader, ruleSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 1 GSUB contextual Coverage offset is invalid.");
        SfntGsubContextRuleSet[] ruleSets =
            new SfntGsubContextRuleSet[ruleSetCount];
        int index = 0;

        while (index < ruleSets.Length)
        {
            if (ruleSetOffsets[index] != 0)
            {
                int ruleSetOffset = ResolveRequiredOffset(
                    offset,
                    ruleSetOffsets[index],
                    headerLength,
                    2,
                    "A GSUB contextual rule-set offset is invalid.");
                ruleSets[index] = ParseContextRuleSet(
                    ruleSetOffset,
                    false,
                    0);
            }

            index += 1;
        }

        return new SfntGsubContextSubstitution(
            1,
            coverage,
            null,
            ruleSets,
            new SfntCoverageTable[0],
            null);
    }

    SfntGsubContextSubstitution ParseContextFormat2(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            8,
            "A format 2 GSUB contextual header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int classDefinitionOffset = reader.ReadUInt16();
        int ruleSetCount = reader.ReadUInt16();
        AddEntries(ruleSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            8,
            ruleSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A format 2 GSUB contextual rule-set array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] ruleSetOffsets = ReadOffsets(reader, ruleSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 2 GSUB contextual Coverage offset is invalid.");
        int classTableOffset = ResolveRequiredOffset(
            offset,
            classDefinitionOffset,
            headerLength,
            2,
            "A format 2 GSUB contextual ClassDef offset is invalid.");
        SfntClassDefinitionTable classDefinition =
            SfntLayoutGlyphClassParser.ParseClassDefinition(
                tableData,
                classTableOffset,
                glyphCount,
                limits);
        SfntGsubContextRuleSet[] ruleSets =
            new SfntGsubContextRuleSet[ruleSetCount];
        int index = 0;

        while (index < ruleSets.Length)
        {
            if (ruleSetOffsets[index] != 0)
            {
                int ruleSetOffset = ResolveRequiredOffset(
                    offset,
                    ruleSetOffsets[index],
                    headerLength,
                    2,
                    "A GSUB contextual class-rule-set offset is invalid.");
                ruleSets[index] = ParseContextRuleSet(
                    ruleSetOffset,
                    true,
                    classDefinition.GetMaximumClassValue());
            }

            index += 1;
        }

        return new SfntGsubContextSubstitution(
            2,
            coverage,
            classDefinition,
            ruleSets,
            new SfntCoverageTable[0],
            null);
    }

    SfntGsubContextSubstitution ParseContextFormat3(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A format 3 GSUB contextual header is truncated.");
        reader.ReadUInt16();
        int inputCount = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();

        if (inputCount == 0)
        {
            throw FontFormatException.Create(
                "A GSUB contextual input sequence is empty.");
        }

        AddEntries(inputCount);
        AddEntries(recordCount);
        int coverageArrayLength = CheckedFontMath.MultiplyCount(
            inputCount,
            2);
        int recordsOffset = CheckedFontMath.AddOffset(
            offset,
            CheckedFontMath.AddOffset(6, coverageArrayLength));
        int recordLength = CheckedFontMath.MultiplyCount(recordCount, 4);
        int headerLength = CheckedFontMath.AddOffset(
            6,
            CheckedFontMath.AddOffset(coverageArrayLength, recordLength));
        reader = CreateReader(
            offset,
            headerLength,
            "A format 3 GSUB contextual subtable is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] coverageOffsets = ReadOffsets(reader, inputCount);
        SfntCoverageTable[] coverages =
            new SfntCoverageTable[inputCount];
        int index = 0;

        while (index < coverages.Length)
        {
            coverages[index] = ParseCoverage(
                offset,
                coverageOffsets[index],
                headerLength,
                "A format 3 GSUB contextual Coverage offset is invalid.");
            index += 1;
        }

        SfntSequenceLookupRecords records =
            SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                tableData,
                recordsOffset,
                recordCount,
                inputCount,
                lookupCount,
                limits);
        return new SfntGsubContextSubstitution(
            3,
            null,
            null,
            new SfntGsubContextRuleSet[0],
            coverages,
            records);
    }

    SfntGsubContextRuleSet ParseContextRuleSet(
        int offset,
        bool classBased,
        int maximumClassValue)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB contextual rule-set header is truncated.");
        int ruleCount = reader.ReadUInt16();
        AddEntries(ruleCount);
        int headerLength = CheckedFontMath.GetRangeEnd(2, ruleCount, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB contextual rule-set offset array is truncated.");
        reader.ReadUInt16();
        int[] ruleOffsets = ReadOffsets(reader, ruleCount);
        SfntGsubContextRule[] rules = new SfntGsubContextRule[ruleCount];
        int index = 0;

        while (index < rules.Length)
        {
            int ruleOffset = ResolveRequiredOffset(
                offset,
                ruleOffsets[index],
                headerLength,
                4,
                "A GSUB contextual rule offset is invalid.");
            rules[index] = ParseContextRule(
                ruleOffset,
                classBased,
                maximumClassValue);
            index += 1;
        }

        return new SfntGsubContextRuleSet(rules);
    }

    SfntGsubContextRule ParseContextRule(
        int offset,
        bool classBased,
        int maximumClassValue)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A GSUB contextual rule header is truncated.");
        int inputCount = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();

        if (inputCount == 0)
        {
            throw FontFormatException.Create(
                "A GSUB contextual rule has no input glyphs.");
        }

        int trailingInputCount = inputCount - 1;
        AddEntries(trailingInputCount);
        AddEntries(recordCount);
        int inputLength = CheckedFontMath.MultiplyCount(
            trailingInputCount,
            2);
        int recordOffset = CheckedFontMath.AddOffset(
            offset,
            CheckedFontMath.AddOffset(4, inputLength));
        int length = CheckedFontMath.AddOffset(
            4,
            CheckedFontMath.AddOffset(
                inputLength,
                CheckedFontMath.MultiplyCount(recordCount, 4)));
        reader = CreateReader(
            offset,
            length,
            "A GSUB contextual rule is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] inputValues = new int[trailingInputCount];
        int index = 0;

        while (index < inputValues.Length)
        {
            int inputValue = reader.ReadUInt16();

            if (classBased)
            {
                if (inputValue > maximumClassValue)
                {
                    throw FontFormatException.Create(
                        "A GSUB contextual input class is invalid.");
                }
            }
            else
            {
                ValidateGlyphId(inputValue);
            }

            inputValues[index] = inputValue;
            index += 1;
        }

        SfntSequenceLookupRecords records =
            SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                tableData,
                recordOffset,
                recordCount,
                inputCount,
                lookupCount,
                limits);
        return new SfntGsubContextRule(inputValues, records);
    }

    internal SfntGsubChainedContextSubstitution ParseChainedContext(
        int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB chained contextual subtable is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParseChainedContextFormat1(offset);
        }

        if (format == 2)
        {
            return ParseChainedContextFormat2(offset);
        }

        if (format == 3)
        {
            return ParseChainedContextFormat3(offset);
        }

        throw FontFormatException.Create(
            "The GSUB chained contextual format is not supported.");
    }

    SfntGsubChainedContextSubstitution ParseChainedContextFormat1(
        int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A format 1 GSUB chained contextual header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int ruleSetCount = reader.ReadUInt16();
        AddEntries(ruleSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            ruleSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A format 1 GSUB chained rule-set array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] ruleSetOffsets = ReadOffsets(reader, ruleSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 1 GSUB chained Coverage offset is invalid.");
        SfntGsubChainedRuleSet[] ruleSets =
            new SfntGsubChainedRuleSet[ruleSetCount];
        int index = 0;

        while (index < ruleSets.Length)
        {
            if (ruleSetOffsets[index] != 0)
            {
                int ruleSetOffset = ResolveRequiredOffset(
                    offset,
                    ruleSetOffsets[index],
                    headerLength,
                    2,
                    "A GSUB chained rule-set offset is invalid.");
                ruleSets[index] = ParseChainedRuleSet(
                    ruleSetOffset,
                    false,
                    0,
                    0,
                    0);
            }

            index += 1;
        }

        return new SfntGsubChainedContextSubstitution(
            1,
            coverage,
            null,
            null,
            null,
            ruleSets,
            new SfntCoverageTable[0],
            new SfntCoverageTable[0],
            new SfntCoverageTable[0],
            null);
    }

    SfntGsubChainedContextSubstitution ParseChainedContextFormat2(
        int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            12,
            "A format 2 GSUB chained contextual header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int backtrackClassOffset = reader.ReadUInt16();
        int inputClassOffset = reader.ReadUInt16();
        int lookaheadClassOffset = reader.ReadUInt16();
        int ruleSetCount = reader.ReadUInt16();
        AddEntries(ruleSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            12,
            ruleSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A format 2 GSUB chained rule-set array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] ruleSetOffsets = ReadOffsets(reader, ruleSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 2 GSUB chained Coverage offset is invalid.");
        SfntClassDefinitionTable backtrackClassDefinition =
            ParseClassDefinition(
                offset,
                backtrackClassOffset,
                headerLength,
                "A GSUB chained backtrack ClassDef offset is invalid.");
        SfntClassDefinitionTable inputClassDefinition =
            ParseClassDefinition(
                offset,
                inputClassOffset,
                headerLength,
                "A GSUB chained input ClassDef offset is invalid.");
        SfntClassDefinitionTable lookaheadClassDefinition =
            ParseClassDefinition(
                offset,
                lookaheadClassOffset,
                headerLength,
                "A GSUB chained lookahead ClassDef offset is invalid.");
        SfntGsubChainedRuleSet[] ruleSets =
            new SfntGsubChainedRuleSet[ruleSetCount];
        int index = 0;

        while (index < ruleSets.Length)
        {
            if (ruleSetOffsets[index] != 0)
            {
                int ruleSetOffset = ResolveRequiredOffset(
                    offset,
                    ruleSetOffsets[index],
                    headerLength,
                    2,
                    "A GSUB chained class-rule-set offset is invalid.");
                ruleSets[index] = ParseChainedRuleSet(
                    ruleSetOffset,
                    true,
                    backtrackClassDefinition.GetMaximumClassValue(),
                    inputClassDefinition.GetMaximumClassValue(),
                    lookaheadClassDefinition.GetMaximumClassValue());
            }

            index += 1;
        }

        return new SfntGsubChainedContextSubstitution(
            2,
            coverage,
            backtrackClassDefinition,
            inputClassDefinition,
            lookaheadClassDefinition,
            ruleSets,
            new SfntCoverageTable[0],
            new SfntCoverageTable[0],
            new SfntCoverageTable[0],
            null);
    }

    SfntGsubChainedContextSubstitution ParseChainedContextFormat3(
        int offset)
    {
        int cursor = CheckedFontMath.AddOffset(offset, 2);
        BigEndianReader reader = CreateReader(
            cursor,
            2,
            "A format 3 GSUB chained backtrack count is truncated.");
        int backtrackCount = reader.ReadUInt16();
        AddEntries(backtrackCount);
        int backtrackLength = CheckedFontMath.GetRangeEnd(
            2,
            backtrackCount,
            2);
        reader = CreateReader(
            cursor,
            backtrackLength,
            "A format 3 GSUB chained backtrack array is truncated.");
        reader.ReadUInt16();
        int[] backtrackOffsets = ReadOffsets(reader, backtrackCount);
        cursor = CheckedFontMath.AddOffset(cursor, backtrackLength);
        reader = CreateReader(
            cursor,
            2,
            "A format 3 GSUB chained input count is truncated.");
        int inputCount = reader.ReadUInt16();

        if (inputCount == 0)
        {
            throw FontFormatException.Create(
                "A GSUB chained contextual input sequence is empty.");
        }

        AddEntries(inputCount);
        int inputLength = CheckedFontMath.GetRangeEnd(2, inputCount, 2);
        reader = CreateReader(
            cursor,
            inputLength,
            "A format 3 GSUB chained input array is truncated.");
        reader.ReadUInt16();
        int[] inputOffsets = ReadOffsets(reader, inputCount);
        cursor = CheckedFontMath.AddOffset(cursor, inputLength);
        reader = CreateReader(
            cursor,
            2,
            "A format 3 GSUB chained lookahead count is truncated.");
        int lookaheadCount = reader.ReadUInt16();
        AddEntries(lookaheadCount);
        int lookaheadLength = CheckedFontMath.GetRangeEnd(
            2,
            lookaheadCount,
            2);
        reader = CreateReader(
            cursor,
            lookaheadLength,
            "A format 3 GSUB chained lookahead array is truncated.");
        reader.ReadUInt16();
        int[] lookaheadOffsets = ReadOffsets(reader, lookaheadCount);
        cursor = CheckedFontMath.AddOffset(cursor, lookaheadLength);
        reader = CreateReader(
            cursor,
            2,
            "A format 3 GSUB chained lookup count is truncated.");
        int recordCount = reader.ReadUInt16();
        AddEntries(recordCount);
        int recordsOffset = CheckedFontMath.AddOffset(cursor, 2);
        int recordsLength = CheckedFontMath.MultiplyCount(recordCount, 4);
        int endOffset = CheckedFontMath.AddOffset(
            recordsOffset,
            recordsLength);
        CreateReader(
            recordsOffset,
            recordsLength,
            "A format 3 GSUB chained lookup array is truncated.");
        int headerLength = endOffset - offset;
        SfntCoverageTable[] backtrackCoverages = ParseCoverageArray(
            offset,
            backtrackOffsets,
            headerLength,
            "A format 3 GSUB chained backtrack Coverage offset is invalid.");
        SfntCoverageTable[] inputCoverages = ParseCoverageArray(
            offset,
            inputOffsets,
            headerLength,
            "A format 3 GSUB chained input Coverage offset is invalid.");
        SfntCoverageTable[] lookaheadCoverages = ParseCoverageArray(
            offset,
            lookaheadOffsets,
            headerLength,
            "A format 3 GSUB chained lookahead Coverage offset is invalid.");
        SfntSequenceLookupRecords records =
            SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                tableData,
                recordsOffset,
                recordCount,
                inputCount,
                lookupCount,
                limits);
        return new SfntGsubChainedContextSubstitution(
            3,
            null,
            null,
            null,
            null,
            new SfntGsubChainedRuleSet[0],
            backtrackCoverages,
            inputCoverages,
            lookaheadCoverages,
            records);
    }

    SfntGsubChainedRuleSet ParseChainedRuleSet(
        int offset,
        bool classBased,
        int maximumBacktrackClass,
        int maximumInputClass,
        int maximumLookaheadClass)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB chained rule-set header is truncated.");
        int ruleCount = reader.ReadUInt16();
        AddEntries(ruleCount);
        int headerLength = CheckedFontMath.GetRangeEnd(2, ruleCount, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB chained rule-set offset array is truncated.");
        reader.ReadUInt16();
        int[] ruleOffsets = ReadOffsets(reader, ruleCount);
        SfntGsubChainedRule[] rules =
            new SfntGsubChainedRule[ruleCount];
        int index = 0;

        while (index < rules.Length)
        {
            int ruleOffset = ResolveRequiredOffset(
                offset,
                ruleOffsets[index],
                headerLength,
                6,
                "A GSUB chained rule offset is invalid.");
            rules[index] = ParseChainedRule(
                ruleOffset,
                classBased,
                maximumBacktrackClass,
                maximumInputClass,
                maximumLookaheadClass);
            index += 1;
        }

        return new SfntGsubChainedRuleSet(rules);
    }

    SfntGsubChainedRule ParseChainedRule(
        int offset,
        bool classBased,
        int maximumBacktrackClass,
        int maximumInputClass,
        int maximumLookaheadClass)
    {
        int cursor = offset;
        BigEndianReader reader = CreateReader(
            cursor,
            2,
            "A GSUB chained backtrack count is truncated.");
        int backtrackCount = reader.ReadUInt16();
        AddEntries(backtrackCount);
        int backtrackLength = CheckedFontMath.GetRangeEnd(
            2,
            backtrackCount,
            2);
        reader = CreateReader(
            cursor,
            backtrackLength,
            "A GSUB chained backtrack sequence is truncated.");
        reader.ReadUInt16();
        int[] backtrackValues = ReadChainedValues(
            reader,
            backtrackCount,
            classBased,
            maximumBacktrackClass);
        cursor = CheckedFontMath.AddOffset(cursor, backtrackLength);
        reader = CreateReader(
            cursor,
            2,
            "A GSUB chained input count is truncated.");
        int inputCount = reader.ReadUInt16();

        if (inputCount == 0)
        {
            throw FontFormatException.Create(
                "A GSUB chained contextual rule has no input glyphs.");
        }

        int trailingInputCount = inputCount - 1;
        AddEntries(trailingInputCount);
        int inputLength = CheckedFontMath.GetRangeEnd(
            2,
            trailingInputCount,
            2);
        reader = CreateReader(
            cursor,
            inputLength,
            "A GSUB chained input sequence is truncated.");
        reader.ReadUInt16();
        int[] inputValues = ReadChainedValues(
            reader,
            trailingInputCount,
            classBased,
            maximumInputClass);
        cursor = CheckedFontMath.AddOffset(cursor, inputLength);
        reader = CreateReader(
            cursor,
            2,
            "A GSUB chained lookahead count is truncated.");
        int lookaheadCount = reader.ReadUInt16();
        AddEntries(lookaheadCount);
        int lookaheadLength = CheckedFontMath.GetRangeEnd(
            2,
            lookaheadCount,
            2);
        reader = CreateReader(
            cursor,
            lookaheadLength,
            "A GSUB chained lookahead sequence is truncated.");
        reader.ReadUInt16();
        int[] lookaheadValues = ReadChainedValues(
            reader,
            lookaheadCount,
            classBased,
            maximumLookaheadClass);
        cursor = CheckedFontMath.AddOffset(cursor, lookaheadLength);
        reader = CreateReader(
            cursor,
            2,
            "A GSUB chained lookup count is truncated.");
        int recordCount = reader.ReadUInt16();
        AddEntries(recordCount);
        int recordsOffset = CheckedFontMath.AddOffset(cursor, 2);
        SfntSequenceLookupRecords records =
            SfntLayoutPrimitiveParser.ParseSequenceLookupRecords(
                tableData,
                recordsOffset,
                recordCount,
                inputCount,
                lookupCount,
                limits);
        return new SfntGsubChainedRule(
            backtrackValues,
            inputValues,
            lookaheadValues,
            records);
    }

    SfntGsubReverseSubstitution ParseReverse(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GSUB reverse chained subtable is truncated.");
        int format = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GSUB reverse chained format is not supported.");
        }

        int coverageOffset = reader.ReadUInt16();
        int backtrackCount = reader.ReadUInt16();
        AddEntries(backtrackCount);
        int cursor = CheckedFontMath.AddOffset(
            offset,
            CheckedFontMath.GetRangeEnd(6, backtrackCount, 2));
        reader = CreateReader(
            offset,
            cursor - offset,
            "A GSUB reverse backtrack array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] backtrackOffsets = ReadOffsets(reader, backtrackCount);
        reader = CreateReader(
            cursor,
            2,
            "A GSUB reverse lookahead count is truncated.");
        int lookaheadCount = reader.ReadUInt16();
        AddEntries(lookaheadCount);
        int lookaheadLength = CheckedFontMath.GetRangeEnd(
            2,
            lookaheadCount,
            2);
        reader = CreateReader(
            cursor,
            lookaheadLength,
            "A GSUB reverse lookahead array is truncated.");
        reader.ReadUInt16();
        int[] lookaheadOffsets = ReadOffsets(reader, lookaheadCount);
        cursor = CheckedFontMath.AddOffset(cursor, lookaheadLength);
        reader = CreateReader(
            cursor,
            2,
            "A GSUB reverse substitute count is truncated.");
        int substituteCount = reader.ReadUInt16();
        AddEntries(substituteCount);
        int substituteLength = CheckedFontMath.GetRangeEnd(
            2,
            substituteCount,
            2);
        reader = CreateReader(
            cursor,
            substituteLength,
            "A GSUB reverse substitute array is truncated.");
        reader.ReadUInt16();
        int[] substitutes = ReadGlyphIds(reader, substituteCount);
        int headerLength = CheckedFontMath.AddOffset(
            cursor - offset,
            substituteLength);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A GSUB reverse input Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            substituteCount,
            "A GSUB reverse substitute count does not match its Coverage table.");
        SfntCoverageTable[] backtrackCoverages = ParseCoverageArray(
            offset,
            backtrackOffsets,
            headerLength,
            "A GSUB reverse backtrack Coverage offset is invalid.");
        SfntCoverageTable[] lookaheadCoverages = ParseCoverageArray(
            offset,
            lookaheadOffsets,
            headerLength,
            "A GSUB reverse lookahead Coverage offset is invalid.");
        return new SfntGsubReverseSubstitution(
            coverage,
            backtrackCoverages,
            lookaheadCoverages,
            substitutes);
    }

    int[] ReadChainedValues(
        BigEndianReader reader,
        int count,
        bool classBased,
        int maximumClassValue)
    {
        int[] values = new int[count];
        int index = 0;

        while (index < values.Length)
        {
            int value = reader.ReadUInt16();

            if (classBased)
            {
                if (value > maximumClassValue)
                {
                    throw FontFormatException.Create(
                        "A GSUB chained contextual class is invalid.");
                }
            }
            else
            {
                ValidateGlyphId(value);
            }

            values[index] = value;
            index += 1;
        }

        return values;
    }

    SfntCoverageTable[] ParseCoverageArray(
        int parentOffset,
        int[] relativeOffsets,
        int minimumRelativeOffset,
        string message)
    {
        SfntCoverageTable[] coverages =
            new SfntCoverageTable[relativeOffsets.Length];
        int index = 0;

        while (index < coverages.Length)
        {
            coverages[index] = ParseCoverage(
                parentOffset,
                relativeOffsets[index],
                minimumRelativeOffset,
                message);
            index += 1;
        }

        return coverages;
    }

    SfntClassDefinitionTable ParseClassDefinition(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        int classDefinitionOffset = ResolveRequiredOffset(
            parentOffset,
            relativeOffset,
            minimumRelativeOffset,
            2,
            message);
        return SfntLayoutGlyphClassParser.ParseClassDefinition(
            tableData,
            classDefinitionOffset,
            glyphCount,
            limits);
    }

    SfntGsubGlyphSequence ParseSequence(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB Sequence header is truncated.");
        int count = reader.ReadUInt16();

        if (count == 0)
        {
            throw FontFormatException.Create(
                "A GSUB Sequence cannot delete its input glyph.");
        }

        AddEntries(count);
        int length = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            length,
            "A GSUB Sequence glyph array is truncated.");
        reader.ReadUInt16();
        return new SfntGsubGlyphSequence(ReadGlyphIds(reader, count));
    }

    SfntGsubAlternateSet ParseAlternateSet(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB AlternateSet header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int length = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            length,
            "A GSUB AlternateSet glyph array is truncated.");
        reader.ReadUInt16();
        return new SfntGsubAlternateSet(ReadGlyphIds(reader, count));
    }

    SfntGsubLigatureSet ParseLigatureSet(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GSUB LigatureSet header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GSUB LigatureSet offset array is truncated.");
        reader.ReadUInt16();
        int[] offsets = ReadOffsets(reader, count);
        SfntGsubLigature[] ligatures = new SfntGsubLigature[count];
        int index = 0;

        while (index < ligatures.Length)
        {
            int ligatureOffset = ResolveRequiredOffset(
                offset,
                offsets[index],
                headerLength,
                4,
                "A GSUB Ligature offset is invalid.");
            ligatures[index] = ParseLigatureTable(ligatureOffset);
            index += 1;
        }

        return new SfntGsubLigatureSet(ligatures);
    }

    SfntGsubLigature ParseLigatureTable(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A GSUB Ligature table is truncated.");
        int ligatureGlyphId = reader.ReadUInt16();
        int componentCount = reader.ReadUInt16();
        ValidateGlyphId(ligatureGlyphId);

        if (componentCount == 0)
        {
            throw FontFormatException.Create(
                "A GSUB Ligature has no components.");
        }

        int trailingCount = componentCount - 1;
        AddEntries(trailingCount);
        int length = CheckedFontMath.GetRangeEnd(4, trailingCount, 2);
        reader = CreateReader(
            offset,
            length,
            "A GSUB Ligature component array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        return new SfntGsubLigature(
            ligatureGlyphId,
            ReadGlyphIds(reader, trailingCount));
    }

    SfntCoverageTable ParseCoverage(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        int coverageOffset = ResolveRequiredOffset(
            parentOffset,
            relativeOffset,
            minimumRelativeOffset,
            2,
            message);
        SfntCoverageTable coverage =
            SfntLayoutGlyphClassParser.ParseCoverage(
                tableData,
                coverageOffset,
                glyphCount,
                limits);
        AddEntries(coverage.GetCoveredGlyphCount());
        return coverage;
    }

    int[] ReadGlyphIds(BigEndianReader reader, int count)
    {
        int[] glyphIds = new int[count];
        int index = 0;

        while (index < glyphIds.Length)
        {
            int glyphId = reader.ReadUInt16();
            ValidateGlyphId(glyphId);
            glyphIds[index] = glyphId;
            index += 1;
        }

        return glyphIds;
    }

    static int[] ReadOffsets(BigEndianReader reader, int count)
    {
        int[] offsets = new int[count];
        int index = 0;

        while (index < offsets.Length)
        {
            offsets[index] = reader.ReadUInt16();
            index += 1;
        }

        return offsets;
    }

    void ValidateDeltaOutputs(
        SfntCoverageTable coverage,
        int deltaGlyphId)
    {
        int index = 0;

        while (index < coverage.GetCoveredGlyphCount())
        {
            int glyphId = coverage.GetGlyphId(index);
            int substituteGlyphId = glyphId + deltaGlyphId & 0xFFFF;
            ValidateGlyphId(substituteGlyphId);
            index += 1;
        }
    }

    static void ValidateCoverageCount(
        SfntCoverageTable coverage,
        int count,
        string message)
    {
        if (coverage.GetCoveredGlyphCount() != count)
        {
            throw FontFormatException.Create(message);
        }
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw FontFormatException.Create(
                "A GSUB output glyph ID is invalid.");
        }
    }

    void AddEntries(int count)
    {
        if (count > limits.GetMaxCoverageEntries() - parsedEntries)
        {
            throw FontLimitException.Create(
                "GSUB data exceeds the configured coverage-entry limit.");
        }

        parsedEntries += count;
    }

    BigEndianReader CreateReader(
        int offset,
        int requiredLength,
        string message)
    {
        if (offset < 0 ||
            requiredLength < 0 ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return new BigEndianReader(tableData.GetSlice(
            offset,
            tableData.GetLength() - offset));
    }

    int ResolveRequiredOffset(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(message);
        }

        int absoluteOffset = CheckedFontMath.AddOffset(
            parentOffset,
            relativeOffset);

        if (absoluteOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return absoluteOffset;
    }
}

internal static class SfntGsubTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Gsub,
        1,
        1,
        1,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntGsubTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntGsubTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The GSUB table requires a supported maxp table.");
        }

        FontDataSlice tableData = table.GetDataSlice();
        SfntLayoutTable layoutTable = SfntLayoutTableParser.Parse(
            tableData,
            limits);
        return new SfntGsubParserContext(
            tableData,
            maxp.GetGlyphCount(),
            limits).Parse(layoutTable);
    }
}
