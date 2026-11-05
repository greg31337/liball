using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlUrlSchemeResult"/>.</summary>
public interface IHtmlUrlSchemeResult
{
    /// <summary>Executes the GetDeobfuscatedValue operation.</summary>
    public string GetDeobfuscatedValue();
    /// <summary>Executes the HasScheme operation.</summary>
    public bool HasScheme();
    /// <summary>Executes the GetScheme operation.</summary>
    public string GetScheme();
    /// <summary>Executes the WasDeobfuscated operation.</summary>
    public bool WasDeobfuscated();
    /// <summary>Executes the IsComplete operation.</summary>
    public bool IsComplete();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
}
