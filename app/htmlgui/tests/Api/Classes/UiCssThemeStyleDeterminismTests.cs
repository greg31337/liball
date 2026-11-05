using System;
using System.Linq;
using NUnit.Framework;

namespace htmlgui.Tests;

public class UiCssThemeStyleDeterminismTests
{
    [Test]
    public void VariantInheritanceAndHostOverridesAreCanonicalAcrossReordering()
    {
        IUiCssCompiler compiler = new UiCssCompiler();
        UiValueEntry[] firstOverrides = new UiValueEntry[]
        {
            Entry("density", Integer(3)),
            Entry("accent", Text("host-accent"))
        };
        UiValueEntry[] secondOverrides = new UiValueEntry[]
        {
            Entry("accent", Text("host-accent")),
            Entry("density", Integer(3))
        };

        UiCssCompilation firstTheme = compiler.ResolveTheme(Theme(false), UiThemeMode.Light, "contrast", firstOverrides);
        UiCssCompilation secondTheme = compiler.ResolveTheme(Theme(true), UiThemeMode.Light, "contrast", secondOverrides);
        UiCssCompilation firstCss = compiler.SerializeStylesheet(firstTheme.Rules);
        UiCssCompilation secondCss = compiler.SerializeStylesheet(secondTheme.Rules);

        Assert.Multiple(() =>
        {
            Assert.That(firstTheme.Success, Is.True);
            Assert.That(secondTheme.Success, Is.True);
            Assert.That(firstTheme.VariantKey, Is.EqualTo("contrast"));
            Assert.That(firstTheme.Theme.Mode, Is.EqualTo(UiThemeMode.HighContrast));
            Assert.That(firstTheme.Theme.Tokens.Select(entry => entry.Key), Is.EqualTo(new string[] { "accent", "border", "density", "surface" }));
            Assert.That(StringToken(firstTheme, "accent"), Is.EqualTo("host-accent"));
            Assert.That(StringToken(firstTheme, "border"), Is.EqualTo("host-accent"));
            Assert.That(firstCss.Css, Is.EqualTo(secondCss.Css));
            Assert.That(firstCss.Hash, Is.EqualTo(secondCss.Hash));
            Assert.That(firstTheme.Declarations.Select(item => item.Property), Is.EqualTo(secondTheme.Declarations.Select(item => item.Property)));
        });
    }

    [Test]
    public void CyclicVariantsAndTokensReturnDiagnosticsWithoutCss()
    {
        UiTheme theme = new UiTheme
        {
            Key = "cycles",
            Tokens = new UiValueEntry[]
            {
                Entry("a", Text("{b}")),
                Entry("b", Text("{a}"))
            },
            Variants = new UiThemeVariant[]
            {
                new UiThemeVariant { Key = "one", ParentKey = "two", Mode = UiThemeMode.Dark },
                new UiThemeVariant { Key = "two", ParentKey = "one", Mode = UiThemeMode.Dark }
            }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation result = compiler.ResolveTheme(theme, UiThemeMode.Dark, "one", null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("theme.variant-cycle"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("theme.token-cycle"));
            Assert.That(result.Declarations, Is.Empty);
            Assert.That(result.Rules, Is.Empty);
            Assert.That(result.Css, Is.Empty);
        });
    }

    [Test]
    public void StylePrecedenceHonorsImportantBeforeOrigin()
    {
        UiStyle global = Style("global", "red", true);
        UiStyle window = Style("window", "orange", false);
        UiStyle page = Style("page", "yellow", false);
        UiStyle component = Style("component", "green", false);
        UiStyle control = Style("control", "blue", false);
        UiStyle state = Style("state", "indigo", true);
        UiStyle inline = Style("inline", "violet", false);
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation result = compiler.GenerateScopedRules(
            "root",
            "owner",
            new UiStyle[] { global },
            new UiStyle[] { window },
            new UiStyle[] { page },
            new UiStyle[] { component },
            new UiStyle[] { control },
            new UiStyle[] { state },
            inline);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Declarations, Has.Length.EqualTo(1));
            Assert.That(result.Declarations[0].Property, Is.EqualTo(CssProperties.Color));
            Assert.That(result.Declarations[0].Value, Is.EqualTo("indigo"));
            Assert.That(result.Declarations[0].Important, Is.True);
        });
    }

    [Test]
    public void ReorderedStyleInputsProduceByteIdenticalScopedStylesheets()
    {
        UiStyle alpha = ComplexStyle("alpha", "shared", "red", ".alpha", 30);
        UiStyle zeta = ComplexStyle("zeta", "shared", "blue", ".zeta", 60);
        UiStyle firstAlpha = ComplexStyle("alpha", "shared", "red", ".alpha", 30);
        UiStyle firstZeta = ComplexStyle("zeta", "shared", "blue", ".zeta", 60);
        ReverseStyleMembers(firstAlpha);
        ReverseStyleMembers(firstZeta);
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation first = compiler.GenerateScopedRules("root", "owner", null, null, new UiStyle[] { firstZeta, firstAlpha }, null, null, null, null);
        UiCssCompilation second = compiler.GenerateScopedRules("root", "owner", null, null, new UiStyle[] { alpha, zeta }, null, null, null, null);
        UiCssCompilation firstCss = compiler.SerializeStylesheet(first.Rules);
        UiCssCompilation secondCss = compiler.SerializeStylesheet(second.Rules);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.ScopeId, Is.EqualTo(second.ScopeId));
            Assert.That(firstCss.Css, Is.EqualTo(secondCss.Css));
            Assert.That(firstCss.Hash, Is.EqualTo(secondCss.Hash));
            Assert.That(firstCss.Css, Does.Contain("color:blue"));
            Assert.That(firstCss.Css, Does.Contain("@media (min-width: 30rem)"));
            Assert.That(firstCss.Css, Does.Contain("@media (min-width: 60rem)"));
        });
    }

    [Test]
    public void CanonicalResultRulesIgnoreEquivalentRawOrderingAndWhitespace()
    {
        UiCssRule raw = new UiCssRule
        {
            Selector = "  .card  ",
            Declarations = new UiCssDeclaration[]
            {
                Declaration(CssProperties.Opacity, " .5 ", false),
                Declaration(CssProperties.Display, " grid ", false),
                Declaration(CssProperties.Display, " grid ", false)
            },
            Condition = new UiCssCondition { Kind = "media", Expression = "  (hover: hover)  " }
        };
        UiCssRule canonical = new UiCssRule
        {
            Selector = ".card",
            Declarations = new UiCssDeclaration[]
            {
                Declaration(CssProperties.Display, "grid", false),
                Declaration(CssProperties.Opacity, ".5", false)
            },
            Condition = new UiCssCondition { Kind = "media", Expression = "(hover: hover)" }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation first = compiler.SerializeStylesheet(new UiCssRule[] { raw });
        UiCssCompilation second = compiler.SerializeStylesheet(new UiCssRule[] { canonical });

        Assert.Multiple(() =>
        {
            Assert.That(first.Css, Is.EqualTo(second.Css));
            Assert.That(first.Hash, Is.EqualTo(second.Hash));
            Assert.That(first.Rules[0].Selector, Is.EqualTo(second.Rules[0].Selector));
            Assert.That(first.Rules[0].Condition!.Expression, Is.EqualTo(second.Rules[0].Condition!.Expression));
            Assert.That(first.Rules[0].Declarations, Has.Length.EqualTo(2));
            Assert.That(first.Rules[0].Declarations.Select(item => item.Property), Is.EqualTo(new string[] { CssProperties.Display, CssProperties.Opacity }));
            Assert.That(first.Rules[0].Declarations.Select(item => item.Value), Is.EqualTo(new string[] { "grid", ".5" }));
        });
    }

    [Test]
    public void ScopesAreStableForOneOwnerAndIsolatedAcrossOwnersAndOrigins()
    {
        UiStyle shared = Style("shared", "red", false);
        shared.Scope = "profile";
        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation first = compiler.GenerateScopedRules("root", "owner-a", null, null, null, new UiStyle[] { shared }, null, null, null);
        UiCssCompilation repeat = compiler.GenerateScopedRules("root", "owner-a", null, null, null, new UiStyle[] { shared }, null, null, null);
        UiCssCompilation otherOwner = compiler.GenerateScopedRules("root", "owner-b", null, null, null, new UiStyle[] { shared }, null, null, null);
        UiCssCompilation otherOrigin = compiler.GenerateScopedRules("root", "owner-a", new UiStyle[] { shared }, null, null, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(first.ScopeId, Is.EqualTo(repeat.ScopeId));
            Assert.That(first.ScopeId, Is.Not.EqualTo(otherOwner.ScopeId));
            Assert.That(first.ScopeId, Is.Not.EqualTo(otherOrigin.ScopeId));
            Assert.That(first.Rules[0].Selector, Is.EqualTo("." + first.ScopeId));
        });
    }

    [Test]
    public void SerializerInterningReusesCanonicalRulesAndHashesRemainContentBased()
    {
        CssSerializer serializer = new CssSerializer();
        UiCssRule firstRule = Rule(".card", CssProperties.Color, "red");
        UiCssRule duplicateRule = Rule(" .card ", CssProperties.Color, " red ");
        UiCssRule otherRule = Rule(".card", CssProperties.Color, "blue");

        string first = serializer.SerializeRule(firstRule);
        string duplicate = serializer.SerializeRule(duplicateRule);
        CssSerializationResult firstSheet = serializer.SerializeStylesheet(new UiCssRule[] { firstRule });
        CssSerializationResult duplicateSheet = serializer.SerializeStylesheet(new UiCssRule[] { duplicateRule });
        CssSerializationResult otherSheet = serializer.SerializeStylesheet(new UiCssRule[] { otherRule });

        Assert.Multiple(() =>
        {
            Assert.That(object.ReferenceEquals(first, duplicate), Is.True);
            Assert.That(serializer.GetInternedRuleCount(), Is.EqualTo(2));
            Assert.That(firstSheet.Css, Is.EqualTo(duplicateSheet.Css));
            Assert.That(firstSheet.Hash, Is.EqualTo(duplicateSheet.Hash));
            Assert.That(firstSheet.Hash, Is.Not.EqualTo(otherSheet.Hash));
            Assert.That(firstSheet.Hash, Is.EqualTo(StableHash.HashText(firstSheet.Css)));
        });
    }

    private static UiTheme Theme(bool reverse)
    {
        UiValueEntry[] tokens = new UiValueEntry[]
        {
            Entry("surface", Text("white")),
            Entry("accent", Text("blue"))
        };
        UiThemeVariant dark = new UiThemeVariant
        {
            Key = "dark",
            Mode = UiThemeMode.Dark,
            Tokens = new UiValueEntry[]
            {
                Entry("surface", Text("black")),
                Entry("accent", Text("gray"))
            }
        };
        UiThemeVariant contrast = new UiThemeVariant
        {
            Key = "contrast",
            ParentKey = "dark",
            Mode = UiThemeMode.HighContrast,
            Tokens = new UiValueEntry[]
            {
                Entry("border", Text("{accent}")),
                Entry("accent", Text("canvastext"))
            }
        };
        if (reverse)
        {
            tokens = new UiValueEntry[] { tokens[1], tokens[0] };
            dark.Tokens = new UiValueEntry[] { dark.Tokens[1], dark.Tokens[0] };
            contrast.Tokens = new UiValueEntry[] { contrast.Tokens[1], contrast.Tokens[0] };
        }

        return new UiTheme
        {
            Key = "application",
            Mode = UiThemeMode.Auto,
            Tokens = tokens,
            Variants = reverse ? new UiThemeVariant[] { contrast, dark } : new UiThemeVariant[] { dark, contrast }
        };
    }

    private static UiStyle ComplexStyle(string key, string scope, string color, string selector, int minimumWidth)
    {
        return new UiStyle
        {
            Key = key,
            Scope = scope,
            Declarations = new UiCssDeclaration[]
            {
                Declaration(CssProperties.Display, "grid", false),
                Declaration(CssProperties.Color, color, false)
            },
            Rules = new UiCssRule[]
            {
                Rule(selector + ":hover", CssProperties.Opacity, ".8"),
                Rule(selector + ":focus", CssProperties.Outline, "2px solid currentcolor")
            },
            ResponsiveRules = new UiResponsiveRule[]
            {
                new UiResponsiveRule
                {
                    Operator = UiResponsiveOperator.MinWidth,
                    Length = new UiLength { HasValue = true, Value = minimumWidth, Unit = UiLengthUnit.Rem },
                    Declarations = new UiCssDeclaration[]
                    {
                        Declaration(CssProperties.Gap, "2rem", false),
                        Declaration(CssProperties.Order, "1", false)
                    }
                }
            }
        };
    }

    private static void ReverseStyleMembers(UiStyle style)
    {
        Array.Reverse(style.Declarations);
        Array.Reverse(style.Rules);
        Array.Reverse(style.ResponsiveRules[0].Declarations);
    }

    private static UiStyle Style(string key, string color, bool important)
    {
        return new UiStyle
        {
            Key = key,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Color, color, important) }
        };
    }

    private static UiCssRule Rule(string selector, string property, string value)
    {
        return new UiCssRule
        {
            Selector = selector,
            Declarations = new UiCssDeclaration[] { Declaration(property, value, false) }
        };
    }

    private static UiCssDeclaration Declaration(string property, string value, bool important)
    {
        return new UiCssDeclaration { Property = property, Value = value, Important = important };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static string? StringToken(UiCssCompilation compilation, string key)
    {
        return compilation.Theme.Tokens.Single(entry => entry.Key == key).Value.StringValue;
    }
}
