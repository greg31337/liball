using System;

namespace pdf;

/// <summary>Stores one packing-slip item.</summary>
public class PdfPackingSlipItem
{
    /// <summary>Stores the stock-keeping unit.</summary>
    public string Sku = string.Empty;
    /// <summary>Stores the item description.</summary>
    public string Description = string.Empty;
    /// <summary>Stores the shipped quantity.</summary>
    public int Quantity;

    /// <summary>Initializes a packing-slip item.</summary>
    /// <param name="sku">The stock-keeping unit.</param>
    /// <param name="description">The item description.</param>
    /// <param name="quantity">The positive shipped quantity.</param>
    public PdfPackingSlipItem(string sku, string description, int quantity)
    {
        if (sku == null) { throw new ArgumentNullException("sku"); }
        if (description == null) { throw new ArgumentNullException("description"); }
        if (quantity <= 0) { throw new ArgumentOutOfRangeException("quantity", "Packing-slip quantity must be positive."); }
        Sku = sku;
        Description = description;
        Quantity = quantity;
    }
}
