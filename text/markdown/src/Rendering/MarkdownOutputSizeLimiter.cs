using System;

namespace markdown;

internal class MarkdownOutputSizeLimiter
{
    internal int MaximumOutputSize;
    internal int OutputSize;

    internal MarkdownOutputSizeLimiter(int maximumOutputSize)
    {
        MarkdownOptions.ValidatePositiveLimit(maximumOutputSize, "maximumOutputSize");
        MaximumOutputSize = maximumOutputSize;
        OutputSize = 0;
    }

    internal void Add(string text)
    {
        EnsureCanAdd(text);
        CommitCount(text.Length);
    }

    internal void EnsureCanAdd(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length > MaximumOutputSize - OutputSize)
        {
            throw new InvalidOperationException("Rendered output exceeds the configured maximum output size.");
        }
    }

    internal void CommitCount(int count)
    {
        if (count < 0 || count > MaximumOutputSize - OutputSize)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        OutputSize = OutputSize + count;
    }
}
