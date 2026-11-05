using System;
using System.Threading;

namespace html;
/// <summary>Stores a compiled CSS selector that can match and query HTML subtrees.</summary>
public class HtmlSelector : IHtmlSelector
{
    internal string source;
    internal HtmlSelectorMatchPlan[] plans;
    internal HtmlLimits limits;
    internal HtmlSelector(string sourceValue, HtmlSelectorMatchPlan[] plansValue, HtmlLimits limitsValue)
    {
        this.source = sourceValue;
        this.plans = CopyPlans(plansValue);
        this.limits = limitsValue.Copy();
    }

    /// <summary>Gets the original selector text used to create this compiled selector.</summary>
    /// <returns>The selector source exactly as supplied to compilation.</returns>
    public string GetSource()
    {
        return this.source;
    }

    /// <summary>Gets the specificity of every selector-list branch in source order.</summary>
    /// <returns>New specificity values that the caller may modify without affecting matching.</returns>
    public HtmlSelectorSpecificity[] GetSpecificities()
    {
        HtmlSelectorSpecificity[] result =
            new HtmlSelectorSpecificity[this.plans.Length];
        int index = 0;
        while (index < this.plans.Length)
        {
            HtmlSelectorSpecificity value =
                this.plans[index].GetSpecificity();
            result[index] = new HtmlSelectorSpecificity(
                value.GetIdCount(), value.GetClassCount(),
                value.GetTypeCount());
            index = index + 1;
        }

        return result;
    }

    internal HtmlSelectorMatchPlan[] GetMatchPlans()
    {
        return CopyPlans(this.plans);
    }

    /// <summary>Tests whether the node matches this compiled selector within the supplied scope.</summary>
    /// <param name = "node">The node to test.</param>
    /// <param name = "scope">The root that defines selector scope and traversal bounds.</param>
    /// <returns><c>true</c> when the supplied value matches; otherwise, <c>false</c>.</returns>
    public bool Matches(HtmlNode node, HtmlNode scope)
    {
        return this.MatchesWithCancellation(node, scope, CancellationToken.None);
    }

    /// <summary>Tests whether the node matches this compiled selector within the supplied scope.</summary>
    /// <param name = "node">The node to test.</param>
    /// <param name = "scope">The root that defines selector scope and traversal bounds.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns><c>true</c> when the supplied value matches; otherwise, <c>false</c>.</returns>
    public bool MatchesWithCancellation(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.Matches(this, node, scope, this.limits, cancellationToken);
    }

    /// <summary>Tests a node with operation-specific limits and cancellation.</summary>
    /// <param name="node">The node to test.</param>
    /// <param name="scope">The root defining selector scope.</param>
    /// <param name="limits">The limits enforced only for this operation.</param>
    /// <param name="cancellationToken">The token checked while matching.</param>
    /// <returns><c>true</c> when the node matches; otherwise, <c>false</c>.</returns>
    public bool MatchesWithLimits(HtmlNode node, HtmlNode scope,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.Matches(this, node, scope, limits,
            cancellationToken);
    }

    /// <summary>Finds the closest matching element, including the supplied node.</summary>
    /// <param name="node">The node at which ancestor matching starts.</param>
    /// <returns>The closest matching element. Returns null when no matching element exists on the ancestor chain.</returns>
    public HtmlNode? Closest(HtmlNode node)
    {
        return this.ClosestWithCancellation(node, CancellationToken.None);
    }

    /// <summary>Finds the closest matching element with cancellation.</summary>
    /// <param name="node">The node at which ancestor matching starts.</param>
    /// <param name="cancellationToken">The token checked while matching.</param>
    /// <returns>The closest matching element. Returns null when no matching element exists on the ancestor chain.</returns>
    public HtmlNode? ClosestWithCancellation(HtmlNode node,
        CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.Closest(this, node, this.limits,
            cancellationToken);
    }

    /// <summary>Finds the closest matching element with operation-specific limits and cancellation.</summary>
    /// <param name="node">The node at which ancestor matching starts.</param>
    /// <param name="limits">The limits enforced only for this operation.</param>
    /// <param name="cancellationToken">The token checked while matching.</param>
    /// <returns>The closest matching element. Returns null when no matching element exists on the ancestor chain.</returns>
    public HtmlNode? ClosestWithLimits(HtmlNode node, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.Closest(this, node, limits,
            cancellationToken);
    }

    /// <summary>Finds the first matching node in document order.</summary>
    /// <param name = "root">The root node that bounds the operation.</param>
    /// <returns>The first matching node. Returns null when no node matches.</returns>
    public HtmlNode? FindFirst(HtmlNode root)
    {
        return this.FindFirstWithCancellation(root, CancellationToken.None);
    }

    /// <summary>Finds the first matching node in document order.</summary>
    /// <param name = "root">The root node that bounds the operation.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The first matching node. Returns null when no node matches.</returns>
    public HtmlNode? FindFirstWithCancellation(HtmlNode root, CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.FindFirst(this, root, this.limits, cancellationToken);
    }

    /// <summary>Finds the first match with operation-specific limits and cancellation.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <param name="limits">The limits enforced only for this operation.</param>
    /// <param name="cancellationToken">The token checked while querying.</param>
    /// <returns>The first matching node. Returns null when no node matches.</returns>
    public HtmlNode? FindFirstWithLimits(HtmlNode root, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.FindFirst(this, root, limits,
            cancellationToken);
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "root">The root node that bounds the operation.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAll(HtmlNode root)
    {
        return this.FindAllWithCancellation(root, CancellationToken.None);
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "root">The root node that bounds the operation.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAllWithCancellation(HtmlNode root, CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.FindAll(this, root, this.limits, cancellationToken);
    }

    /// <summary>Finds all matches with operation-specific limits and cancellation.</summary>
    /// <param name="root">The root node that bounds the operation.</param>
    /// <param name="limits">The result and work limits enforced only for this operation.</param>
    /// <param name="cancellationToken">The token checked while querying.</param>
    /// <returns>Distinct matching nodes in document order, bounded by the result limit.</returns>
    public HtmlNode[] FindAllWithLimits(HtmlNode root, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return HtmlSelectorQuery.FindAll(this, root, limits,
            cancellationToken);
    }

    internal HtmlSelectorMatchPlan[] GetMatchPlansStorage()
    {
        return this.plans;
    }

    internal static HtmlSelectorMatchPlan[] CopyPlans(HtmlSelectorMatchPlan[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("plansValue");
        }

        HtmlSelectorMatchPlan[] result = new HtmlSelectorMatchPlan[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A selector match plan cannot be null.", "plansValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
