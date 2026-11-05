using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCompletionScriptTests
{
    [Test]
    public void GenerateScript_RejectsNullCommandAndUnsupportedShell()
    {
        ArgCompletionProvider provider = new ArgCompletionProvider();
        ArgCommand command = new ArgCommand("tool");

        Assert.Throws<System.ArgumentNullException>(delegate { provider.GenerateScript(null!, ArgCompletionShell.Bash); });
        Assert.Throws<System.ArgumentException>(delegate { provider.GenerateScript(command, (ArgCompletionShell)99); });
    }

    [Test]
    public void GenerateScript_BashIncludesVisibleModelCandidatesAndEscapesValues()
    {
        ArgApp app = CompletionApp();
        ArgCompletionProvider provider = new ArgCompletionProvider();

        string script = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);

        Assert.That(script, Does.StartWith("# bash completion for my-tool\n_args_my_tool_completion()"));
        Assert.That(script, Does.Contain("        'build'"));
        Assert.That(script, Does.Contain("        'b'"));
        Assert.That(script, Does.Contain("        '--verbose'"));
        Assert.That(script, Does.Contain("        '--configuration'"));
        Assert.That(script, Does.Contain("        'Release Candidate'"));
        Assert.That(script, Does.Contain("        'O'\\''Reilly'"));
        Assert.That(script, Does.Contain("        'linux'"));
        Assert.That(script, Does.Contain("        '--help'"));
        Assert.That(script, Does.Contain("        '--version'"));
        Assert.That(script, Does.EndWith("complete -F _args_my_tool_completion -- 'my-tool'"));
        Assert.That(script, Does.Not.Contain("internal"));
        Assert.That(script, Does.Not.Contain("--secret"));
        Assert.That(script, Does.Not.Contain("SecretValue"));
        Assert.That(script, Does.Not.Contain("/windows"));
    }

    [Test]
    public void GenerateScript_ZshFishAndPowerShellUseNativeRegistrationAndDescriptions()
    {
        ArgApp app = CompletionApp();
        ArgCompletionProvider provider = new ArgCompletionProvider();

        string zsh = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Zsh);
        string fish = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Fish);
        string powerShell = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.PowerShell);

        Assert.That(zsh, Does.StartWith("#compdef my-tool\n_args_my_tool_completion()"));
        Assert.That(zsh, Does.Contain("compadd -- \"${candidates[@]}\""));
        Assert.That(zsh, Does.EndWith("compdef _args_my_tool_completion 'my-tool'"));
        Assert.That(fish, Does.StartWith("# fish completion for my-tool\ncomplete -c 'my-tool' -f"));
        Assert.That(fish, Does.Contain("complete -c 'my-tool' -a 'build' -d 'Build projects.'"));
        Assert.That(fish, Does.Contain("-a 'Release\\\\ Candidate'"));
        Assert.That(fish, Does.Contain("-a 'O\\'Reilly'"));
        Assert.That(powerShell, Does.StartWith("# PowerShell completion for my-tool\nRegister-ArgumentCompleter -Native -CommandName 'my-tool'"));
        Assert.That(powerShell, Does.Contain("@{ Text = 'build'; Description = 'Build projects.' }"));
        Assert.That(powerShell, Does.Contain("@{ Text = 'O''Reilly'; Description = '' }"));
        Assert.That(powerShell, Does.Contain("[System.Management.Automation.CompletionResult]::new"));
    }

    [Test]
    public void GenerateScript_IsDeterministicAndUsesTheRootOfDescendantCommands()
    {
        ArgApp app = CompletionApp();
        ArgCommand build = app.GetRootCommand().Commands[0];
        ArgCompletionProvider provider = new ArgCompletionProvider();

        string rootScript = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);
        string childScript = provider.GenerateScript(build, ArgCompletionShell.Bash);
        string repeatedScript = provider.GenerateScript(app.GetRootCommand(), ArgCompletionShell.Bash);

        Assert.That(childScript, Is.EqualTo(rootScript));
        Assert.That(repeatedScript, Is.EqualTo(rootScript));
    }

    [Test]
    public void GenerateScript_UsesParserSettingsForSlashCandidates()
    {
        ArgApp app = CompletionApp();
        ArgParserSettings settings = new ArgParserSettings();
        settings.Mode = ArgParserMode.WindowsFriendly;

        string script = new ArgCompletionProvider().UseSettings(settings).GenerateScript(app.GetRootCommand(), ArgCompletionShell.PowerShell);

        Assert.That(script, Does.Contain("@{ Text = '/windows'; Description = 'Windows option.' }"));
        Assert.That(script, Does.Contain("[System.StringComparison]::OrdinalIgnoreCase"));
    }

    static ArgApp CompletionApp()
    {
        ArgApp app = new ArgApp("my-tool").Version("1.2.3");
        ArgCommand root = app.GetRootCommand();
        root.GlobalOption("--verbose").Flag().Description("Verbose output.");
        root.GlobalOption("/windows").Flag().Description("Windows option.");
        root.Option("--secret").Flag().Hidden().Complete("SecretValue");
        root.Command("internal").Hidden();
        ArgCommand build = root.Command("build").Alias("b").Summary("Build projects.");
        build.Option("--configuration")
            .Alias("-c")
            .Value("name")
            .Complete("Debug")
            .Complete("Release Candidate")
            .Complete("O'Reilly");
        build.Argument("target").Complete("linux");
        return app;
    }
}
