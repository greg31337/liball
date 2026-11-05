using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgTokenizerTests
{
    [Test]
    public void ArgToken_ConstructorStoresRawTextKindIndexAndDefaultConsumedState()
    {
        ArgToken token = new ArgToken("--configuration=Release", ArgTokenKind.LongOption, 3);

        AssertToken(token, "--configuration=Release", ArgTokenKind.LongOption, 3, false);
    }

    [Test]
    public void ArgToken_ConstructorRejectsNullRawText()
    {
        Assert.Throws<System.ArgumentNullException>(delegate { new ArgToken(null!, ArgTokenKind.Positional, 0); });
    }

    [Test]
    public void Tokenize_PreservesRawTextOrderIndexesAndConsumedState()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();
        string[] argv = new string[] { "build", "--configuration=Release", "-v", "project.csproj" };

        List<ArgToken> tokens = tokenizer.Tokenize(argv);

        Assert.That(tokens.Count, Is.EqualTo(4));
        AssertToken(tokens[0], "build", ArgTokenKind.Positional, 0, false);
        AssertToken(tokens[1], "--configuration=Release", ArgTokenKind.LongOption, 1, false);
        AssertToken(tokens[2], "-v", ArgTokenKind.ShortOption, 2, false);
        AssertToken(tokens[3], "project.csproj", ArgTokenKind.Positional, 3, false);
    }

    [Test]
    public void MarkConsumed_UpdatesTokenConsumedFlag()
    {
        ArgToken token = new ArgToken("--verbose", ArgTokenKind.LongOption, 0);

        token.MarkConsumed();

        Assert.That(token.Consumed, Is.True);
    }

    [Test]
    public void Tokenize_ClassifiesEndOfOptionsAndFollowingTokensAsPositionals()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();
        string[] argv = new string[] { "--verbose", "--", "--literal", "-x" };

        List<ArgToken> tokens = tokenizer.Tokenize(argv);

        AssertToken(tokens[0], "--verbose", ArgTokenKind.LongOption, 0, false);
        AssertToken(tokens[1], "--", ArgTokenKind.EndOfOptions, 1, false);
        AssertToken(tokens[2], "--literal", ArgTokenKind.Positional, 2, false);
        AssertToken(tokens[3], "-x", ArgTokenKind.Positional, 3, false);
    }

    [Test]
    public void Tokenize_TreatsLoneDashAndEmptyStringAsPositionals()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();
        string[] argv = new string[] { "-", "" };

        List<ArgToken> tokens = tokenizer.Tokenize(argv);

        AssertToken(tokens[0], "-", ArgTokenKind.Positional, 0, false);
        AssertToken(tokens[1], "", ArgTokenKind.Positional, 1, false);
    }

    [Test]
    public void Tokenize_UsesSlashOptionSetting()
    {
        ArgTokenizer defaultTokenizer = new ArgTokenizer();
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        ArgTokenizer slashTokenizer = ArgTokenizer.WithSettings(settings);

        List<ArgToken> defaultTokens = defaultTokenizer.Tokenize(new string[] { "/verbose" });
        List<ArgToken> slashTokens = slashTokenizer.Tokenize(new string[] { "/verbose" });

        AssertToken(defaultTokens[0], "/verbose", ArgTokenKind.Positional, 0, false);
        AssertToken(slashTokens[0], "/verbose", ArgTokenKind.SlashOption, 0, false);
    }

    [Test]
    public void Tokenize_UsesConfiguredEndOfOptionsDelimiter()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EndOfOptionsDelimiter = "++";
        ArgTokenizer tokenizer = ArgTokenizer.WithSettings(settings);

        List<ArgToken> tokens = tokenizer.Tokenize(new string[] { "--name", "++", "--literal" });

        AssertToken(tokens[0], "--name", ArgTokenKind.LongOption, 0, false);
        AssertToken(tokens[1], "++", ArgTokenKind.EndOfOptions, 1, false);
        AssertToken(tokens[2], "--literal", ArgTokenKind.Positional, 2, false);
    }

    [Test]
    public void Tokenize_EmptyArgvReturnsNoTokens()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();

        List<ArgToken> tokens = tokenizer.Tokenize(new string[] { });

        Assert.That(tokens.Count, Is.EqualTo(0));
    }

    [Test]
    public void Tokenize_RejectsNullArgv()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();

        Assert.Throws<System.ArgumentNullException>(delegate { tokenizer.Tokenize(null!); });
    }

    [Test]
    public void Classify_CoversKindsBeforeAndAfterEndOfOptions()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.EnableSlashOptions = true;
        ArgTokenizer tokenizer = ArgTokenizer.WithSettings(settings);

        Assert.That(tokenizer.Classify("build", false), Is.EqualTo(ArgTokenKind.Positional));
        Assert.That(tokenizer.Classify("--name", false), Is.EqualTo(ArgTokenKind.LongOption));
        Assert.That(tokenizer.Classify("-n", false), Is.EqualTo(ArgTokenKind.ShortOption));
        Assert.That(tokenizer.Classify("/name", false), Is.EqualTo(ArgTokenKind.SlashOption));
        Assert.That(tokenizer.Classify("--", false), Is.EqualTo(ArgTokenKind.EndOfOptions));
        Assert.That(tokenizer.Classify("--name", true), Is.EqualTo(ArgTokenKind.Positional));
    }

    [Test]
    public void Classify_RejectsNullRawText()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();

        Assert.Throws<System.ArgumentNullException>(delegate { tokenizer.Classify(null!, false); });
    }

    [Test]
    public void OptionTokenHelpersUseRawTokenShape()
    {
        ArgTokenizer tokenizer = new ArgTokenizer();

        Assert.That(tokenizer.IsLongOptionToken("--"), Is.False);
        Assert.That(tokenizer.IsLongOptionToken("--name"), Is.True);
        Assert.That(tokenizer.IsShortOptionToken("-"), Is.False);
        Assert.That(tokenizer.IsShortOptionToken("-n"), Is.True);
        Assert.That(tokenizer.IsShortOptionToken("--name"), Is.False);
        Assert.That(tokenizer.IsSlashOptionToken("/"), Is.False);
        Assert.That(tokenizer.IsSlashOptionToken("/name"), Is.True);
    }

    [Test]
    public void TokenKind_DefinesStableValues()
    {
        Assert.That((int)ArgTokenKind.Positional, Is.EqualTo(1));
        Assert.That((int)ArgTokenKind.LongOption, Is.EqualTo(2));
        Assert.That((int)ArgTokenKind.ShortOption, Is.EqualTo(3));
        Assert.That((int)ArgTokenKind.SlashOption, Is.EqualTo(4));
        Assert.That((int)ArgTokenKind.EndOfOptions, Is.EqualTo(5));
    }

    static void AssertToken(ArgToken token, string rawText, ArgTokenKind kind, int index, bool consumed)
    {
        Assert.That(token.RawText, Is.EqualTo(rawText));
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Index, Is.EqualTo(index));
        Assert.That(token.Consumed, Is.EqualTo(consumed));
    }
}
