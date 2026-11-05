using System.Text;
using email;

namespace email.Tests;

public class EmailFingerprintTests
{
    [Test]
    public void ExactHashMatchesThePreservedSourceBytes()
    {
        byte[] source = Encoding.ASCII.GetBytes("sUbJeCt: exact\nX-Fold: one\r\n\ttwo\n\nbody\n");
        EmailMessage message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;

        EmailHashResult actual = new EmailFingerprint().ComputeExactWithMessageAndAlgorithm(message!, EmailHashAlgorithm.Sha384);
        EmailHashResult expected = new EmailHash().ComputeExactWithContentAndAlgorithm(source, EmailHashAlgorithm.Sha384);

        Assert.That(actual.GetLowercaseHex(), Is.EqualTo(expected.GetLowercaseHex()));
        Assert.That(actual.GetHashedByteCount(), Is.EqualTo(source.LongLength));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void SemanticFingerprintIgnoresTransportAndPresentationDifferences()
    {
        EmailMessage plain = ParseMessage(
            "subject: semantic\n"
            + "Content-Type: text/plain; charset=us-ascii\n"
            + "Content-Transfer-Encoding: 7bit\n\n"
            + "body line\n");
        EmailMessage encoded = ParseMessage(
            "Subject: semantic\r\n"
            + "Content-Type: text/plain; charset=ascii\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + "Ym9keSBsaW5lCg==\r\n");

        EmailHashResult plainExact = new EmailFingerprint().ComputeExactWithMessage(plain);
        EmailHashResult encodedExact = new EmailFingerprint().ComputeExactWithMessage(encoded);
        EmailHashResult plainSemantic = new EmailFingerprint().ComputeSemanticWithMessage(plain);
        EmailHashResult encodedSemantic = new EmailFingerprint().ComputeSemanticWithMessage(encoded);

        Assert.That(plainExact.GetLowercaseHex(), Is.Not.EqualTo(encodedExact.GetLowercaseHex()));
        Assert.That(plainSemantic.GetLowercaseHex(), Is.EqualTo(encodedSemantic.GetLowercaseHex()));
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(plain, encoded).AreEqual(), Is.True);
        Assert.That(plain.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(encoded.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void TraceAndAuthenticationDifferencesCanBeIgnoredExplicitly()
    {
        EmailMessage left = ParseMessage(
            "Subject: same\r\n"
            + "Received: from left.example by mx.example; Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Authentication-Results: mx.example; dkim=pass header.d=left.example\r\n\r\nbody");
        EmailMessage right = ParseMessage(
            "Subject: same\r\n"
            + "Received: from right.example by mx.example; Fri, 21 Nov 1997 10:55:06 -0600\r\n"
            + "Authentication-Results: mx.example; dkim=fail header.d=right.example\r\n\r\nbody");
        EmailCanonicalComparisonResult strict = new EmailCanonicalComparer().CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(left, right);
        EmailCanonicalComparisonOptions options = new EmailCanonicalComparisonOptions();
        options.SetIgnoreReceivedHeaders(true);
        options.SetIgnoreAuthenticationHeaders(true);

        EmailCanonicalComparisonResult ignored = new EmailCanonicalComparer().CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, options);

        Assert.That(strict.AreEqual(), Is.False);
        Assert.That(ignored.AreEqual(), Is.True);
        Assert.That(ignored.GetLeftFingerprint().GetAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha256));
        Assert.That(ignored.GetLeftFingerprint().GetLowercaseHex(), Is.EqualTo(ignored.GetRightFingerprint().GetLowercaseHex()));
    }

    [Test]
    public void CustomIgnoredFieldsAndHeaderOrderingAreIndependentOptions()
    {
        EmailMessage left = ParseMessage("X-First: one\r\nX-Second: two\r\nX-Tracking: left\r\n\r\nbody");
        EmailMessage right = ParseMessage("X-Second: two\r\nX-First: one\r\nX-Tracking: right\r\n\r\nbody");
        EmailCanonicalComparisonOptions ignoredTracking = new EmailCanonicalComparisonOptions();
        ignoredTracking.AddIgnoredHeaderName("X-Tracking");
        EmailCanonicalComparisonOptions unordered = ignoredTracking.Clone();
        unordered.SetIgnoreHeaderOrder(true);

        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(left, right).AreEqual(), Is.False);
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, ignoredTracking).AreEqual(), Is.False);
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, unordered).AreEqual(), Is.True);

        MimeEntity same = new EmailEntityFactory().CreateTextWithValue("body");
        MimeEntity different = new EmailEntityFactory().CreateTextWithValue("different");
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightUsingMimeEntityAndMimeEntity(same, same.Clone()).AreEqual(), Is.True);
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightUsingMimeEntityAndMimeEntity(same, different).AreEqual(), Is.False);
    }

    [Test]
    public void IdentityDateAndSubjectPoliciesAreConfigurable()
    {
        EmailMessage left = ParseMessage(
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Message-ID: <left@example.test>\r\n"
            + "Subject: left\r\n\r\nbody");
        EmailMessage right = ParseMessage(
            "Date: Sat, 22 Nov 1997 09:55:06 -0600\r\n"
            + "Message-ID: <right@example.test>\r\n"
            + "Subject: right\r\n\r\nbody");
        EmailCanonicalComparisonOptions options = new EmailCanonicalComparisonOptions();
        options.SetIgnoreDateHeaders(true);
        options.SetIgnoreMessageIdentifiers(true);
        options.SetIgnoreSubjectHeaders(true);

        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(left, right).AreEqual(), Is.False);
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, options).AreEqual(), Is.True);
    }

    [Test]
    public void OptionsResultsArgumentsAndLimitsAreDefensive()
    {
        EmailCanonicalComparisonOptions options = new EmailCanonicalComparisonOptions();
        options.SetHashAlgorithm(EmailHashAlgorithm.Sha512);
        options.AddIgnoredHeaderName("X-One");
        options.AddIgnoredHeaderName("x-one");
        options.SetDeterministicSeed(new byte[] { 1, 2, 3 });
        EmailCanonicalComparisonOptions clone = options.Clone();
        List<string> names = clone.GetIgnoredHeaderNames();
        names.Clear();
        byte[] seed = clone.GetDeterministicSeed();
        seed[0] = 9;
        EmailMessage message = ParseMessage("Subject: value\r\n\r\nbody");

        Assert.That(clone.GetIgnoredHeaderNameCount(), Is.EqualTo(1));
        Assert.That(clone.GetDeterministicSeed()[0], Is.EqualTo(1));
        Assert.That(clone.RemoveIgnoredHeaderName("X-One"), Is.True);
        Assert.That(clone.GetIgnoredHeaderNameCount(), Is.Zero);
        Assert.Throws<ArgumentNullException>(() => new EmailFingerprint().ComputeExactWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFingerprint().ComputeSemanticWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFingerprint().ComputeSemanticWithMessageAndOptions(message, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailCanonicalComparer().CompareWithLeftAndRightUsingEmailMessageAndEmailMessage((EmailMessage)null!, message));
        Assert.Throws<ArgumentException>(() => options.AddIgnoredHeaderName("Bad:Name"));
        Assert.Throws<ArgumentNullException>(() => options.SetDeterministicSeed(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumOutputBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<EmailLimitException>(() => new EmailFingerprint().ComputeExactWithMessageAndAlgorithmAndMaximumBytes(message, EmailHashAlgorithm.Sha256, 1));

        message.GetHeaders()!.GetFirst("Subject")!.SetValue("changed");
        Assert.Throws<InvalidOperationException>(() => new EmailFingerprint().ComputeExactWithMessage(message));

        EmailCanonicalComparisonOptions shallow = new EmailCanonicalComparisonOptions();
        shallow.SetMaximumDepth(0);
        MimeEntity deep = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("child") });
        Assert.Throws<EmailLimitException>(() => new EmailFingerprint().ComputeSemanticWithEntityAndOptions(deep, shallow));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }
}
