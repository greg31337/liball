namespace odf.Tests;

public class OpenPgpKeyMaterialTests
{
    [TestCase(OdfEncryptionMethod.BlowfishCfb, 4)]
    [TestCase(OdfEncryptionMethod.BlowfishCfb, 56)]
    [TestCase(OdfEncryptionMethod.Aes128Cbc, 16)]
    [TestCase(OdfEncryptionMethod.Aes192Cbc, 24)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, 32)]
    public void AcquiresSupportedCallerKeyMaterialWithOperationScopedLifetime(
        OdfEncryptionMethod encryptionMethod,
        int keySize)
    {
        OdfEncryptedKey metadata = CreateValidMetadata();
        byte[] supplied = CreateSequence(keySize);
        byte[] expected = CreateSequence(keySize);
        TestOpenPgpKeyProvider provider = new TestOpenPgpKeyProvider(supplied);

        SecurityBuffer key = OpenPgpKeyMaterial.AcquireEntryKey(
            provider,
            metadata,
            encryptionMethod);
        byte[] operationBytes = key.GetOperationBytes();

        Assert.Multiple(() =>
        {
            Assert.That(provider.RequestedMetadata, Is.SameAs(metadata));
            Assert.That(operationBytes, Is.EqualTo(expected));
            Assert.That(AllBytesAreZero(supplied), Is.True);
        });

        key.Dispose();

        Assert.That(AllBytesAreZero(operationBytes), Is.True);
        SecurityBuffer.ClearBytes(expected);
    }

    [TestCase(OdfEncryptionMethod.BlowfishCfb, 3)]
    [TestCase(OdfEncryptionMethod.BlowfishCfb, 57)]
    [TestCase(OdfEncryptionMethod.Aes128Cbc, 24)]
    [TestCase(OdfEncryptionMethod.Aes192Cbc, 32)]
    [TestCase(OdfEncryptionMethod.Aes256Cbc, 16)]
    public void RejectsWrongKeySizesAndClearsTransferredBytes(
        OdfEncryptionMethod encryptionMethod,
        int keySize)
    {
        byte[] supplied = CreateSequence(keySize);
        TestOpenPgpKeyProvider provider = new TestOpenPgpKeyProvider(supplied);

        Assert.Throws<OdfSecurityException>(() => OpenPgpKeyMaterial.AcquireEntryKey(
            provider,
            CreateValidMetadata(),
            encryptionMethod));

        Assert.That(AllBytesAreZero(supplied), Is.True);
    }

    [Test]
    public void RejectsInvalidMetadataAndUnsupportedMethodsBeforeRequestingAKey()
    {
        TestOpenPgpKeyProvider invalidMetadataProvider = new TestOpenPgpKeyProvider([1, 2, 3, 4]);
        TestOpenPgpKeyProvider unsupportedMethodProvider = new TestOpenPgpKeyProvider([1, 2, 3, 4]);

        Assert.Throws<OdfSecurityException>(() => OpenPgpKeyMaterial.AcquireEntryKey(
            invalidMetadataProvider,
            new OdfEncryptedKey(),
            OdfEncryptionMethod.BlowfishCfb));
        Assert.Throws<OdfSecurityException>(() => OpenPgpKeyMaterial.AcquireEntryKey(
            unsupportedMethodProvider,
            CreateValidMetadata(),
            OdfEncryptionMethod.Unsupported));

        Assert.Multiple(() =>
        {
            Assert.That(invalidMetadataProvider.CallCount, Is.Zero);
            Assert.That(unsupportedMethodProvider.CallCount, Is.Zero);
        });
    }

    static OdfEncryptedKey CreateValidMetadata()
    {
        OdfEncryptedKey result = new OdfEncryptedKey();
        result.IsValid = true;
        return result;
    }

    static byte[] CreateSequence(int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(index + 1);
            index = index + 1;
        }
        return result;
    }

    static bool AllBytesAreZero(byte[] data)
    {
        foreach (byte value in data)
        {
            if (value != 0)
            {
                return false;
            }
        }
        return true;
    }

    class TestOpenPgpKeyProvider : IOdfOpenPgpKeyProvider
    {
        readonly byte[] keyBytes;

        internal int CallCount;
        internal OdfEncryptedKey? RequestedMetadata;

        internal TestOpenPgpKeyProvider(byte[] keyBytesValue)
        {
            keyBytes = keyBytesValue;
        }

        public byte[] GetDecryptedKey(OdfEncryptedKey encryptedKey)
        {
            CallCount = CallCount + 1;
            RequestedMetadata = encryptedKey;
            return keyBytes;
        }
    }
}
