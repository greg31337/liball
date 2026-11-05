namespace fake;

/// <summary>Represents an explicitly synthetic promotion and coupon fixture.</summary>
/// <remarks>The coupon uses a visible test prefix and is not redeemable.</remarks>
public class PromotionRecord
{
    /// <summary>The project-authored promotion name.</summary>
    public string Name = string.Empty;

    /// <summary>The coupon-shaped value beginning with <c>TEST</c>.</summary>
    public string CouponCode = string.Empty;

    /// <summary>The whole-number discount percentage from five through fifty.</summary>
    public int DiscountPercent;

    /// <summary>Whether the promotion is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the coupon represents a redeemable offer, always <c>false</c>.</summary>
    public bool RepresentsRedeemableOffer;

    internal PromotionRecord()
    {
    }
}
