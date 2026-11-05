namespace chart;

internal static class ChartTickLabelOperations
{
    internal static string Prepare(string label, bool multiline, bool trim, int maximumCharacters, bool ellipsis)
    {
        if (label == null)
        {
            throw new System.ArgumentNullException("label");
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        System.Text.StringBuilder line = new System.Text.StringBuilder();
        int lineNumber = 0;
        int index = 0;
        while (index <= label.Length)
        {
            bool atEnd = index == label.Length;
            char character = atEnd ? '\n' : label[index];
            if (character == '\r')
            {
                index = index + 1;
                continue;
            }

            if (character == '\n')
            {
                string processed = ProcessLine(line.ToString(), trim, maximumCharacters, ellipsis);
                if (lineNumber > 0)
                {
                    if (multiline)
                    {
                        result.Append('\n');
                    }
                    else
                    {
                        result.Append(' ');
                    }
                }

                result.Append(processed);
                line.Clear();
                lineNumber = lineNumber + 1;
            }
            else
            {
                line.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal static string ProcessLine(string line, bool trim, int maximumCharacters, bool ellipsis)
    {
        string result = line;
        if (trim)
        {
            result = result.Trim();
        }

        if (maximumCharacters == 0 || result.Length <= maximumCharacters)
        {
            return result;
        }

        if (!ellipsis)
        {
            return result.Substring(0, maximumCharacters);
        }

        if (maximumCharacters == 1)
        {
            return "…";
        }

        return result.Substring(0, maximumCharacters - 1) + "…";
    }

    internal static int LongestLineLength(string label)
    {
        int longest = 0;
        int current = 0;
        int index = 0;
        while (index < label.Length)
        {
            if (label[index] == '\n')
            {
                if (current > longest)
                {
                    longest = current;
                }

                current = 0;
            }
            else
            {
                current = current + 1;
            }

            index = index + 1;
        }

        if (current > longest)
        {
            longest = current;
        }

        return longest;
    }

    internal static int LineCount(string label)
    {
        int count = 1;
        int index = 0;
        while (index < label.Length)
        {
            if (label[index] == '\n')
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
