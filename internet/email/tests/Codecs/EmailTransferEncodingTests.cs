using System.Text;
using email;

namespace email.Tests;

public class EmailTransferEncodingTests
{
    [Test]
    public void SevenBitAcceptsCanonicalShortCrlfLines()
    {
        byte[] input = Encoding.ASCII.GetBytes("abcd\r\nx\r\n");

        EmailTransferValidationResult result = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            input,
            EmailTransferEncodingKind.SevenBit,
            input.Length,
            4);

        Assert.That(result.GetKind(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(input.Length));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(input.Length));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void SevenBitDiagnosesNulAndHighBitBytesAtExactOffsets()
    {
        byte[] input = new byte[] { 65, 0, 127, 128, 255 };

        EmailTransferValidationResult result = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            input,
            EmailTransferEncodingKind.SevenBit,
            input.Length,
            10);

        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(3));
        AssertIssue(result.GetIssues().GetAt(0), EmailIssueCode.TransferEncodingNullByte, 1, 1, 2);
        AssertIssue(result.GetIssues().GetAt(1), EmailIssueCode.TransferEncodingHighBitByte, 3, 1, 4);
        AssertIssue(result.GetIssues().GetAt(2), EmailIssueCode.TransferEncodingHighBitByte, 4, 1, 5);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.False);
    }

    [Test]
    public void EightBitAllowsHighBitBytesButStillRejectsNul()
    {
        byte[] valid = new byte[] { 128, 200, 255, 13, 10 };
        byte[] invalid = new byte[] { 128, 0, 255 };

        EmailTransferValidationResult validResult = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            valid,
            EmailTransferEncodingKind.EightBit,
            valid.Length,
            10);
        EmailTransferValidationResult invalidResult = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            invalid,
            EmailTransferEncodingKind.EightBit,
            invalid.Length,
            10);

        Assert.That(validResult.IsSuccess(), Is.True);
        Assert.That(validResult.GetIssues().GetCount(), Is.Zero);
        Assert.That(invalidResult.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(invalidResult.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.TransferEncodingNullByte));
    }

    [Test]
    public void BinaryAcceptsEveryByteAndIgnoresLineConstraints()
    {
        byte[] input = new byte[256];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)index;
            index++;
        }

        EmailTransferValidationResult result = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            input,
            EmailTransferEncodingKind.Binary,
            input.Length,
            1);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(input.Length));
    }

    [Test]
    public void NonCanonicalLineBreaksAreReportedAtTheOffendingByte()
    {
        byte[] input = Encoding.ASCII.GetBytes("a\rb\nc\r");

        EmailTransferValidationResult result = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            input,
            EmailTransferEncodingKind.EightBit,
            input.Length,
            10);

        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(3));
        AssertIssue(result.GetIssues().GetAt(0), EmailIssueCode.TransferEncodingNonCanonicalLineBreak, 1, 1, 2);
        AssertIssue(result.GetIssues().GetAt(1), EmailIssueCode.TransferEncodingNonCanonicalLineBreak, 3, 2, 2);
        AssertIssue(result.GetIssues().GetAt(2), EmailIssueCode.TransferEncodingNonCanonicalLineBreak, 5, 3, 2);
    }

    [Test]
    public void OverlongLineProducesOneIssueAtItsFirstExcessByte()
    {
        byte[] input = Encoding.ASCII.GetBytes("abcdef\r\nxy");

        EmailTransferValidationResult result = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(
            input,
            EmailTransferEncodingKind.SevenBit,
            input.Length,
            3);

        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        AssertIssue(result.GetIssues().GetAt(0), EmailIssueCode.TransferEncodingLineTooLong, 3, 1, 4);
    }

    [Test]
    public void IncrementalIdentityCopyIsIndependentOfEveryTwoChunkSplit()
    {
        byte[] input = Encoding.ASCII.GetBytes("ab\r\ncd\r\nef");
        int split = 0;
        while (split <= input.Length)
        {
            EmailTransferValidator validator = new EmailTransferValidator(
                EmailTransferEncodingKind.SevenBit,
                input.Length,
                2);
            List<byte> copied = new List<byte>();
            copied.AddRange(validator.Feed(new ReadOnlySpan<byte>(input, 0, split)));
            copied.AddRange(validator.Feed(new ReadOnlySpan<byte>(input, split, input.Length - split)));
            validator.Complete();

            Assert.That(copied.ToArray(), Is.EqualTo(input), "split " + split);
            Assert.That(validator.GetResult().GetIssues().GetCount(), Is.Zero, "split " + split);
            split++;
        }
    }

    [Test]
    public void ByteLimitReturnsOnlyTheDeterministicPrefixAndConsumesTheChunk()
    {
        EmailTransferValidator validator = new EmailTransferValidator(
            EmailTransferEncodingKind.Binary,
            3,
            10);

        byte[] first = validator.Feed(Encoding.ASCII.GetBytes("abcdef"));
        byte[] second = validator.Feed(Encoding.ASCII.GetBytes("gh"));
        validator.Complete();
        EmailTransferValidationResult result = validator.GetResult();

        Assert.That(Encoding.ASCII.GetString(first), Is.EqualTo("abc"));
        Assert.That(second, Is.Empty);
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(8));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(3));
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.TransferEncodingByteLimitExceeded));
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(3));
    }

    [Test]
    public void StreamCopyPreservesBytesAndUpdatesProgressPerChunk()
    {
        byte[] inputBytes = new byte[256];
        int index = 0;
        while (index < inputBytes.Length)
        {
            inputBytes[index] = (byte)index;
            index++;
        }

        using MemoryStream input = new MemoryStream(inputBytes, false);
        using MemoryStream output = new MemoryStream();
        EmailProgress progress = new EmailProgressOperations().CreateWith();

        EmailTransferValidationResult result = new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(
            input,
            output,
            EmailTransferEncodingKind.Binary,
            inputBytes.Length,
            1,
            7,
            null,
            progress);

        Assert.That(output.ToArray(), Is.EqualTo(inputBytes));
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(progress.GetSourceBytesRead(), Is.EqualTo(inputBytes.Length));
        Assert.That(progress.GetOutputBytesWritten(), Is.EqualTo(inputBytes.Length));
        Assert.That(progress.GetDecodedBytesProduced(), Is.EqualTo(inputBytes.Length));
        Assert.That(progress.GetOperationCount(), Is.EqualTo(1));
    }

    [Test]
    public void StreamCopyReportsValidationErrorsWithoutChangingBytes()
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes("one\ntwo");
        using MemoryStream input = new MemoryStream(inputBytes, false);
        using MemoryStream output = new MemoryStream();

        EmailTransferValidationResult result = new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytes(
            input,
            output,
            EmailTransferEncodingKind.EightBit,
            inputBytes.Length);

        Assert.That(output.ToArray(), Is.EqualTo(inputBytes));
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.TransferEncodingNonCanonicalLineBreak));
    }

    [Test]
    public void PreRequestedCancellationReturnsAnEmptyPartialResult()
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes("not read");
        using MemoryStream input = new MemoryStream(inputBytes, false);
        using MemoryStream output = new MemoryStream();
        EmailCancellation cancellation = new EmailCancellation();
        EmailProgress progress = new EmailProgressOperations().CreateWith();
        cancellation.Cancel();

        EmailTransferValidationResult result = new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(
            input,
            output,
            EmailTransferEncodingKind.Binary,
            inputBytes.Length,
            10,
            2,
            cancellation,
            progress);

        Assert.That(input.Position, Is.Zero);
        Assert.That(output.Length, Is.Zero);
        Assert.That(result.IsCancelled(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.OperationCancelled));
        Assert.That(progress.GetOperationCount(), Is.EqualTo(1));
    }

    [Test]
    public void CancellationAfterAReadReturnsOnlyTheCompletedChunk()
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes("abcdef");
        EmailCancellation cancellation = new EmailCancellation();
        using CancellingReadStream input = new CancellingReadStream(inputBytes, cancellation);
        using MemoryStream output = new MemoryStream();

        EmailTransferValidationResult result = new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(
            input,
            output,
            EmailTransferEncodingKind.SevenBit,
            inputBytes.Length,
            10,
            3,
            cancellation,
            null);

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo("abc"));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(3));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(3));
        Assert.That(result.IsCancelled(), Is.True);
        Assert.That(result.GetIssues().GetAt(0).GetCode(), Is.EqualTo(EmailIssueCode.OperationCancelled));
    }

    [Test]
    public void InterfaceCollaboratorsMakeCancellationAndProgressDeterministic()
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes("abcdef");
        using MemoryStream input = new MemoryStream(inputBytes, false);
        using MemoryStream output = new MemoryStream();
        CheckCountCancellation cancellation = new CheckCountCancellation(2);
        RecordingProgress progress = new RecordingProgress();

        EmailTransferValidationResult result = new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(
            input,
            output,
            EmailTransferEncodingKind.SevenBit,
            inputBytes.Length,
            10,
            3,
            cancellation,
            progress);

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo("abc"));
        Assert.That(result.IsCancelled(), Is.True);
        Assert.That(cancellation.GetCheckCount(), Is.EqualTo(2));
        Assert.That(progress.GetSourceBytesRead(), Is.EqualTo(3));
        Assert.That(progress.GetOutputBytesWritten(), Is.EqualTo(3));
        Assert.That(progress.GetDecodedBytesProduced(), Is.EqualTo(3));
        Assert.That(progress.GetOperationCount(), Is.EqualTo(1));
    }

    [Test]
    public void ValidatorLifecycleAndIssueSnapshotsAreExplicit()
    {
        EmailTransferValidator validator = new EmailTransferValidator(
            EmailTransferEncodingKind.SevenBit,
            10,
            10);
        validator.Feed(new byte[] { 128 });
        EmailIssueList snapshot = validator.GetIssues();
        snapshot.Clear();

        Assert.Throws<InvalidOperationException>(() => validator.GetResult());
        validator.Complete();
        EmailTransferValidationResult result = validator.GetResult();

        Assert.That(validator.GetKind(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(validator.GetMaximumBytes(), Is.EqualTo(10));
        Assert.That(validator.GetMaximumLineBytes(), Is.EqualTo(10));
        Assert.That(validator.GetSourceByteCount(), Is.EqualTo(1));
        Assert.That(validator.GetCopiedByteCount(), Is.EqualTo(1));
        Assert.That(validator.IsCancelled(), Is.False);
        Assert.That(validator.IsComplete(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => validator.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => validator.Complete());
        Assert.Throws<InvalidOperationException>(() => validator.Cancel());

        EmailTransferValidator cancelled = new EmailTransferValidator(EmailTransferEncodingKind.Binary, 10, 10);
        cancelled.Cancel();
        Assert.That(cancelled.IsCancelled(), Is.True);
        Assert.That(cancelled.GetSourceByteCount(), Is.Zero);
    }

    [Test]
    public void ArgumentsAndResultOwnershipAreValidated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTransferValidator((EmailTransferEncodingKind)10, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTransferValidator(EmailTransferEncodingKind.Binary, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTransferValidator(EmailTransferEncodingKind.Binary, 1, 0));
        Assert.Throws<ArgumentNullException>(() => new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytes(null!, new MemoryStream(), EmailTransferEncodingKind.Binary, 1));
        Assert.Throws<ArgumentNullException>(() => new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytes(new MemoryStream(), null!, EmailTransferEncodingKind.Binary, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(
            new MemoryStream(),
            new MemoryStream(),
            EmailTransferEncodingKind.Binary,
            1,
            1,
            0,
            null,
            null));
        Assert.Throws<ArgumentNullException>(() => new EmailTransferValidationResult(
            EmailTransferEncodingKind.Binary,
            0,
            0,
            false,
            null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailTransferValidationResult(
            EmailTransferEncodingKind.Binary,
            1,
            2,
            false,
            new EmailIssueList()));

        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.ValidationFailed, "test"));
        EmailTransferValidationResult result = new EmailTransferValidationResult(
            EmailTransferEncodingKind.Binary,
            1,
            1,
            false,
            issues);
        issues.Clear();
        EmailIssueList returned = result.GetIssues();
        returned.Clear();
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
    }

    static void AssertIssue(EmailIssue issue, EmailIssueCode code, long offset, long line, long column)
    {
        Assert.That(issue.GetCode(), Is.EqualTo(code));
        Assert.That(issue.GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(issue.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(offset));
        Assert.That(issue.GetSourcePosition()!.GetLineNumber(), Is.EqualTo(line));
        Assert.That(issue.GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(column));
    }
}

public class CancellingReadStream : MemoryStream
{
    EmailCancellation cancellation;

    public CancellingReadStream(byte[] value, EmailCancellation cancellation) : base(value, false)
    {
        this.cancellation = cancellation;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        cancellation.Cancel();
        return read;
    }
}

public class CheckCountCancellation : IEmailCancellation
{
    int cancelAtCheck;
    int checkCount;

    public CheckCountCancellation(int cancelAtCheck)
    {
        this.cancelAtCheck = cancelAtCheck;
    }

    public bool IsCancellationRequested()
    {
        checkCount++;
        return checkCount >= cancelAtCheck;
    }

    public int GetCheckCount()
    {
        return checkCount;
    }
}

public class RecordingProgress : IEmailProgress
{
    long sourceBytesRead;
    long outputBytesWritten;
    long decodedBytesProduced;
    long headerCount;
    long entityCount;
    long operationCount;

    public void AddSourceBytesRead(long value) { sourceBytesRead += value; }
    public void AddOutputBytesWritten(long value) { outputBytesWritten += value; }
    public void AddDecodedBytesProduced(long value) { decodedBytesProduced += value; }
    public void AddHeaderCount(long value) { headerCount += value; }
    public void AddEntityCount(long value) { entityCount += value; }
    public void AddOperationCount(long value) { operationCount += value; }
    public long GetSourceBytesRead() { return sourceBytesRead; }
    public long GetOutputBytesWritten() { return outputBytesWritten; }
    public long GetDecodedBytesProduced() { return decodedBytesProduced; }
    public long GetHeaderCount() { return headerCount; }
    public long GetEntityCount() { return entityCount; }
    public long GetOperationCount() { return operationCount; }
}
