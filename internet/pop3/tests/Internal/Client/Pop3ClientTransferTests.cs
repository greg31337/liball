namespace pop3.Tests;

public class Pop3ClientTransferTests
{
    [Test]
    public void DispatcherSupportsEveryTransferKindInCallOrder()
    {
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);

        transfer.ReportProgress(Pop3TransferKind.List, 0, 12, -1, false);
        transfer.ReportProgress(Pop3TransferKind.Uidl, 0, 24, -1, true);
        transfer.ReportProgress(Pop3TransferKind.Retrieve, 3, 100, 120, false);
        transfer.ReportProgress(Pop3TransferKind.Top, 4, 80, 80, true);
        transfer.ReportProgress(Pop3TransferKind.ExtensionData, 0, 9, -1, true);

        Assert.That(observer.Reports, Has.Count.EqualTo(5));
        AssertProgress(observer.Reports[0], Pop3TransferKind.List, 0, 12, -1, false);
        AssertProgress(observer.Reports[1], Pop3TransferKind.Uidl, 0, 24, -1, true);
        AssertProgress(observer.Reports[2], Pop3TransferKind.Retrieve, 3, 100, 120, false);
        AssertProgress(observer.Reports[3], Pop3TransferKind.Top, 4, 80, 80, true);
        AssertProgress(observer.Reports[4], Pop3TransferKind.ExtensionData, 0, 9, -1, true);
    }

    [Test]
    public void EveryCallbackReceivesAnIndependentSnapshot()
    {
        MutatingObserver observer = new MutatingObserver();
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);

        transfer.ReportProgress(Pop3TransferKind.Retrieve, 1, 10, 20, false);
        transfer.ReportProgress(Pop3TransferKind.Retrieve, 1, 20, 20, true);

        Assert.That(observer.First, Is.Not.Null);
        Assert.That(observer.Second, Is.Not.Null);
        Assert.That(observer.Second, Is.Not.SameAs(observer.First));
        AssertProgress(observer.Second!, Pop3TransferKind.Retrieve, 1, 20, 20, true);
    }

    [Test]
    public void UnknownExpectedSizeAndLargeCountersArePreserved()
    {
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);
        long largeCount = (long)int.MaxValue + 100;

        transfer.ReportProgress(Pop3TransferKind.ExtensionData, 0, largeCount, -1, true);

        AssertProgress(observer.Reports[0], Pop3TransferKind.ExtensionData, 0, largeCount, -1, true);
    }

    [Test]
    public void NullObserverDisablesCallbacksWhileRetainingValidation()
    {
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(null);

        Assert.DoesNotThrow(() => transfer.ReportProgress(Pop3TransferKind.List, 0, 0, -1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress((Pop3TransferKind)99, 0, 0, -1, false));
    }

    [Test]
    public void DispatcherRejectsInvalidProgressMetadata()
    {
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);

        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress((Pop3TransferKind)99, 0, 0, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress(Pop3TransferKind.List, -1, 0, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress(Pop3TransferKind.Retrieve, 0, 0, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress(Pop3TransferKind.Top, 0, 0, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress(Pop3TransferKind.Uidl, 0, -1, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => transfer.ReportProgress(Pop3TransferKind.ExtensionData, 0, 0, -2, false));
        Assert.That(observer.Reports, Is.Empty);
    }

    [Test]
    public void ObserverFailureIsPropagatedImmediately()
    {
        ThrowingObserver observer = new ThrowingObserver();
        Pop3ClientTransfer transfer = new Pop3ClientTransfer(observer);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            transfer.ReportProgress(Pop3TransferKind.List, 0, 1, -1, false))!;

        Assert.That(exception.Message, Is.EqualTo("observer failed"));
        Assert.That(observer.CallCount, Is.EqualTo(1));
    }

    private static void AssertProgress(
        Pop3TransferProgress progress,
        Pop3TransferKind kind,
        int messageNumber,
        long bytesTransferred,
        long expectedBytes,
        bool isComplete)
    {
        Assert.That(progress.Kind, Is.EqualTo(kind));
        Assert.That(progress.MessageNumber, Is.EqualTo(messageNumber));
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytesTransferred));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(expectedBytes));
        Assert.That(progress.IsComplete, Is.EqualTo(isComplete));
    }

    private class RecordingObserver : IPop3TransferObserver
    {
        public List<Pop3TransferProgress> Reports = new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Reports.Add(progress);
        }
    }

    private class MutatingObserver : IPop3TransferObserver
    {
        public Pop3TransferProgress? First;
        public Pop3TransferProgress? Second;
        int CallCount;

        public void Report(Pop3TransferProgress progress)
        {
            if (CallCount == 0)
            {
                First = progress;
                progress.Kind = Pop3TransferKind.ExtensionData;
                progress.MessageNumber = 99;
                progress.BytesTransferred = 999;
                progress.ExpectedBytes = 999;
                progress.IsComplete = true;
            }
            else
            {
                Second = progress;
            }
            CallCount++;
        }
    }

    private class ThrowingObserver : IPop3TransferObserver
    {
        public int CallCount;

        public void Report(Pop3TransferProgress progress)
        {
            CallCount++;
            throw new InvalidOperationException("observer failed");
        }
    }
}
