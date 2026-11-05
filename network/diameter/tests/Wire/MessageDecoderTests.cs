using System;

namespace diameter.Tests;

public class MessageDecoderTests
{
    [Test]
    public void CompleteDecodeReadsTheDeclaredFrameAndOwnsEveryAvpPayload()
    {
        DiameterMessage original = CreateUnknownMessage();
        DiameterEncodeResult encoding = MessageEncoder.EncodeMessageOwned(original);
        byte[] source = new byte[encoding.Data.Length + 4];
        Array.Copy(encoding.Data, source, encoding.Data.Length);
        Array.Fill(source, (byte)238, encoding.Data.Length, 4);

        DiameterDecodeResult result = MessageDecoder.DecodeMessage(
            source,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());
        source[28] = 99;

        Assert.Multiple(() =>
        {
            AssertSuccess(result, encoding.Data.Length, encoding.Data.Length);
            Assert.That(result.Message!.Flags, Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(result.Message.CommandCode, Is.EqualTo(400U));
            Assert.That(result.Message.ApplicationId, Is.EqualTo(17U));
            Assert.That(result.Message.HopByHopIdentifier, Is.EqualTo(16909060U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(2864434397U));
            Assert.That(result.Message.Avps, Has.Count.EqualTo(2));
            Assert.That(result.Message.Avps[0].Code, Is.EqualTo(4000000U));
            Assert.That(result.Message.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Message.Avps[1].Code, Is.EqualTo(4000001U));
            Assert.That(result.Message.Avps[1].VendorId, Is.EqualTo(4242U));
            Assert.That(result.Message.Avps[1].Data, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
        });
    }

    [Test]
    public void UnknownAvpsArePreservedForLosslessReencoding()
    {
        DiameterEncodeResult original = MessageEncoder.EncodeMessageOwned(CreateUnknownMessage());

        DiameterDecodeResult decoding = MessageDecoder.DecodeMessage(
            original.Data,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());
        DiameterEncodeResult reencoding = MessageEncoder.EncodeMessageOwned(decoding.Message);

        Assert.Multiple(() =>
        {
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(reencoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(reencoding.Data, Is.EqualTo(original.Data));
            Assert.That(reencoding.Data, Is.Not.SameAs(original.Data));
        });
    }

    [Test]
    public void CompleteDecodeWaitsForTheDeclaredMessageLength()
    {
        DiameterEncodeResult encoding = MessageEncoder.EncodeMessageOwned(CreateUnknownMessage());
        byte[] incomplete = new byte[encoding.Data.Length - 1];
        Array.Copy(encoding.Data, incomplete, incomplete.Length);

        DiameterDecodeResult result = MessageDecoder.DecodeMessage(
            incomplete,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            AssertFailure(result, DiameterCodecStatus.Incomplete, "incomplete-message");
            Assert.That(result.RequiredCount, Is.EqualTo(encoding.Data.Length));
            Assert.That(result.Issues[0].Offset, Is.EqualTo(incomplete.Length));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Information));
        });
    }

    [Test]
    public void PermissiveProfileRecoversFlagAndPaddingViolationsAsWarnings()
    {
        byte[] invalid = CreateSingleAvpFrame();
        invalid[4] = (byte)(invalid[4] | 1);
        invalid[24] = (byte)(invalid[24] | 1);
        invalid[30] = 7;

        DiameterDecodeResult strict = MessageDecoder.DecodeMessage(
            invalid,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());
        DiameterDecodeResult permissive = MessageDecoder.DecodeMessage(
            invalid,
            DiameterDecodeProfile.Permissive,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            AssertFailure(strict, DiameterCodecStatus.Invalid, "invalid-message-flags");
            Assert.That(strict.Issues[0].Offset, Is.EqualTo(4));
            AssertSuccess(permissive, invalid.Length, invalid.Length);
            Assert.That(permissive.Message!.Avps, Has.Count.EqualTo(1));
            Assert.That(permissive.Message.Avps[0].Data, Is.EqualTo(new byte[] { 42 }));
            Assert.That(permissive.Issues, Has.Count.EqualTo(3));
            AssertWarning(permissive.Issues[0], 4, "invalid-message-flags");
            AssertWarning(permissive.Issues[1], 24, "invalid-avp-flags");
            AssertWarning(permissive.Issues[2], 30, "nonzero-avp-padding");
        });
    }

    [Test]
    public void StrictProfileRejectsEachRecoverableAvpViolation()
    {
        byte[] invalidFlags = CreateSingleAvpFrame();
        invalidFlags[24] = (byte)(invalidFlags[24] | 1);
        byte[] invalidPadding = CreateSingleAvpFrame();
        invalidPadding[30] = 7;

        DiameterDecodeResult flags = MessageDecoder.DecodeMessage(
            invalidFlags,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());
        DiameterDecodeResult padding = MessageDecoder.DecodeMessage(
            invalidPadding,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            AssertFailure(flags, DiameterCodecStatus.Invalid, "invalid-avp-flags");
            Assert.That(flags.Issues[0].Offset, Is.EqualTo(24));
            AssertFailure(padding, DiameterCodecStatus.Invalid, "nonzero-avp-padding");
            Assert.That(padding.Issues[0].Offset, Is.EqualTo(30));
        });
    }

    [Test]
    public void PermissiveProfilePreservesARecoverableZeroVendorIdentifier()
    {
        DiameterMessage message = new DiameterMessage();
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 5000U;
        avp.Flags = DiameterAvpFlags.VendorSpecific;
        avp.VendorId = 1U;
        avp.Data = new byte[] { 9 };
        message.Avps.Add(avp);
        byte[] encoded = MessageEncoder.EncodeMessageOwned(message).Data;
        encoded[28] = 0;
        encoded[29] = 0;
        encoded[30] = 0;
        encoded[31] = 0;

        DiameterDecodeResult strict = MessageDecoder.DecodeMessage(
            encoded,
            DiameterDecodeProfile.Strict,
            new DiameterLimitConfiguration());
        DiameterDecodeResult permissive = MessageDecoder.DecodeMessage(
            encoded,
            DiameterDecodeProfile.Permissive,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            AssertFailure(strict, DiameterCodecStatus.Invalid, "invalid-vendor-id");
            Assert.That(strict.Issues[0].Offset, Is.EqualTo(28));
            AssertSuccess(permissive, encoded.Length, encoded.Length);
            Assert.That(permissive.Message!.Avps[0].VendorId, Is.Zero);
            Assert.That(permissive.Issues, Has.Count.EqualTo(1));
            AssertWarning(permissive.Issues[0], 28, "invalid-vendor-id");
        });
    }

    [Test]
    public void PermissiveProfileStillRejectsUnrecoverableAvpLengths()
    {
        byte[] truncatedAvp = CreateSingleAvpFrame();
        truncatedAvp[25] = 0;
        truncatedAvp[26] = 0;
        truncatedAvp[27] = 16;
        byte[] shortLength = CreateSingleAvpFrame();
        shortLength[25] = 0;
        shortLength[26] = 0;
        shortLength[27] = 7;

        DiameterDecodeResult truncated = MessageDecoder.DecodeMessage(
            truncatedAvp,
            DiameterDecodeProfile.Permissive,
            new DiameterLimitConfiguration());
        DiameterDecodeResult invalid = MessageDecoder.DecodeMessage(
            shortLength,
            DiameterDecodeProfile.Permissive,
            new DiameterLimitConfiguration());

        Assert.Multiple(() =>
        {
            AssertFailure(
                truncated,
                DiameterCodecStatus.Invalid,
                "invalid-message-avp-sequence");
            Assert.That(truncated.Issues[0].Offset, Is.EqualTo(32));
            AssertFailure(invalid, DiameterCodecStatus.Invalid, "invalid-avp-length");
            Assert.That(invalid.Issues[0].Offset, Is.EqualTo(25));
        });
    }

    [Test]
    public void ConfiguredMessageAndAvpCountLimitsAreEnforced()
    {
        byte[] encoded = MessageEncoder.EncodeMessageOwned(CreateUnknownMessage()).Data;
        DiameterLimitConfiguration sizeLimits = new DiameterLimitConfiguration();
        sizeLimits.MaximumMessageSize = 20;
        DiameterLimitConfiguration countLimits = new DiameterLimitConfiguration();
        countLimits.MaximumAvpCount = 1;

        DiameterDecodeResult size = MessageDecoder.DecodeMessage(
            encoded,
            DiameterDecodeProfile.Strict,
            sizeLimits);
        DiameterDecodeResult count = MessageDecoder.DecodeMessage(
            encoded,
            DiameterDecodeProfile.Strict,
            countLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(
                size,
                DiameterCodecStatus.LimitExceeded,
                "configured-message-size-limit");
            Assert.That(size.RequiredCount, Is.EqualTo(encoded.Length));
            Assert.That(size.Issues[0].Offset, Is.EqualTo(1));
            AssertFailure(
                count,
                DiameterCodecStatus.LimitExceeded,
                "message-avp-count-limit");
            Assert.That(count.Issues[0].Offset, Is.EqualTo(32));
        });
    }

    [Test]
    public void PermissiveWarningsAreBoundedByTheConfiguredIssueLimit()
    {
        DiameterMessage message = CreateUnknownMessage();
        byte[] encoded = MessageEncoder.EncodeMessageOwned(message).Data;
        encoded[4] = (byte)(encoded[4] | 1);
        encoded[24] = (byte)(encoded[24] | 1);
        encoded[36] = (byte)(encoded[36] | 1);
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumCodecIssueCount = 1;

        DiameterDecodeResult result = MessageDecoder.DecodeMessage(
            encoded,
            DiameterDecodeProfile.Permissive,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            AssertWarning(result.Issues[0], 4, "invalid-message-flags");
        });
    }

    [Test]
    public void CompleteDecodeRejectsInvalidProfilesAndLimits()
    {
        DiameterLimitConfiguration invalidLimits = new DiameterLimitConfiguration();
        invalidLimits.MaximumMessageSize = 21;

        DiameterDecodeResult profile = MessageDecoder.DecodeMessage(
            new byte[0],
            (DiameterDecodeProfile)99,
            new DiameterLimitConfiguration());
        DiameterDecodeResult missingLimits = MessageDecoder.DecodeMessage(
            new byte[0],
            DiameterDecodeProfile.Strict,
            null);
        DiameterDecodeResult invalid = MessageDecoder.DecodeMessage(
            new byte[0],
            DiameterDecodeProfile.Strict,
            invalidLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(profile, DiameterCodecStatus.Invalid, "invalid-decode-profile");
            AssertFailure(missingLimits, DiameterCodecStatus.Invalid, "invalid-decode-limits");
            AssertFailure(invalid, DiameterCodecStatus.Invalid, "invalid-decode-limits");
        });
    }

    private static DiameterMessage CreateUnknownMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = 400U;
        message.ApplicationId = 17U;
        message.HopByHopIdentifier = 16909060U;
        message.EndToEndIdentifier = 2864434397U;

        DiameterAvp first = new DiameterAvp();
        first.Code = 4000000U;
        first.Flags = DiameterAvpFlags.Mandatory;
        first.Data = new byte[] { 1, 2, 3 };
        message.Avps.Add(first);

        DiameterAvp second = new DiameterAvp();
        second.Code = 4000001U;
        second.Flags = DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Protected;
        second.VendorId = 4242U;
        second.Data = new byte[] { 4, 5, 6, 7 };
        message.Avps.Add(second);
        return message;
    }

    private static byte[] CreateSingleAvpFrame()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 1U;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[] { 42 };
        message.Avps.Add(avp);
        return MessageEncoder.EncodeMessageOwned(message).Data;
    }

    private static void AssertSuccess(
        DiameterDecodeResult result,
        int requiredCount,
        int consumedCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.EqualTo(consumedCount));
        Assert.That(result.Message, Is.Not.Null);
    }

    private static void AssertFailure(
        DiameterDecodeResult result,
        DiameterCodecStatus status,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Message, Is.Null);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }

    private static void AssertWarning(
        DiameterCodecIssue issue,
        int offset,
        string code)
    {
        Assert.That(issue.Severity, Is.EqualTo(DiameterIssueSeverity.Warning));
        Assert.That(issue.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(issue.Offset, Is.EqualTo(offset));
        Assert.That(issue.Code, Is.EqualTo(code));
        Assert.That(issue.Message, Is.Not.Empty);
    }
}
