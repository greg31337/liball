using System;
using System.IO;
using System.Threading;

namespace ftp;

internal static class TransferPump
{
    internal static FtpTransferPumpResult Execute(FtpTransferPumpRequest? request)
    {
        ValidateRequest(request);
        if (request == null || request.Source == null ||
            request.Destination == null || request.Options == null)
        {
            throw new FtpConfigurationException("A complete transfer pump request is required.", null);
        }

        Stream sourceValue = request.Source;
        Stream destinationValue = request.Destination;
        FtpTransferOptions optionsValue = request.Options;
        CancellationToken cancellationToken = optionsValue.Timeouts.CancellationToken;
        FtpDeadline deadline = FtpTimeout.StartDeadline(request.DeadlineMilliseconds);
        long startMilliseconds = Environment.TickCount64;
        FtpTransferPumpCounters counters = new FtpTransferPumpCounters(
            request.InitialLogicalBytes, request.InitialWireBytes);

        FtpResourceLifetime sourceLifetime = new FtpResourceLifetime(sourceValue);
        FtpResourceLifetime destinationLifetime = new FtpResourceLifetime(destinationValue);
        if (request.LeaveSourceOpen)
        {
            sourceLifetime.ReleaseResource();
        }

        if (request.LeaveDestinationOpen)
        {
            destinationLifetime.ReleaseResource();
        }

        FtpFaultIsolationResult transferResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            CheckOperationState(cancellationToken, deadline);
            ReportProgress(request, CreateProgress(
                request, counters.LogicalBytes, counters.WireBytes, startMilliseconds, false));
            byte[] buffer = new byte[optionsValue.BufferBytes];
            int bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
            while (bytesRead != 0)
            {
                ValidateReadCount(bytesRead, buffer.Length);
                CheckOperationState(cancellationToken, deadline);
                AddByteCount(counters.SourceBytes, bytesRead, "source");
                EnforceByteCeiling(
                    request, counters.SourceBytes, bytesRead);
                counters.SourceBytes += bytesRead;
                FtpTransferPumpChunk[] chunks = TransformBytes(
                    request.Transform, buffer, bytesRead, false);
                WriteChunks(
                    chunks,
                    destinationValue,
                    request,
                    cancellationToken,
                    deadline,
                    startMilliseconds,
                    counters);
                ReportTransferredChunk(request.Transform);
                CheckOperationState(cancellationToken, deadline);
                bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
            }

            ValidateReadCount(bytesRead, buffer.Length);

            FtpTransferPumpChunk[] finalChunks = TransformBytes(
                request.Transform, new byte[0], 0, true);
            WriteChunks(
                finalChunks,
                destinationValue,
                request,
                cancellationToken,
                deadline,
                startMilliseconds,
                counters);
            ReportTransferredChunk(request.Transform);
            CheckOperationState(cancellationToken, deadline);
            return null;
        });

        FtpFaultIsolationResult flushResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            destinationValue.Flush();
            return null;
        });
        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        ThrowTerminalFailure(
            transferResult,
            flushResult,
            destinationCleanup,
            sourceCleanup,
            cancellationToken,
            deadline);

        FtpTransferProgress completedProgress = CreateProgress(
            request, counters.LogicalBytes, counters.WireBytes, startMilliseconds, true);
        FtpFaultIsolationResult progressResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            ReportProgress(request, completedProgress);
            return null;
        });
        if (!progressResult.Succeeded)
        {
            throw new FtpTransferException("The transfer progress callback failed.", progressResult.Failure);
        }

        ThrowTerminalFailure(
            transferResult,
            flushResult,
            destinationCleanup,
            sourceCleanup,
            cancellationToken,
            deadline);

        FtpTransferPumpResult result = new FtpTransferPumpResult();
        result.SourceBytes = counters.SourceBytes;
        result.DestinationBytes = counters.DestinationBytes;
        result.Progress = completedProgress;
        return result;
    }

    internal static void WriteChunks(
        FtpTransferPumpChunk[]? chunks,
        Stream destination,
        FtpTransferPumpRequest request,
        CancellationToken cancellationToken,
        FtpDeadline deadline,
        long startMilliseconds,
        FtpTransferPumpCounters counters)
    {
        if (chunks == null)
        {
            throw new FtpTransferException("A transfer transform returned no chunk collection.", null);
        }

        int index = 0;
        while (index < chunks.Length)
        {
            CheckOperationState(cancellationToken, deadline);
            FtpTransferPumpChunk? chunk = chunks[index];
            ValidateChunk(chunk);
            if (chunk == null || chunk.Data == null)
            {
                throw new FtpTransferException("A complete transfer pump chunk is required.", null);
            }

            AddByteCount(counters.LogicalBytes, chunk.LogicalBytes, "logical");
            AddByteCount(counters.WireBytes, chunk.WireBytes, "wire");
            AddByteCount(counters.DestinationBytes, chunk.Count, "destination");
            EnforceDestinationByteCeiling(
                request, counters.DestinationBytes, chunk.Count);
            EnforceByteCeiling(
                request,
                counters.WireBytes - request.InitialWireBytes,
                chunk.WireBytes);
            ApplyBandwidthCeiling(
                request,
                counters.WireBytes - request.InitialWireBytes,
                chunk.WireBytes,
                startMilliseconds);
            if (chunk.Count != 0)
            {
                destination.Write(chunk.Data, 0, chunk.Count);
            }

            counters.LogicalBytes += chunk.LogicalBytes;
            counters.WireBytes += chunk.WireBytes;
            counters.DestinationBytes += chunk.Count;
            ReportProgress(request, CreateProgress(
                request, counters.LogicalBytes, counters.WireBytes, startMilliseconds, false));
            index++;
        }
    }

    internal static FtpTransferPumpChunk[] TransformBytes(
        IFtpTransferPumpTransform? transform,
        byte[] data,
        int count,
        bool finalChunk)
    {
        if (transform != null)
        {
            return transform.TransformChunk(data, count, finalChunk);
        }

        if (finalChunk)
        {
            return new FtpTransferPumpChunk[0];
        }

        byte[] copy = new byte[count];
        if (count != 0)
        {
            System.Array.Copy(data, 0, copy, 0, count);
        }

        FtpTransferPumpChunk chunk = new FtpTransferPumpChunk();
        chunk.Data = copy;
        chunk.Count = count;
        chunk.LogicalBytes = count;
        chunk.WireBytes = count;
        return new FtpTransferPumpChunk[] { chunk };
    }

    internal static void ReportTransferredChunk(
        IFtpTransferPumpTransform? transform)
    {
        IFtpTransferMarkerProvider? markerProvider =
            transform as IFtpTransferMarkerProvider;
        if (markerProvider != null)
        {
            markerProvider.ReportTransferredChunk();
        }
    }

    internal static FtpTransferProgress CreateProgress(
        FtpTransferPumpRequest request,
        long logicalBytes,
        long wireBytes,
        long startMilliseconds,
        bool completed)
    {
        long currentMilliseconds = Environment.TickCount64;
        long elapsedMilliseconds = 0;
        if (currentMilliseconds > startMilliseconds)
        {
            elapsedMilliseconds = currentMilliseconds - startMilliseconds;
        }

        FtpTransferProgress progress = new FtpTransferProgress();
        progress.Direction = request.Direction;
        progress.Path = request.Path;
        progress.HasExpectedBytes = request.HasExpectedBytes;
        progress.ExpectedBytes = request.ExpectedBytes;
        progress.LogicalBytes = logicalBytes;
        progress.WireBytes = wireBytes;
        progress.Elapsed = TimeSpan.FromMilliseconds(elapsedMilliseconds);
        progress.Completed = completed;
        return progress;
    }

    internal static void ReportProgress(
        FtpTransferPumpRequest request,
        FtpTransferProgress progress)
    {
        if (request.ProgressCallback != null)
        {
            request.ProgressCallback.Invoke(progress);
        }
    }

    internal static void CheckOperationState(
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        FtpCancellation.ThrowIfCancellationRequested(cancellationToken);
        FtpTimeout.ThrowIfDeadlineExpired(deadline, "data transfer");
    }

    internal static void ThrowTerminalFailure(
        FtpFaultIsolationResult transferResult,
        FtpFaultIsolationResult flushResult,
        FtpFaultIsolationResult destinationCleanup,
        FtpFaultIsolationResult sourceCleanup,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP data transfer was canceled.", transferResult.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException("The FTP data transfer timed out.", transferResult.Failure);
        }

        if (!transferResult.Succeeded)
        {
            throw new FtpTransferException("The transfer pump failed.", transferResult.Failure);
        }

        if (!flushResult.Succeeded)
        {
            throw new FtpTransferException("The transfer destination could not be flushed.", flushResult.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The transfer destination could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The transfer source could not be closed.", sourceCleanup.Failure);
        }
    }

    internal static void ValidateRequest(FtpTransferPumpRequest? request)
    {
        if (request == null || request.Source == null ||
            request.Destination == null || request.Options == null)
        {
            throw new FtpConfigurationException("A complete transfer pump request is required.", null);
        }

        if (object.ReferenceEquals(request.Source, request.Destination))
        {
            throw new FtpConfigurationException("The transfer pump requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(request.Options);
        FtpTransferValidator.ValidateDirection(request.Direction);
        if (!request.Source.CanRead)
        {
            throw new FtpConfigurationException("The transfer pump source stream must be readable.", null);
        }

        if (!request.Destination.CanWrite)
        {
            throw new FtpConfigurationException("The transfer pump destination stream must be writable.", null);
        }

        if (request.DeadlineMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException("A transfer deadline must be non-negative or Timeout.Infinite.", null);
        }

        if (request.InitialLogicalBytes < 0 || request.InitialWireBytes < 0)
        {
            throw new FtpConfigurationException("Initial transfer byte counts cannot be negative.", null);
        }

        if ((request.HasMaximumBytes && request.MaximumBytes < 0) ||
            (!request.HasMaximumBytes && request.MaximumBytes != 0) ||
            (request.HasDestinationMaximumBytes &&
                request.DestinationMaximumBytes < 0) ||
            (!request.HasDestinationMaximumBytes &&
                request.DestinationMaximumBytes != 0) ||
            (request.HasBytesPerSecond && request.BytesPerSecond <= 0) ||
            (!request.HasBytesPerSecond && request.BytesPerSecond != 0))
        {
            throw new FtpConfigurationException(
                "Transfer pump resource limits are invalid.", null);
        }

        if (request.HasExpectedBytes)
        {
            if (request.ExpectedBytes < request.InitialLogicalBytes)
            {
                throw new FtpConfigurationException("Expected transfer bytes cannot precede the initial logical position.", null);
            }
        }
        else if (request.ExpectedBytes != 0)
        {
            throw new FtpConfigurationException("An expected byte count requires its availability flag.", null);
        }
    }

    internal static void ValidateChunk(FtpTransferPumpChunk? chunk)
    {
        if (chunk == null || chunk.Data == null ||
            chunk.Count < 0 || chunk.Count > chunk.Data.Length)
        {
            throw new FtpTransferException("A transfer transform returned invalid output bounds.", null);
        }

        if (chunk.LogicalBytes < 0 || chunk.WireBytes < 0)
        {
            throw new FtpTransferException("A transfer transform returned negative byte accounting.", null);
        }
    }

    internal static void ValidateReadCount(int count, int bufferLength)
    {
        if (count < 0 || count > bufferLength)
        {
            throw new FtpTransferException("The transfer source returned an invalid read count.", null);
        }
    }

    internal static void AddByteCount(long currentCount, long additionalCount, string countName)
    {
        if (additionalCount < 0 || currentCount > long.MaxValue - additionalCount)
        {
            throw new FtpTransferException($"The transfer {countName} byte count exceeded Int64 capacity.", null);
        }
    }

    internal static void EnforceByteCeiling(
        FtpTransferPumpRequest request,
        long transferredBytes,
        long additionalBytes)
    {
        if (request.HasMaximumBytes &&
            (transferredBytes > request.MaximumBytes ||
                additionalBytes > request.MaximumBytes - transferredBytes))
        {
            throw new FtpResourceLimitException(
                "The configured transfer byte ceiling was exceeded.");
        }
    }

    internal static void ApplyBandwidthCeiling(
        FtpTransferPumpRequest request,
        long transferredBytes,
        long additionalBytes,
        long startMilliseconds)
    {
        if (!request.HasBytesPerSecond || additionalBytes == 0)
        {
            return;
        }
        long projectedBytes = transferredBytes + additionalBytes;
        long targetMilliseconds = CalculateMinimumMilliseconds(
            projectedBytes, request.BytesPerSecond);
        long elapsedMilliseconds = Environment.TickCount64 - startMilliseconds;
        while (elapsedMilliseconds < targetMilliseconds)
        {
            long delay = targetMilliseconds - elapsedMilliseconds;
            int boundedDelay = delay > int.MaxValue ? int.MaxValue : (int)delay;
            Thread.Sleep(boundedDelay);
            elapsedMilliseconds = Environment.TickCount64 - startMilliseconds;
        }
    }

    internal static void EnforceDestinationByteCeiling(
        FtpTransferPumpRequest request,
        long destinationBytes,
        long additionalBytes)
    {
        if (request.HasDestinationMaximumBytes &&
            (destinationBytes > request.DestinationMaximumBytes ||
                additionalBytes >
                    request.DestinationMaximumBytes - destinationBytes))
        {
            throw new FtpResourceLimitException(
                "The configured destination storage ceiling was exceeded.");
        }
    }

    internal static long CalculateMinimumMilliseconds(
        long byteCount,
        long bytesPerSecond)
    {
        long wholeSeconds = byteCount / bytesPerSecond;
        long remainder = byteCount % bytesPerSecond;
        if (wholeSeconds > long.MaxValue / 1000)
        {
            return long.MaxValue;
        }
        long milliseconds = wholeSeconds * 1000;
        if (remainder == 0)
        {
            return milliseconds;
        }
        long remainderMilliseconds = (long)Math.Ceiling(
            (double)remainder * 1000.0 / (double)bytesPerSecond);
        if (milliseconds > long.MaxValue - remainderMilliseconds)
        {
            return long.MaxValue;
        }
        return milliseconds + remainderMilliseconds;
    }
}
