using System;
using System.IO;

namespace markdown;

internal class PlainTextWriter
{
    internal TextWriter Writer;
    internal MarkdownOutputSizeLimiter OutputSizeLimiter;

    internal PlainTextWriter(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        Writer = writer;
        OutputSizeLimiter = new MarkdownOutputSizeLimiter(int.MaxValue);
    }

    internal static PlainTextWriter Create(TextWriter writer, int maximumOutputSize)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        PlainTextWriter plainTextWriter = new PlainTextWriter(writer);
        plainTextWriter.OutputSizeLimiter = new MarkdownOutputSizeLimiter(maximumOutputSize);
        return plainTextWriter;
    }

    internal void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        OutputSizeLimiter.Add(text);
        Writer.Write(text);
    }

    internal void WriteLineEnding(string lineEnding)
    {
        if (lineEnding == null)
        {
            throw new ArgumentNullException("lineEnding");
        }

        Write(lineEnding);
    }
}
