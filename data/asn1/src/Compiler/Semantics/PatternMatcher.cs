namespace asn1;

internal class PatternMatcher
{
    internal int[] Input = [];

    internal bool Matches(PatternNode root, string text)
    {
        if (root == null)
        {
            throw new System.ArgumentNullException("root");
        }

        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        int[]? input = this.ReadInput(text);
        if (input == null)
        {
            return false;
        }

        this.Input = input;
        int start = 0;
        while (start <= this.Input.Length)
        {
            int[] positions = this.MatchNode(root, start);
            if (positions.Length > 0)
            {
                return true;
            }

            start = start + 1;
        }

        return false;
    }

    internal int[]? ReadInput(string text)
    {
        System.Collections.Generic.List<int> scalars =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < text.Length)
        {
            int first = text[index];
            if (first >= 55296 && first <= 56319)
            {
                if (index + 1 >= text.Length)
                {
                    return null;
                }

                int second = text[index + 1];
                if (second < 56320 || second > 57343)
                {
                    return null;
                }

                scalars.Add(65536 + (first - 55296) * 1024 + second - 56320);
                index = index + 2;
            }
            else if (first >= 56320 && first <= 57343)
            {
                return null;
            }
            else
            {
                scalars.Add(first);
                index = index + 1;
            }
        }

        return scalars.ToArray();
    }

    internal int[] MatchNode(PatternNode node, int position)
    {
        if (node.Kind == PatternNodeKind.Sequence)
        {
            return this.MatchSequence(node, position);
        }

        if (node.Kind == PatternNodeKind.Alternative)
        {
            return this.MatchAlternative(node, position);
        }

        if (node.Kind == PatternNodeKind.Repeat)
        {
            return this.MatchRepeat(node, position);
        }

        if (node.Kind == PatternNodeKind.StartAnchor)
        {
            return position == 0 ? [position] : [];
        }

        if (node.Kind == PatternNodeKind.EndAnchor)
        {
            return position == this.Input.Length ? [position] : [];
        }

        if (position >= this.Input.Length)
        {
            return [];
        }

        int scalar = this.Input[position];
        if (node.Kind == PatternNodeKind.Literal)
        {
            return scalar == node.Scalar ? [position + 1] : [];
        }

        if (node.Kind == PatternNodeKind.Any)
        {
            return [position + 1];
        }

        if (node.Kind == PatternNodeKind.CharacterClass)
        {
            bool found = false;
            int index = 0;
            while (index < node.Children.Length)
            {
                PatternNode range = node.Children[index];
                if (scalar >= range.LowerScalar && scalar <= range.UpperScalar)
                {
                    found = true;
                }

                index = index + 1;
            }

            return found != node.Negated ? [position + 1] : [];
        }

        return [];
    }

    internal int[] MatchSequence(PatternNode node, int position)
    {
        System.Collections.Generic.List<int> positions =
            new System.Collections.Generic.List<int>();
        positions.Add(position);
        int childIndex = 0;
        while (childIndex < node.Children.Length && positions.Count > 0)
        {
            System.Collections.Generic.List<int> next =
                new System.Collections.Generic.List<int>();
            int positionIndex = 0;
            while (positionIndex < positions.Count)
            {
                int[] matches = this.MatchNode(
                    node.Children[childIndex],
                    positions[positionIndex]);
                int matchIndex = 0;
                while (matchIndex < matches.Length)
                {
                    this.AddUnique(next, matches[matchIndex]);
                    matchIndex = matchIndex + 1;
                }

                positionIndex = positionIndex + 1;
            }

            positions = next;
            childIndex = childIndex + 1;
        }

        return positions.ToArray();
    }

    internal int[] MatchAlternative(PatternNode node, int position)
    {
        System.Collections.Generic.List<int> positions =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < node.Children.Length)
        {
            int[] matches = this.MatchNode(node.Children[index], position);
            int matchIndex = 0;
            while (matchIndex < matches.Length)
            {
                this.AddUnique(positions, matches[matchIndex]);
                matchIndex = matchIndex + 1;
            }

            index = index + 1;
        }

        return positions.ToArray();
    }

    internal int[] MatchRepeat(PatternNode node, int position)
    {
        System.Collections.Generic.List<int> results =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> current =
            new System.Collections.Generic.List<int>();
        current.Add(position);
        if (node.Minimum == 0)
        {
            results.Add(position);
        }

        int maximum = node.Unbounded ? this.Input.Length + 1 : node.Maximum;
        int count = 0;
        while (count < maximum && current.Count > 0)
        {
            System.Collections.Generic.List<int> next =
                new System.Collections.Generic.List<int>();
            int currentIndex = 0;
            while (currentIndex < current.Count)
            {
                int[] matches = this.MatchNode(
                    node.Children[0],
                    current[currentIndex]);
                int matchIndex = 0;
                while (matchIndex < matches.Length)
                {
                    this.AddUnique(next, matches[matchIndex]);
                    matchIndex = matchIndex + 1;
                }

                currentIndex = currentIndex + 1;
            }

            count = count + 1;
            if (count >= node.Minimum)
            {
                int resultIndex = 0;
                while (resultIndex < next.Count)
                {
                    this.AddUnique(results, next[resultIndex]);
                    resultIndex = resultIndex + 1;
                }
            }

            if (this.SamePositions(current, next))
            {
                break;
            }

            current = next;
        }

        return results.ToArray();
    }

    internal void AddUnique(System.Collections.Generic.List<int> values, int value)
    {
        if (!values.Contains(value))
        {
            values.Add(value);
        }
    }

    internal bool SamePositions(
        System.Collections.Generic.List<int> left,
        System.Collections.Generic.List<int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            if (!right.Contains(left[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
