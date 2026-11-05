namespace ttf;

internal class SfntGposParserContext
{
    FontDataSlice tableData;
    int glyphCount;
    FontLimits limits;
    SfntItemVariationStore? variationStore;
    int parsedEntries;
    int lookupCount;
    SfntGsubParserContext? contextParser;

    internal SfntGposParserContext(
        FontDataSlice tableData,
        int glyphCount,
        FontLimits limits,
        SfntItemVariationStore? variationStore)
    {
        this.tableData = tableData;
        this.glyphCount = glyphCount;
        this.limits = limits;
        this.variationStore = variationStore;
        parsedEntries = 0;
    }

    internal SfntGposTable Parse(SfntLayoutTable layoutTable)
    {
        SfntLayoutLookupList layoutLookups = layoutTable.GetLookupList();
        lookupCount = layoutLookups.GetLookupCount();
        contextParser = new SfntGsubParserContext(
            tableData,
            glyphCount,
            limits);
        SfntGposLookup[] lookups =
            new SfntGposLookup[layoutLookups.GetLookupCount()];
        int lookupIndex = 0;

        while (lookupIndex < lookups.Length)
        {
            SfntLayoutLookup layoutLookup =
                layoutLookups.GetLookup(lookupIndex);
            int lookupType = layoutLookup.GetLookupType();

            if (lookupType < 1 || lookupType > 9)
            {
                throw FontFormatException.Create(
                    "A GPOS lookup type is invalid.");
            }

            int subtableCount = layoutLookup.GetSubtableCount();
            int effectiveLookupType = lookupType;
            int[] subtableOffsets = new int[subtableCount];
            SfntExtensionLookup[] extensions = lookupType == 9 ?
                new SfntExtensionLookup[subtableCount] :
                new SfntExtensionLookup[0];
            int subtableIndex = 0;

            while (subtableIndex < subtableCount)
            {
                int offset = layoutLookup.GetSubtableTableOffset(
                    subtableIndex);

                if (lookupType == 9)
                {
                    SfntExtensionLookup extension =
                        SfntLayoutPrimitiveParser.ParseExtension(
                            tableData,
                            offset,
                            9,
                            limits);
                    int extensionType = extension.GetLookupType();

                    if (extensionType < 1 || extensionType > 8)
                    {
                        throw FontFormatException.Create(
                            "A GPOS extension lookup type is invalid.");
                    }

                    if (subtableIndex > 0 &&
                        extensionType != effectiveLookupType)
                    {
                        throw FontFormatException.Create(
                            "GPOS extension subtables have different lookup types.");
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

            SfntGposSingleAdjustment[] singles = effectiveLookupType == 1 ?
                new SfntGposSingleAdjustment[subtableCount] :
                new SfntGposSingleAdjustment[0];
            SfntGposPairAdjustment[] pairs = effectiveLookupType == 2 ?
                new SfntGposPairAdjustment[subtableCount] :
                new SfntGposPairAdjustment[0];
            SfntGposCursiveAdjustment[] cursives = effectiveLookupType == 3 ?
                new SfntGposCursiveAdjustment[subtableCount] :
                new SfntGposCursiveAdjustment[0];
            SfntGposMarkAttachment[] markBases = effectiveLookupType == 4 ?
                new SfntGposMarkAttachment[subtableCount] :
                new SfntGposMarkAttachment[0];
            SfntGposMarkLigatureAdjustment[] markLigatures =
                effectiveLookupType == 5 ?
                new SfntGposMarkLigatureAdjustment[subtableCount] :
                new SfntGposMarkLigatureAdjustment[0];
            SfntGposMarkAttachment[] markMarks = effectiveLookupType == 6 ?
                new SfntGposMarkAttachment[subtableCount] :
                new SfntGposMarkAttachment[0];
            SfntGsubContextSubstitution[] contexts =
                effectiveLookupType == 7 ?
                new SfntGsubContextSubstitution[subtableCount] :
                new SfntGsubContextSubstitution[0];
            SfntGsubChainedContextSubstitution[] chainedContexts =
                effectiveLookupType == 8 ?
                new SfntGsubChainedContextSubstitution[subtableCount] :
                new SfntGsubChainedContextSubstitution[0];
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
                    pairs[subtableIndex] = ParsePair(offset);
                }
                else if (effectiveLookupType == 3)
                {
                    cursives[subtableIndex] = ParseCursive(offset);
                }
                else if (effectiveLookupType == 4)
                {
                    markBases[subtableIndex] = ParseMarkAttachment(
                        offset,
                        "MarkBasePos");
                }
                else if (effectiveLookupType == 5)
                {
                    markLigatures[subtableIndex] =
                        ParseMarkLigature(offset);
                }
                else if (effectiveLookupType == 6)
                {
                    markMarks[subtableIndex] = ParseMarkAttachment(
                        offset,
                        "MarkMarkPos");
                }
                else if (effectiveLookupType == 7)
                {
                    contextParser.SetSharedParsingState(
                        lookupCount,
                        parsedEntries);
                    contexts[subtableIndex] =
                        contextParser.ParseContext(offset);
                    parsedEntries = contextParser.GetParsedEntryCount();
                }
                else if (effectiveLookupType == 8)
                {
                    contextParser.SetSharedParsingState(
                        lookupCount,
                        parsedEntries);
                    chainedContexts[subtableIndex] =
                        contextParser.ParseChainedContext(offset);
                    parsedEntries = contextParser.GetParsedEntryCount();
                }

                subtableIndex += 1;
            }

            lookups[lookupIndex] = new SfntGposLookup(
                layoutLookup,
                singles,
                pairs,
                cursives,
                markBases,
                markLigatures,
                markMarks,
                contexts,
                chainedContexts,
                extensions,
                effectiveLookupType);
            lookupIndex += 1;
        }

        return new SfntGposTable(layoutTable, lookups, limits);
    }

    SfntGposSingleAdjustment ParseSingle(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GPOS SinglePos header is truncated.");
        int format = reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int valueFormat = reader.ReadUInt16();
        int valueSize = SfntGposValueRecordParser.GetRecordSize(
            valueFormat);

        if (format == 1)
        {
            int headerLength = CheckedFontMath.AddOffset(6, valueSize);
            CreateReader(
                offset,
                headerLength,
                "A format 1 GPOS SinglePos ValueRecord is truncated.");
            SfntCoverageTable coverage = ParseCoverage(
                offset,
                coverageOffset,
                headerLength,
                "A GPOS SinglePos Coverage offset is invalid.");
            SfntGposValueRecord record = ParseValueRecord(
                CheckedFontMath.AddOffset(offset, 6),
                valueFormat,
                offset,
                headerLength);
            return new SfntGposSingleAdjustment(
                1,
                coverage,
                valueFormat,
                new SfntGposValueRecord[] { record });
        }

        if (format == 2)
        {
            reader = CreateReader(
                offset,
                8,
                "A format 2 GPOS SinglePos header is truncated.");
            reader.ReadUInt16();
            reader.ReadUInt16();
            reader.ReadUInt16();
            int valueCount = reader.ReadUInt16();
            AddEntries(valueCount);
            int valueArrayLength = CheckedFontMath.MultiplyCount(
                valueCount,
                valueSize);
            int headerLength = CheckedFontMath.AddOffset(
                8,
                valueArrayLength);
            CreateReader(
                offset,
                headerLength,
                "A format 2 GPOS SinglePos value array is truncated.");
            SfntCoverageTable coverage = ParseCoverage(
                offset,
                coverageOffset,
                headerLength,
                "A GPOS SinglePos Coverage offset is invalid.");
            ValidateCoverageCount(
                coverage,
                valueCount,
                "A GPOS SinglePos value count does not match its Coverage table.");
            SfntGposValueRecord[] records =
                new SfntGposValueRecord[valueCount];
            int recordOffset = CheckedFontMath.AddOffset(offset, 8);
            int index = 0;

            while (index < records.Length)
            {
                records[index] = ParseValueRecord(
                    recordOffset,
                    valueFormat,
                    offset,
                    headerLength);
                recordOffset = CheckedFontMath.AddOffset(
                    recordOffset,
                    valueSize);
                index += 1;
            }

            return new SfntGposSingleAdjustment(
                2,
                coverage,
                valueFormat,
                records);
        }

        throw FontFormatException.Create(
            "The GPOS SinglePos format is not supported.");
    }

    SfntGposPairAdjustment ParsePair(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GPOS PairPos subtable is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return ParsePairFormat1(offset);
        }

        if (format == 2)
        {
            return ParsePairFormat2(offset);
        }

        throw FontFormatException.Create(
            "The GPOS PairPos format is not supported.");
    }

    SfntGposCursiveAdjustment ParseCursive(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A GPOS CursivePos header is truncated.");
        int format = reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int entryExitCount = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GPOS CursivePos format is not supported.");
        }

        AddEntries(entryExitCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            entryExitCount,
            4);
        reader = CreateReader(
            offset,
            headerLength,
            "A GPOS CursivePos EntryExit array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntGposEntryExitRecord[] records =
            new SfntGposEntryExitRecord[entryExitCount];
        int recordIndex = 0;

        while (recordIndex < records.Length)
        {
            int entryAnchorOffset = reader.ReadUInt16();
            int exitAnchorOffset = reader.ReadUInt16();
            records[recordIndex] = new SfntGposEntryExitRecord(
                ParseOptionalAnchor(
                    offset,
                    entryAnchorOffset,
                    headerLength,
                    "A GPOS cursive entry-anchor offset is invalid."),
                ParseOptionalAnchor(
                    offset,
                    exitAnchorOffset,
                    headerLength,
                    "A GPOS cursive exit-anchor offset is invalid."));
            recordIndex += 1;
        }

        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A GPOS CursivePos Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            entryExitCount,
            "A GPOS CursivePos record count does not match its Coverage table.");
        return new SfntGposCursiveAdjustment(coverage, records);
    }

    SfntGposMarkAttachment ParseMarkAttachment(
        int offset,
        string tableName)
    {
        BigEndianReader reader = CreateReader(
            offset,
            12,
            "A GPOS mark attachment header is truncated.");
        int format = reader.ReadUInt16();
        int markCoverageOffset = reader.ReadUInt16();
        int baseCoverageOffset = reader.ReadUInt16();
        int markClassCount = reader.ReadUInt16();
        int markArrayOffset = reader.ReadUInt16();
        int baseArrayOffset = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GPOS " + tableName + " format is not supported.");
        }

        if (markClassCount == 0)
        {
            throw FontFormatException.Create(
                "A GPOS mark attachment has no mark classes.");
        }

        SfntCoverageTable markCoverage = ParseCoverage(
            offset,
            markCoverageOffset,
            12,
            "A GPOS mark Coverage offset is invalid.");
        SfntCoverageTable baseCoverage = ParseCoverage(
            offset,
            baseCoverageOffset,
            12,
            "A GPOS attachment-base Coverage offset is invalid.");
        SfntMarkArray markArray = ParseMarkArray(
            offset,
            markArrayOffset,
            markClassCount,
            markCoverage.GetCoveredGlyphCount());
        int baseArrayTableOffset = ResolveRequiredOffset(
            offset,
            baseArrayOffset,
            12,
            2,
            "A GPOS attachment-base array offset is invalid.");
        SfntGposAnchorMatrix baseAnchors = ParseAnchorMatrix(
            baseArrayTableOffset,
            markClassCount,
            baseCoverage.GetCoveredGlyphCount(),
            "A GPOS attachment-base");
        return new SfntGposMarkAttachment(
            markCoverage,
            baseCoverage,
            markClassCount,
            markArray,
            baseAnchors);
    }

    SfntGposMarkLigatureAdjustment ParseMarkLigature(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            12,
            "A GPOS MarkLigPos header is truncated.");
        int format = reader.ReadUInt16();
        int markCoverageOffset = reader.ReadUInt16();
        int ligatureCoverageOffset = reader.ReadUInt16();
        int markClassCount = reader.ReadUInt16();
        int markArrayOffset = reader.ReadUInt16();
        int ligatureArrayOffset = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GPOS MarkLigPos format is not supported.");
        }

        if (markClassCount == 0)
        {
            throw FontFormatException.Create(
                "A GPOS MarkLigPos subtable has no mark classes.");
        }

        SfntCoverageTable markCoverage = ParseCoverage(
            offset,
            markCoverageOffset,
            12,
            "A GPOS ligature-mark Coverage offset is invalid.");
        SfntCoverageTable ligatureCoverage = ParseCoverage(
            offset,
            ligatureCoverageOffset,
            12,
            "A GPOS ligature Coverage offset is invalid.");
        SfntMarkArray markArray = ParseMarkArray(
            offset,
            markArrayOffset,
            markClassCount,
            markCoverage.GetCoveredGlyphCount());
        int ligatureArrayTableOffset = ResolveRequiredOffset(
            offset,
            ligatureArrayOffset,
            12,
            2,
            "A GPOS LigatureArray offset is invalid.");
        SfntGposLigatureAttach[] ligatureAttachments =
            ParseLigatureArray(
                ligatureArrayTableOffset,
                markClassCount,
                ligatureCoverage.GetCoveredGlyphCount());
        return new SfntGposMarkLigatureAdjustment(
            markCoverage,
            ligatureCoverage,
            markClassCount,
            markArray,
            ligatureAttachments);
    }

    SfntMarkArray ParseMarkArray(
        int parentOffset,
        int relativeOffset,
        int markClassCount,
        int expectedMarkCount)
    {
        int markArrayTableOffset = ResolveRequiredOffset(
            parentOffset,
            relativeOffset,
            12,
            2,
            "A GPOS MarkArray offset is invalid.");
        SfntMarkArray markArray = SfntLayoutPrimitiveParser.ParseMarkArray(
            tableData,
            markArrayTableOffset,
            markClassCount,
            limits);
        ValidateMarkArray(
            markArray,
            expectedMarkCount);
        return markArray;
    }

    void ValidateMarkArray(
        SfntMarkArray markArray,
        int expectedMarkCount)
    {
        ValidateArrayCount(
            markArray.GetMarkCount(),
            expectedMarkCount,
            "A GPOS MarkArray count does not match its Coverage table.");
        AddEntries(markArray.GetMarkCount());
        int markIndex = 0;

        while (markIndex < markArray.GetMarkCount())
        {
            SfntGposAdjustmentValidator.ValidateAnchorVariationIndexes(
                markArray.GetMark(markIndex).GetAnchor(),
                variationStore);
            markIndex += 1;
        }
    }

    SfntGposAnchorMatrix ParseAnchorMatrix(
        int offset,
        int markClassCount,
        int expectedRecordCount,
        string description)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            description + " array is truncated.");
        int recordCount = reader.ReadUInt16();
        ValidateArrayCount(
            recordCount,
            expectedRecordCount,
            description +
            " array count does not match its Coverage table.");
        int anchorCount = CheckedFontMath.MultiplyCount(
            recordCount,
            markClassCount);
        AddEntries(anchorCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            anchorCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            description + " anchor-offset array is truncated.");
        reader.ReadUInt16();
        int[] anchorOffsets = ReadOffsets(reader, anchorCount);
        SfntLayoutAnchor?[] anchors = ParseAnchorOffsets(
            offset,
            anchorOffsets,
            headerLength,
            description + " anchor offset is invalid.");
        return new SfntGposAnchorMatrix(
            recordCount,
            markClassCount,
            anchors);
    }

    SfntGposLigatureAttach[] ParseLigatureArray(
        int offset,
        int markClassCount,
        int expectedLigatureCount)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GPOS LigatureArray is truncated.");
        int ligatureCount = reader.ReadUInt16();
        ValidateArrayCount(
            ligatureCount,
            expectedLigatureCount,
            "A GPOS LigatureArray count does not match its Coverage table.");
        AddEntries(ligatureCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            ligatureCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GPOS LigatureArray offset array is truncated.");
        reader.ReadUInt16();
        int[] attachmentOffsets = ReadOffsets(reader, ligatureCount);
        SfntGposLigatureAttach[] attachments =
            new SfntGposLigatureAttach[ligatureCount];
        int ligatureIndex = 0;

        while (ligatureIndex < attachments.Length)
        {
            int attachmentOffset = ResolveRequiredOffset(
                offset,
                attachmentOffsets[ligatureIndex],
                headerLength,
                2,
                "A GPOS LigatureAttach offset is invalid.");
            attachments[ligatureIndex] = ParseLigatureAttach(
                attachmentOffset,
                markClassCount);
            ligatureIndex += 1;
        }

        return attachments;
    }

    SfntGposLigatureAttach ParseLigatureAttach(
        int offset,
        int markClassCount)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GPOS LigatureAttach table is truncated.");
        int componentCount = reader.ReadUInt16();

        if (componentCount == 0)
        {
            throw FontFormatException.Create(
                "A GPOS LigatureAttach table has no components.");
        }

        int anchorCount = CheckedFontMath.MultiplyCount(
            componentCount,
            markClassCount);
        AddEntries(anchorCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            anchorCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GPOS LigatureAttach anchor-offset array is truncated.");
        reader.ReadUInt16();
        int[] anchorOffsets = ReadOffsets(reader, anchorCount);
        SfntLayoutAnchor?[] anchors = ParseAnchorOffsets(
            offset,
            anchorOffsets,
            headerLength,
            "A GPOS ligature anchor offset is invalid.");
        return new SfntGposLigatureAttach(
            componentCount,
            markClassCount,
            anchors);
    }

    SfntLayoutAnchor?[] ParseAnchorOffsets(
        int parentOffset,
        int[] anchorOffsets,
        int minimumRelativeOffset,
        string message)
    {
        SfntLayoutAnchor?[] anchors =
            new SfntLayoutAnchor[anchorOffsets.Length];
        int anchorIndex = 0;

        while (anchorIndex < anchors.Length)
        {
            anchors[anchorIndex] = ParseOptionalAnchor(
                parentOffset,
                anchorOffsets[anchorIndex],
                minimumRelativeOffset,
                message);
            anchorIndex += 1;
        }

        return anchors;
    }

    SfntLayoutAnchor? ParseOptionalAnchor(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        if (relativeOffset == 0)
        {
            return null;
        }

        int anchorOffset = ResolveRequiredOffset(
            parentOffset,
            relativeOffset,
            minimumRelativeOffset,
            2,
            message);
        SfntLayoutAnchor anchor = SfntLayoutPrimitiveParser.ParseAnchor(
            tableData,
            anchorOffset,
            limits);
        SfntGposAdjustmentValidator.ValidateAnchorVariationIndexes(
            anchor,
            variationStore);
        return anchor;
    }

    SfntGposPairAdjustment ParsePairFormat1(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            10,
            "A format 1 GPOS PairPos header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int firstValueFormat = reader.ReadUInt16();
        int secondValueFormat = reader.ReadUInt16();
        int pairSetCount = reader.ReadUInt16();
        SfntGposValueRecordParser.GetRecordSize(firstValueFormat);
        SfntGposValueRecordParser.GetRecordSize(secondValueFormat);
        AddEntries(pairSetCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            10,
            pairSetCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A format 1 GPOS PairPos pair-set array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] pairSetOffsets = ReadOffsets(reader, pairSetCount);
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 1 GPOS PairPos Coverage offset is invalid.");
        ValidateCoverageCount(
            coverage,
            pairSetCount,
            "A GPOS PairPos pair-set count does not match its Coverage table.");
        SfntGposPairSet[] pairSets = new SfntGposPairSet[pairSetCount];
        int index = 0;

        while (index < pairSets.Length)
        {
            int pairSetOffset = ResolveRequiredOffset(
                offset,
                pairSetOffsets[index],
                headerLength,
                2,
                "A GPOS PairSet offset is invalid.");
            pairSets[index] = ParsePairSet(
                pairSetOffset,
                firstValueFormat,
                secondValueFormat);
            index += 1;
        }

        return new SfntGposPairAdjustment(
            1,
            coverage,
            firstValueFormat,
            secondValueFormat,
            pairSets,
            null,
            null,
            0,
            0,
            new SfntGposClassPairRecord[0]);
    }

    SfntGposPairSet ParsePairSet(
        int offset,
        int firstValueFormat,
        int secondValueFormat)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GPOS PairSet header is truncated.");
        int pairValueCount = reader.ReadUInt16();
        AddEntries(pairValueCount);
        int firstValueSize = SfntGposValueRecordParser.GetRecordSize(
            firstValueFormat);
        int secondValueSize = SfntGposValueRecordParser.GetRecordSize(
            secondValueFormat);
        int recordSize = CheckedFontMath.AddOffset(
            2,
            CheckedFontMath.AddOffset(firstValueSize, secondValueSize));
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            pairValueCount,
            recordSize);
        CreateReader(
            offset,
            headerLength,
            "A GPOS PairSet value array is truncated.");
        SfntGposPairValueRecord[] records =
            new SfntGposPairValueRecord[pairValueCount];
        int recordOffset = CheckedFontMath.AddOffset(offset, 2);
        int previousSecondGlyphId = -1;
        int index = 0;

        while (index < records.Length)
        {
            reader = CreateReader(
                recordOffset,
                2,
                "A GPOS PairValue record is truncated.");
            int secondGlyphId = reader.ReadUInt16();
            ValidateGlyphId(secondGlyphId);

            if (secondGlyphId <= previousSecondGlyphId)
            {
                throw FontFormatException.Create(
                    "GPOS PairValue records are not strictly ordered.");
            }

            previousSecondGlyphId = secondGlyphId;
            int firstRecordOffset = CheckedFontMath.AddOffset(
                recordOffset,
                2);
            int secondRecordOffset = CheckedFontMath.AddOffset(
                firstRecordOffset,
                firstValueSize);
            SfntGposValueRecord firstRecord = ParseValueRecord(
                firstRecordOffset,
                firstValueFormat,
                offset,
                headerLength);
            SfntGposValueRecord secondRecord = ParseValueRecord(
                secondRecordOffset,
                secondValueFormat,
                offset,
                headerLength);
            records[index] = new SfntGposPairValueRecord(
                secondGlyphId,
                firstRecord,
                secondRecord);
            recordOffset = CheckedFontMath.AddOffset(
                recordOffset,
                recordSize);
            index += 1;
        }

        return new SfntGposPairSet(records);
    }

    SfntGposPairAdjustment ParsePairFormat2(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            16,
            "A format 2 GPOS PairPos header is truncated.");
        reader.ReadUInt16();
        int coverageOffset = reader.ReadUInt16();
        int firstValueFormat = reader.ReadUInt16();
        int secondValueFormat = reader.ReadUInt16();
        int firstClassOffset = reader.ReadUInt16();
        int secondClassOffset = reader.ReadUInt16();
        int firstClassCount = reader.ReadUInt16();
        int secondClassCount = reader.ReadUInt16();

        if (firstClassCount == 0 || secondClassCount == 0)
        {
            throw FontFormatException.Create(
                "A GPOS PairPos class matrix is empty.");
        }

        int firstValueSize = SfntGposValueRecordParser.GetRecordSize(
            firstValueFormat);
        int secondValueSize = SfntGposValueRecordParser.GetRecordSize(
            secondValueFormat);
        int classRecordSize = CheckedFontMath.AddOffset(
            firstValueSize,
            secondValueSize);
        int classRecordCount = CheckedFontMath.MultiplyCount(
            firstClassCount,
            secondClassCount);
        AddEntries(classRecordCount);
        int classArrayLength = CheckedFontMath.MultiplyCount(
            classRecordCount,
            classRecordSize);
        int headerLength = CheckedFontMath.AddOffset(
            16,
            classArrayLength);
        CreateReader(
            offset,
            headerLength,
            "A format 2 GPOS PairPos class matrix is truncated.");
        SfntCoverageTable coverage = ParseCoverage(
            offset,
            coverageOffset,
            headerLength,
            "A format 2 GPOS PairPos Coverage offset is invalid.");
        SfntClassDefinitionTable firstClassDefinition =
            ParseClassDefinition(
                offset,
                firstClassOffset,
                headerLength,
                "A GPOS PairPos first ClassDef offset is invalid.");
        SfntClassDefinitionTable secondClassDefinition =
            ParseClassDefinition(
                offset,
                secondClassOffset,
                headerLength,
                "A GPOS PairPos second ClassDef offset is invalid.");

        if (firstClassDefinition.GetMaximumClassValue() >= firstClassCount ||
            secondClassDefinition.GetMaximumClassValue() >= secondClassCount)
        {
            throw FontFormatException.Create(
                "A GPOS PairPos class exceeds its matrix bounds.");
        }

        SfntGposClassPairRecord[] records =
            new SfntGposClassPairRecord[classRecordCount];
        int recordOffset = CheckedFontMath.AddOffset(offset, 16);
        int index = 0;

        while (index < records.Length)
        {
            SfntGposValueRecord firstRecord = ParseValueRecord(
                recordOffset,
                firstValueFormat,
                offset,
                headerLength);
            recordOffset = CheckedFontMath.AddOffset(
                recordOffset,
                firstValueSize);
            SfntGposValueRecord secondRecord = ParseValueRecord(
                recordOffset,
                secondValueFormat,
                offset,
                headerLength);
            recordOffset = CheckedFontMath.AddOffset(
                recordOffset,
                secondValueSize);
            records[index] = new SfntGposClassPairRecord(
                firstRecord,
                secondRecord);
            index += 1;
        }

        return new SfntGposPairAdjustment(
            2,
            coverage,
            firstValueFormat,
            secondValueFormat,
            new SfntGposPairSet[0],
            firstClassDefinition,
            secondClassDefinition,
            firstClassCount,
            secondClassCount,
            records);
    }

    SfntGposValueRecord ParseValueRecord(
        int recordOffset,
        int valueFormat,
        int parentOffset,
        int minimumAdjustmentOffset)
    {
        SfntGposValueRecord record = SfntGposValueRecordParser.Parse(
            tableData,
            recordOffset,
            valueFormat,
            parentOffset,
            minimumAdjustmentOffset,
            limits);
        record.ValidateVariationIndexes(variationStore);
        return record;
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

    SfntClassDefinitionTable ParseClassDefinition(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        int classOffset = ResolveRequiredOffset(
            parentOffset,
            relativeOffset,
            minimumRelativeOffset,
            2,
            message);
        return SfntLayoutGlyphClassParser.ParseClassDefinition(
            tableData,
            classOffset,
            glyphCount,
            limits);
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

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw FontFormatException.Create(
                "A GPOS glyph ID is invalid.");
        }
    }

    static void ValidateCoverageCount(
        SfntCoverageTable coverage,
        int expectedCount,
        string message)
    {
        if (coverage.GetCoveredGlyphCount() != expectedCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    static void ValidateArrayCount(
        int actualCount,
        int expectedCount,
        string message)
    {
        if (actualCount != expectedCount)
        {
            throw FontFormatException.Create(message);
        }
    }

    void AddEntries(int count)
    {
        if (count > limits.GetMaxCoverageEntries() - parsedEntries)
        {
            throw FontLimitException.Create(
                "GPOS data exceeds the configured coverage-entry limit.");
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

internal static class SfntGposTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Gpos,
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

    internal static SfntGposTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntGposTable)view.GetParsedData();
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
                "The GPOS table requires a supported maxp table.");
        }

        SfntGdefTable? gdef = SfntGdefTableParser.FindTable(cache);
        SfntItemVariationStore? variationStore = gdef == null ? null :
            gdef.GetItemVariationStore();
        FontDataSlice tableData = table.GetDataSlice();
        SfntLayoutTable layoutTable = SfntLayoutTableParser.Parse(
            tableData,
            limits);
        return new SfntGposParserContext(
            tableData,
            maxp.GetGlyphCount(),
            limits,
            variationStore).Parse(layoutTable);
    }
}
