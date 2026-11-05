namespace pop3;

internal class Pop3ServerFailureGuard : System.IDisposable
{
    Pop3ServerEngine Engine;
    bool IsComplete;

    internal Pop3ServerFailureGuard(Pop3ServerEngine engine)
    {
        if (engine == null)
        {
            throw new System.ArgumentNullException("engine");
        }
        Engine = engine;
    }

    internal void CompleteOperation()
    {
        IsComplete = true;
    }

    public void Dispose()
    {
        if (!IsComplete)
        {
            Engine.AbortSession();
        }
    }
}
