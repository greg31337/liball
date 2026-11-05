using System.Text;
using email;

namespace email.Tests;

public class EmailMaildirReaderTests
{
    [Test]
    public void EnumeratesNewThenCurrentDeterministicallyAndReadsMessages()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "new", "b-message", "Subject: new-b\n\nbody b\n");
            Write(root, "new", "a-message", "Subject: new-a\n\nbody a\n");
            Write(root, "cur", "c-message:2,DFPRSTZ", "Subject: current-c\n\nbody c\n");
            Write(root, "cur", "d-message:2,RS", "Subject: current-d\n\nbody d\n");
            EmailMaildirReader reader = new EmailMaildirReaderOperations().CreateWithRootPath(root);

            List<EmailMaildirEntry> entries = reader.Enumerate();

            Assert.That(entries.Select(entry => entry.GetFileName()), Is.EqualTo(new[] { "a-message", "b-message", "c-message:2,DFPRSTZ", "d-message:2,RS" }));
            Assert.That(entries.Select(entry => entry.GetFolder()), Is.EqualTo(new[] { EmailMaildirFolder.New, EmailMaildirFolder.New, EmailMaildirFolder.Current, EmailMaildirFolder.Current }));
            Assert.That(entries.All(entry => !entry.HasMessage()), Is.True);
            EmailMaildirName currentName = entries[2].GetName();
            Assert.That(currentName.GetUniqueName(), Is.EqualTo("c-message"));
            Assert.That(currentName.GetInfoVersion(), Is.EqualTo(2));
            Assert.That(currentName.GetUnknownFlagText(), Is.EqualTo("Z"));
            Assert.That(currentName.HasFlag(EmailMaildirFlags.Draft | EmailMaildirFlags.Flagged | EmailMaildirFlags.Passed | EmailMaildirFlags.Replied | EmailMaildirFlags.Seen | EmailMaildirFlags.Trashed), Is.True);
            Assert.That(currentName.IsCanonical(), Is.True);

            List<string> subjects = new List<string>();
            while (reader.ReadNext())
            {
                EmailMaildirEntry entry = reader.GetCurrent()!;
                Assert.That(entry!.IsSuccess(), Is.True);
                subjects.Add(entry.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue()!);
            }

            Assert.That(subjects, Is.EqualTo(new[] { "new-a", "new-b", "current-c", "current-d" }));
            EmailMaildirEntry direct = reader.ReadRelativePath("cur/d-message:2,RS");
            File.Delete(direct.GetPath());
            Assert.That(Body(direct), Is.EqualTo("body d\n"));
            Assert.That(reader.GetEntryCount(), Is.EqualTo(4));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void PreservesRecognizedAndUnknownFlagsAndDiagnosesNoncanonicalNames()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "cur", "missing-info", "Subject: missing\n\nbody\n");
            Write(root, "cur", "unordered:2,SRR", "Subject: flags\n\nbody\n");
            Write(root, "cur", "experimental:1,value", "Subject: experimental\n\nbody\n");
            Write(root, "new", "unsafe:name", "Subject: ignored\n\nbody\n");
            Write(root, "new", ".hidden", "Subject: ignored\n\nbody\n");
            EmailMaildirReader reader = new EmailMaildirReaderOperations().CreateWithRootPath(root);

            List<EmailMaildirEntry> entries = reader.Enumerate();

            Assert.That(entries, Has.Count.EqualTo(3));
            EmailMaildirEntry missing = entries.Single(entry => entry.GetFileName() == "missing-info");
            EmailMaildirEntry unordered = entries.Single(entry => entry.GetFileName() == "unordered:2,SRR");
            EmailMaildirEntry experimental = entries.Single(entry => entry.GetFileName() == "experimental:1,value");
            Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.MaildirNameMalformed), Is.True);
            Assert.That(unordered.GetIssues().ContainsCode(EmailIssueCode.MaildirFlagsMalformed), Is.True);
            Assert.That(unordered.GetName().HasFlag(EmailMaildirFlags.Replied | EmailMaildirFlags.Seen), Is.True);
            Assert.That(unordered.GetName().GetFlagText(), Is.EqualTo("SRR"));
            Assert.That(experimental.GetName().IsInfoWellFormed(), Is.True);
            Assert.That(experimental.GetName().GetInfoVersion(), Is.EqualTo(1));
            Assert.That(experimental.GetName().GetInfoText(), Is.EqualTo("1,value"));
            Assert.That(reader.GetIssues().ContainsCode(EmailIssueCode.MaildirNameMalformed), Is.True);
            Assert.That(reader.GetIssues().ContainsCode(EmailIssueCode.MaildirFlagsMalformed), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void RejectsTraversalAbsolutePathsUnsafeSegmentsAndSymbolicLinks()
    {
        string root = CreateMaildir();
        string outside = root + "-outside.eml";
        try
        {
            File.WriteAllText(outside, "Subject: outside\n\nsecret\n");
            Write(root, "new", "safe", "Subject: safe\n\nbody\n");
            EmailMaildirReader reader = new EmailMaildirReaderOperations().CreateWithRootPath(root);

            Assert.Throws<ArgumentException>(() => reader.ReadRelativePath("../" + Path.GetFileName(outside)));
            Assert.Throws<ArgumentException>(() => reader.ReadRelativePath("new/../safe"));
            Assert.Throws<ArgumentException>(() => reader.ReadRelativePath(Path.GetFullPath(outside)));
            Assert.Throws<ArgumentException>(() => reader.Read(EmailMaildirFolder.New, "../safe"));
            Assert.Throws<ArgumentException>(() => new EmailMaildirNameOperations().Parse("../safe", EmailMaildirFolder.New));
            Assert.Throws<ArgumentException>(() => new EmailMaildirNameOperations().CreateNewFileName(".hidden"));

            if (!OperatingSystem.IsWindows())
            {
                string link = Path.Combine(root, "cur", "linked:2,S");
                File.CreateSymbolicLink(link, outside);
                EmailMaildirReader linkedReader = new EmailMaildirReaderOperations().CreateWithRootPath(root);
                Assert.Throws<UnauthorizedAccessException>(() => linkedReader.Enumerate());
            }

            Assert.That(File.ReadAllText(outside), Does.Contain("secret"));
        }
        finally
        {
            if (File.Exists(outside)) File.Delete(outside);
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void GeneratesSafeUniqueNamesAndCanonicalStandardFlagSuffixes()
    {
        string first = new EmailMaildirNameOperations().CreateUniqueNameWithHostName("bad/host:name");
        string second = new EmailMaildirNameOperations().CreateUniqueNameWithHostName("bad/host:name");
        EmailMaildirFlags flags = EmailMaildirFlags.Seen | EmailMaildirFlags.Draft | EmailMaildirFlags.Passed | EmailMaildirFlags.Trashed | EmailMaildirFlags.Flagged | EmailMaildirFlags.Replied;
        string current = new EmailMaildirNameOperations().CreateCurrentFileName(first, flags);
        EmailMaildirName parsed = new EmailMaildirNameOperations().Parse(current, EmailMaildirFolder.Current);

        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(new EmailMaildirNameOperations().IsSafeUniqueName(first), Is.True);
        Assert.That(first, Does.Not.Contain("/"));
        Assert.That(first, Does.Not.Contain(":"));
        Assert.That(current, Does.EndWith(":2,DFPRST"));
        Assert.That(parsed.GetFlags(), Is.EqualTo(flags));
        Assert.That(parsed.IsCanonical(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailMaildirNameOperations().CreateCurrentFileName(first, (EmailMaildirFlags)128));
    }

    [Test]
    public void EnforcesEntryCountArchiveEntryAndPathLimitsWithDefensiveOptions()
    {
        string root = CreateMaildir();
        try
        {
            string first = "Subject: first\n\nbody\n";
            string second = "Subject: second\n\nbody\n";
            Write(root, "new", "first", first);
            Write(root, "cur", "second:2,S", second);

            EmailLimitException count = Assert.Throws<EmailLimitException>(() => ReaderWithLimits(root, 1000, 1000, 1, 4096).Enumerate());
            EmailLimitException archive = Assert.Throws<EmailLimitException>(() => ReaderWithLimits(root, Bytes(first).Length + Bytes(second).Length - 1, 1000, 2, 4096).Enumerate());
            EmailLimitException entry = Assert.Throws<EmailLimitException>(() => ReaderWithLimits(root, 1000, Bytes(second).Length - 1, 2, 4096).Enumerate());

            Assert.That(count.GetLimitName(), Is.EqualTo("MaxMaildirEntryCount"));
            Assert.That(archive.GetLimitName(), Is.EqualTo("MaxMaildirArchiveBytes"));
            Assert.That(entry.GetLimitName(), Is.EqualTo("MaxMaildirEntryBytes"));

            EmailMaildirReaderOptions supplied = new EmailMaildirReaderOptions();
            EmailArchiveLimits limits = supplied.GetLimits();
            limits.SetMaxEntryCount(1);
            supplied.SetLimits(limits);
            EmailMaildirReader snapshot = new EmailMaildirReaderOperations().CreateWithRootPathAndOptions(root, supplied);
            limits.SetMaxEntryCount(100);
            supplied.SetLimits(limits);
            Assert.That(Assert.Throws<EmailLimitException>(() => snapshot.Enumerate()).GetLimitName(), Is.EqualTo("MaxMaildirEntryCount"));

            EmailMaildirReaderOptions shortPathOptions = new EmailMaildirReaderOptions();
            EmailArchiveLimits shortPathLimits = shortPathOptions.GetLimits();
            shortPathLimits.SetMaxPathCharacters(root.Length - 1);
            shortPathOptions.SetLimits(shortPathLimits);
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailMaildirReaderOperations().CreateWithRootPathAndOptions(root, shortPathOptions)).GetLimitName(), Is.EqualTo("MaxMaildirPathCharacters"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    static EmailMaildirReader ReaderWithLimits(string root, long archiveBytes, long entryBytes, int entryCount, int pathCharacters)
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        limits.SetMaxArchiveBytes(archiveBytes);
        limits.SetMaxEntryBytes(entryBytes);
        limits.SetMaxEntryCount(entryCount);
        limits.SetMaxPathCharacters(pathCharacters);
        EmailMaildirReaderOptions options = new EmailMaildirReaderOptions();
        options.SetLimits(limits);
        return new EmailMaildirReaderOperations().CreateWithRootPathAndOptions(root, options);
    }

    static string CreateMaildir()
    {
        string root = Path.Combine(Path.GetTempPath(), "email-maildir-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "new"));
        Directory.CreateDirectory(Path.Combine(root, "cur"));
        return root;
    }

    static void Write(string root, string folder, string name, string value)
    {
        File.WriteAllBytes(Path.Combine(root, folder, name), Bytes(value));
    }

    static string Body(EmailMaildirEntry entry)
    {
        return Encoding.UTF8.GetString(entry.GetMessage()!.GetBody()!.GetContent()!.Materialize(10000));
    }

    static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }
}
