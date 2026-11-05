namespace json;

internal class JsonExceptionCapture
{
    global::System.Action operation;
    global::System.Exception? failure;

    internal JsonExceptionCapture(global::System.Action operation)
    {
        this.operation = operation;
    }

    internal void ExecuteOperation()
    {
        global::System.Threading.Tasks.Task task = new global::System.Threading.Tasks.Task(operation);
        task.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (task.IsFaulted)
        {
            failure = task.Exception?.InnerException
                ?? throw new global::System.InvalidOperationException("An operation failed without an exception.");
        }
    }

    internal global::System.Exception? GetFailure()
    {
        return failure;
    }
}
