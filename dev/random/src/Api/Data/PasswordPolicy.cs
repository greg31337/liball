namespace random;

/// <summary>
/// Describes the length, allowed characters, required groups, repetition rule, and shuffle rule for a generated password.
/// </summary>
/// <remarks>
/// Fields are mutable so callers may adjust a policy before use. Password generation validates and snapshots
/// every field before requesting entropy. Required alphabet and count arrays are copied by the constructor.
/// A valid policy does not by itself guarantee adequate password strength: entropy depends on the full alphabet,
/// length, required groups, repetition rule, and security of the <see cref="SecureRandomGenerator"/> used.
/// </remarks>
public class PasswordPolicy
{
    /// <summary>
    /// The positive number of UTF-16 characters in the generated password.
    /// </summary>
    public int Length;

    /// <summary>
    /// The full set of distinct, non-surrogate characters allowed in the password.
    /// </summary>
    public string? Alphabet;

    /// <summary>
    /// Character groups from which minimum required counts are selected.
    /// </summary>
    public string[]? RequiredAlphabets;

    /// <summary>
    /// Minimum selected counts corresponding positionally to <see cref="RequiredAlphabets"/>.
    /// </summary>
    public int[]? RequiredCounts;

    /// <summary>
    /// Whether the same character may occur more than once in the generated password.
    /// </summary>
    public bool AllowRepeatedCharacters;

    /// <summary>
    /// Whether required and filler characters receive a final unbiased Fisher-Yates shuffle.
    /// </summary>
    public bool Shuffle;

    /// <summary>
    /// Creates a policy with explicit repetition and final-shuffle rules.
    /// </summary>
    /// <param name="length">The positive password length.</param>
    /// <param name="alphabet">The full allowed character alphabet.</param>
    /// <param name="requiredAlphabets">Required character groups.</param>
    /// <param name="requiredCounts">Minimum selected counts corresponding to the required groups.</param>
    /// <param name="allowRepeatedCharacters">Whether characters may repeat.</param>
    /// <param name="shuffle">Whether to apply a final unbiased Fisher-Yates shuffle.</param>
    public PasswordPolicy(int length, string? alphabet, string[]? requiredAlphabets, int[]? requiredCounts, bool allowRepeatedCharacters, bool shuffle)
    {
        Length = length;
        Alphabet = alphabet;
        RequiredAlphabets = requiredAlphabets == null ? null : (string[])requiredAlphabets.Clone();
        RequiredCounts = requiredCounts == null ? null : (int[])requiredCounts.Clone();
        AllowRepeatedCharacters = allowRepeatedCharacters;
        Shuffle = shuffle;
    }
}
