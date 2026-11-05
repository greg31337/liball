namespace unicode;

/// <summary>
/// Provides PRECIS-style string preparation and validation operations.
/// </summary>
public interface IUnicodePrecis
{
    /// <summary>
    /// Prepares text with the default case-mapped username profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareUsername(string text);

    /// <summary>
    /// Prepares text with the default nickname profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareNickname(string text);

    /// <summary>
    /// Prepares text with the default opaque-string profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareOpaqueString(string text);

    /// <summary>
    /// Maps, case-converts, normalizes, and validates text according to a PRECIS-style profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    string Prepare(string text, UnicodePrecisProfile profile);

    /// <summary>
    /// Prepares text and applies the profile's case-insensitive comparison policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    string GetCompareKey(string text, UnicodePrecisProfile profile);

    /// <summary>
    /// Reports whether two strings produce the same prepared comparison key under a profile.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c>, <c>profile</c> is <c>null</c>.</exception>
    bool EqualsPrepared(string left, string right, UnicodePrecisProfile profile);

    /// <summary>
    /// Reports whether preparation would leave text unchanged and valid under the profile.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    bool IsPrepared(string text, UnicodePrecisProfile profile);

    /// <summary>
    /// Validates text under the selected profile and returns the prepared value and diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="profile">The profile that defines preparation and validation rules.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>profile</c> is <c>null</c>.</exception>
    UnicodePrecisValidationResult Validate(string text, UnicodePrecisProfile profile);
}

