using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgNameValidationTests
{
    [Test]
    public void CommandNames_AcceptPlainTokens()
    {
        Assert.That(ArgName.CommandNameError("build"), Is.EqualTo(""));
        Assert.That(ArgName.CommandNameError("build-all"), Is.EqualTo(""));
        Assert.That(ArgName.CommandNameError("build_all"), Is.EqualTo(""));

        ArgApp app = new ArgApp("tool");
        ArgCommand command = app.Command("build").Alias("b");

        Assert.That(command.Name, Is.EqualTo("build"));
        Assert.That(command.Aliases, Is.EqualTo(new string[] { "b" }));
    }

    [Test]
    public void CommandNames_RejectOptionLikeOrValueLikeTokens()
    {
        Assert.That(new TestDelegate(CreateEmptyAppName), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateDashCommandName), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateSlashCommandName), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateWhitespaceCommandName), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateValueLikeCommandName), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void OptionAliases_AcceptLongShortAndSlashForms()
    {
        Assert.That(ArgName.OptionAliasError("--configuration"), Is.EqualTo(""));
        Assert.That(ArgName.OptionAliasError("-c"), Is.EqualTo(""));
        Assert.That(ArgName.OptionAliasError("/configuration"), Is.EqualTo(""));

        ArgOption option = new ArgOption("--configuration").Alias("-c").Alias("/configuration");

        Assert.That(option.Aliases, Is.EqualTo(new string[] { "--configuration", "-c", "/configuration" }));
    }

    [Test]
    public void OptionAliases_RejectAmbiguousOrValueLikeForms()
    {
        Assert.That(new TestDelegate(CreateEmptyOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateBareDashOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateClusterOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateBareLongOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateTripleDashOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateMissingPrefixOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateEqualsOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateWhitespaceOptionAlias), Throws.TypeOf<System.ArgumentException>());
        Assert.That(new TestDelegate(CreateSlashValueOptionAlias), Throws.TypeOf<System.ArgumentException>());
    }

    static void CreateEmptyAppName()
    {
        new ArgApp("");
    }

    static void CreateDashCommandName()
    {
        new ArgCommand("-build");
    }

    static void CreateSlashCommandName()
    {
        new ArgCommand("/build");
    }

    static void CreateWhitespaceCommandName()
    {
        new ArgCommand("build all");
    }

    static void CreateValueLikeCommandName()
    {
        new ArgCommand("build=all");
    }

    static void CreateEmptyOptionAlias()
    {
        new ArgOption("");
    }

    static void CreateBareDashOptionAlias()
    {
        new ArgOption("-");
    }

    static void CreateClusterOptionAlias()
    {
        new ArgOption("-abc");
    }

    static void CreateBareLongOptionAlias()
    {
        new ArgOption("--");
    }

    static void CreateTripleDashOptionAlias()
    {
        new ArgOption("---name");
    }

    static void CreateMissingPrefixOptionAlias()
    {
        new ArgOption("name");
    }

    static void CreateEqualsOptionAlias()
    {
        new ArgOption("--name=value");
    }

    static void CreateWhitespaceOptionAlias()
    {
        new ArgOption("--name value");
    }

    static void CreateSlashValueOptionAlias()
    {
        new ArgOption("/name:value");
    }
}
