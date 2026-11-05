namespace raw;

internal class RawNearestSample
{
    internal bool Found;
    internal double Value;
    internal long Distance;
    internal int X;
    internal int Y;

    internal RawNearestSample()
    {
        Found = false;
        Value = 0.0;
        Distance = long.MaxValue;
        X = int.MaxValue;
        Y = int.MaxValue;
    }
}

internal static class RawDemosaicer
{
    internal static RawRgbImage Reconstruct(RawSensorImage image, RawRenderOptions options)
    {
        Validate(image, options);
        if (image.Info.Cfa == null || options.DemosaicMode == RawDemosaicMode.None)
        {
            return CopyLinear(image, options);
        }
        return ReconstructBayer(image, options);
    }

    internal static void Validate(RawSensorImage image, RawRenderOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and render options are required for reconstruction.", -1);
        }
        RawPhotometricInterpretation photometric = image.Info.Photometric;
        bool passThrough = photometric == RawPhotometricInterpretation.WhiteIsZero || photometric == RawPhotometricInterpretation.BlackIsZero || photometric == RawPhotometricInterpretation.Rgb || photometric == RawPhotometricInterpretation.LinearRaw;
        if (image.Info.Cfa == null && !passThrough)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Linear reconstruction supports monochrome, RGB, linear raw, or CFA sensor samples.", -1);
        }
        if (image.Info.Cfa == null && image.Info.GetChannelCount() != 1 && image.Info.GetChannelCount() != 3 && image.Info.GetChannelCount() != 4)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Linear pass-through output requires one, three, or four logical channels.", -1);
        }
        if (image.Info.Cfa != null && (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).DemosaicMode != RawDemosaicMode.None)
        {
            if (options.DemosaicMode != RawDemosaicMode.NearestNeighbor && options.DemosaicMode != RawDemosaicMode.Bilinear && options.DemosaicMode != RawDemosaicMode.Vng && options.DemosaicMode != RawDemosaicMode.Ahd)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "This reconstruction stage supports nearest-neighbor, bilinear, VNG, and AHD Bayer demosaic.", -1);
            }
            if (image.Info.Cfa.RepeatWidth != 2 || image.Info.Cfa.RepeatHeight != 2 || !RawFastProcessor.BayerColors(image.Info.Cfa))
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Bayer reconstruction requires a two-by-two Bayer CFA.", -1);
            }
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
    }

    internal static RawRgbImage CopyLinear(RawSensorImage image, RawRenderOptions options)
    {
        RawArea area = RawDefectProcessor.SelectedArea(image);
        int channelCount = image.Info.GetChannelCount();
        long pixelCount = CheckedMath.PixelCount(area.Width, area.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] samples = new float[sampleCount];
        int outputY = 0;
        while (outputY < area.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < area.Width)
            {
                int channel = 0;
                while (channel < channelCount)
                {
                    double value = RawFastProcessor.ReadNormalized(image, area.X + outputX, area.Y + outputY, channel);
                    if (image.Info.Photometric == RawPhotometricInterpretation.WhiteIsZero)
                    {
                        value = 1.0 - value;
                    }
                    samples[(outputY * area.Width + outputX) * channelCount + channel] = (float)value;
                    channel++;
                }
                outputX++;
            }
            outputY++;
        }
        RawChannelDescription[] channels = new RawChannelDescription[channelCount];
        int index = 0;
        while (index < channelCount)
        {
            channels[index] = image.Info.Channels[index];
            index++;
        }
        return new RawRgbImage(area.Width, area.Height, channelCount, area.Width * channelCount, null, null, samples, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, image.Info.Orientation, false, true, image.Diagnostics);
    }

    internal static RawRgbImage ReconstructBayer(RawSensorImage image, RawRenderOptions options)
    {
        switch (options.DemosaicMode)
        {
            case RawDemosaicMode.NearestNeighbor:
            case RawDemosaicMode.Bilinear:
                return ReconstructSimpleBayer(image, options);
            case RawDemosaicMode.Vng:
                return RawVngDemosaicer.Reconstruct(image, options);
            case RawDemosaicMode.Ahd:
                return RawAhdDemosaicer.Reconstruct(image, options);
            default:
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The selected Bayer demosaic mode has no reconstruction stage.", -1);
        }
    }

    internal static RawRgbImage ReconstructSimpleBayer(RawSensorImage image, RawRenderOptions options)
    {
        RawArea area = RawDefectProcessor.SelectedArea(image);
        long pixelCount = CheckedMath.PixelCount(area.Width, area.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, 3);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] samples = new float[sampleCount];
        int outputY = 0;
        while (outputY < area.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < area.Width)
            {
                int x = area.X + outputX;
                int y = area.Y + outputY;
                int destination = (outputY * area.Width + outputX) * 3;
                samples[destination] = (float)ColorValue(image, options, x, y, RawCfaColor.Red);
                samples[destination + 1] = (float)ColorValue(image, options, x, y, RawCfaColor.Green);
                samples[destination + 2] = (float)ColorValue(image, options, x, y, RawCfaColor.Blue);
                outputX++;
            }
            outputY++;
        }
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(area.Width, area.Height, 3, area.Width * 3, null, null, samples, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, image.Info.Orientation, false, true, image.Diagnostics);
    }

    internal static double ColorValue(RawSensorImage image, RawRenderOptions options, int x, int y, RawCfaColor wanted)
    {
        RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
        if (MatchingColor(actual, wanted) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y))
        {
            return RawFastProcessor.ReadNormalized(image, x, y, 0);
        }
        if (options.DemosaicMode == RawDemosaicMode.Bilinear)
        {
            double average = AdjacentAverage(image, x, y, wanted);
            if (average >= 0.0)
            {
                return average;
            }
        }
        return NearestValue(image, x, y, wanted, options);
    }

    internal static double AdjacentAverage(RawSensorImage image, int x, int y, RawCfaColor wanted)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        double sum = 0.0;
        int count = 0;
        int neighborY = y - 1;
        while (neighborY <= y + 1)
        {
            int neighborX = x - 1;
            while (neighborX <= x + 1)
            {
                if ((neighborX != x || neighborY != y) && visible.Contains(neighborX, neighborY) && !RawDefectProcessor.Masked(image.MaskedAreas, neighborX, neighborY) && MatchingColor((image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(neighborX, neighborY), wanted))
                {
                    sum += RawFastProcessor.ReadNormalized(image, neighborX, neighborY, 0);
                    count++;
                }
                neighborX++;
            }
            neighborY++;
        }
        return count == 0 ? -1.0 : sum / count;
    }

    internal static double NearestValue(RawSensorImage image, int x, int y, RawCfaColor wanted, RawRenderOptions options)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int maximumRadius = visible.Width > visible.Height ? visible.Width : visible.Height;
        int radius = 1;
        while (radius <= maximumRadius)
        {
            options.CheckCancellation();
            RawNearestSample nearest = new RawNearestSample();
            int left = x - radius;
            int right = x + radius;
            int top = y - radius;
            int bottom = y + radius;
            int candidateX = left;
            while (candidateX <= right)
            {
                Consider(image, visible, candidateX, top, x, y, wanted, nearest);
                if (bottom != top)
                {
                    Consider(image, visible, candidateX, bottom, x, y, wanted, nearest);
                }
                candidateX++;
            }
            int candidateY = top + 1;
            while (candidateY < bottom)
            {
                Consider(image, visible, left, candidateY, x, y, wanted, nearest);
                if (right != left)
                {
                    Consider(image, visible, right, candidateY, x, y, wanted, nearest);
                }
                candidateY++;
            }
            if (nearest.Found)
            {
                return nearest.Value;
            }
            radius++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Bayer reconstruction requires an unmasked red, green, and blue source sample.", -1);
    }

    internal static void Consider(RawSensorImage image, RawArea visible, int candidateX, int candidateY, int x, int y, RawCfaColor wanted, RawNearestSample nearest)
    {
        if (!visible.Contains(candidateX, candidateY) || RawDefectProcessor.Masked(image.MaskedAreas, candidateX, candidateY) || !MatchingColor((image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(candidateX, candidateY), wanted))
        {
            return;
        }
        long deltaX = candidateX - x;
        long deltaY = candidateY - y;
        long distance = deltaX * deltaX + deltaY * deltaY;
        if (!nearest.Found || distance < nearest.Distance || distance == nearest.Distance && (candidateY < nearest.Y || candidateY == nearest.Y && candidateX < nearest.X))
        {
            nearest.Found = true;
            nearest.Value = RawFastProcessor.ReadNormalized(image, candidateX, candidateY, 0);
            nearest.Distance = distance;
            nearest.X = candidateX;
            nearest.Y = candidateY;
        }
    }

    internal static bool MatchingColor(RawCfaColor actual, RawCfaColor wanted)
    {
        if (wanted == RawCfaColor.Green)
        {
            return actual == RawCfaColor.Green || actual == RawCfaColor.Green2;
        }
        return actual == wanted;
    }
}
