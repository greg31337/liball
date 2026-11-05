using System;
using System.Collections.Generic;

namespace html;
/// <summary>Reports whether an HTML dimension parsed and provides its value and unit.</summary>
public class HtmlDimensionResult : IHtmlDimensionResult
{
    internal bool success;
    internal double value;
    internal HtmlDimensionUnit unit;
    internal int consumedLength;
    internal HtmlDimensionResult(bool successValue, double valueValue,
        HtmlDimensionUnit unitValue, int consumedLengthValue)
    {
        this.success = successValue;
        this.value = valueValue;
        this.unit = unitValue;
        this.consumedLength = consumedLengthValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Gets the parsed numeric magnitude.</summary>
    /// <returns>The numeric value when parsing succeeded; otherwise, zero.</returns>
    public double GetValue()
    {
        return this.value;
    }

    /// <summary>Gets the unit suffix recognized after the numeric value.</summary>
    /// <returns>Number, pixels, or percentage when parsing succeeded.</returns>
    public HtmlDimensionUnit GetUnit()
    {
        return this.unit;
    }

    /// <summary>Reports whether the parsed dimension uses a percentage unit.</summary>
    /// <returns><c>true</c> when the unit is a percentage; otherwise, <c>false</c>.</returns>
    public bool IsPercentage()
    {
        return this.unit == HtmlDimensionUnit.Percentage;
    }

    /// <summary>Gets how many leading input characters were consumed.</summary>
    /// <returns>The consumed character count, including leading HTML whitespace.</returns>
    public int GetConsumedLength()
    {
        return this.consumedLength;
    }
}
