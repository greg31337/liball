namespace icc;

internal class GamutAnalysisCore
{
    internal GamutAnalysisCore()
    {
    }

    internal IccGamutResult Evaluate(IccProfile profile, System.ReadOnlySpan<float> color)
    {
        ValidateProfile(profile);
        IccTag? gamutTag = FindUsableGamutTag(profile);
        if (gamutTag != null)
        {
            ValidateColor(color, 3);
            return EvaluateGamutTag(profile, gamutTag, color);
        }

        IccGamutBoundary? boundary = FindUsableBoundary(profile);
        if (boundary != null)
        {
            ValidateColor(color, boundary.PcsChannels);
            return EvaluateBoundary(profile, boundary, color);
        }

        bool hasModel = HasGamutModelTag(profile);
        IccGamutResult unknown = CreateResult(profile, color, IccGamutStatus.Unknown,
            hasModel ? IccOperationStatus.InvalidData : IccOperationStatus.NotFound,
            hasModel
                ? "The profile contains gamut data, but no valid executable gamut model."
                : "The profile has no valid executable gamut model.");
        return unknown;
    }

    internal IccOperationResult Probe(IccProfile profile)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Tags == null)
        {
            return CreateOperationResult(IccOperationStatus.InvalidData,
                "Gamut probing requires a profile with a header, version, and tag collection.");
        }

        if (FindUsableGamutTag(profile) != null)
        {
            return CreateOperationResult(IccOperationStatus.Success,
                "The profile contains an executable ICC gamut tag.");
        }
        if (FindUsableBoundary(profile) != null)
        {
            return CreateOperationResult(IccOperationStatus.Success,
                "The profile contains a valid three-dimensional gamut-boundary mesh.");
        }
        if (HasGamutModelTag(profile))
        {
            return CreateOperationResult(IccOperationStatus.InvalidData,
                "The profile contains gamut data, but no valid executable gamut model.");
        }

        return CreateOperationResult(IccOperationStatus.NotFound,
            "The profile contains no gamut tag or gamut-boundary mesh.");
    }

    internal IccGamutResult EvaluateGamutTag(IccProfile profile, IccTag tag, System.ReadOnlySpan<float> color)
    {
        IccProfile context = ProofingTransformCompiler.CreateGamutContext(profile);
        LutPipelinePlan plan = LutPipelineCompiler.Compile(context, tag,
            IccTransformDirection.PcsToDevice, IccInterpolationKind.Multilinear);
        IccTransformOptions options = new IccTransformOptions();
        options.Direction = IccTransformDirection.PcsToDevice;
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.Interpolation = IccInterpolationKind.Multilinear;
        options.ClipOutput = false;
        IccColorTransform transform = new IccColorTransform();
        transform.ConfigureCompiledPlan(plan.Stages, plan.SourceChannels, plan.DestinationChannels,
            plan.SourceRanges, plan.DestinationRanges, options, IccTagSignatures.Gamut);
        float[] output = transform.TransformSingleColor(color);
        IccGamutStatus status = output[0] > 0.0f ? IccGamutStatus.Outside : IccGamutStatus.Inside;
        return CreateResult(profile, color, status, IccOperationStatus.Success,
            status == IccGamutStatus.Outside
                ? "The ICC gamut tag classified the colour as outside the gamut."
                : "The ICC gamut tag classified the colour as inside the gamut.");
    }

    internal IccGamutResult EvaluateBoundary(IccProfile profile, IccGamutBoundary boundary,
        System.ReadOnlySpan<float> color)
    {
        double x = color[0];
        double y = color[1];
        double z = color[2];
        double tolerance = CalculateTolerance(boundary);
        double squaredTolerance = tolerance * tolerance;
        GamutClosestPoint nearest = new GamutClosestPoint();
        double solidAngle = 0.0;
        int index = 0;
        while (index < boundary.Triangles.Length)
        {
            IccGamutTriangle triangle = boundary.Triangles[index];
            double[] a = boundary.Vertices[triangle.First].PcsColor.Components;
            double[] b = boundary.Vertices[triangle.Second].PcsColor.Components;
            double[] c = boundary.Vertices[triangle.Third].PcsColor.Components;
            GamutClosestPoint candidate = ClosestPointOnTriangle(x, y, z, a, b, c);
            if (candidate.SquaredDistance < nearest.SquaredDistance)
            {
                nearest = candidate;
            }
            solidAngle = solidAngle + TriangleSolidAngle(x, y, z, a, b, c);
            index = index + 1;
        }

        IccGamutStatus status;
        if (nearest.SquaredDistance <= squaredTolerance)
        {
            status = IccGamutStatus.Boundary;
        }
        else if (System.Math.Abs(solidAngle) > System.Math.PI * 2.0)
        {
            status = IccGamutStatus.Inside;
        }
        else
        {
            status = IccGamutStatus.Outside;
        }

        IccGamutResult result = CreateResult(profile, color, status, IccOperationStatus.Success,
            CreateBoundaryMessage(status, boundary));
        result.Distance = System.Math.Sqrt(nearest.SquaredDistance);
        result.NearestBoundaryColor = CreateBoundaryColor(profile, boundary, nearest);
        return result;
    }

    internal IccTag? FindUsableGamutTag(IccProfile profile)
    {
        if (profile.Header.Version.Kind != IccVersionKind.Version2
            && profile.Header.Version.Kind != IccVersionKind.Version4)
        {
            return null;
        }
        if (!IsGamutProfileClass(profile.Header.ProfileClass)
            || !ProfileTransformCompiler.HasConnectablePcs(profile))
        {
            return null;
        }

        IccTag? tag = FindTag(profile, IccTagSignatures.Gamut);
        if (tag == null)
        {
            return null;
        }
        IccTagData? data = ResolveTagData(tag);
        if (data == null || data.Kind != IccTagDataKind.Lut || data.MultiProcessElements != null)
        {
            return null;
        }
        if (data.Lut != null && IsUsableClassicGamutLut(data.Lut, ResolveLutType(data)))
        {
            return tag;
        }
        if (data.MultiStageLut != null && IsUsableMultiStageGamutLut(data.MultiStageLut, ResolveLutType(data)))
        {
            return tag;
        }
        return null;
    }

    internal IccGamutBoundary? FindUsableBoundary(IccProfile profile)
    {
        if (profile.Header.Version.Kind != IccVersionKind.Version5
            || !IsGamutProfileClass(profile.Header.ProfileClass))
        {
            return null;
        }

        uint preferred = IntentBoundarySignature(profile.Header.RenderingIntentKind);
        uint[] signatures = new uint[]
        {
            preferred,
            IccTagSignatures.GamutBoundary0,
            IccTagSignatures.GamutBoundary1,
            IccTagSignatures.GamutBoundary2,
            IccTagSignatures.GamutBoundary3
        };
        int index = 0;
        while (index < signatures.Length)
        {
            uint signature = signatures[index];
            if (signature != 0 && !WasChecked(signatures, index, signature))
            {
                IccTag? tag = FindTag(profile, signature);
                if (tag != null)
                {
                    IccTagData? data = ResolveTagData(tag);
                    if (data != null && data.Kind == IccTagDataKind.GamutBoundary && data.GamutBoundary != null
                        && IsUsableBoundary(profile, data.GamutBoundary))
                    {
                        return data.GamutBoundary;
                    }
                }
            }
            index = index + 1;
        }
        return null;
    }

    internal bool IsUsableBoundary(IccProfile profile, IccGamutBoundary boundary)
    {
        if (boundary.PcsChannels != 3 || GamutBoundaryTagCodec.ValidateBoundaryForWrite(boundary).Length != 0)
        {
            return false;
        }
        int colorimetricChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        int spectralChannels = (int)(profile.Header.SpectralPcsSignature & 0x0000ffff);
        if (boundary.PcsChannels != colorimetricChannels && boundary.PcsChannels != spectralChannels)
        {
            return false;
        }
        int deviceChannels = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        if (boundary.DeviceChannels != 0 && boundary.DeviceChannels != deviceChannels)
        {
            return false;
        }

        double tolerance = CalculateTolerance(boundary);
        double volume = 0.0;
        int index = 0;
        while (index < boundary.Triangles.Length)
        {
            IccGamutTriangle triangle = boundary.Triangles[index];
            double[] a = boundary.Vertices[triangle.First].PcsColor.Components;
            double[] b = boundary.Vertices[triangle.Second].PcsColor.Components;
            double[] c = boundary.Vertices[triangle.Third].PcsColor.Components;
            if (TriangleAreaSquared(a, b, c) <= tolerance * tolerance * tolerance * tolerance)
            {
                return false;
            }
            volume = volume + SignedTriangleVolume(a, b, c);
            index = index + 1;
        }
        return System.Math.Abs(volume) > tolerance * tolerance * tolerance;
    }

    internal bool IsUsableClassicGamutLut(IccLut lut, uint typeSignature)
    {
        if (lut.InputChannels != 3 || lut.OutputChannels != 1 || lut.Matrix == null
            || lut.InputTables == null || lut.OutputTables == null || lut.Clut == null
            || lut.Matrix.Rows != 3 || lut.Matrix.Columns != 3 || lut.Matrix.Values == null
            || lut.Matrix.Values.Length != 9 || lut.InputTables.Length != 3 || lut.OutputTables.Length != 1)
        {
            return false;
        }
        int inputTableSize = lut.InputTables[0] == null ? 0 : lut.InputTables[0].Length;
        int outputTableSize = lut.OutputTables[0] == null ? 0 : lut.OutputTables[0].Length;
        if ((typeSignature != IccTypeSignatures.Lut8 && typeSignature != IccTypeSignatures.Lut16)
            || (typeSignature == IccTypeSignatures.Lut8 && lut.PcsEncoding != IccPcsEncodingKind.Normalized)
            || (typeSignature == IccTypeSignatures.Lut16 && lut.PcsEncoding != IccPcsEncodingKind.Version2)
            || (typeSignature == IccTypeSignatures.Lut8 && (inputTableSize != 256 || outputTableSize != 256))
            || (typeSignature == IccTypeSignatures.Lut16
                && (inputTableSize < 2 || inputTableSize > 4096 || outputTableSize < 2 || outputTableSize > 4096)))
        {
            return false;
        }
        if (!HasValidTables(lut.InputTables, inputTableSize) || !HasValidTables(lut.OutputTables, outputTableSize)
            || !HasFiniteValues(lut.Matrix.Values) || !IsUsableClut(lut.Clut, 3, 1, true))
        {
            return false;
        }
        return true;
    }

    internal bool IsUsableMultiStageGamutLut(IccMultiStageLut lut, uint typeSignature)
    {
        if (typeSignature != IccTypeSignatures.LutBToA || lut.InputChannels != 3 || lut.OutputChannels != 1
            || lut.PcsEncoding != IccPcsEncodingKind.Version4 || lut.ACurves == null
            || lut.BCurves == null || lut.MCurves == null || lut.BCurves.Length != 3)
        {
            return false;
        }
        bool hasMatrix = lut.Matrix != null;
        bool hasM = lut.MCurves.Length > 0;
        bool hasClut = lut.Clut != null;
        bool hasA = lut.ACurves.Length > 0;
        if (hasMatrix != hasM || hasClut != hasA || !hasClut || lut.ACurves.Length != 1
            || (hasM && lut.MCurves.Length != 3) || !HasValidCurves(lut.BCurves)
            || !HasValidCurves(lut.ACurves) || (hasM && !HasValidCurves(lut.MCurves)))
        {
            return false;
        }
        if (lut.Clut == null || !IsUsableClut(lut.Clut, 3, 1, false))
        {
            return false;
        }
        if (hasMatrix && (lut.Matrix == null || lut.Matrix.Rows != 3
            || (lut.Matrix.Columns != 3 && lut.Matrix.Columns != 4)
            || lut.Matrix.Values == null || lut.Matrix.Values.Length != lut.Matrix.Rows * lut.Matrix.Columns
            || !HasFiniteValues(lut.Matrix.Values)))
        {
            return false;
        }
        return true;
    }

    internal bool IsUsableClut(IccClut clut, int inputChannels, int outputChannels, bool requireUniformGrid)
    {
        if (clut.InputChannels != inputChannels || clut.OutputChannels != outputChannels
            || clut.GridPoints == null || clut.Values == null || clut.GridPoints.Length != inputChannels)
        {
            return false;
        }
        int count = outputChannels;
        int firstGrid = clut.GridPoints[0];
        int index = 0;
        while (index < clut.GridPoints.Length)
        {
            int grid = clut.GridPoints[index];
            if (grid < 2 || grid > 255 || count > int.MaxValue / grid
                || (requireUniformGrid && grid != firstGrid))
            {
                return false;
            }
            count = count * grid;
            index = index + 1;
        }
        return clut.Values.Length == count && HasNormalizedValues(clut.Values);
    }

    internal bool HasValidTables(double[][] tables, int size)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index] == null || tables[index].Length != size || !HasNormalizedValues(tables[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool HasValidCurves(IccCurve[] curves)
    {
        int index = 0;
        while (index < curves.Length)
        {
            IccCurve curve = curves[index];
            if (!IsUsableIcc1Curve(curve))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsUsableIcc1Curve(IccCurve curve)
    {
        if (curve == null || curve.Samples == null || curve.Segments == null)
        {
            return false;
        }
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                return true;
            case IccCurveKind.Gamma:
                return System.Double.IsFinite(curve.Gamma) && curve.Gamma > 0.0;
            case IccCurveKind.Sampled:
                return curve.Samples.Length >= 2 && HasFiniteValues(curve.Samples);
            case IccCurveKind.Parametric:
                return IsUsableParametricCurve(curve.Parametric);
            default:
                return false;
        }
    }

    internal bool IsUsableParametricCurve(IccParametricCurve? curve)
    {
        if (curve == null || curve.Parameters == null)
        {
            return false;
        }
        int expected = CurveTagCodec.GetParameterCount(curve.FunctionType);
        if (expected < 0 || curve.Parameters.Length != expected || !HasFiniteValues(curve.Parameters))
        {
            return false;
        }
        return IsFiniteParametricValue(curve, 0.0)
            && IsFiniteParametricValue(curve, 0.5)
            && IsFiniteParametricValue(curve, 1.0);
    }

    internal bool IsFiniteParametricValue(IccParametricCurve curve, double input)
    {
        double[] parameters = curve.Parameters;
        double value;
        switch (curve.FunctionType)
        {
            case 0:
                value = System.Math.Pow(input, parameters[0]);
                break;
            case 1:
                value = input >= -parameters[2] / parameters[1]
                    ? System.Math.Pow((parameters[1] * input) + parameters[2], parameters[0]) : 0.0;
                break;
            case 2:
                value = input >= -parameters[2] / parameters[1]
                    ? System.Math.Pow((parameters[1] * input) + parameters[2], parameters[0]) + parameters[3]
                    : parameters[3];
                break;
            case 3:
                value = input >= parameters[4]
                    ? System.Math.Pow((parameters[1] * input) + parameters[2], parameters[0])
                    : parameters[3] * input;
                break;
            case 4:
                value = input >= parameters[4]
                    ? System.Math.Pow((parameters[1] * input) + parameters[2], parameters[0]) + parameters[5]
                    : (parameters[3] * input) + parameters[6];
                break;
            default:
                return false;
        }
        return System.Double.IsFinite(value);
    }

    internal bool HasNormalizedValues(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Double.IsFinite(values[index]) || values[index] < 0.0 || values[index] > 1.0)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool HasFiniteValues(double[] values)
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

    internal uint ResolveLutType(IccTagData data)
    {
        if (data.RawData != null && data.RawData.HasTypeSignature)
        {
            return data.RawData.TypeSignature;
        }
        if (data.Lut != null && data.MultiStageLut == null)
        {
            return data.Lut.PcsEncoding == IccPcsEncodingKind.Version2
                ? IccTypeSignatures.Lut16 : IccTypeSignatures.Lut8;
        }
        if (data.MultiStageLut != null && data.Lut == null)
        {
            return IccTypeSignatures.LutBToA;
        }
        return 0;
    }

    internal IccTagData? ResolveTagData(IccTag tag)
    {
        if (tag.DecodeState != null)
        {
            return TagDecoder.GetDecodedData(tag);
        }
        return tag.Data;
    }

    internal IccTag? FindTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }
            index = index + 1;
        }
        return null;
    }

    internal bool HasGamutModelTag(IccProfile profile)
    {
        return FindTag(profile, IccTagSignatures.Gamut) != null
            || FindTag(profile, IccTagSignatures.GamutBoundary0) != null
            || FindTag(profile, IccTagSignatures.GamutBoundary1) != null
            || FindTag(profile, IccTagSignatures.GamutBoundary2) != null
            || FindTag(profile, IccTagSignatures.GamutBoundary3) != null;
    }

    internal bool IsGamutProfileClass(IccProfileClass profileClass)
    {
        return profileClass == IccProfileClass.Input || profileClass == IccProfileClass.Display
            || profileClass == IccProfileClass.Output || profileClass == IccProfileClass.ColorSpace;
    }

    internal uint IntentBoundarySignature(IccRenderingIntent intent)
    {
        switch (intent)
        {
            case IccRenderingIntent.Perceptual:
                return IccTagSignatures.GamutBoundary0;
            case IccRenderingIntent.MediaRelativeColorimetric:
                return IccTagSignatures.GamutBoundary1;
            case IccRenderingIntent.Saturation:
                return IccTagSignatures.GamutBoundary2;
            case IccRenderingIntent.IccAbsoluteColorimetric:
                return IccTagSignatures.GamutBoundary3;
            default:
                return 0;
        }
    }

    internal bool WasChecked(uint[] signatures, int currentIndex, uint signature)
    {
        int index = 0;
        while (index < currentIndex)
        {
            if (signatures[index] == signature)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateProfile(IccProfile profile)
    {
        if (profile == null || profile.Header == null || profile.Header.Version == null || profile.Tags == null)
        {
            throw new IccProfileValidationException(
                "Gamut analysis requires a profile with a header, version, and tag collection.");
        }
    }

    internal void ValidateColor(System.ReadOnlySpan<float> color, int channels)
    {
        if (color.Length != channels)
        {
            throw new IccProfileValidationException($"The gamut model requires exactly {channels} PCS colour channels.");
        }
        int index = 0;
        while (index < color.Length)
        {
            if (!System.Single.IsFinite(color[index]))
            {
                throw new IccProfileValidationException($"Gamut colour component {index} must be finite.");
            }
            index = index + 1;
        }
    }

    internal IccGamutResult CreateResult(IccProfile profile, System.ReadOnlySpan<float> color,
        IccGamutStatus status, IccOperationStatus operationStatus, string message)
    {
        IccGamutResult result = new IccGamutResult();
        result.Status = status;
        result.Color = CreateColor(profile, color);
        result.Result = CreateOperationResult(operationStatus, message);
        return result;
    }

    internal IccColorVector CreateColor(IccProfile profile, System.ReadOnlySpan<float> color)
    {
        IccColorVector vector = new IccColorVector();
        vector.Components = new double[color.Length];
        int index = 0;
        while (index < color.Length)
        {
            vector.Components[index] = color[index];
            index = index + 1;
        }
        SetPcsMetadata(profile, vector);
        return vector;
    }

    internal IccColorVector CreateBoundaryColor(IccProfile profile, IccGamutBoundary boundary,
        GamutClosestPoint point)
    {
        IccColorVector vector = new IccColorVector();
        vector.Components = new double[] { point.X, point.Y, point.Z };
        SetPcsMetadata(profile, vector);
        if (boundary.Vertices.Length > 0 && boundary.Vertices[0].PcsColor != null
            && boundary.Vertices[0].PcsColor.ColorSpaceSignature != 0)
        {
            vector.ColorSpace = boundary.Vertices[0].PcsColor.ColorSpace;
            vector.ColorSpaceSignature = boundary.Vertices[0].PcsColor.ColorSpaceSignature;
        }
        return vector;
    }

    internal void SetPcsMetadata(IccProfile profile, IccColorVector vector)
    {
        int colorimetricChannels = ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature);
        if (colorimetricChannels == vector.Components.Length)
        {
            vector.ColorSpaceSignature = profile.Header.ProfileConnectionSpaceSignature;
            vector.ColorSpace = HeaderCodec.DecodeColorSpace(vector.ColorSpaceSignature);
            return;
        }
        int spectralChannels = (int)(profile.Header.SpectralPcsSignature & 0x0000ffff);
        if (spectralChannels == vector.Components.Length)
        {
            vector.ColorSpaceSignature = profile.Header.SpectralPcsSignature;
            vector.ColorSpace = HeaderCodec.DecodeSpectralPcs(vector.ColorSpaceSignature);
        }
    }

    internal IccOperationResult CreateOperationResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }

    internal string CreateBoundaryMessage(IccGamutStatus status, IccGamutBoundary boundary)
    {
        string relation = status == IccGamutStatus.Inside ? "inside"
            : status == IccGamutStatus.Outside ? "outside" : "on";
        return $"The colour lies {relation} the gamut boundary described by {boundary.Vertices.Length} vertices and {boundary.Triangles.Length} triangles.";
    }

    internal double CalculateTolerance(IccGamutBoundary boundary)
    {
        double minimumX = boundary.Vertices[0].PcsColor.Components[0];
        double minimumY = boundary.Vertices[0].PcsColor.Components[1];
        double minimumZ = boundary.Vertices[0].PcsColor.Components[2];
        double maximumX = minimumX;
        double maximumY = minimumY;
        double maximumZ = minimumZ;
        int index = 1;
        while (index < boundary.Vertices.Length)
        {
            double[] value = boundary.Vertices[index].PcsColor.Components;
            if (value[0] < minimumX) minimumX = value[0];
            if (value[1] < minimumY) minimumY = value[1];
            if (value[2] < minimumZ) minimumZ = value[2];
            if (value[0] > maximumX) maximumX = value[0];
            if (value[1] > maximumY) maximumY = value[1];
            if (value[2] > maximumZ) maximumZ = value[2];
            index = index + 1;
        }
        double x = maximumX - minimumX;
        double y = maximumY - minimumY;
        double z = maximumZ - minimumZ;
        double diagonal = System.Math.Sqrt((x * x) + (y * y) + (z * z));
        return System.Math.Max(1.0, diagonal) * 0.0000001;
    }

    internal double TriangleAreaSquared(double[] a, double[] b, double[] c)
    {
        double abX = b[0] - a[0];
        double abY = b[1] - a[1];
        double abZ = b[2] - a[2];
        double acX = c[0] - a[0];
        double acY = c[1] - a[1];
        double acZ = c[2] - a[2];
        double crossX = (abY * acZ) - (abZ * acY);
        double crossY = (abZ * acX) - (abX * acZ);
        double crossZ = (abX * acY) - (abY * acX);
        return (crossX * crossX) + (crossY * crossY) + (crossZ * crossZ);
    }

    internal double SignedTriangleVolume(double[] a, double[] b, double[] c)
    {
        double crossX = (b[1] * c[2]) - (b[2] * c[1]);
        double crossY = (b[2] * c[0]) - (b[0] * c[2]);
        double crossZ = (b[0] * c[1]) - (b[1] * c[0]);
        return ((a[0] * crossX) + (a[1] * crossY) + (a[2] * crossZ)) / 6.0;
    }

    internal double TriangleSolidAngle(double x, double y, double z, double[] a, double[] b, double[] c)
    {
        double ax = a[0] - x;
        double ay = a[1] - y;
        double az = a[2] - z;
        double bx = b[0] - x;
        double by = b[1] - y;
        double bz = b[2] - z;
        double cx = c[0] - x;
        double cy = c[1] - y;
        double cz = c[2] - z;
        double crossX = (by * cz) - (bz * cy);
        double crossY = (bz * cx) - (bx * cz);
        double crossZ = (bx * cy) - (by * cx);
        double numerator = (ax * crossX) + (ay * crossY) + (az * crossZ);
        double lengthA = System.Math.Sqrt((ax * ax) + (ay * ay) + (az * az));
        double lengthB = System.Math.Sqrt((bx * bx) + (by * by) + (bz * bz));
        double lengthC = System.Math.Sqrt((cx * cx) + (cy * cy) + (cz * cz));
        double denominator = (lengthA * lengthB * lengthC)
            + (((ax * bx) + (ay * by) + (az * bz)) * lengthC)
            + (((bx * cx) + (by * cy) + (bz * cz)) * lengthA)
            + (((cx * ax) + (cy * ay) + (cz * az)) * lengthB);
        return 2.0 * System.Math.Atan2(numerator, denominator);
    }

    internal GamutClosestPoint ClosestPointOnTriangle(double px, double py, double pz,
        double[] a, double[] b, double[] c)
    {
        double abX = b[0] - a[0];
        double abY = b[1] - a[1];
        double abZ = b[2] - a[2];
        double acX = c[0] - a[0];
        double acY = c[1] - a[1];
        double acZ = c[2] - a[2];
        double apX = px - a[0];
        double apY = py - a[1];
        double apZ = pz - a[2];
        double d1 = (abX * apX) + (abY * apY) + (abZ * apZ);
        double d2 = (acX * apX) + (acY * apY) + (acZ * apZ);
        if (d1 <= 0.0 && d2 <= 0.0)
        {
            return CreateClosestPoint(px, py, pz, a[0], a[1], a[2]);
        }

        double bpX = px - b[0];
        double bpY = py - b[1];
        double bpZ = pz - b[2];
        double d3 = (abX * bpX) + (abY * bpY) + (abZ * bpZ);
        double d4 = (acX * bpX) + (acY * bpY) + (acZ * bpZ);
        if (d3 >= 0.0 && d4 <= d3)
        {
            return CreateClosestPoint(px, py, pz, b[0], b[1], b[2]);
        }

        double vc = (d1 * d4) - (d3 * d2);
        if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
        {
            double v = d1 / (d1 - d3);
            return CreateClosestPoint(px, py, pz, a[0] + (v * abX), a[1] + (v * abY), a[2] + (v * abZ));
        }

        double cpX = px - c[0];
        double cpY = py - c[1];
        double cpZ = pz - c[2];
        double d5 = (abX * cpX) + (abY * cpY) + (abZ * cpZ);
        double d6 = (acX * cpX) + (acY * cpY) + (acZ * cpZ);
        if (d6 >= 0.0 && d5 <= d6)
        {
            return CreateClosestPoint(px, py, pz, c[0], c[1], c[2]);
        }

        double vb = (d5 * d2) - (d1 * d6);
        if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
        {
            double w = d2 / (d2 - d6);
            return CreateClosestPoint(px, py, pz, a[0] + (w * acX), a[1] + (w * acY), a[2] + (w * acZ));
        }

        double va = (d3 * d6) - (d5 * d4);
        if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
        {
            double edgeX = c[0] - b[0];
            double edgeY = c[1] - b[1];
            double edgeZ = c[2] - b[2];
            double w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return CreateClosestPoint(px, py, pz, b[0] + (w * edgeX), b[1] + (w * edgeY), b[2] + (w * edgeZ));
        }

        double denominator = 1.0 / (va + vb + vc);
        double faceV = vb * denominator;
        double faceW = vc * denominator;
        return CreateClosestPoint(px, py, pz,
            a[0] + (abX * faceV) + (acX * faceW),
            a[1] + (abY * faceV) + (acY * faceW),
            a[2] + (abZ * faceV) + (acZ * faceW));
    }

    internal GamutClosestPoint CreateClosestPoint(double px, double py, double pz,
        double x, double y, double z)
    {
        GamutClosestPoint result = new GamutClosestPoint();
        result.X = x;
        result.Y = y;
        result.Z = z;
        double dx = px - x;
        double dy = py - y;
        double dz = pz - z;
        result.SquaredDistance = (dx * dx) + (dy * dy) + (dz * dz);
        return result;
    }
}
