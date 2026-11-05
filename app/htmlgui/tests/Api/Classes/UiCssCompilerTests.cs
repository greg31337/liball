using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class UiCssCompilerTests
{
    [Test]
    public void ResolvesThemeVariantsAliasesAndOverridesIntoRootVariables()
    {
        UiTheme theme = new UiTheme
        {
            Key = "application",
            Mode = UiThemeMode.Auto,
            Tokens = new UiValueEntry[]
            {
                Entry("surface", "{accent}"),
                Entry("accent", "blue")
            },
            Variants = new UiThemeVariant[]
            {
                new UiThemeVariant
                {
                    Key = "dark",
                    Mode = UiThemeMode.Dark,
                    Tokens = new UiValueEntry[] { Entry("accent", "black") }
                }
            }
        };
        UiValueEntry[] overrides = new UiValueEntry[] { Entry("accent", "white") };
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation resolved = compiler.ResolveTheme(theme, UiThemeMode.Dark, null, overrides);
        UiCssCompilation serialized = compiler.SerializeStylesheet(resolved.Rules);
        overrides[0].Value.StringValue = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.True);
            Assert.That(resolved.Theme.Key, Is.EqualTo("application"));
            Assert.That(resolved.Theme.Mode, Is.EqualTo(UiThemeMode.Dark));
            Assert.That(resolved.VariantKey, Is.EqualTo("dark"));
            Assert.That(resolved.Theme.Tokens, Has.Length.EqualTo(2));
            Assert.That(resolved.Theme.Tokens[0].Key, Is.EqualTo("accent"));
            Assert.That(resolved.Theme.Tokens[0].Value.StringValue, Is.EqualTo("white"));
            Assert.That(resolved.Theme.Tokens[1].Key, Is.EqualTo("surface"));
            Assert.That(resolved.Theme.Tokens[1].Value.StringValue, Is.EqualTo("white"));
            Assert.That(resolved.Declarations, Has.Length.EqualTo(2));
            Assert.That(resolved.Declarations[0].Property, Does.StartWith("--application-accent-"));
            Assert.That(resolved.Declarations[1].Property, Does.StartWith("--application-surface-"));
            Assert.That(serialized.Css, Does.StartWith(":root{"));
            Assert.That(serialized.Css, Does.Contain(":white;"));
            Assert.That(resolved.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReturnsThemeDiagnosticsWithoutPublishingPartialCss()
    {
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation result = compiler.ResolveTheme(null, UiThemeMode.Light, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(UiDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("theme.null"));
            Assert.That(result.Declarations, Is.Empty);
            Assert.That(result.Rules, Is.Empty);
        });
    }

    [Test]
    public void GeneratesScopedPrecedenceNestedAndResponsiveRules()
    {
        UiStyle global = Style("global", "red");
        UiStyle component = Style("component", "blue");
        component.Declarations = new UiCssDeclaration[]
        {
            Declaration(CssProperties.Display, "grid"),
            Declaration(CssProperties.Color, "blue")
        };
        component.Rules = new UiCssRule[]
        {
            new UiCssRule
            {
                Selector = ".button,.link",
                Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Opacity, ".8") }
            }
        };
        component.ResponsiveRules = new UiResponsiveRule[]
        {
            new UiResponsiveRule
            {
                Operator = UiResponsiveOperator.MinWidth,
                Length = Length(40, UiLengthUnit.Rem),
                Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Gap, "2rem") }
            }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation generated = compiler.GenerateScopedRules("root", "owner", new UiStyle[] { global }, null, null, new UiStyle[] { component }, null, null, null);
        UiCssCompilation serialized = compiler.SerializeStylesheet(generated.Rules);
        component.Declarations[1].Value = "green";
        component.Rules[0].Selector = "body";
        component.ResponsiveRules[0].Length!.Value = 80;

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True);
            Assert.That(generated.ScopeId, Does.StartWith("htmlgui-scope-"));
            Assert.That(generated.Declarations, Has.Length.EqualTo(2));
            Assert.That(generated.Declarations[0].Property, Is.EqualTo(CssProperties.Color));
            Assert.That(generated.Declarations[0].Value, Is.EqualTo("blue"));
            Assert.That(generated.Rules, Has.Length.EqualTo(3));
            Assert.That(generated.Rules[0].Selector, Is.EqualTo("." + generated.ScopeId));
            Assert.That(generated.Rules[1].Selector, Is.EqualTo(":where(." + generated.ScopeId + ") :is(.button,.link)"));
            Assert.That(generated.Rules[2].Condition!.Expression, Is.EqualTo("(min-width: 40rem)"));
            Assert.That(serialized.Css, Does.Contain("." + generated.ScopeId + "{color:blue;display:grid;}"));
            Assert.That(serialized.Css, Does.Contain(":where(." + generated.ScopeId + ") :is(.button,.link){opacity:.8;}"));
            Assert.That(serialized.Css, Does.Contain("@media (min-width: 40rem){." + generated.ScopeId + "{gap:2rem;}}"));
            Assert.That(generated.ResponsiveRules[0].Length!.Value, Is.EqualTo(40));
        });
    }

    [Test]
    public void InvalidStyleInputProducesDiagnosticsAndNoRules()
    {
        UiStyle invalid = Style(" ", "red");
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation result = compiler.GenerateScopedRules("root", "owner", new UiStyle[] { invalid }, null, null, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("style.invalid-key"));
            Assert.That(result.Rules, Is.Empty);
        });
    }

    [Test]
    public void GeneratesResponsiveRulesThroughThePublicContract()
    {
        UiResponsiveRule wide = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.MinWidth,
            Length = Length(50, UiLengthUnit.Rem),
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "grid") }
        };
        UiResponsiveRule duplicate = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.MinWidth,
            Length = Length(50, UiLengthUnit.Rem),
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "grid") }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation generated = compiler.GenerateResponsiveRules(new UiResponsiveRule[] { wide, duplicate }, ".card");
        wide.Length.Value = 10;

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True);
            Assert.That(generated.Rules, Has.Length.EqualTo(1));
            Assert.That(generated.ResponsiveRules, Has.Length.EqualTo(2));
            Assert.That(generated.Rules[0].Selector, Is.EqualTo(".card"));
            Assert.That(generated.Rules[0].Condition!.Expression, Is.EqualTo("(min-width: 50rem)"));
            Assert.That(generated.ResponsiveRules[0].Length!.Value, Is.EqualTo(50));
        });
    }

    [Test]
    public void SerializesDeduplicatesHashesAndDetachesCanonicalRules()
    {
        UiCssRule zeta = CssRule(".zeta", CssProperties.Color, "red");
        UiCssRule alpha = CssRule(".alpha", CssProperties.Display, "block");
        UiCssRule duplicate = CssRule(".zeta", CssProperties.Color, "red");
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation result = compiler.SerializeStylesheet(new UiCssRule[] { zeta, alpha, duplicate });
        alpha.Declarations[0].Value = "grid";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Css, Is.EqualTo(".alpha{display:block;}.zeta{color:red;}"));
            Assert.That(result.Hash, Is.EqualTo(StableHash.HashText(result.Css)));
            Assert.That(result.InputRuleCount, Is.EqualTo(3));
            Assert.That(result.Rules, Has.Length.EqualTo(2));
            Assert.That(result.Rules[0].Selector, Is.EqualTo(".alpha"));
            Assert.That(result.Rules[0].Declarations[0].Value, Is.EqualTo("block"));
        });
    }

    [Test]
    public void RejectsInvalidDirectCompilationInputs()
    {
        IUiCssCompiler compiler = new UiCssCompiler();
        UiResponsiveRule invalidResponsive = new UiResponsiveRule { Operator = UiResponsiveOperator.MinWidth };
        UiCssRule invalidRule = CssRule("body{}", CssProperties.Color, "red");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => compiler.GenerateResponsiveRules(null, ".card"));
            Assert.Throws<ArgumentException>(() => compiler.GenerateResponsiveRules(new UiResponsiveRule[] { invalidResponsive }, ".card"));
            Assert.Throws<ArgumentNullException>(() => compiler.SerializeStylesheet(null));
            Assert.Throws<ArgumentException>(() => compiler.SerializeStylesheet(new UiCssRule[] { invalidRule }));
        });
    }

    [Test]
    public void CompilationResultHasFreshDeterministicDefaults()
    {
        UiCssCompilation first = new UiCssCompilation();
        UiCssCompilation second = new UiCssCompilation();

        Assert.Multiple(() =>
        {
            Assert.That(first.Theme, Is.Not.SameAs(second.Theme));
            Assert.That(first.ScopeId, Is.Empty);
            Assert.That(first.Declarations, Is.Empty);
            Assert.That(first.Rules, Is.Empty);
            Assert.That(first.ResponsiveRules, Is.Empty);
            Assert.That(first.Css, Is.Empty);
            Assert.That(first.Hash, Is.Empty);
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(first.Success, Is.False);
        });
    }

    private static UiStyle Style(string key, string color)
    {
        return new UiStyle
        {
            Key = key,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Color, color) }
        };
    }

    private static UiCssRule CssRule(string selector, string property, string value)
    {
        return new UiCssRule
        {
            Selector = selector,
            Declarations = new UiCssDeclaration[] { Declaration(property, value) }
        };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        };
    }
}
