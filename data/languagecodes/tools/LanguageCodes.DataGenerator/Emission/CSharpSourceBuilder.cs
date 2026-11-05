using System;
using System.Text;

namespace LanguageCodes.DataGenerator.Emission;

internal sealed class CSharpSourceBuilder
{
    private readonly StringBuilder builder = new StringBuilder();

    internal void AppendLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.Contains('\r', StringComparison.Ordinal) || line.Contains('\n', StringComparison.Ordinal))
        {
            throw new ArgumentException("A generated source line cannot contain a line-ending character.", nameof(line));
        }

        builder.Append(line);
        builder.Append('\n');
    }

    internal string Build()
    {
        return builder.ToString();
    }
}
