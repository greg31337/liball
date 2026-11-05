using System;

namespace template;

/// <summary>
/// Maps a range of rendered output to the template source span that produced it.
/// </summary>
public class TemplateSourceMapEntry
{
    /// <summary>
    /// Stores the zero-based start offset in rendered output.
    /// </summary>
    public int OutputStart;
    /// <summary>
    /// Stores the number of rendered characters in this mapping.
    /// </summary>
    public int OutputLength;
    /// <summary>
    /// Stores the exclusive rendered-output end offset.
    /// </summary>
    public int OutputEnd;
    /// <summary>
    /// Stores the display name of the originating template.
    /// </summary>
    public string TemplateName;
    /// <summary>
    /// Stores the originating template source span.
    /// </summary>
    public TemplateSourceSpan SourceSpan;

    /// <summary>
    /// Initializes a source-map entry and validates its output range and source span.
    /// </summary>
    /// <param name="outputStart">The zero-based rendered-output start offset.</param>
    /// <param name="outputLength">The number of rendered characters.</param>
    /// <param name="sourceSpan">The template source span that produced the output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>sourceSpan</c> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an output range value is negative or overflows.</exception>
    public TemplateSourceMapEntry(int outputStart, int outputLength, TemplateSourceSpan sourceSpan)
    {
        if (outputStart < 0)
        {
            throw new ArgumentOutOfRangeException("outputStart");
        }

        if (outputLength < 0)
        {
            throw new ArgumentOutOfRangeException("outputLength");
        }

        if (outputLength > 2147483647 - outputStart)
        {
            throw new ArgumentOutOfRangeException("outputLength");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        OutputStart = outputStart;
        OutputLength = outputLength;
        OutputEnd = outputStart + outputLength;
        TemplateName = sourceSpan.Source.GetDisplayName();
        SourceSpan = sourceSpan;
    }
}
