using NUnit.Framework;

namespace template.Tests;

public class CompatibilityFixture
{
    public string Engine;
    public string SourceUrl;
    public string ProfileName;
    public string TemplateText;
    public string ExpectedOutput;
    public bool ShouldRender;

    public CompatibilityFixture(string engine, string sourceUrl, string profileName, string templateText, string expectedOutput, bool shouldRender)
    {
        Engine = engine;
        SourceUrl = sourceUrl;
        ProfileName = profileName;
        TemplateText = templateText;
        ExpectedOutput = expectedOutput;
        ShouldRender = shouldRender;
    }
}

[TestFixture]
public class CompatibilityFixtureTests
{
    [Test]
    public void DocumentedCompatibilityFixtures_CoverRequiredEngines()
    {
        List<CompatibilityFixture> fixtures = CreateFixtures();

        AssertHasEngine(fixtures, "mustache");
        AssertHasEngine(fixtures, "handlebars");
        AssertHasEngine(fixtures, "liquid");
        AssertHasEngine(fixtures, "jinja");
        AssertHasEngine(fixtures, "django");
        AssertHasEngine(fixtures, "scriban");
        AssertHasEngine(fixtures, "nunjucks");
        AssertHasEngine(fixtures, "go-text-template");
        AssertHasEngine(fixtures, "razor-inspired");
        Assert.That(fixtures.Count, Is.EqualTo(9));
    }

    [Test]
    public void DocumentedCompatibilityFixtures_HaveSourceAndExpectedOutputMetadata()
    {
        List<CompatibilityFixture> fixtures = CreateFixtures();
        int index = 0;
        while (index < fixtures.Count)
        {
            CompatibilityFixture fixture = fixtures[index];
            Assert.That(fixture.SourceUrl, Is.Not.EqualTo(""));
            Assert.That(fixture.TemplateText, Is.Not.EqualTo(""));
            Assert.That(fixture.ExpectedOutput, Is.Not.EqualTo(""));
            index = index + 1;
        }
    }

    [Test]
    public void DocumentedCompatibilityFixtures_RenderSupportedExamples()
    {
        List<CompatibilityFixture> fixtures = CreateFixtures();
        TemplateValue model = CreateFixtureModel();
        int index = 0;
        while (index < fixtures.Count)
        {
            CompatibilityFixture fixture = fixtures[index];
            if (fixture.ShouldRender)
            {
                TemplateRenderer renderer = TemplateRenderer.Create(CreateOptions(fixture.ProfileName));
                string result = renderer.RenderToString(fixture.TemplateText, model);
                Assert.That(result, Is.EqualTo(fixture.ExpectedOutput), fixture.Engine);
            }

            index = index + 1;
        }
    }

    [Test]
    public void DocumentedCompatibilityFixtures_RenderExpandedDocumentedProfiles()
    {
        List<CompatibilityFixture> fixtures = CreateFixtures();

        AssertSupported(fixtures, "scriban");
        AssertSupported(fixtures, "nunjucks");
        AssertSupported(fixtures, "go-text-template");
        AssertSupported(fixtures, "razor-inspired");
    }

    static List<CompatibilityFixture> CreateFixtures()
    {
        List<CompatibilityFixture> fixtures = new List<CompatibilityFixture>();
        fixtures.Add(new CompatibilityFixture(
            "mustache",
            "https://mustache.github.io/mustache.5.html",
            "mustache",
            "Hello {{name}}\nYou have just won {{value}} dollars!\n{{#in_ca}}Well, {{taxed_value}} dollars, after taxes.\n{{/in_ca}}",
            "Hello Chris\nYou have just won 10000 dollars!\nWell, 6000 dollars, after taxes.\n",
            true));
        fixtures.Add(new CompatibilityFixture(
            "handlebars",
            "https://handlebarsjs.com/guide/block-helpers.html",
            "handlebars",
            "{{#each users as |user userId|}}{{userId}}:{{user.name}};{{/each}}",
            "0:Yehuda;1:Carl;",
            true));
        fixtures.Add(new CompatibilityFixture(
            "liquid",
            "https://shopify.github.io/liquid/basics/introduction/",
            "liquid",
            "{{ \"/my/fancy/url\" | append: \".html\" }}",
            "/my/fancy/url.html",
            true));
        fixtures.Add(new CompatibilityFixture(
            "jinja",
            "https://jinja.palletsprojects.com/en/stable/templates/",
            "jinja",
            "{% filter upper %}This text becomes uppercase{% endfilter %}",
            "THIS TEXT BECOMES UPPERCASE",
            true));
        fixtures.Add(new CompatibilityFixture(
            "django",
            "https://docs.djangoproject.com/en/dev/ref/templates/language/",
            "django",
            "{% autoescape off %}{{ data }}{% endautoescape %}",
            "<b>Hello!</b>",
            true));
        fixtures.Add(new CompatibilityFixture(
            "scriban",
            "https://scriban.github.io/",
            "native",
            "Hello {{ Name }}! You have {{ count }} messages.",
            "Hello World! You have 5 messages.",
            true));
        fixtures.Add(new CompatibilityFixture(
            "nunjucks",
            "https://mozilla.github.io/nunjucks/templating.html",
            "jinja",
            "{% filter title %}may the force be with you{% endfilter %}",
            "May The Force Be With You",
            true));
        fixtures.Add(new CompatibilityFixture(
            "go-text-template",
            "https://pkg.go.dev/text/template",
            "go-text-template",
            "Hello {{.Name}}!",
            "Hello World!",
            true));
        fixtures.Add(new CompatibilityFixture(
            "razor-inspired",
            "https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor",
            "razor-inspired",
            "@(\"<span>Hello World</span>\")",
            "&lt;span&gt;Hello World&lt;/span&gt;",
            true));
        return fixtures;
    }

    static TemplateOptions CreateOptions(string profileName)
    {
        if (profileName == "scriban" || profileName == "nunjucks" || profileName == "go-text-template" || profileName == "razor-inspired")
        {
            return new TemplateProfileRegistry().CreateOptions(profileName, TemplateCompatibilityMode.Pragmatic);
        }

        if (profileName == "mustache")
        {
            return MustacheProfile.CreateOptions();
        }

        if (profileName == "handlebars")
        {
            return HandlebarsProfile.CreateOptions();
        }

        if (profileName == "liquid")
        {
            return LiquidProfile.CreateOptions();
        }

        if (profileName == "jinja")
        {
            return JinjaProfile.CreateOptions();
        }

        if (profileName == "django")
        {
            return DjangoProfile.CreateOptions();
        }

        return NativeProfile.CreateOptions();
    }

    static TemplateValue CreateFixtureModel()
    {
        Dictionary<string, TemplateValue> root = new Dictionary<string, TemplateValue>();
        root["name"] = TemplateValue.FromString("Chris");
        root["Name"] = TemplateValue.FromString("World");
        root["count"] = TemplateValue.FromNumber(5m);
        root["data"] = TemplateValue.FromString("<b>Hello!</b>");
        root["value"] = TemplateValue.FromNumber(10000m);
        root["taxed_value"] = TemplateValue.FromNumber(6000m);
        root["in_ca"] = TemplateValue.FromBoolean(true);
        root["users"] = TemplateValue.FromList(CreateUsers());
        return TemplateValue.FromMap(root);
    }

    static List<TemplateValue> CreateUsers()
    {
        List<TemplateValue> users = new List<TemplateValue>();
        users.Add(CreateUser("Yehuda"));
        users.Add(CreateUser("Carl"));
        return users;
    }

    static TemplateValue CreateUser(string name)
    {
        Dictionary<string, TemplateValue> user = new Dictionary<string, TemplateValue>();
        user["name"] = TemplateValue.FromString(name);
        return TemplateValue.FromMap(user);
    }

    static void AssertHasEngine(List<CompatibilityFixture> fixtures, string engine)
    {
        bool found = false;
        int index = 0;
        while (index < fixtures.Count)
        {
            if (fixtures[index].Engine == engine)
            {
                found = true;
            }

            index = index + 1;
        }

        Assert.That(found, Is.True, engine);
    }

    static void AssertSupported(List<CompatibilityFixture> fixtures, string engine)
    {
        int index = 0;
        while (index < fixtures.Count)
        {
            if (fixtures[index].Engine == engine)
            {
                Assert.That(fixtures[index].ShouldRender, Is.True, engine);
                return;
            }

            index = index + 1;
        }

        Assert.Fail(engine);
    }
}
