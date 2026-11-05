namespace version;

/// <summary>
/// Defines version comparison operations for dependency-injected consumers.
/// </summary>
public interface IVersionComparison
{
    /// <summary>
    /// Parses and compares two version texts with the supplied options.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int Compare(string leftText, string rightText, VersionComparisonOptions options);

    /// <summary>
    /// Compares two parsed versions with the supplied options.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int CompareVersions(UniversalVersion left, UniversalVersion right, VersionComparisonOptions options);

    /// <summary>
    /// Sorts parsed candidates in ascending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidates in ascending order.</returns>
    public UniversalVersion[] SortVersionsAscending(UniversalVersion[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Sorts parsed candidates in descending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidates in descending order.</returns>
    public UniversalVersion[] SortVersionsDescending(UniversalVersion[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Parses and sorts textual candidates in ascending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidate texts in ascending order.</returns>
    public string[] SortTextsAscending(string[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Parses and sorts textual candidates in descending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidate texts in descending order.</returns>
    public string[] SortTextsDescending(string[] candidates, VersionComparisonOptions options);
}

