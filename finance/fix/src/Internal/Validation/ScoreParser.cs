using System.Collections.Generic;
using System.Text;

namespace fix;

internal class ScoreParser
{
    internal const int MaximumParseDepth = 64;
    internal List<ScoreToken> Tokens = [];
    internal int Position;
    internal int Depth;
    internal ScoreParseResult Result = new ScoreParseResult();

    internal ScoreParser(List<ScoreToken> tokens, ScoreParseResult result)
    {
        Tokens = tokens;
        Result = result;
    }

    internal static ScoreParseResult Parse(string source)
    {
        ScoreParseResult result = new ScoreParseResult();
        ScoreLexResult lexed = ScoreLexer.Lex(source);
        if (!lexed.Success)
        {
            CopyLexError(result, lexed);
            return result;
        }
        ScoreParser parser = new ScoreParser(lexed.Tokens, result);
        if (parser.CurrentToken().Kind == ScoreTokenKind.End)
        {
            parser.SetParseError(parser.CurrentToken().Start, "empty-expression", "The Score expression is empty.");
            return result;
        }

        ScoreSyntaxNode? root = parser.ParseLogicalOr();
        if (root == null)
        {
            return result;
        }
        if (parser.CurrentToken().Kind == ScoreTokenKind.Assign)
        {
            ScoreToken assignment = parser.ConsumeToken();
            if (root.Kind != ScoreSyntaxKind.Reference || root.WasParenthesized)
            {
                parser.SetParseError(assignment.Start, "invalid-assignment-target", "A Score assignment target must be a variable or message field reference.");
                return result;
            }
            ScoreSyntaxNode? value = parser.ParseLogicalOr();
            if (value == null)
            {
                return result;
            }
            ScoreSyntaxNode assigned = new ScoreSyntaxNode();
            assigned.Kind = ScoreSyntaxKind.Assignment;
            assigned.Operator = ScoreTokenKind.Assign;
            assigned.Left = root;
            assigned.Right = value;
            assigned.Start = root.Start;
            assigned.Length = NodeEnd(value) - root.Start;
            root = assigned;
        }
        if (parser.CurrentToken().Kind != ScoreTokenKind.End)
        {
            parser.SetParseError(parser.CurrentToken().Start, "unexpected-token", "The Score expression contains an unexpected token.");
            return result;
        }
        result.Root = root;
        result.Success = true;
        return result;
    }

    internal ScoreSyntaxNode? ParseLogicalOr()
    {
        ScoreSyntaxNode? left = ParseLogicalAnd();
        while (left != null && CurrentToken().Kind == ScoreTokenKind.Or)
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseLogicalAnd();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseLogicalAnd()
    {
        ScoreSyntaxNode? left = ParseEquality();
        while (left != null && CurrentToken().Kind == ScoreTokenKind.And)
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseEquality();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseEquality()
    {
        ScoreSyntaxNode? left = ParseRelational();
        while (left != null && (CurrentToken().Kind == ScoreTokenKind.Equal || CurrentToken().Kind == ScoreTokenKind.NotEqual))
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseRelational();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseRelational()
    {
        ScoreSyntaxNode? left = ParseMembership();
        while (left != null && IsRelationalOperator(CurrentToken().Kind))
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseMembership();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseMembership()
    {
        ScoreSyntaxNode? value = ParseAdditive();
        while (value != null && (CurrentToken().Kind == ScoreTokenKind.In || CurrentToken().Kind == ScoreTokenKind.Between))
        {
            ScoreToken operation = ConsumeToken();
            if (operation.Kind == ScoreTokenKind.In)
            {
                if (!ConsumeRequiredToken(ScoreTokenKind.LeftBrace, "expected-left-brace", "A Score set expression requires an opening brace."))
                {
                    return null;
                }
                ScoreSyntaxNode set = new ScoreSyntaxNode();
                set.Kind = ScoreSyntaxKind.SetMembership;
                set.Operator = ScoreTokenKind.In;
                set.Left = value;
                set.Start = value.Start;
                if (CurrentToken().Kind == ScoreTokenKind.RightBrace)
                {
                    SetParseError(CurrentToken().Start, "empty-set", "A Score set expression requires at least one member.");
                    return null;
                }
                while (true)
                {
                    ScoreSyntaxNode? member = ParseLogicalOr();
                    if (member == null)
                    {
                        return null;
                    }
                    set.Children.Add(member);
                    if (CurrentToken().Kind != ScoreTokenKind.Comma)
                    {
                        break;
                    }
                    ConsumeToken();
                    if (CurrentToken().Kind == ScoreTokenKind.RightBrace)
                    {
                        SetParseError(CurrentToken().Start, "expected-set-member", "A Score set expression has a missing member.");
                        return null;
                    }
                }
                if (!ConsumeRequiredToken(ScoreTokenKind.RightBrace, "expected-right-brace", "A Score set expression requires a closing brace."))
                {
                    return null;
                }
                set.Length = PreviousTokenEnd() - set.Start;
                value = set;
            }
            else
            {
                ScoreSyntaxNode? minimum = ParseAdditive();
                if (minimum == null)
                {
                    return null;
                }
                if (!ConsumeRequiredToken(ScoreTokenKind.And, "expected-range-and", "A Score range expression requires the word and between its bounds."))
                {
                    return null;
                }
                ScoreSyntaxNode? maximum = ParseAdditive();
                if (maximum == null)
                {
                    return null;
                }
                ScoreSyntaxNode range = new ScoreSyntaxNode();
                range.Kind = ScoreSyntaxKind.RangeMembership;
                range.Operator = ScoreTokenKind.Between;
                range.Left = value;
                range.Right = minimum;
                range.Third = maximum;
                range.Start = value.Start;
                range.Length = NodeEnd(maximum) - value.Start;
                value = range;
            }
        }
        return value;
    }

    internal ScoreSyntaxNode? ParseAdditive()
    {
        ScoreSyntaxNode? left = ParseMultiplicative();
        while (left != null && (CurrentToken().Kind == ScoreTokenKind.Plus || CurrentToken().Kind == ScoreTokenKind.Minus))
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseMultiplicative();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseMultiplicative()
    {
        ScoreSyntaxNode? left = ParseUnary();
        while (left != null && (CurrentToken().Kind == ScoreTokenKind.Multiply || CurrentToken().Kind == ScoreTokenKind.Divide || CurrentToken().Kind == ScoreTokenKind.Modulo))
        {
            ScoreToken operation = ConsumeToken();
            ScoreSyntaxNode? right = ParseUnary();
            if (right == null)
            {
                return null;
            }
            left = CreateBinaryNode(left, operation, right);
        }
        return left;
    }

    internal ScoreSyntaxNode? ParseUnary()
    {
        ScoreTokenKind kind = CurrentToken().Kind;
        if (kind == ScoreTokenKind.Minus || kind == ScoreTokenKind.Not)
        {
            ScoreToken operation = ConsumeToken();
            if (!EnterParseDepth(operation.Start))
            {
                return null;
            }
            ScoreSyntaxNode? operand = ParseUnary();
            Depth--;
            if (operand == null)
            {
                return null;
            }
            return CreateUnaryNode(operation, operand);
        }
        if (kind == ScoreTokenKind.Exists)
        {
            return ParseExistsExpression();
        }
        return ParsePrimary();
    }

    internal ScoreSyntaxNode? ParseExistsExpression()
    {
        ScoreToken exists = ConsumeToken();
        ScoreSyntaxNode? reference;
        if (CurrentToken().Kind == ScoreTokenKind.LeftParenthesis)
        {
            ConsumeToken();
            if (CurrentToken().Kind == ScoreTokenKind.UnsignedInteger && PeekToken(1).Kind == ScoreTokenKind.RightParenthesis)
            {
                reference = CreateNumericReference(ConsumeToken());
            }
            else
            {
                reference = ParseReference();
            }
            if (reference == null)
            {
                return null;
            }
            if (!ConsumeRequiredToken(ScoreTokenKind.RightParenthesis, "expected-right-parenthesis", "A parenthesized exists expression requires a closing parenthesis."))
            {
                return null;
            }
        }
        else
        {
            reference = ParseReference();
            if (reference == null)
            {
                return null;
            }
        }
        ScoreSyntaxNode node = new ScoreSyntaxNode();
        node.Kind = ScoreSyntaxKind.Exists;
        node.Operator = ScoreTokenKind.Exists;
        node.Left = reference;
        node.Start = exists.Start;
        node.Length = NodeEnd(reference) - exists.Start;
        if (PreviousToken().Kind == ScoreTokenKind.RightParenthesis)
        {
            node.Length = PreviousTokenEnd() - exists.Start;
        }
        return node;
    }

    internal ScoreSyntaxNode? ParsePrimary()
    {
        ScoreToken token = CurrentToken();
        if (token.Kind == ScoreTokenKind.LeftParenthesis)
        {
            ScoreToken left = ConsumeToken();
            if (!EnterParseDepth(left.Start))
            {
                return null;
            }
            ScoreSyntaxNode? expression = ParseLogicalOr();
            Depth--;
            if (expression == null)
            {
                return null;
            }
            if (!ConsumeRequiredToken(ScoreTokenKind.RightParenthesis, "expected-right-parenthesis", "A parenthesized Score expression requires a closing parenthesis."))
            {
                return null;
            }
            expression.Start = left.Start;
            expression.Length = PreviousTokenEnd() - left.Start;
            expression.WasParenthesized = true;
            return expression;
        }
        if (IsLiteralToken(token.Kind))
        {
            ConsumeToken();
            return CreateLiteralNode(token);
        }
        if (CanStartReference(token))
        {
            return ParseReference();
        }
        SetParseError(token.Start, "expected-expression", "A Score expression operand is missing or invalid.");
        return null;
    }

    internal ScoreSyntaxNode? ParseReference()
    {
        ScoreToken startToken = CurrentToken();
        string scope = "";
        if (startToken.Kind == ScoreTokenKind.Dollar || startToken.Kind == ScoreTokenKind.Caret)
        {
            scope = startToken.Kind == ScoreTokenKind.Dollar ? "$" : "^";
            ConsumeToken();
        }
        else if (startToken.Kind == ScoreTokenKind.In && PeekToken(1).Kind == ScoreTokenKind.Dot)
        {
            scope = "in";
            ConsumeToken();
            ConsumeToken();
        }
        else if (startToken.Kind == ScoreTokenKind.Identifier && (startToken.Text == "out" || startToken.Text == "this") && PeekToken(1).Kind == ScoreTokenKind.Dot)
        {
            scope = startToken.Text;
            ConsumeToken();
            ConsumeToken();
        }

        if (CurrentToken().Kind != ScoreTokenKind.Identifier)
        {
            SetParseError(CurrentToken().Start, "expected-reference", "A Score variable or message field reference is missing.");
            return null;
        }
        ScoreSyntaxNode reference = new ScoreSyntaxNode();
        reference.Kind = ScoreSyntaxKind.Reference;
        reference.Text = scope;
        reference.Start = startToken.Start;
        ScoreSyntaxNode? first = ParseReferencePart();
        if (first == null)
        {
            return null;
        }
        reference.Children.Add(first);
        while (CurrentToken().Kind == ScoreTokenKind.Dot)
        {
            ConsumeToken();
            ScoreSyntaxNode? part = ParseReferencePart();
            if (part == null)
            {
                return null;
            }
            reference.Children.Add(part);
        }
        reference.Length = NodeEnd(reference.Children[reference.Children.Count - 1]) - reference.Start;
        return reference;
    }

    internal ScoreSyntaxNode? ParseReferencePart()
    {
        ScoreToken identifier = CurrentToken();
        if (identifier.Kind != ScoreTokenKind.Identifier)
        {
            SetParseError(identifier.Start, "expected-identifier", "A Score reference qualifier requires an identifier.");
            return null;
        }
        ConsumeToken();
        ScoreSyntaxNode part = new ScoreSyntaxNode();
        part.Kind = ScoreSyntaxKind.ReferencePart;
        part.Text = identifier.Text;
        part.Start = identifier.Start;
        part.Length = identifier.Length;
        if (CurrentToken().Kind != ScoreTokenKind.LeftBracket)
        {
            return part;
        }

        ConsumeToken();
        if (CurrentToken().Kind == ScoreTokenKind.UnsignedInteger && PeekToken(1).Kind == ScoreTokenKind.RightBracket)
        {
            ScoreToken ordinal = ConsumeToken();
            ScoreSyntaxNode selector = CreateLiteralNode(ordinal);
            part.Left = selector;
        }
        else
        {
            ScoreToken predicateName = CurrentToken();
            if (predicateName.Kind != ScoreTokenKind.Identifier)
            {
                SetParseError(predicateName.Start, "expected-predicate-field", "A Score group selector requires an ordinal or predicate field name.");
                return null;
            }
            ConsumeToken();
            if (!ConsumeRequiredToken(ScoreTokenKind.Equal, "expected-predicate-equality", "A Score group predicate selector requires an equality operator."))
            {
                return null;
            }
            ScoreSyntaxNode? predicateValue = ParseLogicalOr();
            if (predicateValue == null)
            {
                return null;
            }
            ScoreSyntaxNode selector = new ScoreSyntaxNode();
            selector.Kind = ScoreSyntaxKind.PredicateSelector;
            selector.Operator = ScoreTokenKind.Equal;
            selector.Text = predicateName.Text;
            selector.Start = predicateName.Start;
            selector.Right = predicateValue;
            selector.Length = NodeEnd(predicateValue) - predicateName.Start;
            part.Left = selector;
        }
        if (!ConsumeRequiredToken(ScoreTokenKind.RightBracket, "expected-right-bracket", "A Score group selector requires a closing bracket."))
        {
            return null;
        }
        part.Length = PreviousTokenEnd() - part.Start;
        return part;
    }

    internal ScoreSyntaxNode CreateNumericReference(ScoreToken token)
    {
        ScoreSyntaxNode part = new ScoreSyntaxNode();
        part.Kind = ScoreSyntaxKind.ReferencePart;
        part.Text = token.Text;
        part.Start = token.Start;
        part.Length = token.Length;
        ScoreSyntaxNode reference = new ScoreSyntaxNode();
        reference.Kind = ScoreSyntaxKind.Reference;
        reference.Start = token.Start;
        reference.Length = token.Length;
        reference.Children.Add(part);
        return reference;
    }

    internal ScoreSyntaxNode CreateBinaryNode(ScoreSyntaxNode left, ScoreToken operation, ScoreSyntaxNode right)
    {
        ScoreSyntaxNode node = new ScoreSyntaxNode();
        node.Kind = ScoreSyntaxKind.Binary;
        node.Operator = operation.Kind;
        node.Left = left;
        node.Right = right;
        node.Start = left.Start;
        node.Length = NodeEnd(right) - left.Start;
        return node;
    }

    internal ScoreSyntaxNode CreateUnaryNode(ScoreToken operation, ScoreSyntaxNode operand)
    {
        ScoreSyntaxNode node = new ScoreSyntaxNode();
        node.Kind = ScoreSyntaxKind.Unary;
        node.Operator = operation.Kind;
        node.Left = operand;
        node.Start = operation.Start;
        node.Length = NodeEnd(operand) - operation.Start;
        return node;
    }

    internal ScoreSyntaxNode CreateLiteralNode(ScoreToken token)
    {
        ScoreSyntaxNode node = new ScoreSyntaxNode();
        node.Kind = SyntaxKindForLiteral(token.Kind);
        node.Text = LiteralValue(token);
        node.Start = token.Start;
        node.Length = token.Length;
        return node;
    }

    internal static ScoreSyntaxKind SyntaxKindForLiteral(ScoreTokenKind kind)
    {
        if (kind == ScoreTokenKind.UnsignedInteger) return ScoreSyntaxKind.IntegerLiteral;
        if (kind == ScoreTokenKind.Decimal) return ScoreSyntaxKind.DecimalLiteral;
        if (kind == ScoreTokenKind.Character) return ScoreSyntaxKind.CharacterLiteral;
        if (kind == ScoreTokenKind.String) return ScoreSyntaxKind.StringLiteral;
        if (kind == ScoreTokenKind.Date) return ScoreSyntaxKind.DateLiteral;
        if (kind == ScoreTokenKind.Time) return ScoreSyntaxKind.TimeLiteral;
        if (kind == ScoreTokenKind.Timestamp) return ScoreSyntaxKind.TimestampLiteral;
        if (kind == ScoreTokenKind.Duration) return ScoreSyntaxKind.DurationLiteral;
        return ScoreSyntaxKind.Unknown;
    }

    internal static string LiteralValue(ScoreToken token)
    {
        if (token.Kind == ScoreTokenKind.String || token.Kind == ScoreTokenKind.Character)
        {
            return DecodeQuotedToken(token.Text);
        }
        if (token.Kind == ScoreTokenKind.Date || token.Kind == ScoreTokenKind.Time || token.Kind == ScoreTokenKind.Timestamp || token.Kind == ScoreTokenKind.Duration)
        {
            return token.Text.Substring(1, token.Text.Length - 2);
        }
        return token.Text;
    }

    internal static string DecodeQuotedToken(string text)
    {
        StringBuilder builder = new StringBuilder();
        int index = 1;
        while (index < text.Length - 1)
        {
            char current = text[index];
            if (current != '\\')
            {
                builder.Append(current);
                index++;
                continue;
            }
            char escaped = text[index + 1];
            if (escaped == 'b') builder.Append('\b');
            else if (escaped == 't') builder.Append('\t');
            else if (escaped == 'n') builder.Append('\n');
            else if (escaped == 'f') builder.Append('\f');
            else if (escaped == 'r') builder.Append('\r');
            else builder.Append(escaped);
            index += 2;
        }
        return builder.ToString();
    }

    internal bool ConsumeRequiredToken(ScoreTokenKind kind, string code, string description)
    {
        if (CurrentToken().Kind != kind)
        {
            SetParseError(CurrentToken().Start, code, description);
            return false;
        }
        ConsumeToken();
        return true;
    }

    internal bool EnterParseDepth(int offset)
    {
        Depth++;
        if (Depth > MaximumParseDepth)
        {
            SetParseError(offset, "expression-depth-limit-exceeded", "The Score expression exceeds the supported nesting depth.");
            Depth--;
            return false;
        }
        return true;
    }

    internal ScoreToken CurrentToken()
    {
        return Tokens[Position];
    }

    internal ScoreToken PeekToken(int distance)
    {
        int index = Position + distance;
        if (index >= Tokens.Count)
        {
            return Tokens[Tokens.Count - 1];
        }
        return Tokens[index];
    }

    internal ScoreToken ConsumeToken()
    {
        ScoreToken token = CurrentToken();
        if (Position + 1 < Tokens.Count)
        {
            Position++;
        }
        return token;
    }

    internal ScoreToken PreviousToken()
    {
        int index = Position == 0 ? 0 : Position - 1;
        return Tokens[index];
    }

    internal int PreviousTokenEnd()
    {
        ScoreToken token = PreviousToken();
        return token.Start + token.Length;
    }

    internal static int NodeEnd(ScoreSyntaxNode node)
    {
        return node.Start + node.Length;
    }

    internal static bool IsRelationalOperator(ScoreTokenKind kind)
    {
        return kind == ScoreTokenKind.Less || kind == ScoreTokenKind.LessEqual || kind == ScoreTokenKind.Greater || kind == ScoreTokenKind.GreaterEqual;
    }

    internal static bool IsLiteralToken(ScoreTokenKind kind)
    {
        return kind == ScoreTokenKind.UnsignedInteger || kind == ScoreTokenKind.Decimal || kind == ScoreTokenKind.Character || kind == ScoreTokenKind.String || kind == ScoreTokenKind.Date || kind == ScoreTokenKind.Time || kind == ScoreTokenKind.Timestamp || kind == ScoreTokenKind.Duration;
    }

    internal bool CanStartReference(ScoreToken token)
    {
        if (token.Kind == ScoreTokenKind.Identifier || token.Kind == ScoreTokenKind.Dollar || token.Kind == ScoreTokenKind.Caret)
        {
            return true;
        }
        return token.Kind == ScoreTokenKind.In && PeekToken(1).Kind == ScoreTokenKind.Dot;
    }

    internal void SetParseError(int offset, string code, string description)
    {
        if (Result.ErrorCode.Length != 0)
        {
            return;
        }
        Result.ErrorOffset = offset;
        Result.ErrorCode = code;
        Result.ErrorMessage = description;
    }

    internal static void CopyLexError(ScoreParseResult result, ScoreLexResult lexed)
    {
        result.ErrorOffset = lexed.ErrorOffset;
        result.ErrorCode = lexed.ErrorCode;
        result.ErrorMessage = lexed.ErrorMessage;
    }
}
