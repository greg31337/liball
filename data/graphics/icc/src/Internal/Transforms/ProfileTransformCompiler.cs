namespace icc;

internal class ProfileDirectionPlan
{
    internal IccPcsKind PcsKind;
    internal IccVersionKind VersionKind;
    internal IccRenderingIntent SelectedIntent;
    internal bool IsLutBased;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;

    internal ProfileDirectionPlan()
    {
        PcsKind = IccPcsKind.None;
        VersionKind = IccVersionKind.Unknown;
        SelectedIntent = IccRenderingIntent.Perceptual;
        IsLutBased = false;
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
    }
}

internal class MultiProfileTransformPlan
{
    internal int SourceChannels;
    internal int DestinationChannels;
    internal IccEncodingRange[] SourceRanges;
    internal IccEncodingRange[] DestinationRanges;
    internal PipelineStage[] Stages;
    internal IccTransformOptions Options;

    internal MultiProfileTransformPlan()
    {
        SourceChannels = 0;
        DestinationChannels = 0;
        SourceRanges = new IccEncodingRange[0];
        DestinationRanges = new IccEncodingRange[0];
        Stages = new PipelineStage[0];
        Options = new IccTransformOptions();
    }
}

internal static class ProfileTransformCompiler
{
    internal static IccColorTransform Compile(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback,
        IccInterpolationKind interpolation)
    {
        MultiProfileTransformPlan plan = CompilePlan(profiles, intent, fallback, interpolation);
        return CreateTransform(plan);
    }

    internal static IccColorTransform CompileWithOptions(IccProfile[] profiles, IccTransformOptions options)
    {
        MultiProfileTransformPlan plan = CompilePlanWithOptions(profiles, options);
        return CreateTransform(plan);
    }

    internal static IccColorTransform CreateTransform(MultiProfileTransformPlan plan)
    {
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureCompiledPlan(plan.Stages, plan.SourceChannels, plan.DestinationChannels, plan.SourceRanges,
            plan.DestinationRanges, plan.Options, 0);
        return transform;
    }

    internal static MultiProfileTransformPlan CompilePlan(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback,
        IccInterpolationKind interpolation)
    {
        IccTransformOptions options = new IccTransformOptions();
        options.Direction = IccTransformDirection.DeviceToDevice;
        options.RenderingIntent = intent;
        options.IntentFallback = fallback;
        options.Interpolation = interpolation;
        options.PcsEncoding = IccPcsEncodingKind.Normalized;
        options.BlackPointCompensation = false;
        options.ClipOutput = true;
        options.EnableTrace = false;
        return CompilePlanWithOptions(profiles, options);
    }

    internal static MultiProfileTransformPlan CompilePlanWithOptions(IccProfile[] profiles, IccTransformOptions requestedOptions)
    {
        if (requestedOptions == null)
        {
            throw new IccTransformException("Profile transform options cannot be null.");
        }

        ValidateSequenceRequest(profiles, requestedOptions.RenderingIntent, requestedOptions.IntentFallback, requestedOptions.Interpolation);
        IccTransformOptions options = CopySequenceOptions(requestedOptions);
        if (options.BlackPointCompensation)
        {
            BlackPointCompensation.ValidateSequence(profiles, options.RenderingIntent);
        }
        if (HasIntermediateAbstractProfile(profiles))
        {
            return CompileSequenceWithAbstractProfiles(profiles, options);
        }
        System.Collections.Generic.List<PipelineStage> stages = new System.Collections.Generic.List<PipelineStage>();
        ProfileDirectionPlan? first = null;
        ProfileDirectionPlan? last = null;
        int link = 0;
        while (link < profiles.Length - 1)
        {
            ProfileDirectionPlan source = CompileProfileDirection(profiles[link], IccTransformDirection.DeviceToPcs,
                options.RenderingIntent, options.IntentFallback, options.Interpolation, link);
            ProfileDirectionPlan destination = CompileProfileDirection(profiles[link + 1], IccTransformDirection.PcsToDevice,
                options.RenderingIntent, options.IntentFallback, options.Interpolation, link + 1);
            if (first == null)
            {
                first = source;
            }

            AppendStages(stages, source.Stages);
            if (options.BlackPointCompensation)
            {
                AppendStages(stages, BlackPointCompensation.CompileLink(profiles[link], profiles[link + 1], source, destination,
                    options.Interpolation, link));
            }
            else
            {
                AppendStages(stages, CreateConnectionBridge(source.PcsKind, destination.PcsKind, source.VersionKind, destination.VersionKind));
            }
            AppendStages(stages, destination.Stages);
            last = destination;
            link = link + 1;
        }

        if (first == null || last == null)
        {
            throw new IccTransformException("A profile transform requires at least one complete source-to-destination link.");
        }

        PipelineStage[] compiledStages = stages.ToArray();
        PipelineExecutor.GetRequiredValueScratchLength(compiledStages);
        MultiProfileTransformPlan plan = new MultiProfileTransformPlan();
        plan.SourceChannels = first.SourceChannels;
        plan.DestinationChannels = last.DestinationChannels;
        plan.SourceRanges = CloneRanges(first.SourceRanges);
        plan.DestinationRanges = CloneRanges(last.DestinationRanges);
        plan.Stages = compiledStages;
        plan.Options = options;
        return plan;
    }

    internal static bool HasIntermediateAbstractProfile(IccProfile[] profiles)
    {
        int index = 1;
        while (index < profiles.Length - 1)
        {
            if (profiles[index].Header.ProfileClass == IccProfileClass.Abstract)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static MultiProfileTransformPlan CompileSequenceWithAbstractProfiles(IccProfile[] profiles,
        IccTransformOptions options)
    {
        ProfileDirectionPlan first = CompileProfileDirection(profiles[0], IccTransformDirection.DeviceToPcs,
            options.RenderingIntent, options.IntentFallback, options.Interpolation, 0);
        ProfileDirectionPlan current = first;
        System.Collections.Generic.List<PipelineStage> stages = new System.Collections.Generic.List<PipelineStage>();
        AppendStages(stages, first.Stages);
        int profileIndex = 1;
        while (profileIndex < profiles.Length - 1)
        {
            IccProfile profile = profiles[profileIndex];
            if (profile.Header.ProfileClass == IccProfileClass.Abstract)
            {
                ProfileDirectionPlan abstractDirection = CompileProfileDirection(profile,
                    IccTransformDirection.DeviceToPcs, options.RenderingIntent, options.IntentFallback,
                    options.Interpolation, profileIndex);
                IccPcsKind abstractInput = ResolveAbstractInputPcsKind(profile);
                AppendStages(stages, CreateConnectionBridge(current.PcsKind, abstractInput,
                    current.VersionKind, abstractDirection.VersionKind));
                AppendStages(stages, abstractDirection.Stages);
                current = abstractDirection;
            }
            else
            {
                ProfileDirectionPlan reverse = CompileProfileDirection(profile,
                    IccTransformDirection.PcsToDevice, options.RenderingIntent, options.IntentFallback,
                    options.Interpolation, profileIndex);
                AppendStages(stages, CreateConnectionBridge(current.PcsKind, reverse.PcsKind,
                    current.VersionKind, reverse.VersionKind));
                AppendStages(stages, reverse.Stages);
                ProfileDirectionPlan forward = CompileProfileDirection(profile,
                    IccTransformDirection.DeviceToPcs, options.RenderingIntent, options.IntentFallback,
                    options.Interpolation, profileIndex);
                if (reverse.DestinationChannels != forward.SourceChannels)
                {
                    throw new IccTransformException($"Profile {profileIndex} does not preserve its device-space channel count between multiprofile links.");
                }
                AppendStages(stages, forward.Stages);
                current = forward;
            }
            profileIndex = profileIndex + 1;
        }

        int destinationIndex = profiles.Length - 1;
        ProfileDirectionPlan destination = CompileProfileDirection(profiles[destinationIndex],
            IccTransformDirection.PcsToDevice, options.RenderingIntent, options.IntentFallback,
            options.Interpolation, destinationIndex);
        AppendStages(stages, CreateConnectionBridge(current.PcsKind, destination.PcsKind,
            current.VersionKind, destination.VersionKind));
        AppendStages(stages, destination.Stages);
        PipelineStage[] compiledStages = stages.ToArray();
        PipelineExecutor.GetRequiredValueScratchLength(compiledStages);
        MultiProfileTransformPlan plan = new MultiProfileTransformPlan();
        plan.SourceChannels = first.SourceChannels;
        plan.DestinationChannels = destination.DestinationChannels;
        plan.SourceRanges = CloneRanges(first.SourceRanges);
        plan.DestinationRanges = CloneRanges(destination.DestinationRanges);
        plan.Stages = compiledStages;
        plan.Options = options;
        return plan;
    }

    internal static IccPcsKind ResolveAbstractInputPcsKind(IccProfile profile)
    {
        if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Xyz)
        {
            return IccPcsKind.Xyz;
        }
        if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Lab)
        {
            return IccPcsKind.Lab;
        }
        throw new IccTransformException("An intermediate abstract profile requires PCSXYZ or PCSLAB input data.");
    }

    internal static ProfileDirectionPlan CompileProfileDirection(IccProfile profile, IccTransformDirection direction,
        IccRenderingIntent intent, IccIntentFallback fallback, IccInterpolationKind interpolation, int profileIndex)
    {
        IntentSelection selection = profile.Header.Version.Kind == IccVersionKind.Version5
            ? IntentSelector.SelectVersionFiveTransform(profile, direction, intent, fallback)
            : IntentSelector.SelectIcc1Transform(profile, direction, intent, fallback, false);
        if (!selection.Success)
        {
            throw new IccTransformException($"Profile {profileIndex} cannot provide its {direction} link: {selection.Message}");
        }

        ProfileDirectionPlan plan = new ProfileDirectionPlan();
        plan.PcsKind = ResolvePcsKind(profile);
        plan.VersionKind = profile.Header.Version.Kind;
        plan.SelectedIntent = selection.SelectedIntent;
        plan.IsLutBased = !selection.UsesMatrixTrc;
        if (selection.UsesMatrixTrc)
        {
            PopulateMatrixPlan(plan, MatrixTrcCompiler.Compile(profile, direction), direction);
        }
        else
        {
            IccTag? selectedTag = selection.Tag;
            if (selectedTag == null || selectedTag.Data == null)
            {
                throw new IccTransformException($"Profile {profileIndex} selected a transform tag without decoded data.");
            }
            if (selectedTag.Data.MultiProcessElements != null)
            {
                PopulateMpePlan(plan, MpePipelineCompiler.Compile(profile, selectedTag, direction, interpolation));
            }
            else
            {
                PopulateLutPlan(plan, LutPipelineCompiler.Compile(profile, selectedTag, direction, interpolation));
            }
        }

        plan.Stages = AddAbsoluteStages(profile, selection, plan.Stages, plan.PcsKind);
        StampStages(plan.Stages, selection.TagSignature, profileIndex, direction, selection.SelectedIntent, selection.UsedFallback);
        return plan;
    }

    internal static IccTransformOptions CopySequenceOptions(IccTransformOptions source)
    {
        IccTransformOptions copy = new IccTransformOptions();
        copy.Direction = IccTransformDirection.DeviceToDevice;
        copy.RenderingIntent = source.RenderingIntent;
        copy.IntentFallback = source.IntentFallback;
        copy.Interpolation = source.Interpolation;
        copy.PcsEncoding = IccPcsEncodingKind.Normalized;
        copy.BlackPointCompensation = source.BlackPointCompensation;
        copy.ClipOutput = source.ClipOutput;
        copy.EnableTrace = source.EnableTrace;
        return copy;
    }

    internal static void PopulateMatrixPlan(ProfileDirectionPlan target, MatrixTrcPlan source, IccTransformDirection direction)
    {
        target.SourceChannels = source.SourceChannels;
        target.DestinationChannels = source.DestinationChannels;
        target.SourceRanges = direction == IccTransformDirection.DeviceToPcs
            ? LutPipelineCompiler.CreateNormalizedRanges(source.SourceChannels)
            : LutPipelineCompiler.CreatePcsRanges(IccPcsKind.Xyz, IccPcsEncodingKind.Version4);
        target.DestinationRanges = direction == IccTransformDirection.DeviceToPcs
            ? LutPipelineCompiler.CreatePcsRanges(IccPcsKind.Xyz, IccPcsEncodingKind.Version4)
            : LutPipelineCompiler.CreateNormalizedRanges(source.DestinationChannels);
        target.Stages = source.Stages;
    }

    internal static void PopulateLutPlan(ProfileDirectionPlan target, LutPipelinePlan source)
    {
        target.SourceChannels = source.SourceChannels;
        target.DestinationChannels = source.DestinationChannels;
        target.SourceRanges = source.SourceRanges;
        target.DestinationRanges = source.DestinationRanges;
        target.Stages = source.Stages;
    }

    internal static void PopulateMpePlan(ProfileDirectionPlan target, MpePipelinePlan source)
    {
        target.SourceChannels = source.SourceChannels;
        target.DestinationChannels = source.DestinationChannels;
        target.SourceRanges = source.SourceRanges;
        target.DestinationRanges = source.DestinationRanges;
        target.Stages = source.Stages;
    }

    internal static PipelineStage[] AddAbsoluteStages(IccProfile profile, IntentSelection selection, PipelineStage[] selectedStages, IccPcsKind pcsKind)
    {
        if (!selection.ConvertAbsoluteBefore && !selection.ConvertAbsoluteAfter)
        {
            return selectedStages;
        }

        PipelineStage[] absoluteStages = CreateAbsoluteConversionStages(profile, pcsKind, selection.ConvertAbsoluteAfter);
        PipelineStage[] result = new PipelineStage[selectedStages.Length + absoluteStages.Length];
        if (selection.ConvertAbsoluteBefore)
        {
            CopyStageReferences(absoluteStages, result, 0);
            CopyStageReferences(selectedStages, result, absoluteStages.Length);
        }
        else
        {
            CopyStageReferences(selectedStages, result, 0);
            CopyStageReferences(absoluteStages, result, selectedStages.Length);
        }

        return result;
    }

    internal static PipelineStage[] CreateAbsoluteConversionStages(IccProfile profile, IccPcsKind pcsKind, bool toAbsolute)
    {
        double[] mediaWhite = ReadMediaWhite(profile);
        double[] scales = new double[]
        {
            toAbsolute ? mediaWhite[0] / IccStandardColorimetry.D50X : IccStandardColorimetry.D50X / mediaWhite[0],
            toAbsolute ? mediaWhite[1] / IccStandardColorimetry.D50Y : IccStandardColorimetry.D50Y / mediaWhite[1],
            toAbsolute ? mediaWhite[2] / IccStandardColorimetry.D50Z : IccStandardColorimetry.D50Z / mediaWhite[2]
        };
        PipelineStage scale = MatrixTrcCompiler.CreateMatrixStage(new double[]
        {
            scales[0], 0.0, 0.0,
            0.0, scales[1], 0.0,
            0.0, 0.0, scales[2]
        }, 3, 3, toAbsolute ? "convert media-relative PCS to ICC-absolute PCS" : "convert ICC-absolute PCS to media-relative PCS");
        if (pcsKind == IccPcsKind.Xyz)
        {
            return new PipelineStage[] { scale };
        }

        PipelineStage labToXyz = CreatePcsBridgeStage(PipelineStageKind.PcsLabToXyz, "convert D50 PCSLAB to PCSXYZ for absolute conversion");
        PipelineStage xyzToLab = CreatePcsBridgeStage(PipelineStageKind.PcsXyzToLab, "convert PCSXYZ to D50 PCSLAB after absolute conversion");
        return new PipelineStage[] { labToXyz, scale, xyzToLab };
    }

    internal static double[] ReadMediaWhite(IccProfile profile)
    {
        IccTag? tag = IntentSelector.FindTag(profile, IccTagSignatures.MediaWhitePoint);
        if (tag == null || tag.Data == null || tag.Data.XyzValues == null || tag.Data.XyzValues.Length != 1 || tag.Data.XyzValues[0] == null)
        {
            throw new IccTransformException("ICC-absolute synthesis requires one decoded media-white-point XYZ value.");
        }

        IccXyzNumber white = tag.Data.XyzValues[0];
        if (!System.Double.IsFinite(white.X) || !System.Double.IsFinite(white.Y) || !System.Double.IsFinite(white.Z)
            || white.X <= 0.0 || white.Y <= 0.0 || white.Z <= 0.0)
        {
            throw new IccTransformException("The media-white-point XYZ components must be finite and positive.");
        }

        return new double[] { white.X, white.Y, white.Z };
    }

    internal static PipelineStage[] CreateConnectionBridge(IccPcsKind sourcePcs, IccPcsKind destinationPcs,
        IccVersionKind sourceVersion, IccVersionKind destinationVersion)
    {
        int count = (sourcePcs == destinationPcs ? 0 : 1) + (sourceVersion == destinationVersion ? 0 : 1);
        PipelineStage[] stages = new PipelineStage[count];
        int index = 0;
        if (sourcePcs != destinationPcs)
        {
            stages[index] = sourcePcs == IccPcsKind.Lab
                ? CreatePcsBridgeStage(PipelineStageKind.PcsLabToXyz, "bridge D50 PCSLAB to PCSXYZ")
                : CreatePcsBridgeStage(PipelineStageKind.PcsXyzToLab, "bridge PCSXYZ to D50 PCSLAB");
            index = index + 1;
        }
        if (sourceVersion != destinationVersion)
        {
            PipelineStage versionBridge = new PipelineStage();
            versionBridge.Kind = PipelineStageKind.Identity;
            versionBridge.InputChannels = 3;
            versionBridge.OutputChannels = 3;
            versionBridge.Description = $"bridge ICC version {(int)sourceVersion} PCS values to version {(int)destinationVersion} PCS values";
            stages[index] = versionBridge;
        }

        return stages;
    }

    internal static PipelineStage CreatePcsBridgeStage(PipelineStageKind kind, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = kind;
        stage.InputChannels = 3;
        stage.OutputChannels = 3;
        stage.Description = description;
        return stage;
    }

    internal static IccPcsKind ResolvePcsKind(IccProfile profile)
    {
        IccPcsKind kind = profile.Header.ProfileConnectionSpace;
        uint signature = profile.Header.ProfileConnectionSpaceSignature;
        if ((kind == IccPcsKind.Xyz || kind == IccPcsKind.None) && (signature == IccColorSpaceSignatures.Xyz || signature == 0)
            && (kind == IccPcsKind.Xyz || signature == IccColorSpaceSignatures.Xyz))
        {
            return IccPcsKind.Xyz;
        }
        if ((kind == IccPcsKind.Lab || kind == IccPcsKind.None) && (signature == IccColorSpaceSignatures.Lab || signature == 0)
            && (kind == IccPcsKind.Lab || signature == IccColorSpaceSignatures.Lab))
        {
            return IccPcsKind.Lab;
        }

        throw new IccTransformException("A standard profile link requires a consistent PCSXYZ or PCSLAB connection space.");
    }

    internal static void StampStages(PipelineStage[] stages, uint tagSignature, int profileIndex, IccTransformDirection direction,
        IccRenderingIntent selectedIntent, bool usedFallback)
    {
        int index = 0;
        while (index < stages.Length)
        {
            PipelineStage stage = stages[index];
            stage.TagSignature = tagSignature;
            string fallback = usedFallback ? " fallback" : string.Empty;
            stage.Description = $"profile {profileIndex} {direction} {selectedIntent}{fallback}: {stage.Description}";
            index = index + 1;
        }
    }

    internal static void AppendStages(System.Collections.Generic.List<PipelineStage> destination, PipelineStage[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CopyStageReferences(PipelineStage[] source, PipelineStage[] destination, int destinationOffset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationOffset + index] = source[index];
            index = index + 1;
        }
    }

    internal static IccEncodingRange[] CloneRanges(IccEncodingRange[] source)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = source[index].Minimum;
            range.Maximum = source[index].Maximum;
            ranges[index] = range;
            index = index + 1;
        }
        return ranges;
    }

    internal static void ValidateSequenceRequest(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback,
        IccInterpolationKind interpolation)
    {
        if (profiles == null || profiles.Length < 2)
        {
            throw new IccTransformException("A standard profile sequence requires at least a source and destination profile.");
        }
        if (intent != IccRenderingIntent.Perceptual && intent != IccRenderingIntent.MediaRelativeColorimetric
            && intent != IccRenderingIntent.Saturation && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            throw new IccTransformException("The profile sequence rendering intent is invalid.");
        }
        if (fallback != IccIntentFallback.None && fallback != IccIntentFallback.AllowColorimetric && fallback != IccIntentFallback.AllowAny)
        {
            throw new IccTransformException("The profile sequence intent fallback policy is invalid.");
        }
        if (interpolation != IccInterpolationKind.Linear && interpolation != IccInterpolationKind.Multilinear
            && interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccTransformException("The profile sequence interpolation mode is invalid.");
        }

        int index = 0;
        while (index < profiles.Length)
        {
            IccProfile? profile = profiles[index];
            if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Tags == null)
            {
                throw new IccTransformException($"Profile {index} is missing its header, version, or tag collection.");
            }
            if (profile.Header.Version.Kind != IccVersionKind.Version2
                && profile.Header.Version.Kind != IccVersionKind.Version4
                && profile.Header.Version.Kind != IccVersionKind.Version5)
            {
                throw new IccTransformException($"Profile {index} is not an ICC version 2, version 4, or version 5 standard profile.");
            }
            ResolvePcsKind(profile);
            index = index + 1;
        }
    }

    internal static IccOperationResult ProbePair(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent)
    {
        if (!HasBasicProfileData(sourceProfile) || !HasBasicProfileData(destinationProfile))
        {
            return CreateProbeResult(IccOperationStatus.InvalidData, "Profile transform probing requires two profiles with headers, versions, and tag collections.");
        }
        if (intent != IccRenderingIntent.Perceptual && intent != IccRenderingIntent.MediaRelativeColorimetric
            && intent != IccRenderingIntent.Saturation && intent != IccRenderingIntent.IccAbsoluteColorimetric)
        {
            return CreateProbeResult(IccOperationStatus.InvalidData, "The requested rendering intent is invalid.");
        }
        if (!HasSupportedVersion(sourceProfile) || !HasSupportedVersion(destinationProfile))
        {
            return CreateProbeResult(IccOperationStatus.UnsupportedFeature, "Standard profile chaining supports ICC version 2, version 4, and version 5 profiles.");
        }
        if (!HasConnectablePcs(sourceProfile) || !HasConnectablePcs(destinationProfile))
        {
            return CreateProbeResult(IccOperationStatus.Conflict, "The profiles do not declare connectable PCSXYZ or PCSLAB spaces.");
        }

        IntentSelection source = SelectProfileTransform(sourceProfile, IccTransformDirection.DeviceToPcs, intent);
        if (!source.Success)
        {
            return CreateProbeResult(IccOperationStatus.NotFound, $"The source profile has no requested transform: {source.Message}");
        }
        IntentSelection destination = SelectProfileTransform(destinationProfile, IccTransformDirection.PcsToDevice, intent);
        if (!destination.Success)
        {
            return CreateProbeResult(IccOperationStatus.NotFound, $"The destination profile has no requested transform: {destination.Message}");
        }

        return CreateProbeResult(IccOperationStatus.Success, "The profiles provide a connectable transform for the requested intent.");
    }

    internal static bool HasBasicProfileData(IccProfile profile)
    {
        return profile != null && profile.Header != null && profile.Header.Version != null && profile.Tags != null;
    }

    internal static bool HasSupportedVersion(IccProfile profile)
    {
        return profile.Header.Version.Kind == IccVersionKind.Version2
            || profile.Header.Version.Kind == IccVersionKind.Version4
            || profile.Header.Version.Kind == IccVersionKind.Version5;
    }

    internal static IntentSelection SelectProfileTransform(IccProfile profile,
        IccTransformDirection direction, IccRenderingIntent intent)
    {
        return profile.Header.Version.Kind == IccVersionKind.Version5
            ? IntentSelector.SelectVersionFiveTransform(profile, direction, intent, IccIntentFallback.None)
            : IntentSelector.SelectIcc1Transform(profile, direction, intent, IccIntentFallback.None, false);
    }

    internal static bool HasConnectablePcs(IccProfile profile)
    {
        IccPcsKind kind = profile.Header.ProfileConnectionSpace;
        uint signature = profile.Header.ProfileConnectionSpaceSignature;
        bool xyz = (kind == IccPcsKind.Xyz || kind == IccPcsKind.None) && (signature == IccColorSpaceSignatures.Xyz || signature == 0)
            && (kind == IccPcsKind.Xyz || signature == IccColorSpaceSignatures.Xyz);
        bool lab = (kind == IccPcsKind.Lab || kind == IccPcsKind.None) && (signature == IccColorSpaceSignatures.Lab || signature == 0)
            && (kind == IccPcsKind.Lab || signature == IccColorSpaceSignatures.Lab);
        return xyz || lab;
    }

    internal static IccOperationResult CreateProbeResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
