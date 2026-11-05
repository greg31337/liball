namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgConditionalProcessing"/>.</summary>
public interface ISvgConditionalProcessing
{
    /// <summary>Evaluates the conditional-processing attributes on one element using an empty host environment.</summary>
    /// <param name="node">The element to evaluate.</param>
    /// <returns>The evaluated conditional state.</returns>
    SvgConditionalResult EvaluateWithNode(SvgNode node);
    /// <summary>Evaluates the conditional-processing attributes on one element.</summary>
    /// <param name="node">The element to evaluate.</param>
    /// <param name="options">The host capabilities, preferences, catalog, and limits.</param>
    /// <returns>The evaluated conditional state.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    SvgConditionalResult EvaluateWithNodeAndOptions(SvgNode node, SvgConditionalProcessingOptions options);
    /// <summary>Selects the switch child for an empty host environment.</summary>
    /// <param name="switchElement">The switch element.</param>
    /// <returns>The ordered candidate evaluations and selected child.</returns>
    SvgSwitchResult SelectSwitchWithSwitchElement(SvgNode switchElement);
    /// <summary>Selects the best matching direct switch child using ordered host language preferences.</summary>
    /// <param name="switchElement">The switch element.</param>
    /// <param name="options">The host capabilities, preferences, catalog, and limits.</param>
    /// <returns>The ordered candidate evaluations and selected child.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an SVG switch element.</exception>
    SvgSwitchResult SelectSwitchWithSwitchElementAndOptions(SvgNode switchElement, SvgConditionalProcessingOptions options);
}
