using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlDimensionResult"/>.</summary>
public interface IHtmlDimensionResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the GetValue operation.</summary>
    public double GetValue();
    /// <summary>Executes the GetUnit operation.</summary>
    public HtmlDimensionUnit GetUnit();
    /// <summary>Executes the IsPercentage operation.</summary>
    public bool IsPercentage();
    /// <summary>Gets the consumed input length.</summary>
    public int GetConsumedLength();
}
