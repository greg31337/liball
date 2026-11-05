using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SharedEnumTests
{
    [Test]
    public void VersionDialect_ContainsSupportedDialectNames()
    {
        Assert.That(Enum.GetNames(typeof(VersionDialect)), Is.EqualTo(new string[]
        {
            "Unknown",
            "SemVer",
            "NodeSemVer",
            "NuGet",
            "Maven",
            "Go",
            "Cargo",
            "RubyGems",
            "Python"
        }));
    }

    [Test]
    public void VersionParseMode_ContainsCommonValidationModes()
    {
        Assert.That(Enum.GetNames(typeof(VersionParseMode)), Is.EqualTo(new string[]
        {
            "Strict",
            "Loose",
            "Coerce",
            "Partial",
            "DialectSpecific"
        }));
    }

    [Test]
    public void VersionComparisonMode_SeparatesPrecedenceAndIdentity()
    {
        Assert.That(Enum.GetNames(typeof(VersionComparisonMode)), Is.EqualTo(new string[]
        {
            "Precedence",
            "Identity",
            "NormalizedIdentity",
            "Dialect",
            "StabilityAware"
        }));
    }

    [Test]
    public void PrereleasePolicy_ContainsSelectionPolicies()
    {
        Assert.That(Enum.GetNames(typeof(PrereleasePolicy)), Is.EqualTo(new string[]
        {
            "Default",
            "Include",
            "Exclude",
            "ExplicitOnly",
            "AutoIfNoStableCandidate"
        }));
    }

    [Test]
    public void RangeSyntax_ContainsPlannedRangeDialects()
    {
        Assert.That(Enum.GetNames(typeof(RangeSyntax)), Is.EqualTo(new string[]
        {
            "Unknown",
            "NodeSemVer",
            "Cargo",
            "NuGet",
            "Maven",
            "RubyGems",
            "Python"
        }));
    }

    [Test]
    public void RangeOperator_ContainsPrimitiveComparators()
    {
        Assert.That(Enum.GetNames(typeof(RangeOperator)), Is.EqualTo(new string[]
        {
            "Equal",
            "NotEqual",
            "GreaterThan",
            "GreaterThanOrEqual",
            "LessThan",
            "LessThanOrEqual"
        }));
    }

    [Test]
    public void BoundKind_ContainsIntervalEdgeKinds()
    {
        Assert.That(Enum.GetNames(typeof(BoundKind)), Is.EqualTo(new string[]
        {
            "Unbounded",
            "Inclusive",
            "Exclusive"
        }));
    }

    [Test]
    public void VersionRelation_MapsToComparisonResultSigns()
    {
        Assert.That((int)VersionRelation.LessThan, Is.EqualTo(-1));
        Assert.That((int)VersionRelation.EqualTo, Is.EqualTo(0));
        Assert.That((int)VersionRelation.GreaterThan, Is.EqualTo(1));
    }
}
