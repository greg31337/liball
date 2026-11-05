using System.Text;
using argon2;

namespace argon2.Tests.Api;

public class Argon2PasswordVerificationTests
{
    [TestCase(Argon2Variant.D, Argon2Version.Version10)]
    [TestCase(Argon2Variant.D, Argon2Version.Version13)]
    [TestCase(Argon2Variant.I, Argon2Version.Version10)]
    [TestCase(Argon2Variant.I, Argon2Version.Version13)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13)]
    public void ByteAndTextVerificationAgreeForEveryVariantAndVersion(
        Argon2Variant variant,
        Argon2Version version)
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateVerificationOptions());
        string passwordText = "päss\0😀";
        byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordText);
        byte[] originalPasswordBytes = (byte[])passwordBytes.Clone();
        string encoded = CreateEncodedHash(
            passwordBytes,
            variant,
            version,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty);
        byte[] wrongPasswordBytes = (byte[])passwordBytes.Clone();
        wrongPasswordBytes[wrongPasswordBytes.Length - 1] = (byte)(
            wrongPasswordBytes[wrongPasswordBytes.Length - 1] ^ 1);

        Argon2VerificationResult matchingBytes = hasher.VerifyPasswordBytes(
            passwordBytes,
            encoded);
        Argon2VerificationResult matchingText = hasher.VerifyPasswordText(
            passwordText,
            encoded);
        Argon2VerificationResult wrongBytes = hasher.VerifyPasswordBytes(
            wrongPasswordBytes,
            encoded);
        Argon2VerificationResult wrongText = hasher.VerifyPasswordText(
            "päss\0😃",
            encoded);
        bool directNeedsRehash = hasher.NeedsRehash(encoded);
        bool expectedNeedsRehash = variant != Argon2Variant.Id
            || version != Argon2Version.Version13;

        Assert.Multiple(() =>
        {
            Assert.That(matchingBytes.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(matchingText.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(matchingBytes.NeedsRehash, Is.EqualTo(expectedNeedsRehash));
            Assert.That(matchingText.NeedsRehash, Is.EqualTo(expectedNeedsRehash));
            Assert.That(directNeedsRehash, Is.EqualTo(expectedNeedsRehash));
            Assert.That(wrongBytes.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(wrongText.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(wrongBytes.NeedsRehash, Is.False);
            Assert.That(wrongText.NeedsRehash, Is.False);
            Assert.That(passwordBytes, Is.EqualTo(originalPasswordBytes));
        });
    }

    [Test]
    public void ContextVerificationRequiresExactSecretAndAssociatedData()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateVerificationOptions());
        string passwordText = "context 😀";
        byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordText);
        byte[] secret = new byte[] { 0xA0, 0x00, 0xFF, 0xA3 };
        byte[] associatedData = new byte[] { 0xB0, 0x00, 0xFE, 0xB3 };
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();
        string encoded = CreateEncodedHash(
            passwordBytes,
            Argon2Variant.Id,
            Argon2Version.Version13,
            secret,
            associatedData);

        Argon2VerificationResult matchingBytes = hasher.VerifyPasswordBytesWithContext(
            passwordBytes,
            encoded,
            secret,
            associatedData);
        Argon2VerificationResult matchingText = hasher.VerifyPasswordTextWithContext(
            passwordText,
            encoded,
            secret,
            associatedData);
        Argon2VerificationResult missingSecret = hasher.VerifyPasswordBytesWithContext(
            passwordBytes,
            encoded,
            ReadOnlySpan<byte>.Empty,
            associatedData);
        Argon2VerificationResult wrongSecret = hasher.VerifyPasswordBytesWithContext(
            passwordBytes,
            encoded,
            new byte[] { 0xA0, 0x00, 0xFF, 0xA2 },
            associatedData);
        Argon2VerificationResult missingAssociatedData = hasher.VerifyPasswordBytesWithContext(
            passwordBytes,
            encoded,
            secret,
            ReadOnlySpan<byte>.Empty);
        Argon2VerificationResult wrongAssociatedData = hasher.VerifyPasswordTextWithContext(
            passwordText,
            encoded,
            secret,
            new byte[] { 0xB0, 0x00, 0xFE, 0xB2 });
        Argon2VerificationResult missingContext = hasher.VerifyPasswordBytes(
            passwordBytes,
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(matchingBytes.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(matchingText.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(matchingBytes.NeedsRehash, Is.False);
            Assert.That(matchingText.NeedsRehash, Is.False);
            Assert.That(missingSecret.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(wrongSecret.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(missingAssociatedData.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(wrongAssociatedData.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(missingContext.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
        });
    }

    [TestCase("")]
    [TestCase("invalid")]
    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=7,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB")]
    public void MalformedHashesReturnTheSameResultForByteAndTextVerification(string encoded)
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateVerificationOptions());

        Argon2VerificationResult byteResult = hasher.VerifyPasswordBytes(
            Encoding.UTF8.GetBytes("password"),
            encoded);
        Argon2VerificationResult textResult = hasher.VerifyPasswordText(
            "password",
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(textResult.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(byteResult.NeedsRehash, Is.False);
            Assert.That(textResult.NeedsRehash, Is.False);
        });
    }

    [Test]
    public void NullHashReturnsInvalidHashForByteAndTextVerification()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateVerificationOptions());

        Argon2VerificationResult byteResult = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            null!);
        Argon2VerificationResult textResult = hasher.VerifyPasswordText("", null!);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(textResult.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(byteResult.NeedsRehash, Is.False);
            Assert.That(textResult.NeedsRehash, Is.False);
        });
    }

    static Argon2PasswordHasherOptions CreateVerificationOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Variant.Id;
        options.Parameters.Version = Argon2Version.Version13;
        options.Parameters.MemorySizeKiB = 8L;
        options.Parameters.Iterations = 1L;
        options.Parameters.DegreeOfParallelism = 1;
        options.SaltLength = 8;
        options.TagLength = 16;
        options.ExecutionOptions.MaximumWorkerCount = 1;
        options.VerificationPolicy.MaximumMemorySizeKiB = 8L;
        options.VerificationPolicy.MaximumIterations = 1L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 1;
        options.VerificationPolicy.MaximumSaltLength = 8;
        options.VerificationPolicy.MaximumTagLength = 16;
        return options;
    }

    static string CreateEncodedHash(
        byte[] password,
        Argon2Variant variant,
        Argon2Version version,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.MemorySizeKiB = 8L;
        parameters.Iterations = 1L;
        parameters.DegreeOfParallelism = 1;
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 1;
        byte[] salt = new byte[] { 8, 9, 10, 11, 12, 13, 14, 15 };
        byte[] tag = new byte[16];
        IArgon2Primitive primitive = new Argon2Primitive();
        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            tag);
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = parameters;
        hash.Salt = salt;
        hash.Tag = tag;
        return new Argon2EncodedHashCodec().EncodeHash(hash);
    }
}
