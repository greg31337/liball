namespace qrcode;

internal class RectangularMicroFinderDetector
{
    internal const double MinimumSubfinderConfidence = 0.84;
    internal const int SubfinderSearchQuarterSteps = 4;

    internal static RectangularMicroFinderDetection[] Detect(
        QrRaster raster,
        FinderCandidate[] candidates,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (candidates == null)
        {
            throw new QrInputException("The finder candidates are required.", "candidates");
        }

        if (options.MaximumCandidates <= 0)
        {
            throw new QrInputException(
                "The maximum candidate count must be positive.",
                "MaximumCandidates");
        }

        if (candidates.Length > options.MaximumCandidates)
        {
            throw new QrResourceLimitException(
                "The finder candidate count exceeds the configured limit.",
                "MaximumCandidates",
                candidates.Length,
                options.MaximumCandidates);
        }

        for (int index = 0; index < candidates.Length; index++)
        {
            ValidateCandidate(candidates[index]);
        }

        if (!options.AllowRectangularMicro)
        {
            return new RectangularMicroFinderDetection[0];
        }

        global::System.Collections.Generic.List<RectangularMicroFinderDetection> detections =
            new global::System.Collections.Generic.List<RectangularMicroFinderDetection>();
        for (int index = 0; index < candidates.Length; index++)
        {
            RectangularMicroFinderDetection? detection = DetectCandidate(
                raster,
                candidates[index]);
            if (detection != null)
            {
                detections.Add(detection);
            }
        }

        RectangularMicroFinderDetection[] result = detections.ToArray();
        SortDetections(result);
        if (!options.DetectMultiple && result.Length > 1)
        {
            return new RectangularMicroFinderDetection[] { result[0] };
        }

        return result;
    }

    static RectangularMicroFinderDetection? DetectCandidate(
        QrRaster raster,
        FinderCandidate candidate)
    {
        MicroFinderBasis basis = CreateBasis(candidate);
        MicroFinderBasis reflected = new MicroFinderBasis
        {
            ColumnX = basis.RowX, ColumnY = basis.RowY, RowX = basis.ColumnX, RowY = basis.ColumnY
        };
        RectangularMicroFinderDetection? best = null;
        for (int orientation = 0; orientation < 8; orientation++)
        {
            int quarterTurns = orientation % 4;
            bool mirrored = orientation >= 4;
            RectangularMicroFinderDetection? detection = EvaluateOrientation(
                raster,
                candidate,
                mirrored ? reflected : basis,
                quarterTurns);
            if (detection != null)
            {
                detection.HasMirrorEstimate = true;
                detection.IsMirrored = mirrored;
            }
            if (detection != null
                && (best == null || detection.Confidence > best.Confidence + 1.0E-12))
            {
                best = detection;
            }
            else if (detection != null && best != null && mirrored != best.IsMirrored
                && global::System.Math.Abs(detection.Confidence - best.Confidence) <= 1.0E-12)
            {
                best.HasMirrorEstimate = false;
            }
        }

        return best;
    }

    static RectangularMicroFinderDetection? EvaluateOrientation(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns)
    {
        int finderCode = ReadFinderFormat(
            raster,
            candidate,
            basis,
            quarterTurns);
        if (finderCode < 0)
        {
            return null;
        }

        BchCorrectionResult finderCorrection = CorrectFinderFormat(finderCode);
        if (!finderCorrection.Success)
        {
            return null;
        }

        int versionIndex = finderCorrection.Data & 31;
        int errorCorrectionIndex = finderCorrection.Data >> 5;
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        RectangularMicroSubfinderEvidence subfinder = FindSubfinder(
            raster,
            candidate,
            basis,
            quarterTurns,
            width,
            height);
        if (!subfinder.Success)
        {
            return null;
        }

        int subfinderCode = ReadSubfinderFormat(
            raster,
            candidate,
            basis,
            quarterTurns,
            versionIndex,
            subfinder.ShiftX,
            subfinder.ShiftY);
        if (subfinderCode < 0)
        {
            return null;
        }

        BchCorrectionResult combined = FormatInformationCodec.CorrectRectangularMicroCopies(
            finderCode,
            subfinderCode);
        if (!combined.Success || combined.Data != finderCorrection.Data)
        {
            return null;
        }

        double finderFormatConfidence = 1.0 - finderCorrection.Distance / 4.0;
        double subfinderFormatConfidence = 1.0 - combined.SecondDistance / 4.0;
        if (subfinderFormatConfidence < 0.0)
        {
            subfinderFormatConfidence = 0.0;
        }

        double formatConfidence = (finderFormatConfidence
            + subfinderFormatConfidence) / 2.0;
        double confidence = (candidate.Confidence
            + candidate.RegionConfidence
            + subfinder.Confidence
            + formatConfidence) / 4.0;
        return new RectangularMicroFinderDetection
        {
            Finder = candidate,
            SubfinderCenter = new QrPoint
            {
                X = subfinder.CenterX,
                Y = subfinder.CenterY
            },
            SymbolCorners = CreateSymbolCorners(
                candidate,
                basis,
                width,
                height,
                quarterTurns),
            VersionIndex = versionIndex,
            Width = width,
            Height = height,
            QuarterTurnsClockwise = quarterTurns,
            ErrorCorrectionIndex = errorCorrectionIndex,
            FinderFormatCode = FormatInformationCodec.CreateRectangularMicroFinderCode(
                versionIndex,
                errorCorrectionIndex),
            SubfinderFormatCode = FormatInformationCodec.CreateRectangularMicroSubfinderCode(
                versionIndex,
                errorCorrectionIndex),
            FinderFormatDistance = combined.FirstDistance,
            SubfinderFormatDistance = combined.SecondDistance,
            FormatCopiesAgree = combined.CopiesAgree,
            SubfinderConfidence = subfinder.Confidence,
            FormatConfidence = formatConfidence,
            Confidence = confidence,
            Polarity = candidate.Polarity
        };
    }

    static BchCorrectionResult CorrectFinderFormat(int receivedCode)
    {
        int[] codes = new int[64];
        int[] data = new int[64];
        for (int value = 0; value < codes.Length; value++)
        {
            codes[value] = BchCodec.CreateCode(value, 6, 12, 0x1F25, 0x1FAB2);
            data[value] = value;
        }

        return BchCodec.CorrectNearestCode(receivedCode, 18, codes, data, 3);
    }

    static int ReadFinderFormat(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            int sample = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex),
                0.0,
                0.0);
            if (sample < 0)
            {
                return -1;
            }

            if (sample != 0)
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static int ReadSubfinderFormat(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int versionIndex,
        double shiftX,
        double shiftY)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            int sample = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex),
                shiftX,
                shiftY);
            if (sample < 0)
            {
                return -1;
            }

            if (sample != 0)
            {
                code |= 1 << bitIndex;
            }
        }

        return code;
    }

    static RectangularMicroSubfinderEvidence FindSubfinder(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int width,
        int height)
    {
        RectangularMicroSubfinderEvidence best = new RectangularMicroSubfinderEvidence();
        int bestDistance = int.MaxValue;
        for (int rowStep = -SubfinderSearchQuarterSteps;
            rowStep <= SubfinderSearchQuarterSteps;
            rowStep++)
        {
            for (int columnStep = -SubfinderSearchQuarterSteps;
                columnStep <= SubfinderSearchQuarterSteps;
                columnStep++)
            {
                double shiftHorizontal = columnStep * candidate.ModuleSize / 4.0;
                double shiftVertical = rowStep * candidate.ModuleSize / 4.0;
                double shiftX = MapHorizontalShiftX(
                    basis,
                    quarterTurns,
                    shiftHorizontal,
                    shiftVertical);
                double shiftY = MapHorizontalShiftY(
                    basis,
                    quarterTurns,
                    shiftHorizontal,
                    shiftVertical);
                double confidence = MeasureSubfinder(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    width,
                    height,
                    shiftX,
                    shiftY);
                int distance = rowStep * rowStep + columnStep * columnStep;
                if (confidence > best.Confidence
                    || confidence == best.Confidence && distance < bestDistance)
                {
                    QrPoint center = MapLogicalPoint(
                        candidate,
                        basis,
                        quarterTurns,
                        width - 2.5,
                        height - 2.5);
                    best.Success = confidence >= MinimumSubfinderConfidence;
                    best.ShiftX = shiftX;
                    best.ShiftY = shiftY;
                    best.CenterX = center.X + shiftX;
                    best.CenterY = center.Y + shiftY;
                    best.Confidence = confidence;
                    bestDistance = distance;
                }
            }
        }

        return best;
    }

    static double MeasureSubfinder(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int width,
        int height,
        double shiftX,
        double shiftY)
    {
        int matches = 0;
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                int sample = SampleLogicalModule(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    height - 3 + rowOffset,
                    width - 3 + columnOffset,
                    shiftX,
                    shiftY);
                if (sample < 0)
                {
                    return -1.0;
                }

                bool expected = rowOffset == -2
                    || rowOffset == 2
                    || columnOffset == -2
                    || columnOffset == 2
                    || rowOffset == 0 && columnOffset == 0;
                if ((sample != 0) == expected)
                {
                    matches++;
                }
            }
        }

        return matches / 25.0;
    }

    static int SampleLogicalModule(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int row,
        int column,
        double shiftX,
        double shiftY)
    {
        QrPoint point = MapLogicalPoint(
            candidate,
            basis,
            quarterTurns,
            column + 0.5,
            row + 0.5);
        double x = point.X + shiftX;
        double y = point.Y + shiftY;
        if (x < 0.0 || y < 0.0 || x >= raster.Width || y >= raster.Height)
        {
            return -1;
        }

        return raster.Pixels[(int)y * raster.Stride + (int)x] == 0 ? 1 : 0;
    }

    static QrQuadrilateral CreateSymbolCorners(
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int width,
        int height,
        int quarterTurns)
    {
        return new QrQuadrilateral
        {
            TopLeft = MapLogicalPoint(candidate, basis, quarterTurns, 0.0, 0.0),
            TopRight = MapLogicalPoint(candidate, basis, quarterTurns, width, 0.0),
            BottomRight = MapLogicalPoint(
                candidate,
                basis,
                quarterTurns,
                width,
                height),
            BottomLeft = MapLogicalPoint(candidate, basis, quarterTurns, 0.0, height)
        };
    }

    static QrPoint MapLogicalPoint(
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        double column,
        double row)
    {
        double horizontal = (column - 3.5) * candidate.ModuleSize;
        double vertical = (row - 3.5) * candidate.ModuleSize;
        QrPoint point = new QrPoint();
        if (quarterTurns == 0)
        {
            point.X = candidate.CenterX
                + horizontal * basis.ColumnX
                + vertical * basis.RowX;
            point.Y = candidate.CenterY
                + horizontal * basis.ColumnY
                + vertical * basis.RowY;
        }
        else if (quarterTurns == 1)
        {
            point.X = candidate.CenterX
                + horizontal * basis.RowX
                - vertical * basis.ColumnX;
            point.Y = candidate.CenterY
                + horizontal * basis.RowY
                - vertical * basis.ColumnY;
        }
        else if (quarterTurns == 2)
        {
            point.X = candidate.CenterX
                - horizontal * basis.ColumnX
                - vertical * basis.RowX;
            point.Y = candidate.CenterY
                - horizontal * basis.ColumnY
                - vertical * basis.RowY;
        }
        else
        {
            point.X = candidate.CenterX
                + vertical * basis.ColumnX
                - horizontal * basis.RowX;
            point.Y = candidate.CenterY
                + vertical * basis.ColumnY
                - horizontal * basis.RowY;
        }

        return point;
    }

    static double MapHorizontalShiftX(
        MicroFinderBasis basis,
        int quarterTurns,
        double horizontal,
        double vertical)
    {
        if (quarterTurns == 0)
        {
            return horizontal * basis.ColumnX + vertical * basis.RowX;
        }

        if (quarterTurns == 1)
        {
            return horizontal * basis.RowX - vertical * basis.ColumnX;
        }

        if (quarterTurns == 2)
        {
            return -horizontal * basis.ColumnX - vertical * basis.RowX;
        }

        return vertical * basis.ColumnX - horizontal * basis.RowX;
    }

    static double MapHorizontalShiftY(
        MicroFinderBasis basis,
        int quarterTurns,
        double horizontal,
        double vertical)
    {
        if (quarterTurns == 0)
        {
            return horizontal * basis.ColumnY + vertical * basis.RowY;
        }

        if (quarterTurns == 1)
        {
            return horizontal * basis.RowY - vertical * basis.ColumnY;
        }

        if (quarterTurns == 2)
        {
            return -horizontal * basis.ColumnY - vertical * basis.RowY;
        }

        return vertical * basis.ColumnY - horizontal * basis.RowY;
    }

    static MicroFinderBasis CreateBasis(FinderCandidate candidate)
    {
        QrQuadrilateral? cornersValue = candidate.FinderCorners;
        if (cornersValue == null)
        {
            throw new QrInputException(
                "Validated finder candidates require four recovered corners.",
                "candidates");
        }

        QrQuadrilateral corners = cornersValue;
        QrPoint? topLeftValue = corners.TopLeft;
        QrPoint? topRightValue = corners.TopRight;
        QrPoint? bottomRightValue = corners.BottomRight;
        QrPoint? bottomLeftValue = corners.BottomLeft;
        if (topLeftValue == null
            || topRightValue == null
            || bottomRightValue == null
            || bottomLeftValue == null)
        {
            throw new QrInputException(
                "Validated finder candidates require four recovered corners.",
                "candidates");
        }

        QrPoint topLeft = topLeftValue;
        QrPoint topRight = topRightValue;
        QrPoint bottomRight = bottomRightValue;
        QrPoint bottomLeft = bottomLeftValue;
        double columnX = (topRight.X + bottomRight.X - topLeft.X - bottomLeft.X) / 2.0;
        double columnY = (topRight.Y + bottomRight.Y - topLeft.Y - bottomLeft.Y) / 2.0;
        double rowX = (bottomLeft.X + bottomRight.X - topLeft.X - topRight.X) / 2.0;
        double rowY = (bottomLeft.Y + bottomRight.Y - topLeft.Y - topRight.Y) / 2.0;
        double columnLength = global::System.Math.Sqrt(
            columnX * columnX + columnY * columnY);
        double rowLength = global::System.Math.Sqrt(rowX * rowX + rowY * rowY);
        return new MicroFinderBasis
        {
            ColumnX = columnX / columnLength,
            ColumnY = columnY / columnLength,
            RowX = rowX / rowLength,
            RowY = rowY / rowLength
        };
    }

    static void SortDetections(RectangularMicroFinderDetection[] detections)
    {
        for (int index = 1; index < detections.Length; index++)
        {
            RectangularMicroFinderDetection value = detections[index];
            int position = index;
            while (position > 0
                && DetectionComesBefore(value, detections[position - 1]))
            {
                detections[position] = detections[position - 1];
                position--;
            }

            detections[position] = value;
        }
    }

    static bool DetectionComesBefore(
        RectangularMicroFinderDetection left,
        RectangularMicroFinderDetection right)
    {
        if (left.Confidence != right.Confidence)
        {
            return left.Confidence > right.Confidence;
        }

        if (left.Finder.CenterY != right.Finder.CenterY)
        {
            return left.Finder.CenterY < right.Finder.CenterY;
        }

        if (left.Finder.CenterX != right.Finder.CenterX)
        {
            return left.Finder.CenterX < right.Finder.CenterX;
        }

        return left.VersionIndex < right.VersionIndex;
    }

    static void ValidateCandidate(FinderCandidate candidate)
    {
        if (candidate == null
            || !IsFinite(candidate.CenterX)
            || !IsFinite(candidate.CenterY)
            || !IsFinite(candidate.ModuleSize)
            || !IsFinite(candidate.Confidence)
            || candidate.CenterX < 0.0
            || candidate.CenterY < 0.0
            || candidate.ModuleSize <= 0.0
            || candidate.Confidence < 0.0
            || candidate.Confidence > 1.0
            || candidate.ScaleToSource <= 0
            || !candidate.RegionValidated
            || !IsFinite(candidate.RegionConfidence)
            || candidate.RegionConfidence <= 0.0
            || candidate.RegionConfidence > 1.0
            || !HasValidCorners(candidate))
        {
            throw new QrInputException(
                "Finder candidate geometry or confidence is invalid.",
                "candidates");
        }

        int polarity = (int)candidate.Polarity;
        if (polarity < (int)QrPolarity.Automatic
            || polarity > (int)QrPolarity.LightOnDark)
        {
            throw new QrInputException(
                "The finder candidate polarity is not defined.",
                "candidates");
        }
    }

    static bool HasValidCorners(FinderCandidate candidate)
    {
        QrQuadrilateral? corners = candidate.FinderCorners;
        if (corners == null
            || corners.TopLeft == null
            || corners.TopRight == null
            || corners.BottomRight == null
            || corners.BottomLeft == null)
        {
            return false;
        }

        QrPoint topLeft = corners.TopLeft;
        QrPoint topRight = corners.TopRight;
        QrPoint bottomRight = corners.BottomRight;
        QrPoint bottomLeft = corners.BottomLeft;
        double columnX = topRight.X + bottomRight.X - topLeft.X - bottomLeft.X;
        double columnY = topRight.Y + bottomRight.Y - topLeft.Y - bottomLeft.Y;
        double rowX = bottomLeft.X + bottomRight.X - topLeft.X - topRight.X;
        double rowY = bottomLeft.Y + bottomRight.Y - topLeft.Y - topRight.Y;
        return IsFinite(topLeft.X)
            && IsFinite(topLeft.Y)
            && IsFinite(topRight.X)
            && IsFinite(topRight.Y)
            && IsFinite(bottomRight.X)
            && IsFinite(bottomRight.Y)
            && IsFinite(bottomLeft.X)
            && IsFinite(bottomLeft.Y)
            && columnX * columnX + columnY * columnY > 0.0
            && rowX * rowX + rowY * rowY > 0.0;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
