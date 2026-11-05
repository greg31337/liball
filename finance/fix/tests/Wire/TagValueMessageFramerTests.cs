using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class TagValueMessageFramerTests
{
    [Test]
    public void StrictModeReturnsScannerResultsWithoutSkippingInput()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions();
        byte[] frame = BuildFrame("35=D\u000111=ABC\u0001");
        byte[] junked = Prefix(Encoding.ASCII.GetBytes("junk"), frame);

        FrameResult complete = framer.ReadFrame(frame, options);
        FrameResult incomplete = framer.ReadFrame(frame.AsSpan(0, frame.Length - 1), options);
        FrameResult invalid = framer.ReadFrame(junked, options);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(complete.FrameOffset, Is.Zero);
            Assert.That(complete.FrameLength, Is.EqualTo(frame.Length));
            Assert.That(incomplete.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(incomplete.Required, Is.EqualTo(frame.Length));
            Assert.That(invalid.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(invalid.ErrorCode, Is.EqualTo("missing-begin-string"));
            Assert.That(invalid.Consumed, Is.Zero);
        });
    }

    [Test]
    public void RecoveryReturnsACompleteFrameAfterJunkWithAbsoluteOffsets()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true };
        byte[] frame = BuildFrame("35=D\u000111=ABC\u0001");
        byte[] source = Prefix(Encoding.ASCII.GetBytes("junk"), frame);

        FrameResult result = framer.ReadFrame(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Recovered));
            Assert.That(result.FrameOffset, Is.EqualTo(4));
            Assert.That(result.FrameLength, Is.EqualTo(frame.Length));
            Assert.That(result.Consumed, Is.EqualTo(source.Length));
            Assert.That(result.Required, Is.EqualTo(source.Length));
            Assert.That(result.ErrorCode, Is.Empty);
        });
    }

    [Test]
    public void RecoverySkipsMalformedCandidatesAndChoosesALaterCompleteFrame()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true };
        byte[] malformed = Encoding.ASCII.GetBytes("8=BAD\u000135=X\u0001noise");
        byte[] valid = BuildFrame("35=0\u0001");
        byte[] source = Prefix(malformed, valid);

        FrameResult result = framer.ReadFrame(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Recovered));
            Assert.That(result.FrameOffset, Is.EqualTo(malformed.Length));
            Assert.That(result.FrameLength, Is.EqualTo(valid.Length));
            Assert.That(result.Consumed, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void RecoveryPreservesTheEarliestViablePartialFrame()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true };
        byte[] frame = BuildFrame("35=D\u000111=ABC\u0001");
        byte[] source = Prefix(Encoding.ASCII.GetBytes("bad"), frame.AsSpan(0, frame.Length - 3).ToArray());

        FrameResult result = framer.ReadFrame(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(result.FrameOffset, Is.EqualTo(3));
            Assert.That(result.Consumed, Is.EqualTo(3));
            Assert.That(result.Required, Is.EqualTo(3 + frame.Length));
            Assert.That(result.ErrorOffset, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void RecoveryConsumesJunkButRetainsATrailingBeginPrefix()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true };

        FrameResult junk = framer.ReadFrame("noise"u8, options);
        FrameResult trailing = framer.ReadFrame("noise8"u8, options);
        FrameResult trailingPair = framer.ReadFrame("noise8="u8, options);

        Assert.Multiple(() =>
        {
            Assert.That(junk.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(junk.FrameOffset, Is.EqualTo(5));
            Assert.That(junk.Consumed, Is.EqualTo(5));
            Assert.That(junk.Required, Is.EqualTo(6));
            Assert.That(trailing.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(trailing.FrameOffset, Is.EqualTo(5));
            Assert.That(trailing.Consumed, Is.EqualTo(5));
            Assert.That(trailing.Required, Is.EqualTo(7));
            Assert.That(trailingPair.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(trailingPair.FrameOffset, Is.EqualTo(5));
            Assert.That(trailingPair.Consumed, Is.EqualTo(5));
            Assert.That(trailingPair.Required, Is.EqualTo(8));
        });
    }

    [Test]
    public void RecoveryCanSkipAnOversizedCandidateForAValidLaterFrame()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true, MaxFrameSize = 64 };
        byte[] oversized = Encoding.ASCII.GetBytes("8=FIX.4.4\u00019=999\u0001noise");
        byte[] valid = BuildFrame("35=0\u0001");
        byte[] source = Prefix(oversized, valid);

        FrameResult recovered = framer.ReadFrame(source, options);
        FrameResult limited = framer.ReadFrame(oversized, options);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Status, Is.EqualTo(FrameStatus.Recovered));
            Assert.That(recovered.FrameOffset, Is.EqualTo(oversized.Length));
            Assert.That(limited.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(limited.FrameOffset, Is.Zero);
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
        });
    }

    [Test]
    public void VerificationPoliciesArePassedThroughDuringRecovery()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        byte[] frame = BuildFrame("35=D\u0001");
        frame[FindBodyStart(frame) + 3] = 69;
        byte[] source = Prefix(Encoding.ASCII.GetBytes("x"), frame);

        FrameResult strict = framer.ReadFrame(source, new FrameOptions { RecoverFromJunk = true });
        FrameResult relaxed = framer.ReadFrame(source, new FrameOptions { RecoverFromJunk = true, VerifyCheckSum = false });

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(strict.Consumed, Is.EqualTo(source.Length));
            Assert.That(relaxed.Status, Is.EqualTo(FrameStatus.Recovered));
            Assert.That(relaxed.FrameOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void RecoveryDoesNotMaskInvalidFrameLimits()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameResult negative = framer.ReadFrame("junk"u8, new FrameOptions { RecoverFromJunk = true, MaxFrameSize = -1 });
        FrameResult zero = framer.ReadFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { RecoverFromJunk = true, MaxFrameSize = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(negative.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(negative.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(zero.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(zero.ErrorCode, Is.EqualTo("limit-exceeded"));
        });
    }

    private static byte[] BuildFrame(string bodyText)
    {
        byte[] body = Encoding.ASCII.GetBytes(bodyText);
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={body.Length}\u0001");
        byte[] checksumInput = Prefix(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        byte[] trailer = Encoding.ASCII.GetBytes($"10={checksum:000}\u0001");
        return Prefix(checksumInput, trailer);
    }

    private static byte[] Prefix(byte[] prefix, byte[] suffix)
    {
        byte[] combined = new byte[prefix.Length + suffix.Length];
        prefix.CopyTo(combined, 0);
        suffix.CopyTo(combined, prefix.Length);
        return combined;
    }

    private static int FindBodyStart(byte[] frame)
    {
        int delimiters = 0;
        for (int index = 0; index < frame.Length; index++)
        {
            if (frame[index] == FixDefaults.Soh)
            {
                delimiters++;
                if (delimiters == 2)
                {
                    return index + 1;
                }
            }
        }
        return -1;
    }
}
