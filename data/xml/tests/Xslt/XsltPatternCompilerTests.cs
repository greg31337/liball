using xml;

namespace xml.Tests;

[TestFixture]
public class XsltPatternCompilerTests
{
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CompilesAndMatchesRelativeAndDescendantPathsWithPredicates()
    {
        XmlDocument document = Parse("<root><book><chapter role='main'/><chapter/></book><appendix><group><section/></group></appendix></root>");
        XmlNode firstChapter = FindElement(document, "chapter", 0);
        XmlNode secondChapter = FindElement(document, "chapter", 1);
        XmlNode section = FindElement(document, "section", 0);
        XsltMatchPatternPlan chapter = Pattern("book/chapter[@role='main']", "", null);
        XsltMatchPatternPlan descendant = Pattern("appendix//section", "", null);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(chapter, firstChapter), Is.True);
            Assert.That(Matches(chapter, secondChapter), Is.False);
            Assert.That(Matches(descendant, section), Is.True);
            Assert.That(chapter.IsDeferred, Is.False);
            Assert.That(chapter.IsMotionless, Is.True);
            Assert.That(chapter.EquivalentExpression, Is.Not.Null);
        });
    }

    [Test]
    public void SupportsAllPermittedForwardAxes()
    {
        XmlDocument document = Parse("<root xmlns:p='urn:p'><group><p:item code='x'/></group></root>");
        XmlNode item = FindElement(document, "item", 0);
        XmlNode code = item.Attributes!.Single(attribute => attribute.Name!.LocalName == "code");

        Assert.Multiple(() =>
        {
            Assert.That(Matches(Pattern("child::p:item", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Matches(Pattern("descendant::p:item", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Matches(Pattern("attribute::code", "", null), code), Is.True);
            Assert.That(Matches(Pattern("self::p:item", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Matches(Pattern("descendant-or-self::p:item", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Pattern("namespace::*", "", null).Expression, Is.Not.Null);
        });
    }

    [Test]
    public void MatchesNameWildcardsAndKindTests()
    {
        XmlDocument document = Parse("<root xmlns:p='urn:p'><p:item id='a'>text<!--note--><?go now?></p:item></root>");
        XmlNode item = FindElement(document, "item", 0);
        XmlNode attribute = item.Attributes!.Single(node => node.Name!.LocalName == "id");
        XmlNode text = item.Children!.Single(node => node.Kind == XmlNodeKind.Text);
        XmlNode[] itemChildren = item.Children!;
        XmlNode comment = itemChildren.Single(node => node.Kind == XmlNodeKind.Comment);
        XmlNode instruction = itemChildren.Single(node => node.Kind == XmlNodeKind.ProcessingInstruction);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(Pattern("p:*", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Matches(Pattern("*:item", "", null), item), Is.True);
            Assert.That(Matches(Pattern("Q{urn:p}*", "", null), item), Is.True);
            Assert.That(Matches(Pattern("element(p:item)", "xmlns:p='urn:p'", null), item), Is.True);
            Assert.That(Matches(Pattern("@attribute(id)", "", null), attribute), Is.True);
            Assert.That(Matches(Pattern("text()", "", null), text), Is.True);
            Assert.That(Matches(Pattern("comment()", "", null), comment), Is.True);
            Assert.That(Matches(Pattern("processing-instruction('go')", "", null), instruction), Is.True);
        });
    }

    [Test]
    public void AppliesUnionIntersectionAndExceptAsPatternSetOperators()
    {
        XmlDocument document = Parse("<root><chapter/><section/><other/></root>");
        XmlNode chapter = FindElement(document, "chapter", 0);
        XmlNode section = FindElement(document, "section", 0);
        XmlNode other = FindElement(document, "other", 0);
        XsltMatchPatternPlan union = Pattern("chapter | section", "", null);
        XsltMatchPatternPlan intersection = Pattern("* intersect chapter", "", null);
        XsltMatchPatternPlan except = Pattern("* except chapter", "", null);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(union, chapter), Is.True);
            Assert.That(Matches(union, section), Is.True);
            Assert.That(Matches(union, other), Is.False);
            Assert.That(Matches(intersection, chapter), Is.True);
            Assert.That(Matches(intersection, section), Is.False);
            Assert.That(Matches(except, chapter), Is.False);
            Assert.That(Matches(except, other), Is.True);
        });
    }

    [Test]
    public void MatchesRootedDocumentAndIdPatternsAndPlansKeys()
    {
        XmlDocument document = Parse("<root><item xml:id='target'/><item/></root>");
        XmlNode root = FindElement(document, "root", 0);
        XmlNode target = FindElement(document, "item", 0);
        XsltMatchPatternPlan documentPattern = Pattern("/", "", null);
        XsltMatchPatternPlan rootPattern = Pattern("/*", "", null);
        XsltMatchPatternPlan idPattern = Pattern("id('target')", "", null);
        XsltMatchPatternPlan keyPattern = Pattern("key('by-name', 'target')", "", null);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(documentPattern, document.DocumentNode!), Is.True);
            Assert.That(Matches(rootPattern, root), Is.True);
            Assert.That(Matches(idPattern, target), Is.True);
            Assert.That(idPattern.HasRootedPath, Is.True);
            Assert.That(idPattern.IsMotionless, Is.False);
            Assert.That(keyPattern.HasKeyCall, Is.True);
            Assert.That(keyPattern.HasRootedPath, Is.True);
            Assert.That(keyPattern.EquivalentExpression, Is.Not.Null);
        });
    }

    [Test]
    public void PredicatePatternsMatchNodesAndAtomicItems()
    {
        XmlDocument document = Parse("<root><chapter/></root>");
        XmlNode chapter = FindElement(document, "chapter", 0);
        XsltMatchPatternPlan nodePattern = Pattern(".[self::chapter]", "", null);
        XsltMatchPatternPlan atomicPattern = Pattern(".[. instance of xs:string]", "xmlns:xs='" + Xsd + "'", null);
        XdmAtomicValue text = new XdmFactory().CreateXdmStringAtomicValue("value", XdmAtomicType.String);
        XdmSequence atomic = new XdmSequenceOperations().CreateXdmAtomicSequence(text);

        Assert.Multiple(() =>
        {
            Assert.That(nodePattern.IsPredicatePattern, Is.True);
            Assert.That(Matches(nodePattern, chapter), Is.True);
            Assert.That(new XsltPatternMatcher().MatchesXsltPattern(atomicPattern, atomic, new XPathDynamicContext()), Is.True);
            Assert.That(new XsltPatternMatcher().MatchesXsltPattern(nodePattern, atomic, new XPathDynamicContext()), Is.False);
        });
    }

    [Test]
    public void PreservesPositionalPredicateSemantics()
    {
        XmlDocument document = Parse("<root><chapter/><other/><chapter/></root>");
        XmlNode first = FindElement(document, "chapter", 0);
        XmlNode second = FindElement(document, "chapter", 1);
        XsltMatchPatternPlan pattern = Pattern("chapter[1]", "", null);

        Assert.Multiple(() =>
        {
            Assert.That(Matches(pattern, first), Is.True);
            Assert.That(Matches(pattern, second), Is.False);
            Assert.That(pattern.IsMotionless, Is.False);
        });
    }

    [Test]
    public void RelativePatternsMatchParentlessNodes()
    {
        XmlDocument document = Parse("<chapter role='main'/>");
        XmlNode chapter = document.DocumentElement!;
        chapter.Parent = null;

        Assert.Multiple(() =>
        {
            Assert.That(Matches(Pattern("chapter", "", null), chapter), Is.True);
            Assert.That(Matches(Pattern("chapter[@role='main']", "", null), chapter), Is.True);
            Assert.That(Matches(Pattern("node()", "", null), chapter), Is.True);
            Assert.That(Matches(Pattern("descendant::chapter", "", null), chapter), Is.False);
            Assert.That(Matches(Pattern("descendant-or-self::chapter", "", null), chapter), Is.True);
        });
    }

    [Test]
    public void ImplicitDocumentTestMatchesAParentlessDocumentButExplicitChildDoesNot()
    {
        XmlDocument document = Parse("<root/>");
        XmlNode documentNode = document.DocumentNode!;

        Assert.Multiple(() =>
        {
            Assert.That(Matches(Pattern("document-node()", "", null), documentNode), Is.True);
            Assert.That(Matches(Pattern("child::document-node()", "", null), documentNode), Is.False);
        });
    }

    [TestCase("chapter[@role='main']", true)]
    [TestCase("chapter[base-uri()]", true)]
    [TestCase("chapter[ancestor::*[@xml:lang]]", true)]
    [TestCase("chapter[1]", false)]
    [TestCase("chapter[child]", false)]
    [TestCase("chapter[. = 'text']", false)]
    [TestCase("chapter[starts-with(., 'x')]", false)]
    [TestCase("chapter[position() = 1]", false)]
    [TestCase("id('target')", false)]
    public void ClassifiesMotionlessPatterns(string source, bool expected)
    {
        Assert.That(Pattern(source, "", null).IsMotionless, Is.EqualTo(expected));
    }

    [Test]
    public void CompilesSchemaElementAndAttributeTests()
    {
        XmlSchemaSet schemas = CompileSchema();
        XsltMatchPatternPlan element = Pattern("schema-element(s:root)", "xmlns:s='urn:s'", schemas);
        XsltMatchPatternPlan attribute = Pattern("@schema-attribute(s:code)", "xmlns:s='urn:s'", schemas);
        XPathAstNode elementStep = element.Expression!;
        XPathAstNode attributeStep = attribute.Expression!;

        Assert.Multiple(() =>
        {
            Assert.That(elementStep.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.SchemaElement));
            Assert.That(elementStep.NodeTest.ResolvedName!.NamespaceUri, Is.EqualTo("urn:s"));
            Assert.That(attributeStep.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.SchemaAttribute));
            Assert.That(attributeStep.NodeTest.ResolvedName!.LocalName, Is.EqualTo("code"));
        });
    }

    [TestCase("parent::item")]
    [TestCase("preceding-sibling::item")]
    [TestCase("1 + 1")]
    [TestCase("contains('a', 'b')")]
    [TestCase("id(concat('a', 'b'))")]
    [TestCase("root(.)")]
    [TestCase("(item, other)")]
    [TestCase("item[")]
    public void RejectsExpressionsOutsideThePatternGrammar(string source)
    {
        XsltStaticException error = Assert.Throws<XsltStaticException>(() => Pattern(source, "", null))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XsltErrorCodes.XTSE0340));
    }

    [Test]
    public void TreatsDynamicErrorsDuringPatternEvaluationAsNoMatch()
    {
        XmlDocument document = Parse("<root><chapter/></root>");
        XsltMatchPatternPlan pattern = Pattern("chapter[error()]", "", null);

        Assert.That(Matches(pattern, FindElement(document, "chapter", 0)), Is.False);
    }

    private static XsltMatchPatternPlan Pattern(string source, string attributes, XmlSchemaSet? schemas)
    {
        string stylesheet = "<xsl:stylesheet xmlns:xsl='" + Xsl + "' " + attributes + " version='3.0'><xsl:template match=\"" + EscapeAttribute(source) + "\"/></xsl:stylesheet>";
        XsltCompileOptions options = new XsltCompileOptions
        {
            BaseUri = "https://example.test/pattern.xsl",
            ConformanceLevel = schemas == null ? XsltConformanceLevel.Basic : XsltConformanceLevel.SchemaAware,
            SchemaSet = schemas,
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        XsltExecutable executable = new XsltCompiler().CompileXslt(new XmlInput { Text = stylesheet }, options, new XmlDefaultResolver());
        return ((XsltStylesheetPlan)executable.Plan!).Templates.Single().MatchPlan!;
    }

    private static bool Matches(XsltMatchPatternPlan pattern, XmlNode node)
    {
        XdmSequence candidate = new XdmSequenceOperations().CreateXdmNodeSequence(node);
        return new XsltPatternMatcher().MatchesXsltPattern(pattern, candidate, new XPathDynamicContext());
    }

    private static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;");
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }

    private static XmlNode FindElement(XmlDocument document, string localName, int occurrence)
    {
        List<XmlNode> matches = new List<XmlNode>();
        AddElements(document.DocumentNode!, localName, matches);
        return matches[occurrence];
    }

    private static void AddElements(XmlNode node, string localName, List<XmlNode> matches)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name != null && node.Name.LocalName == localName)
        {
            matches.Add(node);
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AddElements(children[index], localName, matches);
            index = index + 1;
        }
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' targetNamespace='urn:s' xmlns:s='urn:s'>" +
            "<xs:element name='root' type='xs:string'/><xs:attribute name='code' type='xs:string'/></xs:schema>";
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions
        {
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied }
        };
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Text = schema, BaseUri = "https://example.test/schema.xsd" }], options, new XmlDefaultResolver());
    }
}
