using System.Text.Json;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgMetadataJsonTests
{
    [Test]
    public void MetadataJson_EmitsCompleteVersionedCommandMetadata()
    {
        ArgApp app = MetadataApp();
        IArgDiagnostics diagnostics = new ArgDiagnostics();

        string json = diagnostics.MetadataJson(app);

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            JsonElement application = root.GetProperty("application");
            Assert.That(application.GetProperty("name").GetString(), Is.EqualTo("tool"));
            Assert.That(application.GetProperty("description").GetString(), Is.EqualTo("Example \"app\".\nNext line."));
            Assert.That(application.GetProperty("version").GetString(), Is.EqualTo("1.2.3"));

            JsonElement command = root.GetProperty("command");
            Assert.That(StringArray(command.GetProperty("path")), Is.EqualTo(new string[] { "tool" }));
            Assert.That(StringArray(command.GetProperty("aliases")), Is.EqualTo(new string[] { "t" }));
            Assert.That(command.GetProperty("defaultCommand").GetString(), Is.EqualTo("build"));
            Assert.That(command.GetProperty("globalOptions")[0].GetProperty("global").GetBoolean(), Is.True);

            JsonElement build = command.GetProperty("commands")[0];
            Assert.That(build.GetProperty("name").GetString(), Is.EqualTo("build"));
            Assert.That(StringArray(build.GetProperty("path")), Is.EqualTo(new string[] { "tool", "build" }));
            Assert.That(StringArray(build.GetProperty("aliases")), Is.EqualTo(new string[] { "b" }));
            Assert.That(build.GetProperty("group").GetString(), Is.EqualTo("Workflows"));
            Assert.That(build.GetProperty("deprecated").GetString(), Is.EqualTo("Use compile."));
            Assert.That(StringArray(build.GetProperty("examples")), Is.EqualTo(new string[] { "tool build src" }));

            JsonElement option = build.GetProperty("options")[0];
            Assert.That(option.GetProperty("name").GetString(), Is.EqualTo("--configuration"));
            Assert.That(StringArray(option.GetProperty("aliases")), Is.EqualTo(new string[] { "--configuration", "-c" }));
            Assert.That(option.GetProperty("arity").GetProperty("minimum").GetInt32(), Is.EqualTo(1));
            Assert.That(option.GetProperty("arity").GetProperty("maximum").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(option.GetProperty("arity").GetProperty("unbounded").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("hasDefault").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("default").GetString(), Is.EqualTo("Debug"));
            Assert.That(option.GetProperty("hasFallback").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("fallback").GetString(), Is.EqualTo("Release"));
            Assert.That(option.GetProperty("hasEnvironment").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("environment").GetString(), Is.EqualTo("CONFIGURATION"));
            Assert.That(StringArray(option.GetProperty("choices")), Is.EqualTo(new string[] { "Debug", "Release" }));
            Assert.That(StringArray(option.GetProperty("completions")), Is.EqualTo(new string[] { "Debug", "Release" }));
            Assert.That(StringArray(option.GetProperty("conflicts")), Is.EqualTo(new string[] { "--quiet" }));
            Assert.That(StringArray(option.GetProperty("requires")), Is.EqualTo(new string[] { "--project" }));
            Assert.That(option.GetProperty("implications")[0].GetProperty("option").GetString(), Is.EqualTo("--quiet"));
            Assert.That(option.GetProperty("implications")[0].GetProperty("value").GetString(), Is.EqualTo("false"));
            Assert.That(option.GetProperty("choiceValidationStage").GetString(), Is.EqualTo("normalized"));
            Assert.That(option.GetProperty("hasNormalizer").GetBoolean(), Is.True);
            Assert.That(option.GetProperty("deprecated").GetString(), Is.EqualTo("Use --mode."));

            JsonElement argument = build.GetProperty("arguments")[0];
            Assert.That(argument.GetProperty("name").GetString(), Is.EqualTo("project"));
            Assert.That(argument.GetProperty("position").GetInt32(), Is.EqualTo(0));
            Assert.That(argument.GetProperty("arity").GetProperty("maximum").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(argument.GetProperty("variadic").GetBoolean(), Is.True);
            Assert.That(argument.GetProperty("default").GetString(), Is.EqualTo("src"));
            Assert.That(StringArray(argument.GetProperty("completions")), Is.EqualTo(new string[] { "src", "tests" }));
            Assert.That(argument.GetProperty("deprecated").GetString(), Is.EqualTo("Use workspace."));

            JsonElement group = build.GetProperty("optionGroups")[0];
            Assert.That(group.GetProperty("name").GetString(), Is.EqualTo("output"));
            Assert.That(group.GetProperty("mutuallyExclusive").GetBoolean(), Is.True);
            Assert.That(StringArray(group.GetProperty("options")), Is.EqualTo(new string[] { "--configuration", "--quiet" }));
        }
    }

    [Test]
    public void CommandMetadataJson_PreservesFullPathAndDeclarationOrderFromDescendant()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand project = root.Command("project");
        ArgCommand build = project.Command("build");
        build.Command("first");
        build.Command("second");

        string json = new ArgDiagnostics().CommandMetadataJson(build);

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement documentRoot = document.RootElement;
            Assert.That(documentRoot.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(documentRoot.TryGetProperty("application", out JsonElement application), Is.False);
            JsonElement command = documentRoot.GetProperty("command");
            Assert.That(StringArray(command.GetProperty("path")), Is.EqualTo(new string[] { "tool", "project", "build" }));
            Assert.That(command.GetProperty("commands")[0].GetProperty("name").GetString(), Is.EqualTo("first"));
            Assert.That(command.GetProperty("commands")[1].GetProperty("name").GetString(), Is.EqualTo("second"));
            Assert.That(command.GetProperty("commands")[0].GetProperty("path")[3].GetString(), Is.EqualTo("first"));
        }
    }

    [Test]
    public void MetadataJson_IsDeterministicAndEmitsStableEmptyFields()
    {
        ArgApp app = new ArgApp("tool");
        ArgDiagnostics diagnostics = new ArgDiagnostics();

        string first = diagnostics.MetadataJson(app);
        string second = diagnostics.MetadataJson(app);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first, Does.StartWith("{\"schemaVersion\":1,\"application\":{\"name\":\"tool\",\"description\":\"\",\"version\":\"\"},\"command\":{\"name\":\"tool\",\"path\":[\"tool\"],\"aliases\":[]"));
        using (JsonDocument document = JsonDocument.Parse(first))
        {
            JsonElement command = document.RootElement.GetProperty("command");
            Assert.That(command.GetProperty("defaultCommand").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(command.GetProperty("globalOptions").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("options").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("arguments").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("optionGroups").GetArrayLength(), Is.EqualTo(0));
            Assert.That(command.GetProperty("commands").GetArrayLength(), Is.EqualTo(0));
        }
    }

    [Test]
    public void MetadataJson_RejectsNullEntrypoints()
    {
        ArgDiagnostics diagnostics = new ArgDiagnostics();

        Assert.Throws<System.ArgumentNullException>(delegate { diagnostics.MetadataJson(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { diagnostics.CommandMetadataJson(null!); });
    }

    static ArgApp MetadataApp()
    {
        ArgApp app = new ArgApp("tool").Description("Example \"app\".\nNext line.").Version("1.2.3");
        ArgCommand root = app.GetRootCommand();
        root.Alias("t");
        root.GlobalOption("--verbose").Alias("-v").Flag();
        ArgCommand build = root.Command("build")
            .Alias("b")
            .Summary("Build projects.")
            .Description("Build the selected project.")
            .Usage("tool build [options] <project>")
            .Group("Workflows")
            .Deprecated("Use compile.")
            .Example("tool build src");
        build.Option("--configuration")
            .Alias("-c")
            .Arity(1, ArgArity.Unbounded)
            .Required()
            .Repeatable()
            .Default("Debug")
            .Fallback("Release")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release")
            .Normalize(delegate(string value) { return value; })
            .ValidateChoicesAt(ArgChoiceValidationStage.Normalized)
            .Complete("Debug")
            .Complete("Release")
            .Conflicts("--quiet")
            .Requires("--project")
            .Implies("--quiet", "false")
            .Deprecated("Use --mode.");
        build.Option("--quiet").Flag();
        build.Option("--project").Value("path");
        build.Argument("project")
            .Required()
            .Variadic()
            .Default("src")
            .Env("PROJECT")
            .Choice("src")
            .Complete("src")
            .Complete("tests")
            .Deprecated("Use workspace.");
        build.MutuallyExclusiveGroup("output", new string[] { "--configuration", "--quiet" });
        root.DefaultCommand("build");
        return app;
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
}
