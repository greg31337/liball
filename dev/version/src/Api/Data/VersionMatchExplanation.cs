namespace version;

/// <summary>
/// Represents a version match explanation.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionMatchExplanation
{
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;
    /// <summary>
    /// Stores the <c>match</c> value.
    /// </summary>
    public bool match;
    /// <summary>
    /// Stores the <c>matchedSetIndex</c> value.
    /// </summary>
    public int matchedSetIndex;
    /// <summary>
    /// Stores the <c>failedSetIndex</c> value.
    /// </summary>
    public int failedSetIndex;
    /// <summary>
    /// Stores the <c>failedComparator</c> value.
    /// </summary>
    public VersionComparator? failedComparator;
    /// <summary>
    /// Stores the <c>message</c> value.
    /// </summary>
    public string message;

    /// <summary>
    /// Initializes a new Version Match Explanation instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <param name="matchValue">Whether to enable match value.</param>
    /// <param name="matchedSetIndexValue">The matched set index value used by the operation.</param>
    /// <param name="failedSetIndexValue">The failed set index value used by the operation.</param>
    /// <param name="failedComparatorValue">The failed comparator value used by the operation.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionMatchExplanation(
        UniversalVersion versionValue,
        bool matchValue,
        int matchedSetIndexValue,
        int failedSetIndexValue,
        VersionComparator? failedComparatorValue,
        string messageValue)
    {
        System.ArgumentNullException.ThrowIfNull(versionValue);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        version = versionValue;
        match = matchValue;
        matchedSetIndex = matchedSetIndexValue;
        failedSetIndex = failedSetIndexValue;
        failedComparator = failedComparatorValue;
        message = messageValue;
    }

    /// <summary>
    /// Creates a match.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="setIndex">The zero-based set index.</param>
    /// <returns>The resulting version match explanation.</returns>
    internal static VersionMatchExplanation CreateMatch(UniversalVersion version, int setIndex)
    {
        return new VersionMatchExplanation(version, true, setIndex, -1, null, "Version satisfies range.");
    }

    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="setIndex">The zero-based set index.</param>
    /// <param name="comparator">The comparator used by the operation.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <returns>The resulting version match explanation.</returns>
    internal static VersionMatchExplanation CreateFailure(
        UniversalVersion version,
        int setIndex,
        VersionComparator? comparator,
        string message)
    {
        return new VersionMatchExplanation(version, false, -1, setIndex, comparator, message);
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Reports whether match.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsMatch()
    {
        return match;
    }

    /// <summary>
    /// Reports whether the operation failed and produced diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsFailure()
    {
        return !match;
    }

    /// <summary>
    /// Reports whether this value has matched set.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasMatchedSet()
    {
        return matchedSetIndex >= 0;
    }

    /// <summary>
    /// Gets a matched set index.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal int GetMatchedSetIndex()
    {
        if (matchedSetIndex < 0)
        {
            throw new System.InvalidOperationException("Explanation does not contain a matched set.");
        }

        return matchedSetIndex;
    }

    /// <summary>
    /// Reports whether this value has failed set.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFailedSet()
    {
        return failedSetIndex >= 0;
    }

    /// <summary>
    /// Gets a failed set index.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal int GetFailedSetIndex()
    {
        if (failedSetIndex < 0)
        {
            throw new System.InvalidOperationException("Explanation does not contain a failed set.");
        }

        return failedSetIndex;
    }

    /// <summary>
    /// Reports whether this value has failed comparator.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFailedComparator()
    {
        return failedComparator != null;
    }

    /// <summary>
    /// Gets a failed comparator.
    /// </summary>
    /// <returns>The resulting version comparator.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal VersionComparator GetFailedComparator()
    {
        if (failedComparator == null)
        {
            throw new System.InvalidOperationException("Explanation does not contain a failed comparator.");
        }

        return failedComparator;
    }

    /// <summary>
    /// Gets a message.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMessage()
    {
        return message;
    }
}
