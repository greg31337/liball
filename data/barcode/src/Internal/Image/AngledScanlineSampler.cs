namespace barcode;

internal class AngledScanlineSample
{
    internal BitRow Row = new BitRow(1);
    internal BarcodePoint Start = new BarcodePoint();
    internal BarcodePoint End = new BarcodePoint();
    internal double AngleDegrees;
    internal double Offset;
    internal int FanIndex;
    internal bool ReversedTraversal;
}

internal class AngledScanlineObservation
{
    internal LinearScanlineObservation Decoded = new LinearScanlineObservation();
    internal AngledScanlineSample Sample = new AngledScanlineSample();
}

internal class AngledScanlineConsensus
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal int VoteCount;
    internal int ObservationCount;
    internal double AverageStartPosition;
    internal double AverageEndPosition;
    internal double AverageModuleSize;
    internal double AverageVariance;
    internal double OrientationDegrees;
    internal double SkewDegrees;
    internal double MinimumOffset;
    internal double MaximumOffset;
}

internal static class AngledScanlineSampler
{
    internal static AngledScanlineSample Sample(
        ModuleMatrix matrix,
        LineSegment requestedLine,
        bool reverseTraversal)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Angled scanline source must not be null.");
        }

        if (requestedLine == null)
        {
            throw new BarcodeValidationException("Angled scanline must not be null.");
        }

        BarcodeRectangle bounds = GeometryMath.CreateRectangle(
            0.0,
            0.0,
            matrix.Width - 1.0,
            matrix.Height - 1.0);
        LineSegment? clipped = GeometryMath.ClipLineToRectangle(requestedLine, bounds);
        if (clipped == null || clipped.CalculateLength() < 1.0)
        {
            throw new BarcodeValidationException("Angled scanline does not cross enough of the source matrix.");
        }

        BarcodePoint start = reverseTraversal
            ? GeometryMath.CopyPointValue(clipped.End)
            : GeometryMath.CopyPointValue(clipped.Start);
        BarcodePoint end = reverseTraversal
            ? GeometryMath.CopyPointValue(clipped.Start)
            : GeometryMath.CopyPointValue(clipped.End);
        double length = GeometryMath.DistanceBetweenPoints(start, end);
        int sampleCount = (int)global::System.Math.Floor(length) + 1;
        if (sampleCount < 2)
        {
            throw new BarcodeValidationException("Angled scanline sample count is too small.");
        }

        BitRow row = new BitRow(sampleCount);
        int index = 0;
        while (index < sampleCount)
        {
            double fraction = sampleCount == 1 ? 0.0 : (double)index / (sampleCount - 1);
            double x = start.X + ((end.X - start.X) * fraction);
            double y = start.Y + ((end.Y - start.Y) * fraction);
            int sourceX = (int)global::System.Math.Floor(x + 0.5);
            int sourceY = (int)global::System.Math.Floor(y + 0.5);
            if (sourceX < 0)
            {
                sourceX = 0;
            }
            else if (sourceX >= matrix.Width)
            {
                sourceX = matrix.Width - 1;
            }

            if (sourceY < 0)
            {
                sourceY = 0;
            }
            else if (sourceY >= matrix.Height)
            {
                sourceY = matrix.Height - 1;
            }

            if (matrix.GetModule(sourceX, sourceY))
            {
                row.SetBit(index, true);
            }

            index = index + 1;
        }

        AngledScanlineSample sample = new AngledScanlineSample();
        sample.Row = row;
        sample.Start = start;
        sample.End = end;
        sample.AngleDegrees = CalculateAngleDegrees(start, end);
        sample.ReversedTraversal = reverseTraversal;
        return sample;
    }

    static double CalculateAngleDegrees(BarcodePoint start, BarcodePoint end)
    {
        double radians = global::System.Math.Atan2(end.Y - start.Y, end.X - start.X);
        double degrees = radians * 180.0 / global::System.Math.PI;
        return NormalizeDegrees(degrees);
    }

    static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;
        if (normalized < 0.0)
        {
            normalized = normalized + 360.0;
        }

        return normalized;
    }
}

internal static class DeskewedScanlineFan
{
    internal static AngledScanlineSample[] Create(
        ModuleMatrix matrix,
        BarcodeQuadrilateral region,
        double lineSpacing,
        int maximumLineCount,
        bool reverseTraversal)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Deskewed scanline source must not be null.");
        }

        GeometryMath.ValidateQuadrilateral(region);
        if (lineSpacing <= 0.0 || double.IsNaN(lineSpacing) || double.IsInfinity(lineSpacing))
        {
            throw new BarcodeValidationException("Deskewed scanline spacing must be finite and positive.");
        }

        if (maximumLineCount <= 0 || maximumLineCount > ScanlineSampler.MaximumFanLineCount)
        {
            throw new BarcodeValidationException("Deskewed scanline count is outside the supported range.");
        }

        BarcodePoint topLeft = GeometryMath.RequirePoint(region.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(region.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(region.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(region.BottomLeft);
        double leftHeight = GeometryMath.DistanceBetweenPoints(topLeft, bottomLeft);
        double rightHeight = GeometryMath.DistanceBetweenPoints(topRight, bottomRight);
        double averageHeight = (leftHeight + rightHeight) * 0.5;
        if (averageHeight < 1.0)
        {
            throw new BarcodeValidationException("Deskewed scanline region has insufficient height.");
        }

        double fractionStep = lineSpacing / averageHeight;
        global::System.Collections.Generic.List<AngledScanlineSample> samples =
            new global::System.Collections.Generic.List<AngledScanlineSample>();
        samples.Add(CreateAtFraction(
            matrix,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft,
            0.5,
            0,
            averageHeight,
            reverseTraversal));

        int distanceIndex = 1;
        while (samples.Count < maximumLineCount)
        {
            double distance = fractionStep * distanceIndex;
            double lowerFraction = 0.5 - distance;
            double upperFraction = 0.5 + distance;
            bool added = false;
            if (lowerFraction >= 0.0)
            {
                samples.Add(CreateAtFraction(
                    matrix,
                    topLeft,
                    topRight,
                    bottomRight,
                    bottomLeft,
                    lowerFraction,
                    samples.Count,
                    averageHeight,
                    reverseTraversal));
                added = true;
            }

            if (samples.Count < maximumLineCount && upperFraction <= 1.0)
            {
                samples.Add(CreateAtFraction(
                    matrix,
                    topLeft,
                    topRight,
                    bottomRight,
                    bottomLeft,
                    upperFraction,
                    samples.Count,
                    averageHeight,
                    reverseTraversal));
                added = true;
            }

            if (!added)
            {
                break;
            }

            distanceIndex = distanceIndex + 1;
        }

        return samples.ToArray();
    }

    static AngledScanlineSample CreateAtFraction(
        ModuleMatrix matrix,
        BarcodePoint topLeft,
        BarcodePoint topRight,
        BarcodePoint bottomRight,
        BarcodePoint bottomLeft,
        double fraction,
        int fanIndex,
        double averageHeight,
        bool reverseTraversal)
    {
        BarcodePoint start = GeometryMath.InterpolatePoint(topLeft, bottomLeft, fraction);
        BarcodePoint end = GeometryMath.InterpolatePoint(topRight, bottomRight, fraction);
        AngledScanlineSample sample = AngledScanlineSampler.Sample(
            matrix,
            new LineSegment(start, end),
            reverseTraversal);
        sample.Offset = (fraction - 0.5) * averageHeight;
        sample.FanIndex = fanIndex;
        return sample;
    }
}

internal static class AngledScanlineReconciler
{
    internal static AngledScanlineConsensus? Reconcile(
        AngledScanlineObservation[] observations,
        int minimumVotes)
    {
        if (observations == null)
        {
            throw new BarcodeValidationException("Angled scanline observations must not be null.");
        }

        LinearScanlineObservation[] decoded = new LinearScanlineObservation[observations.Length];
        int index = 0;
        while (index < observations.Length)
        {
            AngledScanlineObservation observation = observations[index];
            ValidateObservation(observation);
            decoded[index] = observation.Decoded;
            index = index + 1;
        }

        LinearScanlineConsensus? baseConsensus = LinearScanlineVoter.Reconcile(decoded, minimumVotes);
        if (baseConsensus == null)
        {
            return null;
        }

        global::System.Collections.Generic.List<AngledScanlineObservation> winners =
            new global::System.Collections.Generic.List<AngledScanlineObservation>();
        index = 0;
        while (index < observations.Length)
        {
            if (ResultsMatch(observations[index].Decoded.Result, baseConsensus.Result))
            {
                winners.Add(observations[index]);
            }

            index = index + 1;
        }

        if (winners.Count < minimumVotes)
        {
            return null;
        }

        AngledScanlineConsensus consensus = new AngledScanlineConsensus();
        consensus.Result = ResultCopier.CopyDecodeResult(baseConsensus.Result);
        consensus.VoteCount = winners.Count;
        consensus.ObservationCount = observations.Length;
        consensus.AverageStartPosition = baseConsensus.AverageStartPosition;
        consensus.AverageEndPosition = baseConsensus.AverageEndPosition;
        consensus.AverageModuleSize = baseConsensus.AverageModuleSize;
        consensus.AverageVariance = baseConsensus.AverageVariance;
        SetOrientation(consensus, winners);
        consensus.Result.Location = CreateLocation(consensus, winners);
        return consensus;
    }

    static void ValidateObservation(AngledScanlineObservation observation)
    {
        if (observation == null || observation.Decoded == null || observation.Sample == null ||
            observation.Sample.Row == null || observation.Sample.Start == null || observation.Sample.End == null)
        {
            throw new BarcodeValidationException("Angled scanline observation is incomplete.");
        }

        GeometryMath.ValidatePoint(observation.Sample.Start);
        GeometryMath.ValidatePoint(observation.Sample.End);
        if (observation.Sample.Row.Length < 2 ||
            observation.Decoded.StartPosition < 0 ||
            observation.Decoded.EndPosition <= observation.Decoded.StartPosition ||
            observation.Decoded.EndPosition > observation.Sample.Row.Length)
        {
            throw new BarcodeValidationException("Angled scanline observation geometry is invalid.");
        }
    }

    static bool ResultsMatch(BarcodeDecodeResult first, BarcodeDecodeResult second)
    {
        if (first.Format != second.Format || first.Payload == null || second.Payload == null ||
            first.Payload.Text != second.Payload.Text)
        {
            return false;
        }

        return BytesMatch(first.Payload.RawBytes, second.Payload.RawBytes);
    }

    static bool BytesMatch(byte[]? first, byte[]? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void SetOrientation(
        AngledScanlineConsensus consensus,
        global::System.Collections.Generic.List<AngledScanlineObservation> winners)
    {
        double sumX = 0.0;
        double sumY = 0.0;
        double minimumOffset = double.MaxValue;
        double maximumOffset = double.MinValue;
        int index = 0;
        while (index < winners.Count)
        {
            AngledScanlineObservation winner = winners[index];
            double angle = winner.Sample.AngleDegrees;
            if (winner.Decoded.IsReversed)
            {
                angle = angle + 180.0;
            }

            double radians = angle * global::System.Math.PI / 180.0;
            sumX = sumX + global::System.Math.Cos(radians);
            sumY = sumY + global::System.Math.Sin(radians);
            minimumOffset = global::System.Math.Min(minimumOffset, winner.Sample.Offset);
            maximumOffset = global::System.Math.Max(maximumOffset, winner.Sample.Offset);
            index = index + 1;
        }

        double orientation = NormalizeDegrees(
            global::System.Math.Atan2(sumY, sumX) * 180.0 / global::System.Math.PI);
        int quarterTurn = (int)global::System.Math.Floor((orientation + 45.0) / 90.0) % 4;
        double rightAngle = quarterTurn * 90.0;
        double skew = orientation - rightAngle;
        if (skew > 180.0)
        {
            skew = skew - 360.0;
        }
        else if (skew < -180.0)
        {
            skew = skew + 360.0;
        }

        consensus.OrientationDegrees = orientation;
        consensus.SkewDegrees = skew;
        consensus.MinimumOffset = minimumOffset;
        consensus.MaximumOffset = maximumOffset;
        consensus.Result.Rotation = GetRotation(quarterTurn);
        consensus.Result.OrientationDegrees = orientation;
        consensus.Result.SkewDegrees = skew;
    }

    static BarcodeRotation GetRotation(int quarterTurn)
    {
        if (quarterTurn == 1)
        {
            return BarcodeRotation.Clockwise90;
        }

        if (quarterTurn == 2)
        {
            return BarcodeRotation.Clockwise180;
        }

        if (quarterTurn == 3)
        {
            return BarcodeRotation.Clockwise270;
        }

        return BarcodeRotation.None;
    }

    static BarcodeQuadrilateral CreateLocation(
        AngledScanlineConsensus consensus,
        global::System.Collections.Generic.List<AngledScanlineObservation> winners)
    {
        AngledScanlineObservation minimum = winners[0];
        AngledScanlineObservation maximum = winners[0];
        int index = 1;
        while (index < winners.Count)
        {
            if (winners[index].Sample.Offset < minimum.Sample.Offset)
            {
                minimum = winners[index];
            }

            if (winners[index].Sample.Offset > maximum.Sample.Offset)
            {
                maximum = winners[index];
            }

            index = index + 1;
        }

        BarcodePoint[] minimumEnds = CalculateDecodedEnds(minimum);
        BarcodePoint[] maximumEnds = CalculateDecodedEnds(maximum);
        if (minimum == maximum)
        {
            double radians = consensus.OrientationDegrees * global::System.Math.PI / 180.0;
            double offsetX = global::System.Math.Sin(radians) * 0.5;
            double offsetY = -global::System.Math.Cos(radians) * 0.5;
            return GeometryMath.CreateQuadrilateral(
                GeometryMath.CreatePoint(minimumEnds[0].X + offsetX, minimumEnds[0].Y + offsetY),
                GeometryMath.CreatePoint(minimumEnds[1].X + offsetX, minimumEnds[1].Y + offsetY),
                GeometryMath.CreatePoint(minimumEnds[1].X - offsetX, minimumEnds[1].Y - offsetY),
                GeometryMath.CreatePoint(minimumEnds[0].X - offsetX, minimumEnds[0].Y - offsetY));
        }

        return GeometryMath.CreateQuadrilateral(
            minimumEnds[0],
            minimumEnds[1],
            maximumEnds[1],
            maximumEnds[0]);
    }

    static BarcodePoint[] CalculateDecodedEnds(AngledScanlineObservation observation)
    {
        double denominator = observation.Sample.Row.Length - 1.0;
        double startFraction = observation.Decoded.StartPosition / denominator;
        double endFraction = observation.Decoded.EndPosition / denominator;
        if (endFraction > 1.0)
        {
            endFraction = 1.0;
        }

        BarcodePoint start = GeometryMath.InterpolatePoint(
            observation.Sample.Start,
            observation.Sample.End,
            startFraction);
        BarcodePoint end = GeometryMath.InterpolatePoint(
            observation.Sample.Start,
            observation.Sample.End,
            endFraction);
        if (observation.Sample.ReversedTraversal)
        {
            return new BarcodePoint[] { end, start };
        }

        return new BarcodePoint[] { start, end };
    }

    static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;
        if (normalized < 0.0)
        {
            normalized = normalized + 360.0;
        }

        return normalized;
    }
}

internal static class AngledLinearScanner
{
    internal static AngledScanlineConsensus? Scan(
        ModuleMatrix matrix,
        BarcodeFormat format,
        BarcodeQuadrilateral region,
        double lineSpacing,
        int maximumLineCount,
        int minimumVotes,
        bool reverseTraversal)
    {
        AngledScanlineSample[] samples = DeskewedScanlineFan.Create(
            matrix,
            region,
            lineSpacing,
            maximumLineCount,
            reverseTraversal);
        global::System.Collections.Generic.List<AngledScanlineObservation> observations =
            new global::System.Collections.Generic.List<AngledScanlineObservation>();
        int index = 0;
        while (index < samples.Length)
        {
            AngledScanlineSample sample = samples[index];
            LinearScanlineObservation? decoded = LinearScanlineDecoder.TryDecode(
                sample.Row,
                format,
                false,
                sample.FanIndex);
            if (decoded != null)
            {
                AngledScanlineObservation observation = new AngledScanlineObservation();
                observation.Decoded = decoded;
                observation.Sample = sample;
                observations.Add(observation);
            }

            index = index + 1;
        }

        return AngledScanlineReconciler.Reconcile(observations.ToArray(), minimumVotes);
    }
}
