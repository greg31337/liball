using token;

namespace token.Tests;

public class HmacAlgorithmsTests
{
    [TestCase(
        JoseAlgorithms.HmacSha256,
        "60e431591ee0b67f0d8a26aacbf5b77f8e0bc6213728c5140546040f0ee37f54")]
    [TestCase(
        JoseAlgorithms.HmacSha384,
        "4ece084485813e9088d2c63a041bc5b44f9ef1012a2b588f3cd11f05033ac4c60c2ef6ab4030fe8296248df163f44952")]
    [TestCase(
        JoseAlgorithms.HmacSha512,
        "80b24263c7c1a3ebb71493c1dd7be8b49b46d1f41b4aeec1121b013783f8f3526b56d037e05f2598bd0fd2215d6a1e5295e64f73f63f0aec8b915a985d786598")]
    public void SignAndVerifyMatchRfc4231CaseSix(string algorithm, string expectedHex)
    {
        byte[] secret = Bytes(131, 0xaa);
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "Test Using Larger Than Block-Size Key - Hash Key First");
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(secret, algorithm).Key;

        TokenBytesResult signature = HmacAlgorithms.Sign(data, key, algorithm);
        TokenResult verification = HmacAlgorithms.Verify(
            data,
            Convert.FromHexString(expectedHex),
            key,
            algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Error, Is.Null);
            Assert.That(signature.Bytes, Is.EqualTo(Convert.FromHexString(expectedHex)));
            Assert.That(verification.Success, Is.True);
            Assert.That(verification.Error, Is.Null);
            Assert.That(key.SymmetricKey, Is.EqualTo(secret));
        });
    }

    [Test]
    public void SpanOverloadsAuthenticateOnlyTheSelectedSlices()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 0x31),
            JoseAlgorithms.HmacSha256).Key;
        byte[] data = new byte[] { 99, 1, 2, 3, 88 };

        TokenBytesResult sliced = HmacAlgorithms.Sign(
            data.AsSpan(1, 3),
            key,
            JoseAlgorithms.HmacSha256);
        TokenBytesResult exact = HmacAlgorithms.Sign(
            new byte[] { 1, 2, 3 },
            key,
            JoseAlgorithms.HmacSha256);
        byte[] wrappedSignature = new byte[sliced.Bytes.Length + 2];
        sliced.Bytes.CopyTo(wrappedSignature, 1);
        TokenResult verified = HmacAlgorithms.Verify(
            data.AsSpan(1, 3),
            wrappedSignature.AsSpan(1, sliced.Bytes.Length),
            key,
            JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(sliced.Success, Is.True);
            Assert.That(sliced.Bytes, Is.EqualTo(exact.Bytes));
            Assert.That(verified.Success, Is.True);
        });
    }

    [TestCase(JoseAlgorithms.HmacSha256, 32)]
    [TestCase(JoseAlgorithms.HmacSha384, 48)]
    [TestCase(JoseAlgorithms.HmacSha512, 64)]
    public void VerifyRejectsAlteredDataTagsAndTagLengths(string algorithm, int tagLength)
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(Bytes(tagLength, 7), algorithm).Key;
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenBytesResult signed = HmacAlgorithms.Sign(data, key, algorithm);
        byte[] alteredData = (byte[])data.Clone();
        alteredData[0] ^= 1;
        byte[] alteredTag = (byte[])signed.Bytes.Clone();
        alteredTag[alteredTag.Length - 1] ^= 1;
        byte[] shortTag = signed.Bytes[..^1];

        TokenResult wrongData = HmacAlgorithms.Verify(alteredData, signed.Bytes, key, algorithm);
        TokenResult wrongTag = HmacAlgorithms.Verify(data, alteredTag, key, algorithm);
        TokenResult wrongLength = HmacAlgorithms.Verify(data, shortTag, key, algorithm);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "signature_mismatch");
            AssertFailure(wrongTag, "signature_mismatch");
            AssertFailure(wrongLength, "signature_mismatch");
            Assert.That(signed.Bytes, Has.Length.EqualTo(tagLength));
        });
    }

    [Test]
    public void OperationsAndUseMetadataAreEnforcedForEachAction()
    {
        TokenKey unrestricted = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 9),
            JoseAlgorithms.HmacSha256).Key;
        byte[] data = new byte[] { 1, 3, 5, 7 };
        TokenBytesResult signature = HmacAlgorithms.Sign(
            data,
            unrestricted,
            JoseAlgorithms.HmacSha256);

        TokenKey verifyOnly = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 9),
            JoseAlgorithms.HmacSha256).Key;
        verifyOnly.KeyOperations = new string[] { TokenKeyOperations.Verify };
        TokenResult verification = HmacAlgorithms.Verify(
            data,
            signature.Bytes,
            verifyOnly,
            JoseAlgorithms.HmacSha256);
        TokenBytesResult disallowedSign = HmacAlgorithms.Sign(
            data,
            verifyOnly,
            JoseAlgorithms.HmacSha256);

        TokenKey encryptionUse = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 9),
            JoseAlgorithms.HmacSha256).Key;
        encryptionUse.Use = TokenKeyUses.Encryption;
        TokenResult disallowedVerify = HmacAlgorithms.Verify(
            data,
            signature.Bytes,
            encryptionUse,
            JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(verification.Success, Is.True);
            AssertBytesFailure(disallowedSign, "key_operation_not_permitted");
            AssertFailure(disallowedVerify, "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void SigningRejectsWeakMismatchedAndNonSymmetricKeys()
    {
        TokenKey weak = new TokenKey();
        weak.KeyType = TokenKeyTypes.Symmetric;
        weak.SymmetricKey = Bytes(31, 1);
        TokenKey mismatched = SymmetricKeyImporterOperations.ImportJose(
            Bytes(48, 2),
            JoseAlgorithms.HmacSha384).Key;
        TokenKey rsa = new TokenKey();
        rsa.KeyType = TokenKeyTypes.Rsa;
        rsa.SymmetricKey = Bytes(32, 3);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], weak, JoseAlgorithms.HmacSha256),
                "weak_key");
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], mismatched, JoseAlgorithms.HmacSha256),
                "conflicting_key_algorithm");
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], rsa, JoseAlgorithms.HmacSha256),
                "invalid_key_type");
        });
    }

    [Test]
    public void UnsupportedAlgorithmNamesReturnStableFailures()
    {
        TokenKey key = SymmetricKeyImporterOperations.Import(Bytes(64, 4)).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], key, "hs256"),
                "unknown_algorithm");
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], key, JoseAlgorithms.HmacSha1),
                "prohibited_algorithm");
            AssertBytesFailure(
                HmacAlgorithms.Sign(new byte[0], key, JoseAlgorithms.RsaSha256),
                "invalid_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = SymmetricKeyImporterOperations.ImportJose(
            Bytes(32, 5),
            JoseAlgorithms.HmacSha256).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => HmacAlgorithms.Sign(null, key, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Sign(new byte[0], null, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Sign(new byte[0], key, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Verify(null, new byte[32], key, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Verify(new byte[0], null, key, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Verify(new byte[0], new byte[32], null, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => HmacAlgorithms.Verify(new byte[0], new byte[32], key, null),
                Throws.ArgumentNullException);
        });
    }

    static byte[] Bytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
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

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
