using fake;

namespace fake.Tests;

[TestFixture]
public class TemplateProviderFunctionalTests
{
    [Test]
    public void RegistrationPrecedenceIsStableAfterRejectedDuplicates()
    {
        TemplateProvider template = Fake.Create(12300).Template();
        int registeredCalls = 0;
        template.RegisterFromNameAndResolver("display", argument =>
        {
            registeredCalls++;
            return "registered";
        });
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["display"] = "scoped",
            ["primitive.digit"] = "caller-digit"
        };

        Assert.Throws<ArgumentException>(() =>
            template.RegisterFromNameAndResolver("display", argument => "replacement"));
        Assert.Throws<ArgumentException>(() =>
            template.RegisterFromNameAndResolver("primitive.digit", argument => "replacement"));

        string scoped = template.RenderFromTemplateAndValues(
            "{{display}}|{{primitive.digit}}",
            values);

        Assert.Multiple(() =>
        {
            Assert.That(scoped, Does.Match("^scoped\\|[0-9]$"));
            Assert.That(registeredCalls, Is.Zero);
            Assert.That(template.Resolve("display"), Is.EqualTo("registered"));
            Assert.That(registeredCalls, Is.EqualTo(1));
            Assert.That(template.Tokens(), Has.Exactly(1).EqualTo("display"));
        });
    }

    [Test]
    public void ProviderAndCallerResolversComposeDeterministically()
    {
        const long seed = 12301;
        TemplateProvider first = Fake.Create(seed).Template();
        TemplateProvider second = Fake.Create(seed).Template();
        first.RegisterFromNameAndResolver("caller.wrap", argument => "<" + argument + ">");
        second.RegisterFromNameAndResolver("caller.wrap", argument => "<" + argument + ">");
        string source = "{{primitive.digits:4}}/{{caller.wrap:alpha:beta}}/{{text.format:??-##}}";

        string actual = first.Render(source);
        string expected = second.Render(source);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual,
                Does.Match("^[0-9]{4}/<alpha:beta>/[A-Za-z]{2}-[0-9]{2}$"));
            Assert.That(first.ResolveFromNameAndArgument("caller.wrap", "x:y"),
                Is.EqualTo("<x:y>"));
        });
    }

    [Test]
    public void NestedValuesUseTheCallerComparerAndIgnoreUnusedEntries()
    {
        TemplateProvider template = Fake.Create(12302).Template();
        Dictionary<string, string> values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["greeting"] = "Hello {{PERSON}}",
            ["person"] = "{{First}} {{LAST}}",
            ["first"] = "Ada",
            ["last"] = "Lovelace",
            ["unused-missing"] = "{{missing}}",
            ["unused-null"] = null
        };

        template.ValidateFromTemplateAndValues("{{GREETING}}", values);
        string actual = template.RenderFromTemplateAndValues("{{GREETING}}", values);

        Assert.That(actual, Is.EqualTo("Hello Ada Lovelace"));
    }

    [Test]
    public void EscapesProduceLiteralSyntaxWithoutCallingEscapedResolvers()
    {
        TemplateProvider template = Fake.Create(12303).Template();
        int calls = 0;
        template.RegisterFromNameAndResolver("caller.hit", argument =>
        {
            calls++;
            return "resolved";
        });

        string actual = template.Render(
            "\\{{caller.hit}}|\\{|\\}|\\\\|{{caller.hit}}|}}");

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo("{{caller.hit}}|{|}|\\|resolved|}}"));
            Assert.That(calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void FormatArgumentsPreserveWhitespaceColonsAndBraceQuantifiers()
    {
        TemplateProvider template = Fake.Create(12304).Template();
        template.RegisterFromNameAndResolver("caller.echo", argument =>
        {
            if (argument == null)
            {
                return "<none>";
            }
            return argument;
        });

        string rendered = template.Render(
            "{{caller.echo: left:middle:right }}|{{text.format:??-##}}|{{text.pattern:[A-Z]{2}\\d{2}}}");

        Assert.Multiple(() =>
        {
            Assert.That(template.Resolve("caller.echo"), Is.EqualTo("<none>"));
            Assert.That(
                template.ResolveFromNameAndArgument("caller.echo", " left:middle:right "),
                Is.EqualTo(" left:middle:right "));
            Assert.That(rendered,
                Does.Match("^ left:middle:right \\|[A-Za-z]{2}-[0-9]{2}\\|[A-Z]{2}[0-9]{2}$"));
        });
    }

    [Test]
    public void ValidationDiagnosticsIdentifyMissingAndMalformedNestedTokens()
    {
        TemplateProvider template = Fake.Create(12305).Template();
        int calls = 0;
        template.RegisterFromNameAndResolver("caller.value", argument =>
        {
            calls++;
            return "value";
        });
        string missingSource = "ok {{caller.value}} then {{missing}}";
        Dictionary<string, string> malformed = new Dictionary<string, string>
        {
            ["outer"] = "x {{bad name}}"
        };

        ArgumentException missing = Assert.Throws<ArgumentException>(() =>
            template.Validate(missingSource));
        ArgumentException nested = Assert.Throws<ArgumentException>(() =>
            template.ValidateFromTemplateAndValues("{{outer}}", malformed));
        ArgumentException escape = Assert.Throws<ArgumentException>(() =>
            template.Validate("text\\"));

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.Zero);
            Assert.That(missing.ParamName, Is.EqualTo("template"));
            Assert.That(missing.Message,
                Does.Contain("index " + missingSource.IndexOf("{{missing}}", StringComparison.Ordinal))
                    .And.Contain("token 'missing'"));
            Assert.That(nested.ParamName, Is.EqualTo("template"));
            Assert.That(nested.Message,
                Does.Contain("index 2").And.Contain("unsupported character"));
            Assert.That(escape.ParamName, Is.EqualTo("template"));
            Assert.That(escape.Message, Does.Contain("trailing escape"));
        });
    }

    [Test]
    public void SelfMutualAndDepthLimitsApplyToValidationAndRendering()
    {
        TemplateProvider template = Fake.Create(12306).Template();
        Dictionary<string, string> self = new Dictionary<string, string>
        {
            ["a"] = "{{a}}"
        };
        Dictionary<string, string> mutual = new Dictionary<string, string>
        {
            ["a"] = "{{b}}",
            ["b"] = "{{a}}"
        };
        Dictionary<string, string> deep = new Dictionary<string, string>
        {
            ["a"] = "{{b}}",
            ["b"] = "{{c}}",
            ["c"] = "done"
        };

        template.ValidateFromTemplateAndValuesAndMaximumDepth("{{a}}", deep, 3);

        Assert.Multiple(() =>
        {
            Assert.That(
                template.RenderFromTemplateAndValuesAndMaximumDepth("{{a}}", deep, 3),
                Is.EqualTo("done"));
            Assert.That(
                () => template.ValidateFromTemplateAndValues("{{a}}", self),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("cycle"));
            Assert.That(
                () => template.RenderFromTemplateAndValues("{{a}}", mutual),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("cycle"));
            Assert.That(
                () => template.ValidateFromTemplateAndValuesAndMaximumDepth("{{a}}", deep, 2),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("depth"));
            Assert.That(
                () => template.RenderFromTemplateAndValuesAndMaximumDepth("{{a}}", deep, 2),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("depth"));
        });
    }

    [Test]
    public void MixedEvaluationOrderMatchesEquivalentDirectResolution()
    {
        const long seed = 12307;
        TemplateProvider templated = Fake.Create(seed).Template();
        TemplateProvider direct = Fake.Create(seed).Template();
        List<string> templatedOrder = new List<string>();
        List<string> directOrder = new List<string>();
        templated.RegisterFromNameAndResolver("caller.a", argument =>
        {
            templatedOrder.Add("a:" + argument);
            return "A";
        });
        templated.RegisterFromNameAndResolver("caller.b", argument =>
        {
            templatedOrder.Add("b:" + argument);
            return "B";
        });
        direct.RegisterFromNameAndResolver("caller.a", argument =>
        {
            directOrder.Add("a:" + argument);
            return "A";
        });
        direct.RegisterFromNameAndResolver("caller.b", argument =>
        {
            directOrder.Add("b:" + argument);
            return "B";
        });
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["nested"] = "{{primitive.digits:2}}{{caller.b:B}}"
        };

        string actual = templated.RenderFromTemplateAndValues(
            "{{caller.a:A}}{{nested}}{{primitive.alpha:2}}{{caller.a:C}}",
            values);
        string expected =
            direct.ResolveFromNameAndArgument("caller.a", "A") +
            direct.ResolveFromNameAndArgument("primitive.digits", "2") +
            direct.ResolveFromNameAndArgument("caller.b", "B") +
            direct.ResolveFromNameAndArgument("primitive.alpha", "2") +
            direct.ResolveFromNameAndArgument("caller.a", "C");

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(templatedOrder, Is.EqualTo(new[] { "a:A", "b:B", "a:C" }));
            Assert.That(templatedOrder, Is.EqualTo(directOrder));
        });
    }
}
