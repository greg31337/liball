using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValueSourceTrackingTests
{
    [Test]
    public void Parse_TracksCommandLineDefaultEnvironmentFallbackAndImpliedOptionSources()
    {
        string envName = "ARGS_TEST_VALUE_SOURCE";
        ArgCommand command = new ArgCommand("tool");
        ArgOption commandLine = command.Option("--cli").Value("value");
        ArgOption defaulted = command.Option("--defaulted").Value("value").Default("default-value");
        ArgOption environment = command.Option("--env").Value("value").Env(envName);
        ArgOption fallback = command.Option("--fallback").OptionalValue("value").Fallback("fallback-value");
        ArgOption implied = command.Option("--implied").Value("value");
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = new TestArgEnvironment(envName, "env-value");
        ArgParser parser = ArgParser.WithSettings(command, settings);

        ArgParseResult result = parser.Parse(new string[] { "--cli", "cli-value", "--fallback" });
        result.AddImpliedOption(implied, "implied-value");

        Assert.That(result.HasErrors(), Is.False);
        AssertSource(result.OptionOccurrences("--cli")[0], commandLine, ArgValueSource.CommandLine, new string[] { "cli-value" });
        AssertSource(result.OptionOccurrences("--defaulted")[0], defaulted, ArgValueSource.Default, new string[] { "default-value" });
        AssertSource(result.OptionOccurrences("--env")[0], environment, ArgValueSource.Environment, new string[] { "env-value" });
        AssertSource(result.OptionOccurrences("--fallback")[0], fallback, ArgValueSource.Fallback, new string[] { "fallback-value" });
        AssertSource(result.OptionOccurrences("--implied")[0], implied, ArgValueSource.Implied, new string[] { "implied-value" });
    }

    [Test]
    public void AddImpliedArgument_TracksImpliedSource()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgArgument argument = command.Argument("project");
        ArgParser parser = new ArgParser(command);

        ArgParseResult result = parser.Parse(new string[] { });
        ArgOccurrence occurrence = result.AddImpliedArgument(argument, "src");

        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(occurrence.SymbolName, Is.EqualTo("project"));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(-1));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.Implied));
        Assert.That(occurrence.Values, Is.EqualTo(new string[] { "src" }));
        Assert.That(result.Occurrences, Is.EqualTo(new ArgOccurrence[] { occurrence }));
    }

    [Test]
    public void NewOccurrence_SourceStartsAsNoneUntilAssigned()
    {
        ArgOccurrence occurrence = new ArgOccurrence();

        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.None));
    }

    static void AssertSource(ArgOccurrence occurrence, ArgOption option, ArgValueSource source, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(occurrence.Option, Is.SameAs(option));
        Assert.That(occurrence.Source, Is.EqualTo(source));
        Assert.That(occurrence.Values, Is.EqualTo(values));
    }
}
