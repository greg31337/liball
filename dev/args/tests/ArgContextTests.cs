using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgContextTests
{
    [Test]
    public void Empty_InitializesEmptyContext()
    {
        ArgContext context = ArgContext.Empty();

        Assert.That(context.Command, Is.Not.Null);
        Assert.That(context.ParseResult, Is.Not.Null);
        Assert.That(context.Values, Is.Not.Null);
        Assert.That(context.ParseResult.Command, Is.SameAs(context.Command));
        Assert.That(context.ParseResult.Occurrences, Is.Empty);
        Assert.That(context.Values.ParseResult, Is.SameAs(context.ParseResult));
    }

    [Test]
    public void Constructor_BindsParseResultValuesAndResolvedCommand()
    {
        ArgCommand root = new ArgCommand("tool");
        ArgCommand build = root.Command("build");
        build.Option("--configuration").Value("name");
        ArgParseResult parseResult = new ArgParser(root).Parse(new string[] { "build", "--configuration", "Release" });

        ArgContext context = new ArgContext(parseResult);

        Assert.That(context.ParseResult, Is.SameAs(parseResult));
        Assert.That(context.Command, Is.SameAs(build));
        Assert.That(context.Values.ParseResult, Is.SameAs(parseResult));
        Assert.That(context.Values.String("--configuration"), Is.EqualTo("Release"));
    }

    [Test]
    public void Constructor_RejectsNullParseResult()
    {
        Assert.That(new TestDelegate(CreateNullContext), Throws.TypeOf<System.ArgumentNullException>());
    }

    [Test]
    public void Command_HandlerStorageUsesBuiltInFuncDelegates()
    {
        ArgCommand command = new ArgCommand("tool");
        System.Func<ArgContext, int> handler = new System.Func<ArgContext, int>(ReturnCommandNameLength);
        System.Func<ArgContext, int> before = new System.Func<ArgContext, int>(ReturnZero);
        System.Func<ArgContext, int> after = new System.Func<ArgContext, int>(ReturnSeven);

        ArgCommand returned = command.Handler(handler).Before(before).After(after);
        ArgContext context = new ArgContext(new ArgParser(command).Parse(new string[] { }));

        Assert.That(returned, Is.SameAs(command));
        Assert.That(command.HandlerFunc, Is.SameAs(handler));
        Assert.That(command.BeforeFunc, Is.SameAs(before));
        Assert.That(command.AfterFunc, Is.SameAs(after));
        Assert.That(command.HandlerFunc(context), Is.EqualTo(4));
        Assert.That(command.BeforeFunc(context), Is.EqualTo(0));
        Assert.That(command.AfterFunc(context), Is.EqualTo(7));
    }

    [Test]
    public void Command_HandlerStorageCanBeReplaced()
    {
        ArgCommand command = new ArgCommand("tool");
        System.Func<ArgContext, int> first = new System.Func<ArgContext, int>(ReturnZero);
        System.Func<ArgContext, int> second = new System.Func<ArgContext, int>(ReturnSeven);

        command.Handler(first);
        command.Handler(second);

        Assert.That(command.HandlerFunc, Is.SameAs(second));
        Assert.That(command.HandlerFunc(ArgContext.Empty()), Is.EqualTo(7));
    }

    static void CreateNullContext()
    {
        new ArgContext(null!);
    }

    static int ReturnCommandNameLength(ArgContext context)
    {
        return context.Command.Name.Length;
    }

    static int ReturnZero(ArgContext context)
    {
        return 0;
    }

    static int ReturnSeven(ArgContext context)
    {
        return 7;
    }
}
