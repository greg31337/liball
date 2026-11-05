namespace ttf;

internal interface ITextShapingBarrier
{
    void Apply(ShapingBuffer buffer);
}

internal class TextShapingStagePlan
{
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    ITextShapingBarrier? barrier;

    internal TextShapingStagePlan(
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        ITextShapingBarrier? barrier)
    {
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.barrier = barrier;
    }

    internal DefaultShaperTableKind GetTableKind()
    {
        return tableKind;
    }

    internal SfntLayoutFeaturePlan GetFeaturePlan()
    {
        return featurePlan;
    }

    internal bool HasBarrier()
    {
        return barrier != null;
    }

    internal void ApplyBarrier(ShapingBuffer buffer)
    {
        if (barrier == null)
        {
            throw new System.InvalidOperationException(
                "This shaping stage does not have a barrier.");
        }

        barrier.Apply(buffer);
    }
}

internal class TextShapingPreparedPlan
{
    TextShapingPlanKind kind;
    int scriptTag;
    TextShapingStagePlan[] stages;

    internal TextShapingPreparedPlan(
        TextShapingPlanKind kind,
        int scriptTag,
        TextShapingStagePlan[] stages)
    {
        this.kind = kind;
        this.scriptTag = scriptTag;
        this.stages = stages;
    }

    internal TextShapingPlanKind GetKind()
    {
        return kind;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal TextShapingStagePlan GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

}

internal class TextShapingSegment
{
    int runeStart;
    int runeLength;
    int utf16Start;
    int utf16Length;
    int bidiLevel;
    int visualKey;
    string script;

    internal TextShapingSegment(
        int runeStart,
        int runeLength,
        int utf16Start,
        int utf16Length,
        int bidiLevel,
        int visualKey,
        string script)
    {
        this.runeStart = runeStart;
        this.runeLength = runeLength;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.bidiLevel = bidiLevel;
        this.visualKey = visualKey;
        this.script = script;
    }

    internal int GetRuneStart() { return runeStart; }
    internal int GetRuneLength() { return runeLength; }
    internal int GetUtf16Start() { return utf16Start; }
    internal int GetUtf16Length() { return utf16Length; }
    internal int GetBidiLevel() { return bidiLevel; }
    internal int GetVisualKey() { return visualKey; }
    internal string GetScript() { return script; }
}

internal class TextShapingResolvedSegment
{
    TextShapingSegment segment;
    FontMatch match;

    internal TextShapingResolvedSegment(
        TextShapingSegment segment,
        FontMatch match)
    {
        this.segment = segment;
        this.match = match;
    }

    internal TextShapingSegment GetSegment()
    {
        return segment;
    }

    internal FontMatch GetMatch()
    {
        return match;
    }
}

internal static class TextShaper
{
    const int DefaultScriptTag = 0x44464C54;

    internal static TextShapingResult Shape(
        string text,
        FontInstance fontInstance)
    {
        return Shape(text, fontInstance, TextShapingOptions.Create());
    }

    internal static TextShapingResult Shape(
        string text,
        FontInstance fontInstance,
        TextShapingOptions options)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (fontInstance == null)
        {
            throw new System.ArgumentNullException("fontInstance");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options = options.Copy();
        FontOperationOptions operation = options.GetOperationSettings();
        FontOperationContext.Checkpoint(operation);
        FontFace face = fontInstance.GetFace();
        FontLimits limits = FontOperationContext.CreateEffectiveLimits(
            face.GetTableCache().GetLimits(),
            operation);
        TextShapingLimits operationLimits = options.GetOperationLimits();
        ValidateSourceLength(text, operationLimits);
        int maximumRunes = Minimum(
            limits.GetMaxGlyphsPerFace(),
            operationLimits.GetMaximumRunes());
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        RuneBuffer source = Utf16Decoder.Decode(
            text,
            options.GetDecodingMode(),
            maximumRunes);
        RuneBuffer normalized = UnicodeNormalizer.Normalize(
            source,
            unicodeData,
            options.GetNormalizationMode(),
            maximumRunes);
        FontOperationContext.Checkpoint(operation);
        GraphemeClusterMap clusters = CreateClusters(
            normalized,
            unicodeData,
            maximumRunes,
            Minimum(maximumRunes, operationLimits.GetMaximumClusters()),
            options.GetClusterPolicy());
        ScriptRunMap scripts = ScriptItemizer.Itemize(
            normalized,
            clusters,
            unicodeData,
            maximumRunes,
            operationLimits.GetMaximumRuns());
        BidiResult bidi = BidiAlgorithm.Resolve(
            normalized,
            unicodeData,
            GetParagraphDirection(options),
            maximumRunes,
            operationLimits.GetMaximumRuns());
        TextShapingSegment[] segments = BuildSegments(
            normalized,
            scripts,
            bidi,
            options);
        ValidateRunCount(segments.Length, operationLimits);
        OpenTypeLanguageTagResolution language =
            OpenTypeLanguageTagMap.Resolve(options.GetLanguageTag());
        int languageTag = language.GetLanguageSystemTagCount() == 0 ?
            0 : language.GetLanguageSystemTag(0);
        System.Collections.Generic.List<TextShapingRun> shapedRuns =
            new System.Collections.Generic.List<TextShapingRun>();
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        System.Collections.Generic.List<FontFallbackDecision> fallbackDecisions =
            new System.Collections.Generic.List<FontFallbackDecision>();
        AddUnsupportedTechnologyDiagnostic(face, diagnostics, limits);
        AddUnsupportedTechnologyDecision(
            face,
            fontInstance,
            "",
            "",
            0,
            text.Length,
            fallbackDecisions);
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            FontOperationContext.Checkpoint(operation);
            TextShapingRun run = ShapeSegment(
                source,
                normalized,
                segments[segmentIndex],
                fontInstance,
                options,
                unicodeData,
                languageTag,
                diagnostics,
                limits);
            shapedRuns.Add(run);
            segmentIndex += 1;
        }

        TextShapingRun[] runArray = new TextShapingRun[shapedRuns.Count];
        shapedRuns.CopyTo(runArray, 0);
        FontDiagnostic[] diagnosticArray =
            new FontDiagnostic[diagnostics.Count];
        diagnostics.CopyTo(diagnosticArray, 0);
        FontOperationContext.RejectDiagnosticsInStrictMode(
            diagnosticArray,
            operation,
            "Strict shaping rejected a diagnostic finding.");
        TextShapingResult result = new TextShapingResult(
            runArray,
            diagnosticArray,
            source.GetCount(),
            normalized.GetCount(),
            clusters.GetClusterCount(),
            scripts.GetRunCount(),
            bidi.GetRunCount());
        result.SetFallbackReport(CreateFallbackReport(fallbackDecisions));
        result.SetOperationOptions(operation);
        return result;
    }

    internal static TextShapingResult Shape(
        string text,
        FontSet fontSet,
        FontRequest request)
    {
        return Shape(text, fontSet, request, TextShapingOptions.Create());
    }

    internal static TextShapingResult Shape(
        string text,
        FontSet fontSet,
        FontRequest request,
        TextShapingOptions options)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (fontSet == null)
        {
            throw new System.ArgumentNullException("fontSet");
        }

        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options = options.Copy();
        FontOperationOptions operation = options.GetOperationSettings();
        FontOperationContext.Checkpoint(operation);
        FontLimits retainedLimits = fontSet.GetFaceCount() == 0 ?
            FontLimits.CreateCompatible() :
            fontSet.GetFace(0).GetTableCache().GetLimits();
        FontLimits limits = FontOperationContext.CreateEffectiveLimits(
            retainedLimits,
            operation);
        TextShapingLimits operationLimits = options.GetOperationLimits();
        ValidateSourceLength(text, operationLimits);
        int maximumRunes = Minimum(
            limits.GetMaxGlyphsPerFace(),
            operationLimits.GetMaximumRunes());
        UnicodeData unicodeData = fontSet.GetUnicodeData();
        RuneBuffer source = Utf16Decoder.Decode(
            text,
            options.GetDecodingMode(),
            maximumRunes);
        RuneBuffer normalized = UnicodeNormalizer.Normalize(
            source,
            unicodeData,
            options.GetNormalizationMode(),
            maximumRunes);
        FontOperationContext.Checkpoint(operation);
        GraphemeClusterMap clusters = CreateClusters(
            normalized,
            unicodeData,
            maximumRunes,
            Minimum(maximumRunes, operationLimits.GetMaximumClusters()),
            options.GetClusterPolicy());
        ScriptRunMap scripts = ScriptItemizer.Itemize(
            normalized,
            clusters,
            unicodeData,
            maximumRunes,
            operationLimits.GetMaximumRuns());
        BidiResult bidi = BidiAlgorithm.Resolve(
            normalized,
            unicodeData,
            GetParagraphDirection(options),
            maximumRunes,
            operationLimits.GetMaximumRuns());
        TextShapingSegment[] segments = BuildSegments(
            normalized,
            scripts,
            bidi,
            options);
        ValidateRunCount(segments.Length, operationLimits);
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        System.Collections.Generic.List<FontFallbackDecision> fallbackDecisions =
            new System.Collections.Generic.List<FontFallbackDecision>();
        TextShapingResolvedSegment[] resolvedSegments =
            BuildResolvedSegments(
                normalized,
                clusters,
                segments,
                bidi,
                fontSet,
                request,
                unicodeData,
                diagnostics,
                fallbackDecisions,
                limits);
        ValidateRunCount(resolvedSegments.Length, operationLimits);
        OpenTypeLanguageTagResolution language =
            OpenTypeLanguageTagMap.Resolve(options.GetLanguageTag());
        int languageTag = language.GetLanguageSystemTagCount() == 0 ?
            0 : language.GetLanguageSystemTag(0);
        System.Collections.Generic.List<TextShapingRun> shapedRuns =
            new System.Collections.Generic.List<TextShapingRun>();
        System.Collections.Generic.List<FontFace> diagnosedFaces =
            new System.Collections.Generic.List<FontFace>();
        int segmentIndex = 0;
        while (segmentIndex < resolvedSegments.Length)
        {
            FontOperationContext.Checkpoint(operation);
            TextShapingResolvedSegment resolved =
                resolvedSegments[segmentIndex];
            FontInstance instance = resolved.GetMatch().GetInstance();
            FontFace face = instance.GetFace();
            AddSelectionDecision(
                resolved,
                request,
                fallbackDecisions);
            if (!ContainsFace(diagnosedFaces, face))
            {
                AddUnsupportedTechnologyDiagnostic(
                    face,
                    diagnostics,
                    limits);
                AddUnsupportedTechnologyDecision(
                    face,
                    instance,
                    resolved.GetSegment().GetScript(),
                    request.GetFamilyName(),
                    resolved.GetSegment().GetUtf16Start(),
                    resolved.GetSegment().GetUtf16Length(),
                    fallbackDecisions);
                diagnosedFaces.Add(face);
            }

            shapedRuns.Add(ShapeSegment(
                source,
                normalized,
                resolved.GetSegment(),
                instance,
                options,
                unicodeData,
                languageTag,
                diagnostics,
                limits));
            segmentIndex += 1;
        }

        TextShapingRun[] runArray = new TextShapingRun[shapedRuns.Count];
        shapedRuns.CopyTo(runArray, 0);
        FontDiagnostic[] diagnosticArray =
            new FontDiagnostic[diagnostics.Count];
        diagnostics.CopyTo(diagnosticArray, 0);
        FontOperationContext.RejectDiagnosticsInStrictMode(
            diagnosticArray,
            operation,
            "Strict shaping rejected a diagnostic finding.");
        TextShapingResult result = new TextShapingResult(
            runArray,
            diagnosticArray,
            source.GetCount(),
            normalized.GetCount(),
            clusters.GetClusterCount(),
            scripts.GetRunCount(),
            bidi.GetRunCount());
        result.SetFallbackReport(CreateFallbackReport(fallbackDecisions));
        result.SetOperationOptions(operation);
        return result;
    }

    static TextShapingResolvedSegment[] BuildResolvedSegments(
        RuneBuffer normalized,
        GraphemeClusterMap clusters,
        TextShapingSegment[] segments,
        BidiResult bidi,
        FontSet fontSet,
        FontRequest request,
        UnicodeData unicodeData,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        System.Collections.Generic.List<FontFallbackDecision> fallbackDecisions,
        FontLimits limits)
    {
        System.Collections.Generic.List<TextShapingResolvedSegment> resolved =
            new System.Collections.Generic.List<TextShapingResolvedSegment>();
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            TextShapingSegment segment = segments[segmentIndex];
            FontFallbackList fallbacks = fontSet.GetFallbacks(
                request,
                segment.GetScript());
            AddResolvedSegmentClusters(
                normalized,
                clusters,
                segment,
                bidi,
                fallbacks,
                request,
                unicodeData,
                diagnostics,
                fallbackDecisions,
                limits,
                resolved);
            segmentIndex += 1;
        }

        TextShapingResolvedSegment[] values =
            new TextShapingResolvedSegment[resolved.Count];
        resolved.CopyTo(values, 0);
        SortResolvedSegments(values);
        return values;
    }

    static void AddResolvedSegmentClusters(
        RuneBuffer normalized,
        GraphemeClusterMap clusters,
        TextShapingSegment segment,
        BidiResult bidi,
        FontFallbackList fallbacks,
        FontRequest request,
        UnicodeData unicodeData,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        System.Collections.Generic.List<FontFallbackDecision> fallbackDecisions,
        FontLimits limits,
        System.Collections.Generic.List<TextShapingResolvedSegment> resolved)
    {
        int segmentStart = segment.GetRuneStart();
        int segmentEnd = segmentStart + segment.GetRuneLength();
        int groupStart = -1;
        int groupEnd = -1;
        FontMatch? groupMatch = null;
        int clusterIndex = 0;
        while (clusterIndex < clusters.GetClusterCount())
        {
            int clusterStart = clusters.GetRuneStart(clusterIndex);
            int clusterEnd = clusters.GetRuneEnd(clusterIndex);
            if (clusterStart >= segmentStart && clusterEnd <= segmentEnd)
            {
                RuneBuffer clusterRunes = CopyRuneRange(
                    normalized,
                    clusterStart,
                    clusterEnd - clusterStart);
                FontMatch? match = FontClusterFallbackResolver.Resolve(
                    fallbacks,
                    clusterRunes,
                    unicodeData);

                if (match == null)
                {
                    AddResolvedGroup(
                        normalized,
                        bidi,
                        segment,
                        groupStart,
                        groupEnd,
                        groupMatch,
                        resolved);
                    groupStart = -1;
                    groupEnd = -1;
                    groupMatch = null;
                    AddUnavailableClusterDiagnostic(
                        normalized,
                        clusterStart,
                        clusters.GetUtf16Start(clusterIndex),
                        clusters.GetUtf16Length(clusterIndex),
                        diagnostics,
                        limits);
                    AddMissingGlyphDecision(
                        segment.GetScript(),
                        request.GetFamilyName(),
                        "",
                        clusters.GetUtf16Start(clusterIndex),
                        clusters.GetUtf16Length(clusterIndex),
                        normalized.GetScalarValue(clusterStart),
                        null,
                        fallbackDecisions);
                }
                else
                {
                    if (!FontClusterFallbackResolver.HasCompleteCoverage(
                        match,
                        clusterRunes,
                        unicodeData))
                    {
                        AddMissingGlyphDecision(
                            segment.GetScript(),
                            request.GetFamilyName(),
                            match.GetConfiguredFamilyName(),
                            clusters.GetUtf16Start(clusterIndex),
                            clusters.GetUtf16Length(clusterIndex),
                            FontClusterFallbackResolver.GetFirstMissingCodePoint(
                                match,
                                clusterRunes,
                                unicodeData),
                            match,
                            fallbackDecisions);
                    }
                    AddUnsupportedVariationDiagnostics(
                        match,
                        clusterRunes,
                        clusters.GetUtf16Start(clusterIndex),
                        clusters.GetUtf16Length(clusterIndex),
                        diagnostics,
                        limits);
                    if (groupMatch == null || !SameMatch(groupMatch, match))
                    {
                        AddResolvedGroup(
                            normalized,
                            bidi,
                            segment,
                            groupStart,
                            groupEnd,
                            groupMatch,
                            resolved);
                        groupStart = clusterStart;
                        groupEnd = clusterEnd;
                        groupMatch = match;
                    }
                    else
                    {
                        groupEnd = clusterEnd;
                    }
                }
            }

            clusterIndex += 1;
        }

        AddResolvedGroup(
            normalized,
            bidi,
            segment,
            groupStart,
            groupEnd,
            groupMatch,
            resolved);
    }

    static void AddResolvedGroup(
        RuneBuffer normalized,
        BidiResult bidi,
        TextShapingSegment parent,
        int start,
        int end,
        FontMatch? match,
        System.Collections.Generic.List<TextShapingResolvedSegment> resolved)
    {
        if (start < 0 || match == null)
        {
            return;
        }

        TextShapingSegment segment = CreateSegment(
            normalized,
            bidi,
            start,
            end - start,
            parent.GetBidiLevel(),
            parent.GetScript());
        resolved.Add(new TextShapingResolvedSegment(segment, match));
    }

    static void AddUnavailableClusterDiagnostic(
        RuneBuffer normalized,
        int runeStart,
        int utf16Start,
        int utf16Length,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        if (diagnostics.Count == limits.GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured diagnostic limit.");
        }

        int codePoint = normalized.GetScalarValue(runeStart);
        string code = codePoint.ToString(
            "X4",
            System.Globalization.CultureInfo.InvariantCulture);
        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MissingGlyph,
            "No configured font match is available for U+" + code + ".",
            -1,
            0x636D6170,
            -1,
            "Shaping.Fallback[" + utf16Start + ":" + utf16Length + "]"));
    }

    static void AddUnsupportedVariationDiagnostics(
        FontMatch match,
        RuneBuffer runes,
        int utf16Start,
        int utf16Length,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        SfntCmapMapping? mapping = match.GetFace().GetCmapMapping();
        if (mapping == null)
        {
            return;
        }

        int index = 0;
        while (index + 1 < runes.GetCount())
        {
            int codePoint = runes.GetScalarValue(index);
            int selector = runes.GetScalarValue(index + 1);
            bool variationSelector = IsVariationSelector(selector);
            if (variationSelector &&
                mapping.MapUnicode(codePoint) != 0 &&
                mapping.MapVariation(codePoint, selector) == 0)
            {
                if (diagnostics.Count == limits.GetMaxDiagnostics())
                {
                    throw FontLimitException.Create(
                        "Text shaping exceeds the configured diagnostic limit.");
                }

                string baseCode = codePoint.ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture);
                string selectorCode = selector.ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture);
                diagnostics.Add(new FontDiagnostic(
                    FontDiagnosticSeverity.Warning,
                    FontDiagnosticCode.MissingGlyph,
                    "No glyph mapping is available for variation sequence U+" +
                    baseCode + " U+" + selectorCode + ".",
                    match.GetFace().GetFaceIndex(),
                    0x636D6170,
                    -1,
                    "Shaping.Fallback[" + utf16Start + ":" +
                    utf16Length + "]"));
                index += 2;
            }
            else
            {
                index += 1;
            }
        }
    }

    static bool IsVariationSelector(int codePoint)
    {
        return (codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||
            (codePoint >= 0xE0100 && codePoint <= 0xE01EF);
    }

    static bool SameMatch(FontMatch first, FontMatch second)
    {
        return object.ReferenceEquals(first.GetFace(), second.GetFace()) &&
            string.Equals(
                first.GetCoordinates().GetKey(),
                second.GetCoordinates().GetKey(),
                System.StringComparison.Ordinal);
    }

    static bool ContainsFace(
        System.Collections.Generic.List<FontFace> faces,
        FontFace candidate)
    {
        int index = 0;
        while (index < faces.Count)
        {
            if (object.ReferenceEquals(faces[index], candidate))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void AddSelectionDecision(
        TextShapingResolvedSegment resolved,
        FontRequest request,
        System.Collections.Generic.List<FontFallbackDecision> decisions)
    {
        FontMatch match = resolved.GetMatch();
        FontFallbackMatchKind matchKind = match.GetFallbackMatchKind();
        FontFallbackDecisionKind kind;
        if (matchKind == FontFallbackMatchKind.GenericFamily)
        {
            kind = FontFallbackDecisionKind.GenericFamilySubstitution;
        }
        else if (matchKind == FontFallbackMatchKind.ScriptFallback)
        {
            kind = FontFallbackDecisionKind.ScriptFallback;
        }
        else if (matchKind == FontFallbackMatchKind.FallbackFamily)
        {
            kind = FontFallbackDecisionKind.FamilySubstitution;
        }
        else
        {
            return;
        }

        TextShapingSegment segment = resolved.GetSegment();
        decisions.Add(new FontFallbackDecision(
            kind,
            segment.GetUtf16Start(),
            segment.GetUtf16Length(),
            segment.GetScript(),
            request.GetFamilyName(),
            match.GetConfiguredFamilyName(),
            match.GetFace(),
            match.GetInstance(),
            -1,
            0));
    }

    static void AddMissingGlyphDecision(
        string script,
        string requestedFamilyName,
        string resolvedFamilyName,
        int utf16Start,
        int utf16Length,
        int codePoint,
        FontMatch? match,
        System.Collections.Generic.List<FontFallbackDecision> decisions)
    {
        FontFace? face = match == null ? null : match.GetFace();
        FontInstance? instance = match == null ? null : match.GetInstance();
        decisions.Add(new FontFallbackDecision(
            FontFallbackDecisionKind.MissingGlyph,
            utf16Start,
            utf16Length,
            script,
            requestedFamilyName,
            resolvedFamilyName,
            face,
            instance,
            codePoint,
            0));
        if (match != null)
        {
            decisions.Add(new FontFallbackDecision(
                FontFallbackDecisionKind.FinalMissingGlyphFace,
                utf16Start,
                utf16Length,
                script,
                requestedFamilyName,
                resolvedFamilyName,
                face,
                instance,
                codePoint,
                0));
        }
    }

    static void AddUnsupportedTechnologyDecision(
        FontFace face,
        FontInstance instance,
        string script,
        string requestedFamilyName,
        int utf16Start,
        int utf16Length,
        System.Collections.Generic.List<FontFallbackDecision> decisions)
    {
        int tag = FindUnsupportedTechnologyTag(face);
        if (tag == 0)
        {
            return;
        }

        decisions.Add(new FontFallbackDecision(
            FontFallbackDecisionKind.UnsupportedFontTechnology,
            utf16Start,
            utf16Length,
            script,
            requestedFamilyName,
            "",
            face,
            instance,
            -1,
            tag));
    }

    static FontFallbackReport CreateFallbackReport(
        System.Collections.Generic.List<FontFallbackDecision> decisions)
    {
        FontFallbackDecision[] values =
            new FontFallbackDecision[decisions.Count];
        decisions.CopyTo(values, 0);
        int index = 1;
        while (index < values.Length)
        {
            FontFallbackDecision value = values[index];
            int target = index;
            while (target > 0 && IsDecisionAfter(values[target - 1], value))
            {
                values[target] = values[target - 1];
                target -= 1;
            }
            values[target] = value;
            index += 1;
        }
        return new FontFallbackReport(values);
    }

    static bool IsDecisionAfter(
        FontFallbackDecision first,
        FontFallbackDecision second)
    {
        if (first.GetUtf16Start() != second.GetUtf16Start())
        {
            return first.GetUtf16Start() > second.GetUtf16Start();
        }
        return first.GetKind() > second.GetKind();
    }

    static void SortResolvedSegments(TextShapingResolvedSegment[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            TextShapingResolvedSegment value = values[index];
            int target = index;
            while (target > 0 &&
                values[target - 1].GetSegment().GetVisualKey() >
                value.GetSegment().GetVisualKey())
            {
                values[target] = values[target - 1];
                target -= 1;
            }

            values[target] = value;
            index += 1;
        }
    }

    static TextShapingRun ShapeSegment(
        RuneBuffer source,
        RuneBuffer normalized,
        TextShapingSegment segment,
        FontInstance fontInstance,
        TextShapingOptions options,
        UnicodeData unicodeData,
        int languageTag,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        RuneBuffer runInput = CopyRuneRange(
            normalized,
            segment.GetRuneStart(),
            segment.GetRuneLength());
        RuneBuffer sourceView = CopySourceView(
            source,
            segment.GetUtf16Start(),
            segment.GetUtf16Start() + segment.GetUtf16Length());
        ShapingDirection direction = GetRunDirection(options, segment);
        ShapingBuffer buffer = new ShapingBuffer(runInput, direction);
        int maximumGlyphs = Minimum(
            limits.GetMaxGlyphsPerFace(),
            options.GetOperationLimits().GetMaximumGlyphs());
        EmojiShaper.Prepare(buffer, unicodeData, maximumGlyphs);
        TextShapingPreparedPlan plan = TextShapingPlanFactory.Prepare(
            buffer,
            fontInstance.GetFace(),
            segment.GetScript(),
            options.CopyFeatures(),
            unicodeData,
            maximumGlyphs);
        ShapingMappingResult mapping = NominalGlyphMapper.Map(
            buffer,
            fontInstance.GetFace().GetCmapMapping(),
            unicodeData,
            RemainingDiagnostics(diagnostics, limits));
        AddMappingDiagnostics(mapping, diagnostics, limits);
        SfntGposPositionBuffer positions = TextShapingLayoutEngine.Apply(
            buffer,
            plan,
            fontInstance,
            languageTag,
            options);
        RemoveUnmappedDefaultIgnorables(buffer, positions);
        ShapedRun shapedRun = ShapedRunBuilder.Build(
            buffer,
            sourceView,
            positions);
        return new TextShapingRun(
            shapedRun,
            fontInstance,
            segment.GetScript(),
            plan.GetScriptTag(),
            languageTag,
            segment.GetBidiLevel(),
            segment.GetUtf16Start(),
            segment.GetUtf16Length(),
            plan.GetKind(),
            plan.GetStageCount());
    }

    static void RemoveUnmappedDefaultIgnorables(
        ShapingBuffer buffer,
        SfntGposPositionBuffer positions)
    {
        bool[] removed = new bool[buffer.GetCount()];
        int removedCount = 0;
        int index = 0;
        while (index < buffer.GetCount())
        {
            removed[index] = buffer.IsDefaultIgnorable(index) &&
                buffer.GetGlyphId(index) == 0;
            if (removed[index])
            {
                removedCount += 1;
            }

            index += 1;
        }

        if (removedCount == 0)
        {
            return;
        }

        SfntLayoutGlyphBuffer layout = positions.GetGlyphBuffer();
        int[] keptGlyphIds = new int[buffer.GetCount() - removedCount];
        int[] starts = new int[keptGlyphIds.Length];
        int[] lengths = new int[keptGlyphIds.Length];
        int[] componentCounts = new int[keptGlyphIds.Length];
        int[] componentIndexes = new int[keptGlyphIds.Length];
        int oldIndex = 0;
        int newIndex = 0;
        while (oldIndex < buffer.GetCount())
        {
            if (!removed[oldIndex])
            {
                keptGlyphIds[newIndex] = layout.GetGlyphId(oldIndex);
                starts[newIndex] = layout.GetClusterStart(oldIndex);
                lengths[newIndex] = layout.GetClusterLength(oldIndex);
                componentCounts[newIndex] =
                    layout.GetLigatureComponentCount(oldIndex);
                componentIndexes[newIndex] =
                    layout.GetLigatureComponentIndex(oldIndex);
                newIndex += 1;
            }

            oldIndex += 1;
        }

        buffer.RemoveItems(removed);
        SfntLayoutGlyphBuffer keptLayout = SfntLayoutGlyphBuffer.Create(
            keptGlyphIds,
            starts,
            lengths,
            componentCounts,
            componentIndexes);
        SfntGposPositionBuffer keptPositions =
            SfntGposPositionBuffer.Create(keptLayout);
        newIndex = 0;
        oldIndex = 0;
        while (oldIndex < removed.Length)
        {
            if (!removed[oldIndex])
            {
                keptPositions.ApplyAdjustment(
                    newIndex,
                    new SfntGposAdjustment(
                        positions.GetXPlacement(oldIndex),
                        positions.GetYPlacement(oldIndex),
                        positions.GetXAdvance(oldIndex),
                        positions.GetYAdvance(oldIndex)));
                newIndex += 1;
            }

            oldIndex += 1;
        }

        positions.ReplaceWith(keptPositions);
    }

    static int RemainingDiagnostics(
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        return limits.GetMaxDiagnostics() - diagnostics.Count;
    }

    static void AddMappingDiagnostics(
        ShapingMappingResult mapping,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        int index = 0;
        while (index < mapping.GetDiagnosticCount())
        {
            if (diagnostics.Count == limits.GetMaxDiagnostics())
            {
                throw FontLimitException.Create(
                    "Text shaping exceeds the configured diagnostic limit.");
            }

            diagnostics.Add(mapping.GetDiagnostic(index));
            index += 1;
        }
    }

    static TextShapingSegment[] BuildSegments(
        RuneBuffer input,
        ScriptRunMap scripts,
        BidiResult bidi,
        TextShapingOptions options)
    {
        if (input.GetCount() == 0)
        {
            return new TextShapingSegment[0];
        }

        System.Collections.Generic.List<TextShapingSegment> values =
            new System.Collections.Generic.List<TextShapingSegment>();
        int start = 0;
        string script = options.HasScript() ?
            options.GetScript() : FindScript(scripts, 0);
        int level = EffectiveLevel(bidi, options, 0);
        int index = 1;
        while (index <= input.GetCount())
        {
            string nextScript = index < input.GetCount() ?
                (options.HasScript() ? options.GetScript() :
                    FindScript(scripts, index)) : "";
            int nextLevel = index < input.GetCount() ?
                EffectiveLevel(bidi, options, index) : -1;
            if (index == input.GetCount() ||
                nextScript != script || nextLevel != level)
            {
                values.Add(CreateSegment(
                    input,
                    bidi,
                    start,
                    index - start,
                    level,
                    script));
                start = index;
                script = nextScript;
                level = nextLevel;
            }

            index += 1;
        }

        TextShapingSegment[] segments =
            new TextShapingSegment[values.Count];
        values.CopyTo(segments, 0);
        SortSegments(segments);
        return segments;
    }

    static TextShapingSegment CreateSegment(
        RuneBuffer input,
        BidiResult bidi,
        int start,
        int length,
        int level,
        string script)
    {
        int utf16Start = input.GetUtf16Start(start);
        int utf16End = input.GetUtf16End(start);
        int visualKey = int.MaxValue;
        int index = start;
        while (index < start + length)
        {
            if (input.GetUtf16Start(index) < utf16Start)
            {
                utf16Start = input.GetUtf16Start(index);
            }

            if (input.GetUtf16End(index) > utf16End)
            {
                utf16End = input.GetUtf16End(index);
            }

            int visualIndex = bidi.GetVisualIndex(index);
            if (visualIndex >= 0 && visualIndex < visualKey)
            {
                visualKey = visualIndex;
            }

            index += 1;
        }

        if (visualKey == int.MaxValue)
        {
            visualKey = start;
        }

        return new TextShapingSegment(
            start,
            length,
            utf16Start,
            utf16End - utf16Start,
            level,
            visualKey,
            script);
    }

    static void SortSegments(TextShapingSegment[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            TextShapingSegment value = values[index];
            int target = index;
            while (target > 0 &&
                values[target - 1].GetVisualKey() > value.GetVisualKey())
            {
                values[target] = values[target - 1];
                target -= 1;
            }

            values[target] = value;
            index += 1;
        }
    }

    static string FindScript(ScriptRunMap scripts, int runeIndex)
    {
        int runIndex = 0;
        while (runIndex < scripts.GetRunCount())
        {
            if (runeIndex >= scripts.GetRuneStart(runIndex) &&
                runeIndex < scripts.GetRuneEnd(runIndex))
            {
                return scripts.GetScript(runIndex);
            }

            runIndex += 1;
        }

        throw new System.InvalidOperationException(
            "A rune does not belong to a script run.");
    }

    static int EffectiveLevel(
        BidiResult bidi,
        TextShapingOptions options,
        int runeIndex)
    {
        if (options.GetDirection() == TextShapingDirection.LeftToRight)
        {
            return 0;
        }

        if (options.GetDirection() == TextShapingDirection.RightToLeft)
        {
            return 1;
        }

        return bidi.GetLevel(runeIndex);
    }

    static ShapingDirection GetRunDirection(
        TextShapingOptions options,
        TextShapingSegment segment)
    {
        TextShapingDirection direction = options.GetDirection();
        if (direction == TextShapingDirection.LeftToRight)
        {
            return ShapingDirection.LeftToRight;
        }

        if (direction == TextShapingDirection.RightToLeft)
        {
            return ShapingDirection.RightToLeft;
        }

        if (direction == TextShapingDirection.TopToBottom)
        {
            return ShapingDirection.TopToBottom;
        }

        if (direction == TextShapingDirection.BottomToTop)
        {
            return ShapingDirection.BottomToTop;
        }

        return (segment.GetBidiLevel() & 1) == 0 ?
            ShapingDirection.LeftToRight : ShapingDirection.RightToLeft;
    }

    static BidiParagraphDirection GetParagraphDirection(
        TextShapingOptions options)
    {
        if (options.GetDirection() == TextShapingDirection.LeftToRight)
        {
            return BidiParagraphDirection.LeftToRight;
        }

        if (options.GetDirection() == TextShapingDirection.RightToLeft)
        {
            return BidiParagraphDirection.RightToLeft;
        }

        return options.GetParagraphDirection();
    }

    static RuneBuffer CopyRuneRange(
        RuneBuffer input,
        int start,
        int length)
    {
        System.Text.Rune[] runes = new System.Text.Rune[length];
        int[] starts = new int[length];
        int[] lengths = new int[length];
        int index = 0;
        while (index < length)
        {
            runes[index] = input.GetRune(start + index);
            starts[index] = input.GetUtf16Start(start + index);
            lengths[index] = input.GetUtf16Length(start + index);
            index += 1;
        }

        return new RuneBuffer(
            runes,
            starts,
            lengths,
            input.GetSourceLength(),
            input.GetReplacementCount());
    }

    static RuneBuffer CopySourceView(
        RuneBuffer source,
        int start,
        int end)
    {
        int count = 0;
        int index = 0;
        while (index < source.GetCount())
        {
            if (source.GetUtf16Start(index) < end &&
                start < source.GetUtf16End(index))
            {
                count += 1;
            }

            index += 1;
        }

        System.Text.Rune[] runes = new System.Text.Rune[count];
        int[] starts = new int[count];
        int[] lengths = new int[count];
        index = 0;
        int target = 0;
        while (index < source.GetCount())
        {
            if (source.GetUtf16Start(index) < end &&
                start < source.GetUtf16End(index))
            {
                runes[target] = source.GetRune(index);
                starts[target] = source.GetUtf16Start(index);
                lengths[target] = source.GetUtf16Length(index);
                target += 1;
            }

            index += 1;
        }

        return new RuneBuffer(
            runes,
            starts,
            lengths,
            source.GetSourceLength(),
            source.GetReplacementCount());
    }

    static GraphemeClusterMap CreateClusters(
        RuneBuffer input,
        UnicodeData unicodeData,
        int maximumRunes,
        int maximumClusters,
        TextShapingClusterPolicy policy)
    {
        if (policy == TextShapingClusterPolicy.GraphemeClusters)
        {
            return GraphemeSegmenter.Segment(
                input,
                unicodeData,
                maximumRunes,
                maximumClusters);
        }

        int count = input.GetCount();
        if (count > maximumRunes)
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured rune limit.");
        }
        if (count > maximumClusters)
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured cluster limit.");
        }
        int[] runeStarts = new int[count];
        int[] runeLengths = new int[count];
        int[] utf16Starts = new int[count];
        int[] utf16Lengths = new int[count];
        int index = 0;
        while (index < count)
        {
            runeStarts[index] = index;
            runeLengths[index] = 1;
            utf16Starts[index] = input.GetUtf16Start(index);
            utf16Lengths[index] = input.GetUtf16Length(index);
            index += 1;
        }
        return new GraphemeClusterMap(
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            input.GetSourceLength());
    }

    static void ValidateSourceLength(
        string text,
        TextShapingLimits limits)
    {
        if (text.Length > limits.GetMaximumUtf16CodeUnits())
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured UTF-16 length limit.");
        }
    }

    static void ValidateRunCount(int count, TextShapingLimits limits)
    {
        if (count > limits.GetMaximumRuns())
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured run limit.");
        }
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }

    static void AddUnsupportedTechnologyDiagnostic(
        FontFace face,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        int tag = FindUnsupportedTechnologyTag(face);
        if (tag == 0)
        {
            return;
        }

        if (diagnostics.Count == limits.GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "Text shaping exceeds the configured diagnostic limit.");
        }

        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.UnsupportedShapingTechnology,
            "The font contains shaping technology that is not executed.",
            face.GetFaceIndex(),
            tag,
            -1,
            "Shaping.Technology"));
    }

    static int FindUnsupportedTechnologyTag(FontFace face)
    {
        int[] tags = new int[]
        {
            0x616E6B72,
            0x62736C6E,
            0x66656174,
            0x6A757374,
            0x6B657278,
            0x6C636172,
            0x6D6F7278,
            0x6D6F7274,
            0x6F706264,
            0x70726F70,
            0x7472616B,
            0x46656174,
            0x53696C66,
            0x476C6174,
            0x476C6F63,
            0x53696C6C
        };
        int index = 0;
        while (index < tags.Length)
        {
            if (face.HasTable(tags[index]))
            {
                return tags[index];
            }

            index += 1;
        }

        return 0;
    }
}
