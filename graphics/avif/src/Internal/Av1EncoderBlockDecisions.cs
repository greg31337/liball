using System;

namespace avif;

internal class Av1EncoderBlockAnalysis
{
    internal int MiRow;
    internal int MiColumn;
    internal int SuperblockIndex;
    internal Av1BlockSize BlockSize;
    internal int TargetQIndex;
    internal int[] TargetLoopFilterLevels;
    internal long CodedDistortion;
    internal int CodedRate;
    internal long SkipDistortion;
    internal int SkipRate;
    internal bool AllowSkip;
    internal long[] MatrixDistortions;
    internal int[] MatrixRates;
    internal int CoefficientLevel;
    internal int DcSign;
    internal object? Value;

    internal Av1EncoderBlockAnalysis()
    {
        TargetLoopFilterLevels = new int[4];
        MatrixDistortions = System.Array.Empty<long>();
        MatrixRates = System.Array.Empty<int>();
    }
}

internal class Av1EncoderBlockDecisionRequest
{
    internal int FrameMiRows;
    internal int FrameMiColumns;
    internal int BaseQIndex;
    internal int[] BaseLoopFilterLevels;
    internal int RateDistortionLambdaQ8;
    internal bool EnableSegmentation;
    internal int MaximumSegments;
    internal bool EnableDeltaQuantizer;
    internal int DeltaQuantizerResolution;
    internal bool EnableDeltaLoopFilter;
    internal int DeltaLoopFilterResolution;
    internal bool DeltaLoopFilterMulti;
    internal bool EnableQuantizerMatrix;
    internal Av1EncoderBlockAnalysis[] Blocks;

    internal Av1EncoderBlockDecisionRequest()
    {
        BaseLoopFilterLevels = new int[4];
        MaximumSegments = Av1SyntaxConstants.MaxSegments;
        Blocks = System.Array.Empty<Av1EncoderBlockAnalysis>();
    }
}

internal class Av1EncoderBlockEntropyContexts
{
    internal int SkipContext;
    internal int SegmentPredictedContext;
    internal int SegmentIdContext;
    internal int PredictedSegmentId;
    internal int TransformBlockSkipContext;
    internal int DcSignContext;
}

internal class Av1EncoderBlockDecision
{
    internal Av1EncoderBlockAnalysis? Analysis;
    internal int SegmentId;
    internal bool SegmentForcesSkip;
    internal bool Skip;
    internal bool SkipSignaled;
    internal bool DeltaQuantizerPresent;
    internal int DeltaQuantizer;
    internal bool DeltaLoopFilterPresent;
    internal int[] DeltaLoopFilters;
    internal int CurrentQIndex;
    internal int EffectiveQIndex;
    internal int[] EffectiveLoopFilterLevels;
    internal int MatrixLevel;
    internal long Distortion;
    internal int EstimatedRate;
    internal long RateDistortionScore;
    internal Av1EncoderBlockEntropyContexts? EntropyContexts;

    internal Av1EncoderBlockDecision()
    {
        DeltaLoopFilters = new int[4];
        EffectiveLoopFilterLevels = new int[4];
        MatrixLevel = Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
    }

    internal Av1BlockModeSyntax CreateModeSyntax()
    {
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SegmentId = SegmentId;
        syntax.Skip = SkipSignaled;
        syntax.DeltaQuantizerPresent = DeltaQuantizerPresent;
        syntax.DeltaQuantizer = DeltaQuantizer;
        syntax.DeltaLoopFilterPresent = DeltaLoopFilterPresent;
        syntax.DeltaLoopFilters = (int[])DeltaLoopFilters.Clone();
        return syntax;
    }

    internal Av1TransformBlockEncodeRequest CreateTransformRequest(
        Av1TransformSize transformSize,
        Av1TransformType transformType,
        int plane,
        int bitDepth,
        int dcDelta,
        int acDelta,
        int[] residual)
    {
        Av1TransformBlockEncodeRequest request =
            new Av1TransformBlockEncodeRequest();
        request.TransformSize = transformSize;
        request.TransformType = transformType;
        request.Plane = plane;
        request.BitDepth = bitDepth;
        request.QIndex = EffectiveQIndex;
        request.DcDelta = dcDelta;
        request.AcDelta = acDelta;
        request.Lossless = EffectiveQIndex == 0
            && dcDelta == 0 && acDelta == 0;
        request.UseQuantizerMatrix = !request.Lossless
            && MatrixLevel != Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        request.MatrixLevel = MatrixLevel;
        request.TransformBlockSkipContext =
            (EntropyContexts ?? throw new InvalidOperationException("Invalid internal state.")).TransformBlockSkipContext;
        request.DcSignContext = EntropyContexts.DcSignContext;
        request.Residual = residual;
        return request;
    }
}

internal class Av1EncoderBlockDecisionResult
{
    internal AvifResultCode Code;
    internal Av1SegmentationParameters? Segmentation;
    internal Av1QuantizationParameters? Quantization;
    internal Av1EncoderBlockDecision[] Blocks;

    internal Av1EncoderBlockDecisionResult(AvifResultCode code)
    {
        Code = code;
        Blocks = System.Array.Empty<Av1EncoderBlockDecision>();
    }
}

internal class Av1EncoderSegmentProfile
{
    internal int QuantizerDelta;
    internal int[] LoopFilterDeltas;
    internal bool Skip;

    internal Av1EncoderSegmentProfile()
    {
        LoopFilterDeltas = new int[4];
    }
}

// Encoder-side decisions for AV1 specification sections 5.9.12, 5.9.15,
// 5.11.4, 6.8.13, 6.8.17, and 6.10.5.
internal class Av1EncoderBlockDecisionEngine
{
    internal Av1EncoderBlockDecisionRequest Request;
    internal int[] SegmentIds;
    internal Av1EncoderSegmentProfile[]? Profiles;
    internal bool[] SkipChoices;
    internal int MatrixLevel;
    internal int[] StoredSegmentIds;
    internal bool[] StoredSkips;
    internal bool[] StoredAvailable;
    internal int[] StoredCoefficientLevels;
    internal int[] StoredDcSigns;
    internal int CurrentQIndex;
    internal int[] CurrentLoopFilterDeltas;
    internal int CurrentSuperblockIndex;

    internal Av1EncoderBlockDecisionEngine(
        Av1EncoderBlockDecisionRequest request)
    {
        Request = request;
        SegmentIds = new int[request.Blocks.Length];
        SkipChoices = new bool[request.Blocks.Length];
        int cellCount = request.FrameMiRows * request.FrameMiColumns;
        StoredSegmentIds = new int[cellCount];
        StoredSkips = new bool[cellCount];
        StoredAvailable = new bool[cellCount];
        StoredCoefficientLevels = new int[cellCount];
        StoredDcSigns = new int[cellCount];
        CurrentLoopFilterDeltas = new int[4];
        CurrentQIndex = request.BaseQIndex;
        CurrentSuperblockIndex = -1;
    }

    internal static Av1EncoderBlockDecisionResult Decide(
        Av1EncoderBlockDecisionRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return new Av1EncoderBlockDecisionResult(validation);
        }

        Av1EncoderBlockDecisionEngine engine =
            new Av1EncoderBlockDecisionEngine(request);
        engine.SelectInitialSkipChoices();
        engine.MatrixLevel = engine.SelectMatrixLevel();
        engine.SelectFinalSkipChoices();
        engine.Profiles = engine.CreateSegmentProfiles();
        Av1EncoderBlockDecisionResult result =
            new Av1EncoderBlockDecisionResult(AvifResultCode.Success);
        result.Segmentation = engine.CreateSegmentation();
        result.Quantization = engine.CreateQuantization();
        result.Blocks = engine.CreateBlockDecisions();
        return result;
    }

    void SelectInitialSkipChoices()
    {
        int index = 0;
        while (index < Request.Blocks.Length)
        {
            Av1EncoderBlockAnalysis block = Request.Blocks[index];
            SkipChoices[index] = IsSkipBetter(
                block,
                block.CodedDistortion,
                block.CodedRate);
            index++;
        }
    }

    void SelectFinalSkipChoices()
    {
        int index = 0;
        while (index < Request.Blocks.Length)
        {
            Av1EncoderBlockAnalysis block = Request.Blocks[index];
            long distortion = GetCodedDistortion(block, MatrixLevel);
            int rate = GetCodedRate(block, MatrixLevel);
            SkipChoices[index] = IsSkipBetter(block, distortion, rate);
            index++;
        }
    }

    bool IsSkipBetter(
        Av1EncoderBlockAnalysis block,
        long codedDistortion,
        int codedRate)
    {
        if (!block.AllowSkip)
        {
            return false;
        }
        long codedScore = GetScore(
            codedDistortion,
            codedRate,
            Request.RateDistortionLambdaQ8);
        long skipScore = GetScore(
            block.SkipDistortion,
            block.SkipRate,
            Request.RateDistortionLambdaQ8);
        if (skipScore != codedScore)
        {
            return skipScore < codedScore;
        }
        if (block.SkipDistortion != codedDistortion)
        {
            return block.SkipDistortion < codedDistortion;
        }
        return block.SkipRate < codedRate;
    }

    int SelectMatrixLevel()
    {
        if (!Request.EnableQuantizerMatrix)
        {
            return Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        }

        int bestLevel = Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        long bestDistortion = GetMatrixTotalDistortion(bestLevel);
        long bestRate = GetMatrixTotalRate(bestLevel);
        long bestScore = GetScore(
            bestDistortion,
            bestRate,
            Request.RateDistortionLambdaQ8);
        int level = 0;
        while (level < Av1SyntaxConstants.NumQuantizerMatrixLevels - 1)
        {
            long distortion = GetMatrixTotalDistortion(level);
            long rate = GetMatrixTotalRate(level);
            long score = GetScore(
                distortion,
                rate,
                Request.RateDistortionLambdaQ8);
            if (score < bestScore
                || score == bestScore && distortion < bestDistortion
                || score == bestScore && distortion == bestDistortion
                    && rate < bestRate)
            {
                bestLevel = level;
                bestDistortion = distortion;
                bestRate = rate;
                bestScore = score;
            }
            level++;
        }
        return bestLevel;
    }

    long GetMatrixTotalDistortion(int level)
    {
        long total = 0;
        int index = 0;
        while (index < Request.Blocks.Length)
        {
            if (!SkipChoices[index])
            {
                total = AddSaturated(
                    total,
                    GetCodedDistortion(Request.Blocks[index], level));
            }
            index++;
        }
        return total;
    }

    long GetMatrixTotalRate(int level)
    {
        long total = 0;
        int index = 0;
        while (index < Request.Blocks.Length)
        {
            if (!SkipChoices[index])
            {
                total = AddSaturated(
                    total,
                    GetCodedRate(Request.Blocks[index], level));
            }
            index++;
        }
        return total;
    }

    Av1EncoderSegmentProfile[] CreateSegmentProfiles()
    {
        if (!Request.EnableSegmentation)
        {
            return new Av1EncoderSegmentProfile[]
            {
                new Av1EncoderSegmentProfile()
            };
        }

        Av1EncoderSegmentProfile[] profiles =
            new Av1EncoderSegmentProfile[Request.MaximumSegments];
        int profileCount = 0;
        int blockIndex = 0;
        while (blockIndex < Request.Blocks.Length)
        {
            Av1EncoderSegmentProfile desired = CreateDesiredProfile(
                Request.Blocks[blockIndex],
                SkipChoices[blockIndex]);
            int profileIndex = FindMatchingProfile(
                profiles,
                profileCount,
                desired);
            if (profileIndex < 0 && profileCount < profiles.Length)
            {
                profileIndex = profileCount;
                profiles[profileCount] = desired;
                profileCount++;
            }
            if (profileIndex < 0)
            {
                profileIndex = FindNearestProfile(
                    profiles,
                    profileCount,
                    desired);
            }
            SegmentIds[blockIndex] = profileIndex;
            blockIndex++;
        }

        Av1EncoderSegmentProfile[] result =
            new Av1EncoderSegmentProfile[profileCount];
        System.Array.Copy(profiles, result, profileCount);
        NormalizeProfileSkip(result);
        result = ConsolidateProfiles(result);
        MoveDefaultProfileFirst(result);
        return result;
    }

    void NormalizeProfileSkip(Av1EncoderSegmentProfile[] profiles)
    {
        int profile = 0;
        while (profile < profiles.Length)
        {
            profiles[profile].Skip = true;
            profile++;
        }
        int block = 0;
        while (block < SegmentIds.Length)
        {
            if (!SkipChoices[block])
            {
                profiles[SegmentIds[block]].Skip = false;
            }
            block++;
        }
    }

    Av1EncoderSegmentProfile[] ConsolidateProfiles(
        Av1EncoderSegmentProfile[] profiles)
    {
        Av1EncoderSegmentProfile[] compact =
            new Av1EncoderSegmentProfile[profiles.Length];
        int[] mapping = new int[profiles.Length];
        int count = 0;
        int profile = 0;
        while (profile < profiles.Length)
        {
            int target = FindMatchingProfile(compact, count, profiles[profile]);
            if (target < 0)
            {
                target = count;
                compact[count] = profiles[profile];
                count++;
            }
            mapping[profile] = target;
            profile++;
        }
        int block = 0;
        while (block < SegmentIds.Length)
        {
            SegmentIds[block] = mapping[SegmentIds[block]];
            block++;
        }
        Av1EncoderSegmentProfile[] result =
            new Av1EncoderSegmentProfile[count];
        System.Array.Copy(compact, result, count);
        return result;
    }

    void MoveDefaultProfileFirst(Av1EncoderSegmentProfile[] profiles)
    {
        int defaultIndex = -1;
        int index = 0;
        while (index < profiles.Length)
        {
            if (IsDefaultProfile(profiles[index]))
            {
                defaultIndex = index;
                break;
            }
            index++;
        }
        if (defaultIndex <= 0)
        {
            return;
        }
        Av1EncoderSegmentProfile temporary = profiles[0];
        profiles[0] = profiles[defaultIndex];
        profiles[defaultIndex] = temporary;
        int block = 0;
        while (block < SegmentIds.Length)
        {
            if (SegmentIds[block] == 0)
            {
                SegmentIds[block] = defaultIndex;
            }
            else if (SegmentIds[block] == defaultIndex)
            {
                SegmentIds[block] = 0;
            }
            block++;
        }
    }

    static bool IsDefaultProfile(Av1EncoderSegmentProfile profile)
    {
        if (profile.QuantizerDelta != 0 || profile.Skip)
        {
            return false;
        }
        int index = 0;
        while (index < profile.LoopFilterDeltas.Length)
        {
            if (profile.LoopFilterDeltas[index] != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    Av1EncoderSegmentProfile CreateDesiredProfile(
        Av1EncoderBlockAnalysis block,
        bool skip)
    {
        Av1EncoderSegmentProfile profile = new Av1EncoderSegmentProfile();
        profile.QuantizerDelta = Clip(
            -255,
            255,
            block.TargetQIndex - Request.BaseQIndex);
        int index = 0;
        while (index < 4)
        {
            profile.LoopFilterDeltas[index] = Clip(
                -63,
                63,
                block.TargetLoopFilterLevels[index]
                    - Request.BaseLoopFilterLevels[index]);
            index++;
        }
        profile.Skip = skip;
        return profile;
    }

    static int FindMatchingProfile(
        Av1EncoderSegmentProfile[] profiles,
        int count,
        Av1EncoderSegmentProfile desired)
    {
        int index = 0;
        while (index < count)
        {
            if (ProfilesEqual(profiles[index], desired))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static int FindNearestProfile(
        Av1EncoderSegmentProfile[] profiles,
        int count,
        Av1EncoderSegmentProfile desired)
    {
        int bestIndex = 0;
        int bestDistance = GetProfileDistance(profiles[0], desired);
        int index = 1;
        while (index < count)
        {
            int distance = GetProfileDistance(profiles[index], desired);
            if (distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
            }
            index++;
        }
        return bestIndex;
    }

    Av1SegmentationParameters CreateSegmentation()
    {
        Av1SegmentationParameters segmentation =
            new Av1SegmentationParameters();
        segmentation.FeatureEnabled =
            new bool[Av1SyntaxConstants.MaxSegments][];
        segmentation.FeatureData =
            new int[Av1SyntaxConstants.MaxSegments][];
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            segmentation.FeatureEnabled[segment] =
                new bool[Av1SyntaxConstants.SegmentFeatureCount];
            segmentation.FeatureData[segment] =
                new int[Av1SyntaxConstants.SegmentFeatureCount];
            if (segment < (Profiles ?? throw new InvalidOperationException("Invalid internal state.")).Length)
            {
                SetSegmentFeatures(segmentation, segment, Profiles[segment]);
            }
            segment++;
        }

        segmentation.Enabled = HasSegmentFeatures(segmentation);
        segmentation.UpdateMap = segmentation.Enabled;
        segmentation.UpdateData = segmentation.Enabled;
        segmentation.TemporalUpdate = false;
        segmentation.LastActiveSegmentId = GetLastActiveSegment(segmentation);
        segmentation.SegmentIdPreSkip = HasSkipSegment();
        if (!segmentation.Enabled)
        {
            int index = 0;
            while (index < SegmentIds.Length)
            {
                SegmentIds[index] = 0;
                index++;
            }
        }
        return segmentation;
    }

    static int GetLastActiveSegment(
        Av1SegmentationParameters segmentation)
    {
        int last = 0;
        int segment = 0;
        while (segment < (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            int feature = 0;
            while (feature < segmentation.FeatureEnabled[segment].Length)
            {
                if (segmentation.FeatureEnabled[segment][feature])
                {
                    last = segment;
                }
                feature++;
            }
            segment++;
        }
        return last;
    }

    static void SetSegmentFeatures(
        Av1SegmentationParameters segmentation,
        int segment,
        Av1EncoderSegmentProfile profile)
    {
        if (profile.QuantizerDelta != 0)
        {
            int feature = (int)Av1SegmentFeature.AlternateQuantizer;
            (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] = true;
            (segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] = profile.QuantizerDelta;
        }
        int index = 0;
        while (index < 4)
        {
            if (profile.LoopFilterDeltas[index] != 0)
            {
                int feature =
                    (int)Av1SegmentFeature.AlternateLoopFilterYVertical + index;
                (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] = true;
                (segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] =
                    profile.LoopFilterDeltas[index];
            }
            index++;
        }
        if (profile.Skip)
        {
            (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment]
                [(int)Av1SegmentFeature.Skip] = true;
        }
    }

    static bool HasSegmentFeatures(Av1SegmentationParameters segmentation)
    {
        int segment = 0;
        while (segment < (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            int feature = 0;
            while (feature < segmentation.FeatureEnabled[segment].Length)
            {
                if (segmentation.FeatureEnabled[segment][feature])
                {
                    return true;
                }
                feature++;
            }
            segment++;
        }
        return false;
    }

    bool HasSkipSegment()
    {
        int index = 0;
        while (index < (Profiles ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (Profiles[index].Skip)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    Av1QuantizationParameters CreateQuantization()
    {
        Av1QuantizationParameters quantization =
            new Av1QuantizationParameters();
        quantization.BaseQIndex = Request.BaseQIndex;
        quantization.DeltaQPresent = Request.EnableDeltaQuantizer;
        quantization.DeltaQResolution = Request.DeltaQuantizerResolution;
        quantization.DeltaLoopFilterPresent = Request.EnableDeltaLoopFilter;
        quantization.DeltaLoopFilterResolution =
            Request.DeltaLoopFilterResolution;
        quantization.DeltaLoopFilterMulti = Request.DeltaLoopFilterMulti;
        quantization.UsesQuantizerMatrix = MatrixLevel
            != Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        quantization.MatrixLevelY = MatrixLevel;
        quantization.MatrixLevelU = MatrixLevel;
        quantization.MatrixLevelV = MatrixLevel;
        return quantization;
    }

    Av1EncoderBlockDecision[] CreateBlockDecisions()
    {
        Av1EncoderBlockDecision[] decisions =
            new Av1EncoderBlockDecision[Request.Blocks.Length];
        int index = 0;
        while (index < decisions.Length)
        {
            Av1EncoderBlockAnalysis analysis = Request.Blocks[index];
            Av1EncoderBlockDecision decision = new Av1EncoderBlockDecision();
            decision.Analysis = analysis;
            decision.SegmentId = SegmentIds[index];
            decision.SegmentForcesSkip = (Profiles ?? throw new InvalidOperationException("Invalid internal state."))[decision.SegmentId].Skip;
            decision.Skip = SkipChoices[index];
            decision.SkipSignaled = decision.Skip && !decision.SegmentForcesSkip;
            decision.MatrixLevel = MatrixLevel;
            decision.EntropyContexts = GetEntropyContexts(analysis);
            ApplyDeltas(decision);
            SetEffectiveValues(decision);
            SetRateDistortion(decision);
            StoreContexts(decision);
            decisions[index] = decision;
            index++;
        }
        return decisions;
    }

    void ApplyDeltas(Av1EncoderBlockDecision decision)
    {
        Av1EncoderBlockAnalysis? block = decision.Analysis;
        if ((block ?? throw new InvalidOperationException("Invalid internal state.")).SuperblockIndex == CurrentSuperblockIndex)
        {
            return;
        }
        CurrentSuperblockIndex = block.SuperblockIndex;
        bool wholeSuperblockSkipped = decision.Skip
            && (block.BlockSize == Av1BlockSize.Block64x64
                || block.BlockSize == Av1BlockSize.Block128x128);
        if (!Request.EnableDeltaQuantizer || wholeSuperblockSkipped)
        {
            return;
        }

        int segmentDelta = (Profiles ?? throw new InvalidOperationException("Invalid internal state."))[decision.SegmentId].QuantizerDelta;
        int desiredCurrent = Clip(
            1,
            Av1SyntaxConstants.MaxQuantizerIndex,
            block.TargetQIndex - segmentDelta);
        decision.DeltaQuantizerPresent = true;
        decision.DeltaQuantizer = QuantizeDelta(
            desiredCurrent - CurrentQIndex,
            Request.DeltaQuantizerResolution);
        CurrentQIndex = Clip(
            1,
            Av1SyntaxConstants.MaxQuantizerIndex,
            CurrentQIndex + decision.DeltaQuantizer);
        if (!Request.EnableDeltaLoopFilter)
        {
            return;
        }

        decision.DeltaLoopFilterPresent = true;
        int count = Request.DeltaLoopFilterMulti ? 4 : 1;
        int index = 0;
        while (index < count)
        {
            int segmentFilterDelta =
                Profiles[decision.SegmentId].LoopFilterDeltas[index];
            int desired = Clip(
                -63,
                63,
                block.TargetLoopFilterLevels[index]
                    - Request.BaseLoopFilterLevels[index]
                    - segmentFilterDelta);
            int delta = QuantizeDelta(
                desired - CurrentLoopFilterDeltas[index],
                Request.DeltaLoopFilterResolution);
            decision.DeltaLoopFilters[index] = delta;
            CurrentLoopFilterDeltas[index] = Clip(
                -63,
                63,
                CurrentLoopFilterDeltas[index] + delta);
            index++;
        }
    }

    void SetEffectiveValues(Av1EncoderBlockDecision decision)
    {
        Av1EncoderSegmentProfile profile = (Profiles ?? throw new InvalidOperationException("Invalid internal state."))[decision.SegmentId];
        decision.CurrentQIndex = CurrentQIndex;
        decision.EffectiveQIndex = Clip(
            0,
            Av1SyntaxConstants.MaxQuantizerIndex,
            CurrentQIndex + profile.QuantizerDelta);
        int index = 0;
        while (index < 4)
        {
            decision.EffectiveLoopFilterLevels[index] = Clip(
                0,
                Av1SyntaxConstants.MaxLoopFilter,
                Request.BaseLoopFilterLevels[index]
                    + CurrentLoopFilterDeltas[index]
                    + profile.LoopFilterDeltas[index]);
            index++;
        }
    }

    void SetRateDistortion(Av1EncoderBlockDecision decision)
    {
        if (decision.Skip)
        {
            decision.Distortion = (decision.Analysis ?? throw new InvalidOperationException("Invalid internal state.")).SkipDistortion;
            decision.EstimatedRate = decision.Analysis.SkipRate;
        }
        else
        {
            decision.Distortion = GetCodedDistortion(
                decision.Analysis,
                MatrixLevel);
            decision.EstimatedRate = GetCodedRate(
                decision.Analysis,
                MatrixLevel);
        }
        decision.RateDistortionScore = GetScore(
            decision.Distortion,
            decision.EstimatedRate,
            Request.RateDistortionLambdaQ8);
    }

    Av1EncoderBlockEntropyContexts GetEntropyContexts(
        Av1EncoderBlockAnalysis block)
    {
        Av1EncoderBlockEntropyContexts contexts =
            new Av1EncoderBlockEntropyContexts();
        int aboveIndex = GetCellIndex(block.MiRow - 1, block.MiColumn);
        int leftIndex = GetCellIndex(block.MiRow, block.MiColumn - 1);
        int upperLeftIndex = GetCellIndex(
            block.MiRow - 1,
            block.MiColumn - 1);
        bool aboveAvailable = aboveIndex >= 0 && StoredAvailable[aboveIndex];
        bool leftAvailable = leftIndex >= 0 && StoredAvailable[leftIndex];
        contexts.SkipContext = BooleanValue(
            aboveAvailable && StoredSkips[aboveIndex])
            + BooleanValue(leftAvailable && StoredSkips[leftIndex]);
        int aboveSegment = aboveAvailable ? StoredSegmentIds[aboveIndex] : 0;
        int leftSegment = leftAvailable ? StoredSegmentIds[leftIndex] : 0;
        contexts.PredictedSegmentId = PredictSegmentId(
            aboveAvailable,
            leftAvailable,
            aboveSegment,
            leftSegment,
            upperLeftIndex);
        contexts.SegmentIdContext = aboveAvailable && leftAvailable
            ? (aboveSegment == leftSegment ? 2 : 1)
            : (aboveAvailable || leftAvailable ? 1 : 0);
        contexts.SegmentPredictedContext = 0;
        int level = (aboveAvailable ? StoredCoefficientLevels[aboveIndex] : 0)
            + (leftAvailable ? StoredCoefficientLevels[leftIndex] : 0);
        contexts.TransformBlockSkipContext = Clip(
            0,
            Av1SyntaxConstants.TransformBlockSkipContexts - 1,
            level);
        int sign = (aboveAvailable ? StoredDcSigns[aboveIndex] : 0)
            + (leftAvailable ? StoredDcSigns[leftIndex] : 0);
        contexts.DcSignContext = sign < 0 ? 1 : (sign > 0 ? 2 : 0);
        return contexts;
    }

    int PredictSegmentId(
        bool aboveAvailable,
        bool leftAvailable,
        int aboveSegment,
        int leftSegment,
        int upperLeftIndex)
    {
        if (!aboveAvailable)
        {
            return leftAvailable ? leftSegment : 0;
        }
        if (!leftAvailable)
        {
            return aboveSegment;
        }
        return upperLeftIndex >= 0 && StoredAvailable[upperLeftIndex]
            && StoredSegmentIds[upperLeftIndex] == aboveSegment
            ? aboveSegment
            : leftSegment;
    }

    void StoreContexts(Av1EncoderBlockDecision decision)
    {
        Av1EncoderBlockAnalysis? block = decision.Analysis;
        int width = Av1ConversionTables.GetBlockMiWidth((block ?? throw new InvalidOperationException("Invalid internal state.")).BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(block.BlockSize);
        int rowEnd = Minimum(Request.FrameMiRows, block.MiRow + height);
        int columnEnd = Minimum(Request.FrameMiColumns, block.MiColumn + width);
        int row = block.MiRow;
        while (row < rowEnd)
        {
            int column = block.MiColumn;
            while (column < columnEnd)
            {
                int index = row * Request.FrameMiColumns + column;
                StoredSegmentIds[index] = decision.SegmentId;
                StoredSkips[index] = decision.Skip;
                StoredAvailable[index] = true;
                StoredCoefficientLevels[index] = decision.Skip
                    ? 0
                    : block.CoefficientLevel;
                StoredDcSigns[index] = decision.Skip ? 0 : block.DcSign;
                column++;
            }
            row++;
        }
    }

    int GetCellIndex(int row, int column)
    {
        if (row < 0 || row >= Request.FrameMiRows
            || column < 0 || column >= Request.FrameMiColumns)
        {
            return -1;
        }
        return row * Request.FrameMiColumns + column;
    }

    static bool ProfilesEqual(
        Av1EncoderSegmentProfile first,
        Av1EncoderSegmentProfile second)
    {
        if (first.QuantizerDelta != second.QuantizerDelta
            || first.Skip != second.Skip)
        {
            return false;
        }
        int index = 0;
        while (index < 4)
        {
            if (first.LoopFilterDeltas[index]
                != second.LoopFilterDeltas[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static int GetProfileDistance(
        Av1EncoderSegmentProfile first,
        Av1EncoderSegmentProfile second)
    {
        int distance = Absolute(first.QuantizerDelta - second.QuantizerDelta);
        int index = 0;
        while (index < 4)
        {
            distance += Absolute(
                first.LoopFilterDeltas[index]
                    - second.LoopFilterDeltas[index]);
            index++;
        }
        return first.Skip == second.Skip ? distance : distance + 1024;
    }

    static long GetCodedDistortion(
        Av1EncoderBlockAnalysis? block,
        int matrixLevel)
    {
        return (block ?? throw new InvalidOperationException("Invalid internal state.")).MatrixDistortions.Length
            == Av1SyntaxConstants.NumQuantizerMatrixLevels
            ? block.MatrixDistortions[matrixLevel]
            : block.CodedDistortion;
    }

    static int GetCodedRate(
        Av1EncoderBlockAnalysis? block,
        int matrixLevel)
    {
        return (block ?? throw new InvalidOperationException("Invalid internal state.")).MatrixRates.Length
            == Av1SyntaxConstants.NumQuantizerMatrixLevels
            ? block.MatrixRates[matrixLevel]
            : block.CodedRate;
    }

    static int QuantizeDelta(int delta, int resolution)
    {
        int step = 1 << resolution;
        int magnitude = Absolute(delta);
        int quantized = (magnitude + (step >> 1)) / step * step;
        return delta < 0 ? -quantized : quantized;
    }

    static long GetScore(long distortion, long rate, int lambdaQ8)
    {
        if (lambdaQ8 == 0 || rate == 0)
        {
            return distortion;
        }
        if (rate > (long.MaxValue - 128) / lambdaQ8)
        {
            return long.MaxValue;
        }
        return AddSaturated(
            distortion,
            (rate * lambdaQ8 + 128) >> 8);
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue
            : first + second;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int BooleanValue(bool value)
    {
        return value ? 1 : 0;
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : value;
    }

    static AvifResultCode Validate(Av1EncoderBlockDecisionRequest request)
    {
        if (request == null || request.Blocks == null
            || request.BaseLoopFilterLevels == null
            || request.BaseLoopFilterLevels.Length != 4
            || request.FrameMiRows <= 0 || request.FrameMiColumns <= 0
            || (long)request.FrameMiRows * request.FrameMiColumns > int.MaxValue
            || request.BaseQIndex < 0
            || request.BaseQIndex > Av1SyntaxConstants.MaxQuantizerIndex
            || request.RateDistortionLambdaQ8 < 0
            || request.MaximumSegments < 1
            || request.MaximumSegments > Av1SyntaxConstants.MaxSegments
            || request.DeltaQuantizerResolution < 0
            || request.DeltaQuantizerResolution > 3
            || request.DeltaLoopFilterResolution < 0
            || request.DeltaLoopFilterResolution > 3
            || request.EnableDeltaLoopFilter && !request.EnableDeltaQuantizer
            || request.EnableDeltaQuantizer && request.BaseQIndex == 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        int filter = 0;
        while (filter < 4)
        {
            if (request.BaseLoopFilterLevels[filter] < 0
                || request.BaseLoopFilterLevels[filter]
                    > Av1SyntaxConstants.MaxLoopFilter)
            {
                return AvifResultCode.InvalidArgument;
            }
            filter++;
        }
        return ValidateBlocks(request);
    }

    static AvifResultCode ValidateBlocks(
        Av1EncoderBlockDecisionRequest request)
    {
        bool[] occupied = new bool[request.FrameMiRows * request.FrameMiColumns];
        int previousRow = -1;
        int previousColumn = -1;
        int previousSuperblock = -1;
        int index = 0;
        while (index < request.Blocks.Length)
        {
            Av1EncoderBlockAnalysis block = request.Blocks[index];
            if (!ValidateBlock(request, block)
                || block.MiRow < previousRow
                || block.MiRow == previousRow && block.MiColumn <= previousColumn
                || block.SuperblockIndex < previousSuperblock
                || HasOverlap(request, block, occupied))
            {
                return AvifResultCode.InvalidArgument;
            }
            StoreOccupied(request, block, occupied);
            previousRow = block.MiRow;
            previousColumn = block.MiColumn;
            previousSuperblock = block.SuperblockIndex;
            index++;
        }
        return AvifResultCode.Success;
    }

    static bool ValidateBlock(
        Av1EncoderBlockDecisionRequest request,
        Av1EncoderBlockAnalysis block)
    {
        if (block == null || block.TargetLoopFilterLevels == null
            || block.TargetLoopFilterLevels.Length != 4
            || block.MatrixDistortions == null || block.MatrixRates == null
            || block.MatrixDistortions.Length != 0
                && block.MatrixDistortions.Length
                    != Av1SyntaxConstants.NumQuantizerMatrixLevels
            || block.MatrixRates.Length != 0
                && block.MatrixRates.Length
                    != Av1SyntaxConstants.NumQuantizerMatrixLevels
            || block.MatrixDistortions.Length != block.MatrixRates.Length
            || block.MiRow < 0 || block.MiRow >= request.FrameMiRows
            || block.MiColumn < 0 || block.MiColumn >= request.FrameMiColumns
            || block.SuperblockIndex < 0
            || Av1ConversionTables.GetBlockMiWidth(block.BlockSize) <= 0
            || Av1ConversionTables.GetBlockMiHeight(block.BlockSize) <= 0
            || block.TargetQIndex < 0
            || block.TargetQIndex > Av1SyntaxConstants.MaxQuantizerIndex
            || block.CodedDistortion < 0 || block.CodedRate < 0
            || block.SkipDistortion < 0 || block.SkipRate < 0
            || block.CoefficientLevel < 0 || block.CoefficientLevel > 6
            || block.DcSign < -1 || block.DcSign > 1)
        {
            return false;
        }
        int filter = 0;
        while (filter < 4)
        {
            if (block.TargetLoopFilterLevels[filter] < 0
                || block.TargetLoopFilterLevels[filter]
                    > Av1SyntaxConstants.MaxLoopFilter)
            {
                return false;
            }
            filter++;
        }
        int level = 0;
        while (level < block.MatrixDistortions.Length)
        {
            if (block.MatrixDistortions[level] < 0
                || block.MatrixRates[level] < 0)
            {
                return false;
            }
            level++;
        }
        return true;
    }

    static bool HasOverlap(
        Av1EncoderBlockDecisionRequest request,
        Av1EncoderBlockAnalysis block,
        bool[] occupied)
    {
        int width = Av1ConversionTables.GetBlockMiWidth(block.BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(block.BlockSize);
        int rowEnd = Minimum(request.FrameMiRows, block.MiRow + height);
        int columnEnd = Minimum(request.FrameMiColumns, block.MiColumn + width);
        int row = block.MiRow;
        while (row < rowEnd)
        {
            int column = block.MiColumn;
            while (column < columnEnd)
            {
                if (occupied[row * request.FrameMiColumns + column])
                {
                    return true;
                }
                column++;
            }
            row++;
        }
        return false;
    }

    static void StoreOccupied(
        Av1EncoderBlockDecisionRequest request,
        Av1EncoderBlockAnalysis block,
        bool[] occupied)
    {
        int width = Av1ConversionTables.GetBlockMiWidth(block.BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(block.BlockSize);
        int rowEnd = Minimum(request.FrameMiRows, block.MiRow + height);
        int columnEnd = Minimum(request.FrameMiColumns, block.MiColumn + width);
        int row = block.MiRow;
        while (row < rowEnd)
        {
            int column = block.MiColumn;
            while (column < columnEnd)
            {
                occupied[row * request.FrameMiColumns + column] = true;
                column++;
            }
            row++;
        }
    }
}
