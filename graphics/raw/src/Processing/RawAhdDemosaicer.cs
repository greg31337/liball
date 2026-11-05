namespace raw;

internal class RawHomogeneityScore
{
    internal int Count;
    internal double Cost;

    internal RawHomogeneityScore()
    {
        Count = 0;
        Cost = 0.0;
    }

    internal void Reset()
    {
        Count = 0;
        Cost = 0.0;
    }
}

internal static class RawAhdDemosaicer
{
    internal static RawRgbImage Reconstruct(RawSensorImage image, RawRenderOptions options)
    {
        RawArea selected = RawDefectProcessor.SelectedArea(image);
        long fullPixels = CheckedMath.Multiply(image.Info.Width, image.Info.Height);
        long fullRgbSamples = CheckedMath.Multiply(fullPixels, 3);
        long selectedPixels = CheckedMath.PixelCount(selected.Width, selected.Height, options.Limits);
        long selectedRgbSamples = CheckedMath.Multiply(selectedPixels, 3);
        long fullWorkspace = CheckedMath.Multiply(fullPixels, 8);
        long selectedWorkspace = CheckedMath.Multiply(selectedRgbSamples, 2);
        long combined = CheckedMath.Add(fullWorkspace, selectedWorkspace);
        CheckedMath.DecodedSize(combined, 4, options.Limits);
        CheckedMath.AllocationSize(fullPixels, 4, options.Limits);
        CheckedMath.AllocationSize(fullRgbSamples, 4, options.Limits);
        CheckedMath.AllocationSize(selectedRgbSamples, 4, options.Limits);
        int fullCount = CheckedMath.ToInt32(fullPixels);
        int fullRgbCount = CheckedMath.ToInt32(fullRgbSamples);
        int selectedRgbCount = CheckedMath.ToInt32(selectedRgbSamples);
        float[] horizontalGreen = new float[fullCount];
        float[] verticalGreen = new float[fullCount];
        BuildGreenCandidates(image, options, horizontalGreen, verticalGreen);
        float[] horizontal = new float[fullRgbCount];
        float[] vertical = new float[fullRgbCount];
        BuildColorCandidate(image, options, horizontalGreen, horizontal);
        BuildColorCandidate(image, options, verticalGreen, vertical);
        float[] selectedValues = new float[selectedRgbCount];
        SelectHomogeneous(horizontal, vertical, selectedValues, image, selected, options);
        float[] suppressed = new float[selectedRgbCount];
        SuppressArtifacts(selectedValues, suppressed, image, selected, options);
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(selected.Width, selected.Height, 3, selected.Width * 3, null, null, suppressed, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, image.Info.Orientation, false, true, image.Diagnostics);
    }

    internal static void BuildGreenCandidates(RawSensorImage image, RawRenderOptions options, float[] horizontal, float[] vertical)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        RawDirectionalValue horizontalValue = new RawDirectionalValue();
        RawDirectionalValue verticalValue = new RawDirectionalValue();
        int y = visible.Y;
        while (y < visible.Y + visible.Height)
        {
            options.CheckCancellation();
            int x = visible.X;
            while (x < visible.X + visible.Width)
            {
                int index = y * image.Info.Width + x;
                RawCfaColor color = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                if ((color == RawCfaColor.Green || color == RawCfaColor.Green2) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    float value = (float)RawFastProcessor.ReadNormalized(image, x, y, 0);
                    horizontal[index] = value;
                    vertical[index] = value;
                }
                else
                {
                    HamiltonGreen(image, x, y, 1, 0, horizontalValue);
                    HamiltonGreen(image, x, y, 0, 1, verticalValue);
                    if (!horizontalValue.Valid && !verticalValue.Valid)
                    {
                        double fallback = RawVngDemosaicer.InterpolateGreen(image, x, y, options, horizontalValue, verticalValue);
                        horizontal[index] = (float)fallback;
                        vertical[index] = (float)fallback;
                    }
                    else if (!horizontalValue.Valid)
                    {
                        horizontal[index] = (float)verticalValue.Value;
                        vertical[index] = (float)verticalValue.Value;
                    }
                    else if (!verticalValue.Valid)
                    {
                        horizontal[index] = (float)horizontalValue.Value;
                        vertical[index] = (float)horizontalValue.Value;
                    }
                    else
                    {
                        horizontal[index] = (float)horizontalValue.Value;
                        vertical[index] = (float)verticalValue.Value;
                    }
                }
                x++;
            }
            y++;
        }
    }

    internal static void HamiltonGreen(RawSensorImage image, int x, int y, int stepX, int stepY, RawDirectionalValue result)
    {
        result.Reset();
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int firstX = x - stepX;
        int firstY = y - stepY;
        int secondX = x + stepX;
        int secondY = y + stepY;
        bool first = RawVngDemosaicer.ValidColor(image, visible, firstX, firstY, RawCfaColor.Green);
        bool second = RawVngDemosaicer.ValidColor(image, visible, secondX, secondY, RawCfaColor.Green);
        if (!first && !second)
        {
            return;
        }
        double firstGreen = first ? RawFastProcessor.ReadNormalized(image, firstX, firstY, 0) : 0.0;
        double secondGreen = second ? RawFastProcessor.ReadNormalized(image, secondX, secondY, 0) : 0.0;
        double value = first && second ? (firstGreen + secondGreen) * 0.5 : first ? firstGreen : secondGreen;
        double center = RawFastProcessor.ReadNormalized(image, x, y, 0);
        RawCfaColor centerColor = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
        int farFirstX = x - stepX * 2;
        int farFirstY = y - stepY * 2;
        int farSecondX = x + stepX * 2;
        int farSecondY = y + stepY * 2;
        bool farFirst = RawVngDemosaicer.ValidExactColor(image, visible, farFirstX, farFirstY, centerColor);
        bool farSecond = RawVngDemosaicer.ValidExactColor(image, visible, farSecondX, farSecondY, centerColor);
        if (first && second && farFirst && farSecond)
        {
            double sameFirst = RawFastProcessor.ReadNormalized(image, farFirstX, farFirstY, 0);
            double sameSecond = RawFastProcessor.ReadNormalized(image, farSecondX, farSecondY, 0);
            value += (center * 2.0 - sameFirst - sameSecond) * 0.25;
        }
        result.Valid = true;
        result.Value = RawVngDemosaicer.Clamp(value);
        result.Gradient = first && second ? System.Math.Abs(firstGreen - secondGreen) : System.Math.Abs(center - value) * 2.0;
    }

    internal static void BuildColorCandidate(RawSensorImage image, RawRenderOptions options, float[] green, float[] candidate)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int y = visible.Y;
        while (y < visible.Y + visible.Height)
        {
            options.CheckCancellation();
            int x = visible.X;
            while (x < visible.X + visible.Width)
            {
                int pixel = y * image.Info.Width + x;
                int output = pixel * 3;
                RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                candidate[output + 1] = green[pixel];
                candidate[output] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Red) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y) ? (float)RawFastProcessor.ReadNormalized(image, x, y, 0) : (float)CandidateColor(image, green, x, y, RawCfaColor.Red, options);
                candidate[output + 2] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Blue) && !RawDefectProcessor.Masked(image.MaskedAreas, x, y) ? (float)RawFastProcessor.ReadNormalized(image, x, y, 0) : (float)CandidateColor(image, green, x, y, RawCfaColor.Blue, options);
                x++;
            }
            y++;
        }
    }

    internal static double CandidateColor(RawSensorImage image, float[] green, int x, int y, RawCfaColor wanted, RawRenderOptions options)
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
                if ((neighborX != x || neighborY != y) && RawVngDemosaicer.ValidColor(image, visible, neighborX, neighborY, wanted))
                {
                    int neighbor = neighborY * image.Info.Width + neighborX;
                    sum += RawFastProcessor.ReadNormalized(image, neighborX, neighborY, 0) - green[neighbor];
                    count++;
                }
                neighborX++;
            }
            neighborY++;
        }
        if (count != 0)
        {
            return RawVngDemosaicer.Clamp(green[y * image.Info.Width + x] + sum / count);
        }
        double adjacent = RawDemosaicer.AdjacentAverage(image, x, y, wanted);
        if (adjacent >= 0.0)
        {
            return adjacent;
        }
        return RawDemosaicer.NearestValue(image, x, y, wanted, options);
    }

    internal static void SelectHomogeneous(float[] horizontal, float[] vertical, float[] selectedValues, RawSensorImage image, RawArea selected, RawRenderOptions options)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        RawHomogeneityScore horizontalScore = new RawHomogeneityScore();
        RawHomogeneityScore verticalScore = new RawHomogeneityScore();
        int outputY = 0;
        while (outputY < selected.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < selected.Width)
            {
                int x = selected.X + outputX;
                int y = selected.Y + outputY;
                Score(horizontal, image.Info.Width, visible, x, y, 1, 0, horizontalScore);
                Score(vertical, image.Info.Width, visible, x, y, 0, 1, verticalScore);
                int horizontalIndex = (y * image.Info.Width + x) * 3;
                int destination = (outputY * selected.Width + outputX) * 3;
                bool useHorizontal = horizontalScore.Count > verticalScore.Count || horizontalScore.Count == verticalScore.Count && horizontalScore.Cost < verticalScore.Cost;
                bool useVertical = verticalScore.Count > horizontalScore.Count || horizontalScore.Count == verticalScore.Count && verticalScore.Cost < horizontalScore.Cost;
                int channel = 0;
                while (channel < 3)
                {
                    if (useHorizontal)
                    {
                        selectedValues[destination + channel] = horizontal[horizontalIndex + channel];
                    }
                    else if (useVertical)
                    {
                        selectedValues[destination + channel] = vertical[horizontalIndex + channel];
                    }
                    else
                    {
                        selectedValues[destination + channel] = (horizontal[horizontalIndex + channel] + vertical[horizontalIndex + channel]) * 0.5F;
                    }
                    channel++;
                }
                outputX++;
            }
            outputY++;
        }
    }

    internal static void Score(float[] candidate, int width, RawArea visible, int x, int y, int stepX, int stepY, RawHomogeneityScore score)
    {
        score.Reset();
        int center = (y * width + x) * 3;
        int distance = 1;
        while (distance <= 2)
        {
            AddScore(candidate, width, visible, center, x - stepX * distance, y - stepY * distance, score);
            AddScore(candidate, width, visible, center, x + stepX * distance, y + stepY * distance, score);
            distance++;
        }
    }

    internal static void AddScore(float[] candidate, int width, RawArea visible, int center, int x, int y, RawHomogeneityScore score)
    {
        if (!visible.Contains(x, y))
        {
            return;
        }
        int neighbor = (y * width + x) * 3;
        double difference = ColorDifference(candidate, center, neighbor);
        if (difference <= 0.12)
        {
            score.Count++;
        }
        score.Cost += difference;
    }

    internal static double ColorDifference(float[] candidate, int first, int second)
    {
        double firstLuminance = candidate[first] * 0.25 + candidate[first + 1] * 0.5 + candidate[first + 2] * 0.25;
        double secondLuminance = candidate[second] * 0.25 + candidate[second + 1] * 0.5 + candidate[second + 2] * 0.25;
        double firstRedDifference = candidate[first] - candidate[first + 1];
        double secondRedDifference = candidate[second] - candidate[second + 1];
        double firstBlueDifference = candidate[first + 2] - candidate[first + 1];
        double secondBlueDifference = candidate[second + 2] - candidate[second + 1];
        return System.Math.Abs(firstLuminance - secondLuminance) * 2.0 + System.Math.Abs(firstRedDifference - secondRedDifference) + System.Math.Abs(firstBlueDifference - secondBlueDifference);
    }

    internal static void SuppressArtifacts(float[] source, float[] destination, RawSensorImage image, RawArea selected, RawRenderOptions options)
    {
        double[] differences = new double[9];
        int y = 0;
        while (y < selected.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < selected.Width)
            {
                int index = (y * selected.Width + x) * 3;
                RawCfaColor actual = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(selected.X + x, selected.Y + y);
                double green = source[index + 1];
                destination[index + 1] = source[index + 1];
                destination[index] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Red) ? source[index] : RawVngDemosaicer.SuppressedChannel(source, selected.Width, selected.Height, x, y, 0, green, differences, 0.025);
                destination[index + 2] = RawDemosaicer.MatchingColor(actual, RawCfaColor.Blue) ? source[index + 2] : RawVngDemosaicer.SuppressedChannel(source, selected.Width, selected.Height, x, y, 2, green, differences, 0.025);
                x++;
            }
            y++;
        }
    }
}
