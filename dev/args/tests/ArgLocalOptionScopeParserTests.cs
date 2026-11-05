using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgLocalOptionScopeParserTests
{
    [Test]
    public void Parse_RootLocalOptionIsAvailableAtRoot()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--root").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "--root" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--root"));
    }

    [Test]
    public void Parse_ResolvedCommandLocalOptionIsAvailable()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_ResolvedIntermediateCommandLocalOptionIsAvailable()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.Option("--workspace").Value("path");
        project.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "--workspace", "src" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(project));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--workspace"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src" }));
    }

    [Test]
    public void Parse_ParentLocalOptionDoesNotLeakIntoResolvedSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--root").Flag();
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--root", "--local" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--local"));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--root'."));
    }

    [Test]
    public void Parse_ChildLocalOptionDoesNotLeakIntoParentCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--build").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "--build" });

        Assert.That(result.Command, Is.SameAs(project));
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--build'."));
    }

    [Test]
    public void Parse_SiblingLocalOptionDoesNotLeakIntoResolvedSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--build").Flag();
        ArgCommand test = root.Command("test");
        test.Option("--test").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--test", "--build" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--build"));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--test'."));
    }
}
