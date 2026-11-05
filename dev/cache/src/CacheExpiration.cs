namespace cache;

internal static class CacheExpiration
{
    internal static bool HasExpiration(CacheEntryOptions options)
    {
        if (options.HasAbsoluteExpiration)
        {
            return true;
        }

        if (options.HasTimeToLive)
        {
            return true;
        }

        if (options.HasSlidingExpiration)
        {
            return true;
        }

        return false;
    }

    internal static long CalculateFixedExpirationUtcTicks(CacheEntryOptions options, long createdUtcTicks)
    {
        bool foundExpiration = false;
        long expirationUtcTicks = 0L;

        if (options.HasAbsoluteExpiration)
        {
            foundExpiration = true;
            expirationUtcTicks = options.AbsoluteExpirationUtcTicks;
        }

        if (options.HasTimeToLive)
        {
            long ttlExpirationUtcTicks = AddTicks(createdUtcTicks, options.TimeToLiveTicks);
            if (!foundExpiration || ttlExpirationUtcTicks < expirationUtcTicks)
            {
                foundExpiration = true;
                expirationUtcTicks = ttlExpirationUtcTicks;
            }
        }

        if (foundExpiration)
        {
            return expirationUtcTicks;
        }

        return 0L;
    }

    internal static long CalculateSlidingExpirationUtcTicks(CacheEntryOptions options, long lastAccessUtcTicks)
    {
        if (options.HasSlidingExpiration)
        {
            return AddTicks(lastAccessUtcTicks, options.SlidingExpirationTicks);
        }

        return 0L;
    }

    internal static long CalculateEffectiveExpirationUtcTicks(CacheEntryOptions options, long createdUtcTicks, long lastAccessUtcTicks)
    {
        bool foundExpiration = false;
        long expirationUtcTicks = 0L;
        long fixedExpirationUtcTicks = CalculateFixedExpirationUtcTicks(options, createdUtcTicks);
        long slidingExpirationUtcTicks = CalculateSlidingExpirationUtcTicks(options, lastAccessUtcTicks);

        if (fixedExpirationUtcTicks != 0L || options.HasAbsoluteExpiration || options.HasTimeToLive)
        {
            foundExpiration = true;
            expirationUtcTicks = fixedExpirationUtcTicks;
        }

        if (slidingExpirationUtcTicks != 0L || options.HasSlidingExpiration)
        {
            if (!foundExpiration || slidingExpirationUtcTicks < expirationUtcTicks)
            {
                foundExpiration = true;
                expirationUtcTicks = slidingExpirationUtcTicks;
            }
        }

        if (foundExpiration)
        {
            return expirationUtcTicks;
        }

        return 0L;
    }

    internal static bool IsExpired(CacheEntryOptions options, long createdUtcTicks, long lastAccessUtcTicks, long nowUtcTicks)
    {
        long expirationUtcTicks = CalculateEffectiveExpirationUtcTicks(options, createdUtcTicks, lastAccessUtcTicks);
        if (expirationUtcTicks != 0L || HasExpiration(options))
        {
            if (nowUtcTicks >= expirationUtcTicks)
            {
                return true;
            }
        }

        return false;
    }

    internal static long AddTicks(long startUtcTicks, long durationTicks)
    {
        return CacheMath.Add(startUtcTicks, durationTicks);
    }
}
