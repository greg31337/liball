using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgGlobalOptionInheritanceParserTests
{
    [Test]
    public void Parse_RootGlobalOptionIsAvailableAtRoot()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "--verbose" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[0].Option, Is.SameAs(root.GlobalOptions[0]));
    }

    [Test]
    public void Parse_RootGlobalOptionIsAvailableToNestedSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Flag();
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--configuration").Value("name");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "build", "--verbose", "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[1].SymbolName, Is.EqualTo("--configuration"));
        Assert.That(result.Occurrences[1].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_RootGlobalOptionAliasIsAvailableToNestedSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbose").Alias("-v").Flag();
        ArgCommand build = root.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "-v" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--verbose"));
        Assert.That(result.Occurrences[0].Alias, Is.EqualTo("-v"));
        Assert.That(result.Occurrences[0].Option, Is.SameAs(root.GlobalOptions[0]));
    }

    [Test]
    public void Parse_IntermediateGlobalOptionIsAvailableToDescendant()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.GlobalOption("--workspace").Value("path");
        ArgCommand build = project.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "build", "--workspace", "src" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--workspace"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src" }));
    }

    [Test]
    public void Parse_DescendantGlobalOptionDoesNotLeakIntoAncestorCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        project.GlobalOption("--workspace").Value("path");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "--workspace", "src" });

        Assert.That(result.Command, Is.SameAs(root));
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--workspace'."));
        Assert.That(result.Tokens[1].Consumed, Is.False);
    }

    [Test]
    public void Parse_SiblingGlobalOptionDoesNotLeakIntoResolvedCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.GlobalOption("--build-global").Flag();
        ArgCommand test = root.Command("test");
        test.GlobalOption("--test-global").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--test-global", "--build-global" });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--build-global"));
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("Unknown option '--test-global'."));
    }

    [Test]
    public void Parse_ResolvedLocalOptionTakesPrecedenceOverInheritedGlobalOption()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--global-mode").Alias("--mode").Value("name");
        ArgCommand build = root.Command("build");
        build.Option("--local-mode").Alias("--mode").Value("name");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--mode", "fast" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--local-mode"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "fast" }));
    }

    [Test]
    public void Parse_NearestGlobalOptionTakesPrecedenceOverAncestorGlobalOption()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--root-mode").Alias("--mode").Value("name");
        ArgCommand project = root.Command("project");
        project.GlobalOption("--project-mode").Alias("--mode").Value("name");
        ArgCommand build = project.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "project", "build", "--mode", "fast" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--project-mode"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "fast" }));
    }
}
