namespace svg;

/// <summary>Collects diagnostics with bounded storage and strict or compatible behavior.</summary>
public class SvgDiagnosticBag
{
    /// <summary>The maximum number of diagnostics retained in memory.</summary>
    public int MaximumCount;

    /// <summary>The configured strict or compatible behavior.</summary>
    public SvgDiagnosticBehavior Behavior;

    /// <summary>The number of diagnostics omitted because of the configured bound.</summary>
    public int DroppedCount;

    /// <summary>Indicates whether any added diagnostic had error severity.</summary>
    public bool HasErrors;

    /// <summary>Indicates whether strict behavior requested processing to stop.</summary>
    public bool Stopped;

    internal System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    internal SvgDiagnostic? LimitDiagnostic;
    internal long NextSequence;

    /// <summary>Creates an empty bounded diagnostic bag.</summary>
    /// <param name="maximumCount">The nonnegative maximum retained count.</param>
    /// <param name="behavior">The error-handling behavior.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumCount</c> or <c>behavior</c> is invalid.</exception>
    public SvgDiagnosticBag(int maximumCount, SvgDiagnosticBehavior behavior)
    {
        if (maximumCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCount");
        }
        if (behavior != SvgDiagnosticBehavior.Strict && behavior != SvgDiagnosticBehavior.Compatible)
        {
            throw new System.ArgumentOutOfRangeException("behavior");
        }

        MaximumCount = maximumCount;
        Behavior = behavior;
        DroppedCount = 0;
        HasErrors = false;
        Stopped = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        LimitDiagnostic = null;
        NextSequence = 0;
    }

    /// <summary>Adds a diagnostic and reports whether processing may continue.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <returns><c>false</c> after an error in strict behavior; otherwise <c>true</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    internal bool Add(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        if (Stopped)
        {
            return false;
        }

        diagnostic.Sequence = NextSequence;
        NextSequence = NextSequence + 1;

        if (diagnostic.Severity == SvgDiagnosticSeverity.Error)
        {
            HasErrors = true;
        }

        if (MaximumCount == 0)
        {
            if (DroppedCount < int.MaxValue)
            {
                DroppedCount = DroppedCount + 1;
            }
        }
        else if (Diagnostics.Count < MaximumCount)
        {
            Diagnostics.Add(diagnostic);
        }
        else if (LimitDiagnostic == null)
        {
            Diagnostics.RemoveAt(Diagnostics.Count - 1);
            DroppedCount = 2;
            LimitDiagnostic = new SvgDiagnostic(
                SvgDiagnosticSeverity.Warning,
                SvgDiagnosticCode.DiagnosticsTruncated,
                "Diagnostic limit reached; 2 diagnostics were omitted.",
                null);
            LimitDiagnostic.Sequence = diagnostic.Sequence;
            Diagnostics.Add(LimitDiagnostic);
        }
        else
        {
            if (DroppedCount < int.MaxValue)
            {
                DroppedCount = DroppedCount + 1;
            }
            LimitDiagnostic.Message = "Diagnostic limit reached; " + DroppedCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " diagnostics were omitted.";
        }

        if (Behavior == SvgDiagnosticBehavior.Strict && diagnostic.Severity == SvgDiagnosticSeverity.Error)
        {
            Stopped = true;
            return false;
        }
        return true;
    }

    /// <summary>Creates and adds an informational diagnostic.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="source">The primary source span, or <c>null</c>.</param>
    /// <returns><c>true</c> when processing may continue.</returns>
    internal bool AddInfo(SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        return Add(new SvgDiagnostic(SvgDiagnosticSeverity.Info, code, message, source));
    }

    /// <summary>Creates and adds a warning diagnostic.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="source">The primary source span, or <c>null</c>.</param>
    /// <returns><c>true</c> when processing may continue.</returns>
    internal bool AddWarning(SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        return Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, code, message, source));
    }

    /// <summary>Creates and adds an error diagnostic.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="source">The primary source span, or <c>null</c>.</param>
    /// <returns><c>false</c> in strict behavior and <c>true</c> in compatible behavior.</returns>
    internal bool AddError(SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        return Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }

    /// <summary>Returns the number of retained diagnostics.</summary>
    /// <returns>The retained count, which never exceeds <see cref="MaximumCount"/>.</returns>
    internal int Count()
    {
        return Diagnostics.Count;
    }

    /// <summary>Reports whether processing may continue.</summary>
    /// <returns><c>false</c> only after strict behavior receives an error.</returns>
    internal bool ShouldContinue()
    {
        return !Stopped;
    }

    /// <summary>Returns an independently allocated, deterministically ordered list.</summary>
    /// <returns>A sorted snapshot of retained diagnostics.</returns>
    internal System.Collections.Generic.List<SvgDiagnostic> Snapshot()
    {
        System.Collections.Generic.List<SvgDiagnostic> result = new System.Collections.Generic.List<SvgDiagnostic>();
        int index = 0;
        while (index < Diagnostics.Count)
        {
            result.Add(Diagnostics[index]);
            index = index + 1;
        }

        int outer = 1;
        while (outer < result.Count)
        {
            SvgDiagnostic item = result[outer];
            int inner = outer - 1;
            while (inner >= 0 && new SvgDiagnosticOrdering().Compare(item, result[inner]) < 0)
            {
                result[inner + 1] = result[inner];
                inner = inner - 1;
            }
            result[inner + 1] = item;
            outer = outer + 1;
        }
        return result;
    }

    /// <summary>Clears retained diagnostics and all bag state.</summary>
    internal void Clear()
    {
        Diagnostics.Clear();
        LimitDiagnostic = null;
        DroppedCount = 0;
        HasErrors = false;
        Stopped = false;
        NextSequence = 0;
    }
}
