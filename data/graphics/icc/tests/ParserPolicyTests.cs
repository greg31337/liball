using icc;

namespace icc.Tests;

public class ParserPolicyTests
{
    [Test]
    public void StrictRejectsAWarningThatConformingModeRetains()
    {
        byte[] source = CreateProfile(132, 0);
        source[100] = 1;
        IccReadOptions conformingOptions = new IccReadOptions();
        conformingOptions.Mode = IccReadMode.Conforming;

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 40L, new IccReadOptions()))!;
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 40L, conformingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("reserved").And.Contain("byte offset 140"));
            Assert.That(profile.Header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0], Is.SameAs(profile.Header.ValidationIssues[0]));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderReserved));
            Assert.That(profile.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Warning));
        });
    }

    [Test]
    public void ConformingRejectsAnErrorThatRecoveryModeRetains()
    {
        byte[] source = CreateProfile(156, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 148, 8);
        WriteUInt32(source, 148, IccTypeSignatures.Text);
        IccReadOptions conformingOptions = new IccReadOptions();
        conformingOptions.Mode = IccReadMode.Conforming;
        IccReadOptions recoveryOptions = new IccReadOptions();
        recoveryOptions.Mode = IccReadMode.Recovery;

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 100L, conformingOptions))!;
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 100L, recoveryOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("4-byte gap").And.Contain("before"));
            Assert.That(profile.Tags, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableGap));
            Assert.That(profile.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Error));
            Assert.That(profile.ValidationIssues[0].Location.Offset, Is.EqualTo(244));
            Assert.That(profile.ValidationIssues[0].Location.Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void RecoveryAccumulatesStructuralIssuesUpToTheConfiguredLimit()
    {
        byte[] source = CreateProfile(192, 3);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 168, 8);
        WriteEntry(source, 1, IccTagSignatures.Copyright, 172, 8);
        WriteEntry(source, 2, IccTagSignatures.ProfileDescription, 181, 8);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits.MaximumDiagnosticCount = 3;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 900L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.TagTableEntries, Has.Length.EqualTo(3));
            Assert.That(profile.Tags, Has.Length.EqualTo(3));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(3));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableDuplicateSignature));
            Assert.That(profile.ValidationIssues[1].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableAlignment));
            Assert.That(profile.ValidationIssues[2].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableOverlap));
            Assert.That(profile.ReadResult.Message, Does.Contain("3 retained diagnostics"));
        });
    }

    [Test]
    public void RecoveryEagerDecodeNeverReadsPastAMalformedTagBoundary()
    {
        byte[] source = CreateProfile(168, 2);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 156, 4);
        WriteEntry(source, 1, IccTagSignatures.ProfileDescription, 160, 8);
        WriteUInt32(source, 156, IccTypeSignatures.Text);
        WriteUInt32(source, 160, IccTypeSignatures.Data);
        source[167] = 0x7f;
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.DecodeTagsLazily = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 500L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(profile.Tags[0].Data.RawData.Bytes, Is.EqualTo(new byte[] { 0x74, 0x65, 0x78, 0x74 }));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(profile.Tags[1].Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Data));
            Assert.That(profile.Tags[1].Data.RawData.Bytes, Is.EqualTo(new byte[] { 0x64, 0x61, 0x74, 0x61, 0, 0, 0, 0x7f }));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagSize));
            Assert.That(profile.ValidationIssues[0].Location.Offset, Is.EqualTo(656));
            Assert.That(profile.ValidationIssues[0].Location.Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void FatalPayloadBoundsRemainRejectedInRecoveryMode()
    {
        byte[] source = CreateProfile(152, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 12);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, options))!;

        Assert.That(exception.Message, Does.Contain("outside the declared profile size 152"));
    }

    [Test]
    public void RetainedHeaderDiagnosticsHonorTheConfiguredLimit()
    {
        byte[] source = CreateProfile(132, 0);
        source[100] = 1;
        WriteUInt32(source, 36, 0x42414421);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits.MaximumDiagnosticCount = 1;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.HeaderReserved));
        });
    }

    [Test]
    public void InvalidParserPolicyOptionsAreRejectedBeforeReading()
    {
        byte[] source = CreateProfile(132, 0);
        IccReadOptions invalidMode = new IccReadOptions();
        invalidMode.Mode = (IccReadMode)99;
        IccReadOptions invalidLimit = new IccReadOptions();
        invalidLimit.ResourceLimits.MaximumDiagnosticCount = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, invalidMode));
            Assert.Throws<IccProfileValidationException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, invalidLimit));
        });
    }

    private static byte[] CreateProfile(int declaredSize, uint tagCount)
    {
        byte[] source = new byte[declaredSize];
        WriteUInt32(source, 0, (uint)declaredSize);
        WriteUInt32(source, 8, 0x04400000);
        WriteUInt32(source, 12, IccProfileClassSignatures.Display);
        WriteUInt32(source, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(source, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(source, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(source, 128, tagCount);
        return source;
    }

    private static void WriteEntry(byte[] destination, int entryIndex, uint signature, uint payloadOffset, uint payloadSize)
    {
        int offset = 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, payloadOffset);
        WriteUInt32(destination, offset + 8, payloadSize);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
