using System.Security.Cryptography;

namespace random.Tests;

public class RandomStateTests
{
    [Test]
    public void StateMetadataAndPayloadAreDefensivelyCopied()
    {
        byte[] payload = [1, 2, 3, 4, 5, 6, 7, 8];
        RandomState state = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, payload);
        payload[0] = 99;
        byte[] returnedPayload = state.Payload();
        returnedPayload[1] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(state.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(state.AlgorithmVersion(), Is.EqualTo(1));
            Assert.That(state.TransformVersion(), Is.EqualTo(1));
            Assert.That(state.Payload(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        });
    }

    [Test]
    public void BinaryEnvelopeHasCanonicalHeaderPayloadAndDigest()
    {
        // Provenance: independent byte layout and SHA-256 fixture for the project's RNDSTATE v1
        // envelope; digest computed over the 28-byte header followed by payload 10 20 30 40.
        byte[] payload = [0x10, 0x20, 0x30, 0x40];
        RandomState state = StateCodec.Create(RandomAlgorithm.Pcg64DxsmV1, 1, 1, payload);
        byte[] encoded = state.ToBytes();
        int digestOffset = StateCodec.HeaderSize + payload.Length;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(StateCodec.MinimumEnvelopeSize + payload.Length));
            Assert.That(encoded.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 0x52, 0x4E, 0x44, 0x53, 0x54, 0x41, 0x54, 0x45 }));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 8), Is.EqualTo(1U));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 12), Is.EqualTo((uint)RandomAlgorithm.Pcg64DxsmV1));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 16), Is.EqualTo(1U));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 20), Is.EqualTo(1U));
            Assert.That(BinaryCodec.ReadUInt32(encoded, 24), Is.EqualTo(4U));
            Assert.That(BinaryCodec.ReadBytes(encoded, StateCodec.HeaderSize, payload.Length), Is.EqualTo(payload));
            Assert.That(BinaryCodec.ReadBytes(encoded, digestOffset, StateCodec.DigestSize), Is.EqualTo(SHA256.HashData(encoded.AsSpan(0, digestOffset))));
            Assert.That(Convert.ToHexString(encoded), Is.EqualTo(
                "524E445354415445010000000100000001000000010000000400000010203040" +
                "5718292D857C24F852DED15521C27FB2DE38C9CE16CEF1F8FB8D503893322A89"));
        });
    }

    [Test]
    public void BinaryAndBase64RoundTripsAreStable()
    {
        RandomState original = StateCodec.Create(
            RandomAlgorithm.Xoshiro256StarStarV1,
            1,
            1,
            Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());

        byte[] binary = original.ToBytes();
        RandomState fromBinary = RandomState.FromBytes(binary);
        string base64 = original.ToBase64();
        RandomState fromBase64 = RandomState.FromBase64(base64);
        binary[StateCodec.HeaderSize] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(fromBinary.ToBytes(), Is.EqualTo(original.ToBytes()));
            Assert.That(fromBase64.ToBytes(), Is.EqualTo(original.ToBytes()));
            Assert.That(fromBase64.ToBase64(), Is.EqualTo(base64));
        });
    }

    [Test]
    public void CorruptionTruncationAndTrailingBytesAreRejected()
    {
        byte[] valid = CreateValidEnvelope();
        byte[] corruptedPayload = (byte[])valid.Clone();
        corruptedPayload[StateCodec.HeaderSize] ^= 0x80;
        byte[] corruptedDigest = (byte[])valid.Clone();
        corruptedDigest[corruptedDigest.Length - 1] ^= 0x01;
        byte[] badMagic = (byte[])valid.Clone();
        badMagic[0] ^= 0x01;
        byte[] truncated = valid[..^1];
        byte[] trailing = new byte[valid.Length + 1];
        valid.CopyTo(trailing, 0);

        AssertStateFailure(corruptedPayload);
        AssertStateFailure(corruptedDigest);
        AssertStateFailure(badMagic);
        AssertStateFailure(truncated);
        AssertStateFailure(trailing);
        AssertStateFailure(new byte[StateCodec.MinimumEnvelopeSize - 1]);
    }

    [Test]
    public void UnknownVersionsAlgorithmsAndSecureAlgorithmsAreRejectedAfterIntegrityValidation()
    {
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 8, 2U));
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 12, 999U));
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 12, (uint)RandomAlgorithm.SystemCryptoV1));
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 12, (uint)RandomAlgorithm.HmacDrbgSha256V1));
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 16, 2U));
        AssertStateFailure(ChangeHeaderAndRehash(CreateValidEnvelope(), 20, 2U));
    }

    [Test]
    public void InvalidPayloadLengthsAndEmptyPayloadAreRejected()
    {
        byte[] valid = CreateValidEnvelope();
        AssertStateFailure(ChangeHeaderAndRehash(valid, 24, uint.MaxValue));

        byte[] empty = new byte[StateCodec.MinimumEnvelopeSize];
        BinaryCodec.WriteUInt64(empty, 0, 0x4554415453444E52UL);
        BinaryCodec.WriteUInt32(empty, 8, 1U);
        BinaryCodec.WriteUInt32(empty, 12, (uint)RandomAlgorithm.SplitMix64V1);
        BinaryCodec.WriteUInt32(empty, 16, 1U);
        BinaryCodec.WriteUInt32(empty, 20, 1U);
        BinaryCodec.WriteUInt32(empty, 24, 0U);
        Rehash(empty);
        AssertStateFailure(empty);

        RandomArgumentException createError = Assert.Throws<RandomArgumentException>(
            () => StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, []));
        Assert.That(createError.ArgumentName(), Is.EqualTo("payload"));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("!!!!")]
    [TestCase("AAAA AAAA")]
    [TestCase("A===")]
    public void MalformedBase64IsReportedAsStateFailure(string encoded)
    {
        Assert.Throws<RandomStateException>(() => RandomState.FromBase64(encoded));
    }

    [Test]
    public void FailedDecodeDoesNotChangeExistingStateOrInput()
    {
        RandomState existing = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, [1, 2, 3, 4]);
        byte[] before = existing.ToBytes();
        byte[] invalid = (byte[])before.Clone();
        invalid[StateCodec.HeaderSize] ^= 0x40;
        byte[] invalidBefore = (byte[])invalid.Clone();

        Assert.Throws<RandomStateException>(() => RandomState.FromBytes(invalid));

        Assert.Multiple(() =>
        {
            Assert.That(existing.ToBytes(), Is.EqualTo(before));
            Assert.That(invalid, Is.EqualTo(invalidBefore));
        });
    }

    [Test]
    public void NullInputsUseArgumentFailures()
    {
        RandomArgumentException bytesError = Assert.Throws<RandomArgumentException>(
            () => RandomState.FromBytes(NullReference.Value<byte[]>()));
        RandomArgumentException textError = Assert.Throws<RandomArgumentException>(
            () => RandomState.FromBase64(NullReference.Value<string>()));

        Assert.That(bytesError.ArgumentName(), Is.EqualTo("encoded"));
        Assert.That(textError.ArgumentName(), Is.EqualTo("encoded"));
    }

    static byte[] CreateValidEnvelope()
    {
        return StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, [1, 2, 3, 4, 5, 6, 7, 8]).ToBytes();
    }

    static byte[] ChangeHeaderAndRehash(byte[] source, int offset, uint value)
    {
        byte[] changed = (byte[])source.Clone();
        BinaryCodec.WriteUInt32(changed, offset, value);
        Rehash(changed);
        return changed;
    }

    static void Rehash(byte[] envelope)
    {
        int digestOffset = envelope.Length - StateCodec.DigestSize;
        byte[] digest = SHA256.HashData(envelope.AsSpan(0, digestOffset));
        digest.CopyTo(envelope, digestOffset);
    }

    static void AssertStateFailure(byte[] encoded)
    {
        Assert.Throws<RandomStateException>(() => RandomState.FromBytes(encoded));
    }
}
