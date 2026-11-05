namespace fake;

internal static class ScienceData
{
    internal const string Source = "Project-authored science fixtures with SI facts from the BIPM SI Brochure, ninth edition, updated 2026";
    internal const string License = "project-license and CC-BY-4.0";

    internal static readonly string[] Fields = new string[]
    {
        "astronomy", "biology", "chemistry", "geology", "meteorology",
        "physics"
    };

    internal static readonly string[] ExperimentSubjects = new string[]
    {
        "orbital sample", "growth sample", "reaction sample",
        "mineral sample", "weather sample", "motion sample"
    };

    internal static readonly string[] Elements = new string[]
    {
        "hydrogen", "carbon", "nitrogen", "oxygen", "silicon", "iron"
    };

    internal static readonly string[] ElementSymbols = new string[]
    {
        "H", "C", "N", "O", "Si", "Fe"
    };

    internal static readonly int[] AtomicNumbers = new int[]
    {
        1, 6, 7, 8, 14, 26
    };

    internal static readonly string[] Quantities = new string[]
    {
        "length", "mass", "time", "volume", "power", "voltage"
    };

    internal static readonly string[] Units = new string[]
    {
        "metre", "gram", "second", "litre", "watt", "volt"
    };

    internal static readonly string[] UnitSymbols = new string[]
    {
        "m", "g", "s", "L", "W", "V"
    };

    internal static readonly string[] SiPrefixes = new string[]
    {
        "nano", "micro", "milli", "centi", "kilo", "mega", "giga"
    };

    internal static readonly string[] SiPrefixSymbols = new string[]
    {
        "n", "μ", "m", "c", "k", "M", "G"
    };

    internal static readonly int[] SiPrefixExponents = new int[]
    {
        -9, -6, -3, -2, 3, 6, 9
    };

    internal static readonly string[] Planets = new string[]
    {
        "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn",
        "Uranus", "Neptune"
    };

    internal static readonly string[] SpaceTerms = new string[]
    {
        "asteroid", "comet", "galaxy", "moon", "nebula", "orbit", "star"
    };

    internal static readonly string[] ExperimentActions = new string[]
    {
        "Compare", "Measure", "Observe", "Record", "Simulate", "Test"
    };
}
