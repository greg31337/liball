using System.Text;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2Version10VectorTests
{
    // Generated with P-H-C/phc-winner-argon2 commit
    // f57e61e19229e23c4445b85494dbf7c07de721cb using its version-1.0 CLI mode.
    [TestCase(
        Argon2Variant.D,
        "c03e6ea388e8b206ee787353eebf6225f4a487546952f0fca2a76efb0ab3d8ae",
        "$argon2d$v=16$m=32,t=3,p=4$c29tZXNhbHQ$wD5uo4josgbueHNT7r9iJfSkh1RpUvD8oqdu+wqz2K4")]
    [TestCase(
        Argon2Variant.I,
        "a5742f9e3bf59936648fffc596029c224361c5efe42ccc704d4b2f7bf4ac0403",
        "$argon2i$v=16$m=32,t=3,p=4$c29tZXNhbHQ$pXQvnjv1mTZkj//FlgKcIkNhxe/kLMxwTUsve/SsBAM")]
    [TestCase(
        Argon2Variant.Id,
        "4ed472a832960d0fd677459f7926f9b4bf72df7ec7e55436d578dd5f24e85fd1",
        "$argon2id$v=16$m=32,t=3,p=4$c29tZXNhbHQ$TtRyqDKWDQ/Wd0WfeSb5tL9y337H5VQ21XjdXyToX9E")]
    public void PinnedLibargon2VectorAndOmittedVersionInteroperateAsVersion10(
        Argon2Variant variant,
        string expectedRawTag,
        string expectedEncodedHash)
    {
        byte[] password = Encoding.ASCII.GetBytes("password");
        byte[] salt = Encoding.ASCII.GetBytes("somesalt");
        Argon2Parameters parameters = CreateParameters(variant);
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 4;
        byte[] tag = new byte[32];
        IArgon2Primitive primitive = new Argon2Primitive();

        primitive.ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            tag);

        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = parameters;
        hash.Salt = salt;
        hash.Tag = tag;
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        string encodedHash = codec.EncodeHash(hash);
        string omittedVersionHash = expectedEncodedHash.Replace("$v=16", "");
        Argon2EncodedHash decoded = codec.DecodeHash(omittedVersionHash);
        string canonicalHash = codec.EncodeHash(decoded);

        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(
            new Argon2PasswordHasherOptions());
        Argon2VerificationResult verification = hasher.VerifyPasswordBytes(
            password,
            omittedVersionHash);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(tag).ToLowerInvariant(), Is.EqualTo(expectedRawTag));
            Assert.That(encodedHash, Is.EqualTo(expectedEncodedHash));
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(variant));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(32L));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(3L));
            Assert.That(decoded.Parameters.DegreeOfParallelism, Is.EqualTo(4));
            Assert.That(decoded.Salt, Is.EqualTo(salt));
            Assert.That(decoded.Tag, Is.EqualTo(tag));
            Assert.That(canonicalHash, Is.EqualTo(expectedEncodedHash));
            Assert.That(verification.Status, Is.EqualTo(Argon2VerificationStatus.Match));
        });
    }

    static Argon2Parameters CreateParameters(Argon2Variant variant)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = Argon2Version.Version10;
        parameters.MemorySizeKiB = 32L;
        parameters.Iterations = 3L;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }
}
