namespace ttf;

internal class SfntGsubSingleSubstitution
{
    int format;
    SfntCoverageTable coverage;
    int deltaGlyphId;
    int[] substituteGlyphIds;

    internal SfntGsubSingleSubstitution(
        int format,
        SfntCoverageTable coverage,
        int deltaGlyphId,
        int[] substituteGlyphIds)
    {
        this.format = format;
        this.coverage = coverage;
        this.deltaGlyphId = deltaGlyphId;
        this.substituteGlyphIds = substituteGlyphIds;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetSubstituteGlyphId(int glyphId)
    {
        int coverageIndex = coverage.GetCoverageIndex(glyphId);

        if (coverageIndex < 0)
        {
            return -1;
        }

        if (format == 1)
        {
            return glyphId + deltaGlyphId & 0xFFFF;
        }

        return substituteGlyphIds[coverageIndex];
    }
}

internal class SfntGsubGlyphSequence
{
    int[] glyphIds;

    internal SfntGsubGlyphSequence(int[] glyphIds)
    {
        this.glyphIds = glyphIds;
    }

    internal int GetGlyphCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return glyphIds[index];
    }

    internal int[] CopyGlyphIds()
    {
        int[] copy = new int[glyphIds.Length];
        System.Array.Copy(glyphIds, copy, glyphIds.Length);
        return copy;
    }
}

internal class SfntGsubMultipleSubstitution
{
    SfntCoverageTable coverage;
    SfntGsubGlyphSequence[] sequences;

    internal SfntGsubMultipleSubstitution(
        SfntCoverageTable coverage,
        SfntGsubGlyphSequence[] sequences)
    {
        this.coverage = coverage;
        this.sequences = sequences;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetSequenceCount()
    {
        return sequences.Length;
    }

    internal SfntGsubGlyphSequence GetSequence(int index)
    {
        if (index < 0 || index >= sequences.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return sequences[index];
    }

    internal SfntGsubGlyphSequence? FindSequence(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : sequences[index];
    }
}

internal class SfntGsubAlternateSet
{
    int[] glyphIds;

    internal SfntGsubAlternateSet(int[] glyphIds)
    {
        this.glyphIds = glyphIds;
    }

    internal int GetGlyphCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return glyphIds[index];
    }
}

internal class SfntGsubAlternateSubstitution
{
    SfntCoverageTable coverage;
    SfntGsubAlternateSet[] alternateSets;

    internal SfntGsubAlternateSubstitution(
        SfntCoverageTable coverage,
        SfntGsubAlternateSet[] alternateSets)
    {
        this.coverage = coverage;
        this.alternateSets = alternateSets;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetAlternateSetCount()
    {
        return alternateSets.Length;
    }

    internal SfntGsubAlternateSet GetAlternateSet(int index)
    {
        if (index < 0 || index >= alternateSets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return alternateSets[index];
    }

    internal SfntGsubAlternateSet? FindAlternateSet(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : alternateSets[index];
    }
}

internal class SfntGsubLigature
{
    int ligatureGlyphId;
    int[] componentGlyphIds;

    internal SfntGsubLigature(
        int ligatureGlyphId,
        int[] componentGlyphIds)
    {
        this.ligatureGlyphId = ligatureGlyphId;
        this.componentGlyphIds = componentGlyphIds;
    }

    internal int GetLigatureGlyphId()
    {
        return ligatureGlyphId;
    }

    internal int GetComponentCount()
    {
        return componentGlyphIds.Length + 1;
    }

    internal int GetComponentGlyphId(int index)
    {
        if (index < 0 || index >= componentGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return componentGlyphIds[index];
    }
}

internal class SfntGsubLigatureSet
{
    SfntGsubLigature[] ligatures;

    internal SfntGsubLigatureSet(SfntGsubLigature[] ligatures)
    {
        this.ligatures = ligatures;
    }

    internal int GetLigatureCount()
    {
        return ligatures.Length;
    }

    internal SfntGsubLigature GetLigature(int index)
    {
        if (index < 0 || index >= ligatures.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return ligatures[index];
    }
}

internal class SfntGsubLigatureSubstitution
{
    SfntCoverageTable coverage;
    SfntGsubLigatureSet[] ligatureSets;

    internal SfntGsubLigatureSubstitution(
        SfntCoverageTable coverage,
        SfntGsubLigatureSet[] ligatureSets)
    {
        this.coverage = coverage;
        this.ligatureSets = ligatureSets;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetLigatureSetCount()
    {
        return ligatureSets.Length;
    }

    internal SfntGsubLigatureSet GetLigatureSet(int index)
    {
        if (index < 0 || index >= ligatureSets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return ligatureSets[index];
    }

    internal SfntGsubLigatureSet? FindLigatureSet(int glyphId)
    {
        int index = coverage.GetCoverageIndex(glyphId);
        return index < 0 ? null : ligatureSets[index];
    }
}

internal class SfntGsubContextRule
{
    int[] inputValues;
    SfntSequenceLookupRecords lookupRecords;

    internal SfntGsubContextRule(
        int[] inputValues,
        SfntSequenceLookupRecords lookupRecords)
    {
        this.inputValues = inputValues;
        this.lookupRecords = lookupRecords;
    }

    internal int GetGlyphCount()
    {
        return inputValues.Length + 1;
    }

    internal int GetInputValue(int index)
    {
        if (index < 0 || index >= inputValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return inputValues[index];
    }

    internal SfntSequenceLookupRecords GetLookupRecords()
    {
        return lookupRecords;
    }
}

internal class SfntGsubContextRuleSet
{
    SfntGsubContextRule[] rules;

    internal SfntGsubContextRuleSet(SfntGsubContextRule[] rules)
    {
        this.rules = rules;
    }

    internal int GetRuleCount()
    {
        return rules.Length;
    }

    internal SfntGsubContextRule GetRule(int index)
    {
        if (index < 0 || index >= rules.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return rules[index];
    }
}

internal class SfntGsubContextSubstitution
{
    int format;
    SfntCoverageTable? coverage;
    SfntClassDefinitionTable? classDefinition;
    SfntGsubContextRuleSet[] ruleSets;
    SfntCoverageTable[] inputCoverages;
    SfntSequenceLookupRecords? lookupRecords;

    internal SfntGsubContextSubstitution(
        int format,
        SfntCoverageTable? coverage,
        SfntClassDefinitionTable? classDefinition,
        SfntGsubContextRuleSet[] ruleSets,
        SfntCoverageTable[] inputCoverages,
        SfntSequenceLookupRecords? lookupRecords)
    {
        this.format = format;
        this.coverage = coverage;
        this.classDefinition = classDefinition;
        this.ruleSets = ruleSets;
        this.inputCoverages = inputCoverages;
        this.lookupRecords = lookupRecords;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal SfntCoverageTable? GetCoverage()
    {
        return coverage;
    }

    internal SfntClassDefinitionTable? GetClassDefinition()
    {
        return classDefinition;
    }

    internal int GetRuleSetCount()
    {
        return ruleSets.Length;
    }

    internal SfntGsubContextRuleSet GetRuleSet(int index)
    {
        if (index < 0 || index >= ruleSets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return ruleSets[index];
    }

    internal int GetInputCoverageCount()
    {
        return inputCoverages.Length;
    }

    internal SfntCoverageTable GetInputCoverage(int index)
    {
        if (index < 0 || index >= inputCoverages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return inputCoverages[index];
    }

    internal SfntSequenceLookupRecords? GetLookupRecords()
    {
        return lookupRecords;
    }
}

internal class SfntGsubChainedRule
{
    int[] backtrackValues;
    int[] inputValues;
    int[] lookaheadValues;
    SfntSequenceLookupRecords lookupRecords;

    internal SfntGsubChainedRule(
        int[] backtrackValues,
        int[] inputValues,
        int[] lookaheadValues,
        SfntSequenceLookupRecords lookupRecords)
    {
        this.backtrackValues = backtrackValues;
        this.inputValues = inputValues;
        this.lookaheadValues = lookaheadValues;
        this.lookupRecords = lookupRecords;
    }

    internal int GetBacktrackCount()
    {
        return backtrackValues.Length;
    }

    internal int GetBacktrackValue(int index)
    {
        return backtrackValues[index];
    }

    internal int GetInputGlyphCount()
    {
        return inputValues.Length + 1;
    }

    internal int GetInputValue(int index)
    {
        return inputValues[index];
    }

    internal int GetLookaheadCount()
    {
        return lookaheadValues.Length;
    }

    internal int GetLookaheadValue(int index)
    {
        return lookaheadValues[index];
    }

    internal SfntSequenceLookupRecords GetLookupRecords()
    {
        return lookupRecords;
    }
}

internal class SfntGsubChainedRuleSet
{
    SfntGsubChainedRule[] rules;

    internal SfntGsubChainedRuleSet(SfntGsubChainedRule[] rules)
    {
        this.rules = rules;
    }

    internal int GetRuleCount()
    {
        return rules.Length;
    }

    internal SfntGsubChainedRule GetRule(int index)
    {
        return rules[index];
    }
}

internal class SfntGsubChainedContextSubstitution
{
    int format;
    SfntCoverageTable? coverage;
    SfntClassDefinitionTable? backtrackClassDefinition;
    SfntClassDefinitionTable? inputClassDefinition;
    SfntClassDefinitionTable? lookaheadClassDefinition;
    SfntGsubChainedRuleSet[] ruleSets;
    SfntCoverageTable[] backtrackCoverages;
    SfntCoverageTable[] inputCoverages;
    SfntCoverageTable[] lookaheadCoverages;
    SfntSequenceLookupRecords? lookupRecords;

    internal SfntGsubChainedContextSubstitution(
        int format,
        SfntCoverageTable? coverage,
        SfntClassDefinitionTable? backtrackClassDefinition,
        SfntClassDefinitionTable? inputClassDefinition,
        SfntClassDefinitionTable? lookaheadClassDefinition,
        SfntGsubChainedRuleSet[] ruleSets,
        SfntCoverageTable[] backtrackCoverages,
        SfntCoverageTable[] inputCoverages,
        SfntCoverageTable[] lookaheadCoverages,
        SfntSequenceLookupRecords? lookupRecords)
    {
        this.format = format;
        this.coverage = coverage;
        this.backtrackClassDefinition = backtrackClassDefinition;
        this.inputClassDefinition = inputClassDefinition;
        this.lookaheadClassDefinition = lookaheadClassDefinition;
        this.ruleSets = ruleSets;
        this.backtrackCoverages = backtrackCoverages;
        this.inputCoverages = inputCoverages;
        this.lookaheadCoverages = lookaheadCoverages;
        this.lookupRecords = lookupRecords;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal SfntCoverageTable? GetCoverage()
    {
        return coverage;
    }

    internal SfntClassDefinitionTable? GetBacktrackClassDefinition()
    {
        return backtrackClassDefinition;
    }

    internal SfntClassDefinitionTable? GetInputClassDefinition()
    {
        return inputClassDefinition;
    }

    internal SfntClassDefinitionTable? GetLookaheadClassDefinition()
    {
        return lookaheadClassDefinition;
    }

    internal int GetRuleSetCount()
    {
        return ruleSets.Length;
    }

    internal SfntGsubChainedRuleSet GetRuleSet(int index)
    {
        return ruleSets[index];
    }

    internal int GetBacktrackCoverageCount()
    {
        return backtrackCoverages.Length;
    }

    internal SfntCoverageTable GetBacktrackCoverage(int index)
    {
        return backtrackCoverages[index];
    }

    internal int GetInputCoverageCount()
    {
        return inputCoverages.Length;
    }

    internal SfntCoverageTable GetInputCoverage(int index)
    {
        return inputCoverages[index];
    }

    internal int GetLookaheadCoverageCount()
    {
        return lookaheadCoverages.Length;
    }

    internal SfntCoverageTable GetLookaheadCoverage(int index)
    {
        return lookaheadCoverages[index];
    }

    internal SfntSequenceLookupRecords? GetLookupRecords()
    {
        return lookupRecords;
    }
}

internal class SfntGsubReverseSubstitution
{
    SfntCoverageTable coverage;
    SfntCoverageTable[] backtrackCoverages;
    SfntCoverageTable[] lookaheadCoverages;
    int[] substituteGlyphIds;

    internal SfntGsubReverseSubstitution(
        SfntCoverageTable coverage,
        SfntCoverageTable[] backtrackCoverages,
        SfntCoverageTable[] lookaheadCoverages,
        int[] substituteGlyphIds)
    {
        this.coverage = coverage;
        this.backtrackCoverages = backtrackCoverages;
        this.lookaheadCoverages = lookaheadCoverages;
        this.substituteGlyphIds = substituteGlyphIds;
    }

    internal SfntCoverageTable GetCoverage()
    {
        return coverage;
    }

    internal int GetBacktrackCoverageCount()
    {
        return backtrackCoverages.Length;
    }

    internal SfntCoverageTable GetBacktrackCoverage(int index)
    {
        return backtrackCoverages[index];
    }

    internal int GetLookaheadCoverageCount()
    {
        return lookaheadCoverages.Length;
    }

    internal SfntCoverageTable GetLookaheadCoverage(int index)
    {
        return lookaheadCoverages[index];
    }

    internal int GetSubstituteGlyphId(int glyphId)
    {
        int coverageIndex = coverage.GetCoverageIndex(glyphId);
        return coverageIndex < 0 ? -1 : substituteGlyphIds[coverageIndex];
    }
}

internal class SfntGsubLookup
{
    SfntLayoutLookup layoutLookup;
    SfntGsubSingleSubstitution[] singleSubstitutions;
    SfntGsubMultipleSubstitution[] multipleSubstitutions;
    SfntGsubAlternateSubstitution[] alternateSubstitutions;
    SfntGsubLigatureSubstitution[] ligatureSubstitutions;
    SfntGsubContextSubstitution[] contextSubstitutions;
    SfntGsubChainedContextSubstitution[] chainedContextSubstitutions;
    SfntExtensionLookup[] extensions;
    SfntGsubReverseSubstitution[] reverseSubstitutions;
    int lookupType;

    internal SfntGsubLookup(
        SfntLayoutLookup layoutLookup,
        SfntGsubSingleSubstitution[] singleSubstitutions,
        SfntGsubMultipleSubstitution[] multipleSubstitutions,
        SfntGsubAlternateSubstitution[] alternateSubstitutions,
        SfntGsubLigatureSubstitution[] ligatureSubstitutions,
        SfntGsubContextSubstitution[] contextSubstitutions,
        SfntGsubChainedContextSubstitution[] chainedContextSubstitutions,
        SfntExtensionLookup[] extensions,
        SfntGsubReverseSubstitution[] reverseSubstitutions,
        int lookupType)
    {
        this.layoutLookup = layoutLookup;
        this.singleSubstitutions = singleSubstitutions;
        this.multipleSubstitutions = multipleSubstitutions;
        this.alternateSubstitutions = alternateSubstitutions;
        this.ligatureSubstitutions = ligatureSubstitutions;
        this.contextSubstitutions = contextSubstitutions;
        this.chainedContextSubstitutions = chainedContextSubstitutions;
        this.extensions = extensions;
        this.reverseSubstitutions = reverseSubstitutions;
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

    internal SfntGsubSingleSubstitution GetSingleSubstitution(int index)
    {
        return singleSubstitutions[index];
    }

    internal SfntGsubMultipleSubstitution GetMultipleSubstitution(int index)
    {
        return multipleSubstitutions[index];
    }

    internal SfntGsubAlternateSubstitution GetAlternateSubstitution(int index)
    {
        return alternateSubstitutions[index];
    }

    internal SfntGsubLigatureSubstitution GetLigatureSubstitution(int index)
    {
        return ligatureSubstitutions[index];
    }

    internal SfntGsubContextSubstitution GetContextSubstitution(int index)
    {
        return contextSubstitutions[index];
    }

    internal SfntGsubChainedContextSubstitution
        GetChainedContextSubstitution(int index)
    {
        return chainedContextSubstitutions[index];
    }

    internal SfntExtensionLookup GetExtension(int index)
    {
        return extensions[index];
    }

    internal SfntGsubReverseSubstitution GetReverseSubstitution(int index)
    {
        return reverseSubstitutions[index];
    }

    internal int GetLookupType()
    {
        return lookupType;
    }
}

internal class SfntGsubTable
{
    SfntLayoutTable layoutTable;
    SfntGsubLookup[] lookups;
    FontLimits limits;

    internal SfntGsubTable(
        SfntLayoutTable layoutTable,
        SfntGsubLookup[] lookups,
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

    internal FontLimits GetLimits()
    {
        return limits;
    }

    internal SfntGsubLookup GetLookup(int index)
    {
        if (index < 0 || index >= lookups.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return lookups[index];
    }
}
