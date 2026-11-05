using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgEnvironmentValueParserTests
{
    [Test]
    public void Parse_AppliesOptionEnvironmentValueWhenOptionIsMissing()
    {
        string name = "CONFIGURATION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Env(name);
        ArgParser parser = Parser(command, name, "Release");

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertEnvironmentOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--configuration", "Release");
    }

    [Test]
    public void Parse_CommandLineOptionValueWinsOverEnvironmentValue()
    {
        string name = "CONFIGURATION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Env(name);
        ArgParser parser = Parser(command, name, "Debug");

        ArgParseResult result = parser.Parse(new string[] { "--configuration", "Release" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Release" }));
    }

    [Test]
    public void Parse_EnvironmentValueWinsOverDefaultValue()
    {
        string name = "CONFIGURATION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Env(name).Default("Debug");
        ArgParser parser = Parser(command, name, "Release");

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertEnvironmentOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--configuration", "Release");
    }

    [Test]
    public void Parse_DefaultValueAppliesWhenEnvironmentValueIsMissing()
    {
        string name = "CONFIGURATION";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Env(name).Default("Debug");
        ArgParser parser = Parser(command, name, null);

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.Default));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "Debug" }));
    }

    [Test]
    public void Parse_AppliesArgumentEnvironmentValueWhenArgumentIsMissing()
    {
        string name = "PROJECT";
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Env(name);
        ArgParser parser = Parser(command, name, "src");

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertEnvironmentOccurrence(result.Occurrences[0], ArgSymbolKind.Argument, "project", "src");
    }

    [Test]
    public void Parse_CommandLineArgumentValueWinsOverEnvironmentValue()
    {
        string name = "PROJECT";
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Env(name);
        ArgParser parser = Parser(command, name, "env-src");

        ArgParseResult result = parser.Parse(new string[] { "cli-src" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "cli-src" }));
    }

    [Test]
    public void Parse_ArgumentEnvironmentValueWinsOverDefaultValue()
    {
        string name = "PROJECT";
        ArgCommand command = new ArgCommand("tool");
        command.Argument("project").Env(name).Default("default-src");
        ArgParser parser = Parser(command, name, "env-src");

        ArgParseResult result = parser.Parse(new string[] { });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertEnvironmentOccurrence(result.Occurrences[0], ArgSymbolKind.Argument, "project", "env-src");
    }

    [Test]
    public void Parse_AppliesInheritedGlobalOptionEnvironmentValue()
    {
        string name = "VERBOSITY";
        ArgCommand root = new ArgCommand("tool");
        root.GlobalOption("--verbosity").Value("level").Env(name);
        root.Command("build");
        ArgParser parser = Parser(root, name, "normal");

        ArgParseResult result = parser.Parse(new string[] { "build" });

        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertEnvironmentOccurrence(result.Occurrences[0], ArgSymbolKind.Option, "--verbosity", "normal");
    }

    static ArgParser Parser(ArgCommand command, string name, string? value)
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(name, value);
        return ArgParser.WithSettings(command, settings);
    }

    static void AssertEnvironmentOccurrence(ArgOccurrence occurrence, ArgSymbolKind kind, string symbolName, string value)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(kind));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(""));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(-1));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.Environment));
        Assert.That(occurrence.Values, Is.EqualTo(new string[] { value }));
    }
}
