namespace icc;

internal class GrayIdentityProfileFactory
{
    internal ProfileConstructionCore Core;

    internal GrayIdentityProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateGrayProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        IccGrayProfileDefinition definition = ValidateGrayOptions(options);
        MatrixProfileFactory matrixFactory = new MatrixProfileFactory(Core);
        double[] sourceWhite = matrixFactory.CreateNormalizedWhite(definition.WhitePoint);
        double[] adaptation = matrixFactory.CreateAdaptationMatrix(options, sourceWhite);

        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        matrixFactory.AddGeneratedAdaptation(tags, options, definition.WhitePoint, adaptation);
        tags.Add(matrixFactory.CreateCurveTag(IccTagSignatures.GrayTrc,
            definition.GrayCurve, options.Version.Kind));
        if (options.ProfileClass == IccProfileClass.Display
            && definition.WhitePoint.Luminance > 0.0)
        {
            tags.Add(matrixFactory.CreateXyzTag(IccTagSignatures.Luminance,
                0.0, definition.WhitePoint.Luminance, 0.0));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        return Core.PreflightProfile(profile);
    }

    internal IccProfile CreateXyzIdentityProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        ValidateIdentityOptions(options);
        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        tags.Add(CreateIdentityTransformTag(IccTagSignatures.AToB0,
            true, options.Version.Kind));
        if (options.ProfileClass == IccProfileClass.Display
            || options.ProfileClass == IccProfileClass.ColorSpace)
        {
            tags.Add(CreateIdentityTransformTag(IccTagSignatures.BToA0,
                false, options.Version.Kind));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        return Core.PreflightProfile(profile);
    }

    internal IccGrayProfileDefinition ValidateGrayOptions(IccProfileBuildOptions options)
    {
        IccGrayProfileDefinition? candidate = options.GrayDefinition;
        if (candidate == null)
        {
            throw new IccProfileValidationException("Gray profile construction requires a gray colourimetric definition.");
        }
        if (options.ProfileClass != IccProfileClass.Input
            && options.ProfileClass != IccProfileClass.Display
            && options.ProfileClass != IccProfileClass.Output)
        {
            throw new IccProfileValidationException("A gray TRC profile must use the Input, Display, or Output class.");
        }
        if (options.DataColorSpaceSignature != IccColorSpaceSignatures.Gray
            || options.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Xyz)
        {
            throw new IccProfileValidationException("A gray TRC profile requires the Gray data space and PCSXYZ.");
        }
        if (!Core.IsD50(options.MediaWhitePoint))
        {
            throw new IccProfileValidationException("A gray TRC profile media white point must be PCS D50.");
        }
        if (options.AdaptiveGainCurve != null)
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata is permitted only for RGB Input or Display profiles.");
        }

        IccGrayProfileDefinition definition = candidate;
        MatrixProfileFactory matrixFactory = new MatrixProfileFactory(Core);
        matrixFactory.ValidateChromaticity(definition.WhitePoint,
            "gray source white point", true);
        matrixFactory.ValidateCurve(definition.GrayCurve, "gray",
            options.Version.Kind);
        if (options.ProfileClass == IccProfileClass.Display
            && definition.WhitePoint.Luminance > IccNumberCodec.S15Fixed16Maximum)
        {
            throw new IccProfileValidationException("Gray display luminance must fit the ICC XYZ signed 15.16 representation.");
        }
        return definition;
    }

    internal void ValidateIdentityOptions(IccProfileBuildOptions options)
    {
        if (options.ProfileClass != IccProfileClass.Input
            && options.ProfileClass != IccProfileClass.Display
            && options.ProfileClass != IccProfileClass.ColorSpace
            && options.ProfileClass != IccProfileClass.Abstract)
        {
            throw new IccProfileValidationException("An XYZ identity profile must use the Input, Display, ColorSpace, or Abstract class.");
        }
        if (options.DataColorSpaceSignature != IccColorSpaceSignatures.Xyz
            || options.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Xyz)
        {
            throw new IccProfileValidationException("An XYZ identity profile requires the XYZ data space and PCSXYZ.");
        }
        if (!Core.IsD50(options.MediaWhitePoint))
        {
            throw new IccProfileValidationException("An XYZ identity profile media white point must be PCS D50.");
        }
        if (options.AdaptiveGainCurve != null)
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata is permitted only for RGB Input or Display profiles.");
        }
        if (options.ChromaticAdaptationMatrix != null)
        {
            double[] d50 = ChromaticAdaptationCore.CreateD50WhitePoint();
            double[] mapped = MatrixMath.ApplyMatrix3x3(
                options.ChromaticAdaptationMatrix.Values, d50);
            MatrixProfileFactory matrixFactory = new MatrixProfileFactory(Core);
            if (!matrixFactory.AreCloseWhitePoints(mapped, d50, 0.0005))
            {
                throw new IccProfileValidationException("An XYZ identity profile chromatic-adaptation matrix must preserve PCS D50.");
            }
        }
    }

    internal IccTag CreateIdentityTransformTag(uint signature, bool forward,
        IccVersionKind version)
    {
        if (version == IccVersionKind.Version2)
        {
            return CreateClassicIdentityTag(signature);
        }
        return CreateMultiStageIdentityTag(signature, forward);
    }

    internal IccTag CreateClassicIdentityTag(uint signature)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = CreateIdentityMatrixValues();
        lut.InputTables = CreateIdentityTables();
        lut.OutputTables = CreateIdentityTables();
        lut.Clut.InputChannels = 3;
        lut.Clut.OutputChannels = 3;
        lut.Clut.GridPoints = new int[] { 2, 2, 2 };
        lut.Clut.Values = CreateIdentityClutValues();
        lut.Clut.Interpolation = IccInterpolationKind.Tetrahedral;
        lut.Clut.InterpolationHint = 1;
        lut.PcsEncoding = IccPcsEncodingKind.Version2;

        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.Lut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Lut16;
        return Core.CreateTag(signature, data);
    }

    internal IccTag CreateMultiStageIdentityTag(uint signature, bool forward)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.BCurves = CreateIdentityCurves();
        lut.PcsEncoding = IccPcsEncodingKind.Version4;

        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.MultiStageLut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = forward
            ? IccTypeSignatures.LutAToB : IccTypeSignatures.LutBToA;
        return Core.CreateTag(signature, data);
    }

    internal double[] CreateIdentityMatrixValues()
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
    }

    internal double[][] CreateIdentityTables()
    {
        return new double[][]
        {
            new double[] { 0.0, 1.0 },
            new double[] { 0.0, 1.0 },
            new double[] { 0.0, 1.0 }
        };
    }

    internal IccCurve[] CreateIdentityCurves()
    {
        return new IccCurve[]
        {
            new IccCurve(),
            new IccCurve(),
            new IccCurve()
        };
    }

    internal double[] CreateIdentityClutValues()
    {
        return new double[]
        {
            0.0, 0.0, 0.0,
            0.0, 0.0, 1.0,
            0.0, 1.0, 0.0,
            0.0, 1.0, 1.0,
            1.0, 0.0, 0.0,
            1.0, 0.0, 1.0,
            1.0, 1.0, 0.0,
            1.0, 1.0, 1.0
        };
    }
}
