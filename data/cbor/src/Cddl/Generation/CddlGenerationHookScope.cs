namespace cbor;

internal class CddlGenerationHookScope : System.IDisposable
{
    internal string Phase;
    internal CddlGenerationContext Context;
    internal bool IsComplete;

    internal CddlGenerationHookScope(
        string phase,
        CddlGenerationContext context)
    {
        Phase = phase;
        Context = context;
    }

    internal void CompleteInvocation()
    {
        IsComplete = true;
    }

    /// <summary>Completes the callback scope or reports its contextual failure.</summary>
    public void Dispose()
    {
        if (!IsComplete)
        {
            throw new CddlGenerationException(
                "CDDL_GENERATION_HOOK_EXCEPTION",
                "The generation callback failed during " + Phase + ".",
                Context.Span,
                Context.Rule == null ? null : Context.Rule.Name
            );
        }
    }
}
