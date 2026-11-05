namespace fake;

internal static class AirlineData
{
    internal const string Source = "Project-authored fictional aviation fixtures and generic cabin vocabulary";
    internal const string License = "project-license";

    internal static readonly string[] AirlineNames = new string[]
    {
        "Example Aster Air",
        "Fixture Beacon Airways",
        "Sample Calder Regional",
        "Test Meridian Flights",
        "Demo Northstar Air",
        "Mock Pine Aviation"
    };

    internal static readonly string[] AirlineCodes = new string[]
    {
        "X1", "X2", "X3", "X4", "X5", "X6"
    };

    internal static readonly string[] AirportNames = new string[]
    {
        "Example City Airport",
        "Sample Bay Airport",
        "Fixture Field",
        "North Test Airport",
        "South Demo Airport",
        "Harbor Example Airport",
        "Cedar Sample Airport",
        "River Fixture Airport"
    };

    internal static readonly string[] AirportCodes = new string[]
    {
        "QAA", "QBB", "QCC", "QDD", "QEE", "QFF", "QGG", "QHH"
    };

    internal static readonly string[] AircraftNames = new string[]
    {
        "Example Aster 100",
        "Fixture Beacon 200",
        "Sample Calder Regional 50",
        "Test Meridian Twinjet",
        "Demo Northstar Turboprop",
        "Mock Pine Shuttle"
    };

    internal static readonly string[] AircraftCodes = new string[]
    {
        "X100", "X200", "X050", "X300", "X400", "X500"
    };

    internal static readonly string[] Cabins = new string[]
    {
        "economy", "premium economy", "business", "first"
    };
}
