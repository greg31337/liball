using NUnit.Framework;

namespace args.Tests;

public enum ArgPublicApiBindingMode
{
    Debug,
    Release
}

[TestFixture]
public class ArgPublicApiBindingTests
{
    [Test]
    public void Resolution_ExposesCommandPathOccurrencesAndRemainingTokens()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CollectUnmatched = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        ArgCommand project = app.Command("project");
        ArgCommand build = project.Command("build");
        build.Option("--include").Value("path");

        ArgParseResult result = app.Parse(new string[] { "project", "build", "--include", "src", "extra", "--unknown" });
        IArgValues values = new ArgValues(result);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Command, Is.SameAs(build));
        Assert.That(result.CommandPath, Is.EqualTo(new ArgCommand[] { app.GetRootCommand(), project, build }));
        Assert.That(result.Tokens[0].Consumed, Is.True);
        Assert.That(result.Tokens[1].Consumed, Is.True);
        Assert.That(result.Occurrences.Count, Is.EqualTo(1));
        Assert.That(result.Occurrences[0].SymbolName, Is.EqualTo("--include"));
        Assert.That(result.Occurrences[0].Values, Is.EqualTo(new string[] { "src" }));
        Assert.That(result.Positionals, Is.EqualTo(new ArgToken[] { result.Tokens[4] }));
        Assert.That(result.Positionals[0].Consumed, Is.False);
        Assert.That(result.UnmatchedTokens, Is.EqualTo(new ArgToken[] { result.Tokens[5] }));
        Assert.That(values.GetParseResult(), Is.SameAs(result));
        Assert.That(values.Occurrence("--include"), Is.SameAs(result.Occurrences[0]));
    }

    [Test]
    public void ValueSources_FollowTheDocumentedPrecedenceOrder()
    {
        PublicApiBindingEnvironment environment = new PublicApiBindingEnvironment("TARGET", "environment");
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = environment;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--trigger").Flag().Implies("--target", "implied");
        app.Option("--target").OptionalValue("value").Fallback("fallback").Env("TARGET").Default("default");

        IArgValues commandLine = Values(app, new string[] { "--trigger", "--target=command-line" });
        IArgValues fallback = Values(app, new string[] { "--trigger", "--target" });
        IArgValues implied = Values(app, new string[] { "--trigger" });
        IArgValues environmentValues = Values(app, new string[] { });
        environment.Value = null;
        IArgValues defaulted = Values(app, new string[] { });

        AssertSource(commandLine, "command-line", ArgValueSource.CommandLine, true, false);
        AssertSource(fallback, "fallback", ArgValueSource.Fallback, true, false);
        AssertSource(implied, "implied", ArgValueSource.Implied, false, false);
        AssertSource(environmentValues, "environment", ArgValueSource.Environment, false, false);
        AssertSource(defaulted, "default", ArgValueSource.Default, false, true);
        Assert.That(defaulted.Source("--missing"), Is.EqualTo(ArgValueSource.None));
        Assert.That(defaulted.Has("--missing"), Is.False);
    }

    [Test]
    public void EnvironmentAndDefaultSources_ResolveOptionsAndArguments()
    {
        PublicApiBindingEnvironment environment = new PublicApiBindingEnvironment("CONFIGURATION", "Release");
        environment.ArgumentName = "PROJECT";
        environment.ArgumentValue = "src";
        ArgParserSettings settings = new ArgParserSettings();
        settings.Environment = environment;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--configuration").Value("name").Env("CONFIGURATION").Default("Debug");
        app.Argument("project").Env("PROJECT").Default(".");

        IArgValues environmentValues = Values(app, new string[] { });
        environment.Value = null;
        environment.ArgumentValue = null;
        IArgValues defaultValues = Values(app, new string[] { });

        Assert.That(environmentValues.String("--configuration"), Is.EqualTo("Release"));
        Assert.That(environmentValues.String("project"), Is.EqualTo("src"));
        Assert.That(environmentValues.Source("--configuration"), Is.EqualTo(ArgValueSource.Environment));
        Assert.That(environmentValues.Source("project"), Is.EqualTo(ArgValueSource.Environment));
        Assert.That(defaultValues.String("--configuration"), Is.EqualTo("Debug"));
        Assert.That(defaultValues.String("project"), Is.EqualTo("."));
        Assert.That(defaultValues.Source("--configuration"), Is.EqualTo(ArgValueSource.Default));
        Assert.That(defaultValues.Source("project"), Is.EqualTo(ArgValueSource.Default));
    }

    [Test]
    public void Lookup_ResolvesCanonicalNamesAndAliasesAndPreservesOccurrenceOrder()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--include").Alias("-I").Value("path").Repeatable();
        app.Argument("files").Variadic();
        ArgParseResult result = app.Parse(new string[] { "--include", "src", "one.txt", "-I", "tests", "two.txt" });
        IArgValues values = new ArgValues(result);

        Assert.That(values.Has("--include"), Is.True);
        Assert.That(values.Has("-I"), Is.True);
        Assert.That(values.String("--include"), Is.EqualTo("src"));
        Assert.That(values.String("-I"), Is.EqualTo("src"));
        Assert.That(values.Strings("--include"), Is.EqualTo(new string[] { "src", "tests" }));
        Assert.That(values.Strings("-I"), Is.EqualTo(new string[] { "src", "tests" }));
        Assert.That(values.Strings("files"), Is.EqualTo(new string[] { "one.txt", "two.txt" }));
        Assert.That(values.Occurrence("-I"), Is.SameAs(result.Occurrences[0]));
        Assert.That(values.Occurrences("--include"), Is.EqualTo(new ArgOccurrence[] { result.Occurrences[0], result.Occurrences[1] }));
        Assert.That(values.Occurrence("--missing"), Is.Null);
        Assert.That(values.Occurrences("--missing"), Is.Empty);
        Assert.That(values.String("--missing"), Is.EqualTo(""));
        Assert.That(values.Strings("--missing"), Is.Empty);
    }

    [Test]
    public void Metadata_ReportsSuppliedDefaultedSourceAndCountValues()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--verbose").Alias("-v").Count();
        app.Option("--configuration").Value("name").Default("Debug");
        app.Option("--quiet").Flag();
        IArgValues values = Values(app, new string[] { "-vv", "--verbose", "--quiet" });

        Assert.That(values.Count("--verbose"), Is.EqualTo(3));
        Assert.That(values.Count("-v"), Is.EqualTo(3));
        Assert.That(values.Count("--quiet"), Is.EqualTo(0));
        Assert.That(values.Count("--missing"), Is.EqualTo(0));
        Assert.That(values.IsSupplied("--verbose"), Is.True);
        Assert.That(values.IsDefaulted("--verbose"), Is.False);
        Assert.That(values.Source("--verbose"), Is.EqualTo(ArgValueSource.CommandLine));
        Assert.That(values.IsSupplied("--configuration"), Is.False);
        Assert.That(values.IsDefaulted("--configuration"), Is.True);
        Assert.That(values.Source("--configuration"), Is.EqualTo(ArgValueSource.Default));
        Assert.That(values.IsSupplied("--missing"), Is.False);
        Assert.That(values.IsDefaulted("--missing"), Is.False);
    }

    [Test]
    public void ScalarConversions_CoverEveryBuiltInScalarType()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--enabled").Value("bool");
        app.Option("--verbose").Flag();
        app.Option("--int").Value("number");
        app.Option("--long").Value("number");
        app.Option("--double").Value("number");
        app.Option("--decimal").Value("number");
        app.Option("--date").Value("date");
        app.Option("--duration").Value("duration");
        app.Option("--file").Value("path");
        app.Option("--directory").Value("path");
        app.Option("--mode").Value("name");
        IArgValues values = Values(app, new string[]
        {
            "--enabled", "yes",
            "--verbose",
            "--int", "42",
            "--long", "9000000000",
            "--double", "3.5",
            "--decimal", "12.75",
            "--date", "2026-08-25",
            "--duration", "01:30:00",
            "--file", "src/ArgValues.cs",
            "--directory", "tests",
            "--mode", "release"
        });

        Assert.That(values.Bool("--enabled"), Is.True);
        Assert.That(values.Bool("--verbose"), Is.True);
        Assert.That(values.Int("--int"), Is.EqualTo(42));
        Assert.That(values.Long("--long"), Is.EqualTo(9000000000L));
        Assert.That(values.Double("--double"), Is.EqualTo(3.5d));
        Assert.That(values.Decimal("--decimal"), Is.EqualTo(12.75m));
        Assert.That(values.DateTime("--date"), Is.EqualTo(new System.DateTime(2026, 8, 25)));
        Assert.That(values.TimeSpan("--duration"), Is.EqualTo(new System.TimeSpan(1, 30, 0)));
        Assert.That(values.FileInfo("--file").Name, Is.EqualTo("ArgValues.cs"));
        Assert.That(values.DirectoryInfo("--directory").Name, Is.EqualTo("tests"));
        Assert.That(values.EnumValue("--mode", typeof(ArgPublicApiBindingMode)), Is.EqualTo(ArgPublicApiBindingMode.Release));
        Assert.That(values.HasConversionErrors(), Is.False);
        Assert.That(values.GetConversionErrors(), Is.Empty);
        Assert.That(values.GetLastConversionResult().Success, Is.True);
    }

    [Test]
    public void ArrayConversions_CoverEveryBuiltInArrayTypeInEncounterOrder()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--bool").Arity(2, 2);
        app.Option("--verbose").Flag().Repeatable();
        app.Option("--int").Arity(2, 2);
        app.Option("--long").Arity(2, 2);
        app.Option("--double").Arity(2, 2);
        app.Option("--decimal").Arity(2, 2);
        app.Option("--date").Arity(2, 2);
        app.Option("--duration").Arity(2, 2);
        app.Option("--file").Arity(2, 2);
        app.Option("--directory").Arity(2, 2);
        app.Option("--mode").Arity(2, 2);
        IArgValues values = Values(app, new string[]
        {
            "--bool", "yes", "off",
            "--verbose", "--verbose",
            "--int", "1", "2",
            "--long", "3", "4",
            "--double", "1.5", "2.5",
            "--decimal", "3.5", "4.5",
            "--date", "2026-08-25", "2026-08-26",
            "--duration", "01:00:00", "02:30:00",
            "--file", "src/one.cs", "tests/two.cs",
            "--directory", "src", "tests",
            "--mode", "debug", "RELEASE"
        });

        Assert.That(values.Bools("--bool"), Is.EqualTo(new bool[] { true, false }));
        Assert.That(values.Bools("--verbose"), Is.EqualTo(new bool[] { true, true }));
        Assert.That(values.Ints("--int"), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(values.Longs("--long"), Is.EqualTo(new long[] { 3L, 4L }));
        Assert.That(values.Doubles("--double"), Is.EqualTo(new double[] { 1.5d, 2.5d }));
        Assert.That(values.Decimals("--decimal"), Is.EqualTo(new decimal[] { 3.5m, 4.5m }));
        Assert.That(values.DateTimes("--date"), Is.EqualTo(new System.DateTime[] { new System.DateTime(2026, 8, 25), new System.DateTime(2026, 8, 26) }));
        Assert.That(values.TimeSpans("--duration"), Is.EqualTo(new System.TimeSpan[] { new System.TimeSpan(1, 0, 0), new System.TimeSpan(2, 30, 0) }));

        System.IO.FileInfo[] files = values.FileInfos("--file");
        System.IO.DirectoryInfo[] directories = values.DirectoryInfos("--directory");
        object?[] modes = values.EnumValues("--mode", typeof(ArgPublicApiBindingMode));

        Assert.That(files[0].Name, Is.EqualTo("one.cs"));
        Assert.That(files[1].Name, Is.EqualTo("two.cs"));
        Assert.That(directories[0].Name, Is.EqualTo("src"));
        Assert.That(directories[1].Name, Is.EqualTo("tests"));
        Assert.That(modes, Is.EqualTo(new object?[] { ArgPublicApiBindingMode.Debug, ArgPublicApiBindingMode.Release }));
        Assert.That(values.HasConversionErrors(), Is.False);
    }

    [Test]
    public void ConversionCulture_AppliesToCultureSensitiveScalarsAndArrays()
    {
        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("fr-FR");
        culture.NumberFormat.NegativeSign = "~";
        ArgParserSettings settings = new ArgParserSettings();
        settings.ConversionCulture = culture;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Option("--int").Value("number");
        app.Option("--long").Value("number");
        app.Option("--double").Arity(2, 2);
        app.Option("--decimal").Arity(2, 2);
        app.Option("--date").Arity(2, 2);
        app.Option("--duration").Arity(2, 2);
        IArgValues values = Values(app, new string[]
        {
            "--int", "~42",
            "--long", "~9000000000",
            "--double", "3,5", "4,5",
            "--decimal", "12,75", "13,25",
            "--date", "25/08/2026", "26/08/2026",
            "--duration", "01:02:03,5", "02:03:04,5"
        });

        Assert.That(values.Int("--int"), Is.EqualTo(-42));
        Assert.That(values.Long("--long"), Is.EqualTo(-9000000000L));
        Assert.That(values.Doubles("--double"), Is.EqualTo(new double[] { 3.5d, 4.5d }));
        Assert.That(values.Decimals("--decimal"), Is.EqualTo(new decimal[] { 12.75m, 13.25m }));
        Assert.That(values.DateTimes("--date"), Is.EqualTo(new System.DateTime[] { new System.DateTime(2026, 8, 25), new System.DateTime(2026, 8, 26) }));
        Assert.That(values.TimeSpans("--duration"), Is.EqualTo(new System.TimeSpan[] { new System.TimeSpan(0, 1, 2, 3, 500), new System.TimeSpan(0, 2, 3, 4, 500) }));
        Assert.That(values.HasConversionErrors(), Is.False);
    }

    [Test]
    public void Normalization_AppliesBeforeStringAndBuiltInConversionBinding()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--enabled").Value("bool").Normalize(TrimLower);
        app.Argument("number").Normalize(TrimLower);
        app.Argument("items").Variadic().Normalize(TrimLower);
        ArgParseResult result = app.Parse(new string[] { "--enabled", " YES ", " 42 ", " FIRST ", "Second" });
        IArgValues values = new ArgValues(result);

        Assert.That(result.Occurrences[0].Values[0], Is.EqualTo(" YES "));
        Assert.That(result.Occurrences[1].Values[0], Is.EqualTo(" 42 "));
        Assert.That(result.Occurrences[2].Values, Is.EqualTo(new string[] { " FIRST ", "Second" }));
        Assert.That(values.String("--enabled"), Is.EqualTo("yes"));
        Assert.That(values.Bool("--enabled"), Is.True);
        Assert.That(values.String("number"), Is.EqualTo("42"));
        Assert.That(values.Int("number"), Is.EqualTo(42));
        Assert.That(values.Strings("items"), Is.EqualTo(new string[] { "first", "second" }));
    }

    [Test]
    public void CustomConverters_ReturnDetailedSuccessAndFailureResults()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--name").Value("text");
        app.Option("--color").Value("text");
        IArgValues values = Values(app, new string[] { "--name", "alpha", "--color", "blue" });

        ArgValues concrete = values.RegisterConverter("upper", ConvertUpper);
        Assert.That(concrete, Is.SameAs(values));
        Assert.That(values.Custom("--name", "upper"), Is.EqualTo("ALPHA"));
        Assert.That(values.GetLastConversionResult().Success, Is.True);
        Assert.That(values.GetLastConversionResult().ExpectedType, Is.EqualTo("upper"));
        Assert.That(values.GetLastConversionResult().ActualValue, Is.EqualTo("alpha"));

        values.RegisterConverter("color", RejectColor);
        ArgConvertResult failure = values.CustomResult("--color", "color");

        Assert.That(failure.Success, Is.False);
        Assert.That(failure.Value, Is.Null);
        Assert.That(failure.Error, Is.EqualTo("Color rejected."));
        Assert.That(failure.ExpectedType, Is.EqualTo("color"));
        Assert.That(failure.ActualValue, Is.EqualTo("blue"));
        Assert.That(values.HasConversionErrors(), Is.True);
        Assert.That(values.GetConversionErrors(), Is.EqualTo(new ArgConvertResult[] { failure }));

        values.ClearConversionErrors();
        Assert.That(values.HasConversionErrors(), Is.False);
        Assert.That(values.GetConversionErrors(), Is.Empty);
        Assert.That(values.GetLastConversionResult(), Is.SameAs(failure));
    }

    [Test]
    public void ConversionFailures_PreserveArrayPositionsAndDetailedDiagnostics()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--int").Arity(3, 3);
        app.Option("--mode").Arity(3, 3);
        IArgValues values = Values(app, new string[] { "--int", "1", "bad", "3", "--mode", "debug", "staging", "release" });

        Assert.That(values.Ints("--int"), Is.EqualTo(new int[] { 1, 0, 3 }));
        Assert.That(values.EnumValues("--mode", typeof(ArgPublicApiBindingMode)), Is.EqualTo(new object?[] { ArgPublicApiBindingMode.Debug, null, ArgPublicApiBindingMode.Release }));
        Assert.That(values.HasConversionErrors(), Is.True);
        Assert.That(values.GetConversionErrors().Count, Is.EqualTo(2));
        AssertFailure(values.GetConversionErrors()[0], "int", "bad");
        AssertFailure(values.GetConversionErrors()[1], "enum ArgPublicApiBindingMode", "staging");
        Assert.That(values.GetLastConversionResult().Success, Is.True);
        Assert.That(values.GetLastConversionResult().ActualValue, Is.EqualTo("release"));
    }

    [Test]
    public void MissingValues_ReturnEmptyMetadataAndArraysWithoutArrayFailures()
    {
        IArgValues values = Values(new ArgApp("tool"), new string[] { });

        Assert.That(values.Has("missing"), Is.False);
        Assert.That(values.IsSupplied("missing"), Is.False);
        Assert.That(values.IsDefaulted("missing"), Is.False);
        Assert.That(values.Source("missing"), Is.EqualTo(ArgValueSource.None));
        Assert.That(values.Count("missing"), Is.EqualTo(0));
        Assert.That(values.String("missing"), Is.EqualTo(""));
        Assert.That(values.Strings("missing"), Is.Empty);
        Assert.That(values.Bools("missing"), Is.Empty);
        Assert.That(values.Ints("missing"), Is.Empty);
        Assert.That(values.Longs("missing"), Is.Empty);
        Assert.That(values.Doubles("missing"), Is.Empty);
        Assert.That(values.Decimals("missing"), Is.Empty);
        Assert.That(values.DateTimes("missing"), Is.Empty);
        Assert.That(values.TimeSpans("missing"), Is.Empty);
        Assert.That(values.FileInfos("missing"), Is.Empty);
        Assert.That(values.DirectoryInfos("missing"), Is.Empty);
        Assert.That(values.EnumValues("missing", typeof(ArgPublicApiBindingMode)), Is.Empty);
        Assert.That(values.Occurrence("missing"), Is.Null);
        Assert.That(values.Occurrences("missing"), Is.Empty);
        Assert.That(values.HasConversionErrors(), Is.False);
    }

    static IArgValues Values(IArgApp app, string[] argv)
    {
        return new ArgValues(app.Parse(argv));
    }

    static void AssertSource(IArgValues values, string text, ArgValueSource source, bool supplied, bool defaulted)
    {
        Assert.That(values.String("--target"), Is.EqualTo(text));
        Assert.That(values.Source("--target"), Is.EqualTo(source));
        Assert.That(values.IsSupplied("--target"), Is.EqualTo(supplied));
        Assert.That(values.IsDefaulted("--target"), Is.EqualTo(defaulted));
        Assert.That(values.Occurrence("--target"), Is.Not.Null);
    }

    static string TrimLower(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    static ArgConvertResult? ConvertUpper(string value)
    {
        return new ArgConvertResult(true, value.ToUpperInvariant(), "");
    }

    static ArgConvertResult? RejectColor(string value)
    {
        return new ArgConvertResult(false, null, "Color rejected.");
    }

    static void AssertFailure(ArgConvertResult result, string expectedType, string actualValue)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ExpectedType, Is.EqualTo(expectedType));
        Assert.That(result.ActualValue, Is.EqualTo(actualValue));
    }
}

public class PublicApiBindingEnvironment : IArgEnvironment
{
    public string Name;
    public string? Value;
    public string ArgumentName;
    public string? ArgumentValue;

    public PublicApiBindingEnvironment(string name, string value)
    {
        Name = name;
        Value = value;
        ArgumentName = "";
        ArgumentValue = null;
    }

    public string? GetEnvironmentVariable(string name)
    {
        if (name == Name)
        {
            return Value;
        }

        if (name == ArgumentName)
        {
            return ArgumentValue;
        }

        return null;
    }
}
