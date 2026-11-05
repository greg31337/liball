namespace fake;

internal static class ColorData
{
    internal const string Source = "W3C CSS Color 4 named-color factual subset and project-authored conversion implementation";
    internal const string License = "W3C Software and Document License 2023 and project-license";

    internal static readonly string[] Names = new string[]
    {
        "black", "blue", "cyan", "gray", "green", "lime", "magenta",
        "maroon", "navy", "orange", "purple", "red", "silver", "teal",
        "white", "yellow"
    };

    internal static readonly int[] Red = new int[]
    {
        0, 0, 0, 128, 0, 0, 255, 128, 0, 255, 128, 255, 192, 0, 255, 255
    };

    internal static readonly int[] Green = new int[]
    {
        0, 0, 255, 128, 128, 255, 0, 0, 0, 165, 0, 0, 192, 128, 255, 255
    };

    internal static readonly int[] Blue = new int[]
    {
        0, 255, 255, 128, 0, 0, 255, 0, 128, 0, 128, 0, 192, 128, 255, 0
    };
}
