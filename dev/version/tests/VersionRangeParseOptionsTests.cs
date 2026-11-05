using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeParseOptionsTests
{
    [Test]
    public void RangeParseOptions_DefaultsToUnknownSyntaxWithPermissiveDialectSwitches()
    {
        VersionRangeParseOptions options = VersionRangeParseOptions.CreateDefault();

        Assert.That(options.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Unknown));
        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Default));
        Assert.That(options.GetMaxInputLength(), Is.EqualTo(4096));
        Assert.That(options.AllowsWildcardVersions(), Is.True);
        Assert.That(options.AllowsHyphenRanges(), Is.True);
        Assert.That(options.AllowsFloatingVersions(), Is.True);
        Assert.That(options.AllowsMultipleSets(), Is.True);
        Assert.That(options.AllowsUnboundedRanges(), Is.True);
    }

    [Test]
    public void RangeParseOptions_CreationPathsSetSyntaxPolicyLengthAndSwitches()
    {
        VersionRangeParseOptions syntaxOnly = VersionRangeParseOptions.CreateForSyntax(RangeSyntax.Python);
        VersionRangeParseOptions withPolicy = VersionRangeParseOptions.CreateWithPrereleasePolicy(
            RangeSyntax.NodeSemVer,
            PrereleasePolicy.Include);
        VersionRangeParseOptions full = new VersionRangeParseOptions(
            RangeSyntax.NuGet,
            PrereleasePolicy.Exclude,
            128,
            false,
            false,
            false,
            false,
            false);

        Assert.That(syntaxOnly.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Python));
        Assert.That(syntaxOnly.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Default));
        Assert.That(withPolicy.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(withPolicy.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Include));
        Assert.That(full.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet));
        Assert.That(full.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Exclude));
        Assert.That(full.GetMaxInputLength(), Is.EqualTo(128));
        Assert.That(full.AllowsWildcardVersions(), Is.False);
        Assert.That(full.AllowsHyphenRanges(), Is.False);
        Assert.That(full.AllowsFloatingVersions(), Is.False);
        Assert.That(full.AllowsMultipleSets(), Is.False);
        Assert.That(full.AllowsUnboundedRanges(), Is.False);
    }

    [Test]
    public void RangeParseOptions_FactoriesCreateDialectSpecificSyntaxes()
    {
        Assert.That(VersionRangeParseOptions.CreateDefault().GetRangeSyntax(), Is.EqualTo(RangeSyntax.Unknown));
        Assert.That(VersionRangeParseOptions.CreateForSyntax(RangeSyntax.Cargo).GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(VersionRangeParseOptions.CreateNodeSemVer().GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(VersionRangeParseOptions.CreateCargo().GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(VersionRangeParseOptions.CreateNuGet().GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet));
        Assert.That(VersionRangeParseOptions.CreateMaven().GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven));
        Assert.That(VersionRangeParseOptions.CreateRubyGems().GetRangeSyntax(), Is.EqualTo(RangeSyntax.RubyGems));
        Assert.That(VersionRangeParseOptions.CreatePython().GetRangeSyntax(), Is.EqualTo(RangeSyntax.Python));
        Assert.That(
            VersionRangeParseOptions.CreateWithPrereleasePolicy(RangeSyntax.NodeSemVer, PrereleasePolicy.Include).GetPrereleasePolicy(),
            Is.EqualTo(PrereleasePolicy.Include));
        Assert.That(VersionRangeParseOptions.CreateWithMaxInputLength(RangeSyntax.Python, 32).GetMaxInputLength(), Is.EqualTo(32));
    }

    [Test]
    public void RangeParseOptions_MutationMethodsExposeExplicitFields()
    {
        VersionRangeParseOptions options = VersionRangeParseOptions.CreateDefault();

        options.SetRangeSyntax(RangeSyntax.Maven);
        options.SetPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);
        options.SetMaxInputLength(64);
        options.SetAllowWildcardVersions(false);
        options.SetAllowHyphenRanges(false);
        options.SetAllowFloatingVersions(false);
        options.SetAllowMultipleSets(false);
        options.SetAllowUnboundedRanges(false);

        Assert.That(options.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven));
        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.AutoIfNoStableCandidate));
        Assert.That(options.GetMaxInputLength(), Is.EqualTo(64));
        Assert.That(options.AllowsWildcardVersions(), Is.False);
        Assert.That(options.AllowsHyphenRanges(), Is.False);
        Assert.That(options.AllowsFloatingVersions(), Is.False);
        Assert.That(options.AllowsMultipleSets(), Is.False);
        Assert.That(options.AllowsUnboundedRanges(), Is.False);
    }

    [Test]
    public void RangeParseOptions_RejectsInvalidEnumValuesAndMaxInputLength()
    {
        Assert.That(
            delegate
            {
                VersionRangeParseOptions.CreateForSyntax((RangeSyntax)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionRangeParseOptions.CreateForSyntaxAndPrereleasePolicy(
                    RangeSyntax.NodeSemVer,
                    (PrereleasePolicy)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionRangeParseOptions.CreateWithMaxInputLength(RangeSyntax.NodeSemVer, 0);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void RangeParseOptions_PublicMethodsDoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(VersionRangeParseOptions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        int methodIndex = 0;

        while (methodIndex < methods.Length)
        {
            ParameterInfo[] parameters = methods[methodIndex].GetParameters();
            int parameterIndex = 0;

            while (parameterIndex < parameters.Length)
            {
                Assert.That(parameters[parameterIndex].IsOptional, Is.False, methods[methodIndex].Name);
                parameterIndex = parameterIndex + 1;
            }

            methodIndex = methodIndex + 1;
        }
    }

    [Test]
    public void RangeParseOptions_DoesNotExposeProperties()
    {
        Assert.That(typeof(VersionRangeParseOptions).GetProperties().Length, Is.EqualTo(0));
        Assert.That(typeof(VersionRangeParseOptions).GetMethods().Any(method => method.Name.StartsWith("get_")), Is.False);
    }
}
