using System.Text;
using email;

namespace email.Tests;

public class EmailArchiveIndexTests
{
    [Test]
    public void CreatesPersistenceNeutralEmlRecordWithHeadersSizesHashesAndStatus()
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "indexed.eml");
        byte[] source = Bytes("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: Alice <alice@example.test>\r\nTo: Bob <bob@example.test>\r\nSubject: Indexed subject\r\nMessage-ID: <indexed@example.test>\r\nX-Ignored: no\r\n\r\nbody\r\n");
        try
        {
            File.WriteAllBytes(path, source);
            EmailFileLoadResult loaded = new EmailFile().LoadWithPath(path);

            EmailArchiveIndexRecord record = new EmailArchiveIndex().CreateWithFile(loaded);

            Assert.That(record.GetSourceKind(), Is.EqualTo(EmailArchiveSourceKind.EmlFile));
            Assert.That(record.GetSourcePath(), Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(record.HasIndex(), Is.True);
            Assert.That(record.GetIndex(), Is.Zero);
            Assert.That(record.GetArchiveRange()!.EqualsRange(new EmailRange(0, source.Length)), Is.True);
            Assert.That(record.GetMessageRange()!.EqualsRange(new EmailRange(0, source.Length)), Is.True);
            Assert.That(record.GetMessageByteCount(), Is.EqualTo(source.LongLength));
            Assert.That(record.GetParseStatus(), Is.EqualTo(EmailArchiveParseStatus.Success));
            Assert.That(record.GetFirstSelectedHeaderValue("Subject"), Is.EqualTo("Indexed subject"));
            Assert.That(record.GetFirstSelectedHeaderValue("Message-ID"), Is.EqualTo("<indexed@example.test>"));
            Assert.That(record.GetFirstSelectedHeaderValue("X-Ignored"), Is.Null);
            Assert.That(record.GetExactHash()!.GetLowercaseHex(), Is.EqualTo(new EmailHash().ComputeExactWithContentAndAlgorithm(source, EmailHashAlgorithm.Sha256).GetLowercaseHex()));
            Assert.That(record.HasSemanticHash(), Is.True);
            Assert.That(record.GetSummary()!.GetHeaderCount(), Is.EqualTo(6));
            Assert.That(record.GetSummary()!.HasDate(), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IndexesMboxOffsetsAndMaildirPathsWithoutOwningPersistence()
    {
        const string envelope = "From sender@example.test Fri Nov 21 09:55:06 1997\n";
        const string messageSource = "Subject: mbox indexed\nMessage-ID: <mbox@example.test>\n\nbody\n";
        byte[] mboxBytes = Bytes(envelope + messageSource);
        EmailMboxReader mboxReader = new EmailMboxReaderOperations().CreateWithInput(new MemoryStream(mboxBytes, false));
        Assert.That(mboxReader.ReadNext(), Is.True);
        EmailMboxEntry mboxEntry = mboxReader.GetCurrent()!;

        EmailArchiveIndexRecord mbox = new EmailArchiveIndex().CreateWithEntryAndSourcePathAndOptions(mboxEntry!, "/archives/mail.mbox", new EmailArchiveIndexOptions());

        Assert.That(mbox.GetSourceKind(), Is.EqualTo(EmailArchiveSourceKind.Mbox));
        Assert.That(mbox.GetIndex(), Is.Zero);
        Assert.That(mbox.GetSourcePath(), Is.EqualTo("/archives/mail.mbox"));
        Assert.That(mbox.GetArchiveRange()!.EqualsRange(mboxEntry.GetArchiveRange()), Is.True);
        Assert.That(mbox.GetMessageRange()!.EqualsRange(mboxEntry.GetMessageRange()), Is.True);
        Assert.That(mbox.GetFirstSelectedHeaderValue("Subject"), Is.EqualTo("mbox indexed"));

        string root = CreateMaildir();
        try
        {
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);
            EmailMaildirWriteResult written = writer.DeliverWithUniqueName(Parse("Subject: maildir indexed\nMessage-ID: <maildir@example.test>\n\nbody\n"), "indexed", EmailMaildirFolder.New, EmailMaildirFlags.None);
            EmailMaildirEntry entry = new EmailMaildirReaderOperations().CreateWithRootPath(root).ReadRelativePath("new/" + written.GetFileName());

            EmailArchiveIndexRecord maildir = new EmailArchiveIndex().CreateWithEntryUsingEmailMaildirEntry(entry);

            Assert.That(maildir.GetSourceKind(), Is.EqualTo(EmailArchiveSourceKind.Maildir));
            Assert.That(maildir.GetSourcePath(), Is.EqualTo(entry.GetPath()));
            Assert.That(maildir.HasArchiveRange(), Is.False);
            Assert.That(maildir.GetMessageRange()!.EqualsRange(new EmailRange(0, entry.GetByteCount())), Is.True);
            Assert.That(maildir.GetExactHash()!.GetHashedByteCount(), Is.EqualTo(entry.GetByteCount()));
            Assert.That(maildir.GetFirstSelectedHeaderValue("Subject"), Is.EqualTo("maildir indexed"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void RetainsExactHashAndFailureStatusForAnUnparseableEml()
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "broken.eml");
        byte[] source = Bytes("not a valid header\nwithout separator");
        try
        {
            File.WriteAllBytes(path, source);
            EmailFileLoadOptions loadOptions = new EmailFileLoadOptions();
            loadOptions.SetParseOptions(new EmailParseOptionsOperations().CreateStrict());
            EmailFileLoadResult loaded = new EmailFile().LoadWithPathAndOptions(path, loadOptions);

            EmailArchiveIndexRecord record = new EmailArchiveIndex().CreateWithFile(loaded);

            Assert.That(record.IsParseSuccess(), Is.False);
            Assert.That(record.GetParseStatus(), Is.AnyOf(EmailArchiveParseStatus.CompleteWithErrors, EmailArchiveParseStatus.Partial, EmailArchiveParseStatus.Failed));
            Assert.That(record.HasExactHash(), Is.True);
            Assert.That(record.GetExactHash()!.GetLowercaseHex(), Is.EqualTo(new EmailHash().ComputeExactWithContentAndAlgorithm(source, EmailHashAlgorithm.Sha256).GetLowercaseHex()));
            Assert.That(record.HasSemanticHash(), Is.False);
            Assert.That(record.GetIssues().HasSeverityAtLeast(EmailIssueSeverity.Error), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CustomHeaderSelectionAndIndexLimitsAreDefensiveAndBounded()
    {
        EmailMessage message = Parse("Subject: one\nSubject: two\nX-Custom: selected\n\nbody\n");
        EmailArchiveIndexOptions selectedOptions = new EmailArchiveIndexOptions();
        selectedOptions.ClearSelectedHeaderNames();
        selectedOptions.AddSelectedHeaderName("Subject");
        selectedOptions.AddSelectedHeaderName("X-Custom");
        selectedOptions.SetIncludeSemanticHash(false);

        EmailArchiveIndexRecord selected = new EmailArchiveIndex().CreateWithMessageAndOptions(message, selectedOptions);

        Assert.That(selected.GetSelectedHeaderCount(), Is.EqualTo(3));
        Assert.That(selected.GetSelectedHeaderAt(0).GetOccurrenceIndex(), Is.Zero);
        Assert.That(selected.GetSelectedHeaderAt(1).GetOccurrenceIndex(), Is.EqualTo(1));
        Assert.That(selected.GetSelectedHeaderAt(2).GetValue(), Is.EqualTo("selected"));
        Assert.That(selected.HasSemanticHash(), Is.False);
        List<EmailArchiveIndexHeader> copy = selected.GetSelectedHeaders();
        copy.Clear();
        Assert.That(selected.GetSelectedHeaderCount(), Is.EqualTo(3));

        EmailArchiveIndexOptions headerCount = selectedOptions.Clone();
        headerCount.SetMaximumSelectedHeaderCount(2);
        Assert.That(Assert.Throws<EmailLimitException>(() => new EmailArchiveIndex().CreateWithMessageAndOptions(message, headerCount)).GetLimitName(), Is.EqualTo("MaxArchiveIndexHeaders"));

        EmailArchiveIndexOptions characters = selectedOptions.Clone();
        characters.SetMaximumSelectedHeaderCharacters(2);
        Assert.That(Assert.Throws<EmailLimitException>(() => new EmailArchiveIndex().CreateWithMessageAndOptions(message, characters)).GetLimitName(), Is.EqualTo("MaxArchiveIndexHeaderCharacters"));

        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "hash.eml");
            File.WriteAllBytes(path, Bytes("Subject: hash\n\nbody\n"));
            EmailArchiveIndexOptions hash = new EmailArchiveIndexOptions();
            hash.SetMaximumHashBytes(5);
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailArchiveIndex().CreateWithFileAndOptions(new EmailFile().LoadWithPath(path), hash)).GetLimitName(), Is.EqualTo("MaxArchiveIndexHashBytes"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static EmailMessage Parse(string value)
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-index-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static string CreateMaildir()
    {
        string root = CreateDirectory();
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "new"));
        Directory.CreateDirectory(Path.Combine(root, "cur"));
        return root;
    }

    static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }
}
