namespace fake;

/// <summary>Represents an explicitly synthetic product-review fixture.</summary>
/// <remarks>The text is project-authored and does not describe a real purchase or product experience.</remarks>
public class ProductReview
{
    /// <summary>The integer rating from one through five.</summary>
    public int Rating;

    /// <summary>The project-authored title correlated with <c>Rating</c>.</summary>
    public string Title = string.Empty;

    /// <summary>The project-authored review text correlated with <c>Rating</c>.</summary>
    public string Text = string.Empty;

    /// <summary>Whether the review is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the review represents a verified purchase, always <c>false</c>.</summary>
    public bool RepresentsVerifiedPurchase;

    internal ProductReview()
    {
    }
}
