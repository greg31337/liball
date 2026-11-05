using System.Globalization;

namespace docx;

internal class DocxIdAllocator
{
    internal string Prefix;

    internal DocxIdAllocator(string prefix)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (prefix == "")
        {
            throw new ArgumentException("ID prefix is required.", "prefix");
        }

        this.Prefix = prefix;
    }

    internal string AllocateNext(List<string> existingIds)
    {
        if (existingIds == null)
        {
            throw new ArgumentNullException("existingIds");
        }

        int candidate = 1;

        while (ContainsId(existingIds, this.Prefix + candidate.ToString(CultureInfo.InvariantCulture)))
        {
            candidate = candidate + 1;
        }

        return this.Prefix + candidate.ToString(CultureInfo.InvariantCulture);
    }

    internal bool ContainsId(List<string> existingIds, string id)
    {
        if (existingIds == null)
        {
            throw new ArgumentNullException("existingIds");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (string existingId in existingIds)
        {
            if (existingId == id)
            {
                return true;
            }
        }

        return false;
    }

    internal int GetNumericSuffix(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (!id.StartsWith(this.Prefix))
        {
            return -1;
        }

        if (id.Length == this.Prefix.Length)
        {
            return -1;
        }

        string suffix = id.Substring(this.Prefix.Length);
        int value = 0;

        for (int index = 0; index < suffix.Length; index = index + 1)
        {
            char current = suffix[index];

            if (current < '0' || current > '9')
            {
                return -1;
            }

            int digit = current - '0';

            if (value > (int.MaxValue - digit) / 10)
            {
                return -1;
            }

            value = value * 10 + digit;
        }

        if (value < 1)
        {
            return -1;
        }

        return value;
    }
}
