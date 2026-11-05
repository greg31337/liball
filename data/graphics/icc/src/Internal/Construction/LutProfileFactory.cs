namespace icc;

internal class LutProfileFactory
{
    internal ProfileConstructionCore Core;

    internal LutProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateAbstractProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        IccAbstractProfileDefinition definition = ValidateAbstractOptions(options);
        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        tags.Add(CreateLegacyTransformTag(definition));
        if (definition.MultiProcessElements != null)
        {
            tags.Add(CreateMpeTransformTag(definition.MultiProcessElements));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        IccProfile result = Core.SerializeProfile(profile, false);
        IccTag legacy = FindTag(result, IccTagSignatures.AToB0);
        LutPipelineCompiler.Compile(result, legacy,
            IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        if (definition.MultiProcessElements != null)
        {
            IccTag mpe = FindTag(result, IccTagSignatures.DToB0);
            MpePipelineCompiler.Compile(result, mpe,
                IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        }
        return Core.ValidateConformingProfile(result);
    }

    internal IccAbstractProfileDefinition ValidateAbstractOptions(
        IccProfileBuildOptions options)
    {
        IccAbstractProfileDefinition? candidate = options.AbstractDefinition;
        if (candidate == null)
        {
            throw new IccProfileValidationException("Abstract profile construction requires an abstract transform definition.");
        }
        if (options.ProfileClass != IccProfileClass.Abstract)
        {
            throw new IccProfileValidationException("An abstract profile must use the Abstract profile class.");
        }
        if ((options.DataColorSpaceSignature != IccColorSpaceSignatures.Xyz
                && options.DataColorSpaceSignature != IccColorSpaceSignatures.Lab)
            || options.DataColorSpaceSignature
                != options.ProfileConnectionSpaceSignature)
        {
            throw new IccProfileValidationException("An abstract profile requires matching XYZ-to-PCSXYZ or Lab-to-PCSLAB spaces.");
        }
        if (!Core.IsD50(options.MediaWhitePoint))
        {
            throw new IccProfileValidationException("An abstract profile media white point must be PCS D50.");
        }
        if (options.AdaptiveGainCurve != null)
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata is not permitted in an Abstract profile.");
        }

        IccAbstractProfileDefinition definition = candidate;
        int fallbackCount = definition.Lut == null ? 0 : 1;
        if (definition.MultiStageLut != null)
        {
            fallbackCount = fallbackCount + 1;
        }
        if (fallbackCount != 1)
        {
            throw new IccProfileValidationException("An abstract profile requires exactly one classic LUT or lutAToB fallback transform.");
        }
        if (definition.Lut != null
            && definition.Lut.PcsEncoding != IccPcsEncodingKind.Normalized
            && definition.Lut.PcsEncoding != IccPcsEncodingKind.Version2)
        {
            throw new IccProfileValidationException("A classic abstract-profile LUT must select LUT8 normalized or LUT16 version-2 PCS encoding.");
        }
        if (definition.MultiStageLut != null
            && (options.Version.Kind != IccVersionKind.Version4
                || definition.MultiStageLut.PcsEncoding
                    != IccPcsEncodingKind.Version4))
        {
            throw new IccProfileValidationException("A lutAToB abstract transform requires ICC version 4 PCS encoding.");
        }
        if (definition.MultiProcessElements != null
            && options.Version.Kind != IccVersionKind.Version4)
        {
            throw new IccProfileValidationException("An abstract multi-process-elements transform requires ICC version 4.");
        }
        return definition;
    }

    internal IccTag CreateLegacyTransformTag(IccAbstractProfileDefinition definition)
    {
        ProfileCloneContext context = new ProfileCloneContext();
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.RawData.HasTypeSignature = true;
        if (definition.Lut != null)
        {
            data.Lut = ExtendedTagDataClone.CloneLut(definition.Lut, context);
            data.RawData.TypeSignature = definition.Lut.PcsEncoding
                == IccPcsEncodingKind.Version2
                ? IccTypeSignatures.Lut16 : IccTypeSignatures.Lut8;
        }
        else
        {
            IccMultiStageLut? source = definition.MultiStageLut;
            if (source == null)
            {
                throw new IccProfileValidationException("The abstract lutAToB fallback transform is missing.");
            }
            data.MultiStageLut = ExtendedTagDataClone.CloneMultiStageLut(
                source, context);
            data.RawData.TypeSignature = IccTypeSignatures.LutAToB;
        }
        return Core.CreateTag(IccTagSignatures.AToB0, data);
    }

    internal IccTag CreateMpeTransformTag(IccMultiProcessElements source)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = ExtendedTagDataClone.CloneMultiProcessElements(
            source, new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return Core.CreateTag(IccTagSignatures.DToB0, data);
    }

    internal IccTag FindTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            if (profile.Tags[index].Signature == signature)
            {
                return profile.Tags[index];
            }
            index = index + 1;
        }
        throw new IccProfileValidationException($"The constructed profile is missing tag '{FourCcCodec.Format(signature)}'.");
    }
}
