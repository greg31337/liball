namespace raw;

internal static class RawLensCorrector
{
    internal static RawSensorImage Correct(RawSensorImage image, RawLensCorrectionOptions options)
    {
        Validate(image, options);
        RawImageInfo info = image.Info;
        int channelCount = info.GetChannelCount();
        int rowStride = CheckedMath.ToInt32(CheckedMath.Multiply(info.Width, channelCount));
        long sampleCountLong = CheckedMath.Multiply(rowStride, info.Height);
        CheckedMath.DecodedSize(sampleCountLong, 8, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] source = CopySamples(image, rowStride, sampleCount, options);
        float[] corrected = new float[sampleCount];
        System.Array.Copy(source, corrected, sampleCount);
        if (options.ApplyGainMaps)
        {
            ApplyGainMaps(corrected, image, options, rowStride);
        }
        if (options.ApplyChromaticAberration)
        {
            float[] caSource = new float[sampleCount];
            System.Array.Copy(corrected, caSource, sampleCount);
            ApplyChromaticAberration(caSource, corrected, image, options, rowStride);
        }
        RawSensorImage result = new RawSensorImage(info, null, corrected, rowStride, true, image.StoredImage, image.UserCrop, image.MaskedAreas, image.BlackLevel, image.WhiteLevels, image.WhiteBalance, image.ColorProfiles, image.Diagnostics);
        result.CropModel = image.CropModel;
        result.ClippingMask = RawDefectProcessor.CopyClippingMask(image.ClippingMask, info.Width, info.Height, channelCount, rowStride);
        result.DefectMap = CopyDefectMap(image.DefectMap, info.Width, info.Height, channelCount, rowStride);
        result.BandingCorrection = image.BandingCorrection;
        result.GainMaps = image.GainMaps;
        result.ChromaticAberration = image.ChromaticAberration;
        result.GainMapsApplied = image.GainMapsApplied || options.ApplyGainMaps;
        result.ChromaticAberrationApplied = image.ChromaticAberrationApplied || options.ApplyChromaticAberration;
        return result;
    }

    internal static void Validate(RawSensorImage image, RawLensCorrectionOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and lens-correction options are required.", -1);
        }
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).ApplyGainMaps && image.GainMaps.Length == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Gain-map correction requires complete embedded gain metadata.", -1);
        }
        if (options.ApplyChromaticAberration && image.ChromaticAberration == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Raw chromatic-aberration correction requires complete embedded calibration.", -1);
        }
        if (options.ApplyGainMaps && image.GainMapsApplied || options.ApplyChromaticAberration && image.ChromaticAberrationApplied)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A lens correction cannot be applied more than once to the same samples.", -1);
        }
        if (options.ApplyGainMaps)
        {
            ValidateGainCoverage(image);
        }
        options.CheckCancellation();
    }

    internal static void ValidateGainCoverage(RawSensorImage image)
    {
        RawArea area = CorrectionArea(image);
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            int x = area.X;
            while (x < area.X + area.Width)
            {
                if (!RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    bool covered = false;
                    int mapIndex = 0;
                    while (mapIndex < image.GainMaps.Length)
                    {
                        covered = covered || image.GainMaps[mapIndex].Contains(x, y);
                        mapIndex++;
                    }
                    if (!covered)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Embedded gain maps must cover every visible sensor sample.", -1);
                    }
                }
                x++;
            }
            y++;
        }
    }

    internal static float[] CopySamples(RawSensorImage image, int rowStride, int sampleCount, RawLensCorrectionOptions options)
    {
        float[] result = new float[sampleCount];
        int channelCount = image.Info.GetChannelCount();
        int y = 0;
        while (y < image.Info.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Info.Width)
            {
                int channel = 0;
                while (channel < channelCount)
                {
                    int sourceIndex = y * image.RowStrideSamples + x * channelCount + channel;
                    int destinationIndex = y * rowStride + x * channelCount + channel;
                    double value = image.FloatingSamples.Length == 0 ? image.IntegerSamples[sourceIndex] / 65535.0 : image.FloatingSamples[sourceIndex];
                    if (!RawModelValidation.IsFinite(value) || value < 0.0 || value > 1.0)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Lens correction requires finite normalized samples between zero and one.", -1);
                    }
                    result[destinationIndex] = (float)value;
                    channel++;
                }
                x++;
            }
            y++;
        }
        return result;
    }

    internal static void ApplyGainMaps(float[] samples, RawSensorImage image, RawLensCorrectionOptions options, int rowStride)
    {
        RawArea area = CorrectionArea(image);
        int channelCount = image.Info.GetChannelCount();
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                if (!RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    int channel = 0;
                    while (channel < channelCount)
                    {
                        double gain = 1.0;
                        int mapIndex = 0;
                        while (mapIndex < image.GainMaps.Length)
                        {
                            RawGainMap map = image.GainMaps[mapIndex];
                            if (map.Contains(x, y))
                            {
                                gain *= GainAt(map, image.Info, x, y, channel);
                            }
                            mapIndex++;
                        }
                        int index = y * rowStride + x * channelCount + channel;
                        double value = samples[index] * gain;
                        samples[index] = value > 1.0 ? 1.0F : (float)value;
                        channel++;
                    }
                }
                x++;
            }
            y++;
        }
    }

    internal static double GainAt(RawGainMap map, RawImageInfo info, int x, int y, int channel)
    {
        double gridX = map.Width == 1 || map.MapWidth == 1 ? 0.0 : (double)(x - map.X) * (map.MapWidth - 1) / (map.Width - 1);
        double gridY = map.Height == 1 || map.MapHeight == 1 ? 0.0 : (double)(y - map.Y) * (map.MapHeight - 1) / (map.Height - 1);
        int x0 = (int)gridX;
        int y0 = (int)gridY;
        int x1 = x0 + 1 < map.MapWidth ? x0 + 1 : x0;
        int y1 = y0 + 1 < map.MapHeight ? y0 + 1 : y0;
        double fractionX = gridX - x0;
        double fractionY = gridY - y0;
        int plane = GainPlane(map, info, x, y, channel);
        double top = MapValue(map, x0, y0, plane) * (1.0 - fractionX) + MapValue(map, x1, y0, plane) * fractionX;
        double bottom = MapValue(map, x0, y1, plane) * (1.0 - fractionX) + MapValue(map, x1, y1, plane) * fractionX;
        return top * (1.0 - fractionY) + bottom * fractionY;
    }

    internal static int GainPlane(RawGainMap map, RawImageInfo info, int x, int y, int channel)
    {
        if (map.PlaneCount == 1)
        {
            return 0;
        }
        if (map.PlaneCount == info.GetChannelCount())
        {
            return channel;
        }
        return y % (info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepeatHeight * info.Cfa.RepeatWidth + x % info.Cfa.RepeatWidth;
    }

    internal static double MapValue(RawGainMap map, int x, int y, int plane)
    {
        return map.Gains[(y * map.MapWidth + x) * map.PlaneCount + plane];
    }

    internal static void ApplyChromaticAberration(float[] source, float[] destination, RawSensorImage image, RawLensCorrectionOptions options, int rowStride)
    {
        RawArea area = CorrectionArea(image);
        RawCfaPattern? cfa = image.Info.Cfa;
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                RawCfaColor color = (cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                if ((color == RawCfaColor.Red || color == RawCfaColor.Blue) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    double scale = color == RawCfaColor.Red ? (image.ChromaticAberration ?? throw new System.InvalidOperationException("A required value is unavailable.")).RedScale : (image.ChromaticAberration ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlueScale;
                    double sourceX = image.ChromaticAberration.CenterX + (x - image.ChromaticAberration.CenterX) * scale;
                    double sourceY = image.ChromaticAberration.CenterY + (y - image.ChromaticAberration.CenterY) * scale;
                    destination[y * rowStride + x] = (float)SampleCfaPhase(source, image, rowStride, x, y, sourceX, sourceY);
                }
                x++;
            }
            y++;
        }
    }

    internal static double SampleCfaPhase(float[] source, RawSensorImage image, int rowStride, int phaseSourceX, int phaseSourceY, double sourceX, double sourceY)
    {
        RawCfaPattern? cfa = image.Info.Cfa;
        int phaseX = phaseSourceX % (cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepeatWidth;
        int phaseY = phaseSourceY % cfa.RepeatHeight;
        int maximumX = (image.Info.Width - 1 - phaseX) / cfa.RepeatWidth;
        int maximumY = (image.Info.Height - 1 - phaseY) / cfa.RepeatHeight;
        double gridX = (sourceX - phaseX) / cfa.RepeatWidth;
        double gridY = (sourceY - phaseY) / cfa.RepeatHeight;
        if (gridX < 0.0)
        {
            gridX = 0.0;
        }
        else if (gridX > maximumX)
        {
            gridX = maximumX;
        }
        if (gridY < 0.0)
        {
            gridY = 0.0;
        }
        else if (gridY > maximumY)
        {
            gridY = maximumY;
        }
        int x0 = (int)gridX;
        int y0 = (int)gridY;
        int x1 = x0 < maximumX ? x0 + 1 : x0;
        int y1 = y0 < maximumY ? y0 + 1 : y0;
        double fractionX = gridX - x0;
        double fractionY = gridY - y0;
        double result = 0.0;
        double weight = 0.0;
        result += WeightedCfaSample(source, image, rowStride, phaseX + x0 * cfa.RepeatWidth, phaseY + y0 * cfa.RepeatHeight, (1.0 - fractionX) * (1.0 - fractionY));
        weight += ValidCfaWeight(image, phaseX + x0 * cfa.RepeatWidth, phaseY + y0 * cfa.RepeatHeight, (1.0 - fractionX) * (1.0 - fractionY));
        result += WeightedCfaSample(source, image, rowStride, phaseX + x1 * cfa.RepeatWidth, phaseY + y0 * cfa.RepeatHeight, fractionX * (1.0 - fractionY));
        weight += ValidCfaWeight(image, phaseX + x1 * cfa.RepeatWidth, phaseY + y0 * cfa.RepeatHeight, fractionX * (1.0 - fractionY));
        result += WeightedCfaSample(source, image, rowStride, phaseX + x0 * cfa.RepeatWidth, phaseY + y1 * cfa.RepeatHeight, (1.0 - fractionX) * fractionY);
        weight += ValidCfaWeight(image, phaseX + x0 * cfa.RepeatWidth, phaseY + y1 * cfa.RepeatHeight, (1.0 - fractionX) * fractionY);
        result += WeightedCfaSample(source, image, rowStride, phaseX + x1 * cfa.RepeatWidth, phaseY + y1 * cfa.RepeatHeight, fractionX * fractionY);
        weight += ValidCfaWeight(image, phaseX + x1 * cfa.RepeatWidth, phaseY + y1 * cfa.RepeatHeight, fractionX * fractionY);
        if (weight <= 0.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Chromatic-aberration scaling requires an unmasked same-phase source sample.", -1);
        }
        return result / weight;
    }

    internal static double WeightedCfaSample(float[] source, RawSensorImage image, int rowStride, int x, int y, double weight)
    {
        if (weight <= 0.0 || RawDefectProcessor.Masked(image.MaskedAreas, x, y))
        {
            return 0.0;
        }
        return source[y * rowStride + x] * weight;
    }

    internal static double ValidCfaWeight(RawSensorImage image, int x, int y, double weight)
    {
        if (weight <= 0.0 || RawDefectProcessor.Masked(image.MaskedAreas, x, y))
        {
            return 0.0;
        }
        return weight;
    }

    internal static RawDefectMap? CopyDefectMap(RawDefectMap? source, int width, int height, int channelCount, int rowStride)
    {
        if (source == null)
        {
            return null;
        }
        byte[] repaired = new byte[rowStride * height];
        int y = 0;
        while (y < height)
        {
            System.Array.Copy(source.RepairedSamples, y * source.RowStrideSamples, repaired, y * rowStride, width * channelCount);
            y++;
        }
        return new RawDefectMap(width, height, channelCount, rowStride, repaired, source.Defects, source.GreenEquilibriumApplied);
    }

    internal static RawArea CorrectionArea(RawSensorImage image)
    {
        return image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
    }
}
