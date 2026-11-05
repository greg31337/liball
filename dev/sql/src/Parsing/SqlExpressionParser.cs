using System;
using System.Collections.Generic;

namespace sql;

internal class SqlExpressionParser
{
    internal SqlParserContext Context;

    internal SqlExpressionParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlExpression ParseExpression()
    {
        return ParseExpressionWithPrecedence(0);
    }

    internal SqlExpression ParseExpressionWithPrecedence(int parentPrecedence)
    {
        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        SkipTrivia();
        SqlExpression left = ParsePrefixExpression();

        while (true)
        {
            SkipTrivia();
            SqlExpression? subscript = TryParseSubscriptExpression(left, parentPrecedence);
            if (subscript != null)
            {
                left = subscript;
                continue;
            }

            SkipTrivia();
            SqlExpression? postgreSqlCast = TryParsePostgreSqlCastExpression(left, parentPrecedence);
            if (postgreSqlCast != null)
            {
                left = postgreSqlCast;
                continue;
            }

            SkipTrivia();
            SqlExpression? window = TryParseWindowExpression(left, parentPrecedence);
            if (window != null)
            {
                left = window;
                continue;
            }

            SkipTrivia();
            SqlExpression? collate = TryParseCollateExpression(left, parentPrecedence);
            if (collate != null)
            {
                left = collate;
                continue;
            }

            SkipTrivia();
            SqlExpression? special = TryParseSpecialExpression(left, parentPrecedence);
            if (special != null)
            {
                left = special;
                continue;
            }

            SkipTrivia();
            SqlToken operatorToken = Context.Peek();
            int precedence = GetBinaryPrecedence(operatorToken);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            Context.Consume();
            int rightPrecedence = precedence;
            if (operatorToken.Kind == SqlTokenKind.CustomOperator && Context.Options.Dialect.IsCustomOperatorRightAssociative(operatorToken.Text))
            {
                rightPrecedence = precedence - 1;
            }

            SqlExpression right = ParseExpressionWithPrecedence(rightPrecedence);
            SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, right.Span.GetEnd());
            SqlExpressionKind binaryKind = SqlExpressionKind.Binary;
            if (operatorToken.Kind == SqlTokenKind.Arrow || operatorToken.Kind == SqlTokenKind.DoubleArrow || (Context.Options.Dialect.Kind == SqlDialectKind.PostgreSql && IsJsonOperatorText(operatorToken.Text)))
            {
                binaryKind = SqlExpressionKind.JsonOperator;
            }

            SqlExpression binary = SqlExpression.Create(binaryKind, span, new SqlToken[] { operatorToken });
            binary.Left = left;
            binary.Right = right;
            binary.OperatorText = operatorToken.Text;
            binary.OperatorToken = operatorToken;
            binary.BinaryOperator = GetBinaryOperatorKind(operatorToken);
            if (binaryKind == SqlExpressionKind.JsonOperator || operatorToken.Kind == SqlTokenKind.CustomOperator)
            {
                SqlOperatorExpressionKind operatorKind = SqlOperatorExpressionKind.Custom;
                if (binaryKind == SqlExpressionKind.JsonOperator)
                {
                    operatorKind = SqlOperatorExpressionKind.Json;
                }

                binary.OperatorData = new SqlOperatorExpressionData(left, right, operatorToken.Text, precedence, operatorKind);
            }
            left = binary;
        }

        return left;
    }

    internal SqlExpression? TryParseWindowExpression(SqlExpression left, int parentPrecedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "OVER"))
        {
            return null;
        }

        int precedence = 100;
        if (precedence <= parentPrecedence)
        {
            return null;
        }

        SqlToken over = Context.Consume();
        if (!Context.Options.Dialect.AllowWindowFunctions)
        {
            Context.AddUnsupportedFeature("window functions", over.Span);
        }

        SkipTrivia();
        bool namedWindow = Context.Peek().Kind != SqlTokenKind.OpenParen;
        SqlWindowDefinition windowDefinition = ParseOverWindowDefinition(over);
        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, windowDefinition.Span.GetEnd());
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Window, span, new SqlToken[] { over });
        expression.Operand = left;
        expression.WindowDefinition = windowDefinition;
        if (left.FunctionData != null)
        {
            if (namedWindow)
            {
                left.FunctionData.WindowName = windowDefinition.BaseWindowName;
            }
            else
            {
                left.FunctionData.WindowDefinition = windowDefinition;
            }

            expression.FunctionData = left.FunctionData;
        }
        return expression;
    }

    internal SqlExpression? TryParseCollateExpression(SqlExpression left, int parentPrecedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "COLLATE"))
        {
            return null;
        }

        int precedence = 85;
        if (precedence <= parentPrecedence)
        {
            return null;
        }

        SqlToken collate = Context.Consume();
        SkipTrivia();
        SqlName? collation = null;
        int end = collate.Span.GetEnd();
        if (IsIdentifierLike(Context.Peek()))
        {
            SqlToken nameToken = Context.Consume();
            collation = CreateSinglePartName(nameToken);
            end = nameToken.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR030", "Expected collation name after COLLATE.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Collate, span, new SqlToken[] { collate });
        expression.Operand = left;
        expression.Name = collation;
        expression.OperatorText = collate.Text;
        expression.OperatorToken = collate;
        if (collation != null)
        {
            expression.CollateData = new SqlCollateExpressionData(left, collation);
        }
        return expression;
    }

    internal SqlExpression? TryParseSubscriptExpression(SqlExpression left, int parentPrecedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        if (Context.Peek().Kind != SqlTokenKind.OpenBracket || 100 <= parentPrecedence)
        {
            return null;
        }

        List<SqlToken> tokens = new List<SqlToken>();
        SqlToken open = Context.Consume();
        tokens.Add(open);
        SkipTrivia();
        SqlExpression? lower = null;
        SqlExpression? upper = null;
        bool isSlice = false;
        if (Context.Peek().Kind != SqlTokenKind.Colon && Context.Peek().Kind != SqlTokenKind.CloseBracket)
        {
            lower = ParseExpressionWithPrecedence(0);
            SkipTrivia();
        }

        if (Context.Peek().Kind == SqlTokenKind.Colon)
        {
            tokens.Add(Context.Consume());
            isSlice = true;
            SkipTrivia();
            if (Context.Peek().Kind != SqlTokenKind.CloseBracket)
            {
                upper = ParseExpressionWithPrecedence(0);
                SkipTrivia();
            }
        }

        int end = open.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.CloseBracket)
        {
            SqlToken close = Context.Consume();
            tokens.Add(close);
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR032", "Expected closing bracket in subscript expression.", Context.Peek().Span);
            if (upper != null)
            {
                end = upper.Span.GetEnd();
            }
            else if (lower != null)
            {
                end = lower.Span.GetEnd();
            }
        }

        if (!isSlice && lower == null)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR033", "Expected an index in subscript expression.", open.Span);
        }

        List<SqlExpression> bounds = new List<SqlExpression>();
        if (lower != null)
        {
            bounds.Add(lower);
        }

        if (upper != null)
        {
            bounds.Add(upper);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Subscript, span, tokens.ToArray(), bounds.ToArray());
        expression.Operand = left;
        expression.Left = lower;
        expression.Right = upper;
        expression.SubscriptData = new SqlSubscriptExpressionData(left, lower, upper, isSlice);
        return expression;
    }

    internal SqlExpression? TryParsePostgreSqlCastExpression(SqlExpression left, int parentPrecedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        if (Context.Peek().Kind != SqlTokenKind.DoubleColon || 95 <= parentPrecedence)
        {
            return null;
        }

        SqlToken doubleColon = Context.Consume();
        SkipTrivia();
        SqlTextSpan typeSpan = ParsePostgreSqlTypeNameSpan(doubleColon.Span.GetEnd());
        string typeName = GetSourceText(typeSpan);
        if (typeName.Length == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR034", "Expected a type name after PostgreSQL cast operator.", Context.Peek().Span);
        }

        int end = typeSpan.GetEnd();
        if (typeName.Length == 0)
        {
            end = doubleColon.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Cast, span, new SqlToken[] { doubleColon });
        expression.Operand = left;
        expression.TypeName = typeName;
        expression.OperatorText = doubleColon.Text;
        expression.OperatorToken = doubleColon;
        expression.CastData = new SqlCastExpressionData(left, typeName, SqlCastSyntaxKind.PostgreSql);
        return expression;
    }

    internal SqlTextSpan ParsePostgreSqlTypeNameSpan(int fallbackStart)
    {
        if (!IsIdentifierLike(Context.Peek()))
        {
            return new SqlTextSpan(fallbackStart, 0);
        }

        SqlToken first = Context.Consume();
        SqlToken last = first;
        bool done = false;
        while (!done)
        {
            SkipTrivia();
            SqlToken token = Context.Peek();
            if (token.Kind == SqlTokenKind.Dot && IsIdentifierLike(PeekNonTrivia(1)))
            {
                Context.Consume();
                SkipTrivia();
                last = Context.Consume();
            }
            else if (token.Kind == SqlTokenKind.OpenBracket && PeekNonTrivia(1).Kind == SqlTokenKind.CloseBracket)
            {
                Context.Consume();
                SkipTrivia();
                last = Context.Consume();
            }
            else if (token.Kind == SqlTokenKind.OpenParen)
            {
                int depth = 0;
                while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
                {
                    SqlToken part = Context.Consume();
                    last = part;
                    if (part.Kind == SqlTokenKind.OpenParen)
                    {
                        depth = depth + 1;
                    }
                    else if (part.Kind == SqlTokenKind.CloseParen)
                    {
                        depth = depth - 1;
                        if (depth == 0)
                        {
                            break;
                        }
                    }
                }
            }
            else if (IsIdentifierLike(token) && !IsTypeNameTerminatorKeyword(token))
            {
                last = Context.Consume();
            }
            else
            {
                done = true;
            }
        }

        return SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
    }

    internal bool IsTypeNameTerminatorKeyword(SqlToken token)
    {
        return IsKeyword(token, "COLLATE") || IsKeyword(token, "OVER") || IsKeyword(token, "IS") || IsKeyword(token, "NOT") || IsKeyword(token, "BETWEEN") || IsKeyword(token, "IN") || IsKeyword(token, "LIKE") || IsKeyword(token, "GLOB") || IsKeyword(token, "REGEXP") || IsKeyword(token, "MATCH") || IsKeyword(token, "AND") || IsKeyword(token, "OR") || IsKeyword(token, "ESCAPE") || IsKeyword(token, "AS") || IsKeyword(token, "FROM") || IsKeyword(token, "WHERE") || IsKeyword(token, "GROUP") || IsKeyword(token, "HAVING") || IsKeyword(token, "WINDOW") || IsKeyword(token, "ORDER") || IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT");
    }

    internal SqlExpression? TryParseSpecialExpression(SqlExpression left, int parentPrecedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (parentPrecedence < 0)
        {
            throw new ArgumentOutOfRangeException("parentPrecedence");
        }

        SkipTrivia();
        SqlToken token = Context.Peek();
        int precedence = GetSpecialOperatorPrecedence(token);
        if (precedence == 0 && IsPostfixNullOperator(token))
        {
            precedence = 40;
        }

        if (precedence == 0)
        {
            if (IsKeyword(token, "NOT"))
            {
                SqlToken next = PeekNonTrivia(1);
                precedence = GetSpecialOperatorPrecedence(next);
            }
        }

        if (precedence == 0 || precedence <= parentPrecedence)
        {
            return null;
        }

        if (IsKeyword(token, "IS"))
        {
            return ParseIsExpression(left, precedence);
        }

        if (IsKeyword(token, "BETWEEN"))
        {
            return ParseBetweenExpression(left, precedence, false);
        }

        if (IsKeyword(token, "IN"))
        {
            return ParseInExpression(left, precedence, false);
        }

        if (IsPatternMatchOperator(token))
        {
            return ParseLikeExpression(left, precedence, false);
        }

        if (IsPostfixNullOperator(token))
        {
            return ParsePostfixNullExpression(left);
        }

        if (IsKeyword(token, "NOT"))
        {
            SqlToken next = PeekNonTrivia(1);
            if (IsKeyword(next, "BETWEEN"))
            {
                return ParseBetweenExpression(left, precedence, true);
            }

            if (IsKeyword(next, "IN"))
            {
                return ParseInExpression(left, precedence, true);
            }

            if (IsPatternMatchOperator(next))
            {
                return ParseLikeExpression(left, precedence, true);
            }
        }

        return null;
    }

    internal SqlExpression ParsePrefixExpression()
    {
        SkipTrivia();
        SqlToken operatorToken = Context.Peek();
        int precedence = GetUnaryPrecedence(operatorToken);
        if (precedence > 0)
        {
            Context.Consume();
            SqlExpression operand = ParseExpressionWithPrecedence(precedence);
            SqlTextSpan span = SqlTextSpan.FromBounds(operatorToken.Span.Start, operand.Span.GetEnd());
            SqlExpression unary = SqlExpression.Create(SqlExpressionKind.Unary, span, new SqlToken[] { operatorToken });
            unary.Operand = operand;
            unary.OperatorText = operatorToken.Text;
            unary.OperatorToken = operatorToken;
            unary.UnaryOperator = GetUnaryOperatorKind(operatorToken);
            return unary;
        }

        return ParsePrimaryExpression();
    }

    internal SqlExpression ParsePrimaryExpression()
    {
        SkipTrivia();
        SqlToken token = Context.Peek();
        if (token.Kind == SqlTokenKind.EndOfFile)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR001", "Expected expression.", token.Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, token.Span);
        }

        if (IsExpressionTerminator(token.Kind))
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR001", "Expected expression.", token.Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, token.Span);
        }

        if (token.Kind == SqlTokenKind.NumericLiteral || token.Kind == SqlTokenKind.StringLiteral || token.Kind == SqlTokenKind.BlobLiteral || IsLiteralKeyword(token))
        {
            return ParseLiteralExpression();
        }

        if (token.Kind == SqlTokenKind.Parameter)
        {
            return ParseParameterExpression();
        }

        if (token.Kind == SqlTokenKind.OpenParen)
        {
            return ParseParenthesizedOrRowValueExpression();
        }

        if (IsKeyword(token, "CASE"))
        {
            return ParseCaseExpression();
        }

        if (IsKeyword(token, "CAST"))
        {
            return ParseCastExpression();
        }

        if (IsKeyword(token, "EXISTS"))
        {
            return ParseExistsExpression();
        }

        if (IsKeyword(token, "EXTRACT") && PeekNonTrivia(1).Kind == SqlTokenKind.OpenParen)
        {
            return ParseExtractExpression();
        }

        if (IsKeyword(token, "ARRAY") && IsArrayConstructorStart())
        {
            return ParseArrayExpression();
        }

        if (token.Kind == SqlTokenKind.Star)
        {
            return ParseWildcardExpression();
        }

        if (IsIdentifierLike(token))
        {
            return ParseNameOrQualifiedWildcardExpression();
        }

        token = Context.Consume();
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Unknown, token.Span, new SqlToken[] { token });
        expression.Value = token.Text;
        return expression;
    }

    internal int GetUnaryPrecedence(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        SqlUnaryOperatorKind operatorKind = GetUnaryOperatorKind(token);
        if (operatorKind == SqlUnaryOperatorKind.Not)
        {
            return 30;
        }

        if (operatorKind != SqlUnaryOperatorKind.None)
        {
            return 90;
        }

        return 0;
    }

    internal int GetBinaryPrecedence(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.Kind == SqlTokenKind.CustomOperator)
        {
            return Context.Options.Dialect.GetCustomOperatorPrecedence(token.Text);
        }

        SqlBinaryOperatorKind operatorKind = GetBinaryOperatorKind(token);
        if (operatorKind == SqlBinaryOperatorKind.Concat)
        {
            return 80;
        }

        if (operatorKind == SqlBinaryOperatorKind.Multiply || operatorKind == SqlBinaryOperatorKind.Divide || operatorKind == SqlBinaryOperatorKind.Modulo)
        {
            return 70;
        }

        if (operatorKind == SqlBinaryOperatorKind.Add || operatorKind == SqlBinaryOperatorKind.Subtract)
        {
            return 60;
        }

        if (operatorKind == SqlBinaryOperatorKind.BitwiseAnd || operatorKind == SqlBinaryOperatorKind.BitwiseOr || operatorKind == SqlBinaryOperatorKind.BitwiseXor || operatorKind == SqlBinaryOperatorKind.ShiftLeft || operatorKind == SqlBinaryOperatorKind.ShiftRight)
        {
            return 50;
        }

        if (operatorKind == SqlBinaryOperatorKind.JsonExtract || operatorKind == SqlBinaryOperatorKind.JsonExtractText)
        {
            return 50;
        }

        if (operatorKind == SqlBinaryOperatorKind.Equal || operatorKind == SqlBinaryOperatorKind.NotEqual || operatorKind == SqlBinaryOperatorKind.Less || operatorKind == SqlBinaryOperatorKind.LessOrEqual || operatorKind == SqlBinaryOperatorKind.Greater || operatorKind == SqlBinaryOperatorKind.GreaterOrEqual)
        {
            return 40;
        }

        if (operatorKind == SqlBinaryOperatorKind.And)
        {
            return 20;
        }

        if (operatorKind == SqlBinaryOperatorKind.Or)
        {
            return 10;
        }

        return 0;
    }

    internal SqlUnaryOperatorKind GetUnaryOperatorKind(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.Kind == SqlTokenKind.Plus)
        {
            return SqlUnaryOperatorKind.Positive;
        }

        if (token.Kind == SqlTokenKind.Minus)
        {
            return SqlUnaryOperatorKind.Negative;
        }

        if (token.Kind == SqlTokenKind.Tilde)
        {
            return SqlUnaryOperatorKind.BitwiseNot;
        }

        if (IsKeyword(token, "NOT"))
        {
            return SqlUnaryOperatorKind.Not;
        }

        return SqlUnaryOperatorKind.None;
    }

    internal SqlBinaryOperatorKind GetBinaryOperatorKind(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.Kind == SqlTokenKind.Plus)
        {
            return SqlBinaryOperatorKind.Add;
        }

        if (token.Kind == SqlTokenKind.Minus)
        {
            return SqlBinaryOperatorKind.Subtract;
        }

        if (token.Kind == SqlTokenKind.Star)
        {
            return SqlBinaryOperatorKind.Multiply;
        }

        if (token.Kind == SqlTokenKind.Slash)
        {
            return SqlBinaryOperatorKind.Divide;
        }

        if (token.Kind == SqlTokenKind.Percent)
        {
            return SqlBinaryOperatorKind.Modulo;
        }

        if (token.Kind == SqlTokenKind.DoublePipe)
        {
            return SqlBinaryOperatorKind.Concat;
        }

        if (token.Kind == SqlTokenKind.Equal || token.Kind == SqlTokenKind.DoubleEqual)
        {
            return SqlBinaryOperatorKind.Equal;
        }

        if (token.Kind == SqlTokenKind.BangEqual || token.Kind == SqlTokenKind.LessGreater)
        {
            return SqlBinaryOperatorKind.NotEqual;
        }

        if (token.Kind == SqlTokenKind.Less)
        {
            return SqlBinaryOperatorKind.Less;
        }

        if (token.Kind == SqlTokenKind.LessEqual)
        {
            return SqlBinaryOperatorKind.LessOrEqual;
        }

        if (token.Kind == SqlTokenKind.Greater)
        {
            return SqlBinaryOperatorKind.Greater;
        }

        if (token.Kind == SqlTokenKind.GreaterEqual)
        {
            return SqlBinaryOperatorKind.GreaterOrEqual;
        }

        if (token.Kind == SqlTokenKind.Ampersand)
        {
            return SqlBinaryOperatorKind.BitwiseAnd;
        }

        if (token.Kind == SqlTokenKind.Pipe)
        {
            return SqlBinaryOperatorKind.BitwiseOr;
        }

        if (token.Kind == SqlTokenKind.Caret)
        {
            return SqlBinaryOperatorKind.BitwiseXor;
        }

        if (token.Kind == SqlTokenKind.ShiftLeft)
        {
            return SqlBinaryOperatorKind.ShiftLeft;
        }

        if (token.Kind == SqlTokenKind.ShiftRight)
        {
            return SqlBinaryOperatorKind.ShiftRight;
        }

        if (token.Kind == SqlTokenKind.Arrow)
        {
            return SqlBinaryOperatorKind.JsonExtract;
        }

        if (token.Kind == SqlTokenKind.DoubleArrow)
        {
            return SqlBinaryOperatorKind.JsonExtractText;
        }

        if (IsKeyword(token, "AND"))
        {
            return SqlBinaryOperatorKind.And;
        }

        if (IsKeyword(token, "OR"))
        {
            return SqlBinaryOperatorKind.Or;
        }

        return SqlBinaryOperatorKind.None;
    }

    internal int GetSpecialOperatorPrecedence(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (IsKeyword(token, "IS") || IsKeyword(token, "BETWEEN") || IsKeyword(token, "IN") || IsPatternMatchOperator(token))
        {
            return 40;
        }

        return 0;
    }

    internal bool IsPatternMatchOperator(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        return IsKeyword(token, "LIKE") || IsKeyword(token, "GLOB") || IsKeyword(token, "REGEXP") || IsKeyword(token, "MATCH");
    }

    internal bool IsPostfixNullOperator(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (IsKeyword(token, "ISNULL") || IsKeyword(token, "NOTNULL"))
        {
            return true;
        }

        return IsKeyword(token, "NOT") && IsKeyword(PeekNonTrivia(1), "NULL");
    }

    internal SqlExpression ParsePostfixNullExpression(SqlExpression left)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        SqlToken operatorToken = Context.Consume();
        List<SqlToken> tokens = new List<SqlToken>();
        tokens.Add(operatorToken);
        SqlExpression right;
        if (IsKeyword(operatorToken, "NOT"))
        {
            SkipTrivia();
            SqlToken nullToken = Context.Consume();
            tokens.Add(nullToken);
            SqlLiteral literal = CreateLiteral(nullToken);
            right = SqlExpression.Create(SqlExpressionKind.Literal, nullToken.Span, new SqlToken[] { nullToken });
            right.Literal = literal;
        }
        else
        {
            int nullStart = operatorToken.Span.Start + 2;
            if (IsKeyword(operatorToken, "NOTNULL"))
            {
                nullStart = operatorToken.Span.Start + 3;
            }

            SqlTextSpan nullSpan = new SqlTextSpan(nullStart, 4);
            string nullText = GetSourceText(nullSpan);
            right = SqlExpression.Create(SqlExpressionKind.Literal, nullSpan);
            right.Literal = SqlLiteral.Create(SqlLiteralKind.Null, nullText, nullSpan);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, right.Span.GetEnd());
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Is, span, tokens.ToArray());
        expression.Left = left;
        expression.Right = right;
        expression.OperatorText = GetOperatorText(tokens);
        expression.OperatorToken = operatorToken;
        return expression;
    }

    internal bool IsJsonOperatorText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return text == "->" || text == "->>" || text == "#>" || text == "#>>" || text == "@>" || text == "<@" || text == "?" || text == "?|" || text == "?&" || text == "#-";
    }

    internal SqlExpression ParseIsExpression(SqlExpression left, int precedence)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        List<SqlToken> tokens = new List<SqlToken>();
        SqlToken isToken = Context.Consume();
        tokens.Add(isToken);
        SkipTrivia();
        bool negated = false;
        if (IsKeyword(Context.Peek(), "NOT"))
        {
            tokens.Add(Context.Consume());
            negated = true;
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "DISTINCT"))
        {
            tokens.Add(Context.Consume());
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "FROM"))
            {
                tokens.Add(Context.Consume());
                SkipTrivia();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR031", "Expected FROM in distinctness predicate.", Context.Peek().Span);
            }

            SqlExpression distinctRight = ParseExpressionWithPrecedence(precedence);
            SqlTextSpan distinctSpan = SqlTextSpan.FromBounds(left.Span.Start, distinctRight.Span.GetEnd());
            SqlExpression distinct = SqlExpression.Create(SqlExpressionKind.Distinctness, distinctSpan, tokens.ToArray());
            distinct.Left = left;
            distinct.Right = distinctRight;
            distinct.OperatorText = GetOperatorText(tokens);
            distinct.OperatorToken = isToken;
            SqlDistinctnessKind distinctnessKind = SqlDistinctnessKind.IsDistinctFrom;
            if (negated)
            {
                distinctnessKind = SqlDistinctnessKind.IsNotDistinctFrom;
            }

            distinct.DistinctnessData = new SqlDistinctnessExpressionData(left, distinctRight, distinctnessKind);
            return distinct;
        }

        SqlExpression right = ParseExpressionWithPrecedence(precedence);
        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, right.Span.GetEnd());
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Is, span, tokens.ToArray());
        expression.Left = left;
        expression.Right = right;
        expression.OperatorText = GetOperatorText(tokens);
        expression.OperatorToken = isToken;
        return expression;
    }

    internal SqlExpression ParseLikeExpression(SqlExpression left, int precedence, bool negated)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        List<SqlToken> tokens = new List<SqlToken>();
        if (negated)
        {
            tokens.Add(Context.Consume());
            SkipTrivia();
        }

        SqlToken operatorToken = Context.Consume();
        tokens.Add(operatorToken);
        SqlExpression right = ParseExpressionWithPrecedence(precedence);
        SqlExpression? escape = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ESCAPE"))
        {
            tokens.Add(Context.Consume());
            escape = ParseExpressionWithPrecedence(precedence);
        }

        int end = right.Span.GetEnd();
        if (escape != null)
        {
            end = escape.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Like, span, tokens.ToArray());
        expression.Left = left;
        expression.Right = right;
        expression.Alternate = escape;
        expression.OperatorText = GetOperatorText(tokens);
        expression.OperatorToken = operatorToken;
        expression.PatternData = new SqlPatternExpressionData(left, right, escape, operatorToken.Text, negated);
        return expression;
    }

    internal SqlExpression ParseBetweenExpression(SqlExpression left, int precedence, bool negated)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        List<SqlToken> tokens = new List<SqlToken>();
        if (negated)
        {
            tokens.Add(Context.Consume());
            SkipTrivia();
        }

        SqlToken betweenToken = Context.Consume();
        tokens.Add(betweenToken);
        SqlExpression lower = ParseExpressionWithPrecedence(precedence);
        SqlExpression upper = SqlExpression.Create(SqlExpressionKind.Unknown, lower.Span);
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AND"))
        {
            tokens.Add(Context.Consume());
            upper = ParseExpressionWithPrecedence(precedence);
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR003", "Expected AND in BETWEEN expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, upper.Span.GetEnd());
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Between, span, tokens.ToArray());
        expression.Operand = left;
        expression.Left = lower;
        expression.Right = upper;
        expression.OperatorText = GetOperatorText(tokens);
        expression.OperatorToken = betweenToken;
        return expression;
    }

    internal SqlExpression ParseInExpression(SqlExpression left, int precedence, bool negated)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        List<SqlToken> tokens = new List<SqlToken>();
        if (negated)
        {
            tokens.Add(Context.Consume());
            SkipTrivia();
        }

        SqlToken inToken = Context.Consume();
        tokens.Add(inToken);
        SkipTrivia();
        SqlExpression right = SqlExpression.Create(SqlExpressionKind.Unknown, inToken.Span);
        SqlExpression[] arguments = new SqlExpression[0];
        SqlQuery? targetQuery = null;
        SqlName? targetName = null;
        SqlExpression[] functionArguments = new SqlExpression[0];
        SqlInTargetKind targetKind = SqlInTargetKind.None;
        int end = inToken.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            SqlToken open = Context.Consume();
            end = open.Span.GetEnd();
            SkipTrivia();
            SqlQuery? query = null;
            if (IsSubqueryStart(Context.Peek()))
            {
                SqlExpression subquery = ParseSubqueryExpressionAfterOpen(open);
                query = subquery.Query;
                targetQuery = query;
                targetKind = SqlInTargetKind.Subquery;
                end = subquery.Span.GetEnd();
            }
            else
            {
                List<SqlExpression> items = new List<SqlExpression>();
                SkipTrivia();
                while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
                {
                    SqlExpression item = ParseExpressionWithPrecedence(0);
                    items.Add(item);
                    end = item.Span.GetEnd();
                    SkipTrivia();
                    if (Context.Peek().Kind == SqlTokenKind.Comma)
                    {
                        SqlToken comma = Context.Consume();
                        end = comma.Span.GetEnd();
                        SkipTrivia();
                        continue;
                    }

                    break;
                }

                if (Context.Peek().Kind == SqlTokenKind.CloseParen)
                {
                    SqlToken close = Context.Consume();
                    end = close.Span.GetEnd();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR004", "Expected closing parenthesis in IN expression.", Context.Peek().Span);
                }

                arguments = items.ToArray();
                targetKind = SqlInTargetKind.List;
            }

            right.Query = query;
        }
        else if (IsIdentifierLike(Context.Peek()))
        {
            right = ParseNameOrQualifiedWildcardExpression();
            end = right.Span.GetEnd();
            targetName = right.Name;
            if (right.Kind == SqlExpressionKind.FunctionCall)
            {
                targetKind = SqlInTargetKind.Function;
                functionArguments = right.Arguments;
            }
            else
            {
                targetKind = SqlInTargetKind.Table;
            }
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR005", "Expected parenthesized list in IN expression.", Context.Peek().Span);
            if (!IsExpressionTerminator(Context.Peek().Kind))
            {
                right = ParseExpressionWithPrecedence(precedence);
                end = right.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(left.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.In, span, tokens.ToArray(), arguments);
        expression.Operand = left;
        expression.Right = right;
        expression.Query = right.Query;
        expression.OperatorText = GetOperatorText(tokens);
        expression.OperatorToken = inToken;
        expression.InData = new SqlInExpressionData(left, negated, targetKind, arguments, targetQuery, targetName, functionArguments);
        return expression;
    }

    internal bool IsExpressionTerminator(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.EndOfFile || kind == SqlTokenKind.Semicolon || kind == SqlTokenKind.Comma || kind == SqlTokenKind.CloseParen || kind == SqlTokenKind.CloseBracket;
    }

    internal SqlExpression ParseLiteralExpression()
    {
        SqlToken token = Context.Consume();
        SqlLiteral literal = CreateLiteral(token);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Literal, token.Span, new SqlToken[] { token });
        expression.Literal = literal;
        return expression;
    }

    internal SqlExpression ParseParameterExpression()
    {
        SqlToken token = Context.Consume();
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Parameter, token.Span, new SqlToken[] { token });
        expression.Value = token.Text;
        return expression;
    }

    internal SqlExpression ParseWildcardExpression()
    {
        SqlToken token = Context.Consume();
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Wildcard, token.Span, new SqlToken[] { token });
        expression.WildcardData = new SqlWildcardExpressionData(null);
        return expression;
    }

    internal SqlExpression ParseParenthesizedOrRowValueExpression()
    {
        SqlToken open = Context.Consume();
        SkipTrivia();
        if (IsSubqueryStart(Context.Peek()))
        {
            return ParseSubqueryExpressionAfterOpen(open);
        }

        List<SqlExpression> arguments = new List<SqlExpression>();
        bool sawComma = false;
        int end = open.Span.GetEnd();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlExpression argument = ParseExpressionWithPrecedence(0);
            arguments.Add(argument);
            end = argument.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                SqlToken comma = Context.Consume();
                sawComma = true;
                end = comma.Span.GetEnd();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR013", "Expected closing parenthesis in expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(open.Span.Start, end);
        if (arguments.Count == 1 && !sawComma)
        {
            SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Parenthesized, span, new SqlToken[] { open });
            expression.Operand = arguments[0];
            return expression;
        }

        return SqlExpression.Create(SqlExpressionKind.RowValue, span, new SqlToken[] { open }, arguments.ToArray());
    }

    internal bool IsArrayConstructorStart()
    {
        SqlToken delimiter = PeekNonTrivia(1);
        if (delimiter.Kind == SqlTokenKind.OpenBracket)
        {
            return true;
        }

        if (delimiter.Kind == SqlTokenKind.OpenParen)
        {
            return IsSubqueryStart(PeekNonTrivia(2));
        }

        return false;
    }

    internal SqlExpression ParseArrayExpression()
    {
        SqlToken arrayToken = Context.Consume();
        List<SqlToken> tokens = new List<SqlToken>();
        tokens.Add(arrayToken);
        SkipTrivia();
        SqlToken open = Context.Consume();
        tokens.Add(open);
        int end = open.Span.GetEnd();
        SqlQuery? query = null;
        List<SqlExpression> elements = new List<SqlExpression>();
        if (open.Kind == SqlTokenKind.OpenParen)
        {
            SqlExpression subquery = ParseSubqueryExpressionAfterOpen(open);
            query = subquery.Query;
            end = subquery.Span.GetEnd();
        }
        else
        {
            SkipTrivia();
            while (Context.Peek().Kind != SqlTokenKind.CloseBracket && Context.Peek().Kind != SqlTokenKind.EndOfFile)
            {
                SqlExpression element = ParseExpressionWithPrecedence(0);
                elements.Add(element);
                end = element.Span.GetEnd();
                SkipTrivia();
                if (Context.Peek().Kind == SqlTokenKind.Comma)
                {
                    tokens.Add(Context.Consume());
                    SkipTrivia();
                    continue;
                }

                break;
            }

            if (Context.Peek().Kind == SqlTokenKind.CloseBracket)
            {
                SqlToken close = Context.Consume();
                tokens.Add(close);
                end = close.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR035", "Expected closing bracket in array constructor.", Context.Peek().Span);
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(arrayToken.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Array, span, tokens.ToArray(), elements.ToArray());
        expression.Query = query;
        expression.ArrayData = new SqlArrayExpressionData(expression.Arguments, query);
        return expression;
    }

    internal SqlExpression ParseExtractExpression()
    {
        SqlToken extractToken = Context.Consume();
        List<SqlToken> tokens = new List<SqlToken>();
        tokens.Add(extractToken);
        SkipTrivia();
        SqlToken open = Context.Consume();
        tokens.Add(open);
        SkipTrivia();
        SqlToken fieldFirst = Context.Peek();
        SqlToken fieldLast = fieldFirst;
        bool hasField = false;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.CloseParen && !IsKeyword(Context.Peek(), "FROM"))
        {
            SqlToken fieldToken = Context.Consume();
            if (!IsTrivia(fieldToken.Kind))
            {
                if (!hasField)
                {
                    fieldFirst = fieldToken;
                }

                fieldLast = fieldToken;
                hasField = true;
            }
        }

        string field = "";
        if (hasField)
        {
            field = GetSourceText(SqlTextSpan.FromBounds(fieldFirst.Span.Start, fieldLast.Span.GetEnd()));
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR036", "Expected a field in EXTRACT expression.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "FROM"))
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR037", "Expected FROM in EXTRACT expression.", Context.Peek().Span);
        }

        SqlExpression source = ParseExpressionWithPrecedence(0);
        int end = source.Span.GetEnd();
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            tokens.Add(close);
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR038", "Expected closing parenthesis in EXTRACT expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(extractToken.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Extract, span, tokens.ToArray());
        expression.Operand = source;
        expression.Value = field;
        expression.ExtractData = new SqlExtractExpressionData(field, source);
        return expression;
    }

    internal SqlExpression ParseCastExpression()
    {
        SqlToken castToken = Context.Consume();
        List<SqlToken> tokens = new List<SqlToken>();
        tokens.Add(castToken);
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR014", "Expected opening parenthesis after CAST.", Context.Peek().Span);
        }

        SqlExpression operand = ParseExpressionWithPrecedence(0);
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR015", "Expected AS in CAST expression.", Context.Peek().Span);
        }

        SkipTrivia();
        SqlTextSpan typeSpan = ParseTypeNameSpan();
        string typeName = GetSourceText(typeSpan);
        int end = typeSpan.GetEnd();
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            tokens.Add(close);
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR016", "Expected closing parenthesis in CAST expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(castToken.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Cast, span, tokens.ToArray());
        expression.Operand = operand;
        expression.TypeName = typeName;
        expression.CastData = new SqlCastExpressionData(operand, typeName, SqlCastSyntaxKind.CastFunction);
        return expression;
    }

    internal SqlExpression ParseCaseExpression()
    {
        SqlToken caseToken = Context.Consume();
        List<SqlToken> tokens = new List<SqlToken>();
        List<SqlExpression> arguments = new List<SqlExpression>();
        tokens.Add(caseToken);
        SqlExpression? operand = null;
        SqlExpression? alternate = null;
        int end = caseToken.Span.GetEnd();
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "WHEN") && !IsKeyword(Context.Peek(), "END") && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            operand = ParseExpressionWithPrecedence(0);
            end = operand.Span.GetEnd();
            SkipTrivia();
        }

        while (IsKeyword(Context.Peek(), "WHEN"))
        {
            tokens.Add(Context.Consume());
            SqlExpression condition = ParseExpressionWithPrecedence(0);
            arguments.Add(condition);
            end = condition.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "THEN"))
            {
                tokens.Add(Context.Consume());
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR017", "Expected THEN in CASE expression.", Context.Peek().Span);
            }

            SqlExpression result = ParseExpressionWithPrecedence(0);
            arguments.Add(result);
            end = result.Span.GetEnd();
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "ELSE"))
        {
            tokens.Add(Context.Consume());
            alternate = ParseExpressionWithPrecedence(0);
            end = alternate.Span.GetEnd();
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "END"))
        {
            SqlToken endToken = Context.Consume();
            tokens.Add(endToken);
            end = endToken.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR018", "Expected END in CASE expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(caseToken.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Case, span, tokens.ToArray(), arguments.ToArray());
        expression.Operand = operand;
        expression.Alternate = alternate;
        return expression;
    }

    internal SqlExpression ParseExistsExpression()
    {
        SqlToken existsToken = Context.Consume();
        SkipTrivia();
        int end = existsToken.Span.GetEnd();
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], existsToken.Span);
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            SqlToken open = Context.Consume();
            SkipTrivia();
            if (!IsSubqueryStart(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR019", "Expected subquery in EXISTS expression.", Context.Peek().Span);
            }

            SqlExpression subquery = ParseSubqueryExpressionAfterOpen(open);
            if (subquery.Query != null)
            {
                query = subquery.Query;
            }

            end = subquery.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR020", "Expected subquery after EXISTS.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(existsToken.Span.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Exists, span, new SqlToken[] { existsToken });
        expression.Query = query;
        return expression;
    }

    internal SqlExpression ParseSubqueryExpressionAfterOpen(SqlToken open)
    {
        if (open == null)
        {
            throw new ArgumentNullException("open");
        }

        SkipTrivia();
        SqlToken first = Context.Peek();
        SqlToken last = first;
        List<SqlToken> queryTokens = new List<SqlToken>();
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (Context.Peek().Kind == SqlTokenKind.CloseParen && depth == 0)
            {
                break;
            }

            SqlToken token = Context.Consume();
            queryTokens.Add(token);
            last = token;
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        int queryEnd = last.Span.GetEnd();
        int expressionEnd = queryEnd;
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            expressionEnd = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR021", "Expected closing parenthesis in subquery expression.", Context.Peek().Span);
        }

        SqlTextSpan querySpan = SqlTextSpan.FromBounds(first.Span.Start, queryEnd);
        SqlTextSpan span = SqlTextSpan.FromBounds(open.Span.Start, expressionEnd);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Subquery, span, new SqlToken[] { open });
        expression.Query = ParseSubqueryTokens(queryTokens, querySpan);
        return expression;
    }

    internal SqlQuery ParseSubqueryTokens(List<SqlToken> queryTokens, SqlTextSpan querySpan)
    {
        if (queryTokens.Count == 0)
        {
            return SqlQuery.Create(new SqlSelectCore[0], querySpan);
        }

        SqlToken[] tokens = AddEndOfFileToken(queryTokens, querySpan.GetEnd());
        SqlTokenStream stream = SqlTokenStream.Create(tokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlQueryParser parser = new SqlQueryParser(context);
        return parser.ParseQuery();
    }

    internal SqlToken[] AddEndOfFileToken(List<SqlToken> tokens, int end)
    {
        int count = tokens.Count;
        SqlToken[] result = new SqlToken[count + 1];
        int index = 0;
        while (index < count)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        result[count] = new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(end, 0), "");
        return result;
    }

    internal SqlExpression ParseNameOrQualifiedWildcardExpression()
    {
        List<SqlToken> tokens = new List<SqlToken>();
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SqlToken first = ConsumeNamePart(tokens, parts);
        SqlToken last = first;

        while (true)
        {
            SkipTrivia();
            if (Context.Peek().Kind != SqlTokenKind.Dot)
            {
                break;
            }

            SqlToken dot = Context.Consume();
            tokens.Add(dot);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Star)
            {
                SqlToken star = Context.Consume();
                tokens.Add(star);
                last = star;
                SqlTextSpan wildcardSpan = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
                SqlExpression wildcard = SqlExpression.Create(SqlExpressionKind.Wildcard, wildcardSpan, tokens.ToArray());
                wildcard.Name = CreateName(parts);
                wildcard.WildcardData = new SqlWildcardExpressionData(wildcard.Name);
                return wildcard;
            }

            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR002", "Expected identifier after dot.", Context.Peek().Span);
                break;
            }

            last = ConsumeNamePart(tokens, parts);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        SqlName name = new SqlName(parts.ToArray(), span);
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            return ParseFunctionCallExpression(name, tokens, span);
        }

        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, span, tokens.ToArray());
        expression.Name = name;
        return expression;
    }

    internal SqlExpression ParseFunctionCallExpression(SqlName name, List<SqlToken> tokens, SqlTextSpan nameSpan)
    {
        SqlToken open = Context.Consume();
        tokens.Add(open);
        List<SqlExpression> arguments = new List<SqlExpression>();
        bool isDistinct = false;
        int end = open.Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DISTINCT"))
        {
            tokens.Add(Context.Consume());
            isDistinct = true;
            SkipTrivia();
        }

        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (IsKeyword(Context.Peek(), "ORDER"))
            {
                break;
            }

            SqlExpression argument = ParseExpressionWithPrecedence(0);
            arguments.Add(argument);
            end = argument.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                SqlToken comma = Context.Consume();
                tokens.Add(comma);
                end = comma.Span.GetEnd();
                SkipTrivia();
                continue;
            }

            break;
        }

        SqlOrderingTerm[] orderBy = new SqlOrderingTerm[0];
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ORDER"))
        {
            orderBy = ParseFunctionOrderBy(tokens);
            if (orderBy.Length > 0)
            {
                end = orderBy[orderBy.Length - 1].Span.GetEnd();
            }
        }

        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            tokens.Add(close);
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR006", "Expected closing parenthesis in function call.", Context.Peek().Span);
        }

        SqlExpression? filter = ParseOptionalFilter(tokens);
        if (filter != null)
        {
            end = filter.Span.GetEnd();
            if (tokens.Count > 0 && tokens[tokens.Count - 1].Span.GetEnd() > end)
            {
                end = tokens[tokens.Count - 1].Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(nameSpan.Start, end);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.FunctionCall, span, tokens.ToArray(), arguments.ToArray());
        expression.Name = name;
        expression.IsDistinct = isDistinct;
        expression.OrderBy = orderBy;
        expression.Filter = filter;
        expression.FunctionData = new SqlFunctionInvocationData(name, expression.Arguments, isDistinct, orderBy, filter, null, null);
        return expression;
    }

    internal SqlOrderingTerm[] ParseFunctionOrderBy(List<SqlToken> tokens)
    {
        SqlToken order = Context.Consume();
        tokens.Add(order);
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BY"))
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR007", "Expected BY after ORDER in function call.", Context.Peek().Span);
        }

        List<SqlOrderingTerm> terms = new List<SqlOrderingTerm>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlOrderingTerm term = ParseOrderingTerm();
            terms.Add(term);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                SqlToken comma = Context.Consume();
                tokens.Add(comma);
                SkipTrivia();
                continue;
            }

            break;
        }

        return terms.ToArray();
    }

    internal SqlOrderingTerm ParseOrderingTerm()
    {
        SqlExpression expression = ParseExpressionWithPrecedence(0);
        SqlName? collation = null;
        SqlOrderingDirection direction = SqlOrderingDirection.None;
        SqlNullOrdering nullOrdering = SqlNullOrdering.None;
        int end = expression.Span.GetEnd();
        if (expression.Kind == SqlExpressionKind.Collate)
        {
            collation = expression.Name;
            if (expression.Operand != null)
            {
                expression = expression.Operand;
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "COLLATE"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsIdentifierLike(Context.Peek()))
            {
                SqlToken collationToken = Context.Consume();
                collation = CreateSinglePartName(collationToken);
                end = collation.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR008", "Expected collation name.", Context.Peek().Span);
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ASC"))
        {
            SqlToken token = Context.Consume();
            direction = SqlOrderingDirection.Asc;
            end = token.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "DESC"))
            {
                SqlToken token = Context.Consume();
                direction = SqlOrderingDirection.Desc;
                end = token.Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "NULLS"))
        {
            SqlToken nulls = Context.Consume();
            end = nulls.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "FIRST"))
            {
                SqlToken first = Context.Consume();
                nullOrdering = SqlNullOrdering.First;
                end = first.Span.GetEnd();
            }
            else
            {
                if (IsKeyword(Context.Peek(), "LAST"))
                {
                    SqlToken last = Context.Consume();
                    nullOrdering = SqlNullOrdering.Last;
                    end = last.Span.GetEnd();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR009", "Expected FIRST or LAST after NULLS.", Context.Peek().Span);
                }
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(expression.Span.Start, end);
        return new SqlOrderingTerm(expression, collation, direction, nullOrdering, span);
    }

    internal SqlExpression? ParseOptionalFilter(List<SqlToken> tokens)
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "FILTER"))
        {
            return null;
        }

        tokens.Add(Context.Consume());
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR010", "Expected opening parenthesis after FILTER.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WHERE"))
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR011", "Expected WHERE in FILTER clause.", Context.Peek().Span);
        }

        SqlExpression filter = ParseExpressionWithPrecedence(0);
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            tokens.Add(Context.Consume());
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR012", "Expected closing parenthesis in FILTER clause.", Context.Peek().Span);
        }

        return filter;
    }

    internal SqlWindowDefinition ParseOverWindowDefinition(SqlToken over)
    {
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            return ParseInlineWindowDefinition();
        }

        if (IsIdentifierLike(Context.Peek()))
        {
            SqlToken nameToken = Context.Consume();
            SqlName name = CreateSinglePartName(nameToken);
            return new SqlWindowDefinition(null, name, new SqlExpression[0], new SqlOrderingTerm[0], null, name.Span);
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR022", "Expected window name or definition after OVER.", Context.Peek().Span);
        return new SqlWindowDefinition(null, null, new SqlExpression[0], new SqlOrderingTerm[0], null, over.Span);
    }

    internal SqlWindowDefinition ParseInlineWindowDefinition()
    {
        SqlToken open = Context.Consume();
        int end = open.Span.GetEnd();
        SqlName? baseWindowName = null;
        SqlExpression[] partitionBy = new SqlExpression[0];
        SqlOrderingTerm[] orderBy = new SqlOrderingTerm[0];
        SqlWindowFrame? frame = null;
        SkipTrivia();
        if (IsIdentifierLike(Context.Peek()) && !IsWindowClauseKeyword(Context.Peek()))
        {
            SqlToken nameToken = Context.Consume();
            baseWindowName = CreateSinglePartName(nameToken);
            end = nameToken.Span.GetEnd();
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "PARTITION"))
        {
            partitionBy = ParseWindowPartitionBy();
            if (partitionBy.Length > 0)
            {
                end = partitionBy[partitionBy.Length - 1].Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ORDER"))
        {
            orderBy = ParseFunctionOrderBy(new List<SqlToken>());
            if (orderBy.Length > 0)
            {
                end = orderBy[orderBy.Length - 1].Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsWindowFrameStart(Context.Peek()))
        {
            frame = ParseWindowFrame();
            end = frame.Span.GetEnd();
        }

        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR023", "Expected closing parenthesis in window definition.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(open.Span.Start, end);
        return new SqlWindowDefinition(null, baseWindowName, partitionBy, orderBy, frame, span);
    }

    internal SqlExpression[] ParseWindowPartitionBy()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BY"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR024", "Expected BY after PARTITION in window definition.", Context.Peek().Span);
        }

        List<SqlExpression> expressions = new List<SqlExpression>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (IsKeyword(Context.Peek(), "ORDER") || IsWindowFrameStart(Context.Peek()))
            {
                break;
            }

            SqlExpression expression = ParseExpressionWithPrecedence(0);
            expressions.Add(expression);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        return expressions.ToArray();
    }

    internal SqlWindowFrame ParseWindowFrame()
    {
        SqlToken frameToken = Context.Consume();
        SqlWindowFrameKind kind = GetWindowFrameKind(frameToken);
        SqlWindowFrameBoundary start;
        SqlWindowFrameBoundary? endBoundary = null;
        int end = frameToken.Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BETWEEN"))
        {
            Context.Consume();
            start = ParseWindowFrameBoundary();
            end = start.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "AND"))
            {
                Context.Consume();
                endBoundary = ParseWindowFrameBoundary();
                end = endBoundary.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR025", "Expected AND in window frame.", Context.Peek().Span);
            }
        }
        else
        {
            start = ParseWindowFrameBoundary();
            end = start.Span.GetEnd();
        }

        SqlWindowFrameExclusionKind exclusion = SqlWindowFrameExclusionKind.None;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXCLUDE"))
        {
            SqlToken exclude = Context.Consume();
            end = exclude.Span.GetEnd();
            SkipTrivia();
            exclusion = ParseWindowFrameExclusion();
            if (exclusion != SqlWindowFrameExclusionKind.None)
            {
                end = Context.PeekAtOffset(-1).Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(frameToken.Span.Start, end);
        return new SqlWindowFrame(kind, start, endBoundary, exclusion, span);
    }

    internal SqlWindowFrameBoundary ParseWindowFrameBoundary()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(first, "UNBOUNDED"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "PRECEDING"))
            {
                SqlToken preceding = Context.Consume();
                SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, preceding.Span.GetEnd());
                return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.UnboundedPreceding, null, span);
            }

            if (IsKeyword(Context.Peek(), "FOLLOWING"))
            {
                SqlToken following = Context.Consume();
                SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, following.Span.GetEnd());
                return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.UnboundedFollowing, null, span);
            }

            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR026", "Expected PRECEDING or FOLLOWING after UNBOUNDED.", Context.Peek().Span);
            return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.None, null, first.Span);
        }

        if (IsKeyword(first, "CURRENT"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ROW"))
            {
                SqlToken row = Context.Consume();
                SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, row.Span.GetEnd());
                return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, span);
            }

            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR027", "Expected ROW after CURRENT.", Context.Peek().Span);
            return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.CurrentRow, null, first.Span);
        }

        SqlExpression expression = ParseExpressionWithPrecedence(0);
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "PRECEDING"))
        {
            SqlToken preceding = Context.Consume();
            SqlTextSpan span = SqlTextSpan.FromBounds(expression.Span.Start, preceding.Span.GetEnd());
            return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.Preceding, expression, span);
        }

        if (IsKeyword(Context.Peek(), "FOLLOWING"))
        {
            SqlToken following = Context.Consume();
            SqlTextSpan span = SqlTextSpan.FromBounds(expression.Span.Start, following.Span.GetEnd());
            return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.Following, expression, span);
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR028", "Expected PRECEDING or FOLLOWING in window frame boundary.", Context.Peek().Span);
        return new SqlWindowFrameBoundary(SqlWindowFrameBoundaryKind.None, expression, expression.Span);
    }

    internal SqlWindowFrameExclusionKind ParseWindowFrameExclusion()
    {
        if (IsKeyword(Context.Peek(), "CURRENT"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ROW"))
            {
                Context.Consume();
                return SqlWindowFrameExclusionKind.CurrentRow;
            }
        }

        if (IsKeyword(Context.Peek(), "GROUP"))
        {
            Context.Consume();
            return SqlWindowFrameExclusionKind.Group;
        }

        if (IsKeyword(Context.Peek(), "TIES"))
        {
            Context.Consume();
            return SqlWindowFrameExclusionKind.Ties;
        }

        if (IsKeyword(Context.Peek(), "NO"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "OTHERS"))
            {
                Context.Consume();
                return SqlWindowFrameExclusionKind.NoOthers;
            }
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR029", "Expected window frame exclusion.", Context.Peek().Span);
        return SqlWindowFrameExclusionKind.None;
    }

    internal SqlWindowFrameKind GetWindowFrameKind(SqlToken token)
    {
        if (IsKeyword(token, "ROWS"))
        {
            return SqlWindowFrameKind.Rows;
        }

        if (IsKeyword(token, "RANGE"))
        {
            return SqlWindowFrameKind.Range;
        }

        if (IsKeyword(token, "GROUPS"))
        {
            return SqlWindowFrameKind.Groups;
        }

        return SqlWindowFrameKind.None;
    }

    internal SqlName CreateSinglePartName(SqlToken token)
    {
        SqlNamePart part = SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token);
        return new SqlName(new SqlNamePart[] { part }, token.Span);
    }

    internal SqlTextSpan ParseTypeNameSpan()
    {
        SqlToken first = Context.Peek();
        SqlToken last = first;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (Context.Peek().Kind == SqlTokenKind.CloseParen && depth == 0)
            {
                break;
            }

            SqlToken token = Context.Consume();
            last = token;
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        return SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
    }

    internal string GetSourceText(SqlTextSpan span)
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.GetText(span);
        }

        return "";
    }

    internal bool IsSubqueryStart(SqlToken token)
    {
        return IsKeyword(token, "SELECT") || IsKeyword(token, "WITH") || IsKeyword(token, "VALUES");
    }

    internal bool IsWindowClauseKeyword(SqlToken token)
    {
        return IsKeyword(token, "PARTITION") || IsKeyword(token, "ORDER") || IsWindowFrameStart(token);
    }

    internal bool IsWindowFrameStart(SqlToken token)
    {
        return IsKeyword(token, "ROWS") || IsKeyword(token, "RANGE") || IsKeyword(token, "GROUPS");
    }

    internal SqlToken ConsumeNamePart(List<SqlToken> tokens, List<SqlNamePart> parts)
    {
        SqlToken token = Context.Consume();
        tokens.Add(token);
        parts.Add(SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token));
        return token;
    }

    internal SqlName CreateName(List<SqlNamePart> parts)
    {
        if (parts.Count == 0)
        {
            return new SqlName(new SqlNamePart[0], new SqlTextSpan(0, 0));
        }

        SqlNamePart first = parts[0];
        SqlNamePart last = parts[parts.Count - 1];
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlName(parts.ToArray(), span);
    }

    internal SqlLiteral CreateLiteral(SqlToken token)
    {
        if (token.Kind == SqlTokenKind.NumericLiteral)
        {
            if (IsDecimalText(token.Text))
            {
                return SqlLiteral.Create(SqlLiteralKind.Decimal, token.Text, token.Span, token.Text, token);
            }

            return SqlLiteral.Create(SqlLiteralKind.Integer, token.Text, token.Span, token.Text, token);
        }

        if (token.Kind == SqlTokenKind.StringLiteral)
        {
            return SqlLiteral.Create(SqlLiteralKind.String, token.Text, token.Span, token.Value, token);
        }

        if (token.Kind == SqlTokenKind.BlobLiteral)
        {
            return SqlLiteral.Create(SqlLiteralKind.Blob, token.Text, token.Span, token.Text, token);
        }

        if (IsKeyword(token, "NULL"))
        {
            return SqlLiteral.Create(SqlLiteralKind.Null, token.Text, token.Span, null, token);
        }

        if (IsKeyword(token, "TRUE"))
        {
            return SqlLiteral.Create(SqlLiteralKind.Boolean, token.Text, token.Span, true, token);
        }

        if (IsKeyword(token, "FALSE"))
        {
            return SqlLiteral.Create(SqlLiteralKind.Boolean, token.Text, token.Span, false, token);
        }

        if (IsKeyword(token, "CURRENT_DATE"))
        {
            return SqlLiteral.Create(SqlLiteralKind.CurrentDate, token.Text, token.Span, token.Text, token);
        }

        if (IsKeyword(token, "CURRENT_TIME"))
        {
            return SqlLiteral.Create(SqlLiteralKind.CurrentTime, token.Text, token.Span, token.Text, token);
        }

        if (IsKeyword(token, "CURRENT_TIMESTAMP"))
        {
            return SqlLiteral.Create(SqlLiteralKind.CurrentTimestamp, token.Text, token.Span, token.Text, token);
        }

        return SqlLiteral.Create(SqlLiteralKind.Unknown, token.Text, token.Span, token.Text, token);
    }

    internal void SkipTrivia()
    {
        while (Context.Peek().Kind == SqlTokenKind.Whitespace || Context.Peek().Kind == SqlTokenKind.LineComment || Context.Peek().Kind == SqlTokenKind.BlockComment)
        {
            Context.Consume();
        }
    }

    internal SqlToken PeekNonTrivia(int nonTriviaOffset)
    {
        if (nonTriviaOffset < 0)
        {
            throw new ArgumentOutOfRangeException("nonTriviaOffset");
        }

        int offset = 0;
        int seen = 0;
        while (true)
        {
            SqlToken token = Context.PeekAtOffset(offset);
            if (!IsTrivia(token.Kind))
            {
                if (seen == nonTriviaOffset)
                {
                    return token;
                }

                seen = seen + 1;
            }

            if (token.Kind == SqlTokenKind.EndOfFile)
            {
                return token;
            }

            offset = offset + 1;
        }
    }

    internal bool IsTrivia(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }

    internal string GetOperatorText(List<SqlToken> tokens)
    {
        string text = "";
        int index = 0;
        while (index < tokens.Count)
        {
            if (index > 0)
            {
                text = text + " ";
            }

            text = text + tokens[index].Text;
            index = index + 1;
        }

        return text;
    }

    internal bool IsKeyword(SqlToken token, string text)
    {
        string? canonical = Context.Options.Dialect.GetCanonicalKeyword(token.Text);
        if (canonical == null)
        {
            canonical = token.Text.ToUpperInvariant();
        }

        return canonical == text;
    }

    internal bool IsIdentifierLike(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Identifier || token.Kind == SqlTokenKind.Keyword;
    }

    internal bool IsLiteralKeyword(SqlToken token)
    {
        return IsKeyword(token, "NULL") || IsKeyword(token, "TRUE") || IsKeyword(token, "FALSE") || IsKeyword(token, "CURRENT_DATE") || IsKeyword(token, "CURRENT_TIME") || IsKeyword(token, "CURRENT_TIMESTAMP");
    }

    internal bool IsDecimalText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '.' || text[index] == 'e' || text[index] == 'E')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal SqlNameQuoteStyle GetQuoteStyle(SqlToken token)
    {
        if (token.Text.Length == 0)
        {
            return SqlNameQuoteStyle.None;
        }

        if (token.Text[0] == '"')
        {
            return SqlNameQuoteStyle.DoubleQuote;
        }

        if (token.Text[0] == '[')
        {
            return SqlNameQuoteStyle.Bracket;
        }

        if (token.Text[0] == '`')
        {
            return SqlNameQuoteStyle.Backtick;
        }

        if (token.Text[0] == '\'')
        {
            return SqlNameQuoteStyle.SingleQuote;
        }

        return SqlNameQuoteStyle.None;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        string text = StripOuterQuotes(token.Text, quoteStyle);
        if (quoteStyle == SqlNameQuoteStyle.DoubleQuote)
        {
            return text.Replace("\"\"", "\"");
        }

        if (quoteStyle == SqlNameQuoteStyle.Bracket)
        {
            return text.Replace("]]", "]");
        }

        if (quoteStyle == SqlNameQuoteStyle.Backtick)
        {
            return text.Replace("``", "`");
        }

        if (quoteStyle == SqlNameQuoteStyle.SingleQuote)
        {
            return text.Replace("''", "'");
        }

        return text;
    }

    internal string GetStringValue(string sourceText)
    {
        if (sourceText.Length == 0)
        {
            return sourceText;
        }

        SqlNameQuoteStyle quoteStyle = SqlNameQuoteStyle.None;
        if (sourceText[0] == '\'')
        {
            quoteStyle = SqlNameQuoteStyle.SingleQuote;
        }

        if (sourceText[0] == '"')
        {
            quoteStyle = SqlNameQuoteStyle.DoubleQuote;
        }

        string text = StripOuterQuotes(sourceText, quoteStyle);
        if (quoteStyle == SqlNameQuoteStyle.DoubleQuote)
        {
            return text.Replace("\"\"", "\"");
        }

        if (quoteStyle == SqlNameQuoteStyle.SingleQuote)
        {
            return text.Replace("''", "'");
        }

        return text;
    }

    internal string StripOuterQuotes(string text, SqlNameQuoteStyle quoteStyle)
    {
        if (quoteStyle == SqlNameQuoteStyle.None || text.Length == 0)
        {
            return text;
        }

        int start = 1;
        int end = text.Length;
        if (text.Length > 1)
        {
            if (quoteStyle == SqlNameQuoteStyle.DoubleQuote && text[text.Length - 1] == '"')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Bracket && text[text.Length - 1] == ']')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Backtick && text[text.Length - 1] == '`')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.SingleQuote && text[text.Length - 1] == '\'')
            {
                end = text.Length - 1;
            }
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start);
    }
}
