using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseHmacAlgorithmsTests
{
    [TestCase(
        CoseAlgorithms.HmacSha256Truncated64,
        8,
        "60e431591ee0b67f")]
    [TestCase(
        CoseAlgorithms.HmacSha256,
        32,
        "60e431591ee0b67f0d8a26aacbf5b77f8e0bc6213728c5140546040f0ee37f54")]
    [TestCase(
        CoseAlgorithms.HmacSha384,
        48,
        "4ece084485813e9088d2c63a041bc5b44f9ef1012a2b588f3cd11f05033ac4c60c2ef6ab4030fe8296248df163f44952")]
    [TestCase(
        CoseAlgorithms.HmacSha512,
        64,
        "80b24263c7c1a3ebb71493c1dd7be8b49b46d1f41b4aeec1121b013783f8f3526b56d037e05f2598bd0fd2215d6a1e5295e64f73f63f0aec8b915a985d786598")]
    public void CreateAndVerifyMatchRfc4231CaseSix(
        int algorithm,
        int tagLength,
        string expectedHex)
    {
        byte[] secret = Repeated(131, 0xAA);
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "Test Using Larger Than Block-Size Key - Hash Key First");
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            secret,
            algorithm).Key;

        TokenBytesResult tag = CoseHmacAlgorithms.Create(
            data,
            key,
            algorithm);
        TokenResult verification = CoseHmacAlgorithms.Verify(
            data,
            Convert.FromHexString(expectedHex),
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Success, Is.True);
            Assert.That(tag.Error, Is.Null);
            Assert.That(tag.Bytes, Has.Length.EqualTo(tagLength));
            Assert.That(tag.Bytes,
                Is.EqualTo(Convert.FromHexString(expectedHex)));
            Assert.That(verification.Success, Is.True);
            Assert.That(verification.Error, Is.Null);
            Assert.That(key.SymmetricKey, Is.EqualTo(secret));
        });
    }

    [Test]
    public void TruncatedAlgorithmUsesLeftmostSha256Bytes()
    {
        byte[] secret = Repeated(32, 0x3A);
        byte[] data = new byte[] { 1, 2, 3, 4 };
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            secret,
            CoseAlgorithms.HmacSha256Truncated64).Key;
        byte[] complete = HMACSHA256.HashData(secret, data);

        TokenBytesResult result = CoseHmacAlgorithms.Create(
            data,
            key,
            CoseAlgorithms.HmacSha256Truncated64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(complete[..8]));
        });
    }

    [Test]
    public void SpanOverloadsAuthenticateOnlySelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Repeated(48, 0x31),
            CoseAlgorithms.HmacSha384).Key;
        byte[] data = new byte[] { 99, 1, 2, 3, 88 };

        TokenBytesResult sliced = CoseHmacAlgorithms.Create(
            data.AsSpan(1, 3),
            key,
            CoseAlgorithms.HmacSha384);
        TokenBytesResult exact = CoseHmacAlgorithms.Create(
            new byte[] { 1, 2, 3 },
            key,
            CoseAlgorithms.HmacSha384);
        byte[] wrapped = new byte[sliced.Bytes.Length + 2];
        sliced.Bytes.CopyTo(wrapped, 1);
        TokenResult verified = CoseHmacAlgorithms.Verify(
            data.AsSpan(1, 3),
            wrapped.AsSpan(1, sliced.Bytes.Length),
            key,
            CoseAlgorithms.HmacSha384);

        Assert.Multiple(() =>
        {
            Assert.That(sliced.Success, Is.True);
            Assert.That(sliced.Bytes, Is.EqualTo(exact.Bytes));
            Assert.That(verified.Success, Is.True);
        });
    }

    [TestCase(CoseAlgorithms.HmacSha256Truncated64, 8)]
    [TestCase(CoseAlgorithms.HmacSha256, 32)]
    [TestCase(CoseAlgorithms.HmacSha384, 48)]
    [TestCase(CoseAlgorithms.HmacSha512, 64)]
    public void VerifyRejectsAlteredInputsAndIncorrectTagLengths(
        int algorithm,
        int tagLength)
    {
        int keyLength = algorithm == CoseAlgorithms.HmacSha512
            ? 64
            : algorithm == CoseAlgorithms.HmacSha384
                ? 48
                : 32;
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Repeated(keyLength, 7),
            algorithm).Key;
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenBytesResult created = CoseHmacAlgorithms.Create(
            data,
            key,
            algorithm);
        byte[] alteredData = data.ToArray();
        alteredData[0] ^= 1;
        byte[] alteredTag = created.Bytes.ToArray();
        alteredTag[alteredTag.Length - 1] ^= 1;

        TokenResult wrongData = CoseHmacAlgorithms.Verify(
            alteredData,
            created.Bytes,
            key,
            algorithm);
        TokenResult wrongTag = CoseHmacAlgorithms.Verify(
            data,
            alteredTag,
            key,
            algorithm);
        TokenResult shortTag = CoseHmacAlgorithms.Verify(
            data,
            created.Bytes[..^1],
            key,
            algorithm);
        TokenResult longTag = CoseHmacAlgorithms.Verify(
            data,
            new byte[tagLength + 1],
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "mac_mismatch");
            AssertFailure(wrongTag, "mac_mismatch");
            AssertFailure(shortTag, "mac_mismatch");
            AssertFailure(longTag, "mac_mismatch");
        });
    }

    [Test]
    public void JoseAndCoseAlgorithmMetadataMustMatchExactly()
    {
        TokenKey matchingJose = SymmetricKeyImporterOperations.ImportJose(
            Repeated(32, 1),
            JoseAlgorithms.HmacSha256).Key;
        TokenKey wrongJose = SymmetricKeyImporterOperations.ImportJose(
            Repeated(48, 2),
            JoseAlgorithms.HmacSha384).Key;
        TokenKey wrongCose = SymmetricKeyImporterOperations.ImportCose(
            Repeated(48, 3),
            CoseAlgorithms.HmacSha384).Key;

        TokenBytesResult matching = CoseHmacAlgorithms.Create(
            Array.Empty<byte>(),
            matchingJose,
            CoseAlgorithms.HmacSha256);
        TokenBytesResult truncated = CoseHmacAlgorithms.Create(
            Array.Empty<byte>(),
            matchingJose,
            CoseAlgorithms.HmacSha256Truncated64);
        TokenBytesResult joseConflict = CoseHmacAlgorithms.Create(
            Array.Empty<byte>(),
            wrongJose,
            CoseAlgorithms.HmacSha256);
        TokenBytesResult coseConflict = CoseHmacAlgorithms.Create(
            Array.Empty<byte>(),
            wrongCose,
            CoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Success, Is.True);
            AssertBytesFailure(
                truncated,
                "conflicting_key_algorithm");
            AssertBytesFailure(
                joseConflict,
                "conflicting_key_algorithm");
            AssertBytesFailure(
                coseConflict,
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void MacOperationsAndUseMetadataAreEnforced()
    {
        byte[] data = new byte[] { 1, 3, 5, 7 };
        TokenKey unrestricted = SymmetricKeyImporterOperations.ImportCose(
            Repeated(32, 9),
            CoseAlgorithms.HmacSha256).Key;
        TokenBytesResult tag = CoseHmacAlgorithms.Create(
            data,
            unrestricted,
            CoseAlgorithms.HmacSha256);
        TokenKey verifyOnly = SymmetricKeyImporterOperations.ImportCose(
            Repeated(32, 9),
            CoseAlgorithms.HmacSha256).Key;
        verifyOnly.KeyOperations =
            new string[] { TokenKeyOperations.MacVerify };
        TokenKey createOnly = SymmetricKeyImporterOperations.ImportCose(
            Repeated(32, 9),
            CoseAlgorithms.HmacSha256).Key;
        createOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationMacCreate };
        TokenKey encryptionUse = SymmetricKeyImporterOperations.ImportCose(
            Repeated(32, 9),
            CoseAlgorithms.HmacSha256).Key;
        encryptionUse.Use = TokenKeyUses.Encryption;

        TokenResult allowedVerify = CoseHmacAlgorithms.Verify(
            data,
            tag.Bytes,
            verifyOnly,
            CoseAlgorithms.HmacSha256);
        TokenBytesResult deniedCreate = CoseHmacAlgorithms.Create(
            data,
            verifyOnly,
            CoseAlgorithms.HmacSha256);
        TokenResult deniedVerify = CoseHmacAlgorithms.Verify(
            data,
            tag.Bytes,
            createOnly,
            CoseAlgorithms.HmacSha256);
        TokenBytesResult deniedUse = CoseHmacAlgorithms.Create(
            data,
            encryptionUse,
            CoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(allowedVerify.Success, Is.True);
            AssertBytesFailure(
                deniedCreate,
                "key_operation_not_permitted");
            AssertFailure(
                deniedVerify,
                "key_operation_not_permitted");
            AssertBytesFailure(
                deniedUse,
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void WeakAndNonSymmetricKeysAreRejected()
    {
        TokenKey weak = new TokenKey();
        weak.KeyType = TokenKeyTypes.Symmetric;
        weak.CoseKeyType = CoseKeyLabels.KeyTypeSymmetric;
        weak.SymmetricKey = Repeated(31, 1);
        TokenKey wrongType = new TokenKey();
        wrongType.KeyType = TokenKeyTypes.Rsa;
        wrongType.SymmetricKey = Repeated(32, 2);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    weak,
                    CoseAlgorithms.HmacSha256),
                "weak_key");
            AssertBytesFailure(
                CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    wrongType,
                    CoseAlgorithms.HmacSha256),
                "invalid_key_type");
        });
    }

    [Test]
    public void UnsupportedAndNonHmacAlgorithmsReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(
            Repeated(64, 4)).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    key,
                    999999),
                "unknown_algorithm");
            AssertBytesFailure(
                CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    key,
                    CoseAlgorithms.Aes256Gcm),
                "invalid_algorithm");
            AssertBytesFailure(
                CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    key,
                    CoseAlgorithms.RsaSha1),
                "unsupported_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportCose(
            Repeated(32, 5),
            CoseAlgorithms.HmacSha256).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseHmacAlgorithms.Create(
                    null,
                    key,
                    CoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseHmacAlgorithms.Create(
                    Array.Empty<byte>(),
                    null,
                    CoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseHmacAlgorithms.Verify(
                    null,
                    new byte[32],
                    key,
                    CoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseHmacAlgorithms.Verify(
                    Array.Empty<byte>(),
                    null,
                    key,
                    CoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseHmacAlgorithms.Verify(
                    Array.Empty<byte>(),
                    new byte[32],
                    null,
                    CoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Repeated(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static void AssertBytesFailure(
        TokenBytesResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(-1));
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(-1));
    }
}
