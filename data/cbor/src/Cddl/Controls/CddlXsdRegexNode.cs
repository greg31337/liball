using System.Collections.Generic;

namespace cbor;

internal class CddlXsdRegexNode
{
    internal const int Empty = 0;
    internal const int Character = 1;
    internal const int Sequence = 2;
    internal const int Choice = 3;
    internal const int Repeat = 4;
    internal int Kind;
    internal CddlXsdCharacterSet? CharacterSet;
    internal List<CddlXsdRegexNode> Children =
        new List<CddlXsdRegexNode>();
    internal int Minimum = 1;
    internal int Maximum = 1;

    internal CddlXsdRegexNode(int kind)
    {
        Kind = kind;
    }
}

internal class CddlXsdCharacterRange
{
    internal int Start;
    internal int End;

    internal CddlXsdCharacterRange(int start, int end)
    {
        Start = start;
        End = end;
    }
}

internal class CddlXsdCharacterSet
{
    internal List<CddlXsdCharacterRange> Ranges =
        new List<CddlXsdCharacterRange>();
    internal List<CddlXsdCharacterSet> Includes =
        new List<CddlXsdCharacterSet>();
    internal string? EscapeName;
    internal bool EscapeComplement;
    internal bool Negated;
    internal CddlXsdCharacterSet? Subtraction;

    internal CddlXsdCharacterSet()
    {
    }

    internal bool Contains(int codePoint)
    {
        if (!CddlXsdUnicode.IsXmlCharacter(codePoint))
        {
            return false;
        }
        bool included = ContainsBase(codePoint);
        if (Negated)
        {
            included = !included;
        }
        if (included
            && Subtraction != null
            && Subtraction.Contains(codePoint))
        {
            return false;
        }
        return included;
    }

    internal bool ContainsBase(int codePoint)
    {
        bool included = false;
        if (EscapeName != null)
        {
            included = CddlXsdUnicode.MatchesEscape(
                EscapeName,
                codePoint
            );
            if (EscapeComplement)
            {
                included = !included;
            }
        }
        int index = 0;
        while (index < Ranges.Count && !included)
        {
            included = codePoint >= Ranges[index].Start
                && codePoint <= Ranges[index].End;
            index = index + 1;
        }
        index = 0;
        while (index < Includes.Count && !included)
        {
            included = Includes[index].Contains(codePoint);
            index = index + 1;
        }
        return included;
    }
}

internal class CddlXsdClassUnit
{
    internal CddlXsdCharacterSet Set = new CddlXsdCharacterSet();
    internal bool IsSingle;
    internal int CodePoint;

    internal CddlXsdClassUnit()
    {
    }
}
