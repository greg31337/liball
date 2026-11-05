using System;
using System.Collections.Generic;

namespace html;
/// <summary>Reports whether an HTML integer parsed and provides the parsed value.</summary>
public class HtmlIntegerResult : IHtmlIntegerResult
{
    internal bool success;
    internal bool overflow;
    internal int value;
    internal int consumedLength;
    internal HtmlIntegerResult(bool successValue, bool overflowValue,
        int valueValue, int consumedLengthValue)
    {
        this.success = successValue;
        this.overflow = overflowValue;
        this.value = valueValue;
        this.consumedLength = consumedLengthValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Reports whether the numeric input exceeded the supported integer range.</summary>
    /// <returns><c>true</c> when the value overflowed the supported range; otherwise, <c>false</c>.</returns>
    public bool WasOverflow()
    {
        return this.overflow;
    }

    /// <summary>Gets the parsed integer.</summary>
    /// <returns>The integer value when parsing succeeded; otherwise, zero.</returns>
    public int GetValue()
    {
        return this.value;
    }

    /// <summary>Gets how many leading input characters were consumed.</summary>
    /// <returns>The consumed character count, including leading HTML whitespace.</returns>
    public int GetConsumedLength()
    {
        return this.consumedLength;
    }
}
