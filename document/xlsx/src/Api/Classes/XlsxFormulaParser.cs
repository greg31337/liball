using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Parses Excel formula text into tokens and an expression tree that can be inspected or written again.
/// </summary>
public class XlsxFormulaParser : IXlsxFormulaParser
{
    /// <summary>
    /// Initializes a new formula parser with its default state.
    /// </summary>
    public XlsxFormulaParser()
    {
        Formula = "";
        Tokens = new List<XlsxFormulaToken>();
        Position = 0;
        Result = new XlsxFormulaParseResult();
    }

    internal string Formula;
    internal List<XlsxFormulaToken> Tokens;
    internal int Position;
    internal XlsxFormulaParseResult Result;
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxFormulaParseResult Parse(string formula)
    {
        Result = new XlsxFormulaParseResult();
        Formula = XlsxCellValueConverter.NormalizeText(formula);
        Result.Formula = Formula;
        Tokens = new List<XlsxFormulaToken>();
        Position = 0;
        XlsxFormulaTokenizeResult tokenizeResult = new XlsxFormulaTokenizer().Tokenize(formula);
        CopyTokenizerDiagnostics(Result, tokenizeResult);
        foreach (XlsxFormulaToken token in tokenizeResult.Tokens)
        {
            Result.Tokens.Add(token);
            if (token.Kind != XlsxFormulaTokenKind.Whitespace)
            {
                Tokens.Add(token);
            }
        }

        if (!tokenizeResult.IsValid)
        {
            return Result;
        }

        if (Tokens.Count == 0)
        {
            Result.AddError("formulaParser.formulaEmpty", "Formula text must not be empty.");
            return Result;
        }

        XlsxFormulaNode root = ParseExpression(0);
        if (root != null)
        {
            Result.Root = root;
            Result.HasRoot = true;
        }

        if (Position < Tokens.Count)
        {
            Result.AddError("formulaParser.unexpectedToken", "Formula contains an unexpected token.");
        }

        return Result;
    }

    internal XlsxFormulaNode ParseExpression(int minimumPrecedence)
    {
        XlsxFormulaNode? left = ParsePrefix();
        if (left == null)
        {
            return CreateUnknownNodeAtCurrent();
        }

        while (Position < Tokens.Count)
        {
            XlsxFormulaToken token = Tokens[Position];
            if (token.Kind == XlsxFormulaTokenKind.Percent)
            {
                if (6 < minimumPrecedence)
                {
                    break;
                }

                Position = Position + 1;
                left = CreateUnaryNode("%", left, token.Start, token.Start + token.Length);
                continue;
            }

            int precedence = GetBinaryPrecedence(token);
            if (precedence < minimumPrecedence)
            {
                break;
            }

            string op = token.Text;
            Position = Position + 1;
            int nextMinimum = precedence + 1;
            if (op == "^")
            {
                nextMinimum = precedence;
            }

            XlsxFormulaNode right = ParseExpression(nextMinimum);
            left = CreateBinaryNode(op, left, right);
        }

        return left;
    }

    internal XlsxFormulaNode? ParsePrefix()
    {
        if (Position >= Tokens.Count)
        {
            Result.AddError("formulaParser.operandMissing", "Formula operand is missing.");
            return null;
        }

        XlsxFormulaToken token = Tokens[Position];
        if (token.Kind == XlsxFormulaTokenKind.Operator && (token.Text == "+" || token.Text == "-" || token.Text == "@"))
        {
            Position = Position + 1;
            XlsxFormulaNode operand = ParseExpression(7);
            return CreateUnaryNode(token.Text, operand, token.Start, GetNodeEnd(operand));
        }

        return ParsePrimary();
    }

    internal XlsxFormulaNode ParsePrimary()
    {
        XlsxFormulaToken token = Tokens[Position];
        if (token.Kind == XlsxFormulaTokenKind.Number || token.Kind == XlsxFormulaTokenKind.String || token.Kind == XlsxFormulaTokenKind.Boolean || token.Kind == XlsxFormulaTokenKind.Error)
        {
            Position = Position + 1;
            return CreateTokenNode(XlsxFormulaNodeKind.Literal, token);
        }

        if (token.Kind == XlsxFormulaTokenKind.CellReference)
        {
            Position = Position + 1;
            return CreateTokenNode(XlsxFormulaNodeKind.Reference, token);
        }

        if (token.Kind == XlsxFormulaTokenKind.Identifier)
        {
            return ParseIdentifierPrimary();
        }

        if (token.Kind == XlsxFormulaTokenKind.SheetName || token.Kind == XlsxFormulaTokenKind.StructuredReference)
        {
            return ParseReferencePrefixPrimary();
        }

        if (token.Kind == XlsxFormulaTokenKind.OpenParen)
        {
            return ParseParenthesizedExpression();
        }

        if (token.Kind == XlsxFormulaTokenKind.ArrayOpen)
        {
            return ParseArrayConstant();
        }

        Result.AddError("formulaParser.unexpectedToken", "Formula contains an unexpected token.");
        Position = Position + 1;
        return CreateTokenNode(XlsxFormulaNodeKind.Unknown, token);
    }

    internal XlsxFormulaNode ParseIdentifierPrimary()
    {
        XlsxFormulaToken token = Tokens[Position];
        if (Position + 1 < Tokens.Count && Tokens[Position + 1].Kind == XlsxFormulaTokenKind.OpenParen)
        {
            return ParseFunctionCall();
        }

        if (Position + 1 < Tokens.Count && Tokens[Position + 1].Kind == XlsxFormulaTokenKind.StructuredReference)
        {
            XlsxFormulaToken structured = Tokens[Position + 1];
            Position = Position + 2;
            return CreateTextNode(XlsxFormulaNodeKind.StructuredReference, token.Start, structured.Start + structured.Length);
        }

        return ParseReferencePrefixPrimary();
    }

    internal XlsxFormulaNode ParseReferencePrefixPrimary()
    {
        int startPosition = Position;
        int start = Tokens[Position].Start;
        bool external = Tokens[Position].Kind == XlsxFormulaTokenKind.StructuredReference;
        while (Position < Tokens.Count && Tokens[Position].Kind != XlsxFormulaTokenKind.Bang)
        {
            if (Tokens[Position].Kind != XlsxFormulaTokenKind.Identifier && Tokens[Position].Kind != XlsxFormulaTokenKind.SheetName && Tokens[Position].Kind != XlsxFormulaTokenKind.StructuredReference)
            {
                break;
            }

            Position = Position + 1;
        }

        if (Position < Tokens.Count && Tokens[Position].Kind == XlsxFormulaTokenKind.Bang)
        {
            Position = Position + 1;
            if (Position < Tokens.Count && (Tokens[Position].Kind == XlsxFormulaTokenKind.CellReference || Tokens[Position].Kind == XlsxFormulaTokenKind.StructuredReference || Tokens[Position].Kind == XlsxFormulaTokenKind.Identifier))
            {
                XlsxFormulaToken target = Tokens[Position];
                Position = Position + 1;
                if (external)
                {
                    return CreateTextNode(XlsxFormulaNodeKind.ExternalReference, start, target.Start + target.Length);
                }

                return CreateTextNode(XlsxFormulaNodeKind.Reference, start, target.Start + target.Length);
            }

            Result.AddError("formulaParser.referenceTargetMissing", "Formula sheet or external reference target is missing.");
            return CreateTextNode(XlsxFormulaNodeKind.Reference, start, Tokens[Position - 1].Start + Tokens[Position - 1].Length);
        }

        Position = startPosition;
        XlsxFormulaToken token = Tokens[Position];
        Position = Position + 1;
        if (token.Kind == XlsxFormulaTokenKind.StructuredReference)
        {
            return CreateTokenNode(XlsxFormulaNodeKind.StructuredReference, token);
        }

        return CreateTokenNode(XlsxFormulaNodeKind.Name, token);
    }

    internal XlsxFormulaNode ParseFunctionCall()
    {
        XlsxFormulaToken name = Tokens[Position];
        Position = Position + 2;
        XlsxFormulaNode function = CreateTokenNode(XlsxFormulaNodeKind.FunctionCall, name);
        function.Operator = name.Text;
        while (Position < Tokens.Count && Tokens[Position].Kind != XlsxFormulaTokenKind.CloseParen)
        {
            XlsxFormulaNode argument = ParseExpression(0);
            if (argument != null)
            {
                function.Children.Add(argument);
            }

            if (Position < Tokens.Count && (Tokens[Position].Kind == XlsxFormulaTokenKind.UnionOperator || Tokens[Position].Kind == XlsxFormulaTokenKind.ArgumentSeparator))
            {
                Position = Position + 1;
                continue;
            }

            if (Position < Tokens.Count && Tokens[Position].Kind != XlsxFormulaTokenKind.CloseParen)
            {
                Result.AddError("formulaParser.argumentSeparatorMissing", "Formula function argument separator is missing.");
                break;
            }
        }

        if (Position < Tokens.Count && Tokens[Position].Kind == XlsxFormulaTokenKind.CloseParen)
        {
            XlsxFormulaToken close = Tokens[Position];
            Position = Position + 1;
            function.Length = close.Start + close.Length - function.Start;
            function.Text = Formula.Substring(function.Start, function.Length);
        }
        else
        {
            Result.AddError("formulaParser.closeParenMissing", "Formula function call is missing a closing parenthesis.");
            function.Length = GetNodeEnd(function) - function.Start;
            if (function.Length > 0)
            {
                function.Text = Formula.Substring(function.Start, function.Length);
            }
        }

        return function;
    }

    internal XlsxFormulaNode ParseParenthesizedExpression()
    {
        XlsxFormulaToken open = Tokens[Position];
        Position = Position + 1;
        XlsxFormulaNode expression = ParseExpression(0);
        if (Position < Tokens.Count && Tokens[Position].Kind == XlsxFormulaTokenKind.CloseParen)
        {
            Position = Position + 1;
            return expression;
        }

        Result.AddError("formulaParser.closeParenMissing", "Formula parenthesized expression is missing a closing parenthesis.");
        if (expression != null)
        {
            return expression;
        }

        return CreateTokenNode(XlsxFormulaNodeKind.Unknown, open);
    }

    internal XlsxFormulaNode ParseArrayConstant()
    {
        XlsxFormulaToken open = Tokens[Position];
        Position = Position + 1;
        XlsxFormulaNode array = CreateTokenNode(XlsxFormulaNodeKind.ArrayConstant, open);
        while (Position < Tokens.Count && Tokens[Position].Kind != XlsxFormulaTokenKind.ArrayClose)
        {
            XlsxFormulaNode item = ParseExpression(0);
            if (item != null)
            {
                array.Children.Add(item);
            }

            if (Position < Tokens.Count && (Tokens[Position].Kind == XlsxFormulaTokenKind.UnionOperator || Tokens[Position].Kind == XlsxFormulaTokenKind.ArgumentSeparator))
            {
                Position = Position + 1;
                continue;
            }

            if (Position < Tokens.Count && Tokens[Position].Kind != XlsxFormulaTokenKind.ArrayClose)
            {
                Result.AddError("formulaParser.arraySeparatorMissing", "Formula array constant item separator is missing.");
                break;
            }
        }

        if (Position < Tokens.Count && Tokens[Position].Kind == XlsxFormulaTokenKind.ArrayClose)
        {
            XlsxFormulaToken close = Tokens[Position];
            Position = Position + 1;
            array.Length = close.Start + close.Length - array.Start;
            array.Text = Formula.Substring(array.Start, array.Length);
        }
        else
        {
            Result.AddError("formulaParser.arrayCloseMissing", "Formula array constant is missing a closing brace.");
            array.Length = Formula.Length - array.Start;
            array.Text = Formula.Substring(array.Start, array.Length);
        }

        return array;
    }

    internal int GetBinaryPrecedence(XlsxFormulaToken token)
    {
        if (token.Kind == XlsxFormulaTokenKind.RangeOperator)
        {
            return 8;
        }

        if (token.Kind != XlsxFormulaTokenKind.Operator)
        {
            return -1;
        }

        if (token.Text == "=" || token.Text == "<>" || token.Text == "<=" || token.Text == ">=" || token.Text == "<" || token.Text == ">")
        {
            return 1;
        }

        if (token.Text == "&")
        {
            return 2;
        }

        if (token.Text == "+" || token.Text == "-")
        {
            return 3;
        }

        if (token.Text == "*" || token.Text == "/")
        {
            return 4;
        }

        if (token.Text == "^")
        {
            return 5;
        }

        return -1;
    }

    internal XlsxFormulaNode CreateTokenNode(XlsxFormulaNodeKind kind, XlsxFormulaToken token)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = kind;
        node.Text = token.Text;
        node.Start = token.Start;
        node.Length = token.Length;
        return node;
    }

    internal XlsxFormulaNode CreateTextNode(XlsxFormulaNodeKind kind, int start, int end)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = kind;
        node.Start = start;
        node.Length = end - start;
        node.Text = Formula.Substring(start, node.Length);
        return node;
    }

    internal XlsxFormulaNode CreateUnaryNode(string op, XlsxFormulaNode operand, int start, int end)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.UnaryOperation;
        node.Operator = op;
        node.Start = start;
        node.Length = end - start;
        if (node.Length > 0 && start >= 0 && start + node.Length <= Formula.Length)
        {
            node.Text = Formula.Substring(start, node.Length);
        }

        if (operand != null)
        {
            node.Children.Add(operand);
        }

        return node;
    }

    internal XlsxFormulaNode CreateBinaryNode(string op, XlsxFormulaNode left, XlsxFormulaNode right)
    {
        XlsxFormulaNode node = new XlsxFormulaNode();
        if (op == ":")
        {
            node.Kind = XlsxFormulaNodeKind.Range;
        }
        else
        {
            node.Kind = XlsxFormulaNodeKind.BinaryOperation;
        }

        node.Operator = op;
        node.Start = left.Start;
        node.Length = GetNodeEnd(right) - left.Start;
        node.Text = Formula.Substring(node.Start, node.Length);
        node.Children.Add(left);
        if (right != null)
        {
            node.Children.Add(right);
        }

        return node;
    }

    internal XlsxFormulaNode CreateUnknownNodeAtCurrent()
    {
        if (Position < Tokens.Count)
        {
            XlsxFormulaToken token = Tokens[Position];
            Position = Position + 1;
            return CreateTokenNode(XlsxFormulaNodeKind.Unknown, token);
        }

        XlsxFormulaNode node = new XlsxFormulaNode();
        node.Kind = XlsxFormulaNodeKind.Unknown;
        node.Start = Formula.Length;
        node.Length = 0;
        return node;
    }

    internal static int GetNodeEnd(XlsxFormulaNode node)
    {
        if (node == null)
        {
            return 0;
        }

        return node.Start + node.Length;
    }

    internal static void CopyTokenizerDiagnostics(XlsxFormulaParseResult result, XlsxFormulaTokenizeResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
