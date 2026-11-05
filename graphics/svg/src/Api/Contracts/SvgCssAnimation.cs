namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCssAnimation"/>.</summary>
public interface ISvgCssAnimation
{
    /// <summary>Builds a CSS animation plan using deterministic defaults.</summary>
    /// <param name="cascade">The target element's completed cascade.</param>
    /// <param name="styleSheets">The ordered stylesheets containing keyframes rules.</param>
    /// <returns>The resolved animation list.</returns>
    SvgCssAnimationPlan ResolveWithCascadeAndStyleSheets(SvgCascadeResult cascade, System.Collections.Generic.List<SvgStyleSheet> styleSheets);
    /// <summary>Builds a bounded CSS animation plan.</summary>
    /// <param name="cascade">The target element's completed cascade.</param>
    /// <param name="styleSheets">The ordered stylesheets containing keyframes rules.</param>
    /// <param name="options">The property, parsing, and interpolation options.</param>
    /// <returns>The resolved animation list.</returns>
    SvgCssAnimationPlan ResolveWithCascadeAndStyleSheetsAndOptions(SvgCascadeResult cascade, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgCssAnimationOptions options);
    /// <summary>Samples and composes a CSS animation plan at caller-provided seconds.</summary>
    /// <param name="plan">The resolved animation plan.</param>
    /// <param name="timeSeconds">The finite caller time in seconds.</param>
    /// <param name="underlying">The typed underlying cascade values.</param>
    /// <param name="options">The interpolation options.</param>
    /// <returns>The timing phases and final affected property values.</returns>
    SvgCssAnimationSampleResult Sample(SvgCssAnimationPlan? plan, double timeSeconds, System.Collections.Generic.List<SvgCssAnimationUnderlyingValue>? underlying, SvgCssAnimationOptions options);
}
