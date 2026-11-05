namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationTimeline"/>.</summary>
public interface ISvgAnimationTimeline
{
    /// <summary>Evaluates every animation in a document using default options.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <returns>The resolved timeline state.</returns>
    SvgAnimationTimelineResult EvaluateWithDocumentAndTime(SvgDocument document, double time);
    /// <summary>Evaluates every animation in a document with explicit events and limits.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <param name="options">The timeline options.</param>
    /// <returns>The resolved timeline state.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when time is not finite.</exception>
    SvgAnimationTimelineResult EvaluateWithDocumentAndTimeAndOptions(SvgDocument document, double time, SvgAnimationTimelineOptions options);
    /// <summary>Evaluates animations in one connected or detached subtree.</summary>
    /// <param name="root">The subtree root.</param>
    /// <param name="time">The finite caller time in seconds.</param>
    /// <param name="options">The timeline options.</param>
    /// <returns>The resolved timeline state.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when time is not finite.</exception>
    SvgAnimationTimelineResult EvaluateWithRootAndTimeAndOptions(SvgNode root, double time, SvgAnimationTimelineOptions options);
}
