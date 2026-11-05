using System.Text;

namespace docx;

internal class DocxPartName
{
    internal string FullName;

    internal DocxPartName(string partName)
    {
        this.FullName = Normalize(partName);
    }

    internal static string Normalize(string partName)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        string normalized = NormalizeSeparators(partName);
        string error = GetValidationErrorForNormalizedName(normalized);

        if (error != "")
        {
            throw new ArgumentException(error, "partName");
        }

        return normalized;
    }

    internal static bool IsValid(string partName)
    {
        if (partName == null)
        {
            return false;
        }

        string normalized = NormalizeSeparators(partName);
        string error = GetValidationErrorForNormalizedName(normalized);
        return error == "";
    }

    internal static string GetValidationError(string partName)
    {
        if (partName == null)
        {
            return "Part name is required.";
        }

        string normalized = NormalizeSeparators(partName);
        return GetValidationErrorForNormalizedName(normalized);
    }

    internal static string ToZipEntryName(string partName)
    {
        string normalized = Normalize(partName);
        return normalized.Substring(1);
    }

    internal string ToZipEntryName()
    {
        return this.FullName.Substring(1);
    }

    internal static string NormalizeSeparators(string partName)
    {
        string replaced = partName.Replace('\\', '/');
        string prefixed = replaced;

        if (!prefixed.StartsWith("/"))
        {
            prefixed = "/" + prefixed;
        }

        StringBuilder builder = new StringBuilder();
        bool previousWasSlash = false;

        for (int index = 0; index < prefixed.Length; index = index + 1)
        {
            char value = prefixed[index];

            if (value == '/')
            {
                if (!previousWasSlash)
                {
                    builder.Append(value);
                }

                previousWasSlash = true;
            }
            else
            {
                builder.Append(value);
                previousWasSlash = false;
            }
        }

        return builder.ToString();
    }

    internal static string GetValidationErrorForNormalizedName(string normalizedPartName)
    {
        if (normalizedPartName == null)
        {
            return "Part name is required.";
        }

        if (normalizedPartName.Length == 0)
        {
            return "Part name is required.";
        }

        if (!normalizedPartName.StartsWith("/"))
        {
            return "Part name must start with '/'.";
        }

        if (normalizedPartName == "/")
        {
            return "Part name cannot be the package root.";
        }

        if (normalizedPartName.EndsWith("/"))
        {
            return "Part name cannot identify a directory.";
        }

        if (normalizedPartName.Contains("://"))
        {
            return "Part name cannot be an absolute URI.";
        }

        for (int index = 0; index < normalizedPartName.Length; index = index + 1)
        {
            char value = normalizedPartName[index];

            if (value == '?' || value == '#')
            {
                return "Part name cannot contain query or fragment characters.";
            }

            if (value == ':')
            {
                return "Part name cannot contain URI scheme characters.";
            }

            if (value <= ' ')
            {
                return "Part name cannot contain whitespace or control characters.";
            }
        }

        int segmentStart = 1;

        for (int index = 1; index <= normalizedPartName.Length; index = index + 1)
        {
            if (index == normalizedPartName.Length || normalizedPartName[index] == '/')
            {
                int segmentLength = index - segmentStart;

                if (segmentLength == 0)
                {
                    return "Part name cannot contain empty path segments.";
                }

                string segment = normalizedPartName.Substring(segmentStart, segmentLength);

                if (segment == "." || segment == "..")
                {
                    return "Part name cannot contain relative path segments.";
                }

                segmentStart = index + 1;
            }
        }

        return "";
    }
}
