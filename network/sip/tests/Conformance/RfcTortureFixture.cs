namespace sip.Tests;

internal static class RfcTortureFixture
{
    internal static byte[] Read4475(string name)
    {
        return File.ReadAllBytes(Path.Combine(Root(), "Rfc4475", name + ".dat"));
    }

    internal static byte[] Read5118(string name)
    {
        byte[] source = File.ReadAllBytes(Path.Combine(Root(), "Rfc5118", name + ".dat"));
        string text = System.Text.Encoding.ASCII.GetString(source).Replace("\n", "\r\n");
        int separator = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (separator < 0)
        {
            text = text + "\r\n";
            separator = text.Length - 4;
        }

        int bodyLength = System.Text.Encoding.ASCII.GetByteCount(text.Substring(separator + 4));
        int header = text.IndexOf("Content-Length:", StringComparison.OrdinalIgnoreCase);
        if (header >= 0)
        {
            int valueStart = header + "Content-Length:".Length;
            while (valueStart < text.Length && text[valueStart] == ' ')
            {
                valueStart = valueStart + 1;
            }
            int valueEnd = valueStart;
            while (valueEnd < text.Length && text[valueEnd] >= '0' && text[valueEnd] <= '9')
            {
                valueEnd = valueEnd + 1;
            }
            text = text.Substring(0, valueStart)
                + bodyLength.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + text.Substring(valueEnd);
        }

        return System.Text.Encoding.ASCII.GetBytes(text);
    }

    static string Root()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "todo.md")))
        {
            directory = directory.Parent;
        }
        if (directory == null)
        {
            throw new DirectoryNotFoundException("The repository root containing todo.md was not found.");
        }
        return Path.Combine(directory.FullName, "tests", "Conformance", "Fixtures");
    }
}
