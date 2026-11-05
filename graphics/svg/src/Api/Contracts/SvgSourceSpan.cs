namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSourceMapping"/>.</summary>
public interface ISvgSourceMapping
{
    /// <summary>Returns a source span followed by each parent reference span up to the root.</summary>
    /// <param name="span">The innermost source span.</param>
    /// <returns>A new innermost-to-outermost provenance list.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>span</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgSourceSpan> GetProvenance(SvgSourceSpan span);
    /// <summary>Compares spans in deterministic resource-path and source order.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns>A negative value, zero, or a positive value according to stable order.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when either span is <c>null</c>.</exception>
    int Compare(SvgSourceSpan left, SvgSourceSpan right);
}
