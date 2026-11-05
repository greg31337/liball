using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCommandConfigurationTests
{
    [Test]
    public void ConfigurationMethods_ExposeCommandCapabilitiesThroughInterface()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build").Alias("b");
        IArgCommand command = root;

        ArgCommand usageResult = command.Usage("tool [--fast]");
        ArgCommand groupResult = command.Group("Workflows");
        ArgCommand branchResult = command.Branch();
        ArgCommand defaultResult = command.DefaultCommand("b");
        ArgCommand deprecatedResult = command.Deprecated("Use runner instead.");

        Assert.That(usageResult, Is.SameAs(root));
        Assert.That(groupResult, Is.SameAs(root));
        Assert.That(branchResult, Is.SameAs(root));
        Assert.That(defaultResult, Is.SameAs(root));
        Assert.That(deprecatedResult, Is.SameAs(root));
        Assert.That(command.GetUsageText(), Is.EqualTo("tool [--fast]"));
        Assert.That(command.GetGroupName(), Is.EqualTo("Workflows"));
        Assert.That(command.GetIsBranch(), Is.True);
        Assert.That(command.GetDefaultCommand(), Is.SameAs(build));
        Assert.That(command.GetDeprecatedMessage(), Is.EqualTo("Use runner instead."));
    }

    [Test]
    public void UsageAndGroup_AffectGeneratedHelp()
    {
        ArgCommand root = new ArgCommand("tool").Usage("tool custom");
        root.Command("build").Group("Workflows").Summary("Build projects.");

        string help = new ArgHelpBuilder().Help(root);

        Assert.That(help, Does.StartWith("Usage: tool custom"));
        Assert.That(help, Does.Contain("Commands:\n  Workflows:\n    build - Build projects."));
    }

    [Test]
    public void Branch_LeafWithoutHandlerReturnsHelp()
    {
        ArgApp app = new ArgApp("tool");
        ArgCommand status = app.Command("status").Branch().Summary("Status commands.");

        ArgRunResult result = app.Run(new string[] { "status" });

        Assert.That(result.Context.Command, Is.SameAs(status));
        Assert.That(result.HandlerInvoked, Is.False);
        Assert.That(result.ValidationResult.HelpRequested, Is.True);
        Assert.That(result.HelpText, Does.Contain("Status commands."));
    }

    [Test]
    public void DefaultCommand_RejectsUnknownOrInvalidChildName()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");

        Assert.That(new TestDelegate(() => root.DefaultCommand("test")), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(() => root.DefaultCommand("--build")), Throws.TypeOf<System.ArgumentException>());
        Assert.That(root.GetDefaultCommand(), Is.Null);
    }

    [Test]
    public void Diagnostics_IncludeNewCommandConfiguration()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");
        root.DefaultCommand("build").Deprecated("Use runner instead.");

        string diagnostics = new ArgDiagnostics().DescribeCommand(root);

        Assert.That(diagnostics, Does.Contain("default-command: build"));
        Assert.That(diagnostics, Does.Contain("deprecated: Use runner instead."));
    }
}
