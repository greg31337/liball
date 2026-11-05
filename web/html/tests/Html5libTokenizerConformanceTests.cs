
namespace UniversalHtml.Tests;

[TestFixture]
public class Html5libTokenizerConformanceTests
{
    [Test]
    public void PinnedTokenOutputsReportEveryCaseByInitialTokenizerState()
    {
        Html5libTokenizerReport report = Html5libTokenizerAdapter.RunPinnedSuite();
        Html5libTokenizerStateSummary[] summaries = report.GetStateSummaries();
        TestContext.Out.WriteLine(report.FormatSummary());

        Assert.That(summaries.Select(value => value.State + ":"
            + value.Passed + ":" + value.Failed + ":" + value.Skipped),
            Is.EqualTo(new[]
        {
            "CDATA section state:3:53:0",
            "Data state:6682:8:4",
            "PLAINTEXT state:52:0:0",
            "RAWTEXT state:69:2:0",
            "RCDATA state:72:2:0",
            "Script data state:89:0:0"
        }), report.FormatSummary());
        Assert.That(summaries.Sum(value => value.GetTotal()), Is.EqualTo(7036),
            report.FormatSummary());
    }
}
