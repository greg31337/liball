namespace icc;

internal class MatrixTrcPlan
{
    internal IccTransformDirection Direction;
    internal int SourceChannels;
    internal int DestinationChannels;
    internal bool IsGray;
    internal PipelineStage[] Stages;
    internal IccXyzNumber? Luminance;

    internal MatrixTrcPlan()
    {
        Direction = IccTransformDirection.DeviceToPcs;
        SourceChannels = 0;
        DestinationChannels = 0;
        IsGray = false;
        Stages = new PipelineStage[0];
        Luminance = null;
    }
}

internal static class MatrixTrcCompiler
{
    internal const int CurveValidationSamples = 1025;

    internal static MatrixTrcPlan Compile(IccProfile profile, IccTransformDirection direction)
    {
        ValidateRequest(profile, direction);
        MatrixTrcPlan plan;
        if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Gray)
        {
            plan = CompileGray(profile, direction);
        }
        else if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Rgb)
        {
            plan = CompileRgb(profile, direction);
        }
        else
        {
            throw new IccTransformException("A matrix/TRC plan requires a GRAY or RGB device colour space.");
        }

        plan.Luminance = ReadOptionalXyz(profile, IccTagSignatures.Luminance, "luminance");
        PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        return plan;
    }

    internal static MatrixTrcPlan CompileRgb(IccProfile profile, IccTransformDirection direction)
    {
        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display
            && profileClass != IccProfileClass.ColorSpace)
        {
            throw new IccTransformException("An RGB matrix/TRC plan is defined only for input, display, and colour-space profiles.");
        }

        IccCurve[] curves = new IccCurve[]
        {
            ReadCurve(profile, IccTagSignatures.RedTrc, "red TRC", direction),
            ReadCurve(profile, IccTagSignatures.GreenTrc, "green TRC", direction),
            ReadCurve(profile, IccTagSignatures.BlueTrc, "blue TRC", direction)
        };
        IccXyzNumber red = ReadRequiredXyz(profile, IccTagSignatures.RedMatrixColumn, "red matrix column");
        IccXyzNumber green = ReadRequiredXyz(profile, IccTagSignatures.GreenMatrixColumn, "green matrix column");
        IccXyzNumber blue = ReadRequiredXyz(profile, IccTagSignatures.BlueMatrixColumn, "blue matrix column");
        double[] matrixValues = new double[]
        {
            red.X, green.X, blue.X,
            red.Y, green.Y, blue.Y,
            red.Z, green.Z, blue.Z
        };

        PipelineStage curveStage = CreateCurveStage(curves, direction == IccTransformDirection.PcsToDevice, "RGB tone reproduction curves");
        PipelineStage matrixStage;
        MatrixTrcPlan plan = new MatrixTrcPlan();
        plan.Direction = direction;
        plan.SourceChannels = 3;
        plan.DestinationChannels = 3;
        if (direction == IccTransformDirection.DeviceToPcs)
        {
            matrixStage = CreateMatrixStage(matrixValues, 3, 3, "RGB colorant matrix");
            plan.Stages = new PipelineStage[] { curveStage, matrixStage };
        }
        else
        {
            double[] inverse = MatrixMath.InvertMatrix3x3(matrixValues);
            matrixStage = CreateMatrixStage(inverse, 3, 3, "inverse RGB colorant matrix");
            plan.Stages = new PipelineStage[] { matrixStage, curveStage };
        }

        return plan;
    }

    internal static MatrixTrcPlan CompileGray(IccProfile profile, IccTransformDirection direction)
    {
        IccProfileClass profileClass = profile.Header.ProfileClass;
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display && profileClass != IccProfileClass.Output)
        {
            throw new IccTransformException("A gray TRC plan is defined only for input, display, and output profiles.");
        }

        IccCurve curve = ReadCurve(profile, IccTagSignatures.GrayTrc, "gray TRC", direction);
        PipelineStage curveStage = CreateCurveStage(new IccCurve[] { curve }, direction == IccTransformDirection.PcsToDevice, "gray tone reproduction curve");
        PipelineStage matrixStage;
        MatrixTrcPlan plan = new MatrixTrcPlan();
        plan.Direction = direction;
        plan.IsGray = true;
        if (direction == IccTransformDirection.DeviceToPcs)
        {
            matrixStage = CreateMatrixStage(
                new double[] { IccStandardColorimetry.D50X, IccStandardColorimetry.D50Y, IccStandardColorimetry.D50Z },
                3,
                1,
                "gray luminance to PCS D50 XYZ");
            plan.SourceChannels = 1;
            plan.DestinationChannels = 3;
            plan.Stages = new PipelineStage[] { curveStage, matrixStage };
        }
        else
        {
            matrixStage = CreateMatrixStage(new double[] { 0.0, 1.0, 0.0 }, 1, 3, "PCS XYZ luminance extraction");
            plan.SourceChannels = 3;
            plan.DestinationChannels = 1;
            plan.Stages = new PipelineStage[] { matrixStage, curveStage };
        }

        return plan;
    }

    internal static PipelineStage CreateCurveStage(IccCurve[] curves, bool invert, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Curves;
        stage.InputChannels = curves.Length;
        stage.OutputChannels = curves.Length;
        stage.Curves = curves;
        stage.InvertCurves = invert;
        stage.Description = description;
        return stage;
    }

    internal static PipelineStage CreateMatrixStage(double[] values, int rows, int columns, string description)
    {
        PipelineStage stage = new PipelineStage();
        stage.Kind = PipelineStageKind.Matrix;
        stage.InputChannels = columns;
        stage.OutputChannels = rows;
        stage.Matrix = new IccMatrix();
        stage.Matrix.Rows = rows;
        stage.Matrix.Columns = columns;
        stage.Matrix.Values = values;
        stage.Description = description;
        return stage;
    }

    internal static IccCurve ReadCurve(IccProfile profile, uint signature, string description, IccTransformDirection direction)
    {
        IccTag tag = FindRequiredTag(profile, signature, description);
        if (tag.Data == null || tag.Data.Curve == null)
        {
            throw new IccTransformException($"The {description} tag has no decoded curve data.");
        }

        IccCurve curve = tag.Data.Curve;
        ValidateCurve(curve, description, direction == IccTransformDirection.PcsToDevice);
        return ExtendedTagDataClone.CloneCurve(curve, new ProfileCloneContext());
    }

    internal static void ValidateCurve(IccCurve curve, string description, bool requireInverse)
    {
        CurveMath.EvaluateCurve(curve, 0.0);
        CurveMath.EvaluateCurve(curve, 0.5);
        CurveMath.EvaluateCurve(curve, 1.0);
        if (requireInverse)
        {
            IccCurveAnalysis analysis = CurveAnalyzer.AnalyzeCurve(curve, CurveValidationSamples);
            if (!analysis.IsMonotonic || !analysis.IsContinuous || (analysis.Minimum == analysis.Maximum))
            {
                throw new IccTransformException($"The {description} is not continuously monotonic and cannot be inverted.");
            }
        }
    }

    internal static IccXyzNumber ReadRequiredXyz(IccProfile profile, uint signature, string description)
    {
        IccTag tag = FindRequiredTag(profile, signature, description);
        if (tag.Data == null || tag.Data.XyzValues == null || tag.Data.XyzValues.Length != 1 || tag.Data.XyzValues[0] == null)
        {
            throw new IccTransformException($"The {description} tag must contain exactly one decoded XYZ value.");
        }

        return CloneValidatedXyz(tag.Data.XyzValues[0], description);
    }

    internal static IccXyzNumber? ReadOptionalXyz(IccProfile profile, uint signature, string description)
    {
        IccTag? tag = FindTag(profile, signature);
        if (tag == null)
        {
            return null;
        }
        if (tag.Data == null || tag.Data.XyzValues == null || tag.Data.XyzValues.Length != 1 || tag.Data.XyzValues[0] == null)
        {
            throw new IccTransformException($"The {description} tag must contain exactly one decoded XYZ value.");
        }

        return CloneValidatedXyz(tag.Data.XyzValues[0], description);
    }

    internal static IccXyzNumber CloneValidatedXyz(IccXyzNumber value, string description)
    {
        if (!System.Double.IsFinite(value.X) || !System.Double.IsFinite(value.Y) || !System.Double.IsFinite(value.Z))
        {
            throw new IccTransformException($"The {description} XYZ value must be finite.");
        }

        IccXyzNumber clone = new IccXyzNumber();
        clone.X = value.X;
        clone.Y = value.Y;
        clone.Z = value.Z;
        return clone;
    }

    internal static IccTag FindRequiredTag(IccProfile profile, uint signature, string description)
    {
        IccTag? tag = FindTag(profile, signature);
        if (tag == null)
        {
            throw new IccTransformException($"The profile does not contain its required {description} tag.");
        }

        return tag;
    }

    internal static IccTag? FindTag(IccProfile profile, uint signature)
    {
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

    internal static void ValidateRequest(IccProfile profile, IccTransformDirection direction)
    {
        if (profile == null || profile.Header == null || profile.Tags == null)
        {
            throw new IccTransformException("A profile, header, and tag collection are required to compile a matrix/TRC plan.");
        }
        if (direction != IccTransformDirection.DeviceToPcs && direction != IccTransformDirection.PcsToDevice)
        {
            throw new IccTransformException("A matrix/TRC plan supports only device-to-PCS and PCS-to-device directions.");
        }

        IccPcsKind pcs = profile.Header.ProfileConnectionSpace;
        uint signature = profile.Header.ProfileConnectionSpaceSignature;
        bool kindAllowsXyz = pcs == IccPcsKind.Xyz || pcs == IccPcsKind.None;
        bool signatureAllowsXyz = signature == IccColorSpaceSignatures.Xyz || signature == 0;
        bool identifiesXyz = pcs == IccPcsKind.Xyz || signature == IccColorSpaceSignatures.Xyz;
        if (!kindAllowsXyz || !signatureAllowsXyz || !identifiesXyz)
        {
            throw new IccTransformException("A matrix/TRC plan requires the CIE XYZ Profile Connection Space.");
        }
    }
}
