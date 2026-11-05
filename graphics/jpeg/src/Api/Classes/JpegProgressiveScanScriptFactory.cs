namespace jpeg;

/// <summary>Default factory for standard progressive JPEG scan scripts.</summary>
public class JpegProgressiveScanScriptFactory : IJpegProgressiveScanScriptFactory
{
    /// <summary>Creates an independent progressive scan-script factory.</summary>
    public JpegProgressiveScanScriptFactory()
    {
    }

    /// <summary>Creates one interleaved DC-first scan followed by one full-band AC-first scan per frame component.</summary>
    /// <param name="frame">The progressive frame whose component identifiers are selected.</param>
    /// <returns>A newly allocated standard progressive scan script.</returns>
    public JpegProgressiveScanScript CreateStandardScript(JpegFrameHeader frame)
    {
        JpegProgressiveScanScript script = new JpegProgressiveScanScript();
        script.Scans = new JpegProgressiveSof2Encoder().CreateDefaultScanScript(frame);
        return script;
    }
}
