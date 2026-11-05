using System.Text;

namespace odf;

internal static class FormulaFormatter
{
    internal static string FormatPreserved(OdfFormula formula)
    {
        if (formula == null)
        {
            throw new OdfFormulaException(
                "A parsed formula is required for formatting.");
        }
        if (IsFormulaUnchanged(formula))
        {
            return formula.Source;
        }
        return FormatCanonical(formula);
    }

    internal static string FormatCanonical(OdfFormula formula)
    {
        if (formula == null || formula.Root == null)
        {
            throw new OdfFormulaException(
                "A parsed formula root is required for canonical formatting.");
        }
        StringBuilder result = new StringBuilder();
        AppendFormulaPrefix(result, formula);
        AppendCanonicalNode(result, formula.Root, 0, false);
        return result.ToString();
    }

    internal static string EscapeString(string value)
    {
        if (value == null)
        {
            throw new OdfFormulaException(
                "A string value is required for formula escaping.");
        }
        StringBuilder result = new StringBuilder();
        result.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\0')
            {
                throw new OdfFormulaException(
                    "Formula strings cannot contain a null character.");
            }
            if (current == '"')
            {
                result.Append('"');
            }
            result.Append(current);
            index = index + 1;
        }
        result.Append('"');
        return result.ToString();
    }

    internal static string UnescapeString(string lexical)
    {
        if (lexical == null
            || lexical.Length < 2
            || lexical[0] != '"'
            || lexical[lexical.Length - 1] != '"')
        {
            throw new OdfFormulaException(
                "A quoted OpenFormula string is required.");
        }
        StringBuilder result = new StringBuilder();
        int index = 1;
        while (index < lexical.Length - 1)
        {
            char current = lexical[index];
            if (current == '\0')
            {
                throw new OdfFormulaException(
                    "Formula strings cannot contain a null character.");
            }
            if (current == '"')
            {
                if (index + 1 >= lexical.Length - 1
                    || lexical[index + 1] != '"')
                {
                    throw new OdfFormulaException(
                        "Embedded formula quotes must be doubled.");
                }
                result.Append('"');
                index = index + 2;
            }
            else
            {
                result.Append(current);
                index = index + 1;
            }
        }
        return result.ToString();
    }

    static void AppendFormulaPrefix(
        StringBuilder result,
        OdfFormula formula)
    {
        if (formula.NamespacePrefix.Length > 0)
        {
            result.Append(formula.NamespacePrefix);
            result.Append(":=");
            if (formula.ForcedRecalculation)
            {
                result.Append('=');
            }
        }
        else
        {
            result.Append('=');
            if (formula.ForcedRecalculation)
            {
                result.Append('=');
            }
        }
    }

    static void AppendCanonicalNode(
        StringBuilder result,
        OdfFormulaNode node,
        int parentPrecedence,
        bool isRightChild)
    {
        int precedence = CanonicalPrecedence(node);
        bool parentheses = NeedsParentheses(
            precedence,
            parentPrecedence,
            isRightChild);
        if (parentheses)
        {
            result.Append('(');
        }
        if (node.Kind == OdfFormulaNodeKind.FunctionCall)
        {
            result.Append(node.Text.ToUpperInvariant());
            result.Append('(');
            int index = 0;
            while (index < node.Children.Length)
            {
                if (index > 0)
                {
                    result.Append(';');
                }
                AppendCanonicalNode(result, node.Children[index], 0, false);
                index = index + 1;
            }
            result.Append(')');
        }
        else if (node.Kind == OdfFormulaNodeKind.Array)
        {
            result.Append('{');
            int index = 0;
            while (index < node.Children.Length)
            {
                if (index > 0)
                {
                    result.Append(ArraySeparator(node, index - 1));
                }
                AppendCanonicalNode(result, node.Children[index], 0, false);
                index = index + 1;
            }
            result.Append('}');
        }
        else if (node.Kind == OdfFormulaNodeKind.UnaryOperation
            && node.Children.Length == 1)
        {
            if (node.Text == "%")
            {
                AppendCanonicalNode(
                    result,
                    node.Children[0],
                    precedence,
                    false);
                result.Append('%');
            }
            else
            {
                result.Append(node.Text);
                AppendCanonicalNode(
                    result,
                    node.Children[0],
                    precedence,
                    true);
            }
        }
        else if (IsBinaryNode(node) && node.Children.Length == 2)
        {
            AppendCanonicalNode(
                result,
                node.Children[0],
                precedence,
                false);
            result.Append(node.Text);
            AppendCanonicalNode(
                result,
                node.Children[1],
                precedence,
                true);
        }
        else
        {
            AppendCanonicalLeaf(result, node);
        }
        if (parentheses)
        {
            result.Append(')');
        }
    }

    static void AppendCanonicalLeaf(
        StringBuilder result,
        OdfFormulaNode node)
    {
        if (node.TokenKind == OdfFormulaTokenKind.String)
        {
            result.Append(EscapeString(node.Value));
        }
        else if (node.TokenKind == OdfFormulaTokenKind.Boolean)
        {
            string value = node.Value.Length > 0 ? node.Value : node.Text;
            result.Append(value.ToUpperInvariant());
        }
        else
        {
            result.Append(node.Text);
        }
    }

    static char ArraySeparator(OdfFormulaNode node, int index)
    {
        if (index < node.Separators.Length
            && node.Separators[index] == "|")
        {
            return '|';
        }
        return ';';
    }

    static bool IsBinaryNode(OdfFormulaNode node)
    {
        return node.Kind == OdfFormulaNodeKind.BinaryOperation
            || node.Kind == OdfFormulaNodeKind.Range
            || node.Kind == OdfFormulaNodeKind.Intersection
            || node.Kind == OdfFormulaNodeKind.ReferenceConcatenation;
    }

    static int CanonicalPrecedence(OdfFormulaNode node)
    {
        if (node.Children.Length == 0
            || node.Kind == OdfFormulaNodeKind.FunctionCall
            || node.Kind == OdfFormulaNodeKind.Array)
        {
            return 100;
        }
        if (node.Kind == OdfFormulaNodeKind.Range || node.Text == ":")
        {
            return 90;
        }
        if (node.Kind == OdfFormulaNodeKind.Intersection)
        {
            return 80;
        }
        if (node.Kind == OdfFormulaNodeKind.ReferenceConcatenation)
        {
            return 70;
        }
        if (node.Kind == OdfFormulaNodeKind.UnaryOperation)
        {
            return node.Text == "%" ? 65 : 66;
        }
        if (node.Text == "^")
        {
            return 60;
        }
        if (node.Text == "*" || node.Text == "/")
        {
            return 50;
        }
        if (node.Text == "+" || node.Text == "-")
        {
            return 40;
        }
        if (node.Text == "&")
        {
            return 30;
        }
        return 20;
    }

    static bool NeedsParentheses(
        int precedence,
        int parentPrecedence,
        bool isRightChild)
    {
        return precedence < parentPrecedence
            || (isRightChild && precedence == parentPrecedence);
    }

    static bool IsFormulaUnchanged(OdfFormula formula)
    {
        return formula.Root != null
            && formula.OriginalRoot == formula.Root
            && formula.NamespacePrefix == formula.OriginalNamespacePrefix
            && formula.ForcedRecalculation
                == formula.OriginalForcedRecalculation
            && IsNodeUnchanged(formula.Root);
    }

    static bool IsNodeUnchanged(OdfFormulaNode node)
    {
        if (node.Kind != node.OriginalKind
            || node.Text != node.OriginalText
            || node.Value != node.OriginalValue
            || node.TokenKind != node.OriginalTokenKind
            || node.Children.Length != node.OriginalChildCount
            || !SeparatorArraysEqual(node.Separators, node.OriginalSeparators))
        {
            return false;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (!IsNodeUnchanged(node.Children[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool SeparatorArraysEqual(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
