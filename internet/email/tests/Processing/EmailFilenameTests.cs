using System.Text;
using email;

namespace email.Tests;

public class EmailFilenameTests
{
    [Test]
    public void DecodeUsesExtendedDispositionFilenameBeforeMediaTypeName()
    {
        MimeEntity entity = ParseEntity(
            "Content-Type: application/pdf; name=type-name.pdf\r\n"
            + "Content-Disposition: attachment; filename*=iso-8859-1''caf%E9.pdf\r\n\r\npdf");

        Assert.That(new EmailFilename().Decode(entity), Is.EqualTo("café.pdf"));

        MimeEntity emptyDisposition = ParseEntity(
            "Content-Type: text/plain; name=fallback.txt\r\n"
            + "Content-Disposition: attachment; filename=\"\"\r\n\r\ntext");
        Assert.That(new EmailFilename().Decode(emptyDisposition), Is.EqualTo("fallback.txt"));
    }

    [Test]
    public void DecodeSupportsEncodedWordCompatibilityFilenames()
    {
        MimeEntity entity = ParseEntity(
            "Content-Type: application/pdf\r\n"
            + "Content-Disposition: attachment; filename=\"=?utf-8?B?Y2Fmw6kucGRm?=\"\r\n\r\npdf");

        Assert.That(new EmailFilename().Decode(entity), Is.EqualTo("café.pdf"));
    }

    [Test]
    public void SanitizeStripsPathsControlsFormatsAndForbiddenCharacters()
    {
        EmailFilenameResult result = new EmailFilename().SanitizeWithFilename("..\\folder/\0bad<na\u202Eme>.txt. ");

        Assert.That(result.GetFilename(), Is.EqualTo("bad_name_.txt"));
        Assert.That(result.WasPathStripped(), Is.True);
        Assert.That(result.WereCharactersRemoved(), Is.True);
        Assert.That(result.WereCharactersReplaced(), Is.True);
        Assert.That(result.WasChanged(), Is.True);

        Assert.That(new EmailFilename().SanitizeWithFilename("📎.txt").GetFilename(), Is.EqualTo("📎.txt"));
    }

    [Test]
    public void CompatibilityNormalizationCannotCreateAPathTraversalAfterSanitization()
    {
        EmailFilenameOptions options = new EmailFilenameOptions();
        options.SetNormalization(EmailFilenameNormalization.Nfkc);

        EmailFilenameResult forward = new EmailFilename().SanitizeWithFilenameAndOptions("folder／..／secret.txt", options);
        EmailFilenameResult backward = new EmailFilename().SanitizeWithFilenameAndOptions("..＼payload.exe", options);

        Assert.That(forward.GetFilename(), Is.EqualTo("secret.txt"));
        Assert.That(backward.GetFilename(), Is.EqualTo("payload.exe"));
        Assert.That(forward.WasPathStripped(), Is.True);
        Assert.That(backward.WasPathStripped(), Is.True);
        Assert.That(forward.GetFilename(), Does.Not.Contain('/').And.Not.Contain('\\'));
        Assert.That(backward.GetFilename(), Does.Not.Contain('/').And.Not.Contain('\\'));
    }

    [Test]
    public void UnicodeNormalizationIsExplicitlyConfigurable()
    {
        const string decomposed = "cafe\u0301.txt";
        EmailFilenameOptions none = new EmailFilenameOptions();
        none.SetNormalization(EmailFilenameNormalization.None);
        EmailFilenameOptions compatibility = new EmailFilenameOptions();
        compatibility.SetNormalization(EmailFilenameNormalization.Nfkc);

        EmailFilenameResult normalized = new EmailFilename().SanitizeWithFilename(decomposed);

        Assert.That(normalized.GetFilename(), Is.EqualTo("café.txt"));
        Assert.That(normalized.WasUnicodeNormalized(), Is.True);
        Assert.That(new EmailFilename().SanitizeWithFilenameAndOptions(decomposed, none).GetFilename(), Is.EqualTo(decomposed));
        Assert.That(new EmailFilename().SanitizeWithFilenameAndOptions("Ｆｉｌｅ.txt", compatibility).GetFilename(), Is.EqualTo("File.txt"));
    }

    [Test]
    public void EmptyDotAndReservedNamesReceiveSafeFallbacksOrPrefixes()
    {
        Assert.That(new EmailFilename().SanitizeWithFilename("CON.txt").GetFilename(), Is.EqualTo("_CON.txt"));
        Assert.That(new EmailFilename().SanitizeWithFilename("lpt9.log").GetFilename(), Is.EqualTo("_lpt9.log"));
        Assert.That(new EmailFilename().SanitizeWithFilename(".").GetFilename(), Is.EqualTo("attachment"));

        EmailFilenameResult removed = new EmailFilename().SanitizeWithFilename("../\0");
        Assert.That(removed.GetFilename(), Is.EqualTo("attachment"));
        Assert.That(removed.UsedFallback(), Is.True);
        Assert.That(removed.WasReservedNameAdjusted(), Is.False);
    }

    [Test]
    public void MaximumLengthPreservesExtensionAndAvoidsSplittingUnicodePairs()
    {
        EmailFilenameOptions options = new EmailFilenameOptions();
        options.SetMaximumLength(20);

        EmailFilenameResult result = new EmailFilename().SanitizeWithFilenameAndOptions("abcdefghijklmnopqrstuvwx.txt", options);

        Assert.That(result.GetFilename(), Is.EqualTo("abcdefghijklmnop.txt"));
        Assert.That(result.GetFilename().Length, Is.EqualTo(20));
        Assert.That(result.WasTruncated(), Is.True);

        string emojiBase = string.Concat(Enumerable.Repeat("📎", 12)) + ".x";
        string safe = new EmailFilename().SanitizeWithFilenameAndOptions(emojiBase, options).GetFilename();
        Assert.That(char.IsHighSurrogate(safe[safe.Length - 1]), Is.False);
        Assert.That(safe, Does.EndWith(".x"));
    }

    [Test]
    public void SuggestAddsDeterministicCaseInsensitiveCollisionSuffixes()
    {
        List<string> existing = new List<string> { "Report.PDF", "report (2).pdf" };

        EmailFilenameResult result = new EmailFilename().SuggestWithFilenameAndExistingFilenames("report.pdf", existing);

        Assert.That(result.GetFilename(), Is.EqualTo("report (3).pdf"));
        Assert.That(result.HadCollision(), Is.True);
        Assert.That(result.GetCollisionIndex(), Is.EqualTo(3));

        EmailFilenameOptions sensitive = new EmailFilenameOptions();
        sensitive.SetCaseInsensitiveCollisions(false);
        Assert.That(new EmailFilename().SuggestWithFilenameAndExistingFilenamesAndOptions("report.pdf", existing, sensitive).GetFilename(), Is.EqualTo("report.pdf"));
    }

    [Test]
    public void SuggestNormalizesExistingNamesAndReportsSuffixTruncation()
    {
        EmailFilenameResult normalized = new EmailFilename().SuggestWithFilenameAndExistingFilenames("café.txt", new List<string> { "cafe\u0301.txt" });
        Assert.That(normalized.GetFilename(), Is.EqualTo("café (2).txt"));

        EmailFilenameOptions options = new EmailFilenameOptions();
        options.SetMaximumLength(20);
        EmailFilenameResult shortened = new EmailFilename().SuggestWithFilenameAndExistingFilenamesAndOptions("abcdefghijklmnop.txt", new List<string> { "abcdefghijklmnop.txt" }, options);
        Assert.That(shortened.GetFilename(), Is.EqualTo("abcdefghijkl (2).txt"));
        Assert.That(shortened.GetFilename().Length, Is.EqualTo(20));
        Assert.That(shortened.WasTruncated(), Is.True);
    }

    [Test]
    public void AttachmentViewsExposeSafeAndCollisionSafeSuggestions()
    {
        MimeEntity entity = ParseEntity("Content-Type: text/plain\r\nContent-Disposition: attachment; filename=\"../unsafe?.txt\"\r\n\r\ntext");
        EmailAttachmentView view = new EmailAttachmentFinder().FindWithRoot(entity)[0];

        Assert.That(view.GetFilename(), Is.EqualTo("../unsafe?.txt"));
        Assert.That(view.GetSafeFilename(), Is.EqualTo("unsafe_.txt"));
        Assert.That(view.SuggestSafeFilename(new List<string> { "UNSAFE_.TXT" }), Is.EqualTo("unsafe_ (2).txt"));
        Assert.That(view.GetSafeFilenameResult().WasPathStripped(), Is.True);
    }

    [Test]
    public void OptionsAndPublicMethodsValidateArguments()
    {
        EmailFilenameOptions options = new EmailFilenameOptions();

        Assert.Throws<ArgumentNullException>(() => new EmailFilename().Decode(null));
        Assert.Throws<ArgumentNullException>(() => new EmailFilename().SanitizeWithFilenameAndOptions("file", null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFilename().SuggestWithFilenameAndExistingFilenames("file", null!));
        Assert.Throws<ArgumentException>(() => new EmailFilename().SuggestWithFilenameAndExistingFilenames("file", new List<string> { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetNormalization((EmailFilenameNormalization)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumLength(15));
        Assert.Throws<ArgumentNullException>(() => options.SetFallbackFilename(null!));
        Assert.Throws<ArgumentException>(() => options.SetFallbackFilename(""));
        Assert.Throws<ArgumentException>(() => options.SetReplacementCharacter('/'));
        Assert.Throws<ArgumentException>(() => options.SetReplacementCharacter(' '));

        EmailFilenameOptions clone = options.Clone();
        clone.SetFallbackFilename("changed");
        Assert.That(options.GetFallbackFilename(), Is.EqualTo("attachment"));
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
