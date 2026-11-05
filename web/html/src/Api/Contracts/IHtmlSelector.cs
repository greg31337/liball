using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSelector"/>.</summary>
public interface IHtmlSelector
{
    /// <summary>Executes the GetSource operation.</summary>
    public string GetSource();
    /// <summary>Gets selector-list specificity values in source order.</summary>
    public HtmlSelectorSpecificity[] GetSpecificities();
    /// <summary>Executes the Matches operation.</summary>
    public bool Matches(HtmlNode node, HtmlNode scope);
    /// <summary>Executes the MatchesWithCancellation operation.</summary>
    public bool MatchesWithCancellation(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken);
    /// <summary>Matches with operation-specific limits and cancellation.</summary>
    public bool MatchesWithLimits(HtmlNode node, HtmlNode scope, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Finds the closest matching element.</summary>
    public HtmlNode? Closest(HtmlNode node);
    /// <summary>Finds the closest matching element with cancellation.</summary>
    public HtmlNode? ClosestWithCancellation(HtmlNode node, CancellationToken cancellationToken);
    /// <summary>Finds the closest matching element with operation-specific limits and cancellation.</summary>
    public HtmlNode? ClosestWithLimits(HtmlNode node, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the FindFirst operation.</summary>
    public HtmlNode? FindFirst(HtmlNode root);
    /// <summary>Executes the FindFirstWithCancellation operation.</summary>
    public HtmlNode? FindFirstWithCancellation(HtmlNode root, CancellationToken cancellationToken);
    /// <summary>Finds the first match with operation-specific limits and cancellation.</summary>
    public HtmlNode? FindFirstWithLimits(HtmlNode root, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the FindAll operation.</summary>
    public HtmlNode[] FindAll(HtmlNode root);
    /// <summary>Executes the FindAllWithCancellation operation.</summary>
    public HtmlNode[] FindAllWithCancellation(HtmlNode root, CancellationToken cancellationToken);
    /// <summary>Finds all matches with operation-specific limits and cancellation.</summary>
    public HtmlNode[] FindAllWithLimits(HtmlNode root, HtmlLimits limits, CancellationToken cancellationToken);
}
