namespace xml.Tests;

[TestFixture]
public class Xslt30SuiteTests
{
    [Test]
    public void ClassifiesCompleteCatalogAndExecutesEveryConformanceProfile()
    {
        using Xslt30SuiteAdapter suite = LoadSuite();
        Xslt30SuiteRunner runner = new Xslt30SuiteRunner(suite);
        Xslt30Profile[] profiles = suite.TestCases.Select(runner.ClassifyProfile).ToArray();
        string[] exclusions = suite.TestCases.Select(runner.ClassifyExclusion).Where(reason => reason.Length != 0).ToArray();
        Xslt30RunResult[] results =
        [
            runner.Run(suite.TestCases.Single(test => test.Name == "as-0102")),
            runner.Run(suite.TestCases.Single(test => test.Name == "as-0142")),
            runner.Run(suite.TestCases.Single(test => test.Name == "mode-1406")),
            runner.Run(suite.TestCases.First(test => test.TestSetName == "load-xquery-module"))
        ];
        Xslt30RunResult[] failures = results.Where(result => result.Disposition == Xslt30RunDisposition.Failed).ToArray();
        Xslt30RunResult[] excluded = results.Where(result => result.Disposition == Xslt30RunDisposition.NotApplicable).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(profiles, Has.Length.EqualTo(suite.TestCases.Length));
            Assert.That(profiles, Does.Contain(Xslt30Profile.Basic));
            Assert.That(profiles, Does.Contain(Xslt30Profile.SchemaAware));
            Assert.That(profiles, Does.Contain(Xslt30Profile.Streaming));
            Assert.That(exclusions, Has.All.Matches<string>(reason => ExactExclusionReasons().Contains(reason) || reason.StartsWith(Xslt30SuiteRunner.DependencyMismatchPrefix, StringComparison.Ordinal)));
            Assert.That(excluded.Single().Reason, Is.EqualTo(Xslt30SuiteRunner.XQueryModuleReason));
            Assert.That(failures, Is.Empty, FormatFailures(failures));
        });
    }

    private static string[] ExactExclusionReasons()
    {
        return
        [
            Xslt30SuiteRunner.XQueryModuleReason,
            Xslt30SuiteRunner.OlderXsltReason,
            Xslt30SuiteRunner.ExternalDocumentReason,
            Xslt30SuiteRunner.ExtensionFunctionReason
        ];
    }

    private static string FormatFailures(Xslt30RunResult[] failures)
    {
        string[] groups = failures.GroupBy(result => result.Profile + "/" + result.Test.Assertion.Kind + "/" + (result.Exception == null ? "assertion" : result.Exception.GetType().Name) + "/" + result.ErrorCode)
            .OrderByDescending(group => group.Count()).Take(100).Select(group => group.Count() + " " + group.Key).ToArray();
        string[] details = failures.Take(200).Select(result => result.Profile + "/" + result.Test.TestSetName + "/" + result.Test.Name + " [" + result.Test.Assertion.Kind + "] " + result.Reason + " " + result.ErrorCode + " " + result.Exception?.Message).ToArray();
        return failures.Length + " applicable XSLT 3.0 tests failed." + Environment.NewLine + string.Join(Environment.NewLine, groups) + Environment.NewLine + string.Join(Environment.NewLine, details);
    }

    private static Xslt30SuiteAdapter LoadSuite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "xslt30-test-master.zip"));
        return new Xslt30SuiteAdapter(path);
    }
}
