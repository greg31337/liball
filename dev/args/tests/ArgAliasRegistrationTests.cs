using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgAliasRegistrationTests
{
    [Test]
    public void CommandRegistration_RejectsDuplicateSiblingNames()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build");

        Assert.That(new TestDelegate(delegate { root.Command("build"); }), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void CommandRegistration_RejectsSiblingNameAndAliasCollisions()
    {
        ArgCommand root = new ArgCommand("tool");
        root.Command("build").Alias("b");
        root.Command("run");

        Assert.That(new TestDelegate(delegate { root.Command("b"); }), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { root.Commands[1].Alias("build"); }), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void CommandAliasRegistration_RejectsDuplicatesOnSameCommand()
    {
        ArgCommand command = new ArgCommand("build");
        command.Alias("b");

        Assert.That(new TestDelegate(delegate { command.Alias("b"); }), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { command.Alias("build"); }), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void OptionRegistration_RejectsDuplicateAliasesAcrossLocalAndGlobalOptions()
    {
        ArgCommand command = new ArgCommand("tool");
        command.Option("--verbose").Alias("-v");

        Assert.That(new TestDelegate(delegate { command.Option("--verbose"); }), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { command.Option("-v"); }), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { command.GlobalOption("-v"); }), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void OptionAliasRegistration_RejectsDuplicatesOnSameOptionAndSiblingOptions()
    {
        ArgCommand command = new ArgCommand("tool");
        ArgOption verbose = command.Option("--verbose").Alias("-v");
        ArgOption quiet = command.Option("--quiet");

        Assert.That(new TestDelegate(delegate { verbose.Alias("-v"); }), Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(new TestDelegate(delegate { quiet.Alias("-v"); }), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void OptionAliasRegistration_AllowsSameAliasUnderDifferentCommands()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        ArgCommand test = root.Command("test");

        build.Option("--configuration").Alias("-c");
        test.Option("--configuration").Alias("-c");

        Assert.That(build.Options[0].Aliases, Is.EqualTo(new string[] { "--configuration", "-c" }));
        Assert.That(test.Options[0].Aliases, Is.EqualTo(new string[] { "--configuration", "-c" }));
    }
}
