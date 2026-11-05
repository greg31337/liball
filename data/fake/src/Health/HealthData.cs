namespace fake;

internal static class HealthData
{
    internal const string Source = "Project-authored non-clinical health-shaped fixtures and generic anatomy vocabulary";
    internal const string License = "project-license";

    internal static readonly string[] BloodTypes = new string[]
    {
        "fixture A+", "fixture A-", "fixture B+", "fixture B-",
        "fixture AB+", "fixture AB-", "fixture O+", "fixture O-"
    };

    internal static readonly string[] BodyParts = new string[]
    {
        "bone", "brain", "heart", "kidney", "lung", "muscle", "skin",
        "stomach"
    };

    internal static readonly string[] BodySystems = new string[]
    {
        "skeletal", "nervous", "circulatory", "urinary", "respiratory",
        "muscular", "integumentary", "digestive"
    };

    internal static readonly string[] Symptoms = new string[]
    {
        "sample ache", "sample cough", "sample fatigue", "sample nausea",
        "sample soreness", "sample stiffness"
    };

    internal static readonly string[] MedicineForms = new string[]
    {
        "capsule-shaped", "cream-shaped", "inhaler-shaped", "liquid-shaped",
        "patch-shaped", "tablet-shaped"
    };

    internal static readonly string[] ProviderNames = new string[]
    {
        "Avery Example", "Casey Fixture", "Jordan Sample", "Morgan Test",
        "Riley Demo", "Taylor Mock"
    };

    internal static readonly string[] Facilities = new string[]
    {
        "Example Test Clinic",
        "Fixture Sample Center",
        "Sample Demo Practice",
        "Test Example Facility",
        "Demo Fixture Center",
        "Mock Sample Clinic"
    };
}
