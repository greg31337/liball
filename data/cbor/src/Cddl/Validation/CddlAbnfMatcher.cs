using System.Collections.Generic;

namespace cbor;

internal class CddlAbnfMemoEntry
{
    internal int NodeIndex;
    internal int Position;
    internal int Status;
    internal List<int> Positions = new List<int>();
}

internal class CddlAbnfMatcher
{
    internal CddlAbnfGrammar Grammar;
    internal int[] Units;
    internal CddlValidationState State;
    internal CddlNode ControlNode;
    internal List<CddlAbnfMemoEntry> Memo =
        new List<CddlAbnfMemoEntry>();

    internal CddlAbnfMatcher(
        CddlAbnfGrammar grammar,
        int[] units,
        CddlValidationState state,
        CddlNode controlNode)
    {
        Grammar = grammar;
        Units = units;
        State = state;
        ControlNode = controlNode;
    }

    internal bool Matches()
    {
        if (Grammar.Root == null)
        {
            return false;
        }
        List<int> positions = MatchNodeAt(Grammar.Root, 0);
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index] == Units.Length)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal List<int> MatchNodeAt(CddlAbnfNode node, int position)
    {
        CddlAbnfMemoEntry? existing = FindMemoEntry(node.Index, position);
        if (existing != null)
        {
            if (existing.Status == 1)
            {
                return new List<int>();
            }
            return CopyPositions(existing.Positions);
        }
        if (!State.TakePatternStates(1, ControlNode))
        {
            return new List<int>();
        }
        CddlAbnfMemoEntry entry = new CddlAbnfMemoEntry();
        entry.NodeIndex = node.Index;
        entry.Position = position;
        entry.Status = 1;
        Memo.Add(entry);
        List<int> result;
        if (node.Kind == CddlAbnfNode.Alternative)
        {
            result = MatchAlternativeNode(node, position);
        }
        else if (node.Kind == CddlAbnfNode.Sequence)
        {
            result = MatchSequenceNode(node, position);
        }
        else if (node.Kind == CddlAbnfNode.Repeat)
        {
            result = MatchRepeatNode(node, position);
        }
        else if (node.Kind == CddlAbnfNode.RuleReference)
        {
            result = MatchReferenceNode(node, position);
        }
        else if (node.Kind == CddlAbnfNode.Literal)
        {
            result = MatchLiteralNode(node, position);
        }
        else if (node.Kind == CddlAbnfNode.Range)
        {
            result = MatchRangeNode(node, position);
        }
        else
        {
            result = new List<int>();
        }
        entry.Status = 2;
        entry.Positions = CopyPositions(result);
        return result;
    }

    internal List<int> MatchAlternativeNode(
        CddlAbnfNode node,
        int position)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < node.Children.Count)
        {
            if (!State.TakeAlternative(ControlNode))
            {
                return result;
            }
            AddUniquePositions(
                result,
                MatchNodeAt(node.Children[index], position)
            );
            if (State.Diagnostics.HasErrors)
            {
                return result;
            }
            index = index + 1;
        }
        return result;
    }

    internal List<int> MatchSequenceNode(
        CddlAbnfNode node,
        int position)
    {
        List<int> current = new List<int>();
        current.Add(position);
        int childIndex = 0;
        while (childIndex < node.Children.Count && current.Count > 0)
        {
            List<int> next = new List<int>();
            int positionIndex = 0;
            while (positionIndex < current.Count)
            {
                AddUniquePositions(
                    next,
                    MatchNodeAt(
                        node.Children[childIndex],
                        current[positionIndex]
                    )
                );
                if (State.Diagnostics.HasErrors)
                {
                    return next;
                }
                positionIndex = positionIndex + 1;
            }
            current = next;
            childIndex = childIndex + 1;
        }
        return current;
    }

    internal List<int> MatchRepeatNode(
        CddlAbnfNode node,
        int position)
    {
        List<int> result = new List<int>();
        if (node.Children.Count != 1)
        {
            return result;
        }
        List<int> current = new List<int>();
        current.Add(position);
        int count = 0;
        if (node.Minimum == 0)
        {
            result.Add(position);
        }
        while (node.Maximum < 0 || count < node.Maximum)
        {
            if (!State.TakePatternStates(1, ControlNode))
            {
                return result;
            }
            List<int> next = new List<int>();
            int positionIndex = 0;
            while (positionIndex < current.Count)
            {
                AddUniquePositions(
                    next,
                    MatchNodeAt(node.Children[0], current[positionIndex])
                );
                if (State.Diagnostics.HasErrors)
                {
                    return result;
                }
                positionIndex = positionIndex + 1;
            }
            count = count + 1;
            if (count >= node.Minimum)
            {
                AddUniquePositions(result, next);
            }
            if (next.Count == 0)
            {
                break;
            }
            bool stable = PositionSetsEqual(current, next);
            current = next;
            if (stable && count >= node.Minimum)
            {
                break;
            }
        }
        return result;
    }

    internal List<int> MatchReferenceNode(
        CddlAbnfNode node,
        int position)
    {
        CddlAbnfRule? rule = FindRule(node.Text);
        if (rule == null || rule.Expression == null)
        {
            return new List<int>();
        }
        return MatchNodeAt(rule.Expression, position);
    }

    internal List<int> MatchLiteralNode(
        CddlAbnfNode node,
        int position)
    {
        int offset = 0;
        while (offset < node.Text.Length)
        {
            if (position + offset >= Units.Length)
            {
                return new List<int>();
            }
            int expected = node.Text[offset];
            int actual = Units[position + offset];
            if (!node.CaseSensitive)
            {
                expected = FoldAscii(expected);
                actual = FoldAscii(actual);
            }
            if (expected != actual)
            {
                return new List<int>();
            }
            offset = offset + 1;
        }
        List<int> result = new List<int>();
        result.Add(position + node.Text.Length);
        return result;
    }

    internal List<int> MatchRangeNode(
        CddlAbnfNode node,
        int position)
    {
        List<int> result = new List<int>();
        if (position < Units.Length
            && Units[position] >= node.Lower
            && Units[position] <= node.Upper)
        {
            result.Add(position + 1);
        }
        return result;
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

    internal CddlAbnfMemoEntry? FindMemoEntry(int nodeIndex, int position)
    {
        int index = 0;
        while (index < Memo.Count)
        {
            if (Memo[index].NodeIndex == nodeIndex
                && Memo[index].Position == position)
            {
                return Memo[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal List<int> CopyPositions(List<int> source)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }
        return result;
    }

    internal void AddUniquePositions(
        List<int> destination,
        List<int> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!ContainsPosition(destination, source[index]))
            {
                destination.Add(source[index]);
            }
            index = index + 1;
        }
    }

    internal bool ContainsPosition(List<int> positions, int position)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index] == position)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool PositionSetsEqual(List<int> left, List<int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Count)
        {
            if (!ContainsPosition(right, left[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal int FoldAscii(int value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value + ('a' - 'A');
        }
        return value;
    }
}
