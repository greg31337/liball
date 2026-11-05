namespace ttf;

internal class FontSubsetLayoutClosure
{
    bool applicable;
    bool retainGdef;
    int[] glyphIds;
    int[] gsubFeatureIndices;
    int[] gsubLookupIndices;
    int[] gposFeatureIndices;
    int[] gposLookupIndices;
    int[] gdefMarkGlyphSetIndices;

    internal FontSubsetLayoutClosure(
        bool applicable,
        bool retainGdef,
        int[] glyphIds,
        int[] gsubFeatureIndices,
        int[] gsubLookupIndices,
        int[] gposFeatureIndices,
        int[] gposLookupIndices,
        int[] gdefMarkGlyphSetIndices)
    {
        this.applicable = applicable;
        this.retainGdef = retainGdef;
        this.glyphIds = CopyIntegers(glyphIds);
        this.gsubFeatureIndices = CopyIntegers(gsubFeatureIndices);
        this.gsubLookupIndices = CopyIntegers(gsubLookupIndices);
        this.gposFeatureIndices = CopyIntegers(gposFeatureIndices);
        this.gposLookupIndices = CopyIntegers(gposLookupIndices);
        this.gdefMarkGlyphSetIndices =
            CopyIntegers(gdefMarkGlyphSetIndices);
    }

    internal bool IsApplicable()
    {
        return applicable;
    }

    internal bool RetainsGdef()
    {
        return retainGdef;
    }

    internal int[] CopyGlyphIds()
    {
        return CopyIntegers(glyphIds);
    }

    internal int[] CopyGsubFeatureIndices()
    {
        return CopyIntegers(gsubFeatureIndices);
    }

    internal int[] CopyGsubLookupIndices()
    {
        return CopyIntegers(gsubLookupIndices);
    }

    internal int[] CopyGposFeatureIndices()
    {
        return CopyIntegers(gposFeatureIndices);
    }

    internal int[] CopyGposLookupIndices()
    {
        return CopyIntegers(gposLookupIndices);
    }

    internal int[] CopyGdefMarkGlyphSetIndices()
    {
        return CopyIntegers(gdefMarkGlyphSetIndices);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetLayoutClosureBuilder
{
    SfntGsubTable? gsub;
    SfntGposTable? gpos;
    SfntGdefTable? gdef;
    int maximumEntries;
    int maximumNesting;
    System.Collections.Generic.HashSet<int> glyphIds;
    System.Collections.Generic.HashSet<int> gsubFeatures;
    System.Collections.Generic.HashSet<int> gsubLookups;
    System.Collections.Generic.HashSet<int> gposFeatures;
    System.Collections.Generic.HashSet<int> gposLookups;
    System.Collections.Generic.HashSet<int> gdefMarkGlyphSets;

    FontSubsetLayoutClosureBuilder(
        SfntGsubTable? gsub,
        SfntGposTable? gpos,
        SfntGdefTable? gdef,
        int maximumEntries,
        int maximumNesting)
    {
        this.gsub = gsub;
        this.gpos = gpos;
        this.gdef = gdef;
        this.maximumEntries = maximumEntries;
        this.maximumNesting = maximumNesting;
        glyphIds = new System.Collections.Generic.HashSet<int>();
        gsubFeatures = new System.Collections.Generic.HashSet<int>();
        gsubLookups = new System.Collections.Generic.HashSet<int>();
        gposFeatures = new System.Collections.Generic.HashSet<int>();
        gposLookups = new System.Collections.Generic.HashSet<int>();
        gdefMarkGlyphSets =
            new System.Collections.Generic.HashSet<int>();
    }

    internal static FontSubsetLayoutClosure Create(
        FontInstance instance,
        FontSubsetOptions options,
        int[] initialGlyphIds,
        int[] featureTags,
        int maximumEntries)
    {
        if (options.GetLayoutPolicy() != FontSubsetLayoutPolicy.Retain)
        {
            return Empty(false, initialGlyphIds);
        }

        FontFace face = instance.GetFace();
        FontLimits limits = face.GetTableCache().GetLimits();
        return CreateForTables(
            face.GetGsubTable(),
            face.GetGposTable(),
            face.GetGdefTable(),
            initialGlyphIds,
            featureTags,
            maximumEntries,
            limits.GetMaxLayoutNesting());
    }

    internal static FontSubsetLayoutClosure CreateForTables(
        SfntGsubTable gsub,
        SfntGposTable gpos,
        SfntGdefTable gdef,
        int[] initialGlyphIds,
        int[] featureTags,
        int maximumEntries)
    {
        return CreateForTables(
            gsub,
            gpos,
            gdef,
            initialGlyphIds,
            featureTags,
            maximumEntries,
            FontLimits.Create().GetMaxLayoutNesting());
    }

    internal static FontSubsetLayoutClosure CreateForTables(
        SfntGsubTable? gsub,
        SfntGposTable? gpos,
        SfntGdefTable? gdef,
        int[] initialGlyphIds,
        int[] featureTags,
        int maximumEntries,
        int maximumNesting)
    {
        if (initialGlyphIds == null || featureTags == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (maximumEntries <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumEntries");
        }

        if (maximumNesting <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumNesting");
        }

        FontSubsetLayoutClosureBuilder builder =
            new FontSubsetLayoutClosureBuilder(
                gsub,
                gpos,
                gdef,
                maximumEntries,
                maximumNesting);
        return builder.Build(initialGlyphIds, featureTags);
    }

    FontSubsetLayoutClosure Build(
        int[] initialGlyphIds,
        int[] featureTags)
    {
        int index = 0;

        while (index < initialGlyphIds.Length)
        {
            AddGlyph(initialGlyphIds[index]);
            index++;
        }

        if (gsub != null)
        {
            SelectFeatures(
                gsub.GetLayoutTable(),
                featureTags,
                gsubFeatures);
            AddGsubFeatureLookups();
            AddGsubOutputs();
        }

        if (gpos != null)
        {
            SelectFeatures(
                gpos.GetLayoutTable(),
                featureTags,
                gposFeatures);
            AddGposFeatureLookups();
        }

        CollectGdefDependencies();
        return new FontSubsetLayoutClosure(
            true,
            gdef != null,
            ToSortedArray(glyphIds),
            ToSortedArray(gsubFeatures),
            ToSortedArray(gsubLookups),
            ToSortedArray(gposFeatures),
            ToSortedArray(gposLookups),
            ToSortedArray(gdefMarkGlyphSets));
    }

    void AddGsubFeatureLookups()
    {
        System.ArgumentNullException.ThrowIfNull(gsub);
        SfntLayoutFeatureList features =
            gsub.GetLayoutTable().GetFeatureList();
        int[] featureIndices = ToSortedArray(gsubFeatures);
        int featurePosition = 0;

        while (featurePosition < featureIndices.Length)
        {
            SfntLayoutFeature feature = features.GetFeature(
                featureIndices[featurePosition]);
            int lookupPosition = 0;

            while (lookupPosition < feature.GetLookupIndexCount())
            {
                AddGsubLookup(feature.GetLookupIndex(lookupPosition), 0);
                lookupPosition++;
            }

            featurePosition++;
        }

        if (gsub.GetLayoutTable().GetFeatureVariationsOffset() != 0)
        {
            int lookupIndex = 0;
            while (lookupIndex < gsub.GetLookupCount())
            {
                AddGsubLookup(lookupIndex, 0);
                lookupIndex++;
            }
        }
    }

    void AddGposFeatureLookups()
    {
        System.ArgumentNullException.ThrowIfNull(gpos);
        SfntLayoutFeatureList features =
            gpos.GetLayoutTable().GetFeatureList();
        int[] featureIndices = ToSortedArray(gposFeatures);
        int featurePosition = 0;

        while (featurePosition < featureIndices.Length)
        {
            SfntLayoutFeature feature = features.GetFeature(
                featureIndices[featurePosition]);
            int lookupPosition = 0;

            while (lookupPosition < feature.GetLookupIndexCount())
            {
                AddGposLookup(feature.GetLookupIndex(lookupPosition), 0);
                lookupPosition++;
            }

            featurePosition++;
        }

        if (gpos.GetLayoutTable().GetFeatureVariationsOffset() != 0)
        {
            int lookupIndex = 0;
            while (lookupIndex < gpos.GetLookupCount())
            {
                AddGposLookup(lookupIndex, 0);
                lookupIndex++;
            }
        }
    }

    void AddGsubLookup(int lookupIndex, int depth)
    {
        System.ArgumentNullException.ThrowIfNull(gsub);
        if (lookupIndex < 0 || lookupIndex >= gsub.GetLookupCount())
        {
            throw FontFormatException.Create(
                "A retained GSUB feature references an invalid lookup.");
        }

        if (gsubLookups.Contains(lookupIndex))
        {
            return;
        }

        AddEntry(gsubLookups, lookupIndex);
        SfntGsubLookup lookup = gsub.GetLookup(lookupIndex);
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            if (lookup.GetLookupType() == 5)
            {
                EnsureNesting(depth);
                AddGsubContextDependencies(
                    lookup.GetContextSubstitution(subtableIndex),
                    depth + 1);
            }
            else if (lookup.GetLookupType() == 6)
            {
                EnsureNesting(depth);
                AddGsubChainedDependencies(
                    lookup.GetChainedContextSubstitution(subtableIndex),
                    depth + 1);
            }

            subtableIndex++;
        }
    }

    void AddGposLookup(int lookupIndex, int depth)
    {
        System.ArgumentNullException.ThrowIfNull(gpos);
        if (lookupIndex < 0 || lookupIndex >= gpos.GetLookupCount())
        {
            throw FontFormatException.Create(
                "A retained GPOS feature references an invalid lookup.");
        }

        if (gposLookups.Contains(lookupIndex))
        {
            return;
        }

        AddEntry(gposLookups, lookupIndex);
        SfntGposLookup lookup = gpos.GetLookup(lookupIndex);
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            if (lookup.GetLookupType() == 7)
            {
                EnsureNesting(depth);
                AddGposContextDependencies(
                    lookup.GetContextAdjustment(subtableIndex),
                    depth + 1);
            }
            else if (lookup.GetLookupType() == 8)
            {
                EnsureNesting(depth);
                AddGposChainedDependencies(
                    lookup.GetChainedContextAdjustment(subtableIndex),
                    depth + 1);
            }

            subtableIndex++;
        }
    }

    void AddGsubContextDependencies(
        SfntGsubContextSubstitution context,
        int depth)
    {
        AddContextDependencies(context, true, depth);
    }

    void AddGposContextDependencies(
        SfntGsubContextSubstitution context,
        int depth)
    {
        AddContextDependencies(context, false, depth);
    }

    void AddContextDependencies(
        SfntGsubContextSubstitution context,
        bool substitution,
        int depth)
    {
        if (context.GetFormat() == 3)
        {
            AddLookupRecords(
                context.GetLookupRecords(), substitution, depth);
            return;
        }

        int setIndex = 0;
        while (setIndex < context.GetRuleSetCount())
        {
            SfntGsubContextRuleSet set = context.GetRuleSet(setIndex);

            if (set != null)
            {
                int ruleIndex = 0;
                while (ruleIndex < set.GetRuleCount())
                {
                    AddLookupRecords(
                        set.GetRule(ruleIndex).GetLookupRecords(),
                        substitution,
                        depth);
                    ruleIndex++;
                }
            }

            setIndex++;
        }
    }

    void AddGsubChainedDependencies(
        SfntGsubChainedContextSubstitution context,
        int depth)
    {
        AddChainedDependencies(context, true, depth);
    }

    void AddGposChainedDependencies(
        SfntGsubChainedContextSubstitution context,
        int depth)
    {
        AddChainedDependencies(context, false, depth);
    }

    void AddChainedDependencies(
        SfntGsubChainedContextSubstitution context,
        bool substitution,
        int depth)
    {
        if (context.GetFormat() == 3)
        {
            AddLookupRecords(
                context.GetLookupRecords(), substitution, depth);
            return;
        }

        int setIndex = 0;
        while (setIndex < context.GetRuleSetCount())
        {
            SfntGsubChainedRuleSet set = context.GetRuleSet(setIndex);

            if (set != null)
            {
                int ruleIndex = 0;
                while (ruleIndex < set.GetRuleCount())
                {
                    AddLookupRecords(
                        set.GetRule(ruleIndex).GetLookupRecords(),
                        substitution,
                        depth);
                    ruleIndex++;
                }
            }

            setIndex++;
        }
    }

    void AddLookupRecords(
        SfntSequenceLookupRecords? records,
        bool substitution,
        int depth)
    {
        int recordIndex = 0;

        System.ArgumentNullException.ThrowIfNull(records);
        while (recordIndex < records.GetRecordCount())
        {
            int lookupIndex = records.GetRecord(recordIndex)
                .GetLookupListIndex();

            if (substitution)
            {
                AddGsubLookup(lookupIndex, depth);
            }
            else
            {
                AddGposLookup(lookupIndex, depth);
            }

            recordIndex++;
        }
    }

    void AddGsubOutputs()
    {
        bool changed = true;

        while (changed)
        {
            changed = false;
            int[] lookupIndices = ToSortedArray(gsubLookups);
            int lookupPosition = 0;

            while (lookupPosition < lookupIndices.Length)
            {
                System.ArgumentNullException.ThrowIfNull(gsub);
                if (AddLookupOutputs(
                    gsub.GetLookup(lookupIndices[lookupPosition])))
                {
                    changed = true;
                }

                lookupPosition++;
            }
        }
    }

    bool AddLookupOutputs(SfntGsubLookup lookup)
    {
        bool changed = false;
        int subtableIndex = 0;

        while (subtableIndex < lookup.GetSubtableCount())
        {
            if (lookup.GetLookupType() == 1)
            {
                if (AddSingleOutputs(
                    lookup.GetSingleSubstitution(subtableIndex)))
                {
                    changed = true;
                }
            }
            else if (lookup.GetLookupType() == 2)
            {
                if (AddMultipleOutputs(
                    lookup.GetMultipleSubstitution(subtableIndex)))
                {
                    changed = true;
                }
            }
            else if (lookup.GetLookupType() == 3)
            {
                if (AddAlternateOutputs(
                    lookup.GetAlternateSubstitution(subtableIndex)))
                {
                    changed = true;
                }
            }
            else if (lookup.GetLookupType() == 4)
            {
                if (AddLigatureOutputs(
                    lookup.GetLigatureSubstitution(subtableIndex)))
                {
                    changed = true;
                }
            }
            else if (lookup.GetLookupType() == 8)
            {
                if (AddReverseOutputs(
                    lookup.GetReverseSubstitution(subtableIndex)))
                {
                    changed = true;
                }
            }

            subtableIndex++;
        }

        return changed;
    }

    bool AddSingleOutputs(SfntGsubSingleSubstitution substitution)
    {
        bool changed = false;
        int[] retainedGlyphs = ToSortedArray(glyphIds);
        int index = 0;

        while (index < retainedGlyphs.Length)
        {
            int output = substitution.GetSubstituteGlyphId(
                retainedGlyphs[index]);

            if (output >= 0 && AddGlyph(output))
            {
                changed = true;
            }

            index++;
        }

        return changed;
    }

    bool AddMultipleOutputs(SfntGsubMultipleSubstitution substitution)
    {
        bool changed = false;
        int[] retainedGlyphs = ToSortedArray(glyphIds);
        int index = 0;

        while (index < retainedGlyphs.Length)
        {
            SfntGsubGlyphSequence? sequence = substitution.FindSequence(
                retainedGlyphs[index]);

            if (sequence != null)
            {
                int glyphIndex = 0;
                while (glyphIndex < sequence.GetGlyphCount())
                {
                    if (AddGlyph(sequence.GetGlyphId(glyphIndex)))
                    {
                        changed = true;
                    }

                    glyphIndex++;
                }
            }

            index++;
        }

        return changed;
    }

    bool AddAlternateOutputs(SfntGsubAlternateSubstitution substitution)
    {
        bool changed = false;
        int[] retainedGlyphs = ToSortedArray(glyphIds);
        int index = 0;

        while (index < retainedGlyphs.Length)
        {
            SfntGsubAlternateSet? set = substitution.FindAlternateSet(
                retainedGlyphs[index]);

            if (set != null)
            {
                int glyphIndex = 0;
                while (glyphIndex < set.GetGlyphCount())
                {
                    if (AddGlyph(set.GetGlyphId(glyphIndex)))
                    {
                        changed = true;
                    }

                    glyphIndex++;
                }
            }

            index++;
        }

        return changed;
    }

    bool AddLigatureOutputs(SfntGsubLigatureSubstitution substitution)
    {
        bool changed = false;
        SfntCoverageTable coverage = substitution.GetCoverage();
        int coverageIndex = 0;

        while (coverageIndex < coverage.GetCoveredGlyphCount())
        {
            int firstGlyphId = coverage.GetGlyphId(coverageIndex);

            if (glyphIds.Contains(firstGlyphId))
            {
                SfntGsubLigatureSet set =
                    substitution.GetLigatureSet(coverageIndex);
                int ligatureIndex = 0;

                while (ligatureIndex < set.GetLigatureCount())
                {
                    SfntGsubLigature ligature =
                        set.GetLigature(ligatureIndex);

                    if (HasLigatureInputs(ligature) &&
                        AddGlyph(ligature.GetLigatureGlyphId()))
                    {
                        changed = true;
                    }

                    ligatureIndex++;
                }
            }

            coverageIndex++;
        }

        return changed;
    }

    bool HasLigatureInputs(SfntGsubLigature ligature)
    {
        int componentIndex = 0;

        while (componentIndex < ligature.GetComponentCount() - 1)
        {
            if (!glyphIds.Contains(
                ligature.GetComponentGlyphId(componentIndex)))
            {
                return false;
            }

            componentIndex++;
        }

        return true;
    }

    bool AddReverseOutputs(SfntGsubReverseSubstitution substitution)
    {
        bool changed = false;
        int[] retainedGlyphs = ToSortedArray(glyphIds);
        int index = 0;

        while (index < retainedGlyphs.Length)
        {
            int glyphId = retainedGlyphs[index];
            int output = substitution.GetSubstituteGlyphId(glyphId);

            if (output >= 0 &&
                HasReverseContext(substitution) &&
                AddGlyph(output))
            {
                changed = true;
            }

            index++;
        }

        return changed;
    }

    bool HasReverseContext(SfntGsubReverseSubstitution substitution)
    {
        int index = 0;

        while (index < substitution.GetBacktrackCoverageCount())
        {
            if (!CoverageIntersectsRetainedGlyphs(
                substitution.GetBacktrackCoverage(index)))
            {
                return false;
            }

            index++;
        }

        index = 0;
        while (index < substitution.GetLookaheadCoverageCount())
        {
            if (!CoverageIntersectsRetainedGlyphs(
                substitution.GetLookaheadCoverage(index)))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    bool CoverageIntersectsRetainedGlyphs(SfntCoverageTable coverage)
    {
        int index = 0;

        while (index < coverage.GetCoveredGlyphCount())
        {
            if (glyphIds.Contains(coverage.GetGlyphId(index)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    void CollectGdefDependencies()
    {
        if (gsub != null)
        {
            int[] indices = ToSortedArray(gsubLookups);
            int index = 0;

            while (index < indices.Length)
            {
                ValidateAndAddGdefDependencies(
                    gsub.GetLookup(indices[index]).GetLayoutLookup());
                index++;
            }
        }

        if (gpos != null)
        {
            int[] indices = ToSortedArray(gposLookups);
            int index = 0;

            while (index < indices.Length)
            {
                ValidateAndAddGdefDependencies(
                    gpos.GetLookup(indices[index]).GetLayoutLookup());
                index++;
            }
        }
    }

    void ValidateAndAddGdefDependencies(SfntLayoutLookup lookup)
    {
        bool usesGlyphClasses = lookup.IgnoresBaseGlyphs() ||
            lookup.IgnoresLigatures() ||
            lookup.IgnoresMarks() ||
            lookup.UsesMarkFilteringSet() ||
            lookup.GetMarkAttachmentType() != 0;

        if (usesGlyphClasses &&
            (gdef == null || gdef.GetGlyphClasses() == null))
        {
            throw FontFormatException.Create(
                "A retained layout lookup requires GDEF glyph classes.");
        }

        if (lookup.GetMarkAttachmentType() != 0 &&
            (gdef == null || gdef.GetMarkAttachmentClasses() == null))
        {
            throw FontFormatException.Create(
                "A retained layout lookup requires GDEF mark attachment classes.");
        }

        if (!lookup.UsesMarkFilteringSet())
        {
            return;
        }

        System.ArgumentNullException.ThrowIfNull(gdef);
        SfntGdefMarkGlyphSets? sets = gdef.GetMarkGlyphSets();
        int setIndex = lookup.GetMarkFilteringSet();

        if (sets == null || setIndex < 0 || setIndex >= sets.GetSetCount())
        {
            throw FontFormatException.Create(
                "A retained layout lookup references an invalid GDEF mark glyph set.");
        }

        AddEntry(gdefMarkGlyphSets, setIndex);
    }

    bool AddGlyph(int glyphId)
    {
        if (glyphIds.Contains(glyphId))
        {
            return false;
        }

        if (glyphIds.Count >= maximumEntries)
        {
            throw FontLimitException.Create(
                "Layout glyph closure exceeds the configured entry limit.");
        }

        glyphIds.Add(glyphId);
        return true;
    }

    void AddEntry(
        System.Collections.Generic.HashSet<int> values,
        int value)
    {
        if (!values.Contains(value))
        {
            if (values.Count >= maximumEntries)
            {
                throw FontLimitException.Create(
                    "Layout closure exceeds the configured entry limit.");
            }

            values.Add(value);
        }
    }

    void EnsureNesting(int depth)
    {
        if (depth >= maximumNesting)
        {
            throw FontLimitException.Create(
                "Layout subset closure exceeds the configured nesting limit.");
        }
    }

    static void SelectFeatures(
        SfntLayoutTable table,
        int[] featureTags,
        System.Collections.Generic.HashSet<int> selected)
    {
        SfntLayoutFeatureList features = table.GetFeatureList();
        int featureIndex = 0;

        while (featureIndex < features.GetFeatureCount())
        {
            if (featureTags.Length == 0 ||
                Contains(featureTags,
                    features.GetFeature(featureIndex).GetFeatureTag()))
            {
                selected.Add(featureIndex);
            }

            featureIndex++;
        }

        AddRequiredFeatures(table.GetScriptList(), selected);
    }

    static void AddRequiredFeatures(
        SfntLayoutScriptList scripts,
        System.Collections.Generic.HashSet<int> selected)
    {
        int scriptIndex = 0;

        while (scriptIndex < scripts.GetScriptCount())
        {
            SfntLayoutScript script = scripts.GetScript(scriptIndex);
            AddRequiredFeature(
                script.GetDefaultLanguageSystem(),
                selected);
            int languageIndex = 0;

            while (languageIndex < script.GetLanguageSystemCount())
            {
                AddRequiredFeature(
                    script.GetLanguageSystem(languageIndex),
                    selected);
                languageIndex++;
            }

            scriptIndex++;
        }
    }

    static void AddRequiredFeature(
        SfntLayoutLanguageSystem? language,
        System.Collections.Generic.HashSet<int> selected)
    {
        if (language != null && language.HasRequiredFeature())
        {
            selected.Add(language.GetRequiredFeatureIndex());
        }
    }

    static bool Contains(int[] values, int value)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static int[] ToSortedArray(
        System.Collections.Generic.HashSet<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result);
        System.Array.Sort(result);
        return result;
    }

    static FontSubsetLayoutClosure Empty(
        bool applicable,
        int[] glyphIds)
    {
        return new FontSubsetLayoutClosure(
            applicable,
            false,
            glyphIds,
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }
}
