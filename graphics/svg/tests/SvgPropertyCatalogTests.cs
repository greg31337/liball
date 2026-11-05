using System.Reflection;

namespace svg.Tests;

public class SvgPropertyCatalogTests
{
    [Test]
    public void DefaultCatalogCoversEveryDeclaredInventoryPropertyExactlyOnce()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        string[] declaredNames = typeof(SvgCssPropertyNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] catalogNames = catalog.Entries.Select(entry => entry.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Entries, Has.Count.EqualTo(98));
            Assert.That(catalogNames, Is.EqualTo(declaredNames));
            Assert.That(catalogNames.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(catalogNames.Length));
        });
    }

    [Test]
    public void EveryDefaultEntryHasCompleteMetadataAndPresentationMapping()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Entries.All(entry => entry.InitialValue.Length > 0), Is.True);
            Assert.That(catalog.Entries.All(entry => entry.Grammar.Length > 0), Is.True);
            Assert.That(catalog.Entries.All(entry => entry.IsPresentationAttribute == (entry.PresentationAttributeName.Length > 0)), Is.True);
            Assert.That(catalog.Entries.Where(entry => entry.IsPresentationAttribute).All(entry => entry.PresentationAttributeName == entry.Name), Is.True);
            Assert.That(catalog.Entries.Where(entry => entry.IsPresentationAttribute).All(entry => catalog.FindPresentationAttribute(entry.PresentationAttributeName) == entry), Is.True);
            Assert.That(catalog.FindPresentationAttribute("fill").Name, Is.EqualTo("fill"));
            Assert.That(catalog.FindPresentationAttribute("font-variation-settings"), Is.Null);
            Assert.That(catalog.FindPresentationAttribute("FILL"), Is.Null);
            Assert.That(catalog.FindPresentationAttribute("not-a-property"), Is.Null);
        });
    }

    [Test]
    public void RepresentativePropertiesExposeCascadePercentageAndAnimationMetadata()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgPropertySupportEntry fill = catalog.Find("FILL");
        SvgPropertySupportEntry opacity = catalog.Find("opacity");
        SvgPropertySupportEntry strokeWidth = catalog.Find("stroke-width");
        SvgPropertySupportEntry x = catalog.Find("x");
        SvgPropertySupportEntry colorProfile = catalog.Find("color-profile");

        Assert.Multiple(() =>
        {
            Assert.That(fill.InitialValue, Is.EqualTo("black"));
            Assert.That(fill.Inherited, Is.True);
            Assert.That(fill.ValueKind, Is.EqualTo(SvgPropertyValueSupportKind.Paint));
            Assert.That(fill.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Paint));
            Assert.That(fill.PercentageBasis, Is.EqualTo(SvgPropertyPercentageBasis.None));
            Assert.That(opacity.Inherited, Is.False);
            Assert.That(opacity.PercentageBasis, Is.EqualTo(SvgPropertyPercentageBasis.Number));
            Assert.That(opacity.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Number));
            Assert.That(strokeWidth.PercentageBasis, Is.EqualTo(SvgPropertyPercentageBasis.ViewportSize));
            Assert.That(strokeWidth.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Length));
            Assert.That(x.PercentageBasis, Is.EqualTo(SvgPropertyPercentageBasis.ViewportWidth));
            Assert.That(colorProfile.IsLegacy, Is.True);
            Assert.That(colorProfile.InitialValue, Is.EqualTo("auto"));
        });
    }

    [Test]
    public void ShorthandsDeclareTheirOrderedLonghandsAndAreNotDirectlyAnimatable()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgPropertySupportEntry font = catalog.Find("font");
        SvgPropertySupportEntry marker = catalog.Find("marker");
        SvgPropertySupportEntry overflow = catalog.Find("overflow");
        SvgPropertySupportEntry decoration = catalog.Find("text-decoration");

        Assert.Multiple(() =>
        {
            Assert.That(font.IsShorthand, Is.True);
            Assert.That(font.AnimationType, Is.EqualTo(SvgPropertyAnimationType.NotAnimatable));
            Assert.That(font.Longhands, Is.EqualTo(new[] { "font-style", "font-variant", "font-weight", "font-stretch", "font-size", "font-family" }));
            Assert.That(marker.Longhands, Is.EqualTo(new[] { "marker-start", "marker-mid", "marker-end" }));
            Assert.That(overflow.Longhands, Is.EqualTo(new[] { "overflow-x", "overflow-y" }));
            Assert.That(decoration.Longhands, Is.EqualTo(new[] { "text-decoration-line", "text-decoration-style", "text-decoration-color" }));
            Assert.That(catalog.Entries.Where(entry => !entry.IsShorthand).All(entry => entry.Longhands.Count == 0), Is.True);
        });
    }

    [Test]
    public void ExpandedCatalogParticipatesInSupportsDeclarationEvaluation()
    {
        SvgSupportsEnvironment environment = new SvgSupportsEnvironment();
        SvgCssLimits limits = new SvgCssLimits();

        Assert.Multiple(() =>
        {
            Assert.That(environment.Matches("(fill-rule: evenodd)", limits), Is.True);
            Assert.That(environment.Matches("(fill-rule: winding)", limits), Is.False);
            Assert.That(environment.Matches("(stroke-width: 2.5%)", limits), Is.True);
            Assert.That(environment.Matches("(font-family: 'Example', sans-serif)", limits), Is.True);
            Assert.That(environment.Matches("(width: auto)", limits), Is.True);
            Assert.That(environment.Matches("(glyph-orientation-vertical: auto)", limits), Is.True);
            Assert.That(environment.Matches("(text-rendering: optimizeLegibility)", limits), Is.True);
        });
    }

    [Test]
    public void EveryCatalogInitialValueIsAcceptedByItsSupportGrammar()
    {
        SvgPropertyCatalog catalog = SvgPropertyCatalog.CreateDefault();
        SvgCapabilitySet capabilities = SvgCapabilitySet.Create(new[] { SvgFeatureIdentifiers.CssParsing });

        string[] rejected = catalog.Entries
            .Where(entry => !catalog.Supports(entry.Name, entry.InitialValue, new SvgCssLimits(), capabilities))
            .Select(entry => entry.Name + ":" + entry.InitialValue)
            .ToArray();

        Assert.That(rejected, Is.Empty);
    }

    [Test]
    public void CompleteEntriesValidateMetadataAndCatalogsRemainIndependent()
    {
        SvgPropertySupportEntry entry = SvgPropertySupportEntry.CreateMetadata(
            "host-size",
            SvgPropertyValueSupportKind.Length,
            Array.Empty<string>(),
            "",
            "1px",
            true,
            "<length-percentage>",
            SvgPropertyPercentageBasis.ViewportWidth,
            SvgPropertyAnimationType.Length,
            "host-size",
            false,
            false,
            Array.Empty<string>());
        SvgPropertyCatalog first = SvgPropertyCatalog.CreateDefault();
        SvgPropertyCatalog second = SvgPropertyCatalog.CreateDefault();
        first.Add(entry);

        Assert.Multiple(() =>
        {
            Assert.That(first.Find("HOST-SIZE"), Is.SameAs(entry));
            Assert.That(first.FindPresentationAttribute("host-size"), Is.SameAs(entry));
            Assert.That(second.Find("host-size"), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPropertySupportEntry.CreateMetadata("bad", SvgPropertyValueSupportKind.Any, Array.Empty<string>(), "", "", false, "*", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete, "", false, false, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => SvgPropertySupportEntry.CreateMetadata("bad", SvgPropertyValueSupportKind.Any, Array.Empty<string>(), "", "x", false, "*", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete, "", true, false, Array.Empty<string>()));
        });
    }
}
