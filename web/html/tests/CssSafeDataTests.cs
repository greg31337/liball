
namespace UniversalHtml.Tests;

[TestFixture]
public class CssSafeDataTests
{
    [Test]
    public void PropertyProfilesAreConservativeAndCaseInsensitive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.GetPropertyProfile("COLOR"),
                Is.EqualTo(CssSafePropertyProfile.Color));
            Assert.That(CssSafeData.GetPropertyProfile("border-color"),
                Is.EqualTo(CssSafePropertyProfile.ColorList));
            Assert.That(CssSafeData.GetPropertyProfile("margin"),
                Is.EqualTo(CssSafePropertyProfile.MarginList));
            Assert.That(CssSafeData.GetPropertyProfile("width"),
                Is.EqualTo(CssSafePropertyProfile.Size));
            Assert.That(CssSafeData.GetPropertyProfile("background-image"),
                Is.EqualTo(CssSafePropertyProfile.UrlImage));
            Assert.That(CssSafeData.GetPropertyProfile("list-style-image"),
                Is.EqualTo(CssSafePropertyProfile.UrlImage));
            Assert.That(CssSafeData.GetPropertyProfile("position"),
                Is.EqualTo(CssSafePropertyProfile.Unknown));
            Assert.That(CssSafeData.GetPropertyProfile("--custom"),
                Is.EqualTo(CssSafePropertyProfile.Unknown));
        });
    }

    [Test]
    public void FunctionProfilesKeepUrlsSeparateFromPassiveFunctions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.GetFunctionProfile("CALC"),
                Is.EqualTo(CssSafeFunctionProfile.Math));
            Assert.That(CssSafeData.GetFunctionProfile("rgb"),
                Is.EqualTo(CssSafeFunctionProfile.Color));
            Assert.That(CssSafeData.GetFunctionProfile("var"),
                Is.EqualTo(CssSafeFunctionProfile.Variable));
            Assert.That(CssSafeData.GetFunctionProfile("URL"),
                Is.EqualTo(CssSafeFunctionProfile.Url));
            Assert.That(CssSafeData.GetFunctionProfile("expression"),
                Is.EqualTo(CssSafeFunctionProfile.Unknown));
            Assert.That(CssSafeData.GetFunctionProfile("image-set"),
                Is.EqualTo(CssSafeFunctionProfile.Unknown));
        });
    }

    [Test]
    public void KeywordsUseAnAllowlistInsteadOfAcceptingArbitraryIdentifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.IsKeyword("INHERIT"), Is.True);
            Assert.That(CssSafeData.IsKeyword("revert-layer"), Is.True);
            Assert.That(CssSafeData.IsKeyword("inline-block"), Is.True);
            Assert.That(CssSafeData.IsKeyword("break-spaces"), Is.True);
            Assert.That(CssSafeData.IsKeyword("expression"), Is.False);
            Assert.That(CssSafeData.IsKeyword("made-up-value"), Is.False);
        });
    }

    [Test]
    public void UnitsAreRestrictedToLengthsAndAnglesNeededByProfiles()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.GetUnitKind("PX"),
                Is.EqualTo(CssSafeUnitKind.Length));
            Assert.That(CssSafeData.GetUnitKind("dvh"),
                Is.EqualTo(CssSafeUnitKind.Length));
            Assert.That(CssSafeData.GetUnitKind("TURN"),
                Is.EqualTo(CssSafeUnitKind.Angle));
            Assert.That(CssSafeData.GetUnitKind("ms"),
                Is.EqualTo(CssSafeUnitKind.Unknown));
            Assert.That(CssSafeData.GetUnitKind("fr"),
                Is.EqualTo(CssSafeUnitKind.Unknown));
            Assert.That(CssSafeData.GetUnitKind("cqw"),
                Is.EqualTo(CssSafeUnitKind.Unknown));
        });
    }

    [Test]
    public void ColorTableContainsStandardNamesButNotSystemColors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.IsColorKeyword("RebeccaPurple"), Is.True);
            Assert.That(CssSafeData.IsColorKeyword("TRANSPARENT"), Is.True);
            Assert.That(CssSafeData.IsColorKeyword("currentColor"), Is.True);
            Assert.That(CssSafeData.IsColorKeyword("canvas"), Is.False);
            Assert.That(CssSafeData.IsColorKeyword("linktext"), Is.False);
            Assert.That(CssSafeData.IsColorKeyword("not-a-color"), Is.False);
        });
    }

    [Test]
    public void SecuritySensitiveExamplesRemainExplicitlyExcluded()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CssSafeData.IsExplicitlyExcludedProperty("behavior"),
                Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedProperty("-MOZ-BINDING"),
                Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedProperty("position"),
                Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedFunction("expression"),
                Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedFunction("ATTR"),
                Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedUnit("dpi"), Is.True);
            Assert.That(CssSafeData.IsExplicitlyExcludedUnit("px"), Is.False);
        });
    }

    [Test]
    public void GeneratedSnapshotsAreSortedCompleteAndDefensive()
    {
        string[] properties = CssSafeData.GetPropertyNames();
        string[] functions = CssSafeData.GetFunctionNames();
        string[] keywords = CssSafeData.GetKeywords();
        string[] units = CssSafeData.GetUnitNames();
        string[] colors = CssSafeData.GetColorNames();

        Assert.Multiple(() =>
        {
            Assert.That(properties, Has.Length.EqualTo(60));
            Assert.That(functions, Has.Length.EqualTo(11));
            Assert.That(keywords, Has.Length.EqualTo(110));
            Assert.That(units, Has.Length.EqualTo(47));
            Assert.That(colors, Has.Length.EqualTo(150));
            Assert.That(properties,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(functions,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(keywords,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(units,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(colors,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(properties.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(properties.Length));
            Assert.That(colors.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(colors.Length));
        });

        properties[0] = "changed";
        Assert.That(CssSafeData.GetPropertyNames()[0],
            Is.EqualTo("background-color"));
    }

    [Test]
    public void SecurityRationalesAreExposedAsDefensiveSnapshots()
    {
        string[] rationales = CssSafeData.GetSecurityRationales();

        Assert.Multiple(() =>
        {
            Assert.That(rationales, Has.Length.EqualTo(6));
            Assert.That(rationales, Has.Some.Contains("Allowlist-only"));
            Assert.That(rationales, Has.Some.Contains("URL-policy approval"));
            Assert.That(rationales, Has.Some.Contains("positioning"));
        });

        rationales[0] = "changed";
        Assert.That(CssSafeData.GetSecurityRationales()[0],
            Does.StartWith("Allowlist-only"));
    }
}
