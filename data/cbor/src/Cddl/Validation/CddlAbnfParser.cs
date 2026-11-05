using System;
using System.Collections.Generic;

namespace cbor;

internal class CddlAbnfParser
{
    internal string Source;
    internal bool ByteMode;
    internal int Index;
    internal string? Error;
    internal CddlAbnfGrammar Grammar = new CddlAbnfGrammar();

    internal CddlAbnfParser(string source, bool byteMode)
    {
        Source = source;
        ByteMode = byteMode;
    }

    internal CddlAbnfGrammar? ParseGrammar()
    {
        CddlAbnfNode? root = ParseElement();
        if (root == null)
        {
            SetError("The ABNF controller does not start with an element.");
            return null;
        }
        Grammar.Root = root;
        SkipHorizontalSpace();
        if (Index == Source.Length)
        {
            return ValidateGrammar() ? Grammar : null;
        }
        if (CurrentCharacter() == ';')
        {
            if (!ConsumeComment())
            {
                return null;
            }
        }
        else if (!ConsumeNewline())
        {
            SetError("A newline must separate the ABNF root element from its rules.");
            return null;
        }
        while (Index < Source.Length)
        {
            if (!SkipRuleTrivia())
            {
                return null;
            }
            if (Index == Source.Length)
            {
                break;
            }
            if (!ParseRule())
            {
                return null;
            }
        }
        return ValidateGrammar() ? Grammar : null;
    }

    internal bool ParseRule()
    {
        SkipHorizontalSpace();
        string? name = ParseRuleName();
        if (name == null)
        {
            SetError("An ABNF rule must start with a rule name.");
            return false;
        }
        SkipCwsp();
        bool incremental = false;
        if (Index < Source.Length && Source[Index] == '=')
        {
            Index = Index + 1;
            if (Index < Source.Length && Source[Index] == '/')
            {
                incremental = true;
                Index = Index + 1;
            }
        }
        else
        {
            SetError("An ABNF rule requires '=' or '=/'.");
            return false;
        }
        SkipCwsp();
        CddlAbnfNode? expression = ParseAlternation();
        if (expression == null)
        {
            SetError("An ABNF rule requires an element expression.");
            return false;
        }
        SkipHorizontalSpace();
        if (Index < Source.Length && Source[Index] == ';')
        {
            if (!ConsumeComment())
            {
                return false;
            }
        }
        else if (!ConsumeNewline())
        {
            SetError("Each embedded ABNF rule must end with a newline.");
            return false;
        }
        return AddRule(name, expression, incremental);
    }

    internal CddlAbnfNode? ParseAlternation()
    {
        CddlAbnfNode? first = ParseConcatenation();
        if (first == null)
        {
            return null;
        }
        List<CddlAbnfNode> choices = new List<CddlAbnfNode>();
        choices.Add(first);
        while (true)
        {
            int checkpoint = Index;
            SkipCwsp();
            if (Index >= Source.Length || Source[Index] != '/')
            {
                Index = checkpoint;
                break;
            }
            Index = Index + 1;
            SkipCwsp();
            CddlAbnfNode? choice = ParseConcatenation();
            if (choice == null)
            {
                SetError("An ABNF alternative requires a concatenation after '/'.");
                return null;
            }
            choices.Add(choice);
        }
        return CreateContainerNode(CddlAbnfNode.Alternative, choices);
    }

    internal CddlAbnfNode? ParseConcatenation()
    {
        CddlAbnfNode? first = ParseRepetition();
        if (first == null)
        {
            return null;
        }
        List<CddlAbnfNode> sequence = new List<CddlAbnfNode>();
        sequence.Add(first);
        while (true)
        {
            int checkpoint = Index;
            if (!SkipCwsp() || !StartsRepetition())
            {
                Index = checkpoint;
                break;
            }
            CddlAbnfNode? next = ParseRepetition();
            if (next == null)
            {
                Index = checkpoint;
                break;
            }
            sequence.Add(next);
        }
        return CreateContainerNode(CddlAbnfNode.Sequence, sequence);
    }

    internal CddlAbnfNode? ParseRepetition()
    {
        int minimum = 1;
        int maximum = 1;
        int checkpoint = Index;
        if (Index < Source.Length && IsDecimalDigit(Source[Index]))
        {
            int first = ParseDecimalCount();
            if (first < 0)
            {
                return null;
            }
            if (Index < Source.Length && Source[Index] == '*')
            {
                minimum = first;
                maximum = -1;
                Index = Index + 1;
                if (Index < Source.Length && IsDecimalDigit(Source[Index]))
                {
                    int upper = ParseDecimalCount();
                    if (upper < 0)
                    {
                        return null;
                    }
                    maximum = upper;
                }
            }
            else
            {
                minimum = first;
                maximum = first;
            }
        }
        else if (Index < Source.Length && Source[Index] == '*')
        {
            minimum = 0;
            maximum = -1;
            Index = Index + 1;
            if (Index < Source.Length && IsDecimalDigit(Source[Index]))
            {
                int upper = ParseDecimalCount();
                if (upper < 0)
                {
                    return null;
                }
                maximum = upper;
            }
        }
        CddlAbnfNode? element = ParseElement();
        if (element == null)
        {
            Index = checkpoint;
            return null;
        }
        if (maximum >= 0 && minimum > maximum)
        {
            SetError("An ABNF repetition minimum cannot exceed its maximum.");
            return null;
        }
        if (minimum == 1 && maximum == 1)
        {
            return element;
        }
        CddlAbnfNode repeated = CreateNode(CddlAbnfNode.Repeat);
        repeated.Minimum = minimum;
        repeated.Maximum = maximum;
        repeated.Children.Add(element);
        return repeated;
    }

    internal CddlAbnfNode? ParseElement()
    {
        if (Index >= Source.Length)
        {
            return null;
        }
        char current = Source[Index];
        if (IsAlpha(current))
        {
            string? name = ParseRuleName();
            if (name == null)
            {
                return null;
            }
            CddlAbnfNode reference = CreateNode(
                CddlAbnfNode.RuleReference
            );
            reference.Text = name;
            return reference;
        }
        if (current == '(')
        {
            return ParseGroup();
        }
        if (current == '[')
        {
            return ParseOption();
        }
        if (current == '"')
        {
            return ParseQuotedLiteral(false);
        }
        if (current == '%')
        {
            return ParsePercentElement();
        }
        if (current == '<')
        {
            SetError("ABNF prose values are not supported for validation.");
            return null;
        }
        return null;
    }

    internal CddlAbnfNode? ParseGroup()
    {
        Index = Index + 1;
        SkipCwsp();
        CddlAbnfNode? expression = ParseAlternation();
        if (expression == null)
        {
            SetError("An ABNF group requires an expression.");
            return null;
        }
        SkipCwsp();
        if (Index >= Source.Length || Source[Index] != ')')
        {
            SetError("An ABNF group is missing ')'.");
            return null;
        }
        Index = Index + 1;
        return expression;
    }

    internal CddlAbnfNode? ParseOption()
    {
        Index = Index + 1;
        SkipCwsp();
        CddlAbnfNode? expression = ParseAlternation();
        if (expression == null)
        {
            SetError("An ABNF option requires an expression.");
            return null;
        }
        SkipCwsp();
        if (Index >= Source.Length || Source[Index] != ']')
        {
            SetError("An ABNF option is missing ']'.");
            return null;
        }
        Index = Index + 1;
        CddlAbnfNode optional = CreateNode(CddlAbnfNode.Repeat);
        optional.Minimum = 0;
        optional.Maximum = 1;
        optional.Children.Add(expression);
        return optional;
    }

    internal CddlAbnfNode? ParsePercentElement()
    {
        int start = Index;
        Index = Index + 1;
        if (Index >= Source.Length)
        {
            Index = start;
            return null;
        }
        char marker = ToLowerAscii(Source[Index]);
        if ((marker == 'i' || marker == 's')
            && Index + 1 < Source.Length
            && Source[Index + 1] == '"')
        {
            Index = Index + 1;
            return ParseQuotedLiteral(marker == 's');
        }
        if (marker != 'b' && marker != 'd' && marker != 'x')
        {
            SetError("An ABNF percent value requires b, d, x, i, or s.");
            return null;
        }
        Index = Index + 1;
        return ParseNumericValue(marker);
    }

    internal CddlAbnfNode? ParseQuotedLiteral(bool caseSensitive)
    {
        if (Index >= Source.Length || Source[Index] != '"')
        {
            return null;
        }
        Index = Index + 1;
        int start = Index;
        while (Index < Source.Length && Source[Index] != '"')
        {
            char current = Source[Index];
            if (current < 0x20 || current > 0x7e)
            {
                SetError("An ABNF quoted string must contain printable ASCII characters.");
                return null;
            }
            Index = Index + 1;
        }
        if (Index >= Source.Length)
        {
            SetError("An ABNF quoted string is missing its closing quote.");
            return null;
        }
        CddlAbnfNode literal = CreateNode(CddlAbnfNode.Literal);
        literal.Text = Source.Substring(start, Index - start);
        literal.CaseSensitive = caseSensitive;
        Index = Index + 1;
        return literal;
    }

    internal CddlAbnfNode? ParseNumericValue(char marker)
    {
        int first = ParseBasedInteger(marker);
        if (first < 0)
        {
            SetError("An ABNF numeric value requires at least one digit.");
            return null;
        }
        if (Index < Source.Length && Source[Index] == '-')
        {
            Index = Index + 1;
            int upper = ParseBasedInteger(marker);
            if (upper < 0 || first > upper)
            {
                SetError("An ABNF numeric range requires ordered endpoints.");
                return null;
            }
            return CreateRangeNode(first, upper);
        }
        if (Index < Source.Length && Source[Index] == '.')
        {
            List<CddlAbnfNode> sequence = new List<CddlAbnfNode>();
            sequence.Add(CreateRangeNode(first, first));
            while (Index < Source.Length && Source[Index] == '.')
            {
                Index = Index + 1;
                int next = ParseBasedInteger(marker);
                if (next < 0)
                {
                    SetError("Each dotted ABNF numeric value requires digits.");
                    return null;
                }
                sequence.Add(CreateRangeNode(next, next));
            }
            return CreateContainerNode(CddlAbnfNode.Sequence, sequence);
        }
        return CreateRangeNode(first, first);
    }

    internal int ParseBasedInteger(char marker)
    {
        int radix = marker == 'b' ? 2 : marker == 'd' ? 10 : 16;
        int value = 0;
        int digits = 0;
        while (Index < Source.Length)
        {
            int digit = DigitValue(Source[Index]);
            if (digit < 0 || digit >= radix)
            {
                break;
            }
            if (value > (0x10ffff - digit) / radix)
            {
                SetError("An ABNF numeric terminal exceeds the Unicode scalar range.");
                return -1;
            }
            value = value * radix + digit;
            digits = digits + 1;
            Index = Index + 1;
        }
        if (digits == 0)
        {
            return -1;
        }
        if (ByteMode && value > 255)
        {
            SetError("An .abnfb numeric terminal must fit in one byte.");
            return -1;
        }
        if (!ByteMode && value >= 0xd800 && value <= 0xdfff)
        {
            SetError("An .abnf numeric terminal cannot be a surrogate code point.");
            return -1;
        }
        return value;
    }

    internal int ParseDecimalCount()
    {
        int value = 0;
        int digits = 0;
        while (Index < Source.Length && IsDecimalDigit(Source[Index]))
        {
            int digit = Source[Index] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                SetError("An ABNF repetition count exceeds Int32.");
                return -1;
            }
            value = value * 10 + digit;
            digits = digits + 1;
            Index = Index + 1;
        }
        return digits == 0 ? -1 : value;
    }

    internal bool SkipCwsp()
    {
        bool consumed = false;
        while (true)
        {
            if (SkipHorizontalSpace())
            {
                consumed = true;
                continue;
            }
            int checkpoint = Index;
            bool newline;
            if (Index < Source.Length && Source[Index] == ';')
            {
                newline = ConsumeComment();
            }
            else
            {
                newline = ConsumeNewline();
            }
            if (!newline || !SkipHorizontalSpace())
            {
                Index = checkpoint;
                break;
            }
            consumed = true;
        }
        return consumed;
    }

    internal bool SkipHorizontalSpace()
    {
        int start = Index;
        while (Index < Source.Length
            && (Source[Index] == ' ' || Source[Index] == '\t'))
        {
            Index = Index + 1;
        }
        return Index > start;
    }

    internal bool SkipRuleTrivia()
    {
        while (Index < Source.Length)
        {
            int checkpoint = Index;
            SkipHorizontalSpace();
            if (Index < Source.Length && Source[Index] == ';')
            {
                if (!ConsumeComment())
                {
                    return false;
                }
                continue;
            }
            if (ConsumeNewline())
            {
                continue;
            }
            Index = checkpoint;
            break;
        }
        return true;
    }

    internal bool ConsumeComment()
    {
        if (Index >= Source.Length || Source[Index] != ';')
        {
            return false;
        }
        Index = Index + 1;
        while (Index < Source.Length
            && Source[Index] != '\r'
            && Source[Index] != '\n')
        {
            char current = Source[Index];
            if (current != '\t' && (current < 0x20 || current > 0x7e))
            {
                SetError("An ABNF comment contains an invalid character.");
                return false;
            }
            Index = Index + 1;
        }
        if (!ConsumeNewline())
        {
            SetError("An ABNF comment must end with a newline.");
            return false;
        }
        return true;
    }

    internal bool ConsumeNewline()
    {
        if (Index < Source.Length && Source[Index] == '\n')
        {
            Index = Index + 1;
            return true;
        }
        if (Index + 1 < Source.Length
            && Source[Index] == '\r'
            && Source[Index + 1] == '\n')
        {
            Index = Index + 2;
            return true;
        }
        return false;
    }

    internal bool StartsRepetition()
    {
        if (Index >= Source.Length)
        {
            return false;
        }
        char current = Source[Index];
        return IsAlpha(current)
            || IsDecimalDigit(current)
            || current == '*'
            || current == '('
            || current == '['
            || current == '"'
            || current == '%'
            || current == '<';
    }

    internal string? ParseRuleName()
    {
        if (Index >= Source.Length || !IsAlpha(Source[Index]))
        {
            return null;
        }
        int start = Index;
        Index = Index + 1;
        while (Index < Source.Length
            && (IsAlpha(Source[Index])
                || IsDecimalDigit(Source[Index])
                || Source[Index] == '-'))
        {
            Index = Index + 1;
        }
        return Source.Substring(start, Index - start).ToLowerInvariant();
    }

    internal bool AddRule(
        string name,
        CddlAbnfNode expression,
        bool incremental)
    {
        CddlAbnfRule? existing = FindRule(name);
        if (!incremental)
        {
            if (existing != null)
            {
                SetError("The ABNF rule '" + name + "' is defined more than once.");
                return false;
            }
            CddlAbnfRule rule = new CddlAbnfRule();
            rule.Name = name;
            rule.Expression = expression;
            Grammar.Rules.Add(rule);
            return true;
        }
        if (existing == null || existing.Expression == null)
        {
            SetError("The incremental ABNF rule '" + name + "' has no base definition.");
            return false;
        }
        CddlAbnfNode combined = CreateNode(CddlAbnfNode.Alternative);
        AddAlternativeChildren(combined, existing.Expression);
        AddAlternativeChildren(combined, expression);
        existing.Expression = combined;
        return true;
    }

    internal void AddAlternativeChildren(
        CddlAbnfNode destination,
        CddlAbnfNode source)
    {
        if (source.Kind == CddlAbnfNode.Alternative)
        {
            int index = 0;
            while (index < source.Children.Count)
            {
                destination.Children.Add(source.Children[index]);
                index = index + 1;
            }
        }
        else
        {
            destination.Children.Add(source);
        }
    }

    internal bool ValidateGrammar()
    {
        if (Grammar.Root == null)
        {
            return false;
        }
        if (!ValidateReferences(Grammar.Root))
        {
            return false;
        }
        int index = 0;
        while (index < Grammar.Rules.Count)
        {
            CddlAbnfNode? expression = Grammar.Rules[index].Expression;
            if (expression == null || !ValidateReferences(expression))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool ValidateReferences(CddlAbnfNode node)
    {
        if (node.Kind == CddlAbnfNode.RuleReference
            && FindRule(node.Text) == null)
        {
            SetError("The ABNF rule '" + node.Text + "' is not defined.");
            return false;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!ValidateReferences(node.Children[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal CddlAbnfRule? FindRule(string name)
    {
        int index = 0;
        while (index < Grammar.Rules.Count)
        {
            if (Grammar.Rules[index].Name == name)
            {
                return Grammar.Rules[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlAbnfNode CreateNode(int kind)
    {
        CddlAbnfNode node = new CddlAbnfNode();
        node.Index = Grammar.NodeCount;
        node.Kind = kind;
        Grammar.NodeCount = Grammar.NodeCount + 1;
        return node;
    }

    internal CddlAbnfNode CreateRangeNode(int lower, int upper)
    {
        CddlAbnfNode range = CreateNode(CddlAbnfNode.Range);
        range.Lower = lower;
        range.Upper = upper;
        return range;
    }

    internal CddlAbnfNode? CreateContainerNode(
        int kind,
        List<CddlAbnfNode> children)
    {
        if (children.Count == 0)
        {
            return null;
        }
        if (children.Count == 1)
        {
            return children[0];
        }
        CddlAbnfNode container = CreateNode(kind);
        int index = 0;
        while (index < children.Count)
        {
            container.Children.Add(children[index]);
            index = index + 1;
        }
        return container;
    }

    internal char CurrentCharacter()
    {
        return Index < Source.Length ? Source[Index] : '\0';
    }

    internal int DigitValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        char lower = ToLowerAscii(value);
        if (lower >= 'a' && lower <= 'f')
        {
            return lower - 'a' + 10;
        }
        return -1;
    }

    internal bool IsAlpha(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z');
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal char ToLowerAscii(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return (char)(value + ('a' - 'A'));
        }
        return value;
    }

    internal void SetError(string message)
    {
        if (Error == null)
        {
            Error = message + " Offset " + Index + ".";
        }
    }
}
