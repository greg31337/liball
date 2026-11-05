namespace barcode;

internal class Pdf417PerspectiveRow
{
    internal int SampleIndex;
    internal double VerticalFraction;
    internal Pdf417ScanlineRow Row = new Pdf417ScanlineRow();
}

internal class Pdf417ImageDecodeResult
{
    internal Pdf417CodewordDecodeResult Decode =
        new Pdf417CodewordDecodeResult();
    internal Pdf417FinderEvidence FinderEvidence =
        new Pdf417FinderEvidence();
    internal Pdf417PerspectiveRow[] SampledRows =
        new Pdf417PerspectiveRow[0];
    internal BarcodeMirrorMode MirrorMode;
}

internal static class Pdf417ProjectiveDecoder
{
    internal const int MaximumPerspectiveLineCount = 512;
    internal const int DefaultMaximumResultCount = 16;
    internal const double SamplingQuietZoneModules = 3.0;

    internal static Pdf417ImageDecodeResult[] DecodeImage(
        ModuleMatrix image,
        BarcodeDecodeOptions? options)
    {
        Pdf417ImageDecodeResult[] results = TryDecodeImage(image, options);
        if (results.Length == 0)
        {
            throw new BarcodeDecodingException("The image does not contain a decodable PDF417 symbol.");
        }

        return results;
    }

    internal static Pdf417ImageDecodeResult[] TryDecodeImage(
        ModuleMatrix image,
        BarcodeDecodeOptions? options)
    {
        ValidateProjectiveImage(image);
        BarcodeDecodeOptions settings = CopyProjectiveOptions(options);
        ValidateProjectiveOptions(settings);
        int maximumResults = settings.MaximumResults == 0
            ? DefaultMaximumResultCount
            : settings.MaximumResults;
        ModuleMatrix working = CopyProjectiveMatrix(image);
        global::System.Collections.Generic.List<Pdf417ImageDecodeResult> results =
            new global::System.Collections.Generic.List<Pdf417ImageDecodeResult>();
        bool continueSearching = true;
        while (continueSearching && results.Count < maximumResults)
        {
            Pdf417FinderEvidence? evidence = Pdf417ImageFinder.Locate(
                working,
                settings.Polarity);
            if (evidence == null)
            {
                continueSearching = false;
            }
            else
            {
                if (IsProjectiveVariantAllowed(
                    Pdf417Variant.Standard,
                    settings.AllowedFormats))
                {
                    Pdf417ImageDecodeResult? decoded = TryDecodeCandidate(
                        image,
                        evidence,
                        settings);
                    if (decoded != null)
                    {
                        results.Add(decoded);
                    }
                }

                MaskFinderRegion(working, evidence);
            }
        }

        continueSearching = true;
        while (continueSearching && results.Count < maximumResults)
        {
            Pdf417FinderEvidence? evidence = Pdf417ImageFinder.LocateCompact(
                working,
                settings.Polarity);
            if (evidence == null)
            {
                continueSearching = false;
            }
            else
            {
                if (IsProjectiveVariantAllowed(
                    Pdf417Variant.Compact,
                    settings.AllowedFormats))
                {
                    Pdf417ImageDecodeResult? decoded = TryDecodeCandidate(
                        image,
                        evidence,
                        settings);
                    if (decoded != null)
                    {
                        results.Add(decoded);
                    }
                }

                MaskFinderRegion(working, evidence);
            }
        }

        SortProjectiveResults(results);
        return results.ToArray();
    }

    internal static Pdf417ImageDecodeResult DecodeCandidate(
        ModuleMatrix image,
        Pdf417FinderEvidence finderEvidence,
        BarcodeDecodeOptions? options)
    {
        Pdf417ImageDecodeResult? result = TryDecodeCandidate(
            image,
            finderEvidence,
            options);
        if (result == null)
        {
            throw new BarcodeDecodingException("The PDF417 image candidate could not be perspective-sampled and decoded.");
        }

        return result;
    }

    internal static Pdf417ImageDecodeResult? TryDecodeCandidate(
        ModuleMatrix image,
        Pdf417FinderEvidence finderEvidence,
        BarcodeDecodeOptions? options)
    {
        ValidateProjectiveCandidate(image, finderEvidence);
        BarcodeDecodeOptions settings = CopyProjectiveOptions(options);
        ValidateProjectiveOptions(settings);
        if (!IsProjectiveVariantAllowed(
            finderEvidence.Variant,
            settings.AllowedFormats))
        {
            return null;
        }

        Pdf417PerspectiveRow[] sampledRows = SamplePerspectiveRows(
            image,
            finderEvidence);
        if (sampledRows.Length < Pdf417ImageFinder.MinimumRowEvidenceCount)
        {
            return null;
        }

        Pdf417ScanlineRow[] observations =
            new Pdf417ScanlineRow[sampledRows.Length];
        int index = 0;
        while (index < sampledRows.Length)
        {
            observations[index] = sampledRows[index].Row;
            index = index + 1;
        }

        Pdf417CodewordDecodeResult decoded =
            Pdf417CodewordDecoder.DecodeRows(observations);
        BarcodeMirrorMode mirror = DetermineProjectiveMirror(
            sampledRows,
            decoded.RowMetadata);
        if (settings.MirrorMode != BarcodeMirrorMode.Both
            && settings.MirrorMode != mirror)
        {
            return null;
        }

        byte[] rawBytes = decoded.Result.Payload == null
            || decoded.Result.Payload.RawBytes == null
            ? new byte[0]
            : decoded.Result.Payload.RawBytes;
        if (!IsProjectiveLengthAllowed(
            rawBytes.Length,
            settings.AllowedLengths)
            || !IsProjectiveFormatAllowed(
                decoded.Result.Format,
                settings.AllowedFormats))
        {
            return null;
        }

        decoded.Result.Location = CopyProjectiveQuadrilateral(
            finderEvidence.Location);
        decoded.Result.OrientationDegrees = finderEvidence.AngleDegrees;
        decoded.Result.SkewDegrees = CalculateProjectiveSkew(
            finderEvidence.AngleDegrees);
        decoded.Result.Polarity = finderEvidence.Polarity;
        decoded.Result.MirrorMode = mirror;
        if (decoded.Result.Metadata != null)
        {
            decoded.Result.Metadata.Confidence =
                (decoded.Result.Metadata.Confidence
                    + finderEvidence.Confidence) * 0.5;
        }

        Pdf417ImageDecodeResult result = new Pdf417ImageDecodeResult();
        result.Decode = decoded;
        result.FinderEvidence = finderEvidence;
        result.SampledRows = sampledRows;
        result.MirrorMode = mirror;
        return result;
    }

    internal static Pdf417PerspectiveRow[] SamplePerspectiveRows(
        ModuleMatrix image,
        Pdf417FinderEvidence finderEvidence)
    {
        ValidateProjectiveCandidate(image, finderEvidence);
        BarcodePoint topLeft = GeometryMath.RequirePoint(
            finderEvidence.Location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(
            finderEvidence.Location.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(
            finderEvidence.Location.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(
            finderEvidence.Location.BottomLeft);
        double leftLength = GeometryMath.DistanceBetweenPoints(
            topLeft,
            bottomLeft);
        double rightLength = GeometryMath.DistanceBetweenPoints(
            topRight,
            bottomRight);
        int lineCount = (int)global::System.Math.Ceiling(
            (leftLength + rightLength) * 0.5);
        if (lineCount < Pdf417ImageFinder.MinimumRowEvidenceCount)
        {
            lineCount = Pdf417ImageFinder.MinimumRowEvidenceCount;
        }

        if (lineCount > MaximumPerspectiveLineCount)
        {
            lineCount = MaximumPerspectiveLineCount;
        }

        Pdf417PerspectiveRow[] buffer = new Pdf417PerspectiveRow[lineCount];
        int count = 0;
        int line = 0;
        while (line < lineCount)
        {
            double fraction = (line + 0.5) / lineCount;
            BarcodePoint left = GeometryMath.InterpolatePoint(
                topLeft,
                bottomLeft,
                fraction);
            BarcodePoint right = GeometryMath.InterpolatePoint(
                topRight,
                bottomRight,
                fraction);
            BitRow sampled = SampleProjectiveLine(
                image,
                finderEvidence,
                left,
                right);
            Pdf417ScanlineRow? row =
                Pdf417ScanningDecoder.TryDecodeRow(sampled);
            if (row != null && row.Variant == finderEvidence.Variant)
            {
                Pdf417PerspectiveRow perspectiveRow =
                    new Pdf417PerspectiveRow();
                perspectiveRow.SampleIndex = line;
                perspectiveRow.VerticalFraction = fraction;
                perspectiveRow.Row = row;
                buffer[count] = perspectiveRow;
                count = count + 1;
            }

            line = line + 1;
        }

        return CopyPerspectiveRowPrefix(buffer, count);
    }

    static BitRow SampleProjectiveLine(
        ModuleMatrix image,
        Pdf417FinderEvidence finderEvidence,
        BarcodePoint left,
        BarcodePoint right)
    {
        double deltaX = right.X - left.X;
        double deltaY = right.Y - left.Y;
        double length = global::System.Math.Sqrt(
            (deltaX * deltaX) + (deltaY * deltaY));
        if (length <= GeometryMath.Epsilon)
        {
            throw new BarcodeValidationException("PDF417 perspective row width must be positive.");
        }

        double unitX = deltaX / length;
        double unitY = deltaY / length;
        double quietPixels = finderEvidence.ModuleSize
            * SamplingQuietZoneModules;
        double startX = left.X - (unitX * quietPixels);
        double startY = left.Y - (unitY * quietPixels);
        double totalLength = length + (quietPixels * 2.0);
        int sampleCount = (int)global::System.Math.Round(totalLength) + 1;
        BitRow row = new BitRow(sampleCount);
        int sample = 0;
        while (sample < sampleCount)
        {
            double fraction = sampleCount == 1
                ? 0.0
                : sample / (double)(sampleCount - 1);
            int x = (int)global::System.Math.Round(
                startX + (unitX * totalLength * fraction));
            int y = (int)global::System.Math.Round(
                startY + (unitY * totalLength * fraction));
            if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
            {
                bool value = image.GetModule(x, y);
                bool foreground = finderEvidence.Polarity
                    == BarcodePolarity.DarkOnLight
                    ? value
                    : !value;
                if (foreground)
                {
                    row.SetBit(sample, true);
                }
            }

            sample = sample + 1;
        }

        return row;
    }

    static BarcodeMirrorMode DetermineProjectiveMirror(
        Pdf417PerspectiveRow[] sampledRows,
        Pdf417ReconciledMetadata metadata)
    {
        double sampleMean = 0.0;
        double rowMean = 0.0;
        int matchedCount = 0;
        int assignmentIndex = 0;
        while (assignmentIndex < metadata.Assignments.Length)
        {
            Pdf417RowAssignment assignment =
                metadata.Assignments[assignmentIndex];
            int sampleIndex = FindPerspectiveSampleIndex(
                sampledRows,
                assignment.Observation);
            if (sampleIndex >= 0)
            {
                sampleMean = sampleMean + sampleIndex;
                rowMean = rowMean + assignment.RowNumber;
                matchedCount = matchedCount + 1;
            }

            assignmentIndex = assignmentIndex + 1;
        }

        if (matchedCount < 2)
        {
            return BarcodeMirrorMode.Normal;
        }

        sampleMean = sampleMean / matchedCount;
        rowMean = rowMean / matchedCount;
        double covariance = 0.0;
        assignmentIndex = 0;
        while (assignmentIndex < metadata.Assignments.Length)
        {
            Pdf417RowAssignment assignment =
                metadata.Assignments[assignmentIndex];
            int sampleIndex = FindPerspectiveSampleIndex(
                sampledRows,
                assignment.Observation);
            if (sampleIndex >= 0)
            {
                covariance = covariance
                    + ((sampleIndex - sampleMean)
                        * (assignment.RowNumber - rowMean));
            }

            assignmentIndex = assignmentIndex + 1;
        }

        return covariance < 0.0
            ? BarcodeMirrorMode.Mirrored
            : BarcodeMirrorMode.Normal;
    }

    static int FindPerspectiveSampleIndex(
        Pdf417PerspectiveRow[] sampledRows,
        Pdf417ScanlineRow observation)
    {
        int index = 0;
        while (index < sampledRows.Length)
        {
            if (sampledRows[index].Row == observation)
            {
                return sampledRows[index].SampleIndex;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void MaskFinderRegion(
        ModuleMatrix image,
        Pdf417FinderEvidence evidence)
    {
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(
            evidence.Location);
        int margin = (int)global::System.Math.Ceiling(
            evidence.ModuleSize * SamplingQuietZoneModules);
        int left = (int)global::System.Math.Floor(bounds.X) - margin;
        int top = (int)global::System.Math.Floor(bounds.Y) - margin;
        int right = (int)global::System.Math.Ceiling(
            bounds.X + bounds.Width) + margin;
        int bottom = (int)global::System.Math.Ceiling(
            bounds.Y + bounds.Height) + margin;
        if (left < 0)
        {
            left = 0;
        }

        if (top < 0)
        {
            top = 0;
        }

        if (right >= image.Width)
        {
            right = image.Width - 1;
        }

        if (bottom >= image.Height)
        {
            bottom = image.Height - 1;
        }

        bool background = evidence.Polarity == BarcodePolarity.LightOnDark;
        int y = top;
        while (y <= bottom)
        {
            int x = left;
            while (x <= right)
            {
                image.SetModule(x, y, background);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    static double CalculateProjectiveSkew(double angleDegrees)
    {
        double nearestRightAngle = global::System.Math.Round(
            angleDegrees / 90.0) * 90.0;
        double skew = angleDegrees - nearestRightAngle;
        while (skew <= -45.0)
        {
            skew = skew + 90.0;
        }

        while (skew > 45.0)
        {
            skew = skew - 90.0;
        }

        return skew;
    }

    static bool IsProjectiveVariantAllowed(
        Pdf417Variant variant,
        BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null || allowedFormats.Length == 0)
        {
            return true;
        }

        int index = 0;
        while (index < allowedFormats.Length)
        {
            BarcodeFormat format = allowedFormats[index];
            if (format == BarcodeFormat.Automatic
                || format == BarcodeFormat.MacroPdf417
                || (variant == Pdf417Variant.Standard
                    && format == BarcodeFormat.Pdf417)
                || (variant == Pdf417Variant.Compact
                    && format == BarcodeFormat.CompactPdf417))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsProjectiveFormatAllowed(
        BarcodeFormat format,
        BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null || allowedFormats.Length == 0)
        {
            return true;
        }

        int index = 0;
        while (index < allowedFormats.Length)
        {
            if (allowedFormats[index] == BarcodeFormat.Automatic
                || allowedFormats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsProjectiveLengthAllowed(
        int length,
        int[]? allowedLengths)
    {
        if (allowedLengths == null || allowedLengths.Length == 0)
        {
            return true;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static BarcodeDecodeOptions CopyProjectiveOptions(
        BarcodeDecodeOptions? source)
    {
        BarcodeDecodeOptions copy = new BarcodeDecodeOptions();
        if (source == null)
        {
            copy.Polarity = BarcodePolarity.Automatic;
            copy.MirrorMode = BarcodeMirrorMode.Both;
        }
        else
        {
            copy.AllowedFormats = CopyProjectiveFormats(
                source.AllowedFormats);
            copy.AllowedLengths = CopyProjectiveIntegers(
                source.AllowedLengths);
            copy.MaximumResults = source.MaximumResults;
            copy.ExpectedResultCount = source.ExpectedResultCount;
            copy.Polarity = source.Polarity;
            copy.MirrorMode = source.MirrorMode;
        }

        return copy;
    }

    static BarcodeFormat[]? CopyProjectiveFormats(
        BarcodeFormat[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeFormat[] copy = new BarcodeFormat[source.Length];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[]? CopyProjectiveIntegers(int[]? source)
    {
        if (source == null)
        {
            return null;
        }

        int[] copy = new int[source.Length];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static ModuleMatrix CopyProjectiveMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                copy.SetModule(x, y, source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return copy;
    }

    static Pdf417PerspectiveRow[] CopyPerspectiveRowPrefix(
        Pdf417PerspectiveRow[] source,
        int count)
    {
        Pdf417PerspectiveRow[] copy = new Pdf417PerspectiveRow[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static BarcodeQuadrilateral CopyProjectiveQuadrilateral(
        BarcodeQuadrilateral source)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.RequirePoint(source.TopLeft),
            GeometryMath.RequirePoint(source.TopRight),
            GeometryMath.RequirePoint(source.BottomRight),
            GeometryMath.RequirePoint(source.BottomLeft));
    }

    static void SortProjectiveResults(
        global::System.Collections.Generic.List<Pdf417ImageDecodeResult> results)
    {
        int index = 1;
        while (index < results.Count)
        {
            Pdf417ImageDecodeResult value = results[index];
            int destination = index - 1;
            while (destination >= 0
                && ProjectiveResultComesAfter(results[destination], value))
            {
                results[destination + 1] = results[destination];
                destination = destination - 1;
            }

            results[destination + 1] = value;
            index = index + 1;
        }
    }

    static bool ProjectiveResultComesAfter(
        Pdf417ImageDecodeResult first,
        Pdf417ImageDecodeResult second)
    {
        BarcodeRectangle firstBounds =
            GeometryMath.CalculateQuadrilateralBounds(
                first.FinderEvidence.Location);
        BarcodeRectangle secondBounds =
            GeometryMath.CalculateQuadrilateralBounds(
                second.FinderEvidence.Location);
        if (firstBounds.Y > secondBounds.Y + GeometryMath.Epsilon)
        {
            return true;
        }

        return global::System.Math.Abs(firstBounds.Y - secondBounds.Y)
            <= GeometryMath.Epsilon
            && firstBounds.X > secondBounds.X;
    }

    static void ValidateProjectiveImage(ModuleMatrix image)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("PDF417 projective image must not be null.");
        }

        if (image.Width < Pdf417ImageFinder.MinimumImageDimension
            || image.Height < Pdf417ImageFinder.MinimumImageDimension)
        {
            throw new BarcodeValidationException("PDF417 projective image dimensions are too small.");
        }
    }

    static void ValidateProjectiveCandidate(
        ModuleMatrix image,
        Pdf417FinderEvidence finderEvidence)
    {
        ValidateProjectiveImage(image);
        if (finderEvidence == null)
        {
            throw new BarcodeValidationException("PDF417 finder evidence must not be null.");
        }

        GeometryMath.ValidateQuadrilateral(finderEvidence.Location);
        if (finderEvidence.Variant != Pdf417Variant.Standard
            && finderEvidence.Variant != Pdf417Variant.Compact)
        {
            throw new BarcodeValidationException("PDF417 finder evidence has an invalid variant.");
        }

        if (finderEvidence.Polarity != BarcodePolarity.DarkOnLight
            && finderEvidence.Polarity != BarcodePolarity.LightOnDark)
        {
            throw new BarcodeValidationException("PDF417 finder evidence has an unresolved polarity.");
        }

        if (finderEvidence.ModuleSize <= 0.0)
        {
            throw new BarcodeValidationException("PDF417 finder evidence has an invalid module size.");
        }
    }

    static void ValidateProjectiveOptions(BarcodeDecodeOptions options)
    {
        if (options.MaximumResults < 0)
        {
            throw new BarcodeValidationException("PDF417 maximum result count must not be negative.");
        }

        if (options.Polarity != BarcodePolarity.DarkOnLight
            && options.Polarity != BarcodePolarity.LightOnDark
            && options.Polarity != BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException("PDF417 decode polarity is invalid.");
        }

        if (options.MirrorMode != BarcodeMirrorMode.Normal
            && options.MirrorMode != BarcodeMirrorMode.Mirrored
            && options.MirrorMode != BarcodeMirrorMode.Both)
        {
            throw new BarcodeValidationException("PDF417 mirror mode is invalid.");
        }

        if (options.AllowedLengths != null)
        {
            int index = 0;
            while (index < options.AllowedLengths.Length)
            {
                if (options.AllowedLengths[index] < 0)
                {
                    throw new BarcodeValidationException("PDF417 allowed lengths must not be negative.");
                }

                index = index + 1;
            }
        }
    }
}
