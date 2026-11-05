namespace pptx;

/// <summary>
/// Provides public operations for presentation show properties.
/// </summary>
public class PptxPresentationPropertiesOperations : IPptxPresentationPropertiesOperations
{
    /// <summary>
    /// Creates a presentation properties operations instance.
    /// </summary>
    public PptxPresentationPropertiesOperations()
    {
    }

    /// <summary>
    /// Saves the current presentation show properties while preserving unrelated markup.
    /// </summary>
    /// <param name="target">The presentation properties to save.</param>
    public void Save(PptxPresentationProperties target)
    {
        target.Save();
    }
    /// <summary>
    /// Enables or disables presentation looping and saves the change.
    /// </summary>
    /// <param name="loop">Whether the slide show loops after its final slide.</param>
    /// <param name="target">The presentation properties to update.</param>
    public void SetLoop(PptxPresentationProperties target, bool loop)
    {
        target.SetLoop(loop);
    }
    /// <summary>
    /// Enables or disables recorded slide timings and saves the change.
    /// </summary>
    /// <param name="useTimings">Whether recorded slide timings are used.</param>
    /// <param name="target">The presentation properties to update.</param>
    public void SetUseTimings(PptxPresentationProperties target, bool useTimings)
    {
        target.SetUseTimings(useTimings);
    }
}
