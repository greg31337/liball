using System.Collections.Generic;

namespace cbor;

internal class CddlXsdRegexParser
{
    internal string Pattern;
    internal int Index;
    internal int Depth;
    internal int NodeCount;
    internal string? Error;

    internal CddlXsdRegexParser(string pattern)
    {
        Pattern = pattern;
    }

    internal CddlXsdRegexNode? ParsePattern()
    {
        CddlXsdRegexNode? result = ParseExpression();
        if (result == null)
        {
            return null;
        }
        if (Index != Pattern.Length)
        {
            SetError("unexpected character at offset " + Index);
            return null;
        }
        return result;
    }

    internal CddlXsdRegexNode? ParseExpression()
    {
        if (!EnterParserDepth())
        {
            return null;
        }
        CddlXsdRegexNode? first = ParseBranch();
        if (first == null)
        {
            LeaveParserDepth();
            return null;
        }
        if (!HasCharacter('|'))
        {
            LeaveParserDepth();
            return first;
        }
        CddlXsdRegexNode choice = CreateNode(CddlXsdRegexNode.Choice);
        choice.Children.Add(first);
        while (HasCharacter('|'))
        {
            Index = Index + 1;
            CddlXsdRegexNode? branch = ParseBranch();
            if (branch == null)
            {
                LeaveParserDepth();
                return null;
            }
            choice.Children.Add(branch);
        }
        LeaveParserDepth();
        return choice;
    }

    internal CddlXsdRegexNode? ParseBranch()
    {
        CddlXsdRegexNode sequence = CreateNode(CddlXsdRegexNode.Sequence);
        while (Index < Pattern.Length
            && Pattern[Index] != '|'
            && Pattern[Index] != ')')
        {
            CddlXsdRegexNode? piece = ParsePiece();
            if (piece == null)
            {
                return null;
            }
            sequence.Children.Add(piece);
        }
        if (sequence.Children.Count == 0)
        {
            sequence.Kind = CddlXsdRegexNode.Empty;
        }
        return sequence;
    }

    internal CddlXsdRegexNode? ParsePiece()
    {
        CddlXsdRegexNode? atom = ParseAtom();
        if (atom == null)
        {
            return null;
        }
        if (Index >= Pattern.Length)
        {
            return atom;
        }
        int minimum = 1;
        int maximum = 1;
        bool quantified = true;
        char current = Pattern[Index];
        if (current == '?')
        {
            minimum = 0;
            maximum = 1;
            Index = Index + 1;
        }
        else if (current == '*')
        {
            minimum = 0;
            maximum = -1;
            Index = Index + 1;
        }
        else if (current == '+')
        {
            minimum = 1;
            maximum = -1;
            Index = Index + 1;
        }
        else if (current == '{')
        {
            CddlXsdQuantifier quantifier = ParseBoundedQuantifier();
            if (!quantifier.IsValid)
            {
                return null;
            }
            minimum = quantifier.Minimum;
            maximum = quantifier.Maximum;
        }
        else
        {
            quantified = false;
        }
        if (!quantified)
        {
            return atom;
        }
        CddlXsdRegexNode repeat = CreateNode(CddlXsdRegexNode.Repeat);
        repeat.Children.Add(atom);
        repeat.Minimum = minimum;
        repeat.Maximum = maximum;
        return repeat;
    }

    internal CddlXsdRegexNode? ParseAtom()
    {
        if (Index >= Pattern.Length)
        {
            SetError("an atom was expected at the end of the pattern");
            return null;
        }
        char current = Pattern[Index];
        if (current == '(')
        {
            Index = Index + 1;
            CddlXsdRegexNode? grouped = ParseExpression();
            if (grouped == null)
            {
                return null;
            }
            if (!HasCharacter(')'))
            {
                SetError("a closing parenthesis was expected");
                return null;
            }
            Index = Index + 1;
            return grouped;
        }
        if (current == '[')
        {
            CddlXsdCharacterSet? set = ParseCharacterSet();
            if (set == null)
            {
                return null;
            }
            return CreateCharacterNode(set);
        }
        if (current == '.')
        {
            Index = Index + 1;
            CddlXsdCharacterSet wildcard = new CddlXsdCharacterSet();
            wildcard.Negated = true;
            wildcard.Ranges.Add(new CddlXsdCharacterRange(0x0a, 0x0a));
            wildcard.Ranges.Add(new CddlXsdCharacterRange(0x0d, 0x0d));
            return CreateCharacterNode(wildcard);
        }
        if (current == '\\')
        {
            CddlXsdClassUnit? escaped = ParseEscape();
            if (escaped == null)
            {
                return null;
            }
            return CreateCharacterNode(escaped.Set);
        }
        if (IsMetaCharacter(current))
        {
            SetError("an unexpected metacharacter occurs at offset " + Index);
            return null;
        }
        int codePoint = ReadCodePoint();
        if (!CddlXsdUnicode.IsXmlCharacter(codePoint))
        {
            SetError("the pattern contains a character outside the XML character set");
            return null;
        }
        return CreateCharacterNode(CreateSingleSet(codePoint));
    }

    internal CddlXsdQuantifier ParseBoundedQuantifier()
    {
        CddlXsdQuantifier result = new CddlXsdQuantifier();
        Index = Index + 1;
        int minimum = ReadQuantity();
        if (minimum < 0)
        {
            return result;
        }
        int maximum = minimum;
        if (HasCharacter(','))
        {
            Index = Index + 1;
            if (HasCharacter('}'))
            {
                maximum = -1;
            }
            else
            {
                maximum = ReadQuantity();
                if (maximum < 0)
                {
                    return result;
                }
            }
        }
        if (!HasCharacter('}'))
        {
            SetError("a closing quantifier brace was expected");
            return result;
        }
        Index = Index + 1;
        if (maximum >= 0 && minimum > maximum)
        {
            SetError("a quantifier minimum exceeds its maximum");
            return result;
        }
        result.IsValid = true;
        result.Minimum = minimum;
        result.Maximum = maximum;
        return result;
    }

    internal int ReadQuantity()
    {
        if (Index >= Pattern.Length
            || Pattern[Index] < '0'
            || Pattern[Index] > '9')
        {
            SetError("a decimal quantifier value was expected");
            return -1;
        }
        int value = 0;
        while (Index < Pattern.Length
            && Pattern[Index] >= '0'
            && Pattern[Index] <= '9')
        {
            int digit = Pattern[Index] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                SetError("a quantifier exceeds the Int32 limit");
                return -1;
            }
            value = value * 10 + digit;
            Index = Index + 1;
        }
        return value;
    }

    internal CddlXsdCharacterSet? ParseCharacterSet()
    {
        Index = Index + 1;
        CddlXsdCharacterSet set = new CddlXsdCharacterSet();
        if (HasCharacter('^'))
        {
            set.Negated = true;
            Index = Index + 1;
        }
        bool hasUnit = false;
        while (Index < Pattern.Length && Pattern[Index] != ']')
        {
            if (Pattern[Index] == '-'
                && Index + 1 < Pattern.Length
                && Pattern[Index + 1] == '['
                && hasUnit)
            {
                Index = Index + 1;
                set.Subtraction = ParseCharacterSet();
                if (set.Subtraction == null)
                {
                    return null;
                }
                break;
            }
            if (Pattern[Index] == '-'
                && hasUnit
                && Index + 1 < Pattern.Length
                && Pattern[Index + 1] != ']')
            {
                SetError("a literal hyphen is only allowed at a character-group edge");
                return null;
            }
            CddlXsdClassUnit? first = ParseClassUnit();
            if (first == null)
            {
                return null;
            }
            hasUnit = true;
            if (first.IsSingle
                && HasCharacter('-')
                && Index + 1 < Pattern.Length
                && Pattern[Index + 1] != ']'
                && Pattern[Index + 1] != '[')
            {
                Index = Index + 1;
                CddlXsdClassUnit? last = ParseClassUnit();
                if (last == null || !last.IsSingle)
                {
                    SetError("a character range requires single-character endpoints");
                    return null;
                }
                if (last.CodePoint < first.CodePoint)
                {
                    SetError("a character range is in descending order");
                    return null;
                }
                set.Ranges.Add(new CddlXsdCharacterRange(
                    first.CodePoint,
                    last.CodePoint
                ));
            }
            else
            {
                set.Includes.Add(first.Set);
            }
        }
        if (!hasUnit)
        {
            SetError("an XML Schema character class cannot be empty");
            return null;
        }
        if (!HasCharacter(']'))
        {
            SetError("a closing character-class bracket was expected");
            return null;
        }
        Index = Index + 1;
        return set;
    }

    internal CddlXsdClassUnit? ParseClassUnit()
    {
        if (Index >= Pattern.Length)
        {
            return null;
        }
        if (Pattern[Index] == '\\')
        {
            return ParseEscape();
        }
        char current = Pattern[Index];
        if (current == '[' || current == ']')
        {
            SetError("an unescaped bracket occurs in a character class");
            return null;
        }
        int codePoint = ReadCodePoint();
        if (!CddlXsdUnicode.IsXmlCharacter(codePoint))
        {
            SetError("a character class contains a character outside the XML character set");
            return null;
        }
        CddlXsdClassUnit unit = new CddlXsdClassUnit();
        unit.IsSingle = true;
        unit.CodePoint = codePoint;
        unit.Set = CreateSingleSet(codePoint);
        return unit;
    }

    internal CddlXsdClassUnit? ParseEscape()
    {
        Index = Index + 1;
        if (Index >= Pattern.Length)
        {
            SetError("an escape is incomplete");
            return null;
        }
        char escaped = Pattern[Index];
        if (escaped == 'p' || escaped == 'P')
        {
            bool complement = escaped == 'P';
            Index = Index + 1;
            if (!HasCharacter('{'))
            {
                SetError("a category escape requires an opening brace");
                return null;
            }
            Index = Index + 1;
            int start = Index;
            while (Index < Pattern.Length && Pattern[Index] != '}')
            {
                Index = Index + 1;
            }
            if (!HasCharacter('}') || Index == start)
            {
                SetError("a category escape requires a category name and closing brace");
                return null;
            }
            string name = Pattern.Substring(start, Index - start);
            Index = Index + 1;
            if (!CddlXsdUnicode.IsKnownCategory(name))
            {
                SetError("the Unicode category or block is unknown: " + name);
                return null;
            }
            CddlXsdClassUnit category = new CddlXsdClassUnit();
            category.Set.EscapeName = name;
            category.Set.EscapeComplement = complement;
            return category;
        }
        if (IsMultiEscape(escaped))
        {
            Index = Index + 1;
            CddlXsdClassUnit multiple = new CddlXsdClassUnit();
            char lower = char.ToLowerInvariant(escaped);
            multiple.Set.EscapeName = lower.ToString();
            multiple.Set.EscapeComplement = char.IsUpper(escaped);
            return multiple;
        }
        int codePoint = FindSingleEscape(escaped);
        if (codePoint < 0)
        {
            SetError("the XML Schema escape is not supported: \\" + escaped);
            return null;
        }
        Index = Index + 1;
        CddlXsdClassUnit single = new CddlXsdClassUnit();
        single.IsSingle = true;
        single.CodePoint = codePoint;
        single.Set = CreateSingleSet(codePoint);
        return single;
    }

    internal bool IsMultiEscape(char value)
    {
        return value == 's' || value == 'S'
            || value == 'i' || value == 'I'
            || value == 'c' || value == 'C'
            || value == 'd' || value == 'D'
            || value == 'w' || value == 'W';
    }

    internal int FindSingleEscape(char value)
    {
        if (value == 'n') return 0x0a;
        if (value == 'r') return 0x0d;
        if (value == 't') return 0x09;
        string allowed = "\\|.?*+(){}-[]^";
        return allowed.IndexOf(value) >= 0 ? value : -1;
    }

    internal CddlXsdRegexNode CreateCharacterNode(CddlXsdCharacterSet set)
    {
        CddlXsdRegexNode node = CreateNode(CddlXsdRegexNode.Character);
        node.CharacterSet = set;
        return node;
    }

    internal CddlXsdCharacterSet CreateSingleSet(int codePoint)
    {
        CddlXsdCharacterSet set = new CddlXsdCharacterSet();
        set.Ranges.Add(new CddlXsdCharacterRange(codePoint, codePoint));
        return set;
    }

    internal CddlXsdRegexNode CreateNode(int kind)
    {
        NodeCount = NodeCount + 1;
        return new CddlXsdRegexNode(kind);
    }

    internal int ReadCodePoint()
    {
        char first = Pattern[Index];
        if (char.IsHighSurrogate(first)
            && Index + 1 < Pattern.Length
            && char.IsLowSurrogate(Pattern[Index + 1]))
        {
            int value = char.ConvertToUtf32(first, Pattern[Index + 1]);
            Index = Index + 2;
            return value;
        }
        Index = Index + 1;
        return first;
    }

    internal bool IsMetaCharacter(char value)
    {
        return value == '\\' || value == '.' || value == '?'
            || value == '*' || value == '+' || value == '{'
            || value == '}' || value == '(' || value == ')'
            || value == '[' || value == ']' || value == '|';
    }

    internal bool HasCharacter(char value)
    {
        return Index < Pattern.Length && Pattern[Index] == value;
    }

    internal bool EnterParserDepth()
    {
        if (Depth >= 256)
        {
            SetError("the regular expression nesting depth exceeds 256");
            return false;
        }
        Depth = Depth + 1;
        return true;
    }

    internal void LeaveParserDepth()
    {
        if (Depth > 0)
        {
            Depth = Depth - 1;
        }
    }

    internal void SetError(string message)
    {
        if (Error == null)
        {
            Error = message;
        }
    }
}

internal class CddlXsdQuantifier
{
    internal bool IsValid;
    internal int Minimum;
    internal int Maximum;

    internal CddlXsdQuantifier()
    {
    }
}
