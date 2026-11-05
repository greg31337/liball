namespace asn1;

/// <summary>
/// Contains one named ASN.1 source text supplied to a compilation.
/// </summary>
/// <remarks>
/// The name identifies the source within a multi-file compilation. Text offsets are zero-based UTF-16 code-unit offsets.
/// </remarks>
public class AsnSourceUnit
{
    /// <summary>
    /// Contains the source name used by positions and diagnostics.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the source text.
    /// </summary>
    public string Text = "";

    internal AsnSourcePosition GetPosition(int offset)
    {
        if (offset < 0 || offset > this.Text.Length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (offset > 0 && offset < this.Text.Length
            && char.IsHighSurrogate(this.Text[offset - 1])
            && char.IsLowSurrogate(this.Text[offset]))
        {
            throw new System.ArgumentException("The offset must be on a Unicode scalar boundary.", "offset");
        }

        int line = 1;
        int column = 1;
        int index = 0;

        while (index < offset)
        {
            char value = this.Text[index];
            if (value == '\r')
            {
                if (index + 1 < offset && this.Text[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }

                line = line + 1;
                column = 1;
                continue;
            }

            if (value == '\n')
            {
                index = index + 1;
                line = line + 1;
                column = 1;
                continue;
            }

            if (char.IsHighSurrogate(value)
                && index + 1 < offset
                && char.IsLowSurrogate(this.Text[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            column = column + 1;
        }

        AsnSourcePosition position = new AsnSourcePosition();
        position.SourceName = this.Name;
        position.Offset = offset;
        position.Line = line;
        position.Column = column;
        return position;
    }

    internal AsnSourceSpan GetSpan(int startOffset, int endOffset)
    {
        if (endOffset < startOffset)
        {
            throw new System.ArgumentException("The end offset must not precede the start offset.", "endOffset");
        }

        AsnSourceSpan span = new AsnSourceSpan();
        span.Start = this.GetPosition(startOffset);
        span.End = this.GetPosition(endOffset);
        return span;
    }
}
