using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFilterPipelineTests
{
    [Test]
    public void AddFilter_AppendsFiltersAndDecides()
    {
        LogFilterPipeline pipeline = new LogFilterPipeline();
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        pipeline.AddFilter(LogFilter.MinimumLevel(LogLevel.Warn));

        Assert.That(pipeline.Filters.Length, Is.EqualTo(1));
        Assert.That(pipeline.Decide(infoEvent), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void SetFilters_CopiesFilterArray()
    {
        LogFilterPipeline pipeline = new LogFilterPipeline();
        LogFilter[] filters = new LogFilter[] { LogFilter.MinimumLevel(LogLevel.Warn) };

        pipeline.SetFilters(filters);
        filters[0] = LogFilter.MinimumLevel(LogLevel.Trace);

        Assert.That(pipeline.Filters[0].Level, Is.EqualTo(LogLevel.Warn));
    }

    [Test]
    public void ClearFilters_RemovesFilters()
    {
        LogFilterPipeline pipeline = new LogFilterPipeline();

        pipeline.AddFilter(LogFilter.MinimumLevel(LogLevel.Warn));
        pipeline.ClearFilters();

        Assert.That(pipeline.Filters.Length, Is.EqualTo(0));
    }
}
