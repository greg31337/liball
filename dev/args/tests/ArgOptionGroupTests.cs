using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgOptionGroupTests
{
    [Test]
    public void Command_CreatesPublicOptionGroupModels()
    {
        ArgApp app = new ArgApp("tool");
        IArgCommand command = app.GetRootCommand();

        ArgOptionGroup exclusive = command.MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        ArgOptionGroup required = command.AtLeastOneRequiredGroup("input", new string[] { "--file", "--stdin" });

        Assert.That(exclusive.Name, Is.EqualTo("output"));
        Assert.That(exclusive.OptionNames, Is.EqualTo(new string[] { "--json", "--text" }));
        Assert.That(exclusive.IsMutuallyExclusive, Is.True);
        Assert.That(exclusive.IsAtLeastOneRequired, Is.False);
        Assert.That(exclusive.ParentCommand, Is.SameAs(command));
        Assert.That(required.IsMutuallyExclusive, Is.False);
        Assert.That(required.IsAtLeastOneRequired, Is.True);
        Assert.That(command.GetOptionGroups(), Is.EqualTo(new ArgOptionGroup[] { exclusive, required }));
    }

    [Test]
    public void Validate_MutuallyExclusiveGroupRejectsMultipleOptions()
    {
        ArgApp app = ExclusiveApp();

        ArgValidationResult result = app.Validate(new string[] { "--json", "--text" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertGroupError(result.Errors[0], "mutually_exclusive_group", "Option group 'output' allows at most one option.", "output");
    }

    [Test]
    public void Validate_MutuallyExclusiveGroupAllowsZeroOrOneOption()
    {
        ArgApp app = ExclusiveApp();

        ArgValidationResult empty = app.Validate(new string[] { });
        ArgValidationResult one = app.Validate(new string[] { "--json" });

        Assert.That(empty.Errors, Is.Empty);
        Assert.That(one.Errors, Is.Empty);
    }

    [Test]
    public void Validate_AtLeastOneRequiredGroupRejectsNoOptions()
    {
        ArgApp app = RequiredApp();

        ArgValidationResult result = app.Validate(new string[] { });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        AssertGroupError(result.Errors[0], "missing_required_option_group", "Option group 'input' requires at least one option.", "input");
    }

    [Test]
    public void Validate_AtLeastOneRequiredGroupAcceptsAliasesAndDefaults()
    {
        ArgApp aliasApp = new ArgApp("tool");
        aliasApp.Option("--file").Alias("-f").Value("path");
        aliasApp.Option("--stdin").Flag();
        aliasApp.GetRootCommand().AtLeastOneRequiredGroup("input", new string[] { "-f", "--stdin" });
        ArgApp defaultApp = new ArgApp("tool");
        defaultApp.Option("--format").Value("name").Default("text");
        defaultApp.Option("--raw").Flag();
        defaultApp.GetRootCommand().AtLeastOneRequiredGroup("format", new string[] { "--format", "--raw" });

        ArgValidationResult aliasResult = aliasApp.Validate(new string[] { "-f", "input.txt" });
        ArgValidationResult defaultResult = defaultApp.Validate(new string[] { });

        Assert.That(aliasResult.Errors, Is.Empty);
        Assert.That(defaultResult.Errors, Is.Empty);
    }

    [Test]
    public void Validate_GroupCanReferenceInheritedGlobalOption()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().GlobalOption("--verbose").Flag();
        ArgCommand build = app.Command("build");
        build.Option("--quiet").Flag();
        build.MutuallyExclusiveGroup("verbosity", new string[] { "--verbose", "--quiet" });

        ArgValidationResult result = app.Validate(new string[] { "build", "--verbose", "--quiet" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("mutually_exclusive_group"));
    }

    [Test]
    public void Validate_GroupsAreLocalToSelectedCommandAndAggregateErrors()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--root").Flag();
        app.GetRootCommand().AtLeastOneRequiredGroup("root-input", new string[] { "--root" });
        ArgCommand build = app.Command("build");
        build.Option("--json").Flag();
        build.Option("--text").Flag();
        build.Option("--file").Flag();
        build.MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        build.AtLeastOneRequiredGroup("input", new string[] { "--file" });

        ArgValidationResult result = app.Validate(new string[] { "build", "--json", "--text" });

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        Assert.That(result.Errors[0].Code, Is.EqualTo("mutually_exclusive_group"));
        Assert.That(result.Errors[1].Code, Is.EqualTo("missing_required_option_group"));
    }

    [Test]
    public void Help_ListsOptionGroupConstraints()
    {
        ArgApp app = ExclusiveApp();
        app.Option("--file").Value("path");
        app.Option("--stdin").Flag();
        app.GetRootCommand().AtLeastOneRequiredGroup("input", new string[] { "--file", "--stdin" });

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Does.Contain("Option groups:\n  output (mutually exclusive): --json, --text\n  input (at least one required): --file, --stdin"));
    }

    [Test]
    public void Help_OptionGroupsDoNotRevealHiddenOptions()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--public").Flag();
        app.Option("--secret").Flag().Hidden();
        app.GetRootCommand().MutuallyExclusiveGroup("mixed", new string[] { "--public", "--secret" });
        app.GetRootCommand().AtLeastOneRequiredGroup("hidden", new string[] { "--secret" });

        string help = new ArgHelpBuilder().RootHelp(app);

        Assert.That(help, Does.Contain("Option groups:\n  mixed (mutually exclusive): --public"));
        Assert.That(help, Does.Not.Contain("--secret"));
        Assert.That(help, Does.Not.Contain("hidden (at least one required)"));
    }

    [Test]
    public void Diagnostics_IncludeOptionGroupModel()
    {
        ArgApp app = ExclusiveApp();

        string diagnostics = new ArgDiagnostics().Describe(app);

        Assert.That(diagnostics, Does.Contain("option-groups:\n    group output\n      mutually-exclusive: true\n      at-least-one-required: false\n      options: --json, --text"));
    }

    [Test]
    public void Validate_HelpShortCircuitSkipsOptionGroups()
    {
        ArgApp app = RequiredApp();

        ArgValidationResult result = app.Validate(new string[] { "--help" });

        Assert.That(result.ShortCircuited, Is.True);
        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Command_RejectsInvalidOrDuplicateGroups()
    {
        ArgApp app = new ArgApp("tool");
        app.GetRootCommand().MutuallyExclusiveGroup("output", new string[] { "--json" });

        Assert.That(new TestDelegate(() => app.GetRootCommand().AtLeastOneRequiredGroup("output", new string[] { "--text" })), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(() => app.GetRootCommand().MutuallyExclusiveGroup("", new string[] { "--json" })), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(() => app.GetRootCommand().MutuallyExclusiveGroup("input", new string[] { "invalid" })), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(() => app.GetRootCommand().MutuallyExclusiveGroup("input", null!)), Throws.TypeOf<System.ArgumentNullException>());
    }

    static ArgApp ExclusiveApp()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--json").Flag();
        app.Option("--text").Flag();
        app.GetRootCommand().MutuallyExclusiveGroup("output", new string[] { "--json", "--text" });
        return app;
    }

    static ArgApp RequiredApp()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--file").Value("path");
        app.Option("--stdin").Flag();
        app.GetRootCommand().AtLeastOneRequiredGroup("input", new string[] { "--file", "--stdin" });
        return app;
    }

    static void AssertGroupError(ArgParseError error, string code, string message, string name)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(error.Message, Is.EqualTo(message));
        Assert.That(error.TokenIndex, Is.EqualTo(-1));
        Assert.That(error.SymbolName, Is.EqualTo(name));
    }
}
