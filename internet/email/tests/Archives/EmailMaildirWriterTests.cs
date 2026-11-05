using System.Text;
using email;

namespace email.Tests;

public class EmailMaildirWriterTests
{
    [Test]
    public void DeliversThroughTemporaryStorageIntoNewAndRoundTripsWithTheReader()
    {
        string root = CreateMaildir();
        try
        {
            EmailMessage message = Parse("Subject: delivered\n\nmaildir body\n");
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);

            EmailMaildirWriteResult result = writer.DeliverWithUniqueName(message, "fixed-unique", EmailMaildirFolder.New, EmailMaildirFlags.None);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetUniqueName(), Is.EqualTo("fixed-unique"));
            Assert.That(result.GetFileName(), Is.EqualTo("fixed-unique"));
            Assert.That(result.GetFolder(), Is.EqualTo(EmailMaildirFolder.New));
            Assert.That(result.GetFlags(), Is.EqualTo(EmailMaildirFlags.None));
            Assert.That(result.WasFlushedToDisk(), Is.True);
            Assert.That(result.WasOverwritten(), Is.False);
            Assert.That(result.GetCollisionCount(), Is.Zero);
            Assert.That(File.Exists(Path.Combine(root, "new", "fixed-unique")), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);
            Assert.That(writer.GetDeliveryCount(), Is.EqualTo(1));

            EmailMaildirEntry read = new EmailMaildirReaderOperations().CreateWithRootPath(root).ReadRelativePath("new/fixed-unique");
            Assert.That(read.IsSuccess(), Is.True);
            Assert.That(read.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("delivered"));
            Assert.That(Body(read), Is.EqualTo("maildir body\n"));
            Assert.That(result.GetByteCount(), Is.EqualTo(new FileInfo(result.GetPath()!).Length));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void GeneratesUniqueNamesAndPublishesCanonicalFlagsIntoCurrent()
    {
        string root = CreateMaildir();
        try
        {
            EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
            options.SetHostName("mail/host:test");
            options.SetFlushToDisk(false);
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, options);
            EmailMaildirFlags flags = EmailMaildirFlags.Trashed | EmailMaildirFlags.Seen | EmailMaildirFlags.Draft | EmailMaildirFlags.Replied;

            EmailMaildirWriteResult first = writer.DeliverToFolder(Parse("Subject: first\n\nbody\n"), EmailMaildirFolder.Current, flags);
            EmailMaildirWriteResult second = writer.DeliverToFolder(Parse("Subject: second\n\nbody\n"), EmailMaildirFolder.Current, flags);

            Assert.That(first.GetUniqueName(), Is.Not.EqualTo(second.GetUniqueName()));
            Assert.That(new EmailMaildirNameOperations().IsSafeUniqueName(first.GetUniqueName()), Is.True);
            Assert.That(first.GetFileName(), Does.EndWith(":2,DRST"));
            Assert.That(first.WasFlushedToDisk(), Is.False);
            Assert.That(Directory.GetFiles(Path.Combine(root, "cur")), Has.Length.EqualTo(2));
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Is.Empty);
            EmailMaildirName parsed = new EmailMaildirNameOperations().Parse(first.GetFileName(), EmailMaildirFolder.Current);
            Assert.That(parsed.GetFlags(), Is.EqualTo(flags));
            Assert.That(parsed.IsCanonical(), Is.True);
            Assert.Throws<ArgumentException>(() => writer.DeliverToFolder(Parse("Subject: bad\n\nbody\n"), EmailMaildirFolder.New, EmailMaildirFlags.Seen));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void AppliesFailGenerateUniqueAndExactOverwriteCollisionPolicies()
    {
        string root = CreateMaildir();
        try
        {
            new EmailMaildirWriterOperations().CreateWithRootPath(root).DeliverWithUniqueName(Parse("Subject: original\n\nold\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None);
            byte[] original = File.ReadAllBytes(Path.Combine(root, "new", "same"));

            EmailMaildirWriterOptions failOptions = new EmailMaildirWriterOptions();
            failOptions.SetCollisionPolicy(EmailMaildirCollisionPolicy.Fail);
            EmailMaildirWriter fail = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, failOptions);
            Assert.Throws<IOException>(() => fail.DeliverWithUniqueName(Parse("Subject: rejected\n\nbody\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.That(File.ReadAllBytes(Path.Combine(root, "new", "same")), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);

            EmailMaildirWriterOptions generateOptions = new EmailMaildirWriterOptions();
            generateOptions.SetCollisionPolicy(EmailMaildirCollisionPolicy.GenerateUnique);
            generateOptions.SetHostName("collision-host");
            EmailMaildirWriteResult generated = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, generateOptions).DeliverWithUniqueName(Parse("Subject: generated\n\nbody\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None);
            Assert.That(generated.GetCollisionCount(), Is.EqualTo(1));
            Assert.That(generated.GetUniqueName(), Is.Not.EqualTo("same"));
            Assert.That(File.Exists(generated.GetPath()), Is.True);

            EmailMaildirWriterOptions overwriteOptions = new EmailMaildirWriterOptions();
            overwriteOptions.SetCollisionPolicy(EmailMaildirCollisionPolicy.Overwrite);
            EmailMaildirWriteResult overwritten = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, overwriteOptions).DeliverWithUniqueName(Parse("Subject: replacement\n\nnew\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None);
            Assert.That(overwritten.WasOverwritten(), Is.True);
            Assert.That(overwritten.GetCollisionCount(), Is.Zero);
            Assert.That(new EmailMaildirReaderOperations().CreateWithRootPath(root).ReadRelativePath("new/same").GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("replacement"));
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Has.Length.EqualTo(2));

            Assert.Throws<IOException>(() => new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, overwriteOptions).DeliverWithUniqueName(Parse("Subject: moved\n\nbody\n"), "same", EmailMaildirFolder.Current, EmailMaildirFlags.Seen));
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void EnforcesLimitsBeforePublicationAndRemovesTheStagedFile()
    {
        string root = CreateMaildir();
        try
        {
            EmailMaildirWriterOptions entryOptions = OptionsWithLimits(1000, 10, 10);
            EmailMaildirWriter entryWriter = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, entryOptions);
            EmailLimitException entry = Assert.Throws<EmailLimitException>(() => entryWriter.DeliverWithUniqueName(Parse("Subject: too-large\n\nbody\n"), "large", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.That(entry.GetLimitName(), Is.EqualTo("MaxMaildirEntryBytes"));
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Is.Empty);
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);

            string existingSource = "Subject: existing\n\nbody\n";
            new EmailMaildirWriterOperations().CreateWithRootPath(root).DeliverWithUniqueName(Parse(existingSource), "existing", EmailMaildirFolder.New, EmailMaildirFlags.None);
            EmailMaildirWriter countWriter = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, OptionsWithLimits(1000, 1000, 1));
            EmailLimitException count = Assert.Throws<EmailLimitException>(() => countWriter.DeliverWithUniqueName(Parse("Subject: another\n\nbody\n"), "another", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.That(count.GetLimitName(), Is.EqualTo("MaxMaildirEntryCount"));
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);

            string nextSource = "Subject: next\n\nbody\n";
            EmailMaildirWriter archiveWriter = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, OptionsWithLimits(Bytes(existingSource).Length + Bytes(nextSource).Length - 1, 1000, 10));
            EmailLimitException archive = Assert.Throws<EmailLimitException>(() => archiveWriter.DeliverWithUniqueName(Parse(nextSource), "next", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.That(archive.GetLimitName(), Is.EqualTo("MaxMaildirArchiveBytes"));
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Has.Length.EqualTo(1));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void RejectsUnsafeNamesAndSubdirectoryLinksAndSnapshotsOptions()
    {
        string root = CreateMaildir();
        string linkedRoot = null!;
        try
        {
            EmailMaildirWriterOptions supplied = new EmailMaildirWriterOptions();
            supplied.SetCollisionPolicy(EmailMaildirCollisionPolicy.Fail);
            EmailMaildirWriter snapshot = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, supplied);
            supplied.SetCollisionPolicy(EmailMaildirCollisionPolicy.GenerateUnique);
            snapshot.DeliverWithUniqueName(Parse("Subject: first\n\nbody\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None);

            Assert.Throws<IOException>(() => snapshot.DeliverWithUniqueName(Parse("Subject: collision\n\nbody\n"), "same", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.Throws<ArgumentException>(() => snapshot.DeliverWithUniqueName(Parse("Subject: unsafe\n\nbody\n"), "../outside", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.Throws<ArgumentException>(() => snapshot.DeliverWithUniqueName(Parse("Subject: unsafe\n\nbody\n"), ".hidden", EmailMaildirFolder.New, EmailMaildirFlags.None));
            Assert.That(snapshot.GetOptions().GetCollisionPolicy(), Is.EqualTo(EmailMaildirCollisionPolicy.Fail));

            if (!OperatingSystem.IsWindows())
            {
                linkedRoot = CreateMaildir();
                Directory.Delete(Path.Combine(linkedRoot, "tmp"));
                Directory.CreateSymbolicLink(Path.Combine(linkedRoot, "tmp"), Path.Combine(root, "tmp"));
                Assert.Throws<UnauthorizedAccessException>(() => new EmailMaildirWriterOperations().CreateWithRootPath(linkedRoot));
            }
        }
        finally
        {
            if (linkedRoot != null) Directory.Delete(linkedRoot, true);
            Directory.Delete(root, true);
        }
    }

    static EmailMaildirWriterOptions OptionsWithLimits(long archiveBytes, long entryBytes, int entryCount)
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        limits.SetMaxArchiveBytes(archiveBytes);
        limits.SetMaxEntryBytes(entryBytes);
        limits.SetMaxEntryCount(entryCount);
        EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
        options.SetLimits(limits);
        return options;
    }

    static EmailMessage Parse(string value)
    {
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        return parsed.GetMessage()!;
    }

    static string CreateMaildir()
    {
        string root = Path.Combine(Path.GetTempPath(), "email-maildir-writer-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "new"));
        Directory.CreateDirectory(Path.Combine(root, "cur"));
        return root;
    }

    static string Body(EmailMaildirEntry entry)
    {
        return Encoding.UTF8.GetString(entry.GetMessage()!.GetBody()!.GetContent()!.Materialize(10000));
    }

    static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }
}
