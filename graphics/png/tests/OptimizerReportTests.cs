using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class OptimizerReportTests
{
    [Test]
    public void ReportModel_DefaultsToNoBestResult()
    {
        PngOptimizerReport report = new PngOptimizerReport();

        Assert.That(report.OriginalByteCount, Is.EqualTo(0));
        Assert.That(report.BestByteCount, Is.EqualTo(0));
        Assert.That(report.ByteSavings, Is.EqualTo(0));
        Assert.That(report.BestEncodedBytes, Is.Not.Null);
        Assert.That(report.BestEncodedBytes, Is.Empty);
        Assert.That(report.FoundBestResult, Is.False);
        Assert.That(report.BestFromCompressionTrial, Is.False);
        Assert.That(report.BestFromFilterStrategyTrial, Is.False);
        Assert.That(report.BestCompressionLevel, Is.EqualTo(0));
        Assert.That(report.BestFilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(report.BestUsesFixedFilter, Is.False);
        Assert.That(report.CompressionTrialCount, Is.EqualTo(0));
        Assert.That(report.FilterStrategyTrialCount, Is.EqualTo(0));
        Assert.That(report.BailedOutTrialCount, Is.EqualTo(0));
    }

    [Test]
    public void Create_SelectsCompressionTrialWhenItIsSmallest()
    {
        PngCompressionTrialResult compression = CreateCompressionResult(CreateCompressionTrial(4, 80, new byte[] { 1, 2, 3 }), 3, 1);
        PngFilterStrategyTrialResult filters = CreateFilterResult(CreateFilterTrial(PngFilterType.Paeth, true, 100, new byte[] { 4, 5 }), 6, 2);

        PngOptimizerReport report = PngOptimizerReportBuilder.Create(120, compression, filters);

        Assert.That(report.FoundBestResult, Is.True);
        Assert.That(report.BestFromCompressionTrial, Is.True);
        Assert.That(report.BestFromFilterStrategyTrial, Is.False);
        Assert.That(report.BestCompressionLevel, Is.EqualTo(4));
        Assert.That(report.BestByteCount, Is.EqualTo(80));
        Assert.That(report.ByteSavings, Is.EqualTo(40));
        Assert.That(report.BestEncodedBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(report.CompressionTrialCount, Is.EqualTo(3));
        Assert.That(report.FilterStrategyTrialCount, Is.EqualTo(6));
        Assert.That(report.BailedOutTrialCount, Is.EqualTo(3));
    }

    [Test]
    public void Create_SelectsFilterStrategyTrialWhenItIsSmallestAndCopiesPayload()
    {
        byte[] filterBytes = new byte[] { 9, 8, 7 };
        PngCompressionTrialResult compression = CreateCompressionResult(CreateCompressionTrial(9, 90, new byte[] { 1, 2 }), 2, 0);
        PngFilterStrategyTrialResult filters = CreateFilterResult(CreateFilterTrial(PngFilterType.Sub, true, 70, filterBytes), 6, 1);

        PngOptimizerReport report = PngOptimizerReportBuilder.Create(new byte[100], compression, filters);
        filterBytes[0] = 99;

        Assert.That(report.FoundBestResult, Is.True);
        Assert.That(report.BestFromCompressionTrial, Is.False);
        Assert.That(report.BestFromFilterStrategyTrial, Is.True);
        Assert.That(report.BestCompressionLevel, Is.EqualTo(7));
        Assert.That(report.BestFilterType, Is.EqualTo(PngFilterType.Sub));
        Assert.That(report.BestUsesFixedFilter, Is.True);
        Assert.That(report.BestByteCount, Is.EqualTo(70));
        Assert.That(report.ByteSavings, Is.EqualTo(30));
        Assert.That(report.BestEncodedBytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(report.BailedOutTrialCount, Is.EqualTo(1));
    }

    [Test]
    public void Create_ReportsNoBestResultWhenNoTrialRetainedPayload()
    {
        PngCompressionTrialResult compression = CreateCompressionResult(CreateCompressionTrial(9, 80, new byte[0]), 1, 1);
        PngFilterStrategyTrialResult filters = CreateFilterResult(CreateFilterTrial(PngFilterType.None, false, 90, new byte[0]), 1, 1);

        PngOptimizerReport report = PngOptimizerReportBuilder.Create(100, compression, filters);

        Assert.That(report.FoundBestResult, Is.False);
        Assert.That(report.BestEncodedBytes, Is.Empty);
        Assert.That(report.BestByteCount, Is.EqualTo(0));
        Assert.That(report.ByteSavings, Is.EqualTo(0));
        Assert.That(report.CompressionTrialCount, Is.EqualTo(1));
        Assert.That(report.FilterStrategyTrialCount, Is.EqualTo(1));
        Assert.That(report.BailedOutTrialCount, Is.EqualTo(2));
    }

    [Test]
    public void Create_AllowsNegativeSavingsWhenBestIsLargerThanOriginal()
    {
        PngCompressionTrialResult compression = CreateCompressionResult(CreateCompressionTrial(9, 140, new byte[] { 1 }), 1, 0);
        PngFilterStrategyTrialResult filters = CreateFilterResult(CreateFilterTrial(PngFilterType.None, false, 150, new byte[] { 2 }), 1, 0);

        PngOptimizerReport report = PngOptimizerReportBuilder.Create(100, compression, filters);

        Assert.That(report.ByteSavings, Is.EqualTo(-40));
    }

    [Test]
    public void Create_RejectsInvalidInputs()
    {
        PngCompressionTrialResult compression = CreateCompressionResult(CreateCompressionTrial(9, 80, new byte[] { 1 }), 1, 0);
        PngFilterStrategyTrialResult filters = CreateFilterResult(CreateFilterTrial(PngFilterType.None, false, 90, new byte[] { 2 }), 1, 0);
        PngCompressionTrialResult nullCompressionTrials = CreateCompressionResult(CreateCompressionTrial(9, 80, new byte[] { 1 }), 1, 0);
        PngFilterStrategyTrialResult nullFilterTrials = CreateFilterResult(CreateFilterTrial(PngFilterType.None, false, 90, new byte[] { 2 }), 1, 0);
        PngCompressionTrialResult nullCompressionBest = CreateCompressionResult(null!, 1, 0);
        PngFilterStrategyTrialResult nullFilterBest = CreateFilterResult(null!, 1, 0);
        PngCompressionTrialResult nullCompressionBytes = CreateCompressionResult(CreateCompressionTrial(9, 80, null!), 1, 0);
        PngFilterStrategyTrialResult nullFilterBytes = CreateFilterResult(CreateFilterTrial(PngFilterType.None, false, 90, null!), 1, 0);
        nullCompressionTrials.Trials = null!;
        nullFilterTrials.Trials = null!;

        Assert.Throws<ArgumentOutOfRangeException>(() => PngOptimizerReportBuilder.Create(-1, compression, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(null!, compression, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, null!, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, compression, null!));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, nullCompressionTrials, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, compression, nullFilterTrials));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, nullCompressionBest, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, compression, nullFilterBest));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, nullCompressionBytes, filters));
        Assert.Throws<ArgumentNullException>(() => PngOptimizerReportBuilder.Create(0, compression, nullFilterBytes));
    }

    static PngCompressionTrial CreateCompressionTrial(int compressionLevel, long byteCount, byte[] encodedBytes)
    {
        PngCompressionTrial trial = new PngCompressionTrial();
        trial.CompressionLevel = compressionLevel;
        trial.ByteCount = byteCount;
        trial.EncodedBytes = encodedBytes;
        return trial;
    }

    static PngFilterStrategyTrial CreateFilterTrial(PngFilterType filterType, bool useFixedFilter, long byteCount, byte[] encodedBytes)
    {
        PngFilterStrategyTrial trial = new PngFilterStrategyTrial();
        trial.CompressionLevel = 7;
        trial.FilterType = filterType;
        trial.UseFixedFilter = useFixedFilter;
        trial.ByteCount = byteCount;
        trial.EncodedBytes = encodedBytes;
        return trial;
    }

    static PngCompressionTrialResult CreateCompressionResult(PngCompressionTrial bestTrial, int trialCount, int bailedOutTrialCount)
    {
        PngCompressionTrialResult result = new PngCompressionTrialResult();
        result.Trials = new PngCompressionTrial[trialCount];
        result.BestTrial = bestTrial;
        result.BailedOutTrialCount = bailedOutTrialCount;
        return result;
    }

    static PngFilterStrategyTrialResult CreateFilterResult(PngFilterStrategyTrial bestTrial, int trialCount, int bailedOutTrialCount)
    {
        PngFilterStrategyTrialResult result = new PngFilterStrategyTrialResult();
        result.Trials = new PngFilterStrategyTrial[trialCount];
        result.BestTrial = bestTrial;
        result.BailedOutTrialCount = bailedOutTrialCount;
        return result;
    }
}
