namespace qrcode;

internal class FinderCluster
{
    internal double WeightedCenterX;
    internal double WeightedCenterY;
    internal double WeightedModuleSize;
    internal double GeometryWeight;
    internal double ConfidenceSum;
    internal int EvidenceCount;
    internal int HorizontalEvidenceCount;
    internal int VerticalEvidenceCount;
    internal int MinimumScaleToSource = int.MaxValue;
    internal int MaximumScaleToSource;
    internal int BestScaleToSource = 1;
    internal double BestCandidateConfidence = -1.0;
    internal QrPolarity Polarity = QrPolarity.Automatic;
    internal double BestPolarityConfidence = -1.0;
    internal bool SawDarkOnLight;
    internal bool SawLightOnDark;
    internal double RegionConfidenceSum;
    internal double RegionWeight;
    internal double CornerWeight;
    internal int CornerScaleToSource = int.MaxValue;
    internal double TopLeftX;
    internal double TopLeftY;
    internal double TopRightX;
    internal double TopRightY;
    internal double BottomRightX;
    internal double BottomRightY;
    internal double BottomLeftX;
    internal double BottomLeftY;
    internal double BestRegionConfidence = -1.0;
    internal int CenterRegionArea;
    internal int OuterRegionArea;

    internal double GetMatchScore(FinderCandidate candidate)
    {
        double centerX = WeightedCenterX / GeometryWeight;
        double centerY = WeightedCenterY / GeometryWeight;
        double moduleSize = WeightedModuleSize / GeometryWeight;
        double horizontal = candidate.CenterX - centerX;
        double vertical = candidate.CenterY - centerY;
        double distance = global::System.Math.Sqrt(
            horizontal * horizontal + vertical * vertical);
        double maximumModule = moduleSize;
        double minimumModule = candidate.ModuleSize;
        if (candidate.ModuleSize > maximumModule)
        {
            maximumModule = candidate.ModuleSize;
            minimumModule = moduleSize;
        }

        double moduleRatio = minimumModule / maximumModule;
        if (moduleRatio < 0.5 || distance > maximumModule * 1.5)
        {
            return -1.0;
        }

        return distance / maximumModule + (1.0 - moduleRatio);
    }

    internal void AddCandidate(FinderCandidate candidate)
    {
        int evidence = candidate.EvidenceCount;
        double weight = candidate.Confidence * evidence;
        if (weight < 0.01 * evidence)
        {
            weight = 0.01 * evidence;
        }

        WeightedCenterX += candidate.CenterX * weight;
        WeightedCenterY += candidate.CenterY * weight;
        WeightedModuleSize += candidate.ModuleSize * weight;
        GeometryWeight += weight;
        ConfidenceSum += candidate.Confidence * evidence;
        EvidenceCount += evidence;

        int horizontalEvidence = candidate.HorizontalEvidenceCount;
        int verticalEvidence = candidate.VerticalEvidenceCount;
        if (horizontalEvidence + verticalEvidence == 0)
        {
            if (candidate.ScannedVertically)
            {
                verticalEvidence = evidence;
            }
            else
            {
                horizontalEvidence = evidence;
            }
        }

        HorizontalEvidenceCount += horizontalEvidence;
        VerticalEvidenceCount += verticalEvidence;
        int minimumScale = candidate.MinimumScaleToSource;
        if (minimumScale <= 0)
        {
            minimumScale = candidate.ScaleToSource;
        }

        int maximumScale = candidate.MaximumScaleToSource;
        if (maximumScale <= 0)
        {
            maximumScale = candidate.ScaleToSource;
        }

        if (minimumScale < MinimumScaleToSource)
        {
            MinimumScaleToSource = minimumScale;
        }

        if (maximumScale > MaximumScaleToSource)
        {
            MaximumScaleToSource = maximumScale;
        }

        if (candidate.Confidence > BestCandidateConfidence)
        {
            BestCandidateConfidence = candidate.Confidence;
            BestScaleToSource = candidate.ScaleToSource;
        }

        if (candidate.Polarity == QrPolarity.DarkOnLight)
        {
            SawDarkOnLight = true;
        }
        else if (candidate.Polarity == QrPolarity.LightOnDark)
        {
            SawLightOnDark = true;
        }

        if (candidate.Polarity != QrPolarity.Automatic
            && candidate.Confidence > BestPolarityConfidence)
        {
            BestPolarityConfidence = candidate.Confidence;
            Polarity = candidate.Polarity;
        }

        if (candidate.RegionValidated)
        {
            double regionWeight = candidate.RegionConfidence * evidence;
            if (regionWeight <= 0.0)
            {
                regionWeight = 0.01 * evidence;
            }

            RegionConfidenceSum += candidate.RegionConfidence * evidence;
            RegionWeight += evidence;
            AddCorners(candidate, regionWeight);
            if (candidate.RegionConfidence > BestRegionConfidence)
            {
                BestRegionConfidence = candidate.RegionConfidence;
                CenterRegionArea = candidate.CenterRegionArea;
                OuterRegionArea = candidate.OuterRegionArea;
            }
        }
    }

    internal FinderCandidate CreateCandidate()
    {
        FinderCandidate result = new FinderCandidate
        {
            CenterX = WeightedCenterX / GeometryWeight,
            CenterY = WeightedCenterY / GeometryWeight,
            ModuleSize = WeightedModuleSize / GeometryWeight,
            Confidence = ConfidenceSum / EvidenceCount,
            ScaleToSource = BestScaleToSource,
            ScannedVertically = VerticalEvidenceCount > HorizontalEvidenceCount,
            Polarity = Polarity,
            EvidenceCount = EvidenceCount,
            HorizontalEvidenceCount = HorizontalEvidenceCount,
            VerticalEvidenceCount = VerticalEvidenceCount,
            MinimumScaleToSource = MinimumScaleToSource,
            MaximumScaleToSource = MaximumScaleToSource,
            MixedPolarityEvidence = SawDarkOnLight && SawLightOnDark,
            RegionValidated = RegionWeight > 0.0,
            CenterRegionArea = CenterRegionArea,
            OuterRegionArea = OuterRegionArea
        };
        if (RegionWeight > 0.0)
        {
            result.RegionConfidence = RegionConfidenceSum / RegionWeight;
        }

        if (CornerWeight > 0.0)
        {
            result.FinderCorners = new QrQuadrilateral
            {
                TopLeft = new QrPoint
                {
                    X = TopLeftX / CornerWeight,
                    Y = TopLeftY / CornerWeight
                },
                TopRight = new QrPoint
                {
                    X = TopRightX / CornerWeight,
                    Y = TopRightY / CornerWeight
                },
                BottomRight = new QrPoint
                {
                    X = BottomRightX / CornerWeight,
                    Y = BottomRightY / CornerWeight
                },
                BottomLeft = new QrPoint
                {
                    X = BottomLeftX / CornerWeight,
                    Y = BottomLeftY / CornerWeight
                }
            };
        }

        return result;
    }

    void AddCorners(FinderCandidate candidate, double weight)
    {
        QrQuadrilateral? corners = candidate.FinderCorners;
        if (corners == null
            || corners.TopLeft == null
            || corners.TopRight == null
            || corners.BottomRight == null
            || corners.BottomLeft == null)
        {
            return;
        }

        // Quantized edges from coarser pyramid levels can tilt an otherwise
        // accurate finder basis. The error grows along long rectangular symbols.
        if (candidate.ScaleToSource > CornerScaleToSource)
        {
            return;
        }

        if (candidate.ScaleToSource < CornerScaleToSource)
        {
            CornerScaleToSource = candidate.ScaleToSource;
            CornerWeight = 0.0;
            TopLeftX = 0.0;
            TopLeftY = 0.0;
            TopRightX = 0.0;
            TopRightY = 0.0;
            BottomRightX = 0.0;
            BottomRightY = 0.0;
            BottomLeftX = 0.0;
            BottomLeftY = 0.0;
        }

        TopLeftX += corners.TopLeft.X * weight;
        TopLeftY += corners.TopLeft.Y * weight;
        TopRightX += corners.TopRight.X * weight;
        TopRightY += corners.TopRight.Y * weight;
        BottomRightX += corners.BottomRight.X * weight;
        BottomRightY += corners.BottomRight.Y * weight;
        BottomLeftX += corners.BottomLeft.X * weight;
        BottomLeftY += corners.BottomLeft.Y * weight;
        CornerWeight += weight;
    }
}
