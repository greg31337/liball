namespace argon2;

internal static class Argon2VerificationPolicyEvaluator
{
    internal static bool IsEncodedLengthAllowed(
        int encodedLength,
        Argon2VerificationPolicy policy)
    {
        return encodedLength <= policy.MaximumEncodedHashLength;
    }

    internal static bool IsDecodedHashAllowed(
        Argon2EncodedHash hash,
        Argon2VerificationPolicy policy)
    {
        Argon2Parameters parameters = hash.Parameters;
        if (parameters.MemorySizeKiB > policy.MaximumMemorySizeKiB
            || parameters.Iterations > policy.MaximumIterations
            || parameters.DegreeOfParallelism > policy.MaximumDegreeOfParallelism
            || hash.Salt.Length > policy.MaximumSaltLength
            || hash.Tag.Length > policy.MaximumTagLength)
        {
            return false;
        }

        if (!Argon2ParameterValidator.IsVariantAllowed(parameters.Variant, policy)
            || !Argon2ParameterValidator.IsVersionAllowed(parameters.Version, policy))
        {
            return false;
        }

        return true;
    }
}
