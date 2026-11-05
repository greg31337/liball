namespace xml.Tests;

[TestFixture]
public class Qt3SuiteTests
{
    [Test]
    public void CompleteApplicableQt3SuiteConforms()
    {
        Qt3SuiteAdapter suite = LoadSuite();
        Qt3RunResult[] results = new Qt3SuiteRunner(suite).RunAll();
        Qt3RunResult[] failures = results.Where(result => result.Disposition == Qt3RunDisposition.Failed).ToArray();
        Qt3RunResult[] excluded = results.Where(result => result.Disposition == Qt3RunDisposition.NotApplicable).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(suite.TestCases.Length));
            Assert.That(excluded.Where(result => result.Test.Applicability == Qt3LanguageApplicability.XQueryOnly), Has.All.Matches<Qt3RunResult>(result => result.Reason == Qt3SuiteAdapter.XQueryOnlyReason));
            Assert.That(excluded.Where(result => result.Test.Applicability == Qt3LanguageApplicability.OtherXPathVersion), Has.All.Matches<Qt3RunResult>(result => result.Reason == Qt3SuiteAdapter.OtherXPathVersionReason));
            Assert.That(failures.Length, Is.EqualTo(0), FormatFailures(failures));
        });
    }

    private static string FormatFailures(Qt3RunResult[] failures)
    {
        string[] groups = failures
            .GroupBy(result => result.Test.Assertion.Kind + "/" + (result.Exception == null ? "assertion" : result.Exception.GetType().Name) + "/" + result.ErrorCode)
            .OrderByDescending(group => group.Count())
            .Take(80)
            .Select(group => group.Count() + " " + group.Key)
            .ToArray();
        string[] details = failures.Take(160).Select(result =>
            result.Test.TestSetName + "/" + result.Test.Name + " [" + result.Test.Assertion.Kind + "] " + result.Reason + " " + result.ErrorCode + " " + result.Exception?.Message).ToArray();
        return failures.Length + " applicable QT3 tests failed." + Environment.NewLine + string.Join(Environment.NewLine, groups) + Environment.NewLine + string.Join(Environment.NewLine, details);
    }

    private static Qt3SuiteAdapter LoadSuite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "qt3tests-master.zip"));
        return new Qt3SuiteAdapter(path);
    }
}
