namespace uspto;

internal class RecoverableBulkBodyResult
{
    internal long BytesCopied = 0L;
    internal global::System.Exception? Failure = null;
    internal string? FailureCode = null;
    internal string? FailureMessage = null;
}

internal static class RecoverableBulkBodyCopier
{
    internal static RecoverableBulkBodyResult CopyRecoverableBody(global::System.Net.Http.HttpResponseMessage response, global::System.IO.Stream destination, RequestContext context, HttpTimeoutScope timeout, DownloadPolicy policy, DownloadRequest download, string? fileName, long initialBytes, long resumedBytes, long totalBytes, long expectedSegmentBytes)
    {
        RecoverableBulkBodyResult result = new RecoverableBulkBodyResult();
        global::System.Threading.CancellationTokenSource linked = global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        global::System.DateTimeOffset phaseDeadline = global::System.DateTimeOffset.UtcNow.AddMilliseconds(timeout.BodyTimeoutMilliseconds);
        global::System.IO.Stream input = response.Content.ReadAsStream(linked.Token);
        byte[] buffer = new byte[policy.BufferSize];
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.DateTimeOffset now = global::System.DateTimeOffset.UtcNow;
            global::System.DateTimeOffset deadline = phaseDeadline;
            string timeoutCode = "BODY_TIMEOUT";
            if (timeout.OverallDeadline < deadline)
            {
                deadline = timeout.OverallDeadline;
                timeoutCode = "OVERALL_TIMEOUT";
            }

            global::System.TimeSpan remaining = deadline - now;
            if (remaining <= global::System.TimeSpan.Zero)
            {
                linked.Cancel();
                linked.Dispose();
                result.Failure = TimeoutController.CreateTimeoutFailure(timeoutCode, ApiService.Odp);
                return result;
            }

            global::System.Threading.Tasks.Task<int> readTask = global::System.Threading.Tasks.Task.Run(() => input.ReadAsync(buffer, 0, buffer.Length, linked.Token).GetAwaiter().GetResult(), global::System.Threading.CancellationToken.None);
            global::System.Threading.Tasks.Task delayTask = global::System.Threading.Tasks.Task.Delay(remaining);
            global::System.Threading.Tasks.Task cancellationTask = global::System.Threading.Tasks.Task.Delay(global::System.Threading.Timeout.Infinite, context.CancellationToken);
            global::System.Threading.Tasks.Task winner = global::System.Threading.Tasks.Task.WhenAny(new global::System.Threading.Tasks.Task[] { readTask, delayTask, cancellationTask }).GetAwaiter().GetResult();
            if (context.CancellationToken.IsCancellationRequested)
            {
                linked.Cancel();
                linked.Dispose();
                context.CancellationToken.ThrowIfCancellationRequested();
            }

            if (!global::System.Object.ReferenceEquals(winner, readTask) && !readTask.IsCompleted)
            {
                linked.Cancel();
                linked.Dispose();
                result.Failure = TimeoutController.CreateTimeoutFailure(timeoutCode, ApiService.Odp);
                return result;
            }

            if (readTask.IsCanceled)
            {
                linked.Dispose();
                result.Failure = new global::System.OperationCanceledException("The bulk response body read was canceled.");
                return result;
            }

            if (readTask.IsFaulted)
            {
                result.Failure = readTask.Exception.GetBaseException();
                linked.Dispose();
                return result;
            }

            int read = readTask.GetAwaiter().GetResult();
            if (read == 0)
            {
                linked.Dispose();
                if (result.BytesCopied != expectedSegmentBytes)
                {
                    result.Failure = new global::System.IO.IOException("The bulk response ended before its declared range was complete.");
                }

                return result;
            }

            long completedBeforeChunk = initialBytes + result.BytesCopied;
            if (policy.HasMaximumBytes && completedBeforeChunk > policy.MaximumBytes - read)
            {
                linked.Dispose();
                result.FailureCode = "PAYLOAD_TOO_LARGE";
                result.FailureMessage = "The bulk download exceeds the configured size limit.";
                return result;
            }

            if (totalBytes >= 0L && completedBeforeChunk > totalBytes - read)
            {
                linked.Dispose();
                result.FailureCode = "RESPONSE_LENGTH_MISMATCH";
                result.FailureMessage = "The bulk response exceeded its verified total length.";
                return result;
            }

            destination.Write(buffer, 0, read);
            result.BytesCopied += read;
            BulkFileResumeOperation.ReportResumeProgress(download, DownloadState.Downloading, initialBytes + result.BytesCopied, resumedBytes, totalBytes, fileName);
        }
    }
}
