namespace argon2;

internal static class Argon2PasswordHasherOptionsSnapshot
{
    internal static Argon2PasswordHasherOptions Create(Argon2PasswordHasherOptions source)
    {
        Argon2ParameterValidator.ValidatePasswordHasherOptions(source);

        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = source.Parameters.Variant;
        parameters.Version = source.Parameters.Version;
        parameters.Iterations = source.Parameters.Iterations;
        parameters.MemorySizeKiB = source.Parameters.MemorySizeKiB;
        parameters.DegreeOfParallelism = source.Parameters.DegreeOfParallelism;

        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = source.ExecutionOptions.MaximumWorkerCount;
        executionOptions.ClearMemory = source.ExecutionOptions.ClearMemory;

        Argon2VerificationPolicy verificationPolicy = new Argon2VerificationPolicy();
        verificationPolicy.MaximumEncodedHashLength = source.VerificationPolicy.MaximumEncodedHashLength;
        verificationPolicy.MaximumMemorySizeKiB = source.VerificationPolicy.MaximumMemorySizeKiB;
        verificationPolicy.MaximumIterations = source.VerificationPolicy.MaximumIterations;
        verificationPolicy.MaximumDegreeOfParallelism = source.VerificationPolicy.MaximumDegreeOfParallelism;
        verificationPolicy.MaximumSaltLength = source.VerificationPolicy.MaximumSaltLength;
        verificationPolicy.MaximumTagLength = source.VerificationPolicy.MaximumTagLength;
        verificationPolicy.AllowVariantD = source.VerificationPolicy.AllowVariantD;
        verificationPolicy.AllowVariantI = source.VerificationPolicy.AllowVariantI;
        verificationPolicy.AllowVariantId = source.VerificationPolicy.AllowVariantId;
        verificationPolicy.AllowVersion10 = source.VerificationPolicy.AllowVersion10;
        verificationPolicy.AllowVersion13 = source.VerificationPolicy.AllowVersion13;

        Argon2PasswordHasherOptions snapshot = new Argon2PasswordHasherOptions();
        snapshot.Parameters = parameters;
        snapshot.SaltLength = source.SaltLength;
        snapshot.TagLength = source.TagLength;
        snapshot.ExecutionOptions = executionOptions;
        snapshot.VerificationPolicy = verificationPolicy;
        return snapshot;
    }
}
