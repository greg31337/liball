using System.Collections.Generic;
using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgPublicApiCompletionTests
{
    [Test]
    public void Complete_ReturnsPartialCommandNamesAndAliasesWithCanonicalContext()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand publish = app.Command("publish").Alias("push").Summary("Publish packages.");
        app.Command("build").Alias("b");
        IArgCompletionProvider provider = new ArgCompletionProvider();

        List<ArgCompletion> completions = provider.Complete(app.GetRootCommand(), new string[] { "pu" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "publish", "push" }));
        Assert.That(completions[0].Kind, Is.EqualTo(ArgCompletionKind.Command));
        Assert.That(completions[0].SourceName, Is.EqualTo("publish"));
        Assert.That(completions[0].DescriptionText, Is.EqualTo("Publish packages."));
        Assert.That(completions[0].Command, Is.SameAs(publish));
        Assert.That(completions[1].SourceName, Is.EqualTo("publish"));
        Assert.That(completions[1].Command, Is.SameAs(publish));
    }

    [Test]
    public void Complete_UsesNestedAliasPathsForTheCurrentCommandContext()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand project = app.Command("project").Alias("p");
        ArgCommand publish = project.Command("publish").Alias("push");
        project.Command("build");
        app.Command("package");
        IArgCompletionProvider provider = new ArgCompletionProvider();

        List<ArgCompletion> completions = provider.Complete(app.GetRootCommand(), new string[] { "p", "pu" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "publish", "push" }));
        Assert.That(completions[0].Command, Is.SameAs(publish));
        Assert.That(completions[1].Command, Is.SameAs(publish));
        Assert.That(completions, Has.None.Matches<ArgCompletion>(delegate(ArgCompletion completion)
        {
            return completion.Text == "package";
        }));
    }

    [Test]
    public void Complete_ReturnsLocalAndInheritedOptionsButNotUnrelatedOrHiddenOptions()
    {
        IArgApp app = new ArgApp("tool");
        app.Option("--root-only").Flag();
        ArgOption verbose = app.GetRootCommand().GlobalOption("--verbose").Alias("-v").Flag();
        app.GetRootCommand().GlobalOption("--root-secret").Flag().Hidden();
        ArgCommand project = app.Command("project");
        project.Option("--project-only").Flag();
        ArgOption workspace = project.GlobalOption("--workspace").Flag();
        ArgCommand build = project.Command("build");
        ArgOption configuration = build.Option("--configuration").Alias("-c").Value("name");
        build.Option("--secret").Flag().Hidden();
        IArgCompletionProvider provider = new ArgCompletionProvider();

        List<ArgCompletion> completions = provider.Complete(app.GetRootCommand(), new string[] { "project", "build", "--" });

        Assert.That(Texts(completions), Is.EqualTo(new string[] { "--configuration", "--workspace", "--verbose", "--help" }));
        Assert.That(completions[0].Option, Is.SameAs(configuration));
        Assert.That(completions[1].Option, Is.SameAs(workspace));
        Assert.That(completions[2].Option, Is.SameAs(verbose));
        Assert.That(completions, Has.None.Matches<ArgCompletion>(delegate(ArgCompletion completion)
        {
            return completion.Text == "--root-only" || completion.Text == "--project-only" || completion.Text == "--secret" || completion.Text == "--root-secret";
        }));
    }

    [Test]
    public void Complete_ReturnsPartialOptionAndSuccessiveArgumentValues()
    {
        IArgApp app = new ArgApp("tool");
        ArgCommand build = app.Command("build");
        ArgOption configuration = build.Option("--configuration")
            .Alias("-c")
            .Value("name")
            .Complete("Debug")
            .Complete("Release");
        ArgArgument platform = build.Argument("platform")
            .Complete("linux")
            .Complete("windows");
        ArgArgument output = build.Argument("output")
            .Complete("artifacts")
            .Complete("release");
        IArgCompletionProvider provider = new ArgCompletionProvider();

        List<ArgCompletion> optionValues = provider.Complete(app.GetRootCommand(), new string[] { "build", "-c", "R" });
        List<ArgCompletion> firstArgumentValues = provider.Complete(app.GetRootCommand(), new string[] { "build", "l" });
        List<ArgCompletion> secondArgumentValues = provider.Complete(app.GetRootCommand(), new string[] { "build", "linux", "r" });

        Assert.That(Texts(optionValues), Is.EqualTo(new string[] { "Release" }));
        AssertValueCompletion(optionValues[0], "--configuration", configuration, null);
        Assert.That(Texts(firstArgumentValues), Is.EqualTo(new string[] { "linux" }));
        AssertValueCompletion(firstArgumentValues[0], "platform", null, platform);
        Assert.That(Texts(secondArgumentValues), Is.EqualTo(new string[] { "release" }));
        AssertValueCompletion(secondArgumentValues[0], "output", null, output);
    }

    [Test]
    public void Complete_AlwaysFiltersHiddenCommandsAndOptions()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.IncludeHiddenInHelp = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        app.Command("internal").Alias("i").Hidden();
        app.Command("inspect");
        app.Option("--secret").Alias("-s").Flag().Hidden();
        app.Option("--show").Flag();
        IArgCompletionProvider provider = new ArgCompletionProvider().UseSettings(settings);

        List<ArgCompletion> names = provider.Complete(app.GetRootCommand(), new string[] { "i" });
        List<ArgCompletion> options = provider.Complete(app.GetRootCommand(), new string[] { "--s" });

        Assert.That(Texts(names), Is.EqualTo(new string[] { "inspect" }));
        Assert.That(Texts(options), Is.EqualTo(new string[] { "--show" }));
    }

    [Test]
    public void Complete_AppliesCommandAndOptionCaseSettingsToPathsAndPrefixes()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.CommandsCaseInsensitive = true;
        settings.OptionsCaseInsensitive = true;
        IArgApp app = new ArgApp("tool").UseSettings(settings);
        ArgCommand build = app.Command("Build").Alias("MAKE");
        build.Option("--Configuration").Alias("-C").Value("name");
        IArgCompletionProvider insensitiveProvider = new ArgCompletionProvider().UseSettings(settings);
        IArgCompletionProvider sensitiveProvider = new ArgCompletionProvider();

        List<ArgCompletion> commands = insensitiveProvider.Complete(app.GetRootCommand(), new string[] { "bu" });
        List<ArgCompletion> aliasPathOptions = insensitiveProvider.Complete(app.GetRootCommand(), new string[] { "make", "--c" });
        List<ArgCompletion> sensitiveCommands = sensitiveProvider.Complete(app.GetRootCommand(), new string[] { "bu" });
        List<ArgCompletion> sensitiveOptions = sensitiveProvider.Complete(app.GetRootCommand(), new string[] { "Build", "--c" });

        Assert.That(Texts(commands), Is.EqualTo(new string[] { "Build" }));
        Assert.That(Texts(aliasPathOptions), Is.EqualTo(new string[] { "--Configuration" }));
        Assert.That(sensitiveCommands, Is.Empty);
        Assert.That(sensitiveOptions, Is.Empty);
    }

    [Test]
    public void Complete_TreatsEmptyAndFinalTokensAsPartialInput()
    {
        IArgApp app = new ArgApp("tool");
        app.Command("build");
        app.Command("test");
        app.Option("--verbose").Flag();
        IArgCompletionProvider provider = new ArgCompletionProvider();

        List<ArgCompletion> emptyArray = provider.Complete(app.GetRootCommand(), new string[] { });
        List<ArgCompletion> emptyToken = provider.Complete(app.GetRootCommand(), new string[] { "" });
        List<ArgCompletion> commandPrefix = provider.Complete(app.GetRootCommand(), new string[] { "te" });
        List<ArgCompletion> optionPrefix = provider.Complete(app.GetRootCommand(), new string[] { "--v" });

        Assert.That(Texts(emptyArray), Is.EqualTo(Texts(emptyToken)));
        Assert.That(Texts(emptyArray), Is.EqualTo(new string[] { "build", "test", "--verbose", "--help", "-h" }));
        Assert.That(Texts(commandPrefix), Is.EqualTo(new string[] { "test" }));
        Assert.That(Texts(optionPrefix), Is.EqualTo(new string[] { "--verbose" }));
    }

    [Test]
    public void GenerateScript_SupportsEveryShellAndIncludesOnlyVisiblePublicModelCandidates()
    {
        IArgApp app = new ArgApp("my-tool").Version("1.2.3");
        app.Command("internal").Hidden();
        ArgCommand build = app.Command("build").Alias("b").Summary("Build projects.");
        build.Option("--configuration").Value("name").Complete("Release");
        build.Option("--secret").Flag().Hidden();
        build.Argument("platform").Complete("linux");
        IArgCompletionProvider provider = new ArgCompletionProvider();

        string bash = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);
        string zsh = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Zsh);
        string fish = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Fish);
        string powerShell = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.PowerShell);

        Assert.That(bash, Does.StartWith("# bash completion for my-tool\n_args_my_tool_completion()"));
        Assert.That(bash, Does.EndWith("complete -F _args_my_tool_completion -- 'my-tool'"));
        Assert.That(zsh, Does.StartWith("#compdef my-tool\n_args_my_tool_completion()"));
        Assert.That(zsh, Does.EndWith("compdef _args_my_tool_completion 'my-tool'"));
        Assert.That(fish, Does.StartWith("# fish completion for my-tool\ncomplete -c 'my-tool' -f"));
        Assert.That(fish, Does.Contain("complete -c 'my-tool' -a 'build' -d 'Build projects.'"));
        Assert.That(powerShell, Does.StartWith("# PowerShell completion for my-tool\nRegister-ArgumentCompleter -Native -CommandName 'my-tool'"));
        Assert.That(powerShell, Does.Contain("[System.Management.Automation.CompletionResult]::new"));
        AssertVisibleScriptCandidates(bash);
        AssertVisibleScriptCandidates(zsh);
        AssertVisibleScriptCandidates(fish);
        AssertVisibleScriptCandidates(powerShell);
    }

    static void AssertValueCompletion(ArgCompletion completion, string sourceName, ArgOption? option, ArgArgument? argument)
    {
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Value));
        Assert.That(completion.SourceName, Is.EqualTo(sourceName));
        Assert.That(completion.Option, Is.SameAs(option));
        Assert.That(completion.Argument, Is.SameAs(argument));
    }

    static void AssertVisibleScriptCandidates(string script)
    {
        Assert.That(script, Does.Contain("build"));
        Assert.That(script, Does.Contain("--configuration"));
        Assert.That(script, Does.Contain("Release"));
        Assert.That(script, Does.Contain("linux"));
        Assert.That(script, Does.Contain("--help"));
        Assert.That(script, Does.Contain("--version"));
        Assert.That(script, Does.Not.Contain("internal"));
        Assert.That(script, Does.Not.Contain("--secret"));
    }

    static string[] Texts(List<ArgCompletion> completions)
    {
        string[] values = new string[completions.Count];
        int index = 0;
        while (index < completions.Count)
        {
            values[index] = completions[index].Text;
            index = index + 1;
        }

        return values;
    }
}
