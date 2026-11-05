using System;
using System.Threading.Tasks;

namespace email;

internal static class EmailOperationCapture
{
    internal static EmailCapturedOperation RunWithOperationUsingAction(Action operation)
    {
        if (operation == null) throw new ArgumentNullException("operation");
        Task task = new Task(operation);
        task.RunSynchronously(TaskScheduler.Default);
        return task.IsCompletedSuccessfully ? EmailCapturedOperation.SuccessWith() : EmailCapturedOperation.Failure((task.Exception ?? throw new InvalidOperationException()).GetBaseException());
    }

    internal static EmailCapturedOperation RunWithOperationUsingFunc(Func<object?> operation)
    {
        if (operation == null) throw new ArgumentNullException("operation");
        Task<object?> task = new Task<object?>(operation);
        task.RunSynchronously(TaskScheduler.Default);
        return task.IsCompletedSuccessfully ? EmailCapturedOperation.SuccessWithValue(task.Result) : EmailCapturedOperation.Failure((task.Exception ?? throw new InvalidOperationException()).GetBaseException());
    }
}

internal class EmailCapturedOperation
{
    bool success;
    Exception? exception;
    object? value;

    internal EmailCapturedOperation(bool success, object? value, Exception? exception) { this.success = success; this.value = value; this.exception = exception; }
    internal static EmailCapturedOperation SuccessWith() { return new EmailCapturedOperation(true, null, null); }
    internal static EmailCapturedOperation SuccessWithValue(object? value) { return new EmailCapturedOperation(true, value, null); }
    internal static EmailCapturedOperation Failure(Exception exception) { return new EmailCapturedOperation(false, null, exception); }
    internal bool IsSuccess() { return success; }
    internal object? GetValue() { return value; }
    internal Exception? GetException() { return exception; }
}
