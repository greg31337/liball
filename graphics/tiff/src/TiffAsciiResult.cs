namespace tiff;

internal class TiffAsciiResult
{
    internal bool Success;
    internal string Text;
    internal string[] Values;
    internal TiffDiagnostics Diagnostics;

    internal TiffAsciiResult()
    {
        Success = false;
        Text = "";
        Values = new string[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffAsciiResult Create(string[] values, TiffDiagnostics diagnostics)
    {
        TiffAsciiResult value = new TiffAsciiResult();
        value.Initialize(values, diagnostics);
        return value;
    }

    internal void Initialize(string[] values, TiffDiagnostics diagnostics)
    {
        Values = TiffAscii.CopyStrings(values);
        Text = TiffAscii.JoinStrings(Values);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
