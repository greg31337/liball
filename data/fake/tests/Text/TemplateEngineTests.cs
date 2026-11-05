using fake;

namespace fake.Tests;

[TestFixture]
public class TemplateEngineTests
{
    [Test]
    public void TemplateTokenCatalogHasStableOrderingAndIndependentArrays()
    {
        TextProvider text = Fake.Create(4001).Text();
        string[] expected = new[]
        {
            "primitive.alpha",
            "primitive.alphanumeric",
            "primitive.digit",
            "primitive.digits",
            "primitive.hash",
            "primitive.hex",
            "primitive.passphrase",
            "primitive.password",
            "primitive.pin",
            "primitive.recovery-code",
            "primitive.token",
            "text.format",
            "text.pattern"
        };

        string[] first = text.TemplateTokens();
        first[0] = "changed";

        Assert.That(text.TemplateTokens(), Is.EqualTo(expected));
    }

    [Test]
    public void PrimitiveCatalogTokensAcceptDocumentedLengthArguments()
    {
        TextProvider text = Fake.Create(4002).Text();
        string template = "{{primitive.digit}}|{{primitive.digits:4}}|{{primitive.alpha:5}}|{{primitive.alphanumeric:6}}|{{primitive.hex:7}}|{{primitive.hash:8}}|{{primitive.token:9}}|{{primitive.pin:4}}";

        string actual = text.Template(template);

        Assert.That(actual, Does.Match("^[0-9]\\|[0-9]{4}\\|[A-Za-z]{5}\\|[A-Za-z0-9]{6}\\|[0-9a-f]{7}\\|[0-9a-f]{8}\\|[A-Za-z0-9_-]{9}\\|[0-9]{4}$"));
    }

    [Test]
    public void SecretCatalogTokensUseSafeDefaultsAndRequiredClasses()
    {
        TextProvider text = Fake.Create(4003).Text();

        string hash = text.Template("{{primitive.hash}}");
        string recovery = text.Template("{{primitive.recovery-code}}");
        string password = text.Template("{{primitive.password}}");
        string passphrase = text.Template("{{primitive.passphrase}}");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Does.Match("^[0-9a-f]{40}$"));
            Assert.That(recovery, Does.Match("^[2-9A-HJ-NP-Z]{4}(-[2-9A-HJ-NP-Z]{4}){3}$"));
            Assert.That(password, Has.Length.EqualTo(16));
            Assert.That(password.Any(character => character >= 'a' && character <= 'z'), Is.True);
            Assert.That(password.Any(character => character >= 'A' && character <= 'Z'), Is.True);
            Assert.That(password.Any(character => character >= '0' && character <= '9'), Is.True);
            Assert.That(password.Any(character => "!#$%&()*+,-./:;<=>?@[]^_{|}~".Contains(character)), Is.True);
            Assert.That(passphrase, Does.Match("^[a-z]{5}(-[a-z]{5}){3}$"));
            Assert.That(text.Template("{{primitive.passphrase:0}}"), Is.Empty);
        });
    }

    [Test]
    public void TextCatalogTokensAcceptMasksAndBraceHeavyPatterns()
    {
        TextProvider text = Fake.Create(4004).Text();

        string actual = text.Template("{{text.format:??-##}}/{{text.pattern:[A-Z]{3}-\\d{2}}}");

        Assert.That(actual, Does.Match("^[A-Za-z]{2}-[0-9]{2}/[A-Z]{3}-[0-9]{2}$"));
        Assert.That(text.Template("{{text.format:}}/{{text.pattern:}}"), Is.EqualTo("/"));
    }

    [Test]
    public void CallerValuesEvaluateRecursivelyAndDepthFirst()
    {
        TextProvider text = Fake.Create(4005).Text();
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["name"] = "{{first}} {{last}}",
            ["first"] = "Ada",
            ["last"] = "{{family}}",
            ["family"] = "Lovelace",
            ["code"] = "{{text.format:??-##}}"
        };

        string actual = text.TemplateFromTemplateAndValues("Hello {{name}}; code={{code}}.", values);

        Assert.That(actual, Does.Match("^Hello Ada Lovelace; code=[A-Za-z]{2}-[0-9]{2}\\.$"));
    }

    [Test]
    public void EscapingPreservesLiteralDelimitersAndBackslashes()
    {
        TextProvider text = Fake.Create(4006).Text();

        string actual = text.Template("\\{{primitive.digit}} \\{ \\} \\\\ {{primitive.digit}} }}");

        Assert.That(actual, Does.Match("^\\{\\{primitive\\.digit}} \\{ } \\\\ [0-9] }}$"));
    }

    [Test]
    public void BuiltInTokensTakePrecedenceOverSuppliedNames()
    {
        TextProvider text = Fake.Create(4007).Text();
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["primitive.digit"] = "not-a-digit"
        };

        Assert.That(text.TemplateFromTemplateAndValues("{{primitive.digit}}", values), Does.Match("^[0-9]$"));
    }

    [Test]
    public void TemplateEvaluationOrderMatchesDirectDepthFirstCalls()
    {
        TextProvider templated = Fake.Create(4008).Text();
        TextProvider direct = Fake.Create(4008).Text();
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["nested"] = "{{text.format:##}}-{{text.pattern:[A-Z]{2}}}"
        };

        string actual = templated.TemplateFromTemplateAndValues("{{nested}}/{{text.format:**}}", values);
        string expected = direct.Format("##") + "-" + direct.FromPattern("[A-Z]{2}") + "/" + direct.Format("**");

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SelfAndMutualCyclesAreRejectedWithTheTokenName()
    {
        TextProvider text = Fake.Create(4009).Text();
        Dictionary<string, string> self = new Dictionary<string, string>
        {
            ["a"] = "{{a}}"
        };
        Dictionary<string, string> mutual = new Dictionary<string, string>
        {
            ["a"] = "{{b}}",
            ["b"] = "{{a}}"
        };

        ArgumentException selfException = Assert.Throws<ArgumentException>(() => text.TemplateFromTemplateAndValues("{{a}}", self));
        ArgumentException mutualException = Assert.Throws<ArgumentException>(() => text.TemplateFromTemplateAndValues("{{a}}", mutual));

        Assert.Multiple(() =>
        {
            Assert.That(selfException.ParamName, Is.EqualTo("template"));
            Assert.That(selfException.Message, Does.Contain("token 'a'").And.Contain("cycle"));
            Assert.That(mutualException.ParamName, Is.EqualTo("template"));
            Assert.That(mutualException.Message, Does.Contain("token 'a'").And.Contain("cycle"));
        });
    }

    [Test]
    public void ExplicitRecursionDepthBoundsNestedValues()
    {
        TextProvider text = Fake.Create(4010).Text();
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["a"] = "{{b}}",
            ["b"] = "{{c}}",
            ["c"] = "done"
        };

        Assert.That(text.TemplateFromTemplateAndValuesAndMaximumDepth("{{a}}", values, 3), Is.EqualTo("done"));
        ArgumentException exception = Assert.Throws<ArgumentException>(() => text.TemplateFromTemplateAndValuesAndMaximumDepth("{{a}}", values, 2));

        Assert.That(exception.ParamName, Is.EqualTo("template"));
        Assert.That(exception.Message, Does.Contain("depth").And.Contain("token 'c'"));
    }

    [TestCase("{{", 0)]
    [TestCase("{{}}", 0)]
    [TestCase("{{bad name}}", 0)]
    [TestCase("{{missing}}", 0)]
    [TestCase("{{primitive.digit:2}}", 0)]
    [TestCase("{{primitive.alpha:nope}}", 0)]
    [TestCase("{{primitive.password:3}}", 0)]
    [TestCase("{{text.format}}", 0)]
    [TestCase("{{text.pattern}}", 0)]
    [TestCase("\\x", 0)]
    public void InvalidTemplatesReportTheFailingTemplateIndex(string template, int index)
    {
        TextProvider text = Fake.Create(4011).Text();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => text.Template(template));

        Assert.That(exception.ParamName, Is.EqualTo("template"));
        Assert.That(exception.Message, Does.Contain($"index {index}"));
    }

    [Test]
    public void TemplateValidatesReferenceArgumentsAndNestedUtf16()
    {
        TextProvider text = Fake.Create(4012).Text();
        string malformed = new string(new[] { '\uD800' });
        Dictionary<string, string> malformedValue = new Dictionary<string, string>
        {
            ["bad"] = malformed
        };
        Dictionary<string, string> nullValue = new Dictionary<string, string>
        {
            ["bad"] = null
        };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Template(null)).ParamName, Is.EqualTo("template"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.TemplateFromTemplateAndValues("value", null)).ParamName, Is.EqualTo("values"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Template(malformed)).ParamName, Is.EqualTo("template"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.TemplateFromTemplateAndValues("{{bad}}", malformedValue)).ParamName, Is.EqualTo("template"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.TemplateFromTemplateAndValues("{{bad}}", nullValue)).ParamName, Is.EqualTo("values"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.TemplateFromTemplateAndValuesAndMaximumDepth("value", new Dictionary<string, string>(), 0)).ParamName, Is.EqualTo("maximumDepth"));
        });
    }

    [Test]
    public void PreflightFailuresConsumeNoEntropy()
    {
        FakeOptions options = FakeOptions.Create(4013L);
        options.MaximumOutputSize = 10;
        TextProvider actual = Fake.Create(options).Text();
        TextProvider expectedPosition = Fake.Create(options).Text();

        Assert.Throws<ArgumentException>(() => actual.Template("{{text.format:##}}/{{missing}}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Template("{{text.format:######}}/{{primitive.digits:5}}"));
        Assert.Throws<ArgumentException>(() => actual.Template("\\q"));
        actual.Template("\\{ok\\}");

        Assert.That(actual.Format("####????"), Is.EqualTo(expectedPosition.Format("####????")));
    }

    [Test]
    public void TemplateHonorsCountDepthAndCombinedOutputLimits()
    {
        FakeOptions options = FakeOptions.Create(4014L);
        options.MaximumCount = 50;
        options.MaximumOutputSize = 12;
        TextProvider text = Fake.Create(options).Text();
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["a"] = "123456",
            ["b"] = "{{a}}{{a}}"
        };

        Assert.Multiple(() =>
        {
            Assert.That(text.TemplateFromTemplateAndValues("{{b}}", values), Is.EqualTo("123456123456"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.TemplateFromTemplateAndValues("{{b}}x", values)).ParamName, Is.EqualTo("template"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Template(new string('x', 51))).ParamName, Is.EqualTo("template"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.TemplateFromTemplateAndValuesAndMaximumDepth("x", values, 51)).ParamName, Is.EqualTo("maximumDepth"));
        });
    }

    [Test]
    public void CryptographicModeSupportsTemplateCatalogEvaluation()
    {
        FakeOptions options = FakeOptions.Create(4015L);
        options.RandomMode = RandomMode.Cryptographic;
        TextProvider text = Fake.Create(options).Text();

        string actual = text.Template("{{primitive.token:24}}/{{text.format:####}}/{{text.pattern:[A-Z]{3}}}");

        Assert.That(actual, Does.Match("^[A-Za-z0-9_-]{24}/[0-9]{4}/[A-Z]{3}$"));
    }
}
