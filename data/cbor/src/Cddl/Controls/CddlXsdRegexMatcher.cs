using System.Collections.Generic;

namespace cbor;

internal class CddlXsdRegexMatcher
{
    internal CddlValidationState State;
    internal CddlNode SchemaNode;

    internal CddlXsdRegexMatcher(
        CddlValidationState state,
        CddlNode schemaNode)
    {
        State = state;
        SchemaNode = schemaNode;
    }

    internal bool Matches(CddlXsdRegexNode expression, string value)
    {
        int[]? codePoints = CreateCodePoints(value);
        if (codePoints == null)
        {
            return false;
        }
        int index = 0;
        while (index < codePoints.Length)
        {
            if (!CddlXsdUnicode.IsXmlCharacter(codePoints[index]))
            {
                return false;
            }
            index = index + 1;
        }
        List<int> ends = new List<int>();
        MatchExpression(expression, codePoints, 0, ends);
        return ContainsPosition(ends, codePoints.Length);
    }

    internal void MatchExpression(
        CddlXsdRegexNode expression,
        int[] input,
        int start,
        List<int> ends)
    {
        if (!State.TakePatternStates(1, SchemaNode))
        {
            return;
        }
        if (expression.Kind == CddlXsdRegexNode.Empty)
        {
            AddPosition(ends, start);
            return;
        }
        if (expression.Kind == CddlXsdRegexNode.Character)
        {
            if (start < input.Length
                && expression.CharacterSet != null
                && expression.CharacterSet.Contains(input[start]))
            {
                AddPosition(ends, start + 1);
            }
            return;
        }
        if (expression.Kind == CddlXsdRegexNode.Sequence)
        {
            MatchSequence(expression, input, start, ends);
            return;
        }
        if (expression.Kind == CddlXsdRegexNode.Choice)
        {
            int index = 0;
            while (index < expression.Children.Count)
            {
                MatchExpression(
                    expression.Children[index],
                    input,
                    start,
                    ends
                );
                if (State.Diagnostics.HasErrors)
                {
                    return;
                }
                index = index + 1;
            }
            return;
        }
        if (expression.Kind == CddlXsdRegexNode.Repeat)
        {
            MatchRepeat(expression, input, start, ends);
        }
    }

    internal void MatchSequence(
        CddlXsdRegexNode expression,
        int[] input,
        int start,
        List<int> ends)
    {
        List<int> current = new List<int>();
        current.Add(start);
        int childIndex = 0;
        while (childIndex < expression.Children.Count
            && current.Count > 0)
        {
            List<int> next = new List<int>();
            int positionIndex = 0;
            while (positionIndex < current.Count)
            {
                MatchExpression(
                    expression.Children[childIndex],
                    input,
                    current[positionIndex],
                    next
                );
                if (State.Diagnostics.HasErrors)
                {
                    return;
                }
                positionIndex = positionIndex + 1;
            }
            current = next;
            childIndex = childIndex + 1;
        }
        AddPositions(ends, current);
    }

    internal void MatchRepeat(
        CddlXsdRegexNode expression,
        int[] input,
        int start,
        List<int> ends)
    {
        if (expression.Children.Count != 1)
        {
            return;
        }
        if (expression.Minimum == 0)
        {
            AddPosition(ends, start);
        }
        int maximum = expression.Maximum;
        if (maximum < 0)
        {
            maximum = input.Length - start + 1;
            if (expression.Minimum > maximum)
            {
                maximum = expression.Minimum;
            }
        }
        List<int> current = new List<int>();
        current.Add(start);
        int count = 0;
        while (count < maximum && current.Count > 0)
        {
            List<int> next = new List<int>();
            int positionIndex = 0;
            while (positionIndex < current.Count)
            {
                List<int> occurrence = new List<int>();
                MatchExpression(
                    expression.Children[0],
                    input,
                    current[positionIndex],
                    occurrence
                );
                int endIndex = 0;
                while (endIndex < occurrence.Count)
                {
                    int end = occurrence[endIndex];
                    if (count + 1 >= expression.Minimum)
                    {
                        AddPosition(ends, end);
                    }
                    if ((end != current[positionIndex]
                            && end <= input.Length)
                        || count + 1 < expression.Minimum)
                    {
                        AddPosition(next, end);
                    }
                    endIndex = endIndex + 1;
                }
                if (State.Diagnostics.HasErrors)
                {
                    return;
                }
                positionIndex = positionIndex + 1;
            }
            current = next;
            count = count + 1;
        }
    }

    internal int[]? CreateCodePoints(string value)
    {
        List<int> points = new List<int>();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length
                    || !char.IsLowSurrogate(value[index + 1]))
                {
                    return null;
                }
                points.Add(char.ConvertToUtf32(current, value[index + 1]));
                index = index + 2;
            }
            else
            {
                if (char.IsLowSurrogate(current))
                {
                    return null;
                }
                points.Add(current);
                index = index + 1;
            }
        }
        return points.ToArray();
    }

    internal void AddPositions(List<int> destination, List<int> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            AddPosition(destination, source[index]);
            index = index + 1;
        }
    }

    internal void AddPosition(List<int> positions, int position)
    {
        if (!ContainsPosition(positions, position))
        {
            positions.Add(position);
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
}
