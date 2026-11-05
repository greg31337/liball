using System;

namespace http;

internal static class CacheAgeSemantics
{
    internal static HttpCacheDecision Calculate(
        HttpCacheResponseContext responseContext)
    {
        ValidateContext(responseContext);
        long apparentAge = responseContext.HasDate ?
            ElapsedSeconds(responseContext.ResponseTime, responseContext.Date) : 0;
        long responseDelay = ElapsedSeconds(
            responseContext.ResponseTime,
            responseContext.RequestTime);
        long ageValue = responseContext.HasAge ? responseContext.Age : 0;
        long correctedAgeValue = AddSaturated(ageValue, responseDelay);
        long correctedInitialAge = apparentAge > correctedAgeValue ?
            apparentAge : correctedAgeValue;
        long residentTime = ElapsedSeconds(
            responseContext.CurrentTime,
            responseContext.ResponseTime);

        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Store;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.HasCurrentAge = true;
        result.CurrentAge = AddSaturated(correctedInitialAge, residentTime);
        result.Reason = "The stored response age was calculated.";
        return result;
    }

    static void ValidateContext(HttpCacheResponseContext responseContext)
    {
        if (responseContext == null || responseContext.Response == null)
        {
            throw new HttpProtocolException(
                "The current-age context is incomplete.",
                "invalid-cache-age-context");
        }

        if (responseContext.HasAge && responseContext.Age < 0)
        {
            throw new HttpProtocolException(
                "The parsed Age value cannot be negative.",
                "invalid-cache-age-value");
        }
    }

    static long ElapsedSeconds(DateTimeOffset later, DateTimeOffset earlier)
    {
        long laterTicks = later.UtcDateTime.Ticks;
        long earlierTicks = earlier.UtcDateTime.Ticks;
        if (laterTicks <= earlierTicks)
        {
            return 0;
        }

        return (laterTicks - earlierTicks) / TimeSpan.TicksPerSecond;
    }

    static long AddSaturated(long left, long right)
    {
        if (left >= long.MaxValue - right)
        {
            return long.MaxValue;
        }

        return left + right;
    }
}
