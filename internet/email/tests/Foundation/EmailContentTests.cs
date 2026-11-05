using email;

namespace email.Tests;

public class EmailContentTests
{
    [Test]
    public void MemoryCopyOwnsStableRepeatableContent()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(source, false);
        source[0] = 9;

        Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.Memory));
        Assert.That(content.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(content.IsRepeatable(), Is.True);
        Assert.That(content.HasKnownLength(), Is.True);
        Assert.That(content.GetLength(), Is.EqualTo(3));
        Assert.That(content.HasFilePath(), Is.False);
        Assert.That(content.GetFilePath(), Is.Null);
        Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        content.Close();
    }

    [Test]
    public void BorrowedMemoryReflectsCallerMutationAndIsNotCleared()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        EmailContent content = new EmailContentOperations().CreateMemory(source, EmailContentOwnership.Borrowed, false);
        source[1] = 8;

        Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 1, 8, 3 }));
        content.Close();

        Assert.That(source, Is.EqualTo(new byte[] { 1, 8, 3 }));
        Assert.Throws<ArgumentException>(() => new EmailContentOperations().CreateMemory(source, EmailContentOwnership.Borrowed, true));
    }

    [Test]
    public void OwnedSensitiveMemoryIsClearedOnClose()
    {
        byte[] source = new byte[] { 5, 6, 7 };
        EmailContent content = new EmailContentOperations().CreateMemory(source, EmailContentOwnership.Owned, true);

        content.Close();
        content.Close();

        Assert.That(source, Is.All.Zero);
        Assert.That(content.IsClosed(), Is.True);
        Assert.That(content.GetClearsSensitiveBytes(), Is.True);
        Assert.That(content.GetLength(), Is.EqualTo(3));
        Assert.Throws<ObjectDisposedException>(() => content.Materialize(3));
        Assert.Throws<ObjectDisposedException>(() => content.OpenRead());
    }

    [Test]
    public void FileModeOpensIndependentReadsAndBorrowedCloseKeepsFile()
    {
        string path = CreateTemporaryTestFile(new byte[] { 10, 20, 30 });
        try
        {
            EmailContent content = new EmailContentOperations().CreateFile(path, EmailContentOwnership.Borrowed);

            Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.File));
            Assert.That(content.GetOwnership(), Is.EqualTo(EmailContentOwnership.Borrowed));
            Assert.That(content.IsRepeatable(), Is.True);
            Assert.That(content.GetLength(), Is.EqualTo(3));
            Assert.That(content.HasFilePath(), Is.True);
            Assert.That(content.GetFilePath(), Is.EqualTo(Path.GetFullPath(path)));
            using (Stream first = content.OpenRead()!)
            using (Stream second = content.OpenRead()!)
            {
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.ReadByte(), Is.EqualTo(10));
                Assert.That(second!.ReadByte(), Is.EqualTo(10));
            }

            Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 10, 20, 30 }));
            content.Close();
            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void OwnedFileAndTemporaryFileAreDeletedOnClose()
    {
        string ownedPath = CreateTemporaryTestFile(new byte[] { 1 });
        string temporaryPath = CreateTemporaryTestFile(new byte[] { 2 });
        try
        {
            EmailContent owned = new EmailContentOperations().CreateFile(ownedPath, EmailContentOwnership.Owned);
            EmailContent temporary = new EmailContentOperations().CreateTemporaryFile(temporaryPath);

            Assert.That(temporary.GetKind(), Is.EqualTo(EmailContentKind.TemporaryFile));
            Assert.That(temporary.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
            owned.Dispose();
            temporary.Close();

            Assert.That(File.Exists(ownedPath), Is.False);
            Assert.That(File.Exists(temporaryPath), Is.False);
        }
        finally
        {
            File.Delete(ownedPath);
            File.Delete(temporaryPath);
        }
    }

    [Test]
    public void SeekableStreamResetsToOriginalPositionForEveryRead()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        stream.Position = 1;
        EmailContent content = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);

        Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.Stream));
        Assert.That(content.IsRepeatable(), Is.True);
        Assert.That(content.HasKnownLength(), Is.True);
        Assert.That(content.GetLength(), Is.EqualTo(3));
        Stream first = content.OpenRead()!;
        Assert.That(first!.ReadByte(), Is.EqualTo(2));
        Assert.That(first.ReadByte(), Is.EqualTo(3));
        Stream second = content.OpenRead()!;
        Assert.That(second, Is.SameAs(first));
        Assert.That(second.ReadByte(), Is.EqualTo(2));
        Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 2, 3, 4 }));

        content.Close();
        Assert.That(stream.CanRead, Is.True);
        stream.Close();
    }

    [Test]
    public void NonSeekableStreamIsUnknownLengthAndOneShot()
    {
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 4, 5, 6 });
        EmailContent content = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);

        Assert.That(content.IsRepeatable(), Is.False);
        Assert.That(content.HasKnownLength(), Is.False);
        Assert.Throws<InvalidOperationException>(() => content.GetLength());
        Assert.That(content.OpenRead(), Is.SameAs(stream));
        Assert.Throws<InvalidOperationException>(() => content.OpenRead());

        content.Close();
        Assert.That(stream.CanRead, Is.True);
        stream.Close();
    }

    [Test]
    public void OwnedStreamIsClosedButBorrowedStreamRemainsOpen()
    {
        MemoryStream ownedStream = new MemoryStream(new byte[] { 1 });
        EmailContent owned = new EmailContentOperations().CreateStream(ownedStream, EmailContentOwnership.Owned);
        owned.Close();
        Assert.That(ownedStream.CanRead, Is.False);

        MemoryStream borrowedStream = new MemoryStream(new byte[] { 1 });
        EmailContent borrowed = new EmailContentOperations().CreateStream(borrowedStream, EmailContentOwnership.Borrowed);
        borrowed.Dispose();
        Assert.That(borrowedStream.CanRead, Is.True);
        borrowedStream.Close();
    }

    [Test]
    public void MaterializationEnforcesBoundForKnownAndUnknownLengths()
    {
        EmailContent memory = new EmailContentOperations().CreateMemoryCopy(new byte[] { 1, 2, 3, 4 }, false);
        EmailLimitException knownException = Assert.Throws<EmailLimitException>(() => memory.Materialize(3));
        Assert.That(knownException.GetLimitName(), Is.EqualTo("MaxMaterializedBytes"));
        Assert.That(knownException.GetLimit(), Is.EqualTo(3));
        Assert.That(knownException.GetObserved(), Is.EqualTo(4));
        memory.Close();

        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4, 5 });
        EmailContent unknown = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Owned);
        EmailLimitException unknownException = Assert.Throws<EmailLimitException>(() => unknown.Materialize(3));
        Assert.That(unknownException.GetLimit(), Is.EqualTo(3));
        Assert.That(unknownException.GetObserved(), Is.EqualTo(5));
        unknown.Close();
    }

    [Test]
    public void MaterializationValidatesBoundsAndSupportsEmptyContent()
    {
        EmailContent empty = new EmailContentOperations().CreateMemoryCopy(Array.Empty<byte>(), false);

        Assert.That(empty.Materialize(0), Is.Empty);
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.Materialize(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.Materialize((long)int.MaxValue + 1));
        empty.Close();
    }

    [Test]
    public void SlicesAndRangeReadsPreserveOffsetsOwnershipAndSourceLifetime()
    {
        EmailContent source = new EmailContentOperations().CreateMemoryCopy(new byte[] { 10, 20, 30, 40, 50 }, false);
        EmailContent slice = new EmailContentOperations().CreateSlice(source, 1, 3);

        Assert.That(slice.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(slice.GetOwnership(), Is.EqualTo(EmailContentOwnership.Borrowed));
        Assert.That(slice.IsRepeatable(), Is.True);
        Assert.That(slice.GetLength(), Is.EqualTo(3));
        Assert.That(slice.Materialize(3), Is.EqualTo(new byte[] { 20, 30, 40 }));
        Assert.That(source.IsRangeReadStreamBorrowed(), Is.False);
        Assert.That(slice.IsRangeReadStreamBorrowed(), Is.False);

        using (Stream range = slice.OpenReadRange(1, 2))
        {
            byte[] value = new byte[2];
            range.ReadExactly(value);
            Assert.That(value, Is.EqualTo(new byte[] { 30, 40 }));
        }

        slice.Close();
        Assert.That(source.IsClosed(), Is.False);
        Assert.That(source.Materialize(5), Is.EqualTo(new byte[] { 10, 20, 30, 40, 50 }));
        source.Close();
    }

    [Test]
    public void SliceAndRangeFactoriesRejectUnavailableOrOutOfBoundsSources()
    {
        EmailContent source = new EmailContentOperations().CreateMemoryCopy(new byte[] { 1, 2, 3 }, false);
        Assert.Throws<ArgumentNullException>(() => new EmailContentOperations().CreateSlice(null, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailContentOperations().CreateSlice(source, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailContentOperations().CreateSlice(source, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailContentOperations().CreateSlice(source, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.OpenReadRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.OpenReadRange(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.OpenReadRange(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.OpenReadRange(2, 2));
        source.Close();
        Assert.Throws<ObjectDisposedException>(() => new EmailContentOperations().CreateSlice(source, 0, 0));

        EmailContent unknown = new EmailContentOperations().CreateStream(new NonSeekableReadStream(new byte[] { 1 }), EmailContentOwnership.Owned);
        Assert.Throws<ArgumentException>(() => new EmailContentOperations().CreateSlice(unknown, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => unknown.OpenReadRange(0, 0));
        unknown.Close();
    }

    [Test]
    public void FactoriesValidateOwnershipSourcesAndPaths()
    {
        MemoryStream unreadable = new MemoryStream(new byte[] { 1 });
        unreadable.Close();

        Assert.Throws<ArgumentNullException>(() => new EmailContentOperations().CreateMemory(null, EmailContentOwnership.Borrowed, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailContentOperations().CreateMemory(Array.Empty<byte>(), (EmailContentOwnership)10, false));
        Assert.Throws<ArgumentNullException>(() => new EmailContentOperations().CreateStream(null!, EmailContentOwnership.Borrowed));
        Assert.Throws<ArgumentException>(() => new EmailContentOperations().CreateStream(unreadable, EmailContentOwnership.Borrowed));
        Assert.Throws<ArgumentNullException>(() => new EmailContentOperations().CreateFile(null!, EmailContentOwnership.Borrowed));
        Assert.Throws<ArgumentException>(() => new EmailContentOperations().CreateFile(string.Empty, EmailContentOwnership.Borrowed));
        Assert.Throws<FileNotFoundException>(() => new EmailContentOperations().CreateFile(
            Path.Combine(Path.GetTempPath(), "email-content-missing-" + Guid.NewGuid().ToString("N")),
            EmailContentOwnership.Borrowed));
    }

    static string CreateTemporaryTestFile(byte[] value)
    {
        string path = Path.Combine(Path.GetTempPath(), "email-content-test-" + Guid.NewGuid().ToString("N") + ".bin");
        File.WriteAllBytes(path, value);
        return path;
    }
}

public class NonSeekableReadStream : Stream
{
    MemoryStream inner;

    public NonSeekableReadStream(byte[] value)
    {
        inner = new MemoryStream(value, false);
    }

    public override bool CanRead
    {
        get
        {
            return inner.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            return false;
        }
    }

    public override bool CanWrite
    {
        get
        {
            return false;
        }
    }

    public override long Length
    {
        get
        {
            throw new NotSupportedException();
        }
    }

    public override long Position
    {
        get
        {
            throw new NotSupportedException();
        }
        set
        {
            throw new NotSupportedException();
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return inner.Read(buffer, offset, count);
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
