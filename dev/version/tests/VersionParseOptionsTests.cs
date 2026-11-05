using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionParseOptionsTests
{
    [Test]
    public void ParseOptions_DefaultsToDialectSpecificUnknownWithPermissiveMetadata()
    {
        VersionParseOptions options = VersionParseOptions.CreateDefault();

        Assert.That(options.GetDialect(), Is.EqualTo(VersionDialect.Unknown));
        Assert.That(options.GetParseMode(), Is.EqualTo(VersionParseMode.DialectSpecific));
        Assert.That(options.GetMaxInputLength(), Is.EqualTo(4096));
        Assert.That(options.AllowsPrereleaseVersions(), Is.True);
        Assert.That(options.AllowsBuildMetadata(), Is.True);
        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Default));
    }

    [Test]
    public void ParseOptions_CreationPathsSetDialectModeLengthAndMetadataPolicy()
    {
        VersionParseOptions dialectOnly = VersionParseOptions.CreateForDialect(VersionDialect.Python);
        VersionParseOptions strictSemVer = VersionParseOptions.CreateStrict(VersionDialect.SemVer);
        VersionParseOptions full = new VersionParseOptions(
            VersionDialect.NodeSemVer,
            VersionParseMode.Loose,
            128,
            false,
            false,
            PrereleasePolicy.Exclude);

        Assert.That(dialectOnly.GetDialect(), Is.EqualTo(VersionDialect.Python));
        Assert.That(dialectOnly.GetParseMode(), Is.EqualTo(VersionParseMode.DialectSpecific));
        Assert.That(strictSemVer.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(strictSemVer.GetParseMode(), Is.EqualTo(VersionParseMode.Strict));
        Assert.That(full.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(full.GetParseMode(), Is.EqualTo(VersionParseMode.Loose));
        Assert.That(full.GetMaxInputLength(), Is.EqualTo(128));
        Assert.That(full.AllowsPrereleaseVersions(), Is.False);
        Assert.That(full.AllowsBuildMetadata(), Is.False);
        Assert.That(full.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Exclude));
    }

    [Test]
    public void ParseOptions_FactoriesCreateCommonConfigurations()
    {
        VersionParseOptions defaultOptions = VersionParseOptions.CreateDefault();
        VersionParseOptions dialectOptions = VersionParseOptions.CreateForDialect(VersionDialect.RubyGems);
        VersionParseOptions strictOptions = VersionParseOptions.CreateStrict(VersionDialect.SemVer);
        VersionParseOptions looseOptions = VersionParseOptions.CreateLoose(VersionDialect.NodeSemVer);
        VersionParseOptions coerceOptions = VersionParseOptions.CreateCoerce(VersionDialect.SemVer);
        VersionParseOptions lengthOptions = VersionParseOptions.CreateWithMaxInputLength(
            VersionDialect.Python,
            VersionParseMode.DialectSpecific,
            32);
        VersionParseOptions prereleaseOptions = VersionParseOptions.CreateWithPrereleasePolicy(
            VersionDialect.Cargo,
            VersionParseMode.DialectSpecific,
            PrereleasePolicy.Include);

        Assert.That(defaultOptions.GetDialect(), Is.EqualTo(VersionDialect.Unknown));
        Assert.That(dialectOptions.GetDialect(), Is.EqualTo(VersionDialect.RubyGems));
        Assert.That(strictOptions.GetParseMode(), Is.EqualTo(VersionParseMode.Strict));
        Assert.That(looseOptions.GetParseMode(), Is.EqualTo(VersionParseMode.Loose));
        Assert.That(coerceOptions.GetParseMode(), Is.EqualTo(VersionParseMode.Coerce));
        Assert.That(lengthOptions.GetMaxInputLength(), Is.EqualTo(32));
        Assert.That(prereleaseOptions.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Include));
    }

    [Test]
    public void ParseOptions_MutationMethodsExposeExplicitFields()
    {
        VersionParseOptions options = VersionParseOptions.CreateDefault();

        options.SetDialect(VersionDialect.Maven);
        options.SetParseMode(VersionParseMode.Partial);
        options.SetMaxInputLength(64);
        options.SetAllowPrereleaseVersions(false);
        options.SetAllowBuildMetadata(false);
        options.SetPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);

        Assert.That(options.GetDialect(), Is.EqualTo(VersionDialect.Maven));
        Assert.That(options.GetParseMode(), Is.EqualTo(VersionParseMode.Partial));
        Assert.That(options.GetMaxInputLength(), Is.EqualTo(64));
        Assert.That(options.AllowsPrereleaseVersions(), Is.False);
        Assert.That(options.AllowsBuildMetadata(), Is.False);
        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.AutoIfNoStableCandidate));
    }

    [Test]
    public void ParseOptions_RejectsInvalidEnumValuesAndMaxInputLength()
    {
        Assert.That(
            delegate
            {
                VersionParseOptions.CreateForDialect((VersionDialect)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionParseOptions.CreateForDialectAndMode(VersionDialect.SemVer, (VersionParseMode)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionParseOptions.CreateWithMaxInputLength(VersionDialect.SemVer, VersionParseMode.Strict, 0);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionParseOptions.CreateWithPrereleasePolicy(
                    VersionDialect.SemVer,
                    VersionParseMode.Strict,
                    (PrereleasePolicy)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void ParseOptions_PublicMethodsDoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(VersionParseOptions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
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
    public void ParseOptions_DoesNotExposeProperties()
    {
        Assert.That(typeof(VersionParseOptions).GetProperties().Length, Is.EqualTo(0));
        Assert.That(typeof(VersionParseOptions).GetMethods().Any(method => method.Name.StartsWith("get_")), Is.False);
    }
}
