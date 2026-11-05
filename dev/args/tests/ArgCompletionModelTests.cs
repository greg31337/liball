using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCompletionModelTests
{
    [Test]
    public void Constructor_InitializesEmptyCandidate()
    {
        ArgCompletion completion = new ArgCompletion("", ArgCompletionKind.None);

        Assert.That(completion.Text, Is.EqualTo(""));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.None));
        Assert.That(completion.DescriptionText, Is.EqualTo(""));
        Assert.That(completion.SourceName, Is.EqualTo(""));
        Assert.That(completion.Command, Is.Null);
        Assert.That(completion.Option, Is.Null);
        Assert.That(completion.Argument, Is.Null);
    }

    [Test]
    public void Constructor_StoresTextAndKind()
    {
        ArgCompletion completion = new ArgCompletion("build", ArgCompletionKind.Command).Description("Build projects.");

        Assert.That(completion.Text, Is.EqualTo("build"));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Command));
        Assert.That(completion.DescriptionText, Is.EqualTo("Build projects."));
    }

    [Test]
    public void ForCommand_CreatesCommandCandidate()
    {
        ArgCommand command = new ArgCommand("build").Summary("Build projects.");

        ArgCompletion completion = ArgCompletion.ForCommand(command);

        Assert.That(completion.Text, Is.EqualTo("build"));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Command));
        Assert.That(completion.DescriptionText, Is.EqualTo("Build projects."));
        Assert.That(completion.SourceName, Is.EqualTo("build"));
        Assert.That(completion.Command, Is.SameAs(command));
        Assert.That(completion.Option, Is.Null);
        Assert.That(completion.Argument, Is.Null);
    }

    [Test]
    public void ForOption_CreatesOptionCandidateForAlias()
    {
        ArgOption option = new ArgOption("--configuration").Alias("-c").Description("Build configuration.");

        ArgCompletion completion = ArgCompletion.ForOption(option, "-c");

        Assert.That(completion.Text, Is.EqualTo("-c"));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Option));
        Assert.That(completion.DescriptionText, Is.EqualTo("Build configuration."));
        Assert.That(completion.SourceName, Is.EqualTo("--configuration"));
        Assert.That(completion.Command, Is.Null);
        Assert.That(completion.Option, Is.SameAs(option));
        Assert.That(completion.Argument, Is.Null);
    }

    [Test]
    public void ForOptionValue_CreatesStaticValueCandidateForOption()
    {
        ArgOption option = new ArgOption("--configuration").Complete("Debug");

        ArgCompletion completion = ArgCompletion.ForOptionValue(option, option.Completions[0]);

        Assert.That(completion.Text, Is.EqualTo("Debug"));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Value));
        Assert.That(completion.SourceName, Is.EqualTo("--configuration"));
        Assert.That(completion.Option, Is.SameAs(option));
        Assert.That(completion.Argument, Is.Null);
    }

    [Test]
    public void ForArgumentValue_CreatesStaticValueCandidateForArgument()
    {
        ArgArgument argument = new ArgArgument("project").Complete("src");

        ArgCompletion completion = ArgCompletion.ForArgumentValue(argument, argument.Completions[0]);

        Assert.That(completion.Text, Is.EqualTo("src"));
        Assert.That(completion.Kind, Is.EqualTo(ArgCompletionKind.Value));
        Assert.That(completion.SourceName, Is.EqualTo("project"));
        Assert.That(completion.Option, Is.Null);
        Assert.That(completion.Argument, Is.SameAs(argument));
    }

    [Test]
    public void CompletionKind_DefinesStableValues()
    {
        Assert.That((int)ArgCompletionKind.None, Is.EqualTo(0));
        Assert.That((int)ArgCompletionKind.Command, Is.EqualTo(1));
        Assert.That((int)ArgCompletionKind.Option, Is.EqualTo(2));
        Assert.That((int)ArgCompletionKind.Value, Is.EqualTo(3));
    }
}
