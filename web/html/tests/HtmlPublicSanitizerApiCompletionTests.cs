namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicSanitizerApiCompletionTests
{
    const string HtmlNamespace = "http://www.w3.org/1999/xhtml";
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string XlinkNamespace = "http://www.w3.org/1999/xlink";

    [Test]
    public void PublicFactoryExposesEverySanitizerPreset()
    {
        IHtmlFactory factory = new HtmlFactory();
        HtmlCompiledSanitizerPolicy text =
            factory.CreateTextOnlySanitizerPolicy().Compile();
        HtmlCompiledSanitizerPolicy basic =
            factory.CreateBasicFormattingSanitizerPolicy().Compile();
        HtmlCompiledSanitizerPolicy rich =
            factory.CreateRichTextSanitizerPolicy().Compile();
        HtmlCompiledSanitizerPolicy html =
            factory.CreateHtmlOnlySanitizerPolicy().Compile();

        Assert.Multiple(() =>
        {
            Assert.That(text.GetAllowedElements(), Is.Empty);
            Assert.That(basic.IsElementAllowed("strong"), Is.True);
            Assert.That(basic.IsElementAllowed("a"), Is.False);
            Assert.That(rich.IsElementAllowed("a"), Is.True);
            Assert.That(rich.IsElementAllowed("video"), Is.False);
            Assert.That(html.IsElementAllowed("video"), Is.True);
            Assert.That(html.IsElementAllowed("script"), Is.False);
        });
    }

    [Test]
    public void LegacySanitizerApiSupportsEveryCloneAndInPlaceOperation()
    {
        IHtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlCompiledSanitizerPolicy policy = CreateContentPolicy();
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlNode cloneInput = ParseSubtree();
        HtmlNode boundedCloneInput = ParseSubtree();
        HtmlNode inPlaceInput = ParseSubtree();
        HtmlNode boundedInPlaceInput = ParseSubtree();

        HtmlSanitizeResult html = api.SanitizeHtml(
            "<p onclick=secret>safe<script>bad</script></p>", policy,
            options, CancellationToken.None);
        HtmlSanitizeResult clone = api.SanitizeNode(cloneInput, policy,
            CancellationToken.None);
        HtmlSanitizeResult boundedClone = api.SanitizeNodeWithLimits(
            boundedCloneInput, policy, limits, CancellationToken.None);
        HtmlSanitizeResult inPlace = api.SanitizeInPlace(inPlaceInput,
            policy, CancellationToken.None);
        HtmlSanitizeResult boundedInPlace = api.SanitizeInPlaceWithLimits(
            boundedInPlaceInput, policy, limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(html.IsSuccess(), Is.True);
            Assert.That(html.GetHtml(), Does.Contain("<p>safe</p>"));
            Assert.That(clone.GetSubtree(), Is.Not.SameAs(cloneInput));
            Assert.That(clone.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(boundedClone.GetHtml(), Is.EqualTo("<p>safe</p>"));
            Assert.That(HtmlSerializer.Serialize(cloneInput), Is.EqualTo(
                "<p onclick=\"secret\">safe<script>bad</script></p>"));
            Assert.That(inPlace.GetSubtree(), Is.SameAs(inPlaceInput));
            Assert.That(HtmlSerializer.Serialize(inPlaceInput),
                Is.EqualTo("<p>safe</p>"));
            Assert.That(boundedInPlace.GetSubtree(),
                Is.SameAs(boundedInPlaceInput));
            Assert.That(HtmlSerializer.Serialize(boundedInPlaceInput),
                Is.EqualTo("<p>safe</p>"));
        });
    }

    [Test]
    public void LegacySanitizerApiHonorsCancellationBeforeMutation()
    {
        IHtmlSanitizerApi api = new HtmlSanitizerApi();
        HtmlCompiledSanitizerPolicy policy = CreateContentPolicy();
        HtmlNode subtree = ParseSubtree();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            api.SanitizeHtml("<p>safe</p>", policy,
                HtmlParseOptions.CreateDefault(), cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.SanitizeNodeWithLimits(subtree, policy,
                HtmlLimits.CreateDefault(), cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.SanitizeInPlaceWithLimits(subtree, policy,
                HtmlLimits.CreateDefault(), cancellation.Token));
        Assert.That(HtmlSerializer.Serialize(subtree), Is.EqualTo(
            "<p onclick=\"secret\">safe<script>bad</script></p>"));
    }

    [Test]
    public void MutablePolicySupportsEveryNamespaceSpecificRuleOperation()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowElementInNamespace(SvgNamespace, "use");
        builder.AllowForeignNamespace(SvgNamespace);
        builder.BlockAttributeInNamespace(XlinkNamespace, "blocked");
        builder.BlockAttributeOnNamespacedElement(XlinkNamespace,
            "blocked-use", SvgNamespace, "use");
        builder.NormalizeAttributeInNamespace("", "data-global",
            HtmlSanitizerAttributeNormalization.Trim);
        builder.NormalizeAttributeOnNamespacedElement("", "data-use",
            SvgNamespace, "use",
            HtmlSanitizerAttributeNormalization.LowercaseAscii);
        builder.PrefixAttribute("title", "safe-");
        builder.PrefixAttributeInNamespace(XlinkNamespace, "label",
            "safe-");
        builder.PrefixAttributeOnNamespacedElement("", "data-prefix",
            SvgNamespace, "use", "safe-");
        builder.RenameAttributeOnElement("data-old", "a", "data-new");
        builder.ValidateAttributeInNamespace("", "lang",
            HtmlSanitizerAttributeValidation.LanguageTag);
        builder.AllowAttributeOnNamespacedElement(XlinkNamespace, "href",
            SvgNamespace, "use");
        HtmlSanitizerUrlPolicy global = new HtmlSanitizerUrlPolicy(
            new[] { "https" }, new[] { "example.test" },
            new[] { 443 }, false, true, false, true,
            "https://example.test/root/", false);
        HtmlSanitizerUrlPolicy svg = new HtmlSanitizerUrlPolicy(
            new[] { "mailto" }, Array.Empty<string>(), Array.Empty<int>(),
            false, false, false, false, "", false);
        builder.SetUrlPolicy(global);
        builder.SetUrlPolicyOnNamespacedElement(XlinkNamespace, "href",
            SvgNamespace, "use", svg);

        HtmlCompiledSanitizerPolicy compiled = builder.Compile();
        HtmlSanitizerAttributeRule[] rules =
            compiled.GetSanitizerAttributeRules();

        Assert.Multiple(() =>
        {
            Assert.That(rules.Count(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Drop),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(rules.Count(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Normalize),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(rules.Count(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Prefix),
                Is.GreaterThanOrEqualTo(3));
            Assert.That(rules.Any(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Rename), Is.True);
            Assert.That(rules.Any(value => value.GetAction()
                == HtmlSanitizerAttributeAction.Validate), Is.True);
            Assert.That(compiled.GetUrlPolicy(SvgNamespace, "use",
                XlinkNamespace, "href").IsSchemeAllowed("mailto"),
                Is.True);
            Assert.That(compiled.GetGlobalUrlPolicy().IsSchemeAllowed(
                "https"), Is.True);
        });
    }

    [Test]
    public void CompiledPolicyExposesEveryAdvancedSnapshotAndFlag()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowElementInNamespace(SvgNamespace, "use");
        builder.AllowAttributeOnElement("href", "a");
        builder.BlockElement("script", HtmlBlockedElementAction.DropSubtree);
        builder.BlockAttributeOnElement("onclick", "a");
        builder.ForceAttribute("a", "target", "_blank");
        builder.ForceAttributeInNamespace(SvgNamespace, "use",
            XlinkNamespace, "role", "xlink", "image");
        builder.RequireAttributeInNamespace(SvgNamespace, "use",
            XlinkNamespace, "title", "xlink", "fallback");
        builder.BlockCssFunction("rgb");
        builder.BlockCssKeyword("block");
        builder.BlockCssUnit("px");
        builder.SetAllowCustomizedBuiltInElements(true);
        builder.SetAllowDoctype(true);
        builder.SetAllowTemplateElements(true);
        HtmlSanitizerUrlPolicy urlPolicy = new HtmlSanitizerUrlPolicy(
            new[] { "https" }, new[] { "example.test" },
            new[] { 8443 }, true, true, true, true,
            "https://example.test/root/", true);
        builder.SetUrlPolicy(urlPolicy);
        builder.AllowDataUrlMediaType("image/png");
        builder.SetMaximumDataUrlBytes(128);

        HtmlCompiledSanitizerPolicy compiled = builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetAllowCustomizedBuiltInElements(),
                Is.True);
            Assert.That(compiled.GetAllowDoctype(), Is.True);
            Assert.That(compiled.GetAllowTemplateElements(), Is.True);
            Assert.That(compiled.GetAllowUrlCredentials(), Is.True);
            Assert.That(compiled.GetAllowRelativeUrls(), Is.True);
            Assert.That(compiled.GetAllowProtocolRelativeUrls(), Is.True);
            Assert.That(compiled.GetAllowFragmentUrls(), Is.True);
            Assert.That(compiled.GetRewriteRelativeUrls(), Is.True);
            Assert.That(compiled.GetUrlBase(),
                Is.EqualTo("https://example.test/root/"));
            Assert.That(compiled.GetAllowedUrlPorts(),
                Is.EqualTo(new[] { 8443 }));
            Assert.That(compiled.IsUrlPortAllowed(8443), Is.True);
            Assert.That(compiled.GetBlockedCssFunctions(),
                Is.EqualTo(new[] { "rgb" }));
            Assert.That(compiled.GetBlockedCssKeywords(),
                Is.EqualTo(new[] { "block" }));
            Assert.That(compiled.GetBlockedCssUnits(),
                Is.EqualTo(new[] { "px" }));
            Assert.That(compiled.GetBlockedElementRules(),
                Has.Length.EqualTo(1));
            Assert.That(compiled.GetElementAttributeRules(),
                Is.Not.Empty);
            Assert.That(compiled.GetForcedAttributeRules(),
                Has.Length.EqualTo(1));
            Assert.That(compiled.HasForcedAttributeInNamespaces(
                SvgNamespace, "use", XlinkNamespace, "role"), Is.True);
            Assert.That(compiled.GetForcedAttributeValueInNamespaces(
                SvgNamespace, "use", XlinkNamespace, "role"),
                Is.EqualTo("image"));
            Assert.That(compiled.HasRequiredAttributeInNamespaces(
                SvgNamespace, "use", XlinkNamespace, "title"), Is.True);
            Assert.That(compiled.GetRequiredAttributeValueInNamespaces(
                SvgNamespace, "use", XlinkNamespace, "title"),
                Is.EqualTo("fallback"));
        });
    }

    [Test]
    public void UrlPolicyExposesEveryAllowlistAndReferenceDecision()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.SetUrlPolicy(new HtmlSanitizerUrlPolicy(
            new[] { "HTTPS" }, new[] { "Example.TEST" },
            new[] { 8443 }, true, true, true, true,
            "https://example.test/root/", true));
        builder.AllowDataUrlMediaType("IMAGE/PNG");
        builder.SetMaximumDataUrlBytes(64);
        HtmlSanitizerUrlPolicy policy = builder.Compile()
            .GetGlobalUrlPolicy();

        string[] mediaTypes = policy.GetAllowedDataMediaTypes();
        mediaTypes[0] = "changed/type";

        Assert.Multiple(() =>
        {
            Assert.That(policy.GetAllowedSchemes(),
                Is.EqualTo(new[] { "https" }));
            Assert.That(policy.GetAllowedHosts(),
                Is.EqualTo(new[] { "example.test" }));
            Assert.That(policy.GetAllowedPorts(),
                Is.EqualTo(new[] { 8443 }));
            Assert.That(policy.GetAllowCredentials(), Is.True);
            Assert.That(policy.GetAllowRelativeUrls(), Is.True);
            Assert.That(policy.GetAllowProtocolRelativeUrls(), Is.True);
            Assert.That(policy.GetAllowFragmentUrls(), Is.True);
            Assert.That(policy.GetBaseUrl(),
                Is.EqualTo("https://example.test/root/"));
            Assert.That(policy.GetRewriteRelativeUrls(), Is.True);
            Assert.That(policy.GetAllowedDataMediaTypes(),
                Is.EqualTo(new[] { "image/png" }));
            Assert.That(policy.GetMaximumDataUrlBytes(), Is.EqualTo(64));
            Assert.That(policy.IsDataMediaTypeAllowed("IMAGE/PNG"),
                Is.True);
            Assert.That(policy.IsSchemeAllowed("HTTPS"), Is.True);
            Assert.That(policy.IsHostAllowed("EXAMPLE.TEST"), Is.True);
            Assert.That(policy.IsPortAllowed(8443), Is.True);
            Assert.That(policy.IsPortAllowed(443), Is.False);
        });
    }

    static HtmlCompiledSanitizerPolicy CreateContentPolicy()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("html");
        builder.AllowElement("head");
        builder.AllowElement("body");
        builder.AllowElement("p");
        return builder.Compile();
    }

    static HtmlNode ParseSubtree()
    {
        return Html.ParseFragment(
            "<p onclick=secret>safe<script>bad</script></p>", "body")
            .GetFragment().GetFirstChild();
    }
}
