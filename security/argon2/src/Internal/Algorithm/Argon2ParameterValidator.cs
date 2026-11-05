using System;

namespace argon2;

internal static class Argon2ParameterValidator
{
    internal static void ValidateParameters(Argon2Parameters parameters)
    {
        if (parameters == null)
        {
            throw new Argon2ParameterException("Argon2 parameters are required.");
        }

        if (parameters.Variant != Argon2Variant.D
            && parameters.Variant != Argon2Variant.I
            && parameters.Variant != Argon2Variant.Id)
        {
            throw new Argon2ParameterException("The Argon2 variant is not supported.");
        }

        if (parameters.Version != Argon2Version.Version10
            && parameters.Version != Argon2Version.Version13)
        {
            throw new Argon2ParameterException("The Argon2 version is not supported.");
        }

        if (parameters.Iterations < Argon2Limits.MinimumIterations
            || parameters.Iterations > Argon2Limits.MaximumIterations)
        {
            throw new Argon2ParameterException("Argon2 iterations are outside the RFC range.");
        }

        if (parameters.DegreeOfParallelism < Argon2Limits.MinimumDegreeOfParallelism
            || parameters.DegreeOfParallelism > Argon2Limits.MaximumDegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 degree of parallelism is outside the RFC range.");
        }

        long minimumMemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB
            * parameters.DegreeOfParallelism;
        if (parameters.MemorySizeKiB < minimumMemorySizeKiB
            || parameters.MemorySizeKiB > Argon2Limits.MaximumMemorySizeKiB)
        {
            throw new Argon2ParameterException("Argon2 memory size is outside the RFC range for the lane count.");
        }
    }

    internal static void ValidateExecutionOptions(
        Argon2ExecutionOptions executionOptions,
        Argon2Parameters parameters)
    {
        ValidateParameters(parameters);

        if (executionOptions == null)
        {
            throw new Argon2ParameterException("Argon2 execution options are required.");
        }

        if (executionOptions.MaximumWorkerCount < Argon2Limits.MinimumWorkerCount
            || executionOptions.MaximumWorkerCount > Argon2Limits.MaximumWorkerCount)
        {
            throw new Argon2ParameterException("Argon2 maximum worker count is outside the supported range.");
        }

        if (executionOptions.MaximumWorkerCount > parameters.DegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 maximum worker count must not exceed the lane count.");
        }
    }

    internal static void ValidateInputLengths(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        int outputLength)
    {
        if (salt.Length < Argon2Limits.MinimumSaltLength)
        {
            throw new Argon2ParameterException("Argon2 salt length is outside the interoperable range.");
        }

        if (outputLength < Argon2Limits.MinimumTagLength)
        {
            throw new Argon2ParameterException("Argon2 output length is outside the supported range.");
        }
    }

    internal static void ValidateEncodedHash(Argon2EncodedHash hash)
    {
        if (hash == null)
        {
            throw new Argon2ParameterException("Decoded Argon2 hash components are required.");
        }

        if (hash.Parameters == null)
        {
            throw new Argon2ParameterException("Decoded Argon2 parameters are required.");
        }

        if (hash.Salt == null)
        {
            throw new Argon2ParameterException("Decoded Argon2 salt bytes are required.");
        }

        if (hash.Tag == null)
        {
            throw new Argon2ParameterException("Decoded Argon2 tag bytes are required.");
        }

        ValidateParameters(hash.Parameters);
        ValidateInputLengths(
            ReadOnlySpan<byte>.Empty,
            hash.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            hash.Tag.Length);
    }

    internal static void ValidateVerificationPolicy(Argon2VerificationPolicy policy)
    {
        if (policy == null)
        {
            throw new Argon2ParameterException("Argon2 verification policy is required.");
        }

        if (policy.MaximumEncodedHashLength < 1 || policy.MaximumEncodedHashLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("Maximum encoded-hash length is outside the managed range.");
        }

        if (policy.MaximumMemorySizeKiB < Argon2Limits.MinimumMemorySizePerLaneKiB
            || policy.MaximumMemorySizeKiB > Argon2Limits.MaximumMemorySizeKiB)
        {
            throw new Argon2ParameterException("Maximum verification memory is outside the RFC range.");
        }

        if (policy.MaximumIterations < Argon2Limits.MinimumIterations
            || policy.MaximumIterations > Argon2Limits.MaximumIterations)
        {
            throw new Argon2ParameterException("Maximum verification iterations are outside the RFC range.");
        }

        if (policy.MaximumDegreeOfParallelism < Argon2Limits.MinimumDegreeOfParallelism
            || policy.MaximumDegreeOfParallelism > Argon2Limits.MaximumDegreeOfParallelism)
        {
            throw new Argon2ParameterException("Maximum verification parallelism is outside the RFC range.");
        }

        if (policy.MaximumSaltLength < Argon2Limits.MinimumSaltLength
            || policy.MaximumSaltLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("Maximum verification salt length is outside the supported range.");
        }

        if (policy.MaximumTagLength < Argon2Limits.MinimumTagLength
            || policy.MaximumTagLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("Maximum verification tag length is outside the supported range.");
        }

        if (!policy.AllowVariantD && !policy.AllowVariantI && !policy.AllowVariantId)
        {
            throw new Argon2ParameterException("Verification policy must allow at least one Argon2 variant.");
        }

        if (!policy.AllowVersion10 && !policy.AllowVersion13)
        {
            throw new Argon2ParameterException("Verification policy must allow at least one Argon2 version.");
        }
    }

    internal static void ValidatePasswordHasherOptions(Argon2PasswordHasherOptions options)
    {
        if (options == null)
        {
            throw new Argon2ParameterException("Argon2 password hasher options are required.");
        }

        if (options.Parameters == null)
        {
            throw new Argon2ParameterException("Password hashing parameters are required.");
        }

        if (options.ExecutionOptions == null)
        {
            throw new Argon2ParameterException("Password hashing execution options are required.");
        }

        if (options.VerificationPolicy == null)
        {
            throw new Argon2ParameterException("Password hashing verification policy is required.");
        }

        ValidateParameters(options.Parameters);
        ValidateExecutionOptions(options.ExecutionOptions, options.Parameters);
        ValidateVerificationPolicy(options.VerificationPolicy);

        if (options.SaltLength < Argon2Limits.MinimumSaltLength
            || options.SaltLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("Password hashing salt length is outside the supported range.");
        }

        if (options.TagLength < Argon2Limits.MinimumTagLength
            || options.TagLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("Password hashing tag length is outside the supported range.");
        }

        int generatedEncodedHashLength = Argon2PhcFormatter.GetEncodedLengthForComponents(
            options.Parameters,
            options.SaltLength,
            options.TagLength);
        if (options.VerificationPolicy.MaximumEncodedHashLength < generatedEncodedHashLength)
        {
            throw new Argon2ParameterException(
                "Verification policy must accept the generated encoded-hash length.");
        }

        Argon2VerificationPolicy policy = options.VerificationPolicy;
        Argon2Parameters parameters = options.Parameters;
        if (policy.MaximumMemorySizeKiB < parameters.MemorySizeKiB
            || policy.MaximumIterations < parameters.Iterations
            || policy.MaximumDegreeOfParallelism < parameters.DegreeOfParallelism
            || policy.MaximumSaltLength < options.SaltLength
            || policy.MaximumTagLength < options.TagLength)
        {
            throw new Argon2ParameterException("Verification policy must accept hashes generated by these options.");
        }

        if (!IsVariantAllowed(parameters.Variant, policy)
            || !IsVersionAllowed(parameters.Version, policy))
        {
            throw new Argon2ParameterException("Verification policy must allow the generated variant and version.");
        }
    }

    internal static long GetNormalizedMemoryBlockCount(Argon2Parameters parameters)
    {
        ValidateParameters(parameters);
        long alignment = (long)parameters.DegreeOfParallelism * Argon2Limits.SynchronizationPoints;
        return (parameters.MemorySizeKiB / alignment) * alignment;
    }

    internal static long GetNormalizedMemoryByteCount(Argon2Parameters parameters)
    {
        long blockCount = GetNormalizedMemoryBlockCount(parameters);
        if (blockCount > long.MaxValue / Argon2Limits.BlockSizeInBytes)
        {
            throw new Argon2ParameterException("Normalized Argon2 memory exceeds the managed byte-count range.");
        }

        return blockCount * Argon2Limits.BlockSizeInBytes;
    }

    internal static int GetManagedArrayLength(long length)
    {
        if (length < 0L || length > Array.MaxLength)
        {
            throw new Argon2ParameterException("The requested array length exceeds the managed range.");
        }

        return (int)length;
    }

    internal static bool IsVariantAllowed(Argon2Variant variant, Argon2VerificationPolicy policy)
    {
        if (policy == null)
        {
            throw new Argon2ParameterException("Argon2 verification policy is required.");
        }

        if (variant == Argon2Variant.D)
        {
            return policy.AllowVariantD;
        }

        if (variant == Argon2Variant.I)
        {
            return policy.AllowVariantI;
        }

        if (variant == Argon2Variant.Id)
        {
            return policy.AllowVariantId;
        }

        return false;
    }

    internal static bool IsVersionAllowed(Argon2Version version, Argon2VerificationPolicy policy)
    {
        if (policy == null)
        {
            throw new Argon2ParameterException("Argon2 verification policy is required.");
        }

        if (version == Argon2Version.Version10)
        {
            return policy.AllowVersion10;
        }

        if (version == Argon2Version.Version13)
        {
            return policy.AllowVersion13;
        }

        return false;
    }
}
