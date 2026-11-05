namespace ttf;

internal enum ArabicShaperStageKind
{
    CompositionAndDecomposition = 0,
    LocalizedForms = 1,
    IsolatedForms = 2,
    FinalForms = 3,
    Final2Forms = 4,
    Final3Forms = 5,
    MedialForms = 6,
    Medial2Forms = 7,
    InitialForms = 8,
    RequiredLigatures = 9,
    RequiredContextualAlternates = 10,
    ContextualAlternates = 11,
    StandardLigatures = 12,
    MarkSubstitution = 13,
    UserSubstitution = 14,
    CursivePositioning = 15,
    Kerning = 16,
    MarkPositioning = 17,
    UserPositioning = 18
}

internal class ArabicShaperFeatureStage
{
    ArabicShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;

    internal ArabicShaperFeatureStage(
        ArabicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int textLength)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.textLength = textLength;
    }

    internal ArabicShaperStageKind GetKind()
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

    internal SfntLayoutSelection CreateSelection(
        SfntLayoutTable table,
        int scriptTag,
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

internal class ArabicShaperPlan
{
    ArabicShapingScript script;
    ArabicJoiningAnalysis joiningAnalysis;
    ArabicShaperFeatureStage[] stages;

    internal ArabicShaperPlan(
        ArabicShapingScript script,
        ArabicJoiningAnalysis joiningAnalysis,
        ArabicShaperFeatureStage[] stages)
    {
        this.script = script;
        this.joiningAnalysis = joiningAnalysis;
        this.stages = stages;
    }

    internal ArabicShapingScript GetScript()
    {
        return script;
    }

    internal ArabicJoiningAnalysis GetJoiningAnalysis()
    {
        return joiningAnalysis;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal ArabicShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal ArabicShaperFeatureStage? FindStage(
        ArabicShaperStageKind kind)
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

    static void ValidateStageKind(ArabicShaperStageKind kind)
    {
        if (kind < ArabicShaperStageKind.CompositionAndDecomposition ||
            kind > ArabicShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class ArabicShaper
{
    const int ContextualAlternatesTag = 0x63616C74;
    const int ContextualLigaturesTag = 0x636C6967;
    const int CharacterCompositionTag = 0x63636D70;
    const int CursivePositioningTag = 0x63757273;
    const int DiscretionaryLigaturesTag = 0x646C6967;
    const int FinalFormsTag = 0x66696E61;
    const int Final2FormsTag = 0x66696E32;
    const int Final3FormsTag = 0x66696E33;
    const int InitialFormsTag = 0x696E6974;
    const int IsolatedFormsTag = 0x69736F6C;
    const int KerningTag = 0x6B65726E;
    const int LigaturesTag = 0x6C696761;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkSubstitutionTag = 0x6D736574;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int MedialFormsTag = 0x6D656469;
    const int Medial2FormsTag = 0x6D656432;
    const int RequiredContextualAlternatesTag = 0x72636C74;
    const int RequiredLigaturesTag = 0x726C6967;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        CharacterCompositionTag,
        LocalizedFormsTag,
        IsolatedFormsTag,
        FinalFormsTag,
        Final2FormsTag,
        Final3FormsTag,
        MedialFormsTag,
        Medial2FormsTag,
        InitialFormsTag,
        RequiredLigaturesTag,
        RequiredContextualAlternatesTag,
        ContextualAlternatesTag,
        LigaturesTag,
        ContextualLigaturesTag,
        DiscretionaryLigaturesTag,
        MarkSubstitutionTag,
        CursivePositioningTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static ArabicShaperPlan BuildPlan(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        ArabicShapingScript script,
        SfntUserFeature[] userFeatures)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        ValidateUserFeatures(userFeatures);
        ArabicJoiningAnalysis analysis = ArabicJoiningAnalyzer.Analyze(
            buffer,
            unicodeData,
            script);
        int textLength = buffer.GetCount();
        ArabicShaperFeatureStage[] stages =
            new ArabicShaperFeatureStage[19];
        stages[0] = CreatePolicyStage(
            ArabicShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { CharacterCompositionTag }),
            textLength);
        stages[1] = CreatePolicyStage(
            ArabicShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { LocalizedFormsTag }),
            textLength);
        stages[2] = CreateActionStage(
            ArabicShaperStageKind.IsolatedForms,
            IsolatedFormsTag,
            ArabicJoiningAction.Isolated,
            analysis,
            textLength);
        stages[3] = CreateActionStage(
            ArabicShaperStageKind.FinalForms,
            FinalFormsTag,
            ArabicJoiningAction.Final,
            analysis,
            textLength);
        int final2Tag = script == ArabicShapingScript.Syriac ?
            Final2FormsTag : 0;
        int final3Tag = script == ArabicShapingScript.Syriac ?
            Final3FormsTag : 0;
        int medial2Tag = script == ArabicShapingScript.Syriac ?
            Medial2FormsTag : 0;
        stages[4] = CreateActionStage(
            ArabicShaperStageKind.Final2Forms,
            final2Tag,
            ArabicJoiningAction.Final2,
            analysis,
            textLength);
        stages[5] = CreateActionStage(
            ArabicShaperStageKind.Final3Forms,
            final3Tag,
            ArabicJoiningAction.Final3,
            analysis,
            textLength);
        stages[6] = CreateActionStage(
            ArabicShaperStageKind.MedialForms,
            MedialFormsTag,
            ArabicJoiningAction.Medial,
            analysis,
            textLength);
        stages[7] = CreateActionStage(
            ArabicShaperStageKind.Medial2Forms,
            medial2Tag,
            ArabicJoiningAction.Medial2,
            analysis,
            textLength);
        stages[8] = CreateActionStage(
            ArabicShaperStageKind.InitialForms,
            InitialFormsTag,
            ArabicJoiningAction.Initial,
            analysis,
            textLength);
        stages[9] = CreateMandatoryStage(
            ArabicShaperStageKind.RequiredLigatures,
            DefaultShaperTableKind.Substitution,
            RequiredLigaturesTag,
            userFeatures,
            textLength);
        stages[10] = CreateMandatoryStage(
            ArabicShaperStageKind.RequiredContextualAlternates,
            DefaultShaperTableKind.Substitution,
            RequiredContextualAlternatesTag,
            userFeatures,
            textLength);
        stages[11] = CreateMandatoryStage(
            ArabicShaperStageKind.ContextualAlternates,
            DefaultShaperTableKind.Substitution,
            ContextualAlternatesTag,
            userFeatures,
            textLength);
        stages[12] = CreatePolicyStage(
            ArabicShaperStageKind.StandardLigatures,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[] { LigaturesTag, ContextualLigaturesTag },
            new int[] { DiscretionaryLigaturesTag },
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    LigaturesTag,
                    ContextualLigaturesTag,
                    DiscretionaryLigaturesTag
                }),
            textLength);
        stages[13] = CreateMandatoryStage(
            ArabicShaperStageKind.MarkSubstitution,
            DefaultShaperTableKind.Substitution,
            MarkSubstitutionTag,
            userFeatures,
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[14] = CreatePolicyStage(
            ArabicShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        stages[15] = CreateMandatoryStage(
            ArabicShaperStageKind.CursivePositioning,
            DefaultShaperTableKind.Positioning,
            CursivePositioningTag,
            userFeatures,
            textLength);
        stages[16] = CreateMandatoryStage(
            ArabicShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            KerningTag,
            userFeatures,
            textLength);
        stages[17] = CreatePolicyStage(
            ArabicShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[]
            {
                MarkPositioningTag,
                MarkToMarkPositioningTag
            },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    MarkPositioningTag,
                    MarkToMarkPositioningTag
                }),
            textLength);
        stages[18] = CreatePolicyStage(
            ArabicShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        return new ArabicShaperPlan(script, analysis, stages);
    }

    static ArabicShaperFeatureStage CreateMandatoryStage(
        ArabicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int featureTag,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            tableKind,
            new int[] { featureTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { featureTag }),
            textLength);
    }

    static ArabicShaperFeatureStage CreateActionStage(
        ArabicShaperStageKind kind,
        int featureTag,
        ArabicJoiningAction action,
        ArabicJoiningAnalysis analysis,
        int textLength)
    {
        if (featureTag == 0)
        {
            return CreatePolicyStage(
                kind,
                DefaultShaperTableKind.Substitution,
                new int[0],
                new int[0],
                new int[0],
                new SfntUserFeature[0],
                textLength);
        }

        SfntUserFeature[] ranges = CreateActionRanges(
            analysis,
            action,
            featureTag);
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[] { featureTag },
            ranges,
            textLength);
    }

    static ArabicShaperFeatureStage CreatePolicyStage(
        ArabicShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        int[] discretionaryTags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        SfntLayoutFeaturePlan featurePlan =
            SfntLayoutFeaturePlanBuilder.Build(
                mandatoryTags,
                defaultTags,
                discretionaryTags,
                userFeatures,
                textLength);
        return new ArabicShaperFeatureStage(
            kind,
            tableKind,
            featurePlan,
            textLength);
    }

    static SfntUserFeature[] CreateActionRanges(
        ArabicJoiningAnalysis analysis,
        ArabicJoiningAction action,
        int featureTag)
    {
        int rangeCount = 0;
        int index = 0;
        while (index < analysis.GetCount())
        {
            if (analysis.GetAction(index) != action)
            {
                index++;
                continue;
            }

            rangeCount++;
            while (index < analysis.GetCount() &&
                analysis.GetAction(index) == action)
            {
                index++;
            }
        }

        SfntUserFeature[] ranges =
            new SfntUserFeature[rangeCount];
        int rangeIndex = 0;
        index = 0;
        while (index < analysis.GetCount())
        {
            if (analysis.GetAction(index) != action)
            {
                index++;
                continue;
            }

            int start = index;
            while (index < analysis.GetCount() &&
                analysis.GetAction(index) == action)
            {
                index++;
            }

            ranges[rangeIndex] = new SfntUserFeature(
                featureTag,
                1,
                start,
                index);
            rangeIndex++;
        }

        return ranges;
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

    static void ValidateUserFeatures(SfntUserFeature[] userFeatures)
    {
        if (userFeatures == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        int index = 0;
        while (index < userFeatures.Length)
        {
            if (userFeatures[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index++;
        }
    }
}
