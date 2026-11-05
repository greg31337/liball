namespace tiff;

internal class TiffRationalCodecResult
{
    internal bool Success;
    internal TiffRational[] Values;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffRationalCodecResult()
    {
        Success = false;
        Values = new TiffRational[0];
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRationalCodecResult Create(TiffRational[] values, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffRationalCodecResult value = new TiffRationalCodecResult();
        value.Initialize(values, bytes, diagnostics);
        return value;
    }

    internal void Initialize(TiffRational[] values, byte[] bytes, TiffDiagnostics diagnostics)
    {
        Values = TiffTagValue.CopyRationalArray(values);
        Bytes = TiffTagValue.CopyByteArray(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
