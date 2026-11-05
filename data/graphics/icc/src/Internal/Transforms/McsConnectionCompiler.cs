namespace icc;

internal class McsConnectionPlan
{
    internal IccTransformDirection Direction;
    internal uint SourceTagSignature;
    internal uint DestinationTagSignature;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal string[] SourceMcsNames;
    internal string[] DestinationMcsNames;
    internal int[] ChannelOrder;
    internal double[] ChannelDefaults;
    internal PipelineStage[] Stages;

    internal McsConnectionPlan()
    {
        Direction = IccTransformDirection.DeviceToPcs;
        SourceTagSignature = 0;
        DestinationTagSignature = 0;
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        SourceMcsNames = new string[0];
        DestinationMcsNames = new string[0];
        ChannelOrder = new int[0];
        ChannelDefaults = new double[0];
        Stages = new PipelineStage[0];
    }
}

internal class McsProfileChannels
{
    internal string[] Names;
    internal double[] Defaults;

    internal McsProfileChannels()
    {
        Names = new string[0];
        Defaults = new double[0];
    }
}

internal static class McsConnectionCompiler
{
    internal static McsConnectionPlan Compile(IccProfile sourceProfile, IccProfile destinationProfile, IccInterpolationKind interpolation)
    {
        ValidateProfiles(sourceProfile, destinationProfile, interpolation);
        McsProfileChannels sourceChannels = ResolveMultiplexChannels(sourceProfile, false);
        McsProfileChannels destinationChannels = ResolveMultiplexChannels(destinationProfile, true);
        ValidateSubsetRequirements(sourceProfile, destinationProfile, sourceChannels.Names, destinationChannels.Names);

        IccTag sourceTag = FindRequiredTag(sourceProfile, IccTagSignatures.AToM0, "source AToM0");
        IccTag destinationTag = FindDestinationTag(destinationProfile);
        GetDecodedData(sourceTag, "source AToM0");
        GetDecodedData(destinationTag, "destination MCS transform");
        IccTransformDirection destinationDirection = destinationTag.Signature == IccTagSignatures.MToA0
            ? IccTransformDirection.PcsToDevice : IccTransformDirection.DeviceToPcs;
        MpePipelinePlan sourcePlan = MpePipelineCompiler.Compile(sourceProfile, sourceTag, IccTransformDirection.DeviceToPcs, interpolation);
        MpePipelinePlan destinationPlan = MpePipelineCompiler.Compile(destinationProfile, destinationTag, destinationDirection, interpolation);
        if (sourcePlan.DestinationChannels != sourceChannels.Names.Length || destinationPlan.SourceChannels != destinationChannels.Names.Length)
        {
            throw new IccTransformException("The MCS transform endpoints do not match their multiplex channel definitions.");
        }

        PipelineStage channelMap = CreateChannelMap(sourceChannels.Names, destinationChannels.Names, destinationChannels.Defaults);
        PipelineStage[] stages = JoinStages(sourcePlan.Stages, channelMap, destinationPlan.Stages);
        PipelineExecutor.GetRequiredValueScratchLength(stages);

        McsConnectionPlan plan = new McsConnectionPlan();
        plan.Direction = destinationProfile.Header.ProfileClass == IccProfileClass.MultiplexLink
            ? IccTransformDirection.DeviceToDevice : IccTransformDirection.DeviceToPcs;
        plan.SourceTagSignature = sourceTag.Signature;
        plan.DestinationTagSignature = destinationTag.Signature;
        plan.SourceChannels = sourcePlan.SourceChannels;
        plan.DestinationChannels = destinationPlan.DestinationChannels;
        plan.SourceRanges = CloneRanges(sourcePlan.SourceRanges);
        plan.DestinationRanges = CloneRanges(destinationPlan.DestinationRanges);
        plan.SourceMcsNames = CloneStrings(sourceChannels.Names);
        plan.DestinationMcsNames = CloneStrings(destinationChannels.Names);
        plan.ChannelOrder = CopyIntegers(channelMap.ChannelOrder);
        plan.ChannelDefaults = CopyDoubles(channelMap.ChannelDefaults);
        plan.Stages = stages;
        return plan;
    }

    internal static void ValidateProfiles(IccProfile sourceProfile, IccProfile destinationProfile, IccInterpolationKind interpolation)
    {
        if (sourceProfile == null || sourceProfile.Header == null || destinationProfile == null || destinationProfile.Header == null)
        {
            throw new IccTransformException("MCS connection requires source and destination profiles with headers.");
        }
        if (sourceProfile.Header.Version.Kind != IccVersionKind.Version5 || destinationProfile.Header.Version.Kind != IccVersionKind.Version5)
        {
            throw new IccTransformException("MCS connection requires two ICC.2 profiles.");
        }
        if (sourceProfile.Header.ProfileClass != IccProfileClass.Input && sourceProfile.Header.ProfileClass != IccProfileClass.MultiplexIdentification)
        {
            throw new IccTransformException("An MCS source shall be an Input or MultiplexIdentification profile.");
        }
        if (destinationProfile.Header.ProfileClass != IccProfileClass.MultiplexLink && destinationProfile.Header.ProfileClass != IccProfileClass.MultiplexVisualization)
        {
            throw new IccTransformException("An MCS destination shall be a MultiplexLink or MultiplexVisualization profile.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested MCS transform interpolation algorithm is invalid.");
        }
    }

    internal static McsProfileChannels ResolveMultiplexChannels(IccProfile profile, bool includeDefaults)
    {
        int expected = DecodeMcsChannelCount(profile.Header.MaterialConnectionSpaceSignature);
        IccTag namesTag = FindRequiredTag(profile, IccTagSignatures.MultiplexTypeArray, "multiplexTypeArray");
        IccTagData namesData = GetDecodedData(namesTag, "multiplexTypeArray");
        IccMultiplexChannels? namesProjection = namesData.MultiplexChannels;
        if (namesProjection == null || namesProjection.Names == null || namesProjection.Names.Length != expected)
        {
            throw new IccTransformException($"The multiplexTypeArray tag shall define exactly {expected} channel names.");
        }
        ValidateNames(namesProjection.Names, "multiplexTypeArray");

        double[] defaults = new double[expected];
        IccTag? defaultsTag = FindOptionalTag(profile, IccTagSignatures.MultiplexDefaultValues);
        if (includeDefaults && defaultsTag != null)
        {
            IccTagData defaultsData = GetDecodedData(defaultsTag, "multiplexDefaultValues");
            IccMultiplexChannels? defaultsProjection = defaultsData.MultiplexChannels;
            if (defaultsProjection == null || defaultsProjection.DefaultValues == null || defaultsProjection.DefaultValues.Length != expected)
            {
                throw new IccTransformException($"The multiplexDefaultValues tag shall define exactly {expected} channel defaults.");
            }
            defaults = CopyDoubles(defaultsProjection.DefaultValues);
            ValidateDefaults(defaults);
        }

        McsProfileChannels result = new McsProfileChannels();
        result.Names = CloneStrings(namesProjection.Names);
        result.Defaults = defaults;
        return result;
    }

    internal static int DecodeMcsChannelCount(uint signature)
    {
        int count = (int)(signature & 0x0000ffff);
        if ((signature & 0xffff0000) != IccColorSpaceSignatures.MultiplexPrefix || count < 1)
        {
            throw new IccTransformException("An MCS profile header shall declare a non-empty multiplex Material Connection Space.");
        }
        return count;
    }

    internal static void ValidateNames(string[] names, string description)
    {
        System.Collections.Generic.Dictionary<string, bool> seen = new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < names.Length)
        {
            string? name = names[index];
            if (name == null || name.Length == 0)
            {
                throw new IccTransformException($"The {description} channel name {index} shall be non-empty.");
            }
            if (seen.ContainsKey(name))
            {
                throw new IccTransformException($"The {description} channel name '{name}' is duplicated; MCS matching is case-sensitive.");
            }
            seen.Add(name, true);
            index = index + 1;
        }
    }

    internal static void ValidateDefaults(double[] defaults)
    {
        int index = 0;
        while (index < defaults.Length)
        {
            if (!System.Double.IsFinite(defaults[index]))
            {
                throw new IccTransformException($"MCS default value {index} shall be finite.");
            }
            index = index + 1;
        }
    }

    internal static void ValidateSubsetRequirements(IccProfile sourceProfile, IccProfile destinationProfile, string[] sourceNames, string[] destinationNames)
    {
        if (sourceProfile.Header.RequiresMcsSubset)
        {
            string missing = FindFirstMissingName(sourceNames, destinationNames);
            if (missing.Length != 0)
            {
                throw new IccTransformException($"The source profile requires its MCS channels to be a subset, but source channel '{missing}' is absent from the destination profile.");
            }
        }
        if (destinationProfile.Header.RequiresMcsSubset)
        {
            string missing = FindFirstMissingName(destinationNames, sourceNames);
            if (missing.Length != 0)
            {
                throw new IccTransformException($"The destination profile requires its MCS channels to be a subset, but source channel '{missing}' is absent.");
            }
        }
    }

    internal static string FindFirstMissingName(string[] required, string[] available)
    {
        int requiredIndex = 0;
        while (requiredIndex < required.Length)
        {
            int availableIndex = 0;
            while (availableIndex < available.Length && !System.String.Equals(required[requiredIndex], available[availableIndex], System.StringComparison.Ordinal))
            {
                availableIndex = availableIndex + 1;
            }
            if (availableIndex == available.Length)
            {
                return required[requiredIndex];
            }
            requiredIndex = requiredIndex + 1;
        }
        return string.Empty;
    }

    internal static PipelineStage CreateChannelMap(string[] sourceNames, string[] destinationNames, double[] defaults)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.ChannelMap;
        stage.InputChannels = sourceNames.Length;
        stage.OutputChannels = destinationNames.Length;
        stage.ChannelOrder = new int[destinationNames.Length];
        stage.ChannelDefaults = CopyDoubles(defaults);
        stage.Description = "route MCS channels by case-sensitive multiplex names";
        int destination = 0;
        while (destination < destinationNames.Length)
        {
            stage.ChannelOrder[destination] = -1;
            int source = 0;
            while (source < sourceNames.Length)
            {
                if (System.String.Equals(destinationNames[destination], sourceNames[source], System.StringComparison.Ordinal))
                {
                    stage.ChannelOrder[destination] = source;
                    break;
                }
                source = source + 1;
            }
            destination = destination + 1;
        }
        return stage;
    }

    internal static IccTag FindRequiredTag(IccProfile profile, uint signature, string description)
    {
        IccTag? tag = FindOptionalTag(profile, signature);
        if (tag == null)
        {
            throw new IccTransformException($"The {description} tag is required for MCS connection.");
        }
        return tag;
    }

    internal static IccTag FindDestinationTag(IccProfile profile)
    {
        if (profile.Header.ProfileClass == IccProfileClass.MultiplexLink)
        {
            return FindRequiredTag(profile, IccTagSignatures.MToA0, "destination MToA0");
        }

        IccTag? colorimetric = FindOptionalTag(profile, IccTagSignatures.MToB0);
        int colorimetricChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (colorimetric != null && colorimetricChannels > 0)
        {
            return colorimetric;
        }
        IccTag? spectral = FindOptionalTag(profile, IccTagSignatures.MToS0);
        int spectralChannels = (int)(profile.Header.SpectralPcsSignature & 0x0000ffff);
        if (spectral != null && spectralChannels > 0)
        {
            return spectral;
        }
        throw new IccTransformException("A MultiplexVisualization destination requires an MToB0 or MToS0 tag.");
    }

    internal static IccTag? FindOptionalTag(IccProfile profile, uint signature)
    {
        if (profile.Tags == null)
        {
            return null;
        }
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag? tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }
            index = index + 1;
        }
        return null;
    }

    internal static IccTagData GetDecodedData(IccTag tag, string description)
    {
        IccTagData data = tag.DecodeState == null ? tag.Data : TagDecoder.GetDecodedData(tag);
        if (tag.DecodeResult.Status != IccOperationStatus.Success && data.Kind == IccTagDataKind.Unknown)
        {
            throw new IccTransformException($"The {description} tag could not be decoded: {tag.DecodeResult.Message}");
        }
        return data;
    }

    internal static PipelineStage[] JoinStages(PipelineStage[] source, PipelineStage map, PipelineStage[] destination)
    {
        PipelineStage[] stages = new PipelineStage[source.Length + destination.Length + 1];
        int target = 0;
        int index = 0;
        while (index < source.Length)
        {
            stages[target] = source[index];
            target = target + 1;
            index = index + 1;
        }
        stages[target] = map;
        target = target + 1;
        index = 0;
        while (index < destination.Length)
        {
            stages[target] = destination[index];
            target = target + 1;
            index = index + 1;
        }
        return stages;
    }

    internal static IccEncodingRange[] CloneRanges(IccEncodingRange[] source)
    {
        IccEncodingRange[] result = new IccEncodingRange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new IccEncodingRange();
            result[index].Minimum = source[index].Minimum;
            result[index].Maximum = source[index].Maximum;
            index = index + 1;
        }
        return result;
    }

    internal static string[] CloneStrings(string[] source)
    {
        string[] result = new string[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static int[] CopyIntegers(int[] source)
    {
        int[] result = new int[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static double[] CopyDoubles(double[] source)
    {
        double[] result = new double[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }
}
