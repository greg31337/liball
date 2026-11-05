using System.IO;

namespace avif.Tests;

public class InputSourceInterfaceTests
{
    [Test]
    public void AllInputImplementationsHonorTheSharedReadContract()
    {
        byte[] expected = new byte[] { 1, 2, 3, 4 };
        IncrementalInput incremental = new IncrementalInput(0, 4, 4);
        incremental.Append(expected);
        incremental.CompleteInput();
        IInputSource[] sources = new IInputSource[]
        {
            MemoryInput.Create(expected),
            new StreamInput(new MemoryStream(expected), false),
            incremental,
            new RangeCache(new MemoryStream(expected), false, 2, 4)
        };

        int index = 0;
        while (index < sources.Length)
        {
            byte[] destination = new byte[expected.Length];
            InputReadResult read = sources[index].ReadExact(0, destination);
            Assert.Multiple(() =>
            {
                Assert.That(read.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(read.BytesRead, Is.EqualTo(expected.Length));
                Assert.That(destination, Is.EqualTo(expected));
            });

            Assert.That(sources[index].Close(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                sources[index].Read(0, new byte[1]).Code,
                Is.EqualTo(AvifResultCode.Closed));
            index++;
        }
    }

    [Test]
    public void SharedReadAllPathAcceptsControlledSourcesAndPropagatesErrors()
    {
        ScriptedInput complete = new ScriptedInput(
            new byte[] { 5, 6, 7, 8, 9 },
            2,
            AvifResultCode.EndOfData);
        ScriptedInput waiting = new ScriptedInput(
            new byte[] { 5, 6 },
            1,
            AvifResultCode.NeedMoreData);

        AvifOneCallStreamResult completeResult = AvifOneCallCodec.ReadInput(
            complete,
            8,
            null);
        AvifOneCallStreamResult waitingResult = AvifOneCallCodec.ReadInput(
            waiting,
            8,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(completeResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(completeResult.Data, Is.EqualTo(new byte[] { 5, 6, 7, 8, 9 }));
            Assert.That(complete.ReadCallCount, Is.EqualTo(4));
            Assert.That(waitingResult.Code, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(waitingResult.Data, Is.Empty);
            Assert.That(waiting.ReadCallCount, Is.EqualTo(3));
        });
    }
}

internal class ScriptedInput : IInputSource
{
    internal byte[] Data;
    internal int MaximumReadSize;
    internal AvifResultCode TerminalCode;
    internal int ReadCallCount;
    internal bool Closed;

    internal ScriptedInput(
        byte[] data,
        int maximumReadSize,
        AvifResultCode terminalCode)
    {
        Data = data;
        MaximumReadSize = maximumReadSize;
        TerminalCode = terminalCode;
        ReadCallCount = 0;
        Closed = false;
    }

    public InputReadResult Read(long offset, Span<byte> destination)
    {
        ReadCallCount++;
        if (Closed)
        {
            return new InputReadResult(AvifResultCode.Closed, 0, 0, 0);
        }
        if (offset >= Data.Length)
        {
            return new InputReadResult(TerminalCode, 0, offset, destination.Length);
        }
        int remaining = Data.Length - (int)offset;
        int count = destination.Length < remaining ? destination.Length : remaining;
        if (count > MaximumReadSize)
        {
            count = MaximumReadSize;
        }
        Data.AsSpan((int)offset, count).CopyTo(destination);
        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    public InputReadResult ReadExact(long offset, Span<byte> destination)
    {
        if (offset < 0 || offset > Data.Length - destination.Length)
        {
            return new InputReadResult(TerminalCode, 0, offset, destination.Length);
        }
        Data.AsSpan((int)offset, destination.Length).CopyTo(destination);
        return new InputReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    public AvifResultCode Close()
    {
        Closed = true;
        return AvifResultCode.Success;
    }
}
