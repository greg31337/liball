namespace ttf;

internal class FontSubsetLayoutData
{
    byte[]? gsub;
    byte[]? gpos;
    byte[]? gdef;

    internal FontSubsetLayoutData(byte[]? gsub, byte[]? gpos, byte[]? gdef)
    {
        this.gsub = CopyOptional(gsub);
        this.gpos = CopyOptional(gpos);
        this.gdef = CopyOptional(gdef);
    }

    internal bool HasGsub() { return gsub != null; }
    internal bool HasGpos() { return gpos != null; }
    internal bool HasGdef() { return gdef != null; }
    internal byte[]? CopyGsub() { return CopyOptional(gsub); }
    internal byte[]? CopyGpos() { return CopyOptional(gpos); }
    internal byte[]? CopyGdef() { return CopyOptional(gdef); }

    static byte[]? CopyOptional(byte[]? value)
    {
        return value == null ? null : value.AsSpan().ToArray();
    }
}

internal static class FontSubsetLayoutBuilder
{
    internal static FontSubsetLayoutData Create(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        FontSubsetLayoutClosure closure)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (oldToNewGlyphIds == null || closure == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (!closure.IsApplicable())
        {
            return new FontSubsetLayoutData(null, null, null);
        }

        FontFace face = instance.GetFace();
        FontLimits limits = face.GetTableCache().GetLimits();
        SfntGsubTable? gsub = face.GetGsubTable();
        SfntGposTable? gpos = face.GetGposTable();
        SfntGdefTable? gdef = face.GetGdefTable();
        byte[]? gsubBytes = gsub == null ? null : CreateGsub(
            gsub,
            oldToNewGlyphIds,
            closure.CopyGsubFeatureIndices(),
            closure.CopyGsubLookupIndices(),
            limits);
        byte[]? gposBytes = gpos == null ? null : CreateGpos(
            gpos,
            oldToNewGlyphIds,
            closure.CopyGposFeatureIndices(),
            closure.CopyGposLookupIndices(),
            limits);
        byte[]? gdefBytes = !closure.RetainsGdef() || gdef == null ? null :
            CreateGdef(
                gdef,
                FindRawTable(face, FontTags.Gdef),
                oldToNewGlyphIds,
                closure.CopyGdefMarkGlyphSetIndices(),
                limits);
        return new FontSubsetLayoutData(gsubBytes, gposBytes, gdefBytes);
    }

    static byte[]? FindRawTable(FontFace face, int tag)
    {
        SfntTableData? table = face.GetTableStore().FindTable(tag);
        return table == null ? null : table.GetDataSlice().CopyBytes();
    }

    static byte[] CreateGsub(
        SfntGsubTable table,
        int[] glyphMap,
        int[] retainedFeatures,
        int[] retainedLookups,
        FontLimits limits)
    {
        retainedLookups = FilterEffectiveGsubLookups(
            table, glyphMap, retainedLookups);
        int[] lookupMap = CreateIndexMap(
            table.GetLookupCount(), retainedLookups);
        byte[][] lookups = new byte[retainedLookups.Length][];
        int index = 0;

        while (index < lookups.Length)
        {
            lookups[index] = CreateGsubLookup(
                table.GetLookup(retainedLookups[index]),
                glyphMap,
                lookupMap,
                limits);
            index += 1;
        }

        return CreatePrunedLayout(
            table.GetLayoutTable(),
            retainedFeatures,
            lookupMap,
            lookups,
            limits);
    }

    static byte[] CreateGpos(
        SfntGposTable table,
        int[] glyphMap,
        int[] retainedFeatures,
        int[] retainedLookups,
        FontLimits limits)
    {
        retainedLookups = FilterEffectiveGposLookups(
            table, glyphMap, retainedLookups);
        int[] lookupMap = CreateIndexMap(
            table.GetLookupCount(), retainedLookups);
        byte[][] lookups = new byte[retainedLookups.Length][];
        int index = 0;

        while (index < lookups.Length)
        {
            lookups[index] = CreateGposLookup(
                table.GetLookup(retainedLookups[index]),
                glyphMap,
                lookupMap,
                limits);
            index += 1;
        }

        return CreatePrunedLayout(
            table.GetLayoutTable(),
            retainedFeatures,
            lookupMap,
            lookups,
            limits);
    }

    static byte[] CreateLookupList(byte[][] lookups, FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)lookups.Length);
        int offset = 2 + lookups.Length * 2;
        int index = 0;

        while (index < lookups.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, lookups[index].Length, limits);
            index += 1;
        }

        index = 0;
        while (index < lookups.Length)
        {
            writer.WriteBytes(lookups[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static byte[] CreatePrunedLayout(
        SfntLayoutTable table,
        int[] retainedFeatures,
        int[] lookupMap,
        byte[][] lookups,
        FontLimits limits)
    {
        int[] featureIndices = FilterFeatures(
            table.GetFeatureList(), retainedFeatures, lookupMap);
        int[] featureMap = CreateIndexMap(
            table.GetFeatureList().GetFeatureCount(), featureIndices);
        byte[] scripts = CreateScriptList(
            table.GetScriptList(), featureMap, limits);
        byte[] features = CreateFeatureList(
            table.GetFeatureList(), featureIndices, lookupMap, limits);
        byte[] lookupList = CreateLookupList(lookups, limits);
        int scriptOffset = 10;
        int featureOffset = scriptOffset + scripts.Length;
        int lookupOffset = featureOffset + features.Length;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        WriteOffset16(writer, scriptOffset);
        WriteOffset16(writer, featureOffset);
        WriteOffset16(writer, lookupOffset);
        writer.WriteBytes(scripts);
        writer.WriteBytes(features);
        writer.WriteBytes(lookupList);
        return writer.CopyBytes();
    }

    static int[] FilterFeatures(
        SfntLayoutFeatureList features,
        int[] retainedFeatures,
        int[] lookupMap)
    {
        System.Collections.Generic.List<int> output =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < retainedFeatures.Length)
        {
            int featureIndex = retainedFeatures[index];
            if (featureIndex >= 0 &&
                featureIndex < features.GetFeatureCount() &&
                HasMappedLookup(
                    features.GetFeature(featureIndex), lookupMap))
            {
                output.Add(featureIndex);
            }
            index += 1;
        }
        return output.ToArray();
    }

    static bool HasMappedLookup(
        SfntLayoutFeature feature,
        int[] lookupMap)
    {
        int index = 0;
        while (index < feature.GetLookupIndexCount())
        {
            if (MapIndex(feature.GetLookupIndex(index), lookupMap) >= 0)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static byte[] CreateFeatureList(
        SfntLayoutFeatureList features,
        int[] retained,
        int[] lookupMap,
        FontLimits limits)
    {
        byte[][] records = new byte[retained.Length][];
        int index = 0;
        while (index < retained.Length)
        {
            records[index] = CreateFeature(
                features.GetFeature(retained[index]), lookupMap, limits);
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)records.Length);
        int offset = 2 + records.Length * 6;
        index = 0;
        while (index < records.Length)
        {
            writer.WriteUInt32(CheckedFontMath.ReinterpretInt32AsUInt32(
                features.GetFeature(retained[index]).GetFeatureTag()));
            WriteOffset16(writer, offset);
            offset = AddLength(offset, records[index].Length, limits);
            index += 1;
        }
        WriteBlobs(writer, records);
        return writer.CopyBytes();
    }

    static byte[] CreateFeature(
        SfntLayoutFeature feature,
        int[] lookupMap,
        FontLimits limits)
    {
        int count = CountMappedFeatureLookups(feature, lookupMap);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(0);
        writer.WriteUInt16((ushort)count);
        int index = 0;
        while (index < feature.GetLookupIndexCount())
        {
            int mapped = MapIndex(
                feature.GetLookupIndex(index), lookupMap);
            if (mapped >= 0) { writer.WriteUInt16((ushort)mapped); }
            index += 1;
        }
        return writer.CopyBytes();
    }

    static int CountMappedFeatureLookups(
        SfntLayoutFeature feature,
        int[] lookupMap)
    {
        int count = 0;
        int index = 0;
        while (index < feature.GetLookupIndexCount())
        {
            if (MapIndex(feature.GetLookupIndex(index), lookupMap) >= 0)
            {
                count += 1;
            }
            index += 1;
        }
        return count;
    }

    static byte[] CreateScriptList(
        SfntLayoutScriptList scripts,
        int[] featureMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<SfntLayoutScript> retained =
            new System.Collections.Generic.List<SfntLayoutScript>();
        System.Collections.Generic.List<byte[]> records =
            new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < scripts.GetScriptCount())
        {
            SfntLayoutScript script = scripts.GetScript(index);
            byte[]? record = CreateScript(script, featureMap, limits);
            if (record != null)
            {
                retained.Add(script);
                records.Add(record);
            }
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)records.Count);
        int offset = 2 + records.Count * 6;
        index = 0;
        while (index < records.Count)
        {
            writer.WriteUInt32(CheckedFontMath.ReinterpretInt32AsUInt32(
                retained[index].GetScriptTag()));
            WriteOffset16(writer, offset);
            offset = AddLength(offset, records[index].Length, limits);
            index += 1;
        }
        WriteList(writer, records);
        return writer.CopyBytes();
    }

    static byte[]? CreateScript(
        SfntLayoutScript script,
        int[] featureMap,
        FontLimits limits)
    {
        byte[]? defaultLanguage = CreateLanguageSystem(
            script.GetDefaultLanguageSystem(), featureMap, limits);
        System.Collections.Generic.List<SfntLayoutLanguageSystem> languages =
            new System.Collections.Generic.List<SfntLayoutLanguageSystem>();
        System.Collections.Generic.List<byte[]> records =
            new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < script.GetLanguageSystemCount())
        {
            SfntLayoutLanguageSystem language =
                script.GetLanguageSystem(index);
            byte[]? record = CreateLanguageSystem(
                language, featureMap, limits);
            if (record != null)
            {
                languages.Add(language);
                records.Add(record);
            }
            index += 1;
        }
        if (defaultLanguage == null && records.Count == 0) { return null; }
        int headerLength = 4 + records.Count * 6;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(defaultLanguage == null ? (ushort)0 :
            (ushort)headerLength);
        writer.WriteUInt16((ushort)records.Count);
        int offset = headerLength +
            (defaultLanguage == null ? 0 : defaultLanguage.Length);
        index = 0;
        while (index < records.Count)
        {
            writer.WriteUInt32(CheckedFontMath.ReinterpretInt32AsUInt32(
                languages[index].GetLanguageTag()));
            WriteOffset16(writer, offset);
            offset = AddLength(offset, records[index].Length, limits);
            index += 1;
        }
        if (defaultLanguage != null) { writer.WriteBytes(defaultLanguage); }
        WriteList(writer, records);
        return writer.CopyBytes();
    }

    static byte[]? CreateLanguageSystem(
        SfntLayoutLanguageSystem? language,
        int[] featureMap,
        FontLimits limits)
    {
        if (language == null) { return null; }
        int required = language.HasRequiredFeature() ?
            MapIndex(language.GetRequiredFeatureIndex(), featureMap) : -1;
        int count = 0;
        int index = 0;
        while (index < language.GetFeatureIndexCount())
        {
            if (MapIndex(language.GetFeatureIndex(index), featureMap) >= 0)
            {
                count += 1;
            }
            index += 1;
        }
        if (required < 0 && count == 0) { return null; }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(0);
        writer.WriteUInt16(required < 0 ? (ushort)0xFFFF :
            (ushort)required);
        writer.WriteUInt16((ushort)count);
        index = 0;
        while (index < language.GetFeatureIndexCount())
        {
            int mapped = MapIndex(
                language.GetFeatureIndex(index), featureMap);
            if (mapped >= 0) { writer.WriteUInt16((ushort)mapped); }
            index += 1;
        }
        return writer.CopyBytes();
    }

    static int[] CreateIndexMap(int count, int[] retained)
    {
        int[] map = new int[count];
        int index = 0;
        while (index < map.Length)
        {
            map[index] = -1;
            index += 1;
        }
        index = 0;
        while (index < retained.Length)
        {
            int oldIndex = retained[index];
            if (oldIndex < 0 || oldIndex >= count)
            {
                throw FontFormatException.Create(
                    "A retained layout index is invalid.");
            }
            map[oldIndex] = index;
            index += 1;
        }
        return map;
    }

    static int MapIndex(int index, int[] map)
    {
        return index < 0 || index >= map.Length ? -1 : map[index];
    }

    static int[] FilterEffectiveGsubLookups(
        SfntGsubTable table,
        int[] glyphMap,
        int[] retained)
    {
        bool[] effective = new bool[table.GetLookupCount()];
        bool changed = true;
        while (changed)
        {
            changed = false;
            int index = 0;
            while (index < retained.Length)
            {
                int lookupIndex = retained[index];
                if (!effective[lookupIndex] && HasGsubContent(
                    table.GetLookup(lookupIndex), glyphMap, effective))
                {
                    effective[lookupIndex] = true;
                    changed = true;
                }
                index += 1;
            }
        }
        return CopyEffectiveIndices(retained, effective);
    }

    static int[] FilterEffectiveGposLookups(
        SfntGposTable table,
        int[] glyphMap,
        int[] retained)
    {
        bool[] effective = new bool[table.GetLookupCount()];
        bool changed = true;
        while (changed)
        {
            changed = false;
            int index = 0;
            while (index < retained.Length)
            {
                int lookupIndex = retained[index];
                if (!effective[lookupIndex] && HasGposContent(
                    table.GetLookup(lookupIndex), glyphMap, effective))
                {
                    effective[lookupIndex] = true;
                    changed = true;
                }
                index += 1;
            }
        }
        return CopyEffectiveIndices(retained, effective);
    }

    static int[] CopyEffectiveIndices(int[] retained, bool[] effective)
    {
        System.Collections.Generic.List<int> output =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < retained.Length)
        {
            int value = retained[index];
            if (value >= 0 && value < effective.Length && effective[value])
            {
                output.Add(value);
            }
            index += 1;
        }
        return output.ToArray();
    }

    static bool HasGsubContent(
        SfntGsubLookup lookup,
        int[] glyphMap,
        bool[] effective)
    {
        int type = lookup.GetLookupType();
        int index = 0;
        while (index < lookup.GetSubtableCount())
        {
            if (type == 1 && HasSingleContent(
                lookup.GetSingleSubstitution(index), glyphMap))
            {
                return true;
            }
            if (type == 2 && HasMultipleContent(
                lookup.GetMultipleSubstitution(index), glyphMap))
            {
                return true;
            }
            if (type == 3 && HasAlternateContent(
                lookup.GetAlternateSubstitution(index), glyphMap))
            {
                return true;
            }
            if (type == 4 && HasLigatureContent(
                lookup.GetLigatureSubstitution(index), glyphMap))
            {
                return true;
            }
            if (type == 5 && HasContextContent(
                lookup.GetContextSubstitution(index), glyphMap, effective))
            {
                return true;
            }
            if (type == 6 && HasChainedContextContent(
                lookup.GetChainedContextSubstitution(index),
                glyphMap,
                effective))
            {
                return true;
            }
            if (type == 8 && HasReverseContent(
                lookup.GetReverseSubstitution(index), glyphMap))
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasGposContent(
        SfntGposLookup lookup,
        int[] glyphMap,
        bool[] effective)
    {
        int type = lookup.GetLookupType();
        int index = 0;
        while (index < lookup.GetSubtableCount())
        {
            if (type == 1 && HasMappedCoverage(
                lookup.GetSingleAdjustment(index).GetCoverage(), glyphMap))
            {
                return true;
            }
            if (type == 2 && HasPairContent(
                lookup.GetPairAdjustment(index), glyphMap))
            {
                return true;
            }
            if (type == 3 && HasMappedCoverage(
                lookup.GetCursiveAdjustment(index).GetCoverage(), glyphMap))
            {
                return true;
            }
            if ((type == 4 || type == 6) && HasMarkContent(
                type == 4 ? lookup.GetMarkBaseAdjustment(index) :
                    lookup.GetMarkMarkAdjustment(index), glyphMap))
            {
                return true;
            }
            if (type == 5 && HasMarkLigatureContent(
                lookup.GetMarkLigatureAdjustment(index), glyphMap))
            {
                return true;
            }
            if (type == 7 && HasContextContent(
                lookup.GetContextAdjustment(index), glyphMap, effective))
            {
                return true;
            }
            if (type == 8 && HasChainedContextContent(
                lookup.GetChainedContextAdjustment(index),
                glyphMap,
                effective))
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasSingleContent(
        SfntGsubSingleSubstitution value,
        int[] glyphMap)
    {
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            int glyph = coverage.GetGlyphId(index);
            if (MapGlyph(glyph, glyphMap) >= 0 && MapGlyph(
                value.GetSubstituteGlyphId(glyph), glyphMap) >= 0)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasMultipleContent(
        SfntGsubMultipleSubstitution value,
        int[] glyphMap)
    {
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            if (MapGlyph(coverage.GetGlyphId(index), glyphMap) >= 0 &&
                MapGlyphs(value.GetSequence(index).CopyGlyphIds(), glyphMap)
                != null)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasAlternateContent(
        SfntGsubAlternateSubstitution value,
        int[] glyphMap)
    {
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            if (MapGlyph(coverage.GetGlyphId(index), glyphMap) >= 0)
            {
                SfntGsubAlternateSet set = value.GetAlternateSet(index);
                int alternate = 0;
                while (alternate < set.GetGlyphCount())
                {
                    if (MapGlyph(set.GetGlyphId(alternate), glyphMap) >= 0)
                    {
                        return true;
                    }
                    alternate += 1;
                }
            }
            index += 1;
        }
        return false;
    }

    static bool HasLigatureContent(
        SfntGsubLigatureSubstitution value,
        int[] glyphMap)
    {
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            if (MapGlyph(coverage.GetGlyphId(index), glyphMap) >= 0)
            {
                SfntGsubLigatureSet set = value.GetLigatureSet(index);
                int ligature = 0;
                while (ligature < set.GetLigatureCount())
                {
                    if (HasLigatureGlyphContent(
                        set.GetLigature(ligature), glyphMap))
                    {
                        return true;
                    }
                    ligature += 1;
                }
            }
            index += 1;
        }
        return false;
    }

    static bool HasLigatureGlyphContent(
        SfntGsubLigature value,
        int[] glyphMap)
    {
        if (MapGlyph(value.GetLigatureGlyphId(), glyphMap) < 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.GetComponentCount() - 1)
        {
            if (MapGlyph(value.GetComponentGlyphId(index), glyphMap) < 0)
            {
                return false;
            }
            index += 1;
        }
        return true;
    }

    static bool HasReverseContent(
        SfntGsubReverseSubstitution value,
        int[] glyphMap)
    {
        if (!AllReverseCoveragesMapped(value, glyphMap)) { return false; }
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            int glyph = coverage.GetGlyphId(index);
            if (MapGlyph(glyph, glyphMap) >= 0 && MapGlyph(
                value.GetSubstituteGlyphId(glyph), glyphMap) >= 0)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool AllReverseCoveragesMapped(
        SfntGsubReverseSubstitution value,
        int[] glyphMap)
    {
        int index = 0;
        while (index < value.GetBacktrackCoverageCount())
        {
            if (!HasMappedCoverage(
                value.GetBacktrackCoverage(index), glyphMap))
            {
                return false;
            }
            index += 1;
        }
        index = 0;
        while (index < value.GetLookaheadCoverageCount())
        {
            if (!HasMappedCoverage(
                value.GetLookaheadCoverage(index), glyphMap))
            {
                return false;
            }
            index += 1;
        }
        return true;
    }

    static bool HasPairContent(
        SfntGposPairAdjustment value,
        int[] glyphMap)
    {
        if (!HasMappedCoverage(value.GetCoverage(), glyphMap))
        {
            return false;
        }
        if (value.GetFormat() == 2) { return true; }
        int index = 0;
        while (index < value.GetPairSetCount())
        {
            SfntGposPairSet set = value.GetPairSet(index);
            int pair = 0;
            while (pair < set.GetPairValueCount())
            {
                if (MapGlyph(set.GetPairValue(pair).GetSecondGlyphId(),
                    glyphMap) >= 0)
                {
                    return true;
                }
                pair += 1;
            }
            index += 1;
        }
        return false;
    }

    static bool HasMarkContent(
        SfntGposMarkAttachment value,
        int[] glyphMap)
    {
        return HasMappedCoverage(value.GetMarkCoverage(), glyphMap) &&
            HasMappedCoverage(value.GetBaseCoverage(), glyphMap);
    }

    static bool HasMarkLigatureContent(
        SfntGposMarkLigatureAdjustment value,
        int[] glyphMap)
    {
        return HasMappedCoverage(value.GetMarkCoverage(), glyphMap) &&
            HasMappedCoverage(value.GetLigatureCoverage(), glyphMap);
    }

    static bool HasContextContent(
        SfntGsubContextSubstitution value,
        int[] glyphMap,
        bool[] effective)
    {
        if (value.GetFormat() == 3)
        {
            int index = 0;
            while (index < value.GetInputCoverageCount())
            {
                if (!HasMappedCoverage(
                    value.GetInputCoverage(index), glyphMap))
                {
                    return false;
                }
                index += 1;
            }
            return HasEffectiveLookupRecord(
                value.GetLookupRecords(), effective);
        }
        SfntCoverageTable? coverage = value.GetCoverage();
        if (value.GetFormat() != 2)
        {
            System.ArgumentNullException.ThrowIfNull(coverage);
        }
        int setIndex = 0;
        while (setIndex < value.GetRuleSetCount())
        {
            SfntGsubContextRuleSet set = value.GetRuleSet(setIndex);
            bool firstMapped = true;
            if (value.GetFormat() != 2)
            {
                System.ArgumentNullException.ThrowIfNull(coverage);
                firstMapped = setIndex < coverage.GetCoveredGlyphCount() &&
                    MapGlyph(coverage.GetGlyphId(setIndex), glyphMap) >= 0;
            }
            if (firstMapped && HasContextRuleSetContent(
                set, glyphMap, value.GetFormat() == 1, effective))
            {
                return true;
            }
            setIndex += 1;
        }
        return false;
    }

    static bool HasContextRuleSetContent(
        SfntGsubContextRuleSet set,
        int[] glyphMap,
        bool glyphValues,
        bool[] effective)
    {
        if (set == null) { return false; }
        int index = 0;
        while (index < set.GetRuleCount())
        {
            SfntGsubContextRule rule = set.GetRule(index);
            bool mapped = true;
            int input = 0;
            while (glyphValues && input < rule.GetGlyphCount() - 1)
            {
                if (MapGlyph(rule.GetInputValue(input), glyphMap) < 0)
                {
                    mapped = false;
                }
                input += 1;
            }
            if (mapped && HasEffectiveLookupRecord(
                rule.GetLookupRecords(), effective))
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasChainedContextContent(
        SfntGsubChainedContextSubstitution value,
        int[] glyphMap,
        bool[] effective)
    {
        if (value.GetFormat() == 3)
        {
            if (!AllChainedCoveragesMapped(value, glyphMap)) { return false; }
            return HasEffectiveLookupRecord(
                value.GetLookupRecords(), effective);
        }
        SfntCoverageTable? coverage = value.GetCoverage();
        if (value.GetFormat() != 2)
        {
            System.ArgumentNullException.ThrowIfNull(coverage);
        }
        int setIndex = 0;
        while (setIndex < value.GetRuleSetCount())
        {
            SfntGsubChainedRuleSet set = value.GetRuleSet(setIndex);
            bool firstMapped = true;
            if (value.GetFormat() != 2)
            {
                System.ArgumentNullException.ThrowIfNull(coverage);
                firstMapped = setIndex < coverage.GetCoveredGlyphCount() &&
                    MapGlyph(coverage.GetGlyphId(setIndex), glyphMap) >= 0;
            }
            if (firstMapped && HasChainedRuleSetContent(
                set, glyphMap, value.GetFormat() == 1, effective))
            {
                return true;
            }
            setIndex += 1;
        }
        return false;
    }

    static bool AllChainedCoveragesMapped(
        SfntGsubChainedContextSubstitution value,
        int[] glyphMap)
    {
        int index = 0;
        while (index < value.GetBacktrackCoverageCount())
        {
            if (!HasMappedCoverage(
                value.GetBacktrackCoverage(index), glyphMap))
            {
                return false;
            }
            index += 1;
        }
        index = 0;
        while (index < value.GetInputCoverageCount())
        {
            if (!HasMappedCoverage(value.GetInputCoverage(index), glyphMap))
            {
                return false;
            }
            index += 1;
        }
        index = 0;
        while (index < value.GetLookaheadCoverageCount())
        {
            if (!HasMappedCoverage(
                value.GetLookaheadCoverage(index), glyphMap))
            {
                return false;
            }
            index += 1;
        }
        return true;
    }

    static bool HasChainedRuleSetContent(
        SfntGsubChainedRuleSet set,
        int[] glyphMap,
        bool glyphValues,
        bool[] effective)
    {
        if (set == null) { return false; }
        int index = 0;
        while (index < set.GetRuleCount())
        {
            SfntGsubChainedRule rule = set.GetRule(index);
            if (HasChainedRuleContent(
                rule, glyphMap, glyphValues, effective))
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasChainedRuleContent(
        SfntGsubChainedRule rule,
        int[] glyphMap,
        bool glyphValues,
        bool[] effective)
    {
        if (glyphValues)
        {
            int index = 0;
            while (index < rule.GetBacktrackCount())
            {
                if (MapGlyph(rule.GetBacktrackValue(index), glyphMap) < 0)
                {
                    return false;
                }
                index += 1;
            }
            index = 0;
            while (index < rule.GetInputGlyphCount() - 1)
            {
                if (MapGlyph(rule.GetInputValue(index), glyphMap) < 0)
                {
                    return false;
                }
                index += 1;
            }
            index = 0;
            while (index < rule.GetLookaheadCount())
            {
                if (MapGlyph(rule.GetLookaheadValue(index), glyphMap) < 0)
                {
                    return false;
                }
                index += 1;
            }
        }
        return HasEffectiveLookupRecord(rule.GetLookupRecords(), effective);
    }

    static bool HasEffectiveLookupRecord(
        SfntSequenceLookupRecords? records,
        bool[] effective)
    {
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(records);
        while (index < records.GetRecordCount())
        {
            int lookup = records.GetRecord(index).GetLookupListIndex();
            if (lookup >= 0 && lookup < effective.Length && effective[lookup])
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static bool HasMappedCoverage(
        SfntCoverageTable coverage,
        int[] glyphMap)
    {
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            if (MapGlyph(coverage.GetGlyphId(index), glyphMap) >= 0)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static byte[] CreateNoOpLookup(bool substitution, FontLimits limits)
    {
        byte[] subtable = substitution ?
            Join(limits, new byte[][]
            {
                U16(2), U16(8), U16(0), U16(0), U16(1), U16(0)
            }) :
            Join(limits, new byte[][]
            {
                U16(1), U16(6), U16(0), U16(1), U16(0)
            });
        return CreateLookup(1, 0, 0, new byte[][] { subtable }, limits);
    }

    static byte[] CreateLookup(
        int lookupType,
        int flags,
        int markFilteringSet,
        byte[][] subtables,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)lookupType);
        writer.WriteUInt16((ushort)flags);
        writer.WriteUInt16((ushort)subtables.Length);
        int offset = 6 + subtables.Length * 2 +
            ((flags & 0x0010) == 0 ? 0 : 2);
        int index = 0;

        while (index < subtables.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, subtables[index].Length, limits);
            index += 1;
        }

        if ((flags & 0x0010) != 0)
        {
            writer.WriteUInt16((ushort)markFilteringSet);
        }

        index = 0;
        while (index < subtables.Length)
        {
            writer.WriteBytes(subtables[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static byte[] CreateCoverage(
        SfntCoverageTable? coverage,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> glyphs =
            new System.Collections.Generic.List<int>();
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(coverage);
        while (index < coverage.GetCoveredGlyphCount())
        {
            int mapped = MapGlyph(coverage.GetGlyphId(index), glyphMap);
            if (mapped >= 0) { glyphs.Add(mapped); }
            index += 1;
        }

        return CreateCoverage(glyphs.ToArray(), limits);
    }

    static byte[] CreateCoverage(int[] glyphs, FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        writer.WriteUInt16((ushort)glyphs.Length);
        int index = 0;

        while (index < glyphs.Length)
        {
            writer.WriteUInt16((ushort)glyphs[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static byte[] CreateClassDefinition(
        SfntClassDefinitionTable? classes,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> ends =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> values =
            new System.Collections.Generic.List<int>();
        int oldGlyph = 0;

        while (oldGlyph < glyphMap.Length)
        {
            int mapped = glyphMap[oldGlyph];
            System.ArgumentNullException.ThrowIfNull(classes);
            int value = classes.GetClassForGlyph(oldGlyph);

            if (mapped >= 0 && value != 0)
            {
                int last = starts.Count - 1;
                if (last >= 0 && ends[last] + 1 == mapped &&
                    values[last] == value)
                {
                    ends[last] = mapped;
                }
                else
                {
                    starts.Add(mapped);
                    ends.Add(mapped);
                    values.Add(value);
                }
            }

            oldGlyph += 1;
        }

        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(2);
        writer.WriteUInt16((ushort)starts.Count);
        int index = 0;
        while (index < starts.Count)
        {
            writer.WriteUInt16((ushort)starts[index]);
            writer.WriteUInt16((ushort)ends[index]);
            writer.WriteUInt16((ushort)values[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateGsubLookup(
        SfntGsubLookup lookup,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        byte[][] subtables = new byte[lookup.GetSubtableCount()][];
        int type = lookup.GetLookupType();
        int index = 0;

        while (index < subtables.Length)
        {
            if (type == 1)
            {
                subtables[index] = CreateSingleSubstitution(
                    lookup.GetSingleSubstitution(index), glyphMap, limits);
            }
            else if (type == 2)
            {
                subtables[index] = CreateMultipleSubstitution(
                    lookup.GetMultipleSubstitution(index), glyphMap, limits);
            }
            else if (type == 3)
            {
                subtables[index] = CreateAlternateSubstitution(
                    lookup.GetAlternateSubstitution(index), glyphMap, limits);
            }
            else if (type == 4)
            {
                subtables[index] = CreateLigatureSubstitution(
                    lookup.GetLigatureSubstitution(index), glyphMap, limits);
            }
            else if (type == 5)
            {
                subtables[index] = CreateContext(
                    lookup.GetContextSubstitution(index),
                    glyphMap,
                    lookupMap,
                    limits);
            }
            else if (type == 6)
            {
                subtables[index] = CreateChainedContext(
                    lookup.GetChainedContextSubstitution(index),
                    glyphMap,
                    lookupMap,
                    limits);
            }
            else if (type == 8)
            {
                subtables[index] = CreateReverseSubstitution(
                    lookup.GetReverseSubstitution(index), glyphMap, limits);
            }
            else
            {
                return CreateNoOpLookup(true, limits);
            }

            index += 1;
        }

        return CreateLookup(
            type,
            lookup.GetLayoutLookup().GetLookupFlag(),
            lookup.GetLayoutLookup().GetMarkFilteringSet(),
            subtables,
            limits);
    }

    static byte[] CreateSingleSubstitution(
        SfntGsubSingleSubstitution value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> inputs =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> outputs =
            new System.Collections.Generic.List<int>();
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;

        while (index < coverage.GetCoveredGlyphCount())
        {
            int oldInput = coverage.GetGlyphId(index);
            int input = MapGlyph(oldInput, glyphMap);
            int output = MapGlyph(
                value.GetSubstituteGlyphId(oldInput), glyphMap);
            if (input >= 0 && output >= 0)
            {
                inputs.Add(input);
                outputs.Add(output);
            }
            index += 1;
        }

        byte[] coverageBytes = CreateCoverage(inputs.ToArray(), limits);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(2);
        WriteOffset16(writer, 6 + outputs.Count * 2);
        writer.WriteUInt16((ushort)outputs.Count);
        index = 0;
        while (index < outputs.Count)
        {
            writer.WriteUInt16((ushort)outputs[index]);
            index += 1;
        }
        writer.WriteBytes(coverageBytes);
        return writer.CopyBytes();
    }

    static byte[] CreateMultipleSubstitution(
        SfntGsubMultipleSubstitution value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> inputs =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<byte[]> sequences =
            new System.Collections.Generic.List<byte[]>();
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;

        while (index < coverage.GetCoveredGlyphCount())
        {
            int input = MapGlyph(coverage.GetGlyphId(index), glyphMap);
            SfntGsubGlyphSequence sequence = value.GetSequence(index);
            int[]? mapped = MapGlyphs(sequence.CopyGlyphIds(), glyphMap);
            if (input >= 0 && mapped != null)
            {
                inputs.Add(input);
                sequences.Add(CreateGlyphSequence(mapped, limits));
            }
            index += 1;
        }

        return CreateOffsetArraySubtable(
            1, inputs.ToArray(), sequences.ToArray(), limits);
    }

    static byte[] CreateAlternateSubstitution(
        SfntGsubAlternateSubstitution value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> inputs =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<byte[]> sets =
            new System.Collections.Generic.List<byte[]>();
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;

        while (index < coverage.GetCoveredGlyphCount())
        {
            int input = MapGlyph(coverage.GetGlyphId(index), glyphMap);
            SfntGsubAlternateSet set = value.GetAlternateSet(index);
            System.Collections.Generic.List<int> mapped =
                new System.Collections.Generic.List<int>();
            int glyphIndex = 0;
            while (glyphIndex < set.GetGlyphCount())
            {
                int glyph = MapGlyph(set.GetGlyphId(glyphIndex), glyphMap);
                if (glyph >= 0) { mapped.Add(glyph); }
                glyphIndex += 1;
            }
            if (input >= 0 && mapped.Count != 0)
            {
                inputs.Add(input);
                sets.Add(CreateGlyphSequence(mapped.ToArray(), limits));
            }
            index += 1;
        }

        return CreateOffsetArraySubtable(
            1, inputs.ToArray(), sets.ToArray(), limits);
    }

    static byte[] CreateLigatureSubstitution(
        SfntGsubLigatureSubstitution value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> inputs =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<byte[]> sets =
            new System.Collections.Generic.List<byte[]>();
        SfntCoverageTable coverage = value.GetCoverage();
        int coverageIndex = 0;

        while (coverageIndex < coverage.GetCoveredGlyphCount())
        {
            int input = MapGlyph(
                coverage.GetGlyphId(coverageIndex), glyphMap);
            SfntGsubLigatureSet set = value.GetLigatureSet(coverageIndex);
            System.Collections.Generic.List<byte[]> ligatures =
                new System.Collections.Generic.List<byte[]>();
            int ligatureIndex = 0;
            while (ligatureIndex < set.GetLigatureCount())
            {
                SfntGsubLigature ligature =
                    set.GetLigature(ligatureIndex);
                int output = MapGlyph(
                    ligature.GetLigatureGlyphId(), glyphMap);
                int[] components = new int[
                    ligature.GetComponentCount() - 1];
                int componentIndex = 0;
                bool complete = output >= 0;
                while (componentIndex < components.Length)
                {
                    components[componentIndex] = MapGlyph(
                        ligature.GetComponentGlyphId(componentIndex),
                        glyphMap);
                    if (components[componentIndex] < 0) { complete = false; }
                    componentIndex += 1;
                }
                if (complete)
                {
                    ligatures.Add(CreateLigature(
                        output, components, limits));
                }
                ligatureIndex += 1;
            }
            if (input >= 0 && ligatures.Count != 0)
            {
                inputs.Add(input);
                sets.Add(CreateOffsetList(ligatures.ToArray(), limits));
            }
            coverageIndex += 1;
        }

        return CreateOffsetArraySubtable(
            1, inputs.ToArray(), sets.ToArray(), limits);
    }

    static byte[] CreateReverseSubstitution(
        SfntGsubReverseSubstitution value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> inputs =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> outputs =
            new System.Collections.Generic.List<int>();
        SfntCoverageTable coverage = value.GetCoverage();
        int index = 0;
        while (index < coverage.GetCoveredGlyphCount())
        {
            int oldGlyph = coverage.GetGlyphId(index);
            int input = MapGlyph(oldGlyph, glyphMap);
            int output = MapGlyph(
                value.GetSubstituteGlyphId(oldGlyph), glyphMap);
            if (input >= 0 && output >= 0)
            {
                inputs.Add(input);
                outputs.Add(output);
            }
            index += 1;
        }
        byte[][] backtrack = CopyCoverages(
            value.GetBacktrackCoverageCount(),
            value,
            null,
            glyphMap,
            true,
            limits);
        byte[][] lookahead = CopyCoverages(
            value.GetLookaheadCoverageCount(),
            value,
            null,
            glyphMap,
            false,
            limits);
        return CreateReverse(
            inputs.ToArray(), outputs.ToArray(), backtrack, lookahead, limits);
    }

    static byte[] CreateGlyphSequence(int[] glyphs, FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)glyphs.Length);
        int index = 0;
        while (index < glyphs.Length)
        {
            writer.WriteUInt16((ushort)glyphs[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateLigature(
        int output,
        int[] components,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)output);
        writer.WriteUInt16((ushort)(components.Length + 1));
        int index = 0;
        while (index < components.Length)
        {
            writer.WriteUInt16((ushort)components[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateOffsetArraySubtable(
        int format,
        int[] coverageGlyphs,
        byte[][] children,
        FontLimits limits)
    {
        byte[] coverage = CreateCoverage(coverageGlyphs, limits);
        int headerLength = 6 + children.Length * 2;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)format);
        WriteOffset16(writer, headerLength);
        writer.WriteUInt16((ushort)children.Length);
        int offset = headerLength + coverage.Length;
        int index = 0;
        while (index < children.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, children[index].Length, limits);
            index += 1;
        }
        writer.WriteBytes(coverage);
        index = 0;
        while (index < children.Length)
        {
            writer.WriteBytes(children[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateOffsetList(byte[][] children, FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)children.Length);
        int offset = 2 + children.Length * 2;
        int index = 0;
        while (index < children.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, children[index].Length, limits);
            index += 1;
        }
        index = 0;
        while (index < children.Length)
        {
            writer.WriteBytes(children[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateGposLookup(
        SfntGposLookup lookup,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        byte[][] subtables = new byte[lookup.GetSubtableCount()][];
        int type = lookup.GetLookupType();
        int index = 0;
        while (index < subtables.Length)
        {
            if (type == 1)
            {
                subtables[index] = CreateSingleAdjustment(
                    lookup.GetSingleAdjustment(index), glyphMap, limits);
            }
            else if (type == 2)
            {
                subtables[index] = CreatePairAdjustment(
                    lookup.GetPairAdjustment(index), glyphMap, limits);
            }
            else if (type == 3)
            {
                subtables[index] = CreateCursiveAdjustment(
                    lookup.GetCursiveAdjustment(index), glyphMap, limits);
            }
            else if (type == 4 || type == 6)
            {
                SfntGposMarkAttachment attachment = type == 4 ?
                    lookup.GetMarkBaseAdjustment(index) :
                    lookup.GetMarkMarkAdjustment(index);
                subtables[index] = CreateMarkAttachment(
                    attachment, glyphMap, limits);
            }
            else if (type == 5)
            {
                subtables[index] = CreateMarkLigatureAdjustment(
                    lookup.GetMarkLigatureAdjustment(index), glyphMap, limits);
            }
            else if (type == 7)
            {
                subtables[index] = CreateContext(
                    lookup.GetContextAdjustment(index),
                    glyphMap,
                    lookupMap,
                    limits);
            }
            else if (type == 8)
            {
                subtables[index] = CreateChainedContext(
                    lookup.GetChainedContextAdjustment(index),
                    glyphMap,
                    lookupMap,
                    limits);
            }
            else
            {
                return CreateNoOpLookup(false, limits);
            }
            index += 1;
        }
        return CreateLookup(
            type,
            lookup.GetLayoutLookup().GetLookupFlag(),
            lookup.GetLayoutLookup().GetMarkFilteringSet(),
            subtables,
            limits);
    }

    static byte[] CreateSingleAdjustment(
        SfntGposSingleAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        int format = value.GetFormat();
        int recordCount = format == 1 ? 1 : retained.Length;
        int fixedLength = 6 + (format == 1 ? 0 : 2) +
            recordCount * GetValueSize(value.GetValueFormat());
        System.Collections.Generic.List<byte[]> adjustments =
            new System.Collections.Generic.List<byte[]>();
        int adjustmentOffset = fixedLength;
        byte[][] records = new byte[recordCount][];
        int index = 0;
        while (index < recordCount)
        {
            int sourceIndex = format == 1 ? 0 : retained[index];
            records[index] = CreateValueRecord(
                value.GetValueRecord(sourceIndex),
                value.GetValueFormat(),
                adjustmentOffset,
                adjustments,
                limits);
            adjustmentOffset += SumLengths(adjustments) -
                (adjustmentOffset - fixedLength);
            index += 1;
        }
        int[] glyphs = MapCoverageAtIndices(
            value.GetCoverage(), retained, glyphMap);
        byte[] coverage = CreateCoverage(glyphs, limits);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)format);
        WriteOffset16(writer, fixedLength + SumLengths(adjustments));
        writer.WriteUInt16((ushort)value.GetValueFormat());
        if (format != 1) { writer.WriteUInt16((ushort)recordCount); }
        WriteBlobs(writer, records);
        WriteList(writer, adjustments);
        writer.WriteBytes(coverage);
        return writer.CopyBytes();
    }

    static byte[] CreatePairAdjustment(
        SfntGposPairAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        return value.GetFormat() == 1 ?
            CreateGlyphPairAdjustment(value, glyphMap, limits) :
            CreateClassPairAdjustment(value, glyphMap, limits);
    }

    static byte[] CreateGlyphPairAdjustment(
        SfntGposPairAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[][] pairSets = new byte[retained.Length][];
        int[] glyphs = MapCoverageAtIndices(
            value.GetCoverage(), retained, glyphMap);
        byte[] coverage = CreateCoverage(glyphs, limits);
        int headerLength = 10 + pairSets.Length * 2;
        int pairSetOffset = headerLength + coverage.Length;
        int index = 0;
        while (index < retained.Length)
        {
            pairSets[index] = CreatePairSet(
                value.GetPairSet(retained[index]),
                value.GetFirstValueFormat(),
                value.GetSecondValueFormat(),
                glyphMap,
                pairSetOffset,
                limits);
            pairSetOffset = AddLength(
                pairSetOffset, pairSets[index].Length, limits);
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        WriteOffset16(writer, headerLength);
        writer.WriteUInt16((ushort)value.GetFirstValueFormat());
        writer.WriteUInt16((ushort)value.GetSecondValueFormat());
        writer.WriteUInt16((ushort)pairSets.Length);
        int offset = headerLength + coverage.Length;
        index = 0;
        while (index < pairSets.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, pairSets[index].Length, limits);
            index += 1;
        }
        writer.WriteBytes(coverage);
        WriteBlobs(writer, pairSets);
        return writer.CopyBytes();
    }

    static byte[] CreatePairSet(
        SfntGposPairSet set,
        int firstFormat,
        int secondFormat,
        int[] glyphMap,
        int parentOffset,
        FontLimits limits)
    {
        int count = 0;
        int index = 0;
        while (index < set.GetPairValueCount())
        {
            if (MapGlyph(set.GetPairValue(index).GetSecondGlyphId(), glyphMap)
                >= 0) { count += 1; }
            index += 1;
        }
        int fixedLength = 2 + count * (2 + GetValueSize(firstFormat) +
            GetValueSize(secondFormat));
        int adjustmentOffset = parentOffset + fixedLength;
        System.Collections.Generic.List<byte[]> adjustments =
            new System.Collections.Generic.List<byte[]>();
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)count);
        index = 0;
        while (index < set.GetPairValueCount())
        {
            SfntGposPairValueRecord record = set.GetPairValue(index);
            int second = MapGlyph(record.GetSecondGlyphId(), glyphMap);
            if (second >= 0)
            {
                writer.WriteUInt16((ushort)second);
                byte[] first = CreateValueRecord(
                    record.GetFirstValueRecord(),
                    firstFormat,
                    adjustmentOffset,
                    adjustments,
                    limits);
                adjustmentOffset = parentOffset + fixedLength +
                    SumLengths(adjustments);
                byte[] secondValue = CreateValueRecord(
                    record.GetSecondValueRecord(),
                    secondFormat,
                    adjustmentOffset,
                    adjustments,
                    limits);
                adjustmentOffset = parentOffset + fixedLength +
                    SumLengths(adjustments);
                writer.WriteBytes(first);
                writer.WriteBytes(secondValue);
            }
            index += 1;
        }
        WriteList(writer, adjustments);
        return writer.CopyBytes();
    }

    static byte[] CreateClassPairAdjustment(
        SfntGposPairAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        byte[] coverage = CreateCoverage(value.GetCoverage(), glyphMap, limits);
        byte[] firstClasses = CreateClassDefinition(
            value.GetFirstClassDefinition(), glyphMap, limits);
        byte[] secondClasses = CreateClassDefinition(
            value.GetSecondClassDefinition(), glyphMap, limits);
        int firstFormat = value.GetFirstValueFormat();
        int secondFormat = value.GetSecondValueFormat();
        int headerLength = 16 +
            value.GetFirstClassCount() * value.GetSecondClassCount() *
            (GetValueSize(firstFormat) + GetValueSize(secondFormat));
        System.Collections.Generic.List<byte[]> adjustments =
            new System.Collections.Generic.List<byte[]>();
        System.Collections.Generic.List<byte[]> records =
            new System.Collections.Generic.List<byte[]>();
        int adjustmentOffset = headerLength;
        int first = 0;
        while (first < value.GetFirstClassCount())
        {
            int second = 0;
            while (second < value.GetSecondClassCount())
            {
                SfntGposClassPairRecord record =
                    value.GetClassPairRecord(first, second);
                records.Add(CreateValueRecord(
                    record.GetFirstValueRecord(), firstFormat,
                    adjustmentOffset, adjustments, limits));
                adjustmentOffset = headerLength + SumLengths(adjustments);
                records.Add(CreateValueRecord(
                    record.GetSecondValueRecord(), secondFormat,
                    adjustmentOffset, adjustments, limits));
                adjustmentOffset = headerLength + SumLengths(adjustments);
                second += 1;
            }
            first += 1;
        }
        int adjustmentLength = SumLengths(adjustments);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(2);
        WriteOffset16(writer, headerLength + adjustmentLength);
        writer.WriteUInt16((ushort)firstFormat);
        writer.WriteUInt16((ushort)secondFormat);
        WriteOffset16(
            writer, headerLength + adjustmentLength + coverage.Length);
        WriteOffset16(
            writer,
            headerLength + adjustmentLength + coverage.Length +
            firstClasses.Length);
        writer.WriteUInt16((ushort)value.GetFirstClassCount());
        writer.WriteUInt16((ushort)value.GetSecondClassCount());
        WriteList(writer, records);
        WriteList(writer, adjustments);
        writer.WriteBytes(coverage);
        writer.WriteBytes(firstClasses);
        writer.WriteBytes(secondClasses);
        return writer.CopyBytes();
    }

    static byte[] CreateCursiveAdjustment(
        SfntGposCursiveAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[]?[] entryAnchors = new byte[]?[retained.Length];
        byte[]?[] exitAnchors = new byte[]?[retained.Length];
        int index = 0;
        while (index < retained.Length)
        {
            SfntGposEntryExitRecord record =
                value.GetEntryExitRecord(retained[index]);
            entryAnchors[index] = record.HasEntryAnchor() ?
                CreateAnchor(record.GetEntryAnchor(), limits) : null;
            exitAnchors[index] = record.HasExitAnchor() ?
                CreateAnchor(record.GetExitAnchor(), limits) : null;
            index += 1;
        }
        return CreateAnchorRecordSubtable(
            MapCoverageAtIndices(value.GetCoverage(), retained, glyphMap),
            entryAnchors,
            exitAnchors,
            limits);
    }

    static byte[] CreateMarkAttachment(
        SfntGposMarkAttachment value,
        int[] glyphMap,
        FontLimits limits)
    {
        int[] marks = CopyRetainedCoverageIndices(
            value.GetMarkCoverage(), glyphMap);
        int[] bases = CopyRetainedCoverageIndices(
            value.GetBaseCoverage(), glyphMap);
        byte[] markCoverage = CreateCoverage(MapCoverageAtIndices(
            value.GetMarkCoverage(), marks, glyphMap), limits);
        byte[] baseCoverage = CreateCoverage(MapCoverageAtIndices(
            value.GetBaseCoverage(), bases, glyphMap), limits);
        byte[] markArray = CreateMarkArray(
            value.GetMarkArray(), marks, limits);
        byte[] baseArray = CreateAnchorMatrix(
            value.GetBaseAnchors(), bases, limits);
        int headerLength = 12;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        WriteOffset16(writer, headerLength);
        WriteOffset16(writer, headerLength + markCoverage.Length);
        writer.WriteUInt16((ushort)value.GetMarkClassCount());
        WriteOffset16(
            writer,
            headerLength + markCoverage.Length + baseCoverage.Length);
        WriteOffset16(
            writer,
            headerLength + markCoverage.Length + baseCoverage.Length +
            markArray.Length);
        writer.WriteBytes(markCoverage);
        writer.WriteBytes(baseCoverage);
        writer.WriteBytes(markArray);
        writer.WriteBytes(baseArray);
        return writer.CopyBytes();
    }

    static byte[] CreateMarkLigatureAdjustment(
        SfntGposMarkLigatureAdjustment value,
        int[] glyphMap,
        FontLimits limits)
    {
        int[] marks = CopyRetainedCoverageIndices(
            value.GetMarkCoverage(), glyphMap);
        int[] ligatures = CopyRetainedCoverageIndices(
            value.GetLigatureCoverage(), glyphMap);
        byte[] markCoverage = CreateCoverage(MapCoverageAtIndices(
            value.GetMarkCoverage(), marks, glyphMap), limits);
        byte[] ligatureCoverage = CreateCoverage(MapCoverageAtIndices(
            value.GetLigatureCoverage(), ligatures, glyphMap), limits);
        byte[] markArray = CreateMarkArray(
            value.GetMarkArray(), marks, limits);
        byte[][] attachments = new byte[ligatures.Length][];
        int index = 0;
        while (index < ligatures.Length)
        {
            attachments[index] = CreateLigatureAttachment(
                value.FindLigatureAttach(value.GetLigatureCoverage().GetGlyphId(
                    ligatures[index])),
                value.GetMarkClassCount(),
                limits);
            index += 1;
        }
        byte[] ligatureArray = CreateOffsetList(attachments, limits);
        int headerLength = 12;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        WriteOffset16(writer, headerLength);
        WriteOffset16(writer, headerLength + markCoverage.Length);
        writer.WriteUInt16((ushort)value.GetMarkClassCount());
        WriteOffset16(
            writer,
            headerLength + markCoverage.Length + ligatureCoverage.Length);
        WriteOffset16(
            writer,
            headerLength + markCoverage.Length + ligatureCoverage.Length +
            markArray.Length);
        writer.WriteBytes(markCoverage);
        writer.WriteBytes(ligatureCoverage);
        writer.WriteBytes(markArray);
        writer.WriteBytes(ligatureArray);
        return writer.CopyBytes();
    }

    static byte[] CreateMarkArray(
        SfntMarkArray marks,
        int[] retained,
        FontLimits limits)
    {
        byte[][] anchors = new byte[retained.Length][];
        int index = 0;
        while (index < retained.Length)
        {
            anchors[index] = CreateAnchor(
                marks.GetMark(retained[index]).GetAnchor(), limits);
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)retained.Length);
        int offset = 2 + retained.Length * 4;
        index = 0;
        while (index < retained.Length)
        {
            writer.WriteUInt16((ushort)marks.GetMark(
                retained[index]).GetMarkClass());
            WriteOffset16(writer, offset);
            offset = AddLength(offset, anchors[index].Length, limits);
            index += 1;
        }
        WriteBlobs(writer, anchors);
        return writer.CopyBytes();
    }

    static byte[] CreateAnchorMatrix(
        SfntGposAnchorMatrix matrix,
        int[] retained,
        FontLimits limits)
    {
        int count = retained.Length * matrix.GetMarkClassCount();
        byte[]?[] anchors = new byte[]?[count];
        int outputIndex = 0;
        int index = 0;
        while (index < retained.Length)
        {
            int markClass = 0;
            while (markClass < matrix.GetMarkClassCount())
            {
                SfntLayoutAnchor? anchor = matrix.GetAnchor(
                    retained[index], markClass);
                anchors[outputIndex] = anchor == null ? null :
                    CreateAnchor(anchor, limits);
                outputIndex += 1;
                markClass += 1;
            }
            index += 1;
        }
        return CreateNullableOffsetMatrix(
            retained.Length, anchors, limits);
    }

    static byte[] CreateLigatureAttachment(
        SfntGposLigatureAttach? attachment,
        int classCount,
        FontLimits limits)
    {
        System.ArgumentNullException.ThrowIfNull(attachment);
        byte[]?[] anchors = new byte[]?[attachment.GetComponentCount() *
            classCount];
        int index = 0;
        int component = 0;
        while (component < attachment.GetComponentCount())
        {
            int markClass = 0;
            while (markClass < classCount)
            {
                SfntLayoutAnchor? anchor = attachment.GetAnchor(
                    component, markClass);
                anchors[index] = anchor == null ? null :
                    CreateAnchor(anchor, limits);
                index += 1;
                markClass += 1;
            }
            component += 1;
        }
        return CreateNullableOffsetMatrix(
            attachment.GetComponentCount(), anchors, limits);
    }

    static byte[] CreateNullableOffsetMatrix(
        int recordCount,
        byte[]?[] values,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)recordCount);
        int offset = 2 + values.Length * 2;
        int index = 0;
        while (index < values.Length)
        {
            byte[]? value = values[index];
            if (value == null) { writer.WriteUInt16(0); }
            else
            {
                WriteOffset16(writer, offset);
                offset = AddLength(offset, value.Length, limits);
            }
            index += 1;
        }
        index = 0;
        while (index < values.Length)
        {
            if (values[index] != null) { writer.WriteBytes(values[index]); }
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateAnchorRecordSubtable(
        int[] glyphs,
        byte[]?[] first,
        byte[]?[] second,
        FontLimits limits)
    {
        byte[] coverage = CreateCoverage(glyphs, limits);
        int headerLength = 6 + glyphs.Length * 4;
        int offset = headerLength + coverage.Length;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        WriteOffset16(writer, headerLength);
        writer.WriteUInt16((ushort)glyphs.Length);
        int index = 0;
        while (index < glyphs.Length)
        {
            byte[]? firstValue = first[index];
            byte[]? secondValue = second[index];
            if (firstValue == null) { writer.WriteUInt16(0); }
            else
            {
                WriteOffset16(writer, offset);
                offset = AddLength(offset, firstValue.Length, limits);
            }
            if (secondValue == null) { writer.WriteUInt16(0); }
            else
            {
                WriteOffset16(writer, offset);
                offset = AddLength(offset, secondValue.Length, limits);
            }
            index += 1;
        }
        writer.WriteBytes(coverage);
        index = 0;
        while (index < glyphs.Length)
        {
            if (first[index] != null) { writer.WriteBytes(first[index]); }
            if (second[index] != null) { writer.WriteBytes(second[index]); }
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateAnchor(SfntLayoutAnchor? anchor, FontLimits limits)
    {
        System.ArgumentNullException.ThrowIfNull(anchor);
        if (anchor.GetFormat() == 1 || anchor.GetFormat() == 2)
        {
            BigEndianWriter simple = CreateWriter(limits);
            simple.WriteUInt16((ushort)anchor.GetFormat());
            simple.WriteInt16((short)anchor.GetXCoordinate());
            simple.WriteInt16((short)anchor.GetYCoordinate());
            if (anchor.GetFormat() == 2)
            {
                simple.WriteUInt16((ushort)anchor.GetAnchorPoint());
            }
            return simple.CopyBytes();
        }
        byte[]? x = CreateAdjustment(anchor.GetXAdjustment(), limits);
        byte[]? y = CreateAdjustment(anchor.GetYAdjustment(), limits);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(3);
        writer.WriteInt16((short)anchor.GetXCoordinate());
        writer.WriteInt16((short)anchor.GetYCoordinate());
        writer.WriteUInt16(x == null ? (ushort)0 : (ushort)10);
        writer.WriteUInt16(y == null ? (ushort)0 :
            (ushort)(10 + (x == null ? 0 : x.Length)));
        if (x != null) { writer.WriteBytes(x); }
        if (y != null) { writer.WriteBytes(y); }
        return writer.CopyBytes();
    }

    static byte[] CreateValueRecord(
        SfntGposValueRecord value,
        int format,
        int adjustmentOffset,
        System.Collections.Generic.List<byte[]> adjustments,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        WriteScalarValueRecord(writer, value, format);
        int bit = 0x10;
        while (bit <= 0x80)
        {
            if ((format & bit) != 0)
            {
                SfntDeviceOrVariationIndex? adjustment =
                    GetValueAdjustment(value, bit);
                byte[]? bytes = CreateAdjustment(adjustment, limits);
                if (bytes == null) { writer.WriteUInt16(0); }
                else
                {
                    WriteOffset16(writer, adjustmentOffset);
                    adjustments.Add(bytes);
                    adjustmentOffset = AddLength(
                        adjustmentOffset, bytes.Length, limits);
                }
            }
            bit <<= 1;
        }
        return writer.CopyBytes();
    }

    static SfntDeviceOrVariationIndex? GetValueAdjustment(
        SfntGposValueRecord value,
        int bit)
    {
        if (bit == 0x10) { return value.GetXPlacementAdjustment(); }
        if (bit == 0x20) { return value.GetYPlacementAdjustment(); }
        if (bit == 0x40) { return value.GetXAdvanceAdjustment(); }
        return value.GetYAdvanceAdjustment();
    }

    static void WriteScalarValueRecord(
        BigEndianWriter writer,
        SfntGposValueRecord value,
        int format)
    {
        if ((format & 1) != 0)
        {
            writer.WriteInt16((short)value.GetXPlacement());
        }
        if ((format & 2) != 0)
        {
            writer.WriteInt16((short)value.GetYPlacement());
        }
        if ((format & 4) != 0)
        {
            writer.WriteInt16((short)value.GetXAdvance());
        }
        if ((format & 8) != 0)
        {
            writer.WriteInt16((short)value.GetYAdvance());
        }
    }

    static int GetValueSize(int format)
    {
        int size = 0;
        int bit = 1;
        while (bit <= 0x80)
        {
            if ((format & bit) != 0) { size += 2; }
            bit <<= 1;
        }
        return size;
    }

    static byte[]? CreateAdjustment(
        SfntDeviceOrVariationIndex? value,
        FontLimits limits)
    {
        if (value == null) { return null; }
        BigEndianWriter writer = CreateWriter(limits);
        if (value.IsVariationIndex())
        {
            SfntVariationIndex? index = value.GetVariationIndex();
            System.ArgumentNullException.ThrowIfNull(index);
            writer.WriteUInt16((ushort)index.GetOuterIndex());
            writer.WriteUInt16((ushort)index.GetInnerIndex());
            writer.WriteUInt16(0x8000);
        }
        else
        {
            SfntDeviceTable? device = value.GetDevice();
            System.ArgumentNullException.ThrowIfNull(device);
            writer.WriteUInt16((ushort)device.GetStartSize());
            writer.WriteUInt16((ushort)device.GetEndSize());
            writer.WriteUInt16((ushort)device.GetDeltaFormat());
            int index = 0;
            while (index < device.GetPackedValueCount())
            {
                writer.WriteUInt16((ushort)device.GetPackedValue(index));
                index += 1;
            }
        }
        return writer.CopyBytes();
    }

    static int[] CopyRetainedCoverageIndices(
        SfntCoverageTable? coverage,
        int[] glyphMap)
    {
        System.Collections.Generic.List<int> output =
            new System.Collections.Generic.List<int>();
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(coverage);
        while (index < coverage.GetCoveredGlyphCount())
        {
            if (MapGlyph(coverage.GetGlyphId(index), glyphMap) >= 0)
            {
                output.Add(index);
            }
            index += 1;
        }
        return output.ToArray();
    }

    static int[] MapCoverageAtIndices(
        SfntCoverageTable? coverage,
        int[] indices,
        int[] glyphMap)
    {
        int[] output = new int[indices.Length];
        int index = 0;
        while (index < indices.Length)
        {
            System.ArgumentNullException.ThrowIfNull(coverage);
            output[index] = MapGlyph(
                coverage.GetGlyphId(indices[index]), glyphMap);
            index += 1;
        }
        return output;
    }

    static int SumLengths(
        System.Collections.Generic.List<byte[]> values)
    {
        int length = 0;
        int index = 0;
        while (index < values.Count)
        {
            length += values[index].Length;
            index += 1;
        }
        return length;
    }

    static void WriteList(
        BigEndianWriter writer,
        System.Collections.Generic.List<byte[]> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            writer.WriteBytes(values[index]);
            index += 1;
        }
    }

    static byte[] CreateContext(
        SfntGsubContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        if (value.GetFormat() == 1)
        {
            return CreateGlyphContext(value, glyphMap, lookupMap, limits);
        }
        if (value.GetFormat() == 2)
        {
            return CreateClassContext(value, glyphMap, lookupMap, limits);
        }
        if (value.GetFormat() != 3)
        {
            return CreateEmptyContext(false, limits);
        }

        byte[][] coverages = new byte[value.GetInputCoverageCount()][];
        int index = 0;
        while (index < coverages.Length)
        {
            coverages[index] = CreateCoverage(
                value.GetInputCoverage(index), glyphMap, limits);
            index += 1;
        }
        return CreateFormatThreeContext(
            coverages,
            value.GetLookupRecords(),
            lookupMap,
            limits);
    }

    static byte[] CreateChainedContext(
        SfntGsubChainedContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        if (value.GetFormat() == 1)
        {
            return CreateGlyphChainedContext(
                value, glyphMap, lookupMap, limits);
        }
        if (value.GetFormat() == 2)
        {
            return CreateClassChainedContext(
                value, glyphMap, lookupMap, limits);
        }
        if (value.GetFormat() != 3)
        {
            return CreateEmptyContext(true, limits);
        }

        byte[][] backtrack = new byte[value.GetBacktrackCoverageCount()][];
        byte[][] input = new byte[value.GetInputCoverageCount()][];
        byte[][] lookahead = new byte[value.GetLookaheadCoverageCount()][];
        int index = 0;
        while (index < backtrack.Length)
        {
            backtrack[index] = CreateCoverage(
                value.GetBacktrackCoverage(index), glyphMap, limits);
            index += 1;
        }
        index = 0;
        while (index < input.Length)
        {
            input[index] = CreateCoverage(
                value.GetInputCoverage(index), glyphMap, limits);
            index += 1;
        }
        index = 0;
        while (index < lookahead.Length)
        {
            lookahead[index] = CreateCoverage(
                value.GetLookaheadCoverage(index), glyphMap, limits);
            index += 1;
        }
        return CreateFormatThreeChainedContext(
            backtrack,
            input,
            lookahead,
            value.GetLookupRecords(),
            lookupMap,
            limits);
    }

    static byte[] CreateGlyphContext(
        SfntGsubContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[]?[] ruleSets = new byte[]?[retained.Length];
        int index = 0;
        while (index < retained.Length)
        {
            ruleSets[index] = CreateContextRuleSet(
                value.GetRuleSet(retained[index]),
                glyphMap,
                lookupMap,
                true,
                limits);
            index += 1;
        }
        return CreateContextRuleSetSubtable(
            1,
            CreateCoverage(MapCoverageAtIndices(
                value.GetCoverage(), retained, glyphMap), limits),
            null,
            ruleSets,
            limits);
    }

    static byte[] CreateClassContext(
        SfntGsubContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        byte[]?[] ruleSets = new byte[]?[value.GetRuleSetCount()];
        int index = 0;
        while (index < ruleSets.Length)
        {
            ruleSets[index] = CreateContextRuleSet(
                value.GetRuleSet(index),
                glyphMap,
                lookupMap,
                false,
                limits);
            index += 1;
        }
        return CreateContextRuleSetSubtable(
            2,
            CreateCoverage(value.GetCoverage(), glyphMap, limits),
            CreateClassDefinition(
                value.GetClassDefinition(), glyphMap, limits),
            ruleSets,
            limits);
    }

    static byte[]? CreateContextRuleSet(
        SfntGsubContextRuleSet set,
        int[] glyphMap,
        int[] lookupMap,
        bool mapGlyphs,
        FontLimits limits)
    {
        if (set == null) { return null; }
        System.Collections.Generic.List<byte[]> rules =
            new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < set.GetRuleCount())
        {
            SfntGsubContextRule rule = set.GetRule(index);
            byte[]? bytes = CreateContextRule(
                rule, glyphMap, lookupMap, mapGlyphs, limits);
            if (bytes != null) { rules.Add(bytes); }
            index += 1;
        }
        return CreateOffsetList(rules.ToArray(), limits);
    }

    static byte[]? CreateContextRule(
        SfntGsubContextRule rule,
        int[] glyphMap,
        int[] lookupMap,
        bool mapGlyphs,
        FontLimits limits)
    {
        int[] values = new int[rule.GetGlyphCount() - 1];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = mapGlyphs ?
                MapGlyph(rule.GetInputValue(index), glyphMap) :
                rule.GetInputValue(index);
            if (values[index] < 0) { return null; }
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)rule.GetGlyphCount());
        writer.WriteUInt16((ushort)CountMappedLookupRecords(
            rule.GetLookupRecords(), lookupMap));
        index = 0;
        while (index < values.Length)
        {
            writer.WriteUInt16((ushort)values[index]);
            index += 1;
        }
        WriteLookupRecords(writer, rule.GetLookupRecords(), lookupMap);
        return writer.CopyBytes();
    }

    static byte[] CreateContextRuleSetSubtable(
        int format,
        byte[] coverage,
        byte[]? classes,
        byte[]?[] ruleSets,
        FontLimits limits)
    {
        int headerLength = 6 + ruleSets.Length * 2 +
            (classes == null ? 0 : 2);
        int offset = headerLength + coverage.Length +
            (classes == null ? 0 : classes.Length);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)format);
        WriteOffset16(writer, headerLength);
        if (classes != null)
        {
            WriteOffset16(writer, headerLength + coverage.Length);
        }
        writer.WriteUInt16((ushort)ruleSets.Length);
        int index = 0;
        while (index < ruleSets.Length)
        {
            byte[]? ruleSet = ruleSets[index];
            if (ruleSet == null) { writer.WriteUInt16(0); }
            else
            {
                WriteOffset16(writer, offset);
                offset = AddLength(offset, ruleSet.Length, limits);
            }
            index += 1;
        }
        writer.WriteBytes(coverage);
        if (classes != null) { writer.WriteBytes(classes); }
        WriteOptionalBlobs(writer, ruleSets);
        return writer.CopyBytes();
    }

    static byte[] CreateGlyphChainedContext(
        SfntGsubChainedContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[]?[] sets = new byte[]?[retained.Length];
        int index = 0;
        while (index < retained.Length)
        {
            sets[index] = CreateChainedRuleSet(
                value.GetRuleSet(retained[index]),
                glyphMap,
                lookupMap,
                true,
                limits);
            index += 1;
        }
        return CreateChainedRuleSetSubtable(
            1,
            CreateCoverage(MapCoverageAtIndices(
                value.GetCoverage(), retained, glyphMap), limits),
            null,
            null,
            null,
            sets,
            limits);
    }

    static byte[] CreateClassChainedContext(
        SfntGsubChainedContextSubstitution value,
        int[] glyphMap,
        int[] lookupMap,
        FontLimits limits)
    {
        byte[]?[] sets = new byte[]?[value.GetRuleSetCount()];
        int index = 0;
        while (index < sets.Length)
        {
            sets[index] = CreateChainedRuleSet(
                value.GetRuleSet(index),
                glyphMap,
                lookupMap,
                false,
                limits);
            index += 1;
        }
        return CreateChainedRuleSetSubtable(
            2,
            CreateCoverage(value.GetCoverage(), glyphMap, limits),
            CreateClassDefinition(
                value.GetBacktrackClassDefinition(), glyphMap, limits),
            CreateClassDefinition(
                value.GetInputClassDefinition(), glyphMap, limits),
            CreateClassDefinition(
                value.GetLookaheadClassDefinition(), glyphMap, limits),
            sets,
            limits);
    }

    static byte[]? CreateChainedRuleSet(
        SfntGsubChainedRuleSet set,
        int[] glyphMap,
        int[] lookupMap,
        bool mapGlyphs,
        FontLimits limits)
    {
        if (set == null) { return null; }
        System.Collections.Generic.List<byte[]> rules =
            new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < set.GetRuleCount())
        {
            byte[]? rule = CreateChainedRule(
                set.GetRule(index),
                glyphMap,
                lookupMap,
                mapGlyphs,
                limits);
            if (rule != null) { rules.Add(rule); }
            index += 1;
        }
        return CreateOffsetList(rules.ToArray(), limits);
    }

    static byte[]? CreateChainedRule(
        SfntGsubChainedRule rule,
        int[] glyphMap,
        int[] lookupMap,
        bool mapGlyphs,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)rule.GetBacktrackCount());
        int index = 0;
        while (index < rule.GetBacktrackCount())
        {
            int value = mapGlyphs ?
                MapGlyph(rule.GetBacktrackValue(index), glyphMap) :
                rule.GetBacktrackValue(index);
            if (value < 0) { return null; }
            writer.WriteUInt16((ushort)value);
            index += 1;
        }
        writer.WriteUInt16((ushort)rule.GetInputGlyphCount());
        index = 0;
        while (index < rule.GetInputGlyphCount() - 1)
        {
            int value = mapGlyphs ?
                MapGlyph(rule.GetInputValue(index), glyphMap) :
                rule.GetInputValue(index);
            if (value < 0) { return null; }
            writer.WriteUInt16((ushort)value);
            index += 1;
        }
        writer.WriteUInt16((ushort)rule.GetLookaheadCount());
        index = 0;
        while (index < rule.GetLookaheadCount())
        {
            int value = mapGlyphs ?
                MapGlyph(rule.GetLookaheadValue(index), glyphMap) :
                rule.GetLookaheadValue(index);
            if (value < 0) { return null; }
            writer.WriteUInt16((ushort)value);
            index += 1;
        }
        writer.WriteUInt16((ushort)CountMappedLookupRecords(
            rule.GetLookupRecords(), lookupMap));
        WriteLookupRecords(writer, rule.GetLookupRecords(), lookupMap);
        return writer.CopyBytes();
    }

    static byte[] CreateChainedRuleSetSubtable(
        int format,
        byte[] coverage,
        byte[]? backtrackClasses,
        byte[]? inputClasses,
        byte[]? lookaheadClasses,
        byte[]?[] sets,
        FontLimits limits)
    {
        int classLength = 0;
        if (backtrackClasses != null)
        {
            System.ArgumentNullException.ThrowIfNull(inputClasses);
            System.ArgumentNullException.ThrowIfNull(lookaheadClasses);
            classLength = backtrackClasses.Length + inputClasses.Length +
                lookaheadClasses.Length;
        }
        int headerLength = (format == 1 ? 6 : 12) + sets.Length * 2;
        int offset = headerLength + coverage.Length + classLength;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)format);
        WriteOffset16(writer, headerLength);
        if (format == 2)
        {
            WriteOffset16(writer, headerLength + coverage.Length);
            System.ArgumentNullException.ThrowIfNull(backtrackClasses);
            WriteOffset16(writer,
                headerLength + coverage.Length + backtrackClasses.Length);
            System.ArgumentNullException.ThrowIfNull(inputClasses);
            WriteOffset16(writer,
                headerLength + coverage.Length + backtrackClasses.Length +
                inputClasses.Length);
        }
        writer.WriteUInt16((ushort)sets.Length);
        int index = 0;
        while (index < sets.Length)
        {
            byte[]? set = sets[index];
            if (set == null) { writer.WriteUInt16(0); }
            else
            {
                WriteOffset16(writer, offset);
                offset = AddLength(offset, set.Length, limits);
            }
            index += 1;
        }
        writer.WriteBytes(coverage);
        if (format == 2)
        {
            writer.WriteBytes(backtrackClasses);
            writer.WriteBytes(inputClasses);
            writer.WriteBytes(lookaheadClasses);
        }
        WriteOptionalBlobs(writer, sets);
        return writer.CopyBytes();
    }

    static byte[] CreateEmptyContext(bool chained, FontLimits limits)
    {
        byte[] coverage = CreateCoverage(new int[0], limits);
        if (chained)
        {
            return Join(limits, new byte[][]
            {
                U16(3), U16(0), U16(1), U16(12), U16(0), U16(0), coverage
            });
        }
        return Join(limits, new byte[][]
        {
            U16(3), U16(1), U16(0), U16(8), coverage
        });
    }

    static byte[] CreateFormatThreeContext(
        byte[][] coverages,
        SfntSequenceLookupRecords? records,
        int[] lookupMap,
        FontLimits limits)
    {
        int headerLength = 6 + coverages.Length * 2 +
            CountMappedLookupRecords(records, lookupMap) * 4;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(3);
        writer.WriteUInt16((ushort)coverages.Length);
        writer.WriteUInt16((ushort)CountMappedLookupRecords(
            records, lookupMap));
        int offset = headerLength;
        int index = 0;
        while (index < coverages.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, coverages[index].Length, limits);
            index += 1;
        }
        WriteLookupRecords(writer, records, lookupMap);
        index = 0;
        while (index < coverages.Length)
        {
            writer.WriteBytes(coverages[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateFormatThreeChainedContext(
        byte[][] backtrack,
        byte[][] input,
        byte[][] lookahead,
        SfntSequenceLookupRecords? records,
        int[] lookupMap,
        FontLimits limits)
    {
        int headerLength = 10 +
            (backtrack.Length + input.Length + lookahead.Length) * 2 +
            CountMappedLookupRecords(records, lookupMap) * 4;
        int offset = headerLength;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(3);
        writer.WriteUInt16((ushort)backtrack.Length);
        int index = 0;
        while (index < backtrack.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, backtrack[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt16((ushort)input.Length);
        index = 0;
        while (index < input.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, input[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt16((ushort)lookahead.Length);
        index = 0;
        while (index < lookahead.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, lookahead[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt16((ushort)CountMappedLookupRecords(
            records, lookupMap));
        WriteLookupRecords(writer, records, lookupMap);
        WriteBlobs(writer, backtrack);
        WriteBlobs(writer, input);
        WriteBlobs(writer, lookahead);
        return writer.CopyBytes();
    }

    static void WriteLookupRecords(
        BigEndianWriter writer,
        SfntSequenceLookupRecords? records,
        int[] lookupMap)
    {
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(records);
        while (index < records.GetRecordCount())
        {
            SfntSequenceLookupRecord record = records.GetRecord(index);
            int mapped = MapIndex(record.GetLookupListIndex(), lookupMap);
            if (mapped >= 0)
            {
                writer.WriteUInt16((ushort)record.GetSequenceIndex());
                writer.WriteUInt16((ushort)mapped);
            }
            index += 1;
        }
    }

    static int CountMappedLookupRecords(
        SfntSequenceLookupRecords? records,
        int[] lookupMap)
    {
        int count = 0;
        int index = 0;
        System.ArgumentNullException.ThrowIfNull(records);
        while (index < records.GetRecordCount())
        {
            if (MapIndex(records.GetRecord(index).GetLookupListIndex(),
                lookupMap) >= 0)
            {
                count += 1;
            }
            index += 1;
        }
        return count;
    }

    static byte[][] CopyCoverages(
        int count,
        SfntGsubReverseSubstitution reverse,
        SfntGsubChainedContextSubstitution? chained,
        int[] glyphMap,
        bool backtrack,
        FontLimits limits)
    {
        byte[][] output = new byte[count][];
        int index = 0;
        while (index < count)
        {
            SfntCoverageTable coverage;
            if (reverse != null)
            {
                coverage = backtrack ?
                    reverse.GetBacktrackCoverage(index) :
                    reverse.GetLookaheadCoverage(index);
            }
            else
            {
                System.ArgumentNullException.ThrowIfNull(chained);
                coverage = backtrack ?
                    chained.GetBacktrackCoverage(index) :
                    chained.GetLookaheadCoverage(index);
            }
            output[index] = CreateCoverage(coverage, glyphMap, limits);
            index += 1;
        }
        return output;
    }

    static byte[] CreateReverse(
        int[] inputs,
        int[] outputs,
        byte[][] backtrack,
        byte[][] lookahead,
        FontLimits limits)
    {
        byte[] coverage = CreateCoverage(inputs, limits);
        int headerLength = 10 +
            (backtrack.Length + lookahead.Length + outputs.Length) * 2;
        int offset = headerLength;
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        WriteOffset16(writer, offset);
        offset = AddLength(offset, coverage.Length, limits);
        writer.WriteUInt16((ushort)backtrack.Length);
        int index = 0;
        while (index < backtrack.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, backtrack[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt16((ushort)lookahead.Length);
        index = 0;
        while (index < lookahead.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, lookahead[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt16((ushort)outputs.Length);
        index = 0;
        while (index < outputs.Length)
        {
            writer.WriteUInt16((ushort)outputs[index]);
            index += 1;
        }
        writer.WriteBytes(coverage);
        WriteBlobs(writer, backtrack);
        WriteBlobs(writer, lookahead);
        return writer.CopyBytes();
    }

    static byte[] CreateGdef(
        SfntGdefTable table,
        byte[]? source,
        int[] glyphMap,
        int[] retainedMarkSets,
        FontLimits limits)
    {
        byte[]? glyphClasses = table.GetGlyphClasses() == null ? null :
            CreateClassDefinition(table.GetGlyphClasses(), glyphMap, limits);
        byte[]? attachments = table.GetAttachmentList() == null ? null :
            CreateAttachmentList(table.GetAttachmentList(), glyphMap, limits);
        byte[]? carets = table.GetLigatureCaretList() == null ? null :
            CreateCaretList(
                table.GetLigatureCaretList(), glyphMap, limits);
        byte[]? markClasses = table.GetMarkAttachmentClasses() == null ? null :
            CreateClassDefinition(
                table.GetMarkAttachmentClasses(), glyphMap, limits);
        byte[]? markSets = table.GetMarkGlyphSets() == null ? null :
            CreateMarkGlyphSets(
                table.GetMarkGlyphSets(),
                glyphMap,
                retainedMarkSets,
                limits);
        byte[]? variationStore = null;
        if (table.GetItemVariationStore() != null && source != null)
        {
            SfntItemVariationStore? itemVariationStore = table.GetItemVariationStore();
            System.ArgumentNullException.ThrowIfNull(itemVariationStore);
            variationStore = CopyRange(
                source,
                itemVariationStore.GetOffset(),
                source.Length);
        }
        int headerLength = variationStore != null ? 18 :
            (markSets != null ? 14 : 12);
        byte[]?[] values = new byte[]?[]
        {
            glyphClasses, attachments, carets, markClasses, markSets,
            variationStore
        };
        int[] offsets = GetOptionalOffsets(values, headerLength, limits);
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        writer.WriteUInt16((ushort)(variationStore != null ? 3 :
            (markSets != null ? 2 : 0)));
        int index = 0;
        while (index < 4)
        {
            WriteOffset16(writer, offsets[index]);
            index += 1;
        }
        if (markSets != null || variationStore != null)
        {
            WriteOffset16(writer, offsets[4]);
        }
        if (variationStore != null)
        {
            writer.WriteUInt32((uint)offsets[5]);
        }
        WriteOptionalBlobs(writer, values);
        return writer.CopyBytes();
    }

    static byte[] CreateAttachmentList(
        SfntGdefAttachmentList? value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.ArgumentNullException.ThrowIfNull(value);
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[][] points = new byte[retained.Length][];
        int index = 0;
        while (index < retained.Length)
        {
            SfntGdefAttachmentPoint point =
                value.GetAttachmentPoint(retained[index]);
            BigEndianWriter pointWriter = CreateWriter(limits);
            pointWriter.WriteUInt16((ushort)point.GetPointCount());
            int pointIndex = 0;
            while (pointIndex < point.GetPointCount())
            {
                pointWriter.WriteUInt16((ushort)point.GetPointIndex(
                    pointIndex));
                pointIndex += 1;
            }
            points[index] = pointWriter.CopyBytes();
            index += 1;
        }
        return CreateCoverageOffsetList(
            MapCoverageAtIndices(value.GetCoverage(), retained, glyphMap),
            points,
            limits);
    }

    static byte[] CreateCaretList(
        SfntGdefLigatureCaretList? value,
        int[] glyphMap,
        FontLimits limits)
    {
        System.ArgumentNullException.ThrowIfNull(value);
        int[] retained = CopyRetainedCoverageIndices(
            value.GetCoverage(), glyphMap);
        byte[][] glyphs = new byte[retained.Length][];
        int index = 0;
        while (index < retained.Length)
        {
            glyphs[index] = CreateLigatureGlyphCarets(
                value.GetLigatureGlyph(retained[index]), limits);
            index += 1;
        }
        return CreateCoverageOffsetList(
            MapCoverageAtIndices(value.GetCoverage(), retained, glyphMap),
            glyphs,
            limits);
    }

    static byte[] CreateLigatureGlyphCarets(
        SfntGdefLigatureGlyph value,
        FontLimits limits)
    {
        byte[][] carets = new byte[value.GetCaretCount()][];
        int index = 0;
        while (index < carets.Length)
        {
            carets[index] = CreateCaretValue(
                value.GetCaretValue(index), limits);
            index += 1;
        }
        return CreateOffsetList(carets, limits);
    }

    static byte[] CreateCaretValue(
        SfntGdefCaretValue value,
        FontLimits limits)
    {
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16((ushort)value.GetFormat());
        if (value.GetFormat() == 2)
        {
            writer.WriteUInt16((ushort)value.GetContourPointIndex());
        }
        else
        {
            writer.WriteInt16((short)value.GetCoordinate());
        }
        if (value.GetFormat() == 3)
        {
            byte[]? adjustment = CreateAdjustment(
                value.GetAdjustment(), limits);
            writer.WriteUInt16(adjustment == null ? (ushort)0 : (ushort)6);
            if (adjustment != null) { writer.WriteBytes(adjustment); }
        }
        return writer.CopyBytes();
    }

    static byte[] CreateCoverageOffsetList(
        int[] glyphs,
        byte[][] records,
        FontLimits limits)
    {
        byte[] coverage = CreateCoverage(glyphs, limits);
        int headerLength = 4 + records.Length * 2;
        BigEndianWriter writer = CreateWriter(limits);
        WriteOffset16(writer, headerLength);
        writer.WriteUInt16((ushort)records.Length);
        int offset = headerLength + coverage.Length;
        int index = 0;
        while (index < records.Length)
        {
            WriteOffset16(writer, offset);
            offset = AddLength(offset, records[index].Length, limits);
            index += 1;
        }
        writer.WriteBytes(coverage);
        WriteBlobs(writer, records);
        return writer.CopyBytes();
    }

    static byte[] CreateMarkGlyphSets(
        SfntGdefMarkGlyphSets? value,
        int[] glyphMap,
        int[] retainedSets,
        FontLimits limits)
    {
        System.ArgumentNullException.ThrowIfNull(value);
        byte[][] coverages = new byte[value.GetSetCount()][];
        int index = 0;
        while (index < coverages.Length)
        {
            coverages[index] = Contains(retainedSets, index) ?
                CreateCoverage(value.GetCoverage(index), glyphMap, limits) :
                CreateCoverage(new int[0], limits);
            index += 1;
        }
        BigEndianWriter writer = CreateWriter(limits);
        writer.WriteUInt16(1);
        writer.WriteUInt16((ushort)coverages.Length);
        int offset = 4 + coverages.Length * 4;
        index = 0;
        while (index < coverages.Length)
        {
            writer.WriteUInt32((uint)offset);
            offset = AddLength(offset, coverages[index].Length, limits);
            index += 1;
        }
        WriteBlobs(writer, coverages);
        return writer.CopyBytes();
    }

    static int[] GetOptionalOffsets(
        byte[]?[] values,
        int headerLength,
        FontLimits limits)
    {
        int[] offsets = new int[values.Length];
        int offset = headerLength;
        int index = 0;
        while (index < values.Length)
        {
            byte[]? value = values[index];
            if (value != null)
            {
                offsets[index] = offset;
                offset = AddLength(offset, value.Length, limits);
            }
            index += 1;
        }
        return offsets;
    }

    static void WriteOptionalBlobs(
        BigEndianWriter writer,
        byte[]?[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null) { writer.WriteBytes(values[index]); }
            index += 1;
        }
    }

    static byte[] CopyRange(byte[] source, int start, int end)
    {
        byte[] output = new byte[end - start];
        System.Array.Copy(source, start, output, 0, output.Length);
        return output;
    }

    static bool Contains(int[] values, int candidate)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == candidate) { return true; }
            index += 1;
        }
        return false;
    }

    static int MapGlyph(int glyphId, int[] glyphMap)
    {
        return glyphId < 0 || glyphId >= glyphMap.Length ?
            -1 : glyphMap[glyphId];
    }

    static int[]? MapGlyphs(int[] glyphs, int[] glyphMap)
    {
        int[] output = new int[glyphs.Length];
        int index = 0;
        while (index < glyphs.Length)
        {
            output[index] = MapGlyph(glyphs[index], glyphMap);
            if (output[index] < 0) { return null; }
            index += 1;
        }
        return output;
    }

    static byte[] U16(int value)
    {
        return new byte[]
        {
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        };
    }

    static byte[] Join(FontLimits limits, byte[][] values)
    {
        int length = 0;
        int index = 0;
        while (index < values.Length)
        {
            length = AddLength(length, values[index].Length, limits);
            index += 1;
        }
        BigEndianWriter writer = BigEndianWriter.Create(
            length, limits.GetMaxOutputBytes());
        WriteBlobs(writer, values);
        return writer.CopyBytes();
    }

    static BigEndianWriter CreateWriter(FontLimits limits)
    {
        return BigEndianWriter.Create(limits.GetMaxOutputBytes());
    }

    static int AddLength(int offset, int length, FontLimits limits)
    {
        long sum = (long)offset + length;
        if (sum > limits.GetMaxOutputBytes() || sum > int.MaxValue)
        {
            throw FontLimitException.Create("Rewritten layout data is too large.");
        }
        return (int)sum;
    }

    static void WriteOffset16(BigEndianWriter writer, int offset)
    {
        if (offset < 0 || offset > ushort.MaxValue)
        {
            throw FontLimitException.Create(
                "A rewritten layout offset exceeds UInt16.");
        }
        writer.WriteUInt16((ushort)offset);
    }

    static void WriteBlobs(BigEndianWriter writer, byte[][] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteBytes(values[index]);
            index += 1;
        }
    }

    static void WriteUInt16(byte[] output, int offset, int value)
    {
        output[offset] = (byte)((value >> 8) & 0xFF);
        output[offset + 1] = (byte)(value & 0xFF);
    }
}
