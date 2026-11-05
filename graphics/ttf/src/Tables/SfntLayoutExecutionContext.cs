namespace ttf;

internal class SfntLayoutExecutionContext
{
    FontLimits limits;
    int maximumSubstitutionExpansion;
    int maximumGlyphBufferLength;
    int substitutionExpansion;
    int operationCount;
    int maximumLayoutOperations;
    System.TimeSpan maximumDuration;
    System.Threading.CancellationToken cancellationToken;
    System.TimeProvider timeProvider;
    long startTimestamp;
    bool hasTimeLimit;

    internal SfntLayoutExecutionContext(
        FontLimits limits,
        int maximumSubstitutionExpansion,
        int maximumGlyphBufferLength,
        System.TimeSpan maximumDuration,
        System.Threading.CancellationToken cancellationToken,
        System.TimeProvider timeProvider)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (maximumSubstitutionExpansion < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSubstitutionExpansion");
        }

        if (maximumGlyphBufferLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumGlyphBufferLength");
        }

        if (maximumDuration !=
            System.Threading.Timeout.InfiniteTimeSpan &&
            maximumDuration <= System.TimeSpan.Zero)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumDuration");
        }

        if (timeProvider == null)
        {
            throw new System.ArgumentNullException("timeProvider");
        }

        this.limits = limits;
        this.maximumSubstitutionExpansion =
            maximumSubstitutionExpansion;
        this.maximumGlyphBufferLength = maximumGlyphBufferLength;
        maximumLayoutOperations = limits.GetMaxLayoutOperations();
        this.maximumDuration = maximumDuration;
        this.cancellationToken = cancellationToken;
        this.timeProvider = timeProvider;
        startTimestamp = timeProvider.GetTimestamp();
        hasTimeLimit = maximumDuration !=
            System.Threading.Timeout.InfiniteTimeSpan;
    }

    internal static SfntLayoutExecutionContext CreateDefault(
        FontLimits limits)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        return new SfntLayoutExecutionContext(
            limits,
            limits.GetMaxGlyphsPerFace(),
            limits.GetMaxGlyphsPerFace(),
            System.Threading.Timeout.InfiniteTimeSpan,
            System.Threading.CancellationToken.None,
            System.TimeProvider.System);
    }

    internal static SfntLayoutExecutionContext CreateForShaping(
        FontLimits limits,
        int maximumGlyphCount,
        int maximumOperationCount,
        System.Threading.CancellationToken cancellationToken)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }
        if (maximumGlyphCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumGlyphCount");
        }
        if (maximumOperationCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumOperationCount");
        }
        int glyphLimit = maximumGlyphCount < limits.GetMaxGlyphsPerFace() ?
            maximumGlyphCount : limits.GetMaxGlyphsPerFace();
        SfntLayoutExecutionContext context =
            new SfntLayoutExecutionContext(
                limits,
                glyphLimit,
                glyphLimit,
                System.Threading.Timeout.InfiniteTimeSpan,
                cancellationToken,
                System.TimeProvider.System);
        context.maximumLayoutOperations =
            maximumOperationCount < limits.GetMaxLayoutOperations() ?
                maximumOperationCount : limits.GetMaxLayoutOperations();
        return context;
    }

    internal int GetOperationCount()
    {
        return operationCount;
    }

    internal int GetSubstitutionExpansion()
    {
        return substitutionExpansion;
    }

    internal void CheckGlyphBuffer(int glyphCount)
    {
        CheckInterrupts();

        if (glyphCount < 0 || glyphCount > maximumGlyphBufferLength)
        {
            throw FontLimitException.Create(
                "OpenType layout glyph buffer exceeds the configured limit.");
        }
    }

    internal void CheckSubstitution(
        int currentGlyphCount,
        int replacementGlyphCount)
    {
        CheckInterrupts();

        if (currentGlyphCount <= 0 || replacementGlyphCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "replacementGlyphCount");
        }

        int addedGlyphs = replacementGlyphCount - 1;

        if (addedGlyphs >
            maximumSubstitutionExpansion - substitutionExpansion)
        {
            throw FontLimitException.Create(
                "OpenType layout substitution expansion exceeds the configured limit.");
        }

        long newGlyphCount =
            (long)currentGlyphCount + addedGlyphs;

        if (newGlyphCount > maximumGlyphBufferLength)
        {
            throw FontLimitException.Create(
                "OpenType layout glyph buffer exceeds the configured limit.");
        }

        substitutionExpansion += addedGlyphs;
    }

    internal void CheckNesting(int depth)
    {
        CheckInterrupts();

        if (depth >= limits.GetMaxLayoutNesting())
        {
            throw FontLimitException.Create(
                "OpenType layout lookup nesting exceeds the configured limit.");
        }
    }

    internal void RecordOperation()
    {
        CheckInterrupts();

        if (operationCount >= maximumLayoutOperations)
        {
            throw FontLimitException.Create(
                "OpenType layout operation count exceeds the configured limit.");
        }

        operationCount += 1;
    }

    internal void CheckInterrupts()
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (hasTimeLimit &&
            timeProvider.GetElapsedTime(startTimestamp) >= maximumDuration)
        {
            throw FontLimitException.Create(
                "OpenType layout execution exceeded the configured time limit.");
        }
    }
}
