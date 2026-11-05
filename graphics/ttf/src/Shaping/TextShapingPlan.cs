namespace ttf;

internal class IndicInitialReorderingBarrier : ITextShapingBarrier
{
    IndicShaperPreparation preparation;

    internal IndicInitialReorderingBarrier(IndicShaperPreparation preparation)
    {
        this.preparation = preparation;
    }

    /// <summary>Applies the prepared Indic initial reordering.</summary>
    /// <param name="buffer">The shaping buffer to reorder.</param>
    public void Apply(ShapingBuffer buffer)
    {
        preparation.ApplyInitialReordering(buffer);
    }
}

internal class IndicFinalReorderingBarrier : ITextShapingBarrier
{
    IndicShaperPreparation preparation;

    internal IndicFinalReorderingBarrier(IndicShaperPreparation preparation)
    {
        this.preparation = preparation;
    }

    /// <summary>Applies the prepared Indic final reordering.</summary>
    /// <param name="buffer">The shaping buffer to reorder.</param>
    public void Apply(ShapingBuffer buffer)
    {
        preparation.ApplyFinalReordering(buffer);
    }
}

internal static class TextShapingPlanFactory
{
    const int DefaultScriptTag = 0x44464C54;

    internal static TextShapingPreparedPlan Prepare(
        ShapingBuffer buffer,
        FontFace face,
        string script,
        SfntUserFeature[] features,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        if (script == "Arab" || script == "Syrc" || script == "Mong")
        {
            return PrepareArabic(buffer, script, features, unicodeData);
        }

        if (script == "Hebr")
        {
            return PrepareHebrew(buffer, face, features, unicodeData);
        }

        IndicShapingScript indicScript = GetIndicScript(script);
        if (indicScript >= IndicShapingScript.Devanagari)
        {
            return PrepareIndic(
                buffer,
                face,
                indicScript,
                features,
                unicodeData,
                maximumSyllableCount);
        }

        if (script == "Khmr")
        {
            return PrepareKhmer(
                buffer,
                features,
                unicodeData,
                maximumSyllableCount);
        }

        if (script == "Mymr")
        {
            return PrepareMyanmar(
                buffer,
                features,
                unicodeData,
                maximumSyllableCount);
        }

        if (script == "Thai" || script == "Laoo")
        {
            return PrepareThaiLao(buffer, face, script, features);
        }

        if (script == "Hang")
        {
            return PrepareHangul(buffer, face, features);
        }

        if (IsUniversalScript(script))
        {
            return PrepareUniversal(
                buffer,
                script,
                features,
                unicodeData,
                maximumSyllableCount);
        }

        return PrepareDefault(buffer, script, features);
    }

    static TextShapingPreparedPlan PrepareDefault(
        ShapingBuffer buffer,
        string script,
        SfntUserFeature[] features)
    {
        DefaultShaperPlan plan = DefaultShaper.BuildPlan(buffer, features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            DefaultShaperFeatureStage stage = plan.GetStage(index);
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                null);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Default,
            ScriptTag(script),
            stages);
    }

    static TextShapingPreparedPlan PrepareArabic(
        ShapingBuffer buffer,
        string script,
        SfntUserFeature[] features,
        UnicodeData unicodeData)
    {
        ArabicShapingScript shapingScript = script == "Syrc" ?
            ArabicShapingScript.Syriac : script == "Mong" ?
            ArabicShapingScript.Mongolian : ArabicShapingScript.Arabic;
        ArabicShaperPlan plan = ArabicShaper.BuildPlan(
            buffer,
            unicodeData,
            shapingScript,
            features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            ArabicShaperFeatureStage stage = plan.GetStage(index);
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                null);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Arabic,
            ScriptTag(script),
            stages);
    }

    static TextShapingPreparedPlan PrepareHebrew(
        ShapingBuffer buffer,
        FontFace face,
        SfntUserFeature[] features,
        UnicodeData unicodeData)
    {
        HebrewShaper.Prepare(
            buffer,
            unicodeData,
            face.GetGposTable() != null);
        HebrewShaperPlan plan = HebrewShaper.BuildPlan(buffer, features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            HebrewShaperFeatureStage stage = plan.GetStage(index);
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                null);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Hebrew,
            ScriptTag("Hebr"),
            stages);
    }

    static TextShapingPreparedPlan PrepareIndic(
        ShapingBuffer buffer,
        FontFace face,
        IndicShapingScript script,
        SfntUserFeature[] features,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        SfntGsubTable? gsub = face.GetGsubTable();
        IndicShapingModel model = gsub == null ?
            IndicShapingModel.Version2 : IndicShaper.SelectModel(
                gsub.GetLayoutTable(), script);
        IndicShaperPreparation preparation = IndicShaper.Prepare(
            buffer,
            unicodeData,
            script,
            model,
            maximumSyllableCount);
        IndicShaperPlan plan = IndicShaper.BuildPlan(preparation, features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            IndicShaperFeatureStage stage = plan.GetStage(index);
            ITextShapingBarrier? barrier = null;
            if (stage.GetKind() == IndicShaperStageKind.InitialReordering)
            {
                barrier = new IndicInitialReorderingBarrier(preparation);
            }
            else if (stage.GetKind() ==
                IndicShaperStageKind.FinalReordering)
            {
                barrier = new IndicFinalReorderingBarrier(preparation);
            }

            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                barrier);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Indic,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingPreparedPlan PrepareKhmer(
        ShapingBuffer buffer,
        SfntUserFeature[] features,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        KhmerShaperPreparation preparation = KhmerShaper.Prepare(
            buffer,
            unicodeData,
            KhmerDottedCirclePolicy.InsertForBrokenClusters,
            maximumSyllableCount);
        KhmerShaperPlan plan = KhmerShaper.BuildPlan(preparation, features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            KhmerShaperFeatureStage stage = plan.GetStage(index);
            ITextShapingBarrier? barrier = stage.GetKind() ==
                KhmerShaperStageKind.Reordering ?
                preparation : null;
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                barrier);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Khmer,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingPreparedPlan PrepareMyanmar(
        ShapingBuffer buffer,
        SfntUserFeature[] features,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        MyanmarShaperPreparation preparation = MyanmarShaper.Prepare(
            buffer,
            unicodeData,
            MyanmarDottedCirclePolicy.InsertForBrokenClusters,
            maximumSyllableCount);
        MyanmarShaperPlan plan = MyanmarShaper.BuildPlan(
            preparation,
            features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            MyanmarShaperFeatureStage stage = plan.GetStage(index);
            ITextShapingBarrier? barrier = stage.GetKind() ==
                MyanmarShaperStageKind.Reordering ?
                preparation : null;
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                barrier);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Myanmar,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingPreparedPlan PrepareThaiLao(
        ShapingBuffer buffer,
        FontFace face,
        string script,
        SfntUserFeature[] features)
    {
        ThaiLaoScript shapingScript = script == "Thai" ?
            ThaiLaoScript.Thai : ThaiLaoScript.Lao;
        int scriptTag = ThaiLaoShaper.GetScriptTag(shapingScript);
        bool hasScriptSubstitution = HasLayoutScript(
            face.GetGsubTable(),
            scriptTag);
        ThaiLaoShaperPreparation preparation = ThaiLaoShaper.Prepare(
            buffer,
            shapingScript,
            ThaiLegacyPuaPolicy.Disabled,
            hasScriptSubstitution,
            new ThaiLegacyPuaCoverage(new int[0]));
        preparation.ApplyReordering(buffer);
        ThaiLaoShaperPlan plan = ThaiLaoShaper.BuildPlan(
            preparation,
            features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            ThaiLaoShaperFeatureStage stage = plan.GetStage(index);
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                null);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.ThaiLao,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingPreparedPlan PrepareHangul(
        ShapingBuffer buffer,
        FontFace face,
        SfntUserFeature[] features)
    {
        HangulGlyphCoverage coverage = BuildHangulCoverage(
            face.GetCmapMapping());
        HangulShaperPreparation preparation = HangulShaper.Prepare(
            buffer,
            coverage);
        HangulShaperPlan plan = HangulShaper.BuildPlan(
            preparation,
            features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            HangulShaperFeatureStage stage = plan.GetStage(index);
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                null);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Hangul,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingPreparedPlan PrepareUniversal(
        ShapingBuffer buffer,
        string script,
        SfntUserFeature[] features,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        int scriptTag = ScriptTag(script);
        UniversalShaperPreparation preparation = UniversalShaper.Prepare(
            buffer,
            unicodeData,
            maximumSyllableCount);
        UniversalShaperPlan plan = UniversalShaper.BuildPlan(
            preparation,
            scriptTag,
            features);
        TextShapingStagePlan[] stages =
            new TextShapingStagePlan[plan.GetStageCount()];
        int index = 0;
        while (index < stages.Length)
        {
            UniversalShaperFeatureStage stage = plan.GetStage(index);
            ITextShapingBarrier? barrier = stage.GetKind() ==
                UniversalShaperStageKind.Reordering ?
                preparation : null;
            stages[index] = Stage(
                stage.GetTableKind(),
                stage.GetFeaturePlan(),
                barrier);
            index += 1;
        }

        return new TextShapingPreparedPlan(
            TextShapingPlanKind.Universal,
            plan.GetScriptTag(),
            stages);
    }

    static TextShapingStagePlan Stage(
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        ITextShapingBarrier? barrier)
    {
        return new TextShapingStagePlan(tableKind, featurePlan, barrier);
    }

    static bool HasLayoutScript(SfntGsubTable? table, int scriptTag)
    {
        return table != null && table.GetLayoutTable().GetScriptList()
            .FindScript(scriptTag) != null;
    }

    static HangulGlyphCoverage BuildHangulCoverage(
        SfntCmapMapping? mapping)
    {
        System.Collections.Generic.List<int> supported =
            new System.Collections.Generic.List<int>();
        AddMappedRange(mapping, supported, 0x1100, 0x11FF);
        AddMappedRange(mapping, supported, 0x302E, 0x302F);
        AddMappedRange(mapping, supported, 0xA960, 0xA97F);
        AddMappedRange(mapping, supported, 0xAC00, 0xD7FF);
        int[] values = new int[supported.Count];
        supported.CopyTo(values, 0);
        return new HangulGlyphCoverage(values, new int[0]);
    }

    static void AddMappedRange(
        SfntCmapMapping? mapping,
        System.Collections.Generic.List<int> values,
        int start,
        int end)
    {
        int codePoint = start;
        while (codePoint <= end)
        {
            System.ArgumentNullException.ThrowIfNull(mapping);
            if (mapping.MapUnicode(codePoint) != 0)
            {
                values.Add(codePoint);
            }

            codePoint += 1;
        }
    }

    static IndicShapingScript GetIndicScript(string script)
    {
        if (script == "Deva") return IndicShapingScript.Devanagari;
        if (script == "Beng") return IndicShapingScript.Bengali;
        if (script == "Guru") return IndicShapingScript.Gurmukhi;
        if (script == "Gujr") return IndicShapingScript.Gujarati;
        if (script == "Orya") return IndicShapingScript.Oriya;
        if (script == "Taml") return IndicShapingScript.Tamil;
        if (script == "Telu") return IndicShapingScript.Telugu;
        if (script == "Knda") return IndicShapingScript.Kannada;
        if (script == "Mlym") return IndicShapingScript.Malayalam;
        return (IndicShapingScript)(-1);
    }

    static bool IsUniversalScript(string script)
    {
        string[] scripts = new string[]
        {
            "Bali", "Batk", "Brah", "Bugi", "Buhd", "Cakm",
            "Cham", "Dupl", "Gran", "Java", "Kali", "Khar",
            "Khoj", "Lepc", "Limb", "Mahj", "Maka", "Mand",
            "Mani", "Marc", "Modi", "Nand", "Newa", "Phag",
            "Rohg", "Saur", "Shrd", "Sidd", "Sind", "Sinh",
            "Soyo", "Sund", "Sylo", "Tagb", "Takr", "Tibt",
            "Tirh", "Zanb"
        };
        int index = 0;
        while (index < scripts.Length)
        {
            if (scripts[index] == script)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static int ScriptTag(string script)
    {
        if (script == null || script.Length != 4 ||
            script == "Zyyy" || script == "Zinh" || script == "Zzzz")
        {
            return DefaultScriptTag;
        }

        char first = char.ToLowerInvariant(script[0]);
        char second = char.ToLowerInvariant(script[1]);
        char third = char.ToLowerInvariant(script[2]);
        char fourth = char.ToLowerInvariant(script[3]);
        return first << 24 | second << 16 | third << 8 | fourth;
    }
}

internal static class TextShapingLayoutEngine
{
    internal static SfntGposPositionBuffer Apply(
        ShapingBuffer buffer,
        TextShapingPreparedPlan plan,
        FontInstance fontInstance,
        int languageTag,
        TextShapingOptions options)
    {
        FontFace face = fontInstance.GetFace();
        SfntGsubTable? gsub = face.GetGsubTable();
        SfntGposTable? gpos = face.GetGposTable();
        SfntGdefTable? gdef = face.GetGdefTable();
        SfntLayoutGlyphBuffer layout = CreateLayoutBuffer(buffer);
        TextShapingLimits operationLimits = options.GetOperationLimits();
        SfntLayoutExecutionContext? gsubContext = gsub == null ? null :
            SfntLayoutExecutionContext.CreateForShaping(
                FontOperationContext.CreateEffectiveLimits(
                    gsub.GetLimits(),
                    options.GetOperationSettings()),
                operationLimits.GetMaximumGlyphs(),
                operationLimits.GetMaximumLayoutOperations(),
                options.GetOperationSettings().GetCancellationToken());
        int stageIndex = 0;
        while (stageIndex < plan.GetStageCount())
        {
            TextShapingStagePlan stage = plan.GetStage(stageIndex);
            if (stage.HasBarrier())
            {
                buffer.ReplaceLayoutGlyphs(layout);
                stage.ApplyBarrier(buffer);
                layout = CreateLayoutBuffer(buffer);
            }
            else if (stage.GetTableKind() ==
                DefaultShaperTableKind.Substitution && gsub != null)
            {
                SfntLayoutSelection selection = CreateSelection(
                    stage.GetFeaturePlan(),
                    gsub.GetLayoutTable(),
                    plan.GetScriptTag(),
                    languageTag);
                ApplyGsubSelection(
                    gsub,
                    selection,
                    layout,
                    gdef,
                    GetTraversalDirection(buffer),
                    gsubContext);
                buffer.ReplaceLayoutGlyphs(layout);
            }

            stageIndex += 1;
        }

        buffer.ReplaceLayoutGlyphs(layout);
        SfntGposPositionBuffer positions =
            SfntGposPositionBuffer.Create(layout);
        InitializeAdvances(positions, buffer, fontInstance);

        if (gpos != null)
        {
            SfntLayoutExecutionContext gposContext =
                SfntLayoutExecutionContext.CreateForShaping(
                    FontOperationContext.CreateEffectiveLimits(
                        gpos.GetLimits(),
                        options.GetOperationSettings()),
                    operationLimits.GetMaximumGlyphs(),
                    operationLimits.GetMaximumLayoutOperations(),
                    options.GetOperationSettings().GetCancellationToken());
            SfntGposAdjustmentContext adjustmentContext =
                CreateAdjustmentContext(fontInstance, gdef, options);
            stageIndex = 0;
            while (stageIndex < plan.GetStageCount())
            {
                TextShapingStagePlan stage = plan.GetStage(stageIndex);
                if (!stage.HasBarrier() &&
                    stage.GetTableKind() ==
                    DefaultShaperTableKind.Positioning)
                {
                    SfntLayoutSelection selection = CreateSelection(
                        stage.GetFeaturePlan(),
                        gpos.GetLayoutTable(),
                        plan.GetScriptTag(),
                        languageTag);
                    ApplyGposSelection(
                        gpos,
                        selection,
                        positions,
                        gdef,
                        GetTraversalDirection(buffer),
                        adjustmentContext,
                        gposContext);
                }

                stageIndex += 1;
            }

            positions.ResolveAttachments(GetTraversalDirection(buffer));
            CopyAttachments(buffer, positions);
        }

        return positions;
    }

    static void CopyAttachments(
        ShapingBuffer buffer,
        SfntGposPositionBuffer positions)
    {
        int index = 0;
        while (index < positions.GetCount())
        {
            SfntGposAttachmentType type = positions.GetAttachmentType(index);
            if (type == SfntGposAttachmentType.Mark)
            {
                buffer.SetAttachment(
                    index,
                    positions.GetAttachmentParent(index),
                    ShapingAttachmentType.Mark);
            }
            else if (type == SfntGposAttachmentType.Cursive)
            {
                buffer.SetAttachment(
                    index,
                    positions.GetAttachmentParent(index),
                    ShapingAttachmentType.Cursive);
            }
            index += 1;
        }
    }

    static SfntLayoutSelection CreateSelection(
        SfntLayoutFeaturePlan featurePlan,
        SfntLayoutTable table,
        int scriptTag,
        int languageTag)
    {
        SfntUserFeature[] features =
            new SfntUserFeature[featurePlan.GetSelectionCount()];
        int index = 0;
        while (index < features.Length)
        {
            features[index] = featurePlan.GetSelection(index);
            index += 1;
        }

        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            features,
            featurePlan.GetTextLength());
    }

    static SfntLayoutGlyphBuffer CreateLayoutBuffer(ShapingBuffer buffer)
    {
        int count = buffer.GetCount();
        int[] glyphIds = new int[count];
        int[] starts = new int[count];
        int[] lengths = new int[count];
        int[] componentCounts = new int[count];
        int[] componentIndexes = new int[count];
        int index = 0;
        while (index < count)
        {
            glyphIds[index] = buffer.GetGlyphId(index);
            starts[index] = buffer.GetClusterStart(index);
            lengths[index] = buffer.GetClusterLength(index);
            componentCounts[index] = buffer.GetLigatureComponentCount(index);
            componentIndexes[index] = buffer.GetLigatureComponentIndex(index);
            index += 1;
        }

        return SfntLayoutGlyphBuffer.Create(
            glyphIds,
            starts,
            lengths,
            componentCounts,
            componentIndexes);
    }

    static void ApplyGsubSelection(
        SfntGsubTable table,
        SfntLayoutSelection selection,
        SfntLayoutGlyphBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntLayoutExecutionContext? context)
    {
        int index = 0;
        while (index < selection.GetLookupCount())
        {
            SfntLayoutLookupSelection lookup = selection.GetLookup(index);
            SfntGsubExecutor.ApplyLookup(
                table,
                lookup,
                buffer,
                gdef,
                direction,
                context);
            index += 1;
        }
    }

    static void ApplyGposSelection(
        SfntGposTable table,
        SfntLayoutSelection selection,
        SfntGposPositionBuffer buffer,
        SfntGdefTable? gdef,
        SfntLayoutTraversalDirection direction,
        SfntGposAdjustmentContext adjustmentContext,
        SfntLayoutExecutionContext context)
    {
        int index = 0;
        while (index < selection.GetLookupCount())
        {
            SfntLayoutLookupSelection lookup = selection.GetLookup(index);
            SfntGposExecutor.ApplyLookup(
                table,
                lookup.GetLookupIndex(),
                buffer,
                gdef,
                direction,
                adjustmentContext,
                context);
            index += 1;
        }
    }

    static void InitializeAdvances(
        SfntGposPositionBuffer positions,
        ShapingBuffer buffer,
        FontInstance fontInstance)
    {
        int index = 0;
        while (index < positions.GetCount())
        {
            if (!buffer.IsDefaultIgnorable(index))
            {
                GlyphMetrics metrics = fontInstance.GetGlyphMetrics(
                    positions.GetGlyphId(index));
                double xAdvance = 0;
                double yAdvance = 0;
                if (buffer.IsHorizontal())
                {
                    xAdvance = metrics.GetAdvanceWidth();
                }
                else if (buffer.GetDirection() ==
                    ShapingDirection.TopToBottom)
                {
                    yAdvance = -metrics.GetAdvanceHeight();
                }
                else
                {
                    yAdvance = metrics.GetAdvanceHeight();
                }

                positions.ApplyAdjustment(
                    index,
                    new SfntGposAdjustment(0, 0, xAdvance, yAdvance));
            }

            index += 1;
        }
    }

    static SfntGposAdjustmentContext CreateAdjustmentContext(
        FontInstance fontInstance,
        SfntGdefTable? gdef,
        TextShapingOptions options)
    {
        SfntVariationLocation? location = null;
        if (gdef != null && gdef.GetItemVariationStore() != null)
        {
            SfntItemVariationStore? variationStore = gdef.GetItemVariationStore();
            System.ArgumentNullException.ThrowIfNull(variationStore);
            location = variationStore.CreateLocation(
                fontInstance.GetCoordinates().GetNormalizedValues());
        }

        SfntHeadTable? head = fontInstance.GetFace().GetHeadTable();
        System.ArgumentNullException.ThrowIfNull(head);
        int unitsPerEm = head
            .GetUnitsPerEm();
        return new SfntGposAdjustmentContext(
            unitsPerEm,
            options.GetHorizontalPixelsPerEm(),
            options.GetVerticalPixelsPerEm(),
            location);
    }

    static SfntLayoutTraversalDirection GetTraversalDirection(
        ShapingBuffer buffer)
    {
        return buffer.IsForward() ?
            SfntLayoutTraversalDirection.Forward :
            SfntLayoutTraversalDirection.Backward;
    }
}
