using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FilterTrialScorerTests
{
    [Test]
    public void TrialModels_DefaultToEmptyResults()
    {
        PngFilterTrial trial = new PngFilterTrial();
        PngFilterTrialResult result = new PngFilterTrialResult();

        Assert.That(trial.FilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(trial.FilteredRow, Is.Not.Null);
        Assert.That(trial.FilteredRow, Is.Empty);
        Assert.That(trial.Score, Is.EqualTo(0));
        Assert.That(result.Trials, Is.Not.Null);
        Assert.That(result.Trials, Is.Empty);
        Assert.That(result.BestTrial, Is.Not.Null);
        Assert.That(result.TriedAllFilters, Is.False);
        Assert.That(result.UsedFixedFilter, Is.False);
    }

    [Test]
    public void ScoreAll_ReturnsEveryFilterTrialAndChoosesLowestScore()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };

        PngFilterTrialResult result = PngFilterTrialScorer.ScoreAll(rowData, 1);

        Assert.That(result.TriedAllFilters, Is.True);
        Assert.That(result.UsedFixedFilter, Is.False);
        Assert.That(result.Trials, Has.Length.EqualTo(5));
        Assert.That(result.Trials[0].FilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(result.Trials[1].FilterType, Is.EqualTo(PngFilterType.Sub));
        Assert.That(result.Trials[2].FilterType, Is.EqualTo(PngFilterType.Up));
        Assert.That(result.Trials[3].FilterType, Is.EqualTo(PngFilterType.Average));
        Assert.That(result.Trials[4].FilterType, Is.EqualTo(PngFilterType.Paeth));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Sub));
        Assert.That(result.BestTrial.Score, Is.EqualTo(22));
        Assert.That(result.BestTrial.FilteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void ScoreAll_UsesPreviousRowWhenScoringTrials()
    {
        byte[] rowData = new byte[] { 11, 22, 4 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        PngFilterTrialResult result = PngFilterTrialScorer.ScoreAll(rowData, 3, previousRow);

        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Up));
        Assert.That(result.BestTrial.Score, Is.EqualTo(13));
        Assert.That(result.BestTrial.FilteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 }));
    }

    [Test]
    public void ScoreAll_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 10, 15, 9, 10, 88 };

        PngFilterTrialResult result = PngFilterTrialScorer.ScoreAll(rowData, 1, 4, 1);

        Assert.That(result.Trials, Has.Length.EqualTo(5));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Sub));
        Assert.That(result.BestTrial.FilteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void Score_UsesExhaustiveTrialsWhenOptimizerTryFiltersIsEnabled()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };
        PngOptimizerOptions options = new PngOptimizerOptions();

        PngFilterTrialResult result = PngFilterTrialScorer.Score(rowData, 1, options);

        Assert.That(result.TriedAllFilters, Is.True);
        Assert.That(result.Trials, Has.Length.EqualTo(5));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Sub));
    }

    [Test]
    public void Score_UsesSingleFixedTrialWhenOptimizerUsesFixedFilter()
    {
        byte[] rowData = new byte[] { 10, 20, 35, 27, 38, 51 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.Average;

        PngFilterTrialResult result = PngFilterTrialScorer.Score(rowData, 3, previousRow, options);

        Assert.That(result.TriedAllFilters, Is.False);
        Assert.That(result.UsedFixedFilter, Is.True);
        Assert.That(result.Trials, Has.Length.EqualTo(1));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Average));
        Assert.That(result.BestTrial.FilteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void Score_UsesSingleConfiguredTrialWhenFilterTrialsAreDisabled()
    {
        byte[] rowData = new byte[] { 10, 20, 30, 15, 25, 35 };
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.TryFilters = false;
        options.FixedFilterType = PngFilterType.Paeth;

        PngFilterTrialResult result = PngFilterTrialScorer.Score(rowData, 3, options);

        Assert.That(result.TriedAllFilters, Is.False);
        Assert.That(result.UsedFixedFilter, Is.False);
        Assert.That(result.Trials, Has.Length.EqualTo(1));
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Paeth));
        Assert.That(result.BestTrial.FilteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void Score_AppliesFastAndExplicitFixedStrategies()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };
        byte[] previousRow = new byte[] { 9, 12, 7, 9 };
        PngOptimizerOptions fast = new PngOptimizerOptions();
        fast.RowFilterStrategy = PngRowFilterStrategy.Fast;
        PngOptimizerOptions fixedOptions = new PngOptimizerOptions();
        fixedOptions.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        fixedOptions.FixedFilterType = PngFilterType.None;

        PngFilterTrialResult fastFirst = PngFilterTrialScorer.Score(rowData, 1, fast);
        PngFilterTrialResult fastLater = PngFilterTrialScorer.Score(rowData, 1, previousRow, fast);
        PngFilterTrialResult fixedResult = PngFilterTrialScorer.Score(rowData, 1, fixedOptions);

        Assert.That(fastFirst.Trials, Has.Length.EqualTo(1));
        Assert.That(fastFirst.BestTrial.FilterType, Is.EqualTo(PngFilterType.Sub));
        Assert.That(fastLater.BestTrial.FilterType, Is.EqualTo(PngFilterType.Up));
        Assert.That(fixedResult.UsedFixedFilter, Is.True);
        Assert.That(fixedResult.BestTrial.FilterType, Is.EqualTo(PngFilterType.None));
    }

    [Test]
    public void Score_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };
        byte[] previousRow = new byte[] { 1, 2 };
        PngOptimizerOptions options = new PngOptimizerOptions();
        PngOptimizerOptions invalidFixed = new PngOptimizerOptions();
        PngOptimizerOptions invalidStrategy = new PngOptimizerOptions();
        invalidFixed.UseFixedFilter = true;
        invalidFixed.FixedFilterType = (PngFilterType)99;
        invalidStrategy.RowFilterStrategy = (PngRowFilterStrategy)99;

        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.ScoreAll(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.ScoreAll(null!, 1, previousRow));
        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.ScoreAll(rowData, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterTrialScorer.ScoreAll(rowData, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterTrialScorer.ScoreAll(rowData, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterTrialScorer.ScoreAll(rowData, 0, 2, 1, new byte[1]));
        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.Score(null!, 1, options));
        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.Score(rowData, 1, (PngOptimizerOptions)null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterTrialScorer.Score(rowData, 1, previousRow, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterTrialScorer.Score(rowData, 1, invalidFixed));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterTrialScorer.Score(rowData, 1, invalidStrategy));
    }
}
