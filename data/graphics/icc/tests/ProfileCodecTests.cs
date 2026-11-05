using icc;

namespace icc.Tests;

public class ProfileCodecTests
{
    [Test]
    public void EmbeddedEnvelopeRetainsDeclaredBytesTrailingBytesAndAbsoluteLocation()
    {
        byte[] source = CreateProfileSource(7, 135, 132, IccConstants.ProfileFileSignature);
        source[7 + 4] = 9;
        source[7 + 132] = 10;
        source[7 + 133] = 11;
        source[7 + 134] = 12;
        byte[] expectedProfile = source[7..139];
        IccReadOptions options = new IccReadOptions();
        options.RequireExactProfileSize = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 7, 1000L, options);
        source[7 + 4] = 0;
        source[7 + 132] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(132));
            Assert.That(profile.Header.ProfileFileSignature, Is.EqualTo(IccConstants.ProfileFileSignature));
            Assert.That(profile.OriginalBytes, Is.EqualTo(expectedProfile));
            Assert.That(profile.TrailingBytes, Is.EqualTo(new byte[] { 10, 11, 12 }));
            Assert.That(profile.Location.Offset, Is.EqualTo(1007));
            Assert.That(profile.Location.Length, Is.EqualTo(132));
            Assert.That(profile.ReadResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(profile.ReadResult.Message, Does.Contain("3 trailing bytes"));
            Assert.That(profile.ReadResult.Location.Offset, Is.EqualTo(1007));
            Assert.That(profile.ReadResult.Location.Length, Is.EqualTo(132));
            Assert.That(profile.TagTableEntries, Is.Empty);
            Assert.That(profile.Tags, Is.Empty);
        });
    }

    [Test]
    public void ExactSizePolicyRejectsTrailingInput()
    {
        byte[] source = CreateProfileSource(0, 133, 132, IccConstants.ProfileFileSignature);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 40L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("ends at byte offset 172"));
            Assert.That(exception.Message, Does.Contain("1 trailing byte"));
            Assert.That(exception.Message, Does.Contain("exact profile size"));
        });
    }

    [Test]
    public void DeclaredProfileSizeMustIncludeHeaderAndTagCount()
    {
        byte[] source = CreateProfileSource(3, 132, 131, IccConstants.ProfileFileSignature);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 3, 20L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("byte offset 23"));
            Assert.That(exception.Message, Does.Contain("declares 131 bytes"));
            Assert.That(exception.Message, Does.Contain("minimum complete ICC profile size is 132"));
        });
    }

    [Test]
    public void InputMustContainACompleteHeaderAtTheEmbeddedOffset()
    {
        byte[] source = new byte[5 + 127];

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 5, 200L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("byte offset 205"));
            Assert.That(exception.Message, Does.Contain("127 bytes are available"));
            Assert.That(exception.Message, Does.Contain("requires 128 bytes"));
        });
    }

    [Test]
    public void DeclaredSizeCannotExtendPastAvailableInput()
    {
        byte[] source = CreateProfileSource(2, 132, 180, IccConstants.ProfileFileSignature);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 2, 500L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("byte offset 502"));
            Assert.That(exception.Message, Does.Contain("declares 180 bytes"));
            Assert.That(exception.Message, Does.Contain("only 132 bytes are available"));
        });
    }

    [Test]
    public void ProfileSizeCannotExceedConfiguredResourceLimit()
    {
        byte[] source = CreateProfileSource(0, 133, 133, IccConstants.ProfileFileSignature);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumProfileBytes = 132;

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("declares 133 bytes"));
            Assert.That(exception.Message, Does.Contain("configured maximum of 132 bytes"));
        });
    }

    [Test]
    public void UnsignedProfileSizeMustFitTheManagedProfileModel()
    {
        byte[] source = CreateProfileSource(0, 132, 0x80000000, IccConstants.ProfileFileSignature);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 90L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("byte offset 90"));
            Assert.That(exception.Message, Does.Contain("2147483648 bytes"));
            Assert.That(exception.Message, Does.Contain("managed profile model"));
        });
    }

    [Test]
    public void StrictModeRejectsInvalidAcspAtItsAbsoluteOffset()
    {
        byte[] source = CreateProfileSource(4, 132, 132, 0x42414421);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 4, 800L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("byte offset 840"));
            Assert.That(exception.Message, Does.Contain("0x42414421"));
            Assert.That(exception.Message, Does.Contain("'acsp'"));
        });
    }

    [Test]
    public void RecoveryModePreservesInvalidAcspWithAHeaderDiagnostic()
    {
        byte[] source = CreateProfileSource(0, 132, 132, 0x42414421);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 70L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileFileSignature, Is.EqualTo(0x42414421));
            Assert.That(profile.Header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.Header.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderFileSignature));
            Assert.That(profile.Header.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Error));
            Assert.That(profile.Header.ValidationIssues[0].Path, Is.EqualTo("Header.ProfileFileSignature"));
            Assert.That(profile.Header.ValidationIssues[0].Location.Offset, Is.EqualTo(106));
            Assert.That(profile.Header.ValidationIssues[0].Location.Length, Is.EqualTo(4));
            Assert.That(profile.Header.ValidationIssues[0].Remediation, Does.Contain("acsp"));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0], Is.SameAs(profile.Header.ValidationIssues[0]));
        });
    }

    [TestCase(-1)]
    [TestCase(133)]
    public void EmbeddedProfileOffsetMustBeInsideInput(int profileOffset)
    {
        byte[] source = new byte[132];

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, profileOffset, 0L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain($"offset {profileOffset}").And.Contain("132-byte input"));
    }

    [Test]
    public void OriginalBytesCanBeOmittedAfterEnvelopeValidation()
    {
        byte[] source = CreateProfileSource(0, 132, 132, IccConstants.ProfileFileSignature);
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.OriginalBytes, Is.Empty);
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(132));
            Assert.That(profile.ReadResult.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void EnvelopeRejectsMissingOrImpossibleReadLimits()
    {
        byte[] source = CreateProfileSource(0, 132, 132, IccConstants.ProfileFileSignature);
        IccReadOptions missingLimits = new IccReadOptions();
        missingLimits.ResourceLimits = null!;
        IccReadOptions tooSmall = new IccReadOptions();
        tooSmall.ResourceLimits.MaximumProfileBytes = 131;
        IccReadOptions negativeDiagnostics = new IccReadOptions();
        negativeDiagnostics.ResourceLimits.MaximumDiagnosticCount = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, null!));
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, missingLimits));
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, tooSmall));
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, negativeDiagnostics));
        });
    }

    private static byte[] CreateProfileSource(int prefixSize, int availableProfileBytes, uint declaredSize, uint fileSignature)
    {
        byte[] source = new byte[prefixSize + availableProfileBytes];
        WriteUInt32(source, prefixSize, declaredSize);
        WriteUInt32(source, prefixSize + 8, 0x04400000);
        WriteUInt32(source, prefixSize + 12, IccProfileClassSignatures.Display);
        WriteUInt32(source, prefixSize + 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(source, prefixSize + 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(source, prefixSize + 36, fileSignature);
        return source;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
