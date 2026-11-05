namespace fake;

/// <summary>Represents a correlated, explicitly synthetic product fixture.</summary>
/// <remarks>The profile does not identify a real product, allocated identifier, market price, inventory holding, or commercial offer.</remarks>
public class ProductProfile
{
    /// <summary>The generic product component.</summary>
    public string Component = string.Empty;

    /// <summary>The adjective used in <c>Name</c> and <c>Description</c>.</summary>
    public string Adjective = string.Empty;

    /// <summary>The product name composed from adjective, material, and component.</summary>
    public string Name = string.Empty;

    /// <summary>The generic product category used in <c>Description</c>.</summary>
    public string Category = string.Empty;

    /// <summary>The original fictional brand name.</summary>
    public string Brand = string.Empty;

    /// <summary>The generic composed product description.</summary>
    public string Description = string.Empty;

    /// <summary>The generic product material.</summary>
    public string Material = string.Empty;

    /// <summary>The generic product color.</summary>
    public string Color = string.Empty;

    /// <summary>The synthetic SKU shape.</summary>
    public string Sku = string.Empty;

    /// <summary>The checksum-valid but unallocated GTIN-14 shape.</summary>
    public string Gtin = string.Empty;

    /// <summary>The rectangular dimensions in centimeters.</summary>
    public ProductDimensions Dimensions = new ProductDimensions();

    /// <summary>The weight in kilograms.</summary>
    public decimal Weight;

    /// <summary>The numeric fixture price.</summary>
    public decimal Price;

    /// <summary>The currency code associated with <c>Price</c>.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The synthetic inventory quantity.</summary>
    public int Inventory;

    /// <summary>Whether the profile is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the profile claims a real product exists, always <c>false</c>.</summary>
    public bool RepresentsRealProduct;

    /// <summary>Whether the identifiers claim allocation, always <c>false</c>.</summary>
    public bool IdentifiersAreAllocated;

    /// <summary>Whether the price claims to represent a market offer, always <c>false</c>.</summary>
    public bool RepresentsMarketPrice;

    internal ProductProfile()
    {
    }
}
