using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSrcsetCandidate"/>.</summary>
public interface IHtmlSrcsetCandidate
{
    /// <summary>Executes the GetUrl operation.</summary>
    public string GetUrl();
    /// <summary>Executes the HasWidth operation.</summary>
    public bool HasWidth();
    /// <summary>Executes the GetWidth operation.</summary>
    public int GetWidth();
    /// <summary>Executes the HasDensity operation.</summary>
    public bool HasDensity();
    /// <summary>Executes the GetDensity operation.</summary>
    public double GetDensity();
}
