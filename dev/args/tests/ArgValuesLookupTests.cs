using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValuesLookupTests
{
    [Test]
    public void Has_RecognizesSuppliedOptionByNameAndAlias()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Alias("-c").Value("name");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "-c", "Release" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Has("--configuration"), Is.True);
        Assert.That(values.Has("-c"), Is.True);
        Assert.That(values.Has("--missing"), Is.False);
        Assert.That(values.IsSupplied("--configuration"), Is.True);
        Assert.That(values.IsDefaulted("--configuration"), Is.False);
        Assert.That(values.Source("--configuration"), Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(values.Occurrence("--configuration"), Is.SameAs(parseResult.Occurrences[0]));
    }

    [Test]
    public void Has_RecognizesDefaultedOption()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Default("Debug");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Has("--configuration"), Is.True);
        Assert.That(values.IsSupplied("--configuration"), Is.False);
        Assert.That(values.IsDefaulted("--configuration"), Is.True);
        Assert.That(values.Source("--configuration"), Is.EqualTo(ArgValueSource.Default));
    }

    [Test]
    public void Has_RecognizesSuppliedAndDefaultedArguments()
    {
        ArgCommand suppliedCommand = new ArgCommand("tool");
        suppliedCommand.Argument("project");
        ArgParseResult suppliedResult = new ArgParser(suppliedCommand).Parse(new string[] { "src" });
        ArgValues suppliedValues = new ArgValues(suppliedResult);

        ArgCommand defaultedCommand = new ArgCommand("tool");
        defaultedCommand.Argument("project").Default(".");
        ArgParseResult defaultedResult = new ArgParser(defaultedCommand).Parse(new string[] { });
        ArgValues defaultedValues = new ArgValues(defaultedResult);

        Assert.That(suppliedValues.Has("project"), Is.True);
        Assert.That(suppliedValues.IsSupplied("project"), Is.True);
        Assert.That(suppliedValues.IsDefaulted("project"), Is.False);
        Assert.That(defaultedValues.Has("project"), Is.True);
        Assert.That(defaultedValues.IsSupplied("project"), Is.False);
        Assert.That(defaultedValues.IsDefaulted("project"), Is.True);
    }

    [Test]
    public void Occurrences_ReturnsAllMatchesInParseOrder()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--include").Alias("-I").Value("path").Repeatable();
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--include", "src", "-I", "tests" });
        ArgValues values = new ArgValues(parseResult);

        System.Collections.Generic.List<ArgOccurrence> matches = values.Occurrences("--include");

        Assert.That(matches.Count, Is.EqualTo(2));
        Assert.That(matches[0], Is.SameAs(parseResult.Occurrences[0]));
        Assert.That(matches[1], Is.SameAs(parseResult.Occurrences[1]));
        Assert.That(values.Occurrences("--missing"), Is.Empty);
    }

    [Test]
    public void Has_RecognizesEnvironmentSourcedOptionAndArgument()
    {
        string optionName = "ARGS_TEST_VALUES_OPTION";
        string argumentName = "ARGS_TEST_VALUES_ARGUMENT";
        ArgCommand command = new ArgCommand("tool");
        command.Option("--configuration").Value("name").Env(optionName);
        command.Argument("project").Env(argumentName);
        TestArgEnvironment environment = new TestArgEnvironment();
        environment.Set(optionName, "Release").Set(argumentName, "src");
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = environment;
        ArgParseResult parseResult = ArgParser.WithSettings(command, settings).Parse(new string[] { });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Has("--configuration"), Is.True);
        Assert.That(values.Has("project"), Is.True);
        Assert.That(values.Source("--configuration"), Is.EqualTo(ArgValueSource.Environment));
        Assert.That(values.Source("project"), Is.EqualTo(ArgValueSource.Environment));
        Assert.That(values.String("--configuration"), Is.EqualTo("Release"));
        Assert.That(values.String("project"), Is.EqualTo("src"));
    }

    [Test]
    public void IsSupplied_TreatsPresentFallbackOptionAsSuppliedButNotDefaulted()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--mode").OptionalValue("name").Fallback("auto");
        ArgParseResult parseResult = new ArgParser(command).Parse(new string[] { "--mode" });
        ArgValues values = new ArgValues(parseResult);

        Assert.That(values.Has("--mode"), Is.True);
        Assert.That(values.IsSupplied("--mode"), Is.True);
        Assert.That(values.IsDefaulted("--mode"), Is.False);
        Assert.That(values.Source("--mode"), Is.EqualTo(ArgValueSource.Fallback));
    }

    [Test]
    public void EmptyValues_ReturnsNoMatches()
    {
        ArgValues values = ArgValues.Empty();

        Assert.That(values.Has("--missing"), Is.False);
        Assert.That(values.IsSupplied("--missing"), Is.False);
        Assert.That(values.IsDefaulted("--missing"), Is.False);
        Assert.That(values.Source("--missing"), Is.EqualTo(ArgValueSource.None));
        Assert.That(values.Occurrence("--missing"), Is.Null);
        Assert.That(values.Occurrences("--missing"), Is.Empty);
    }
}
