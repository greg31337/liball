using System;
using System.Reflection;
using NUnit.Framework;

namespace htmlgui.Tests;

public class CssSafetyTests
{
    [Test]
    public void UsesContextSpecificEncodingForIdentifiersStringsUrlsSelectorsAndValues()
    {
        CssSerializer serializer = new CssSerializer();
        UiCssRule container = new UiCssRule
        {
            Selector = ".card[data-label=\"safe\"]",
            Declarations = new UiCssDeclaration[] { Trusted("content", "\"</style>\"") },
            Layer = "host layer.safe",
            Condition = new UiCssCondition
            {
                Kind = "container",
                ContainerName = "main panel",
                Expression = "(min-width: 20rem)"
            }
        };

        string css = serializer.SerializeRule(container);

        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeIdentifier("123 item#part"), Is.EqualTo("\\31 23\\20 item\\#part"));
            Assert.That(CssEscaper.EscapeString("\"</style>\\"), Is.EqualTo("\"\\\"\\3c /style\\3e \\\\\""));
            Assert.That(CssEscaper.EscapeUrl("https://example.test/a\"b.svg#icon"), Is.EqualTo("url(\"https://example.test/a\\\"b.svg#icon\")"));
            Assert.That(CssEscaper.EscapeSelector(" .card > [data-state='open'] "), Is.EqualTo(".card > [data-state='open']"));
            Assert.That(CssEscaper.EscapeDeclarationValue(CssProperties.Color, "</style>", false), Is.EqualTo("\\3c /style\\3e "));
            Assert.That(css, Does.Not.Contain("</style>"));
            Assert.That(css, Does.Contain("@layer host\\20 layer.safe{"));
            Assert.That(css, Does.Contain("@container main\\20 panel (min-width: 20rem){"));
        });
    }

    [Test]
    public void RejectsEscapedControlsAndMalformedUnicodeInStructuredCssContexts()
    {
        CssSerializer serializer = new CssSerializer();
        string escapedNewlineSelector = ".item[data-value=\"safe\\\nunsafe\"]";
        string escapedDeleteSelector = ".item\\\u007f";
        string escapedSurrogateSelector = ".item\\\ud800";
        string escapedNewlineValue = "\"safe\\\nunsafe\"";
        string escapedSurrogateValue = "\"safe\\\ud800unsafe\"";
        UiCssRule escapedNewlineCondition = ConditionalRule("(feature: \"safe\\\nunsafe\")");
        UiCssRule escapedSurrogateCondition = ConditionalRule("(feature: \"safe\\\ud800unsafe\")");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(escapedNewlineSelector));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(escapedDeleteSelector));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(escapedSurrogateSelector));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue("content", escapedNewlineValue, true));
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue("content", escapedSurrogateValue, true));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(escapedNewlineCondition));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(escapedSurrogateCondition));
        });
    }

    [TestCase("/assets/icon.svg")]
    [TestCase("./assets/icon.svg#symbol")]
    [TestCase("https://example.test/icon.svg")]
    [TestCase("HTTP://example.test/icon.svg")]
    [TestCase("blob:https://example.test/123")]
    [TestCase("about:blank")]
    public void AcceptsOnlySupportedWellFormedUrlForms(string url)
    {
        Assert.That(CssEscaper.EscapeUrl(url), Does.StartWith("url(\"").And.EndWith("\")"));
    }

    [TestCase("")]
    [TestCase("javascript:alert(1)")]
    [TestCase("JaVaScRiPt:alert(1)")]
    [TestCase("data:image/svg+xml,<svg onload=alert(1)>")]
    [TestCase("vbscript:msgbox(1)")]
    [TestCase("file:///etc/passwd")]
    [TestCase("https://example.test/a b")]
    [TestCase("https://example.test/a\\b")]
    [TestCase("https://example.test/a\r\nContent-Type:text/css")]
    public void RejectsDangerousSchemesAndMalformedUrls(string url)
    {
        Assert.Throws<ArgumentException>(() => CssEscaper.EscapeUrl(url));
    }

    [Test]
    public void ClassifiesEveryCanonicalCssPropertyUnderTheUntrustedPolicy()
    {
        string[] allowed = AllowedProperties();
        FieldInfo[] fields = typeof(CssProperties).GetFields(BindingFlags.Public | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in fields)
            {
                string property = (string)field.GetRawConstantValue()!;
                bool expected = Array.IndexOf(allowed, property) >= 0;
                Assert.That(CssEscaper.IsAllowedUntrustedProperty(property), Is.EqualTo(expected), property);
            }

            Assert.That(CssEscaper.IsAllowedUntrustedProperty("margin-inline-start"), Is.True);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("padding-block-end"), Is.True);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("resize"), Is.True);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("--host-value"), Is.False);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("Color"), Is.False);
            Assert.That(CssEscaper.IsAllowedUntrustedProperty("behavior"), Is.False);
        });
    }

    [Test]
    public void RejectsEveryMaliciousTokenAcrossTheUntrustedAllowlist()
    {
        string[] malicious = new string[]
        {
            "url(javascript:alert(1))",
            "URL(https://example.test/pixel)",
            "expression(alert(1))",
            "red !important",
            "red;position:fixed",
            "red}body{display:none",
            "calc(1px /*hidden*/ + 2px)",
            "var(\\2d payload)",
            "@import 'https://example.test/x.css'",
            "-moz-binding:url(https://example.test/xbl)"
        };
        string[] allowed = AllowedProperties();

        Assert.Multiple(() =>
        {
            foreach (string property in allowed)
            {
                foreach (string value in malicious)
                {
                    Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue(property, value, false), property + ":" + value);
                }
            }
        });
    }

    [Test]
    public void TrustedPolicyAllowsExtendedSyntaxButStillPreventsRuleBreakout()
    {
        string[] breakoutValues = new string[]
        {
            "red;position:fixed",
            "red}body{display:none",
            "red{color:blue",
            "calc(1px /*comment*/ + 2px)",
            "\"unterminated",
            "calc(1px",
            "value\0suffix"
        };

        Assert.Multiple(() =>
        {
            Assert.That(CssEscaper.EscapeDeclarationValue(CssProperties.Background, "linear-gradient(red, blue)", true), Is.EqualTo("linear-gradient(red, blue)"));
            Assert.That(CssEscaper.EscapeDeclarationValue("--theme-color", "color-mix(in srgb, red 20%, blue)", true), Is.EqualTo("color-mix(in srgb, red 20%, blue)"));
            Assert.That(CssEscaper.EscapeDeclarationValue("-webkit-user-select", "none", true), Is.EqualTo("none"));
            foreach (string value in breakoutValues)
            {
                Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue(CssProperties.Background, value, true), value);
            }
        });
    }

    [Test]
    public void RejectsInvalidUnitsAndUnsafeLengthExpressions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssDesignValueFormatter.FormatLength(Expression("calc(100% - var(--gap))"), false), Is.EqualTo("calc(100% - var(--gap))"));
            Assert.That(CssDesignValueFormatter.FormatLength(Expression("env(safe-area-inset-top)"), false), Is.EqualTo("env(safe-area-inset-top)"));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(new UiLength { HasValue = true, Value = 1, Unit = UiLengthUnit.Auto }, false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(new UiLength { HasValue = true, Value = 1, Unit = UiLengthUnit.Fraction }, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatLength(new UiLength { HasValue = true, Value = 1, Unit = (UiLengthUnit)99 }, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatLength(new UiLength { HasValue = true, Value = double.NaN, Unit = UiLengthUnit.Pixel }, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatLength(new UiLength { HasValue = true, Value = double.PositiveInfinity, Unit = UiLengthUnit.Pixel }, false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("url(javascript:alert(1))"), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("calc(1px);color:red"), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("calc(1px /* hidden */ + 2px)"), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("var(\\2d unsafe)"), false));
        });
    }

    [TestCase("")]
    [TestCase("#12")]
    [TestCase("#abcdz1")]
    [TestCase("hsl(0 100% 50%)")]
    [TestCase("color-mix(in srgb, red, blue)")]
    [TestCase("rgb(256 0 0)")]
    [TestCase("rgb(0 0 0 / 1.1)")]
    [TestCase("rgba(0,0,0,2)")]
    [TestCase("rgb(0,0,0 / 50%)")]
    [TestCase("red;display:none")]
    [TestCase("url(javascript:alert(1))")]
    public void RejectsUnsupportedMalformedOrOutOfRangeColors(string color)
    {
        Assert.That(() => CssDesignValueFormatter.NormalizeColor(color), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void RejectsMalformedAndBoundaryBreakingSelectorsConditionsAndLayers()
    {
        CssSerializer serializer = new CssSerializer();
        string[] selectors = new string[]
        {
            "",
            "> .item",
            ".item,",
            ".item >",
            ".item[open",
            ".item:not(.active",
            ".item{}body",
            ".item;color:red",
            ".item/*comment*/ body",
            ".item</style>",
            "\"top-level-string\"",
            ".item\\"
        };
        string[] conditions = new string[]
        {
            "",
            "(min-width: 1px",
            "min-width: 1px)",
            "(display: grid);body",
            "(display: grid){}",
            "(display: grid)@import",
            "(display: /*comment*/ grid)",
            "(display: \"unterminated)"
        };

        Assert.Multiple(() =>
        {
            foreach (string selector in selectors)
            {
                Assert.Throws<ArgumentException>(() => CssEscaper.EscapeSelector(selector), selector);
            }

            foreach (string condition in conditions)
            {
                Assert.Throws<ArgumentException>(() => serializer.SerializeRule(ConditionalRule(condition)), condition);
            }

            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(LayeredRule("")));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(LayeredRule("base..controls")));
        });
    }

    [Test]
    public void PublicCompilerRejectsUnsafeDataAtTheFinalSerializationBoundary()
    {
        IUiCssCompiler compiler = new UiCssCompiler();
        UiCssRule untrustedUrl = Rule(".card", Declaration(CssProperties.Background, "url(javascript:alert(1))"));
        UiCssRule selectorBreakout = Rule(".card{}body", Declaration(CssProperties.Color, "red"));
        UiCssRule customPropertyBreakout = Rule(":root", Trusted("--host-token", "red;}body{display:none"));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => compiler.SerializeStylesheet(new UiCssRule[] { untrustedUrl }));
            Assert.Throws<ArgumentException>(() => compiler.SerializeStylesheet(new UiCssRule[] { selectorBreakout }));
            Assert.Throws<ArgumentException>(() => compiler.SerializeStylesheet(new UiCssRule[] { customPropertyBreakout }));
        });
    }

    private static string[] AllowedProperties()
    {
        return new string[]
        {
            CssProperties.Direction,
            CssProperties.Display,
            CssProperties.Visibility,
            CssProperties.Position,
            CssProperties.Inset,
            CssProperties.Top,
            CssProperties.Right,
            CssProperties.Bottom,
            CssProperties.Left,
            CssProperties.ZIndex,
            CssProperties.Width,
            CssProperties.Height,
            CssProperties.MinimumWidth,
            CssProperties.MinimumHeight,
            CssProperties.MaximumWidth,
            CssProperties.MaximumHeight,
            CssProperties.AspectRatio,
            CssProperties.BoxSizing,
            CssProperties.Margin,
            CssProperties.Padding,
            CssProperties.Border,
            CssProperties.BorderRadius,
            CssProperties.BackgroundColor,
            CssProperties.Color,
            CssProperties.Opacity,
            CssProperties.BoxShadow,
            CssProperties.Overflow,
            CssProperties.OverflowX,
            CssProperties.OverflowY,
            CssProperties.Contain,
            CssProperties.ContainerName,
            CssProperties.ContainerType,
            CssProperties.FlexDirection,
            CssProperties.FlexWrap,
            CssProperties.FlexGrow,
            CssProperties.FlexShrink,
            CssProperties.FlexBasis,
            CssProperties.AlignItems,
            CssProperties.AlignSelf,
            CssProperties.AlignContent,
            CssProperties.JustifyContent,
            CssProperties.JustifySelf,
            CssProperties.Gap,
            CssProperties.RowGap,
            CssProperties.ColumnGap,
            CssProperties.Order,
            CssProperties.GridTemplateColumns,
            CssProperties.GridTemplateRows,
            CssProperties.GridTemplateAreas,
            CssProperties.GridColumn,
            CssProperties.GridRow,
            CssProperties.GridArea,
            CssProperties.FontFamily,
            CssProperties.FontSize,
            CssProperties.FontStyle,
            CssProperties.FontWeight,
            CssProperties.LineHeight,
            CssProperties.LetterSpacing,
            CssProperties.TextAlign,
            CssProperties.TextDecoration,
            CssProperties.WhiteSpace,
            CssProperties.WordBreak,
            CssProperties.PointerEvents,
            CssProperties.UserSelect,
            CssProperties.Transform,
            CssProperties.TransformOrigin,
            CssProperties.Outline,
            CssProperties.OutlineOffset,
            CssProperties.ScrollBehavior,
            CssProperties.ScrollMargin,
            CssProperties.ScrollPadding
        };
    }

    private static UiCssRule ConditionalRule(string expression)
    {
        UiCssRule rule = Rule(".item", Declaration(CssProperties.Display, "block"));
        rule.Condition = new UiCssCondition { Kind = "media", Expression = expression };
        return rule;
    }

    private static UiCssRule LayeredRule(string layer)
    {
        UiCssRule rule = Rule(".item", Declaration(CssProperties.Display, "block"));
        rule.Layer = layer;
        return rule;
    }

    private static UiCssRule Rule(string selector, UiCssDeclaration declaration)
    {
        return new UiCssRule { Selector = selector, Declarations = new UiCssDeclaration[] { declaration } };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value };
    }

    private static UiCssDeclaration Trusted(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value, IsTrusted = true };
    }

    private static UiLength Expression(string value)
    {
        return new UiLength { Expression = value };
    }
}
