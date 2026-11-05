
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlUrlSchemeTests
{
    [TestCase("https://example.test", "https")]
    [TestCase("HTTP://example.test", "http")]
    [TestCase("a:value", "a")]
    [TestCase("git+ssh://example.test", "git+ssh")]
    [TestCase("web.demo-1:value", "web.demo-1")]
    [TestCase("C:\\path", "c")]
    public void ExtractsAndAsciiFoldsLexicallyValidSchemes(string value,
        string expected)
    {
        HtmlUrlSchemeResult result = HtmlUrls.ExtractScheme(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasScheme(), Is.True);
            Assert.That(result.GetScheme(), Is.EqualTo(expected));
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasLimitExceeded(), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("../relative")]
    [TestCase("//example.test/path")]
    [TestCase("#fragment")]
    [TestCase("?query")]
    [TestCase(":value")]
    [TestCase("1http:value")]
    [TestCase("+custom:value")]
    [TestCase("http/path:value")]
    [TestCase("http\\:value")]
    [TestCase("java%73cript:value")]
    [TestCase("javas\u00A0cript:value")]
    [TestCase("é:value")]
    public void DoesNotInventSchemesOutsideTheAsciiGrammar(string value)
    {
        HtmlUrlSchemeResult result = HtmlUrls.ExtractScheme(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasScheme(), Is.False);
            Assert.That(result.GetScheme(), Is.Empty);
            Assert.That(result.IsComplete(), Is.True);
        });
    }

    [Test]
    public void RemovesAsciiWhitespaceAndControlsBeforeSchemeComparison()
    {
        string input = " \tJa\nVa\rSc\0Ri\u007FPT :alert(1)";

        HtmlUrlSchemeResult result = HtmlUrls.ExtractScheme(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasScheme(), Is.True);
            Assert.That(result.GetScheme(), Is.EqualTo("javascript"));
            Assert.That(result.GetDeobfuscatedValue(),
                Is.EqualTo("JaVaScRiPT:alert(1)"));
            Assert.That(result.WasDeobfuscated(), Is.True);
        });
    }

    [Test]
    public void EveryAsciiControlIsIgnoredInsideASchemeCandidate()
    {
        foreach (int scalar in Enumerable.Range(0, 33).Append(127))
        {
            char control = scalar == 32 ? ' ' : (char)scalar;
            HtmlUrlSchemeResult result = HtmlUrls.ExtractScheme(
                "java" + control + "script:value");

            Assert.That(result.GetScheme(), Is.EqualTo("javascript"),
                $"U+{scalar:X4}");
        }
    }

    [Test]
    public void DecodesAttributeCharacterReferencesExactlyOnce()
    {
        HtmlUrlSchemeResult encoded = HtmlUrls.ExtractScheme(
            "&Tab;jav&#x61;\nscript&colon;alert(1)");
        HtmlUrlSchemeResult nested = HtmlUrls.ExtractScheme(
            "java&amp;#x73;cript&colon;alert(1)");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.HasScheme(), Is.True);
            Assert.That(encoded.GetScheme(), Is.EqualTo("javascript"));
            Assert.That(encoded.GetDeobfuscatedValue(),
                Is.EqualTo("javascript:alert(1)"));
            Assert.That(encoded.WasDeobfuscated(), Is.True);
            Assert.That(nested.HasScheme(), Is.False);
            Assert.That(nested.GetDeobfuscatedValue(),
                Is.EqualTo("java&#x73;cript:alert(1)"));
        });
    }

    [Test]
    public void ReportsWhetherAValueRequiredDeobfuscation()
    {
        HtmlUrlSchemeResult unchanged = HtmlUrls.ExtractScheme(
            "MAILTO:user@example.test");
        HtmlUrlSchemeResult changed = HtmlUrls.ExtractScheme(
            "mail&#116;o:user@example.test");

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.GetScheme(), Is.EqualTo("mailto"));
            Assert.That(unchanged.WasDeobfuscated(), Is.False);
            Assert.That(unchanged.GetDeobfuscatedValue(),
                Is.EqualTo("MAILTO:user@example.test"));
            Assert.That(changed.GetScheme(), Is.EqualTo("mailto"));
            Assert.That(changed.WasDeobfuscated(), Is.True);
        });
    }

    [Test]
    public void UrlCharacterLimitFailsClosedWithoutReturningAPartialScheme()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(5);

        HtmlUrlSchemeResult exact = HtmlUrls.ExtractScheme("a:bcd", limits,
            CancellationToken.None);
        HtmlUrlSchemeResult exceeded = HtmlUrls.ExtractScheme("https:",
            limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsComplete(), Is.True);
            Assert.That(exact.GetScheme(), Is.EqualTo("a"));
            Assert.That(exceeded.IsComplete(), Is.False);
            Assert.That(exceeded.WasLimitExceeded(), Is.True);
            Assert.That(exceeded.HasScheme(), Is.False);
            Assert.That(exceeded.GetScheme(), Is.Empty);
            Assert.That(exceeded.GetDeobfuscatedValue(), Is.Empty);
        });
    }

    [Test]
    public void FullOverloadHonorsCancellationAndValidatesArguments()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                HtmlUrls.ExtractScheme("https:", HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.ExtractScheme(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlUrls.ExtractScheme("https:", null,
                    CancellationToken.None));
        });
    }

    [Test]
    public void SchemeResultsValidateTheirState()
    {
        HtmlUrlSchemeResult result = HtmlUrlSchemeResult.Create(
            "custom:value", "custom", true, false, true, false);

        Assert.That(result.GetScheme(), Is.EqualTo("custom"));
        Assert.Throws<ArgumentNullException>(() =>
            HtmlUrlSchemeResult.Create(null, "", false, false, true, false));
        Assert.Throws<ArgumentNullException>(() =>
            HtmlUrlSchemeResult.Create("", null, false, false, true, false));
        Assert.Throws<ArgumentException>(() =>
            HtmlUrlSchemeResult.Create("x", "x", false, false, true, false));
        Assert.Throws<ArgumentException>(() =>
            HtmlUrlSchemeResult.Create("", "", false, false, false, false));
        Assert.Throws<ArgumentException>(() =>
            HtmlUrlSchemeResult.Create("", "", true, false, true, false));
        Assert.Throws<ArgumentException>(() =>
            HtmlUrlSchemeResult.Create("", "", false, false, true, true));
    }
}
