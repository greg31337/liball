using System.Collections.Generic;
using System.Text;

namespace odf;

internal class PackagePathValue
{
    internal string LexicalPath = "";
    internal string CanonicalPath = "";
    internal bool IsValid;
    internal bool IsCanonical;
    internal bool IsTraversal;
    internal bool IsDirectory;
}

internal static class PackagePathRules
{
    internal static PackagePathValue CanonicalizeEntryPath(string? path)
    {
        return CanonicalizePathCore(path, false);
    }

    internal static string ResolveRelativeReference(string? basePartName, string? reference)
    {
        if (basePartName == null || reference == null)
        {
            return "";
        }
        PackagePathValue basePath = CanonicalizeEntryPath(basePartName);
        if (!basePath.IsValid || basePath.IsDirectory || !OdfIriResolver.IsPackageRelativeReference(reference))
        {
            return "";
        }

        int referenceEnd = ReferencePathEnd(reference);
        string referencePath = reference.Substring(0, referenceEnd);
        string suffix = reference.Substring(referenceEnd);
        if (referencePath.Length == 0)
        {
            return basePath.CanonicalPath + suffix;
        }

        int baseSeparator = basePath.CanonicalPath.LastIndexOf('/');
        string baseDirectory = "";
        if (baseSeparator >= 0)
        {
            baseDirectory = basePath.CanonicalPath.Substring(0, baseSeparator + 1);
        }
        string combined = baseDirectory + referencePath;
        if (ReferenceTargetsDirectory(referencePath) && !combined.EndsWith("/", global::System.StringComparison.Ordinal))
        {
            combined = combined + "/";
        }
        PackagePathValue resolved = CanonicalizePathCore(combined, true);
        if (!resolved.IsValid)
        {
            return "";
        }
        return resolved.CanonicalPath + suffix;
    }

    static PackagePathValue CanonicalizePathCore(string? path, bool allowRoot)
    {
        PackagePathValue result = new PackagePathValue();
        if (path == null)
        {
            return result;
        }
        result.LexicalPath = path;
        if (path.Length == 0)
        {
            result.IsValid = allowRoot;
            result.IsCanonical = allowRoot;
            return result;
        }
        if (path[0] == '/')
        {
            result.IsTraversal = true;
            return result;
        }
        if (path.IndexOf('\\') >= 0)
        {
            result.IsTraversal = true;
            return result;
        }
        if (path.IndexOf('?') >= 0 || path.IndexOf('#') >= 0 || !OdfIriResolver.IsIriReference(path))
        {
            return result;
        }

        result.IsDirectory = path.EndsWith("/", global::System.StringComparison.Ordinal);
        string[] lexicalSegments = path.Split('/');
        int segmentCount = lexicalSegments.Length;
        if (result.IsDirectory)
        {
            segmentCount = segmentCount - 1;
        }
        List<string> canonicalSegments = new List<string>();
        bool syntaxValid = true;
        int index = 0;
        while (index < segmentCount)
        {
            string lexicalSegment = lexicalSegments[index];
            if (lexicalSegment.Length == 0)
            {
                syntaxValid = false;
                index = index + 1;
                continue;
            }
            string segment = NormalizeSegment(lexicalSegment);
            if (!OdfIriResolver.IsIriReference(segment))
            {
                return result;
            }
            if (canonicalSegments.Count == 0 && segment.IndexOf(':') >= 0)
            {
                return result;
            }
            if (segment == ".")
            {
                index = index + 1;
                continue;
            }
            if (segment == "..")
            {
                if (canonicalSegments.Count == 0)
                {
                    result.IsTraversal = true;
                    return result;
                }
                canonicalSegments.RemoveAt(canonicalSegments.Count - 1);
                index = index + 1;
                continue;
            }
            canonicalSegments.Add(segment);
            index = index + 1;
        }

        string canonical = string.Join("/", canonicalSegments);
        if (result.IsDirectory && canonical.Length > 0)
        {
            canonical = canonical + "/";
        }
        result.CanonicalPath = canonical;
        result.IsValid = syntaxValid && (allowRoot || canonical.Length > 0);
        result.IsCanonical = result.IsValid
            && string.Equals(path, canonical, global::System.StringComparison.Ordinal);
        return result;
    }

    static string NormalizeSegment(string segment)
    {
        StringBuilder normalized = new StringBuilder();
        int index = 0;
        while (index < segment.Length)
        {
            if (segment[index] == '%' && index + 2 < segment.Length)
            {
                int high = HexValue(segment[index + 1]);
                int low = HexValue(segment[index + 2]);
                int value = high * 16 + low;
                if (IsUnreservedAscii(value))
                {
                    normalized.Append((char)value);
                }
                else if (value >= 128 && IsValidPercentUtf8Sequence(segment, index, value))
                {
                    int width = Utf8SequenceWidth(value);
                    normalized.Append(DecodePercentUtf8Sequence(segment, index, width));
                    index = index + width * 3;
                    continue;
                }
                else
                {
                    normalized.Append('%');
                    normalized.Append(UpperHex(high));
                    normalized.Append(UpperHex(low));
                }
                index = index + 3;
                continue;
            }
            normalized.Append(segment[index]);
            index = index + 1;
        }
        return normalized.ToString().Normalize(NormalizationForm.FormC);
    }

    static bool IsValidPercentUtf8Sequence(string segment, int index, int first)
    {
        int width = Utf8SequenceWidth(first);
        if (width == 0 || index + width * 3 > segment.Length)
        {
            return false;
        }
        int byteIndex = 1;
        while (byteIndex < width)
        {
            int percentIndex = index + byteIndex * 3;
            if (segment[percentIndex] != '%')
            {
                return false;
            }
            int value = HexValue(segment[percentIndex + 1]) * 16
                + HexValue(segment[percentIndex + 2]);
            if (value < 128 || value > 191)
            {
                return false;
            }
            if (byteIndex == 1 && first == 224 && value < 160)
            {
                return false;
            }
            if (byteIndex == 1 && first == 237 && value > 159)
            {
                return false;
            }
            if (byteIndex == 1 && first == 240 && value < 144)
            {
                return false;
            }
            if (byteIndex == 1 && first == 244 && value > 143)
            {
                return false;
            }
            byteIndex = byteIndex + 1;
        }
        return true;
    }

    static int Utf8SequenceWidth(int first)
    {
        if (first >= 194 && first <= 223)
        {
            return 2;
        }
        if (first >= 224 && first <= 239)
        {
            return 3;
        }
        if (first >= 240 && first <= 244)
        {
            return 4;
        }
        return 0;
    }

    static string DecodePercentUtf8Sequence(string segment, int index, int width)
    {
        int first = HexValue(segment[index + 1]) * 16 + HexValue(segment[index + 2]);
        int character;
        if (width == 2)
        {
            int second = PercentByte(segment, index + 3);
            character = ((first & 31) << 6) | (second & 63);
        }
        else if (width == 3)
        {
            int second = PercentByte(segment, index + 3);
            int third = PercentByte(segment, index + 6);
            character = ((first & 15) << 12) | ((second & 63) << 6) | (third & 63);
        }
        else
        {
            int second = PercentByte(segment, index + 3);
            int third = PercentByte(segment, index + 6);
            int fourth = PercentByte(segment, index + 9);
            character = ((first & 7) << 18)
                | ((second & 63) << 12)
                | ((third & 63) << 6)
                | (fourth & 63);
        }
        return char.ConvertFromUtf32(character);
    }

    static int PercentByte(string segment, int index)
    {
        return HexValue(segment[index + 1]) * 16 + HexValue(segment[index + 2]);
    }

    static bool IsUnreservedAscii(int value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z'
            || value >= '0' && value <= '9'
            || value == '-'
            || value == '.'
            || value == '_'
            || value == '~';
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return value - 'a' + 10;
    }

    static char UpperHex(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }
        return (char)('A' + value - 10);
    }

    static bool ReferenceTargetsDirectory(string referencePath)
    {
        return referencePath.EndsWith("/", global::System.StringComparison.Ordinal)
            || referencePath == "."
            || referencePath == ".."
            || referencePath.EndsWith("/.", global::System.StringComparison.Ordinal)
            || referencePath.EndsWith("/..", global::System.StringComparison.Ordinal);
    }

    static int ReferencePathEnd(string value)
    {
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (query < 0)
        {
            return fragment < 0 ? value.Length : fragment;
        }
        if (fragment < 0)
        {
            return query;
        }
        return query < fragment ? query : fragment;
    }
}
