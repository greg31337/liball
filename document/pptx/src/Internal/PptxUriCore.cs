using System.Collections.Generic;

namespace pptx;

internal static class PptxUriCore
{
    internal const string EmptyPartName = "";
    internal static string NormalizePartName(string partName)
    {
        string normalized = NormalizePartNameText(partName);
        PptxResult result = ValidateNormalizedPartName(normalized);

        if (result.Failed())
        {
            throw new System.ArgumentException(result.FormatDiagnostics());
        }

        return normalized;
    }
    internal static bool IsValidPartName(string partName)
    {
        PptxResult result = ValidatePartName(partName);
        return result.Succeeded();
    }
    internal static PptxResult ValidatePartName(string partName)
    {
        string normalized = NormalizePartNameText(partName);
        return ValidateNormalizedPartName(normalized);
    }
    internal static string ToZipEntryName(string partName)
    {
        string normalized = NormalizePartName(partName);
        return normalized.Substring(1);
    }
    internal static bool IsExternalTarget(string target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.Length >= 2 && target[0] == '/' && target[1] == '/')
        {
            return true;
        }

        int colonIndex = target.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        for (int index = 0; index < colonIndex; index++)
        {
            char value = target[index];

            if (value == '/' || value == '\\' || value == '#' || value == '?')
            {
                return false;
            }

            if (!IsSchemeCharacter(value, index))
            {
                return false;
            }
        }

        return true;
    }

    internal static string NormalizeInternalRelationshipTarget(string sourcePartName, string target)
    {
        PptxResult result = ValidateInternalRelationshipTarget(sourcePartName, target);

        if (result.Failed())
        {
            throw new System.ArgumentException(result.FormatDiagnostics());
        }

        return BuildInternalRelationshipTarget(sourcePartName, target);
    }
    internal static bool IsValidInternalRelationshipTarget(string sourcePartName, string target)
    {
        PptxResult result = ValidateInternalRelationshipTarget(sourcePartName, target);
        return result.Succeeded();
    }
    internal static PptxResult ValidateInternalRelationshipTarget(string sourcePartName, string target)
    {
        PptxResult result = new PptxResult();
        PptxResult sourceResult = ValidatePartName(sourcePartName);
        CopyDiagnostics(sourceResult, result);

        if (string.IsNullOrWhiteSpace(target))
        {
            result.AddError("URI020", "Relationship target is required.", "", "", "");
            return result;
        }

        if (IsExternalTarget(target))
        {
            result.AddError("URI021", "Relationship target is external and cannot be normalized as an internal package target.", "", "", "");
            return result;
        }

        if (target.IndexOf('?') >= 0)
        {
            result.AddError("URI022", "Relationship target query strings are not allowed for internal package targets.", "", "", "");
        }

        if (HasInvalidFragment(target))
        {
            result.AddError("URI023", "Relationship target fragment contains invalid characters.", "", "", "");
        }

        if (sourceResult.Failed())
        {
            return result;
        }

        string path = ExtractTargetPath(target);
        if (path.Length == 0)
        {
            if (ExtractFragment(target).Length == 0)
            {
                result.AddError("URI024", "Relationship target path is required unless a fragment is present.", "", "", "");
            }

            return result;
        }

        if (HasEmptyInternalTargetSegment(path))
        {
            result.AddError("URI025", "Relationship target cannot contain empty path segments.", "", "", "");
        }

        if (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\')
        {
            result.AddError("URI026", "Relationship target cannot end with '/'.", "", "", "");
        }

        string resolved = ResolveInternalPath(NormalizePartName(sourcePartName), path);
        AddResolvedPartNameErrors(resolved, result);

        return result;
    }
    internal static string BuildInternalRelationshipTarget(string sourcePartName, string target)
    {
        string source = NormalizePartName(sourcePartName);
        string path = ExtractTargetPath(target);
        string fragment = ExtractFragment(target);

        if (path.Length == 0)
        {
            return source + "#" + fragment;
        }

        string resolved = ResolveInternalPath(source, path);

        if (fragment.Length > 0)
        {
            return resolved + "#" + fragment;
        }

        return resolved;
    }

    internal static string NormalizePartNameText(string partName)
    {
        if (partName == null)
        {
            return EmptyPartName;
        }

        string normalized = partName.Replace('\\', '/');
        if (normalized.Length == 0)
        {
            return normalized;
        }

        if (normalized[0] != '/')
        {
            normalized = "/" + normalized;
        }

        return normalized;
    }

    internal static PptxResult ValidateNormalizedPartName(string partName)
    {
        PptxResult result = new PptxResult();
        AddResolvedPartNameErrors(partName, result);
        return result;
    }

    internal static void AddResolvedPartNameErrors(string partName, PptxResult result)
    {
        if (string.IsNullOrWhiteSpace(partName))
        {
            result.AddError("URI001", "Part name is required.", partName, "", "");
            return;
        }

        if (partName[0] != '/')
        {
            result.AddError("URI002", "Part name must start with '/'.", partName, "", "");
        }

        if (partName.Length == 1)
        {
            result.AddError("URI003", "Part name must include at least one path segment.", partName, "", "");
        }

        if (partName[partName.Length - 1] == '/')
        {
            result.AddError("URI004", "Part name cannot end with '/'.", partName, "", "");
        }

        if (partName.IndexOf("//") >= 0)
        {
            result.AddError("URI005", "Part name cannot contain empty path segments.", partName, "", "");
        }

        if (partName.IndexOf('#') >= 0 || partName.IndexOf('?') >= 0)
        {
            result.AddError("URI006", "Part name cannot contain a query string or fragment.", partName, "", "");
        }

        if (partName.IndexOf(':') >= 0)
        {
            result.AddError("URI007", "Part name cannot contain a URI scheme or drive prefix.", partName, "", "");
        }

        if (HasInvalidPathCharacters(partName))
        {
            result.AddError("URI008", "Part name contains invalid characters.", partName, "", "");
        }

        if (HasInvalidPercentEncoding(partName))
        {
            result.AddError("URI009", "Part name contains invalid or unsafe percent encoding.", partName, "", "");
        }

        if (HasDotSegment(partName))
        {
            result.AddError("URI010", "Part name cannot contain dot segments.", partName, "", "");
        }
    }

    internal static string ResolveInternalPath(string sourcePartName, string targetPath)
    {
        string path = targetPath.Replace('\\', '/');

        if (path.Length > 0 && path[0] == '/')
        {
            return CollapseAbsolutePath(path);
        }

        string parent = GetParentPartDirectory(sourcePartName);
        return CollapseAbsolutePath(parent + "/" + path);
    }

    internal static string CollapseAbsolutePath(string path)
    {
        List<string> segments = new List<string>();
        string current = "";

        for (int index = 0; index < path.Length; index++)
        {
            char value = path[index];

            if (value == '/')
            {
                ApplyPathSegment(segments, current);
                current = "";
            }
            else
            {
                current = current + value;
            }
        }

        ApplyPathSegment(segments, current);

        string result = "";
        for (int index = 0; index < segments.Count; index++)
        {
            result = result + "/" + segments[index];
        }

        if (result.Length == 0)
        {
            return "/";
        }

        return result;
    }

    internal static void ApplyPathSegment(List<string> segments, string segment)
    {
        if (segment.Length == 0)
        {
            return;
        }

        if (segment == ".")
        {
            return;
        }

        if (segment == "..")
        {
            if (segments.Count == 0)
            {
                segments.Add("..");
                return;
            }

            segments.RemoveAt(segments.Count - 1);
            return;
        }

        segments.Add(segment);
    }

    internal static string GetParentPartDirectory(string sourcePartName)
    {
        string source = NormalizePartName(sourcePartName);
        int slashIndex = source.LastIndexOf('/');

        if (slashIndex <= 0)
        {
            return "";
        }

        return source.Substring(0, slashIndex);
    }

    internal static string ExtractTargetPath(string target)
    {
        if (target == null)
        {
            return "";
        }

        int fragmentIndex = target.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            return target.Substring(0, fragmentIndex);
        }

        return target;
    }

    internal static string ExtractFragment(string target)
    {
        if (target == null)
        {
            return "";
        }

        int fragmentIndex = target.IndexOf('#');
        if (fragmentIndex < 0)
        {
            return "";
        }

        if (fragmentIndex == target.Length - 1)
        {
            return "";
        }

        return target.Substring(fragmentIndex + 1);
    }

    internal static bool HasInvalidFragment(string target)
    {
        string fragment = ExtractFragment(target);

        for (int index = 0; index < fragment.Length; index++)
        {
            char value = fragment[index];
            if (value <= ' ' || value == '\\')
            {
                return true;
            }
        }

        return HasInvalidPercentEncoding(fragment);
    }

    internal static bool HasEmptyInternalTargetSegment(string path)
    {
        string normalized = path.Replace('\\', '/');
        int start = 0;

        if (normalized.Length > 0 && normalized[0] == '/')
        {
            start = 1;
        }

        for (int index = start; index < normalized.Length - 1; index++)
        {
            if (normalized[index] == '/' && normalized[index + 1] == '/')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasDotSegment(string partName)
    {
        string current = "";

        for (int index = 0; index < partName.Length; index++)
        {
            char value = partName[index];

            if (value == '/')
            {
                if (current == "." || current == "..")
                {
                    return true;
                }

                current = "";
            }
            else
            {
                current = current + value;
            }
        }

        if (current == "." || current == "..")
        {
            return true;
        }

        return false;
    }

    internal static bool HasInvalidPathCharacters(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];

            if (current <= ' ')
            {
                return true;
            }

            if (current == '\\' || current == '"' || current == '<' || current == '>' || current == '|')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasInvalidPercentEncoding(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length)
            {
                return true;
            }

            char first = ToUpperAscii(value[index + 1]);
            char second = ToUpperAscii(value[index + 2]);

            if (!IsHexCharacter(first) || !IsHexCharacter(second))
            {
                return true;
            }

            if (first == '2' && second == 'E')
            {
                return true;
            }

            if (first == '2' && second == 'F')
            {
                return true;
            }

            if (first == '5' && second == 'C')
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsSchemeCharacter(char value, int index)
    {
        if (index == 0)
        {
            return IsAsciiLetter(value);
        }

        if (IsAsciiLetter(value))
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value == '+' || value == '-' || value == '.')
        {
            return true;
        }

        return false;
    }

    internal static bool IsAsciiLetter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        return false;
    }

    internal static bool IsHexCharacter(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value >= 'A' && value <= 'F')
        {
            return true;
        }

        if (value >= 'a' && value <= 'f')
        {
            return true;
        }

        return false;
    }

    internal static char ToUpperAscii(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return (char)(value - 32);
        }

        return value;
    }

    internal static void CopyDiagnostics(PptxResult source, PptxResult target)
    {
        for (int index = 0; index < source.Diagnostics.Count; index++)
        {
            target.Add(source.Diagnostics[index]);
        }
    }
}

