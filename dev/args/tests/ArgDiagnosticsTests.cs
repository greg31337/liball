using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgDiagnosticsTests
{
    [Test]
    public void Describe_IncludesCommandTreeModelDetails()
    {
        ArgApp app = new ArgApp("tool").Description("Example app.").Version("1.2.3");
        app.RootCommand.Alias("t").RequiredSubcommand().Before(new System.Func<ArgContext, int>(ReturnZero)).After(new System.Func<ArgContext, int>(ReturnZero));
        app.RootCommand.GlobalOption("--verbose").Alias("-v").Flag().Description("Enable verbose output.");
        ArgCommand build = app.Command("build").Alias("b").Summary("Build projects.").Description("Build the selected project.").Example("tool build src").Handler(new System.Func<ArgContext, int>(ReturnZero));
        build.GroupName = "Workflows";
        build.Option("--configuration")
            .Alias("-c")
            .Value("name")
            .Required()
            .Repeatable()
            .Default("Debug")
            .Fallback("Release")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release")
            .Complete("Debug")
            .Complete("Release")
            .Conflicts("--quiet")
            .Requires("--project")
            .Deprecated("Use --mode")
            .Description("Build configuration.");
        build.Argument("project")
            .Required()
            .Description("Project path.")
            .Default("src")
            .Env("PROJECT")
            .Choice("src")
            .Complete("src")
            .Deprecated("Use workspace.");
        string expected = string.Join(
            "\n",
            new string[]
            {
                "command tool",
                "  name: tool",
                "  aliases: t",
                "  hidden: false",
                "  required-subcommand: true",
                "  branch: false",
                "  handler: false",
                "  before: true",
                "  after: true",
                "  description: Example app.",
                "  version: 1.2.3",
                "  global-options:",
                "    option --verbose",
                "      aliases: --verbose, -v",
                "      global: true",
                "      required: false",
                "      hidden: false",
                "      flag: true",
                "      negatable: false",
                "      value: false",
                "      optional-value: false",
                "      repeatable: false",
                "      count: false",
                "      arity: 0..0",
                "      description: Enable verbose output.",
                "  commands:",
                "    command tool build",
                "      name: build",
                "      aliases: b",
                "      hidden: false",
                "      required-subcommand: false",
                "      branch: false",
                "      handler: true",
                "      before: false",
                "      after: false",
                "      summary: Build projects.",
                "      description: Build the selected project.",
                "      group: Workflows",
                "      options:",
                "        option --configuration",
                "          aliases: --configuration, -c",
                "          global: false",
                "          required: true",
                "          hidden: false",
                "          flag: false",
                "          negatable: false",
                "          value: true",
                "          optional-value: false",
                "          repeatable: true",
                "          count: false",
                "          arity: 1..1",
                "          value-name: name",
                "          description: Build configuration.",
                "          default: Debug",
                "          fallback: Release",
                "          env: CONFIGURATION",
                "          choices: Debug, Release",
                "          completions: Debug, Release",
                "          conflicts: --quiet",
                "          requires: --project",
                "          deprecated: Use --mode",
                "      arguments:",
                "        argument project",
                "          required: true",
                "          variadic: false",
                "          arity: 1..1",
                "          description: Project path.",
                "          default: src",
                "          env: PROJECT",
                "          choices: src",
                "          completions: src",
                "          deprecated: Use workspace.",
                "      examples:",
                "        tool build src"
            });

        string diagnostics = new ArgDiagnostics().Describe(app);

        Assert.That(diagnostics, Is.EqualTo(expected));
    }

    [Test]
    public void Describe_EmptyCommandUsesStableDefaults()
    {
        ArgCommand command = new ArgCommand("tool");
        string expected = string.Join(
            "\n",
            new string[]
            {
                "command tool",
                "  name: tool",
                "  aliases: ",
                "  hidden: false",
                "  required-subcommand: false",
                "  branch: false",
                "  handler: false",
                "  before: false",
                "  after: false"
            });

        string diagnostics = new ArgDiagnostics().DescribeCommand(command);

        Assert.That(diagnostics, Is.EqualTo(expected));
    }

    [Test]
    public void Describe_RejectsNullEntrypoints()
    {
        ArgDiagnostics diagnostics = new ArgDiagnostics();

        Assert.Throws<System.ArgumentNullException>(delegate { diagnostics.Describe((ArgApp)null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { diagnostics.DescribeCommand((ArgCommand)null!); });
    }

    static int ReturnZero(ArgContext context)
    {
        return 0;
    }
}
