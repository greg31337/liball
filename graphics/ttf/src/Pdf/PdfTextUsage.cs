namespace ttf;

internal class PdfTextCodeObservation
{
    int resourceIndex;
    int runIndex;
    int glyphIndex;
    int characterCode;
    int sourceGlyphId;
    int sourceUtf16Start;
    int sourceUtf16Length;
    int[] unicodeSequence;

    internal PdfTextCodeObservation(
        int resourceIndex,
        int runIndex,
        int glyphIndex,
        int characterCode,
        int sourceGlyphId,
        int sourceUtf16Start,
        int sourceUtf16Length,
        int[] unicodeSequence)
    {
        this.resourceIndex = resourceIndex;
        this.runIndex = runIndex;
        this.glyphIndex = glyphIndex;
        this.characterCode = characterCode;
        this.sourceGlyphId = sourceGlyphId;
        this.sourceUtf16Start = sourceUtf16Start;
        this.sourceUtf16Length = sourceUtf16Length;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    internal int GetResourceIndex() { return resourceIndex; }
    internal int GetRunIndex() { return runIndex; }
    internal int GetGlyphIndex() { return glyphIndex; }
    internal int GetCharacterCode() { return characterCode; }
    internal int GetSourceGlyphId() { return sourceGlyphId; }
    internal int GetSourceUtf16Start() { return sourceUtf16Start; }
    internal int GetSourceUtf16Length() { return sourceUtf16Length; }
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

internal class PdfTextUsageEngine
{
    FontSet fontSet;
    PdfFontOptions options;
    PdfFontUsageAccumulator accumulator;
    System.Collections.Generic.Dictionary<string, int> resourceIndexes;
    System.Collections.Generic.List<PdfFontUsage> usages;
    System.Collections.Generic.List<PdfCidAllocator> allocators;
    System.Collections.Generic.List<PdfTextCodeObservation> observations;
    System.Collections.Generic.List<PdfTextUsageResult> operationResults;
    System.Collections.Generic.List<
        System.Collections.Generic.List<PdfTextCodeObservation>>
            operationObservationSets;
    PdfFontResource[]? finalizedResources;
    object syncRoot;

    internal PdfTextUsageEngine(
        FontSet fontSet,
        PdfFontOptions options)
    {
        if (fontSet == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        this.fontSet = fontSet;
        this.options = options.Copy();
        accumulator = new PdfFontUsageAccumulator();
        resourceIndexes =
            new System.Collections.Generic.Dictionary<string, int>(
                System.StringComparer.Ordinal);
        usages = new System.Collections.Generic.List<PdfFontUsage>();
        allocators = new System.Collections.Generic.List<PdfCidAllocator>();
        observations =
            new System.Collections.Generic.List<PdfTextCodeObservation>();
        operationResults =
            new System.Collections.Generic.List<PdfTextUsageResult>();
        operationObservationSets =
            new System.Collections.Generic.List<
                System.Collections.Generic.List<PdfTextCodeObservation>>();
        finalizedResources = null;
        syncRoot = new object();
    }

    internal PdfTextUsageResult AddText(
        string text,
        FontRequest request)
    {
        EnsureCanStartOperation();
        Checkpoint();
        TextShapingResult result = fontSet.ShapeText(text, request);
        return AddShapingResult(result);
    }

    internal PdfTextUsageResult AddTextWithOptions(
        string text,
        FontRequest request,
        TextShapingOptions shapingOptions)
    {
        EnsureCanStartOperation();
        Checkpoint();
        TextShapingResult result = fontSet.ShapeTextWithOptions(
            text,
            request,
            shapingOptions);
        return AddShapingResult(result);
    }

    internal PdfTextUsageResult AddShapingResult(
        TextShapingResult shapingResult)
    {
        if (shapingResult == null)
        {
            throw new System.ArgumentNullException("shapingResult");
        }

        lock (syncRoot)
        {
            Checkpoint();
            EnsureMutable();
            PdfTextUsageRun[] runs =
                new PdfTextUsageRun[shapingResult.GetRunCount()];
            System.Collections.Generic.List<PdfTextCodeObservation>
                operationObservations =
                    new System.Collections.Generic.List<
                        PdfTextCodeObservation>();
            int runIndex = 0;

            while (runIndex < runs.Length)
            {
                Checkpoint();
                TextShapingRun shapingRun = shapingResult.GetRun(runIndex);
                FontInstance instance = shapingRun.GetFontInstance();
                int resourceIndex = GetOrAddResource(instance);
                PdfFontUsage usage = usages[resourceIndex];
                PdfCidAllocator allocator = allocators[resourceIndex];
                PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
                    usage,
                    allocator,
                    shapingRun.GetShapedRun(),
                    0);
                runs[runIndex] = new PdfTextUsageRun(
                    resourceIndex,
                    instance,
                    shapingRun.GetUtf16Start(),
                    shapingRun.GetUtf16Length(),
                    encoded);
                AddObservations(
                    shapingRun.GetShapedRun(),
                    encoded,
                    resourceIndex,
                    runIndex,
                    operationObservations);
                runIndex += 1;
            }

            PdfTextUsageResult result = new PdfTextUsageResult(
                shapingResult,
                runs,
                new PdfTextActualTextRecommendation[0]);
            operationResults.Add(result);
            operationObservationSets.Add(operationObservations);
            RefreshRecommendations();
            return result;
        }
    }

    internal int GetResourceCount()
    {
        lock (syncRoot)
        {
            return usages.Count;
        }
    }

    internal PdfFontResource[] FinalizeFonts()
    {
        lock (syncRoot)
        {
            Checkpoint();
            EnsureMutable();
            PdfFontResource[] built =
                new PdfFontResource[usages.Count];
            int index = 0;

            while (index < built.Length)
            {
                built[index] = PdfFontResourceBuilder.Build(
                    usages[index],
                    allocators[index]);
                Checkpoint();
                FontOperationContext.ValidateOutputLength(
                    built[index].CopyEmbeddedFont().Length,
                    GetEffectiveLimits(usages[index].GetInstance()));
                index += 1;
            }

            index = 0;
            while (index < built.Length)
            {
                usages[index].Freeze();
                allocators[index].Freeze();
                index += 1;
            }

            finalizedResources = built;
            return CopyResources(built);
        }
    }

    internal bool IsFinalized()
    {
        lock (syncRoot)
        {
            return finalizedResources != null;
        }
    }

    internal PdfFontResource GetFinalizedResource(int resourceIndex)
    {
        lock (syncRoot)
        {
            if (finalizedResources == null)
            {
                throw new System.InvalidOperationException(
                    "The PDF text resources have not been finalized.");
            }

            if (resourceIndex < 0 ||
                resourceIndex >= finalizedResources.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    "resourceIndex");
            }

            return finalizedResources[resourceIndex];
        }
    }

    int GetOrAddResource(FontInstance instance)
    {
        PdfFontOptionResolver.ValidateInstance(instance, options);
        PdfFontUsage usage = accumulator.GetOrAdd(instance, options);
        string key = usage.GetKey();

        if (resourceIndexes.ContainsKey(key))
        {
            return resourceIndexes[key];
        }

        int index = usages.Count;
        resourceIndexes.Add(key, index);
        usages.Add(usage);
        allocators.Add(PdfCidAllocator.Create(usage));
        return index;
    }

    void AddObservations(
        ShapedRun shapedRun,
        PdfEncodedRun encoded,
        int resourceIndex,
        int runIndex,
        System.Collections.Generic.List<PdfTextCodeObservation>
            operationObservations)
    {
        int glyphIndex = 0;

        while (glyphIndex < shapedRun.GetGlyphCount())
        {
            ShapedGlyph glyph = shapedRun.GetGlyph(glyphIndex);
            int[] sequence = glyph.CopyUnicodeScalars();

            if (sequence.Length != 0)
            {
                PdfTextCodeObservation observation =
                    new PdfTextCodeObservation(
                        resourceIndex,
                        runIndex,
                        glyphIndex,
                        encoded.GetCid(glyphIndex),
                        glyph.GetGlyphId(),
                        glyph.GetUtf16Start(),
                        glyph.GetUtf16Length(),
                        sequence);
                observations.Add(observation);
                operationObservations.Add(observation);
            }

            glyphIndex += 1;
        }
    }

    PdfTextActualTextRecommendation[] CreateRecommendations(
        System.Collections.Generic.List<PdfTextCodeObservation>
            operationObservations)
    {
        System.Collections.Generic.List<PdfTextActualTextRecommendation>
            recommendations =
                new System.Collections.Generic.List<
                    PdfTextActualTextRecommendation>();
        int index = 0;

        while (index < operationObservations.Count)
        {
            PdfTextCodeObservation observation =
                operationObservations[index];
            int[] sequence = observation.CopyUnicodeSequence();
            int[] preferred = GetPreferredSequence(
                observation.GetResourceIndex(),
                observation.GetCharacterCode());
            bool multipleSequences = GetSequenceCount(
                observation.GetResourceIndex(),
                observation.GetCharacterCode()) > 1;
            bool sharedSequence = GetCodeCountForSequence(
                observation.GetResourceIndex(),
                sequence) > 1;
            int ownerCode = GetOwnerCode(
                observation.GetResourceIndex(),
                preferred);
            bool represented = SequencesEqual(sequence, preferred) &&
                observation.GetCharacterCode() == ownerCode;

            if (!represented)
            {
                PdfActualTextRecommendation mappingRecommendation =
                    new PdfActualTextRecommendation(
                        observation.GetCharacterCode(),
                        observation.GetSourceGlyphId(),
                        sequence,
                        GetReason(multipleSequences, sharedSequence));
                recommendations.Add(
                    new PdfTextActualTextRecommendation(
                        observation.GetResourceIndex(),
                        observation.GetRunIndex(),
                        observation.GetGlyphIndex(),
                        observation.GetSourceUtf16Start(),
                        observation.GetSourceUtf16Length(),
                        mappingRecommendation));
            }

            index += 1;
        }

        return recommendations.ToArray();
    }

    void RefreshRecommendations()
    {
        int index = 0;

        while (index < operationResults.Count)
        {
            operationResults[index].SetRecommendations(
                CreateRecommendations(operationObservationSets[index]));
            index += 1;
        }
    }

    int[] GetPreferredSequence(int resourceIndex, int characterCode)
    {
        int[]? preferred = null;
        int index = 0;

        while (index < observations.Count)
        {
            PdfTextCodeObservation candidate = observations[index];

            if (candidate.GetResourceIndex() == resourceIndex &&
                candidate.GetCharacterCode() == characterCode)
            {
                int[] sequence = candidate.CopyUnicodeSequence();

                if (preferred == null ||
                    CompareSequences(sequence, preferred) < 0)
                {
                    preferred = sequence;
                }
            }

            index += 1;
        }

        System.ArgumentNullException.ThrowIfNull(preferred);
        return preferred;
    }

    int GetSequenceCount(int resourceIndex, int characterCode)
    {
        System.Collections.Generic.List<int[]> distinct =
            new System.Collections.Generic.List<int[]>();
        int index = 0;

        while (index < observations.Count)
        {
            PdfTextCodeObservation candidate = observations[index];

            if (candidate.GetResourceIndex() == resourceIndex &&
                candidate.GetCharacterCode() == characterCode)
            {
                AddDistinct(
                    distinct,
                    candidate.CopyUnicodeSequence());
            }

            index += 1;
        }

        return distinct.Count;
    }

    int GetCodeCountForSequence(int resourceIndex, int[] sequence)
    {
        System.Collections.Generic.List<int> codes =
            new System.Collections.Generic.List<int>();
        int index = 0;

        while (index < observations.Count)
        {
            PdfTextCodeObservation candidate = observations[index];

            if (candidate.GetResourceIndex() == resourceIndex &&
                SequencesEqual(
                    candidate.CopyUnicodeSequence(),
                    sequence) &&
                !codes.Contains(candidate.GetCharacterCode()))
            {
                codes.Add(candidate.GetCharacterCode());
            }

            index += 1;
        }

        return codes.Count;
    }

    int GetOwnerCode(int resourceIndex, int[] sequence)
    {
        int ownerCode = int.MaxValue;
        System.Collections.Generic.List<int> visitedCodes =
            new System.Collections.Generic.List<int>();
        int index = 0;

        while (index < observations.Count)
        {
            PdfTextCodeObservation candidate = observations[index];
            int code = candidate.GetCharacterCode();

            if (candidate.GetResourceIndex() == resourceIndex &&
                !visitedCodes.Contains(code))
            {
                visitedCodes.Add(code);
                int[] preferred = GetPreferredSequence(
                    resourceIndex,
                    code);

                if (SequencesEqual(preferred, sequence) &&
                    code < ownerCode)
                {
                    ownerCode = code;
                }
            }

            index += 1;
        }

        return ownerCode;
    }

    void EnsureCanStartOperation()
    {
        lock (syncRoot)
        {
            EnsureMutable();
        }
    }

    void EnsureMutable()
    {
        if (finalizedResources != null)
        {
            throw new System.InvalidOperationException(
                "The PDF text resources have already been finalized.");
        }
    }

    void Checkpoint()
    {
        FontOperationContext.Checkpoint(options.GetOperationSettings());
    }

    FontLimits GetEffectiveLimits(FontInstance instance)
    {
        return FontOperationContext.CreateEffectiveLimits(
            instance.GetFace().GetTableCache().GetLimits(),
            options.GetOperationSettings());
    }

    static PdfActualTextReason GetReason(
        bool multipleSequences,
        bool sharedSequence)
    {
        if (multipleSequences && sharedSequence)
        {
            return PdfActualTextReason
                .MultipleSequencesForCodeAndSequenceSharedByCodes;
        }

        if (multipleSequences)
        {
            return PdfActualTextReason.MultipleSequencesForCode;
        }

        return PdfActualTextReason.SequenceSharedByCodes;
    }

    static void AddDistinct(
        System.Collections.Generic.List<int[]> values,
        int[] sequence)
    {
        int index = 0;

        while (index < values.Count)
        {
            if (SequencesEqual(values[index], sequence))
            {
                return;
            }

            index += 1;
        }

        values.Add(sequence);
    }

    static int CompareSequences(int[] left, int[] right)
    {
        int length = System.Math.Min(left.Length, right.Length);
        int index = 0;

        while (index < length)
        {
            int comparison = left[index].CompareTo(right[index]);

            if (comparison != 0)
            {
                return comparison;
            }

            index += 1;
        }

        return left.Length.CompareTo(right.Length);
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

    static PdfFontResource[] CopyResources(PdfFontResource[] values)
    {
        PdfFontResource[] copy = new PdfFontResource[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
