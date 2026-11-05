using System;

namespace markdown;

internal class MarkdownEntityDecodeResult
{
    internal bool Success;
    internal string Text;
    internal int Length;

    internal MarkdownEntityDecodeResult()
    {
        Success = false;
        Text = "";
        Length = 0;
    }

    internal static MarkdownEntityDecodeResult Create(bool success, string text, int length)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        MarkdownEntityDecodeResult result = new MarkdownEntityDecodeResult();
        result.Success = success;
        result.Text = text;
        result.Length = length;
        return result;
    }

    internal static MarkdownEntityDecodeResult NoMatch()
    {
        return new MarkdownEntityDecodeResult();
    }
}
