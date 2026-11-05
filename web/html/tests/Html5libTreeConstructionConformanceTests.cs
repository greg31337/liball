
namespace UniversalHtml.Tests;

[TestFixture]
public class Html5libTreeConstructionConformanceTests
{
    [Test]
    public void PinnedTreesReportEveryCaseByInsertionModeAndNamespace()
    {
        Html5libTreeReport report =
            Html5libTreeConstructionAdapter.RunPinnedSuite();
        TestContext.Out.WriteLine(report.FormatSummary());

        Assert.Multiple(() =>
        {
            Assert.That(Format(report.GetOverallSummary()),
                Is.EqualTo("Overall:3397:152:4"), report.FormatSummary());
            Assert.That(report.GetInsertionModeSummaries().Select(Format),
                Is.EqualTo(new[]
            {
                "Initial:3021:144:4",
                "BeforeHtml:3021:144:4",
                "BeforeHead:3035:144:4",
                "InHead:3039:144:4",
                "InHeadNoScript:26:0:0",
                "AfterHead:3035:144:4",
                "InBody:3329:146:4",
                "Text:638:10:4",
                "InTable:548:38:0",
                "InTableText:84:8:0",
                "InCaption:80:2:0",
                "InColumnGroup:60:2:0",
                "InTableBody:318:20:0",
                "InRow:298:22:0",
                "InCell:212:22:0",
                "InSelect:82:78:0",
                "InSelectInTable:34:20:0",
                "InTemplate:214:8:0",
                "AfterBody:216:0:0",
                "InFrameset:114:6:0",
                "AfterFrameset:62:4:0",
                "AfterAfterBody:154:0:0",
                "AfterAfterFrameset:26:0:0"
            }), report.FormatSummary());
            Assert.That(report.GetNamespaceSummaries().Select(Format),
                Is.EqualTo(new[]
            {
                "Html:3333:152:4",
                "Svg:326:10:0",
                "MathMl:228:12:0"
            }), report.FormatSummary());
            Assert.That(report.GetOverallSummary().GetTotal(),
                Is.EqualTo(3553), report.FormatSummary());
            Assert.That(report.GetRuns().Where(value =>
                value.Disposition == Html5libTreeDisposition.Skipped)
                .Select(value => value.FileName).Distinct(),
                Is.EqualTo(new[]
            {
                Path.Combine("scripted", "adoption01.dat"),
                Path.Combine("scripted", "ark.dat"),
                Path.Combine("scripted", "webkit01.dat")
            }), report.FormatSummary());
            Assert.That(report.GetRuns()
                .Select(value => value.InvariantViolation)
                .Where(value => value.Length > 0), Is.Empty,
                "Every completed fixture tree must satisfy DOM invariants.");
        });
    }

    static string Format(Html5libTreeSummary summary)
    {
        return summary.Category + ":" + summary.Passed + ":"
            + summary.Failed + ":" + summary.Skipped;
    }
}
