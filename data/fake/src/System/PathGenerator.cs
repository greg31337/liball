using System;
using System.Text;

namespace fake;

internal static class PathGenerator
{
    const int DefaultMaximumDepth = 4;

    internal static string Directory(
        GenerationContext context,
        FakeRandom random)
    {
        return Directory(context, random, FilePathStyle.Unix, false);
    }

    internal static string Directory(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted)
    {
        ValidateDirectoryRequest(context, style, rooted, DefaultMaximumDepth);
        int depth = random.IntFromMinimumAndMaximumAndMaximumInclusive(1, DefaultMaximumDepth, true);
        return DirectoryUnchecked(context, random, style, rooted, depth);
    }

    internal static string Directory(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        ValidateDirectoryRequest(context, style, rooted, depth);
        return DirectoryUnchecked(context, random, style, rooted, depth);
    }

    internal static string File(
        GenerationContext context,
        FakeRandom random)
    {
        return File(context, random, FilePathStyle.Unix, false);
    }

    internal static string File(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted)
    {
        ValidateFileRequest(context, style, rooted, DefaultMaximumDepth);
        int depth = random.IntFromMinimumAndMaximumAndMaximumInclusive(1, DefaultMaximumDepth, true);
        return FileUnchecked(context, random, style, rooted, depth);
    }

    internal static string File(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        ValidateFileRequest(context, style, rooted, depth);
        return FileUnchecked(context, random, style, rooted, depth);
    }

    internal static char Separator(FilePathStyle style)
    {
        ValidateStyle(style);
        if (style == FilePathStyle.Windows)
        {
            return '\\';
        }
        return '/';
    }

    internal static string Root(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style)
    {
        ValidateStyle(style);
        if (style == FilePathStyle.Unix)
        {
            Guard.OutputSize(
                1,
                context.CapturedOptions.MaximumOutputSize,
                "pathRoot");
            return "/";
        }
        ValidateDriveData(context);
        Guard.OutputSize(
            3,
            context.CapturedOptions.MaximumOutputSize,
            "pathRoot");
        return PathData.WindowsDrives[
            random.IntFromMaximumExclusive(PathData.WindowsDrives.Length)] + ":\\";
    }

    internal static string ReservedName(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style)
    {
        ValidateStyle(style);
        string[] values = ReservedNames(style);
        ValidateSpecialNames(context, values, "reservedPathNameData");
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            "reservedPathName");
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    internal static bool IsReservedName(
        GenerationContext context,
        FilePathStyle style,
        string value)
    {
        ValidateStyle(style);
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            "value");
        UnicodeText.Validate(value, "value");
        if (style == FilePathStyle.Unix)
        {
            return value == "." || value == "..";
        }
        return IsWindowsReservedName(value);
    }

    internal static string UnicodeName(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style)
    {
        ValidateStyle(style);
        ValidateSpecialNames(
            context,
            PathData.UnicodeNames,
            "unicodePathNameData");
        int index = 0;
        while (index < PathData.UnicodeNames.Length)
        {
            ValidateSegment(PathData.UnicodeNames[index]);
            index = index + 1;
        }
        Guard.OutputSize(
            MaximumLength(PathData.UnicodeNames),
            context.CapturedOptions.MaximumOutputSize,
            "unicodePathName");
        return PathData.UnicodeNames[random.IntFromMaximumExclusive(PathData.UnicodeNames.Length)];
    }

    internal static char InvalidCharacter(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style)
    {
        char[] values = InvalidCharacterData(style);
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "invalidPathCharacters");
        Guard.OutputSize(
            1,
            context.CapturedOptions.MaximumOutputSize,
            "invalidPathCharacter");
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    internal static char[] InvalidCharacters(
        GenerationContext context,
        FilePathStyle style)
    {
        char[] values = InvalidCharacterData(style);
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "invalidPathCharacters");
        Guard.OutputSize(
            values.Length,
            context.CapturedOptions.MaximumOutputSize,
            "invalidPathCharacters");
        char[] result = new char[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static string AdversarialTraversal(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted)
    {
        ValidateStyle(style);
        if (context.GenerationModeValue != GenerationMode.Adversarial)
        {
            throw new InvalidOperationException(
                "Traversal paths require GenerationMode.Adversarial.");
        }
        ValidateFileRequest(context, style, rooted, 1);
        int maximum = MaximumDirectoryLength(style, rooted, 1) +
            7 + FileGenerator.MaximumNameLength();
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "adversarialTraversalPath");
        string directory = DirectoryUnchecked(
            context,
            random,
            style,
            rooted,
            1);
        string fileName = FileGenerator.Name(context, random);
        char separator = Separator(style);
        return directory + separator + ".." + separator + ".." +
            separator + fileName;
    }

    static string DirectoryUnchecked(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        char separator = Separator(style);
        StringBuilder result = new StringBuilder();
        if (rooted)
        {
            if (style == FilePathStyle.Windows)
            {
                result.Append(PathData.WindowsDrives[
                    random.IntFromMaximumExclusive(PathData.WindowsDrives.Length)]);
                result.Append(':');
                result.Append(separator);
            }
            else
            {
                result.Append(separator);
            }
        }
        int index = 0;
        while (index < depth)
        {
            if (index > 0)
            {
                result.Append(separator);
            }
            result.Append(PathData.DirectoryNames[
                random.IntFromMaximumExclusive(PathData.DirectoryNames.Length)]);
            index = index + 1;
        }
        string value = result.ToString();
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "directoryPath");
        return value;
    }

    static string FileUnchecked(
        GenerationContext context,
        FakeRandom random,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        string directory = DirectoryUnchecked(
            context,
            random,
            style,
            rooted,
            depth);
        string fileName = FileGenerator.Name(context, random);
        string result = directory + Separator(style) + fileName;
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "filePath");
        return result;
    }

    static void ValidateDirectoryRequest(
        GenerationContext context,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        ValidateStyle(style);
        Guard.Positive(depth, "depth");
        Guard.Count(
            depth,
            context.CapturedOptions.MaximumCount,
            "depth");
        ValidatePathData(context);
        Guard.OutputSize(
            MaximumDirectoryLength(style, rooted, depth),
            context.CapturedOptions.MaximumOutputSize,
            "directoryPath");
    }

    static void ValidateFileRequest(
        GenerationContext context,
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        ValidateDirectoryRequest(context, style, rooted, depth);
        FileGenerator.ValidateName(context);
        int maximum = MaximumDirectoryLength(style, rooted, depth) +
            1 + FileGenerator.MaximumNameLength();
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "filePath");
    }

    static void ValidatePathData(GenerationContext context)
    {
        Guard.Count(
            PathData.DirectoryNames.Length,
            context.CapturedOptions.MaximumCount,
            "pathDirectoryData");
        int index = 0;
        while (index < PathData.DirectoryNames.Length)
        {
            ValidateSegment(PathData.DirectoryNames[index]);
            index = index + 1;
        }
        ValidateDriveData(context);
    }

    static void ValidateDriveData(GenerationContext context)
    {
        Guard.Count(
            PathData.WindowsDrives.Length,
            context.CapturedOptions.MaximumCount,
            "pathDriveData");
        int index = 0;
        while (index < PathData.WindowsDrives.Length)
        {
            string drive = PathData.WindowsDrives[index];
            if (drive.Length != 1 || drive[0] < 'A' || drive[0] > 'Z')
            {
                throw FakeException.Create(
                    "The project-authored Windows drive table is malformed.");
            }
            index = index + 1;
        }
    }

    static void ValidateSpecialNames(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored path-name table is malformed.");
            }
            UnicodeText.Validate(values[index], dataName);
            index = index + 1;
        }
    }

    static void ValidateSegment(string value)
    {
        if (value.Length == 0 || value == "." || value == "..")
        {
            throw FakeException.Create(
                "The project-authored path segment table is malformed.");
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= '\u001f' || character == '"' ||
                character == '<' || character == '>' || character == '|' ||
                character == ':' || character == '*' || character == '?' ||
                character == '/' || character == '\\')
            {
                throw FakeException.Create(
                    "The project-authored path segment table contains an invalid character.");
            }
            index = index + 1;
        }
    }

    static void ValidateStyle(FilePathStyle style)
    {
        switch (style)
        {
            case FilePathStyle.Unix:
            case FilePathStyle.Windows:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "style",
                    style,
                    "style is not supported.");
        }
    }

    static char[] InvalidCharacterData(FilePathStyle style)
    {
        ValidateStyle(style);
        if (style == FilePathStyle.Windows)
        {
            return PathData.WindowsInvalidSegmentCharacters;
        }
        return PathData.UnixInvalidSegmentCharacters;
    }

    static string[] ReservedNames(FilePathStyle style)
    {
        if (style == FilePathStyle.Windows)
        {
            return PathData.WindowsReservedNames;
        }
        return PathData.UnixReservedNames;
    }

    static bool IsWindowsReservedName(string value)
    {
        int length = value.Length;
        while (length > 0 &&
            (value[length - 1] == '.' || value[length - 1] == ' '))
        {
            length = length - 1;
        }
        int baseLength = 0;
        while (baseLength < length && value[baseLength] != '.')
        {
            baseLength = baseLength + 1;
        }
        if (EqualsIgnoreCase(value, baseLength, "CON") ||
            EqualsIgnoreCase(value, baseLength, "PRN") ||
            EqualsIgnoreCase(value, baseLength, "AUX") ||
            EqualsIgnoreCase(value, baseLength, "NUL"))
        {
            return true;
        }
        if (baseLength == 4 && value[3] >= '1' && value[3] <= '9')
        {
            if ((value[0] == 'C' || value[0] == 'c') &&
                (value[1] == 'O' || value[1] == 'o') &&
                (value[2] == 'M' || value[2] == 'm'))
            {
                return true;
            }
            if ((value[0] == 'L' || value[0] == 'l') &&
                (value[1] == 'P' || value[1] == 'p') &&
                (value[2] == 'T' || value[2] == 't'))
            {
                return true;
            }
        }
        return false;
    }

    static bool EqualsIgnoreCase(
        string value,
        int length,
        string expected)
    {
        if (length != expected.Length)
        {
            return false;
        }
        return string.Equals(
            value.Substring(0, length),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    static int MaximumDirectoryLength(
        FilePathStyle style,
        bool rooted,
        int depth)
    {
        int rootLength = 0;
        if (rooted)
        {
            rootLength = style == FilePathStyle.Windows ? 3 : 1;
        }
        return rootLength + depth * MaximumLength(
            PathData.DirectoryNames) + depth - 1;
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }
}
