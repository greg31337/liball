namespace log;

/// <summary>
/// Represents contextual fields pushed onto the current asynchronous logging flow.
/// </summary>
/// <remarks>
/// Dispose removes this frame when it is current. Scopes use AsyncLocal storage, so fields follow asynchronous execution contexts.
/// </remarks>
public class LogScope : ILogScope
{
    internal LogScopeStorage Storage = new LogScopeStorage();
    internal LogScopeFrame Frame;
    internal bool IsDisposed;

    internal LogScope(LogScopeStorage storage, LogScopeFrame frame)
    {
        if (storage == null)
        {
            Storage = new LogScopeStorage();
        }
        else
        {
            Storage = storage;
        }

        Frame = frame;
        IsDisposed = false;
    }

    /// <summary>
    /// Removes this scope frame from its asynchronous context.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Storage.Pop(Frame);
    }
}
