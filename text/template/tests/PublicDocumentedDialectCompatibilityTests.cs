using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicDocumentedDialectCompatibilityTests
{
    static object[] RenderFixtures =
    {
        new object[]
        {
            "scriban",
            "{{ greeting = 'Hello' }}{{ greeting }} {{ Name }}! {{ if show }}{{ for item in items }}[{{ item }}]{{ end }}{{ else }}empty{{ end }}",
            "Hello Ada! [one][two]"
        },
        new object[]
        {
            "scriban",
            "A {{## documented comment ##}}{{- Name -}} B",
            "AAdaB"
        },
        new object[]
        {
            "nunjucks",
            "{% set username = 'joe' %}{{ username|title }}|{{ missing|default('fallback') }}|{% filter upper %}hello{% endfilter %}",
            "Joe|fallback|HELLO"
        },
        new object[]
        {
            "go-text-template",
            "{{.Count}} items are made of {{.Material}}.{{if .Show}}{{range .Items}}[{{.}}]{{else}}empty{{end}}{{end}}",
            "2 items are made of wool.[one][two]"
        },
        new object[]
        {
            "go-text-template",
            "{{range $index, $element := .Items}}{{$index}}={{$element}};{{end}}{{with .Person}}{{.Name}}{{end}}",
            "0=one;1=two;Bea"
        },
        new object[]
        {
            "razor-inspired",
            "<p>@Name</p>|@('<b>x</b>')|@Html.Raw('<i>safe</i>')|@@user",
            "<p>Ada</p>|&lt;b&gt;x&lt;/b&gt;|<i>safe</i>|@user"
        },
        new object[]
        {
            "razor-inspired",
            "@{ var greeting = 'Hi'; }@if (show){@greeting @Name}else{hidden}|@foreach (var item in items){[@item]}",
            "Hi Ada|[one][two]"
        }
    };

    static object[] AnalysisFixtures =
    {
        new object[] { "scriban", "{{ if show }}yes{{ end }}", false, false },
        new object[] { "scriban", "{{ func greet }}x{{ end }}", false, true },
        new object[] { "nunjucks", "{{ name|title }}", false, false },
        new object[] { "nunjucks", "{% asyncEach item in items %}{{ item }}{% endeach %}", true, true },
        new object[] { "go-text-template", "{{range .Items}}{{.}}{{end}}", true, false },
        new object[] { "go-text-template", "{{printf '%q' 'output'}}", false, true },
        new object[] { "razor-inspired", "<p>@Name</p>", true, false },
        new object[] { "razor-inspired", "<p>@await GetName()</p>", true, true }
    };

    [TestCaseSource(nameof(RenderFixtures))]
    public void Render_DocumentedDialectFixture_UsesPublicProfileAndRenderer(string profileName, string templateText, string expected)
    {
        TemplateProfileRegistry profiles = new TemplateProfileRegistry();
        TemplateRenderer renderer = new TemplateRenderer();
        renderer.SetProfileRegistry(profiles);
        renderer.Configure(profiles.CreateOptions(profileName, TemplateCompatibilityMode.Pragmatic), new TemplateRegistry(), new TemplateMemberResolver());

        string result = renderer.RenderToString(templateText, CreateModel());

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(AnalysisFixtures))]
    public void Analyze_DocumentedDialectFixture_ReportsTranslationDifferencesAndUnsupportedConstructs(string profileName, string templateText, bool expectsDifference, bool expectsUnsupported)
    {
        ITemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();

        TemplateCompatibilityAnalysis analysis = analyzer.Analyze(templateText, profileName, "native");

        Assert.That(HasCode(analysis, "COMPAT009"), Is.True);
        Assert.That(HasCode(analysis, "COMPAT010"), Is.EqualTo(expectsDifference));
        Assert.That(HasCode(analysis, "COMPAT011"), Is.EqualTo(expectsUnsupported));
        Assert.That(analysis.NativeTemplateText, Is.Not.EqualTo(""));
    }

    [Test]
    public void Profiles_DocumentedDialects_AreAvailableThroughPublicRegistry()
    {
        ITemplateProfileRegistry profiles = new TemplateProfileRegistry();

        Assert.That(profiles.Contains("scriban"), Is.True);
        Assert.That(profiles.Contains("nunjucks"), Is.True);
        Assert.That(profiles.Contains("go-text-template"), Is.True);
        Assert.That(profiles.Contains("razor-inspired"), Is.True);
    }

    static TemplateValue CreateModel()
    {
        Dictionary<string, object> person = new Dictionary<string, object>();
        person["Name"] = "Bea";
        Dictionary<string, object> model = new Dictionary<string, object>();
        model["Name"] = "Ada";
        model["Count"] = 2;
        model["Material"] = "wool";
        model["Show"] = true;
        model["show"] = true;
        model["items"] = new List<string> { "one", "two" };
        model["Items"] = new List<string> { "one", "two" };
        model["Person"] = person;
        return new TemplateValueFactory().Convert(model);
    }

    static bool HasCode(TemplateCompatibilityAnalysis analysis, string code)
    {
        int index = 0;
        while (index < analysis.Diagnostics.Count)
        {
            if (analysis.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
