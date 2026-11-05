namespace icc;

internal class ProofingTransformPlan
{
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;
    internal PipelineStage[] GamutStages;
    internal IccTransformOptions TransformOptions;
    internal IccProofingOptions ProofingOptions;

    internal ProofingTransformPlan()
    {
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
        GamutStages = new PipelineStage[0];
        TransformOptions = new IccTransformOptions();
        ProofingOptions = new IccProofingOptions();
    }
}

internal static class ProofingTransformCompiler
{
    internal static IccColorTransform Compile(IccProfile sourceProfile, IccProfile proofProfile,
        IccProfile destinationProfile, IccProofingOptions requestedOptions)
    {
        ProofingTransformPlan plan = CompilePlan(sourceProfile, proofProfile, destinationProfile, requestedOptions);
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureProofingPlan(plan.Stages, plan.GamutStages, plan.SourceChannels, plan.DestinationChannels,
            plan.SourceRanges, plan.DestinationRanges, plan.TransformOptions, plan.ProofingOptions);
        return transform;
    }

    internal static ProofingTransformPlan CompilePlan(IccProfile sourceProfile, IccProfile proofProfile,
        IccProfile destinationProfile, IccProofingOptions requestedOptions)
    {
        if (requestedOptions == null)
        {
            throw new IccTransformException("Proofing options cannot be null.");
        }

        IccProfile[] profiles = new IccProfile[] { sourceProfile, proofProfile, destinationProfile };
        ProfileTransformCompiler.ValidateSequenceRequest(profiles, requestedOptions.RenderingIntent,
            requestedOptions.IntentFallback, requestedOptions.Interpolation);
        ValidateProofingIntent(requestedOptions.ProofingIntent);

        ProfileDirectionPlan source = ProfileTransformCompiler.CompileProfileDirection(sourceProfile,
            IccTransformDirection.DeviceToPcs, requestedOptions.RenderingIntent, requestedOptions.IntentFallback,
            requestedOptions.Interpolation, 0);
        ProfileDirectionPlan proofRendering = ProfileTransformCompiler.CompileProfileDirection(proofProfile,
            IccTransformDirection.PcsToDevice, requestedOptions.RenderingIntent, requestedOptions.IntentFallback,
            requestedOptions.Interpolation, 1);
        ProfileDirectionPlan proofSimulation = ProfileTransformCompiler.CompileProfileDirection(proofProfile,
            IccTransformDirection.DeviceToPcs, requestedOptions.ProofingIntent, requestedOptions.IntentFallback,
            requestedOptions.Interpolation, 1);
        ProfileDirectionPlan destination = ProfileTransformCompiler.CompileProfileDirection(destinationProfile,
            IccTransformDirection.PcsToDevice, requestedOptions.ProofingIntent, requestedOptions.IntentFallback,
            requestedOptions.Interpolation, 2);

        if (proofRendering.DestinationChannels != proofSimulation.SourceChannels)
        {
            throw new IccTransformException("The proof profile rendering and proofing directions do not share one device colour space.");
        }

        StampRole(source.Stages, "source");
        StampRole(proofRendering.Stages, "proof rendering");
        StampRole(proofSimulation.Stages, "proof simulation");
        StampRole(destination.Stages, "destination");
        PipelineStage[] sourceBridge = ProfileTransformCompiler.CreateConnectionBridge(source.PcsKind,
            proofRendering.PcsKind, source.VersionKind, proofRendering.VersionKind);
        PipelineStage[] destinationBridge = ProfileTransformCompiler.CreateConnectionBridge(proofSimulation.PcsKind,
            destination.PcsKind, proofSimulation.VersionKind, destination.VersionKind);
        StampRole(sourceBridge, "source-to-proof bridge");
        StampRole(destinationBridge, "proof-to-destination bridge");

        System.Collections.Generic.List<PipelineStage> stages = new System.Collections.Generic.List<PipelineStage>();
        ProfileTransformCompiler.AppendStages(stages, source.Stages);
        ProfileTransformCompiler.AppendStages(stages, sourceBridge);
        ProfileTransformCompiler.AppendStages(stages, proofRendering.Stages);
        ProfileTransformCompiler.AppendStages(stages, proofSimulation.Stages);
        ProfileTransformCompiler.AppendStages(stages, destinationBridge);
        ProfileTransformCompiler.AppendStages(stages, destination.Stages);
        PipelineStage[] compiledStages = stages.ToArray();
        PipelineExecutor.GetRequiredValueScratchLength(compiledStages);

        IccProofingOptions options = CopyProofingOptions(requestedOptions);
        ValidateAlarm(options, destination.DestinationChannels, destination.DestinationRanges);
        PipelineStage[] gamutStages = options.EnableGamutCheck
            ? CompileGamutStages(proofProfile, source, requestedOptions.Interpolation)
            : new PipelineStage[0];

        IccTransformOptions transformOptions = new IccTransformOptions();
        transformOptions.Direction = IccTransformDirection.DeviceToDevice;
        transformOptions.RenderingIntent = options.RenderingIntent;
        transformOptions.IntentFallback = options.IntentFallback;
        transformOptions.Interpolation = options.Interpolation;
        transformOptions.PcsEncoding = IccPcsEncodingKind.Normalized;
        transformOptions.BlackPointCompensation = false;
        transformOptions.ClipOutput = options.ClipOutput;
        transformOptions.EnableTrace = options.EnableTrace;

        ProofingTransformPlan plan = new ProofingTransformPlan();
        plan.SourceChannels = source.SourceChannels;
        plan.DestinationChannels = destination.DestinationChannels;
        plan.SourceRanges = ProfileTransformCompiler.CloneRanges(source.SourceRanges);
        plan.DestinationRanges = ProfileTransformCompiler.CloneRanges(destination.DestinationRanges);
        plan.Stages = compiledStages;
        plan.GamutStages = gamutStages;
        plan.TransformOptions = transformOptions;
        plan.ProofingOptions = options;
        return plan;
    }

    internal static PipelineStage[] CompileGamutStages(IccProfile proofProfile, ProfileDirectionPlan source,
        IccInterpolationKind interpolation)
    {
        IntentSelection selection = IntentSelector.SelectIcc1Transform(proofProfile,
            IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, true);
        if (!selection.Success || selection.Tag == null || selection.Tag.Data == null)
        {
            throw new IccTransformException($"Gamut checking requires an executable proof-profile gamut tag: {selection.Message}");
        }
        if (selection.Tag.Data.MultiProcessElements != null)
        {
            throw new IccTransformException("The ICC.1 proof-profile gamut tag must contain a LUT8, LUT16, or lutBToA pipeline.");
        }

        IccProfile gamutContext = CreateGamutContext(proofProfile);
        LutPipelinePlan gamut = LutPipelineCompiler.Compile(gamutContext, selection.Tag,
            IccTransformDirection.PcsToDevice, interpolation);
        if (gamut.SourceChannels != 3 || gamut.DestinationChannels != 1)
        {
            throw new IccTransformException("The proof-profile gamut tag must transform three PCS channels to one gamut channel.");
        }
        StampGamutStages(gamut.Stages, selection.TagSignature);
        PipelineStage[] bridge = ProfileTransformCompiler.CreateConnectionBridge(source.PcsKind,
            ProfileTransformCompiler.ResolvePcsKind(proofProfile), source.VersionKind, proofProfile.Header.Version.Kind);
        StampRole(bridge, "source-to-gamut bridge");
        System.Collections.Generic.List<PipelineStage> stages = new System.Collections.Generic.List<PipelineStage>();
        ProfileTransformCompiler.AppendStages(stages, source.Stages);
        ProfileTransformCompiler.AppendStages(stages, bridge);
        ProfileTransformCompiler.AppendStages(stages, gamut.Stages);
        PipelineStage[] result = stages.ToArray();
        PipelineExecutor.GetRequiredValueScratchLength(result);
        return result;
    }

    internal static IccProfile CreateGamutContext(IccProfile proofProfile)
    {
        IccProfile context = new IccProfile();
        context.Header.Version.Kind = proofProfile.Header.Version.Kind;
        context.Header.ProfileClass = proofProfile.Header.ProfileClass;
        context.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        context.Header.DataColorSpace = IccColorSpaceKind.Gray;
        context.Header.ProfileConnectionSpace = proofProfile.Header.ProfileConnectionSpace;
        context.Header.ProfileConnectionSpaceSignature = proofProfile.Header.ProfileConnectionSpaceSignature;
        return context;
    }

    internal static void ValidateProofingIntent(IccRenderingIntent intent)
    {
        if (intent != IccRenderingIntent.Perceptual && intent != IccRenderingIntent.MediaRelativeColorimetric
            && intent != IccRenderingIntent.Saturation && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            throw new IccTransformException("The proofing rendering intent is invalid.");
        }
    }

    internal static void ValidateAlarm(IccProofingOptions options, int destinationChannels, IccEncodingRange[] destinationRanges)
    {
        if (!options.EnableGamutCheck)
        {
            return;
        }
        if (options.AlarmColor == null || options.AlarmColor.Length != destinationChannels)
        {
            throw new IccTransformException($"Gamut checking requires an alarm colour with exactly {destinationChannels} destination channels.");
        }
        int index = 0;
        while (index < options.AlarmColor.Length)
        {
            if (!System.Double.IsFinite(options.AlarmColor[index]))
            {
                throw new IccTransformException($"Gamut alarm channel {index} must be finite.");
            }
            index = index + 1;
        }
        ProofingTransformExecutor.CreateAlarmFloats(options.AlarmColor, destinationRanges, options.ClipOutput);
    }

    internal static IccProofingOptions CopyProofingOptions(IccProofingOptions source)
    {
        IccProofingOptions copy = new IccProofingOptions();
        copy.RenderingIntent = source.RenderingIntent;
        copy.ProofingIntent = source.ProofingIntent;
        copy.IntentFallback = source.IntentFallback;
        copy.Interpolation = source.Interpolation;
        copy.EnableGamutCheck = source.EnableGamutCheck;
        copy.AlarmColor = CloneAlarm(source.AlarmColor);
        copy.ClipOutput = source.ClipOutput;
        copy.EnableTrace = source.EnableTrace;
        return copy;
    }

    internal static double[] CloneAlarm(double[] source)
    {
        if (source == null)
        {
            return new double[0];
        }
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    internal static void StampRole(PipelineStage[] stages, string role)
    {
        int index = 0;
        while (index < stages.Length)
        {
            stages[index].Description = $"{role}: {stages[index].Description}";
            index = index + 1;
        }
    }

    internal static void StampGamutStages(PipelineStage[] stages, uint tagSignature)
    {
        int index = 0;
        while (index < stages.Length)
        {
            stages[index].TagSignature = tagSignature;
            stages[index].Description = $"gamut check: {stages[index].Description}";
            index = index + 1;
        }
    }
}
