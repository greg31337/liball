namespace icc;

internal class NamedColorProfileFactory
{
    internal ProfileConstructionCore Core;

    internal NamedColorProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateNamedColorProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        IccNamedColorProfileDefinition definition = ValidateNamedColorOptions(options);
        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        tags.Add(CreateNamedColor2Tag(definition.NamedColors));
        if (definition.IncludeLegacyTints)
        {
            tags.Add(CreateLegacyNamedColorTag(definition.NamedColors,
                ColorSpaceChannelCount.Decode(options.DataColorSpaceSignature)));
        }
        if (definition.Colorants != null)
        {
            tags.Add(CreateColorantTag(definition.Colorants));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        return Core.PreflightProfile(profile);
    }

    internal IccNamedColorProfileDefinition ValidateNamedColorOptions(
        IccProfileBuildOptions options)
    {
        IccNamedColorProfileDefinition? candidate = options.NamedColorDefinition;
        if (candidate == null || candidate.NamedColors == null)
        {
            throw new IccProfileValidationException("NamedColor profile construction requires a named-colour definition and record set.");
        }
        if (options.ProfileClass != IccProfileClass.NamedColor)
        {
            throw new IccProfileValidationException("A named-colour profile must use the NamedColor profile class.");
        }
        if (options.AdaptiveGainCurve != null)
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata is not permitted in a NamedColor profile.");
        }

        IccNamedColorProfileDefinition definition = candidate;
        IccNamedColorSet set = definition.NamedColors;
        if (set.Colors == null || set.Colors.Length == 0)
        {
            throw new IccProfileValidationException("A NamedColor profile requires at least one named colour.");
        }
        if (set.TintZero != null)
        {
            throw new IccProfileValidationException("ICC.1 NamedColor profile construction does not support the ICC.2 zero-tint endpoint.");
        }

        int dataChannels = ColorSpaceChannelCount.Decode(
            options.DataColorSpaceSignature);
        if (set.DeviceChannelCount != 0
            && set.DeviceChannelCount != dataChannels)
        {
            throw new IccProfileValidationException("NamedColor device-coordinate counts must be zero or match the profile data-space channel count.");
        }
        ValidateUniqueNames(set);
        bool hasTints = HasTintCoordinates(set);
        if (hasTints != definition.IncludeLegacyTints)
        {
            throw new IccProfileValidationException("Legacy tint coordinates and the IncludeLegacyTints selection must be supplied together.");
        }
        if (definition.IncludeLegacyTints)
        {
            if (options.Version.Kind != IccVersionKind.Version2)
            {
                throw new IccProfileValidationException("Legacy namedColor tint coordinates can be emitted only in an ICC version 2 profile.");
            }
            ValidateTintCoordinates(set, dataChannels);
        }

        IccPcsKind pcs = HeaderCodec.DecodePcs(
            options.ProfileConnectionSpaceSignature);
        NamedColorTagCodec.SerializeNamedColor2(set, options.Version.Kind,
            pcs, options.DataColorSpaceSignature);
        if (definition.IncludeLegacyTints)
        {
            IccNamedColorSet legacy = CreateLegacySet(set, dataChannels);
            NamedColorTagCodec.SerializeNamedColor(legacy,
                options.DataColorSpaceSignature);
        }
        if (definition.Colorants != null)
        {
            if (definition.Colorants.ColorSpaceSignature
                != options.DataColorSpaceSignature)
            {
                throw new IccProfileValidationException("The optional NamedColor colorant table must describe the profile data space.");
            }
            ColorantTableTagCodec.SerializeColorantTable(definition.Colorants,
                options.Version.Kind, pcs, false);
        }
        return definition;
    }

    internal void ValidateUniqueNames(IccNamedColorSet set)
    {
        int index = 0;
        while (index < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[index];
            if (color == null || color.Name == null || color.Name.Length == 0)
            {
                throw new IccProfileValidationException($"Named colour {index} requires a non-empty root name.");
            }
            string fullName = set.Prefix + color.Name + set.Suffix;
            int prior = 0;
            while (prior < index)
            {
                IccNamedColor priorColor = set.Colors[prior];
                string priorName = set.Prefix + priorColor.Name + set.Suffix;
                if (fullName == priorName)
                {
                    throw new IccProfileValidationException($"Named colour '{fullName}' occurs more than once.");
                }
                prior = prior + 1;
            }
            index = index + 1;
        }
    }

    internal bool HasTintCoordinates(IccNamedColorSet set)
    {
        int index = 0;
        while (index < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[index];
            if (color != null && color.Tints != null
                && color.Tints.Length > 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateTintCoordinates(IccNamedColorSet set, int dataChannels)
    {
        int index = 0;
        while (index < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[index];
            if (color.Tints == null || color.Tints.Length != dataChannels)
            {
                throw new IccProfileValidationException($"Named colour {index} must provide one legacy tint coordinate per data-space channel.");
            }
            index = index + 1;
        }
    }

    internal IccNamedColorSet CreateLegacySet(IccNamedColorSet source,
        int dataChannels)
    {
        IccNamedColorSet clone = ExtendedTagDataClone.CloneNamedColorSet(
            source, new ProfileCloneContext());
        clone.DeviceChannelCount = dataChannels;
        return clone;
    }

    internal IccTag CreateNamedColor2Tag(IccNamedColorSet source)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.NamedColors;
        data.NamedColors = ExtendedTagDataClone.CloneNamedColorSet(
            source, new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.NamedColor2;
        return Core.CreateTag(IccTagSignatures.NamedColor2, data);
    }

    internal IccTag CreateLegacyNamedColorTag(IccNamedColorSet source,
        int dataChannels)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.NamedColors;
        data.NamedColors = CreateLegacySet(source, dataChannels);
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.NamedColor;
        return Core.CreateTag(IccTagSignatures.NamedColor, data);
    }

    internal IccTag CreateColorantTag(IccColorantSet source)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Colorants;
        data.Colorants = ExtendedTagDataClone.CloneColorantSet(source);
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.ColorantTable;
        return Core.CreateTag(IccTagSignatures.ColorantTable, data);
    }
}
