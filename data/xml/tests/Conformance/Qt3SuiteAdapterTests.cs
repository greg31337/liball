namespace xml.Tests;

[TestFixture]
public class Qt3SuiteAdapterTests
{
    private Qt3SuiteAdapter _suite = null!;

    [OneTimeSetUp]
    public void LoadQt3Suite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "qt3tests-master.zip"));
        _suite = new Qt3SuiteAdapter(path);
    }

    [Test]
    public void LoadsCompleteCatalogEnvironmentsDependenciesAndExternalResources()
    {
        Qt3TestCase[] externalExpressions = _suite.TestCases.Where(test => test.ExpressionPath != null).ToArray();
        Qt3TestCase[] externalAssertions = _suite.TestCases.Where(test => HasExternalAssertion(test.Assertion)).ToArray();
        string[] environmentKinds = _suite.GlobalEnvironments.Values
            .SelectMany(environment => environment.Nodes)
            .Concat(_suite.TestCases.SelectMany(test => test.Environment.Nodes))
            .Select(node => node.Kind)
            .Distinct()
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(_suite.TestCases.Length, Is.EqualTo(31821));
            Assert.That(_suite.GlobalEnvironments.Keys, Does.Contain("empty"));
            Assert.That(_suite.GlobalEnvironments.Keys, Does.Contain("atomic"));
            Assert.That(_suite.TestCases, Has.All.Matches<Qt3TestCase>(test => test.Name.Length != 0 && test.TestSetName.Length != 0 && test.TestSetPath.Length != 0));
            Assert.That(_suite.TestCases.SelectMany(test => test.Dependencies).Select(dependency => dependency.Type).Distinct(), Does.Contain("spec"));
            Assert.That(environmentKinds, Does.Contain("namespace"));
            Assert.That(environmentKinds, Does.Contain("source"));
            Assert.That(environmentKinds, Does.Contain("schema"));
            Assert.That(environmentKinds, Does.Contain("resource"));
            Assert.That(environmentKinds, Does.Contain("collection"));
            Assert.That(environmentKinds, Does.Contain("param"));
            Assert.That(externalExpressions, Is.Not.Empty);
            Assert.That(externalExpressions, Has.All.Matches<Qt3TestCase>(test => _suite.Resources.ContainsKey(test.ExpressionPath!)));
            Assert.That(externalAssertions, Is.Not.Empty);
            Assert.That(externalAssertions, Has.All.Matches<Qt3TestCase>(test => ExternalAssertions(test.Assertion).All(assertion => _suite.Resources.ContainsKey(assertion.FilePath!))));
        });
    }

    [Test]
    public void RecognizesEveryAssertionFormUsedByQt3()
    {
        string[] actual = _suite.TestCases
            .SelectMany(test => Flatten(test.Assertion))
            .Select(assertion => assertion.Kind)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SubsetOf(Qt3SuiteAdapter.AssertionKinds));
            Assert.That(Qt3SuiteAdapter.AssertionKinds, Has.All.Matches<string>(_suite.SupportsAssertion));
            Assert.That(actual.Length, Is.EqualTo(Qt3SuiteAdapter.AssertionKinds.Length - 1));
            Assert.That(actual, Does.Not.Contain("assert-serialization"), "The archived catalog contains assert-serialization only in commented examples.");
        });
    }

    [Test]
    public void ClassifiesXPath31XQueryOnlyAndEarlierXPathCasesExplicitly()
    {
        Qt3TestCase xpath = Find("fn-abs", "fn-absint1args-2");
        Qt3TestCase xquery = Find("fn-abs", "fn-absint1args-1-dyn");
        Qt3TestCase earlierXPath = Find("prod-FunctionCall", "function-call-reserved-function-names-034");

        Assert.Multiple(() =>
        {
            Assert.That(xpath.Applicability, Is.EqualTo(Qt3LanguageApplicability.XPath31));
            Assert.That(xpath.ApplicabilityReason, Is.Empty);
            Assert.That(xquery.Applicability, Is.EqualTo(Qt3LanguageApplicability.XQueryOnly));
            Assert.That(xquery.ApplicabilityReason, Is.EqualTo(Qt3SuiteAdapter.XQueryOnlyReason));
            Assert.That(earlierXPath.Applicability, Is.EqualTo(Qt3LanguageApplicability.OtherXPathVersion));
            Assert.That(earlierXPath.ApplicabilityReason, Is.EqualTo(Qt3SuiteAdapter.OtherXPathVersionReason));
        });
    }

    [Test]
    public void EvaluatesRepresentativeXPath31TestsWithCatalogAndLocalEnvironments()
    {
        Qt3SuiteRunner runner = new Qt3SuiteRunner(_suite);
        Qt3RunResult integer = runner.Run(Find("fn-abs", "fn-absint1args-2"));
        Qt3RunResult trueValue = runner.Run(Find("fn-true", "fn-true-1"));
        Qt3RunResult falseValue = runner.Run(Find("fn-true", "fn-true-2"));
        Qt3RunResult localSource = runner.Run(Find("fn-abs", "K2-ABSFunc-35"));
        Qt3RunResult xquery = runner.Run(Find("fn-abs", "fn-absint1args-1-dyn"));

        Assert.Multiple(() =>
        {
            Assert.That(integer.Disposition, Is.EqualTo(Qt3RunDisposition.Passed), integer.Reason);
            Assert.That(trueValue.Disposition, Is.EqualTo(Qt3RunDisposition.Passed), trueValue.Reason);
            Assert.That(falseValue.Disposition, Is.EqualTo(Qt3RunDisposition.Passed), falseValue.Reason);
            Assert.That(localSource.Disposition, Is.EqualTo(Qt3RunDisposition.Passed), localSource.Reason);
            Assert.That(xquery.Disposition, Is.EqualTo(Qt3RunDisposition.NotApplicable));
            Assert.That(xquery.Reason, Is.EqualTo(Qt3SuiteAdapter.XQueryOnlyReason));
        });
    }

    [Test]
    public void EvaluatesEverySupportedAssertionForm()
    {
        Qt3SuiteRunner runner = new Qt3SuiteRunner(_suite);
        List<Qt3TestCase> tests =
        [
            Case("assert", "2", "$result = 2"),
            Case("assert-count", "(1, 2)", "2"),
            Case("assert-deep-eq", "(1, 2)", "(1, 2)"),
            Case("assert-empty", "()", ""),
            Case("assert-eq", "2", "2"),
            Case("assert-false", "false()", ""),
            Case("assert-permutation", "(2, 1)", "(1, 2)"),
            Case("assert-serialization", "'text'", "text"),
            Case("assert-serialization-error", "fn:true#0", "", new Dictionary<string, string> { ["code"] = "*" }),
            Case("assert-string-value", "(1, 2)", "1 2"),
            Case("assert-true", "true()", ""),
            Case("assert-type", "2", "xs:integer"),
            Case("error", "1 idiv 0", "", new Dictionary<string, string> { ["code"] = "FOAR0001" }),
            Case("serialization-matches", "'text'", "^text$"),
            CompositeCase("all-of", CaseAssertion("assert-true", ""), CaseAssertion("assert-type", "xs:boolean")),
            CompositeCase("any-of", CaseAssertion("assert-false", ""), CaseAssertion("assert-true", "")),
            CompositeCase("not", CaseAssertion("assert-false", ""))
        ];
        Qt3TestCase xml = Case("assert-xml", "/*", "<e>0</e>");
        xml.Environment = Find("fn-abs", "K2-ABSFunc-35").Environment;
        tests.Add(xml);

        Qt3RunResult[] results = tests.Select(runner.Run).ToArray();
        Qt3RunResult[] failures = results.Where(result => result.Disposition != Qt3RunDisposition.Passed).ToArray();

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Select(result => result.Test.Assertion.Kind + ": " + result.Reason + " Code=" + result.ErrorCode + " Exception=" + result.Exception?.GetType().Name)));
    }

    private Qt3TestCase Find(string testSet, string name)
    {
        return _suite.TestCases.Single(test => test.TestSetName == testSet && test.Name == name);
    }

    private Qt3TestCase Case(string assertionKind, string expression, string assertionValue, Dictionary<string, string>? attributes = null)
    {
        return new Qt3TestCase
        {
            Name = "adapter-" + assertionKind,
            TestSetName = "adapter",
            TestSetPath = _suite.CatalogPath,
            Expression = expression,
            Environment = _suite.GlobalEnvironments["empty"],
            Assertion = new Qt3Assertion
            {
                Kind = assertionKind,
                Value = assertionValue,
                Attributes = attributes ?? new Dictionary<string, string>()
            },
            Applicability = Qt3LanguageApplicability.XPath31
        };
    }

    private Qt3TestCase CompositeCase(string assertionKind, params Qt3Assertion[] children)
    {
        Qt3TestCase test = Case(assertionKind, "true()", "");
        test.Assertion.Children = children;
        return test;
    }

    private static Qt3Assertion CaseAssertion(string kind, string value)
    {
        return new Qt3Assertion { Kind = kind, Value = value };
    }

    private static bool HasExternalAssertion(Qt3Assertion assertion)
    {
        return Flatten(assertion).Any(candidate => candidate.FilePath != null);
    }

    private static Qt3Assertion[] ExternalAssertions(Qt3Assertion assertion)
    {
        return Flatten(assertion).Where(candidate => candidate.FilePath != null).ToArray();
    }

    private static Qt3Assertion[] Flatten(Qt3Assertion assertion)
    {
        return new[] { assertion }.Concat(assertion.Children.SelectMany(Flatten)).ToArray();
    }
}
