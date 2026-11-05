namespace uuid;

/// <summary>Defines Nano ID generation, validation, and collision-analysis operations.</summary>
public interface INanoId
{
    /// <summary>Generates a default Nano ID.</summary>
    string New();
    /// <summary>Generates a Nano ID with a custom size.</summary>
    string NewWithSize(int size);
    /// <summary>Generates a Nano ID with a custom alphabet and size.</summary>
    string NewWithAlphabet(string? alphabet, int size);
    /// <summary>Generates a Nano ID from settings.</summary>
    string NewWithSettings(NanoIdSettings? settings);
    /// <summary>Creates independent default settings.</summary>
    NanoIdSettings DefaultSettings();
    /// <summary>Validates an alphabet.</summary>
    IdentifierValidationResult ValidateAlphabet(string? alphabet);
    /// <summary>Validates text with default settings.</summary>
    IdentifierValidationResult Validate(string? text);
    /// <summary>Validates text with custom settings.</summary>
    IdentifierValidationResult ValidateWithSettings(string? text, NanoIdSettings? settings);
    /// <summary>Returns the default collision space.</summary>
    double CollisionSpace();
    /// <summary>Returns collision space for a custom size.</summary>
    double CollisionSpaceForSize(int size);
    /// <summary>Returns collision space for an alphabet and size.</summary>
    double CollisionSpaceForAlphabet(string? alphabet, int size);
    /// <summary>Returns collision space for settings.</summary>
    double CollisionSpaceForSettings(NanoIdSettings? settings);
    /// <summary>Returns collision space for dimensions.</summary>
    double CollisionSpaceForDimensions(int alphabetLength, int size);
    /// <summary>Returns default entropy.</summary>
    double BitsOfEntropy();
    /// <summary>Returns entropy for a custom size.</summary>
    double BitsOfEntropyForSize(int size);
    /// <summary>Returns entropy for an alphabet and size.</summary>
    double BitsOfEntropyForAlphabet(string? alphabet, int size);
    /// <summary>Returns entropy for settings.</summary>
    double BitsOfEntropyForSettings(NanoIdSettings? settings);
    /// <summary>Returns entropy for dimensions.</summary>
    double BitsOfEntropyForDimensions(int alphabetLength, int size);
    /// <summary>Approximates collision probability for dimensions.</summary>
    double ApproximateCollisionProbability(int alphabetLength, int size, double generatedCount);
    /// <summary>Approximates collision probability for settings.</summary>
    double ApproximateCollisionProbabilityForSettings(NanoIdSettings? settings, double generatedCount);
}
