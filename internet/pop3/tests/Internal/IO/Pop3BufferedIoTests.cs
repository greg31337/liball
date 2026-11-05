namespace pop3.Tests;

public class Pop3BufferedIoTests
{
    [Test]
    public void InputAccumulatesOneByteFragmentsAndPreservesOrder()
    {
        FragmentingReadStream stream = new FragmentingReadStream(
            new MemoryStream(new byte[] { 10, 20, 30, 40, 50, 60 }),
            1);
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 2, 8);
        byte[] destination = new byte[4];

        Assert.That(input.EnsureBytes(6, CancellationToken.None), Is.True);
        Assert.That(input.GetAvailableCount(), Is.EqualTo(6));
        Assert.That(input.ReadByteValue(CancellationToken.None), Is.EqualTo(10));
        Assert.That(input.ReadBytes(destination, 1, 3, CancellationToken.None), Is.EqualTo(3));
        Assert.That(destination, Is.EqualTo(new byte[] { 0, 20, 30, 40 }));
        Assert.That(input.ReadByteValue(CancellationToken.None), Is.EqualTo(50));
        Assert.That(input.ReadByteValue(CancellationToken.None), Is.EqualTo(60));
        Assert.That(input.GetAvailableCount(), Is.Zero);
        Assert.That(stream.ReadCallCount, Is.EqualTo(6));
    }

    [Test]
    public void InputReportsEofAfterDeliveringBufferedBytes()
    {
        FragmentingReadStream stream = new FragmentingReadStream(
            new MemoryStream(new byte[] { 1, 2 }),
            1);
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 2, 4);
        byte[] destination = new byte[4];

        Assert.That(input.EnsureBytes(3, CancellationToken.None), Is.False);
        Assert.That(input.GetAvailableCount(), Is.EqualTo(2));
        Assert.That(input.ReadBytes(destination, 0, 4, CancellationToken.None), Is.EqualTo(2));
        Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 0, 0 }));
        Assert.That(input.ReadByteValue(CancellationToken.None), Is.EqualTo(-1));
        Assert.That(input.ReadBytes(destination, 0, 1, CancellationToken.None), Is.Zero);
    }

    [Test]
    public void InputCancellationPreventsAnotherBlockingRead()
    {
        FragmentingReadStream stream = new FragmentingReadStream(
            new MemoryStream(new byte[] { 1 }),
            1);
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 1, 1);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => input.EnsureBytes(1, source.Token));
        Assert.That(stream.ReadCallCount, Is.Zero);
    }

    [Test]
    public void InputEnforcesBufferCeilingAndCanDiscardReadAhead()
    {
        FragmentingReadStream stream = new FragmentingReadStream(
            new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }),
            4);
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 4, 4);

        Assert.That(input.EnsureBytes(2, CancellationToken.None), Is.True);
        Assert.That(input.GetAvailableCount(), Is.EqualTo(4));
        input.DiscardBufferedBytes();
        Assert.That(input.GetAvailableCount(), Is.Zero);
        Assert.That(input.ReadByteValue(CancellationToken.None), Is.EqualTo(5));
        Assert.That(input.GetUnderlyingStream(), Is.SameAs(stream));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.EnsureBytes(5, CancellationToken.None));
    }

    [Test]
    public void InputRejectsInvalidConfigurationAndDestinationRanges()
    {
        MemoryStream stream = new MemoryStream();
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 2, 4);

        Assert.Throws<ArgumentNullException>(() => new Pop3BufferedInput(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3BufferedInput(stream, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3BufferedInput(stream, 2, 1));
        Assert.Throws<ArgumentNullException>(() => input.ReadBytes(null!, 0, 0, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.ReadBytes(new byte[2], 1, 2, CancellationToken.None));
    }

    [Test]
    public void OutputBuffersCallerFragmentsAndFlushesEveryByteInOrder()
    {
        ShortWriteStream stream = new ShortWriteStream(2);
        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, 2, 4);

        output.WriteByteValue(1, CancellationToken.None);
        output.WriteBytes(new byte[] { 2, 3, 4, 5 }, 0, 4, CancellationToken.None);

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(output.GetBufferedCount(), Is.EqualTo(1));
        output.FlushOutput(CancellationToken.None);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(stream.FlushCallCount, Is.EqualTo(1));
        Assert.That(output.GetBufferedCount(), Is.Zero);
        Assert.That(output.GetUnderlyingStream(), Is.SameAs(stream));
    }

    [Test]
    public void OutputCancellationKeepsUnflushedBytesAvailableForRetry()
    {
        ShortWriteStream stream = new ShortWriteStream(3);
        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, 3, 3);
        CancellationTokenSource source = new CancellationTokenSource();
        output.WriteBytes(new byte[] { 7, 8 }, 0, 2, CancellationToken.None);
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => output.FlushOutput(source.Token));
        Assert.That(stream.ToArray(), Is.Empty);
        Assert.That(output.GetBufferedCount(), Is.EqualTo(2));

        output.FlushOutput(CancellationToken.None);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 7, 8 }));
    }

    [Test]
    public void OutputRejectsInvalidConfigurationAndSourceRanges()
    {
        MemoryStream stream = new MemoryStream();
        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, 2, 4);

        Assert.Throws<ArgumentNullException>(() => new Pop3BufferedOutput(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3BufferedOutput(stream, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pop3BufferedOutput(stream, 2, 1));
        Assert.Throws<ArgumentNullException>(() => output.WriteBytes(null!, 0, 0, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => output.WriteBytes(new byte[2], 1, 2, CancellationToken.None));
    }

}
