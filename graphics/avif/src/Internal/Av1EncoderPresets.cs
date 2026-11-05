namespace avif;

internal enum Av1EncoderKernelBackend
{
    Automatic = 0,
    Scalar = 1,
    Intrinsic = 2
}

internal class Av1EncoderKernelResult
{
    internal AvifResultCode Code;
    internal long Value;
    internal Av1EncoderKernelBackend Backend;

    internal Av1EncoderKernelResult(AvifResultCode code)
    {
        Code = code;
        Backend = Av1EncoderKernelBackend.Scalar;
    }
}

internal class Av1EncoderPresetDecision
{
    internal AvifResultCode Code;
    internal Av1PartitionSearchEffort PartitionEffort;
    internal bool EnablePartitionPruning;
    internal int MotionSearchRadius;
    internal bool EnableSubpixelSearch;
    internal bool EnableCompound;
    internal bool EnableInterIntra;
    internal bool EnableGlobalMotion;
    internal bool EnableWarpedMotion;
    internal bool EnableSegmentation;
    internal int MaximumSegments;
    internal bool EnableDeltaQuantizer;
    internal bool EnableDeltaLoopFilter;
    internal bool EnableQuantizerMatrix;
    internal int FilterCandidateLimit;

    internal Av1EncoderPresetDecision(AvifResultCode code)
    {
        Code = code;
    }

    internal void ApplyTo(Av1PartitionSearchRequest request)
    {
        if (Code != AvifResultCode.Success || request == null)
        {
            return;
        }
        request.Effort = PartitionEffort;
        request.EnablePruning = EnablePartitionPruning;
    }

    internal void ApplyToInterSearch(Av1EncoderInterSearchRequest request)
    {
        if (Code != AvifResultCode.Success || request == null)
        {
            return;
        }
        request.MaximumIntegerSearchRadius = MotionSearchRadius;
        request.EnableSubpixelSearch = EnableSubpixelSearch;
        request.EnableCompound = EnableCompound;
        request.EnableInterIntra = EnableInterIntra;
        request.EnableGlobalMotion = EnableGlobalMotion;
        request.EnableWarpedMotion = EnableWarpedMotion;
    }

    internal void ApplyToBlockDecision(Av1EncoderBlockDecisionRequest request)
    {
        if (Code != AvifResultCode.Success || request == null)
        {
            return;
        }
        request.EnableSegmentation = EnableSegmentation;
        request.MaximumSegments = MaximumSegments;
        request.EnableDeltaQuantizer = EnableDeltaQuantizer;
        request.EnableDeltaLoopFilter = EnableDeltaLoopFilter;
        request.EnableQuantizerMatrix = EnableQuantizerMatrix;
    }
}

internal static class Av1EncoderPresetResolver
{
    internal static Av1EncoderPresetDecision Resolve(
        Av1EncoderOptions options)
    {
        if (options == null || options.Preset < AvifEncoderPreset.Fastest
            || options.Preset > AvifEncoderPreset.Best)
        {
            return new Av1EncoderPresetDecision(
                AvifResultCode.InvalidArgument);
        }
        Av1EncoderPresetDecision decision =
            new Av1EncoderPresetDecision(AvifResultCode.Success);
        switch (options.Preset)
        {
            case AvifEncoderPreset.Fastest:
                Configure(decision, Av1PartitionSearchEffort.Fast, true,
                    1, false, false, false, false, false,
                    false, 1, false, false, false, 1);
                break;
            case AvifEncoderPreset.Fast:
                Configure(decision, Av1PartitionSearchEffort.Fast, true,
                    2, true, true, false, false, false,
                    false, 1, false, false, false, 2);
                break;
            case AvifEncoderPreset.Balanced:
                Configure(decision, Av1PartitionSearchEffort.Balanced, true,
                    4, true, true, true, true, false,
                    true, 4, true, true, false, 4);
                break;
            case AvifEncoderPreset.Quality:
                Configure(decision, Av1PartitionSearchEffort.Thorough, true,
                    8, true, true, true, true, true,
                    true, 8, true, true, true, 8);
                break;
            default:
                Configure(decision, Av1PartitionSearchEffort.Exhaustive, false,
                    16, true, true, true, true, true,
                    true, 8, true, true, true, 16);
                break;
        }
        decision.EnableGlobalMotion = decision.EnableGlobalMotion
            && options.EnableGlobalMotion;
        decision.EnableWarpedMotion = decision.EnableWarpedMotion
            && options.EnableWarpedMotion;
        return decision;
    }

    static void Configure(
        Av1EncoderPresetDecision decision,
        Av1PartitionSearchEffort partitionEffort,
        bool partitionPruning,
        int motionRadius,
        bool subpixel,
        bool compound,
        bool interIntra,
        bool globalMotion,
        bool warpedMotion,
        bool segmentation,
        int maximumSegments,
        bool deltaQuantizer,
        bool deltaLoopFilter,
        bool quantizerMatrix,
        int filterCandidates)
    {
        decision.PartitionEffort = partitionEffort;
        decision.EnablePartitionPruning = partitionPruning;
        decision.MotionSearchRadius = motionRadius;
        decision.EnableSubpixelSearch = subpixel;
        decision.EnableCompound = compound;
        decision.EnableInterIntra = interIntra;
        decision.EnableGlobalMotion = globalMotion;
        decision.EnableWarpedMotion = warpedMotion;
        decision.EnableSegmentation = segmentation;
        decision.MaximumSegments = maximumSegments;
        decision.EnableDeltaQuantizer = deltaQuantizer;
        decision.EnableDeltaLoopFilter = deltaLoopFilter;
        decision.EnableQuantizerMatrix = quantizerMatrix;
        decision.FilterCandidateLimit = filterCandidates;
    }
}

internal static class Av1EncoderKernelDispatcher
{
    internal static Av1EncoderKernelResult SumSquaredError(
        int[] first,
        int[] second,
        Av1EncoderKernelBackend requestedBackend)
    {
        Av1EncoderKernelResult result = Validate(
            first,
            second,
            requestedBackend);
        if (result.Code != AvifResultCode.Success)
        {
            return result;
        }
        result.Backend = ResolveBackend(requestedBackend);
        result.Value = result.Backend == Av1EncoderKernelBackend.Intrinsic
            ? IntrinsicSumSquaredError(first, second)
            : ScalarSumSquaredError(first, second);
        return result;
    }

    internal static Av1EncoderKernelBackend ResolveBackend(
        Av1EncoderKernelBackend requestedBackend)
    {
        if (requestedBackend == Av1EncoderKernelBackend.Scalar)
        {
            return Av1EncoderKernelBackend.Scalar;
        }
        return System.Numerics.Vector.IsHardwareAccelerated
            ? Av1EncoderKernelBackend.Intrinsic
            : Av1EncoderKernelBackend.Scalar;
    }

    static long ScalarSumSquaredError(int[] first, int[] second)
    {
        long total = 0;
        int index = 0;
        while (index < first.Length)
        {
            long difference = first[index] - second[index];
            total = AddSaturated(total, difference * difference);
            index++;
        }
        return total;
    }

    static long IntrinsicSumSquaredError(int[] first, int[] second)
    {
        long total = 0;
        int width = System.Numerics.Vector<int>.Count;
        int index = 0;
        while (index <= first.Length - width)
        {
            System.Numerics.Vector<int> firstVector =
                new System.Numerics.Vector<int>(first, index);
            System.Numerics.Vector<int> secondVector =
                new System.Numerics.Vector<int>(second, index);
            System.Numerics.Vector<int> difference =
                firstVector - secondVector;
            int lane = 0;
            while (lane < width)
            {
                long value = difference[lane];
                total = AddSaturated(total, value * value);
                lane++;
            }
            index += width;
        }
        while (index < first.Length)
        {
            long difference = first[index] - second[index];
            total = AddSaturated(total, difference * difference);
            index++;
        }
        return total;
    }

    static Av1EncoderKernelResult Validate(
        int[] first,
        int[] second,
        Av1EncoderKernelBackend requestedBackend)
    {
        if (first == null || second == null || first.Length == 0
            || first.Length != second.Length
            || requestedBackend < Av1EncoderKernelBackend.Automatic
            || requestedBackend > Av1EncoderKernelBackend.Intrinsic)
        {
            return new Av1EncoderKernelResult(
                AvifResultCode.InvalidArgument);
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] < 0 || first[index] > 4095
                || second[index] < 0 || second[index] > 4095)
            {
                return new Av1EncoderKernelResult(
                    AvifResultCode.InvalidArgument);
            }
            index++;
        }
        return new Av1EncoderKernelResult(AvifResultCode.Success);
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue
            : first + second;
    }
}
