using System;
using System.Threading.Tasks;

namespace ftp;

internal static class FtpFaultIsolationRunner
{
    internal static FtpFaultIsolationResult RunOperation(Func<object?> operation)
    {
        Task<object?> task = Task.Run(operation);
        return ObserveTask(task);
    }

    internal static FtpFaultIsolationResult ObserveTask(Task<object?> task)
    {
        Task<Task<object?>> completion = Task.WhenAny(task);
        Task<object?> completedTask = completion.Result;
        FtpFaultIsolationResult result = new FtpFaultIsolationResult();

        if (completedTask.IsCanceled)
        {
            result.Canceled = true;
            return result;
        }

        if (completedTask.IsFaulted)
        {
            AggregateException? failure = completedTask.Exception;
            if (failure != null)
            {
                result.Failure = failure.GetBaseException();
            }
            else
            {
                result.Failure = new FtpProtocolException("A faulted task did not expose an exception.", null);
            }

            return result;
        }

        if (completedTask.Status == TaskStatus.RanToCompletion)
        {
            result.Succeeded = true;
            result.Value = completedTask.Result;
            return result;
        }

        result.Failure = new FtpProtocolException("The isolated task did not reach a terminal state.", null);
        return result;
    }
}
