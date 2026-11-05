using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using htmlgui;

namespace htmlgui.Tests;

public class StaticNamesTests
{
    [Test]
    public void HtmlCatalogsExposeCanonicalTokens()
    {
        AssertCatalog(typeof(HtmlElements), "^[a-z][a-z0-9]*$");
        AssertCatalog(typeof(HtmlAttributes), "^[a-z][a-z0-9-]*-?$");
        AssertCatalog(typeof(HtmlInputTypes), "^[a-z][a-z0-9-]*$");

        Assert.Multiple(() =>
        {
            Assert.That(HtmlElements.Html, Is.EqualTo("html"));
            Assert.That(HtmlElements.Search, Is.EqualTo("search"));
            Assert.That(HtmlElements.TableHeader, Is.EqualTo("th"));
            Assert.That(HtmlElements.TextArea, Is.EqualTo("textarea"));
            Assert.That(HtmlAttributes.HyperlinkReference, Is.EqualTo("href"));
            Assert.That(HtmlAttributes.MinimumLength, Is.EqualTo("minlength"));
            Assert.That(HtmlAttributes.DataPrefix, Is.EqualTo("data-"));
            Assert.That(HtmlInputTypes.Telephone, Is.EqualTo("tel"));
            Assert.That(HtmlInputTypes.LocalDateTime, Is.EqualTo("datetime-local"));
        });
    }

    [Test]
    public void AriaCatalogsExposeCanonicalTokens()
    {
        AssertCatalog(typeof(AriaRoles), "^[a-z]+$");
        AssertCatalog(typeof(AriaAttributes), "^aria-[a-z]+$");
        AssertCatalog(typeof(AriaKeyNames), "^ |[A-Za-z0-9]+$");

        Assert.Multiple(() =>
        {
            Assert.That(AriaRoles.AlertDialog, Is.EqualTo("alertdialog"));
            Assert.That(AriaRoles.MenuItemCheckbox, Is.EqualTo("menuitemcheckbox"));
            Assert.That(AriaRoles.TreeGrid, Is.EqualTo("treegrid"));
            Assert.That(AriaAttributes.ActiveDescendant, Is.EqualTo("aria-activedescendant"));
            Assert.That(AriaAttributes.ErrorMessage, Is.EqualTo("aria-errormessage"));
            Assert.That(AriaAttributes.ValueText, Is.EqualTo("aria-valuetext"));
            Assert.That(AriaKeyNames.Space, Is.EqualTo(" "));
            Assert.That(AriaKeyNames.ArrowDown, Is.EqualTo("ArrowDown"));
        });
    }

    [Test]
    public void CssCatalogsExposeCanonicalTokens()
    {
        AssertCatalog(typeof(CssProperties), "^[a-z][a-z-]*$");
        AssertCatalog(typeof(CssUnits), "^%|[a-z]+$");
        AssertCatalog(typeof(CssMediaFeatures), "^[a-z][a-z-]*$");

        Assert.Multiple(() =>
        {
            Assert.That(CssProperties.Direction, Is.EqualTo("direction"));
            Assert.That(CssProperties.GridTemplateColumns, Is.EqualTo("grid-template-columns"));
            Assert.That(CssProperties.PointerEvents, Is.EqualTo("pointer-events"));
            Assert.That(CssUnits.Pixel, Is.EqualTo("px"));
            Assert.That(CssUnits.Fraction, Is.EqualTo("fr"));
            Assert.That(CssMediaFeatures.PreferredReducedMotion, Is.EqualTo("prefers-reduced-motion"));
            Assert.That(CssMediaFeatures.ForcedColors, Is.EqualTo("forced-colors"));
        });
    }

    [Test]
    public void ProtocolCatalogsExposeCanonicalTokensAndVersion()
    {
        AssertCatalog(typeof(DomEventNames), "^[a-z]+$");
        AssertCatalog(typeof(BridgeProtocolNames), "^[a-z][A-Za-z]*$");

        Assert.Multiple(() =>
        {
            Assert.That(DomEventNames.DoubleClick, Is.EqualTo("dblclick"));
            Assert.That(DomEventNames.BeforeInput, Is.EqualTo("beforeinput"));
            Assert.That(DomEventNames.VisibilityChange, Is.EqualTo("visibilitychange"));
            Assert.That(BridgeProtocolNames.CorrelationId, Is.EqualTo("correlationId"));
            Assert.That(BridgeProtocolNames.BaseRevision, Is.EqualTo("baseRevision"));
            Assert.That(BridgeProtocolNames.HostOrigin, Is.EqualTo("host"));
            Assert.That(BridgeProtocolVersions.MinimumSupported, Is.EqualTo(1));
            Assert.That(BridgeProtocolVersions.Current, Is.EqualTo(1));
        });
    }

    [Test]
    public void AccessibilityHookCatalogExposesStableClassesAndDataAttributes()
    {
        AssertCatalog(typeof(AccessibilityHooks), "^(htmlgui-[a-z-]+|data-htmlgui-[a-z-]+)$");

        Assert.Multiple(() =>
        {
            Assert.That(AccessibilityHooks.ForcedColorsClass, Is.EqualTo("htmlgui-forced-colors"));
            Assert.That(AccessibilityHooks.FocusVisibleClass, Is.EqualTo("htmlgui-focus-visible"));
            Assert.That(AccessibilityHooks.ScreenReaderOnlyClass, Is.EqualTo("htmlgui-screen-reader-only"));
            Assert.That(AccessibilityHooks.StatusTextClass, Is.EqualTo("htmlgui-status-text"));
            Assert.That(AccessibilityHooks.TargetSizeAttribute, Is.EqualTo("data-htmlgui-target-size"));
        });
    }

    private static void AssertCatalog(Type catalogType, string tokenPattern)
    {
        FieldInfo[] fields = catalogType.GetFields(BindingFlags.Public | BindingFlags.Static);
        List<string> values = new List<string>();
        foreach (FieldInfo field in fields)
        {
            string? value = field.GetRawConstantValue() as string;
            Assert.That(value, Is.Not.Null, field.Name);
            string token = value ?? string.Empty;
            Assert.That(Regex.IsMatch(token, tokenPattern), Is.True, field.Name);
            values.Add(token);
        }

        Assert.That(fields, Is.Not.Empty);
        Assert.That(values, Is.Unique);
    }
}
