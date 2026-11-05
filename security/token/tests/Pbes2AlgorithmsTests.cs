using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class Pbes2AlgorithmsTests
{
    [Test]
    public void UnwrapMatchesRfc7517AppendixCVector()
    {
        byte[] password = Encoding.UTF8.GetBytes(
            "Thus from my lips, by yours, my sin is purged.");
        byte[] saltInput = Convert.FromHexString(
            "D9609370967546F77F089B89AE2A50D7");
        byte[] wrappedKey = Convert.FromHexString(
            "4EBA973B0B8D51F0D5F553D335BC86BC427D24C8DE7C0567F93475B88C51F69E"
            + "A1B11421F5393B04");
        byte[] expected = Convert.FromHexString(
            "6F1B1934421D144E5CB038F041D05270A1832437CAECB9AC811799C2C330FDB6");

        TokenBytesResult result = Pbes2AlgorithmsOperations.Unwrap(
            wrappedKey,
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            saltInput,
            4096);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [TestCase(
        JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
        JoseAlgorithms.Aes128KeyWrap,
        "SHA256",
        16)]
    [TestCase(
        JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
        JoseAlgorithms.Aes192KeyWrap,
        "SHA384",
        24)]
    [TestCase(
        JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
        JoseAlgorithms.Aes256KeyWrap,
        "SHA512",
        32)]
    public void WrapDerivesInteroperableKeysForEveryAlgorithm(
        string algorithm,
        string keyWrapAlgorithm,
        string hashName,
        int keySize)
    {
        byte[] password = Sequence(29, 3);
        byte[] keyMaterial = Sequence(32, 51);

        Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
            keyMaterial,
            password,
            algorithm,
            Pbes2Algorithms.MinimumIterationCount);
        byte[] salt = Salt(algorithm, wrapped.SaltInput);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            wrapped.IterationCount,
            new HashAlgorithmName(hashName),
            keySize);
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            derivedKey,
            keyWrapAlgorithm).Key;
        TokenBytesResult unwrapped = AesKeyWrap.Unwrap(
            wrapped.EncryptedKey,
            key,
            keyWrapAlgorithm);

        Assert.Multiple(() =>
        {
            AssertWrapSuccess(wrapped, keyMaterial.Length);
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial));
        });
    }

    [Test]
    public void DefaultWrapUsesFreshSaltAndDefaultIterationCount()
    {
        byte[] password = Bytes(24, 1);
        byte[] keyMaterial = Bytes(16, 2);

        Pbes2KeyWrapResult first = Pbes2AlgorithmsOperations.Wrap(
            keyMaterial,
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap);
        Pbes2KeyWrapResult second = Pbes2AlgorithmsOperations.Wrap(
            keyMaterial,
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap);

        Assert.Multiple(() =>
        {
            Assert.That(first.IterationCount,
                Is.EqualTo(Pbes2Algorithms.DefaultIterationCount));
            Assert.That(second.IterationCount,
                Is.EqualTo(Pbes2Algorithms.DefaultIterationCount));
            Assert.That(first.SaltInput,
                Has.Length.EqualTo(Pbes2Algorithms.GeneratedSaltInputSize));
            Assert.That(first.SaltInput, Is.Not.EqualTo(second.SaltInput));
            Assert.That(first.EncryptedKey, Is.Not.EqualTo(second.EncryptedKey));
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        byte[] keyMaterial = Container(Bytes(32, 4));
        byte[] password = Container(Bytes(24, 5));

        Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
            keyMaterial.AsSpan(1, 32),
            password.AsSpan(1, 24),
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            Pbes2Algorithms.MinimumIterationCount);
        byte[] encryptedContainer = Container(wrapped.EncryptedKey);
        byte[] saltContainer = Container(wrapped.SaltInput);
        TokenBytesResult unwrapped = Pbes2AlgorithmsOperations.Unwrap(
            encryptedContainer.AsSpan(1, wrapped.EncryptedKey.Length),
            password.AsSpan(1, 24),
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            saltContainer.AsSpan(1, wrapped.SaltInput.Length),
            wrapped.IterationCount);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            Assert.That(unwrapped.Success, Is.True);
            Assert.That(unwrapped.Bytes, Is.EqualTo(Bytes(32, 4)));
            Assert.That(keyMaterial[0], Is.Zero);
            Assert.That(keyMaterial[33], Is.Zero);
            Assert.That(password[0], Is.Zero);
            Assert.That(password[25], Is.Zero);
        });
    }

    [Test]
    public void WrapValidatesPasswordKeyMaterialAndIterationCount()
    {
        byte[] password = Bytes(16, 1);
        string algorithm = JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap;

        Assert.Multiple(() =>
        {
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2),
                Array.Empty<byte>(),
                algorithm,
                1000), "invalid_password");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(8, 2),
                password,
                algorithm,
                1000), "invalid_key_size");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(17, 2),
                password,
                algorithm,
                1000), "invalid_key_size");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2),
                password,
                algorithm,
                Pbes2Algorithms.MinimumIterationCount - 1),
                "invalid_iteration_count");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2),
                password,
                algorithm,
                Pbes2Algorithms.MaximumIterationCount + 1),
                "invalid_iteration_count");
        });
    }

    [Test]
    public void MinimumIterationCountIsAccepted()
    {
        Pbes2KeyWrapResult result = Pbes2AlgorithmsOperations.Wrap(
            Bytes(16, 2),
            Bytes(16, 1),
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            Pbes2Algorithms.MinimumIterationCount);

        AssertWrapSuccess(result, 16);
    }

    [Test]
    public void UnwrapUsesUniformFailureForMalformedOrIncorrectInputs()
    {
        byte[] password = Bytes(24, 7);
        string algorithm = JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap;
        Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
            Bytes(32, 8),
            password,
            algorithm,
            Pbes2Algorithms.MinimumIterationCount);
        byte[] changedWrappedKey = (byte[])wrapped.EncryptedKey.Clone();
        changedWrappedKey[0] ^= 1;
        byte[] changedSalt = (byte[])wrapped.SaltInput.Clone();
        changedSalt[0] ^= 1;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                Bytes(16, 1), password, algorithm, wrapped.SaltInput, 1000),
                "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                Bytes(25, 1), password, algorithm, wrapped.SaltInput, 1000),
                "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey, password, algorithm, Bytes(7, 1), 1000),
                "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey,
                password,
                algorithm,
                Bytes(Pbes2Algorithms.MaximumSaltInputSize + 1, 1),
                1000), "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey, password, algorithm, wrapped.SaltInput, 999),
                "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey,
                password,
                algorithm,
                wrapped.SaltInput,
                Pbes2Algorithms.MaximumIterationCount + 1), "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                changedWrappedKey,
                password,
                algorithm,
                wrapped.SaltInput,
                1000), "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey,
                Bytes(24, 9),
                algorithm,
                wrapped.SaltInput,
                1000), "unwrap_failed");
            AssertBytesFailure(Pbes2AlgorithmsOperations.Unwrap(
                wrapped.EncryptedKey,
                password,
                algorithm,
                changedSalt,
                1000), "unwrap_failed");
        });
    }

    [Test]
    public void AlgorithmNameIsPartOfPbkdf2Salt()
    {
        byte[] password = Bytes(24, 10);
        Pbes2KeyWrapResult wrapped = Pbes2AlgorithmsOperations.Wrap(
            Bytes(16, 11),
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            Pbes2Algorithms.MinimumIterationCount);

        TokenBytesResult result = Pbes2AlgorithmsOperations.Unwrap(
            wrapped.EncryptedKey,
            password,
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            wrapped.SaltInput,
            wrapped.IterationCount);

        AssertBytesFailure(result, "unwrap_failed");
    }

    [Test]
    public void UnsupportedAlgorithmsReturnStableFailures()
    {
        byte[] password = Bytes(16, 1);

        Assert.Multiple(() =>
        {
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2), password, "pbes2-hs256+a128kw", 1000),
                "unknown_algorithm");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2), password, JoseAlgorithms.Aes128KeyWrap, 1000),
                "invalid_algorithm");
            AssertWrapFailure(Pbes2AlgorithmsOperations.Wrap(
                Bytes(16, 2), password, JoseAlgorithms.Aes128Cbc, 1000),
                "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        byte[] keyMaterial = Bytes(16, 1);
        byte[] password = Bytes(16, 2);
        byte[] wrappedKey = Bytes(24, 3);
        byte[] saltInput = Bytes(16, 4);
        string algorithm = JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap;

        Assert.Multiple(() =>
        {
            Assert.That(() => Pbes2AlgorithmsOperations.Wrap(
                null, password, algorithm), Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Wrap(
                keyMaterial, null, algorithm), Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Wrap(
                keyMaterial, password, null), Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Unwrap(
                null, password, algorithm, saltInput, 1000),
                Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Unwrap(
                wrappedKey, null, algorithm, saltInput, 1000),
                Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Unwrap(
                wrappedKey, password, null, saltInput, 1000),
                Throws.ArgumentNullException);
            Assert.That(() => Pbes2AlgorithmsOperations.Unwrap(
                wrappedKey, password, algorithm, null, 1000),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Salt(string algorithm, byte[] saltInput)
    {
        byte[] algorithmBytes = Encoding.UTF8.GetBytes(algorithm);
        byte[] salt = new byte[algorithmBytes.Length + 1 + saltInput.Length];
        algorithmBytes.CopyTo(salt, 0);
        saltInput.CopyTo(salt, algorithmBytes.Length + 1);
        return salt;
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

    static void AssertWrapSuccess(Pbes2KeyWrapResult result, int keySize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.EncryptedKey, Has.Length.EqualTo(keySize + 8));
            Assert.That(result.SaltInput,
                Has.Length.EqualTo(Pbes2Algorithms.GeneratedSaltInputSize));
            Assert.That(result.IterationCount,
                Is.InRange(
                    Pbes2Algorithms.MinimumIterationCount,
                    Pbes2Algorithms.MaximumIterationCount));
        });
    }

    static void AssertWrapFailure(Pbes2KeyWrapResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.EncryptedKey, Is.Null);
            Assert.That(result.SaltInput, Is.Null);
            Assert.That(result.IterationCount, Is.Zero);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
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
