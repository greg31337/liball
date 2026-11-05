namespace unicode;

/// <summary>
/// Looks up Unicode Character Database properties, names, scripts, blocks, and numeric values.
/// </summary>
public class UnicodeProperties : IUnicodeProperties
{
    internal static readonly UnicodeProperties Instance = new UnicodeProperties();
    internal static readonly string[] HangulLeadingNames = new string[]
    {
        "G", "GG", "N", "D", "DD", "R", "M", "B", "BB", "S", "SS", "", "J", "JJ", "C", "K", "T", "P", "H",
    };
    internal static readonly string[] HangulVowelNames = new string[]
    {
        "A", "AE", "YA", "YAE", "EO", "E", "YEO", "YE", "O", "WA", "WAE", "OE", "YO", "U", "WEO", "WE", "WI", "YU", "EU", "YI", "I",
    };
    internal static readonly string[] HangulTrailingNames = new string[]
    {
        "", "G", "GG", "GS", "N", "NJ", "NH", "D", "L", "LG", "LM", "LB", "LS", "LT", "LP", "LH", "M", "B", "BS", "S", "SS", "NG", "J", "C", "K", "T", "P", "H",
    };

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeProperties()
    {
    }
    /// <summary>
    /// Returns the Unicode General_Category value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeGeneralCategory GetGeneralCategory(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeCategoryTables.GeneralCategories(),
            codePoint,
            (int)UnicodeGeneralCategory.OtherNotAssigned);
        return (UnicodeGeneralCategory)value;
    }

    /// <summary>
    /// Returns the canonical combining class for a code point; unassigned values use class zero.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int GetCombiningClass(int codePoint)
    {
        return UnicodeTableLookup.LookupRangeValue(
            UnicodeCategoryTables.CombiningClasses(),
            codePoint,
            0);
    }

    /// <summary>
    /// Returns the Unicode Bidi_Class value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeBidiClass GetBidiClass(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeBidiTables.BidiClasses(),
            codePoint,
            (int)UnicodeBidiClass.LeftToRight);
        return (UnicodeBidiClass)value;
    }

    /// <summary>
    /// Reports whether the Unicode bidirectional mirrored property is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsBidiMirrored(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeBidiTables.BidiMirrored(), codePoint);
    }

    /// <summary>
    /// Returns the bidirectional mirroring glyph, or the input code point when no mapping is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The mapped code point or the input value.</returns>
    public int GetBidiMirror(int codePoint)
    {
        return UnicodeTableLookup.LookupRangeValue(
            UnicodeBidiTables.BidiMirrors(),
            codePoint,
            codePoint);
    }

    /// <summary>
    /// Returns the bidirectional paired bracket, or the input code point when none is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The paired bracket code point or the input value.</returns>
    public int GetBidiPairedBracket(int codePoint)
    {
        return UnicodeTableLookup.LookupRangeValue(
            UnicodeBidiTables.BidiBracketPairs(),
            codePoint,
            codePoint);
    }

    /// <summary>
    /// Returns the bidirectional paired-bracket type.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeBidiPairedBracketType GetBidiPairedBracketType(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeBidiTables.BidiBracketTypes(),
            codePoint,
            -1);
        if (value < 0)
        {
            return UnicodeBidiPairedBracketType.None;
        }

        return (UnicodeBidiPairedBracketType)(value + 1);
    }

    /// <summary>
    /// Returns the decomposition type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeDecompositionType GetDecompositionType(int codePoint)
    {
        if (UnicodeHangul.Instance.IsHangulSyllable(codePoint))
        {
            return UnicodeDecompositionType.Canonical;
        }

        int index = FindCodePointIndex(UnicodeNormalizationTables.DecompositionCodePoints(), codePoint);
        if (index < 0)
        {
            return UnicodeDecompositionType.None;
        }

        int typeIndex = UnicodeNormalizationTables.DecompositionTypeIndexes()[index];
        string typeName = UnicodeNormalizationTables.DecompositionTypeNames()[typeIndex];
        return DecompositionTypeFromName(typeName);
    }

    /// <summary>
    /// Returns a copy of the direct decomposition mapping, or an empty array.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the direct mapping.</returns>
    public int[] GetDecompositionMapping(int codePoint)
    {
        if (UnicodeHangul.Instance.IsHangulSyllable(codePoint))
        {
            return UnicodeHangul.Instance.DecomposeSyllable(codePoint);
        }

        int index = FindCodePointIndex(UnicodeNormalizationTables.DecompositionCodePoints(), codePoint);
        if (index < 0)
        {
            return new int[]
            {
            };
        }

        int offset = UnicodeNormalizationTables.DecompositionMappingOffsets()[index];
        int length = UnicodeNormalizationTables.DecompositionMappingLengths()[index];
        int[] mappings = UnicodeNormalizationTables.DecompositionMappingCodePoints();
        int[] result = new int[length];
        int mappingIndex = 0;
        while (mappingIndex < length)
        {
            result[mappingIndex] = mappings[offset + mappingIndex];
            mappingIndex = mappingIndex + 1;
        }

        return result;
    }

    /// <summary>
    /// Returns the primary Unicode Script value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeScript GetScript(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeScriptTables.Scripts(),
            codePoint,
            (int)UnicodeScript.Unknown);
        return (UnicodeScript)value;
    }

    /// <summary>
    /// Returns Script_Extensions values when present, otherwise the primary script as a one-item array.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing the resulting values.</returns>
    public UnicodeScript[] GetScriptExtensions(int codePoint)
    {
        int[] starts = UnicodeScriptTables.ScriptExtensionStarts();
        int[] ends = UnicodeScriptTables.ScriptExtensionEnds();
        int[] offsets = UnicodeScriptTables.ScriptExtensionOffsets();
        int[] lengths = UnicodeScriptTables.ScriptExtensionLengths();
        int[] values = UnicodeScriptTables.ScriptExtensionValues();
        int index = 0;

        while (index < starts.Length)
        {
            if (codePoint >= starts[index] && codePoint <= ends[index] && lengths[index] > 0)
            {
                UnicodeScript[] scripts = new UnicodeScript[lengths[index]];
                int scriptIndex = 0;
                while (scriptIndex < scripts.Length)
                {
                    scripts[scriptIndex] = (UnicodeScript)values[offsets[index] + scriptIndex];
                    scriptIndex = scriptIndex + 1;
                }

                return scripts;
            }

            index = index + 1;
        }

        return new UnicodeScript[]
        {
            GetScript(codePoint),
        };
    }

    /// <summary>
    /// Returns the Unicode block containing a code point, or NoBlock when none is assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeBlock GetBlock(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeBlockTables.Blocks(),
            codePoint,
            (int)UnicodeBlock.NoBlock);
        return (UnicodeBlock)value;
    }

    /// <summary>
    /// Returns the Unicode East_Asian_Width value for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeEastAsianWidth GetEastAsianWidth(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeWidthTables.EastAsianWidths(),
            codePoint,
            (int)UnicodeEastAsianWidth.Neutral);
        return (UnicodeEastAsianWidth)value;
    }

    /// <summary>
    /// Returns the exact integer or rational Unicode numeric value, or <c>double.NaN</c>.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A numeric Unicode property value, or <c>double.NaN</c> when the scalar has no numeric value.</returns>
    public double GetNumericValue(int codePoint)
    {
        int[] codePoints = UnicodeNumericTables.NumericCodePoints();
        long[] numerators = UnicodeNumericTables.NumericNumerators();
        long[] denominators = UnicodeNumericTables.NumericDenominators();
        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                return (double)numerators[index] / (double)denominators[index];
            }

            index = index + 1;
        }

        return -1.0;
    }

    /// <summary>
    /// Returns the Unicode numeric type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeNumericType GetNumericType(int codePoint)
    {
        int index = FindCodePointIndex(UnicodeNumericTables.NumericCodePoints(), codePoint);
        if (index < 0)
        {
            return UnicodeNumericType.None;
        }

        return (UnicodeNumericType)UnicodeNumericTables.NumericTypes()[index];
    }

    /// <summary>
    /// Returns the exact optional integer or rational Unicode numeric value.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A value containing the exact numerator and denominator when assigned.</returns>
    public UnicodeRationalValue GetNumericValueExact(int codePoint)
    {
        int index = FindCodePointIndex(UnicodeNumericTables.NumericCodePoints(), codePoint);
        if (index < 0)
        {
            return new UnicodeRationalValue(false, 0, 1);
        }

        return new UnicodeRationalValue(
            true,
            UnicodeNumericTables.NumericNumerators()[index],
            UnicodeNumericTables.NumericDenominators()[index]);
    }

    /// <summary>
    /// Returns the Unicode joining type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeJoiningType GetJoiningType(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeJoiningTables.JoiningTypes(),
            codePoint,
            -1);
        if (value >= 0)
        {
            return (UnicodeJoiningType)value;
        }

        UnicodeGeneralCategory category = GetGeneralCategory(codePoint);
        if (category == UnicodeGeneralCategory.NonspacingMark)
        {
            return UnicodeJoiningType.Transparent;
        }

        if (category == UnicodeGeneralCategory.EnclosingMark)
        {
            return UnicodeJoiningType.Transparent;
        }

        if (category == UnicodeGeneralCategory.Format)
        {
            return UnicodeJoiningType.Transparent;
        }

        return UnicodeJoiningType.NonJoining;
    }

    /// <summary>
    /// Returns the Unicode joining group of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeJoiningGroup GetJoiningGroup(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeJoiningTables.JoiningGroups(),
            codePoint,
            (int)UnicodeJoiningGroup.NoJoiningGroup);
        return (UnicodeJoiningGroup)value;
    }

    /// <summary>
    /// Returns the Unicode version in which a code point was assigned.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeAge GetAge(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeAgeTables.Ages(),
            codePoint,
            -1);
        if (value < 0)
        {
            return UnicodeAge.Unassigned;
        }

        return (UnicodeAge)(value + 1);
    }

    /// <summary>
    /// Returns the Indic conjunct-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeIndicConjunctBreak GetIndicConjunctBreak(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeBoundaryTables.IndicConjunctBreaks(),
            codePoint,
            -1);
        if (value < 0)
        {
            return UnicodeIndicConjunctBreak.None;
        }

        return (UnicodeIndicConjunctBreak)(value + 1);
    }

    /// <summary>
    /// Returns the Hangul syllable type of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeHangulSyllableType GetHangulSyllableType(int codePoint)
    {
        int value = UnicodeTableLookup.LookupRangeValue(
            UnicodeBoundaryTables.HangulSyllableTypes(),
            codePoint,
            -1);
        if (value < 0)
        {
            return UnicodeHangulSyllableType.NotApplicable;
        }

        return (UnicodeHangulSyllableType)(value + 1);
    }

    /// <summary>
    /// Returns the grapheme-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeGraphemeBreakClass GetGraphemeBreakClass(int codePoint)
    {
        return UnicodeSegments.Instance.GetGraphemeBreakClass(codePoint);
    }

    /// <summary>
    /// Returns the word-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeWordBreakClass GetWordBreakClass(int codePoint)
    {
        return UnicodeSegments.Instance.GetWordBreakClass(codePoint);
    }

    /// <summary>
    /// Returns the sentence-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeSentenceBreakClass GetSentenceBreakClass(int codePoint)
    {
        return UnicodeSegments.Instance.GetSentenceBreakClass(codePoint);
    }

    /// <summary>
    /// Returns the line-break property of a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeLineBreakClass GetLineBreakClass(int codePoint)
    {
        return UnicodeSegments.Instance.GetLineBreakClass(codePoint);
    }

    /// <summary>
    /// Returns the Unicode name of a code point, including supported algorithmic names, or an empty string.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    public string GetName(int codePoint)
    {
        int[] codePoints = UnicodeNameTables.NameCodePoints();
        string[] names = UnicodeNameTables.Names();
        int index = FindCodePointIndex(codePoints, codePoint);
        if (index >= 0)
        {
            return names[index];
        }

        return GetAlgorithmicName(codePoint);
    }

    /// <summary>
    /// Returns caller-owned character-name alias records for a code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>A new array containing new alias records.</returns>
    public UnicodeNameAlias[] GetNameAliases(int codePoint)
    {
        int[] codePoints = UnicodeAliasTables.NameAliasCodePoints();
        string[] names = UnicodeAliasTables.NameAliasNames();
        int[] types = UnicodeAliasTables.NameAliasTypes();
        System.Collections.Generic.List<UnicodeNameAlias> aliases = new System.Collections.Generic.List<UnicodeNameAlias>();
        int index = 0;
        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                aliases.Add(new UnicodeNameAlias(codePoint, names[index], (UnicodeNameAliasKind)types[index]));
            }

            index = index + 1;
        }

        return aliases.ToArray();
    }

    /// <summary>
    /// Looks up a Unicode character name case-insensitively and returns its code point, or <c>-1</c>.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is <c>null</c>.</exception>
    public int LookupName(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        int[] codePoints = UnicodeNameTables.NameCodePoints();
        string[] names = UnicodeNameTables.Names();
        int index = 0;

        while (index < names.Length)
        {
            if (string.Equals(names[index], name, System.StringComparison.OrdinalIgnoreCase))
            {
                return codePoints[index];
            }

            index = index + 1;
        }

        int[] aliasCodePoints = UnicodeAliasTables.NameAliasCodePoints();
        string[] aliasNames = UnicodeAliasTables.NameAliasNames();
        index = 0;
        while (index < aliasNames.Length)
        {
            if (string.Equals(aliasNames[index], name, System.StringComparison.OrdinalIgnoreCase))
            {
                return aliasCodePoints[index];
            }

            index = index + 1;
        }

        return LookupAlgorithmicName(name);
    }

    /// <summary>
    /// Looks up a Unicode named sequence case-insensitively and returns a copy of its scalars, or an empty array.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is <c>null</c>.</exception>
    public int[] LookupNamedSequence(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        string[] names = UnicodeNamedSequenceTables.Names();
        int[] offsets = UnicodeNamedSequenceTables.Offsets();
        int[] lengths = UnicodeNamedSequenceTables.Lengths();
        int[] codePoints = UnicodeNamedSequenceTables.CodePoints();
        int index = 0;

        while (index < names.Length)
        {
            if (string.Equals(names[index], name, System.StringComparison.OrdinalIgnoreCase))
            {
                int length = lengths[index];
                int[] sequence = new int[length];
                int offset = offsets[index];
                int sequenceIndex = 0;

                while (sequenceIndex < length)
                {
                    sequence[sequenceIndex] = codePoints[offset + sequenceIndex];
                    sequenceIndex = sequenceIndex + 1;
                }

                return sequence;
            }

            index = index + 1;
        }

        return new int[]
        {
        };
    }

    /// <summary>
    /// Returns caller-owned records for every normative named sequence.
    /// </summary>
    /// <returns>A new array containing new named-sequence records.</returns>
    public UnicodeNamedSequence[] GetNamedSequences()
    {
        string[] names = UnicodeNamedSequenceTables.Names();
        int[] offsets = UnicodeNamedSequenceTables.Offsets();
        int[] lengths = UnicodeNamedSequenceTables.Lengths();
        int[] codePoints = UnicodeNamedSequenceTables.CodePoints();
        UnicodeNamedSequence[] sequences = new UnicodeNamedSequence[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            int[] sequenceCodePoints = new int[lengths[index]];
            int sequenceIndex = 0;
            while (sequenceIndex < sequenceCodePoints.Length)
            {
                sequenceCodePoints[sequenceIndex] = codePoints[offsets[index] + sequenceIndex];
                sequenceIndex = sequenceIndex + 1;
            }

            sequences[index] = new UnicodeNamedSequence(names[index], sequenceCodePoints);
            index = index + 1;
        }

        return sequences;
    }

    /// <summary>
    /// Resolves a loosely matched property alias to its canonical long name.
    /// </summary>
    /// <param name="alias">A short, long, or additional property alias.</param>
    /// <returns>The canonical long name, or an empty string when unresolved.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>alias</c> is <c>null</c>.</exception>
    public string ResolvePropertyAlias(string alias)
    {
        System.ArgumentNullException.ThrowIfNull(alias);

        int index = FindPropertyAliasRecord(alias);
        if (index < 0)
        {
            return "";
        }

        return UnicodeAliasTables.PropertyLongNames()[index];
    }

    /// <summary>
    /// Resolves a loosely matched property-value alias to its canonical long name.
    /// </summary>
    /// <param name="propertyAlias">A short, long, or additional property alias.</param>
    /// <param name="valueAlias">A short, long, or additional value alias.</param>
    /// <returns>The canonical long value name, or an empty string when unresolved.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public string ResolvePropertyValueAlias(string propertyAlias, string valueAlias)
    {
        System.ArgumentNullException.ThrowIfNull(propertyAlias);
        System.ArgumentNullException.ThrowIfNull(valueAlias);

        int propertyIndex = FindPropertyAliasRecord(propertyAlias);
        if (propertyIndex < 0)
        {
            return "";
        }

        string propertyShortName = UnicodeAliasTables.PropertyShortNames()[propertyIndex];
        string[] valuePropertyNames = UnicodeAliasTables.ValuePropertyNames();
        string[] valueShortNames = UnicodeAliasTables.ValueShortNames();
        string[] valueLongNames = UnicodeAliasTables.ValueLongNames();
        int[] offsets = UnicodeAliasTables.ValueAliasOffsets();
        int[] lengths = UnicodeAliasTables.ValueAliasLengths();
        string[] aliases = UnicodeAliasTables.ValueAliasNames();
        int index = 0;
        while (index < valuePropertyNames.Length)
        {
            if (LooseAliasEquals(valuePropertyNames[index], propertyShortName))
            {
                if (LooseAliasEquals(valueShortNames[index], valueAlias))
                {
                    return valueLongNames[index];
                }

                int aliasIndex = 0;
                while (aliasIndex < lengths[index])
                {
                    if (LooseAliasEquals(aliases[offsets[index] + aliasIndex], valueAlias))
                    {
                        return valueLongNames[index];
                    }

                    aliasIndex = aliasIndex + 1;
                }
            }

            index = index + 1;
        }

        return "";
    }

    /// <summary>
    /// Reports whether a code point has the selected binary Unicode property.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <param name="property">The binary Unicode property to test.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HasBinaryProperty(int codePoint, UnicodeBinaryProperty property)
    {
        if (property == UnicodeBinaryProperty.PrivateUse)
        {
            return GetGeneralCategory(codePoint) == UnicodeGeneralCategory.PrivateUse;
        }

        if (property == UnicodeBinaryProperty.Control)
        {
            return GetGeneralCategory(codePoint) == UnicodeGeneralCategory.Control;
        }

        UnicodeRangeValue[] ranges = UnicodePropertyTables.BinaryProperties();
        int propertyValue = (int)property;
        int low = 0;
        int high = ranges.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            UnicodeRangeValue range = ranges[middle];
            if (range.Value < propertyValue)
            {
                low = middle + 1;
                continue;
            }

            if (range.Value > propertyValue)
            {
                high = middle - 1;
                continue;
            }

            if (codePoint < range.Start)
            {
                high = middle - 1;
                continue;
            }

            if (codePoint > range.End)
            {
                low = middle + 1;
                continue;
            }

            return true;
        }

        return false;
    }

    internal static int FindCodePointIndex(int[] codePoints, int codePoint)
    {
        int low = 0;
        int high = codePoints.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            if (codePoints[middle] == codePoint)
            {
                return middle;
            }

            if (codePoints[middle] < codePoint)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static string GetAlgorithmicName(int codePoint)
    {
        int[] starts = UnicodeNameTables.AlgorithmicNameStarts();
        int[] ends = UnicodeNameTables.AlgorithmicNameEnds();
        string[] prefixes = UnicodeNameTables.AlgorithmicNamePrefixes();
        int index = 0;
        while (index < starts.Length)
        {
            if (codePoint >= starts[index] && codePoint <= ends[index])
            {
                if (prefixes[index] == "HANGUL SYLLABLE")
                {
                    return GetHangulSyllableName(codePoint);
                }

                return prefixes[index] + "-" + codePoint.ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            index = index + 1;
        }

        return "";
    }

    internal static string GetHangulSyllableName(int codePoint)
    {
        int syllableIndex = codePoint - UnicodeHangul.SBase;
        int leadingIndex = syllableIndex / UnicodeHangul.NCount;
        int vowelIndex = (syllableIndex % UnicodeHangul.NCount) / UnicodeHangul.TCount;
        int trailingIndex = syllableIndex % UnicodeHangul.TCount;
        return "HANGUL SYLLABLE " + HangulLeadingNames[leadingIndex] +
            HangulVowelNames[vowelIndex] + HangulTrailingNames[trailingIndex];
    }

    internal static int LookupAlgorithmicName(string name)
    {
        if (name.StartsWith("HANGUL SYLLABLE ", System.StringComparison.OrdinalIgnoreCase))
        {
            int codePoint = UnicodeHangul.SBase;
            while (codePoint < UnicodeHangul.SBase + UnicodeHangul.SCount)
            {
                if (string.Equals(GetHangulSyllableName(codePoint), name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return codePoint;
                }

                codePoint = codePoint + 1;
            }

            return -1;
        }

        int[] starts = UnicodeNameTables.AlgorithmicNameStarts();
        int[] ends = UnicodeNameTables.AlgorithmicNameEnds();
        string[] prefixes = UnicodeNameTables.AlgorithmicNamePrefixes();
        int index = 0;
        while (index < starts.Length)
        {
            if (prefixes[index] != "HANGUL SYLLABLE")
            {
                string namePrefix = prefixes[index] + "-";
                if (name.StartsWith(namePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    int codePoint = ParseHexCodePoint(name, namePrefix.Length);
                    if (codePoint >= starts[index] && codePoint <= ends[index] &&
                        string.Equals(GetAlgorithmicName(codePoint), name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return codePoint;
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int ParseHexCodePoint(string text, int start)
    {
        if (start >= text.Length)
        {
            return -1;
        }

        int value = 0;
        int index = start;
        while (index < text.Length)
        {
            char character = text[index];
            int digit = -1;
            if (character >= '0' && character <= '9')
            {
                digit = character - '0';
            }
            else if (character >= 'A' && character <= 'F')
            {
                digit = character - 'A' + 10;
            }
            else if (character >= 'a' && character <= 'f')
            {
                digit = character - 'a' + 10;
            }

            if (digit < 0 || value > 0x10FFFF / 16)
            {
                return -1;
            }

            value = (value * 16) + digit;
            if (value > 0x10FFFF)
            {
                return -1;
            }

            index = index + 1;
        }

        return value;
    }

    internal static int FindPropertyAliasRecord(string alias)
    {
        string[] shortNames = UnicodeAliasTables.PropertyShortNames();
        string[] longNames = UnicodeAliasTables.PropertyLongNames();
        int[] offsets = UnicodeAliasTables.PropertyAliasOffsets();
        int[] lengths = UnicodeAliasTables.PropertyAliasLengths();
        string[] aliases = UnicodeAliasTables.PropertyAliasNames();
        int index = 0;
        while (index < shortNames.Length)
        {
            if (LooseAliasEquals(shortNames[index], alias))
            {
                return index;
            }

            if (LooseAliasEquals(longNames[index], alias))
            {
                return index;
            }

            int aliasIndex = 0;
            while (aliasIndex < lengths[index])
            {
                if (LooseAliasEquals(aliases[offsets[index] + aliasIndex], alias))
                {
                    return index;
                }

                aliasIndex = aliasIndex + 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool LooseAliasEquals(string left, string right)
    {
        string normalizedLeft = NormalizeLooseAlias(left);
        string normalizedRight = NormalizeLooseAlias(right);
        if (normalizedLeft == normalizedRight)
        {
            return true;
        }

        return NormalizeLooseNumericAlias(normalizedLeft) == NormalizeLooseNumericAlias(normalizedRight);
    }

    internal static string NormalizeLooseAlias(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isNumericSign = character == '-' && index == 0 && value.Length > 1 &&
                value[1] >= '0' && value[1] <= '9';
            if (character != '_' && !char.IsWhiteSpace(character) &&
                (character != '-' || isNumericSign))
            {
                builder.Append(char.ToLowerInvariant(character));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeLooseNumericAlias(string value)
    {
        int start = 0;
        bool negative = false;
        if (value.Length > 0 && (value[0] == '-' || value[0] == '+'))
        {
            negative = value[0] == '-';
            start = 1;
        }

        int decimalIndex = -1;
        int index = start;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (decimalIndex >= 0)
                {
                    return value;
                }

                decimalIndex = index;
            }
            else if (character < '0' || character > '9')
            {
                return value;
            }

            index = index + 1;
        }

        if (index == start)
        {
            return value;
        }

        int integerEnd = decimalIndex < 0 ? value.Length : decimalIndex;
        int integerStart = start;
        while (integerStart < integerEnd - 1 && value[integerStart] == '0')
        {
            integerStart = integerStart + 1;
        }

        int fractionEnd = value.Length;
        if (decimalIndex >= 0)
        {
            while (fractionEnd > decimalIndex + 1 && value[fractionEnd - 1] == '0')
            {
                fractionEnd = fractionEnd - 1;
            }
        }

        bool hasNonzero = false;
        index = integerStart;
        while (index < integerEnd)
        {
            if (value[index] != '0')
            {
                hasNonzero = true;
            }

            index = index + 1;
        }

        index = decimalIndex + 1;
        while (decimalIndex >= 0 && index < fractionEnd)
        {
            if (value[index] != '0')
            {
                hasNonzero = true;
            }

            index = index + 1;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (negative && hasNonzero)
        {
            builder.Append('-');
        }

        if (integerStart >= integerEnd)
        {
            builder.Append('0');
        }
        else
        {
            index = integerStart;
            while (index < integerEnd)
            {
                builder.Append(value[index]);
                index = index + 1;
            }
        }

        if (decimalIndex >= 0 && fractionEnd > decimalIndex + 1)
        {
            builder.Append('.');
            index = decimalIndex + 1;
            while (index < fractionEnd)
            {
                builder.Append(value[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static UnicodeDecompositionType DecompositionTypeFromName(string name)
    {
        if (name.Length == 0)
        {
            return UnicodeDecompositionType.Canonical;
        }

        if (name == "font")
        {
            return UnicodeDecompositionType.Font;
        }

        if (name == "noBreak")
        {
            return UnicodeDecompositionType.NoBreak;
        }

        if (name == "initial")
        {
            return UnicodeDecompositionType.Initial;
        }

        if (name == "medial")
        {
            return UnicodeDecompositionType.Medial;
        }

        if (name == "final")
        {
            return UnicodeDecompositionType.Final;
        }

        if (name == "isolated")
        {
            return UnicodeDecompositionType.Isolated;
        }

        if (name == "circle")
        {
            return UnicodeDecompositionType.Circle;
        }

        if (name == "super")
        {
            return UnicodeDecompositionType.Super;
        }

        if (name == "sub")
        {
            return UnicodeDecompositionType.Sub;
        }

        if (name == "vertical")
        {
            return UnicodeDecompositionType.Vertical;
        }

        if (name == "wide")
        {
            return UnicodeDecompositionType.Wide;
        }

        if (name == "narrow")
        {
            return UnicodeDecompositionType.Narrow;
        }

        if (name == "small")
        {
            return UnicodeDecompositionType.Small;
        }

        if (name == "square")
        {
            return UnicodeDecompositionType.Square;
        }

        if (name == "fraction")
        {
            return UnicodeDecompositionType.Fraction;
        }

        return UnicodeDecompositionType.Compat;
    }
}
