namespace svg;

/// <summary>Provides deterministic operations for nested source provenance.</summary>
public class SvgSourceMapping : ISvgSourceMapping
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgSourceMapping()
    {
    }

    /// <summary>Returns a source span followed by each parent reference span up to the root.</summary>
    /// <param name="span">The innermost source span.</param>
    /// <returns>A new innermost-to-outermost provenance list.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>span</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgSourceSpan> GetProvenance(SvgSourceSpan span)
    {
        if (span == null)
        {
            throw new System.ArgumentNullException("span");
        }

        System.Collections.Generic.List<SvgSourceSpan> result = new System.Collections.Generic.List<SvgSourceSpan>();
        result.Add(span);
        SvgSourceUri source = span.Source;
        while (source.Parent != null)
        {
            result.Add((source.ReferenceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            source = source.Parent;
        }
        return result;
    }

    /// <summary>Compares spans in deterministic resource-path and source order.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns>A negative value, zero, or a positive value according to stable order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when either span is <c>null</c>.</exception>
    public int Compare(SvgSourceSpan left, SvgSourceSpan right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }
        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        SvgSourceUri leftRoot = left.Source;
        while (leftRoot.Parent != null)
        {
            leftRoot = leftRoot.Parent;
        }
        SvgSourceUri rightRoot = right.Source;
        while (rightRoot.Parent != null)
        {
            rightRoot = rightRoot.Parent;
        }

        int rootOrder = string.Compare(leftRoot.Uri, rightRoot.Uri, System.StringComparison.Ordinal);
        if (rootOrder != 0)
        {
            return rootOrder;
        }

        int pathOrder = string.Compare(left.Source.ResourcePath, right.Source.ResourcePath, System.StringComparison.Ordinal);
        if (pathOrder != 0)
        {
            return pathOrder;
        }

        int uriOrder = string.Compare(left.Source.Uri, right.Source.Uri, System.StringComparison.Ordinal);
        if (uriOrder != 0)
        {
            return uriOrder;
        }

        if (left.StartOffset >= 0 && right.StartOffset >= 0)
        {
            if (left.StartOffset < right.StartOffset)
            {
                return -1;
            }
            if (left.StartOffset > right.StartOffset)
            {
                return 1;
            }
            if (left.EndOffset < right.EndOffset)
            {
                return -1;
            }
            if (left.EndOffset > right.EndOffset)
            {
                return 1;
            }
            return 0;
        }

        if (left.StartLine > 0 && right.StartLine > 0)
        {
            if (left.StartLine < right.StartLine)
            {
                return -1;
            }
            if (left.StartLine > right.StartLine)
            {
                return 1;
            }
            if (left.StartColumn < right.StartColumn)
            {
                return -1;
            }
            if (left.StartColumn > right.StartColumn)
            {
                return 1;
            }
            if (left.EndLine < right.EndLine)
            {
                return -1;
            }
            if (left.EndLine > right.EndLine)
            {
                return 1;
            }
            if (left.EndColumn < right.EndColumn)
            {
                return -1;
            }
            if (left.EndColumn > right.EndColumn)
            {
                return 1;
            }
            return 0;
        }

        if (left.StartOffset >= 0 || left.StartLine > 0)
        {
            return -1;
        }
        if (right.StartOffset >= 0 || right.StartLine > 0)
        {
            return 1;
        }
        return 0;
    }
}
