namespace version;

/// <summary>
/// Represents a cargo feature.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoFeature
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>members</c> value.
    /// </summary>
    public string[] members;

    /// <summary>
    /// Initializes a new Cargo Feature instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="memberValues">The member values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoFeature(string nameValue, string[] memberValues)
    {
        ValidateFeatureToken(nameValue, "Cargo feature name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(memberValues);

        name = nameValue;
        members = CopyFeatureMembers(memberValues);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="members">The members to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting cargo feature.</returns>
    internal static CargoFeature Create(string name, string[] members)
    {
        return new CargoFeature(name, members);
    }

    /// <summary>
    /// Gets a name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets a member count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetMemberCount()
    {
        return members.Length;
    }

    /// <summary>
    /// Gets a member.
    /// </summary>
    /// <param name="memberIndex">The zero-based member index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetMember(int memberIndex)
    {
        if (memberIndex < 0 || memberIndex >= members.Length)
        {
            throw new System.ArgumentOutOfRangeException("memberIndex");
        }

        return members[memberIndex];
    }

    /// <summary>
    /// Gets a members.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetMembers()
    {
        return CopyStrings(members);
    }

    /// <summary>
    /// Validates a feature token.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidateFeatureToken(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index])
                && text[index] != '-'
                && text[index] != '_'
                && text[index] != '+'
                && text[index] != '.'
                && text[index] != '/'
                && text[index] != ':')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }

    /// <summary>
    /// Validates a feature member token.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidateFeatureMemberToken(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index])
                && text[index] != '-'
                && text[index] != '_'
                && text[index] != '+'
                && text[index] != '.'
                && text[index] != '/'
                && text[index] != ':'
                && text[index] != '?')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }

    internal static string[] CopyFeatureMembers(string[] memberValues)
    {
        string[] copy = new string[memberValues.Length];
        int index = 0;

        while (index < memberValues.Length)
        {
            ValidateFeatureMemberToken(memberValues[index], "Cargo feature members cannot contain empty or invalid values.");
            copy[index] = memberValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
