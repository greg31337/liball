namespace icc;

/// <summary>
/// Produces bounded deterministic descriptions of profiles, tags, and transform selection.
/// </summary>
public interface IIccProfileInspector
{
    /// <summary>
    /// Builds a bounded structured profile inspection.
    /// </summary>
    /// <param name="profile">The profile to inspect.</param>
    /// <param name="options">The hierarchy and raw-data limits.</param>
    /// <returns>The populated profile summary and inspection tree.</returns>
    IccProfileSummary InspectProfile(IccProfile profile, IccInspectionOptions options);

    /// <summary>
    /// Produces a concise deterministic profile summary.
    /// </summary>
    /// <param name="profile">The profile to summarize.</param>
    /// <returns>The summary text.</returns>
    string SummarizeProfile(IccProfile profile);

    /// <summary>
    /// Produces a deterministic profile inspection at the requested detail level.
    /// </summary>
    /// <param name="profile">The profile to inspect.</param>
    /// <param name="detail">The requested inspection detail.</param>
    /// <param name="maximumValues">The maximum number of repeated values to include.</param>
    /// <returns>The inspection text.</returns>
    string InspectProfileText(IccProfile profile, IccInspectionDetail detail, int maximumValues);

    /// <summary>
    /// Produces a deterministic inspection of one profile tag.
    /// </summary>
    /// <param name="tag">The tag to inspect.</param>
    /// <param name="detail">The requested inspection detail.</param>
    /// <param name="maximumValues">The maximum number of repeated values to include.</param>
    /// <returns>The tag inspection text.</returns>
    string InspectTagText(IccTag tag, IccInspectionDetail detail, int maximumValues);

    /// <summary>
    /// Describes the transform tag and fallback selected for a profile operation.
    /// </summary>
    /// <param name="profile">The profile whose transform is selected.</param>
    /// <param name="direction">The transform direction.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <returns>The transform-selection description.</returns>
    string DescribeTransformSelection(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent);

    /// <summary>
    /// Formats a bounded transform execution trace.
    /// </summary>
    /// <param name="trace">The transform trace to inspect.</param>
    /// <param name="maximumValues">The maximum number of intermediate vectors and components to include.</param>
    /// <returns>The deterministic trace text.</returns>
    string InspectTransformTrace(IccTransformTrace trace, int maximumValues);
}
