using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDefaultValueParserTests
{
    [Test]
    public void Parse_AppliesOptionDefaultWhenOptionIsMissing()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Default("Debug");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertDefaultOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--configuration", new string[] { "Debug" });
        Assert.That(result.Occurrences[0].Option, Is.Not.Null);
    }

    [Test]
    public void Parse_DoesNotApplyOptionDefaultWhenOptionIsSupplied()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Default("Debug");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_AppliesArgumentDefaultWhenArgumentIsMissing()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Default(".");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertDefaultOccurrence(result.Occurrences[0], ArgSymbolKind.Argument, "project", new string[] { "." });
        Assert.That(result.Occurrences[0].Argument, Is.Not.Null);
    }

    [Test]
    public void Parse_DoesNotApplyArgumentDefaultWhenArgumentIsSupplied()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Default(".");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { "src" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src" }));
    }

    [Test]
    public void Parse_AppliesInheritedGlobalOptionDefaultToSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbosity").Value("level").Default("normal");
        ArgCommand build = root.Command("build");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertDefaultOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--verbosity", new string[] { "normal" });
    }

    [Test]
    public void Parse_DoesNotApplyParentLocalOptionDefaultToSubcommand()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Option("--configuration").Value("name").Default("Debug");
        ArgCommand build = root.Command("build");
        build.Option("--local").Flag();
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build", "--local" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--local"));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
    }

    [Test]
    public void Parse_DoesNotApplySiblingGlobalOptionDefaultToResolvedCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.GlobalOption("--build-default").Value("value").Default("build");
        ArgCommand test = root.Command("test");
        test.GlobalOption("--test-default").Value("value").Default("test");
        ArgParser parser = new ArgParser(root);

        ArgParseResult result = parser.Parse(new string[] { "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertDefaultOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--build-default", new string[] { "build" });
    }

    static void AssertDefaultOccurrence(ArgOccurrence occurrence, ArgSymbolKind kind, string symbolName, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(kind));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(""));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(-1));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.Default));
        Assert.That(occurrence.Values, Is.EqualTo(values));
    }
}
