
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerComplexUrlTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
    const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

    [Test]
    public void SrcsetKeepsSafeRecoverableCandidatesAndDescriptors()
    {
        HtmlNode image = CreateHtmlElement("img", "srcset",
            "https://example.com/one.png 1x, javascript:alert(1) 2x, "
            + "broken.png 0w, https://example.com/two.png 400w");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("img");
        builder.AllowAttributeOnElement("srcset", "img");
        builder.AllowUrlScheme("https");
        builder.SetAllowRelativeUrls(false);

        HtmlSanitizerAttributes.Apply(image, builder.Compile());

        Assert.That(image.GetAttribute("srcset"), Is.EqualTo(
            "https://example.com/one.png 1x, "
            + "https://example.com/two.png 400w"));
    }

    [Test]
    public void PingFiltersEachUrlAndDropsTheAttributeWhenNoneSurvive()
    {
        HtmlNode mixed = CreateHtmlElement("a", "ping",
            "https://example.com/one javascript:alert(1) "
            + "https://example.com/two");
        HtmlNode unsafeOnly = CreateHtmlElement("a", "ping",
            "javascript:one data:text/plain,two");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowAttributeOnElement("ping", "a");
        builder.AllowUrlScheme("https");
        builder.SetAllowRelativeUrls(false);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(mixed, policy);
        HtmlSanitizerAttributes.Apply(unsafeOnly, policy);

        Assert.Multiple(() =>
        {
            Assert.That(mixed.GetAttribute("ping"), Is.EqualTo(
                "https://example.com/one https://example.com/two"));
            Assert.That(unsafeOnly.HasAttribute("ping"), Is.False);
        });
    }

    [Test]
    public void RefreshRequiresAParsedAllowedUrlAndIsCanonicalized()
    {
        HtmlNode safe = CreateRefresh(
            " 5.9 ; URL = 'https://example.com/next' ignored");
        HtmlNode unsafeNode = CreateRefresh("0;url=javascript:alert(1)");
        HtmlNode noUrl = CreateRefresh("0");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("meta");
        builder.AllowAttributeOnElement("http-equiv", "meta");
        builder.AllowAttributeOnElement("content", "meta");
        builder.AllowUrlScheme("https");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(safe, policy);
        HtmlSanitizerAttributes.Apply(unsafeNode, policy);
        HtmlSanitizerAttributes.Apply(noUrl, policy);

        Assert.Multiple(() =>
        {
            Assert.That(safe.GetAttribute("content"),
                Is.EqualTo("5;url=https://example.com/next"));
            Assert.That(unsafeNode.HasAttribute("content"), Is.False);
            Assert.That(noUrl.HasAttribute("content"), Is.False);
        });
    }

    [Test]
    public void XlinkHrefUsesTheSameSingleUrlPolicy()
    {
        HtmlNode safe = CreateSvgLink("#icon");
        HtmlNode unsafeNode = CreateSvgLink("javascript:alert(1)");
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElementInNamespace(SvgNamespaceUri, "a");
        builder.AllowAttributeOnNamespacedElement(XlinkNamespaceUri, "href",
            SvgNamespaceUri, "a");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(safe, policy);
        HtmlSanitizerAttributes.Apply(unsafeNode, policy);

        Assert.Multiple(() =>
        {
            Assert.That(safe.GetAttributesStorage().Length, Is.EqualTo(1));
            Assert.That(safe.GetAttributesStorage()[0].GetValue(),
                Is.EqualTo("#icon"));
            Assert.That(unsafeNode.GetAttributesStorage(), Is.Empty);
        });
    }

    [Test]
    public void DataUrlsEnforceMediaTypeBase64AndDecodedSize()
    {
        HtmlSanitizerPolicy builder = CreateDataImagePolicy(3);
        HtmlCompiledSanitizerPolicy policy = builder.Compile();
        HtmlNode acceptedNode = CreateHtmlElement("img", "src",
            "data:image/png;base64,AQID");
        HtmlNode rejectedNode = CreateHtmlElement("img", "src",
            "data:image/png;base64,AQIDBA==");

        HtmlSanitizerAttributes.Apply(acceptedNode, policy);
        HtmlSanitizerAttributes.Apply(rejectedNode, policy);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedNode.HasAttribute("src"), Is.True);
            Assert.That(rejectedNode.HasAttribute("src"), Is.False);
            Assert.That(EvaluateImageSource(
                "data:image/png;base64,AQID", policy).IsAllowed(), Is.True);
            Assert.That(EvaluateImageSource(
                "data:image/png;charset=utf-8;base64,AQID", policy)
                .IsAllowed(), Is.True);
            Assert.That(EvaluateImageSource(
                "data:image/png,%00%01%02", policy).IsAllowed(), Is.True);
            Assert.That(EvaluateImageSource(
                "data:image/png;base64,AQIDBA==", policy).IsAllowed(),
                Is.False);
            Assert.That(EvaluateImageSource(
                "data:image/png,%00%01%02%03", policy).IsAllowed(), Is.False);
            Assert.That(EvaluateImageSource(
                "data:image/svg+xml;base64,AQID", policy).IsAllowed(),
                Is.False);
            Assert.That(EvaluateImageSource(
                "data:image/png;base64,A===", policy).IsAllowed(), Is.False);
            Assert.That(EvaluateImageSource(
                "data:image/png;base64,AQ%2fD", policy).IsAllowed(), Is.True);
        });
    }

    [Test]
    public void DefaultDataMediaTypeAndSrcsetCandidatesUseTheDataPolicy()
    {
        HtmlSanitizerPolicy textBuilder = new HtmlSanitizerPolicy();
        textBuilder.AllowElement("img");
        textBuilder.AllowAttributeOnElement("src", "img");
        textBuilder.AllowUrlScheme("data");
        textBuilder.AllowDataUrlMediaType("text/plain");
        textBuilder.SetMaximumDataUrlBytes(3);
        HtmlCompiledSanitizerPolicy textPolicy = textBuilder.Compile();
        HtmlNode image = CreateHtmlElement("img", "srcset",
            "data:image/png;base64,AQID 1x, "
            + "data:image/png;base64,AQIDBA== 2x, safe.png 3x");
        HtmlSanitizerPolicy imageBuilder = CreateDataImagePolicy(3);
        imageBuilder.AllowAttributeOnElement("srcset", "img");
        HtmlCompiledSanitizerPolicy imagePolicy = imageBuilder.Compile();

        HtmlSanitizerAttributes.Apply(image, imagePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(EvaluateImageSource("data:,abc", textPolicy)
                .IsAllowed(), Is.True);
            Assert.That(EvaluateImageSource("data:,abcd", textPolicy)
                .IsAllowed(), Is.False);
            Assert.That(image.GetAttribute("srcset"), Is.EqualTo(
                "data:image/png;base64,AQID 1x, safe.png 3x"));
        });
    }

    [Test]
    public void OperationDataUriLimitCapsTheCompiledPolicyLimit()
    {
        HtmlNode image = CreateHtmlElement("img", "src",
            "data:image/png;base64,AQIDBA==");
        HtmlCompiledSanitizerPolicy policy = CreateDataImagePolicy(16)
            .Compile();
        HtmlLimits exactLimits = HtmlLimits.CreateDefault();
        exactLimits.SetMaximumDataUriBytes(4);
        HtmlLimits shortLimits = exactLimits.Copy();
        shortLimits.SetMaximumDataUriBytes(3);

        HtmlSanitizeResult exact = HtmlSanitizer.Sanitize(image, policy,
            exactLimits, CancellationToken.None);
        HtmlSanitizeResult limited = HtmlSanitizer.Sanitize(image, policy,
            shortLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsSuccess(), Is.True);
            Assert.That(exact.GetHtml(), Does.Contain("data:image/png"));
            Assert.That(limited.IsSuccess(), Is.True);
            Assert.That(limited.GetHtml(), Is.EqualTo("<img>"));
            Assert.That(limited.GetHtml(), Does.Not.Contain("data:"));
        });
    }

    [Test]
    public void OperationUrlCharacterLimitAppliesDuringSanitization()
    {
        const string url = "https://example.test/path";
        HtmlNode link = CreateHtmlElement("a", "href", url);
        link.Append(link.GetOwnerDocument().CreateText("link"));
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowAttributeOnElement("href", "a");
        builder.AllowUrlScheme("https");
        HtmlLimits exactLimits = HtmlLimits.CreateDefault();
        exactLimits.SetMaximumUrlCharacters(url.Length);
        HtmlLimits shortLimits = exactLimits.Copy();
        shortLimits.SetMaximumUrlCharacters(url.Length - 1);

        HtmlSanitizeResult exact = HtmlSanitizer.Sanitize(link,
            builder.Compile(), exactLimits, CancellationToken.None);
        HtmlSanitizeResult limited = HtmlSanitizer.Sanitize(link,
            builder.Compile(), shortLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exact.GetHtml(), Is.EqualTo(
                "<a href=\"https://example.test/path\">link</a>"));
            Assert.That(limited.IsSuccess(), Is.True);
            Assert.That(limited.GetHtml(), Is.EqualTo("<a>link</a>"));
        });
    }

    [Test]
    public void DataPolicySnapshotsAndInvalidConfigurationAreValidated()
    {
        HtmlSanitizerPolicy builder = CreateDataImagePolicy(12);
        HtmlCompiledSanitizerPolicy compiled = builder.Compile();
        string[] mediaTypes = compiled.GetAllowedDataUrlMediaTypes();
        mediaTypes[0] = "changed/type";
        HtmlSanitizerPolicy restored = compiled.CreatePolicy();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetAllowedDataUrlMediaTypes(),
                Is.EqualTo(new[] { "image/png" }));
            Assert.That(compiled.GetMaximumDataUrlBytes(), Is.EqualTo(12));
            Assert.That(restored.Compile().GetAllowedDataUrlMediaTypes(),
                Is.EqualTo(new[] { "image/png" }));
            Assert.Throws<ArgumentException>(() =>
                builder.AllowDataUrlMediaType("image"));
            Assert.Throws<ArgumentException>(() =>
                builder.AllowDataUrlMediaType("image/png;charset=utf-8"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.SetMaximumDataUrlBytes(-1));
            Assert.Throws<ArgumentException>(() =>
                HtmlSanitizerUrlPolicy.CreateWithDataUrls(
                    new[] { "data" },
                    Array.Empty<string>(), Array.Empty<int>(), false, true,
                    false, true, "", false,
                    new[] { "IMAGE/PNG", "image/png" }, 10));
        });
    }

    static HtmlSanitizerPolicy CreateDataImagePolicy(int maximumBytes)
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("img");
        builder.AllowAttributeOnElement("src", "img");
        builder.AllowUrlScheme("data");
        builder.AllowDataUrlMediaType("image/png");
        builder.SetMaximumDataUrlBytes(maximumBytes);
        return builder;
    }

    static HtmlSanitizerUrlDecision EvaluateImageSource(string value,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlNode image = CreateHtmlElement("img", "src", value);
        return HtmlSanitizerUrls.Evaluate(image,
            image.GetAttributesStorage()[0], policy);
    }

    static HtmlNode CreateRefresh(string content)
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute[] attributes =
        [
            HtmlAttribute.Create("http-equiv", "refresh", span),
            HtmlAttribute.Create("content", content, span)
        ];
        return CreateHtmlElement("meta", attributes);
    }

    static HtmlNode CreateSvgLink(string value)
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute attribute = HtmlAttribute.CreateNamespaced(
            "xlink:href", "href", "xlink", XlinkNamespaceUri, value, span);
        HtmlDocument document = CreateDocument();
        return document.CreateElementStorage(HtmlNamespace.Svg,
            SvgNamespaceUri, "a", "a", "", [attribute], span);
    }

    static HtmlNode CreateHtmlElement(string elementName,
        string attributeName, string value)
    {
        HtmlAttribute attribute = HtmlAttribute.Create(attributeName, value,
            HtmlSourceSpan.CreateUnknown());
        return CreateHtmlElement(elementName, [attribute]);
    }

    static HtmlNode CreateHtmlElement(string elementName,
        HtmlAttribute[] attributes)
    {
        HtmlDocument document = CreateDocument();
        return document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceUri, elementName, elementName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
