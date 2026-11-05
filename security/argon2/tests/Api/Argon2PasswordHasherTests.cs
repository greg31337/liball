using argon2;

namespace argon2.Tests.Api;

public class Argon2PasswordHasherTests
{
    [TestCase(Argon2Variant.D, Argon2Version.Version10)]
    [TestCase(Argon2Variant.D, Argon2Version.Version13)]
    [TestCase(Argon2Variant.I, Argon2Version.Version10)]
    [TestCase(Argon2Variant.I, Argon2Version.Version13)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13)]
    public void ByteVerificationIsVariantAndVersionAgile(
        Argon2Variant variant,
        Argon2Version version)
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(new Argon2PasswordHasherOptions());
        byte[] password = new byte[] { 0, 1, 2, 3, 4, 255 };
        byte[] originalPassword = (byte[])password.Clone();
        string encoded = CreateEncodedHash(
            password,
            variant,
            version,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            8L);

        Argon2VerificationResult match = hasher.VerifyPasswordBytes(password, encoded);
        Argon2VerificationResult mismatch = hasher.VerifyPasswordBytes(
            new byte[] { 0, 1, 2, 3, 4, 254 },
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(match.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(match.NeedsRehash, Is.True);
            Assert.That(mismatch.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(mismatch.NeedsRehash, Is.False);
            Assert.That(password, Is.EqualTo(originalPassword));
        });
    }

    [Test]
    public void OmittedLegacyVersionIsVerifiedAsVersion10()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(new Argon2PasswordHasherOptions());
        byte[] password = new byte[] { 4, 3, 2, 1 };
        string explicitVersion = CreateEncodedHash(
            password,
            Argon2Variant.D,
            Argon2Version.Version10,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            8L);
        string omittedVersion = explicitVersion.Replace("$v=16", "");

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(password, omittedVersion);

        Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.Match));
    }

    [Test]
    public void ContextualByteVerificationRequiresTheSameExternalInputs()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        byte[] password = new byte[] { 1, 2, 3, 4 };
        byte[] secret = new byte[] { 5, 6, 7 };
        byte[] associatedData = new byte[] { 8, 9, 10 };
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();
        string encoded = CreateEncodedHash(
            password,
            Argon2Variant.Id,
            Argon2Version.Version13,
            secret,
            associatedData,
            8L);

        Argon2VerificationResult matching = hasher.VerifyPasswordBytesWithContext(
            password,
            encoded,
            secret,
            associatedData);
        Argon2VerificationResult missingContext = hasher.VerifyPasswordBytes(password, encoded);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(missingContext.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
        });
    }

    [Test]
    public void TextVerificationUsesExactUnnormalizedUtf8WithContext()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        string password = "e\u0301\0😀";
        byte[] passwordBytes = new byte[] { 0x65, 0xCC, 0x81, 0x00, 0xF0, 0x9F, 0x98, 0x80 };
        byte[] secret = new byte[] { 1, 3, 5 };
        byte[] associatedData = new byte[] { 2, 4, 6 };
        string encoded = CreateEncodedHash(
            passwordBytes,
            Argon2Variant.I,
            Argon2Version.Version10,
            secret,
            associatedData,
            8L);

        Argon2VerificationResult matching = hasher.VerifyPasswordTextWithContext(
            password,
            encoded,
            secret,
            associatedData);
        Argon2VerificationResult normalizedMismatch = hasher.VerifyPasswordTextWithContext(
            "é\0😀",
            encoded,
            secret,
            associatedData);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(normalizedMismatch.Status, Is.EqualTo(Argon2VerificationStatus.Mismatch));
        });
    }

    [TestCase("invalid")]
    [TestCase("$argon2x$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=18$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdA")]
    [TestCase("$argon2id$v=19$m=8,t=1,p=1$c29tZXNhbHQ$dGVzdB")]
    public void MalformedAndUnsupportedHashesReturnInvalidHash(string encoded)
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.That(result.NeedsRehash, Is.False);
        });
    }

    [Test]
    public void ValidHashesOutsidePolicyReturnRejectedByPolicy()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        string encoded = CreateEncodedHash(
            new byte[] { 1, 2, 3 },
            Argon2Variant.D,
            Argon2Version.Version13,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            16L);

        Argon2VerificationResult result = hasher.VerifyPasswordBytes(
            new byte[] { 1, 2, 3 },
            encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2VerificationStatus.RejectedByPolicy));
            Assert.That(result.NeedsRehash, Is.False);
        });
    }

    [Test]
    public void NullHashIsInvalidAndNullTextRemainsAnInvalidCallerInput()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());

        Argon2VerificationResult nullHash = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            null!);

        Assert.Multiple(() =>
        {
            Assert.That(nullHash.Status, Is.EqualTo(Argon2VerificationStatus.InvalidHash));
            Assert.Throws<Argon2ParameterException>(() => hasher.VerifyPasswordText(null!, "invalid"));
            Assert.Throws<Argon2ParameterException>(
                () => hasher.VerifyPasswordTextWithContext(
                    null!,
                    "invalid",
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty));
        });
    }

    [Test]
    public void ByteHashingUsesFreshSaltAndCanonicalConfiguredMetadata()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2Primitive primitive = new Argon2Primitive();
        byte[] password = new byte[] { 0, 1, 2, 3, 4, 5, 255 };
        byte[] originalPassword = (byte[])password.Clone();

        string firstText = hasher.HashPasswordBytes(password);
        string secondText = hasher.HashPasswordBytes(password);
        Argon2EncodedHash first = codec.DecodeHash(firstText);
        Argon2EncodedHash second = codec.DecodeHash(secondText);
        Argon2ExecutionOptions executionOptions = CreateFastHashingOptions().ExecutionOptions;
        bool verifies = primitive.VerifyHash(
            password,
            first.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            first.Parameters,
            executionOptions,
            first.Tag);

        Assert.Multiple(() =>
        {
            Assert.That(firstText, Does.StartWith("$argon2i$v=16$m=8,t=1,p=1$"));
            Assert.That(codec.EncodeHash(first), Is.EqualTo(firstText));
            Assert.That(codec.GetEncodedHashLength(first), Is.EqualTo(firstText.Length));
            Assert.That(first.Parameters.Variant, Is.EqualTo(Argon2Variant.I));
            Assert.That(first.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(first.Parameters.MemorySizeKiB, Is.EqualTo(8L));
            Assert.That(first.Parameters.Iterations, Is.EqualTo(1L));
            Assert.That(first.Parameters.DegreeOfParallelism, Is.EqualTo(1));
            Assert.That(first.Salt.Length, Is.EqualTo(8));
            Assert.That(first.Tag.Length, Is.EqualTo(16));
            Assert.That(second.Salt, Is.Not.EqualTo(first.Salt));
            Assert.That(firstText, Is.Not.EqualTo(secondText));
            Assert.That(verifies, Is.True);
            Assert.That(password, Is.EqualTo(originalPassword));
        });
    }

    [Test]
    public void EmptyByteAndTextPasswordsAreHashedAndVerified()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());

        string byteHash = hasher.HashPasswordBytes(ReadOnlySpan<byte>.Empty);
        string textHash = hasher.HashPasswordText("");
        Argon2VerificationResult byteResult = hasher.VerifyPasswordBytes(
            ReadOnlySpan<byte>.Empty,
            byteHash);
        Argon2VerificationResult textResult = hasher.VerifyPasswordText("", textHash);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(textResult.Status, Is.EqualTo(Argon2VerificationStatus.Match));
            Assert.That(byteResult.NeedsRehash, Is.False);
            Assert.That(textResult.NeedsRehash, Is.False);
            Assert.That(byteHash, Does.StartWith("$argon2i$v=16$m=8,t=1,p=1$"));
            Assert.That(textHash, Does.StartWith("$argon2i$v=16$m=8,t=1,p=1$"));
            Assert.That(byteHash, Is.Not.EqualTo(textHash));
        });
    }

    [Test]
    public void CustomOptionsControlEveryGeneratedMetadataField()
    {
        Argon2PasswordHasherOptions options = CreateCustomOptions();
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2Primitive primitive = new Argon2Primitive();
        byte[] password = new byte[] { 0, 1, 2, 3, 255 };
        byte[] originalPassword = (byte[])password.Clone();

        string encoded = hasher.HashPasswordBytes(password);
        Argon2EncodedHash decoded = codec.DecodeHash(encoded);
        bool verifies = primitive.VerifyHash(
            password,
            decoded.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            decoded.Parameters,
            options.ExecutionOptions,
            decoded.Tag);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(Argon2Variant.D));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(64L));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(2L));
            Assert.That(decoded.Parameters.DegreeOfParallelism, Is.EqualTo(2));
            Assert.That(decoded.Salt.Length, Is.EqualTo(24));
            Assert.That(decoded.Tag.Length, Is.EqualTo(48));
            Assert.That(codec.EncodeHash(decoded), Is.EqualTo(encoded));
            Assert.That(codec.GetEncodedHashLength(decoded), Is.EqualTo(encoded.Length));
            Assert.That(verifies, Is.True);
            Assert.That(password, Is.EqualTo(originalPassword));
        });
    }

    [Test]
    public void ContextualByteHashingUsesExternalContextWithoutMutatingIt()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2Primitive primitive = new Argon2Primitive();
        byte[] password = new byte[] { 1, 2, 3, 4 };
        byte[] secret = new byte[] { 5, 6, 7 };
        byte[] associatedData = new byte[] { 8, 9, 10 };
        byte[] originalPassword = (byte[])password.Clone();
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();

        string encoded = hasher.HashPasswordBytesWithContext(password, secret, associatedData);
        Argon2EncodedHash decoded = codec.DecodeHash(encoded);
        Argon2ExecutionOptions executionOptions = CreateFastHashingOptions().ExecutionOptions;
        bool matchingContext = primitive.VerifyHash(
            password,
            decoded.Salt,
            secret,
            associatedData,
            decoded.Parameters,
            executionOptions,
            decoded.Tag);
        bool missingContext = primitive.VerifyHash(
            password,
            decoded.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            decoded.Parameters,
            executionOptions,
            decoded.Tag);

        Assert.Multiple(() =>
        {
            Assert.That(matchingContext, Is.True);
            Assert.That(missingContext, Is.False);
            Assert.That(password, Is.EqualTo(originalPassword));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
        });
    }

    [Test]
    public void TextHashingUsesExactUnnormalizedUtf8WithAndWithoutContext()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        IArgon2Primitive primitive = new Argon2Primitive();
        string password = "e\u0301\0😀";
        byte[] passwordBytes = new byte[] { 0x65, 0xCC, 0x81, 0x00, 0xF0, 0x9F, 0x98, 0x80 };
        byte[] normalizedBytes = new byte[] { 0xC3, 0xA9, 0x00, 0xF0, 0x9F, 0x98, 0x80 };
        byte[] secret = new byte[] { 1, 3, 5 };
        byte[] associatedData = new byte[] { 2, 4, 6 };
        Argon2ExecutionOptions executionOptions = CreateFastHashingOptions().ExecutionOptions;

        Argon2EncodedHash plain = codec.DecodeHash(hasher.HashPasswordText(password));
        Argon2EncodedHash contextual = codec.DecodeHash(
            hasher.HashPasswordTextWithContext(password, secret, associatedData));
        bool plainMatchesExactBytes = primitive.VerifyHash(
            passwordBytes,
            plain.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            plain.Parameters,
            executionOptions,
            plain.Tag);
        bool plainMatchesNormalizedBytes = primitive.VerifyHash(
            normalizedBytes,
            plain.Salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            plain.Parameters,
            executionOptions,
            plain.Tag);
        bool contextualMatches = primitive.VerifyHash(
            passwordBytes,
            contextual.Salt,
            secret,
            associatedData,
            contextual.Parameters,
            executionOptions,
            contextual.Tag);

        Assert.Multiple(() =>
        {
            Assert.That(plainMatchesExactBytes, Is.True);
            Assert.That(plainMatchesNormalizedBytes, Is.False);
            Assert.That(contextualMatches, Is.True);
            Assert.That(password, Is.EqualTo("e\u0301\0😀"));
        });
    }

    [Test]
    public void TextHashingRejectsNullPassword()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateFastHashingOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => hasher.HashPasswordText(null!));
            Assert.Throws<Argon2ParameterException>(
                () => hasher.HashPasswordTextWithContext(
                    null!,
                    ReadOnlySpan<byte>.Empty,
                    ReadOnlySpan<byte>.Empty));
        });
    }

    [Test]
    public void DefaultOptionsUseTheRfcLowMemoryProfile()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(new Argon2PasswordHasherOptions());

        Argon2PasswordHasherOptions snapshot = hasher.GetHashingOptions();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Parameters.Variant, Is.EqualTo(Argon2Variant.Id));
            Assert.That(snapshot.Parameters.Version, Is.EqualTo(Argon2Version.Version13));
            Assert.That(snapshot.Parameters.MemorySizeKiB, Is.EqualTo(65536L));
            Assert.That(snapshot.Parameters.Iterations, Is.EqualTo(3L));
            Assert.That(snapshot.Parameters.DegreeOfParallelism, Is.EqualTo(4));
            Assert.That(snapshot.SaltLength, Is.EqualTo(16));
            Assert.That(snapshot.TagLength, Is.EqualTo(32));
            Assert.That(snapshot.ExecutionOptions.MaximumWorkerCount, Is.EqualTo(4));
            Assert.That(snapshot.ExecutionOptions.ClearMemory, Is.True);
        });
    }

    [Test]
    public void DefaultHasherGeneratesTheSecondRecommendedProfile()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(
            new Argon2PasswordHasherOptions());
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        byte[] password = new byte[] { 0x00, 0x01, 0xFE, 0xFF };
        byte[] originalPassword = (byte[])password.Clone();

        string encoded = hasher.HashPasswordBytes(password);
        Argon2EncodedHash decoded = codec.DecodeHash(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Parameters.Variant, Is.EqualTo(Argon2Profiles.SecondRecommendedVariant));
            Assert.That(decoded.Parameters.Version, Is.EqualTo(Argon2Profiles.SecondRecommendedVersion));
            Assert.That(decoded.Parameters.MemorySizeKiB, Is.EqualTo(Argon2Profiles.SecondRecommendedMemorySizeKiB));
            Assert.That(decoded.Parameters.Iterations, Is.EqualTo(Argon2Profiles.SecondRecommendedIterations));
            Assert.That(
                decoded.Parameters.DegreeOfParallelism,
                Is.EqualTo(Argon2Profiles.SecondRecommendedDegreeOfParallelism));
            Assert.That(decoded.Salt.Length, Is.EqualTo(Argon2Profiles.SecondRecommendedSaltLength));
            Assert.That(decoded.Tag.Length, Is.EqualTo(Argon2Profiles.SecondRecommendedTagLength));
            Assert.That(codec.EncodeHash(decoded), Is.EqualTo(encoded));
            Assert.That(password, Is.EqualTo(originalPassword));
        });
    }

    [Test]
    public void FirstRecommendedProfileCanBeConfiguredWithoutExecutingItsMemoryCost()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Profiles.FirstRecommendedVariant;
        options.Parameters.Version = Argon2Profiles.FirstRecommendedVersion;
        options.Parameters.MemorySizeKiB = Argon2Profiles.FirstRecommendedMemorySizeKiB;
        options.Parameters.Iterations = Argon2Profiles.FirstRecommendedIterations;
        options.Parameters.DegreeOfParallelism = Argon2Profiles.FirstRecommendedDegreeOfParallelism;
        options.SaltLength = Argon2Profiles.FirstRecommendedSaltLength;
        options.TagLength = Argon2Profiles.FirstRecommendedTagLength;
        options.VerificationPolicy.MaximumMemorySizeKiB = Argon2Profiles.FirstRecommendedMemorySizeKiB;
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(options);

        Argon2PasswordHasherOptions snapshot = hasher.GetHashingOptions();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Parameters.Variant, Is.EqualTo(Argon2Profiles.FirstRecommendedVariant));
            Assert.That(snapshot.Parameters.Version, Is.EqualTo(Argon2Profiles.FirstRecommendedVersion));
            Assert.That(snapshot.Parameters.MemorySizeKiB, Is.EqualTo(Argon2Profiles.FirstRecommendedMemorySizeKiB));
            Assert.That(snapshot.Parameters.Iterations, Is.EqualTo(Argon2Profiles.FirstRecommendedIterations));
            Assert.That(
                snapshot.Parameters.DegreeOfParallelism,
                Is.EqualTo(Argon2Profiles.FirstRecommendedDegreeOfParallelism));
            Assert.That(snapshot.SaltLength, Is.EqualTo(Argon2Profiles.FirstRecommendedSaltLength));
            Assert.That(snapshot.TagLength, Is.EqualTo(Argon2Profiles.FirstRecommendedTagLength));
        });
    }

    [Test]
    public void ConstructorDeepCopiesEveryOptionModel()
    {
        Argon2PasswordHasherOptions source = CreateCustomOptions();
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(source);

        source.Parameters.Variant = Argon2Variant.Id;
        source.Parameters.Iterations = 99L;
        source.SaltLength = 63;
        source.TagLength = 63;
        source.ExecutionOptions.MaximumWorkerCount = 2;
        source.ExecutionOptions.ClearMemory = true;
        source.VerificationPolicy.MaximumEncodedHashLength = 100;
        source.VerificationPolicy.AllowVariantD = false;

        Argon2PasswordHasherOptions snapshot = hasher.GetHashingOptions();

        AssertCustomOptions(snapshot);
    }

    [Test]
    public void ReturnedSnapshotsAreIndependentFromRetainedStateAndEachOther()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateCustomOptions());
        Argon2PasswordHasherOptions first = hasher.GetHashingOptions();

        first.Parameters.MemorySizeKiB = 1024L;
        first.SaltLength = 32;
        first.ExecutionOptions.ClearMemory = true;
        first.VerificationPolicy.AllowVariantD = false;
        Argon2PasswordHasherOptions second = hasher.GetHashingOptions();

        Assert.Multiple(() =>
        {
            AssertCustomOptions(second);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Parameters, Is.Not.SameAs(second.Parameters));
            Assert.That(first.ExecutionOptions, Is.Not.SameAs(second.ExecutionOptions));
            Assert.That(first.VerificationPolicy, Is.Not.SameAs(second.VerificationPolicy));
        });
    }

    [Test]
    public void ConcurrentSnapshotCallersCannotMutateRetainedState()
    {
        IArgon2PasswordHasher hasher = new Argon2PasswordHasher(CreateCustomOptions());
        Argon2PasswordHasherOptions[] snapshots = new Argon2PasswordHasherOptions[32];

        Parallel.For(0, snapshots.Length, index =>
        {
            Argon2PasswordHasherOptions snapshot = hasher.GetHashingOptions();
            snapshot.Parameters.Iterations = index + 10L;
            snapshot.ExecutionOptions.MaximumWorkerCount = 2;
            snapshot.VerificationPolicy.AllowVariantD = false;
            snapshots[index] = snapshot;
        });

        Argon2PasswordHasherOptions retained = hasher.GetHashingOptions();
        Assert.Multiple(() =>
        {
            AssertCustomOptions(retained);
            Assert.That(snapshots.Select(snapshot => snapshot.Parameters).Distinct().Count(), Is.EqualTo(32));
            Assert.That(snapshots.Select(snapshot => snapshot.ExecutionOptions).Distinct().Count(), Is.EqualTo(32));
            Assert.That(snapshots.Select(snapshot => snapshot.VerificationPolicy).Distinct().Count(), Is.EqualTo(32));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidOrIncoherentOptions()
    {
        Argon2PasswordHasherOptions shortSalt = CreateCustomOptions();
        shortSalt.SaltLength = 7;
        Argon2PasswordHasherOptions tooManyWorkers = CreateCustomOptions();
        tooManyWorkers.ExecutionOptions.MaximumWorkerCount = 3;
        Argon2PasswordHasherOptions rejectingPolicy = CreateCustomOptions();
        rejectingPolicy.VerificationPolicy.MaximumIterations = 1L;

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(null!));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(shortSalt));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(tooManyWorkers));
            Assert.Throws<Argon2ParameterException>(() => new Argon2PasswordHasher(rejectingPolicy));
        });
    }

    static Argon2PasswordHasherOptions CreateCustomOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Variant.D;
        options.Parameters.Version = Argon2Version.Version10;
        options.Parameters.MemorySizeKiB = 64L;
        options.Parameters.Iterations = 2L;
        options.Parameters.DegreeOfParallelism = 2;
        options.SaltLength = 24;
        options.TagLength = 48;
        options.ExecutionOptions.MaximumWorkerCount = 1;
        options.ExecutionOptions.ClearMemory = false;
        options.VerificationPolicy.MaximumEncodedHashLength = 2048;
        options.VerificationPolicy.MaximumMemorySizeKiB = 128L;
        options.VerificationPolicy.MaximumIterations = 4L;
        options.VerificationPolicy.MaximumDegreeOfParallelism = 4;
        options.VerificationPolicy.MaximumSaltLength = 48;
        options.VerificationPolicy.MaximumTagLength = 96;
        options.VerificationPolicy.AllowVariantD = true;
        options.VerificationPolicy.AllowVariantI = false;
        options.VerificationPolicy.AllowVariantId = false;
        options.VerificationPolicy.AllowVersion10 = true;
        options.VerificationPolicy.AllowVersion13 = false;
        return options;
    }

    static Argon2PasswordHasherOptions CreateFastHashingOptions()
    {
        Argon2PasswordHasherOptions options = new Argon2PasswordHasherOptions();
        options.Parameters.Variant = Argon2Variant.I;
        options.Parameters.Version = Argon2Version.Version10;
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
        ReadOnlySpan<byte> password,
        Argon2Variant variant,
        Argon2Version version,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        long memorySizeKiB)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.MemorySizeKiB = memorySizeKiB;
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

    static void AssertCustomOptions(Argon2PasswordHasherOptions options)
    {
        Assert.Multiple(() =>
        {
            Assert.That(options.Parameters.Variant, Is.EqualTo(Argon2Variant.D));
            Assert.That(options.Parameters.Version, Is.EqualTo(Argon2Version.Version10));
            Assert.That(options.Parameters.MemorySizeKiB, Is.EqualTo(64L));
            Assert.That(options.Parameters.Iterations, Is.EqualTo(2L));
            Assert.That(options.Parameters.DegreeOfParallelism, Is.EqualTo(2));
            Assert.That(options.SaltLength, Is.EqualTo(24));
            Assert.That(options.TagLength, Is.EqualTo(48));
            Assert.That(options.ExecutionOptions.MaximumWorkerCount, Is.EqualTo(1));
            Assert.That(options.ExecutionOptions.ClearMemory, Is.False);
            Assert.That(options.VerificationPolicy.MaximumEncodedHashLength, Is.EqualTo(2048));
            Assert.That(options.VerificationPolicy.MaximumMemorySizeKiB, Is.EqualTo(128L));
            Assert.That(options.VerificationPolicy.MaximumIterations, Is.EqualTo(4L));
            Assert.That(options.VerificationPolicy.MaximumDegreeOfParallelism, Is.EqualTo(4));
            Assert.That(options.VerificationPolicy.MaximumSaltLength, Is.EqualTo(48));
            Assert.That(options.VerificationPolicy.MaximumTagLength, Is.EqualTo(96));
            Assert.That(options.VerificationPolicy.AllowVariantD, Is.True);
            Assert.That(options.VerificationPolicy.AllowVariantI, Is.False);
            Assert.That(options.VerificationPolicy.AllowVariantId, Is.False);
            Assert.That(options.VerificationPolicy.AllowVersion10, Is.True);
            Assert.That(options.VerificationPolicy.AllowVersion13, Is.False);
        });
    }
}
