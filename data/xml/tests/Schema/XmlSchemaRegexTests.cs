using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaRegexTests
{
    [TestCase("A.*Z", "Albert Z", true)]
    [TestCase("A.*Z", "Albert Zed", false)]
    [TestCase(".*A{3}.*", "BAAAB", true)]
    [TestCase(".*A{3}.*", "BAAB", false)]
    [TestCase("(a|b)+", "abba", true)]
    [TestCase("(a|b)+", "abc", false)]
    [TestCase("\\n\\r\\t\\\\\\|\\.\\?\\*\\+\\(\\)\\{\\}\\-\\[\\]\\^", "\n\r\t\\|.?*+(){}-[]^", true)]
    public void MatchesXmlSchemaSyntaxExamplesWithImplicitAnchoring(string pattern, string value, bool expected)
    {
        XmlSchemaRegexProgram program = Compile(pattern, "", false);

        Assert.That(new XmlSchemaRegexMatcher().IsEntireXmlSchemaRegexMatch(program, value), Is.EqualTo(expected));
    }

    [Test]
    public void MatchesCharacterClassRangesSubtractionAndNameEscapes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Matches("^[a-z-[aeiou]]+$", "rhythm", "", true), Is.True);
            Assert.That(Matches("^[a-z-[aeiou]]+$", "hello", "", true), Is.False);
            Assert.That(Matches("^[\\i-[:]][\\c-[:]]*$", "valid-name", "", true), Is.True);
            Assert.That(Matches("^[\\i-[:]][\\c-[:]]*$", ":invalid", "", true), Is.False);
            Assert.That(Matches("^[^0-9]+$", "letters", "", true), Is.True);
            Assert.That(Matches("^[^0-9]+$", "letter2", "", true), Is.False);
            Assert.That(Matches("^[a--[b]]+$", "a-a", "", true), Is.True);
            Assert.That(Matches("^[a--[b]]+$", "b", "", true), Is.False);
        });
    }

    [Test]
    public void RejectsAmbiguousUnescapedHyphensAfterRanges()
    {
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex("[0-9-.]", "", true);

        Assert.That(compilation.Success, Is.False);
    }

    [Test]
    public void MatchesUnicodeCategoriesBlocksAndPredefinedClasses()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Matches("^\\p{Lu}\\p{Ll}+$", "Ωmega", "", true), Is.True);
            Assert.That(Matches("^\\p{Lu}+$", "lower", "i", true), Is.False);
            Assert.That(Matches("^\\P{Lu}+$", "lower", "", true), Is.True);
            Assert.That(Matches("^\\p{IsEmoticons}$", "😀", "", true), Is.True);
            Assert.That(Matches("^\\p{IsOldItalic}$", "𐌀", "", true), Is.True);
            Assert.That(Matches("^\\d+$", "١٢٣", "", true), Is.True);
            Assert.That(Matches("^\\s$", "\u00a0", "", true), Is.False);
            Assert.That(Matches("^\\w+$", "nameA1", "", true), Is.True);
            Assert.That(Matches("^\\w+$", "name_1", "", true), Is.False);
            Assert.That(Matches("^\\w+$", "name-1", "", true), Is.False);
        });
    }

    [Test]
    public void AppliesCaseVariantsToRangesNegationAndSubtraction()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Matches("^[A-Z]$", "K", "i", true), Is.True);
            Assert.That(Matches("^[K]$", "k", "i", true), Is.True);
            Assert.That(Matches("^[Α-Ω]$", "Ω", "i", true), Is.True);
            Assert.That(Matches("^[Ω]$", "ω", "i", true), Is.True);
            Assert.That(Matches("^[À-Ö]$", "Å", "i", true), Is.True);
            Assert.That(Matches("^[Å]$", "å", "i", true), Is.True);
            Assert.That(Matches("^[A-Z-[IO]]$", "a", "i", true), Is.True);
            Assert.That(Matches("^[A-Z-[IO]]$", "i", "i", true), Is.False);
            Assert.That(Matches("^[A-Z-[IO]]$", "o", "i", true), Is.False);
            Assert.That(Matches("^[^Q]$", "q", "i", true), Is.False);
            Assert.That(Matches("^[^\\p{N}\\p{L}]+$", " -!", "", true), Is.True);
            Assert.That(Matches("^[^\\p{N}\\p{L}]+$", " !A", "", true), Is.False);
        });
    }

    [Test]
    public void TreatsSupplementaryCharactersAsSingleRegexCharacters()
    {
        XmlSchemaRegexMatch match = Find("^(😀)(.)$", "😀𐐀", "", true);

        Assert.Multiple(() =>
        {
            Assert.That(match.Success, Is.True);
            Assert.That(match.Length, Is.EqualTo(4));
            Assert.That(match.Captures[1], Is.EqualTo("😀"));
            Assert.That(match.CaptureIndexes[1], Is.EqualTo(0));
            Assert.That(match.CaptureLengths[1], Is.EqualTo(2));
            Assert.That(match.Captures[2], Is.EqualTo("𐐀"));
            Assert.That(Matches("^.$", "😀", "", true), Is.True);
            Assert.That(Matches("^.$", "😀A", "", true), Is.False);
        });
    }

    [Test]
    public void AppliesGreedyAndReluctantQuantifierPriority()
    {
        XmlSchemaRegexMatch greedy = Find("a+", "zaaa", "", true);
        XmlSchemaRegexMatch reluctant = Find("a+?", "zaaa", "", true);
        XmlSchemaRegexMatch bounded = Find("^(ab){2,3}$", "ababab", "", true);

        Assert.Multiple(() =>
        {
            Assert.That(greedy.Index, Is.EqualTo(1));
            Assert.That(greedy.Length, Is.EqualTo(3));
            Assert.That(reluctant.Index, Is.EqualTo(1));
            Assert.That(reluctant.Length, Is.EqualTo(1));
            Assert.That(bounded.Success, Is.True);
            Assert.That(bounded.Captures[1], Is.EqualTo("ab"));
        });
    }

    [Test]
    public void MatchesLongRepeatedInputsWithoutConsumingTheCallStack()
    {
        string input = new string('a', 20000);

        Assert.That(Matches("^a+$", input, "", true), Is.True);
    }

    [Test]
    public void CapturesGroupsAndMatchesBackReferences()
    {
        XmlSchemaRegexMatch singleQuoted = Find("^('|\").*?\\1$", "'value'", "", true);
        XmlSchemaRegexMatch doubleQuoted = Find("^('|\").*?\\1$", "\"value\"", "", true);

        Assert.Multiple(() =>
        {
            Assert.That(singleQuoted.Success, Is.True);
            Assert.That(singleQuoted.Captures[1], Is.EqualTo("'"));
            Assert.That(doubleQuoted.Success, Is.True);
            Assert.That(doubleQuoted.Captures[1], Is.EqualTo("\""));
            Assert.That(Matches("^(a)(?:b)\\1$", "aba", "", true), Is.True);
            Assert.That(Matches("^(a)(?:b)\\1$", "abb", "", true), Is.False);
        });
    }

    [Test]
    public void AppliesAnchorsAndAllSupportedFlags()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Matches("^abc$", "ABC", "i", true), Is.True);
            Assert.That(Matches("^a.b$", "a\nb", "s", true), Is.True);
            Assert.That(Matches("^two$", "one\ntwo\nthree", "m", true), Is.True);
            Assert.That(Matches(" ^ a b c $ ", "abc", "x", true), Is.True);
            Assert.That(Matches("^hello\\ sworld$", "hello world", "x", true), Is.True);
            Assert.That(Matches("^a#b$", "a#b", "x", true), Is.True);
            Assert.That(Matches("^a.b$", "^a.b$", "q", true), Is.True);
            Assert.That(Matches("A^B$C", "A^B$C", "", false), Is.True);
            Assert.That(Matches("^abc$", "xabc", "", true), Is.False);
        });
    }

    [Test]
    public void FindsTheFirstLeftmostMatchAtOrAfterAUtf16Offset()
    {
        XmlSchemaRegexProgram program = Compile("(a+)", "", true);
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        XmlSchemaRegexMatch first = matcher.FindXmlSchemaRegexMatch(program, "😀aa aa", 0);
        XmlSchemaRegexMatch second = matcher.FindXmlSchemaRegexMatch(program, "😀aa aa", first.Index + first.Length);

        Assert.Multiple(() =>
        {
            Assert.That(first.Index, Is.EqualTo(2));
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first.Captures[1], Is.EqualTo("aa"));
            Assert.That(second.Index, Is.EqualTo(5));
            Assert.That(second.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void DoesNotCreateAMultilineAnchorAfterAFinalNewline()
    {
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        XmlSchemaRegexProgram start = Compile("^", "m", true);
        XmlSchemaRegexProgram end = Compile("$", "m", true);

        Assert.Multiple(() =>
        {
            Assert.That(matcher.FindXmlSchemaRegexMatch(start, "\n", 1).Success, Is.False);
            Assert.That(matcher.FindXmlSchemaRegexMatch(end, "\n", 1).Success, Is.False);
            Assert.That(matcher.FindXmlSchemaRegexMatch(end, "a\nb", 0).Index, Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesXmlSchemaWholeStringSemantics()
    {
        XmlSchemaRegexProgram program = Compile("[0-9]+", "", false);
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();

        Assert.Multiple(() =>
        {
            Assert.That(matcher.IsEntireXmlSchemaRegexMatch(program, "123"), Is.True);
            Assert.That(matcher.IsEntireXmlSchemaRegexMatch(program, "x123"), Is.False);
            Assert.That(matcher.IsEntireXmlSchemaRegexMatch(program, "123x"), Is.False);
        });
    }

    [TestCase("[a-")]
    [TestCase("[a[b]")]
    [TestCase("(abc")]
    [TestCase("a{3,2}")]
    [TestCase("\\p{NotAProperty}")]
    [TestCase("\\p{Emoticons}")]
    [TestCase("\\p{isBasicLatin}")]
    [TestCase("\\1(a)")]
    [TestCase("abc\\")]
    public void RejectsMalformedExpressions(string pattern)
    {
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(pattern, "", true);

        Assert.Multiple(() =>
        {
            Assert.That(compilation.Success, Is.False);
            Assert.That(compilation.Program, Is.Null);
            Assert.That(compilation.ErrorMessage, Does.Contain("UTF-16 offset"));
        });
    }

    [Test]
    public void RejectsUnknownFlagsAndXPathOnlySyntaxInSchemaMode()
    {
        XmlSchemaRegexCompilationResult badFlag = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex("abc", "z", true);
        XmlSchemaRegexCompilationResult reluctantSchemaQuantifier = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex("a+?", "", false);
        XmlSchemaRegexCompilationResult schemaBackReference = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex("(a)\\1", "", false);
        XmlSchemaRegexCompilationResult schemaNonCapturingGroup = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex("(?:a)", "", false);

        Assert.Multiple(() =>
        {
            Assert.That(badFlag.Success, Is.False);
            Assert.That(reluctantSchemaQuantifier.Success, Is.False);
            Assert.That(schemaBackReference.Success, Is.False);
            Assert.That(schemaNonCapturingGroup.Success, Is.False);
        });
    }

    private static bool Matches(string pattern, string value, string flags, bool xpathMode)
    {
        XmlSchemaRegexProgram program = Compile(pattern, flags, xpathMode);
        return new XmlSchemaRegexMatcher().IsXmlSchemaRegexMatch(program, value);
    }

    private static XmlSchemaRegexMatch Find(string pattern, string value, string flags, bool xpathMode)
    {
        XmlSchemaRegexProgram program = Compile(pattern, flags, xpathMode);
        return new XmlSchemaRegexMatcher().FindXmlSchemaRegexMatch(program, value, 0);
    }

    private static XmlSchemaRegexProgram Compile(string pattern, string flags, bool xpathMode)
    {
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(pattern, flags, xpathMode);
        Assert.That(compilation.Success, Is.True, compilation.ErrorMessage);
        Assert.That(compilation.Program, Is.Not.Null);
        return compilation.Program!;
    }
}
