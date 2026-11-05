namespace raw;

internal static class RawNormalizer
{
    internal static RawSensorImage Normalize(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        Validate(storedImage, options);
        RawImageInfo info = storedImage.Info;
        int channelCount = info.GetChannelCount();
        int rowStride = CheckedMath.ToInt32(CheckedMath.Multiply(info.Width, channelCount));
        long sampleCountLong = CheckedMath.Multiply(rowStride, info.Height);
        CheckedMath.DecodedSize(sampleCountLong, 6, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 1, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] normalized = new float[sampleCount];
        byte[] blackClipped = new byte[sampleCount];
        byte[] whiteClipped = new byte[sampleCount];
        double naturalMinimum = GetNaturalMinimum(storedImage, options);
        double naturalMaximum = GetNaturalMaximum(storedImage, options);
        RawBlackLevel blackLevel = options.UseOpticalBlack ? RawOpticalBlackEstimator.EstimateValidated(storedImage, options) : options.BlackLevel == null ? RawBlackLevel.CreateScalar(naturalMinimum, channelCount) : options.BlackLevel;
        double[] whiteLevels = GetWhiteLevels(options.WhiteLevels, naturalMaximum, channelCount);
        RawBandingCorrectionResult? bandingCorrection = RawBandingCorrector.Estimate(storedImage, options, blackLevel);
        RawCropModel crops = new RawCropModel(info, options.UserCrop);
        RawArea[] maskedAreas = CopyMaskedAreas(options.MaskedAreas);
        int y = 0;
        while (y < info.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < info.Width)
            {
                int channel = 0;
                while (channel < channelCount)
                {
                    int sourceIndex = y * storedImage.RowStrideSamples + x * channelCount + channel;
                    int destinationIndex = y * rowStride + x * channelCount + channel;
                    double source = ReadValue(storedImage, sourceIndex);
                    double linear = Linearize(source, options);
                    double black = blackLevel.GetValueUnchecked(x, y, channel);
                    double white = whiteLevels.Length == 1 ? whiteLevels[0] : whiteLevels[channel];
                    if (!RawModelValidation.IsFinite(source) || !RawModelValidation.IsFinite(linear) || !RawModelValidation.IsFinite(black) || !RawModelValidation.IsFinite(white) || white <= black)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Finite samples and a white level greater than black are required for normalization.", -1);
                    }
                    double correctedLinear = linear - RawBandingCorrector.Correction(bandingCorrection, x, y);
                    if (!RawModelValidation.IsFinite(correctedLinear))
                    {
                        throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A banding-corrected sample is not finite.", -1);
                    }
                    if (correctedLinear <= black)
                    {
                        blackClipped[destinationIndex] = 1;
                    }
                    if (correctedLinear >= white)
                    {
                        whiteClipped[destinationIndex] = 1;
                    }
                    double offset = options.SubtractBlackLevel ? black : naturalMinimum;
                    double ceiling = options.ScaleWhiteLevel ? white : naturalMaximum;
                    if (ceiling <= offset)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The applied normalization range must have a positive extent.", -1);
                    }
                    double value = (correctedLinear - offset) / (ceiling - offset);
                    if (value < 0.0)
                    {
                        value = 0.0;
                    }
                    else if (value > 1.0)
                    {
                        value = 1.0;
                    }
                    normalized[destinationIndex] = (float)value;
                    channel++;
                }
                x++;
            }
            y++;
        }
        RawStoredImage? retained = options.PreserveOriginalValues ? storedImage : null;
        RawSensorImage result = new RawSensorImage(info, null, normalized, rowStride, true, retained, crops.UserCrop, maskedAreas, blackLevel, whiteLevels, null, null, storedImage.Diagnostics);
        result.CropModel = crops;
        result.ClippingMask = new RawClippingMask(info.Width, info.Height, channelCount, rowStride, blackClipped, whiteClipped);
        result.BandingCorrection = bandingCorrection;
        return result;
    }

    internal static void Validate(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        if (options == null || !options.Validate().IsValid() || storedImage == null || storedImage.Info == null || !storedImage.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid stored image and normalization options are required.", -1);
        }
        RawImageInfo info = storedImage.Info;
        int channelCount = info.GetChannelCount();
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).LinearizationTable.Length != 0 && info.SampleKind != RawSampleKind.UnsignedInteger)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Linearization tables require unsigned stored sample codes.", -1);
        }
        ValidateLinearization(options.LinearizationTable, options.ApplyLinearization);
        if (options.BlackLevel != null && !options.BlackLevel.Validate(info.Width, info.Height, channelCount).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Black-level calibration does not match the stored image.", -1);
        }
        if (options.UseOpticalBlack && options.BlackLevel != null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Supplied and estimated black calibration cannot both be selected.", -1);
        }
        if (options.OpticalBlackRepeatWidth > info.Width || options.OpticalBlackRepeatHeight > info.Height)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Optical-black repeat dimensions must fit the stored image.", -1);
        }
        ValidateWhiteLevels(options.WhiteLevels, channelCount, info.SampleKind == RawSampleKind.SignedInteger);
        ValidateMaskedAreas(options.MaskedAreas, info);
        if (options.UseOpticalBlack && !HasOpticalBlack(options.MaskedAreas))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Optical-black estimation requires at least one optical-black area.", -1);
        }
        if ((options.ApplyRowBandingCorrection || options.ApplyColumnBandingCorrection) && options.MaskedAreas.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Banding correction requires masked or optical-black calibration areas.", -1);
        }
        if (options.ApplyRowBandingCorrection && options.EstimateBlackRowDeltas || options.ApplyColumnBandingCorrection && options.EstimateBlackColumnDeltas)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Black-level axis deltas and banding correction cannot estimate the same axis together.", -1);
        }
    }

    internal static void ValidateMaskedAreas(RawArea[] maskedAreas, RawImageInfo info)
    {
        int index = 0;
        while (index < maskedAreas.Length)
        {
            RawArea area = maskedAreas[index];
            if (area == null || area.Kind != RawAreaKind.Masked && area.Kind != RawAreaKind.OpticalBlack || !area.Validate(info.Width, info.Height).IsValid())
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Every masked area must be bounded and have masked or optical-black semantics.", -1);
            }
            if (info.ActiveArea != null && Overlaps(area, info.ActiveArea))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Masked areas cannot overlap the active sensor area.", -1);
            }
            int other = 0;
            while (other < index)
            {
                if (Overlaps(area, maskedAreas[other]))
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Masked areas cannot overlap each other.", -1);
                }
                other++;
            }
            index++;
        }
    }

    internal static bool HasOpticalBlack(RawArea[] areas)
    {
        int index = 0;
        while (index < areas.Length)
        {
            if (areas[index] != null && areas[index].Kind == RawAreaKind.OpticalBlack)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool Overlaps(RawArea first, RawArea second)
    {
        if (first == null || second == null)
        {
            return false;
        }
        return first.X < (long)second.X + second.Width && second.X < (long)first.X + first.Width && first.Y < (long)second.Y + second.Height && second.Y < (long)first.Y + first.Height;
    }

    internal static void ValidateWhiteLevels(double[] whiteLevels, int channelCount, bool signed)
    {
        if (whiteLevels == null || whiteLevels.Length != 0 && whiteLevels.Length != 1 && whiteLevels.Length != channelCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "White levels must be empty, scalar, or contain one value per logical channel.", -1);
        }
        int index = 0;
        while (index < whiteLevels.Length)
        {
            if (!RawModelValidation.IsFinite(whiteLevels[index]) || !signed && whiteLevels[index] <= 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "White levels must be finite and unsigned or floating white levels must be positive.", -1);
            }
            index++;
        }
    }

    internal static void ValidateLinearization(ushort[] table, bool applied)
    {
        if (!applied || table.Length == 0)
        {
            return;
        }
        int index = 1;
        while (index < table.Length)
        {
            if (table[index] < table[index - 1])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The applied linearization table must be monotonic.", -1);
            }
            index++;
        }
    }

    internal static double ReadValue(RawStoredImage storedImage, int index)
    {
        if (storedImage.UnsignedSamples.Length != 0)
        {
            return storedImage.UnsignedSamples[index];
        }
        if (storedImage.SignedSamples.Length != 0)
        {
            return storedImage.SignedSamples[index];
        }
        return storedImage.FloatingSamples[index];
    }

    internal static double Linearize(double source, RawNormalizationOptions options)
    {
        if (!options.ApplyLinearization || options.LinearizationTable.Length == 0)
        {
            return source;
        }
        if (source < 0.0 || source >= options.LinearizationTable.LongLength || source != System.Math.Truncate(source))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A stored sample falls outside the linearization table.", -1);
        }
        return options.LinearizationTable[(int)source];
    }

    internal static double GetNaturalMinimum(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        if (options.ApplyLinearization && options.LinearizationTable.Length != 0)
        {
            ushort minimum = ushort.MaxValue;
            int index = 0;
            while (index < options.LinearizationTable.Length)
            {
                if (options.LinearizationTable[index] < minimum)
                {
                    minimum = options.LinearizationTable[index];
                }
                index++;
            }
            return minimum;
        }
        if (storedImage.Info.SampleKind == RawSampleKind.SignedInteger)
        {
            return storedImage.Info.BitDepth == 32 ? int.MinValue : -(1L << (storedImage.Info.BitDepth - 1));
        }
        return 0.0;
    }

    internal static double GetNaturalMaximum(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        if (options.ApplyLinearization && options.LinearizationTable.Length != 0)
        {
            ushort maximum = 0;
            int index = 0;
            while (index < options.LinearizationTable.Length)
            {
                if (options.LinearizationTable[index] > maximum)
                {
                    maximum = options.LinearizationTable[index];
                }
                index++;
            }
            return maximum;
        }
        if (storedImage.Info.SampleKind == RawSampleKind.UnsignedInteger)
        {
            return storedImage.Info.BitDepth == 32 ? uint.MaxValue : (1UL << storedImage.Info.BitDepth) - 1UL;
        }
        if (storedImage.Info.SampleKind == RawSampleKind.SignedInteger)
        {
            return storedImage.Info.BitDepth == 32 ? int.MaxValue : (1L << (storedImage.Info.BitDepth - 1)) - 1L;
        }
        return 1.0;
    }

    internal static double[] GetWhiteLevels(double[] supplied, double fallback, int channelCount)
    {
        if (supplied.Length != 0)
        {
            double[] copy = new double[supplied.Length];
            System.Array.Copy(supplied, copy, supplied.Length);
            return copy;
        }
        double[] result = new double[channelCount];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = fallback;
            index++;
        }
        return result;
    }

    internal static RawArea[] CopyMaskedAreas(RawArea[] supplied)
    {
        RawArea[] result = new RawArea[supplied.Length];
        int index = 0;
        while (index < supplied.Length)
        {
            result[index] = RawCropModel.Copy(supplied[index]) ?? throw new System.InvalidOperationException("A validated masked area cannot be null.");
            index++;
        }
        return result;
    }
}
