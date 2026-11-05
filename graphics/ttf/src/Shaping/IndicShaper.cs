namespace ttf;

internal enum IndicShapingScript
{
    Devanagari = 0,
    Bengali = 1,
    Gurmukhi = 2,
    Gujarati = 3,
    Oriya = 4,
    Tamil = 5,
    Telugu = 6,
    Kannada = 7,
    Malayalam = 8
}

internal enum IndicRephMode
{
    None = 0,
    Implicit = 1,
    Explicit = 2,
    Logical = 3
}

internal enum IndicRephPosition
{
    AfterMain = 0,
    BeforeSub = 1,
    AfterSub = 2,
    BeforePost = 3,
    AfterPost = 4
}

internal enum IndicShaperStageKind
{
    LocalizedForms = 0,
    CompositionAndDecomposition = 1,
    InitialReordering = 2,
    NuktaForms = 3,
    AkhandForms = 4,
    RephForms = 5,
    RephAfterRephForms = 6,
    PrebaseForms = 7,
    BelowbaseForms = 8,
    AbovebaseForms = 9,
    HalfForms = 10,
    PostbaseForms = 11,
    VattuForms = 12,
    ConjunctForms = 13,
    FinalReordering = 14,
    InitialForms = 15,
    PresentationForms = 16,
    AbovebaseSubstitutions = 17,
    BelowbaseSubstitutions = 18,
    PostbaseSubstitutions = 19,
    HalantForms = 20,
    UserSubstitution = 21,
    Distances = 22,
    AboveMarkPositioning = 23,
    BelowMarkPositioning = 24,
    Kerning = 25,
    MarkPositioning = 26,
    UserPositioning = 27
}

internal class IndicScriptConfiguration
{
    IndicShapingScript script;
    int oldScriptTag;
    int newScriptTag;
    int virama;
    IndicRephMode rephMode;
    IndicRephPosition rephPosition;

    internal IndicScriptConfiguration(
        IndicShapingScript script,
        int oldScriptTag,
        int newScriptTag,
        int virama,
        IndicRephMode rephMode,
        IndicRephPosition rephPosition)
    {
        this.script = script;
        this.oldScriptTag = oldScriptTag;
        this.newScriptTag = newScriptTag;
        this.virama = virama;
        this.rephMode = rephMode;
        this.rephPosition = rephPosition;
    }

    internal IndicShapingScript GetScript()
    {
        return script;
    }

    internal int GetScriptTag(IndicShapingModel model)
    {
        ValidateModel(model);
        return model == IndicShapingModel.Version1 ?
            oldScriptTag : newScriptTag;
    }

    internal int GetFallbackScriptTag(IndicShapingModel model)
    {
        ValidateModel(model);
        return model == IndicShapingModel.Version1 ?
            newScriptTag : oldScriptTag;
    }

    internal int GetVirama()
    {
        return virama;
    }

    internal IndicRephMode GetRephMode()
    {
        return rephMode;
    }

    internal IndicRephPosition GetRephPosition()
    {
        return rephPosition;
    }

    static void ValidateModel(IndicShapingModel model)
    {
        if (model != IndicShapingModel.Version1 &&
            model != IndicShapingModel.Version2)
        {
            throw new System.ArgumentOutOfRangeException("model");
        }
    }
}

internal class IndicShaperFeatureStage
{
    IndicShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int scriptTag;
    int textLength;
    bool reorderingBarrier;

    internal IndicShaperFeatureStage(
        IndicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int scriptTag,
        int textLength,
        bool reorderingBarrier)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.scriptTag = scriptTag;
        this.textLength = textLength;
        this.reorderingBarrier = reorderingBarrier;
    }

    internal IndicShaperStageKind GetKind()
    {
        return kind;
    }

    internal DefaultShaperTableKind GetTableKind()
    {
        return tableKind;
    }

    internal SfntLayoutFeaturePlan GetFeaturePlan()
    {
        return featurePlan;
    }

    internal bool IsReorderingBarrier()
    {
        return reorderingBarrier;
    }

    internal SfntLayoutSelection CreateSelection(
        SfntLayoutTable table,
        int languageTag)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntUserFeature[] selections =
            new SfntUserFeature[featurePlan.GetSelectionCount()];
        int index = 0;
        while (index < selections.Length)
        {
            selections[index] = featurePlan.GetSelection(index);
            index++;
        }

        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class IndicShaperPlan
{
    IndicShapingScript script;
    IndicShapingModel model;
    int scriptTag;
    int fallbackScriptTag;
    int virama;
    IndicShaperPreparation preparation;
    IndicShaperFeatureStage[] stages;

    internal IndicShaperPlan(
        IndicShapingScript script,
        IndicShapingModel model,
        int scriptTag,
        int fallbackScriptTag,
        int virama,
        IndicShaperPreparation preparation,
        IndicShaperFeatureStage[] stages)
    {
        this.script = script;
        this.model = model;
        this.scriptTag = scriptTag;
        this.fallbackScriptTag = fallbackScriptTag;
        this.virama = virama;
        this.preparation = preparation;
        this.stages = stages;
    }

    internal IndicShapingScript GetScript()
    {
        return script;
    }

    internal IndicShapingModel GetModel()
    {
        return model;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal int GetFallbackScriptTag()
    {
        return fallbackScriptTag;
    }

    internal int GetVirama()
    {
        return virama;
    }

    internal IndicShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal IndicShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal IndicShaperFeatureStage? FindStage(
        IndicShaperStageKind kind)
    {
        ValidateStageKind(kind);
        int index = 0;
        while (index < stages.Length)
        {
            if (stages[index].GetKind() == kind)
            {
                return stages[index];
            }

            index++;
        }

        return null;
    }

    static void ValidateStageKind(IndicShaperStageKind kind)
    {
        if (kind < IndicShaperStageKind.LocalizedForms ||
            kind > IndicShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class IndicShaper
{
    const int AbovebaseFormsTag = 0x61627666;
    const int AbovebaseSubstitutionsTag = 0x61627673;
    const int AboveMarkPositioningTag = 0x6162766D;
    const int AkhandFormsTag = 0x616B686E;
    const int BelowbaseFormsTag = 0x626C7766;
    const int BelowbaseSubstitutionsTag = 0x626C7773;
    const int BelowMarkPositioningTag = 0x626C776D;
    const int CharacterCompositionTag = 0x63636D70;
    const int ConjunctFormsTag = 0x636A6374;
    const int DistancesTag = 0x64697374;
    const int HalantFormsTag = 0x68616C6E;
    const int HalfFormsTag = 0x68616C66;
    const int InitialFormsTag = 0x696E6974;
    const int KerningTag = 0x6B65726E;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int NuktaFormsTag = 0x6E756B74;
    const int PostbaseFormsTag = 0x70737466;
    const int PostbaseSubstitutionsTag = 0x70737473;
    const int PrebaseFormsTag = 0x70726566;
    const int PresentationFormsTag = 0x70726573;
    const int RephAfterRephFormsTag = 0x726B7266;
    const int RephFormsTag = 0x72706866;
    const int VattuFormsTag = 0x76617475;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        NuktaFormsTag,
        AkhandFormsTag,
        RephFormsTag,
        RephAfterRephFormsTag,
        PrebaseFormsTag,
        BelowbaseFormsTag,
        AbovebaseFormsTag,
        HalfFormsTag,
        PostbaseFormsTag,
        VattuFormsTag,
        ConjunctFormsTag,
        InitialFormsTag,
        PresentationFormsTag,
        AbovebaseSubstitutionsTag,
        BelowbaseSubstitutionsTag,
        PostbaseSubstitutionsTag,
        HalantFormsTag,
        DistancesTag,
        AboveMarkPositioningTag,
        BelowMarkPositioningTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    static readonly IndicScriptConfiguration[] Configurations =
        new IndicScriptConfiguration[]
    {
        Configuration(IndicShapingScript.Devanagari, "deva", "dev2",
            0x094D, IndicRephMode.Implicit,
            IndicRephPosition.BeforePost),
        Configuration(IndicShapingScript.Bengali, "beng", "bng2",
            0x09CD, IndicRephMode.Implicit,
            IndicRephPosition.AfterSub),
        Configuration(IndicShapingScript.Gurmukhi, "guru", "gur2",
            0x0A4D, IndicRephMode.None,
            IndicRephPosition.BeforeSub),
        Configuration(IndicShapingScript.Gujarati, "gujr", "gjr2",
            0x0ACD, IndicRephMode.Implicit,
            IndicRephPosition.BeforePost),
        Configuration(IndicShapingScript.Oriya, "orya", "ory2",
            0x0B4D, IndicRephMode.Implicit,
            IndicRephPosition.AfterMain),
        Configuration(IndicShapingScript.Tamil, "taml", "tml2",
            0x0BCD, IndicRephMode.None,
            IndicRephPosition.AfterPost),
        Configuration(IndicShapingScript.Telugu, "telu", "tel2",
            0x0C4D, IndicRephMode.Explicit,
            IndicRephPosition.AfterPost),
        Configuration(IndicShapingScript.Kannada, "knda", "knd2",
            0x0CCD, IndicRephMode.Implicit,
            IndicRephPosition.AfterPost),
        Configuration(IndicShapingScript.Malayalam, "mlym", "mlm2",
            0x0D4D, IndicRephMode.Logical,
            IndicRephPosition.AfterMain)
    };

    internal static IndicShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        IndicShapingScript script,
        IndicShapingModel model,
        int maximumSyllableCount)
    {
        IndicScriptConfiguration configuration =
            GetConfiguration(script);
        return IndicReorderer.Prepare(
            buffer,
            unicodeData,
            configuration,
            model,
            maximumSyllableCount);
    }

    internal static IndicShaperPlan BuildPlan(
        IndicShaperPreparation preparation,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        ValidateUserFeatures(userFeatures);
        IndicScriptConfiguration configuration = GetConfiguration(
            preparation.GetScript());
        IndicShapingModel model = preparation.GetModel();
        int scriptTag = configuration.GetScriptTag(model);
        int textLength = preparation.GetSyllableMap().GetRuneCount();
        IndicShaperFeatureStage[] stages =
            new IndicShaperFeatureStage[28];
        stages[0] = CreateGlobalStage(
            IndicShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            LocalizedFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[1] = CreateGlobalStage(
            IndicShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            CharacterCompositionTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[2] = CreateBarrierStage(
            IndicShaperStageKind.InitialReordering,
            scriptTag,
            textLength);
        stages[3] = CreateGlobalStage(
            IndicShaperStageKind.NuktaForms,
            DefaultShaperTableKind.Substitution,
            NuktaFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[4] = CreateGlobalStage(
            IndicShaperStageKind.AkhandForms,
            DefaultShaperTableKind.Substitution,
            AkhandFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[5] = CreateSelectiveStage(
            IndicShaperStageKind.RephForms,
            RephFormsTag,
            CreateRephValues(preparation),
            scriptTag,
            textLength);
        stages[6] = CreateGlobalStage(
            IndicShaperStageKind.RephAfterRephForms,
            DefaultShaperTableKind.Substitution,
            RephAfterRephFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[7] = CreateSelectiveStage(
            IndicShaperStageKind.PrebaseForms,
            PrebaseFormsTag,
            CreatePrebaseValues(preparation),
            scriptTag,
            textLength);
        stages[8] = CreateGlobalStage(
            IndicShaperStageKind.BelowbaseForms,
            DefaultShaperTableKind.Substitution,
            BelowbaseFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[9] = CreateGlobalStage(
            IndicShaperStageKind.AbovebaseForms,
            DefaultShaperTableKind.Substitution,
            AbovebaseFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[10] = CreateSelectiveStage(
            IndicShaperStageKind.HalfForms,
            HalfFormsTag,
            CreateHalfValues(preparation),
            scriptTag,
            textLength);
        stages[11] = CreateGlobalStage(
            IndicShaperStageKind.PostbaseForms,
            DefaultShaperTableKind.Substitution,
            PostbaseFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[12] = CreateGlobalStage(
            IndicShaperStageKind.VattuForms,
            DefaultShaperTableKind.Substitution,
            VattuFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[13] = CreateGlobalStage(
            IndicShaperStageKind.ConjunctForms,
            DefaultShaperTableKind.Substitution,
            ConjunctFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[14] = CreateBarrierStage(
            IndicShaperStageKind.FinalReordering,
            scriptTag,
            textLength);
        stages[15] = CreateSelectiveStage(
            IndicShaperStageKind.InitialForms,
            InitialFormsTag,
            CreateInitialFormValues(preparation),
            scriptTag,
            textLength);
        stages[16] = CreateGlobalStage(
            IndicShaperStageKind.PresentationForms,
            DefaultShaperTableKind.Substitution,
            PresentationFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[17] = CreateGlobalStage(
            IndicShaperStageKind.AbovebaseSubstitutions,
            DefaultShaperTableKind.Substitution,
            AbovebaseSubstitutionsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[18] = CreateGlobalStage(
            IndicShaperStageKind.BelowbaseSubstitutions,
            DefaultShaperTableKind.Substitution,
            BelowbaseSubstitutionsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[19] = CreateGlobalStage(
            IndicShaperStageKind.PostbaseSubstitutions,
            DefaultShaperTableKind.Substitution,
            PostbaseSubstitutionsTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[20] = CreateGlobalStage(
            IndicShaperStageKind.HalantForms,
            DefaultShaperTableKind.Substitution,
            HalantFormsTag,
            userFeatures,
            scriptTag,
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[21] = CreatePolicyStage(
            IndicShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            scriptTag,
            textLength,
            false);
        stages[22] = CreateGlobalStage(
            IndicShaperStageKind.Distances,
            DefaultShaperTableKind.Positioning,
            DistancesTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[23] = CreateGlobalStage(
            IndicShaperStageKind.AboveMarkPositioning,
            DefaultShaperTableKind.Positioning,
            AboveMarkPositioningTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[24] = CreateGlobalStage(
            IndicShaperStageKind.BelowMarkPositioning,
            DefaultShaperTableKind.Positioning,
            BelowMarkPositioningTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[25] = CreateGlobalStage(
            IndicShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            KerningTag,
            userFeatures,
            scriptTag,
            textLength);
        stages[26] = CreatePolicyStage(
            IndicShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[] { MarkPositioningTag, MarkToMarkPositioningTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { MarkPositioningTag, MarkToMarkPositioningTag }),
            scriptTag,
            textLength,
            false);
        stages[27] = CreatePolicyStage(
            IndicShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            scriptTag,
            textLength,
            false);
        return new IndicShaperPlan(
            preparation.GetScript(),
            model,
            scriptTag,
            configuration.GetFallbackScriptTag(model),
            configuration.GetVirama(),
            preparation,
            stages);
    }

    internal static int GetScriptTag(
        IndicShapingScript script,
        IndicShapingModel model)
    {
        return GetConfiguration(script).GetScriptTag(model);
    }

    internal static IndicShapingModel SelectModel(
        SfntLayoutTable table,
        IndicShapingScript script)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        IndicScriptConfiguration configuration = GetConfiguration(script);
        int version2Tag = configuration.GetScriptTag(
            IndicShapingModel.Version2);
        if (table.GetScriptList().FindScript(version2Tag) != null)
        {
            return IndicShapingModel.Version2;
        }

        int version1Tag = configuration.GetScriptTag(
            IndicShapingModel.Version1);
        if (table.GetScriptList().FindScript(version1Tag) != null)
        {
            return IndicShapingModel.Version1;
        }

        return IndicShapingModel.Version2;
    }

    static IndicShaperFeatureStage CreateGlobalStage(
        IndicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int tag,
        SfntUserFeature[] userFeatures,
        int scriptTag,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            tableKind,
            new int[] { tag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(userFeatures, new int[] { tag }),
            scriptTag,
            textLength,
            false);
    }

    static IndicShaperFeatureStage CreateSelectiveStage(
        IndicShaperStageKind kind,
        int tag,
        bool[] values,
        int scriptTag,
        int textLength)
    {
        SfntUserFeature[] ranges = CreateRanges(tag, values);
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[] { tag },
            ranges,
            scriptTag,
            textLength,
            false);
    }

    static IndicShaperFeatureStage CreateBarrierStage(
        IndicShaperStageKind kind,
        int scriptTag,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            new SfntUserFeature[0],
            scriptTag,
            textLength,
            true);
    }

    static IndicShaperFeatureStage CreatePolicyStage(
        IndicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        int[] discretionaryTags,
        SfntUserFeature[] features,
        int scriptTag,
        int textLength,
        bool barrier)
    {
        SfntLayoutFeaturePlan featurePlan =
            SfntLayoutFeaturePlanBuilder.Build(
                mandatoryTags,
                defaultTags,
                discretionaryTags,
                features,
                textLength);
        return new IndicShaperFeatureStage(
            kind,
            tableKind,
            featurePlan,
            scriptTag,
            textLength,
            barrier);
    }

    static bool[] CreateRephValues(IndicShaperPreparation preparation)
    {
        int count = preparation.GetSyllableMap().GetRuneCount();
        bool[] values = new bool[count];
        int syllableIndex = 0;
        while (syllableIndex < preparation.GetSyllableCount())
        {
            int logicalStart = preparation.GetSyllableMap().GetSyllableStart(
                syllableIndex);
            int logicalEnd = preparation.GetSyllableMap().GetSyllableEnd(
                syllableIndex);
            int rephLength = preparation.GetRephLength(syllableIndex);
            int index = logicalStart;
            while (index < logicalEnd)
            {
                int sourceIndex = preparation.GetInitialSourceIndex(index);
                if (sourceIndex >= logicalStart &&
                    sourceIndex < logicalStart + rephLength)
                {
                    values[index] = true;
                }

                index++;
            }

            syllableIndex++;
        }

        return values;
    }

    static bool[] CreatePrebaseValues(
        IndicShaperPreparation preparation)
    {
        int count = preparation.GetSyllableMap().GetRuneCount();
        bool[] values = new bool[count];
        int syllableIndex = 0;
        while (syllableIndex < preparation.GetSyllableCount())
        {
            int sourceStart = preparation.GetPrebaseSourceStart(
                syllableIndex);
            if (sourceStart >= 0)
            {
                int start = preparation.GetSyllableMap().GetSyllableStart(
                    syllableIndex);
                int end = preparation.GetSyllableMap().GetSyllableEnd(
                    syllableIndex);
                int index = start;
                while (index < end)
                {
                    int sourceIndex = preparation.GetInitialSourceIndex(index);
                    if (sourceIndex == sourceStart ||
                        sourceIndex == sourceStart + 1)
                    {
                        values[index] = true;
                    }

                    index++;
                }
            }

            syllableIndex++;
        }

        return values;
    }

    static bool[] CreateHalfValues(IndicShaperPreparation preparation)
    {
        int count = preparation.GetSyllableMap().GetRuneCount();
        bool[] values = new bool[count];
        int syllableIndex = 0;
        while (syllableIndex < preparation.GetSyllableCount())
        {
            int start = preparation.GetSyllableMap().GetSyllableStart(
                syllableIndex);
            int end = preparation.GetSyllableMap().GetSyllableEnd(
                syllableIndex);
            int baseIndex = preparation.GetInitialBaseIndex(syllableIndex);
            int index = start;
            while (index < end && index < baseIndex)
            {
                IndicSyllabicCategory category =
                    preparation.GetInitialCategory(index);
                values[index] = category == IndicSyllabicCategory.Consonant ||
                    category == IndicSyllabicCategory.Ra ||
                    category == IndicSyllabicCategory.Halant ||
                    category == IndicSyllabicCategory.Nukta ||
                    category == IndicSyllabicCategory.Joiner ||
                    category == IndicSyllabicCategory.NonJoiner;
                index++;
            }

            index = start;
            while (index < end)
            {
                if (preparation.GetInitialCategory(index) ==
                    IndicSyllabicCategory.NonJoiner)
                {
                    int clearIndex = index;
                    while (clearIndex > start)
                    {
                        clearIndex--;
                        values[clearIndex] = false;
                        IndicSyllabicCategory category =
                            preparation.GetInitialCategory(clearIndex);
                        if (category == IndicSyllabicCategory.Consonant ||
                            category == IndicSyllabicCategory.Ra)
                        {
                            break;
                        }
                    }
                }

                index++;
            }

            int rephLength = preparation.GetRephLength(syllableIndex);
            index = start;
            while (index < end && rephLength > 0)
            {
                int sourceIndex = preparation.GetInitialSourceIndex(index);
                int logicalStart = preparation.GetSyllableMap()
                    .GetSyllableStart(syllableIndex);
                if (sourceIndex >= logicalStart &&
                    sourceIndex < logicalStart + rephLength)
                {
                    values[index] = false;
                }

                index++;
            }

            syllableIndex++;
        }

        return values;
    }

    static bool[] CreateInitialFormValues(
        IndicShaperPreparation preparation)
    {
        int count = preparation.GetSyllableMap().GetRuneCount();
        bool[] values = new bool[count];
        int syllableIndex = 0;
        while (syllableIndex < preparation.GetSyllableCount())
        {
            int start = preparation.GetSyllableMap().GetSyllableStart(
                syllableIndex);
            if (start < count)
            {
                IndicSyllabicCategory category =
                    preparation.GetFinalCategory(start);
                IndicPositionalCategory position =
                    preparation.GetFinalPosition(start);
                if ((category == IndicSyllabicCategory.Matra ||
                    category == IndicSyllabicCategory.PostMatra) &&
                    IsLeftPosition(position))
                {
                    values[start] = true;
                }
            }

            syllableIndex++;
        }

        return values;
    }

    static bool IsLeftPosition(IndicPositionalCategory position)
    {
        return position == IndicPositionalCategory.Left ||
            position == IndicPositionalCategory.LeftAndRight ||
            position == IndicPositionalCategory.TopAndBottomAndLeft ||
            position == IndicPositionalCategory.TopAndLeft ||
            position == IndicPositionalCategory.TopAndLeftAndRight ||
            position == IndicPositionalCategory.VisualOrderLeft;
    }

    static SfntUserFeature[] CreateRanges(int tag, bool[] values)
    {
        int rangeCount = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (!values[index])
            {
                index++;
                continue;
            }

            rangeCount++;
            while (index < values.Length && values[index])
            {
                index++;
            }
        }

        SfntUserFeature[] ranges = new SfntUserFeature[rangeCount];
        index = 0;
        int rangeIndex = 0;
        while (index < values.Length)
        {
            if (!values[index])
            {
                index++;
                continue;
            }

            int start = index;
            while (index < values.Length && values[index])
            {
                index++;
            }

            ranges[rangeIndex] = new SfntUserFeature(
                tag,
                1,
                start,
                index);
            rangeIndex++;
        }

        return ranges;
    }

    static IndicScriptConfiguration GetConfiguration(
        IndicShapingScript script)
    {
        if (script < IndicShapingScript.Devanagari ||
            script > IndicShapingScript.Malayalam)
        {
            throw new System.ArgumentOutOfRangeException("script");
        }

        return Configurations[(int)script];
    }

    static IndicScriptConfiguration Configuration(
        IndicShapingScript script,
        string oldTag,
        string newTag,
        int virama,
        IndicRephMode rephMode,
        IndicRephPosition rephPosition)
    {
        return new IndicScriptConfiguration(
            script,
            EncodeTag(oldTag),
            EncodeTag(newTag),
            virama,
            rephMode,
            rephPosition);
    }

    static int EncodeTag(string value)
    {
        return value[0] << 24 | value[1] << 16 |
            value[2] << 8 | value[3];
    }

    static SfntUserFeature[] CopyMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags)
    {
        int count = CountMatchingFeatures(features, tags, true);
        SfntUserFeature[] result = new SfntUserFeature[count];
        int index = 0;
        int resultIndex = 0;
        while (index < features.Length)
        {
            if (ContainsTag(tags, features[index].GetFeatureTag()))
            {
                result[resultIndex] = features[index];
                resultIndex++;
            }

            index++;
        }

        return result;
    }

    static SfntUserFeature[] CopyCustomFeatures(
        SfntUserFeature[] features)
    {
        int count = CountMatchingFeatures(
            features,
            BuiltInFeatureTags,
            false);
        SfntUserFeature[] result = new SfntUserFeature[count];
        int index = 0;
        int resultIndex = 0;
        while (index < features.Length)
        {
            if (!ContainsTag(
                BuiltInFeatureTags,
                features[index].GetFeatureTag()))
            {
                result[resultIndex] = features[index];
                resultIndex++;
            }

            index++;
        }

        return result;
    }

    static int CountMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags,
        bool matching)
    {
        int count = 0;
        int index = 0;
        while (index < features.Length)
        {
            bool contains = ContainsTag(
                tags,
                features[index].GetFeatureTag());
            if (contains == matching)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static bool ContainsTag(int[] tags, int tag)
    {
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index] == tag)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateUserFeatures(SfntUserFeature[] features)
    {
        if (features == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        int index = 0;
        while (index < features.Length)
        {
            if (features[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index++;
        }
    }
}
