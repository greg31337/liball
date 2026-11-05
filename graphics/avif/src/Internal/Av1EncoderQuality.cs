namespace avif;

internal class Av1EncodedSizeTrial
{
    internal int Quantizer;
    internal long SizeBytes;

    internal Av1EncodedSizeTrial(int quantizer, long sizeBytes)
    {
        Quantizer = quantizer;
        SizeBytes = sizeBytes;
    }
}

internal class Av1EncoderQualityDecision
{
    internal AvifResultCode Code;
    internal int QIndex;
    internal bool Lossless;
    internal bool NeedsSizeTrial;
    internal bool TargetMet;
    internal long SelectedSizeBytes;

    internal Av1EncoderQualityDecision(AvifResultCode code)
    {
        Code = code;
    }

    internal void ApplyTo(Av1TransformBlockEncodeRequest request)
    {
        if (request == null || Code != AvifResultCode.Success)
        {
            return;
        }
        request.QIndex = QIndex;
        request.Lossless = Lossless;
        if (Lossless)
        {
            request.DcDelta = 0;
            request.AcDelta = 0;
            request.UseQuantizerMatrix = false;
        }
    }
}

internal class AvifEncoderQualityPlan
{
    internal AvifResultCode Code;
    internal Av1EncoderQualityDecision? Color;
    internal Av1EncoderQualityDecision? Alpha;
    internal Av1EncoderQualityDecision? GainMap;
    internal bool HasAlpha;
    internal bool HasGainMap;

    internal AvifEncoderQualityPlan(AvifResultCode code)
    {
        Code = code;
    }
}

internal static class Av1EncoderQualityResolver
{
    const int MaximumSizeTrials = 12;

    internal static Av1EncoderQualityDecision Resolve(
        Av1EncoderOptions options,
        Av1EncodedSizeTrial[] trials)
    {
        if (options == null)
        {
            return new Av1EncoderQualityDecision(
                AvifResultCode.InvalidArgument);
        }
        return Resolve(
            options.Mode,
            options.Quality,
            options.Quantizer,
            options.TargetSizeBytes,
            trials);
    }

    internal static Av1EncoderQualityDecision Resolve(
        AvifEncoderMode mode,
        int quality,
        int quantizer,
        long targetSizeBytes,
        Av1EncodedSizeTrial[] trials)
    {
        switch (mode)
        {
            case AvifEncoderMode.ConstantQuality:
                return ResolveQuality(quality);
            case AvifEncoderMode.Quantizer:
                return ResolveQuantizer(quantizer);
            case AvifEncoderMode.Lossless:
                return CreateFixed(0, true);
            case AvifEncoderMode.TargetSize:
                return ResolveTargetSize(targetSizeBytes, trials);
            default:
                return new Av1EncoderQualityDecision(
                    AvifResultCode.InvalidArgument);
        }
    }

    internal static Av1EncoderQualityDecision ResolveQuality(int quality)
    {
        if (quality < 0 || quality > 100)
        {
            return new Av1EncoderQualityDecision(
                AvifResultCode.InvalidArgument);
        }
        int qIndex = ((100 - quality) * 255 + 50) / 100;
        return CreateFixed(qIndex, qIndex == 0);
    }

    static Av1EncoderQualityDecision ResolveQuantizer(int quantizer)
    {
        if (quantizer < 0
            || quantizer > Av1SyntaxConstants.MaxQuantizerIndex)
        {
            return new Av1EncoderQualityDecision(
                AvifResultCode.InvalidArgument);
        }
        return CreateFixed(quantizer, quantizer == 0);
    }

    static Av1EncoderQualityDecision ResolveTargetSize(
        long targetSizeBytes,
        Av1EncodedSizeTrial[] trials)
    {
        AvifResultCode validation = ValidateTrials(targetSizeBytes, trials);
        if (validation != AvifResultCode.Success)
        {
            return new Av1EncoderQualityDecision(validation);
        }

        if (trials.Length == 0)
        {
            return CreateTrial(128);
        }

        long tolerance = targetSizeBytes / 100;
        if (tolerance < 1)
        {
            tolerance = 1;
        }
        Av1EncodedSizeTrial best = GetBestTrial(trials, targetSizeBytes);
        if (GetDifference(best.SizeBytes, targetSizeBytes) <= tolerance)
        {
            return CreateCompleted(best, true);
        }
        if (trials.Length >= MaximumSizeTrials)
        {
            return CreateCompleted(best, false);
        }

        Av1EncodedSizeTrial? tooLarge = null;
        Av1EncodedSizeTrial? smallEnough = null;
        int index = 0;
        while (index < trials.Length)
        {
            Av1EncodedSizeTrial trial = trials[index];
            if (trial.SizeBytes > targetSizeBytes
                && (tooLarge == null
                    || trial.Quantizer > tooLarge.Quantizer))
            {
                tooLarge = trial;
            }
            if (trial.SizeBytes < targetSizeBytes
                && (smallEnough == null
                    || trial.Quantizer < smallEnough.Quantizer))
            {
                smallEnough = trial;
            }
            index++;
        }

        int minimum = tooLarge == null ? 0 : tooLarge.Quantizer + 1;
        int maximum = smallEnough == null
            ? Av1SyntaxConstants.MaxQuantizerIndex
            : smallEnough.Quantizer - 1;
        if (minimum > maximum)
        {
            return CreateCompleted(best, false);
        }
        int next = minimum + (maximum - minimum) / 2;
        next = FindUntried(next, minimum, maximum, trials);
        if (next < 0)
        {
            return CreateCompleted(best, false);
        }
        return CreateTrial(next);
    }

    static int FindUntried(
        int preferred,
        int minimum,
        int maximum,
        Av1EncodedSizeTrial[] trials)
    {
        int distance = 0;
        while (preferred - distance >= minimum
            || preferred + distance <= maximum)
        {
            int lower = preferred - distance;
            if (lower >= minimum && !WasTried(lower, trials))
            {
                return lower;
            }
            int upper = preferred + distance;
            if (distance != 0 && upper <= maximum
                && !WasTried(upper, trials))
            {
                return upper;
            }
            distance++;
        }
        return -1;
    }

    static bool WasTried(int quantizer, Av1EncodedSizeTrial[] trials)
    {
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].Quantizer == quantizer)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static Av1EncodedSizeTrial GetBestTrial(
        Av1EncodedSizeTrial[] trials,
        long targetSizeBytes)
    {
        Av1EncodedSizeTrial best = trials[0];
        long bestDifference = GetDifference(best.SizeBytes, targetSizeBytes);
        int index = 1;
        while (index < trials.Length)
        {
            Av1EncodedSizeTrial candidate = trials[index];
            long difference = GetDifference(
                candidate.SizeBytes,
                targetSizeBytes);
            bool candidateUnder = candidate.SizeBytes <= targetSizeBytes;
            bool bestUnder = best.SizeBytes <= targetSizeBytes;
            if (difference < bestDifference
                || difference == bestDifference && candidateUnder && !bestUnder
                || difference == bestDifference && candidateUnder == bestUnder
                    && candidate.Quantizer < best.Quantizer)
            {
                best = candidate;
                bestDifference = difference;
            }
            index++;
        }
        return best;
    }

    static long GetDifference(long first, long second)
    {
        return first >= second ? first - second : second - first;
    }

    static AvifResultCode ValidateTrials(
        long targetSizeBytes,
        Av1EncodedSizeTrial[] trials)
    {
        if (targetSizeBytes <= 0 || trials == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        int index = 0;
        while (index < trials.Length)
        {
            Av1EncodedSizeTrial trial = trials[index];
            if (trial == null || trial.Quantizer < 0
                || trial.Quantizer > Av1SyntaxConstants.MaxQuantizerIndex
                || trial.SizeBytes <= 0)
            {
                return AvifResultCode.InvalidArgument;
            }
            int previous = 0;
            while (previous < index)
            {
                if (trials[previous].Quantizer == trial.Quantizer)
                {
                    return AvifResultCode.InvalidArgument;
                }
                previous++;
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    static Av1EncoderQualityDecision CreateFixed(
        int qIndex,
        bool lossless)
    {
        Av1EncoderQualityDecision decision =
            new Av1EncoderQualityDecision(AvifResultCode.Success);
        decision.QIndex = qIndex;
        decision.Lossless = lossless;
        return decision;
    }

    static Av1EncoderQualityDecision CreateTrial(int qIndex)
    {
        Av1EncoderQualityDecision decision = CreateFixed(
            qIndex,
            qIndex == 0);
        decision.NeedsSizeTrial = true;
        return decision;
    }

    static Av1EncoderQualityDecision CreateCompleted(
        Av1EncodedSizeTrial trial,
        bool targetMet)
    {
        Av1EncoderQualityDecision decision = CreateFixed(
            trial.Quantizer,
            trial.Quantizer == 0);
        decision.TargetMet = targetMet;
        decision.SelectedSizeBytes = trial.SizeBytes;
        return decision;
    }
}

internal static class AvifEncoderQualityResolver
{
    internal static AvifEncoderQualityPlan Resolve(
        AvifEncoderOptions options,
        bool hasAlpha,
        bool hasGainMap,
        Av1EncodedSizeTrial[] colorTrials)
    {
        if (options == null)
        {
            return new AvifEncoderQualityPlan(
                AvifResultCode.InvalidArgument);
        }
        Av1EncoderQualityDecision color =
            Av1EncoderQualityResolver.Resolve(
                options.Mode,
                options.Quality,
                options.Quantizer,
                options.TargetSizeBytes,
                colorTrials);
        Av1EncoderQualityDecision? alpha = hasAlpha
            ? Av1EncoderQualityResolver.ResolveQuality(options.AlphaQuality)
            : null;
        Av1EncoderQualityDecision? gainMap = hasGainMap
            ? Av1EncoderQualityResolver.ResolveQuality(options.GainMapQuality)
            : null;
        AvifResultCode code = color.Code;
        if (code == AvifResultCode.Success
            && alpha != null && alpha.Code != AvifResultCode.Success)
        {
            code = alpha.Code;
        }
        if (code == AvifResultCode.Success
            && gainMap != null && gainMap.Code != AvifResultCode.Success)
        {
            code = gainMap.Code;
        }
        AvifEncoderQualityPlan plan = new AvifEncoderQualityPlan(code);
        plan.Color = color;
        plan.Alpha = alpha;
        plan.GainMap = gainMap;
        plan.HasAlpha = hasAlpha;
        plan.HasGainMap = hasGainMap;
        return plan;
    }
}
