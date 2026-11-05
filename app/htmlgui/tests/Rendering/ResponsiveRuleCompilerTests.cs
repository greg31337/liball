using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class ResponsiveRuleCompilerTests
{
    [TestCase(UiResponsiveOperator.MinWidth, "min-width")]
    [TestCase(UiResponsiveOperator.MaxWidth, "max-width")]
    [TestCase(UiResponsiveOperator.MinHeight, "min-height")]
    [TestCase(UiResponsiveOperator.MaxHeight, "max-height")]
    public void CompilesEveryViewportSizeOperator(UiResponsiveOperator responsiveOperator, string feature)
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiResponsiveRule responsiveRule = Rule(responsiveOperator, Declaration(CssProperties.Display, "grid"));
        responsiveRule.Length = Length(40, UiLengthUnit.Rem);

        UiCssRule rule = compiler.CreateRule(responsiveRule, " .card ");

        Assert.Multiple(() =>
        {
            Assert.That(rule.Condition!.Kind, Is.EqualTo("media"));
            Assert.That(rule.Condition.Expression, Is.EqualTo("(" + feature + ": 40rem)"));
            Assert.That(rule.Condition.ContainerName, Is.Null);
            Assert.That(new CssSerializer().SerializeRule(rule), Is.EqualTo("@media (" + feature + ": 40rem){.card{display:grid;}}"));
        });
    }

    [TestCase(UiResponsiveOperator.Orientation, "LANDSCAPE", "orientation", "landscape")]
    [TestCase(UiResponsiveOperator.Pointer, "Coarse", "pointer", "coarse")]
    [TestCase(UiResponsiveOperator.Hover, "HOVER", "hover", "hover")]
    [TestCase(UiResponsiveOperator.ColorScheme, "Dark", "prefers-color-scheme", "dark")]
    [TestCase(UiResponsiveOperator.Contrast, "MORE", "prefers-contrast", "more")]
    [TestCase(UiResponsiveOperator.Motion, "Reduce", "prefers-reduced-motion", "reduce")]
    public void CompilesEveryViewportFeatureOperator(UiResponsiveOperator responsiveOperator, string sourceValue, string feature, string expectedValue)
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiResponsiveRule responsiveRule = Rule(responsiveOperator, Declaration(CssProperties.Opacity, ".5"));
        responsiveRule.Value = sourceValue;

        UiCssRule rule = compiler.CreateRule(responsiveRule, ".card");

        Assert.That(new CssSerializer().SerializeRule(rule), Is.EqualTo("@media (" + feature + ": " + expectedValue + "){.card{opacity:.5;}}"));
    }

    [Test]
    public void CompilesNamedSizeAndUnnamedOrientationContainerRules()
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiResponsiveRule size = Rule(UiResponsiveOperator.MinWidth, Declaration(CssProperties.Display, "grid"));
        size.Length = Length(30, UiLengthUnit.Rem);
        size.Value = "main panel";
        size.IsContainer = true;
        UiResponsiveRule orientation = Rule(UiResponsiveOperator.Orientation, Declaration(CssProperties.FlexDirection, "column"));
        orientation.Value = "portrait";
        orientation.IsContainer = true;

        string named = new CssSerializer().SerializeRule(compiler.CreateRule(size, ".card"));
        string unnamed = new CssSerializer().SerializeRule(compiler.CreateRule(orientation, ".card"));

        Assert.Multiple(() =>
        {
            Assert.That(named, Is.EqualTo("@container main\\20 panel (min-width: 30rem){.card{display:grid;}}"));
            Assert.That(unnamed, Is.EqualTo("@container (orientation: portrait){.card{flex-direction:column;}}"));
        });
    }

    [Test]
    public void DeduplicatesAndCanonicallyOrdersDetachedResponsiveRules()
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiResponsiveRule wide = Rule(UiResponsiveOperator.MinWidth, Declaration(CssProperties.Color, "red"));
        wide.Length = Length(50, UiLengthUnit.Rem);
        UiResponsiveRule dark = Rule(UiResponsiveOperator.ColorScheme, Declaration(CssProperties.Color, "white"));
        dark.Value = "dark";
        UiResponsiveRule duplicate = Rule(UiResponsiveOperator.MinWidth, Declaration(CssProperties.Color, "red"));
        duplicate.Length = Length(50, UiLengthUnit.Rem);

        UiCssRule[] first = compiler.CompileRules(new UiResponsiveRule[] { dark, duplicate, wide }, ".card");
        UiCssRule[] reordered = compiler.CompileRules(new UiResponsiveRule[] { wide, dark, duplicate }, ".card");
        wide.Declarations[0].Value = "blue";
        CssSerializationResult firstCss = new CssSerializer().SerializeStylesheet(first);
        CssSerializationResult reorderedCss = new CssSerializer().SerializeStylesheet(reordered);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(2));
            Assert.That(firstCss.Css, Is.EqualTo("@media (min-width: 50rem){.card{color:red;}}@media (prefers-color-scheme: dark){.card{color:white;}}"));
            Assert.That(reorderedCss.Css, Is.EqualTo(firstCss.Css));
            Assert.That(first[0].Declarations[0], Is.Not.SameAs(duplicate.Declarations[0]));
        });
    }

    [TestCase(UiLayoutKind.Flow, UiOrientation.Horizontal, false, "display:block;")]
    [TestCase(UiLayoutKind.Inline, UiOrientation.Horizontal, false, "display:inline;")]
    [TestCase(UiLayoutKind.VerticalStack, UiOrientation.Horizontal, false, "display:flex;flex-direction:column;")]
    [TestCase(UiLayoutKind.HorizontalStack, UiOrientation.Horizontal, false, "display:flex;flex-direction:row;")]
    [TestCase(UiLayoutKind.FlexRow, UiOrientation.Horizontal, true, "display:flex;flex-direction:row;flex-wrap:wrap;")]
    [TestCase(UiLayoutKind.FlexColumn, UiOrientation.Horizontal, false, "display:flex;flex-direction:column;")]
    [TestCase(UiLayoutKind.Wrap, UiOrientation.Horizontal, true, "display:flex;flex-wrap:wrap;")]
    [TestCase(UiLayoutKind.Grid, UiOrientation.Horizontal, false, "display:grid;")]
    [TestCase(UiLayoutKind.UniformGrid, UiOrientation.Horizontal, false, "display:grid;")]
    [TestCase(UiLayoutKind.Dock, UiOrientation.Horizontal, false, "display:grid;")]
    [TestCase(UiLayoutKind.Overlay, UiOrientation.Horizontal, false, "display:grid;")]
    [TestCase(UiLayoutKind.Absolute, UiOrientation.Horizontal, false, "position:relative;")]
    [TestCase(UiLayoutKind.Split, UiOrientation.Vertical, false, "display:flex;flex-direction:column;")]
    [TestCase(UiLayoutKind.Scroll, UiOrientation.Horizontal, false, "display:block;")]
    [TestCase(UiLayoutKind.ResponsiveShell, UiOrientation.Horizontal, false, "display:grid;")]
    public void CreatesEveryResponsiveLayoutOverride(UiLayoutKind kind, UiOrientation orientation, bool wrap, string expected)
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();

        UiCssDeclaration[] declarations = compiler.CreateLayoutOverride(kind, orientation, wrap);

        Assert.That(new CssSerializer().SerializeDeclarations(declarations), Is.EqualTo(expected));
    }

    [Test]
    public void CreatesValueVisibilityAndOrderOverridesThatCompileIntoRules()
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiCssDeclaration value = compiler.CreateValueOverride(CssProperties.Opacity, " .75 ", false);
        UiCssDeclaration hidden = compiler.CreateVisibilityOverride(false);
        UiCssDeclaration visible = compiler.CreateVisibilityOverride(true);
        UiCssDeclaration order = compiler.CreateOrderOverride(-2);
        UiResponsiveRule responsiveRule = Rule(UiResponsiveOperator.MinWidth, value, order);
        responsiveRule.Length = Length(20, UiLengthUnit.Em);

        string css = new CssSerializer().SerializeRule(compiler.CreateRule(responsiveRule, ".item"));

        Assert.Multiple(() =>
        {
            Assert.That(css, Is.EqualTo("@media (min-width: 20em){.item{opacity:.75;order:-2;}}"));
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { hidden }), Is.EqualTo("display:none;"));
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { visible }), Is.EqualTo("display:revert-layer;"));
        });
    }

    [Test]
    public void RejectsInvalidResponsiveConditionsAndOverrides()
    {
        ResponsiveRuleCompiler compiler = new ResponsiveRuleCompiler();
        UiResponsiveRule missingSize = Rule(UiResponsiveOperator.MinWidth, Declaration(CssProperties.Display, "block"));
        UiResponsiveRule negativeSize = Rule(UiResponsiveOperator.MaxWidth, Declaration(CssProperties.Display, "block"));
        negativeSize.Length = Length(-1, UiLengthUnit.Pixel);
        UiResponsiveRule keywordSize = Rule(UiResponsiveOperator.MinHeight, Declaration(CssProperties.Display, "block"));
        keywordSize.Length = new UiLength { Expression = "auto" };
        UiResponsiveRule unexpectedName = Rule(UiResponsiveOperator.MaxHeight, Declaration(CssProperties.Display, "block"));
        unexpectedName.Length = Length(10, UiLengthUnit.Pixel);
        unexpectedName.Value = "main";
        UiResponsiveRule lengthFeature = Rule(UiResponsiveOperator.Hover, Declaration(CssProperties.Display, "block"));
        lengthFeature.Value = "hover";
        lengthFeature.Length = Length(1, UiLengthUnit.Pixel);
        UiResponsiveRule containerFeature = Rule(UiResponsiveOperator.Motion, Declaration(CssProperties.Display, "block"));
        containerFeature.Value = "reduce";
        containerFeature.IsContainer = true;
        UiResponsiveRule unsupportedToken = Rule(UiResponsiveOperator.Contrast, Declaration(CssProperties.Display, "block"));
        unsupportedToken.Value = "maximum";
        UiResponsiveRule nullDeclarations = Rule(UiResponsiveOperator.ColorScheme, Declaration(CssProperties.Display, "block"));
        nullDeclarations.Value = "dark";
        nullDeclarations.Declarations = new UiCssDeclaration[] { null! };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => compiler.CompileRules(null, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CompileRules(new UiResponsiveRule[] { null! }, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CompileRules(Array.Empty<UiResponsiveRule>(), " "));
            Assert.Throws<ArgumentNullException>(() => compiler.CreateRule(null, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(missingSize, ".card"));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CreateRule(negativeSize, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(keywordSize, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(unexpectedName, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(lengthFeature, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(containerFeature, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(unsupportedToken, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.CreateRule(nullDeclarations, ".card"));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CreateCondition(new UiResponsiveRule { Operator = (UiResponsiveOperator)99 }));
            Assert.Throws<ArgumentException>(() => compiler.CreateValueOverride(CssProperties.Background, "url(https://example.test)", false));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CreateLayoutOverride((UiLayoutKind)99, UiOrientation.Horizontal, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CreateLayoutOverride(UiLayoutKind.Split, (UiOrientation)99, false));
        });
    }

    private static UiResponsiveRule Rule(UiResponsiveOperator responsiveOperator, params UiCssDeclaration[] declarations)
    {
        return new UiResponsiveRule { Operator = responsiveOperator, Declarations = declarations };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }
}
