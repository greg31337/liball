namespace ttf;

internal class SfntGsubLigatureMatch
{
    SfntGsubLigature ligature;
    int[] matchedIndices;

    internal SfntGsubLigatureMatch(
        SfntGsubLigature ligature,
        int[] matchedIndices)
    {
        this.ligature = ligature;
        this.matchedIndices = matchedIndices;
    }

    internal SfntGsubLigature GetLigature()
    {
        return ligature;
    }

    internal int[] GetMatchedIndices()
    {
        return matchedIndices;
    }
}

internal class SfntGsubContextMatch
{
    int[] matchedIndices;
    SfntSequenceLookupRecords? lookupRecords;

    internal SfntGsubContextMatch(
        int[] matchedIndices,
        SfntSequenceLookupRecords? lookupRecords)
    {
        this.matchedIndices = matchedIndices;
        this.lookupRecords = lookupRecords;
    }

    internal int[] GetMatchedIndices()
    {
        return matchedIndices;
    }

    internal SfntSequenceLookupRecords? GetLookupRecords()
    {
        return lookupRecords;
    }
}

internal class SfntGsubLookupApplication
{
    bool applied;
    int[] indexMap;
    int[] boundaryMap;
    int nextForwardIndex;
    int nextBackwardIndex;

    internal SfntGsubLookupApplication(
        bool applied,
        int[] indexMap,
        int[] boundaryMap,
        int nextForwardIndex,
        int nextBackwardIndex)
    {
        this.applied = applied;
        this.indexMap = indexMap;
        this.boundaryMap = boundaryMap;
        this.nextForwardIndex = nextForwardIndex;
        this.nextBackwardIndex = nextBackwardIndex;
    }

    internal bool WasApplied()
    {
        return applied;
    }

    internal int MapIndex(int oldIndex)
    {
        return indexMap[oldIndex];
    }

    internal int MapBoundary(int oldBoundary)
    {
        return boundaryMap[oldBoundary];
    }

    internal int GetNextForwardIndex()
    {
        return nextForwardIndex;
    }

    internal int GetNextBackwardIndex()
    {
        return nextBackwardIndex;
    }
}

internal static class SfntGsubExecutor
{
    internal static int ApplyLookup(
        SfntGsubTable table,
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        return ApplyLookup(
            table,
            lookupIndex,
            buffer,
            gdef,
            direction,
            alternateValue,
            SfntLayoutExecutionContext.CreateDefault(table.GetLimits()));
    }

    internal static int ApplyLookup(
        SfntGsubTable table,
        SfntLayoutLookupSelection selection,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntLayoutExecutionContext? executionContext)
    {
        if (selection == null)
        {
            throw new System.ArgumentNullException("selection");
        }

        return ApplyLookup(
            table,
            selection.GetLookupIndex(),
            buffer,
            gdef,
            direction,
            1,
            executionContext,
            selection);
    }

    internal static int ApplyLookup(
        SfntGsubTable table,
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        SfntLayoutExecutionContext executionContext)
    {
        return ApplyLookup(
            table,
            lookupIndex,
            buffer,
            gdef,
            direction,
            alternateValue,
            executionContext,
            null);
    }

    static int ApplyLookup(
        SfntGsubTable table,
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        SfntLayoutExecutionContext? executionContext,
        SfntLayoutLookupSelection? selection)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (lookupIndex < 0 || lookupIndex >= table.GetLookupCount())
        {
            throw new System.ArgumentOutOfRangeException("lookupIndex");
        }

        if (direction != SfntLayoutTraversalDirection.Forward &&
            direction != SfntLayoutTraversalDirection.Backward)
        {
            throw new System.ArgumentOutOfRangeException("direction");
        }

        if (alternateValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("alternateValue");
        }

        if (executionContext == null)
        {
            throw new System.ArgumentNullException("executionContext");
        }

        executionContext.CheckGlyphBuffer(buffer.GetCount());

        SfntGsubLookup lookup = table.GetLookup(lookupIndex);
        SfntLayoutLookup layoutLookup = lookup.GetLayoutLookup();
        SfntLookupGlyphFilter filter = new SfntLookupGlyphFilter(
            layoutLookup,
            gdef);
        int lookupType = lookup.GetLookupType();
        SfntLayoutTraversalDirection traversalDirection = lookupType == 8 ?
            GetOppositeDirection(direction) : direction;
        int step = traversalDirection == SfntLayoutTraversalDirection.Forward ?
            1 : -1;
        int index = traversalDirection == SfntLayoutTraversalDirection.Forward ?
            0 : buffer.GetCount() - 1;
        int applicationCount = 0;

        while (index >= 0 && index < buffer.GetCount())
        {
            executionContext.RecordOperation();
            bool applied = false;
            int selectedValue = GetSelectedValue(
                selection,
                index,
                alternateValue);

            if (selection != null && selectedValue == 0)
            {
                index += step;
                continue;
            }

            if (lookupType == 1)
            {
                applied = ApplySingleAt(lookup, buffer, index);
            }
            else if (lookupType == 2)
            {
                int replacementCount = ApplyMultipleAt(
                    lookup,
                    buffer,
                    index,
                    executionContext);
                applied = replacementCount > 0;

                if (applied &&
                    direction == SfntLayoutTraversalDirection.Forward)
                {
                    index += replacementCount;
                }
            }
            else if (lookupType == 3)
            {
                applied = ApplyAlternateAt(
                    lookup,
                    buffer,
                    index,
                    selectedValue);
            }
            else if (lookupType == 4)
            {
                SfntGsubLigatureMatch? match = FindLigatureMatch(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction);

                if (match != null)
                {
                    int[] matchedIndices = match.GetMatchedIndices();
                    int minimumIndex = FindMinimum(matchedIndices);
                    int maximumIndex = FindMaximum(matchedIndices);
                    int spanLength = maximumIndex - minimumIndex + 1;
                    int newSpanLength = spanLength -
                        matchedIndices.Length + 1;
                    buffer.ReplaceWithLigature(
                        matchedIndices,
                        match.GetLigature().GetLigatureGlyphId());
                    applied = true;
                    index = direction ==
                        SfntLayoutTraversalDirection.Forward ?
                        minimumIndex + newSpanLength : minimumIndex - 1;
                }
            }
            else if (lookupType == 5)
            {
                SfntGsubLookupApplication application = ApplyLookupAt(
                    table,
                    lookupIndex,
                    buffer,
                    index,
                    gdef,
                    direction,
                    selectedValue,
                    0,
                    executionContext);
                applied = application.WasApplied();

                if (applied)
                {
                    index = direction ==
                        SfntLayoutTraversalDirection.Forward ?
                        application.GetNextForwardIndex() :
                        application.GetNextBackwardIndex();
                }
            }
            else if (lookupType == 6)
            {
                SfntGsubLookupApplication application = ApplyLookupAt(
                    table,
                    lookupIndex,
                    buffer,
                    index,
                    gdef,
                    direction,
                    selectedValue,
                    0,
                    executionContext);
                applied = application.WasApplied();

                if (applied)
                {
                    index = direction ==
                        SfntLayoutTraversalDirection.Forward ?
                        application.GetNextForwardIndex() :
                        application.GetNextBackwardIndex();
                }
            }
            else if (lookupType == 8)
            {
                applied = ApplyReverseAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction);
            }

            if (applied)
            {
                applicationCount += 1;

                if (lookupType == 2 &&
                    direction == SfntLayoutTraversalDirection.Backward)
                {
                    index -= 1;
                }
                else if (lookupType != 2 &&
                    lookupType != 4 &&
                    lookupType != 5 &&
                    lookupType != 6)
                {
                    index += step;
                }
            }
            else
            {
                index += step;
            }
        }

        return applicationCount;
    }

    static int GetSelectedValue(
        SfntLayoutLookupSelection? selection,
        int index,
        int defaultValue)
    {
        if (selection == null)
        {
            return defaultValue;
        }

        if (index < 0 || index >= selection.GetTextLength())
        {
            return 0;
        }

        return selection.GetValueAt(index);
    }

    static SfntGsubLookupApplication ApplyLookupAt(
        SfntGsubTable table,
        int lookupIndex,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        int depth,
        SfntLayoutExecutionContext executionContext)
    {
        int oldCount = buffer.GetCount();

        if (index < 0 || index >= oldCount)
        {
            return CreateIdentityApplication(oldCount, index, false);
        }

        executionContext.RecordOperation();

        SfntGsubLookup lookup = table.GetLookup(lookupIndex);
        SfntLayoutLookup layoutLookup = lookup.GetLayoutLookup();
        new SfntLookupGlyphFilter(layoutLookup, gdef);
        int lookupType = lookup.GetLookupType();

        if (lookupType == 1)
        {
            return CreateIdentityApplication(
                oldCount,
                index,
                ApplySingleAt(lookup, buffer, index));
        }

        if (lookupType == 2)
        {
            int replacementCount = ApplyMultipleAt(
                lookup,
                buffer,
                index,
                executionContext);

            if (replacementCount == 0)
            {
                return CreateIdentityApplication(oldCount, index, false);
            }

            return CreateSequenceApplication(
                oldCount,
                index,
                replacementCount);
        }

        if (lookupType == 3)
        {
            return CreateIdentityApplication(
                oldCount,
                index,
                ApplyAlternateAt(
                    lookup,
                    buffer,
                    index,
                    alternateValue));
        }

        if (lookupType == 4)
        {
            SfntGsubLigatureMatch? ligatureMatch = FindLigatureMatch(
                lookup,
                buffer,
                index,
                gdef,
                direction);

            if (ligatureMatch == null)
            {
                return CreateIdentityApplication(oldCount, index, false);
            }

            int[] ligatureIndices = ligatureMatch.GetMatchedIndices();
            SfntGsubLookupApplication application =
                CreateLigatureApplication(oldCount, ligatureIndices);
            buffer.ReplaceWithLigature(
                ligatureIndices,
                ligatureMatch.GetLigature().GetLigatureGlyphId());
            return application;
        }

        if (lookupType == 5)
        {
            return ApplyContextAt(
                table,
                lookup,
                buffer,
                index,
                gdef,
                direction,
                alternateValue,
                depth,
                executionContext);
        }

        if (lookupType == 6)
        {
            return ApplyChainedContextAt(
                table,
                lookup,
                buffer,
                index,
                gdef,
                direction,
                alternateValue,
                depth,
                executionContext);
        }

        if (lookupType == 8)
        {
            return CreateIdentityApplication(
                oldCount,
                index,
                ApplyReverseAt(
                    lookup,
                    buffer,
                    index,
                    gdef,
                    direction));
        }

        return CreateIdentityApplication(oldCount, index, false);
    }

    static SfntGsubLookupApplication ApplyContextAt(
        SfntGsubTable table,
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        int depth,
        SfntLayoutExecutionContext executionContext)
    {
        int oldCount = buffer.GetCount();
        SfntGsubContextMatch? match = FindContextMatch(
            lookup,
            buffer,
            index,
            gdef,
            direction);

        if (match == null)
        {
            return CreateIdentityApplication(oldCount, index, false);
        }

        return ApplyContextMatch(
            table,
            buffer,
            gdef,
            direction,
            alternateValue,
            depth,
            oldCount,
            match,
            executionContext);
    }

    static SfntGsubLookupApplication ApplyContextMatch(
        SfntGsubTable table,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        int depth,
        int oldCount,
        SfntGsubContextMatch match,
        SfntLayoutExecutionContext executionContext)
    {
        executionContext.CheckNesting(depth);

        int[] matchedIndices = match.GetMatchedIndices();
        int minimumIndex = FindMinimum(matchedIndices);
        int maximumIndex = FindMaximum(matchedIndices);
        int[] indexMap = CreateIdentityMap(oldCount);
        int[] boundaryMap = CreateIdentityMap(oldCount + 1);
        SfntSequenceLookupRecords? records = match.GetLookupRecords();
        int recordIndex = 0;

        System.ArgumentNullException.ThrowIfNull(records);
        while (recordIndex < records.GetRecordCount())
        {
            SfntSequenceLookupRecord record = records.GetRecord(recordIndex);
            int targetIndex = matchedIndices[record.GetSequenceIndex()];
            SfntGsubLookupApplication nested = ApplyLookupAt(
                table,
                record.GetLookupListIndex(),
                buffer,
                targetIndex,
                gdef,
                direction,
                alternateValue,
                depth + 1,
                executionContext);
            RemapValues(matchedIndices, nested, false);
            RemapValues(indexMap, nested, false);
            RemapValues(boundaryMap, nested, true);
            recordIndex += 1;
        }

        return new SfntGsubLookupApplication(
            true,
            indexMap,
            boundaryMap,
            boundaryMap[maximumIndex + 1],
            boundaryMap[minimumIndex] - 1);
    }

    static SfntGsubContextMatch? FindContextMatch(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubContextSubstitution substitution =
                lookup.GetContextSubstitution(subtableIndex);
            SfntGsubContextMatch? match;

            if (substitution.GetFormat() == 1)
            {
                match = MatchContextRules(
                    substitution,
                    false,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }
            else if (substitution.GetFormat() == 2)
            {
                match = MatchContextRules(
                    substitution,
                    true,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }
            else
            {
                match = MatchContextCoverages(
                    substitution,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }

            if (match != null)
            {
                return match;
            }

            subtableIndex += 1;
        }

        return null;
    }

    internal static SfntGsubContextMatch? MatchContextRules(
        SfntGsubContextSubstitution substitution,
        bool classBased,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int glyphId = buffer.GetGlyphId(index);
        SfntCoverageTable? coverage = substitution.GetCoverage();
        System.ArgumentNullException.ThrowIfNull(coverage);
        int coverageIndex = coverage
            .GetCoverageIndex(glyphId);
        int ruleSetIndex = coverageIndex;
        if (classBased)
        {
            SfntClassDefinitionTable? classes = substitution.GetClassDefinition();
            System.ArgumentNullException.ThrowIfNull(classes);
            ruleSetIndex = classes.GetClassForGlyph(glyphId);
        }

        if (coverageIndex < 0 ||
            ruleSetIndex < 0 ||
            ruleSetIndex >= substitution.GetRuleSetCount())
        {
            return null;
        }

        SfntGsubContextRuleSet ruleSet =
            substitution.GetRuleSet(ruleSetIndex);

        if (ruleSet == null)
        {
            return null;
        }

        int ruleIndex = 0;

        while (ruleIndex < ruleSet.GetRuleCount())
        {
            SfntGsubContextRule rule = ruleSet.GetRule(ruleIndex);
            int[]? matchedIndices = MatchContextRule(
                rule,
                classBased,
                substitution.GetClassDefinition(),
                layoutLookup,
                buffer,
                index,
                gdef,
                direction);

            if (matchedIndices != null)
            {
                return new SfntGsubContextMatch(
                    matchedIndices,
                    rule.GetLookupRecords());
            }

            ruleIndex += 1;
        }

        return null;
    }

    static int[]? MatchContextRule(
        SfntGsubContextRule rule,
        bool classBased,
        SfntClassDefinitionTable? classDefinition,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int[] matchedIndices = new int[rule.GetGlyphCount()];
        matchedIndices[0] = index;
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer,
            layoutLookup,
            gdef,
            direction);
        iterator.Reset(index);
        if (classBased)
        {
            System.ArgumentNullException.ThrowIfNull(classDefinition);
        }
        int inputIndex = 1;

        while (inputIndex < matchedIndices.Length)
        {
            if (!iterator.MoveNext())
            {
                return null;
            }

            int actualValue = iterator.GetGlyphId();
            if (classBased)
            {
                System.ArgumentNullException.ThrowIfNull(classDefinition);
                actualValue = classDefinition.GetClassForGlyph(iterator.GetGlyphId());
            }

            if (actualValue != rule.GetInputValue(inputIndex - 1))
            {
                return null;
            }

            matchedIndices[inputIndex] = iterator.GetIndex();
            inputIndex += 1;
        }

        return matchedIndices;
    }

    internal static SfntGsubContextMatch? MatchContextCoverages(
        SfntGsubContextSubstitution substitution,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int inputCount = substitution.GetInputCoverageCount();

        if (!substitution.GetInputCoverage(0).Covers(
            buffer.GetGlyphId(index)))
        {
            return null;
        }

        int[] matchedIndices = new int[inputCount];
        matchedIndices[0] = index;
        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer,
            layoutLookup,
            gdef,
            direction);
        iterator.Reset(index);
        int inputIndex = 1;

        while (inputIndex < inputCount)
        {
            if (!iterator.MoveNext() ||
                !substitution.GetInputCoverage(inputIndex).Covers(
                    iterator.GetGlyphId()))
            {
                return null;
            }

            matchedIndices[inputIndex] = iterator.GetIndex();
            inputIndex += 1;
        }

        return new SfntGsubContextMatch(
            matchedIndices,
            substitution.GetLookupRecords());
    }

    static SfntGsubLookupApplication ApplyChainedContextAt(
        SfntGsubTable table,
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        int alternateValue,
        int depth,
        SfntLayoutExecutionContext executionContext)
    {
        int oldCount = buffer.GetCount();
        SfntGsubContextMatch? match = FindChainedContextMatch(
            lookup,
            buffer,
            index,
            gdef,
            direction);

        if (match == null)
        {
            return CreateIdentityApplication(oldCount, index, false);
        }

        return ApplyContextMatch(
            table,
            buffer,
            gdef,
            direction,
            alternateValue,
            depth,
            oldCount,
            match,
            executionContext);
    }

    static SfntGsubContextMatch? FindChainedContextMatch(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubChainedContextSubstitution substitution =
                lookup.GetChainedContextSubstitution(subtableIndex);
            SfntGsubContextMatch? match;

            if (substitution.GetFormat() == 1)
            {
                match = MatchChainedRules(
                    substitution,
                    false,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }
            else if (substitution.GetFormat() == 2)
            {
                match = MatchChainedRules(
                    substitution,
                    true,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }
            else
            {
                match = MatchChainedCoverages(
                    substitution,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction);
            }

            if (match != null)
            {
                return match;
            }

            subtableIndex += 1;
        }

        return null;
    }

    internal static SfntGsubContextMatch? MatchChainedRules(
        SfntGsubChainedContextSubstitution substitution,
        bool classBased,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int glyphId = buffer.GetGlyphId(index);
        SfntCoverageTable? coverage = substitution.GetCoverage();
        System.ArgumentNullException.ThrowIfNull(coverage);
        int coverageIndex = coverage
            .GetCoverageIndex(glyphId);
        int ruleSetIndex = coverageIndex;
        if (classBased)
        {
            SfntClassDefinitionTable? classes = substitution.GetInputClassDefinition();
            System.ArgumentNullException.ThrowIfNull(classes);
            ruleSetIndex = classes.GetClassForGlyph(glyphId);
        }

        if (coverageIndex < 0 ||
            ruleSetIndex < 0 ||
            ruleSetIndex >= substitution.GetRuleSetCount())
        {
            return null;
        }

        SfntGsubChainedRuleSet ruleSet =
            substitution.GetRuleSet(ruleSetIndex);

        if (ruleSet == null)
        {
            return null;
        }

        int ruleIndex = 0;

        while (ruleIndex < ruleSet.GetRuleCount())
        {
            SfntGsubChainedRule rule = ruleSet.GetRule(ruleIndex);
            int[]? matchedIndices = MatchChainedRule(
                rule,
                substitution,
                classBased,
                layoutLookup,
                buffer,
                index,
                gdef,
                direction);

            if (matchedIndices != null)
            {
                return new SfntGsubContextMatch(
                    matchedIndices,
                    rule.GetLookupRecords());
            }

            ruleIndex += 1;
        }

        return null;
    }

    static int[]? MatchChainedRule(
        SfntGsubChainedRule rule,
        SfntGsubChainedContextSubstitution substitution,
        bool classBased,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int[] matchedIndices = new int[rule.GetInputGlyphCount()];
        matchedIndices[0] = index;
        SfntLookupGlyphIterator inputIterator = SfntLookupGlyphIterator.Create(
            buffer,
            layoutLookup,
            gdef,
            direction);
        inputIterator.Reset(index);
        int inputIndex = 1;
        SfntClassDefinitionTable? inputClasses = null;
        SfntClassDefinitionTable? backtrackClasses = null;
        SfntClassDefinitionTable? lookaheadClasses = null;
        if (classBased)
        {
            inputClasses = substitution.GetInputClassDefinition();
            backtrackClasses = substitution.GetBacktrackClassDefinition();
            lookaheadClasses = substitution.GetLookaheadClassDefinition();
            System.ArgumentNullException.ThrowIfNull(inputClasses);
            System.ArgumentNullException.ThrowIfNull(backtrackClasses);
            System.ArgumentNullException.ThrowIfNull(lookaheadClasses);
        }

        while (inputIndex < matchedIndices.Length)
        {
            if (!inputIterator.MoveNext())
            {
                return null;
            }

            int actualValue = inputIterator.GetGlyphId();
            if (classBased)
            {
                System.ArgumentNullException.ThrowIfNull(inputClasses);
                actualValue = inputClasses.GetClassForGlyph(
                    inputIterator.GetGlyphId());
            }

            if (actualValue != rule.GetInputValue(inputIndex - 1))
            {
                return null;
            }

            matchedIndices[inputIndex] = inputIterator.GetIndex();
            inputIndex += 1;
        }

        SfntLookupGlyphIterator backtrackIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                GetOppositeDirection(direction));
        backtrackIterator.Reset(index);
        int backtrackIndex = 0;

        while (backtrackIndex < rule.GetBacktrackCount())
        {
            if (!backtrackIterator.MoveNext())
            {
                return null;
            }

            int actualValue = backtrackIterator.GetGlyphId();
            if (classBased)
            {
                System.ArgumentNullException.ThrowIfNull(backtrackClasses);
                actualValue = backtrackClasses.GetClassForGlyph(
                    backtrackIterator.GetGlyphId());
            }

            if (actualValue != rule.GetBacktrackValue(backtrackIndex))
            {
                return null;
            }

            backtrackIndex += 1;
        }

        SfntLookupGlyphIterator lookaheadIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                direction);
        lookaheadIterator.Reset(
            matchedIndices[matchedIndices.Length - 1]);
        int lookaheadIndex = 0;

        while (lookaheadIndex < rule.GetLookaheadCount())
        {
            if (!lookaheadIterator.MoveNext())
            {
                return null;
            }

            int actualValue = lookaheadIterator.GetGlyphId();
            if (classBased)
            {
                System.ArgumentNullException.ThrowIfNull(lookaheadClasses);
                actualValue = lookaheadClasses.GetClassForGlyph(
                    lookaheadIterator.GetGlyphId());
            }

            if (actualValue != rule.GetLookaheadValue(lookaheadIndex))
            {
                return null;
            }

            lookaheadIndex += 1;
        }

        return matchedIndices;
    }

    internal static SfntGsubContextMatch? MatchChainedCoverages(
        SfntGsubChainedContextSubstitution substitution,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        if (!substitution.GetInputCoverage(0).Covers(
            buffer.GetGlyphId(index)))
        {
            return null;
        }

        int[] matchedIndices =
            new int[substitution.GetInputCoverageCount()];
        matchedIndices[0] = index;
        SfntLookupGlyphIterator inputIterator = SfntLookupGlyphIterator.Create(
            buffer,
            layoutLookup,
            gdef,
            direction);
        inputIterator.Reset(index);
        int inputIndex = 1;

        while (inputIndex < matchedIndices.Length)
        {
            if (!inputIterator.MoveNext() ||
                !substitution.GetInputCoverage(inputIndex).Covers(
                    inputIterator.GetGlyphId()))
            {
                return null;
            }

            matchedIndices[inputIndex] = inputIterator.GetIndex();
            inputIndex += 1;
        }

        SfntLookupGlyphIterator backtrackIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                GetOppositeDirection(direction));
        backtrackIterator.Reset(index);
        int backtrackIndex = 0;

        while (backtrackIndex < substitution.GetBacktrackCoverageCount())
        {
            if (!backtrackIterator.MoveNext() ||
                !substitution.GetBacktrackCoverage(backtrackIndex).Covers(
                    backtrackIterator.GetGlyphId()))
            {
                return null;
            }

            backtrackIndex += 1;
        }

        SfntLookupGlyphIterator lookaheadIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                direction);
        lookaheadIterator.Reset(
            matchedIndices[matchedIndices.Length - 1]);
        int lookaheadIndex = 0;

        while (lookaheadIndex < substitution.GetLookaheadCoverageCount())
        {
            if (!lookaheadIterator.MoveNext() ||
                !substitution.GetLookaheadCoverage(lookaheadIndex).Covers(
                    lookaheadIterator.GetGlyphId()))
            {
                return null;
            }

            lookaheadIndex += 1;
        }

        return new SfntGsubContextMatch(
            matchedIndices,
            substitution.GetLookupRecords());
    }

    static bool ApplyReverseAt(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubReverseSubstitution substitution =
                lookup.GetReverseSubstitution(subtableIndex);
            int substituteGlyphId = substitution.GetSubstituteGlyphId(
                buffer.GetGlyphId(index));

            if (substituteGlyphId >= 0 &&
                MatchReverseContext(
                    substitution,
                    lookup.GetLayoutLookup(),
                    buffer,
                    index,
                    gdef,
                    direction))
            {
                buffer.SetGlyphId(index, substituteGlyphId);
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static bool MatchReverseContext(
        SfntGsubReverseSubstitution substitution,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        SfntLookupGlyphIterator backtrackIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                GetOppositeDirection(direction));
        backtrackIterator.Reset(index);
        int backtrackIndex = 0;

        while (backtrackIndex < substitution.GetBacktrackCoverageCount())
        {
            if (!backtrackIterator.MoveNext() ||
                !substitution.GetBacktrackCoverage(backtrackIndex).Covers(
                    backtrackIterator.GetGlyphId()))
            {
                return false;
            }

            backtrackIndex += 1;
        }

        SfntLookupGlyphIterator lookaheadIterator =
            SfntLookupGlyphIterator.Create(
                buffer,
                layoutLookup,
                gdef,
                direction);
        lookaheadIterator.Reset(index);
        int lookaheadIndex = 0;

        while (lookaheadIndex < substitution.GetLookaheadCoverageCount())
        {
            if (!lookaheadIterator.MoveNext() ||
                !substitution.GetLookaheadCoverage(lookaheadIndex).Covers(
                    lookaheadIterator.GetGlyphId()))
            {
                return false;
            }

            lookaheadIndex += 1;
        }

        return true;
    }

    static SfntLayoutTraversalDirection GetOppositeDirection(
        SfntLayoutTraversalDirection direction)
    {
        return direction == SfntLayoutTraversalDirection.Forward ?
            SfntLayoutTraversalDirection.Backward :
            SfntLayoutTraversalDirection.Forward;
    }

    static SfntGsubLookupApplication CreateIdentityApplication(
        int count,
        int index,
        bool applied)
    {
        return new SfntGsubLookupApplication(
            applied,
            CreateIdentityMap(count),
            CreateIdentityMap(count + 1),
            index + 1,
            index - 1);
    }

    static SfntGsubLookupApplication CreateSequenceApplication(
        int oldCount,
        int index,
        int replacementCount)
    {
        int delta = replacementCount - 1;
        int[] indexMap = new int[oldCount];
        int oldIndex = 0;

        while (oldIndex < oldCount)
        {
            indexMap[oldIndex] = oldIndex <= index ?
                oldIndex : oldIndex + delta;
            oldIndex += 1;
        }

        int[] boundaryMap = new int[oldCount + 1];
        int boundary = 0;

        while (boundary < boundaryMap.Length)
        {
            boundaryMap[boundary] = boundary <= index ?
                boundary : boundary + delta;
            boundary += 1;
        }

        return new SfntGsubLookupApplication(
            true,
            indexMap,
            boundaryMap,
            index + replacementCount,
            index - 1);
    }

    static SfntGsubLookupApplication CreateLigatureApplication(
        int oldCount,
        int[] matchedIndices)
    {
        bool[] matched = new bool[oldCount];
        int index = 0;

        while (index < matchedIndices.Length)
        {
            matched[matchedIndices[index]] = true;
            index += 1;
        }

        int minimumIndex = FindMinimum(matchedIndices);
        int maximumIndex = FindMaximum(matchedIndices);
        int firstIndex = matchedIndices[0];
        int[] indexMap = new int[oldCount];
        int[] boundaryMap = new int[oldCount + 1];
        int newIndex = 0;
        int outputIndex = -1;
        int oldIndex = 0;

        while (oldIndex < oldCount)
        {
            if (oldIndex == firstIndex)
            {
                outputIndex = newIndex;
                indexMap[oldIndex] = outputIndex;
                newIndex += 1;
            }
            else if (!matched[oldIndex])
            {
                indexMap[oldIndex] = newIndex;
                newIndex += 1;
            }

            boundaryMap[oldIndex + 1] = newIndex;
            oldIndex += 1;
        }

        index = 0;

        while (index < matchedIndices.Length)
        {
            indexMap[matchedIndices[index]] = outputIndex;
            index += 1;
        }

        return new SfntGsubLookupApplication(
            true,
            indexMap,
            boundaryMap,
            boundaryMap[maximumIndex + 1],
            boundaryMap[minimumIndex] - 1);
    }

    static int[] CreateIdentityMap(int count)
    {
        int[] map = new int[count];
        int index = 0;

        while (index < map.Length)
        {
            map[index] = index;
            index += 1;
        }

        return map;
    }

    static void RemapValues(
        int[] values,
        SfntGsubLookupApplication application,
        bool boundaries)
    {
        int index = 0;

        while (index < values.Length)
        {
            values[index] = boundaries ?
                application.MapBoundary(values[index]) :
                application.MapIndex(values[index]);
            index += 1;
        }
    }

    static bool ApplySingleAt(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubSingleSubstitution substitution =
                lookup.GetSingleSubstitution(subtableIndex);
            int substituteGlyphId = substitution.GetSubstituteGlyphId(
                buffer.GetGlyphId(index));

            if (substituteGlyphId >= 0)
            {
                buffer.SetGlyphId(index, substituteGlyphId);
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static int ApplyMultipleAt(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntLayoutExecutionContext executionContext)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubMultipleSubstitution substitution =
                lookup.GetMultipleSubstitution(subtableIndex);
            SfntGsubGlyphSequence? sequence = substitution.FindSequence(
                buffer.GetGlyphId(index));

            if (sequence != null)
            {
                executionContext.CheckSubstitution(
                    buffer.GetCount(),
                    sequence.GetGlyphCount());
                buffer.ReplaceWithSequence(
                    index,
                    sequence.CopyGlyphIds());
                return sequence.GetGlyphCount();
            }

            subtableIndex += 1;
        }

        return 0;
    }

    static bool ApplyAlternateAt(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        int alternateValue)
    {
        if (alternateValue == 0)
        {
            return false;
        }

        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubAlternateSubstitution substitution =
                lookup.GetAlternateSubstitution(subtableIndex);
            SfntGsubAlternateSet? alternateSet =
                substitution.FindAlternateSet(buffer.GetGlyphId(index));

            if (alternateSet != null &&
                alternateValue <= alternateSet.GetGlyphCount())
            {
                buffer.SetGlyphId(
                    index,
                    alternateSet.GetGlyphId(alternateValue - 1));
                return true;
            }

            subtableIndex += 1;
        }

        return false;
    }

    static SfntGsubLigatureMatch? FindLigatureMatch(
        SfntGsubLookup lookup,
        SfntLayoutGlyphBuffer buffer,
        int index,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            SfntGsubLigatureSubstitution substitution =
                lookup.GetLigatureSubstitution(subtableIndex);
            SfntGsubLigatureSet? set = substitution.FindLigatureSet(
                buffer.GetGlyphId(index));

            if (set != null)
            {
                int ligatureIndex = 0;

                while (ligatureIndex < set.GetLigatureCount())
                {
                    SfntGsubLigature ligature =
                        set.GetLigature(ligatureIndex);
                    int[]? matchedIndices = MatchLigature(
                        ligature,
                        lookup.GetLayoutLookup(),
                        buffer,
                        index,
                        gdef,
                        direction);

                    if (matchedIndices != null)
                    {
                        return new SfntGsubLigatureMatch(
                            ligature,
                            matchedIndices);
                    }

                    ligatureIndex += 1;
                }
            }

            subtableIndex += 1;
        }

        return null;
    }

    static int[]? MatchLigature(
        SfntGsubLigature ligature,
        SfntLayoutLookup layoutLookup,
        SfntLayoutGlyphBuffer buffer,
        int firstIndex,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction)
    {
        int componentCount = ligature.GetComponentCount();
        int[] matchedIndices = new int[componentCount];
        matchedIndices[0] = firstIndex;

        if (componentCount == 1)
        {
            return matchedIndices;
        }

        SfntLookupGlyphIterator iterator = SfntLookupGlyphIterator.Create(
            buffer,
            layoutLookup,
            gdef,
            direction);
        iterator.Reset(firstIndex);
        int componentIndex = 1;

        while (componentIndex < componentCount)
        {
            if (!iterator.MoveNext() ||
                iterator.GetGlyphId() !=
                ligature.GetComponentGlyphId(componentIndex - 1))
            {
                return null;
            }

            matchedIndices[componentIndex] = iterator.GetIndex();
            componentIndex += 1;
        }

        return matchedIndices;
    }

    static int FindMinimum(int[] values)
    {
        int minimum = values[0];
        int index = 1;

        while (index < values.Length)
        {
            minimum = System.Math.Min(minimum, values[index]);
            index += 1;
        }

        return minimum;
    }

    static int FindMaximum(int[] values)
    {
        int maximum = values[0];
        int index = 1;

        while (index < values.Length)
        {
            maximum = System.Math.Max(maximum, values[index]);
            index += 1;
        }

        return maximum;
    }
}
