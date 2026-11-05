using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class TagValueFrameScannerTests
{
    [Test]
    public void ScannerReturnsOneCompleteFrameFromConcatenatedInput()
    {
        byte[] first = BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001"), 12);
        byte[] second = BuildFrame(Encoding.ASCII.GetBytes("35=0\u0001"), 5);
        byte[] concatenated = Combine(first, second);

        FrameResult result = TagValueFrameScanner.ScanFrame(concatenated, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(result.FrameOffset, Is.Zero);
            Assert.That(result.FrameLength, Is.EqualTo(first.Length));
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Required, Is.EqualTo(first.Length));
            Assert.That(result.IsSofh, Is.False);
            Assert.That(result.EncodingType, Is.Zero);
        });
    }

    [Test]
    public void EveryPrefixIsIncompleteAndReportsTheNextKnownRequirement()
    {
        byte[] frame = BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001"), 12);
        int bodyStart = FindBodyStart(frame);

        Assert.Multiple(() =>
        {
            for (int length = 0; length < frame.Length; length++)
            {
                FrameResult result = TagValueFrameScanner.ScanFrame(frame.AsSpan(0, length), new FrameOptions());
                Assert.That(result.Status, Is.EqualTo(FrameStatus.Incomplete), length.ToString());
                Assert.That(result.Consumed, Is.Zero, length.ToString());
                Assert.That(result.Required, Is.GreaterThan(length), length.ToString());
                Assert.That(result.ErrorCode, Is.EqualTo("incomplete"), length.ToString());
                if (length >= bodyStart)
                {
                    Assert.That(result.Required, Is.EqualTo(frame.Length), length.ToString());
                }
            }
        });
    }

    [Test]
    public void DeclaredBodyLengthSetsAnExactRequirementBeforeTheBodyArrives()
    {
        byte[] frame = BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001"), 12);
        int bodyStart = FindBodyStart(frame);

        FrameResult result = TagValueFrameScanner.ScanFrame(frame.AsSpan(0, bodyStart), new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(result.Required, Is.EqualTo(frame.Length));
            Assert.That(result.ErrorOffset, Is.EqualTo(bodyStart));
        });
    }

    [Test]
    public void ScannerUsesBodyLengthInsteadOfSearchingEmbeddedDataForSoh()
    {
        byte[] body = [51, 53, 61, 68, 1, 57, 53, 61, 51, 1, 57, 54, 61, 65, 1, 66, 1];
        byte[] frame = BuildFrame(body, body.Length);

        FrameResult result = TagValueFrameScanner.ScanFrame(frame, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(result.FrameLength, Is.EqualTo(frame.Length));
        });
    }

    [Test]
    public void OversizedFramesAreRejectedAsSoonAsTheirSizeIsKnown()
    {
        byte[] full = BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001"), 12);
        int bodyStart = FindBodyStart(full);
        FrameOptions oneByteShort = new FrameOptions { MaxFrameSize = full.Length - 1 };
        FrameOptions small = new FrameOptions { MaxFrameSize = 64 };
        FrameOptions zero = new FrameOptions { MaxFrameSize = 0 };

        FrameResult knownSize = TagValueFrameScanner.ScanFrame(full.AsSpan(0, bodyStart), oneByteShort);
        FrameResult hugeDeclaration = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u00019=999\u0001"u8, small);
        FrameResult partialHugeDeclaration = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u00019=999"u8, small);
        FrameResult integerOverflow = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u00019=2147483648\u0001"u8, new FrameOptions());
        FrameResult zeroLimit = TagValueFrameScanner.ScanFrame(ReadOnlySpan<byte>.Empty, zero);

        Assert.Multiple(() =>
        {
            Assert.That(knownSize.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(knownSize.Required, Is.EqualTo(full.Length));
            Assert.That(hugeDeclaration.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(partialHugeDeclaration.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(integerOverflow.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(integerOverflow.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(zeroLimit.Status, Is.EqualTo(FrameStatus.LimitExceeded));
        });
    }

    [Test]
    public void MalformedEnvelopePrefixesAndLengthsAreInvalid()
    {
        FrameOptions options = new FrameOptions();
        FrameResult begin = TagValueFrameScanner.ScanFrame("7=FIX.4.4\u0001"u8, options);
        FrameResult emptyBegin = TagValueFrameScanner.ScanFrame("8=\u0001"u8, options);
        FrameResult bodyTag = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u000135=D\u0001"u8, options);
        FrameResult lengthDigits = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u00019=1X\u0001"u8, options);
        FrameResult zeroLength = TagValueFrameScanner.ScanFrame("8=FIX.4.4\u00019=0\u0001"u8, options);
        FrameResult invalidLimit = TagValueFrameScanner.ScanFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { MaxFrameSize = -1 });

        Assert.Multiple(() =>
        {
            Assert.That(begin.ErrorCode, Is.EqualTo("missing-begin-string"));
            Assert.That(begin.ErrorOffset, Is.Zero);
            Assert.That(emptyBegin.ErrorCode, Is.EqualTo("empty-begin-string"));
            Assert.That(bodyTag.ErrorCode, Is.EqualTo("missing-body-length"));
            Assert.That(lengthDigits.ErrorCode, Is.EqualTo("invalid-body-length"));
            Assert.That(zeroLength.ErrorCode, Is.EqualTo("zero-body-length"));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
        });
    }

    [Test]
    public void ChecksumFieldShapeAndValueAreValidatedIndependently()
    {
        byte[] invalidTag = BuildFrame(Encoding.ASCII.GetBytes("35=D\u0001"), 5);
        int checksumStart = invalidTag.Length - 7;
        invalidTag[checksumStart] = 49;
        invalidTag[checksumStart + 1] = 49;
        byte[] invalidDigits = BuildFrame(Encoding.ASCII.GetBytes("35=D\u0001"), 5);
        invalidDigits[invalidDigits.Length - 4] = 88;
        byte[] invalidDelimiter = BuildFrame(Encoding.ASCII.GetBytes("35=D\u0001"), 5);
        invalidDelimiter[invalidDelimiter.Length - 1] = 124;
        byte[] mismatch = BuildFrame(Encoding.ASCII.GetBytes("35=D\u0001"), 5);
        mismatch[FindBodyStart(mismatch) + 3] = 69;

        FrameResult tagResult = TagValueFrameScanner.ScanFrame(invalidTag, new FrameOptions());
        FrameResult digitResult = TagValueFrameScanner.ScanFrame(invalidDigits, new FrameOptions());
        FrameResult delimiterResult = TagValueFrameScanner.ScanFrame(invalidDelimiter, new FrameOptions());
        FrameResult mismatchResult = TagValueFrameScanner.ScanFrame(mismatch, new FrameOptions());
        FrameResult uncheckedResult = TagValueFrameScanner.ScanFrame(mismatch, new FrameOptions { VerifyCheckSum = false });

        Assert.Multiple(() =>
        {
            Assert.That(tagResult.ErrorCode, Is.EqualTo("invalid-checksum-field"));
            Assert.That(digitResult.ErrorCode, Is.EqualTo("invalid-checksum-field"));
            Assert.That(delimiterResult.ErrorCode, Is.EqualTo("invalid-checksum-delimiter"));
            Assert.That(mismatchResult.ErrorCode, Is.EqualTo("checksum-mismatch"));
            Assert.That(uncheckedResult.Status, Is.EqualTo(FrameStatus.Complete));
        });
    }

    [Test]
    public void BodyLengthMismatchIsExplicitAndCanBeAcceptedByPolicy()
    {
        byte[] body = Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001");
        byte[] tooSmall = BuildFrame(body, body.Length - 1);
        byte[] tooLarge = BuildFrame(body, body.Length + 1);
        FrameOptions strict = new FrameOptions();
        FrameOptions relaxed = new FrameOptions { VerifyBodyLength = false };

        FrameResult strictSmall = TagValueFrameScanner.ScanFrame(tooSmall, strict);
        FrameResult strictLarge = TagValueFrameScanner.ScanFrame(tooLarge, strict);
        FrameResult relaxedSmall = TagValueFrameScanner.ScanFrame(tooSmall, relaxed);
        FrameResult relaxedLarge = TagValueFrameScanner.ScanFrame(tooLarge, relaxed);

        Assert.Multiple(() =>
        {
            Assert.That(strictSmall.ErrorCode, Is.EqualTo("body-length-mismatch"));
            Assert.That(strictLarge.ErrorCode, Is.EqualTo("body-length-mismatch"));
            Assert.That(relaxedSmall.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(relaxedSmall.FrameLength, Is.EqualTo(tooSmall.Length));
            Assert.That(relaxedLarge.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(relaxedLarge.FrameLength, Is.EqualTo(tooLarge.Length));
        });
    }

    private static byte[] BuildFrame(byte[] body, int declaredLength)
    {
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={declaredLength}\u0001");
        byte[] checksumInput = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        byte[] trailer = Encoding.ASCII.GetBytes($"10={checksum:000}\u0001");
        return Combine(checksumInput, trailer);
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

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
