using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarParseOptions"/>.
/// </summary>
public interface IEmailCalendarParseOptionsContract
{
    /// <summary>
    /// Gets the maximum calendar bytes.
    /// </summary>
    /// <returns>The current maximum calendar bytes.</returns>
    int GetMaximumCalendarBytes();
    /// <summary>
    /// Sets the maximum calendar bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCalendarBytes(int value);
    /// <summary>
    /// Gets the maximum physical line bytes.
    /// </summary>
    /// <returns>The current maximum physical line bytes.</returns>
    int GetMaximumPhysicalLineBytes();
    /// <summary>
    /// Sets the maximum physical line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumPhysicalLineBytes(int value);
    /// <summary>
    /// Gets the maximum unfolded line bytes.
    /// </summary>
    /// <returns>The current maximum unfolded line bytes.</returns>
    int GetMaximumUnfoldedLineBytes();
    /// <summary>
    /// Sets the maximum unfolded line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumUnfoldedLineBytes(int value);
    /// <summary>
    /// Gets the maximum content lines.
    /// </summary>
    /// <returns>The current maximum content lines.</returns>
    int GetMaximumContentLines();
    /// <summary>
    /// Sets the maximum content lines used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumContentLines(int value);
    /// <summary>
    /// Gets the maximum properties.
    /// </summary>
    /// <returns>The current maximum properties.</returns>
    int GetMaximumProperties();
    /// <summary>
    /// Sets the maximum properties used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumProperties(int value);
    /// <summary>
    /// Gets the maximum components.
    /// </summary>
    /// <returns>The current maximum components.</returns>
    int GetMaximumComponents();
    /// <summary>
    /// Sets the maximum components used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumComponents(int value);
    /// <summary>
    /// Gets the maximum component depth.
    /// </summary>
    /// <returns>The current maximum component depth.</returns>
    int GetMaximumComponentDepth();
    /// <summary>
    /// Sets the maximum component depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumComponentDepth(int value);
    /// <summary>
    /// Gets the maximum parameters per property.
    /// </summary>
    /// <returns>The current maximum parameters per property.</returns>
    int GetMaximumParametersPerProperty();
    /// <summary>
    /// Sets the maximum parameters per property used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumParametersPerProperty(int value);
    /// <summary>
    /// Gets the maximum parameter values.
    /// </summary>
    /// <returns>The current maximum parameter values.</returns>
    int GetMaximumParameterValues();
    /// <summary>
    /// Sets the maximum parameter values used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumParameterValues(int value);
    /// <summary>
    /// Gets the maximum traversal depth.
    /// </summary>
    /// <returns>The current maximum traversal depth.</returns>
    int GetMaximumTraversalDepth();
    /// <summary>
    /// Sets the maximum traversal depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumTraversalDepth(int value);
    /// <summary>
    /// Gets the maximum traversal entity count.
    /// </summary>
    /// <returns>The current maximum traversal entity count.</returns>
    int GetMaximumTraversalEntityCount();
    /// <summary>
    /// Sets the maximum traversal entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumTraversalEntityCount(int value);
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailStructuredValueParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailStructuredValueParseMode value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCalendarParseOptions Clone();
}
