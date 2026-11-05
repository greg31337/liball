using System;

namespace avif;

internal class Av1EncoderFilterCandidate
{
    internal Av1FrameSize? Size;
    internal PlaneBuffer[] ReconstructedPlanes;
    internal Av1ModeInfo[] Modes;
    internal Av1TransformSize[][] TransformSizes;
    internal Av1QuantizationParameters Quantization;
    internal Av1SegmentationParameters? Segmentation;
    internal Av1LoopFilterParameters LoopFilter;
    internal Av1CdefParameters Cdef;
    internal int[] CdefStrengthIndexes;
    internal int[] CdefRegionIds;
    internal Av1LoopRestorationParameters LoopRestoration;
    internal Av1RestorationUnit[][] RestorationUnits;
    internal int EstimatedRate;
    internal int Order;
    internal object? Value;

    internal Av1EncoderFilterCandidate()
    {
        ReconstructedPlanes = System.Array.Empty<PlaneBuffer>();
        Modes = System.Array.Empty<Av1ModeInfo>();
        TransformSizes = System.Array.Empty<Av1TransformSize[]>();
        Quantization = new Av1QuantizationParameters();
        LoopFilter = new Av1LoopFilterParameters();
        LoopFilter.Levels = new int[4];
        LoopFilter.ReferenceDeltas = new int[] { 1, 0, 0, 0, -1, 0, -1, -1 };
        LoopFilter.ModeDeltas = new int[2];
        Cdef = new Av1CdefParameters();
        Cdef.Damping = 3;
        Cdef.YPrimaryStrengths = new int[] { 0 };
        Cdef.YSecondaryStrengths = new int[] { 0 };
        Cdef.UvPrimaryStrengths = new int[] { 0 };
        Cdef.UvSecondaryStrengths = new int[] { 0 };
        CdefStrengthIndexes = System.Array.Empty<int>();
        CdefRegionIds = System.Array.Empty<int>();
        LoopRestoration = new Av1LoopRestorationParameters();
        LoopRestoration.Types = System.Array.Empty<Av1RestorationType>();
        LoopRestoration.UnitSizes = System.Array.Empty<int>();
        RestorationUnits = System.Array.Empty<Av1RestorationUnit[]>();
    }
}

internal class Av1EncoderFilterDecisionRequest
{
    internal PlaneBuffer[] TargetPlanes;
    internal Av1EncoderFilterCandidate[] Candidates;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int RateDistortionLambdaQ8;
    internal long MaximumTemporaryBytes;

    internal Av1EncoderFilterDecisionRequest()
    {
        TargetPlanes = System.Array.Empty<PlaneBuffer>();
        Candidates = System.Array.Empty<Av1EncoderFilterCandidate>();
        BitDepth = 8;
        MaximumTemporaryBytes = 1L << 30;
    }
}

internal class Av1EncoderFilterDecisionResult
{
    internal AvifResultCode Code;
    internal Av1EncoderFilterCandidate? BestCandidate;
    internal PlaneBuffer[] ReconstructedPlanes;
    internal long Distortion;
    internal int EstimatedRate;
    internal long RateDistortionScore;
    internal int EvaluatedCandidateCount;
    internal bool Closed;

    internal Av1EncoderFilterDecisionResult(AvifResultCode code)
    {
        Code = code;
        ReconstructedPlanes = System.Array.Empty<PlaneBuffer>();
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }
        Av1EncoderFilterDecisionEngine.ClosePlanes(ReconstructedPlanes);
        ReconstructedPlanes = System.Array.Empty<PlaneBuffer>();
        Closed = true;
        return AvifResultCode.Success;
    }
}

internal class Av1EncoderFilterEvaluation
{
    internal AvifResultCode Code;
    internal PlaneBuffer[] Planes;
    internal long Distortion;
    internal int EstimatedRate;
    internal long RateDistortionScore;

    internal Av1EncoderFilterEvaluation(AvifResultCode code)
    {
        Code = code;
        Planes = System.Array.Empty<PlaneBuffer>();
    }
}

// Encoder-side in-loop filter decisions for AV1 specification sections
// 7.14 through 7.17. The decoder filters produce the retained reconstruction.
internal static class Av1EncoderFilterDecisionEngine
{
    internal static Av1EncoderFilterDecisionResult Decide(
        Av1EncoderFilterDecisionRequest request)
    {
        AvifResultCode validation = ValidateRequest(request);
        if (validation != AvifResultCode.Success)
        {
            return new Av1EncoderFilterDecisionResult(validation);
        }

        Av1EncoderFilterDecisionResult result =
            new Av1EncoderFilterDecisionResult(AvifResultCode.EncodeFailed);
        int index = 0;
        while (index < request.Candidates.Length)
        {
            Av1EncoderFilterCandidate candidate = request.Candidates[index];
            Av1EncoderFilterEvaluation evaluation = Evaluate(request, candidate);
            if (evaluation.Code != AvifResultCode.Success)
            {
                ClosePlanes(result.ReconstructedPlanes);
                result.ReconstructedPlanes = System.Array.Empty<PlaneBuffer>();
                result.Code = evaluation.Code;
                return result;
            }
            result.EvaluatedCandidateCount++;
            if (result.BestCandidate == null
                || Compare(evaluation, candidate, result) < 0)
            {
                ClosePlanes(result.ReconstructedPlanes);
                result.BestCandidate = candidate;
                result.ReconstructedPlanes = evaluation.Planes;
                result.Distortion = evaluation.Distortion;
                result.EstimatedRate = evaluation.EstimatedRate;
                result.RateDistortionScore = evaluation.RateDistortionScore;
            }
            else
            {
                ClosePlanes(evaluation.Planes);
            }
            index++;
        }
        if (result.BestCandidate != null)
        {
            result.Code = AvifResultCode.Success;
        }
        return result;
    }

    static Av1EncoderFilterEvaluation Evaluate(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate)
    {
        AvifResultCode validation = ValidateCandidate(request, candidate);
        if (validation != AvifResultCode.Success)
        {
            return new Av1EncoderFilterEvaluation(validation);
        }

        PlaneBuffer[] deblocked = CopyPlanes(candidate.ReconstructedPlanes);
        AvifResultCode state = GetPlaneState(deblocked);
        if (state != AvifResultCode.Success)
        {
            ClosePlanes(deblocked);
            return new Av1EncoderFilterEvaluation(state);
        }
        ParsedAv1FrameHeader header = CreateHeader(candidate);
        state = ApplyDeblock(request, candidate, header, deblocked);
        if (state != AvifResultCode.Success)
        {
            ClosePlanes(deblocked);
            return new Av1EncoderFilterEvaluation(state);
        }

        PlaneBuffer[] cdef = CopyPlanes(deblocked);
        state = GetPlaneState(cdef);
        if (state == AvifResultCode.Success)
        {
            state = ApplyCdef(request, candidate, header, cdef);
        }
        if (state != AvifResultCode.Success)
        {
            ClosePlanes(deblocked);
            ClosePlanes(cdef);
            return new Av1EncoderFilterEvaluation(state);
        }

        PlaneBuffer[] upscaledDeblocked = AllocateUpscaledPlanes(
            request,
            candidate);
        PlaneBuffer[] upscaledCdef = AllocateUpscaledPlanes(request, candidate);
        state = GetPlaneState(upscaledDeblocked);
        if (state == AvifResultCode.Success)
        {
            state = GetPlaneState(upscaledCdef);
        }
        if (state == AvifResultCode.Success)
        {
            state = ApplySuperResolution(
                request,
                candidate,
                deblocked,
                upscaledDeblocked);
        }
        if (state == AvifResultCode.Success)
        {
            state = ApplySuperResolution(
                request,
                candidate,
                cdef,
                upscaledCdef);
        }
        ClosePlanes(deblocked);
        ClosePlanes(cdef);
        if (state != AvifResultCode.Success)
        {
            ClosePlanes(upscaledDeblocked);
            ClosePlanes(upscaledCdef);
            return new Av1EncoderFilterEvaluation(state);
        }

        PlaneBuffer[] restored = AllocateUpscaledPlanes(request, candidate);
        state = GetPlaneState(restored);
        if (state == AvifResultCode.Success)
        {
            state = ApplyRestoration(
                request,
                candidate,
                header,
                upscaledDeblocked,
                upscaledCdef,
                restored);
        }
        ClosePlanes(upscaledDeblocked);
        ClosePlanes(upscaledCdef);
        if (state != AvifResultCode.Success)
        {
            ClosePlanes(restored);
            return new Av1EncoderFilterEvaluation(state);
        }

        Av1EncoderFilterEvaluation evaluation =
            new Av1EncoderFilterEvaluation(AvifResultCode.Success);
        evaluation.Planes = restored;
        evaluation.Distortion = GetDistortion(
            request.TargetPlanes,
            restored,
            candidate.Size,
            request.SubsamplingX,
            request.SubsamplingY);
        evaluation.EstimatedRate = candidate.EstimatedRate;
        evaluation.RateDistortionScore = GetScore(
            evaluation.Distortion,
            evaluation.EstimatedRate,
            request.RateDistortionLambdaQ8);
        return evaluation;
    }

    static ParsedAv1FrameHeader CreateHeader(
        Av1EncoderFilterCandidate candidate)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Size = candidate.Size;
        header.Quantization = candidate.Quantization;
        header.Segmentation = candidate.Segmentation;
        header.LoopFilter = candidate.LoopFilter;
        header.Cdef = candidate.Cdef;
        header.LoopRestoration = candidate.LoopRestoration;
        return header;
    }

    static AvifResultCode ApplyDeblock(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes)
    {
        Av1LoopFilterRequest filter = new Av1LoopFilterRequest();
        filter.Header = header;
        filter.Planes = planes;
        filter.Modes = candidate.Modes;
        filter.TransformSizes = candidate.TransformSizes;
        filter.MiRows = (candidate.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows;
        filter.MiColumns = candidate.Size.MiColumns;
        filter.BitDepth = request.BitDepth;
        filter.SubsamplingX = request.SubsamplingX;
        filter.SubsamplingY = request.SubsamplingY;
        return Av1LoopFilter.Filter(filter);
    }

    static AvifResultCode ApplyCdef(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes)
    {
        bool[] skips = new bool[candidate.Modes.Length];
        int index = 0;
        while (index < skips.Length)
        {
            skips[index] = candidate.Modes[index].Skip;
            index++;
        }
        Av1CdefRequest cdef = new Av1CdefRequest();
        cdef.Header = header;
        cdef.Planes = planes;
        cdef.Skips = skips;
        cdef.StrengthIndexes = candidate.CdefStrengthIndexes;
        cdef.RegionIds = candidate.CdefRegionIds;
        cdef.MiRows = (candidate.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows;
        cdef.MiColumns = candidate.Size.MiColumns;
        cdef.BitDepth = request.BitDepth;
        cdef.SubsamplingX = request.SubsamplingX;
        cdef.SubsamplingY = request.SubsamplingY;
        return Av1CdefFilter.Filter(cdef);
    }

    static AvifResultCode ApplySuperResolution(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate,
        PlaneBuffer[] source,
        PlaneBuffer[] destination)
    {
        Av1SuperResolutionRequest superResolution =
            new Av1SuperResolutionRequest();
        superResolution.Size = candidate.Size;
        superResolution.Source = source;
        superResolution.Destination = destination;
        superResolution.BitDepth = request.BitDepth;
        superResolution.SubsamplingX = request.SubsamplingX;
        superResolution.SubsamplingY = request.SubsamplingY;
        return Av1SuperResolution.Upscale(superResolution);
    }

    static AvifResultCode ApplyRestoration(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] current,
        PlaneBuffer[] cdef,
        PlaneBuffer[] destination)
    {
        Av1LoopRestorationRequest restoration =
            new Av1LoopRestorationRequest();
        restoration.Header = header;
        restoration.CurrentPlanes = current;
        restoration.CdefPlanes = cdef;
        restoration.DestinationPlanes = destination;
        restoration.Units = candidate.RestorationUnits;
        restoration.BitDepth = request.BitDepth;
        restoration.SubsamplingX = request.SubsamplingX;
        restoration.SubsamplingY = request.SubsamplingY;
        return Av1LoopRestoration.Filter(restoration);
    }

    static PlaneBuffer[] CopyPlanes(PlaneBuffer[] source)
    {
        PlaneBuffer[] copies = new PlaneBuffer[source.Length];
        int plane = 0;
        while (plane < source.Length)
        {
            copies[plane] = source[plane].CreateCopy();
            if (copies[plane].GetStateCode() != AvifResultCode.Success)
            {
                return copies;
            }
            plane++;
        }
        return copies;
    }

    static PlaneBuffer[] AllocateUpscaledPlanes(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate)
    {
        PlaneBuffer[] planes =
            new PlaneBuffer[candidate.ReconstructedPlanes.Length];
        int bytesPerSample = request.BitDepth > 8 ? 2 : 1;
        int plane = 0;
        while (plane < planes.Length)
        {
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int width = Round2((candidate.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, subX);
            int height = Round2(candidate.Size.FrameHeight, subY);
            planes[plane] = new PlaneBuffer(
                width,
                height,
                bytesPerSample,
                1,
                request.MaximumTemporaryBytes,
                true);
            if (planes[plane].GetStateCode() != AvifResultCode.Success)
            {
                return planes;
            }
            plane++;
        }
        return planes;
    }

    static AvifResultCode GetPlaneState(PlaneBuffer[] planes)
    {
        if (planes == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        int plane = 0;
        while (plane < planes.Length)
        {
            if (planes[plane] == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode state = planes[plane].GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            plane++;
        }
        return AvifResultCode.Success;
    }

    internal static void ClosePlanes(PlaneBuffer[] planes)
    {
        if (planes == null)
        {
            return;
        }
        int plane = 0;
        while (plane < planes.Length)
        {
            if (planes[plane] != null)
            {
                planes[plane].Close();
            }
            plane++;
        }
    }

    static long GetDistortion(
        PlaneBuffer[] target,
        PlaneBuffer[] reconstructed,
        Av1FrameSize? size,
        bool subsamplingX,
        bool subsamplingY)
    {
        long distortion = 0;
        int plane = 0;
        while (plane < target.Length)
        {
            int subX = plane == 0 || !subsamplingX ? 0 : 1;
            int subY = plane == 0 || !subsamplingY ? 0 : 1;
            int width = Round2((size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, subX);
            int height = Round2(size.FrameHeight, subY);
            int y = 0;
            while (y < height)
            {
                int x = 0;
                while (x < width)
                {
                    long difference = ReadSample(target[plane], x, y)
                        - ReadSample(reconstructed[plane], x, y);
                    long squared = difference * difference;
                    distortion = AddSaturated(distortion, squared);
                    x++;
                }
                y++;
            }
            plane++;
        }
        return distortion;
    }

    static int Compare(
        Av1EncoderFilterEvaluation evaluation,
        Av1EncoderFilterCandidate candidate,
        Av1EncoderFilterDecisionResult best)
    {
        if (evaluation.RateDistortionScore != best.RateDistortionScore)
        {
            return evaluation.RateDistortionScore < best.RateDistortionScore
                ? -1
                : 1;
        }
        if (evaluation.Distortion != best.Distortion)
        {
            return evaluation.Distortion < best.Distortion ? -1 : 1;
        }
        if (evaluation.EstimatedRate != best.EstimatedRate)
        {
            return evaluation.EstimatedRate < best.EstimatedRate ? -1 : 1;
        }
        return candidate.Order < (best.BestCandidate ?? throw new InvalidOperationException("Invalid internal state.")).Order
            ? -1
            : (candidate.Order > best.BestCandidate.Order ? 1 : 0);
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
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

    static int Round2(int value, int shift)
    {
        return (value + (1 << shift) - 1) >> shift;
    }

    static AvifResultCode ValidateRequest(
        Av1EncoderFilterDecisionRequest request)
    {
        if (request == null || request.TargetPlanes == null
            || request.Candidates == null || request.Candidates.Length == 0
            || request.TargetPlanes.Length != 1
                && request.TargetPlanes.Length != 3
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.RateDistortionLambdaQ8 < 0
            || request.MaximumTemporaryBytes <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        return GetPlaneState(request.TargetPlanes);
    }

    static AvifResultCode ValidateCandidate(
        Av1EncoderFilterDecisionRequest request,
        Av1EncoderFilterCandidate candidate)
    {
        if (candidate == null || candidate.Size == null
            || candidate.ReconstructedPlanes == null
            || candidate.ReconstructedPlanes.Length != request.TargetPlanes.Length
            || candidate.Modes == null || candidate.TransformSizes == null
            || candidate.Quantization == null || candidate.LoopFilter == null
            || candidate.Cdef == null || candidate.CdefStrengthIndexes == null
            || candidate.CdefRegionIds == null
            || candidate.LoopRestoration == null
            || candidate.RestorationUnits == null
            || candidate.EstimatedRate < 0
            || candidate.Size.FrameWidth <= 0
            || candidate.Size.UpscaledWidth <= 0
            || candidate.Size.FrameHeight <= 0
            || candidate.Size.UpscaledWidth != request.TargetPlanes[0].Width
            || candidate.Size.FrameHeight != request.TargetPlanes[0].Height
            || candidate.Size.MiRows <= 0 || candidate.Size.MiColumns <= 0
            || candidate.Modes.Length
                != candidate.Size.MiRows * candidate.Size.MiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode state = GetPlaneState(candidate.ReconstructedPlanes);
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        int plane = 0;
        while (plane < request.TargetPlanes.Length)
        {
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int targetWidth = Round2(candidate.Size.UpscaledWidth, subX);
            int sourceWidth = Round2(candidate.Size.FrameWidth, subX);
            int height = Round2(candidate.Size.FrameHeight, subY);
            int bytesPerSample = request.BitDepth > 8 ? 2 : 1;
            if (request.TargetPlanes[plane].Width != targetWidth
                || request.TargetPlanes[plane].Height != height
                || candidate.ReconstructedPlanes[plane].Width < sourceWidth
                || candidate.ReconstructedPlanes[plane].Height < height
                || request.TargetPlanes[plane].BytesPerSample != bytesPerSample
                || candidate.ReconstructedPlanes[plane].BytesPerSample
                    != bytesPerSample)
            {
                return AvifResultCode.InvalidArgument;
            }
            plane++;
        }
        return AvifResultCode.Success;
    }
}
