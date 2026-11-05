namespace log;

internal class LogScopeFrame
{
    internal LogScopeFrame? Parent;
    internal LogField[] Fields = new LogField[0];
    internal int Depth;
    internal bool IsDisposed;

    internal LogScopeFrame(LogScopeFrame? parent, LogField[] fields)
    {
        Parent = parent;
        Fields = Logger.CopyFields(fields);
        Depth = 1;
        IsDisposed = false;

        if (parent != null)
        {
            Depth = LogResult.IncrementCount(parent.Depth);
        }
    }
}
