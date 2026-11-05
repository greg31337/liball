namespace pptx;

/// <summary>
/// Defines operations for presentation show properties.
/// </summary>
public interface IPptxPresentationPropertiesOperations
{
    /// <summary>
    /// Saves the current presentation show properties while preserving unrelated markup.
    /// </summary>
    /// <param name="target">The presentation properties to save.</param>
    void Save(PptxPresentationProperties target);
    /// <summary>
    /// Enables or disables presentation looping and saves the change.
    /// </summary>
    /// <param name="loop">Whether the slide show loops after its final slide.</param>
    /// <param name="target">The presentation properties to update.</param>
    void SetLoop(PptxPresentationProperties target, bool loop);
    /// <summary>
    /// Enables or disables recorded slide timings and saves the change.
    /// </summary>
    /// <param name="useTimings">Whether recorded slide timings are used.</param>
    /// <param name="target">The presentation properties to update.</param>
    void SetUseTimings(PptxPresentationProperties target, bool useTimings);
}
