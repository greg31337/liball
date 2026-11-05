namespace icc;

internal class DeviceLinkTransformPlan
{
    internal int SourceChannels;
    internal int DestinationChannels;
    internal uint SourceColorSpaceSignature;
    internal uint DestinationColorSpaceSignature;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;
    internal IccTransformOptions Options;
    internal IccProfileSequenceEntry[] ProfileSequence;
    internal IccProfileSequenceIdentifier[] ProfileSequenceIdentifiers;

    internal DeviceLinkTransformPlan()
    {
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceColorSpaceSignature = 0;
        DestinationColorSpaceSignature = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
        Options = new IccTransformOptions();
        ProfileSequence = new IccProfileSequenceEntry[0];
        ProfileSequenceIdentifiers = new IccProfileSequenceIdentifier[0];
    }
}

internal static class DeviceLinkTransformCompiler
{
    internal static IccColorTransform Compile(IccProfile profile, IccTransformOptions requestedOptions)
    {
        DeviceLinkTransformPlan plan = CompilePlan(profile, requestedOptions);
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureDeviceLinkPlan(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            plan.SourceRanges, plan.DestinationRanges, plan.Options, plan.SourceColorSpaceSignature,
            plan.DestinationColorSpaceSignature, plan.ProfileSequence, plan.ProfileSequenceIdentifiers);
        return transform;
    }

    internal static DeviceLinkTransformPlan CompilePlan(IccProfile profile, IccTransformOptions requestedOptions)
    {
        ValidateRequest(profile, requestedOptions);
        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.DeviceToDevice,
            requestedOptions.RenderingIntent, requestedOptions.IntentFallback, false);
        if (!selection.Success || selection.Tag == null || selection.Tag.Data == null)
        {
            throw new IccTransformException($"The DeviceLink transform cannot be selected: {selection.Message}");
        }

        PipelineStage[] stages;
        int sourceChannels;
        int destinationChannels;
        IccEncodingRange[] sourceRanges;
        IccEncodingRange[] destinationRanges;
        if (selection.TagSignature == IccTagSignatures.DToB0)
        {
            MpePipelinePlan mpe = MpePipelineCompiler.CompileDeviceLink(profile, selection.Tag,
                requestedOptions.Interpolation);
            stages = mpe.Stages;
            sourceChannels = mpe.SourceChannels;
            destinationChannels = mpe.DestinationChannels;
            sourceRanges = mpe.SourceRanges;
            destinationRanges = mpe.DestinationRanges;
        }
        else
        {
            LutPipelinePlan lut = LutPipelineCompiler.CompileDeviceLink(profile, selection.Tag,
                requestedOptions.Interpolation);
            stages = lut.Stages;
            sourceChannels = lut.SourceChannels;
            destinationChannels = lut.DestinationChannels;
            sourceRanges = lut.SourceRanges;
            destinationRanges = lut.DestinationRanges;
        }
        StampStages(stages, selection);

        IccTransformOptions options = new IccTransformOptions();
        options.Direction = IccTransformDirection.DeviceToDevice;
        options.RenderingIntent = requestedOptions.RenderingIntent;
        options.IntentFallback = requestedOptions.IntentFallback;
        options.Interpolation = requestedOptions.Interpolation;
        options.PcsEncoding = IccPcsEncodingKind.Normalized;
        options.BlackPointCompensation = false;
        options.ClipOutput = requestedOptions.ClipOutput;
        options.EnableTrace = requestedOptions.EnableTrace;

        ProfileCloneContext context = new ProfileCloneContext();
        DeviceLinkTransformPlan plan = new DeviceLinkTransformPlan();
        plan.SourceChannels = sourceChannels;
        plan.DestinationChannels = destinationChannels;
        plan.SourceColorSpaceSignature = profile.Header.DataColorSpaceSignature;
        plan.DestinationColorSpaceSignature = profile.Header.ProfileConnectionSpaceSignature;
        plan.SourceRanges = ProfileTransformCompiler.CloneRanges(sourceRanges);
        plan.DestinationRanges = ProfileTransformCompiler.CloneRanges(destinationRanges);
        plan.Stages = stages;
        plan.Options = options;
        plan.ProfileSequence = ReadProfileSequence(profile, context);
        plan.ProfileSequenceIdentifiers = ReadProfileSequenceIdentifiers(profile, context);
        return plan;
    }

    internal static void ValidateRequest(IccProfile profile, IccTransformOptions options)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Tags == null
            || options == null)
        {
            throw new IccTransformException("A DeviceLink profile with header, tags, and transform options is required.");
        }
        if (profile.Header.ProfileClass != IccProfileClass.DeviceLink)
        {
            throw new IccTransformException("Direct DeviceLink application requires a DeviceLink profile class.");
        }
        if (profile.Header.Version.Kind != IccVersionKind.Version2
            && profile.Header.Version.Kind != IccVersionKind.Version4)
        {
            throw new IccTransformException("Direct DeviceLink application supports ICC version 2 and version 4 profiles.");
        }
        int sourceChannels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        int destinationChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (sourceChannels < 1 || destinationChannels < 1)
        {
            throw new IccTransformException("A DeviceLink header must declare supported input and output data colour spaces.");
        }
        if (options.RenderingIntent != IccRenderingIntent.Perceptual
            && options.RenderingIntent != IccRenderingIntent.MediaRelativeColorimetric
            && options.RenderingIntent != IccRenderingIntent.Saturation
            && options.RenderingIntent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            throw new IccTransformException("The requested DeviceLink rendering intent is invalid.");
        }
        if (options.IntentFallback != IccIntentFallback.None
            && options.IntentFallback != IccIntentFallback.AllowColorimetric
            && options.IntentFallback != IccIntentFallback.AllowAny)
        {
            throw new IccTransformException("The requested DeviceLink intent fallback policy is invalid.");
        }
        if (options.Interpolation != IccInterpolationKind.Linear
            && options.Interpolation != IccInterpolationKind.Multilinear
            && options.Interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The requested DeviceLink interpolation algorithm is invalid.");
        }
        if (options.BlackPointCompensation)
        {
            throw new IccTransformException("Black-point compensation cannot be inserted into a pre-evaluated DeviceLink transform.");
        }
    }

    internal static IccProfileSequenceEntry[] ReadProfileSequence(IccProfile profile, ProfileCloneContext context)
    {
        IccTag? tag = IntentSelector.FindTag(profile, IccTagSignatures.ProfileSequenceDescription);
        if (tag == null)
        {
            return new IccProfileSequenceEntry[0];
        }
        if (tag.Data == null || tag.Data.ProfileSequence == null)
        {
            throw new IccTransformException("The DeviceLink profile-sequence description tag has no decoded records.");
        }
        return ExtendedTagDataClone.CloneProfileSequenceEntryArray(tag.Data.ProfileSequence,
            "DeviceLink profile sequence", context);
    }

    internal static IccProfileSequenceIdentifier[] ReadProfileSequenceIdentifiers(IccProfile profile,
        ProfileCloneContext context)
    {
        IccTag? tag = IntentSelector.FindTag(profile, IccTagSignatures.ProfileSequenceIdentifier);
        if (tag == null)
        {
            return new IccProfileSequenceIdentifier[0];
        }
        if (tag.Data == null || tag.Data.ProfileSequenceIdentifiers == null)
        {
            throw new IccTransformException("The DeviceLink profile-sequence identifier tag has no decoded records.");
        }
        return ExtendedTagDataClone.CloneProfileSequenceIdentifierArray(tag.Data.ProfileSequenceIdentifiers,
            "DeviceLink profile sequence identifiers", context);
    }

    internal static void StampStages(PipelineStage[] stages, IntentSelection selection)
    {
        int index = 0;
        while (index < stages.Length)
        {
            stages[index].TagSignature = selection.TagSignature;
            string fallback = selection.UsedFallback ? " fallback" : string.Empty;
            stages[index].Description = $"DeviceLink {selection.SelectedIntent}{fallback}: {stages[index].Description}";
            index = index + 1;
        }
    }
}
