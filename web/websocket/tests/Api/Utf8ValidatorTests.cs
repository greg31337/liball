using websocket;

namespace websocket.Tests;

public class Utf8ValidatorTests
{
    [Test]
    public void ScalarValueBoundariesAreAccepted()
    {
        byte[][] validStreams =
        [
            [],
            [0x00],
            [0x7f],
            [0xc2, 0x80],
            [0xdf, 0xbf],
            [0xe0, 0xa0, 0x80],
            [0xe1, 0x80, 0x80],
            [0xec, 0xbf, 0xbf],
            [0xed, 0x9f, 0xbf],
            [0xee, 0x80, 0x80],
            [0xef, 0xbf, 0xbf],
            [0xf0, 0x90, 0x80, 0x80],
            [0xf1, 0x80, 0x80, 0x80],
            [0xf3, 0xbf, 0xbf, 0xbf],
            [0xf4, 0x8f, 0xbf, 0xbf]
        ];

        foreach (var stream in validStreams)
        {
            var validator = new WebSocketUtf8Validator();
            var chunkResult = validator.ValidateChunk(stream);
            var completeResult = validator.CompleteValidation();

            Assert.Multiple(() =>
            {
                Assert.That(chunkResult, Is.True, Convert.ToHexString(stream));
                Assert.That(completeResult, Is.True, Convert.ToHexString(stream));
            });
        }
    }

    [Test]
    public void ValidStreamIsAcceptedAtEveryTwoChunkSplit()
    {
        byte[] stream =
        [
            0x41,
            0xc2, 0xa2,
            0xe2, 0x82, 0xac,
            0xf0, 0x90, 0x8d, 0x88,
            0x42
        ];

        for (var split = 0; split <= stream.Length; split++)
        {
            var validator = new WebSocketUtf8Validator();

            var firstResult = validator.ValidateChunk(stream.AsSpan(0, split));
            var secondResult = validator.ValidateChunk(stream.AsSpan(split));
            var completeResult = validator.CompleteValidation();

            Assert.Multiple(() =>
            {
                Assert.That(firstResult, Is.True, $"First chunk at split {split}");
                Assert.That(secondResult, Is.True, $"Second chunk at split {split}");
                Assert.That(completeResult, Is.True, $"Completion at split {split}");
            });
        }
    }

    [Test]
    public void OneByteAndEmptyChunksPreserveSequenceState()
    {
        var validator = new WebSocketUtf8Validator();
        byte[] stream = [0xf0, 0x90, 0x8d, 0x88];

        Assert.That(validator.ValidateChunk([]), Is.True);
        foreach (var value in stream)
        {
            Assert.That(validator.ValidateChunk(new byte[] { value }), Is.True);
            Assert.That(validator.ValidateChunk([]), Is.True);
        }

        Assert.That(validator.CompleteValidation(), Is.True);
    }

    [TestCase("80")]
    [TestCase("c0af")]
    [TestCase("c1bf")]
    [TestCase("e08080")]
    [TestCase("eda080")]
    [TestCase("edbfbf")]
    [TestCase("f0808080")]
    [TestCase("f4908080")]
    [TestCase("f5808080")]
    [TestCase("ff")]
    [TestCase("e228a1")]
    [TestCase("c2c2")]
    public void InvalidUtf8IsRejected(string hexadecimalStream)
    {
        var validator = new WebSocketUtf8Validator();
        var stream = Convert.FromHexString(hexadecimalStream);

        Assert.That(validator.ValidateChunk(stream), Is.False);
        Assert.That(validator.CompleteValidation(), Is.False);
    }

    [Test]
    public void EveryForbiddenLeadingByteIsRejected()
    {
        for (int value = 0x80; value <= 0xc1; value++)
        {
            WebSocketUtf8Validator validator =
                new WebSocketUtf8Validator();

            Assert.That(
                validator.ValidateChunk([(byte)value]),
                Is.False,
                $"Leading byte 0x{value:X2}");
        }

        for (int value = 0xf5; value <= 0xff; value++)
        {
            WebSocketUtf8Validator validator =
                new WebSocketUtf8Validator();

            Assert.That(
                validator.ValidateChunk([(byte)value]),
                Is.False,
                $"Leading byte 0x{value:X2}");
        }
    }

    [Test]
    public void EveryNonContinuationByteFailsAtEveryContinuationPosition()
    {
        byte[][] sequences =
        [
            [0xc2, 0x80],
            [0xe1, 0x80, 0x80],
            [0xf1, 0x80, 0x80, 0x80]
        ];

        foreach (byte[] sequence in sequences)
        {
            for (int continuationIndex = 1;
                continuationIndex < sequence.Length;
                continuationIndex++)
            {
                for (int value = 0; value <= 0xff; value++)
                {
                    if (value >= 0x80 && value <= 0xbf)
                    {
                        continue;
                    }

                    byte[] invalid = (byte[])sequence.Clone();
                    invalid[continuationIndex] = (byte)value;
                    WebSocketUtf8Validator validator =
                        new WebSocketUtf8Validator();

                    Assert.That(
                        validator.ValidateChunk(invalid),
                        Is.False,
                        $"{Convert.ToHexString(invalid)} at byte " +
                        continuationIndex);
                }
            }
        }
    }

    [TestCase("c080")]
    [TestCase("c1bf")]
    [TestCase("e08080")]
    [TestCase("e09fbf")]
    [TestCase("f0808080")]
    [TestCase("f08fbfbf")]
    [TestCase("eda080")]
    [TestCase("edafbf")]
    [TestCase("edb080")]
    [TestCase("edbfbf")]
    [TestCase("f4908080")]
    [TestCase("f4bfbfbf")]
    public void OverlongSurrogateAndOutOfRangeBoundaryFormsAreRejected(
        string hexadecimalStream)
    {
        WebSocketUtf8Validator validator = new WebSocketUtf8Validator();
        byte[] stream = Convert.FromHexString(hexadecimalStream);

        Assert.That(validator.ValidateChunk(stream), Is.False);
        Assert.That(validator.CompleteValidation(), Is.False);
    }

    [Test]
    public void EveryMultibyteBoundaryPrefixIsRejectedAsTruncated()
    {
        byte[][] completeSequences =
        [
            [0xc2, 0x80],
            [0xdf, 0xbf],
            [0xe0, 0xa0, 0x80],
            [0xed, 0x9f, 0xbf],
            [0xee, 0x80, 0x80],
            [0xef, 0xbf, 0xbf],
            [0xf0, 0x90, 0x80, 0x80],
            [0xf4, 0x8f, 0xbf, 0xbf]
        ];

        foreach (byte[] sequence in completeSequences)
        {
            for (int prefixLength = 1;
                prefixLength < sequence.Length;
                prefixLength++)
            {
                WebSocketUtf8Validator validator =
                    new WebSocketUtf8Validator();

                bool chunkResult = validator.ValidateChunk(
                    sequence.AsSpan(0, prefixLength));
                bool completeResult = validator.CompleteValidation();

                Assert.Multiple(() =>
                {
                    Assert.That(
                        chunkResult,
                        Is.True,
                        Convert.ToHexString(
                            sequence.AsSpan(0, prefixLength)));
                    Assert.That(
                        completeResult,
                        Is.False,
                        Convert.ToHexString(
                            sequence.AsSpan(0, prefixLength)));
                });
            }
        }
    }

    [Test]
    public void MalformedStreamsAreRejectedAtEveryTwoChunkSplit()
    {
        byte[][] invalidStreams =
        [
            [0x41, 0xc0, 0xaf, 0x42],
            [0x41, 0xe0, 0x9f, 0xbf, 0x42],
            [0x41, 0xed, 0xa0, 0x80, 0x42],
            [0x41, 0xf4, 0x90, 0x80, 0x80, 0x42],
            [0x41, 0xe2, 0x28, 0xa1, 0x42]
        ];

        foreach (byte[] stream in invalidStreams)
        {
            for (int split = 0; split <= stream.Length; split++)
            {
                WebSocketUtf8Validator validator =
                    new WebSocketUtf8Validator();

                bool firstResult =
                    validator.ValidateChunk(stream.AsSpan(0, split));
                bool secondResult =
                    validator.ValidateChunk(stream.AsSpan(split));
                bool completeResult = validator.CompleteValidation();

                Assert.Multiple(() =>
                {
                    Assert.That(
                        firstResult && secondResult,
                        Is.False,
                        $"{Convert.ToHexString(stream)} split {split}");
                    Assert.That(
                        completeResult,
                        Is.False,
                        $"{Convert.ToHexString(stream)} split {split}");
                });
            }
        }
    }

    [Test]
    public void InvalidContinuationAcrossChunkBoundaryIsRejected()
    {
        var validator = new WebSocketUtf8Validator();

        Assert.That(validator.ValidateChunk(new byte[] { 0xe2 }), Is.True);
        Assert.That(validator.ValidateChunk(new byte[] { 0x82 }), Is.True);
        Assert.That(validator.ValidateChunk(new byte[] { 0x41 }), Is.False);
        Assert.That(validator.CompleteValidation(), Is.False);
    }

    [TestCase("c2")]
    [TestCase("e282")]
    [TestCase("f09080")]
    public void CompletionRejectsAnIncompleteFinalSequence(string hexadecimalStream)
    {
        var validator = new WebSocketUtf8Validator();
        var stream = Convert.FromHexString(hexadecimalStream);

        Assert.That(validator.ValidateChunk(stream), Is.True);
        Assert.That(validator.CompleteValidation(), Is.False);
        Assert.That(validator.ValidateChunk(new byte[] { 0x80 }), Is.False);
        Assert.That(validator.CompleteValidation(), Is.False);
    }

    [Test]
    public void ResetRecoversFromInvalidAndCompletedStreams()
    {
        var validator = new WebSocketUtf8Validator();

        Assert.That(validator.ValidateChunk(new byte[] { 0xff }), Is.False);
        validator.ResetValidation();
        Assert.That(validator.ValidateChunk(new byte[] { 0xc2, 0xa2 }), Is.True);
        Assert.That(validator.CompleteValidation(), Is.True);
        Assert.That(validator.ValidateChunk(new byte[] { 0x41 }), Is.False);

        validator.ResetValidation();

        Assert.That(validator.ValidateChunk(new byte[] { 0x41 }), Is.True);
        Assert.That(validator.CompleteValidation(), Is.True);
    }

    [Test]
    public void EmptyStreamCompletionIsSuccessfulAndIdempotent()
    {
        var validator = new WebSocketUtf8Validator();

        Assert.That(validator.CompleteValidation(), Is.True);
        Assert.That(validator.CompleteValidation(), Is.True);
    }
}
