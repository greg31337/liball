namespace svg;

/// <summary>Compares diagnostics using deterministic source and content ordering.</summary>
public class SvgDiagnosticOrdering : ISvgDiagnosticOrdering
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDiagnosticOrdering()
    {
    }

    /// <summary>Returns an independently allocated snapshot in stable reporting order.</summary>
    /// <param name="bag">The bounded diagnostic bag.</param>
    /// <returns>A sorted snapshot of retained diagnostics.</returns>
    public System.Collections.Generic.List<SvgDiagnostic> SnapshotBag(SvgDiagnosticBag bag)
    {
        if (bag == null) throw new System.ArgumentNullException("bag");
        return bag.Snapshot();
    }

    /// <summary>Compares two diagnostics in stable reporting order.</summary>
    /// <param name="left">The first diagnostic.</param>
    /// <param name="right">The second diagnostic.</param>
    /// <returns>A negative value, zero, or a positive value according to reporting order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when either diagnostic is <c>null</c>.</exception>
    public int Compare(SvgDiagnostic left, SvgDiagnostic right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }
        if (right == null)
        {
            throw new System.ArgumentNullException("right");
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

        if (left.Severity > right.Severity)
        {
            return -1;
        }
        if (left.Severity < right.Severity)
        {
            return 1;
        }

        int leftCode = (int)left.Code;
        int rightCode = (int)right.Code;
        if (leftCode < rightCode)
        {
            return -1;
        }
        if (leftCode > rightCode)
        {
            return 1;
        }

        int messageOrder = string.Compare(left.Message, right.Message, System.StringComparison.Ordinal);
        if (messageOrder != 0)
        {
            return messageOrder;
        }

        int instanceOrder = string.Compare(left.InstancePath, right.InstancePath, System.StringComparison.Ordinal);
        if (instanceOrder != 0)
        {
            return instanceOrder;
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
