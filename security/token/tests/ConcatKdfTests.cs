using System.Text;
using token;

namespace token.Tests;

public class ConcatKdfTests
{
    [Test]
    public void DeriveKeyMatchesRfc7518AppendixCVector()
    {
        byte[] sharedSecret = new byte[]
        {
            158, 86, 217, 29, 129, 113, 53, 211,
            114, 131, 66, 131, 191, 132, 38, 156,
            251, 49, 110, 163, 218, 128, 106, 72,
            246, 218, 167, 121, 140, 254, 144, 196
        };
        byte[] expected = new byte[]
        {
            86, 170, 141, 234, 248, 35, 109, 32,
            92, 34, 40, 205, 113, 167, 16, 26
        };

        TokenBytesResult result = ConcatKdfOperations.DeriveKey(
            sharedSecret,
            JoseAlgorithms.Aes128Gcm,
            Encoding.UTF8.GetBytes("Alice"),
            Encoding.UTF8.GetBytes("Bob"),
            128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void DeriveKeySupportsMultipleSha256RoundsAndSpanSlices()
    {
        byte[] sharedSecret = Container(Sequence(32, 1));
        byte[] partyU = Container(Encoding.UTF8.GetBytes("producer"));
        byte[] partyV = Container(Encoding.UTF8.GetBytes("consumer"));

        TokenBytesResult first = ConcatKdfOperations.DeriveKey(
            sharedSecret.AsSpan(1, 32),
            JoseAlgorithms.Aes256CbcHmacSha512,
            partyU.AsSpan(1, 8),
            partyV.AsSpan(1, 8),
            512);
        TokenBytesResult second = ConcatKdfOperations.DeriveKey(
            Sequence(32, 1),
            JoseAlgorithms.Aes256CbcHmacSha512,
            Encoding.UTF8.GetBytes("producer"),
            Encoding.UTF8.GetBytes("consumer"),
            512);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Bytes, Has.Length.EqualTo(64));
            Assert.That(first.Bytes, Is.EqualTo(second.Bytes));
            Assert.That(first.Bytes.AsSpan(0, 32).ToArray(),
                Is.Not.EqualTo(first.Bytes.AsSpan(32, 32).ToArray()));
        });
    }

    [Test]
    public void AgreementParametersAndKeySizeAreBoundIntoOutput()
    {
        byte[] secret = Sequence(32, 9);
        byte[] partyU = Encoding.UTF8.GetBytes("Alice");
        byte[] partyV = Encoding.UTF8.GetBytes("Bob");

        TokenBytesResult baseline = ConcatKdfOperations.DeriveKey(
            secret, "A128GCM", partyU, partyV, 128);
        TokenBytesResult algorithm = ConcatKdfOperations.DeriveKey(
            secret, "A256GCM", partyU, partyV, 128);
        TokenBytesResult producer = ConcatKdfOperations.DeriveKey(
            secret, "A128GCM", Encoding.UTF8.GetBytes("Carol"), partyV, 128);
        TokenBytesResult consumer = ConcatKdfOperations.DeriveKey(
            secret, "A128GCM", partyU, Encoding.UTF8.GetBytes("Dave"), 128);
        TokenBytesResult size = ConcatKdfOperations.DeriveKey(
            secret, "A128GCM", partyU, partyV, 256);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Bytes, Is.Not.EqualTo(algorithm.Bytes));
            Assert.That(baseline.Bytes, Is.Not.EqualTo(producer.Bytes));
            Assert.That(baseline.Bytes, Is.Not.EqualTo(consumer.Bytes));
            Assert.That(baseline.Bytes,
                Is.Not.EqualTo(size.Bytes.AsSpan(0, 16).ToArray()));
        });
    }

    [Test]
    public void InvalidInputsReturnStableFailures()
    {
        Assert.Multiple(() =>
        {
            AssertFailure(ConcatKdfOperations.DeriveKey(
                Array.Empty<byte>(), "A128GCM", Array.Empty<byte>(), Array.Empty<byte>(), 128),
                "invalid_shared_secret");
            AssertFailure(ConcatKdfOperations.DeriveKey(
                Bytes(32, 1), "", Array.Empty<byte>(), Array.Empty<byte>(), 128),
                "invalid_algorithm_id");
            AssertFailure(ConcatKdfOperations.DeriveKey(
                Bytes(32, 1), "A128GCM", Array.Empty<byte>(), Array.Empty<byte>(), 0),
                "invalid_key_size");
            AssertFailure(ConcatKdfOperations.DeriveKey(
                Bytes(32, 1), "A128GCM", Array.Empty<byte>(), Array.Empty<byte>(), 127),
                "invalid_key_size");
            AssertFailure(ConcatKdfOperations.DeriveKey(
                Bytes(32, 1),
                "A128GCM",
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                ConcatKdf.MaximumKeySizeInBits + 8), "invalid_key_size");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        byte[] secret = Bytes(32, 1);
        byte[] info = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(() => ConcatKdfOperations.DeriveKey(
                null, "A128GCM", info, info, 128), Throws.ArgumentNullException);
            Assert.That(() => ConcatKdfOperations.DeriveKey(
                secret, null, info, info, 128), Throws.ArgumentNullException);
            Assert.That(() => ConcatKdfOperations.DeriveKey(
                secret, "A128GCM", null, info, 128), Throws.ArgumentNullException);
            Assert.That(() => ConcatKdfOperations.DeriveKey(
                secret, "A128GCM", info, null, 128), Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static byte[] Sequence(int length, int start)
    {
        byte[] bytes = new byte[length];
        for (int index = 0; index < length; index += 1)
        {
            bytes[index] = (byte)(start + index);
        }

        return bytes;
    }

    static byte[] Container(byte[] value)
    {
        byte[] container = new byte[value.Length + 2];
        value.CopyTo(container, 1);
        return container;
    }

    static void AssertFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
