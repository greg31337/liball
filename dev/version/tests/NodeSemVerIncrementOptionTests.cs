using System;
using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerIncrementOptionTests
{
    [Test]
    public void IncrementReleaseType_ContainsNodeSemVerIncrementKinds()
    {
        Assert.That(Enum.GetNames(typeof(NodeSemVerIncrementReleaseType)), Is.EqualTo(new string[]
        {
            "Major",
            "Minor",
            "Patch",
            "Premajor",
            "Preminor",
            "Prepatch",
            "Prerelease",
            "Release"
        }));
    }

    [Test]
    public void IncrementOptions_DefaultsToPatchStrictZeroBaseWithoutPrereleaseInclusion()
    {
        NodeSemVerIncrementOptions options = NodeSemVerIncrementOptions.CreateDefault();

        Assert.That(options.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Patch));
        Assert.That(options.HasIdentifier(), Is.False);
        Assert.That(options.GetIdentifier(), Is.EqualTo(""));
        Assert.That(options.IsIdentifierBaseEnabled(), Is.True);
        Assert.That(options.GetIdentifierBase(), Is.EqualTo(0));
        Assert.That(options.IsStrict(), Is.True);
        Assert.That(options.IsLoose(), Is.False);
        Assert.That(options.ShouldIncludePrerelease(), Is.False);
    }

    [Test]
    public void IncrementOptions_FactoriesSetReleaseTypeIdentifierBaseLooseAndPrereleaseFields()
    {
        NodeSemVerIncrementOptions premajor = NodeSemVerIncrementOptions.Create(NodeSemVerIncrementReleaseType.Premajor);
        NodeSemVerIncrementOptions beta = NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prerelease, "beta");
        NodeSemVerIncrementOptions rc = NodeSemVerIncrementOptions.CreateWithIdentifierBase(NodeSemVerIncrementReleaseType.Prepatch, "rc", 1);
        NodeSemVerIncrementOptions loose = NodeSemVerIncrementOptions.CreateLoose(NodeSemVerIncrementReleaseType.Minor);
        NodeSemVerIncrementOptions includePrerelease = NodeSemVerIncrementOptions.CreateWithPrereleaseInclusion(NodeSemVerIncrementReleaseType.Patch, true);

        Assert.That(premajor.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Premajor));
        Assert.That(beta.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Prerelease));
        Assert.That(beta.HasIdentifier(), Is.True);
        Assert.That(beta.GetIdentifier(), Is.EqualTo("beta"));
        Assert.That(rc.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Prepatch));
        Assert.That(rc.GetIdentifier(), Is.EqualTo("rc"));
        Assert.That(rc.GetIdentifierBase(), Is.EqualTo(1));
        Assert.That(loose.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Minor));
        Assert.That(loose.IsLoose(), Is.True);
        Assert.That(includePrerelease.ShouldIncludePrerelease(), Is.True);
    }

    [Test]
    public void IncrementOptions_MutationMethodsExposeExplicitFields()
    {
        NodeSemVerIncrementOptions options = NodeSemVerIncrementOptions.CreateDefault();

        options.SetReleaseType(NodeSemVerIncrementReleaseType.Preminor);
        options.SetIdentifier("alpha-1");
        options.SetIdentifierBase(2);
        options.SetLoose(true);
        options.SetIncludePrerelease(true);

        Assert.That(options.GetReleaseType(), Is.EqualTo(NodeSemVerIncrementReleaseType.Preminor));
        Assert.That(options.HasIdentifier(), Is.True);
        Assert.That(options.GetIdentifier(), Is.EqualTo("alpha-1"));
        Assert.That(options.GetIdentifierBase(), Is.EqualTo(2));
        Assert.That(options.IsIdentifierBaseEnabled(), Is.True);
        Assert.That(options.IsLoose(), Is.True);
        Assert.That(options.ShouldIncludePrerelease(), Is.True);

        options.DisableIdentifierBase();
        options.ClearIdentifier();

        Assert.That(options.IsIdentifierBaseEnabled(), Is.False);
        Assert.That(options.HasIdentifier(), Is.False);
        Assert.That(options.GetIdentifier(), Is.EqualTo(""));
    }

    [Test]
    public void IncrementOptions_RejectInvalidReleaseTypeIdentifierAndIdentifierBase()
    {
        Assert.That(
            delegate
            {
                new NodeSemVerIncrementOptions((NodeSemVerIncrementReleaseType)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prerelease, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prerelease, "bad.id");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NodeSemVerIncrementOptions.CreateWithIdentifierBase(NodeSemVerIncrementReleaseType.Prerelease, "beta", -1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void IncrementOptions_PublicMethodsDoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(NodeSemVerIncrementOptions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
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
}
