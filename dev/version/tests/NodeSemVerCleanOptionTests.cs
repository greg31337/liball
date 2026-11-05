using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerCleanOptionTests
{
    [Test]
    public void CleanStrictAliasesMatchDefaultCleanBehavior()
    {
        Assert.That(NodeSemVer.CleanStrict(" = v1.2.3 "), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.CleanStrictResult(" = v1.2.3 ").GetVersionText(), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.CleanStrict("v01.2.3"), Is.Null);
    }

    [Test]
    public void CleanOptions_SelectStrictOrLooseModeExplicitly()
    {
        NodeSemVerCleanOptions strictOptions = NodeSemVerCleanOptions.CreateStrict();
        NodeSemVerCleanOptions looseOptions = NodeSemVerCleanOptions.CreateLoose();

        Assert.That(NodeSemVer.Clean(" = v01.002.0003 ", strictOptions), Is.Null);
        Assert.That(NodeSemVer.Clean(" = v01.002.0003 ", looseOptions), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void CleanResultOptions_SelectStrictOrLooseModeExplicitly()
    {
        NodeSemVerCleanOptions strictOptions = new NodeSemVerCleanOptions(false);
        NodeSemVerCleanOptions looseOptions = new NodeSemVerCleanOptions(true);

        NodeSemVerValidResult strictResult = NodeSemVer.CleanResult(" = v01.002.0003 ", strictOptions);
        NodeSemVerValidResult looseResult = NodeSemVer.CleanResult(" = v01.002.0003 ", looseOptions);

        Assert.That(strictResult.IsInvalid(), Is.True);
        Assert.That(strictResult.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(looseResult.IsValid(), Is.True);
        Assert.That(looseResult.GetVersionText(), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void CleanOptions_FactoryAndMutationExposeExplicitMode()
    {
        NodeSemVerCleanOptions options = NodeSemVerCleanOptions.CreateWithLooseMode(false);

        Assert.That(options.IsStrict(), Is.True);
        Assert.That(options.IsLoose(), Is.False);

        options.SetLoose(true);

        Assert.That(options.IsStrict(), Is.False);
        Assert.That(options.IsLoose(), Is.True);
    }

    [Test]
    public void CleanOptions_NullOptionsAreRejectedByOptionOverloads()
    {
        NodeSemVerCleanOptions? options = null;

        Assert.That(
            delegate
            {
                NodeSemVer.Clean("1.2.3", options!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                NodeSemVer.CleanResult("1.2.3", options!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    [Test]
    public void CleanFacadeMethods_DoNotUseOptionalParameters()
    {
        MethodInfo[] methods = typeof(NodeSemVer).GetMethods(BindingFlags.Public | BindingFlags.Static);
        int methodIndex = 0;

        while (methodIndex < methods.Length)
        {
            if (methods[methodIndex].Name.StartsWith("Clean"))
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
