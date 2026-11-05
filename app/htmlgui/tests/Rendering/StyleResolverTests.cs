using System;
using System.Linq;
using NUnit.Framework;

namespace htmlgui.Tests;

public class StyleResolverTests
{
    [Test]
    public void AppliesEveryStyleOriginFromGlobalThroughInline()
    {
        UiStyle global = Style("global", "black");
        UiStyle window = Style("window", "navy");
        UiStyle page = Style("page", "blue");
        UiStyle component = Style("component", "teal");
        UiStyle control = Style("control", "green");
        UiStyle state = Style("state", "orange");
        UiStyle inline = Style("inline", "red");
        StyleResolver resolver = new StyleResolver(20);

        StyleResolution result = resolver.ResolveStyles(
            "application-seed",
            "save-button",
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
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Styles.Select(style => style.Origin), Is.EqualTo(new StyleOrigin[]
            {
                StyleOrigin.Global,
                StyleOrigin.Window,
                StyleOrigin.Page,
                StyleOrigin.Component,
                StyleOrigin.Control,
                StyleOrigin.State,
                StyleOrigin.Inline
            }));
            Assert.That(result.Styles.Select(style => style.Style.Key), Is.EqualTo(new string[]
            {
                "global", "window", "page", "component", "control", "state", "inline"
            }));
            Assert.That(Declaration(result, CssProperties.Color).Value, Is.EqualTo("red"));
            Assert.That(result.ScopeId, Is.EqualTo(result.Styles[6].ScopeId));
        });
    }

    [Test]
    public void ImportantDeclarationsWinBeforeOriginPrecedence()
    {
        UiStyle global = Style("global", "red");
        global.Declarations[0].Important = true;
        UiStyle page = Style("page", "blue");
        UiStyle state = Style("state", "green");
        state.Declarations[0].Important = true;
        UiStyle inline = Style("inline", "purple");
        StyleResolver resolver = new StyleResolver(20);

        StyleResolution result = resolver.ResolveStyles(
            "seed",
            "owner",
            new UiStyle[] { global },
            null,
            new UiStyle[] { page },
            null,
            null,
            new UiStyle[] { state },
            inline);

        UiCssDeclaration color = Declaration(result, CssProperties.Color);
        Assert.Multiple(() =>
        {
            Assert.That(color.Value, Is.EqualTo("green"));
            Assert.That(color.Important, Is.True);
            Assert.That(result.Success, Is.True);
        });
    }

    [Test]
    public void SameOriginStylesUseOrdinalKeyOrderRegardlessOfInputOrder()
    {
        UiStyle alpha = Style("alpha", "red");
        alpha.Scope = "shared-card";
        UiStyle zeta = Style("zeta", "blue");
        zeta.Scope = "shared-card";
        StyleResolver resolver = new StyleResolver(20);

        StyleResolution forward = resolver.ResolveStyles("seed", "owner", null, null, new UiStyle[] { alpha, zeta }, null, null, null, null);
        StyleResolution reversed = resolver.ResolveStyles("seed", "owner", null, null, new UiStyle[] { zeta, alpha }, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Styles.Select(style => style.Style.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(reversed.Styles.Select(style => style.Style.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(Declaration(forward, CssProperties.Color).Value, Is.EqualTo("blue"));
            Assert.That(Declaration(reversed, CssProperties.Color).Value, Is.EqualTo("blue"));
            Assert.That(reversed.ScopeId, Is.EqualTo(forward.ScopeId));
        });
    }

    [Test]
    public void ScopeIdsAreStableSafeAndSensitiveToEveryIdentityPart()
    {
        StyleResolver resolver = new StyleResolver(20);

        string scope = resolver.CreateScopeId("seed", "owner", "profile-card");

        Assert.Multiple(() =>
        {
            Assert.That(scope, Does.Match("^htmlgui-scope-[0-9a-f]{24}$"));
            Assert.That(resolver.CreateScopeId("seed", "owner", "profile-card"), Is.EqualTo(scope));
            Assert.That(resolver.CreateScopeId("other-seed", "owner", "profile-card"), Is.Not.EqualTo(scope));
            Assert.That(resolver.CreateScopeId("seed", "other-owner", "profile-card"), Is.Not.EqualTo(scope));
            Assert.That(resolver.CreateScopeId("seed", "owner", "other-card"), Is.Not.EqualTo(scope));
            Assert.Throws<ArgumentNullException>(() => resolver.CreateScopeId(null, "owner", "scope"));
            Assert.Throws<ArgumentException>(() => resolver.CreateScopeId("seed", " ", "scope"));
            Assert.Throws<ArgumentException>(() => resolver.CreateScopeId("seed", "owner", ""));
        });
    }

    [Test]
    public void SameNamedScopesAtDifferentOriginsHaveDistinctIds()
    {
        UiStyle global = Style("shared", "black");
        global.Scope = "shared";
        UiStyle component = Style("shared", "blue");
        component.Scope = "shared";
        StyleResolver resolver = new StyleResolver(20);

        StyleResolution result = resolver.ResolveStyles("seed", "owner", new UiStyle[] { global }, null, null, new UiStyle[] { component }, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Styles, Has.Length.EqualTo(2));
            Assert.That(result.Styles[0].ScopeId, Is.Not.EqualTo(result.Styles[1].ScopeId));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Global), Is.EqualTo("global"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Window), Is.EqualTo("window"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Page), Is.EqualTo("page"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Component), Is.EqualTo("component"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Control), Is.EqualTo("control"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.State), Is.EqualTo("state"));
            Assert.That(resolver.GetStyleOriginToken(StyleOrigin.Inline), Is.EqualTo("inline"));
            Assert.Throws<ArgumentOutOfRangeException>(() => resolver.GetStyleOriginToken((StyleOrigin)99));
        });
    }

    [Test]
    public void CarriesRulesAndResponsiveRulesInCascadeOrderAsDetachedCopies()
    {
        UiCssDeclaration nestedDeclaration = Declaration(CssProperties.Display, "grid", false);
        UiCssRule rule = new UiCssRule
        {
            Selector = ".card:hover",
            Layer = "components",
            Condition = new UiCssCondition { Kind = "media", ContainerName = "main", Expression = "(hover: hover)" },
            Declarations = new UiCssDeclaration[] { nestedDeclaration }
        };
        UiResponsiveRule responsive = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.MinWidth,
            Length = new UiLength { HasValue = true, Value = 40, Unit = UiLengthUnit.Rem },
            Value = "wide",
            IsContainer = true,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Gap, "2rem", false) }
        };
        UiStyle page = Style("page", "blue");
        page.Rules = new UiCssRule[] { rule };
        page.ResponsiveRules = new UiResponsiveRule[] { responsive };
        UiStyle state = Style("state", "red");
        state.Rules = new UiCssRule[] { new UiCssRule { Selector = ".card:active" } };
        StyleResolver resolver = new StyleResolver(20);

        StyleResolution result = resolver.ResolveStyles("seed", "owner", null, null, new UiStyle[] { page }, null, null, new UiStyle[] { state }, null);
        rule.Selector = ".mutated";
        nestedDeclaration.Value = "block";
        responsive.Length!.Value = 80;

        Assert.Multiple(() =>
        {
            Assert.That(result.Rules.Select(item => item.Selector), Is.EqualTo(new string[] { ".card:hover", ".card:active" }));
            Assert.That(result.Rules[0], Is.Not.SameAs(rule));
            Assert.That(result.Rules[0].Condition, Is.Not.SameAs(rule.Condition));
            Assert.That(result.Rules[0].Declarations[0].Value, Is.EqualTo("grid"));
            Assert.That(result.ResponsiveRules[0], Is.Not.SameAs(responsive));
            Assert.That(result.ResponsiveRules[0].Length, Is.Not.SameAs(responsive.Length));
            Assert.That(result.ResponsiveRules[0].Length!.Value, Is.EqualTo(40));
        });
    }

    [Test]
    public void ReportsMalformedStylesAndKeepsValidDeclarations()
    {
        UiStyle invalidMembers = Style("invalid-members", "red");
        invalidMembers.Scope = " ";
        invalidMembers.Declarations = new UiCssDeclaration[]
        {
            null!,
            Declaration(" ", "none", false),
            Declaration(CssProperties.Color, "first", false),
            Declaration(CssProperties.Display, "grid", false),
            Declaration(CssProperties.Color, "second", false)
        };
        invalidMembers.Rules = new UiCssRule[] { null! };
        invalidMembers.ResponsiveRules = new UiResponsiveRule[] { null! };
        UiStyle nullCollections = Style("null-collections", "green");
        nullCollections.Declarations = null!;
        nullCollections.Rules = null!;
        nullCollections.ResponsiveRules = null!;
        UiStyle duplicateOne = Style("duplicate", "pink");
        UiStyle duplicateTwo = Style("duplicate", "purple");
        UiStyle blank = Style(" ", "gray");
        StyleResolver resolver = new StyleResolver(30);

        StyleResolution result = resolver.ResolveStyles(
            null,
            " ",
            null,
            null,
            new UiStyle[] { null!, blank, duplicateOne, invalidMembers, duplicateTwo, nullCollections },
            null,
            null,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.invalid-root-seed"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.invalid-owner"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-style"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.invalid-key"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.duplicate-key"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.invalid-scope"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-declaration"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.invalid-property"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.duplicate-property"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-rule"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-responsive-rule"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-declarations"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-rules"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("style.null-responsive-rules"));
            Assert.That(result.Declarations.Select(declaration => declaration.Property), Is.EqualTo(new string[] { CssProperties.Display }));
            Assert.That(result.Declarations[0].Value, Is.EqualTo("grid"));
        });
    }

    private static UiStyle Style(string key, string color)
    {
        return new UiStyle
        {
            Key = key,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Color, color, false) }
        };
    }

    private static UiCssDeclaration Declaration(string property, string value, bool important)
    {
        return new UiCssDeclaration { Property = property, Value = value, Important = important };
    }

    private static UiCssDeclaration Declaration(StyleResolution resolution, string property)
    {
        return resolution.Declarations.Single(declaration => declaration.Property == property);
    }
}
