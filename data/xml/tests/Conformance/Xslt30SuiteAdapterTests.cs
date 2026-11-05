namespace xml.Tests;

[TestFixture]
public class Xslt30SuiteAdapterTests
{
    [Test]
    public void LoadsCompleteCatalogAndPreservesExecutionMetadata()
    {
        using Xslt30SuiteAdapter suite = LoadSuite();
        Xslt30TestCase initialFunction = suite.TestCases.Single(test => test.Name == "initial-function-001");
        Xslt30CatalogNode function = initialFunction.Invocation.InitialFunction!;
        Xslt30TestCase resultDocument = suite.TestCases.Single(test => test.Name == "result-document-0101");
        Xslt30Assertion secondary = resultDocument.Assertion.Children.Single(assertion => assertion.Kind == "assert-result-document");
        Xslt30CatalogNode stylesheet = initialFunction.Invocation.Stylesheets.Single();
        string stylesheetText = suite.ReadText(stylesheet.FilePath!);
        string[] assertionKinds = suite.TestCases.SelectMany(test => Flatten(test.Assertion)).Select(assertion => assertion.Kind).Distinct().Order().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(suite.TestSets, Has.Length.EqualTo(234));
            Assert.That(suite.TestCases, Has.Length.EqualTo(14601));
            Assert.That(suite.TestCases.Any(test => test.Environment.Find("source").Length != 0), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Environment.Find("schema").Length != 0), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Environment.Find("collection").Length != 0), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Invocation.Packages.Length != 0), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Invocation.InitialTemplate != null), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Invocation.InitialMode != null), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Invocation.InitialFunction != null), Is.True);
            Assert.That(suite.TestCases.Any(test => test.Invocation.Parameters.Length != 0), Is.True);
            Assert.That(function.Attributes["name"], Is.EqualTo("my:foo"));
            Assert.That(function.NameNamespaceUri, Is.EqualTo("http://my.net/"));
            Assert.That(function.Find("param").Select(parameter => parameter.Attributes["select"]), Is.EqualTo(new[] { "1234", "5678" }));
            Assert.That(secondary.Attributes["uri"], Is.EqualTo("multresult1.out"));
            Assert.That(secondary.Children.Single().Kind, Is.EqualTo("assert-xml"));
            Assert.That(assertionKinds, Is.SubsetOf(Xslt30SuiteAdapter.AssertionKinds));
            Assert.That(assertionKinds, Does.Contain("assert-message"));
            Assert.That(assertionKinds, Does.Contain("assert-result-document"));
            Assert.That(assertionKinds, Does.Contain("assert-serialization"));
            Assert.That(assertionKinds, Does.Contain("serialization-matches"));
            Assert.That(initialFunction.Dependencies.Any(dependency => dependency.Kind == "spec" && dependency.Value == "XSLT30+" && dependency.Satisfied), Is.True);
            Assert.That(suite.ContainsResource(stylesheet.FilePath!), Is.True);
            Assert.That(stylesheetText, Does.Contain("xsl:"));
        });
    }

    private static IEnumerable<Xslt30Assertion> Flatten(Xslt30Assertion assertion)
    {
        yield return assertion;
        int index = 0;
        while (index < assertion.Children.Length)
        {
            foreach (Xslt30Assertion child in Flatten(assertion.Children[index]))
            {
                yield return child;
            }

            index = index + 1;
        }
    }

    private static Xslt30SuiteAdapter LoadSuite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "xslt30-test-master.zip"));
        return new Xslt30SuiteAdapter(path);
    }
}
