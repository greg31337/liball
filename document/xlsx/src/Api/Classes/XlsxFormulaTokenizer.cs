using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents formula tokenizer data used by the public workbook API.
/// </summary>
public class XlsxFormulaTokenizer : IXlsxFormulaTokenizer
{
    /// <summary>
    /// Initializes a new formula tokenizer with its default state.
    /// </summary>
    public XlsxFormulaTokenizer()
    {
    }

    /// <summary>
    /// Converts kenize to the requested OOXML-compatible representation.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxFormulaTokenizeResult Tokenize(string? formula)
    {
        XlsxFormulaTokenizeResult result = new XlsxFormulaTokenizeResult();
        if (formula == null)
        {
            result.AddError("formulaTokenizer.formulaNull", "Formula text must not be null.");
            return result;
        }

        int index = 0;
        while (index < formula.Length)
        {
            index = ReadToken(result, formula, index);
        }

        return result;
    }

    internal static int ReadToken(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        char current = formula[index];
        if (IsWhitespace(current))
        {
            return ReadWhile(result, formula, index, XlsxFormulaTokenKind.Whitespace, 1);
        }

        if (IsNumberStart(formula, index))
        {
            return ReadNumber(result, formula, index);
        }

        if (current == '"')
        {
            return ReadString(result, formula, index);
        }

        if (current == '\'')
        {
            return ReadQuotedSheetName(result, formula, index);
        }

        if (current == '[')
        {
            return ReadStructuredReference(result, formula, index);
        }

        if (current == '#')
        {
            return ReadError(result, formula, index);
        }

        if (IsIdentifierStart(current))
        {
            return ReadIdentifierLike(result, formula, index);
        }

        return ReadPunctuation(result, formula, index);
    }

    internal static int ReadWhile(XlsxFormulaTokenizeResult result, string formula, int index, XlsxFormulaTokenKind kind, int mode)
    {
        int current = index;
        while (current < formula.Length)
        {
            char value = formula[current];
            bool keepGoing = false;
            if (mode == 1)
            {
                keepGoing = IsWhitespace(value);
            }
            else if (mode == 2)
            {
                keepGoing = IsIdentifierPart(value);
            }

            if (!keepGoing)
            {
                break;
            }

            current = current + 1;
        }

        AddToken(result, kind, formula, index, current);
        return current;
    }

    internal static int ReadNumber(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        int current = index;
        while (current < formula.Length && IsDigit(formula[current]))
        {
            current = current + 1;
        }

        if (current < formula.Length && formula[current] == '.')
        {
            current = current + 1;
            while (current < formula.Length && IsDigit(formula[current]))
            {
                current = current + 1;
            }
        }

        if (current < formula.Length && (formula[current] == 'E' || formula[current] == 'e'))
        {
            int exponentStart = current;
            current = current + 1;
            if (current < formula.Length && (formula[current] == '+' || formula[current] == '-'))
            {
                current = current + 1;
            }

            if (current < formula.Length && IsDigit(formula[current]))
            {
                while (current < formula.Length && IsDigit(formula[current]))
                {
                    current = current + 1;
                }
            }
            else
            {
                current = exponentStart;
            }
        }

        AddToken(result, XlsxFormulaTokenKind.Number, formula, index, current);
        return current;
    }

    internal static int ReadString(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        int current = index + 1;
        while (current < formula.Length)
        {
            if (formula[current] == '"')
            {
                if (current + 1 < formula.Length && formula[current + 1] == '"')
                {
                    current = current + 2;
                    continue;
                }

                current = current + 1;
                AddToken(result, XlsxFormulaTokenKind.String, formula, index, current);
                return current;
            }

            current = current + 1;
        }

        AddToken(result, XlsxFormulaTokenKind.String, formula, index, current);
        result.AddError("formulaTokenizer.stringUnterminated", "Formula string literal is not terminated.");
        return current;
    }

    internal static int ReadQuotedSheetName(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        int current = index + 1;
        while (current < formula.Length)
        {
            if (formula[current] == '\'')
            {
                if (current + 1 < formula.Length && formula[current + 1] == '\'')
                {
                    current = current + 2;
                    continue;
                }

                current = current + 1;
                AddToken(result, XlsxFormulaTokenKind.SheetName, formula, index, current);
                return current;
            }

            current = current + 1;
        }

        AddToken(result, XlsxFormulaTokenKind.SheetName, formula, index, current);
        result.AddError("formulaTokenizer.sheetNameUnterminated", "Formula quoted sheet name is not terminated.");
        return current;
    }

    internal static int ReadStructuredReference(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        int current = index;
        int depth = 0;
        while (current < formula.Length)
        {
            if (formula[current] == '[')
            {
                depth = depth + 1;
            }
            else if (formula[current] == ']')
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    current = current + 1;
                    AddToken(result, XlsxFormulaTokenKind.StructuredReference, formula, index, current);
                    return current;
                }
            }

            current = current + 1;
        }

        AddToken(result, XlsxFormulaTokenKind.StructuredReference, formula, index, current);
        result.AddError("formulaTokenizer.structuredReferenceUnterminated", "Formula structured reference is not terminated.");
        return current;
    }

    internal static int ReadError(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        string known = ReadKnownError(formula, index);
        if (known.Length > 0)
        {
            AddToken(result, XlsxFormulaTokenKind.Error, formula, index, index + known.Length);
            return index + known.Length;
        }

        int current = index + 1;
        while (current < formula.Length && !IsFormulaDelimiter(formula[current]))
        {
            current = current + 1;
        }

        AddToken(result, XlsxFormulaTokenKind.Error, formula, index, current);
        return current;
    }

    internal static string ReadKnownError(string formula, int index)
    {
        if (StartsWithAt(formula, index, "#DIV/0!"))
        {
            return "#DIV/0!";
        }

        if (StartsWithAt(formula, index, "#VALUE!"))
        {
            return "#VALUE!";
        }

        if (StartsWithAt(formula, index, "#REF!"))
        {
            return "#REF!";
        }

        if (StartsWithAt(formula, index, "#NAME?"))
        {
            return "#NAME?";
        }

        if (StartsWithAt(formula, index, "#NULL!"))
        {
            return "#NULL!";
        }

        if (StartsWithAt(formula, index, "#NUM!"))
        {
            return "#NUM!";
        }

        if (StartsWithAt(formula, index, "#N/A"))
        {
            return "#N/A";
        }

        if (StartsWithAt(formula, index, "#GETTING_DATA"))
        {
            return "#GETTING_DATA";
        }

        return "";
    }

    internal static bool StartsWithAt(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
        {
            return false;
        }

        int current = 0;
        while (current < value.Length)
        {
            if (text[index + current] != value[current])
            {
                return false;
            }

            current = current + 1;
        }

        return true;
    }

    internal static int ReadIdentifierLike(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        int current = index;
        while (current < formula.Length && IsIdentifierPart(formula[current]))
        {
            current = current + 1;
        }

        string text = formula.Substring(index, current - index);
        if (text == "TRUE" || text == "FALSE" || text == "true" || text == "false")
        {
            AddToken(result, XlsxFormulaTokenKind.Boolean, formula, index, current);
            return current;
        }

        if (IsCellReferenceText(text))
        {
            AddToken(result, XlsxFormulaTokenKind.CellReference, formula, index, current);
            return current;
        }

        AddToken(result, XlsxFormulaTokenKind.Identifier, formula, index, current);
        return current;
    }

    internal static int ReadPunctuation(XlsxFormulaTokenizeResult result, string formula, int index)
    {
        char current = formula[index];
        if (index + 1 < formula.Length)
        {
            string two = formula.Substring(index, 2);
            if (two == "<=" || two == ">=" || two == "<>")
            {
                AddToken(result, XlsxFormulaTokenKind.Operator, formula, index, index + 2);
                return index + 2;
            }
        }

        if (current == '+' || current == '-' || current == '*' || current == '/' || current == '^' || current == '&' || current == '=' || current == '<' || current == '>' || current == '@')
        {
            AddToken(result, XlsxFormulaTokenKind.Operator, formula, index, index + 1);
            return index + 1;
        }

        if (current == ':')
        {
            AddToken(result, XlsxFormulaTokenKind.RangeOperator, formula, index, index + 1);
            return index + 1;
        }

        if (current == ',')
        {
            AddToken(result, XlsxFormulaTokenKind.UnionOperator, formula, index, index + 1);
            return index + 1;
        }

        if (current == ';')
        {
            AddToken(result, XlsxFormulaTokenKind.ArgumentSeparator, formula, index, index + 1);
            return index + 1;
        }

        if (current == '(')
        {
            AddToken(result, XlsxFormulaTokenKind.OpenParen, formula, index, index + 1);
            return index + 1;
        }

        if (current == ')')
        {
            AddToken(result, XlsxFormulaTokenKind.CloseParen, formula, index, index + 1);
            return index + 1;
        }

        if (current == '{')
        {
            AddToken(result, XlsxFormulaTokenKind.ArrayOpen, formula, index, index + 1);
            return index + 1;
        }

        if (current == '}')
        {
            AddToken(result, XlsxFormulaTokenKind.ArrayClose, formula, index, index + 1);
            return index + 1;
        }

        if (current == '!')
        {
            AddToken(result, XlsxFormulaTokenKind.Bang, formula, index, index + 1);
            return index + 1;
        }

        if (current == '%')
        {
            AddToken(result, XlsxFormulaTokenKind.Percent, formula, index, index + 1);
            return index + 1;
        }

        AddToken(result, XlsxFormulaTokenKind.Unknown, formula, index, index + 1);
        result.AddError("formulaTokenizer.characterUnsupported", "Formula contains an unsupported character.");
        return index + 1;
    }

    internal static void AddToken(XlsxFormulaTokenizeResult result, XlsxFormulaTokenKind kind, string formula, int start, int end)
    {
        XlsxFormulaToken token = new XlsxFormulaToken();
        token.Kind = kind;
        token.Start = start;
        token.Length = end - start;
        token.Text = formula.Substring(start, token.Length);
        result.Tokens.Add(token);
    }

    internal static bool IsCellReferenceText(string text)
    {
        XlsxA1CellReferenceResult result = XlsxA1CellReferenceConverter.Parse(text);
        return result.IsValid;
    }

    internal static bool IsNumberStart(string formula, int index)
    {
        if (IsDigit(formula[index]))
        {
            return true;
        }

        return formula[index] == '.' && index + 1 < formula.Length && IsDigit(formula[index + 1]);
    }

    internal static bool IsIdentifierStart(char value)
    {
        return IsAsciiLetter(value) || value == '_' || value == '$' || value == '\\';
    }

    internal static bool IsIdentifierPart(char value)
    {
        return IsAsciiLetter(value) || IsDigit(value) || value == '_' || value == '.' || value == '$' || value == '\\';
    }

    internal static bool IsFormulaDelimiter(char value)
    {
        if (IsWhitespace(value))
        {
            return true;
        }

        return value == '+' || value == '-' || value == '*' || value == '/' || value == '^' || value == '&' || value == '=' || value == '<' || value == '>' || value == ':' || value == ',' || value == ';' || value == '(' || value == ')' || value == '{' || value == '}' || value == '!';
    }

    internal static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
