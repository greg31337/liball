using System;

namespace html;
/// <summary>Describes a half-open range in normalized and original HTML input.</summary>
/// <remarks>A known span includes its start and excludes its end. Unknown spans retain unknown endpoint objects and report <c>-1</c> for every length.</remarks>
public class HtmlSourceSpan : IHtmlSourceSpan
{
    internal HtmlSourcePosition start;
    internal HtmlSourcePosition end;
    internal bool known;
    internal HtmlSourceSpan(HtmlSourcePosition start, HtmlSourcePosition end, bool known)
    {
        this.start = start;
        this.end = end;
        this.known = known;
    }

    /// <summary>Creates a known half-open source span from ordered start and end positions.</summary>
    /// <param name = "start">The inclusive start position.</param>
    /// <param name = "end">The exclusive end position.</param>
    /// <returns>A half-open source span from <c>start</c>, inclusive, to <c>end</c>, exclusive.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    internal static HtmlSourceSpan Create(HtmlSourcePosition start, HtmlSourcePosition end)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new ArgumentNullException("end");
        }

        if (!start.IsKnown() || !end.IsKnown())
        {
            throw new ArgumentException("A known source span requires two known positions.");
        }

        if (end.GetNormalizedCharacterOffset() < start.GetNormalizedCharacterOffset())
        {
            throw new ArgumentException("The end of a source span cannot precede its start.");
        }

        if (end.GetCharacterOffset() < start.GetCharacterOffset())
        {
            throw new ArgumentException("The original end offset cannot precede the original start offset.");
        }

        if (end.GetLine() < start.GetLine())
        {
            throw new ArgumentException("The end line cannot precede the start line.");
        }

        if (end.GetLine() == start.GetLine() && end.GetColumn() < start.GetColumn())
        {
            throw new ArgumentException("The end column cannot precede the start column on the same line.");
        }

        if (start.HasByteOffset() != end.HasByteOffset())
        {
            throw new ArgumentException("Both source-span positions must agree on byte-offset availability.");
        }

        if (start.HasByteOffset() && end.GetByteOffset() < start.GetByteOffset())
        {
            throw new ArgumentException("The end byte offset cannot precede the start byte offset.");
        }

        return new HtmlSourceSpan(start, end, true);
    }

    /// <summary>Creates a value that explicitly represents an unknown source location.</summary>
    /// <returns>An explicitly unknown source span.</returns>
    internal static HtmlSourceSpan CreateUnknown()
    {
        return new HtmlSourceSpan(HtmlSourcePosition.CreateUnknown(), HtmlSourcePosition.CreateUnknown(), false);
    }

    /// <summary>Reports whether source location information is available.</summary>
    /// <returns><c>true</c> when source location information is available; otherwise, <c>false</c>.</returns>
    public bool IsKnown()
    {
        return this.known;
    }

    /// <summary>Gets the inclusive start position.</summary>
    /// <returns>The known start position, or an unknown position when this span is unknown.</returns>
    public HtmlSourcePosition GetStart()
    {
        return this.start;
    }

    /// <summary>Gets the exclusive end position.</summary>
    /// <returns>The known end position, or an unknown position when this span is unknown.</returns>
    public HtmlSourcePosition GetEnd()
    {
        return this.end;
    }

    /// <summary>Gets the span length after HTML input normalization.</summary>
    /// <returns>The normalized character length, or <c>-1</c> when this span is unknown.</returns>
    public int GetNormalizedCharacterLength()
    {
        if (!this.known)
        {
            return -1;
        }

        return this.end.GetNormalizedCharacterOffset() - this.start.GetNormalizedCharacterOffset();
    }

    /// <summary>Gets the span length in the caller's original character input.</summary>
    /// <returns>The original character length, or <c>-1</c> when this span is unknown.</returns>
    public int GetCharacterLength()
    {
        if (!this.known)
        {
            return -1;
        }

        return this.end.GetCharacterOffset() - this.start.GetCharacterOffset();
    }

    /// <summary>Gets the span length in the caller's original byte input.</summary>
    /// <returns>The original byte length, or <c>-1</c> when this span is unknown or has no byte mapping.</returns>
    public long GetByteLength()
    {
        if (!this.known || !this.start.HasByteOffset())
        {
            return -1;
        }

        return this.end.GetByteOffset() - this.start.GetByteOffset();
    }
}
