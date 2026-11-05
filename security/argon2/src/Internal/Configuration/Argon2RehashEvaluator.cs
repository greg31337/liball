namespace argon2;

internal static class Argon2RehashEvaluator
{
    internal static bool NeedsRehash(
        Argon2EncodedHash hash,
        Argon2PasswordHasherOptions options)
    {
        Argon2Parameters actual = hash.Parameters;
        Argon2Parameters expected = options.Parameters;
        return actual.Variant != expected.Variant
            || actual.Version != expected.Version
            || actual.MemorySizeKiB != expected.MemorySizeKiB
            || actual.Iterations != expected.Iterations
            || actual.DegreeOfParallelism != expected.DegreeOfParallelism
            || hash.Salt.Length != options.SaltLength
            || hash.Tag.Length != options.TagLength;
    }
}
