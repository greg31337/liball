using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class CssEscaperTests
{
    [Test]
    public void EscapesIdentifiersUsingCssIdentifierRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeIdentifier("button-primary"), Is.EqualTo("button-primary"));
            Assert.That(CssEscaper.EscapeIdentifier("123 item"), Is.EqualTo("\\31 23\\20 item"));
            Assert.That(CssEscaper.EscapeIdentifier("-1item"), Is.EqualTo("-\\31 item"));
            Assert.That(CssEscaper.EscapeIdentifier("a.b#c"), Is.EqualTo("a\\.b\\#c"));
            Assert.That(CssEscaper.EscapeIdentifier("-"), Is.EqualTo("\\-"));
            Assert.That(CssEscaper.EscapeIdentifier("a\0b\ud800c"), Is.EqualTo("a\uFFFDb\uFFFDc"));
            Assert.That(CssEscaper.EscapeIdentifier("icon-😀"), Is.EqualTo("icon-😀"));
        });
    }

    [Test]
    public void EscapesStringsForCssAndEmbeddedStyleMarkup()
    {
        string escaped = CssEscaper.EscapeString("quote\" slash\\ line\n</style>");

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("\"quote\\\" slash\\\\ line\\a \\3c /style\\3e \""));
            Assert.That(escaped, Does.Not.Contain("</style>"));
            Assert.That(CssEscaper.EscapeString("a\0b\udc00"), Is.EqualTo("\"a\uFFFDb\uFFFD\""));
            Assert.That(CssEscaper.EscapeString(string.Empty), Is.EqualTo("\"\""));
        });
    }

    [Test]
    public void WrapsAllowedUrlsInEscapedCssStringTokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeUrl("/images/card.svg#icon"), Is.EqualTo("url(\"/images/card.svg#icon\")"));
            Assert.That(CssEscaper.EscapeUrl("HTTPS://example.test/a\"b.png"), Is.EqualTo("url(\"HTTPS://example.test/a\\\"b.png\")"));
            Assert.That(CssEscaper.EscapeUrl("blob:https://example.test/id"), Is.EqualTo("url(\"blob:https://example.test/id\")"));
            Assert.That(CssEscaper.EscapeUrl("about:blank"), Is.EqualTo("url(\"about:blank\")"));
            Assert.That(CssEscaper.EscapeUrl("/images/😀.png"), Is.EqualTo("url(\"/images/😀.png\")"));
        });
    }

    [TestCase("")]
    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/css,body{}")]
    [TestCase("file:///secret")]
    [TestCase("/images/a b.png")]
    [TestCase("/images/a\\b.png")]
    public void RejectsEmptyUnsafeOrMalformedUrls(string value)
    {
        Assert.Throws<ArgumentException>(() => CssEscaper.EscapeUrl(value));
    }

    [Test]
    public void PreservesSafeSelectorsAndRejectsRuleBoundaryInjection()
    {
        string selector = "  .card[data-state=\"open\"] > button:hover, #fallback  ";

        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeSelector(selector), Is.EqualTo(".card[data-state=\"open\"] > button:hover, #fallback"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(""));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector("> .card"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card,"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card[aria-current"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card{}body"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card;color:red"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card/* hidden */ body"));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(".card</style>"));
        });
    }

    [Test]
    public void AppliesUntrustedPropertyAndValuePolicy()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.IsAllowedUntrustedProperty(CssProperties.Color), Is.True);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("padding-inline-start"), Is.True);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty(CssProperties.Background), Is.False);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty(CssProperties.Cursor), Is.False);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("--host-token"), Is.False);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("Color"), Is.False);
            Assert.That(CssEscaper.EscapeDeclarationValue(CssProperties.Color, " rgb(1 2 3 / 50%) ", false), Is.EqualTo("rgb(1 2 3 / 50%)"));
            Assert.That(CssEscaper.EscapeDeclarationValue(CssProperties.FontFamily, "system-ui, sans-serif", false), Is.EqualTo("system-ui, sans-serif"));
        });
    }

    [TestCase("url(https://example.test/image.png)")]
    [TestCase("expression(alert(1))")]
    [TestCase("red !important")]
    [TestCase("red;position:fixed")]
    [TestCase("calc(1px /* comment */ + 2px)")]
    [TestCase("var(\\2d x)")]
    [TestCase("rgb(1 2 3")]
    public void RejectsUnsafeUntrustedDeclarationValues(string value)
    {
        Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue(CssProperties.Color, value, false));
    }

    [Test]
    public void TrustedDeclarationsAllowExtendedPropertiesWithoutAllowingBreakout()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeDeclarationValue(CssProperties.Background, "linear-gradient(red, blue)", true), Is.EqualTo("linear-gradient(red, blue)"));
            Assert.That(CssEscaper.EscapeDeclarationValue("--host-accent", "color-mix(in srgb, red 20%, blue)", true), Is.EqualTo("color-mix(in srgb, red 20%, blue)"));
            Assert.That(CssEscaper.EscapeDeclarationValue("-webkit-user-select", "none", true), Is.EqualTo("none"));
            Assert.That(CssEscaper.EscapeDeclarationValue("content", "\"</style>\"", true), Is.EqualTo("\"\\3c /style\\3e \""));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue(CssProperties.Background, "red;}body{color:black", true));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue("--token", "calc(1px", true));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue("Bad-Property", "value", true));
        });
    }

    [Test]
    public void RejectsNullInputsAtEachEscapingBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeIdentifier(null));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeIdentifier(string.Empty));
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeString(null));
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeUrl(null));
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeSelector(null));
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeDeclarationValue(null, "red", false));
            Assert.Throws<ArgumentNullException>(() => CssEscaper.EscapeDeclarationValue(CssProperties.Color, null, false));
        });
    }
}
