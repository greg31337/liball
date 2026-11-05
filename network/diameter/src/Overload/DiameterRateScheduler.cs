using System;

namespace diameter;

internal class DiameterRateBucketState
{
    internal uint MaximumRate;
    internal ulong SequenceNumber;
    internal decimal BucketContentScaled;
    internal long LastConformingMonotonicMilliseconds;
}

internal class DiameterRateScheduleDecision
{
    internal bool Admitted;
    internal bool Excess;
    internal decimal NextBucketContentScaled;
    internal long NextLastConformingMonotonicMilliseconds;
    internal int RetryAfterMilliseconds;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal static class DiameterRateScheduler
{
    internal const decimal InterRequestIncrementScaled = 1000M;

    internal static DiameterRateBucketState CreateDiameterRateBucket(
        DiameterOverloadReport report,
        long activatedMonotonicMilliseconds)
    {
        if (report == null ||
            report.Algorithm != DiameterOverloadAlgorithm.Rate ||
            !report.MaximumRatePresent)
        {
            throw new ArgumentException(
                "A rate overload report is required.");
        }

        report.ValidateOverloadReport();

        DiameterRateBucketState result =
            new DiameterRateBucketState();
        result.MaximumRate = report.MaximumRate;
        result.SequenceNumber = report.SequenceNumber;
        result.LastConformingMonotonicMilliseconds =
            activatedMonotonicMilliseconds;
        return result;
    }

    internal static void UpdateDiameterRateBucket(
        DiameterRateBucketState bucket,
        DiameterOverloadReport report,
        long updatedMonotonicMilliseconds)
    {
        if (bucket == null || report == null ||
            report.Algorithm != DiameterOverloadAlgorithm.Rate ||
            !report.MaximumRatePresent)
        {
            throw new ArgumentException(
                "Rate bucket updates require valid state and a rate report.");
        }

        report.ValidateOverloadReport();
        long elapsed = ReadDiameterMonotonicElapsed(
            bucket.LastConformingMonotonicMilliseconds,
            updatedMonotonicMilliseconds);
        decimal currentContent = ReadDiameterRateProvisionalContent(
            bucket.BucketContentScaled,
            elapsed,
            bucket.MaximumRate);
        if (bucket.MaximumRate == 0U || report.MaximumRate == 0U)
        {
            currentContent = 0M;
        }
        else
        {
            currentContent = currentContent *
                (decimal)report.MaximumRate /
                (decimal)bucket.MaximumRate;
        }

        bucket.MaximumRate = report.MaximumRate;
        bucket.SequenceNumber = report.SequenceNumber;
        bucket.BucketContentScaled = currentContent;
        bucket.LastConformingMonotonicMilliseconds =
            updatedMonotonicMilliseconds;
    }

    internal static DiameterRateScheduleDecision EvaluateDiameterRateBucket(
        DiameterRateBucketState bucket,
        int toleranceMilliseconds,
        long arrivalMonotonicMilliseconds)
    {
        if (bucket == null || toleranceMilliseconds < 0 ||
            bucket.BucketContentScaled < 0M)
        {
            throw new ArgumentException(
                "Rate scheduling requires valid bucket state and tolerance.");
        }

        DiameterRateScheduleDecision result =
            new DiameterRateScheduleDecision();
        result.NextBucketContentScaled = bucket.BucketContentScaled;
        result.NextLastConformingMonotonicMilliseconds =
            bucket.LastConformingMonotonicMilliseconds;
        if (bucket.MaximumRate == 0U)
        {
            result.Excess = true;
            result.Code = "overload-rate-zero";
            result.Message =
                "The active rate report permits no Diameter requests.";
            return result;
        }

        long elapsed = ReadDiameterMonotonicElapsed(
            bucket.LastConformingMonotonicMilliseconds,
            arrivalMonotonicMilliseconds);

        decimal provisional = ReadDiameterRateProvisionalContent(
            bucket.BucketContentScaled,
            elapsed,
            bucket.MaximumRate);
        decimal toleranceScaled =
            (decimal)toleranceMilliseconds *
            (decimal)bucket.MaximumRate;
        if (provisional > toleranceScaled)
        {
            result.Excess = true;
            result.RetryAfterMilliseconds =
                ReadDiameterRateRetryMilliseconds(
                    provisional,
                    toleranceScaled,
                    bucket.MaximumRate);
            result.Code = "overload-rate-excess";
            result.Message =
                "The request exceeds the active maximum Diameter request rate.";
            return result;
        }

        if (provisional < 0M)
        {
            provisional = 0M;
        }

        result.Admitted = true;
        result.NextBucketContentScaled =
            provisional + InterRequestIncrementScaled;
        result.NextLastConformingMonotonicMilliseconds =
            arrivalMonotonicMilliseconds;
        result.Code = "overload-rate-conforming";
        result.Message =
            "The request conforms to the active maximum Diameter request rate.";
        return result;
    }

    internal static long ReadDiameterMonotonicElapsed(
        long previousMonotonicMilliseconds,
        long currentMonotonicMilliseconds)
    {
        if (currentMonotonicMilliseconds <=
            previousMonotonicMilliseconds)
        {
            return 0L;
        }

        if (previousMonotonicMilliseconds < 0L &&
            currentMonotonicMilliseconds >
                long.MaxValue + previousMonotonicMilliseconds)
        {
            return long.MaxValue;
        }

        return currentMonotonicMilliseconds -
            previousMonotonicMilliseconds;
    }

    internal static void CommitDiameterRateBucket(
        DiameterRateBucketState bucket,
        DiameterRateScheduleDecision decision)
    {
        if (bucket == null || decision == null || !decision.Admitted)
        {
            throw new ArgumentException(
                "Only an admitted rate decision can update bucket state.");
        }

        bucket.BucketContentScaled = decision.NextBucketContentScaled;
        bucket.LastConformingMonotonicMilliseconds =
            decision.NextLastConformingMonotonicMilliseconds;
    }

    internal static decimal ReadDiameterRateProvisionalContent(
        decimal bucketContentScaled,
        long elapsedMilliseconds,
        uint maximumRate)
    {
        if (elapsedMilliseconds <= 0L || maximumRate == 0U)
        {
            return bucketContentScaled;
        }

        decimal drained = (decimal)elapsedMilliseconds *
            (decimal)maximumRate;
        if (drained >= bucketContentScaled)
        {
            return 0M;
        }

        return bucketContentScaled - drained;
    }

    internal static int ReadDiameterRateRetryMilliseconds(
        decimal provisionalContentScaled,
        decimal toleranceScaled,
        uint maximumRate)
    {
        if (maximumRate == 0U ||
            provisionalContentScaled <= toleranceScaled)
        {
            return 0;
        }

        decimal difference = provisionalContentScaled - toleranceScaled;
        decimal result = difference / (decimal)maximumRate;
        if (result > int.MaxValue)
        {
            return int.MaxValue;
        }

        int wholeMilliseconds = (int)result;
        if ((decimal)wholeMilliseconds < result)
        {
            wholeMilliseconds = wholeMilliseconds + 1;
        }

        return wholeMilliseconds;
    }
}
