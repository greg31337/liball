
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerUrlPolicyTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";

    [Test]
    public void AbsoluteUrlsEnforceSchemeHostPortAndCredentialRules()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        builder.AllowUrlHost("example.com");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerUrlDecision allowed = EvaluateLink(
            "https://example.com/path", policy);
        HtmlSanitizerUrlDecision scheme = EvaluateLink(
            "http://example.com/path", policy);
        HtmlSanitizerUrlDecision host = EvaluateLink(
            "https://other.example/path", policy);
        HtmlSanitizerUrlDecision credentials = EvaluateLink(
            "https://user:pass@example.com/path", policy);
        HtmlSanitizerUrlDecision port = EvaluateLink(
            "https://example.com:8443/path", policy);

        builder.SetAllowUrlCredentials(true);
        builder.AllowUrlPort(8443);
        HtmlCompiledSanitizerPolicy widened = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(allowed.IsAllowed(), Is.True);
            Assert.That(allowed.GetValue(),
                Is.EqualTo("https://example.com/path"));
            Assert.That(scheme.IsAllowed(), Is.False);
            Assert.That(host.IsAllowed(), Is.False);
            Assert.That(credentials.IsAllowed(), Is.False);
            Assert.That(port.IsAllowed(), Is.False);
            Assert.That(EvaluateLink(
                "https://user:pass@example.com:8443/path", widened)
                .IsAllowed(), Is.True);
        });
    }

    [Test]
    public void ExplicitPortZeroIsRejectedWithoutThrowing()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();
        HtmlSanitizerUrlDecision link = EvaluateLink(
            "https://[::]:0/path", policy);

        builder.AllowCssProperty("background-image");
        HtmlSanitizerStyleResult style = HtmlSanitizerStyles.Sanitize(
            "background-image:url(https://[::]:0/path)",
            builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(link.IsAllowed(), Is.False);
            Assert.That(style.IsSuccess(), Is.True);
            Assert.That(style.GetValue(), Is.Empty);
        });
    }

    [Test]
    public void ReferenceKindsHaveIndependentControls()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        builder.AllowUrlHost("example.com");
        builder.SetAllowRelativeUrls(false);
        builder.SetAllowProtocolRelativeUrls(false);
        builder.SetAllowFragmentUrls(false);
        HtmlCompiledSanitizerPolicy closed = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateLink("next", closed).IsAllowed(), Is.False);
            Assert.That(EvaluateLink("//example.com/x", closed).IsAllowed(),
                Is.False);
            Assert.That(EvaluateLink("#part", closed).IsAllowed(), Is.False);
        });

        builder.SetAllowRelativeUrls(true);
        builder.SetAllowProtocolRelativeUrls(true);
        builder.SetAllowFragmentUrls(true);
        builder.SetUrlBase("https://example.com/root/page");
        HtmlCompiledSanitizerPolicy preserving = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateLink("../next", preserving).GetValue(),
                Is.EqualTo("../next"));
            Assert.That(EvaluateLink("//example.com/x", preserving).GetValue(),
                Is.EqualTo("//example.com/x"));
            Assert.That(EvaluateLink("#part", preserving).GetValue(),
                Is.EqualTo("#part"));
        });

        builder.SetRewriteRelativeUrls(true);
        HtmlCompiledSanitizerPolicy rewriting = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateLink("../next", rewriting).GetValue(),
                Is.EqualTo("https://example.com/next"));
            Assert.That(EvaluateLink("#part", rewriting).GetValue(),
                Is.EqualTo("https://example.com/root/page#part"));
        });
    }

    [Test]
    public void TrustedPolicyBaseIgnoresDocumentUrlAndBaseMarkup()
    {
        HtmlDocument document = CreateDocument("https://evil.example/root/");
        HtmlNode link = document.CreateElement("a");
        link.SetAttribute("href", "child");
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        builder.AllowUrlHost("trusted.example");
        builder.SetUrlBase("https://trusted.example/safe/");
        builder.SetRewriteRelativeUrls(true);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerUrlDecision decision = HtmlSanitizerUrls.Evaluate(link,
            link.GetAttributesStorage()[0], policy);
        HtmlSanitizerAttributes.Apply(link, policy);

        Assert.Multiple(() =>
        {
            Assert.That(decision.IsAllowed(), Is.True);
            Assert.That(decision.GetValue(),
                Is.EqualTo("https://trusted.example/safe/child"));
            Assert.That(decision.WasRewritten(), Is.True);
            Assert.That(link.GetAttribute("href"),
                Is.EqualTo("https://trusted.example/safe/child"));
        });
    }

    [Test]
    public void PerElementOverrideReplacesTheGlobalUrlPolicy()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowElement("img");
        builder.AllowAttributeOnElement("src", "img");
        builder.AllowUrlScheme("https");
        HtmlSanitizerUrlPolicy mailPolicy = new HtmlSanitizerUrlPolicy(
            new[] { "mailto" }, Array.Empty<string>(), Array.Empty<int>(),
            false, false, false, false, "", false);
        builder.SetUrlPolicyOnElement("href", "a", mailPolicy);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();
        HtmlNode image = CreateElement("img", "src",
            "https://cdn.example/image.png");
        HtmlAttribute imageSource = image.GetAttributesStorage()[0];

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateLink("mailto:user@example.com", policy)
                .IsAllowed(), Is.True);
            Assert.That(EvaluateLink("https://example.com", policy)
                .IsAllowed(), Is.False);
            Assert.That(HtmlSanitizerUrls.Evaluate(image, imageSource, policy)
                .IsAllowed(), Is.True);
            Assert.That(policy.GetUrlPolicy(HtmlNamespaceUri, "a", "", "href")
                .GetAllowedSchemes(), Is.EqualTo(new[] { "mailto" }));
        });
    }

    [Test]
    public void AttributeApplicationDropsUnsafeAndFiltersForcedUrlValues()
    {
        HtmlNode link = CreateElement("a", "href",
            "java&#x73;cript:alert(1)");
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(link, policy);

        HtmlNode forced = CreateElement("a", "title", "link");
        HtmlSanitizerPolicy forcedBuilder = CreateLinkPolicy();
        forcedBuilder.AllowAttribute("title");
        forcedBuilder.AllowUrlScheme("https");
        forcedBuilder.ForceAttribute("a", "href", "javascript:alert(1)");

        HtmlSanitizerAttributes.Apply(forced, forcedBuilder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(link.HasAttribute("href"), Is.False);
            Assert.That(forced.HasAttribute("href"), Is.False);
            Assert.That(forced.GetAttribute("title"), Is.EqualTo("link"));
        });
    }

    [Test]
    public void UrlPolicyCopiesAndCompiledSnapshotsAreIndependent()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        builder.AllowUrlScheme("https");
        builder.AllowUrlHost("example.com");
        builder.AllowUrlPort(8443);
        builder.SetUrlBase("https://example.com/root/");
        HtmlSanitizerUrlPolicy overridePolicy =
            new HtmlSanitizerUrlPolicy(new[] { "mailto" },
                Array.Empty<string>(), Array.Empty<int>(), false, false,
                false, false, "", false);
        builder.SetUrlPolicyOnElement("href", "a", overridePolicy);
        HtmlCompiledSanitizerPolicy compiled = builder.Compile();
        HtmlSanitizerPolicy copy = compiled.CreatePolicy();
        string[] hosts = compiled.GetGlobalUrlPolicy().GetAllowedHosts();
        int[] ports = compiled.GetGlobalUrlPolicy().GetAllowedPorts();
        HtmlSanitizerUrlAttributeRule[] rules =
            compiled.GetUrlAttributeRules();
        hosts[0] = "changed";
        ports[0] = 1;
        rules[0] = null;
        copy.AllowUrlScheme("http");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetGlobalUrlPolicy().GetAllowedHosts(),
                Is.EqualTo(new[] { "example.com" }));
            Assert.That(compiled.GetGlobalUrlPolicy().GetAllowedPorts(),
                Is.EqualTo(new[] { 8443 }));
            Assert.That(compiled.GetUrlAttributeRules()[0].GetPolicy()
                .GetAllowedSchemes(), Is.EqualTo(new[] { "mailto" }));
            Assert.That(compiled.GetAllowedUrlSchemes(),
                Is.EqualTo(new[] { "https" }));
            Assert.That(copy.Compile().GetAllowedUrlSchemes(),
                Is.EqualTo(new[] { "http", "https" }));
        });
    }

    [Test]
    public void InvalidUrlConfigurationAndMalformedReferencesAreRejected()
    {
        HtmlSanitizerPolicy builder = CreateLinkPolicy();
        HtmlSanitizerPolicy unresolved = new HtmlSanitizerPolicy();
        HtmlSanitizerUrlPolicy urlPolicy = new HtmlSanitizerUrlPolicy(
            new[] { "https" }, Array.Empty<string>(), Array.Empty<int>(),
            false, true, false, true, "", false);
        unresolved.SetUrlPolicyOnElement("href", "a", urlPolicy);
        builder.AllowUrlScheme("https");
        HtmlCompiledSanitizerPolicy compiled = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AllowUrlPort(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.AllowUrlPort(65536));
            Assert.Throws<ArgumentException>(() =>
                builder.AllowUrlHost("example.com:443"));
            Assert.Throws<ArgumentException>(() =>
                builder.SetUrlBase("relative/path"));
            Assert.Throws<InvalidOperationException>(() =>
                new HtmlSanitizerPolicy().SetRewriteRelativeUrls(true));
            Assert.Throws<InvalidOperationException>(() =>
                unresolved.Compile());
            Assert.That(EvaluateLink("bad%zz", compiled).IsAllowed(),
                Is.False);
        });
    }

    static HtmlSanitizerPolicy CreateLinkPolicy()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowAttributeOnElement("href", "a");
        return builder;
    }

    static HtmlSanitizerUrlDecision EvaluateLink(string value,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlNode link = CreateElement("a", "href", value);
        return HtmlSanitizerUrls.Evaluate(link,
            link.GetAttributesStorage()[0], policy);
    }

    static HtmlNode CreateElement(string elementName, string attributeName,
        string value)
    {
        HtmlDocument document = CreateDocument("about:blank");
        HtmlAttribute attribute = HtmlAttribute.Create(attributeName, value,
            HtmlSourceSpan.CreateUnknown());
        return document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceUri, elementName, elementName, "", [attribute],
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument(string url)
    {
        return new HtmlDocument(url, url, HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}
