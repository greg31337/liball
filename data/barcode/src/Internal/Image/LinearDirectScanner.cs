namespace barcode;

internal class LinearScanlineObservation
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double AverageVariance;
    internal bool IsReversed;
    internal bool IsVertical;
    internal int Coordinate;
}

internal class LinearScanlineConsensus
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal int VoteCount;
    internal int ObservationCount;
    internal int FirstObservationIndex;
    internal double AverageStartPosition;
    internal double AverageEndPosition;
    internal double AverageModuleSize;
    internal double AverageVariance;
    internal int MinimumCoordinate;
    internal int MaximumCoordinate;
    internal bool IsVertical;
}

internal class LinearScanlineVoteGroup
{
    internal LinearScanlineObservation Representative = new LinearScanlineObservation();
    internal int FirstObservationIndex;
    internal int VoteCount;
    internal double TotalStartPosition;
    internal double TotalEndPosition;
    internal double TotalModuleSize;
    internal double TotalVariance;
    internal int MinimumCoordinate;
    internal int MaximumCoordinate;
}

internal static class LinearScanlineDecoder
{
    internal static LinearScanlineObservation? TryDecode(
        BitRow row,
        BarcodeFormat format,
        bool vertical,
        int coordinate)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Linear scanline must not be null.");
        }

        ValidateFormat(format);
        if (format == BarcodeFormat.Automatic || format == BarcodeFormat.Gs1128)
        {
            Gs1128DecodedData? gs1 = Gs1128Codec.TryDecodeScanline(row);
            if (gs1 != null)
            {
                return CreateObservation(
                    gs1.Result,
                    gs1.StartPosition,
                    gs1.EndPosition,
                    0.0,
                    ReadConfidenceVariance(gs1.Result, Code128ScanlineDecoder.MaximumAverageVariance),
                    gs1.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format == BarcodeFormat.Gs1128)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic || IsEanUpcFormat(format))
        {
            EanUpcDecodedData? eanUpc = EanUpcScanlineDecoder.TryDecodeRow(row);
            if (eanUpc != null && (format == BarcodeFormat.Automatic || eanUpc.Format == format))
            {
                return CreateObservation(
                    eanUpc.Result,
                    eanUpc.StartPosition,
                    eanUpc.EndPosition,
                    eanUpc.ModuleSize,
                    eanUpc.AverageVariance,
                    eanUpc.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format != BarcodeFormat.Automatic)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic
            || IsEanUpcAddOnFormat(format))
        {
            EanUpcAddonDecodedData? addOn =
                EanUpcAddonScanlineDecoder.TryDecodeRow(row, null);
            if (addOn != null
                && (format == BarcodeFormat.Automatic
                    || addOn.Format == format))
            {
                return CreateObservation(
                    addOn.Result,
                    addOn.StartPosition,
                    addOn.EndPosition,
                    addOn.ModuleSize,
                    addOn.AverageVariance,
                    addOn.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format != BarcodeFormat.Automatic)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic || format == BarcodeFormat.Code128)
        {
            Code128DecodedData? code128 = Code128ScanlineDecoder.TryDecodeRow(row);
            if (code128 != null)
            {
                BarcodeDecodeResult result = CreateCode128Result(code128);
                return CreateObservation(
                    result,
                    code128.StartPosition,
                    code128.EndPosition,
                    code128.ModuleSize,
                    code128.AverageVariance,
                    code128.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format == BarcodeFormat.Code128)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic || format == BarcodeFormat.Code39)
        {
            Code39ScanlineDecodedData? code39 = Code39ScanlineDecoder.TryDecodeRow(row, null);
            if (code39 != null)
            {
                return CreateObservation(
                    code39.Result,
                    code39.StartPosition,
                    code39.EndPosition,
                    code39.NarrowWidth,
                    code39.AverageVariance,
                    code39.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format == BarcodeFormat.Code39)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic || format == BarcodeFormat.Itf14)
        {
            ItfScanlineDecodedData? itf = ItfScanlineDecoder.TryDecodeRow(
                row,
                BarcodeFormat.Itf14,
                null);
            if (itf != null)
            {
                return CreateObservation(
                    itf.Result,
                    itf.StartPosition,
                    itf.EndPosition,
                    itf.NarrowWidth,
                    itf.AverageVariance,
                    itf.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format == BarcodeFormat.Itf14)
            {
                return null;
            }
        }

        if (format == BarcodeFormat.Automatic || format == BarcodeFormat.Itf)
        {
            ItfScanlineDecodedData? itf = ItfScanlineDecoder.TryDecodeRow(
                row,
                BarcodeFormat.Itf,
                null);
            if (itf != null)
            {
                return CreateObservation(
                    itf.Result,
                    itf.StartPosition,
                    itf.EndPosition,
                    itf.NarrowWidth,
                    itf.AverageVariance,
                    itf.IsReversed,
                    vertical,
                    coordinate);
            }

            if (format == BarcodeFormat.Itf)
            {
                return null;
            }
        }

        CodabarScanlineDecodedData? codabar = CodabarScanlineDecoder.TryDecodeRow(row, null);
        if (codabar == null)
        {
            return null;
        }

        return CreateObservation(
            codabar.Result,
            codabar.StartPosition,
            codabar.EndPosition,
            codabar.NarrowWidth,
            codabar.AverageVariance,
            codabar.IsReversed,
            vertical,
            coordinate);
    }

    static bool IsEanUpcFormat(BarcodeFormat format)
    {
        return format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE;
    }

    static bool IsEanUpcAddOnFormat(BarcodeFormat format)
    {
        return format == BarcodeFormat.EanUpcAddon2
            || format == BarcodeFormat.EanUpcAddon5;
    }

    static void ValidateFormat(BarcodeFormat format)
    {
        if (format != BarcodeFormat.Automatic && format != BarcodeFormat.Code128 &&
            format != BarcodeFormat.Gs1128 && !IsEanUpcFormat(format) &&
            !IsEanUpcAddOnFormat(format) &&
            format != BarcodeFormat.Code39 && format != BarcodeFormat.Itf &&
            format != BarcodeFormat.Itf14 && format != BarcodeFormat.Codabar)
        {
            throw new BarcodeValidationException("Direct linear scan format is not supported.");
        }
    }

    static LinearScanlineObservation CreateObservation(
        BarcodeDecodeResult result,
        int startPosition,
        int endPosition,
        double moduleSize,
        double averageVariance,
        bool reversed,
        bool vertical,
        int coordinate)
    {
        LinearScanlineObservation observation = new LinearScanlineObservation();
        observation.Result = ResultCopier.CopyDecodeResult(result);
        observation.StartPosition = startPosition;
        observation.EndPosition = endPosition;
        observation.ModuleSize = moduleSize;
        observation.AverageVariance = averageVariance;
        observation.IsReversed = reversed;
        observation.IsVertical = vertical;
        observation.Coordinate = coordinate;
        ApplyOrientation(observation.Result, vertical, reversed);
        return observation;
    }

    static BarcodeDecodeResult CreateCode128Result(Code128DecodedData decoded)
    {
        byte[]? copiedBytes = ResultCopier.CopyByteArray(decoded.Bytes);
        byte[] bytes = copiedBytes == null ? new byte[0] : copiedBytes;
        char[] characters = new char[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            characters[index] = (char)bytes[index];
            index = index + 1;
        }

        BarcodePayload payload = new BarcodePayload();
        payload.Text = new string(characters);
        payload.RawBytes = bytes;
        payload.CharacterEncoding = "ISO-8859-1";

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = true;
        metadata.ChecksumValid = true;
        metadata.ChecksumValue = decoded.ChecksumValue;
        metadata.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Code128;
        metadata.Codewords = ResultCopier.CopyIntArray(decoded.Codewords);
        metadata.Confidence = CalculateConfidence(
            decoded.AverageVariance,
            Code128ScanlineDecoder.MaximumAverageVariance);

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = BarcodeFormat.Code128;
        result.Dimension = BarcodeDimension.Linear;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Normal;
        result.Metadata = metadata;
        result.Diagnostics = new BarcodeDiagnostic[0];
        return result;
    }

    static double ReadConfidenceVariance(BarcodeDecodeResult result, double maximumVariance)
    {
        if (result.Metadata == null)
        {
            return maximumVariance;
        }

        return maximumVariance * (1.0 - result.Metadata.Confidence);
    }

    static double CalculateConfidence(double variance, double maximumVariance)
    {
        double confidence = 1.0 - (variance / maximumVariance);
        if (confidence < 0.0)
        {
            return 0.0;
        }

        if (confidence > 1.0)
        {
            return 1.0;
        }

        return confidence;
    }

    static void ApplyOrientation(BarcodeDecodeResult result, bool vertical, bool reversed)
    {
        if (vertical)
        {
            result.Rotation = reversed ? BarcodeRotation.Clockwise270 : BarcodeRotation.Clockwise90;
            result.OrientationDegrees = reversed ? 270.0 : 90.0;
        }
        else
        {
            result.Rotation = reversed ? BarcodeRotation.Clockwise180 : BarcodeRotation.None;
            result.OrientationDegrees = reversed ? 180.0 : 0.0;
        }
    }
}

internal static class LinearScanlineVoter
{
    internal static LinearScanlineConsensus? Reconcile(
        LinearScanlineObservation[] observations,
        int minimumVotes)
    {
        if (observations == null)
        {
            throw new BarcodeValidationException("Linear scanline observations must not be null.");
        }

        if (minimumVotes <= 0)
        {
            throw new BarcodeValidationException("Linear scanline minimum vote count must be positive.");
        }

        global::System.Collections.Generic.List<LinearScanlineVoteGroup> groups =
            new global::System.Collections.Generic.List<LinearScanlineVoteGroup>();
        int observationIndex = 0;
        while (observationIndex < observations.Length)
        {
            LinearScanlineObservation observation = observations[observationIndex];
            ValidateObservation(observation);
            LinearScanlineVoteGroup? group = FindGroup(groups, observation);
            if (group == null)
            {
                group = CreateGroup(observation, observationIndex);
                groups.Add(group);
            }
            else
            {
                AddVote(group, observation);
            }

            observationIndex = observationIndex + 1;
        }

        LinearScanlineVoteGroup? winner = SelectWinner(groups);
        if (winner == null || winner.VoteCount < minimumVotes)
        {
            return null;
        }

        return CreateConsensus(winner, observations.Length);
    }

    static void ValidateObservation(LinearScanlineObservation observation)
    {
        if (observation == null || observation.Result == null || observation.Result.Payload == null)
        {
            throw new BarcodeValidationException("Linear scanline observation is incomplete.");
        }

        if (observation.StartPosition < 0 || observation.EndPosition <= observation.StartPosition ||
            observation.Coordinate < 0 || observation.ModuleSize < 0.0 ||
            double.IsNaN(observation.ModuleSize) || double.IsInfinity(observation.ModuleSize) ||
            observation.AverageVariance < 0.0 || double.IsNaN(observation.AverageVariance) ||
            double.IsInfinity(observation.AverageVariance))
        {
            throw new BarcodeValidationException("Linear scanline observation geometry is invalid.");
        }
    }

    static LinearScanlineVoteGroup? FindGroup(
        global::System.Collections.Generic.List<LinearScanlineVoteGroup> groups,
        LinearScanlineObservation observation)
    {
        int index = 0;
        while (index < groups.Count)
        {
            LinearScanlineVoteGroup candidate = groups[index];
            if (candidate.Representative.IsVertical == observation.IsVertical &&
                ResultsMatch(candidate.Representative.Result, observation.Result))
            {
                return candidate;
            }

            index = index + 1;
        }

        return null;
    }

    static bool ResultsMatch(BarcodeDecodeResult first, BarcodeDecodeResult second)
    {
        if (first.Format != second.Format || first.Payload == null || second.Payload == null)
        {
            return false;
        }

        if (first.Payload.Text != second.Payload.Text)
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

    static LinearScanlineVoteGroup CreateGroup(
        LinearScanlineObservation observation,
        int observationIndex)
    {
        LinearScanlineVoteGroup group = new LinearScanlineVoteGroup();
        group.Representative = observation;
        group.FirstObservationIndex = observationIndex;
        group.MinimumCoordinate = observation.Coordinate;
        group.MaximumCoordinate = observation.Coordinate;
        AddVote(group, observation);
        return group;
    }

    static void AddVote(LinearScanlineVoteGroup group, LinearScanlineObservation observation)
    {
        group.VoteCount = group.VoteCount + 1;
        group.TotalStartPosition = group.TotalStartPosition + observation.StartPosition;
        group.TotalEndPosition = group.TotalEndPosition + observation.EndPosition;
        group.TotalModuleSize = group.TotalModuleSize + observation.ModuleSize;
        group.TotalVariance = group.TotalVariance + observation.AverageVariance;
        if (observation.Coordinate < group.MinimumCoordinate)
        {
            group.MinimumCoordinate = observation.Coordinate;
        }

        if (observation.Coordinate > group.MaximumCoordinate)
        {
            group.MaximumCoordinate = observation.Coordinate;
        }
    }

    static LinearScanlineVoteGroup? SelectWinner(
        global::System.Collections.Generic.List<LinearScanlineVoteGroup> groups)
    {
        LinearScanlineVoteGroup? winner = null;
        int index = 0;
        while (index < groups.Count)
        {
            LinearScanlineVoteGroup candidate = groups[index];
            if (winner == null || candidate.VoteCount > winner.VoteCount ||
                (candidate.VoteCount == winner.VoteCount &&
                    candidate.TotalVariance / candidate.VoteCount < winner.TotalVariance / winner.VoteCount))
            {
                winner = candidate;
            }

            index = index + 1;
        }

        return winner;
    }

    static LinearScanlineConsensus CreateConsensus(
        LinearScanlineVoteGroup winner,
        int observationCount)
    {
        LinearScanlineConsensus consensus = new LinearScanlineConsensus();
        consensus.Result = ResultCopier.CopyDecodeResult(winner.Representative.Result);
        consensus.VoteCount = winner.VoteCount;
        consensus.ObservationCount = observationCount;
        consensus.FirstObservationIndex = winner.FirstObservationIndex;
        consensus.AverageStartPosition = winner.TotalStartPosition / winner.VoteCount;
        consensus.AverageEndPosition = winner.TotalEndPosition / winner.VoteCount;
        consensus.AverageModuleSize = winner.TotalModuleSize / winner.VoteCount;
        consensus.AverageVariance = winner.TotalVariance / winner.VoteCount;
        consensus.MinimumCoordinate = winner.MinimumCoordinate;
        consensus.MaximumCoordinate = winner.MaximumCoordinate;
        consensus.IsVertical = winner.Representative.IsVertical;
        consensus.Result.Location = CreateLocation(consensus);
        UpdateConfidence(consensus);
        return consensus;
    }

    static BarcodeQuadrilateral CreateLocation(LinearScanlineConsensus consensus)
    {
        double crossStart = consensus.MinimumCoordinate;
        double crossEnd = consensus.MaximumCoordinate + 1.0;
        if (consensus.IsVertical)
        {
            return GeometryMath.CreateQuadrilateral(
                GeometryMath.CreatePoint(crossStart, consensus.AverageStartPosition),
                GeometryMath.CreatePoint(crossEnd, consensus.AverageStartPosition),
                GeometryMath.CreatePoint(crossEnd, consensus.AverageEndPosition),
                GeometryMath.CreatePoint(crossStart, consensus.AverageEndPosition));
        }

        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(consensus.AverageStartPosition, crossStart),
            GeometryMath.CreatePoint(consensus.AverageEndPosition, crossStart),
            GeometryMath.CreatePoint(consensus.AverageEndPosition, crossEnd),
            GeometryMath.CreatePoint(consensus.AverageStartPosition, crossEnd));
    }

    static void UpdateConfidence(LinearScanlineConsensus consensus)
    {
        if (consensus.Result.Metadata == null)
        {
            return;
        }

        double agreement = (double)consensus.VoteCount / consensus.ObservationCount;
        consensus.Result.Metadata.Confidence = consensus.Result.Metadata.Confidence * agreement;
    }
}

internal static class LinearDirectScanner
{
    internal static LinearScanlineConsensus? Scan(
        ModuleMatrix matrix,
        BarcodeFormat format,
        bool vertical,
        int centerCoordinate,
        int lineSpacing,
        int maximumLineCount,
        int minimumVotes)
    {
        LinearScanlineSample[] samples = ScanlineSampler.CreateFan(
            matrix,
            vertical,
            centerCoordinate,
            lineSpacing,
            maximumLineCount);
        global::System.Collections.Generic.List<LinearScanlineObservation> observations =
            new global::System.Collections.Generic.List<LinearScanlineObservation>();
        int index = 0;
        while (index < samples.Length)
        {
            LinearScanlineSample sample = samples[index];
            LinearScanlineObservation? observation = LinearScanlineDecoder.TryDecode(
                sample.Row,
                format,
                sample.IsVertical,
                sample.Coordinate);
            if (observation != null)
            {
                observations.Add(observation);
            }

            index = index + 1;
        }

        return LinearScanlineVoter.Reconcile(observations.ToArray(), minimumVotes);
    }
}
