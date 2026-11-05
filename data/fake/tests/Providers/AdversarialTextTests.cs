using fake;

namespace fake.Tests;

[TestFixture]
public class AdversarialTextTests
{
    [TestCase(AdversarialTextContext.Sql, "sql", "' OR 1=1 --", "SQL input must be treated as data")]
    [TestCase(AdversarialTextContext.Html, "html", "<script>alert(1)</script>", "HTML input must be escaped for its output context")]
    [TestCase(AdversarialTextContext.Spreadsheet, "spreadsheet", "=1+1", "spreadsheet formulas must be guarded")]
    [TestCase(AdversarialTextContext.Shell, "shell", "$(touch /tmp/fake-edge-probe)", "shell input must not be executed")]
    [TestCase(AdversarialTextContext.Path, "path-traversal", "../../etc/passwd", "path input must remain within its allowed root")]
    [TestCase(AdversarialTextContext.FormatString, "format-string", "%s%s%s%s%n", "format input must not be interpreted as directives")]
    [TestCase(AdversarialTextContext.Template, "template", "{{7*7}}", "template input must not be evaluated")]
    [TestCase(AdversarialTextContext.Header, "header-splitting", "value\r\nX-Fake: injected", "header values must not contain record separators")]
    public void ContextSpecificApisReturnExactInertDataAndMetadata(
        AdversarialTextContext context,
        string name,
        string value,
        string rule)
    {
        EdgeProvider edge = Fake.Create(149L).Edge();

        string text = edge.AdversarialString(context);
        EdgeSelectionResult result = edge.AdversarialFromContext(context);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(value));
            Assert.That(result.Edge.Value, Is.EqualTo(value));
            Assert.That(result.SelectedEdge, Is.EqualTo(name));
            Assert.That(result.Category, Is.EqualTo(EdgeCaseCategory.Injection));
            Assert.That(result.GenerationMode, Is.EqualTo(GenerationMode.Adversarial));
            Assert.That(result.Validity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(result.ViolatedRule, Is.EqualTo(rule));
        });
    }

    [Test]
    public void ContextSpecificLookupsDoNotConsumeEdgeRandomness()
    {
        EdgeProvider inspected = Fake.Create(149L).Edge();
        EdgeProvider expected = Fake.Create(149L).Edge();

        foreach (AdversarialTextContext context in Enum.GetValues<AdversarialTextContext>())
        {
            inspected.AdversarialString(context);
            inspected.AdversarialFromContext(context);
        }

        Assert.That(TakeAdversarial(inspected, 100), Is.EqualTo(TakeAdversarial(expected, 100)));
    }

    [Test]
    public void ContextSpecificSelectionDoesNotEnumerateTheWholeCatalog()
    {
        FakeOptions options = FakeOptions.Create(149L);
        options.MaximumCount = 1;
        options.MaximumOutputSize = 64;
        EdgeProvider edge = Fake.Create(options).Edge();

        Assert.Multiple(() =>
        {
            Assert.That(edge.AdversarialString(AdversarialTextContext.Sql), Is.EqualTo("' OR 1=1 --"));
            Assert.That(edge.AdversarialFromContext(AdversarialTextContext.Header).SelectedEdge, Is.EqualTo("header-splitting"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => edge.Injections()).ParamName, Is.EqualTo("category"));
        });
    }

    [Test]
    public void UnsupportedContextIsRejectedWithoutConsumingRandomness()
    {
        EdgeProvider inspected = Fake.Create(149L).Edge();
        EdgeProvider expected = Fake.Create(149L).Edge();
        AdversarialTextContext unsupported = (AdversarialTextContext)999;

        ArgumentOutOfRangeException text = Assert.Throws<ArgumentOutOfRangeException>(() => inspected.AdversarialString(unsupported));
        ArgumentOutOfRangeException metadata = Assert.Throws<ArgumentOutOfRangeException>(() => inspected.AdversarialFromContext(unsupported));

        Assert.Multiple(() =>
        {
            Assert.That(text.ParamName, Is.EqualTo("context"));
            Assert.That(metadata.ParamName, Is.EqualTo("context"));
            Assert.That(TakeAdversarial(inspected, 50), Is.EqualTo(TakeAdversarial(expected, 50)));
        });
    }

    [Test]
    public void OrdinaryProvidersIgnoreAdversarialModeAndNeverReturnItsPayloads()
    {
        FakeOptions typicalOptions = FakeOptions.Create(149L);
        FakeOptions adversarialOptions = FakeOptions.Create(149L);
        adversarialOptions.GenerationMode = GenerationMode.Adversarial;
        Fake typical = Fake.Create(typicalOptions);
        Fake adversarial = Fake.Create(adversarialOptions);
        HashSet<string> payloads = Payloads(Fake.Create(149L).Edge());

        int index = 0;
        while (index < 100)
        {
            string[] expected = OrdinaryValues(typical);
            string[] actual = OrdinaryValues(adversarial);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.All(value => !payloads.Contains(value)), Is.True);
            index++;
        }
    }

    static string[] OrdinaryValues(Fake value)
    {
        return new string[]
        {
            value.Text().Word(),
            value.Text().Sentence(),
            value.Identifier().Sku(),
            value.Internet().DomainName(),
            value.Internet().Email(),
            value.Internet().Url(),
            value.Person().FullName(),
            value.Company().Name(),
            value.System().FilePath(),
            value.Database().TableName(),
            value.Git().CommitMessage(),
            value.Color().Name()
        };
    }

    static HashSet<string> Payloads(EdgeProvider edge)
    {
        HashSet<string> result = new HashSet<string>();
        foreach (AdversarialTextContext context in Enum.GetValues<AdversarialTextContext>())
            result.Add(edge.AdversarialString(context));
        return result;
    }

    static string[] TakeAdversarial(EdgeProvider edge, int count)
    {
        string[] result = new string[count];
        int index = 0;
        while (index < result.Length)
        {
            EdgeSelectionResult selected = edge.Adversarial();
            result[index] = selected.SelectedEdge + "|" + selected.ViolatedRule;
            index++;
        }
        return result;
    }
}
