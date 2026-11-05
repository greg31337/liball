namespace fake;

/// <summary>
/// Contains the correlated grammatical forms of one locale-provided pronoun set.
/// </summary>
public class PronounSet
{
    /// <summary>The subject form, such as <c>they</c>.</summary>
    public string Subject = string.Empty;

    /// <summary>The object form, such as <c>them</c>.</summary>
    public string Object = string.Empty;

    /// <summary>The possessive adjective form, such as <c>their</c>.</summary>
    public string PossessiveAdjective = string.Empty;

    /// <summary>The possessive pronoun form, such as <c>theirs</c>.</summary>
    public string PossessivePronoun = string.Empty;

    /// <summary>The reflexive form, such as <c>themself</c>.</summary>
    public string Reflexive = string.Empty;

    /// <summary>The compact locale-provided display form.</summary>
    public string Display = string.Empty;

    internal PronounSet(
        string subject,
        string objectValue,
        string possessiveAdjective,
        string possessivePronoun,
        string reflexive,
        string display)
    {
        Subject = subject;
        Object = objectValue;
        PossessiveAdjective = possessiveAdjective;
        PossessivePronoun = possessivePronoun;
        Reflexive = reflexive;
        Display = display;
    }
}
