using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgModelTests
{
    [Test]
    public void ArgArity_DefaultAllowsNoValuesOnly()
    {
        ArgArity arity = new ArgArity(0, 0);

        Assert.That(arity.Minimum, Is.EqualTo(0));
        Assert.That(arity.Maximum, Is.EqualTo(0));
        Assert.That(arity.Allows(0), Is.True);
        Assert.That(arity.Allows(1), Is.False);
    }

    [Test]
    public void ArgArity_BoundedAndUnboundedRanges()
    {
        ArgArity bounded = new ArgArity(1, 2);
        ArgArity unbounded = new ArgArity(1, ArgArity.Unbounded);

        Assert.That(bounded.Allows(0), Is.False);
        Assert.That(bounded.Allows(1), Is.True);
        Assert.That(bounded.Allows(2), Is.True);
        Assert.That(bounded.Allows(3), Is.False);
        Assert.That(unbounded.Allows(0), Is.False);
        Assert.That(unbounded.Allows(1), Is.True);
        Assert.That(unbounded.Allows(20), Is.True);
    }

    [Test]
    public void ArgArity_RejectsInvalidRanges()
    {
        Assert.That(new TestDelegate(CreateNegativeMinimumArity), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(new TestDelegate(CreateMaximumBelowMinimumArity), Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void ArgCommand_InitializesCommandModel()
    {
        ArgCommand command = new ArgCommand("build");

        Assert.That(command.Name, Is.EqualTo("build"));
        Assert.That(command.Kind, Is.EqualTo(ArgSymbolKind.Command));
        Assert.That(command.Parent, Is.Null);
        Assert.That(command.Aliases, Is.Empty);
        Assert.That(command.Commands, Is.Empty);
        Assert.That(command.Options, Is.Empty);
        Assert.That(command.GlobalOptions, Is.Empty);
        Assert.That(command.Arguments, Is.Empty);
        Assert.That(command.Examples, Is.Empty);
        Assert.That(command.CommandValidators, Is.Empty);
        Assert.That(command.CrossOptionValidators, Is.Empty);
        Assert.That(command.SummaryText, Is.EqualTo(""));
        Assert.That(command.DescriptionText, Is.EqualTo(""));
        Assert.That(command.UsageText, Is.EqualTo(""));
        Assert.That(command.VersionText, Is.EqualTo(""));
        Assert.That(command.VersionProviderFunc, Is.Null);
        Assert.That(command.GroupName, Is.EqualTo(""));
        Assert.That(command.IsHidden, Is.False);
        Assert.That(command.RequiresSubcommand, Is.False);
        Assert.That(command.IsBranch, Is.False);
    }

    [Test]
    public void ArgOption_InitializesOptionModel()
    {
        ArgOption option = new ArgOption("--configuration");

        Assert.That(option.Name, Is.EqualTo("--configuration"));
        Assert.That(option.Kind, Is.EqualTo(ArgSymbolKind.Option));
        Assert.That(option.Aliases, Is.EqualTo(new string[] { "--configuration" }));
        Assert.That(option.Choices, Is.Empty);
        Assert.That(option.ValueNormalizer, Is.Null);
        Assert.That(option.ChoiceConverter, Is.Null);
        Assert.That(option.ChoiceValidationStage, Is.EqualTo(ArgChoiceValidationStage.Raw));
        Assert.That(option.ConflictNames, Is.Empty);
        Assert.That(option.RequiredNames, Is.Empty);
        Assert.That(option.Completions, Is.Empty);
        Assert.That(option.DescriptionText, Is.EqualTo(""));
        Assert.That(option.ValueName, Is.EqualTo(""));
        Assert.That(option.DefaultValue, Is.EqualTo(""));
        Assert.That(option.FallbackValue, Is.EqualTo(""));
        Assert.That(option.EnvName, Is.EqualTo(""));
        Assert.That(option.DeprecatedMessage, Is.EqualTo(""));
        Assert.That(option.ValueArity.Minimum, Is.EqualTo(0));
        Assert.That(option.ValueArity.Maximum, Is.EqualTo(0));
        Assert.That(option.ParentCommand, Is.Null);
        Assert.That(option.IsGlobal, Is.False);
        Assert.That(option.IsRequired, Is.False);
        Assert.That(option.IsHidden, Is.False);
        Assert.That(option.IsFlag, Is.False);
        Assert.That(option.IsNegatable, Is.False);
        Assert.That(option.HasValue, Is.False);
        Assert.That(option.ValueIsOptional, Is.False);
        Assert.That(option.IsRepeatable, Is.False);
        Assert.That(option.IsCount, Is.False);
        Assert.That(option.HasDefault, Is.False);
        Assert.That(option.HasFallback, Is.False);
        Assert.That(option.HasEnv, Is.False);
    }

    [Test]
    public void ArgArgument_InitializesArgumentModel()
    {
        ArgArgument argument = new ArgArgument("project");

        Assert.That(argument.Name, Is.EqualTo("project"));
        Assert.That(argument.Kind, Is.EqualTo(ArgSymbolKind.Argument));
        Assert.That(argument.Choices, Is.Empty);
        Assert.That(argument.ValueNormalizer, Is.Null);
        Assert.That(argument.ChoiceConverter, Is.Null);
        Assert.That(argument.ChoiceValidationStage, Is.EqualTo(ArgChoiceValidationStage.Raw));
        Assert.That(argument.Completions, Is.Empty);
        Assert.That(argument.DescriptionText, Is.EqualTo(""));
        Assert.That(argument.DefaultValue, Is.EqualTo(""));
        Assert.That(argument.EnvName, Is.EqualTo(""));
        Assert.That(argument.DeprecatedMessage, Is.EqualTo(""));
        Assert.That(argument.ValueArity.Minimum, Is.EqualTo(0));
        Assert.That(argument.ValueArity.Maximum, Is.EqualTo(1));
        Assert.That(argument.IsRequired, Is.False);
        Assert.That(argument.IsVariadic, Is.False);
        Assert.That(argument.HasDefault, Is.False);
        Assert.That(argument.HasEnv, Is.False);
    }

    [Test]
    public void Enums_DefineStableModelValues()
    {
        Assert.That((int)ArgSymbolKind.None, Is.EqualTo(0));
        Assert.That((int)ArgSymbolKind.Command, Is.EqualTo(1));
        Assert.That((int)ArgSymbolKind.Option, Is.EqualTo(2));
        Assert.That((int)ArgSymbolKind.Argument, Is.EqualTo(3));

        Assert.That((int)ArgValueSource.None, Is.EqualTo(0));
        Assert.That((int)ArgValueSource.CommandLine, Is.EqualTo(1));
        Assert.That((int)ArgValueSource.Default, Is.EqualTo(2));
        Assert.That((int)ArgValueSource.Environment, Is.EqualTo(3));
        Assert.That((int)ArgValueSource.Fallback, Is.EqualTo(4));
        Assert.That((int)ArgValueSource.Implied, Is.EqualTo(5));

        Assert.That((int)ArgChoiceValidationStage.Raw, Is.EqualTo(0));
        Assert.That((int)ArgChoiceValidationStage.Normalized, Is.EqualTo(1));
        Assert.That((int)ArgChoiceValidationStage.Converted, Is.EqualTo(2));
    }

    static void CreateNegativeMinimumArity()
    {
        new ArgArity(-1, 1);
    }

    static void CreateMaximumBelowMinimumArity()
    {
        new ArgArity(2, 1);
    }
}
