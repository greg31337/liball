namespace ttf;

/// <summary>Incrementally collects requests for one deterministic font subset.</summary>
/// <remarks>
/// The builder is bound to one immutable font instance and takes a snapshot of
/// the supplied options. Repeated set-like inputs are retained only once.
/// </remarks>
public class FontSubsetBuilder : IFontSubsetBuilder
{
    FontInstance instance;
    FontSubsetOptions options;
    int maximumEntries;
    System.Collections.Generic.List<int> codePoints;
    System.Collections.Generic.HashSet<int> codePointSet;
    System.Collections.Generic.List<int> glyphIds;
    System.Collections.Generic.HashSet<int> glyphIdSet;
    System.Collections.Generic.List<int> featureTags;
    System.Collections.Generic.HashSet<int> featureTagSet;
    System.Collections.Generic.List<int> paletteIndices;
    System.Collections.Generic.HashSet<int> paletteIndexSet;
    System.Collections.Generic.List<int[]> unicodeSequences;
    System.Collections.Generic.List<FontSubsetGlyphRequest> shapedGlyphs;
    FontSubset? builtSubset;

    /// <summary>Creates a request builder for one font instance.</summary>
    /// <param name="instance">The immutable font instance to subset.</param>
    /// <param name="options">The options to snapshot for the subset.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>instance</c> or <c>options</c> is null.
    /// </exception>
    public FontSubsetBuilder(
        FontInstance instance,
        FontSubsetOptions options)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        this.instance = instance;
        this.options = CopyOptions(options);
        FontLimits effectiveLimits = FontOperationContext.CreateEffectiveLimits(
            instance.GetFace().GetTableCache().GetLimits(),
            this.options.GetOperationSettings());
        maximumEntries = effectiveLimits.GetMaxCoverageEntries();
        codePoints = new System.Collections.Generic.List<int>();
        codePointSet = new System.Collections.Generic.HashSet<int>();
        glyphIds = new System.Collections.Generic.List<int>();
        glyphIdSet = new System.Collections.Generic.HashSet<int>();
        featureTags = new System.Collections.Generic.List<int>();
        featureTagSet = new System.Collections.Generic.HashSet<int>();
        paletteIndices = new System.Collections.Generic.List<int>();
        paletteIndexSet = new System.Collections.Generic.HashSet<int>();
        unicodeSequences = new System.Collections.Generic.List<int[]>();
        shapedGlyphs =
            new System.Collections.Generic.List<FontSubsetGlyphRequest>();
    }

    /// <summary>Gets the immutable font instance bound to this builder.</summary>
    /// <returns>The constructor-supplied source instance.</returns>
    public FontInstance GetSourceInstance()
    {
        return instance;
    }

    /// <summary>Gets a copy of the effective subset options.</summary>
    /// <returns>A defensive copy of the constructor-time option snapshot.</returns>
    public FontSubsetOptions GetEffectiveOptions()
    {
        return CopyOptions(options);
    }

    /// <summary>Adds one Unicode scalar request.</summary>
    /// <param name="codePoint">The Unicode scalar value.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>codePoint</c> is not a Unicode scalar value.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddCodePoint(int codePoint)
    {
        Checkpoint();
        EnsureMutable();
        ValidateCodePoint(codePoint, "codePoint");
        AddCodePointValue(codePoint);
        AddUnicodeSequenceValue(new int[] { codePoint });
    }

    /// <summary>Adds every Unicode scalar in a UTF-16 string.</summary>
    /// <param name="text">Strictly valid UTF-16 text.</param>
    /// <exception cref="System.ArgumentNullException"><c>text</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>text</c> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request or decoded-rune limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddText(string text)
    {
        Checkpoint();
        EnsureMutable();

        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        int maximumRunes = GetEffectiveLimits().GetMaxGlyphsPerFace();
        RuneBuffer runes = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            maximumRunes);
        int count = runes.GetCount();

        if (count == 0)
        {
            return;
        }

        int[] sequence = new int[count];
        int index = 0;

        while (index < count)
        {
            sequence[index] = runes.GetScalarValue(index);
            index++;
        }

        index = 0;
        while (index < sequence.Length)
        {
            AddCodePointValue(sequence[index]);
            index++;
        }

        AddUnicodeSequenceValue(sequence);
    }

    /// <summary>Adds one exact sequence of Unicode scalar values.</summary>
    /// <remarks>
    /// Every scalar is also retained as a nominal character request. The exact
    /// sequence is used for variation-sequence discovery and deterministic
    /// subset identity.
    /// </remarks>
    /// <param name="unicodeSequence">A nonempty sequence of Unicode scalars.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>unicodeSequence</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// <c>unicodeSequence</c> is empty.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The sequence contains a value that is not a Unicode scalar.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddUnicodeSequence(int[] unicodeSequence)
    {
        Checkpoint();
        EnsureMutable();
        ValidateUnicodeSequence(unicodeSequence);
        int index = 0;

        while (index < unicodeSequence.Length)
        {
            AddCodePointValue(unicodeSequence[index]);
            index += 1;
        }

        AddUnicodeSequenceValue(unicodeSequence);
    }

    /// <summary>Adds one raw glyph identifier.</summary>
    /// <param name="glyphId">The glyph identifier in this builder's instance.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font instance.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddGlyph(int glyphId)
    {
        Checkpoint();
        EnsureMutable();
        ValidateGlyphId(glyphId);
        AddGlyphValue(glyphId);
    }

    /// <summary>Adds a glyph with its exact Unicode scalar sequence.</summary>
    /// <remarks>
    /// The association is retained even when the source cmap maps the sequence
    /// differently. Its scalars and exact sequence are also added as character
    /// requests.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier in this builder's instance.</param>
    /// <param name="unicodeSequence">A nonempty sequence of Unicode scalars.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>unicodeSequence</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// <c>unicodeSequence</c> is empty.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The glyph is outside the font or the sequence contains a non-scalar value.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddGlyphUnicodeSequence(
        int glyphId,
        int[] unicodeSequence)
    {
        Checkpoint();
        EnsureMutable();
        ValidateGlyphId(glyphId);
        ValidateUnicodeSequence(unicodeSequence);
        AddGlyphValue(glyphId);
        int index = 0;

        while (index < unicodeSequence.Length)
        {
            AddCodePointValue(unicodeSequence[index]);
            index += 1;
        }

        AddUnicodeSequenceValue(unicodeSequence);
        AddGlyphUnicodeSequenceValue(glyphId, unicodeSequence);
    }

    /// <summary>Adds glyph and exact Unicode requests from a shaped run.</summary>
    /// <param name="run">
    /// A run shaped for this builder's font instance.
    /// </param>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run contains a glyph outside this builder's font instance or an
    /// invalid Unicode scalar.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddShapedRun(ShapedRun run)
    {
        Checkpoint();
        EnsureMutable();

        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        ValidateShapedRun(run);
        int glyphIndex = 0;

        while (glyphIndex < run.GetGlyphCount())
        {
            ShapedGlyph glyph = run.GetGlyph(glyphIndex);
            int glyphId = glyph.GetGlyphId();
            int[] sequence = CopyGlyphScalars(glyph);
            AddGlyphValue(glyphId);

            if (sequence.Length != 0)
            {
                AddGlyphUnicodeSequenceValue(glyphId, sequence);
            }

            glyphIndex++;
        }

        int clusterIndex = 0;

        while (clusterIndex < run.GetClusterCount())
        {
            ShapedCluster cluster = run.GetCluster(clusterIndex);
            int[] sequence = CopyClusterScalars(cluster);
            int scalarIndex = 0;

            while (scalarIndex < sequence.Length)
            {
                AddCodePointValue(sequence[scalarIndex]);
                scalarIndex++;
            }

            if (sequence.Length != 0)
            {
                AddUnicodeSequence(sequence);
            }

            clusterIndex++;
        }
    }

    /// <summary>Adds one fallback-resolved shaping run from this builder's instance.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    public void AddResolvedRun(TextShapingRun run)
    {
        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        if (!object.ReferenceEquals(run.GetFontInstance(), instance))
        {
            throw new System.ArgumentException(
                "The resolved run belongs to a different font instance.",
                "run");
        }

        AddShapedRun(run.GetShapedRun());
    }

    /// <summary>Adds one OpenType feature tag request.</summary>
    /// <param name="featureTag">A four-byte printable ASCII OpenType tag.</param>
    /// <exception cref="System.ArgumentException">
    /// <c>featureTag</c> is not a printable four-byte tag.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddFeature(int featureTag)
    {
        Checkpoint();
        EnsureMutable();
        ValidateTag(featureTag);

        if (!featureTagSet.Contains(featureTag))
        {
            EnsureCapacity(featureTags.Count, "feature tag");
            featureTagSet.Add(featureTag);
            featureTags.Add(featureTag);
        }
    }

    /// <summary>Adds one CPAL palette index request.</summary>
    /// <param name="paletteIndex">The zero-based palette index.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The font has no such palette.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    public void AddPalette(int paletteIndex)
    {
        Checkpoint();
        EnsureMutable();
        SfntCpalTable? cpal = instance.GetFace().GetCpalTable();

        if (cpal == null ||
            paletteIndex < 0 ||
            paletteIndex >= cpal.GetPaletteCount())
        {
            throw new System.ArgumentOutOfRangeException("paletteIndex");
        }

        if (!paletteIndexSet.Contains(paletteIndex))
        {
            EnsureCapacity(paletteIndices.Count, "palette");
            paletteIndexSet.Add(paletteIndex);
            paletteIndices.Add(paletteIndex);
        }
    }

    /// <summary>Freezes and returns the deterministic subset request.</summary>
    /// <remarks>
    /// The first call sorts every set-like input and permanently prevents
    /// additions. Later calls return the same immutable result.
    /// </remarks>
    /// <returns>The immutable subset request.</returns>
    public FontSubset Build()
    {
        Checkpoint();
        if (builtSubset == null)
        {
            FontSubsetEmbeddingDecision embeddingDecision =
                FontSubsetRightsResolver.Resolve(instance, options);
            FontSubsetOptions effectiveOptions = CopyOptions(options);
            effectiveOptions.SetEmbeddingMode(
                embeddingDecision.GetEffectiveEmbeddingMode());
            int[] sortedCodePoints = CopyAndSort(codePoints);
            int[] sortedGlyphIds = CopyAndSort(glyphIds);
            int[][] sortedSequences =
                CopyAndSortSequences(unicodeSequences);
            FontSubsetGlyphRequest[] sortedShapedGlyphs =
                CopyAndSortShapedGlyphs(shapedGlyphs);
            FontSubsetSeed seed = FontSubsetSeedBuilder.Create(
                instance,
                effectiveOptions,
                sortedCodePoints,
                sortedGlyphIds,
                CopyAndSort(featureTags),
                CopyAndSort(paletteIndices),
                sortedSequences,
                sortedShapedGlyphs);
            FontSubsetGlyphAssignment glyphAssignment =
                FontSubsetGlyphAssignmentBuilder.Create(
                    instance.GetGlyphCount(),
                    seed.CopyGlyphIds(),
                    effectiveOptions.GetEmbeddingMode(),
                    effectiveOptions.GetGlyphRetentionMode());
            FontDiagnostic[] diagnostics = MergeDiagnostics(
                embeddingDecision.CopyDiagnostics(),
                seed.CopyDiagnostics(),
                GetEffectiveLimits());
            builtSubset = new FontSubset(
                instance,
                effectiveOptions,
                sortedCodePoints,
                seed.CopyGlyphIds(),
                seed.CopyRecommendedGlyphIds(),
                CopyAndSort(featureTags),
                CopyAndSort(paletteIndices),
                sortedSequences,
                sortedShapedGlyphs,
                seed.CopyUnicodeMappings(),
                seed.CopyLayoutClosure(),
                seed.CopyColorClosure(),
                seed.CopyCompositeClosure(),
                seed.CopyCffClosure(),
                glyphAssignment,
                embeddingDecision,
                diagnostics);
        }

        return builtSubset;
    }

    /// <summary>Builds the request and generates its complete output result.</summary>
    /// <returns>The generated standalone font and all associated reports.</returns>
    public FontSubsetResult BuildResult()
    {
        Checkpoint();
        FontSubsetResult result = Build().CreateResult();
        Checkpoint();
        FontOperationContext.ValidateOutputLength(
            result.GetFontByteLength(),
            GetEffectiveLimits());
        return result;
    }

    internal FontInstance GetInstance()
    {
        return instance;
    }

    internal FontSubsetOptions CopyOptions()
    {
        return CopyOptions(options);
    }

    internal int GetCodePointCount()
    {
        return codePoints.Count;
    }

    internal int GetCodePoint(int index)
    {
        return GetValue(codePoints, index);
    }

    internal int GetGlyphCount()
    {
        return glyphIds.Count;
    }

    internal int GetGlyph(int index)
    {
        return GetValue(glyphIds, index);
    }

    internal int GetFeatureCount()
    {
        return featureTags.Count;
    }

    internal int GetFeature(int index)
    {
        return GetValue(featureTags, index);
    }

    internal int GetPaletteCount()
    {
        return paletteIndices.Count;
    }

    internal int GetPalette(int index)
    {
        return GetValue(paletteIndices, index);
    }

    internal int GetUnicodeSequenceCount()
    {
        return unicodeSequences.Count;
    }

    internal int[] CopyUnicodeSequence(int index)
    {
        ValidateIndex(index, unicodeSequences.Count);
        return CopyIntegers(unicodeSequences[index]);
    }

    internal int GetShapedGlyphCount()
    {
        return shapedGlyphs.Count;
    }

    internal FontSubsetGlyphRequest GetShapedGlyph(int index)
    {
        ValidateIndex(index, shapedGlyphs.Count);
        return shapedGlyphs[index];
    }

    void EnsureMutable()
    {
        if (builtSubset != null)
        {
            throw new System.InvalidOperationException(
                "Font subset requests cannot be added after Build.");
        }
    }

    void Checkpoint()
    {
        FontOperationContext.Checkpoint(options.GetOperationSettings());
    }

    FontLimits GetEffectiveLimits()
    {
        return FontOperationContext.CreateEffectiveLimits(
            instance.GetFace().GetTableCache().GetLimits(),
            options.GetOperationSettings());
    }

    void ValidateShapedRun(ShapedRun run)
    {
        int glyphIndex = 0;

        while (glyphIndex < run.GetGlyphCount())
        {
            ShapedGlyph glyph = run.GetGlyph(glyphIndex);
            int glyphId = glyph.GetGlyphId();

            if (glyphId < 0 || glyphId >= instance.GetGlyphCount())
            {
                throw new System.ArgumentException(
                    "The shaped run contains a glyph outside the font instance.",
                    "run");
            }

            ValidateGlyphScalars(glyph);
            glyphIndex++;
        }

        int clusterIndex = 0;

        while (clusterIndex < run.GetClusterCount())
        {
            ShapedCluster cluster = run.GetCluster(clusterIndex);
            int scalarIndex = 0;

            while (scalarIndex < cluster.GetUnicodeScalarCount())
            {
                int scalar = cluster.GetUnicodeScalar(scalarIndex);

                if (!IsValidCodePoint(scalar))
                {
                    throw new System.ArgumentException(
                        "The shaped run contains an invalid Unicode scalar.",
                        "run");
                }

                scalarIndex++;
            }

            clusterIndex++;
        }
    }

    void ValidateGlyphScalars(ShapedGlyph glyph)
    {
        int index = 0;

        while (index < glyph.GetUnicodeScalarCount())
        {
            if (!IsValidCodePoint(glyph.GetUnicodeScalar(index)))
            {
                throw new System.ArgumentException(
                    "The shaped run contains an invalid Unicode scalar.",
                    "run");
            }

            index++;
        }
    }

    void AddCodePointValue(int codePoint)
    {
        if (!codePointSet.Contains(codePoint))
        {
            EnsureCapacity(codePoints.Count, "code point");
            codePointSet.Add(codePoint);
            codePoints.Add(codePoint);
        }
    }

    void AddGlyphValue(int glyphId)
    {
        if (!glyphIdSet.Contains(glyphId))
        {
            EnsureCapacity(glyphIds.Count, "glyph");
            glyphIdSet.Add(glyphId);
            glyphIds.Add(glyphId);
        }
    }

    void AddUnicodeSequenceValue(int[] sequence)
    {
        if (ContainsSequence(unicodeSequences, sequence))
        {
            return;
        }

        EnsureCapacity(unicodeSequences.Count, "Unicode sequence");
        unicodeSequences.Add(CopyIntegers(sequence));
    }

    void AddGlyphUnicodeSequenceValue(int glyphId, int[] sequence)
    {
        int index = 0;

        while (index < shapedGlyphs.Count)
        {
            FontSubsetGlyphRequest request = shapedGlyphs[index];

            if (request.GetGlyphId() == glyphId &&
                SequencesEqual(request.CopyUnicodeSequence(), sequence))
            {
                return;
            }

            index++;
        }

        EnsureCapacity(shapedGlyphs.Count, "shaped glyph");
        shapedGlyphs.Add(new FontSubsetGlyphRequest(glyphId, sequence));
    }

    void EnsureCapacity(int currentCount, string requestKind)
    {
        if (currentCount >= maximumEntries)
        {
            throw FontLimitException.Create(
                "Font subset " + requestKind +
                " requests exceed the configured entry limit.");
        }
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= instance.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }

    void ValidateUnicodeSequence(int[] unicodeSequence)
    {
        if (unicodeSequence == null)
        {
            throw new System.ArgumentNullException("unicodeSequence");
        }
        if (unicodeSequence.Length == 0)
        {
            throw new System.ArgumentException(
                "A Unicode sequence cannot be empty.",
                "unicodeSequence");
        }
        if (unicodeSequence.Length > maximumEntries)
        {
            throw FontLimitException.Create(
                "A font subset Unicode sequence exceeds the configured entry limit.");
        }

        int index = 0;

        while (index < unicodeSequence.Length)
        {
            ValidateCodePoint(unicodeSequence[index], "unicodeSequence");
            index += 1;
        }
    }

    static void ValidateCodePoint(int codePoint, string parameterName)
    {
        if (!IsValidCodePoint(codePoint))
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static bool IsValidCodePoint(int codePoint)
    {
        return codePoint >= 0 &&
            codePoint <= 0x10FFFF &&
            (codePoint < 0xD800 || codePoint > 0xDFFF);
    }

    static void ValidateTag(int tag)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int shift = 24;

        while (shift >= 0)
        {
            int character = (int)(value >> shift) & 0xFF;

            if (character < 0x20 || character > 0x7E)
            {
                throw new System.ArgumentException(
                    "A feature tag must contain four printable ASCII bytes.",
                    "featureTag");
            }

            shift -= 8;
        }
    }

    static int[] CopyGlyphScalars(ShapedGlyph glyph)
    {
        int[] sequence = new int[glyph.GetUnicodeScalarCount()];
        int index = 0;

        while (index < sequence.Length)
        {
            sequence[index] = glyph.GetUnicodeScalar(index);
            index++;
        }

        return sequence;
    }

    static int[] CopyClusterScalars(ShapedCluster cluster)
    {
        int[] sequence = new int[cluster.GetUnicodeScalarCount()];
        int index = 0;

        while (index < sequence.Length)
        {
            sequence[index] = cluster.GetUnicodeScalar(index);
            index++;
        }

        return sequence;
    }

    static bool ContainsSequence(
        System.Collections.Generic.List<int[]> values,
        int[] sequence)
    {
        int index = 0;

        while (index < values.Count)
        {
            if (SequencesEqual(values[index], sequence))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool SequencesEqual(int[] first, int[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;

        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[] CopyAndSort(
        System.Collections.Generic.List<int> values)
    {
        int[] copy = values.ToArray();
        System.Array.Sort(copy);
        return copy;
    }

    static int[][] CopyAndSortSequences(
        System.Collections.Generic.List<int[]> values)
    {
        int[][] copy = new int[values.Count][];
        int index = 0;

        while (index < copy.Length)
        {
            copy[index] = CopyIntegers(values[index]);
            index++;
        }

        System.Array.Sort(
            copy,
            (left, right) => CompareSequences(left, right));
        return copy;
    }

    static FontSubsetGlyphRequest[] CopyAndSortShapedGlyphs(
        System.Collections.Generic.List<FontSubsetGlyphRequest> values)
    {
        FontSubsetGlyphRequest[] copy =
            new FontSubsetGlyphRequest[values.Count];
        int index = 0;

        while (index < copy.Length)
        {
            FontSubsetGlyphRequest source = values[index];
            copy[index] = new FontSubsetGlyphRequest(
                source.GetGlyphId(),
                source.CopyUnicodeSequence());
            index++;
        }

        System.Array.Sort(
            copy,
            (left, right) => CompareShapedGlyphs(left, right));
        return copy;
    }

    static int CompareShapedGlyphs(
        FontSubsetGlyphRequest left,
        FontSubsetGlyphRequest right)
    {
        int glyphComparison = left.GetGlyphId().CompareTo(right.GetGlyphId());

        if (glyphComparison != 0)
        {
            return glyphComparison;
        }

        return CompareSequences(
            left.CopyUnicodeSequence(),
            right.CopyUnicodeSequence());
    }

    static int CompareSequences(int[] left, int[] right)
    {
        int length = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;

        while (index < length)
        {
            int comparison = left[index].CompareTo(right[index]);

            if (comparison != 0)
            {
                return comparison;
            }

            index++;
        }

        return left.Length.CompareTo(right.Length);
    }

    static FontSubsetOptions CopyOptions(FontSubsetOptions source)
    {
        FontSubsetOptions copy = FontSubsetOptions.Create(
            source.GetEmbeddingMode(),
            source.GetGlyphRetentionMode(),
            source.GetRecommendedGlyphPolicy(),
            source.GetHintPolicy(),
            source.GetLayoutPolicy(),
            source.GetVerticalPolicy(),
            source.GetColorPolicy(),
            source.GetMetadataPolicy(),
            source.GetUnknownTablePolicy(),
            source.GetRightsPolicy());
        copy.SetSubsetPrefixOverride(source.GetSubsetPrefixOverride());
        copy.SetOperationOptions(source.GetOperationOptions());
        return copy;
    }

    static FontDiagnostic[] MergeDiagnostics(
        FontDiagnostic[] first,
        FontDiagnostic[] second,
        FontLimits limits)
    {
        long count = (long)first.Length + second.Length;

        if (count > limits.GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "The configured diagnostic count limit was exceeded.");
        }

        FontDiagnostic[] output = new FontDiagnostic[(int)count];
        System.Array.Copy(first, 0, output, 0, first.Length);
        System.Array.Copy(
            second,
            0,
            output,
            first.Length,
            second.Length);
        return output;
    }

    static int GetValue(
        System.Collections.Generic.List<int> values,
        int index)
    {
        ValidateIndex(index, values.Count);
        return values[index];
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
