using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EventSourceStreamReader
{
    internal Task<int> ReadStreamAsync(
        Stream input,
        byte[] buffer,
        string endpoint,
        CancellationToken cancellationToken)
    {
        Task<int> reading = input.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        TaskCompletionSource<int> completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        reading.ContinueWith(
            task => CompleteRead(task, completion, endpoint, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteRead(
        Task<int> reading,
        TaskCompletionSource<int> completion,
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (reading.IsCanceled || cancellationToken.IsCancellationRequested)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (reading.IsFaulted)
        {
            AggregateException? failure = reading.Exception;
            Exception? cause = failure == null ? null : failure.GetBaseException();
            completion.TrySetException(new JmapTransportException(
                "The EventSource response stream could not be read.",
                endpoint,
                200,
                true,
                true,
                null,
                cause));
            return;
        }

        completion.TrySetResult(reading.Result);
    }
}
