namespace ttf;

internal class HyphenationNode
{
    System.Collections.Generic.Dictionary<int, HyphenationNode> children;
    int[]? weights;

    internal HyphenationNode()
    {
        children = new System.Collections.Generic.Dictionary<int, HyphenationNode>();
    }

    internal bool HasChild(int scalar)
    {
        return children.ContainsKey(scalar);
    }

    internal HyphenationNode GetChild(int scalar)
    {
        return children[scalar];
    }

    internal void AddChild(int scalar, HyphenationNode child)
    {
        children.Add(scalar, child);
    }

    internal int[]? GetWeights()
    {
        return weights;
    }

    internal void MergeWeights(int[] incoming)
    {
        if (weights == null)
        {
            weights = incoming;
            return;
        }

        int index = 0;
        while (index < incoming.Length)
        {
            if (incoming[index] > weights[index])
            {
                weights[index] = incoming[index];
            }

            index++;
        }
    }
}
