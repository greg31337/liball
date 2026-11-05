using ttf;

namespace ttf.Tests;

internal class SfntManualTimeProvider : TimeProvider
{
    long timestamp;

    public override long TimestampFrequency
    {
        get { return 1000; }
    }

    public override long GetTimestamp()
    {
        return timestamp;
    }

    internal void AdvanceMilliseconds(long milliseconds)
    {
        timestamp += milliseconds;
    }
}

internal class SfntLayoutExecutionLimitTests
{
    [Test]
    public void RejectsCumulativeSubstitutionExpansionBeforeMutation()
    {
        SfntGsubTable table = CreateMultipleGsub();
        SfntLayoutGlyphBuffer buffer =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(100, 4),
            1,
            100,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => ApplyGsub(table, buffer, context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout substitution expansion exceeds the configured limit."));
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(1));
            Assert.That(context.GetSubstitutionExpansion(), Is.Zero);
        });
    }

    [Test]
    public void RejectsGlyphBufferGrowthBeforeAllocation()
    {
        SfntGsubTable table = CreateMultipleGsub();
        SfntLayoutGlyphBuffer buffer =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(100, 4),
            100,
            2,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => ApplyGsub(table, buffer, context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout glyph buffer exceeds the configured limit."));
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(context.GetSubstitutionExpansion(), Is.Zero);
        });
    }

    [Test]
    public void RejectsAnOversizedInitialGlyphBuffer()
    {
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { 1, 1, 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(100, 4),
            100,
            2,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => ApplyGsub(CreateSingleGsub(), buffer, context));

        Assert.That(exception!.Message, Is.EqualTo(
            "OpenType layout glyph buffer exceeds the configured limit."));
    }

    [Test]
    public void SharesAndEnforcesOperationBudgetAcrossLookups()
    {
        SfntGsubTable table = CreateSingleGsub();
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(2, 4),
            100,
            100,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);
        SfntLayoutGlyphBuffer first =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutGlyphBuffer second =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutGlyphBuffer third =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });

        ApplyGsub(table, first, context);
        ApplyGsub(table, second, context);
        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => ApplyGsub(table, third, context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout operation count exceeds the configured limit."));
            Assert.That(context.GetOperationCount(), Is.EqualTo(2));
            Assert.That(first.GetGlyphId(0), Is.EqualTo(2));
            Assert.That(second.GetGlyphId(0), Is.EqualTo(2));
            Assert.That(third.GetGlyphId(0), Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesOperationBudgetToGposExecution()
    {
        SfntGposTable table = CreateSingleGpos();
        SfntGposPositionBuffer buffer = SfntGposPositionBuffer.Create(
            new int[] { 1, 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(1, 4),
            0,
            10,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => SfntGposExecutor.ApplyLookup(
                table,
                0,
                buffer,
                null,
                SfntLayoutTraversalDirection.Forward,
                new SfntGposAdjustmentContext(1000, 0, 0, null),
                context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout operation count exceeds the configured limit."));
            Assert.That(buffer.GetXPlacement(0), Is.EqualTo(5));
            Assert.That(buffer.GetXPlacement(1), Is.Zero);
        });
    }

    [Test]
    public void ObservesCancellationBeforeChangingTheBuffer()
    {
        using CancellationTokenSource source =
            new CancellationTokenSource();
        source.Cancel();
        SfntLayoutGlyphBuffer buffer =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(100, 4),
            100,
            100,
            Timeout.InfiniteTimeSpan,
            source.Token,
            TimeProvider.System);

        Assert.Throws<OperationCanceledException>(() =>
            ApplyGsub(CreateSingleGsub(), buffer, context));
        Assert.That(buffer.GetGlyphId(0), Is.EqualTo(1));
    }

    [Test]
    public void EnforcesElapsedTimeBeforeChangingTheBuffer()
    {
        SfntManualTimeProvider timeProvider =
            new SfntManualTimeProvider();
        SfntLayoutGlyphBuffer buffer =
            SfntLayoutGlyphBuffer.Create(new int[] { 1 });
        SfntLayoutExecutionContext context = CreateContext(
            CreateLimits(100, 4),
            100,
            100,
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None,
            timeProvider);
        timeProvider.AdvanceMilliseconds(10);

        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => ApplyGsub(CreateSingleGsub(), buffer, context));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout execution exceeded the configured time limit."));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(1));
        });
    }

    [Test]
    public void EnforcesStableNestingDiagnosticAndValidatesContext()
    {
        FontLimits limits = CreateLimits(100, 1);
        SfntLayoutExecutionContext context = CreateContext(
            limits,
            0,
            10,
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            TimeProvider.System);
        FontLimitException? exception = Assert.Throws<FontLimitException>(
            () => context.CheckNesting(1));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(
                "OpenType layout lookup nesting exceeds the configured limit."));
            Assert.Throws<ArgumentNullException>(() =>
                new SfntLayoutExecutionContext(
                    null!,
                    1,
                    1,
                    Timeout.InfiniteTimeSpan,
                    CancellationToken.None,
                    TimeProvider.System));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateContext(
                    limits,
                    -1,
                    1,
                    Timeout.InfiniteTimeSpan,
                    CancellationToken.None,
                    TimeProvider.System));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateContext(
                    limits,
                    1,
                    0,
                    Timeout.InfiniteTimeSpan,
                    CancellationToken.None,
                    TimeProvider.System));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateContext(
                    limits,
                    1,
                    1,
                    TimeSpan.Zero,
                    CancellationToken.None,
                    TimeProvider.System));
            Assert.Throws<ArgumentNullException>(() =>
                CreateContext(
                    limits,
                    1,
                    1,
                    Timeout.InfiniteTimeSpan,
                    CancellationToken.None,
                    null));
        });
    }

    static int ApplyGsub(
        SfntGsubTable table,
        SfntLayoutGlyphBuffer buffer,
        SfntLayoutExecutionContext context)
    {
        return SfntGsubExecutor.ApplyLookup(
            table,
            0,
            buffer,
            null!,
            SfntLayoutTraversalDirection.Forward,
            1,
            context);
    }

    static SfntLayoutExecutionContext CreateContext(
        FontLimits limits,
        int maximumExpansion,
        int maximumBufferLength,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken,
        TimeProvider? timeProvider)
    {
        return new SfntLayoutExecutionContext(
            limits,
            maximumExpansion,
            maximumBufferLength,
            maximumDuration,
            cancellationToken,
            timeProvider!);
    }

    static SfntGsubTable CreateSingleGsub()
    {
        SfntLayoutLookup layoutLookup = CreateLayoutLookup(1);
        SfntGsubSingleSubstitution substitution =
            new SfntGsubSingleSubstitution(
                1,
                Coverage(1),
                1,
                new int[0]);
        SfntGsubLookup lookup = new SfntGsubLookup(
            layoutLookup,
            new SfntGsubSingleSubstitution[] { substitution },
            new SfntGsubMultipleSubstitution[0],
            new SfntGsubAlternateSubstitution[0],
            new SfntGsubLigatureSubstitution[0],
            new SfntGsubContextSubstitution[0],
            new SfntGsubChainedContextSubstitution[0],
            new SfntExtensionLookup[0],
            new SfntGsubReverseSubstitution[0],
            1);
        return new SfntGsubTable(
            CreateLayoutTable(layoutLookup),
            new SfntGsubLookup[] { lookup },
            FontLimits.CreateCompatible());
    }

    static SfntGsubTable CreateMultipleGsub()
    {
        SfntLayoutLookup layoutLookup = CreateLayoutLookup(2);
        SfntGsubMultipleSubstitution substitution =
            new SfntGsubMultipleSubstitution(
                Coverage(1),
                new SfntGsubGlyphSequence[]
                {
                    new SfntGsubGlyphSequence(new int[] { 1, 1, 1 })
                });
        SfntGsubLookup lookup = new SfntGsubLookup(
            layoutLookup,
            new SfntGsubSingleSubstitution[0],
            new SfntGsubMultipleSubstitution[] { substitution },
            new SfntGsubAlternateSubstitution[0],
            new SfntGsubLigatureSubstitution[0],
            new SfntGsubContextSubstitution[0],
            new SfntGsubChainedContextSubstitution[0],
            new SfntExtensionLookup[0],
            new SfntGsubReverseSubstitution[0],
            2);
        return new SfntGsubTable(
            CreateLayoutTable(layoutLookup),
            new SfntGsubLookup[] { lookup },
            FontLimits.CreateCompatible());
    }

    static SfntGposTable CreateSingleGpos()
    {
        SfntLayoutLookup layoutLookup = CreateLayoutLookup(1);
        SfntGposValueRecord value = new SfntGposValueRecord(
            1,
            5,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            null);
        SfntGposSingleAdjustment adjustment =
            new SfntGposSingleAdjustment(
                1,
                Coverage(1),
                1,
                new SfntGposValueRecord[] { value });
        SfntGposLookup lookup = new SfntGposLookup(
            layoutLookup,
            new SfntGposSingleAdjustment[] { adjustment },
            new SfntGposPairAdjustment[0],
            new SfntGposCursiveAdjustment[0],
            new SfntGposMarkAttachment[0],
            new SfntGposMarkLigatureAdjustment[0],
            new SfntGposMarkAttachment[0],
            new SfntGsubContextSubstitution[0],
            new SfntGsubChainedContextSubstitution[0],
            new SfntExtensionLookup[0],
            1);
        return new SfntGposTable(
            CreateLayoutTable(layoutLookup),
            new SfntGposLookup[] { lookup },
            FontLimits.CreateCompatible());
    }

    static SfntCoverageTable Coverage(int glyphId)
    {
        return new SfntCoverageTable(
            1,
            new int[] { glyphId },
            new SfntCoverageRange[0],
            1);
    }

    static SfntLayoutLookup CreateLayoutLookup(int lookupType)
    {
        return new SfntLayoutLookup(
            lookupType,
            0,
            new int[] { 0 },
            new int[] { 0 },
            0);
    }

    static SfntLayoutTable CreateLayoutTable(
        SfntLayoutLookup layoutLookup)
    {
        return new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(new SfntLayoutScript[0]),
            new SfntLayoutFeatureList(new SfntLayoutFeature[0]),
            new SfntLayoutLookupList(
                new SfntLayoutLookup[] { layoutLookup }),
            FontData.Create(new byte[0]).GetSlice(0, 0));
    }

    static FontLimits CreateLimits(
        int maximumOperations,
        int maximumNesting)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            1,
            10,
            65535,
            1000,
            100,
            10,
            10,
            maximumNesting,
            10,
            maximumOperations,
            1000,
            1000,
            1000000,
            100,
            100,
            10,
            10);
    }
}
