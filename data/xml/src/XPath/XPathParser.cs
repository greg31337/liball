using System;
using System.Collections.Generic;

namespace xml;

internal class XPathParser
{
    internal XPathToken[] Tokens;
    internal int Position;
    internal string Source;
    internal string? SourceUri;
    internal string ErrorCode;

    internal XPathParser(string source, string? sourceUri)
    {
        Guard.NotNull(source, "source");
        Tokens = [];
        Position = 0;
        Source = source;
        SourceUri = sourceUri;
        ErrorCode = XPathErrorCodes.XPST0003;
    }

    internal XPathAstNode ParseXPathExpression()
    {
        XPathAstNode result = ParseXPathSequenceExpression();
        XPathToken remaining = CurrentXPathToken();
        if (remaining.Kind != XPathTokenKind.EndOfInput)
        {
            throw CreateXPathParserError("The XPath expression contains an unexpected terminal.", remaining);
        }

        return result;
    }

    internal XPathAstNode ParseXPathSequenceExpression()
    {
        XPathAstNode first = ParseXPathExpressionSingle();
        if (!MatchXPathKind(XPathTokenKind.Comma))
        {
            return first;
        }

        List<XPathAstNode> items = new List<XPathAstNode>();
        items.Add(first);
        do
        {
            items.Add(ParseXPathExpressionSingle());
        }
        while (MatchXPathKind(XPathTokenKind.Comma));

        XPathAstNode result = new XPathAstNode(XPathAstKind.Sequence, CombineXPathRanges(first.Range, items[items.Count - 1].Range));
        result.Children = items.ToArray();
        return result;
    }

    internal XPathAstNode ParseXPathExpressionSingle()
    {
        if (IsXPathKeywordAt(0, "for") && PeekXPathToken(1).Kind == XPathTokenKind.Dollar)
        {
            return ParseXPathForExpression();
        }

        if (IsXPathKeywordAt(0, "let") && PeekXPathToken(1).Kind == XPathTokenKind.Dollar)
        {
            return ParseXPathLetExpression();
        }

        if ((IsXPathKeywordAt(0, "some") || IsXPathKeywordAt(0, "every")) && PeekXPathToken(1).Kind == XPathTokenKind.Dollar)
        {
            return ParseXPathQuantifiedExpression();
        }

        if (IsXPathKeywordAt(0, "if") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathIfExpression();
        }

        return ParseXPathOrExpression();
    }

    internal XPathAstNode ParseXPathForExpression()
    {
        XPathToken opening = ExpectXPathKeyword("for");
        List<XPathBinding> bindings = new List<XPathBinding>();
        while (true)
        {
            XPathToken dollar = ExpectXPathKind(XPathTokenKind.Dollar, "A for binding requires '$'.");
            XPathToken name = ReadXPathEqName("A for binding requires a variable name.");
            ExpectXPathKeyword("in");
            XPathAstNode expression = ParseXPathExpressionSingle();
            XPathBinding binding = CreateXPathBinding(dollar, name, expression);
            binding.SingletonValue = true;
            bindings.Add(binding);
            if (!MatchXPathKind(XPathTokenKind.Comma))
            {
                break;
            }

            if (CurrentXPathToken().Kind != XPathTokenKind.Dollar)
            {
                throw CreateXPathParserError("A comma in a for clause must be followed by another binding.", CurrentXPathToken());
            }
        }

        ExpectXPathKeyword("return");
        XPathAstNode body = ParseXPathExpressionSingle();
        XPathAstNode result = new XPathAstNode(XPathAstKind.For, CombineXPathRanges(opening.Range, body.Range));
        result.Bindings = bindings.ToArray();
        result.Body = body;
        return result;
    }

    internal XPathAstNode ParseXPathLetExpression()
    {
        XPathToken opening = ExpectXPathKeyword("let");
        List<XPathBinding> bindings = new List<XPathBinding>();
        while (true)
        {
            XPathToken dollar = ExpectXPathKind(XPathTokenKind.Dollar, "A let binding requires '$'.");
            XPathToken name = ReadXPathEqName("A let binding requires a variable name.");
            ExpectXPathKind(XPathTokenKind.ColonEquals, "A let binding requires ':='.");
            XPathAstNode expression = ParseXPathExpressionSingle();
            bindings.Add(CreateXPathBinding(dollar, name, expression));
            if (!MatchXPathKind(XPathTokenKind.Comma))
            {
                break;
            }

            if (CurrentXPathToken().Kind != XPathTokenKind.Dollar)
            {
                throw CreateXPathParserError("A comma in a let clause must be followed by another binding.", CurrentXPathToken());
            }
        }

        ExpectXPathKeyword("return");
        XPathAstNode body = ParseXPathExpressionSingle();
        XPathAstNode result = new XPathAstNode(XPathAstKind.Let, CombineXPathRanges(opening.Range, body.Range));
        result.Bindings = bindings.ToArray();
        result.Body = body;
        return result;
    }

    internal XPathAstNode ParseXPathQuantifiedExpression()
    {
        XPathToken opening = AdvanceXPathToken();
        string quantifier = opening.Value == null ? opening.Lexeme : opening.Value;
        List<XPathBinding> bindings = new List<XPathBinding>();
        while (true)
        {
            XPathToken dollar = ExpectXPathKind(XPathTokenKind.Dollar, "A quantified binding requires '$'.");
            XPathToken name = ReadXPathEqName("A quantified binding requires a variable name.");
            ExpectXPathKeyword("in");
            XPathAstNode expression = ParseXPathExpressionSingle();
            XPathBinding binding = CreateXPathBinding(dollar, name, expression);
            binding.SingletonValue = true;
            bindings.Add(binding);
            if (!MatchXPathKind(XPathTokenKind.Comma))
            {
                break;
            }

            if (CurrentXPathToken().Kind != XPathTokenKind.Dollar)
            {
                throw CreateXPathParserError("A comma in a quantified clause must be followed by another binding.", CurrentXPathToken());
            }
        }

        ExpectXPathKeyword("satisfies");
        XPathAstNode body = ParseXPathExpressionSingle();
        XPathAstNode result = new XPathAstNode(XPathAstKind.Quantified, CombineXPathRanges(opening.Range, body.Range));
        result.Operator = quantifier;
        result.Bindings = bindings.ToArray();
        result.Body = body;
        return result;
    }

    internal XPathAstNode ParseXPathIfExpression()
    {
        XPathToken opening = ExpectXPathKeyword("if");
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "An if expression requires '('.");
        XPathAstNode condition = ParseXPathSequenceExpression();
        ExpectXPathKind(XPathTokenKind.RightParenthesis, "An if expression requires ')'.");
        ExpectXPathKeyword("then");
        XPathAstNode thenExpression = ParseXPathExpressionSingle();
        ExpectXPathKeyword("else");
        XPathAstNode elseExpression = ParseXPathExpressionSingle();
        XPathAstNode result = new XPathAstNode(XPathAstKind.If, CombineXPathRanges(opening.Range, elseExpression.Range));
        result.Condition = condition;
        result.ThenExpression = thenExpression;
        result.ElseExpression = elseExpression;
        return result;
    }

    internal XPathAstNode ParseXPathOrExpression()
    {
        XPathAstNode result = ParseXPathAndExpression();
        while (MatchXPathKeyword("or"))
        {
            result = CreateXPathBinaryNode(XPathAstKind.Or, "or", result, ParseXPathAndExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathAndExpression()
    {
        XPathAstNode result = ParseXPathComparisonExpression();
        while (MatchXPathKeyword("and"))
        {
            result = CreateXPathBinaryNode(XPathAstKind.And, "and", result, ParseXPathComparisonExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathComparisonExpression()
    {
        XPathAstNode left = ParseXPathStringConcatenationExpression();
        XPathToken token = CurrentXPathToken();
        XPathAstKind kind = XPathAstKind.Sequence;
        string? operation = null;
        if (token.Kind == XPathTokenKind.Equals || token.Kind == XPathTokenKind.NotEqual || token.Kind == XPathTokenKind.LessThan || token.Kind == XPathTokenKind.LessThanOrEqual || token.Kind == XPathTokenKind.GreaterThan || token.Kind == XPathTokenKind.GreaterThanOrEqual)
        {
            kind = XPathAstKind.GeneralComparison;
            operation = token.Lexeme;
        }
        else if (IsXPathValueComparison(token))
        {
            kind = XPathAstKind.ValueComparison;
            operation = token.Value;
        }
        else if (IsXPathNodeComparison(token))
        {
            kind = XPathAstKind.NodeComparison;
            operation = token.Kind == XPathTokenKind.Keyword ? token.Value : token.Lexeme;
        }

        if (operation == null)
        {
            return left;
        }

        AdvanceXPathToken();
        XPathAstNode right = ParseXPathStringConcatenationExpression();
        return CreateXPathBinaryNode(kind, operation, left, right);
    }

    internal XPathAstNode ParseXPathStringConcatenationExpression()
    {
        XPathAstNode result = ParseXPathRangeExpression();
        while (MatchXPathKind(XPathTokenKind.PipePipe))
        {
            result = CreateXPathBinaryNode(XPathAstKind.StringConcatenation, "||", result, ParseXPathRangeExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathRangeExpression()
    {
        XPathAstNode left = ParseXPathAdditiveExpression();
        if (!MatchXPathKeyword("to"))
        {
            return left;
        }

        return CreateXPathBinaryNode(XPathAstKind.Range, "to", left, ParseXPathAdditiveExpression());
    }

    internal XPathAstNode ParseXPathAdditiveExpression()
    {
        XPathAstNode result = ParseXPathMultiplicativeExpression();
        while (CurrentXPathToken().Kind == XPathTokenKind.Plus || CurrentXPathToken().Kind == XPathTokenKind.Minus)
        {
            XPathToken operation = AdvanceXPathToken();
            XPathAstKind kind = operation.Kind == XPathTokenKind.Plus ? XPathAstKind.Addition : XPathAstKind.Subtraction;
            result = CreateXPathBinaryNode(kind, operation.Lexeme, result, ParseXPathMultiplicativeExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathMultiplicativeExpression()
    {
        XPathAstNode result = ParseXPathUnionExpression();
        while (true)
        {
            XPathToken operation = CurrentXPathToken();
            XPathAstKind kind;
            string? symbol = null;
            if (operation.Kind == XPathTokenKind.Star)
            {
                kind = XPathAstKind.Multiplication;
                symbol = "*";
            }
            else if (IsXPathKeywordToken(operation, "div"))
            {
                kind = XPathAstKind.Division;
                symbol = "div";
            }
            else if (IsXPathKeywordToken(operation, "idiv"))
            {
                kind = XPathAstKind.IntegerDivision;
                symbol = "idiv";
            }
            else if (IsXPathKeywordToken(operation, "mod"))
            {
                kind = XPathAstKind.Modulo;
                symbol = "mod";
            }
            else
            {
                break;
            }

            AdvanceXPathToken();
            result = CreateXPathBinaryNode(kind, symbol, result, ParseXPathUnionExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathUnionExpression()
    {
        XPathAstNode result = ParseXPathIntersectExceptExpression();
        while (CurrentXPathToken().Kind == XPathTokenKind.Pipe || IsXPathKeywordAt(0, "union"))
        {
            XPathToken operation = AdvanceXPathToken();
            string symbol = operation.Kind == XPathTokenKind.Pipe ? "|" : "union";
            result = CreateXPathBinaryNode(XPathAstKind.Union, symbol, result, ParseXPathIntersectExceptExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathIntersectExceptExpression()
    {
        XPathAstNode result = ParseXPathInstanceOfExpression();
        while (IsXPathKeywordAt(0, "intersect") || IsXPathKeywordAt(0, "except"))
        {
            XPathToken operation = AdvanceXPathToken();
            string symbol = operation.Value == null ? operation.Lexeme : operation.Value;
            XPathAstKind kind = symbol == "intersect" ? XPathAstKind.Intersect : XPathAstKind.Except;
            result = CreateXPathBinaryNode(kind, symbol, result, ParseXPathInstanceOfExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathInstanceOfExpression()
    {
        XPathAstNode operand = ParseXPathTreatExpression();
        if (!MatchXPathKeyword("instance"))
        {
            return operand;
        }

        ExpectXPathKeyword("of");
        XPathTypeSyntax type = ParseXPathSequenceType();
        return CreateXPathTypeOperation(XPathAstKind.InstanceOf, "instance of", operand, type);
    }

    internal XPathAstNode ParseXPathTreatExpression()
    {
        XPathAstNode operand = ParseXPathCastableExpression();
        if (!MatchXPathKeyword("treat"))
        {
            return operand;
        }

        ExpectXPathKeyword("as");
        XPathTypeSyntax type = ParseXPathSequenceType();
        return CreateXPathTypeOperation(XPathAstKind.TreatAs, "treat as", operand, type);
    }

    internal XPathAstNode ParseXPathCastableExpression()
    {
        XPathAstNode operand = ParseXPathCastExpression();
        if (!MatchXPathKeyword("castable"))
        {
            return operand;
        }

        ExpectXPathKeyword("as");
        XPathTypeSyntax type = ParseXPathSingleType();
        return CreateXPathTypeOperation(XPathAstKind.CastableAs, "castable as", operand, type);
    }

    internal XPathAstNode ParseXPathCastExpression()
    {
        XPathAstNode operand = ParseXPathArrowExpression();
        if (!MatchXPathKeyword("cast"))
        {
            return operand;
        }

        ExpectXPathKeyword("as");
        XPathTypeSyntax type = ParseXPathSingleType();
        return CreateXPathTypeOperation(XPathAstKind.CastAs, "cast as", operand, type);
    }

    internal XPathAstNode ParseXPathArrowExpression()
    {
        XPathAstNode result = ParseXPathUnaryExpression();
        while (MatchXPathKind(XPathTokenKind.Arrow))
        {
            XPathAstNode function = ParseXPathArrowFunctionSpecifier();
            XPathAstNode[] arguments = ParseXPathArgumentList("An arrow function specifier requires an argument list.", "An arrow argument list requires ')'.");
            SourceRange endRange = PreviousXPathToken().Range;
            XPathAstNode arrow = new XPathAstNode(XPathAstKind.Arrow, CombineXPathRanges(result.Range, endRange));
            arrow.Left = result;
            arrow.Right = function;
            arrow.Children = arguments;
            arrow.Operator = "=>";
            result = arrow;
        }

        return result;
    }

    internal XPathAstNode ParseXPathUnaryExpression()
    {
        List<XPathToken> operations = new List<XPathToken>();
        while (CurrentXPathToken().Kind == XPathTokenKind.Plus || CurrentXPathToken().Kind == XPathTokenKind.Minus)
        {
            operations.Add(AdvanceXPathToken());
        }

        XPathAstNode result = ParseXPathSimpleMapExpression();
        int index = operations.Count - 1;
        while (index >= 0)
        {
            XPathToken operation = operations[index];
            XPathAstKind kind = operation.Kind == XPathTokenKind.Plus ? XPathAstKind.UnaryPlus : XPathAstKind.UnaryMinus;
            XPathAstNode unary = new XPathAstNode(kind, CombineXPathRanges(operation.Range, result.Range));
            unary.Operator = operation.Lexeme;
            unary.Operand = result;
            result = unary;
            index = index - 1;
        }

        return result;
    }

    internal XPathAstNode ParseXPathSimpleMapExpression()
    {
        XPathAstNode result = ParseXPathPathExpression();
        while (MatchXPathKind(XPathTokenKind.Bang))
        {
            result = CreateXPathBinaryNode(XPathAstKind.SimpleMap, "!", result, ParseXPathPathExpression());
        }

        return result;
    }

    internal XPathAstNode ParseXPathPathExpression()
    {
        bool absolute = false;
        bool leadingDescendant = false;
        XPathToken? opening = null;
        if (CurrentXPathToken().Kind == XPathTokenKind.Slash || CurrentXPathToken().Kind == XPathTokenKind.SlashSlash)
        {
            opening = AdvanceXPathToken();
            absolute = true;
            leadingDescendant = opening.Kind == XPathTokenKind.SlashSlash;
            if (leadingDescendant && !CanStartXPathPathStep(CurrentXPathToken()))
            {
                throw CreateXPathParserError("A leading '//' requires a relative path expression.", CurrentXPathToken());
            }

            if (!leadingDescendant && !CanStartXPathPathStep(CurrentXPathToken()))
            {
                XPathAstNode root = new XPathAstNode(XPathAstKind.Path, opening.Range);
                root.Absolute = true;
                return root;
            }
        }

        XPathAstNode first = ParseXPathPathStep();
        List<XPathAstNode> steps = new List<XPathAstNode>();
        List<string> operations = new List<string>();
        steps.Add(first);
        while (CurrentXPathToken().Kind == XPathTokenKind.Slash || CurrentXPathToken().Kind == XPathTokenKind.SlashSlash)
        {
            XPathToken separator = AdvanceXPathToken();
            if (!CanStartXPathPathStep(CurrentXPathToken()))
            {
                throw CreateXPathParserError("A path separator requires a following step.", CurrentXPathToken());
            }

            operations.Add(separator.Lexeme);
            steps.Add(ParseXPathPathStep());
        }

        if (!absolute && steps.Count == 1)
        {
            return first;
        }

        SourceRange startRange = opening == null ? first.Range : opening.Range;
        XPathAstNode result = new XPathAstNode(XPathAstKind.Path, CombineXPathRanges(startRange, steps[steps.Count - 1].Range));
        result.Children = steps.ToArray();
        result.PathOperators = operations.ToArray();
        result.Absolute = absolute;
        result.LeadingDescendant = leadingDescendant;
        return result;
    }

    internal XPathAstNode ParseXPathPathStep()
    {
        XPathToken current = CurrentXPathToken();
        if (current.Kind == XPathTokenKind.DotDot)
        {
            AdvanceXPathToken();
            XPathAstNode parent = new XPathAstNode(XPathAstKind.ParentStep, current.Range);
            parent.Token = current;
            parent.Value = "..";
            parent.Axis = XPathAxis.Parent;
            parent.ReverseAxis = true;
            XPathNodeTest test = new XPathNodeTest(XPathNodeTestKind.AnyKind, current.Range);
            test.Token = current;
            test.Value = "node";
            parent.NodeTest = test;
            return ParseXPathAxisPredicateList(parent);
        }

        if (IsXPathAxisAt(0))
        {
            XPathToken opening = current;
            XPathAxis axis = ReadXPathAxis();
            XPathNodeTest test = ParseXPathNodeTest();
            XPathAstNode step = CreateXPathAxisStepNode(XPathAstKind.AxisStep, opening, axis, test, IsXPathReverseAxis(axis));
            return ParseXPathAxisPredicateList(step);
        }

        if (current.Kind == XPathTokenKind.At)
        {
            XPathToken attribute = AdvanceXPathToken();
            XPathNodeTest test = ParseXPathNodeTest();
            XPathAstNode step = CreateXPathAxisStepNode(XPathAstKind.AttributeStep, attribute, XPathAxis.Attribute, test, false);
            return ParseXPathAxisPredicateList(step);
        }

        if (CanStartXPathNodeTest())
        {
            XPathToken opening = current;
            XPathNodeTest test = ParseXPathNodeTest();
            XPathAstKind kind = test.Kind == XPathNodeTestKind.Name ? XPathAstKind.Name : XPathAstKind.AxisStep;
            XPathAstNode step = CreateXPathAxisStepNode(kind, opening, XPathAxis.Child, test, false);
            return ParseXPathAxisPredicateList(step);
        }

        return ParseXPathPostfixExpression();
    }

    internal XPathAstNode ParseXPathAxisPredicateList(XPathAstNode step)
    {
        XPathAstNode result = step;
        while (CurrentXPathToken().Kind == XPathTokenKind.LeftBracket)
        {
            result = ParseXPathPredicate(result);
        }

        return result;
    }

    internal XPathAstNode ParseXPathPredicate(XPathAstNode operand)
    {
        AdvanceXPathToken();
        XPathAstNode expression = ParseXPathSequenceExpression();
        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightBracket, "A predicate requires ']'.");
        XPathAstNode predicate = new XPathAstNode(XPathAstKind.Predicate, CombineXPathRanges(operand.Range, closing.Range));
        predicate.Left = operand;
        predicate.Right = expression;
        return predicate;
    }

    internal XPathAstNode CreateXPathAxisStepNode(XPathAstKind kind, XPathToken opening, XPathAxis axis, XPathNodeTest test, bool reverse)
    {
        XPathAstNode result;
        if ((kind == XPathAstKind.Name || kind == XPathAstKind.AttributeStep) && test.Kind == XPathNodeTestKind.Name && test.Token != null)
        {
            result = CreateXPathNameNode(kind, test.Token);
        }
        else
        {
            result = new XPathAstNode(kind, test.Range);
            result.Token = opening;
            result.Value = test.Value;
            result.Prefix = test.Prefix;
            result.LocalName = test.LocalName;
            result.NamespaceUri = test.NamespaceUri;
        }

        result.Range = CombineXPathRanges(opening.Range, test.Range);
        result.Axis = axis;
        result.NodeTest = test;
        result.ReverseAxis = reverse;
        return result;
    }

    internal bool CanStartXPathNodeTest()
    {
        XPathToken current = CurrentXPathToken();
        if (IsXPathKindTestAt(0))
        {
            return true;
        }

        if (current.Kind == XPathTokenKind.Star || current.Kind == XPathTokenKind.StarColon || current.Kind == XPathTokenKind.BracedUriLiteral)
        {
            return true;
        }

        if (!IsXPathEqNameToken(current))
        {
            return false;
        }

        XPathTokenKind next = PeekXPathToken(1).Kind;
        if (next == XPathTokenKind.LeftParenthesis || next == XPathTokenKind.Hash)
        {
            return false;
        }

        if ((IsXPathKeywordToken(current, "map") || IsXPathKeywordToken(current, "array")) && next == XPathTokenKind.LeftBrace)
        {
            return false;
        }

        return true;
    }

    internal XPathNodeTest ParseXPathNodeTest()
    {
        if (IsXPathKindTestAt(0))
        {
            return ParseXPathKindTest();
        }

        XPathToken token = CurrentXPathToken();
        if ((token.Kind == XPathTokenKind.NcName || token.Kind == XPathTokenKind.Keyword) && PeekXPathToken(1).Kind == XPathTokenKind.ColonStar)
        {
            AdvanceXPathToken();
            XPathToken ending = AdvanceXPathToken();
            if (ending.HasLeadingSeparator)
            {
                throw CreateXPathParserError("Whitespace and comments are not permitted inside a namespace wildcard.", ending);
            }

            XPathNodeTest namespaceWildcard = new XPathNodeTest(XPathNodeTestKind.NamespaceWildcard, CombineXPathRanges(token.Range, ending.Range));
            namespaceWildcard.Token = token;
            namespaceWildcard.Value = token.Lexeme + ":*";
            namespaceWildcard.Prefix = token.Value == null ? token.Lexeme : token.Value;
            return namespaceWildcard;
        }

        if (token.Kind == XPathTokenKind.StarColon)
        {
            AdvanceXPathToken();
            XPathToken local = CurrentXPathToken();
            if (local.Kind != XPathTokenKind.NcName && local.Kind != XPathTokenKind.Keyword)
            {
                throw CreateXPathParserError("A local-name wildcard requires an NCName after '*:'.", local);
            }

            if (local.HasLeadingSeparator)
            {
                throw CreateXPathParserError("Whitespace and comments are not permitted inside a local-name wildcard.", local);
            }

            AdvanceXPathToken();
            XPathNodeTest localWildcard = new XPathNodeTest(XPathNodeTestKind.LocalNameWildcard, CombineXPathRanges(token.Range, local.Range));
            localWildcard.Token = token;
            localWildcard.Value = "*:" + local.Lexeme;
            localWildcard.LocalName = local.Value == null ? local.Lexeme : local.Value;
            return localWildcard;
        }

        if (token.Kind == XPathTokenKind.BracedUriLiteral)
        {
            AdvanceXPathToken();
            XPathToken star = ExpectXPathKind(XPathTokenKind.Star, "A braced URI wildcard requires '*'.");
            if (star.HasLeadingSeparator)
            {
                throw CreateXPathParserError("Whitespace is not permitted inside a braced URI wildcard.", star);
            }

            XPathNodeTest uriWildcard = new XPathNodeTest(XPathNodeTestKind.UriWildcard, CombineXPathRanges(token.Range, star.Range));
            uriWildcard.Token = token;
            uriWildcard.Value = token.Lexeme + "*";
            uriWildcard.NamespaceUri = token.NamespaceUri;
            return uriWildcard;
        }

        if (token.Kind == XPathTokenKind.Star)
        {
            AdvanceXPathToken();
            XPathNodeTest wildcard = new XPathNodeTest(XPathNodeTestKind.Wildcard, token.Range);
            wildcard.Token = token;
            wildcard.Value = "*";
            return wildcard;
        }

        if (IsXPathEqNameToken(token))
        {
            AdvanceXPathToken();
            XPathNodeTest name = new XPathNodeTest(XPathNodeTestKind.Name, token.Range);
            name.Token = token;
            name.Value = token.Value == null ? token.Lexeme : token.Value;
            name.Prefix = token.Prefix;
            name.LocalName = token.LocalName;
            name.NamespaceUri = token.NamespaceUri;
            return name;
        }

        throw CreateXPathParserError("An XPath node test was expected.", token);
    }

    internal XPathNodeTest ParseXPathKindTest()
    {
        XPathToken opening = AdvanceXPathToken();
        string name = opening.Value == null ? opening.Lexeme : opening.Value;
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "A kind test requires '('.");
        if (name == "node" || name == "text" || name == "comment" || name == "namespace-node")
        {
            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "This kind test requires ')'.");
            XPathNodeTestKind kind = XPathNodeTestKind.AnyKind;
            if (name == "text")
            {
                kind = XPathNodeTestKind.Text;
            }
            else if (name == "comment")
            {
                kind = XPathNodeTestKind.Comment;
            }
            else if (name == "namespace-node")
            {
                kind = XPathNodeTestKind.NamespaceNode;
            }

            XPathNodeTest empty = new XPathNodeTest(kind, CombineXPathRanges(opening.Range, closing.Range));
            empty.Token = opening;
            empty.Value = name;
            return empty;
        }

        if (name == "processing-instruction")
        {
            XPathNodeTest processingInstruction = new XPathNodeTest(XPathNodeTestKind.ProcessingInstruction, opening.Range);
            processingInstruction.Token = opening;
            if (CurrentXPathToken().Kind != XPathTokenKind.RightParenthesis)
            {
                XPathToken target = CurrentXPathToken();
                if (target.Kind != XPathTokenKind.NcName && target.Kind != XPathTokenKind.Keyword && target.Kind != XPathTokenKind.StringLiteral)
                {
                    throw CreateXPathParserError("A processing-instruction test requires an NCName or string literal target.", target);
                }

                AdvanceXPathToken();
                processingInstruction.Value = target.Value == null ? target.Lexeme : target.Value;
                processingInstruction.LocalName = target.Kind == XPathTokenKind.StringLiteral ? null : processingInstruction.Value;
            }

            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A processing-instruction test requires ')'.");
            processingInstruction.Range = CombineXPathRanges(opening.Range, closing.Range);
            return processingInstruction;
        }

        if (name == "document-node")
        {
            XPathNodeTest document = new XPathNodeTest(XPathNodeTestKind.Document, opening.Range);
            document.Token = opening;
            document.Value = name;
            if (CurrentXPathToken().Kind != XPathTokenKind.RightParenthesis)
            {
                if ((!IsXPathKeywordAt(0, "element") && !IsXPathKeywordAt(0, "schema-element")) || PeekXPathToken(1).Kind != XPathTokenKind.LeftParenthesis)
                {
                    throw CreateXPathParserError("A document-node test may contain only an element or schema-element test.", CurrentXPathToken());
                }

                document.InnerTest = ParseXPathKindTest();
            }

            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A document-node test requires ')'.");
            document.Range = CombineXPathRanges(opening.Range, closing.Range);
            return document;
        }

        if (name == "attribute" || name == "schema-attribute")
        {
            return ParseXPathAttributeKindTest(opening, name == "schema-attribute");
        }

        if (name == "element" || name == "schema-element")
        {
            return ParseXPathElementKindTest(opening, name == "schema-element");
        }

        throw CreateXPathParserError("The name does not identify an XPath kind test.", opening);
    }

    internal XPathNodeTest ParseXPathAttributeKindTest(XPathToken opening, bool schema)
    {
        XPathNodeTestKind kind = schema ? XPathNodeTestKind.SchemaAttribute : XPathNodeTestKind.Attribute;
        XPathNodeTest result = new XPathNodeTest(kind, opening.Range);
        result.Token = opening;
        result.Value = opening.Value == null ? opening.Lexeme : opening.Value;
        if (CurrentXPathToken().Kind == XPathTokenKind.RightParenthesis)
        {
            if (schema)
            {
                throw CreateXPathParserError("A schema-attribute test requires an attribute declaration name.", CurrentXPathToken());
            }
        }
        else
        {
            ReadXPathKindTestName(result, !schema);
            if (MatchXPathKind(XPathTokenKind.Comma))
            {
                if (schema)
                {
                    throw CreateXPathParserError("A schema-attribute test cannot include a type name.", PreviousXPathToken());
                }

                result.Type = ParseXPathTypeName();
            }
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "An attribute kind test requires ')'.");
        result.Range = CombineXPathRanges(opening.Range, closing.Range);
        return result;
    }

    internal XPathNodeTest ParseXPathElementKindTest(XPathToken opening, bool schema)
    {
        XPathNodeTestKind kind = schema ? XPathNodeTestKind.SchemaElement : XPathNodeTestKind.Element;
        XPathNodeTest result = new XPathNodeTest(kind, opening.Range);
        result.Token = opening;
        result.Value = opening.Value == null ? opening.Lexeme : opening.Value;
        if (CurrentXPathToken().Kind == XPathTokenKind.RightParenthesis)
        {
            if (schema)
            {
                throw CreateXPathParserError("A schema-element test requires an element declaration name.", CurrentXPathToken());
            }
        }
        else
        {
            ReadXPathKindTestName(result, !schema);
            if (MatchXPathKind(XPathTokenKind.Comma))
            {
                if (schema)
                {
                    throw CreateXPathParserError("A schema-element test cannot include a type name.", PreviousXPathToken());
                }

                result.Type = ParseXPathTypeName();
                if (MatchXPathKind(XPathTokenKind.Question))
                {
                    result.Nillable = true;
                }
            }
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "An element kind test requires ')'.");
        result.Range = CombineXPathRanges(opening.Range, closing.Range);
        return result;
    }

    internal void ReadXPathKindTestName(XPathNodeTest test, bool allowWildcard)
    {
        XPathToken token = CurrentXPathToken();
        if (allowWildcard && token.Kind == XPathTokenKind.Star)
        {
            AdvanceXPathToken();
            test.LocalName = "*";
            return;
        }

        if (!IsXPathEqNameToken(token))
        {
            throw CreateXPathParserError("A kind test requires an EQName or permitted wildcard.", token);
        }

        AdvanceXPathToken();
        test.Prefix = token.Prefix;
        test.LocalName = token.LocalName;
        test.NamespaceUri = token.NamespaceUri;
    }

    internal XPathTypeSyntax ParseXPathTypeName()
    {
        XPathToken name = ReadXPathEqName("A kind test type name requires an EQName.");
        string lexical = name.Value == null ? name.Lexeme : name.Value;
        return new XPathTypeSyntax(lexical, name.Prefix, name.LocalName, name.NamespaceUri, null, false, name.Range);
    }

    internal XPathAstNode ParseXPathPostfixExpression()
    {
        XPathAstNode result = ParseXPathPrimaryExpression();
        while (true)
        {
            if (CurrentXPathToken().Kind == XPathTokenKind.LeftBracket)
            {
                result = ParseXPathPredicate(result);
                continue;
            }

            if (CurrentXPathToken().Kind == XPathTokenKind.LeftParenthesis)
            {
                XPathAstNode[] arguments = ParseXPathArgumentList("A dynamic function call requires '('.", "A dynamic function call requires ')'.");
                XPathAstNode call = new XPathAstNode(XPathAstKind.DynamicFunctionCall, CombineXPathRanges(result.Range, PreviousXPathToken().Range));
                call.Operand = result;
                call.Children = arguments;
                result = call;
                continue;
            }

            if (CurrentXPathToken().Kind == XPathTokenKind.Question)
            {
                AdvanceXPathToken();
                XPathAstNode key = ParseXPathKeySpecifier();
                XPathAstNode lookup = new XPathAstNode(XPathAstKind.Lookup, CombineXPathRanges(result.Range, key.Range));
                lookup.Left = result;
                lookup.Right = key;
                result = lookup;
                continue;
            }

            break;
        }

        return result;
    }

    internal XPathAstNode ParseXPathPrimaryExpression()
    {
        XPathToken token = CurrentXPathToken();
        if (token.Kind == XPathTokenKind.IntegerLiteral || token.Kind == XPathTokenKind.DecimalLiteral || token.Kind == XPathTokenKind.DoubleLiteral || token.Kind == XPathTokenKind.StringLiteral)
        {
            AdvanceXPathToken();
            XPathAstKind kind = XPathAstKind.StringLiteral;
            if (token.Kind == XPathTokenKind.IntegerLiteral)
            {
                kind = XPathAstKind.IntegerLiteral;
            }
            else if (token.Kind == XPathTokenKind.DecimalLiteral)
            {
                kind = XPathAstKind.DecimalLiteral;
            }
            else if (token.Kind == XPathTokenKind.DoubleLiteral)
            {
                kind = XPathAstKind.DoubleLiteral;
            }

            XPathAstNode literal = new XPathAstNode(kind, token.Range);
            literal.Token = token;
            literal.Value = token.Value;
            return literal;
        }

        if (IsXPathKeywordToken(token, "function") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathInlineFunctionExpression();
        }

        if (IsXPathKeywordToken(token, "map") && PeekXPathToken(1).Kind == XPathTokenKind.LeftBrace)
        {
            return ParseXPathMapConstructor();
        }

        if (IsXPathKeywordToken(token, "array") && PeekXPathToken(1).Kind == XPathTokenKind.LeftBrace)
        {
            return ParseXPathCurlyArrayConstructor();
        }

        if (IsXPathEqNameToken(token))
        {
            AdvanceXPathToken();
            if (CurrentXPathToken().Kind == XPathTokenKind.Hash)
            {
                if (token.Prefix == null && token.NamespaceUri == null && IsXPathReservedFunctionName(token.Value == null ? token.Lexeme : token.Value))
                {
                    throw CreateXPathParserError("A reserved unprefixed name cannot be used as a named function reference.", token);
                }

                AdvanceXPathToken();
                XPathToken arity = ExpectXPathKind(XPathTokenKind.IntegerLiteral, "A named function reference requires an integer arity.");
                XPathAstNode reference = CreateXPathNameNode(XPathAstKind.NamedFunctionReference, token);
                reference.Range = CombineXPathRanges(token.Range, arity.Range);
                reference.Arity = ReadXPathArity(arity);
                return reference;
            }

            if (CurrentXPathToken().Kind == XPathTokenKind.LeftParenthesis)
            {
                if (token.Prefix == null && token.NamespaceUri == null && IsXPathReservedFunctionName(token.Value == null ? token.Lexeme : token.Value))
                {
                    throw CreateXPathParserError("A reserved unprefixed name cannot be used as a function name.", token);
                }

                XPathAstNode[] arguments = ParseXPathArgumentList("A static function call requires '('.", "A static function call requires ')'.");
                XPathAstNode call = CreateXPathNameNode(XPathAstKind.StaticFunctionCall, token);
                call.Range = CombineXPathRanges(token.Range, PreviousXPathToken().Range);
                call.Children = arguments;
                return call;
            }

            return CreateXPathNameNode(XPathAstKind.Name, token);
        }

        if (token.Kind == XPathTokenKind.Dollar)
        {
            XPathToken dollar = AdvanceXPathToken();
            XPathToken name = ReadXPathEqName("A variable reference requires a name.");
            XPathAstNode variable = CreateXPathNameNode(XPathAstKind.Variable, name);
            variable.Range = CombineXPathRanges(dollar.Range, name.Range);
            return variable;
        }

        if (token.Kind == XPathTokenKind.Dot)
        {
            AdvanceXPathToken();
            XPathAstNode context = new XPathAstNode(XPathAstKind.ContextItem, token.Range);
            context.Token = token;
            context.Value = ".";
            return context;
        }

        if (token.Kind == XPathTokenKind.LeftParenthesis)
        {
            XPathToken opening = AdvanceXPathToken();
            XPathAstNode? expression = null;
            if (CurrentXPathToken().Kind != XPathTokenKind.RightParenthesis)
            {
                expression = ParseXPathSequenceExpression();
            }

            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A parenthesized expression requires ')'.");
            XPathAstNode parenthesized = new XPathAstNode(XPathAstKind.Parenthesized, CombineXPathRanges(opening.Range, closing.Range));
            parenthesized.Operand = expression;
            return parenthesized;
        }

        if (token.Kind == XPathTokenKind.LeftBracket)
        {
            return ParseXPathSquareArrayConstructor();
        }

        if (token.Kind == XPathTokenKind.Question)
        {
            XPathToken opening = AdvanceXPathToken();
            XPathAstNode key = ParseXPathKeySpecifier();
            XPathAstNode lookup = new XPathAstNode(XPathAstKind.UnaryLookup, CombineXPathRanges(opening.Range, key.Range));
            lookup.Right = key;
            return lookup;
        }

        throw CreateXPathParserError("An XPath expression operand was expected.", token);
    }

    internal bool IsXPathReservedFunctionName(string name)
    {
        return name == "array" || name == "attribute" || name == "comment" || name == "document-node" || name == "element" || name == "empty-sequence" || name == "function" || name == "if" || name == "item" || name == "map" || name == "namespace-node" || name == "node" || name == "processing-instruction" || name == "schema-attribute" || name == "schema-element" || name == "switch" || name == "text" || name == "typeswitch";
    }

    internal XPathAstNode ParseXPathInlineFunctionExpression()
    {
        XPathToken opening = ExpectXPathKeyword("function");
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "An inline function requires '('.");
        List<XPathParameter> parameters = new List<XPathParameter>();
        if (CurrentXPathToken().Kind != XPathTokenKind.RightParenthesis)
        {
            while (true)
            {
                XPathToken dollar = ExpectXPathKind(XPathTokenKind.Dollar, "An inline function parameter requires '$'.");
                XPathToken name = ReadXPathEqName("An inline function parameter requires a name.");
                XPathTypeSyntax? type = null;
                SourceRange end = name.Range;
                if (MatchXPathKeyword("as"))
                {
                    type = ParseXPathSequenceType();
                    end = type.Range;
                }

                string lexical = name.Value == null ? name.Lexeme : name.Value;
                parameters.Add(new XPathParameter(lexical, name.Prefix, name.LocalName, name.NamespaceUri, type, CombineXPathRanges(dollar.Range, end)));
                if (!MatchXPathKind(XPathTokenKind.Comma))
                {
                    break;
                }
            }
        }

        ExpectXPathKind(XPathTokenKind.RightParenthesis, "An inline function parameter list requires ')'.");
        XPathTypeSyntax? resultType = null;
        if (MatchXPathKeyword("as"))
        {
            resultType = ParseXPathSequenceType();
        }

        ExpectXPathKind(XPathTokenKind.LeftBrace, "An inline function requires a body enclosed in braces.");
        XPathAstNode? body = null;
        if (CurrentXPathToken().Kind != XPathTokenKind.RightBrace)
        {
            body = ParseXPathSequenceExpression();
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightBrace, "An inline function body requires '}'.");
        XPathAstNode result = new XPathAstNode(XPathAstKind.InlineFunction, CombineXPathRanges(opening.Range, closing.Range));
        result.Parameters = parameters.ToArray();
        result.Type = resultType;
        result.Body = body;
        return result;
    }

    internal XPathAstNode ParseXPathMapConstructor()
    {
        XPathToken opening = ExpectXPathKeyword("map");
        ExpectXPathKind(XPathTokenKind.LeftBrace, "A map constructor requires '{'.");
        List<XPathMapEntry> entries = new List<XPathMapEntry>();
        if (CurrentXPathToken().Kind != XPathTokenKind.RightBrace)
        {
            while (true)
            {
                XPathAstNode key = ParseXPathExpressionSingle();
                ExpectXPathKind(XPathTokenKind.Colon, "A map constructor entry requires ':'.");
                XPathAstNode value = ParseXPathExpressionSingle();
                entries.Add(new XPathMapEntry(key, value, CombineXPathRanges(key.Range, value.Range)));
                if (!MatchXPathKind(XPathTokenKind.Comma))
                {
                    break;
                }
            }
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightBrace, "A map constructor requires '}'.");
        XPathAstNode result = new XPathAstNode(XPathAstKind.MapConstructor, CombineXPathRanges(opening.Range, closing.Range));
        result.MapEntries = entries.ToArray();
        return result;
    }

    internal XPathAstNode ParseXPathSquareArrayConstructor()
    {
        XPathToken opening = ExpectXPathKind(XPathTokenKind.LeftBracket, "A square array constructor requires '['.");
        List<XPathAstNode> members = new List<XPathAstNode>();
        if (CurrentXPathToken().Kind != XPathTokenKind.RightBracket)
        {
            while (true)
            {
                members.Add(ParseXPathExpressionSingle());
                if (!MatchXPathKind(XPathTokenKind.Comma))
                {
                    break;
                }
            }
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightBracket, "A square array constructor requires ']'.");
        XPathAstNode result = new XPathAstNode(XPathAstKind.SquareArrayConstructor, CombineXPathRanges(opening.Range, closing.Range));
        result.Children = members.ToArray();
        return result;
    }

    internal XPathAstNode ParseXPathCurlyArrayConstructor()
    {
        XPathToken opening = ExpectXPathKeyword("array");
        ExpectXPathKind(XPathTokenKind.LeftBrace, "A curly array constructor requires '{'.");
        XPathAstNode? contents = null;
        if (CurrentXPathToken().Kind != XPathTokenKind.RightBrace)
        {
            contents = ParseXPathSequenceExpression();
        }

        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightBrace, "A curly array constructor requires '}'.");
        XPathAstNode result = new XPathAstNode(XPathAstKind.CurlyArrayConstructor, CombineXPathRanges(opening.Range, closing.Range));
        result.Body = contents;
        return result;
    }

    internal XPathAstNode ParseXPathKeySpecifier()
    {
        XPathToken token = CurrentXPathToken();
        if (token.Kind == XPathTokenKind.NcName || token.Kind == XPathTokenKind.Keyword)
        {
            AdvanceXPathToken();
            return CreateXPathNameNode(XPathAstKind.Name, token);
        }

        if (token.Kind == XPathTokenKind.IntegerLiteral)
        {
            AdvanceXPathToken();
            XPathAstNode integer = new XPathAstNode(XPathAstKind.IntegerLiteral, token.Range);
            integer.Token = token;
            integer.Value = token.Value;
            return integer;
        }

        if (token.Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathPrimaryExpression();
        }

        if (token.Kind == XPathTokenKind.Star)
        {
            AdvanceXPathToken();
            XPathAstNode wildcard = new XPathAstNode(XPathAstKind.Wildcard, token.Range);
            wildcard.Token = token;
            wildcard.Value = "*";
            return wildcard;
        }

        throw CreateXPathParserError("A lookup operator requires an NCName, integer, parenthesized expression, or '*'.", token);
    }

    internal XPathAstNode ParseXPathArrowFunctionSpecifier()
    {
        XPathToken token = CurrentXPathToken();
        if (IsXPathEqNameToken(token))
        {
            AdvanceXPathToken();
            return CreateXPathNameNode(XPathAstKind.Name, token);
        }

        if (token.Kind == XPathTokenKind.Dollar)
        {
            XPathToken dollar = AdvanceXPathToken();
            XPathToken name = ReadXPathEqName("An arrow variable function specifier requires a name.");
            XPathAstNode variable = CreateXPathNameNode(XPathAstKind.Variable, name);
            variable.Range = CombineXPathRanges(dollar.Range, name.Range);
            return variable;
        }

        if (token.Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathPrimaryExpression();
        }

        throw CreateXPathParserError("An arrow expression requires a function specifier.", token);
    }

    internal XPathAstNode[] ParseXPathArgumentList(string openingMessage, string closingMessage)
    {
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, openingMessage);
        List<XPathAstNode> arguments = new List<XPathAstNode>();
        if (MatchXPathKind(XPathTokenKind.RightParenthesis))
        {
            return arguments.ToArray();
        }

        while (true)
        {
            if (CurrentXPathToken().Kind == XPathTokenKind.Question && (PeekXPathToken(1).Kind == XPathTokenKind.Comma || PeekXPathToken(1).Kind == XPathTokenKind.RightParenthesis))
            {
                XPathToken placeholderToken = AdvanceXPathToken();
                XPathAstNode placeholder = new XPathAstNode(XPathAstKind.ArgumentPlaceholder, placeholderToken.Range);
                placeholder.Token = placeholderToken;
                placeholder.Value = "?";
                arguments.Add(placeholder);
            }
            else
            {
                arguments.Add(ParseXPathExpressionSingle());
            }

            if (!MatchXPathKind(XPathTokenKind.Comma))
            {
                break;
            }
        }

        ExpectXPathKind(XPathTokenKind.RightParenthesis, closingMessage);
        return arguments.ToArray();
    }

    internal int ReadXPathArity(XPathToken token)
    {
        int result = 0;
        int index = 0;
        while (index < token.Lexeme.Length)
        {
            int digit = token.Lexeme[index] - '0';
            if (result > (Int32.MaxValue - digit) / 10)
            {
                throw CreateXPathParserError("A named function reference arity is too large.", token);
            }

            result = (result * 10) + digit;
            index = index + 1;
        }

        return result;
    }

    internal XPathTypeSyntax ParseXPathSequenceType()
    {
        if (IsXPathKeywordAt(0, "empty-sequence"))
        {
            XPathToken empty = AdvanceXPathToken();
            ExpectXPathKind(XPathTokenKind.LeftParenthesis, "empty-sequence requires '()'.");
            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "empty-sequence requires '()'.");
            XPathTypeSyntax emptyType = new XPathTypeSyntax("empty-sequence", null, "empty-sequence", null, null, true, CombineXPathRanges(empty.Range, closing.Range));
            emptyType.ParenthesizedTest = true;
            return emptyType;
        }

        XPathTypeSyntax result = ParseXPathItemType();
        if (CurrentXPathToken().Kind == XPathTokenKind.Question || CurrentXPathToken().Kind == XPathTokenKind.Star || CurrentXPathToken().Kind == XPathTokenKind.Plus)
        {
            XPathToken occurrence = AdvanceXPathToken();
            result.Occurrence = occurrence.Lexeme;
            result.Range = CombineXPathRanges(result.Range, occurrence.Range);
        }

        return result;
    }

    internal XPathTypeSyntax ParseXPathItemType()
    {
        XPathToken token = CurrentXPathToken();
        if (token.Kind == XPathTokenKind.LeftParenthesis)
        {
            XPathToken opening = AdvanceXPathToken();
            XPathTypeSyntax inner = ParseXPathItemType();
            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A parenthesized item type requires ')'.");
            XPathTypeSyntax parenthesized = new XPathTypeSyntax(inner.Name, inner.Prefix, inner.LocalName, inner.NamespaceUri, null, false, CombineXPathRanges(opening.Range, closing.Range));
            parenthesized.ParenthesizedItemType = true;
            parenthesized.InnerType = inner;
            return parenthesized;
        }

        if (IsXPathKindTestAt(0))
        {
            XPathNodeTest test = ParseXPathKindTest();
            string name = test.Value == null ? "node" : test.Value;
            XPathTypeSyntax kind = new XPathTypeSyntax(name, null, name, null, null, false, test.Range);
            kind.ParenthesizedTest = true;
            kind.NodeTest = test;
            return kind;
        }

        if (IsXPathKeywordToken(token, "item") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            XPathToken opening = AdvanceXPathToken();
            AdvanceXPathToken();
            XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "item requires '()'.");
            XPathTypeSyntax item = new XPathTypeSyntax("item", null, "item", null, null, false, CombineXPathRanges(opening.Range, closing.Range));
            item.ParenthesizedTest = true;
            return item;
        }

        if (IsXPathKeywordToken(token, "function") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathFunctionTest();
        }

        if (IsXPathKeywordToken(token, "map") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathMapTest();
        }

        if (IsXPathKeywordToken(token, "array") && PeekXPathToken(1).Kind == XPathTokenKind.LeftParenthesis)
        {
            return ParseXPathArrayTest();
        }

        XPathToken typeName = ReadXPathEqName("An XPath item type was expected.");
        string lexical = typeName.Value == null ? typeName.Lexeme : typeName.Value;
        return new XPathTypeSyntax(lexical, typeName.Prefix, typeName.LocalName, typeName.NamespaceUri, null, false, typeName.Range);
    }

    internal XPathTypeSyntax ParseXPathFunctionTest()
    {
        XPathToken opening = ExpectXPathKeyword("function");
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "A function test requires '('.");
        if (MatchXPathKind(XPathTokenKind.Star))
        {
            XPathToken wildcardClosing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A wildcard function test requires ')'.");
            XPathTypeSyntax wildcard = new XPathTypeSyntax("function", null, "function", null, null, false, CombineXPathRanges(opening.Range, wildcardClosing.Range));
            wildcard.ParenthesizedTest = true;
            wildcard.WildcardTest = true;
            return wildcard;
        }

        List<XPathTypeSyntax> parameters = new List<XPathTypeSyntax>();
        if (CurrentXPathToken().Kind != XPathTokenKind.RightParenthesis)
        {
            parameters.Add(ParseXPathSequenceType());
            while (MatchXPathKind(XPathTokenKind.Comma))
            {
                parameters.Add(ParseXPathSequenceType());
            }
        }

        ExpectXPathKind(XPathTokenKind.RightParenthesis, "A typed function test requires ')'.");
        ExpectXPathKeyword("as");
        XPathTypeSyntax resultType = ParseXPathSequenceType();
        XPathTypeSyntax result = new XPathTypeSyntax("function", null, "function", null, null, false, CombineXPathRanges(opening.Range, resultType.Range));
        result.ParenthesizedTest = true;
        result.ParameterTypes = parameters.ToArray();
        result.ResultType = resultType;
        return result;
    }

    internal XPathTypeSyntax ParseXPathMapTest()
    {
        XPathToken opening = ExpectXPathKeyword("map");
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "A map test requires '('.");
        if (MatchXPathKind(XPathTokenKind.Star))
        {
            XPathToken wildcardClosing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A wildcard map test requires ')'.");
            XPathTypeSyntax wildcard = new XPathTypeSyntax("map", null, "map", null, null, false, CombineXPathRanges(opening.Range, wildcardClosing.Range));
            wildcard.ParenthesizedTest = true;
            wildcard.WildcardTest = true;
            return wildcard;
        }

        XPathToken keyName = ReadXPathEqName("A typed map test requires an atomic key type.");
        string keyLexical = keyName.Value == null ? keyName.Lexeme : keyName.Value;
        XPathTypeSyntax keyType = new XPathTypeSyntax(keyLexical, keyName.Prefix, keyName.LocalName, keyName.NamespaceUri, null, false, keyName.Range);
        ExpectXPathKind(XPathTokenKind.Comma, "A typed map test requires ','.");
        XPathTypeSyntax valueType = ParseXPathSequenceType();
        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A typed map test requires ')'.");
        XPathTypeSyntax result = new XPathTypeSyntax("map", null, "map", null, null, false, CombineXPathRanges(opening.Range, closing.Range));
        result.ParenthesizedTest = true;
        result.KeyType = keyType;
        result.ValueType = valueType;
        return result;
    }

    internal XPathTypeSyntax ParseXPathArrayTest()
    {
        XPathToken opening = ExpectXPathKeyword("array");
        ExpectXPathKind(XPathTokenKind.LeftParenthesis, "An array test requires '('.");
        if (MatchXPathKind(XPathTokenKind.Star))
        {
            XPathToken wildcardClosing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A wildcard array test requires ')'.");
            XPathTypeSyntax wildcard = new XPathTypeSyntax("array", null, "array", null, null, false, CombineXPathRanges(opening.Range, wildcardClosing.Range));
            wildcard.ParenthesizedTest = true;
            wildcard.WildcardTest = true;
            return wildcard;
        }

        XPathTypeSyntax memberType = ParseXPathSequenceType();
        XPathToken closing = ExpectXPathKind(XPathTokenKind.RightParenthesis, "A typed array test requires ')'.");
        XPathTypeSyntax result = new XPathTypeSyntax("array", null, "array", null, null, false, CombineXPathRanges(opening.Range, closing.Range));
        result.ParenthesizedTest = true;
        result.MemberType = memberType;
        return result;
    }

    internal XPathTypeSyntax ParseXPathSingleType()
    {
        XPathToken name = ReadXPathEqName("A single type was expected.");
        XPathToken end = name;
        string? occurrence = null;
        if (CurrentXPathToken().Kind == XPathTokenKind.Question)
        {
            end = AdvanceXPathToken();
            occurrence = "?";
        }

        string lexical = name.Value == null ? name.Lexeme : name.Value;
        return new XPathTypeSyntax(lexical, name.Prefix, name.LocalName, name.NamespaceUri, occurrence, false, CombineXPathRanges(name.Range, end.Range));
    }

    internal XPathBinding CreateXPathBinding(XPathToken dollar, XPathToken name, XPathAstNode expression)
    {
        string lexical = name.Value == null ? name.Lexeme : name.Value;
        return new XPathBinding(lexical, name.Prefix, name.LocalName, name.NamespaceUri, expression, CombineXPathRanges(dollar.Range, expression.Range));
    }

    internal XPathAstNode CreateXPathBinaryNode(XPathAstKind kind, string operation, XPathAstNode left, XPathAstNode right)
    {
        XPathAstNode result = new XPathAstNode(kind, CombineXPathRanges(left.Range, right.Range));
        result.Operator = operation;
        result.Left = left;
        result.Right = right;
        return result;
    }

    internal XPathAstNode CreateXPathTypeOperation(XPathAstKind kind, string operation, XPathAstNode operand, XPathTypeSyntax type)
    {
        XPathAstNode result = new XPathAstNode(kind, CombineXPathRanges(operand.Range, type.Range));
        result.Operator = operation;
        result.Operand = operand;
        result.Type = type;
        return result;
    }

    internal XPathAstNode CreateXPathNameNode(XPathAstKind kind, XPathToken token)
    {
        XPathAstNode result = new XPathAstNode(kind, token.Range);
        result.Token = token;
        result.Value = token.Value == null ? token.Lexeme : token.Value;
        result.Prefix = token.Prefix;
        result.LocalName = token.LocalName;
        result.NamespaceUri = token.NamespaceUri;
        return result;
    }

    internal SourceRange CombineXPathRanges(SourceRange first, SourceRange last)
    {
        SourceRange result = new SourceRange();
        result.Start = first.Start;
        result.End = last.End;
        return result;
    }

    internal bool CanStartXPathPathStep(XPathToken token)
    {
        if (IsXPathEqNameToken(token))
        {
            return true;
        }

        return token.Kind == XPathTokenKind.IntegerLiteral || token.Kind == XPathTokenKind.DecimalLiteral || token.Kind == XPathTokenKind.DoubleLiteral || token.Kind == XPathTokenKind.StringLiteral || token.Kind == XPathTokenKind.Dollar || token.Kind == XPathTokenKind.Dot || token.Kind == XPathTokenKind.DotDot || token.Kind == XPathTokenKind.At || token.Kind == XPathTokenKind.LeftParenthesis || token.Kind == XPathTokenKind.LeftBracket || token.Kind == XPathTokenKind.Question || token.Kind == XPathTokenKind.Star || token.Kind == XPathTokenKind.StarColon || token.Kind == XPathTokenKind.BracedUriLiteral;
    }

    internal bool IsXPathAxisAt(int distance)
    {
        XPathToken token = PeekXPathToken(distance);
        if (PeekXPathToken(distance + 1).Kind != XPathTokenKind.ColonColon)
        {
            return false;
        }

        return IsXPathKeywordToken(token, "child") || IsXPathKeywordToken(token, "descendant") || IsXPathKeywordToken(token, "attribute") || IsXPathKeywordToken(token, "self") || IsXPathKeywordToken(token, "descendant-or-self") || IsXPathKeywordToken(token, "following-sibling") || IsXPathKeywordToken(token, "following") || IsXPathKeywordToken(token, "namespace") || IsXPathKeywordToken(token, "parent") || IsXPathKeywordToken(token, "ancestor") || IsXPathKeywordToken(token, "preceding-sibling") || IsXPathKeywordToken(token, "preceding") || IsXPathKeywordToken(token, "ancestor-or-self");
    }

    internal XPathAxis ReadXPathAxis()
    {
        XPathToken token = AdvanceXPathToken();
        ExpectXPathKind(XPathTokenKind.ColonColon, "An XPath axis name requires '::'.");
        string name = token.Value == null ? token.Lexeme : token.Value;
        if (name == "descendant")
        {
            return XPathAxis.Descendant;
        }

        if (name == "attribute")
        {
            return XPathAxis.Attribute;
        }

        if (name == "self")
        {
            return XPathAxis.Self;
        }

        if (name == "descendant-or-self")
        {
            return XPathAxis.DescendantOrSelf;
        }

        if (name == "following-sibling")
        {
            return XPathAxis.FollowingSibling;
        }

        if (name == "following")
        {
            return XPathAxis.Following;
        }

        if (name == "namespace")
        {
            return XPathAxis.Namespace;
        }

        if (name == "parent")
        {
            return XPathAxis.Parent;
        }

        if (name == "ancestor")
        {
            return XPathAxis.Ancestor;
        }

        if (name == "preceding-sibling")
        {
            return XPathAxis.PrecedingSibling;
        }

        if (name == "preceding")
        {
            return XPathAxis.Preceding;
        }

        if (name == "ancestor-or-self")
        {
            return XPathAxis.AncestorOrSelf;
        }

        return XPathAxis.Child;
    }

    internal bool IsXPathReverseAxis(XPathAxis axis)
    {
        return axis == XPathAxis.Parent || axis == XPathAxis.Ancestor || axis == XPathAxis.PrecedingSibling || axis == XPathAxis.Preceding || axis == XPathAxis.AncestorOrSelf;
    }

    internal bool IsXPathKindTestAt(int distance)
    {
        XPathToken token = PeekXPathToken(distance);
        if (PeekXPathToken(distance + 1).Kind != XPathTokenKind.LeftParenthesis)
        {
            return false;
        }

        return IsXPathKeywordToken(token, "document-node") || IsXPathKeywordToken(token, "element") || IsXPathKeywordToken(token, "attribute") || IsXPathKeywordToken(token, "schema-element") || IsXPathKeywordToken(token, "schema-attribute") || IsXPathKeywordToken(token, "processing-instruction") || IsXPathKeywordToken(token, "comment") || IsXPathKeywordToken(token, "text") || IsXPathKeywordToken(token, "namespace-node") || IsXPathKeywordToken(token, "node");
    }

    internal bool IsXPathValueComparison(XPathToken token)
    {
        return IsXPathKeywordToken(token, "eq") || IsXPathKeywordToken(token, "ne") || IsXPathKeywordToken(token, "lt") || IsXPathKeywordToken(token, "le") || IsXPathKeywordToken(token, "gt") || IsXPathKeywordToken(token, "ge");
    }

    internal bool IsXPathNodeComparison(XPathToken token)
    {
        return IsXPathKeywordToken(token, "is") || token.Kind == XPathTokenKind.NodeBefore || token.Kind == XPathTokenKind.NodeAfter;
    }

    internal bool IsXPathEqNameToken(XPathToken token)
    {
        return token.Kind == XPathTokenKind.NcName || token.Kind == XPathTokenKind.QName || token.Kind == XPathTokenKind.UriQualifiedName || token.Kind == XPathTokenKind.Keyword;
    }

    internal bool IsXPathEmptyTypeTest(XPathToken token)
    {
        return IsXPathKeywordToken(token, "empty-sequence") || IsXPathKeywordToken(token, "item") || IsXPathKeywordToken(token, "node") || IsXPathKeywordToken(token, "text") || IsXPathKeywordToken(token, "comment") || IsXPathKeywordToken(token, "namespace-node") || IsXPathKeywordToken(token, "processing-instruction") || IsXPathKeywordToken(token, "document-node") || IsXPathKeywordToken(token, "element") || IsXPathKeywordToken(token, "attribute") || IsXPathKeywordToken(token, "schema-element") || IsXPathKeywordToken(token, "schema-attribute");
    }

    internal bool IsXPathWildcardTypeTest(XPathToken token)
    {
        return IsXPathKeywordToken(token, "function") || IsXPathKeywordToken(token, "map") || IsXPathKeywordToken(token, "array");
    }

    internal bool IsXPathKeywordToken(XPathToken token, string keyword)
    {
        return token.Kind == XPathTokenKind.Keyword && token.Value == keyword;
    }

    internal bool IsXPathKeywordAt(int distance, string keyword)
    {
        return IsXPathKeywordToken(PeekXPathToken(distance), keyword);
    }

    internal bool MatchXPathKeyword(string keyword)
    {
        if (!IsXPathKeywordAt(0, keyword))
        {
            return false;
        }

        Position = Position + 1;
        return true;
    }

    internal XPathToken ExpectXPathKeyword(string keyword)
    {
        XPathToken token = CurrentXPathToken();
        if (!IsXPathKeywordToken(token, keyword))
        {
            throw CreateXPathParserError("The XPath keyword '" + keyword + "' was expected.", token);
        }

        Position = Position + 1;
        return token;
    }

    internal bool MatchXPathKind(XPathTokenKind kind)
    {
        if (CurrentXPathToken().Kind != kind)
        {
            return false;
        }

        Position = Position + 1;
        return true;
    }

    internal XPathToken ExpectXPathKind(XPathTokenKind kind, string message)
    {
        XPathToken token = CurrentXPathToken();
        if (token.Kind != kind)
        {
            throw CreateXPathParserError(message, token);
        }

        Position = Position + 1;
        return token;
    }

    internal XPathToken ReadXPathEqName(string message)
    {
        XPathToken token = CurrentXPathToken();
        if (!IsXPathEqNameToken(token))
        {
            throw CreateXPathParserError(message, token);
        }

        Position = Position + 1;
        return token;
    }

    internal XPathToken CurrentXPathToken()
    {
        return PeekXPathToken(0);
    }

    internal XPathToken PeekXPathToken(int distance)
    {
        EnsureXPathTokens();
        int index = Position + distance;
        if (index >= Tokens.Length)
        {
            return Tokens[Tokens.Length - 1];
        }

        return Tokens[index];
    }

    internal void EnsureXPathTokens()
    {
        if (Tokens.Length != 0)
        {
            return;
        }

        XPathLexer lexer = new XPathLexer(Source, SourceUri);
        lexer.ErrorCode = ErrorCode;
        Tokens = lexer.TokenizeXPath();
    }

    internal XPathToken AdvanceXPathToken()
    {
        XPathToken token = CurrentXPathToken();
        if (token.Kind != XPathTokenKind.EndOfInput)
        {
            Position = Position + 1;
        }

        return token;
    }

    internal XPathToken PreviousXPathToken()
    {
        EnsureXPathTokens();
        if (Position == 0)
        {
            return Tokens[0];
        }

        return Tokens[Position - 1];
    }

    internal XPathStaticException CreateXPathParserError(string message, XPathToken token)
    {
        SourceLocation? optionalLocation = token.Range.Start;
        SourceLocation location = optionalLocation == null ? new SourceLocation() : optionalLocation;
        if (optionalLocation == null)
        {
            location.SourceUri = null;
            location.CharacterOffset = token.StartUtf16Offset;
            location.ByteOffset = -1;
            location.Line = 1;
            location.Column = token.StartUtf16Offset + 1;
        }

        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = ErrorCode;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = token.Range;
        return new XPathStaticException(message, diagnostic, location);
    }
}
