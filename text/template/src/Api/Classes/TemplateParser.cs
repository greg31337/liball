using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Builds a native template syntax tree from lexer tokens and combines lexer and parser diagnostics.
/// </summary>
public class TemplateParser : ITemplateParser
{
    internal TemplateLexerResult LexerResult;
    internal List<TemplateToken> Tokens;
    internal TemplateSource Source;
    internal int Position;
    internal TemplateParseResult? ActiveResult;
    internal TemplateExpressionPrecedenceMode ExpressionPrecedence;
    internal ITemplateDiagnosticSink? DiagnosticSink;

    /// <summary>
    /// Initializes a new template parser and validates any supplied values.
    /// </summary>
    /// <param name="lexerResult">The lexer result used by the operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateParser(TemplateLexerResult lexerResult)
    {
        if (lexerResult == null)
        {
            throw new ArgumentNullException("lexerResult");
        }

        if (lexerResult.Tokens == null || lexerResult.Tokens.Count == 0)
        {
            throw new ArgumentException("Lexer result must contain at least one token.", "lexerResult");
        }

        LexerResult = lexerResult;
        Tokens = lexerResult.Tokens;
        Source = Tokens[0].Span.Source;
        Position = 0;
        ActiveResult = null;
        ExpressionPrecedence = TemplateExpressionPrecedenceMode.Standard;
        DiagnosticSink = null;
    }

    /// <summary>
    /// Sets the sink that receives lexer and parser diagnostics during parsing.
    /// </summary>
    /// <param name="diagnosticSink">The sink that receives diagnostics.</param>
    public void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink)
    {
        if (diagnosticSink == null)
        {
            throw new ArgumentNullException("diagnosticSink");
        }

        DiagnosticSink = diagnosticSink;
    }

    /// <summary>
    /// Lexes and parses native template source into a document and a combined diagnostic list.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateParseResult ParseNative(string text)
    {
        TemplateLexerResult lexerResult = TemplateLexer.LexNative(text);
        TemplateParser parser = new TemplateParser(lexerResult);
        return parser.Parse();
    }

    /// <summary>
    /// Lexes and parses native template source into a document and a combined diagnostic list.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateParseResult ParseNative(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        TemplateLexer lexer = new TemplateLexer(source);
        TemplateLexerResult lexerResult = lexer.Lex();
        TemplateParser parser = new TemplateParser(lexerResult);
        return parser.Parse();
    }

    /// <summary>
    /// Parses lexer tokens into a document and combines lexer and parser diagnostics.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TemplateParseResult Parse()
    {
        TemplateDocumentSyntax document = new TemplateDocumentSyntax(Source);
        TemplateParseResult result = new TemplateParseResult(document);
        ActiveResult = result;
        CopyLexerDiagnostics(result);
        ParseNodes(result, document.Children, "");
        return result;
    }

    internal TemplateParserBlockResult ParseNodes(TemplateParseResult result, List<TemplateSyntaxNode> children, string expectedEndName)
    {
        TemplateParserBlockResult blockResult = new TemplateParserBlockResult();

        while (!AtEnd())
        {
            TemplateToken token = Current();
            if (token.Kind == TemplateTokenKind.Text)
            {
                children.Add(ReadTextNode());
            }
            else if (token.Kind == TemplateTokenKind.OutputStartDelimiter)
            {
                children.Add(ReadOutputNode());
            }
            else if (token.Kind == TemplateTokenKind.CommentStartDelimiter)
            {
                children.Add(ReadCommentNode());
            }
            else if (token.Kind == TemplateTokenKind.RawBlockStartDelimiter)
            {
                children.Add(ReadRawNode());
            }
            else if (token.Kind == TemplateTokenKind.StatementStartDelimiter)
            {
                TemplateStatementSyntax statement = ReadStatement(result);
                if (statement.Name == "")
                {
                    AddDiagnosticAndError(result, children, "PARSE007", "Statement tag is missing a name.", statement.ContentSpan, "Add a statement name after the opening delimiter.");
                }
                else if (IsEndStatementName(statement.Name))
                {
                    if (expectedEndName == "")
                    {
                        AddDiagnosticAndError(result, children, "PARSE002", "Unexpected end tag '" + statement.Name + "'.", statement.Span, "Remove the end tag or add a matching block start.");
                    }
                    else if (statement.Name == expectedEndName)
                    {
                        blockResult.FoundEnd = true;
                        blockResult.EndStatement = statement;
                        return blockResult;
                    }
                    else
                    {
                        AddDiagnostic(result, "PARSE003", "Mismatched end tag '" + statement.Name + "'. Expected '" + expectedEndName + "'.", statement.Span, "Use the matching end tag for this block.");
                        blockResult.FoundEnd = true;
                        blockResult.EndStatement = statement;
                        return blockResult;
                    }
                }
                else if (IsBranchStatementName(statement.Name))
                {
                    if (expectedEndName == "")
                    {
                        AddDiagnosticAndError(result, children, "PARSE005", "Invalid branch tag '" + statement.Name + "' outside a block.", statement.Span, "Move the branch inside a compatible block.");
                    }
                    else
                    {
                        blockResult.FoundBranch = true;
                        blockResult.BranchStatement = statement;
                        return blockResult;
                    }
                }
                else if (IsBlockStartName(statement.Name))
                {
                    children.Add(ReadBlockNode(result, statement));
                }
                else if (IsKnownStandaloneStatementName(statement.Name))
                {
                    children.Add(TemplateSyntaxNode.FromStatement(statement));
                }
                else
                {
                    AddDiagnosticAndError(result, children, "PARSE006", "Unknown tag '" + statement.Name + "'.", statement.Span, "Register the tag or check its spelling.");
                }
            }
            else
            {
                ParseUnexpectedToken(result, children);
            }
        }

        return blockResult;
    }

    internal TemplateSyntaxNode ReadBlockNode(TemplateParseResult result, TemplateStatementSyntax startStatement)
    {
        string expectedEndName = GetEndStatementName(startStatement.Name);
        TemplateBlockSyntax block = new TemplateBlockSyntax(startStatement.Span, startStatement.Name, startStatement);
        TemplateParserBlockResult childResult = ParseNodes(result, block.Children, expectedEndName);

        while (childResult.FoundBranch)
        {
            TemplateStatementSyntax? branchStatement = childResult.BranchStatement;
            if (branchStatement == null)
            {
                break;
            }

            if (IsAllowedBranchStatement(startStatement.Name, branchStatement.Name))
            {
                block.AddElseChild(TemplateSyntaxNode.FromStatement(branchStatement));
                TemplateBranchSyntax branch = new TemplateBranchSyntax(branchStatement.Span, branchStatement);
                childResult = ParseNodes(result, branch.Children, expectedEndName);
                int childIndex = 0;
                while (childIndex < branch.Children.Count)
                {
                    block.AddElseChild(branch.Children[childIndex]);
                    childIndex = childIndex + 1;
                }

                int branchEnd = Source.Length;
                if (childResult.FoundBranch && childResult.BranchStatement != null)
                {
                    branchEnd = childResult.BranchStatement.Span.Start;
                }
                else if (childResult.FoundEnd && childResult.EndStatement != null)
                {
                    branchEnd = childResult.EndStatement.Span.Start;
                }

                branch.Span = CreateSpanBetween(branchStatement.Span.Start, branchEnd);
                block.AddBranch(branch);
            }
            else
            {
                TemplateDiagnostic diagnostic = AddDiagnostic(result, "PARSE005", "Invalid branch tag '" + branchStatement.Name + "' inside '" + startStatement.Name + "' block.", branchStatement.Span, "Use a branch tag that belongs to this block type.");
                TemplateErrorSyntax error = CreateRecoveryError(branchStatement.Span, diagnostic);
                block.AddChild(TemplateSyntaxNode.FromError(error));
                childResult = ParseNodes(result, block.Children, expectedEndName);
            }
        }

        if (childResult.FoundEnd)
        {
            TemplateStatementSyntax? endStatement = childResult.EndStatement;
            if (endStatement != null)
            {
                block.SetEndStatement(endStatement);
                if (endStatement.Name != expectedEndName && result.Diagnostics.Count > 0)
                {
                    TemplateDiagnostic diagnostic = result.Diagnostics[result.Diagnostics.Count - 1];
                    block.AddRecoveryNode(CreateRecoveryError(endStatement.Span, diagnostic));
                }

                block.Span = CreateSpanBetween(startStatement.Span.Start, endStatement.Span.End);
            }
        }
        else
        {
            TemplateSourceSpan span = CreateSpanBetween(startStatement.Span.Start, Source.Length);
            AddDiagnostic(result, "PARSE004", "Unexpected end of file in '" + startStatement.Name + "' block. Expected '" + expectedEndName + "'.", span, "Add the missing end tag.");
            TemplateDiagnostic diagnostic = result.Diagnostics[result.Diagnostics.Count - 1];
            block.AddRecoveryNode(CreateRecoveryError(span, diagnostic));
            block.Span = span;
        }

        return TemplateSyntaxNode.FromBlock(block);
    }

    internal TemplateSyntaxNode ReadTextNode()
    {
        TemplateToken token = Current();
        TemplateTextSyntax text = TemplateTextSyntax.Create(token.Span, token.Text);
        Position = Position + 1;
        return TemplateSyntaxNode.FromText(text);
    }

    internal TemplateSyntaxNode ReadOutputNode()
    {
        TemplateToken start = Current();
        Position = Position + 1;
        SkipLeftTrimMarker();

        int contentStartIndex = Position;
        int contentEndIndex = FindTagEnd(TemplateTokenKind.OutputEndDelimiter);
        int contentLastIndex = contentEndIndex - 1;
        if (contentLastIndex >= contentStartIndex && Tokens[contentLastIndex].Kind == TemplateTokenKind.RightTrimMarker)
        {
            contentLastIndex = contentLastIndex - 1;
        }

        TemplateExpressionSyntax expression = CreateExpression(contentStartIndex, contentLastIndex);
        TemplateToken end = TokenBeforeOrCurrentEnd(contentEndIndex, start);
        TemplateSourceSpan span = CreateSpanBetween(start.Span.Start, end.Span.End);
        TemplateOutputSyntax output = new TemplateOutputSyntax(span, expression, false, true);
        Position = MovePastEnd(contentEndIndex);
        return TemplateSyntaxNode.FromOutput(output);
    }

    internal TemplateStatementSyntax ReadStatement(TemplateParseResult result)
    {
        TemplateToken start = Current();
        Position = Position + 1;
        SkipLeftTrimMarker();

        int contentStartIndex = Position;
        int contentEndIndex = FindTagEnd(TemplateTokenKind.StatementEndDelimiter);
        int contentLastIndex = contentEndIndex - 1;
        if (contentLastIndex >= contentStartIndex && Tokens[contentLastIndex].Kind == TemplateTokenKind.RightTrimMarker)
        {
            contentLastIndex = contentLastIndex - 1;
        }

        string name = "";
        if (contentStartIndex <= contentLastIndex && (Tokens[contentStartIndex].Kind == TemplateTokenKind.Identifier || Tokens[contentStartIndex].Kind == TemplateTokenKind.EmptyLiteral))
        {
            name = Tokens[contentStartIndex].Text;
        }

        TemplateToken end = TokenBeforeOrCurrentEnd(contentEndIndex, start);
        TemplateSourceSpan span = CreateSpanBetween(start.Span.Start, end.Span.End);
        TemplateStatementSyntax statement = new TemplateStatementSyntax(span, name);
        TemplateSourceSpan nameSpan = Source.CreateSpan(start.Span.End, 0);
        if (contentStartIndex <= contentLastIndex)
        {
            nameSpan = Tokens[contentStartIndex].Span;
        }

        TemplateSourceSpan contentSpan = Source.CreateSpan(start.Span.End, 0);
        if (contentStartIndex <= contentLastIndex)
        {
            contentSpan = CreateSpanBetween(Tokens[contentStartIndex].Span.Start, Tokens[contentLastIndex].Span.End);
        }

        statement.SetLexicalSpans(nameSpan, contentSpan);
        AddStatementArguments(result, statement, contentStartIndex + 1, contentLastIndex);
        ConfigureDynamicName(statement);
        Position = MovePastEnd(contentEndIndex);
        return statement;
    }

    internal TemplateSyntaxNode ReadCommentNode()
    {
        TemplateToken start = Current();
        Position = Position + 1;
        SkipLeftTrimMarker();

        string text = "";
        if (!AtEnd() && Current().Kind == TemplateTokenKind.CommentText)
        {
            text = Current().Text;
            Position = Position + 1;
        }

        if (!AtEnd() && Current().Kind == TemplateTokenKind.RightTrimMarker)
        {
            Position = Position + 1;
        }

        TemplateToken end = start;
        if (!AtEnd() && Current().Kind == TemplateTokenKind.CommentEndDelimiter)
        {
            end = Current();
            Position = Position + 1;
        }

        TemplateSourceSpan span = CreateSpanBetween(start.Span.Start, end.Span.End);
        TemplateCommentSyntax comment = new TemplateCommentSyntax(span, text);
        return TemplateSyntaxNode.FromComment(comment);
    }

    internal TemplateSyntaxNode ReadRawNode()
    {
        TemplateToken start = Current();
        Position = Position + 1;

        string text = "";
        if (!AtEnd() && Current().Kind == TemplateTokenKind.RawText)
        {
            text = Current().Text;
            Position = Position + 1;
        }

        TemplateToken end = start;
        if (!AtEnd() && Current().Kind == TemplateTokenKind.RawBlockEndDelimiter)
        {
            end = Current();
            Position = Position + 1;
        }

        TemplateSourceSpan span = CreateSpanBetween(start.Span.Start, end.Span.End);
        TemplateRawSyntax raw = new TemplateRawSyntax(span, text);
        return TemplateSyntaxNode.FromRaw(raw);
    }

    internal void ParseUnexpectedToken(TemplateParseResult result, List<TemplateSyntaxNode> children)
    {
        TemplateToken token = Current();
        AddDiagnosticAndError(result, children, "PARSE001", "Unexpected token '" + TemplateTokenKindFacts.GetDisplayName(token.Kind) + "'.", token.Span, "Skip the token or place it inside a valid tag.");
        Position = Position + 1;
    }

    internal void AddDiagnosticAndError(TemplateParseResult result, List<TemplateSyntaxNode> children, string code, string message, TemplateSourceSpan span, string hint)
    {
        TemplateDiagnostic diagnostic = AddDiagnostic(result, code, message, span, hint);
        TemplateErrorSyntax error = CreateRecoveryError(span, diagnostic);
        children.Add(TemplateSyntaxNode.FromError(error));
    }

    internal TemplateErrorSyntax CreateRecoveryError(TemplateSourceSpan span, TemplateDiagnostic diagnostic)
    {
        TemplateErrorSyntax error = new TemplateErrorSyntax(span, diagnostic);
        int index = 0;
        while (index < Tokens.Count)
        {
            TemplateToken token = Tokens[index];
            if (token.Kind != TemplateTokenKind.EndOfFile && token.Span.End > span.Start && token.Span.Start < span.End)
            {
                error.AddRecoveredToken(token);
            }

            index = index + 1;
        }

        return error;
    }

    internal TemplateDiagnostic AddDiagnostic(TemplateParseResult result, string code, string message, TemplateSourceSpan span, string hint)
    {
        TemplateDiagnostic diagnostic = new TemplateDiagnostic(code, TemplateDiagnosticSeverity.Error, message, span, hint);
        result.AddDiagnostic(diagnostic);
        ReportDiagnostic(diagnostic);
        return diagnostic;
    }

    internal void ReportDiagnostic(TemplateDiagnostic diagnostic)
    {
        if (DiagnosticSink != null)
        {
            DiagnosticSink.Report(diagnostic);
        }
    }

    internal TemplateExpressionSyntax CreateExpression(int startIndex, int endIndex)
    {
        if (startIndex > endIndex || startIndex >= Tokens.Count)
        {
            int emptyStart = Source.Length;
            if (startIndex >= 0 && startIndex < Tokens.Count)
            {
                emptyStart = Tokens[startIndex].Span.Start;
            }
            else if (startIndex > 0 && startIndex - 1 < Tokens.Count)
            {
                emptyStart = Tokens[startIndex - 1].Span.End;
            }

            TemplateSourceSpan emptySpan = Source.CreateSpan(emptyStart, 0);
            return new TemplateExpressionSyntax(emptySpan, TemplateExpressionSyntaxKind.Unknown, "");
        }

        TemplateExpressionOperatorMatch conditional = FindTopLevelConditional(startIndex, endIndex);
        if (conditional.Found)
        {
            TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, TemplateExpressionOperatorKind.Conditional);
            expression.AddChild(CreateExpression(startIndex, conditional.StartIndex - 1));
            expression.AddChild(CreateExpression(conditional.StartIndex + 1, conditional.ColonIndex - 1));
            expression.AddChild(CreateExpression(conditional.ColonIndex + 1, endIndex));
            return expression;
        }

        TemplateExpressionOperatorMatch nullCoalescing = FindTopLevelNullCoalescing(startIndex, endIndex);
        if (nullCoalescing.Found)
        {
            TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, TemplateExpressionOperatorKind.NullCoalescing);
            expression.AddChild(CreateExpression(startIndex, nullCoalescing.StartIndex - 1));
            expression.AddChild(CreateExpression(nullCoalescing.EndIndex + 1, endIndex));
            return expression;
        }

        if (ExpressionPrecedence == TemplateExpressionPrecedenceMode.LiquidRightToLeft)
        {
            TemplateExpressionOperatorMatch liquidOperator = FindTopLevelLiquidBinaryOperator(startIndex, endIndex);
            if (liquidOperator.Found)
            {
                TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, liquidOperator.OperatorKind);
                expression.AddChild(CreateExpression(startIndex, liquidOperator.StartIndex - 1));
                expression.AddChild(CreateExpression(liquidOperator.EndIndex + 1, endIndex));
                return expression;
            }
        }
        else
        {
            int precedence = 1;
            while (precedence <= 2)
            {
                TemplateExpressionOperatorMatch binaryOperator = FindTopLevelBinaryOperator(startIndex, endIndex, precedence);
                if (binaryOperator.Found)
                {
                    TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, binaryOperator.OperatorKind);
                    expression.AddChild(CreateExpression(startIndex, binaryOperator.StartIndex - 1));
                    expression.AddChild(CreateExpression(binaryOperator.EndIndex + 1, endIndex));
                    return expression;
                }

                precedence = precedence + 1;
            }
        }

        if (IsIdentifierText(startIndex, "not") && startIndex < endIndex)
        {
            TemplateExpressionSyntax keywordNot = CreateOperatorExpression(startIndex, endIndex, TemplateExpressionOperatorKind.UnaryNot);
            keywordNot.AddChild(CreateExpression(startIndex + 1, endIndex));
            return keywordNot;
        }

        if (ExpressionPrecedence == TemplateExpressionPrecedenceMode.Standard)
        {
            int precedence = 3;
            while (precedence <= 6)
            {
                TemplateExpressionOperatorMatch binaryOperator = FindTopLevelBinaryOperator(startIndex, endIndex, precedence);
                if (binaryOperator.Found)
                {
                    TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, binaryOperator.OperatorKind);
                    expression.AddChild(CreateExpression(startIndex, binaryOperator.StartIndex - 1));
                    expression.AddChild(CreateExpression(binaryOperator.EndIndex + 1, endIndex));
                    return expression;
                }

                precedence = precedence + 1;
            }
        }

        TemplateExpressionOperatorMatch testOperator = FindTopLevelTestOperator(startIndex, endIndex);
        if (testOperator.Found)
        {
            return CreateTestExpression(startIndex, endIndex, testOperator.StartIndex);
        }

        int filterPipeIndex = FindTopLevelFilterPipe(startIndex, endIndex);
        if (filterPipeIndex >= 0)
        {
            return CreateFilterExpression(startIndex, endIndex, filterPipeIndex);
        }

        int rangeOperatorIndex = FindTopLevelRangeOperator(startIndex, endIndex);
        if (rangeOperatorIndex >= 0)
        {
            TemplateExpressionSyntax range = CreateLiteralExpression(startIndex, endIndex, TemplateLiteralSyntaxKind.Range);
            if (startIndex < rangeOperatorIndex)
            {
                range.AddChild(CreateExpression(startIndex, rangeOperatorIndex - 1));
            }

            if (rangeOperatorIndex + 2 <= endIndex)
            {
                range.AddChild(CreateExpression(rangeOperatorIndex + 2, endIndex));
            }

            return range;
        }

        TemplateExpressionOperatorKind unaryOperator = GetUnaryOperatorKind(startIndex);
        if (unaryOperator != TemplateExpressionOperatorKind.None && startIndex < endIndex)
        {
            TemplateExpressionSyntax expression = CreateOperatorExpression(startIndex, endIndex, unaryOperator);
            expression.AddChild(CreateExpression(startIndex + 1, endIndex));
            return expression;
        }

        TemplateExpressionSyntax? subexpression = TryCreateSubexpression(startIndex, endIndex);
        if (subexpression != null)
        {
            return subexpression;
        }

        if (IsDelimitedExpression(startIndex, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen))
        {
            TemplateToken groupFirst = Tokens[startIndex];
            TemplateToken groupLast = Tokens[endIndex];
            TemplateSourceSpan groupSpan = CreateSpanBetween(groupFirst.Span.Start, groupLast.Span.End);
            TemplateExpressionSyntax expression = new TemplateExpressionSyntax(groupSpan, TemplateExpressionSyntaxKind.Group, JoinTokenText(startIndex, endIndex));
            expression.AddChild(CreateExpression(startIndex + 1, endIndex - 1));
            return expression;
        }

        if (IsDelimitedExpression(startIndex, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket))
        {
            TemplateExpressionSyntax array = CreateLiteralExpression(startIndex, endIndex, TemplateLiteralSyntaxKind.Array);
            AddSeparatedExpressionChildren(array, startIndex + 1, endIndex - 1);
            return array;
        }

        if (IsDelimitedExpression(startIndex, endIndex, TemplateTokenKind.OpenBrace, TemplateTokenKind.CloseBrace))
        {
            TemplateExpressionSyntax objectLiteral = CreateLiteralExpression(startIndex, endIndex, TemplateLiteralSyntaxKind.Object);
            AddObjectLiteralChildren(objectLiteral, startIndex + 1, endIndex - 1);
            return objectLiteral;
        }

        TemplateExpressionSyntax? functionCall = TryCreateFunctionCallExpression(startIndex, endIndex);
        if (functionCall != null)
        {
            return functionCall;
        }

        TemplateExpressionSyntax? whitespaceCall = TryCreateWhitespaceCallExpression(startIndex, endIndex);
        if (whitespaceCall != null)
        {
            return whitespaceCall;
        }

        TemplateExpressionSyntax? path = TryCreatePathExpression(startIndex, endIndex);
        if (path != null)
        {
            return path;
        }

        if (startIndex == endIndex)
        {
            TemplateToken token = Tokens[startIndex];
            TemplateExpressionSyntaxKind singleKind = InferSingleTokenExpressionKind(token.Kind);
            TemplateLiteralSyntaxKind literalKind = InferSingleTokenLiteralKind(token.Kind);
            return TemplateExpressionSyntax.Create(token.Span, singleKind, literalKind, token.Text);
        }

        if (LooksInvalidExpression(startIndex, endIndex))
        {
            return CreateUnknownExpression(startIndex, endIndex);
        }

        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        string text = JoinTokenText(startIndex, endIndex);
        TemplateExpressionSyntaxKind kind = InferExpressionKind(startIndex, endIndex);
        return new TemplateExpressionSyntax(span, kind, text);
    }

    internal void AddStatementArguments(TemplateParseResult result, TemplateStatementSyntax statement, int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
        {
            return;
        }

        int argumentEndIndex = endIndex;
        int blockParameterStart = FindBlockParameterStart(startIndex, endIndex);
        if (blockParameterStart >= 0)
        {
            AddBlockParameters(result, statement, blockParameterStart, endIndex);
            argumentEndIndex = blockParameterStart - 1;
        }

        if (startIndex > argumentEndIndex)
        {
            return;
        }

        if (ShouldParseSingleExpressionArgument(statement.Name))
        {
            statement.AddArgument(CreateExpression(startIndex, argumentEndIndex));
            return;
        }

        int index = startIndex;
        while (index <= argumentEndIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Comma)
            {
                index = index + 1;
            }
            else if (!ShouldKeepAssignmentOperands(statement.Name) && IsNamedArgumentStart(index, argumentEndIndex))
            {
                int namedEndIndex = AddStatementNamedArgument(result, statement, index, argumentEndIndex);
                index = namedEndIndex + 1;
            }
            else
            {
                int positionalEndIndex = FindStatementArgumentEnd(index, argumentEndIndex);
                statement.AddArgument(CreateExpression(index, positionalEndIndex));
                index = positionalEndIndex + 1;
            }
        }
    }

    internal int FindBlockParameterStart(int startIndex, int endIndex)
    {
        int depth = 0;
        int index = startIndex;
        while (index + 1 <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (depth == 0 && IsIdentifierText(index, "as") && Tokens[index + 1].Kind == TemplateTokenKind.Pipe)
            {
                return index;
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return -1;
    }

    internal void AddBlockParameters(TemplateParseResult result, TemplateStatementSyntax statement, int startIndex, int endIndex)
    {
        int closePipeIndex = -1;
        int index = startIndex + 2;
        while (index <= endIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Pipe)
            {
                closePipeIndex = index;
                break;
            }

            if (Tokens[index].Kind == TemplateTokenKind.Identifier)
            {
                statement.AddBlockParameter(new TemplateBlockParameterSyntax(Tokens[index].Span, Tokens[index].Text));
            }
            else if (Tokens[index].Kind != TemplateTokenKind.Comma)
            {
                AddStatementRecovery(result, statement, "PARSE009", "Invalid token in block parameter list.", Tokens[index].Span, "Use identifier aliases between the pipe delimiters.");
            }

            index = index + 1;
        }

        if (closePipeIndex < 0)
        {
            TemplateSourceSpan span = CreateSpanBetween(Tokens[startIndex].Span.Start, Tokens[endIndex].Span.End);
            AddStatementRecovery(result, statement, "PARSE009", "Unterminated block parameter list.", span, "Add the closing pipe delimiter.");
        }
        else
        {
            if (statement.BlockParameters.Count == 0)
            {
                TemplateSourceSpan emptySpan = CreateSpanBetween(Tokens[startIndex + 1].Span.Start, Tokens[closePipeIndex].Span.End);
                AddStatementRecovery(result, statement, "PARSE009", "Block parameter list must declare at least one alias.", emptySpan, "Add an identifier between the pipe delimiters.");
            }

            if (closePipeIndex < endIndex)
            {
                TemplateSourceSpan trailingSpan = CreateSpanBetween(Tokens[closePipeIndex + 1].Span.Start, Tokens[endIndex].Span.End);
                AddStatementRecovery(result, statement, "PARSE009", "Unexpected source after block parameter list.", trailingSpan, "Move arguments before the block parameter declaration.");
            }
        }
    }

    internal int AddStatementNamedArgument(TemplateParseResult result, TemplateStatementSyntax statement, int startIndex, int endIndex)
    {
        bool hash = IsHashArgumentStatement(statement.Name);
        int valueStartIndex = startIndex + 2;
        int valueEndIndex = FindNamedArgumentValueEnd(valueStartIndex, endIndex);
        TemplateExpressionSyntax value;
        TemplateSourceSpan argumentSpan;
        if (valueStartIndex > endIndex || valueEndIndex < valueStartIndex)
        {
            TemplateToken equalsToken = Tokens[startIndex + 1];
            TemplateSourceSpan missingSpan = Source.CreateSpan(equalsToken.Span.End, 0);
            value = new TemplateExpressionSyntax(missingSpan, TemplateExpressionSyntaxKind.Unknown, "");
            argumentSpan = CreateSpanBetween(Tokens[startIndex].Span.Start, equalsToken.Span.End);
            AddStatementRecovery(result, statement, "PARSE008", "Named argument '" + Tokens[startIndex].Text + "' is missing a value.", argumentSpan, "Add an expression after the equals delimiter.");
            valueEndIndex = startIndex + 1;
        }
        else
        {
            value = CreateExpression(valueStartIndex, valueEndIndex);
            argumentSpan = CreateSpanBetween(Tokens[startIndex].Span.Start, Tokens[valueEndIndex].Span.End);
        }

        TemplateNamedArgumentSyntax argument = new TemplateNamedArgumentSyntax(argumentSpan, Tokens[startIndex].Span, Tokens[startIndex + 1].Span, Tokens[startIndex].Text, value, hash);
        if (HasArgumentName(statement.NamedArguments, statement.HashArguments, argument.Name))
        {
            AddStatementRecovery(result, statement, "PARSE012", "Duplicate named argument '" + argument.Name + "'.", argument.NameSpan, "Remove or rename the duplicate argument.");
        }

        if (hash)
        {
            statement.AddHashArgument(argument);
        }
        else
        {
            statement.AddNamedArgument(argument);
        }

        return valueEndIndex;
    }

    internal int FindNamedArgumentValueEnd(int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
        {
            return startIndex - 1;
        }

        int depth = 0;
        int index = startIndex;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (depth == 0 && kind == TemplateTokenKind.Comma)
            {
                return index - 1;
            }

            if (depth == 0 && index > startIndex && IsNamedArgumentStart(index, endIndex))
            {
                return index - 1;
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return endIndex;
    }

    internal bool IsNamedArgumentStart(int index, int endIndex)
    {
        return index + 1 <= endIndex
            && Tokens[index].Kind == TemplateTokenKind.Identifier
            && Tokens[index + 1].Kind == TemplateTokenKind.Equals;
    }

    internal bool ShouldKeepAssignmentOperands(string statementName)
    {
        return statementName == "set" || statementName == "assign";
    }

    internal bool IsHashArgumentStatement(string statementName)
    {
        return statementName == "handlebarsBlock"
            || statementName == "handlebarsEach"
            || statementName == "handlebarsPartialBlock"
            || statementName == "handlebarsInlinePartial";
    }

    internal bool HasArgumentName(List<TemplateNamedArgumentSyntax> namedArguments, List<TemplateNamedArgumentSyntax> hashArguments, string name)
    {
        int index = 0;
        while (index < namedArguments.Count)
        {
            if (namedArguments[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < hashArguments.Count)
        {
            if (hashArguments[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddStatementRecovery(TemplateParseResult result, TemplateStatementSyntax statement, string code, string message, TemplateSourceSpan span, string hint)
    {
        TemplateDiagnostic diagnostic = AddDiagnostic(result, code, message, span, hint);
        statement.AddRecoveryNode(CreateRecoveryError(span, diagnostic));
    }

    internal void ConfigureDynamicName(TemplateStatementSyntax statement)
    {
        if (!SupportsDynamicName(statement.Name) || statement.Arguments.Count == 0)
        {
            return;
        }

        TemplateExpressionSyntax expression = statement.Arguments[0];
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Literal && expression.LiteralKind == TemplateLiteralSyntaxKind.String)
        {
            return;
        }

        TemplateExpressionSyntax dynamicExpression = expression;
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Group && expression.Children.Count == 1)
        {
            dynamicExpression = expression.Children[0];
        }

        statement.SetDynamicName(new TemplateDynamicNameSyntax(expression.Span, dynamicExpression));
    }

    internal bool SupportsDynamicName(string statementName)
    {
        return statementName == "include"
            || statementName == "render"
            || statementName == "liquidRenderFor"
            || statementName == "liquidIncludeFor"
            || statementName == "partial"
            || statementName == "extends"
            || statementName == "import"
            || statementName == "handlebarsPartialBlock"
            || statementName == "handlebarsInlinePartial";
    }

    internal bool ShouldParseSingleExpressionArgument(string statementName)
    {
        return statementName == "if"
            || statementName == "unless"
            || statementName == "elseif"
            || statementName == "while"
            || statementName == "with"
            || statementName == "case"
            || statementName == "return";
    }

    internal TemplateExpressionSyntax CreateLiteralExpression(int startIndex, int endIndex, TemplateLiteralSyntaxKind literalKind)
    {
        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        string text = JoinTokenText(startIndex, endIndex);
        return TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Literal, literalKind, text);
    }

    internal TemplateExpressionSyntax CreateOperatorExpression(int startIndex, int endIndex, TemplateExpressionOperatorKind operatorKind)
    {
        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        string text = JoinTokenText(startIndex, endIndex);
        return TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Operator, operatorKind, text);
    }

    internal TemplateExpressionSyntax CreateUnknownExpression(int startIndex, int endIndex)
    {
        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        string text = JoinTokenText(startIndex, endIndex);
        return new TemplateExpressionSyntax(span, TemplateExpressionSyntaxKind.Unknown, text);
    }

    internal TemplateExpressionSyntax CreateCallExpression(int startIndex, int endIndex, TemplateExpressionCallKind callKind, string name)
    {
        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        string text = JoinTokenText(startIndex, endIndex);
        return TemplateExpressionSyntax.Create(span, TemplateExpressionSyntaxKind.Call, callKind, name, text);
    }

    internal void AddSeparatedExpressionChildren(TemplateExpressionSyntax parent, int startIndex, int endIndex)
    {
        int segmentStart = startIndex;
        int index = startIndex;
        int depth = 0;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == TemplateTokenKind.Comma && depth == 0)
            {
                if (segmentStart <= index - 1)
                {
                    parent.AddChild(CreateExpression(segmentStart, index - 1));
                }

                segmentStart = index + 1;
            }
            else
            {
                depth = AdjustDepth(depth, kind);
            }

            index = index + 1;
        }

        if (segmentStart <= endIndex)
        {
            parent.AddChild(CreateExpression(segmentStart, endIndex));
        }
    }

    internal void AddObjectLiteralChildren(TemplateExpressionSyntax parent, int startIndex, int endIndex)
    {
        int segmentStart = startIndex;
        int index = startIndex;
        int depth = 0;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == TemplateTokenKind.Comma && depth == 0)
            {
                AddObjectLiteralSegment(parent, segmentStart, index - 1);
                segmentStart = index + 1;
            }
            else
            {
                depth = AdjustDepth(depth, kind);
            }

            index = index + 1;
        }

        AddObjectLiteralSegment(parent, segmentStart, endIndex);
    }

    internal void AddObjectLiteralSegment(TemplateExpressionSyntax parent, int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
        {
            return;
        }

        int colonIndex = FindTopLevelToken(startIndex, endIndex, TemplateTokenKind.Colon);
        if (colonIndex < 0)
        {
            parent.AddChild(CreateExpression(startIndex, endIndex));
            return;
        }

        if (startIndex <= colonIndex - 1)
        {
            parent.AddChild(CreateExpression(startIndex, colonIndex - 1));
        }

        if (colonIndex + 1 <= endIndex)
        {
            parent.AddChild(CreateExpression(colonIndex + 1, endIndex));
        }
    }

    internal TemplateExpressionOperatorMatch FindTopLevelConditional(int startIndex, int endIndex)
    {
        TemplateExpressionOperatorMatch result = new TemplateExpressionOperatorMatch();
        int depth = 0;
        int index = startIndex;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == TemplateTokenKind.Question && depth == 0)
            {
                if (index + 1 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.Question)
                {
                    index = index + 2;
                    continue;
                }

                int colonIndex = FindTopLevelToken(index + 1, endIndex, TemplateTokenKind.Colon);
                if (colonIndex > index)
                {
                    result.Found = true;
                    result.StartIndex = index;
                    result.EndIndex = index;
                    result.OperatorKind = TemplateExpressionOperatorKind.Conditional;
                    result.ColonIndex = colonIndex;
                    return result;
                }
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return result;
    }

    internal TemplateExpressionOperatorMatch FindTopLevelNullCoalescing(int startIndex, int endIndex)
    {
        TemplateExpressionOperatorMatch result = new TemplateExpressionOperatorMatch();
        int depth = 0;
        int index = endIndex;
        while (index - 1 >= startIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            depth = AdjustReverseDepth(depth, kind);
            if (Tokens[index - 1].Kind == TemplateTokenKind.Question && kind == TemplateTokenKind.Question && depth == 0)
            {
                result.Found = true;
                result.StartIndex = index - 1;
                result.EndIndex = index;
                result.OperatorKind = TemplateExpressionOperatorKind.NullCoalescing;
                return result;
            }

            index = index - 1;
        }

        return result;
    }

    internal TemplateExpressionOperatorMatch FindTopLevelBinaryOperator(int startIndex, int endIndex, int precedence)
    {
        TemplateExpressionOperatorMatch result = new TemplateExpressionOperatorMatch();
        int depth = 0;
        int index = endIndex;
        while (index >= startIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            depth = AdjustReverseDepth(depth, kind);
            if (depth == 0 && index > startIndex && index < endIndex)
            {
                TemplateExpressionOperatorKind operatorKind = GetBinaryOperatorKind(index, precedence);
                if (operatorKind != TemplateExpressionOperatorKind.None)
                {
                    result.Found = true;
                    result.StartIndex = GetBinaryOperatorStartIndex(index, operatorKind);
                    result.EndIndex = GetBinaryOperatorEndIndex(index, operatorKind);
                    result.OperatorKind = operatorKind;
                    return result;
                }
            }

            index = index - 1;
        }

        return result;
    }

    internal TemplateExpressionOperatorMatch FindTopLevelLiquidBinaryOperator(int startIndex, int endIndex)
    {
        TemplateExpressionOperatorMatch result = new TemplateExpressionOperatorMatch();
        int depth = 0;
        int index = startIndex;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (depth == 0 && index > startIndex && index < endIndex && !IsWordOperatorHyphen(index, startIndex, endIndex))
            {
                int precedence = 1;
                while (precedence <= 6)
                {
                    TemplateExpressionOperatorKind operatorKind = GetBinaryOperatorKind(index, precedence);
                    if (operatorKind != TemplateExpressionOperatorKind.None)
                    {
                        int operatorStart = GetBinaryOperatorStartIndex(index, operatorKind);
                        int operatorEnd = GetBinaryOperatorEndIndex(index, operatorKind);
                        if (operatorStart > startIndex && operatorEnd < endIndex)
                        {
                            result.Found = true;
                            result.StartIndex = operatorStart;
                            result.EndIndex = operatorEnd;
                            result.OperatorKind = operatorKind;
                            return result;
                        }
                    }

                    precedence = precedence + 1;
                }
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return result;
    }

    internal bool IsWordOperatorHyphen(int index, int startIndex, int endIndex)
    {
        if (!IsOperatorText(index, "-") || index - 1 < startIndex || index + 1 > endIndex)
        {
            return false;
        }

        if (!IsIdentifierText(index + 1, "with"))
        {
            return false;
        }

        return IsIdentifierText(index - 1, "starts") || IsIdentifierText(index - 1, "ends");
    }

    internal int GetBinaryOperatorStartIndex(int index, TemplateExpressionOperatorKind operatorKind)
    {
        if ((operatorKind == TemplateExpressionOperatorKind.StartsWith || operatorKind == TemplateExpressionOperatorKind.EndsWith) && IsIdentifierText(index, "with"))
        {
            if (index - 1 >= 0 && IsOperatorText(index - 1, "-"))
            {
                return index - 2;
            }

            return index - 1;
        }

        return index;
    }

    internal int GetBinaryOperatorEndIndex(int index, TemplateExpressionOperatorKind operatorKind)
    {
        if (operatorKind == TemplateExpressionOperatorKind.Or && Tokens[index].Kind == TemplateTokenKind.Pipe)
        {
            return index + 1;
        }

        return index;
    }

    internal TemplateExpressionOperatorKind GetBinaryOperatorKind(int index, int precedence)
    {
        if (precedence == 1)
        {
            if (IsIdentifierText(index, "or") || IsOperatorText(index, "||") || IsPipePipeOperator(index))
            {
                return TemplateExpressionOperatorKind.Or;
            }
        }
        else if (precedence == 2)
        {
            if (IsIdentifierText(index, "and") || IsOperatorText(index, "&&"))
            {
                return TemplateExpressionOperatorKind.And;
            }
        }
        else if (precedence == 3)
        {
            if (IsOperatorText(index, "=="))
            {
                return TemplateExpressionOperatorKind.Equal;
            }

            if (IsOperatorText(index, "!="))
            {
                return TemplateExpressionOperatorKind.NotEqual;
            }
        }
        else if (precedence == 4)
        {
            if (IsIdentifierText(index, "contains"))
            {
                return TemplateExpressionOperatorKind.Contains;
            }

            if (IsIdentifierText(index, "starts_with"))
            {
                return TemplateExpressionOperatorKind.StartsWith;
            }

            if (IsIdentifierText(index, "ends_with"))
            {
                return TemplateExpressionOperatorKind.EndsWith;
            }

            if (IsIdentifierText(index, "with") && index - 1 >= 0)
            {
                if (IsIdentifierText(index - 1, "starts") || (index - 2 >= 0 && IsOperatorText(index - 1, "-") && IsIdentifierText(index - 2, "starts")))
                {
                    return TemplateExpressionOperatorKind.StartsWith;
                }

                if (IsIdentifierText(index - 1, "ends") || (index - 2 >= 0 && IsOperatorText(index - 1, "-") && IsIdentifierText(index - 2, "ends")))
                {
                    return TemplateExpressionOperatorKind.EndsWith;
                }
            }

            if (IsOperatorText(index, "<"))
            {
                return TemplateExpressionOperatorKind.LessThan;
            }

            if (IsOperatorText(index, "<="))
            {
                return TemplateExpressionOperatorKind.LessThanOrEqual;
            }

            if (IsOperatorText(index, ">"))
            {
                return TemplateExpressionOperatorKind.GreaterThan;
            }

            if (IsOperatorText(index, ">="))
            {
                return TemplateExpressionOperatorKind.GreaterThanOrEqual;
            }
        }
        else if (precedence == 5)
        {
            if (IsOperatorText(index, "+"))
            {
                return TemplateExpressionOperatorKind.Add;
            }

            if (IsOperatorText(index, "-"))
            {
                return TemplateExpressionOperatorKind.Subtract;
            }
        }
        else if (precedence == 6)
        {
            if (IsOperatorText(index, "*"))
            {
                return TemplateExpressionOperatorKind.Multiply;
            }

            if (IsOperatorText(index, "/"))
            {
                if (IsParentSlash(index))
                {
                    return TemplateExpressionOperatorKind.None;
                }

                return TemplateExpressionOperatorKind.Divide;
            }

            if (IsOperatorText(index, "%"))
            {
                return TemplateExpressionOperatorKind.Modulo;
            }
        }

        return TemplateExpressionOperatorKind.None;
    }

    internal TemplateExpressionOperatorKind GetUnaryOperatorKind(int index)
    {
        if (IsIdentifierText(index, "not") || IsOperatorText(index, "!"))
        {
            return TemplateExpressionOperatorKind.UnaryNot;
        }

        if (IsOperatorText(index, "-"))
        {
            return TemplateExpressionOperatorKind.UnaryNegate;
        }

        if (IsOperatorText(index, "+"))
        {
            return TemplateExpressionOperatorKind.UnaryPlus;
        }

        return TemplateExpressionOperatorKind.None;
    }

    internal bool IsPipePipeOperator(int index)
    {
        if (index + 1 >= Tokens.Count)
        {
            return false;
        }

        return Tokens[index].Kind == TemplateTokenKind.Pipe && Tokens[index + 1].Kind == TemplateTokenKind.Pipe;
    }

    internal bool IsParentSlash(int index)
    {
        if (index - 2 < 0)
        {
            return false;
        }

        return Tokens[index].Kind == TemplateTokenKind.Operator
            && Tokens[index].Text == "/"
            && Tokens[index - 1].Kind == TemplateTokenKind.Dot
            && Tokens[index - 2].Kind == TemplateTokenKind.Dot;
    }

    internal TemplateExpressionOperatorMatch FindTopLevelTestOperator(int startIndex, int endIndex)
    {
        TemplateExpressionOperatorMatch result = new TemplateExpressionOperatorMatch();
        int depth = 0;
        int index = startIndex + 1;
        while (index < endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (depth == 0 && IsIdentifierText(index, "is"))
            {
                result.Found = true;
                result.StartIndex = index;
                result.EndIndex = index;
                return result;
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return result;
    }

    internal int FindTopLevelFilterPipe(int startIndex, int endIndex)
    {
        int depth = 0;
        int index = endIndex;
        while (index > startIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            depth = AdjustReverseDepth(depth, kind);
            if (depth == 0 && Tokens[index].Kind == TemplateTokenKind.Pipe && index < endIndex)
            {
                if (!IsPipePipeAtOrBefore(index))
                {
                    return index;
                }
            }

            index = index - 1;
        }

        return -1;
    }

    internal bool IsPipePipeAtOrBefore(int index)
    {
        if (index + 1 < Tokens.Count && Tokens[index + 1].Kind == TemplateTokenKind.Pipe)
        {
            return true;
        }

        if (index - 1 >= 0 && Tokens[index - 1].Kind == TemplateTokenKind.Pipe)
        {
            return true;
        }

        return false;
    }

    internal bool LooksInvalidExpression(int startIndex, int endIndex)
    {
        TemplateTokenKind firstKind = Tokens[startIndex].Kind;
        TemplateTokenKind lastKind = Tokens[endIndex].Kind;
        if (firstKind == TemplateTokenKind.CloseParen
            || firstKind == TemplateTokenKind.CloseBracket
            || firstKind == TemplateTokenKind.CloseBrace
            || firstKind == TemplateTokenKind.Comma
            || firstKind == TemplateTokenKind.Colon
            || firstKind == TemplateTokenKind.Pipe)
        {
            return true;
        }

        if (lastKind == TemplateTokenKind.Operator
            || lastKind == TemplateTokenKind.Dot
            || lastKind == TemplateTokenKind.Question
            || lastKind == TemplateTokenKind.Colon
            || lastKind == TemplateTokenKind.Comma
            || lastKind == TemplateTokenKind.Pipe
            || lastKind == TemplateTokenKind.OpenParen
            || lastKind == TemplateTokenKind.OpenBracket
            || lastKind == TemplateTokenKind.OpenBrace)
        {
            return true;
        }

        return false;
    }

    internal TemplateExpressionSyntax CreateFilterExpression(int startIndex, int endIndex, int pipeIndex)
    {
        TemplateExpressionSyntax input = CreateExpression(startIndex, pipeIndex - 1);
        int filterStartIndex = pipeIndex + 1;
        string name = "";
        if (filterStartIndex <= endIndex && Tokens[filterStartIndex].Kind == TemplateTokenKind.Identifier)
        {
            name = Tokens[filterStartIndex].Text;
        }

        TemplateExpressionSyntax expression = CreateCallExpression(startIndex, endIndex, TemplateExpressionCallKind.Filter, name);
        expression.AddArgument(input);
        if (filterStartIndex + 1 <= endIndex
            && Tokens[filterStartIndex + 1].Kind == TemplateTokenKind.OpenParen
            && IsDelimitedExpression(filterStartIndex + 1, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen))
        {
            AddCommaSeparatedCallArguments(expression, filterStartIndex + 2, endIndex - 1);
        }
        else if (filterStartIndex + 1 <= endIndex)
        {
            AddWhitespaceSeparatedCallArguments(expression, filterStartIndex + 1, endIndex);
        }

        return expression;
    }

    internal TemplateExpressionSyntax CreateTestExpression(int startIndex, int endIndex, int isIndex)
    {
        int nameIndex = isIndex + 1;
        string name = "";
        bool negated = false;
        if (nameIndex <= endIndex && IsIdentifierText(nameIndex, "not"))
        {
            negated = true;
            nameIndex = nameIndex + 1;
        }

        if (nameIndex <= endIndex && IsTestNameToken(Tokens[nameIndex].Kind))
        {
            string testName = Tokens[nameIndex].Text;
            if (nameIndex + 1 <= endIndex && IsCompoundTestName(testName, Tokens[nameIndex + 1].Text))
            {
                nameIndex = nameIndex + 1;
                testName = testName + " " + Tokens[nameIndex].Text;
            }

            if (negated)
            {
                name = "not " + testName;
            }
            else
            {
                name = testName;
            }
        }

        TemplateExpressionSyntax expression = CreateCallExpression(startIndex, endIndex, TemplateExpressionCallKind.Test, name);
        expression.AddArgument(CreateExpression(startIndex, isIndex - 1));
        if (nameIndex + 1 <= endIndex
            && Tokens[nameIndex + 1].Kind == TemplateTokenKind.OpenParen
            && IsDelimitedExpression(nameIndex + 1, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen))
        {
            AddCommaSeparatedCallArguments(expression, nameIndex + 2, endIndex - 1);
        }
        else if (nameIndex + 1 <= endIndex)
        {
            AddWhitespaceSeparatedCallArguments(expression, nameIndex + 1, endIndex);
        }

        return expression;
    }

    internal bool IsTestNameToken(TemplateTokenKind kind)
    {
        return kind == TemplateTokenKind.Identifier
            || kind == TemplateTokenKind.NullLiteral
            || kind == TemplateTokenKind.EmptyLiteral
            || kind == TemplateTokenKind.BooleanLiteral;
    }

    internal bool IsCompoundTestName(string first, string second)
    {
        if (first == "divisible" && second == "by")
        {
            return true;
        }

        if (first == "equal" && second == "to")
        {
            return true;
        }

        return false;
    }

    internal TemplateExpressionSyntax? TryCreateFunctionCallExpression(int startIndex, int endIndex)
    {
        if (startIndex + 2 > endIndex)
        {
            return null;
        }

        if (Tokens[startIndex].Kind != TemplateTokenKind.Identifier)
        {
            return null;
        }

        if (Tokens[startIndex + 1].Kind != TemplateTokenKind.OpenParen)
        {
            return null;
        }

        if (!IsDelimitedExpression(startIndex + 1, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen))
        {
            return null;
        }

        TemplateExpressionSyntax expression = CreateCallExpression(startIndex, endIndex, TemplateExpressionCallKind.Function, Tokens[startIndex].Text);
        AddCommaSeparatedCallArguments(expression, startIndex + 2, endIndex - 1);
        return expression;
    }

    internal TemplateExpressionSyntax? TryCreateWhitespaceCallExpression(int startIndex, int endIndex)
    {
        if (startIndex + 1 > endIndex || Tokens[startIndex].Kind != TemplateTokenKind.Identifier)
        {
            return null;
        }

        TemplateTokenKind secondKind = Tokens[startIndex + 1].Kind;
        if (secondKind == TemplateTokenKind.Dot
            || secondKind == TemplateTokenKind.OpenBracket
            || secondKind == TemplateTokenKind.Question
            || secondKind == TemplateTokenKind.Operator
            || secondKind == TemplateTokenKind.Pipe
            || secondKind == TemplateTokenKind.Colon
            || secondKind == TemplateTokenKind.Equals)
        {
            return null;
        }

        TemplateExpressionSyntax expression = CreateCallExpression(startIndex, endIndex, TemplateExpressionCallKind.Subexpression, Tokens[startIndex].Text);
        AddWhitespaceSeparatedCallArguments(expression, startIndex + 1, endIndex);
        return expression;
    }

    internal TemplateExpressionSyntax? TryCreateSubexpression(int startIndex, int endIndex)
    {
        if (!IsDelimitedExpression(startIndex, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen))
        {
            return null;
        }

        if (startIndex + 1 > endIndex - 1 || Tokens[startIndex + 1].Kind != TemplateTokenKind.Identifier)
        {
            return null;
        }

        if (startIndex + 2 > endIndex - 1)
        {
            return null;
        }

        if (Tokens[startIndex + 2].Kind == TemplateTokenKind.Operator
            || Tokens[startIndex + 2].Kind == TemplateTokenKind.Question
            || Tokens[startIndex + 2].Kind == TemplateTokenKind.Colon
            || Tokens[startIndex + 2].Kind == TemplateTokenKind.Pipe)
        {
            return null;
        }

        TemplateExpressionSyntax expression = CreateCallExpression(startIndex, endIndex, TemplateExpressionCallKind.Subexpression, Tokens[startIndex + 1].Text);
        AddWhitespaceSeparatedCallArguments(expression, startIndex + 2, endIndex - 1);
        return expression;
    }

    internal void AddCommaSeparatedCallArguments(TemplateExpressionSyntax expression, int startIndex, int endIndex)
    {
        int segmentStart = startIndex;
        int index = startIndex;
        int depth = 0;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == TemplateTokenKind.Comma && depth == 0)
            {
                if (segmentStart <= index - 1)
                {
                    AddCallArgument(expression, segmentStart, index - 1, false);
                }

                segmentStart = index + 1;
            }
            else
            {
                depth = AdjustDepth(depth, kind);
            }

            index = index + 1;
        }

        if (segmentStart <= endIndex)
        {
            AddCallArgument(expression, segmentStart, endIndex, false);
        }
    }

    internal void AddWhitespaceSeparatedCallArguments(TemplateExpressionSyntax expression, int startIndex, int endIndex)
    {
        int index = startIndex;
        while (index <= endIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Comma)
            {
                index = index + 1;
            }
            else
            {
                int argumentEndIndex;
                if (IsNamedArgumentStart(index, endIndex))
                {
                    argumentEndIndex = FindNamedArgumentValueEnd(index + 2, endIndex);
                    if (argumentEndIndex < index + 2)
                    {
                        argumentEndIndex = index + 1;
                    }

                    AddCallArgument(expression, index, argumentEndIndex, true);
                }
                else
                {
                    argumentEndIndex = FindExpressionArgumentEnd(index, endIndex);
                    expression.AddArgument(CreateExpression(index, argumentEndIndex));
                }

                index = argumentEndIndex + 1;
            }
        }
    }

    internal void AddCallArgument(TemplateExpressionSyntax expression, int startIndex, int endIndex, bool hash)
    {
        if (!IsNamedArgumentStart(startIndex, endIndex))
        {
            expression.AddArgument(CreateExpression(startIndex, endIndex));
            return;
        }

        int valueStartIndex = startIndex + 2;
        TemplateExpressionSyntax value;
        TemplateSourceSpan argumentSpan;
        if (valueStartIndex > endIndex)
        {
            TemplateToken equalsToken = Tokens[startIndex + 1];
            TemplateSourceSpan missingSpan = Source.CreateSpan(equalsToken.Span.End, 0);
            value = new TemplateExpressionSyntax(missingSpan, TemplateExpressionSyntaxKind.Unknown, "");
            argumentSpan = CreateSpanBetween(Tokens[startIndex].Span.Start, equalsToken.Span.End);
            AddExpressionRecovery(expression, "PARSE008", "Named argument '" + Tokens[startIndex].Text + "' is missing a value.", argumentSpan, "Add an expression after the equals delimiter.");
        }
        else
        {
            value = CreateExpression(valueStartIndex, endIndex);
            argumentSpan = CreateSpanBetween(Tokens[startIndex].Span.Start, Tokens[endIndex].Span.End);
        }

        TemplateNamedArgumentSyntax argument = new TemplateNamedArgumentSyntax(argumentSpan, Tokens[startIndex].Span, Tokens[startIndex + 1].Span, Tokens[startIndex].Text, value, hash);
        if (HasArgumentName(expression.NamedArguments, expression.HashArguments, argument.Name))
        {
            AddExpressionRecovery(expression, "PARSE012", "Duplicate named argument '" + argument.Name + "'.", argument.NameSpan, "Remove or rename the duplicate argument.");
        }

        if (hash)
        {
            expression.AddHashArgument(argument);
        }
        else
        {
            expression.AddNamedArgument(argument);
        }
    }

    internal void AddExpressionRecovery(TemplateExpressionSyntax expression, string code, string message, TemplateSourceSpan span, string hint)
    {
        TemplateParseResult? result = ActiveResult;
        if (result == null)
        {
            return;
        }

        TemplateDiagnostic diagnostic = AddDiagnostic(result, code, message, span, hint);
        expression.AddRecoveryNode(CreateRecoveryError(span, diagnostic));
    }

    internal int FindStatementArgumentEnd(int startIndex, int endIndex)
    {
        return FindExpressionArgumentEnd(startIndex, endIndex);
    }

    internal int FindExpressionArgumentEnd(int startIndex, int endIndex)
    {
        TemplateTokenKind kind = Tokens[startIndex].Kind;
        if (kind == TemplateTokenKind.OpenParen)
        {
            return FindDelimitedArgumentEnd(startIndex, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen);
        }

        if (kind == TemplateTokenKind.OpenBracket)
        {
            return FindDelimitedArgumentEnd(startIndex, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
        }

        if (kind == TemplateTokenKind.OpenBrace)
        {
            return FindDelimitedArgumentEnd(startIndex, endIndex, TemplateTokenKind.OpenBrace, TemplateTokenKind.CloseBrace);
        }

        int rangeOperatorIndex = FindImmediateRangeOperator(startIndex, endIndex);
        if (rangeOperatorIndex >= 0)
        {
            return FindRangeArgumentEnd(rangeOperatorIndex + 2, endIndex);
        }

        int functionCallEndIndex = FindFunctionCallArgumentEnd(startIndex, endIndex);
        if (functionCallEndIndex > startIndex)
        {
            return functionCallEndIndex;
        }

        int pathEndIndex = FindPathArgumentEnd(startIndex, endIndex);
        return pathEndIndex;
    }

    internal int FindFunctionCallArgumentEnd(int startIndex, int endIndex)
    {
        if (startIndex + 2 > endIndex)
        {
            return startIndex;
        }

        if (Tokens[startIndex].Kind != TemplateTokenKind.Identifier || Tokens[startIndex + 1].Kind != TemplateTokenKind.OpenParen)
        {
            return startIndex;
        }

        return FindDelimitedArgumentEnd(startIndex + 1, endIndex, TemplateTokenKind.OpenParen, TemplateTokenKind.CloseParen);
    }

    internal int FindDelimitedArgumentEnd(int startIndex, int endIndex, TemplateTokenKind openKind, TemplateTokenKind closeKind)
    {
        int matchingIndex = FindMatchingDelimiter(startIndex, endIndex, openKind, closeKind);
        if (matchingIndex >= 0)
        {
            return matchingIndex;
        }

        return endIndex;
    }

    internal int FindRangeArgumentEnd(int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
        {
            return endIndex;
        }

        TemplateTokenKind kind = Tokens[startIndex].Kind;
        if (kind == TemplateTokenKind.OpenBracket)
        {
            return FindDelimitedArgumentEnd(startIndex, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
        }

        if (kind == TemplateTokenKind.OpenBrace)
        {
            return FindDelimitedArgumentEnd(startIndex, endIndex, TemplateTokenKind.OpenBrace, TemplateTokenKind.CloseBrace);
        }

        return startIndex;
    }

    internal int FindImmediateRangeOperator(int startIndex, int endIndex)
    {
        if (startIndex + 2 > endIndex)
        {
            return -1;
        }

        if (Tokens[startIndex + 1].Kind == TemplateTokenKind.Dot && Tokens[startIndex + 2].Kind == TemplateTokenKind.Dot)
        {
            return startIndex + 1;
        }

        return -1;
    }

    internal int FindPathArgumentEnd(int startIndex, int endIndex)
    {
        int index = startIndex;
        int lastValidIndex = startIndex;
        bool allowImmediateName = false;

        if (IsOperatorText(startIndex, "$") || IsOperatorText(startIndex, "@"))
        {
            index = startIndex + 1;
            lastValidIndex = startIndex;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsParentPrefix(startIndex, endIndex))
        {
            index = startIndex + 2;
            lastValidIndex = startIndex + 1;
            allowImmediateName = true;
            if (index <= endIndex && IsOperatorText(index, "/"))
            {
                index = index + 1;
            }

            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (Tokens[startIndex].Kind == TemplateTokenKind.Dot)
        {
            index = startIndex + 1;
            lastValidIndex = startIndex;
            allowImmediateName = true;
        }
        else if (IsIdentifierText(startIndex, "root")
            || IsIdentifierText(startIndex, "this")
            || IsIdentifierText(startIndex, "current")
            || IsIdentifierText(startIndex, "parent")
            || IsIdentifierText(startIndex, "local"))
        {
            index = startIndex + 1;
            lastValidIndex = startIndex;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (Tokens[startIndex].Kind == TemplateTokenKind.Identifier)
        {
            index = startIndex + 1;
            lastValidIndex = startIndex;
        }
        else
        {
            return startIndex;
        }

        while (index <= endIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Identifier && allowImmediateName)
            {
                lastValidIndex = index;
                index = index + 1;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Dot && index + 1 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.Identifier)
            {
                lastValidIndex = index + 1;
                index = index + 2;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Question && index + 2 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.Dot && Tokens[index + 2].Kind == TemplateTokenKind.Identifier)
            {
                lastValidIndex = index + 2;
                index = index + 3;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.OpenBracket)
            {
                int closeIndex = FindMatchingDelimiter(index, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
                if (closeIndex < 0)
                {
                    return lastValidIndex;
                }

                lastValidIndex = closeIndex;
                index = closeIndex + 1;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Question && index + 1 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.OpenBracket)
            {
                int closeIndex = FindMatchingDelimiter(index + 1, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
                if (closeIndex < 0)
                {
                    return lastValidIndex;
                }

                lastValidIndex = closeIndex;
                index = closeIndex + 1;
                allowImmediateName = false;
            }
            else
            {
                return lastValidIndex;
            }
        }

        return lastValidIndex;
    }

    internal TemplateExpressionSyntax? TryCreatePathExpression(int startIndex, int endIndex)
    {
        if (startIndex > endIndex)
        {
            return null;
        }

        TemplatePathSyntaxKind pathKind = TemplatePathSyntaxKind.Variable;
        TemplateExpressionSyntaxKind expressionKind = TemplateExpressionSyntaxKind.Path;
        int index = startIndex;
        bool allowImmediateName = false;

        if (IsOperatorText(startIndex, "$"))
        {
            pathKind = TemplatePathSyntaxKind.Root;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsOperatorText(startIndex, "@"))
        {
            pathKind = TemplatePathSyntaxKind.Local;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsParentPrefix(startIndex, endIndex))
        {
            pathKind = TemplatePathSyntaxKind.Parent;
            index = index + 2;
            allowImmediateName = true;
            if (index <= endIndex && IsOperatorText(index, "/"))
            {
                index = index + 1;
            }

            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (Tokens[startIndex].Kind == TemplateTokenKind.Dot)
        {
            pathKind = TemplatePathSyntaxKind.Current;
            index = index + 1;
            allowImmediateName = true;
        }
        else if (IsIdentifierText(startIndex, "root"))
        {
            pathKind = TemplatePathSyntaxKind.Root;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsIdentifierText(startIndex, "this") || IsIdentifierText(startIndex, "current"))
        {
            pathKind = TemplatePathSyntaxKind.Current;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsIdentifierText(startIndex, "parent"))
        {
            pathKind = TemplatePathSyntaxKind.Parent;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (IsIdentifierText(startIndex, "local"))
        {
            pathKind = TemplatePathSyntaxKind.Local;
            index = index + 1;
            allowImmediateName = true;
            index = ConsumeOptionalDot(index, endIndex);
        }
        else if (Tokens[startIndex].Kind == TemplateTokenKind.Identifier)
        {
            expressionKind = TemplateExpressionSyntaxKind.Variable;
        }
        else
        {
            return null;
        }

        TemplateToken first = Tokens[startIndex];
        TemplateToken last = Tokens[endIndex];
        TemplateSourceSpan span = CreateSpanBetween(first.Span.Start, last.Span.End);
        TemplateExpressionSyntax expression = TemplateExpressionSyntax.Create(span, expressionKind, pathKind, JoinTokenText(startIndex, endIndex));

        if (expressionKind == TemplateExpressionSyntaxKind.Variable)
        {
            TemplatePathSegmentSyntax firstSegment = new TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind.Name, Tokens[index].Text);
            firstSegment.SetSpan(Tokens[index].Span);
            expression.AddPathSegment(firstSegment);
            index = index + 1;
        }

        while (index <= endIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Identifier && allowImmediateName)
            {
                TemplatePathSegmentSyntax immediateSegment = new TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind.Name, Tokens[index].Text);
                immediateSegment.SetSpan(Tokens[index].Span);
                expression.AddPathSegment(immediateSegment);
                index = index + 1;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Dot)
            {
                if (index + 1 > endIndex || Tokens[index + 1].Kind != TemplateTokenKind.Identifier)
                {
                    return null;
                }

                expression.ExpressionKind = TemplateExpressionSyntaxKind.Path;
                TemplatePathSegmentSyntax nameSegment = new TemplatePathSegmentSyntax(TemplatePathSegmentSyntaxKind.Name, Tokens[index + 1].Text);
                nameSegment.SetSpan(Tokens[index + 1].Span);
                expression.AddPathSegment(nameSegment);
                index = index + 2;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Question && index + 2 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.Dot)
            {
                if (Tokens[index + 2].Kind != TemplateTokenKind.Identifier)
                {
                    return null;
                }

                expression.ExpressionKind = TemplateExpressionSyntaxKind.Path;
                TemplatePathSegmentSyntax nullSafeNameSegment = TemplatePathSegmentSyntax.Create(TemplatePathSegmentSyntaxKind.Name, Tokens[index + 2].Text, true);
                nullSafeNameSegment.SetSpan(CreateSpanBetween(Tokens[index].Span.Start, Tokens[index + 2].Span.End));
                expression.AddPathSegment(nullSafeNameSegment);
                index = index + 3;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.OpenBracket)
            {
                int closeIndex = FindMatchingDelimiter(index, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
                if (closeIndex < 0 || closeIndex > endIndex)
                {
                    return null;
                }

                TemplateExpressionSyntax indexExpression = CreateExpression(index + 1, closeIndex - 1);
                TemplatePathSegmentSyntax segment = TemplatePathSegmentSyntax.Create(TemplatePathSegmentSyntaxKind.Indexer, JoinTokenText(index, closeIndex), indexExpression);
                segment.SetSpan(CreateSpanBetween(Tokens[index].Span.Start, Tokens[closeIndex].Span.End));
                expression.ExpressionKind = TemplateExpressionSyntaxKind.Path;
                expression.AddPathSegment(segment);
                index = closeIndex + 1;
                allowImmediateName = false;
            }
            else if (Tokens[index].Kind == TemplateTokenKind.Question && index + 1 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.OpenBracket)
            {
                int closeIndex = FindMatchingDelimiter(index + 1, endIndex, TemplateTokenKind.OpenBracket, TemplateTokenKind.CloseBracket);
                if (closeIndex < 0 || closeIndex > endIndex)
                {
                    return null;
                }

                TemplateExpressionSyntax indexExpression = CreateExpression(index + 2, closeIndex - 1);
                TemplatePathSegmentSyntax segment = TemplatePathSegmentSyntax.Create(TemplatePathSegmentSyntaxKind.Indexer, JoinTokenText(index, closeIndex), indexExpression, true);
                segment.SetSpan(CreateSpanBetween(Tokens[index].Span.Start, Tokens[closeIndex].Span.End));
                expression.ExpressionKind = TemplateExpressionSyntaxKind.Path;
                expression.AddPathSegment(segment);
                index = closeIndex + 1;
                allowImmediateName = false;
            }
            else
            {
                return null;
            }
        }

        return expression;
    }

    internal int ConsumeOptionalDot(int index, int endIndex)
    {
        if (index <= endIndex && Tokens[index].Kind == TemplateTokenKind.Dot)
        {
            return index + 1;
        }

        return index;
    }

    internal bool IsParentPrefix(int startIndex, int endIndex)
    {
        if (startIndex + 1 > endIndex)
        {
            return false;
        }

        return Tokens[startIndex].Kind == TemplateTokenKind.Dot && Tokens[startIndex + 1].Kind == TemplateTokenKind.Dot;
    }

    internal bool IsOperatorText(int index, string text)
    {
        return Tokens[index].Kind == TemplateTokenKind.Operator && Tokens[index].Text == text;
    }

    internal bool IsIdentifierText(int index, string text)
    {
        return Tokens[index].Kind == TemplateTokenKind.Identifier && Tokens[index].Text == text;
    }

    internal bool IsDelimitedExpression(int startIndex, int endIndex, TemplateTokenKind openKind, TemplateTokenKind closeKind)
    {
        if (Tokens[startIndex].Kind != openKind || Tokens[endIndex].Kind != closeKind)
        {
            return false;
        }

        return FindMatchingDelimiter(startIndex, endIndex, openKind, closeKind) == endIndex;
    }

    internal int FindMatchingDelimiter(int startIndex, int endIndex, TemplateTokenKind openKind, TemplateTokenKind closeKind)
    {
        int depth = 0;
        int index = startIndex;
        while (index <= endIndex)
        {
            if (Tokens[index].Kind == openKind)
            {
                depth = depth + 1;
            }
            else if (Tokens[index].Kind == closeKind)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindTopLevelRangeOperator(int startIndex, int endIndex)
    {
        int depth = 0;
        int index = startIndex;
        while (index + 1 <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == TemplateTokenKind.Dot && depth == 0 && index > startIndex && index + 2 <= endIndex && Tokens[index + 1].Kind == TemplateTokenKind.Dot)
            {
                return index;
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return -1;
    }

    internal int FindTopLevelToken(int startIndex, int endIndex, TemplateTokenKind tokenKind)
    {
        int depth = 0;
        int index = startIndex;
        while (index <= endIndex)
        {
            TemplateTokenKind kind = Tokens[index].Kind;
            if (kind == tokenKind && depth == 0)
            {
                return index;
            }

            depth = AdjustDepth(depth, kind);
            index = index + 1;
        }

        return -1;
    }

    internal int AdjustDepth(int depth, TemplateTokenKind kind)
    {
        if (kind == TemplateTokenKind.OpenBracket || kind == TemplateTokenKind.OpenBrace || kind == TemplateTokenKind.OpenParen)
        {
            return depth + 1;
        }

        if (kind == TemplateTokenKind.CloseBracket || kind == TemplateTokenKind.CloseBrace || kind == TemplateTokenKind.CloseParen)
        {
            if (depth > 0)
            {
                return depth - 1;
            }
        }

        return depth;
    }

    internal int AdjustReverseDepth(int depth, TemplateTokenKind kind)
    {
        if (kind == TemplateTokenKind.CloseBracket || kind == TemplateTokenKind.CloseBrace || kind == TemplateTokenKind.CloseParen)
        {
            return depth + 1;
        }

        if (kind == TemplateTokenKind.OpenBracket || kind == TemplateTokenKind.OpenBrace || kind == TemplateTokenKind.OpenParen)
        {
            if (depth > 0)
            {
                return depth - 1;
            }
        }

        return depth;
    }

    internal TemplateExpressionSyntaxKind InferExpressionKind(int startIndex, int endIndex)
    {
        if (startIndex == endIndex)
        {
            return InferSingleTokenExpressionKind(Tokens[startIndex].Kind);
        }

        int index = startIndex;
        while (index <= endIndex)
        {
            if (Tokens[index].Kind == TemplateTokenKind.Dot)
            {
                return TemplateExpressionSyntaxKind.Path;
            }

            index = index + 1;
        }

        return TemplateExpressionSyntaxKind.Unknown;
    }

    internal TemplateExpressionSyntaxKind InferSingleTokenExpressionKind(TemplateTokenKind kind)
    {
        if (kind == TemplateTokenKind.Identifier)
        {
            return TemplateExpressionSyntaxKind.Variable;
        }

        if (TemplateTokenKindFacts.IsLiteral(kind))
        {
            return TemplateExpressionSyntaxKind.Literal;
        }

        return TemplateExpressionSyntaxKind.Unknown;
    }

    internal TemplateLiteralSyntaxKind InferSingleTokenLiteralKind(TemplateTokenKind kind)
    {
        if (kind == TemplateTokenKind.StringLiteral)
        {
            return TemplateLiteralSyntaxKind.String;
        }

        if (kind == TemplateTokenKind.NumberLiteral)
        {
            return TemplateLiteralSyntaxKind.Number;
        }

        if (kind == TemplateTokenKind.BooleanLiteral)
        {
            return TemplateLiteralSyntaxKind.Boolean;
        }

        if (kind == TemplateTokenKind.NullLiteral)
        {
            return TemplateLiteralSyntaxKind.Null;
        }

        if (kind == TemplateTokenKind.EmptyLiteral)
        {
            return TemplateLiteralSyntaxKind.Empty;
        }

        return TemplateLiteralSyntaxKind.None;
    }

    internal string JoinTokenText(int startIndex, int endIndex)
    {
        StringBuilder builder = new StringBuilder();
        int index = startIndex;
        while (index <= endIndex)
        {
            builder.Append(Tokens[index].Text);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal int FindTagEnd(TemplateTokenKind endKind)
    {
        int index = Position;
        while (index < Tokens.Count)
        {
            if (Tokens[index].Kind == endKind)
            {
                return index;
            }

            if (Tokens[index].Kind == TemplateTokenKind.EndOfFile)
            {
                return index;
            }

            index = index + 1;
        }

        return Tokens.Count - 1;
    }

    internal int MovePastEnd(int endIndex)
    {
        if (endIndex < Tokens.Count && Tokens[endIndex].Kind != TemplateTokenKind.EndOfFile)
        {
            return endIndex + 1;
        }

        return endIndex;
    }

    internal TemplateToken TokenBeforeOrCurrentEnd(int endIndex, TemplateToken fallback)
    {
        if (endIndex < Tokens.Count && Tokens[endIndex].Kind != TemplateTokenKind.EndOfFile)
        {
            return Tokens[endIndex];
        }

        if (endIndex > 0)
        {
            return Tokens[endIndex - 1];
        }

        return fallback;
    }

    internal void SkipLeftTrimMarker()
    {
        if (!AtEnd() && Current().Kind == TemplateTokenKind.LeftTrimMarker)
        {
            Position = Position + 1;
        }
    }

    internal bool IsBlockStartName(string name)
    {
        return name == "if"
            || name == "unless"
            || name == "for"
            || name == "while"
            || name == "with"
            || name == "case"
            || name == "capture"
            || name == "liquidTableRow"
            || name == "block"
            || name == "macro"
            || name == "call"
            || name == "filter"
            || name == "mustacheSection"
            || name == "mustacheInverted"
            || name == "handlebarsBlock"
            || name == "handlebarsEach"
            || name == "handlebarsPartialBlock"
            || name == "handlebarsInlinePartial"
            || name == "jinjaAutoescape";
    }

    internal bool IsKnownStandaloneStatementName(string name)
    {
        return name == "set"
            || name == "assign"
            || name == "include"
            || name == "render"
            || name == "partial"
            || name == "import"
            || name == "extends"
            || name == "return"
            || name == "break"
            || name == "continue"
            || name == "increment"
            || name == "decrement"
            || name == "liquidCycle"
            || name == "liquidCycleNamed"
            || name == "liquidComment"
            || name == "liquidRenderFor"
            || name == "liquidIncludeFor"
            || name == "handlebarsPartialBlockBody";
    }

    internal bool IsEndStatementName(string name)
    {
        return name.Length > 3 && name.StartsWith("end");
    }

    internal string GetEndStatementName(string blockName)
    {
        return "end" + blockName;
    }

    internal bool IsBranchStatementName(string name)
    {
        return name == "else"
            || name == "elseif"
            || name == "when"
            || name == "empty";
    }

    internal bool IsAllowedBranchStatement(string blockName, string branchName)
    {
        if (blockName == "if" || blockName == "unless" || blockName == "with" || blockName == "handlebarsBlock")
        {
            return branchName == "else" || branchName == "elseif";
        }

        if (blockName == "for")
        {
            return branchName == "else" || branchName == "empty";
        }

        if (blockName == "handlebarsEach")
        {
            return branchName == "else";
        }

        if (blockName == "case")
        {
            return branchName == "else" || branchName == "when";
        }

        return false;
    }

    internal TemplateSourceSpan CreateSpanBetween(int start, int end)
    {
        if (end < start)
        {
            end = start;
        }

        return Source.CreateSpan(start, end - start);
    }

    internal void CopyLexerDiagnostics(TemplateParseResult result)
    {
        int index = 0;
        while (index < LexerResult.Diagnostics.Count)
        {
            TemplateDiagnostic diagnostic = LexerResult.Diagnostics[index];
            result.AddDiagnostic(diagnostic);
            ReportDiagnostic(diagnostic);
            index = index + 1;
        }
    }

    internal TemplateToken Current()
    {
        return Tokens[Position];
    }

    internal bool AtEnd()
    {
        if (Position >= Tokens.Count)
        {
            return true;
        }

        return Tokens[Position].Kind == TemplateTokenKind.EndOfFile;
    }
}
