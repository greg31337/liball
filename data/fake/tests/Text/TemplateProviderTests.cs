using fake;

namespace fake.Tests;

[TestFixture]
public class TemplateProviderTests
{
    [Test]
    public void TemplateProviderIsCachedReachableAndDeterministic()
    {
        const long seed = 12200;
        Fake firstFake = Fake.Create(seed);
        Fake secondFake = Fake.Create(seed);
        TemplateProvider first = firstFake.Template();
        TemplateProvider second = secondFake.Template();
        ITemplateProvider contract = first;

        Assert.Multiple(() =>
        {
            Assert.That(firstFake.Template(), Is.SameAs(first));
            Assert.That(contract, Is.SameAs(first));
            Assert.That(first.ResolveFromNameAndArgument("primitive.digits", "6"),
                Is.EqualTo(second.ResolveFromNameAndArgument("primitive.digits", "6")));
            Assert.That(first.Render("{{primitive.alpha:4}}-{{primitive.digit}}"),
                Is.EqualTo(second.Render("{{primitive.alpha:4}}-{{primitive.digit}}")));
        });
    }

    [Test]
    public void RegisteredResolversReceiveArgumentsAndAppearInOrder()
    {
        TemplateProvider template = Fake.Create(12201).Template();
        int calls = 0;
        template.RegisterFromNameAndResolver("caller.first", argument =>
        {
            calls++;
            if (argument == null)
            {
                return "plain";
            }
            return "[" + argument + "]";
        });
        template.RegisterFromNameAndResolver("caller.second", argument => "second");

        string[] firstTokens = template.Tokens();
        firstTokens[firstTokens.Length - 1] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(template.Resolve("caller.first"), Is.EqualTo("plain"));
            Assert.That(template.ResolveFromNameAndArgument("caller.first", "format:02"),
                Is.EqualTo("[format:02]"));
            Assert.That(template.Render("\\{{caller.first}}|{{caller.first:x}}"),
                Is.EqualTo("{{caller.first}}|[x]"));
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(template.Tokens()[template.Tokens().Length - 2], Is.EqualTo("caller.first"));
            Assert.That(template.Tokens()[template.Tokens().Length - 1], Is.EqualTo("caller.second"));
            Assert.That(template.Tokens(), Does.Not.Contain("changed"));
        });
    }

    [Test]
    public void RenderValuesNestAndTakeDocumentedPrecedence()
    {
        TemplateProvider template = Fake.Create(12202).Template();
        int registeredCalls = 0;
        template.RegisterFromNameAndResolver("name", argument =>
        {
            registeredCalls++;
            return "registered";
        });
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["name"] = "{{first}} {{last}}",
            ["first"] = "Ada",
            ["last"] = "Lovelace",
            ["primitive.digit"] = "not-a-digit"
        };

        string actual = template.RenderFromTemplateAndValues(
            "Hello {{name}}; {{primitive.digit}}",
            values);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Does.Match("^Hello Ada Lovelace; [0-9]$"));
            Assert.That(registeredCalls, Is.Zero);
        });
    }

    [Test]
    public void ValidationDoesNotInvokeResolversOrConsumeTemplateEntropy()
    {
        const long seed = 12203;
        TemplateProvider inspected = Fake.Create(seed).Template();
        TemplateProvider untouched = Fake.Create(seed).Template();
        int calls = 0;
        inspected.RegisterFromNameAndResolver("caller.value", argument =>
        {
            calls++;
            return "value";
        });
        untouched.RegisterFromNameAndResolver("caller.value", argument => "value");

        inspected.Validate("{{caller.value:x}}/{{primitive.digits:4}}");
        Assert.Throws<ArgumentException>(() =>
            inspected.Validate("{{caller.value}}{{missing}}"));

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.Zero);
            Assert.That(inspected.Render("{{primitive.digits:8}}"),
                Is.EqualTo(untouched.Render("{{primitive.digits:8}}")));
        });
    }

    [Test]
    public void ValidationReportsMissingTokensCyclesAndDepthFailures()
    {
        TemplateProvider template = Fake.Create(12204).Template();
        Dictionary<string, string> cycle = new Dictionary<string, string>
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

        ArgumentException missing = Assert.Throws<ArgumentException>(() =>
            template.Validate("prefix {{missing}}"));
        ArgumentException cyclic = Assert.Throws<ArgumentException>(() =>
            template.ValidateFromTemplateAndValues("{{a}}", cycle));
        ArgumentException depth = Assert.Throws<ArgumentException>(() =>
            template.ValidateFromTemplateAndValuesAndMaximumDepth("{{a}}", deep, 2));

        Assert.Multiple(() =>
        {
            Assert.That(missing.ParamName, Is.EqualTo("template"));
            Assert.That(missing.Message, Does.Contain("index 7").And.Contain("missing"));
            Assert.That(cyclic.Message, Does.Contain("cycle").And.Contain("token 'a'"));
            Assert.That(depth.Message, Does.Contain("depth").And.Contain("token 'c'"));
        });
    }

    [Test]
    public void RegistrationResolutionAndOutputLimitsRejectInvalidInputs()
    {
        TemplateProvider template = Fake.Create(12205).Template();
        template.RegisterFromNameAndResolver("duplicate", argument => "first");
        template.RegisterFromNameAndResolver("null-output", argument => null);
        FakeOptions options = FakeOptions.Create(12205);
        options.MaximumOutputSize = 5;
        TemplateProvider limited = Fake.Create(options).Template();
        limited.RegisterFromNameAndResolver("large", argument => "123456");

        Assert.Multiple(() =>
        {
            Assert.That(() => template.RegisterFromNameAndResolver(null, argument => "value"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => template.RegisterFromNameAndResolver("bad name", argument => "value"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => template.RegisterFromNameAndResolver("primitive.digit", argument => "value"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => template.RegisterFromNameAndResolver("duplicate", argument => "second"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => template.RegisterFromNameAndResolver("missing-resolver", null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => template.Resolve("missing"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => template.ResolveFromNameAndArgument("duplicate", null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => template.Resolve("null-output"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => limited.Render("{{large}}"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ResolverEvaluationIsLeftToRightAndDepthFirst()
    {
        TemplateProvider template = Fake.Create(12206).Template();
        List<string> order = new List<string>();
        template.RegisterFromNameAndResolver("caller.a", argument =>
        {
            order.Add("a:" + argument);
            return "A";
        });
        template.RegisterFromNameAndResolver("caller.b", argument =>
        {
            order.Add("b:" + argument);
            return "B";
        });
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["nested"] = "{{caller.b:2}}"
        };

        string actual = template.RenderFromTemplateAndValues(
            "{{caller.a:1}}{{nested}}{{caller.a:3}}",
            values);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo("ABA"));
            Assert.That(order, Is.EqualTo(new[] { "a:1", "b:2", "a:3" }));
        });
    }
}
