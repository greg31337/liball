
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerStyleTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";

    [Test]
    public void AttributeIntegrationKeepsOnlyAllowedSafeDeclarations()
    {
        HtmlNode element = CreateElement("div");
        element.SetAttribute("style",
            @" COLOR : r\65 d ;position:fixed;width:calc(100% - 2px);background-image:url(javascript:alert(1));font-weight:700!important;unknown:");
        HtmlSanitizerPolicy builder = CreateStylePolicy("color", "width",
            "background-image", "font-weight");

        HtmlSanitizerAttributeApplyResult applied =
            HtmlSanitizerAttributes.ApplyWithResult(element,
                builder.Compile());
        HtmlSanitizerStyleChange[] changes = applied.GetStyleChanges();

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("style"), Is.EqualTo(
                "color:red;width:calc(100% - 2px);font-weight:700!important"));
            Assert.That(changes.Select(value => value.GetPropertyName()),
                Is.EqualTo(new[]
                {
                    "color", "position", "background-image", "unknown"
                }));
            Assert.That(changes.Select(value => value.GetAction()),
                Is.EqualTo(new[]
                {
                    HtmlSanitizerStyleDeclarationAction.Replaced,
                    HtmlSanitizerStyleDeclarationAction.Removed,
                    HtmlSanitizerStyleDeclarationAction.Removed,
                    HtmlSanitizerStyleDeclarationAction.Removed
                }));
            Assert.That(changes.All(value => value.GetValueSpan().IsKnown()),
                Is.True);
        });
    }

    [Test]
    public void StyleAndPropertiesAreIndependentOptIns()
    {
        HtmlNode attributeDisabled = CreateElement("div");
        attributeDisabled.SetAttribute("style", "color:red");
        HtmlSanitizerPolicy propertyOnly = new HtmlSanitizerPolicy();
        propertyOnly.AllowElement("div");
        propertyOnly.AllowCssProperty("color");

        HtmlNode propertiesDisabled = CreateElement("div");
        propertiesDisabled.SetAttribute("style", "color:red;width:2px");
        HtmlSanitizerPolicy attributeOnly = new HtmlSanitizerPolicy();
        attributeOnly.AllowElement("div");
        attributeOnly.AllowAttribute("style");

        HtmlSanitizerAttributeApplyResult first =
            HtmlSanitizerAttributes.ApplyWithResult(attributeDisabled,
                propertyOnly.Compile());
        HtmlSanitizerAttributeApplyResult second =
            HtmlSanitizerAttributes.ApplyWithResult(propertiesDisabled,
                attributeOnly.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(attributeDisabled.HasAttribute("style"), Is.False);
            Assert.That(first.GetStyleChanges(), Is.Empty);
            Assert.That(propertiesDisabled.HasAttribute("style"), Is.False);
            Assert.That(second.GetStyleChanges(), Has.Length.EqualTo(2));
            Assert.That(second.GetStyleChanges().All(value =>
                value.GetAction()
                    == HtmlSanitizerStyleDeclarationAction.Removed), Is.True);
        });
    }

    [Test]
    public void CssUrlsUseTheCompiledGlobalUrlPolicy()
    {
        HtmlNode element = CreateElement("div");
        element.SetAttribute("style",
            "background-image:url(images/a.png);color:red");
        HtmlSanitizerPolicy builder = CreateStylePolicy("background-image",
            "color");
        builder.AllowUrlScheme("https");
        builder.SetUrlBase("https://example.test/base/");
        builder.SetRewriteRelativeUrls(true);

        HtmlSanitizerAttributeApplyResult result =
            HtmlSanitizerAttributes.ApplyWithResult(element,
                builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttribute("style"), Is.EqualTo(
                "background-image:url(\"https://example.test/base/images/a.png\");color:red"));
            Assert.That(result.GetStyleChanges(), Has.Length.EqualTo(1));
            Assert.That(result.GetStyleChanges()[0].GetPropertyName(),
                Is.EqualTo("background-image"));
            Assert.That(result.GetStyleChanges()[0].GetAction(),
                Is.EqualTo(HtmlSanitizerStyleDeclarationAction.Replaced));
        });
    }

    [Test]
    public void RecoveryPreservesValidDeclarationsAndReportsParsedRemovals()
    {
        HtmlSanitizerPolicy builder = CreateStylePolicy("color", "width");

        HtmlSanitizerStyleResult result = HtmlSanitizerStyles.Sanitize(
            "broken;color:red;bad:url(javascript:x);width:2px",
            builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetParseErrorCount(), Is.EqualTo(1));
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetValue(), Is.EqualTo("color:red;width:2px"));
            Assert.That(result.GetChanges(), Has.Length.EqualTo(1));
            Assert.That(result.GetChanges()[0].GetPropertyName(),
                Is.EqualTo("bad"));
            Assert.That(result.GetChanges()[0].GetAction(),
                Is.EqualTo(HtmlSanitizerStyleDeclarationAction.Removed));
        });
    }

    [Test]
    public void DuplicateAndImportantDeclarationsKeepCascadeOrder()
    {
        HtmlSanitizerPolicy builder = CreateStylePolicy("color");

        HtmlSanitizerStyleResult result = HtmlSanitizerStyles.Sanitize(
            "color:red;color:BLUE!important;color:expression(alert(1))",
            builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetValue(),
                Is.EqualTo("color:red;color:blue!important"));
            Assert.That(result.GetChanges().Select(value =>
                value.GetAction()), Is.EqualTo(new[]
                {
                    HtmlSanitizerStyleDeclarationAction.Replaced,
                    HtmlSanitizerStyleDeclarationAction.Removed
                }));
            Assert.That(result.GetChanges().Select(value =>
                value.GetPropertyName()), Is.EqualTo(new[]
                {
                    "color", "color"
                }));
        });
    }

    [Test]
    public void ReportsDoNotRetainRejectedValuesAndRedactOversizedNames()
    {
        string longName = new string('x', 100);
        string secret = new string('s', 10000);
        HtmlSanitizerStyleResult result = HtmlSanitizerStyles.Sanitize(
            longName + ":" + secret, new HtmlSanitizerPolicy().Compile());
        HtmlSanitizerStyleChange change = result.GetChanges()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetValue(), Is.Empty);
            Assert.That(change.GetAction(),
                Is.EqualTo(HtmlSanitizerStyleDeclarationAction.Removed));
            Assert.That(change.GetPropertyName(), Is.Empty);
            Assert.That(change.WasPropertyNameRedacted(), Is.True);
            Assert.That(change.GetValueSpan().GetCharacterLength(),
                Is.EqualTo(secret.Length));
        });
    }

    [Test]
    public void SecurityRelevantCssLimitsFailClosed()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumCssDeclarations(1);
        HtmlSanitizerPolicy builder = CreateStylePolicy("color", "width");

        HtmlSanitizerStyleResult result = HtmlSanitizerStyles.Sanitize(
            "color:red;width:2px", builder.Compile(), limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetValue(), Is.Empty);
            Assert.That(result.GetChanges(), Has.Length.EqualTo(1));
            Assert.That(result.GetChanges()[0].GetAction(),
                Is.EqualTo(HtmlSanitizerStyleDeclarationAction.Removed));
        });
    }

    [Test]
    public void FullSanitizerPassesOperationCssLimitsToStyleAttributes()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<div style='color:red;width:2px'>safe</div>", "body")
            .GetFragment();
        HtmlSanitizerPolicy builder = CreateStylePolicy("color", "width");
        HtmlLimits declarationLimits = HtmlLimits.CreateDefault();
        declarationLimits.SetMaximumCssDeclarations(1);
        HtmlLimits tokenLimits = HtmlLimits.CreateDefault();
        tokenLimits.SetMaximumCssTokens(3);

        HtmlSanitizeResult declarations = HtmlSanitizer.Sanitize(fragment,
            builder.Compile(), declarationLimits, CancellationToken.None);
        HtmlSanitizeResult tokens = HtmlSanitizer.Sanitize(fragment,
            builder.Compile(), tokenLimits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(declarations.IsSuccess(), Is.True);
            Assert.That(declarations.GetHtml(),
                Is.EqualTo("<div>safe</div>"));
            Assert.That(tokens.IsSuccess(), Is.True);
            Assert.That(tokens.GetHtml(), Is.EqualTo("<div>safe</div>"));
        });
    }

    [Test]
    public void NestedCssUrlAndSrcdocStagesHonorCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        HtmlSanitizerPolicy styleBuilder = CreateStylePolicy("color");
        HtmlSanitizerPolicy urlBuilder = new HtmlSanitizerPolicy();
        urlBuilder.AllowElement("a");
        urlBuilder.AllowAttributeOnElement("href", "a");
        urlBuilder.AllowUrlScheme("https");
        HtmlNode link = CreateElement("a");
        link.SetAttribute("href", "https://example.test/path");
        HtmlAttribute href = link.GetAttributes()[0];
        HtmlCompiledSanitizerPolicy srcdocPolicy =
            new HtmlSanitizerPolicy().Compile();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                HtmlSanitizerStyles.Sanitize("color:red",
                    styleBuilder.Compile(), HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlSanitizerUrlAttributes.Evaluate(link, href,
                    urlBuilder.Compile(), HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlSanitizerNestedContent.SanitizeSrcdoc("<p>safe</p>",
                    srcdocPolicy, 0, HtmlLimits.CreateDefault(),
                    source.Token));
        });
    }

    [Test]
    public void InlineStyleOptInDoesNotEnableStyleElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode element = document.CreateElement("div");
        element.SetAttribute("style", "color:red;position:fixed");
        HtmlNode style = document.CreateElement("style");
        style.Append(document.CreateText("body{color:red}"));
        fragment.Append(element);
        fragment.Append(style);
        HtmlSanitizerPolicy builder = CreateStylePolicy("color");

        HtmlSanitizerNestedContent.Apply(fragment, builder.Compile());

        Assert.Multiple(() =>
        {
            Assert.That(fragment.GetChildren(), Has.Length.EqualTo(1));
            Assert.That(fragment.GetFirstChild(), Is.SameAs(element));
            Assert.That(element.GetAttribute("style"), Is.EqualTo("color:red"));
            Assert.That(style.GetParent(), Is.Null);
        });
    }

    [Test]
    public void PolicyRejectsCssPropertiesOutsideTheConservativeTable()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.AllowCssProperty("position"),
                Throws.ArgumentException);
            Assert.That(() => builder.AllowCssProperty("behavior"),
                Throws.ArgumentException);
            Assert.That(() => builder.AllowCssProperty("made-up"),
                Throws.ArgumentException);
            Assert.That(() => builder.AllowCssProperty("COLOR"),
                Throws.Nothing);
            Assert.That(builder.Compile().GetAllowedCssProperties(),
                Is.EqualTo(new[] { "color" }));
        });
    }

    [Test]
    public void ResultArraysAreDefensiveAndArgumentsAreValidated()
    {
        HtmlSanitizerPolicy builder = CreateStylePolicy("color");
        HtmlSanitizerStyleResult style = HtmlSanitizerStyles.Sanitize(
            "color:BLUE", builder.Compile());
        HtmlSanitizerStyleChange[] changes = style.GetChanges();
        changes[0] = null;
        HtmlNode element = CreateElement("div");
        element.SetAttribute("style", "color:BLUE");
        HtmlSanitizerAttributeApplyResult applied =
            HtmlSanitizerAttributes.ApplyWithResult(element,
                builder.Compile());
        changes = applied.GetStyleChanges();
        changes[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(style.GetChanges()[0].GetPropertyName(),
                Is.EqualTo("color"));
            Assert.That(applied.GetStyleChanges()[0].GetPropertyName(),
                Is.EqualTo("color"));
            Assert.That(() => HtmlSanitizerStyles.Sanitize(null,
                builder.Compile()), Throws.ArgumentNullException);
            Assert.That(() => HtmlSanitizerStyles.Sanitize("color:red",
                null), Throws.ArgumentNullException);
            Assert.That(() => HtmlSanitizerAttributes.ApplyWithResult(null,
                builder.Compile()), Throws.ArgumentNullException);
        });
    }

    static HtmlSanitizerPolicy CreateStylePolicy(string first)
    {
        return CreateStylePolicy(new[] { first });
    }

    static HtmlSanitizerPolicy CreateStylePolicy(string first,
        string second)
    {
        return CreateStylePolicy(new[] { first, second });
    }

    static HtmlSanitizerPolicy CreateStylePolicy(string first,
        string second, string third, string fourth)
    {
        return CreateStylePolicy(new[] { first, second, third, fourth });
    }

    static HtmlSanitizerPolicy CreateStylePolicy(string[] properties)
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("div");
        builder.AllowAttribute("style");
        foreach (string property in properties)
        {
            builder.AllowCssProperty(property);
        }

        return builder;
    }

    static HtmlNode CreateElement(string localName)
    {
        HtmlDocument document = CreateDocument();
        return document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceUri, localName, localName, "",
            Array.Empty<HtmlAttribute>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
