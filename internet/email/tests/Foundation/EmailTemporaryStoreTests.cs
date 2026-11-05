using email;

namespace email.Tests;

public class EmailTemporaryStoreTests
{
    [Test]
    public void ContentAtOrBelowThresholdCompletesInOwnedMemory()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailStorageOptions options = CreateOptions(directory, 3, 20, true);
            EmailTemporaryStore store = new EmailTemporaryStore(options);
            store.Write(new byte[] { 1, 2 });
            store.WriteByte(3);

            Assert.That(store.GetLength(), Is.EqualTo(3));
            Assert.That(store.IsUsingTemporaryFile(), Is.False);
            Assert.That(Directory.GetFiles(directory), Is.Empty);

            EmailContent content = store.Complete();
            Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.Memory));
            Assert.That(content.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
            Assert.That(content.GetClearsSensitiveBytes(), Is.True);
            Assert.That(content.Materialize(3), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(store.IsCompleted(), Is.True);
            Assert.That(store.IsClosed(), Is.True);
            content.Close();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CrossingThresholdMovesExistingAndNewBytesToTemporaryFile()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailTemporaryStore store = new EmailTemporaryStore(CreateOptions(directory, 2, 20, false));
            store.Write(new byte[] { 1, 2 });
            store.Write(new byte[] { 3, 4 });

            Assert.That(store.IsUsingTemporaryFile(), Is.True);
            Assert.That(store.HasTemporaryPath(), Is.True);
            string path = store.GetTemporaryPath()!;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));

            EmailContent content = store.Complete();
            Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.TemporaryFile));
            Assert.That(content.GetFilePath(), Is.EqualTo(path));
            Assert.That(content.Materialize(4), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            store.Close();
            Assert.That(File.Exists(path), Is.True, "ownership was transferred to content");

            content.Close();
            Assert.That(File.Exists(path), Is.False);
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void EveryTwoChunkSplitProducesIdenticalSpooledContent()
    {
        byte[] value = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        int split = 0;
        while (split <= value.Length)
        {
            string directory = CreateTemporaryTestDirectory();
            try
            {
                EmailTemporaryStore store = new EmailTemporaryStore(CreateOptions(directory, 3, 20, false));
                store.Write(new ReadOnlySpan<byte>(value, 0, split));
                store.Write(new ReadOnlySpan<byte>(value, split, value.Length - split));
                EmailContent content = store.Complete();

                Assert.That(content.GetKind(), Is.EqualTo(EmailContentKind.TemporaryFile), "split " + split);
                Assert.That(content.Materialize(value.Length), Is.EqualTo(value), "split " + split);
                string path = content.GetFilePath()!;
                content.Close();
                Assert.That(File.Exists(path), Is.False, "split cleanup " + split);
            }
            finally
            {
                Directory.Delete(directory, true);
            }

            split++;
        }
    }

    [Test]
    public void ExplicitCloseDeletesUnfinishedTemporaryFileAndIsIdempotent()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailTemporaryStore store = new EmailTemporaryStore(CreateOptions(directory, 1, 20, false));
            store.Write(new byte[] { 1, 2 });
            string path = store.GetTemporaryPath()!;
            Assert.That(File.Exists(path), Is.True);

            store.Dispose();
            store.Close();

            Assert.That(store.IsClosed(), Is.True);
            Assert.That(store.IsCompleted(), Is.False);
            Assert.That(store.GetLength(), Is.EqualTo(2));
            Assert.That(store.HasTemporaryPath(), Is.False);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(Directory.GetFiles(directory), Is.Empty);
            Assert.Throws<ObjectDisposedException>(() => store.WriteByte(3));
            Assert.Throws<ObjectDisposedException>(() => store.Complete());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TemporaryByteLimitFailsBeforeMutationAndAllowsPartialCompletion()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailTemporaryStore store = new EmailTemporaryStore(CreateOptions(directory, 1, 3, false));
            store.Write(new byte[] { 1, 2 });
            string path = store.GetTemporaryPath()!;

            EmailLimitException exception = Assert.Throws<EmailLimitException>(() => store.Write(new byte[] { 3, 4 }));
            Assert.That(exception.GetLimitName(), Is.EqualTo("MaxTemporaryBytes"));
            Assert.That(exception.GetLimit(), Is.EqualTo(3));
            Assert.That(exception.GetObserved(), Is.EqualTo(4));
            Assert.That(store.GetLength(), Is.EqualTo(2));

            EmailContent content = store.Complete();
            Assert.That(content.Materialize(2), Is.EqualTo(new byte[] { 1, 2 }));
            content.Close();
            Assert.That(File.Exists(path), Is.False);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void OversizeFirstWriteDoesNotCreateTemporaryFile()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailTemporaryStore store = new EmailTemporaryStore(CreateOptions(directory, 2, 3, false));

            Assert.Throws<EmailLimitException>(() => store.Write(new byte[] { 1, 2, 3, 4 }));

            Assert.That(store.GetLength(), Is.Zero);
            Assert.That(store.IsUsingTemporaryFile(), Is.False);
            Assert.That(Directory.GetFiles(directory), Is.Empty);
            store.Close();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ZeroThresholdSpoolsFirstByteButNotEmptyCompletion()
    {
        string emptyDirectory = CreateTemporaryTestDirectory();
        string byteDirectory = CreateTemporaryTestDirectory();
        try
        {
            EmailTemporaryStore emptyStore = new EmailTemporaryStore(CreateOptions(emptyDirectory, 0, 3, false));
            EmailContent empty = emptyStore.Complete();
            Assert.That(empty.GetKind(), Is.EqualTo(EmailContentKind.Memory));
            Assert.That(empty.Materialize(0), Is.Empty);
            empty.Close();

            EmailTemporaryStore byteStore = new EmailTemporaryStore(CreateOptions(byteDirectory, 0, 3, false));
            byteStore.WriteByte(9);
            Assert.That(byteStore.IsUsingTemporaryFile(), Is.True);
            EmailContent oneByte = byteStore.Complete();
            Assert.That(oneByte.Materialize(1), Is.EqualTo(new byte[] { 9 }));
            oneByte.Close();
        }
        finally
        {
            Directory.Delete(emptyDirectory, true);
            Directory.Delete(byteDirectory, true);
        }
    }

    [Test]
    public void StoreSnapshotsOptionsAtConstruction()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailStorageOptions options = CreateOptions(directory, 1, 10, false);
            EmailTemporaryStore store = new EmailTemporaryStore(options);
            options.GetLimits().SetMemoryThresholdBytes(100);
            options.GetLimits().SetMaxTemporaryBytes(100);
            options.SetClearsSensitiveBytes(true);

            store.Write(new byte[] { 1, 2 });

            Assert.That(store.IsUsingTemporaryFile(), Is.True);
            EmailContent content = store.Complete();
            Assert.That(content.GetClearsSensitiveBytes(), Is.False);
            content.Close();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void StorageOptionsCloneDeepCopiesLimitsAndValidatesValues()
    {
        string directory = CreateTemporaryTestDirectory();
        try
        {
            EmailStorageOptions options = CreateOptions(directory, 7, 11, true);
            options.SetCopyBufferBytes(17);
            EmailStorageOptions copy = options.Clone();
            options.GetLimits().SetMemoryThresholdBytes(1);
            options.SetClearsSensitiveBytes(false);
            options.SetCopyBufferBytes(2);

            Assert.That(copy.GetLimits(), Is.Not.SameAs(options.GetLimits()));
            Assert.That(copy.GetLimits().GetMemoryThresholdBytes(), Is.EqualTo(7));
            Assert.That(copy.GetLimits().GetMaxTemporaryBytes(), Is.EqualTo(11));
            Assert.That(copy.GetTemporaryDirectory(), Is.EqualTo(Path.GetFullPath(directory)));
            Assert.That(copy.GetClearsSensitiveBytes(), Is.True);
            Assert.That(copy.GetCopyBufferBytes(), Is.EqualTo(17));
            Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
            Assert.Throws<ArgumentNullException>(() => options.SetTemporaryDirectory(null!));
            Assert.Throws<ArgumentException>(() => options.SetTemporaryDirectory(string.Empty));
            Assert.Throws<DirectoryNotFoundException>(() => options.SetTemporaryDirectory(Path.Combine(directory, "missing")));
            Assert.Throws<ArgumentOutOfRangeException>(() => options.SetCopyBufferBytes(0));
            Assert.Throws<ArgumentNullException>(() => new EmailTemporaryStore(null!));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static EmailStorageOptions CreateOptions(string directory, long threshold, long maximumTemporaryBytes, bool sensitive)
    {
        EmailTemporaryStorageLimits limits = new EmailTemporaryStorageLimits();
        limits.SetMemoryThresholdBytes(threshold);
        limits.SetMaxTemporaryBytes(maximumTemporaryBytes);
        EmailStorageOptions options = new EmailStorageOptions();
        options.SetLimits(limits);
        options.SetTemporaryDirectory(directory);
        options.SetClearsSensitiveBytes(sensitive);
        options.SetCopyBufferBytes(3);
        return options;
    }

    static string CreateTemporaryTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "email-store-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
