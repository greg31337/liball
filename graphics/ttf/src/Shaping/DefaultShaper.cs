namespace ttf;

internal enum DefaultShaperTableKind
{
    Substitution = 0,
    Positioning = 1
}

internal enum DefaultShaperStageKind
{
    LocalizedForms = 0,
    CompositionAndDecomposition = 1,
    Fractions = 2,
    Ligatures = 3,
    UserSubstitution = 4,
    Kerning = 5,
    Marks = 6,
    UserPositioning = 7
}

internal class DefaultShaperFeatureStage
{
    DefaultShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;

    internal DefaultShaperFeatureStage(
        DefaultShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int textLength)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.textLength = textLength;
    }

    internal DefaultShaperStageKind GetKind()
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

        SfntUserFeature[] features =
            new SfntUserFeature[featurePlan.GetSelectionCount()];
        int index = 0;
        while (index < features.Length)
        {
            features[index] = featurePlan.GetSelection(index);
            index++;
        }

        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            features,
            textLength);
    }
}

internal class DefaultShaperPlan
{
    DefaultShaperFeatureStage[] stages;
    int textLength;

    internal DefaultShaperPlan(
        DefaultShaperFeatureStage[] stages,
        int textLength)
    {
        this.stages = stages;
        this.textLength = textLength;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal DefaultShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal DefaultShaperFeatureStage? FindStage(
        DefaultShaperStageKind kind)
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

    internal int GetTextLength()
    {
        return textLength;
    }

    static void ValidateStageKind(DefaultShaperStageKind kind)
    {
        if (kind != DefaultShaperStageKind.LocalizedForms &&
            kind != DefaultShaperStageKind.CompositionAndDecomposition &&
            kind != DefaultShaperStageKind.Fractions &&
            kind != DefaultShaperStageKind.Ligatures &&
            kind != DefaultShaperStageKind.UserSubstitution &&
            kind != DefaultShaperStageKind.Kerning &&
            kind != DefaultShaperStageKind.Marks &&
            kind != DefaultShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class DefaultShaper
{
    const int CharacterCompositionTag = 0x63636D70;
    const int ContextualLigaturesTag = 0x636C6967;
    const int DenominatorsTag = 0x646E6F6D;
    const int FractionsTag = 0x66726163;
    const int KerningTag = 0x6B65726E;
    const int LigaturesTag = 0x6C696761;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int NumeratorsTag = 0x6E756D72;
    const int RequiredLigaturesTag = 0x726C6967;
    const int Solidus = 0x002F;
    const int FractionSlash = 0x2044;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        FractionsTag,
        NumeratorsTag,
        DenominatorsTag,
        RequiredLigaturesTag,
        LigaturesTag,
        ContextualLigaturesTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static DefaultShaperPlan BuildPlan(
        ShapingBuffer buffer,
        SfntUserFeature[] userFeatures)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        ValidateUserFeatures(userFeatures);
        int textLength = buffer.GetCount();
        DefaultShaperFeatureStage[] stages =
            new DefaultShaperFeatureStage[8];
        stages[0] = CreateStage(
            DefaultShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { LocalizedFormsTag }),
            textLength);
        stages[1] = CreateStage(
            DefaultShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { CharacterCompositionTag }),
            textLength);
        stages[2] = CreateStage(
            DefaultShaperStageKind.Fractions,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[]
            {
                FractionsTag,
                NumeratorsTag,
                DenominatorsTag
            },
            BuildFractionFeatures(buffer, userFeatures),
            textLength);
        stages[3] = CreateStage(
            DefaultShaperStageKind.Ligatures,
            DefaultShaperTableKind.Substitution,
            new int[] { RequiredLigaturesTag },
            new int[] { LigaturesTag, ContextualLigaturesTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    RequiredLigaturesTag,
                    LigaturesTag,
                    ContextualLigaturesTag
                }),
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[4] = CreateStage(
            DefaultShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        stages[5] = CreateStage(
            DefaultShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[] { KerningTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { KerningTag }),
            textLength);
        stages[6] = CreateStage(
            DefaultShaperStageKind.Marks,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[]
            {
                MarkPositioningTag,
                MarkToMarkPositioningTag
            },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    MarkPositioningTag,
                    MarkToMarkPositioningTag
                }),
            textLength);
        stages[7] = CreateStage(
            DefaultShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        return new DefaultShaperPlan(stages, textLength);
    }

    static DefaultShaperFeatureStage CreateStage(
        DefaultShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        int[] discretionaryTags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            defaultTags,
            discretionaryTags,
            userFeatures,
            textLength);
        return new DefaultShaperFeatureStage(
            kind,
            tableKind,
            plan,
            textLength);
    }

    static SfntUserFeature[] BuildFractionFeatures(
        ShapingBuffer buffer,
        SfntUserFeature[] userFeatures)
    {
        System.Collections.Generic.List<SfntUserFeature> features =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if (codePoint == Solidus || codePoint == FractionSlash)
            {
                int numeratorStart = index;
                while (numeratorStart > 0 && IsDecimalDigit(
                    buffer.GetRune(numeratorStart - 1)))
                {
                    numeratorStart--;
                }

                int denominatorEnd = index + 1;
                while (denominatorEnd < buffer.GetCount() &&
                    IsDecimalDigit(buffer.GetRune(denominatorEnd)))
                {
                    denominatorEnd++;
                }

                if (numeratorStart < index &&
                    denominatorEnd > index + 1)
                {
                    features.Add(new SfntUserFeature(
                        FractionsTag,
                        1,
                        numeratorStart,
                        denominatorEnd));
                    features.Add(new SfntUserFeature(
                        NumeratorsTag,
                        1,
                        numeratorStart,
                        index));
                    features.Add(new SfntUserFeature(
                        DenominatorsTag,
                        1,
                        index + 1,
                        denominatorEnd));
                }
            }

            index++;
        }

        SfntUserFeature[] overrides = CopyMatchingFeatures(
            userFeatures,
            new int[]
            {
                FractionsTag,
                NumeratorsTag,
                DenominatorsTag
            });
        index = 0;
        while (index < overrides.Length)
        {
            features.Add(overrides[index]);
            index++;
        }

        return features.ToArray();
    }

    static SfntUserFeature[] CopyMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags)
    {
        int count = 0;
        int index = 0;
        while (index < features.Length)
        {
            if (ContainsTag(tags, features[index].GetFeatureTag()))
            {
                count++;
            }

            index++;
        }

        SfntUserFeature[] result = new SfntUserFeature[count];
        index = 0;
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
        int count = 0;
        int index = 0;
        while (index < features.Length)
        {
            if (!ContainsTag(
                BuiltInFeatureTags,
                features[index].GetFeatureTag()))
            {
                count++;
            }

            index++;
        }

        SfntUserFeature[] result = new SfntUserFeature[count];
        index = 0;
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

    static bool IsDecimalDigit(System.Text.Rune rune)
    {
        return System.Text.Rune.GetUnicodeCategory(rune) ==
            System.Globalization.UnicodeCategory.DecimalDigitNumber;
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
