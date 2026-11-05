using System;

namespace uuid;

/// <summary>Configures the alphabet and character count used to generate or validate Nano IDs.</summary>
/// <remarks>The fields are mutable. Generation copies and validates the settings before use, so later caller changes do not affect an operation already in progress.</remarks>
public class NanoIdSettings
{
    /// <summary>The 64-character URL-safe alphabet used by default.</summary>
    public const string DefaultAlphabet = "useandom-26T198340PX75pxJACKVERYMINDBUSHWOLF_GQZbfghjklqvwyzrict";

    /// <summary>The default number of characters in a generated Nano ID.</summary>
    public const int DefaultSize = 21;

    /// <summary>The distinct characters from which each output character is selected.</summary>
    /// <remarks>An alphabet must contain between 2 and 256 characters with no duplicates.</remarks>
    public string Alphabet;

    /// <summary>The exact number of characters to generate or accept.</summary>
    /// <remarks>Zero is allowed and produces an empty identifier; negative sizes are invalid.</remarks>
    public int Size;

    /// <summary>Creates validated settings with a custom alphabet and size.</summary>
    /// <param name="alphabet">Between 2 and 256 distinct characters.</param>
    /// <param name="size">The non-negative output length.</param>
    /// <exception cref="IdentifierException">The alphabet is <c>null</c>, has an invalid length, contains duplicate characters, or the size is negative.</exception>
    public NanoIdSettings(string? alphabet, int size)
    {
        if (alphabet == null)
        {
            throw new IdentifierException(IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        }

        ThrowIfInvalidAlphabet(alphabet);
        ThrowIfInvalidSize(size);
        Alphabet = alphabet;
        Size = size;
    }

    /// <summary>Creates a new settings instance populated with library defaults.</summary>
    /// <returns>Independent mutable default settings.</returns>
    internal static NanoIdSettings Default()
    {
        return new NanoIdSettings(DefaultAlphabet, DefaultSize);
    }

    internal static NanoIdSettings Copy(NanoIdSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        return new NanoIdSettings(settings.Alphabet, settings.Size);
    }

    /// <summary>Validates the current field values without throwing.</summary>
    /// <returns>A successful result when the alphabet and size are usable, or a failure reason.</returns>
    internal IdentifierValidationResult Validate()
    {
        IdentifierValidationResult alphabetValidation = ValidateAlphabet(Alphabet);
        if (!alphabetValidation.Ok)
        {
            return alphabetValidation;
        }

        return ValidateSize(Size);
    }

    internal void ThrowIfInvalid()
    {
        ThrowIfInvalidAlphabet(Alphabet);
        ThrowIfInvalidSize(Size);
    }

    internal static IdentifierValidationResult ValidateAlphabet(string? alphabet)
    {
        if (alphabet == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        }

        if (alphabet.Length < 2 || alphabet.Length > 256)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        }

        int index = 0;
        while (index < alphabet.Length)
        {
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (alphabet[earlierIndex] == alphabet[index])
                {
                    return IdentifierValidationResult.Failure(IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index " + index.ToString());
                }

                earlierIndex = earlierIndex + 1;
            }

            index = index + 1;
        }

        return IdentifierValidationResult.Success(IdentifierKind.NanoId, alphabet);
    }

    internal static IdentifierValidationResult ValidateSize(int size)
    {
        if (size < 0)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        }

        return IdentifierValidationResult.Success(IdentifierKind.NanoId, size.ToString());
    }

    internal static void ThrowIfInvalidAlphabet(string? alphabet)
    {
        if (alphabet == null)
        {
            throw new IdentifierException(IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        }

        IdentifierValidationResult validation = ValidateAlphabet(alphabet);
        if (!validation.Ok)
        {
            throw new IdentifierException(validation.Code, validation.Error);
        }
    }

    internal static void ThrowIfInvalidSize(int size)
    {
        if (size < 0)
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        }
    }
}
