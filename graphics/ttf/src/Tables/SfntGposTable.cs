namespace ttf;

internal class SfntGposSingleAdjustment
{
    int format;
    SfntCoverageTable coverage;
    int valueFormat;
    SfntGposValueRecord[] valueRecords;

    internal SfntGposSingleAdjustment(
        int format,
        SfntCoverageTable coverage,
        int valueFormat,
        SfntGposValueRecord[] valueRecords)
    {
        this.format = format;
        this.coverage = coverage;
        this.valueFormat = valueFormat;
        this.valueRecords = valueRecords;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetValueFormat()
    {
        return valueFormat;
    }

    internal int GetValueRecordCount()
    {
        return valueRecords.Length;
    }

    internal SfntGposValueRecord GetValueRecord(int index)
    {
        if (index < 0 || index >= valueRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return valueRecords[index];
    }

    internal SfntGposValueRecord? FindValueRecord(int glyphId)
    {
        int coverageIndex = coverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return format == 1 ? valueRecords[0] :
            valueRecords[coverageIndex];
    }
}

internal class SfntGposPairValueRecord
{
    int secondGlyphId;
    SfntGposValueRecord firstValueRecord;
    SfntGposValueRecord secondValueRecord;

    internal SfntGposPairValueRecord(
        int secondGlyphId,
        SfntGposValueRecord firstValueRecord,
        SfntGposValueRecord secondValueRecord)
    {
        this.secondGlyphId = secondGlyphId;
        this.firstValueRecord = firstValueRecord;
        this.secondValueRecord = secondValueRecord;
    }

    internal int GetSecondGlyphId()
    {
        return secondGlyphId;
    }

    internal SfntGposValueRecord GetFirstValueRecord()
    {
        return firstValueRecord;
    }

    internal SfntGposValueRecord GetSecondValueRecord()
    {
        return secondValueRecord;
    }
}

internal class SfntGposPairSet
{
    SfntGposPairValueRecord[] records;

    internal SfntGposPairSet(SfntGposPairValueRecord[] records)
    {
        this.records = records;
    }

    internal int GetPairValueCount()
    {
        return records.Length;
    }

    internal SfntGposPairValueRecord GetPairValue(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }

    internal SfntGposPairValueRecord? FindPairValue(int secondGlyphId)
    {
        int minimum = 0;
        int maximum = records.Length - 1;

        while (minimum <= maximum)
        {
            int middle = minimum + (maximum - minimum) / 2;
            SfntGposPairValueRecord record = records[middle];
            int candidate = record.GetSecondGlyphId();

            if (candidate == secondGlyphId)
            {
                return record;
            }

            if (candidate < secondGlyphId)
            {
                minimum = middle + 1;
            }
            else
            {
                maximum = middle - 1;
            }
        }

        return null;
    }
}

internal class SfntGposClassPairRecord
{
    SfntGposValueRecord firstValueRecord;
    SfntGposValueRecord secondValueRecord;

    internal SfntGposClassPairRecord(
        SfntGposValueRecord firstValueRecord,
        SfntGposValueRecord secondValueRecord)
    {
        this.firstValueRecord = firstValueRecord;
        this.secondValueRecord = secondValueRecord;
    }

    internal SfntGposValueRecord GetFirstValueRecord()
    {
        return firstValueRecord;
    }

    internal SfntGposValueRecord GetSecondValueRecord()
    {
        return secondValueRecord;
    }
}

internal class SfntGposPairAdjustment
{
    int format;
    SfntCoverageTable coverage;
    int firstValueFormat;
    int secondValueFormat;
    SfntGposPairSet[] pairSets;
    SfntClassDefinitionTable? firstClassDefinition;
    SfntClassDefinitionTable? secondClassDefinition;
    int firstClassCount;
    int secondClassCount;
    SfntGposClassPairRecord[] classPairRecords;

    internal SfntGposPairAdjustment(
        int format,
        SfntCoverageTable coverage,
        int firstValueFormat,
        int secondValueFormat,
        SfntGposPairSet[] pairSets,
        SfntClassDefinitionTable? firstClassDefinition,
        SfntClassDefinitionTable? secondClassDefinition,
        int firstClassCount,
        int secondClassCount,
        SfntGposClassPairRecord[] classPairRecords)
    {
        this.format = format;
        this.coverage = coverage;
        this.firstValueFormat = firstValueFormat;
        this.secondValueFormat = secondValueFormat;
        this.pairSets = pairSets;
        this.firstClassDefinition = firstClassDefinition;
        this.secondClassDefinition = secondClassDefinition;
        this.firstClassCount = firstClassCount;
        this.secondClassCount = secondClassCount;
        this.classPairRecords = classPairRecords;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetFirstValueFormat()
    {
        return firstValueFormat;
    }

    internal int GetSecondValueFormat()
    {
        return secondValueFormat;
    }

    internal int GetPairSetCount()
    {
        return pairSets.Length;
    }

    internal SfntGposPairSet GetPairSet(int index)
    {
        if (index < 0 || index >= pairSets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return pairSets[index];
    }

    internal SfntClassDefinitionTable? GetFirstClassDefinition()
    {
        return firstClassDefinition;
    }

    internal SfntClassDefinitionTable? GetSecondClassDefinition()
    {
        return secondClassDefinition;
    }

    internal int GetFirstClassCount()
    {
        return firstClassCount;
    }

    internal int GetSecondClassCount()
    {
        return secondClassCount;
    }

    internal SfntGposClassPairRecord GetClassPairRecord(
        int firstClass,
        int secondClass)
    {
        if (firstClass < 0 || firstClass >= firstClassCount)
        {
            throw new System.ArgumentOutOfRangeException("firstClass");
        }

        if (secondClass < 0 || secondClass >= secondClassCount)
        {
            throw new System.ArgumentOutOfRangeException("secondClass");
        }

        return classPairRecords[
            firstClass * secondClassCount + secondClass];
    }

    internal SfntGposPairValueRecord? FindPairValue(
        int firstGlyphId,
        int secondGlyphId)
    {
        int coverageIndex = coverage.GetCoverageIndex(firstGlyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        if (format == 1)
        {
            return pairSets[coverageIndex].FindPairValue(secondGlyphId);
        }

        System.ArgumentNullException.ThrowIfNull(firstClassDefinition);
        int firstClass = firstClassDefinition.GetClassForGlyph(firstGlyphId);
        System.ArgumentNullException.ThrowIfNull(secondClassDefinition);
        int secondClass = secondClassDefinition.GetClassForGlyph(
            secondGlyphId);
        SfntGposClassPairRecord classRecord = GetClassPairRecord(
            firstClass,
            secondClass);
        return new SfntGposPairValueRecord(
            secondGlyphId,
            classRecord.GetFirstValueRecord(),
            classRecord.GetSecondValueRecord());
    }
}

internal class SfntGposEntryExitRecord
{
    SfntLayoutAnchor? entryAnchor;
    SfntLayoutAnchor? exitAnchor;

    internal SfntGposEntryExitRecord(
        SfntLayoutAnchor? entryAnchor,
        SfntLayoutAnchor? exitAnchor)
    {
        this.entryAnchor = entryAnchor;
        this.exitAnchor = exitAnchor;
    }

    internal bool HasEntryAnchor()
    {
        return entryAnchor != null;
    }

    internal SfntLayoutAnchor? GetEntryAnchor()
    {
        return entryAnchor;
    }

    internal bool HasExitAnchor()
    {
        return exitAnchor != null;
    }

    internal SfntLayoutAnchor? GetExitAnchor()
    {
        return exitAnchor;
    }
}

internal class SfntGposCursiveAdjustment
{
    SfntCoverageTable coverage;
    SfntGposEntryExitRecord[] entryExitRecords;

    internal SfntGposCursiveAdjustment(
        SfntCoverageTable coverage,
        SfntGposEntryExitRecord[] entryExitRecords)
    {
        this.coverage = coverage;
        this.entryExitRecords = entryExitRecords;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetEntryExitCount()
    {
        return entryExitRecords.Length;
    }

    internal SfntGposEntryExitRecord GetEntryExitRecord(int index)
    {
        if (index < 0 || index >= entryExitRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entryExitRecords[index];
    }

    internal SfntGposEntryExitRecord? FindEntryExitRecord(int glyphId)
    {
        int coverageIndex = coverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return null;
        }

        return entryExitRecords[coverageIndex];
    }
}

internal class SfntGposLookup
{
    SfntLayoutLookup layoutLookup;
    SfntGposSingleAdjustment[] singleAdjustments;
    SfntGposPairAdjustment[] pairAdjustments;
    SfntGposCursiveAdjustment[] cursiveAdjustments;
    SfntGposMarkAttachment[] markBaseAdjustments;
    SfntGposMarkLigatureAdjustment[] markLigatureAdjustments;
    SfntGposMarkAttachment[] markMarkAdjustments;
    SfntGsubContextSubstitution[] contextAdjustments;
    SfntGsubChainedContextSubstitution[] chainedContextAdjustments;
    SfntExtensionLookup[] extensions;
    int lookupType;

    internal SfntGposLookup(
        SfntLayoutLookup layoutLookup,
        SfntGposSingleAdjustment[] singleAdjustments,
        SfntGposPairAdjustment[] pairAdjustments,
        SfntGposCursiveAdjustment[] cursiveAdjustments,
        SfntGposMarkAttachment[] markBaseAdjustments,
        SfntGposMarkLigatureAdjustment[] markLigatureAdjustments,
        SfntGposMarkAttachment[] markMarkAdjustments,
        SfntGsubContextSubstitution[] contextAdjustments,
        SfntGsubChainedContextSubstitution[] chainedContextAdjustments,
        SfntExtensionLookup[] extensions,
        int lookupType)
    {
        this.layoutLookup = layoutLookup;
        this.singleAdjustments = singleAdjustments;
        this.pairAdjustments = pairAdjustments;
        this.cursiveAdjustments = cursiveAdjustments;
        this.markBaseAdjustments = markBaseAdjustments;
        this.markLigatureAdjustments = markLigatureAdjustments;
        this.markMarkAdjustments = markMarkAdjustments;
        this.contextAdjustments = contextAdjustments;
        this.chainedContextAdjustments = chainedContextAdjustments;
        this.extensions = extensions;
        this.lookupType = lookupType;
    }

    internal SfntLayoutLookup GetLayoutLookup()
    {
        return layoutLookup;
    }

    internal int GetSubtableCount()
    {
        return layoutLookup.GetSubtableCount();
    }

    internal SfntGposSingleAdjustment GetSingleAdjustment(int index)
    {
        return singleAdjustments[index];
    }

    internal SfntGposPairAdjustment GetPairAdjustment(int index)
    {
        return pairAdjustments[index];
    }

    internal SfntGposCursiveAdjustment GetCursiveAdjustment(int index)
    {
        return cursiveAdjustments[index];
    }

    internal SfntGposMarkAttachment GetMarkBaseAdjustment(int index)
    {
        return markBaseAdjustments[index];
    }

    internal SfntGposMarkLigatureAdjustment GetMarkLigatureAdjustment(
        int index)
    {
        return markLigatureAdjustments[index];
    }

    internal SfntGposMarkAttachment GetMarkMarkAdjustment(int index)
    {
        return markMarkAdjustments[index];
    }

    internal SfntGsubContextSubstitution GetContextAdjustment(int index)
    {
        return contextAdjustments[index];
    }

    internal SfntGsubChainedContextSubstitution GetChainedContextAdjustment(
        int index)
    {
        return chainedContextAdjustments[index];
    }

    internal SfntExtensionLookup GetExtension(int index)
    {
        return extensions[index];
    }

    internal int GetLookupType()
    {
        return lookupType;
    }
}

internal class SfntGposTable
{
    SfntLayoutTable layoutTable;
    SfntGposLookup[] lookups;
    FontLimits limits;

    internal SfntGposTable(
        SfntLayoutTable layoutTable,
        SfntGposLookup[] lookups,
        FontLimits limits)
    {
        this.layoutTable = layoutTable;
        this.lookups = lookups;
        this.limits = limits;
    }

    internal SfntLayoutTable GetLayoutTable()
    {
        return layoutTable;
    }

    internal int GetLookupCount()
    {
        return lookups.Length;
    }

    internal SfntGposLookup GetLookup(int index)
    {
        if (index < 0 || index >= lookups.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return lookups[index];
    }

    internal FontLimits GetLimits()
    {
        return limits;
    }
}
