using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FramingEnvelopeExhaustiveTests
{
    [Test]
    public void EveryTwoChunkBoundaryCompletesAfterTheSecondChunk()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions();
        byte[] frame = BuildFrame(BuildBody(100));

        Assert.Multiple(() =>
        {
            for (int split = 0; split < frame.Length; split++)
            {
                FrameResult partial = framer.ReadFrame(frame.AsSpan(0, split), options);
                byte[] reassembled = Combine(frame.AsSpan(0, split).ToArray(), frame.AsSpan(split).ToArray());
                FrameResult complete = framer.ReadFrame(reassembled, options);

                Assert.That(partial.Status, Is.EqualTo(FrameStatus.Incomplete), $"split {split}");
                Assert.That(partial.Consumed, Is.Zero, $"split {split}");
                Assert.That(partial.Required, Is.GreaterThan(split), $"split {split}");
                Assert.That(complete.Status, Is.EqualTo(FrameStatus.Complete), $"split {split}");
                Assert.That(complete.FrameLength, Is.EqualTo(frame.Length), $"split {split}");
                Assert.That(complete.Consumed, Is.EqualTo(frame.Length), $"split {split}");
            }
        });
    }

    [Test]
    public void ConcatenatedFramesAreConsumedOneAtATime()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions options = new FrameOptions();
        byte[][] frames =
        [
            BuildFrame(Encoding.ASCII.GetBytes("35=0\u0001")),
            BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001")),
            BuildFrame(BuildBody(100))
        ];
        byte[] source = Combine(Combine(frames[0], frames[1]), frames[2]);
        int offset = 0;

        Assert.Multiple(() =>
        {
            for (int index = 0; index < frames.Length; index++)
            {
                FrameResult result = framer.ReadFrame(source.AsSpan(offset), options);

                Assert.That(result.Status, Is.EqualTo(FrameStatus.Complete), $"frame {index}");
                Assert.That(result.FrameOffset, Is.Zero, $"frame {index}");
                Assert.That(result.FrameLength, Is.EqualTo(frames[index].Length), $"frame {index}");
                Assert.That(result.Consumed, Is.EqualTo(frames[index].Length), $"frame {index}");
                offset += result.Consumed;
            }

            Assert.That(offset, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void RecoverySkipsEveryTestedJunkPrefixLength()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        FrameOptions recovery = new FrameOptions { RecoverFromJunk = true };
        FrameOptions strict = new FrameOptions();
        byte[] frame = BuildFrame(Encoding.ASCII.GetBytes("35=D\u000111=ABC\u0001"));

        Assert.Multiple(() =>
        {
            for (int junkLength = 1; junkLength <= frame.Length; junkLength++)
            {
                byte[] junk = new byte[junkLength];
                Array.Fill(junk, (byte)88);
                byte[] source = Combine(junk, frame);
                FrameResult rejected = framer.ReadFrame(source, strict);
                FrameResult recovered = framer.ReadFrame(source, recovery);

                Assert.That(rejected.Status, Is.EqualTo(FrameStatus.Invalid), $"junk length {junkLength}");
                Assert.That(rejected.Consumed, Is.Zero, $"junk length {junkLength}");
                Assert.That(recovered.Status, Is.EqualTo(FrameStatus.Recovered), $"junk length {junkLength}");
                Assert.That(recovered.FrameOffset, Is.EqualTo(junkLength), $"junk length {junkLength}");
                Assert.That(recovered.FrameLength, Is.EqualTo(frame.Length), $"junk length {junkLength}");
                Assert.That(recovered.Consumed, Is.EqualTo(source.Length), $"junk length {junkLength}");
            }
        });
    }

    [Test]
    public void MaximumFrameSizeIsInclusiveAtTheExactBoundary()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        byte[] frame = BuildFrame(BuildBody(100));
        int bodyStart = FindBodyStart(frame);

        FrameResult exact = framer.ReadFrame(frame, new FrameOptions { MaxFrameSize = frame.Length });
        FrameResult exceeded = framer.ReadFrame(frame.AsSpan(0, bodyStart), new FrameOptions { MaxFrameSize = frame.Length - 1 });
        FrameResult declaredOversized = framer.ReadFrame("8=FIX.4.4\u00019=999999\u0001"u8, new FrameOptions { MaxFrameSize = frame.Length });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(exact.FrameLength, Is.EqualTo(frame.Length));
            Assert.That(exceeded.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(exceeded.Required, Is.EqualTo(frame.Length));
            Assert.That(declaredOversized.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(declaredOversized.ErrorCode, Is.EqualTo("limit-exceeded"));
        });
    }

    [TestCase(5)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(99)]
    [TestCase(100)]
    [TestCase(999)]
    [TestCase(1000)]
    public void BodyLengthIsExactAcrossDecimalDigitBoundaries(int bodyLength)
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();
        byte[] frame = BuildFrame(BuildBody(bodyLength));

        BodyLengthResult envelope = BodyLengthCalculator.CalculateBodyLength(frame);
        FrameResult framed = framer.ReadFrame(frame, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(envelope.Success, Is.True);
            Assert.That(envelope.Matches, Is.True);
            Assert.That(envelope.DeclaredLength, Is.EqualTo(bodyLength));
            Assert.That(envelope.CalculatedLength, Is.EqualTo(bodyLength));
            Assert.That(envelope.ChecksumStart - envelope.BodyStart, Is.EqualTo(bodyLength));
            Assert.That(framed.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(framed.FrameLength, Is.EqualTo(frame.Length));
        });
    }

    [Test]
    public void EveryChecksumValueFormsAValidThreeDigitFrameTrailer()
    {
        TagValueMessageFramer framer = new TagValueMessageFramer();

        Assert.Multiple(() =>
        {
            for (int checksum = 0; checksum < FixDefaults.ChecksumModulo; checksum++)
            {
                byte[] frame = BuildFrameWithChecksum(checksum);
                int checksumStart = frame.Length - 7;
                FrameResult result = framer.ReadFrame(frame, new FrameOptions());
                BodyLengthResult envelope = BodyLengthCalculator.CalculateBodyLength(frame);
                AsciiIntegerResult parsed = ChecksumCalculator.ParseChecksum(frame.AsSpan(checksumStart + 3, 3));

                Assert.That(result.Status, Is.EqualTo(FrameStatus.Complete), checksum.ToString());
                Assert.That(result.FrameLength, Is.EqualTo(frame.Length), checksum.ToString());
                Assert.That(envelope.Success, Is.True, checksum.ToString());
                Assert.That(envelope.Matches, Is.True, checksum.ToString());
                Assert.That(parsed.Success, Is.True, checksum.ToString());
                Assert.That(parsed.Value, Is.EqualTo(checksum), checksum.ToString());
                Assert.That(ChecksumCalculator.CalculateChecksum(frame.AsSpan(0, checksumStart)), Is.EqualTo(checksum), checksum.ToString());
            }
        });
    }

    private static byte[] BuildBody(int bodyLength)
    {
        if (bodyLength < 5)
        {
            throw new ArgumentOutOfRangeException(nameof(bodyLength));
        }

        byte[] body = new byte[bodyLength];
        Array.Fill(body, (byte)88);
        body[0] = 51;
        body[1] = 53;
        body[2] = 61;
        body[3] = 68;
        body[4] = FixDefaults.Soh;
        return body;
    }

    private static byte[] BuildFrame(byte[] body)
    {
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={body.Length}\u0001");
        byte[] checksumInput = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        return Combine(checksumInput, BuildTrailer(checksum));
    }

    private static byte[] BuildFrameWithChecksum(int requestedChecksum)
    {
        byte[] body = [51, 53, 61, 68, FixDefaults.Soh, 88, 61, 0, FixDefaults.Soh];
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={body.Length}\u0001");
        byte[] checksumInput = Combine(header, body);
        int currentChecksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        body[7] = (byte)((requestedChecksum - currentChecksum) & 255);
        checksumInput = Combine(header, body);
        return Combine(checksumInput, BuildTrailer(requestedChecksum));
    }

    private static byte[] BuildTrailer(int checksum)
    {
        byte[] trailer = [49, 48, 61, 0, 0, 0, FixDefaults.Soh];
        bool written = ChecksumCalculator.WriteChecksum(checksum, trailer.AsSpan(3, 3));
        if (!written)
        {
            throw new ArgumentOutOfRangeException(nameof(checksum));
        }
        return trailer;
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
