namespace svg;

/// <summary>Creates and resolves deterministic child-index paths.</summary>
public class SvgPatchPath : ISvgPatchPath
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPatchPath()
    {
    }

    /// <summary>Returns the stable child-index path of a connected document node.</summary>
    /// <param name="node">The connected node.</param>
    /// <returns><c>/</c> for the document node or a slash-separated child-index path.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when parent and child links are inconsistent.</exception>
    public string Get(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (node.Parent == null)
        {
            if (node.Kind == SvgNodeKind.Document && node.OwnerDocument != null && object.ReferenceEquals(node.OwnerDocument.DocumentNode, node)) return "/";
            throw new System.ArgumentException("The node is not connected to a document.", "node");
        }
        System.Collections.Generic.List<int> indexes = new System.Collections.Generic.List<int>();
        SvgNode current = node;
        while (current.Parent != null)
        {
            int index = new SvgPatchPath().FindChild(current.Parent, current);
            if (index < 0) throw new System.ArgumentException("A parent link is inconsistent with its child collection.", "node");
            indexes.Add(index);
            current = current.Parent;
        }
        if (current.Kind != SvgNodeKind.Document || current.OwnerDocument == null || !object.ReferenceEquals(current.OwnerDocument.DocumentNode, current)) throw new System.ArgumentException("The node is not connected to a document.", "node");
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int position = indexes.Count - 1;
        while (position >= 0)
        {
            builder.Append('/');
            builder.Append(indexes[position].ToString(System.Globalization.CultureInfo.InvariantCulture));
            position = position - 1;
        }
        return builder.Length == 0 ? "/" : builder.ToString();
    }

    /// <summary>Resolves a stable child-index path against a document.</summary>
    /// <param name="document">The document containing the path.</param>
    /// <param name="path">The slash-separated path.</param>
    /// <returns>The resolved node, or <c>null</c> for malformed or missing paths.</returns>
    public SvgNode? Resolve(SvgDocument? document, string? path)
    {
        if (document == null || path == null || path.Length == 0 || path[0] != '/') return null;
        SvgNode current = document.DocumentNode;
        if (path.Length == 1) return current;
        int position = 1;
        while (position < path.Length)
        {
            int index = 0;
            int digits = 0;
            while (position < path.Length && path[position] != '/')
            {
                char value = path[position];
                if (value < '0' || value > '9') return null;
                int digit = value - '0';
                if (index > (System.Int32.MaxValue - digit) / 10) return null;
                index = index * 10 + digit;
                digits = digits + 1;
                position = position + 1;
            }
            if (digits == 0 || index >= current.Children.Count) return null;
            current = current.Children[index];
            if (position < path.Length)
            {
                position = position + 1;
                if (position == path.Length) return null;
            }
        }
        return current;
    }

    internal string Append(string parentPath, int index)
    {
        if (parentPath == "/") return "/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return parentPath + "/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
int FindChild(SvgNode parent, SvgNode child)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (object.ReferenceEquals(parent.Children[index], child)) return index;
            index = index + 1;
        }
        return -1;
    }
}
