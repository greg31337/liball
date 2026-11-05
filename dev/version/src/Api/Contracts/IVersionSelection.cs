namespace version;

/// <summary>
/// Defines finite candidate filtering and selection operations for dependency-injected consumers.
/// </summary>
public interface IVersionSelection
{
    /// <summary>
    /// Filters candidates by range membership and selection options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public UniversalVersion[] Filter(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options);

    /// <summary>
    /// Selects the lowest candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? MinSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options);

    /// <summary>
    /// Selects the highest candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? MaxSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options);

    /// <summary>
    /// Selects the preferred stable candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? BestSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options);

    /// <summary>
    /// Selects the minimum parsed candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The minimum candidate, or null when the input is empty.</returns>
    public UniversalVersion? MinimumVersion(UniversalVersion[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Selects the maximum parsed candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The maximum candidate, or null when the input is empty.</returns>
    public UniversalVersion? MaximumVersion(UniversalVersion[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Parses and selects the minimum textual candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The minimum candidate text, or null when the input is empty.</returns>
    public string? MinimumText(string[] candidates, VersionComparisonOptions options);

    /// <summary>
    /// Parses and selects the maximum textual candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The maximum candidate text, or null when the input is empty.</returns>
    public string? MaximumText(string[] candidates, VersionComparisonOptions options);
}

