namespace qrcode;

internal class MicroFinderDetector
{
    internal const double MinimumTimingConfidence = 0.75;
    internal const double MinimumExtentConfidence = 0.75;

    internal static MicroFinderDetection[] Detect(
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

        if (!options.AllowMicro)
        {
            return new MicroFinderDetection[0];
        }

        global::System.Collections.Generic.List<MicroFinderDetection> detections =
            new global::System.Collections.Generic.List<MicroFinderDetection>();
        for (int index = 0; index < candidates.Length; index++)
        {
            MicroFinderDetection? detection = DetectCandidate(
                raster,
                candidates[index],
                options);
            if (detection != null)
            {
                detections.Add(detection);
            }
        }

        MicroFinderDetection[] result = detections.ToArray();
        SortDetections(result);
        if (!options.DetectMultiple && result.Length > 1)
        {
            return new MicroFinderDetection[] { result[0] };
        }

        return result;
    }

    static MicroFinderDetection? DetectCandidate(
        QrRaster raster,
        FinderCandidate candidate,
        QrDetectionOptions options)
    {
        MicroFinderBasis basis = CreateBasis(candidate);
        MicroFinderBasis reflected = new MicroFinderBasis
        {
            ColumnX = basis.RowX, ColumnY = basis.RowY, RowX = basis.ColumnX, RowY = basis.ColumnY
        };
        MicroOrientationEvidence? best = null;
        int bestQuarterTurns = 0;
        bool bestMirrored = false;
        bool mirrorKnown = false;
        for (int orientation = 0; orientation < 8; orientation++)
        {
            int quarterTurns = orientation % 4;
            bool mirrored = orientation >= 4;
            MicroOrientationEvidence evidence = EvaluateOrientation(
                raster,
                candidate,
                mirrored ? reflected : basis,
                quarterTurns,
                options);
            if (evidence.Success
                && (best == null || evidence.Confidence > best.Confidence + 1.0E-12))
            {
                best = evidence;
                bestQuarterTurns = quarterTurns;
                bestMirrored = mirrored;
                mirrorKnown = true;
            }
            else if (evidence.Success && best != null && mirrored != bestMirrored
                && global::System.Math.Abs(evidence.Confidence - best.Confidence) <= 1.0E-12)
            {
                mirrorKnown = false;
            }
        }

        if (best == null)
        {
            return null;
        }

        return new MicroFinderDetection
        {
            Finder = candidate,
            SymbolCorners = CreateSymbolCorners(
                candidate,
                bestMirrored ? reflected : basis,
                best.Dimension,
                bestQuarterTurns),
            Version = best.Version,
            Dimension = best.Dimension,
            QuarterTurnsClockwise = bestQuarterTurns,
            ErrorCorrectionIndex = best.ErrorCorrectionIndex,
            Mask = best.Mask,
            FormatCode = best.FormatCode,
            FormatDistance = best.FormatDistance,
            TimingConfidence = best.TimingConfidence,
            FormatConfidence = best.FormatConfidence,
            ExtentConfidence = best.ExtentConfidence,
            Confidence = best.Confidence,
            Polarity = candidate.Polarity,
            HasMirrorEstimate = mirrorKnown,
            IsMirrored = mirrorKnown && bestMirrored
        };
    }

    static MicroOrientationEvidence EvaluateOrientation(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        QrDetectionOptions options)
    {
        MicroOrientationEvidence failure = new MicroOrientationEvidence();
        int receivedFormat = ReadFormatCode(raster, candidate, basis, quarterTurns);
        if (receivedFormat < 0)
        {
            return failure;
        }

        BchCorrectionResult correction = FormatInformationCodec.CorrectMicroCopies(
            receivedFormat,
            receivedFormat);
        if (!correction.Success)
        {
            return failure;
        }

        int symbolNumber = correction.Data >> 2;
        int version = GetVersion(symbolNumber);
        int errorCorrectionIndex = GetErrorCorrectionIndex(symbolNumber);
        int dimension = MicroVersionTable.GetDimension(version);
        double timingConfidence = MeasureTimingConfidence(
            raster,
            candidate,
            basis,
            quarterTurns,
            version,
            dimension);
        if (timingConfidence < MinimumTimingConfidence)
        {
            return failure;
        }

        double extentConfidence = MeasureExtentConfidence(
            raster,
            candidate,
            basis,
            quarterTurns,
            dimension,
            options.AllowCroppedQuietZone);
        if (extentConfidence < 0.0)
        {
            return failure;
        }

        double formatConfidence = 1.0 - correction.Distance / 4.0;
        double confidence = (candidate.Confidence
            + candidate.RegionConfidence
            + timingConfidence
            + formatConfidence
            + extentConfidence) / 5.0;
        return new MicroOrientationEvidence
        {
            Success = true,
            Version = version,
            Dimension = dimension,
            ErrorCorrectionIndex = errorCorrectionIndex,
            Mask = correction.Data & 3,
            FormatCode = correction.Code,
            FormatDistance = correction.Distance,
            TimingConfidence = timingConfidence,
            FormatConfidence = formatConfidence,
            ExtentConfidence = extentConfidence,
            Confidence = confidence
        };
    }

    static int ReadFormatCode(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns)
    {
        int code = 0;
        for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
        {
            int sample = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                MicroPlacementTable.GetFormatVerticalRow(bitIndex),
                MicroPlacementTable.GetFormatVerticalColumn(bitIndex));
            if (sample < 0)
            {
                return -1;
            }

            if (sample != 0)
            {
                code |= 1 << bitIndex;
            }
        }

        for (int bitIndex = 8; bitIndex < 15; bitIndex++)
        {
            int sample = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                MicroPlacementTable.GetFormatHorizontalRow(bitIndex),
                MicroPlacementTable.GetFormatHorizontalColumn(bitIndex));
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

    static double MeasureTimingConfidence(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int version,
        int dimension)
    {
        int matches = 0;
        int total = 0;
        for (int index = MicroPlacementTable.TimingStartIndex;
            index < dimension;
            index++)
        {
            int horizontal = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                0,
                index);
            int vertical = SampleLogicalModule(
                raster,
                candidate,
                basis,
                quarterTurns,
                index,
                0);
            if (horizontal < 0 || vertical < 0)
            {
                return -1.0;
            }

            bool expected = MicroPlacementTable.GetTimingModuleValue(version, index);
            if ((horizontal != 0) == expected)
            {
                matches++;
            }

            if ((vertical != 0) == expected)
            {
                matches++;
            }

            total += 2;
        }

        return (double)matches / total;
    }

    static double MeasureExtentConfidence(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int dimension,
        bool allowCroppedQuietZone)
    {
        int light = 0;
        int observed = 0;
        int expected = 0;
        for (int quietOffset = 1;
            quietOffset <= MicroPlacementTable.QuietZoneWidth;
            quietOffset++)
        {
            int first = -quietOffset;
            int last = dimension - 1 + quietOffset;
            for (int index = first; index <= last; index++)
            {
                int sample = SampleLogicalModule(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    first,
                    index);
                if (sample >= 0)
                {
                    observed++;
                    if (sample == 0)
                    {
                        light++;
                    }
                }

                sample = SampleLogicalModule(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    last,
                    index);
                if (sample >= 0)
                {
                    observed++;
                    if (sample == 0)
                    {
                        light++;
                    }
                }

                sample = SampleLogicalModule(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    index,
                    first);
                if (sample >= 0)
                {
                    observed++;
                    if (sample == 0)
                    {
                        light++;
                    }
                }

                sample = SampleLogicalModule(
                    raster,
                    candidate,
                    basis,
                    quarterTurns,
                    index,
                    last);
                if (sample >= 0)
                {
                    observed++;
                    if (sample == 0)
                    {
                        light++;
                    }
                }

                expected += 4;
            }
        }

        if (!allowCroppedQuietZone && observed != expected)
        {
            return -1.0;
        }

        if (observed == 0)
        {
            return allowCroppedQuietZone ? 0.5 : -1.0;
        }

        double confidence = (double)light / observed;
        if (confidence < MinimumExtentConfidence)
        {
            return -1.0;
        }

        return confidence;
    }

    static int SampleLogicalModule(
        QrRaster raster,
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int quarterTurns,
        int row,
        int column)
    {
        double horizontal = (column - 3.0) * candidate.ModuleSize;
        double vertical = (row - 3.0) * candidate.ModuleSize;
        double x = candidate.CenterX;
        double y = candidate.CenterY;
        if (quarterTurns == 0)
        {
            x += horizontal * basis.ColumnX + vertical * basis.RowX;
            y += horizontal * basis.ColumnY + vertical * basis.RowY;
        }
        else if (quarterTurns == 1)
        {
            x += horizontal * basis.RowX - vertical * basis.ColumnX;
            y += horizontal * basis.RowY - vertical * basis.ColumnY;
        }
        else if (quarterTurns == 2)
        {
            x -= horizontal * basis.ColumnX + vertical * basis.RowX;
            y -= horizontal * basis.ColumnY + vertical * basis.RowY;
        }
        else
        {
            x += vertical * basis.ColumnX - horizontal * basis.RowX;
            y += vertical * basis.ColumnY - horizontal * basis.RowY;
        }

        if (x < 0.0 || y < 0.0 || x >= raster.Width || y >= raster.Height)
        {
            return -1;
        }

        return raster.Pixels[(int)y * raster.Stride + (int)x] == 0 ? 1 : 0;
    }

    static QrQuadrilateral CreateSymbolCorners(
        FinderCandidate candidate,
        MicroFinderBasis basis,
        int dimension,
        int quarterTurns)
    {
        return new QrQuadrilateral
        {
            TopLeft = MapSymbolCorner(
                candidate,
                basis,
                quarterTurns,
                0.0,
                0.0),
            TopRight = MapSymbolCorner(
                candidate,
                basis,
                quarterTurns,
                dimension,
                0.0),
            BottomRight = MapSymbolCorner(
                candidate,
                basis,
                quarterTurns,
                dimension,
                dimension),
            BottomLeft = MapSymbolCorner(
                candidate,
                basis,
                quarterTurns,
                0.0,
                dimension)
        };
    }

    static QrPoint MapSymbolCorner(
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

    static int GetVersion(int symbolNumber)
    {
        if (symbolNumber == 0)
        {
            return 1;
        }

        if (symbolNumber <= 2)
        {
            return 2;
        }

        if (symbolNumber <= 4)
        {
            return 3;
        }

        return 4;
    }

    static int GetErrorCorrectionIndex(int symbolNumber)
    {
        if (symbolNumber == 0)
        {
            return MicroVersionTable.DetectionOnly;
        }

        if (symbolNumber <= 2)
        {
            return symbolNumber - 1;
        }

        if (symbolNumber <= 4)
        {
            return symbolNumber - 3;
        }

        return symbolNumber - 5;
    }

    static void SortDetections(MicroFinderDetection[] detections)
    {
        for (int index = 1; index < detections.Length; index++)
        {
            MicroFinderDetection value = detections[index];
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
        MicroFinderDetection left,
        MicroFinderDetection right)
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

        return left.QuarterTurnsClockwise < right.QuarterTurnsClockwise;
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
