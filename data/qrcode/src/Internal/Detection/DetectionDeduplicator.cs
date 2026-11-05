namespace qrcode;

internal class DetectionDeduplicator
{
    // All corners and module sizes must use the same source-image coordinates.
    // Distinct candidates carry distinct stable CandidateIndex values assigned
    // before this operation. Returned entries retain their original metadata.
    internal static QrDetection[] SelectDetections(QrDetection[] candidates, QrDetectionOptions options)
    {
        if (candidates == null || options == null)
        {
            throw new QrInputException("Candidates and detection options are required.",
                candidates == null ? "candidates" : "options");
        }

        if (options.MaximumCandidates <= 0 || options.MaximumPyramidLevels <= 0)
        {
            throw new QrInputException("Candidate and pyramid limits must be positive.", "options");
        }

        long maximumInput = (long)options.MaximumCandidates * options.MaximumPyramidLevels * 2;
        if (candidates.Length > maximumInput)
        {
            throw new QrResourceLimitException("The combined detections exceed the configured pass limits.",
                "MaximumCandidates", candidates.Length,
                maximumInput > int.MaxValue ? int.MaxValue : (int)maximumInput);
        }

        CheckedDimensions.CalculateAllocationBytes(candidates.Length, 128, QrLimits.DefaultMaximumAllocationBytes);
        DetectionGeometry[] ordered = new DetectionGeometry[candidates.Length];
        global::System.Collections.Generic.Dictionary<int, QrDetection> indices =
            new global::System.Collections.Generic.Dictionary<int, QrDetection>();
        for (int index = 0; index < candidates.Length; index++)
        {
            QrDetection candidate = candidates[index];
            ordered[index] = CreateGeometry(candidate);
            if (indices.ContainsKey(candidate.CandidateIndex)
                && indices[candidate.CandidateIndex] != candidate)
            {
                throw new QrInputException("Distinct candidates require distinct stable indices.", "CandidateIndex");
            }

            indices[candidate.CandidateIndex] = candidate;
        }

        global::System.Array.Sort(ordered, (left, right) => CompareRank(left, right));
        global::System.Collections.Generic.List<DetectionGeometry> retained =
            new global::System.Collections.Generic.List<DetectionGeometry>();
        int limit = options.DetectMultiple ? options.MaximumCandidates : 1;
        foreach (DetectionGeometry candidate in ordered)
        {
            bool duplicate = false;
            foreach (DetectionGeometry selected in retained)
            {
                if (AreDuplicates(candidate, selected))
                {
                    selected.Members.Add(candidate.Detection);
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                retained.Add(candidate);
                if (retained.Count == limit)
                {
                    break;
                }
            }
        }

        DetectionGeometry[] resultOrder = retained.ToArray();
        global::System.Array.Sort(resultOrder, (left, right) => ComparePosition(left, right));
        QrDetection[] result = new QrDetection[resultOrder.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = resultOrder[index].Detection;
        }

        return result;
    }

    internal static bool RepresentSameSymbol(QrDetection first, QrDetection second)
    {
        return AreDuplicates(CreateGeometry(first), CreateGeometry(second));
    }

    static DetectionGeometry CreateGeometry(QrDetection candidate)
    {
        if (candidate == null)
        {
            throw new QrInputException("Detection entries cannot be null.", "candidates");
        }

        if (!double.IsFinite(candidate.Confidence) || candidate.Confidence < 0.0 || candidate.Confidence > 1.0
            || !double.IsFinite(candidate.ModuleSize) || candidate.ModuleSize <= 0.0
            || candidate.CandidateIndex < 0 || candidate.EstimatedWidth < 0 || candidate.EstimatedHeight < 0
            || candidate.EstimatedVersion < 0)
        {
            throw new QrInputException("The detection geometry, confidence, or index is invalid.", "candidates");
        }

        if ((int)candidate.Polarity < (int)QrPolarity.Automatic
            || (int)candidate.Polarity > (int)QrPolarity.LightOnDark
            || candidate.HasSymbolKindEstimate && ((int)candidate.EstimatedSymbolKind < (int)QrSymbolKind.Model2
                || (int)candidate.EstimatedSymbolKind > (int)QrSymbolKind.LegacyModel1))
        {
            throw new QrInputException("The detection metadata contains an undefined enum value.", "candidates");
        }

        QrQuadrilateral? corners = candidate.Corners;
        if (corners == null || corners.TopLeft == null || corners.TopRight == null
            || corners.BottomRight == null || corners.BottomLeft == null)
        {
            throw new QrInputException("Detections require four symbol corners.", "Corners");
        }

        QrQuadrilateral unit = new QrQuadrilateral
        {
            TopLeft = new QrPoint(), TopRight = new QrPoint { X = 1 },
            BottomRight = new QrPoint { X = 1, Y = 1 }, BottomLeft = new QrPoint { Y = 1 }
        };
        PerspectiveTransform.Create(unit, corners);
        QrPoint[] points = new QrPoint[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft };
        double minimumX = points[0].X;
        double minimumY = points[0].Y;
        double tolerance = candidate.ModuleSize * 0.75;
        int start = 0;
        for (int index = 0; index < points.Length; index++)
        {
            minimumX = global::System.Math.Min(minimumX, points[index].X);
            minimumY = global::System.Math.Min(minimumY, points[index].Y);
            if (ComparePoint(points[index], points[start]) < 0)
            {
                start = index;
            }

            QrPoint next = points[(index + 1) % 4];
            double dx = next.X - points[index].X;
            double dy = next.Y - points[index].Y;
            double scale = global::System.Math.Max(global::System.Math.Abs(dx), global::System.Math.Abs(dy));
            double edge = scale * global::System.Math.Sqrt(dx / scale * (dx / scale) + dy / scale * (dy / scale));
            tolerance = global::System.Math.Min(tolerance, edge * 0.1);
        }

        int direction = ComparePoint(points[(start + 1) % 4], points[(start + 3) % 4]) <= 0 ? 1 : -1;
        QrPoint[] canonical = new QrPoint[4];
        for (int index = 0; index < 4; index++)
        {
            canonical[index] = points[(start + direction * index + 4) % 4];
        }

        DetectionGeometry geometry = new DetectionGeometry
        {
            Detection = candidate, Corners = canonical,
            MinimumX = minimumX, MinimumY = minimumY, Tolerance = tolerance
        };
        geometry.Members.Add(candidate);
        return geometry;
    }

    static bool AreDuplicates(DetectionGeometry left, DetectionGeometry right)
    {
        QrDetection first = left.Detection;
        QrDetection second = right.Detection;
        if (first == second)
        {
            return true;
        }

        // An unresolved high-confidence candidate must not bridge conflicting
        // known hypotheses that were already suppressed into this representative.
        foreach (QrDetection member in right.Members)
        {
            if (!HaveCompatibleMetadata(first, member))
            {
                return false;
            }
        }

        double tolerance = global::System.Math.Min(left.Tolerance, right.Tolerance);
        // Compare all cyclic/reversed corner correspondences. Axis-aligned
        // bounding-box overlap alone cannot identify the same physical symbol.
        for (int start = 0; start < 4; start++)
        {
            for (int direction = -1; direction <= 1; direction += 2)
            {
                bool matches = true;
                for (int index = 0; index < 4; index++)
                {
                    QrPoint a = left.Corners[index];
                    QrPoint b = right.Corners[(start + direction * index + 4) % 4];
                    double dx = (a.X - b.X) / tolerance;
                    double dy = (a.Y - b.Y) / tolerance;
                    if (!double.IsFinite(dx) || !double.IsFinite(dy) || dx * dx + dy * dy > 1.0)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool HaveCompatibleMetadata(QrDetection first, QrDetection second)
    {
        if (first.HasSymbolKindEstimate && second.HasSymbolKindEstimate
            && first.EstimatedSymbolKind != second.EstimatedSymbolKind
            || first.Polarity != QrPolarity.Automatic && second.Polarity != QrPolarity.Automatic
                && first.Polarity != second.Polarity
            || first.HasMirrorEstimate && second.HasMirrorEstimate && first.IsMirrored != second.IsMirrored)
        {
            return false;
        }

        if (first.EstimatedWidth > 0 && first.EstimatedHeight > 0
            && second.EstimatedWidth > 0 && second.EstimatedHeight > 0
            && !(first.EstimatedWidth == second.EstimatedWidth && first.EstimatedHeight == second.EstimatedHeight)
            && !(first.EstimatedWidth == second.EstimatedHeight && first.EstimatedHeight == second.EstimatedWidth))
        {
            return false;
        }

        return true;
    }

    static int CompareRank(DetectionGeometry left, DetectionGeometry right)
    {
        int comparison = right.Detection.Confidence.CompareTo(left.Detection.Confidence);
        if (comparison != 0)
        {
            return comparison;
        }

        // Prefer resolved metadata on confidence ties, then the stable identity.
        comparison = right.Detection.HasSymbolKindEstimate.CompareTo(left.Detection.HasSymbolKindEstimate);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = right.Detection.HasMirrorEstimate.CompareTo(left.Detection.HasMirrorEstimate);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.Detection.CandidateIndex.CompareTo(right.Detection.CandidateIndex);
    }

    static int ComparePosition(DetectionGeometry left, DetectionGeometry right)
    {
        int comparison = left.MinimumY.CompareTo(right.MinimumY);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.MinimumX.CompareTo(right.MinimumX);
        if (comparison != 0)
        {
            return comparison;
        }

        for (int index = 0; index < 4; index++)
        {
            comparison = ComparePoint(left.Corners[index], right.Corners[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return CompareRank(left, right);
    }

    static int ComparePoint(QrPoint left, QrPoint right)
    {
        int comparison = left.Y.CompareTo(right.Y);
        return comparison != 0 ? comparison : left.X.CompareTo(right.X);
    }
}
