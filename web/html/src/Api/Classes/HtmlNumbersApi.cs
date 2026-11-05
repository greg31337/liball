using System;
using System.Collections.Generic;

namespace html;
/// <summary>Provides instance-based access to the HtmlNumbers operations.</summary>
public class HtmlNumbersApi : IHtmlNumbersApi
{
    /// <summary>Executes the ParseInteger operation.</summary>
    public HtmlIntegerResult ParseInteger(string value)
    {
        return HtmlNumbers.ParseInteger(value);
    }

    /// <summary>Executes the ParseNonNegativeInteger operation.</summary>
    public HtmlIntegerResult ParseNonNegativeInteger(string value)
    {
        return HtmlNumbers.ParseNonNegativeInteger(value);
    }

    /// <summary>Executes the ParseDimension operation.</summary>
    public HtmlDimensionResult ParseDimension(string value)
    {
        return HtmlNumbers.ParseDimension(value);
    }

    /// <summary>Executes the ParseColumnSpan operation.</summary>
    public int ParseColumnSpan(string value)
    {
        return HtmlNumbers.ParseColumnSpan(value);
    }

    /// <summary>Executes the ParseRowSpan operation.</summary>
    public int ParseRowSpan(string value)
    {
        return HtmlNumbers.ParseRowSpan(value);
    }

    /// <summary>Parses a legacy table alignment keyword.</summary>
    public HtmlTableAlignment ParseTableAlignment(string value)
    {
        return HtmlNumbers.ParseTableAlignment(value);
    }

    /// <summary>Gets alignment from a table-related element.</summary>
    public HtmlTableAlignment GetTableAlignment(HtmlNode element)
    {
        return HtmlNumbers.GetTableAlignment(element);
    }

    /// <summary>Executes the GetOrderedListStart operation.</summary>
    public int GetOrderedListStart(HtmlNode orderedList)
    {
        return HtmlNumbers.GetOrderedListStart(orderedList);
    }

    /// <summary>Executes the GetOrderedListOrdinals operation.</summary>
    public int[] GetOrderedListOrdinals(HtmlNode orderedList)
    {
        return HtmlNumbers.GetOrderedListOrdinals(orderedList);
    }

    /// <summary>Executes the GetOrderedListMarkerStyle operation.</summary>
    public HtmlOrderedListMarkerStyle GetOrderedListMarkerStyle(HtmlNode orderedList)
    {
        return HtmlNumbers.GetOrderedListMarkerStyle(orderedList);
    }
}
