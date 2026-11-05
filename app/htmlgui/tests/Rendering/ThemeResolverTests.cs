using System;
using System.Linq;
using NUnit.Framework;

namespace htmlgui.Tests;

public class ThemeResolverTests
{
    [Test]
    public void ResolvesVariantInheritanceTokenAliasesAndHostOverrides()
    {
        UiTheme theme = new UiTheme
        {
            Key = "application",
            Mode = UiThemeMode.Auto,
            Tokens = new UiValueEntry[]
            {
                Entry("surface", Text("white")),
                Entry("button.background", Text("{accent}")),
                Entry("accent", Text("blue"))
            },
            Variants = new UiThemeVariant[]
            {
                new UiThemeVariant
                {
                    Key = "contrast-dark",
                    ParentKey = "dark-base",
                    Mode = UiThemeMode.HighContrast,
                    Tokens = new UiValueEntry[]
                    {
                        Entry("border", Text("{accent}")),
                        Entry("accent", Text("#eeeeee"))
                    }
                },
                new UiThemeVariant
                {
                    Key = "dark-base",
                    Mode = UiThemeMode.Dark,
                    Tokens = new UiValueEntry[]
                    {
                        Entry("surface", Text("#111111")),
                        Entry("accent", Text("#dddddd"))
                    }
                }
            }
        };
        UiValueEntry[] hostOverrides = new UiValueEntry[]
        {
            Entry("host-only", Integer(4)),
            Entry("accent", Text("host-accent"))
        };
        ThemeResolver resolver = new ThemeResolver(20);

        ThemeResolution result = resolver.ResolveTheme(theme, UiThemeMode.Light, "contrast-dark", hostOverrides);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ThemeKey, Is.EqualTo("application"));
            Assert.That(result.VariantKey, Is.EqualTo("contrast-dark"));
            Assert.That(result.Mode, Is.EqualTo(UiThemeMode.HighContrast));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Tokens.Select(entry => entry.Key), Is.EqualTo(new string[] { "accent", "border", "button.background", "host-only", "surface" }));
            Assert.That(StringToken(result, "accent"), Is.EqualTo("host-accent"));
            Assert.That(StringToken(result, "border"), Is.EqualTo("host-accent"));
            Assert.That(StringToken(result, "button.background"), Is.EqualTo("host-accent"));
            Assert.That(StringToken(result, "surface"), Is.EqualTo("#111111"));
            Assert.That(Token(result, "host-only").IntegerValue, Is.EqualTo(4));
            Assert.That(theme.Tokens[1].Value.StringValue, Is.EqualTo("{accent}"));
            Assert.That(hostOverrides[1].Value.StringValue, Is.EqualTo("host-accent"));
        });

        Token(result, "accent").StringValue = "mutated";
        Assert.That(hostOverrides[1].Value.StringValue, Is.EqualTo("host-accent"));
    }

    [Test]
    public void SelectsLightDarkAndHighContrastVariantsFromThemeAndHostModes()
    {
        UiTheme theme = ModeTheme();
        ThemeResolver resolver = new ThemeResolver(20);

        ThemeResolution light = resolver.ResolveTheme(theme, UiThemeMode.Auto, null, null);
        ThemeResolution dark = resolver.ResolveTheme(theme, UiThemeMode.Dark, null, null);
        ThemeResolution contrast = resolver.ResolveTheme(theme, UiThemeMode.HighContrast, null, null);
        theme.Mode = UiThemeMode.Dark;
        ThemeResolution explicitThemeMode = resolver.ResolveTheme(theme, UiThemeMode.HighContrast, null, null);
        ThemeResolution named = resolver.ResolveTheme(theme, UiThemeMode.HighContrast, "light", null);

        Assert.Multiple(() =>
        {
            Assert.That(light.Mode, Is.EqualTo(UiThemeMode.Light));
            Assert.That(light.VariantKey, Is.EqualTo("light"));
            Assert.That(StringToken(light, "mode"), Is.EqualTo("light"));
            Assert.That(dark.Mode, Is.EqualTo(UiThemeMode.Dark));
            Assert.That(dark.VariantKey, Is.EqualTo("dark"));
            Assert.That(StringToken(dark, "mode"), Is.EqualTo("dark"));
            Assert.That(contrast.Mode, Is.EqualTo(UiThemeMode.HighContrast));
            Assert.That(contrast.VariantKey, Is.EqualTo("contrast"));
            Assert.That(StringToken(contrast, "mode"), Is.EqualTo("contrast"));
            Assert.That(explicitThemeMode.VariantKey, Is.EqualTo("dark"));
            Assert.That(named.Mode, Is.EqualTo(UiThemeMode.Light));
            Assert.That(named.VariantKey, Is.EqualTo("light"));
        });
    }

    [Test]
    public void UsesOrdinalVariantSelectionAndAutoFallbackDeterministically()
    {
        UiTheme theme = new UiTheme
        {
            Key = "fallback",
            Mode = UiThemeMode.Dark,
            Tokens = new UiValueEntry[] { Entry("base", Text("base")) },
            Variants = new UiThemeVariant[]
            {
                Variant("z-dark", UiThemeMode.Dark, "z"),
                Variant("a-dark", UiThemeMode.Dark, "a"),
                Variant("generic", UiThemeMode.Auto, "generic")
            }
        };
        ThemeResolver resolver = new ThemeResolver(20);

        ThemeResolution ordinal = resolver.ResolveTheme(theme, UiThemeMode.Light, null, null);
        theme.Mode = UiThemeMode.HighContrast;
        ThemeResolution fallback = resolver.ResolveTheme(theme, UiThemeMode.Light, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(ordinal.VariantKey, Is.EqualTo("a-dark"));
            Assert.That(StringToken(ordinal, "mode"), Is.EqualTo("a"));
            Assert.That(fallback.VariantKey, Is.EqualTo("generic"));
            Assert.That(fallback.Mode, Is.EqualTo(UiThemeMode.HighContrast));
            Assert.That(StringToken(fallback, "mode"), Is.EqualTo("generic"));
        });
    }

    [Test]
    public void ReportsMissingAndCyclicVariantInheritance()
    {
        UiTheme theme = new UiTheme
        {
            Key = "invalid-variants",
            Tokens = new UiValueEntry[] { Entry("base", Text("available")) },
            Variants = new UiThemeVariant[]
            {
                new UiThemeVariant { Key = "a", ParentKey = "b", Mode = UiThemeMode.Dark },
                new UiThemeVariant { Key = "b", ParentKey = "a", Mode = UiThemeMode.Dark },
                new UiThemeVariant { Key = "orphan", ParentKey = "missing", Mode = UiThemeMode.Light }
            }
        };
        ThemeResolver resolver = new ThemeResolver(20);

        ThemeResolution result = resolver.ResolveTheme(theme, UiThemeMode.Dark, "a", null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.VariantKey, Is.Null);
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "theme.variant-cycle", "theme.missing-parent" }));
            Assert.That(result.Diagnostics[0].Location, Does.EndWith("/parentKey"));
            Assert.That(result.Tokens, Has.Length.EqualTo(1));
            Assert.That(StringToken(result, "base"), Is.EqualTo("available"));
        });
    }

    [Test]
    public void ReportsMissingCyclicEmptyAndDuplicateTokenReferences()
    {
        UiTheme theme = new UiTheme
        {
            Key = "invalid-tokens",
            Tokens = new UiValueEntry[]
            {
                Entry("duplicate", Text("first")),
                Entry("duplicate", Text("second")),
                Entry("missing", Text("{not-present}")),
                Entry("cycle-a", Text("{cycle-b}")),
                Entry("cycle-b", Text("{cycle-a}")),
                Entry("empty", Text("{}")),
                Entry("good", Boolean(true))
            }
        };
        ThemeResolver resolver = new ThemeResolver(20);

        ThemeResolution result = resolver.ResolveTheme(theme, UiThemeMode.Light, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
            {
                "theme.duplicate-token",
                "theme.missing-token",
                "theme.token-cycle",
                "theme.invalid-token-reference"
            }));
            Assert.That(result.Tokens, Has.Length.EqualTo(1));
            Assert.That(result.Tokens[0].Key, Is.EqualTo("good"));
            Assert.That(result.Tokens[0].Value.BooleanValue, Is.True);
        });
    }

    [Test]
    public void ReportsInvalidThemeInputsWithoutThrowing()
    {
        ThemeResolver resolver = new ThemeResolver(20);
        ThemeResolution missing = resolver.ResolveTheme(null, UiThemeMode.Light, null, null);
        UiTheme malformed = new UiTheme
        {
            Key = " ",
            Mode = (UiThemeMode)99,
            Tokens = null!,
            Variants = null!
        };

        ThemeResolution invalid = resolver.ResolveTheme(malformed, (UiThemeMode)98, "missing", null);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[] { "theme.null" }));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Mode, Is.EqualTo(UiThemeMode.Light));
            Assert.That(invalid.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
            {
                "theme.invalid-key",
                "theme.invalid-host-mode",
                "theme.invalid-mode",
                "theme.null-variants",
                "theme.missing-variant",
                "theme.null-tokens"
            }));
        });
    }

    [Test]
    public void DiagnosesMalformedVariantsAndTokenEntries()
    {
        UiValueEntry nullValue = Entry("null-value", Text("temporary"));
        nullValue.Value = null!;
        UiTheme theme = new UiTheme
        {
            Key = "malformed-members",
            Tokens = new UiValueEntry[]
            {
                null!,
                Entry(" ", Text("empty-key")),
                nullValue,
                Entry("duplicate", Text("first")),
                Entry("unrelated", Text("kept")),
                Entry("duplicate", Text("second"))
            },
            Variants = new UiThemeVariant[]
            {
                null!,
                new UiThemeVariant { Key = " ", Mode = UiThemeMode.Light },
                new UiThemeVariant { Key = "bad-mode", Mode = (UiThemeMode)99 },
                new UiThemeVariant { Key = "duplicate", Mode = UiThemeMode.Dark },
                new UiThemeVariant { Key = "duplicate", Mode = UiThemeMode.Dark },
                new UiThemeVariant { Key = "blank-parent", ParentKey = " ", Mode = UiThemeMode.Light },
                new UiThemeVariant { Key = "shared-parent", Mode = UiThemeMode.Light },
                new UiThemeVariant { Key = "child-one", ParentKey = "shared-parent", Mode = UiThemeMode.Light },
                new UiThemeVariant { Key = "child-two", ParentKey = "shared-parent", Mode = UiThemeMode.Light },
                new UiThemeVariant { Key = "invalid-child", ParentKey = "duplicate", Mode = UiThemeMode.Dark }
            }
        };
        ThemeResolver resolver = new ThemeResolver(30);

        ThemeResolution result = resolver.ResolveTheme(theme, UiThemeMode.Light, " ", null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.null-variant"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.invalid-variant-key"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.invalid-variant-mode"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.duplicate-variant"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.invalid-parent"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.invalid-requested-variant"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.null-token"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.invalid-token-key"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.null-token-value"));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("theme.duplicate-token"));
            Assert.That(result.Tokens.Select(entry => entry.Key), Is.EqualTo(new string[] { "unrelated" }));
        });
    }

    private static UiTheme ModeTheme()
    {
        return new UiTheme
        {
            Key = "modes",
            Mode = UiThemeMode.Auto,
            Variants = new UiThemeVariant[]
            {
                Variant("dark", UiThemeMode.Dark, "dark"),
                Variant("contrast", UiThemeMode.HighContrast, "contrast"),
                Variant("light", UiThemeMode.Light, "light")
            }
        };
    }

    private static UiThemeVariant Variant(string key, UiThemeMode mode, string value)
    {
        return new UiThemeVariant
        {
            Key = key,
            Mode = mode,
            Tokens = new UiValueEntry[] { Entry("mode", Text(value)) }
        };
    }

    private static UiValue Token(ThemeResolution resolution, string key)
    {
        return resolution.Tokens.Single(entry => entry.Key == key).Value;
    }

    private static string? StringToken(ThemeResolution resolution, string key)
    {
        return Token(resolution, key).StringValue;
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

    private static UiValue Boolean(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
    }
}
