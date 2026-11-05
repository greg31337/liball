using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicMicrosyntaxLayerTests
{
    [Test]
    public void TokenListOperationsExposeBoundedParsingAndSerialization()
    {
        HtmlTokenListsApi api = new HtmlTokenListsApi();
        HtmlLimits tokenLimits = HtmlLimits.CreateDefault();
        tokenLimits.SetMaximumTokens(2);
        HtmlTokenListResult result = api.ParseSpaceSeparatedWithLimits(
            "one two three", tokenLimits, CancellationToken.None);
        HtmlDiagnostic original = result.GetDiagnostics()[0];
        HtmlDiagnostic[] snapshot = result.GetDiagnostics();
        snapshot[0] = null;
        HtmlLimits exact = HtmlLimits.CreateDefault();
        exact.SetMaximumOutputCharacters(7);
        HtmlLimits shortOutput = HtmlLimits.CreateDefault();
        shortOutput.SetMaximumOutputCharacters(6);
        CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasLimitExceeded(), Is.True);
            Assert.That(result.GetTokens(), Is.EqualTo(
                new[] { "one", "two" }));
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MicrosyntaxLimitExceeded));
            Assert.That(result.GetDiagnostics()[0], Is.SameAs(original));
            Assert.That(original.GetSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(8));
            Assert.That(api.SerializeSpaceSeparatedWithLimits(
                new[] { "one", "two" }, exact, CancellationToken.None),
                Is.EqualTo("one two"));
            Assert.Throws<HtmlSerializationLimitException>(() =>
                api.SerializeSpaceSeparatedWithLimits(
                    new[] { "one", "two" }, shortOutput,
                    CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() =>
                api.ParseCommaSeparatedWithLimits("one,two",
                    HtmlLimits.CreateDefault(), cancellation.Token));
        });
    }

    [Test]
    public void SrcsetReportsDetailedErrorsAndSupportsBoundedConstruction()
    {
        HtmlSrcsetApi api = new HtmlSrcsetApi();
        string source = "good.png 1x,\nzero.png 0w, conflict.png 1x 2x";
        HtmlSrcsetResult result = api.Parse(source);
        HtmlLimits diagnosticLimits = HtmlLimits.CreateDefault();
        diagnosticLimits.SetMaximumDiagnostics(1);
        HtmlSrcsetResult truncated = api.ParseWithLimits(source,
            diagnosticLimits, CancellationToken.None);
        HtmlSrcsetCandidate plain = api.CreateCandidate("a.png");
        HtmlSrcsetCandidate density = api.CreateDensityCandidate(
            "b.png", 2);
        HtmlSrcsetCandidate[] candidates = new[] { plain, density };
        string serialized = api.Serialize(candidates);
        HtmlLimits exact = HtmlLimits.CreateDefault();
        exact.SetMaximumOutputCharacters(serialized.Length);
        HtmlLimits shortOutput = HtmlLimits.CreateDefault();
        shortOutput.SetMaximumOutputCharacters(serialized.Length - 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(result.IsPartial(), Is.True);
            Assert.That(result.GetErrorCount(), Is.EqualTo(2));
            Assert.That(result.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MicrosyntaxInvalid));
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(2));
            Assert.That(result.GetDiagnostics()[0].GetMessage(),
                Does.Contain("width descriptor"));
            Assert.That(result.GetDiagnostics()[0].GetSpan().GetStart()
                .GetLine(), Is.EqualTo(2));
            Assert.That(result.GetDiagnostics()[1].GetMessage(),
                Does.Contain("conflicting descriptors"));
            Assert.That(truncated.GetDiagnosticCount(), Is.EqualTo(2));
            Assert.That(truncated.GetRetainedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(truncated.GetDroppedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(truncated.WereDiagnosticsTruncated(), Is.True);
            Assert.That(api.SerializeWithLimits(candidates, exact,
                CancellationToken.None), Is.EqualTo(serialized));
            Assert.Throws<HtmlSerializationLimitException>(() =>
                api.SerializeWithLimits(candidates, shortOutput,
                    CancellationToken.None));
        });
    }

    [Test]
    public void RefreshFailuresHaveDiagnosticsAndSerializationIsBounded()
    {
        HtmlRefreshApi api = new HtmlRefreshApi();
        HtmlRefreshResult invalid = api.Parse("5x; url=next");
        HtmlRefreshResult valid = api.Parse("7; URL = next.html");
        HtmlLimits exact = HtmlLimits.CreateDefault();
        exact.SetMaximumOutputCharacters(16);
        HtmlLimits shortOutput = HtmlLimits.CreateDefault();
        shortOutput.SetMaximumOutputCharacters(15);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.IsSuccess(), Is.False);
            Assert.That(invalid.IsComplete(), Is.True);
            Assert.That(invalid.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MicrosyntaxInvalid));
            Assert.That(invalid.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(invalid.GetDiagnostics()[0].GetSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(1));
            Assert.That(valid.IsSuccess(), Is.True);
            Assert.That(valid.GetDiagnostics(), Is.Empty);
            Assert.That(api.SerializeWithLimits(7, "next.html", exact,
                CancellationToken.None), Is.EqualTo("7; url=next.html"));
            Assert.Throws<HtmlSerializationLimitException>(() =>
                api.SerializeWithLimits(7, "next.html", shortOutput,
                    CancellationToken.None));
        });
    }

    [Test]
    public void LanguageTagsNormalizeCaseAndReportMalformedSubtags()
    {
        HtmlLanguageTagsApi api = new HtmlLanguageTagsApi();
        HtmlLanguageTagResult valid = api.Parse(
            " ZH-hant-cn-x-PRIVATE ");
        HtmlLanguageTagResult invalid = api.Parse("en--US");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumAttributeValueCharacters(4);
        HtmlLanguageTagResult limited = api.ParseWithLimits("en-US",
            limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsSuccess(), Is.True);
            Assert.That(valid.GetTag(), Is.EqualTo(
                "zh-Hant-CN-x-private"));
            Assert.That(valid.GetPrimaryLanguage(), Is.EqualTo("zh"));
            Assert.That(invalid.IsSuccess(), Is.False);
            Assert.That(invalid.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MicrosyntaxInvalid));
            Assert.That(invalid.GetDiagnostics()[0].GetSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(3));
            Assert.That(limited.WasLimitExceeded(), Is.True);
            Assert.That(limited.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MicrosyntaxLimitExceeded));
        });
    }

    [Test]
    public void NumericTableAndListOperationsExposeRecoveredDetails()
    {
        HtmlNumbersApi api = new HtmlNumbersApi();
        HtmlIntegerResult integer = api.ParseInteger(" +12junk");
        HtmlDimensionResult dimension = api.ParseDimension(" 10.25%tail");
        HtmlDocument document = Html.Parse(
            "<table><tr><td id=cell align=' CENTER '></td></tr></table>"
            + "<ol id=list reversed><li></li><li></li></ol>");
        HtmlNode cell = document.FindById("cell");
        HtmlNode list = document.FindById("list");

        Assert.Multiple(() =>
        {
            Assert.That(integer.GetValue(), Is.EqualTo(12));
            Assert.That(integer.GetConsumedLength(), Is.EqualTo(4));
            Assert.That(dimension.GetValue(), Is.EqualTo(10.25));
            Assert.That(dimension.GetConsumedLength(), Is.EqualTo(7));
            Assert.That(api.GetTableAlignment(cell), Is.EqualTo(
                HtmlTableAlignment.Center));
            Assert.That(api.ParseTableAlignment("char"), Is.EqualTo(
                HtmlTableAlignment.Character));
            Assert.That(api.ParseColumnSpan("5000"), Is.EqualTo(1000));
            Assert.That(api.ParseRowSpan("0"), Is.Zero);
            Assert.That(api.GetOrderedListOrdinals(list),
                Is.EqualTo(new[] { 2, 1 }));
        });
    }

    [Test]
    public void UrlApisResolveBasesInspectSchemesAndClassifyNames()
    {
        HtmlUrlsApi urls = new HtmlUrlsApi();
        HtmlDomUrlsApi domUrls = new HtmlDomUrlsApi();
        HtmlUrlSchemeResult scheme = urls.ExtractScheme(
            "java&#x73;cript:alert(1)");
        HtmlUrlResult resolved = urls.Resolve("../asset.png",
            "https://example.test/root/page/");
        HtmlDocument document = Html.Parse(
            "<html manifest=site.appcache><body>"
            + "<object id=object archive='one.jar two.jar'></object>"
            + "<img id=image usemap=#map></body></html>");

        Assert.Multiple(() =>
        {
            Assert.That(scheme.GetScheme(), Is.EqualTo("javascript"));
            Assert.That(scheme.WasDeobfuscated(), Is.True);
            Assert.That(resolved.GetUrl(), Is.EqualTo(
                "https://example.test/root/asset.png"));
            Assert.That(domUrls.GetNamedAttributeKind(
                document.GetDocumentElement(), "manifest"), Is.EqualTo(
                HtmlUrlAttributeKind.Single));
            Assert.That(domUrls.GetNamedAttributeKind(
                document.FindById("object"), "archive"), Is.EqualTo(
                HtmlUrlAttributeKind.SpaceSeparated));
            Assert.That(domUrls.GetNamedAttributeKind(
                document.FindById("image"), "usemap"), Is.EqualTo(
                HtmlUrlAttributeKind.None));
        });
    }
}
