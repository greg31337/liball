namespace ttf;

internal class PdfFontUsageMapping
{
    int glyphId;
    int[] unicodeSequence;

    internal PdfFontUsageMapping(int glyphId, int[] unicodeSequence)
    {
        this.glyphId = glyphId;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int[] CopyUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class PdfFontUsageKey
{
    string value;

    PdfFontUsageKey(string value)
    {
        this.value = value;
    }

    internal static PdfFontUsageKey Create(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (instance == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        AppendString(
            builder,
            FontSetFaceIdentity.Create(instance.GetFace()));
        AppendString(builder, instance.GetCoordinates().GetKey());
        AppendInteger(builder, (int)options.GetFontKind());
        AppendInteger(builder, (int)options.GetVersion());
        AppendInteger(builder, (int)options.GetWritingMode());
        AppendInteger(builder, (int)options.GetEmbeddingMode());
        AppendInteger(builder, (int)options.GetRightsPolicy());
        AppendInteger(builder, (int)options.GetArchivalProfile());
        AppendInteger(builder, (int)options.GetAccessibilityProfile());
        AppendInteger(builder, (int)options.GetCidAllocationMode());
        string? prefixOverride = options.GetSubsetPrefixOverride();
        AppendString(builder, prefixOverride == null ? "" : prefixOverride);
        AppendInteger(builder, (int)options.GetHintPolicy());
        AppendInteger(builder, (int)options.GetLayoutPolicy());
        AppendInteger(builder, (int)options.GetVerticalPolicy());
        AppendInteger(builder, (int)options.GetColorPolicy());
        AppendInteger(builder, (int)options.GetMetadataPolicy());
        AppendInteger(builder, (int)options.GetSimpleFontPolicy());
        AppendInteger(builder, (int)options.GetStaticInstancePolicy());
        AppendInteger(builder, (int)options.GetVersionCompatibilityPolicy());
        AppendInteger(builder, (int)options.GetProfileCompatibilityPolicy());
        return new PdfFontUsageKey(builder.ToString());
    }

    internal string GetValue()
    {
        return value;
    }

    static void AppendString(
        System.Text.StringBuilder builder,
        string value)
    {
        AppendInteger(builder, value.Length);
        builder.Append(value);
        builder.Append(';');
    }

    static void AppendInteger(
        System.Text.StringBuilder builder,
        int value)
    {
        builder.Append(value.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
    }
}

internal class PdfFontUsage
{
    PdfFontUsageKey key;
    FontInstance instance;
    PdfFontOptions options;
    int maximumEntries;
    System.Collections.Generic.HashSet<int> glyphIds;
    System.Collections.Generic.List<int> orderedGlyphIds;
    System.Collections.Generic.List<PdfFontUsageMapping> mappings;
    bool frozen;

    internal PdfFontUsage(
        PdfFontUsageKey key,
        FontInstance instance,
        PdfFontOptions options)
    {
        this.key = key;
        this.instance = instance;
        this.options = options.Copy();
        maximumEntries = FontOperationContext.CreateEffectiveLimits(
            instance.GetFace().GetTableCache().GetLimits(),
            this.options.GetOperationSettings()).GetMaxCoverageEntries();
        glyphIds = new System.Collections.Generic.HashSet<int>();
        orderedGlyphIds = new System.Collections.Generic.List<int>();
        mappings =
            new System.Collections.Generic.List<PdfFontUsageMapping>();
        glyphIds.Add(0);
        orderedGlyphIds.Add(0);
        frozen = false;
    }

    internal string GetKey()
    {
        return key.GetValue();
    }

    internal FontInstance GetInstance()
    {
        return instance;
    }

    internal PdfFontOptions CopyOptions()
    {
        return options.Copy();
    }

    internal void AddText(string text)
    {
        EnsureMutable();

        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        RuneBuffer runes = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            instance.GetFace().GetTableCache().GetLimits()
                .GetMaxGlyphsPerFace());
        SfntCmapMapping? cmap = instance.GetFace().GetCmapMapping();
        int index = 0;

        while (index < runes.GetCount())
        {
            int scalar = runes.GetScalarValue(index);
            System.ArgumentNullException.ThrowIfNull(cmap);
            AddGlyph(cmap.MapUnicode(scalar), new int[] { scalar });
            index += 1;
        }
    }

    internal void AddShapedRun(ShapedRun run)
    {
        EnsureMutable();

        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        int index = 0;

        while (index < run.GetGlyphCount())
        {
            ShapedGlyph glyph = run.GetGlyph(index);
            int[] sequence = new int[glyph.GetUnicodeScalarCount()];
            int scalarIndex = 0;

            while (scalarIndex < sequence.Length)
            {
                sequence[scalarIndex] =
                    glyph.GetUnicodeScalar(scalarIndex);
                scalarIndex += 1;
            }

            AddGlyph(glyph.GetGlyphId(), sequence);
            index += 1;
        }
    }

    internal void AddGlyph(int glyphId, int[] unicodeSequence)
    {
        EnsureMutable();

        if (unicodeSequence == null)
        {
            throw new System.ArgumentNullException("unicodeSequence");
        }

        if (glyphId < 0 || glyphId >= instance.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        ValidateUnicodeSequence(unicodeSequence);

        if (!glyphIds.Contains(glyphId))
        {
            if (glyphIds.Count >= maximumEntries)
            {
                throw FontLimitException.Create(
                    "PDF font usage exceeds the configured glyph limit.");
            }

            glyphIds.Add(glyphId);
            orderedGlyphIds.Add(glyphId);
        }

        if (unicodeSequence.Length != 0 &&
            !ContainsMapping(glyphId, unicodeSequence))
        {
            if (mappings.Count >= maximumEntries)
            {
                throw FontLimitException.Create(
                    "PDF font usage exceeds the configured mapping limit.");
            }

            mappings.Add(new PdfFontUsageMapping(
                glyphId,
                unicodeSequence));
        }
    }

    internal int[] CopyGlyphIds()
    {
        int[] copy = new int[glyphIds.Count];
        glyphIds.CopyTo(copy);
        System.Array.Sort(copy);
        return copy;
    }

    internal int[] CopyGlyphIdsInUseOrder()
    {
        return orderedGlyphIds.ToArray();
    }

    internal PdfFontUsageMapping[] CopyMappings()
    {
        PdfFontUsageMapping[] copy =
            new PdfFontUsageMapping[mappings.Count];
        int index = 0;

        while (index < copy.Length)
        {
            PdfFontUsageMapping mapping = mappings[index];
            copy[index] = new PdfFontUsageMapping(
                mapping.GetGlyphId(),
                mapping.CopyUnicodeSequence());
            index += 1;
        }

        System.Array.Sort(
            copy,
            (left, right) => CompareMappings(left, right));
        return copy;
    }

    internal void Freeze()
    {
        frozen = true;
    }

    internal bool IsFrozen()
    {
        return frozen;
    }

    void EnsureMutable()
    {
        if (frozen)
        {
            throw new System.InvalidOperationException(
                "PDF font usage has already been finalized.");
        }
    }

    bool ContainsMapping(int glyphId, int[] sequence)
    {
        int index = 0;

        while (index < mappings.Count)
        {
            PdfFontUsageMapping mapping = mappings[index];

            if (mapping.GetGlyphId() == glyphId &&
                SequencesEqual(
                    mapping.CopyUnicodeSequence(),
                    sequence))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static int CompareMappings(
        PdfFontUsageMapping left,
        PdfFontUsageMapping right)
    {
        int comparison = left.GetGlyphId().CompareTo(right.GetGlyphId());

        if (comparison != 0)
        {
            return comparison;
        }

        int[] leftSequence = left.CopyUnicodeSequence();
        int[] rightSequence = right.CopyUnicodeSequence();
        int commonLength = System.Math.Min(
            leftSequence.Length,
            rightSequence.Length);
        int index = 0;

        while (index < commonLength)
        {
            comparison = leftSequence[index].CompareTo(
                rightSequence[index]);

            if (comparison != 0)
            {
                return comparison;
            }

            index += 1;
        }

        return leftSequence.Length.CompareTo(rightSequence.Length);
    }

    static bool SequencesEqual(int[] left, int[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;

        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static void ValidateUnicodeSequence(int[] sequence)
    {
        int index = 0;

        while (index < sequence.Length)
        {
            int scalar = sequence[index];

            if (scalar < 0 || scalar > 0x10FFFF ||
                (scalar >= 0xD800 && scalar <= 0xDFFF))
            {
                throw new System.ArgumentException(
                    "A PDF font usage sequence contains an invalid Unicode scalar.",
                    "unicodeSequence");
            }

            index += 1;
        }
    }
}

internal class PdfFontUsageAccumulator
{
    System.Collections.Generic.Dictionary<string, PdfFontUsage> usages;
    System.Collections.Generic.List<PdfFontUsage> orderedUsages;

    internal PdfFontUsageAccumulator()
    {
        usages =
            new System.Collections.Generic.Dictionary<string, PdfFontUsage>(
                System.StringComparer.Ordinal);
        orderedUsages =
            new System.Collections.Generic.List<PdfFontUsage>();
    }

    internal PdfFontUsage GetOrAdd(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (instance == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        PdfFontUsageKey key = PdfFontUsageKey.Create(instance, options);
        string value = key.GetValue();

        if (usages.ContainsKey(value))
        {
            return usages[value];
        }

        PdfFontUsage usage = new PdfFontUsage(key, instance, options);
        usages.Add(value, usage);
        orderedUsages.Add(usage);
        return usage;
    }

    internal int GetCount()
    {
        return orderedUsages.Count;
    }

    internal PdfFontUsage GetUsage(int index)
    {
        if (index < 0 || index >= orderedUsages.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return orderedUsages[index];
    }
}
