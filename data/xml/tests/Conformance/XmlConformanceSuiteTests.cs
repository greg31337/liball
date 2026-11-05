namespace xml.Tests;

[TestFixture]
public class XmlConformanceSuiteTests
{
    [Test]
    public void CompleteW3cXmlSuiteConformsForEveryApplicableParserProfile()
    {
        XmlConformanceSuiteAdapter suite = LoadSuite();
        XmlConformanceRunResult[] results = new XmlConformanceSuiteRunner(suite).RunAll();
        XmlConformanceRunResult[] failures = results.Where(result => result.Disposition == XmlConformanceRunDisposition.Failed).ToArray();
        XmlConformanceRunResult[] excluded = results.Where(result => result.Disposition == XmlConformanceRunDisposition.NotApplicable).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(suite.TestCases.Length * 2));
            Assert.That(results.Where(result => result.Disposition != XmlConformanceRunDisposition.NotApplicable).Select(result => result.Test.Classification).Distinct(), Is.EquivalentTo(Enum.GetValues<XmlConformanceClassification>()));
            Assert.That(results.Where(result => result.Disposition != XmlConformanceRunDisposition.NotApplicable).Select(result => result.Test.Version).Distinct(), Is.EquivalentTo(new[] { xml.XmlVersion.Xml10, xml.XmlVersion.Xml11 }));
            Assert.That(results.Where(result => result.Disposition != XmlConformanceRunDisposition.NotApplicable).Select(result => result.Test.NamespaceHandling).Distinct(), Is.EquivalentTo(new[] { xml.XmlNamespaceHandling.Disabled, xml.XmlNamespaceHandling.Namespaces10, xml.XmlNamespaceHandling.Namespaces11 }));
            Assert.That(excluded, Is.Not.Empty);
            Assert.That(excluded, Has.All.Matches<XmlConformanceRunResult>(result => result.Reason == XmlConformanceSuiteRunner.HistoricalEditionReason));
            Assert.That(failures, Is.Empty, FormatFailures(failures));
        });
    }

    private static string FormatFailures(XmlConformanceRunResult[] failures)
    {
        string[] groups = failures
            .GroupBy(result => result.Test.Classification + "/" + (result.Validating ? "validating" : "nonvalidating") + "/" + (result.Exception == null ? "accepted-or-diagnostic" : result.Exception.GetType().Name))
            .OrderByDescending(group => group.Count())
            .Select(group => group.Count() + " " + group.Key)
            .ToArray();
        string[] details = failures.Take(120).Select(result =>
            result.Test.Id + " [" + (result.Validating ? "validating" : "nonvalidating") + ", " + result.Test.Recommendation + ", " + result.Test.Classification + "] " + result.Reason + " Expected diagnostic: " + result.Test.ExpectedDiagnosticText).ToArray();
        string suffix = failures.Length > details.Length ? Environment.NewLine + "... " + (failures.Length - details.Length) + " additional failures" : "";
        return failures.Length + " XML conformance runs failed." + Environment.NewLine + string.Join(Environment.NewLine, groups) + Environment.NewLine + string.Join(Environment.NewLine, details) + suffix;
    }

    private static XmlConformanceSuiteAdapter LoadSuite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "xmlts-20130923.zip"));
        return new XmlConformanceSuiteAdapter(path);
    }
}
