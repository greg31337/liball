namespace cbor;

internal class CddlSchemaSinkScope : System.IDisposable
{
    internal string Phase;
    internal CddlEmissionContext Context;
    internal bool IsComplete;

    internal CddlSchemaSinkScope(
        string phase,
        CddlEmissionContext context)
    {
        Phase = phase;
        Context = context;
    }

    internal void CompleteCallback()
    {
        IsComplete = true;
    }

    /// <summary>Completes the sink callback scope or reports its contextual failure.</summary>
    public void Dispose()
    {
        if (!IsComplete)
        {
            throw new CddlEmissionException(
                "CDDL_EMISSION_SINK_EXCEPTION",
                "The schema sink failed during " + Phase + ".",
                Context.Span,
                Context.Rule == null ? null : Context.Rule.Name,
                Context.RuleIndex,
                Context.NodeIndex
            );
        }
    }
}
