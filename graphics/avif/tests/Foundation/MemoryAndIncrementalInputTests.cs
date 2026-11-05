namespace avif.Tests;

public class MemoryAndIncrementalInputTests
{
    [Test]
    public void MemoryInputReadsCallerOwnedSliceAndSupportsPartialReads()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        MemoryInput input = MemoryInput.Create(source, 1, 4);
        byte[] first = new byte[2];
        byte[] partial = new byte[3];

        InputReadResult firstResult = input.Read(1, first);
        source[2] = 9;
        InputReadResult viewResult = input.Read(1, first);
        InputReadResult partialResult = input.Read(3, partial);

        Assert.Multiple(() =>
        {
            Assert.That(input.GetLength(), Is.EqualTo(4));
            Assert.That(firstResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstResult.BytesRead, Is.EqualTo(2));
            Assert.That(viewResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first, Is.EqualTo(new byte[] { 9, 3 }));
            Assert.That(partialResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(partialResult.BytesRead, Is.EqualTo(1));
            Assert.That(partial, Is.EqualTo(new byte[] { 4, 0, 0 }));
        });
    }

    [Test]
    public void MemoryExactReadIsAtomicOnTruncationAndOverflow()
    {
        MemoryInput input = MemoryInput.Create(new byte[] { 1, 2, 3 });
        byte[] destination = [204, 204];

        InputReadResult truncated = input.ReadExact(2, destination);
        InputReadResult overflow = input.ReadExact(long.MaxValue, destination);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.BytesRead, Is.Zero);
            Assert.That(truncated.MissingOffset, Is.EqualTo(3));
            Assert.That(truncated.MissingLength, Is.EqualTo(1));
            Assert.That(overflow.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(destination, Is.EqualTo(new byte[] { 204, 204 }));
        });
    }

    [Test]
    public void MemoryInputCloseReleasesViewAndRejectsReads()
    {
        MemoryInput input = MemoryInput.Create(new byte[] { 1 });
        input.Close();

        InputReadResult result = input.Read(0, new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Closed));
            Assert.That(input.Data, Is.Empty);
            Assert.That(input.GetLength(), Is.Zero);
        });
    }

    [Test]
    public void IncrementalExactReadReportsMissingRangeUntilAppended()
    {
        IncrementalInput input = new IncrementalInput(2, 8, 8);
        input.Append(new byte[] { 1, 2, 3 });
        byte[] destination = [204, 204, 204];

        InputReadResult waiting = input.ReadExact(1, destination);
        byte[] waitingBytes = (byte[])destination.Clone();
        input.Append(new byte[] { 4 });
        InputReadResult complete = input.ReadExact(1, destination);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Code, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(waiting.BytesRead, Is.Zero);
            Assert.That(waiting.MissingOffset, Is.EqualTo(3));
            Assert.That(waiting.MissingLength, Is.EqualTo(1));
            Assert.That(waitingBytes, Is.EqualTo(new byte[] { 204, 204, 204 }));
            Assert.That(complete.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(complete.BytesRead, Is.EqualTo(3));
            Assert.That(destination, Is.EqualTo(new byte[] { 2, 3, 4 }));
        });
    }

    [Test]
    public void IncrementalCompletionChangesWaitingToFinalTruncation()
    {
        IncrementalInput input = new IncrementalInput(0, 8, 8);
        input.Append(new byte[] { 1, 2, 3, 4 });
        input.CompleteInput();

        InputReadResult truncated = input.ReadExact(3, new byte[2]);
        InputReadResult end = input.Read(4, new byte[1]);
        AvifResultCode appendCode = input.Append(new byte[] { 5 });

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncated.MissingOffset, Is.EqualTo(4));
            Assert.That(truncated.MissingLength, Is.EqualTo(1));
            Assert.That(end.Code, Is.EqualTo(AvifResultCode.EndOfData));
            Assert.That(appendCode, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(input.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void IncrementalGrowthIsBoundedAndPreservesAcceptedBytes()
    {
        IncrementalInput input = new IncrementalInput(1, 4, 4);
        AvifResultCode accepted = input.Append(new byte[] { 1, 2, 3, 4 });
        AvifResultCode rejected = input.Append(new byte[] { 5 });

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo(AvifResultCode.Success));
            Assert.That(rejected, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(input.Length, Is.EqualTo(4));
            Assert.That(input.Buffer[0..input.Length], Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }
}
