using System;

namespace uuid;

/// <summary>Generates, validates, and estimates the collision characteristics of Nano IDs.</summary>
/// <remarks>Generation uses cryptographically secure random bytes and rejection sampling, so every character has equal probability. Nano IDs are identifiers, not authentication secrets.</remarks>
public class NanoId : INanoId
{
    /// <summary>Generates a Nano ID with the default URL-safe alphabet and 21-character length.</summary>
    /// <returns>A new random Nano ID.</returns>
    public string New()
    {
        return NewWithSettings(NanoIdSettings.Default());
    }

    /// <summary>Generates a Nano ID of a requested length with the default alphabet.</summary>
    /// <param name="size">The non-negative number of characters.</param>
    /// <returns>A new random Nano ID.</returns>
    /// <exception cref="IdentifierException">The size is negative.</exception>
    public string NewWithSize(int size)
    {
        return NewWithSettings(new NanoIdSettings(NanoIdSettings.DefaultAlphabet, size));
    }

    /// <summary>Generates a Nano ID from a custom alphabet and length.</summary>
    /// <param name="alphabet">Between 2 and 256 distinct characters.</param>
    /// <param name="size">The non-negative number of characters.</param>
    /// <returns>A new uniformly distributed random Nano ID.</returns>
    /// <exception cref="IdentifierException">The alphabet is invalid or the size is negative.</exception>
    public string NewWithAlphabet(string? alphabet, int size)
    {
        return NewWithSettings(new NanoIdSettings(alphabet, size));
    }

    /// <summary>Generates a Nano ID from a settings snapshot.</summary>
    /// <param name="settings">The alphabet and exact output size.</param>
    /// <returns>A new uniformly distributed random Nano ID.</returns>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The settings contain an invalid alphabet or negative size.</exception>
    public string NewWithSettings(NanoIdSettings? settings)
    {
        return NewWithSettings(settings, RandomTools.SecureSource);
    }

    internal static string NewWithSettings(NanoIdSettings? settings, IRandomSource? source)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        NanoIdSettings copy = NanoIdSettings.Copy(settings);
        copy.ThrowIfInvalid();

        char[] characters = new char[copy.Size];
        int index = 0;
        while (index < characters.Length)
        {
            int alphabetIndex = RandomTools.RandomIndex(copy.Alphabet.Length, source);
            characters[index] = copy.Alphabet[alphabetIndex];
            index = index + 1;
        }

        return new string(characters);
    }

    /// <summary>Checks that an alphabet contains 2 through 256 distinct characters.</summary>
    /// <param name="alphabet">The alphabet to inspect.</param>
    /// <returns>A validation result with a specific failure reason when unusable.</returns>
    public IdentifierValidationResult ValidateAlphabet(string? alphabet)
    {
        return NanoIdSettings.ValidateAlphabet(alphabet);
    }

    /// <summary>Checks text against the default alphabet and length.</summary>
    /// <param name="text">The candidate Nano ID.</param>
    /// <returns>A validation result whose normalized text is unchanged on success.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        return ValidateWithSettings(text, NanoIdSettings.Default());
    }

    /// <summary>Checks text against a custom settings snapshot.</summary>
    /// <param name="text">The candidate Nano ID.</param>
    /// <param name="settings">The required alphabet and exact length.</param>
    /// <returns>A validation result. Invalid settings and invalid text are both reported without throwing.</returns>
    public IdentifierValidationResult ValidateWithSettings(string? text, NanoIdSettings? settings)
    {
        if (settings == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "Nano ID settings is null");
        }

        IdentifierValidationResult settingsValidation = settings.Validate();
        if (!settingsValidation.Ok)
        {
            return settingsValidation;
        }

        NanoIdSettings copy = NanoIdSettings.Copy(settings);
        if (text == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "Nano ID text is null");
        }

        if (text.Length != copy.Size)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected " + copy.Size.ToString() + " Nano ID characters but found " + text.Length.ToString());
        }

        int index = 0;
        while (index < text.Length)
        {
            if (copy.Alphabet.IndexOf(text[index]) < 0)
            {
                return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid Nano ID character at index " + index.ToString());
            }

            index = index + 1;
        }

        return IdentifierValidationResult.Success(IdentifierKind.NanoId, text);
    }

    /// <summary>Calculates the number of possible IDs under the default settings.</summary>
    /// <returns>The default alphabet length raised to the default size.</returns>
    public double CollisionSpace()
    {
        return CollisionSpaceForSettings(NanoIdSettings.Default());
    }

    /// <summary>Calculates the number of possible IDs for a default-alphabet size.</summary>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns>The default alphabet length raised to the supplied size.</returns>
    /// <exception cref="IdentifierException">The size is negative.</exception>
    public double CollisionSpaceForSize(int size)
    {
        return CollisionSpaceForDimensions(NanoIdSettings.DefaultAlphabet.Length, size);
    }

    /// <summary>Calculates the number of possible IDs for a custom alphabet and size.</summary>
    /// <param name="alphabet">Between 2 and 256 distinct characters.</param>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns>The alphabet length raised to the supplied size.</returns>
    /// <exception cref="IdentifierException">The alphabet is invalid or the size is negative.</exception>
    public double CollisionSpaceForAlphabet(string? alphabet, int size)
    {
        return CollisionSpaceForSettings(new NanoIdSettings(alphabet, size));
    }

    /// <summary>Calculates the number of possible IDs described by settings.</summary>
    /// <param name="settings">The alphabet and size to evaluate.</param>
    /// <returns>The configured alphabet length raised to the configured size.</returns>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The configured alphabet or size is invalid.</exception>
    public double CollisionSpaceForSettings(NanoIdSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        NanoIdSettings copy = NanoIdSettings.Copy(settings);
        copy.ThrowIfInvalid();
        return CollisionSpaceForDimensions(copy.Alphabet.Length, copy.Size);
    }

    /// <summary>Calculates the number of possible IDs from an alphabet length and size.</summary>
    /// <param name="alphabetLength">The alphabet size from 2 through 256.</param>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns>The alphabet length raised to the identifier size. Large spaces may return positive infinity.</returns>
    /// <exception cref="IdentifierException">Either argument is outside its supported range.</exception>
    public double CollisionSpaceForDimensions(int alphabetLength, int size)
    {
        ThrowIfInvalidAlphabetLength(alphabetLength);
        RandomTools.ValidateAlphabetLength(alphabetLength);
        NanoIdSettings.ThrowIfInvalidSize(size);
        return Math.Pow(alphabetLength, size);
    }

    /// <summary>Calculates the ideal entropy in bits under the default settings.</summary>
    /// <returns>The base-2 logarithm of the default collision space.</returns>
    public double BitsOfEntropy()
    {
        return BitsOfEntropyForSettings(NanoIdSettings.Default());
    }

    /// <summary>Calculates the ideal entropy in bits for a default-alphabet size.</summary>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns>The base-2 logarithm of the possible-ID count.</returns>
    /// <exception cref="IdentifierException">The size is negative.</exception>
    public double BitsOfEntropyForSize(int size)
    {
        return BitsOfEntropyForDimensions(NanoIdSettings.DefaultAlphabet.Length, size);
    }

    /// <summary>Calculates the ideal entropy in bits for a custom alphabet and size.</summary>
    /// <param name="alphabet">Between 2 and 256 distinct characters.</param>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns>The base-2 logarithm of the possible-ID count.</returns>
    /// <exception cref="IdentifierException">The alphabet is invalid or the size is negative.</exception>
    public double BitsOfEntropyForAlphabet(string? alphabet, int size)
    {
        return BitsOfEntropyForSettings(new NanoIdSettings(alphabet, size));
    }

    /// <summary>Calculates the ideal entropy in bits described by settings.</summary>
    /// <param name="settings">The alphabet and size to evaluate.</param>
    /// <returns>The base-2 logarithm of the possible-ID count.</returns>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The configured alphabet or size is invalid.</exception>
    public double BitsOfEntropyForSettings(NanoIdSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        NanoIdSettings copy = NanoIdSettings.Copy(settings);
        copy.ThrowIfInvalid();
        return BitsOfEntropyForDimensions(copy.Alphabet.Length, copy.Size);
    }

    /// <summary>Calculates ideal entropy from an alphabet length and identifier size.</summary>
    /// <param name="alphabetLength">The alphabet size from 2 through 256.</param>
    /// <param name="size">The non-negative identifier length.</param>
    /// <returns><c>size * log2(alphabetLength)</c>.</returns>
    /// <exception cref="IdentifierException">Either argument is outside its supported range.</exception>
    public double BitsOfEntropyForDimensions(int alphabetLength, int size)
    {
        ThrowIfInvalidAlphabetLength(alphabetLength);
        RandomTools.ValidateAlphabetLength(alphabetLength);
        NanoIdSettings.ThrowIfInvalidSize(size);
        return size * (Math.Log(alphabetLength) / Math.Log(2.0));
    }

    /// <summary>Estimates the chance that at least two generated IDs collide.</summary>
    /// <param name="alphabetLength">The alphabet size from 2 through 256.</param>
    /// <param name="size">The non-negative identifier length.</param>
    /// <param name="generatedCount">The non-negative number of generated IDs.</param>
    /// <returns>A birthday-bound probability from zero through one.</returns>
    /// <remarks>The approximation assumes independent, uniformly random generation.</remarks>
    /// <exception cref="IdentifierException">The alphabet length or identifier size is outside its supported range.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The generated count is negative.</exception>
    public double ApproximateCollisionProbability(int alphabetLength, int size, double generatedCount)
    {
        ThrowIfInvalidAlphabetLength(alphabetLength);
        RandomTools.ValidateAlphabetLength(alphabetLength);
        NanoIdSettings.ThrowIfInvalidSize(size);
        if (generatedCount < 0 || double.IsNaN(generatedCount))
        {
            throw new ArgumentOutOfRangeException("generatedCount");
        }

        if (generatedCount <= 1)
        {
            return 0;
        }

        double space = CollisionSpaceForDimensions(alphabetLength, size);
        if (space <= 1)
        {
            return 1;
        }

        if (double.IsPositiveInfinity(generatedCount))
        {
            return 1;
        }

        if (double.IsPositiveInfinity(space))
        {
            double logMagnitude = Math.Log(generatedCount) + Math.Log(generatedCount - 1) - Math.Log(2.0) - BitsOfEntropyForDimensions(alphabetLength, size) * Math.Log(2.0);
            double magnitude = Math.Exp(logMagnitude);
            return 1 - Math.Exp(-magnitude);
        }

        double exponent = -(generatedCount * (generatedCount - 1)) / (2 * space);
        return 1 - Math.Exp(exponent);
    }

    /// <summary>Estimates the chance that at least two IDs generated with the settings collide.</summary>
    /// <param name="settings">The alphabet and size to evaluate.</param>
    /// <param name="generatedCount">The non-negative number of generated IDs.</param>
    /// <returns>A birthday-bound probability from zero through one.</returns>
    /// <remarks>The approximation assumes independent, uniformly random generation.</remarks>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The configured alphabet or size is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The generated count is negative.</exception>
    public double ApproximateCollisionProbabilityForSettings(NanoIdSettings? settings, double generatedCount)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        NanoIdSettings copy = NanoIdSettings.Copy(settings);
        copy.ThrowIfInvalid();
        return ApproximateCollisionProbability(copy.Alphabet.Length, copy.Size, generatedCount);
    }

    /// <summary>Creates a new settings instance populated with library defaults.</summary>
    /// <returns>Independent mutable default settings.</returns>
    public NanoIdSettings DefaultSettings()
    {
        return NanoIdSettings.Default();
    }

    internal static void ThrowIfInvalidAlphabetLength(int alphabetLength)
    {
        if (alphabetLength < 2 || alphabetLength > 256)
        {
            throw new IdentifierException(IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        }
    }
}
