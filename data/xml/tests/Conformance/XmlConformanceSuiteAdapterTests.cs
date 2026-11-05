using xml;

namespace xml.Tests;

[TestFixture]
public class XmlConformanceSuiteAdapterTests
{
    [Test]
    public void LoadsTheCompleteEntityExpandedCatalogAndEveryClassification()
    {
        XmlConformanceSuiteAdapter suite = LoadSuite();

        Assert.Multiple(() =>
        {
            Assert.That(suite.TestCases.Length, Is.GreaterThan(2000));
            Assert.That(suite.TestCases.Select(test => test.Id).Distinct().Count(), Is.EqualTo(suite.TestCases.Length));
            Assert.That(suite.TestCases.Select(test => test.Classification).Distinct(), Is.EquivalentTo(Enum.GetValues<XmlConformanceClassification>()));
            Assert.That(suite.TestCases.Select(test => test.Recommendation).Distinct(), Is.EquivalentTo(new[]
            {
                "XML1.0",
                "XML1.1",
                "NS1.0",
                "NS1.1",
                "XML1.0-errata2e",
                "XML1.0-errata3e",
                "XML1.0-errata4e",
                "NS1.0-errata1e"
            }));
            Assert.That(suite.TestCases.Select(test => test.Version).Distinct(), Is.EquivalentTo(new[] { XmlVersion.Xml10, XmlVersion.Xml11 }));
            Assert.That(suite.TestCases.Select(test => test.NamespaceHandling).Distinct(), Is.EquivalentTo(new[] { XmlNamespaceHandling.Disabled, XmlNamespaceHandling.Namespaces10, XmlNamespaceHandling.Namespaces11 }));
            Assert.That(suite.TestCases.SelectMany(test => test.Editions).Distinct(), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(suite.TestCases, Has.All.Matches<XmlConformanceTestCase>(test => test.Profile.Length != 0 && test.ExpectedDiagnosticText.Length != 0));
            Assert.That(suite.TestCases, Has.All.Matches<XmlConformanceTestCase>(test => suite.Resources.ContainsKey(test.InputPath)));
            Assert.That(suite.TestCases.Where(test => test.OutputPath != null), Has.All.Matches<XmlConformanceTestCase>(test => suite.Resources.ContainsKey(test.OutputPath!)));
            Assert.That(suite.TestCases.Where(test => test.ValidatingOutputPath != null), Has.All.Matches<XmlConformanceTestCase>(test => suite.Resources.ContainsKey(test.ValidatingOutputPath!)));
        });
    }

    [Test]
    public void MapsVersionsNamespacesEditionsAndExpectedDiagnostics()
    {
        XmlConformanceSuiteAdapter suite = LoadSuite();
        XmlConformanceTestCase xml11 = Find(suite, "rmt-007");
        XmlConformanceTestCase namespace10 = Find(suite, "rmt-ns10-009");
        XmlConformanceTestCase fifthEdition = Find(suite, "x-rmt-008b");
        XmlConformanceTestCase invalid = Find(suite, "invalid-sa-140");
        XmlConformanceTestCase optionalError = Find(suite, "rmt-008");

        Assert.Multiple(() =>
        {
            Assert.That(xml11.Version, Is.EqualTo(XmlVersion.Xml11));
            Assert.That(xml11.Classification, Is.EqualTo(XmlConformanceClassification.Valid));
            Assert.That(xml11.ExpectedOutcome(false), Is.EqualTo(XmlConformanceExpectation.Accept));
            Assert.That(namespace10.NamespaceHandling, Is.EqualTo(XmlNamespaceHandling.Namespaces10));
            Assert.That(namespace10.ExpectedOutcome(false), Is.EqualTo(XmlConformanceExpectation.RejectWellFormedness));
            Assert.That(namespace10.ExpectedDiagnosticCategories(false), Does.Contain(XmlErrorCategory.Namespace));
            Assert.That(fifthEdition.Version, Is.EqualTo(XmlVersion.Xml10));
            Assert.That(fifthEdition.AppliesToEdition(5), Is.True);
            Assert.That(fifthEdition.AppliesToEdition(4), Is.False);
            Assert.That(invalid.ExpectedOutcome(false), Is.EqualTo(XmlConformanceExpectation.Accept));
            Assert.That(invalid.ExpectedOutcome(true), Is.EqualTo(XmlConformanceExpectation.RejectValidity));
            Assert.That(invalid.ExpectedDiagnosticCategories(true), Is.EqualTo(new[] { XmlErrorCategory.Validation }));
            Assert.That(optionalError.ExpectedOutcome(false), Is.EqualTo(XmlConformanceExpectation.OptionalError));
            Assert.That(optionalError.ExpectedDiagnosticText, Does.Contain("implausibly-versioned document"));
        });
    }

    [Test]
    public void ProvidesByteInputsCanonicalOutputsAndArchiveBackedResolution()
    {
        XmlConformanceSuiteAdapter suite = LoadSuite();
        XmlConformanceTestCase vanilla = Find(suite, "rmt-007");
        XmlConformanceTestCase external = Find(suite, "rmt-001");
        XmlInput input = suite.CreateInput(vanilla);
        byte[] output = suite.ReadResource(vanilla.OutputPath!);
        IXmlResourceResolver resolver = suite.CreateResourceResolver();
        XmlResolvedResource resolved = resolver.ResolveXmlResource(
            XmlResourceKind.ExternalSubset,
            null,
            "001.dtd",
            external.InputUri,
            suite.CreateParserOptions(external, false).ResolverPolicy!);
        XmlResolvedResource missing = resolver.ResolveXmlResource(
            XmlResourceKind.ExternalSubset,
            null,
            "missing.dtd",
            external.InputUri,
            suite.CreateParserOptions(external, false).ResolverPolicy!);

        Assert.Multiple(() =>
        {
            Assert.That(input.Kind, Is.EqualTo(XmlInputKind.ByteMemory));
            Assert.That(input.Bytes.Length, Is.GreaterThan(0));
            Assert.That(input.BaseUri, Is.EqualTo("https://xmlts.test/xmlconf/eduni/xml-1.1/007.xml"));
            Assert.That(vanilla.OutputPath, Is.EqualTo("xmlconf/eduni/xml-1.1/out/007.xml"));
            Assert.That(output, Is.Not.Empty);
            Assert.That(resolved.Access, Is.EqualTo(XmlResourceAccess.Allowed));
            Assert.That(resolved.AbsoluteUri, Is.EqualTo("https://xmlts.test/xmlconf/eduni/xml-1.1/001.dtd"));
            Assert.That(resolved.Input!.Bytes.Length, Is.GreaterThan(0));
            Assert.That(missing.Access, Is.EqualTo(XmlResourceAccess.Denied));
            Assert.That(missing.Input, Is.Null);
        });
    }

    [Test]
    public void CreatesParserProfilesForNonvalidatingAndValidatingRuns()
    {
        XmlConformanceSuiteAdapter suite = LoadSuite();
        XmlConformanceTestCase disabled = suite.TestCases.First(test => test.NamespaceHandling == XmlNamespaceHandling.Disabled);
        XmlConformanceTestCase namespace11 = Find(suite, "rmt-ns11-001");
        XmlParserOptions nonvalidating = suite.CreateParserOptions(disabled, false);
        XmlParserOptions validating = suite.CreateParserOptions(namespace11, true);

        Assert.Multiple(() =>
        {
            Assert.That(nonvalidating.Version, Is.EqualTo(disabled.Version));
            Assert.That(nonvalidating.NamespaceHandling, Is.EqualTo(XmlNamespaceHandling.Disabled));
            Assert.That(nonvalidating.DtdProcessing, Is.EqualTo(XmlDtdProcessing.Parse));
            Assert.That(nonvalidating.EntityHandling, Is.EqualTo(XmlEntityHandling.Expand));
            Assert.That(nonvalidating.ValidationMode, Is.EqualTo(XmlValidationMode.None));
            Assert.That(validating.Version, Is.EqualTo(XmlVersion.Xml11));
            Assert.That(validating.NamespaceHandling, Is.EqualTo(XmlNamespaceHandling.Namespaces11));
            Assert.That(validating.ValidationMode, Is.EqualTo(XmlValidationMode.Dtd));
            Assert.That(validating.ResolverPolicy!.Access, Is.EqualTo(XmlResourceAccess.Allowed));
            Assert.That(validating.ResolverPolicy.AllowedSchemes, Is.EqualTo(new[] { "https" }));
            Assert.That(validating.SecurityLimits!.MaximumExternalResources, Is.GreaterThan(0));
        });
    }

    private static XmlConformanceSuiteAdapter LoadSuite()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "specs", "test-suites", "xmlts-20130923.zip"));
        return new XmlConformanceSuiteAdapter(path);
    }

    private static XmlConformanceTestCase Find(XmlConformanceSuiteAdapter suite, string id)
    {
        return suite.TestCases.Single(test => test.Id == id);
    }
}
