using System.Text;
using email;

namespace email.Tests;

public class EmailArchiveApiCompletionTests
{
    [Test]
    public void FileAndInMemoryIndexOperationsExposePathsHashesAndSummaries()
    {
        EmailMessage message = Parse("Subject: indexed\r\nMessage-ID: <indexed@example.test>\r\n\r\nbody\r\n");
        EmailArchiveIndexOptions indexOptions = new EmailArchiveIndexOptions();
        Assert.That(indexOptions.GetSelectedHeaderNameCount(), Is.GreaterThan(0));
        Assert.That(indexOptions.GetSelectedHeaderNameAt(0), Is.Not.Empty);
        List<string> selectedNames = indexOptions.GetSelectedHeaderNames();
        string removed = selectedNames[0];
        Assert.That(indexOptions.RemoveSelectedHeaderName(removed), Is.True);
        Assert.That(indexOptions.ContainsSelectedHeaderName(removed), Is.False);

        EmailArchiveIndex index = new EmailArchiveIndex();
        EmailArchiveIndexRecord memory = index.CreateWithMessage(message);
        Assert.That(memory.HasSourcePath(), Is.False);
        Assert.That(memory.HasMessageRange(), Is.True);
        Assert.That(memory.GetMessageRange(), Is.Not.Null);
        Assert.That(memory.HasSemanticHash(), Is.True);
        Assert.That(memory.GetSemanticHash(), Is.Not.Null);
        Assert.That(memory.HasSummary(), Is.True);
        Assert.That(memory.GetSummary()!.GetPartCount(), Is.EqualTo(1));

        string directory = CreateDirectory("file");
        string path = Path.Combine(directory, "message.eml");
        try
        {
            EmailFileSaveResult saved = new EmailFile().Save(message, path, new EmailFileSaveOptions());
            Assert.That(saved.GetPath(), Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(saved.GetWriteResult().IsSuccess(), Is.True);
            EmailFileLoadResult loaded = new EmailFile().LoadWithPath(path);
            Assert.That(loaded.GetLastWriteTimeUtc().UtcDateTime, Is.EqualTo(File.GetLastWriteTimeUtc(path)).Within(TimeSpan.FromSeconds(1)));
            Assert.That(loaded.HasMessage(), Is.True);
            Assert.That(loaded.GetMessage(), Is.Not.Null);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MboxReadWriteAndIndexConveniencesRetainRangesAndEnvelopeMetadata()
    {
        EmailMessage message = Parse("Subject: mbox\n\nFrom body\n");
        MemoryStream stream = new MemoryStream();
        EmailMboxWriterOptions writerOptions = new EmailMboxWriterOptions();
        writerOptions.SetEnvelopeSender("sender@example.test");
        writerOptions.ClearEnvelopeSender();
        writerOptions.SetEnvelopeSender("sender@example.test");
        EmailMboxWriter writer = new EmailMboxWriter(stream, writerOptions);

        EmailMboxWriteResult written = writer.Write(message);
        Assert.That(written.GetIndex(), Is.Zero);
        Assert.That(written.WasWritten(), Is.True);
        Assert.That(written.HasArchiveRange(), Is.True);
        Assert.That(written.HasEnvelopeLineRange(), Is.True);
        Assert.That(written.HasMessageRange(), Is.True);
        Assert.That(Encoding.ASCII.GetString(written.GetEnvelopeLine()!), Does.StartWith("From sender@example.test "));
        Assert.That(written.GetMessageWriteResult().IsSuccess(), Is.True);
        Assert.That(writer.GetWrittenArchiveByteCount(), Is.EqualTo(stream.Length));
        Assert.That(writer.IsClosed(), Is.False);
        writer.Dispose();
        Assert.That(writer.IsClosed(), Is.True);

        EmailMboxReader reader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(stream.ToArray(), false));
        Assert.That(reader.HasCurrent(), Is.False);
        Assert.That(reader.ReadNext(), Is.True);
        EmailMboxEntry entry = reader.GetCurrent()!;
        Assert.That(reader.HasCurrent(), Is.True);
        Assert.That(entry.HasEnvelopeLineRange(), Is.True);
        Assert.That(entry.HasEnvelopeSenderText(), Is.True);
        Assert.That(entry.HasEnvelopeDateText(), Is.True);

        EmailArchiveIndex index = new EmailArchiveIndex();
        Assert.That(index.CreateWithEntryUsingEmailMboxEntry(entry).GetSourceKind(), Is.EqualTo(EmailArchiveSourceKind.Mbox));
        Assert.That(index.CreateWithEntryAndOptionsUsingEmailMboxEntryAndEmailArchiveIndexOptions(entry, new EmailArchiveIndexOptions()).GetMessageByteCount(), Is.EqualTo(entry.GetUnescapedMessageByteCount()));
        Assert.That(index.CreateWithEntryAndSourcePathAndOptions(entry, "archive.mbox", new EmailArchiveIndexOptions()).GetSourcePath(), Is.EqualTo("archive.mbox"));
        reader.Dispose();
        Assert.That(reader.IsClosed(), Is.True);
    }

    [Test]
    public void MaildirConveniencesPublishReadMoveAndIndexARealEntry()
    {
        string root = CreateMaildir();
        try
        {
            EmailMaildirNameOperations names = new EmailMaildirNameOperations();
            string generatedName = names.CreateUniqueNameWith();
            Assert.That(names.IsSafeUniqueName(generatedName), Is.True);

            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);
            Assert.That(writer.GetRootPath(), Is.EqualTo(Path.GetFullPath(root)));
            EmailMaildirWriteResult written = writer.Deliver(Parse("Subject: maildir\n\nbody\n"));
            Assert.That(written.HasFileName(), Is.True);
            Assert.That(written.HasPath(), Is.True);
            Assert.That(written.GetMessageWriteResult().IsSuccess(), Is.True);
            Assert.That(written.WasPublished(), Is.True);

            EmailMaildirReader reader = new EmailMaildirReaderOperations().CreateWithRootPath(root);
            Assert.That(reader.GetRootPath(), Is.EqualTo(Path.GetFullPath(root)));
            Assert.That(reader.GetOptions(), Is.Not.Null);
            List<EmailMaildirEntry> entries = reader.GetEntries();
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].GetFlags(), Is.EqualTo(EmailMaildirFlags.None));
            Assert.That(reader.HasCurrent(), Is.False);
            Assert.That(reader.ReadNext(), Is.True);
            Assert.That(reader.HasCurrent(), Is.True);

            EmailArchiveIndex index = new EmailArchiveIndex();
            EmailArchiveIndexRecord indexed = index.CreateWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(entries[0], new EmailArchiveIndexOptions());
            Assert.That(indexed.GetSourcePath(), Is.EqualTo(entries[0].GetPath()));

            EmailMaildirOperationResult moved = writer.Move(EmailMaildirFolder.New, written.GetFileName()!, EmailMaildirFolder.Current);
            Assert.That(moved.GetSourceFileName(), Is.EqualTo(written.GetFileName()));
            Assert.That(moved.WasCompleted(), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    static EmailMessage Parse(string value)
    {
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(value), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        return parsed.GetMessage()!;
    }

    static string CreateDirectory(string label)
    {
        string path = Path.Combine(Path.GetTempPath(), "email-archive-completion-" + label + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    static string CreateMaildir()
    {
        string root = CreateDirectory("maildir");
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "new"));
        Directory.CreateDirectory(Path.Combine(root, "cur"));
        return root;
    }
}
