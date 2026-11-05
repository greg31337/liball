using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class HttpContentReader
{
    internal const int BufferSize = 81920;

    internal byte[] ReadBoundedContent(
        HttpContent content,
        int maximumBytes,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        if (maximumBytes <= 0 || maximumBytes == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        if (content.Headers.ContentLength.HasValue && content.Headers.ContentLength.Value > maximumBytes)
        {
            throw TooLarge(endpoint, httpStatus);
        }

        using Stream input = GetContentStreamAsync(
            content,
            endpoint,
            httpStatus,
            cancellationToken).GetAwaiter().GetResult();
        using MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[BufferSize];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int permitted = maximumBytes - (int)output.Length + 1;
            int requested = permitted < buffer.Length ? permitted : buffer.Length;
            int read = ReadResponseStreamAsync(
                input,
                buffer,
                requested,
                endpoint,
                httpStatus,
                cancellationToken).GetAwaiter().GetResult();
            if (read == 0)
            {
                return output.ToArray();
            }

            output.Write(buffer, 0, read);
            if (output.Length > maximumBytes)
            {
                throw TooLarge(endpoint, httpStatus);
            }
        }
    }

    internal Task<Stream> GetContentStreamAsync(
        HttpContent content,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        Task<Stream> reading = content.ReadAsStreamAsync(cancellationToken);
        TaskCompletionSource<Stream> completion = new TaskCompletionSource<Stream>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        reading.ContinueWith(
            task => CompleteContentStream(task, completion, endpoint, httpStatus, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal Task<Stream> GetStreamingContentAsync(
        HttpResponseMessage response,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        Task<Stream> reading = response.Content.ReadAsStreamAsync(cancellationToken);
        TaskCompletionSource<Stream> completion = new TaskCompletionSource<Stream>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        reading.ContinueWith(
            task => CompleteStreamingContent(task, completion, response, endpoint, httpStatus, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteStreamingContent(
        Task<Stream> reading,
        TaskCompletionSource<Stream> completion,
        HttpResponseMessage response,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        if (reading.IsCanceled)
        {
            response.Dispose();
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (reading.IsFaulted)
        {
            AggregateException? failure = reading.Exception;
            Exception? cause = failure == null ? null : failure.GetBaseException();
            response.Dispose();
            completion.TrySetException(ReadFailure(endpoint, httpStatus, cause));
            return;
        }

        completion.TrySetResult(reading.Result);
    }

    internal void CompleteContentStream(
        Task<Stream> reading,
        TaskCompletionSource<Stream> completion,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        if (reading.IsCanceled)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (reading.IsFaulted)
        {
            AggregateException? failure = reading.Exception;
            Exception? cause = failure == null ? null : failure.GetBaseException();
            completion.TrySetException(ReadFailure(endpoint, httpStatus, cause));
            return;
        }

        completion.TrySetResult(reading.Result);
    }

    internal Task<int> ReadResponseStreamAsync(
        Stream input,
        byte[] buffer,
        int count,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        Task<int> reading = input.ReadAsync(buffer, 0, count, cancellationToken);
        TaskCompletionSource<int> completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        reading.ContinueWith(
            task => CompleteResponseRead(task, completion, endpoint, httpStatus, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteResponseRead(
        Task<int> reading,
        TaskCompletionSource<int> completion,
        string endpoint,
        int httpStatus,
        CancellationToken cancellationToken)
    {
        if (reading.IsCanceled)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (reading.IsFaulted)
        {
            AggregateException? failure = reading.Exception;
            Exception? cause = failure == null ? null : failure.GetBaseException();
            completion.TrySetException(ReadFailure(endpoint, httpStatus, cause));
            return;
        }

        completion.TrySetResult(reading.Result);
    }

    internal JmapTransportException TooLarge(string endpoint, int httpStatus)
    {
        return new JmapTransportException(
            "The HTTP response exceeds the configured size limit.",
            endpoint,
            httpStatus,
            true,
            false,
            null,
            null);
    }

    internal JmapTransportException ReadFailure(string endpoint, int httpStatus, Exception? cause)
    {
        return new JmapTransportException(
            "The HTTP response body could not be read completely.",
            endpoint,
            httpStatus,
            true,
            true,
            null,
            cause);
    }
}
