using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class HttpResponseSender
{
    internal Task<HttpResponseMessage> SendResponseAsync(
        HttpClient client,
        HttpRequestMessage request,
        string endpoint,
        CancellationToken cancellationToken)
    {
        Task<HttpResponseMessage> sending = client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        TaskCompletionSource<HttpResponseMessage> completion = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        sending.ContinueWith(
            task => CompleteSend(task, completion, endpoint, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal Task<HttpResponseMessage> SendStreamingResponseAsync(
        HttpClient client,
        HttpRequestMessage request,
        string endpoint,
        CancellationToken cancellationToken)
    {
        Task<HttpResponseMessage> sending = client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        TaskCompletionSource<HttpResponseMessage> completion = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        sending.ContinueWith(
            task => CompleteStreamingSend(task, completion, request, endpoint, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteStreamingSend(
        Task<HttpResponseMessage> sending,
        TaskCompletionSource<HttpResponseMessage> completion,
        HttpRequestMessage request,
        string endpoint,
        CancellationToken cancellationToken)
    {
        request.Content = null;
        request.Dispose();
        CompleteSend(sending, completion, endpoint, cancellationToken);
    }

    internal void CompleteSend(
        Task<HttpResponseMessage> sending,
        TaskCompletionSource<HttpResponseMessage> completion,
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (sending.IsCanceled)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (sending.IsFaulted)
        {
            AggregateException? failure = sending.Exception;
            Exception? cause = failure == null ? null : failure.GetBaseException();
            completion.TrySetException(new JmapTransportException(
                "The HTTP request failed before a response was received.",
                endpoint,
                0,
                false,
                true,
                null,
                cause));
            return;
        }

        completion.TrySetResult(sending.Result);
    }
}
