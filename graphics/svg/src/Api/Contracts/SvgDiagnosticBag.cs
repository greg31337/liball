namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDiagnosticOrdering"/>.</summary>
public interface ISvgDiagnosticOrdering
{
    /// <summary>Returns an independently allocated snapshot in stable reporting order.</summary>
    /// <param name="bag">The bounded diagnostic bag.</param>
    /// <returns>A sorted snapshot of retained diagnostics.</returns>
    System.Collections.Generic.List<SvgDiagnostic> SnapshotBag(SvgDiagnosticBag bag);
    /// <summary>Compares two diagnostics in stable reporting order.</summary>
    /// <param name="left">The first diagnostic.</param>
    /// <param name="right">The second diagnostic.</param>
    /// <returns>A negative value, zero, or a positive value according to reporting order.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when either diagnostic is <c>null</c>.</exception>
    int Compare(SvgDiagnostic left, SvgDiagnostic right);
}
