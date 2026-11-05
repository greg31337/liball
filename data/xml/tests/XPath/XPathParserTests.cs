using xml;

namespace xml.Tests;

[TestFixture]
public class XPathParserTests
{
    [Test]
    public void ParsesForLetQuantifiedAndIfExpressionsWithBindings()
    {
        XPathAstNode forExpression = Parse("for $x in 1, $p:y in 2 return if ($x) then $p:y else 0");
        XPathAstNode letExpression = Parse("let $x := 1, $y := 2 return $x + $y");
        XPathAstNode someExpression = Parse("some $x in 1, $y in 2 satisfies $x eq $y");
        XPathAstNode everyExpression = Parse("every $x in 1 satisfies $x gt 0");

        Assert.Multiple(() =>
        {
            Assert.That(forExpression.Kind, Is.EqualTo(XPathAstKind.For));
            Assert.That(forExpression.Bindings, Has.Length.EqualTo(2));
            Assert.That(forExpression.Bindings[0].Name, Is.EqualTo("x"));
            Assert.That(forExpression.Bindings[1].Prefix, Is.EqualTo("p"));
            Assert.That(forExpression.Bindings[1].LocalName, Is.EqualTo("y"));
            Assert.That(forExpression.Body!.Kind, Is.EqualTo(XPathAstKind.If));
            Assert.That(forExpression.Body.Condition!.Kind, Is.EqualTo(XPathAstKind.Parenthesized).Or.EqualTo(XPathAstKind.Variable));
            Assert.That(forExpression.Body.ThenExpression!.Kind, Is.EqualTo(XPathAstKind.Variable));
            Assert.That(forExpression.Body.ElseExpression!.Kind, Is.EqualTo(XPathAstKind.IntegerLiteral));
            Assert.That(letExpression.Kind, Is.EqualTo(XPathAstKind.Let));
            Assert.That(letExpression.Bindings, Has.Length.EqualTo(2));
            Assert.That(letExpression.Body!.Kind, Is.EqualTo(XPathAstKind.Addition));
            Assert.That(someExpression.Kind, Is.EqualTo(XPathAstKind.Quantified));
            Assert.That(someExpression.Operator, Is.EqualTo("some"));
            Assert.That(someExpression.Bindings, Has.Length.EqualTo(2));
            Assert.That(someExpression.Body!.Kind, Is.EqualTo(XPathAstKind.ValueComparison));
            Assert.That(everyExpression.Operator, Is.EqualTo("every"));
        });
    }

    [Test]
    public void AppliesLogicalComparisonConcatenationRangeAndArithmeticPrecedence()
    {
        XPathAstNode logical = Parse("1 or 2 and 3");
        XPathAstNode comparison = Parse("1 = 2 || 3 to 4");
        XPathAstNode arithmetic = Parse("1 + 2 * 3 - 4");

        Assert.Multiple(() =>
        {
            Assert.That(logical.Kind, Is.EqualTo(XPathAstKind.Or));
            Assert.That(logical.Left!.Kind, Is.EqualTo(XPathAstKind.IntegerLiteral));
            Assert.That(logical.Right!.Kind, Is.EqualTo(XPathAstKind.And));
            Assert.That(comparison.Kind, Is.EqualTo(XPathAstKind.GeneralComparison));
            Assert.That(comparison.Operator, Is.EqualTo("="));
            Assert.That(comparison.Right!.Kind, Is.EqualTo(XPathAstKind.StringConcatenation));
            Assert.That(comparison.Right.Right!.Kind, Is.EqualTo(XPathAstKind.Range));
            Assert.That(arithmetic.Kind, Is.EqualTo(XPathAstKind.Subtraction));
            Assert.That(arithmetic.Left!.Kind, Is.EqualTo(XPathAstKind.Addition));
            Assert.That(arithmetic.Left.Right!.Kind, Is.EqualTo(XPathAstKind.Multiplication));
        });
    }

    [TestCase("1 and 2 = 3", "And", "GeneralComparison")]
    [TestCase("1 = 2 || 3", "GeneralComparison", "StringConcatenation")]
    [TestCase("1 || 2 to 3", "StringConcatenation", "Range")]
    [TestCase("1 to 2 + 3", "Range", "Addition")]
    [TestCase("1 + 2 * 3", "Addition", "Multiplication")]
    [TestCase("1 * 2 union 3", "Multiplication", "Union")]
    [TestCase("1 union 2 intersect 3", "Union", "Intersect")]
    [TestCase("1 intersect 2 instance of item()", "Intersect", "InstanceOf")]
    [TestCase("1 + 2 => fn:value()", "Addition", "Arrow")]
    [TestCase("-1 ! 2", "UnaryMinus", "SimpleMap")]
    [TestCase("1 ! a/b", "SimpleMap", "Path")]
    public void PreservesEveryAdjacentPrecedenceBoundary(string source, string rootKind, string nestedKind)
    {
        XPathAstNode result = Parse(source);
        XPathAstNode? nested = result.Right == null ? result.Operand : result.Right;

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind.ToString(), Is.EqualTo(rootKind));
            Assert.That(nested, Is.Not.Null);
            Assert.That(nested!.Kind.ToString(), Is.EqualTo(nestedKind));
        });
    }

    [TestCase("1 + 2", "Addition", "+")]
    [TestCase("1 - 2", "Subtraction", "-")]
    [TestCase("1 * 2", "Multiplication", "*")]
    [TestCase("1 div 2", "Division", "div")]
    [TestCase("1 idiv 2", "IntegerDivision", "idiv")]
    [TestCase("1 mod 2", "Modulo", "mod")]
    [TestCase("1 || 2", "StringConcatenation", "||")]
    [TestCase("1 to 2", "Range", "to")]
    [TestCase("1 union 2", "Union", "union")]
    [TestCase("1 | 2", "Union", "|")]
    [TestCase("1 intersect 2", "Intersect", "intersect")]
    [TestCase("1 except 2", "Except", "except")]
    [TestCase("1 ! 2", "SimpleMap", "!")]
    public void CreatesDedicatedAstKindsForBinaryExpressionFamilies(string source, string expectedKind, string expectedOperator)
    {
        XPathAstNode result = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind.ToString(), Is.EqualTo(expectedKind));
            Assert.That(result.Operator, Is.EqualTo(expectedOperator));
            Assert.That(result.Left, Is.Not.Null);
            Assert.That(result.Right, Is.Not.Null);
        });
    }

    [Test]
    public void DistinguishesGeneralValueAndNodeComparisons()
    {
        XPathAstNode general = Parse("1 != 2");
        XPathAstNode value = Parse("1 le 2");
        XPathAstNode identity = Parse("a is b");
        XPathAstNode before = Parse("a << b");
        XPathAstNode after = Parse("a >> b");

        Assert.Multiple(() =>
        {
            Assert.That(general.Kind, Is.EqualTo(XPathAstKind.GeneralComparison));
            Assert.That(general.Operator, Is.EqualTo("!="));
            Assert.That(value.Kind, Is.EqualTo(XPathAstKind.ValueComparison));
            Assert.That(value.Operator, Is.EqualTo("le"));
            Assert.That(identity.Kind, Is.EqualTo(XPathAstKind.NodeComparison));
            Assert.That(identity.Operator, Is.EqualTo("is"));
            Assert.That(before.Kind, Is.EqualTo(XPathAstKind.NodeComparison));
            Assert.That(before.Operator, Is.EqualTo("<<"));
            Assert.That(after.Kind, Is.EqualTo(XPathAstKind.NodeComparison));
            Assert.That(after.Operator, Is.EqualTo(">>"));
        });
    }

    [Test]
    public void AppliesIntersectExceptBeforeLeftAssociativeUnion()
    {
        XPathAstNode result = Parse("1 union 2 intersect 3 except 4 | 5");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Union));
            Assert.That(result.Operator, Is.EqualTo("|"));
            Assert.That(result.Left!.Kind, Is.EqualTo(XPathAstKind.Union));
            Assert.That(result.Left.Operator, Is.EqualTo("union"));
            Assert.That(result.Left.Right!.Kind, Is.EqualTo(XPathAstKind.Except));
            Assert.That(result.Left.Right.Left!.Kind, Is.EqualTo(XPathAstKind.Intersect));
            Assert.That(result.Right!.Value, Is.EqualTo("5"));
        });
    }

    [Test]
    public void BuildsRepeatedBinaryOperatorsLeftAssociatively()
    {
        XPathAstNode subtraction = Parse("1 - 2 - 3");
        XPathAstNode concatenation = Parse("1 || 2 || 3");
        XPathAstNode simpleMap = Parse("1 ! 2 ! 3");
        XPathAstNode unaryPlus = Parse("++1");

        Assert.Multiple(() =>
        {
            Assert.That(subtraction.Left!.Kind, Is.EqualTo(XPathAstKind.Subtraction));
            Assert.That(concatenation.Left!.Kind, Is.EqualTo(XPathAstKind.StringConcatenation));
            Assert.That(simpleMap.Left!.Kind, Is.EqualTo(XPathAstKind.SimpleMap));
            Assert.That(unaryPlus.Kind, Is.EqualTo(XPathAstKind.UnaryPlus));
            Assert.That(unaryPlus.Operand!.Kind, Is.EqualTo(XPathAstKind.UnaryPlus));
        });
    }

    [Test]
    public void ParsesInstanceTreatCastableAndCastTypeOperators()
    {
        XPathAstNode result = Parse("1 cast as xs:int? castable as xs:integer? treat as item()* instance of item()*");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.InstanceOf));
            Assert.That(result.Type!.Name, Is.EqualTo("item"));
            Assert.That(result.Type.Occurrence, Is.EqualTo("*"));
            Assert.That(result.Operand!.Kind, Is.EqualTo(XPathAstKind.TreatAs));
            Assert.That(result.Operand.Type!.Name, Is.EqualTo("item"));
            Assert.That(result.Operand.Type.Occurrence, Is.EqualTo("*"));
            Assert.That(result.Operand.Operand!.Kind, Is.EqualTo(XPathAstKind.CastableAs));
            Assert.That(result.Operand.Operand.Type!.Name, Is.EqualTo("xs:integer"));
            Assert.That(result.Operand.Operand.Type.Prefix, Is.EqualTo("xs"));
            Assert.That(result.Operand.Operand.Type.Occurrence, Is.EqualTo("?"));
            Assert.That(result.Operand.Operand.Operand!.Kind, Is.EqualTo(XPathAstKind.CastAs));
            Assert.That(result.Operand.Operand.Operand.Type!.Name, Is.EqualTo("xs:int"));
        });
    }

    [Test]
    public void ParsesUnarySimpleMapAndLeftAssociativeArrowExpressions()
    {
        XPathAstNode result = Parse("--1 ! 2 => fn:first(3) => $next()");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Arrow));
            Assert.That(result.Right!.Kind, Is.EqualTo(XPathAstKind.Variable));
            Assert.That(result.Right.LocalName, Is.EqualTo("next"));
            Assert.That(result.Children, Is.Empty);
            Assert.That(result.Left!.Kind, Is.EqualTo(XPathAstKind.Arrow));
            Assert.That(result.Left.Right!.Prefix, Is.EqualTo("fn"));
            Assert.That(result.Left.Right.LocalName, Is.EqualTo("first"));
            Assert.That(result.Left.Children, Has.Length.EqualTo(1));
            Assert.That(result.Left.Children[0].Value, Is.EqualTo("3"));
            Assert.That(result.Left.Left!.Kind, Is.EqualTo(XPathAstKind.UnaryMinus));
            Assert.That(result.Left.Left.Operand!.Kind, Is.EqualTo(XPathAstKind.UnaryMinus));
            Assert.That(result.Left.Left.Operand.Operand!.Kind, Is.EqualTo(XPathAstKind.SimpleMap));
        });
    }

    [Test]
    public void ParsesAbsoluteDescendantAndRelativePaths()
    {
        XPathAstNode absolute = Parse("/a//b/c");
        XPathAstNode descendant = Parse("//Q{urn:test}a/@id/..");
        XPathAstNode relative = Parse("a/b");
        XPathAstNode root = Parse("/");

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Kind, Is.EqualTo(XPathAstKind.Path));
            Assert.That(absolute.Absolute, Is.True);
            Assert.That(absolute.LeadingDescendant, Is.False);
            Assert.That(absolute.Children.Select(step => step.LocalName), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(absolute.PathOperators, Is.EqualTo(new[] { "//", "/" }));
            Assert.That(descendant.Absolute, Is.True);
            Assert.That(descendant.LeadingDescendant, Is.True);
            Assert.That(descendant.Children[0].NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(descendant.Children[1].Kind, Is.EqualTo(XPathAstKind.AttributeStep));
            Assert.That(descendant.Children[2].Kind, Is.EqualTo(XPathAstKind.ParentStep));
            Assert.That(relative.Absolute, Is.False);
            Assert.That(relative.PathOperators, Is.EqualTo(new[] { "/" }));
            Assert.That(root.Absolute, Is.True);
            Assert.That(root.Children, Is.Empty);
        });
    }

    [TestCase("child::book", "Child", "Name")]
    [TestCase("descendant::node()", "Descendant", "AnyKind")]
    [TestCase("attribute::id", "Attribute", "Name")]
    [TestCase("self::node()", "Self", "AnyKind")]
    [TestCase("descendant-or-self::node()", "DescendantOrSelf", "AnyKind")]
    [TestCase("following-sibling::item", "FollowingSibling", "Name")]
    [TestCase("following::item", "Following", "Name")]
    [TestCase("namespace::*", "Namespace", "Wildcard")]
    public void ParsesEveryForwardAxis(string source, string expectedAxis, string expectedTest)
    {
        XPathAstNode result = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.AxisStep));
            Assert.That(result.Axis.ToString(), Is.EqualTo(expectedAxis));
            Assert.That(result.NodeTest!.Kind.ToString(), Is.EqualTo(expectedTest));
            Assert.That(result.ReverseAxis, Is.False);
        });
    }

    [TestCase("parent::node()", "Parent", "AnyKind")]
    [TestCase("ancestor::*", "Ancestor", "Wildcard")]
    [TestCase("preceding-sibling::item", "PrecedingSibling", "Name")]
    [TestCase("preceding::item", "Preceding", "Name")]
    [TestCase("ancestor-or-self::node()", "AncestorOrSelf", "AnyKind")]
    public void ParsesEveryReverseAxis(string source, string expectedAxis, string expectedTest)
    {
        XPathAstNode result = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Axis.ToString(), Is.EqualTo(expectedAxis));
            Assert.That(result.NodeTest!.Kind.ToString(), Is.EqualTo(expectedTest));
            Assert.That(result.ReverseAxis, Is.True);
        });
    }

    [Test]
    public void ParsesAbbreviatedChildAttributeAndParentSteps()
    {
        XPathAstNode result = Parse("book/@id/../*");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Path));
            Assert.That(result.Children[0].Kind, Is.EqualTo(XPathAstKind.Name));
            Assert.That(result.Children[0].Axis, Is.EqualTo(XPathAxis.Child));
            Assert.That(result.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Name));
            Assert.That(result.Children[1].Kind, Is.EqualTo(XPathAstKind.AttributeStep));
            Assert.That(result.Children[1].Axis, Is.EqualTo(XPathAxis.Attribute));
            Assert.That(result.Children[2].Kind, Is.EqualTo(XPathAstKind.ParentStep));
            Assert.That(result.Children[2].Axis, Is.EqualTo(XPathAxis.Parent));
            Assert.That(result.Children[2].ReverseAxis, Is.True);
            Assert.That(result.Children[3].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Wildcard));
        });
    }

    [Test]
    public void ParsesEveryNameTestWildcardForm()
    {
        XPathAstNode result = Parse("p:*/*:local/Q{urn:items}*/*");

        Assert.Multiple(() =>
        {
            Assert.That(result.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.NamespaceWildcard));
            Assert.That(result.Children[0].NodeTest!.Prefix, Is.EqualTo("p"));
            Assert.That(result.Children[1].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.LocalNameWildcard));
            Assert.That(result.Children[1].NodeTest!.LocalName, Is.EqualTo("local"));
            Assert.That(result.Children[2].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.UriWildcard));
            Assert.That(result.Children[2].NodeTest!.NamespaceUri, Is.EqualTo("urn:items"));
            Assert.That(result.Children[3].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Wildcard));
        });
    }

    [TestCase("*:(:comment:)local")]
    [TestCase("prefix :*")]
    [TestCase("prefix(:comment:):*")]
    public void RejectsSeparatorsInsideWildcardNameTests(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => Parse(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
    }

    [TestCase("empty-sequence()")]
    [TestCase("item()")]
    [TestCase("typeswitch()")]
    [TestCase("switch()")]
    [TestCase("array()")]
    [TestCase("map()")]
    [TestCase("attribute#0")]
    [TestCase("namespace-node#1")]
    public void RejectsReservedUnprefixedFunctionNames(string source)
    {
        XPathStaticException error = Assert.Throws<XPathStaticException>(() => Parse(source))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
    }

    [Test]
    public void ParsesSimpleAndProcessingInstructionKindTests()
    {
        XPathAstNode result = Parse("node()/text()/comment()/namespace-node()/processing-instruction()/processing-instruction(target)/processing-instruction('xml-stylesheet')");

        Assert.Multiple(() =>
        {
            Assert.That(result.Children.Select(step => step.NodeTest!.Kind), Is.EqualTo(new[]
            {
                XPathNodeTestKind.AnyKind,
                XPathNodeTestKind.Text,
                XPathNodeTestKind.Comment,
                XPathNodeTestKind.NamespaceNode,
                XPathNodeTestKind.ProcessingInstruction,
                XPathNodeTestKind.ProcessingInstruction,
                XPathNodeTestKind.ProcessingInstruction
            }));
            Assert.That(result.Children[4].NodeTest!.Value, Is.Null);
            Assert.That(result.Children[5].NodeTest!.Value, Is.EqualTo("target"));
            Assert.That(result.Children[6].NodeTest!.Value, Is.EqualTo("xml-stylesheet"));
        });
    }

    [Test]
    public void ParsesElementAndAttributeKindTestsWithTypes()
    {
        XPathAstNode result = Parse("element()/element(book)/element(*, xs:string?)/attribute()/attribute(id, xs:ID)");

        Assert.Multiple(() =>
        {
            Assert.That(result.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(result.Children[0].NodeTest!.LocalName, Is.Null);
            Assert.That(result.Children[1].NodeTest!.LocalName, Is.EqualTo("book"));
            Assert.That(result.Children[2].NodeTest!.LocalName, Is.EqualTo("*"));
            Assert.That(result.Children[2].NodeTest!.Type!.Name, Is.EqualTo("xs:string"));
            Assert.That(result.Children[2].NodeTest!.Nillable, Is.True);
            Assert.That(result.Children[3].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Attribute));
            Assert.That(result.Children[4].NodeTest!.LocalName, Is.EqualTo("id"));
            Assert.That(result.Children[4].NodeTest!.Type!.Name, Is.EqualTo("xs:ID"));
        });
    }

    [Test]
    public void ParsesDocumentAndSchemaKindTests()
    {
        XPathAstNode result = Parse("document-node()/document-node(element(root))/document-node(schema-element(p:root))/schema-element(p:item)/schema-attribute(p:id)");

        Assert.Multiple(() =>
        {
            Assert.That(result.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Document));
            Assert.That(result.Children[0].NodeTest!.InnerTest, Is.Null);
            Assert.That(result.Children[1].NodeTest!.InnerTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(result.Children[1].NodeTest!.InnerTest!.LocalName, Is.EqualTo("root"));
            Assert.That(result.Children[2].NodeTest!.InnerTest!.Kind, Is.EqualTo(XPathNodeTestKind.SchemaElement));
            Assert.That(result.Children[2].NodeTest!.InnerTest!.Prefix, Is.EqualTo("p"));
            Assert.That(result.Children[3].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.SchemaElement));
            Assert.That(result.Children[3].NodeTest!.LocalName, Is.EqualTo("item"));
            Assert.That(result.Children[4].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.SchemaAttribute));
            Assert.That(result.Children[4].NodeTest!.LocalName, Is.EqualTo("id"));
        });
    }

    [Test]
    public void AppliesPredicateListsToAxisSteps()
    {
        XPathAstNode result = Parse("child::book[1][@id]/parent::node()[position() eq 1]");
        XPathAstNode childPredicates = result.Children[0];
        XPathAstNode parentPredicate = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(childPredicates.Kind, Is.EqualTo(XPathAstKind.Predicate));
            Assert.That(childPredicates.Left!.Kind, Is.EqualTo(XPathAstKind.Predicate));
            Assert.That(childPredicates.Left.Left!.Axis, Is.EqualTo(XPathAxis.Child));
            Assert.That(childPredicates.Left.Right!.Value, Is.EqualTo("1"));
            Assert.That(childPredicates.Right!.Kind, Is.EqualTo(XPathAstKind.AttributeStep));
            Assert.That(parentPredicate.Kind, Is.EqualTo(XPathAstKind.Predicate));
            Assert.That(parentPredicate.Left!.Axis, Is.EqualTo(XPathAxis.Parent));
            Assert.That(parentPredicate.Left.ReverseAxis, Is.True);
            Assert.That(parentPredicate.Right!.Kind, Is.EqualTo(XPathAstKind.ValueComparison));
        });
    }

    [Test]
    public void DistinguishesKindTestsFromStaticFunctionCalls()
    {
        XPathAstNode result = Parse("(node(), fn:node(), Q{}node())");
        XPathAstNode sequence = result.Operand!;

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Children[0].Kind, Is.EqualTo(XPathAstKind.AxisStep));
            Assert.That(sequence.Children[0].NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.AnyKind));
            Assert.That(sequence.Children[1].Kind, Is.EqualTo(XPathAstKind.StaticFunctionCall));
            Assert.That(sequence.Children[2].Kind, Is.EqualTo(XPathAstKind.StaticFunctionCall));
        });
    }

    [Test]
    public void KeepsContextualKeywordsAvailableAsPathNames()
    {
        XPathAstNode path = Parse("for/let/some/if/return");

        Assert.Multiple(() =>
        {
            Assert.That(path.Kind, Is.EqualTo(XPathAstKind.Path));
            Assert.That(path.Children.Select(step => step.Value), Is.EqualTo(new[] { "for", "let", "some", "if", "return" }));
            Assert.That(path.PathOperators, Is.EqualTo(new[] { "/", "/", "/", "/" }));
        });
    }

    [Test]
    public void PreservesGroupingSequencesAndPreciseSourceRanges()
    {
        string source = "\n  (1, 2 + 3)";

        XPathAstNode result = new XPathParser(source, "urn:expression").ParseXPathExpression();

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(result.Operand!.Kind, Is.EqualTo(XPathAstKind.Sequence));
            Assert.That(result.Operand.Children, Has.Length.EqualTo(2));
            Assert.That(result.Range.Start!.SourceUri, Is.EqualTo("urn:expression"));
            Assert.That(result.Range.Start.CharacterOffset, Is.EqualTo(3));
            Assert.That(result.Range.Start.Line, Is.EqualTo(2));
            Assert.That(result.Range.Start.Column, Is.EqualTo(3));
            Assert.That(result.Range.End!.CharacterOffset, Is.EqualTo(source.Length));
            Assert.That(result.Range.End.Line, Is.EqualTo(2));
            Assert.That(result.Range.End.Column, Is.EqualTo(13));
        });
    }

    [Test]
    public void ParsesIntegerDecimalDoubleAndStringLiteralProductions()
    {
        XPathAstNode result = Parse("(17, 2.5, .75e+2, 'text')").Operand!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Children.Select(item => item.Kind), Is.EqualTo(new[]
            {
                XPathAstKind.IntegerLiteral,
                XPathAstKind.DecimalLiteral,
                XPathAstKind.DoubleLiteral,
                XPathAstKind.StringLiteral
            }));
            Assert.That(result.Children.Select(item => item.Value), Is.EqualTo(new[] { "17", "2.5", ".75e+2", "text" }));
        });
    }

    [Test]
    public void ParsesDetailedKindAndParenthesizedItemTypes()
    {
        XPathAstNode element = Parse("1 instance of (element(Q{urn:elements}book, Q{urn:types}Book?))?");
        XPathAstNode document = Parse("1 treat as document-node(element(root))*");
        XPathAstNode processingInstruction = Parse("1 instance of processing-instruction('target')");

        Assert.Multiple(() =>
        {
            Assert.That(element.Type!.ParenthesizedItemType, Is.True);
            Assert.That(element.Type.Occurrence, Is.EqualTo("?"));
            Assert.That(element.Type.InnerType!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(element.Type.InnerType.NodeTest.NamespaceUri, Is.EqualTo("urn:elements"));
            Assert.That(element.Type.InnerType.NodeTest.LocalName, Is.EqualTo("book"));
            Assert.That(element.Type.InnerType.NodeTest.Type!.NamespaceUri, Is.EqualTo("urn:types"));
            Assert.That(element.Type.InnerType.NodeTest.Nillable, Is.True);
            Assert.That(document.Type!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.Document));
            Assert.That(document.Type.NodeTest.InnerTest!.Kind, Is.EqualTo(XPathNodeTestKind.Element));
            Assert.That(document.Type.Occurrence, Is.EqualTo("*"));
            Assert.That(processingInstruction.Type!.NodeTest!.Value, Is.EqualTo("target"));
        });
    }

    [Test]
    public void ParsesTypedFunctionMapAndArrayTestsRecursively()
    {
        string source = "1 instance of function(xs:string, item()*) as array(map(Q{urn:types}Key, node()+))";

        XPathTypeSyntax type = Parse(source).Type!;
        XPathTypeSyntax emptyParameters = Parse("1 instance of function() as empty-sequence()").Type!;
        XPathPlan compiled = (XPathPlan)new XPathCompiler().CompileXPath(
            "1 instance of function(xs:string) as array(map(xs:string, node()+))",
            new XPathStaticContext { AllowHigherOrderFunctions = true }).Plan!;

        Assert.Multiple(() =>
        {
            Assert.That(type.Name, Is.EqualTo("function"));
            Assert.That(type.WildcardTest, Is.False);
            Assert.That(type.ParameterTypes, Has.Length.EqualTo(2));
            Assert.That(type.ParameterTypes[0].Name, Is.EqualTo("xs:string"));
            Assert.That(type.ParameterTypes[1].Name, Is.EqualTo("item"));
            Assert.That(type.ParameterTypes[1].Occurrence, Is.EqualTo("*"));
            Assert.That(type.ResultType!.Name, Is.EqualTo("array"));
            Assert.That(type.ResultType.MemberType!.Name, Is.EqualTo("map"));
            Assert.That(type.ResultType.MemberType.KeyType!.NamespaceUri, Is.EqualTo("urn:types"));
            Assert.That(type.ResultType.MemberType.KeyType.LocalName, Is.EqualTo("Key"));
            Assert.That(type.ResultType.MemberType.ValueType!.Name, Is.EqualTo("node"));
            Assert.That(type.ResultType.MemberType.ValueType.Occurrence, Is.EqualTo("+"));
            Assert.That(emptyParameters.ParameterTypes, Is.Empty);
            Assert.That(emptyParameters.ResultType!.EmptySequence, Is.True);
            Assert.That(compiled.Root.Type!.ParameterTypes[0].AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(compiled.Root.Type.ResultType!.MemberType!.KeyType!.AtomicType!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(compiled.Root.Type.ResultType.MemberType.ValueType!.NodeTest!.Kind, Is.EqualTo(XPathNodeTestKind.AnyKind));
        });
    }

    [Test]
    public void ValidatesNestedCompositeTypeNamesDuringCompilation()
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath(
            "1 instance of function(Q{urn:missing}Type) as item()",
            new XPathStaticContext { AllowHigherOrderFunctions = true }))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0051));
            Assert.That(exception.Diagnostic.Range!.Start!.CharacterOffset, Is.EqualTo("1 instance of function(".Length));
        });
    }

    [Test]
    public void ParsesEmptyAndWildcardSequenceTypeAlternatives()
    {
        XPathTypeSyntax empty = Parse("() instance of empty-sequence()").Type!;
        XPathTypeSyntax anyFunction = Parse("1 instance of function(*)").Type!;
        XPathTypeSyntax anyMap = Parse("1 instance of map(*)").Type!;
        XPathTypeSyntax anyArray = Parse("1 instance of array(*)").Type!;

        Assert.Multiple(() =>
        {
            Assert.That(empty.EmptySequence, Is.True);
            Assert.That(empty.Occurrence, Is.Null);
            Assert.That(anyFunction.WildcardTest, Is.True);
            Assert.That(anyMap.WildcardTest, Is.True);
            Assert.That(anyArray.WildcardTest, Is.True);
        });
    }

    [Test]
    public void ParsesStaticNamedAndInlineFunctionExpressions()
    {
        XPathAstNode call = Parse("fn:concat('a', 'b')");
        XPathAstNode reference = Parse("Q{urn:functions}join#2");
        XPathAstNode inline = Parse("function($value as xs:string, $p:other) as xs:string {$value}");

        Assert.Multiple(() =>
        {
            Assert.That(call.Kind, Is.EqualTo(XPathAstKind.StaticFunctionCall));
            Assert.That(call.Prefix, Is.EqualTo("fn"));
            Assert.That(call.LocalName, Is.EqualTo("concat"));
            Assert.That(call.Children.Select(argument => argument.Value), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(reference.Kind, Is.EqualTo(XPathAstKind.NamedFunctionReference));
            Assert.That(reference.NamespaceUri, Is.EqualTo("urn:functions"));
            Assert.That(reference.LocalName, Is.EqualTo("join"));
            Assert.That(reference.Arity, Is.EqualTo(2));
            Assert.That(inline.Kind, Is.EqualTo(XPathAstKind.InlineFunction));
            Assert.That(inline.Parameters, Has.Length.EqualTo(2));
            Assert.That(inline.Parameters[0].Name, Is.EqualTo("value"));
            Assert.That(inline.Parameters[0].Type!.Name, Is.EqualTo("xs:string"));
            Assert.That(inline.Parameters[1].Prefix, Is.EqualTo("p"));
            Assert.That(inline.Type!.Name, Is.EqualTo("xs:string"));
            Assert.That(inline.Body!.Kind, Is.EqualTo(XPathAstKind.Variable));
        });
    }

    [Test]
    public void PreservesUriQualifiedNamesInEveryNameBearingProduction()
    {
        XPathAstNode binding = Parse("for $Q{urn:variables}value in 1 return $Q{urn:variables}value");
        XPathAstNode call = Parse("Q{urn:functions}call(Q{urn:functions}item#0)");
        XPathAstNode arrow = Parse("1 => Q{urn:functions}call()");

        Assert.Multiple(() =>
        {
            Assert.That(binding.Bindings[0].NamespaceUri, Is.EqualTo("urn:variables"));
            Assert.That(binding.Bindings[0].LocalName, Is.EqualTo("value"));
            Assert.That(binding.Body!.NamespaceUri, Is.EqualTo("urn:variables"));
            Assert.That(call.NamespaceUri, Is.EqualTo("urn:functions"));
            Assert.That(call.Children[0].Kind, Is.EqualTo(XPathAstKind.NamedFunctionReference));
            Assert.That(call.Children[0].NamespaceUri, Is.EqualTo("urn:functions"));
            Assert.That(arrow.Right!.NamespaceUri, Is.EqualTo("urn:functions"));
        });
    }

    [Test]
    public void WrapsDynamicCallsPredicatesAndLookupsInSourceOrder()
    {
        XPathAstNode result = Parse("fn:factory(?, 2)($function)[1]?name");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Lookup));
            Assert.That(result.Right!.Value, Is.EqualTo("name"));
            Assert.That(result.Left!.Kind, Is.EqualTo(XPathAstKind.Predicate));
            Assert.That(result.Left.Right!.Value, Is.EqualTo("1"));
            Assert.That(result.Left.Left!.Kind, Is.EqualTo(XPathAstKind.DynamicFunctionCall));
            Assert.That(result.Left.Left.Children, Has.Length.EqualTo(1));
            Assert.That(result.Left.Left.Children[0].Kind, Is.EqualTo(XPathAstKind.Variable));
            Assert.That(result.Left.Left.Operand!.Kind, Is.EqualTo(XPathAstKind.StaticFunctionCall));
            Assert.That(result.Left.Left.Operand.Children[0].Kind, Is.EqualTo(XPathAstKind.ArgumentPlaceholder));
            Assert.That(result.Range.Start!.CharacterOffset, Is.EqualTo(0));
            Assert.That(result.Range.End!.CharacterOffset, Is.EqualTo("fn:factory(?, 2)($function)[1]?name".Length));
        });
    }

    [Test]
    public void ParsesArrowAndDynamicPartialApplications()
    {
        XPathAstNode arrow = Parse("1 => fn:add(?, 2)");
        XPathAstNode dynamic = Parse("($function)(?, ?key)");

        Assert.Multiple(() =>
        {
            Assert.That(arrow.Kind, Is.EqualTo(XPathAstKind.Arrow));
            Assert.That(arrow.Children[0].Kind, Is.EqualTo(XPathAstKind.ArgumentPlaceholder));
            Assert.That(arrow.Children[1].Value, Is.EqualTo("2"));
            Assert.That(dynamic.Kind, Is.EqualTo(XPathAstKind.DynamicFunctionCall));
            Assert.That(dynamic.Operand!.Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(dynamic.Children[0].Kind, Is.EqualTo(XPathAstKind.ArgumentPlaceholder));
            Assert.That(dynamic.Children[1].Kind, Is.EqualTo(XPathAstKind.UnaryLookup));
            Assert.That(dynamic.Children[1].Right!.Value, Is.EqualTo("key"));
        });
    }

    [Test]
    public void ParsesVariablesContextAndEmptyParenthesizedExpressions()
    {
        XPathAstNode result = Parse("($value, ., ())");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(result.Operand!.Kind, Is.EqualTo(XPathAstKind.Sequence));
            Assert.That(result.Operand.Children[0].Kind, Is.EqualTo(XPathAstKind.Variable));
            Assert.That(result.Operand.Children[0].LocalName, Is.EqualTo("value"));
            Assert.That(result.Operand.Children[1].Kind, Is.EqualTo(XPathAstKind.ContextItem));
            Assert.That(result.Operand.Children[2].Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(result.Operand.Children[2].Operand, Is.Null);
        });
    }

    [Test]
    public void ParsesMapAndArrayConstructors()
    {
        XPathAstNode map = Parse("map {'a': 1, $key: (2, 3)}");
        XPathAstNode square = Parse("[1, 2 + 3]");
        XPathAstNode curly = Parse("array {1, 2 + 3}");
        XPathAstNode emptyMap = Parse("map {}");
        XPathAstNode emptySquare = Parse("[]");
        XPathAstNode emptyCurly = Parse("array {}");

        Assert.Multiple(() =>
        {
            Assert.That(map.Kind, Is.EqualTo(XPathAstKind.MapConstructor));
            Assert.That(map.MapEntries, Has.Length.EqualTo(2));
            Assert.That(map.MapEntries[0].Key.Value, Is.EqualTo("a"));
            Assert.That(map.MapEntries[0].Value.Value, Is.EqualTo("1"));
            Assert.That(map.MapEntries[1].Key.Kind, Is.EqualTo(XPathAstKind.Variable));
            Assert.That(map.MapEntries[1].Value.Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(square.Kind, Is.EqualTo(XPathAstKind.SquareArrayConstructor));
            Assert.That(square.Children, Has.Length.EqualTo(2));
            Assert.That(square.Children[1].Kind, Is.EqualTo(XPathAstKind.Addition));
            Assert.That(curly.Kind, Is.EqualTo(XPathAstKind.CurlyArrayConstructor));
            Assert.That(curly.Body!.Kind, Is.EqualTo(XPathAstKind.Sequence));
            Assert.That(emptyMap.MapEntries, Is.Empty);
            Assert.That(emptySquare.Children, Is.Empty);
            Assert.That(emptyCurly.Body, Is.Null);
        });
    }

    [Test]
    public void ParsesEveryLookupKeySpecifierForm()
    {
        XPathAstNode result = Parse("(?name, ?2, ?($key), ?*, map {}?(1 + 1))");
        XPathAstNode sequence = result.Operand!;

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Children, Has.Length.EqualTo(5));
            Assert.That(sequence.Children.Take(4).Select(item => item.Kind), Is.All.EqualTo(XPathAstKind.UnaryLookup));
            Assert.That(sequence.Children[0].Right!.Kind, Is.EqualTo(XPathAstKind.Name));
            Assert.That(sequence.Children[1].Right!.Kind, Is.EqualTo(XPathAstKind.IntegerLiteral));
            Assert.That(sequence.Children[2].Right!.Kind, Is.EqualTo(XPathAstKind.Parenthesized));
            Assert.That(sequence.Children[3].Right!.Kind, Is.EqualTo(XPathAstKind.Wildcard));
            Assert.That(sequence.Children[4].Kind, Is.EqualTo(XPathAstKind.Lookup));
            Assert.That(sequence.Children[4].Left!.Kind, Is.EqualTo(XPathAstKind.MapConstructor));
            Assert.That(sequence.Children[4].Right!.Operand!.Kind, Is.EqualTo(XPathAstKind.Addition));
        });
    }

    [TestCase("")]
    [TestCase("1,")]
    [TestCase("for $x 1 return $x")]
    [TestCase("let $x = 1 return $x")]
    [TestCase("some $x in 1 return $x")]
    [TestCase("if (1) then 2")]
    [TestCase("1 = 2 = 3")]
    [TestCase("1 to 2 to 3")]
    [TestCase("1 => fn:f")]
    [TestCase("a/")]
    [TestCase("//")]
    [TestCase("1 instance item()")]
    [TestCase("1 cast as")]
    [TestCase("1 instance of xs:int()")]
    [TestCase("(1 + 2")]
    [TestCase("fn:f(1, )")]
    [TestCase("fn:f#")]
    [TestCase("fn:f#2147483648")]
    [TestCase("function($x as) {$x}")]
    [TestCase("function($x) $x")]
    [TestCase("map {'a' 1}")]
    [TestCase("[1,]")]
    [TestCase("array {1")]
    [TestCase("a?")]
    [TestCase("a?p:key")]
    [TestCase("sideways::node()")]
    [TestCase("child::")]
    [TestCase("child::fn:f()")]
    [TestCase("*:")]
    [TestCase("Q{urn:test} *")]
    [TestCase("document-node(text())")]
    [TestCase("node(1)")]
    [TestCase("processing-instruction(p:name)")]
    [TestCase("attribute(*, xs:type?)")]
    [TestCase("schema-element(*)")]
    [TestCase("element(book,)")]
    [TestCase("@")]
    public void ReportsSyntaxErrorsAsXpSt0003(string source)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Parse(source))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathStatic));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
            Assert.That(exception.Location, Is.SameAs(exception.Diagnostic.Range!.Start));
        });
    }

    [TestCase("\n  1 +", 6, 2, 6, 6)]
    [TestCase("map {'a' 1}", 9, 1, 10, 10)]
    [TestCase("fn:f(1,\n )", 9, 2, 2, 10)]
    [TestCase("1 2", 2, 1, 3, 3)]
    public void ReportsPreciseParserErrorRanges(string source, int offset, int line, int column, int endOffset)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => new XPathParser(source, "urn:parser-error").ParseXPathExpression())!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Location!.CharacterOffset, Is.EqualTo(offset));
            Assert.That(exception.Location.Line, Is.EqualTo(line));
            Assert.That(exception.Location.Column, Is.EqualTo(column));
            Assert.That(exception.Location.SourceUri, Is.EqualTo("urn:parser-error"));
            Assert.That(exception.Diagnostic!.Range!.Start, Is.SameAs(exception.Location));
            Assert.That(exception.Diagnostic.Range.End!.CharacterOffset, Is.EqualTo(endOffset));
        });
    }

    [TestCase("1 instance of function(xs:string)")]
    [TestCase("1 instance of map(item(), xs:string)")]
    [TestCase("1 instance of map(xs:string)")]
    [TestCase("1 instance of array()")]
    [TestCase("1 instance of function(xs:string,) as item()")]
    [TestCase("1 instance of (empty-sequence())")]
    public void RejectsMalformedCompositeItemTypes(string source)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Parse(source))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
    }

    private static XPathAstNode Parse(string source)
    {
        return new XPathParser(source, "urn:test").ParseXPathExpression();
    }
}
