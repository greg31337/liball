
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerNestedContentTests
{
    [Test]
    public void EnabledTemplateContentIsSanitizedRecursively()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<template><p title=ok onclick=run()>safe<script>bad()</script>"
            + "<template><strong>nested</strong><script>bad2()</script>"
            + "</template></p></template>", "div").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.SetAllowTemplateElements(true);
        builder.AllowElement("p");
        builder.AllowElement("strong");
        builder.AllowAttribute("title");

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());
        HtmlNode outer = fragment.GetFirstChild();
        HtmlNode paragraph = outer.GetTemplateContent().GetFirstChild();
        HtmlNode inner = paragraph.GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(outer.GetLocalName(), Is.EqualTo("template"));
            Assert.That(paragraph.GetLocalName(), Is.EqualTo("p"));
            Assert.That(paragraph.GetAttribute("title"), Is.EqualTo("ok"));
            Assert.That(paragraph.HasAttribute("onclick"), Is.False);
            Assert.That(paragraph.FindByTag("script"), Is.Empty);
            Assert.That(inner.GetLocalName(), Is.EqualTo("template"));
            Assert.That(inner.GetTemplateContent().FindByTag("strong"),
                Has.Length.EqualTo(1));
            Assert.That(inner.GetTemplateContent().FindByTag("script"),
                Is.Empty);
            Assert.That(HtmlSerializer.SerializeChildren(fragment),
                Is.EqualTo("<template><p title=\"ok\">safe<template>"
                    + "<strong>nested</strong></template></p></template>"));
        });
    }

    [Test]
    public void DisabledTemplateIsDroppedWithItsHiddenContent()
    {
        HtmlNode fragment = Html.ParseFragment(
            "before<template><p>hidden</p></template>after", "div")
            .GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());

        Assert.That(HtmlSerializer.SerializeChildren(fragment),
            Is.EqualTo("beforeafter"));
    }

    [Test]
    public void ExplicitlyAllowedSrcdocIsParsedSanitizedAndSerialized()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode iframe = document.CreateElement("iframe");
        iframe.SetAttribute("title", "Frame");
        iframe.SetAttribute("srcdoc",
            "<p title='inside' onclick='run()'>safe<script>bad()</script>"
            + "<b>bold</b></p>");
        fragment.Append(iframe);
        HtmlSanitizerPolicy builder = CreateSrcdocPolicy();

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(iframe.GetAttribute("title"), Is.EqualTo("Frame"));
            Assert.That(iframe.GetAttribute("srcdoc"),
                Is.EqualTo("<p title=\"inside\">safe<b>bold</b></p>"));
            Assert.That(iframe.GetAttributes().Select(value => value.GetName()),
                Is.EqualTo(new[] { "title", "srcdoc" }));
        });
    }

    [Test]
    public void SrcdocMustSurviveTheOrdinaryAttributeAllowlist()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode iframe = document.CreateElement("iframe");
        iframe.SetAttribute("srcdoc", "<p>safe</p><script>bad()</script>");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("iframe");
        builder.AllowElement("p");

        HtmlSanitizerAttributes.Apply(iframe, builder.Compile());

        Assert.That(iframe.HasAttribute("srcdoc"), Is.False);
    }

    [Test]
    public void NestedSrcdocDepthLimitFailsClosed()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode iframe = document.CreateElement("iframe");
        iframe.SetAttribute("srcdoc", "<p>safe</p>");
        HtmlCompiledSanitizerPolicy policy =
            CreateSrcdocPolicy().Compile();

        HtmlSanitizerAttributes.Apply(iframe, policy, 16);
        HtmlSanitizerNestedMarkupResult rejected =
            HtmlSanitizerNestedContent.SanitizeSrcdoc("<p>safe</p>", policy,
                17);

        Assert.Multiple(() =>
        {
            Assert.That(iframe.HasAttribute("srcdoc"), Is.False);
            Assert.That(rejected.IsSuccess(), Is.False);
            Assert.That(rejected.GetValue(), Is.Empty);
        });
    }

    [Test]
    public void SrcdocUsesTheParentSanitizerInputLimit()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode iframe = document.CreateElement("iframe");
        iframe.SetAttribute("srcdoc", "<p>long nested value</p>");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(8);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(iframe,
            CreateSrcdocPolicy().Compile(), limits,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetHtml(), Is.EqualTo("<iframe></iframe>"));
            Assert.That(result.GetHtml(), Does.Not.Contain("srcdoc"));
            Assert.That(result.GetHtml(), Does.Not.Contain("nested value"));
        });
    }

    [Test]
    public void SrcdocSanitizationRecursesIntoNestedSrcdocAttributes()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode outer = document.CreateElement("iframe");
        outer.SetAttribute("srcdoc",
            "<iframe srcdoc=\"&lt;p onclick='run()'&gt;inner"
            + "&lt;script&gt;bad()&lt;/script&gt;&lt;/p&gt;\"></iframe>");
        HtmlCompiledSanitizerPolicy policy =
            CreateSrcdocPolicy().Compile();

        HtmlSanitizerAttributes.Apply(outer, policy);
        HtmlNode nestedFragment = Html.ParseFragment(
            outer.GetAttribute("srcdoc"), "body").GetFragment();
        HtmlNode inner = nestedFragment.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(inner.GetLocalName(), Is.EqualTo("iframe"));
            Assert.That(inner.GetAttribute("srcdoc"),
                Is.EqualTo("<p>inner</p>"));
            Assert.That(inner.GetAttribute("srcdoc"),
                Does.Not.Contain("script"));
            Assert.That(inner.GetAttribute("srcdoc"),
                Does.Not.Contain("onclick"));
        });
    }

    [Test]
    public void TemplateMarkersRemainUntouchedByDefault()
    {
        const string value = "ordinary {{user}} ${value} <% code %> text";
        HtmlNode fragment = Html.ParseFragment(
            "<p title=\"ordinary {{title}}\">" + value + "</p>", "div")
            .GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("title");

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());
        HtmlNode paragraph = fragment.GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(paragraph.GetTextContent(), Is.EqualTo(value));
            Assert.That(paragraph.GetAttribute("title"),
                Is.EqualTo("ordinary {{title}}"));
        });
    }

    [Test]
    public void MarkerOptInNeutralizesTextCommentsAndAttributesIdempotently()
    {
        const string original = "{{{user}} ${value} <% code %> ordinary";
        HtmlNode fragment = Html.ParseFragment(
            "<p title=\"${title}\">" + original
            + "<!--{{comment}}--></p>", "div").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("title");
        builder.SetAllowComments(true);
        builder.SetNeutralizeServerTemplateMarkers(true);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerNestedContent.Apply(fragment, policy);
        string first = HtmlSerializer.SerializeChildren(fragment);
        HtmlSanitizerNestedContent.Apply(fragment, policy);
        string second = HtmlSerializer.SerializeChildren(fragment);
        string text = fragment.GetFirstChild().GetTextContent();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerTemplateMarkers.ContainsMarker(first),
                Is.False);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(text.Replace("\u200B", ""), Is.EqualTo(original));
            Assert.That(fragment.GetFirstChild().GetAttribute("title")
                .Replace("\u200B", ""), Is.EqualTo("${title}"));
            Assert.That(first.Replace("\u200B", ""),
                Does.Contain("<!--{{comment}}-->"));
        });
    }

    [Test]
    public void MarkerOptInHandlesTextMadeAdjacentByUnwrapping()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        fragment.Append(document.CreateText("{"));
        HtmlNode wrapper = document.CreateElement("span");
        wrapper.Append(document.CreateText("{user}}"));
        fragment.Append(wrapper);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.SetNeutralizeServerTemplateMarkers(true);

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());
        string serialized = HtmlSerializer.SerializeChildren(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerTemplateMarkers.ContainsMarker(
                serialized), Is.False);
            Assert.That(serialized.Replace("\u200B", ""),
                Is.EqualTo("{{user}}"));
        });
    }

    [Test]
    public void MarkerOptInNeutralizesFlattenedReplacementText()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<x-marker>{<b>{user}}</b></x-marker>", "div").GetFragment();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.BlockElement("x-marker",
            HtmlBlockedElementAction.ReplaceWithText);
        builder.SetNeutralizeServerTemplateMarkers(true);

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());
        string value = HtmlSerializer.SerializeChildren(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSanitizerTemplateMarkers.ContainsMarker(value),
                Is.False);
            Assert.That(value.Replace("\u200B", ""),
                Is.EqualTo("{{user}}"));
        });
    }

    [Test]
    public void MarkerPolicyCopiesAndCompiledSnapshotsAreIndependent()
    {
        HtmlSanitizerPolicy original = new HtmlSanitizerPolicy();
        original.SetNeutralizeServerTemplateMarkers(true);
        HtmlCompiledSanitizerPolicy compiled = original.Compile();
        HtmlSanitizerPolicy restored = compiled.CreatePolicy();
        restored.SetNeutralizeServerTemplateMarkers(false);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetNeutralizeServerTemplateMarkers(),
                Is.True);
            Assert.That(restored.Compile()
                .GetNeutralizeServerTemplateMarkers(), Is.False);
            Assert.That(HtmlSanitizerTemplateMarkers.Neutralize("ordinary"),
                Is.EqualTo("ordinary"));
            Assert.That(HtmlSanitizerTemplateMarkers.Neutralize("{{{")
                .Replace("\u200B", ""), Is.EqualTo("{{{"));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSanitizerTemplateMarkers.Neutralize(null));
        });
    }

    static HtmlSanitizerPolicy CreateSrcdocPolicy()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("iframe");
        builder.AllowElement("p");
        builder.AllowElement("b");
        builder.AllowAttribute("title");
        builder.AllowAttributeOnElement("srcdoc", "iframe");
        return builder;
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
