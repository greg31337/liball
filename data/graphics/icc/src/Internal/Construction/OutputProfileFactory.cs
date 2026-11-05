namespace icc;

internal class OutputProfileFactory
{
    internal ProfileConstructionCore Core;

    internal OutputProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateOutputProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        IccOutputProfileDefinition definition = ValidateOutputOptions(options);
        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        AddIntentTags(tags, definition.ForwardTransforms, true);
        AddIntentTags(tags, definition.BackwardTransforms, false);
        tags.Add(CreateTransformTag(IccTagSignatures.Gamut,
            definition.GamutTransform, false));
        if (definition.Colorants != null)
        {
            tags.Add(CreateColorantTag(definition.Colorants));
        }
        if (definition.Measurement != null)
        {
            tags.Add(CreateMeasurementTag(definition.Measurement));
        }
        if (definition.ViewingConditions != null)
        {
            tags.Add(CreateViewingConditionsTag(definition.ViewingConditions));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        IccProfile result = Core.SerializeProfile(profile, false);
        ValidateExecutableTransforms(result);
        return Core.ValidateConformingProfile(result);
    }

    internal IccOutputProfileDefinition ValidateOutputOptions(
        IccProfileBuildOptions options)
    {
        IccOutputProfileDefinition? candidate = options.OutputDefinition;
        if (candidate == null)
        {
            throw new IccProfileValidationException("Output profile construction requires an Output profile definition.");
        }
        if (options.ProfileClass != IccProfileClass.Output)
        {
            throw new IccProfileValidationException("An Output profile must use the Output profile class.");
        }
        if (options.DataColorSpaceSignature == IccColorSpaceSignatures.Gray)
        {
            throw new IccProfileValidationException("A gray Output profile must be created with the monochrome profile builder.");
        }
        if (options.AdaptiveGainCurve != null)
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata is not permitted in an Output profile.");
        }

        IccOutputProfileDefinition definition = candidate;
        ValidateIntentTransforms(definition.ForwardTransforms, true,
            options.Version.Kind);
        ValidateIntentTransforms(definition.BackwardTransforms, false,
            options.Version.Kind);
        ValidateTransform(definition.GamutTransform, options.Version.Kind,
            "gamut");
        if (definition.Colorants == null)
        {
            if (Icc1ConformanceValidationCore.IsXclr(
                options.DataColorSpaceSignature))
            {
                throw new IccProfileValidationException("An xCLR Output profile requires a device-space colorant table.");
            }
        }
        else
        {
            if (definition.Colorants.ColorSpaceSignature
                != options.DataColorSpaceSignature)
            {
                throw new IccProfileValidationException("The Output colorant table must describe the profile data space.");
            }
            ColorantTableTagCodec.SerializeColorantTable(definition.Colorants,
                options.Version.Kind, HeaderCodec.DecodePcs(
                    options.ProfileConnectionSpaceSignature), false);
        }
        if (definition.Measurement != null)
        {
            MeasurementTagCodec.SerializeMeasurement(definition.Measurement);
        }
        if (definition.ViewingConditions != null)
        {
            ViewingConditionsTagCodec.SerializeViewingConditions(
                definition.ViewingConditions);
        }
        return definition;
    }

    internal void ValidateIntentTransforms(
        IccOutputTransformDefinition[] transforms, bool forward,
        IccVersionKind version)
    {
        if (transforms == null || transforms.Length != 3)
        {
            throw new IccProfileValidationException("An Output profile requires exactly three forward and three backward intent transforms.");
        }

        int index = 0;
        while (index < transforms.Length)
        {
            string direction = forward ? "forward" : "backward";
            ValidateTransform(transforms[index], version,
                $"{direction} intent {index}");
            index = index + 1;
        }
    }

    internal void ValidateTransform(IccOutputTransformDefinition? transform,
        IccVersionKind version, string description)
    {
        if (transform == null)
        {
            throw new IccProfileValidationException($"The Output {description} transform is missing.");
        }
        int count = transform.Lut == null ? 0 : 1;
        if (transform.MultiStageLut != null)
        {
            count = count + 1;
        }
        if (count != 1)
        {
            throw new IccProfileValidationException($"The Output {description} transform requires exactly one classic or multi-stage LUT.");
        }
        if (transform.Lut != null
            && transform.Lut.PcsEncoding != IccPcsEncodingKind.Normalized
            && transform.Lut.PcsEncoding != IccPcsEncodingKind.Version2)
        {
            throw new IccProfileValidationException($"The Output {description} classic LUT must select LUT8 normalized or LUT16 version-2 PCS encoding.");
        }
        if (transform.MultiStageLut != null
            && (version != IccVersionKind.Version4
                || transform.MultiStageLut.PcsEncoding
                    != IccPcsEncodingKind.Version4))
        {
            throw new IccProfileValidationException($"The Output {description} multi-stage LUT requires ICC version 4 PCS encoding.");
        }
    }

    internal void AddIntentTags(System.Collections.Generic.List<IccTag> tags,
        IccOutputTransformDefinition[] transforms, bool forward)
    {
        uint[] signatures = forward
            ? new uint[]
            {
                IccTagSignatures.AToB0,
                IccTagSignatures.AToB1,
                IccTagSignatures.AToB2
            }
            : new uint[]
            {
                IccTagSignatures.BToA0,
                IccTagSignatures.BToA1,
                IccTagSignatures.BToA2
            };
        int index = 0;
        while (index < signatures.Length)
        {
            tags.Add(CreateTransformTag(signatures[index], transforms[index],
                forward));
            index = index + 1;
        }
    }

    internal IccTag CreateTransformTag(uint signature,
        IccOutputTransformDefinition? transform, bool forward)
    {
        if (transform == null)
        {
            throw new IccProfileValidationException("The Output transform is missing.");
        }
        ProfileCloneContext context = new ProfileCloneContext();
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.RawData.HasTypeSignature = true;
        if (transform.Lut != null)
        {
            data.Lut = ExtendedTagDataClone.CloneLut(transform.Lut, context);
            data.RawData.TypeSignature = transform.Lut.PcsEncoding
                == IccPcsEncodingKind.Version2
                ? IccTypeSignatures.Lut16 : IccTypeSignatures.Lut8;
        }
        else
        {
            IccMultiStageLut? source = transform.MultiStageLut;
            if (source == null)
            {
                throw new IccProfileValidationException("The Output multi-stage transform is missing.");
            }
            data.MultiStageLut = ExtendedTagDataClone.CloneMultiStageLut(
                source, context);
            data.RawData.TypeSignature = forward
                ? IccTypeSignatures.LutAToB : IccTypeSignatures.LutBToA;
        }
        return Core.CreateTag(signature, data);
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

    internal IccTag CreateMeasurementTag(IccMeasurement source)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Measurement;
        data.Measurement = ExtendedTagDataClone.CloneMeasurement(source);
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Measurement;
        return Core.CreateTag(IccTagSignatures.Measurement, data);
    }

    internal IccTag CreateViewingConditionsTag(IccViewingConditions source)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.ViewingConditions;
        data.ViewingConditions = ExtendedTagDataClone.CloneViewingConditions(
            source);
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.ViewingConditions;
        return Core.CreateTag(IccTagSignatures.ViewingConditions, data);
    }

    internal void ValidateExecutableTransforms(IccProfile profile)
    {
        uint[] forward = new uint[]
        {
            IccTagSignatures.AToB0,
            IccTagSignatures.AToB1,
            IccTagSignatures.AToB2
        };
        uint[] backward = new uint[]
        {
            IccTagSignatures.BToA0,
            IccTagSignatures.BToA1,
            IccTagSignatures.BToA2
        };
        int index = 0;
        while (index < forward.Length)
        {
            LutPipelineCompiler.Compile(profile, FindTag(profile, forward[index]),
                IccTransformDirection.DeviceToPcs,
                IccInterpolationKind.Multilinear);
            LutPipelineCompiler.Compile(profile, FindTag(profile, backward[index]),
                IccTransformDirection.PcsToDevice,
                IccInterpolationKind.Multilinear);
            index = index + 1;
        }
        IccOperationResult gamut = new GamutAnalysisCore().Probe(profile);
        if (gamut.Status != IccOperationStatus.Success)
        {
            throw new IccTransformException(gamut.Message);
        }
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
        throw new IccProfileValidationException($"The constructed Output profile is missing tag '{FourCcCodec.Format(signature)}'.");
    }
}
