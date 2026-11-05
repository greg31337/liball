namespace fake;

internal static class ProductData
{
    internal const string Source = "Original project-authored product vocabulary";
    internal const string License = "project-license";

    internal static readonly string[] Components = new string[]
    {
        "Basket", "Bottle", "Case", "Lamp", "Notebook", "Organizer",
        "Stand", "Tray"
    };

    internal static readonly string[] Adjectives = new string[]
    {
        "Compact", "Durable", "Lightweight", "Modular", "Portable",
        "Practical", "Stackable", "Versatile"
    };

    internal static readonly string[] Categories = new string[]
    {
        "desk accessories", "home organization", "kitchen tools",
        "office supplies", "outdoor accessories", "storage supplies",
        "travel accessories", "workshop supplies"
    };

    internal static readonly string[] Brands = new string[]
    {
        "Cedar & Finch", "Harbor Plain", "Lantern Field", "Northbridge Goods",
        "Silver Meadow", "Stone & Grove", "Summit Workshop", "Willow Current"
    };

    internal static readonly string[] Materials = new string[]
    {
        "Aluminum", "Bamboo", "Canvas", "Ceramic", "Cork",
        "Recycled Plastic", "Stainless Steel", "Wood"
    };

    internal static readonly string[] Colors = new string[]
    {
        "Amber", "Charcoal", "Forest Green", "Ivory", "Navy", "Slate",
        "Terracotta", "White"
    };

    internal static readonly string[] ReviewTitles = new string[]
    {
        "Needs improvement", "Below expectations", "Met expectations",
        "Worked well", "Excellent for my needs"
    };

    internal static readonly string[] ReviewTexts = new string[]
    {
        "The fixture was not a good match for my example scenario.",
        "I would choose a different option for this test case.",
        "The fixture covered some needs but left room for improvement.",
        "It was usable, though another option may suit the scenario better.",
        "The fixture matched the basic expectations of my sample scenario.",
        "It provided a straightforward result for an ordinary test case.",
        "The fixture worked well for the example scenario I had in mind.",
        "It was a practical choice for this synthetic use case.",
        "The fixture was an excellent match for my example scenario.",
        "It handled the synthetic use case exactly as expected."
    };

    internal static readonly string[] PromotionNames = new string[]
    {
        "Catalog test discount", "Fixture bundle savings", "Sample seasonal offer",
        "Synthetic customer offer", "Test checkout promotion"
    };

    internal static readonly string[] SubscriptionPlanNames = new string[]
    {
        "Starter", "Standard", "Expanded", "Complete"
    };

    internal static readonly string[] ReturnReasons = new string[]
    {
        "Changed preference", "Different size needed", "No longer needed",
        "Packaging was damaged", "Product did not fit", "Wrong item selected"
    };

    internal static readonly string[] RefundReasons = new string[]
    {
        "Billing correction requested", "Duplicate order placed", "Item was returned",
        "Order was cancelled", "Shipment did not arrive", "Wrong item received"
    };
}
