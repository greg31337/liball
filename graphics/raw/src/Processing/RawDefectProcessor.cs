namespace raw;

internal static class RawDefectProcessor
{
    internal static RawSensorImage Repair(RawSensorImage image, RawDefectOptions options)
    {
        Validate(image, options);
        RawImageInfo info = image.Info;
        int channelCount = info.GetChannelCount();
        int rowStride = CheckedMath.ToInt32(CheckedMath.Multiply(info.Width, channelCount));
        long sampleCountLong = CheckedMath.Multiply(rowStride, info.Height);
        CheckedMath.DecodedSize(sampleCountLong, 14, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 1, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] source = CopySamples(image, rowStride, sampleCount, options);
        float[] output = new float[sampleCount];
        System.Array.Copy(source, output, sampleCount);
        byte[] pixelDefects = new byte[sampleCount];
        byte[] badRows = new byte[info.Height * channelCount];
        byte[] badColumns = new byte[info.Width * channelCount];
        byte[] repaired = new byte[sampleCount];
        System.Collections.Generic.List<RawDefect> defects = new System.Collections.Generic.List<RawDefect>();
        AddDeclared(options.MetadataDefects, RawDefectSource.Metadata, info, channelCount, options, defects, pixelDefects, badRows, badColumns);
        AddDeclared(options.CallerDefects, RawDefectSource.Caller, info, channelCount, options, defects, pixelDefects, badRows, badColumns);
        Detect(source, image, options, rowStride, pixelDefects, badRows, badColumns, defects);
        RepairSamples(source, output, image, options, rowStride, pixelDefects, badRows, badColumns, repaired);
        bool greenApplied = ApplyGreenEquilibrium(output, image, options, rowStride);
        RawSensorImage result = new RawSensorImage(info, null, output, rowStride, true, image.StoredImage, image.UserCrop, image.MaskedAreas, image.BlackLevel, image.WhiteLevels, image.WhiteBalance, image.ColorProfiles, image.Diagnostics);
        result.CropModel = image.CropModel;
        result.ClippingMask = CopyClippingMask(image.ClippingMask, info.Width, info.Height, channelCount, rowStride);
        result.DefectMap = new RawDefectMap(info.Width, info.Height, channelCount, rowStride, repaired, defects.ToArray(), greenApplied);
        result.BandingCorrection = image.BandingCorrection;
        result.GainMaps = image.GainMaps;
        result.ChromaticAberration = image.ChromaticAberration;
        result.GainMapsApplied = image.GainMapsApplied;
        result.ChromaticAberrationApplied = image.ChromaticAberrationApplied;
        return result;
    }

    internal static void Validate(RawSensorImage image, RawDefectOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and defect options are required.", -1);
        }
        ValidateDeclarations((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).MetadataDefects, RawDefectSource.Metadata, image.Info);
        ValidateDeclarations(options.CallerDefects, RawDefectSource.Caller, image.Info);
        if (options.ApplyGreenEquilibrium && image.Info.Cfa == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Green equilibrium requires a color filter array.", -1);
        }
        options.CheckCancellation();
    }

    internal static void ValidateDeclarations(RawDefect[] declarations, RawDefectSource source, RawImageInfo info)
    {
        int channelCount = info.GetChannelCount();
        int index = 0;
        while (index < declarations.Length)
        {
            RawDefect defect = declarations[index];
            if (defect == null || defect.Source != source || !defect.Validate(info.Width, info.Height, channelCount).IsValid())
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Every defect declaration must be valid and match its metadata or caller collection.", -1);
            }
            index++;
        }
    }

    internal static float[] CopySamples(RawSensorImage image, int rowStride, int sampleCount, RawDefectOptions options)
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
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Defect repair requires finite normalized samples between zero and one.", -1);
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

    internal static void AddDeclared(RawDefect[] declarations, RawDefectSource source, RawImageInfo info, int channelCount, RawDefectOptions options, System.Collections.Generic.List<RawDefect> defects, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            RawDefect defect = declarations[index];
            bool applied = defect.Kind == RawDefectKind.Pixel || defect.Kind == RawDefectKind.Row && options.RepairRows || defect.Kind == RawDefectKind.Column && options.RepairColumns;
            if (applied)
            {
                RawDefect copy = new RawDefect(defect.Kind, source, defect.X, defect.Y, defect.Channel);
                defects.Add(copy);
                Mark(copy, info.Width, channelCount, pixelDefects, badRows, badColumns);
            }
            index++;
        }
    }

    internal static void Mark(RawDefect defect, int width, int channelCount, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        int firstChannel = defect.Channel < 0 ? 0 : defect.Channel;
        int endChannel = defect.Channel < 0 ? channelCount : defect.Channel + 1;
        int channel = firstChannel;
        while (channel < endChannel)
        {
            if (defect.Kind == RawDefectKind.Row)
            {
                badRows[defect.Y * channelCount + channel] = 1;
            }
            else if (defect.Kind == RawDefectKind.Column)
            {
                badColumns[defect.X * channelCount + channel] = 1;
            }
            else
            {
                pixelDefects[(defect.Y * width + defect.X) * channelCount + channel] = 1;
            }
            channel++;
        }
    }

    internal static void Detect(float[] source, RawSensorImage image, RawDefectOptions options, int rowStride, byte[] pixelDefects, byte[] badRows, byte[] badColumns, System.Collections.Generic.List<RawDefect> defects)
    {
        if (!options.DetectHotPixels && !options.DetectDeadPixels)
        {
            return;
        }
        RawArea area = SelectedArea(image);
        int channelCount = image.Info.GetChannelCount();
        System.Collections.Generic.List<RawDefect> detected = new System.Collections.Generic.List<RawDefect>();
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                if (!Masked(image.MaskedAreas, x, y))
                {
                    int channel = 0;
                    while (channel < channelCount)
                    {
                        int sampleIndex = y * rowStride + x * channelCount + channel;
                        if (pixelDefects[sampleIndex] == 0 && badRows[y * channelCount + channel] == 0 && badColumns[x * channelCount + channel] == 0)
                        {
                            int detectionRadius = image.Info.Cfa == null ? 1 : 4;
                            double[] neighbors = NeighborValues(source, image, rowStride, x, y, channel, detectionRadius, pixelDefects, badRows, badColumns);
                            if (neighbors.Length >= 4)
                            {
                                double median = Median(neighbors);
                                double[] deviations = new double[neighbors.Length];
                                int deviationIndex = 0;
                                while (deviationIndex < neighbors.Length)
                                {
                                    deviations[deviationIndex] = System.Math.Abs(neighbors[deviationIndex] - median);
                                    deviationIndex++;
                                }
                                double mad = Median(deviations);
                                double required = options.DetectionMinimumDifference;
                                if (mad * options.DetectionNoiseMultiplier > required)
                                {
                                    required = mad * options.DetectionNoiseMultiplier;
                                }
                                double value = source[sampleIndex];
                                if (options.DetectHotPixels && value >= options.HotPixelMinimum && value - median >= required && IsolatedExtreme(source, image, rowStride, x, y, channel, detectionRadius, true, options))
                                {
                                    detected.Add(new RawDefect(RawDefectKind.HotPixel, RawDefectSource.Detected, x, y, channel));
                                }
                                else if (options.DetectDeadPixels && value <= options.DeadPixelMaximum && median - value >= required && IsolatedExtreme(source, image, rowStride, x, y, channel, detectionRadius, false, options))
                                {
                                    detected.Add(new RawDefect(RawDefectKind.DeadPixel, RawDefectSource.Detected, x, y, channel));
                                }
                            }
                        }
                        channel++;
                    }
                }
                x++;
            }
            y++;
        }
        int index = 0;
        while (index < detected.Count)
        {
            RawDefect defect = detected[index];
            defects.Add(defect);
            Mark(defect, image.Info.Width, channelCount, pixelDefects, badRows, badColumns);
            index++;
        }
    }

    internal static bool IsolatedExtreme(float[] source, RawSensorImage image, int rowStride, int x, int y, int channel, int radius, bool hot, RawDefectOptions options)
    {
        int channelCount = image.Info.GetChannelCount();
        int neighborY = y - radius;
        while (neighborY <= y + radius)
        {
            int neighborX = x - radius;
            while (neighborX <= x + radius)
            {
                if ((neighborX != x || neighborY != y) && neighborX >= 0 && neighborY >= 0 && neighborX < image.Info.Width && neighborY < image.Info.Height && SameColor(image.Info.Cfa, neighborX, neighborY, x, y))
                {
                    double value = source[neighborY * rowStride + neighborX * channelCount + channel];
                    if (hot && value >= options.HotPixelMinimum || !hot && value <= options.DeadPixelMaximum)
                    {
                        return false;
                    }
                }
                neighborX++;
            }
            neighborY++;
        }
        return true;
    }

    internal static void RepairSamples(float[] source, float[] output, RawSensorImage image, RawDefectOptions options, int rowStride, byte[] pixelDefects, byte[] badRows, byte[] badColumns, byte[] repaired)
    {
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
                    int index = y * rowStride + x * channelCount + channel;
                    bool pixel = pixelDefects[index] != 0;
                    bool row = badRows[y * channelCount + channel] != 0;
                    bool column = badColumns[x * channelCount + channel] != 0;
                    if (pixel || row && column)
                    {
                        double[] neighbors = NeighborValues(source, image, rowStride, x, y, channel, 4, pixelDefects, badRows, badColumns);
                        if (neighbors.Length == 0)
                        {
                            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A defective sample requires at least one valid same-color neighbor.", -1);
                        }
                        output[index] = (float)Median(neighbors);
                        repaired[index] = 1;
                    }
                    else if (row)
                    {
                        output[index] = (float)RowReplacement(source, image, rowStride, x, y, channel, pixelDefects, badRows, badColumns);
                        repaired[index] = 1;
                    }
                    else if (column)
                    {
                        output[index] = (float)ColumnReplacement(source, image, rowStride, x, y, channel, pixelDefects, badRows, badColumns);
                        repaired[index] = 1;
                    }
                    channel++;
                }
                x++;
            }
            y++;
        }
    }

    internal static double RowReplacement(float[] source, RawSensorImage image, int rowStride, int x, int y, int channel, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        double sum = 0.0;
        int count = 0;
        int distance = 1;
        while (distance < image.Info.Height && count < 2)
        {
            int before = y - distance;
            int after = y + distance;
            if (before >= 0 && ValidReplacement(image, x, before, x, y, channel, rowStride, pixelDefects, badRows, badColumns))
            {
                sum += source[before * rowStride + x * image.Info.GetChannelCount() + channel];
                count++;
            }
            if (after < image.Info.Height && ValidReplacement(image, x, after, x, y, channel, rowStride, pixelDefects, badRows, badColumns))
            {
                sum += source[after * rowStride + x * image.Info.GetChannelCount() + channel];
                count++;
            }
            distance++;
        }
        if (count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A defective row requires a valid same-color row neighbor.", -1);
        }
        return sum / count;
    }

    internal static double ColumnReplacement(float[] source, RawSensorImage image, int rowStride, int x, int y, int channel, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        double sum = 0.0;
        int count = 0;
        int distance = 1;
        while (distance < image.Info.Width && count < 2)
        {
            int before = x - distance;
            int after = x + distance;
            if (before >= 0 && ValidReplacement(image, before, y, x, y, channel, rowStride, pixelDefects, badRows, badColumns))
            {
                sum += source[y * rowStride + before * image.Info.GetChannelCount() + channel];
                count++;
            }
            if (after < image.Info.Width && ValidReplacement(image, after, y, x, y, channel, rowStride, pixelDefects, badRows, badColumns))
            {
                sum += source[y * rowStride + after * image.Info.GetChannelCount() + channel];
                count++;
            }
            distance++;
        }
        if (count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A defective column requires a valid same-color column neighbor.", -1);
        }
        return sum / count;
    }

    internal static bool ValidReplacement(RawSensorImage image, int x, int y, int originalX, int originalY, int channel, int rowStride, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        int channelCount = image.Info.GetChannelCount();
        int index = y * rowStride + x * channelCount + channel;
        return SameColor(image.Info.Cfa, x, y, originalX, originalY) && !Masked(image.MaskedAreas, x, y) && pixelDefects[index] == 0 && badRows[y * channelCount + channel] == 0 && badColumns[x * channelCount + channel] == 0;
    }

    internal static double[] NeighborValues(float[] source, RawSensorImage image, int rowStride, int x, int y, int channel, int radius, byte[] pixelDefects, byte[] badRows, byte[] badColumns)
    {
        System.Collections.Generic.List<double> values = new System.Collections.Generic.List<double>();
        int channelCount = image.Info.GetChannelCount();
        int neighborY = y - radius;
        while (neighborY <= y + radius)
        {
            int neighborX = x - radius;
            while (neighborX <= x + radius)
            {
                if ((neighborX != x || neighborY != y) && neighborX >= 0 && neighborY >= 0 && neighborX < image.Info.Width && neighborY < image.Info.Height && SameColor(image.Info.Cfa, neighborX, neighborY, x, y) && !Masked(image.MaskedAreas, neighborX, neighborY))
                {
                    int index = neighborY * rowStride + neighborX * channelCount + channel;
                    if (pixelDefects[index] == 0 && badRows[neighborY * channelCount + channel] == 0 && badColumns[neighborX * channelCount + channel] == 0)
                    {
                        values.Add(source[index]);
                    }
                }
                neighborX++;
            }
            neighborY++;
        }
        return values.ToArray();
    }

    internal static bool SameColor(RawCfaPattern? cfa, int firstX, int firstY, int secondX, int secondY)
    {
        return cfa == null || cfa.GetColorUnchecked(firstX, firstY) == cfa.GetColorUnchecked(secondX, secondY);
    }

    internal static bool Masked(RawArea[] areas, int x, int y)
    {
        int index = 0;
        while (index < areas.Length)
        {
            if (areas[index].Contains(x, y))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static double Median(double[] values)
    {
        System.Array.Sort(values);
        int middle = values.Length / 2;
        if (values.Length % 2 == 0)
        {
            return (values[middle - 1] + values[middle]) * 0.5;
        }
        return values[middle];
    }

    internal static RawArea SelectedArea(RawSensorImage image)
    {
        if (image.CropModel != null && image.CropModel.SelectedArea != null)
        {
            return image.CropModel.SelectedArea;
        }
        if (image.UserCrop != null)
        {
            return image.UserCrop;
        }
        if (image.Info.DefaultCrop != null)
        {
            return image.Info.DefaultCrop;
        }
        if (image.Info.ActiveArea != null)
        {
            return image.Info.ActiveArea;
        }
        return image.Info.StoredArea;
    }

    internal static bool ApplyGreenEquilibrium(float[] output, RawSensorImage image, RawDefectOptions options, int rowStride)
    {
        if (!options.ApplyGreenEquilibrium)
        {
            return false;
        }
        RawCfaPattern? cfa = image.Info.Cfa;
        int cellCount = (cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepeatWidth * cfa.RepeatHeight;
        double[] sums = new double[cellCount];
        long[] counts = new long[cellCount];
        int greenCells = 0;
        int cell = 0;
        while (cell < cellCount)
        {
            RawCfaColor color = cfa.Colors[cell];
            if (color == RawCfaColor.Green || color == RawCfaColor.Green2)
            {
                greenCells++;
            }
            cell++;
        }
        if (greenCells < 2)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Green equilibrium requires at least two green CFA phases.", -1);
        }
        RawArea area = SelectedArea(image);
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                RawCfaColor color = cfa.GetColorUnchecked(x, y);
                if ((color == RawCfaColor.Green || color == RawCfaColor.Green2) && !Masked(image.MaskedAreas, x, y))
                {
                    int phase = y % cfa.RepeatHeight * cfa.RepeatWidth + x % cfa.RepeatWidth;
                    sums[phase] += output[y * rowStride + x * image.Info.GetChannelCount()];
                    counts[phase]++;
                }
                x++;
            }
            y++;
        }
        double target = 0.0;
        cell = 0;
        while (cell < cellCount)
        {
            RawCfaColor color = cfa.Colors[cell];
            if (color == RawCfaColor.Green || color == RawCfaColor.Green2)
            {
                if (counts[cell] == 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Every green CFA phase requires visible samples for equilibrium.", -1);
                }
                sums[cell] /= counts[cell];
                target += sums[cell];
            }
            cell++;
        }
        target /= greenCells;
        if (target <= 0.0)
        {
            return false;
        }
        cell = 0;
        while (cell < cellCount)
        {
            RawCfaColor color = cfa.Colors[cell];
            if ((color == RawCfaColor.Green || color == RawCfaColor.Green2) && (sums[cell] <= 0.0 || System.Math.Abs(sums[cell] - target) / target > options.GreenEquilibriumMaximumRelativeDifference))
            {
                return false;
            }
            cell++;
        }
        y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                RawCfaColor color = cfa.GetColorUnchecked(x, y);
                if ((color == RawCfaColor.Green || color == RawCfaColor.Green2) && !Masked(image.MaskedAreas, x, y))
                {
                    int phase = y % cfa.RepeatHeight * cfa.RepeatWidth + x % cfa.RepeatWidth;
                    int sample = y * rowStride + x * image.Info.GetChannelCount();
                    double value = output[sample] * target / sums[phase];
                    output[sample] = value > 1.0 ? 1.0F : (float)value;
                }
                x++;
            }
            y++;
        }
        return true;
    }

    internal static RawClippingMask? CopyClippingMask(RawClippingMask? source, int width, int height, int channelCount, int rowStride)
    {
        if (source == null)
        {
            return null;
        }
        byte[] black = new byte[rowStride * height];
        byte[] white = new byte[rowStride * height];
        int y = 0;
        while (y < height)
        {
            int count = width * channelCount;
            System.Array.Copy(source.BlackClipped, y * source.RowStrideSamples, black, y * rowStride, count);
            System.Array.Copy(source.WhiteClipped, y * source.RowStrideSamples, white, y * rowStride, count);
            y++;
        }
        return new RawClippingMask(width, height, channelCount, rowStride, black, white);
    }
}
