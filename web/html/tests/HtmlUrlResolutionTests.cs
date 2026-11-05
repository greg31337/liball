
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlUrlResolutionTests
{
    [TestCase("HTTP://Example.COM:80/a/../b", "http://example.com/b")]
    [TestCase("https://EXAMPLE.test", "https://example.test/")]
    [TestCase("https://example.test/a/./b/../c",
        "https://example.test/a/c")]
    [TestCase("MAILTO:User@Example.test", "mailto:User@example.test")]
    [TestCase("https&colon;//EXAMPLE.test/path",
        "https://example.test/path")]
    public void NormalizesValidatedAbsoluteUrls(string value, string expected)
    {
        HtmlUrlResult result = HtmlUrls.Normalize(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True,
                result.GetDiagnostics().FirstOrDefault()?.GetMessage()
                    ?? value);
            Assert.That(result.GetUrl(), Is.EqualTo(expected), value);
            Assert.That(result.WasRelative(), Is.False);
            Assert.That(result.WasLimitExceeded(), Is.False);
            Assert.That(result.GetDiagnostics(), Is.Empty);
        });
    }

    [TestCase("../image.png?size=2#preview",
        "https://example.test/a/b/page.html?old=1#old",
        "https://example.test/a/image.png?size=2#preview")]
    [TestCase("/root/file", "https://example.test/a/b/",
        "https://example.test/root/file")]
    [TestCase("//cdn.example.test/lib.js", "https://example.test/a/",
        "https://cdn.example.test/lib.js")]
    [TestCase("?new=1", "https://example.test/a/page?old=1#old",
        "https://example.test/a/page?new=1")]
    [TestCase("#new", "https://example.test/a/page?old=1#old",
        "https://example.test/a/page?old=1#new")]
    [TestCase("", "https://example.test/a/page?old=1#old",
        "https://example.test/a/page?old=1#old")]
    public void ResolvesRelativeReferencesAgainstValidatedBases(string value,
        string baseUrl, string expected)
    {
        HtmlUrlResult result = HtmlUrls.Resolve(value, baseUrl);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True,
                result.GetDiagnostics().FirstOrDefault()?.GetMessage()
                    ?? value);
            Assert.That(result.GetUrl(), Is.EqualTo(expected), value);
            Assert.That(result.GetScheme(), Is.EqualTo("https"));
            Assert.That(result.WasRelative(), Is.True);
            Assert.That(result.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void AbsoluteInputDoesNotDependOnAnUnusedBaseUrl()
    {
        HtmlUrlResult result = HtmlUrls.Resolve(
            "https://EXAMPLE.test/a/../b", "not a base");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetUrl(),
                Is.EqualTo("https://example.test/b"));
            Assert.That(result.WasRelative(), Is.False);
        });
    }

    [Test]
    public void DeobfuscatesInputAndBaseBeforeValidatedResolution()
    {
        HtmlUrlResult result = HtmlUrls.Resolve(
            " ../im&#x61;ges/pic ture.png ",
            " https&colon;//EXAMPLE.test/a/b/ ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetUrl(), Is.EqualTo(
                "https://example.test/a/images/picture.png"));
            Assert.That(result.WasDeobfuscated(), Is.True);
        });
    }

    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("//example.test/path")]
    [TestCase("1invalid:value")]
    [TestCase("https://[::1")]
    [TestCase("https://example.test/%zz")]
    [TestCase("https://example.test:bad/")]
    public void NormalizeRejectsRelativeAndMalformedUrls(string value)
    {
        HtmlUrlResult result = HtmlUrls.Normalize(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False, value);
            Assert.That(result.GetUrl(), Is.Empty);
            Assert.That(result.GetScheme(), Is.Empty);
            Assert.That(result.WasLimitExceeded(), Is.False);
            Assert.That(result.GetDiagnostics().Single().GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.UrlInvalid));
        });
    }

    [TestCase("child", "")]
    [TestCase("child", "relative/base")]
    [TestCase("child", "https://[::1")]
    [TestCase("child", "mailto:user@example.test")]
    [TestCase("1invalid:value", "https://example.test/")]
    [TestCase("//host:bad/path", "https://example.test/")]
    [TestCase("//[::1/path", "https://example.test/")]
    [TestCase("child#%zz", "https://example.test/")]
    public void ResolveRejectsMissingMalformedAndNonHierarchicalBases(
        string value, string baseUrl)
    {
        HtmlUrlResult result = HtmlUrls.Resolve(value, baseUrl);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False,
                $"{baseUrl} + {value}");
            Assert.That(result.WasRelative(), Is.True);
            Assert.That(result.GetDiagnostics().Single().GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.UrlInvalid));
        });
    }

    [Test]
    public void UrlCharacterLimitCoversInputBaseAndNormalizedOutput()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(24);
        HtmlUrlResult input = HtmlUrls.Normalize(
            "https://example.test/long", limits, CancellationToken.None);
        HtmlUrlResult baseResult = HtmlUrls.Resolve("x",
            "https://example.test/base", limits, CancellationToken.None);
        HtmlUrlResult output = HtmlUrls.Resolve("123456789",
            "https://e.test/a/", limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            AssertLimitFailure(input);
            AssertLimitFailure(baseResult);
            AssertLimitFailure(output);
        });
    }

    [Test]
    public void FullOverloadsHonorCancellationAndValidateArguments()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                HtmlUrls.Normalize("https://example.test/",
                    HtmlLimits.CreateDefault(), source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlUrls.Resolve("child", "https://example.test/",
                    HtmlLimits.CreateDefault(), source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.Normalize(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.Normalize("https://example.test/", null,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.Resolve("child", null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.Resolve(null, "https://example.test/"));
        });
    }

    [Test]
    public void DiagnosticsAreReturnedAsSnapshots()
    {
        HtmlUrlResult result = HtmlUrls.Normalize("not-relative");
        HtmlDiagnostic original = result.GetDiagnostics()[0];
        HtmlDiagnostic[] snapshot = result.GetDiagnostics();
        snapshot[0] = null;

        Assert.That(result.GetDiagnostics()[0], Is.SameAs(original));
    }

    static void AssertLimitFailure(HtmlUrlResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.WasLimitExceeded(), Is.True);
        Assert.That(result.GetDiagnostics().Single().GetCode(), Is.EqualTo(
            HtmlDiagnosticCode.ResourceLimitExceeded));
    }
}
