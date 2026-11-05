using System.Threading;

namespace tiff;

internal class TiffOperationMonitor
{
    internal TiffProgressOperation Operation;
    internal ITiffProgress? Progress;
    internal CancellationToken CancellationToken;

    internal TiffOperationMonitor(TiffProgressOperation operation, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        Operation = operation;
        Progress = progress;
        CancellationToken = cancellationToken;
    }

    internal void Start(long totalUnits)
    {
        CheckCancellation();
        ReportProgress(0, totalUnits);
        CheckCancellation();
    }

    internal void Advance(long completedUnits, long totalUnits)
    {
        ReportProgress(completedUnits, totalUnits);
        CheckCancellation();
    }

    internal void Complete(long totalUnits)
    {
        CheckCancellation();
        ReportProgress(totalUnits, totalUnits);
    }

    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }

    internal void ReportProgress(long completedUnits, long totalUnits)
    {
        if (Progress != null)
        {
            Progress.Report(TiffProgress.Create(Operation, completedUnits, totalUnits));
        }
    }

    internal static long ConvertUnits(ulong units)
    {
        if (units > long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)units;
    }
}
