namespace raw;

internal class RawDirectionalValue
{
    internal bool Valid;
    internal double Value;
    internal double Gradient;

    internal RawDirectionalValue()
    {
        Valid = false;
        Value = 0.0;
        Gradient = 0.0;
    }

    internal void Reset()
    {
        Valid = false;
        Value = 0.0;
        Gradient = 0.0;
    }
}

internal static class RawVngDemosaicer
{
    internal static RawRgbImage Reconstruct(RawSensorImage image, RawRenderOptions options)
    {
        RawArea area = RawDefectProcessor.SelectedArea(image);
        long outputPixels = CheckedMath.PixelCount(area.Width, area.Height, options.Limits);
        long outputSamplesLong = CheckedMath.Multiply(outputPixels, 3);
        long greenSamplesLong = CheckedMath.Multiply(image.Info.Width, image.Info.Height);
        long combinedSamples = CheckedMath.Add(CheckedMath.Multiply(outputSamplesLong, 2), greenSamplesLong);
        CheckedMath.DecodedSize(combinedSamples, 4, options.Limits);
        CheckedMath.AllocationSize(outputSamplesLong, 4, options.Limits);
        CheckedMath.AllocationSize(greenSamplesLong, 4, options.Limits);
        int outputSamples = CheckedMath.ToInt32(outputSamplesLong);
        int greenSamples = CheckedMath.ToInt32(greenSamplesLong);
        float[] green = new float[greenSamples];
        BuildGreen(image, options, green);
        float[] initial = new float[outputSamples];
        RawDirectionalValue firstDirection = new RawDirectionalValue();
        RawDirectionalValue secondDirection = new RawDirectionalValue();
        int outputY = 0;
        while (outputY < area.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < area.Width)
            {
                int x = area.X + outputX;
                int y = area.Y + outputY;
                RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                int destination = (outputY * area.Width + outputX) * 3;
                initial[destination + 1] = green[y * image.Info.Width + x];
                initial[destination] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Red) ? (float)RawFastProcessor.ReadNormalized(image, x, y, 0) : (float)ChromaValue(image, green, x, y, RawCfaColor.Red, options, firstDirection, secondDirection);
                initial[destination + 2] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Blue) ? (float)RawFastProcessor.ReadNormalized(image, x, y, 0) : (float)ChromaValue(image, green, x, y, RawCfaColor.Blue, options, firstDirection, secondDirection);
                outputX++;
            }
            outputY++;
        }
        float[] suppressed = new float[outputSamples];
        SuppressFalseColor(initial, suppressed, image, area, options);
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(area.Width, area.Height, 3, area.Width * 3, null, null, suppressed, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, image.Info.Orientation, false, true, image.Diagnostics);
    }

    internal static void BuildGreen(RawSensorImage image, RawRenderOptions options, float[] green)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        RawDirectionalValue horizontal = new RawDirectionalValue();
        RawDirectionalValue vertical = new RawDirectionalValue();
        int y = visible.Y;
        while (y < visible.Y + visible.Height)
        {
            options.CheckCancellation();
            int x = visible.X;
            while (x < visible.X + visible.Width)
            {
                RawCfaColor color = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                if ((color == RawCfaColor.Green || color == RawCfaColor.Green2) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    green[y * image.Info.Width + x] = (float)RawFastProcessor.ReadNormalized(image, x, y, 0);
                }
                else
                {
                    green[y * image.Info.Width + x] = (float)InterpolateGreen(image, x, y, options, horizontal, vertical);
                }
                x++;
            }
            y++;
        }
    }

    internal static double InterpolateGreen(RawSensorImage image, int x, int y, RawRenderOptions options, RawDirectionalValue horizontal, RawDirectionalValue vertical)
    {
        GreenDirection(image, x, y, 1, 0, horizontal);
        GreenDirection(image, x, y, 0, 1, vertical);
        if (horizontal.Valid && vertical.Valid)
        {
            if (horizontal.Gradient < vertical.Gradient)
            {
                return horizontal.Value;
            }
            if (vertical.Gradient < horizontal.Gradient)
            {
                return vertical.Value;
            }
            return (horizontal.Value + vertical.Value) * 0.5;
        }
        if (horizontal.Valid)
        {
            return horizontal.Value;
        }
        if (vertical.Valid)
        {
            return vertical.Value;
        }
        double fallback = RawDemosaicer.AdjacentAverage(image, x, y, RawCfaColor.Green);
        if (fallback >= 0.0)
        {
            return fallback;
        }
        return RawDemosaicer.NearestValue(image, x, y, RawCfaColor.Green, options);
    }

    internal static void GreenDirection(RawSensorImage image, int x, int y, int stepX, int stepY, RawDirectionalValue result)
    {
        result.Reset();
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int firstX = x - stepX;
        int firstY = y - stepY;
        int secondX = x + stepX;
        int secondY = y + stepY;
        bool first = ValidColor(image, visible, firstX, firstY, RawCfaColor.Green);
        bool second = ValidColor(image, visible, secondX, secondY, RawCfaColor.Green);
        if (!first && !second)
        {
            return;
        }
        double center = RawFastProcessor.ReadNormalized(image, x, y, 0);
        double firstValue = first ? RawFastProcessor.ReadNormalized(image, firstX, firstY, 0) : 0.0;
        double secondValue = second ? RawFastProcessor.ReadNormalized(image, secondX, secondY, 0) : 0.0;
        result.Valid = true;
        result.Value = first && second ? (firstValue + secondValue) * 0.5 : first ? firstValue : secondValue;
        result.Gradient = first && second ? System.Math.Abs(firstValue - secondValue) : System.Math.Abs(center - result.Value) * 2.0;
        int farFirstX = x - stepX * 2;
        int farFirstY = y - stepY * 2;
        int farSecondX = x + stepX * 2;
        int farSecondY = y + stepY * 2;
        RawCfaColor centerColor = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
        bool farFirst = ValidExactColor(image, visible, farFirstX, farFirstY, centerColor);
        bool farSecond = ValidExactColor(image, visible, farSecondX, farSecondY, centerColor);
        if (farFirst && farSecond)
        {
            double valueFirst = RawFastProcessor.ReadNormalized(image, farFirstX, farFirstY, 0);
            double valueSecond = RawFastProcessor.ReadNormalized(image, farSecondX, farSecondY, 0);
            result.Gradient += System.Math.Abs(center * 2.0 - valueFirst - valueSecond);
        }
        else if (farFirst)
        {
            result.Gradient += System.Math.Abs(center - RawFastProcessor.ReadNormalized(image, farFirstX, farFirstY, 0));
        }
        else if (farSecond)
        {
            result.Gradient += System.Math.Abs(center - RawFastProcessor.ReadNormalized(image, farSecondX, farSecondY, 0));
        }
    }

    internal static double ChromaValue(RawSensorImage image, float[] green, int x, int y, RawCfaColor wanted, RawRenderOptions options, RawDirectionalValue first, RawDirectionalValue second)
    {
        RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
        if (actual == RawCfaColor.Red || actual == RawCfaColor.Blue)
        {
            ChromaDirection(image, green, x, y, wanted, 1, 1, first);
            ChromaDirection(image, green, x, y, wanted, 1, -1, second);
            double difference = SelectDirection(first, second);
            if (difference > -2.0)
            {
                return Clamp(green[y * image.Info.Width + x] + difference);
            }
        }
        else
        {
            ChromaDirection(image, green, x, y, wanted, 1, 0, first);
            ChromaDirection(image, green, x, y, wanted, 0, 1, second);
            double difference = SelectDirection(first, second);
            if (difference > -2.0)
            {
                return Clamp(green[y * image.Info.Width + x] + difference);
            }
        }
        double adjacent = RawDemosaicer.AdjacentAverage(image, x, y, wanted);
        if (adjacent >= 0.0)
        {
            return adjacent;
        }
        return RawDemosaicer.NearestValue(image, x, y, wanted, options);
    }

    internal static void ChromaDirection(RawSensorImage image, float[] green, int x, int y, RawCfaColor wanted, int stepX, int stepY, RawDirectionalValue result)
    {
        result.Reset();
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int firstX = x - stepX;
        int firstY = y - stepY;
        int secondX = x + stepX;
        int secondY = y + stepY;
        bool first = ValidColor(image, visible, firstX, firstY, wanted);
        bool second = ValidColor(image, visible, secondX, secondY, wanted);
        if (!first && !second)
        {
            return;
        }
        double centerGreen = green[y * image.Info.Width + x];
        double sum = 0.0;
        double gradient = 0.0;
        int count = 0;
        if (first)
        {
            double neighborGreen = green[firstY * image.Info.Width + firstX];
            sum += RawFastProcessor.ReadNormalized(image, firstX, firstY, 0) - neighborGreen;
            gradient += System.Math.Abs(centerGreen - neighborGreen);
            count++;
        }
        if (second)
        {
            double neighborGreen = green[secondY * image.Info.Width + secondX];
            sum += RawFastProcessor.ReadNormalized(image, secondX, secondY, 0) - neighborGreen;
            gradient += System.Math.Abs(centerGreen - neighborGreen);
            count++;
        }
        result.Valid = true;
        result.Value = sum / count;
        result.Gradient = gradient / count;
    }

    internal static double SelectDirection(RawDirectionalValue first, RawDirectionalValue second)
    {
        if (first.Valid && second.Valid)
        {
            if (first.Gradient < second.Gradient)
            {
                return first.Value;
            }
            if (second.Gradient < first.Gradient)
            {
                return second.Value;
            }
            return (first.Value + second.Value) * 0.5;
        }
        if (first.Valid)
        {
            return first.Value;
        }
        if (second.Valid)
        {
            return second.Value;
        }
        return -2.0;
    }

    internal static void SuppressFalseColor(float[] source, float[] destination, RawSensorImage image, RawArea area, RawRenderOptions options)
    {
        double[] differences = new double[9];
        int outputY = 0;
        while (outputY < area.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < area.Width)
            {
                int index = (outputY * area.Width + outputX) * 3;
                double green = source[index + 1];
                RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(area.X + outputX, area.Y + outputY);
                destination[index + 1] = source[index + 1];
                destination[index] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Red) ? source[index] : SuppressedChannel(source, area.Width, area.Height, outputX, outputY, 0, green, differences, 0.05);
                destination[index + 2] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Blue) ? source[index + 2] : SuppressedChannel(source, area.Width, area.Height, outputX, outputY, 2, green, differences, 0.05);
                outputX++;
            }
            outputY++;
        }
    }

    internal static float SuppressedChannel(float[] source, int width, int height, int x, int y, int channel, double green, double[] differences, double threshold)
    {
        int count = 0;
        int neighborY = y - 1;
        while (neighborY <= y + 1)
        {
            int neighborX = x - 1;
            while (neighborX <= x + 1)
            {
                if (neighborX >= 0 && neighborY >= 0 && neighborX < width && neighborY < height)
                {
                    int index = (neighborY * width + neighborX) * 3;
                    differences[count] = source[index + channel] - source[index + 1];
                    count++;
                }
                neighborX++;
            }
            neighborY++;
        }
        System.Array.Sort(differences, 0, count);
        int middle = count / 2;
        double median = count % 2 == 0 ? (differences[middle - 1] + differences[middle]) * 0.5 : differences[middle];
        int current = (y * width + x) * 3;
        double difference = source[current + channel] - source[current + 1];
        if (System.Math.Abs(difference - median) > threshold)
        {
            difference = median;
        }
        return (float)Clamp(green + difference);
    }

    internal static bool ValidColor(RawSensorImage image, RawArea visible, int x, int y, RawCfaColor wanted)
    {
        return visible.Contains(x, y) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y) && RawDemosaicer.MatchingColor((image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y), wanted);
    }

    internal static bool ValidExactColor(RawSensorImage image, RawArea visible, int x, int y, RawCfaColor wanted)
    {
        return visible.Contains(x, y) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y) && (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y) == wanted;
    }

    internal static double Clamp(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }
        if (value > 1.0)
        {
            return 1.0;
        }
        return value;
    }
}
