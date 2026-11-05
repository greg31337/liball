using System;

namespace fake;

internal static class SpanDestinationWriter
{
    internal static int Write(Span<char> destination, string value)
    {
        if (destination.Length < value.Length)
        {
            throw new ArgumentException(
                "destination must contain at least " + value.Length +
                " characters.",
                "destination");
        }
        value.AsSpan().CopyTo(destination);
        return value.Length;
    }
}
