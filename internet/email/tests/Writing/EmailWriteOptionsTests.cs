using email;

namespace email.Tests;

public class EmailWriteOptionsTests
{
    [Test]
    public void DefaultsSelectMinimalAsciiSevenBitWriting()
    {
        EmailWriteOptions options = new EmailWriteOptions();

        Assert.That(options.GetMode(), Is.EqualTo(EmailWriteMode.MinimalRegeneration));
        Assert.That(options.IsMinimalRegeneration(), Is.True);
        Assert.That(options.IsExactPreservation(), Is.False);
        Assert.That(options.IsCanonical(), Is.False);
        Assert.That(options.GetHeaderWriteMode(), Is.EqualTo(EmailHeaderWriteMode.Preserving));
        Assert.That(options.GetAllowSmtpUtf8(), Is.False);
        Assert.That(options.GetTransportConstraint(), Is.EqualTo(EmailTransferEncodingConstraint.SevenBit));
        Assert.That(options.GetDeterministicOutput(), Is.False);
        Assert.That(options.GetIncludeBlindRecipientHeaders(), Is.False);
        Assert.That(options.GetDeterministicSeed(), Is.Empty);
        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(options.GetMaximumNestingDepth(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(options.GetMaximumPartCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxPartCount));
    }

    [Test]
    public void PresetsRepresentExactMinimalAndCanonicalPolicies()
    {
        EmailWriteOptions exact = new EmailWriteOptionsOperations().CreateExactPreservation();
        EmailWriteOptions minimal = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        EmailWriteOptions preserving = new EmailWriteOptionsOperations().CreatePreserving();
        EmailWriteOptions internet = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        EmailWriteOptions smtpUtf8 = new EmailWriteOptionsOperations().CreateCanonicalSmtpUtf8();

        Assert.That(exact.IsExactPreservation(), Is.True);
        Assert.That(exact.GetHeaderWriteMode(), Is.EqualTo(EmailHeaderWriteMode.Raw));
        Assert.That(exact.GetIncludeBlindRecipientHeaders(), Is.True);
        Assert.That(minimal.IsMinimalRegeneration(), Is.True);
        Assert.That(preserving.IsMinimalRegeneration(), Is.True);
        Assert.That(internet.IsCanonical(), Is.True);
        Assert.That(internet.GetHeaderWriteMode(), Is.EqualTo(EmailHeaderWriteMode.Canonical));
        Assert.That(internet.GetAllowSmtpUtf8(), Is.False);
        Assert.That(internet.GetTransportConstraint(), Is.EqualTo(EmailTransferEncodingConstraint.SevenBit));
        Assert.That(smtpUtf8.IsCanonical(), Is.True);
        Assert.That(smtpUtf8.GetAllowSmtpUtf8(), Is.True);
        Assert.That(smtpUtf8.GetTransportConstraint(), Is.EqualTo(EmailTransferEncodingConstraint.EightBit));
    }

    [TestCase(EmailTransferEncodingConstraint.SevenBit)]
    [TestCase(EmailTransferEncodingConstraint.EightBit)]
    [TestCase(EmailTransferEncodingConstraint.Binary)]
    public void SupportsEveryTransportConstraint(EmailTransferEncodingConstraint constraint)
    {
        EmailWriteOptions options = new EmailWriteOptions();

        options.SetTransportConstraint(constraint);

        Assert.That(options.GetTransportConstraint(), Is.EqualTo(constraint));
    }

    [Test]
    public void SmtpUtf8PolicyAndHeaderPolicyStayConsistent()
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetAllowSmtpUtf8(true);

        EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
        Assert.That(headerOptions.GetAllowInternationalized(), Is.True);
        headerOptions.SetAllowInternationalized(false);
        options.SetHeaderWriteOptions(headerOptions);

        Assert.That(options.GetAllowSmtpUtf8(), Is.False);
    }

    [Test]
    public void CloneAndArrayGettersDoNotShareMutablePolicyState()
    {
        EmailWriteOptions original = new EmailWriteOptionsOperations().CreateCanonicalSmtpUtf8();
        byte[] suppliedSeed = new byte[] { 1, 2, 3 };
        original.SetDeterministicOutput(true);
        original.SetDeterministicSeed(suppliedSeed);
        original.SetIncludeBlindRecipientHeaders(true);
        original.SetMaximumOutputBytes(1234);
        original.SetMaximumNestingDepth(7);
        original.SetMaximumPartCount(9);
        EmailHeaderWriteOptions headerOptions = original.GetHeaderWriteOptions();
        headerOptions.SetPreferredLineLength(64);
        original.SetHeaderWriteOptions(headerOptions);

        suppliedSeed[0] = 99;
        byte[] returnedSeed = original.GetDeterministicSeed();
        returnedSeed[1] = 99;
        EmailWriteOptions clone = original.Clone();
        clone.SetAllowSmtpUtf8(false);
        clone.SetDeterministicSeed(new byte[] { 8 });
        EmailHeaderWriteOptions cloneHeaders = clone.GetHeaderWriteOptions();
        cloneHeaders.SetPreferredLineLength(40);
        clone.SetHeaderWriteOptions(cloneHeaders);

        Assert.That(original.GetDeterministicSeed(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(original.GetAllowSmtpUtf8(), Is.True);
        Assert.That(original.GetHeaderWriteOptions().GetPreferredLineLength(), Is.EqualTo(64));
        Assert.That(clone.GetDeterministicOutput(), Is.True);
        Assert.That(clone.GetIncludeBlindRecipientHeaders(), Is.True);
        Assert.That(clone.GetMaximumOutputBytes(), Is.EqualTo(1234));
        Assert.That(clone.GetMaximumNestingDepth(), Is.EqualTo(7));
        Assert.That(clone.GetMaximumPartCount(), Is.EqualTo(9));
    }

    [Test]
    public void InvalidValuesAreRejectedImmediately()
    {
        EmailWriteOptions options = new EmailWriteOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailWriteMode)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailWriteMode)3));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTransportConstraint((EmailTransferEncodingConstraint)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTransportConstraint((EmailTransferEncodingConstraint)3));
        Assert.Throws<ArgumentNullException>(() => options.SetHeaderWriteOptions(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetDeterministicSeed(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumOutputBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumNestingDepth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumPartCount(0));
    }
}
