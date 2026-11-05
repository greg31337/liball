namespace icc;

/// <summary>Produces bounded deterministic profile descriptions.</summary>
public class IccProfileInspector : IIccProfileInspector
{
    internal ServiceAvailability Core;

    /// <summary>Initializes a profile inspector.</summary>
    public IccProfileInspector()
    {
        Core = new ServiceAvailability("Profile inspection");
    }

    /// <summary>Builds a bounded structured profile inspection.</summary>
    /// <param name="profile">The profile to inspect.</param>
    /// <param name="options">The hierarchy and raw-data limits.</param>
    /// <returns>The populated summary and inspection tree.</returns>
    public IccProfileSummary InspectProfile(IccProfile profile, IccInspectionOptions options)
    {
        return InspectionBuilder.Build(profile, options);
    }

    /// <summary>Summarizes a profile.</summary>
    /// <param name="profile">The profile to summarize.</param>
    /// <returns>The summary text.</returns>
    public string SummarizeProfile(IccProfile profile)
    {
        return ProfileDescriptionCore.SummarizeProfile(profile);
    }

    /// <summary>Inspects a profile.</summary>
    /// <param name="profile">The profile to inspect.</param>
    /// <param name="detail">The detail level.</param>
    /// <param name="maximumValues">The repeated-value limit.</param>
    /// <returns>The inspection text.</returns>
    public string InspectProfileText(IccProfile profile, IccInspectionDetail detail, int maximumValues)
    {
        return ProfileDescriptionCore.InspectProfileText(profile, detail, maximumValues);
    }

    /// <summary>Inspects a tag.</summary>
    /// <param name="tag">The tag to inspect.</param>
    /// <param name="detail">The detail level.</param>
    /// <param name="maximumValues">The repeated-value limit.</param>
    /// <returns>The inspection text.</returns>
    public string InspectTagText(IccTag tag, IccInspectionDetail detail, int maximumValues)
    {
        return ProfileDescriptionCore.InspectTagText(tag, detail, maximumValues);
    }

    /// <summary>Describes transform selection.</summary>
    /// <param name="profile">The source profile.</param>
    /// <param name="direction">The transform direction.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <returns>The selection description.</returns>
    public string DescribeTransformSelection(IccProfile profile, IccTransformDirection direction, IccRenderingIntent intent)
    {
        return ProfileDescriptionCore.DescribeTransformSelection(profile, direction, intent);
    }

    /// <summary>Formats a bounded transform execution trace.</summary>
    /// <param name="trace">The trace to inspect.</param>
    /// <param name="maximumValues">The repeated-value limit.</param>
    /// <returns>The deterministic trace text.</returns>
    public string InspectTransformTrace(IccTransformTrace trace, int maximumValues)
    {
        return InspectionTextWriter.WriteTransformTrace(trace, maximumValues);
    }
}
