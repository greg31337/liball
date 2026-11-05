using email;

namespace email.Tests;

public class EmailMaildirOperationTests
{
    [Test]
    public void MovesTemporaryEntryToNewAtomically()
    {
        string root = CreateMaildir();
        try
        {
            string source = Write(root, "tmp", "message", "temporary content");
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);

            EmailMaildirOperationResult result = writer.Move(EmailMaildirFolder.Temporary, "message", EmailMaildirFolder.New);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasCompleted(), Is.True);
            Assert.That(result.WasMoved(), Is.True);
            Assert.That(result.WasOverwritten(), Is.False);
            Assert.That(result.GetSourceFolder(), Is.EqualTo(EmailMaildirFolder.Temporary));
            Assert.That(result.GetDestinationFolder(), Is.EqualTo(EmailMaildirFolder.New));
            Assert.That(result.GetSourcePath(), Is.EqualTo(source));
            Assert.That(result.GetDestinationPath(), Is.EqualTo(Path.Combine(root, "new", "message")));
            Assert.That(File.Exists(source), Is.False);
            Assert.That(File.ReadAllText(result.GetDestinationPath()), Is.EqualTo("temporary content"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void MovesNewEntryToCurrentWithCanonicalFlags()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "new", "message", "content");
            EmailMaildirFlags flags = EmailMaildirFlags.Trashed | EmailMaildirFlags.Draft | EmailMaildirFlags.Seen | EmailMaildirFlags.Flagged;

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPath(root).MoveWithFlags(EmailMaildirFolder.New, "message", EmailMaildirFolder.Current, flags);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDestinationFileName(), Is.EqualTo("message:2,DFST"));
            Assert.That(result.GetPreviousFlags(), Is.EqualTo(EmailMaildirFlags.None));
            Assert.That(result.GetFlags(), Is.EqualTo(flags));
            Assert.That(File.Exists(Path.Combine(root, "cur", "message:2,DFST")), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void FlagUpdatePreservesSupportedUnknownFlagsAndCanonicalizesAllFlags()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "cur", "message:2,ZSD", "content");
            EmailMaildirFlags flags = EmailMaildirFlags.Replied | EmailMaildirFlags.Flagged;

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPath(root).UpdateFlags("message:2,ZSD", flags);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPreviousFlags(), Is.EqualTo(EmailMaildirFlags.Seen | EmailMaildirFlags.Draft));
            Assert.That(result.GetFlags(), Is.EqualTo(flags));
            Assert.That(result.GetPreservedUnknownFlagText(), Is.EqualTo("Z"));
            Assert.That(result.GetDestinationFileName(), Is.EqualTo("message:2,FRZ"));
            Assert.That(new EmailMaildirNameOperations().Parse(result.GetDestinationFileName(), EmailMaildirFolder.Current).IsCanonical(), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "cur", "message:2,ZSD")), Is.False);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void MoveOutOfCurrentRemovesInfoSuffixAndFlags()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "cur", "message:2,SZ", "content");

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPath(root).Move(EmailMaildirFolder.Current, "message:2,SZ", EmailMaildirFolder.Temporary);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDestinationFileName(), Is.EqualTo("message"));
            Assert.That(result.GetFlags(), Is.EqualTo(EmailMaildirFlags.None));
            Assert.That(result.GetPreservedUnknownFlagText(), Is.Empty);
            Assert.That(File.Exists(Path.Combine(root, "tmp", "message")), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void MoveCanTraverseNewTemporaryAndCurrent()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "new", "message", "content");
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);

            EmailMaildirOperationResult temporary = writer.Move(EmailMaildirFolder.New, "message", EmailMaildirFolder.Temporary);
            EmailMaildirOperationResult current = writer.MoveWithFlags(EmailMaildirFolder.Temporary, temporary.GetDestinationFileName(), EmailMaildirFolder.Current, EmailMaildirFlags.Seen);

            Assert.That(temporary.IsSuccess(), Is.True);
            Assert.That(current.IsSuccess(), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "cur", "message:2,S")), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(root, "tmp")), Is.Empty);
            Assert.That(Directory.GetFiles(Path.Combine(root, "new")), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void AlreadyCanonicalFlagStateCompletesWithoutFilesystemMove()
    {
        string root = CreateMaildir();
        try
        {
            string path = Write(root, "cur", "message:2,SZ", "content");

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPath(root).UpdateFlags("message:2,SZ", EmailMaildirFlags.Seen);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasCompleted(), Is.True);
            Assert.That(result.WasMoved(), Is.False);
            Assert.That(result.GetSourcePath(), Is.EqualTo(path));
            Assert.That(result.GetDestinationPath(), Is.EqualTo(path));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void FailCollisionPolicyReturnsIssueWithoutMutation()
    {
        string root = CreateMaildir();
        try
        {
            string source = Write(root, "tmp", "same", "source");
            string collision = Write(root, "new", "same", "existing");
            EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
            options.SetCollisionPolicy(EmailMaildirCollisionPolicy.Fail);

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, options).Move(EmailMaildirFolder.Temporary, "same", EmailMaildirFolder.Current);

            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasCompleted(), Is.False);
            Assert.That(result.WasMoved(), Is.False);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MaildirOperationCollision), Is.True);
            Assert.That(File.ReadAllText(source), Is.EqualTo("source"));
            Assert.That(File.ReadAllText(collision), Is.EqualTo("existing"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void GenerateUniqueCollisionPolicyPreservesBothEntries()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "tmp", "same", "source");
            Write(root, "new", "same", "existing");
            EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
            options.SetCollisionPolicy(EmailMaildirCollisionPolicy.GenerateUnique);
            options.SetHostName("move-host");

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, options).MoveWithFlags(EmailMaildirFolder.Temporary, "same", EmailMaildirFolder.Current, EmailMaildirFlags.Seen);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetCollisionCount(), Is.EqualTo(1));
            Assert.That(result.GetDestinationFileName(), Is.Not.EqualTo("same:2,S"));
            Assert.That(result.GetDestinationFileName(), Does.EndWith(":2,S"));
            Assert.That(File.ReadAllText(result.GetDestinationPath()), Is.EqualTo("source"));
            Assert.That(File.ReadAllText(Path.Combine(root, "new", "same")), Is.EqualTo("existing"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void OverwritePolicyReplacesOnlyExactDestinationCollision()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "new", "same", "replacement");
            Write(root, "cur", "same:2,S", "old");
            EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
            options.SetCollisionPolicy(EmailMaildirCollisionPolicy.Overwrite);

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, options).MoveWithFlags(EmailMaildirFolder.New, "same", EmailMaildirFolder.Current, EmailMaildirFlags.Seen);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.WasOverwritten(), Is.True);
            Assert.That(result.WasMoved(), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(root, "cur", "same:2,S")), Is.EqualTo("replacement"));
            Assert.That(File.Exists(Path.Combine(root, "new", "same")), Is.False);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void OverwritePolicyRejectsNonDestinationIdentityCollision()
    {
        string root = CreateMaildir();
        try
        {
            string source = Write(root, "tmp", "same", "source");
            Write(root, "new", "same", "other");
            EmailMaildirWriterOptions options = new EmailMaildirWriterOptions();
            options.SetCollisionPolicy(EmailMaildirCollisionPolicy.Overwrite);

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPathAndOptions(root, options).Move(EmailMaildirFolder.Temporary, "same", EmailMaildirFolder.Current);

            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MaildirOperationCollision), Is.True);
            Assert.That(File.Exists(source), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(root, "cur")), Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void UnsupportedInfoVersionIsReportedAndCanonicalized()
    {
        string root = CreateMaildir();
        try
        {
            Write(root, "cur", "message:3,opaque", "content");

            EmailMaildirOperationResult result = new EmailMaildirWriterOperations().CreateWithRootPath(root).UpdateFlags("message:3,opaque", EmailMaildirFlags.Seen);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDestinationFileName(), Is.EqualTo("message:2,S"));
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MaildirUnknownInfoNotPreserved), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void NameOperationsMergeStandardAndUnknownFlagsInAsciiOrder()
    {
        EmailMaildirNameOperations names = new EmailMaildirNameOperations();

        string fileName = names.CreateCurrentFileNamePreservingUnknownFlags("message", EmailMaildirFlags.Trashed | EmailMaildirFlags.Draft, "zAZA");
        EmailMaildirName parsed = names.Parse(fileName, EmailMaildirFolder.Current);

        Assert.That(fileName, Is.EqualTo("message:2,ADTZz"));
        Assert.That(parsed.GetFlags(), Is.EqualTo(EmailMaildirFlags.Draft | EmailMaildirFlags.Trashed));
        Assert.That(parsed.GetUnknownFlagText(), Is.EqualTo("AZz"));
        Assert.That(parsed.IsCanonical(), Is.True);
        Assert.That(names.Parse("message", EmailMaildirFolder.Temporary).IsCanonical(), Is.True);
        Assert.Throws<ArgumentException>(() => names.CreateCurrentFileNamePreservingUnknownFlags("message", EmailMaildirFlags.None, ","));
    }

    [Test]
    public void InvalidMoveInputsAndSymbolicLinksAreRejected()
    {
        string root = CreateMaildir();
        string outside = root + "-outside";
        try
        {
            Write(root, "new", "safe", "content");
            EmailMaildirWriter writer = new EmailMaildirWriterOperations().CreateWithRootPath(root);

            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Move((EmailMaildirFolder)3, "safe", EmailMaildirFolder.Current));
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Move(EmailMaildirFolder.New, "safe", (EmailMaildirFolder)3));
            Assert.Throws<ArgumentException>(() => writer.Move(EmailMaildirFolder.New, "../safe", EmailMaildirFolder.Current));
            Assert.Throws<ArgumentException>(() => writer.MoveWithFlags(EmailMaildirFolder.New, "safe", EmailMaildirFolder.Temporary, EmailMaildirFlags.Seen));
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.UpdateFlags("safe:2,S", (EmailMaildirFlags)128));
            Assert.Throws<FileNotFoundException>(() => writer.Move(EmailMaildirFolder.New, "missing", EmailMaildirFolder.Current));

            if (!OperatingSystem.IsWindows())
            {
                File.WriteAllText(outside, "outside");
                File.CreateSymbolicLink(Path.Combine(root, "tmp", "linked"), outside);
                Assert.Throws<UnauthorizedAccessException>(() => writer.Move(EmailMaildirFolder.Temporary, "linked", EmailMaildirFolder.New));
            }
        }
        finally
        {
            if (File.Exists(outside)) File.Delete(outside);
            Directory.Delete(root, true);
        }
    }

    static string CreateMaildir()
    {
        string root = Path.Combine(Path.GetTempPath(), "email-maildir-operation-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tmp"));
        Directory.CreateDirectory(Path.Combine(root, "new"));
        Directory.CreateDirectory(Path.Combine(root, "cur"));
        return root;
    }

    static string Write(string root, string folder, string fileName, string value)
    {
        string path = Path.Combine(root, folder, fileName);
        File.WriteAllText(path, value);
        return path;
    }
}
