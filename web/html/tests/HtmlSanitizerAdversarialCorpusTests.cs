using System.Text;
using System.Text.Json;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerAdversarialCorpusTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string XlinkNamespace = "http://www.w3.org/1999/xlink";

    [TestCaseSource(nameof(GetCorpusCases))]
    public void AdversarialPayloadsFailClosedOrProduceSafeOutput(
        SanitizerCorpusCase testCase)
    {
        SanitizerCorpusProfile profile = CreateProfile(testCase.Profile);
        string input = CreateInput(testCase);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(input,
            profile.Policy, profile.Options, CancellationToken.None);

        Assert.That(result.IsSuccess(), Is.EqualTo(testCase.ExpectSuccess),
            testCase.Name);
        if (!testCase.ExpectSuccess)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.WasTruncated(), Is.True, testCase.Name);
                Assert.That(result.GetNode(), Is.Null, testCase.Name);
                Assert.That(result.GetHtml(), Is.Empty, testCase.Name);
                Assert.That(result.GetReport().WasLimitExceeded(), Is.True,
                    testCase.Name);
            });
            return;
        }

        string output = result.GetHtml();
        foreach (string required in testCase.Required)
        {
            Assert.That(output, Does.Contain(required), testCase.Name);
        }

        foreach (string forbidden in testCase.Forbidden)
        {
            Assert.That(output, Does.Not.Contain(forbidden).IgnoreCase,
                testCase.Name);
        }

        AssertSafeOutput(testCase, output);
    }

    [Test]
    public void CorpusCoversEveryPlannedAttackFamilyWithUniqueNames()
    {
        SanitizerCorpusCase[] cases = LoadCorpus().Cases;
        string[] expectedCategories =
        [
            "deep-large-input",
            "dom-clobbering",
            "entity-protocol-obfuscation",
            "malformed-url-srcset-css",
            "mutation-xss",
            "namespace-confusion",
            "raw-text-escape",
            "tag-poisoning"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(cases.Select(value => value.Category).Distinct()
                .OrderBy(value => value), Is.EqualTo(expectedCategories));
            Assert.That(cases.GroupBy(value => value.Category)
                .All(group => group.Count() >= 4), Is.True);
            Assert.That(cases.Select(value => value.Name).Distinct().Count(),
                Is.EqualTo(cases.Length));
        });
    }

    static IEnumerable<TestCaseData> GetCorpusCases()
    {
        foreach (SanitizerCorpusCase testCase in LoadCorpus().Cases)
        {
            yield return new TestCaseData(testCase).SetName(
                "Sanitizer corpus: " + testCase.Category + "/"
                + testCase.Name);
        }
    }

    static SanitizerCorpusDocument LoadCorpus()
    {
        string path = FindCorpusPath();
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        SanitizerCorpusDocument document =
            JsonSerializer.Deserialize<SanitizerCorpusDocument>(
                File.ReadAllText(path), options);
        if (document == null || document.Cases == null)
        {
            throw new InvalidDataException(
                "The sanitizer adversarial corpus is malformed.");
        }

        return document;
    }

    static string FindCorpusPath()
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, "tests", "data",
                "sanitization", "adversarial.json");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the sanitizer adversarial corpus.");
    }

    static SanitizerCorpusProfile CreateProfile(string name)
    {
        HtmlSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateRichText();
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        if (name == "rich-text")
        {
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        if (name == "strict-links")
        {
            policy.SetAllowRelativeUrls(false);
            policy.SetAllowProtocolRelativeUrls(false);
            policy.SetAllowUrlCredentials(false);
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        if (name == "rich-srcset")
        {
            policy.AllowAttributeOnElement("srcset", "img");
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        if (name == "inline-style")
        {
            policy.AllowAttribute("style");
            policy.AllowCssProperty("background-image");
            policy.AllowCssProperty("color");
            policy.AllowCssProperty("width");
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        if (name == "foreign-svg")
        {
            policy.AllowForeignNamespace(SvgNamespace);
            policy.AllowElementInNamespace(SvgNamespace, "svg");
            policy.AllowElementInNamespace(SvgNamespace, "a");
            policy.AllowElementInNamespace(SvgNamespace, "circle");
            policy.AllowAttributeOnNamespacedElement(XlinkNamespace, "href",
                SvgNamespace, "a");
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        if (name == "clobbering")
        {
            policy = new HtmlSanitizerPolicy();
            policy.AllowElement("a");
            policy.AllowElement("div");
            policy.AllowElement("form");
            policy.AllowElement("input");
            policy.AllowElement("p");
            policy.AllowAttribute("id");
            policy.AllowAttribute("name");
            policy.AllowAttributeOnElement("href", "a");
            policy.AllowUrlScheme("https");
            return new SanitizerCorpusProfile(policy.Compile(), options);
        }

        HtmlLimits limits = HtmlLimits.CreateDefault();
        if (name == "limit-depth")
        {
            limits.SetMaximumDepth(12);
        }
        else if (name == "limit-input")
        {
            limits.SetMaximumInputCharacters(128);
        }
        else if (name == "limit-token")
        {
            limits.SetMaximumTokenCharacters(32);
        }
        else if (name == "limit-output")
        {
            limits.SetMaximumOutputCharacters(32);
        }
        else
        {
            throw new InvalidOperationException(
                "Unknown sanitizer corpus profile: " + name);
        }

        options.SetLimits(limits);
        return new SanitizerCorpusProfile(policy.Compile(), options);
    }

    static string CreateInput(SanitizerCorpusCase testCase)
    {
        if (testCase.InputKind == "literal")
        {
            return testCase.Input;
        }

        if (testCase.InputKind == "text")
        {
            return "<p>" + new string('x', testCase.Size) + "</p>";
        }

        if (testCase.InputKind == "comment")
        {
            return "<!--" + new string('x', testCase.Size);
        }

        if (testCase.InputKind == "deep")
        {
            StringBuilder input = new StringBuilder();
            int index = 0;
            while (index < testCase.Size)
            {
                input.Append("<div>");
                index++;
            }

            input.Append("<p>SAFE</p>");
            index = 0;
            while (index < testCase.Size)
            {
                input.Append("</div>");
                index++;
            }

            return input.ToString();
        }

        throw new InvalidOperationException(
            "Unknown sanitizer corpus input kind: " + testCase.InputKind);
    }

    static void AssertSafeOutput(SanitizerCorpusCase testCase, string output)
    {
        HtmlParseResult parsed = Html.ParseFragment(output, "body");
        Assert.That(parsed.IsSuccess(), Is.True, testCase.Name);
        HtmlNode[] nodes = parsed.GetFragment().GetDescendants();
        foreach (HtmlNode node in nodes)
        {
            if (node.GetKind() != HtmlNodeKind.Element)
            {
                continue;
            }

            string name = node.GetLocalName();
            Assert.That(name, Is.Not.AnyOf("script", "style", "iframe",
                "object", "embed", "template", "noscript", "noembed",
                "frameset", "plaintext"), testCase.Name);
            if (testCase.Profile != "foreign-svg")
            {
                Assert.That(node.GetNamespaceUri(),
                    Is.Not.EqualTo(SvgNamespace), testCase.Name);
            }

            foreach (HtmlAttribute attribute in node.GetAttributes())
            {
                string attributeName = attribute.GetLocalName();
                string value = attribute.GetValue();
                Assert.That(attributeName.StartsWith("on",
                    StringComparison.OrdinalIgnoreCase), Is.False,
                    testCase.Name);
                Assert.That(RemoveAsciiControls(value),
                    Does.Not.StartWith("javascript:").IgnoreCase,
                    testCase.Name);
                if (attributeName == "style")
                {
                    Assert.That(value, Does.Not.Contain("expression")
                        .IgnoreCase, testCase.Name);
                    Assert.That(value, Does.Not.Contain("behavior")
                        .IgnoreCase, testCase.Name);
                    Assert.That(value, Does.Not.Contain("binding")
                        .IgnoreCase, testCase.Name);
                }
            }
        }
    }

    static string RemoveAsciiControls(string value)
    {
        StringBuilder result = new StringBuilder();
        foreach (char current in value)
        {
            if (current > 32 && current != 127)
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }
}

public class SanitizerCorpusDocument
{
    public SanitizerCorpusCase[] Cases { get; set; }
}

public class SanitizerCorpusCase
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Profile { get; set; }
    public string InputKind { get; set; }
    public string Input { get; set; }
    public int Size { get; set; }
    public bool ExpectSuccess { get; set; }
    public string[] Required { get; set; }
    public string[] Forbidden { get; set; }
}

public class SanitizerCorpusProfile
{
    public HtmlCompiledSanitizerPolicy Policy { get; }
    public HtmlParseOptions Options { get; }

    public SanitizerCorpusProfile(HtmlCompiledSanitizerPolicy policy,
        HtmlParseOptions options)
    {
        this.Policy = policy;
        this.Options = options;
    }
}
