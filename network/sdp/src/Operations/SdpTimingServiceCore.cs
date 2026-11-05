using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace sdp;

internal static class SdpTimingServiceCore
{
    internal static readonly DateTimeOffset NtpEpoch = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

    internal static SdpNtpConversionResult ConvertNtpToDateTimeOffset(string ntpValue)
    {
        SdpNtpConversionResult result = new SdpNtpConversionResult();
        SdpNumberParseResult parsed = SdpNumberParser.ParseCharacterNtpValue(ntpValue.AsSpan());
        if (!parsed.Success)
        {
            result.DiagnosticCode = parsed.DiagnosticCode;
            if (parsed.Overflow)
            {
                result.Detail = "The NTP value exceeds the unsigned 64-bit representation.";
            }
            else
            {
                result.Detail = "The NTP value must contain one or more unsigned decimal digits.";
            }
            return result;
        }

        long maximumTicks = DateTimeOffset.MaxValue.Ticks - NtpEpoch.Ticks;
        ulong maximumSeconds = (ulong)(maximumTicks / TimeSpan.TicksPerSecond);
        if (parsed.UnsignedValue > maximumSeconds)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.TimingUnrepresentable;
            result.Detail = "The NTP value is later than the maximum .NET instant.";
            return result;
        }

        long elapsedTicks = (long)parsed.UnsignedValue * TimeSpan.TicksPerSecond;
        result.Success = true;
        result.NtpValue = ntpValue;
        result.DateTimeValue = new DateTimeOffset(NtpEpoch.Ticks + elapsedTicks, TimeSpan.Zero);
        return result;
    }

    internal static SdpNtpConversionResult ConvertDateTimeOffsetToNtp(DateTimeOffset dateTimeValue)
    {
        SdpNtpConversionResult result = new SdpNtpConversionResult();
        DateTimeOffset utcValue = dateTimeValue.ToUniversalTime();
        if (utcValue.Ticks < NtpEpoch.Ticks)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.TimingUnrepresentable;
            result.Detail = "The .NET instant is earlier than the NTP epoch.";
            return result;
        }

        long elapsedTicks = utcValue.Ticks - NtpEpoch.Ticks;
        if (elapsedTicks % TimeSpan.TicksPerSecond != 0)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.TimingUnrepresentable;
            result.Detail = "The .NET instant contains a fractional second that cannot be represented by an SDP NTP value.";
            return result;
        }

        ulong seconds = (ulong)(elapsedTicks / TimeSpan.TicksPerSecond);
        result.Success = true;
        result.NtpValue = seconds.ToString(CultureInfo.InvariantCulture);
        result.DateTimeValue = utcValue;
        return result;
    }

    internal static SdpTypedTimeNormalizationResult NormalizeTypedTime(SdpTypedTime value)
    {
        SdpTypedTimeNormalizationResult result = new SdpTypedTimeNormalizationResult();
        if (value.Unit == null)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.ValidationTiming;
            result.Detail = "The typed-time unit must not be null.";
            return result;
        }

        long multiplier = 1;
        if (value.Unit == "m")
        {
            multiplier = 60;
        }
        else if (value.Unit == "h")
        {
            multiplier = 3600;
        }
        else if (value.Unit == "d")
        {
            multiplier = 86400;
        }
        else if (value.Unit.Length != 0 && value.Unit != "s")
        {
            result.DiagnosticCode = SdpDiagnosticCodes.ValidationTiming;
            result.Detail = "The typed-time unit must be empty or one of d, h, m, or s.";
            return result;
        }

        if (value.Value > 0 && value.Value > long.MaxValue / multiplier)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.TimingUnrepresentable;
            result.Detail = "The typed-time value exceeds the signed 64-bit seconds representation.";
            return result;
        }
        if (value.Value < 0 && value.Value < long.MinValue / multiplier)
        {
            result.DiagnosticCode = SdpDiagnosticCodes.TimingUnrepresentable;
            result.Detail = "The typed-time value exceeds the signed 64-bit seconds representation.";
            return result;
        }

        result.Success = true;
        result.Seconds = value.Value * multiplier;
        return result;
    }

    internal static SdpTimingOccurrenceResult CalculateOccurrences(SdpSessionDescription description, DateTimeOffset windowStart, DateTimeOffset windowStop, int resultLimit)
    {
        SdpTimingOccurrenceResult result = new SdpTimingOccurrenceResult();
        if (description.Timings == null || description.TimeZoneAdjustments == null)
        {
            SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "The timing collections must be initialized.");
            return result;
        }

        List<SdpTimingZoneOffset>? zoneOffsets = BuildZoneOffsets(description.TimeZoneAdjustments, result);
        if (zoneOffsets == null)
        {
            return result;
        }

        DateTimeOffset utcWindowStart = windowStart.ToUniversalTime();
        DateTimeOffset utcWindowStop = windowStop.ToUniversalTime();
        int timingIndex = 0;
        while (timingIndex < description.Timings.Count)
        {
            SdpTiming timing = description.Timings[timingIndex];
            if (timing == null || timing.RepeatTimes == null)
            {
                SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "Every timing period and repeat collection must be initialized.");
                return result;
            }
            if (!AddTimingPeriodOccurrences(result, timing, timingIndex, zoneOffsets, utcWindowStart, utcWindowStop, resultLimit))
            {
                return result;
            }
            timingIndex = timingIndex + 1;
        }

        result.Success = true;
        return result;
    }

    internal static List<SdpTimingZoneOffset>? BuildZoneOffsets(List<SdpTimeZoneAdjustment> adjustments, SdpTimingOccurrenceResult result)
    {
        List<SdpTimingZoneOffset> offsets = new List<SdpTimingZoneOffset>();
        int index = 0;
        while (index < adjustments.Count)
        {
            SdpTimeZoneAdjustment adjustment = adjustments[index];
            if (adjustment == null || adjustment.Offset == null || adjustment.AdjustmentTime == null)
            {
                SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "Every time-zone adjustment must be complete.");
                return null;
            }

            SdpNtpConversionResult adjustmentTime = ConvertNtpToDateTimeOffset(adjustment.AdjustmentTime);
            if (!adjustmentTime.Success)
            {
                SetOccurrenceFailure(result, adjustmentTime.DiagnosticCode, "A time-zone adjustment time is invalid or unrepresentable.");
                return null;
            }
            SdpTypedTimeNormalizationResult adjustmentOffset = NormalizeTypedTime(adjustment.Offset);
            if (!adjustmentOffset.Success)
            {
                SetOccurrenceFailure(result, adjustmentOffset.DiagnosticCode, "A time-zone adjustment offset is invalid or unrepresentable.");
                return null;
            }

            SdpTimingZoneOffset item = new SdpTimingZoneOffset();
            item.AdjustmentSecond = ReadNtpSecond(adjustmentTime.DateTimeValue);
            item.OffsetSecond = adjustmentOffset.Seconds;
            InsertZoneOffset(offsets, item);
            index = index + 1;
        }
        return offsets;
    }

    internal static void InsertZoneOffset(List<SdpTimingZoneOffset> offsets, SdpTimingZoneOffset item)
    {
        int index = 0;
        while (index < offsets.Count && offsets[index].AdjustmentSecond < item.AdjustmentSecond)
        {
            index = index + 1;
        }
        if (index < offsets.Count && offsets[index].AdjustmentSecond == item.AdjustmentSecond)
        {
            offsets[index] = item;
            return;
        }
        offsets.Insert(index, item);
    }

    internal static bool AddTimingPeriodOccurrences(SdpTimingOccurrenceResult result, SdpTiming timing, int timingIndex, List<SdpTimingZoneOffset> zoneOffsets, DateTimeOffset windowStart, DateTimeOffset windowStop, int resultLimit)
    {
        if (timing.StartTime == null || timing.StopTime == null)
        {
            SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "Every timing period must contain start and stop values.");
            return false;
        }

        SdpNtpConversionResult startConversion = ConvertNtpToDateTimeOffset(timing.StartTime);
        SdpNtpConversionResult stopConversion = ConvertNtpToDateTimeOffset(timing.StopTime);
        if (!startConversion.Success)
        {
            SetOccurrenceFailure(result, startConversion.DiagnosticCode, "A timing start value is invalid or unrepresentable.");
            return false;
        }
        if (!stopConversion.Success)
        {
            SetOccurrenceFailure(result, stopConversion.DiagnosticCode, "A timing stop value is invalid or unrepresentable.");
            return false;
        }

        long startSecond = ReadNtpSecond(startConversion.DateTimeValue);
        long stopSecond = ReadNtpSecond(stopConversion.DateTimeValue);
        bool unbounded = stopSecond == 0;
        if (!unbounded && stopSecond <= startSecond)
        {
            SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "A bounded timing period must stop after it starts.");
            return false;
        }

        if (timing.RepeatTimes.Count == 0)
        {
            AddNonRepeatingOccurrence(result, startConversion.DateTimeValue, stopConversion.DateTimeValue, startSecond == 0 && unbounded, unbounded, timingIndex, windowStart, windowStop, resultLimit);
            return true;
        }

        BigInteger windowLowerTicks = new BigInteger(windowStart.Ticks - NtpEpoch.Ticks);
        BigInteger windowUpperTicks = new BigInteger(windowStop.Ticks - NtpEpoch.Ticks);
        BigInteger timingLowerTicks = new BigInteger(startSecond) * TimeSpan.TicksPerSecond;
        if (windowLowerTicks < timingLowerTicks)
        {
            windowLowerTicks = timingLowerTicks;
        }
        if (!unbounded)
        {
            BigInteger timingUpperTicks = new BigInteger(stopSecond) * TimeSpan.TicksPerSecond;
            if (windowUpperTicks > timingUpperTicks)
            {
                windowUpperTicks = timingUpperTicks;
            }
        }
        if (windowUpperTicks <= windowLowerTicks)
        {
            return true;
        }

        int repeatIndex = 0;
        while (repeatIndex < timing.RepeatTimes.Count)
        {
            SdpRepeatTime repeat = timing.RepeatTimes[repeatIndex];
            if (!AddRepeatOccurrences(result, repeat, timingIndex, repeatIndex, startSecond, zoneOffsets, windowLowerTicks, windowUpperTicks, resultLimit))
            {
                return false;
            }
            repeatIndex = repeatIndex + 1;
        }
        return true;
    }

    internal static void AddNonRepeatingOccurrence(SdpTimingOccurrenceResult result, DateTimeOffset startTime, DateTimeOffset stopTime, bool permanent, bool unbounded, int timingIndex, DateTimeOffset windowStart, DateTimeOffset windowStop, int resultLimit)
    {
        if (windowStop <= windowStart)
        {
            return;
        }

        DateTimeOffset occurrenceStart = startTime;
        DateTimeOffset occurrenceStop = stopTime;
        if (permanent)
        {
            occurrenceStart = windowStart;
            occurrenceStop = windowStop;
        }
        else
        {
            if (occurrenceStart < windowStart)
            {
                occurrenceStart = windowStart;
            }
            if (unbounded || occurrenceStop > windowStop)
            {
                occurrenceStop = windowStop;
            }
        }
        if (occurrenceStop <= occurrenceStart || occurrenceStart >= windowStop || occurrenceStop <= windowStart)
        {
            return;
        }

        SdpTimingOccurrence occurrence = CreateOccurrence(occurrenceStart, occurrenceStop, timingIndex, -1, -1);
        InsertOccurrence(result, occurrence, resultLimit);
    }

    internal static bool AddRepeatOccurrences(SdpTimingOccurrenceResult result, SdpRepeatTime repeat, int timingIndex, int repeatIndex, long timingStartSecond, List<SdpTimingZoneOffset> zoneOffsets, BigInteger windowLowerTicks, BigInteger windowUpperTicks, int resultLimit)
    {
        if (repeat == null || repeat.RepeatInterval == null || repeat.ActiveDuration == null || repeat.Offsets == null)
        {
            SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "Every repeat description must be complete.");
            return false;
        }

        SdpTypedTimeNormalizationResult interval = NormalizeTypedTime(repeat.RepeatInterval);
        SdpTypedTimeNormalizationResult duration = NormalizeTypedTime(repeat.ActiveDuration);
        if (!interval.Success || interval.Seconds <= 0)
        {
            string code = interval.DiagnosticCode.Length == 0 ? SdpDiagnosticCodes.ValidationTiming : interval.DiagnosticCode;
            SetOccurrenceFailure(result, code, "A repeat interval must be a positive representable typed time.");
            return false;
        }
        if (!duration.Success || duration.Seconds <= 0)
        {
            string code = duration.DiagnosticCode.Length == 0 ? SdpDiagnosticCodes.ValidationTiming : duration.DiagnosticCode;
            SetOccurrenceFailure(result, code, "A repeat active duration must be a positive representable typed time.");
            return false;
        }

        int offsetIndex = 0;
        while (offsetIndex < repeat.Offsets.Count)
        {
            SdpTypedTime offset = repeat.Offsets[offsetIndex];
            if (offset == null)
            {
                SetOccurrenceFailure(result, SdpDiagnosticCodes.ValidationTiming, "Every repeat offset must be initialized.");
                return false;
            }
            SdpTypedTimeNormalizationResult normalizedOffset = NormalizeTypedTime(offset);
            if (!normalizedOffset.Success || normalizedOffset.Seconds < 0)
            {
                string code = normalizedOffset.DiagnosticCode.Length == 0 ? SdpDiagnosticCodes.ValidationTiming : normalizedOffset.DiagnosticCode;
                SetOccurrenceFailure(result, code, "A repeat offset must be a nonnegative representable typed time.");
                return false;
            }

            BigInteger baseOriginSecond = new BigInteger(timingStartSecond) + normalizedOffset.Seconds;
            AddRepeatOffsetSegments(result, baseOriginSecond, interval.Seconds, duration.Seconds, timingIndex, repeatIndex, offsetIndex, zoneOffsets, windowLowerTicks, windowUpperTicks, resultLimit);
            offsetIndex = offsetIndex + 1;
        }
        return true;
    }

    internal static void AddRepeatOffsetSegments(SdpTimingOccurrenceResult result, BigInteger baseOriginSecond, long intervalSecond, long durationSecond, int timingIndex, int repeatIndex, int offsetIndex, List<SdpTimingZoneOffset> zoneOffsets, BigInteger windowLowerTicks, BigInteger windowUpperTicks, int resultLimit)
    {
        int segmentIndex = -1;
        while (segmentIndex < zoneOffsets.Count)
        {
            BigInteger segmentLower = BigInteger.Zero;
            long zoneOffsetSecond = 0;
            if (segmentIndex >= 0)
            {
                segmentLower = new BigInteger(zoneOffsets[segmentIndex].AdjustmentSecond);
                zoneOffsetSecond = zoneOffsets[segmentIndex].OffsetSecond;
            }

            bool hasSegmentUpper = segmentIndex + 1 < zoneOffsets.Count;
            BigInteger segmentUpper = BigInteger.Zero;
            if (hasSegmentUpper)
            {
                segmentUpper = new BigInteger(zoneOffsets[segmentIndex + 1].AdjustmentSecond);
            }
            AddRepeatOffsetSegment(result, baseOriginSecond, intervalSecond, durationSecond, timingIndex, repeatIndex, offsetIndex, zoneOffsetSecond, segmentLower, segmentUpper, hasSegmentUpper, windowLowerTicks, windowUpperTicks, resultLimit);
            segmentIndex = segmentIndex + 1;
        }
    }

    internal static void AddRepeatOffsetSegment(SdpTimingOccurrenceResult result, BigInteger baseOriginSecond, long intervalSecond, long durationSecond, int timingIndex, int repeatIndex, int offsetIndex, long zoneOffsetSecond, BigInteger segmentLower, BigInteger segmentUpper, bool hasSegmentUpper, BigInteger windowLowerTicks, BigInteger windowUpperTicks, int resultLimit)
    {
        BigInteger ticksPerSecond = new BigInteger(TimeSpan.TicksPerSecond);
        BigInteger minimumStartSecond = FloorDivide(windowLowerTicks, ticksPerSecond) + 1 - durationSecond;
        BigInteger maximumStartSecond = FloorDivide(windowUpperTicks - 1, ticksPerSecond);
        BigInteger minimumBaseSecond = minimumStartSecond - zoneOffsetSecond;
        BigInteger maximumBaseSecond = maximumStartSecond - zoneOffsetSecond;
        if (minimumBaseSecond < segmentLower)
        {
            minimumBaseSecond = segmentLower;
        }
        if (hasSegmentUpper && maximumBaseSecond >= segmentUpper)
        {
            maximumBaseSecond = segmentUpper - 1;
        }
        if (maximumBaseSecond < minimumBaseSecond || maximumBaseSecond < baseOriginSecond)
        {
            return;
        }

        BigInteger firstIndex = BigInteger.Zero;
        if (minimumBaseSecond > baseOriginSecond)
        {
            BigInteger difference = minimumBaseSecond - baseOriginSecond;
            firstIndex = (difference + intervalSecond - 1) / intervalSecond;
        }
        BigInteger lastIndex = (maximumBaseSecond - baseOriginSecond) / intervalSecond;
        if (firstIndex > lastIndex)
        {
            return;
        }

        BigInteger currentIndex = firstIndex;
        long retainedScanCount = 0;
        while (currentIndex <= lastIndex && retainedScanCount <= resultLimit)
        {
            BigInteger baseSecond = baseOriginSecond + currentIndex * intervalSecond;
            BigInteger startSecond = baseSecond + zoneOffsetSecond;
            BigInteger startTicks = startSecond * ticksPerSecond;
            BigInteger stopTicks = startTicks + new BigInteger(durationSecond) * ticksPerSecond;
            BigInteger clippedStartTicks = startTicks;
            BigInteger clippedStopTicks = stopTicks;
            if (clippedStartTicks < windowLowerTicks)
            {
                clippedStartTicks = windowLowerTicks;
            }
            if (clippedStopTicks > windowUpperTicks)
            {
                clippedStopTicks = windowUpperTicks;
            }

            DateTimeOffset occurrenceStart = CreateDateTimeOffset(clippedStartTicks);
            DateTimeOffset occurrenceStop = CreateDateTimeOffset(clippedStopTicks);
            SdpTimingOccurrence occurrence = CreateOccurrence(occurrenceStart, occurrenceStop, timingIndex, repeatIndex, offsetIndex);
            InsertOccurrence(result, occurrence, resultLimit);
            currentIndex = currentIndex + 1;
            retainedScanCount = retainedScanCount + 1;
        }
        if (currentIndex <= lastIndex)
        {
            result.ResultLimitReached = true;
        }
    }

    internal static BigInteger FloorDivide(BigInteger value, BigInteger divisor)
    {
        BigInteger quotient = value / divisor;
        BigInteger remainder = value % divisor;
        if (remainder < 0)
        {
            quotient = quotient - 1;
        }
        return quotient;
    }

    internal static DateTimeOffset CreateDateTimeOffset(BigInteger relativeTicks)
    {
        long ticks = NtpEpoch.Ticks + (long)relativeTicks;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    internal static SdpTimingOccurrence CreateOccurrence(DateTimeOffset startTime, DateTimeOffset stopTime, int timingIndex, int repeatIndex, int offsetIndex)
    {
        SdpTimingOccurrence occurrence = new SdpTimingOccurrence();
        occurrence.StartTime = startTime;
        occurrence.StopTime = stopTime;
        occurrence.TimingIndex = timingIndex;
        occurrence.RepeatIndex = repeatIndex;
        occurrence.OffsetIndex = offsetIndex;
        return occurrence;
    }

    internal static void InsertOccurrence(SdpTimingOccurrenceResult result, SdpTimingOccurrence occurrence, int resultLimit)
    {
        int index = 0;
        while (index < result.Occurrences.Count && CompareOccurrences(result.Occurrences[index], occurrence) <= 0)
        {
            index = index + 1;
        }
        if (result.Occurrences.Count < resultLimit)
        {
            result.Occurrences.Insert(index, occurrence);
            return;
        }

        result.ResultLimitReached = true;
        if (index < resultLimit)
        {
            result.Occurrences.Insert(index, occurrence);
            result.Occurrences.RemoveAt(resultLimit);
        }
    }

    internal static int CompareOccurrences(SdpTimingOccurrence left, SdpTimingOccurrence right)
    {
        int comparison = left.StartTime.CompareTo(right.StartTime);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = left.StopTime.CompareTo(right.StopTime);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = left.TimingIndex.CompareTo(right.TimingIndex);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = left.RepeatIndex.CompareTo(right.RepeatIndex);
        if (comparison != 0)
        {
            return comparison;
        }
        return left.OffsetIndex.CompareTo(right.OffsetIndex);
    }

    internal static long ReadNtpSecond(DateTimeOffset value)
    {
        return (value.Ticks - NtpEpoch.Ticks) / TimeSpan.TicksPerSecond;
    }

    internal static void SetOccurrenceFailure(SdpTimingOccurrenceResult result, string diagnosticCode, string detail)
    {
        result.Success = false;
        result.Occurrences.Clear();
        result.ResultLimitReached = false;
        result.DiagnosticCode = diagnosticCode;
        result.Detail = detail;
    }
}
