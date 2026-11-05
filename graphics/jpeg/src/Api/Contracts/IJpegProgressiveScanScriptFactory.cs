namespace jpeg;

/// <summary>Creates standard progressive JPEG scan scripts for explicit frame definitions.</summary>
public interface IJpegProgressiveScanScriptFactory
{
    /// <summary>Creates one interleaved DC-first scan followed by one full-band AC-first scan per frame component.</summary>
    /// <param name="frame">The progressive frame whose component identifiers are selected.</param>
    /// <returns>A newly allocated standard progressive scan script.</returns>
    JpegProgressiveScanScript CreateStandardScript(JpegFrameHeader frame);
}
