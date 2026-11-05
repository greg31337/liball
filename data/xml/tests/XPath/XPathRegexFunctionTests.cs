using xml;

namespace xml.Tests;

[TestFixture]
public class XPathRegexFunctionTests
{
    [TestCase("matches('abracadabra', 'bra')", true)]
    [TestCase("matches('abracadabra', '^a.*a$')", true)]
    [TestCase("matches('abracadabra', '^bra')", false)]
    [TestCase("matches('helloworld', 'hello world', 'x')", true)]
    [TestCase("matches('helloworld', 'hello[ ]world', 'x')", false)]
    [TestCase("matches('hello world', 'hello\\ sworld', 'x')", true)]
    [TestCase("matches('hello world', 'hello world', 'x')", false)]
    [TestCase("matches('abcd', '.*', 'q')", false)]
    [TestCase("matches('Mr. B. Obama', 'B. OBAMA', 'iq')", true)]
    [TestCase("matches('Mum', '([md])[aeiou]\\1', 'i')", true)]
    [TestCase("matches('mom', '([md])[aeiou]\\1', 'i')", true)]
    [TestCase("matches('Dad', '([md])[aeiou]\\1', 'i')", true)]
    [TestCase("matches('DUD', '([md])[aeiou]\\1', 'i')", true)]
    public void MatchesReturnsXPathExampleResults(string expression, bool expected)
    {
        Assert.That(Evaluate(expression).AtomicValues![0].BooleanValue, Is.EqualTo(expected));
    }

    [Test]
    public void MatchesUsesXPathRegexFlagsAnchorsAndOptionalInput()
    {
        XdmAtomicValue[] values = Evaluate("(matches((), '^$'), matches('abracadabra', 'bra'), matches('Line\nTWO', '^two$', 'im'), matches('a+b', 'a+b', 'q'), matches('A😀B', '😀'), matches('A', 'a', 'ii'))").AtomicValues!;

        Assert.That(values.Select(value => value.BooleanValue).ToArray(), Is.EqualTo(new[] { true, true, true, true, true, true }));
    }

    [Test]
    public void ReplaceProcessesAllMatchesCapturesAndReplacementReferences()
    {
        XdmAtomicValue[] values = Evaluate("(replace('abracadabra', 'bra', '[$0]'), replace('abcd', '(a)(b)?', '$2-$1-$3'), replace('ab', '(a)(b)', '$23'), replace('ab', '(?:a)(b)', '$1'), replace('aaaa', 'a+?', 'x'))").AtomicValues!;

        Assert.That(values.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "a[bra]cada[bra]", "b-a-cd", "b3", "b", "xxxx" }));
    }

    [Test]
    public void ReplaceSupportsEscapedAndQuotedReplacementText()
    {
        XdmAtomicValue[] values = Evaluate("(replace('a', 'a', '\\$\\\\'), replace('a+b', 'a+b', '$\\', 'q'))").AtomicValues!;

        Assert.That(values.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "$\\", "$\\" }));
    }

    [TestCase("replace('abracadabra', 'bra', '*')", "a*cada*")]
    [TestCase("replace('abracadabra', 'a.*a', '*')", "*")]
    [TestCase("replace('abracadabra', 'a.*?a', '*')", "*c*bra")]
    [TestCase("replace('abracadabra', 'a', '')", "brcdbr")]
    [TestCase("replace('abracadabra', 'a(.)', 'a$1$1')", "abbraccaddabbra")]
    [TestCase("replace('AAAA', 'A+', 'b')", "b")]
    [TestCase("replace('AAAA', 'A+?', 'b')", "bbbb")]
    [TestCase("replace('darted', '^(.*?)d(.*)$', '$1c$2')", "carted")]
    [TestCase("replace('abcd', '(ab)|(a)', '[1=$1][2=$2]')", "[1=ab][2=]cd")]
    [TestCase("replace('a/b/c', '/', '$', 'q')", "a$b$c")]
    [TestCase("replace('a\\b\\c', '\\', '\\\\', 'q')", "a\\\\b\\\\c")]
    public void ReplaceReturnsXPathExampleResults(string expression, string expected)
    {
        Assert.That(Evaluate(expression).AtomicValues![0].StringValue, Is.EqualTo(expected));
    }

    [Test]
    public void TokenizePreservesExplicitEmptyTokensAndProvidesOneArgumentForm()
    {
        XdmSequence explicitTokens = Evaluate("tokenize('a,,b,', ',')");
        XdmSequence foldedTokens = Evaluate("tokenize('aXbxb', 'x', 'i')");
        XdmSequence words = Evaluate("tokenize('  red \n blue  ')");
        XdmSequence empty = Evaluate("tokenize('', ',')");

        Assert.Multiple(() =>
        {
            Assert.That(explicitTokens.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "a", "", "b", "" }));
            Assert.That(foldedTokens.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "a", "b", "b" }));
            Assert.That(words.AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(new[] { "red", "blue" }));
            Assert.That(empty.Count, Is.Zero);
        });
    }

    [TestCase("tokenize(' red green blue ')", new[] { "red", "green", "blue" })]
    [TestCase("tokenize('The cat sat on the mat', '\\s+')", new[] { "The", "cat", "sat", "on", "the", "mat" })]
    [TestCase("tokenize(' red green blue ', '\\s+')", new[] { "", "red", "green", "blue", "" })]
    [TestCase("tokenize('1, 15, 24, 50', ',\\s*')", new[] { "1", "15", "24", "50" })]
    [TestCase("tokenize('1,15,,24,50,', ',')", new[] { "1", "15", "", "24", "50", "" })]
    [TestCase("tokenize('Some unparsed <br> HTML <BR> text', '\\s*<br>\\s*', 'i')", new[] { "Some unparsed", "HTML", "text" })]
    [TestCase("tokenize('12.3.5.6', '.', 'q')", new[] { "12", "3", "5", "6" })]
    public void TokenizeReturnsXPathExampleResults(string expression, string[] expected)
    {
        Assert.That(Evaluate(expression).AtomicValues!.Select(value => value.StringValue).ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void AnalyzeStringBuildsNamespacedNestedCaptureTree()
    {
        XmlNode root = Evaluate("analyze-string('xabcdz', '(a(b))(c)')").Nodes![0];
        XmlNode[] children = root.Children!;
        XmlNode match = children[1];
        XmlNode groupOne = match.Children![0];
        XmlNode groupTwo = groupOne.Children![1];
        XmlNode groupThree = match.Children![1];

        Assert.Multiple(() =>
        {
            Assert.That(root.Name!.LocalName, Is.EqualTo("analyze-string-result"));
            Assert.That(root.Name.NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(root.Parent, Is.Null);
            Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
            Assert.That(root.NamespaceBindings![0].Prefix, Is.Empty);
            Assert.That(root.NamespaceBindings[0].NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(children.Select(node => node.Name!.LocalName).ToArray(), Is.EqualTo(new[] { "non-match", "match", "non-match" }));
            Assert.That(children[0].Children![0].Value, Is.EqualTo("x"));
            Assert.That(children[2].Children![0].Value, Is.EqualTo("dz"));
            Assert.That(groupOne.Name!.LocalName, Is.EqualTo("group"));
            Assert.That(groupOne.Attributes![0].Name!.LocalName, Is.EqualTo("nr"));
            Assert.That(groupOne.Attributes[0].Name!.NamespaceUri, Is.Null);
            Assert.That(groupOne.Attributes[0].Value, Is.EqualTo("1"));
            Assert.That(groupOne.Children![0].Value, Is.EqualTo("a"));
            Assert.That(groupTwo.Attributes![0].Value, Is.EqualTo("2"));
            Assert.That(groupTwo.Children![0].Value, Is.EqualTo("b"));
            Assert.That(groupThree.Attributes![0].Value, Is.EqualTo("3"));
            Assert.That(groupThree.Children![0].Value, Is.EqualTo("c"));
            Assert.That(match.Name!.NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
            Assert.That(groupTwo.Name!.NamespaceUri, Is.EqualTo(XPathNamespaces.Functions));
        });
    }

    [Test]
    public void AnalyzeStringPartitionsRepeatedMatchesAndHandlesEmptyInput()
    {
        XmlNode root = Evaluate("analyze-string('a1b22', '\\d+')").Nodes![0];
        XmlNode emptyRoot = Evaluate("analyze-string((), 'x')").Nodes![0];

        Assert.Multiple(() =>
        {
            Assert.That(root.Children!.Select(node => node.Name!.LocalName).ToArray(), Is.EqualTo(new[] { "non-match", "match", "non-match", "match" }));
            Assert.That(root.Children!.Select(NodeStringValue).ToArray(), Is.EqualTo(new[] { "a", "1", "b", "22" }));
            Assert.That(emptyRoot.Children, Is.Empty);
        });
    }

    [Test]
    public void AnalyzeStringBuildsTheXPathDateExampleTree()
    {
        XmlNode root = Evaluate("analyze-string('2008-12-03', '^(\\d+)\\-(\\d+)\\-(\\d+)$')").Nodes![0];
        XmlNode match = root.Children![0];

        Assert.Multiple(() =>
        {
            Assert.That(root.Children, Has.Length.EqualTo(1));
            Assert.That(match.Name!.LocalName, Is.EqualTo("match"));
            Assert.That(match.Children!.Select(node => node.Name == null ? "text" : node.Name.LocalName).ToArray(), Is.EqualTo(new[] { "group", "text", "group", "text", "group" }));
            Assert.That(match.Children!.Select(NodeStringValue).ToArray(), Is.EqualTo(new[] { "2008", "-", "12", "-", "03" }));
            Assert.That(match.Children!.Where(node => node.Name != null).Select(node => node.Attributes![0].Value).ToArray(), Is.EqualTo(new[] { "1", "2", "3" }));
        });
    }

    [Test]
    public void AnalyzeStringBuildsTheXPathRepeatedMatchExampleTree()
    {
        XmlNode root = Evaluate("analyze-string('A1,C15,,D24, X50,', '([A-Z])([0-9]+)')").Nodes![0];

        Assert.Multiple(() =>
        {
            Assert.That(root.Children!.Select(node => node.Name!.LocalName).ToArray(), Is.EqualTo(new[] { "match", "non-match", "match", "non-match", "match", "non-match", "match", "non-match" }));
            Assert.That(root.Children!.Select(NodeStringValue).ToArray(), Is.EqualTo(new[] { "A1", ",", "C15", ",,", "D24", ", ", "X50", "," }));
            Assert.That(root.Children!.Where(node => node.Name!.LocalName == "match").All(node => node.Children!.Length == 2), Is.True);
        });
    }

    [TestCase("matches('x', 'x', 'z')", XPathErrorCodes.FORX0001)]
    [TestCase("replace('x', 'x', 'y', 'z')", XPathErrorCodes.FORX0001)]
    [TestCase("tokenize('x', 'x', 'z')", XPathErrorCodes.FORX0001)]
    [TestCase("analyze-string('x', 'x', 'z')", XPathErrorCodes.FORX0001)]
    [TestCase("matches('x', '[')", XPathErrorCodes.FORX0002)]
    [TestCase("replace('x', '[', 'y')", XPathErrorCodes.FORX0002)]
    [TestCase("tokenize('x', '[')", XPathErrorCodes.FORX0002)]
    [TestCase("analyze-string('x', '[')", XPathErrorCodes.FORX0002)]
    [TestCase("replace('abracadabra', '.*?', '$1')", XPathErrorCodes.FORX0003)]
    [TestCase("tokenize('abba', '.?')", XPathErrorCodes.FORX0003)]
    [TestCase("analyze-string('abba', '.?')", XPathErrorCodes.FORX0003)]
    [TestCase("replace('x', 'x', '$')", XPathErrorCodes.FORX0004)]
    [TestCase("replace('x', 'x', '$x')", XPathErrorCodes.FORX0004)]
    [TestCase("replace('x', 'x', '\\')", XPathErrorCodes.FORX0004)]
    [TestCase("replace('x', 'x', '\\x')", XPathErrorCodes.FORX0004)]
    public void ReportsEveryForxErrorFromApplicableRegexFunctions(string expression, string expectedCode)
    {
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate(expression))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void ReportsForxErrorsForFlagsPatternsZeroLengthAndReplacements()
    {
        XPathDynamicException invalidFlag = Assert.Throws<XPathDynamicException>(() => Evaluate("matches('x', 'x', 'z')"))!;
        XPathDynamicException invalidPattern = Assert.Throws<XPathDynamicException>(() => Evaluate("matches('x', '[')"))!;
        XPathDynamicException replaceEmpty = Assert.Throws<XPathDynamicException>(() => Evaluate("replace('x', 'a*', 'y')"))!;
        XPathDynamicException tokenizeEmpty = Assert.Throws<XPathDynamicException>(() => Evaluate("tokenize('x', 'a?')"))!;
        XPathDynamicException analyzeEmpty = Assert.Throws<XPathDynamicException>(() => Evaluate("analyze-string('x', 'a*')"))!;
        XPathDynamicException invalidDollar = Assert.Throws<XPathDynamicException>(() => Evaluate("replace('x', 'x', '$x')"))!;
        XPathDynamicException invalidEscape = Assert.Throws<XPathDynamicException>(() => Evaluate("replace('x', 'x', '\\x')"))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalidFlag.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0001));
            Assert.That(invalidPattern.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0002));
            Assert.That(replaceEmpty.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0003));
            Assert.That(tokenizeEmpty.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0003));
            Assert.That(analyzeEmpty.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0003));
            Assert.That(invalidDollar.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0004));
            Assert.That(invalidEscape.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORX0004));
        });
    }

    [Test]
    public void AnalyzeStringHasOnlySupportedArities()
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("analyze-string('x')", new XPathStaticContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    private static XdmSequence Evaluate(string source)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, new XPathStaticContext());
        return new XPathEvaluator().EvaluateXPath(expression, new XPathDynamicContext()).Value!;
    }

    private static string NodeStringValue(XmlNode node)
    {
        if (node.Value != null)
        {
            return node.Value;
        }

        string result = "";
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            result = result + NodeStringValue(children[index]);
            index = index + 1;
        }

        return result;
    }
}
