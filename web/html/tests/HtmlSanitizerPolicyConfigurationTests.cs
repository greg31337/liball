using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerPolicyConfigurationTests
{
    [Test]
    public void ValidationAndPrefixActionsTransformOnlyMatchingValues()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<p lang='en-US' data-count='12' title='item'></p>"
            + "<p lang='bad_tag' data-count='1x' title='safe-item'></p>",
            "body").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.ValidateAttribute("lang",
            HtmlSanitizerAttributeValidation.LanguageTag);
        builder.ValidateAttributeOnElement("data-count", "p",
            HtmlSanitizerAttributeValidation.Integer);
        builder.PrefixAttributeOnElement("title", "p", "safe-");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerNestedContent.Apply(fragment, policy);
        string first = HtmlSerializer.SerializeChildren(fragment);
        HtmlSanitizerNestedContent.Apply(fragment, policy);
        string second = HtmlSerializer.SerializeChildren(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(
                "<p lang=\"en-US\" data-count=\"12\" title=\"safe-item\"></p>"
                + "<p title=\"safe-item\"></p>"));
            Assert.That(second, Is.EqualTo(first));
            HtmlSanitizerAttributeRule[] rules =
                policy.GetSanitizerAttributeRules();
            Assert.That(rules.Any(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Validate), Is.True);
            Assert.That(rules.Any(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Prefix
                && value.GetValuePrefix() == "safe-"), Is.True);
        });
    }

    [Test]
    public void CssControlsRestrictFunctionsKeywordsUnitsAndCustomProperties()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<p style='color:rgb(1 2 3)'></p>"
            + "<p style='display:block'></p>"
            + "<p style='width:1px'></p>"
            + "<p style='width:2em'></p>"
            + "<p style='--tone:blue;color:var(--tone)'></p>",
            "body").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("style");
        builder.AllowCssProperty("color");
        builder.AllowCssProperty("display");
        builder.AllowCssProperty("width");
        builder.BlockCssFunction("rgb");
        builder.BlockCssKeyword("block");
        builder.BlockCssUnit("px");
        builder.SetAllowCssCustomProperties(true);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerNestedContent.Apply(fragment, policy);
        HtmlNode[] paragraphs = fragment.GetChildren();

        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[0].HasAttribute("style"), Is.False);
            Assert.That(paragraphs[1].HasAttribute("style"), Is.False);
            Assert.That(paragraphs[2].HasAttribute("style"), Is.False);
            Assert.That(paragraphs[3].GetAttribute("style"),
                Is.EqualTo("width:2em"));
            Assert.That(paragraphs[4].GetAttribute("style"),
                Is.EqualTo("--tone:blue;color:var(--tone)"));
            Assert.That(policy.IsCssFunctionAllowed("rgb"), Is.False);
            Assert.That(policy.IsCssFunctionAllowed("calc"), Is.True);
            Assert.That(policy.IsCssKeywordAllowed("block"), Is.False);
            Assert.That(policy.IsCssUnitAllowed("px"), Is.False);
            Assert.That(policy.GetAllowCssCustomProperties(), Is.True);
        });
    }

    [Test]
    public void NamespaceAwareValidationAppliesOnlyToConfiguredElement()
    {
        const string svg = "http://www.w3.org/2000/svg";
        HtmlNode fragment = Html.ParseFragment(
            "<svg><g data-level='3'></g><path data-level='bad'></path></svg>",
            "body").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowForeignNamespace(svg);
        builder.ValidateAttributeOnNamespacedElement("", "data-level", svg,
            "g", HtmlSanitizerAttributeValidation.Integer);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerNestedContent.Apply(fragment, policy);

        Assert.That(HtmlSerializer.SerializeChildren(fragment), Is.EqualTo(
            "<svg><g data-level=\"3\"></g><path></path></svg>"));
    }
}
