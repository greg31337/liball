using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgFluentModelTests
{
    [Test]
    public void ArgApp_BuildsRootCommandModel()
    {
        ArgApp app = new ArgApp("tool");
        ArgApp returnedApp = app.Description("Example app").Version("1.2.3");
        ArgCommand command = app.Command("build");
        ArgOption option = app.Option("--verbose");
        ArgArgument argument = app.Argument("project");

        Assert.That(returnedApp, Is.SameAs(app));
        Assert.That(app.Name, Is.EqualTo("tool"));
        Assert.That(app.DescriptionText, Is.EqualTo("Example app"));
        Assert.That(app.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(app.RootCommand.Name, Is.EqualTo("tool"));
        Assert.That(app.RootCommand.DescriptionText, Is.EqualTo("Example app"));
        Assert.That(app.RootCommand.VersionText, Is.EqualTo("1.2.3"));
        Assert.That(app.RootCommand.Commands, Is.EqualTo(new ArgCommand[] { command }));
        Assert.That(app.RootCommand.Options, Is.EqualTo(new ArgOption[] { option }));
        Assert.That(app.RootCommand.Arguments, Is.EqualTo(new ArgArgument[] { argument }));
    }

    [Test]
    public void ArgCommand_FluentMethodsMutateCommandAndCreateChildren()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand returnedCommand = root.Description("Long description")
            .Summary("Short")
            .Alias("t")
            .Example("tool build")
            .Hidden()
            .RequiredSubcommand()
            .Handler(new System.Func<ArgContext, int>(ReturnSeven))
            .Before(new System.Func<ArgContext, int>(ReturnZero))
            .After(new System.Func<ArgContext, int>(ReturnZero));
        ArgCommand child = root.Command("build");
        ArgOption localOption = root.Option("--configuration");
        ArgOption globalOption = root.GlobalOption("--verbose");
        ArgArgument argument = root.Argument("project");

        Assert.That(returnedCommand, Is.SameAs(root));
        Assert.That(root.DescriptionText, Is.EqualTo("Long description"));
        Assert.That(root.SummaryText, Is.EqualTo("Short"));
        Assert.That(root.Aliases, Is.EqualTo(new string[] { "t" }));
        Assert.That(root.Examples, Is.EqualTo(new string[] { "tool build" }));
        Assert.That(root.IsHidden, Is.True);
        Assert.That(root.RequiresSubcommand, Is.True);
        Assert.That(root.HandlerFunc, Is.Not.Null);
        Assert.That(root.BeforeFunc, Is.Not.Null);
        Assert.That(root.AfterFunc, Is.Not.Null);
        Assert.That(root.HandlerFunc(ArgContext.Empty()), Is.EqualTo(7));
        Assert.That(child.Parent, Is.SameAs(root));
        Assert.That(localOption.ParentCommand, Is.SameAs(root));
        Assert.That(localOption.IsGlobal, Is.False);
        Assert.That(globalOption.ParentCommand, Is.SameAs(root));
        Assert.That(globalOption.IsGlobal, Is.True);
        Assert.That(root.Commands, Is.EqualTo(new ArgCommand[] { child }));
        Assert.That(root.Options, Is.EqualTo(new ArgOption[] { localOption }));
        Assert.That(root.GlobalOptions, Is.EqualTo(new ArgOption[] { globalOption }));
        Assert.That(root.Arguments, Is.EqualTo(new ArgArgument[] { argument }));
    }

    [Test]
    public void ArgOption_FluentMethodsMutateOption()
    {
        ArgOption option = new ArgOption("--configuration");
        ArgOption returnedOption = option.Alias("-c")
            .Description("Build configuration")
            .Required()
            .Hidden()
            .Value("name")
            .Arity(1, 2)
            .Repeatable()
            .Default("Debug")
            .Fallback("Release")
            .Env("CONFIGURATION")
            .Choice("Debug")
            .Choice("Release")
            .Conflicts("--fast")
            .Requires("--project")
            .Deprecated("Use --mode")
            .Complete("Debug");

        Assert.That(returnedOption, Is.SameAs(option));
        Assert.That(option.Aliases, Is.EqualTo(new string[] { "--configuration", "-c" }));
        Assert.That(option.DescriptionText, Is.EqualTo("Build configuration"));
        Assert.That(option.IsRequired, Is.True);
        Assert.That(option.IsHidden, Is.True);
        Assert.That(option.ValueName, Is.EqualTo("name"));
        Assert.That(option.HasValue, Is.True);
        Assert.That(option.ValueIsOptional, Is.False);
        Assert.That(option.IsFlag, Is.False);
        Assert.That(option.ValueArity.Minimum, Is.EqualTo(1));
        Assert.That(option.ValueArity.Maximum, Is.EqualTo(2));
        Assert.That(option.IsRepeatable, Is.True);
        Assert.That(option.DefaultValue, Is.EqualTo("Debug"));
        Assert.That(option.HasDefault, Is.True);
        Assert.That(option.FallbackValue, Is.EqualTo("Release"));
        Assert.That(option.HasFallback, Is.True);
        Assert.That(option.EnvName, Is.EqualTo("CONFIGURATION"));
        Assert.That(option.HasEnv, Is.True);
        Assert.That(option.Choices, Is.EqualTo(new string[] { "Debug", "Release" }));
        Assert.That(option.ConflictNames, Is.EqualTo(new string[] { "--fast" }));
        Assert.That(option.RequiredNames, Is.EqualTo(new string[] { "--project" }));
        Assert.That(option.DeprecatedMessage, Is.EqualTo("Use --mode"));
        Assert.That(option.Completions, Is.EqualTo(new string[] { "Debug" }));
    }

    [Test]
    public void ArgOption_FlagNegatableOptionalValueAndCountSetArity()
    {
        ArgOption flag = new ArgOption("--verbose").Flag();
        ArgOption negatable = new ArgOption("--color").Negatable();
        ArgOption optional = new ArgOption("--log").OptionalValue("level");
        ArgOption count = new ArgOption("-v").Count();

        Assert.That(flag.IsFlag, Is.True);
        Assert.That(flag.HasValue, Is.False);
        Assert.That(flag.ValueArity.Maximum, Is.EqualTo(0));
        Assert.That(negatable.IsNegatable, Is.True);
        Assert.That(negatable.IsFlag, Is.True);
        Assert.That(optional.HasValue, Is.True);
        Assert.That(optional.ValueIsOptional, Is.True);
        Assert.That(optional.ValueArity.Minimum, Is.EqualTo(0));
        Assert.That(optional.ValueArity.Maximum, Is.EqualTo(1));
        Assert.That(count.IsCount, Is.True);
        Assert.That(count.IsFlag, Is.True);
        Assert.That(count.HasValue, Is.False);
    }

    [Test]
    public void ArgArgument_FluentMethodsMutateArgument()
    {
        ArgArgument argument = new ArgArgument("project");
        ArgArgument returnedArgument = argument.Description("Project path")
            .Required()
            .Variadic()
            .Default(".")
            .Env("PROJECT")
            .Choice("src")
            .Deprecated("Use workspace")
            .Complete("src");

        Assert.That(returnedArgument, Is.SameAs(argument));
        Assert.That(argument.DescriptionText, Is.EqualTo("Project path"));
        Assert.That(argument.IsRequired, Is.True);
        Assert.That(argument.IsVariadic, Is.True);
        Assert.That(argument.ValueArity.Minimum, Is.EqualTo(1));
        Assert.That(argument.ValueArity.Maximum, Is.EqualTo(ArgArity.Unbounded));
        Assert.That(argument.DefaultValue, Is.EqualTo("."));
        Assert.That(argument.HasDefault, Is.True);
        Assert.That(argument.EnvName, Is.EqualTo("PROJECT"));
        Assert.That(argument.HasEnv, Is.True);
        Assert.That(argument.Choices, Is.EqualTo(new string[] { "src" }));
        Assert.That(argument.DeprecatedMessage, Is.EqualTo("Use workspace"));
        Assert.That(argument.Completions, Is.EqualTo(new string[] { "src" }));
    }

    [Test]
    public void ArgArgument_OptionalAndArityUpdateRequirementAndVariadicFlags()
    {
        ArgArgument argument = new ArgArgument("files").Arity(2, ArgArity.Unbounded).Optional();

        Assert.That(argument.IsRequired, Is.False);
        Assert.That(argument.IsVariadic, Is.True);
        Assert.That(argument.ValueArity.Minimum, Is.EqualTo(0));
        Assert.That(argument.ValueArity.Maximum, Is.EqualTo(ArgArity.Unbounded));
    }

    static int ReturnSeven(ArgContext context)
    {
        return 7;
    }

    static int ReturnZero(ArgContext context)
    {
        return 0;
    }
}
