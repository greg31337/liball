using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsRequirementInputTests
{
    [Test]
    public void RequirementList_DefaultAndDefaultPrereleaseMatchRubyGemsDefaults()
    {
        RubyGemsRequirementList defaultRequirement = RubyGemsVersions.CreateDefaultRequirement();
        RubyGemsRequirementList defaultPrerelease = RubyGemsVersions.CreateDefaultPrereleaseRequirement();

        Assert.That(defaultRequirement.IsDefault(), Is.True);
        Assert.That(defaultRequirement.IsSpecific(), Is.False);
        Assert.That(defaultRequirement.ToNormalizedRequirementText(), Is.EqualTo(">= 0"));
        Assert.That(defaultRequirement.AllowsVersionText("1.0.0"), Is.True);
        Assert.That(defaultRequirement.AllowsVersionText("1.0.a"), Is.False);

        Assert.That(defaultPrerelease.IsDefaultPrerelease(), Is.True);
        Assert.That(defaultPrerelease.ToNormalizedRequirementText(), Is.EqualTo(">= 0.a"));
        Assert.That(defaultPrerelease.AllowsVersionText("1.0.a"), Is.True);
    }

    [Test]
    public void RequirementList_FlattensCommaSeparatedRequirementsAndDeduplicatesNormalizedEntries()
    {
        RubyGemsRequirementList requirements = new RubyGemsRequirementList(
            new string[] { ">= 1.0, < 2.0", null!, ">=1.0", "< 2.0" });

        Assert.That(requirements.GetRequirementCount(), Is.EqualTo(2));
        Assert.That(requirements.GetNormalizedRequirementText(0), Is.EqualTo(">= 1.0"));
        Assert.That(requirements.GetNormalizedRequirementText(1), Is.EqualTo("< 2.0"));
        Assert.That(requirements.ToNormalizedRequirementText(), Is.EqualTo(">= 1.0, < 2.0"));
        Assert.That(requirements.AllowsVersionText("1.5"), Is.True);
        Assert.That(requirements.AllowsVersionText("2.0"), Is.False);
    }

    [Test]
    public void RequirementList_FlattensMultipleRequirementArraysAndIgnoresNulls()
    {
        RubyGemsRequirementList requirements = RubyGemsVersions.CreateRequirementList(
            new string?[]?[]
            {
                new string?[] { ">= 1.0", null },
                null,
                new string[] { "!= 1.5", "< 2.0" }
            });

        Assert.That(requirements.GetRequirementCount(), Is.EqualTo(3));
        Assert.That(requirements.ToNormalizedRequirementText(), Is.EqualTo(">= 1.0, != 1.5, < 2.0"));
        Assert.That(requirements.AllowsVersionText("1.4"), Is.True);
        Assert.That(requirements.AllowsVersionText("1.5"), Is.False);
    }

    [Test]
    public void RequirementList_CreatesExactRequirementsFromVersionInputs()
    {
        VersionParseResult version = RubyGemsVersions.Parse("1.2.3");
        RubyGemsRequirementList fromText = RubyGemsRequirementList.CreateFromVersionText("1.2.3");
        RubyGemsRequirementList fromVersion = RubyGemsRequirementList.CreateFromVersion(version.GetVersion());

        Assert.That(fromText.IsExact(), Is.True);
        Assert.That(fromText.IsSpecific(), Is.True);
        Assert.That(fromText.ToNormalizedRequirementText(), Is.EqualTo("= 1.2.3"));
        Assert.That(fromText.AllowsVersionText("1.2.3"), Is.True);
        Assert.That(fromText.AllowsVersionText("1.2.4"), Is.False);
        Assert.That(fromVersion.ToNormalizedRequirementText(), Is.EqualTo("= 1.2.3"));
    }

    [Test]
    public void RequirementList_NormalizesRequirementDisplay()
    {
        RubyGemsRequirementList requirements = RubyGemsVersions.CreateRequirementList(
            new string[] { "~>2.2.0", " !=   2.2.5 ", "1.0" });

        Assert.That(requirements.GetNormalizedRequirementText(0), Is.EqualTo("~> 2.2.0"));
        Assert.That(requirements.GetNormalizedRequirementText(1), Is.EqualTo("!= 2.2.5"));
        Assert.That(requirements.GetNormalizedRequirementText(2), Is.EqualTo("= 1.0"));
        Assert.That(requirements.ToNormalizedRequirementText(), Is.EqualTo("~> 2.2.0, != 2.2.5, = 1.0"));
    }

    [Test]
    public void RequirementList_NullableTextFactoryFallsBackToDefaultRequirement()
    {
        RubyGemsRequirementList requirements = RubyGemsRequirementList.CreateFromNullableText(null);

        Assert.That(requirements.IsDefault(), Is.True);
        Assert.That(requirements.ToNormalizedRequirementText(), Is.EqualTo(">= 0"));
    }

    [Test]
    public void RequirementList_RejectsMalformedRequirementInputs()
    {
        Assert.That(
            delegate
            {
                RubyGemsRequirementList.Create("");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsRequirementList.Create(">= 1.0,");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsRequirementList.CreateFromVersionText("bad");
            },
            Throws.TypeOf<ArgumentException>());
    }
}
