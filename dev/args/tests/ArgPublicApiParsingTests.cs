using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiParsingTests
{
    [Test]
    public void LongOptions_ParseFlagsAndSeparateOrInlineValues()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--verbose").Flag();
        app.Option("--configuration").Alias("--config").Value("name");
        app.Option("--define").Value("pair");
        app.Option("--empty").Value("text");

        ArgParseResult result = app.Parse(new string[]
        {
            "--verbose",
            "--configuration",
            "Debug",
            "--config=Release",
            "--define=name=value",
            "--empty="
        });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(5));
        AssertOption(result.Occurrences[0], "--verbose", "--verbose", 0, new string[] { });
        AssertOption(result.Occurrences[1], "--configuration", "--configuration", 1, new string[] { "Debug" });
        AssertOption(result.Occurrences[2], "--configuration", "--config", 3, new string[] { "Release" });
        AssertOption(result.Occurrences[3], "--define", "--define", 4, new string[] { "name=value" });
        AssertOption(result.Occurrences[4], "--empty", "--empty", 5, new string[] { "" });
        Assert.That(result.Tokens[2].Consumed, Is.True);
    }

    [Test]
    public void ShortOptions_ParseFlagsAndSeparateOrAttachedValues()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--verbose").Alias("-v").Flag();
        app.Option("--configuration").Alias("-c").Value("name");
        app.Option("--number").Alias("-n").Value("number");

        ArgParseResult result = app.Parse(new string[] { "-v", "-cRelease", "-n", "5" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOption(result.Occurrences[0], "--verbose", "-v", 0, new string[] { });
        AssertOption(result.Occurrences[1], "--configuration", "-c", 1, new string[] { "Release" });
        AssertOption(result.Occurrences[2], "--number", "-n", 2, new string[] { "5" });
        Assert.That(result.Tokens[3].Consumed, Is.True);
    }

    [Test]
    public void ShortClusters_ParseFlagsAndFinalAttachedOrSeparateValues()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--all").Alias("-a").Flag();
        app.Option("--brief").Alias("-b").Flag();
        app.Option("--verbose").Alias("-v").Flag();
        app.Option("--number").Alias("-n").Value("number");

        ArgParseResult result = app.Parse(new string[] { "-ab", "-vn5", "-vn", "6" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(6));
        AssertOption(result.Occurrences[0], "--all", "-a", 0, new string[] { });
        AssertOption(result.Occurrences[1], "--brief", "-b", 0, new string[] { });
        AssertOption(result.Occurrences[2], "--verbose", "-v", 1, new string[] { });
        AssertOption(result.Occurrences[3], "--number", "-n", 1, new string[] { "5" });
        AssertOption(result.Occurrences[4], "--verbose", "-v", 2, new string[] { });
        AssertOption(result.Occurrences[5], "--number", "-n", 2, new string[] { "6" });
        Assert.That(result.Tokens[3].Consumed, Is.True);
    }

    [Test]
    public void ShortClusterAndAttachedValueSettings_CanDisableRecognition()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = false;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--all").Alias("-a").Flag();
        app.Option("--configuration").Alias("-c").Value("name");

        ArgParseResult result = app.Parse(new string[] { "-aa", "-cRelease" });

        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(2));
        AssertError(result.Errors[0], "unknown_option", 0, "-aa");
        AssertError(result.Errors[1], "unknown_option", 1, "-cRelease");
    }

    [Test]
    public void SlashOptions_ParseFlagsAndSeparateOrColonValuesWhenEnabled()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("/verbose").Flag();
        app.Option("/configuration").Value("name");
        app.Option("/define").Value("pair");

        ArgParseResult result = app.Parse(new string[] { "/verbose", "/configuration", "Debug", "/define:name:value" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOption(result.Occurrences[0], "/verbose", "/verbose", 0, new string[] { });
        AssertOption(result.Occurrences[1], "/configuration", "/configuration", 1, new string[] { "Debug" });
        AssertOption(result.Occurrences[2], "/define", "/define", 3, new string[] { "name:value" });
    }

    [Test]
    public void SlashOptions_ArePositionalWhenDisabled()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("/verbose").Flag();

        ArgParseResult result = app.Parse(new string[] { "/verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[0] }));
        AssertToken(result.Tokens[0], "/verbose", ArgTokenKind.Positional, 0, false);
    }

    [Test]
    public void EndOfOptionsAndLoneDash_ArePreservedAsPositionalValues()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--verbose").Alias("-v").Flag();
        app.Argument("files").Variadic();

        ArgParseResult result = app.Parse(new string[] { "--verbose", "--", "-", "-v", "--verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertOption(result.Occurrences[0], "--verbose", "--verbose", 0, new string[] { });
        AssertArgument(result.Occurrences[1], "files", 2, new string[] { "-", "-v", "--verbose" });
        AssertToken(result.Tokens[1], "--", ArgTokenKind.EndOfOptions, 1, true);
        AssertToken(result.Tokens[2], "-", ArgTokenKind.Positional, 2, true);
        AssertToken(result.Tokens[3], "-v", ArgTokenKind.Positional, 3, true);
        AssertToken(result.Tokens[4], "--verbose", ArgTokenKind.Positional, 4, true);
    }

    [Test]
    public void Matching_IsSensitiveByDefaultAndCanBeCaseInsensitive()
    {
        IArgApp sensitive = new ArgApp("tool");
        sensitive.Command("Build");
        sensitive.Option("--Verbose").Flag();

        ArgParseResult sensitiveCommand = sensitive.Parse(new string[] { "build" });
        ArgParseResult sensitiveOption = sensitive.Parse(new string[] { "--verbose" });

        Assert.That(sensitiveCommand.Command, Is.SameAs(sensitive.GetRootCommand()));
        Assert.That(sensitiveCommand.Positionals.Count, Is.EqualTo(1));
        Assert.That(sensitiveOption.Occurrences, Is.Empty);
        AssertError(sensitiveOption.Errors[0], "unknown_option", 0, "--verbose");

        ArgParserSettings settings = new ArgParserSettings();
        settings.CommandsCaseInsensitive = true;
        settings.OptionsCaseInsensitive = true;
        settings.EnableSlashOptions = true;
        IArgApp insensitive = new ArgApp("tool").UseSettings(settings);
        ArgCommand build = insensitive.Command("Build").Alias("B");
        build.Option("--Configuration").Alias("-C").Value("name");
        build.Option("/Verbose").Flag();

        ArgParseResult result = insensitive.Parse(new string[] { "b", "--configuration=Debug", "/verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Command, Is.SameAs(build));
        AssertOption(result.Occurrences[0], "--Configuration", "--configuration", 1, new string[] { "Debug" });
        AssertOption(result.Occurrences[1], "/Verbose", "/verbose", 2, new string[] { });
    }

    [Test]
    public void StopAtFirstPositional_ControlsLaterOptionRecognition()
    {
        IArgApp normal = new ArgApp("tool");
        normal.Option("--verbose").Flag();
        ArgParseResult normalResult = normal.Parse(new string[] { "file.txt", "--verbose" });

        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstPositional = true;
        IArgApp stopping = new ArgApp("tool").UseSettings(settings);
        stopping.Option("--verbose").Flag();
        ArgParseResult stoppingResult = stopping.Parse(new string[] { "file.txt", "--verbose" });

        Assert.That(normalResult.Errors, Is.Empty);
        Assert.That(normalResult.Occurrences.Count, Is.EqualTo(1));
        Assert.That(normalResult.Positionals, Is.EqualTo(new ArgToken[] { normalResult.Tokens[0] }));
        Assert.That(stoppingResult.Errors, Is.Empty);
        Assert.That(stoppingResult.Occurrences, Is.Empty);
        Assert.That(stoppingResult.Positionals, Is.EqualTo(new ArgToken[] { stoppingResult.Tokens[0], stoppingResult.Tokens[1] }));
        Assert.That(stoppingResult.Tokens[1].Kind, Is.EqualTo(ArgTokenKind.LongOption));
        Assert.That(stoppingResult.Tokens[1].Consumed, Is.False);
    }

    [Test]
    public void StopAtFirstUnmatched_CollectsTheUnknownTokenAndRemainingInput()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.StopAtFirstUnmatched = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--verbose").Flag();

        ArgParseResult result = app.Parse(new string[] { "--missing", "--verbose", "file.txt" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences, Is.Empty);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0], result.Tokens[1], result.Tokens[2] }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.False);
        Assert.That(result.Tokens[2].Consumed, Is.False);
    }

    [Test]
    public void CollectUnmatched_RetainsUnknownOptionsAndContinuesParsing()
    {
        IArgApp strict = new ArgApp("tool");
        ArgParseResult strictResult = strict.Parse(new string[] { "--missing" });

        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        IArgApp collecting = new ArgApp("tool").UseSettings(settings);
        collecting.Option("--verbose").Alias("-v").Flag();
        ArgParseResult result = collecting.Parse(new string[] { "--missing", "-x", "-v" });

        Assert.That(strictResult.UnmatchedTokens, Is.Empty);
        AssertError(strictResult.Errors[0], "unknown_option", 0, "--missing");
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[0], result.Tokens[1] }));
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        AssertOption(result.Occurrences[0], "--verbose", "-v", 2, new string[] { });
    }

    [Test]
    public void DefaultCommands_ResolveNestedPathsAndYieldToExplicitChildren()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        ArgCommand test = app.Command("test");
        app.GetRootCommand().DefaultCommand("project");
        project.DefaultCommand("build");
        build.Option("--configuration").Value("name");
        build.Argument("target");

        ArgParseResult defaultResult = app.Parse(new string[] { "--configuration", "Release", "src" });
        ArgParseResult explicitResult = app.Parse(new string[] { "test" });

        Assert.That(defaultResult.Errors, Is.Empty);
        Assert.That(defaultResult.Command, Is.SameAs(build));
        Assert.That(defaultResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project, build }));
        AssertOption(defaultResult.Occurrences[0], "--configuration", "--configuration", 0, new string[] { "Release" });
        AssertArgument(defaultResult.Occurrences[1], "target", 2, new string[] { "src" });
        Assert.That(explicitResult.Errors, Is.Empty);
        Assert.That(explicitResult.Command, Is.SameAs(test));
        Assert.That(explicitResult.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), test }));
    }

    [Test]
    public void LocalAndGlobalOptions_RespectTheResolvedCommandScope()
    {
        IArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--verbose").Flag();
        app.Option("--root-only").Flag();
        ArgCommand project = app.Command("project");
        project.GlobalOption("--workspace").Value("path");
        ArgCommand build = project.Command("build");
        build.Option("--configuration").Value("name");

        ArgParseResult result = app.Parse(new string[]
        {
            "project",
            "build",
            "--verbose",
            "--workspace",
            "src",
            "--root-only",
            "--configuration",
            "Release"
        });

        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOption(result.Occurrences[0], "--verbose", "--verbose", 2, new string[] { });
        AssertOption(result.Occurrences[1], "--workspace", "--workspace", 3, new string[] { "src" });
        AssertOption(result.Occurrences[2], "--configuration", "--configuration", 6, new string[] { "Release" });
        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertError(result.Errors[0], "unknown_option", 5, "--root-only");
    }

    [Test]
    public void PositionalArguments_AreAssignedByDeclarationOrderAndLeaveExtras()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--configuration").Value("name");
        app.Argument("source");
        app.Argument("destination");

        ArgParseResult result = app.Parse(new string[] { "--configuration", "Release", "src", "dist", "extra" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(3));
        AssertOption(result.Occurrences[0], "--configuration", "--configuration", 0, new string[] { "Release" });
        AssertArgument(result.Occurrences[1], "source", 2, new string[] { "src" });
        AssertArgument(result.Occurrences[2], "destination", 3, new string[] { "dist" });
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[4] }));
        Assert.That(result.Tokens[4].Consumed, Is.False);
    }

    [Test]
    public void FixedAndBoundedArity_CollectTheConfiguredNumberOfValues()
    {
        IArgApp fixedApp = new ArgApp("tool");
        fixedApp.Option("--point").Arity(3, 3);
        ArgParseResult fixedResult = fixedApp.Parse(new string[] { "--point=10", "20", "30", "extra" });

        IArgApp boundedApp = new ArgApp("tool");
        boundedApp.Option("--include").Arity(1, 2);
        boundedApp.Argument("target").Required();
        ArgParseResult boundedResult = boundedApp.Parse(new string[] { "--include", "src", "tests", "project" });

        Assert.That(fixedResult.Errors, Is.Empty);
        AssertOption(fixedResult.Occurrences[0], "--point", "--point", 0, new string[] { "10", "20", "30" });
        Assert.That(fixedResult.Positionals, Is.EqualTo(new ArgToken[] { fixedResult.Tokens[3] }));
        Assert.That(boundedResult.Errors, Is.Empty);
        AssertOption(boundedResult.Occurrences[0], "--include", "--include", 0, new string[] { "src", "tests" });
        AssertArgument(boundedResult.Occurrences[1], "target", 3, new string[] { "project" });
    }

    [Test]
    public void UnboundedArity_StopsAtOptionsDelimitersAndReservedPositionals()
    {
        IArgApp optionApp = new ArgApp("tool");
        optionApp.Option("--include").Arity(1, ArgArity.Unbounded);
        optionApp.Option("--verbose").Flag();
        ArgParseResult optionResult = optionApp.Parse(new string[] { "--include", "src", "tests", "--verbose" });

        IArgApp positionalApp = new ArgApp("tool");
        positionalApp.Option("--include").Arity(1, ArgArity.Unbounded);
        positionalApp.Argument("source").Required();
        positionalApp.Argument("destination").Required();
        ArgParseResult positionalResult = positionalApp.Parse(new string[] { "--include", "one", "two", "src", "dist" });

        IArgApp delimiterApp = new ArgApp("tool");
        delimiterApp.Option("--include").Arity(1, ArgArity.Unbounded);
        delimiterApp.Argument("files").Variadic();
        ArgParseResult delimiterResult = delimiterApp.Parse(new string[] { "--include", "src", "--", "--literal", "file.txt" });

        Assert.That(optionResult.Errors, Is.Empty);
        AssertOption(optionResult.Occurrences[0], "--include", "--include", 0, new string[] { "src", "tests" });
        AssertOption(optionResult.Occurrences[1], "--verbose", "--verbose", 3, new string[] { });
        Assert.That(positionalResult.Errors, Is.Empty);
        AssertOption(positionalResult.Occurrences[0], "--include", "--include", 0, new string[] { "one", "two" });
        AssertArgument(positionalResult.Occurrences[1], "source", 3, new string[] { "src" });
        AssertArgument(positionalResult.Occurrences[2], "destination", 4, new string[] { "dist" });
        Assert.That(delimiterResult.Errors, Is.Empty);
        AssertOption(delimiterResult.Occurrences[0], "--include", "--include", 0, new string[] { "src" });
        AssertArgument(delimiterResult.Occurrences[1], "files", 3, new string[] { "--literal", "file.txt" });
    }

    [Test]
    public void VariadicArguments_ConsumeAllRemainingPositionalsAfterScalars()
    {
        IArgApp app = new ArgApp("tool");
        app.Argument("source");
        app.Argument("files").Variadic();

        ArgParseResult result = app.Parse(new string[] { "src", "a.txt", "b.txt", "c.txt" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Positionals, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        AssertArgument(result.Occurrences[0], "source", 0, new string[] { "src" });
        AssertArgument(result.Occurrences[1], "files", 1, new string[] { "a.txt", "b.txt", "c.txt" });
    }

    [Test]
    public void RepeatableOptions_PreserveEveryOccurrenceInEncounterOrder()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--include").Alias("-I").Value("path").Repeatable();
        app.Option("--tag").OptionalValue("name").Repeatable();

        ArgParseResult result = app.Parse(new string[]
        {
            "--include",
            "src",
            "--tag=one",
            "-I",
            "tests",
            "--tag",
            "--include=docs"
        });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(5));
        AssertOption(result.Occurrences[0], "--include", "--include", 0, new string[] { "src" });
        AssertOption(result.Occurrences[1], "--tag", "--tag", 2, new string[] { "one" });
        AssertOption(result.Occurrences[2], "--include", "-I", 3, new string[] { "tests" });
        AssertOption(result.Occurrences[3], "--tag", "--tag", 5, new string[] { });
        AssertOption(result.Occurrences[4], "--include", "--include", 6, new string[] { "docs" });
    }

    [Test]
    public void CountOptions_RecordEveryClusteredAndRepeatedOccurrence()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--verbose").Alias("-v").Count();
        app.Option("--quiet").Alias("-q").Flag();

        ArgParseResult result = app.Parse(new string[] { "-vvq", "--verbose" });

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Occurrences.Count, Is.EqualTo(4));
        AssertCount(result.Occurrences[0], "--verbose", "-v", 0);
        AssertCount(result.Occurrences[1], "--verbose", "-v", 0);
        AssertOption(result.Occurrences[2], "--quiet", "-q", 0, new string[] { });
        AssertCount(result.Occurrences[3], "--verbose", "--verbose", 1);
    }

    [Test]
    public void InvalidOptionForms_ReturnStructuredParsingErrors()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--configuration").Value("name");
        app.Option("--verbose").Flag();

        ArgParseResult result = app.Parse(new string[] { "--configuration", "--verbose=true", "--missing" });

        Assert.That(result.Occurrences.Count, Is.EqualTo(2));
        Assert.That(result.Occurrences[0].Values, Is.Empty);
        Assert.That(result.Occurrences[1].Values, Is.Empty);
        Assert.That(result.Errors.Count, Is.EqualTo(3));
        AssertError(result.Errors[0], "missing_value", 0, "--configuration");
        AssertError(result.Errors[1], "unexpected_value", 1, "--verbose");
        AssertError(result.Errors[2], "unknown_option", 2, "--missing");
    }

    static void AssertOption(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(alias));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Values, Is.EqualTo(values));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(occurrence.Option, Is.Not.Null);
        Assert.That(occurrence.Argument, Is.Null);
    }

    static void AssertArgument(ArgOccurrence occurrence, string symbolName, int tokenIndex, string[] values)
    {
        Assert.That(occurrence.SymbolKind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(occurrence.SymbolName, Is.EqualTo(symbolName));
        Assert.That(occurrence.Alias, Is.EqualTo(""));
        Assert.That(occurrence.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(occurrence.Values, Is.EqualTo(values));
        Assert.That(occurrence.Source, Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(occurrence.Option, Is.Null);
        Assert.That(occurrence.Argument, Is.Not.Null);
    }

    static void AssertCount(ArgOccurrence occurrence, string symbolName, string alias, int tokenIndex)
    {
        AssertOption(occurrence, symbolName, alias, tokenIndex, new string[] { });
        Assert.That(occurrence.Count, Is.EqualTo(1));
    }

    static void AssertError(ArgParseError error, string code, int tokenIndex, string symbolName)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
    }

    static void AssertToken(ArgToken token, string rawText, ArgTokenKind kind, int index, bool consumed)
    {
        Assert.That(token.RawText, Is.EqualTo(rawText));
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Index, Is.EqualTo(index));
        Assert.That(token.Consumed, Is.EqualTo(consumed));
    }
}
