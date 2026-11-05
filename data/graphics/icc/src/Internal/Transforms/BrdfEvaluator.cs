namespace icc;

internal static class BrdfEvaluator
{
    internal const uint BlinnPhongMonochrome = 0x42506830;
    internal const uint BlinnPhongColor = 0x42506831;
    internal const uint CookTorranceOneMonochrome = 0x43543130;
    internal const uint CookTorranceOneColor = 0x43543131;
    internal const uint CookTorranceTwoMonochrome = 0x43543230;
    internal const uint CookTorranceTwoColor = 0x43543231;
    internal const uint CookTorranceThreeMonochrome = 0x43543330;
    internal const uint CookTorranceThreeColor = 0x43543331;
    internal const uint WardMonochrome = 0x57617230;
    internal const uint WardColor = 0x57617231;
    internal const uint LafortuneOneMonochrome = 0x4c613130;
    internal const uint LafortuneOneColor = 0x4c613131;
    internal const uint LafortuneTwoMonochrome = 0x4c613230;
    internal const uint LafortuneTwoColor = 0x4c613231;
    internal const uint LafortuneThreeMonochrome = 0x4c613330;
    internal const uint LafortuneThreeColor = 0x4c613331;

    internal static int GetParameterCount(uint modelSignature)
    {
        switch (modelSignature)
        {
            case BlinnPhongMonochrome:
                return 4;
            case BlinnPhongColor:
                return 3;
            case CookTorranceOneMonochrome:
                return 6;
            case CookTorranceOneColor:
                return 5;
            case CookTorranceTwoMonochrome:
                return 9;
            case CookTorranceTwoColor:
                return 8;
            case CookTorranceThreeMonochrome:
                return 11;
            case CookTorranceThreeColor:
                return 10;
            case WardMonochrome:
                return 5;
            case WardColor:
                return 4;
            case LafortuneOneMonochrome:
                return 9;
            case LafortuneOneColor:
                return 5;
            case LafortuneTwoMonochrome:
                return 16;
            case LafortuneTwoColor:
                return 9;
            case LafortuneThreeMonochrome:
                return 23;
            case LafortuneThreeColor:
                return 13;
            default:
                return 0;
        }
    }

    internal static bool IsMonochromeModel(uint modelSignature)
    {
        return modelSignature == BlinnPhongMonochrome || modelSignature == CookTorranceOneMonochrome
            || modelSignature == CookTorranceTwoMonochrome || modelSignature == CookTorranceThreeMonochrome
            || modelSignature == WardMonochrome || modelSignature == LafortuneOneMonochrome
            || modelSignature == LafortuneTwoMonochrome || modelSignature == LafortuneThreeMonochrome;
    }

    internal static IccOperationResult EvaluateFunction(uint modelSignature, System.ReadOnlySpan<double> parameters, System.ReadOnlySpan<double> absoluteValues,
        double viewingAzimuthDegrees, double viewingZenithDegrees, double lightingAzimuthDegrees, double lightingZenithDegrees, System.Span<double> outputs)
    {
        int parameterCount = GetParameterCount(modelSignature);
        if (parameterCount == 0)
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature, $"BRDF model '{FourCcCodec.Format(modelSignature)}' is not supported for evaluation.");
        }
        if (absoluteValues.Length < 1 || outputs.Length < absoluteValues.Length)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "BRDF evaluation requires at least one absolute-transform value and a matching output span.");
        }

        bool monochrome = IsMonochromeModel(modelSignature);
        if (!monochrome && absoluteValues.Length > System.Int32.MaxValue / parameterCount)
        {
            return CreateOperationResult(IccOperationStatus.ResourceLimitExceeded, "The BRDF parameter count exceeds the supported managed span length.");
        }
        int expectedParameters = monochrome ? parameterCount : parameterCount * absoluteValues.Length;
        if (parameters.Length != expectedParameters)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, $"BRDF model '{FourCcCodec.Format(modelSignature)}' requires {expectedParameters} parameter values for {absoluteValues.Length} output channels.");
        }
        if (!AreFinite(parameters) || !AreFinite(absoluteValues) || !AreValidAngles(viewingAzimuthDegrees, viewingZenithDegrees, lightingAzimuthDegrees, lightingZenithDegrees))
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "BRDF parameters, absolute values, and angles shall be finite and within their defined ranges.");
        }

        double viewAzimuth = viewingAzimuthDegrees * System.Math.PI / 180.0;
        double viewZenith = viewingZenithDegrees * System.Math.PI / 180.0;
        double lightAzimuth = lightingAzimuthDegrees * System.Math.PI / 180.0;
        double lightZenith = lightingZenithDegrees * System.Math.PI / 180.0;
        double viewX = System.Math.Sin(viewZenith) * System.Math.Cos(viewAzimuth);
        double viewY = System.Math.Sin(viewZenith) * System.Math.Sin(viewAzimuth);
        double viewZ = System.Math.Cos(viewZenith);
        double lightX = System.Math.Sin(lightZenith) * System.Math.Cos(lightAzimuth);
        double lightY = System.Math.Sin(lightZenith) * System.Math.Sin(lightAzimuth);
        double lightZ = System.Math.Cos(lightZenith);
        double halfX = viewX + lightX;
        double halfY = viewY + lightY;
        double halfZ = viewZ + lightZ;
        double halfLength = System.Math.Sqrt((halfX * halfX) + (halfY * halfY) + (halfZ * halfZ));
        if (halfLength > 0.0)
        {
            halfX = halfX / halfLength;
            halfY = halfY / halfLength;
            halfZ = halfZ / halfLength;
        }

        double normalLight = System.Math.Max(0.0, lightZ);
        double normalView = System.Math.Max(0.0, viewZ);
        double normalHalf = System.Math.Max(0.0, halfZ);
        double viewHalf = System.Math.Max(0.0, (viewX * halfX) + (viewY * halfY) + (viewZ * halfZ));
        double[] evaluated = new double[absoluteValues.Length];
        int channel = 0;
        while (channel < absoluteValues.Length)
        {
            int offset = monochrome ? 0 : channel * parameterCount;
            double value;
            if (modelSignature == BlinnPhongMonochrome || modelSignature == BlinnPhongColor)
            {
                value = EvaluateBlinnPhongChannel(parameters, offset, absoluteValues[channel], monochrome, normalLight, normalHalf);
            }
            else if (modelSignature == WardMonochrome || modelSignature == WardColor)
            {
                value = EvaluateWardChannel(parameters, offset, absoluteValues[channel], monochrome, normalLight, normalView, normalHalf, halfX, halfY);
            }
            else if (modelSignature == LafortuneOneMonochrome || modelSignature == LafortuneOneColor
                || modelSignature == LafortuneTwoMonochrome || modelSignature == LafortuneTwoColor
                || modelSignature == LafortuneThreeMonochrome || modelSignature == LafortuneThreeColor)
            {
                value = EvaluateLafortuneChannel(modelSignature, parameters, offset, absoluteValues[channel], monochrome,
                    normalLight, lightX, lightY, lightZ, viewX, viewY, viewZ);
            }
            else
            {
                value = EvaluateCookTorranceChannel(modelSignature, parameters, offset, absoluteValues[channel], monochrome,
                    normalLight, normalView, normalHalf, viewHalf);
            }

            if (!System.Double.IsFinite(value))
            {
                return CreateOperationResult(IccOperationStatus.InvalidData, $"BRDF model '{FourCcCodec.Format(modelSignature)}' produced a non-finite value from its parameters.");
            }
            evaluated[channel] = value;
            channel = channel + 1;
        }

        new System.ReadOnlySpan<double>(evaluated).CopyTo(outputs);
        return CreateOperationResult(IccOperationStatus.Success, $"Evaluated BRDF model '{FourCcCodec.Format(modelSignature)}'.");
    }

    internal static IccOperationResult EvaluateParameterWorkflow(IccProfile profile, IccBrdf brdf, System.ReadOnlySpan<double> transformInputs,
        System.ReadOnlySpan<double> absoluteValues, double viewingAzimuthDegrees, double viewingZenithDegrees, double lightingAzimuthDegrees,
        double lightingZenithDegrees, bool spectral, IccInterpolationKind interpolation, System.Span<double> outputs)
    {
        if (brdf == null)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "A decoded BRDF structure is required.");
        }

        int parameterCount = GetParameterCount(brdf.ModelSignature);
        if (parameterCount == 0)
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature, $"BRDF model '{FourCcCodec.Format(brdf.ModelSignature)}' is not supported for evaluation.");
        }

        bool monochrome = IsMonochromeModel(brdf.ModelSignature);
        uint expectedType = monochrome ? StructuredTagProjectionCodec.MonochromeBrdf : StructuredTagProjectionCodec.ColorBrdf;
        if (brdf.TypeSignature != 0 && brdf.TypeSignature != expectedType)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "The BRDF model and parameterization type do not agree.");
        }
        if (brdf.Parameters == null || brdf.Parameters.Length != 1 || brdf.Parameters[0] != parameterCount)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, $"BRDF model '{FourCcCodec.Format(brdf.ModelSignature)}' requires a declared count of {parameterCount} parameters per channel.");
        }

        IccMultiProcessElements? pipeline = spectral ? brdf.SpectralTransform : brdf.ColorimetricTransform;
        if (pipeline == null)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, spectral ? "The BRDF structure has no spectral parameter transform." : "The BRDF structure has no colorimetric parameter transform.");
        }

        if (!monochrome && absoluteValues.Length > System.Int32.MaxValue / parameterCount)
        {
            return CreateOperationResult(IccOperationStatus.ResourceLimitExceeded, "The BRDF parameter count exceeds the supported managed span length.");
        }
        int expectedOutputs = monochrome ? parameterCount : parameterCount * absoluteValues.Length;
        if (pipeline.InputChannels != transformInputs.Length || pipeline.OutputChannels != expectedOutputs)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, $"The BRDF parameter transform requires {transformInputs.Length} input channels and {expectedOutputs} output channels for this workflow.");
        }

        MpePipelinePlan plan = MpePipelineCompiler.CompileEmbedded(profile, pipeline, interpolation);
        double[] parameters = new double[plan.DestinationChannels];
        ExecutePlan(plan, transformInputs, parameters);
        return EvaluateFunction(brdf.ModelSignature, parameters, absoluteValues, viewingAzimuthDegrees, viewingZenithDegrees,
            lightingAzimuthDegrees, lightingZenithDegrees, outputs);
    }

    internal static IccOperationResult EvaluateBrdfTransform(IccProfile profile, IccTag tag, System.ReadOnlySpan<double> transformInputs,
        double viewingAzimuthDegrees, double viewingZenithDegrees, double lightingAzimuthDegrees, double lightingZenithDegrees,
        IccTransformDirection direction, IccInterpolationKind interpolation, System.Span<double> outputs)
    {
        if (tag == null || !IsBrdfTransformTag(tag.Signature))
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature, "The selected tag is not a registered direct BRDF transform.");
        }

        return ExecuteAngularTransform(profile, tag, transformInputs, viewingAzimuthDegrees, viewingZenithDegrees,
            lightingAzimuthDegrees, lightingZenithDegrees, direction, interpolation, outputs, false);
    }

    internal static IccOperationResult EvaluateDirectionalEmission(IccProfile profile, IccTag tag, System.ReadOnlySpan<double> transformInputs,
        double viewingAzimuthDegrees, double viewingZenithDegrees, double horizontalPosition, double verticalPosition,
        IccTransformDirection direction, IccInterpolationKind interpolation, System.Span<double> outputs)
    {
        if (tag == null || !IsDirectionalTransformTag(tag.Signature))
        {
            return CreateOperationResult(IccOperationStatus.UnsupportedFeature, "The selected tag is not a registered directional transform.");
        }

        return ExecuteAngularTransform(profile, tag, transformInputs, viewingAzimuthDegrees, viewingZenithDegrees,
            horizontalPosition, verticalPosition, direction, interpolation, outputs, true);
    }

    internal static double EvaluateBlinnPhongChannel(System.ReadOnlySpan<double> parameters, int offset, double absoluteValue,
        bool monochrome, double normalLight, double normalHalf)
    {
        double diffuse = monochrome ? parameters[offset] * absoluteValue : parameters[offset];
        double specular = monochrome ? (parameters[offset + 1] * absoluteValue) + parameters[offset + 2] : parameters[offset + 1];
        double shininess = monochrome ? parameters[offset + 3] : parameters[offset + 2];
        return (diffuse * normalLight) + (specular * System.Math.Pow(normalHalf, shininess));
    }

    internal static double EvaluateCookTorranceChannel(uint modelSignature, System.ReadOnlySpan<double> parameters, int offset, double absoluteValue,
        bool monochrome, double normalLight, double normalView, double normalHalf, double viewHalf)
    {
        int lobes = GetLobeCount(modelSignature);
        double diffuse = monochrome ? parameters[offset] * absoluteValue : parameters[offset];
        double specular = monochrome ? (parameters[offset + 1] * absoluteValue) + parameters[offset + 2] : parameters[offset + 1];
        int cursor = offset + (monochrome ? 3 : 2);
        double distribution = 0.0;
        int lobe = 0;
        while (lobe < lobes)
        {
            double slope = parameters[cursor];
            double weight = lobes == 1 ? 1.0 : parameters[cursor + 1];
            cursor = cursor + (lobes == 1 ? 1 : 2);
            distribution = distribution + (weight * EvaluateCookDistribution(slope, normalHalf));
            lobe = lobe + 1;
        }

        double refractiveIndex = parameters[cursor];
        double extinction = parameters[cursor + 1];
        if (normalLight <= 0.0 || normalView <= 0.0 || viewHalf <= 0.0)
        {
            return diffuse * normalLight;
        }

        double geometry = System.Math.Min(1.0, System.Math.Min(2.0 * normalHalf * normalView / viewHalf, 2.0 * normalHalf * normalLight / viewHalf));
        double fresnel = EvaluateFresnel(refractiveIndex, extinction, viewHalf);
        double reflection = fresnel * distribution * geometry / (System.Math.PI * normalLight * normalView);
        return (diffuse * normalLight) + (specular * reflection);
    }

    internal static double EvaluateWardChannel(System.ReadOnlySpan<double> parameters, int offset, double absoluteValue, bool monochrome,
        double normalLight, double normalView, double normalHalf, double halfX, double halfY)
    {
        double diffuse = monochrome ? parameters[offset] * absoluteValue : parameters[offset];
        double specular = monochrome ? (parameters[offset + 1] * absoluteValue) + parameters[offset + 2] : parameters[offset + 1];
        double alphaX = parameters[offset + (monochrome ? 3 : 2)];
        double alphaY = parameters[offset + (monochrome ? 4 : 3)];
        if (normalLight <= 0.0 || normalView <= 0.0 || alphaX == 0.0 || alphaY == 0.0)
        {
            return diffuse * normalLight;
        }

        double exponent = -2.0 * (((halfX / alphaX) * (halfX / alphaX)) + ((halfY / alphaY) * (halfY / alphaY))) / (1.0 + normalHalf);
        double reflection = normalLight * System.Math.Exp(exponent) / (4.0 * System.Math.PI * alphaX * alphaY * System.Math.Sqrt(normalLight * normalView));
        return (diffuse * normalLight) + (specular * reflection);
    }

    internal static double EvaluateLafortuneChannel(uint modelSignature, System.ReadOnlySpan<double> parameters, int offset, double absoluteValue,
        bool monochrome, double normalLight, double lightX, double lightY, double lightZ, double viewX, double viewY, double viewZ)
    {
        int lobes = GetLobeCount(modelSignature);
        double diffuse = monochrome ? parameters[offset] + (parameters[offset + 1] * absoluteValue) : parameters[offset];
        int cursor = offset + (monochrome ? 2 : 1);
        double reflection = 0.0;
        int lobe = 0;
        while (lobe < lobes)
        {
            double coefficientX;
            double coefficientY;
            double coefficientZ;
            double exponent;
            if (monochrome)
            {
                coefficientX = (parameters[cursor] * absoluteValue) + parameters[cursor + 3];
                coefficientY = (parameters[cursor + 1] * absoluteValue) + parameters[cursor + 4];
                coefficientZ = (parameters[cursor + 2] * absoluteValue) + parameters[cursor + 5];
                exponent = parameters[cursor + 6];
                cursor = cursor + 7;
            }
            else
            {
                coefficientX = parameters[cursor];
                coefficientY = parameters[cursor + 1];
                coefficientZ = parameters[cursor + 2];
                exponent = parameters[cursor + 3];
                cursor = cursor + 4;
            }

            double lobeValue = (coefficientX * lightX * viewX) + (coefficientY * lightY * viewY) + (coefficientZ * lightZ * viewZ);
            reflection = reflection + System.Math.Pow(System.Math.Max(0.0, lobeValue), exponent);
            lobe = lobe + 1;
        }

        return (diffuse * normalLight) + reflection;
    }

    internal static double EvaluateCookDistribution(double slope, double normalHalf)
    {
        if (slope <= 0.0 || normalHalf <= 0.0)
        {
            return double.NaN;
        }

        double tangentSquared = (1.0 - (normalHalf * normalHalf)) / (normalHalf * normalHalf);
        double slopeSquared = slope * slope;
        return System.Math.Exp(-tangentSquared / slopeSquared) / (slopeSquared * normalHalf * normalHalf * normalHalf * normalHalf);
    }

    internal static double EvaluateFresnel(double refractiveIndex, double extinction, double cosine)
    {
        double indexSquared = refractiveIndex * refractiveIndex;
        double extinctionSquared = extinction * extinction;
        double cosineSquared = cosine * cosine;
        double common = indexSquared + extinctionSquared;
        double twiceIndexCosine = 2.0 * refractiveIndex * cosine;
        double perpendicular = (common - twiceIndexCosine + cosineSquared) / (common + twiceIndexCosine + cosineSquared);
        double parallel = ((common * cosineSquared) - twiceIndexCosine + 1.0) / ((common * cosineSquared) + twiceIndexCosine + 1.0);
        return (perpendicular + parallel) * 0.5;
    }

    internal static int GetLobeCount(uint modelSignature)
    {
        if (modelSignature == CookTorranceTwoMonochrome || modelSignature == CookTorranceTwoColor
            || modelSignature == LafortuneTwoMonochrome || modelSignature == LafortuneTwoColor)
        {
            return 2;
        }
        if (modelSignature == CookTorranceThreeMonochrome || modelSignature == CookTorranceThreeColor
            || modelSignature == LafortuneThreeMonochrome || modelSignature == LafortuneThreeColor)
        {
            return 3;
        }

        return 1;
    }

    internal static IccOperationResult ExecuteAngularTransform(IccProfile profile, IccTag tag, System.ReadOnlySpan<double> transformInputs,
        double firstGeometry, double secondGeometry, double thirdGeometry, double fourthGeometry, IccTransformDirection direction,
        IccInterpolationKind interpolation, System.Span<double> outputs, bool directional)
    {
        bool geometryValid = directional
            ? AreValidDirectionalGeometry(firstGeometry, secondGeometry, thirdGeometry, fourthGeometry)
            : AreValidAngles(firstGeometry, secondGeometry, thirdGeometry, fourthGeometry);
        if (!geometryValid)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "Angular transform geometry shall be finite and within its defined ranges.");
        }

        MpePipelinePlan plan = MpePipelineCompiler.Compile(profile, tag, direction, interpolation);
        if (plan.SourceChannels != transformInputs.Length + 4 || outputs.Length < plan.DestinationChannels || !AreFinite(transformInputs))
        {
            return CreateOperationResult(IccOperationStatus.InvalidData, "Angular transform source or destination channel counts are inconsistent, or an input is non-finite.");
        }

        double[] inputs = new double[plan.SourceChannels];
        inputs[0] = (firstGeometry + 180.0) / 360.0;
        inputs[1] = secondGeometry / 90.0;
        inputs[2] = directional ? thirdGeometry : (thirdGeometry + 180.0) / 360.0;
        inputs[3] = directional ? fourthGeometry : fourthGeometry / 90.0;
        transformInputs.CopyTo(new System.Span<double>(inputs, 4, transformInputs.Length));
        double[] evaluated = new double[plan.DestinationChannels];
        ExecutePlan(plan, inputs, evaluated);
        new System.ReadOnlySpan<double>(evaluated).CopyTo(outputs);
        return CreateOperationResult(IccOperationStatus.Success, directional ? "Evaluated the directional-emission transform." : "Evaluated the direct BRDF transform.");
    }

    internal static void ExecutePlan(MpePipelinePlan plan, System.ReadOnlySpan<double> inputs, System.Span<double> outputs)
    {
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        PipelineExecutor.ExecutePipeline(plan.Stages, inputs, outputs, new double[valueLength], new double[valueLength], new int[indexLength], new double[weightLength]);
    }

    internal static bool IsBrdfTransformTag(uint signature)
    {
        return MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfAToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToA0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfBToD0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfDToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.BrdfMToS0);
    }

    internal static bool IsDirectionalTransformTag(uint signature)
    {
        return MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalAToB0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToA0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToD0)
            || MultiProcessElementsTagCodec.IsAngularTransformTag(signature, IccTagSignatures.DirectionalDToB0);
    }

    internal static bool AreFinite(System.ReadOnlySpan<double> values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }

        return true;
    }

    internal static bool AreValidAngles(double firstAzimuth, double firstZenith, double secondAzimuth, double secondZenith)
    {
        return System.Double.IsFinite(firstAzimuth) && firstAzimuth >= -180.0 && firstAzimuth <= 180.0
            && System.Double.IsFinite(firstZenith) && firstZenith >= 0.0 && firstZenith <= 90.0
            && System.Double.IsFinite(secondAzimuth) && secondAzimuth >= -180.0 && secondAzimuth <= 180.0
            && System.Double.IsFinite(secondZenith) && secondZenith >= 0.0 && secondZenith <= 90.0;
    }

    internal static bool AreValidDirectionalGeometry(double azimuth, double zenith, double horizontal, double vertical)
    {
        return System.Double.IsFinite(azimuth) && azimuth >= -180.0 && azimuth <= 180.0
            && System.Double.IsFinite(zenith) && zenith >= 0.0 && zenith <= 90.0
            && System.Double.IsFinite(horizontal) && horizontal >= -1.0 && horizontal <= 1.0
            && System.Double.IsFinite(vertical) && vertical >= -1.0 && vertical <= 1.0;
    }

    internal static IccOperationResult CreateOperationResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
