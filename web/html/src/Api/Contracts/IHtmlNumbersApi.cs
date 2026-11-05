using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines instance-based access to the HtmlNumbers operations.</summary>
public interface IHtmlNumbersApi
{
    /// <summary>Executes the ParseInteger operation.</summary>
    public HtmlIntegerResult ParseInteger(string value);
    /// <summary>Executes the ParseNonNegativeInteger operation.</summary>
    public HtmlIntegerResult ParseNonNegativeInteger(string value);
    /// <summary>Executes the ParseDimension operation.</summary>
    public HtmlDimensionResult ParseDimension(string value);
    /// <summary>Executes the ParseColumnSpan operation.</summary>
    public int ParseColumnSpan(string value);
    /// <summary>Executes the ParseRowSpan operation.</summary>
    public int ParseRowSpan(string value);
    /// <summary>Parses a legacy table alignment keyword.</summary>
    public HtmlTableAlignment ParseTableAlignment(string value);
    /// <summary>Gets alignment from a table-related element.</summary>
    public HtmlTableAlignment GetTableAlignment(HtmlNode element);
    /// <summary>Executes the GetOrderedListStart operation.</summary>
    public int GetOrderedListStart(HtmlNode orderedList);
    /// <summary>Executes the GetOrderedListOrdinals operation.</summary>
    public int[] GetOrderedListOrdinals(HtmlNode orderedList);
    /// <summary>Executes the GetOrderedListMarkerStyle operation.</summary>
    public HtmlOrderedListMarkerStyle GetOrderedListMarkerStyle(HtmlNode orderedList);
}
