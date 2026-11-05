namespace tiff;

internal class TiffImageInfoResult
{
    internal bool Success;
    internal TiffImageInfo Info;
    internal TiffDiagnostics Diagnostics;

    internal TiffImageInfoResult()
    {
        Info = new TiffImageInfo();
        Diagnostics = new TiffDiagnostics();
        Success = true;
    }

    internal static TiffImageInfoResult Create(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        TiffImageInfoResult value = new TiffImageInfoResult();
        value.Initialize(info, diagnostics);
        return value;
    }

    internal void Initialize(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            Info = new TiffImageInfo();
        }
        else
        {
            Info = info.Copy();
        }

        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }
}
