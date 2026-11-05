using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerAccessorTests
{
    [Test]
    public void Accessors_ReturnNormalizedCoreComponentText()
    {
        Assert.That(NodeSemVer.GetMajor("12345678901234567890.2.3"), Is.EqualTo("12345678901234567890"));
        Assert.That(NodeSemVer.GetMinor("1.2345678901234567890.3"), Is.EqualTo("2345678901234567890"));
        Assert.That(NodeSemVer.GetPatch("1.2.345678901234567890"), Is.EqualTo("345678901234567890"));
    }

    [Test]
    public void Accessors_ReturnPrereleaseIdentifiersAndText()
    {
        string[] identifiers = NodeSemVer.GetPrerelease("1.2.3-alpha.1");

        Assert.That(identifiers, Is.EqualTo(new string[] { "alpha", "1" }));
        Assert.That(NodeSemVer.GetPrereleaseText("1.2.3-alpha.1"), Is.EqualTo("alpha.1"));
    }

    [Test]
    public void Accessors_ReturnBuildMetadataIdentifiersAndText()
    {
        string[] identifiers = NodeSemVer.GetBuildMetadata("1.2.3+build.05");

        Assert.That(identifiers, Is.EqualTo(new string[] { "build", "05" }));
        Assert.That(NodeSemVer.GetBuildMetadataText("1.2.3+build.05"), Is.EqualTo("build.05"));
    }

    [Test]
    public void Accessors_ReturnEmptyOrNullWhenSectionIsAbsent()
    {
        Assert.That(NodeSemVer.GetPrerelease("1.2.3"), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetPrereleaseText("1.2.3"), Is.Null);
        Assert.That(NodeSemVer.GetBuildMetadata("1.2.3"), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetBuildMetadataText("1.2.3"), Is.Null);
    }

    [Test]
    public void Accessors_ReturnNullOrEmptyForInvalidInputs()
    {
        Assert.That(NodeSemVer.GetMajor("v1.2.3"), Is.Null);
        Assert.That(NodeSemVer.GetMinor("1.2"), Is.Null);
        Assert.That(NodeSemVer.GetPatch(null), Is.Null);
        Assert.That(NodeSemVer.GetPrerelease("1.2"), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetPrereleaseText("1.2"), Is.Null);
        Assert.That(NodeSemVer.GetBuildMetadata("1.2"), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetBuildMetadataText("1.2"), Is.Null);
    }

    [Test]
    public void AccessorArrays_AreDefensiveCopies()
    {
        string[] prerelease = NodeSemVer.GetPrerelease("1.2.3-alpha.1");
        string[] build = NodeSemVer.GetBuildMetadata("1.2.3+build.1");

        prerelease[0] = "changed";
        build[0] = "changed";

        Assert.That(NodeSemVer.GetPrerelease("1.2.3-alpha.1"), Is.EqualTo(new string[] { "alpha", "1" }));
        Assert.That(NodeSemVer.GetBuildMetadata("1.2.3+build.1"), Is.EqualTo(new string[] { "build", "1" }));
    }

    [Test]
    public void AccessorFacadeMethods_DoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(NodeSemVer).GetMethods(BindingFlags.Public | BindingFlags.Static);
        int methodIndex = 0;

        while (methodIndex < methods.Length)
        {
            if (methods[methodIndex].Name == "GetMajor"
                || methods[methodIndex].Name == "GetMinor"
                || methods[methodIndex].Name == "GetPatch"
                || methods[methodIndex].Name == "GetPrerelease"
                || methods[methodIndex].Name == "GetPrereleaseText"
                || methods[methodIndex].Name == "GetBuildMetadata"
                || methods[methodIndex].Name == "GetBuildMetadataText")
            {
                ParameterInfo[] parameters = methods[methodIndex].GetParameters();
                int parameterIndex = 0;
                while (parameterIndex < parameters.Length)
                {
                    Assert.That(parameters[parameterIndex].IsOptional, Is.False, methods[methodIndex].Name);
                    parameterIndex = parameterIndex + 1;
                }
            }

            methodIndex = methodIndex + 1;
        }
    }
}
