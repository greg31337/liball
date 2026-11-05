using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlIntegerResult"/>.</summary>
public interface IHtmlIntegerResult
{
    /// <summary>Executes the IsSuccess operation.</summary>
    public bool IsSuccess();
    /// <summary>Executes the WasOverflow operation.</summary>
    public bool WasOverflow();
    /// <summary>Executes the GetValue operation.</summary>
    public int GetValue();
    /// <summary>Gets the consumed input length.</summary>
    public int GetConsumedLength();
}
