using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerPropertyTests
{
    [Test]
    public void SuccessfulCanonicalOutputReparsesToTheSameTree()
    {
        HtmlCompiledSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateRichText().Compile();
        SanitizerInputGenerator generator =
            new SanitizerInputGenerator(0x51A71E2u);

        int index = 0;
        while (index < 300)
        {
            string input = generator.CreateFragment();
            HtmlSanitizeResult sanitized = Sanitize(input, policy,
                HtmlLimits.CreateDefault());
            Assert.That(sanitized.IsSuccess(), Is.True,
                Describe(index, input));

            HtmlParseResult reparsed = Html.ParseFragment(
                sanitized.GetHtml(), "body");
            Assert.Multiple(() =>
            {
                Assert.That(reparsed.IsSuccess(), Is.True,
                    Describe(index, input));
                Assert.That(reparsed.IsComplete(), Is.True,
                    Describe(index, input));
                Assert.That(reparsed.WasTruncated(), Is.False,
                    Describe(index, input));
                Assert.That(HtmlSerializer.SerializeChildren(
                    reparsed.GetFragment()), Is.EqualTo(sanitized.GetHtml()),
                    Describe(index, input));
            });
            AssertNoBlockedCapabilities(reparsed.GetFragment(),
                Describe(index, input));
            index++;
        }
    }

    [Test]
    public void SanitizationIsIdempotentAcrossGeneratedInputs()
    {
        HtmlCompiledSanitizerPolicy policy = CreatePropertyPolicy();
        SanitizerInputGenerator generator =
            new SanitizerInputGenerator(0x1DEB907Eu);

        int index = 0;
        while (index < 300)
        {
            string input = generator.CreateFragment();
            HtmlSanitizeResult first = Sanitize(input, policy,
                HtmlLimits.CreateDefault());
            HtmlSanitizeResult second = Sanitize(first.GetHtml(), policy,
                HtmlLimits.CreateDefault());

            Assert.Multiple(() =>
            {
                Assert.That(first.IsSuccess(), Is.True,
                    Describe(index, input));
                Assert.That(second.IsSuccess(), Is.True,
                    Describe(index, input));
                Assert.That(second.GetHtml(), Is.EqualTo(first.GetHtml()),
                    Describe(index, input));
            });
            index++;
        }
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("JaVaScRiPt:alert(1)")]
    [TestCase("jav&#x61;script:alert(1)")]
    [TestCase("java&#115;cript:alert(1)")]
    [TestCase("javascript&colon;alert(1)")]
    [TestCase("java&#x09;script:alert(1)")]
    [TestCase("java&#x0A;script:alert(1)")]
    [TestCase("java&#x0D;script:alert(1)")]
    [TestCase("java\0script:alert(1)")]
    [TestCase("  javascript:alert(1)")]
    public void EncodedJavascriptProtocolsNeverSurvive(string encoded)
    {
        string input = "<p><a href='" + encoded
            + "'>SAFE</a><img src='" + encoded + "'></p>";
        HtmlSanitizeResult result = Sanitize(input,
            HtmlSanitizerPolicy.CreateRichText().Compile(),
            HtmlLimits.CreateDefault());
        HtmlParseResult reparsed = Html.ParseFragment(result.GetHtml(),
            "body");

        Assert.That(result.IsSuccess(), Is.True);
        foreach (HtmlNode node in reparsed.GetFragment().GetDescendants())
        {
            if (node.GetKind() == HtmlNodeKind.Element)
            {
                Assert.That(node.HasAttribute("href"), Is.False, encoded);
                Assert.That(node.HasAttribute("src"), Is.False, encoded);
            }
        }
    }

    [TestCase("<script>alert(1)</script><p>SAFE</p>")]
    [TestCase("<ScRiPt>alert(1)</sCrIpT><p>SAFE</p>")]
    [TestCase("<scr&#x69;pt>alert(1)</scr&#x69;pt><p>SAFE</p>")]
    [TestCase("&#60;script&#62;alert(1)&#60;/script&#62;<p>SAFE</p>")]
    [TestCase("<svg><script>alert(1)</script></svg><p>SAFE</p>")]
    [TestCase("<math><script>alert(1)</script></math><p>SAFE</p>")]
    public void EncodedOrNamespacedBlockedElementsNeverBecomeActive(
        string input)
    {
        HtmlSanitizeResult result = Sanitize(input,
            HtmlSanitizerPolicy.CreateRichText().Compile(),
            HtmlLimits.CreateDefault());
        HtmlParseResult reparsed = Html.ParseFragment(result.GetHtml(),
            "body");

        Assert.That(result.IsSuccess(), Is.True);
        AssertNoBlockedCapabilities(reparsed.GetFragment(), input);
    }

    [Test]
    public void SequentialPresetCompositionNeverWidensTheNarrowerPreset()
    {
        HtmlCompiledSanitizerPolicy[] presets =
        [
            HtmlSanitizerPolicy.CreateTextOnly().Compile(),
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile(),
            HtmlSanitizerPolicy.CreateRichText().Compile(),
            HtmlSanitizerPolicy.CreateHtmlOnly().Compile()
        ];
        SanitizerInputGenerator generator =
            new SanitizerInputGenerator(0xC05E71u);

        int caseIndex = 0;
        while (caseIndex < 160)
        {
            string input = generator.CreateFragment();
            int narrowIndex = 0;
            while (narrowIndex + 1 < presets.Length)
            {
                HtmlCompiledSanitizerPolicy narrow = presets[narrowIndex];
                HtmlCompiledSanitizerPolicy broad = presets[narrowIndex + 1];
                string narrowOnly = Sanitize(input, narrow,
                    HtmlLimits.CreateDefault()).GetHtml();
                string broadThenNarrow = Sanitize(Sanitize(input, broad,
                    HtmlLimits.CreateDefault()).GetHtml(), narrow,
                    HtmlLimits.CreateDefault()).GetHtml();
                string narrowThenBroad = Sanitize(narrowOnly, broad,
                    HtmlLimits.CreateDefault()).GetHtml();

                AssertWithinPolicy(narrowOnly, narrow,
                    Describe(caseIndex, input));
                AssertWithinPolicy(broadThenNarrow, narrow,
                    Describe(caseIndex, input));
                AssertWithinPolicy(narrowThenBroad, narrow,
                    Describe(caseIndex, input));
                narrowIndex++;
            }

            caseIndex++;
        }
    }

    [Test]
    public void ReportRetentionLimitsNeverChangeSanitizationDecisions()
    {
        HtmlCompiledSanitizerPolicy policy = CreatePropertyPolicy();
        SanitizerInputGenerator generator =
            new SanitizerInputGenerator(0xAE9017u);
        HtmlLimits retainedOne = HtmlLimits.CreateDefault();
        retainedOne.SetMaximumDiagnostics(1);
        HtmlLimits retainedMany = HtmlLimits.CreateDefault();
        retainedMany.SetMaximumDiagnostics(1000);

        int index = 0;
        while (index < 200)
        {
            string input = generator.CreateFragment();
            HtmlParseResult parsed = Html.ParseFragment(input, "body");
            Assert.That(parsed.IsSuccess(), Is.True,
                Describe(index, input));
            HtmlNode firstNode = parsed.GetFragment().Clone(true);
            HtmlNode secondNode = parsed.GetFragment().Clone(true);
            HtmlSanitizerReport first =
                HtmlSanitizerNestedContent.ApplyWithReport(firstNode,
                    policy, retainedOne);
            HtmlSanitizerReport second =
                HtmlSanitizerNestedContent.ApplyWithReport(secondNode,
                    policy, retainedMany);

            Assert.Multiple(() =>
            {
                Assert.That(HtmlSerializer.SerializeChildren(firstNode),
                    Is.EqualTo(HtmlSerializer.SerializeChildren(secondNode)),
                    Describe(index, input));
                Assert.That(first.GetTotalCount(),
                    Is.EqualTo(second.GetTotalCount()),
                    Describe(index, input));
                Assert.That(first.GetReasonCount(
                    HtmlSanitizerChangeReason.AttributeNotAllowed),
                    Is.EqualTo(second.GetReasonCount(
                        HtmlSanitizerChangeReason.AttributeNotAllowed)),
                    Describe(index, input));
            });
            index++;
        }
    }

    static HtmlSanitizeResult Sanitize(string input,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLimits(limits);
        return HtmlSanitizer.Sanitize(input, policy, options,
            CancellationToken.None);
    }

    static HtmlCompiledSanitizerPolicy CreatePropertyPolicy()
    {
        HtmlSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateRichText();
        policy.AllowAttribute("id");
        policy.AllowAttribute("style");
        policy.AllowAttributeOnElement("srcset", "img");
        policy.AllowCssProperty("background-image");
        policy.AllowCssProperty("color");
        policy.AllowCssProperty("width");
        return policy.Compile();
    }

    static void AssertNoBlockedCapabilities(HtmlNode root, string detail)
    {
        foreach (HtmlNode node in root.GetDescendants())
        {
            if (node.GetKind() != HtmlNodeKind.Element)
            {
                continue;
            }

            Assert.That(node.GetLocalName(), Is.Not.AnyOf("script", "style",
                "iframe", "object", "embed", "template", "noscript",
                "noembed", "frameset"), detail);
            Assert.That(node.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html), detail);
            foreach (HtmlAttribute attribute in node.GetAttributes())
            {
                Assert.That(attribute.GetLocalName().StartsWith("on",
                    StringComparison.OrdinalIgnoreCase), Is.False, detail);
                if (HtmlDomUrls.GetAttributeKind(node, attribute)
                    != HtmlUrlAttributeKind.None)
                {
                    string compact = RemoveAsciiControls(
                        attribute.GetValue());
                    Assert.That(compact, Does.Not.Contain("javascript:")
                        .IgnoreCase, detail);
                }

                if (attribute.GetLocalName() == "style")
                {
                    Assert.That(attribute.GetValue(),
                        Does.Not.Contain("expression").IgnoreCase, detail);
                    Assert.That(attribute.GetValue(),
                        Does.Not.Contain("behavior").IgnoreCase, detail);
                    Assert.That(attribute.GetValue(),
                        Does.Not.Contain("binding").IgnoreCase, detail);
                }
            }
        }
    }

    static void AssertWithinPolicy(string output,
        HtmlCompiledSanitizerPolicy policy, string detail)
    {
        HtmlParseResult parsed = Html.ParseFragment(output, "body");
        Assert.That(parsed.IsSuccess(), Is.True, detail);
        foreach (HtmlNode node in parsed.GetFragment().GetDescendants())
        {
            if (node.GetKind() != HtmlNodeKind.Element)
            {
                continue;
            }

            Assert.That(policy.IsElementAllowedInNamespace(node.GetNamespaceUri(),
                node.GetLocalName()), Is.True, detail);
            foreach (HtmlAttribute attribute in node.GetAttributes())
            {
                Assert.That(policy.IsAttributeAllowedInNamespaces(
                    attribute.GetNamespaceUri(), attribute.GetLocalName(),
                    node.GetNamespaceUri(), node.GetLocalName()), Is.True,
                    detail);
            }
        }

        AssertNoBlockedCapabilities(parsed.GetFragment(), detail);
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

    static string Describe(int index, string input)
    {
        return "case=" + index + " input=" + input;
    }
}

class SanitizerInputGenerator
{
    static readonly string[] Elements =
    [
        "p", "div", "strong", "em", "a", "img", "ul", "li", "table",
        "tr", "td", "script", "style", "svg", "math", "form", "input",
        "template", "video", "audio", "x-box"
    ];

    static readonly string[] Attributes =
    [
        "class", "title", "id", "href", "src", "srcset", "target",
        "rel", "style", "onclick", "onerror", "name", "data-value"
    ];

    static readonly string[] Values =
    [
        "safe", " location ", "_blank", "https://example.test/x",
        "relative.png", "javascript:alert(1)", "jav&#x61;script:alert(1)",
        "safe.png 1x, javascript:alert(1) 2x", "color:red;width:10px",
        "color:blue;behavior:url(x)", "{{marker}}", "a b c", "&quot;x&quot;"
    ];

    static readonly string[] Text =
    [
        "SAFE", "alpha &amp; beta", "<", "{{value}}", "\0", "\r\n",
        "😀", "</script>", "javascript:alert(1)"
    ];

    uint state;

    public SanitizerInputGenerator(uint seed)
    {
        this.state = seed;
    }

    public string CreateFragment()
    {
        StringBuilder result = new StringBuilder();
        int count = 2 + this.Next(8);
        int index = 0;
        while (index < count)
        {
            this.AppendNode(result, 0);
            index++;
        }

        return result.ToString();
    }

    void AppendNode(StringBuilder result, int depth)
    {
        if (depth >= 3 || this.Next(5) == 0)
        {
            result.Append(Text[this.Next(Text.Length)]);
            return;
        }

        string element = Elements[this.Next(Elements.Length)];
        result.Append('<');
        result.Append(element);
        int attributeCount = this.Next(4);
        int attributeIndex = 0;
        while (attributeIndex < attributeCount)
        {
            result.Append(' ');
            result.Append(Attributes[this.Next(Attributes.Length)]);
            result.Append("=\"");
            result.Append(Values[this.Next(Values.Length)]);
            result.Append('"');
            attributeIndex++;
        }

        result.Append('>');
        if (element != "img" && element != "input")
        {
            int childCount = 1 + this.Next(3);
            int childIndex = 0;
            while (childIndex < childCount)
            {
                this.AppendNode(result, depth + 1);
                childIndex++;
            }

            if (this.Next(5) != 0)
            {
                result.Append("</");
                result.Append(this.Next(6) == 0
                    ? Elements[this.Next(Elements.Length)] : element);
                result.Append('>');
            }
        }
    }

    int Next(int maximum)
    {
        this.state = this.state * 1664525u + 1013904223u;
        return (int)(this.state % (uint)maximum);
    }
}
