using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class CssDesignValueFormatterTests
{
    [TestCase(UiLengthUnit.Pixel, "px")]
    [TestCase(UiLengthUnit.Percent, "%")]
    [TestCase(UiLengthUnit.Em, "em")]
    [TestCase(UiLengthUnit.Rem, "rem")]
    [TestCase(UiLengthUnit.ViewportWidth, "vw")]
    [TestCase(UiLengthUnit.ViewportHeight, "vh")]
    [TestCase(UiLengthUnit.Fraction, "fr")]
    [TestCase(UiLengthUnit.Character, "ch")]
    [TestCase(UiLengthUnit.Centimeter, "cm")]
    [TestCase(UiLengthUnit.Millimeter, "mm")]
    [TestCase(UiLengthUnit.Inch, "in")]
    [TestCase(UiLengthUnit.Point, "pt")]
    [TestCase(UiLengthUnit.Pica, "pc")]
    public void FormatsEverySupportedLengthUnit(UiLengthUnit unit, string suffix)
    {
        UiLength length = Length(1.25, unit);

        Assert.That(CssDesignValueFormatter.FormatLength(length, unit == UiLengthUnit.Fraction), Is.EqualTo("1.25" + suffix));
    }

    [Test]
    public void AcceptsSafeLengthExpressionsAndRejectsInvalidLengthForms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssDesignValueFormatter.FormatLength(Expression(" calc(100% - 2rem) "), false), Is.EqualTo("calc(100% - 2rem)"));
            Assert.That(CssDesignValueFormatter.FormatLength(Expression("min-content"), false), Is.EqualTo("min-content"));
            Assert.That(CssDesignValueFormatter.FormatLength(Expression("var(--card-width)"), false), Is.EqualTo("var(--card-width)"));
            Assert.Throws<ArgumentNullException>(() => CssDesignValueFormatter.FormatLength(null, false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(new UiLength(), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Length(1, UiLengthUnit.Auto), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Length(1, UiLengthUnit.Fraction), false));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatLength(Length(1, (UiLengthUnit)99), true));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("url(https://example.test/a)"), false));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatLength(Expression("calc(1px);color:red"), false));
        });
    }

    [TestCase("#AbC", "#aabbcc")]
    [TestCase("#AbCd", "#aabbccdd")]
    [TestCase("#A0B1C2", "#a0b1c2")]
    [TestCase("#A0B1C2D3", "#a0b1c2d3")]
    [TestCase("ReD", "red")]
    [TestCase("currentColor", "currentcolor")]
    [TestCase("RebeccaPurple", "rebeccapurple")]
    [TestCase("CanvasText", "canvastext")]
    [TestCase("rgb(255, 0, 16)", "rgb(255 0 16)")]
    [TestCase("rgba(255, 0, 16, .50)", "rgb(255 0 16 / 0.5)")]
    [TestCase("rgba(0, 0, 0)", "rgb(0 0 0)")]
    [TestCase("rgb(100% 0% 50% / 25%)", "rgb(100% 0% 50% / 25%)")]
    public void NormalizesSupportedColors(string source, string expected)
    {
        Assert.That(CssDesignValueFormatter.NormalizeColor(source), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("#12")]
    [TestCase("#ggg")]
    [TestCase("not-a-color")]
    [TestCase("rgb(256 0 0)")]
    [TestCase("rgb(0 0 0 / 101%)")]
    [TestCase("rgb(0 0)")]
    [TestCase("rgb(0, 0, 0 / 50%)")]
    [TestCase("rgba(0 0 0 .5)")]
    [TestCase("rgb(0 0 0);color:red")]
    public void RejectsInvalidOrUnsupportedColors(string source)
    {
        Assert.That(() => CssDesignValueFormatter.NormalizeColor(source), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void CreatesCanonicalTypographyDeclarations()
    {
        CssTypographyValue typography = new CssTypographyValue
        {
            FontFamilies = new string[] { "Open Sans", "SYSTEM-UI", "system-ui" },
            FontSize = Length(1.125, UiLengthUnit.Rem),
            HasFontWeight = true,
            FontWeight = 650,
            FontStyle = "Italic",
            HasLineHeight = true,
            LineHeight = 1.5,
            LetterSpacing = Length(-0.02, UiLengthUnit.Em)
        };

        UiCssDeclaration[] declarations = CssDesignValueFormatter.CreateTypographyDeclarations(typography);
        string serialized = new CssSerializer().SerializeDeclarations(declarations);

        Assert.That(serialized, Is.EqualTo("font-family:\"Open Sans\",system-ui;font-size:1.125rem;font-style:italic;font-weight:650;letter-spacing:-0.02em;line-height:1.5;"));
    }

    [Test]
    public void CreatesLogicalSpacingAndRejectsNegativePadding()
    {
        UiSpacing margin = new UiSpacing
        {
            BlockStart = Length(1, UiLengthUnit.Rem),
            BlockEnd = Length(-2, UiLengthUnit.Pixel),
            InlineStart = Expression("var(--space)")
        };
        UiSpacing padding = new UiSpacing { InlineEnd = Length(-1, UiLengthUnit.Pixel) };

        string serialized = new CssSerializer().SerializeDeclarations(CssDesignValueFormatter.CreateSpacingDeclarations(margin, "margin"));

        Assert.Multiple(() =>
        {
            Assert.That(serialized, Is.EqualTo("margin-block-end:-2px;margin-block-start:1rem;margin-inline-start:var(--space);"));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.CreateSpacingDeclarations(padding, "padding"));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateSpacingDeclarations(margin, "gap"));
            Assert.Throws<ArgumentNullException>(() => CssDesignValueFormatter.CreateSpacingDeclarations(null, "margin"));
        });
    }

    [Test]
    public void CreatesLonghandBordersAndLogicalRadii()
    {
        CssBorderValue border = new CssBorderValue
        {
            Width = Length(1, UiLengthUnit.Pixel),
            Style = "Dashed",
            Color = "#AbC"
        };
        CssRadiusValue radius = new CssRadiusValue
        {
            BlockStartStart = Length(4, UiLengthUnit.Pixel),
            BlockStartEnd = Length(25, UiLengthUnit.Percent),
            BlockEndEnd = Expression("var(--radius)"),
            BlockEndStart = Length(0, UiLengthUnit.Pixel)
        };
        CssSerializer serializer = new CssSerializer();

        string borderCss = serializer.SerializeDeclarations(CssDesignValueFormatter.CreateBorderDeclarations(border));
        string radiusCss = serializer.SerializeDeclarations(CssDesignValueFormatter.CreateRadiusDeclarations(radius));

        Assert.Multiple(() =>
        {
            Assert.That(borderCss, Is.EqualTo("border-color:#aabbcc;border-style:dashed;border-width:1px;"));
            Assert.That(radiusCss, Is.EqualTo("border-end-end-radius:var(--radius);border-end-start-radius:0px;border-start-end-radius:25%;border-start-start-radius:4px;"));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateBorderDeclarations(new CssBorderValue { Width = Length(1, UiLengthUnit.Pixel), Style = "sparkly" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.CreateRadiusDeclarations(new CssRadiusValue { BlockStartStart = Length(-1, UiLengthUnit.Pixel) }));
        });
    }

    [Test]
    public void FormatsMultipleShadowsAndFocusRingLonghands()
    {
        CssShadowValue ambient = new CssShadowValue
        {
            OffsetX = Length(0, UiLengthUnit.Pixel),
            OffsetY = Length(2, UiLengthUnit.Pixel),
            Blur = Length(8, UiLengthUnit.Pixel),
            Color = "rgba(0, 0, 0, .50)"
        };
        CssShadowValue inset = new CssShadowValue
        {
            OffsetX = Length(1, UiLengthUnit.Pixel),
            OffsetY = Length(1, UiLengthUnit.Pixel),
            Blur = Length(2, UiLengthUnit.Pixel),
            Spread = Length(-1, UiLengthUnit.Pixel),
            Color = "#fff",
            Inset = true
        };
        CssFocusRingValue focus = new CssFocusRingValue
        {
            Width = Length(2, UiLengthUnit.Pixel),
            Offset = Length(3, UiLengthUnit.Pixel),
            Style = "solid",
            Color = "#abc"
        };

        string shadow = CssDesignValueFormatter.FormatShadows(new CssShadowValue[] { ambient, inset });
        string focusCss = new CssSerializer().SerializeDeclarations(CssDesignValueFormatter.CreateFocusRingDeclarations(focus));

        Assert.Multiple(() =>
        {
            Assert.That(shadow, Is.EqualTo("0px 2px 8px rgb(0 0 0 / 0.5),inset 1px 1px 2px -1px #ffffff"));
            Assert.That(focusCss, Is.EqualTo("outline-color:#aabbcc;outline-offset:3px;outline-style:solid;outline-width:2px;"));
            Assert.That(CssDesignValueFormatter.FormatShadows(Array.Empty<CssShadowValue>()), Is.EqualTo("none"));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatShadows(new CssShadowValue[] { new CssShadowValue { OffsetX = Length(0, UiLengthUnit.Pixel), OffsetY = Length(0, UiLengthUnit.Pixel), Blur = Length(-1, UiLengthUnit.Pixel) } }));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateFocusRingDeclarations(new CssFocusRingValue { Width = Length(1, UiLengthUnit.Pixel), Offset = Length(1, UiLengthUnit.Pixel), Style = "hidden" }));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateFocusRingDeclarations(new CssFocusRingValue { Width = Length(1, UiLengthUnit.Pixel), Offset = Length(1, UiLengthUnit.Pixel), Style = "none" }));
        });
    }

    [TestCase("base", 0, "0")]
    [TestCase("dropdown", 2, "1002")]
    [TestCase("sticky", -2, "1098")]
    [TestCase("overlay", 0, "1200")]
    [TestCase("modal", 0, "1300")]
    [TestCase("popover", 0, "1400")]
    [TestCase("tooltip", 0, "1500")]
    [TestCase("toast", 99, "1699")]
    public void FormatsNamedZLayers(string layer, int offset, string expected)
    {
        Assert.That(CssDesignValueFormatter.FormatZLayer(layer, offset), Is.EqualTo(expected));
    }

    [TestCase("default", ".scope-a .button")]
    [TestCase("hover", ".scope-a .button:hover")]
    [TestCase("focus", ".scope-a .button:focus-visible")]
    [TestCase("active", ".scope-a .button:active")]
    [TestCase("disabled", ".scope-a .button:disabled,.scope-a .button[aria-disabled=\"true\"]")]
    [TestCase("checked", ".scope-a .button:checked,.scope-a .button[aria-checked=\"true\"]")]
    [TestCase("selected", ".scope-a .button[aria-selected=\"true\"]")]
    [TestCase("expanded", ".scope-a .button[aria-expanded=\"true\"]")]
    [TestCase("invalid", ".scope-a .button:invalid,.scope-a .button[aria-invalid=\"true\"]")]
    [TestCase("busy", ".scope-a .button[aria-busy=\"true\"]")]
    [TestCase("read-only", ".scope-a .button:read-only")]
    public void GeneratesControlStateSelectors(string state, string expected)
    {
        Assert.That(CssDesignValueFormatter.CreateControlStateSelector("scope-a", ".button", state), Is.EqualTo(expected));
    }

    [Test]
    public void RejectsInvalidZLayersAndControlStateSelectors()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => CssDesignValueFormatter.FormatZLayer(null, 0));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.FormatZLayer("unknown", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CssDesignValueFormatter.FormatZLayer("modal", 100));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateControlStateSelector("scope", ".a,.b", "hover"));
            Assert.Throws<ArgumentException>(() => CssDesignValueFormatter.CreateControlStateSelector("scope", ".button", "unknown"));
            Assert.Throws<ArgumentNullException>(() => CssDesignValueFormatter.CreateControlStateSelector(null, ".button", "hover"));
        });
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static UiLength Expression(string value)
    {
        return new UiLength { Expression = value };
    }
}
