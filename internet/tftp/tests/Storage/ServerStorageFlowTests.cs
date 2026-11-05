using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerStorageFlowTests
{
    [TestCase(0)]
    [TestCase(73)]
    public void OpenReadPreservesExactSizeAndReturnsIndependentSnapshot(
        long knownSize)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        MemoryStream content = new MemoryStream(new byte[knownSize]);
        storage.ReadResult.Success = true;
        storage.ReadResult.Content = content;
        storage.ReadResult.OwnsContent = true;
        storage.ReadResult.HasKnownSize = true;
        storage.ReadResult.KnownSize = knownSize;
        storage.ReadResult.ErrorCode = TftpErrorCode.AccessViolation;
        storage.ReadResult.ErrorMessage = "stale";
        storage.ReadResult.CompletionToken = new object();
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);
        TftpServerRequest request = ReadRequest();

        TftpStorageOpenResult result = flow.OpenRead(
            request,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("OpenRead"));
            Assert.That(storage.Request, Is.SameAs(request));
            Assert.That(result, Is.Not.SameAs(storage.ReadResult));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Content, Is.SameAs(content));
            Assert.That(result.OwnsContent, Is.True);
            Assert.That(result.HasKnownSize, Is.True);
            Assert.That(result.KnownSize, Is.EqualTo(knownSize));
            Assert.That(result.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.CompletionToken, Is.Null);
        });

        content.Dispose();
    }

    [Test]
    public void OpenReadPreservesUnknownSizeWithoutStaleSizeValue()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        using MemoryStream content = new MemoryStream(new byte[] { 1 });
        storage.ReadResult.Success = true;
        storage.ReadResult.Content = content;
        storage.ReadResult.HasKnownSize = false;
        storage.ReadResult.KnownSize = 99;

        TftpStorageOpenResult result = new TftpServerStorageFlow(storage).OpenRead(
            ReadRequest(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasKnownSize, Is.False);
            Assert.That(result.KnownSize, Is.Zero);
        });
    }

    [Test]
    public void OpenPendingWriteCarriesWritableContentOwnershipAndToken()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        MemoryStream content = new MemoryStream();
        object completionToken = new object();
        storage.WriteResult.Success = true;
        storage.WriteResult.Content = content;
        storage.WriteResult.OwnsContent = true;
        storage.WriteResult.CompletionToken = completionToken;
        TftpServerRequest request = WriteRequest();

        TftpStorageOpenResult result =
            new TftpServerStorageFlow(storage).OpenPendingWrite(
                request,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("OpenPendingWrite"));
            Assert.That(storage.Request, Is.SameAs(request));
            Assert.That(result, Is.Not.SameAs(storage.WriteResult));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Content, Is.SameAs(content));
            Assert.That(result.OwnsContent, Is.True);
            Assert.That(result.CompletionToken, Is.SameAs(completionToken));
        });

        content.Dispose();
    }

    [TestCase(false, TftpErrorCode.FileNotFound, TftpErrorCode.FileNotFound)]
    [TestCase(true, TftpErrorCode.AccessViolation, TftpErrorCode.AccessViolation)]
    [TestCase(false, (TftpErrorCode)99, TftpErrorCode.Undefined)]
    public void OpenFailuresPreserveValidProtocolSelectionsAndNormalizeInvalidOnes(
        bool pendingWrite,
        TftpErrorCode selectedError,
        TftpErrorCode expectedError)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        TftpStorageOpenResult providerResult = pendingWrite
            ? storage.WriteResult
            : storage.ReadResult;
        providerResult.Success = false;
        providerResult.ErrorCode = selectedError;
        providerResult.ErrorMessage = "expected failure";
        providerResult.KnownSize = 15;
        providerResult.CompletionToken = new object();
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);

        TftpStorageOpenResult result;
        if (pendingWrite)
        {
            result = flow.OpenPendingWrite(
                WriteRequest(),
                CancellationToken.None);
        }
        else
        {
            result = flow.OpenRead(
                ReadRequest(),
                CancellationToken.None);
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
            Assert.That(result.ErrorMessage, Is.EqualTo("expected failure"));
            Assert.That(result.Content, Is.Null);
            Assert.That(result.HasKnownSize, Is.False);
            Assert.That(result.KnownSize, Is.Zero);
            Assert.That(result.CompletionToken, Is.Null);
        });
    }

    [Test]
    public void CommitAndAbortForwardTokensAndSnapshotSuccessfulResults()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.CommitResult.Success = true;
        storage.CommitResult.ErrorCode = TftpErrorCode.StorageExhausted;
        storage.CommitResult.ErrorMessage = "stale commit failure";
        storage.AbortResult.Success = true;
        storage.AbortResult.ErrorCode = TftpErrorCode.AccessViolation;
        storage.AbortResult.ErrorMessage = "stale abort failure";
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);
        object commitToken = new object();
        object abortToken = new object();

        TftpStorageCommitResult committed = flow.CommitWrite(
            commitToken,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("CommitWrite"));
            Assert.That(storage.CompletionToken, Is.SameAs(commitToken));
            Assert.That(committed, Is.Not.SameAs(storage.CommitResult));
            Assert.That(committed.Success, Is.True);
            Assert.That(committed.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(committed.ErrorMessage, Is.Empty);
        });

        TftpStorageCommitResult aborted = flow.AbortWrite(
            abortToken,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(storage.CompletionToken, Is.SameAs(abortToken));
            Assert.That(aborted, Is.Not.SameAs(storage.AbortResult));
            Assert.That(aborted.Success, Is.True);
            Assert.That(aborted.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(aborted.ErrorMessage, Is.Empty);
        });
    }

    [TestCase(false, TftpErrorCode.StorageExhausted,
        TftpErrorCode.StorageExhausted)]
    [TestCase(true, TftpErrorCode.AccessViolation,
        TftpErrorCode.AccessViolation)]
    [TestCase(false, (TftpErrorCode)(-1), TftpErrorCode.Undefined)]
    public void CompletionFailuresSelectSafeProtocolErrors(
        bool abort,
        TftpErrorCode selectedError,
        TftpErrorCode expectedError)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        TftpStorageCommitResult providerResult = abort
            ? storage.AbortResult
            : storage.CommitResult;
        providerResult.ErrorCode = selectedError;
        providerResult.ErrorMessage = "completion failed";
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);

        TftpStorageCommitResult result;
        if (abort)
        {
            result = flow.AbortWrite(new object(), CancellationToken.None);
        }
        else
        {
            result = flow.CommitWrite(new object(), CancellationToken.None);
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
            Assert.That(result.ErrorMessage, Is.EqualTo("completion failed"));
        });
    }

    [TestCase(0, "OpenRead")]
    [TestCase(1, "OpenRead")]
    [TestCase(2, "OpenRead")]
    public void RejectsMalformedSuccessfulReadResults(
        int malformedKind,
        string expectedOperation)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.ReadResult.Success = true;
        if (malformedKind == 1)
        {
            storage.ReadResult.Content = new StorageCapabilityStream(false, true);
        }
        else if (malformedKind == 2)
        {
            storage.ReadResult.Content = new MemoryStream();
            storage.ReadResult.HasKnownSize = true;
            storage.ReadResult.KnownSize = -1;
        }

        TftpStorageException exception = Assert.Throws<TftpStorageException>(() =>
            new TftpServerStorageFlow(storage).OpenRead(
                ReadRequest(),
                CancellationToken.None));

        Assert.That(exception.Operation, Is.EqualTo(expectedOperation));
        storage.ReadResult.Content?.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void RejectsMalformedSuccessfulPendingWriteResults(int malformedKind)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.WriteResult.Success = true;
        storage.WriteResult.CompletionToken = new object();
        if (malformedKind == 1)
        {
            storage.WriteResult.Content = new StorageCapabilityStream(true, false);
        }
        else if (malformedKind == 2)
        {
            storage.WriteResult.Content = new MemoryStream();
            storage.WriteResult.CompletionToken = null;
        }
        else if (malformedKind == 3)
        {
            storage.WriteResult.Content = new MemoryStream();
            storage.WriteResult.HasKnownSize = true;
            storage.WriteResult.KnownSize = -1;
        }

        TftpStorageException exception = Assert.Throws<TftpStorageException>(() =>
            new TftpServerStorageFlow(storage).OpenPendingWrite(
                WriteRequest(),
                CancellationToken.None));

        Assert.That(exception.Operation, Is.EqualTo("OpenPendingWrite"));
        storage.WriteResult.Content?.Dispose();
    }

    [Test]
    public void RejectsNullProviderResultsAndInvalidFlowInputs()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);
        storage.ReturnNullReadResult = true;
        storage.ReturnNullCommitResult = true;

        AssertStorageFailure(
            () => new TftpServerStorageFlow(null!),
            "CreateStorageFlow");
        AssertStorageFailure(
            () => flow.OpenRead(null!, CancellationToken.None),
            "OpenRead");
        AssertStorageFailure(
            () => flow.OpenRead(WriteRequest(), CancellationToken.None),
            "OpenRead");
        AssertStorageFailure(
            () => flow.OpenPendingWrite(ReadRequest(), CancellationToken.None),
            "OpenPendingWrite");
        AssertStorageFailure(
            () => flow.OpenRead(ReadRequest(), CancellationToken.None),
            "OpenRead");
        AssertStorageFailure(
            () => flow.CommitWrite(null!, CancellationToken.None),
            "CommitWrite");
        AssertStorageFailure(
            () => flow.CommitWrite(new object(), CancellationToken.None),
            "CommitWrite");
    }

    [Test]
    public void CancellationStopsFlowBeforeCallingProvider()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        TftpServerStorageFlow flow = new TftpServerStorageFlow(storage);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                flow.OpenRead(ReadRequest(), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                flow.OpenPendingWrite(WriteRequest(), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                flow.CommitWrite(new object(), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                flow.AbortWrite(new object(), cancellation.Token));
        });
        Assert.That(storage.CallCount, Is.Zero);
    }

    private static TftpServerRequest ReadRequest()
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Direction = TftpTransferDirection.Read;
        request.Filename = "read.bin";
        return request;
    }

    private static void AssertStorageFailure(
        Action action,
        string expectedOperation)
    {
        TftpStorageException exception =
            Assert.Throws<TftpStorageException>(() => action())!;
        Assert.That(exception.Operation, Is.EqualTo(expectedOperation));
    }

    private static TftpServerRequest WriteRequest()
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Direction = TftpTransferDirection.Write;
        request.Filename = "write.bin";
        return request;
    }
}

internal class RecordingServerStorage : ITftpServerStorage
{
    internal string LastOperation = "";
    internal TftpServerRequest? Request = null;
    internal object? CompletionToken = null;
    internal CancellationToken CancellationToken;
    internal int CallCount = 0;
    internal bool ReturnNullReadResult = false;
    internal bool ReturnNullCommitResult = false;
    internal TftpStorageOpenResult ReadResult = new TftpStorageOpenResult();
    internal TftpStorageOpenResult WriteResult = new TftpStorageOpenResult();
    internal TftpStorageCommitResult CommitResult = new TftpStorageCommitResult();
    internal TftpStorageCommitResult AbortResult = new TftpStorageCommitResult();

    public TftpStorageOpenResult OpenRead(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        LastOperation = "OpenRead";
        Request = request;
        CancellationToken = cancellationToken;
        CallCount = CallCount + 1;
        if (ReturnNullReadResult)
        {
            return null!;
        }

        return ReadResult;
    }

    public TftpStorageOpenResult OpenPendingWrite(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        LastOperation = "OpenPendingWrite";
        Request = request;
        CancellationToken = cancellationToken;
        CallCount = CallCount + 1;
        return WriteResult;
    }

    public TftpStorageCommitResult CommitWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        LastOperation = "CommitWrite";
        CompletionToken = completionToken;
        CancellationToken = cancellationToken;
        CallCount = CallCount + 1;
        if (ReturnNullCommitResult)
        {
            return null!;
        }

        return CommitResult;
    }

    public TftpStorageCommitResult AbortWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        LastOperation = "AbortWrite";
        CompletionToken = completionToken;
        CancellationToken = cancellationToken;
        CallCount = CallCount + 1;
        return AbortResult;
    }
}

internal class StorageCapabilityStream : Stream
{
    internal bool Readable;
    internal bool Writable;

    internal StorageCapabilityStream(bool readable, bool writable)
    {
        Readable = readable;
        Writable = writable;
    }

    public override bool CanRead => Readable;

    public override bool CanSeek => false;

    public override bool CanWrite => Writable;

    public override long Length => 0;

    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }
}
