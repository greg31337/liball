using System.Text;
using email;

namespace email.Tests;

public class EmailFileTests
{
    [Test]
    public void LoadsEmlWithSourceMetadataAndRetainsContentAfterTheFileIsRemoved()
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "message.EML");
        byte[] source = Bytes("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: sender@example.test\r\nSubject: loaded\r\n\r\nbody\r\n");
        try
        {
            File.WriteAllBytes(path, source);

            EmailFileLoadResult result = new EmailFile().LoadWithPath(path);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetPath(), Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(result.GetFileName(), Is.EqualTo("message.EML"));
            Assert.That(result.GetByteCount(), Is.EqualTo(source.LongLength));
            Assert.That(result.GetBytes(), Is.EqualTo(source));
            Assert.That(result.GetMessage()!.GetSourceInfo()!.GetSourceName(), Is.EqualTo(Path.GetFullPath(path)));
            File.Delete(path);
            Assert.That(Body(result.GetMessage()!), Is.EqualTo("body\r\n"));
            Assert.That(new EmailWriter().WriteMessageWithMessageAndOptions(result.GetMessage()!, new EmailWriteOptionsOperations().CreateExactPreservation()).GetBytes(), Is.EqualTo(source));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SaveRequiresExplicitPolicyAndAtomicallyReplacesOnlyWhenAllowed()
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "saved.eml");
        try
        {
            EmailFileSaveOptions fail = new EmailFileSaveOptions();
            EmailFileSaveResult first = new EmailFile().Save(Parse("Subject: first\n\nold\n"), path, fail);
            byte[] original = File.ReadAllBytes(path);

            Assert.That(first.IsSuccess(), Is.True);
            Assert.That(first.WasOverwritten(), Is.False);
            Assert.That(first.WasFlushedToDisk(), Is.True);
            Assert.That(first.GetByteCount(), Is.EqualTo(original.LongLength));
            Assert.Throws<IOException>(() => new EmailFile().Save(Parse("Subject: rejected\n\nbody\n"), path, fail));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));

            EmailFileSaveOptions overwrite = new EmailFileSaveOptions();
            overwrite.SetOverwritePolicy(EmailFileOverwritePolicy.Overwrite);
            overwrite.SetFlushToDisk(false);
            EmailFileSaveResult replaced = new EmailFile().Save(Parse("Subject: replacement\n\nnew\n"), path, overwrite);
            Assert.That(replaced.WasOverwritten(), Is.True);
            Assert.That(replaced.WasFlushedToDisk(), Is.False);
            Assert.That(new EmailFile().LoadWithPath(path).GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("replacement"));
            Assert.That(Directory.GetFiles(directory, ".email-save-*"), Is.Empty);

            EmailFileSaveOptions anyExtension = new EmailFileSaveOptions();
            anyExtension.SetRequireEmlExtension(false);
            Assert.That(new EmailFile().Save(Parse("Subject: alternate\n\nbody\n"), Path.Combine(directory, "message.msg"), anyExtension).IsSuccess(), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FileHelpersEnforceExtensionsLimitsSymlinkSafetyAndDefensiveOptions()
    {
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "bounded.eml");
        string link = Path.Combine(directory, "linked.eml");
        byte[] source = Bytes("Subject: bounded\n\nbody\n");
        try
        {
            File.WriteAllBytes(path, source);
            EmailFileLoadOptions supplied = new EmailFileLoadOptions();
            EmailParseOptions parseOptions = supplied.GetParseOptions();
            EmailLimits limits = parseOptions.GetLimits();
            EmailParsingLimits parsing = limits.GetParsingLimits();
            parsing.SetMaxMessageBytes(source.Length - 1);
            limits.SetParsingLimits(parsing);
            parseOptions.SetLimits(limits);
            supplied.SetParseOptions(parseOptions);
            EmailFileLoadOptions snapshot = supplied.Clone();
            supplied.SetRequireEmlExtension(false);

            EmailLimitException size = Assert.Throws<EmailLimitException>(() => new EmailFile().LoadWithPathAndOptions(path, snapshot));
            Assert.That(size.GetLimitName(), Is.EqualTo("MaxMessageBytes"));
            Assert.Throws<ArgumentException>(() => new EmailFile().LoadWithPath(Path.Combine(directory, "not-eml.txt")));
            Assert.Throws<ArgumentException>(() => new EmailFile().Save(Parse("Subject: x\n\nbody\n"), Path.Combine(directory, "not-eml.txt"), new EmailFileSaveOptions()));

            EmailFileSaveOptions smallOutput = new EmailFileSaveOptions();
            EmailWriteOptions writeOptions = smallOutput.GetWriteOptions();
            writeOptions.SetMaximumOutputBytes(5);
            smallOutput.SetWriteOptions(writeOptions);
            EmailFileSaveResult unsaved = new EmailFile().Save(Parse("Subject: too-large\n\nbody\n"), Path.Combine(directory, "unsaved.eml"), smallOutput);
            Assert.That(unsaved.WasSaved(), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "unsaved.eml")), Is.False);

            EmailFileLoadOptions shortPath = new EmailFileLoadOptions();
            shortPath.SetMaximumPathCharacters(Path.GetFullPath(path).Length - 1);
            Assert.That(Assert.Throws<EmailLimitException>(() => new EmailFile().LoadWithPathAndOptions(path, shortPath)).GetLimitName(), Is.EqualTo("MaxEmailFilePathCharacters"));

            if (!OperatingSystem.IsWindows())
            {
                File.CreateSymbolicLink(link, path);
                Assert.Throws<UnauthorizedAccessException>(() => new EmailFile().LoadWithPath(link));
                EmailFileSaveOptions overwrite = new EmailFileSaveOptions();
                overwrite.SetOverwritePolicy(EmailFileOverwritePolicy.Overwrite);
                Assert.Throws<UnauthorizedAccessException>(() => new EmailFile().Save(Parse("Subject: no\n\nbody\n"), link, overwrite));
            }
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

    static string Body(EmailMessage message)
    {
        return Encoding.UTF8.GetString(message.GetBody()!.GetContent()!.Materialize(10000));
    }

    static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-file-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }
}
