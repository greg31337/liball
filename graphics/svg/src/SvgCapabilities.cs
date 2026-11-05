namespace svg;

/// <summary>Provides deterministic ordering for feature entries.</summary>
internal static class SvgFeatureOrdering
{
    /// <summary>Compares two feature entries in stable report order.</summary>
    /// <param name="left">The first entry.</param>
    /// <param name="right">The second entry.</param>
    /// <returns>A negative value, zero, or a positive value in report order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when either entry is <c>null</c>.</exception>
    public static int Compare(SvgFeatureEntry left, SvgFeatureEntry right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }
        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        int identifierOrder = string.Compare(left.Identifier, right.Identifier, System.StringComparison.Ordinal);
        if (identifierOrder != 0)
        {
            return identifierOrder;
        }
        int profileOrder = string.Compare(left.ProfileIdentifier, right.ProfileIdentifier, System.StringComparison.Ordinal);
        if (profileOrder != 0)
        {
            return profileOrder;
        }
        if (left.Status < right.Status)
        {
            return -1;
        }
        if (left.Status > right.Status)
        {
            return 1;
        }
        if (left.Source != null && right.Source == null)
        {
            return -1;
        }
        if (left.Source == null && right.Source != null)
        {
            return 1;
        }
        if (left.Source != null && right.Source != null)
        {
            int sourceOrder = new SvgSourceMapping().Compare(left.Source, right.Source);
            if (sourceOrder != 0)
            {
                return sourceOrder;
            }
        }
        int detailOrder = string.Compare(left.Detail, right.Detail, System.StringComparison.Ordinal);
        if (detailOrder != 0)
        {
            return detailOrder;
        }
        if (left.Sequence < right.Sequence)
        {
            return -1;
        }
        if (left.Sequence > right.Sequence)
        {
            return 1;
        }
        return 0;
    }
}
