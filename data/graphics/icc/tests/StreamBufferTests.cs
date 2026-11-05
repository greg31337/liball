using System.Reflection;
using icc;

namespace icc.Tests;

public class StreamBufferTests
{
    [Test]
    public void SeekableStreamReadsSkipsAndAlignsAtExactOffsets()
    {
        byte[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        using MemoryStream stream = new MemoryStream(source);
        stream.Position = 2;
        object buffer = CreateStreamBuffer(stream, 3);

        byte[] first = Method<Func<int, byte[]>>(buffer, "ReadExactBytes")(3);
        Method<Action<long>>(buffer, "SkipReadBytes")(2);
        Method<Action<int, long>>(buffer, "AlignReadPosition")(4, 2);
        byte final = Method<Func<byte>>(buffer, "ReadByteExact")();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new byte[] { 2, 3, 4 }));
            Assert.That(final, Is.EqualTo(10));
            Assert.That(Method<Func<long>>(buffer, "GetAbsoluteOffset")(), Is.EqualTo(11));
            Assert.That(stream.Position, Is.EqualTo(11));
        });
    }

    [Test]
    public void NonSeekableStreamReadsAndSkipsInBoundedChunks()
    {
        byte[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        using ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(source, 2);
        object buffer = CreateStreamBuffer(stream, 3);

        byte[] first = Method<Func<int, byte[]>>(buffer, "ReadExactSlice")(5);
        Method<Action<long>>(buffer, "SkipReadBytes")(3);
        byte final = Method<Func<byte>>(buffer, "ReadByteExact")();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
            Assert.That(final, Is.EqualTo(8));
            Assert.That(Method<Func<long>>(buffer, "GetAbsoluteOffset")(), Is.EqualTo(9));
        });
    }

    [Test]
    public void ExactStreamReadsRejectTruncatedInput()
    {
        using ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(new byte[] { 1, 2 }, 1);
        object buffer = CreateStreamBuffer(stream, 2);
        Func<int, byte[]> read = Method<Func<int, byte[]>>(buffer, "ReadExactBytes");

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => read(3));
            Assert.That(Method<Func<long>>(buffer, "GetAbsoluteOffset")(), Is.EqualTo(2));
        });
    }

    [Test]
    public void StreamWritesUseZeroAlignmentAndTrackAbsoluteOffsets()
    {
        using MemoryStream stream = new MemoryStream();
        stream.Write(new byte[] { 1, 2 });
        object buffer = CreateStreamBuffer(stream, 3);

        Method<Action<byte[]>>(buffer, "WriteExactBytes")(new byte[] { 9, 8, 7 });
        Method<Action<int, long>>(buffer, "AlignWritePosition")(4, 2);
        Method<Action<byte>>(buffer, "WriteByteExact")(6);

        Assert.Multiple(() =>
        {
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 9, 8, 7, 0, 6 }));
            Assert.That(Method<Func<long>>(buffer, "GetAbsoluteOffset")(), Is.EqualTo(7));
        });
    }

    private static object CreateStreamBuffer(Stream stream, int bufferSize)
    {
        Type type = typeof(IccProfile).Assembly.GetType("icc.StreamBuffer")
            ?? throw new InvalidOperationException("StreamBuffer was not found in the production assembly.");
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new object[] { stream, bufferSize },
            null) ?? throw new InvalidOperationException("StreamBuffer could not be created.");
    }

    private static MethodInfo InternalMethod(object instance, string name)
    {
        return instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} was not found.");
    }

    private static T Method<T>(object instance, string name) where T : Delegate
    {
        return InternalMethod(instance, name).CreateDelegate<T>(instance);
    }

    private class ChunkedNonSeekableStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly int maximumRead;

        public ChunkedNonSeekableStream(byte[] bytes, int maximumRead)
        {
            inner = new MemoryStream(bytes);
            this.maximumRead = maximumRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return inner.Read(buffer, offset, Math.Min(count, maximumRead));
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
