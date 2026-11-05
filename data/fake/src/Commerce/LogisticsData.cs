namespace fake;

internal static class LogisticsData
{
    internal const string Source = "Original project-authored logistics vocabulary";
    internal const string License = "project-license";

    internal static readonly string[] Carriers = new string[]
    {
        "Cedar Parcel", "Harbor Dispatch", "Northbridge Freight",
        "Summit Courier"
    };

    internal static readonly string[] Methods = new string[]
    {
        "Economy Ground", "Standard Ground", "Two-Day", "Next-Day"
    };

    internal static readonly int[] MinimumTransitDays = new int[]
    {
        5, 3, 2, 1
    };

    internal static readonly int[] MaximumTransitDays = new int[]
    {
        8, 5, 2, 1
    };

    internal static readonly string[] Warehouses = new string[]
    {
        "Cedar Depot", "Harbor Storehouse", "Northbridge Warehouse",
        "Silver Meadow Depot", "Summit Distribution"
    };

    internal static readonly string[] PackageTypes = new string[]
    {
        "Box", "Crate", "Envelope", "Tube"
    };
}
