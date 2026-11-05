using System.Collections.Generic;

namespace odf;

internal static class FormulaParser
{
    internal static OdfFormula Parse(string source, int maximumDepth)
    {
        if (source == null)
        {
            throw new OdfFormulaException("A formula is required for parsing.");
        }
        if (maximumDepth <= 0)
        {
            throw new OdfFormulaException(
                "The formula nesting limit must be positive.");
        }
        OdfFormulaToken[] tokens = FormulaLexer.Tokenize(source);
        FormulaParserState state = new FormulaParserState(
            source,
            tokens,
            maximumDepth);
        return state.ParseFormula();
    }
}

internal class FormulaParserState
{
    internal string Source;
    internal OdfFormulaToken[] Tokens;
    internal int MaximumDepth;
    internal int Index;
    internal bool DepthExceeded;
    internal List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();

    internal FormulaParserState(
        string source,
        OdfFormulaToken[] tokens,
        int maximumDepth)
    {
        Source = source;
        Tokens = tokens;
        MaximumDepth = maximumDepth;
    }

    internal OdfFormula ParseFormula()
    {
        OdfFormula result = new OdfFormula();
        result.Source = Source;
        result.Tokens = Tokens;
        if (CurrentToken().Kind == OdfFormulaTokenKind.Prefix)
        {
            OdfFormulaToken prefix = CurrentToken();
            result.NamespacePrefix = PrefixName(prefix.Text);
            result.ForcedRecalculation = IsForcedPrefix(prefix.Text);
            Index = Index + 1;
        }
        SkipWhitespace();
        if (CurrentToken().Kind == OdfFormulaTokenKind.End)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                "A formula expression is required.");
            result.Root = CreateUnknownNode(CurrentToken());
        }
        else
        {
            result.Root = ParseExpression(0, 1);
        }
        SkipWhitespace();
        while (CurrentToken().Kind != OdfFormulaTokenKind.End)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                $"Unexpected token '{CurrentToken().Text}'.");
            Index = Index + 1;
            SkipWhitespace();
        }
        result.Diagnostics = Diagnostics.ToArray();
        result.IsValid = !HasErrorDiagnostic();
        if (!result.IsValid && result.Root == null)
        {
            result.Root = CreateUnknownNode(CurrentToken());
        }
        if (result.Root != null)
        {
            FormulaReferenceModel.BindReferences(result.Root, Source);
            FormulaFunctionCatalog.BindMetadata(result.Root);
        }
        result.OriginalNamespacePrefix = result.NamespacePrefix;
        result.OriginalForcedRecalculation = result.ForcedRecalculation;
        result.OriginalRoot = result.Root;
        if (result.Root != null)
        {
            SnapshotNode(result.Root);
        }
        return result;
    }

    OdfFormulaNode ParseExpression(int minimumPrecedence, int depth)
    {
        if (!RequireDepth(depth))
        {
            return CreateUnknownNode(CurrentToken());
        }
        OdfFormulaNode left = ParsePrefixExpression(depth);
        bool reading = true;
        while (reading && !DepthExceeded)
        {
            SkipWhitespace();
            OdfFormulaToken token = CurrentToken();
            if (token.Kind == OdfFormulaTokenKind.Operator
                && token.Text == "%")
            {
                int postfixPrecedence = 65;
                if (postfixPrecedence < minimumPrecedence)
                {
                    reading = false;
                }
                else
                {
                    Index = Index + 1;
                    left = CreateUnaryNode(
                        OdfFormulaNodeKind.UnaryOperation,
                        token.Text,
                        left,
                        left.Start,
                        TokenEnd(token));
                }
            }
            else if (token.Kind == OdfFormulaTokenKind.Operator
                && IsBinaryOperator(token.Text))
            {
                int precedence = BinaryPrecedence(token.Text);
                if (precedence < minimumPrecedence)
                {
                    reading = false;
                }
                else
                {
                    Index = Index + 1;
                    SkipWhitespace();
                    OdfFormulaNode right = ParseExpression(
                        precedence + 1,
                        depth);
                    left = CreateBinaryNode(token, left, right);
                }
            }
            else
            {
                reading = false;
            }
        }
        return left;
    }

    OdfFormulaNode ParsePrefixExpression(int depth)
    {
        SkipWhitespace();
        OdfFormulaToken token = CurrentToken();
        if (token.Kind == OdfFormulaTokenKind.Operator
            && (token.Text == "+" || token.Text == "-"))
        {
            Index = Index + 1;
            OdfFormulaNode operand = ParseExpression(66, depth + 1);
            return CreateUnaryNode(
                OdfFormulaNodeKind.UnaryOperation,
                token.Text,
                operand,
                token.Start,
                NodeEnd(operand));
        }
        return ParsePrimaryExpression(depth);
    }

    OdfFormulaNode ParsePrimaryExpression(int depth)
    {
        SkipWhitespace();
        OdfFormulaToken token = CurrentToken();
        if (token.Kind == OdfFormulaTokenKind.Number
            || token.Kind == OdfFormulaTokenKind.Percentage
            || token.Kind == OdfFormulaTokenKind.String
            || token.Kind == OdfFormulaTokenKind.Boolean)
        {
            Index = Index + 1;
            return CreateLeafNode(OdfFormulaNodeKind.Literal, token);
        }
        if (token.Kind == OdfFormulaTokenKind.Error)
        {
            Index = Index + 1;
            return CreateLeafNode(OdfFormulaNodeKind.Error, token);
        }
        if (token.Kind == OdfFormulaTokenKind.NamedExpression
            || token.Kind == OdfFormulaTokenKind.Identifier)
        {
            Index = Index + 1;
            return CreateLeafNode(OdfFormulaNodeKind.Name, token);
        }
        if (token.Kind == OdfFormulaTokenKind.CellReference)
        {
            Index = Index + 1;
            return CreateLeafNode(OdfFormulaNodeKind.Reference, token);
        }
        if (token.Kind == OdfFormulaTokenKind.RangeReference)
        {
            Index = Index + 1;
            return CreateLeafNode(OdfFormulaNodeKind.Range, token);
        }
        if (token.Kind == OdfFormulaTokenKind.ExternalReference)
        {
            Index = Index + 1;
            return CreateLeafNode(
                OdfFormulaNodeKind.ExternalReference,
                token);
        }
        if (token.Kind == OdfFormulaTokenKind.Function)
        {
            return ParseFunctionCall(depth + 1);
        }
        if (token.Kind == OdfFormulaTokenKind.LeftParenthesis)
        {
            return ParseGroupedExpression(depth + 1);
        }
        if (token.Kind == OdfFormulaTokenKind.LeftBrace)
        {
            return ParseArrayValue(depth + 1);
        }
        if (token.Kind == OdfFormulaTokenKind.Unknown)
        {
            AddInvalidDiagnostic(token, $"Unknown token '{token.Text}'.");
            Index = Index + 1;
            return CreateUnknownNode(token);
        }
        AddInvalidDiagnostic(token, "An expression was expected.");
        if (token.Kind != OdfFormulaTokenKind.End)
        {
            Index = Index + 1;
        }
        return CreateUnknownNode(token);
    }

    OdfFormulaNode ParseGroupedExpression(int depth)
    {
        OdfFormulaToken opening = CurrentToken();
        Index = Index + 1;
        if (!RequireDepth(depth))
        {
            return CreateUnknownNode(opening);
        }
        SkipWhitespace();
        if (CurrentToken().Kind == OdfFormulaTokenKind.RightParenthesis)
        {
            OdfFormulaToken emptyClose = CurrentToken();
            AddInvalidDiagnostic(emptyClose, "A grouped expression cannot be empty.");
            Index = Index + 1;
            return CreateUnknownSpan(opening.Start, TokenEnd(emptyClose));
        }
        OdfFormulaNode expression = ParseExpression(0, depth);
        SkipWhitespace();
        if (CurrentToken().Kind != OdfFormulaTokenKind.RightParenthesis)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                "A closing parenthesis is required.");
            expression.Start = opening.Start;
            expression.Length = NodeEnd(expression) - opening.Start;
            return expression;
        }
        OdfFormulaToken closing = CurrentToken();
        Index = Index + 1;
        expression.Start = opening.Start;
        expression.Length = TokenEnd(closing) - opening.Start;
        return expression;
    }

    OdfFormulaNode ParseFunctionCall(int depth)
    {
        OdfFormulaToken function = CurrentToken();
        Index = Index + 1;
        SkipWhitespace();
        if (CurrentToken().Kind != OdfFormulaTokenKind.LeftParenthesis)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                $"Function '{function.Text}' requires a parameter list.");
            return CreateLeafNode(OdfFormulaNodeKind.Name, function);
        }
        OdfFormulaToken opening = CurrentToken();
        Index = Index + 1;
        if (!RequireDepth(depth))
        {
            return CreateUnknownSpan(function.Start, TokenEnd(opening));
        }
        List<OdfFormulaNode> arguments = new List<OdfFormulaNode>();
        List<string> separators = new List<string>();
        SkipWhitespace();
        bool expectArgument = true;
        while (CurrentToken().Kind != OdfFormulaTokenKind.RightParenthesis
            && CurrentToken().Kind != OdfFormulaTokenKind.End
            && !DepthExceeded)
        {
            if (CurrentToken().Kind == OdfFormulaTokenKind.Separator)
            {
                arguments.Add(CreateEmptyNode(CurrentToken().Start));
                separators.Add(CurrentToken().Text);
                Index = Index + 1;
                SkipWhitespace();
                expectArgument = true;
            }
            else
            {
                arguments.Add(ParseExpression(0, depth));
                SkipWhitespace();
                expectArgument = false;
                if (CurrentToken().Kind == OdfFormulaTokenKind.Separator)
                {
                    separators.Add(CurrentToken().Text);
                    Index = Index + 1;
                    SkipWhitespace();
                    expectArgument = true;
                }
                else if (CurrentToken().Kind
                    != OdfFormulaTokenKind.RightParenthesis)
                {
                    AddInvalidDiagnostic(
                        CurrentToken(),
                        "A function argument separator or closing parenthesis is required.");
                    RecoverFunctionArgument();
                }
            }
        }
        if (expectArgument && arguments.Count > 0)
        {
            arguments.Add(CreateEmptyNode(CurrentToken().Start));
        }
        int end = CurrentToken().Start;
        if (CurrentToken().Kind == OdfFormulaTokenKind.RightParenthesis)
        {
            end = TokenEnd(CurrentToken());
            Index = Index + 1;
        }
        else if (!DepthExceeded)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                $"Function '{function.Text}' requires a closing parenthesis.");
        }
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = OdfFormulaNodeKind.FunctionCall;
        result.Text = function.Text;
        result.Children = arguments.ToArray();
        result.Separators = separators.ToArray();
        result.Start = function.Start;
        result.Length = end - function.Start;
        return result;
    }

    OdfFormulaNode ParseArrayValue(int depth)
    {
        OdfFormulaToken opening = CurrentToken();
        Index = Index + 1;
        if (!RequireDepth(depth))
        {
            return CreateUnknownNode(opening);
        }
        List<OdfFormulaNode> values = new List<OdfFormulaNode>();
        List<string> separators = new List<string>();
        SkipWhitespace();
        bool expectValue = true;
        while (CurrentToken().Kind != OdfFormulaTokenKind.RightBrace
            && CurrentToken().Kind != OdfFormulaTokenKind.End
            && !DepthExceeded)
        {
            if (CurrentToken().Kind == OdfFormulaTokenKind.Separator
                || CurrentToken().Kind
                    == OdfFormulaTokenKind.ArrayRowSeparator)
            {
                AddInvalidDiagnostic(
                    CurrentToken(),
                    "Inline array values cannot be empty.");
                Index = Index + 1;
                SkipWhitespace();
                expectValue = true;
            }
            else
            {
                values.Add(ParseExpression(0, depth));
                SkipWhitespace();
                expectValue = false;
                if (CurrentToken().Kind == OdfFormulaTokenKind.Separator
                    || CurrentToken().Kind
                        == OdfFormulaTokenKind.ArrayRowSeparator)
                {
                    separators.Add(CurrentToken().Text);
                    Index = Index + 1;
                    SkipWhitespace();
                    expectValue = true;
                }
                else if (CurrentToken().Kind != OdfFormulaTokenKind.RightBrace)
                {
                    AddInvalidDiagnostic(
                        CurrentToken(),
                        "An array separator or closing brace is required.");
                    RecoverArrayValue();
                }
            }
        }
        if (values.Count == 0 || expectValue)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                "An inline array requires a value after each separator.");
        }
        int end = CurrentToken().Start;
        if (CurrentToken().Kind == OdfFormulaTokenKind.RightBrace)
        {
            end = TokenEnd(CurrentToken());
            Index = Index + 1;
        }
        else if (!DepthExceeded)
        {
            AddInvalidDiagnostic(
                CurrentToken(),
                "An inline array requires a closing brace.");
        }
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = OdfFormulaNodeKind.Array;
        result.Text = Source.Substring(opening.Start, end - opening.Start);
        result.Children = values.ToArray();
        result.Separators = separators.ToArray();
        result.Start = opening.Start;
        result.Length = end - opening.Start;
        return result;
    }

    void RecoverFunctionArgument()
    {
        while (CurrentToken().Kind != OdfFormulaTokenKind.Separator
            && CurrentToken().Kind != OdfFormulaTokenKind.RightParenthesis
            && CurrentToken().Kind != OdfFormulaTokenKind.End)
        {
            Index = Index + 1;
        }
        if (CurrentToken().Kind == OdfFormulaTokenKind.Separator)
        {
            Index = Index + 1;
        }
        SkipWhitespace();
    }

    void RecoverArrayValue()
    {
        while (CurrentToken().Kind != OdfFormulaTokenKind.Separator
            && CurrentToken().Kind != OdfFormulaTokenKind.ArrayRowSeparator
            && CurrentToken().Kind != OdfFormulaTokenKind.RightBrace
            && CurrentToken().Kind != OdfFormulaTokenKind.End)
        {
            Index = Index + 1;
        }
        if (CurrentToken().Kind == OdfFormulaTokenKind.Separator
            || CurrentToken().Kind == OdfFormulaTokenKind.ArrayRowSeparator)
        {
            Index = Index + 1;
        }
        SkipWhitespace();
    }

    bool RequireDepth(int depth)
    {
        if (depth <= MaximumDepth)
        {
            return true;
        }
        if (!DepthExceeded)
        {
            DepthExceeded = true;
            OdfFormulaToken token = CurrentToken();
            OdfLocation location = TokenLocation(token);
            Diagnostics.Add(OdfDiagnosticFactory.Create(
                OdfDiagnosticCodes.FormulaDepthLimitExceeded,
                $"The formula exceeds the nesting limit of {MaximumDepth}.",
                OdfValidationSeverity.Fatal,
                location,
                null,
                OdfVersion.Unknown));
        }
        MoveToEnd();
        return false;
    }

    void MoveToEnd()
    {
        while (CurrentToken().Kind != OdfFormulaTokenKind.End)
        {
            Index = Index + 1;
        }
    }

    OdfFormulaNode CreateLeafNode(
        OdfFormulaNodeKind kind,
        OdfFormulaToken token)
    {
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = kind;
        result.Text = token.Text;
        result.Value = token.Text;
        result.TokenKind = token.Kind;
        if (token.Kind == OdfFormulaTokenKind.String)
        {
            result.Value = FormulaFormatter.UnescapeString(token.Text);
        }
        result.Start = token.Start;
        result.Length = token.Length;
        return result;
    }

    OdfFormulaNode CreateUnknownNode(OdfFormulaToken token)
    {
        return CreateLeafNode(OdfFormulaNodeKind.Unknown, token);
    }

    OdfFormulaNode CreateUnknownSpan(int start, int end)
    {
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = OdfFormulaNodeKind.Unknown;
        result.Text = Source.Substring(start, end - start);
        result.Start = start;
        result.Length = end - start;
        return result;
    }

    OdfFormulaNode CreateEmptyNode(int start)
    {
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = OdfFormulaNodeKind.Unknown;
        result.Start = start;
        return result;
    }

    OdfFormulaNode CreateUnaryNode(
        OdfFormulaNodeKind kind,
        string text,
        OdfFormulaNode operand,
        int start,
        int end)
    {
        OdfFormulaNode result = new OdfFormulaNode();
        result.Kind = kind;
        result.Text = text;
        result.Children = [operand];
        result.Start = start;
        result.Length = end - start;
        return result;
    }

    OdfFormulaNode CreateBinaryNode(
        OdfFormulaToken token,
        OdfFormulaNode left,
        OdfFormulaNode right)
    {
        OdfFormulaNode result = new OdfFormulaNode();
        if (token.Text == "!" || token.Text == "!!")
        {
            result.Kind = OdfFormulaNodeKind.Intersection;
        }
        else if (token.Text == "~")
        {
            result.Kind = OdfFormulaNodeKind.ReferenceConcatenation;
        }
        else if (token.Text == ":")
        {
            result.Kind = OdfFormulaNodeKind.Range;
        }
        else
        {
            result.Kind = OdfFormulaNodeKind.BinaryOperation;
        }
        result.Text = token.Text;
        result.Children = [left, right];
        result.Start = left.Start;
        result.Length = NodeEnd(right) - left.Start;
        return result;
    }

    bool IsBinaryOperator(string text)
    {
        return text == ":"
            || text == "!"
            || text == "!!"
            || text == "~"
            || text == "^"
            || text == "*"
            || text == "/"
            || text == "+"
            || text == "-"
            || text == "&"
            || text == "="
            || text == "<>"
            || text == "<"
            || text == ">"
            || text == "<="
            || text == ">=";
    }

    int BinaryPrecedence(string text)
    {
        if (text == ":")
        {
            return 90;
        }
        if (text == "!" || text == "!!")
        {
            return 80;
        }
        if (text == "~")
        {
            return 70;
        }
        if (text == "^")
        {
            return 60;
        }
        if (text == "*" || text == "/")
        {
            return 50;
        }
        if (text == "+" || text == "-")
        {
            return 40;
        }
        if (text == "&")
        {
            return 30;
        }
        return 20;
    }

    void SkipWhitespace()
    {
        while (CurrentToken().Kind == OdfFormulaTokenKind.Whitespace)
        {
            Index = Index + 1;
        }
    }

    OdfFormulaToken CurrentToken()
    {
        if (Index >= Tokens.Length)
        {
            return Tokens[Tokens.Length - 1];
        }
        return Tokens[Index];
    }

    void AddInvalidDiagnostic(OdfFormulaToken token, string message)
    {
        Diagnostics.Add(OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.InvalidFormula,
            message,
            OdfValidationSeverity.Error,
            TokenLocation(token),
            null,
            OdfVersion.Unknown));
    }

    OdfLocation TokenLocation(OdfFormulaToken token)
    {
        OdfLocation result = new OdfLocation();
        result.TextOffset = token.Start;
        result.TextLength = token.Length;
        return result;
    }

    bool HasErrorDiagnostic()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].Severity == OdfValidationSeverity.Error
                || Diagnostics[index].Severity == OdfValidationSeverity.Fatal)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    string PrefixName(string text)
    {
        int separator = text.IndexOf(':');
        return separator < 0 ? "" : text.Substring(0, separator);
    }

    bool IsForcedPrefix(string text)
    {
        return text.Length >= 2
            && text[text.Length - 1] == '='
            && text[text.Length - 2] == '=';
    }

    int TokenEnd(OdfFormulaToken token)
    {
        return token.Start + token.Length;
    }

    int NodeEnd(OdfFormulaNode node)
    {
        return node.Start + node.Length;
    }

    void SnapshotNode(OdfFormulaNode node)
    {
        node.OriginalKind = node.Kind;
        node.OriginalText = node.Text;
        node.OriginalValue = node.Value;
        node.OriginalTokenKind = node.TokenKind;
        node.OriginalChildCount = node.Children.Length;
        node.OriginalSeparators = CopySeparators(node.Separators);
        int index = 0;
        while (index < node.Children.Length)
        {
            SnapshotNode(node.Children[index]);
            index = index + 1;
        }
    }

    string[] CopySeparators(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }
}
