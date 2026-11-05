namespace icc;

internal class MatrixProfileFactory
{
    internal ProfileConstructionCore Core;

    internal MatrixProfileFactory(ProfileConstructionCore core)
    {
        Core = core;
    }

    internal IccProfile CreateRgbProfile(IccProfileBuildOptions options)
    {
        Core.ValidateBuildOptions(options);
        IccRgbProfileDefinition definition = ValidateRgbOptions(options);
        double[] sourceWhite = CreateNormalizedWhite(definition.WhitePoint);
        double[] sourceMatrix = CreateSourceMatrix(definition, sourceWhite);
        ValidateFiniteMatrix(sourceMatrix, "source RGB colorant matrix");
        double[] adaptation = CreateAdaptationMatrix(options, sourceWhite);
        double[] colorantMatrix = MatrixMath.MultiplyMatrices3x3(adaptation, sourceMatrix);
        ValidateColorantMatrix(colorantMatrix);

        IccProfile profile = new IccProfile();
        profile.Header = Core.CreateSkeletonHeader(options);
        System.Collections.Generic.List<IccTag> tags =
            new System.Collections.Generic.List<IccTag>(Core.CreateSkeletonTags(options));
        AddGeneratedAdaptation(tags, options, definition.WhitePoint, adaptation);
        tags.Add(CreateXyzTag(IccTagSignatures.RedMatrixColumn,
            colorantMatrix[0], colorantMatrix[3], colorantMatrix[6]));
        tags.Add(CreateXyzTag(IccTagSignatures.GreenMatrixColumn,
            colorantMatrix[1], colorantMatrix[4], colorantMatrix[7]));
        tags.Add(CreateXyzTag(IccTagSignatures.BlueMatrixColumn,
            colorantMatrix[2], colorantMatrix[5], colorantMatrix[8]));
        tags.Add(CreateCurveTag(IccTagSignatures.RedTrc, definition.RedCurve,
            options.Version.Kind));
        tags.Add(CreateCurveTag(IccTagSignatures.GreenTrc, definition.GreenCurve,
            options.Version.Kind));
        tags.Add(CreateCurveTag(IccTagSignatures.BlueTrc, definition.BlueCurve,
            options.Version.Kind));
        if (options.ProfileClass != IccProfileClass.ColorSpace)
        {
            tags.Add(CreateChromaticityTag(definition));
        }
        if (options.ProfileClass == IccProfileClass.Display
            && definition.WhitePoint.Luminance > 0.0)
        {
            tags.Add(CreateXyzTag(IccTagSignatures.Luminance,
                0.0, definition.WhitePoint.Luminance, 0.0));
        }
        if (definition.Cicp != null)
        {
            tags.Add(CreateCicpTag(definition.Cicp));
        }
        if (options.AdaptiveGainCurve != null)
        {
            tags.Add(CreateAdaptiveGainTag(options.AdaptiveGainCurve));
        }

        profile.Tags = tags.ToArray();
        profile.TagTableEntries = Core.BuildTableEntries(profile.Tags);
        return Core.PreflightProfile(profile);
    }

    internal IccRgbProfileDefinition ValidateRgbOptions(IccProfileBuildOptions options)
    {
        IccRgbProfileDefinition? candidate = options.RgbDefinition;
        if (candidate == null)
        {
            throw new IccProfileValidationException("RGB profile construction requires an RGB colourimetric definition.");
        }
        if (options.ProfileClass != IccProfileClass.Input
            && options.ProfileClass != IccProfileClass.Display
            && options.ProfileClass != IccProfileClass.ColorSpace)
        {
            throw new IccProfileValidationException("An RGB matrix/TRC profile must use the Input, Display, or ColorSpace class.");
        }
        if (options.DataColorSpaceSignature != IccColorSpaceSignatures.Rgb
            || options.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Xyz)
        {
            throw new IccProfileValidationException("An RGB matrix/TRC profile requires the RGB data space and PCSXYZ.");
        }
        if (!Core.IsD50(options.MediaWhitePoint))
        {
            throw new IccProfileValidationException("An RGB matrix/TRC profile media white point must be PCS D50.");
        }

        IccRgbProfileDefinition definition = candidate;
        ValidateChromaticity(definition.RedPrimary, "RGB red primary", false);
        ValidateChromaticity(definition.GreenPrimary, "RGB green primary", false);
        ValidateChromaticity(definition.BluePrimary, "RGB blue primary", false);
        ValidateChromaticity(definition.WhitePoint, "RGB source white point", true);
        if (options.ProfileClass == IccProfileClass.Display
            && definition.WhitePoint.Luminance > IccNumberCodec.S15Fixed16Maximum)
        {
            throw new IccProfileValidationException("RGB display luminance must fit the ICC XYZ signed 15.16 representation.");
        }
        ValidatePrimaryLuminances(definition);
        ValidateCurve(definition.RedCurve, "RGB red", options.Version.Kind);
        ValidateCurve(definition.GreenCurve, "RGB green", options.Version.Kind);
        ValidateCurve(definition.BlueCurve, "RGB blue", options.Version.Kind);
        ValidateCicp(definition.Cicp, options);
        ValidateAdaptiveGain(options.AdaptiveGainCurve, options);
        return definition;
    }

    internal void ValidateChromaticity(IccXyYColor value, string description, bool white)
    {
        if (value == null || !System.Double.IsFinite(value.X)
            || !System.Double.IsFinite(value.Y) || !System.Double.IsFinite(value.Luminance)
            || value.X < 0.0 || value.X > 1.0 || value.Y <= 0.0 || value.Y > 1.0
            || value.X + value.Y > 1.0 || value.Luminance < 0.0)
        {
            throw new IccProfileValidationException($"The {description} must contain valid finite xy or xyY coordinates.");
        }
        double normalizedX = value.X / value.Y;
        double normalizedZ = (1.0 - value.X - value.Y) / value.Y;
        if (!System.Double.IsFinite(normalizedX) || !System.Double.IsFinite(normalizedZ))
        {
            throw new IccProfileValidationException($"The {description} cannot be converted to finite XYZ coordinates.");
        }
        if (white)
        {
            double[] normalized = CreateNormalizedWhite(value);
            double[] cone = MatrixMath.ApplyMatrix3x3(
                ChromaticAdaptationCore.CreateBradfordMatrix(), normalized);
            if (cone[0] <= 0.0 || cone[1] <= 0.0 || cone[2] <= 0.0)
            {
                throw new IccProfileValidationException($"The {description} must produce positive Bradford cone responses.");
            }
        }
    }

    internal void ValidatePrimaryLuminances(IccRgbProfileDefinition definition)
    {
        bool red = definition.RedPrimary.Luminance > 0.0;
        bool green = definition.GreenPrimary.Luminance > 0.0;
        bool blue = definition.BluePrimary.Luminance > 0.0;
        if ((red || green || blue) && !(red && green && blue))
        {
            throw new IccProfileValidationException("RGB primary luminances must either all be positive xyY values or all be zero xy chromaticities.");
        }
    }

    internal void ValidateCurve(IccCurve curve, string description, IccVersionKind version)
    {
        if (curve == null || curve.Samples == null)
        {
            throw new IccProfileValidationException($"The {description} tone curve is incomplete or cannot be encoded for the selected generation.");
        }

        bool valid;
        if (curve.Kind == IccCurveKind.Identity)
        {
            valid = curve.Samples.Length == 0;
        }
        else if (curve.Kind == IccCurveKind.Gamma)
        {
            valid = System.Double.IsFinite(curve.Gamma) && curve.Gamma > 0.0
                && curve.Gamma <= IccNumberCodec.U8Fixed8Maximum;
        }
        else if (curve.Kind == IccCurveKind.Sampled)
        {
            valid = curve.Samples.Length >= 2 && AreNormalizedValues(curve.Samples);
        }
        else if (curve.Kind == IccCurveKind.Parametric)
        {
            valid = version == IccVersionKind.Version4
                && Icc1TagValidationCore.IsValidParametricCurve(curve.Parametric);
        }
        else
        {
            valid = false;
        }

        if (!valid)
        {
            throw new IccProfileValidationException($"The {description} tone curve is incomplete or cannot be encoded for the selected generation.");
        }

        IccCurveAnalysis analysis = CurveAnalyzer.AnalyzeCurve(curve,
            MatrixTrcCompiler.CurveValidationSamples);
        if (!analysis.IsMonotonic || !analysis.IsIncreasing || !analysis.IsContinuous
            || analysis.Minimum == analysis.Maximum)
        {
            throw new IccProfileValidationException($"The {description} tone curve must be continuously increasing and invertible.");
        }
    }

    internal void ValidateCicp(IccCicp? cicp, IccProfileBuildOptions options)
    {
        if (cicp == null)
        {
            return;
        }
        if (options.Version.Kind != IccVersionKind.Version4
            || (options.ProfileClass != IccProfileClass.Input
                && options.ProfileClass != IccProfileClass.Display))
        {
            throw new IccProfileValidationException("CICP metadata requires a version 4 Input or Display RGB profile.");
        }
        if (cicp.ColorPrimaries < 0 || cicp.ColorPrimaries > byte.MaxValue
            || cicp.TransferCharacteristics < 0
            || cicp.TransferCharacteristics > byte.MaxValue
            || cicp.MatrixCoefficients != 0)
        {
            throw new IccProfileValidationException("RGB CICP metadata requires byte-sized code points and zero matrix coefficients.");
        }
    }

    internal void ValidateAdaptiveGain(IccAdaptiveGainCurve? curve,
        IccProfileBuildOptions options)
    {
        if (curve == null)
        {
            return;
        }
        if (options.Version.Kind != IccVersionKind.Version4
            || (options.ProfileClass != IccProfileClass.Input
                && options.ProfileClass != IccProfileClass.Display))
        {
            throw new IccProfileValidationException("Adaptive Gain Curve metadata requires a version 4 RGB Input or Display profile.");
        }
        if (curve.IsImageSpecific)
        {
            throw new IccProfileValidationException("Image-specific Adaptive Gain Curve metadata requires embedded-profile flags that basic RGB construction does not set.");
        }
        AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(curve,
            options.ProfileClass, IccColorSpaceKind.Rgb, false, false);
    }

    internal bool AreNormalizedValues(double[] values)
    {
        int index = 0;
        double previous = -1.0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!System.Double.IsFinite(value) || value < 0.0 || value > 1.0
                || value < previous)
            {
                return false;
            }
            previous = value;
            index = index + 1;
        }
        return values[0] < values[values.Length - 1];
    }

    internal double[] CreateNormalizedWhite(IccXyYColor white)
    {
        return new double[]
        {
            white.X / white.Y,
            1.0,
            (1.0 - white.X - white.Y) / white.Y
        };
    }

    internal double[] CreateSourceMatrix(IccRgbProfileDefinition definition, double[] sourceWhite)
    {
        double[] basis = new double[]
        {
            definition.RedPrimary.X / definition.RedPrimary.Y,
            definition.GreenPrimary.X / definition.GreenPrimary.Y,
            definition.BluePrimary.X / definition.BluePrimary.Y,
            1.0,
            1.0,
            1.0,
            (1.0 - definition.RedPrimary.X - definition.RedPrimary.Y)
                / definition.RedPrimary.Y,
            (1.0 - definition.GreenPrimary.X - definition.GreenPrimary.Y)
                / definition.GreenPrimary.Y,
            (1.0 - definition.BluePrimary.X - definition.BluePrimary.Y)
                / definition.BluePrimary.Y
        };
        double determinant = MatrixMath.CalculateDeterminant3x3(basis);
        if (!System.Double.IsFinite(determinant) || System.Math.Abs(determinant) <= 1.0e-12)
        {
            throw new IccProfileValidationException("The RGB primary chromaticities form a singular colourant matrix.");
        }

        if (definition.RedPrimary.Luminance > 0.0)
        {
            return CreateMeasuredPrimaryMatrix(definition, sourceWhite);
        }

        double[] inverse = MatrixMath.InvertMatrix3x3(basis);
        double[] scales = MatrixMath.ApplyMatrix3x3(inverse, sourceWhite);
        int index = 0;
        while (index < scales.Length)
        {
            if (!System.Double.IsFinite(scales[index]) || scales[index] <= 0.0)
            {
                throw new IccProfileValidationException("The RGB source white lies outside the positive gamut of the supplied primaries.");
            }
            index = index + 1;
        }
        return ScalePrimaryColumns(basis, scales);
    }

    internal double[] CreateMeasuredPrimaryMatrix(IccRgbProfileDefinition definition,
        double[] sourceWhite)
    {
        double[] red = ConvertXyYToXyz(definition.RedPrimary);
        double[] green = ConvertXyYToXyz(definition.GreenPrimary);
        double[] blue = ConvertXyYToXyz(definition.BluePrimary);
        double totalY = red[1] + green[1] + blue[1];
        if (!System.Double.IsFinite(totalY) || totalY <= 0.0)
        {
            throw new IccProfileValidationException("The RGB primary luminances do not define a positive white luminance.");
        }
        double[] matrix = new double[]
        {
            red[0] / totalY, green[0] / totalY, blue[0] / totalY,
            red[1] / totalY, green[1] / totalY, blue[1] / totalY,
            red[2] / totalY, green[2] / totalY, blue[2] / totalY
        };
        double[] summedWhite = new double[]
        {
            matrix[0] + matrix[1] + matrix[2],
            matrix[3] + matrix[4] + matrix[5],
            matrix[6] + matrix[7] + matrix[8]
        };
        if (!AreCloseWhitePoints(summedWhite, sourceWhite, 0.0005))
        {
            throw new IccProfileValidationException("The RGB primary xyY luminances do not sum to the supplied source-white chromaticity.");
        }
        return matrix;
    }

    internal double[] ConvertXyYToXyz(IccXyYColor value)
    {
        double scale = value.Luminance / value.Y;
        return new double[]
        {
            value.X * scale,
            value.Luminance,
            (1.0 - value.X - value.Y) * scale
        };
    }

    internal double[] ScalePrimaryColumns(double[] basis, double[] scales)
    {
        double[] matrix = new double[9];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                matrix[(row * 3) + column] = basis[(row * 3) + column]
                    * scales[column];
                column = column + 1;
            }
            row = row + 1;
        }
        return matrix;
    }

    internal double[] CreateAdaptationMatrix(IccProfileBuildOptions options,
        double[] sourceWhite)
    {
        double[] adaptation = options.ChromaticAdaptationMatrix == null
            ? ChromaticAdaptationCore.BuildToD50Matrix(sourceWhite)
            : Core.CloneDoubles(options.ChromaticAdaptationMatrix.Values);
        double[] mappedWhite = MatrixMath.ApplyMatrix3x3(adaptation, sourceWhite);
        double[] d50 = ChromaticAdaptationCore.CreateD50WhitePoint();
        if (!AreCloseWhitePoints(mappedWhite, d50, 0.0005))
        {
            throw new IccProfileValidationException("The chromatic-adaptation matrix does not map the source white to PCS D50.");
        }
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = 3;
        matrix.Columns = 3;
        matrix.Values = adaptation;
        Core.ValidateChromaticAdaptationMatrix(matrix);
        return adaptation;
    }

    internal void ValidateFiniteMatrix(double[] matrix, string description)
    {
        int index = 0;
        while (index < matrix.Length)
        {
            if (!System.Double.IsFinite(matrix[index]))
            {
                throw new IccProfileValidationException($"The {description} must contain only finite values.");
            }
            index = index + 1;
        }
    }

    internal void ValidateColorantMatrix(double[] matrix)
    {
        int index = 0;
        while (index < matrix.Length)
        {
            if (!Core.IsRepresentable(matrix[index]))
            {
                throw new IccProfileValidationException("The adapted RGB colorant matrix cannot be represented as ICC XYZ values.");
            }
            index = index + 1;
        }
        double determinant = MatrixMath.CalculateDeterminant3x3(matrix);
        if (!System.Double.IsFinite(determinant) || System.Math.Abs(determinant) <= 1.0e-12)
        {
            throw new IccProfileValidationException("The adapted RGB colorant matrix must be invertible.");
        }
    }

    internal void AddGeneratedAdaptation(System.Collections.Generic.List<IccTag> tags,
        IccProfileBuildOptions options, IccXyYColor sourceWhite, double[] adaptation)
    {
        if (options.ChromaticAdaptationMatrix != null || IsD50Chromaticity(sourceWhite))
        {
            return;
        }
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = 3;
        matrix.Columns = 3;
        matrix.Values = adaptation;
        tags.Add(Core.CreateChromaticAdaptationTag(matrix));
    }

    internal IccTag CreateXyzTag(uint signature, double x, double y, double z)
    {
        IccXyzColor value = new IccXyzColor();
        value.X = x;
        value.Y = y;
        value.Z = z;
        return CreateXyzTagFromColor(signature, value);
    }

    internal IccTag CreateXyzTagFromColor(uint signature, IccXyzColor value)
    {
        IccTag tag = Core.CreateMediaWhiteTag(value);
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        return tag;
    }

    internal IccTag CreateCurveTag(uint signature, IccCurve curve,
        IccVersionKind version)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Curve;
        data.Curve = ExtendedTagDataClone.CloneCurve(curve, new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = curve.Kind == IccCurveKind.Parametric
            && version == IccVersionKind.Version4
            ? IccTypeSignatures.ParametricCurve : IccTypeSignatures.Curve;
        return Core.CreateTag(signature, data);
    }

    internal IccTag CreateChromaticityTag(IccRgbProfileDefinition definition)
    {
        IccChromaticity chromaticity = new IccChromaticity();
        chromaticity.XCoordinates = new double[]
        {
            definition.RedPrimary.X,
            definition.GreenPrimary.X,
            definition.BluePrimary.X
        };
        chromaticity.YCoordinates = new double[]
        {
            definition.RedPrimary.Y,
            definition.GreenPrimary.Y,
            definition.BluePrimary.Y
        };
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Chromaticity;
        data.Chromaticity = chromaticity;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Chromaticity;
        return Core.CreateTag(IccTagSignatures.Chromaticity, data);
    }

    internal IccTag CreateCicpTag(IccCicp value)
    {
        IccCicp clone = new IccCicp();
        clone.ColorPrimaries = value.ColorPrimaries;
        clone.TransferCharacteristics = value.TransferCharacteristics;
        clone.MatrixCoefficients = value.MatrixCoefficients;
        clone.FullRange = value.FullRange;
        clone.UsesProfilePrimaries = value.ColorPrimaries == 2;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Cicp;
        data.Cicp = clone;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Cicp;
        return Core.CreateTag(IccTagSignatures.Cicp, data);
    }

    internal IccTag CreateAdaptiveGainTag(IccAdaptiveGainCurve value)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.AdaptiveGainCurve;
        data.AdaptiveGainCurve = ExtendedTagDataClone.CloneAdaptiveGainCurve(
            value, new ProfileCloneContext());
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.AdaptiveGainCurve;
        return Core.CreateTag(IccTagSignatures.AdaptiveGainCurve, data);
    }

    internal bool AreCloseWhitePoints(double[] first, double[] second, double tolerance)
    {
        int index = 0;
        while (index < 3)
        {
            if (!System.Double.IsFinite(first[index]) || !System.Double.IsFinite(second[index])
                || System.Math.Abs(first[index] - second[index]) > tolerance)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsD50Chromaticity(IccXyYColor white)
    {
        return System.Math.Abs(white.X - IccStandardColorimetry.D50ChromaticityX) <= 0.000001
            && System.Math.Abs(white.Y - IccStandardColorimetry.D50ChromaticityY) <= 0.000001;
    }
}
