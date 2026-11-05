using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiDiagnosticsMetadataTests
{
    [Test]
    public void DeprecatedUsage_ExposesEveryStructuredDiagnosticFieldAcrossResults()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build").Deprecated("Use compile.");
        build.Option("--configuration").Alias("-c").Value("name").Deprecated("Use --mode.");
        build.Argument("project").Required().Deprecated("Use workspace.");
        build.Handler(delegate(ArgContext context)
        {
            return 0;
        });
        string[] argv = new string[] { "build", "-c", "Release", "src" };

        ArgParseResult parse = app.Parse(argv);
        ArgValidationResult validation = app.Validate(argv);
        ArgRunResult run = app.Run(argv);

        Assert.That(parse.Diagnostics.Count, Is.EqualTo(3));
        AssertDiagnostic(parse.Diagnostics[0], "deprecated_command", ArgSymbolKind.Command, "Use compile.", 0, "build");
        AssertDiagnostic(parse.Diagnostics[1], "deprecated_option", ArgSymbolKind.Option, "Use --mode.", 1, "--configuration");
        AssertDiagnostic(parse.Diagnostics[2], "deprecated_argument", ArgSymbolKind.Argument, "Use workspace.", 3, "project");
        Assert.That(validation.Diagnostics, Is.SameAs(validation.ParseResult.Diagnostics));
        Assert.That(validation.Diagnostics.Count, Is.EqualTo(3));
        Assert.That(run.Diagnostics, Is.SameAs(run.ParseResult.Diagnostics));
        Assert.That(run.Diagnostics.Count, Is.EqualTo(3));
        Assert.That(run.HandlerInvoked, Is.True);
        Assert.That(run.ExitCode, Is.EqualTo(0));
    }

    [Test]
    public void Describe_IsDeterministicAndIncludesTheConfiguredPublicModel()
    {
        ArgApp app = MetadataApp();
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string first = diagnostics.Describe(app);
        string second = diagnostics.Describe(app);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.StartWith("command tool\n  name: tool\n  aliases: t"));
        Assert.That(first, Does.Contain("  required-subcommand: true\n  branch: true\n  handler: true\n  before: true\n  after: true"));
        Assert.That(first, Does.Contain("  default-command: build"));
        Assert.That(first, Does.Contain("    option --verbose\n      aliases: --verbose, -v\n      global: true"));
        Assert.That(first, Does.Contain("        option --configuration"));
        Assert.That(first, Does.Contain("          arity: 0..1"));
        Assert.That(first, Does.Contain("          implies: --quiet=false"));
        Assert.That(first, Does.Contain("        argument inputs\n          required: true\n          variadic: true\n          arity: 1..*"));
        Assert.That(first, Does.Contain("        group output\n          mutually-exclusive: true\n          at-least-one-required: false"));
        Assert.That(first, Does.Contain("        group destination\n          mutually-exclusive: false\n          at-least-one-required: true"));
        Assert.That(first, Does.EndWith("      build --configuration Release src"));
    }

    [Test]
    public void MetadataJson_ConformsToTheApplicationAndCommandSchema()
    {
        ArgApp app = MetadataApp();
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string json = diagnostics.MetadataJson(app);

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            AssertPropertyNames(root, new string[] { "schemaVersion", "application", "command" });
            Assert.That(root.GetProperty("schemaVersion").ValueKind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));

            JsonElement application = root.GetProperty("application");
            AssertPropertyNames(application, new string[] { "name", "description", "version" });
            Assert.That(application.GetProperty("name").GetString(), Is.EqualTo("tool"));
            Assert.That(application.GetProperty("description").GetString(), Is.EqualTo("Diagnostic application."));
            Assert.That(application.GetProperty("version").GetString(), Is.EqualTo("1.2.3"));

            JsonElement command = root.GetProperty("command");
            AssertCommandPropertyNames(command);
            Assert.That(command.GetProperty("name").GetString(), Is.EqualTo("tool"));
            Assert.That(StringArray(command.GetProperty("path")), Is.EqualTo(new string[] { "tool" }));
            Assert.That(StringArray(command.GetProperty("aliases")), Is.EqualTo(new string[] { "t" }));
            Assert.That(command.GetProperty("summary").GetString(), Is.EqualTo("Tool summary."));
            Assert.That(command.GetProperty("description").GetString(), Is.EqualTo("Diagnostic application."));
            Assert.That(command.GetProperty("usage").GetString(), Is.EqualTo("tool [options] [command]"));
            Assert.That(command.GetProperty("version").GetString(), Is.EqualTo("1.2.3"));
            Assert.That(command.GetProperty("group").GetString(), Is.EqualTo("Utilities"));
            Assert.That(command.GetProperty("defaultCommand").GetString(), Is.EqualTo("build"));
            Assert.That(command.GetProperty("deprecated").GetString(), Is.EqualTo("Use new-tool."));
            Assert.That(command.GetProperty("hidden").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("requiresSubcommand").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("branch").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("hasHandler").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("hasBeforeHook").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("hasAfterHook").GetBoolean(), Is.True);
            Assert.That(command.GetProperty("globalOptions").GetArrayLength(), Is.EqualTo(1));
            Assert.That(command.GetProperty("options").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("arguments").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("optionGroups").GetArrayLength(), Is.EqualTo(0));
            Assert.That(StringArray(command.GetProperty("examples")), Is.EqualTo(new string[] { "tool build" }));
            Assert.That(command.GetProperty("commands").GetArrayLength(), Is.EqualTo(1));

            JsonElement build = command.GetProperty("commands")[0];
            AssertCommandPropertyNames(build);
            Assert.That(build.GetProperty("name").GetString(), Is.EqualTo("build"));
            Assert.That(StringArray(build.GetProperty("path")), Is.EqualTo(new string[] { "tool", "build" }));
            Assert.That(build.GetProperty("defaultCommand").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void MetadataJson_ConformsToEveryOptionArgumentAndRelationshipField()
    {
        ArgApp app = MetadataApp();
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string json = diagnostics.MetadataJson(app);

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement rootCommand = document.RootElement.GetProperty("command");
            JsonElement globalOption = rootCommand.GetProperty("globalOptions")[0];
            AssertOptionPropertyNames(globalOption);
            Assert.That(globalOption.GetProperty("name").GetString(), Is.EqualTo("--verbose"));
            Assert.That(globalOption.GetProperty("global").GetBoolean(), Is.True);
            Assert.That(globalOption.GetProperty("flag").GetBoolean(), Is.True);
            Assert.That(globalOption.GetProperty("count").GetBoolean(), Is.True);
            AssertArity(globalOption.GetProperty("arity"), 0, 0, false);

            JsonElement build = rootCommand.GetProperty("commands")[0];
            JsonElement option = build.GetProperty("options")[0];
            AssertOptionPropertyNames(option);
            Assert.That(option.GetProperty("name").GetString(), Is.EqualTo("--configuration"));
            Assert.That(StringArray(option.GetProperty("aliases")), Is.EqualTo(new string[] { "--configuration", "-c" }));
            Assert.That(option.GetProperty("global").GetBoolean(), Is.False);
            Assert.That(option.GetProperty("description").GetString(), Is.EqualTo("Build configuration."));
            Assert.That(option.GetProperty("valueName").GetString(), Is.EqualTo("name"));
            AssertArity(option.GetProperty("arity"), 0, 1, false);
            Assert.That(option.GetProperty("required").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("hidden").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("flag").GetBoolean(), Is.False);
            Assert.That(option.GetProperty("negatable").GetBoolean(), Is.False);
            Assert.That(option.GetProperty("hasValue").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("optionalValue").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("repeatable").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("count").GetBoolean(), Is.False);
            Assert.That(option.GetProperty("hasDefault").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("default").GetString(), Is.EqualTo("Debug"));
            Assert.That(option.GetProperty("hasFallback").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("fallback").GetString(), Is.EqualTo("Release"));
            Assert.That(option.GetProperty("hasEnvironment").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("environment").GetString(), Is.EqualTo("CONFIGURATION"));
            Assert.That(StringArray(option.GetProperty("choices")), Is.EqualTo(new string[] { "Debug", "Release" }));
            Assert.That(option.GetProperty("choiceValidationStage").GetString(), Is.EqualTo("converted"));
            Assert.That(option.GetProperty("hasNormalizer").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("hasChoiceConverter").GetBoolean(), Is.True);
            Assert.That(StringArray(option.GetProperty("completions")), Is.EqualTo(new string[] { "Debug", "Release" }));
            Assert.That(StringArray(option.GetProperty("conflicts")), Is.EqualTo(new string[] { "--quiet" }));
            Assert.That(StringArray(option.GetProperty("requires")), Is.EqualTo(new string[] { "--project" }));
            Assert.That(option.GetProperty("deprecated").GetString(), Is.EqualTo("Use --mode."));

            JsonElement implication = option.GetProperty("implications")[0];
            AssertPropertyNames(implication, new string[] { "option", "value" });
            Assert.That(implication.GetProperty("option").GetString(), Is.EqualTo("--quiet"));
            Assert.That(implication.GetProperty("value").GetString(), Is.EqualTo("false"));

            JsonElement negatable = build.GetProperty("options")[1];
            Assert.That(negatable.GetProperty("negatable").GetBoolean(), Is.True);
            Assert.That(negatable.GetProperty("flag").GetBoolean(), Is.True);

            JsonElement argument = build.GetProperty("arguments")[0];
            AssertArgumentPropertyNames(argument);
            Assert.That(argument.GetProperty("name").GetString(), Is.EqualTo("inputs"));
            Assert.That(argument.GetProperty("position").GetInt32(), Is.EqualTo(0));
            Assert.That(argument.GetProperty("description").GetString(), Is.EqualTo("Input paths."));
            AssertArity(argument.GetProperty("arity"), 1, 0, true);
            Assert.That(argument.GetProperty("required").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("variadic").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("hasDefault").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("default").GetString(), Is.EqualTo("src"));
            Assert.That(argument.GetProperty("hasEnvironment").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("environment").GetString(), Is.EqualTo("INPUTS"));
            Assert.That(StringArray(argument.GetProperty("choices")), Is.EqualTo(new string[] { "src", "tests" }));
            Assert.That(argument.GetProperty("choiceValidationStage").GetString(), Is.EqualTo("converted"));
            Assert.That(argument.GetProperty("hasNormalizer").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("hasChoiceConverter").GetBoolean(), Is.True);
            Assert.That(StringArray(argument.GetProperty("completions")), Is.EqualTo(new string[] { "src", "tests" }));
            Assert.That(argument.GetProperty("deprecated").GetString(), Is.EqualTo("Use workspace."));

            JsonElement exclusive = build.GetProperty("optionGroups")[0];
            AssertGroup(exclusive, "output", true, false, new string[] { "--json", "--text" });
            JsonElement required = build.GetProperty("optionGroups")[1];
            AssertGroup(required, "destination", false, true, new string[] { "--file", "--project" });
        }
    }

    [Test]
    public void MetadataAndTextOutputs_AreDeterministicAndPreserveEscapedContent()
    {
        string special = "quote: \" slash: \\ newline:\n tab:\t snowman: \u2603";
        ArgApp app = new ArgApp("tool").Description(special).Version("v\"1\\2");
        ArgCommand build = app.Command("build").Summary(special).Example(special);
        build.Option("--label").Value("text").Description(special).Complete(special);
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string firstJson = diagnostics.MetadataJson((ArgApp)app);
        string secondJson = diagnostics.MetadataJson((ArgApp)app);
        string firstText = diagnostics.Describe((ArgApp)app);
        string secondText = diagnostics.Describe((ArgApp)app);

        Assert.That(secondJson, Is.EqualTo(firstJson));
        Assert.That(secondText, Is.EqualTo(firstText));
        Assert.That(firstJson, Does.Contain("\\n"));
        Assert.That(firstJson, Does.Contain("\\t"));
        Assert.That(firstJson, Does.Contain("\\\\"));
        Assert.That(firstJson, Does.Not.Contain("newline:\n"));
        Assert.That(firstText, Does.Contain(special));
        using (JsonDocument document = JsonDocument.Parse(firstJson))
        {
            JsonElement root = document.RootElement;
            Assert.That(root.GetProperty("application").GetProperty("description").GetString(), Is.EqualTo(special));
            Assert.That(root.GetProperty("application").GetProperty("version").GetString(), Is.EqualTo("v\"1\\2"));
            JsonElement command = root.GetProperty("command").GetProperty("commands")[0];
            Assert.That(command.GetProperty("summary").GetString(), Is.EqualTo(special));
            Assert.That(command.GetProperty("examples")[0].GetString(), Is.EqualTo(special));
            JsonElement option = command.GetProperty("options")[0];
            Assert.That(option.GetProperty("description").GetString(), Is.EqualTo(special));
            Assert.That(option.GetProperty("completions")[0].GetString(), Is.EqualTo(special));
        }
    }

    [Test]
    public void DiagnosticsAndMetadata_HandleEmptyAndDeeplyNestedCommandModels()
    {
        ArgApp emptyApp = new ArgApp("empty");
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string emptyText = diagnostics.Describe(emptyApp);
        string emptyJson = diagnostics.MetadataJson(emptyApp);

        Assert.That(emptyText, Is.EqualTo("command empty\n  name: empty\n  aliases: \n  hidden: false\n  required-subcommand: false\n  branch: false\n  handler: false\n  before: false\n  after: false"));
        using (JsonDocument emptyDocument = JsonDocument.Parse(emptyJson))
        {
            JsonElement command = emptyDocument.RootElement.GetProperty("command");
            Assert.That(command.GetProperty("defaultCommand").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(command.GetProperty("globalOptions").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("options").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("arguments").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("optionGroups").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("examples").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("commands").GetArrayLength(), Is.EqualTo(0));
        }

        ArgApp deepApp = new ArgApp("tool");
        ArgCommand commandModel = deepApp.GetRootCommand();
        string[] expectedPath = new string[13];
        expectedPath[0] = "tool";
        int index = 1;
        while (index < expectedPath.Length)
        {
            string name = "level" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expectedPath[index] = name;
            commandModel = commandModel.Command(name);
            index = index + 1;
        }

        string deepJson = diagnostics.MetadataJson(deepApp);
        string deepText = diagnostics.DescribeCommand(commandModel);

        using (JsonDocument deepDocument = JsonDocument.Parse(deepJson))
        {
            JsonElement current = deepDocument.RootElement.GetProperty("command");
            index = 1;
            while (index < expectedPath.Length)
            {
                current = current.GetProperty("commands")[0];
                Assert.That(StringArray(current.GetProperty("path")), Is.EqualTo(Subarray(expectedPath, index + 1)));
                index = index + 1;
            }

            Assert.That(current.GetProperty("name").GetString(), Is.EqualTo("level12"));
            Assert.That(current.GetProperty("commands").GetArrayLength(), Is.EqualTo(0));
        }

        Assert.That(deepText, Does.StartWith("command " + string.Join(" ", expectedPath)));
    }

    static ArgApp MetadataApp()
    {
        ArgApp app = new ArgApp("tool").Description("Diagnostic application.").Version("1.2.3");
        ArgCommand root = app.GetRootCommand();
        root.Alias("t")
            .Summary("Tool summary.")
            .Usage("tool [options] [command]")
            .Group("Utilities")
            .Hidden()
            .Deprecated("Use new-tool.")
            .RequiredSubcommand()
            .Branch()
            .Example("tool build")
            .Before(delegate(ArgContext context) { return 0; })
            .Handler(delegate(ArgContext context) { return 0; })
            .After(delegate(ArgContext context) { return 0; });
        root.GlobalOption("--verbose").Alias("-v").Count().Description("Verbosity.");
        ArgCommand build = root.Command("build")
            .Alias("b")
            .Summary("Build projects.")
            .Description("Build configured inputs.")
            .Usage("tool build [options] <inputs>")
            .Group("Workflows")
            .Deprecated("Use compile.")
            .Example("build --configuration Release src");
        root.DefaultCommand("build");
        build.Option("--configuration")
            .Alias("-c")
            .OptionalValue("name")
            .Required()
            .Repeatable()
            .Hidden()
            .Description("Build configuration.")
            .Default("Debug")
            .Fallback("Release")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release")
            .Normalize(delegate(string value) { return value; })
            .ConvertChoices(delegate(string value) { return new ArgConvertResult(true, value, ""); })
            .ValidateChoicesAt(ArgChoiceValidationStage.Converted)
            .Complete("Debug")
            .Complete("Release")
            .Conflicts("--quiet")
            .Requires("--project")
            .Implies("--quiet", "false")
            .Deprecated("Use --mode.");
        build.Option("--quiet").Flag().Negatable();
        build.Option("--project").Value("path");
        build.Option("--json").Flag();
        build.Option("--text").Flag();
        build.Option("--file").Value("path");
        build.Argument("inputs")
            .Required()
            .Variadic()
            .Description("Input paths.")
            .Default("src")
            .Env("INPUTS")
            .Choice("src")
            .Choice("tests")
            .Normalize(delegate(string value) { return value; })
            .ConvertChoices(delegate(string value) { return new ArgConvertResult(true, value, ""); })
            .ValidateChoicesAt(ArgChoiceValidationStage.Converted)
            .Complete("src")
            .Complete("tests")
            .Deprecated("Use workspace.");
        build.MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        build.AtLeastOneRequiredGroup("destination", new string[] { "--file", "--project" });
        return app;
    }

    static void AssertDiagnostic(ArgDiagnostic diagnostic, string code, ArgSymbolKind kind, string message, int tokenIndex, string symbolName)
    {
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.SymbolKind, Is.EqualTo(kind));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
        Assert.That(diagnostic.TokenIndex, Is.EqualTo(tokenIndex));
        Assert.That(diagnostic.SymbolName, Is.EqualTo(symbolName));
    }

    static void AssertCommandPropertyNames(JsonElement command)
    {
        AssertPropertyNames(command, new string[]
        {
            "name", "path", "aliases", "summary", "description", "usage", "version", "group",
            "defaultCommand", "deprecated", "hidden", "requiresSubcommand", "branch", "hasHandler",
            "hasBeforeHook", "hasAfterHook", "globalOptions", "options", "arguments", "optionGroups",
            "examples", "commands"
        });
    }

    static void AssertOptionPropertyNames(JsonElement option)
    {
        AssertPropertyNames(option, new string[]
        {
            "name", "aliases", "global", "description", "valueName", "arity", "required", "hidden",
            "flag", "negatable", "hasValue", "optionalValue", "repeatable", "count", "hasDefault",
            "default", "hasFallback", "fallback", "hasEnvironment", "environment", "choices",
            "choiceValidationStage", "hasNormalizer", "hasChoiceConverter", "completions", "conflicts",
            "requires", "implications", "deprecated"
        });
    }

    static void AssertArgumentPropertyNames(JsonElement argument)
    {
        AssertPropertyNames(argument, new string[]
        {
            "name", "position", "description", "arity", "required", "variadic", "hasDefault", "default",
            "hasEnvironment", "environment", "choices", "choiceValidationStage", "hasNormalizer",
            "hasChoiceConverter", "completions", "deprecated"
        });
    }

    static void AssertGroup(JsonElement group, string name, bool mutuallyExclusive, bool atLeastOneRequired, string[] options)
    {
        AssertPropertyNames(group, new string[] { "name", "mutuallyExclusive", "atLeastOneRequired", "options" });
        Assert.That(group.GetProperty("name").GetString(), Is.EqualTo(name));
        Assert.That(group.GetProperty("mutuallyExclusive").GetBoolean(), Is.EqualTo(mutuallyExclusive));
        Assert.That(group.GetProperty("atLeastOneRequired").GetBoolean(), Is.EqualTo(atLeastOneRequired));
        Assert.That(StringArray(group.GetProperty("options")), Is.EqualTo(options));
    }

    static void AssertArity(JsonElement arity, int minimum, int maximum, bool unbounded)
    {
        AssertPropertyNames(arity, new string[] { "minimum", "maximum", "unbounded" });
        Assert.That(arity.GetProperty("minimum").GetInt32(), Is.EqualTo(minimum));
        if (unbounded)
        {
            Assert.That(arity.GetProperty("maximum").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
        else
        {
            Assert.That(arity.GetProperty("maximum").GetInt32(), Is.EqualTo(maximum));
        }

        Assert.That(arity.GetProperty("unbounded").GetBoolean(), Is.EqualTo(unbounded));
    }

    static void AssertPropertyNames(JsonElement element, string[] expected)
    {
        List<string> actual = new List<string>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            actual.Add(property.Name);
        }

        Assert.That(actual, Is.EqualTo(expected));
    }

    static string[] StringArray(JsonElement array)
    {
        string[] values = new string[array.GetArrayLength()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = array[index].GetString() ?? "";
            index = index + 1;
        }

        return values;
    }

    static string[] Subarray(string[] values, int count)
    {
        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
