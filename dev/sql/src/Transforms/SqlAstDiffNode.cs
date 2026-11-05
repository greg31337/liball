using System.Collections.Generic;

namespace sql;

internal class SqlAstDiffNode
{
    internal string Segment;
    internal string NodeType;
    internal object Node;
    internal SqlTextSpan Span;
    internal string ScalarSignature;
    internal List<SqlAstDiffNode> Children;

    internal SqlAstDiffNode(string segment, string nodeType, object node, SqlTextSpan span, string scalarSignature)
    {
        Segment = segment;
        NodeType = nodeType;
        Node = node;
        Span = span;
        ScalarSignature = scalarSignature;
        Children = new List<SqlAstDiffNode>();
    }

    internal string GetStructuralSignature()
    {
        string signature = NodeType + ":" + ScalarSignature;
        int index = 0;
        while (index < Children.Count)
        {
            SqlAstDiffNode child = Children[index];
            signature = signature + "|" + child.Segment + "=" + child.GetStructuralSignature();
            index = index + 1;
        }

        return signature;
    }

    internal SqlAstDiffNode? FindChild(string segment)
    {
        int index = 0;
        while (index < Children.Count)
        {
            if (Children[index].Segment == segment)
            {
                return Children[index];
            }

            index = index + 1;
        }

        return null;
    }
}
