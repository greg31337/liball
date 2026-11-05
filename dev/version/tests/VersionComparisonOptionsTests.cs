using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionComparisonOptionsTests
{
    [Test]
    public void ComparisonOptions_DefaultsToDialectModeCaseSensitivePrecedenceStyleComparison()
    {
        VersionComparisonOptions options = VersionComparisonOptions.CreateDefault();

        Assert.That(options.GetDialect(), Is.EqualTo(VersionDialect.Unknown));
        Assert.That(options.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Dialect));
        Assert.That(options.IsCaseSensitive(), Is.True);
        Assert.That(options.IncludesBuildMetadata(), Is.False);
        Assert.That(options.GetInvalidVersionOrdering(), Is.EqualTo(InvalidVersionOrdering.Reject));
    }

    [Test]
    public void ComparisonOptions_CreationPathsSetDialectModeCaseBuildAndInvalidOrdering()
    {
        VersionComparisonOptions dialectOnly = VersionComparisonOptions.CreateForDialect(VersionDialect.Python);
        VersionComparisonOptions withMode = VersionComparisonOptions.CreateWithMode(
            VersionDialect.SemVer,
            VersionComparisonMode.Identity);
        VersionComparisonOptions full = new VersionComparisonOptions(
            VersionDialect.NodeSemVer,
            VersionComparisonMode.NormalizedIdentity,
            false,
            true,
            InvalidVersionOrdering.SortAfterValid);

        Assert.That(dialectOnly.GetDialect(), Is.EqualTo(VersionDialect.Python));
        Assert.That(dialectOnly.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Dialect));
        Assert.That(withMode.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(withMode.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Identity));
        Assert.That(full.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(full.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.NormalizedIdentity));
        Assert.That(full.IsCaseSensitive(), Is.False);
        Assert.That(full.IncludesBuildMetadata(), Is.True);
        Assert.That(full.GetInvalidVersionOrdering(), Is.EqualTo(InvalidVersionOrdering.SortAfterValid));
    }

    [Test]
    public void ComparisonOptions_FactoriesCreateCommonConfigurations()
    {
        VersionComparisonOptions defaultOptions = VersionComparisonOptions.CreateDefault();
        VersionComparisonOptions dialectOptions = VersionComparisonOptions.CreateForDialect(VersionDialect.RubyGems);
        VersionComparisonOptions modeOptions = VersionComparisonOptions.CreateWithMode(
            VersionDialect.Maven,
            VersionComparisonMode.Dialect);
        VersionComparisonOptions precedenceOptions = VersionComparisonOptions.CreatePrecedence(VersionDialect.SemVer);
        VersionComparisonOptions identityOptions = VersionComparisonOptions.CreateIdentity(VersionDialect.NuGet);
        VersionComparisonOptions normalizedOptions = VersionComparisonOptions.CreateNormalizedIdentity(VersionDialect.NuGet);
        VersionComparisonOptions dialectModeOptions = VersionComparisonOptions.CreateDialect(VersionDialect.Cargo);
        VersionComparisonOptions stabilityOptions = VersionComparisonOptions.CreateStabilityAware(VersionDialect.Python);
        VersionComparisonOptions caseOptions = VersionComparisonOptions.CreateCaseInsensitive(
            VersionDialect.Maven,
            VersionComparisonMode.Precedence);
        VersionComparisonOptions buildOptions = VersionComparisonOptions.CreateIncludingBuildMetadata(
            VersionDialect.SemVer,
            VersionComparisonMode.Identity);
        VersionComparisonOptions invalidOptions = VersionComparisonOptions.CreateWithInvalidVersionOrdering(
            VersionDialect.Go,
            VersionComparisonMode.Dialect,
            InvalidVersionOrdering.SortBeforeValid);

        Assert.That(defaultOptions.GetDialect(), Is.EqualTo(VersionDialect.Unknown));
        Assert.That(dialectOptions.GetDialect(), Is.EqualTo(VersionDialect.RubyGems));
        Assert.That(modeOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Dialect));
        Assert.That(precedenceOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Precedence));
        Assert.That(identityOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Identity));
        Assert.That(normalizedOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.NormalizedIdentity));
        Assert.That(dialectModeOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Dialect));
        Assert.That(stabilityOptions.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.StabilityAware));
        Assert.That(caseOptions.IsCaseSensitive(), Is.False);
        Assert.That(buildOptions.IncludesBuildMetadata(), Is.True);
        Assert.That(invalidOptions.GetInvalidVersionOrdering(), Is.EqualTo(InvalidVersionOrdering.SortBeforeValid));
    }

    [Test]
    public void ComparisonOptions_MutationMethodsExposeExplicitFields()
    {
        VersionComparisonOptions options = VersionComparisonOptions.CreateDefault();

        options.SetDialect(VersionDialect.Maven);
        options.SetComparisonMode(VersionComparisonMode.StabilityAware);
        options.SetCaseSensitive(false);
        options.SetIncludeBuildMetadata(true);
        options.SetInvalidVersionOrdering(InvalidVersionOrdering.TreatAsEqual);

        Assert.That(options.GetDialect(), Is.EqualTo(VersionDialect.Maven));
        Assert.That(options.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.StabilityAware));
        Assert.That(options.IsCaseSensitive(), Is.False);
        Assert.That(options.IncludesBuildMetadata(), Is.True);
        Assert.That(options.GetInvalidVersionOrdering(), Is.EqualTo(InvalidVersionOrdering.TreatAsEqual));
    }

    [Test]
    public void ComparisonOptions_RejectsInvalidEnumValues()
    {
        Assert.That(
            delegate
            {
                VersionComparisonOptions.CreateForDialect((VersionDialect)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionComparisonOptions.CreateWithMode(VersionDialect.SemVer, (VersionComparisonMode)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new VersionComparisonOptions(
                    VersionDialect.SemVer,
                    VersionComparisonMode.Precedence,
                    true,
                    false,
                    (InvalidVersionOrdering)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                VersionComparisonOptions.CreateWithInvalidVersionOrdering(
                    VersionDialect.SemVer,
                    VersionComparisonMode.Precedence,
                    (InvalidVersionOrdering)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void InvalidVersionOrdering_ExposesStableOrderingChoices()
    {
        Assert.That((int)InvalidVersionOrdering.Reject, Is.EqualTo(0));
        Assert.That((int)InvalidVersionOrdering.SortBeforeValid, Is.EqualTo(1));
        Assert.That((int)InvalidVersionOrdering.SortAfterValid, Is.EqualTo(2));
        Assert.That((int)InvalidVersionOrdering.TreatAsEqual, Is.EqualTo(3));
    }

    [Test]
    public void ComparisonOptions_PublicMethodsDoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(VersionComparisonOptions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
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
    public void ComparisonOptions_DoesNotExposeProperties()
    {
        Assert.That(typeof(VersionComparisonOptions).GetProperties().Length, Is.EqualTo(0));
        Assert.That(typeof(VersionComparisonOptions).GetMethods().Any(method => method.Name.StartsWith("get_")), Is.False);
    }
}
