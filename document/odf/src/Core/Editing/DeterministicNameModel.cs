using System;
using System.Collections.Generic;

namespace odf;

internal static class DeterministicNameModel
{
    internal static string CreateUniqueName(
        OdfDocument document,
        string requested)
    {
        RequireDocumentAndValue(document, requested, "name");
        return CreateReservedName(
            document,
            requested,
            new List<string>());
    }

    internal static string CreateUniqueIdentifier(
        OdfDocument document,
        string requested)
    {
        RequireDocumentAndValue(document, requested, "identifier");
        return CreateReservedIdentifier(
            document,
            requested,
            new List<string>());
    }

    internal static string CreateUniquePackagePath(
        OdfDocument document,
        string requested)
    {
        RequireDocumentAndValue(document, requested, "package path");
        PackagePathValue parsed = PackagePathRules.CanonicalizeEntryPath(
            requested);
        if (!parsed.IsValid || !parsed.IsCanonical)
        {
            throw new OdfEditException(
                "A deterministic package name requires a canonical safe path.");
        }
        if (!PackagePathExists(document, requested))
        {
            return requested;
        }
        int number = 2;
        string candidate = SuffixedPath(requested, number);
        while (PackagePathExists(document, candidate))
        {
            number = number + 1;
            candidate = SuffixedPath(requested, number);
        }
        return candidate;
    }

    internal static string CreateReservedName(
        OdfDocument document,
        string requested,
        List<string> reserved)
    {
        RequireDocumentAndValue(document, requested, "name");
        if (reserved == null)
        {
            throw new OdfEditException(
                "A reserved name collection is required.");
        }
        if (!NameExists(document, requested)
            && !Contains(reserved, requested))
        {
            return requested;
        }
        int number = 2;
        string candidate = requested + " Copy 2";
        while (NameExists(document, candidate)
            || Contains(reserved, candidate))
        {
            number = number + 1;
            candidate = requested + " Copy " + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    internal static string CreateReservedIdentifier(
        OdfDocument document,
        string requested,
        List<string> reserved)
    {
        RequireDocumentAndValue(document, requested, "identifier");
        if (reserved == null)
        {
            throw new OdfEditException(
                "A reserved identifier collection is required.");
        }
        string basis = OdfNameRules.IsXmlId(requested)
            ? requested : "id";
        if (!IdentifierExists(document, basis)
            && !Contains(reserved, basis))
        {
            return basis;
        }
        int number = 2;
        string candidate = basis + "-2";
        while (IdentifierExists(document, candidate)
            || Contains(reserved, candidate))
        {
            number = number + 1;
            candidate = basis + "-" + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static bool NameExists(OdfDocument document, string value)
    {
        return new CrossPartQueryIndex(document).FindNames(value).Length > 0;
    }

    static bool IdentifierExists(OdfDocument document, string value)
    {
        return new CrossPartQueryIndex(document).FindIds(value).Length > 0;
    }

    static bool PackagePathExists(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            if (document.PackageEntries[index].Path == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string SuffixedPath(string path, int number)
    {
        bool directory = path.EndsWith(
            "/",
            StringComparison.Ordinal);
        string value = directory
            ? path.Substring(0, path.Length - 1)
            : path;
        int separator = value.LastIndexOf('/');
        int dot = value.LastIndexOf('.');
        if (dot <= separator)
        {
            dot = value.Length;
        }
        string suffix = "-" + number.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
        string result = value.Substring(0, dot)
            + suffix + value.Substring(dot);
        return directory ? result + "/" : result;
    }

    static bool Contains(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RequireDocumentAndValue(
        OdfDocument document,
        string value,
        string description)
    {
        if (document == null || value == null || value.Length == 0)
        {
            throw new OdfEditException(
                "A document and non-empty " + description + " are required.");
        }
    }
}
