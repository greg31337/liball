namespace icc;

internal static class ExtendedTagDataClone
{
    internal static void CopyFields(IccTagData source, IccTagData clone, string path, ProfileCloneContext context)
    {
        clone.IntegerValues = CloneLongArray(source.IntegerValues, $"{path} integer values");
        clone.FixedPointValues = CloneDoubleArray(source.FixedPointValues, $"{path} fixed-point values");
        clone.FloatingPointValues = CloneDoubleArray(source.FloatingPointValues, $"{path} floating-point values");
        clone.XyzValues = CloneXyzNumberArray(source.XyzValues, $"{path} XYZ values");
        clone.Signature = source.Signature;
        clone.DateTime = source.DateTime == null ? null : ProfileClone.CloneDateTime(source.DateTime);
        clone.BinaryData = ProfileClone.CloneBytes(source.BinaryData, $"{path} binary data", context);
        clone.Text = source.Text == null ? null : CloneTextData(source.Text, context);
        clone.LocalizedStrings = CloneLocalizedStringArray(source.LocalizedStrings, $"{path} localized strings", context);
        clone.Metadata = CloneMetadataEntryArray(source.Metadata, $"{path} metadata", context);
        clone.Curve = source.Curve == null ? null : CloneCurve(source.Curve, context);
        clone.Lut = source.Lut == null ? null : CloneLut(source.Lut, context);
        clone.MultiStageLut = source.MultiStageLut == null ? null : CloneMultiStageLut(source.MultiStageLut, context);
        clone.MultiProcessElements = source.MultiProcessElements == null ? null : CloneMultiProcessElements(source.MultiProcessElements, context);
        clone.Measurement = source.Measurement == null ? null : CloneMeasurement(source.Measurement);
        clone.ViewingConditions = source.ViewingConditions == null ? null : CloneViewingConditions(source.ViewingConditions);
        clone.Chromaticity = source.Chromaticity == null ? null : CloneChromaticity(source.Chromaticity);
        clone.Cicp = source.Cicp == null ? null : CloneCicp(source.Cicp);
        clone.NamedColors = source.NamedColors == null ? null : CloneNamedColorSet(source.NamedColors, context);
        clone.Colorants = source.Colorants == null ? null : CloneColorantSet(source.Colorants);
        clone.ResponseCurves = CloneResponseCurveArray(source.ResponseCurves, $"{path} response curves");
        clone.ResponseCurveChannelCount = source.ResponseCurveChannelCount;
        clone.ProfileSequence = CloneProfileSequenceEntryArray(source.ProfileSequence, $"{path} profile sequence", context);
        clone.ProfileSequenceIdentifiers = CloneProfileSequenceIdentifierArray(source.ProfileSequenceIdentifiers, $"{path} profile sequence identifiers", context);
        clone.AdaptiveGainCurve = source.AdaptiveGainCurve == null ? null : CloneAdaptiveGainCurve(source.AdaptiveGainCurve, context);
        clone.CrdInfo = source.CrdInfo == null ? null : CloneCrdInfo(source.CrdInfo);
        clone.Screening = source.Screening == null ? null : CloneScreening(source.Screening);
        clone.UcrBg = source.UcrBg == null ? null : CloneUcrBg(source.UcrBg);
        clone.VendorData = source.VendorData == null ? null : CloneVendorTagData(source.VendorData, context);
        clone.SpectralData = source.SpectralData == null ? null : CloneSpectralData(source.SpectralData);
        clone.SpectralViewingConditions = source.SpectralViewingConditions == null ? null : CloneSpectralViewingConditions(source.SpectralViewingConditions);
        clone.SpectralRanges = source.SpectralRanges == null ? null : CloneSpectralRanges(source.SpectralRanges);
        clone.EmbeddedProfile = source.EmbeddedProfile == null ? null : ProfileClone.CloneProfileWithContext(source.EmbeddedProfile, context);
        clone.SparseMatrixArray = source.SparseMatrixArray == null ? null : CloneSparseMatrixArray(source.SparseMatrixArray);
        clone.TagArray = source.TagArray == null ? null : CloneHierarchicalTagArray(source.TagArray, path, context);
        clone.TagStructure = source.TagStructure == null ? null : CloneHierarchicalTagStructure(source.TagStructure, path, context);
        clone.GamutBoundary = source.GamutBoundary == null ? null : CloneGamutBoundary(source.GamutBoundary);
        clone.EmbeddedImage = source.EmbeddedImage == null ? null : CloneEmbeddedImage(source.EmbeddedImage, context);
        clone.Calculator = source.Calculator == null ? null : CloneCalculatorProgram(source.Calculator, path, context);
        clone.ColorEncodingParameters = source.ColorEncodingParameters == null ? null : CloneColorEncodingParameters(source.ColorEncodingParameters, context);
        clone.ProfileConnectionConditions = source.ProfileConnectionConditions == null ? null : CloneProfileConnectionConditions(source.ProfileConnectionConditions, context);
        clone.MultiplexChannels = source.MultiplexChannels == null ? null : CloneMultiplexChannels(source.MultiplexChannels);
        clone.Brdf = source.Brdf == null ? null : CloneBrdf(source.Brdf, context);
        clone.SurfaceMap = source.SurfaceMap == null ? null : CloneSurfaceMap(source.SurfaceMap, context);
        if (clone.SurfaceMap != null && source.SurfaceMap != null && source.EmbeddedImage != null)
        {
            if (source.SurfaceMap.HeightImage == source.EmbeddedImage)
            {
                clone.SurfaceMap.HeightImage = clone.EmbeddedImage;
            }

            if (source.SurfaceMap.NormalImage == source.EmbeddedImage)
            {
                clone.SurfaceMap.NormalImage = clone.EmbeddedImage;
            }
        }
    }

    internal static long[] CloneLongArray(long[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        long[] clone = new long[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static IccCrdInfo CloneCrdInfo(IccCrdInfo source)
    {
        IccCrdInfo clone = new IccCrdInfo();
        clone.ProductName = source.ProductName;
        clone.RenderingIntentCrdNames = CloneStringArray(source.RenderingIntentCrdNames, "CRD rendering-intent names");
        return clone;
    }

    internal static IccScreeningChannel CloneScreeningChannel(IccScreeningChannel source)
    {
        IccScreeningChannel clone = new IccScreeningChannel();
        clone.Frequency = source.Frequency;
        clone.AngleDegrees = source.AngleDegrees;
        clone.SpotShape = source.SpotShape;
        return clone;
    }

    internal static IccScreening CloneScreening(IccScreening source)
    {
        if (source.Channels == null)
        {
            throw new IccProfileValidationException("The screening channels to clone cannot be null.");
        }

        IccScreening clone = new IccScreening();
        clone.UsesPrinterDefaultScreens = source.UsesPrinterDefaultScreens;
        clone.FrequencyIsLinesPerInch = source.FrequencyIsLinesPerInch;
        clone.Channels = new IccScreeningChannel[source.Channels.Length];
        int index = 0;
        while (index < source.Channels.Length)
        {
            clone.Channels[index] = CloneScreeningChannel(source.Channels[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccUcrBg CloneUcrBg(IccUcrBg source)
    {
        IccUcrBg clone = new IccUcrBg();
        clone.UnderColorRemovalValues = CloneUShortArray(source.UnderColorRemovalValues, "UCR values");
        clone.BlackGenerationValues = CloneUShortArray(source.BlackGenerationValues, "black-generation values");
        clone.Description = source.Description;
        return clone;
    }

    internal static IccVendorTagData CloneVendorTagData(IccVendorTagData source, ProfileCloneContext context)
    {
        IccVendorTagData clone = new IccVendorTagData();
        clone.VendorName = source.VendorName;
        clone.FormatName = source.FormatName;
        clone.TagSignature = source.TagSignature;
        clone.TypeSignature = source.TypeSignature;
        clone.VideoCardGamma = source.VideoCardGamma == null ? null : CloneVideoCardGamma(source.VideoCardGamma, context);
        clone.MicrosoftHardwareCalibration = source.MicrosoftHardwareCalibration == null ? null : CloneMicrosoftHardwareCalibration(source.MicrosoftHardwareCalibration);
        return clone;
    }

    internal static IccVideoCardGamma CloneVideoCardGamma(IccVideoCardGamma source, ProfileCloneContext context)
    {
        IccVideoCardGamma clone = new IccVideoCardGamma();
        clone.IsFormula = source.IsFormula;
        clone.ChannelCount = source.ChannelCount;
        clone.EntryCount = source.EntryCount;
        clone.EntrySize = source.EntrySize;
        clone.TableData = ProfileClone.CloneBytes(source.TableData, "video-card gamma table data", context);
        clone.RedFormula = CloneDoubleArray(source.RedFormula, "video-card red formula");
        clone.GreenFormula = CloneDoubleArray(source.GreenFormula, "video-card green formula");
        clone.BlueFormula = CloneDoubleArray(source.BlueFormula, "video-card blue formula");
        return clone;
    }

    internal static IccMicrosoftHardwareCalibration CloneMicrosoftHardwareCalibration(IccMicrosoftHardwareCalibration source)
    {
        IccMicrosoftHardwareCalibration clone = new IccMicrosoftHardwareCalibration();
        clone.LutEntryCount = source.LutEntryCount;
        clone.MinimumLuminance = source.MinimumLuminance;
        clone.PeakLuminance = source.PeakLuminance;
        clone.Matrix = CloneDoubleArray(source.Matrix, "Microsoft hardware-calibration matrix");
        clone.RedLut = CloneDoubleArray(source.RedLut, "Microsoft hardware-calibration red LUT");
        clone.GreenLut = CloneDoubleArray(source.GreenLut, "Microsoft hardware-calibration green LUT");
        clone.BlueLut = CloneDoubleArray(source.BlueLut, "Microsoft hardware-calibration blue LUT");
        return clone;
    }

    internal static double[] CloneDoubleArray(double[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        double[] clone = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static int[] CloneIntArray(int[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        int[] clone = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static string[] CloneStringArray(string[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        string[] clone = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static IccXyzNumber CloneXyzNumber(IccXyzNumber source)
    {
        IccXyzNumber clone = new IccXyzNumber();
        clone.X = source.X;
        clone.Y = source.Y;
        clone.Z = source.Z;
        return clone;
    }

    internal static IccXyzNumber[] CloneXyzNumberArray(IccXyzNumber[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccXyzNumber[] clone = new IccXyzNumber[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneXyzNumber(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccXyzColor CloneXyzColor(IccXyzColor source)
    {
        IccXyzColor clone = new IccXyzColor();
        clone.X = source.X;
        clone.Y = source.Y;
        clone.Z = source.Z;
        clone.IsAbsolute = source.IsAbsolute;
        return clone;
    }

    internal static IccLabColor CloneLabColor(IccLabColor source)
    {
        IccLabColor clone = new IccLabColor();
        clone.L = source.L;
        clone.A = source.A;
        clone.B = source.B;
        return clone;
    }

    internal static IccXyYColor CloneXyYColor(IccXyYColor source)
    {
        IccXyYColor clone = new IccXyYColor();
        clone.X = source.X;
        clone.Y = source.Y;
        clone.Luminance = source.Luminance;
        return clone;
    }

    internal static IccLchColor CloneLchColor(IccLchColor source)
    {
        IccLchColor clone = new IccLchColor();
        clone.L = source.L;
        clone.C = source.C;
        clone.HueDegrees = source.HueDegrees;
        return clone;
    }

    internal static IccColorVector CloneColorVector(IccColorVector source)
    {
        IccColorVector clone = new IccColorVector();
        clone.ColorSpace = source.ColorSpace;
        clone.ColorSpaceSignature = source.ColorSpaceSignature;
        clone.Components = CloneDoubleArray(source.Components, "colour-vector components");
        return clone;
    }

    internal static IccMatrix CloneMatrix(IccMatrix source)
    {
        IccMatrix clone = new IccMatrix();
        clone.Rows = source.Rows;
        clone.Columns = source.Columns;
        clone.Values = CloneDoubleArray(source.Values, "matrix values");
        return clone;
    }

    internal static IccChromaticity CloneChromaticity(IccChromaticity source)
    {
        IccChromaticity clone = new IccChromaticity();
        clone.ColorantEncoding = source.ColorantEncoding;
        clone.XCoordinates = CloneDoubleArray(source.XCoordinates, "chromaticity x coordinates");
        clone.YCoordinates = CloneDoubleArray(source.YCoordinates, "chromaticity y coordinates");
        return clone;
    }

    internal static IccEncodingRange CloneEncodingRange(IccEncodingRange source)
    {
        IccEncodingRange clone = new IccEncodingRange();
        clone.Minimum = source.Minimum;
        clone.Maximum = source.Maximum;
        return clone;
    }

    internal static IccEncodingRange[] CloneEncodingRangeArray(IccEncodingRange[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccEncodingRange[] clone = new IccEncodingRange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneEncodingRange(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccViewingConditions CloneViewingConditions(IccViewingConditions source)
    {
        IccViewingConditions clone = new IccViewingConditions();
        clone.Illuminant = CloneXyzColor(source.Illuminant);
        clone.Surround = CloneXyzColor(source.Surround);
        clone.IlluminantKind = source.IlluminantKind;
        return clone;
    }

    internal static IccColorDescription CloneColorDescription(IccColorDescription source)
    {
        IccColorDescription clone = new IccColorDescription();
        clone.Name = source.Name;
        clone.Xyz = source.Xyz == null ? null : CloneXyzColor(source.Xyz);
        clone.Lab = source.Lab == null ? null : CloneLabColor(source.Lab);
        clone.XyY = source.XyY == null ? null : CloneXyYColor(source.XyY);
        clone.Lch = source.Lch == null ? null : CloneLchColor(source.Lch);
        clone.DeviceColor = source.DeviceColor == null ? null : CloneColorVector(source.DeviceColor);
        return clone;
    }

    internal static IccParametricCurve CloneParametricCurve(IccParametricCurve source)
    {
        IccParametricCurve clone = new IccParametricCurve();
        clone.FunctionType = source.FunctionType;
        clone.Parameters = CloneDoubleArray(source.Parameters, "parametric-curve parameters");
        return clone;
    }

    internal static IccCurveSegment CloneCurveSegment(IccCurveSegment source)
    {
        IccCurveSegment clone = new IccCurveSegment();
        clone.Kind = source.Kind;
        clone.FormulaType = source.FormulaType;
        clone.BreakPoint = source.BreakPoint;
        clone.Parameters = CloneDoubleArray(source.Parameters, "curve-segment parameters");
        clone.Samples = CloneDoubleArray(source.Samples, "curve-segment samples");
        return clone;
    }

    internal static IccCurveSegment[] CloneCurveSegmentArray(IccCurveSegment[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccCurveSegment[] clone = new IccCurveSegment[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneCurveSegment(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccCurveAnalysis CloneCurveAnalysis(IccCurveAnalysis source, ProfileCloneContext context)
    {
        IccCurveAnalysis clone = new IccCurveAnalysis();
        clone.IsMonotonic = source.IsMonotonic;
        clone.IsIncreasing = source.IsIncreasing;
        clone.IsDecreasing = source.IsDecreasing;
        clone.IsContinuous = source.IsContinuous;
        clone.IsInvertible = source.IsInvertible;
        clone.DomainMinimum = source.DomainMinimum;
        clone.DomainMaximum = source.DomainMaximum;
        clone.Minimum = source.Minimum;
        clone.Maximum = source.Maximum;
        clone.MinimumSlope = source.MinimumSlope;
        clone.MaximumSlope = source.MaximumSlope;
        clone.MaximumRoundTripError = source.MaximumRoundTripError;
        clone.SampleCount = source.SampleCount;
        clone.Result = ProfileClone.CloneOperationResult(source.Result, "curve-analysis result", context);
        return clone;
    }

    internal static IccCurve CloneCurve(IccCurve source, ProfileCloneContext context)
    {
        IccCurve clone = new IccCurve();
        clone.Kind = source.Kind;
        clone.Gamma = source.Gamma;
        clone.Samples = CloneDoubleArray(source.Samples, "curve samples");
        clone.Parametric = source.Parametric == null ? null : CloneParametricCurve(source.Parametric);
        clone.Segments = CloneCurveSegmentArray(source.Segments, "curve segments");
        clone.Analysis = source.Analysis == null ? null : CloneCurveAnalysis(source.Analysis, context);
        clone.InputMinimum = source.InputMinimum;
        clone.InputMaximum = source.InputMaximum;
        clone.Extension = source.Extension;
        clone.Encoding = source.Encoding;
        return clone;
    }

    internal static IccCurve[] CloneCurveArray(IccCurve[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccCurve[] clone = new IccCurve[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneCurve(source[index], context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccClut CloneClut(IccClut source)
    {
        IccClut clone = new IccClut();
        clone.InputChannels = source.InputChannels;
        clone.OutputChannels = source.OutputChannels;
        clone.GridPoints = CloneIntArray(source.GridPoints, "CLUT grid points");
        clone.Values = CloneDoubleArray(source.Values, "CLUT values");
        clone.Interpolation = source.Interpolation;
        clone.InterpolationHint = source.InterpolationHint;
        clone.Encoding = source.Encoding;
        return clone;
    }

    internal static double[][] CloneDoubleJaggedArray(double[][] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        double[][] clone = new double[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneDoubleArray(source[index], $"{path} row {index}");
            index = index + 1;
        }

        return clone;
    }

    internal static IccLut CloneLut(IccLut source, ProfileCloneContext context)
    {
        IccLut clone = new IccLut();
        clone.InputChannels = source.InputChannels;
        clone.OutputChannels = source.OutputChannels;
        clone.Matrix = CloneMatrix(source.Matrix);
        clone.InputTables = CloneDoubleJaggedArray(source.InputTables, "LUT input tables");
        clone.Clut = CloneClut(source.Clut);
        clone.OutputTables = CloneDoubleJaggedArray(source.OutputTables, "LUT output tables");
        clone.PcsEncoding = source.PcsEncoding;
        return clone;
    }

    internal static IccMultiStageLut CloneMultiStageLut(IccMultiStageLut source, ProfileCloneContext context)
    {
        IccMultiStageLut clone = new IccMultiStageLut();
        clone.InputChannels = source.InputChannels;
        clone.OutputChannels = source.OutputChannels;
        clone.ACurves = CloneCurveArray(source.ACurves, "multi-stage LUT A curves", context);
        clone.BCurves = CloneCurveArray(source.BCurves, "multi-stage LUT B curves", context);
        clone.MCurves = CloneCurveArray(source.MCurves, "multi-stage LUT M curves", context);
        clone.Matrix = source.Matrix == null ? null : CloneMatrix(source.Matrix);
        clone.Clut = source.Clut == null ? null : CloneClut(source.Clut);
        clone.PcsEncoding = source.PcsEncoding;
        return clone;
    }

    internal static IccToneMap CloneToneMap(IccToneMap source, ProfileCloneContext context)
    {
        if (source.LuminanceCurve == null || source.Functions == null)
        {
            throw new IccProfileValidationException("The tone-map curve and functions to clone cannot be null.");
        }

        IccToneMap clone = new IccToneMap();
        clone.LuminanceCurve = CloneCurve(source.LuminanceCurve, context);
        clone.Functions = new IccToneMapFunction[source.Functions.Length];
        int index = 0;
        while (index < source.Functions.Length)
        {
            if (source.Functions[index] == null)
            {
                throw new IccProfileValidationException($"Tone-map function {index} to clone cannot be null.");
            }

            IccToneMapFunction function = new IccToneMapFunction();
            function.FunctionType = source.Functions[index].FunctionType;
            function.Parameters = CloneDoubleArray(source.Functions[index].Parameters, $"tone-map function {index} parameters");
            clone.Functions[index] = function;
            index = index + 1;
        }

        return clone;
    }

    internal static IccMultiProcessElements CloneMultiProcessElements(IccMultiProcessElements source, ProfileCloneContext context)
    {
        IccMultiProcessElements clone = new IccMultiProcessElements();
        clone.InputChannels = source.InputChannels;
        clone.OutputChannels = source.OutputChannels;
        clone.InputRanges = CloneEncodingRangeArray(source.InputRanges, "MPE input ranges");
        clone.OutputRanges = CloneEncodingRangeArray(source.OutputRanges, "MPE output ranges");
        clone.Elements = CloneProcessingElementArray(source.Elements, "MPE elements", context);
        clone.IsExecutable = source.IsExecutable;
        return clone;
    }

    internal static IccProcessingElement[] CloneProcessingElementArray(IccProcessingElement[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccProcessingElement[] clone = new IccProcessingElement[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneProcessingElement(source[index], $"{path} item {index}", context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccProcessingElement CloneProcessingElement(IccProcessingElement source, string path, ProfileCloneContext context)
    {
        if (context.ProcessingElements.ContainsKey(source))
        {
            return context.ProcessingElements[source];
        }

        IccProcessingElement clone = new IccProcessingElement();
        context.ProcessingElements.Add(source, clone);
        clone.Signature = source.Signature;
        clone.Kind = source.Kind;
        clone.InputChannels = source.InputChannels;
        clone.OutputChannels = source.OutputChannels;
        clone.InputRanges = CloneEncodingRangeArray(source.InputRanges, $"{path} input ranges");
        clone.OutputRanges = CloneEncodingRangeArray(source.OutputRanges, $"{path} output ranges");
        clone.Curves = CloneCurveArray(source.Curves, $"{path} curves", context);
        clone.Matrix = source.Matrix == null ? null : CloneMatrix(source.Matrix);
        clone.Clut = source.Clut == null ? null : CloneClut(source.Clut);
        clone.SparseMatrixLut = source.SparseMatrixLut == null ? null : CloneSparseMatrixArray(source.SparseMatrixLut);
        clone.ToneMap = source.ToneMap == null ? null : CloneToneMap(source.ToneMap, context);
        clone.CamViewingConditions = source.CamViewingConditions == null ? null : CloneCamViewingConditions(source.CamViewingConditions);
        clone.SpectralProcessingData = source.SpectralProcessingData == null ? null : CloneSpectralProcessingData(source.SpectralProcessingData);
        clone.AcsSignature = source.AcsSignature;
        clone.AcsData = ProfileClone.CloneBytes(source.AcsData, $"{path} ACS data", context);
        clone.Calculator = source.Calculator == null ? null : CloneCalculatorProgram(source.Calculator, path, context);
        clone.RawData = ProfileClone.CloneRawData(source.RawData, $"{path} raw data", context);
        return clone;
    }

    internal static IccSpectralProcessingData CloneSpectralProcessingData(IccSpectralProcessingData source)
    {
        IccSpectralProcessingData clone = new IccSpectralProcessingData();
        clone.White = CloneSpectralData(source.White);
        clone.Flags = source.Flags;
        return clone;
    }

    internal static IccCamViewingConditions CloneCamViewingConditions(IccCamViewingConditions source)
    {
        if (source.WhitePoint == null)
        {
            throw new IccProfileValidationException("The CIECAM02 white point to clone cannot be null.");
        }

        IccCamViewingConditions clone = new IccCamViewingConditions();
        clone.WhitePoint = CloneXyzColor(source.WhitePoint);
        clone.Luminance = source.Luminance;
        clone.BackgroundLuminance = source.BackgroundLuminance;
        clone.ImpactOfSurround = source.ImpactOfSurround;
        clone.ChromaticInductionFactor = source.ChromaticInductionFactor;
        clone.AdaptationFactor = source.AdaptationFactor;
        return clone;
    }

    internal static IccCalculatorProgram CloneCalculatorProgram(IccCalculatorProgram source, string path, ProfileCloneContext context)
    {
        if (context.CalculatorPrograms.ContainsKey(source))
        {
            return context.CalculatorPrograms[source];
        }

        IccCalculatorProgram clone = new IccCalculatorProgram();
        context.CalculatorPrograms.Add(source, clone);
        clone.Bytecode = ProfileClone.CloneBytes(source.Bytecode, $"{path} calculator bytecode", context);
        clone.Instructions = CloneCalculatorInstructions(source.Instructions, $"{path} calculator instructions", context);
        clone.InstructionCount = source.InstructionCount;
        clone.RequiredStackDepth = source.RequiredStackDepth;
        clone.RequiredStorageValues = source.RequiredStorageValues;
        clone.SubElements = CloneProcessingElementArray(source.SubElements, $"{path} calculator sub-elements", context);
        clone.UnknownOperationCount = source.UnknownOperationCount;
        clone.IsExecutable = source.IsExecutable;
        return clone;
    }

    internal static IccCalculatorInstruction[] CloneCalculatorInstructions(IccCalculatorInstruction[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccCalculatorInstruction[] clone = new IccCalculatorInstruction[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccCalculatorInstruction instruction = source[index];
            if (instruction == null)
            {
                throw new IccProfileValidationException($"The {path} contains a null instruction at index {index}.");
            }

            IccCalculatorInstruction copy = new IccCalculatorInstruction();
            copy.Signature = instruction.Signature;
            copy.Operand = instruction.Operand;
            copy.FloatingPointValue = instruction.FloatingPointValue;
            copy.FirstSelector = instruction.FirstSelector;
            copy.SecondSelector = instruction.SecondSelector;
            copy.IsKnown = instruction.IsKnown;
            copy.IsEnvironmentReference = instruction.IsEnvironmentReference;
            copy.IsTemporaryStorageReference = instruction.IsTemporaryStorageReference;
            copy.IsSubElementReference = instruction.IsSubElementReference;
            clone[index] = copy;
            index = index + 1;
        }

        return clone;
    }

    internal static IccLocalizedString CloneLocalizedString(IccLocalizedString source, ProfileCloneContext context)
    {
        IccLocalizedString clone = new IccLocalizedString();
        clone.Language = source.Language;
        clone.Country = source.Country;
        clone.Text = source.Text;
        clone.RawBytes = ProfileClone.CloneBytes(source.RawBytes, "localized-string raw bytes", context);
        return clone;
    }

    internal static IccLocalizedString[] CloneLocalizedStringArray(IccLocalizedString[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccLocalizedString[] clone = new IccLocalizedString[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneLocalizedString(source[index], context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccMetadataEntry CloneMetadataEntry(IccMetadataEntry source, ProfileCloneContext context)
    {
        IccMetadataEntry clone = new IccMetadataEntry();
        clone.Name = source.Name;
        clone.Value = source.Value;
        clone.HasValue = source.HasValue;
        clone.IsRegisteredName = source.IsRegisteredName;
        clone.DisplayNames = CloneLocalizedStringArray(source.DisplayNames, "metadata display names", context);
        clone.HasDisplayNameElement = source.HasDisplayNameElement;
        clone.DisplayValues = CloneLocalizedStringArray(source.DisplayValues, "metadata display values", context);
        clone.HasDisplayValueElement = source.HasDisplayValueElement;
        return clone;
    }

    internal static IccMetadataEntry[] CloneMetadataEntryArray(IccMetadataEntry[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccMetadataEntry[] clone = new IccMetadataEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneMetadataEntry(source[index], context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccTextData CloneTextData(IccTextData source, ProfileCloneContext context)
    {
        IccTextData clone = new IccTextData();
        clone.TypeSignature = source.TypeSignature;
        clone.Text = source.Text;
        clone.AsciiText = source.AsciiText;
        clone.UnicodeText = source.UnicodeText;
        clone.UnicodeLanguageCode = source.UnicodeLanguageCode;
        clone.ScriptCodeText = source.ScriptCodeText;
        clone.ScriptCode = source.ScriptCode;
        clone.Bytes = ProfileClone.CloneBytes(source.Bytes, "text-data bytes", context);
        clone.IsCompressed = source.IsCompressed;
        clone.IsXml = source.IsXml;
        return clone;
    }

    internal static IccNamedColor CloneNamedColor(IccNamedColor source)
    {
        IccNamedColor clone = new IccNamedColor();
        clone.Name = source.Name;
        clone.PcsColor = CloneColorVector(source.PcsColor);
        clone.DeviceColor = CloneColorVector(source.DeviceColor);
        clone.Tints = CloneDoubleArray(source.Tints, "named-colour tints");
        clone.Description = CloneColorDescription(source.Description);
        return clone;
    }

    internal static IccNamedColor[] CloneNamedColorArray(IccNamedColor[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccNamedColor[] clone = new IccNamedColor[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneNamedColor(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccNamedColorSet CloneNamedColorSet(IccNamedColorSet source, ProfileCloneContext context)
    {
        IccNamedColorSet clone = new IccNamedColorSet();
        clone.VendorFlags = source.VendorFlags;
        clone.Prefix = source.Prefix;
        clone.Suffix = source.Suffix;
        clone.DeviceChannelCount = source.DeviceChannelCount;
        clone.TintZero = source.TintZero == null ? null : CloneNamedColor(source.TintZero);
        clone.Colors = CloneNamedColorArray(source.Colors, "named colours");
        return clone;
    }

    internal static IccColorant CloneColorant(IccColorant source)
    {
        IccColorant clone = new IccColorant();
        clone.Name = source.Name;
        clone.PcsColor = CloneColorVector(source.PcsColor);
        clone.DeviceColor = CloneColorVector(source.DeviceColor);
        return clone;
    }

    internal static IccColorant[] CloneColorantArray(IccColorant[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccColorant[] clone = new IccColorant[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneColorant(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccColorantSet CloneColorantSet(IccColorantSet source)
    {
        IccColorantSet clone = new IccColorantSet();
        clone.ColorSpaceSignature = source.ColorSpaceSignature;
        clone.Colorants = CloneColorantArray(source.Colorants, "colorants");
        return clone;
    }

    internal static IccMeasurement CloneMeasurement(IccMeasurement source)
    {
        IccMeasurement clone = new IccMeasurement();
        clone.Observer = source.Observer;
        clone.Backing = CloneXyzColor(source.Backing);
        clone.Geometry = source.Geometry;
        clone.Flare = source.Flare;
        clone.Illuminant = source.Illuminant;
        return clone;
    }

    internal static ushort[] CloneUShortArray(ushort[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        ushort[] clone = new ushort[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static ushort[][] CloneUShortJaggedArray(ushort[][] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        ushort[][] clone = new ushort[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneUShortArray(source[index], $"{path} row {index}");
            index = index + 1;
        }

        return clone;
    }

    internal static IccResponseCurve CloneResponseCurve(IccResponseCurve source)
    {
        IccResponseCurve clone = new IccResponseCurve();
        clone.MeasurementUnitSignature = source.MeasurementUnitSignature;
        clone.MaximumColorantXyz = CloneXyzNumberArray(source.MaximumColorantXyz, "response-curve maximum XYZ values");
        clone.DeviceCodes = CloneUShortJaggedArray(source.DeviceCodes, "response-curve device codes");
        clone.Measurements = CloneDoubleJaggedArray(source.Measurements, "response-curve measurements");
        return clone;
    }

    internal static IccResponseCurve[] CloneResponseCurveArray(IccResponseCurve[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccResponseCurve[] clone = new IccResponseCurve[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneResponseCurve(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccProfileSequenceEntry CloneProfileSequenceEntry(IccProfileSequenceEntry source, ProfileCloneContext context)
    {
        IccProfileSequenceEntry clone = new IccProfileSequenceEntry();
        clone.DeviceManufacturerSignature = source.DeviceManufacturerSignature;
        clone.DeviceModelSignature = source.DeviceModelSignature;
        clone.DeviceAttributes = source.DeviceAttributes;
        clone.TechnologySignature = source.TechnologySignature;
        clone.ManufacturerDescription = CloneTextData(source.ManufacturerDescription, context);
        clone.ManufacturerLocalizedDescriptions = CloneLocalizedStringArray(source.ManufacturerLocalizedDescriptions, "profile-sequence manufacturer descriptions", context);
        clone.ModelDescription = CloneTextData(source.ModelDescription, context);
        clone.ModelLocalizedDescriptions = CloneLocalizedStringArray(source.ModelLocalizedDescriptions, "profile-sequence model descriptions", context);
        return clone;
    }

    internal static IccProfileSequenceEntry[] CloneProfileSequenceEntryArray(IccProfileSequenceEntry[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccProfileSequenceEntry[] clone = new IccProfileSequenceEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneProfileSequenceEntry(source[index], context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccProfileSequenceIdentifier CloneProfileSequenceIdentifier(IccProfileSequenceIdentifier source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile-sequence identifier to clone cannot be null.");
        }

        if (context.ProfileSequenceIdentifiers.ContainsKey(source))
        {
            return context.ProfileSequenceIdentifiers[source];
        }

        IccProfileSequenceIdentifier clone = new IccProfileSequenceIdentifier();
        context.ProfileSequenceIdentifiers.Add(source, clone);
        clone.ProfileId = ProfileClone.CloneProfileId(source.ProfileId, context);
        clone.Descriptions = CloneLocalizedStringArray(source.Descriptions, "profile-sequence identifier descriptions", context);
        return clone;
    }

    internal static IccProfileSequenceIdentifier[] CloneProfileSequenceIdentifierArray(IccProfileSequenceIdentifier[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccProfileSequenceIdentifier[] clone = new IccProfileSequenceIdentifier[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneProfileSequenceIdentifier(source[index], context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccCicp CloneCicp(IccCicp source)
    {
        IccCicp clone = new IccCicp();
        clone.ColorPrimaries = source.ColorPrimaries;
        clone.TransferCharacteristics = source.TransferCharacteristics;
        clone.MatrixCoefficients = source.MatrixCoefficients;
        clone.FullRange = source.FullRange;
        clone.UsesProfilePrimaries = source.UsesProfilePrimaries;
        return clone;
    }

    internal static IccAdaptiveGainCurveNode CloneAdaptiveGainCurveNode(IccAdaptiveGainCurveNode source)
    {
        IccAdaptiveGainCurveNode clone = new IccAdaptiveGainCurveNode();
        clone.Input = source.Input;
        clone.Gain = source.Gain;
        clone.Slope = source.Slope;
        return clone;
    }

    internal static IccAdaptiveGainCurveNode[] CloneAdaptiveGainCurveNodeArray(IccAdaptiveGainCurveNode[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.AdaptiveGainCurveNodeArrays.ContainsKey(source))
        {
            return context.AdaptiveGainCurveNodeArrays[source];
        }

        IccAdaptiveGainCurveNode[] clone = new IccAdaptiveGainCurveNode[source.Length];
        context.AdaptiveGainCurveNodeArrays.Add(source, clone);
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneAdaptiveGainCurveNode(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccAdaptiveGainCurve CloneAdaptiveGainCurve(IccAdaptiveGainCurve source, ProfileCloneContext context)
    {
        IccAdaptiveGainCurve clone = new IccAdaptiveGainCurve();
        clone.Identifier = ProfileClone.CloneBytes(source.Identifier, "adaptive-gain identifier", context);
        clone.TargetHeadroom = source.TargetHeadroom;
        clone.AlternateHeadroom = source.AlternateHeadroom;
        clone.FunctionData = ProfileClone.CloneBytes(source.FunctionData, "adaptive-gain function data", context);
        clone.BackwardCompatibleTargetHeadrooms = CloneDoubleArray(source.BackwardCompatibleTargetHeadrooms, "adaptive-gain backward-compatible target headrooms");
        clone.RedNodes = CloneAdaptiveGainCurveNodeArray(source.RedNodes, "adaptive-gain red nodes", context);
        clone.GreenNodes = CloneAdaptiveGainCurveNodeArray(source.GreenNodes, "adaptive-gain green nodes", context);
        clone.BlueNodes = CloneAdaptiveGainCurveNodeArray(source.BlueNodes, "adaptive-gain blue nodes", context);
        clone.IsImageSpecific = source.IsImageSpecific;
        return clone;
    }

    internal static IccSpectralRange CloneSpectralRange(IccSpectralRange source)
    {
        IccSpectralRange clone = new IccSpectralRange();
        clone.StartWavelength = source.StartWavelength;
        clone.EndWavelength = source.EndWavelength;
        clone.Steps = source.Steps;
        return clone;
    }

    internal static IccSpectralRanges CloneSpectralRanges(IccSpectralRanges source)
    {
        IccSpectralRanges clone = new IccSpectralRanges();
        clone.SpectralRange = CloneSpectralRange(source.SpectralRange);
        clone.BiSpectralRange = CloneSpectralRange(source.BiSpectralRange);
        return clone;
    }

    internal static IccSparseMatrix CloneSparseMatrix(IccSparseMatrix source)
    {
        IccSparseMatrix clone = new IccSparseMatrix();
        clone.Rows = source.Rows;
        clone.Columns = source.Columns;
        clone.Encoding = source.Encoding;
        clone.RowStartOffsets = CloneIntArray(source.RowStartOffsets, "sparse-matrix row offsets");
        clone.ColumnIndices = CloneIntArray(source.ColumnIndices, "sparse-matrix column indices");
        clone.Values = CloneDoubleArray(source.Values, "sparse-matrix values");
        clone.IndexPadding = CloneByteArray(source.IndexPadding, "sparse-matrix index padding");
        clone.DataPadding = CloneByteArray(source.DataPadding, "sparse-matrix data padding");
        return clone;
    }

    internal static IccSparseMatrixArray CloneSparseMatrixArray(IccSparseMatrixArray source)
    {
        if (source.Matrices == null)
        {
            throw new IccProfileValidationException("The sparse-matrix array matrices to clone cannot be null.");
        }

        IccSparseMatrixArray clone = new IccSparseMatrixArray();
        clone.EquivalentOutputChannels = source.EquivalentOutputChannels;
        clone.Encoding = source.Encoding;
        clone.GridPoints = CloneIntArray(source.GridPoints, "sparse-matrix LUT grid points");
        clone.Matrices = new IccSparseMatrix[source.Matrices.Length];
        int index = 0;
        while (index < source.Matrices.Length)
        {
            if (source.Matrices[index] == null)
            {
                throw new IccProfileValidationException($"Sparse matrix {index} to clone cannot be null.");
            }

            clone.Matrices[index] = CloneSparseMatrix(source.Matrices[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static byte[] CloneByteArray(byte[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        byte[] clone = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }

    internal static IccSpectralData CloneSpectralData(IccSpectralData source)
    {
        IccSpectralData clone = new IccSpectralData();
        clone.Range = CloneSpectralRange(source.Range);
        clone.Values = CloneDoubleArray(source.Values, "spectral values");
        clone.SparseValues = source.SparseValues == null ? null : CloneSparseMatrix(source.SparseValues);
        clone.SpectrumCount = source.SpectrumCount;
        return clone;
    }

    internal static IccSpectralViewingConditions CloneSpectralViewingConditions(IccSpectralViewingConditions source)
    {
        IccSpectralViewingConditions clone = new IccSpectralViewingConditions();
        clone.Illuminant = source.Illuminant;
        clone.IlluminantSpectrum = source.IlluminantSpectrum == null ? null : CloneSpectralData(source.IlluminantSpectrum);
        clone.ColorTemperature = source.ColorTemperature;
        clone.Observer = source.Observer;
        clone.ObserverFunctions = source.ObserverFunctions == null ? null : CloneSpectralData(source.ObserverFunctions);
        clone.IlluminantColor = CloneXyzColor(source.IlluminantColor);
        clone.SurroundColor = CloneXyzColor(source.SurroundColor);
        return clone;
    }

    internal static IccTagArray CloneHierarchicalTagArray(IccTagArray source, string path, ProfileCloneContext context)
    {
        if (context.TagArrays.ContainsKey(source))
        {
            return context.TagArrays[source];
        }

        IccTagArray clone = new IccTagArray();
        context.TagArrays.Add(source, clone);
        clone.ArraySignature = source.ArraySignature;
        clone.Values = CloneNestedTagDataArray(source.Values, $"{path} tag-array values", context);
        return clone;
    }

    internal static IccTagData[] CloneNestedTagDataArray(IccTagData[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccTagData[] clone = new IccTagData[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = ProfileClone.CloneTagData(source[index], $"{path} item {index}", context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccStructureMember CloneStructureMember(IccStructureMember source, string path, ProfileCloneContext context)
    {
        IccStructureMember clone = new IccStructureMember();
        clone.Signature = source.Signature;
        clone.Data = ProfileClone.CloneTagData(source.Data, $"{path} data", context);
        return clone;
    }

    internal static IccStructureMember[] CloneStructureMemberArray(IccStructureMember[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccStructureMember[] clone = new IccStructureMember[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneStructureMember(source[index], $"{path} item {index}", context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccTagStructure CloneHierarchicalTagStructure(IccTagStructure source, string path, ProfileCloneContext context)
    {
        if (context.TagStructures.ContainsKey(source))
        {
            return context.TagStructures[source];
        }

        IccTagStructure clone = new IccTagStructure();
        context.TagStructures.Add(source, clone);
        clone.StructureSignature = source.StructureSignature;
        clone.Members = CloneStructureMemberArray(source.Members, $"{path} tag-structure members", context);
        return clone;
    }

    internal static IccColorEncodingParameters CloneColorEncodingParameters(IccColorEncodingParameters source, ProfileCloneContext context)
    {
        IccColorEncodingParameters clone = new IccColorEncodingParameters();
        clone.Name = source.Name;
        clone.ChannelRanges = CloneEncodingRangeArray(source.ChannelRanges, "colour-encoding channel ranges");
        clone.ReferenceWhite = CloneXyzColor(source.ReferenceWhite);
        clone.Curves = CloneCurveArray(source.Curves, "colour-encoding curves", context);
        clone.Matrix = source.Matrix == null ? null : CloneMatrix(source.Matrix);
        clone.Metadata = CloneMetadataEntryArray(source.Metadata, "colour-encoding metadata", context);
        return clone;
    }

    internal static IccProfileConnectionConditions CloneProfileConnectionConditions(IccProfileConnectionConditions source, ProfileCloneContext context)
    {
        IccProfileConnectionConditions clone = new IccProfileConnectionConditions();
        clone.ViewingConditions = CloneSpectralViewingConditions(source.ViewingConditions);
        clone.MediaWhitePoint = CloneXyzColor(source.MediaWhitePoint);
        clone.SpectralWhitePoint = CloneDoubleArray(source.SpectralWhitePoint, "PCC spectral white point");
        clone.ToStandardPcs = source.ToStandardPcs == null ? null : CloneMultiProcessElements(source.ToStandardPcs, context);
        clone.FromStandardPcs = source.FromStandardPcs == null ? null : CloneMultiProcessElements(source.FromStandardPcs, context);
        return clone;
    }

    internal static IccMultiplexChannels CloneMultiplexChannels(IccMultiplexChannels source)
    {
        IccMultiplexChannels clone = new IccMultiplexChannels();
        clone.Names = CloneStringArray(source.Names, "multiplex-channel names");
        clone.DefaultValues = CloneDoubleArray(source.DefaultValues, "multiplex-channel defaults");
        clone.Order = CloneIntArray(source.Order, "multiplex-channel order");
        return clone;
    }

    internal static IccGamutVertex CloneGamutVertex(IccGamutVertex source)
    {
        IccGamutVertex clone = new IccGamutVertex();
        clone.PcsColor = CloneColorVector(source.PcsColor);
        clone.DeviceColor = CloneColorVector(source.DeviceColor);
        return clone;
    }

    internal static IccGamutVertex[] CloneGamutVertexArray(IccGamutVertex[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccGamutVertex[] clone = new IccGamutVertex[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneGamutVertex(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccGamutTriangle CloneGamutTriangle(IccGamutTriangle source)
    {
        IccGamutTriangle clone = new IccGamutTriangle();
        clone.First = source.First;
        clone.Second = source.Second;
        clone.Third = source.Third;
        return clone;
    }

    internal static IccGamutTriangle[] CloneGamutTriangleArray(IccGamutTriangle[] source, string path)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        IccGamutTriangle[] clone = new IccGamutTriangle[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneGamutTriangle(source[index]);
            index = index + 1;
        }

        return clone;
    }

    internal static IccGamutBoundary CloneGamutBoundary(IccGamutBoundary source)
    {
        IccGamutBoundary clone = new IccGamutBoundary();
        clone.PcsChannels = source.PcsChannels;
        clone.DeviceChannels = source.DeviceChannels;
        clone.Vertices = CloneGamutVertexArray(source.Vertices, "gamut vertices");
        clone.Triangles = CloneGamutTriangleArray(source.Triangles, "gamut triangles");
        return clone;
    }

    internal static IccEmbeddedImage CloneEmbeddedImage(IccEmbeddedImage source, ProfileCloneContext context)
    {
        IccEmbeddedImage clone = new IccEmbeddedImage();
        clone.Width = source.Width;
        clone.Height = source.Height;
        clone.Components = source.Components;
        clone.EncodingSignature = source.EncodingSignature;
        clone.Bytes = ProfileClone.CloneBytes(source.Bytes, "embedded-image bytes", context);
        clone.IsCompressed = source.IsCompressed;
        clone.IsSeamless = source.IsSeamless;
        clone.MinimumHeightMeters = source.MinimumHeightMeters;
        clone.MaximumHeightMeters = source.MaximumHeightMeters;
        return clone;
    }

    internal static IccBrdf CloneBrdf(IccBrdf source, ProfileCloneContext context)
    {
        IccBrdf clone = new IccBrdf();
        clone.TypeSignature = source.TypeSignature;
        clone.ModelSignature = source.ModelSignature;
        clone.Parameters = CloneDoubleArray(source.Parameters, "BRDF parameters");
        clone.ColorimetricTransform = source.ColorimetricTransform == null ? null : CloneMultiProcessElements(source.ColorimetricTransform, context);
        clone.SpectralTransform = source.SpectralTransform == null ? null : CloneMultiProcessElements(source.SpectralTransform, context);
        return clone;
    }

    internal static IccSurfaceMap CloneSurfaceMap(IccSurfaceMap source, ProfileCloneContext context)
    {
        IccSurfaceMap clone = new IccSurfaceMap();
        clone.Width = source.Width;
        clone.Height = source.Height;
        clone.HeightImage = source.HeightImage == null ? null : CloneEmbeddedImage(source.HeightImage, context);
        clone.NormalImage = source.NormalImage == null ? null : CloneEmbeddedImage(source.NormalImage, context);
        clone.Scale = CloneDoubleArray(source.Scale, "surface-map scale");
        return clone;
    }
}
