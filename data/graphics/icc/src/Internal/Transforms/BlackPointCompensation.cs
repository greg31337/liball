namespace icc;

internal static class BlackPointCompensation
{
    internal const int RampPointCount = 256;

    internal static void ValidateSequence(IccProfile[] profiles, IccRenderingIntent intent)
    {
        if (intent != IccRenderingIntent.MediaRelativeColorimetric)
        {
            throw new IccTransformException("Black-point compensation is available only for explicitly requested media-relative profile links.");
        }

        int index = 0;
        while (index < profiles.Length)
        {
            IccProfile profile = profiles[index];
            IccProfileClass profileClass = profile.Header.ProfileClass;
            if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display
                && profileClass != IccProfileClass.Output && profileClass != IccProfileClass.ColorSpace)
            {
                throw new IccTransformException($"Profile {index} has a class that is not eligible for ISO 18619 black-point compensation.");
            }

            uint space = profile.Header.DataColorSpaceSignature;
            if (space != IccColorSpaceSignatures.Gray && space != IccColorSpaceSignatures.Rgb
                && space != IccColorSpaceSignatures.Cmyk && space != IccColorSpaceSignatures.Lab)
            {
                throw new IccTransformException($"Profile {index} does not use an ISO 18619 Gray, RGB, CMYK, or CIELAB device colour space.");
            }
            index = index + 1;
        }
    }

    internal static PipelineStage[] CompileLink(IccProfile sourceProfile, IccProfile destinationProfile,
        ProfileDirectionPlan sourceDirection, ProfileDirectionPlan destinationDirection,
        IccInterpolationKind interpolation, int linkIndex)
    {
        if (sourceDirection.SelectedIntent != IccRenderingIntent.MediaRelativeColorimetric
            || destinationDirection.SelectedIntent != IccRenderingIntent.MediaRelativeColorimetric)
        {
            throw new IccTransformException($"Profile link {linkIndex} cannot use black-point compensation because intent fallback did not select media-relative transforms at both ends.");
        }

        double sourceLightness = DetectSourceBlack(sourceProfile, sourceDirection, interpolation, linkIndex);
        double destinationLightness = DetectDestinationBlack(destinationProfile, destinationDirection, interpolation, linkIndex + 1);
        PipelineStage mapping = CreateMappingStage(sourceLightness, destinationLightness, linkIndex);
        int count = 1;
        if (sourceDirection.PcsKind == IccPcsKind.Lab)
        {
            count = count + 1;
        }
        if (destinationDirection.PcsKind == IccPcsKind.Lab)
        {
            count = count + 1;
        }
        if (sourceDirection.VersionKind != destinationDirection.VersionKind)
        {
            count = count + 1;
        }

        PipelineStage[] stages = new PipelineStage[count];
        int index = 0;
        if (sourceDirection.PcsKind == IccPcsKind.Lab)
        {
            stages[index] = ProfileTransformCompiler.CreatePcsBridgeStage(PipelineStageKind.PcsLabToXyz,
                $"black-point link {linkIndex}: convert D50 PCSLAB to PCSXYZ");
            index = index + 1;
        }
        stages[index] = mapping;
        index = index + 1;
        if (destinationDirection.PcsKind == IccPcsKind.Lab)
        {
            stages[index] = ProfileTransformCompiler.CreatePcsBridgeStage(PipelineStageKind.PcsXyzToLab,
                $"black-point link {linkIndex}: convert PCSXYZ to D50 PCSLAB");
            index = index + 1;
        }
        if (sourceDirection.VersionKind != destinationDirection.VersionKind)
        {
            PipelineStage versionBridge = new PipelineStage();
            versionBridge.Kind = PipelineStageKind.Identity;
            versionBridge.InputChannels = 3;
            versionBridge.OutputChannels = 3;
            versionBridge.Description = $"black-point link {linkIndex}: bridge ICC version {(int)sourceDirection.VersionKind} PCS values to version {(int)destinationDirection.VersionKind} PCS values";
            stages[index] = versionBridge;
        }
        return stages;
    }

    internal static double DetectSourceBlack(IccProfile profile, ProfileDirectionPlan requestedForward,
        IccInterpolationKind interpolation, int profileIndex)
    {
        double[] localBlack = SelectSourceLocalBlack(profile, requestedForward, interpolation, profileIndex);
        double[] lab = ExecuteDeviceToLab(requestedForward, localBlack);
        return ClampBlackLightness(lab[0]);
    }

    internal static double DetectDestinationBlack(IccProfile profile, ProfileDirectionPlan requestedReverse,
        IccInterpolationKind interpolation, int profileIndex)
    {
        ProfileDirectionPlan requestedForward = ProfileTransformCompiler.CompileProfileDirection(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, interpolation, profileIndex);
        if (!requestedReverse.IsLutBased)
        {
            double[] localBlack = FindDarkestDeviceValue(profile, requestedForward);
            double[] lab = ExecuteDeviceToLab(requestedForward, localBlack);
            return ClampBlackLightness(lab[0]);
        }

        double[] initialLab = CreateInitialLab(profile, requestedForward, interpolation, profileIndex);
        ProfileDirectionPlan relativeForward = ProfileTransformCompiler.CompileProfileDirection(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, interpolation, profileIndex);
        double[] inputRamp = new double[RampPointCount];
        double[] outputRamp = new double[RampPointCount];
        double ka = ClampValue(initialLab[1], -50.0, 50.0);
        double kb = ClampValue(initialLab[2], -50.0, 50.0);
        int index = 0;
        while (index < RampPointCount)
        {
            double fraction = (double)index / (double)(RampPointCount - 1);
            double[] inputLab = new double[] { 100.0 * fraction, ka * (1.0 - fraction), kb * (1.0 - fraction) };
            double[] pcs = ConvertLabToPcs(inputLab, requestedReverse.PcsKind);
            double[] device = ExecuteDirection(requestedReverse, pcs);
            double[] outputLab = ExecuteDeviceToLab(relativeForward, device);
            inputRamp[index] = inputLab[0];
            outputRamp[index] = outputLab[0];
            index = index + 1;
        }

        return FitDestinationBlack(inputRamp, outputRamp, initialLab[0], true);
    }

    internal static double[] SelectSourceLocalBlack(IccProfile profile, ProfileDirectionPlan requestedForward,
        IccInterpolationKind interpolation, int profileIndex)
    {
        uint space = profile.Header.DataColorSpaceSignature;
        if (space == IccColorSpaceSignatures.Lab)
        {
            return new double[] { 0.0, 0.0, 0.0 };
        }
        if (space == IccColorSpaceSignatures.Cmyk && HasPerceptualReverse(profile))
        {
            ProfileDirectionPlan perceptualReverse = ProfileTransformCompiler.CompileProfileDirection(profile,
                IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.None,
                interpolation, profileIndex);
            return ExecuteDirection(perceptualReverse, ConvertLabToPcs(new double[] { 0.0, 0.0, 0.0 }, perceptualReverse.PcsKind));
        }
        return FindDarkestDeviceValue(profile, requestedForward);
    }

    internal static double[] CreateInitialLab(IccProfile profile, ProfileDirectionPlan requestedForward,
        IccInterpolationKind interpolation, int profileIndex)
    {
        uint space = profile.Header.DataColorSpaceSignature;
        double[] localBlack;
        if (space == IccColorSpaceSignatures.Cmyk)
        {
            ProfileDirectionPlan perceptualReverse = ProfileTransformCompiler.CompileProfileDirection(profile,
                IccTransformDirection.PcsToDevice, IccRenderingIntent.Perceptual, IccIntentFallback.None,
                interpolation, profileIndex);
            localBlack = ExecuteDirection(perceptualReverse,
                ConvertLabToPcs(new double[] { 0.0, 0.0, 0.0 }, perceptualReverse.PcsKind));
        }
        else if (space == IccColorSpaceSignatures.Lab)
        {
            localBlack = new double[] { 0.0, 0.0, 0.0 };
        }
        else
        {
            localBlack = FindDarkestDeviceValue(profile, requestedForward);
        }

        double[] initialLab = ExecuteDeviceToLab(requestedForward, localBlack);
        if (space == IccColorSpaceSignatures.Cmyk)
        {
            initialLab[1] = 0.0;
            initialLab[2] = 0.0;
        }
        initialLab[0] = ClampBlackLightness(initialLab[0]);
        return initialLab;
    }

    internal static bool HasPerceptualReverse(IccProfile profile)
    {
        IntentSelection selection = IntentSelector.SelectIcc1Transform(profile, IccTransformDirection.PcsToDevice,
            IccRenderingIntent.Perceptual, IccIntentFallback.None, false);
        return selection.Success;
    }

    internal static double[] FindDarkestDeviceValue(IccProfile profile, ProfileDirectionPlan forward)
    {
        double[][] vertices = CreateDarkVertices(profile.Header.DataColorSpaceSignature);
        double[] darkest = vertices[0];
        double darkestLightness = ExecuteDeviceToLab(forward, darkest)[0];
        int index = 1;
        while (index < vertices.Length)
        {
            double lightness = ExecuteDeviceToLab(forward, vertices[index])[0];
            if (lightness < darkestLightness)
            {
                darkest = vertices[index];
                darkestLightness = lightness;
            }
            index = index + 1;
        }
        return darkest;
    }

    internal static double[][] CreateDarkVertices(uint space)
    {
        if (space == IccColorSpaceSignatures.Gray)
        {
            return new double[][] { new double[] { 0.0 }, new double[] { 1.0 } };
        }
        if (space == IccColorSpaceSignatures.Rgb)
        {
            return new double[][] { new double[] { 0.0, 0.0, 0.0 }, new double[] { 1.0, 1.0, 1.0 } };
        }
        if (space == IccColorSpaceSignatures.Cmyk)
        {
            return new double[][]
            {
                new double[] { 0.0, 0.0, 0.0, 0.0 },
                new double[] { 1.0, 1.0, 1.0, 1.0 },
                new double[] { 0.0, 0.0, 0.0, 1.0 },
                new double[] { 1.0, 1.0, 1.0, 0.0 }
            };
        }
        throw new IccTransformException("Darkest-device-value detection requires a Gray, RGB, or CMYK device colour space.");
    }

    internal static double[] ExecuteDeviceToLab(ProfileDirectionPlan forward, double[] device)
    {
        double[] pcs = ExecuteDirection(forward, device);
        if (forward.PcsKind == IccPcsKind.Lab)
        {
            return pcs;
        }
        double[] lab = new double[3];
        PipelineExecutor.ApplyPcsXyzToLabStage(pcs, lab);
        return lab;
    }

    internal static double[] ConvertLabToPcs(double[] lab, IccPcsKind pcsKind)
    {
        if (pcsKind == IccPcsKind.Lab)
        {
            return new double[] { lab[0], lab[1], lab[2] };
        }
        double[] xyz = new double[3];
        PipelineExecutor.ApplyPcsLabToXyzStage(lab, xyz);
        return xyz;
    }

    internal static double[] ExecuteDirection(ProfileDirectionPlan direction, double[] input)
    {
        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(direction.Stages);
        double[] output = new double[direction.DestinationChannels];
        double[] firstScratch = new double[scratchLength];
        double[] secondScratch = new double[scratchLength];
        int[] indexScratch = new int[PipelineExecutor.GetRequiredClutIndexScratchLength(direction.Stages)];
        double[] weightScratch = new double[PipelineExecutor.GetRequiredClutWeightScratchLength(direction.Stages)];
        PipelineExecutor.ExecutePipeline(direction.Stages, input, output, firstScratch, secondScratch, indexScratch, weightScratch);
        return output;
    }

    internal static PipelineStage CreateMappingStage(double sourceLightness, double destinationLightness, int linkIndex)
    {
        double sourceY = DecodeLightness(sourceLightness);
        double destinationY = DecodeLightness(destinationLightness);
        double scale = (1.0 - destinationY) / (1.0 - sourceY);
        if (!System.Double.IsFinite(scale))
        {
            throw new IccTransformException("The detected black points do not define a finite ISO 18619 mapping.");
        }
        double offsetFactor = 1.0 - scale;
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Matrix;
        stage.InputChannels = 3;
        stage.OutputChannels = 3;
        stage.Matrix = new IccMatrix();
        stage.Matrix.Rows = 3;
        stage.Matrix.Columns = 4;
        stage.Matrix.Values = new double[]
        {
            scale, 0.0, 0.0, offsetFactor * IccStandardColorimetry.D50X,
            0.0, scale, 0.0, offsetFactor * IccStandardColorimetry.D50Y,
            0.0, 0.0, scale, offsetFactor * IccStandardColorimetry.D50Z
        };
        stage.Description = $"ISO 18619 black-point link {linkIndex}: map L* {sourceLightness:F6} to L* {destinationLightness:F6}";
        return stage;
    }

    internal static double DecodeLightness(double lightness)
    {
        if (lightness < 0.0)
        {
            return -DecodePositiveLightness(-lightness);
        }
        return DecodePositiveLightness(lightness);
    }

    internal static double DecodePositiveLightness(double lightness)
    {
        if (lightness <= 8.0)
        {
            double threshold = 24.0 / 116.0;
            return lightness * threshold * threshold * threshold / 8.0;
        }
        double value = (lightness + 16.0) / 116.0;
        return value * value * value;
    }

    internal static double FitDestinationBlack(double[] inputRamp, double[] outputRamp, double initialLightness, bool relative)
    {
        int last = outputRamp.Length - 1;
        int index = last - 1;
        while (index >= 0)
        {
            if (outputRamp[index] > outputRamp[index + 1])
            {
                outputRamp[index] = outputRamp[index + 1];
            }
            index = index - 1;
        }

        double minimum = outputRamp[0];
        double maximum = outputRamp[last];
        if (!(minimum < maximum))
        {
            return 0.0;
        }
        if (relative && IsMidRangeStraight(inputRamp, outputRamp, minimum, maximum))
        {
            return ClampBlackLightness(initialLightness);
        }

        double lower = relative ? 0.1 : 0.03;
        double upper = relative ? 0.5 : 0.25;
        double s0 = 0.0;
        double s1 = 0.0;
        double s2 = 0.0;
        double s3 = 0.0;
        double s4 = 0.0;
        double t0 = 0.0;
        double t1 = 0.0;
        double t2 = 0.0;
        int count = 0;
        index = 0;
        while (index < outputRamp.Length)
        {
            double y = (outputRamp[index] - minimum) / (maximum - minimum);
            if (y >= lower && y < upper)
            {
                double x = inputRamp[index];
                double x2 = x * x;
                s0 = s0 + 1.0;
                s1 = s1 + x;
                s2 = s2 + x2;
                s3 = s3 + (x2 * x);
                s4 = s4 + (x2 * x2);
                t0 = t0 + y;
                t1 = t1 + (x * y);
                t2 = t2 + (x2 * y);
                count = count + 1;
            }
            index = index + 1;
        }
        if (count < 3)
        {
            return 0.0;
        }

        double determinant = Determinant3(s4, s3, s2, s3, s2, s1, s2, s1, s0);
        if (System.Math.Abs(determinant) < 1.0E-20)
        {
            return 0.0;
        }
        double a = Determinant3(t2, s3, s2, t1, s2, s1, t0, s1, s0) / determinant;
        double b = Determinant3(s4, t2, s2, s3, t1, s1, s2, t0, s0) / determinant;
        double c = Determinant3(s4, s3, t2, s3, s2, t1, s2, s1, t0) / determinant;
        if (System.Math.Abs(a) < 1.0E-10)
        {
            if (System.Math.Abs(b) < 1.0E-20)
            {
                return 0.0;
            }
            return ClampValue(-c / b, 0.0, 50.0);
        }
        double discriminant = (b * b) - (4.0 * a * c);
        if (discriminant <= 0.0)
        {
            return 0.0;
        }
        return ClampValue((-b + System.Math.Sqrt(discriminant)) / (2.0 * a), 0.0, 50.0);
    }

    internal static bool IsMidRangeStraight(double[] inputRamp, double[] outputRamp, double minimum, double maximum)
    {
        double threshold = minimum + (0.2 * (maximum - minimum));
        int index = 0;
        while (index < outputRamp.Length)
        {
            if (inputRamp[index] > threshold && System.Math.Abs(inputRamp[index] - outputRamp[index]) >= 4.0)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static double Determinant3(double a00, double a01, double a02, double a10, double a11, double a12,
        double a20, double a21, double a22)
    {
        return (a00 * ((a11 * a22) - (a12 * a21)))
            - (a01 * ((a10 * a22) - (a12 * a20)))
            + (a02 * ((a10 * a21) - (a11 * a20)));
    }

    internal static double ClampBlackLightness(double value)
    {
        return value > 50.0 ? 50.0 : value;
    }

    internal static double ClampValue(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }
        if (value > maximum)
        {
            return maximum;
        }
        return value;
    }
}
