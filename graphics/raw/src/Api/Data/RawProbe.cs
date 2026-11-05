namespace raw;

/// <summary>Describes content-based raw format identification and its diagnostics.</summary>
public class RawProbe
{
    /// <summary>The uniquely detected format, or <see cref="RawFormat.Unknown"/>.</summary>
    public RawFormat Format;
    /// <summary>A deterministic confidence score from zero through one hundred.</summary>
    public int Confidence;
    /// <summary>All formats supported by the observed evidence in preferred order.</summary>
    public RawFormat[] Candidates;
    /// <summary>Diagnostics produced by ambiguity, unknown content, or caller-hint mismatch.</summary>
    public RawDiagnostic[] Diagnostics;
    /// <summary>Whether generic raw interpretation requires a caller-provided descriptor.</summary>
    public bool GenericDescriptorRequired;
    /// <summary>Whether a TIFF-derived byte order was recognized.</summary>
    public bool ByteOrderKnown;
    /// <summary>The recognized TIFF-derived byte order when <see cref="ByteOrderKnown"/> is true.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>Whether the recognized TIFF-derived container uses BigTIFF structures.</summary>
    public bool BigTiff;

    /// <summary>Creates a format probe result.</summary>
    /// <param name="format">The uniquely detected format.</param>
    /// <param name="confidence">The confidence score from zero through one hundred.</param>
    /// <param name="candidates">The supported candidates in preferred order.</param>
    /// <param name="diagnostics">Probe diagnostics.</param>
    /// <param name="genericDescriptorRequired">Whether a generic descriptor is required.</param>
    /// <param name="byteOrderKnown">Whether TIFF byte order was recognized.</param>
    /// <param name="byteOrder">The recognized TIFF byte order.</param>
    /// <param name="bigTiff">Whether BigTIFF structures were recognized.</param>
    public RawProbe(RawFormat format, int confidence, RawFormat[] candidates, RawDiagnostic[] diagnostics, bool genericDescriptorRequired, bool byteOrderKnown, RawByteOrder byteOrder, bool bigTiff)
    {
        Format = format;
        Confidence = confidence;
        Candidates = candidates == null ? System.Array.Empty<RawFormat>() : candidates;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
        GenericDescriptorRequired = genericDescriptorRequired;
        ByteOrderKnown = byteOrderKnown;
        ByteOrder = byteOrder;
        BigTiff = bigTiff;
    }

    /// <summary>Returns whether more than one candidate remains without a unique detection.</summary>
    /// <returns><c>true</c> when multiple candidates remain and <see cref="Format"/> is unknown; otherwise <c>false</c>.</returns>
    internal bool IsAmbiguous()
    {
        return Format == RawFormat.Unknown && Candidates.Length > 1;
    }

    /// <summary>Returns whether the probe produced no error diagnostic.</summary>
    /// <returns><c>true</c> when no error is present; otherwise <c>false</c>.</returns>
    internal bool Succeeded()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            RawDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic != null && diagnostic.Severity == RawSeverity.Error)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
