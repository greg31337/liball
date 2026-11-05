using System;

namespace email;
/// <summary>
/// Controls how calendar parse operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCalendarParseOptions : IEmailCalendarParseOptionsContract
{
    /// <summary>
    /// The default maximum calendar bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumCalendarBytes = 10485760;
    /// <summary>
    /// The default maximum physical line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumPhysicalLineBytes = 65536;
    /// <summary>
    /// The default maximum unfolded line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumUnfoldedLineBytes = 262144;
    /// <summary>
    /// The default maximum content lines used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumContentLines = 10000;
    /// <summary>
    /// The default maximum properties used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumProperties = 10000;
    /// <summary>
    /// The default maximum components used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumComponents = 1000;
    /// <summary>
    /// The default maximum parameters per property used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumParametersPerProperty = 128;
    /// <summary>
    /// The default maximum parameter values used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumParameterValues = 512;
    int maximumCalendarBytes;
    int maximumPhysicalLineBytes;
    int maximumUnfoldedLineBytes;
    int maximumContentLines;
    int maximumProperties;
    int maximumComponents;
    int maximumComponentDepth;
    int maximumParametersPerProperty;
    int maximumParameterValues;
    int maximumTraversalDepth;
    int maximumTraversalEntityCount;
    EmailStructuredValueParseMode mode;
    /// <summary>
    /// Creates a new calendar parse options object.
    /// </summary>
    public EmailCalendarParseOptions()
    {
        maximumCalendarBytes = DefaultMaximumCalendarBytes;
        maximumPhysicalLineBytes = DefaultMaximumPhysicalLineBytes;
        maximumUnfoldedLineBytes = DefaultMaximumUnfoldedLineBytes;
        maximumContentLines = DefaultMaximumContentLines;
        maximumProperties = DefaultMaximumProperties;
        maximumComponents = DefaultMaximumComponents;
        maximumComponentDepth = 16;
        maximumParametersPerProperty = DefaultMaximumParametersPerProperty;
        maximumParameterValues = DefaultMaximumParameterValues;
        maximumTraversalDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumTraversalEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        mode = EmailStructuredValueParseMode.Tolerant;
    }

    /// <summary>
    /// Gets the maximum calendar bytes.
    /// </summary>
    /// <returns>The current maximum calendar bytes.</returns>
    public int GetMaximumCalendarBytes()
    {
        return maximumCalendarBytes;
    }

    /// <summary>
    /// Sets the maximum calendar bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCalendarBytes(int value)
    {
        maximumCalendarBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum physical line bytes.
    /// </summary>
    /// <returns>The current maximum physical line bytes.</returns>
    public int GetMaximumPhysicalLineBytes()
    {
        return maximumPhysicalLineBytes;
    }

    /// <summary>
    /// Sets the maximum physical line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPhysicalLineBytes(int value)
    {
        maximumPhysicalLineBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum unfolded line bytes.
    /// </summary>
    /// <returns>The current maximum unfolded line bytes.</returns>
    public int GetMaximumUnfoldedLineBytes()
    {
        return maximumUnfoldedLineBytes;
    }

    /// <summary>
    /// Sets the maximum unfolded line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumUnfoldedLineBytes(int value)
    {
        maximumUnfoldedLineBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum content lines.
    /// </summary>
    /// <returns>The current maximum content lines.</returns>
    public int GetMaximumContentLines()
    {
        return maximumContentLines;
    }

    /// <summary>
    /// Sets the maximum content lines used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumContentLines(int value)
    {
        maximumContentLines = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum properties.
    /// </summary>
    /// <returns>The current maximum properties.</returns>
    public int GetMaximumProperties()
    {
        return maximumProperties;
    }

    /// <summary>
    /// Sets the maximum properties used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumProperties(int value)
    {
        maximumProperties = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum components.
    /// </summary>
    /// <returns>The current maximum components.</returns>
    public int GetMaximumComponents()
    {
        return maximumComponents;
    }

    /// <summary>
    /// Sets the maximum components used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumComponents(int value)
    {
        maximumComponents = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum component depth.
    /// </summary>
    /// <returns>The current maximum component depth.</returns>
    public int GetMaximumComponentDepth()
    {
        return maximumComponentDepth;
    }

    /// <summary>
    /// Sets the maximum component depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumComponentDepth(int value)
    {
        maximumComponentDepth = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum parameters per property.
    /// </summary>
    /// <returns>The current maximum parameters per property.</returns>
    public int GetMaximumParametersPerProperty()
    {
        return maximumParametersPerProperty;
    }

    /// <summary>
    /// Sets the maximum parameters per property used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumParametersPerProperty(int value)
    {
        maximumParametersPerProperty = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum parameter values.
    /// </summary>
    /// <returns>The current maximum parameter values.</returns>
    public int GetMaximumParameterValues()
    {
        return maximumParameterValues;
    }

    /// <summary>
    /// Sets the maximum parameter values used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumParameterValues(int value)
    {
        maximumParameterValues = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum traversal depth.
    /// </summary>
    /// <returns>The current maximum traversal depth.</returns>
    public int GetMaximumTraversalDepth()
    {
        return maximumTraversalDepth;
    }

    /// <summary>
    /// Sets the maximum traversal depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumTraversalDepth(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value");
        maximumTraversalDepth = value;
    }

    /// <summary>
    /// Gets the maximum traversal entity count.
    /// </summary>
    /// <returns>The current maximum traversal entity count.</returns>
    public int GetMaximumTraversalEntityCount()
    {
        return maximumTraversalEntityCount;
    }

    /// <summary>
    /// Sets the maximum traversal entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumTraversalEntityCount(int value)
    {
        maximumTraversalEntityCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailStructuredValueParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailStructuredValueParseMode value)
    {
        if (value < EmailStructuredValueParseMode.Strict || value > EmailStructuredValueParseMode.Tolerant)
            throw new ArgumentOutOfRangeException("value");
        mode = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCalendarParseOptions Clone()
    {
        EmailCalendarParseOptions copy = new EmailCalendarParseOptions();
        copy.SetMaximumCalendarBytes(maximumCalendarBytes);
        copy.SetMaximumPhysicalLineBytes(maximumPhysicalLineBytes);
        copy.SetMaximumUnfoldedLineBytes(maximumUnfoldedLineBytes);
        copy.SetMaximumContentLines(maximumContentLines);
        copy.SetMaximumProperties(maximumProperties);
        copy.SetMaximumComponents(maximumComponents);
        copy.SetMaximumComponentDepth(maximumComponentDepth);
        copy.SetMaximumParametersPerProperty(maximumParametersPerProperty);
        copy.SetMaximumParameterValues(maximumParameterValues);
        copy.SetMaximumTraversalDepth(maximumTraversalDepth);
        copy.SetMaximumTraversalEntityCount(maximumTraversalEntityCount);
        copy.SetMode(mode);
        return copy;
    }
}
