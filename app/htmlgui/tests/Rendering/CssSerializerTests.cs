using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class CssSerializerTests
{
    [Test]
    public void SerializesDeclarationsInCanonicalOrderWithSafetyAndImportance()
    {
        CssSerializer serializer = new CssSerializer();
        UiCssDeclaration[] declarations = new UiCssDeclaration[]
        {
            Declaration(CssProperties.Display, "block", false, false),
            Declaration("content", "\"</style>\"", false, true),
            Declaration(CssProperties.Color, "red", true, false),
            Declaration("--theme-accent", "#f00", false, true),
            Declaration(CssProperties.Display, "block", false, false)
        };

        string result = serializer.SerializeDeclarations(declarations);

        Assert.That(result, Is.EqualTo("--theme-accent:#f00;color:red!important;content:\"\\3c /style\\3e \";display:block;"));
    }

    [Test]
    public void RejectsConflictingOrUnsafeDeclarations()
    {
        CssSerializer serializer = new CssSerializer();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => serializer.SerializeDeclarations(new UiCssDeclaration[]
            {
                Declaration(CssProperties.Color, "red", false, false),
                Declaration(CssProperties.Color, "blue", false, false)
            }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeDeclarations(new UiCssDeclaration[]
            {
                Declaration(CssProperties.Background, "url(https://example.test/a.png)", false, false)
            }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeDeclarations(new UiCssDeclaration[]
            {
                Declaration("Bad-Property", "red", false, true)
            }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeDeclarations(new UiCssDeclaration[] { null! }));
            Assert.Throws<ArgumentNullException>(() => serializer.SerializeDeclarations(null));
        });
    }

    [Test]
    public void SerializesLayeredMediaSupportsAndContainerRules()
    {
        CssSerializer serializer = new CssSerializer();
        UiCssRule media = Rule(".card:hover", CssProperties.Opacity, ".8");
        media.Layer = "components.controls";
        media.Condition = new UiCssCondition { Kind = "media", Expression = "(prefers-reduced-motion: no-preference)" };
        UiCssRule supports = Rule(".grid", CssProperties.Display, "grid");
        supports.Condition = new UiCssCondition { Kind = "supports", Expression = "(display: grid)" };
        UiCssRule container = Rule(".item", CssProperties.Width, "50%" );
        container.Condition = new UiCssCondition { Kind = "container", ContainerName = "main panel", Expression = "(width >= 40rem)" };

        Assert.Multiple(() =>
        {
            Assert.That(serializer.SerializeRule(media), Is.EqualTo("@layer components.controls{@media (prefers-reduced-motion: no-preference){.card:hover{opacity:.8;}}}"));
            Assert.That(serializer.SerializeRule(supports), Is.EqualTo("@supports (display: grid){.grid{display:grid;}}"));
            Assert.That(serializer.SerializeRule(container), Is.EqualTo("@container main\\20 panel (width >= 40rem){.item{width:50%;}}"));
        });
    }

    [Test]
    public void InternsDeduplicatesAndStablyOrdersStylesheetRules()
    {
        UiCssRule alpha = Rule(".alpha", CssProperties.Display, "block");
        UiCssRule zeta = Rule(".zeta", CssProperties.Color, "red");
        UiCssRule duplicateZeta = Rule(".zeta", CssProperties.Color, "red");
        CssSerializer serializer = new CssSerializer();

        CssSerializationResult first = serializer.SerializeStylesheet(new UiCssRule[] { zeta, alpha, duplicateZeta });
        CssSerializationResult reordered = serializer.SerializeStylesheet(new UiCssRule[] { alpha, duplicateZeta, zeta });

        Assert.Multiple(() =>
        {
            Assert.That(first.Css, Is.EqualTo(".alpha{display:block;}.zeta{color:red;}"));
            Assert.That(first.Css, Is.EqualTo(reordered.Css));
            Assert.That(first.Hash, Is.EqualTo(StableHash.HashText(first.Css)));
            Assert.That(first.Hash, Is.EqualTo(reordered.Hash));
            Assert.That(first.Rules, Has.Length.EqualTo(2));
            Assert.That(first.InputRuleCount, Is.EqualTo(3));
            Assert.That(serializer.GetInternedRuleCount(), Is.EqualTo(2));
            Assert.That(object.ReferenceEquals(first.Rules[0], reordered.Rules[0]), Is.True);
            Assert.That(object.ReferenceEquals(first.Rules[1], reordered.Rules[1]), Is.True);
        });
    }

    [Test]
    public void CreatesStableCustomPropertiesForScalarThemeTokens()
    {
        UiValueEntry[] tokens = new UiValueEntry[]
        {
            Entry("ratio", Floating(1.5)),
            Entry("a.b", Text("red")),
            Entry("enabled", Boolean(true)),
            Entry("count", Integer(4)),
            Entry("a-b", Text("blue")),
            Entry("date", DateTimeValue("2026-09-14")),
            Entry("empty", new UiValue()),
            Entry("space", DecimalValue(2.500m)),
            Entry("enabled", Boolean(true))
        };
        CssSerializer serializer = new CssSerializer();

        UiCssDeclaration[] declarations = serializer.CreateCustomPropertyDeclarations(tokens, "host theme");
        string serialized = serializer.SerializeDeclarations(declarations);
        UiValueEntry[] reorderedTokens = new UiValueEntry[]
        {
            tokens[7], tokens[6], tokens[5], tokens[4], tokens[3], tokens[2], tokens[1], tokens[0]
        };
        string reordered = serializer.SerializeDeclarations(serializer.CreateCustomPropertyDeclarations(reorderedTokens, "host theme"));

        Assert.Multiple(() =>
        {
            Assert.That(declarations, Has.Length.EqualTo(8));
            Assert.That(declarations[0].Property, Does.Match("^--host-theme-a-b-[0-9a-f]{12}$"));
            Assert.That(declarations[1].Property, Does.Match("^--host-theme-a-b-[0-9a-f]{12}$"));
            Assert.That(declarations[0].Property, Is.Not.EqualTo(declarations[1].Property));
            Assert.That(declarations[2].Value, Is.EqualTo("4"));
            Assert.That(declarations[3].Value, Is.EqualTo("\"2026-09-14\""));
            Assert.That(declarations[4].Value, Is.EqualTo("initial"));
            Assert.That(declarations[5].Value, Is.EqualTo("true"));
            Assert.That(declarations[6].Value, Is.EqualTo("1.5"));
            Assert.That(declarations[7].Value, Is.EqualTo("2.5"));
            Assert.That(serialized, Is.EqualTo(reordered));
        });
    }

    [Test]
    public void RejectsMalformedRulesConditionsLayersAndCustomProperties()
    {
        CssSerializer serializer = new CssSerializer();
        UiCssRule invalidSelector = Rule(".card{}body", CssProperties.Color, "red");
        UiCssRule invalidLayer = Rule(".card", CssProperties.Color, "red");
        invalidLayer.Layer = "components..controls";
        UiCssRule invalidCondition = Rule(".card", CssProperties.Color, "red");
        invalidCondition.Condition = new UiCssCondition { Kind = "media", Expression = "(width > 1px){body" };
        UiCssRule namedMedia = Rule(".card", CssProperties.Color, "red");
        namedMedia.Condition = new UiCssCondition { Kind = "media", ContainerName = "main", Expression = "(width > 1px)" };
        UiCssRule unknownCondition = Rule(".card", CssProperties.Color, "red");
        unknownCondition.Condition = new UiCssCondition { Kind = "unknown", Expression = "(width > 1px)" };
        UiCssRule blankContainer = Rule(".card", CssProperties.Color, "red");
        blankContainer.Condition = new UiCssCondition { Kind = "container", ContainerName = " ", Expression = "(width > 1px)" };
        UiValueEntry[] conflictingTokens = new UiValueEntry[]
        {
            Entry("accent", Text("red")),
            Entry("accent", Text("blue"))
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => serializer.SerializeRule(null));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(invalidSelector));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(invalidLayer));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(invalidCondition));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(namedMedia));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(unknownCondition));
            Assert.Throws<ArgumentException>(() => serializer.SerializeRule(blankContainer));
            Assert.Throws<ArgumentNullException>(() => serializer.SerializeStylesheet(null));
            Assert.Throws<ArgumentException>(() => serializer.SerializeStylesheet(new UiCssRule[] { null! }));
            Assert.Throws<ArgumentNullException>(() => serializer.CreateCustomPropertyDeclarations(null, "theme"));
            Assert.Throws<ArgumentException>(() => serializer.CreateCustomPropertyDeclarations(Array.Empty<UiValueEntry>(), " "));
            Assert.Throws<ArgumentException>(() => serializer.CreateCustomPropertyDeclarations(conflictingTokens, "theme"));
            Assert.Throws<ArgumentException>(() => serializer.CreateCustomPropertyDeclarations(new UiValueEntry[] { null! }, "theme"));
            Assert.Throws<ArgumentException>(() => serializer.CreateCustomPropertyDeclarations(new UiValueEntry[] { Entry("aggregate", new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() }) }, "theme"));
        });
    }

    [Test]
    public void EmptyStylesheetsHaveStableEmptyHashAndNoRules()
    {
        CssSerializer serializer = new CssSerializer();

        CssSerializationResult result = serializer.SerializeStylesheet(Array.Empty<UiCssRule>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Css, Is.Empty);
            Assert.That(result.Rules, Is.Empty);
            Assert.That(result.InputRuleCount, Is.Zero);
            Assert.That(result.Hash, Is.EqualTo(StableHash.HashText(string.Empty)));
        });
    }

    private static UiCssRule Rule(string selector, string property, string value)
    {
        return new UiCssRule
        {
            Selector = selector,
            Declarations = new UiCssDeclaration[] { Declaration(property, value, false, false) }
        };
    }

    private static UiCssDeclaration Declaration(string property, string value, bool important, bool trusted)
    {
        return new UiCssDeclaration { Property = property, Value = value, Important = important, IsTrusted = trusted };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Boolean(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Floating(double value)
    {
        return new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = value };
    }

    private static UiValue DecimalValue(decimal value)
    {
        return new UiValue { Kind = UiValueKind.Decimal, DecimalValue = value };
    }

    private static UiValue DateTimeValue(string value)
    {
        return new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = value };
    }
}
